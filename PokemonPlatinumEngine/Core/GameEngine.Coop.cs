using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// A friend in the partner's place (plan 13 · V20; <c>docs/mechanics/rulings.md</c>, "A friend in the partner's place"):
/// with the options' FRIEND AS PARTNER row on, a battle beside a partner is first asked about, in a line of our own with
/// its answers beside it, as a script's question has them, so a script that started the battle waits for it as for any
/// text. A yes hands the battle <see cref="BattleSetup.FriendPartner"/>, and the partner's place is chosen through the
/// battle's own menus; a no, or the row off, leaves the partner to think for itself as ever, and nothing is asked.
/// </summary>
public partial class GameEngine
{
    /// <summary>A battle beside a partner waiting for the question's answer: what it was to be started with.</summary>
    private sealed record CoopWaiting(NPC Trainer, NPC? Second, Trainer Partner, bool FirstBattle, BattleTerms? Terms);

    private CoopWaiting? coopWaiting;

    /// <summary>Whether the question on the screen is the one about a friend in the partner's place.</summary>
    internal bool AskingAboutAFriend => coopWaiting != null;

    /// <summary>
    /// Asks whether a friend chooses for <paramref name="partner"/> before the battle begins: true while the question
    /// takes the field (the battle starts once it is answered). False, and nothing asked, with the row off.
    /// </summary>
    private bool AskAboutAFriend(NPC trainer, NPC? second, Trainer partner, bool firstBattle, BattleTerms? terms)
    {
        if (!Settings.Coop) return false;
        coopWaiting = new CoopWaiting(trainer, second, partner, firstBattle, terms);
        flow.Choice.Dismiss();
        flow.PendingAnswers = new[] { Text.Of("scriptLines.yes"), Text.Of("scriptLines.no") };
        flow.PendingCancel = 1;
        flow.Dialogue.ShowQuestion("", Text.Of("engine.coop.question", ("partner", partner.Name)));
        flow.State = GameState.Dialogue;
        return true;
    }

    /// <summary>The answer: the battle begins, with the friend at the partner's place on a yes.</summary>
    private void AnswerAboutAFriend(bool yes)
    {
        if (coopWaiting is not { } waiting) return;
        coopWaiting = null;
        StartTrainerBattle(waiting.Trainer, waiting.Second, waiting.Partner, waiting.FirstBattle, waiting.Terms, friend: yes);
    }
}
