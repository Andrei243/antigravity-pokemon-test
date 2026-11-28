using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

// The winter masks on screen (plan 46 · CP13): the move menu's MASK button (the PageLeft action, beside the TALE button's
// PageRight) turns through the masks the player could put on and back to none; a move chosen with one armed puts it on.
// The screen's Pokémon takes the mask (its second type and its ability) from the log's MaskWorn, never from the rules,
// which are a turn ahead, and gives the ability back as the battle ends. The mask drawn on the body is CP14's.
public partial class BattleEngine
{
    /// <summary>The mask armed for the move to be chosen next, or null.</summary>
    public string? MaskArmed { get; private set; }

    /// <summary>The screen's Pokémon that put a mask on, and the ability each had before, which the battle's end gives back.</summary>
    private readonly Dictionary<Pokemon, string?> maskShown = new();

    /// <summary>The Pokémon whose moves are being chosen could put a mask on now, and nobody of the player's has chosen one this turn.</summary>
    public bool CanWearMaskNow =>
        !IsLink && core.Request is ActionRequest && !playingTheLog && MenuBattler.Pokemon != null && !choices.Values.Any(c => c.Mask != null)
        && core.MasksToWear(InCore(MenuBattler)).Count > 0;

    /// <summary>The mask the MASK button shows: the one armed, or the first that could be put on.</summary>
    public string? MaskOffered => MaskArmed ?? (CanWearMaskNow ? core.MasksToWear(InCore(MenuBattler))[0] : null);

    /// <summary>
    /// The MASK button: from none to the first mask that can be put on, to the next, and after the last back to none;
    /// false (and disarmed) where none can be put on.
    /// </summary>
    public bool ToggleMask()
    {
        if (!CanWearMaskNow)
        {
            MaskArmed = null;
            return false;
        }
        var masks = core.MasksToWear(InCore(MenuBattler));
        int at = MaskArmed == null ? -1 : masks.ToList().IndexOf(MaskArmed);
        MaskArmed = at + 1 < masks.Count ? masks[at + 1] : null;
        return true;
    }

    /// <summary>A move chosen with a mask armed puts it on, where no Mega Evolution, Ultra Burst, Dynamax or Terastallization goes with it.</summary>
    private BattleChoice WithMaskArmed(BattleChoice choice)
    {
        string? armed = MaskArmed;
        MaskArmed = null;
        if (armed == null || choice.Kind != ChoiceKind.Fight || choice.Mega || choice.UltraBurst || choice.Dynamax || choice.Terastallize
            || !CanWearMaskNow || !core.CanWearMask(InCore(MenuBattler), armed)) return choice;
        return choice with { Mask = armed };
    }

    /// <summary>A mask put on, and the battle's end, on screen.</summary>
    private void ShowMask(BattleEvent happening)
    {
        switch (happening)
        {
            case MaskWorn worn:
                Sound("select", worn.Place.Side);
                if (At(worn.Place).Pokemon is { } shown && WinterMasks.Get(worn.Mask) is { } mask)
                {
                    maskShown.TryAdd(shown, shown.AbilityName);
                    shown.WornMask = mask.Id;
                    shown.AbilityName = mask.Ability;
                }
                break;
            case Ended:
                foreach (var (p, ability) in maskShown)
                {
                    p.WornMask = null;
                    p.AbilityName = ability;
                }
                maskShown.Clear();
                break;
        }
    }
}
