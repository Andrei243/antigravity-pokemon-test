using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
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
        // Unova's legends after the League
        ["unova_route_13.Cobalion Cobalion"] = "LS8",
        ["unova_route_11.Virizion Virizion"] = "LS8",
        ["unova_route_22.Terrakion Terrakion"] = "LS8",
        ["unova_route_23.Azelf Azelf"] = "LS8",
        ["celestial_tower.Mesprit Mesprit"] = "LS8",
        ["giant_chasm_6.KyuremRests Kyurem"] = "LS8",
        ["reversal_mountain_5.Heatran Heatran"] = "LS8",
        ["rock_peak_chamber.Regirock Regirock"] = "LS8",
        ["iceberg_chamber.Regice Regice"] = "LS8",
        ["iron_chamber.Registeel Registeel"] = "LS8",
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
