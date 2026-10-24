using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>What a battle's rules start from. The Pokémon handed over are the core's to change.</summary>
public sealed class CoreSetup
{
    public required Party PlayerParty { get; init; }

    /// <summary>The wild Pokémon met (one, or two in a wild double battle). Empty in trainer battles.</summary>
    public List<Pokemon> WildPokemon { get; init; } = new();

    /// <summary>The opposing trainers: one, or two who each send one Pokémon at a time.</summary>
    public List<Trainer> Trainers { get; init; } = new();

    /// <summary>The first trainer's first Pokémon when it isn't the first of their party.</summary>
    public Pokemon? FirstTrainerPokemon { get; init; }

    public BattleFormat Format { get; init; } = BattleFormat.Single;
    public Random? Random { get; init; }
    public Ruleset? Rules { get; init; }
    public BattleConditions Conditions { get; init; } = new();

    /// <summary>The player's name, for the lines that say it.</summary>
    public string PlayerName { get; init; } = "Player";

    /// <summary>Who chooses for the player's side from inside the core; null when the choices come from outside.</summary>
    public IBattleController? PlayerController { get; init; }

    /// <summary>Who chooses for the other side; left out, the opponents' own chooser (<see cref="TrainerAi"/>).</summary>
    public IBattleController? EnemyController { get; init; } = TrainerAi.Instance;

    /// <summary>What kind of battle it is (<see cref="BattleKind"/>).</summary>
    public BattleKind Kind { get; init; } = BattleKind.Normal;

    /// <summary>The trainer beside the player in a tag battle: their party fills the player's second place.</summary>
    public Trainer? Partner { get; init; }

    /// <summary>Who chooses for the partner; left out, the trainer AI.</summary>
    public IBattleController? PartnerController { get; init; } = TrainerAi.Instance;

    /// <summary>The player can't run from this battle.</summary>
    public bool CannotFlee { get; init; }

    /// <summary>The game's first battle: no critical hits (<c>BATTLE_STATUS_FIRST_BATTLE</c>).</summary>
    public bool FirstBattle { get; init; }

    /// <summary>The Safari Balls or Park Balls the player has.</summary>
    public int SpecialBalls { get; init; }

    /// <summary>
    /// A battle of the Battle Frontier (<c>BATTLE_TYPE_FRONTIER</c>, plan 06 · R18a): no item from the bag
    /// (<c>PLAYER_INPUT_ITEM</c>), no EXP (<c>BATTLE_TYPE_NO_EXPERIENCE</c>), no money won or lost and nothing picked up
    /// (<c>subscript_battle_won</c>, <c>subscript_battle_lost</c>), RUN gives the match up, a Soul Dew does nothing
    /// (<c>BattleSystem_CalcMoveDamage</c>), the foes may take the player's items (<c>BtlCmd_TryStealItem</c>), every
    /// Pokémon obeys (<c>BattleSystem_CheckObedience</c>) and Burmy keeps its cloak (<c>BattleSystem_SetBurmyForm</c>).
    /// </summary>
    public bool Frontier { get; init; }

    /// <summary>
    /// Nothing may be used from the bag by either side (plan 14 · B14, Let's Go's Master Trainers; plan 13 · V5's run
    /// rule of the same name): <see cref="BattleCore.WhyNot"/> refuses every item, and the trainer AI reaches for none.
    /// </summary>
    public bool NoBattleItems { get; init; }

    /// <summary>
    /// Battle Style Shift (plan 08 · P1; the original's <c>OPTIONS_BATTLE_STYLE_SHIFT</c>): when a trainer's Pokémon
    /// faints with another to come, the player is asked whether to switch before it comes in
    /// (<see cref="SwitchOfferRequest"/>). Off unless given, which is Set: nothing is asked.
    /// </summary>
    public bool OffersSwitch { get; init; }

    /// <summary>
    /// The difficulty chosen at NEW GAME (plan 13 · V2): under Easy a battle gives three halves of the EXP
    /// (<see cref="DifficultyRules.ScaleExp"/>). Normal unless given, which changes nothing; a record of the battle keeps it.
    /// </summary>
    public Difficulty Difficulty { get; init; } = Difficulty.Normal;

    /// <summary>
    /// The player has a Key Stone (plan 06 · R20): under rules with <see cref="Ruleset.MegaEvolution"/> one of their
    /// own Pokémon may Mega Evolve once in the battle. A trainer has theirs as <see cref="Trainer.KeyStone"/>.
    /// </summary>
    public bool KeyStone { get; init; }

    /// <summary>
    /// The player has a Z-Ring (plan 06 · R21): under rules with <see cref="Ruleset.ZMoves"/> one of their own Pokémon
    /// may use a Z-Move once in the battle, and one may Ultra Burst. A trainer has theirs as <see cref="Trainer.ZRing"/>.
    /// </summary>
    public bool ZRing { get; init; }

    /// <summary>
    /// The player has a Dynamax Band (plan 06 · R22): under rules with <see cref="Ruleset.Dynamax"/> one of their own
    /// Pokémon may Dynamax once in the battle. A trainer has theirs as <see cref="Trainer.DynamaxBand"/>.
    /// </summary>
    public bool DynamaxBand { get; init; }

    /// <summary>
    /// The player has a Tera Orb (plan 06 · R23): under rules with <see cref="Ruleset.Terastallization"/> one of their
    /// own Pokémon may Terastallize once in the battle. A trainer has theirs as <see cref="Trainer.TeraOrb"/>.
    /// </summary>
    public bool TeraOrb { get; init; }
}

/// <summary>
/// The rules of a battle and nothing else (plan 06 · R2): no screen, no sound, no keys, no clock. It is given the
/// two sides and a source of random numbers, asks for choices (<see cref="Request"/>), and each time it is answered
/// (<see cref="Submit"/>) works out everything that follows from them and writes it into its log
/// (<see cref="TakeLog"/>), up to the next thing it has to ask. The same seed and the same choices give the same
/// log, on any machine: that is a replay, and it is what a server runs for two players.
/// <para>
/// It changes the Pokémon it was handed as it goes, at once: a hit's damage is taken when the hit is worked out,
/// not when someone has read the line about it. So whoever shows the battle hands it copies and lets the log tell
/// them when each change is seen (<see cref="BattleEngine"/> does). Abilities and held items join in through
/// <see cref="BattleEffect"/>'s hooks, with the core as their <see cref="IBattleContext"/>.
/// </para>
/// Split over BattleCore.*.cs: this file is a battle's course and who goes first; .Choices.cs what may be chosen;
/// .Moves.cs a move from the checks before it to what follows it; .Effects.cs what each move does of its own;
/// .Switch.cs coming and going; .Turn.cs the end of a turn; .Context.cs the changes of HP, stats and conditions
/// that everything goes through.
/// </summary>
public sealed partial class BattleCore : IBattleContext
{
    public Party PlayerParty { get; }
    public IReadOnlyList<Trainer> Trainers { get; }
    public IReadOnlyList<Pokemon> WildPokemon { get; }
    public BattleFormat Format { get; }
    public Ruleset Rules { get; }
    public BattleConditions Conditions { get; }
    public Random Random => rng;

    public bool IsTrainerBattle => Trainers.Count > 0;
    public bool IsDouble => Format == BattleFormat.Double;

    /// <summary>The places on each side: one each in a single battle, two in a double.</summary>
    public IReadOnlyList<Battler> PlayerSlots { get; }
    public IReadOnlyList<Battler> EnemySlots { get; }
    public IEnumerable<Battler> AllBattlers => PlayerSlots.Concat(EnemySlots);

    /// <summary>The weather, the rooms and what each side has up or underfoot (plan 06 · R3).</summary>
    public FieldState Field { get; } = new();

    public BattleResult Result { get; private set; } = BattleResult.None;

    /// <summary>Turns played to their end.</summary>
    public int Turn { get; private set; }

    /// <summary>What the core waits for; null once the battle is over.</summary>
    public BattleRequest? Request { get; private set; }

    /// <summary>The player's Pokémon that gained a level, in the order they did.</summary>
    public IReadOnlyList<Pokemon> LeveledUp => leveledUp;

    /// <summary>What Pay Day has scattered for the player so far; picked up with the winnings.</summary>
    public int PayDayMoney { get; private set; }

