using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.StageShow;

/// <summary>A rival of a show's cast: its species, the pieces it wears and how often (out of a hundred) it tricks on the beat.</summary>
public sealed class ShowCastMember
{
    [DataRequired] public string Species { get; set; } = "";
    [DataRequired] public List<string> Props { get; set; } = new();
    [DataRequired] public int Knack { get; set; }
}

/// <summary>A performer's spotlight: from a bar, for so many bars (performer 0 is the player's Pokémon).</summary>
public sealed class ShowCue
{
    [DataRequired] public int Performer { get; set; }
    [DataRequired] public int Bar { get; set; }
    [DataRequired] public int Length { get; set; }
}

/// <summary>One show of <c>Data/stage-shows.json</c> (ours; <c>docs/data-files.md</c>, "Stage shows").</summary>
public sealed class StageShowData
{
    [DataRequired] public string Id { get; set; } = "";
    [DataRequired] public string Song { get; set; } = "";
    [DataRequired] public double Tempo { get; set; }
    [DataRequired] public int BeatsPerBar { get; set; }
    [DataRequired] public int Bars { get; set; }
    [DataRequired] public PropMood Mood { get; set; }
    [DataRequired] public List<ShowCastMember> Cast { get; set; } = new();
    [DataRequired] public List<ShowCue> Cues { get; set; } = new();

    public string NameKey { get { string key = ShowPropDatabase.KeyOf(Id); return $"stageShow.show.{key}"; } }
}

public sealed class StageShowsFile
{
    [DataRequired] public List<StageShowData> Shows { get; set; } = new();
}

public static class StageShowDatabase
{
    public const string FileName = "stage-shows.json";

    private static readonly Lazy<List<StageShowData>> all = new(() => GameDataFiles.Load<StageShowsFile>(FileName, strict: true).Shows);

    public static IReadOnlyList<StageShowData> All => all.Value;

    public static StageShowData? Get(string id) => All.FirstOrDefault(s => s.Id == id);
}

/// <summary>One of the four on the stage: its species, its dressing, whether it is the player's and its knack (a rival's).</summary>
public sealed class Performer
{
    public string Species { get; init; } = "";
    public Dressing Dressing { get; init; } = new();
    public bool IsPlayer { get; init; }
    public int Knack { get; init; }
}

/// <summary>A trick done on the stage: by whom, at which frame and beat, with which piece (null for none), on the beat or not, and the eyes it drew.</summary>
public readonly record struct ShowTrick(int Performer, int Frame, int Beat, string? Prop, PropTrick Trick, bool OnBeat, int Drew);

/// <summary>
/// A stage show from its curtain to its bows (plan 42 · PS2; <c>docs/mechanics/rulings.md</c>, "The stage show"), GPU-free,
/// on the Pokéathlon's shape: thirty frames a second, <see cref="Step"/> a frame with the player's press, the rivals'
/// tricks on the show's own generator from its seed, so a show is a pure function of its seed and the presses. It counts
/// its own frames and never reads the song's position. The crowd's eyes are a share of a hundred among the four.
/// </summary>
public sealed class ShowRun
{
    public const int FramesPerSecond = 30;

    /// <summary>How far off a beat a press still counts, in seconds (doubled under a timing assist).</summary>
    public const float BeatWindow = 0.1f;

    /// <summary>The eyes a trick on the beat draws, the more for a piece of the show's mood; a trick repeated draws half.</summary>
    public const int TrickEyes = 4, MoodEyes = 3, BareEyes = 2;

    /// <summary>What each piece of the show's mood adds to the score at the curtain.</summary>
    public const int MatchPoints = 5;

    /// <summary>The frames of curtain after the last bar before the show is over.</summary>
    public const int CurtainFrames = 60;

    private readonly Random random;
    private readonly int[] eyes = { 25, 25, 25, 25 };
    private readonly int[] trickCount = new int[4];
    private readonly PropTrick?[] lastTrick = new PropTrick?[4];
    private readonly HashSet<(int Performer, int Beat)> used = new();
    private readonly List<ShowTrick> tricks = new();
    private float carried;

    public StageShowData Show { get; }
    public IReadOnlyList<Performer> Performers { get; }
    public TimingAssist Assist { get; }
    public int Seed { get; }

    public int Frame { get; private set; }

    public ShowRun(StageShowData show, Performer player, int seed, TimingAssist assist = TimingAssist.Off)
    {
        Show = show;
        Seed = seed;
        Assist = TimingAssists.Scored(assist);
        random = new Random(seed);
        var four = new List<Performer> { new() { Species = player.Species, Dressing = player.Dressing.Copy(), IsPlayer = true } };
        four.AddRange(show.Cast.Take(3).Select(c => new Performer { Species = c.Species, Dressing = Dressing.Of(c.Props), Knack = c.Knack }));
        Performers = four;
    }

