using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The Stroll Counter in the game (plan 35 · BN7; <see cref="StrollCounter"/>; docs/mechanics/rulings.md, "The Stroll
/// Counter"): its screen from the bag and the item button, every step of the player's a step of its Pokémon
/// (<c>OnStep</c>), and at a PC the script's <c>stroll</c> acts: its Pokémon paid and stored, what it met into the boxes,
/// what it found into the bag, and another picked out of the boxes and put in.
/// </summary>
public partial class GameEngine
{
    private bool strollFromBag;

    // What the last `stroll back` delivered: the Pokémon met and the things found
    private int strollCaughtBack, strollFoundBack;

    /// <summary>Opens the counter's screen.</summary>
    private void OpenStroll(bool fromBag)
    {
        strollFromBag = fromBag;
        flow.State = GameState.StrollCounter;
        var entered = RegionsEnteredNow();
        flow.StrollScreen.Open(flow.Stroll, flow.Run, TrainerNumber, r => entered.Contains(r, StringComparer.OrdinalIgnoreCase), KeepTheMeet);
    }

    private void UpdateStroll(float dt)
    {
        flow.StrollScreen.Update(dt);
        if (flow.StrollScreen.IsActive) return;
        if (strollFromBag)
        {
            flow.State = GameState.BagMenu;
            flow.BagScreen.Resume();
        }
        else flow.State = GameState.Overworld;
    }

    /// <summary>
    /// A meet won: the Pokémon is made on the meet's own generator at the course's level, mapped as its region's levels
    /// are where the game didn't begin in it (plan 18 · W4), and held in the counter until a PC.
    /// </summary>
    private Pokemon? KeepTheMeet(StrollMeet meet)
    {
        var region = StrollCounter.Course(flow.Stroll.Course) is { } course ? RegionDatabase.Get(course.Region) : null;
        bool crossed = region != null && !string.Equals(region.Id, flow.BeganIn, StringComparison.OrdinalIgnoreCase);
        return StrollCounter.EndMeet(flow.Stroll, meet, TrainerNumber, level => crossed ? region!.Levels?.Map(level) ?? level : level,
            Randomizer.For(flow.Run), GameClock.Today);
    }

    /// <summary>An act of the counter at a PC (the script's <c>stroll</c>); each answer is in docs/scripts.md, "The Stroll Counter".</summary>
    internal StrollAnswer StrollAct(string act)
    {
        var save = flow.Stroll;
        switch (act)
        {
            case "back":
            {
                strollCaughtBack = strollFoundBack = 0;
                // Its own Pokémon first, and only with a place for it in the boxes: nothing leaves the counter that
                // has nowhere to go (3: the boxes are full, and everything stays where it is)
                if (save.Pokemon != null && flow.Boxes.Free <= 0) return new(3, save.Pokemon.ToPokemon().DisplayName);
                var paid = StrollCounter.TakeBack(save, CurrentLevelCap());
                if (paid != null) flow.Boxes.Store(paid.Pokemon);
                // Then what it met, as many as the boxes have places for (the rest stay in the counter), and what it found
                foreach (var held in save.Caught.ToList())
                {
                    if (flow.Boxes.Free <= 0) break;
                    var caught = held.ToPokemon();
                    flow.Pokedex.RegisterSeen(caught);
                    flow.Pokedex.RegisterCaught(caught);
                    if (flow.Boxes.Store(caught) == null) break;
                    save.Caught.Remove(held);
                    strollCaughtBack++;
                }
                foreach (string item in StrollCounter.TakeFound(save))
                    if (ItemDatabase.Get(item) is { } data) { flow.Bag.AddItem(data, 1); strollFoundBack++; }
                GiveIds();
                return paid == null ? new(0) : new(1, paid.Pokemon.DisplayName, paid.Strides);
            }
            case "caught":
                return new(strollCaughtBack, "", strollCaughtBack);
            case "found":
                return new(strollFoundBack, "", strollFoundBack);
            case "pick":
                if (save.Pokemon != null) return new(0);
                flow.PCScreen.OpenToPickPokemon(flow.Boxes);
                flow.State = GameState.PCStorage;
                return new(1);
            case "put":
            {
                var pc = flow.PCScreen;
                if (!pc.PickingPokemon || pc.PickedBox is not { } box || pc.PickedCell is not { } cell) return new(0);
                if (flow.Boxes[box, cell] is not { } pokemon) return new(0);
                if (StrollCounter.WhyNotPut(save, pokemon) != StrollRefusal.None) return new(2);
                var first = StrollCounter.Open(save.StridesEver, r => RegionsEnteredNow().Contains(r, StringComparer.OrdinalIgnoreCase)).FirstOrDefault();
                // (taken out of its box first, and put back if the counter refuses it after all)
                if (!flow.Boxes.Remove(pokemon)) return new(0);
                if (!StrollCounter.Put(save, pokemon, first?.Id ?? "")) { flow.Boxes.Store(pokemon); return new(2); }
                return new(1, pokemon.DisplayName);
            }
        }
        return new(0);
    }

    private sealed partial class FieldHost
    {
        public StrollAnswer Stroll(string act) => game.StrollAct(act);
    }
}
