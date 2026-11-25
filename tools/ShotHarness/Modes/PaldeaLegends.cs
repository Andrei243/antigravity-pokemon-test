using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Core.Autoplay;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;

partial class Harness
{
    /// <summary>
    /// The Path of Legends (plan 34 · PD4), last of the `paldea` mode's places: Arven at Poco Path's lighthouse
    /// (<c>pd90</c>), the Titan Klawf in the Stony Cliff's mouth (<c>pd91</c>), at its den on the ledge (<c>pd92</c>), and
    /// the gully's crag with its climbable face (<c>pd93</c>); and the second session's four stand-in places, each Titan
    /// where it first waits (<c>pd94</c> Bombirdier in Open Sky, <c>pd95</c> Orthworm in Lurking Steel, <c>pd96</c> Great
    /// Tusk in Quaking Earth, <c>pd97</c> the Tatsugiri on False Dragon's shore, <c>pd98</c> the Dondozo on its far shore),
    /// each with the story's flags set by hand for who stands there. It prints who is on the open map near each.
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
        ShootToward(paldea, "pd94_titan_bombirdier", "open_sky_mouth", -238, -386, 15);
        TellLegends(paldea, "pd94_titan_bombirdier");
        game.Story.Set("FLAG_PALDEA_MOUNT_SWIM");
        ShootToward(paldea, "pd95_titan_orthworm", "lurking_steel_mouth", 516, -534, 14);
        TellLegends(paldea, "pd95_titan_orthworm");
        game.Story.Set("FLAG_PALDEA_QUAKING_TUSK");
        ShootToward(paldea, "pd96_titan_great_tusk", "quaking_earth_mouth", -416, -428, 15);
        TellLegends(paldea, "pd96_titan_great_tusk");
        game.Story.Set("FLAG_PALDEA_MOUNT_GLIDE");
        ShootToward(paldea, "pd97_titan_tatsugiri", "false_dragon_mouth", 505, -436, 8);
        TellLegends(paldea, "pd97_titan_tatsugiri");
        game.Story.Set("FLAG_PALDEA_TATSUGIRI_FLED");
        ShootToward(paldea, "pd98_titan_dondozo", "false_dragon_mouth", 599, -436, 40);
        TellLegends(paldea, "pd98_titan_dondozo");
    }

    void TellLegends(OpenMap paldea, string shot)
    {
        if (!Wanted(shot)) return;
        var figures = game.OpenFigures.Where(f => f.Key.StartsWith("person:titan_") || f.Key.StartsWith("person:arven")).Select(f => $"{f.Key} as {f.Look} at {f.Height:0.0} m");
        Console.WriteLine($"    figures: {string.Join(", ", figures)}");
    }

    /// <summary>
    /// The Path of Legends played (plan 34 · PD4, its second session), in a new game of its own past the academy's welcome
    /// put down at the lighthouse, by the playthrough bot on <see cref="Itineraries.PaldeaPathOfLegends"/>: every battle it meets at its
    /// menu, in order (<c>pdl01</c> on, named after the foe: the students on the ways it walks, the ten Titan battles and
    /// Arven's), all through the game's own triggers, talk and battles. It prints each battle's foe and aura and what the bot left.
    /// </summary>
    void PaldeaLegendsPlayed()
    {
        if (!Wanted("pdl")) return;
        engine.NewGameRegion = "Paldea";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        foreach (var (species, level) in new[] { ("Pawmot", 70), ("Skeledirge", 70), ("Garganacl", 70) })
            game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, level));
        game.Flow.EncounterAids.RepelSteps = 1_000_000;
        game.Story.SetVar("VAR_PALDEA_STORY", 7);
        game.Story.Unset("FLAG_PALDEA_HIDE_ARVEN_LIGHTHOUSE");
        game.Story.SetVar("VAR_PALDEA_MOUNT", 1);
        game.PlaceOpen(engine.OpenMapNamed("Paldea")!, "lighthouse_foot");
        game.LocationSign.Hide();
        Frames(20);

        var bot = new Autoplayer(engine, seed: 1) { Itinerary = new Itinerary(Itineraries.PaldeaPathOfLegends) };
        int battle = 0;
        BattleEngine? seen = null;
        for (int f = 0; f < 60 * 60 * 60 && !bot.Itinerary.Finished && !bot.Stopped; f++)
        {
            bot.Frame(1f / 60f);
            if (f % 6 == 0) Frames(1);
            if (game.State != GameState.Battle || game.Battle is not { } b || ReferenceEquals(b, seen)) continue;
            if (seen != null) Console.WriteLine($"    the battle before ended {seen.Result}");
            seen = b;
            battle++;
            string shot = $"pdl{battle:00}_" + (b.IsTrainerBattle ? "trainer_" : "") + b.EnemyPokemon.Species.Name.ToLowerInvariant().Replace(" ", "_");
            InputManager.Feed(null);
            ToMainMenu(b);
            Frames(2);
            if (Wanted(shot)) Shot(shot);
            Console.WriteLine($"  {shot}: VAR_PALDEA_LEGENDS {game.Story.Var("VAR_PALDEA_LEGENDS")}, foe {b.EnemyPokemon.Species.Name} {b.EnemyPokemon.Level}, "
                + $"aura {(b.Core.Totem is { } aura ? (aura.Titan ? "Titan " : "totem ") + string.Join(",", aura.Stats) + "+" + aura.Stages : "none")}");
        }
        for (int f = 0; f < 600 && engine.ScriptRunning; f++) bot.Frame(1f / 60f);
        if (seen != null) Console.WriteLine($"    the last battle ended {seen.Result}");
        bot.Release();
        var report = bot.Report;
        Console.WriteLine($"  the bot: legs {string.Join(", ", report.Legs)}{(report.LegsGivenUp.Count > 0 ? $"; !! given up {string.Join(", ", report.LegsGivenUp)}" : "")}, "
            + $"{report.Seconds:0.0} s; VAR_PALDEA_LEGENDS {game.Story.Var("VAR_PALDEA_LEGENDS")}, grants {MountRules.Grants(game.Story)}");
        foreach (string softlock in report.Softlocks) Console.WriteLine($"  !! {softlock}");
    }
}
