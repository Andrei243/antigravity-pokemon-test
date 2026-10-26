using System;
using System.IO;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The developer's surface (plan 16 · T6; plan 16's "One build, two faces"): on under <c>--dev</c> or with
/// <c>"developer": true</c> in <c>settings.json</c>. While it is on the grave key opens the console (<c>UI/DevConsole</c>)
/// and the game watches its own files and reloads what changes (<see cref="ReloadWatch"/>), reading them from the
/// repository's <c>PokemonPlatinumEngine/Data</c> when it can find it above the executable, so the file saved is the
/// file read. Only <c>Program.Main</c> turns it on; a test or a tool that makes an engine of its own leaves it off and
/// reaches the console's commands through <see cref="DevCommands"/>. No drawing, no input.
/// </summary>
public static class DevMode
{
    /// <summary>Whether the developer's surface is on in this run.</summary>
    public static bool On { get; private set; }

    /// <summary>The data folder this run reads, when developer mode moved it to the repository's; null otherwise.</summary>
    public static string? RepositoryData { get; private set; }

    /// <summary>Whether a run started with these switches and options is a developer's.</summary>
    public static bool Wanted(LaunchOptions launch, GameSettings settings) => launch.Dev || settings.Developer;

    /// <summary>
    /// Turns developer mode on for the run: the data are read from the repository's folder when one is found above
    /// <paramref name="baseDirectory"/> (the executable's), or from the copy beside the executable as ever. Called
    /// before anything that reads <see cref="GameDataFiles"/> has loaded but the regions, which <c>--help</c> names.
    /// </summary>
    public static void Enable(string? baseDirectory = null)
    {
        On = true;
        RepositoryData = FindRepositoryData(baseDirectory ?? AppContext.BaseDirectory);
        if (RepositoryData != null) GameDataFiles.Directory = RepositoryData;
        Log.Info(RepositoryData != null
            ? $"Developer mode: the data are read from {RepositoryData}, and what changes there is reloaded."
            : "Developer mode: no repository was found above the game, so the data beside it are read and watched.");
    }

    /// <summary>
    /// The repository's data folder above a folder (the executable's <c>bin/Debug/net10.0</c>): the first folder up
    /// the tree that holds <c>PokemonPlatinumEngine/Data/species.json</c>, or null when there is none.
    /// </summary>
    public static string? FindRepositoryData(string start)
    {
        for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
        {
            string data = Path.Combine(dir.FullName, "PokemonPlatinumEngine", "Data");
            if (File.Exists(Path.Combine(data, "species.json"))) return data;
        }
        return null;
    }
}
