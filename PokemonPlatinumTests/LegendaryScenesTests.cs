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
        ["GeosengeTown_FlareHQ.Xerneas Xerneas"] = "Kalos Z4: Xerneas wakes in a scene of its own (wks85); its LS beat is plan 29's to write",
        ["ns_castle_5.Zekrom Zekrom"] = "Unova U10: Zekrom's meeting before plan 29 LS beats; owed",
        ["abundant_shrine.Tornadus Tornadus"] = "plan 40 GE3 placed it; its beat is owed to plan 29 LS",
        ["abundant_shrine.Thundurus Thundurus"] = "plan 40 GE3 placed it; its beat is owed to plan 29 LS",
        ["dragonspiral_tower_9.Reshiram Reshiram"] = "plan 40 GE3 placed it; its beat is owed to plan 29 LS",
        // Plan 33 · GL6's staged battle in the fog of the open Weald, written with no beat of plan 29's
        ["slumbering_weald.Shadow Zacian"] = "plan 33 GL6's staged shadow in an open map; its beat is owed to plan 29 LS",
        ["slumbering_weald.Shadow Zamazenta"] = "plan 33 GL6's staged shadow in an open map; its beat is owed to plan 29 LS",
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
                // A wild battle, or (plan 29 · LS11a) an Egg of a legendary or mythical handed over: Route 230's Manaphy
                foreach (var battle in lines.Where(i => i.Op is Op.WildBattle or Op.GiveEgg && IsLegend(i.Name)))
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

    /// <summary>Plan 29 · LS11a: Sinnoh's event Pokémon each have a meeting of their own, held to the beat by the test below.</summary>
    [Theory]
    [InlineData("newmoon_island_forest.Darkrai Darkrai")]
    [InlineData("flower_paradise.Shaymin Shaymin")]
    [InlineData("hall_of_origin.Arceus Arceus")]
    [InlineData("route_230.Ranger Manaphy")]
    public void SinnohsEventPokemonAreMetAfterTheirBeat(string key) =>
        Assert.Contains(Meetings(), m => m.Key == key && m.Staged);

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
