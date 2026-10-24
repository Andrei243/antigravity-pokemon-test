using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>A row of the Bug-Catching Contest's table of wild Pokémon (HeartGold's <c>BUGMON</c>).</summary>
public sealed class BugContestSlot
{
    public string Species { get; set; } = "";
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; }

    /// <summary>Where the slot begins on a roll of a hundred: the first row whose rate the roll reaches is met.</summary>
    public int Rate { get; set; }

    /// <summary>What the species is worth to the judges.</summary>
    public int Score { get; set; }
}

/// <summary>One of a contestant's eight rows of what they may have caught (HeartGold's <c>BugContestantData</c>).</summary>
public sealed class BugContestantRow
{
    /// <summary>Only once the player has the National Pokédex.</summary>
    public bool National { get; set; }

    /// <summary>The weekday it is drawn on (0 Sunday), or <see cref="BugContestVenue.AnyDay"/>.</summary>
    public int Day { get; set; }

    public string Species { get; set; } = "";
    public int Score { get; set; }

    /// <summary>How far either way the score is moved by a draw (the original's <c>randmod</c>).</summary>
    public int Spread { get; set; }
}

/// <summary>One of the ten the original draws the contest's five other entrants from.</summary>
public sealed class BugContestantData
{
    public string Name { get; set; } = "";

    /// <summary>The trainer class, as the trainer table writes it (the results say it before the name).</summary>
    public string Class { get; set; } = "";

    /// <summary>The look they stand in the park as.</summary>
    public string Look { get; set; } = "";

    public List<BugContestantRow> Rows { get; set; } = new();
}

/// <summary>
/// The Bug-Catching Contest's venue as data (plan 15 · E5; <c>Data/world/johto/bug-contest.json</c>, HeartGold's
/// numbers): the days it is held, the balls and minutes, the four tables of wild Pokémon and the ten contestants.
/// </summary>
public sealed class BugContestVenue
{
    /// <summary>A contestant's row for any day (the original's <c>RTC_WEEK_MAX</c>).</summary>
    public const int AnyDay = 7;

    public List<int> Days { get; set; } = new();
    public int Balls { get; set; } = 20;
    public int Minutes { get; set; } = 20;
    public List<List<BugContestSlot>> Encounters { get; set; } = new();
    public List<BugContestantData> Contestants { get; set; } = new();

    private static readonly Lazy<BugContestVenue> park = new(() =>
        GameDataFiles.Load<BugContestVenue>(Path.Combine(World.Folder, "johto", "bug-contest.json")));

    /// <summary>The National Park's, the one venue there is.</summary>
    public static BugContestVenue NationalPark => park.Value;

    /// <summary>Whether a contest is held on this weekday (HeartGold's Tuesday, Thursday and Saturday).</summary>
    public bool OpenOn(DayOfWeek day) => Days.Contains((int)day);

    /// <summary>
    /// The table met on this day (<c>BugContest_InitEncounters</c>): the first before the National Pokédex, and after it
    /// the weekday halved (Tuesday's, Thursday's or Saturday's; a day the contest isn't held takes the nearest below).
    /// </summary>
    public int TableFor(int weekday, bool nationalDex) => nationalDex ? Math.Clamp(weekday / 2, 0, Encounters.Count - 1) : 0;
}

/// <summary>One of the five other entrants of a contest: who they are by their place in the venue's list, what they caught and its score.</summary>
public sealed record BugContestant(int Id, string Species, int Score);

/// <summary>A place on the results' board: who (an entrant's id, or <see cref="BugContest.Player"/>), their catch and score.</summary>
public sealed record BugContestPlace(int Id, string Species, int Score);

/// <summary>
/// The Bug-Catching Contest as HeartGold plays it (plan 15 · E5; <c>src/overlay_bug_contest.c</c>; the rulings'
/// "The Bug-Catching Contest"): the team set aside but for its first Pokémon able to fight, twenty Sport Balls and
/// twenty minutes in the National Park, the one Pokémon kept, five others drawn from ten with what they caught, the
/// judging by HeartGold's own score and the prizes. No drawing or input: the scripts start, judge and end it
/// (<c>bugcontest</c>), the engine counts its minutes while the field or a battle is on screen. Every draw is on the
/// generator it is handed, in the original's order.
/// </summary>
public sealed class BugContest
{
    /// <summary>The player's place among the entrants: after the five (the original's <c>BUGCONTESTANT_PLAYER</c>).</summary>
    public const int Player = 5;

    /// <summary>How many others enter (the original's <c>BUGCONTESTANT_NPC_COUNT</c>).</summary>
    public const int Others = 5;

