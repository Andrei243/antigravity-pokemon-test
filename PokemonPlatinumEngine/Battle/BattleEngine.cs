using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

public enum BattleResult
{
    None,
    PlayerVictory,
    PlayerDefeat,
    EnemyCaught,
    PlayerRan
}

/// <summary>
/// The rules of a battle, single or double. Each place on the field is a <see cref="Battler"/>; the player picks
/// an action for each of theirs, the AI for the others, and the turn plays out as a queue of messages:
/// <c>QueueMessage(text, onComplete, onShow)</c>, where <c>onShow</c> starts what the message describes and
/// <c>onComplete</c> carries on once the player dismisses it. Abilities and held items join in through
/// <see cref="BattleEffect"/> hooks. Split over BattleEngine.*.cs: menus, the turn, moves, and the context the
/// effects use.
/// </summary>
public partial class BattleEngine : IBattleContext
{
    public Party PlayerParty { get; }
    public Inventory PlayerInventory { get; }
    public Pokedex Pokedex { get; }
    public BattleFormat Format { get; }

    /// <summary>The opposing trainers (none in a wild battle).</summary>
    public IReadOnlyList<Trainer> Trainers { get; }
    public Trainer? OpponentTrainer => Trainers.Count > 0 ? Trainers[0] : null;

    /// <summary>The first trainer's team, for the ball tray on the HUD.</summary>
    public Party? EnemyParty => OpponentTrainer?.Party;

    public bool IsTrainerBattle => Trainers.Count > 0;
    public bool IsDouble => Format == BattleFormat.Double;

    /// <summary>The places on each side: one each in a single battle, two in a double.</summary>
    public IReadOnlyList<Battler> PlayerSlots { get; }
    public IReadOnlyList<Battler> EnemySlots { get; }
    public IEnumerable<Battler> AllBattlers => PlayerSlots.Concat(EnemySlots);

    /// <summary>The player's Pokémon the menus are acting for (the only one in a single battle).</summary>
    public Pokemon PlayerPokemon => MenuBattler.Pokemon!;

    /// <summary>The first foe (the only one in a single battle).</summary>
    public Pokemon EnemyPokemon => EnemySlots[0].Pokemon!;

    /// <summary>The place whose action the player is choosing, or which needs a Pokémon sent in.</summary>
    public Battler MenuBattler => PlayerSlots[Math.Clamp(menuSlot, 0, PlayerSlots.Count - 1)];

    public BattleHUD HUD { get; } = new();

    /// <summary>What the battle looks like: send-outs, attacks, hits, faints and the HP bars as they drain.</summary>
    public BattleAnimator Anim { get; } = new();

    public Random Random => rng;

    private readonly Queue<Action> turnEventQueue = new();
    private string currentMessage = "";
    private float messageWaitTimer = 0f;
    private bool waitingForMessageConfirm = false;

    // Effects that land a moment after the message that started them, like a hit after the attacker's lunge
    private const float HitDelay = 0.35f;
    private readonly List<(float Delay, Action Effect)> pendingEffects = new();

    private readonly List<Pokemon>? pcBoxStorage;
    private Action? currentMessageCallback = null;

    /// <summary>
    /// The player's Pokémon that gained a level in this battle. They are the ones that may evolve once it is over,
    /// as in the games: nothing changes species while the battle is on.
    /// </summary>
    public IReadOnlyList<Pokemon> LeveledUp => leveledUpPokemon;
    private readonly List<Pokemon> leveledUpPokemon = new();

    public BattleResult Result { get; private set; } = BattleResult.None;
    public bool IsBattleOver => Result != BattleResult.None && !waitingForMessageConfirm && turnEventQueue.Count == 0;

    private readonly Random rng;

    public BattleEngine(
        Party playerParty,
        Pokemon wildOrEnemyPokemon,
        Inventory playerInventory,
        Pokedex pokedex,
        Trainer? trainer = null,
        List<Pokemon>? pcStorage = null)
        : this(new BattleSetup
        {
            PlayerParty = playerParty,
            Inventory = playerInventory,
            Pokedex = pokedex,
            PcStorage = pcStorage,
            WildPokemon = trainer == null ? new List<Pokemon> { wildOrEnemyPokemon } : new List<Pokemon>(),
            Trainers = trainer == null ? new List<Trainer>() : new List<Trainer> { trainer },
            FirstTrainerPokemon = trainer != null ? wildOrEnemyPokemon : null
        })
    {
    }

