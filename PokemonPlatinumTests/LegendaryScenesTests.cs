using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 29 (style guide, "Legendary scenes"): every battle with a legendary or mythical Pokémon met in a place of its
/// own comes after a staged beat, a <c>scene</c> of its script that moves the camera and lights a sign.
/// </summary>
public class LegendaryScenesTests
{
    /// <summary>
    /// The meetings still waiting for their beat (<c>file.Script Species</c>), each with the session of plan 29 that
    /// owes it. A session takes its rows off; a row whose meeting has its beat fails as stale.
    /// </summary>
    private static readonly Dictionary<string, string> StillBare = new()
    {
        // Sinnoh's lakes, Heatran and Regigigas
        ["acuity_cavern.Uxie Uxie"] = "LS2",
        ["valor_cavern.Azelf Azelf"] = "LS2",
        ["stark_mountain_room_3.Heatran Heatran"] = "LS2",
        ["snowpoint_temple_b5f.Regigigas Regigigas"] = "LS2",
        // The room's Arrival has its cry, shake and ripple, but its camera never leaves the player before the battle
        // (its pans are Cynthia's, after it): LS1 found it, LS2 owes its look
        ["distortion_world_giratina_room.Giratina Giratina-Origin"] = "LS2",
        // Kanto's birds and Mewtwo
        ["seafoam_islands_b4f.Articuno Articuno"] = "LS3",
        ["power_plant.Zapdos Zapdos"] = "LS3",
        ["mt_ember_summit.Moltres Moltres"] = "LS3",
        ["cerulean_cave_b1f.Mewtwo Mewtwo"] = "LS3",
        // Johto's birds of the towers
        ["bell_tower_roof.HoOh Ho-Oh"] = "LS4",
        ["whirl_islands_b3f_lugia_cave.Lugia Lugia"] = "LS4",
        ["mount_silver_cave_moltres_chamber.Moltres Moltres"] = "LS4",
        // Hoenn's titans after the story
        ["sky_pillar_top.Rayquaza Rayquaza"] = "LS6",
        ["marine_cave_end.Kyogre Kyogre"] = "LS6",
        ["terra_cave_end.Groudon Groudon"] = "LS6",
        // Hoenn's Regis and islands
        ["desert_ruins.Regirock Regirock"] = "LS7",
        ["island_cave.Regice Regice"] = "LS7",
        ["ancient_tomb.Registeel Registeel"] = "LS7",
        ["southern_island_interior.Stone Latias"] = "LS7",
        ["southern_island_interior.Stone Latios"] = "LS7",
        ["birth_island_exterior.Triangle Deoxys-Speed"] = "LS7",
        ["faraway_island_interior.Mew Mew"] = "LS7",
        ["navel_rock_bottom.Lugia Lugia"] = "LS7",
        ["navel_rock_top.HoOh Ho-Oh"] = "LS7",
    };

    /// <summary>Whether a wild battle's species (or form) is a legendary or a mythical one.</summary>
    private static bool IsLegend(string name)
    {
        var species = PokemonDatabase.Get(name) ?? PokemonDatabase.SpeciesOfForm(name);
        return species != null && (species.Legendary || species.Mythical);
    }

    /// <summary>A camera line that takes the picture somewhere: a pan, a zoom, a path or a follow (a shake is no look).</summary>
    private static bool Looks(Instruction i) => i.Op == Op.Camera
        && i.Camera is CameraMove.Pan or CameraMove.Zoom or CameraMove.Path or CameraMove.Follow;

    /// <summary>A sign: a scene effect or a set piece.</summary>
    private static bool Signs(Instruction i) => i.Op is Op.Effect or Op.Shake or Op.Flicker or Op.Anim;