    public BugContestVenue Venue { get; private set; } = BugContestVenue.NationalPark;

    /// <summary>A contest is under way: from the counter's start to the judging's end.</summary>
    public bool Active { get; private set; }

    public int Balls { get; set; }

    /// <summary>The seconds of the contest gone (the original's <c>elapsed_time</c>).</summary>
    public float Elapsed { get; private set; }

    public float Length => Venue.Minutes * 60f;
    public float SecondsLeft => Math.Max(0f, Length - Elapsed);
    public bool TimeUp => Active && Elapsed >= Length;

    /// <summary>The weekday it was entered on, and whether the National Pokédex was the player's then.</summary>
    public int Weekday { get; private set; }
    public bool NationalDex { get; private set; }

    /// <summary>The table of wild Pokémon met, by its place in the venue's four.</summary>
    public int Table { get; private set; }

    public IReadOnlyList<BugContestSlot> Slots => Venue.Encounters[Table];

    /// <summary>The team as it was, set aside while only <see cref="LeadIndex"/>'s Pokémon goes into the park with the player.</summary>
    public List<Pokemon> Team { get; } = new();

    /// <summary>The place in <see cref="Team"/> of the Pokémon that went in (the original's <c>Save_GetPartyLeadAlive</c>).</summary>
    public int LeadIndex { get; private set; }

    /// <summary>The Pokémon kept from the park, if one has been caught.</summary>
    public Pokemon? Caught { get; set; }

    /// <summary>A Pokémon just caught while another is kept: the player is asked which to keep.</summary>
    public Pokemon? Pending { get; set; }

    public List<BugContestant> Entrants { get; } = new();

    /// <summary>The results once judged: six places, best first.</summary>
    public List<BugContestPlace> Ranking { get; } = new();

    /// <summary>The player's place once judged: 0 first, 1 second, 2 third, 3 none of those.</summary>
    public int Placement { get; private set; } = 3;

    /// <summary>The prize the placement wins, by the item's name.</summary>
    public string Prize { get; private set; } = "";

    /// <summary>The player's score once judged.</summary>
    public int Score { get; private set; }

    public bool Judged => Ranking.Count > 0;

    /// <summary>The team had a Pokémon able to go in (<see cref="Start"/> refuses a team with none).</summary>
    public static bool CanEnter(Party party) => party.Members.Any(p => !p.IsFainted);

    /// <summary>
    /// <c>BugContest_New</c>: twenty Sport Balls, the team set aside but for the first Pokémon able to fight
    /// (<c>BugContest_BackUpParty</c>), the five others drawn (<c>BugContest_InitOpponents</c>) and the day's table
    /// (<c>BugContest_InitEncounters</c>). False, with nothing changed, for a team with none able.
    /// </summary>
    public bool Start(Party party, int weekday, bool nationalDex, Random rng, BugContestVenue? venue = null)
    {
        int lead = party.Members.FindIndex(p => !p.IsFainted);
        if (lead < 0) return false;
        Venue = venue ?? BugContestVenue.NationalPark;
        Active = true;
        Balls = Venue.Balls;
        Elapsed = 0f;
        Weekday = weekday;
        NationalDex = nationalDex;
        Caught = null;
        Pending = null;
        Ranking.Clear();
        Placement = 3;
        Prize = "";
        Score = 0;
        Team.Clear();
        Team.AddRange(party.Members);
        LeadIndex = lead;
        var goesIn = party.Members[lead];
        party.Clear();
        party.Add(goesIn);
        DrawEntrants(rng);
        Table = Venue.TableFor(weekday, nationalDex);
        return true;
    }

    /// <summary>
    /// <c>BugContest_InitOpponents</c>: five of the ten, none twice, each a row of theirs fit for the day and the
    /// Pokédex, its score spread by a draw.
    /// </summary>
    private void DrawEntrants(Random rng)
    {
        Entrants.Clear();
        var taken = new List<int>();
        for (int i = 0; i < Others; i++)
        {
            int id;
            do id = rng.Next(Venue.Contestants.Count);
            while (taken.Contains(id));
            taken.Add(id);
            var fit = Venue.Contestants[id].Rows
                .Where(row => (!row.National || NationalDex) && (row.Day >= BugContestVenue.AnyDay || row.Day == Weekday)).ToList();
            var chosen = fit[rng.Next(fit.Count)];
            int spread = Math.Max(1, chosen.Spread);
            int score = rng.Next(2 * spread) - spread + chosen.Score;
            Entrants.Add(new BugContestant(id, chosen.Species, score));
        }
    }

