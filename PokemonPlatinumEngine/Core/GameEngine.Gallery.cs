using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The gallery (plan 12 · Q30; style guide, "Menu screens", "Gallery"): the title's GALLERY opens it on the species any
/// readable slot has seen, a Pokédex entry on its own species with the game's Pokédex; B goes back to where it was opened
/// from. It reads what the Pokédex has seen and writes nothing: the save holds nothing new for it.
/// </summary>
public partial class GameEngine
{
    private void OpenGallery(GameState returnTo, string? openOn = null)
    {
        flow.GalleryReturn = returnTo;
        IEnumerable<int> seen;
        List<GalleryPerson> people;
        if (returnTo == GameState.Title)
        {
            // On the title no game is loaded: what any slot has seen, and the people as a new game would have them
            seen = titleSlots.SelectMany(s => (IEnumerable<int>?)s.Save?.SeenSpecies ?? Array.Empty<int>());
            people = GalleryScreen.PeopleFor(PlayerIdentity.CharacterOf(PlayerLook.Boy), PlayerIdentity.DefaultName(PlayerLook.Boy),
                PlayerIdentity.CharacterOf(PlayerLook.Girl), PlayerIdentity.DefaultName(PlayerLook.Girl), RegionDatabase.First.Professor);
        }
        else
        {
            seen = flow.Pokedex.SeenSpecies;
            people = GalleryScreen.PeopleFor(PlayerIdentity.Character, PlayerIdentity.Name, PlayerIdentity.CharacterOf(PlayerIdentity.Look == PlayerLook.Girl ? PlayerLook.Boy : PlayerLook.Girl),
                PlayerIdentity.AssistantName, (Here.Professor ?? RegionDatabase.First.Professor));
        }
        flow.GalleryScreen.Open(seen, people, openOn);
        flow.State = GameState.Gallery;
    }

    private void UpdateGallery(float dt)
    {
        flow.GalleryScreen.Update(dt);
        if (!flow.GalleryScreen.IsActive) CloseGallery();
    }

    /// <summary>Back to the title's menu or the Pokédex entry it was opened from.</summary>
    private void CloseGallery()
    {
        if (flow.GalleryScreen.IsActive) flow.GalleryScreen.Close();
        flow.State = flow.GalleryReturn;
    }
}
