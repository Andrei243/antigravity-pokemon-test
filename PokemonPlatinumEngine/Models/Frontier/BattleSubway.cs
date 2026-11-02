using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Frontier;

/// <summary>
/// The trains of the Battle Subway (plan 22 · U6, part d): one on one, two on two, and a pair beside a partner; and since
/// plan 22 · U11 the Super train of each (<see cref="BattleSubway.IsSuper"/>), opened by the Subway Boss of its own line
/// beaten at the 21st car, its trainers from the fourth room's rows on and its Boss at the 49th alone.
/// </summary>
public enum SubwayTrain
{
    Single,
    Double,
    Multi,
    SuperSingle,
    SuperDouble,
    SuperMulti
}

/// <summary>
/// The Battle Subway's rules (plan 22 · U6, part d), the smallest honest version of plan 06 · R18's machinery for
/// Black 2's line under Nimbasa City: a run of cars, a trainer in each, and the Frontier's own rules for who may go in
/// (<see cref="FrontierRules"/>) and how a team is made (<see cref="FrontierTrainers"/>), so a Pokémon that goes in is
/// a copy at level 50 and nothing a battle does reaches it. What a run keeps is the story's own variables
/// (<see cref="Vars"/>), so no host and no save holds anything new: the train, the battles won, the Pokémon entered
/// and a seed for the partner's team. The trainers of the Frontier's own table stand in for Black 2's Subway
/// trainers (whom the import doesn't hold), drawn by the rooms of the Battle Tower's rows, and the Subway Bosses
/// stand at the 21st and the 49th battle in every train; a Super train (plan 22 · U11) begins three rooms further on and
/// has its Boss at the 49th alone. What is left (the run's pause, Wi-Fi, the
/// Subway's own records and prints and its Battle Point counter) is listed in plan 22. No drawing or input; every
/// draw is on the sixteen-bit draw the caller hands over.
/// </summary>
public static class BattleSubway
{
    /// <summary>The cars of a set: seven trainers, and after each set a choice to go on or to get off.</summary>
    public const int Cars = 7;

    /// <summary>The level a battle of the Subway, and of a tournament that borrows its rules, is fought at.</summary>
    public const int Level = FrontierRules.Level;

    /// <summary>The battle of the streak that Ingo and Emmet wait at (<c>21</c> and <c>49</c>, as the Tower's Tycoon does in its Single Battle Room).</summary>
    public const int IngoAt = 21, EmmetAt = 49;

    /// <summary>The Subway Bosses' own teams, which the data keeps beside the trainers of our own (<c>Data/our-trainers.json</c>).</summary>
    public const string IngoId = "subway_ingo", EmmetId = "subway_emmet";

    /// <summary>The story's variables a run keeps its state in.</summary>
    public static class Vars
    {
        /// <summary>0 while no run is under way, 1 while one is.</summary>
        public const string State = "VAR_UNOVA_SUBWAY_STATE";
        /// <summary>The train a run is on, as <see cref="SubwayTrain"/>.</summary>
        public const string Train = "VAR_UNOVA_SUBWAY_TRAIN";
        /// <summary>The battles the run has won, which is its streak.</summary>
        public const string Wins = "VAR_UNOVA_SUBWAY_WINS";
        /// <summary>The seed of the partner's team in the Multi Train: the same two Pokémon every battle of a run.</summary>
        public const string Seed = "VAR_UNOVA_SUBWAY_SEED";
        /// <summary>The places in the team of the Pokémon entered, one variable for each (a place plus one; 0 for none).</summary>
        public static string Slot(int n) => $"VAR_UNOVA_SUBWAY_SLOT_{n}";
        /// <summary>The longest streak of each train.</summary>
        public static string Best(SubwayTrain train) => $"VAR_UNOVA_SUBWAY_BEST_{train.ToString().ToUpperInvariant()}";
    }

    /// <summary>Whether the train is one of the Super lines (plan 22 · U11).</summary>
    public static bool IsSuper(SubwayTrain train) => train >= SubwayTrain.SuperSingle;

    /// <summary>The line a train runs on: a Super train's own line, or the train itself.</summary>
    public static SubwayTrain LineOf(SubwayTrain train) => IsSuper(train) ? (SubwayTrain)((int)train - 3) : train;

    /// <summary>The Super train of a line.</summary>
    public static SubwayTrain SuperOf(SubwayTrain train) => IsSuper(train) ? train : (SubwayTrain)((int)train + 3);

    /// <summary>
    /// Whether a line's Super train is open: once its Boss at the 21st car has been beaten, which is a streak of 21 or
    /// more kept as the line's best (the Boss's car counts as won only when the Boss is beaten).
    /// </summary>
    public static bool SuperOpen(Story.StoryState story, SubwayTrain train) => story.Var(Vars.Best(LineOf(train))) >= IngoAt;

    /// <summary>The rooms a Super train's trainers begin past: its first car is fought as a line's 22nd.</summary>
    public const int SuperRooms = 3;

    /// <summary>How many Pokémon a train takes: three, four in the Double Train, two beside a partner.</summary>
    public static int PartySize(SubwayTrain train) => LineOf(train) switch
    {
        SubwayTrain.Double => 4,
        SubwayTrain.Multi => 2,
        _ => 3
    };

    /// <summary>Whether the battles are fought two at a time.</summary>
    public static bool Doubles(SubwayTrain train) => LineOf(train) != SubwayTrain.Single;

    /// <summary>Whether the train is fought beside a partner.</summary>
    public static bool Multi(SubwayTrain train) => LineOf(train) == SubwayTrain.Multi;

