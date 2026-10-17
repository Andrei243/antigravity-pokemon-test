using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

// Dynamax on screen (plan 06 · R22): the move menu's item button arms it where neither a Mega Evolution nor a Z-Move
// can be had, the screen's Pokémon takes its raised HP and its Gigantamax form from the events, and its moves are
// named as Max Moves while it lasts (MaxMoveAt). The looks are plan 25 · Y15's: the growth and the red clouds
// (BattleAnimator.Dynamax, MoveFx.Giants.cs), each Max Move's set piece (MoveFx.MaxMoves.cs) and the button.
public partial class BattleEngine
{
    /// <summary>
    /// The move menu's Dynamax is armed (plan 06 · R22): the move chosen next goes with it, and is used as its Max
    /// Move. Toggled with the item button in the move menu where no Mega Evolution or Z-Move can be had
    /// (<see cref="ToggleLaterMechanic"/>).
    /// </summary>
    public bool DynamaxArmed { get; private set; }

    /// <summary>The Pokémon whose moves are being chosen can Dynamax, and no other of the player's has been chosen to this turn.</summary>
    public bool CanDynamaxNow =>
        core.Request is ActionRequest && !playingTheLog && MenuBattler.Pokemon != null && !choices.Values.Any(c => c.Dynamax) && core.CanDynamax(InCore(MenuBattler));

    /// <summary>The Pokémon whose moves are being chosen is Dynamaxed.</summary>
    public bool MenuDynamaxed => MenuBattler.Pokemon != null && BattleCore.IsDynamaxed(InCore(MenuBattler));

    /// <summary>The Max Move the move in this position is (Dynamaxed) or would be (Dynamax armed): what the move menu may name in its place (plan 25 · Y15).</summary>
    public MoveData? MaxMoveAt(int move) => MenuDynamaxed || DynamaxArmed ? core.MaxMoveFor(InCore(MenuBattler), move) : null;

    /// <summary>Arms or disarms the Dynamax for the move to be chosen; false (and disarmed) where it can't be had.</summary>
    public bool ToggleDynamax()
    {
        if (!CanDynamaxNow)
        {
            DynamaxArmed = false;
            // A Terastallization where no Dynamax can be had either (plan 06 · R23)
            return ToggleTera();
        }
        DynamaxArmed = !DynamaxArmed;
        return true;
    }

    /// <summary>A move chosen with the Dynamax armed goes with it.</summary>
    private BattleChoice WithDynamaxArmed(BattleChoice choice)
    {
        bool armed = DynamaxArmed;
        DynamaxArmed = false;
        if (!armed || choice.Kind != ChoiceKind.Fight || choice.Move < 0 || choice.Mega || choice.ZMove || choice.UltraBurst || !CanDynamaxNow) return choice;
        return choice with { Dynamax = true };
    }

    /// <summary>
    /// The screen's Pokémon takes the HP a Dynamax raises or its end gives back, and its bar keeps its share: the
    /// numbers the box shows move with it at once, never easing up like a heal or showing more than the most (plan 25 · Y15).
    /// </summary>
    private void TakeRaisedHp(Place place, Pokemon? shown, int maxHp, int hp)
    {
        if (shown == null) return;
        int before = Math.Max(1, shown.MaxHP);
        shown.TakeHp(maxHp, hp);
        var v = Anim[place.Side, place.Slot];
        if (v.Shown == shown) v.DisplayedHp = Math.Min(v.DisplayedHp * maxHp / before, maxHp);
    }

    /// <summary>
    /// A Dynamax or its end on screen (plan 25 · Y15): its sound and its cue; the screen's Pokémon takes the HP and
    /// the most HP the event says at once (the box shows the raised numbers from the line on), and the form (a
    /// Gigantamax's, or back from one) inside the cue's light; at once with the scene off, where nothing plays.
    /// </summary>
    private void ShowDynamax(BattleEvent happening)
    {
        switch (happening)
        {
            case Dynamaxed grown:
            {
                Sound(grown.Gigantamax ? "gigantamax" : "dynamax", grown.Place.Side);
                var big = At(grown.Place).Pokemon;
                TakeRaisedHp(grown.Place, big, grown.MaxHp, grown.Hp);
                Anim.Dynamax(grown.Place.Side, grown.Place.Slot, grown.Gigantamax, played: Scene);
                if (big == null) break;
                if (Scene && big.Form != grown.Form) After(BattleAnimator.DynamaxSwapTime, () => big.Form = grown.Form);
                else big.Form = grown.Form;
                break;
            }
            case DynamaxEnded ended:
            {
                var small = At(ended.Place).Pokemon;
                TakeRaisedHp(ended.Place, small, ended.MaxHp, ended.Hp);
                var v = Anim[ended.Place.Side, ended.Place.Slot];
                bool standing = v.Dynamaxed && v.Present && v.FaintAge < 0f && v.RecallAge < 0f;
                if (standing) Sound("dynamax_end", ended.Place.Side);
                Anim.Undynamax(ended.Place.Side, ended.Place.Slot, played: Scene);
                if (small == null) break;
                if (Scene && standing && small.Form != ended.Form) After(BattleAnimator.DynamaxEndSwapTime, () => small.Form = ended.Form);
                else small.Form = ended.Form;
                break;
            }
        }
    }
}
