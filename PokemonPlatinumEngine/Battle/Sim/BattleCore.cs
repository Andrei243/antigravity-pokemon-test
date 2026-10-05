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
/// Split over BattleCore.*.cs: this file is a battle's course, .Moves.cs a move from the checks before it to what
/// follows it, .Context.cs the changes of HP, stats and conditions that everything goes through.
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

    public BattleResult Result { get; private set; } = BattleResult.None;

    /// <summary>Turns played to their end.</summary>
    public int Turn { get; private set; }

    /// <summary>What the core waits for; null once the battle is over.</summary>
    public BattleRequest? Request { get; private set; }

    /// <summary>The player's Pokémon that gained a level, in the order they did.</summary>
    public IReadOnlyList<Pokemon> LeveledUp => leveledUp;

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

    // ---------------------------------------------------------------- the course of a battle

    private IEnumerable<BattleRequest> Course()
    {
        Intro();
        while (Result == BattleResult.None)
        {
            // Whoever faces a foe at the start of a turn has fought it, for the EXP it leaves
            foreach (var foe in EnemySlots.Where(b => b.IsActive))
                foreach (var mine in PlayerSlots.Where(b => b.IsActive)) foe.FoughtAgainst.Add(mine.Pokemon!);

            var choices = new List<BattleChoice>();
            var asked = InTurnOrderOfPlaces().Where(b => b.IsActive && controllers[(int)b.Side] == null).Select(b => b.Place).ToList();
            if (asked.Count > 0)
            {
                yield return new ActionRequest(asked);
                choices.AddRange(answer);
            }
            foreach (var b in InTurnOrderOfPlaces().Where(b => b.IsActive && controllers[(int)b.Side] != null))
                choices.Add(controllers[(int)b.Side]!.ChooseAction(this, b));

            foreach (var request in PlayTurn(choices)) yield return request;
        }
    }

    /// <summary>The trainer's opening lines and the send-outs, then the abilities that act on entry.</summary>
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
                    OnEntered(b);
                    said.With(new Entered(b.Place, b.Pokemon!, FromBall: true));
                }
            }
        }
        else
        {
            foreach (var b in foes)
            {
                OnEntered(b);
                Emit(new Entered(b.Place, b.Pokemon!, FromBall: false));
            }
            Say(foes.Count > 1
                ? $"Wild {JoinNames(foes.Select(b => b.Pokemon!.DisplayName))} appeared!"
                : $"A wild {foes[0].Pokemon!.DisplayName} appeared!");
        }

        var go = Say($"Go! {JoinNames(mine.Select(b => b.Pokemon!.DisplayName))}!");
        foreach (var b in mine)
        {
            OnEntered(b);
            go.With(new Entered(b.Place, b.Pokemon!, FromBall: true));
        }
        RunEntryEffects(AllBattlers.Where(b => b.IsActive).ToList());
    }

    /// <summary>A turn: everyone acts in order, the end-of-turn effects run, fainted Pokémon are replaced.</summary>
    private IEnumerable<BattleRequest> PlayTurn(List<BattleChoice> choices)
    {
        foreach (var b in AllBattlers)
        {
            b.MovedThisTurn = false;
            b.Flinched = false;
            b.TookCriticalHit = false;
        }

        foreach (var act in Order(choices.Select(ToAct).ToList()))
        {
            if (Result != BattleResult.None) yield break;
            // A Pokémon that fainted (or was switched out by its own trainer) loses its action
            if (act.User.Pokemon != act.Actor || !act.User.IsActive) continue;

            switch (act.Choice.Kind)
            {
                case ChoiceKind.Run:
                    if (!ranThisTurn) TryRun(act.User);
                    break;
                case ChoiceKind.Fight:
                    ExecuteMove(act.User, act.Move!, act.Target);
                    break;
                case ChoiceKind.Switch:
                    ExecuteSwitch(act.User, act.Choice.SwitchTo);
                    break;
                case ChoiceKind.Item:
                    ExecuteItemUse(act.User, act.Item!);
                    break;
            }
            if (Result == BattleResult.None) ResolveFaints();
        }
        ranThisTurn = false;
        if (Result != BattleResult.None) yield break;

        EndOfTurn();
        ResolveFaints();
        if (Result != BattleResult.None) yield break;

        foreach (var request in ReplaceFainted()) yield return request;
        Turn++;
    }

    private bool ranThisTurn;

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
                var moves = user.Pokemon!.Moves;
                act.Move = choice.Move >= 0 && choice.Move < moves.Count ? moves[choice.Move] : new Move(StruggleData);
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

    private static int Number(Battler b) => b.Slot * 2 + (b.Side == BattleSide.Enemy ? 1 : 0);

    /// <summary>
    /// The order of a turn, as the original works it out: running first; then items and switches, in the order
    /// of the places; then the moves, by priority, Quick Claw and Speed, with a coin for Pokémon of equal Speed.
    /// </summary>
    private List<Act> Order(List<Act> acts)
    {
        // Every place draws a number for the turn; a Quick Claw works on one in five of them
        var rolls = new int[4];
        for (int i = 0; i < rolls.Length; i++) rolls[i] = rng.Roll(RollKind.Speed, 65536);
        foreach (var act in acts)
            act.QuickClaw = BattleEffects.Of(act.User).Any(e => e.MovesFirstInBracket(rolls[Number(act.User)]));

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
        int speedA = EffectiveSpeed(first.User), speedB = EffectiveSpeed(second.User);
        if (speedA != speedB) return speedA < speedB;
        return rng.Roll(RollKind.SpeedTie, 2) == 1;
    }

    /// <summary>
    /// Speed as the turn order sees it: the stat by its stage, then what abilities and items do to it, then
    /// paralysis.
    /// </summary>
    public int EffectiveSpeed(Battler b) => SpeedOf(b, Rules);

    public static int SpeedOf(Battler b, Ruleset rules)
    {
        var p = b.Pokemon!;
        var effects = BattleEffects.Of(b).ToList();
        int speed = Formulas.Staged(p.Speed, p.StatStages.GetValueOrDefault(StatType.Speed));
        foreach (var e in effects) speed = Formulas.Scale(speed, e.SpeedMultiplier(b));
        if (p.Status == StatusCondition.Paralyze && !effects.Any(e => e.IgnoresParalysisSlowdown)) speed /= rules.ParalysisSpeedDivisor;
        return Math.Max(1, speed);
    }

    // ---------------------------------------------------------------- entering and leaving the field

    /// <summary>Puts a Pokémon in a place on the field.</summary>
    private void SendIn(Battler place, Pokemon pokemon)
    {
        place.Pokemon = pokemon;
        OnEntered(place);
    }

    private void OnEntered(Battler place)
    {
        place.ClearVolatile();
        place.Pokemon!.ResetStatStages();
        place.Pokemon.ToxicCounter = 0;
        place.FoughtAgainst.Clear();
        foreach (var foe in EnemySlots.Where(b => b.IsActive))
            foreach (var mine in PlayerSlots.Where(b => b.IsActive)) foe.FoughtAgainst.Add(mine.Pokemon!);
    }

    private void Withdraw(Battler place)
    {
        foreach (var e in BattleEffects.Of(place)) e.OnWithdraw(place);
        place.ClearVolatile();
        place.Pokemon!.ResetStatStages();
        place.Pokemon.ToxicCounter = 0;
    }

    /// <summary>Abilities that act on entry (Intimidate), fastest first.</summary>
    private void RunEntryEffects(List<Battler> entered)
    {
        foreach (var b in entered.Where(b => b.IsActive).OrderByDescending(EffectiveSpeed).ToList())
        {
            foreach (var e in BattleEffects.Of(b)) e.OnEntry(this, b);
            CheckConditionHooks(b, null);
        }
    }

    private void ExecuteSwitch(Battler place, int partyIndex)
    {
        if (!CanSendIn(partyIndex)) return;
        var outgoing = place.Pokemon!;
        var incoming = PlayerParty.Members[partyIndex];
        Say($"Come back, {outgoing.DisplayName}!").With(new Recalled(place.Place));
        Withdraw(place);
        Emit(new Left(place.Place));
        SendIn(place, incoming);
        Say($"Go! {incoming.DisplayName}!").With(new Entered(place.Place, incoming, FromBall: true));
        RunEntryEffects(new List<Battler> { place });
    }

    /// <summary>Fills the places of fainted Pokémon from the bench: trainers send theirs, the player picks.</summary>
    private IEnumerable<BattleRequest> ReplaceFainted()
    {
        var entered = new List<Battler>();

        // The foes first
        foreach (var place in EnemySlots.Where(b => !b.IsActive && b.Roster != null))
        {
            var next = NextFromRoster(place, EnemySlots.Select(b => b.Pokemon));
            if (next == null) continue;
            entered.Add(place);
            SendIn(place, next);
            Say($"{place.Trainer!.FullTitle} sent out {next.DisplayName}!")
                .With(new Seen(next.Species)).With(new Entered(place.Place, next, FromBall: true));
        }

        // Then each empty place of the player's, while there is anyone left to send
        foreach (var place in PlayerSlots.Where(b => !b.IsActive))
        {
            if (!Enumerable.Range(0, PlayerParty.Count).Any(CanSendIn)) break;
            int index;
            if (controllers[(int)BattleSide.Player] is { } controller) index = controller.ChooseReplacement(this, place);
            else
            {
                yield return new ReplacementRequest(place.Place);
                index = answer[0].SwitchTo;
            }
            if (!CanSendIn(index)) continue;

            var chosen = PlayerParty.Members[index];
            entered.Add(place);
            SendIn(place, chosen);
            Say($"Go! {chosen.DisplayName}!").With(new Entered(place.Place, chosen, FromBall: true));
        }

        RunEntryEffects(entered);
    }

    /// <summary>The next Pokémon of a place's roster that can fight and isn't already out.</summary>
    private static Pokemon? NextFromRoster(Battler place, IEnumerable<Pokemon?> exclude)
    {
        var used = exclude.Where(p => p != null).ToHashSet();
        return place.Roster?.Members.FirstOrDefault(p => !p.IsFainted && !used.Contains(p));
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

    // ---------------------------------------------------------------- the end of the turn

    /// <summary>Leftovers, poison and burns, Speed Boost and the like, fastest Pokémon first.</summary>
    private void EndOfTurn()
    {
        foreach (var b in AllBattlers.Where(b => b.IsActive).OrderByDescending(EffectiveSpeed).ToList())
        {
            if (!b.IsActive) continue;
            var p = b.Pokemon!;
            HeldItemEffects.For(p.HeldItem)?.AtEndOfTurn(this, b);

            switch (p.Status)
            {
                case StatusCondition.Burn:
                    LoseHp(b, Math.Max(1, p.MaxHP / Rules.BurnDamageDivisor), $"{b.Name} is hurt by its burn!");
                    break;
                case StatusCondition.Poison:
                    LoseHp(b, Math.Max(1, p.MaxHP / 8), $"{b.Name} is hurt by poison!");
                    break;
                case StatusCondition.Toxic:
                    p.ToxicCounter = Math.Min(15, p.ToxicCounter + 1);
                    LoseHp(b, Math.Max(1, p.MaxHP * p.ToxicCounter / 16), $"{b.Name} is hurt by poison!");
                    break;
            }

            if (b.IsActive) p.Ability?.Effect?.AtEndOfTurn(this, b);
        }
    }

    // ---------------------------------------------------------------- fainting, EXP and the end of the battle

    /// <summary>Every Pokémon down to 0 HP faints (with EXP for the player's), then the battle ends if a side has nobody left.</summary>
    private void ResolveFaints()
    {
        while (Result == BattleResult.None)
        {
            var down = AllBattlers.FirstOrDefault(b => b.Pokemon != null && b.Pokemon.CurrentHP <= 0 && b.Pokemon.Status != StatusCondition.Faint);
            if (down == null) break;

            down.Pokemon!.Status = StatusCondition.Faint;
            down.Pokemon.CurrentHP = 0;
            down.ClearVolatile();
            NoteFaint(down);
            Say($"{down.Name} fainted!").With(new Fainted(down.Place));
            if (!down.IsPlayerSide) AwardExp(down);
        }
        if (Result == BattleResult.None) CheckBattleEnd();
    }

    /// <summary>
    /// What a faint leaves behind outside the battle: the player's Pokémon likes its trainer a little less (a lot
    /// less against a foe thirty levels above it), and a foe going down counts for the Pokémon that were facing
    /// it, for the evolutions that count knock-outs.
    /// </summary>
    private void NoteFaint(Battler down)
    {
        if (down.IsPlayerSide)
        {
            int strongest = EnemySlots.Where(b => b.Pokemon != null).Select(b => b.Pokemon!.Level).DefaultIfEmpty(0).Max();
            FriendshipRules.Apply(down.Pokemon!, strongest - down.Pokemon!.Level >= 30 ? FriendshipEvent.FaintToStronger : FriendshipEvent.Faint);
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
        End(BattleResult.PlayerVictory);
    }

    private void End(BattleResult result)
    {
        Result = result;
        Emit(new Ended(result));
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

        var (share, shared) = Formulas.ExpShares(foe.Species.BaseExpYield, foe.Level, fought.Count, holders.Count);
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