    /// <summary>Which Subway Boss waits at the battle of that number: 0 for none, 1 for Ingo and 2 for Emmet.</summary>
    public static int BossAt(int battle) => battle == IngoAt ? 1 : battle == EmmetAt ? 2 : 0;

    /// <summary>
    /// Which Subway Boss waits at the battle of that number on a train: the lines' as <see cref="BossAt(int)"/>; on a
    /// Super train the 49th car alone, Ingo on the Super Single Train and Emmet on the Super Double (both together on
    /// the Super Multi, which <see cref="Bosses"/> stands side by side).
    /// </summary>
    public static int BossAt(int battle, SubwayTrain train) => !IsSuper(train) ? BossAt(battle)
        : battle != EmmetAt ? 0
        : LineOf(train) == SubwayTrain.Double ? 2 : 1;

    /// <summary>The Pokémon put to use on the team, in the order they were entered, from the variables the run keeps them in.</summary>
    public static List<int> SlotsOf(Story.StoryState story, SubwayTrain train)
    {
        var slots = new List<int>();
        for (int n = 0; n < PartySize(train); n++)
            if (story.Var(Vars.Slot(n)) > 0) slots.Add(story.Var(Vars.Slot(n)) - 1);
        return slots;
    }

    /// <summary>Keeps the Pokémon entered in the story's variables.</summary>
    public static void KeepSlots(Story.StoryState story, IReadOnlyList<int> slots)
    {
        for (int n = 0; n < 4; n++) story.SetVar(Vars.Slot(n), n < slots.Count ? slots[n] + 1 : 0);
    }

    /// <summary>
    /// The Frontier's trainer for the battle of that number, as <see cref="BattleTowerChallenge.TrainerFor"/> draws one:
    /// the row of the room the battle is in (seven battles to a room), the seventh of each from the row of bosses,
    /// every room from the eighth on from the last row.
    /// </summary>
    public static int TrainerFor(int battle, Func<int> next16)
    {
        int room = ((battle - 1) / Cars) & 0xFF, place = (battle - 1) % Cars;
        int[] row = room >= 7 ? FrontierDatabase.TowerRooms[7]
            : place == Cars - 1 ? FrontierDatabase.TowerBossRooms[room] : FrontierDatabase.TowerRooms[room];
        return row[0] + (next16() & 0xFFFF) % (row[1] - row[0] + 1);
    }

    /// <summary>
    /// The trainers of the battle of that number and the teams made for them: one trainer with the train's number of
    /// Pokémon, or in the Multi Train two trainers with two each, the second's kept clear of the first's.
    /// </summary>
    public static List<FrontierOpponent> MakeOpponents(SubwayTrain train, int battle, Func<int> next16)
    {
        // A Super train's first car is fought as its line's 22nd: three rooms of trainers further on
        int at = IsSuper(train) ? battle + SuperRooms * Cars : battle;
        int first = TrainerFor(at, next16), second = first;
        if (Multi(train))
            while ((second = TrainerFor(at, next16)) == first) { }
        return FrontierTrainers.ForTrainers(PartySize(train) * (Multi(train) ? 2 : 1), first, second, Multi(train), next16);
    }

    /// <summary>
    /// The partner of the Multi Train: a trainer of the first room's row with two Pokémon, the same every battle of
    /// a run because the draw is on the run's own seed.
    /// </summary>
    public static FrontierOpponent MakePartner(int seed)
    {
        var rng = new Random(seed);
        int Next16() => rng.Next(0x10000);
        int trainer = TrainerFor(1, Next16);
        return FrontierTrainers.ForTrainers(PartySize(SubwayTrain.Multi), trainer, trainer, false, Next16)[0];
    }

    /// <summary>
    /// A Subway Boss (<paramref name="which"/> 1 for Ingo, 2 for Emmet) as a trainer of the battle: their own team of
    /// our data, at level 50, cut to the train's two where they stand side by side.
    /// </summary>
    public static Trainer Boss(int which, SubwayTrain train)
    {
        var record = TrainerDatabase.Get(which == 1 ? IngoId : EmmetId)
            ?? throw new InvalidOperationException($"No Subway Boss {which}");
        var trainer = new Trainer
        {
            Id = record.Id,
            Name = record.Name,
            TrainerClass = record.Class,
            PrizeMoney = 0,
            Ai = FrontierTrainers.Mind,
            Look = CharacterStyles.Default.CharacterFor(which == 1 ? "ingo" : "emmet")
        };
        int count = Multi(train) ? PartySize(train) : record.Party.Count;
        foreach (var m in record.Party.Take(count)) trainer.Party.Add(TrainerDatabase.Build(m));
        return trainer;
    }

    /// <summary>The bosses who stand at the battle: Ingo, or Emmet, or in the Multi Train the two together.</summary>
    public static List<Trainer> Bosses(SubwayTrain train, int battle)
    {
        int which = BossAt(battle, train);
        if (which == 0) return new List<Trainer>();
        return Multi(train)
            ? new List<Trainer> { Boss(1, train), Boss(2, train) }
            : new List<Trainer> { Boss(which, train) };
    }

    /// <summary>The Battle Points a run earns on getting off: three a set, one more for each set before it, up to ten.</summary>
    public static int PointsFor(int wins)
    {
        int points = 0;
        for (int set = 0; set < wins / Cars; set++) points += Math.Min(10, 3 + set);
        return points;
    }

    /// <summary>The Battle Points of a run on a train: a Super train's sets are paid as its line's from the fourth set on (six for its first, up to ten).</summary>
    public static int PointsFor(int wins, SubwayTrain train)
    {
        if (!IsSuper(train)) return PointsFor(wins);
        int points = 0;
        for (int set = 0; set < wins / Cars; set++) points += Math.Min(10, 3 + SuperRooms + set);
        return points;
    }
}
