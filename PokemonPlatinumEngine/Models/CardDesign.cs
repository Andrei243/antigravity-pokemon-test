using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Models;

/// <summary>The few marks a card's background is painted with (plan 43 · LF1; style guide, "The card designer").</summary>
public enum CardPattern { None, Stripes, Dots, Waves, Checks, Rays, Stars, Leaves, Hills }

/// <summary>How a card's frame is drawn round its edge (plan 43 · LF1).</summary>
public enum CardFrameStyle { Plain, Double, Scallop, Corners, Beads, Ribbon, Laurel, Gilt }

/// <summary>A background of <c>card-looks.json</c>: two tones from top to bottom, a pattern in its ink, and what it waits for.</summary>
public sealed record CardBackground
{
    [DataRequired] public string Id { get; init; } = "";
    [DataRequired] public string Top { get; init; } = "#ffffff";
    [DataRequired] public string Bottom { get; init; } = "#ffffff";
    public CardPattern Pattern { get; init; }
    [DataRequired] public string Ink { get; init; } = "#cccccc";
    [DataRequired] public string Unlock { get; init; } = CardLooks.Start;
}

/// <summary>A frame of <c>card-looks.json</c>: its colour, its accent, its style, and what it waits for.</summary>
public sealed record CardFrame
{
    [DataRequired] public string Id { get; init; } = "";
    [DataRequired] public string Color { get; init; } = "#ffffff";
    [DataRequired] public string Accent { get; init; } = "#cccccc";
    public CardFrameStyle Style { get; init; }
    [DataRequired] public string Unlock { get; init; } = CardLooks.Start;
}

/// <summary>What <c>Data/card-looks.json</c> holds (ours, written by hand, read strictly).</summary>
public sealed record CardLooksFile
{
    public List<CardBackground> Backgrounds { get; init; } = new();
    public List<CardFrame> Frames { get; init; } = new();
}

