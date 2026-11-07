using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 10 · F10, its rules alone (docs/mechanics/rulings.md, "Alphas"): an alpha row of the post-game zone gives the
/// size, three perfect IVs and a move ahead of its level; the size is saved and copied; a catch carries the Alpha mark
/// alone; the cry is lower; and no area of Platinum's own lists one.
/// </summary>
public class CompanionAlphaTests
{
    private static int Perfect(Pokemon p) => new[] { p.IvHP, p.IvAttack, p.IvDefense, p.IvSpAttack, p.IvSpDefense, p.IvSpeed }.Count(v => v == 31);

    [Fact]
    public void AnAlphaRowGivesTheSizeTheIvsAndAMoveAheadOfItsLevel()
    {
        var list = new List<OursSlot> { new("Machamp", 55, 57) };
        int hits = 0;
        WildEncounterEntry? row = null;
        for (int seed = 0; seed < 4000 && row == null; seed++)
        {
            row = Alphas.Roll(list, new Random(seed));
            if (row != null) hits++;
        }
        Assert.NotNull(row);
        Assert.True(row!.Alpha);
        Assert.Equal((55, 57), (row.MinLevel, row.MaxLevel));

        var p = new Pokemon(PokemonDatabase.Get("Machamp")!, 55, new Random(4));
        var known = p.Moves.Select(m => m.Data.Name).ToHashSet();
        Assert.False(p.IsAlpha);
        Alphas.Apply(p, new Random(1));
        Assert.True(p.IsAlpha);
        Assert.Equal(Alphas.Scale, p.Scale);
        Assert.True(Perfect(p) >= Alphas.PerfectIvs);
        Assert.Equal(4, p.Moves.Count);
        Assert.Contains(p.Moves, m => !known.Contains(m.Data.Name));
        // The same draw gives the same alpha
        var q = new Pokemon(PokemonDatabase.Get("Machamp")!, 55, new Random(4));
        Alphas.Apply(q, new Random(1));
        Assert.Equal(p.Moves.Select(m => m.Data.Name), q.Moves.Select(m => m.Data.Name));
        Assert.Equal(p.IvSpeed, q.IvSpeed);
    }

    [Fact]
    public void AnAlphaIsOneStepInAHundredAndAnAreaWithNoneDrawsNothing()
    {
        var list = new List<OursSlot> { new("Fearow", 55, 57) };
        var rng = new Random(77);
        int alphas = 0;
        for (int i = 0; i < 20000; i++) if (Alphas.Roll(list, rng) != null) alphas++;
        Assert.InRange(alphas, 130, 280);

        // An empty list draws nothing: the generator stands where it was
        var a = new Random(5);
        var b = new Random(5);
        Assert.Null(Alphas.Roll(new List<OursSlot>(), a));
        Assert.Equal(b.Next(), a.Next());
    }

    [Fact]
    public void TheSizeIsSavedCopiedAndOneForEveryoneElse()
    {
        var p = new Pokemon(PokemonDatabase.Get("Fearow")!, 55);
        Assert.Equal(1f, p.Scale);
        Assert.Null(SavedPokemonData.FromPokemon(p).Scale);
        Alphas.Apply(p, new Random(2));
        var saved = SavedPokemonData.FromPokemon(p);
        Assert.Equal(Alphas.Scale, saved.Scale);
        var back = System.Text.Json.JsonSerializer.Deserialize<SavedPokemonData>(GameDataFiles.Serialize(saved), GameDataFiles.Json)!.ToPokemon();
        Assert.True(back.IsAlpha);
        Assert.Equal(Alphas.Scale, back.Scale);
        Assert.Equal(Alphas.Scale, p.Clone().Scale);
        var other = new Pokemon(PokemonDatabase.Get("Fearow")!, 55);
        other.CopyStateFrom(p);
        Assert.Equal(Alphas.Scale, other.Scale);
        // A size nobody wrote is cut to the alpha's
        var odd = SavedPokemonData.FromPokemon(p);
        odd.Scale = 9f;
        Assert.Equal(Alphas.Scale, odd.ToPokemon().Scale);
    }