    /// <summary>What kind of battle it is: a roamer, the catching lesson, the Great Marsh, Pal Park (plan 06 · R9).</summary>
    public BattleKind Kind { get; }

    /// <summary>The trainer beside the player in a tag battle, whose Pokémon stand in the player's second place.</summary>
    public Trainer? Partner { get; }

    /// <summary>The player can't run from this battle.</summary>
    public bool CannotFlee { get; }

    /// <summary>The game's first battle, which has no critical hits.</summary>
    public bool FirstBattle { get; }

    /// <summary>A battle of the Battle Frontier, with the rules of its own (<see cref="CoreSetup.Frontier"/>).</summary>
    public bool Frontier { get; }

    /// <summary>Nothing may be used from the bag by either side (<see cref="CoreSetup.NoBattleItems"/>).</summary>
    public bool NoBattleItems { get; }

    /// <summary>Battle Style Shift: the player is offered a switch before a trainer's next Pokémon comes in (<see cref="CoreSetup.OffersSwitch"/>).</summary>
    public bool OffersSwitch { get; }

    /// <summary>The difficulty the battle is fought under (<see cref="CoreSetup.Difficulty"/>).</summary>
    public Difficulty Difficulty { get; }

    /// <summary>No critical hit lands in the catching lesson or the first battle (<c>BtlCmd_CalcCrit</c>).</summary>
    internal bool NoCriticalHits => Kind == BattleKind.CatchingLesson || FirstBattle;

    /// <summary>The Safari Balls or Park Balls left, in the Great Marsh and Pal Park.</summary>
    public int SpecialBalls { get; private set; }

    /// <summary>What the trainer AI knows of the battle (the moves, abilities and items it has seen, its trainers' items).</summary>
    public Ai.AiMemory AiMemory { get; } = new();

    /// <summary>The Great Marsh's two counters (the original's <c>safariCatchStage</c> and <c>safariEscapeCount</c>, 0 to 12, 6 to begin with): bait and mud move them.</summary>
    public int SafariCatchStage { get; private set; } = 6;
    public int SafariEscapeCount { get; private set; } = 6;

    private readonly Random rng;
    private readonly string playerName;
    private readonly IBattleController? playerController, enemyController, partnerController;
    private readonly Pokemon? firstTrainerPokemon;
    private readonly List<Pokemon> leveledUp = new();
    private readonly HashSet<Pokemon> sentOut = new();

    /// <summary>The player's own Pokémon that were sent out in the battle (<c>battleParticipantMask</c>).</summary>
    public IReadOnlyCollection<Pokemon> SentOut => sentOut;

    /// <summary>What losing cost the player (<see cref="Formulas.MoneyPenalty"/>): taken off their money once the battle is over.</summary>
    public int MoneyLost { get; private set; }
    private readonly List<BattleEvent> log = new();
    private readonly List<IReadOnlyList<BattleChoice>> answers = new();
    private readonly uint? startedFrom;
    private int taken;
    private IEnumerator<BattleRequest>? course;
    private IReadOnlyList<BattleChoice> answer = new List<BattleChoice>();
    private int runAttempts;

    public BattleCore(CoreSetup setup)
    {
        PlayerParty = setup.PlayerParty;
        Trainers = setup.Trainers;
        WildPokemon = setup.WildPokemon;
        Rules = setup.Rules ?? Ruleset.Current;
        Conditions = setup.Conditions;
        rng = setup.Random ?? new BattleRandom(unchecked((uint)Dice.Shared.Next()));
        startedFrom = (rng as BattleRandom)?.State;
        playerName = setup.PlayerName;
        firstTrainerPokemon = setup.FirstTrainerPokemon;
        playerController = setup.PlayerController;
        enemyController = setup.EnemyController;
        partnerController = setup.PartnerController;
        Kind = setup.Kind;
        CannotFlee = setup.CannotFlee;
        FirstBattle = setup.FirstBattle;
        Frontier = setup.Frontier;
        OffersSwitch = setup.OffersSwitch;
        Difficulty = setup.Difficulty;
        KeyStone = setup.KeyStone;
        ZRing = setup.ZRing;
        DynamaxBand = setup.DynamaxBand;
        TeraOrb = setup.TeraOrb;
        Field.Frontier = Frontier;
        NoBattleItems = setup.NoBattleItems;
        Field.Inverse = Conditions.Inverse;
        SpecialBalls = setup.SpecialBalls;
        Partner = Kind is BattleKind.Safari or BattleKind.PalPark ? null : setup.Partner;

        // A double battle needs two Pokémon able to fight on each side; a partner brings the player's second
        bool foesForTwo = Trainers.Count == 0 ? WildPokemon.Count >= 2 : Trainers.Count >= 2 || Trainers[0].Party.Members.Count(p => !p.IsFainted) >= 2;
        bool canDouble = Partner != null
            ? PlayerParty.Members.Any(p => !p.IsFainted) && Partner.Party.Members.Any(p => !p.IsFainted) && foesForTwo
            : PlayerParty.Members.Count(p => !p.IsFainted) >= 2 && foesForTwo;
        if (Partner != null && !canDouble) Partner = null;
        Format = (setup.Format == BattleFormat.Double || Partner != null) && canDouble && Kind is not (BattleKind.Safari or BattleKind.PalPark)
            ? BattleFormat.Double : BattleFormat.Single;
        int slots = IsDouble ? 2 : 1;

        PlayerSlots = Enumerable.Range(0, slots).Select(i => new Battler(BattleSide.Player, i)
        {
            Roster = i == 1 && Partner != null ? Partner.Party : PlayerParty,
            Trainer = i == 1 ? Partner : null,
            Field = Field
        }).ToList();
        foreach (var p in PlayerParty.Members) Evolution.BeginBattle(p);
        EnemySlots = Enumerable.Range(0, slots).Select(i => new Battler(BattleSide.Enemy, i) { Field = Field }).ToList();
        Field.WeatherIgnored = () => AllBattlers.Any(b => b.IsActive && BattleEffects.Of(b).Any(e => e.IgnoresWeather));
        Field.MudSport = () => AllBattlers.Any(b => b.IsActive && b.Volatile.MudSport);
        Field.WaterSport = () => AllBattlers.Any(b => b.IsActive && b.Volatile.WaterSport);
        Field.Standing = side => SlotsOf(side).Count(b => b.IsActive);
        Field.Turn = () => Turn;
        Field.Battlers = () => AllBattlers;

        // The first Pokémon in: the first ones able to fight (a partner sends its own first)
        var leads = PlayerParty.Members.Where(p => !p.IsFainted).Take(Partner != null ? 1 : slots).ToList();
        if (leads.Count == 0) leads.Add(PlayerParty.Members.First());
        if (Partner != null) leads.Add(Partner.Party.Members.First(p => !p.IsFainted));
        for (int i = 0; i < leads.Count; i++) PlayerSlots[i].Pokemon = leads[i];

        for (int i = 0; i < slots; i++)
        {
            var place = EnemySlots[i];
            if (IsTrainerBattle)
            {
                var trainer = Trainers[Math.Min(i, Trainers.Count - 1)];
                place.Trainer = trainer;
                place.Roster = trainer.Party;
                place.Pokemon = i == 0 && firstTrainerPokemon != null
                    ? firstTrainerPokemon
                    : NextFromRoster(place, EnemySlots.Take(i).Select(b => b.Pokemon));
            }
            else if (i < WildPokemon.Count)
            {
                place.Pokemon = WildPokemon[i];
            }
        }

        foreach (var b in AllBattlers.Where(b => b.Pokemon != null)) b.Pokemon!.ResetStatStages();
    }

    // ---------------------------------------------------------------- asking and answering

    /// <summary>Plays the opening, up to the first choice.</summary>
    public void Start()
    {
        if (course != null) throw new InvalidOperationException("The battle has already started");
        course = Course().GetEnumerator();
        Step();
    }

    /// <summary>Answers <see cref="Request"/> and plays on to the next one, or to the end.</summary>
    public void Submit(params BattleChoice[] choices) => Submit((IReadOnlyList<BattleChoice>)choices);