/// <summary>
/// What a card's look waits for (plan 43 · LF1): plain values the engine gathers when the designer opens, so a test makes
/// its own. Regions are named as <see cref="RegionDatabase"/> names them, compared without case.
/// </summary>
public sealed record CardFacts
{
    /// <summary>The regions whose every Badge is won.</summary>
    public IReadOnlySet<string> BadgesWhole { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>The regions whose Hall of Fame is entered.</summary>
    public IReadOnlySet<string> Champions { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>The categories of medals won whole (<see cref="MedalCategory"/> by name).</summary>
    public IReadOnlySet<string> MedalsWhole { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>The Pokédex diplomas held, by <see cref="Pokedex.DiplomaKey"/>.</summary>
    public IReadOnlySet<string> Diplomas { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>The regions whose five photo spots are all taken (plan 43 · LF4).</summary>
    public IReadOnlySet<string> SpotsTaken { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public static CardFacts None { get; } = new();
}

/// <summary>
/// The card's looks (plan 43 · LF1): the backgrounds and frames of <c>card-looks.json</c>, and what each waits for. A
/// look's <c>unlock</c> is <c>start</c>, <c>badges:&lt;region&gt;</c> (every Badge), <c>champion:&lt;region&gt;</c> (its
/// Hall of Fame), <c>medals:&lt;category&gt;</c> (the category whole), <c>diploma:&lt;key&gt;</c> or
/// <c>spots:&lt;region&gt;</c> (its five photo spots). The Fashion Case's backdrops owned are backgrounds too, named
/// <c>backdrop:&lt;n&gt;</c>. GPU-free.
/// </summary>
public static class CardLooks
{
    public const string FileName = "card-looks.json";
    public const string Start = "start";

    /// <summary>The name a Fashion Case backdrop goes by among the backgrounds.</summary>
    public const string BackdropPrefix = "backdrop:";

    private static readonly Lazy<CardLooksFile> file = new(() => GameDataFiles.Load<CardLooksFile>(FileName, strict: true));

    public static IReadOnlyList<CardBackground> Backgrounds => file.Value.Backgrounds;
    public static IReadOnlyList<CardFrame> Frames => file.Value.Frames;

    public static CardBackground? Background(string? id) => Backgrounds.FirstOrDefault(b => b.Id == id);
    public static CardFrame? Frame(string? id) => Frames.FirstOrDefault(f => f.Id == id);

    /// <summary>The kinds of fact an unlock may wait for.</summary>
    public static readonly string[] Kinds = { "badges", "champion", "medals", "diploma", "spots" };

    /// <summary>Whether an unlock is written as the file allows (for the content's tests).</summary>
    public static bool IsWellFormed(string unlock)
    {
        if (unlock == Start) return true;
        int at = unlock.IndexOf(':');
        return at > 0 && at < unlock.Length - 1 && Kinds.Contains(unlock[..at]);
    }

    /// <summary>Whether what <paramref name="unlock"/> waits for has happened.</summary>
    public static bool Unlocked(string unlock, CardFacts facts)
    {
        if (unlock == Start) return true;
        int at = unlock.IndexOf(':');
        if (at <= 0) return false;
        string what = unlock[(at + 1)..];
        return unlock[..at] switch
        {
            "badges" => facts.BadgesWhole.Contains(what),
            "champion" => facts.Champions.Contains(what),
            "medals" => facts.MedalsWhole.Contains(what),
            "diploma" => facts.Diplomas.Contains(what),
            "spots" => facts.SpotsTaken.Contains(what),
            _ => false
        };
    }

    /// <summary>The backgrounds that may be chosen: the file's unlocked, then the Fashion Case's owned (<paramref name="backdrops"/>).</summary>
    public static List<string> BackgroundsOpen(CardFacts facts, IEnumerable<int>? backdrops = null) =>
        Backgrounds.Where(b => Unlocked(b.Unlock, facts)).Select(b => b.Id)
            .Concat((backdrops ?? Enumerable.Empty<int>()).OrderBy(n => n).Select(n => BackdropPrefix + n)).ToList();

    /// <summary>The frames that may be chosen.</summary>
    public static List<string> FramesOpen(CardFacts facts) => Frames.Where(f => Unlocked(f.Unlock, facts)).Select(f => f.Id).ToList();

    /// <summary>A Fashion Case backdrop's number from its name among the backgrounds; null for one of the file's.</summary>
    public static int? BackdropOf(string? id) =>
        id != null && id.StartsWith(BackdropPrefix, StringComparison.Ordinal) && int.TryParse(id[BackdropPrefix.Length..], out int n) ? n : null;
}

/// <summary>A pose for the card's figure (plan 43 · LF1): standing, sitting on the ground, or a gesture held.</summary>
public enum CardPoseKind { Standing, Sitting, Gesture }

/// <summary>What the card's title line says under the name: nothing, the latest master's title, or a Hall of Fame's region.</summary>
public enum CardTitleKind { None, Master, Champion }

/// <summary>
/// The card as the player designed it (plan 43 · LF1; style guide, "The card designer"): a background and a frame of
/// <see cref="CardLooks"/>, the figure's pose and face, the title under the name, the six Pokémon shown (by
/// <see cref="Pokemon.Id"/>; null for the team as it stands) and the signature (a <see cref="SignaturePad"/>'s bits).
/// The greeting is a phrase kept in the story under <see cref="GreetingPhrase"/>. Saved whole as
/// <c>SaveData.CardDesign</c>, null for a card never designed, which is today's front. Plain data.
/// </summary>
public sealed class CardDesign
{
    /// <summary>The phrase the greeting is kept under in <see cref="Story.StoryState.Phrases"/>.</summary>
    public const string GreetingPhrase = "CARD";

    /// <summary>How far through its round a gesture is held: its height for every gesture of the kit.</summary>
    public const float HeldAt = 0.45f;

    public string Background { get; set; } = "plain";
    public string Frame { get; set; } = "plain";
    public CardPoseKind Pose { get; set; }
    public Gesture? Gesture { get; set; }
    public Mood Mood { get; set; }
    public CardTitleKind Title { get; set; }

    /// <summary>The region whose Hall of Fame the title names, with <see cref="CardTitleKind.Champion"/>.</summary>
    public string? TitleRegion { get; set; }

    /// <summary>The Pokémon shown, at most six, by id; null for the team as it stands.</summary>
    public List<Guid>? Shown { get; set; }

    /// <summary>The signature, a <see cref="SignaturePad"/>'s bits in base 64; null for none.</summary>
    public string? Signature { get; set; }

    public CardDesign Copy() => new()
    {
        Background = Background, Frame = Frame, Pose = Pose, Gesture = Gesture, Mood = Mood, Title = Title,
        TitleRegion = TitleRegion, Shown = Shown?.ToList(), Signature = Signature
    };

    /// <summary>The poses offered, in order: standing, sitting, then each gesture held.</summary>
    public static IReadOnlyList<(CardPoseKind Kind, Gesture? Gesture)> Poses { get; } =
        new (CardPoseKind, Gesture?)[] { (CardPoseKind.Standing, null), (CardPoseKind.Sitting, null) }
            .Concat(Enum.GetValues<Gesture>().Select(g => (CardPoseKind.Gesture, (Gesture?)g))).ToList();

    /// <summary>Which of <see cref="Poses"/> this design holds.</summary>
    public int PoseIndex
    {
        get
        {
            for (int i = 0; i < Poses.Count; i++)
                if (Poses[i].Kind == Pose && (Pose != CardPoseKind.Gesture || Poses[i].Gesture == Gesture)) return i;
            return 0;
        }
    }

    public void SetPose(int index)
    {
        var (kind, gesture) = Poses[((index % Poses.Count) + Poses.Count) % Poses.Count];
        (Pose, Gesture) = (kind, kind == CardPoseKind.Gesture ? gesture : null);
    }

    /// <summary>
    /// The Pokémon the card shows, in order, of <paramref name="owned"/> (every Pokémon of the team and the boxes, by
    /// id): the ones chosen that are still the player's, or the team's first six that are no Eggs.
    /// </summary>
    public IReadOnlyList<Pokemon> ShownOf(IReadOnlyList<Pokemon> team, IReadOnlyDictionary<Guid, Pokemon> owned)
    {
        if (Shown == null) return team.Where(p => !p.IsEgg).Take(Party.MaxSize).ToList();
        return Shown.Where(owned.ContainsKey).Select(id => owned[id]).Where(p => !p.IsEgg).Take(Party.MaxSize).ToList();
    }

    /// <summary>Drops the ids of Pokémon no longer the player's (released, traded): what a save keeps is what is shown.</summary>
    public void Forget(Func<Guid, bool> stillOwned) => Shown?.RemoveAll(id => !stillOwned(id));
}

/// <summary>
/// The signature (plan 43 · LF1): a grid of <see cref="Width"/> by <see cref="Height"/> cells, written with strokes from
/// one cell to the next (every cell on the line between filled), rubbed out the same way, or cleared, and kept as packed
/// bits, a cell to a bit row by row. GPU-free.
/// </summary>
public sealed class SignaturePad
{
    public const int Width = 160, Height = 48;
    private readonly bool[] cells = new bool[Width * Height];

    public bool this[int x, int y] => x >= 0 && y >= 0 && x < Width && y < Height && cells[y * Width + x];

    public bool IsEmpty => !cells.Any(c => c);

    /// <summary>How many cells are written.</summary>
    public int Count => cells.Count(c => c);

    /// <summary>A line from one cell to another, every cell on it written (or rubbed out with <paramref name="erase"/>); cells off the pad are passed over.</summary>
    public void Stroke(int x0, int y0, int x1, int y1, bool erase = false)
    {
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            Set(x0, y0, !erase);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    private void Set(int x, int y, bool on)
    {
        if (x >= 0 && y >= 0 && x < Width && y < Height) cells[y * Width + x] = on;
    }

    public void Clear() => Array.Clear(cells);

    /// <summary>The cells as packed bits, a byte to eight cells in order (the first cell the byte's lowest bit).</summary>
    public byte[] ToBits()
    {
        var bits = new byte[(cells.Length + 7) / 8];
        for (int i = 0; i < cells.Length; i++)
            if (cells[i]) bits[i >> 3] |= (byte)(1 << (i & 7));
        return bits;
    }

    public static SignaturePad FromBits(byte[]? bits)
    {
        var pad = new SignaturePad();
        if (bits == null) return pad;
        for (int i = 0; i < pad.cells.Length && (i >> 3) < bits.Length; i++)
            pad.cells[i] = (bits[i >> 3] & (1 << (i & 7))) != 0;
        return pad;
    }

    /// <summary>The bits as a save keeps them; null for an empty pad.</summary>
    public string? ToSaved() => IsEmpty ? null : Convert.ToBase64String(ToBits());

    public static SignaturePad FromSaved(string? saved)
    {
        if (string.IsNullOrEmpty(saved)) return new SignaturePad();
        try { return FromBits(Convert.FromBase64String(saved)); }
        catch (FormatException) { return new SignaturePad(); }
    }
}
