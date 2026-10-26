using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.Battle;

public enum BattleResult
{
    None,
    PlayerVictory,
    PlayerDefeat,
    EnemyCaught,
    PlayerRan,
    /// <summary>The wild Pokémon ran away (a roamer, one of the Great Marsh's).</summary>
    EnemyFled
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

    /// <summary>
    /// Whether a foe's HP box shows the caught mark: in a battle with no trainer, for a species the Pokédex has as
    /// caught, as the original's health box draws its Poké Ball (<c>HEALTHBOX_INFO_CAUGHT_SPECIES</c>, never in a
    /// trainer's battle; the Great Marsh and Pal Park show it too).
    /// </summary>
    public bool ShowsCaughtMark(Pokemon foe) => !IsTrainerBattle && Pokedex.IsCaught(foe.Species.DexNumber);
    public bool IsDouble => Format == BattleFormat.Double;

    /// <summary>A horde (plan 14 · B4): one of the player's against up to five wild Pokémon, on the screen too.</summary>
    public bool IsHorde => Format == BattleFormat.Horde;

    /// <summary>An SOS battle (plan 14 · B5): one wild Pokémon, and an empty place beside it that an ally it calls may fill.</summary>
    public bool IsSos => Format == BattleFormat.Sos;

    /// <summary>A triple battle (plan 14 · B6): three places a side, reach by position.</summary>
    public bool IsTriple => Format == BattleFormat.Triple;

    /// <summary>A rotation battle (plan 14 · B7): one place a side, the front of a turntable of three.</summary>
    public bool IsRotation => Format == BattleFormat.Rotation;

    /// <summary>
    /// The Pokémon whose action is being chosen stands at an end of a triple battle's side, so its fourth command is
    /// SHIFT, to the centre, in place of RUN (plan 14 · B6; style guide, "Battle panels", "SHIFT").
    /// </summary>
    public bool OffersShift => IsTriple && Kind == BattleKind.Normal && MenuBattler.Slot != BattleCore.CentreSlot && MenuBattler.Trainer == null;

    /// <summary>The calls answered in an SOS battle so far, as the lines shown have told (plan 14 · B5).</summary>
    public int SosChain { get; private set; }

    // The places an ally that answered a call is about to come onto: its entry hops in rather than appearing
    private readonly HashSet<Place> arriving = new();

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

    /// <summary>
    /// The battle as its rules began it, before the first line (plan 08 · P10): both sides, the trainers, the format,
    /// the rules and the conditions, taken from the copies the rules were handed. <see cref="RecordFile"/> adds what it
    /// came to.
    /// </summary>
    public BattleRecordFile Opening { get; }

    /// <summary>
    /// The battle as the Vs. Recorder keeps it, on the day given (plan 08 · P10): the opening, the record of its seed
    /// and answers, and how it ended. Null for a battle whose chance was no <see cref="BattleRandom"/>.
    /// </summary>
    public BattleRecordFile? RecordFile(DateTime day) => ChangedFromOutside ? null : Opening.Closing(core, day);

    /// <summary>
    /// Whether something outside the battle changed one of its Pokémon after it began (a tool keeping a foe at one HP, a
    /// test setting a burn): the rules took the change over (<see cref="BattleMirror.Adopt()"/>), but no answer of the
    /// record holds it, so the battle would replay otherwise and the Vs. Recorder keeps none of it (<see cref="RecordFile"/>).
    /// </summary>
    public bool ChangedFromOutside { get; private set; }

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

    /// <summary>What a trainer battle won pays: the trainers' prize money, doubled by an Amulet Coin or a Luck Incense on the field (plan 06 · R8).</summary>
    public int PrizeMoney => core.PrizeMoney;

    /// <summary>What losing cost the player, by the team's highest level and the badges (plan 06 · R10).</summary>
    public int MoneyLost => core.MoneyLost;

    public BattleResult Result { get; private set; } = BattleResult.None;
    public bool IsBattleOver => Result != BattleResult.None && !waitingForMessageConfirm && steps.Count == 0 && interlude.Count == 0 && learning == null;

    /// <summary>
    /// True while a Pokémon of the player's on the field is in the red as its bar shows it
    /// (<see cref="BattleAnimator.LowHpRatio"/>): when the battle theme turns agitated, as Black and White's does.
    /// </summary>
    public bool PlayerInDanger
    {
        get
        {
            for (int slot = 0; slot < Anim.Slots; slot++)
            {
                var view = Anim[BattleSide.Player, slot];
                var shown = view.Shown;
                if (shown == null || !view.Present || shown.MaxHP <= 0) continue;
                if (view.DisplayedHp > 0f && view.DisplayedHp <= shown.MaxHP * BattleAnimator.LowHpRatio) return true;
            }
            return false;
        }
    }

    private readonly BattleCore core;
    private readonly BattleMirror mirror = new();
    /// <summary>The Pokémon the player caught, and whether its species had never been caught before (the card's score).</summary>
    public Pokemon? Caught { get; private set; }
    public bool CaughtNewSpecies { get; private set; }

    private readonly PcBoxes? pcBoxStorage;
    private readonly string? place;
    private readonly List<Pokemon> leveledUpPokemon = new();

    // What is still to be shown: lines that wait to be read, and things that happen between them
    private abstract record Step;
    /// <param name="Speaker">Who says the line, named on a pill over it (a trainer's line in battle, plan 11 · C12); null for the battle's own.</param>
    private sealed record Line(string Text, Action? OnShow, string? Speaker = null) : Step;
    private sealed record Happening(Action Do) : Step;
    private readonly Queue<Step> steps = new();
    private bool playingTheLog;

    private string currentMessage = "";
    private float messageWaitTimer = 0f;
    private bool waitingForMessageConfirm = false;

    // Effects that land a moment after the message that started them, like a hit after the attacker's lunge
    private const float HitDelay = 0.35f;

    /// <summary>
    /// When what a line carries lands: <see cref="HitDelay"/> after it with the scene on, and at once with it off
    /// (plan 08 · P1), through the same pending effects, so the order the log gives is kept either way.
    /// </summary>
    private float ImpactDelay => Scene ? HitDelay : 0f;

    /// <summary>
    /// The options' Battle Scene (plan 08 · P1; style guide, "Move effects (G8)", "With the scene off"): on, moves and
    /// the battle's animations play. Off, as Platinum's own switch has it (<c>BattleSystem_AreAnimationsOn</c>), no
    /// lunge and no move's effect play (but Transform's), nor a stat's change, a heal's aura, a condition given, the
    /// turn's moments or the weather's cue, with their sounds; the move's own sound, a hit's flicker and sound, the
    /// send-outs, the ball, a faint, the doll, the weather's layer and what lasts on the field and on a Pokémon stay,
    /// and a line's hits land at once. Only the face reads it: the rules and the log are the same either way.
    /// </summary>
    public bool Scene { get; set; } = true;
    private readonly List<(float Delay, Action Effect)> pendingEffects = new();

