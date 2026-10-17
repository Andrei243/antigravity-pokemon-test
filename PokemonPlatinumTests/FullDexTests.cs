using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The game with every species in it (plan 03 · D13): what holds of all 1,025 species and their forms at once. Each
/// can be made at any level with moves of its own, is saved and read back as it was, and fits in the boxes that
/// come with the National Pokédex; and what the game keeps of them in memory and on disk stays bounded (the menu
/// sprites held, the packed mesh cache, the models the start asks for). The pictures and the megabytes are the harness's
/// <c>fulldex</c> mode's.
/// </summary>
public class FullDexTests
{
    private static readonly int[] Levels = { 1, 5, 50, 100 };

    /// <summary>
    /// Every species, and every form by its own name, as the Pokémon a game would hold. A form kept outside battle is
    /// met in it, with its own moves (<see cref="Pokemon.InForm"/>); one taken only during a battle (a Mega, a
    /// Gigantamax form, Cherrim in the sun) is a Pokémon of the species changed into it, which keeps the moves it had.
    /// </summary>
    private static IEnumerable<(string Name, Func<int, Pokemon> Make, bool Met)> Everyone()
    {
        foreach (var species in PokemonDatabase.GetAll().OrderBy(s => s.DexNumber))
        {
            yield return (species.Name, level => new Pokemon(species, level), true);
            foreach (var form in species.Forms ?? new())
            {
                if (form.Kind is FormKind.Look or FormKind.Alternate or FormKind.Regional)
                    yield return (form.Name, level => Pokemon.InForm(species, form.Name, level), true);
                else
                    yield return (form.Name, level =>
                    {
                        var p = new Pokemon(species, level);
                        p.ChangeForm(form.Name);
                        return p;
                    }, false);
            }
        }
    }

    [Fact]
    public void EverySpeciesAndFormMakesAPokemonAtAnyLevelWithMovesOfItsOwn()
    {
        var wrong = new List<string>();
        int made = 0;
        foreach (var (name, make, met) in Everyone())
            foreach (int level in Levels)
            {
                var p = make(level);
                made++;
                if (p.Level != level) wrong.Add($"{name} at {level}: level {p.Level}");
                if (p.MaxHP <= 0 || p.CurrentHP != p.MaxHP) wrong.Add($"{name} at {level}: HP {p.CurrentHP}/{p.MaxHP}");
                if (new[] { p.Attack, p.Defense, p.SpAttack, p.SpDefense, p.Speed }.Any(stat => stat <= 0)) wrong.Add($"{name} at {level}: a stat of nothing");
                if (p.Moves.Count is < 1 or > 4) wrong.Add($"{name} at {level}: {p.Moves.Count} moves");
                // Each move is one it learns by then: none is the stand-in a Pokémon with nothing to know is given
                var learned = p.Learnset.Where(l => l.Level <= level).Select(l => l.MoveName).ToHashSet();
                foreach (var move in p.Moves)
                    if (met && !learned.Contains(move.Name)) wrong.Add($"{name} at {level}: knows {move.Name}, which it doesn't learn by then");
                if (string.IsNullOrEmpty(p.AbilityName) || AbilityDatabase.Get(p.AbilityName) == null) wrong.Add($"{name} at {level}: no ability");
            }
        Assert.True(wrong.Count == 0, $"{wrong.Count} of {made}:\n" + string.Join("\n", wrong.Take(40)));
        Assert.True(made >= (1025 + 500) * Levels.Length, $"only {made} were made");
    }

    [Fact]
    public void EveryMoveALearnsetNamesIsInTheData()
    {
        // MoveDatabase.Get answers an unknown name with Tackle, so a name the data lacks would pass for one
        var named = PokemonDatabase.GetAll().SelectMany(s => new[] { s.Learnset }.Concat((s.Forms ?? new()).Select(f => f.Learnset ?? new())))
            .SelectMany(l => l).Select(l => l.MoveName).Distinct().ToList();
        var unknown = named.Where(name => MoveDatabase.Get(name).Name != name).ToList();
        Assert.True(unknown.Count == 0, "No such move: " + string.Join(", ", unknown));
    }

