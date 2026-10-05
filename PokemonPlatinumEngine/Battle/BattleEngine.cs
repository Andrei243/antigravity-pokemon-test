using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle.Sim;
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
/// A battle as the game shows it, single or double. The rules are not here: they are <see cref="BattleCore"/>,
/// which works on copies of the Pokémon and writes down what happened. This class is the face the game, the tests
/// and the screenshot harness use: it turns the menus' choices into <see cref="BattleChoice"/>s, hands them to
/// the core, and plays the core's log back as a queue of messages. Each line starts what it describes as it
/// appears (<see cref="BattleAnimator"/>, the sounds), and the game's own Pokémon change with it: a hit's damage
/// when the hit lands, a level when its line is reached.
/// <para>
/// So the Pokémon the screen and the party hold are behind the core while a turn is being shown, and the same as
/// the core's whenever a menu is open (<see cref="BattleMirror"/>). Split over BattleEngine.*.cs: this file is
/// the playback, .Menus.cs the choices.
/// </para>
/// </summary>
public partial class BattleEngine
{
    public Party PlayerParty { get; }
    public Inventory PlayerInventory { get; }
    public Pokedex Pokedex { get; }
    public BattleFormat Format => core.Format;

    /// <summary>The opposing trainers (none in a wild battle).</summary>
    public IReadOnlyList<Trainer> Trainers { get; }
    public Trainer? OpponentTrainer => Trainers.Count > 0 ? Trainers[0] : null;

    /// <summary>The first trainer's team, for the ball tray on the HUD.</summary>
    public Party? EnemyParty => OpponentTrainer?.Party;

    public bool IsTrainerBattle => Trainers.Count > 0;
    public bool IsDouble => Format == BattleFormat.Double;

    /// <summary>The places on each side as the screen has them: one each in a single battle, two in a double.</summary>
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

    /// <summary>The rules' side of the battle: its own Pokémon, its requests, its log.</summary>
    public BattleCore Core => core;

    public Random Random => core.Random;

    /// <summary>The rules this battle is fought by: those of the game in progress unless the setup gave its own.</summary>
    public Ruleset Rules => core.Rules;

    /// <summary>
    /// The player's Pokémon that gained a level in this battle. They are the ones that may evolve once it is over,
    /// as in the games: nothing changes species while the battle is on.
    /// </summary>
    public IReadOnlyList<Pokemon> LeveledUp => leveledUpPokemon;

    /// <summary>What Pay Day scattered for the player, picked up with a win.</summary>
    public int PayDayMoney => core.PayDayMoney;

    public BattleResult Result { get; private set; } = BattleResult.None;
    public bool IsBattleOver => Result != BattleResult.None && !waitingForMessageConfirm && steps.Count == 0;

    private readonly BattleCore core;
    private readonly BattleMirror mirror = new();
    private readonly List<Pokemon>? pcBoxStorage;
    private readonly List<Pokemon> leveledUpPokemon = new();

    // What is still to be shown: lines that wait to be read, and things that happen between them
    private abstract record Step;
    private sealed record Line(string Text, Action? OnShow) : Step;
    private sealed record Happening(Action Do) : Step;
    private readonly Queue<Step> steps = new();
    private bool playingTheLog;

    private string currentMessage = "";
    private float messageWaitTimer = 0f;
    private bool waitingForMessageConfirm = false;

    // Effects that land a moment after the message that started them, like a hit after the attacker's lunge
    private const float HitDelay = 0.35f;
    private readonly List<(float Delay, Action Effect)> pendingEffects = new();

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

        // The rules get copies of every Pokémon; the game's own are what the screen shows
        var trainers = setup.Trainers.Select(mirror.Copy).ToList();
        core = new BattleCore(new CoreSetup
        {
            PlayerParty = mirror.Copy(setup.PlayerParty),
            WildPokemon = setup.WildPokemon.Select(mirror.Copy).ToList(),
            Trainers = trainers,
            FirstTrainerPokemon = setup.FirstTrainerPokemon == null ? null : mirror.Copy(setup.FirstTrainerPokemon),
            Format = setup.Format,
            Random = setup.Random,
            Rules = setup.Rules,
            Conditions = setup.Conditions ?? new BattleConditions { HasCaught = species => Pokedex.IsCaught(species.DexNumber) },
            PlayerName = PlayerIdentity.Name
        });