    public void Submit(IReadOnlyList<BattleChoice> choices)
    {
        switch (Request)
        {
            case null:
                throw new InvalidOperationException("The battle isn't waiting for a choice");
            case ActionRequest asked:
                var missing = asked.Places.Where(p => choices.Count(c => c.Who == p) != 1).ToList();
                if (missing.Count > 0 || choices.Count != asked.Places.Count)
                    throw new ArgumentException($"One choice is wanted for each of: {string.Join(", ", asked.Places)}");
                foreach (var choice in choices)
                    if (WhyNot(choice) is { } refusal) throw new ArgumentException(refusal);
                // One Mega Evolution a trainer (plan 06 · R20): two in one answer are refused
                if (choices.Where(c => c.Mega).GroupBy(c => MegaKey(At(c.Who))).Any(g => g.Count() > 1))
                    throw new ArgumentException(Text.Of("battle.megaOnlyOnce"));
                // And one Z-Move and one Ultra Burst (plan 06 · R21)
                RefuseTwoZ(choices);
                // And one Dynamax (plan 06 · R22)
                RefuseTwoDynamax(choices);
                // And one Terastallization (plan 06 · R23)
                RefuseTwoTera(choices);
                break;
            case ReplacementRequest replacement:
                if (choices.Count != 1 || choices[0].Kind != ChoiceKind.Switch || choices[0].Who != replacement.Place || !CanSendIn(At(replacement.Place), choices[0].SwitchTo))
                    throw new ArgumentException($"A Pokémon able to fight is wanted for {replacement.Place}");
                break;
            case SwitchOfferRequest offer:
                if (choices.Count != 1 || choices[0].Who != offer.Place || choices[0].Kind is not (ChoiceKind.Stay or ChoiceKind.Switch))
                    throw new ArgumentException($"Stay or a switch is wanted for {offer.Place}");
                if (WhyNot(choices[0]) is { } refused) throw new ArgumentException(refused);
                break;
        }
        answer = choices.ToList();
        answers.Add(answer);
        Step();
    }

    private void Step() => Request = course!.MoveNext() ? course.Current : null;

    /// <summary>
    /// The battle so far, small enough to keep or send: where its random numbers began and every answer it was
    /// given from outside. Null when its numbers aren't a <see cref="BattleRandom"/>'s: nothing else comes out
    /// the same everywhere.
    /// </summary>
    public BattleRecord? Record => startedFrom is uint seed ? new BattleRecord(seed, answers.ToList(), Inverse, OffersSwitch, Difficulty) : null;

    /// <summary>
    /// Plays a record through. The battle must be new, between teams as they were when the recorded one began,
    /// and have been given a <see cref="BattleRandom"/> of the record's seed: then its log is the recorded
    /// battle's, line for line.
    /// </summary>
    public void Replay(BattleRecord record)
    {
        if (startedFrom != record.Seed) throw new ArgumentException("A replay starts from the record's own seed");
        if (Inverse != record.Inverse) throw new ArgumentException("A replay is fought under the record's own chart: an inverse battle's as an inverse battle");
        if (OffersSwitch != record.OffersSwitch) throw new ArgumentException("A replay offers a switch where the recorded battle did (plan 08 · P1): Shift's as Shift's");
        if (Difficulty != record.Difficulty) throw new ArgumentException("A replay is fought under the record's own difficulty (plan 13 · V2): Easy's EXP as Easy's");
        Start();
        foreach (var answered in record.Answers) Submit(answered);
    }

    /// <summary>Everything that has happened, from the first line on.</summary>
    public IReadOnlyList<BattleEvent> Log => log;

    /// <summary>Takes what has happened since the last time this was asked.</summary>
    public List<BattleEvent> TakeLog()
    {
        var fresh = log.GetRange(taken, log.Count - taken);
        taken = log.Count;
        return fresh;
    }

    /// <summary>The place a battler stands in.</summary>
    public Battler At(Place place) => SlotsOf(place.Side)[place.Slot];

    public IReadOnlyList<Battler> SlotsOf(BattleSide side) => side == BattleSide.Player ? PlayerSlots : EnemySlots;

    private static BattleSide Other(BattleSide side) => side == BattleSide.Player ? BattleSide.Enemy : BattleSide.Player;

    /// <summary>
    /// Who chooses for a place from inside the core: the opponents' chooser for the other side, the partner's for the
    /// partner's place, the player's own (null: the choices come from outside) for the player's. The catching
    /// lesson's helper chooses by itself.
    /// </summary>
    private IBattleController? ControllerOf(Battler b)
    {
        if (!b.IsPlayerSide) return enemyController;
        if (b.Trainer != null) return partnerController;
        if (Kind == BattleKind.CatchingLesson) return TrainerAi.Instance;
        return playerController;
    }

    /// <summary>The player's party member at this position could come in: it can fight and isn't out already.</summary>
    public bool CanSendIn(int partyIndex) => CanSendIn(PlayerSlots[0], partyIndex);

    /// <summary>The member at this position of the party that sends Pokémon to this place could come in there.</summary>
    public bool CanSendIn(Battler place, int partyIndex)
    {
        var roster = At(place.Place).Roster;
        if (roster == null || partyIndex < 0 || partyIndex >= roster.Count) return false;
        var p = roster.Members[partyIndex];
        return !p.IsFainted && SlotsOf(place.Side).All(b => b.Pokemon != p);
    }

    // ---------------------------------------------------------------- the log

    private Said Say(string text)
    {
        var said = new Said(text);
        // What the line names comes with it (plan 12 · Q11)
        said.Shows.AddRange(named);
        named.Clear();
        log.Add(said);
        return said;
    }

    /// <summary>The abilities and items named for the next line said (<see cref="AbilityOf"/>, <see cref="ItemOf"/>).</summary>
    private readonly List<BattleEvent> named = new();

    /// <summary>
    /// The ability in force of the Pokémon in a place, named in the next line said: the original's message tag
    /// (<c>BattleMessage_AbilityTag</c>), which tells whoever reads the log of it by an <see cref="AbilityShown"/>
    /// attached to the line (plan 12 · Q11). Every line that names a Pokémon's ability asks for its name here.
    /// </summary>
    public string AbilityOf(Battler holder)
    {
        string name = holder.Ability?.Name ?? holder.Pokemon?.AbilityName ?? "";
        if (holder.Pokemon != null && name.Length > 0) named.Add(new AbilityShown(holder.Place, name));
        return name.Length == 0 ? name : LocalNames.Ability(name);
    }

    /// <summary>
    /// The held item of the Pokémon in a place, named in the next line said (<c>BattleMessage_ItemTag</c>), or the
    /// one given when it has already left its hands (a berry eaten, an item knocked off).
    /// </summary>
    public string ItemOf(Battler holder, ItemData? item = null)
    {
        item ??= holder.Pokemon?.HeldItem;
        if (item == null) return "";
        if (holder.Pokemon != null) named.Add(new ItemShown(holder.Place, item.Name));
        return item.DisplayName;
    }

    /// <summary>A line made with a name in it isn't said after all: what it named goes unsaid too.</summary>
    private void Unsaid() => named.Clear();

    private void Emit(BattleEvent happening) => log.Add(happening);

    /// <summary>Runs something and takes back what it wrote into the log, to say it later.</summary>
    private List<BattleEvent> Capture(Action action)
    {
        int before = log.Count;
        action();
        var written = log.GetRange(before, log.Count - before);
        log.RemoveRange(before, written.Count);
        return written;
    }

    internal static string JoinNames(IEnumerable<string> names)
    {
        var list = names.ToList();
        return list.Count <= 1 ? list.FirstOrDefault() ?? "" : Text.Of("battle.names", ("some", string.Join(Text.Of("battle.namesComma"), list.Take(list.Count - 1))), ("last", list[^1]));
    }

    /// <summary>"your team" for the player's side and "the foe's team" for the other, as the field's lines say it.</summary>
    private static string TeamOf(BattleSide side, bool capital = false) =>
        side == BattleSide.Player ? Text.Of(capital ? "battle.team.yoursCapital" : "battle.team.yours") : Text.Of(capital ? "battle.team.foesCapital" : "battle.team.foes");

    // ---------------------------------------------------------------- the course of a battle