    [Fact]
    public void EverySpeciesAndFormIsSavedAndReadBackAsItWas()
    {
        var wrong = new List<string>();
        foreach (var (name, make, _) in Everyone())
        {
            var p = make(37);
            // Through the save's own text, as a game writes it
            var boxes = new PcBoxes();
            boxes.Store(p);
            string json = SaveManager.Write(new SaveData { Boxes = SavedBoxes.From(boxes) });
            var back = SaveManager.Read(json)!.Boxes!.ToBoxes()[0, 0];
            if (back == null) { wrong.Add($"{name}: not read back"); continue; }
            if (back.Species != p.Species) wrong.Add($"{name}: came back a {back.Species.Name}");
            if (back.Form != p.Form) wrong.Add($"{name}: form {back.Form ?? "none"} for {p.Form ?? "none"}");
            if (back.ModelName != p.ModelName || back.DisplayName != p.DisplayName) wrong.Add($"{name}: called {back.DisplayName}, drawn as {back.ModelName}");
            if (back.Level != p.Level || back.Nature != p.Nature || back.Gender != p.Gender || back.Personality != p.Personality || back.AbilityName != p.AbilityName)
                wrong.Add($"{name}: not the same Pokémon");
            if ((back.MaxHP, back.Attack, back.Defense, back.SpAttack, back.SpDefense, back.Speed) != (p.MaxHP, p.Attack, p.Defense, p.SpAttack, p.SpDefense, p.Speed))
                wrong.Add($"{name}: its stats changed");
            if (!back.Moves.Select(m => m.Name).SequenceEqual(p.Moves.Select(m => m.Name))) wrong.Add($"{name}: its moves changed");
        }
        Assert.True(wrong.Count == 0, $"{wrong.Count}:\n" + string.Join("\n", wrong.Take(40)));
    }

    [Fact]
    public void ASpeciesWhoseNameTheDataNoLongerHasIsFoundByItsNumber()
    {
        var saved = SavedPokemonData.FromPokemon(new Pokemon(PokemonDatabase.Get("Farfetch'd")!, 20));
        Assert.Equal(83, saved.DexNumber);
        // As if a later import had respelt the name
        saved.SpeciesName = "Farfetch’d";
        saved.Nickname = "Farfetch’d";
        var back = saved.ToPokemon();
        Assert.Equal("Farfetch'd", back.Species.Name);
        // It had no name of its own, so it is called what its species is called now
        Assert.Equal("Farfetch'd", back.DisplayName);

        // One with a name of its own keeps it
        saved.Nickname = "Leeky";
        Assert.Equal("Leeky", saved.ToPokemon().DisplayName);

        // A save from before the number was kept goes by the name alone, as ever
        var old = SavedPokemonData.FromPokemon(new Pokemon(PokemonDatabase.Get("Piplup")!, 5));
        old.DexNumber = 0;
        Assert.Equal("Piplup", old.ToPokemon().Species.Name);
        Assert.DoesNotContain("DexNumber", System.Text.Json.JsonSerializer.Serialize(old));
    }

    // ------------------------------------------------------------------ the boxes

    [Fact]
    public void TheBoxesHoldOneOfEverySpeciesOfTheirPokedex()
    {
        // Platinum's eighteen boxes held one of each species its own National Pokédex had
        Assert.True(new PcBoxes().Capacity >= 493);
        // The boxes that come with this game's National Pokédex do the same for all of its species
        int species = PokemonDatabase.GetAll().Count();
        Assert.True(new PcBoxes(PcBoxes.NationalBoxCount).Capacity >= species, $"{PcBoxes.NationalBoxCount} boxes don't hold {species} species");
        Assert.Equal(18, new PcBoxes().Boxes.Count);
        // Never fewer than Platinum's, nor more than the National Pokédex's
        Assert.Equal(PcBoxes.BoxCount, new PcBoxes(3).Boxes.Count);
        Assert.Equal(PcBoxes.NationalBoxCount, new PcBoxes(500).Boxes.Count);
    }

