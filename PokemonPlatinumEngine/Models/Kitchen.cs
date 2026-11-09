using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// One thing a dish wants in the pot (plan 39 · CR6; <c>recipes.json</c>'s <c>kitchen</c>): a material of the pantry, one
/// of a few items (Honey, a mushroom), or any berry.
/// </summary>
public sealed class KitchenNeed
{
    public string? Material { get; set; }
    public List<string>? Items { get; set; }
    public bool Berry { get; set; }

    /// <summary>Whether an ingredient answers this need.</summary>
    public bool Takes(KitchenIngredient ingredient) =>
        Material != null ? ingredient.IsMaterial && ingredient.Id == Material
        : Items != null ? !ingredient.IsMaterial && Items.Contains(ingredient.Id)
        : Berry && !ingredient.IsMaterial && ItemDatabase.Get(ingredient.Id)?.Pocket == ItemPocket.Berries;
}

/// <summary>
/// A dish of the camp's pot (ours): an id of letters and digits (its words are <c>campPot.dish.name.&lt;id&gt;</c> and
/// <c>campPot.dish.desc.&lt;id&gt;</c>), what it wants in the pot, and the power it starts (none for the plain dish). A type's
/// encounters takes its type from the first berry in the pot (its Natural Gift's type).
/// </summary>
public sealed class KitchenDish
{
    [DataRequired] public string Id { get; set; } = "";
    public List<KitchenNeed> Needs { get; set; } = new();
    public PowerKind? Power { get; set; }
}

/// <summary>What a dish of a grade gives: the share of each Pokémon's HP back, and its power's level and seconds of play.</summary>
public sealed class KitchenGrade
{
    [DataRequired] public int Heal { get; set; }
    [DataRequired] public int Level { get; set; }
    [DataRequired] public int Seconds { get; set; }
}

/// <summary>The camp's section of <c>Data/recipes.json</c> (plan 39 · CR6).</summary>
public sealed class KitchenRecipes
{
    /// <summary>The most ingredients the pot holds.</summary>
    public int Most { get; set; } = 4;
    /// <summary>The dish cooked from anything no other dish wants: it heals and starts nothing.</summary>
    public string Plain { get; set; } = "";
    /// <summary>The four grades, plain to superb.</summary>
    public List<KitchenGrade> Grades { get; set; } = new();
    public List<KitchenDish> Dishes { get; set; } = new();
}

/// <summary>An ingredient in the pot: a material of the pouch (the pantry's) or an item of the bag (a berry, Honey, a mushroom).</summary>
public readonly record struct KitchenIngredient(string Id, bool IsMaterial)
{
    public string Name => IsMaterial ? MaterialTable.NameOf(Id) : ItemDatabase.Get(Id)?.DisplayName ?? Id;
}

/// <summary>A dish's four grades (plan 39 · CR6, "graded in four steps").</summary>
public enum DishGrade { Plain, Good, Great, Superb }

/// <summary>What one round of stirring came to.</summary>
public enum StirScore { Miss, Good, Perfect }

/// <summary>
/// The camp's pot at work (plan 39 · CR6; <c>docs/mechanics/rulings.md</c>, "Materials, crafting and cooking"), thirty frames
/// a second as <c>PoffinPot</c> and the Pokéathlon's events are: four rounds, in each a needle sweeping to and fro over the
/// pot's gauge and a zone of heat to press in. A press scores the round (in the middle half of the zone perfect, in the zone
/// good, out of it a miss), and a round with no press in <see cref="RoundFrames"/> is a miss. The zone's place and the
/// needle's pace are the pot's seed's, the press counts through <see cref="TimingAssists"/> (Generous doubles the zone,
/// Automatic presses as the zone opens), so a grade is a pure function of the seed and the frames the player pressed on.
/// Two buttons: Confirm stirs, Cancel takes the pot off the fire (nothing is cooked or used). No drawing, input or chance.
/// </summary>
public sealed class Kitchen
{
    public const int FramesPerSecond = 30;
    public const int Rounds = 4;
    /// <summary>The frames a round waits for its press.</summary>
    public const int RoundFrames = 150;
    /// <summary>Half the zone's width, in the gauge's own length (nought to one), before the assist.</summary>
    public const float Half = 0.08f;

    private readonly uint seed;
    private readonly List<StirScore> scores = new();

    public Kitchen(uint seed, TimingAssist assist)
    {
        this.seed = seed;
        Assist = assist;
    }

    public TimingAssist Assist { get; }

    /// <summary>The frames since the pot went on, and since this round began.</summary>
    public int Frame { get; private set; }
    public int RoundFrame { get; private set; }

