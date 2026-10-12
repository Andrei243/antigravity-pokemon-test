using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Core.Diagnostics;
using Program = PokemonPlatinumEngine.Program;

namespace PokemonPlatinumTests;

/// <summary>
/// Where the saves live (plan 12 · Q2): which folder the game chooses on a machine and why, and what it calls each
/// file there. Every machine here is made up: its folders are in a temporary folder of the test's own, so nothing is
/// ever asked of, made in or moved in a real user's folder. Deciding is held to touch nothing at all.
/// </summary>
public class SavePathsTests
{
    /// <summary>A made-up machine: the folder the game was started in and the one its executable is in are there, the user's own folders are only names.</summary>
    private sealed class Machine : IDisposable
    {
        public string Root { get; } = Directory.CreateTempSubdirectory("pokemon-paths-").FullName;
        public string StartedIn => Path.Combine(Root, "started-in");
        public string Beside => Path.Combine(Root, "the-game");
        public string AppData => Path.Combine(Root, "users", "ana", "AppData", "Roaming");
        public string Home => Path.Combine(Root, "users", "ana");
        public string Xdg => Path.Combine(Root, "users", "ana", "data");

        /// <summary>Every file the game asked about while it decided.</summary>
        public List<string> Asked { get; } = new();

        public Machine()
        {
            Directory.CreateDirectory(StartedIn);
            Directory.CreateDirectory(Beside);
        }

        public SaveEnvironment On(SavePlatform platform = SavePlatform.Windows, bool xdg = false, bool named = true) =>
            new(StartedIn, Beside, platform, named ? AppData : null, named && xdg ? Xdg : null, named ? Home : null)
            {
                FileExists = path =>
                {
                    Asked.Add(path);
                    return File.Exists(path);
                }
            };

        public void Put(string folder, string name) => File.WriteAllText(Path.Combine(folder, name), "");

        /// <summary>Everything under the machine's root, as paths relative to it.</summary>
        public List<string> Everything() => Directory.EnumerateFileSystemEntries(Root, "*", SearchOption.AllDirectories)
            .Select(p => Path.GetRelativePath(Root, p)).Order(StringComparer.Ordinal).ToList();

        public void Dispose()
        {
            try { Directory.Delete(Root, recursive: true); }
            catch (IOException) { }
        }
    }

    // ------------------------------------------------------------------ which folder

    [Fact]
    public void AFreshInstallKeepsItsSavesInTheUsersOwnFolder()
    {
        using var machine = new Machine();
        var windows = SavePaths.Resolve(machine.On(SavePlatform.Windows));
        Assert.Equal((Path.Combine(machine.AppData, "PokemonPlatinum"), SaveHome.PerUser), (windows.Folder, windows.Home));

        var linux = SavePaths.Resolve(machine.On(SavePlatform.Linux, xdg: true));
        Assert.Equal((Path.Combine(machine.Xdg, "pokemon-platinum"), SaveHome.PerUser), (linux.Folder, linux.Home));
        var plain = SavePaths.Resolve(machine.On(SavePlatform.Linux));
        Assert.Equal(Path.Combine(machine.Home, ".local", "share", "pokemon-platinum"), plain.Folder);

        var mac = SavePaths.Resolve(machine.On(SavePlatform.MacOS));
        Assert.Equal((Path.Combine(machine.Home, "Library", "Application Support", "PokemonPlatinum"), SaveHome.PerUser), (mac.Folder, mac.Home));

        // The folder is only named: deciding makes nothing, and the user's folders are never so much as asked about
        Assert.Equal(new[] { "started-in", "the-game" }, machine.Everything());
        Assert.NotEmpty(machine.Asked);
        Assert.All(machine.Asked, path => Assert.True(Path.GetDirectoryName(path) == machine.StartedIn || Path.GetDirectoryName(path) == machine.Beside, path));
    }

