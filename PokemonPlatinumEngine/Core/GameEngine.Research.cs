using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The Pokédex's research (plan 15 · E1; <see cref="Research"/>): what a battle's lines and the field tell it is counted
/// as it happens, and a level that rises is told by the field's toast once the field is the player's again, never
/// inside a battle. The certificate is given as the Pokédex opens, beside the diplomas.
/// </summary>
public partial class GameEngine
{
    /// <summary>The species whose research level rose and hasn't been told yet, in the order they rose.</summary>
    private readonly Queue<int> researchToTell = new();

    /// <summary>Takes the levels that rose and tells one at a time, while the field is shown with nothing else on it.</summary>
    private void KeepTheResearch()
    {
        if (!gameStarted) return;
        foreach (int dex in flow.Pokedex.Research.TakeRisen()) if (!researchToTell.Contains(dex)) researchToTell.Enqueue(dex);
        if (researchToTell.Count == 0 || flow.State != GameState.Overworld || ScriptRunning || flow.Toast.Visible) return;
        if (PokemonDatabase.GetByDex(researchToTell.Dequeue()) is not { } species) return;
        ShowNotification(Text.Of("pokedex.tasks.toast", ("name", species.DisplayName), ("level", flow.Pokedex.Research.LevelOf(species))));
    }

    /// <summary>Forgets what was waiting to be told (a game begun or loaded).</summary>
    private void ForgetTheResearchToTell()
    {
        researchToTell.Clear();
        flow.Pokedex.Research.TakeRisen();
    }
}