    [Fact]
    public void TheBoxesGrowWithTheNationalPokedexAndKeepWhatTheyHeld()
    {
        var pc = new PcBoxes { CurrentBox = 17 };
        pc.Rename(17, "LAST");
        pc.Boxes[17].Wallpaper = 3;
        var everyone = PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).ToList();
        int stored = 0;
        foreach (var species in everyone)
        {
            if (pc.Store(new Pokemon(species, 5)) == null) break;
            stored++;
        }
        Assert.Equal(PcBoxes.BoxCount * PcBoxes.BoxSize, stored);
        Assert.True(pc.IsFull);
        Assert.Equal(0, pc.Free);

        Assert.True(pc.Grow());
        Assert.False(pc.Grow());
        Assert.Equal(PcBoxes.NationalBoxCount, pc.Boxes.Count);
        Assert.False(pc.IsFull);
        Assert.Equal(stored, pc.Count);
        Assert.Equal(("LAST", 3), (pc.Boxes[17].Name, pc.Boxes[17].Wallpaper));
        Assert.Equal("Box 19", pc.Boxes[18].Name);
        // The box the PC was on took the first thirty, and they are where they were
        Assert.Equal(everyone[0], pc[17, 0]!.Species);
        Assert.Equal(everyone[30], pc[0, 0]!.Species);

