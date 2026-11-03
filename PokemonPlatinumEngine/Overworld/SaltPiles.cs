using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>How much of a pile of Shoal Salt is left on its tile: none to see under the tide, a little once today's is taken, or the whole heap.</summary>
public enum SaltLeft { None, Little, Whole }

/// <summary>
/// Shoal Cave's piles of salt (plan 21 · part r; style guide, "Hoenn's last things"): Emerald lowers a pile's metatile once
/// its salt is taken for the day. Each pile is a tile read by a script of the place named <c>ShoalSalt</c> and a number,
/// whose salt is taken while <c>FLAG_DAILY_RECEIVED_SHOAL_SALT_</c> and the same number is set; the tide
/// (<c>FLAG_SYS_SHOAL_TIDE</c>) covers every pile. The picture follows what this says. No drawing, no GPU.
/// </summary>
public static class SaltPiles
{
    public const string ScriptStem = "ShoalSalt";
    public const string TakenFlagStem = "FLAG_DAILY_RECEIVED_SHOAL_SALT_";
    public const string TideFlag = "FLAG_SYS_SHOAL_TIDE";

    /// <summary>The flag that says a pile's salt is taken for the day, from the name of the script that reads its tile; null for a script that is no pile's.</summary>
    public static string? TakenFlagOf(string script)
    {
        int dot = script.LastIndexOf('.');
        string name = dot >= 0 ? script[(dot + 1)..] : script;
        if (!name.StartsWith(ScriptStem, StringComparison.Ordinal)) return null;
        string number = name[ScriptStem.Length..];
        return number.Length > 0 && int.TryParse(number, out _) ? TakenFlagStem + number : null;
    }

    /// <summary>Every pile of a map and how much of it is left, by the story's flags.</summary>
    public static IEnumerable<(int X, int Y, SaltLeft Left)> On(Map map, Func<string, bool> flagSet)
    {
        bool tide = flagSet(TideFlag);
        foreach (var ((x, y), script) in map.TileScripts)
        {
            if (TakenFlagOf(script) is not { } taken) continue;
            yield return (x, y, tide ? SaltLeft.None : flagSet(taken) ? SaltLeft.Little : SaltLeft.Whole);
        }
    }
}
