using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Core;

/// <summary>Where a garment is worn. The order is the wardrobe's tabs'.</summary>
public enum ClothingSlot { Hat, Top, Bottoms, Shoes, Bag }

/// <summary>
/// What the player wears: a garment of <c>clothes.json</c> in each slot, by its id, or null for the look's own (the
/// clothes the boy or the girl starts in). Nothing but drawing reads it: the name, the trainer id, the assistant and
/// the rules stay the look's. A character dressed in it is named <c>PLAYER@cap_red.-.-.-.-</c> (<see cref="Dress"/>),
/// so the rig, its sprites and its mesh cache follow the outfit with no registry, and an outfit with nothing chosen
/// is the plain look's name, drawn exactly as before.
/// </summary>
public sealed record Outfit(string? Hat = null, string? Top = null, string? Bottoms = null, string? Shoes = null, string? Bag = null)
{
    /// <summary>The look's own clothes in every slot.</summary>
    public static readonly Outfit Own = new();

    public static readonly ClothingSlot[] Slots = Enum.GetValues<ClothingSlot>();

    /// <summary>Stands for the look's own garment in <see cref="Key"/>.</summary>
    public const string OwnMark = "-";

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsOwn => Slots.All(s => this[s] == null);

    public string? this[ClothingSlot slot] => slot switch
    {
        ClothingSlot.Hat => Hat,
        ClothingSlot.Top => Top,
        ClothingSlot.Bottoms => Bottoms,
        ClothingSlot.Shoes => Shoes,
        _ => Bag
    };

    /// <summary>This outfit with <paramref name="garment"/> in its slot (null: the look's own).</summary>
    public Outfit With(ClothingSlot slot, string? garment) => slot switch
    {
        ClothingSlot.Hat => this with { Hat = garment },
        ClothingSlot.Top => this with { Top = garment },
        ClothingSlot.Bottoms => this with { Bottoms = garment },
        ClothingSlot.Shoes => this with { Shoes = garment },
        _ => this with { Bag = garment }
    };

    /// <summary>The garments in slot order, a dash for the look's own: <c>cap_red.-.-.-.-</c>.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string Key => string.Join('.', Slots.Select(s => this[s] ?? OwnMark));

    /// <summary>Reads a <see cref="Key"/> back; a part missing or a dash is the look's own.</summary>
    public static Outfit FromKey(string key)
    {
        var parts = key.Split('.');
        var outfit = Own;
        for (int i = 0; i < Slots.Length && i < parts.Length; i++)
            if (parts[i].Length > 0 && parts[i] != OwnMark) outfit = outfit.With(Slots[i], parts[i]);
        return outfit;
    }

    /// <summary>The character type of <paramref name="character"/> (PLAYER or DAWN) dressed in <paramref name="outfit"/>.</summary>
    public static string Dress(string character, Outfit? outfit) =>
        outfit == null || outfit.IsOwn ? character : character + "@" + outfit.Key;

    /// <summary>Splits a dressed character type into the look's own type and the outfit (none when it isn't dressed).</summary>
    public static (string Character, Outfit? Outfit) Undress(string character)
    {
        int at = character.IndexOf('@');
        return at < 0 ? (character, null) : (character[..at], FromKey(character[(at + 1)..]));
    }

    /// <summary>The garments worn, slot by slot, the look's own left out.</summary>
    public IEnumerable<(ClothingSlot Slot, string Garment)> Garments() =>
        Slots.Where(s => this[s] != null).Select(s => (s, this[s]!));
}
