using ContentCheck;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// What T14 and T15 left of the packs (plan 16): a pack's <c>ui.json</c> laid over the game's key by key, its
/// hand-drawn sprites looked for before the game's own, its credits on the title's CREDITS, a recording naming the
/// packs it was made with (and the first run), and the content check run over the game with packs laid on it. Packs
/// are handed to each read by hand where the read takes them; what is global (<see cref="Overrides.PackRoots"/>,
/// <see cref="DataLayers.Use"/>) is put back, and the class runs alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class PackLayerTests
{
    private static readonly string FixturePacks = Path.Combine(AppContext.BaseDirectory, "Fixtures", "packs");
    private static readonly string Sample = Path.Combine(Repository.Root, "samples", "packs", "gate-lanterns");

    private static string Scratch()
    {
        string folder = Path.Combine(Path.GetTempPath(), "packlayers-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static DataLayer Pack(string folder, string name, string version = "1.0")
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, DataLayers.ManifestFile),
            $"{{ \"name\": \"{name}\", \"version\": \"{version}\", \"credits\": \"Made for a test.\" }}");
        return new DataLayer(folder, DataLayers.ReadManifest(folder));
    }

    private static void Write(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    [Fact]
    public void APacksUiJsonIsLaidOverTheGamesKeyByKey()
    {
        string root = Scratch();
        try
        {
            var first = Pack(Path.Combine(root, "first"), "first");
            var second = Pack(Path.Combine(root, "second"), "second");
            Write(Path.Combine(first.Root, "text", "en", "ui.json"), "{ \"titleCredits\": { \"title\": \"FIRST\", \"hint\": { \"scroll\": \"Roll\" } } }");
            Write(Path.Combine(second.Root, "text", "en", "ui.json"), "{ \"titleCredits\": { \"title\": \"SECOND\" } }");

            var game = Text.PlainLinesOf("en", Array.Empty<DataLayer>());
            var laid = Text.PlainLinesOf("en", new[] { first, second });
            Assert.Equal("CREDITS", game["titleCredits.title"]);
            // The later pack's key wins, an earlier pack's key nobody later changed stays, and every other key is the game's
            Assert.Equal("SECOND", laid["titleCredits.title"]);
            Assert.Equal("Roll", laid["titleCredits.hint.scroll"]);
            Assert.Equal(game.Count, laid.Count);
            foreach (var (key, value) in game)
                if (key is not ("titleCredits.title" or "titleCredits.hint.scroll"))
                    Assert.Equal(value, laid[key]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ASpriteIsLookedForInEachPackLatestFirstThenBesideTheGame()
    {
        string root = Scratch();
        string own = FieldSprites.OverrideFolder;
        var saved = Overrides.PackRoots.ToList();
        try
        {
            string older = Path.Combine(root, "older"), later = Path.Combine(root, "later");
            FieldSprites.OverrideFolder = Path.Combine(root, "game", "overrides", "sprites", "pokemon");
            Directory.CreateDirectory(Path.Combine(older, "overrides", "sprites", "pokemon", "BULBASAUR"));
            Directory.CreateDirectory(Path.Combine(later, "overrides", "sprites", "pokemon", "BULBASAUR"));
            Directory.CreateDirectory(Path.Combine(FieldSprites.OverrideFolder, "IVYSAUR"));
            Write(Path.Combine(older, "overrides", "sprites", "ROWAN", "a.png"), "older");

            // As Packs.Start fills it: the latest pack first
            Overrides.PackRoots.Clear();
            Overrides.PackRoots.AddRange(new[] { later, older });

            var folders = Overrides.FoldersFor(FieldSprites.OverrideFolder, "sprites", "pokemon");
            Assert.Equal(new[] { Path.Combine(later, "overrides", "sprites", "pokemon"), Path.Combine(older, "overrides", "sprites", "pokemon"), FieldSprites.OverrideFolder }, folders);
            Assert.Equal(Path.Combine(later, "overrides", "sprites", "pokemon", "BULBASAUR"), FieldSprites.DrawnFolder("Bulbasaur"));
            Assert.Equal(Path.Combine(FieldSprites.OverrideFolder, "IVYSAUR"), FieldSprites.DrawnFolder("Ivysaur"));
            // A species no folder has is looked for beside the game, as before packs
            Assert.Equal(Path.Combine(FieldSprites.OverrideFolder, "VENUSAUR"), FieldSprites.DrawnFolder("Venusaur"));
            // A person's sprite: the first pack holding the file
            Assert.Equal(Path.Combine(older, "overrides", "sprites", "ROWAN", "a.png"),
                Overrides.FindFile(Overrides.FoldersFor(Path.Combine(root, "game", "overrides", "sprites"), "sprites"), Path.Combine("ROWAN", "a.png")));
            Assert.Null(Overrides.FindFile(Overrides.FoldersFor(Path.Combine(root, "game"), "sprites"), Path.Combine("ROWAN", "b.png")));

            // With no pack in use, the game's own folder alone
            Overrides.PackRoots.Clear();
            Assert.Equal(new[] { FieldSprites.OverrideFolder }, Overrides.FoldersFor(FieldSprites.OverrideFolder, "sprites", "pokemon"));
        }
        finally
        {
            FieldSprites.OverrideFolder = own;
            Overrides.PackRoots.Clear();
            Overrides.PackRoots.AddRange(saved);
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TheTitlesCreditsShowEachPackInUseAfterTheGamesOwn()
    {
        var problems = new List<string>();
        var packs = DataLayers.Discover(new[] { FixturePacks }, null, problems);
        Assert.Equal(new[] { "alpha", "beta" }, packs.Select(p => p.Pack!.Name));

        var sections = TitleCreditsScreen.OfPacks(packs);
        Assert.Equal(2, sections.Count);
        Assert.Equal("Pack: alpha 1.0", sections[0].Heading);
        Assert.Equal(packs[0].Pack!.Credits, sections[0].Lines[0]);
        Assert.Contains("Made by The test suite.", sections[0].Lines);
        Assert.Contains(packs[0].Pack!.Description!, sections[0].Lines);
        Assert.All(sections, s => Assert.Contains(Text.Of("titleCredits.pack.own"), s.Lines));
        Assert.All(sections.SelectMany(s => s.Lines.Append(s.Heading)), line => Assert.True(line.EnumerateRunes().All(r => PokemonPlatinumEngine.UI.Kit.TextMetrics.CanDraw(r)), line));

        var screen = new TitleCreditsScreen();
        screen.Open(Path.Combine(Path.GetTempPath(), "no-credits-here-" + Guid.NewGuid().ToString("N")), packs);
        Assert.Equal(new[] { TitleCreditsScreen.OwnWork().Heading, "Pack: alpha 1.0", "Pack: beta 2.1" }, screen.Sections.Select(s => s.Heading));

        // With no pack in use the screen is what it was
        screen.Open(Path.Combine(Path.GetTempPath(), "no-credits-here"), Array.Empty<DataLayer>());
        Assert.Single(screen.Sections);
        Assert.Empty(TitleCreditsScreen.OfPacks(Array.Empty<DataLayer>()));
    }

    [Fact]
    public void ARecordingNamesThePacksItWasMadeWithAndTheFirstRun()
    {
        var problems = new List<string>();
        var packs = DataLayers.Discover(new[] { FixturePacks }, null, problems);
        var recording = new InputRecording
        {
            GameVersion = "test",
            DataVersion = GameDataFiles.Version,
            Seed = 5,
            Settings = new GameSettings().InForce(),
            Packs = InputRecording.PacksInUse(packs),
            FirstRun = true
        };
        Assert.Equal(new[] { "alpha 1.0", "beta 2.1" }, recording.Packs);

        using var stream = new MemoryStream();
        recording.Write(stream);
        stream.Position = 0;
        var back = InputRecording.Read(stream);
        Assert.Equal(InputRecording.FormatVersion, back.Format);
        Assert.Equal(recording.Packs, back.Packs);
        Assert.True(back.FirstRun);

        // Played where other packs, or none, are in use, it is refused with both named
        string? why = back.Refusal(packsInUse: Array.Empty<string>());
        Assert.NotNull(why);
        Assert.Contains("alpha 1.0, beta 2.1", why);
        Assert.Contains("none", why);
        Assert.Null(back.Refusal(packsInUse: new[] { "alpha 1.0", "beta 2.1" }));
        Assert.NotNull(back.Refusal(packsInUse: new[] { "beta 2.1", "alpha 1.0" }));

        // A recording of the shape before names none and wasn't made on a first run
        var older = new InputRecording { Format = 2, GameVersion = "test", DataVersion = GameDataFiles.Version, Settings = recording.Settings, Packs = { "alpha 1.0" }, FirstRun = true };
        using var olderStream = new MemoryStream();
        older.Write(olderStream);
        olderStream.Position = 0;
        var olderBack = InputRecording.Read(olderStream);
        Assert.Empty(olderBack.Packs);
        Assert.False(olderBack.FirstRun);
        Assert.Null(olderBack.Refusal());
    }

    [Fact]
    public void AReplayOfAFirstRunOpensOnTheFirstStartsPage()
    {
        var recording = new InputRecording
        {
            GameVersion = "test", DataVersion = GameDataFiles.Version, Seed = 9, Settings = new GameSettings().InForce(), FirstRun = true
        };
        string folder = Scratch();
        var saved = SavePaths.Current;
        try
        {
            var engine = new MoviePlayer(recording).Prepare(folder);
            Assert.True(engine.Settings.FirstRun);
            Assert.Equal(GameState.FirstStart, engine.Drive.State);
            engine.Close();
        }
        finally
        {
            SavePaths.Current = saved;
            Movie.Release();
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void TheMergeLaysAPacksFilesOverTheGamesButNotWhatIsFixed()
    {
        string root = Scratch();
        try
        {
            string game = Path.Combine(root, "game"), into = Path.Combine(root, "merged");
            Write(Path.Combine(game, "scripts", "a.txt"), "game a");
            Write(Path.Combine(game, "scripts", "b.txt"), "game b");
            Write(Path.Combine(game, "world", "sinnoh", "world.json"), "game world");
            Write(Path.Combine(game, "species.json"), "not copied");
            var first = Pack(Path.Combine(root, "first"), "first");
            var second = Pack(Path.Combine(root, "second"), "second");
            Write(Path.Combine(first.Root, "scripts", "a.txt"), "first a");
            Write(Path.Combine(first.Root, "scripts", "c.txt"), "first c");
            Write(Path.Combine(second.Root, "scripts", "a.txt"), "second a");
            Write(Path.Combine(second.Root, "world", "sinnoh", "world.json"), "a pack's world");
            Write(Path.Combine(second.Root, "maps", "Room.json"), "a room");
            Write(Path.Combine(second.Root, "maps", "x.patch.json"), "a patch");

            PackContent.Merge(game, new[] { first, second }, into);
            Assert.Equal("second a", File.ReadAllText(Path.Combine(into, "scripts", "a.txt")));
            Assert.Equal("game b", File.ReadAllText(Path.Combine(into, "scripts", "b.txt")));
            Assert.Equal("first c", File.ReadAllText(Path.Combine(into, "scripts", "c.txt")));
            Assert.Equal("game world", File.ReadAllText(Path.Combine(into, "world", "sinnoh", "world.json")));
            Assert.True(File.Exists(Path.Combine(into, "maps", "Room.json")));
            Assert.False(File.Exists(Path.Combine(into, "maps", "x.patch.json")));
            Assert.False(File.Exists(Path.Combine(into, "species.json")));
            Assert.False(File.Exists(Path.Combine(into, DataLayers.ManifestFile)));

            var without = new Findings { new(Kind.Script, "a", "b", "c") };
            var with = new Findings { new(Kind.Script, "a", "b", "c"), new(Kind.Way, "d", "e", "f") };
            Assert.Equal("d: e: f", Assert.Single(PackContent.Added(without, with)).ToString());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TheSamplePackAddsNothingToTheContentCheckAndABrokenOneIsNamed()
    {
        string root = Scratch();
        try
        {
            var sample = PackContent.Read(new[] { Sample });
            var clean = PackContent.Check(GameDataFiles.Directory, sample, Path.Combine(root, "sample"));
            Assert.True(clean.Count == 0, "the sample adds:\n" + string.Join("\n", clean));

            // A pack whose overlay names a door to a map that isn't there: the check names it, as the game's own
            var broken = Pack(Path.Combine(root, "broken"), "broken");
            string overlay = File.ReadAllText(Path.Combine(GameDataFiles.Directory, "world", "sinnoh", "overlays", "twinleaf_town.json"));
            Write(Path.Combine(broken.Root, "scripts", "zz_pack_test.txt"), "script Talk\n  say \"Hello.\"\n  end\n");
            Write(Path.Combine(broken.Root, "world", "sinnoh", "overlays", "twinleaf_town.json"), overlay);
            Write(Path.Combine(broken.Root, "maps", "PackTestRoom.json"), "{ \"name\": \"NotItsName\" }");
            var found = PackContent.Check(GameDataFiles.Directory, new[] { broken }, Path.Combine(root, "broken-merged"));
            Assert.Contains(found, f => f.File == "maps/PackTestRoom.json");
            Assert.False(Directory.Exists(Path.Combine(root, "broken-merged")));
        }
        finally
        {
            DataLayers.Use(null);
            MusicLibrary.Reload();
            Directory.Delete(root, recursive: true);
        }
    }
}
