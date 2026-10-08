using System;
using System.Diagnostics;

using System.Text;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>The parts of a frame the profiler tells apart, in the order a frame goes through them.</summary>
public enum FrameSection
{
    /// <summary>The game's rules for the frame: input, movement, the battle, the screens.</summary>
    Update,
    /// <summary>Getting a scene ready to draw: streaming, who is in view, sprites that need baking.</summary>
    Prepare,
    /// <summary>The shadow map, from the sun.</summary>
    Shadows,
    /// <summary>The 3D scene into its target.</summary>
    Scene,
    /// <summary>The blurred copy of the scene that depth of field reads.</summary>
    Blur,
    /// <summary>The glow of what is bright.</summary>
    Bloom,
    /// <summary>Ambient occlusion from the depth buffer.</summary>
    Occlusion,
    /// <summary>The scene target onto the screen through the post shader.</summary>
    Composite,
    /// <summary>The 2D interface over it.</summary>
    Interface,
    /// <summary>The screen into the window.</summary>
    Present,
    /// <summary>The swap of the window's buffers, and the window's events.</summary>
    Swap
}

/// <summary>
/// Where a frame's time goes (plan 04 · G11). Switched off it costs one test at each lap. Switched on, every
/// lap first waits for the graphics card to finish what it has been given, so a section's time is the work that
/// section caused and not whatever the driver was still doing for the one before. Waiting makes the frame a
/// little slower than it is in play, so the sections are read as shares of a frame, and a frame's own time is
/// still measured with the profiler off.
/// </summary>
public static class FrameProfiler
{
    public static readonly int SectionCount = Enum.GetValues<FrameSection>().Length;

    public static bool Enabled { get; set; }

    private static readonly Stopwatch clock = new();
    private static readonly double[] total = new double[SectionCount];
    private static readonly long[] draws = new long[SectionCount];
    private static readonly long[] triangles = new long[SectionCount];
    private static readonly double[] current = new double[SectionCount];
    private static readonly double[] lastFrame = new double[SectionCount];
    private static long pendingDraws, pendingTriangles;
    private static double last;

    /// <summary>Frames measured since <see cref="Reset"/>.</summary>
    public static int Frames { get; private set; }

    /// <summary>Forgets what was measured, and starts the clock.</summary>
    public static void Reset()
    {
        Array.Clear(total);
        Array.Clear(draws);
        Array.Clear(triangles);
        Array.Clear(current);
        Array.Clear(lastFrame);
        pendingDraws = pendingTriangles = 0;
        Frames = 0;
        clock.Restart();
        last = 0;
    }

    /// <summary>
    /// Ends a section: the time since the lap before, and the meshes drawn in it, are its own.
    /// Call it outside rlgl's Begin and End.
    /// </summary>
    public static void Lap(FrameSection section)
    {
        if (!Enabled) return;
        Rlgl.DrawRenderBatchActive();
        Gl.Finish();
        Add(section, clock.Elapsed.TotalMilliseconds);
    }

    /// <summary>A lap that doesn't touch the graphics card: for the game's rules, which draw nothing.</summary>
    public static void LapCpu(FrameSection section)
    {
        if (Enabled) Add(section, clock.Elapsed.TotalMilliseconds);
    }

    private static void Add(FrameSection section, double now)
    {
        int i = (int)section;
        total[i] += now - last;
        current[i] += now - last;
        draws[i] += pendingDraws;
        triangles[i] += pendingTriangles;
        pendingDraws = pendingTriangles = 0;
        last = now;
    }

    /// <summary>The frame is over.</summary>
    public static void EndFrame()
    {
        if (!Enabled) return;
        Frames++;
        Array.Copy(current, lastFrame, SectionCount);
        Array.Clear(current);
    }

    /// <summary>A section's milliseconds in the last frame measured: for medians over frames (the harness's <c>profile.json</c>).</summary>
    public static double LastFrame(FrameSection section) => lastFrame[(int)section];

    /// <summary>A mesh was drawn.</summary>
    public static void Count(int triangleCount)
    {
        if (!Enabled) return;
        pendingDraws++;
        pendingTriangles += triangleCount;
    }

    /// <summary>A section's share of a frame in milliseconds, averaged over the frames measured.</summary>
    public static double Average(FrameSection section) => Frames > 0 ? total[(int)section] / Frames : 0;

    /// <summary>The meshes a section draws in a frame, and their triangles, averaged over the frames measured.</summary>
    public static (double Draws, double Triangles) Drawn(FrameSection section) =>
        Frames > 0 ? ((double)draws[(int)section] / Frames, (double)triangles[(int)section] / Frames) : (0, 0);

    /// <summary>The sum of the sections: the frame as the profiler ran it.</summary>
    public static double AverageFrame
    {
        get
        {
            double sum = 0;
            for (int i = 0; i < SectionCount; i++) sum += total[i];
            return Frames > 0 ? sum / Frames : 0;
        }
    }

    /// <summary>One line: each section's milliseconds, with the meshes and triangles of those that draw any.</summary>
    public static string Report()
    {
        var text = new StringBuilder();
        text.Append($"{AverageFrame:F2} ms =");
        foreach (var section in Enum.GetValues<FrameSection>())
        {
            var (d, t) = Drawn(section);
            text.Append($" {section.ToString().ToLowerInvariant()} {Average(section):F2}");
            if (d >= 0.5) text.Append($" ({d:F0} meshes, {t / 1000.0:F0}k tris)");
            text.Append(section == FrameSection.Swap ? "" : " |");
        }
        return text.ToString();
    }

    /// <summary>Whether laps can wait for the graphics card on this machine; where they can't, they measure the CPU's side only.</summary>
    public static bool WaitsForGpu => Gl.Available;
}
