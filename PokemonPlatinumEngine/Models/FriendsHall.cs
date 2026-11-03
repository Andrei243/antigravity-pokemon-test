using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// One friend's trainer beaten in the friends' hall, as a save keeps it (plan 13 · V23): the file's checksum and the day.
/// </summary>
public sealed class SavedFriendBeaten
{
    public string Checksum { get; set; } = "";
    public DateTime Day { get; set; }
}

/// <summary>
/// Who of the friends' hall has been beaten, and on which day (plan 13 · V23): a trainer is beaten once a day, told by
/// their file's checksum (a file changed is another trainer). Only the days that count are kept: a save keeps today's,
/// since yesterday's beating no longer holds anyone back. GPU-free.
/// </summary>
public sealed class FriendsHallState
{
    private readonly Dictionary<string, DateTime> beaten = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether the trainer of <paramref name="checksum"/> was beaten on <paramref name="day"/>.</summary>
    public bool BeatenOn(string checksum, DateTime day) => beaten.TryGetValue(checksum, out var when) && when.Date == day.Date;

    /// <summary>The trainer of <paramref name="checksum"/> beaten on <paramref name="day"/>.</summary>
    public void Beat(string checksum, DateTime day) => beaten[checksum] = day.Date;

    /// <summary>How many are kept.</summary>
    public int Count => beaten.Count;

    /// <summary>What a save keeps: those beaten on <paramref name="today"/>, in checksum order; null when there are none.</summary>
    public List<SavedFriendBeaten>? ToSaved(DateTime today)
    {
        var kept = beaten.Where(b => b.Value.Date == today.Date).OrderBy(b => b.Key, StringComparer.Ordinal)
            .Select(b => new SavedFriendBeaten { Checksum = b.Key, Day = b.Value.Date }).ToList();
        return kept.Count == 0 ? null : kept;
    }

    /// <summary>A save's list read back; a malformed entry is passed over.</summary>
    public static FriendsHallState From(IEnumerable<SavedFriendBeaten>? saved)
    {
        var state = new FriendsHallState();
        foreach (var entry in saved ?? Enumerable.Empty<SavedFriendBeaten>())
            if (entry.Checksum.Length == 8 && entry.Checksum.All(Uri.IsHexDigit)) state.Beat(entry.Checksum.ToUpperInvariant(), entry.Day);
        return state;
    }
}

/// <summary>
/// The friends' hall (plan 13 · V23, decision 11; <c>docs/mechanics/rulings.md</c>, "The friends' hall"): the floor of
/// Jubilife City's Pokémon Center upstairs, before the closed link desks, where the trainers of the files in
/// <c>trainers/</c> beside the save (plan 13 · V22) stand as the room is come to, at most <see cref="Most"/>, in the
/// order of their files' names, each on a spot of its own. Each is spoken to, says the line their file chose, battles at
/// level 50 with every routine of the trainer AI on copies of both teams, and is beaten once a day. Nothing is won,
/// paid or used up. GPU-free.
/// </summary>
public static class FriendsHall
{
    /// <summary>The room the friends stand in.</summary>
    public const string Room = "JubilifePokemonCenter2F";

    /// <summary>The most trainers that stand at once: one to a spot.</summary>
    public const int Most = 8;

    /// <summary>What a friend's person's key begins with: the file's checksum follows.</summary>
    public const string KeyPrefix = "friend_";

    /// <summary>The script a friend runs when spoken to (<c>common.txt</c>).</summary>
    public const string Script = "FriendsHallTrainer";

    /// <summary>
    /// The spots, in the order they are filled: two rows of four on the open floor south of the link desks, clear of the
    /// stairs' landing on the west and of the way between them, each facing south toward whoever comes up.
    /// </summary>
    public static readonly IReadOnlyList<(int X, int Y)> Spots = new[]
    {
        (5, 7), (8, 7), (11, 7), (14, 7),
        (5, 9), (8, 9), (11, 9), (14, 9)
    };

    /// <summary>
    /// The trainers that stand, each with their spot: the files the folder read that can battle, in the folder's order
    /// (<see cref="TrainerFiles.In"/>, by name), the same file once, at most <see cref="Most"/>. A refused file stands nobody.
    /// </summary>
    public static IReadOnlyList<(TrainerFile File, (int X, int Y) Spot)> Stand(IReadOnlyList<TrainerFiles.Entry> folder)
    {
        var standing = new List<(TrainerFile, (int, int))>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in folder)
        {
            if (standing.Count == Most) break;
            if (entry.File is not { } file || entry.Refused != null || !seen.Add(file.Checksum)) continue;
            standing.Add((file, Spots[standing.Count]));
        }
        return standing;
    }

    /// <summary>The person a friend's trainer is in the room: their look, their class and name, the hall's script.</summary>
    public static NPC PersonOf(TrainerFile file, (int X, int Y) spot) => new()
    {
        Id = KeyPrefix + file.Checksum,
        Key = KeyPrefix + file.Checksum,
        Name = file.Name,
        NpcType = PlayerIdentity.CharacterOf(file.Look),
        GridX = spot.X,
        GridY = spot.Y,
        Facing = Direction.Down,
        Script = FriendsHall.Script
    };

    /// <summary>Whether a person of a map is a friend the hall stood (not anyone of the map's own).</summary>
    public static bool IsFriend(NPC person) => person.Key?.StartsWith(KeyPrefix, StringComparison.Ordinal) == true;
}
