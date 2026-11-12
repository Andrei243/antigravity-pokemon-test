using System.Linq;
using System.Text;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests.Golden;

/// <summary>
/// A hundred floors of the Battle Pyramid (plan 38 · EX6, plan 44 · SD7), held to <c>pyramid.txt</c> before the laying
/// out of a floor from pieces moved out of <see cref="PyramidFloor"/> into <c>FloorLayout</c>: every seed must go on
/// giving the floor it gave. Each block is the seed and floor, the template, the squares' pieces, the way in and the stairs,
/// the trainers and the finds, and the map file the floor is written as (its ground and solid rows).
/// </summary>
public class GoldenPyramidTests
{
    [Fact]
    public void AHundredPyramidFloorsAreLaidAsTheyWere()
    {
        var text = new StringBuilder($"# A hundred Battle Pyramid floors from their seeds (plan 38 · EX6), made by GoldenPyramidTests. Rewritten by: {GoldenFile.AcceptCommand}\n");
        var finds = new[] { "Potion", "Ether", "Full Heal" };
        var wild = new[] { new WildEncounterEntry { SpeciesName = "Zubat", MinLevel = 50, MaxLevel = 50, Weight = 100 } };
        for (int seed = 1; seed <= 100; seed++)
        {
            int floor = seed % 7;
            var f = PyramidFloor.Generate(seed * 7919, floor);
            text.Append($"{GoldenFile.BlockMark}seed {seed * 7919} floor {floor}\n");
            text.Append($"template {f.Template}, pieces {string.Join(' ', f.Layout)}, in square {f.EntranceSquare} at {f.Entrance.X},{f.Entrance.Y}, stairs square {f.ExitSquare} at {f.Exit.X},{f.Exit.Y}\n");
            text.Append($"trainers {string.Join(' ', f.Trainers.Select(t => $"{t.X},{t.Y},{t.Facing}"))}\n");
            text.Append($"finds {string.Join(' ', f.Finds.Select(t => $"{t.X},{t.Y}"))}\n");
            var file = f.ToFile(finds, wild);
            for (int y = 0; y < file.Height; y++) text.Append($"  {file.Ground[y]} {file.Solid[y]}\n");
            text.Append($"hidden {string.Join(' ', file.HiddenItems!.Select(h => $"{h.X},{h.Y},{h.Item},{h.Flag}"))}\n");
        }
        GoldenFile.Check("pyramid.txt", text.ToString());
    }
}
