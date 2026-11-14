using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Rides;

/// <summary>What lies across a track: a rock to jump, a stretch of mud to push through, or a turn to lean into.</summary>
public enum RaceObstacleKind { Rock, Mud, Turn }

/// <summary>One obstacle of a track: where it starts along it, how long it is (mud, and a turn's window) and a turn's side.</summary>
public sealed class RaceObstacle
{
    [DataRequired] public RaceObstacleKind Kind { get; set; }
    /// <summary>Where it starts, in the track's units (a rock's place, a mud stretch's start, a turn's middle).</summary>
    [DataRequired] public int At { get; set; }
    /// <summary>A mud stretch's length; a rock and a turn have none.</summary>
    public int Length { get; set; }
    /// <summary>A turn's side: -1 left, 1 right.</summary>
    public int Dir { get; set; }
}

/// <summary>One of the stable's riders on a track: their name (<c>rides.rider.&lt;name&gt;</c>), their mount's species and how well they ride, 0 to 1.</summary>
public sealed class RaceRider
{
    [DataRequired] public string Name { get; set; } = "";
    [DataRequired] public string Mount { get; set; } = "";
    [DataRequired] public float Knack { get; set; }
}

/// <summary>A track of the riding race (<c>Data/race-tracks.json</c>, ours): a stable's course of rocks, mud and turns.</summary>
public sealed class RaceTrack
{
    [DataRequired] public string Id { get; set; } = "";
    /// <summary>The area the stable stands in.</summary>
    [DataRequired] public string Stable { get; set; } = "";
    /// <summary>The track's length in units.</summary>
    [DataRequired] public int Length { get; set; }
    /// <summary>The most seconds a race lasts: the riders not home by then are placed by how far they got.</summary>
    [DataRequired] public int Seconds { get; set; }
    [DataRequired] public List<RaceObstacle> Obstacles { get; set; } = new();
    [DataRequired] public List<RaceRider> Riders { get; set; } = new();
    /// <summary>The Battle Points for first, second, third and fourth place.</summary>
    [DataRequired] public List<int> Pay { get; set; } = new();

    public int PayFor(int place) => place >= 1 && place <= Pay.Count ? Pay[place - 1] : 0;
}

public sealed class RaceTracksFile
{
    [DataRequired] public List<RaceTrack> Tracks { get; set; } = new();
}

/// <summary>The tracks of the riding race (<c>Data/race-tracks.json</c>, written by hand, ours), read strictly once.</summary>
public static class RaceTracks
{
    public const string FileName = "race-tracks.json";

    private static readonly Lazy<RaceTracksFile> file = new(() => GameDataFiles.Load<RaceTracksFile>(FileName, strict: true));

    public static IReadOnlyList<RaceTrack> All => file.Value.Tracks;

    public static RaceTrack? Get(string id) => All.FirstOrDefault(t => t.Id == id);

    /// <summary>The script's counter for a race: the screen is told a track by <c>race:&lt;id&gt;</c>.</summary>
    public const string Prefix = "race:";

    /// <summary>The key a track's best and runs are kept under in <see cref="RideRecords"/>.</summary>
    public static string KeyOf(RaceTrack track) => Prefix + track.Id;

    /// <summary>The four riders: the player's mount and the stable's three, each a team of one.</summary>
    public static IReadOnlyList<AthlonTeam> Teams(RaceTrack track, Athlete mine)
    {
        var teams = new List<AthlonTeam> { new("", true, new[] { mine }, 0.8f) };
        for (int i = 0; i < AthlonRun.TeamCount - 1; i++)
        {
            var rider = i < track.Riders.Count ? track.Riders[i] : new RaceRider { Name = "adele", Mount = "Rhyhorn", Knack = 0.6f };
            var species = PokemonDatabase.Get(rider.Mount);
            var athlete = species != null ? Athlete.Of(species) : new Athlete(rider.Mount, new AthleteStats(3, 3, 3, 3, 3));
            teams.Add(new AthlonTeam(rider.Name, false, new[] { athlete }, rider.Knack));
        }
        return teams;
    }
}

/// <summary>
/// A riding race (plan 42 · PS8; ours, no game had one to play, rulings "The riding race"; style guide, "The rides"): an
/// <see cref="AthlonEvent"/> of kind <see cref="AthlonEventKind.Race"/>, which no course of the dome names. Four riders
/// run a track of rocks, mud and turns: the button held pushes (a boost of a fifth more speed that spends Stamina, which
/// comes back as it is let go), a press jumps (the air lasts longer for Jump) and an arrow held through a turn's window
/// leans into it. A rock crossed on the ground is a stumble (speed to a third, the rider stunned for frames fewer by
/// Skill), mud holds a rider to a share of its top speed raised by Power (pushing lifts it a little at a double cost), and
/// a turn not leant into costs a share of the speed lost by Skill. Speed sets the top speed. The first home wins; riders
/// not home when the clock runs out are placed by how far they got. Pure: a function of its seed and the player's inputs.
/// </summary>
public sealed class RidingRace : AthlonEvent
{
    /// <summary>The height and weight a mount may be: the size of a Pokémon one could ride.</summary>
    public const float LeastHeight = 0.9f, MostHeight = 3.2f, LeastWeight = 20f, MostWeight = 450f;