    [Fact]
    public void TheUsersOwnFolderIsEachSystemsOwnPlaceForIt()
    {
        // (The strings alone: no folder of these names is made or looked at)
        Assert.Equal(Path.Combine(@"C:\Users\ana\AppData\Roaming", "PokemonPlatinum"),
            new SaveEnvironment("w", "b", SavePlatform.Windows, AppData: @"C:\Users\ana\AppData\Roaming").PerUserFolder);
        Assert.Equal(Path.Combine("/home/ana/.data", "pokemon-platinum"),
            new SaveEnvironment("w", "b", SavePlatform.Linux, XdgDataHome: "/home/ana/.data", Home: "/home/ana").PerUserFolder);
        Assert.Equal(Path.Combine("/home/ana", ".local", "share", "pokemon-platinum"),
            new SaveEnvironment("w", "b", SavePlatform.Linux, Home: "/home/ana").PerUserFolder);
        Assert.Equal(Path.Combine("/Users/ana", "Library", "Application Support", "PokemonPlatinum"),
            new SaveEnvironment("w", "b", SavePlatform.MacOS, Home: "/Users/ana").PerUserFolder);

        // A machine that doesn't say where a user's files go names no folder; neither does a variable set to nothing
        // or to a bare name. Each system reads its own variable and no other's
        Assert.Null(new SaveEnvironment("w", "b", SavePlatform.Windows).PerUserFolder);
        Assert.Null(new SaveEnvironment("w", "b", SavePlatform.Windows, AppData: "  ").PerUserFolder);
        Assert.Null(new SaveEnvironment("w", "b", SavePlatform.Windows, AppData: "Roaming").PerUserFolder);
        Assert.Null(new SaveEnvironment("w", "b", SavePlatform.Windows, Home: "/home/ana", XdgDataHome: "/home/ana/.data").PerUserFolder);
        Assert.Null(new SaveEnvironment("w", "b", SavePlatform.Linux, AppData: @"C:\Users\ana\AppData\Roaming").PerUserFolder);
        Assert.Null(new SaveEnvironment("w", "b", SavePlatform.MacOS, XdgDataHome: "/home/ana/.data").PerUserFolder);
    }

    [Theory]
    [InlineData("savegame.json")]   // the one save an install had before there were slots
    [InlineData("savegame.bak")]
    [InlineData("save1.json")]      // a slot of a game that has kept its saves here since
    [InlineData("save2.bak")]
    [InlineData("save3.json")]
    [InlineData("autosave.json")]
    [InlineData("settings.json")]   // a folder the game has run in, whether or not anything was saved
    public void AnOldInstallKeepsItsFilesWhereTheyWere(string itsFile)
    {
        // The risk the plan names first: a player who ran the game from a folder and keeps running it there must find
        // their save. A file the game itself wrote in the folder it is started in keeps everything there
        using var machine = new Machine();
        machine.Put(machine.StartedIn, itsFile);
        var paths = SavePaths.Resolve(machine.On());
        Assert.Equal((machine.StartedIn, SaveHome.Portable), (paths.Folder, paths.Home));
        Assert.Contains(itsFile, paths.Why);
        Assert.Contains(itsFile, SavePaths.OwnFiles());

        // Whatever else is in that folder says nothing: a fresh folder with other files is a fresh install
        using var other = new Machine();
        other.Put(other.StartedIn, "notes.txt");
        other.Put(other.StartedIn, "savegame.crash.json");
        other.Put(other.StartedIn, "log.txt");
        Assert.Equal(SaveHome.PerUser, SavePaths.Resolve(other.On()).Home);
    }

    [Fact]
    public void APortableTxtKeepsTheSavesBesideTheGame()
    {
        using var machine = new Machine();
        Assert.Equal("portable.txt", SavePaths.PortableMarker);

        // Beside the executable: its folder, wherever the game is started from
        machine.Put(machine.Beside, SavePaths.PortableMarker);
        var beside = SavePaths.Resolve(machine.On());
        Assert.Equal((machine.Beside, SaveHome.Portable), (beside.Folder, beside.Home));

        // In the folder the game is started in: that folder, which comes first
        machine.Put(machine.StartedIn, SavePaths.PortableMarker);
        var here = SavePaths.Resolve(machine.On());
        Assert.Equal((machine.StartedIn, SaveHome.Portable), (here.Folder, here.Home));
    }