    private IEnumerable<BattleRequest> Course()
    {
        Intro();
        // An Intimidate as the battle opens sets off an Eject Pack or a Mirror Herb (plan 06 · R28b)
        foreach (var request in ItemsAfterTheEntry()) yield return request;
        while (Result == BattleResult.None)
        {
            // Whoever faces a foe at the start of a turn has fought it, for the EXP it leaves
            foreach (var foe in EnemySlots.Where(b => b.IsActive))
                foreach (var mine in PlayerSlots.Where(b => b.IsActive)) foe.FoughtAgainst.Add(mine.Pokemon!);

            // A Pokémon in the middle of a move, or that has to recharge, has nothing to choose
            var choices = new List<BattleChoice>();
            var free = InTurnOrderOfPlaces().Where(b => b.IsActive && !b.IsHeldToItsMove).ToList();
            var asked = free.Where(b => ControllerOf(b) == null).Select(b => b.Place).ToList();
            megaChosen.Clear();
            zChosen.Clear();
            dynamaxChosen.Clear();
            teraChosen.Clear();
            if (asked.Count > 0)
            {
                yield return new ActionRequest(asked);
                choices.AddRange(answer);
                ChooseMegas(answer);
            }
            foreach (var b in free.Where(b => ControllerOf(b) != null))
            {
                var chosen = ControllerOf(b)!.ChooseAction(this, b);
                // A chooser inside may only Mega Evolve where the rules let it (plan 06 · R20)
                if (chosen.Mega && !CanMegaEvolve(b)) chosen = chosen with { Mega = false };
                // And use a Z-Move or Ultra Burst only where they let it (plan 06 · R21)
                chosen = AsTheZRulesLet(b, chosen);
                // And Dynamax only where they let it (plan 06 · R22)
                chosen = AsTheDynamaxRulesLet(b, chosen);
                // And Terastallize only where they let it (plan 06 · R23)
                chosen = AsTheTeraRulesLet(b, chosen);
                ChooseMegas(new[] { chosen });
                choices.Add(chosen);
            }
            // The Great Marsh's Pokémon and Pal Park's have no moves to choose: they watch, or run
            if (Kind is BattleKind.Safari or BattleKind.PalPark) choices.RemoveAll(c => c.Who.Side == BattleSide.Enemy);
            foreach (var b in InTurnOrderOfPlaces().Where(b => b.IsActive && b.IsHeldToItsMove))
                choices.Add(BattleChoice.GoOn(b.Place));

            foreach (var request in PlayTurn(choices)) yield return request;
        }
    }

    /// <summary>The trainer's opening lines and the send-outs, then the weather of the place and the abilities that act on entry.</summary>
    private void Intro()
    {
        var foes = EnemySlots.Where(b => b.Pokemon != null).ToList();
        var mine = PlayerSlots.Where(b => b.Pokemon != null).ToList();
        foreach (var foe in foes) Emit(Seen.Of(foe.Pokemon!));

        if (IsTrainerBattle)
        {
            Emit(new TrainersStand(Trainers[0].TrainerClass, Trainers.Count > 1 ? Trainers[1].TrainerClass : null));
            Say(Trainers.Count > 1
                ? Text.Of("battle.wouldLikeToBattle", ("trainer", Trainers[0].FullTitle), ("trainer2", Trainers[1].FullTitle))
                : Text.Of("battle.wantsToBattle", ("trainer", Trainers[0].FullTitle)));

            // Each trainer announces their own Pokémon
            foreach (var group in foes.GroupBy(b => b.Trainer!))
            {
                var said = Say(Text.Of("battle.sentOut", ("trainer", group.Key.FullTitle), ("pokemon", JoinNames(group.Select(b => ShownAs(b, b.Pokemon!))))));
                foreach (var b in group)
                {
                    OnEntered(b, opening: true);
                    Bringing(said.With(new Entered(b.Place, b.Pokemon!, FromBall: true)), b);
                }
            }
        }
        else
        {
            foreach (var b in foes)
            {
                OnEntered(b, opening: true);
                Emit(new Entered(b.Place, b.Pokemon!, FromBall: false));
                // What it has as it is met shows from the start (plan 09 · L2)
                foreach (var brought in Brought(b)) Emit(brought);
            }
            Say(foes.Count > 1
                ? Text.Of("battle.wildAppeared", ("pokemon", JoinNames(foes.Select(b => b.Pokemon!.DisplayName))))
                : Text.Of("battle.aWildAppeared", ("pokemon", foes[0].Pokemon!.DisplayName)));
        }

        // The Great Marsh and Pal Park are played with balls alone: none of the player's Pokémon comes out
        if (Kind is BattleKind.Safari or BattleKind.PalPark)
        {
            OpenUnderTheSky();
            return;
        }

        // A partner sends theirs out with their own line
        var own = mine.Where(b => b.Trainer == null).ToList();
        var go = Say(Text.Of("battle.go", ("pokemon", JoinNames(own.Select(b => ShownAs(b, b.Pokemon!))))));
        foreach (var b in own)
        {
            OnEntered(b, opening: true);
            Bringing(go.With(new Entered(b.Place, b.Pokemon!, FromBall: true)), b);
        }
        foreach (var b in mine.Where(b => b.Trainer != null))
        {
            OnEntered(b, opening: true);
            Bringing(Say(Text.Of("battle.sentOut", ("trainer", b.Trainer!.FullTitle), ("pokemon", ShownAs(b, b.Pokemon!)))).With(new Entered(b.Place, b.Pokemon!, FromBall: true)), b);
        }

        AnnounceFormat();
        OpenUnderTheSky();
        SwitchInChecks();
    }

    /// <summary>A turn: everyone acts in order, the end of the turn runs, fainted Pokémon are replaced.</summary>
    private IEnumerable<BattleRequest> PlayTurn(List<BattleChoice> choices)
    {
        foreach (var b in AllBattlers)
        {
            b.MovedThisTurn = false;
            b.Flinched = false;
            b.TookCriticalHit = false;
            b.Turn = new TurnFlags();
            b.Volatile.ItemWentOff = false;
        }
        Field.Side(BattleSide.Player).FollowMe = null;
        Field.Side(BattleSide.Enemy).FollowMe = null;

        // Every place draws a number for the turn; a Quick Claw works on one in five of them
        for (int i = 0; i < speedRolls.Length; i++) speedRolls[i] = rng.Roll(RollKind.Speed, 65536);
        waiting = Order(choices.Select(ToAct).ToList());

        // Whoever chose Focus Punch tightens its focus first (BattleControllerPlayer_CheckPreMoveActions)
        foreach (var act in waiting.Where(a => a.Choice.Kind == ChoiceKind.Fight && a.Move?.Data.Effect == "HitLastWhiffIfHit" && a.User.IsActive && a.User.Pokemon!.Status != StatusCondition.Sleep && !Loafs(a.User)))
            Say(Text.Of("battle.isTighteningItsFocus", ("user", act.User.Name)));
        // Beak Blast heats its beak and Shell Trap is set before anyone moves (plan 06 · R25; Showdown's priorityChargeCallback)
        GetReady();

        bool megasDone = false;
        while (waiting.Count > 0)
        {
            var act = waiting[0];
            // The turn's Mega Evolutions come once its switches and items are done, before its first move (plan 06 ·
            // R20); the moves still to come are then put in order again by the Speeds they gave
            if (!megasDone && act.Choice.Kind is not (ChoiceKind.Item or ChoiceKind.Switch or ChoiceKind.Run))
            {
                megasDone = true;
                // The Dynamaxes come after them (plan 06 · R22; Showdown's runDynamax, order 105)
                // And the Terastallizations after those (plan 06 · R23; Showdown's terastallize, order 106)
                if (MegaEvolutions(waiting) | Dynamaxes(waiting) | Terastallizations(waiting))
                {
                    if (Result != BattleResult.None) yield break;
                    waiting = Order(waiting);
                    continue;
                }
            }
            waiting.RemoveAt(0);
            if (Result != BattleResult.None) yield break;
            speedOrder = BySpeed();
            // A Pokémon that fainted (or was switched out by its own trainer) loses its action
            if (act.User.Pokemon != act.Actor || !act.User.IsActive) continue;
            // What a stage noted before this action and nothing took up goes no further (plan 06 · R28b)
            ejectPacks.Clear();
            mirrored.Clear();

            switch (act.Choice.Kind)
            {
                case ChoiceKind.Run:
                    if (!act.User.IsPlayerSide) Flee(act.User);
                    else if (!ranThisTurn) TryRun(act.User);
                    break;
                case ChoiceKind.Bait:
                    ThrowBait(act.User);
                    break;
                case ChoiceKind.Mud:
                    ThrowMud(act.User);
                    break;
                case ChoiceKind.Fight:
                    foreach (var request in ExecuteMove(act)) yield return request;
                    break;
                case ChoiceKind.Switch:
                    ExecuteSwitch(act.User, act.Choice.SwitchTo);
                    break;
                case ChoiceKind.Item:
                    ExecuteItemUse(act.User, act);
                    break;
            }
            act.User.Turn.Acted = true;
            if (Result == BattleResult.None) ResolveFaints();
            if (Result == BattleResult.None) ItemsAfterTheAction();
            // What acts on entry is looked for again after every action (an ability gained by a Transform, a
            // shape the weather changed), as the original runs its switch-in check after every move
            if (Result == BattleResult.None) SwitchInChecks();
            // An Eject Pack and a Mirror Herb take up what a coming in did (plan 06 · R28b)
            foreach (var request in ItemsAfterTheEntry()) yield return request;
            // A trainer whose last Pokémon the action left in the red speaks once it has been told (plan 11 · C12)
            WatchTheRed();

            // Trick Room turns the rest of the turn round
            if (orderChanged)
            {
                orderChanged = false;
                waiting = Order(waiting);
            }
        }
        ranThisTurn = false;
        if (Result != BattleResult.None) yield break;

        // The Great Marsh's Pokémon has its say once the player has thrown: it runs, or watches
        if (Kind == BattleKind.Safari)
        {
            SafariPokemonActs();
            if (Result != BattleResult.None) yield break;
            Turn++;
            yield break;
        }
        if (Kind == BattleKind.PalPark)
        {
            Turn++;
            yield break;
        }

        EndOfTurn();
        if (Result != BattleResult.None) yield break;
        WatchTheRed();

        foreach (var request in ReplaceFainted()) yield return request;
        if (Result != BattleResult.None) yield break;
        Turn++;
        SetUpNextTurn();
        // The original's switch-in check opens every turn: Slow Start's five turns end here, and a shape follows the weather
        SwitchInChecks();
    }

