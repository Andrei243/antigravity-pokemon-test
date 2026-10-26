using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 16 · T15, packs in the game: the save's record of the packs it was played with, the title's word of a pack
/// gone, the packs screen's cursor, order and on and off, <c>--validate-packs</c>, and the sample pack of
/// <c>samples/packs</c> passing its own check. The fixture packs of T14 are handed over by hand and nothing is put in
/// use for the whole process.
/// </summary>
public class PackScreenTests
{
    private static readonly string FixturePacks = Path.Combine(AppContext.BaseDirectory, "Fixtures", "packs");
    private static readonly string SamplePacks = Path.Combine(Repository.Root, "samples", "packs");

    private static List<DataLayer> Found() => DataLayers.Found(new[] { FixturePacks }, new List<string>());

    [Fact]
    public void ASaveKeepsThePacksItWasPlayedWithAndOneWithoutKeepsNone()
    {
        var layers = DataLayers.Discover(new[] { FixturePacks }, null, new List<string>());
        var record = PackRecords.Of(layers);
        Assert.NotNull(record);
        Assert.Equal(new[] { "alpha", "beta" }, record!.Select(p => p.Name));
        Assert.Equal("2.1", record[1].Version);
        Assert.Null(PackRecords.Of(Array.Empty<DataLayer>()));
        Assert.Null(PackRecords.InUse());

        var save = new SaveData { PlayerName = "Lucas", Packs = record };
        var back = SaveManager.Read(SaveManager.Write(save))!;
        Assert.Equal(new[] { "alpha 1.0", "beta 2.1" }, back.Packs!.Select(p => $"{p.Name} {p.Version}"));
        // A save made with no pack keeps nothing, so it reads as every older save does
        Assert.Null(SaveManager.Read(SaveManager.Write(new SaveData { PlayerName = "Lucas" }))!.Packs);
    }

    [Fact]
    public void TheTitleNamesAPackNoLongerInUseOrAtAnotherVersion()
    {
        var saved = new List<SavedPack> { new() { Name = "alpha", Version = "1.0" }, new() { Name = "beta", Version = "2.0" }, new() { Name = "gone", Version = "3" } };
        var inUse = DataLayers.Discover(new[] { FixturePacks }, null, new List<string>());
        var missing = PackRecords.Missing(saved, inUse);
        Assert.Equal(new[] { "beta 2.0 -> 2.1", "gone 3 -> " }, missing.Select(m => $"{m.Name} {m.Version} -> {m.NowVersion}"));
        Assert.Empty(PackRecords.Missing(null, inUse));
        Assert.Empty(PackRecords.Missing(saved.Take(1).ToList(), inUse));

        var notice = TitleNotice.ForPacks(2, missing);
        Assert.NotNull(notice);
        Assert.Contains("2", notice!.Heading);
        Assert.Contains("gone 3", notice.Body);
        Assert.Contains("2.1", notice.Body);
        Assert.Null(TitleNotice.ForPacks(2, Array.Empty<MissingPack>()));
    }

    [Fact]
    public void TheCardSaysWhichPacksAGameWasPlayedWith()
    {
        Assert.Null(ModernUi.PacksMark(new SaveData()));
        Assert.Contains("alpha", ModernUi.PacksMark(new SaveData { Packs = new() { new() { Name = "alpha", Version = "1.0" } } }));
        Assert.Contains("2", ModernUi.PacksMark(new SaveData { Packs = new() { new() { Name = "alpha", Version = "1.0" }, new() { Name = "beta", Version = "2.1" } } }));
    }

    [Fact]
    public void TheScreenListsEveryPackOnOrOffInTheOptionsOrder()
    {
        var found = Found();
        Assert.Equal(new[] { "alpha", "beta" }, found.Select(l => l.Pack!.Name));

        var screen = new PacksScreen();
        screen.Open(found, new[] { new PackChoice { Name = "beta" }, new PackChoice { Name = "alpha", Enabled = false } }, Array.Empty<DataLayer>());
        Assert.True(screen.IsActive);
        Assert.Equal(new[] { "beta", "alpha" }, screen.Rows.Select(r => r.Pack.Name));
        Assert.Equal(new[] { true, false }, screen.Rows.Select(r => r.Enabled));
        Assert.Equal("1 / 2", screen.Place);
        // Only beta would load, and nothing is loaded now: the change waits for the next start
        Assert.Equal(new[] { "beta" }, screen.WillLoad);
        Assert.True(screen.Pending);

        // The cursor wraps round
        screen.Move(-1);
        Assert.Equal(1, screen.Selected);
        screen.Move(1);
        Assert.Equal(0, screen.Selected);
    }

