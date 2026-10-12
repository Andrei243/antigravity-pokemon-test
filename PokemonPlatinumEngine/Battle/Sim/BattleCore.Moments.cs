using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// The turn's moments (plan 25 · Y3): how a line of the rules comes to carry one. Nothing here changes a line, a roll
// or anything a turn does: a moment is one more event on a line that is said anyway, for whoever shows the battle.
public sealed partial class BattleCore
{
    /// <summary>A moment on the Pokémon in a place, for the line that tells of it: <c>Say(...).With(Moment(...))</c>.</summary>
    private static MomentShown Moment(Battler on, BattleMoment moment, Battler? from = null) => new(on.Place, moment, from?.Place);

    /// <summary>
    /// The next line said shows a moment on this Pokémon, and it is handed back: for a line said elsewhere, as
    /// <see cref="LoseHp(Battler, int, string)"/> and <see cref="RestoreHp"/> say theirs
    /// (<c>LoseHp(Marked(b, BattleMoment.Poisoned), …)</c>). It rides as what a line names does
    /// (<see cref="ItemOf"/>): before the line's own events, so before its <see cref="HpChanged"/>, and forgotten
    /// with a line that goes unsaid.
    /// </summary>
    private Battler Marked(Battler on, BattleMoment moment, Battler? from = null)
    {
        named.Add(Moment(on, moment, from));
        return on;
    }

    /// <summary>
    /// The name of a held item on the line that tells of it going off (<see cref="IBattleContext.ItemAtWork"/>): the
    /// line carries the item's <see cref="ItemShown"/>, as with <see cref="ItemOf"/>, and the held item's moment,
    /// where the original plays <c>BATTLE_ANIMATION_HELD_ITEM</c>.
    /// </summary>
    public string ItemAtWork(Battler holder, ItemData? item = null)
    {
        string name = ItemOf(holder, item);
        if (name.Length > 0 && holder.Pokemon != null) named.Add(Moment(holder, BattleMoment.HeldItem));
        return name;
    }

    /// <summary>
    /// A status condition a Pokémon's own item gives it (a Toxic Orb, a Flame Orb; <c>subscript_badly_poison</c> and
    /// <c>subscript_burn</c> with a held item's side effect, which play the held item's animation first): the line
    /// that tells of the condition shows the item going off. A condition that doesn't come says nothing, as before.
    /// </summary>
    public bool TryInflictStatusByItem(Battler holder, StatusCondition status)
    {
        int before = log.Count;
        bool given = TryInflictStatus(holder, status, null);
        if (!given) return false;
        var told = log.Skip(before).OfType<Said>().FirstOrDefault(s => s.Shows.Any(e => e is StatusChanged changed && changed.Place == holder.Place));
        told?.Shows.Insert(0, Moment(holder, BattleMoment.HeldItem));
        return true;
    }

    /// <summary>The moment of a binding move's hurt, by the move (<c>subscript_bind_effect</c>): Bind's for a move it has none of its own for.</summary>
    internal static BattleMoment BindMoment(string move) => move switch
    {
        "Wrap" => BattleMoment.Wrap,
        "Fire Spin" => BattleMoment.FireSpin,
        "Magma Storm" => BattleMoment.MagmaStorm,
        "Clamp" => BattleMoment.Clamp,
        "Whirlpool" => BattleMoment.Whirlpool,
        "Sand Tomb" => BattleMoment.SandTomb,
        _ => BattleMoment.Bind
    };
}
