using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

/// <summary>What the Pokéathlon's screen shows: a course chosen, the team, an event's briefing, the event, its points, the scoreboard.</summary>
public enum AthlonPhase { Courses, Team, Briefing, Playing, EventPoints, Scoreboard, Records }

/// <summary>
/// The Pokéathlon on screen (plan 15 · E6; style guide, "The Pokéathlon"): the dome's clerk opens it on the course
/// choice; three of the team are chosen; each event is briefed, counted in, played frame by frame at thirty a second
/// (<see cref="AthlonEvent"/>) and its points shown; the scoreboard ends the course, records it and closes with the
/// player's place (<see cref="Outcome"/>, 0 when backed out of). Its logic takes no input (<see cref="Move"/>,
/// <see cref="Confirm"/>, <see cref="Cancel"/>, <see cref="Advance"/>, <see cref="Play"/>), so tests and the harness
/// drive it; <see cref="Update"/> reads the keys.
/// </summary>
public sealed class AthlonScreen
{
    /// <summary>The count before an event: three seconds, then GO for the last half second of it.</summary>
    public const float CountIn = 3f;

    private const float AppearTime = 0.3f;

    private Party? party;
    private Random? rng;
    private readonly List<int> chosen = new();

    public bool IsActive { get; private set; }
    public AthlonPhase Phase { get; private set; }
    public AthlonRecords Records { get; private set; } = new();
    public AthlonRun? Run { get; private set; }

    /// <summary>The course under the cursor, or the team's member under it.</summary>
    public int Cursor { get; private set; }

    /// <summary>The team's members chosen, in order (indices of the party).</summary>
    public IReadOnlyList<int> Chosen => chosen;

    public IReadOnlyList<Pokemon> Members => party?.Members ?? (IReadOnlyList<Pokemon>)Array.Empty<Pokemon>();

    /// <summary>The player's place once the course is over (1 to 4); 0 when the screen was left before it.</summary>
    public int Outcome { get; private set; }

    /// <summary>The medal the course's score won (plan 15 · E8), shown on the scoreboard.</summary>
    public AthlonMedal Medal { get; private set; }

    /// <summary>Whether the scoreboard's score is a new best for its course.</summary>
    public bool NewRecord { get; private set; }

    /// <summary>Seconds since the phase began (its slide-in and the count before an event).</summary>
    public float PhaseAge { get; private set; }

    public float Appear => Math.Clamp(PhaseAge / AppearTime, 0f, 1f);

    /// <summary>Seconds of the count still to go before the event's first frame (nought once it runs).</summary>
    public float CountLeft => Phase == AthlonPhase.Playing ? Math.Max(0f, CountIn - PhaseAge) : 0f;

    /// <summary>The input held while an event plays (the keys' in the game, a tool's otherwise).</summary>
    public AthlonInput Input { get; set; }

    public AthlonCourse Course => AthlonCourses.All[Math.Clamp(Phase == AthlonPhase.Courses ? Cursor : courseIndex, 0, AthlonCourses.All.Count - 1)];
    private int courseIndex;

    /// <summary>Whether a team has three that may go in (no Eggs): what the clerk asks first.</summary>
    public static bool CanEnter(Party team) => AthlonRun.CanEnter(team);

    public void Open(Party team, AthlonRecords records, Random random)
    {
        party = team;
        Records = records;
        rng = random;
        chosen.Clear();
        Run = null;
        Outcome = 0;
        NewRecord = false;
        Cursor = 0;
        courseIndex = 0;
        IsActive = true;
        Enter(AthlonPhase.Courses);
        AudioManager.PlaySound("select");
    }

    private void Enter(AthlonPhase phase)
    {
        Phase = phase;
        PhaseAge = 0f;
    }

    public void Move(int dx, int dy)
    {
        if (!IsActive) return;
        int step = dy != 0 ? dy : dx;
        if (step == 0) return;
        int count = Phase switch
        {
            AthlonPhase.Courses => AthlonCourses.All.Count,
            AthlonPhase.Team => Members.Count,
            _ => 0
        };
        if (count <= 1) return;
        Cursor = ((Cursor + step) % count + count) % count;
        AudioManager.PlaySound("cursor");
    }

