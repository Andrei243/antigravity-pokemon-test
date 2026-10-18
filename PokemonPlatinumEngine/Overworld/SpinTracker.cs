using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// Notices the player spinning round: four quarter turns or more one after the other, all the same way, each within
/// <see cref="MaxGap"/> seconds of the last. Walking a tight circle counts, since turning takes a step here. One
/// Pokémon evolves this way (Milcery, in Sword and Shield), and since plan 06 · R29 the way and the length of the spin
/// choose its cream (<see cref="Evolution.AlcremieCream"/>), so a spin is over, and told, once the player stops turning
/// for <see cref="MaxGap"/> or turns the other way: <see cref="Last"/> is the spin just ended.
/// </summary>
public sealed class SpinTracker
{
    /// <summary>The longest pause between two quarter turns of one spin.</summary>
    public const float MaxGap = 1.2f;

    private Direction? last;
    private int turns, sense;
    private float sinceTurn, length;

    /// <summary>The spin that ended last, which way and how long from its first quarter turn to its last.</summary>
    public SpinMade? Last { get; private set; }

    /// <summary>Call every frame with the way the player faces; true on the frame a spin of a full circle or more is over.</summary>
    public bool Update(Direction facing, float dt)
    {
        sinceTurn += dt;
        if (last == null || facing == last)
        {
            last ??= facing;
            // Standing still too long ends the spin
            return sinceTurn > MaxGap && End();
        }

        int step = QuarterTurn(last.Value, facing);
        last = facing;
        bool carriesOn = step != 0 && step == sense && turns > 0 && sinceTurn <= MaxGap;
        // A turn that doesn't carry the spin on ends the one made before it
        bool ended = !carriesOn && End();
        if (carriesOn) length += sinceTurn;
        else length = 0f;
        turns = step == 0 ? 0 : carriesOn ? turns + 1 : 1;
        sense = step;
        sinceTurn = 0f;
        return ended;
    }

    /// <summary>Ends the spin under way: true when it was a full circle or more, which <see cref="Last"/> then tells.</summary>
    private bool End()
    {
        bool whole = turns >= 4;
        if (whole) Last = new SpinMade(sense > 0, length);
        turns = 0;
        length = 0f;
        return whole;
    }

    /// <summary>+1 for a quarter turn clockwise (seen from above), -1 for one the other way, 0 for an about-turn.</summary>
    private static int QuarterTurn(Direction from, Direction to)
    {
        int steps = (Clockwise(to) - Clockwise(from) + 4) % 4;
        return steps == 1 ? 1 : steps == 3 ? -1 : 0;
    }

    private static int Clockwise(Direction d) => d switch
    {
        Direction.Up => 0,
        Direction.Right => 1,
        Direction.Down => 2,
        _ => 3
    };
}