    /// <summary>The seconds the contest has run, while the field or a battle is on screen; held at its end.</summary>
    public void Tick(float dt)
    {
        if (!Active || Judged) return;
        Elapsed = Math.Min(Length, Elapsed + dt);
    }

    /// <summary>
    /// <c>BugContest_GetEncounterSlot</c>: a roll of a hundred, the first row whose rate it reaches, and a level from
    /// the row's least to its most.
    /// </summary>
    public (string Species, int Level) Meet(Random rng)
    {
        int roll = rng.Next(100);
        var slots = Slots;
        int i = 0;
        while (i < slots.Count - 1 && roll < slots[i].Rate) i++;
        var slot = slots[i];
        int level = rng.Next(slot.MaxLevel - slot.MinLevel + 1) + slot.MinLevel;
        return (slot.Species, level);
    }

    /// <summary>
    /// A Pokémon caught in the park: kept at once when none is, and otherwise waiting for the question of which to
    /// keep (<see cref="Pending"/>). True when it asks.
    /// </summary>
    public bool Catch(Pokemon caught)
    {
        if (Caught == null)
        {
            Caught = caught;
            return false;
        }
        Pending = caught;
        return true;
    }

    /// <summary>The answer to the question: the new one kept and the old let go, or the new one let go.</summary>
    public void Keep(bool theNewOne)
    {
        if (Pending == null) return;
        if (theNewOne) Caught = Pending;
        Pending = null;
    }

    /// <summary>
    /// <c>BugContest_JudgePlayerMon</c>: the species' worth, the level against the table's highest, the IVs against
    /// their best and the HP left, each out of a hundred, in whole numbers rounded down after every step; nothing
    /// for a contest with no catch, or for a species the day's table doesn't have. <paramref name="mostLevel"/> is
    /// the table's highest level as the player meets it (a region crossed into maps levels, plan 18 · W4).
    /// </summary>
    public static int Judge(Pokemon? caught, IReadOnlyList<BugContestSlot> table, Func<int, int>? mostLevel = null)
    {
        if (caught == null) return 0;
        var slot = table.FirstOrDefault(s => s.Species == caught.Species.Name);
        if (slot == null) return 0;
        int most = Math.Max(1, mostLevel?.Invoke(slot.MaxLevel) ?? slot.MaxLevel);
        int score = slot.Score;
        score += caught.Level * 100 / most;
        int ivs = caught.IvHP + caught.IvAttack + caught.IvDefense + caught.IvSpAttack + caught.IvSpDefense + caught.IvSpeed;
        score += ivs * 100 / (31 * 6);
        score += caught.CurrentHP * 100 / Math.Max(1, caught.MaxHP);
        return score & 0xFFFF;
    }

    /// <summary>
    /// <c>BugContest_Judge</c>: the player's catch judged, the six put in order by the original's selection sort (a
    /// later entrant takes a tie, so the player wins one against all five), the player's place, and the prize: a
    /// stone for first (the Sun Stone, or one of ten after the National Pokédex, drawn), an Everstone for second, a
    /// Sitrus Berry for third and a Shed Shell for anyone else.
    /// </summary>
    public void JudgeAll(Random rng, Func<int, int>? mostLevel = null)
    {
        Score = Judge(Caught, Slots, mostLevel);
        var all = Entrants.Select(e => new BugContestPlace(e.Id, e.Species, e.Score)).ToList();
        all.Add(new BugContestPlace(Player, Caught?.Species.Name ?? "", Score));
        var order = Enumerable.Range(0, all.Count).ToArray();
        for (int i = 0; i < order.Length - 1; i++)
        {
            int temp = order[i], best = i, score = all[order[i]].Score;
            for (int j = i + 1; j < order.Length; j++)
            {
                int other = all[order[j]].Score;
                if (other >= score)
                {
                    best = j;
                    score = other;
                }
            }
            order[i] = order[best];
            order[best] = temp;
        }
        Ranking.Clear();
        Ranking.AddRange(order.Select(i => all[i]));
        Placement = 3;
        for (int i = 0; i < 3; i++)
            if (Ranking[i].Id == Player)
            {
                Placement = i;
                break;
            }
        Prize = PrizeFor(Placement, NationalDex ? rng.Next(10) : 0);
    }

    /// <summary>The first prize's stones, by the draw (ITEM_SUN_STONE onwards, then ITEM_SHINY_STONE onwards).</summary>
    public static readonly string[] Stones =
        { "Sun Stone", "Moon Stone", "Fire Stone", "Thunder Stone", "Water Stone", "Leaf Stone", "Shiny Stone", "Dusk Stone", "Dawn Stone", "Oval Stone" };

