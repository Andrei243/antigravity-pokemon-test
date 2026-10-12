using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace PokemonPlatinumEngine.Core.Diagnostics;

/// <summary>A step that was timed (<see cref="Log.Phase"/>): its name and how long it took.</summary>
public readonly record struct LoggedPhase(string Name, double Milliseconds);

/// <summary>
/// What is written about a run of the game (plan 16 · T3): lines with the time and a level, kept in memory (the last
/// <see cref="Log.TailLines"/>, which a crash report quotes) and, once <see cref="Open"/> has named a file, written
/// to it. Writing is cheap and off a frame's path: a line goes into a buffer and nowhere else, and the buffer
/// reaches the disk at an error, when it is asked to (<see cref="Flush"/>: the end of the start-up, a crash, the way
/// out) and, for the game's own log, about once a second from a timer's thread (<see cref="FlushIfBehind"/>), never
/// from a frame. A line that comes again and again is written once with its count. A file that has grown to
/// its limit is put aside as <c>log.1.txt</c> (the one before it is dropped) and a new one begun, so the two never
/// hold more than twice the limit. A file that can't be opened or written is no error: the lines stay in memory.
/// The game's one book is <see cref="Log"/>; a test makes its own. No drawing, no input.
/// </summary>
public sealed class LogBook : IDisposable
{
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    // The most timed steps kept: more than a run through every place of the game makes
    private const int MostPhases = 4000;

    private readonly object gate = new();
    private readonly Func<DateTime> clock;
    private readonly int tailLines;
    private readonly LinkedList<(string Line, int Times)> tail = new();
    private readonly List<LoggedPhase> phases = new();

    private StreamWriter? writer;
    private long size, rollAt;
    private string? lastBody;
    private int repeats;
    // Whether the buffer holds what the disk doesn't yet
    private bool behind;

    /// <param name="clock">Where the lines' times come from; the game's is <see cref="WallClock.Local"/>.</param>
    public LogBook(Func<DateTime> clock, int tailLines = Log.TailLines)
    {
        this.clock = clock;
        this.tailLines = tailLines;
    }

    /// <summary>The file being written, or null when none is (none was opened, or it couldn't be).</summary>
    public string? Path { get; private set; }

    public bool IsOpen
    {
        get { lock (gate) return writer != null; }
    }

