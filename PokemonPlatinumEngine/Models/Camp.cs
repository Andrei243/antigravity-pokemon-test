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
    NoBerry
}

/// <summary>
/// A visit to the camp (plan 10 · F6; docs/mechanics/rulings.md, "The camp"), GPU-free: the team is petted and fed,
/// each Pokémon once a visit, which raises its friendship by the table's <see cref="FriendshipEvent.Petted"/> and
/// <see cref="FriendshipEvent.Fed"/> (with the Luxury Ball's and the Soothe Bell's bonus, as every event has), and a
/// visit after a battle cures the team's status conditions as Refresh does, once for each battle
/// (<see cref="CampCare"/>). Its meals are the bag's berries (decision 4: no cooking). Nothing here draws, and
/// nothing is drawn by chance.
/// </summary>
public sealed class CampVisit
{
    private readonly HashSet<Pokemon> petted = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<Pokemon> fed = new(ReferenceEqualityComparer.Instance);

    public bool WasPetted(Pokemon p) => petted.Contains(p);
    public bool WasFed(Pokemon p) => fed.Contains(p);

    /// <summary>Why it can't be petted, or <see cref="CampRefusal.None"/>.</summary>
    public CampRefusal WhyNotPet(Pokemon p) => p.IsEgg ? CampRefusal.Egg : petted.Contains(p) ? CampRefusal.AlreadyThisVisit : CampRefusal.None;

    /// <summary>Pets it: its friendship rises once a visit. False if it couldn't be.</summary>
    public bool Pet(Pokemon p)
    {
        if (WhyNotPet(p) != CampRefusal.None) return false;
        petted.Add(p);
        FriendshipRules.Apply(p, FriendshipEvent.Petted);
        return true;
    }

    /// <summary>Why it can't be fed this berry from this bag, or <see cref="CampRefusal.None"/>.</summary>
    public CampRefusal WhyNotFeed(Pokemon p, ItemData? berry, Inventory bag) =>
        p.IsEgg ? CampRefusal.Egg
        : fed.Contains(p) ? CampRefusal.AlreadyThisVisit
        : berry?.Berry == null || bag.GetQuantity(berry) == 0 ? CampRefusal.NoBerry
        : CampRefusal.None;

    /// <summary>Feeds it a berry out of the bag, which is eaten: its friendship rises once a visit. False if it couldn't be.</summary>
    public bool Feed(Pokemon p, ItemData berry, Inventory bag)
    {
        if (WhyNotFeed(p, berry, bag) != CampRefusal.None) return false;
        bag.RemoveItem(berry);
        fed.Add(p);
        FriendshipRules.Apply(p, FriendshipEvent.Fed);
        return true;
    }
}

/// <summary>
/// What the camp remembers between visits (plan 10 · F6): whether a battle has been fought since it last cured the
/// team. A visit cures every status condition (but fainting) of the team once for each battle, as Refresh does; a
/// second visit with no battle between cures nothing. Not saved: a game loaded counts as after a battle.
/// </summary>
public sealed class CampCare
{
    /// <summary>A battle has ended since the last cure.</summary>
    public bool BattleSinceCure { get; private set; } = true;

    /// <summary>A battle has ended.</summary>
    public void BattleEnded() => BattleSinceCure = true;

    /// <summary>A visit's cure: the team's conditions go, once for each battle. Returns how many were cured.</summary>
    public int Cure(Party party)
    {
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
