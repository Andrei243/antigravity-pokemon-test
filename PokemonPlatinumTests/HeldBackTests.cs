using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// What the overlays hold back for want of a way to it (plan 01 · M12). An object of an area file is left off the map
/// (<c>heldBack</c>) when nothing leads to it yet; each of those is put back here on a world of the test's own, the
/// whole game is walked, and none of them is come to. The day one is, its reason is true no longer and it goes back on
/// the map: take it out of its overlay's list and out of this one.
///
/// What is held back for another reason is not in this list: the things of a scene that the story's lane lays out
/// itself (Stark Mountain's stone and rock, the Lunar Wing) and Amity Square's three, which wait for the square's huts
/// (plan 06 · R16c).
/// </summary>
public class HeldBackTests
{
    private static readonly (string Area, string Id, string Why)[] NoWayTo =
    {
        // On the lake as the game begins, the lake under Team Galactic's guard and the lake drained, the banks shut the
        // ball's shelf in: no shore of that version leads to it by land or by water
        ("lake_verity_low_water", "item_tm38", "the banks shut its shelf in on this version of the lake"),
        ("lake_acuity_low_water", "item_tm14", "cliffs close its grass off on this version of the lake"),
        ("lake_valor_drained", "item_tm25", "its shelf is reached by water, and the lake is drained"),
        // The ruins' room Maniac Tunnel leads to is shut until the tunnel is dug (Solaceon Town's door 7)
        ("solaceon_town", "item_rare_candy", "the ledge over the ruins is come to through Maniac Tunnel"),
        // Two passages run to it under arches of rock, which the builder takes for raised ground
        ("mt_coronet_2f", "item_max_revive", "both passages to it run under arches the builder makes cliffs of"),
        // The north face's ledge seen from the south: this map's copy of the rocks the north face's own map has
        ("mt_coronet_outside_south", "rock_smash_2", "the north face's ledge, which nothing on this map leads up to"),
        ("mt_coronet_outside_south", "rock_smash_3", "the north face's ledge, which nothing on this map leads up to"),
        ("mt_coronet_outside_south", "rock_smash_4", "the north face's ledge, which nothing on this map leads up to"),
        ("mt_coronet_outside_south", "rock_smash_5", "the north face's ledge, which nothing on this map leads up to"),
        ("mt_coronet_outside_south", "rock_smash_6", "the north face's ledge, which nothing on this map leads up to"),
        ("mt_coronet_outside_south", "rock_smash_7", "the north face's ledge, which nothing on this map leads up to"),
    };

    // Each lake's TM lies on both versions of its lake behind one flag: where the story comes to later, it is picked up
    private static readonly (string Held, string Map, string Item)[] OnTheOtherLake =
    {
        ("lake_verity_low_water", "LakeVerityGalactic", "TM38"),
        ("lake_acuity_low_water", "LakeAcuityGalactic", "TM14"),
        ("lake_valor_drained", "LakeValor", "TM25"),
    };

    [Fact]
    public void WhatIsHeldBackForWantOfAWayStillHasNone()
    {
        // A world of this test's own, with everything on the list put back on its map
        var world = World.LoadAll().Single(w => w.Index.Region == "Sinnoh");
        var back = NoWayTo.Where(e => world.Overlay(e.Area)?.HeldBack?.Remove(e.Id) == true).ToList();
        Assert.NotEmpty(back);

        var built = world.BuildMaps().ToDictionary(m => m.Name);
        var rooms = MapDatabase.Fresh();
        Map MapOf(string name) => built.TryGetValue(name, out var map) ? map : rooms(name);
        var reached = WorldWalk.FromEveryStart(MapOf);

        bool ComeTo(Map map, int x, int y) => reached.TryGetValue(map, out var tiles)
            && (tiles.Contains((x, y)) || tiles.Contains((x + 1, y)) || tiles.Contains((x - 1, y)) || tiles.Contains((x, y + 1)) || tiles.Contains((x, y - 1)));

        foreach (var (area, id, why) in back)
        {
            var placed = world.Area(area)!.Objects.First(o => o.Id == id);
            var map = built.Values.First(m => m.FindArea(area) != null);
            // It is on the map again, where the area file stands it
            Assert.True(map.Everyone.Any(n => (n.GridX, n.GridY) == (placed.X, placed.Z)), $"{area}: {id} was not laid out at ({placed.X},{placed.Z})");
            Assert.False(ComeTo(map, placed.X, placed.Z),
                $"{area}: {id} at ({placed.X},{placed.Z}) can be come to now, so it is held back for nothing ({why}): take it out of the overlay's heldBack and out of this list");
        }

        // The lakes' TMs are not lost: the same ball, behind the same flag, is walked up to on the lake the story comes to
        foreach (var (held, name, item) in OnTheOtherLake)
        {
            var map = built[name];
            var ball = map.Everyone.Single(n => n.IsItemBall && n.Item == item);
            Assert.True(ComeTo(map, ball.GridX, ball.GridY), $"{name}: the {item} can't be come to");
            var twin = built.Values.First(m => m.FindArea(held) != null).Everyone.Single(n => n.IsItemBall && n.Item == item);
            Assert.Equal(ball.HiddenBy, twin.HiddenBy);
        }
    }

    [Fact]
    public void WhatTheListNamesIsInTheAreasFiles()
    {
        var world = World.LoadAll().Single(w => w.Index.Region == "Sinnoh");
        foreach (var (area, id, _) in NoWayTo)
            Assert.True(world.Area(area)?.Objects.Any(o => o.Id == id) == true, $"{area} has no object called {id}");
    }
}
