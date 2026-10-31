using System.Reflection;

namespace PokemonPlatinumEngine.Core.Diagnostics;

/// <summary>
/// Which build this is, read from the assembly (plan 16 · T3): its version, and the commit it was built from where
/// the build knew one (the SDK writes it after a plus sign in the informational version). The log and a crash report
/// say both. Plan 16 · T2 gives the build its version from the release's tag (<c>-p:Version=</c> in
/// <c>.github/workflows/release.yml</c>, the tag less its <c>v</c>: <see cref="FromTag"/>) and <see cref="Development"/>
/// otherwise (the project file's own), and the title's corner and the options' help line show <see cref="Shown"/>.
/// No drawing, no input.
/// </summary>
public static class GameVersion
{
    /// <summary>What a build made from the source, and not from a release's tag, calls itself.</summary>
    public const string Development = "0.0.0-dev";

    /// <summary>
    /// Whether this build was published for players (plan 16 · T2): the release's publish profiles set the assembly's
    /// <c>Shipped</c> metadata. Such a build keeps the caches it writes beside the saves, since the folder it is
    /// installed in may not be writable, and reads the cache it was shipped with beside the executable.
    /// </summary>
    public static bool Shipped { get; } =
        typeof(GameVersion).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Any(a => a.Key == "Shipped" && string.Equals(a.Value, "true", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The version as the title's corner and the options show it: the version without the commit, so two builds of
    /// one version draw the same title (<c>0.1.0</c>, <c>0.0.0-dev</c>).
    /// </summary>
    public static string Shown => Version;

    /// <summary>
    /// The version a release's tag gives the build: <c>v0.1.0</c> is <c>0.1.0</c>, <c>v0.2.0-beta.1</c> is
    /// <c>0.2.0-beta.1</c>. A tag that is no version (or none) gives <see cref="Development"/>. The release workflow
    /// does the same in bash; <c>GameVersionTests</c> holds the two to the same cases.
    /// </summary>
    public static string FromTag(string? tag)
    {
        string text = (tag ?? "").Trim();
        if (text.StartsWith("refs/tags/", StringComparison.Ordinal)) text = text["refs/tags/".Length..];
        if (text.StartsWith('v') || text.StartsWith('V')) text = text[1..];
        return IsVersion(text) ? text : Development;
    }

    /// <summary>
    /// Whether a text is a version a build may carry: three whole numbers with dots between, then perhaps a hyphen
    /// and a label of letters, digits, dots and hyphens.
    /// </summary>
    public static bool IsVersion(string text)
    {
        int dash = text.IndexOf('-');
        string core = dash < 0 ? text : text[..dash];
        string[] parts = core.Split('.');
        if (parts.Length != 3 || parts.Any(p => p.Length == 0 || !p.All(char.IsAsciiDigit))) return false;
        if (dash < 0) return true;
        string label = text[(dash + 1)..];
        return label.Length > 0 && label.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-');
    }

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