    [Fact]
    public void APackIsTurnedOnAndOffAndMovedInTheOrder()
    {
        var screen = new PacksScreen();
        screen.Open(Found(), null, Array.Empty<DataLayer>());
        Assert.Equal(new[] { "alpha", "beta" }, screen.Rows.Select(r => r.Pack.Name));

        // Moving beta first: the options keep that order, but beta still loads after alpha, as its manifest asks
        screen.Move(1);
        Assert.True(screen.Shift(-1));
        Assert.Equal(new[] { "beta", "alpha" }, screen.Rows.Select(r => r.Pack.Name));
        Assert.Equal(0, screen.Selected);
        Assert.False(screen.Shift(-1));
        Assert.Equal(new[] { "alpha", "beta" }, screen.WillLoad);

        // alpha off: only beta loads
        screen.Move(1);
        Assert.True(screen.Toggle());
        Assert.Equal(new[] { "beta" }, screen.WillLoad);

        var settings = new GameSettings();
        screen.WriteTo(settings);
        Assert.Equal(new[] { "beta:True", "alpha:False" }, settings.Packs.Select(c => $"{c.Name}:{c.Enabled}"));
        // What the options keep is what the next start loads
        Assert.Equal(new[] { "beta" }, DataLayers.Discover(new[] { FixturePacks }, settings.Packs, new List<string>()).Select(l => l.Pack!.Name));

        // Nothing pending when what would load is what is loaded
        var reopened = new PacksScreen();
        reopened.Open(Found(), settings.Packs, DataLayers.Discover(new[] { FixturePacks }, settings.Packs, new List<string>()));
        Assert.False(reopened.Pending);
    }

    [Fact]
    public void WithNoPackTheScreenSaysWhereToPutOne()
    {
        var screen = new PacksScreen();
        string empty = Path.Combine(Path.GetTempPath(), "pp-no-packs-" + Guid.NewGuid().ToString("N"));
        screen.Open(DataLayers.Found(new[] { empty }, new List<string>()), null, Array.Empty<DataLayer>(), folders: new[] { empty });
        Assert.Empty(screen.Rows);
        Assert.Null(screen.Current);
        Assert.False(screen.Toggle());
        Assert.False(screen.Shift(1));
        Assert.False(screen.Pending);
        Assert.Equal(new[] { empty }, screen.Folders);
    }

    [Fact]
    public void ThePacksRowOpensItsScreenOverTheOptions()
    {
        var rows = Enum.GetValues<OptionRow>();
        // The row stands before HELP, after the jukebox
        Assert.Equal(OptionRow.Packs, rows[Array.IndexOf(rows, OptionRow.Help) - 1]);
        var (label, _, help) = OptionsScreen.Describe(new GameSettings(), OptionRow.Packs);
        Assert.False(string.IsNullOrWhiteSpace(label));
        Assert.False(string.IsNullOrWhiteSpace(help));
    }

    [Fact]
    public void TheSamplePackPassesItsOwnCheck()
    {
        string sample = Path.Combine(SamplePacks, "gate-lanterns");
        Assert.True(Directory.Exists(sample), sample);
        Assert.Empty(PackCheck.Check(sample));

        // It carries what the plan asks of it: an overlay's new lines, a trainer, a script and a short song
        Assert.True(File.Exists(Path.Combine(sample, "world", "sinnoh", "overlays", "oreburgh_gate_b1f.json")));
        Assert.True(File.Exists(Path.Combine(sample, "trainers.patch.json")));
        Assert.True(File.Exists(Path.Combine(sample, "scripts", "oreburgh_gate_b1f.txt")));
        Assert.True(File.Exists(Path.Combine(sample, "music", "gatelanterns", "lantern_walk.mml")));

        // Read over the game's data, the trainer's team is the pack's
        var layer = new DataLayer(sample, DataLayers.ReadManifest(sample));
        string trainers = DataLayers.ReadPatched("trainers.json", new[] { layer });
        Assert.Contains("Chimecho", trainers);
        Assert.Equal(0, PackCheck.CheckAll(new[] { SamplePacks }, _ => { }));
    }

    [Fact]
    public void TheCheckNamesWhatIsWrongWithAPack()
    {
        string root = Path.Combine(Path.GetTempPath(), "pp-bad-pack-" + Guid.NewGuid().ToString("N"));
        string pack = Path.Combine(root, "bad");
        try
        {
            Directory.CreateDirectory(Path.Combine(pack, "scripts"));
            Directory.CreateDirectory(Path.Combine(pack, "world", "sinnoh"));
            File.WriteAllText(Path.Combine(pack, "pack.json"), "{ \"name\": \"bad\", \"version\": \"1\", \"credits\": \"Ours.\" }");
            File.WriteAllText(Path.Combine(pack, "maps.patch.json"), "[]");
            File.WriteAllText(Path.Combine(pack, "moves.json"), "{ not json");
            File.WriteAllText(Path.Combine(pack, "world", "sinnoh", "world.json"), "{}");
            File.WriteAllText(Path.Combine(pack, "scripts", "bad_place.txt"), "script Talk\n  say \"Hello.\"\n  give \"No Such Item\" 1\n  end\n");
            var findings = PackCheck.Check(pack);
            Assert.Contains(findings, f => f.StartsWith("maps.patch.json"));
            Assert.Contains(findings, f => f.StartsWith("moves.json") && f.Contains("JSON"));
            Assert.Contains(findings, f => f.StartsWith("world/sinnoh/world.json"));
            Assert.Contains(findings, f => f.StartsWith("scripts/bad_place.txt"));
            var said = new List<string>();
            Assert.Equal(PackCheck.Findings, PackCheck.CheckAll(new[] { root }, said.Add));
            Assert.Contains(said, l => l.Contains("finding"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void ValidatePacksIsASwitch()
    {
        var launch = CommandLine.Parse(new[] { "--validate-packs" });
        Assert.True(launch.IsValid);
        Assert.True(launch.ValidatePacks);
        Assert.Contains("--validate-packs", CommandLine.Usage());
    }
}
