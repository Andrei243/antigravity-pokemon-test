using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>The five athletic stats of the Pokéathlon (plan 15 · E6), in HeartGold's order.</summary>
public enum AthleteStat { Speed, Power, Skill, Stamina, Jump }

/// <summary>
/// A species' athletic stats, one to five each (plan 15 · E6; plan 15's decision 6: our own formula, the same on every
/// machine, with no table from the games). Speed is its base Speed; Power its stronger attacking stat with a third of
/// the other; Stamina its HP and both defences; Jump its build (light and small jumps, heavy doesn't, wings and fins
/// lift or hold it down); Skill its Special stats and Speed together, with hands for it and no limbs against it.
/// </summary>
public readonly record struct AthleteStats(int Speed, int Power, int Skill, int Stamina, int Jump)
{
    public const int Least = 1, Most = 5;

    public int this[AthleteStat stat] => stat switch
    {
        AthleteStat.Speed => Speed,
        AthleteStat.Power => Power,
        AthleteStat.Skill => Skill,
        AthleteStat.Stamina => Stamina,
        _ => Jump
    };

    /// <summary>The five, in their order.</summary>
    public IEnumerable<int> All => new[] { Speed, Power, Skill, Stamina, Jump };

    public int Sum => Speed + Power + Skill + Stamina + Jump;

    /// <summary>A species' stats, by the formula above (its values as the rules in force have them).</summary>
    public static AthleteStats Of(PokemonSpecies s)
    {
        int strong = Math.Max(s.BaseAttack, s.BaseSpAttack), weak = Math.Min(s.BaseAttack, s.BaseSpAttack);
        int speed = Band(s.BaseSpeed, 45, 65, 85, 105);
        int power = Band((2 * strong + weak) / 3, 55, 75, 95, 115);
        int stamina = Band((s.BaseHP + s.BaseDefense + s.BaseSpDefense) / 3, 55, 70, 85, 100);

        bool flies = s.PrimaryType == PokemonType.Flying || s.SecondaryType == PokemonType.Flying
            || s.Shape is "Wings" or "Bug Wings" || s.Abilities.Contains("Levitate");
        int jump = 3;
        if (s.Weight < 10f) jump++;
        if (s.Weight >= 100f) jump--;
        if (s.Weight >= 300f) jump--;
        if (s.Height >= 3f) jump--;
        if (flies) jump++;
        else if (s.Shape is "Fish" or "Squiggle" or "Blob") jump--;
        if (s.Shape is "Legs" or "Quadruped" && s.BaseSpeed >= 90) jump++;

        int skill = Band((s.BaseSpAttack + s.BaseSpDefense + s.BaseSpeed) / 3, 50, 65, 80, 95);
        if (s.Shape is "Humanoid" or "Arms") skill++;
        else if (s.Shape is "Ball" or "Fish" or "Blob" or "Squiggle" or "Heads") skill--;

        return new AthleteStats(speed, power, Clamp(skill), stamina, Clamp(jump));
    }

    /// <summary>A Pokémon's stats: its own form's (a form is a species of its own to the formula).</summary>
    public static AthleteStats Of(Pokemon pokemon) => Of(SpeciesOf(pokemon));

    /// <summary>The species as the Pokémon stands: its form's values where it is in one.</summary>
    public static PokemonSpecies SpeciesOf(Pokemon pokemon) =>
        pokemon.Form is { } f && pokemon.Species.Form(f) is { } form ? pokemon.Species.AsForm(form) : pokemon.Species;

    private static int Band(int value, int a, int b, int c, int d) => value < a ? 1 : value < b ? 2 : value < c ? 3 : value < d ? 4 : 5;
    private static int Clamp(int v) => Math.Clamp(v, Least, Most);
}

/// <summary>
/// What the player does in one frame of an event (plan 15 · E6): the button pressed this frame (<see cref="Tap"/>) or
/// kept down (<see cref="Held"/>), the arrows held (<see cref="Dx"/>, <see cref="Dy"/>, each -1, 0 or 1), and a change of
/// athlete asked for (<see cref="Switch"/>, -1 or 1). A rival team's input is its own mind's, frame by frame.
/// </summary>
public readonly record struct AthlonInput(bool Tap = false, bool Held = false, int Dx = 0, int Dy = 0, int Switch = 0)
{
    public static readonly AthlonInput None = new();
}

