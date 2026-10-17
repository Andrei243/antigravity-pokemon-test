using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Brings a save's own records up to <see cref="SaveData.CurrentSave"/> as it loads (plan 07 · O2), a step for each
/// <see cref="SaveData.SaveVersion"/> it is behind, on the save itself before the game is made from it: the save's
/// counterpart of <c>StoryMigration</c> (its story) and <see cref="SaveData.PlaceOn"/> (where its player stands). Each
/// step is deterministic and running it twice changes nothing, so the corpus of old saves (plan 24 · X3) comes up to
/// date the same every time. No drawing, no input, no chance.
/// </summary>
public static class SaveMigration
{
    /// <summary>
    /// The save's steps from its version to today's; returns how many of its Pokémon were given ids (nought for a
    /// save of today). The save is changed in place and says <see cref="SaveData.CurrentSave"/> afterwards.
    /// </summary>
    public static int Upgrade(SaveData save)
    {
        int given = 0;
        if (save.SaveVersion < SaveData.PokemonIdsGiven)
        {
            // Version 1 (plan 07 · O2): every Pokémon of the player's is given its own id, in the order the save keeps
            // them (the team, the PC, the Day Care), counted from what the save has issued
            uint next = save.PokemonIdsIssued;
            given = PokemonIds.Give(Owned(save), p => p.Id ?? Guid.Empty, (p, id) => p.Id = id, TrainerNumberOf(save), ref next);
            save.PokemonIdsIssued = next;
        }
        save.SaveVersion = Math.Max(save.SaveVersion, SaveData.CurrentSave);
        return given;
    }

    /// <summary>
    /// The trainer's number a save's ids are made of: the card's number and its hidden half, as the game makes it
    /// (<c>GameEngine.TrainerNumber</c>). A save from before either was kept has nought in its place.
    /// </summary>
    public static uint TrainerNumberOf(SaveData save) =>
        ((uint)(save.Encounters?.SecretId ?? 0) << 16) | (uint)(save.TrainerId & 0xffff);

    /// <summary>
    /// Every Pokémon a save keeps as the player's: the team, the PC (in one list, as saves before the boxes had places
    /// kept it, then box by box) and the Day Care. Not the Battle Frontier's copies, which are nobody's.
    /// </summary>
    public static IEnumerable<SavedPokemonData> Owned(SaveData save)
    {
        foreach (var p in save.Party) yield return p;
        foreach (var p in save.BoxStorage) yield return p;
        if (save.Boxes != null)
            foreach (var box in save.Boxes.Boxes)
                foreach (var boxed in box.Pokemon.OrderBy(b => b.Slot)) yield return boxed.Pokemon;
        if (save.DayCare != null)
            foreach (var place in save.DayCare.Places)
                if (place?.Pokemon is { } left) yield return left;
    }
}
