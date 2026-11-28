using System;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where the loop catch's screen is.</summary>
public enum LoopPhase { Brief, Playing, Over }

/// <summary>
/// The loop catch's screen (plan 44 · SD10; style guide, "The loop catch"): the brief, the try played a tick at a time
/// and how it ended. GPU-free: <see cref="Confirm"/>, <see cref="Cancel"/> and <see cref="Advance"/> take the disc's
/// input as plain values.
/// </summary>
public sealed class LoopScreen
{
    /// <summary>How long the end stands before it may be put away.</summary>
    public const float EndHold = 0.6f;

    public bool IsActive { get; private set; }
    public LoopPhase Phase { get; private set; }
    public float PhaseTime { get; private set; }
    public LoopCatch? Game { get; private set; }
    /// <summary>Whether the try ended with the Pokémon calmed.</summary>
    public bool Calmed => Game?.Outcome == LoopOutcome.Calmed;

    /// <summary>The autopilot's hand on the disc (a tool's: the harness's shots).</summary>
    public bool Autopilot { get; set; }

    // The looks of what has just happened, as seconds since it did (large for never)
    public float SinceLoop { get; private set; } = 99f;
    public float SinceStrike { get; private set; } = 99f;
    public float SinceBreak { get; private set; } = 99f;
    /// <summary>Where the Pokémon stood and the line lay as the last strike landed.</summary>
    public System.Numerics.Vector2 StruckAt { get; private set; }
    public System.Collections.Generic.List<System.Numerics.Vector2> Broken { get; } = new();
    /// <summary>What the ticks of the last <see cref="Advance"/> brought, for the sounds.</summary>
    public LoopEvents Happened { get; private set; }

    private float carry;
    private bool armed;
    private LoopAutopilot hand = new();

    public void Open(LoopCatch game)
    {
        Game = game;
        IsActive = true;
        Autopilot = false;
        SinceLoop = SinceStrike = SinceBreak = 99f;
        Broken.Clear();
        Happened = LoopEvents.None;
        Begin(LoopPhase.Brief);
    }

    private void Begin(LoopPhase phase)
    {
        Phase = phase;
        PhaseTime = 0f;
        carry = 0f;
    }

    public void Confirm()
    {
        switch (Phase)
        {
            case LoopPhase.Brief:
                // The press that begins the try draws nothing: the disc waits for the button to come up
                armed = false;
                hand = new LoopAutopilot();
                Begin(LoopPhase.Playing);
                break;
            case LoopPhase.Over:
                if (PhaseTime >= EndHold) IsActive = false;
                break;
        }
    }

    /// <summary>Back: out of the brief, the try given up, or the end put away.</summary>
    public void Cancel()
    {
        switch (Phase)
        {
            case LoopPhase.Brief:
                Game?.GiveUp();
                IsActive = false;
                break;
            case LoopPhase.Playing:
                Game?.GiveUp();
                Begin(LoopPhase.Over);
                break;
            case LoopPhase.Over:
                IsActive = false;
                break;
        }
    }

    /// <summary>The screen's time moved on: the try stepped a tick for each sixtieth of a second with the disc's input held.</summary>
    public void Advance(float dt, LoopInput input)
    {
        Happened = LoopEvents.None;
        if (!IsActive || Game is not { } game) return;
        SinceLoop += dt;
        SinceStrike += dt;
        SinceBreak += dt;
        PhaseTime += dt;
        if (Phase != LoopPhase.Playing) return;
        if (!input.Down) armed = true;
        carry += dt;
        while (carry >= LoopCatch.Tick && game.Outcome == LoopOutcome.Going)
        {
            carry -= LoopCatch.Tick;
            var told = Autopilot ? hand.Next(game) : input with { Down = input.Down && armed };
            var line = game.Line.Count > 1 ? new System.Collections.Generic.List<System.Numerics.Vector2>(game.Line) : null;
            var events = game.Step(told);
            Happened |= events;
            if ((events & LoopEvents.Looped) != 0) SinceLoop = 0f;
            if ((events & LoopEvents.Struck) != 0)
            {
                SinceStrike = 0f;
                StruckAt = game.At;
            }
            if ((events & LoopEvents.Broke) != 0)
            {
                SinceBreak = 0f;
                Broken.Clear();
                if (line != null) Broken.AddRange(line);
            }
        }
        if (game.Outcome != LoopOutcome.Going) Begin(LoopPhase.Over);
    }
}
