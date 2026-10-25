using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;

namespace SaveTool;

/// <summary>What migrating a save did, line by line, and the save as it came out.</summary>
public sealed record MigratedSave(SaveData Migrated, IReadOnlyList<string> Changes)
{
    /// <summary>Whether the save was already today's: nothing changed.</summary>
    public bool Unchanged => Changes.Count == 0;
}

/// <summary>
/// A save brought up to date without the game (plan 12 · Q3), through the three things the game brings a save up to
/// date by when it loads one: <see cref="PokemonPlatinumEngine.Core.SaveMigration"/> (the save's own records),
/// <see cref="SaveData.PlaceOn"/> (where its player stands on today's maps) and <see cref="StoryMigration"/> (its story
/// and its bag). The save given is not changed; the result is a copy, written with a fresh checksum by the caller.
/// What the game does besides as it loads (giving the PC the National Pokédex's boxes, a Turtwig to a save from before
/// the opening) is not repeated here: the game does it at load whatever the file says.
/// </summary>
public static class SaveMigrate
{
    public static MigratedSave Run(SaveData original)
    {
        // A copy through the file's own text, so nothing of the original is touched
        var save = PokemonPlatinumEngine.Core.SaveManager.Read(PokemonPlatinumEngine.Core.SaveManager.Write(original))!;
        var changes = new List<string>();

        int before = save.SaveVersion;
        int given = PokemonPlatinumEngine.Core.SaveMigration.Upgrade(save);
        if (before != save.SaveVersion) changes.Add($"save version {before} -> {save.SaveVersion}, {given} Pokémon given their ids");

        var spot = save.PlaceOn(MapDatabase.Get);
        if (!string.Equals(spot.Map, save.CurrentMapName, StringComparison.OrdinalIgnoreCase) || (spot.X, spot.Y) != (save.PlayerGridX, save.PlayerGridY))
        {
            changes.Add($"the player moves from {save.CurrentMapName} {save.PlayerGridX},{save.PlayerGridY} to {spot.Map} {spot.X},{spot.Y} facing {spot.Facing}");
            (save.CurrentMapName, save.PlayerGridX, save.PlayerGridY, save.PlayerFacing) = (spot.Map, spot.X, spot.Y, spot.Facing);
        }
        if (save.WorldVersion != SaveData.CurrentWorld)
        {
            changes.Add($"world version {save.WorldVersion} -> {SaveData.CurrentWorld}");
            save.WorldVersion = SaveData.CurrentWorld;
        }

        var story = new StoryState();
        story.Restore(save.ToStory());
        var flagsBefore = story.Snapshot();
        var owned = PokemonPlatinumEngine.Core.SaveMigration.Owned(save).Select(p => p.ToPokemon()).ToList();
        // (A save with nobody on the team is given a Turtwig, as the game gives it)
        if (save.Party.Count == 0 && save.StoryVersion < 2) owned.Insert(0, new Pokemon(PokemonDatabase.Get("Turtwig")!, 5));
        var bag = new Inventory();
        foreach (var item in save.Inventory)
            if (ItemDatabase.Get(item.ItemName) is { } data) bag.AddItem(data, item.Quantity);
        var itemsBefore = Items(bag);
        int storyBefore = save.StoryVersion;
        StoryMigration.Upgrade(story, save.StoryVersion, owned, ScriptLibrary.Default, bag);
        var after = story.Snapshot();

        foreach (string flag in after.Flags.Except(flagsBefore.Flags).Order(StringComparer.Ordinal)) changes.Add($"+ {flag}");
        foreach (string flag in flagsBefore.Flags.Except(after.Flags).Order(StringComparer.Ordinal)) changes.Add($"- {flag}");
        foreach (var (name, value) in after.Variables.OrderBy(v => v.Key, StringComparer.Ordinal))
            if (flagsBefore.Variables.GetValueOrDefault(name, int.MinValue) != value) changes.Add($"= {name} {value}");
        string itemsAfter = Items(bag);
        if (itemsBefore != itemsAfter) changes.Add($"bag: {itemsBefore} -> {itemsAfter}");
        if (storyBefore != StoryState.CurrentVersion) changes.Add($"story version {storyBefore} -> {StoryState.CurrentVersion}");

        save.StoryFlags = after.Flags;
        save.StoryVariables = after.Variables;
        save.DefeatedTrainers = after.DefeatedTrainers;
        save.TakenItems = after.TakenItems;
        save.RegionBadges = after.Badges;
        save.PlayerStarter = after.PlayerStarter;
        save.RivalStarter = after.RivalStarter;
        save.StoryVersion = StoryState.CurrentVersion;
        save.Inventory = bag.AllItems.Select(i => new SavedItemData { ItemName = i.Name, Quantity = i.Quantity }).ToList();
        return new MigratedSave(save, changes);
    }

    private static string Items(Inventory bag) => string.Join(", ", bag.AllItems.Select(i => $"{i.Name} x{i.Quantity}"));
}
