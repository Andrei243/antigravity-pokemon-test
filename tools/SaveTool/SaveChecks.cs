using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;

namespace SaveTool;

/// <summary>How serious a finding is: an error is something the game would trip over or silently change; a note is something it would repair as it loads.</summary>
public enum Severity { Error, Note }

/// <summary>One thing wrong with a save: where in it, and what.</summary>
public sealed record SaveFinding(string Where, string Problem, Severity Severity = Severity.Error)
{
    public override string ToString() => $"{(Severity == Severity.Error ? "error" : "note ")}  {Where}: {Problem}";
}

/// <summary>
/// What a save is held to (plan 12 · Q3): every Pokémon one the data has, with levels, IVs, effort, moves, items and
/// abilities in range; the bag's items; the dex numbers; the place it wakes on; the three versions; the file's own
/// checksum. A hand-edited save is found by the checksum when the editor left it, and by these when not. Reads the
/// game's data folder (<see cref="PokemonDatabase"/> and the rest), so a caller points <c>GameDataFiles</c> at it first
/// and initialises <see cref="MapDatabase"/> if it asks for the place. No drawing, no input, no clock.
/// </summary>
public static class SaveChecks
{
    /// <summary>The most a bag holds of one item.</summary>
    public const int MostOfAnItem = 999;

    /// <summary>The most money a trainer holds.</summary>
    public const int MostMoney = 999_999;

    /// <summary>The Pokémon a team holds.</summary>
    public const int TeamSize = 6;

    /// <summary>
    /// Every finding about a save. <paramref name="fileText"/> is the file as written, when there is one: its checksum is
    /// held to it; a save built in memory passes null.
    /// </summary>
    public static IReadOnlyList<SaveFinding> Validate(SaveData save, string? fileText = null)
    {
        var found = new List<SaveFinding>();
        if (fileText != null && !SaveManager.Intact(fileText))
            found.Add(new("file", "the checksum is not the file's: it was changed after the game wrote it (by hand, or cut short)"));

        Versions(save, found);
        Place(save, found);
        Trainer(save, found);
        var seen = new HashSet<Guid>();
        foreach (var saved in SaveMigration.Owned(save))
        {
            string where = Describe(save, saved);
            Pokemon(where, saved, found);
            if (saved.Id is { } id && id != Guid.Empty && !seen.Add(id))
                found.Add(new(where, $"its id {id} is another Pokémon's too"));
        }
        if (save.Party.Count > TeamSize) found.Add(new("team", $"{save.Party.Count} Pokémon on a team of at most {TeamSize}"));
        Boxes(save, found);
        Bag(save, found);
        Pokedex(save, found);
        return found;
    }

    // Where a Pokémon of the save is, as a person would say it: the team's place, the box and its place, the Day Care
    private static string Describe(SaveData save, SavedPokemonData saved)
    {
        string name = saved.SpeciesName;
        for (int i = 0; i < save.Party.Count; i++) if (ReferenceEquals(save.Party[i], saved)) return $"team #{i + 1} ({name})";
        for (int i = 0; i < save.BoxStorage.Count; i++) if (ReferenceEquals(save.BoxStorage[i], saved)) return $"PC #{i + 1} ({name})";
        if (save.Boxes != null)
            for (int b = 0; b < save.Boxes.Boxes.Count; b++)
                foreach (var boxed in save.Boxes.Boxes[b].Pokemon)
                    if (ReferenceEquals(boxed.Pokemon, saved)) return $"box {b + 1}, place {boxed.Slot + 1} ({name})";
        return $"Day Care ({name})";
    }

