using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Core;

/// <summary>What a caption names: a sound effect, a Pokémon's cry or an ambience bed coming in (the sign beside its words differs).</summary>
public enum CaptionKind { Sound, Cry, Ambience }

/// <summary>
/// Which of the field's ambience beds are sounding, for the captions (plan 12 · Q9): a bed is named once as it comes in
/// (its gain rising past <see cref="In"/>) and not again until it has gone (under <see cref="Out"/>), so walking along a
/// river's bank or out of the rain's edge doesn't name it at every step. GPU-free; told the beds each frame.
/// </summary>
public sealed class BedWatch
{
    /// <summary>The gain at which a bed counts as come in, and the gain under which it counts as gone.</summary>
    public const float In = 0.2f, Out = 0.05f;

    private readonly HashSet<Audio.AmbienceBed> sounding = new();

    /// <summary>The beds sounding now, as far as the captions know.</summary>
    public IReadOnlyCollection<Audio.AmbienceBed> Sounding => sounding;

    /// <summary>Takes the beds asked for this frame and gives back those that have just come in, in the order asked.</summary>
    public List<Audio.AmbienceBed> Notice(IReadOnlyList<Audio.AmbienceLayer> layers)
    {
        var come = new List<Audio.AmbienceBed>();
        var gains = new Dictionary<Audio.AmbienceBed, float>();
        foreach (var layer in layers)
            gains[layer.Bed] = Math.Max(gains.GetValueOrDefault(layer.Bed), layer.Gain);
        sounding.RemoveWhere(bed => gains.GetValueOrDefault(bed) < Out);
        foreach (var layer in layers)
            if (gains[layer.Bed] >= In && sounding.Add(layer.Bed)) come.Add(layer.Bed);
        return come;
    }

    /// <summary>Forgets every bed, so the next frame names whatever is sounding then.</summary>
    public void Clear() => sounding.Clear();
}

/// <summary>One caption standing: its words, what it names, how long it has stood and how many times its sound played.</summary>
public sealed record CaptionLine(string Text, CaptionKind Kind, float Age, int Times);

/// <summary>
/// The captions standing on screen (plan 12 · Q9; style guide, "Field menus and notices", "Captions"): each sound and
/// cry named as it plays, held <see cref="Seconds"/>, at most <see cref="MostShown"/> at once, a sound that plays again
/// while its caption stands renewing it and counting the times. GPU-free: its clock is the frame's time handed to
/// <see cref="Advance"/>, never the computer's, so the harness's pictures repeat.
/// </summary>
public sealed class CaptionQueue
{
    /// <summary>How long a caption stands after its sound.</summary>
    public const float Seconds = 3f;

    /// <summary>How long a caption takes to come in, and to go.</summary>
    public const float FadeIn = 0.15f, FadeOut = 0.4f;

    /// <summary>The most captions standing at once: a fourth puts the oldest away.</summary>
    public const int MostShown = 3;

    private readonly object gate = new();
    private readonly List<CaptionLine> lines = new();

    /// <summary>The captions standing, the oldest first.</summary>
    public IReadOnlyList<CaptionLine> Lines
    {
        get { lock (gate) return lines.ToList(); }
    }

    /// <summary>A sound or a cry named: a caption of the same words standing is renewed in its place and counted once more.</summary>
    public void Push(string text, CaptionKind kind)
    {
        if (string.IsNullOrEmpty(text)) return;
        lock (gate)
        {
            int at = lines.FindIndex(l => l.Text == text && l.Kind == kind);
            if (at >= 0)
            {
                var standing = lines[at];
                // Renewed, but it doesn't come in again: it stands at full strength from here
                lines[at] = standing with { Age = Math.Min(standing.Age, FadeIn), Times = standing.Times + 1 };
                return;
            }
            lines.Add(new CaptionLine(text, kind, 0f, 1));
            while (lines.Count > MostShown) lines.RemoveAt(0);
        }
    }

    /// <summary>Moves every caption on by a frame's time and puts away those whose time is up.</summary>
    public void Advance(float dt)
    {
        lock (gate)
        {
            for (int i = 0; i < lines.Count; i++) lines[i] = lines[i] with { Age = lines[i].Age + Math.Max(0f, dt) };
            lines.RemoveAll(l => l.Age >= Seconds);
        }
    }

    /// <summary>Puts every caption away.</summary>
    public void Clear()
    {
        lock (gate) lines.Clear();
    }

    /// <summary>How strongly a caption of this age is shown, 0 to 1: in over its first <see cref="FadeIn"/>, out over its last <see cref="FadeOut"/>.</summary>
    public static float Strength(float age) =>
        Math.Clamp(Math.Min(age / FadeIn, (Seconds - age) / FadeOut), 0f, 1f);
}