    public void Confirm()
    {
        if (!IsActive) return;
        switch (Phase)
        {
            case AthlonPhase.Courses:
                courseIndex = Cursor;
                Cursor = 0;
                chosen.Clear();
                Enter(AthlonPhase.Team);
                AudioManager.PlaySound("select");
                break;

            case AthlonPhase.Team:
            {
                if (Cursor >= Members.Count) return;
                if (chosen.Contains(Cursor))
                {
                    chosen.Remove(Cursor);
                    AudioManager.PlaySound("cancel");
                    return;
                }
                if (Members[Cursor].IsEgg)
                {
                    AudioManager.PlaySound("error");
                    return;
                }
                chosen.Add(Cursor);
                AudioManager.PlaySound("select");
                if (chosen.Count == AthlonRun.TeamSize) Begin();
                break;
            }

            case AthlonPhase.Briefing:
                Run!.StartEvent();
                Input = AthlonInput.None;
                heard = default;
                Enter(AthlonPhase.Playing);
                AudioManager.PlaySound("select");
                break;

            case AthlonPhase.EventPoints:
                AudioManager.PlaySound("select");
                if (Run!.IsOver) Finish();
                else Enter(AthlonPhase.Briefing);
                break;

            case AthlonPhase.Scoreboard:
            case AthlonPhase.Records:
                AudioManager.PlaySound("select");
                IsActive = false;
                break;
        }
    }

    public void Cancel()
    {
        if (!IsActive) return;
        switch (Phase)
        {
            case AthlonPhase.Courses:
                AudioManager.PlaySound("cancel");
                Outcome = 0;
                IsActive = false;
                break;
            case AthlonPhase.Records:
                AudioManager.PlaySound("cancel");
                IsActive = false;
                break;
            case AthlonPhase.Team:
                AudioManager.PlaySound("cancel");
                if (chosen.Count > 0) chosen.RemoveAt(chosen.Count - 1);
                else
                {
                    Cursor = courseIndex;
                    Enter(AthlonPhase.Courses);
                }
                break;
            // Once the course is under way it is run to its end, as HeartGold's is
        }
    }

    /// <summary>The three are chosen: the course is made, its rivals drawn, and the first event briefed.</summary>
    private void Begin()
    {
        var mine = chosen.Select(i => AthleteStats.SpeciesOf(Members[i])).ToList();
        Run = new AthlonRun(AthlonCourses.All[courseIndex], mine, rng ?? Dice.New());
        Enter(AthlonPhase.Briefing);
    }

    private void Finish()
    {
        var run = Run!;
        Outcome = run.PlayerPlace;
        NewRecord = Records.Record(run.Course.Key, run.Earned, Outcome);
        Records.Count(run.AllBonuses(0));
        Medal = AthlonData.MedalFor(run.Earned);
        Enter(AthlonPhase.Scoreboard);
        AudioManager.PlaySound(Outcome == 1 ? "levelup" : "select");
    }

    /// <summary>Time passes: the phase's clock, and while an event plays its frames with <see cref="Input"/>.</summary>
    public void Advance(float dt)
    {
        if (!IsActive) return;
        float before = PhaseAge;
        PhaseAge += dt;
        if (Phase != AthlonPhase.Playing || Run?.Current is not { } ev) return;
        float running = PhaseAge - Math.Max(before, CountIn);
        if (running <= 0f) return;
        if (ev.Advance(running, Input) > 0) Input = Input with { Tap = false, Switch = 0 };
        Hear(ev);
        if (ev.IsDone) EndEvent();
    }

    /// <summary>
    /// Plays an event's frames one by one, each with the input <paramref name="input"/> gives for the event as it stands
    /// (for tests and the harness), past the count first. Returns the frames played.
    /// </summary>
    public int Play(int frames, Func<AthlonEvent, AthlonInput> input)
    {
        if (!IsActive || Phase != AthlonPhase.Playing || Run?.Current is not { } ev) return 0;
        if (PhaseAge < CountIn) PhaseAge = CountIn;
        int played = 0;
        for (; played < frames && !ev.IsDone; played++)
        {
            ev.Step(input(ev));
            PhaseAge += AthlonEvent.Frame;
            Hear(ev);
        }
        if (ev.IsDone) EndEvent();
        return played;
    }

    private void EndEvent()
    {
        Run!.EndEvent();
        Enter(AthlonPhase.EventPoints);
        AudioManager.PlaySound("lift_ding");
    }

