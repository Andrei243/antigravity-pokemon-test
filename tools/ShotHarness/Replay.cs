// A player's report played back (plan 16 · T20): the recording of a report's folder (written with F8 or by a crash)
// played in the hidden window from the title, the game run every frame and drawn only for the frames it shoots.
//
//   dotnet run --project tools/ShotHarness -- <out dir> replay <folder> [every <seconds>] [from <seconds>]
//
// A shot every so many seconds of the movie's time from `from` on (rp_0010 for ten seconds in), and one at the end
// (rp_end), each with its state.json (Core/StateSnapshot.cs) beside it. It stops at the first second whose hash parts
// from the recording's and prints the frame, the second and the groups; a crash recorded in the movie is thrown again
// at its frame, and printed. The run's saves go into <out dir>/replayed, never anywhere else. This branches before the
// harness's own setup (Program.cs), because the movie's seed must be in the dice before the engine is made, and the
// harness's fixed day, clock and options are not the recording's.
static class Replay
{
    public static void Run(string[] args, string outDir, string startDir)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("usage: <out dir> replay <folder or .ppmovie file> [every <seconds>] [from <seconds>]");
            Environment.ExitCode = 2;
            return;
        }
        string folder = Path.GetFullPath(args[2], startDir);
        double every = 0, from = 0;
        for (int i = 3; i + 1 < args.Length; i += 2)
        {
            double value = double.Parse(args[i + 1], System.Globalization.CultureInfo.InvariantCulture);
            if (args[i] == "every") every = value;
            else if (args[i] == "from") from = value;
            else { Console.WriteLine($"!! '{args[i]}' is neither every nor from"); Environment.ExitCode = 2; return; }
        }

        if (Open(folder, Path.Combine(outDir, ReportFolder.ReplayedName), Console.WriteLine) is not { } opened)
        {
            Environment.ExitCode = 1;
            return;
        }
        var (engine, player) = opened;
        var game = engine.Drive;
        bool native4K = Environment.GetEnvironmentVariable("SHOTS_4K") == "1";

        void ShotNow(string name)
        {
            Shoot(engine, outDir, name, native4K);
            File.WriteAllText(Path.Combine(outDir, name + ".state.json"), StateSnapshot.Of(game.Flow).ToJson());
            Console.WriteLine($"wrote {name} ({player.Seconds:0.00} s, frame {player.Position}: {game.State} on {game.Map?.Name} at {game.Player?.GridX}, {game.Player?.GridY})");
        }

        double next = every > 0 ? (from > 0 ? from : every) : double.PositiveInfinity;
        Exception? thrown = null;
        try
        {
            while (player.Step(engine))
            {
                if (player.Seconds + 1e-9 < next) continue;
                ShotNow($"rp_{(int)Math.Round(next):0000}");
                next += every;
            }
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            thrown = e;
        }
        if (thrown != null) Console.WriteLine($"!! the replay threw at frame {player.Position - 1} ({player.Seconds:0.00} s), as the session did: {thrown.GetType().FullName}: {thrown.Message}");
        else if (player.Parted is { } parted) Console.WriteLine($"!! {parted}");
        else Console.WriteLine($"the replay came to the recording's end: {player.Position} frames, {player.Seconds:0.00} s, every second's hash as recorded");
        try { ShotNow("rp_end"); }
        catch (Exception e) when (e is not OutOfMemoryException) { Console.WriteLine($"!! no last shot: {e.GetType().Name}: {e.Message}"); }
        Movie.Release();
        engine.Close();
        if (player.Parted != null) Environment.ExitCode = 1;
    }

    /// <summary>
    /// A report's recording read, its title's files laid in <paramref name="saves"/> (emptied first), its seed in the
    /// dice and an engine made on its options and initialised in the harness's window; null, said, where it can't be.
    /// The harness's fixed clocks are let go, since the recording has its own.
    /// </summary>
    public static (GameEngine Engine, MoviePlayer Player)? Open(string folder, string saves, Action<string> say)
    {
        MoviePlayer player;
        string movie = ReportFolder.MovieIn(folder);
        try
        {
            player = new MoviePlayer(InputRecording.ReadFrom(movie));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException or EndOfStreamException or RecordingRefusedException)
        {
            say($"!! the recording {movie} can't be played: {e.Message}");
            return null;
        }
        if (Directory.Exists(saves)) Directory.Delete(saves, recursive: true);
        WallClock.Fixed = null;
        GameClock.FixedDate = null;
        FrameClock.Fixed = 0;
        InputManager.Feed(null);
        var engine = player.Prepare(saves, headless: false);
        engine.Settings.Muted = true;
        engine.WindowLocked = true;
        engine.Initialize();
        var r = player.Recording;
        say($"replaying {movie}: {r.Frames.Count} frames, {r.Seconds:0.0} s, seed {r.Seed}, made with {r.GameVersion}");
        return (engine, player);
    }

    /// <summary>The virtual screen drawn and written as a PNG, after a few frames drawn for the chunks the game hasn't drawn yet.</summary>
    public static void Shoot(GameEngine engine, string outDir, string name, bool native4K)
    {
        // The game was run without drawing: what it shows now may still be baking, so a few frames are drawn first
        var waited = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 60 && (i < 8 || waited.ElapsedMilliseconds < 400); i++) engine.Draw();
        engine.Draw();
        var img = Raylib.LoadImageFromTexture(engine.Drive.VirtualScreen.Texture);
        Raylib.ImageFlipVertical(ref img);
        if (!native4K) Raylib.ImageResize(ref img, GameEngine.VirtualWidth, GameEngine.VirtualHeight);
        string path = Path.Combine(outDir, name + ".png");
        if (Path.GetDirectoryName(path) is { } dir) Directory.CreateDirectory(dir);
        Raylib.ExportImage(img, path);
        Raylib.UnloadImage(img);
    }
}
