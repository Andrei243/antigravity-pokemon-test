using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>The ranks there are cards of (plan 43 · LF2).</summary>
public enum CardRank { Leader, EliteFour, Champion }

/// <summary>
/// One card of the album (plan 43 · LF2), as the save keeps it: the id of the team first beaten, the name as the battle
/// called them (a rival's as the player named him then) and the look the battle drew them in, which the data can't tell
/// (a Leader's class names nobody: the look is the person's who stood in the map). Everything else (their rank and
/// region, their team, their colours and their line) is made from the data by the id.
/// </summary>
public sealed class AlbumCard
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Look { get; set; } = "";
}

/// <summary>
/// The cards of the people beaten (plan 43 · LF2; the user asked for it on 2026-11-22): a card is given once, as the
/// first battle with a Gym Leader, one of an Elite Four or a Champion of any region is won. A person is one card
/// whatever team of theirs was beaten (their rematches', their teams beyond the League, their Mega Max ones): a person
/// is told by their region, their rank and the name the data gives them, so Koga as Kanto's Leader and Koga of Johto's
/// Four are two. GPU-free; nothing here draws from any chance.
/// </summary>
public sealed class CardAlbum
{
    private readonly List<AlbumCard> cards = new();

    /// <summary>The cards in the order they were won.</summary>
    public IReadOnlyList<AlbumCard> Cards => cards;

    /// <summary>
    /// The rank a trainer's class gives a card of, or null for anyone else: the classes the way into a battle is told by
    /// (<see cref="BattleMeeting.ClassRow"/>), and Black 2's, whose class names the person after it ("Leader Elesa",
    /// "Elite Four Shauntal"). Team leaders (an "Aqua Leader") are no Gym Leaders.
    /// </summary>
    public static CardRank? RankOf(string trainerClass) => BattleMeeting.ClassRow(trainerClass) switch
    {
        MeetingRow.Leader => CardRank.Leader,
        MeetingRow.EliteFour => CardRank.EliteFour,
        MeetingRow.Champion => CardRank.Champion,
        _ => trainerClass.StartsWith("Leader ", StringComparison.Ordinal) ? CardRank.Leader
            : trainerClass.StartsWith("Elite Four ", StringComparison.Ordinal) ? CardRank.EliteFour
            : null
    };

    /// <summary>The plain class of a rank, by which the table of the bands knows a person.</summary>
    public static string ClassOf(CardRank rank) => rank switch
    {
        CardRank.Leader => "Leader",
        CardRank.EliteFour => "Elite Four",
        _ => "Champion"
    };

    /// <summary>
    /// The region a team of the data's belongs to: the region whose own table has it, else the region its id begins with
    /// (<c>hoenn_sidney_rematch</c> of our own trainers), else Sinnoh, whose table is Platinum's and has no prefix.
    /// </summary>
    public static string RegionOf(string trainerId)
    {
        foreach (string region in TrainerDatabase.Regions)
            if (TrainerDatabase.OfRegion(region).Any(t => string.Equals(t.Id, trainerId, StringComparison.OrdinalIgnoreCase)))
                return RegionDatabase.Get(region)?.Id ?? region;
        foreach (var region in RegionDatabase.All)
            if (trainerId.StartsWith(region.Id + "_", StringComparison.OrdinalIgnoreCase))
                return region.Id;
        return RegionDatabase.Sinnoh;
    }

    /// <summary>The person a team is of: their region, rank and the data's name; null for a team no card is made of (or none of the data's).</summary>
    public static (string Region, CardRank Rank, string Name)? PersonOf(string? trainerId)
    {
        if (TrainerDatabase.Get(trainerId) is not { } record || RankOf(record.Class) is not { } rank) return null;
        return (RegionOf(record.Id), rank, record.Name);
    }

    /// <summary>
    /// A battle with this team was won: their card, if the team is of a Leader, an Elite Four or a Champion of the data
    /// and the album has no card of that person yet. A friend's team, and one a map or a tool made up, is none of the
    /// data's and gives nothing.
    /// </summary>
    /// <returns>The card given, or null.</returns>
    public AlbumCard? Give(string? trainerId, string name, string look)
    {
        if (PersonOf(trainerId) is not { } person || Has(person)) return null;
        var card = new AlbumCard { Id = TrainerDatabase.Get(trainerId)!.Id, Name = name, Look = look };
        cards.Add(card);
        return card;
    }

    /// <summary>Whether the album has the card of this person.</summary>
    public bool Has((string Region, CardRank Rank, string Name) person) => cards.Any(c => PersonOf(c.Id) == person);

    /// <summary>
    /// The cards as the album shows them: by region, in the journey's order (the side regions after it), and within a
    /// region in the order they were won. A card whose team the data no longer has (a pack taken away) is kept and not shown.
    /// </summary>
    public IReadOnlyList<AlbumCard> Shown()
    {
        var order = RegionDatabase.All.Select((r, i) => (r.Id, i)).ToDictionary(p => p.Id, p => p.i, StringComparer.OrdinalIgnoreCase);
        return cards.Select(c => (Card: c, Person: PersonOf(c.Id)))
            .Where(p => p.Person != null)
            .OrderBy(p => order.TryGetValue(p.Person!.Value.Region, out int at) ? at : int.MaxValue)
            .Select(p => p.Card).ToList();
    }

    /// <summary>The album as a save kept it; none for a save without one.</summary>
    public void Load(IEnumerable<AlbumCard>? saved)
    {
        cards.Clear();
        foreach (var c in saved ?? Enumerable.Empty<AlbumCard>())
            if (c is { Id.Length: > 0 }) cards.Add(new AlbumCard { Id = c.Id, Name = c.Name ?? "", Look = c.Look ?? "" });
    }

    /// <summary>What the save keeps: null while the album is empty.</summary>
    public List<AlbumCard>? ToSave() => cards.Count == 0 ? null : cards.Select(c => new AlbumCard { Id = c.Id, Name = c.Name, Look = c.Look }).ToList();
}