/// <summary>One of a team's three: the species and its stats.</summary>
public sealed record Athlete(string Species, AthleteStats Stats)
{
    public static Athlete Of(PokemonSpecies species) => new(species.Name, AthleteStats.Of(species));
}

/// <summary>
/// A team of an event: the player's (<see cref="IsPlayer"/>, the first of the four) or a rival's, by its name's key
/// (<c>athlon.rivals.*</c>), with three athletes and, for a rival, how well it plays (<see cref="Knack"/>, 0 to 1).
/// </summary>
public sealed record AthlonTeam(string Name, bool IsPlayer, IReadOnlyList<Athlete> Athletes, float Knack);

/// <summary>The events of the Pokéathlon this game has (plan 15 · E6: one of each kind; E7 and E8 add at the end).</summary>
public enum AthlonEventKind { HurdleDash, BlockSmash, RingDrop, RelayRun, CirclePush, GoalRoll }

/// <summary>A course: three events in order, by its text key (<c>athlon.course.&lt;key&gt;</c>).</summary>
public sealed record AthlonCourse(string Key, IReadOnlyList<AthlonEventKind> Events);

/// <summary>
/// The courses (plan 15 · E6, E8): the Practice Course of E6's three events first, then HeartGold's five as it pairs them,
/// from <c>Data/athlon.json</c> (<see cref="AthlonData"/>). A course naming an event this game hasn't built is left out
/// until it is (<see cref="AthlonData.Waiting"/>).
/// </summary>
public static class AthlonCourses
{
    public static readonly AthlonCourse Practice = new("practice", new[] { AthlonEventKind.HurdleDash, AthlonEventKind.BlockSmash, AthlonEventKind.RingDrop });

    public static IReadOnlyList<AthlonCourse> All { get; } = new[] { Practice }.Concat(AthlonData.Courses).ToArray();

    public static AthlonCourse? Get(string key) => All.FirstOrDefault(c => c.Key == key);
}

/// <summary>
/// What the save keeps of the Pokéathlon (plan 15 · E6; <c>SaveData.Athlon</c>, null in older saves): the Athlete
/// Points (HeartGold's <c>athletePoints</c>, at most 99,999), each course's best score, the courses run and won.
/// </summary>
public sealed class AthlonRecords
{
    /// <summary>HeartGold's cap on the points (<c>PokeathlonSave_AddAthletePoints</c>).</summary>
    public const int MostPoints = 99999;

    public int Points { get; set; }
    public Dictionary<string, int> Best { get; set; } = new();
    public int Runs { get; set; }
    public int Wins { get; set; }

    public int BestOf(string course) => Best.TryGetValue(course, out int b) ? b : 0;

    public void Earn(int points) => Points = Math.Clamp(Points + Math.Max(0, points), 0, MostPoints);

    /// <summary>Points spent at the dome's shop (plan 15 · E8): false, and nothing taken, where they don't reach.</summary>
    public bool Spend(int points)
    {
        if (points < 0 || points > Points) return false;
        Points -= points;
        return true;
    }

    /// <summary>A course run to its end: its points earned, its best kept. True when the score is a new best.</summary>
    public bool Record(string course, int score, int place)
    {
        Runs++;
        if (place == 1) Wins++;
        Earn(score);
        Award(course, AthlonData.MedalFor(score));
        if (score <= BestOf(course)) return false;
        Best[course] = score;
        return true;
    }

    /// <summary>Each course's best medal (plan 15 · E8), by its key; none where no medal was won.</summary>
    public Dictionary<string, AthlonMedal> Medals { get; set; } = new();

    public AthlonMedal MedalOf(string course) => Medals.TryGetValue(course, out var m) ? m : AthlonMedal.None;

    /// <summary>A course's medal kept where it is better than the one before (plan 15 · E8).</summary>
    public void Award(string course, AthlonMedal medal)
    {
        if (medal > MedalOf(course)) Medals[course] = medal;
    }

    public AthlonRecords Copy() => new() { Points = Points, Best = new Dictionary<string, int>(Best), Runs = Runs, Wins = Wins, Medals = new Dictionary<string, AthlonMedal>(Medals) };
}

