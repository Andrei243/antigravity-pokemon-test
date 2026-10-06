using System;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The banner a field move is used under (plan 02 · S2; style guide, "A field move's cut-in"), as the original's
/// HM cut-in: a band opens across the screen, the Pokémon runs in, holds with the move's name beside it, runs out,
/// and the band closes. A function of its own clock, so tests and the harness step through it without a window.
/// </summary>
public sealed class FieldCutIn
{
    public const float Opens = 0.18f, Enters = 0.3f, Holds = 0.45f, Leaves = 0.25f, Closes = 0.12f;
    public const float Total = Opens + Enters + Holds + Leaves + Closes;

    /// <summary>Whose sprite runs across: the Pokémon's model name (a form's own where it has one).</summary>
    public string Model { get; }

    /// <summary>The move's name, shown beside it.</summary>
    public string Move { get; }

    public FieldMove FieldMove { get; }

    public float Age { get; private set; }

    public FieldCutIn(string model, FieldMove move)
    {
        Model = model;
        FieldMove = move;
        Move = FieldMoveRules.MoveName(move);
    }

    public bool Done => Age >= Total;

    public void Advance(float dt) => Age = Math.Min(Total, Age + dt);

    /// <summary>How far the band is open, 0 to 1.</summary>
    public float Band
    {
        get
        {
            if (Age < Opens) return Ease(Age / Opens);
            float closing = Total - Closes;
            return Age > closing ? 1f - Ease((Age - closing) / Closes) : 1f;
        }
    }

    /// <summary>
    /// Where the Pokémon is across the band: 1 off its right edge, 0 where it holds, -1 off its left edge.
    /// </summary>
    public float Slide
    {
        get
        {
            float t = Age - Opens;
            if (t < Enters) return 1f - Ease(Math.Max(0f, t) / Enters);
            t -= Enters;
            if (t < Holds) return 0f;
            t -= Holds;
            return -Ease(Math.Min(1f, t / Leaves));
        }
    }

    /// <summary>How much of the move's name shows: it comes with the Pokémon and goes when it runs on.</summary>
    public float Label
    {
        get
        {
            float t = Age - Opens - Enters * 0.6f;
            if (t <= 0f) return 0f;
            float gone = Opens + Enters + Holds - Age;
            return Math.Clamp(Math.Min(t / 0.12f, gone / 0.1f + 1f), 0f, 1f);
        }
    }

    private static float Ease(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return 1f - (1f - t) * (1f - t);
    }
}