    [Fact]
    public void AGamesOwnFilesWhereItIsStartedComeBeforeAMarkerBesideTheExecutable()
    {
        // A developer runs the game from the repository, where its save is; the executable is in a build folder
        using var machine = new Machine();
        machine.Put(machine.Beside, SavePaths.PortableMarker);
        machine.Put(machine.StartedIn, SavePaths.OldSaveName);
        Assert.Equal(machine.StartedIn, SavePaths.Resolve(machine.On()).Folder);
    }

    [Fact]
    public void TheSwitchesWinOverWhateverIsFound()
    {
        using var machine = new Machine();
        machine.Put(machine.StartedIn, SavePaths.OldSaveName);
        machine.Put(machine.Beside, SavePaths.PortableMarker);
        string elsewhere = Path.Combine(machine.Root, "elsewhere");

        // --save names the folder: a full path as it is, any other from the folder the game was started in
        var chosen = SavePaths.Resolve(machine.On(), chosen: elsewhere);
        Assert.Equal((elsewhere, SaveHome.Chosen), (chosen.Folder, chosen.Home));
        Assert.Equal(Path.Combine(machine.StartedIn, "my saves"), SavePaths.Resolve(machine.On(), chosen: "my saves").Folder);
        Assert.Equal(Path.Combine(machine.Root, "up"), SavePaths.Resolve(machine.On(), chosen: Path.Combine("..", "up")).Folder);
        // ... over --portable too, though the command line refuses the two together
        Assert.Equal(elsewhere, SavePaths.Resolve(machine.On(), portable: true, chosen: elsewhere).Folder);

        // --portable is the folder the game was started in, on a machine with nothing in it too
        using var fresh = new Machine();
        var portable = SavePaths.Resolve(fresh.On(), portable: true);
        Assert.Equal((fresh.StartedIn, SaveHome.Portable), (portable.Folder, portable.Home));
        // A switch decides without looking at anything
        Assert.Empty(fresh.Asked);
        Assert.Equal(new[] { "started-in", "the-game" }, fresh.Everything());
    }

    [Fact]
    public void AMachineThatNamesNoFolderKeepsTheSavesWhereTheGameWasStarted()
    {
        using var machine = new Machine();
        foreach (var platform in Enum.GetValues<SavePlatform>())
        {
            var paths = SavePaths.Resolve(machine.On(platform, named: false));
            Assert.Equal((machine.StartedIn, SaveHome.Portable), (paths.Folder, paths.Home));
        }
    }

    // ------------------------------------------------------------------ what is in it