    public BattleEngine(
        Party playerParty,
        Pokemon wildOrEnemyPokemon,
        Inventory playerInventory,
        Pokedex pokedex,
        Trainer? trainer = null,
        PcBoxes? pcStorage = null)
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

    public BattleEngine(BattleSetup setup) : this(setup, null)
    {
    }

    /// <summary>
    /// A battle on screen: fought, or (with <paramref name="watched"/>, plan 08 · P11) a recorded battle watched, whose
    /// answers are the record's and whose Pokémon, bag and Pokédex are the recording's own (<see cref="ForReplay"/>).
    /// </summary>
    private BattleEngine(BattleSetup setup, BattleRecordFile? watched)
    {
        Watched = watched;
        PlayerParty = setup.PlayerParty;
        PlayerInventory = setup.Inventory;
        Pokedex = setup.Pokedex;
        pcBoxStorage = setup.PcStorage;
        place = setup.Place;
        Trainers = setup.Trainers;

        Kind = setup.Kind;
        Partner = setup.Partner;
        if (setup.PlayerLook != null) Anim.PlayerTrainer = setup.PlayerLook;
        // The ground fought on, for the looks that go by it (plan 25 · Y6: Secret Power, Camouflage); never the rules'
        terrain = setup.Conditions?.Terrain ?? BattleTerrain.Plain;
        // Whoever battles beside the player stands on the platform beside them, in their class's look or their own
        if (Partner != null)
            Anim.PartnerTrainer = CharacterStyles.Default.InBattle(Partner.TrainerClass, Partner.Look is { } theirs ? PlayerIdentity.CharacterFor(theirs) : null);

        // The rules get copies of every Pokémon; the game's own are what the screen shows
        var trainers = setup.Trainers.Select(mirror.Copy).ToList();
        var rules = new CoreSetup
        {
            PlayerParty = mirror.Copy(setup.PlayerParty),
            WildPokemon = setup.WildPokemon.Select(mirror.Copy).ToList(),
            Trainers = trainers,
            FirstTrainerPokemon = setup.FirstTrainerPokemon == null ? null : mirror.Copy(setup.FirstTrainerPokemon),
            Format = setup.Format,
            Random = setup.Random,
            Rules = setup.Rules,
            Conditions = setup.Conditions ?? new BattleConditions { HasCaught = species => Pokedex.IsCaught(species.DexNumber) },
            PlayerName = setup.PlayerName ?? PlayerIdentity.Name,
            Kind = setup.Kind,
            Partner = setup.Partner == null ? null : mirror.Copy(setup.Partner),
            CannotFlee = setup.CannotFlee,
            FirstBattle = setup.FirstBattle,
            SpecialBalls = setup.SpecialBalls,
            Frontier = setup.Frontier,
            NoBattleItems = setup.NoBattleItems,
            OffersSwitch = setup.OffersSwitch,
            MegaMaxDen = setup.MegaMaxDen,
            Difficulty = setup.Difficulty,
            RunLevelCap = setup.RunLevelCap,
            CrossedExp = setup.CrossedExp,
            Nuzlocke = setup.Nuzlocke,
            Restrictions = setup.Restrictions,
            // The Battle Palace and the Battle Arena (plan 14 · B9); a battle watched has what the recorded one had
            PlayerController = setup.PlayerController,
            Palace = watched?.Palace ?? setup.Palace,
            TurnLimit = watched?.TurnLimit ?? setup.TurnLimit,
            // A Key Stone in the bag lets one of the player's Pokémon Mega Evolve (plan 06 · R20); a battle watched
            // has what the recorded one had (plan 08 · P11)
            KeyStone = watched?.KeyStone ?? setup.Inventory.AllItems.Any(s => s.Name == BattleCore.KeyStoneItem),
            // And a Z-Ring lets one use a Z-Move and Ultra Burst (plan 06 · R21)
            ZRing = watched?.ZRing ?? setup.Inventory.AllItems.Any(s => s.Name == BattleCore.ZRingItem),
            // And a Dynamax Band lets one Dynamax (plan 06 · R22)
            DynamaxBand = watched?.DynamaxBand ?? setup.Inventory.AllItems.Any(s => s.Name == BattleCore.DynamaxBandItem),
            // And a Mega Max Band lets a Mega Dynamax (plan 06 · R20c)
            MegaMaxBand = watched?.MegaMaxBand ?? setup.Inventory.AllItems.Any(s => s.Name == BattleCore.MegaMaxBandItem),
            // And a Tera Orb lets one Terastallize (plan 06 · R23)
            TeraOrb = watched?.TeraOrb ?? setup.Inventory.AllItems.Any(s => s.Name == BattleCore.TeraOrbItem)
        };
        // The battle as its rules begin it, before they have changed anything: what the Vs. Recorder keeps (plan 08 · P10)
        Opening = BattleRecordFile.Opening(rules);
        core = new BattleCore(rules);

        // A horde's wild side has its own count of places (plan 14 · B4); a single's and a double's are the same both sides
        Anim.SetSlots(core.PlayerSlots.Count, core.EnemySlots.Count);
        // An SOS battle's wild Pokémon stands alone in the middle until an ally comes (plan 14 · B5)
        if (IsSos) Anim.Gather(BattleSide.Enemy);
        // A rotation battle's platforms are turntables (plan 14 · B7)
        Anim.Turntables = IsRotation;
        PlayerSlots = core.PlayerSlots.Select(OnScreen).ToList();
        EnemySlots = core.EnemySlots.Select(OnScreen).ToList();

        // The species the Pokédex knew before the battle: its Seen events register the foes as they appear, and a
        // species met for the first time gets no hint (plan 12 · Q10, as Sun and Moon have it)
        ShowsMoveHints = setup.MoveHints;
        SpeaksTrainerLines = setup.TrainerLines;
        Scene = setup.Scene;
        seenBefore = PlayerParty.Members.Concat(setup.WildPokemon).Concat(setup.Trainers.SelectMany(t => t.Party.Members))
            .Concat(setup.Partner?.Party.Members ?? Enumerable.Empty<Pokemon>())
            .Select(p => p.Species.DexNumber).Where(Pokedex.IsSeen).ToHashSet();

        // Every cry the battle may play, made on a worker before it is heard
        AudioManager.RequestCries(PlayerParty.Members.Concat(setup.WildPokemon).Concat(setup.Trainers.SelectMany(t => t.Party.Members)));

        core.Start();
        Play(core.TakeLog());
        Pump();
    }

