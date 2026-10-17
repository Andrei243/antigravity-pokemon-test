using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

// Dynamax on screen (plan 06 · R22): the move menu's item button arms it where neither a Mega Evolution nor a Z-Move
// can be had, the screen's Pokémon takes its raised HP and its Gigantamax form from the events, and its moves are
// named as Max Moves while it lasts (MaxMoveAt). The looks (the growth, the red clouds, each Max Move's effect, the
// button) are plan 25 · Y15's; until then the change plays Transform's cue as Mega Evolution's stand-in does.
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
    /// A Dynamax or its end on screen: the screen's Pokémon takes the HP, the most HP and the form the event says,
    /// and a Dynamax plays Transform's cue on itself until plan 25 · Y15 gives it a look of its own.
    /// </summary>
    private void ShowDynamax(BattleEvent happening)
    {
        switch (happening)
        {
            case Dynamaxed grown:
                if (At(grown.Place).Pokemon is { } big)
                {
                    big.Form = grown.Form;
                    big.TakeHp(grown.MaxHp, grown.Hp);
                }
                if (Scene)
                    Anim.Cue(new EffectCue
                    {
                        Kind = CueKind.Move,
                        Move = "Transform",
                        Type = PokemonType.Normal,
                        Category = MoveCategory.Status,
                        FromSide = grown.Place.Side,
                        FromSlot = grown.Place.Slot,
                        ToSide = grown.Place.Side,
                        ToSlot = grown.Place.Slot
                    });
                break;
            case DynamaxEnded ended:
                if (At(ended.Place).Pokemon is { } small)
                {
                    small.Form = ended.Form;
                    small.TakeHp(ended.MaxHp, ended.Hp);
                }
                break;
        }
    }
}
