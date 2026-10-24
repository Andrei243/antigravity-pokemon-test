using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>
/// The Hidden Moves app (plan 12 · Q18; style guide, "The Pokétch"): ours, where the games have none. While the options'
/// FIELD HELPERS row is on it is the last app on the Pokétch, whether anyone gave it or not, and lists the field moves a
/// wild helper does for the player now (<see cref="PoketchContext.Granted"/>): the eight whose Badge and HM are held.
/// It only shows; nothing on it is touched.
/// </summary>
public sealed class HiddenMovesApp : PoketchAppState
{
    /// <summary>The rows the screen holds: eight moves, a row to each.</summary>
    public const int Rows8 = 8;

    public override PoketchApp App => PoketchApp.HiddenMoves;

    /// <summary>The moves listed, in the field moves' order.</summary>
    public IReadOnlyList<FieldMove> Moves(PoketchContext context) => context.Granted;
}
