using MapImporter.Hg;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>Plan 20 · J15, part o: the last rooms of HeartGold's Kanto, walked to.</summary>
public partial class JohtoTests
{
    [Fact]
    public void CeruleanCaveTheTrainerHousesHallPalParksGateAndTheShipsRoomsAreWalkedTo()
    {
        var walked = Walked.Value;
        var opened = new[]
        {
            "Cerulean_Cave_1F", "Cerulean_Cave_2F", "Cerulean_Cave_B1F", "Viridian_TrainerHouse_B1F", "Fuchsia_PalParkGate",
            "SSAqua_CaptainRoom", "SSAqua_1F_SoutheastRooms", "SSAqua_1F_SouthwestRooms", "SSAqua_1F_NortheastRooms",
            "SSAqua_1F_NorthwestRooms", "SSAqua_B1F"
        };
        Assert.Empty(opened.Where(name => !walked.Keys.Any(m => m.Name == name)));
        Assert.All(opened, name => Assert.Equal(RegionDatabase.Johto, RegionDatabase.RegionOfMap(name)!.Id));
        // Cerulean Cave's keys meet FireRed's and take Johto's prefix; FireRed's own floors and Mewtwo stay its
        foreach (string key in new[] { "cerulean_cave_1f", "cerulean_cave_2f", "cerulean_cave_b1f" })
            Assert.Equal("johto_" + key, HgWorld.KeyOf("MAP_" + key.ToUpperInvariant()));
        Assert.Equal(RegionDatabase.Kanto, RegionDatabase.RegionOfMap("CeruleanCave_B1F")!.Id);
        Assert.NotNull(ScriptLibrary.Default.Find("Mewtwo", "cerulean_cave_b1f"));
        // Mewtwo stands at the back of the lowest floor with its script, a tile beside it walked to
        var b1f = walked.Single(kv => kv.Key.Name == "Cerulean_Cave_B1F");
        var mewtwo = b1f.Key.Everyone.Single(n => n.Key == "d03r0103_tsure_poke_static_mewtwo");
        Assert.Equal("Mewtwo", mewtwo.Species);
        Assert.Equal("Mewtwo", mewtwo.Script);
        Assert.Contains(b1f.Value, t => Math.Abs(t.X - mewtwo.GridX) + Math.Abs(t.Y - mewtwo.GridY) == 1);
        // The man before the cave's mouth stands aside by HeartGold's own flag, which Cerulean's arrival sets after Red
        Assert.Contains(Overworld.Everyone, n => n.Key == "t04_gsman1_2" && n.HiddenBy == "FLAG_HIDE_CERULEAN_CITY_MAN_OUTSIDE_CAVE");
        // The Trainer House's hall: the referee and Cal, never the link records' ten stand-ins
        var hall = MapNamed("Viridian_TrainerHouse_B1F");
        Assert.Contains(hall.NPCs, n => n.Key == "t02r0302_gsman3" && n.Script == "Cal");
        Assert.Contains(hall.NPCs, n => n.Key == "t02r0302_gswoman6");
        Assert.DoesNotContain(hall.Everyone, n => n.Key.StartsWith("t02r0302_var_", StringComparison.Ordinal));
        Assert.NotNull(TrainerDatabase.Get("johto_trainer_house_cal"));
        // The ship's passengers are Johto's own trainers, in three day's groups by HeartGold's flags
        var hold = MapNamed("SSAqua_B1F");
        Assert.Equal("FLAG_UNK_22A", hold.Everyone.Single(n => n.Key == "p01r0307_seaman_2_2").HiddenBy);
        Assert.NotNull(TrainerDatabase.Get("johto_sailor_jeff", RegionDatabase.Johto));
        // Pal Park's gate leads to Sinnoh's Pal Park by its attendant, and the lobby's Kanto attendant back
        Assert.Contains(MapNamed("Fuchsia_PalParkGate").NPCs, n => n.Script == "Attendant");
        Assert.Contains(MapDatabase.Get("PalParkLobby").Everyone, n => n.Key == "kanto_attendant" && n.ShownBy == "JohtoHallOfFame");
    }

    [Fact]
    public void HeartGoldsFlashCavesAreDarkUntilFlashLightsThem()
    {
        // Plan 20 · J15, part o: HeartGold's weather 11 is its Flash weather, as Platinum's DarkFlash
        foreach (string name in new[] { "Rock_Tunnel_1F", "Rock_Tunnel_B1F", "Cerulean_Cave_1F", "Cerulean_Cave_2F", "Cerulean_Cave_B1F" })
            Assert.True(MapNamed(name).IsDark, $"{name} should be dark");
        Assert.False(MapNamed("MountMoon").IsDark);
        Assert.Equal("Zephyr", FieldMoveRules.BadgeFor(FieldMove.Flash, RegionDatabase.Johto));
    }
}
