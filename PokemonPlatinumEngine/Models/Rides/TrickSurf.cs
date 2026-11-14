using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Rides;

/// <summary>One of the surf club's riders on a course: their name (<c>rides.club.&lt;name&gt;</c>) and how well they ride, 0 to 1.</summary>
public sealed class SurfClubRider
{
    [DataRequired] public string Name { get; set; } = "";
    [DataRequired] public float Knack { get; set; }
}

/// <summary>A band of a course's pay: a first place whose score reaches <see cref="Score"/> wins <see cref="Points"/> Battle Points.</summary>
public sealed class SurfPay
{
    [DataRequired] public int Score { get; set; }
    [DataRequired] public int Points { get; set; }
}

/// <summary>A course of trick surfing (<c>Data/surf-courses.json</c>, ours): off a beach, ridden by the player and three of the club.</summary>
public sealed class SurfCourse
{
    [DataRequired] public string Id { get; set; } = "";
    /// <summary>The area the course's hut stands in.</summary>
    [DataRequired] public string Beach { get; set; } = "";
    [DataRequired] public int Seconds { get; set; }
    /// <summary>The wave's height as a share of the first course's: every jump's air is lengthened by it.</summary>
    [DataRequired] public float Swell { get; set; } = 1f;
    [DataRequired] public List<SurfClubRider> Club { get; set; } = new();
    [DataRequired] public List<SurfPay> Pay { get; set; } = new();

    /// <summary>What a first place of <paramref name="score"/> pays: the highest band it reached, nothing under the first.</summary>
    public int PayFor(int score) => Pay.Where(p => score >= p.Score).Select(p => p.Points).DefaultIfEmpty(0).Max();
}

public sealed class SurfCoursesFile
{
    [DataRequired] public List<SurfCourse> Courses { get; set; } = new();
}

/// <summary>The courses of trick surfing (<c>Data/surf-courses.json</c>, written by hand, ours), read strictly once.</summary>
public static class SurfCourses
{
    public const string FileName = "surf-courses.json";

    /// <summary>The species the hut lends every rider.</summary>
    public const string Mount = "Mantine";

    private static readonly Lazy<SurfCoursesFile> file = new(() => GameDataFiles.Load<SurfCoursesFile>(FileName, strict: true));

    public static IReadOnlyList<SurfCourse> All => file.Value.Courses;

    public static SurfCourse? Get(string id) => All.FirstOrDefault(c => c.Id == id);

    /// <summary>The four riders of a course: the player and its club, each a team of one on the lent Mantine.</summary>
    public static IReadOnlyList<AthlonTeam> Riders(SurfCourse course)
    {
        var mount = PokemonDatabase.Get(Mount);
        var athlete = mount != null ? Athlete.Of(mount) : new Athlete(Mount, new AthleteStats(3, 3, 3, 3, 3));
        var teams = new List<AthlonTeam> { new("", true, new[] { athlete }, 0.8f) };
        for (int i = 0; i < AthlonRun.TeamCount - 1; i++)
        {
            var club = i < course.Club.Count ? course.Club[i] : new SurfClubRider { Name = "kai", Knack = 0.6f };
            teams.Add(new AthlonTeam(club.Name, false, new[] { athlete }, club.Knack));
        }
        return teams;
    }
}

/// <summary>A trick taken in the air, by the arrow held: left a spin, right a roll, up a flip, down a dive.</summary>
public enum SurfTrick { None, Spin, Roll, Flip, Dive }

/// <summary>How a jump came down: with no trick, straight on the press in time, or crooked (a fall).</summary>
public enum SurfLanding { None, Plain, Straight, Crooked }

