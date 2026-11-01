using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Battle;

/// <summary>
/// A battle suspended and taken up again (plan 12 · Q25): the battle is made afresh from where it began, on its own
/// seed, and the answers given before it was left are played back at once with nothing shown (as plan 08 · P11's watch
/// mode plays a recording, without its pauses and with no sound), so it opens on the menu it was left at with every
/// Pokémon, the bag and the Pokédex as the battle had made them.
/// </summary>
public partial class BattleEngine
{
    /// <summary>The answers still to be played back while the battle catches up; null once it has, or for a battle that never did.</summary>
    private IReadOnlyList<IReadOnlyList<BattleChoice>>? catchUp;
    private int catchUpFed;

    /// <summary>The record's answers did not fit the battle made again (another data, a move learned outside the record): it can't be taken up.</summary>
    public bool CatchUpBroke { get; private set; }

    /// <summary>
    /// Plays <paramref name="answers"/> (a suspended battle's record, in the order the rules asked) on this battle,
    /// which must have been made from the same opening and seed, at once and silently, to the first question none of
    /// them answers. An item the player used from the bag leaves it as it did when chosen. Returns false where the
    /// record broke off (<see cref="CatchUpBroke"/>).
    /// </summary>
    public bool CatchUp(IReadOnlyList<IReadOnlyList<BattleChoice>> answers)
    {
        catchUp = answers;
        catchUpFed = 0;
        bool wasHushed = AudioManager.Hushed;
        AudioManager.Hushed = true;
        try
        {
            // (Each step either reads a line, ends a wait, or hands an answer over; the guard only stops a record that
            // loops on nothing, which a broken file could)
            for (int guard = 0; guard < 200_000 && catchUp != null && !CatchUpBroke; guard++)
            {
                const float Long = 10f;
                Anim.Update(Long, (side, slot) => SlotsOf(side).ElementAtOrDefault(slot)?.Pokemon);
                UpdatePendingEffects(Long);
                HUD.LevelUp?.Advance(Long);
                if (IsLearningMove || waitingForMessageConfirm) ConfirmMessage();
                else if (messageWaitTimer > 0f)
                {
                    messageWaitTimer = 0f;
                    Pump();
                }
                else Pump();
                if (Result != BattleResult.None && steps.Count == 0 && interlude.Count == 0 && !waitingForMessageConfirm) catchUp = null;
            }
        }
        finally
        {
            AudioManager.Hushed = wasHushed;
        }
        if (catchUp != null) CatchUpBroke = true;
        catchUp = null;
        // What the catching up started lands now, and the views stand as they would after it
        FlushPendingEffects();
        Anim.Update(10f, (side, slot) => SlotsOf(side).ElementAtOrDefault(slot)?.Pokemon);
        return !CatchUpBroke;
    }

    /// <summary>
    /// The rules wait and the screen has shown all before it, while catching up: the next answer of the record is
    /// handed over (true), or, with none left, the battle goes on as any battle does and its menu opens (false).
    /// </summary>
    private bool FeedTheCatchUp()
    {
        if (catchUp == null || core.Request == null || core.Result != BattleResult.None) return false;
        if (catchUpFed >= catchUp.Count)
        {
            catchUp = null;
            return false;
        }
        var answer = catchUp[catchUpFed++];
        try
        {
            SyncToCore();
            core.Submit(answer);
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException)
        {
            CatchUpBroke = true;
            return true;
        }
        // An item the player chose from the bag left it as it was chosen (UseFromBag), not as the rules used it
        foreach (var choice in answer)
            if (choice is { Kind: ChoiceKind.Item, Item: { } name } && choice.Who.Side == BattleSide.Player && ItemDatabase.Get(name) is { } item
                && At(choice.Who).Trainer == null)
            {
                PlayerInventory.RemoveItem(item, 1);
                LastUsedItem = name;
            }
        Play(core.TakeLog());
        return true;
    }
}