    /// <summary>The round under way, from nought; <see cref="Rounds"/> once the dish is done.</summary>
    public int Round => scores.Count;

    public bool Done => scores.Count >= Rounds;

    public IReadOnlyList<StirScore> Scores => scores;

    /// <summary>Frames since the last round was scored, or -1 before any.</summary>
    public int SinceScored { get; private set; } = -1;

    /// <summary>Half the zone's width under the assist.</summary>
    public float HalfWindow => TimingAssists.Window(Half, Assist);

    /// <summary>The middle of this round's zone, between 0.2 and 0.8 of the gauge.</summary>
    public float Zone => Done ? 0.5f : 0.2f + SplitMix.Hash(Round * 2 + 1, seed) % 601 / 1000f;

    /// <summary>The frames the needle takes to cross the gauge once in this round: 30 to 53.</summary>
    public int Sweep => Done ? 40 : 30 + (int)(SplitMix.Hash(Round * 2 + 2, seed) % 24);

    /// <summary>Where the needle is, nought to one: to and fro at the round's pace from the gauge's left end.</summary>
    public float Needle
    {
        get
        {
            int period = Sweep * 2;
            int at = RoundFrame % period;
            return at <= Sweep ? at / (float)Sweep : (period - at) / (float)Sweep;
        }
    }

    /// <summary>Whether the needle is in the zone now.</summary>
    public bool WindowOpen => !Done && MathF.Abs(Needle - Zone) <= HalfWindow;

    /// <summary>One frame: the player's press (or the assist's), the round's score when pressed or timed out, the needle on.</summary>
    public void Step(bool press)
    {
        if (Done) return;
        Frame++;
        if (SinceScored >= 0) SinceScored++;
        if (TimingAssists.Pressed(press, WindowOpen, Assist))
        {
            float off = MathF.Abs(Needle - Zone);
            Score(off <= HalfWindow / 2f ? StirScore.Perfect : off <= HalfWindow ? StirScore.Good : StirScore.Miss);
            return;
        }
        RoundFrame++;
        if (RoundFrame >= RoundFrames) Score(StirScore.Miss);
    }

    private void Score(StirScore score)
    {
        scores.Add(score);
        RoundFrame = 0;
        SinceScored = 0;
    }

    /// <summary>The points of the rounds so far: two for perfect, one for good.</summary>
    public int Points => scores.Sum(s => (int)s);

    /// <summary>The dish's grade from its points: seven or eight superb, five or six great, three or four good, less plain.</summary>
    public DishGrade Grade => GradeOf(Points);

    public static DishGrade GradeOf(int points) =>
        points >= 7 ? DishGrade.Superb : points >= 5 ? DishGrade.Great : points >= 3 ? DishGrade.Good : DishGrade.Plain;
}

/// <summary>What a meal came to: the dish, its grade, how many were healed, cured and fed, and the power it started.</summary>
public sealed record MealServed(KitchenDish Dish, DishGrade Grade, int Healed, int Cured, int Fed, ActivePower? Power);

/// <summary>
/// The camp's dishes and the team eating them (plan 39 · CR6; plan 10 · F8, taken over). Which dish a pot of ingredients
/// cooks, what is in the camp's reach to cook with, and what a meal does: each Pokémon's HP back by the grade's share
/// (one that has fainted is not brought round), its condition cured as the camp cures one, its friendship's
/// <see cref="FriendshipEvent.Fed"/> once a visit, and the dish's power started through <see cref="PowerState.Grant"/>
/// for the grade's level and length. Nothing here is drawn by chance. No drawing or input.
/// </summary>
public static class Meals
{
    public static KitchenRecipes Table => RecipeTable.Kitchen;

    public static KitchenDish? Dish(string id) => Table.Dishes.FirstOrDefault(d => d.Id == id);

    public static KitchenDish Plain => Dish(Table.Plain) ?? new KitchenDish { Id = Table.Plain };

    public static KitchenGrade GradeRow(DishGrade grade) =>
        Table.Grades.Count == 0 ? new KitchenGrade { Heal = 25, Level = 1, Seconds = PowerTable.Seconds } : Table.Grades[Math.Min((int)grade, Table.Grades.Count - 1)];

    /// <summary>The name a dish is shown by, and what it is (<c>campPot.dish.name.*</c>, <c>campPot.dish.desc.*</c>).</summary>
    public static string NameOf(KitchenDish dish)
    {
        string id = dish.Id;
        return Text.Of($"campPot.dish.name.{id}");
    }

    public static string DescriptionOf(KitchenDish dish)
    {
        string id = dish.Id;
        return Text.Of($"campPot.dish.desc.{id}");
    }

