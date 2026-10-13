using System.Runtime.InteropServices;
using PokemonPlatinumEngine.Core.Diagnostics;

namespace PokemonPlatinumEngine.Core;

/// <summary>The kind of machine, for where a user's own files go.</summary>
public enum SavePlatform { Windows, Linux, MacOS }

/// <summary>
/// What <see cref="SavePaths.Resolve"/> asks of the machine to decide where the saves live (plan 12 · Q2): the folder
/// the game was started in, the folder its executable is in, the kind of machine and where it keeps a user's own
/// files. Plain values, so a test hands over a made-up machine whose folders are its own temporary ones;
/// <see cref="Here"/> is this machine, and only the game's own start asks it (<c>Program</c>: a rule of
/// <c>ArchitectureTests</c>).
/// </summary>
/// <param name="WorkingDirectory">The folder the game was started in.</param>
/// <param name="BaseDirectory">The folder the executable is in.</param>
/// <param name="AppData">Windows' <c>%APPDATA%</c>.</param>
/// <param name="XdgDataHome">Linux's <c>$XDG_DATA_HOME</c>.</param>
/// <param name="Home">The user's home folder (<c>$HOME</c>).</param>
public sealed record SaveEnvironment(string WorkingDirectory, string BaseDirectory, SavePlatform Platform,
    string? AppData = null, string? XdgDataHome = null, string? Home = null)
{
    /// <summary>Whether a file is there. Nothing else of the disk is asked, and nothing is ever written to decide.</summary>
    public Func<string, bool> FileExists { get; init; } = File.Exists;

    /// <summary>This machine as it is.</summary>
    public static SaveEnvironment Here()
    {
        var platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? SavePlatform.Windows
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? SavePlatform.MacOS : SavePlatform.Linux;
        // (Windows names the folder in %APPDATA%; a process started without its environment still has the system's answer)
        string? appData = Environment.GetEnvironmentVariable("APPDATA");
        if (string.IsNullOrWhiteSpace(appData) && platform == SavePlatform.Windows) appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return new SaveEnvironment(Environment.CurrentDirectory, AppContext.BaseDirectory, platform,
            appData, Environment.GetEnvironmentVariable("XDG_DATA_HOME"), Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    }

    /// <summary>
    /// The user's own folder for the game on this kind of machine, or null where the machine doesn't say where a
    /// user's files go: <c>%APPDATA%\PokemonPlatinum</c>, <c>$XDG_DATA_HOME/pokemon-platinum</c> (or
    /// <c>~/.local/share/pokemon-platinum</c>), <c>~/Library/Application Support/PokemonPlatinum</c>.
    /// </summary>
    public string? PerUserFolder => Platform switch
    {
        SavePlatform.Windows => KnownOnWindows(AppData) ? Path.Combine(AppData!, "PokemonPlatinum") : null,
        SavePlatform.MacOS => Known(Home) ? Path.Combine(Home!, "Library", "Application Support", "PokemonPlatinum") : null,
        _ => Known(XdgDataHome) ? Path.Combine(XdgDataHome!, "pokemon-platinum")
            : Known(Home) ? Path.Combine(Home!, ".local", "share", "pokemon-platinum") : null
    };

    // (An environment variable that is set to nothing, or to a folder's bare name, names no folder)
    private static bool Known(string? folder) => !string.IsNullOrWhiteSpace(folder) && Path.IsPathRooted(folder);

    // A Windows folder is rooted by its drive or its server whatever system asks: on Linux, which the tests and the
    // cloud run on, .NET calls C:\Users a bare name
    private static bool KnownOnWindows(string? folder) => Known(folder)
        || folder is { Length: >= 3 } f && char.IsAsciiLetter(f[0]) && f[1] == ':' && f[2] is '\\' or '/'
        || folder is { Length: >= 3 } u && u.StartsWith(@"\\", StringComparison.Ordinal);
}

/// <summary>Why the saves are where they are.</summary>
public enum SaveHome
{
    /// <summary><c>--save &lt;folder&gt;</c> named the folder.</summary>
    Chosen,
    /// <summary>Beside the game: the folder it was started in (or, by a <c>portable.txt</c> there, the executable's).</summary>
    Portable,
    /// <summary>The user's own folder for the game.</summary>
    PerUser
}

/// <summary>
/// Where the game keeps what it writes (plan 12 · Q2): the three slots' saves with their backups, the options, the
/// log, the crash reports and the crash save, and later the autosave and the screenshots. One folder holds them all,
/// and this is the one place that decides which and names every file in it; whoever writes one asks here
/// (<see cref="Current"/>), never the working directory.
/// <para>
/// <see cref="Resolve"/> picks the folder, in this order, the first that holds:
/// </para>
/// <list type="number">
/// <item><c>--save &lt;folder&gt;</c>: that folder.</item>
/// <item><c>--portable</c>: the folder the game was started in.</item>
/// <item>The folder the game was started in holds a <c>portable.txt</c>, or a file the game itself wrote there (an
/// old install's <c>savegame.json</c> or its backup, a slot's save or backup, <c>settings.json</c>): that folder. An
/// old install keeps its files where they were, and so does a developer who runs the game from the repository.</item>
/// <item>A <c>portable.txt</c> beside the executable: the executable's folder.</item>
/// <item>Otherwise the user's own folder (<see cref="SaveEnvironment.PerUserFolder"/>); on a machine that names none, the folder the game was started in.</item>
/// </list>
/// <para>
/// Deciding reads whether a few files are there and nothing more: it creates no folder and writes no file, so it can
/// be asked of any machine, a made-up one too (<see cref="SaveEnvironment"/>). <see cref="Current"/> is what the
/// running game uses. <b>Only <c>Program.Main</c> sets it to this machine's real answer</b>; a tool or a test that
/// makes an engine of its own leaves it as it is, which is the working directory (the screenshot harness names its
/// output folder), so nothing but the game itself ever writes in a user's own folder. No drawing, no input.
/// </para>
/// </summary>
public sealed class SavePaths
{
    /// <summary>How many slots a game can be saved in.</summary>
    public const int Slots = 3;

    /// <summary>The marker that keeps the saves beside the game: an empty file is enough.</summary>
    public const string PortableMarker = "portable.txt";

    /// <summary>The one save an install had before there were slots, which becomes slot 1 (<see cref="SaveSlots.AdoptOldSave"/>).</summary>
    public const string OldSaveName = "savegame.json";

    /// <summary>The save Q3's autosave will write, beside the slots.</summary>
    public const string AutosaveName = "autosave.json";

    /// <summary>The folder Q16's screenshots will go in.</summary>
    public const string ScreenshotsName = "screenshots";

    /// <summary>The folder everything is in, as a full path.</summary>
    public string Folder { get; }

    public SaveHome Home { get; }

    /// <summary>Why this folder, in a sentence: what the log and <c>--where</c> say.</summary>
    public string Why { get; }

    private SavePaths(string folder, SaveHome home, string why)
    {
        Folder = Path.GetFullPath(folder);
        Home = home;
        Why = why;
    }

    /// <summary>A slot's file by its name alone: <c>save2.json</c>.</summary>
    public static string SlotName(int slot) => IsSlot(slot) ? $"save{slot}.json" : throw new ArgumentOutOfRangeException(nameof(slot), slot, $"The slots are 1 to {Slots}.");

    public static bool IsSlot(int slot) => slot is >= 1 and <= Slots;

    /// <summary>A slot's save; its backup is <see cref="SaveManager.BackupOf"/> of it (<c>save2.bak</c>).</summary>
    public string Slot(int slot) => Path.Combine(Folder, SlotName(slot));

    /// <summary>The save from before there were slots, if it is still here.</summary>
    public string OldSave => Path.Combine(Folder, OldSaveName);

    public string SettingsFile => Path.Combine(Folder, GameSettings.FileName);
    public string LogFile => Path.Combine(Folder, Log.FileName);
    public string Autosave => Path.Combine(Folder, AutosaveName);
    public string Screenshots => Path.Combine(Folder, ScreenshotsName);

    /// <summary>The game as it stood at a crash (<see cref="CrashReport.SaveName"/>), and the folder of the crash reports.</summary>
    public string CrashSave => Path.Combine(Folder, CrashReport.SaveName);
    public string CrashReports => Path.Combine(Folder, CrashReport.FolderName);

    /// <summary>A named folder, whatever is in it: the harness's output folder, a test's temporary one.</summary>
    /// <param name="why">Why this one, for the log; a folder nobody gives a reason for was simply named.</param>
    public static SavePaths In(string folder, string? why = null) => new(folder, SaveHome.Chosen, why ?? "This folder was named.");

    // ------------------------------------------------------------------ deciding

    /// <summary>The files that say the game has kept its saves in a folder before: an old install's, or a portable one's.</summary>
    public static IEnumerable<string> OwnFiles()
    {
        yield return OldSaveName;
        yield return SaveManager.BackupOf(OldSaveName);
        for (int slot = 1; slot <= Slots; slot++)
        {
            yield return SlotName(slot);
            yield return SaveManager.BackupOf(SlotName(slot));
        }
        yield return AutosaveName;
        yield return GameSettings.FileName;
    }

    /// <summary>
    /// Where the saves live on a machine, by the rules above. It reads whether the marker and the game's own files are
    /// there and touches nothing.
    /// </summary>
    /// <param name="portable"><c>--portable</c> was given.</param>
    /// <param name="chosen"><c>--save</c>'s folder, or null; a relative one is taken from the folder the game was started in.</param>
    public static SavePaths Resolve(SaveEnvironment machine, bool portable = false, string? chosen = null)
    {
        string here = Path.GetFullPath(machine.WorkingDirectory);
        if (!string.IsNullOrWhiteSpace(chosen))
            return new(Path.GetFullPath(chosen, here), SaveHome.Chosen, "--save named this folder.");
        if (portable)
            return new(here, SaveHome.Portable, "--portable keeps the saves in the folder the game was started in.");
        if (machine.FileExists(Path.Combine(here, PortableMarker)))
            return new(here, SaveHome.Portable, $"There is a {PortableMarker} in the folder the game was started in.");
        if (OwnFiles().FirstOrDefault(name => machine.FileExists(Path.Combine(here, name))) is { } found)
            return new(here, SaveHome.Portable, $"The game has kept its files in the folder it was started in before ({found} is there), and keeps them there.");
        string beside = Path.GetFullPath(machine.BaseDirectory);
        if (machine.FileExists(Path.Combine(beside, PortableMarker)))
            return new(beside, SaveHome.Portable, $"There is a {PortableMarker} beside the game.");
        if (machine.PerUserFolder is { } own)
            return new(own, SaveHome.PerUser, "This is your own folder for the game on this computer.");
        return new(here, SaveHome.Portable, "This computer names no folder for a user's own files, so the saves stay in the folder the game was started in.");
    }

    // ------------------------------------------------------------------ the running game's

    private static SavePaths? current;

    /// <summary>
    /// Where the running game keeps its files. Until something names it, it is the working directory, read anew each
    /// time: what a test or a tool that makes its own engine gets. <c>Program.Main</c> sets it once, to
    /// <see cref="Resolve"/> of this machine; the screenshot harness sets it to its output folder.
    /// </summary>
    public static SavePaths Current
    {
        get => current ?? new SavePaths(Environment.CurrentDirectory, SaveHome.Portable, "Nothing has named a folder: this is the working directory.");
        set => current = value;
    }

    /// <summary>What <c>--where</c> prints: the folder, why it is the one, and the files kept in it.</summary>
    public string Describe()
    {
        var text = new System.Text.StringBuilder();
        text.Append("The game keeps its saves in:\n  ").Append(Folder).Append('\n');
        text.Append(Why).Append("\n\n");
        text.Append("In that folder:\n");
        void Row(string name, string what) => text.Append("  ").Append(name.PadRight(28)).Append(what).Append('\n');
        Row($"{SlotName(1)} to {SlotName(Slots)}", $"the saves, one to a slot, each with the save before it beside it ({SaveManager.BackupOf(SlotName(1))})");
        Row(GameSettings.FileName, "the options");
        Row(Log.FileName, "the log of the last runs");
        Row($"{CrashReport.FolderName}/", "a report for each time the game stopped unexpectedly");
        Row(CrashReport.SaveName, "the game as it stood the last time it did");
        text.Append($"\nAn old {OldSaveName} found there becomes the first empty slot's save, slot 1 unless that is taken, the next time the game starts.\n");
        text.Append($"--save <folder> names another folder; --portable, or an empty {PortableMarker} beside the game, keeps everything beside the game.");
        return text.ToString();
    }
}