        Anim.Slots = core.PlayerSlots.Count;
        PlayerSlots = core.PlayerSlots.Select(OnScreen).ToList();
        EnemySlots = core.EnemySlots.Select(OnScreen).ToList();

        core.Start();
        Play(core.TakeLog());
        Pump();
    }

    /// <summary>The screen's own battler for one of the core's places, holding the game's own Pokémon.</summary>
    private Battler OnScreen(Battler place) => new(place.Side, place.Slot)
    {
        Pokemon = place.Pokemon == null ? null : mirror.Shown(place.Pokemon),
        Roster = place.IsPlayerSide ? PlayerParty : IsTrainerBattle ? Trainers[Math.Min(place.Slot, Trainers.Count - 1)].Party : null,
        Trainer = place.IsPlayerSide || !IsTrainerBattle ? null : Trainers[Math.Min(place.Slot, Trainers.Count - 1)]
    };

    private IReadOnlyList<Battler> SlotsOf(BattleSide side) => side == BattleSide.Player ? PlayerSlots : EnemySlots;

    private Battler At(Place place) => SlotsOf(place.Side)[place.Slot];

    // ---------------------------------------------------------------- the game's Pokémon and the rules' copies

    /// <summary>Tells the rules of anything that changed a Pokémon or a place outside the battle since they last looked.</summary>
    private void SyncToCore()
    {
        mirror.Adopt();
        foreach (var place in AllBattlers) core.At(place.Place).CopyVolatileFrom(place);
    }

    /// <summary>Brings what the screen holds up to the rules, once there is nothing left to show.</summary>
    private void SyncFromCore()
    {
        mirror.Publish();
        foreach (var place in AllBattlers)
        {
            var theirs = core.At(place.Place);
            place.Pokemon = theirs.Pokemon == null ? null : mirror.Shown(theirs.Pokemon);
            place.CopyVolatileFrom(theirs);
            Anim[place.Side, place.Slot].Away = theirs.IsActive && theirs.IsElsewhere;
        }
        Weather = core.Field.Weather;
    }

    /// <summary>
    /// The rules' own battler for one of the screen's. While a menu is open the rules are first told of anything
    /// that changed outside the battle; while a turn is being shown they are ahead of the screen and are left alone.
    /// </summary>
    private Battler InCore(Battler b)
    {
        if (!playingTheLog) SyncToCore();
        return core.At(b.Place);
    }

    /// <summary>Speed as the turn order sees it: after stages, paralysis, abilities, items, the weather and a tailwind.</summary>
    public int EffectiveSpeed(Battler b) => core.EffectiveSpeed(InCore(b));

    /// <summary>The weather over the battle, as far as the screen has been told of it.</summary>
    public BattleWeather Weather { get; private set; }

    /// <summary>
    /// Gives a Pokémon on the field a status condition by the battle's rules, outside any turn (a scripted scene,
    /// a test). What is said about it is shown with the next thing the battle shows.
    /// </summary>
    public bool TryInflictStatus(Battler target, StatusCondition status, Battler? source, bool announceFailure = false) =>
        BetweenTurns(() => core.TryInflictStatus(core.At(target.Place), status, source == null ? null : core.At(source.Place), announceFailure));

    /// <summary>Raises or lowers a stat stage by the battle's rules, outside any turn.</summary>
    public bool ChangeStat(Battler target, StatType stat, int amount, Battler? source, bool announceFailure = false) =>
        BetweenTurns(() => core.ChangeStat(core.At(target.Place), stat, amount, source == null ? null : core.At(source.Place), announceFailure));

    private T BetweenTurns<T>(Func<T> change)
    {
        if (!playingTheLog) SyncToCore();
        T result = change();
        Enqueue(core.TakeLog());
        SyncFromCore();
        return result;
    }

    // ---------------------------------------------------------------- playing the log

    /// <summary>Queues what the rules wrote down, to be shown line by line.</summary>
    private void Play(List<BattleEvent> events)
    {
        Enqueue(events);
        playingTheLog = true;
        HUD.MenuState = BattleMenuState.Message;
    }

    private void Enqueue(List<BattleEvent> events)
    {
        foreach (var happening in events)
        {
            if (happening is Said said)
            {
                steps.Enqueue(new Line(said.Text, () =>
                {
                    foreach (var shown in said.Shows) Show(shown);
                    foreach (var landing in said.OnImpact) After(HitDelay, () => Show(landing));
                }));
            }
            else steps.Enqueue(new Happening(() => Show(happening)));
        }
    }

    /// <summary>A line of the screen's own (a menu's refusal), with what follows once it has been read.</summary>
    private void QueueMessage(string msg, Action? onComplete = null)
    {
        steps.Enqueue(new Line(msg, null));
        if (onComplete != null) steps.Enqueue(new Happening(onComplete));
    }

    /// <summary>Shows what is queued until a line waits to be read or an animation holds the queue.</summary>
    private void Pump()
    {
        while (!waitingForMessageConfirm && messageWaitTimer <= 0f)
        {
            if (steps.Count == 0)
            {
                if (!playingTheLog) return;
                playingTheLog = false;
                LogPlayed();
                continue;
            }

            switch (steps.Dequeue())
            {
                case Line line:
                    currentMessage = line.Text;
                    waitingForMessageConfirm = true;
                    HUD.MenuState = BattleMenuState.Message;
                    line.OnShow?.Invoke();
                    break;
                case Happening happening:
                    happening.Do();
                    break;
            }
        }
    }

    /// <summary>Everything the rules worked out has been shown: the screen's Pokémon are the rules' again, and the next menu opens.</summary>
    private void LogPlayed()
    {
        SyncFromCore();
        switch (core.Request)
        {
            case ActionRequest:
                BeginChoosing();
                break;
            case ReplacementRequest replacement:
                replacing = true;
                menuSlot = replacement.Place.Slot;
                HUD.SwitchMenuIndex = 0;
                HUD.MenuState = BattleMenuState.SwitchPokemon;
                break;
        }
    }

    /// <summary>One thing the log says happened, as the screen shows it.</summary>
    private void Show(BattleEvent happening)
    {
        switch (happening)
        {
            case TrainersStand trainers:
                Anim.EnemyTrainer = trainers.First;
                if (trainers.Second != null) Anim.EnemyTrainer2 = trainers.Second;
                break;
            case Seen seen:
                Pokedex.RegisterSeen(seen.Species.DexNumber);
                break;
            case Entered entered:
            {
                var pokemon = mirror.Shown(entered.Pokemon);
                At(entered.Place).Pokemon = pokemon;
                if (entered.FromBall) Anim.SendOut(entered.Place.Side, pokemon, entered.Place.Slot);
                else Anim.Appear(entered.Place.Side, pokemon, entered.Place.Slot);
                break;
            }
            case Recalled recalled:
                Anim.Recall(recalled.Place.Side, recalled.Place.Slot);
                break;
            case Left:
                AudioManager.PlaySound("select");
                break;
            case Lunged lunged:
                Anim.Attack(lunged.Place.Side, lunged.Place.Slot, lunged.Category);
                break;
            case MoveShown move:
                Anim.Cue(new EffectCue
                {
                    Kind = CueKind.Move,
                    Move = move.Move,
                    Type = move.Type,
                    Category = move.Category,
                    FromSide = move.From.Side,
                    FromSlot = move.From.Slot,
                    ToSide = move.To.Side,
                    ToSlot = move.To.Slot,
                    Missed = move.Missed,
                    Blocked = move.Blocked,
                    Critical = move.Critical,
                    SuperEffective = move.SuperEffective
                });
                break;
            case Struck struck:
                if (At(struck.Place).Pokemon is { } hit) hit.CurrentHP = struck.Hp;
                Anim.Hit(struck.Place.Side, struck.Place.Slot, struck.Hard ? 1.8f : 1f);
                break;
            case HitSounded sound:
                AudioManager.PlaySound(sound.SuperEffective ? "hit_super" : "hit_normal");
                break;
            case HpChanged change:
                if (At(change.Place).Pokemon is { } changed) changed.CurrentHP = change.Hp;
                if (change.Healed)
                {
                    Anim.Heal(change.Place.Side, change.Place.Slot);
                    AudioManager.PlaySound("heal");
                }
                else
                {
                    Anim.Hit(change.Place.Side, change.Place.Slot);
                    AudioManager.PlaySound("hit_normal");
                }
                break;
            case StageChanged stage:
                if (At(stage.Place).Pokemon is { } staged) staged.StatStages[stage.Stat] = stage.Stage;
                Anim.StatChange(stage.Place.Side, stage.Place.Slot, stage.Rose);
                break;
            case StatusChanged status:
                if (At(status.Place).Pokemon is { } afflicted) afflicted.Status = status.Status;
                if (status.Status != StatusCondition.None) Anim.StatusGiven(status.Place.Side, status.Place.Slot, status.Status);
                break;
            case Vanished gone:
                Anim[gone.Place.Side, gone.Place.Slot].Away = true;
                break;
            case Reappeared back:
                Anim[back.Place.Side, back.Place.Slot].Away = false;
                break;
            case Reshaped reshaped:
            {
                // Transform: the screen's Pokémon takes the rules' shape as the line appears, ahead of the next sync
                if (core.At(reshaped.Place).Pokemon is { } theirs && At(reshaped.Place).Pokemon is { } shown)
                {
                    shown.Species = theirs.Species;
                    shown.Form = theirs.Form;
                    shown.Nickname = theirs.Nickname;
                }
                break;
            }
            case WeatherChanged weather:
                Weather = weather.Weather;
                break;
            case Fainted fainted:
                if (At(fainted.Place).Pokemon is { } down)
                {
                    down.Status = StatusCondition.Faint;
                    down.CurrentHP = 0;
                }
                AudioManager.PlaySound("faint");
                Anim.Faint(fainted.Place.Side, 0.15f, fainted.Place.Slot);
                break;
            case ExpGained exp:
                mirror.Shown(exp.Pokemon).GainExp(exp.Amount, out _);
                break;
            case LevelRose level:
            {
                var grown = mirror.Shown(level.Pokemon);
                if (!leveledUpPokemon.Contains(grown)) leveledUpPokemon.Add(grown);
                AudioManager.PlayFanfare(MusicRole.FanfareLevelUp);
                break;
            }
            case BallThrown ball:
                // The trainer throws, the ball opens over the foe, which vanishes into it; what comes next waits for
                // the ball to settle. A foe that breaks free bursts out with the ball and is back on its platform
                // before the line about it.
                AudioManager.PlaySound("ball_throw");
                Anim.ThrowBall(ball.Ball, ball.Slot, ball.Shakes);
                if (ball.Shakes >= 4) messageWaitTimer = BattleAnimator.BallThrowTime(ball.Shakes) + 0.1f;
                else
                {
                    After(BattleAnimator.BallSettleTime(ball.Shakes), () => Anim.BreakFree(ball.Slot));
                    messageWaitTimer = BattleAnimator.BallSettleTime(ball.Shakes) + BattleAnimator.SendOutTime + 0.1f;
                }
                break;
            case Caught caught:
            {
                var mine = mirror.Shown(caught.Pokemon);
                AudioManager.PlayMusic(MusicRole.VictoryWild);
                Pokedex.RegisterCaught(mine.Species.DexNumber);
                mine.ResetStatStages();
                mine.Ball = caught.Ball;
                if (caught.ToBox) pcBoxStorage?.Add(mine);
                else PlayerParty.Add(mine);
                break;
            }
            case Won:
                AudioManager.PlayMusic(MusicDirector.VictoryRole(MusicDirector.BattleRole(Trainers.Select(t => t.TrainerClass))));
                break;
            case Ended ended:
                Result = ended.Result;
                break;
        }
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

        // A timed animation (the Poké Ball throw) holds the queue until it ends
        if (messageWaitTimer > 0f)
        {
            messageWaitTimer -= dt;
            if (messageWaitTimer <= 0f) Pump();
            return;
        }

        if (HUD.MenuState == BattleMenuState.Message)
        {
            Pump();
            return;
        }

        HandleMenuInput();
    }

    /// <summary>The message currently on screen.</summary>
    public string CurrentMessage => currentMessage;

    /// <summary>A message is on screen waiting to be dismissed.</summary>
    public bool IsWaitingForConfirm => waitingForMessageConfirm;

    /// <summary>Dismisses the message on screen and goes on to what follows it.</summary>
    public void ConfirmMessage()
    {
        if (!waitingForMessageConfirm) return;

        // Anything the message started lands now, so what is shown never runs ahead of it
        FlushPendingEffects();

        waitingForMessageConfirm = false;
        Pump();
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
