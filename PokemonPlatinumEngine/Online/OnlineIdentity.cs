using System;
using System.IO;
using System.Linq;

namespace PokemonPlatinumEngine.Online;

/// <summary>
/// Who this install is to the online server and where that server is (plan 07 · O7, the game's half): the install
/// token, made once and kept beside <c>settings.json</c> (<c>SavePaths.InstallToken</c>), never shown and never typed;
/// and the server's address read from the options' <c>OnlineServer</c>, a <c>ws://</c> or <c>wss://</c> address only.
/// GPU-free, and the only place the game reads or writes the token.
/// </summary>
public static class OnlineIdentity
{
    /// <summary>A token's length: 32 bytes as hex.</summary>
    public const int TokenLength = 64;

    /// <summary>Whether a text is a token this game would have made.</summary>
    public static bool IsToken(string? text) =>
        text is { Length: TokenLength } && text.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');

    /// <summary>
    /// The token kept in the file, or a new one written there when the file is missing or holds something else. A
    /// token that can't be written is still used for this visit (the server then knows the player by their address
    /// at the next).
    /// </summary>
    public static string TokenIn(string file)
    {
        try
        {
            if (File.Exists(file) && File.ReadAllText(file).Trim() is var kept && IsToken(kept)) return kept;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        string token = OnlineClient.NewToken();
        try
        {
            var folder = Path.GetDirectoryName(file);
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
            File.WriteAllText(file, token);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        return token;
    }

    /// <summary>
    /// The server's address from the options, or null where none is set or it is no WebSocket address. An address
    /// with no path is given the server's own (<see cref="OnlineRules.Path"/>).
    /// </summary>
    public static Uri? ServerOf(string? setting)
    {
        if (string.IsNullOrWhiteSpace(setting)) return null;
        if (!Uri.TryCreate(setting.Trim(), UriKind.Absolute, out var uri)) return null;
        if (uri.Scheme is not ("ws" or "wss")) return null;
        if (string.IsNullOrEmpty(uri.AbsolutePath) || uri.AbsolutePath == "/") uri = new UriBuilder(uri) { Path = OnlineRules.Path }.Uri;
        return uri;
    }

    /// <summary>The address as the counter shows it: its host and port alone, never a path or a query.</summary>
    public static string Shown(Uri server) => server.IsDefaultPort ? server.Host : $"{server.Host}:{server.Port}";
}
