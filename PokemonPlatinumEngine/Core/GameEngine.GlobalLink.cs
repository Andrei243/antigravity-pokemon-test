using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Online;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The Global Link counter (plan 07 · O5 and O7, the game's half; <c>docs/mechanics/rulings.md</c>, "The Global Link
/// counter"): the upper floor's Union Room desk opened onto the online server named in the options
/// (<see cref="GameSettings.OnlineServer"/>), with the install token kept beside them, and every trade made there
/// applied to this game's own slot and written at once (<see cref="SaveKeeper"/>) before the server is told.
/// </summary>
public partial class GameEngine
{
    /// <summary>The server the options name, or null.</summary>
    internal Uri? OnlineServer => OnlineIdentity.ServerOf(Settings.OnlineServer);

    private void OpenGlobalLink()
    {
        var server = OnlineServer;
        // Whatever is offered or received is known by its id: every Pokémon of the player's has one first (plan 07 · O2)
        GiveIds();
        Func<OnlineClient>? make = server == null ? null
            : () => new OnlineClient(server, OnlineIdentity.TokenIn(SavePaths.Current.InstallToken), PlayerIdentity.Name);
        // A run that has ended is never written again (plan 13 · V4), so it can't take a trade's receipt
        string? refused = flow.RunState.Ended ? "globalLink.error.runEnded" : null;
        var session = new GlobalLinkSession(make, new SaveKeeper(this), refused);
        flow.GlobalLinkScreen.Open(session, OfferableNow, server == null ? null : OnlineIdentity.Shown(server));
        flow.State = GameState.GlobalLink;
        if (server != null) Log.Info($"The Global Link counter connects to {OnlineIdentity.Shown(server)}.");
    }

    /// <summary>What may be offered at the counter: the team, then the boxes, each with its id.</summary>
    private IReadOnlyList<Pokemon> OfferableNow()
    {
        GiveIds();
        return flow.Party.Members.Concat(flow.Boxes.All).ToList();
    }

    private void UpdateGlobalLink(float dt)
    {
        flow.GlobalLinkScreen.Update(dt);
        if (flow.State != GameState.GlobalLink) return;
        if (!flow.GlobalLinkScreen.IsActive) flow.State = GameState.Overworld;
    }

    /// <summary>
    /// This game's slot as a trade sees it (plan 07 · O5): what it holds by id (<see cref="OwnedPokemon"/>), and a
    /// swap that hands the received Pokémon to <see cref="ReceiveTradedPokemon"/> in the given one's place and writes
    /// the slot before it returns. Nothing is acknowledged to the server unless the save was written, so a swap that
    /// can't be saved is taken up again at the next visit.
    /// </summary>
    private sealed class SaveKeeper : ITradeKeeper
    {
        private readonly GameEngine game;
        public SaveKeeper(GameEngine game) => this.game = game;

        public bool Holds(Guid id) => id != Guid.Empty && game.OwnedPokemon().Any(p => p.Id == id);

        public void Swap(Guid given, SavedPokemonData received)
        {
            var mine = game.flow.Party.Members.Concat(game.flow.Boxes.All).FirstOrDefault(p => p.Id == given)
                ?? throw new InvalidOperationException("The Pokémon given is in neither the team nor the boxes.");
            var pokemon = received.ToPokemon();
            if (!game.ReceiveTradedPokemon(pokemon, mine))
                throw new InvalidOperationException("The Pokémon received is held already.");
            if (!game.WriteSave()) throw new IOException("The game couldn't be saved after the trade.");
            Log.Info($"A trade at the Global Link counter: {mine.Species.Name} went and {pokemon.Species.Name} came; the game was saved.");
        }
    }
}