    /// <summary>The words of a grade (<c>campPot.grade.*</c>).</summary>
    public static string NameOf(DishGrade grade)
    {
        string key = grade.ToString().ToLowerInvariant();
        return Text.Of($"campPot.grade.{key}");
    }

    /// <summary>Whether something of the pouch or the bag may go in the pot: the pantry's goods, the bag's berries, and the items a dish names.</summary>
    public static bool IsIngredient(KitchenIngredient ingredient) =>
        ingredient.IsMaterial ? MaterialTable.Get(ingredient.Id)?.Kind == MaterialKind.Pantry
        : ItemDatabase.Get(ingredient.Id) is { } item && (item.Pocket == ItemPocket.Berries || Table.Dishes.Any(d => d.Needs.Any(n => n.Items?.Contains(item.Name) == true)));

    /// <summary>What may go in the pot from what is held: the pantry's goods of the pouch in the table's order, then the bag's berries and named items in the bag's.</summary>
    public static List<KitchenIngredient> OnHand(MaterialPouch pouch, Inventory bag)
    {
        var list = MaterialTable.All.Where(m => m.Kind == MaterialKind.Pantry && pouch.Count(m.Id) > 0)
            .Select(m => new KitchenIngredient(m.Id, true)).ToList();
        foreach (var pocket in new[] { ItemPocket.Items, ItemPocket.Berries })
            foreach (var stack in bag.GetPocketItems(pocket))
                if (stack.Quantity > 0 && new KitchenIngredient(stack.Name, false) is var ingredient && IsIngredient(ingredient) && !list.Contains(ingredient))
                    list.Add(ingredient);
        return list;
    }

    /// <summary>How many of an ingredient are held.</summary>
    public static int Held(KitchenIngredient ingredient, MaterialPouch pouch, Inventory bag) =>
        ingredient.IsMaterial ? pouch.Count(ingredient.Id) : ItemDatabase.Get(ingredient.Id) is { } item ? bag.GetQuantity(item) : 0;

    /// <summary>Whether a pot of ingredients holds everything a dish wants, each need met by an ingredient of its own.</summary>
    public static bool Holds(KitchenDish dish, IReadOnlyList<KitchenIngredient> pot)
    {
        var used = new bool[pot.Count];
        foreach (var need in dish.Needs)
        {
            int at = -1;
            for (int i = 0; i < pot.Count; i++)
                if (!used[i] && need.Takes(pot[i])) { at = i; break; }
            if (at < 0) return false;
            used[at] = true;
        }
        return true;
    }

    /// <summary>
    /// The dish a pot cooks: of the dishes whose wants the pot holds, the one that wants the most (the table's order breaks
    /// a tie); the plain dish from anything else.
    /// </summary>
    public static KitchenDish DishFor(IReadOnlyList<KitchenIngredient> pot) =>
        Table.Dishes.Where(d => d.Id != Table.Plain && d.Needs.Count > 0 && Holds(d, pot))
            .OrderByDescending(d => d.Needs.Count).FirstOrDefault() ?? Plain;

    /// <summary>The type of a type's encounters a pot gives: its first berry's Natural Gift type, Normal for a berry with none.</summary>
    public static PokemonType? TypeFor(IReadOnlyList<KitchenIngredient> pot) =>
        pot.Where(i => !i.IsMaterial).Select(i => ItemDatabase.Get(i.Id)).FirstOrDefault(i => i?.Pocket == ItemPocket.Berries) is { } berry
            ? berry.NaturalGiftType ?? PokemonType.Normal : null;

    /// <summary>Whether the pot's ingredients are all held, each as many times as it is in the pot.</summary>
    public static bool AllHeld(IReadOnlyList<KitchenIngredient> pot, MaterialPouch pouch, Inventory bag) =>
        pot.GroupBy(i => i).All(g => Held(g.Key, pouch, bag) >= g.Count());

    /// <summary>Takes the pot's ingredients from the pouch and the bag (the caller has asked <see cref="AllHeld"/>).</summary>
    public static void Use(IReadOnlyList<KitchenIngredient> pot, MaterialPouch pouch, Inventory bag)
    {
        foreach (var ingredient in pot)
        {
            if (ingredient.IsMaterial) pouch.Take(ingredient.Id, 1);
            else if (ItemDatabase.Get(ingredient.Id) is { } item) bag.RemoveItem(item, 1);
        }
    }