    /// <summary>A turn's window either side of its middle, in units.</summary>
    public const int TurnReach = 40;

    /// <summary>A rock's width, in units.</summary>
    public const int RockReach = 14;

    /// <summary>Whether a species is of a riding size.</summary>
    public static bool CanCarry(PokemonSpecies species) =>
        species.Height >= LeastHeight && species.Height <= MostHeight && species.Weight >= LeastWeight && species.Weight <= MostWeight;

    /// <summary>Whether a Pokémon may be the player's mount: of a riding size, no Egg, able to fight.</summary>
    public static bool CanCarry(Pokemon pokemon) => !pokemon.IsEgg && !pokemon.IsFainted && CanCarry(AthleteStats.SpeciesOf(pokemon));

    /// <summary>One rider's state.</summary>
    public sealed class Racer
    {
        public float Distance, Speed, Stamina = 1f;
        public int Air, AirLength, Stun;
        public bool Boosting;
        public int Rocks, Stumbles, Cleared, TurnsLeant, TurnsMissed;
        public int FinishFrame = -1;
        public int LastStumbleFrame = -100, LastClearFrame = -100, LastTurnFrame = -100;
        public bool LastTurnLeant;
        /// <summary>Per obstacle (by index): a turn's arrow was held in its window.</summary>
        public bool[] Leant = Array.Empty<bool>();
        public bool[] Done = Array.Empty<bool>();
        // A rival's plan for the rock ahead
        public int PlanRock = -1;
        public float PlanLead;
        public bool PlanLean;
        public int PlanTurn = -1;
        public bool Finished => FinishFrame >= 0;
    }

    private readonly Racer[] racers = new Racer[AthlonRun.TeamCount];

    public RaceTrack Track { get; }

    public RidingRace(IReadOnlyList<AthlonTeam> teams, int seed) : this(RaceTracks.All.FirstOrDefault() ?? Fallback, teams, seed) { }

    public RidingRace(RaceTrack track, IReadOnlyList<AthlonTeam> teams, int seed) : base(AthlonEventKind.Race, teams, seed)
    {
        Track = track;
        for (int t = 0; t < racers.Length; t++)
            racers[t] = new Racer { Leant = new bool[track.Obstacles.Count], Done = new bool[track.Obstacles.Count] };
    }

    private static readonly RaceTrack Fallback = new() { Id = "blossom", Stable = "kalos_route_12", Length = 1800, Seconds = 60 };

    public override int Length => Math.Max(1, Track.Seconds) * FramesPerSecond;
    public override AthleteStat Leans => AthleteStat.Speed;
    public IReadOnlyList<Racer> Racers => racers;
    protected override bool Finished => racers.All(r => r.Finished);

    public AthleteStats StatsOf(int team) => Teams[team].Athletes.Count > 0 ? Teams[team].Athletes[0].Stats : new AthleteStats(3, 3, 3, 3, 3);

    public static float TopSpeed(AthleteStats s) => 2f + 0.3f * s.Speed;
    public const float Accel = 0.05f, BoostShare = 1.2f;
    public static int AirFrames(AthleteStats s) => 12 + 2 * s.Jump;
    public static float MudShare(AthleteStats s) => 0.4f + 0.1f * s.Power;
    public static int StunFrames(AthleteStats s) => Math.Max(4, 16 - s.Skill);
    public static float TurnKeeps(AthleteStats s) => Math.Min(0.8f, 0.55f + 0.05f * s.Skill);
    public static float BoostDrain(AthleteStats s) => 1f / (60f + 20f * s.Stamina);
    public const float StaminaBack = 1f / 150f;

    protected override void Play(AthlonInput[] inputs)
    {
        for (int t = 0; t < racers.Length; t++)
            if (!racers[t].Finished) Run(racers[t], StatsOf(t), inputs[t]);
    }

