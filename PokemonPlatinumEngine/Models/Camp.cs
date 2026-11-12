using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>Why the camp would not pet or feed a Pokémon.</summary>
public enum CampRefusal
{
    None,
    /// <summary>An Egg is neither petted nor fed.</summary>
    Egg,
    /// <summary>It has had its pat, or its meal, this visit.</summary>
    AlreadyThisVisit,
    /// <summary>What was offered is no berry, or the bag has none of it.</summary>
    NoBerry,
    /// <summary>It has had its five meals today (plan 35 · BN1, the care book's fullness).</summary>
    Full
}

/// <summary>
/// A visit to the camp (plan 10 · F6; docs/mechanics/rulings.md, "The camp"), GPU-free: the team is petted and fed,
/// each Pokémon once a visit, which raises its friendship by the table's <see cref="FriendshipEvent.Petted"/> and
/// <see cref="FriendshipEvent.Fed"/> (with the Luxury Ball's and the Soothe Bell's bonus, as every event has), and a
/// visit after a battle cures the team's status conditions as Refresh does, once for each battle
/// (<see cref="CampCare"/>). Its meals are the bag's berries, and with the crafting kit the pot's dishes (plan 39 · CR6,
/// which took plan 10 · F8's cooking over: <see cref="Meals"/>). Nothing here draws, and nothing is drawn by chance.
/// </summary>
public sealed class CampVisit
{
    private readonly HashSet<Pokemon> petted = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<Pokemon> fed = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// The care book the visit writes in, and the day it is (plan 35 · BN1): with one, a pat counts for friendship once a
    /// day (<see cref="CareRules.Pat"/>) and a Pokémon eats until it is full for the day (<see cref="CareRules.Fullness"/>),
    /// however many visits that takes; without one, the visit's own once-a-visit rule of plan 10 · F6.
    /// </summary>
    public CareBook? Book { get; }
    public int Day { get; }

    public CampVisit() { }

    public CampVisit(CareBook? book, int day)
    {
        Book = book;
        Day = day;
    }

    public bool WasPetted(Pokemon p) => petted.Contains(p);
    public bool WasFed(Pokemon p) => fed.Contains(p);

    /// <summary>Why it can't be petted, or <see cref="CampRefusal.None"/>.</summary>
    public CampRefusal WhyNotPet(Pokemon p) => p.IsEgg ? CampRefusal.Egg : petted.Contains(p) ? CampRefusal.AlreadyThisVisit : CampRefusal.None;

    /// <summary>Pets it: its friendship rises once a visit. False if it couldn't be.</summary>
    public bool Pet(Pokemon p)
    {
        if (WhyNotPet(p) != CampRefusal.None) return false;
        petted.Add(p);
        if (Book != null) CareRules.Pat(Book.Of(p), p, Day);
        else FriendshipRules.Apply(p, FriendshipEvent.Petted);
        return true;
    }

    /// <summary>Why it can't be fed this berry from this bag, or <see cref="CampRefusal.None"/>.</summary>
    public CampRefusal WhyNotFeed(Pokemon p, ItemData? berry, Inventory bag) =>
        p.IsEgg ? CampRefusal.Egg
        : Book != null ? (!CareRules.WouldEat(Book.Of(p), p, Day) ? CampRefusal.Full
            : berry?.Berry == null || bag.GetQuantity(berry) == 0 ? CampRefusal.NoBerry : CampRefusal.None)
        : fed.Contains(p) ? CampRefusal.AlreadyThisVisit
        : berry?.Berry == null || bag.GetQuantity(berry) == 0 ? CampRefusal.NoBerry
        : CampRefusal.None;

    /// <summary>Feeds it a berry out of the bag, which is eaten: its friendship rises once a visit. False if it couldn't be.</summary>
    public bool Feed(Pokemon p, ItemData berry, Inventory bag)
    {
        if (WhyNotFeed(p, berry, bag) != CampRefusal.None) return false;
        bag.RemoveItem(berry);
        fed.Add(p);
        if (Book != null) CareRules.FeedBerry(Book.Of(p), p, Day, berry);
        else FriendshipRules.Apply(p, FriendshipEvent.Fed);
        return true;
    }

    /// <summary>
    /// It eats from the camp's pot (plan 39 · CR6): its friendship rises as a berry's would, once a visit, shared with the
    /// berry (a Pokémon fed a berry this visit eats and is not befriended again). True if it rose.
    /// </summary>
    public bool Eat(Pokemon p)
    {
        if (p.IsEgg) return false;
        // With the care book (plan 35 · BN1) a dish fills a pip of the day as a berry does, and a full Pokémon is not befriended
        if (Book != null)
        {
            fed.Add(p);
            return CareRules.Feed(Book.Of(p), p, Day, MealTaste.Plain, 1, FriendshipRules.ChangeFor(FriendshipEvent.Fed));
        }
        if (!fed.Add(p)) return false;
        FriendshipRules.Apply(p, FriendshipEvent.Fed);
        return true;
    }
}

/// <summary>
/// What the camp remembers between visits (plan 10 · F6): whether a battle has been fought since it last cured the
/// team. A visit cures every status condition (but fainting) of the team once for each battle, as Refresh does; a
/// second visit with no battle between cures nothing. Not saved: a game loaded counts as after a battle. Over the care
/// book (plan 35 · BN1) the cure is each Pokémon's own, once for each battle it fought (<see cref="CareRules.Cure"/>),
/// and the visits it opens write in the book on the day <see cref="Today"/> says.
/// </summary>
public sealed class CampCare
{
    public CampCare() { }

    public CampCare(CareBook book, Func<int> today)
    {
        Book = book;
        Today = today;
    }

    /// <summary>The care book the camp is a view over, or null for plan 10 · F6's camp alone.</summary>
    public CareBook? Book { get; }

    /// <summary>What day it is (the game's clock, counted as <see cref="CareRules.DayOf"/> counts it).</summary>
    public Func<int>? Today { get; }

    /// <summary>A new visit: over the book, on today's day.</summary>
    public CampVisit NewVisit() => Book != null ? new CampVisit(Book, Today?.Invoke() ?? 0) : new CampVisit();

    /// <summary>A battle has ended since the last cure.</summary>
    public bool BattleSinceCure { get; private set; } = true;

    /// <summary>A battle has ended.</summary>
    public void BattleEnded() => BattleSinceCure = true;

    /// <summary>A visit's cure: the team's conditions go, once for each battle. Returns how many were cured.</summary>
    public int Cure(Party party)
    {
        if (Book != null)
        {
            int healed = 0;
            foreach (var p in party.Members)
                if (CareRules.Cure(Book.Of(p), p)) healed++;
            return healed;
        }
        if (!BattleSinceCure) return 0;
        BattleSinceCure = false;
        int cured = 0;
        foreach (var p in party.Members)
        {
            if (p.IsEgg || p.Status is StatusCondition.None or StatusCondition.Faint) continue;
            p.Status = StatusCondition.None;
            p.SleepTurns = 0;
            p.ToxicCounter = 0;
            cured++;
        }
        return cured;
    }
}
