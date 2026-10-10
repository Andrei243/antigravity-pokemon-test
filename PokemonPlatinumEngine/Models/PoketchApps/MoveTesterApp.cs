using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>
/// The Move Tester (<c>move_tester/main.c</c>): a move's type against a Pokémon's one or two types, each changed
/// with the arrows either side of it, and how well the move works as a row of up to five marks: none for no effect,
/// one for a quarter, two for a half, three for normal, four for double and five for four times
/// (<c>GetExclamationCount</c>). The chart is the game's own (<see cref="TypeChart"/>, by the rules the game is
/// played by), which is Platinum's in a Platinum game as the original's table is. What was chosen is kept for the
/// next time (<c>PoketchMemory_Write32</c>).
/// </summary>
public sealed class MoveTesterApp : PoketchAppState
{
    /// <summary>The arrows (<c>sHitTableMoveTester</c>): the move's type, the first type and the second, each down and up.</summary>
    public const int AttackDown = 0, AttackUp = 1, FirstDown = 2, FirstUp = 3, SecondDown = 4, SecondUp = 5;

    private static readonly PoketchButton[] buttons =
    {
        new(AttackDown, 1, 22, 5, 8), new(AttackUp, 21, 22, 5, 8),
        new(FirstDown, 19, 2, 5, 7), new(FirstUp, 39, 2, 5, 7),
        new(SecondDown, 19, 10, 5, 7), new(SecondUp, 39, 10, 5, 7)
    };

    /// <summary>The order the types are gone through (<c>sMoveTesterTypeOrder</c>): Platinum's seventeen, no Fairy.</summary>
    public static readonly PokemonType[] Order =
    {
        PokemonType.Normal, PokemonType.Fire, PokemonType.Water, PokemonType.Electric, PokemonType.Grass,
        PokemonType.Ice, PokemonType.Fighting, PokemonType.Poison, PokemonType.Ground, PokemonType.Flying,
        PokemonType.Psychic, PokemonType.Bug, PokemonType.Rock, PokemonType.Ghost, PokemonType.Dragon,
        PokemonType.Dark, PokemonType.Steel
    };

    /// <summary>The most marks shown.</summary>
    public const int MostMarks = 5;

    public const float PressSeconds = 0.2f;

    public override PoketchApp App => PoketchApp.MoveTester;

    public PokemonType Attack { get; private set; } = PokemonType.Normal;
    public PokemonType First { get; private set; } = PokemonType.Normal;

    /// <summary>The second type; null for none (<c>MOVE_TESTER_NONE_SELECTED</c>).</summary>
    public PokemonType? Second { get; private set; }

    /// <summary>The arrow shown pressed in, and for how long more.</summary>
    public int Pressed { get; private set; } = -1;
    public float PressedFor { get; private set; }

    /// <summary>The marks for what is chosen now.</summary>
    public int Marks => MarksFor(Attack, First, Second, Rules);

    /// <summary>The rules whose chart it reads: the game's (tests may give their own).</summary>
    public Ruleset? Rules { get; set; }

    /// <summary>
    /// The marks for a move of one type against one or two (<c>GetExclamationCount</c>): none if either type takes
    /// nothing from it, else three, one more for each type it is strong against and one fewer for each that
    /// resists it; a second type the same as the first counts once.
    /// </summary>
    public static int MarksFor(PokemonType attack, PokemonType first, PokemonType? second, Ruleset? rules = null)
    {
        float multiplier = TypeChart.GetEffectiveness(attack, first, second, rules);
        if (multiplier <= 0f) return 0;
        return Math.Clamp(3 + (int)MathF.Round(MathF.Log2(multiplier)), 1, MostMarks);
    }

    public override IReadOnlyList<PoketchButton> Buttons(PoketchContext context) => buttons;

    protected override void Opened()
    {
        if (Owner.Recall(App) is { Count: >= 3 } kept)
        {
            Attack = Known(kept[0]) ?? PokemonType.Normal;
            First = Known(kept[1]) ?? PokemonType.Normal;
            Second = Known(kept[2]);
        }
    }

    private static PokemonType? Known(int value) =>
        Array.IndexOf(Order, (PokemonType)value) >= 0 ? (PokemonType)value : null;

    private void Keep() => Owner.Keep(App, new List<int> { (int)Attack, (int)First, Second is { } s ? (int)s : -1 });

    public override void Press(int button, PoketchContext context)
    {
        switch (button)
        {
            case AttackDown: Attack = Shift(Attack, -1, false)!.Value; break;
            case AttackUp: Attack = Shift(Attack, 1, false)!.Value; break;
            case FirstDown: First = Shift(First, -1, false)!.Value; break;
            case FirstUp: First = Shift(First, 1, false)!.Value; break;
            case SecondDown: Second = Shift(Second, -1, true); break;
            case SecondUp: Second = Shift(Second, 1, true); break;
            default: return;
        }
        Pressed = button;
        PressedFor = PressSeconds;
        Keep();
        context.Sound("poketch");
    }

    public override void Update(float dt, PoketchContext context)
    {
        PressedFor = Math.Max(0f, PressedFor - dt);
        if (PressedFor <= 0f) Pressed = -1;
    }

    /// <summary>
    /// The type before or after one in <see cref="Order"/> (<c>GetTypeAfterShift</c>): round again for the move's type
    /// and the first; the second goes to none past either end, and from none to the first or the last.
    /// </summary>
    public static PokemonType? Shift(PokemonType? current, int shift, bool second)
    {
        int index = current is { } c ? Array.IndexOf(Order, c) : -1;
        if (index < 0) return shift > 0 ? Order[0] : Order[^1];
        index += shift;
        if (index >= Order.Length)
        {
            if (second) return null;
            index = 0;
        }
        if (index < 0)
        {
            if (second) return null;
            index = Order.Length - 1;
        }
        return Order[index];
    }
}
