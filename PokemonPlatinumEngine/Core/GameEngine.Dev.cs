using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Raylib_cs;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The developer's surface in the game (plan 16 · T6): the console over everything on the grave key, the files
/// watched and read again as they are saved, a script's trace, the frame's time, a picture of the screen and the wild
/// Pokémon held off. The keys are read only in developer mode (<see cref="DevMode"/>); everything else is reached
/// through the developer's commands (<see cref="DevCommands"/>), which a test and the harness run on any game. What
/// it says is for the developer and stays in English.
/// </summary>
public partial class GameEngine
{
    /// <summary>The developer's console; opened with the grave key in developer mode, or by a tool.</summary>
    internal DevConsole DevConsole { get; } = new();

    /// <summary>The console's <c>encounters off</c>: no wild Pokémon is met in the field until it is on again.</summary>
    internal bool WildEncountersOff { get; set; }

    private ReloadWatch? reloadWatch;
    private bool watchTried;
    // What changed and couldn't be read again yet (a script running, a battle on screen), with the files' paths
    private ReloadKind reloadWaiting;
    private readonly SortedSet<string> reloadPaths = new(StringComparer.Ordinal);
    // The frame's time, eased, and what the console's fps shows (0 nothing, 1 the time, 2 with the profiler's passes)
    private float devFrameSeconds = 1f / 60f;
    private int frameTimes;
    private float profileClock;
    private string profileLine = "";
    private bool devShotWanted;

    /// <summary>
    /// The developer's part of a frame, before the game's: the files watched and what is waiting to be read again, and
    /// in developer mode the console's keys. True while the console is open, when the game waits under it.
    /// </summary>
    private bool UpdateDev(float dt)
    {
        devFrameSeconds += (dt - devFrameSeconds) * 0.1f;
        if (frameTimes == 2 && (profileClock += dt) >= 1f)
        {
            profileClock = 0f;
            profileLine = FrameProfiler.Report();
            FrameProfiler.Reset();
        }
        if (DevMode.On && !watchTried)
        {
            watchTried = true;
            try { reloadWatch = new ReloadWatch(GameDataFiles.Directory, ModelOverrides.Folders); }
            catch (Exception e) when (e is IOException or ArgumentException or PlatformNotSupportedException)
            {
                Log.Warn($"The files can't be watched ({e.Message}): nothing is reloaded by itself, but the console's reload still works.");
            }
        }
        if (reloadWatch?.Take() is { } batch)
        {
            reloadWaiting |= batch.Kinds;
            foreach (string path in batch.Paths) reloadPaths.Add(path);
        }
        if (reloadWaiting != ReloadKind.None) ReloadWhatCan();
        if (DevMode.On && !IsHeadless) ReadTheConsolesKeys();
        return DevConsole.IsOpen;
    }

