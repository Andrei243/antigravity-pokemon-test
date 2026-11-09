using System.Collections.Generic;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>Why the player can't get on the mount (or off it) now, the first that holds (plan 30 · FM12).</summary>
public enum MountRefusal
{
    None,

    /// <summary>The movement in force doesn't list <c>Mount</c>: anywhere but Paldea.</summary>
    NotHere,

    /// <summary>On the grid: a room, a Gym, a place whose map entry bars the modes.</summary>
    OnTheGrid,

    /// <summary>No mount had yet (<see cref="MountRules.MountVariable"/> is nought).</summary>
    NoMount,

    /// <summary>A script has the field.</summary>
    Scripted,

    /// <summary>Someone travels beside the player who battles with them (plan 30: "not with company").</summary>
    Company,

    /// <summary>Not standing on the ground: falling, gliding, swimming or clinging to a face.</summary>
    NotOnTheGround
}

/// <summary>What the game knows when the player asks to get on (plan 30 · FM12): plain values, so the rule needs no game.</summary>
public readonly record struct MountAsk(bool ModeHere, bool InOpenField, int Mount, bool ScriptRunning, bool WithCompany, OpenStance Stance);

/// <summary>
/// Paldea's mount, its rules beside the capsule's (plan 30 · FM12, built as plan 34 · PD2; rulings, "The mount on the
/// open world"): which of the two is had (<see cref="MountVariable"/>), the powers granted (story flags, in plan 30 ·
/// decision 16's order), the form each stance is drawn in (FM13's rig), and whether the player may get on or off. How
/// the mount moves is <see cref="OpenBody"/>'s, while <see cref="OpenBody.Mounted"/>. GPU-free.
/// </summary>
public static class MountRules
{
    /// <summary>Which mount the story gave: <see cref="Koraidon"/>, <see cref="Miraidon"/>, or nought for none.</summary>
    public const string MountVariable = "VAR_PALDEA_MOUNT";

    public const int Koraidon = 1, Miraidon = 2;

    /// <summary>The powers and the flags that grant them, in the order the Titans give them (plan 34 · PD4).</summary>
    public static readonly IReadOnlyList<(MovementGrants Power, string Flag)> Powers = new[]
    {
        (MovementGrants.Dash, "FLAG_PALDEA_MOUNT_DASH"),
        (MovementGrants.Swim, "FLAG_PALDEA_MOUNT_SWIM"),
        (MovementGrants.HighJump, "FLAG_PALDEA_MOUNT_HIGH_JUMP"),
        (MovementGrants.Glide, "FLAG_PALDEA_MOUNT_GLIDE"),
        (MovementGrants.Climb, "FLAG_PALDEA_MOUNT_CLIMB"),
    };

    /// <summary>The mount's powers the story has granted: what the body is handed while mounted (<see cref="OpenPlayer.RideGrants"/>).</summary>
    public static MovementGrants Grants(StoryState story)
    {
        var grants = MovementGrants.None;
        foreach (var (power, flag) in Powers)
            if (story.Has(flag)) grants |= power;
        return grants;
    }

    /// <summary>The species of a mount, or null for none.</summary>
    public static string? SpeciesOf(int mount) => mount switch
    {
        Koraidon => "Koraidon",
        Miraidon => "Miraidon",
        _ => null
    };

    /// <summary>
    /// The form the mount is drawn in for a stance (plan 30, "The mount as a rig"): standing still its limited form,
    /// trotting and sprinting its running one, swimming its swimming one, gliding its gliding one, and clinging to a face
    /// its running one. Null for no mount.
    /// </summary>
    public static string? FormFor(int mount, OpenStance stance, bool moving) => (mount, stance) switch
    {
        (Koraidon, OpenStance.Swimming) => "Koraidon-Swimming-Build",
        (Koraidon, OpenStance.Gliding or OpenStance.Falling) => "Koraidon-Gliding-Build",
        (Koraidon, _) => moving || stance == OpenStance.Climbing ? "Koraidon-Sprinting-Build" : "Koraidon-Limited-Build",
        (Miraidon, OpenStance.Swimming) => "Miraidon-Aquatic-Mode",
        (Miraidon, OpenStance.Gliding or OpenStance.Falling) => "Miraidon-Glide-Mode",
        (Miraidon, _) => moving || stance == OpenStance.Climbing ? "Miraidon-Drive-Mode" : "Miraidon-Low-Power-Mode",
        _ => null
    };

    /// <summary>Why the player can't get on now, the first reason that holds; <see cref="MountRefusal.None"/> where they can.</summary>
    public static MountRefusal WhyNot(in MountAsk ask)
    {
        if (!ask.ModeHere) return MountRefusal.NotHere;
        if (!ask.InOpenField) return MountRefusal.OnTheGrid;
        if (SpeciesOf(ask.Mount) == null) return MountRefusal.NoMount;
        if (ask.ScriptRunning) return MountRefusal.Scripted;
        if (ask.WithCompany) return MountRefusal.Company;
        if (ask.Stance != OpenStance.Standing) return MountRefusal.NotOnTheGround;
        return MountRefusal.None;
    }

    /// <summary>Why the player can't get off by their own press: only on the ground (a fade or a script takes them off anywhere).</summary>
    public static MountRefusal WhyNotOff(OpenStance stance) =>
        stance == OpenStance.Standing ? MountRefusal.None : MountRefusal.NotOnTheGround;
}