/// <summary>
/// A course under way (plan 15 · E6): the player's team and three rivals drawn on the run's own generator, the events
/// one after another, each event's points for each team, and the scoreboard. Every draw is on the generator handed
/// over, in this order: the rivals as the run is made, then an event's seed as each begins.
/// </summary>
public sealed class AthlonRun
{
    public const int TeamCount = 4, TeamSize = 3;

    /// <summary>The rivals' names, keys of <c>athlon.rivals</c>; three are drawn for a run.</summary>
    public static readonly string[] RivalNames = { "ace", "bolt", "comet", "dash", "ember", "flint", "gale", "hazel" };

    private readonly Random rng;
    private readonly int[,] points;

    public AthlonCourse Course { get; }
    public IReadOnlyList<AthlonTeam> Teams { get; }
    public int EventIndex { get; private set; }
    public AthlonEvent? Current { get; private set; }
    public bool IsOver => EventIndex >= Course.Events.Count;

    public AthlonRun(AthlonCourse course, IReadOnlyList<PokemonSpecies> mine, Random rng)
    {
        if (mine.Count != TeamSize) throw new ArgumentException("A team of the Pokéathlon is three.", nameof(mine));
        Course = course;
        this.rng = rng;
        var teams = new List<AthlonTeam> { new("", true, mine.Select(Athlete.Of).ToList(), 1f) };
        teams.AddRange(Rivals(rng));
        Teams = teams;
        points = new int[course.Events.Count, TeamCount];
    }

    /// <summary>
    /// The three rival teams: a name each, three different species each from Johto's Pokédex (no legendary or
    /// mythical Pokémon), and a knack between 0.45 and 0.85.
    /// </summary>
    public static IReadOnlyList<AthlonTeam> Rivals(Random rng)
    {
        var pool = Pokedex.Entries("updated-johto").Select(e => e.Species).Where(s => !s.Legendary && !s.Mythical).ToList();
        if (pool.Count < TeamSize) pool = PokemonDatabase.GetAll().Where(s => !s.Legendary && !s.Mythical).OrderBy(s => s.DexNumber).ToList();
        var names = RivalNames.ToList();
        var rivals = new List<AthlonTeam>();
        for (int t = 1; t < TeamCount; t++)
        {
            int n = rng.Next(names.Count);
            string name = names[n];
            names.RemoveAt(n);
            var three = new List<Athlete>();
            while (three.Count < TeamSize)
            {
                var s = pool[rng.Next(pool.Count)];
                if (three.All(a => a.Species != s.Name)) three.Add(Athlete.Of(s));
            }
            float knack = 0.45f + (float)rng.NextDouble() * 0.4f;
            rivals.Add(new AthlonTeam(name, false, three, knack));
        }
        return rivals;
    }

    /// <summary>Whether a team has three that may go in (no Eggs): what the dome's clerk asks first.</summary>
    public static bool CanEnter(Party team) => team.Members.Count(p => !p.IsEgg) >= TeamSize;

    /// <summary>The next event begins, on a seed drawn now.</summary>
    public AthlonEvent StartEvent()
    {
        if (IsOver) throw new InvalidOperationException("The course is over.");
        Current = AthlonEvent.Create(Course.Events[EventIndex], Teams, rng.Next());
        return Current;
    }

    /// <summary>The event under way is over: its points are written down and the course goes on to the next.</summary>
    public void EndEvent()
    {
        if (Current == null) return;
        for (int t = 0; t < TeamCount; t++) points[EventIndex, t] = Current.Points(t);
        EventIndex++;
        Current = null;
    }

    /// <summary>A team's points in an event that has been played.</summary>
    public int PointsOf(int eventIndex, int team) => points[eventIndex, team];

    public int Total(int team)
    {
        int sum = 0;
        for (int e = 0; e < Math.Min(EventIndex, Course.Events.Count); e++) sum += points[e, team];
        return sum;
    }

    /// <summary>The teams best first; a tie goes to the player, then to the team named first.</summary>
    public IReadOnlyList<int> Ranking => Enumerable.Range(0, TeamCount).OrderByDescending(Total).ThenBy(t => t).ToList();

    /// <summary>The player's place, 1 to 4.</summary>
    public int PlayerPlace => Ranking.ToList().IndexOf(0) + 1;

    /// <summary>The Athlete Points the course earns: the player's score.</summary>
    public int Earned => Total(0);
}