    private bool ranThisTurn;

    /// <summary>The actions of the turn still to come, in order.</summary>
    private List<Act> waiting = new();

    /// <summary>The last move anyone got to use (the original's <c>movePrev</c>): what Copycat copies. Null after a move that never got going.</summary>
    private MoveData? lastMoveShown;

    /// <summary>Items knocked off their holders, lost for the battle and given back at its end (the original's <c>knockedOffItemsMask</c>).</summary>
    private readonly Dictionary<Pokemon, ItemData> knockedOff = new();

    /// <summary>Something changed who is faster in the middle of a turn: what is still to come is put in order again.</summary>
    private bool orderChanged;

    // ---------------------------------------------------------------- who goes first

    /// <summary>One choice with the places and moves it names found.</summary>
    private sealed class Act
    {
        public required BattleChoice Choice;
        public required Battler User;
        public Pokemon? Actor;
        public Move? Move;
        public Battler? Target;
        public ItemData? Item;
        /// <summary>Its item makes it move first among moves of its priority this turn (a Quick Claw, a Custap Berry), or last (a Lagging Tail).</summary>
        public bool MovesFirst, Lags;
    }

    private Act ToAct(BattleChoice choice)
    {
        var user = At(choice.Who);
        var act = new Act { Choice = choice, User = user, Actor = user.Pokemon };
        switch (choice.Kind)
        {
            case ChoiceKind.Fight:
                act.Move = MoveFor(user, choice.Move);
                if (choice.Target is { } target) act.Target = At(target);
                break;
            case ChoiceKind.Item:
                act.Item = ItemDatabase.Get(choice.Item ?? "") ?? throw new ArgumentException($"There is no item called {choice.Item}");
                break;
        }
        return act;
    }

    /// <summary>The places in the order the original numbers them: the player's first, the foe's first, then the second of each.</summary>
    private IEnumerable<Battler> InTurnOrderOfPlaces() => AllBattlers.OrderBy(Number);

    private static int Number(Battler b) => b.Place.Number;

    /// <summary>
    /// The order of a turn, as the original works it out: running first; then items and switches, in the order
    /// of the places; then the moves, by priority, a Quick Claw or Custap Berry, a Lagging Tail, Stall and Speed,
    /// with a coin for Pokémon of equal Speed.
    /// </summary>
    private List<Act> Order(List<Act> acts)
    {
        foreach (var act in acts) ItemsInTheOrder(act);

        var byPlace = acts.OrderBy(a => Number(a.User)).ToList();
        var runner = byPlace.FirstOrDefault(a => a.Choice.Kind == ChoiceKind.Run);
        if (runner != null) return byPlace.Where(a => a == runner).Concat(byPlace.Where(a => a != runner)).ToList();

        var order = byPlace.Where(a => a.Choice.Kind is ChoiceKind.Item or ChoiceKind.Switch)
            .Concat(byPlace.Where(a => a.Choice.Kind is not (ChoiceKind.Item or ChoiceKind.Switch))).ToList();
        for (int i = 0; i < order.Count - 1; i++)
        {
            for (int j = i + 1; j < order.Count; j++)
            {
                if (order[i].Choice.Kind != order[j].Choice.Kind) continue;
                if (GoesAfter(order[i], order[j])) (order[i], order[j]) = (order[j], order[i]);
            }
        }
        return order;
    }

    private readonly int[] speedRolls = new int[4];

    /// <summary>Everyone from the fastest to the slowest, as last worked out (before each action and at the turn's end).</summary>
    private List<Battler> speedOrder = new();

    /// <summary><c>BattleSystem_CompareBattlerSpeed</c>: whether the first of two actions of a kind should come second.</summary>
    private bool GoesAfter(Act first, Act second)
    {
        var a = first.User.Pokemon!;
        var b = second.User.Pokemon!;
        if (a.CurrentHP <= 0 && b.CurrentHP > 0) return true;
        if (a.CurrentHP > 0 && b.CurrentHP <= 0) return false;

        // (A Max Move goes by its own priority, Max Guard's +4 and the rest's 0: plan 06 · R22)
        int priorityA = ActPriority(first), priorityB = ActPriority(second);
        if (priorityA != priorityB) return priorityA < priorityB;

        // A Quick Claw or a Custap Berry that went off (two: by Speed alone, Trick Room or not), then a Lagging
        // Tail or Full Incense (two: the faster goes after), then Stall and Speed
        if (first.MovesFirst && second.MovesFirst) return ByPlainSpeed(first.User, second.User, fasterGoesAfter: false);
        if (first.MovesFirst != second.MovesFirst) return second.MovesFirst;
        if (first.Lags && second.Lags) return ByPlainSpeed(first.User, second.User, fasterGoesAfter: true);
        if (first.Lags != second.Lags) return first.Lags;
        return IsSlower(first.User, second.User);
    }

    /// <summary>By Speed alone, Trick Room or not (how the original settles two Quick Claws or two Lagging Tails), a coin between equals.</summary>
    private bool ByPlainSpeed(Battler first, Battler second, bool fasterGoesAfter)
    {
        int speedA = EffectiveSpeed(first), speedB = EffectiveSpeed(second);
        if (speedA != speedB) return fasterGoesAfter ? speedA > speedB : speedA < speedB;
        return rng.Roll(RollKind.SpeedTie, 2) == 1;
    }

    /// <summary>
    /// By Speed alone, once a Quick Claw has had its say: whoever has Stall goes after (both: the faster goes
    /// after, Trick Room or not); then the slower goes after, or the faster while Trick Room is up; a coin between equals.
    /// </summary>
    private bool IsSlower(Battler first, Battler second)
    {
        int speedA = EffectiveSpeed(first), speedB = EffectiveSpeed(second);
        bool stallA = Has(first, "Stall"), stallB = Has(second, "Stall");
        if (stallA && stallB) return speedA != speedB ? speedA > speedB : rng.Roll(RollKind.SpeedTie, 2) == 1;
        if (stallA != stallB) return stallA;
        if (speedA != speedB) return Field.TrickRoom ? speedA > speedB : speedA < speedB;
        return rng.Roll(RollKind.SpeedTie, 2) == 1;
    }

