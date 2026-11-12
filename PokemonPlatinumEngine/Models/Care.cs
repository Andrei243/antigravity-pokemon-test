using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>What a battle left on a Pokémon's coat (plan 35 · BN1; docs/mechanics/rulings.md, "Care"), at most two at once.</summary>
public enum SoilMark { None, Dust, Mud, Leaves, Wet, Snow }

/// <summary>The five faces of a Pokémon's mood, from its number (<see cref="CareRules.MoodOf"/>).</summary>
public enum CareMood { Glum, Low, Calm, Cheerful, Elated }

/// <summary>
/// One Pokémon's page of the care book (plan 35 · BN1): the marks a battle left on it, how full it is today, the days its
/// pat and its grooming last counted, its mood, and whether a cure is owed it. Plain values: the save keeps it as it is.
/// </summary>
public sealed class CareRecord
{
    /// <summary>Whose page it is (<see cref="Pokemon.Id"/>).</summary>
    public Guid Id { get; set; }

    /// <summary>The marks on its coat, oldest first; at most <see cref="CareRules.MostMarks"/>.</summary>
    public List<SoilMark> Marks { get; set; } = new();

    /// <summary>The day its meals were last counted (<see cref="ApricornBox.DayOf"/>), and how many it has had that day.</summary>
    public int MealDay { get; set; } = -1;
    public int Meals { get; set; }

    /// <summary>The day a pat last counted for its friendship, and the day a grooming did; -1 for never.</summary>
    public int PatDay { get; set; } = -1;
    public int GroomDay { get; set; } = -1;

    /// <summary>Its mood, from <see cref="CareRules.MoodLowest"/> to <see cref="CareRules.MoodHighest"/>; nought is calm.</summary>
    public int Mood { get; set; }

    /// <summary>A battle has been fought since its last cure: the next cure clears its status.</summary>
    public bool CureOwed { get; set; } = true;

    /// <summary>The day it last gave up something it was holding in the pasture (plan 35 · BN5); none yet.</summary>
    public int FoundDay { get; set; } = -1;

    /// <summary>Whether the page says nothing a new page wouldn't (a page like that is not kept in the save).</summary>
    public bool IsBlank => Marks.Count == 0 && Meals == 0 && PatDay < 0 && GroomDay < 0 && Mood == 0 && CureOwed && FoundDay < 0;

    public CareRecord Copy() => new()
    {
        Id = Id, Marks = new List<SoilMark>(Marks), MealDay = MealDay, Meals = Meals, PatDay = PatDay, GroomDay = GroomDay,
        Mood = Mood, CureOwed = CureOwed, FoundDay = FoundDay
    };
}

/// <summary>The care book as the save keeps it (plan 35 · BN1, <c>SaveData.Care</c>): its pages, in no order. Null while empty.</summary>
public sealed class CareSave
{
    public List<CareRecord> Records { get; set; } = new();
}

/// <summary>
/// The care book (plan 35 · BN1; docs/mechanics/rulings.md, "Care"), GPU-free: a page for each Pokémon of the player's,
/// kept by its own id, so the Pokémon itself carries nothing of it (a battle's copy, a trade and the Home box move nothing).
/// A Pokémon with no id yet (a test's, one just caught before <c>GameEngine.GiveIds</c>) has a page kept by the Pokémon
/// itself for as long as the book lives, never saved. The rules that write the pages are <see cref="CareRules"/>'s.
/// </summary>
public sealed class CareBook
{
    private readonly Dictionary<Guid, CareRecord> pages = new();
    private readonly Dictionary<Pokemon, CareRecord> loose = new(ReferenceEqualityComparer.Instance);

    /// <summary>The page of a Pokémon, made blank the first time it is asked for.</summary>
    public CareRecord Of(Pokemon p)
    {
        if (p.Id == Guid.Empty)
        {
            if (!loose.TryGetValue(p, out var free)) loose[p] = free = new CareRecord();
            return free;
        }
        if (!pages.TryGetValue(p.Id, out var page))
        {
            // A Pokémon given its id since its page was begun keeps the page
            if (loose.Remove(p, out var earlier)) { earlier.Id = p.Id; page = earlier; }
            else page = new CareRecord { Id = p.Id };
            pages[p.Id] = page;
        }
        return page;
    }

    /// <summary>The page kept for an id, or null.</summary>
    public CareRecord? Find(Guid id) => pages.TryGetValue(id, out var page) ? page : null;

