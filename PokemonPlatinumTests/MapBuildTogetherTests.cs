using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The regions' maps built several at once (<see cref="MapDatabase.BuildWorlds"/>): from the same seeds they are the
/// maps a build of one region after another makes, to every tile, person and rolled Pokémon, however the threads
/// fall; and the dice a thread is handed (<see cref="Dice.Scope"/>) are its own.
/// </summary>
public class MapBuildTogetherTests
{
    // Everything of a map the build decides: its ground, its ways, and everyone on it with what was rolled for them
    private static string Signature(Map map)
    {
        var text = new StringBuilder();
        ulong ground = 14695981039346656037UL;
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                ground = (ground ^ (map.IsSolid(x, y) ? 1UL : 0UL)) * 1099511628211UL;
                ground = (ground ^ (ulong)(int)map.BehaviourAt(x, y)) * 1099511628211UL;
                ground = (ground ^ (ulong)BitConverter.SingleToInt32Bits(map.HeightAt(x, y))) * 1099511628211UL;
            }
        text.Append(map.Name).Append(' ').Append(map.Width).Append('x').Append(map.Height).Append(' ').Append(ground.ToString("x16"))
            .Append(" areas ").Append(map.Areas.Count)
            // The map keeps a generator of its own, seeded from the dice as it is made: its next number tells the seed
            .Append(" chance ").Append(((Random)MapsChance.GetValue(map)!).Next()).AppendLine();
        foreach (var warp in map.Warps)
            text.Append("  warp ").Append(warp.SourceX).Append(',').Append(warp.SourceY).Append(" -> ").Append(warp.TargetMap)
                .Append(' ').Append(warp.TargetX).Append(',').Append(warp.TargetY).AppendLine();
        foreach (var npc in map.Everyone)
        {
            text.Append("  ").Append(npc.Name).Append(" @").Append(npc.GridX).Append(',').Append(npc.GridY);
            if (npc.TrainerData is { } trainer)
            {
                text.Append(" [").Append(trainer.Id).Append(']');
                foreach (var p in trainer.Party.Members)
                    text.Append(' ').Append(p.Species.Name).Append(" L").Append(p.Level).Append(' ').Append(p.Personality.ToString("x8"))
                        .Append(' ').Append(p.Nature).Append(' ').Append(p.Gender);
            }
            text.AppendLine();
        }
        return text.ToString();
    }

    private static readonly System.Reflection.FieldInfo MapsChance =
        typeof(Map).GetField("rng", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
        ?? throw new InvalidOperationException("Map no longer keeps its generator in a field called rng.");

    private static List<string> Build(IReadOnlyList<int> seeds, bool together)
    {
        var worlds = World.LoadAll();
        return MapDatabase.BuildWorlds(worlds, seeds, together).SelectMany(maps => maps).Select(Signature).ToList();
    }

    [Fact]
    public void RegionsBuiltTogetherAreTheRegionsBuiltOneAfterAnother()
    {
        var seeds = Enumerable.Range(0, World.LoadAll().Count).Select(i => 1000 + 7 * i).ToList();
        var oneByOne = Build(seeds, together: false);
        Assert.NotEmpty(oneByOne);
        // Twice, since what two threads do to one another is not the same every time
        for (int round = 0; round < 2; round++)
        {
            var together = Build(seeds, together: true);
            Assert.Equal(oneByOne.Count, together.Count);
            for (int i = 0; i < oneByOne.Count; i++) Assert.Equal(oneByOne[i], together[i]);
        }
    }

    [Fact]
    public void AnotherSeedMakesOtherMaps()
    {
        // The seeds are what the trainers are rolled from: were they not, the test above would hold nothing of the dice
        var sinnoh = World.LoadAll().Where(w => w.Index.Maps.Any(m => m.Name == MapDatabase.StartMap)).ToList();
        string One(int seed) => string.Concat(MapDatabase.BuildWorlds(sinnoh, new[] { seed }, together: false).Single().Select(Signature));
        Assert.Equal(One(5), One(5));
        Assert.NotEqual(One(5), One(6));
    }

    [Fact]
    public void AThreadsDiceAreItsOwn()
    {
        var expected = new Random(42);
        int first = expected.Next(), seedOfNew = expected.Next();
        using (Dice.Scope(new Random(42)))
        {
            Assert.Equal(first, Dice.Shared.Next());
            Assert.Equal(new Random(seedOfNew).Next(), Dice.New().Next());
            // Another thread has none of it
            bool scopedThere = true;
            var other = new System.Threading.Thread(() =>
            {
                var a = new Random(42);
                scopedThere = Dice.Shared.Next() == a.Next() && Dice.Shared.Next() == a.Next() && Dice.Shared.Next() == a.Next();
            });
            other.Start();
            other.Join();
            Assert.False(scopedThere);
            // A scope within a scope gives the outer one back
            using (Dice.Scope(new Random(7))) Assert.Equal(new Random(7).Next(), Dice.Shared.Next());
            Assert.Equal(expected.Next(), Dice.Shared.Next());
        }
    }

    [Fact]
    public void ARecordingSaysHowItsMapsWereBuilt()
    {
        foreach (bool together in new[] { false, true })
        {
            var recording = new InputRecording { DataVersion = GameDataFiles.Version, Seed = 3, RegionsTogether = together };
            using var file = new MemoryStream();
            recording.Write(file);
            file.Position = 0;
            Assert.Equal(together, InputRecording.Read(file).RegionsTogether);
        }
    }
}