/// <summary>
/// Trick surfing (plan 42 · PS7; an idea from Sun and Moon's Mantine Surf, our own numbers and rules, rulings "Trick
/// surfing"; style guide, "The rides"): an <see cref="AthlonEvent"/> of kind <see cref="AthlonEventKind.Surf"/>, which no
/// course of the dome names, played by four riders on the Mantine a beach's hut lends (each team's first athlete). On the
/// wave a rider carves down its face (down held) to gather speed by Stamina toward a top speed by Speed, climbs it (up
/// held) and jumps off its crest on a press; in the air an arrow held takes a trick whose length Skill shortens, and the
/// air lasts longer for speed, Jump and the course's swell. The jump is landed straight by a press within the landing's
/// window before the water (<see cref="TimingAssists"/>, scored against others); a trick not finished, an early press or
/// no press at all after a trick is a crooked landing, which falls: the jump's tricks are lost, the combo ends and the
/// rider is down for frames fewer by Skill. Straight landings in a row make a combo, a quarter more for each to twice. A team's points are its score. Pure: a function of its seed and the player's inputs.
/// </summary>
public sealed class TrickSurf : AthlonEvent
{
    /// <summary>Where on the face a press jumps: from this share of the way up.</summary>
    public const float CrestLine = 0.8f;

    /// <summary>The landing's window before the water, in frames (a quarter of a second), under no assist.</summary>
    public const int LandFrames = 8;

    /// <summary>How a rider stands on the face, in the air or in the water after a fall.</summary>
    public enum Stance { Riding, Air, Down }

    /// <summary>One rider's state.</summary>
    public sealed class Rider
    {
        public Stance Stance;
        /// <summary>The height on the wave's face, 0 the trough, 1 the crest.</summary>
        public float Face = 0.5f;
        public float Speed;
        /// <summary>The ground ridden, for the wave's scroll.</summary>
        public float Distance;
        public int Air, AirLength, Down;
        public SurfTrick Trick;
        public int TrickFrame, TrickLength;
        public readonly List<SurfTrick> JumpTricks = new();
        public bool Pressed, Spoiled;
        public int Score, Combo, BestCombo, Landed, Falls, TricksLanded, Jumps, BestJump;
        public SurfLanding LastLanding;
        public int LastLandingFrame = -100;
        public SurfTrick LastTrick;
        public int LastTrickPoints, LastTrickFrame = -100;
        // A rival's plan: carving or climbing, the tricks it means to take this jump and the frame left it presses at
        public bool Carving = true;
        public int TricksMeant, PressAt = -1;
        public float CarveTo;
        /// <summary>The trick's share done, 0 to 1 (0 with none).</summary>
        public float TrickDone => Trick == SurfTrick.None || TrickLength <= 0 ? 0f : Math.Clamp((float)TrickFrame / TrickLength, 0f, 1f);
    }

    private readonly Rider[] riders = new Rider[AthlonRun.TeamCount];

    public SurfCourse Course { get; }

    /// <summary>The help with the landing's press (plan 12 · Q24), held to Generous: a ride is scored against others.</summary>
    public TimingAssist Assist { get; set; }

    public TrickSurf(IReadOnlyList<AthlonTeam> teams, int seed) : this(SurfCourses.All.FirstOrDefault() ?? Fallback, teams, seed) { }

    public TrickSurf(SurfCourse course, IReadOnlyList<AthlonTeam> teams, int seed) : base(AthlonEventKind.Surf, teams, seed)
    {
        Course = course;
        for (int t = 0; t < riders.Length; t++) riders[t] = new Rider();
    }

    private static readonly SurfCourse Fallback = new() { Id = "hauoli", Beach = "hauoli_city", Seconds = 45, Swell = 1f };

    public override int Length => Math.Max(1, Course.Seconds) * FramesPerSecond;
    public override AthleteStat Leans => AthleteStat.Skill;
    public IReadOnlyList<Rider> Riders => riders;

    public static float TopSpeed(AthleteStats s) => 4f + 0.8f * s.Speed;
    public static float Gather(AthleteStats s) => 0.08f + 0.03f * s.Stamina;
    public const float CarveStep = 0.02f, ClimbStep = 0.018f, DriftStep = 0.008f;

    /// <summary>A jump's air in frames: longer for speed and Jump, lengthened by the swell.</summary>
    public int AirFrames(float speed, AthleteStats s) => (int)MathF.Round((10f + 2f * speed + 2f * s.Jump) * Math.Max(0.5f, Course.Swell));

