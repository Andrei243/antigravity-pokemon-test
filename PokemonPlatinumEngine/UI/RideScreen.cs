using System;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Rides;

namespace PokemonPlatinumEngine.UI;

/// <summary>What the rides' screen shows: the course's briefing, the ride, the results and the Battle Points.</summary>
public enum RidePhase { Briefing, Riding, Results, Points }

/// <summary>
/// Trick surfing on screen (plan 42 · PS7; style guide, "The rides"): a beach's hut opens it on the course's briefing; the
/// ride is counted in and played frame by frame at thirty a second (<see cref="TrickSurf"/>); the results rank the four
/// and record the run (<see cref="RideRecords.Record"/>), and the points say what it paid. <see cref="Outcome"/> is the
/// player's place (0 backed out of), <see cref="Result"/> what the run left. Its logic takes no input
/// (<see cref="Confirm"/>, <see cref="Cancel"/>, <see cref="Advance"/>, <see cref="Play"/>), so tests and the harness
/// drive it; <see cref="Update"/> reads the keys, and only while it is open.
/// </summary>
public sealed class RideScreen
{
    /// <summary>The count before a ride: three seconds, then GO for half a second.</summary>
    public const float CountIn = 3f;

    private const float AppearTime = 0.3f;

    public bool IsActive { get; private set; }
    public RidePhase Phase { get; private set; }
    public SurfCourse? Course { get; private set; }
    public RideRecords Records { get; private set; } = new();
    public TrickSurf? Ride { get; private set; }

    /// <summary>The track of a riding race (plan 42 · PS8), with its race once begun; both null for a course of trick surfing.</summary>
    public RaceTrack? Track { get; private set; }
    public RidingRace? Race { get; private set; }

    /// <summary>The player's mount in a race: its species and the athlete it is made into.</summary>
    public Athlete? Mount { get; private set; }

    /// <summary>The event ridden, whichever it is.</summary>
    public AthlonEvent? Event => (AthlonEvent?)Ride ?? Race;

    /// <summary>The player's place once the ride is over (1 to 4); 0 when the briefing was left.</summary>
    public int Outcome { get; private set; }

    /// <summary>What the run left once it is recorded.</summary>
    public RideResult Result { get; private set; }

    /// <summary>Seconds since the phase began (its slide-in and the count before the ride).</summary>
    public float PhaseAge { get; private set; }

    public float Appear => Math.Clamp(PhaseAge / AppearTime, 0f, 1f);

    /// <summary>Seconds of the count still to go before the ride's first frame (nought once it runs).</summary>
    public float CountLeft => Phase == RidePhase.Riding ? Math.Max(0f, CountIn - PhaseAge) : 0f;

    /// <summary>The input held while the ride plays (the keys' in the game, a tool's otherwise).</summary>
    public AthlonInput Input { get; set; }

    private TimingAssist assist;
    private Random? rng;
    private (int Tricks, int Falls, int Jumps) heard;

    /// <summary>The Battle Points held before the ride, which the points' card adds what it paid to.</summary>
    public int PointsBefore { get; private set; }

    /// <summary>The Battle Points held once the ride has paid.</summary>
    public int PointsHeld => PointsBefore + Result.Paid;

    /// <summary>Opens a riding race on <paramref name="mount"/>, the player's own Pokémon of a riding size.</summary>
    public void OpenRace(RaceTrack track, Athlete mount, RideRecords records, Random random, int pointsHeld = 0)
    {
        Open(null, records, TimingAssist.Off, random, pointsHeld);
        Track = track;
        Mount = mount;
    }

    public void Open(SurfCourse? course, RideRecords records, TimingAssist timing, Random random, int pointsHeld = 0)
    {
        PointsBefore = pointsHeld;
        Track = null;
        Race = null;
        Mount = null;
        Course = course;
        Records = records;
        assist = timing;
        rng = random;
        Ride = null;
        Outcome = 0;
        Result = default;
        Input = AthlonInput.None;
        IsActive = true;
        Enter(RidePhase.Briefing);
        AudioManager.PlaySound("select");
    }

    private void Enter(RidePhase phase)
    {
        Phase = phase;
        PhaseAge = 0f;
    }

    public void Confirm()
    {
        if (!IsActive) return;
        switch (Phase)
        {
            case RidePhase.Briefing:
                if (Track != null) Race = new RidingRace(Track, RaceTracks.Teams(Track, Mount!), (rng ?? Dice.New()).Next());
                else Ride = new TrickSurf(Course!, SurfCourses.Riders(Course!), (rng ?? Dice.New()).Next()) { Assist = assist };
                Input = AthlonInput.None;
                heard = default;
                Enter(RidePhase.Riding);
                AudioManager.PlaySound("select");
                break;
            case RidePhase.Results:
                Enter(RidePhase.Points);
                AudioManager.PlaySound(Result.Paid > 0 ? "coin_land" : "select");
                break;
            case RidePhase.Points:
                AudioManager.PlaySound("select");
                IsActive = false;
                break;
        }
    }