    public static string PrizeFor(int placement, int stone) => placement switch
    {
        0 => Stones[Math.Clamp(stone, 0, Stones.Length - 1)],
        1 => "Everstone",
        2 => "Sitrus Berry",
        _ => "Shed Shell"
    };

    /// <summary>The name an entrant goes by on the board: their class and name, or nothing for the player.</summary>
    public string NameOf(int id) => id >= 0 && id < Venue.Contestants.Count ? Venue.Contestants[id].Name : "";

    public string ClassOf(int id) => id >= 0 && id < Venue.Contestants.Count ? Venue.Contestants[id].Class : "";

    /// <summary>
    /// <c>BugContest_RestoreParty_RetrieveCaughtPokemon</c>: the team as it was, its Pokémon that went in as the park
    /// left it, and the catch, which the caller puts in the team or, with the team full, in the PC (returned). The
    /// contest is over.
    /// </summary>
    public Pokemon? Finish(Party party)
    {
        if (!Active) return null;
        var wentIn = party.Members.FirstOrDefault();
        var team = Team.ToList();
        if (wentIn != null && LeadIndex < team.Count) team[LeadIndex] = wentIn;
        party.Clear();
        foreach (var p in team) party.Add(p);
        var caught = Caught;
        Clear();
        return caught;
    }

    /// <summary>The contest given up with nothing judged (a battle lost in the park): the team put back, the catch let go.</summary>
    public void Abandon(Party party)
    {
        if (!Active) return;
        Caught = null;
        Finish(party);
    }

    /// <summary>Nothing under way and nothing judged, with the team left as it is: a new game's, or a save's with no contest in it.</summary>
    public void Reset()
    {
        Clear();
        ForgetResults();
    }

    private void Clear()
    {
        Active = false;
        Balls = 0;
        Elapsed = 0f;
        Team.Clear();
        Entrants.Clear();
        Caught = null;
        Pending = null;
    }

    /// <summary>Forgets a contest's results, once the board has been shown and the prize handed over.</summary>
    public void ForgetResults()
    {
        Ranking.Clear();
        Placement = 3;
        Prize = "";
        Score = 0;
    }

    // ------------------------------------------------------------------ the save

    public BugContestSave? ToSave() => !Active ? null : new BugContestSave
    {
        Balls = Balls,
        Elapsed = Elapsed,
        Weekday = Weekday,
        NationalDex = NationalDex,
        LeadIndex = LeadIndex,
        Team = Team.Select((p, i) => i == LeadIndex ? null : SavedPokemonData.FromPokemon(p)).ToList(),
        Caught = Caught is { } c ? SavedPokemonData.FromPokemon(c) : null,
        Entrants = Entrants.Select(e => new BugContestantSave { Id = e.Id, Species = e.Species, Score = e.Score }).ToList()
    };

    /// <summary>A contest read back from a save: the team set aside, the Pokémon that went in being the party's own first.</summary>
    public void Resume(BugContestSave save, Party party, Ruleset? rules = null)
    {
        Clear();
        Venue = BugContestVenue.NationalPark;
        Active = true;
        Balls = save.Balls;
        Elapsed = save.Elapsed;
        Weekday = save.Weekday;
        NationalDex = save.NationalDex;
        LeadIndex = Math.Clamp(save.LeadIndex, 0, Math.Max(0, save.Team.Count - 1));
        Table = Venue.TableFor(Weekday, NationalDex);
        var wentIn = party.Members.FirstOrDefault();
        for (int i = 0; i < save.Team.Count; i++)
        {
            if (i == LeadIndex) { if (wentIn != null) Team.Add(wentIn); continue; }
            if (save.Team[i] is { } kept) Team.Add(kept.ToPokemon(rules));
        }
        Caught = save.Caught?.ToPokemon(rules);
        foreach (var e in save.Entrants) Entrants.Add(new BugContestant(e.Id, e.Species, e.Score));
    }
}

/// <summary>A Bug-Catching Contest under way as a save keeps it (plan 15 · E5).</summary>
public sealed class BugContestSave
{
    public int Balls { get; set; }
    public float Elapsed { get; set; }
    public int Weekday { get; set; }
    public bool NationalDex { get; set; }
    public int LeadIndex { get; set; }

    /// <summary>The team set aside, place by place; the place of the Pokémon that went in is null (it is the party's).</summary>
    public List<SavedPokemonData?> Team { get; set; } = new();

    public SavedPokemonData? Caught { get; set; }
    public List<BugContestantSave> Entrants { get; set; } = new();
}

public sealed class BugContestantSave
{
    public int Id { get; set; }
    public string Species { get; set; } = "";
    public int Score { get; set; }
}