    /// <summary>
    /// The team eats a dish of a grade: HP back by the grade's share (none for a Pokémon that has fainted or an Egg), every
    /// condition but fainting cured, each fed once a visit at the camp (<paramref name="visit"/>; a meal elsewhere befriends
    /// nobody), and the dish's power started, a type's encounters of <paramref name="type"/> (none started without one).
    /// </summary>
    public static MealServed Serve(Party party, KitchenDish dish, DishGrade grade, PowerState? powers, CampVisit? visit, PokemonType? type)
    {
        var row = GradeRow(grade);
        int healed = 0, cured = 0, fed = 0;
        foreach (var p in party.Members)
        {
            if (p.IsEgg) continue;
            if (p.CurrentHP > 0 && p.CurrentHP < p.MaxHP)
            {
                p.CurrentHP = Math.Min(p.MaxHP, p.CurrentHP + Math.Max(1, p.MaxHP * row.Heal / 100));
                healed++;
            }
            if (p.Status is not (StatusCondition.None or StatusCondition.Faint))
            {
                p.Status = StatusCondition.None;
                p.SleepTurns = 0;
                p.ToxicCounter = 0;
                cured++;
            }
            if (visit != null && visit.Eat(p)) fed++;
        }
        ActivePower? started = null;
        if (dish.Power is { } kind && powers != null && PowerTable.Read.Contains(kind)
            && powers.Grant(kind, row.Level, row.Seconds, kind == PowerKind.TypeEncounter ? type ?? PokemonType.Normal : null))
            started = powers.InForce.First(a => a.Kind == kind);
        return new MealServed(dish, grade, healed, cured, fed, started);
    }

    /// <summary>
    /// What is wrong with the kitchen's section of a file, a line each (the content check and the tests ask): the plain dish
    /// named and wanting nothing, four grades that climb, ids of letters and digits named once, each dish's needs naming a
    /// pantry good or an item that exists, a power that is read today, and its words in the table.
    /// </summary>
    public static IEnumerable<(string Name, string What)> Problems(KitchenRecipes table)
    {
        if (table.Dishes.Count == 0) yield break;
        if (table.Dishes.FirstOrDefault(d => d.Id == table.Plain) is not { } plain) yield return ("plain", $"names '{table.Plain}', which is no dish");
        else if (plain.Needs.Count > 0 || plain.Power != null) yield return (plain.Id, "is the plain dish and wants something or starts a power");
        if (table.Most < 1) yield return ("most", "is under one: the pot holds nothing");
        if (table.Grades.Count != 4) yield return ("grades", "are not four");
        for (int g = 0; g < table.Grades.Count; g++)
        {
            var row = table.Grades[g];
            if (row.Heal < 1 || row.Heal > 100) yield return ($"grades[{g}]", "heals no share of HP between 1 and 100");
            if (row.Level < 1 || row.Level > PowerTable.TopLevel) yield return ($"grades[{g}]", "starts a power at no level of one to three");
            if (row.Seconds < 1) yield return ($"grades[{g}]", "starts a power for no time");
            if (g > 0 && (row.Heal < table.Grades[g - 1].Heal || row.Level < table.Grades[g - 1].Level)) yield return ($"grades[{g}]", "gives less than the grade before it");
        }
        foreach (var group in table.Dishes.GroupBy(d => d.Id).Where(g => g.Count() > 1)) yield return (group.Key, "is a dish twice");
        foreach (var dish in table.Dishes)
        {
            if (dish.Id.Length == 0 || !dish.Id.All(char.IsAsciiLetterOrDigit)) yield return (dish.Id, "has no id of letters and digits");
            if (dish.Needs.Count > table.Most) yield return (dish.Id, "wants more than the pot holds");
            if (dish.Power is { } kind && !PowerTable.Read.Contains(kind)) yield return (dish.Id, $"starts {kind}, which nothing reads yet (plan 36 · TR7)");
            if (dish.Power == PowerKind.TypeEncounter && !dish.Needs.Any(n => n.Berry)) yield return (dish.Id, "starts a type's encounters and wants no berry to take its type from");
            foreach (var need in dish.Needs)
            {
                int named = (need.Material != null ? 1 : 0) + (need.Items != null ? 1 : 0) + (need.Berry ? 1 : 0);
                if (named != 1) yield return (dish.Id, "has a need that names not exactly one of a material, items or a berry");
                if (need.Material != null && MaterialTable.Get(need.Material)?.Kind != MaterialKind.Pantry) yield return (dish.Id, $"wants '{need.Material}', which is no good of the pantry");
                foreach (string item in need.Items ?? new()) if (ItemDatabase.Get(item) == null) yield return (dish.Id, $"wants '{item}', which is no item");
            }
            if (!Text.Has($"campPot.dish.name.{dish.Id}")) yield return (dish.Id, $"has no name (campPot.dish.name.{dish.Id} in ui.json)");
            if (!Text.Has($"campPot.dish.desc.{dish.Id}")) yield return (dish.Id, $"has no description (campPot.dish.desc.{dish.Id} in ui.json)");
        }
    }
}