    public void Cancel()
    {
        if (!IsActive) return;
        // The briefing may be left; a ride once begun is ridden to its end, as the Pokéathlon's events are
        if (Phase == RidePhase.Briefing)
        {
            AudioManager.PlaySound("cancel");
            Outcome = 0;
            IsActive = false;
        }
    }

    /// <summary>Time passes: the phase's clock, and while the ride plays its frames with <see cref="Input"/>.</summary>
    public void Advance(float dt)
    {
        if (!IsActive) return;
        float before = PhaseAge;
        PhaseAge += dt;
        if (Phase != RidePhase.Riding || Event is not { } ride) return;
        float running = PhaseAge - Math.Max(before, CountIn);
        if (running <= 0f) return;
        if (ride.Advance(running, Input) > 0) Input = Input with { Tap = false };
        Hear();
        if (ride.IsDone) Finish();
    }

    /// <summary>Plays a race's frames one by one, as <see cref="Play"/> does a ride's (for tests and the harness).</summary>
    public int PlayRace(int frames, Func<RidingRace, AthlonInput> input)
    {
        if (!IsActive || Phase != RidePhase.Riding || Race is not { } race) return 0;
        if (PhaseAge < CountIn) PhaseAge = CountIn;
        int played = 0;
        for (; played < frames && !race.IsDone; played++)
        {
            race.Step(input(race));
            PhaseAge += AthlonEvent.Frame;
            Hear();
        }
        if (race.IsDone) Finish();
        return played;
    }

    /// <summary>
    /// Plays the ride's frames one by one, each with the input <paramref name="input"/> gives for the ride as it stands
    /// (for tests and the harness), past the count first. Returns the frames played.
    /// </summary>
    public int Play(int frames, Func<TrickSurf, AthlonInput> input)
    {
        if (!IsActive || Phase != RidePhase.Riding || Ride is not { } ride) return 0;
        if (PhaseAge < CountIn) PhaseAge = CountIn;
        int played = 0;
        for (; played < frames && !ride.IsDone; played++)
        {
            ride.Step(input(ride));
            PhaseAge += AthlonEvent.Frame;
            Hear();
        }
        if (ride.IsDone) Finish();
        return played;
    }

    private void Finish()
    {
        if (Race is { } race)
        {
            Outcome = race.PlayerPlace;
            Result = Records.RecordRace(Track!, race.Points(0), Outcome);
        }
        else
        {
            var ride = Ride!;
            Outcome = ride.PlayerPlace;
            Result = Records.Record(Course!, ride.Riders[0].Score, Outcome);
        }
        Enter(RidePhase.Results);
        AudioManager.PlaySound(Outcome == 1 ? "levelup" : "lift_ding");
    }

    // The player's own rider is heard as it rides: a straight landing, a fall and each jump off the crest (the game's own
    // sounds stand in; the rides' own wait for plan 31's fuller sound and the user's ear)
    private void Hear()
    {
        if (Race is { } race)
        {
            var racer = race.Racers[0];
            if (racer.Cleared > heard.Tricks) AudioManager.PlaySound("coin_land");
            if (racer.Stumbles > heard.Falls) AudioManager.PlaySound("bump");
            if (racer.Air > 0 && racer.Air == racer.AirLength - 1) AudioManager.PlaySound("ledge");
            heard = (racer.Cleared, racer.Stumbles, heard.Jumps);
            return;
        }
        var ride = Ride!;
        var me = ride.Riders[0];
        if (me.Landed > heard.Tricks) AudioManager.PlaySound("coin_land");
        if (me.Falls > heard.Falls) AudioManager.PlaySound("bump");
        if (me.Jumps > heard.Jumps) AudioManager.PlaySound("ledge");
        heard = (me.Landed, me.Falls, me.Jumps);
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        if (Phase == RidePhase.Riding)
        {
            int dx = (InputManager.IsActionDown(GameAction.Right) ? 1 : 0) - (InputManager.IsActionDown(GameAction.Left) ? 1 : 0);
            int dy = (InputManager.IsActionDown(GameAction.Down) ? 1 : 0) - (InputManager.IsActionDown(GameAction.Up) ? 1 : 0);
            // A race: the button jumps and Run (B, Shift) held pushes; a ride's button is its jump and its landing's press
            Input = new AthlonInput(Tap: Input.Tap || InputManager.IsActionPressed(GameAction.Confirm),
                Held: Track != null ? InputManager.IsActionDown(GameAction.Run) : InputManager.IsActionDown(GameAction.Confirm),
                Dx: dx, Dy: dy);
            Advance(dt);
            return;
        }
        Advance(dt);
        if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
    }

    public void Draw(int sw, int sh)
    {
        if (IsActive) ModernUi.DrawRide(sw, sh, this);
    }
}
