using System.Collections.Generic;

namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>
/// The pedometer (<c>pedometer/main.c</c>): the steps taken in five figures, and a button under them that puts
/// the count back to nought.
/// </summary>
public sealed class PedometerApp : PoketchAppState
{
    /// <summary>The reset button, in blocks.</summary>
    public static readonly PoketchButton Reset = new(0, 15, 27, 15, 6);

    private static readonly PoketchButton[] buttons = { Reset };

    public override PoketchApp App => PoketchApp.Pedometer;

    /// <summary>Seconds left of the button shown pressed in.</summary>
    public float PressedFor { get; private set; }

    public override IReadOnlyList<PoketchButton> Buttons(PoketchContext context) => buttons;

    public override void Press(int button, PoketchContext context)
    {
        context.Poketch.ResetSteps();
        PressedFor = 0.2f;
        context.Sound("poketch");
    }

    public override void Update(float dt, PoketchContext context) => PressedFor = System.Math.Max(0f, PressedFor - dt);
}
