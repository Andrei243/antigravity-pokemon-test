using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Models;

/// <summary>Which side of the player the Pokémon stands on in a studio photograph.</summary>
public enum StudioPlace { Right, Left }

/// <summary>A sticker placed on a studio photograph: a Seal's number and where its middle lies, as shares of the picture (0 to 1).</summary>
public sealed record StudioSticker(int Seal, float X, float Y, float Size = 1f, int Turn = 0)
{
    /// <summary>The sizes a sticker is put on at (plan 43 · LF7's leftover, LF5's owed): small, as it was, large.</summary>
    public static readonly float[] Sizes = { 0.7f, 1f, 1.4f };

    /// <summary>How many steps a sticker's turn has: fifteen degrees each, a whole turn in twenty-four.</summary>
    public const int Turns = 24;

    /// <summary>The turn in radians.</summary>
    public float Angle => Turn * MathF.Tau / Turns;
}

/// <summary>What <c>Data/photo-studio.json</c> holds (ours, written by hand, read strictly): the studio's backdrops and frames, in the card's own shapes.</summary>
public sealed record StudioFile
{
    public List<CardBackground> Backdrops { get; init; } = new();
    public List<CardFrame> Frames { get; init; } = new();
}

/// <summary>
/// What a studio's looks wait for (plan 43 · LF5): whether the clerk has given the first frame, and the card's own facts
/// (a region's Badges, its Hall of Fame…), which a studio look may wait for too.
/// </summary>
public sealed record StudioFacts
{
    public bool FrameGiven { get; init; }
    public CardFacts Card { get; init; } = CardFacts.None;
}

/// <summary>
/// The photo studio's looks (plan 43 · LF5; style guide, "The photo studio"): <c>Data/photo-studio.json</c>'s backdrops and
/// frames. A look's <c>unlock</c> is <c>start</c>, <c>studio:gift</c> (the clerk's gift of a first visit) or any of the
/// card's (<see cref="CardLooks.Unlocked"/>). The Fashion Case's backdrops owned are the studio's too, as they are the
/// card's (<c>backdrop:&lt;n&gt;</c>). GPU-free.
/// </summary>
public static class StudioLooks
{
    public const string FileName = "photo-studio.json";
    public const string Gift = "studio:gift";

    /// <summary>The frame the clerk gives on a first visit.</summary>
    public const string GiftFrame = "studio_gilt";

    private static readonly Lazy<StudioFile> file = new(() => GameDataFiles.Load<StudioFile>(FileName, strict: true));

    public static IReadOnlyList<CardBackground> Backdrops => file.Value.Backdrops;
    public static IReadOnlyList<CardFrame> Frames => file.Value.Frames;

    public static CardBackground? Backdrop(string? id) => Backdrops.FirstOrDefault(b => b.Id == id);
    public static CardFrame? Frame(string? id) => Frames.FirstOrDefault(f => f.Id == id);

    public static bool Unlocked(string unlock, StudioFacts facts) =>
        unlock == Gift ? facts.FrameGiven : CardLooks.Unlocked(unlock, facts.Card);

    public static bool IsWellFormed(string unlock) => unlock == Gift || CardLooks.IsWellFormed(unlock);

    public static List<string> BackdropsOpen(StudioFacts facts, IEnumerable<int>? fashionBackdrops = null) =>
        Backdrops.Where(b => Unlocked(b.Unlock, facts)).Select(b => b.Id)
            .Concat((fashionBackdrops ?? Enumerable.Empty<int>()).OrderBy(n => n).Select(n => CardLooks.BackdropPrefix + n)).ToList();

    public static List<string> FramesOpen(StudioFacts facts) => Frames.Where(f => Unlocked(f.Unlock, facts)).Select(f => f.Id).ToList();
}

/// <summary>
/// A studio photograph's composition (plan 43 · LF5): the backdrop and frame, the player's pose and face, which Pokémon
/// of the team stands beside them and on which side, and up to <see cref="MostStickers"/> stickers of Seals the player
/// owns. Kept with its print in the photo book. Plain data, GPU-free.
/// </summary>
public sealed record StudioShot
{
    /// <summary>The most stickers on one picture.</summary>
    public const int MostStickers = 12;

    public string Backdrop { get; init; } = "plain";
    public string Frame { get; init; } = "plain";
    public CardPoseKind Pose { get; init; }
    public Gesture? Gesture { get; init; }
    public Mood Mood { get; init; }

    /// <summary>The Pokémon beside the player by <see cref="Pokemon.Id"/>; empty for none.</summary>
    public Guid PokemonId { get; init; }
    public StudioPlace Place { get; init; }
    public List<StudioSticker> Stickers { get; init; } = new();

    /// <summary>The Pokémon a photograph may hold: of the team and no Egg.</summary>
    public static bool MayStand(Pokemon? pokemon) => pokemon != null && !pokemon.IsEgg;

    /// <summary>Why a Seal can't be put on the picture; null where it can: not owned, the picture full, or placed off the picture.</summary>
    public string? WhyNotSticker(int seal, float x, float y, SealCase seals)
    {
        if (!SealDatabase.Exists(seal) || seals.Anywhere(seal) == 0) return "notOwned";
        if (Stickers.Count >= MostStickers) return "full";
        if (x < 0f || x > 1f || y < 0f || y > 1f) return "outside";
        return null;
    }

    /// <summary>The composition with a sticker added; unchanged where <see cref="WhyNotSticker"/> refuses. A Seal is not used up: the case keeps its count.</summary>
    public StudioShot WithSticker(int seal, float x, float y, SealCase seals, float size = 1f, int turn = 0) =>
        WhyNotSticker(seal, x, y, seals) != null ? this : this with { Stickers = Stickers.Append(new StudioSticker(seal, x, y, size, ((turn % StudioSticker.Turns) + StudioSticker.Turns) % StudioSticker.Turns)).ToList() };

    /// <summary>The composition without its sticker at <paramref name="index"/>.</summary>
    public StudioShot WithoutSticker(int index) =>
        index < 0 || index >= Stickers.Count ? this : this with { Stickers = Stickers.Where((_, i) => i != index).ToList() };

    /// <summary>The poses offered, as the card's: standing, sitting, then each gesture held.</summary>
    public static IReadOnlyList<(CardPoseKind Kind, Gesture? Gesture)> Poses => CardDesign.Poses;

    public int PoseIndex
    {
        get
        {
            for (int i = 0; i < Poses.Count; i++)
                if (Poses[i].Kind == Pose && (Pose != CardPoseKind.Gesture || Poses[i].Gesture == Gesture)) return i;
            return 0;
        }
    }

    public StudioShot WithPose(int index)
    {
        var (kind, gesture) = Poses[((index % Poses.Count) + Poses.Count) % Poses.Count];
        return this with { Pose = kind, Gesture = kind == CardPoseKind.Gesture ? gesture : null };
    }

    /// <summary>The composition with the stickers a Seal no longer owned dropped (the case spent them on a capsule's burst or the like).</summary>
    public StudioShot Kept(SealCase seals) => this with { Stickers = Stickers.Where(s => seals.Anywhere(s.Seal) != 0).ToList() };
}