    /// <summary>
    /// Everyone on the field from the fastest to the slowest (<c>BattleSystem_SortMonSpeedOrder</c>): the order
    /// the end of a turn, the weather and what acts on entry go in. A Pokémon that can't fight is behind those
    /// that can.
    /// </summary>
    private List<Battler> BySpeed()
    {
        var order = InTurnOrderOfPlaces().Where(b => b.Pokemon != null).ToList();
        for (int i = 0; i < order.Count - 1; i++)
        {
            for (int j = i + 1; j < order.Count; j++)
            {
                bool downA = !order[i].IsActive, downB = !order[j].IsActive;
                bool swap = downA != downB ? downA : !downA && IsSlower(order[i], order[j]);
                if (swap) (order[i], order[j]) = (order[j], order[i]);
            }
        }
        return order;
    }

    /// <summary>
    /// Speed as the turn order sees it (<c>BattleSystem_CompareBattlerSpeed</c>, in its order): the stat by its
    /// stage (doubled for Simple under Platinum's rules), what its ability makes of the weather, its items (a Macho
    /// Brace, an Iron Ball or a Power item halves it, read from the item itself so that neither an Embargo nor
    /// Klutz undoes it; a Choice Scarf adds half; a Quick Powder doubles a Ditto's), Quick Feet with a condition or
    /// else paralysis, Slow Start's first five turns, Unburden once its item is gone, then a tailwind.
    /// </summary>
    public int EffectiveSpeed(Battler b)
    {
        var p = b.Pokemon!;
        string? ability = b.Ability?.Name;
        int stage = p.StatStages.GetValueOrDefault(StatType.Speed);
        if (ability == "Simple" && !Rules.SimpleDoublesChanges) stage = Math.Clamp(stage * 2, -6, 6);
        int speed = Formulas.Staged(p.Speed, stage);
        if (b.Ability?.Effect is { } own) speed = Formulas.Scale(speed, own.SpeedMultiplier(b));
        if (p.HeldItem?.HoldEffect is { } raw && HeldItemEffects.HalvesSpeed(raw)) speed /= 2;
        string? hold = BattleEffects.HoldEffectOf(b);
        if (hold == "ChoiceSpeed" && !b.Volatile.Dynamaxed) speed = speed * 15 / 10;
        if (hold == "DittoSpeedUp" && p.Species.Name == "Ditto") speed *= 2;
        if (ability == "Quick Feet" && p.Status != StatusCondition.None) speed = speed * 15 / 10;
        else if (p.Status == StatusCondition.Paralyze) speed /= Rules.ParalysisSpeedDivisor;
        if (ability == "Slow Start" && Turn - b.Volatile.SlowStartTurn < 5) speed /= 2;
        if (ability == "Unburden" && b.Volatile.CanUnburden && p.HeldItem == null) speed *= 2;
        if (Field.Side(b.Side).Tailwind) speed *= 2;
        // A swamp round its side quarters it (plan 06 · R25's leftovers; Showdown's grasspledge onModifySpe)
        if (Field.Side(b.Side).Swamp) speed = Formulas.Modify(speed, 1024);
        return Math.Max(1, speed);
    }

    // ---------------------------------------------------------------- balls and running (the rest of the bag is in BattleCore.Items.cs)

    private void ThrowBall(ItemData ball)
    {
        var target = EnemySlots.FirstOrDefault(b => b.IsActive);
        if (target == null) return;
        var foe = target.Pokemon!;
        Say(Text.Of("battle.usedOne", ("player", playerName), ("item", Kind == BattleKind.PalPark ? "Park Ball" : ball.DisplayName)));

        // A Pokémon that isn't there to be hit can't be caught either
        if (target.IsElsewhere)
        {
            Say(Text.Of("battle.thePokemonCouldntBe"));
            return;
        }

        // The catching lesson's ball and Pal Park's can't fail (BATTLE_TYPE_ALWAYS_CATCH)
        int shakes = Kind is BattleKind.CatchingLesson or BattleKind.PalPark ? 4 : CatchCalculator.Shakes(foe, ball, rng, Turn, Conditions, SafariCatchStage,
            Rules, PlayerSlots.FirstOrDefault(b => b.IsActive)?.Pokemon);
        if (ball.Name == "Safari Ball" || ball.Name == Conditions.OnlyBall) SpecialBalls = Math.Max(0, SpecialBalls - 1);
        Emit(new BallThrown(ball.Name, target.Slot, shakes));
        if (shakes < 4)
        {
            Say(shakes switch
            {
                0 => Text.Of("battle.ohNoThePokemon"),
                1 => Text.Of("battle.awwItAppearedTo"),
                2 => Text.Of("battle.aarghAlmostHadIt"),
                _ => Text.Of("battle.gahItWasSo")
            });
            if (Kind == BattleKind.Safari) SafariGameOver();
            if (Conditions.OnlyBall != null)
            {
                ContestBallsGone();
                if (Result != BattleResult.None) return;
            }
            // Ball Fetch picks the first ball that failed up (plan 06 · R27b)
            FetchTheBall(ball);
            return;
        }

        // The lesson's catch is the assistant's own: it stays theirs
        if (Kind == BattleKind.CatchingLesson)
        {
            Emit(new Caught(foe, ball.Name, ToBox: false));
            Say(Text.Of("battle.gotchaWasCaught", ("foe", foe.DisplayName)));
            End(BattleResult.EnemyCaught);
            return;
        }

        foe.ResetStatStages();
        foe.Ball = ball.Name;
        // A Heal Ball makes it whole and a Friend Ball its friend (plan 06 · R28a)
        CatchCalculator.AfterTheCatch(foe, ball);
        // The Bug-Catching Contest keeps its catch for itself: it joins neither the team nor the PC here (plan 15 · E5)
        if (Conditions.OnlyBall != null)
        {
            Emit(new Caught(foe, ball.Name, ToBox: false));
            Say(Text.Of("battle.gotchaWasCaught", ("foe", foe.DisplayName)));
            End(BattleResult.EnemyCaught);
            return;
        }
        bool toBox = PlayerParty.IsFull;
        if (!toBox) PlayerParty.Add(foe);
        Emit(new Caught(foe, ball.Name, toBox));
        Say(Text.Of("battle.gotchaWasCaught", ("foe", foe.DisplayName)));
        if (toBox) Say(Text.Of("battle.wasTransferredToBox", ("foe", foe.DisplayName)));
        End(BattleResult.EnemyCaught);
    }

    /// <summary>Running from a wild battle works by the two Pokémon's Speed; each try that fails makes the next likelier.</summary>
    private void TryRun(Battler runner)
    {
        ranThisTurn = true;
        // COMMAND_SELECTION_RUN_SELECT in the Battle Frontier: the match is given up, and lost
        if (Frontier)
        {
            Say(Text.Of("battle.forfeitedTheMatch", ("player", playerName)));
            End(BattleResult.PlayerDefeat);
            return;
        }
        if (IsTrainerBattle)
        {
            Say(Text.Of("battle.noThereNoRunning"));
            return;
        }
        if (CannotFlee)
        {
            Say(Text.Of("battle.thereNoGettingAway"));
            return;
        }
        // The Great Marsh and Pal Park can always be left
        if (Kind is BattleKind.Safari or BattleKind.PalPark)
        {
            Say(Text.Of("battle.gotAwaySafely")).With(new GotAway());
            End(BattleResult.PlayerRan);
            return;
        }

        // Run Away and a Smoke Ball always get away, and don't count as a try
        bool away = BattleEffects.Of(runner).Any(e => e.AlwaysEscapes);
        if (!away)
        {
            var foe = EnemySlots.FirstOrDefault(b => b.IsActive) ?? EnemySlots[0];
            int speed = runner.Pokemon!.Speed, foeSpeed = foe.Pokemon?.Speed ?? 1;
            // The roll is only made for a Pokémon slower than the foe, as in the original
            away = speed >= foeSpeed || Formulas.Escapes(speed, foeSpeed, runAttempts, rng.Roll(RollKind.Escape, 256));
            runAttempts++;
        }
        if (!away)
        {
            Say(Text.Of("battle.cantEscape"));
            return;
        }
        Say(Text.Of("battle.gotAwaySafely")).With(new GotAway());
        End(BattleResult.PlayerRan);
    }

