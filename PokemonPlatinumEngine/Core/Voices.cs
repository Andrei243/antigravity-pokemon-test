using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Core;

/// <summary>The options' VOICES row (plan 26 · V2): none, the named cast alone, or everyone.</summary>
public enum VoiceMode { Off, Story, All }

/// <summary>
/// A line as the voices know it (plan 26 · V2): its key (plan 17 · N4's, with a variant's <c>~starter=Piplup</c> after it
/// where the line has one), the English it is shown in (whose hash says whether a take is current) and the cast's voice
/// that speaks it, or null where nobody is known. It travels beside the line's text from the script to the text box.
/// </summary>
public sealed record SpokenLine(string Key, string English, string? Voice)
{
    /// <summary>Whether the voice is one of the named cast (<c>rival</c>, <c>rowan</c>), which "Story only" speaks; an archetype's (<c>look:lass#2</c>) is not.</summary>
    public bool Named => Voice is { Length: > 0 } v && !VoiceCast.IsArchetype(v) && v != VoiceCast.Self;
}

/// <summary>
/// A mouth that moves with a voice (plan 26 · V6), GPU-free: open while the voice is loud, shut in its gaps, with a gap
/// between the two levels so it doesn't chatter on a level that hovers, and held at least <see cref="Hold"/> in each
/// state, so it moves at most about seven times a second however the voice moves (a texel mouth flickering at the rate
/// of syllables reads as noise). Told the voice's level by <see cref="Hear"/> and the time by <see cref="Advance"/>.
/// </summary>
public sealed class MouthFlap
{
    /// <summary>The window the voice's level is read over, in seconds.</summary>
    public const float Window = 0.04f;

    /// <summary>The level (RMS, of full scale) that opens the mouth, and the lower one that shuts it: about −32 and −40 dB.</summary>
    public const float OpenAt = 0.025f, ShutAt = 0.01f;

    /// <summary>The least time in one state.</summary>
    public const float Hold = 0.07f;

    public bool Open { get; private set; }

    /// <summary>How long the mouth has been as it is.</summary>
    public float Held { get; private set; } = Hold;

    /// <summary>How many times the mouth has opened since it was last reset: what a test counts.</summary>
    public int Opened { get; private set; }

    public void Reset()
    {
        Open = false;
        Held = Hold;
        Opened = 0;
    }

    public void Advance(float dt) => Held += dt;

    /// <summary>The voice's level now: the mouth opens or shuts if the level says so and it has been as it is long enough.</summary>
    public void Hear(float level)
    {
        if (Held < Hold) return;
        if (!Open && level >= OpenAt)
        {
            Open = true;
            Opened++;
            Held = 0f;
        }
        else if (Open && level < ShutAt)
        {
            Open = false;
            Held = 0f;
        }
    }
}

/// <summary>
/// The voices of one text box (plan 26 · V2): the line that speaks now and how long it has left, started as the box
/// shows its line and cut as the box moves on. GPU-free and free of the audio device: a take's length is counted down by
/// the frames handed to <see cref="Advance"/>, so a test or the harness holds the same box the game does, and what is
/// asked of the mixer goes through <see cref="AudioManager"/>, which a test hears as <c>voice &lt;key&gt; &lt;voice&gt;</c>.
/// </summary>
public sealed class VoiceTalk
{
    /// <summary>The options' VOICES row. Off: nothing is looked up, loaded, played or heard.</summary>
    public VoiceMode Mode { get; set; } = VoiceMode.Off;

    /// <summary>The options' LET VOICES FINISH row: A at a finished line waits for its voice to end.</summary>
    public bool LetFinish { get; set; }

    /// <summary>The pack the takes come from; null for none (the game then plays nothing and the text shows as ever).</summary>
    public VoiceLibrary? Library { get; set; }

    /// <summary>The line speaking now, or null.</summary>
    public SpokenLine? Speaking { get; private set; }

    /// <summary>How long the line speaking now has left, in seconds.</summary>
    public float Left { get; private set; }

    /// <summary>Whether a voice is speaking now.</summary>
    public bool IsSpeaking => Speaking != null && Left > 0f;

    /// <summary>Whether this line would be spoken under the options: none with the voices off, only the named cast's with "Story only".</summary>
    public bool Wants(SpokenLine? line) =>
        line != null && Mode != VoiceMode.Off && (Mode == VoiceMode.All || line.Named);