    // The player's own team is heard as it plays, in every one of the ten events (the game's own sounds stand in: the
    // dome's own wait for the user's ear, as its theme does; docs/sound-effects.md): two counts of each event, a good
    // thing and a slip, each heard as it goes up
    private (int Good, int Slip) heard;

    /// <summary>What the player's team did in an event that is heard: two counts and the sound each goes up with.</summary>
    public static (int Good, string GoodSound, int Slip, string SlipSound) Heard(AthlonEvent ev) => ev switch
    {
        HurdleDash h => (h.Runners[0].Air, "ledge", h.Runners[0].Trips, "bump"),
        BlockSmash b => (b.Smashers[0].Broken, "rock_smash", 0, ""),
        RingDrop r => (r.Pushers[0].Knocks, "hit_super", r.Pushers[0].Falls, "run_away"),
        PennantCapture p => (p.Chasers[0].Flags, "coin_land", p.Chasers[0].Bumps, "bump"),
        SnowThrow s => (s.Throwers[0].Thrown, "snowball", s.Throwers[0].Taken, "hit_weak"),
        DiscCatch d => (d.Catchers[0].Caught + d.Catchers[0].CaughtInAir, "coin_land", d.Catchers[0].Missed, "bump"),
        LampJump l => (l.Jumpers[0].Good, "ledge", l.Jumpers[0].Bounces - l.Jumpers[0].Good, "bump"),
        RelayRun rr => (rr.Relays[0].Laps, "poketch_count", rr.Relays[0].Handovers, "recall"),
        CirclePush c => (c.Pushers[0].Alone / AthlonEvent.FramesPerSecond, "poketch_beep", 0, ""),
        GoalRoll g => (g.Players[0].Goals, "hit_super", g.Players[0].OwnGoals, "run_away"),
        _ => (0, "", 0, "")
    };

    private void Hear(AthlonEvent ev)
    {
        var now = Heard(ev);
        if (now.Good > heard.Good && now.GoodSound.Length > 0) AudioManager.PlaySound(now.GoodSound);
        if (now.Slip > heard.Slip && now.SlipSound.Length > 0) AudioManager.PlaySound(now.SlipSound);
        heard = (now.Good, now.Slip);
    }

    /// <summary>The records board (plan 15 · E8; <c>athlon records</c>): each course's best and its medal; any button closes it.</summary>
    public void OpenRecords(AthlonRecords records)
    {
        Records = records;
        Run = null;
        Outcome = 0;
        Cursor = 0;
        IsActive = true;
        Enter(AthlonPhase.Records);
        AudioManager.PlaySound("select");
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        if (Phase == AthlonPhase.Playing)
        {
            int dx = (InputManager.IsActionDown(GameAction.Right) ? 1 : 0) - (InputManager.IsActionDown(GameAction.Left) ? 1 : 0);
            int dy = (InputManager.IsActionDown(GameAction.Down) ? 1 : 0) - (InputManager.IsActionDown(GameAction.Up) ? 1 : 0);
            int change = (InputManager.IsActionPressed(GameAction.PageRight) || InputManager.IsActionPressed(GameAction.Cancel) ? 1 : 0)
                - (InputManager.IsActionPressed(GameAction.PageLeft) ? 1 : 0);
            Input = new AthlonInput(Tap: Input.Tap || InputManager.IsActionPressed(GameAction.Confirm), Held: InputManager.IsActionDown(GameAction.Confirm),
                Dx: dx, Dy: dy, Switch: change != 0 ? change : Input.Switch);
            Advance(dt);
            return;
        }
        Advance(dt);
        if (InputManager.IsActionPressed(GameAction.Up)) Move(0, -1);
        else if (InputManager.IsActionPressed(GameAction.Down)) Move(0, 1);
        if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
    }

    public void Draw(int sw, int sh)
    {
        if (IsActive) ModernUi.DrawAthlon(sw, sh, this);
    }

    /// <summary>
    /// For tools (plan 15 · E7): the screen opened on a course of the tool's own (any events in any order) with three of
    /// the team already chosen, at the first event's briefing. The game opens it only through <see cref="Open"/>.
    /// </summary>
    public void OpenOn(AthlonCourse course, Party team, AthlonRecords records, Random random, IReadOnlyList<int> three)
    {
        Open(team, records, random);
        chosen.AddRange(three);
        Run = new AthlonRun(course, chosen.Select(i => AthleteStats.SpeciesOf(Members[i])).ToList(), random);
        Enter(AthlonPhase.Briefing);
    }
}