        // And now the rest go in: a place for one of every species, the current box first and then round
        foreach (var species in everyone.Skip(stored)) Assert.NotNull(pc.Store(new Pokemon(species, 5)));
        Assert.Equal(everyone.Count, pc.Count);
        Assert.Equal(everyone.Select(s => s.DexNumber).OrderBy(n => n), pc.All.Select(p => p.Species.DexNumber).OrderBy(n => n));
    }

    [Fact]
    public void ThePcsScreenGoesThroughAHundredBoxesAndAHeldKeyStopsAtTheEnds()
    {
        Assert.Equal(100, PcBoxes.NationalBoxCount);
        var boxes = new PcBoxes(PcBoxes.NationalBoxCount);
        // The last has a name that fits a box's eight letters
        Assert.Equal("Box 100", boxes.Boxes[^1].Name);
        var pc = new PCScreen();
        pc.Open(boxes);
        Assert.Equal(PcBoxes.NationalBoxCount, pc.BoxCount);
        pc.Zone = StorageZone.BoxName;
        // A press goes round from the first box to the last and back
        pc.Move(-1, 0, 0);
        Assert.Equal(PcBoxes.NationalBoxCount - 1, pc.Box);
        pc.Move(1, 0, 0);
        Assert.Equal(0, pc.Box);
        // A key kept down runs on through them and stops at either end
        for (int i = 0; i < PcBoxes.NationalBoxCount + 20; i++) pc.Move(1, 0, 0, held: true);
        Assert.Equal(PcBoxes.NationalBoxCount - 1, pc.Box);
        for (int i = 0; i < PcBoxes.NationalBoxCount + 20; i++) pc.Move(-1, 0, 0, held: true);
        Assert.Equal(0, pc.Box);
        // And a Pokémon put away goes into the box on the screen, or the next with room, however many there are
        pc.Box = PcBoxes.NationalBoxCount - 1;
        Assert.Equal((PcBoxes.NationalBoxCount - 1) * PCScreen.BoxSize, pc.StoredIndex - pc.Cell);
    }

    [Fact]
    public void ASaveKeepsAsManyBoxesAsThePcHas()
    {
        int last = PcBoxes.NationalBoxCount - 1;
        var pc = new PcBoxes(PcBoxes.NationalBoxCount) { CurrentBox = last - 5 };
        pc.PutIn(last, 29, new Pokemon(PokemonDatabase.Get("Pecharunt")!, 88));
        pc.Rename(last, "THE END");
        var back = SaveManager.Read(SaveManager.Write(new SaveData { Boxes = SavedBoxes.From(pc) }))!.Boxes!.ToBoxes();
        Assert.Equal(PcBoxes.NationalBoxCount, back.Boxes.Count);
        Assert.Equal(last - 5, back.CurrentBox);
        Assert.Equal("THE END", back.Boxes[last].Name);
        Assert.Equal("Pecharunt", back[last, 29]!.Species.Name);

        // A save of Platinum's eighteen stays at eighteen until the game gives the National Pokédex
        var plain = SaveManager.Read(SaveManager.Write(new SaveData { Boxes = SavedBoxes.From(new PcBoxes()) }))!.Boxes!.ToBoxes();
        Assert.Equal(PcBoxes.BoxCount, plain.Boxes.Count);
    }

    [Fact]
    public void ACompletePokedexIsSavedWholeAndEarnsBothDiplomas()
    {
        var dex = new Pokedex();
        foreach (var species in PokemonDatabase.GetAll()) dex.RegisterCaught(species.DexNumber);
        dex.UnlockNational();
        Assert.Equal(new[] { Pokedex.Sinnoh, Pokedex.National }, dex.AwardDiplomas());
        Assert.Equal((210, 210), dex.Progress(Pokedex.Sinnoh));
        Assert.Equal(1025, dex.CaughtIn(Pokedex.National));

        var save = new SaveData { SeenSpecies = dex.SeenSpecies.OrderBy(n => n).ToList(), CaughtSpecies = dex.CaughtSpecies.OrderBy(n => n).ToList(), NationalPokedex = true };
        var read = SaveManager.Read(SaveManager.Write(save))!;
        Assert.Equal(1025, read.SeenSpecies.Count);
        Assert.Equal(Enumerable.Range(1, 1025), read.CaughtSpecies);
    }

    // ------------------------------------------------------------------ what is held

    [Fact]
    public void TheMenuSpritesLongestUnaskedForGoWhenTooManyAreHeld()
    {
        var ledger = new SpriteLedger(capacity: 3, fresh: 2);
        ledger.Keep("?", forGood: true);
        foreach (string name in new[] { "Bulbasaur", "Ivysaur", "Venusaur" })
        {
            ledger.Keep(name);
            ledger.Tick();
        }
        // Four are held where three may be: the one longest unasked for goes, never the one kept for good, nor one
        // asked for a moment ago
        Assert.Equal(4, ledger.Count);
        Assert.Equal(new[] { "Bulbasaur" }, ledger.Overflow());

        // A sprite asked for again is the newest; the longest unasked for goes in its place
        for (int i = 0; i < 5; i++) ledger.Tick();
        ledger.Touch("Bulbasaur");
        Assert.Equal(new[] { "Ivysaur" }, ledger.Overflow());
        ledger.Forget("Ivysaur");
        Assert.Empty(ledger.Overflow());

        // A screen that shows more than may be held keeps them all while it shows them
        foreach (string name in new[] { "Charmander", "Charmeleon", "Charizard", "Squirtle" }) ledger.Keep(name);
        Assert.Equal(new[] { "Venusaur" }, ledger.Overflow());
        foreach (string gone in ledger.Overflow()) ledger.Forget(gone);
        Assert.Equal(6, ledger.Count);
        Assert.Empty(ledger.Overflow());
        // Once they are no longer asked for, down to what may be held, never the one kept for good
        for (int i = 0; i < 5; i++) ledger.Tick();
        ledger.Touch("Squirtle");
        var going = ledger.Overflow();
        Assert.Equal(3, going.Count);
        Assert.DoesNotContain("?", going);
        Assert.DoesNotContain("Squirtle", going);
        Assert.True(ledger.Holds("?"));
    }

    [Fact]
    public void TheGameHoldsFarFewerSpritesThanThereAreSpecies()
    {
        // A box of thirty and a team of six, a Pokédex page, a Hall of Fame: all at once and well within it
        Assert.InRange(PokemonSprites.Kept, 4 * PcBoxes.BoxSize, PokemonDatabase.GetAll().Count() / 2);
        Assert.True(PokemonSprites.FreshFrames >= 60);
    }

    [Fact]
    public void TheStartAsksOnlyForTheModelsItsFirstScreensDraw()
    {
        // The title's Giratina and the introduction's Pokémon are drawn the moment their screens open; every other
        // model is made when a scene asks for it. Their menu sprites are among those the start makes ready
        Assert.Contains("Giratina", PokemonModels.ShownAtOnce);
        Assert.Contains(new PokemonPlatinumEngine.UI.IntroScreen().ShownSpecies, PokemonModels.ShownAtOnce);
        Assert.True(PokemonModels.ShownAtOnce.Length <= 4, "the start asks for more models than its first screens draw");
        Assert.All(PokemonModels.ShownAtOnce, species => Assert.Contains(species, PokemonModels.Preloaded));
    }

    // ------------------------------------------------------------------ the mesh cache

    private static SdfMesh SampleMesh()
    {
        var model = new SdfModel("full dex test") { BoneCount = 2 };
        model.Capsule(new System.Numerics.Vector3(0, 0, 0), new System.Numerics.Vector3(0, 0.5f, 0), 0.16f, 0.1f, new Raylib_cs.Color(210, 60, 50, 255), 0);
        model.Sphere(new System.Numerics.Vector3(0, 0.62f, 0), 0.2f, new Raylib_cs.Color(60, 90, 220, 255), 1, blend: 0.05f);
        return SdfMesher.Mesh(model, 0.012f);
    }

    private static void AssertSame(SdfMesh mesh, SdfMesh back)
    {
        Assert.Equal(mesh.Positions, back.Positions);
        Assert.Equal(mesh.Normals, back.Normals);
        Assert.Equal(mesh.Colors, back.Colors);
        Assert.Equal(mesh.Materials, back.Materials);
        Assert.Equal(mesh.BoneIndices, back.BoneIndices);
        Assert.Equal(mesh.BoneWeights, back.BoneWeights);
        Assert.Equal(mesh.Indices, back.Indices);
    }

    [Fact]
    public void AMeshIsKeptPackedBitForBitAndInFarLessRoom()
    {
        var mesh = SampleMesh();
        Assert.True(mesh.VertexCount > 2000, $"{mesh.VertexCount} vertices");
        using var packed = new MemoryStream();
        SdfCache.Write(packed, mesh);
        using var plain = new MemoryStream();
        SdfCache.WriteUnpacked(plain, mesh);
        // Under half of what it was written out number by number (the game's own models come to about a third)
        Assert.True(packed.Length * 2 < plain.Length, $"{packed.Length} bytes packed, {plain.Length} as it was");

        packed.Position = 0;
        var back = SdfCache.Read(packed, out bool wasPacked);
        Assert.True(wasPacked);
        AssertSame(mesh, back);
    }

    [Fact]
    public void AMeshKeptAsItWasFirstWrittenIsStillRead()
    {
        var mesh = SampleMesh();
        using var plain = new MemoryStream();
        SdfCache.WriteUnpacked(plain, mesh);
        plain.Position = 0;
        var back = SdfCache.Read(plain, out bool wasPacked);
        Assert.False(wasPacked);
        AssertSame(mesh, back);
    }

    [Fact]
    public void ADamagedMeshFileIsRefusedAndNotTakenForAMesh()
    {
        var mesh = SampleMesh();
        using var whole = new MemoryStream();
        SdfCache.Write(whole, mesh);
        byte[] bytes = whole.ToArray();

        // What the cache takes for "mesh it again": nothing else may come out of a file that isn't whole
        static void Refused(byte[] file)
        {
            var e = Record.Exception(() => SdfCache.Read(new MemoryStream(file), out _));
            Assert.True(e is IOException or InvalidDataException, e?.GetType().Name ?? "it was read");
        }

        // Cut short, as a write the power went out on
        Refused(bytes[..(bytes.Length / 2)]);
        Refused(bytes[..10]);
        // Its counts claiming more than any model has
        var huge = (byte[])bytes.Clone();
        BitConverter.TryWriteBytes(huge.AsSpan(8), int.MaxValue);
        Refused(huge);
        // Not a mesh at all
        Refused(new byte[64]);
        // Its packed part scrambled
        var scrambled = (byte[])bytes.Clone();
        for (int i = 16; i < scrambled.Length; i += 7) scrambled[i] ^= 0x5A;
        Refused(scrambled);
    }
}