    /// <summary>The file a full log is put aside as: <c>log.txt</c>'s is <c>log.1.txt</c>.</summary>
    public static string RolledName(string path) =>
        System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path) ?? "", System.IO.Path.GetFileNameWithoutExtension(path) + ".1" + System.IO.Path.GetExtension(path));

    /// <summary>
    /// Names the file the lines are written to from now on, adding to what it holds (a file already at its limit is
    /// put aside first). False, with nothing written anywhere but memory, where the file can't be opened.
    /// </summary>
    public bool Open(string path, long rollAt = Log.RollAt)
    {
        lock (gate)
        {
            CloseFile();
            this.rollAt = Math.Max(1, rollAt);
            try
            {
                string full = System.IO.Path.GetFullPath(path);
                if (System.IO.Path.GetDirectoryName(full) is { Length: > 0 } folder) Directory.CreateDirectory(folder);
                if (File.Exists(full) && new FileInfo(full).Length >= this.rollAt) File.Move(full, RolledName(full), overwrite: true);
                OpenFile(full);
                Path = full;
                return true;
            }
            catch (Exception e) when (Unwritable(e))
            {
                writer = null;
                Path = null;
                return false;
            }
        }
    }

    private void OpenFile(string full)
    {
        // Whoever looks at the file while the game runs may read it
        var stream = new FileStream(full, FileMode.Append, FileAccess.Write, FileShare.Read);
        size = stream.Length;
        writer = new StreamWriter(stream, Utf8, 4096) { NewLine = "\n" };
    }

    private static bool Unwritable(Exception e) =>
        e is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException or System.Security.SecurityException;

    /// <summary>A line with its time and level. A line the same as the one before it is only counted.</summary>
    public void Write(string level, string message)
    {
        // Lines after a message's first are set in under it, so every line of the file that begins with a time is a line of its own
        string body = $"{level,-5} {message.ReplaceLineEndings("\n").Replace("\n", "\n" + new string(' ', 19))}";
        lock (gate)
        {
            if (body == lastBody && tail.Last is { } again)
            {
                repeats++;
                again.Value = (again.Value.Line, again.Value.Times + 1);
                return;
            }
            string line = $"{clock().ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)} {body}";
            Remember(line);
            PutRepeats();
            lastBody = body;
            Put(line);
            // An error is on the disk at once: what follows it may be the end
            if (level == Log.ErrorLevel) FlushFile();
        }
    }

    /// <summary>A line as it is, with no time or level: a run's heading.</summary>
    public void Raw(string line)
    {
        lock (gate)
        {
            Remember(line);
            PutRepeats();
            lastBody = null;
            Put(line);
        }
    }

    private void Remember(string line)
    {
        tail.AddLast((line, 1));
        while (tail.Count > tailLines) tail.RemoveFirst();
    }

    private void PutRepeats()
    {
        if (repeats > 0) Put($"{new string(' ', 19)}(the line above came {repeats} more time{(repeats == 1 ? "" : "s")})");
        repeats = 0;
    }

    private void Put(string line)
    {
        if (writer == null) return;
        try
        {
            long bytes = Utf8.GetByteCount(line) + 1;
            if (size > 0 && size + bytes > rollAt) Roll();
            writer!.WriteLine(line);
            size += bytes;
            behind = true;
        }
        catch (Exception e) when (Unwritable(e) || e is ObjectDisposedException)
        {
            // A disk that is full or gone must never stop the game: the lines stay in memory
            try { writer?.Dispose(); } catch (Exception f) when (Unwritable(f) || f is ObjectDisposedException) { }
            writer = null;
        }
    }

    // The file has grown to its limit: it is put aside, over the one put aside before, and a new one begun
    private void Roll()
    {
        string full = Path!;
        writer!.Dispose();
        writer = null;
        File.Move(full, RolledName(full), overwrite: true);
        OpenFile(full);
        string note = $"(this log goes on from {System.IO.Path.GetFileName(RolledName(full))})";
        writer!.WriteLine(note);
        size += Utf8.GetByteCount(note) + 1;
    }

    private void FlushFile()
    {
        behind = false;
        if (writer == null) return;
        try { writer.Flush(); }
        catch (Exception e) when (Unwritable(e) || e is ObjectDisposedException) { writer = null; }
    }

    /// <summary>
    /// What the buffer holds and the disk doesn't goes to the disk; nothing happens when there is nothing. Called now
    /// and then from a thread of its own (the game's log, once a second), so that a game the system kills, which can
    /// flush nothing, leaves a log that is a second behind at most. A line still being counted stays as it is.
    /// </summary>
    public void FlushIfBehind()
    {
        lock (gate)
        {
            if (behind) FlushFile();
        }
    }

    /// <summary>Everything written so far reaches the disk.</summary>
    public void Flush()
    {
        lock (gate)
        {
            PutRepeats();
            // The count is written, so the same line coming again is a line again
            lastBody = null;
            FlushFile();
        }
    }

    private void CloseFile()
    {
        if (writer == null) return;
        PutRepeats();
        lastBody = null;
        try { writer.Dispose(); } catch (Exception e) when (Unwritable(e) || e is ObjectDisposedException) { }
        writer = null;
    }

    /// <summary>Closes the file; the lines kept in memory stay.</summary>
    public void Close()
    {
        lock (gate)
        {
            CloseFile();
            Path = null;
        }
    }

    public void Dispose() => Close();

    /// <summary>The last lines written, oldest first: what a crash report quotes. A line that came several times says how often.</summary>
    public IReadOnlyList<string> Tail()
    {
        lock (gate)
        {
            var lines = new List<string>(tail.Count);
            foreach (var (line, times) in tail) lines.Add(times > 1 ? $"{line}  (x{times})" : line);
            return lines;
        }
    }

    /// <summary>
    /// Times a step: from this call to the end of the <c>using</c> it is given to. The step is written as a line and
    /// kept among <see cref="Phases"/>.
    /// </summary>
    public PhaseScope Phase(string name) => new(this, name);

    /// <summary>The steps timed so far, in the order they ended.</summary>
    public IReadOnlyList<LoggedPhase> Phases
    {
        get { lock (gate) return phases.ToArray(); }
    }

    internal void EndPhase(string name, double milliseconds)
    {
        lock (gate)
        {
            phases.Add(new LoggedPhase(name, milliseconds));
            if (phases.Count > MostPhases) phases.RemoveAt(0);
        }
        Write(Log.PhaseLevel, $"{name}: {milliseconds.ToString("F0", CultureInfo.InvariantCulture)} ms");
    }

    /// <summary>A step being timed; its end is the end of the <c>using</c> that holds it.</summary>
    public sealed class PhaseScope : IDisposable
    {
        private readonly LogBook book;
        private readonly string name;
        private readonly long started = Stopwatch.GetTimestamp();
        private bool ended;

        internal PhaseScope(LogBook book, string name)
        {
            this.book = book;
            this.name = name;
        }

        public void Dispose()
        {
            if (ended) return;
            ended = true;
            book.EndPhase(name, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }
}

/// <summary>
/// The game's log (plan 16 · T3): what the engine has to say goes here, never to the console itself (which is a
/// build error anywhere else in the engine: <c>BannedSymbols.txt</c>). A warning and an error are still shown on the
/// console as they always were, and every line is kept for a crash report (<see cref="Tail"/>) whether or not a file
/// is open. Only the game itself opens the file (<c>Program.Main</c>: <c>log.txt</c> beside the save, or where
/// <c>--log</c> says): the screenshot harness and the tests open none, so their runs write nothing and differ in
/// nothing. The lines' times are <see cref="WallClock"/>'s. Nothing here draws, reads a key or takes a number from
/// the game's chance or its clocks: a frame is the same with the log as without it.
/// </summary>
public static class Log
{
    /// <summary>The log's file, beside the save (plan 12 · Q2 moves both to <c>SavePaths</c>).</summary>
    public const string FileName = "log.txt";

    /// <summary>The size at which the file is put aside and a new one begun: two megabytes.</summary>
    public const long RollAt = 2L * 1024 * 1024;

    /// <summary>The lines kept in memory for a crash report.</summary>
    public const int TailLines = 200;

    public const string InfoLevel = "INFO", WarnLevel = "WARN", ErrorLevel = "ERROR", PhaseLevel = "PHASE";

    private static readonly LogBook Book = new(() => WallClock.Local);

    public static bool IsOpen => Book.IsOpen;

    /// <summary>The file being written, or null when none is.</summary>
    public static string? Path => Book.Path;

    // While a file is open, a thread of the timer's own pushes what the buffer holds to the disk once a second; with
    // no file (the harness, the tests) there is no timer and no thread
    private static Timer? flusher;

    /// <summary>Names the file the log is written to from now on. False where it can't be opened: the game runs on without one.</summary>
    public static bool Open(string path = FileName)
    {
        bool opened = Book.Open(path);
        var old = Interlocked.Exchange(ref flusher, opened ? new Timer(_ => Book.FlushIfBehind(), null, FlushEvery, FlushEvery) : null);
        old?.Dispose();
        return opened;
    }

    /// <summary>How often the open file catches up with the lines: a second, in milliseconds.</summary>
    public const int FlushEvery = 1000;

    public static void Close()
    {
        Interlocked.Exchange(ref flusher, null)?.Dispose();
        Book.Close();
    }

    public static void Flush() => Book.Flush();

    /// <summary>A run's heading: a blank line, then the line as it is.</summary>
    public static void Heading(string line)
    {
        Book.Raw("");
        Book.Raw(line);
    }

    /// <summary>Something worth knowing afterwards: the file and a crash report have it, the console doesn't.</summary>
    public static void Info(string message) => Book.Write(InfoLevel, message);

#pragma warning disable RS0030 // the one place the engine writes to the console: a warning and an error are shown there too
    /// <summary>Something that went wrong and was got round (a sound or a song that doesn't exist, a file that couldn't be read).</summary>
    public static void Warn(string message)
    {
        Book.Write(WarnLevel, message);
        Console.WriteLine("WARNING: " + message);
    }

    /// <summary>Something that went wrong and stopped what was being done (a script, the game).</summary>
    public static void Error(string message)
    {
        Book.Write(ErrorLevel, message);
        Console.Error.WriteLine("ERROR: " + message);
    }

    /// <summary>What a switch of the command line prints and nothing keeps (<c>--help</c>, <c>--verify</c>).</summary>
    public static void Say(string text, bool error = false) => (error ? Console.Error : Console.Out).WriteLine(text);
#pragma warning restore RS0030

    /// <summary>
    /// Times a step (<c>using (Log.Phase("maps")) …</c>): the steps of the start-up and the first arrival in each
    /// place. The harness's <c>profile</c> mode prints <see cref="Phases"/>.
    /// </summary>
    public static LogBook.PhaseScope Phase(string name) => Book.Phase(name);

    /// <summary>The steps timed so far this run, in the order they ended.</summary>
    public static IReadOnlyList<LoggedPhase> Phases => Book.Phases;

    /// <summary>The last <see cref="TailLines"/> lines, oldest first.</summary>
    public static IReadOnlyList<string> Tail() => Book.Tail();
}
