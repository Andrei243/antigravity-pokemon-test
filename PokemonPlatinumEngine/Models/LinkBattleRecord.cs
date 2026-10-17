using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models;

/// <summary>How a link battle ended for the player.</summary>
public enum LinkOutcome { Win, Loss, Draw }

/// <summary>The battles fought with one friend: won, lost and drawn.</summary>
public sealed class LinkTally
{
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
}

/// <summary>
/// The link battles' record (plan 07 · O2), which the Trainer Card shows as Platinum's shows its Wi-Fi record: wins,
/// losses and draws with each friend, by the friend code the server gives them (<c>1234-5678-9012</c>, plan 07 ·
/// O4). Saved as <c>SaveData.LinkBattles</c>, left out while nothing is in it. Nothing writes it until plan 07 · O6's
/// battles. GPU-free.
/// </summary>
public sealed class LinkBattleRecord
{
    /// <summary>Each friend's battles, by friend code.</summary>
    public Dictionary<string, LinkTally> ByFriend { get; set; } = new(StringComparer.Ordinal);

    [System.Text.Json.Serialization.JsonIgnore] public int Wins => ByFriend.Values.Sum(t => t.Wins);
    [System.Text.Json.Serialization.JsonIgnore] public int Losses => ByFriend.Values.Sum(t => t.Losses);
    [System.Text.Json.Serialization.JsonIgnore] public int Draws => ByFriend.Values.Sum(t => t.Draws);

    /// <summary>Whether no link battle has been fought.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsEmpty => ByFriend.Count == 0;

    /// <summary>A battle with the friend whose code is <paramref name="friendCode"/>, ended as <paramref name="outcome"/>.</summary>
    public void Record(string friendCode, LinkOutcome outcome)
    {
        if (string.IsNullOrWhiteSpace(friendCode)) throw new ArgumentException("A link battle is recorded by the friend's code.", nameof(friendCode));
        if (!ByFriend.TryGetValue(friendCode, out var tally)) ByFriend[friendCode] = tally = new LinkTally();
        switch (outcome)
        {
            case LinkOutcome.Win: tally.Wins++; break;
            case LinkOutcome.Loss: tally.Losses++; break;
            default: tally.Draws++; break;
        }
    }

    /// <summary>The battles fought with one friend; nothing for one never battled.</summary>
    public LinkTally With(string friendCode) => ByFriend.TryGetValue(friendCode, out var tally) ? tally : new LinkTally();

    /// <summary>The record as a save keeps it: null while it is empty.</summary>
    public LinkBattleRecord? ToSave() => IsEmpty ? null : this;

    /// <summary>A saved record, or an empty one for a save that has none.</summary>
    public static LinkBattleRecord From(LinkBattleRecord? saved)
    {
        var record = new LinkBattleRecord();
        if (saved == null) return record;
        foreach (var (code, tally) in saved.ByFriend)
            if (!string.IsNullOrWhiteSpace(code)) record.ByFriend[code] = new LinkTally { Wins = Math.Max(0, tally.Wins), Losses = Math.Max(0, tally.Losses), Draws = Math.Max(0, tally.Draws) };
        return record;
    }
}
