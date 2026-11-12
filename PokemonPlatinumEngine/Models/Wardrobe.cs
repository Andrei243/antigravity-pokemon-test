using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The clothes the player owns and what they wear (GPU-free). A garment bought once is worn as often as wished at no
/// cost; the free ones and the look's own clothes are always there. Saved as <c>SaveData.Outfit</c> and
/// <c>SaveData.Wardrobe</c>; a save without them is the look's own clothes and nothing bought.
/// </summary>
public sealed class Wardrobe
{
    private readonly SortedSet<string> owned = new(StringComparer.OrdinalIgnoreCase);

    public Outfit Worn { get; private set; } = Outfit.Own;

    /// <summary>The garments bought, by id (the free ones aren't listed: everyone has them).</summary>
    public IReadOnlyCollection<string> Owned => owned;

    public bool Owns(string? garment) =>
        garment == null || owned.Contains(garment) || ClothingDatabase.Get(garment) is { IsFree: true };

    public void Clear()
    {
        owned.Clear();
        Worn = Outfit.Own;
    }

    /// <summary>Adds a garment to the wardrobe (bought, or given); unknown ids are ignored.</summary>
    public void Give(string garment)
    {
        if (ClothingDatabase.Get(garment) is { IsFree: false } g) owned.Add(g.Id);
    }

    /// <summary>Puts on a garment owned (null: the look's own in that slot). False if it isn't owned or isn't for that slot.</summary>
    public bool Wear(ClothingSlot slot, string? garment)
    {
        if (garment != null && (ClothingDatabase.Get(garment) is not { } g || g.Slot != slot || !Owns(garment))) return false;
        Worn = Worn.With(slot, garment == null ? null : ClothingDatabase.Get(garment)!.Id);
        return true;
    }

    /// <summary>
    /// Buys a garment with <paramref name="money"/> and puts it on: returns what it cost, or -1 if the money doesn't
    /// reach. Something owned already costs nothing.
    /// </summary>
    public int Buy(Garment garment, int money)
    {
        int cost = Owns(garment.Id) ? 0 : garment.Price;
        if (cost > money) return -1;
        Give(garment.Id);
        Wear(garment.Slot, garment.Id);
        return cost;
    }

    /// <summary>
    /// A new game's wardrobe (plan 11 · C10): nothing bought, and the outfit chosen in the introduction given for nothing
    /// and worn. Null or the look's own clothes is today's start.
    /// </summary>
    public void Start(Outfit? chosen)
    {
        Clear();
        if (chosen == null) return;
        foreach (var (slot, garment) in chosen.Garments())
        {
            Give(garment);
            Wear(slot, garment);
        }
    }

    /// <summary>Takes a save's wardrobe back: garments the data no longer has are dropped, and so is wearing one not owned.</summary>
    public void Restore(Outfit? worn, IEnumerable<string>? bought)
    {
        Clear();
        foreach (string id in bought ?? Enumerable.Empty<string>()) Give(id);
        if (worn == null) return;
        foreach (var (slot, garment) in worn.Garments()) Wear(slot, garment);
        Restyle(worn);
    }

    /// <summary>
    /// The salon's work (plan 43 · LF3): the cut, the hair's colour, the eyes and the skin of <paramref name="style"/>
    /// put on what is worn, each one the data doesn't know (or null) being the look's own.
    /// </summary>
    public void Restyle(Outfit style) => Worn = Worn with
    {
        Hair = ClothingDatabase.Cuts.FirstOrDefault(c => string.Equals(c, style.Hair, StringComparison.OrdinalIgnoreCase)),
        HairColour = ClothingDatabase.HairColourOf(style.HairColour)?.Id,
        Eyes = ClothingDatabase.EyeColourOf(style.Eyes)?.Id,
        Skin = ClothingDatabase.SkinToneOf(style.Skin)?.Id
    };

    /// <summary>
    /// The dye house's work (plan 43 · LF3): a garment owned, dyed, is a garment of its own beside the plain one, owned
    /// and worn in its slot; returns its id, or null for one not owned, already dyed, with no colour, or a dye unknown.
    /// </summary>
    public string? Dye(string garment, string dye)
    {
        if (!Owns(garment) || ClothingDatabase.DyeIn(garment) != null || ClothingDatabase.Get(garment) is not { IsNothing: false } g || ClothingDatabase.DyeOf(dye) == null)
            return null;
        string dyed = ClothingDatabase.DyedId(g.Id, ClothingDatabase.DyeOf(dye)!.Id);
        owned.Add(dyed);
        Wear(g.Slot, dyed);
        return dyed;
    }
}
