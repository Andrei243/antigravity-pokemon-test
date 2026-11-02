using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The Seaside Cycling Road's timed run (plan 21 · H12, part k; Emerald's <c>Special_BeginCyclingRoadChallenge</c> and
/// <c>Bike_TryAdvanceCyclingRoadCollisions</c>; <see cref="CyclingRoad"/> has the rules). The north gate's step sets
/// <c>VAR_CYCLING_CHALLENGE_STATE</c> to 1 for a rider on the Mach Bike; the first frame out on the overworld begins the
/// run (2), from which the frames are counted (sixty a second, on the frame's own time) and every collision on the bicycle
/// (the player's bump) to 100; the south end's trigger judges it through the host's <see cref="CyclingRun"/>.
/// </summary>
public partial class GameEngine
{
    private float cyclingSeconds;
    private int cyclingBumpsAtStart;

    /// <summary>The run so far: its frames and its collisions, as Emerald counts them.</summary>
    internal (int Frames, int Collisions) CyclingRun =>
        ((int)(cyclingSeconds * 60f), Math.Min(CyclingRoad.MostCollisions, Math.Max(0, flow.Player.Bumps - cyclingBumpsAtStart)));

    private void KeepTheCyclingRun(float dt)
    {
        switch (flow.Story.Var(CyclingRoad.StateVariable))
        {
            case 1 when flow.Map.IsStreamed:
                cyclingSeconds = 0f;
                cyclingBumpsAtStart = flow.Player.Bumps;
                flow.Story.SetVar(CyclingRoad.StateVariable, 2);
                break;
            case 2:
                cyclingSeconds += dt;
                break;
        }
    }
}