    /// <summary>Every page kept by an id.</summary>
    public IEnumerable<CareRecord> Pages => pages.Values;

    public int Count => pages.Count;

    /// <summary>Lets go of every page whose Pokémon the save no longer holds, and of the loose ones. Returns how many went.</summary>
    public int Prune(IEnumerable<Guid> held)
    {
        var keep = new HashSet<Guid>(held);
        var gone = pages.Keys.Where(id => !keep.Contains(id)).ToList();
        foreach (var id in gone) pages.Remove(id);
        loose.Clear();
        return gone.Count;
    }

    /// <summary>The book as the save keeps it, its blank pages left out; null while nothing is written.</summary>
    public CareSave? ToSave()
    {
        var written = pages.Values.Where(p => !p.IsBlank).OrderBy(p => p.Id).Select(p => p.Copy()).ToList();
        return written.Count == 0 ? null : new CareSave { Records = written };
    }

    /// <summary>Takes the book back from a save (null: a save from before the book, or one with nothing written).</summary>
    public void Restore(CareSave? save)
    {
        Clear();
        if (save == null) return;
        foreach (var record in save.Records)
            if (record.Id != Guid.Empty)
            {
                var page = record.Copy();
                if (page.Marks.Count > CareRules.MostMarks) page.Marks.RemoveRange(0, page.Marks.Count - CareRules.MostMarks);
                page.Mood = Math.Clamp(page.Mood, CareRules.MoodLowest, CareRules.MoodHighest);
                pages[record.Id] = page;
            }
    }

    public void Clear()
    {
        pages.Clear();
        loose.Clear();
    }
}

/// <summary>How a meal went down: liked, plain or disliked, by its flavour and the Pokémon's nature (<see cref="Poffins.TasteOf"/>).</summary>
public enum MealTaste { Plain, Liked, Disliked }

/// <summary>
/// The rules of the care book (plan 35 · BN1; docs/mechanics/rulings.md, "Care", where every number is): the soil a
/// battle leaves, the day's meals, a pat and a grooming once a day for friendship, a cure once for each battle, and the
/// mood. No drawing, no input, no chance: every number is fixed, so nothing here draws from <c>Dice</c> or the field's
/// generator, and no battle rule reads any of it (plan 35's decision 3).
/// </summary>
public static class CareRules
{
    public const int MostMarks = 2;
    public const int Fullness = 5;
    public const int MoodLowest = -100, MoodHighest = 100;

    // What lifts and lowers the mood (ours)
    public const int MoodWin = 8, MoodLoss = -10, MoodFaint = -15, MoodLevel = 6, MoodEvolved = 20, MoodHatched = 30;
    public const int MoodHealed = 5, MoodPat = 4, MoodMealLiked = 8, MoodMeal = 4, MoodMealDisliked = -4;
    public const int MoodGroomed = 5, MoodCured = 6, MoodPoisonBite = -1, MoodSoiledWalk = -1;

    /// <summary>How far the mood settles toward calm in a walk cycle (the team) and in a day (the boxes).</summary>
    public const int SettleWalk = 2, SettleDay = 10;

    /// <summary>The day a date is, counted as the Apricorn Box counts it.</summary>
    public static int DayOf(DateTime date) => ApricornBox.DayOf(date);

    /// <summary>The face of a mood's number.</summary>
    public static CareMood MoodOf(int mood) => mood switch
    {
        <= -60 => CareMood.Glum,
        <= -20 => CareMood.Low,
        < 20 => CareMood.Calm,
        < 60 => CareMood.Cheerful,
        _ => CareMood.Elated
    };

    /// <summary>Moves a page's mood, held within its bounds.</summary>
    public static void Lift(CareRecord page, int by) => page.Mood = Math.Clamp(page.Mood + by, MoodLowest, MoodHighest);

    /// <summary>Brings a mood toward calm by up to <paramref name="by"/>.</summary>
    public static void Settle(CareRecord page, int by) =>
        page.Mood = page.Mood > 0 ? Math.Max(0, page.Mood - by) : Math.Min(0, page.Mood + by);

    // ------------------------------------------------------------------ soil

