using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The shots a manner's idle cycle films the menu with (plan 45 · BC2; style guide, "The battle's manners"): the
/// overview, which every cycle begins on, then shots of ours, each held for the manner's time.
/// </summary>
internal enum IdleShot
{
    /// <summary>The overview, as the menu has always been filmed.</summary>
    Overview,
    /// <summary>Low behind the player's Pokémon: its back and the foe beyond it, the foe in the picture's upper middle.</summary>
    Shoulder,
    /// <summary>The foe from the front, close, turned a little.</summary>
    FoeFace,
    /// <summary>The player's own Pokémon from the front: a camera between the two platforms looking south, cut to and from.</summary>
    OwnFace,
    /// <summary>Both platforms from the side, the two facing each other across the picture.</summary>
    Side,
    /// <summary>High over the stage, both sides and the ground (Galar's Leaders and Champions, plan 45 · BC5).</summary>
    Crane,
    /// <summary>The overview's own stand, its lens narrowed onto the foe: a slow push in and back with no move of the camera (Kanto's, Johto's and Hoenn's, BC6).</summary>
    FoeZoom,
}

/// <summary>When a manner splits the picture in two (plan 45 · BC3): none, or any of these.</summary>
[Flags]
internal enum CameraSplits
{
    None = 0,
    /// <summary>The leads' send-outs of a trainer's battle.</summary>
    Leads = 1,
    /// <summary>Every send-out of a trainer's battle.</summary>
    SendOuts = 2,
    /// <summary>Any trainer's last Pokémon coming out.</summary>
    LastPokemon = 4,
    /// <summary>A Gym Leader's, an Elite Four's or a Champion's last Pokémon coming out.</summary>
    BossLast = 8,
    /// <summary>Both sides with a giant at once.</summary>
    Giants = 16,
    /// <summary>A Pokémon Terastallizing facing its foe.</summary>
    Terastal = 32,
}

/// <summary>
/// A way of filming a battle (plan 45; style guide, "The battle's manners"): what the battle's camera does beyond the
/// director's plain shots, each a number or a switch, nought or off doing nothing. A region's file names its manner
/// (<c>battleCamera</c>) and the options' BATTLE CAMERA row chooses between that, Galar's everywhere and
/// <see cref="CameraManners.Classic"/>, whose every switch is off: the director as it was before the plan, to the bit.
/// GPU-free, and read by <see cref="BattleCamera.Direct"/> alone; nothing of it reaches the rules.
/// </summary>
internal sealed record CameraManner(string Name)
{
    /// <summary>
    /// The menu's breathing overview (BC2, Sinnoh's): how far round the field the overview drifts either way while a menu
    /// is open, radians; nought for none. It never leaves the overview's side.
    /// </summary>
    public float Drift { get; init; }

    /// <summary>One breath of <see cref="Drift"/>, seconds of the menu's open time.</summary>
    public float DriftPeriod { get; init; } = 18f;

    /// <summary>
    /// The idle cycle (BC2): the shots the menu is filmed with in turn, the first the overview; none for no cycle. A
    /// cycle and a drift are never both a manner's.
    /// </summary>
    public IReadOnlyList<IdleShot> Cycle { get; init; } = Array.Empty<IdleShot>();

    /// <summary>How long each of the cycle's shots is held, seconds of the menu's open time (the ease into the next not counted).</summary>
    public float CycleHold { get; init; } = 3.5f;

    /// <summary>How long the cycle eases from one shot into the next, seconds (a shot of the player's own front is cut to, never eased).</summary>
    public float CycleEase { get; init; } = 1.4f;

    /// <summary>How far the cycle's shots reach, a share of their own: 1, or less for a closer, quieter cycle (Hisui's, BC6).</summary>
    public float Reach { get; init; } = 1f;

    /// <summary>How much lower the cycle's shots stand, radians of pitch taken off (Alola's and Hisui's, BC6).</summary>
    public float Low { get; init; }

    /// <summary>A move's shots push in on the user and pull out past the target (BC4): nought for none, 1 for Galar's whole push.</summary>
    public float Push { get; init; }

    /// <summary>A short shot of the one hit after a super-effective or critical hit or a faint (BC4).</summary>
    public bool React { get; init; }

