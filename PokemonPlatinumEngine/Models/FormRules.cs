using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// What puts Platinum's own Pokémon in their forms outside battle (plan 06 · R10; the shapes taken in battle,
/// Castform's, Cherrim's and Arceus's, are <c>BattleCore.CheckShapes</c>). No drawing or input.
/// <list type="bullet">
/// <item>Burmy takes the cloak of the ground of every battle it was sent out in (<c>BattleSystem_SetBurmyForm</c>), and Wormadam's is its cloak's.</item>
/// <item>Shellos and Gastrodon are met in the east sea's colours east of Mt. Coronet, as each area's encounter data says, and Unown in the letters of their room of the Solaceon Ruins.</item>
/// <item>Giratina is in its Origin Forme while it holds the Griseous Orb (<c>BoxPokemon_SetGiratinaForm</c>), and Arceus is the type of the plate it holds (<c>BoxPokemon_SetArceusForm</c>).</item>
/// <item>The Gracidea sends Shaymin into its Sky Forme by day (<c>Pokemon_CanShayminSkyForm</c>); night, being frozen and being put in a box bring it back.</item>
/// </list>
/// </summary>
public static class FormRules
{
    public const string GriseousOrb = "Griseous Orb";

    // ---------------------------------------------------------------- Burmy

    /// <summary>
    /// The cloak a battle's ground gives Burmy: grass (and anything not named) the plant cloak, open ground,
    /// sand, mountains, caves and the Distortion World the sandy one, and inside a building, on a bridge or in the
    /// League's and the Frontier's rooms the trash one. Null is the plant cloak, Burmy's own.
    /// </summary>
    public static string? BurmyCloakFor(BattleTerrain ground) => ground switch
    {
        BattleTerrain.Plain or BattleTerrain.Sand or BattleTerrain.Mountain or BattleTerrain.Cave => "Burmy-Sandy",
        BattleTerrain.Building or BattleTerrain.Bridge or BattleTerrain.Special => "Burmy-Trash",
        _ => null
    };

    /// <summary>
    /// After a battle (not in the Great Marsh or Pal Park): every Burmy of the team that was sent out takes the cloak
    /// of the ground it was fought on.
    /// </summary>
    public static void CloakBurmy(IEnumerable<Pokemon> sentOut, BattleTerrain ground)
    {
        foreach (var p in sentOut)
            if (p.Species.Name == "Burmy") p.ChangeForm(BurmyCloakFor(ground));
    }

    // ---------------------------------------------------------------- met in the wild

    /// <summary>
    /// The letters met in each of the original's Unown tables (<c>WildEncounters_UnownTables</c>), numbered as the
    /// encounter data numbers them from 1: most letters in the ruins' dead ends, one letter each in the rooms of the
    /// way through (F, R, I, E, N, D as they come; the tables list N before E), and the two marks in the room past
    /// Maniac Tunnel.
    /// </summary>
    public static readonly string[][] UnownTables =
    {
        new[] { "A", "B", "C", "G", "H", "J", "K", "L", "M", "O", "P", "Q", "S", "T", "U", "V", "W", "X", "Y", "Z" },
        new[] { "F" }, new[] { "R" }, new[] { "I" }, new[] { "N" }, new[] { "E" }, new[] { "D" },
        new[] { "Exclamation", "Question" }
    };

    /// <summary>The form of Unown a letter is: null for A, its species' own.</summary>
    public static string? UnownForm(string letter) => letter == "A" ? null : "Unown-" + letter;

    /// <summary>
    /// The form a wild Pokémon is met in (<c>AddWildMonToParty</c>): Shellos and Gastrodon east of Mt. Coronet
    /// are the east sea's, and Unown is one of its table's letters, drawn at random. Null leaves it as it is.
    /// </summary>
    /// <param name="eastSea">The area's encounter data marks the east sea's colours.</param>
    /// <param name="unownTable">The area's Unown table, from 1; 0 for the first.</param>
    public static string? WildForm(PokemonSpecies species, bool eastSea, int unownTable, Random random) => species.Name switch
    {
        "Shellos" or "Gastrodon" => eastSea ? species.Name + "-East" : null,
        "Unown" => UnownForm(Pick(UnownTables[Math.Clamp(unownTable - 1, 0, UnownTables.Length - 1)], random)),
        _ => null
    };

    private static string Pick(string[] letters, Random random) => letters[random.Next(65536) % letters.Length];

    // ---------------------------------------------------------------- held items

    /// <summary>
    /// What a held item does to its holder's form, every time the item it holds changes: Giratina is in its Origin
    /// Forme with the Griseous Orb and its Altered Forme without, and Arceus with Multitype is the type of its
    /// plate. True when the form changed.
    /// </summary>
    public static bool ByHeldItem(Pokemon p)
    {
        string? form;
        switch (p.Species.Name)
        {
            case "Giratina":
                form = p.HeldItem?.Name == GriseousOrb ? "Giratina-Origin" : null;
                break;
            case "Arceus" when p.AbilityName == "Multitype":
                form = p.HeldItem?.HoldEffect is { } hold && hold.StartsWith("Arceus", StringComparison.Ordinal)
                    ? p.Species.Form("Arceus-" + hold["Arceus".Length..])?.Name : null;
                break;
            default:
                return false;
        }
        if (p.Form == form) return false;
        p.ChangeForm(form);
        return true;
    }

    // ---------------------------------------------------------------- Shaymin

    /// <summary>
    /// Whether the Gracidea can send Shaymin into its Sky Forme now: in its Land Forme, standing, not frozen, from
    /// four in the morning to eight at night. (The original also asks that it was met at an event; this game has
    /// no events, so every Shaymin may, docs/mechanics/rulings.md.)
    /// </summary>
    public static bool CanTakeToTheSky(Pokemon p, int hour) =>
        p.Species.Name == "Shaymin" && p.Form == null && p.CurrentHP > 0 && p.Status != StatusCondition.Freeze
        && hour >= 4 && hour < 20;

    /// <summary>Back to its Land Forme: at night, when frozen, when put in a box (<c>Pokemon_SetShayminForm</c>).</summary>
    public static bool BackToLand(Pokemon p)
    {
        if (p.Species.Name != "Shaymin" || p.Form == null) return false;
        p.ChangeForm(null);
        return true;
    }

    /// <summary>Whether it is night for Shaymin: from eight in the evening to four in the morning.</summary>
    public static bool ShayminNight(int hour) => hour >= 20 || hour < 4;
}