    private void Run(Racer r, AthleteStats s, AthlonInput input)
    {
        bool stunned = r.Stun > 0;
        if (stunned) r.Stun--;
        float top = TopSpeed(s);

        // A jump, from the ground, not stunned
        if (input.Tap && r.Air == 0 && !stunned)
        {
            r.AirLength = AirFrames(s);
            r.Air = r.AirLength;
        }

        // The push: the button held, Stamina left, not stunned and not in the air
        bool inMud = false;
        foreach (var o in Track.Obstacles)
            if (o.Kind == RaceObstacleKind.Mud && r.Distance >= o.At && r.Distance < o.At + o.Length) { inMud = true; break; }
        r.Boosting = input.Held && r.Stamina > 0.02f && !stunned && r.Air == 0;
        float drain = BoostDrain(s) * (inMud ? 2f : 1f);
        r.Stamina = r.Boosting ? Math.Max(0f, r.Stamina - drain) : Math.Min(1f, r.Stamina + StaminaBack);

        float cap = top * (r.Boosting ? BoostShare : 1f);
        if (inMud && r.Air == 0) cap = top * (MudShare(s) + (r.Boosting ? 0.15f : 0f));
        if (stunned) cap = Math.Min(cap, top * 0.5f);
        r.Speed = r.Speed < cap ? Math.Min(cap, r.Speed + Accel) : Math.Max(cap, r.Speed - 0.1f);

        float before = r.Distance;
        r.Distance += r.Speed;
        if (r.Air > 0) r.Air--;

        // The obstacles crossed this frame
        for (int i = 0; i < Track.Obstacles.Count; i++)
        {
            var o = Track.Obstacles[i];
            switch (o.Kind)
            {
                case RaceObstacleKind.Rock:
                    if (!r.Done[i] && before < o.At && r.Distance >= o.At)
                    {
                        r.Done[i] = true;
                        r.Rocks++;
                        if (r.Air > 0) { r.Cleared++; r.LastClearFrame = Frames; }
                        else
                        {
                            r.Stumbles++;
                            r.Speed *= 0.33f;
                            r.Stun = StunFrames(s);
                            r.LastStumbleFrame = Frames;
                        }
                    }
                    break;
                case RaceObstacleKind.Turn:
                    if (r.Distance >= o.At - TurnReach && r.Distance <= o.At + TurnReach && input.Dx == o.Dir) r.Leant[i] = true;
                    if (!r.Done[i] && r.Distance > o.At + TurnReach)
                    {
                        r.Done[i] = true;
                        r.LastTurnFrame = Frames;
                        r.LastTurnLeant = r.Leant[i];
                        if (r.Leant[i]) r.TurnsLeant++;
                        else
                        {
                            r.TurnsMissed++;
                            r.Speed *= TurnKeeps(s);
                        }
                    }
                    break;
            }
        }

        if (r.Distance >= Track.Length) r.FinishFrame = Frames + 1;
    }

    protected override AthlonInput Think(int team, float knack, Random rng)
    {
        var r = racers[team];
        var s = StatsOf(team);
        if (r.Finished) return AthlonInput.None;
        bool tap = false;
        int dx = 0;
        float lookAhead = r.Speed * (AirFrames(s) / 2f);
        for (int i = 0; i < Track.Obstacles.Count; i++)
        {
            var o = Track.Obstacles[i];
            if (r.Done[i]) continue;
            if (o.Kind == RaceObstacleKind.Rock)
            {
                if (r.PlanRock != i)
                {
                    // Where before the rock it means to jump: nearer the ideal the better it rides
                    r.PlanRock = i;
                    r.PlanLead = lookAhead + Wobble(rng) * (1f - knack) * 60f;
                }
                if (r.Air == 0 && o.At - r.Distance <= r.PlanLead && o.At - r.Distance > 0) tap = true;
                break;
            }
            if (o.Kind == RaceObstacleKind.Turn)
            {
                if (r.PlanTurn != i)
                {
                    r.PlanTurn = i;
                    r.PlanLean = rng.NextDouble() < 0.35 + 0.65 * knack;
                }
                if (r.PlanLean && r.Distance >= o.At - TurnReach && r.Distance <= o.At + TurnReach) dx = o.Dir;
                if (r.Distance < o.At + TurnReach) break;
            }
        }
        bool push = r.Stamina > (0.15f + (1f - knack) * 0.4f) && r.Air == 0;
        return new AthlonInput(Tap: tap, Held: push, Dx: dx);
    }

    /// <summary>A team's score: for a rider home, 10,000 less its finishing frame; for one still out there, its way along in five thousandths.</summary>
    public override int Points(int team)
    {
        var r = racers[team];
        return r.Finished ? 10000 - r.FinishFrame : (int)(Math.Min(1f, r.Distance / Math.Max(1, Track.Length)) * 5000f);
    }

    /// <summary>The riders' order: the first home first, then those not home by how far they got (ties to the earlier team).</summary>
    public IReadOnlyList<int> Ranking => Enumerable.Range(0, racers.Length)
        .OrderByDescending(t => Points(t)).ThenBy(t => t).ToList();

    /// <summary>The player's place, 1 to 4.</summary>
    public int PlayerPlace => Ranking.ToList().IndexOf(0) + 1;

    /// <summary>A rider's finishing time in seconds, or null where it did not get home.</summary>
    public float? TimeOf(int team) => racers[team].Finished ? racers[team].FinishFrame * Frame : null;
}