    /// <summary>The screen's own battler for one of the core's places, holding the game's own Pokémon.</summary>
    private Battler OnScreen(Battler place) => new(place.Side, place.Slot)
    {
        Pokemon = place.Pokemon == null ? null : mirror.Shown(place.Pokemon),
        Roster = place.IsPlayerSide ? (place.Slot == 1 && Partner != null && core.Partner != null ? Partner.Party : PlayerParty)
            : IsTrainerBattle ? Trainers[Math.Min(place.Slot, Trainers.Count - 1)].Party : null,
        Trainer = place.IsPlayerSide ? (place.Slot == 1 && core.Partner != null ? Partner : null)
            : !IsTrainerBattle ? null : Trainers[Math.Min(place.Slot, Trainers.Count - 1)]
    };

    /// <summary>What kind of battle it is (a roamer, the catching lesson, the Great Marsh, Pal Park).</summary>
    public BattleKind Kind { get; }

    /// <summary>A battle of the Battle Frontier (plan 06 · R18a): no bag, no EXP, no money, and RUN asks whether to give the match up.</summary>
    public bool IsFrontier => core.Frontier;

    /// <summary>Nothing may be used from the bag (plan 14 · B14, a Master Trainer's battle): BAG is greyed and says why.</summary>
    public bool BagClosed => core.NoBattleItems || core.BagShutByTheRun;

    /// <summary>A battle of the Battle Palace (plan 14 · B9): the player's Pokémon choose for themselves and the commands stand closed.</summary>
    public bool Palace => core.Palace;

    /// <summary>A battle of the Battle Arena (plan 14 · B9): its bouts are judged after <see cref="BattleCore.TurnLimit"/> turns.</summary>
    public bool Arena => core.TurnLimit > 0;

    /// <summary>The judge's card on show (plan 14 · B9): the verdict the line being read gives, until the next line.</summary>
    public ArenaVerdict? JudgeCard { get; private set; }

    /// <summary>
    /// The one ball this battle throws in place of the bag (plan 15 · E5: the Bug-Catching Contest's Sport Ball, counted
    /// by <see cref="SpecialBalls"/>); null in every other battle.
    /// </summary>
    public string? OnlyBall => core.Conditions.OnlyBall;

    /// <summary>The trainer beside the player in a tag battle (their Pokémon stand in the player's second place).</summary>
    public Trainer? Partner { get; }

    /// <summary>The Safari Balls or Park Balls left.</summary>
    public int SpecialBalls => core.SpecialBalls;

    /// <summary>The places the player chooses for: the player's own (not a partner's, and none in the catching lesson).</summary>
    public IEnumerable<Battler> PlayerChooses => Kind == BattleKind.CatchingLesson ? Enumerable.Empty<Battler>() : PlayerSlots.Where(b => b.Trainer == null);

    private IReadOnlyList<Battler> SlotsOf(BattleSide side) => side == BattleSide.Player ? PlayerSlots : EnemySlots;

    private Battler At(Place place) => SlotsOf(place.Side)[place.Slot];

    // ---------------------------------------------------------------- the game's Pokémon and the rules' copies

