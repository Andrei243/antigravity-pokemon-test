using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The two bonuses an event can give a team (plan 15, the bonuses; docs/mechanics/rulings.md, "The Pokéathlon"): a
/// <see cref="Steady"/> one for playing it without the event's slip (no trip, no fall, no ball taken) and a
/// <see cref="Star"/> one for its feat (a fast run, a stack of blocks, a run of lamps). Our own, on the shape of
/// HeartGold's bonuses for what a Pokémon did in an event, whose numbers are in its assembly only.
/// </summary>
public enum AthlonBonusKind { Steady, Star }

/// <summary>A bonus won in an event: its kind and the points it adds to the course's total.</summary>
public readonly record struct AthlonBonus(AthlonBonusKind Kind, int Points);

/// <summary>
/// The bonuses (plan 15, the bonuses): read from an event once it is over, from what its own state counted, so they
/// change nothing of how it is played and draw nothing from its generator. Each is <see cref="Points"/>; an event gives a
/// team none, one or both. The words are <c>athlon.bonus.&lt;event&gt;.steady</c> and <c>.star</c>.
/// </summary>
public static class AthlonBonuses
{
    /// <summary>What a bonus adds to the course's total.</summary>
    public const int Points = 2;

    /// <summary>The bonuses a team won in an event that is over (or as it stands, for one still under way).</summary>
    public static IReadOnlyList<AthlonBonus> Of(AthlonEvent ev, int team)
    {
        var (steady, star) = Earned(ev, team);
        var won = new List<AthlonBonus>(2);
        if (steady) won.Add(new AthlonBonus(AthlonBonusKind.Steady, Points));
        if (star) won.Add(new AthlonBonus(AthlonBonusKind.Star, Points));
        return won;
    }

    /// <summary>The points a team's bonuses in an event come to.</summary>
    public static int Sum(IEnumerable<AthlonBonus> bonuses) => bonuses.Sum(b => b.Points);

    /// <summary>Whether a team earned each of the two, event by event (the rulings' table).</summary>
    /// <remarks>
    /// The lines are set so that an even team (every stat 3, the autopilot) wins each about one time in three to one in
    /// five (<c>AthlonBonusTests</c> holds that each is won sometimes and not always).
    /// </remarks>
    public static (bool Steady, bool Star) Earned(AthlonEvent ev, int team) => ev switch
    {
        // At most one hurdle met on the ground and through; none met and through in 26 seconds
        HurdleDash h => (h.Runners[team].Finished && h.Runners[team].Trips <= 1,
            h.Runners[team].Finished && h.Runners[team].Trips == 0 && h.Runners[team].FinishedAt <= 26 * AthlonEvent.FramesPerSecond),
        // No more than two hits off the beat; twenty-nine blocks broken
        BlockSmash b => (b.Smashers[team].Hits > 0 && b.Smashers[team].Hits - b.Smashers[team].CleanHits <= 2, b.Smashers[team].Broken >= 29),
        // Never off the platform; two knock-offs
        RingDrop r => (r.Pushers[team].Falls == 0, r.Pushers[team].Knocks >= 2),
        // No more than 35 bumps; eighteen pennants
        PennantCapture p => (p.Chasers[team].Bumps <= 35, p.Chasers[team].Flags >= 18),
        // Hit no more than fifteen times; 24 hits
        SnowThrow s => (s.Throwers[team].Taken <= 15, s.Throwers[team].Hits >= 24),
        // No more than four discs let fall; sixteen caught in the air
        DiscCatch d => (d.Catchers[team].Missed <= 4, d.Catchers[team].CaughtInAir >= 16),
        // Nine presses in ten in time; 155 lamps lit
        LampJump l => (l.Jumpers[team].Bounces > 0 && l.Jumpers[team].Good * 10 >= l.Jumpers[team].Bounces * 9, l.Jumpers[team].Lit >= 155),
        // No more than six hand-overs and never run past the line of breath; 6,800 units run (eleven laps and a third)
        RelayRun rr => (rr.Relays[team].Handovers <= 6 && rr.Relays[team].Breathless == 0, rr.Relays[team].Distance >= 6800f),
        // In the circle 24 seconds of the forty; a second and a half in it alone
        CirclePush c => (c.Pushers[team].Held >= 24 * AthlonEvent.FramesPerSecond, c.Pushers[team].Alone >= 45),
        // A goal and none into its own; three goals
        GoalRoll g => (g.Players[team].Goals > 0 && g.Players[team].OwnGoals == 0, g.Players[team].Goals >= 3),
        _ => (false, false)
    };
}