    /// <summary>
    /// Asks the pack for a talk's lines ahead, as the talk starts, so each is decoded on a worker before its box shows it.
    /// </summary>
    public void Prefetch(IEnumerable<SpokenLine?> lines)
    {
        if (Library == null || Mode == VoiceMode.Off) return;
        var takes = new List<VoicePackEntry>();
        foreach (var line in lines)
            if (Wants(line) && Library.StateOf(line!.Key, line.English, line.Voice) is (VoiceState.Current, { } take)) takes.Add(take);
        if (takes.Count > 0) Library.Prefetch(takes);
    }

    /// <summary>
    /// A box shows a line: whatever spoke before is cut, and this line's voice starts, where the options want it and the
    /// pack has a take that is current. Tests hear every line asked for, with a pack or without, as
    /// <c>voice &lt;key&gt; &lt;voice&gt;</c>; a stale or missing one plays nothing.
    /// </summary>
    public void Start(SpokenLine? line)
    {
        Cut();
        if (!Wants(line)) return;
        AudioManager.NoteVoice(line!);
        if (Library == null) return;
        var (state, take) = Library.StateOf(line!.Key, line.English, line.Voice);
        if (state != VoiceState.Current || take == null) return;
        var sample = Library.Load(take);
        if (sample == null || sample.Samples.Length == 0) return;
        Speaking = line;
        Left = (float)sample.Duration;
        heard = sample.Samples;
        Mouth.Reset();
        Mouth.Hear(LevelNow);
        AudioManager.PlayVoice(sample);
    }

    /// <summary>Counts the voice down by a frame's time; a line that has run out is over.</summary>
    public void Advance(float dt)
    {
        if (Speaking == null) return;
        Left = MathF.Max(0f, Left - dt);
        if (Left <= 0f)
        {
            Speaking = null;
            heard = null;
            Mouth.Reset();
            return;
        }
        Mouth.Advance(dt);
        Mouth.Hear(LevelNow);
    }

    /// <summary>Cuts the voice speaking now (a box that moves on, closes or is skipped). Nothing when none speaks.</summary>
    public void Cut()
    {
        if (Speaking == null) return;
        Speaking = null;
        Left = 0f;
        heard = null;
        Mouth.Reset();
        AudioManager.StopVoice();
    }

    // The samples of the take speaking now, which the mouth is read from (plan 26 · V6)
    private float[]? heard;

    /// <summary>The speaker's mouth, moved by the take as it plays (plan 26 · V6): shut whenever no voice speaks.</summary>
    public MouthFlap Mouth { get; } = new();

    /// <summary>Whether the speaker's mouth is open this frame: only while a voice speaks, and with its syllables.</summary>
    public bool MouthOpen => IsSpeaking && Mouth.Open;

    /// <summary>
    /// How loud the take is where it has got to: the RMS of the <see cref="MouthFlap.Window"/> before the place it is
    /// playing, read from the take's own samples (so a test or the harness, with no sound card, sees what a player hears).
    /// </summary>
    public float LevelNow
    {
        get
        {
            if (heard == null || heard.Length == 0) return 0f;
            int at = Math.Clamp((int)((heard.Length / (double)Synthesizer.DsRate - Left) * Synthesizer.DsRate), 0, heard.Length);
            int size = (int)(MouthFlap.Window * Synthesizer.DsRate);
            int from = Math.Max(0, at - size);
            if (at <= from) return 0f;
            double sum = 0;
            for (int i = from; i < at; i++) sum += heard[i] * (double)heard[i];
            return (float)Math.Sqrt(sum / (at - from));
        }
    }

    /// <summary>
    /// The variant's suffix for a line with few-valued placeholders (plan 26 · V1's <c>~starter=Piplup</c>, in the order
    /// the line names them): <paramref name="valueOf"/> says what each stands for now, in English. Nothing for a line with none.
    /// </summary>
    public static string VariantOf(string english, Func<string, string?> valueOf)
    {
        if (!english.Contains('{')) return "";
        string suffix = "";
        foreach (string token in SpokenText.PlaceholdersOf(english))
        {
            string name = token.Trim('{', '}');
            if (name is not ("assistant" or "starter" or "rivalstarter")) continue;
            if (valueOf(name) is { Length: > 0 } value) suffix += $"~{name}={value}";
        }
        return suffix;
    }
}
