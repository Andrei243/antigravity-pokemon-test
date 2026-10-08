using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Pokémon standing in the field (plan 10 · F1): their cards' sizes, the world's Pokémon placed as themselves, and
/// map files that carry one.
/// </summary>
public class FieldPokemonTests
{
    // Maps of this class's own, read only
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Sinnoh"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));

    private static int CardOf(string species) => FieldSprites.ClassOf(FieldSprites.FieldHeight(FieldSprites.MetresOf(species)));

    [Fact]
    public void ASmallPokemonStandsOnASmallCardAndABigOneOnABigOne()
    {
        // Joltik is a tenth of a metre, Machop eight tenths, Steelix over nine metres
        Assert.Equal(FieldSprites.Small, CardOf("Joltik"));
        Assert.Equal(FieldSprites.Medium, CardOf("Machop"));
        Assert.Equal(FieldSprites.Large, CardOf("Steelix"));

        // Shorter than the player (about 1.7 tiles), and never taller than the biggest card holds
        Assert.True(FieldSprites.FieldHeight(0.8f) < 1.2f);
        Assert.Equal(1.9f, FieldSprites.FieldHeight(14.5f));
        Assert.True(FieldSprites.FieldHeight(1.9f) * CharacterSprites.TexelsPerUnit <= FieldSprites.Large - FieldSprites.FootRows - 1f);

        // A form's height is its own where it has one, its species' otherwise
        Assert.Equal(PokemonDatabase.Get("Giratina")!.Form("Giratina-Origin")?.Height ?? PokemonDatabase.Get("Giratina")!.Height, FieldSprites.MetresOf("Giratina-Origin"));
    }

    [Fact]
    public void EveryPokemonsLooksNameASpeciesThatExists()
    {
        foreach (var file in LoadedWorld.Value.Index.Areas.Select(key => LoadedWorld.Value.Area(key)!))
            foreach (var o in file.Objects)
                if (WorldMapBuilder.SpeciesFor(o.Looks) is { } species)
                    Assert.True(PokemonDatabase.Get(species) != null || PokemonDatabase.SpeciesOfForm(species) != null, $"{file.Key}: {o.Id} looks like '{species}', which is no species");

        // And every Pokémon the maps place is one, and runs the common script unless it has its own
        var everyone = BuiltMaps.Value.Values.SelectMany(m => m.Everyone).Where(n => n.IsPokemon).ToList();
        Assert.NotEmpty(everyone);
        foreach (var pokemon in everyone)
        {
            Assert.True(PokemonDatabase.Get(pokemon.Species!) != null || PokemonDatabase.SpeciesOfForm(pokemon.Species!) != null, pokemon.Species);
            Assert.Equal(pokemon.Script ?? FieldScripts.Pokemon, FieldScripts.For(pokemon));
        }
    }

    [Fact]
    public void TheMinesMachopStandWherePlatinumsDo()
    {
        var mine = BuiltMaps.Value["OreburghMineB2F"];
        var machop = mine.Everyone.Where(n => n.IsPokemon).OrderBy(n => n.Key).ToList();
        Assert.Equal(new[] { "machop_1", "machop_2", "machop_3" }, machop.Select(m => m.Key));
        Assert.All(machop, m => Assert.Equal("Machop", m.Species));
        Assert.Equal(new[] { (12, 28), (20, 15), (27, 15) }, machop.Select(m => (m.GridX, m.GridY)));
        // Each stands in the way like a person, and says something of its own
        Assert.All(machop, m => Assert.Same(m, mine.NpcIn(m.GridX, m.GridY, mine.HeightAt(m.GridX, m.GridY))));
        Assert.All(machop, m => Assert.NotEmpty(m.DialogLines));

        // Giratina in Turnback Cave was drawn as a trainer before: it is a Pokémon now, and keeps its own script
        var giratina = BuiltMaps.Value.Values.SelectMany(m => m.Everyone).First(n => n.Key == "giratina" && n.Script == "Giratina");
        Assert.True(giratina.IsPokemon);
        Assert.Equal("Giratina", giratina.Species);

        // Route 209's Poké Kid wears a Pikachu's looks and is a trainer, not a Pikachu
        Assert.DoesNotContain(BuiltMaps.Value.Values.SelectMany(m => m.Everyone), n => n.IsPokemon && n.IsTrainer);
    }

    [Fact]
    public void AMapFileWithAPokemonRoundTrips()
    {
        MapFile Tiny() => new()
        {
            Name = "Tiny",
            Width = 3,
            Height = 2,
            Ground = new() { "...", "..." },
            Solid = new() { "...", "..." }
        };

        var file = Tiny();
        file.Npcs.Add(new MapFile.NpcRecord { Name = "Machop", NpcType = NPC.PokemonType, Species = "Machop", X = 1, Y = 1, Dialog = new() { "Chop!" } });
        var map = file.ToMap();
        var machop = Assert.Single(map.NPCs);
        Assert.True(machop.IsPokemon);
        Assert.Equal("Machop", machop.Species);
        string text = GameDataFiles.Serialize(MapFile.FromMap(map));
        Assert.Contains("\"species\": \"Machop\"", text);
        Assert.Equal(text, GameDataFiles.Serialize(MapFile.FromMap(file.ToMap())));

        // A Pokémon of no species, or a species on someone who is no Pokémon, doesn't load
        var nobody = Tiny();
        nobody.Npcs.Add(new MapFile.NpcRecord { Name = "Missingno", NpcType = NPC.PokemonType, Species = "Missingno", X = 0, Y = 0 });
        Assert.Contains("no species or form", Assert.Throws<InvalidDataException>(() => nobody.ToMap()).Message);
        var person = Tiny();
        person.Npcs.Add(new MapFile.NpcRecord { Name = "Lass", NpcType = "Lass", Species = "Machop", X = 0, Y = 0 });
        Assert.Contains("is no Pokémon", Assert.Throws<InvalidDataException>(() => person.ToMap()).Message);
    }
}
