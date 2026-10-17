using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

// Z-Moves and Ultra Burst on screen (plan 06 · R21): the move menu's item button arms them, as it arms a Mega
// Evolution, and an Ultra Burst's form is taken from its event. Their looks (the Z-Power, the Z-Move's own effect, the
// light of the burst) are plan 25 · Y14's; until then a Z-Move is shown as the move it is and the burst as a Mega
// Evolution's stand-in.
public partial class BattleEngine
{
    /// <summary>
    /// The move menu's Z-Power is armed (plan 06 · R21): the move chosen next is used as its Z-Move where it has one,
    /// and a Necrozma that can Ultra Bursts first. Toggled with the item button in the move menu where no Mega
    /// Evolution can be had (<see cref="ToggleLaterMechanic"/>).
    /// </summary>
    public bool ZArmed { get; private set; }

    /// <summary>The Pokémon whose moves are being chosen has a Z-Move among its moves, or can Ultra Burst, and no other of the player's has been chosen to this turn.</summary>
    public bool CanZMoveNow
    {
        get
        {
            if (core.Request is not ActionRequest || playingTheLog || MenuBattler.Pokemon is not { } p) return false;
            var mine = InCore(MenuBattler);
            bool z = !choices.Values.Any(c => c.ZMove) && Enumerable.Range(0, p.Moves.Count).Any(i => core.CanUseZMove(mine, i));
            bool burst = !choices.Values.Any(c => c.UltraBurst) && core.CanUltraBurst(mine);
            return z || burst;
        }
    }

    /// <summary>The move in this position would be used as a Z-Move if the Z-Power were armed: what the move menu may name in its place (plan 25 · Y14).</summary>
    public MoveData? ZMoveAt(int move)
    {
        if (MenuBattler.Pokemon is not { } p || move < 0 || move >= p.Moves.Count || !core.CanUseZMove(InCore(MenuBattler), move)) return null;
        return BattleCore.ZMoveOf(p, p.Moves[move]);
    }

    /// <summary>The item button in the move menu: a Mega Evolution where one can be had (R20), the Z-Power otherwise; false (and disarmed) where neither can.</summary>
    public bool ToggleLaterMechanic()
    {
        if (CanMegaEvolveNow) return ToggleMega();
        if (!CanZMoveNow)
        {
            ZArmed = false;
            // A Dynamax where no Z-Move can be had either (plan 06 · R22)
            return ToggleDynamax();
        }
        ZArmed = !ZArmed;
        return true;
    }

    /// <summary>A move chosen with the Z-Power armed: its Z-Move where it has one, an Ultra Burst where one can be had.</summary>
    private BattleChoice WithZArmed(BattleChoice choice)
    {
        bool armed = ZArmed;
        ZArmed = false;
        if (!armed || choice.Kind != ChoiceKind.Fight || !CanZMoveNow) return choice;
        var mine = InCore(MenuBattler);
        if (!choices.Values.Any(c => c.UltraBurst) && core.CanUltraBurst(mine)) choice = choice with { UltraBurst = true };
        if (choice.Move >= 0 && !choices.Values.Any(c => c.ZMove) && core.CanUseZMove(mine, choice.Move)) choice = choice with { ZMove = true };
        return choice;
    }

    /// <summary>The screen's Pokémon takes the form the rules' copy has taken (an Ultra Burst), with Transform's cue as its stand-in.</summary>
    private void TakeTheForm(Place place)
    {
        if (core.At(place).Pokemon is { } theirs && At(place).Pokemon is { } shown)
        {
            shown.Species = theirs.Species;
            shown.Form = theirs.Form;
            shown.AbilityName = theirs.AbilityName;
            (shown.Attack, shown.Defense, shown.SpAttack, shown.SpDefense, shown.Speed) = (theirs.Attack, theirs.Defense, theirs.SpAttack, theirs.SpDefense, theirs.Speed);
        }
        if (Scene)
            Anim.Cue(new EffectCue
            {
                Kind = CueKind.Move,
                Move = "Transform",
                Type = PokemonType.Normal,
                Category = MoveCategory.Status,
                FromSide = place.Side,
                FromSlot = place.Slot,
                ToSide = place.Side,
                ToSlot = place.Slot
            });
    }
}