    public BattleEngine(BattleSetup setup)
    {
        PlayerParty = setup.PlayerParty;
        PlayerInventory = setup.Inventory;
        Pokedex = setup.Pokedex;
        pcBoxStorage = setup.PcStorage;
        Trainers = setup.Trainers;
        rng = setup.Random ?? Core.Dice.New();

        // A double battle needs two Pokémon able to fight on each side
        bool canDouble = setup.PlayerParty.Members.Count(p => !p.IsFainted) >= 2 &&
            (setup.Trainers.Count == 0 ? setup.WildPokemon.Count >= 2
                : setup.Trainers.Count >= 2 || setup.Trainers[0].Party.Members.Count(p => !p.IsFainted) >= 2);
        Format = setup.Format == BattleFormat.Double && canDouble ? BattleFormat.Double : BattleFormat.Single;
        int slots = IsDouble ? 2 : 1;
        Anim.Slots = slots;

        PlayerSlots = Enumerable.Range(0, slots).Select(i => new Battler(BattleSide.Player, i) { Roster = PlayerParty }).ToList();
        EnemySlots = Enumerable.Range(0, slots).Select(i => new Battler(BattleSide.Enemy, i)).ToList();

        // The first Pokémon in: the first ones able to fight
        var leads = PlayerParty.Members.Where(p => !p.IsFainted).Take(slots).ToList();
        if (leads.Count == 0) leads.Add(PlayerParty.Members.First());
        for (int i = 0; i < leads.Count; i++) PlayerSlots[i].Pokemon = leads[i];

        for (int i = 0; i < slots; i++)
        {
            var place = EnemySlots[i];
            if (IsTrainerBattle)
            {
                var trainer = Trainers[Math.Min(i, Trainers.Count - 1)];
                place.Trainer = trainer;
                place.Roster = trainer.Party;
                place.Pokemon = i == 0 && setup.FirstTrainerPokemon != null
                    ? setup.FirstTrainerPokemon
                    : NextFromRoster(place, exclude: EnemySlots.Take(i).Select(b => b.Pokemon));
            }
            else if (i < setup.WildPokemon.Count)
            {
                place.Pokemon = setup.WildPokemon[i];
            }
        }

        foreach (var foe in EnemySlots.Where(b => b.Pokemon != null)) Pokedex.RegisterSeen(foe.Pokemon!.Species.DexNumber);
        foreach (var b in AllBattlers.Where(b => b.Pokemon != null)) b.Pokemon!.ResetStatStages();

        PlayIntro();
        AdvanceEventQueue();
    }

    /// <summary>The trainer's opening lines and the send-outs, then the abilities that act on entry.</summary>
    private void PlayIntro()
    {
        var foes = EnemySlots.Where(b => b.Pokemon != null).ToList();
        var mine = PlayerSlots.Where(b => b.Pokemon != null).ToList();

        void SendOutMine() => QueueMessage($"Go! {JoinNames(mine.Select(b => b.Pokemon!.DisplayName))}!", () =>
        {
            foreach (var b in mine) OnEntered(b);
            RunEntryEffects(AllBattlers.Where(b => b.IsActive).ToList(), StartTurn);
        }, onShow: () => { foreach (var b in mine) Anim.SendOut(BattleSide.Player, b.Pokemon!, b.Slot); });

        if (IsTrainerBattle)
        {
            Anim.EnemyTrainer = Trainers[0].TrainerClass;
            if (Trainers.Count > 1) Anim.EnemyTrainer2 = Trainers[1].TrainerClass;
            string who = Trainers.Count > 1 ? $"{Trainers[0].FullTitle} and {Trainers[1].FullTitle} would like" : $"{Trainers[0].FullTitle} wants";
            QueueMessage($"{who} to battle!", () =>
            {
                // Each trainer announces their own Pokémon
                var byTrainer = foes.GroupBy(b => b.Trainer!).ToList();
                void Announce(int i)
                {
                    if (i == byTrainer.Count)
                    {
                        SendOutMine();
                        return;
                    }
                    var group = byTrainer[i].ToList();
                    QueueMessage($"{group[0].Trainer!.FullTitle} sent out {JoinNames(group.Select(b => b.Pokemon!.DisplayName))}!", () => Announce(i + 1),
                        onShow: () =>
                        {
                            foreach (var b in group)
                            {
                                OnEntered(b);
                                Anim.SendOut(BattleSide.Enemy, b.Pokemon!, b.Slot);
                            }
                        });
                }
                Announce(0);
            });
        }
        else
        {
            foreach (var b in foes)
            {
                OnEntered(b);
                Anim.Appear(BattleSide.Enemy, b.Pokemon!, b.Slot);
            }
            string text = foes.Count > 1
                ? $"Wild {JoinNames(foes.Select(b => b.Pokemon!.DisplayName))} appeared!"
                : $"A wild {foes[0].Pokemon!.DisplayName} appeared!";
            QueueMessage(text, SendOutMine);
        }
    }