    /// <summary>Tells the rules of anything that changed a Pokémon or a place outside the battle since they last looked.</summary>
    private void SyncToCore()
    {
        if (mirror.Adopt()) ChangedFromOutside = true;
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
            Anim.SettleSubstitute(place.Side, place.Slot, theirs.IsActive && theirs.HasSubstitute);
            // A charge that ended with no move let go (put to sleep, flinched) has nothing more to gather (plan 25 · Y5)
            Anim.SettleCharge(place.Side, place.Slot, theirs.IsActive && theirs.Volatile.Charging);
            // A Dynamax and the turns it has left, for the HP box (plan 25 · Y15)
            Anim.SettleDynamax(place.Side, place.Slot, theirs.IsActive && theirs.Volatile.Dynamaxed, 3 - theirs.Volatile.DynamaxTurns, theirs.Volatile.Gigantamaxed);
            // What lasts on it (plan 09 · L2): the lines have told all of it already; this catches a Pokémon changed from outside
            Anim.Conditions.Settle(place.Place, ConditionLooks.RulesHave(theirs));
            Anim.Conditions.SettleMarks(place.Place, ConditionLooks.MarksRulesHave(theirs));
        }
        Weather = core.Field.Weather;
        Anim.SettleWeather(Weather);
        // What lies on the field (plan 25 · Y2): the lines have told all of it already; this catches a field changed from outside
        Anim.FieldLook.Settle(core.Field);
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
        // The foe's Pokémon cheer as the player loses (plan 25 · Y5): on the first line after the player's last one
        // went down. The log has told it already: the loss ends the batch its last fall is in
        int cheerBefore = -1;
        if (events.Any(e => e is Ended { Result: BattleResult.PlayerDefeat }))
        {
            int lastFall = events.FindLastIndex(PlayersFall);
            if (lastFall >= 0) cheerBefore = events.FindIndex(lastFall + 1, e => e is Said);
        }
        for (int i = 0; i < events.Count; i++)
        {
            var happening = events[i];
            if (i == cheerBefore)
                steps.Enqueue(new Happening(() =>
                {
                    if (Scene) Anim.Cheer(BattleSide.Enemy);
                }));
            if (happening is Said said)
            {
                steps.Enqueue(new Line(said.Text, () =>
                {
                    // What a line shows is shown knowing its line: a moment on it takes its HP with it (plan 25 · Y3)
                    showing = said;
                    foreach (var shown in said.Shows) Show(shown);
                    showing = null;
                    foreach (var landing in said.OnImpact) After(ImpactDelay, () => Show(landing));
                }));
            }
            else steps.Enqueue(new Happening(() => Show(happening)));
        }
    }

    /// <summary>One of the player's Pokémon going down, by itself or on the line that tells of it.</summary>
    private static bool PlayersFall(BattleEvent e) => e switch
    {
        Fainted fainted => fainted.Place.Side == BattleSide.Player,
        Said said => said.Shows.Concat(said.OnImpact).Any(PlayersFall),
        _ => false
    };

    /// <summary>A line of the screen's own (a menu's refusal), with what follows once it has been read.</summary>
    private void QueueMessage(string msg, Action? onComplete = null)
    {
        steps.Enqueue(new Line(msg, null));
        if (onComplete != null) steps.Enqueue(new Happening(onComplete));
    }

    /// <summary>Shows what is queued until a line waits to be read or an animation holds the queue.</summary>
    private void Pump()
    {
        // A question about a move to learn holds everything until it is answered; its own lines go first
        while (!waitingForMessageConfirm && messageWaitTimer <= 0f && HUD.MenuState != BattleMenuState.LearnMove)
        {
            if (steps.Count == 0 && interlude.Count == 0)
            {
                if (!playingTheLog) return;
                playingTheLog = false;
                LogPlayed();
                continue;
            }

            switch (interlude.Count > 0 ? interlude.Dequeue() : steps.Dequeue())
            {
                case Line line:
                    // The judge's card stands over its own line alone (plan 14 · B9)
                    JudgeCard = null;
                    currentMessage = line.Text;
                    CurrentSpeaker = line.Speaker;
                    waitingForMessageConfirm = true;
                    HUD.MenuState = BattleMenuState.Message;
                    line.OnShow?.Invoke();
                    // Battle Style Shift's line is its question: Yes and No come up beside it as it is written, with
                    // no press between (plan 08 · P1; the original's PrintMessage, Wait, YesNoMenu)
                    // (A battle watched shows the line, and the record answers it once it has been read, plan 08 · P11)
                    if (steps.Count == 0 && interlude.Count == 0 && core.Request is SwitchOfferRequest && playingTheLog && !Watching)
                        waitingForMessageConfirm = false;
                    break;
                case Happening happening:
                    happening.Do();
                    // A level's panel stands over the line that tells of the level, which is what the log says next;
                    // were anything else to come first, the panel waits for its presses over the line already there
                    if (HUD.LevelUp != null && !(interlude.Count > 0 ? interlude.Peek() is Line : steps.Count > 0 && steps.Peek() is Line))
                    {
                        waitingForMessageConfirm = true;
                        HUD.MenuState = BattleMenuState.Message;
                    }
                    break;
            }
        }
    }

    /// <summary>Everything the rules worked out has been shown: the screen's Pokémon are the rules' again, and the next menu opens.</summary>
    private void LogPlayed()
    {
        SyncFromCore();
        // A battle watched opens no menu: the record answers what the rules ask (plan 08 · P11)
        if (Watching)
        {
            FeedTheRecord();
            return;
        }
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
            case SwitchOfferRequest offer:
                OfferSwitch(offer);
                break;
        }
    }

    /// <summary>
    /// The look a trainer stands on the platform in (plan 11 · C1): their class's, as the original shows the class,
    /// or the look of the person who carries them where the class names nobody in particular (a Leader).
    /// </summary>
    private string LookOf(string trainerClass, int index)
    {
        string? look = index < Trainers.Count && Trainers[index].Look is { } own ? PlayerIdentity.CharacterFor(own) : null;
        return CharacterStyles.Default.InBattle(trainerClass, look);
    }

    /// <summary>One thing the log says happened, as the screen shows it.</summary>
    private void Show(BattleEvent happening)
    {
        // What lasts on a Pokémon (plan 09 · L2): a condition given or cured, a confusion or a love begun or ended,
        // what a newcomer brings, a faint, a leaving. The line sets the look, which is drawn for as long as it lasts
        Anim.Conditions.Tell(happening);
        switch (happening)
        {
            case TrainersStand trainers:
                Anim.EnemyTrainer = LookOf(trainers.First, 0);
                if (trainers.Second != null) Anim.EnemyTrainer2 = LookOf(trainers.Second, 1);
                break;
            case Seen seen:
                // The Pokédex isn't told of what is met in the catching lesson or the Battle Frontier (BattleSystem_DexFlagSeen)
                if (Kind != BattleKind.CatchingLesson && !IsFrontier)
                {
                    // The first Spinda met gives its spots (the log's event carries the look and not the personality, which only that species needs)
                    uint? spots = Pokedex.KeepsPersonality(seen.Species) ? EnemySlots.Select(b => b.Pokemon).FirstOrDefault(p => p != null && p.Species == seen.Species)?.Personality : null;
                    Pokedex.RegisterSeen(seen.Species, seen.Form, seen.Gender, spots);
                }
                break;
            case Entered entered:
            {
                var pokemon = mirror.Shown(entered.Pokemon);
                // Its condition is what the log says it brings, and none where the log says nothing: a cure on the
                // bench has no event (Heal Bell, Natural Cure on the way back, medicine out of the bag), so the
                // screen's copy may still have what is gone, and its HP box's pill and its cry go by that copy. The
                // event's own Pokémon is the rules', a turn ahead, and is not asked. One that stands beside a wild
                // Pokémon's entry comes next, by itself
                pokemon.Status = showing?.Shows.OfType<CameInWith>().FirstOrDefault(brings => brings.Place == entered.Place)?.Status ?? StatusCondition.None;
                At(entered.Place).Pokemon = pokemon;
                if (entered.FromBall)
                {
                    Anim.SendOut(entered.Place.Side, pokemon, entered.Place.Slot);
                    // The Seals of its Ball Capsule burst as the ball opens (plan 06 · R17b): the screen's alone, the rules know nothing of it
                    if (CapsuleRules.HasCapsule(pokemon) && pokemon.Capsule is { IsEmpty: false } capsule)
                        Anim.SealBurst(entered.Place.Side, entered.Place.Slot, capsule);
                    After(BattleAnimator.SendOutBallTime, () => Sound("send_out", entered.Place.Side));
                    // It cries as it comes out of the ball: lower if it comes out hurt or ailing, as in the original
                    var mode = Cries.SendOutMode(pokemon);
                    After(BattleAnimator.SendOutBallTime + CryDelay, () => AudioManager.PlayCry(pokemon, mode, Pan(entered.Place.Side)));
                }
                else if (arriving.Remove(entered.Place))
                {
                    // An ally that answered a call hops onto its place, crying as it lands (plan 14 · B5)
                    Anim.Arrive(entered.Place.Side, pokemon, entered.Place.Slot, Scene);
                    After(Scene ? BattleAnimator.ArriveTime * 0.6f : 0f, () => AudioManager.PlayCry(pokemon, CryMode.Normal, Pan(entered.Place.Side)));
                }
                else
                {
                    Anim.Appear(entered.Place.Side, pokemon, entered.Place.Slot);
                    // A horde's five are there together, and cry one after another (plan 14 · B4)
                    if (IsHorde && entered.Place.Slot > 0)
                        After(entered.Place.Slot * HordeCryStep, () => AudioManager.PlayCry(pokemon, CryMode.Normal, Pan(entered.Place.Side)));
                    else AudioManager.PlayCry(pokemon, CryMode.Normal, Pan(entered.Place.Side));
                }
                // A shiny one sparkles as it comes out (plan 09 · L1): the screen's alone, as the Seals are; with the
                // scene off, as the original's other animations that are no moves, it doesn't
                if (pokemon.IsShiny && !pokemon.IsEgg && Scene && Anim.ShinySparkle(entered.Place.Side, entered.Place.Slot, entered.FromBall) is { } sparkle)
                    After(sparkle.Lead, () => Sound("shiny", entered.Place.Side));
                break;
            }
            case CalledForHelp called:
                // The caller cries out and hops where it stands (plan 14 · B5); with the scene off it only cries
                if (At(called.Caller).Pokemon is { } caller)
                {
                    if (Scene) Anim.Attack(called.Caller.Side, called.Caller.Slot, MoveCategory.Status);
                    AudioManager.PlayCry(caller, CryMode.Normal, Pan(called.Caller.Side));
                }
                break;
            case AllyJoined joined:
                // The rules made the ally: the screen takes it in as it came, and its place is marked for the hop in
                mirror.Adopt(joined.Pokemon, joined.AsItCame);
                AudioManager.RequestCries(new[] { joined.AsItCame });
                arriving.Add(joined.Place);
                SosChain = joined.Chain;
                break;
            case CameInWith brought:
                // Nothing is played for it: the screen's Pokémon has what the newcomer brings, from its line on
                if (At(brought.Place).Pokemon is { } newcomer) newcomer.Status = brought.Status;
                break;
            case Recalled recalled:
            {
                // A doll in front of it fades as it goes (plan 25 · Y1)
                bool doll = Anim[recalled.Place.Side, recalled.Place.Slot].BehindDoll;
                Anim.Recall(recalled.Place.Side, recalled.Place.Slot);
                Sound("recall", recalled.Place.Side);
                if (doll) Sound("substitute_out", recalled.Place.Side);
                break;
            }
            case Lunged lunged:
                // With the scene off nobody lunges, and the doll stays where it is (plan 08 · P1)
                if (!Scene) break;
                Anim.Attack(lunged.Place.Side, lunged.Place.Slot, lunged.Category);
                // A doll steps aside for its owner's own move, and back once it has played (plan 25 · Y1)
                Anim.SubstituteAside(lunged.Place.Side, lunged.Place.Slot);
                break;
            case MoveShown move:
                // The move's own sound as it sets off, once however many it is aimed at
                if (Anim.Time != moveSoundAt)
                {
                    moveSoundAt = Anim.Time;
                    MoveHeard(move.Move, move.Type, move.From);
                }
                // With the scene off no move's effect plays but Transform's, as the original plays it all the same
                // (BtlCmd_PlayMoveAnimation; plan 08 · P1); its sound above still does
                if (Scene || move.Move == "Transform")
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
                    SuperEffective = move.SuperEffective,
                    Terrain = terrain,
                    // A Z-Move plays its own set piece, a little longer than a move's (plan 25 · Y14)
                    ZMove = MoveDatabase.Get(move.Move)?.Kind == MoveKind.ZMove,
                    // And a Max Move or a G-Max Move its own (plan 25 · Y15)
                    MaxMove = MoveDatabase.Get(move.Move)?.Kind is MoveKind.MaxMove or MoveKind.GMaxMove
                });
                // A blow that lands on a doll makes it flinch as it lands: the rules say so with no Struck, since the
                // Pokémon behind it takes nothing (subscript_hit_substitute's flicker)
                if (move.Category != MoveCategory.Status && !move.Missed && !move.Blocked && move.To != move.From && Anim[move.To.Side, move.To.Slot].BehindDoll)
                    After(ImpactDelay, () => Anim.SubstituteHit(move.To.Side, move.To.Slot));
                // A miss is dodged, furthest aside as it passes (plan 25 · Y5): our own, so none with the scene off
                if (Scene && move.Missed && move.To != move.From)
                    Anim.Dodge(move.To.Side, move.To.Slot, move.From.Side, move.From.Slot);
                break;
            case Charging charging:
            {
                // A charge turn (plan 25 · Y5): the body gathers itself and its type's aura round it until the move is
                // let go, in place of the hop its line began. Nothing for one that goes up or under on the same line,
                // nor with the scene off, as the lunge it stands in for
                if (!Scene || showing?.Shows.Any(e => e is Vanished gone && gone.Place == charging.Place) == true) break;
                var data = MoveDatabase.Get(charging.Move);
                Anim.Charge(charging.Place.Side, charging.Place.Slot, charging.Move, data?.Type ?? PokemonType.Normal, data?.Category ?? MoveCategory.Physical);
                break;
            }
            case Struck struck:
                if (At(struck.Place).Pokemon is { } hit) hit.CurrentHP = struck.Hp;
                Anim.Hit(struck.Place.Side, struck.Place.Slot, struck.Hard ? 1.8f : 1f);
                break;
            case HitSounded sound:
                AudioManager.PlaySound(sound.SuperEffective ? "hit_super" : sound.NotVeryEffective ? "hit_weak" : "hit_normal");
                break;
            case HpChanged change when OnAMomentsLine:
                // The moment comes first and its HP lands with it: the bar then drains or fills with no flinch, no
                // shake, no sound of a hit and no aura of a heal (SYSCTL_SKIP_SPRITE_BLINK; plan 25 · Y3)
                After(ImpactDelay, () =>
                {
                    if (At(change.Place).Pokemon is { } quietly) quietly.CurrentHP = change.Hp;
                });
                break;
            case HpChanged change:
                if (At(change.Place).Pokemon is { } changed) changed.CurrentHP = change.Hp;
                if (change.Healed)
                {
                    // A heal's aura is one of the battle's animations: none with the scene off, nor its sound
                    if (!Scene) break;
                    Anim.Heal(change.Place.Side, change.Place.Slot);
                    Sound("heal", change.Place.Side);
                }
                else
                {
                    Anim.Hit(change.Place.Side, change.Place.Slot);
                    Sound("hit_normal", change.Place.Side);
                }
                break;
            case StageChanged stage:
                if (At(stage.Place).Pokemon is { } staged) staged.StatStages[stage.Stat] = stage.Stage;
                if (!Scene) break;
                Anim.StatChange(stage.Place.Side, stage.Place.Slot, stage.Rose);
                Sound(stage.Rose ? "stat_up" : "stat_down", stage.Place.Side);
                break;
            case StatusChanged status:
                if (At(status.Place).Pokemon is { } afflicted) afflicted.Status = status.Status;
                if (!Scene) break;
                if (status.Status != StatusCondition.None) Anim.StatusGiven(status.Place.Side, status.Place.Slot, status.Status);
                if (SoundBank.StatusSound(status.Status) is { } ailment) Sound(ailment, status.Place.Side);
                break;
            case Confused confused:
                // As it tries to move, the line's own moment shows it; becoming confused plays the same mark here,
                // as subscript_confuse does (plan 25 · Y3)
                if (showing?.Shows.Any(e => e is MomentShown { Moment: BattleMoment.Confused } own && own.Place == confused.Place) == true) break;
                PlayMoment(new MomentShown(confused.Place, BattleMoment.Confused));
                break;
            case MomentShown moment:
                PlayMoment(moment);
                break;
            case AheadLanded landed:
                // An attack sent ahead lands with its own move's effect and sound, heard where it lands: nobody need
                // stand where it was sent from (plan 25 · Y3). With the scene off, the sound alone (plan 08 · P1)
                Sound(SoundBank.MoveSound(landed.Move, landed.Type), landed.To.Side);
                if (Scene)
                Anim.Cue(new EffectCue
                {
                    Kind = CueKind.Move,
                    Move = landed.Move,
                    Type = landed.Type,
                    Category = landed.Category,
                    FromSide = landed.From.Side,
                    FromSlot = landed.From.Slot,
                    ToSide = landed.To.Side,
                    ToSlot = landed.To.Slot,
                    Arriving = true
                });
                if (Anim[landed.To.Side, landed.To.Slot].BehindDoll)
                    After(ImpactDelay, () => Anim.SubstituteHit(landed.To.Side, landed.To.Slot));
                break;
            case Vanished gone:
            {
                // It is drawn leaving the way its move goes (plan 25 · Y16): the line's own charge names the move; a
                // Pokémon taken up by another's (Sky Drop) goes the way that one does
                string way = showing?.Shows.OfType<Charging>().FirstOrDefault(c => c.Place == gone.Place)?.Move
                    ?? showing?.Shows.OfType<Charging>().FirstOrDefault()?.Move ?? "";
                if (Scene) Anim.GoAway(gone.Place.Side, gone.Place.Slot, way);
                else Anim[gone.Place.Side, gone.Place.Slot].Away = true;
                break;
            }
            case Reappeared back:
                if (Scene) Anim.ComeBack(back.Place.Side, back.Place.Slot);
                else Anim[back.Place.Side, back.Place.Slot].Away = false;
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
            case FormHpChanged raised:
                // A form that raised its most HP (plan 06 · R27b: Power Construct): the box takes the numbers at once
                TakeRaisedHp(raised.Place, At(raised.Place).Pokemon, raised.MaxHp, raised.Hp);
                break;
            case Dynamaxed or DynamaxEnded:
                // A Dynamax and its end (plan 06 · R22)
                ShowDynamax(happening);
                break;
            case Terastallized:
                // A Terastallization (plan 06 · R23)
                ShowTera(happening);
                break;
            case UltraBursted burst:
                // An Ultra Burst (plan 06 · R21): its own look and sound, the form taken inside its light (plan 25 · Y14)
                Change(burst.Place, burst.Form, CueKind.UltraBurst, "ultra_burst", BattleAnimator.UltraBurstSwapTime);
                break;
            case MegaEvolved mega:
                // A Mega Evolution or a Primal Reversion (plan 06 · R20): its own look and sound, the form taken inside
                // its light (plan 25 · Y14)
                Change(mega.Place, mega.Form, CueKind.Mega, mega.Primal ? "primal_reversion" : "mega_evolve", BattleAnimator.MegaSwapTime, mega.Primal);
                break;
            case ZPowerUsed zPower:
                // The Z-Power gathered before a Z-Move (plan 06 · R21; plan 25 · Y14): the pose and the frame in the
                // colour of its Z-Move's type, with its sound; nothing to see with the scene off
                Sound("z_power", zPower.Place.Side);
                if (Scene) Anim.ZPower(zPower.Place.Side, zPower.Place.Slot, MoveDatabase.Get(zPower.ZMove)?.Type ?? MoveDatabase.Get(zPower.From)?.Type ?? PokemonType.Normal);
                break;
            case Judged judged:
                // The Battle Arena's judge gives its verdict: the card stands over the line (plan 14 · B9)
                JudgeCard = judged.Verdict;
                break;
            case Rotated rotated:
            {
                // A rotation battle's turntable turns (plan 14 · B7): the Pokémon brought forward hops to the front as
                // the table turns under it, its own doll (if it kept one behind) in front of it at once
                var forward = mirror.Shown(rotated.Pokemon);
                forward.Status = showing?.Shows.OfType<CameInWith>().FirstOrDefault(brings => brings.Place == rotated.Place)?.Status ?? forward.Status;
                At(rotated.Place).Pokemon = forward;
                Anim.Rotate(rotated.Place.Side, forward, rotated.Way, Scene);
                Anim.SettleSubstitute(rotated.Place.Side, rotated.Place.Slot, rotated.Substitute);
                // The turn's whoosh is the recall's, as the one in front goes back to its seat
                if (Scene) Sound("recall", rotated.Place.Side);
                After(Scene ? BattleAnimator.RotationTime * 0.6f : 0f, () => AudioManager.PlayCry(forward, Cries.SendOutMode(forward), Pan(rotated.Place.Side)));
                break;
            }
            case PlacesSwapped swap:
                // Ally Switch (plan 06 · R25's leftovers): the screen's two places change Pokémon, and the views go
                // with them (the marks that last were moved by Conditions.Tell above)
                BattleCore.Exchange(At(swap.First), At(swap.Second), PlayerSlots.Concat(EnemySlots));
                Anim.SwapPlaces(swap.First.Side, swap.First.Slot, swap.Second.Slot);
                break;
            case AbilityShown ability:
                // Nothing is drawn or heard: the information panel remembers what the line named (plan 12 · Q11)
                if (At(ability.Place).Pokemon is { } bearer) Revealed.Ability(bearer, ability.Ability);
                break;
            case ItemShown item:
                if (At(item.Place).Pokemon is { } holder) Revealed.Item(holder, item.Item);
                break;
            case WeatherChanged weather:
                // The weather's cue and the layer that lasts (plan 25 · Y1), with its sound; a clear sky eases in
                Weather = weather.Weather;
                if (weather.Weather == BattleWeather.None) Anim.WeatherClears();
                // With the scene off the layer comes in with no cue and no sound (plan 08 · P1)
                else if (!Scene) Anim.WeatherQuietly(weather.Weather);
                else
                {
                    Anim.WeatherBegins(weather.Weather);
                    AudioManager.PlaySound(SoundBank.WeatherSound(weather.Weather));
                }
                break;
            case WeatherContinues goesOn:
                Weather = goesOn.Weather;
                if (!Scene)
                {
                    Anim.SettleWeather(goesOn.Weather);
                    break;
                }
                Anim.WeatherGoesOn(goesOn.Weather);
                AudioManager.PlaySound(SoundBank.WeatherSound(goesOn.Weather));
                break;
            case SideConditionChanged or FieldConditionChanged or PlaceConditionChanged:
                // What lies on the field (plan 25 · Y2): the line sets the look, which is drawn for as long as it lasts
                Anim.FieldLook.Tell(happening);
                break;
            case SubstituteChanged doll:
                // The doll's swaps (plan 25 · Y1): the line sets the view, never the rules
                if (doll.Up)
                {
                    Anim.SubstituteUp(doll.Place.Side, doll.Place.Slot);
                    Sound("substitute_in", doll.Place.Side);
                }
                else if (Anim[doll.Place.Side, doll.Place.Slot].BehindDoll)
                {
                    Anim.SubstituteDown(doll.Place.Side, doll.Place.Slot);
                    Sound("substitute_out", doll.Place.Side);
                }
                break;
            case GotAway:
                AudioManager.PlaySound("run_away", -0.3f);
                break;
            case Left { Ran: true } left:
                // A wild Pokémon that runs (a roamer, one of the Great Marsh's) is gone from its platform as its line
                // is read, with the sound of running; one called back goes with its recall
                Anim[left.Place.Side, left.Place.Slot].Present = false;
                Sound("run_away", left.Place.Side);
                break;
            case Fainted fainted:
                if (At(fainted.Place).Pokemon is { } down)
                {
                    down.Status = StatusCondition.Faint;
                    down.CurrentHP = 0;
                }
                // Its cry, low and slow, as it goes down
                if (At(fainted.Place).Pokemon is { } fallen) AudioManager.PlayCry(fallen, CryMode.Faint, Pan(fainted.Place.Side));
                Sound("faint", fainted.Place.Side);
                Anim.Faint(fainted.Place.Side, 0.15f, fainted.Place.Slot);
                break;
            case ExpGained exp:
                var gaining = mirror.Shown(exp.Pokemon);
                gaining.GainExp(exp.Amount, out _, out _, Rules.LevelCap);
                // The bar fills only under a Pokémon on the field
                if (Enumerable.Range(0, Anim.Slots).Any(s => Anim[BattleSide.Player, s].Shown == gaining))
                    AudioManager.PlaySound("exp", -0.3f);
                break;
            case MoveWanted wanted:
                // Nobody is asked anything while a recording is watched (plan 08 · P11)
                if (!Watching) WantMove(wanted);
                break;
            case LevelRose level:
            {
                var grown = mirror.Shown(level.Pokemon);
                if (!leveledUpPokemon.Contains(grown)) leveledUpPokemon.Add(grown);
                // The screen's Pokémon reckoned its own level as its EXP was shown, without the effort of the foe
                // just beaten, which the rules' copy had by then: it takes the rules' numbers, so its HP box and
                // the panel say the same
                grown.TakeStats(level.After);
                AudioManager.PlayFanfare(MusicRole.FanfareLevelUp);
                // The panel goes up with the level's line, which comes next and stays under it (plan 08 · P5); its
                // numbers are the event's, since the rules' copy is a whole turn ahead of this one
                HUD.LevelUp = new LevelUpPanel(grown, level.Level, level.Before, level.After);
                break;
            }
            case BallThrown ball:
                // The trainer throws, the ball opens over the foe, which vanishes into it; what comes next waits for
                // the ball to settle. A foe that breaks free bursts out with the ball and is back on its platform
                // before the line about it.
                AudioManager.PlaySound("ball_throw");
                Anim.ThrowBall(ball.Ball, ball.Slot, ball.Shakes);
                for (int wobble = 0; wobble < Math.Min(ball.Shakes, 3); wobble++)
                    After(BattleAnimator.BallWobbleAt(wobble), () => Sound("ball_shake", BattleSide.Enemy));
                After(BattleAnimator.BallSettleTime(ball.Shakes), () => Sound(ball.Shakes >= 4 ? "ball_click" : "ball_break", BattleSide.Enemy));
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
                // The catching lesson's catch is the assistant's: neither the player's Pokédex nor their team hears of it
                if (Kind == BattleKind.CatchingLesson) break;
                // The player's Pokémon cheer for the catch (plan 25 · Y5)
                if (Scene) Anim.Cheer(BattleSide.Player);
                CaughtNewSpecies = !Pokedex.IsCaught(mine.Species.DexNumber);
                Caught = mine;
                Pokedex.RegisterCaught(mine);
                mine.ResetStatStages();
                mine.Ball = caught.Ball;
                // Where, when and at what level it was met (Pokemon_SetCatchData)
                mine.Met(place, GameClock.Today);
                // A Pokémon caught in Pal Park stays the Catching Show's until the show is over (plan 06 · R16b)
                if (Kind == BattleKind.PalPark) break;
                // ...and one caught in the Bug-Catching Contest the contest's, until it is judged (plan 15 · E5)
                if (OnlyBall != null) break;
                if (caught.ToBox) pcBoxStorage?.Store(mine);
                else PlayerParty.Add(mine);
                break;
            }
            case TrainerReacts reacts:
                // What the trainer says, read straight after the line it came with (plan 11 · C12): the face's own
                // line, so the screen never takes the rules' state while it waits. Nothing with the option off. The
                // words are the chosen language's (plan 17 · N6): the data files' table keyed by the English line
                if (SpeaksTrainerLines && At(reacts.Place).Trainer is { } speaker && TrainerLines.For(speaker.Id, reacts.Cue) is { } said)
                {
                    // They step back onto their platform with the line's face and gesture as it appears, and off
                    // again once it has been read (plan 11 · C13): the second of two trainers at their own edge
                    bool second = Trainers.Count > 1 && (ReferenceEquals(Trainers[1], speaker) || Trainers[1].Id == speaker.Id && Trainers[0].Id != speaker.Id);
                    Action? stepBack = Scene ? () => Anim.StepBack(second, said.Face, said.Gesture) : null;
                    interlude.Enqueue(new Line(PlayerIdentity.Fill(Text.OfData(said.Text)), stepBack, PlayerIdentity.Fill(speaker.Name)));
                    if (Scene) interlude.Enqueue(new Happening(Anim.SendOffSpeaker));
                }
                break;
            case Won:
                AudioManager.PlayMusic(MusicDirector.VictoryRole(MusicDirector.BattleRole(Trainers.Select(t => t.TrainerClass))));
                // Whoever of the player's still stands cheers (plan 25 · Y5)
                if (Scene) Anim.Cheer(BattleSide.Player);
                break;
            case Ended ended:
                Result = ended.Result;
                // Every Tera Type is taken off (plan 06 · R23)
                ShowTera(happening);
                break;
        }
    }

    /// <summary>The line whose events are being shown, while they are (null for an event that stands between lines).</summary>
    private Said? showing;

    /// <summary>The line being shown carries a moment of the turn: its HP changes land with the moment, quietly.</summary>
    private bool OnAMomentsLine => showing?.Shows.Any(e => e is MomentShown) == true;

    /// <summary>
    /// A moment of the turn on its Pokémon, with its sound (plan 25 · Y3). Nothing is seen or heard of one on a
    /// Pokémon that can't be seen, as the original's <c>BattleSystem_ShouldShowStatusEffect</c> has it. This is the
    /// one place every moment goes through: an option that turns the battle's scene off skips it here.
    /// </summary>
    private void PlayMoment(MomentShown moment)
    {
        // With the scene off no moment plays, nor its sound: each is one of the original's battle animations
        // (BtlCmd_PlayBattleAnimation; plan 08 · P1). Its HP still lands, quietly, with its line
        if (!Scene) return;
        // A held item that goes off shows as what it is, and the line that tells of it names it: a berry is eaten
        bool berry = moment.Moment == BattleMoment.HeldItem
            && showing?.Shows.OfType<ItemShown>().FirstOrDefault(named => named.Place == moment.Place) is { } held
            && ItemDatabase.Get(held.Item)?.Berry != null;
        var cue = Anim.Moment(moment.Place.Side, moment.Place.Slot, moment.Moment,
            moment.From is { } from ? (from.Side, from.Slot) : null, berry);
        if (Anim.Shows(cue) && SoundBank.MomentSound(moment.Moment) is { } sound) Sound(sound, moment.Place.Side);
    }

    public void Update(float dt)
    {
        // A recording watched (plan 08 · P11): Menu leaves at any line and Run held plays it three times as fast;
        // nothing else is read
        if (Watching)
        {
            Watch(dt, InputManager.IsActionDown(GameAction.Run), InputManager.IsActionPressed(GameAction.Menu));
            return;
        }
        Anim.Update(dt, (side, slot) => SlotsOf(side).ElementAtOrDefault(slot)?.Pokemon);
        UpdatePendingEffects(dt);
        HUD.LevelUp?.Advance(dt);

        if (IsLearningMove)
        {
            HandleLearnInput();
            return;
        }

        if (waitingForMessageConfirm)
        {
            if (ConfirmPressed || InputManager.IsActionPressed(GameAction.Cancel))
            {
                ConfirmMessage();
            }
            else if (AutoAdvanceAfter is { } pause && HUD.LevelUp == null)
            {
                // A whole line goes on by itself after its pause (plan 08 · P2); a level's panel and a question wait to be read
                autoAdvanceWaited += dt;
                if (autoAdvanceWaited >= pause) ConfirmMessage();
            }
            return;
        }
        autoAdvanceWaited = 0f;

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

    /// <summary>
    /// How long a whole line waits before it is read on by itself (the options' auto-advance, plan 08 · P2); null, the
    /// default, when it waits for a press. Only plain lines: a question never goes on by itself.
    /// </summary>
    public float? AutoAdvanceAfter { get; set; }
    private float autoAdvanceWaited;

    /// <summary>The message currently on screen. A tool may set it to show a line the log did not say (the harness's critical hit).</summary>
    public string CurrentMessage { get => currentMessage; internal set => currentMessage = value; }

    /// <summary>Who says the message on screen, named on a pill over it: a trainer speaking in battle (plan 11 · C12); null for the battle's own lines.</summary>
    public string? CurrentSpeaker { get; private set; }

    /// <summary>The trainers across the field speak at the moments the rules mark (<see cref="BattleSetup.TrainerLines"/>).</summary>
    public bool SpeaksTrainerLines { get; }

    /// <summary>A message is on screen waiting to be dismissed, or a question about a move to learn waits for its answer.</summary>
    public bool IsWaitingForConfirm => waitingForMessageConfirm || IsLearningMove;

    /// <summary>
    /// Dismisses the message on screen and goes on to what follows it. A question about a move to learn is
    /// answered with whatever keeps the moves as they are. A level's panel (plan 08 · P5) is read on like a
    /// message: the first press turns it from the gains to the new values, the second takes it down with the
    /// level's line, so whatever reads a battle on with this gets past a level as it gets past any line.
    /// </summary>
    public void ConfirmMessage()
    {
        if (IsLearningMove)
        {
            ChooseLearn(KeepingAnswer);
            return;
        }
        if (!waitingForMessageConfirm) return;

        // Anything the message started lands now, so what is shown never runs ahead of it
        FlushPendingEffects();

        if (HUD.LevelUp is { } panel)
        {
            if (!panel.Press()) return;
            HUD.LevelUp = null;
        }

        waitingForMessageConfirm = false;
        Pump();
    }

    /// <summary>The level-up panel on show, while a level's two pages wait to be read (null otherwise).</summary>
    public LevelUpPanel? LevelUp => HUD.LevelUp;

    private void After(float delay, Action effect) => pendingEffects.Add((delay, effect));

    /// <summary>A sound from one side of the battle: the player's a little to the left, the foe's to the right, as they stand.</summary>
    private static void Sound(string name, BattleSide side) => AudioManager.PlaySound(name, Pan(side));

    private static float Pan(BattleSide side) => side == BattleSide.Player ? -0.3f : 0.3f;

    /// <summary>How long after its ball opens a Pokémon cries: once it has begun to grow out of the light.</summary>
    public const float CryDelay = 0.15f;

    /// <summary>Seconds between the cries of a horde's Pokémon as they appear, the first at once (plan 14 · B4).</summary>
    public const float HordeCryStep = 0.3f;

    /// <summary>When the last move's sound was played: a move aimed at several Pokémon is shown once for each, and heard once.</summary>
    private float moveSoundAt = -1f;

    /// <summary>The ground the battle is fought on, which a cue carries for the looks that go by it (plan 25 · Y6).</summary>
    private readonly BattleTerrain terrain;

    /// <summary>
    /// A move's own sound as it sets off (plan 25 · Y6): its <c>use_</c> sound, or its type's until its batch gives it
    /// one; or, for the moves whose animation is the user's own voice (Growl, Roar, Hyper Voice, Howl, Chatter), the
    /// user's cry in the modes the original plays, the second as the first ends.
    /// </summary>
    private void MoveHeard(string move, PokemonType type, Place from)
    {
        if (SoundBank.CryModesOf(move) is { } modes && At(from).Pokemon is { } user)
        {
            AudioManager.PlayCry(user, modes[0], Pan(from.Side));
            if (modes.Length > 1)
                After((float)Cries.Get(user, modes[0]).Duration, () => AudioManager.PlayCry(user, modes[1], Pan(from.Side)));
            return;
        }
        Sound(SoundBank.MoveSound(move, type), from.Side);
    }

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
        // The tag that says it is a recording, and how to leave it (plan 08 · P11)
        if (Watched is { } watched) UI.ModernUi.ReplayTag(screenWidth, watched, WatchFast);
    }
}