    public static int TrickFrames(SurfTrick trick, AthleteStats s) => Math.Max(4, trick switch
    {
        SurfTrick.Spin => 12,
        SurfTrick.Roll => 14,
        SurfTrick.Dive => 16,
        SurfTrick.Flip => 18,
        _ => 0
    } - s.Skill);

    public static int TrickPoints(SurfTrick trick) => trick switch
    {
        SurfTrick.Spin => 300,
        SurfTrick.Roll => 350,
        SurfTrick.Dive => 450,
        SurfTrick.Flip => 500,
        _ => 0
    };

    public static int DownFrames(AthleteStats s) => 46 - 4 * s.Skill;

    /// <summary>The landing's window in frames, under the assist as a scored press takes it.</summary>
    public int Window => (int)TimingAssists.Window(LandFrames, TimingAssists.Scored(Assist));

    /// <summary>A combo's multiplier in quarters: one for the first landing, a quarter more for each after it, at most twice the points.</summary>
    public static int ComboQuarters(int combo) => Math.Clamp(3 + combo, 4, 8);

    /// <summary>A trick of the arrow held, left before right and up before down; none with no arrow.</summary>
    public static SurfTrick TrickOf(AthlonInput input) =>
        input.Dx < 0 ? SurfTrick.Spin : input.Dx > 0 ? SurfTrick.Roll : input.Dy < 0 ? SurfTrick.Flip : input.Dy > 0 ? SurfTrick.Dive : SurfTrick.None;

    public static AthlonInput InputFor(SurfTrick trick) => trick switch
    {
        SurfTrick.Spin => new AthlonInput(Dx: -1),
        SurfTrick.Roll => new AthlonInput(Dx: 1),
        SurfTrick.Flip => new AthlonInput(Dy: -1),
        SurfTrick.Dive => new AthlonInput(Dy: 1),
        _ => AthlonInput.None
    };

    public AthleteStats StatsOf(int team) => Teams[team].Athletes.Count > 0 ? Teams[team].Athletes[0].Stats : new AthleteStats(3, 3, 3, 3, 3);

    protected override void Play(AthlonInput[] inputs)
    {
        for (int t = 0; t < riders.Length; t++) Ride(riders[t], StatsOf(t), inputs[t]);
    }

    private void Ride(Rider r, AthleteStats s, AthlonInput input)
    {
        float top = TopSpeed(s);
        switch (r.Stance)
        {
            case Stance.Down:
                r.Distance += 1f;
                if (--r.Down <= 0)
                {
                    r.Stance = Stance.Riding;
                    r.Face = 0.3f;
                    r.Speed = 0f;
                    r.Carving = true;
                }
                return;

            case Stance.Riding:
                if (input.Tap && r.Face >= CrestLine)
                {
                    r.Stance = Stance.Air;
                    r.AirLength = AirFrames(r.Speed, s);
                    r.Air = r.AirLength;
                    r.JumpTricks.Clear();
                    r.Trick = SurfTrick.None;
                    r.Pressed = r.Spoiled = false;
                    r.Jumps++;
                    r.PressAt = -1;
                    r.TricksMeant = 0;
                }
                else if (input.Dy > 0)
                {
                    if (r.Face > 0f) r.Speed = Math.Min(top, r.Speed + Gather(s));
                    r.Face = Math.Max(0f, r.Face - CarveStep);
                }
                else if (input.Dy < 0)
                {
                    r.Face = Math.Min(1f, r.Face + ClimbStep);
                    r.Speed = Math.Max(0f, r.Speed - 0.02f);
                }
                else
                {
                    r.Face = Math.Max(0f, r.Face - DriftStep);
                    r.Speed = Math.Max(0f, r.Speed - 0.03f);
                }
                // In the trough the wave's push is lost
                if (r.Face <= 0f) r.Speed = Math.Max(0f, r.Speed - 0.1f);
                r.Distance += 2f + r.Speed;
                return;

            case Stance.Air:
                r.Distance += 2f + r.Speed;
                // A trick: begun by an arrow while none is under way, then carried through by itself
                if (r.Trick == SurfTrick.None)
                {
                    var wanted = TrickOf(input);
                    if (wanted != SurfTrick.None)
                    {
                        r.Trick = wanted;
                        r.TrickFrame = 0;
                        r.TrickLength = TrickFrames(wanted, s);
                    }
                }
                else if (++r.TrickFrame >= r.TrickLength)
                {
                    r.JumpTricks.Add(r.Trick);
                    r.Trick = SurfTrick.None;
                }
                // The landing's press: within the window, or early and spoiling it
                bool open = r.Air <= Window;
                if (TimingAssists.Pressed(input.Tap, open, TimingAssists.Scored(Assist)))
                {
                    if (open) r.Pressed = true;
                    else if (TimingAssists.EarlyPressCounts(TimingAssists.Scored(Assist))) r.Spoiled = true;
                }
                if (--r.Air <= 0) Land(r, s);
                return;
        }
    }

