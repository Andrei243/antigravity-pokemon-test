using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Core;

/// <summary>Remix's wild tables laid over the world's maps (plan 13 · V15; its trainers are laid in <c>StartTrainerBattle</c>).</summary>
public partial class GameEngine
{
    /// <summary>Whether a game of this engine laid Remix's tables, so that a game begun or loaded after it takes them away again.</summary>
    private bool remixLaid;

    /// <summary>
    /// Every map of the world reads Remix's tables in a Remix run (<see cref="Overworld.Map.Tables"/>) and its own in any
    /// other. A run that never had Remix touches no map at all, so Normal is Platinum's game to the byte; the place come
    /// to is laid again on every arrival (<c>ArriveOnMap</c>), for a map made afresh since.
    /// </summary>
    private void LayTheRemix()
    {
        var remix = Remix.For(flow.Run);
        if (remix == null && !remixLaid) return;
        remixLaid = remix != null;
        foreach (string name in MapDatabase.MapNames)
            if (MapDatabase.TryGet(name) is { } map) map.Tables = remix;
        if (flow.Map != null) flow.Map.Tables = remix;
    }
}
