using System;
using System.Linq;
using PokemonPlatinumEngine.OpenWorld;

partial class Harness
{
    /// <summary>
    /// The Path of Legends (plan 34 · PD4, its first session), last of the `paldea` mode's places: Arven at Poco Path's
    /// lighthouse (<c>pd90</c>), the Titan Klawf in the Stony Cliff's mouth (<c>pd91</c>), at its den on the ledge
    /// (<c>pd92</c>), and the gully's crag with its climbable face (<c>pd93</c>), each with the story's flags set by hand
    /// for who stands there. It prints who is on the open map near each.
    /// </summary>
    void PaldeaLegends(OpenMap paldea)
    {
        game.Story.Unset("FLAG_PALDEA_HIDE_ARVEN_LIGHTHOUSE");
        ShootToward(paldea, "pd90_arven_lighthouse", "lighthouse_foot", 60, -80, 6);
        game.Story.Set("FLAG_PALDEA_LEGENDS_BEGUN");
        ShootToward(paldea, "pd91_titan_klawf", "stony_cliff_mouth", 226, -386, 15);
        TellLegends(paldea, "pd91_titan_klawf");
        game.Story.Set("FLAG_PALDEA_KLAWF_FLED");
        ShootToward(paldea, "pd92_klawf_den", "stony_cliff_herbs", 226, -442, 14);
        TellLegends(paldea, "pd92_klawf_den");
        game.Story.Set("FLAG_PALDEA_KLAWF_BEATEN");
        game.Story.Set("FLAG_PALDEA_MOUNT_DASH");
        ShootToward(paldea, "pd93_stony_cliff_crag", "stony_cliff_mouth", 243, -402, 22);
    }

    void TellLegends(OpenMap paldea, string shot)
    {
        if (!Wanted(shot)) return;
        var figures = game.OpenFigures.Where(f => f.Key.StartsWith("person:titan_") || f.Key.StartsWith("person:arven")).Select(f => $"{f.Key} as {f.Look} at {f.Height:0.0} m");
        Console.WriteLine($"    figures: {string.Join(", ", figures)}");
    }
}