/// <summary>
/// An event of the Pokéathlon (plan 15 · E6): a game of a fixed rate, thirty frames a second as the original's, of a
/// most number of frames, played by four teams of three. <see cref="Step"/> plays one frame with the player's input;
/// the rivals' come from their own minds (<see cref="Think"/>) on the event's own generator, made from its seed, so an
/// event is a pure function of its seed and the player's inputs. <see cref="Points"/> is a team's points.
/// </summary>
public abstract class AthlonEvent
{
    public const int FramesPerSecond = 30;
    public const float Frame = 1f / FramesPerSecond;

    protected readonly Random Rng;
    private readonly Random autopilot;
    private float owed;
    private bool tapOwed;
    private int switchOwed;

    public AthlonEventKind Kind { get; }
    public IReadOnlyList<AthlonTeam> Teams { get; }
    public int Seed { get; }

    /// <summary>The frames played.</summary>
    public int Frames { get; private set; }

    /// <summary>The most frames the event lasts.</summary>
    public abstract int Length { get; }

    public bool IsDone { get; private set; }

    /// <summary>The stat the event leans on most, which the briefing names.</summary>
    public abstract AthleteStat Leans { get; }

    /// <summary>Seconds left on the event's clock.</summary>
    public float SecondsLeft => Math.Max(0, Length - Frames) * Frame;

    protected AthlonEvent(AthlonEventKind kind, IReadOnlyList<AthlonTeam> teams, int seed)
    {
        if (teams.Count != AthlonRun.TeamCount) throw new ArgumentException("An event is four teams.", nameof(teams));
        Kind = kind;
        Teams = teams;
        Seed = seed;
        Rng = new Random(seed);
        autopilot = new Random(seed ^ 0x5A17);
    }

    public static AthlonEvent Create(AthlonEventKind kind, IReadOnlyList<AthlonTeam> teams, int seed) => kind switch
    {
        AthlonEventKind.HurdleDash => new HurdleDash(teams, seed),
        AthlonEventKind.BlockSmash => new BlockSmash(teams, seed),
        AthlonEventKind.RelayRun => new RelayRun(teams, seed),
        AthlonEventKind.CirclePush => new CirclePush(teams, seed),
        AthlonEventKind.GoalRoll => new GoalRoll(teams, seed),
        _ => new RingDrop(teams, seed)
    };

    /// <summary>One frame, the player's team doing what <paramref name="mine"/> says and the rivals what they think.</summary>
    public void Step(AthlonInput mine)
    {
        if (IsDone) return;
        var inputs = new AthlonInput[AthlonRun.TeamCount];
        inputs[0] = mine;
        for (int t = 1; t < inputs.Length; t++) inputs[t] = Think(t, Teams[t].Knack, Rng);
        Play(inputs);
        Frames++;
        if (Frames >= Length || Finished) IsDone = true;
    }

    /// <summary>
    /// Time passes: as many whole frames as it holds, never past the event's end. A press or a switch given here goes
    /// into the first frame played, and waits for the next call if none is.
    /// </summary>
    public int Advance(float dt, AthlonInput input)
    {
        if (input.Tap) tapOwed = true;
        if (input.Switch != 0) switchOwed = input.Switch;
        owed += Math.Max(0f, dt);
        int played = 0;
        while (owed >= Frame && !IsDone)
        {
            owed -= Frame;
            Step(input with { Tap = tapOwed, Switch = switchOwed });
            tapOwed = false;
            switchOwed = 0;
            played++;
        }
        if (IsDone) owed = 0f;
        return played;
    }

    /// <summary>What the player's own team would do now if it played itself (for tests and the harness).</summary>
    public AthlonInput Autopilot(float knack = 0.8f) => Think(0, knack, autopilot);

    /// <summary>Whether every team is through before the clock (a race).</summary>
    protected virtual bool Finished => false;

    protected abstract void Play(AthlonInput[] inputs);

    /// <summary>A team's input for this frame, by how well it plays, on the generator given.</summary>
    protected abstract AthlonInput Think(int team, float knack, Random rng);

    /// <summary>A team's points in the event (its scoreboard's share).</summary>
    public abstract int Points(int team);

    /// <summary>A number drawn about nought: the sum of two draws, -1 to 1.</summary>
    protected static float Wobble(Random rng) => (float)(rng.NextDouble() + rng.NextDouble()) - 1f;
}
