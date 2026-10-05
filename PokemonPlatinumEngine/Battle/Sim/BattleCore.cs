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

    private readonly Random rng;
    private readonly string playerName;
    private readonly IBattleController?[] controllers = new IBattleController?[2];
    private readonly Pokemon? firstTrainerPokemon;
    private readonly List<Pokemon> leveledUp = new();
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
        controllers[(int)BattleSide.Player] = setup.PlayerController;
        controllers[(int)BattleSide.Enemy] = setup.EnemyController;

        // A double battle needs two Pokémon able to fight on each side
        bool canDouble = PlayerParty.Members.Count(p => !p.IsFainted) >= 2 &&
            (Trainers.Count == 0 ? WildPokemon.Count >= 2
                : Trainers.Count >= 2 || Trainers[0].Party.Members.Count(p => !p.IsFainted) >= 2);
        Format = setup.Format == BattleFormat.Double && canDouble ? BattleFormat.Double : BattleFormat.Single;
        int slots = IsDouble ? 2 : 1;

        PlayerSlots = Enumerable.Range(0, slots).Select(i => new Battler(BattleSide.Player, i) { Roster = PlayerParty, Field = Field }).ToList();
        foreach (var p in PlayerParty.Members) Evolution.BeginBattle(p);
        EnemySlots = Enumerable.Range(0, slots).Select(i => new Battler(BattleSide.Enemy, i) { Field = Field }).ToList();
        Field.WeatherIgnored = () => AllBattlers.Any(b => b.IsActive && BattleEffects.Of(b).Any(e => e.IgnoresWeather));
        Field.MudSport = () => AllBattlers.Any(b => b.IsActive && b.Volatile.MudSport);
        Field.WaterSport = () => AllBattlers.Any(b => b.IsActive && b.Volatile.WaterSport);
        Field.Standing = side => SlotsOf(side).Count(b => b.IsActive);
        Field.Turn = () => Turn;
        Field.Battlers = () => AllBattlers;

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
                break;
            case ReplacementRequest replacement:
                if (choices.Count != 1 || choices[0].Kind != ChoiceKind.Switch || choices[0].Who != replacement.Place || !CanSendIn(choices[0].SwitchTo))
                    throw new ArgumentException($"A Pokémon able to fight is wanted for {replacement.Place}");
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
    public BattleRecord? Record => startedFrom is uint seed ? new BattleRecord(seed, answers.ToList()) : null;

    /// <summary>
    /// Plays a record through. The battle must be new, between teams as they were when the recorded one began,
    /// and have been given a <see cref="BattleRandom"/> of the record's seed: then its log is the recorded
    /// battle's, line for line.
    /// </summary>
    public void Replay(BattleRecord record)
    {
        if (startedFrom != record.Seed) throw new ArgumentException("A replay starts from the record's own seed");
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

    /// <summary>The party member at this position could come in: it can fight and isn't out already.</summary>
    public bool CanSendIn(int partyIndex) =>
        partyIndex >= 0 && partyIndex < PlayerParty.Count && !PlayerParty.Members[partyIndex].IsFainted &&
        PlayerSlots.All(b => b.Pokemon != PlayerParty.Members[partyIndex]);

    // ---------------------------------------------------------------- the log

    private Said Say(string text)
    {
        var said = new Said(text);
        log.Add(said);
        return said;
    }

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

    private static string JoinNames(IEnumerable<string> names)
    {
        var list = names.ToList();
        return list.Count <= 1 ? list.FirstOrDefault() ?? "" : string.Join(", ", list.Take(list.Count - 1)) + " and " + list[^1];
    }

    /// <summary>"your team" for the player's side and "the foe's team" for the other, as the field's lines say it.</summary>
    private static string TeamOf(BattleSide side, bool capital = false) =>
        side == BattleSide.Player ? (capital ? "Your team" : "your team") : (capital ? "The foe's team" : "the foe's team");

    // ---------------------------------------------------------------- the course of a battle

    private IEnumerable<BattleRequest> Course()
    {
        Intro();
        while (Result == BattleResult.None)
        {
            // Whoever faces a foe at the start of a turn has fought it, for the EXP it leaves
            foreach (var foe in EnemySlots.Where(b => b.IsActive))
                foreach (var mine in PlayerSlots.Where(b => b.IsActive)) foe.FoughtAgainst.Add(mine.Pokemon!);

            // A Pokémon in the middle of a move, or that has to recharge, has nothing to choose
            var choices = new List<BattleChoice>();
            var free = InTurnOrderOfPlaces().Where(b => b.IsActive && !b.IsHeldToItsMove).ToList();
            var asked = free.Where(b => controllers[(int)b.Side] == null).Select(b => b.Place).ToList();
            if (asked.Count > 0)
            {
                yield return new ActionRequest(asked);
                choices.AddRange(answer);
            }
            foreach (var b in free.Where(b => controllers[(int)b.Side] != null))
                choices.Add(controllers[(int)b.Side]!.ChooseAction(this, b));
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
        foreach (var foe in foes) Emit(new Seen(foe.Pokemon!.Species));

        if (IsTrainerBattle)
        {
            Emit(new TrainersStand(Trainers[0].TrainerClass, Trainers.Count > 1 ? Trainers[1].TrainerClass : null));
            string who = Trainers.Count > 1 ? $"{Trainers[0].FullTitle} and {Trainers[1].FullTitle} would like" : $"{Trainers[0].FullTitle} wants";
            Say($"{who} to battle!");

            // Each trainer announces their own Pokémon
            foreach (var group in foes.GroupBy(b => b.Trainer!))
            {
                var said = Say($"{group.Key.FullTitle} sent out {JoinNames(group.Select(b => b.Pokemon!.DisplayName))}!");
                foreach (var b in group)
                {
                    OnEntered(b, opening: true);
                    said.With(new Entered(b.Place, b.Pokemon!, FromBall: true));
                }
            }
        }
        else
        {
            foreach (var b in foes)
            {
                OnEntered(b, opening: true);
                Emit(new Entered(b.Place, b.Pokemon!, FromBall: false));
            }
            Say(foes.Count > 1
                ? $"Wild {JoinNames(foes.Select(b => b.Pokemon!.DisplayName))} appeared!"
                : $"A wild {foes[0].Pokemon!.DisplayName} appeared!");
        }

        var go = Say($"Go! {JoinNames(mine.Select(b => b.Pokemon!.DisplayName))}!");
        foreach (var b in mine)
        {
            OnEntered(b, opening: true);
            go.With(new Entered(b.Place, b.Pokemon!, FromBall: true));
        }

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
        }
        Field.Side(BattleSide.Player).FollowMe = null;
        Field.Side(BattleSide.Enemy).FollowMe = null;

        // Every place draws a number for the turn; a Quick Claw works on one in five of them
        for (int i = 0; i < speedRolls.Length; i++) speedRolls[i] = rng.Roll(RollKind.Speed, 65536);
        waiting = Order(choices.Select(ToAct).ToList());

        // Whoever chose Focus Punch tightens its focus first (BattleControllerPlayer_CheckPreMoveActions)
        foreach (var act in waiting.Where(a => a.Choice.Kind == ChoiceKind.Fight && a.Move?.Data.Effect == "HitLastWhiffIfHit" && a.User.IsActive && a.User.Pokemon!.Status != StatusCondition.Sleep))
            Say($"{act.User.Name} is tightening its focus!");

        while (waiting.Count > 0)
        {
            var act = waiting[0];
            waiting.RemoveAt(0);
            if (Result != BattleResult.None) yield break;
            speedOrder = BySpeed();
            // A Pokémon that fainted (or was switched out by its own trainer) loses its action
            if (act.User.Pokemon != act.Actor || !act.User.IsActive) continue;

            switch (act.Choice.Kind)
            {
                case ChoiceKind.Run:
                    if (!ranThisTurn) TryRun(act.User);
                    break;
                case ChoiceKind.Fight:
                    foreach (var request in ExecuteMove(act)) yield return request;
                    break;
                case ChoiceKind.Switch:
                    ExecuteSwitch(act.User, act.Choice.SwitchTo);
                    break;
                case ChoiceKind.Item:
                    ExecuteItemUse(act.User, act.Item!);
                    break;
            }
            act.User.Turn.Acted = true;
            if (Result == BattleResult.None) ResolveFaints();
            // What acts on entry is looked for again after every action (an ability gained by a Transform, a
            // shape the weather changed), as the original runs its switch-in check after every move
            if (Result == BattleResult.None) SwitchInChecks();

            // Trick Room turns the rest of the turn round
            if (orderChanged)
            {
                orderChanged = false;
                waiting = Order(waiting);
            }
        }
        ranThisTurn = false;
        if (Result != BattleResult.None) yield break;

        EndOfTurn();
        if (Result != BattleResult.None) yield break;

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
        public bool QuickClaw;
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
    /// of the places; then the moves, by priority, Quick Claw and Speed, with a coin for Pokémon of equal Speed.
    /// </summary>
    private List<Act> Order(List<Act> acts)
    {
        foreach (var act in acts)
            act.QuickClaw = BattleEffects.Of(act.User).Any(e => e.MovesFirstInBracket(speedRolls[Number(act.User)]));

        var byPlace = acts.OrderBy(a => Number(a.User)).ToList();
        var runner = byPlace.FirstOrDefault(a => a.Choice.Kind == ChoiceKind.Run && a.User.IsPlayerSide);
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

        int priorityA = first.Move?.Priority ?? 0, priorityB = second.Move?.Priority ?? 0;
        if (priorityA != priorityB) return priorityA < priorityB;

        if (first.QuickClaw != second.QuickClaw) return second.QuickClaw;
        return IsSlower(first.User, second.User);
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
    /// stage (doubled for Simple under Platinum's rules), what its ability makes of the weather, its item, Quick
    /// Feet with a condition or else paralysis, Slow Start's first five turns, Unburden once its item is gone,
    /// then a tailwind.
    /// </summary>
    public int EffectiveSpeed(Battler b)
    {
        var p = b.Pokemon!;
        string? ability = b.Ability?.Name;
        int stage = p.StatStages.GetValueOrDefault(StatType.Speed);
        if (ability == "Simple" && !Rules.SimpleDoublesChanges) stage = Math.Clamp(stage * 2, -6, 6);
        int speed = Formulas.Staged(p.Speed, stage);
        if (b.Ability?.Effect is { } own) speed = Formulas.Scale(speed, own.SpeedMultiplier(b));
        if (BattleEffects.ItemOf(b) is { } item) speed = Formulas.Scale(speed, item.SpeedMultiplier(b));
        if (ability == "Quick Feet" && p.Status != StatusCondition.None) speed = speed * 15 / 10;
        else if (p.Status == StatusCondition.Paralyze) speed /= Rules.ParalysisSpeedDivisor;
        if (ability == "Slow Start" && Turn - b.Volatile.SlowStartTurn < 5) speed /= 2;
        if (ability == "Unburden" && b.Volatile.CanUnburden && p.HeldItem == null) speed *= 2;
        if (Field.Side(b.Side).Tailwind) speed *= 2;
        return Math.Max(1, speed);
    }

    // ---------------------------------------------------------------- items and running

    private void ExecuteItemUse(Battler user, ItemData item)
    {
        if (item.Pocket == ItemPocket.PokeBalls)
        {
            ThrowBall(item);
        }
        else if (item.EffectType == ItemEffectType.HealHP)
        {
            var p = user.Pokemon!;
            p.CurrentHP = Math.Min(p.MaxHP, p.CurrentHP + item.EffectValue);
            Say($"Used a {item.Name}! {p.DisplayName}'s HP was restored!").With(new HpChanged(user.Place, p.CurrentHP, Healed: true));
        }
    }

    private void ThrowBall(ItemData ball)
    {
        var target = EnemySlots.FirstOrDefault(b => b.IsActive);
        if (target == null) return;
        var foe = target.Pokemon!;
        Say($"{playerName} used one {ball.Name}!");

        // A Pokémon that isn't there to be hit can't be caught either
        if (target.IsElsewhere)
        {
            Say("The Pokémon couldn't be reached!");
            return;
        }

        int shakes = CatchCalculator.Shakes(foe, ball, rng, Turn, Conditions);
        Emit(new BallThrown(ball.Name, target.Slot, shakes));
        if (shakes < 4)
        {
            Say(shakes switch
            {
                0 => "Oh no! The Pokémon broke free!",
                1 => "Aww! It appeared to be caught!",
                2 => "Aargh! Almost had it!",
                _ => "Gah! It was so close, too!"
            });
            return;
        }

        foe.ResetStatStages();
        foe.Ball = ball.Name;
        bool toBox = PlayerParty.IsFull;
        if (!toBox) PlayerParty.Add(foe);
        Emit(new Caught(foe, ball.Name, toBox));
        Say($"Gotcha! {foe.DisplayName} was caught!");
        if (toBox) Say($"{foe.DisplayName} was transferred to Box 1!");
        End(BattleResult.EnemyCaught);
    }

    /// <summary>Running from a wild battle works by the two Pokémon's Speed; each try that fails makes the next likelier.</summary>
    private void TryRun(Battler runner)
    {
        ranThisTurn = true;
        if (IsTrainerBattle)
        {
            Say("No! There's no running from a Trainer battle!");
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
            Say("Can't escape!");
            return;
        }
        Say("Got away safely!");
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
                if (down.Volatile.DestinyBond)
                {
                    foe.Pokemon!.CurrentHP = 0;
                    Say($"{down.Name} took {foe.Name} down with it!").With(new HpChanged(foe.Place, 0, Healed: false));
                    Faint(foe);
                }
                else if (down.Volatile.Grudge && by.Data != StruggleData && foe.Pokemon!.Moves.FirstOrDefault(m => m.Name == by.Move.Name) is { } used)
                {
                    used.CurrentPP = 0;
                    Say($"{foe.Name}'s {used.Name} lost all its PP to the grudge!");
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
        LetGoOf(down);
        ComeBack(down);
        if (down.HasSubstitute) Emit(new SubstituteChanged(down.Place, Up: false));
        Revert(down);
        down.ClearVolatile();
        NoteFaint(down);
        Say($"{down.Name} fainted!").With(new Fainted(down.Place));
        if (!down.IsPlayerSide) AwardExp(down);
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
        if (SideDefeated(PlayerSlots))
        {
            Say($"{playerName} is out of usable Pokémon!");
            Say($"{playerName} whited out...");
            End(BattleResult.PlayerDefeat);
            return;
        }

        if (!SideDefeated(EnemySlots)) return;
        Emit(new Won());
        if (IsTrainerBattle)
        {
            Say($"Player defeated {string.Join(" and ", Trainers.Select(t => t.FullTitle))}!");
            Say($"{playerName} received ${Trainers.Sum(t => t.PrizeMoney)} for winning!");
        }
        else
        {
            Say($"Player defeated the wild {JoinNames(EnemySlots.Where(b => b.Pokemon != null).Select(b => b.Pokemon!.DisplayName))}!");
        }
        // What Pay Day scattered, up to the original's most
        if (PayDayMoney > 0) Say($"{playerName} picked up ${Math.Min(PayDayMoney, 65535)}!");
        End(BattleResult.PlayerVictory);
    }

    private void End(BattleResult result)
    {
        // Whatever a move changed for the battle is put back (the original's party data was never touched), and
        // a knocked-off item is in its holder's hands again
        foreach (var b in AllBattlers.Where(b => b.Pokemon != null)) Revert(b);
        foreach (var (pokemon, item) in knockedOff) pokemon.HeldItem ??= item;
        knockedOff.Clear();
        if (result == BattleResult.PlayerVictory) PickUp();
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
    /// The EXP a fainted foe leaves (<see cref="Formulas.ExpShares"/>): shared among the player's Pokémon that
    /// fought it and are still standing, with the Exp. Share's half for its holders and the Lucky Egg's and a
    /// trainer battle's bonus on each one's part.
    /// </summary>
    private void AwardExp(Battler defeated)
    {
        var foe = defeated.Pokemon!;
        var standing = PlayerParty.Members.Where(p => !p.IsFainted).ToList();
        var fought = standing.Where(defeated.FoughtAgainst.Contains).ToList();
        var holders = standing.Where(p => p.HeldItem?.HoldEffect == HeldItemEffects.ExpShare).ToList();
        if (fought.Count == 0 && holders.Count == 0) return;

        var (share, shared) = Formulas.ExpShares(foe.BaseExpYield, foe.Level, fought.Count, holders.Count);
        foreach (var p in standing)
        {
            if (p.Level >= 100) continue;
            int part = (fought.Contains(p) ? share : 0) + (holders.Contains(p) ? shared : 0);
            if (part == 0) continue;
            int exp = Formulas.ExpFor(part, p.HeldItem?.HoldEffect == HeldItemEffects.ExpUp, IsTrainerBattle);

            Say($"{p.DisplayName} gained {exp} EXP. Points!");
            bool rose = p.GainExp(exp, out var moves);
            Emit(new ExpGained(p, exp));
            if (!rose) continue;

            // Evolution waits for the battle to end (see LeveledUp)
            if (!leveledUp.Contains(p)) leveledUp.Add(p);
            Emit(new LevelRose(p, p.Level));
            Say($"{p.DisplayName} grew to Lv. {p.Level}!");
            foreach (string move in moves) Say($"{p.DisplayName} learned {move}!");
        }
    }
}
