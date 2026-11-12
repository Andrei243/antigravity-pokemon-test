using System;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.OpenWorld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Paldea's mount in an open map (plan 30 · FM12, built as plan 34 · PD2; rulings, "The mount on the open world"): the
/// Mount key gets on and off where <see cref="MountRules.WhyNot"/> lets it, the story's powers are handed to the body
/// while it is mounted, a follower goes into its ball, and every fade over an open field and every script that begins
/// in one takes the player off. No region's file lists <c>Mount</c> today, so nothing here happens in a game of today.
/// </summary>
public partial class GameEngine
{
    /// <summary>Whether the player rides the mount (in an open map; never on the grid).</summary>
    internal bool OpenMounted => flow.Open?.Player.Body.Mounted == true;

    /// <summary>Why the player can't get on now (plan 30 · FM12).</summary>
    internal MountRefusal WhyNotMountNow() => MountRules.WhyNot(new MountAsk(
        MovementHere.Offers(RegionMode.Mount),
        flow.Open != null,
        flow.Story.Var(MountRules.MountVariable),
        ScriptRunning,
        partner != null,
        flow.Open?.Player.Body.Stance ?? OpenStance.Standing));

    /// <summary>
    /// The Mount key: on where the rules allow, off where the body stands on the ground; a refusal is told on the toast.
    /// Returns whether the player changed.
    /// </summary>
    internal bool ToggleMount()
    {
        if (flow.Open is not { } field) return false;
        var reason = OpenMounted ? MountRules.WhyNotOff(field.Player.Body.Stance) : WhyNotMountNow();
        if (reason != MountRefusal.None)
        {
            flow.Toast.Show(Text.Of($"mount.refused.{reason}"));
            return false;
        }
        if (OpenMounted) GetOffTheMount();
        else GetOnTheMount(field);
        return true;
    }

    private void GetOnTheMount(OpenField field)
    {
        field.Player.Body.Mount();
        field.Player.RideGrants = MountRules.Grants(flow.Story);
        // A Pokémon following the player goes back into its ball (plan 30, "Followers")
        field.Follower = null;
    }

    /// <summary>The player off the mount, wherever the body is (a fade's, a script's, the key's), and whoever follows out again behind them.</summary>
    private void GetOffTheMount()
    {
        if (flow.Open is not { } field || !field.Player.Body.Mounted) return;
        field.Player.Body.Dismount();
        field.Follower = OpenFollowerNow(field);
    }

    /// <summary>The mount's frame: the powers the story has granted handed to the body, before its ticks.</summary>
    private void KeepTheMountsPowers(OpenField field) =>
        field.Player.RideGrants = field.Player.Body.Mounted ? MountRules.Grants(flow.Story) : MovementGrants.None;

    /// <summary>A fade's midpoint over an open field takes the player off the mount first (plan 30 · FM12: battles, doors, warps, crossings).</summary>
    private Action? OffTheMountBehind(Action? onMidpoint)
    {
        if (!OpenMounted) return onMidpoint;
        return () =>
        {
            GetOffTheMount();
            // No dismount is played behind the fade, and the camera is on foot at once (plan 30 · FM13)
            openMount.Reset();
            openCamera.OffTheMount();
            onMidpoint?.Invoke();
        };
    }

    /// <summary>Whether a save taken up in an open map may wake mounted: the movement in force lists the mount and one is had.</summary>
    private bool MayWakeMounted() =>
        MovementHere.Offers(RegionMode.Mount) && MountRules.SpeciesOf(flow.Story.Var(MountRules.MountVariable)) != null;

    internal sealed partial class Driver
    {
        /// <summary>Whether the player rides the mount in the open map (plan 30 · FM12).</summary>
        public bool OpenMounted => game.OpenMounted;

        /// <summary>Why the player can't get on now (plan 30 · FM12).</summary>
        public MountRefusal WhyNotMount => game.WhyNotMountNow();

        /// <summary>The Mount key pressed: on or off, or told why not.</summary>
        public bool ToggleMount() => game.ToggleMount();
    }
}
