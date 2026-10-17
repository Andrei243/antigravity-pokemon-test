using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

// Terastallization on screen (plan 06 · R23): the move menu's item button arms it where no Mega Evolution, Z-Move or
// Dynamax can be had, and the screen's Pokémon takes its Tera Type (and a Terapagos its Stellar Form) from the event,
// so that its types, the move cards' hints and the information panel go by it. The looks (the crystal, the crown, the
// button, the HP box's crown) are plan 25 · Y15's (BattleAnimator.Terastallize, MoveFx.Giants.cs, ModernUi.Later.cs).
public partial class BattleEngine
{
    /// <summary>
    /// The move menu's Terastallization is armed (plan 06 · R23): the move chosen next goes with it. Toggled with the
    /// item button in the move menu where no other later mechanic can be had (<see cref="ToggleLaterMechanic"/>).
    /// </summary>
    public bool TeraArmed { get; private set; }

    /// <summary>The Pokémon whose moves are being chosen can Terastallize, and no other of the player's has been chosen to this turn.</summary>
    public bool CanTerastallizeNow =>
        core.Request is ActionRequest && !playingTheLog && MenuBattler.Pokemon != null && !choices.Values.Any(c => c.Terastallize) && core.CanTerastallize(InCore(MenuBattler));

    /// <summary>The Tera Type the Pokémon whose moves are being chosen has taken, or would take: what the move menu may show.</summary>
    public TeraType? MenuTeraType => MenuBattler.Pokemon is { } p ? p.Terastallized ?? (TeraArmed ? p.TeraType : null) : null;

    /// <summary>The screen's Pokémon that Terastallized, whose Tera Types the battle's end takes off.</summary>
    private readonly List<Pokemon> teraShown = new();

    /// <summary>Arms or disarms the Terastallization for the move to be chosen; false (and disarmed) where it can't be had.</summary>
    public bool ToggleTera()
    {
        if (!CanTerastallizeNow)
        {
            TeraArmed = false;
            return false;
        }
        TeraArmed = !TeraArmed;
        return true;
    }

    /// <summary>A move chosen with the Terastallization armed goes with it.</summary>
    private BattleChoice WithTeraArmed(BattleChoice choice)
    {
        bool armed = TeraArmed;
        TeraArmed = false;
        if (!armed || choice.Kind != ChoiceKind.Fight || choice.Mega || choice.ZMove || choice.UltraBurst || choice.Dynamax || !CanTerastallizeNow) return choice;
        return choice with { Terastallize = true };
    }

    /// <summary>
    /// A Terastallization on screen, and the battle's end: the screen's Pokémon takes the Tera Type (and the form and
    /// HP) the event says, and plays Transform's cue on itself until plan 25 gives it a look of its own; at the end
    /// every Tera Type is taken off again.
    /// </summary>
    private void ShowTera(BattleEvent happening)
    {
        switch (happening)
        {
            case Terastallized tera:
            {
                // Its sound and its crystal (plan 25 · Y15): the Tera Type is taken at once (the HP box's crown, the
                // cards' hints), a Terapagos's Stellar Form inside the light, as the shell shatters
                var type = TeraType.Parse(tera.TeraType) ?? TeraType.Of(PokemonType.Normal);
                Sound(type.IsStellar ? "tera_stellar" : "terastallize", tera.Place.Side);
                if (At(tera.Place).Pokemon is { } shown)
                {
                    shown.Terastallized = type;
                    teraShown.Add(shown);
                    if (tera.Form != null)
                    {
                        TakeRaisedHp(tera.Place, shown, tera.MaxHp, tera.Hp);
                        if (Scene && shown.Form != tera.Form) After(BattleAnimator.TeraSwapTime, () => shown.Form = tera.Form);
                        else shown.Form = tera.Form;
                    }
                }
                if (Scene) Anim.Terastallize(tera.Place.Side, tera.Place.Slot, type);
                break;
            }
            case Ended:
                foreach (var p in teraShown) p.Terastallized = null;
                teraShown.Clear();
                break;
        }
    }
}