    private static string JoinNames(IEnumerable<string> names)
    {
        var list = names.ToList();
        return list.Count <= 1 ? list.FirstOrDefault() ?? "" : string.Join(", ", list.Take(list.Count - 1)) + " and " + list[^1];
    }

    public void Update(float dt)
    {
        Anim.Update(dt, (side, slot) => SlotsOf(side).ElementAtOrDefault(slot)?.Pokemon);
        UpdatePendingEffects(dt);

        if (waitingForMessageConfirm)
        {
            if (InputManager.IsActionPressed(GameAction.Confirm) || InputManager.IsActionPressed(GameAction.Cancel) || Raylib_cs.Raylib.IsKeyPressed(Raylib_cs.KeyboardKey.Enter))
            {
                ConfirmMessage();
            }
            return;
        }

        if (messageWaitTimer > 0f)
        {
            messageWaitTimer -= dt;
            if (messageWaitTimer <= 0f)
            {
                AdvanceEventQueue();
            }
            return;
        }

        if (HUD.MenuState == BattleMenuState.Message)
        {
            if (turnEventQueue.Count > 0)
            {
                AdvanceEventQueue();
            }
            return;
        }

        // Handle Menu Navigation
        HandleMenuInput();
    }

    private IReadOnlyList<Battler> SlotsOf(BattleSide side) => side == BattleSide.Player ? PlayerSlots : EnemySlots;

    /// <summary>The message currently on screen.</summary>
    public string CurrentMessage => currentMessage;

    /// <summary>A message is on screen waiting to be dismissed.</summary>
    public bool IsWaitingForConfirm => waitingForMessageConfirm;

    /// <summary>Dismisses the message on screen and carries on with whatever it was waiting to trigger.</summary>
    public void ConfirmMessage()
    {
        if (!waitingForMessageConfirm) return;

        // Anything the message started lands now, so the battle logic never runs ahead of it
        FlushPendingEffects();

        waitingForMessageConfirm = false;
        var cb = currentMessageCallback;
        currentMessageCallback = null;
        cb?.Invoke();

        // A timed animation (the Poké Ball throw) holds the queue; Update releases it when the animation ends
        if (messageWaitTimer > 0f) return;

        if (turnEventQueue.Count > 0)
        {
            AdvanceEventQueue();
        }
    }

    private void QueueMessageSequence(List<string> msgs, Action? onComplete)
    {
        if (msgs == null || msgs.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        void QueueNext(int index)
        {
            if (index < msgs.Count - 1)
            {
                QueueMessage(msgs[index], () => QueueNext(index + 1));
            }
            else
            {
                QueueMessage(msgs[index], onComplete);
            }
        }

        QueueNext(0);
    }

    /// <param name="onComplete">Runs once the player dismisses the message.</param>
    /// <param name="onShow">Runs as the message appears (starts the animation the message describes).</param>
    private void QueueMessage(string msg, Action? onComplete = null, Action? onShow = null)
    {
        turnEventQueue.Enqueue(() =>
        {
            currentMessage = msg;
            waitingForMessageConfirm = true;
            HUD.MenuState = BattleMenuState.Message;
            currentMessageCallback = onComplete;
            onShow?.Invoke();
        });
    }

    private void After(float delay, Action effect) => pendingEffects.Add((delay, effect));

    private void UpdatePendingEffects(float dt)
    {
        for (int i = 0; i < pendingEffects.Count; i++)
        {
            var (delay, effect) = pendingEffects[i];
            if ((delay -= dt) > 0f)
            {
                pendingEffects[i] = (delay, effect);
                continue;
            }
            pendingEffects.RemoveAt(i--);
            effect();
        }
    }

    private void FlushPendingEffects()
    {
        while (pendingEffects.Count > 0)
        {
            var effect = pendingEffects[0].Effect;
            pendingEffects.RemoveAt(0);
            effect();
        }
    }

    private void AdvanceEventQueue()
    {
        if (turnEventQueue.Count > 0)
        {
            var next = turnEventQueue.Dequeue();
            next.Invoke();
        }
    }

    /// <summary>Draws the HUD and menus over the battle field (see <see cref="Graphics.BattleRenderer"/>, which draws the move effects).</summary>
    public void Draw(int screenWidth, int screenHeight)
    {
        HUD.Draw(
            screenWidth,
            screenHeight,
            this,
            currentMessage,
            Anim,
            PlayerInventory);
    }
}
