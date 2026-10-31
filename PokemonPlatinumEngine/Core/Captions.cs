using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Core;

/// <summary>What a caption names: a sound effect or a Pokémon's cry (the sign beside its words differs).</summary>
public enum CaptionKind { Sound, Cry }

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