    /// <summary>
    /// The mark a battle leaves (a table of ours): the weather first (a sandstorm's dust, rain's wet, hail's or snow's
    /// snow), then the ground (sand's dust, the Great Marsh's and a puddle's mud, grass's leaves, water's wet, snow and
    /// ice's snow); nothing on a plain, a mountain, in a cave, a building, on a bridge or somewhere special.
    /// </summary>
    public static SoilMark SoilFor(BattleTerrain terrain, BattleWeather weather) => weather switch
    {
        BattleWeather.Sandstorm => SoilMark.Dust,
        BattleWeather.Rain or BattleWeather.HeavyRain => SoilMark.Wet,
        BattleWeather.Hail or BattleWeather.Snow => SoilMark.Snow,
        _ => terrain switch
        {
            BattleTerrain.Sand => SoilMark.Dust,
            BattleTerrain.GreatMarsh or BattleTerrain.Puddle => SoilMark.Mud,
            BattleTerrain.Grass => SoilMark.Leaves,
            BattleTerrain.Water => SoilMark.Wet,
            BattleTerrain.Snow or BattleTerrain.Ice => SoilMark.Snow,
            _ => SoilMark.None
        }
    };

    /// <summary>Leaves a mark on a coat: a mark it has already is made the newest, and only the newest two are kept.</summary>
    public static void Soil(CareRecord page, SoilMark mark)
    {
        if (mark == SoilMark.None) return;
        page.Marks.Remove(mark);
        page.Marks.Add(mark);
        if (page.Marks.Count > MostMarks) page.Marks.RemoveRange(0, page.Marks.Count - MostMarks);
    }

    /// <summary>The tool that takes a mark off: the brush for dust, leaves and snow, the cloth for mud and wet.</summary>
    public static bool BrushTakes(SoilMark mark) => mark is SoilMark.Dust or SoilMark.Leaves or SoilMark.Snow;

    /// <summary>
    /// A mark cleaned off (the brush's or the cloth's work): the mood lifts, and once a day the friendship rises by
    /// <see cref="FriendshipEvent.Groomed"/>. False if it had no such mark.
    /// </summary>
    public static bool Groom(CareRecord page, Pokemon p, SoilMark mark, int day)
    {
        if (p.IsEgg || !page.Marks.Remove(mark)) return false;
        Lift(page, MoodGroomed);
        if (page.GroomDay != day)
        {
            page.GroomDay = day;
            FriendshipRules.Apply(p, FriendshipEvent.Groomed);
        }
        return true;
    }

    // ------------------------------------------------------------------ the battle's end

    /// <summary>What a battle came to for the player's side, as the care book hears it.</summary>
    /// <param name="Fought">The player's own Pokémon the battle sent out.</param>
    /// <param name="Won">The battle was won (a catch too).</param>
    /// <param name="Lost">The battle was lost.</param>
    /// <param name="Levelled">The player's Pokémon that gained a level in it.</param>
    public readonly record struct BattleOutcome(IReadOnlyCollection<Pokemon> Fought, bool Won, bool Lost, IReadOnlyCollection<Pokemon> Levelled,
        BattleTerrain Terrain, BattleWeather Weather);

    /// <summary>
    /// A battle over (plan 35 · BN1): each Pokémon sent out takes the mark of where it fought, is owed a cure, and is
    /// lifted by a win or lowered by a loss; one that fainted is lowered again; one that gained a level is lifted.
    /// </summary>
    public static void AfterBattle(CareBook book, BattleOutcome outcome)
    {
        var mark = SoilFor(outcome.Terrain, outcome.Weather);
        foreach (var p in outcome.Fought)
        {
            if (p.IsEgg) continue;
            var page = book.Of(p);
            Soil(page, mark);
            page.CureOwed = true;
            if (outcome.Won) Lift(page, MoodWin);
            if (outcome.Lost) Lift(page, MoodLoss);
            if (p.IsFainted) Lift(page, MoodFaint);
        }
        foreach (var p in outcome.Levelled)
            if (!p.IsEgg) Lift(book.Of(p), MoodLevel);
    }

    // ------------------------------------------------------------------ meals, pats and cures

    /// <summary>How full it is today (the day's meals; nothing on a day it hasn't eaten).</summary>
    public static int FullnessOn(CareRecord page, int day) => page.MealDay == day ? page.Meals : 0;

    /// <summary>Whether it would eat a meal that fills this many pips today: not an Egg, and not past full.</summary>
    public static bool WouldEat(CareRecord page, Pokemon p, int day, int fills = 1) =>
        !p.IsEgg && FullnessOn(page, day) + Math.Max(1, fills) <= Fullness;

