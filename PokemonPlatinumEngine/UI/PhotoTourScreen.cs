using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where a ride stands: opening from black once its models are made, riding, closing to black, the judged panel.</summary>
public enum TourPhase { Opening, Riding, Closing, Judged }

/// <summary>
/// The photo tour's viewfinder (plan 44 · SD5; style guide, "The photo tour"): a <see cref="TourRide"/> played at its own
/// thirty ticks a second from the time handed to <see cref="Advance"/>, the aim on the arrows (or the stick), the shutter
/// on Confirm, a fruit on the item button, the whistle on Run and the zoom on the page buttons; Start ends a ride early.
/// At the end the ride's best photograph of each species is judged into a panel; KEEP (the item button) asks the game to
/// keep the best of them in the photo book (<see cref="TakeKeepRequest"/>) and Confirm leaves. The logic takes no input
/// but what it is handed, so tests and the harness drive it; <see cref="Update"/> reads the keys.
/// </summary>
public sealed class PhotoTourScreen
{
    public const float OpenTime = 0.8f, CloseTime = 0.8f;

    private float carry;
    private bool shoot, throwFruit, whistle, keepWanted;
    private IReadOnlyList<TourBest> bests = Array.Empty<TourBest>();

    public bool IsActive { get; private set; }
    public TourPhase Phase { get; private set; }
    public TourRide? Ride { get; private set; }
    public float PhaseTime { get; private set; }

    /// <summary>The ride's best photograph of each species, judged, the best first (once it is over).</summary>
    public IReadOnlyList<TourBest> Shown { get; private set; } = Array.Empty<TourBest>();

    /// <summary>Every best of the ride, by species and stars (what the report is offered).</summary>
    public IReadOnlyList<TourBest> Bests => bests;

    /// <summary>The ride's total.</summary>
    public int Total { get; private set; }

    /// <summary>Whether the best photograph has been kept in the photo book this ride.</summary>
    public bool Kept { get; set; }

    /// <summary>True while the best photograph is drawn alone, for the game to take (no panel over it).</summary>
    public bool Taking { get; set; }

    /// <summary>A tool's hand on the ride: the autopilot plays it in place of the keys.</summary>
    public bool Autopilot { get; set; }

    private int ticksSinceShot = 999;

    /// <summary>How dark the picture is: 1 black, 0 clear.</summary>
    public float Dark => Phase switch
    {
        TourPhase.Opening => 1f - Math.Clamp(PhaseTime / OpenTime, 0f, 1f),
        TourPhase.Closing => Math.Clamp(PhaseTime / CloseTime, 0f, 1f),
        TourPhase.Judged => 1f,
        _ => 0f
    };

    public void Open(TourCourseData course, uint seed, int hour, TourWeather weather)
    {
        Ride = new TourRide(course, seed, hour, weather);
        IsActive = true;
        Phase = TourPhase.Opening;
        PhaseTime = 0f;
        carry = 0f;
        Shown = bests = Array.Empty<TourBest>();
        Total = 0;
        Kept = Taking = keepWanted = false;
        ticksSinceShot = 999;
    }

    public void Close()
    {
        IsActive = false;
        Ride = null;
    }

    /// <summary>The shutter pressed: taken at the next tick.</summary>
    public void Shoot() => shoot = true;

    /// <summary>A fruit thrown at the next tick.</summary>
    public void Throw() => throwFruit = true;

    /// <summary>The whistle blown at the next tick.</summary>
    public void Whistle() => whistle = true;

    /// <summary>The ride ended early (Start), judged as it stands.</summary>
    public void EndEarly()
    {
        if (Phase == TourPhase.Riding) Begin(TourPhase.Closing);
    }

