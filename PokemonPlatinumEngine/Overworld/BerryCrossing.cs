using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// One row of the crossing table (plan 39 · CR5; <c>Data/recipes.json</c>'s <c>crosses</c>, ours): two different berries
/// whose strongest flavour is <see cref="Flavour"/> cross into <see cref="Makes"/>; or, with <see cref="Berry"/> named,
/// that berry beside one whose strongest flavour is <see cref="Flavour"/> does.
/// </summary>
public sealed class CrossRow
{
    /// <summary>A flavour by its English name: Spicy, Dry, Sweet, Bitter or Sour.</summary>
    [DataRequired] public string Flavour { get; set; } = "";
    /// <summary>The berry one side of the pair must be; null for two berries of the flavour.</summary>
    public string? Berry { get; set; }
    [DataRequired] public string Makes { get; set; } = "";
}

/// <summary>The crossing section of <c>Data/recipes.json</c>: the chance in a hundred a harvest holds the cross, and the rows.</summary>
public sealed class CrossRecipes
{
    /// <summary>The chance in a hundred that a harvest of a noted patch holds one of its cross.</summary>
    public int InHundred { get; set; } = 40;
    public List<CrossRow> Rows { get; set; } = new();
}

/// <summary>
/// Crossing berries (plan 39 · CR5; <c>docs/mechanics/rulings.md</c>, "Materials, crafting and cooking"). When a berry is
/// planted on a soil that touches another growing a different berry, and the pair is a row of the table, both patches
/// note the cross (<see cref="BerryPatch.Cross"/>); each harvest of a noted patch rolls once on the field's chance for one
/// of its fruit to be the cross. Only with the crafting kit in the bag: without it nothing is noted and nothing drawn,
/// so a patch with no cross draws nothing and every game without the kit grows as it did. The table is ours, by flavour:
/// a berry's strongest flavour is the one of its five that is greater than the other four (a tie is none). No drawing or
/// input.
/// </summary>
public static class BerryCrossing
{
    /// <summary>The five flavours, in the order the berries' data lists them.</summary>
    public static readonly string[] Flavours = { "Spicy", "Dry", "Sweet", "Bitter", "Sour" };

    public static CrossRecipes Table => Models.RecipeTable.File.Crosses;

    /// <summary>How strong a flavour is in a berry; 0 for a berry the table has no growing for.</summary>
    public static int FlavourOf(string berry, string flavour)
    {
        if (ItemDatabase.Get(berry)?.Berry is not { } b) return 0;
        return flavour switch
        {
            "Spicy" => b.Spiciness,
            "Dry" => b.Dryness,
            "Sweet" => b.Sweetness,
            "Bitter" => b.Bitterness,
            "Sour" => b.Sourness,
            _ => 0
        };
    }

    /// <summary>A berry's strongest flavour: the one greater than the other four; null for a tie or a berry with none.</summary>
    public static string? Strongest(string berry)
    {
        var values = Flavours.Select(f => (Flavour: f, Value: FlavourOf(berry, f))).OrderByDescending(v => v.Value).ToList();
        if (values[0].Value <= 0 || values[0].Value == values[1].Value) return null;
        return values[0].Flavour;
    }

    /// <summary>What two berries planted side by side cross into, by the table's first row that holds; null for none (and for two of the same berry).</summary>
    public static string? CrossOf(string a, string b)
    {
        if (a == b) return null;
        foreach (var row in Table.Rows)
        {
            if (row.Berry == null)
            {
                if (Strongest(a) == row.Flavour && Strongest(b) == row.Flavour) return row.Makes;
            }
            else if (a == row.Berry && Strongest(b) == row.Flavour || b == row.Berry && Strongest(a) == row.Flavour)
                return row.Makes;
        }
        return null;
    }

    /// <summary>
    /// A berry has just been planted on <paramref name="planted"/>: each patch of <paramref name="beside"/> growing another
    /// berry that crosses with it is noted, and so is the planted one (with the first cross found). Returns the cross
    /// noted, or null for none.
    /// </summary>
    public static string? Note(BerryPatches patches, int planted, IEnumerable<int> beside)
    {
        var p = patches[planted];
        if (p.Berry == null) return null;
        string? noted = null;
        foreach (int other in beside)
        {
            if (other == planted || other < 0 || other >= patches.Patches.Count) continue;
            var q = patches[other];
            if (q.Berry == null || q.Stage == BerryStage.None) continue;
            if (CrossOf(p.Berry, q.Berry) is not { } cross) continue;
            q.Cross ??= cross;
            noted ??= cross;
        }
        if (noted != null) p.Cross = noted;
        return noted;
    }

    /// <summary>
    /// A harvest of a patch that held <paramref name="cross"/> and <paramref name="count"/> berries: one roll on the
    /// chance given, and with it one of the fruit is the cross. Returns how many of the cross (0 or 1). No roll for a patch
    /// with no cross or no fruit.
    /// </summary>
    public static int Roll(string? cross, int count, Random chance)
    {
        if (cross == null || count <= 0) return 0;
        return chance.Next(100) < Table.InHundred ? 1 : 0;
    }

    /// <summary>The soils on the eight tiles round a soil of a map: the patches a planting there may cross with.</summary>
    public static IEnumerable<int> Beside(Map map, NPC soil)
    {
        foreach (var npc in map.NPCs.Near(soil.GridX, soil.GridY, 1, new List<NPC>()))
            if (!ReferenceEquals(npc, soil) && npc.IsBerrySoil && npc.BerryPatch is { } id
                && Math.Abs(npc.GridX - soil.GridX) <= 1 && Math.Abs(npc.GridY - soil.GridY) <= 1)
                yield return id;
    }

    /// <summary>The pots beside a pot: the four stand in a row, each touching the one before and the one after.</summary>
    public static IEnumerable<int> PotsBeside(int pot)
    {
        if (pot > BerryPatches.PotFirst) yield return pot - 1;
        if (pot < BerryPatches.PotFirst + BerryPatches.PotCount - 1) yield return pot + 1;
    }

    /// <summary>The rows' findings against the data: a flavour that is none, a berry that isn't one, a cross named twice.</summary>
    public static IEnumerable<string> Problems(CrossRecipes? table = null)
    {
        var made = new HashSet<string>();
        if ((table ?? Table).InHundred is < 1 or > 100) yield return "crosses: inHundred is not between 1 and 100";
        foreach (var row in (table ?? Table).Rows)
        {
            if (!Flavours.Contains(row.Flavour)) yield return $"crosses: '{row.Flavour}' is no flavour";
            // (what a cross makes needs be a berry of the bag, not one that grows: Kalos's Roseli, Kee and Maranga have no plant)
            if (ItemDatabase.Get(row.Makes)?.Pocket != ItemPocket.Berries) yield return $"crosses: '{row.Makes}' is no berry";
            if (row.Berry != null && ItemDatabase.Get(row.Berry)?.Berry == null) yield return $"crosses: '{row.Berry}' is no berry that grows";
            if (!made.Add(row.Makes)) yield return $"crosses: '{row.Makes}' is made by two rows";
        }
    }
}
