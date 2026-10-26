using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// A preset chosen by measuring, and dynamic resolution (plan 16 · T23; style guide, "Quality presets"). On the first
/// run (<see cref="GameSettings.FirstRun"/>) the engine renders the title's three places at each preset from High down,
/// behind the first start's page and the title's notice, which stays until it is done (<see cref="TitleScreen.HoldNotice"/>),
/// and starts on the highest that fits (<see cref="QualityProbe"/>). With the options' DYNAMIC RESOLUTION on, one frame
/// in fifteen waits for the card and its work is fed to <see cref="ResolutionScale"/>, whose step the scene is drawn at
/// (<see cref="RenderContext.ResolutionStep"/>). The harness's options are no first run and have the row off; its
/// <c>SHOTS_SCALE</c> holds a step of its own (<see cref="FixedResolutionStep"/>).
/// </summary>
public partial class GameEngine
{
    private QualityProbe.Measurement? measuring;
    private GraphicsQuality qualityBeforeMeasuring;
    private readonly ResolutionScale resolution = new();
    private long frameStart = Stopwatch.GetTimestamp();
    private int framesDrawn;

    /// <summary>A share of the preset the scene is held at whatever the options say (the harness's <c>SHOTS_SCALE</c>); null for none.</summary>
    public float? FixedResolutionStep { get; set; }

    /// <summary>Whether the first run's measuring is under way.</summary>
    public bool MeasuringQuality => measuring is { Done: false };

    /// <summary>Begins the measuring on a first run where the card can be waited for; a game with no window, or the harness, never does.</summary>
    private void MeasureOnTheFirstRun()
    {
        OptionsScreen.DynamicResolutionUsable = Gl.Available;
        if (measuring != null || IsHeadless || !Settings.FirstRun || !Gl.Available) return;
        measuring = new QualityProbe.Measurement();
        qualityBeforeMeasuring = Settings.Quality;
        flow.TitleScreen.HoldNotice = true;
        Log.Info("The first start: measuring which graphics preset this machine keeps.");
    }

    /// <summary>
    /// A frame of the measuring, rendered offscreen before the frame's own scene: one of the title's three places at
    /// the preset being tried, composited into the virtual screen (which the frame then draws over), timed from a card
    /// with nothing left to do to a card that has done it all, less whatever streaming cost the frame.
    /// </summary>
    private void MeasureAFrame()
    {
        if (measuring is not { Done: false } m) return;
        if (flow.State is not (GameState.FirstStart or GameState.Title) || flow.State == GameState.Title && flow.TitleScreen.Phase != TitlePhase.Notice) return;

        renderContext.SetQuality(m.Current);
        renderContext.ResolutionStep = 1f;
        var segment = TitleScreen.Segments[m.Frame % TitleScreen.Segments.Length];
        Gl.Finish();
        long start = Stopwatch.GetTimestamp();
        world.RenderCinematic(MapDatabase.Get(segment.Map), segment.From.X, segment.From.Y, segment.Hour);
        Raylib.BeginTextureMode(virtualScreen);
        Raylib.BeginMode2D(new Camera2D { Zoom = RenderScale });
        world.DrawToScreen(VirtualWidth, VirtualHeight);
        Raylib.EndMode2D();
        Raylib.EndTextureMode();
        Gl.Finish();
        double ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds - world.Streaming.LastFrameMs;
        m.Record(Math.Max(0, ms));
        if (m.Done) FinishMeasuring(m.Result!);
    }

    /// <summary>What the measuring came to, put in force unless the player chose a preset on the page meanwhile, and told to the log.</summary>
    private void FinishMeasuring(QualityChoice choice)
    {
        Log.Info($"The first start measured {string.Join(", ", choice.Medians.OrderByDescending(p => p.Key).Select(p => $"{p.Key} {p.Value:0.00} ms"))}; starting on {choice.Quality}{(choice.DynamicResolution ? " with dynamic resolution" : "")}.");
        Settings.MeasuredQuality = choice.Quality;
        Settings.MeasuredMilliseconds = choice.Medians.TryGetValue(choice.Quality, out double median) ? Math.Round(median, 1) : null;
        if (Settings.Quality == qualityBeforeMeasuring)
        {
            Settings.Quality = choice.Quality;
            // Plan 16's decision 12: on with the preset only where even Low misses the budget
            if (choice.DynamicResolution) Settings.DynamicResolution = true;
        }
        flow.TitleScreen.HoldNotice = false;
        ApplySettings(window: false);
        // On the page, CONTINUE writes the options; past it, they are written now
        if (flow.State != GameState.FirstStart) Settings.Save();
    }

    /// <summary>The share of the preset this frame's scene is drawn at: the harness's, the options' step, or the whole.</summary>
    private void ChooseTheResolution()
    {
        bool dynamic = Settings.DynamicResolution && OptionsScreen.DynamicResolutionUsable && !MeasuringQuality;
        if (!dynamic && resolution.Step != 0) resolution.Reset();
        renderContext.ResolutionStep = FixedResolutionStep ?? (dynamic ? resolution.Scale : 1f);
    }

    /// <summary>
    /// At the end of the frame's drawing, before the window is shown it: one frame in fifteen waits for the card and its
    /// work since the frame began is a sample of dynamic resolution. The others wait for nothing.
    /// </summary>
    private void SampleTheFrame()
    {
        if (FixedResolutionStep != null || !Settings.DynamicResolution || !OptionsScreen.DynamicResolutionUsable || MeasuringQuality) return;
        if (++framesDrawn % ResolutionScale.SampleEvery != 0) return;
        Gl.Finish();
        double ms = Stopwatch.GetElapsedTime(frameStart).TotalMilliseconds;
        double now = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        if (resolution.Feed(ms, now, Settings.FrameCap))
            Log.Info($"Dynamic resolution: the scene at {ResolutionScale.Steps[resolution.Step]} % ({ms:0.0} ms of work in a frame).");
    }

    /// <summary>The next frame begins as this one is shown.</summary>
    private void BeginTheNextFrame() => frameStart = Stopwatch.GetTimestamp();

    internal sealed partial class Driver
    {
        /// <summary>Dynamic resolution's state (plan 16 · T23): its step and the share it gives.</summary>
        public ResolutionScale Resolution => game.resolution;

        /// <summary>The share of the preset the scene was last drawn at.</summary>
        public float ResolutionStep => game.renderContext.ResolutionStep;
    }
}