    private void Land(Rider r, AthleteStats s)
    {
        r.LastLandingFrame = Frames;
        if (r.Trick == SurfTrick.None && r.JumpTricks.Count == 0)
        {
            // Nothing taken: down on the water as it came, the combo left as it stood
            r.LastLanding = SurfLanding.Plain;
            r.Stance = Stance.Riding;
            r.Face = 0.5f;
            r.Carving = true;
            return;
        }
        if (r.Trick != SurfTrick.None || !r.Pressed || r.Spoiled)
        {
            r.LastLanding = SurfLanding.Crooked;
            r.Falls++;
            r.Combo = 0;
            r.Trick = SurfTrick.None;
            r.Stance = Stance.Down;
            r.Down = DownFrames(s);
            r.Speed = 0f;
            return;
        }
        r.LastLanding = SurfLanding.Straight;
        r.Combo++;
        r.BestCombo = Math.Max(r.BestCombo, r.Combo);
        r.Landed++;
        int sum = 0;
        var seen = new HashSet<SurfTrick>();
        foreach (var trick in r.JumpTricks)
        {
            sum += seen.Add(trick) ? TrickPoints(trick) : TrickPoints(trick) / 2;
            r.TricksLanded++;
        }
        int points = sum * ComboQuarters(r.Combo) / 4;
        r.Score += points;
        r.BestJump = Math.Max(r.BestJump, points);
        r.LastTrick = r.JumpTricks[^1];
        r.LastTrickPoints = points;
        r.LastTrickFrame = Frames;
        r.Stance = Stance.Riding;
        r.Face = 0.5f;
        r.Carving = true;
    }

    protected override AthlonInput Think(int team, float knack, Random rng)
    {
        var r = riders[team];
        var s = StatsOf(team);
        switch (r.Stance)
        {
            case Stance.Riding:
                if (r.Carving)
                {
                    if (float.IsNaN(r.CarveTo) || r.CarveTo <= 0f) r.CarveTo = Math.Clamp(0.1f + Wobble(rng) * (1f - knack) * 0.3f, 0.02f, 0.4f);
                    if (r.Face <= r.CarveTo || r.Speed >= TopSpeed(s) * 0.97f)
                    {
                        r.Carving = false;
                        r.CarveTo = 0f;
                    }
                    return new AthlonInput(Dy: 1);
                }
                if (r.Face >= CrestLine)
                {
                    // The jump: how many tricks it means to take, and the frame left it means to press at
                    return new AthlonInput(Tap: true);
                }
                return new AthlonInput(Dy: -1);

            case Stance.Air:
                if (r.Air == r.AirLength)
                {
                    r.TricksMeant = 1 + (rng.NextDouble() < knack ? 1 : 0) + (rng.NextDouble() < knack * 0.4 ? 1 : 0);
                    // (a press drawn at nought or under is never made: the rider is late and lands crooked)
                    r.PressAt = (int)MathF.Round(Window / 2f + Wobble(rng) * (1f - knack) * 32f);
                }
                if (r.Trick == SurfTrick.None && r.TricksMeant > 0)
                {
                    var trick = (SurfTrick)(1 + rng.Next(4));
                    bool fits = r.Air - TrickFrames(trick, s) > Window / 2 + 1;
                    // A careless rider tries a trick that won't fit now and then, and lands it crooked
                    if (fits || rng.NextDouble() < (1f - knack) * 0.25f)
                    {
                        r.TricksMeant--;
                        return InputFor(trick);
                    }
                    r.TricksMeant = 0;
                }
                return r.Air == r.PressAt ? new AthlonInput(Tap: true) : AthlonInput.None;
        }
        return AthlonInput.None;
    }