    // ---------------------------------------------------------------- fainting, EXP and the end of the battle

    /// <summary>
    /// Every Pokémon down to 0 HP faints (with EXP for the player's), then the battle ends if a side has nobody
    /// left. One brought down by a foe's move (<paramref name="by"/>) may take that foe with it (Destiny Bond,
    /// which is told first and faints the foe first) or leave the move without PP (Grudge), as the original's
    /// <c>subscript_faint_check_destiny_bond</c> has it. <paramref name="last"/> goes down after everyone else
    /// (the user of Explosion, after its targets).
    /// </summary>
    private void ResolveFaints(MoveUse? by = null, Battler? last = null)
    {
        while (Result == BattleResult.None)
        {
            var down = NextFallen(last);
            if (down == null) break;

            if (by != null && by.User.Side != down.Side && by.User.IsActive && by.Hits.Any(h => h.Target == down && h.Touched))
            {
                var foe = by.User;
                // A Dynamaxed Pokémon isn't taken down (plan 06 · R22; Showdown's destinybond onFaint)
                if (down.Volatile.DestinyBond && !foe.Volatile.Dynamaxed)
                {
                    foe.Pokemon!.CurrentHP = 0;
                    Say(Text.Of("battle.tookDownWithIt", ("pokemon", down.Name), ("foe", foe.Name))).With(new HpChanged(foe.Place, 0, Healed: false));
                    Faint(foe);
                }
                else if (down.Volatile.Grudge && by.Data != StruggleData && foe.Pokemon!.Moves.FirstOrDefault(m => m.Name == by.Move.Name) is { } used)
                {
                    used.CurrentPP = 0;
                    Say(Text.Of("battle.lostAllItsPp", ("foe", foe.Name), ("move", used.DisplayName)));
                }
            }
            Faint(down);
        }
        if (Result == BattleResult.None) CheckBattleEnd();
    }

    private void Faint(Battler down)
    {
        down.Pokemon!.Status = StatusCondition.Faint;
        down.Pokemon.CurrentHP = 0;
        var letGo = LetGoOf(down);
        ComeBack(down);
        if (down.HasSubstitute) Emit(new SubstituteChanged(down.Place, Up: false));
        Undynamax(down, said: false);
        Revert(down);
        down.ClearVolatile();
        NoteFaint(down);
        faintedOnTurn[down.Side] = Turn;
        CountTheFallen(down);
        var fell = Say(Text.Of("battle.fainted", ("pokemon", down.Name))).With(new Fainted(down.Place));
        // Whoever loved it no longer does, from the line that tells of its fall (plan 09 · L2)
        foreach (var told in letGo) fell.With(told);
        FellFirst(down, fell);
        if (!down.IsPlayerSide) AwardExp(down);
        CheckStrongWeather(down);
        // Soul-Heart answers every faint (plan 06 · R27), Receiver and Power of Alchemy an ally's (R27b)
        SoulHeart(down);
        Receives(down);
    }

    /// <summary>The next Pokémon at 0 HP that hasn't been told so yet: the fastest first, as the original faints them.</summary>
    private Battler? NextFallen(Battler? last = null)
    {
        static bool Down(Battler b) => b.Pokemon != null && b.Pokemon.CurrentHP <= 0 && b.Pokemon.Status != StatusCondition.Faint;
        return speedOrder.Concat(InTurnOrderOfPlaces()).FirstOrDefault(b => b != last && Down(b)) ?? (last != null && Down(last) ? last : null);
    }

    /// <summary>
    /// What a faint leaves behind outside the battle: the player's Pokémon likes its trainer a little less (a lot
    /// less against a foe thirty levels above it) and loses what it had taken toward an evolution without fainting,
    /// and a foe going down counts for the Pokémon that were facing it, for the evolutions that count knock-outs.
    /// </summary>
    private void NoteFaint(Battler down)
    {
        if (down.IsPlayerSide)
        {
            int strongest = EnemySlots.Where(b => b.Pokemon != null).Select(b => b.Pokemon!.Level).DefaultIfEmpty(0).Max();
            FriendshipRules.Apply(down.Pokemon!, strongest - down.Pokemon!.Level >= 30 ? FriendshipEvent.FaintToStronger : FriendshipEvent.Faint);
            Evolution.CountFaint(down.Pokemon!);
            return;
        }
        foreach (var mine in PlayerSlots.Where(b => b.IsActive)) Evolution.CountDefeat(mine.Pokemon!, down.Pokemon!.Species);
    }

    private bool SideDefeated(IReadOnlyList<Battler> side) =>
        !side.Any(b => b.IsActive) && !side.Any(b => b.Roster != null && b.Roster.Members.Any(p => !p.IsFainted));

    private void CheckBattleEnd()
    {
        // Beside a partner the player is beaten once their own party is down, whatever the partner has left
        // (BattleControllerPlayer_CheckBattleOver, for a battle with an AI partner)
        bool beaten = Partner != null ? !PlayerParty.Members.Any(p => !p.IsFainted) : SideDefeated(PlayerSlots);
        if (beaten)
        {
            // subscript_battle_lost: a share of the money, by the team's highest level and the badges
            // (BtlCmd_PayPrizeMoney), dropped before a wild Pokémon or handed to a trainer
            Say(Text.Of("battle.hasNoPokemonLeft", ("player", playerName)));
            // subscript_battle_lost in the Battle Frontier: no money changes hands and nobody blacks out
            if (Frontier)
            {
                End(BattleResult.PlayerDefeat);
                return;
            }
            int highest = PlayerParty.Members.Select(p => p.Level).DefaultIfEmpty(0).Max();
            MoneyLost = Formulas.MoneyPenalty(highest, Conditions.Badges, Conditions.Money);
            if (MoneyLost > 0)
                Say(IsTrainerBattle ? Text.Of("battle.handedOverToThe", ("player", playerName), ("money", MoneyLost)) : Text.Of("battle.droppedInThePanic", ("player", playerName), ("money", MoneyLost)));
            Say(Text.Of("battle.silence"));
            Say(Text.Of("battle.whitedOut", ("player", playerName)));
            End(BattleResult.PlayerDefeat);
            return;
        }

        if (!SideDefeated(EnemySlots)) return;
        Emit(new Won());
        if (IsTrainerBattle)
        {
            Say(Trainers.Count > 1
                ? Text.Of("battle.defeatedBoth", ("player", playerName), ("trainer", Trainers[0].FullTitle), ("trainer2", Trainers[1].FullTitle))
                : Text.Of("battle.defeated", ("player", playerName), ("trainer", Trainers[0].FullTitle)));
            // subscript_battle_won skips the prize, what Pay Day scattered and Pickup's find in the Battle Frontier
            if (Frontier)
            {
                End(BattleResult.PlayerVictory);
                return;
            }
            Say(Text.Of("battle.receivedForWinning", ("player", playerName), ("money", PrizeMoney)));
        }
        else
        {
            Say(Text.Of("battle.defeatedTheWild", ("player", playerName), ("pokemon", JoinNames(EnemySlots.Where(b => b.Pokemon != null).Select(b => b.Pokemon!.DisplayName)))));
        }
        // What Pay Day scattered, up to the original's most
        if (PayDayMoney > 0) Say(Text.Of("battle.pickedUp", ("player", playerName), ("money", Math.Min(PayDayMoney, 65535))));
        End(BattleResult.PlayerVictory);
    }

