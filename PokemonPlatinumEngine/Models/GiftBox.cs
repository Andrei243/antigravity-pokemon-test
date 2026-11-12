using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// <c>Data/gifts.json</c> (plan 40 · GE7; ours; <c>docs/data-files.md</c>, "The gift box"): the gifts the box hands out.
/// Read strictly; a pack's <c>gifts.json</c> is laid over the game's by id (<see cref="GiftBox.All"/>).
/// </summary>
public sealed class GiftsFile
{
    [DataRequired] public List<Gift> Gifts { get; set; } = new();
}

/// <summary>
/// One gift: its id (also the key of its words in <c>ui.json</c>, <c>giftBox.gift.&lt;id&gt;.sender</c>, <c>.title</c> and
/// <c>.card</c>), its terms, its picture on the card and the script of <c>Data/scripts/gifts.txt</c> that hands it over
/// (through the host's own <c>give</c>, <c>givepokemon</c>, <c>giveegg</c>, <c>seal give</c>, <c>ribbon give</c> and
/// <c>giveaccessory</c>, so the randomiser, a run's rules and the region's levels have their say as everywhere).
/// </summary>
public sealed class Gift
{
    [DataRequired] public string Id { get; set; } = "";
    [DataRequired] public GiftTerms When { get; set; } = new();
    [DataRequired] public GiftPicture Picture { get; set; } = new();
    [DataRequired] public string Script { get; set; } = "";

    /// <summary>Taken once a year (a date's, a festival's, the save's anniversary); otherwise once for good.</summary>
    public bool Yearly { get; set; }
}

/// <summary>
/// When a gift waits: every term given must hold (a gift with none never waits). Days of the year are written
/// <c>MM-dd</c>, a window <c>MM-dd..MM-dd</c> (which may cross the year's end).
/// </summary>
public sealed class GiftTerms
{
    /// <summary>Days of the year it waits on: <c>01-01</c>, or a window <c>12-24..12-26</c>.</summary>
    public List<string>? Dates { get; set; }

    /// <summary>A festival's days (<see cref="Festivals"/>, plan 40 · GE9): <c>lanterns</c>, <c>easter</c>.</summary>
    public string? Festival { get; set; }

    /// <summary>A region's Hall of Fame entered, by the region's id.</summary>
    public string? HallOfFame { get; set; }

    /// <summary>At least this many species caught (the National Pokédex's count, ours left out).</summary>
    public int? Caught { get; set; }

    /// <summary>At least this many hours played.</summary>
    public int? Hours { get; set; }

    /// <summary>The save's anniversary: a year since the game began, and once for every year after.</summary>
    public bool Anniversary { get; set; }

    /// <summary>Two words of the phrase picker told at a courier (<c>small:sparrow</c>, <c>small:clockwork</c>), in either order.</summary>
    public List<string>? Words { get; set; }
}

/// <summary>What a gift's card shows: a Pokémon or an Egg of a species, an item, a Seal, a backdrop or a ribbon, by name.</summary>
public sealed class GiftPicture
{
    /// <summary><c>pokemon</c>, <c>egg</c>, <c>item</c>, <c>seal</c>, <c>backdrop</c> or <c>ribbon</c>.</summary>
    [DataRequired] public string Kind { get; set; } = "";
    [DataRequired] public string Name { get; set; } = "";
}

/// <summary>What the game says of the player for the gifts' terms: the day, the Halls of Fame, the counts, the save's first day, the run.</summary>
public sealed class GiftFacts
{
    public DateTime Today { get; init; } = GameClock.Today;
    public Func<string, bool> HallOfFame { get; init; } = _ => false;
    public int Caught { get; init; }
    public double HoursPlayed { get; init; }
    public DateTime? Started { get; init; }
    public RunRules Run { get; init; } = RunRules.Normal;
    /// <summary>Whether the team has room for a Pokémon or an Egg (an Egg never goes to the boxes).</summary>
    public bool TeamHasRoom { get; init; } = true;
}

/// <summary>What the box has handed over (<see cref="SaveData.Gifts"/>): each gift's days taken, and the words told.</summary>
public sealed class GiftsSave
{
    /// <summary>The days each gift was taken on, <c>yyyy-MM-dd</c>, by gift id.</summary>
    public Dictionary<string, List<string>> Taken { get; set; } = new();

    /// <summary>The gifts whose words have been told at a courier and that wait until taken.</summary>
    public List<string> Told { get; set; } = new();
}

