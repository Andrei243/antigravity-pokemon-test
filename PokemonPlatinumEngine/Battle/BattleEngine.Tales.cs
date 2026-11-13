using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Battle;

// The Tales on screen (plan 46 · CP11): the move menu's TALE button (the PageRight action, beside the item button's
// later mechanics, since a tale may go with a Mega Evolution or a Terastallization) turns through the tales the player
// could tell and back to none; a move chosen with one armed tells it. The screen takes the tale's sky from the log's
// events and a Tale Form's shape from TaleFormChanged, never from the rules' field, which is a turn ahead.
public partial class BattleEngine
{
    /// <summary>
    /// The song layer of the tale told over the field as the lines have shown it (the animator's, never the rules',
    /// which are a turn ahead): what the battle theme's tale layer plays (plan 46 · CP12), from the line that tells the
    /// tale to the line that ends it, with the scene off as well, since the layer is state of the tale told.
    /// </summary>
    public string? TaleLayer => Anim.Tale is { } tale ? core.TaleTable.Get(tale)?.SongLayer : null;

    /// <summary>The tale armed for the move to be chosen next, or null.</summary>
    public string? TaleArmed { get; private set; }

    /// <summary>The trainer of the Pokémon whose moves are being chosen could tell a tale now, and nobody of the player's has chosen one this turn.</summary>
    public bool CanTellTaleNow =>
        !IsLink && core.Request is ActionRequest && !playingTheLog && MenuBattler.Pokemon != null && !choices.Values.Any(c => c.Tale != null)
        && core.TalesToTell(InCore(MenuBattler)).Count > 0;

    /// <summary>The tale the TALE button shows: the one armed, or the first that could be told.</summary>
    public string? TaleOffered => TaleArmed ?? (CanTellTaleNow ? core.TalesToTell(InCore(MenuBattler))[0] : null);

    /// <summary>
    /// The TALE button: from none to the first tale that can be told, to the next, and after the last back to none;
    /// false (and disarmed) where none can be told.
    /// </summary>
    public bool ToggleTale()
    {
        if (!CanTellTaleNow)
        {
            TaleArmed = null;
            return false;
        }
        var tales = core.TalesToTell(InCore(MenuBattler));
        int at = TaleArmed == null ? -1 : tales.ToList().IndexOf(TaleArmed);
        TaleArmed = at + 1 < tales.Count ? tales[at + 1] : null;
        return true;
    }

    /// <summary>A move chosen with a tale armed tells it.</summary>
    private BattleChoice WithTaleArmed(BattleChoice choice)
    {
        string? armed = TaleArmed;
        TaleArmed = null;
        if (armed == null || choice.Kind != ChoiceKind.Fight || !CanTellTaleNow || !core.CanTellTale(InCore(MenuBattler), armed)) return choice;
        return choice with { Tale = armed };
    }

    /// <summary>A tale told, answered or ended, a Tale Form woken or left, and the battle's end, on screen.</summary>
    private void ShowTale(BattleEvent happening)
    {
        switch (happening)
        {
            case TaleTold told:
                Sound("page", told.Teller);
                Anim.TellTale(told.Tale);
                break;
            case TaleAnswered answered:
                Sound("page", answered.Teller);
                Anim.TellTale(answered.Tale);
                break;
            case TaleEnded:
                Anim.TellTale(null);
                break;
            case TaleFormChanged changed:
                // The shape is the event's: the rules' copy may have gone back already
                if (At(changed.Place).Pokemon is { } shown && (changed.Form == null || shown.Species.Form(changed.Form) != null))
                    shown.ChangeForm(changed.Form);
                break;
            case Ended:
                Anim.TellTale(null);
                break;
        }
    }
}
