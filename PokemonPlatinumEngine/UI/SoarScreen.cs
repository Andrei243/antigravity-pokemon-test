using System;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// Soaring on the screen (plan 38 · EX7, part a; style guide, "Soaring"): a flight over a region (<see cref="Soaring"/>)
/// with the Pokémon that carries the player, its clock and the place chosen to land at. Its logic takes no input
/// (<see cref="Steer"/>, <see cref="Confirm"/>, <see cref="Cancel"/>, <see cref="Advance"/>), so tests and the harness fly
/// it; <see cref="Update"/> reads the arrows, Run, A and B. The sky itself is <c>Graphics/SkyRenderer</c>'s.
/// </summary>
public sealed class SoarScreen
{
    private TownMapSpot? landing;
    private SkyPlace? placeLanding;

    public bool IsActive { get; private set; }

    /// <summary>The flight; null until the screen is first opened.</summary>
    public Soaring? Flight { get; private set; }

    /// <summary>The Pokémon that carries the player: whose Fly it is, or the helper the field lends.</summary>
    public Pokemon? Flier { get; private set; }

    /// <summary>How long the flight has gone on, in seconds (the beacons' pulse, the wings' beat, the clouds' drift).</summary>
    public float Age { get; private set; }

    /// <summary>How the flier banks into a turn, from -1 (left) to 1 (right), eased (the rider is drawn banked by up to 20°).</summary>
    public float Bank { get; private set; }

    /// <summary>Whether the flight was ended by Cancel, at the nearest open place, rather than over a beacon.</summary>
    public bool Turned { get; private set; }

    public void Open(Soaring flight, Pokemon flier)
    {
        Flight = flight;
        Flier = flier;
        Age = 0f;
        Bank = 0f;
        landing = null;
        placeLanding = null;
        Turned = false;
        IsActive = true;
    }

    /// <summary>The place of the day under the flier now, or null (plan 38 · EX7, part b).</summary>
    public SkyPlace? PlaceUnder => Flight?.PlaceUnder;

    /// <summary>The beacon under the flier now, or null.</summary>
    public TownMapSpot? Under => Flight?.BeaconUnder;

    /// <summary>A place's name as the language shows it, from the region's block under its first cell.</summary>
    public string NameOf(TownMapSpot spot)
    {
        var (x, y) = spot.Spot.Cells[0];
        return Flight?.Map.Region.BlockAt(x, y) is { } block ? LocalNames.PlaceShown(null, block.Name) : spot.Town.Area;
    }

    /// <summary>A place of the day's name: its kind's (plan 38 · EX7, part b).</summary>
    public static string NameOf(SkyPlace place)
    {
        var kind = place.Kind;
        return Text.Of($"soar.place.{kind}");
    }

    /// <summary>One step of the flight, as the keys would steer it (see <see cref="Soaring.Step"/>).</summary>
    public void Steer(float turn, float climb, bool fast, float dt)
    {
        if (!IsActive || Flight == null) return;
        Flight.Step(turn, climb, fast, dt);
        Bank += (Math.Clamp(turn, -1f, 1f) - Bank) * Math.Min(1f, dt * 6f);
    }

    /// <summary>The screen's clocks moved on; no input.</summary>
    public void Advance(float dt)
    {
        if (IsActive) Age += dt;
    }

    /// <summary>A: lands at the beacon under the flier; a sound of no where there is none.</summary>
    public void Confirm()
    {
        if (!IsActive || Flight == null) return;
        if (Flight.Land() is { } spot)
        {
            landing = spot;
            IsActive = false;
            AudioManager.PlaySound("select");
        }
        else if (Flight.PlaceUnder is { } place)
        {
            placeLanding = place;
            IsActive = false;
            AudioManager.PlaySound("select");
        }
        else AudioManager.PlaySound("error");
    }

    /// <summary>B: the flight ends at the open place nearest the flier.</summary>
    public void Cancel()
    {
        if (!IsActive || Flight == null) return;
        landing = Flight.Nearest();
        Turned = true;
        IsActive = false;
        AudioManager.PlaySound("cancel");
    }

    /// <summary>The place the flight ends at, once.</summary>
    public TownMapSpot? TakeLanding()
    {
        var spot = landing;
        landing = null;
        return spot;
    }

    /// <summary>The place of the day the flight ends at, once (plan 38 · EX7, part b).</summary>
    public SkyPlace? TakePlace()
    {
        var place = placeLanding;
        placeLanding = null;
        return place;
    }

    /// <summary>Lets go of the flight (the screen is put away).</summary>
    public void Close()
    {
        IsActive = false;
        Flight = null;
        Flier = null;
        landing = null;
        placeLanding = null;
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        Advance(dt);
        var stick = InputManager.GetStick();
        float turn = (InputManager.IsActionDown(GameAction.Right) ? 1f : 0f) - (InputManager.IsActionDown(GameAction.Left) ? 1f : 0f);
        float climb = (InputManager.IsActionDown(GameAction.Up) ? 1f : 0f) - (InputManager.IsActionDown(GameAction.Down) ? 1f : 0f);
        if (turn == 0f) turn = stick.X;
        if (climb == 0f) climb = -stick.Y;
        Steer(turn, climb, InputManager.IsActionDown(GameAction.Run), dt);
        if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
    }

    public void Draw(int sw, int sh)
    {
        if (Flight != null) ModernUi.DrawSoaring(sw, sh, this);
    }
}
