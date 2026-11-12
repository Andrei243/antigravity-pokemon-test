using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The treats of ours (plan 35 · BN3; docs/mechanics/rulings.md, "Care"), GPU-free: eleven items with <see cref="ItemData.Treat"/>,
/// five sold (grade 1, one flavour each), five a baker makes from three berries whose strongest flavour is theirs (grade 2),
/// and a mixed one from one berry of each flavour (grade 3), which every nature with a taste likes. A treat fills its grade's
/// pips of fullness; its friendship is its grade's, doubled for the flavour its nature likes and halved for the one it dislikes.
/// </summary>
public static class Treats
{
    /// <summary>The stock the confectioners sell (<c>marts.json</c>'s <c>treats</c>).</summary>
    public const string Stock = "treats";

    /// <summary>A treat's friendship by grade, for friendship below 100, below 200 and from 200 up.</summary>
    public static (int Low, int Mid, int High) FriendshipOf(int grade) => grade switch
    {
        >= 3 => (8, 6, 4),
        2 => (6, 4, 3),
        _ => (4, 3, 2)
    };

    /// <summary>Every treat of the data.</summary>
    public static IEnumerable<ItemData> All => ItemDatabase.GetAll().Where(i => i.Treat != null);

    public static Flavor? FlavourOf(ItemData treat) =>
        treat.Treat?.Flavor is { } f && Enum.TryParse<Flavor>(f, ignoreCase: true, out var flavour) ? flavour : null;

    /// <summary>The treat of a flavour and grade (null for the mixed, grade 3).</summary>
    public static ItemData? Of(Flavor? flavour, int grade) =>
        All.FirstOrDefault(t => t.Treat!.Grade == grade && FlavourOf(t) == flavour);

    /// <summary>How a nature takes a meal: a treat by its flavour (the mixed one liked by any nature with a taste), a berry by its strongest.</summary>
    public static MealTaste TasteOf(ItemData food, Nature nature)
    {
        if (food.Treat is { } t)
        {
            if (t.Flavor == null) return Poffins.TasteOf(nature).Liked != null ? MealTaste.Liked : MealTaste.Plain;
            return CareRules.TasteOf(FlavourOf(food), nature);
        }
        return CareRules.TasteOf(CareRules.StrongestFlavour(food), nature);
    }

    /// <summary>A treat's friendship for a nature: its grade's, doubled liked and halved disliked.</summary>
    public static (int Low, int Mid, int High) FriendshipFor(ItemData treat, Nature nature)
    {
        var (low, mid, high) = FriendshipOf(treat.Treat?.Grade ?? 1);
        return TasteOf(treat, nature) switch
        {
            MealTaste.Liked => (low * 2, mid * 2, high * 2),
            MealTaste.Disliked => (low / 2, mid / 2, high / 2),
            _ => (low, mid, high)
        };
    }

    /// <summary>A treat eaten: its grade's pips, the mood by its taste, its friendship. False if it would not eat.</summary>
    public static bool Feed(CareRecord page, Pokemon p, int day, ItemData treat) =>
        treat.Treat is { } t && CareRules.Feed(page, p, day, TasteOf(treat, p.Nature), t.Grade, FriendshipFor(treat, p.Nature));

    /// <summary>The bag's berries whose strongest flavour is this one, in the bag's own order.</summary>
    public static List<ItemData> BerriesOf(Inventory bag, Flavor flavour) =>
        bag.GetPocketItems(ItemPocket.Berries).Select(s => s.Data).Where(i => i.Berry != null && CareRules.StrongestFlavour(i) == flavour).ToList();

    /// <summary>How many berries of a flavour the bag holds.</summary>
    public static int CountOf(Inventory bag, Flavor flavour) => BerriesOf(bag, flavour).Sum(i => bag.GetQuantity(i));

    /// <summary>The flavour the bag has most berries of, with three at least (the first of the five on a tie); null for none.</summary>
    public static Flavor? MostOf(Inventory bag)
    {
        Flavor? best = null;
        int most = 2;
        foreach (var f in Enum.GetValues<Flavor>())
        {
            int n = CountOf(bag, f);
            if (n > most) { most = n; best = f; }
        }
        return best;
    }

    /// <summary>Whether a bake can be made: three berries of the flavour (or, for the mixed, one of each of the five).</summary>
    public static bool CanBake(Inventory bag, Flavor? flavour, bool mixed) =>
        mixed ? Enum.GetValues<Flavor>().All(f => CountOf(bag, f) > 0) : flavour is { } f2 && CountOf(bag, f2) >= 3;

    /// <summary>
    /// The baker's work: the berries taken from the bag (three of the flavour, the most plentiful first, or one of each for
    /// the mixed) and the treat they make, which goes in the bag; null and nothing taken if they aren't there.
    /// </summary>
    public static ItemData? Bake(Inventory bag, Flavor? flavour, bool mixed)
    {
        if (!CanBake(bag, flavour, mixed)) return null;
        var made = mixed ? Of(null, 3) : Of(flavour, 2);
        if (made == null) return null;
        if (mixed)
            foreach (var f in Enum.GetValues<Flavor>())
                bag.RemoveItem(BerriesOf(bag, f).OrderByDescending(bag.GetQuantity).First());
        else
            for (int k = 0; k < 3; k++)
                bag.RemoveItem(BerriesOf(bag, flavour!.Value).OrderByDescending(bag.GetQuantity).First());
        bag.AddItem(made, 1);
        return made;
    }
}
