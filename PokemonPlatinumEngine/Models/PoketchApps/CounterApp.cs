using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>
/// The counter (<c>counter/main.c</c>): four figures and one big button under them that counts one more each time
/// it is touched, round to nought after 9999 (<c>State_UpdateApp</c>). The original keeps the count in the
/// Pokétch's memory only while the app stays chosen (<c>PoketchMemory_Read32</c>, which the side button resets), so
/// here it lives with the app's state and goes when another app is chosen.
/// </summary>
public sealed class CounterApp : PoketchAppState
{
    /// <summary>The most it counts before going round to nought: four figures.</summary>
    public const int Most = 9999;

    /// <summary>The button, in blocks, under the figures.</summary>
    public static readonly PoketchButton Button = new(0, 13, 20, 19, 13);

    private static readonly PoketchButton[] buttons = { Button };

    public override PoketchApp App => PoketchApp.Counter;

    public int Value { get; private set; }

    /// <summary>Seconds left of the button shown pressed in.</summary>
    public float PressedFor { get; private set; }

    public override IReadOnlyList<PoketchButton> Buttons(PoketchContext context) => buttons;

    public override void Press(int button, PoketchContext context)
    {
        // Task_UpdateButtonSprite plays its click as the button goes down; the count goes on as it comes up
        context.Sound("poketch_count");
        PressedFor = 0.15f;
        if (++Value > Most) Value = 0;
    }

    public override void Update(float dt, PoketchContext context) => PressedFor = Math.Max(0f, PressedFor - dt);
}