    /// <summary>The frames one beat lasts (not a whole number).</summary>
    public double FramesPerBeat => FramesPerSecond * 60.0 / Show.Tempo;

    public int TotalBeats => Show.Bars * Show.BeatsPerBar;

    /// <summary>The frame the show's last beat ends on.</summary>
    public int MusicFrames => (int)Math.Ceiling(TotalBeats * FramesPerBeat);

    public int TotalFrames => MusicFrames + CurtainFrames;

    public bool Over => Frame >= TotalFrames;

    /// <summary>The frame a beat falls on (rounded to the nearest).</summary>
    public int FrameOfBeat(int beat) => (int)Math.Round(beat * FramesPerBeat);

    /// <summary>The beat under way at a frame (the music's), past the last when the music is over.</summary>
    public int BeatAt(int frame) => (int)Math.Floor(frame / FramesPerBeat);

    public int Beat => BeatAt(Frame);
    public int Bar => Beat / Show.BeatsPerBar;

    /// <summary>The cue holding a beat, or null between cues.</summary>
    public ShowCue? CueAt(int beat)
    {
        int bar = beat / Math.Max(1, Show.BeatsPerBar);
        return Show.Cues.FirstOrDefault(c => bar >= c.Bar && bar < c.Bar + c.Length);
    }

    /// <summary>Who has the spotlight now, or -1.</summary>
    public int Spotlight => Frame < MusicFrames && CueAt(Beat) is { } c ? c.Performer : -1;

    public IReadOnlyList<int> Eyes => eyes;
    public IReadOnlyList<ShowTrick> Tricks => tricks;

    /// <summary>The frames either side of a beat a press is on it.</summary>
    public int WindowFrames => (int)Math.Round(TimingAssists.Window(BeatWindow, Assist) * FramesPerSecond);

    /// <summary>The beat nearest a frame.</summary>
    public int NearestBeat(int frame) => (int)Math.Round(frame / FramesPerBeat);

    /// <summary>Whether a press at this frame would be on a beat of the player's own cue that has taken no trick yet.</summary>
    public bool PlayerMayTrickNow()
    {
        int beat = NearestBeat(Frame);
        return Frame < MusicFrames && Math.Abs(Frame - FrameOfBeat(beat)) <= WindowFrames && CueAt(beat)?.Performer == 0 && !used.Contains((0, beat));
    }

    /// <summary>One frame: the player's press (if any) judged, the rivals' tricks on the beats of their cues.</summary>
    public void Step(bool press)
    {
        if (Over) return;
        if (press && Frame < MusicFrames) PlayerPress();
        // A rival tricks on the frame of a beat of its cue, if its knack's draw says so (one draw per beat of its cue)
        if (Frame < MusicFrames)
        {
            int beat = NearestBeat(Frame);
            if (FrameOfBeat(beat) == Frame && CueAt(beat) is { Performer: > 0 } cue && !used.Contains((cue.Performer, beat)))
            {
                int roll = random.Next(100);
                if (roll < Performers[cue.Performer].Knack) Trick(cue.Performer, beat, onBeat: true);
                else used.Add((cue.Performer, beat));
            }
        }
        Frame++;
    }

    /// <summary>Real time at thirty frames a second; the press goes with the first frame stepped.</summary>
    public void Advance(float dt, bool press)
    {
        carried += dt * FramesPerSecond;
        bool first = true;
        while (carried >= 1f && !Over)
        {
            carried -= 1f;
            Step(press && first);
            first = false;
        }
    }

    private void PlayerPress()
    {
        int beat = NearestBeat(Frame);
        bool inCue = CueAt(beat)?.Performer == 0;
        bool onBeat = inCue && Math.Abs(Frame - FrameOfBeat(beat)) <= WindowFrames && !used.Contains((0, beat));
        if (onBeat) Trick(0, beat, onBeat: true);
        else if (CueAt(BeatAt(Frame))?.Performer == 0)
            tricks.Add(new ShowTrick(0, Frame, beat, null, PropTrick.Bounce, false, 0)); // off the beat: nothing drawn
    }

    /// <summary>The piece a performer tricks with next: its pieces in turn, slot by slot.</summary>
    private ShowProp? NextPiece(int performer)
    {
        var props = Performers[performer].Dressing.Props.ToList();
        return props.Count == 0 ? null : props[trickCount[performer] % props.Count];
    }

