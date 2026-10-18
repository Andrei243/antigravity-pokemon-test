using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The later games' items that change a Pokémon outside a battle and are no medicine (plan 06 · R28b), each told by a
/// field use of our own that the importer gives it: a mint (its stats grow as another nature's), an Exp. Candy (so many
/// Experience Points), an Ability Capsule (the other of its two abilities), an Ability Patch (its hidden ability, and
/// back), a Dynamax Candy (a Dynamax Level) and Oricorio's nectars (another style). The feathers, mochi and Legends:
/// Arceus's candies are effort items of Platinum's kind and go through <see cref="EffortRules"/>. Like
/// <see cref="ItemUse"/>, <see cref="WouldHelp"/> is the bag's ABLE and <see cref="Apply"/> does it; each changes
/// nothing where it would have no effect. Pokémon Showdown has no bag, so the rules are the newest games' as published
/// (docs/mechanics/rulings.md, "Items used outside a battle"). No drawing or input.
/// </summary>
public static class LaterItemUse
{
    public const string Mint = "Mint", ExpCandy = "ExpCandy", AbilityCapsule = "AbilityCapsule", AbilityPatch = "AbilityPatch",
        DynamaxCandy = "DynamaxCandy", Nectar = "Nectar";

    /// <summary>The most Dynamax Levels a Pokémon may have.</summary>
    public const int MaxDynamaxLevel = 10;

    /// <summary>The species that can't Dynamax, whom a Dynamax Candy does nothing for (Sword and Shield's).</summary>
    private static readonly HashSet<string> NeverDynamax = new() { "Zacian", "Zamazenta", "Eternatus" };

    /// <summary>Whether the item is one of these.</summary>
    public static bool Handles(ItemData item) => item.FieldUse is Mint or ExpCandy or AbilityCapsule or AbilityPatch or DynamaxCandy or Nectar;

    /// <summary>The nature a mint gives (its name's first word), or null for an item that isn't one.</summary>
    public static Nature? MintOf(ItemData item) =>
        item.FieldUse == Mint && Enum.TryParse<Nature>(item.Name.Split(' ')[0], out var nature) ? nature : null;

    /// <summary>The Experience Points an Exp. Candy gives.</summary>
    public static int ExpOf(ItemData item) => EffortRules.Use(item, "exp");

    /// <summary>The style a nectar puts Oricorio in: null for the Red Nectar's, which is its species' own (Baile).</summary>
    public static string? StyleOf(ItemData item) => item.Name switch
    {
        "Yellow Nectar" => "Oricorio-Pom-Pom",
        "Pink Nectar" => "Oricorio-Pau",
        "Purple Nectar" => "Oricorio-Sensu",
        _ => null
    };

    /// <summary>The abilities it may have that aren't hidden, without the same one twice.</summary>
    private static List<string> Regular(Pokemon p) => p.Abilities.Distinct().ToList();

    /// <summary>Its hidden ability where that is one of its own (not also one of its two).</summary>
    private static string? Hidden(Pokemon p) => p.HiddenAbilityName is { } h && !p.Abilities.Contains(h) ? h : null;

    /// <summary>Whether using it on this Pokémon would do anything.</summary>
    public static bool WouldHelp(ItemData item, Pokemon p)
    {
        if (p.IsEgg || !Handles(item)) return false;
        switch (item.FieldUse)
        {
            case Mint:
                return MintOf(item) is { } nature && p.StatNature != nature;
            case ExpCandy:
                return p.Level < 100 && ExpOf(item) > 0;
            case AbilityCapsule:
                // Between its two, never from or to the hidden one
                return Regular(p).Count >= 2 && p.AbilityName != null && Regular(p).Contains(p.AbilityName);
            case AbilityPatch:
                // To the hidden one, and from it back to its first (Scarlet and Violet's)
                return Hidden(p) != null && p.AbilityName != null;
            case DynamaxCandy:
                return p.DynamaxLevel < MaxDynamaxLevel && !NeverDynamax.Contains(p.Species.Name);
            case Nectar:
                return p.Species.Name == "Oricorio" && p.Form != StyleOf(item) && (StyleOf(item) == null || p.Species.Form(StyleOf(item)) != null);
        }
        return false;
    }

    /// <summary>Uses it on the Pokémon and says what happened; nothing is changed when it would have no effect.</summary>
    public static ItemUseResult Apply(ItemData item, Pokemon p)
    {
        if (!WouldHelp(item, p)) return ItemUseResult.Nothing;
        string name = p.DisplayName;
        switch (item.FieldUse)
        {
            case Mint:
            {
                var nature = MintOf(item)!.Value;
                // A mint of its own nature takes the last mint's away
                p.MintNature = nature == p.Nature ? null : nature;
                p.ReckonStats();
                return new ItemUseResult(true, Text.Of("itemUse.minted", ("name", name), ("nature", Words.Nature(nature))));
            }
            case ExpCandy:
            {
                int friendship = p.Friendship, oldMax = p.MaxHP, oldLevel = p.Level;
                bool fainted = p.IsFainted;
                int exp = Math.Min(ExpOf(item), Math.Max(0, Pokemon.GetExpForLevel(100, p.Species.GrowthRate) - p.CurrentExp));
                p.GainExp(exp, out var learned, out var wanted);
                // As a Rare Candy: the levels' own friendship is a battle's, and a fainted Pokémon comes round with what they added
                p.Friendship = friendship;
                if (p.Level == oldLevel)
                    return new ItemUseResult(true, Text.Of("itemUse.gainedExp", ("name", name), ("exp", exp)));
                if (fainted) p.Revive(Math.Max(1, p.MaxHP - oldMax));
                var said = new List<string> { Text.Of("bag.grew", ("name", name), ("level", p.Level)) };
                said.AddRange(learned.Select(m => Text.Of("bag.learned", ("name", name), ("move", LocalNames.Move(m)))));
                return new ItemUseResult(true, string.Join(" ", said)) { NewLevel = p.Level, Learned = learned, Wanted = wanted };
            }
            case AbilityCapsule:
            {
                var two = Regular(p);
                p.AbilityName = two[0] == p.AbilityName ? two[1] : two[0];
                return new ItemUseResult(true, Text.Of("itemUse.abilityChanged", ("name", name), ("ability", LocalNames.Ability(p.AbilityName))));
            }
            case AbilityPatch:
            {
                string hidden = Hidden(p)!;
                p.AbilityName = p.AbilityName == hidden ? p.Abilities[0] : hidden;
                return new ItemUseResult(true, Text.Of("itemUse.abilityChanged", ("name", name), ("ability", LocalNames.Ability(p.AbilityName))));
            }
            case DynamaxCandy:
                p.DynamaxLevel++;
                return new ItemUseResult(true, Text.Of("itemUse.dynamaxLevel", ("name", name), ("level", p.DynamaxLevel)));
            case Nectar:
                p.ChangeForm(StyleOf(item));
                return new ItemUseResult(true, Text.Of("bag.changedForm", ("name", name)));
        }
        return ItemUseResult.Nothing;
    }
}
