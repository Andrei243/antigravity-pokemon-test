using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// Notices the player spinning round: four quarter turns one after the other, all the same way, each within
/// <see cref="MaxGap"/> seconds of the last. Walking a tight circle counts, since turning takes a step here.
/// One Pokémon evolves this way (Milcery, in Sword and Shield).
/// </summary>
public sealed class SpinTracker
{
    /// <summary>The longest pause between two quarter turns of one spin.</summary>
    public const float MaxGap = 1.2f;

    private Direction? last;
    private int turns, sense;
    private float sinceTurn;

    /// <summary>Call every frame with the way the player faces; true on the frame a full circle is completed.</summary>
    public bool Update(Direction facing, float dt)
    {
        sinceTurn += dt;
        if (last == null || facing == last)
        {
            last = facing;
            return false;
        }

        int step = QuarterTurn(last.Value, facing);
        last = facing;
        bool carriesOn = step != 0 && step == sense && turns > 0 && sinceTurn <= MaxGap;
        turns = step == 0 ? 0 : carriesOn ? turns + 1 : 1;
        sense = step;
        sinceTurn = 0f;

        if (turns < 4) return false;
        turns = 0;
        return true;
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