    [Fact]
    public void OneFolderHoldsEverythingTheGameWrites()
    {
        using var machine = new Machine();
        var paths = SavePaths.In(machine.StartedIn);
        Assert.Equal(machine.StartedIn, paths.Folder);
        Assert.Equal(3, SavePaths.Slots);
        Assert.Equal(new[] { "save1.json", "save2.json", "save3.json" }, Enumerable.Range(1, SavePaths.Slots).Select(SavePaths.SlotName));
        for (int slot = 1; slot <= SavePaths.Slots; slot++)
        {
            Assert.Equal(Path.Combine(machine.StartedIn, $"save{slot}.json"), paths.Slot(slot));
            Assert.Equal(Path.Combine(machine.StartedIn, $"save{slot}.bak"), SaveManager.BackupOf(paths.Slot(slot)));
            Assert.Equal(Path.Combine(machine.StartedIn, $"save{slot}.json.tmp"), SaveManager.TempOf(paths.Slot(slot)));
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => paths.Slot(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => paths.Slot(SavePaths.Slots + 1));
        Assert.False(SavePaths.IsSlot(0) || SavePaths.IsSlot(4));

        Assert.Equal(Path.Combine(machine.StartedIn, "savegame.json"), paths.OldSave);
        Assert.Equal(SaveManager.FileName, SavePaths.OldSaveName);
        Assert.Equal(Path.Combine(machine.StartedIn, "settings.json"), paths.SettingsFile);
        Assert.Equal(Path.Combine(machine.StartedIn, "log.txt"), paths.LogFile);
        Assert.Equal(Path.Combine(machine.StartedIn, "logs"), paths.CrashReports);
        Assert.Equal(Path.Combine(machine.StartedIn, "savegame.crash.json"), paths.CrashSave);
        Assert.Equal(Path.Combine(machine.StartedIn, "autosave.json"), paths.Autosave);
        Assert.Equal(Path.Combine(machine.StartedIn, "screenshots"), paths.Screenshots);
        // A folder named as it isn't written is the same folder
        Assert.Equal(machine.StartedIn, SavePaths.In(Path.Combine(machine.StartedIn, "sub", "..")).Folder);
        // Naming a folder makes nothing in it
        Assert.Equal(new[] { "started-in", "the-game" }, machine.Everything());
    }

    [Fact]
    public void UntilTheGameItselfNamesAFolderItIsTheWorkingDirectory()
    {
        // What a test or a tool that makes an engine of its own gets: only Program.Main sets it to this machine's real
        // answer, so nothing but the game itself ever writes in a user's own folder
        Assert.Equal(Path.GetFullPath(Environment.CurrentDirectory), SavePaths.Current.Folder);
        Assert.Equal(SaveHome.Portable, SavePaths.Current.Home);
        Assert.Equal(SavePaths.Current.Folder, CrashReport.SaveFolder);
        Assert.Equal(Path.Combine(Path.GetFullPath(Environment.CurrentDirectory), "settings.json"), SavePaths.Current.SettingsFile);
    }

    [Fact]
    public void WhereSaysTheFolderWhyAndWhatIsKeptInIt()
    {
        using var machine = new Machine();
        machine.Put(machine.StartedIn, SavePaths.OldSaveName);
        string told = SavePaths.Resolve(machine.On()).Describe();
        Assert.Contains(machine.StartedIn, told);
        Assert.Contains("savegame.json is there", told);
        foreach (string name in new[] { "save1.json", "save3.json", "save1.bak", "settings.json", "log.txt", "logs/", "savegame.crash.json", "portable.txt", "--save", "--portable" })
            Assert.Contains(name, told);
        Assert.NotEqual(SavePaths.Resolve(machine.On()).Why, SavePaths.Resolve(machine.On(), portable: true).Why);
    }

    // ------------------------------------------------------------------ the game's own start

    [Fact]
    public void TheCommandLineNamesTheFolder()
    {
        using var machine = new Machine();
        string elsewhere = Path.Combine(machine.Root, "elsewhere");
        Assert.True(CommandLine.Parse(new[] { "--portable" }).Portable);
        Assert.Equal(elsewhere, CommandLine.Parse(new[] { "--save", elsewhere }).SaveFolder);
        Assert.Equal("my saves", CommandLine.Parse(new[] { "--save=my saves" }).SaveFolder);
        Assert.True(CommandLine.Parse(new[] { "--where" }).Where);

        // The game's start decides with them, on whatever machine it is handed
        Assert.Equal(elsewhere, Program.Locate(CommandLine.Parse(new[] { "--save", elsewhere }), machine.On()).Folder);
        Assert.Equal(machine.StartedIn, Program.Locate(CommandLine.Parse(new[] { "--portable" }), machine.On()).Folder);
        Assert.Equal(Path.Combine(machine.AppData, "PokemonPlatinum"), Program.Locate(CommandLine.Parse(Array.Empty<string>()), machine.On()).Folder);

        // Each says where the saves are kept: together they are a mistake, and so is a folder that can be none
        var both = CommandLine.Parse(new[] { "--portable", "--save", elsewhere });
        Assert.False(both.IsValid);
        Assert.Contains(both.Errors, e => e.Contains("--portable and --save can't both be given"));
        Assert.False(CommandLine.Parse(new[] { "--save" }).IsValid);
        Assert.False(CommandLine.Parse(new[] { "--save", "a\0b" }).IsValid);

        // --help tells of all three
        string usage = CommandLine.Usage();
        foreach (string name in new[] { "--portable", "--save <folder>", "--where", SavePaths.PortableMarker }) Assert.Contains(name, usage);
    }

    [Fact]
    public void TheGamesStartMakesTheFolderAndNothingElseDoes()
    {
        using var machine = new Machine();
        var paths = Program.Locate(CommandLine.Parse(Array.Empty<string>()), machine.On());
        Assert.False(Directory.Exists(paths.Folder));

        var settled = Program.Settle(paths);
        Assert.Same(paths, settled);
        Assert.True(Directory.Exists(paths.Folder));
        Assert.Empty(Directory.EnumerateFileSystemEntries(paths.Folder));
        // A folder that is there already is left as it is
        File.WriteAllText(paths.Slot(1), "{}");
        Assert.Same(paths, Program.Settle(paths));
        Assert.Equal("{}", File.ReadAllText(paths.Slot(1)));

        // A folder that can't be made (a file stands where it should be): the game says so and keeps its files in the
        // folder it was started in, as it always did. (Only the paths are asked here: nothing is written there)
        string inTheWay = Path.Combine(machine.Root, "a-file");
        File.WriteAllText(inTheWay, "");
        var fallen = Program.Settle(SavePaths.In(Path.Combine(inTheWay, "saves")));
        Assert.Equal(Path.GetFullPath(Environment.CurrentDirectory), fallen.Folder);
        Assert.Contains("couldn't be made", fallen.Why);
    }

    // ------------------------------------------------------------------ the options go with the saves

    [Fact]
    public void TheOptionsAreKeptBesideTheSavesAndWrittenBackWhereTheyWereRead()
    {
        using var machine = new Machine();
        var paths = SavePaths.In(Path.Combine(machine.Root, "saves"));
        Assert.Equal("settings.json", GameSettings.FileName);
        Assert.Equal(Path.Combine(paths.Folder, GameSettings.FileName), paths.SettingsFile);

        // A first run finds none, and remembers where they belong
        var first = GameSettings.Load(paths.SettingsFile);
        Assert.True(first.FirstRun);
        Assert.Equal(paths.SettingsFile, first.ReadFrom);
        Assert.False(File.Exists(paths.SettingsFile));

        // Saved with no file named, they go there: the folder is made if this is the first thing written in it
        first.MusicVolume = 40;
        first.Save();
        Assert.True(File.Exists(paths.SettingsFile));
        var again = GameSettings.Load(paths.SettingsFile);
        Assert.False(again.FirstRun);
        Assert.Equal((40, paths.SettingsFile), (again.MusicVolume, again.ReadFrom));
        again.MusicVolume = 70;
        again.Save();
        Assert.Equal(70, GameSettings.Load(paths.SettingsFile).MusicVolume);

        // Where they were read from is no option: the file doesn't hold it
        Assert.DoesNotContain("ReadFrom", File.ReadAllText(paths.SettingsFile));
        Assert.DoesNotContain(machine.Root.Replace("\\", "\\\\"), File.ReadAllText(paths.SettingsFile));
        Assert.Null(new GameSettings().ReadFrom);
    }

    // ------------------------------------------------------------------ nobody but the game asks the real machine

    [Fact]
    public void NoTestAsksTheRealMachineWhereItsSavesAre()
    {
        // The engine's own rule is ArchitectureTests' (only Program asks SaveEnvironment.Here); this is the test
        // project's: nothing here may find a real user's folder, so nothing here can write, move or delete in one.
        // The probes of the architecture rules are never run
        const string probes = "PokemonPlatinumTests.Probes.";
        var asks = ArchitectureTests.Uses(typeof(SavePathsTests).Assembly.Location)
            .Where(u => u.TargetType == typeof(SaveEnvironment).FullName && u.Member == nameof(SaveEnvironment.Here) && !u.Namespace.StartsWith(probes))
            .Select(u => u.ToString()).ToList();
        Assert.True(asks.Count == 0, string.Join("\n", asks));
    }
}