    public override int Points(int team) => riders[team].Score;

    /// <summary>The riders' order by score, best first (ties to the earlier team).</summary>
    public IReadOnlyList<int> Ranking => Enumerable.Range(0, riders.Length).OrderByDescending(t => riders[t].Score).ThenBy(t => t).ToList();

    /// <summary>The player's place, 1 to 4.</summary>
    public int PlayerPlace => Ranking.ToList().IndexOf(0) + 1;
}

/// <summary>What a course run left: the place, the score, the Battle Points it paid and whether it beat the course's best.</summary>
public readonly record struct RideResult(int Place, int Score, int Paid, bool NewRecord);

/// <summary>What the save keeps of the rides (plan 42 · PS7; <c>SaveData.Rides</c>, null while empty).</summary>
public sealed class RidesSave
{
    public Dictionary<string, int> Best { get; set; } = new();
    public Dictionary<string, int> Runs { get; set; } = new();
    public int PointsWon { get; set; }
}

/// <summary>
/// The rides' records (plan 42 · PS7; GPU-free): each course's best score and runs, and the Battle Points the rides have
/// paid. <see cref="Record"/> is the one rule of a run's pay, which the game and the headless host both call.
/// </summary>
public sealed class RideRecords
{
    private readonly Dictionary<string, int> best = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> runs = new(StringComparer.Ordinal);

    public int PointsWon { get; private set; }

    /// <summary>What the last run recorded paid (not saved): the script's <c>{number}</c> after <c>ride surf</c>.</summary>
    public int LastPaid { get; private set; }

    public int BestOf(string course) => best.TryGetValue(course, out int b) ? b : 0;
    public int RunsOf(string course) => runs.TryGetValue(course, out int n) ? n : 0;

    /// <summary>
    /// A run ended: its place and score kept, and what it pays worked out (a first place pays by its score's band,
    /// anything else nothing). The caller hands the points to the Battle Frontier's records.
    /// </summary>
    public RideResult Record(SurfCourse course, int score, int place) =>
        RecordRun(course.Id, score, place, place == 1 ? course.PayFor(score) : 0);

    /// <summary>A riding race ended (plan 42 · PS8): kept under the track's key, and a place pays what the track gives it.</summary>
    public RideResult RecordRace(RaceTrack track, int score, int place) =>
        RecordRun(RaceTracks.KeyOf(track), score, place, track.PayFor(place));

    private RideResult RecordRun(string key, int score, int place, int paid)
    {
        runs[key] = RunsOf(key) + 1;
        bool newRecord = score > BestOf(key);
        if (newRecord) best[key] = score;
        PointsWon += paid;
        LastPaid = paid;
        return new RideResult(place, score, paid, newRecord);
    }

    public void Clear()
    {
        best.Clear();
        runs.Clear();
        PointsWon = 0;
    }

    public RidesSave? ToSave() => best.Count == 0 && runs.Count == 0 && PointsWon == 0 ? null
        : new RidesSave { Best = new(best), Runs = new(runs), PointsWon = PointsWon };

    public void Load(RidesSave? save)
    {
        Clear();
        if (save == null) return;
        foreach (var (k, v) in save.Best) best[k] = v;
        foreach (var (k, v) in save.Runs) runs[k] = v;
        PointsWon = save.PointsWon;
    }
}
