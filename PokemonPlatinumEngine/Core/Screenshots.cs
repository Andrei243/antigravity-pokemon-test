using System;
using System.Globalization;
using System.IO;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Where the screenshot key's pictures go and what they are called (plan 12 · Q16): <c>screenshots/</c> beside the saves
/// (<see cref="SavePaths.Screenshots"/>), each named by the wall clock to the millisecond with a count after it, so
/// that the names sort as the pictures were taken, two in one millisecond included. GPU-free: the picture itself is
/// the engine's (<c>GameEngine.CaptureScreen</c>).
/// </summary>
public static class Screenshots
{
    /// <summary>The most pictures one millisecond names before the last is written over (never, in practice).</summary>
    public const int PerMillisecond = 100;

    /// <summary>The <paramref name="n"/>th picture's name of a moment: <c>shot-20261026-120000-123-00.png</c>.</summary>
    public static string NameFor(DateTime time, int n) =>
        string.Format(CultureInfo.InvariantCulture, NamePattern, time, n);

    // The moment to the millisecond, then the count of that moment
    private const string NamePattern = "shot-{0:yyyyMMdd-HHmmss-fff}-{1:00}.png";

    /// <summary>The path a picture taken at <paramref name="time"/> is written to: the first name of that moment not yet taken.</summary>
    public static string PathFor(string folder, DateTime time, Func<string, bool> exists)
    {
        for (int n = 0; n < PerMillisecond - 1; n++)
        {
            string path = Path.Combine(folder, NameFor(time, n));
            if (!exists(path)) return path;
        }
        return Path.Combine(folder, NameFor(time, PerMillisecond - 1));
    }
}
