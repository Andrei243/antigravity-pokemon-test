using System;

namespace PokemonPlatinumEngine.Models;

/// <summary>What changes a Pokémon's friendship (Platinum's list: pret/pokeplatinum, <c>enum FriendshipEvents</c>).</summary>
public enum FriendshipEvent
{
    LevelUp,
    /// <summary>Winning against a Gym Leader, the Elite Four or the Champion.</summary>
    BeatLeader,
    /// <summary>Learning a move from a TM or HM.</summary>
    LearnMachine,
    /// <summary>Every 128 steps, with a coin flip for each Pokémon in the party.</summary>
    WalkCycle,
    Faint,
    /// <summary>Fainting to a foe thirty or more levels above it.</summary>
    FaintToStronger,
    /// <summary>Coming through poison in the field with one hit point.</summary>
    PoisonSurvive,
    ContestWin
}

/// <summary>
/// Friendship as Platinum counts it (pret/pokeplatinum, <c>Pokemon_UpdateFriendship</c>): 0 to 255, starting at the
/// species' base value, and worth less to gain the higher it already is. Evolutions that ask for friendship happen
/// from <see cref="EvolveAt"/>. The bonus for being in the place the Pokémon was met waits for met locations.
/// </summary>
public static class FriendshipRules
{
    public const int Max = 255;

    /// <summary>The friendship an evolution needs.</summary>
    public const int EvolveAt = 220;

    /// <summary>Steps between two walking bonuses.</summary>
    public const int WalkCycleSteps = 128;

    /// <summary>The name items.json gives the Soothe Bell's hold effect.</summary>
    public const string SootheEffect = "FriendshipUp";

    public const string LuxuryBall = "Luxury Ball";

    /// <summary>The change for friendship below 100, below 200, and from 200 up.</summary>
    public static (int Low, int Mid, int High) ChangeFor(FriendshipEvent e) => e switch
    {
        FriendshipEvent.LevelUp => (5, 3, 2),
        FriendshipEvent.BeatLeader => (3, 2, 1),
        FriendshipEvent.LearnMachine => (1, 1, 0),
        FriendshipEvent.WalkCycle => (1, 1, 1),
        FriendshipEvent.Faint => (-1, -1, -1),
        FriendshipEvent.FaintToStronger => (-5, -5, -10),
        FriendshipEvent.PoisonSurvive => (-5, -5, -10),
        FriendshipEvent.ContestWin => (3, 2, 1),
        _ => (0, 0, 0)
    };

    /// <param name="rng">Flips the coin of a walk cycle; left out, the walk always counts.</param>
    public static void Apply(Pokemon p, FriendshipEvent e, Random? rng = null)
    {
        if (e == FriendshipEvent.WalkCycle && rng != null && rng.Next(2) == 0) return;
        Change(p, ChangeFor(e));
    }

    /// <summary>
    /// Applies a change given for the three bands of friendship (an item's own numbers work the same way). Gains
    /// are one larger for a Pokémon caught in a Luxury Ball and half as large again for one holding a Soothe Bell.
    /// </summary>
    public static void Change(Pokemon p, (int Low, int Mid, int High) change)
    {
        int delta = p.Friendship >= 200 ? change.High : p.Friendship >= 100 ? change.Mid : change.Low;
        if (delta > 0)
        {
            if (p.Ball == LuxuryBall) delta++;
            if (p.HeldItem?.HoldEffect == SootheEffect) delta = delta * 150 / 100;
        }
        p.Friendship = Math.Clamp(p.Friendship + delta, 0, Max);
    }

    private static int Band(Pokemon p, (int Low, int Mid, int High) change) =>
        p.Friendship >= 200 ? change.High : p.Friendship >= 100 ? change.Mid : change.Low;

    /// <summary>
    /// An item's own friendship (a vitamin, an EV berry, a Rare Candy; <c>UpdatePokemonFriendship</c>): the same
    /// bands, but here a Soothe Bell's half again comes before the Luxury Ball's one, the other way round from the
    /// events' (<c>Pokemon_UpdateFriendship</c>).
    /// </summary>
    public static void ChangeByItem(Pokemon p, (int Low, int Mid, int High) change)
    {
        int delta = Band(p, change);
        if (delta > 0)
        {
            if (p.HeldItem?.HoldEffect == SootheEffect) delta = delta * 150 / 100;
            if (p.Ball == LuxuryBall) delta++;
        }
        p.Friendship = Math.Clamp(p.Friendship + delta, 0, Max);
    }

    /// <summary>Whether an item's friendship would raise it (<c>CheckFriendshipItemEffect</c>): not at the most, and only by a gain.</summary>
    public static bool WouldRaise(Pokemon p, (int Low, int Mid, int High) change) => p.Friendship < Max && Band(p, change) > 0;
}