/// <summary>
/// The gift box (plan 40 · GE7; <c>docs/mechanics/rulings.md</c>, "The gift box"), GPU-free: what waits for the player at a
/// courier, by date, by milestone and by words, and what was taken. Nothing is drawn: a gift is due from the facts alone,
/// so a day asked twice says the same.
/// </summary>
public sealed class GiftBox
{
    public const string FileName = "gifts.json";

    /// <summary>The flag the questionnaire in Jubilife TV's lobby sets: the couriers hand out what waits from then on.</summary>
    public const string OpenFlag = "FLAG_GIFT_BOX_OPEN";

    private static readonly Lazy<List<Gift>> all = new(() => Read(DataLayers.All));

    /// <summary>Every gift of the game and of the packs in use, a pack's laid over the game's by id, in the game's order and then the packs' new ones.</summary>
    public static IReadOnlyList<Gift> All => all.Value;

    public static Gift? Get(string id) => All.FirstOrDefault(g => g.Id == id);

    /// <summary>The gifts of these layers' <c>gifts.json</c>, each read strictly, a later layer's gift replacing an earlier's of its id.</summary>
    public static List<Gift> Read(IEnumerable<DataLayer> layers)
    {
        var gifts = new List<Gift>();
        foreach (var layer in layers)
        {
            string path = Path.Combine(layer.Root, FileName);
            if (!File.Exists(path)) continue;
            foreach (var gift in GameDataFiles.ReadStrict<GiftsFile>(File.ReadAllText(path), FileName).Gifts)
            {
                int at = gifts.FindIndex(g => g.Id == gift.Id);
                if (at >= 0) gifts[at] = gift;
                else gifts.Add(gift);
            }
        }
        return gifts;
    }

    private readonly Dictionary<string, List<DateTime>> taken = new(StringComparer.Ordinal);
    private readonly List<string> told = new();
    private readonly IReadOnlyList<Gift> gifts;

    public GiftBox() : this(null) { }

    /// <summary>A box over the gifts given (a test's), or the game's.</summary>
    public GiftBox(IReadOnlyList<Gift>? gifts) => this.gifts = gifts ?? All;

    public IReadOnlyList<Gift> Gifts => gifts;

    /// <summary>The gifts whose words have been told and not yet taken.</summary>
    public IReadOnlyList<string> Told => told;

    public void Clear()
    {
        taken.Clear();
        told.Clear();
    }

    public void Load(GiftsSave? save)
    {
        Clear();
        if (save == null) return;
        foreach (var (id, days) in save.Taken)
            taken[id] = days.Select(d => DateTime.ParseExact(d, "yyyy-MM-dd", CultureInfo.InvariantCulture)).ToList();
        told.AddRange(save.Told.Distinct());
    }

    /// <summary>What a save keeps: null while nothing was taken or told, so an older save and a new one read alike.</summary>
    public GiftsSave? ToSave() => taken.Count == 0 && told.Count == 0 ? null : new GiftsSave
    {
        Taken = taken.ToDictionary(t => t.Key, t => t.Value.Select(d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).ToList()),
        Told = told.ToList()
    };

    /// <summary>The days a gift was taken on.</summary>
    public IReadOnlyList<DateTime> TakenOn(string id) => taken.TryGetValue(id, out var days) ? days : Array.Empty<DateTime>();

    /// <summary>The gifts taken, each with the last day it was taken on, the latest first: the album's cards.</summary>
    public IReadOnlyList<(Gift Gift, DateTime Day)> Album() =>
        gifts.Where(g => taken.ContainsKey(g.Id)).Select(g => (g, taken[g.Id].Max())).OrderByDescending(c => c.Item2).ThenBy(c => c.g.Id, StringComparer.Ordinal).ToList();

    /// <summary>Whether a gift's terms hold today (taken or not).</summary>
    public bool Holds(Gift gift, GiftFacts facts)
    {
        var w = gift.When;
        bool any = false;
        if (w.Dates is { Count: > 0 })
        {
            any = true;
            if (!w.Dates.Any(d => OnDate(d, facts.Today))) return false;
        }
        if (w.Festival is { } festival)
        {
            any = true;
            if (Festivals.Get(festival) is not { } f || !Festivals.Keeps(f, facts.Today)) return false;
        }
        if (w.HallOfFame is { } region)
        {
            any = true;
            if (!facts.HallOfFame(region)) return false;
        }
        if (w.Caught is { } caught)
        {
            any = true;
            if (facts.Caught < caught) return false;
        }
        if (w.Hours is { } hours)
        {
            any = true;
            if (facts.HoursPlayed < hours) return false;
        }
        if (w.Anniversary)
        {
            any = true;
            if (AnniversaryYear(facts) < 1) return false;
        }
        if (w.Words is { Count: > 0 })
        {
            any = true;
            if (!told.Contains(gift.Id)) return false;
        }
        return any;
    }