    private void Trick(int performer, int beat, bool onBeat)
    {
        used.Add((performer, beat));
        var piece = NextPiece(performer);
        trickCount[performer]++;
        var kind = piece?.Trick ?? PropTrick.Bounce;
        int gain = piece == null ? BareEyes : piece.Mood == Show.Mood ? TrickEyes + MoodEyes : TrickEyes;
        if (lastTrick[performer] == kind) gain = (gain + 1) / 2;
        lastTrick[performer] = kind;
        int drew = Draw(performer, gain);
        tricks.Add(new ShowTrick(performer, Frame, beat, piece?.Id, kind, onBeat, drew));
    }

    /// <summary>Moves up to <paramref name="gain"/> eyes to a performer, a point at a time from whoever has the most of the rest (the lowest index first on a tie), never below nought.</summary>
    private int Draw(int performer, int gain)
    {
        int drew = 0;
        for (int k = 0; k < gain; k++)
        {
            int from = -1;
            for (int p = 0; p < 4; p++)
                if (p != performer && eyes[p] > 0 && (from < 0 || eyes[p] > eyes[from])) from = p;
            if (from < 0) break;
            eyes[from]--;
            eyes[performer]++;
            drew++;
        }
        return drew;
    }

    /// <summary>A performer's score at the curtain: its eyes and <see cref="MatchPoints"/> for each piece of the show's mood.</summary>
    public int ScoreOf(int performer) => eyes[performer] + MatchPoints * Performers[performer].Dressing.Matching(Show.Mood);

    /// <summary>The four by place: score, then eyes, then the order on the bill.</summary>
    public IReadOnlyList<int> Places() =>
        Enumerable.Range(0, 4).OrderByDescending(ScoreOf).ThenByDescending(p => eyes[p]).ThenBy(p => p).ToList();

    /// <summary>The player's place, from 1.</summary>
    public int PlayerPlace => Places().ToList().IndexOf(0) + 1;

    /// <summary>The press the autopilot makes this frame: exactly on every beat of the player's cues.</summary>
    public static bool Autopilot(ShowRun run)
    {
        if (run.Frame >= run.MusicFrames) return false;
        int beat = run.NearestBeat(run.Frame);
        return run.FrameOfBeat(beat) == run.Frame && run.CueAt(beat)?.Performer == 0;
    }

    /// <summary>Plays the show to its end with a press chooser (the autopilot's by default).</summary>
    public void PlayOut(Func<ShowRun, bool>? presses = null)
    {
        presses ??= Autopilot;
        while (!Over) Step(presses(this));
    }
}

/// <summary>
/// The lobby's three fans (plan 42 · PS2; the user's answer 27a: their lines kept, their gifts props): after a show one
/// of them waits with a piece the next time the theatre is come to. Who and which piece follow from the shows played and
/// the score alone, nothing drawn.
/// </summary>
public static class ShowFans
{
    /// <summary>The fans, by the person's key in the overlay (persons 1, 4 and 18 of the lobby).</summary>
    public static readonly string[] Keys = { "fan_1", "fan_4", "fan_18" };

    /// <summary>The gifts by the score's band: a modest show, a good one, a great one (ours).</summary>
    public static readonly string[][] Gifts =
    {
        new[] { "beret_red", "head_bow_orange", "bow_tie_red", "tail_ribbon_pink", "baton", "scarf_mint" },
        new[] { "top_hat_white", "paper_crown_gold", "star_glasses", "ruff", "paper_wings", "parasol", "hand_bell", "tail_ribbon_blue" },
        new[] { "tiara", "masquerade_mask", "monocle", "pearl_string", "short_cape_red", "tambourine", "paper_crown_silver", "pointed_hat" }
    };

    public static int BandOf(int score) => score >= 45 ? 2 : score >= 30 ? 1 : 0;

    /// <summary>Which fan waits after the <paramref name="played"/>th show (from 1) and with what.</summary>
    public static (string Fan, string Prop) GiftAfter(int played, int score)
    {
        var band = Gifts[BandOf(score)];
        return (Keys[(played - 1 + Keys.Length) % Keys.Length], band[(played - 1 + score) % band.Length]);
    }

    /// <summary>A show done: recorded, and a fan set waiting with a gift (one fan at a time holds one).</summary>
    public static (string Fan, string Prop) Finish(ShowRecords records, ShowRun run)
    {
        int score = run.ScoreOf(0);
        records.Record(run.Show.Id, score, run.PlayerPlace == 1);
        var gift = GiftAfter(records.Played, score);
        records.Owe(gift.Fan, gift.Prop);
        return gift;
    }
}