    private void ReadTheConsolesKeys()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Grave))
        {
            DevConsole.Toggle();
            // (The grave typed to open it is no letter of the line)
            while (Raylib.GetCharPressed() != 0) { }
            return;
        }
        if (!DevConsole.IsOpen) return;
        for (int c = Raylib.GetCharPressed(); c != 0; c = Raylib.GetCharPressed()) DevConsole.Type((char)c);
        if (Raylib.IsKeyPressed(KeyboardKey.Backspace) || Raylib.IsKeyPressedRepeat(KeyboardKey.Backspace)) DevConsole.Backspace();
        if (Raylib.IsKeyPressed(KeyboardKey.Up)) DevConsole.Older();
        if (Raylib.IsKeyPressed(KeyboardKey.Down)) DevConsole.Newer();
        if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.KpEnter)) DevConsole.Submit(this);
        if (Raylib.IsKeyPressed(KeyboardKey.Escape)) DevConsole.Close();
    }

    // ------------------------------------------------------------------ live reload

    /// <summary>
    /// The console's <c>reload</c>: reads these again now, or as soon as the game can take them. What a file names
    /// that couldn't be read is said, and the rest stays as it was.
    /// </summary>
    internal DevResult DevReload(ReloadKind kinds)
    {
        reloadWaiting |= kinds;
        var done = ReloadWhatCan();
        return reloadWaiting == ReloadKind.None || done != ReloadKind.None
            ? DevResult.Done(reloadWaiting == ReloadKind.None ? $"read again: {Reload.Describe(done)}" : $"read again: {Reload.Describe(done)}; waiting: {Reload.Describe(reloadWaiting)}")
            : DevResult.Done($"waiting for the game to be free: {Reload.Describe(reloadWaiting)}");
    }

    /// <summary>Reads again whatever is waiting that the game can take now; what it read.</summary>
    private ReloadKind ReloadWhatCan()
    {
        var done = ReloadKind.None;
        // The game is free when nothing of a scene, a battle or a screen holds the maps and the runner
        bool free = !gameStarted || (flow.State is GameState.Overworld or GameState.Title && !ScriptRunning);
        if (reloadWaiting.HasFlag(ReloadKind.Music))
        {
            Told("music", MusicLibrary.Reload());
            if (AudioManager.CurrentMusic is { } playing) AudioManager.PlayMusic(playing, immediate: true);
            done |= ReloadKind.Music;
        }
        if (reloadWaiting.HasFlag(ReloadKind.Scripts) && !ScriptRunning)
        {
            var library = ScriptLibrary.Reload(out var problems, mapExists: n => MapDatabase.TryGet(n) != null, songExists: MusicLibrary.Exists);
            Told("scripts", problems, failed: library == null);
            if (library != null)
            {
                flow.Scripts = library;
                // A fresh runner: a script is found by name as it starts, so nobody holds the old ones
                var trace = flow.Runner?.Trace;
                flow.Runner = new ScriptRunner(library, new FieldHost(this)) { Trace = trace };
            }
            done |= ReloadKind.Scripts;
        }
        if ((reloadWaiting & (ReloadKind.Rooms | ReloadKind.Overlays)) != 0 && free)
        {
            var rooms = reloadWaiting.HasFlag(ReloadKind.Overlays) ? null
                : reloadPaths.Where(p => Reload.KindOf(GameDataFiles.Directory, p) == ReloadKind.Rooms && File.Exists(p)).ToList();
            SwapTheMaps(rooms);
            done |= reloadWaiting & (ReloadKind.Rooms | ReloadKind.Overlays);
        }
        if (reloadWaiting.HasFlag(ReloadKind.Models) && free)
        {
            ModelOverrides.Refresh();
            var stems = reloadPaths.Where(p => Path.GetExtension(p).ToLowerInvariant() is ".glb" or ".gltf")
                .Select(p => ModelOverrides.Key(Path.GetFileNameWithoutExtension(p))).ToHashSet();
            int released = 0;
            foreach (var species in PokemonDatabase.Everyone().Where(s => stems.Count == 0 || stems.Contains(ModelOverrides.Key(s.Name)) || stems.Contains(s.DexNumber.ToString(System.Globalization.CultureInfo.InvariantCulture))))
            {
                if (IsHeadless || !PokemonModels.Holds(species.Name)) continue;
                PokemonModels.Release(species.Name);
                PokemonSprites.Forget(species.Name);
                released++;
            }
            Told("models", new List<string>());
            Log.Info($"Reload: {released} model(s) let go, to be built again from their files.");
            done |= ReloadKind.Models;
        }
        reloadWaiting &= ~done;
        if (reloadWaiting == ReloadKind.None) reloadPaths.Clear();
        return done;
    }

    /// <summary>
    /// Every map read again (or only the rooms changed), and the game moved onto the new set: the trainers beaten kept,
    /// the player on their tile of the map of the same name, everyone where the story has them, the renderer's scenes
    /// let go so they are made again from the new maps.
    /// </summary>
    private void SwapTheMaps(IReadOnlyList<string>? rooms)
    {
        // Whoever was beaten on the maps left behind is the story's before the maps go
        foreach (string id in MapDatabase.DefeatedTrainerIds()) flow.Story.Defeat(id);
        var problems = MapDatabase.Reload(rooms);
        Told(rooms != null ? "rooms" : "maps", problems, failed: problems.Count > 0);
        if (problems.Count > 0) return;
        MapDatabase.RestoreDefeatedTrainers(flow.Story.DefeatedTrainers);
        if (gameStarted && flow.Map != null)
        {
            var player = flow.Player;
            flow.Map = MapDatabase.TryGet(flow.Map.Name) ?? flow.Map;
            if (!flow.Map.InBounds(player.GridX, player.GridY))
                player.SetPosition(Math.Clamp(player.GridX, 0, flow.Map.Width - 1), Math.Clamp(player.GridY, 0, flow.Map.Height - 1), player.Facing);
            foreach (var walk in flow.NpcWalks) walk.Finish();
            flow.NpcWalks.Clear();
            RefreshPresence(startOver: true);
            flow.Map.ApplyPresence(flow.Story.Has);
            flow.Wandering.Clear();
            Wandering.SendHome(flow.Map);
            KeepPartnerAlong();
            ArrivePuzzle();
            if (!IsHeadless) KeepCharactersNear(trim: false);
        }
        if (!IsHeadless) world.ForgetAll();
    }

    /// <summary>What a reload came to, on the toast, in the log and in the console.</summary>
    private void Told(string what, List<string> problems, bool failed = false)
    {
        string head = failed ? $"Reload of {what} failed: {problems.FirstOrDefault()}"
            : problems.Count > 0 ? $"Reloaded {what}, with {problems.Count} problem(s)" : $"Reloaded {what}";
        flow.Toast.Show(head);
        if (failed || problems.Count > 0) Log.Warn(head + (problems.Count > 1 ? "\n  " + string.Join("\n  ", problems) : ""));
        else Log.Info(head + ".");
        DevConsole.Print(head, failed ? ConsoleTone.Refused : ConsoleTone.Answer);
        foreach (string problem in problems.Skip(failed ? 1 : 0).Take(20)) DevConsole.Print("  " + problem, ConsoleTone.Refused);
    }

    // ------------------------------------------------------------------ the console's other commands

    /// <summary>The console's <c>trace</c>: every instruction a script carries out printed in the console, or no longer.</summary>
    internal DevResult DevTrace(bool on)
    {
        flow.Runner.Trace = on ? (script, i) => DevConsole.Print(TraceLine(script, i), ConsoleTone.Trace) : null;
        return DevResult.Done(on ? "tracing every script's instructions" : "no longer tracing");
    }

    /// <summary>An instruction as the trace prints it: its script, its line and what it does.</summary>
    internal static string TraceLine(Script script, Instruction i) =>
        $"{script.FullName}:{i.Line} {i.Op.ToString().ToLowerInvariant()}{(string.IsNullOrEmpty(i.Name) ? "" : " " + i.Name)}";

    /// <summary>Whether the console's trace is on.</summary>
    internal bool Tracing => flow.Runner?.Trace != null;

    /// <summary>The console's <c>fps</c>: nothing, the frame's time, or with it the profiler's passes (which slow the frame).</summary>
    internal DevResult DevFrameTimes(string mode)
    {
        frameTimes = mode switch { "off" => 0, "sections" => 2, _ => 1 };
        FrameProfiler.Enabled = frameTimes == 2;
        if (frameTimes == 2) FrameProfiler.Reset();
        profileLine = "";
        profileClock = 0f;
        return DevResult.Done(frameTimes switch
        {
            0 => "the frame's time is no longer shown",
            1 => "the frame's time is shown in the top right corner",
            _ => "the frame's time and its passes are shown; the profiler waits for the card at every pass, which slows the frame"
        });
    }

    /// <summary>The frame's time as the console's fps shows it.</summary>
    internal string FrameTimesLine() =>
        $"{1f / Math.Max(devFrameSeconds, 1e-4f):F0} fps  {devFrameSeconds * 1000f:F2} ms";

    /// <summary>The console's <c>screenshot</c>: the next frame drawn, without the console over it, is written beside the saves.</summary>
    internal DevResult DevShot()
    {
        if (IsHeadless) return DevResult.Refused("a game with no window draws nothing to take a picture of");
        devShotWanted = true;
        return DevResult.Done($"the next frame goes into {SavePaths.Current.Screenshots}");
    }

    /// <summary>The developer's part of the picture, over everything but the close button's question: the frame's time and the console.</summary>
    private void DrawDev()
    {
        if (devShotWanted) return;
        if (frameTimes > 0) ModernUi.FrameTimes(VirtualWidth, FrameTimesLine(), frameTimes == 2 ? profileLine : null);
        if (DevConsole.IsOpen) ModernUi.ConsoleSheet(VirtualWidth, VirtualHeight, DevConsole, (float)Math.Floor(world.Life.Now * 2) % 2 == 0);
    }

    /// <summary>The picture the console's screenshot asked for, once the frame is drawn.</summary>
    private void TakeDevShot()
    {
        if (!devShotWanted) return;
        devShotWanted = false;
        try
        {
            string folder = SavePaths.Current.Screenshots;
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, $"shot-{WallClock.Local.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture)}.png");
            var image = Raylib.LoadImageFromTexture(virtualScreen.Texture);
            Raylib.ImageFlipVertical(ref image);
            Raylib.ExportImage(image, path);
            Raylib.UnloadImage(image);
            DevConsole.Print($"wrote {path}");
            Log.Info($"Screenshot written: {path}");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            DevConsole.Print($"the picture couldn't be written: {e.Message}", ConsoleTone.Refused);
        }
    }
}
