using System;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// What makes a Gym more than a room (plan 01 · M9): the original's "dynamic map features" (<c>src/overlay008/
/// gym_features.c</c>), one for each Gym that has a puzzle. A puzzle is a rule of the field with no drawing or input:
/// it says which of its tiles are closed now (<see cref="Apply"/>, written into the map's own solid grid, so every
/// other rule of walking sees it), what a step onto one of its tiles does where that is more than a walk
/// (<see cref="HopsOver"/>), and how it is laid out again when the player comes in by a door (<see cref="Arrive"/>).
/// What the story must remember of it is kept in the story's variables, under the original's names; the rest lasts
/// only while the player stays, as the original's persisted features do.
/// </summary>
public abstract class GymPuzzle
{
    /// <summary>The name a map file gives the puzzle (<c>"puzzle"</c>).</summary>
    public abstract string Name { get; }

    /// <summary>
    /// Brings the map into line with the story: the tiles the puzzle closes, as it stands now. Cheap when nothing
    /// has changed, so the game calls it every frame (as it keeps the field moves in force).
    /// </summary>
    public abstract void Apply(Map map, StoryState story);

    /// <summary>
    /// The player has come into the room through a door or a warp (not back from a battle): the original's
    /// <c>InitPersistedMapFeaturesFor...</c>, which the room's own script runs as the player arrives.
    /// </summary>
    public virtual void Arrive(Map map, StoryState story, Random rng) { }

    /// <summary>Whether a step onto a tile, going a way, is a hop over it to the tile beyond (Eterna's hour hand).</summary>
    public virtual bool HopsOver(int x, int y, Direction dir) => false;

    /// <summary>The puzzle a map file names; null for none.</summary>
    public static GymPuzzle? Create(string? name) => name switch
    {
        null or "" => null,
        EternaClock.PuzzleName => new EternaClock(),
        VeilstoneBags.PuzzleName => new VeilstoneBags(),
        HearthomeDoors.Room1Name => new HearthomeDoors(1),
        HearthomeDoors.Room2Name => new HearthomeDoors(2),
        _ => throw new ArgumentException($"There is no Gym puzzle called '{name}'.")
    };
}