    /// <summary>Whether a gift has been taken for the turn it is in: once for good, or this year (this anniversary).</summary>
    public bool IsTaken(Gift gift, GiftFacts facts)
    {
        var days = TakenOn(gift.Id);
        if (days.Count == 0) return false;
        if (!gift.Yearly) return true;
        if (gift.When.Anniversary && facts.Started is { } started)
            return days.Any(d => YearsSince(started, d) == AnniversaryYear(facts));
        return days.Any(d => d.Year == facts.Today.Year);
    }

    /// <summary>
    /// Why a gift that holds can't be handed over now, or null: an Egg or a Pokémon with no room on the team, or a Pokémon
    /// whose types a Monotype run refuses (plan 13 · V5). It waits until it can.
    /// </summary>
    public static string? Refused(Gift gift, GiftFacts facts)
    {
        bool pokemon = gift.Picture.Kind is "pokemon" or "egg";
        if (!pokemon) return null;
        if (!facts.TeamHasRoom) return "room";
        if (facts.Run.Monotype is { } type && PokemonDatabase.Get(gift.Picture.Name) is { } species
            && species.PrimaryType != type && species.SecondaryType != type) return "run";
        return null;
    }

    /// <summary>The gifts that wait at a courier now, in the box's order: terms held, not taken, not refused.</summary>
    public IReadOnlyList<Gift> Due(GiftFacts facts) => gifts.Where(g => Holds(g, facts) && !IsTaken(g, facts) && Refused(g, facts) == null).ToList();

    /// <summary>The gifts that hold and aren't taken but are refused for now (a full team, a run): what the courier keeps back.</summary>
    public IReadOnlyList<Gift> Waiting(GiftFacts facts) => gifts.Where(g => Holds(g, facts) && !IsTaken(g, facts) && Refused(g, facts) != null).ToList();

    /// <summary>Notes a gift taken today; its words told are forgotten.</summary>
    public void Take(Gift gift, DateTime today)
    {
        if (!taken.TryGetValue(gift.Id, out var days)) taken[gift.Id] = days = new List<DateTime>();
        days.Add(today.Date);
        told.Remove(gift.Id);
    }

    /// <summary>
    /// Words told at a courier: every gift of those two words (in either order) not yet taken is told, and waits. The gifts
    /// told, none for words that call nothing up.
    /// </summary>
    public IReadOnlyList<Gift> Tell(IReadOnlyList<string>? words, GiftFacts facts)
    {
        var heard = new List<Gift>();
        if (words is not { Count: 2 }) return heard;
        foreach (var gift in gifts)
            if (gift.When.Words is { Count: 2 } want && SameWords(want, words) && !IsTaken(gift, facts) && !told.Contains(gift.Id))
            {
                told.Add(gift.Id);
                heard.Add(gift);
            }
        return heard;
    }

    /// <summary>Two pairs of words alike in either order.</summary>
    public static bool SameWords(IReadOnlyList<string> a, IReadOnlyList<string> b) =>
        a.Count == 2 && b.Count == 2 && (a[0] == b[0] && a[1] == b[1] || a[0] == b[1] && a[1] == b[0]);

    /// <summary>Whether a day of the year (<c>MM-dd</c>) or a window (<c>MM-dd..MM-dd</c>) keeps a date.</summary>
    public static bool OnDate(string term, DateTime day)
    {
        string[] parts = term.Split("..");
        var festival = new Festival { Id = "", From = parts[0], To = parts.Length > 1 ? parts[1] : parts[0] };
        return Festivals.Keeps(festival, day);
    }

    /// <summary>Whole years from the save's first day to today: 1 on and after its first anniversary.</summary>
    public static int AnniversaryYear(GiftFacts facts) => facts.Started is { } started ? YearsSince(started, facts.Today) : 0;

    private static int YearsSince(DateTime from, DateTime to)
    {
        int years = to.Year - from.Year;
        if (to.Date < from.Date.AddYears(years)) years--;
        return Math.Max(0, years);
    }
}