    /// <summary>The battle opens with shots of its own after the way into it (BC5).</summary>
    public bool Open { get; init; }

    /// <summary>The battle closes with shots of its own: a win's face-on shot of the player's Pokémon and trainer (BC5).</summary>
    public bool Close { get; init; }

    /// <summary>A Leader's or a Champion's battle opens and closes with the crane over the stage (BC5, Galar's).</summary>
    public bool Crane { get; init; }

    /// <summary>When the picture splits in two (BC3).</summary>
    public CameraSplits Splits { get; init; }

    /// <summary>How far the cycle's aim sways in the hand (BC6, Paldea's), radians; nought for none. Never a roll.</summary>
    public float Handheld { get; init; }

    /// <summary>True when the manner does nothing beyond the director's plain shots: Classic, or a manner with every switch off.</summary>
    public bool IsStill => Drift == 0f && Cycle.Count == 0 && Push == 0f && !React && !Open && !Close && !Crane
        && Splits == CameraSplits.None && Handheld == 0f;
}

/// <summary>
/// The manners (plan 45; style guide, "The battle's manners"), one to a row of the style guide's table, found by the
/// name a region's file gives (<c>battleCamera</c>). GPU-free.
/// </summary>
internal static class CameraManners
{
    /// <summary>The director as it was before plan 45, to the bit: every switch off. The options' CLASSIC, and every manner under REDUCE MOTION.</summary>
    public static readonly CameraManner Classic = new("classic");

    /// <summary>
    /// Sinnoh's, Platinum's own seat: today's shots with a breathing overview (BC2), then the opening and the closing
    /// (BC5, still to come) and a split for a Leader's, an Elite Four's or a Champion's last Pokémon (BC3).
    /// The manner of a region whose file names none.
    /// </summary>
    public static readonly CameraManner Sinnoh = new("sinnoh")
    {
        Drift = 0.07f, DriftPeriod = 18f, Splits = CameraSplits.BossLast,
        Open = true, Close = true,
    };

    /// <summary>
    /// Unova's (Black and White moved their camera between commands): a slow swing between the overview and a shot low
    /// behind the player's Pokémon looking at the foe (BC2), and a split as the leads face each other (BC3), and half Galar's push on a move's shots (BC4).
    /// </summary>
    public static readonly CameraManner Unova = new("unova")
    {
        Cycle = new[] { IdleShot.Overview, IdleShot.Shoulder },
        CycleHold = 4.5f, CycleEase = 2.5f,
        Splits = CameraSplits.Leads,
        Push = 0.5f,
        Open = true, Close = true,
    };

    /// <summary>
    /// Galar's, and the options' LIVELY: the camera never rests. At the menu, the cycle through the side, over the
    /// shoulder, the foe's face and the player's own front (BC2), and a split at the leads and at any trainer's last Pokémon
    /// (BC3), every move pushed in on its user and pulled out past its target with a reaction after a
    /// super-effective or critical blow (BC4); its opening, closing and crane come with BC5.
    /// </summary>
    public static readonly CameraManner Galar = new("galar")
    {
        Cycle = new[] { IdleShot.Overview, IdleShot.Side, IdleShot.Shoulder, IdleShot.FoeFace, IdleShot.Crane, IdleShot.OwnFace },
        CycleHold = 3.5f, CycleEase = 1.4f,
        Splits = CameraSplits.Leads | CameraSplits.SendOuts | CameraSplits.LastPokemon | CameraSplits.Giants,
        Push = 1f, React = true,
        Open = true, Close = true, Crane = true,
    };

    /// <summary>
    /// Kanto's, Johto's and Hoenn's (BC6; games filmed flat, the quietest manner with something new): the overview held
    /// at the menu with a slow push of its own lens in on the foe and back (<see cref="IdleShot.FoeZoom"/>), the opening and
    /// the closing; no splits, no push on a move.
    /// </summary>
    public static readonly CameraManner Kanto = Flat("kanto", 6f, 3.5f);
    public static readonly CameraManner Johto = Flat("johto", 5.5f, 3.5f);
    public static readonly CameraManner Hoenn = Flat("hoenn", 5f, 3f);