    /// <summary>The flavour a berry is strongest in (the first of the five on a tie), or none for a berry of no flavour.</summary>
    public static Flavor? StrongestFlavour(ItemData item)
    {
        if (item.Berry is not { } b) return null;
        int[] amounts = { b.Spiciness, b.Dryness, b.Sweetness, b.Bitterness, b.Sourness };
        int best = 0;
        for (int i = 1; i < amounts.Length; i++) if (amounts[i] > amounts[best]) best = i;
        return amounts[best] > 0 ? (Flavor)best : null;
    }

    /// <summary>How a nature takes a flavour (none: plain).</summary>
    public static MealTaste TasteOf(Flavor? flavour, Nature nature)
    {
        if (flavour is not { } f) return MealTaste.Plain;
        var (liked, disliked) = Poffins.TasteOf(nature);
        return f == liked ? MealTaste.Liked : f == disliked ? MealTaste.Disliked : MealTaste.Plain;
    }

    /// <summary>
    /// A meal eaten: it fills <paramref name="fills"/> pips of today, the mood follows its taste, and the friendship rises by
    /// <paramref name="friendship"/> (a berry's is <see cref="FriendshipEvent.Fed"/>; a treat's its own, plan 35 · BN3).
    /// False if it would not eat.
    /// </summary>
    public static bool Feed(CareRecord page, Pokemon p, int day, MealTaste taste, int fills, (int Low, int Mid, int High) friendship)
    {
        if (!WouldEat(page, p, day, fills)) return false;
        if (page.MealDay != day) { page.MealDay = day; page.Meals = 0; }
        page.Meals = Math.Min(Fullness, page.Meals + Math.Max(1, fills));
        Lift(page, taste switch { MealTaste.Liked => MoodMealLiked, MealTaste.Disliked => MoodMealDisliked, _ => MoodMeal });
        FriendshipRules.Change(p, friendship);
        return true;
    }

    /// <summary>A berry eaten: one pip, its taste by its strongest flavour, and <see cref="FriendshipEvent.Fed"/>'s friendship.</summary>
    public static bool FeedBerry(CareRecord page, Pokemon p, int day, ItemData berry) =>
        Feed(page, p, day, TasteOf(StrongestFlavour(berry), p.Nature), 1, FriendshipRules.ChangeFor(FriendshipEvent.Fed));

    /// <summary>
    /// A pat: the mood lifts every time, and the friendship rises by <see cref="FriendshipEvent.Petted"/> once a day.
    /// True if the friendship counted it.
    /// </summary>
    public static bool Pat(CareRecord page, Pokemon p, int day)
    {
        if (p.IsEgg) return false;
        Lift(page, MoodPat);
        if (page.PatDay == day) return false;
        page.PatDay = day;
        FriendshipRules.Apply(p, FriendshipEvent.Petted);
        return true;
    }

    /// <summary>Whether a cure would clear anything: a status but fainting, on one owed a cure.</summary>
    public static bool CanCure(CareRecord page, Pokemon p) =>
        !p.IsEgg && page.CureOwed && p.Status is not (StatusCondition.None or StatusCondition.Faint);

    /// <summary>
    /// A cure (the camp's as it opens, the close-up's medicine): its status goes, once for each battle it fought since its
    /// last cure, and the mood lifts. False if nothing was cured.
    /// </summary>
    public static bool Cure(CareRecord page, Pokemon p)
    {
        if (!CanCure(page, p)) return false;
        page.CureOwed = false;
        p.Status = StatusCondition.None;
        p.SleepTurns = 0;
        p.ToxicCounter = 0;
        Lift(page, MoodCured);
        return true;
    }

    // ------------------------------------------------------------------ the field and the days

    /// <summary>A walk cycle of the team (<see cref="FriendshipRules.WalkCycleSteps"/>): each mood settles, and a coat left soiled lowers it.</summary>
    public static void WalkCycle(CareBook book, IEnumerable<Pokemon> team)
    {
        foreach (var p in team)
        {
            if (p.IsEgg) continue;
            var page = book.Of(p);
            Settle(page, SettleWalk);
            if (page.Marks.Count > 0) Lift(page, MoodSoiledWalk);
        }
    }

    /// <summary>Days pass: the mood of every page but the team's settles toward calm, a day's worth for each.</summary>
    public static void DaysPass(CareBook book, int days, IEnumerable<Pokemon> team)
    {
        if (days <= 0) return;
        var onTeam = new HashSet<Guid>(team.Select(p => p.Id));
        int by = (int)Math.Min(MoodHighest - MoodLowest, (long)days * SettleDay);
        foreach (var page in book.Pages)
            if (!onTeam.Contains(page.Id)) Settle(page, by);
    }
}
