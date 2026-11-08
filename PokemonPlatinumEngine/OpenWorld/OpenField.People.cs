using System.Collections.Generic;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// The field's people and its follower at work (plan 32 · OW6): after the player's every tick, everyone of the map
/// who is there and either walked by something or within <see cref="OpenWandering.Reach"/> of the player is stepped
/// in the map's order (a walk steering them, or their own wandering, or their capsule at rest), each told to the
/// map's index where they now stand; then whoever follows the player takes their crumb. GPU-free; ticks only.
/// </summary>
public sealed partial class OpenField
{
    /// <summary>Whoever walks behind the player (the grid's follower, a Pokémon or a partner), or null.</summary>
    public OpenFollower? Follower { get; set; }

    /// <summary>
    /// Whether the people may start a move of their own (the engine's: not while a script or a trainer's walk up has
    /// the field, as the grid's people start none). A move under way goes on either way.
    /// </summary>
    public bool PeopleMayWander { get; set; } = true;

    private readonly List<OpenPerson> near = new();

    /// <summary>The people and the follower after a tick of the player's (every frame's tick runs it: <see cref="OpenFieldTicks"/>).</summary>
    public void StepTheRest()
    {
        var at = Player.Pos;
        foreach (var p in Map.People)
        {
            if (!p.Present) continue;
            if (p.Walk is { } walk)
            {
                p.Body.Step(Map.Ground, walk.InputFor(p.Body));
                if (walk.After(p.Body))
                {
                    // A walk to a marker ends on it; a leg of wandering is followed by a wait
                    if (!walk.GaveUp) walk.Finish(p);
                    if (ReferenceEquals(p.Walk, walk)) p.Walk = null;
                }
            }
            else if (p.Pos.GroundDistanceSquared(at) <= (long)OpenWandering.Reach * OpenWandering.Reach)
                OpenWandering.Step(Map, p, PeopleMayWander);
            else continue;
            Map.Moved(p);
        }
        Follower?.Step(Player.Pos, Player.Yaw);
    }

    /// <summary>Everyone of the map who is there within <paramref name="reach"/> millimetres of a place, in the map's order.</summary>
    public List<OpenPerson> PresentNear(OpenPos pos, int reach)
    {
        Map.PeopleNear(pos, reach, near);
        near.RemoveAll(p => !p.Present);
        return near;
    }
}