    private static CameraManner Flat(string name, float hold, float ease) => new(name)
    {
        Cycle = new[] { IdleShot.Overview, IdleShot.FoeZoom },
        CycleHold = hold, CycleEase = ease,
        Open = true, Close = true,
    };

    /// <summary>
    /// Kalos's (BC6; the first fully 3D generation): a cycle through over the shoulder, the foe's face and the side, a
    /// move pushed in a little (<see cref="CameraManner.Push"/> 0.6) with its target's shot standing low as it is hit
    /// (<see cref="CameraManner.Low"/>), the opening and the closing, and a split at the leads and at the last Pokémon.
    /// </summary>
    public static readonly CameraManner Kalos = new("kalos")
    {
        Cycle = new[] { IdleShot.Overview, IdleShot.Shoulder, IdleShot.FoeFace, IdleShot.Side },
        CycleHold = 4f, CycleEase = 1.8f,
        Low = 0.06f, Push = 0.6f,
        Open = true, Close = true,
        Splits = CameraSplits.Leads | CameraSplits.LastPokemon,
    };

    /// <summary>Alola's (BC6): Kalos's, its shots standing lower and wider (<see cref="CameraManner.Reach"/> 1.15).</summary>
    public static readonly CameraManner Alola = Kalos with
    {
        Name = "alola",
        Cycle = new[] { IdleShot.Overview, IdleShot.Side, IdleShot.Shoulder, IdleShot.FoeFace },
        Reach = 1.15f, Low = 0.12f,
    };

    /// <summary>
    /// Hisui's (BC6; Legends: Arceus's battles were close and low): Galar's cycle at a smaller reach and lower, its push
    /// and reactions, the opening and the closing; no splits and no crane.
    /// </summary>
    public static readonly CameraManner Hisui = Galar with
    {
        Name = "hisui",
        Cycle = new[] { IdleShot.Overview, IdleShot.Side, IdleShot.Shoulder, IdleShot.FoeFace, IdleShot.OwnFace },
        Reach = 0.85f, Low = 0.1f,
        Splits = CameraSplits.None, Crane = false,
    };

    /// <summary>
    /// Paldea's (BC6): Galar's with the cycle's aim held in the hand (<see cref="CameraManner.Handheld"/>, a sway of our
    /// own noise of the battle's clock, never a roll) and a split as a Pokémon Terastallizes facing its foe.
    /// </summary>
    public static readonly CameraManner Paldea = Galar with
    {
        Name = "paldea",
        Handheld = 0.012f,
        Splits = Galar.Splits | CameraSplits.Terastal,
    };

    /// <summary>Every manner of the table, by its name.</summary>
    public static IReadOnlyList<CameraManner> All { get; } = new[] { Classic, Kanto, Johto, Hoenn, Sinnoh, Unova, Kalos, Alola, Galar, Hisui, Paldea };

    /// <summary>The manner of the name (case ignored); null for a name the table lacks.</summary>
    public static CameraManner? Find(string? name) =>
        name is null ? null : All.FirstOrDefault(m => string.Equals(m.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// A region file's <c>battleCamera</c> read: its manner, Sinnoh's where the file names none. A name the table lacks
    /// throws, so a misspelling can't leave a region filmed as another unnoticed.
    /// </summary>
    public static CameraManner OfRegion(string? name) =>
        string.IsNullOrWhiteSpace(name) ? Sinnoh
            : Find(name) ?? throw new FormatException($"'battleCamera' names '{name}', which is no manner of the table ({string.Join(", ", All.Select(m => m.Name))})");

    /// <summary>
    /// The manner a battle is filmed in: the options' choice over the region's own (<paramref name="regionManner"/>,
    /// its file's <c>battleCamera</c>). LIVELY is Galar's everywhere (Sinnoh's until Galar's row exists). A name the
    /// table lacks films as Sinnoh does here, in the middle of a battle; the pack check and the tests hold the files to
    /// <see cref="OfRegion"/>, which refuses it.
    /// </summary>
    public static CameraManner For(BattleCameraChoice choice, string? regionManner) => choice switch
    {
        BattleCameraChoice.Classic => Classic,
        BattleCameraChoice.Lively => Find("galar") ?? Sinnoh,
        _ => Find(regionManner) ?? Sinnoh,
    };
}