    /// <summary>
    /// The screen's time moved on: the opening waits for <paramref name="ready"/> (every resident's model made), then the
    /// ride is stepped a tick for each thirtieth of a second with the aim held (turn, tilt and zoom, −1 to 1).
    /// </summary>
    public void Advance(float dt, float turn, float tilt, float zoom, bool ready)
    {
        if (!IsActive || Ride is not { } ride) return;
        switch (Phase)
        {
            case TourPhase.Opening:
                if (!ready) return;
                PhaseTime += dt;
                if (PhaseTime >= OpenTime) Begin(TourPhase.Riding);
                break;
            case TourPhase.Riding:
                carry += dt;
                while (carry >= TourRide.Tick && !ride.Over)
                {
                    carry -= TourRide.Tick;
                    // (with the autopilot's hand on the aim, a fruit or a whistle pressed by hand still goes)
                    var input = Autopilot ? TourAutopilot.Next(ride, ticksSinceShot) is var auto
                            ? auto with { Shoot = auto.Shoot || shoot, Throw = auto.Throw || throwFruit, Whistle = auto.Whistle || whistle } : default
                        : new TourInput(turn, tilt, zoom, shoot, throwFruit, whistle);
                    shoot = throwFruit = whistle = false;
                    ride.Step(input);
                    ticksSinceShot = input.Shoot ? 0 : ticksSinceShot + 1;
                }
                if (ride.Over) Begin(TourPhase.Closing);
                break;
            case TourPhase.Closing:
                PhaseTime += dt;
                if (PhaseTime >= CloseTime) Judge();
                break;
            case TourPhase.Judged:
                PhaseTime += dt;
                break;
        }
    }

    private void Begin(TourPhase phase)
    {
        Phase = phase;
        PhaseTime = 0f;
    }

    private void Judge()
    {
        bests = TourResults.BestOf(Ride!);
        Shown = TourResults.ShownOf(bests);
        Total = TourResults.TotalOf(bests);
        Begin(TourPhase.Judged);
    }

    /// <summary>The best photograph of the ride (what KEEP keeps); null with none.</summary>
    public TourBest? BestPhoto => Shown.Count > 0 ? Shown[0] : null;

    /// <summary>KEEP: the best photograph asked to be kept in the photo book, once a ride.</summary>
    public void Keep()
    {
        if (Phase == TourPhase.Judged && BestPhoto != null && !Kept) keepWanted = true;
    }

    /// <summary>Whether KEEP was pressed since the last asking (once).</summary>
    public bool TakeKeepRequest()
    {
        bool wanted = keepWanted;
        keepWanted = false;
        return wanted;
    }

    /// <summary>Confirm on the judged panel: the screen is left.</summary>
    public void Leave()
    {
        if (Phase == TourPhase.Judged) IsActive = false;
    }

    /// <summary>A frame of the game: the keys read and the screen moved on.</summary>
    public void Update(float dt, bool ready)
    {
        if (!IsActive) return;
        if (Phase == TourPhase.Riding)
        {
            if (InputManager.IsActionPressed(GameAction.Confirm)) Shoot();
            if (InputManager.IsActionPressed(GameAction.Item)) Throw();
            if (InputManager.IsActionPressed(GameAction.Run)) Whistle();
            if (InputManager.IsActionPressed(GameAction.Menu)) EndEarly();
        }
        else if (Phase == TourPhase.Judged)
        {
            if (InputManager.IsActionPressed(GameAction.Item)) Keep();
            else if (InputManager.IsActionPressed(GameAction.Confirm)) Leave();
        }
        var stick = InputManager.GetStick();
        float turn = InputManager.IsActionDown(GameAction.Right) ? 1f : InputManager.IsActionDown(GameAction.Left) ? -1f : stick.X;
        float tilt = InputManager.IsActionDown(GameAction.Up) ? 1f : InputManager.IsActionDown(GameAction.Down) ? -1f : -stick.Y;
        float zoom = InputManager.IsActionDown(GameAction.PageRight) ? 1f : InputManager.IsActionDown(GameAction.PageLeft) ? -1f : 0f;
        Advance(dt, turn, tilt, zoom, ready);
    }
}
