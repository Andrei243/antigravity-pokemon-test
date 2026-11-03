using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Johto's last leftovers that need no pictures (plan 20, part t; the rulings' "Johto's last leftovers, part t"):
/// Steven's Enigma Stone in Pewter's museum and the Latias or Latios that waits with him in the city, the museum's second
/// Steven, the night aboard the S.S. Aqua, the scenes Cameron's photographs are printed on and Cameron at every place
/// HeartGold stands him.
/// </summary>
public partial class JohtoStoryTests
{
    private static Game StevenGame(BattleOutcome fight = BattleOutcome.Won)
    {
        var game = new Game(fight: fight);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 60));
        game.Story.Set("JohtoHallOfFame");
        game.Story.Set("FLAG_JOHTO_GOT_HOENN_STARTER");
        return game;
    }

    [Theory]
    [InlineData(BattleOutcome.Won)]
    [InlineData(BattleOutcome.Caught)]
    [InlineData(BattleOutcome.Fled)]
    public void StevenShowsTheEnigmaStoneAndHisLatiasWaitsInPewterToBeFacedOnce(BattleOutcome fight)
    {
        var game = StevenGame(fight);
        // Before the stone, nobody waits in the city
        game.Arrive("Johto", 1044, 82);
        Assert.Null(game.Present("t03_daigo"));
        Assert.Null(game.Present("t03_tsure_poke_static_latias"));

        // The museum: the first Steven shows the stone and goes out to fetch his friend
        game.Arrive("Pewter_Museum", 22, 9);
        Assert.NotNull(game.Present("t03r0101_daigo"));
        Assert.Null(game.Present("t03r0101_daigo_2"));
        var shown = game.Talk("t03r0101_daigo");
        Assert.Contains(shown.Transcript, l => l.Text.Contains("Enigma Stone"));
        Assert.True(game.Story.Has("FLAG_JOHTO_STEVEN_SHOWED_STONE"));
        game.Arrive("Pewter_Museum", 22, 9);
        Assert.Null(game.Present("t03r0101_daigo"));

        // The city: Steven and Latias (no Hoenn yet: HeartGold's own), a no keeps them waiting
        game.Arrive("Johto", 1044, 82);
        Assert.NotNull(game.Present("t03_daigo"));
        Assert.NotNull(game.Present("t03_tsure_poke_static_latias"));
        Assert.Null(game.Present("t03_tsure_poke_static_latios"));
        StandsInTheOpen(game, "t03_daigo");
        game.Answers.Enqueue(1);
        game.Talk("t03_daigo");
        Assert.False(game.Story.Has("FLAG_JOHTO_MET_STEVENS_EON"));
        game.Answers.Enqueue(0);
        var met = game.Talk("t03_daigo");
        Assert.Contains(met.Log, l => l.Contains("Latias") && l.Contains("40"));
        Assert.True(game.Story.Has("FLAG_JOHTO_MET_STEVENS_EON"));
        game.Arrive("Johto", 1044, 82);
        Assert.Null(game.Present("t03_daigo"));
        Assert.Null(game.Present("t03_tsure_poke_static_latias"));

        // The museum's second Steven, by the fossils, for good
        game.Arrive("Pewter_Museum", 22, 9);
        Assert.Null(game.Present("t03r0101_daigo"));
        Assert.NotNull(game.Present("t03r0101_daigo_2"));
        StandsInTheOpen(game, "t03r0101_daigo_2");
        Assert.NotEmpty(game.Talk("t03r0101_daigo_2").Transcript);
    }

    [Fact]
    public void ALostBattleLeavesStevenAndHisFriendWaiting()
    {
        var game = StevenGame(BattleOutcome.Lost);
        game.Story.Set("FLAG_JOHTO_STEVEN_SHOWED_STONE");
        game.Arrive("Johto", 1044, 82);
        game.Answers.Enqueue(0);
        game.Talk("t03_daigo");
        Assert.False(game.Story.Has("FLAG_JOHTO_MET_STEVENS_EON"));
        game.Arrive("Johto", 1044, 82);
        Assert.NotNull(game.Present("t03_daigo"));
    }

    [Theory]
    [InlineData(0, "t03_tsure_poke_static_latios", "Latios")]
    [InlineData(1, "t03_tsure_poke_static_latias", "Latias")]
    public void AfterHoennStevensFriendIsTheOneNotFlyingOverHoenn(int roamer, string key, string species)
    {
        var game = StevenGame();
        game.Story.Set("HoennHallOfFame");
        game.Story.SetVar("VAR_ROAMER_POKEMON", roamer);
        game.Story.Set("FLAG_JOHTO_STEVEN_SHOWED_STONE");
        game.Arrive("Johto", 1044, 82);
        Assert.NotNull(game.Present(key));
        Assert.Single(game.Map.NPCs, n => n.Key.StartsWith("t03_tsure_poke_static_"));
        game.Answers.Enqueue(0);
        Assert.Contains(game.Talk("t03_daigo").Log, l => l.Contains(species));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheSSAquaCrossingIsANightInACabinThatRestsTheTeam(bool homeward)
    {
        var game = new Game();
        var lead = new Pokemon(PokemonDatabase.Get("Typhlosion")!, 40);
        game.Party.Add(lead);
        lead.CurrentHP = 1;
        if (homeward) game.Story.Set("FLAG_JOHTO_SS_AQUA_AT_VERMILION");
        game.Arrive("SSAqua_1F", 24, 18);
        game.Answers.Enqueue(0);
        var night = game.Talk("p01r0301_seaman_2_2");
        Assert.Contains(night.Log, l => l.StartsWith("warp ") && l.Contains("SSAqua_1F_SouthwestRooms"));
        Assert.Contains(night.Transcript, l => l.Text.Contains("cabin"));
        Assert.Contains(night.Transcript, l => l.Text.Contains("horn"));
        Assert.Equal(lead.MaxHP, lead.CurrentHP);
        Assert.Equal(!homeward, game.Story.Has("FLAG_JOHTO_SS_AQUA_AT_VERMILION"));
        Assert.Equal("SSAqua_1F", game.Map.Name);
        Assert.Equal((24, 18), game.Tile);

        // The cabin's tile is open floor of the cabins' map
        var cabins = Johto.Value.BuildMaps().Single(m => m.Name == "SSAqua_1F_SouthwestRooms");
        Assert.True(cabins.IsWalkable(23, 3));
    }

    [Fact]
    public void APhotographsSceneIsItsPlacesKind()
    {
        Assert.Equal(PhotoSetting.Room, PhotoScenes.Of("celadon_department_store_2f", false, false, true, false));
        Assert.Equal(PhotoSetting.Mountain, PhotoScenes.Of("mount_silver", false, false, false, false));
        Assert.Equal(PhotoSetting.Mountain, PhotoScenes.Of("ice_path_1f", false, true, false, true));
        Assert.Equal(PhotoSetting.Cave, PhotoScenes.Of("johto_seafoam_islands_b1f", false, true, false, false));
        Assert.Equal(PhotoSetting.Forest, PhotoScenes.Of("ilex_forest", false, false, false, false));
        Assert.Equal(PhotoSetting.Shore, PhotoScenes.Of("route_21", false, false, false, false));
        Assert.Equal(PhotoSetting.Town, PhotoScenes.Of("pallet", true, false, false, false));
        Assert.Equal(PhotoSetting.Route, PhotoScenes.Of("johto_route_1", false, false, false, false));
        Assert.Equal(PhotoSetting.Route, PhotoScenes.Of("", false, false, false, false));

        // Every scene is one of its own, its shapes on the print
        var skies = new HashSet<(byte, byte, byte)>();
        foreach (var setting in Enum.GetValues<PhotoSetting>())
        {
            var look = PhotoScenes.Look(setting);
            Assert.Equal(setting, look.Setting);
            Assert.True(skies.Add((look.Sky.R, look.Sky.G, look.Sky.B)) || setting is PhotoSetting.Route, $"{setting}'s sky is another's");
            Assert.InRange(look.Horizon, 0.4f, 0.8f);
            Assert.NotEmpty(look.Shapes);
            foreach (var s in look.Shapes)
            {
                Assert.InRange(s.X, 0f, 1f);
                Assert.InRange(s.Y, 0f, 1f);
                if (s.Kind != PhotoShapeKind.Circle)
                {
                    Assert.InRange(s.X + s.W, 0f, 1.0001f);
                    Assert.InRange(s.Kind == PhotoShapeKind.Triangle ? s.Y - s.H : s.Y + s.H, 0f, 1.0001f);
                }
            }
        }
        Assert.NotEqual(PhotoScenes.FigureOf("Boy").Hat, PhotoScenes.FigureOf("Girl").Hat);

        // The album's page asks the engine what each place is
        var album = new PhotoAlbum();
        album.Add(new Photo { Place = "ilex_forest", Pokemon = "Pikachu", Player = "Girl", Day = "2026-11-03" });
        var screen = new PhotoAlbumScreen { SettingOf = key => key.Contains("forest") ? PhotoSetting.Forest : PhotoSetting.Route };
        screen.Open(album);
        Assert.Equal(PhotoSetting.Forest, screen.Scene!.Setting);
        album.RemoveAt(0);
        Assert.Null(screen.Scene);
    }

    /// <summary>Every object HeartGold hides behind FLAG_HIDE_CAMERON in an open area of Johto.</summary>
    private static IEnumerable<(string Area, string Id)> CamerasObjects() =>
        Johto.Value.Index.Areas.Select(k => Johto.Value.Area(k)).OfType<WorldAreaFile>().SelectMany(a => a.Objects.Where(o => o.HiddenBy == "FLAG_HIDE_CAMERON").Select(o => (a.Key, o.Id)));

    [Fact]
    public void CameronStandsAtEveryPlaceHeartGoldStandsHimAndBlocksNoWay()
    {
        var objects = CamerasObjects().ToList();
        Assert.True(objects.Count >= 40, $"only {objects.Count} of Cameron's objects");
        foreach (var (area, id) in objects)
        {
            var overlay = Johto.Value.Overlay(area);
            // The two the Pokéathlon's places gave another part (a man who talks, J13) keep it
            if (overlay?.People?.TryGetValue(id, out var other) == true && other.Name != "Cameron") continue;
            Assert.True(overlay?.People != null && overlay.People.TryGetValue(id, out var person) && person.Script == "Cameron"
                && person.ShownBy == "FLAG_JOHTO_MET_CAMERONS_FATHER", $"{area}'s {id} is no Cameron of ours");
            Assert.DoesNotContain(id, overlay!.HeldBack ?? new());
        }

        // With his father met, every map he stands on keeps every tile it had open on foot reachable from its ways
        var story = new StoryState();
        StoryMigration.BeginNewGame(story, Scripts);
        foreach (var map in Johto.Value.BuildMaps())
        {
            var camerons = map.Everyone.Where(n => n.Script == "Cameron").ToList();
            if (camerons.Count == 0) continue;
            story.Unset("FLAG_JOHTO_MET_CAMERONS_FATHER");
            map.ApplyPresence(story.Has);
            map.ApplyDoors(story.Has);
            var without = OnFootFromTheWays(map);
            story.Set("FLAG_JOHTO_MET_CAMERONS_FATHER");
            map.ApplyPresence(story.Has);
            var with = OnFootFromTheWays(map);
            foreach (var c in camerons) without.Remove((c.GridX, c.GridY));
            var cut = without.Except(with).ToList();
            Assert.True(cut.Count == 0, $"Cameron on {map.Name} cuts off {cut.Count} tiles, the first {string.Join(", ", cut.Take(3))}");
            StandsBesideOpenGround(map, camerons);
        }
    }

    private static void StandsBesideOpenGround(Map map, List<NPC> camerons)
    {
        foreach (var c in camerons)
            Assert.True(new[] { (0, 1), (0, -1), (1, 0), (-1, 0) }.Any(d => map.IsWalkable(c.GridX + d.Item1, c.GridY + d.Item2)),
                $"Cameron at {c.GridX},{c.GridY} of {map.Name} has no open tile beside him");
    }

    /// <summary>The tiles walked on foot from every way onto a map (four ways, open tiles: a plain flood, no ledges).</summary>
    private static HashSet<(int X, int Y)> OnFootFromTheWays(Map map)
    {
        var seen = new HashSet<(int X, int Y)>();
        var queue = new Queue<(int X, int Y)>();
        foreach (var w in map.Warps)
            if (seen.Add((w.SourceX, w.SourceY))) queue.Enqueue((w.SourceX, w.SourceY));
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            foreach (var (dx, dy) in new[] { (0, 1), (0, -1), (1, 0), (-1, 0) })
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= map.Width || ny >= map.Height || seen.Contains((nx, ny))) continue;
                if (!map.IsWalkable(nx, ny)) continue;
                seen.Add((nx, ny));
                queue.Enqueue((nx, ny));
            }
        }
        return seen;
    }
}
