using System.Reflection;

namespace PokemonPlatinumEngine.Core.Diagnostics;

/// <summary>
/// Which build this is, read from the assembly (plan 16 · T3): its version, and the commit it was built from where
/// the build knew one (the SDK writes it after a plus sign in the informational version). The log and a crash report
/// say both. Plan 16 · T2 gives the build its version from the release's tag and shows it on the title; until then
/// every build is the SDK's 1.0.0. No drawing, no input.
/// </summary>
public static class GameVersion
{
    /// <summary>The assembly's informational version as the build wrote it: <c>1.2.3</c>, or <c>1.2.3+&lt;commit&gt;</c>.</summary>
    public static string Informational { get; } =
        typeof(GameVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

    /// <summary>The version alone: <c>1.2.3</c>, or <c>1.2.3-dev</c>.</summary>
    public static string Version => Parse(Informational).Version;

    /// <summary>The commit the build was made from, or null where the build knew none.</summary>
    public static string? Commit => Parse(Informational).Commit;

    /// <summary>The two as one reads them: <c>1.2.3 (commit 0123abcd4567)</c>.</summary>
    public static string Describe() => Describe(Informational);

    public static string Describe(string informational)
    {
        var (version, commit) = Parse(informational);
        return commit == null ? $"{version} (commit unknown)" : $"{version} (commit {commit})";
    }

    /// <summary>
    /// An informational version taken apart: what stands before the plus sign is the version, what stands after it
    /// the commit, cut to twelve characters (enough to find it by). Nothing after the sign, or no sign, is no commit.
    /// </summary>
    public static (string Version, string? Commit) Parse(string informational)
    {
        string text = informational.Trim();
        int plus = text.IndexOf('+');
        if (plus < 0) return (text.Length > 0 ? text : "0.0.0", null);
        string version = text[..plus].Trim(), commit = text[(plus + 1)..].Trim();
        if (commit.Length > 12) commit = commit[..12];
        return (version.Length > 0 ? version : "0.0.0", commit.Length > 0 ? commit : null);
    }
}
