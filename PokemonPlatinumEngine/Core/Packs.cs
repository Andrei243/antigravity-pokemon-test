using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The packs put in use as the game starts (plan 16 · T14): <c>Program.Main</c> calls <see cref="Start"/> once, before
/// anything loads the data. Packs are looked for beside the game and beside the saves
/// (<see cref="DataLayers.PackFolders"/>), ordered by the options (<see cref="GameSettings.Packs"/>) and their own
/// <c>after</c>, and their data laid over the game's (<see cref="DataLayers"/>); a pack's <c>overrides/models</c> is
/// looked in before the game's own, a later pack first. <c>--safe-mode</c> and <c>--packs none</c> read none. The
/// harness and the tests never call it, so they read the game's own data alone.
/// </summary>
public static class Packs
{
    public static IReadOnlyList<DataLayer> Start(GameSettings settings, string saveFolder, bool none)
    {
        if (none || !Overrides.Enabled)
        {
            DataLayers.Use(null);
            return DataLayers.Packs;
        }
        var problems = new List<string>();
        var layers = DataLayers.Discover(DataLayers.PackFolders(saveFolder), settings.Packs, problems);
        foreach (string problem in problems) Log.Warn($"A pack was left out: {problem}");
        DataLayers.Use(layers);
        foreach (var layer in layers.AsEnumerable().Reverse())
        {
            string models = Path.Combine(layer.Root, "overrides", "models");
            if (Directory.Exists(models)) ModelOverrides.Folders.Insert(0, models);
        }
        if (layers.Count > 0)
            Log.Info($"Packs in use, in load order: {string.Join(", ", layers.Select(l => $"{l.Pack!.Name} {l.Pack.Version} ({l.Pack.Credits})"))}.");
        return layers;
    }
}