    private void End(BattleResult result)
    {
        // Whatever a move changed for the battle is put back (the original's party data was never touched), and
        // a knocked-off item is in its holder's hands again
        // A Dynamax ends with the battle (plan 06 · R22)
        foreach (var b in AllBattlers.Where(b => b.Pokemon != null)) Undynamax(b, said: false);
        foreach (var b in AllBattlers.Where(b => b.Pokemon != null)) Revert(b);
        // And the shapes that last the battle out are given back (plan 06 · R19b: Disguise, Ice Face, Zero to Hero)
        GiveBackLastingShapes();
        // And so are the Mega and primal forms (plan 06 · R20)
        GiveBackMegas();
        // And a Terastallization ends (plan 06 · R23)
        EndTerastallizations();
        foreach (var (pokemon, item) in knockedOff) pokemon.HeldItem ??= item;
        knockedOff.Clear();
        if (result == BattleResult.PlayerVictory && !Frontier) PickUp();
        // Burmy takes the cloak of the ground it fought on (BattleSystem_SetBurmyForm; not in the Great Marsh or Pal Park)
        if (Kind is not (BattleKind.Safari or BattleKind.PalPark or BattleKind.CatchingLesson) && !Frontier)
            FormRules.CloakBurmy(sentOut, Conditions.Terrain);
        Result = result;
        Emit(new Ended(result));
    }

    /// <summary>What Pickup finds, from the commonest up: the table slides up a row every ten levels (the original's <c>sCommonPickupItems</c>).</summary>
    private static readonly string[] PickupCommon =
    {
        "Potion", "Antidote", "Super Potion", "Great Ball", "Repel", "Escape Rope", "Full Heal", "Hyper Potion", "Ultra Ball",
        "Revive", "Rare Candy", "Dusk Stone", "Shiny Stone", "Dawn Stone", "Full Restore", "Max Revive", "PP Up", "Max Elixir"
    };

    /// <summary>The two rare finds of each row (<c>sRarePickupItems</c>).</summary>
    private static readonly string[] PickupRare =
    {
        "Hyper Potion", "Nugget", "King's Rock", "Full Restore", "Ether", "White Herb", "TM44", "Elixir", "TM01", "Leftovers", "TM26"
    };

    /// <summary>The draw of 100 each common find's share ends at (<c>sCommonPickupRate</c>): 30%, then 10% each down to 4%, 4%, and 1% for each rare find.</summary>
    private static readonly int[] PickupRates = { 30, 40, 50, 60, 70, 80, 90, 94, 98 };

    /// <summary>Honey Gather's chance in a hundred, by ten levels (<c>sHoneyGatherRate</c>).</summary>
    private static readonly int[] HoneyGatherRates = { 5, 10, 15, 20, 25, 30, 35, 40, 45, 50 };

    /// <summary>
    /// <c>BtlCmd_GenerateEndOfBattleItem</c>, at the end of a battle won: each of the player's Pokémon with Pickup
    /// and no item has one chance in ten of finding one, drawn by its level, and each with Honey Gather and no item
    /// its level's chance of finding Honey. Nothing is said of it.
    /// </summary>
    private void PickUp()
    {
        foreach (var p in PlayerParty.Members)
        {
            if (p.HeldItem != null) continue;
            if (p.AbilityName == "Pickup" && rng.Roll(RollKind.Pickup, 10) == 0)
            {
                int draw = rng.Roll(RollKind.Pickup, 100);
                int row = Math.Min((p.Level - 1) / 10, PickupRates.Length);
                for (int j = 0; j < PickupRates.Length; j++)
                {
                    if (PickupRates[j] > draw)
                    {
                        p.HeldItem = ItemDatabase.Get(PickupCommon[row + j]);
                        break;
                    }
                    if (draw >= 98)
                    {
                        p.HeldItem = ItemDatabase.Get(PickupRare[row + (99 - draw)]);
                        break;
                    }
                }
            }
            else if (p.AbilityName == "Honey Gather" && rng.Roll(RollKind.Pickup, 100) < HoneyGatherRates[Math.Min((Math.Max(1, p.Level) - 1) / 10, 9)])
            {
                p.HeldItem = ItemDatabase.Get("Honey");
            }
        }
    }

    /// <summary>
    /// The EXP a fainted foe leaves (<c>Task_GiveExperience</c>; <see cref="Formulas.ExpShares"/>): shared among the
    /// player's Pokémon that fought it and are still standing, with the Exp. Share's half for its holders, and the
    /// Lucky Egg's, a trainer battle's and another trainer's Pokémon's bonus on each one's part. Whoever gains EXP
    /// gains the foe's effort too (<see cref="EffortRules.Gain"/>); a Pokémon at the rules' level cap gains neither
    /// (100 in Platinum; plan 18 · W4's 260 by the modern rules, where effort still comes). By the modern rules the
    /// whole party gains by the scaled formula (<see cref="Formulas.ScaledExp"/>).
    /// </summary>
    private void AwardExp(Battler defeated)
    {
        // BATTLE_TYPE_NO_EXPERIENCE: nothing is gained in the Battle Frontier
        if (Frontier) return;
        var foe = defeated.Pokemon!;
        var standing = PlayerParty.Members.Where(p => !p.IsFainted).ToList();
        var fought = standing.Where(defeated.FoughtAgainst.Contains).ToList();

        if (Rules.ScaledExp)
        {
            foreach (var p in standing)
            {
                bool traded = Obedience.IsOutsider(p, Conditions.Player);
                if (p.Level >= Rules.LevelCap)
                {
                    if (Rules.EvsAtTheCap) EffortRules.Gain(p, foe.EvYield, Rules);
                    continue;
                }
                bool past = p.Species.LevelEvolution is { } next && p.Level >= next.Level;
                int exp = Formulas.ScaledExp(foe.BaseExpYield, foe.Level, p.Level, fought.Contains(p), traded,
                    p.HeldItem?.HoldEffect == HeldItemEffects.ExpUp, past, foreign: p.Language != null);
                Give(p, exp, traded, foe);
            }
            return;
        }

        var holders = standing.Where(p => p.HeldItem?.HoldEffect == HeldItemEffects.ExpShare).ToList();
        if (fought.Count == 0 && holders.Count == 0) return;

        var (share, shared) = Formulas.ExpShares(foe.BaseExpYield, foe.Level, fought.Count, holders.Count);
        foreach (var p in standing)
        {
            if (p.Level >= Rules.LevelCap) continue;
            int part = (fought.Contains(p) ? share : 0) + (holders.Contains(p) ? shared : 0);
            if (part == 0) continue;
            bool traded = Obedience.IsOutsider(p, Conditions.Player);
            Give(p, Formulas.ExpFor(part, p.HeldItem?.HoldEffect == HeldItemEffects.ExpUp, IsTrainerBattle, traded, foreign: p.Language != null), traded, foe);
        }
    }

    /// <summary>
    /// One Pokémon's EXP: the line, the foe's effort (before the level, which counts it), the levels it reaches and
    /// the moves they bring. A move it can't fit is the screen's to ask about (<see cref="MoveWanted"/>).
    /// </summary>
    private void Give(Pokemon p, int exp, bool boosted, Pokemon foe)
    {
        // Easy gives three halves of it (plan 13 · V2); the other difficulties leave it as it is
        exp = DifficultyRules.ScaleExp(exp, Difficulty);
        Say(boosted ? Text.Of("battle.gainedABoostedExp", ("pokemon", p.DisplayName), ("exp", exp)) : Text.Of("battle.gainedExpPoints", ("pokemon", p.DisplayName), ("exp", exp)));
        EffortRules.Gain(p, foe.EvYield, Rules);
        // The stats it had, for the level-up panel (plan 08 · P5): effort changes none of them until a level reckons them again
        var before = p.Stats();
        bool rose = p.GainExp(exp, out var learned, out var wanted, Rules.LevelCap);
        Emit(new ExpGained(p, exp));
        if (!rose) return;

        // Evolution waits for the battle to end (see LeveledUp)
        if (!leveledUp.Contains(p)) leveledUp.Add(p);
        Emit(new LevelRose(p, p.Level, before, p.Stats()));
        var grew = Say(Text.Of("battle.grewToLv", ("pokemon", p.DisplayName), ("level", p.Level)));
        // Only a Pokémon standing on the field plays the level's animation (SEQ_GET_EXP_CHECK_LEVEL_UP; plan 25 · Y3)
        if (PlayerSlots.FirstOrDefault(b => b.IsActive && b.Pokemon == p) is { } standing) grew.With(Moment(standing, BattleMoment.LevelUp));
        foreach (string move in learned) Say(Text.Of("battle.learned", ("pokemon", p.DisplayName), ("move", LocalNames.Move(move))));
        foreach (string move in wanted) Emit(new MoveWanted(p, move));
    }
}