    [Fact]
    public void ACatchOfAnAlphaCarriesTheAlphaMarkAndNoOther()
    {
        var p = new Pokemon(PokemonDatabase.Get("Fearow")!, 55);
        Alphas.Apply(p, new Random(2));
        for (uint personality = 0; personality < 3000; personality++)
        {
            p.Personality = personality;
            p.EncounterMarks.Clear();
            EncounterMarks.Give(p, new MarkMoment(TimeOfDay.Night, FieldWeather.Rain, false));
            Assert.Equal(new[] { nameof(Mark.Alpha) }, p.EncounterMarks.ToArray());
        }
        var plain = new Pokemon(PokemonDatabase.Get("Fearow")!, 55);
        for (uint personality = 0; personality < 3000; personality++)
        {
            plain.Personality = personality;
            plain.EncounterMarks.Clear();
            EncounterMarks.Give(plain, new MarkMoment(TimeOfDay.Night, FieldWeather.Rain, false));
            Assert.DoesNotContain(nameof(Mark.Alpha), plain.EncounterMarks);
        }
    }

    [Fact]
    public void AnAlphaCriesLowerAndLonger()
    {
        var plain = new Pokemon(PokemonDatabase.Get("Bidoof")!, 55);
        var alpha = plain.Clone();
        alpha.Scale = Alphas.Scale;
        var normal = Cries.Get(plain);
        var low = Cries.Get(alpha);
        Assert.NotSame(normal, low);
        double ratio = Math.Pow(2, -Alphas.CrySemitones / 12.0);
        Assert.InRange(low.Duration / normal.Duration, ratio * 0.99, ratio * 1.01);
        Assert.Same(low, Cries.Get(alpha));
        Assert.Same(normal, Cries.Get(plain));
    }

    [Fact]
    public void AnAlphaTurnsUpOnlyAfterTheHallOfFameInAnAreaThatListsOne()
    {
        var map = new Map(32, 32);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++) map.SetGroundTile(x, y, TileType.TallGrass);
        var area = new MapArea { Key = "test_alpha_zone" };
        for (int i = 0; i < 12; i++) area.WildEncounters.Add(new WildEncounterEntry { SpeciesName = "Bidoof", MinLevel = 5, MaxLevel = 5, Weight = 1 });
        area.AlphaLand.Add(new OursSlot("Machamp", 55, 57));
        for (int cy = 0; cy < map.ChunkRows; cy++)
            for (int cx = 0; cx < map.ChunkColumns; cx++) map.SetArea(cx, cy, area);

        int Count(bool open)
        {
            var moment = new EncounterMoment { Time = TimeOfDay.Day, Today = new DateTime(2026, 1, 1), AlphasOpen = open };
            int found = 0;
            for (int i = 0; i < 6000; i++)
                if (map.DrawOutWild(10, 10, false, null, moment) is { Alpha: true } alpha) { found++; Assert.Equal("Machamp", alpha.SpeciesName); }
            return found;
        }
        Assert.Equal(0, Count(false));
        Assert.InRange(Count(true), 20, 120);
    }

    [Fact]
    public void NoAreaOfPlatinumsOwnListsAnAlphaAndTheZonesDoAboveTheirGrass()
    {
        string[] postGame = { "route_225", "route_226", "route_227" };
        var folder = Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Data", "world", "sinnoh");
        foreach (var file in Directory.GetFiles(Path.Combine(folder, "overlays"), "*.json"))
        {
            string key = Path.GetFileNameWithoutExtension(file);
            var overlay = GameDataFiles.Load<WorldOverlayFile>(file, strict: true);
            if (overlay.Alphas == null) continue;
            Assert.Contains(key, postGame);
            Assert.InRange(overlay.Alphas.Land?.Count ?? 0, 1, OverlayAlphas.Most);
            var area = GameDataFiles.Load<WorldAreaFile>(Path.Combine(folder, "areas", key + ".json"));
            int top = area.Land.Max(s => s.Level);
            foreach (var slot in overlay.Alphas.Land!)
            {
                Assert.NotNull(PokemonDatabase.Get(slot.Species));
                Assert.True(slot.MinLevel > top, $"{key}: {slot.Species} is not above the grass ({top})");
                Assert.True(slot.MaxLevel >= slot.MinLevel);
            }
        }
        // And no table row is ever read as one: the flag is never in a file
        Assert.Contains("JsonIgnore", typeof(WildEncounterEntry).GetProperty(nameof(WildEncounterEntry.Alpha))!.GetCustomAttributes(false).Select(a => a.GetType().Name).Single());
    }
}
