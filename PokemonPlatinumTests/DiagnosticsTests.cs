using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumTests;

/// <summary>
/// What is written about the game (plan 16 · T3): the log (its lines, its file, the file put aside at its limit, a
/// line that comes again and again, the steps it times), the crash report given a made-up exception and state (every
/// section, the log's last two hundred lines, no folder with anyone's name in it), the crash save beside the real
/// save and never over it, what is kept of earlier crashes, the note the next start takes up and what the title
/// says of it. Every file is in a folder of the test's own.
/// </summary>
public class DiagnosticsTests
{
    private sealed class Folder : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("pokemon-diagnostics-").FullName;
        public string File(params string[] parts) => System.IO.Path.Combine(parts.Prepend(Path).ToArray());
        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); }
            catch (IOException) { }
        }
    }

    private static readonly DateTime Noon = new(2026, 6, 1, 12, 0, 0);

    private static LogBook Book(int tail = Log.TailLines) => new(() => Noon.AddMilliseconds(7), tail);

    // What stands under a line's message: the width of its time and its level
    private static readonly string Under = new(' ', 19);

    // ------------------------------------------------------------------ the log

    [Fact]
    public void ALineHasItsTimeAndLevelAndTheLastOnesAreKeptWithNoFileAtAll()
    {
        using var book = Book(tail: 3);
        Assert.False(book.IsOpen);
        Assert.Null(book.Path);
        book.Write(Log.InfoLevel, "one");
        book.Write(Log.WarnLevel, "two");
        book.Write(Log.ErrorLevel, "three");
        book.Write(Log.InfoLevel, "four");
        Assert.Equal(new[] { "12:00:00.007 WARN  two", "12:00:00.007 ERROR three", "12:00:00.007 INFO  four" }, book.Tail());
        // Flushing and closing what was never opened is nothing
        book.Flush();
        book.Close();
        Assert.Equal(3, book.Tail().Count);
    }

    [Fact]
    public void TheFileHoldsTheLinesAndASecondRunGoesOnInIt()
    {
        using var folder = new Folder();
        string path = folder.File("log.txt");
        using (var first = Book())
        {
            Assert.True(first.Open(path));
            Assert.True(first.IsOpen);
            Assert.Equal(path, first.Path);
            first.Raw("==== a run ====");
            first.Write(Log.InfoLevel, "started");
            first.Write(Log.WarnLevel, "a message\nof two lines");
            // Whoever looks at the file while the game runs finds what was flushed
            first.Flush();
            Assert.Contains("started", ReadShared(path));
        }
        using (var second = Book())
        {
            Assert.True(second.Open(path));
            second.Write(Log.InfoLevel, "started again");
        }
        Assert.Equal(new[]
        {
            "==== a run ====",
            "12:00:00.007 INFO  started",
            "12:00:00.007 WARN  a message",
            Under + "of two lines",
            "12:00:00.007 INFO  started again"
        }, File.ReadAllLines(path));
        Assert.False(File.Exists(LogBook.RolledName(path)));
    }

    private static string ReadShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [Fact]
    public void AFullLogIsPutAsideAndOnlyOneIsKept()
    {
        using var folder = new Folder();
        string path = folder.File("log.txt"), aside = LogBook.RolledName(path);
        Assert.Equal(folder.File("log.1.txt"), aside);
        // Each line is 35 bytes with its line end: nineteen fill the file to its limit, and the twentieth is one too many
        const int limit = 35 * 19;
        using (var book = Book())
        {
            Assert.True(book.Open(path, limit));
            for (int i = 0; i < 19; i++) book.Write(Log.InfoLevel, $"line number {i:000}");
            book.Flush();
            Assert.False(File.Exists(aside), "nothing is put aside before the limit");
            book.Write(Log.InfoLevel, "line number 019");
            book.Flush();
            Assert.True(File.Exists(aside));
            Assert.Equal(19, File.ReadAllLines(aside).Length);
            Assert.Equal("(this log goes on from log.1.txt)\n12:00:00.007 INFO  line number 019\n", ReadShared(path));

            // The log grows on, and a second full file takes the first one's place: never more than the two
            for (int i = 20; i < 70; i++) book.Write(Log.InfoLevel, $"line number {i:000}");
        }
        Assert.Equal(new[] { "log.1.txt", "log.txt" }, Directory.GetFiles(folder.Path).Select(Path.GetFileName).Order());
        Assert.True(new FileInfo(path).Length <= limit && new FileInfo(aside).Length <= limit);
        Assert.Contains("line number 069", File.ReadAllText(path));
        Assert.DoesNotContain("line number 000", File.ReadAllText(aside) + File.ReadAllText(path));
        // Nothing between the two files is lost: every line they hold follows on from the one before
        var numbers = File.ReadAllLines(aside).Concat(File.ReadAllLines(path)).Where(l => l.Contains("line number")).Select(l => int.Parse(l[^3..])).ToList();
        Assert.Equal(Enumerable.Range(numbers[0], numbers.Count), numbers);
        Assert.Equal(69, numbers[^1]);
    }

    [Fact]
    public void AFileAlreadyAtItsLimitIsPutAsideAsTheNextRunOpensIt()
    {
        using var folder = new Folder();
        string path = folder.File("log.txt");
        File.WriteAllText(path, new string('x', 500) + "\n");
        using (var book = Book())
        {
            Assert.True(book.Open(path, 400));
            book.Write(Log.InfoLevel, "a new run");
        }
        Assert.StartsWith("xxxx", File.ReadAllText(LogBook.RolledName(path)));
        Assert.Equal(new[] { "12:00:00.007 INFO  a new run" }, File.ReadAllLines(path));
        // The limit the game's own log has is two megabytes
        Assert.Equal(2 * 1024 * 1024, Log.RollAt);
        Assert.Equal("log.txt", Log.FileName);
    }

    [Fact]
    public void ALineThatComesAgainAndAgainIsWrittenOnceWithItsCount()
    {
        using var folder = new Folder();
        string path = folder.File("log.txt");
        using (var book = Book())
        {
            book.Open(path);
            book.Write(Log.InfoLevel, "before");
            for (int i = 0; i < 500; i++) book.Write(Log.WarnLevel, "AUDIO: no sound 'kazoo'");
            book.Write(Log.InfoLevel, "after");
            book.Write(Log.InfoLevel, "once more");
            book.Write(Log.InfoLevel, "once more");
            Assert.Equal(new[]
            {
                "12:00:00.007 INFO  before", "12:00:00.007 WARN  AUDIO: no sound 'kazoo'  (x500)", "12:00:00.007 INFO  after", "12:00:00.007 INFO  once more  (x2)"
            }, book.Tail());
        }
        Assert.Equal(new[]
        {
            "12:00:00.007 INFO  before",
            "12:00:00.007 WARN  AUDIO: no sound 'kazoo'",
            Under + "(the line above came 499 more times)",
            "12:00:00.007 INFO  after",
            "12:00:00.007 INFO  once more",
            Under + "(the line above came 1 more time)"
        }, File.ReadAllLines(path));
    }

    [Fact]
    public void ALineGoesToTheBufferAndTheDiskCatchesUpWhenAsked()
    {
        using var folder = new Folder();
        string path = folder.File("log.txt");
        using var book = Book();
        book.Open(path);
        // Writing a line costs no trip to the disk: nothing is there until someone asks, which the game's log does
        // from a timer's thread once a second, never from a frame
        book.Write(Log.InfoLevel, "a line");
        book.Write(Log.WarnLevel, "again");
        book.Write(Log.WarnLevel, "again");
        Assert.Equal("", ReadShared(path));
        book.FlushIfBehind();
        Assert.Equal("12:00:00.007 INFO  a line\n12:00:00.007 WARN  again\n", ReadShared(path));
        // Catching up doesn't cut a line's count short: it is written when the line stops coming
        book.Write(Log.WarnLevel, "again");
        book.FlushIfBehind();
        book.Write(Log.InfoLevel, "something else");
        book.FlushIfBehind();
        Assert.Equal($"12:00:00.007 INFO  a line\n12:00:00.007 WARN  again\n{Under}(the line above came 2 more times)\n12:00:00.007 INFO  something else\n", ReadShared(path));
        // An error is on the disk at once
        book.Write(Log.ErrorLevel, "the end");
        Assert.EndsWith("12:00:00.007 ERROR the end\n", ReadShared(path));
        Assert.Equal(1000, Log.FlushEvery);
    }

    [Fact]
    public void AStepIsTimedAndKept()
    {
        using var book = Book();
        using (book.Phase("maps")) System.Threading.Thread.Sleep(15);
        var scope = book.Phase("scripts");
        scope.Dispose();
        scope.Dispose();
        var phases = book.Phases;
        Assert.Equal(new[] { "maps", "scripts" }, phases.Select(p => p.Name));
        Assert.InRange(phases[0].Milliseconds, 10, 5000);
        Assert.InRange(phases[1].Milliseconds, 0, 1000);
        Assert.Equal(2, book.Tail().Count);
        Assert.Matches(@"^12:00:00\.007 PHASE maps: \d+ ms$", book.Tail()[0]);
    }

    [Fact]
    public void AFileThatCantBeOpenedIsNoError()
    {
        using var folder = new Folder();
        using var book = Book();
        // A folder is no file
        Assert.False(book.Open(folder.Path));
        Assert.False(book.IsOpen);
        Assert.Null(book.Path);
        book.Write(Log.ErrorLevel, "still told");
        book.Flush();
        Assert.Equal("12:00:00.007 ERROR still told", Assert.Single(book.Tail()));
        // And a log in a folder that isn't there yet makes the folder
        Assert.True(book.Open(folder.File("deep", "down", "game.log")));
        book.Write(Log.InfoLevel, "found its way");
        book.Close();
        Assert.Contains("found its way", File.ReadAllText(folder.File("deep", "down", "game.log")));
    }

    [Fact]
    public void TheGamesOwnLogKeepsItsLinesButOpensNoFileUnlessTheGameDoes()
    {
        // No test and no tool opens the game's log: only Program.Main does. Its lines are kept all the same
        Assert.False(Log.IsOpen);
        Assert.Null(Log.Path);
        string mark = $"a line of the tests' own {Guid.NewGuid():N}";
        Log.Info(mark);
        Assert.Contains(Log.Tail(), line => line.EndsWith("INFO  " + mark));
        using (Log.Phase(mark)) { }
        Assert.Contains(Log.Phases, phase => phase.Name == mark);
    }

    // ------------------------------------------------------------------ which build this is

    [Theory]
    [InlineData("1.0.0+f24d7acbbfe0e5d0c0f8a1b2c3d4e5f60718293a", "1.0.0", "f24d7acbbfe0")]
    [InlineData("0.3.1", "0.3.1", null)]
    [InlineData("0.0.0-dev+abc123", "0.0.0-dev", "abc123")]
    [InlineData("2.0.0+", "2.0.0", null)]
    [InlineData("", "0.0.0", null)]
    public void TheVersionAndTheCommitAreReadFromWhatTheBuildWrote(string informational, string version, string? commit)
    {
        Assert.Equal((version, commit), GameVersion.Parse(informational));
        Assert.Equal(commit == null ? $"{version} (commit unknown)" : $"{version} (commit {commit})", GameVersion.Describe(informational));
    }

    [Fact]
    public void ThisBuildSaysWhichItIs()
    {
        Assert.Matches(@"^\d+\.\d+\.\d+", GameVersion.Version);
        Assert.StartsWith(GameVersion.Version, GameVersion.Informational);
        Assert.Contains(GameVersion.Version, GameVersion.Describe());
    }

    // ------------------------------------------------------------------ the crash report

    private static Exception Thrown(string message, Exception? inner = null)
    {
        try { throw new InvalidOperationException(message, inner); }
        catch (Exception e) { return e; }
    }

    private static CrashFacts Facts(IReadOnlyList<string>? log = null, GameFacts? game = null) => new(
        Noon.AddMinutes(34).AddSeconds(56), "0.3.1 (commit 0123456789ab)", "Imaginary OS 12 (X64)", ".NET 10.0.99", "--region Sinnoh --windowed",
        game ?? new GameFacts("Overworld", "Sinnoh", "twinleaf_town", 116, 886, "A Made-Up Card 9000", "{\n  \"Quality\": \"High\",\n  \"FrameCap\": 60\n}"),
        log ?? new[] { "12:00:00.000 INFO  started", "12:00:01.000 WARN  AUDIO: no sound 'kazoo'" });

    [Fact]
    public void AReportHasEverySection()
    {
        string report = CrashReport.Compose(Thrown("The floor gave way.", new ArgumentException("A tile of nothing.")), Facts(), "Kept as savegame.crash.json.");

        // Every section under its heading, in the order the list has them
        int at = 0;
        foreach (string section in CrashReport.Sections)
        {
            int found = report.IndexOf($"\n== {section}", at, StringComparison.Ordinal);
            Assert.True(found >= 0, $"no section '{section}' after the one before it");
            at = found + 1;
        }
        Assert.Equal(9, CrashReport.Sections.Length);
        Assert.Equal(CrashReport.Sections.Length, report.Split('\n').Count(l => l.StartsWith("== ") && l.EndsWith(" ==")));

        // The version and the commit, the system and the runtime, the renderer, the options, the screen, the map and the tile
        foreach (string told in new[]
        {
            "2026-06-01 12:34:56", "0.3.1 (commit 0123456789ab)", "System: Imaginary OS 12 (X64)", "Runtime: .NET 10.0.99", "Renderer: A Made-Up Card 9000",
            "--region Sinnoh --windowed", "\"Quality\": \"High\"", "Screen: Overworld", "Map: Sinnoh (twinleaf_town)", "Tile: 116, 886",
            // ... the exception with what it wraps and where it was thrown, the log's lines and what became of the crash save
            "System.InvalidOperationException: The floor gave way.", "Caused by:", "System.ArgumentException: A tile of nothing.", nameof(Thrown),
            "== Log (its last 2 lines) ==", "12:00:01.000 WARN  AUDIO: no sound 'kazoo'", "Kept as savegame.crash.json."
        })
            Assert.Contains(told, report);
    }

    [Fact]
    public void AReportOfAGameThatNeverBeganSaysSo()
    {
        string title = CrashReport.Compose(new InvalidOperationException("Never thrown."), Facts(game: new GameFacts("Title", Settings: "{}")), "No game was in progress.");
        Assert.Contains("Screen: Title\nNo game in progress.", title);
        Assert.Contains("Renderer: unknown", title);
        Assert.Contains("(no stack: it was never thrown)", title);

        string early = CrashReport.Compose(new InvalidOperationException("Before anything."), Facts(log: Array.Empty<string>()) with { Game = null, Arguments = "" }, "Nothing.");
        Assert.Contains("(the game hadn't been made yet)", early);
        Assert.Contains("(no switches)", early);
        Assert.Contains("(not read yet)", early);
        Assert.Contains("(nothing was logged)", early);
        foreach (string section in CrashReport.Sections) Assert.Contains($"== {section}", early);
    }

    [Fact]
    public void AReportQuotesTheLogsLastTwoHundredLines()
    {
        using var book = Book();
        for (int i = 1; i <= 250; i++) book.Write(Log.InfoLevel, $"step {i}");
        var tail = book.Tail();
        Assert.Equal(Log.TailLines, tail.Count);
        Assert.Equal(200, Log.TailLines);

        string report = CrashReport.Compose(Thrown("Gone."), Facts(tail), "None.");
        Assert.Contains("== Log (its last 200 lines) ==", report);
        Assert.Contains("INFO  step 250\n", report);
        Assert.Contains("INFO  step 51\n", report);
        Assert.DoesNotContain("INFO  step 50\n", report);
        Assert.Equal(200, report.Split('\n').Count(l => l.Contains("INFO  step ")));
    }

    [Theory]
    [InlineData(@"at Game.Run() in C:\Users\andre\source\repos\game\Program.cs:line 12", @"at Game.Run() in C:\Users\~\source\repos\game\Program.cs:line 12")]
    [InlineData(@"C:/Users/Some One/Documents/save", @"C:/Users/~/Documents/save")]
    [InlineData(@"d:\users\builder\agent\_work\1\s\Engine.cs", @"d:\users\~\agent\_work\1\s\Engine.cs")]
    [InlineData("/home/runner/work/game/Engine.cs:line 3", "/home/~/work/game/Engine.cs:line 3")]
    [InlineData("in /Users/ada/game/Engine.cs", "in /Users/~/game/Engine.cs")]
    [InlineData(@"C:\Games\Platinum\Data\species.json", @"C:\Games\Platinum\Data\species.json")]
    [InlineData("logs/crash-20260601-120000.txt and https://example.org/home/page", "logs/crash-20260601-120000.txt and https://example.org/home/page")]
    public void NoFolderOfAnyonesNameIsLeftInAReport(string text, string scrubbed)
    {
        Assert.Equal(scrubbed, CrashReport.Scrub(text, home: null));
    }

    [Fact]
    public void ThisUsersOwnFolderBecomesATilde()
    {
        // Wherever the folder is, and whichever way its slashes lean
        Assert.Equal(@"the save at ~\saves\savegame.json", CrashReport.Scrub(@"the save at E:\People\Ada Lovelace\saves\savegame.json", @"E:\People\Ada Lovelace"));
        Assert.Equal("the save at ~/saves", CrashReport.Scrub("the save at E:/People/Ada Lovelace/saves", @"E:\People\Ada Lovelace"));
        Assert.Equal("the save at ~/saves", CrashReport.Scrub("the save at /srv/people/ada/saves", "/srv/people/ada"));
        // A report written on this machine holds neither this user's folder nor their name in a path
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string report = CrashReport.Compose(Thrown("Lost."), Facts(new[] { $"12:00:00.000 INFO  reading {Path.Combine(home, "savegame.json")}" }), "None.");
        if (home.Length > 3) Assert.DoesNotContain(home, report, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("savegame.json", report);
    }

    // ------------------------------------------------------------------ what a crash leaves beside the save

    private static SaveData Game(string name, int money) => new() { PlayerName = name, Money = money, CurrentMapName = "Sinnoh", PlayerGridX = 116, PlayerGridY = 886 };

    [Fact]
    public void ACrashLeavesAReportACrashSaveAndANoteAndNeverTouchesTheSave()
    {
        using var folder = new Folder();
        string own = folder.File(SaveManager.FileName);
        Assert.True(SaveManager.SaveGame(Game("Saved", 3000), own));
        byte[] before = File.ReadAllBytes(own);

        var outcome = CrashReport.Write(folder.Path, Thrown("The floor gave way."), Facts(), () => Game("As it stood", 4234));

        // The report, in logs/ beside the save, named for the moment
        Assert.Equal(folder.File("logs", "crash-20260601-123456.txt"), outcome.ReportPath);
        string report = File.ReadAllText(outcome.ReportPath!);
        foreach (string section in CrashReport.Sections) Assert.Contains($"== {section}", report);
        Assert.Contains("The floor gave way.", report);
        Assert.Contains("was kept as savegame.crash.json, beside your save and never over it", report);

        // The game as it stood, written as a save is and read back whole
        Assert.Equal(folder.File("savegame.crash.json"), outcome.SavePath);
        Assert.Null(outcome.SaveProblem);
        var (read, crashSave) = SaveManager.Load(outcome.SavePath!);
        Assert.Equal(SaveLoad.Ok, read);
        Assert.Equal(("As it stood", 4234, "Sinnoh", 116, 886), (crashSave!.PlayerName, crashSave.Money, crashSave.CurrentMapName, crashSave.PlayerGridX, crashSave.PlayerGridY));
        Assert.True(SaveManager.Intact(File.ReadAllText(outcome.SavePath!)));
        Assert.NotNull(SaveManager.Read(File.ReadAllText(outcome.SavePath!))!.Checksum);
        Assert.False(File.Exists(SaveManager.TempOf(outcome.SavePath!)));

        // The player's own save is byte for byte what it was, with no backup made of it, and still loads as theirs
        Assert.Equal(before, File.ReadAllBytes(own));
        Assert.False(File.Exists(SaveManager.BackupOf(own)));
        Assert.Equal((SaveLoad.Ok, "Saved"), (SaveManager.Load(own).Outcome, SaveManager.Load(own).Save!.PlayerName));

        // The note for the next start, told once
        var note = CrashReport.TakePending(folder.Path);
        Assert.Equal(new CrashNote("logs/crash-20260601-123456.txt", true, Noon.AddMinutes(34).AddSeconds(56)), note);
        Assert.Null(CrashReport.TakePending(folder.Path));
        Assert.False(File.Exists(folder.File("logs", CrashReport.PendingName)));
    }

    [Fact]
    public void WithNoGameInProgressThereIsAReportAndNoCrashSave()
    {
        using var folder = new Folder();
        foreach (var save in new Func<SaveData?>?[] { null, () => null })
        {
            var outcome = CrashReport.Write(folder.Path, Thrown("On the title."), Facts(game: new GameFacts("Title")), save);
            Assert.NotNull(outcome.ReportPath);
            Assert.Null(outcome.SavePath);
            Assert.Contains("No game was in progress", outcome.SaveProblem);
            Assert.Contains("No game was in progress", File.ReadAllText(outcome.ReportPath!));
            Assert.False(File.Exists(folder.File(CrashReport.SaveName)));
            Assert.False(CrashReport.TakePending(folder.Path)!.SaveKept);
        }
    }

    [Fact]
    public void AGameThatCantSayWhatItHoldsStillLeavesItsReport()
    {
        using var folder = new Folder();
        var outcome = CrashReport.Write(folder.Path, Thrown("In the middle of a turn."), Facts(), () => throw new InvalidOperationException("Collection was modified."));
        Assert.NotNull(outcome.ReportPath);
        Assert.Null(outcome.SavePath);
        Assert.Contains("Collection was modified.", outcome.SaveProblem);
        string report = File.ReadAllText(outcome.ReportPath!);
        Assert.Contains("In the middle of a turn.", report);
        Assert.Contains("couldn't be put into a save", report);
        Assert.False(File.Exists(folder.File(CrashReport.SaveName)));
    }

    [Fact]
    public void ASecondCrashKeepsTheFirstCrashSaveAsItsBackupAndBothReports()
    {
        using var folder = new Folder();
        var first = CrashReport.Write(folder.Path, Thrown("Once."), Facts(), () => Game("First", 1));
        // The same second (or a clock a tool has fixed): the later report takes a number
        var second = CrashReport.Write(folder.Path, Thrown("Twice."), Facts(), () => Game("Second", 2));
        var third = CrashReport.Write(folder.Path, Thrown("Thrice."), Facts(), () => Game("Third", 3));
        Assert.Equal(new[] { "crash-20260601-123456-2.txt", "crash-20260601-123456-3.txt", "crash-20260601-123456.txt" },
            Directory.GetFiles(folder.File("logs"), "crash-*.txt").Select(Path.GetFileName).Order(StringComparer.Ordinal));
        Assert.Equal(3, new[] { first.ReportPath, second.ReportPath, third.ReportPath }.Distinct().Count());

        // One crash save and the one before it, as a save has its backup; the one before that is gone
        Assert.Equal("Third", SaveManager.Load(folder.File(CrashReport.SaveName)).Save!.PlayerName);
        Assert.Equal("Second", SaveManager.Read(File.ReadAllText(SaveManager.BackupOf(folder.File(CrashReport.SaveName))))!.PlayerName);
        Assert.Equal("savegame.crash.bak", Path.GetFileName(SaveManager.BackupOf(folder.File(CrashReport.SaveName))));
        // The note is the last crash's
        Assert.Equal("logs/crash-20260601-123456-3.txt", CrashReport.TakePending(folder.Path)!.Report);
    }

    [Fact]
    public void OnlyTheNewestTenReportsAreKept()
    {
        using var folder = new Folder();
        Assert.Equal(10, CrashReport.Keep);
        var written = new List<string>();
        for (int i = 0; i < 13; i++)
        {
            var outcome = CrashReport.Write(folder.Path, Thrown($"Crash {i}."), Facts() with { When = Noon.AddHours(i) }, null);
            written.Add(outcome.ReportPath!);
            // (Each a moment after the last, as crashes are: the files' own times say which are the newest)
            File.SetLastWriteTimeUtc(outcome.ReportPath!, new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(i));
        }
        var kept = Directory.GetFiles(folder.File("logs"), "crash-*.txt").Order(StringComparer.Ordinal).ToList();
        Assert.Equal(10, kept.Count);
        Assert.Equal(written.Skip(3).Order(StringComparer.Ordinal), kept);
        Assert.Contains("Crash 12.", File.ReadAllText(written[^1]));
    }

    [Fact]
    public void ANoteThatCantBeReadIsToldAsNothingAndNotAskedAboutAgain()
    {
        using var folder = new Folder();
        Assert.Null(CrashReport.TakePending(folder.Path));
        Directory.CreateDirectory(folder.File("logs"));
        File.WriteAllText(folder.File("logs", CrashReport.PendingName), "not a note at all");
        Assert.Null(CrashReport.TakePending(folder.Path));
        Assert.False(File.Exists(folder.File("logs", CrashReport.PendingName)));
    }

    // ------------------------------------------------------------------ the title's notice

    [Fact]
    public void TheNextStartSaysTheGameStoppedAndWhatBecameOfTheSave()
    {
        Assert.Null(TitleNotice.ForCrash(null, SaveLoad.Ok));
        Assert.Null(TitleNotice.ForCrash(null, SaveLoad.Damaged));

        var kept = new CrashNote("logs/crash-20260601-123456.txt", true, Noon);
        var intact = TitleNotice.ForCrash(kept, SaveLoad.Ok)!;
        Assert.Equal("The game stopped unexpectedly", intact.Heading);
        Assert.StartsWith("Your save is intact. ", intact.Body);
        Assert.Contains("logs/crash-20260601-123456.txt", intact.Body);
        Assert.Contains(CrashReport.SaveName, intact.Body);

        // With nothing saved yet it says so, and with no copy kept it names none
        var early = TitleNotice.ForCrash(kept with { SaveKept = false }, SaveLoad.None)!;
        Assert.StartsWith("No game had been saved yet. ", early.Body);
        Assert.DoesNotContain(CrashReport.SaveName, early.Body);
        Assert.DoesNotContain("intact", early.Body);

        // A save that couldn't be read is never called intact: its own notice says what became of it
        foreach (var load in new[] { SaveLoad.Recovered, SaveLoad.Damaged })
        {
            Assert.DoesNotContain("intact", TitleNotice.ForCrash(kept, load)!.Body);
            Assert.NotNull(TitleNotice.ForSave(load));
        }
    }

    [Fact]
    public void TheCrashesNoticeComesFirstAndTheSavesAfterIt()
    {
        var note = new CrashNote("logs/crash-20260601-123456.txt", true, Noon);
        var crash = TitleNotice.ForCrash(note, SaveLoad.Recovered);
        var save = TitleNotice.ForSave(SaveLoad.Recovered);
        var title = new TitleScreen(new SaveData(), crash, save);
        title.PressConfirm();
        title.PressConfirm();
        Assert.Equal(TitlePhase.Warning, title.Phase);
        Assert.Same(crash, title.Notice);
        title.PressConfirm();
        Assert.Equal(TitlePhase.Warning, title.Phase);
        Assert.Same(save, title.Notice);
        title.PressConfirm();
        Assert.Equal(TitlePhase.Menu, title.Phase);

        // A run that ended as the player chose leaves the title as it was: no notice at all
        var quiet = new TitleScreen(new SaveData(), TitleNotice.ForCrash(null, SaveLoad.Ok), TitleNotice.ForSave(SaveLoad.Ok));
        quiet.PressConfirm();
        quiet.PressConfirm();
        Assert.Equal(TitlePhase.Menu, quiet.Phase);
    }

    [Fact]
    public void TheNoticeFitsItsPanel()
    {
        // The panel is 1040 wide with 56 at each side; its heading is 48 Black, its body 30 ExtraBold on three lines at most
        const float width = 1040 - 112;
        // The longest a report's name gets: a number after it, for the twelfth crash of one second
        var longest = new CrashNote("logs/crash-20261231-235959-12.txt", true, Noon);
        foreach (var load in Enum.GetValues<SaveLoad>())
            foreach (bool saveKept in new[] { true, false })
            {
                var notice = TitleNotice.ForCrash(longest with { SaveKept = saveKept }, load)!;
                Assert.True(TextMetrics.Measure(notice.Heading, 48, UiWeight.Black) <= width, notice.Heading);
                var rows = TextMetrics.Wrap(notice.Body, width, 30, UiWeight.ExtraBold);
                Assert.True(rows.Count <= 3, $"{rows.Count} rows: {notice.Body}");
                Assert.Empty(TextMetrics.Undrawable(notice.Heading + notice.Body, UiWeight.ExtraBold));
            }
    }

    // ------------------------------------------------------------------ the handler, as Program.Main runs the game in it

    [Fact]
    public void WhatRunsInsideTheGuardLeavesItsCrashBesideTheSaveAndSaysSoByItsExitCode()
    {
        using var folder = new Folder();
        string own = folder.File(SaveManager.FileName);
        SaveManager.SaveGame(Game("Saved", 3000), own);
        byte[] before = File.ReadAllBytes(own);

        // A run that ends as the player chose leaves nothing
        Assert.Equal(CommandLine.Ended, Program.Guarded(() => null, "--windowed", () => { }, folder.Path));
        Assert.False(Directory.Exists(folder.File("logs")));

        // One that throws in its loop leaves the report (no game was made, so no crash save) and the note, and is a crash
        int code = Program.Guarded(() => null, "--windowed", () => throw new InvalidOperationException("Thrown on purpose by a test."), folder.Path);
        Assert.Equal(CommandLine.Crashed, code);
        string report = File.ReadAllText(Assert.Single(Directory.GetFiles(folder.File("logs"), "crash-*.txt")));
        Assert.Contains("Thrown on purpose by a test.", report);
        Assert.Contains("--windowed", report);
        Assert.Contains("(the game hadn't been made yet)", report);
        Assert.Contains(GameVersion.Version, report);
        Assert.NotNull(CrashReport.Last?.ReportPath);
        Assert.False(File.Exists(folder.File(CrashReport.SaveName)));
        Assert.Equal(before, File.ReadAllBytes(own));
        Assert.NotNull(CrashReport.TakePending(folder.Path));
    }
}
