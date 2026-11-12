using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Core;

// The Tales in the game (plan 46 · CP11): which tales the player has learned is the story's own flags
// (TaleTable.FlagOf, FLAG_TALE_<ID>: no save field, no version), handed to a battle as BattleSetup.Tales and told only
// with the Book of Tales in the bag; the Book's screen (UI/BookOfTalesScreen) lists them, opened by USE on the Book.
// CP12 adds the script's `tale learn`; until then a tale is learned by its flag.
public partial class GameEngine
{
    /// <summary>The Book of Tales' field use, by name (<c>items.json</c>'s <c>fieldUse</c>).</summary>
    internal const string BookOfTalesUse = "BookOfTales";

    /// <summary>The player has the Book of Tales in the bag.</summary>
    internal bool HasBookOfTales => ItemDatabase.Get(Battle.Sim.BattleCore.BookOfTalesItem) is { } book && flow.Bag.GetQuantity(book) > 0;

    /// <summary>The tales the player has learned, in the table's order: those whose flag is set.</summary>
    internal IReadOnlyList<string> TalesLearned() =>
        TaleTable.Default.All.Where(t => flow.Story.Has(TaleTable.FlagOf(t.Id))).Select(t => t.Id).ToList();

    /// <summary>Opens the Book of Tales over the field.</summary>
    private void OpenBookOfTales()
    {
        flow.State = GameState.BookOfTales;
        flow.BookOfTalesScreen.Open(TalesLearned());
    }

    private void UpdateBookOfTales(float dt)
    {
        flow.BookOfTalesScreen.Update(dt);
        if (!flow.BookOfTalesScreen.IsActive) flow.State = GameState.Overworld;
    }
}