    private static void Versions(SaveData save, List<SaveFinding> found)
    {
        if (save.WorldVersion is < 0 or > SaveData.CurrentWorld) found.Add(new("versions", $"WorldVersion {save.WorldVersion} is not one this game wrote (0 to {SaveData.CurrentWorld}): a newer game's?"));
        if (save.StoryVersion is < 0 or > StoryState.CurrentVersion) found.Add(new("versions", $"StoryVersion {save.StoryVersion} is not one this game wrote (0 to {StoryState.CurrentVersion}): a newer game's?"));
        if (save.SaveVersion is < 0 or > SaveData.CurrentSave) found.Add(new("versions", $"SaveVersion {save.SaveVersion} is not one this game wrote (0 to {SaveData.CurrentSave}): a newer game's?"));
        if (save.WorldVersion < SaveData.CurrentWorld || save.StoryVersion < StoryState.CurrentVersion || save.SaveVersion < SaveData.CurrentSave)
            found.Add(new("versions", $"world {save.WorldVersion}/{SaveData.CurrentWorld}, story {save.StoryVersion}/{StoryState.CurrentVersion}, save {save.SaveVersion}/{SaveData.CurrentSave}: the game brings it up to date as it loads (the tool's migrate does so on a copy)", Severity.Note));
    }

    private static void Place(SaveData save, List<SaveFinding> found)
    {
        MapSpot spot;
        try
        {
            spot = save.PlaceOn(MapDatabase.Get);
        }
        catch (Exception e)
        {
            found.Add(new("place", $"it can't be put on a map: {e.Message}"));
            return;
        }
        if (!MapDatabase.MapNames.Contains(spot.Map, StringComparer.OrdinalIgnoreCase))
        {
            found.Add(new("place", $"{save.CurrentMapName} is no map of this game; the player would wake on {spot.Map}"));
            return;
        }
        var map = MapDatabase.Get(spot.Map);
        if (!map.InBounds(spot.X, spot.Y)) found.Add(new("place", $"{spot.X},{spot.Y} is off the map {spot.Map} ({map.Width} by {map.Height})"));
        else if (map.IsSolid(spot.X, spot.Y)) found.Add(new("place", $"{spot.X},{spot.Y} of {spot.Map} is something solid: the player would wake inside it"));
        if (!string.Equals(spot.Map, save.CurrentMapName, StringComparison.OrdinalIgnoreCase) || (spot.X, spot.Y) != (save.PlayerGridX, save.PlayerGridY))
            found.Add(new("place", $"it was made at {save.CurrentMapName} {save.PlayerGridX},{save.PlayerGridY} and wakes at {spot.Map} {spot.X},{spot.Y}", Severity.Note));
    }

    private static void Trainer(SaveData save, List<SaveFinding> found)
    {
        if (save.Money is < 0 or > MostMoney) found.Add(new("trainer", $"money {save.Money} is outside 0 to {MostMoney}"));
        if (save.PlayTimeSeconds < 0 || float.IsNaN(save.PlayTimeSeconds)) found.Add(new("trainer", $"play time {save.PlayTimeSeconds} is not a time"));
        if (save.TrainerId is < 0 or > 65535) found.Add(new("trainer", $"trainer number {save.TrainerId} is outside 0 to 65535"));
        if (string.IsNullOrWhiteSpace(save.PlayerName)) found.Add(new("trainer", "the player has no name"));
        else if (save.PlayerName.Length > 10) found.Add(new("trainer", $"the name '{save.PlayerName}' is longer than the keyboard's ten letters", Severity.Note));
        foreach (var (region, mask) in save.RegionalBadges())
            if (mask is < 0 or > 255) found.Add(new("trainer", $"the badges of {region} are {mask}: a region has eight, so 0 to 255"));
        if (save.Heard.Count != save.Heard.Distinct().Count()) found.Add(new("trainer", "the songs heard are listed twice", Severity.Note));
    }

