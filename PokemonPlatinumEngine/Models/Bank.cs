using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// A Pokémon in the Home box (plan 12 · Q29): whole, as a save keeps it, with its place and where it came from.
/// </summary>
public sealed class BankedPokemon
{
    /// <summary>The box (from 0) and the place in it (from 0).</summary>
    public int Box { get; set; }
    public int Slot { get; set; }

    /// <summary>The Pokémon, every field a save keeps.</summary>
    public SavedPokemonData Pokemon { get; set; } = new();

    /// <summary>
    /// It was put in from a game whose run randomises (plan 13 · V6): the mark it keeps wherever it goes from here, so
    /// that the Home box always says which of its Pokémon are not what the games would give.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Randomised { get; set; }

    /// <summary>The trainer of the save it was put in from, and when, for the screen's side panel.</summary>
    public string? From { get; set; }
}

/// <summary>What reading the Home box's file came to.</summary>
public enum BankLoad
{
    /// <summary>No file: the Home box is empty.</summary>
    None,
    Ok,
    /// <summary>The file was damaged and its backup was read.</summary>
    Recovered,
    /// <summary>Neither the file nor its backup is whole: nothing may be moved until it is.</summary>
    Damaged,
    /// <summary>The file was written under other data (<see cref="GameDataFiles.Version"/>): refused with a notice, and nothing moved.</summary>
    OtherData
}

/// <summary>
/// The Home box (plan 12 · Q29; <c>docs/mechanics/rulings.md</c>, "Trades between saves and the Home box"): boxes of
/// Pokémon beside every slot's own, the one thing the three slots share, kept in <c>home.json</c> beside them
/// (<see cref="SavePaths.HomeBox"/>). It is written as a save is (a temporary file, the old file as its backup, a
/// checksum), keeps the receipts of the moves under way as a save does (<see cref="MoveReceipt"/>), and names the
/// data it was written under, so that a file of another version is refused rather than read. No drawing and no input.
/// </summary>
public sealed class HomeBank
{
    /// <summary>How many boxes, and how many places to a box (a PC box's thirty).</summary>
    public const int BoxCount = 8;
    public const int BoxSize = PcBoxes.BoxSize;

    /// <summary>Every place the Home box has.</summary>
    public const int Capacity = BoxCount * BoxSize;

    /// <summary>The file's checksum (<see cref="SaveManager.Seal"/>), first in the file.</summary>
    [JsonPropertyOrder(-1)]
    public string? Checksum { get; set; }

    /// <summary>The game's data it was written under (<see cref="GameDataFiles.Version"/>).</summary>
    public string DataVersion { get; set; } = "";

    public List<BankedPokemon> Pokemon { get; set; } = new();

    /// <summary>The moves to and from a slot under way (<see cref="HomeMoves"/>): null, left out, with none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<MoveReceipt>? Receipts { get; set; }

    /// <summary>An empty Home box of this game's data.</summary>
    public static HomeBank Empty() => new() { DataVersion = GameDataFiles.Version };

    public int Count => Pokemon.Count;
    public bool IsFull => Pokemon.Count >= Capacity;

    /// <summary>Whether a Pokémon of this id is in the Home box.</summary>
    public bool Holds(Guid id) => Pokemon.Any(p => p.Pokemon.Id == id);

    /// <summary>The Pokémon at a place, or null.</summary>
    public BankedPokemon? At(int box, int slot) => Pokemon.FirstOrDefault(p => p.Box == box && p.Slot == slot);

    /// <summary>The Pokémon in place order: box by box, place by place.</summary>
    public IEnumerable<BankedPokemon> Ordered => Pokemon.OrderBy(p => p.Box).ThenBy(p => p.Slot);

    /// <summary>The first free place, or null when the Home box is full.</summary>
    public (int Box, int Slot)? FirstFree()
    {
        var taken = Pokemon.Select(p => (p.Box, p.Slot)).ToHashSet();
        for (int b = 0; b < BoxCount; b++)
            for (int s = 0; s < BoxSize; s++)
                if (!taken.Contains((b, s))) return (b, s);
        return null;
    }

    /// <summary>Puts a Pokémon in the first free place; false when there is none.</summary>
    public bool Put(SavedPokemonData pokemon, bool randomised, string? from)
    {
        if (FirstFree() is not { } place) return false;
        Pokemon.Add(new BankedPokemon { Box = place.Box, Slot = place.Slot, Pokemon = pokemon, Randomised = randomised, From = from });
        return true;
    }

    /// <summary>Takes a Pokémon out by its id; what was taken, or null.</summary>
    public BankedPokemon? Take(Guid id)
    {
        var banked = Pokemon.FirstOrDefault(p => p.Pokemon.Id == id);
        if (banked != null) Pokemon.Remove(banked);
        return banked;
    }

    // ------------------------------------------------------------------ the file

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    /// <summary>The file's text, sealed with its checksum.</summary>
    public static string Write(HomeBank bank)
    {
        bank.Checksum = null;
        return SaveManager.Seal(JsonSerializer.Serialize(bank, Options));
    }

    /// <summary>
    /// Writes the Home box as a save is written: to a temporary file first, flushed, the old file (if whole) to its
    /// backup, the temporary file moved over it. False, and nothing changed, if anything of it fails.
    /// </summary>
    public static bool Save(HomeBank bank, string path)
    {
        string temp = SaveManager.TempOf(path);
        try
        {
            if (Path.GetDirectoryName(path) is { Length: > 0 } folder) Directory.CreateDirectory(folder);
            byte[] utf8 = Encoding.UTF8.GetBytes(Write(bank));
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(utf8);
                stream.Flush(flushToDisk: true);
            }
            if (File.Exists(path) && SaveManager.Intact(File.ReadAllText(path))) File.Copy(path, SaveManager.BackupOf(path), overwrite: true);
            File.Move(temp, path, overwrite: true);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Reads the Home box: an empty one where there is no file, the backup where the file is damaged, nothing where
    /// neither is whole, and nothing (<see cref="BankLoad.OtherData"/>) where it was written under other data than
    /// <paramref name="dataVersion"/> (the game's own when left out).
    /// </summary>
    public static (BankLoad Outcome, HomeBank? Bank) Load(string path, string? dataVersion = null)
    {
        string backup = SaveManager.BackupOf(path);
        bool hasFile = File.Exists(path), hasBackup = File.Exists(backup);
        if (!hasFile && !hasBackup) return (BankLoad.None, Empty());
        var outcome = BankLoad.Ok;
        var bank = hasFile ? ReadWhole(path) : null;
        if (bank == null && hasBackup)
        {
            bank = ReadWhole(backup);
            outcome = BankLoad.Recovered;
        }
        if (bank == null) return (BankLoad.Damaged, null);
        if (bank.DataVersion != (dataVersion ?? GameDataFiles.Version)) return (BankLoad.OtherData, null);
        return (outcome, bank);
    }

    private static HomeBank? ReadWhole(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            return SaveManager.Intact(json) ? JsonSerializer.Deserialize<HomeBank>(json) : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}

/// <summary>A Pokémon that came into a save by a trade (plan 12 · Q28): its id and the species it was traded for.</summary>
public sealed class TradeArrival
{
    public Guid Id { get; set; }

    /// <summary>The species given for it, which a trade evolution may ask (Karrablast for Shelmet).</summary>
    public string? For { get; set; }
}