    /// <summary>Every legendary wild battle of every script, with whether a staged scene stands before it.</summary>
    private static IEnumerable<(string Key, bool Staged)> Meetings()
    {
        foreach (var script in ScriptLibrary.Default.All)
        {
            var code = script.Code;
            for (int n = 0; n < code.Count; n++)
            {
                var lines = code[n].Then == null ? new[] { code[n] } : new[] { code[n], code[n].Then! };
                foreach (var battle in lines.Where(i => i.Op == Op.WildBattle && IsLegend(i.Name)))
                {
                    int start = -1;
                    for (int k = n - 1; k >= 0; k--)
                        if (code[k].Op == Op.Scene) { start = k; break; }
                    bool staged = false;
                    if (start >= 0)
                    {
                        var inScene = new List<Instruction>();
                        for (int k = start + 1; k < n && code[k].Op != Op.EndScene; k++)
                        {
                            inScene.Add(code[k]);
                            if (code[k].Then != null) inScene.Add(code[k].Then!);
                        }
                        staged = inScene.Any(Looks) && inScene.Any(Signs);
                    }
                    yield return ($"{script.FullName} {battle.Name}", staged);
                }
            }
        }
    }

    /// <summary>
    /// FireRed's roaming beast (plan 29 · LS9): a Champion of Kanto with the National Pokédex sees it race over Pallet
    /// Town, and it roams Kanto's routes from a slot of its own, by FireRed's rule of the starter; once only.
    /// </summary>
    [Theory]
    [InlineData("Bulbasaur", "Entei")]
    [InlineData("Charmander", "Suicune")]
    [InlineData("Squirtle", "Raikou")]
    [InlineData("Turtwig", "Raikou")]
    public void KantosRoamingBeastIsTheStartersAndRoamsKanto(string starter, string beast)
    {
        var host = new HeadlessScriptHost { NationalDex = true };
        host.Story.ChooseStarter(starter);
        host.Story.Set("FLAG_SYS_GAME_CLEAR");
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("OnEnter", "pallet_town")!, null);
        runner.RunToEnd();
        Assert.Empty(host.Problems);

        int slot = Roamers.SlotOf(beast, "kanto")!.Value;
        var roamer = host.Encounters.Roamers[slot];
        Assert.True(roamer.Active);
        Assert.Equal(50, roamer.Level);
        Assert.Contains(Roamers.PlaceOf(roamer), Roamers.KantoRoutes);
        Assert.Equal("VAR_ROAMING_KANTO_" + beast.ToUpperInvariant() + "_STATE", Roamers.StateVariable(slot));
        // Johto's Raikou and Entei stay where they are
        Assert.All(host.Encounters.Roamers.Where((r, s) => s != slot), r => Assert.False(r.Active));
        Assert.True(host.Story.Has("FLAG_KANTO_ROAMER_LOOSE"));

        var rng = new System.Random(3);
        for (int i = 0; i < 300; i++)
        {
            Roamers.PlayerWalkedInto(host.Encounters, i % 2 == 0 ? "route_1" : "route_2", rng);
            Assert.Contains(Roamers.PlaceOf(roamer), Roamers.KantoRoutes);
        }
    }

    [Fact]
    public void EveryLegendaryBattleOfAPlaceHasItsBeat()
    {
        var meetings = Meetings().ToList();
        Assert.NotEmpty(meetings);
        var bare = meetings.Where(m => !m.Staged && !StillBare.ContainsKey(m.Key)).Select(m => m.Key).Distinct().ToList();
        var stale = meetings.Where(m => m.Staged && StillBare.ContainsKey(m.Key)).Select(m => m.Key).Distinct().ToList();
        var gone = StillBare.Keys.Where(k => meetings.All(m => m.Key != k)).ToList();
        Assert.True(bare.Count == 0, "No staged beat before:\n" + string.Join("\n", bare));
        Assert.True(stale.Count == 0, "Staged now, take off StillBare:\n" + string.Join("\n", stale));
        Assert.True(gone.Count == 0, "In StillBare but no such meeting:\n" + string.Join("\n", gone));
    }
}