    /// <summary>The findings about one Pokémon, named <paramref name="where"/>.</summary>
    public static void Pokemon(string where, SavedPokemonData saved, List<SaveFinding> found)
    {
        var species = PokemonDatabase.Get(saved.SpeciesName);
        if (species == null)
        {
            var byNumber = saved.DexNumber > 0 ? PokemonDatabase.GetByDex(saved.DexNumber) : null;
            found.Add(new(where, byNumber != null
                ? $"there is no species '{saved.SpeciesName}'; number {saved.DexNumber} is {byNumber.Name}, which the game would make it"
                : $"there is no species '{saved.SpeciesName}', and number {saved.DexNumber} is none either: the game would make it a Turtwig"));
        }
        else if (saved.DexNumber != 0 && saved.DexNumber != species.DexNumber)
            found.Add(new(where, $"it says it is number {saved.DexNumber} but {species.Name} is {species.DexNumber}"));
        if (saved.Form != null)
        {
            var home = PokemonDatabase.SpeciesOfForm(saved.Form);
            if (home == null) found.Add(new(where, $"there is no form '{saved.Form}'"));
            else if (species != null && home.Name != species.Name) found.Add(new(where, $"{saved.Form} is a form of {home.Name}, not of {species.Name}"));
        }

        int top = Ruleset.Modern.LevelCap;
        if (saved.Level < 1 || saved.Level > top) found.Add(new(where, $"level {saved.Level} is outside 1 to {top}"));
        var ivs = new[] { ("HP", saved.IvHP), ("Attack", saved.IvAttack), ("Defense", saved.IvDefense), ("Sp. Atk", saved.IvSpAttack), ("Sp. Def", saved.IvSpDefense), ("Speed", saved.IvSpeed) };
        foreach (var (stat, iv) in ivs)
            if (iv is < 0 or > 31) found.Add(new(where, $"{stat} IV {iv} is outside 0 to 31"));
        var evs = new[] { ("HP", saved.EvHP), ("Attack", saved.EvAttack), ("Defense", saved.EvDefense), ("Sp. Atk", saved.EvSpAttack), ("Sp. Def", saved.EvSpDefense), ("Speed", saved.EvSpeed) };
        foreach (var (stat, ev) in evs)
        {
            if (ev is < 0 or > 255) found.Add(new(where, $"{stat} effort {ev} is outside 0 to 255"));
            else if (ev > Ruleset.Modern.EvStatCap) found.Add(new(where, $"{stat} effort {ev} is over {Ruleset.Modern.EvStatCap}; the game cuts it as it loads", Severity.Note));
        }
        if (evs.Sum(e => e.Item2) > 510) found.Add(new(where, $"effort adds up to {evs.Sum(e => e.Item2)}, over the 510 a Pokémon can hold"));
        if (saved.CurrentExp < 0) found.Add(new(where, $"experience {saved.CurrentExp} is below nothing"));
        if (saved.Friendship is < 0 or > 255) found.Add(new(where, $"friendship {saved.Friendship} is outside 0 to 255"));
        if (saved.Ability != null && AbilityDatabase.Get(saved.Ability) == null) found.Add(new(where, $"there is no ability '{saved.Ability}'"));
        if (saved.HeldItem != null && ItemDatabase.Get(saved.HeldItem) == null) found.Add(new(where, $"there is no item '{saved.HeldItem}' to hold"));

        if (saved.Moves.Count > 4) found.Add(new(where, $"{saved.Moves.Count} moves, and a Pokémon knows four"));
        else if (saved.Moves.Count == 0 && !saved.IsEgg) found.Add(new(where, "it knows no move: the game gives it its species' own", Severity.Note));
        foreach (var move in saved.Moves)
        {
            if (!MoveDatabase.Has(move.MoveName))
            {
                found.Add(new(where, $"there is no move '{move.MoveName}'; the game would turn it into Tackle"));
                continue;
            }
            var data = MoveDatabase.Get(move.MoveName).Under(Ruleset.Modern);
            if (move.PPUps is < 0 or > 3) found.Add(new(where, $"{move.MoveName} has {move.PPUps} PP Ups, and three is the most"));
            int most = Move.MaxPPWith(data, move.PPUps);
            // (A game from Platinum's rules may hold more PP than the modern ones allow; the game cuts it as it loads)
            int platinumMost = Move.MaxPPWith(MoveDatabase.Get(move.MoveName).Under(Ruleset.Platinum), move.PPUps);
            if (move.CurrentPP < 0 || move.CurrentPP > Math.Max(most, platinumMost)) found.Add(new(where, $"{move.MoveName} has {move.CurrentPP} PP of at most {most}"));
            else if (move.CurrentPP > most) found.Add(new(where, $"{move.MoveName} has {move.CurrentPP} PP, over the {most} of the modern rules; the game cuts it as it loads", Severity.Note));
        }
        if (saved.Moves.Select(m => m.MoveName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != saved.Moves.Count)
            found.Add(new(where, "it knows one move twice"));

        // Its HP against its stats, with the Pokémon made as the game makes it
        if (species != null && saved.Level >= 1 && saved.Level <= top)
        {
            try
            {
                var made = saved.ToPokemon(Ruleset.Modern);
                if (saved.CurrentHP < 0 || saved.CurrentHP > made.MaxHP)
                    found.Add(new(where, $"{saved.CurrentHP} HP of at most {made.MaxHP}"));
            }
            catch (Exception e)
            {
                found.Add(new(where, $"the game can't make it: {e.Message}"));
            }
        }
    }

    private static void Boxes(SaveData save, List<SaveFinding> found)
    {
        if (save.Boxes == null) return;
        int boxes = save.Boxes.Boxes.Count;
        if (boxes != PcBoxes.BoxCount && boxes != PcBoxes.NationalBoxCount)
            found.Add(new("boxes", $"{boxes} boxes; the PC has {PcBoxes.BoxCount}, or {PcBoxes.NationalBoxCount} with the National Pokédex"));
        else if (boxes == PcBoxes.NationalBoxCount && !save.NationalPokedex)
            found.Add(new("boxes", $"{boxes} boxes without the National Pokédex", Severity.Note));
        for (int b = 0; b < boxes; b++)
        {
            var taken = new HashSet<int>();
            foreach (var boxed in save.Boxes.Boxes[b].Pokemon)
            {
                if (boxed.Slot is < 0 or >= PcBoxes.BoxSize) found.Add(new($"box {b + 1}", $"{boxed.Pokemon.SpeciesName} is in place {boxed.Slot + 1} of {PcBoxes.BoxSize}"));
                else if (!taken.Add(boxed.Slot)) found.Add(new($"box {b + 1}", $"place {boxed.Slot + 1} holds two Pokémon"));
            }
        }
    }

    private static void Bag(SaveData save, List<SaveFinding> found)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in save.Inventory)
        {
            if (ItemDatabase.Get(item.ItemName) == null) found.Add(new("bag", $"there is no item '{item.ItemName}'; the game leaves it out"));
            if (item.Quantity < 1 || item.Quantity > MostOfAnItem) found.Add(new("bag", $"{item.ItemName} x{item.Quantity}: a bag holds 1 to {MostOfAnItem} of an item"));
            if (!names.Add(item.ItemName)) found.Add(new("bag", $"{item.ItemName} is listed twice", Severity.Note));
        }
    }

    private static void Pokedex(SaveData save, List<SaveFinding> found)
    {
        int last = PokemonDatabase.GetAll().Max(s => s.DexNumber);
        foreach (var (name, list) in new[] { ("seen", save.SeenSpecies), ("caught", save.CaughtSpecies) })
        {
            var outside = list.Where(n => n < 1 || n > last).Distinct().ToList();
            if (outside.Count > 0) found.Add(new("pokédex", $"{name}: {string.Join(", ", outside)} is no number of 1 to {last}"));
        }
        var unseen = save.CaughtSpecies.Except(save.SeenSpecies).ToList();
        if (unseen.Count > 0) found.Add(new("pokédex", $"{unseen.Count} caught and never seen ({string.Join(", ", unseen.Take(5))}{(unseen.Count > 5 ? ", …" : "")}): the game does not mind, a hand does", Severity.Note));
    }
}
