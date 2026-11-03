using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>
/// The Battle Royal (plan 14 · B13, Sun and Moon's; <c>docs/mechanics/rulings.md</c>, "The Battle Royal"). Four trainers,
/// each for themselves: the player and three others, one Pokémon out each. <b>Decision:</b> the battle keeps its two sides
/// (where the plan had <see cref="BattleSide"/> grow to four): the player's one place, and the three others on the other
/// side's three places, each with a trainer and a team of their own, as a double against two trainers has each place's.
/// Who is whose foe is asked of the places, never the sides (<see cref="AreFoes"/>, <see cref="PlacesOfFoes"/>): in a Royal every
/// other place is a foe's, so a move aimed at one may go to any of the three, a spread move hits all three, nobody has an
/// ally, and the trainer AI weighs every other place as a foe. The four are each called a camp: the player's is 0 and
/// the others' 1 to 3 in their places' order (<see cref="CampOf"/>).
/// <para>
/// The battle ends the moment one camp has nobody left able to fight (<see cref="EndTheRoyal"/>). Each camp scores a
/// point for every Pokémon it knocked out (the last foe to hit the fallen one with a move, <see cref="Volatiles.LastHitBy"/>)
/// and one for every Pokémon of its own still able to fight; they are placed by score, then by the share of their HP
/// left, then in camp order. First place is the player's victory; any other is <see cref="BattleResult.Placed"/>, which
/// pays nothing and whites nobody out. Nobody switches by choice and nothing is used from the bag (Sun and Moon's).
/// </para>
/// </summary>
public sealed partial class BattleCore
{
    /// <summary>A Battle Royal (<see cref="BattleFormat.Royal"/>).</summary>
    public bool IsRoyal => Format == BattleFormat.Royal;

    /// <summary>How many fight in a Battle Royal: the player and three others.</summary>
    public const int RoyalCamps = 4;

    // The knock-outs each camp has dealt so far
    private readonly int[] royalKnockOuts = new int[RoyalCamps];

    /// <summary>
    /// Whether the Pokémon in two places are foes: in a Battle Royal any two places that aren't the same; in every other
    /// battle two places of different sides. The one place it is decided.
    /// </summary>
    public bool AreFoes(Battler a, Battler b) => IsRoyal ? a.Place != b.Place : a.Side != b.Side;

    /// <summary>Every place of a battler's foes, active or not: in a Battle Royal every other place, else the other side's.</summary>
    public IEnumerable<Battler> PlacesOfFoes(Battler battler) =>
        IsRoyal ? AllBattlers.Where(b => b.Place != battler.Place) : SlotsOf(Other(battler.Side));

    /// <summary>A Battle Royal's camp of a place: the player's 0, the other side's places 1 to 3.</summary>
    public static int CampOf(Place place) => place.Side == BattleSide.Player ? 0 : 1 + place.Slot;

    /// <summary>The team a camp fights with.</summary>
    private IReadOnlyList<Pokemon> TeamOfCamp(int camp) =>
        camp == 0 ? PlayerParty.Members : EnemySlots[camp - 1].Roster?.Members ?? (IReadOnlyList<Pokemon>)Array.Empty<Pokemon>();

    /// <summary>Who a camp is, as the battle's lines name them: the player, or the trainer of that place.</summary>
    public string RoyalName(int camp) => camp == 0 ? playerName : EnemySlots[camp - 1].Trainer?.FullTitle ?? "";

    /// <summary>The knock-outs a camp has dealt.</summary>
    public int RoyalKnockOuts(int camp) => royalKnockOuts[camp];

    /// <summary>A camp's score: a point for each knock-out it dealt and one for each Pokémon of its own still able to fight.</summary>
    public int RoyalScore(int camp) => royalKnockOuts[camp] + TeamOfCamp(camp).Count(p => !p.IsFainted && !p.IsEgg);

    /// <summary>The share of a camp's HP left, in thousandths: what breaks a tie of scores.</summary>
    private int RoyalHpLeft(int camp)
    {
        var team = TeamOfCamp(camp).Where(p => !p.IsEgg).ToList();
        int most = team.Sum(p => p.MaxHP);
        return most == 0 ? 0 : team.Sum(p => Math.Max(0, p.CurrentHP)) * 1000 / most;
    }

    /// <summary>The four camps as they stand, first first: by score, then the share of HP left, then camp order. Empty outside a Battle Royal.</summary>
    public IReadOnlyList<int> RoyalStanding => !IsRoyal ? Array.Empty<int>()
        : Enumerable.Range(0, RoyalCamps).OrderByDescending(RoyalScore).ThenByDescending(RoyalHpLeft).ThenBy(c => c).ToList();

    /// <summary>Where the player stands, 1 to 4 (0 outside a Battle Royal).</summary>
    public int RoyalPlace => IsRoyal ? RoyalStanding.ToList().IndexOf(0) + 1 : 0;

    /// <summary>What a Battle Royal says as it opens; each trainer then sends out their own.</summary>
    private static string RoyalBegins() => Text.Of("battle.royalBegins");

    /// <summary>A fallen Pokémon counts for the camp of the last foe that hit it with a move, if it was another camp's.</summary>
    private void CreditTheKnockOut(Battler down)
    {
        if (!IsRoyal || down.Volatile.LastHitBy is not { } by) return;
        int camp = CampOf(by), fallen = CampOf(down.Place);
        if (camp != fallen) royalKnockOuts[camp]++;
    }

    /// <summary>A camp that has nobody left able to fight, on the field or on its team.</summary>
    private bool CampOut(int camp)
    {
        var place = camp == 0 ? PlayerSlots[0] : EnemySlots[camp - 1];
        return !place.IsActive && !TeamOfCamp(camp).Any(p => !p.IsFainted && !p.IsEgg);
    }

    /// <summary>
    /// The end of a Battle Royal, once any camp is out: who ran out, every camp's place and score, and the result, the
    /// player's victory for first place and <see cref="BattleResult.Placed"/> for any other.
    /// </summary>
    private void EndTheRoyal()
    {
        var out_ = Enumerable.Range(0, RoyalCamps).Where(CampOut).ToList();
        if (out_.Count == 0) return;
        Say(Text.Of("battle.royalOver", ("trainer", JoinNames(out_.Select(RoyalName)))));
        var standing = RoyalStanding;
        for (int i = 0; i < standing.Count; i++)
            Say(Text.Of("battle.royalPlaced", ("place", i + 1), ("trainer", RoyalName(standing[i])), ("score", RoyalScore(standing[i]))));
        Emit(new RoyalPlaced(standing.ToList(), standing.Select(RoyalScore).ToList()));
        if (standing[0] == 0)
        {
            Emit(new Won());
            Say(Text.Of("battle.royalWon", ("player", playerName)));
            End(BattleResult.PlayerVictory);
        }
        else End(BattleResult.Placed);
    }
}
