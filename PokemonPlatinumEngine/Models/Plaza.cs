using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>An item a stall sells for pennants, from a rank of the plaza on (<c>Data/plaza.json</c>).</summary>
public sealed class PlazaGoods
{
    [DataRequired] public string Item { get; set; } = "";
    [DataRequired] public int Price { get; set; }
    /// <summary>The plaza's rank from which the stall has it.</summary>
    public int Rank { get; set; } = 1;
}

/// <summary>
/// A stall of Pennant Hall (plan 41 · EG3): its id (its words are <c>plaza.stall.&lt;id&gt;.*</c> in <c>ui.json</c>), the
/// keeper's look, the region whose Hall of Fame opens it (none for any), the plaza's rank it needs, its awning's colour,
/// and what it sells for pennants where it sells anything. Its service is the keeper's own script in
/// <c>Hub_Plaza.txt</c>, which <c>plaza stall</c> tells apart by the stall's place in the catalogue.
/// </summary>
public sealed class PlazaStall
{
    [DataRequired] public string Id { get; set; } = "";
    [DataRequired] public string Keeper { get; set; } = "";
    public string? Region { get; set; }
    public int Rank { get; set; } = 1;
    [DataRequired] public string Awning { get; set; } = "";
    public List<PlazaGoods> Goods { get; set; } = new();
}

/// <summary>A place of the hall a stall is set out at: the booth's north-west tile, three tiles by three.</summary>
public sealed class PlazaPitch
{
    [DataRequired] public int X { get; set; }
    [DataRequired] public int Y { get; set; }
}

/// <summary>A place one of the day's guests stands at.</summary>
public sealed class PlazaSpot
{
    [DataRequired] public int X { get; set; }
    [DataRequired] public int Y { get; set; }
    public Direction Facing { get; set; } = Direction.Down;
}

/// <summary>The file <c>Data/plaza.json</c> (ours, written by hand, read strictly).</summary>
public sealed class PlazaFile
{
    /// <summary>The pennants earned that each rank asks, from the first (0) to the top.</summary>
    [DataRequired] public List<int> Ranks { get; set; } = new();
    /// <summary>How many pitches each rank has, from the first to the top.</summary>
    [DataRequired] public List<int> Pitches { get; set; } = new();
    /// <summary>Where the booths stand, pitch 1 first.</summary>
    [DataRequired] public List<PlazaPitch> Places { get; set; } = new();
    /// <summary>Where the day's guests stand, the first first.</summary>
    [DataRequired] public List<PlazaSpot> Spots { get; set; } = new();
    /// <summary>The trainer classes guests are drawn from, as the regions' tables write them.</summary>
    [DataRequired] public List<string> GuestClasses { get; set; } = new();
    /// <summary>The look a guest of a class stands in, by class; one of each pair by the guest's place, the first otherwise.</summary>
    public Dictionary<string, List<string>> GuestLooks { get; set; } = new();
    /// <summary>The pennants a guest's first word of the day gives, and a battle won against one.</summary>
    [DataRequired] public int GuestWord { get; set; }
    [DataRequired] public int GuestWin { get; set; }
    [DataRequired] public List<PlazaStall> Stalls { get; set; } = new();
}

/// <summary>
/// Pennant Hall, the hub's plaza (plan 41 · EG3; <c>docs/mechanics/rulings.md</c>, "Pennant Hall"), GPU-free: a covered
/// hall whose pitches the player fills with stalls, ranked by the pennants it has earned. The catalogue and every number
/// are ours (<c>Data/plaza.json</c>). The rules are here; what a game's plaza holds is <see cref="PlazaState"/>.
/// </summary>
public static class Plaza
{
    /// <summary>The hall's map, and the scripts its people run (<c>Data/scripts/Hub_Plaza.txt</c>).</summary>
    public const string Map = "Hub_Plaza";

    /// <summary>The people of the hall's file the plaza stands or takes away: a keeper at each pitch and a guest at each spot.</summary>
    public const string KeeperPrefix = "keeper_", GuestPrefix = "guest_";

    /// <summary>The flags that show a pitch's keeper and a spot's guest, which the game sets as the plaza stands (<see cref="KeepFlags"/>).</summary>
    public static string KeeperFlag(int pitch) => $"FLAG_HUB_PLAZA_KEEPER_{pitch}";
    public static string GuestFlag(int spot) => $"FLAG_HUB_PLAZA_GUEST_{spot}";

    private static readonly Lazy<PlazaFile> file = new(() => GameDataFiles.Load<PlazaFile>("plaza.json", strict: true));

    public static PlazaFile File => file.Value;

    public static IReadOnlyList<PlazaStall> Stalls => File.Stalls;

    /// <summary>The highest rank.</summary>
    public static int TopRank => File.Ranks.Count;

    /// <summary>The most pitches the hall has (at the top rank).</summary>
    public static int MostPitches => File.Places.Count;

    /// <summary>Every flag the plaza shows its people by: what the game sets by itself (the content check counts them).</summary>
    public static IEnumerable<string> Flags =>
        Enumerable.Range(1, File.Places.Count).Select(KeeperFlag).Concat(Enumerable.Range(1, File.Spots.Count).Select(GuestFlag));

    public static PlazaStall? Stall(string? id) => Stalls.FirstOrDefault(s => s.Id == id);

    /// <summary>The stall's number (1 first) in the catalogue, which <c>plaza stall</c> gives the keeper's script; 0 for none.</summary>
    public static int NumberOf(string? id) => Stalls.ToList().FindIndex(s => s.Id == id) + 1;

    /// <summary>The plaza's rank from the pennants it has earned: 1 to <see cref="TopRank"/>.</summary>
    public static int RankOf(int earned)
    {
        int rank = 1;
        for (int k = 1; k < File.Ranks.Count; k++)
            if (earned >= File.Ranks[k]) rank = k + 1;
        return rank;
    }

    /// <summary>The pennants still to earn for the next rank; 0 at the top.</summary>
    public static int ToNextRank(int earned)
    {
        int rank = RankOf(earned);
        return rank >= TopRank ? 0 : File.Ranks[rank] - Math.Max(0, earned);
    }

    /// <summary>How many pitches the hall has at a rank: three at the first, eight at the top.</summary>
    public static int PitchesAt(int rank) => File.Pitches[Math.Clamp(rank, 1, File.Pitches.Count) - 1];

    /// <summary>What a stall sells at a rank: every row of its goods from that rank down, in the file's order.</summary>
    public static IReadOnlyList<PlazaGoods> StockAt(PlazaStall stall, int rank) => stall.Goods.Where(g => g.Rank <= rank).ToList();

    /// <summary>Whether a stall may be set out: its region's Hall of Fame entered, and the plaza at its rank.</summary>
    public static bool Offered(PlazaStall stall, StoryState story, int rank) =>
        rank >= stall.Rank && (stall.Region == null || RegionDatabase.Get(stall.Region) is { } region && story.Has(region.StoryCompleteFlag));

    /// <summary>The stalls that may be set out, in the catalogue's order.</summary>
    public static IReadOnlyList<PlazaStall> OfferedNow(StoryState story, int rank) => Stalls.Where(s => Offered(s, story, rank)).ToList();

    /// <summary>The regions of the chain whose Hall of Fame is entered, in the chain's order.</summary>
    public static IReadOnlyList<Region> Finished(StoryState story) =>
        RegionDatabase.All.Where(r => !r.Side && story.Has(r.StoryCompleteFlag)).ToList();

    /// <summary>A region's own trainers: Platinum's table for Sinnoh, the region's world folder's for the rest.</summary>
    public static IEnumerable<TrainerRecord> TrainersOf(Region region) =>
        region.Id == RegionDatabase.Sinnoh ? TrainerDatabase.All : TrainerDatabase.OfRegion(region.Folder);

    /// <summary>
    /// Everyone a guest may be: the trainers of the finished regions' tables whose class is one of the file's, with a team,
    /// and no rematch's second team (the first is the person; the hall meets them once). In the chain's order.
    /// </summary>
    public static IReadOnlyList<TrainerRecord> GuestPool(StoryState story) =>
        Finished(story).SelectMany(TrainersOf)
            .Where(t => t.Party.Count > 0 && File.GuestClasses.Contains(t.Class, StringComparer.OrdinalIgnoreCase) && !TrainerDatabase.IsRematchTeam(t.Id))
            .ToList();

    /// <summary>How many guests a day brings at a rank: two more than the rank, as many as the hall has spots at most.</summary>
    public static int GuestsAt(int rank) => Math.Min(File.Spots.Count, Math.Clamp(rank, 1, TopRank) + 2);

    /// <summary>
    /// The day's guests (<c>DrawGuests</c>): <see cref="GuestsAt"/> trainers of <see cref="GuestPool"/>, none twice, drawn on
    /// the chance handed. Empty with no region finished.
    /// </summary>
    public static List<string> DrawGuests(StoryState story, int rank, Random chance)
    {
        var pool = GuestPool(story).Select(t => t.Id).ToList();
        var drawn = new List<string>();
        int want = Math.Min(GuestsAt(rank), pool.Count);
        while (drawn.Count < want)
        {
            int k = chance.Next(pool.Count);
            drawn.Add(pool[k]);
            pool.RemoveAt(k);
        }
        return drawn;
    }

    /// <summary>The look a guest stands in: their class's from the file, one of a pair by the spot, else a trainer's look of the class's table.</summary>
    public static string LookOf(TrainerRecord guest, int spot)
    {
        if (File.GuestLooks.TryGetValue(guest.Class, out var looks) && looks.Count > 0) return looks[(spot - 1) % looks.Count];
        return "ace_trainer_m";
    }

    /// <summary>The level a guest battles at: the furthest finished region's post-game level (<see cref="LevelLift.PostGameLevel"/>).</summary>
    public static int GuestLevel(StoryState story) => Math.Max(1, LevelLift.PostGameLevel(story));

    /// <summary>A day as the plaza counts them: whole days since the year 2000 of the game's own date.</summary>
    public static int DayOf(DateTime date) => (int)(date.Date - new DateTime(2000, 1, 1)).TotalDays;

    /// <summary>
    /// The flags that show who stands in the hall as the plaza stands: each pitch with a stall its keeper, each guest of the
    /// day at their spot. Set by the game as the hall is come to and whenever a pitch changes.
    /// </summary>
    public static void KeepFlags(StoryState story, PlazaState plaza)
    {
        for (int p = 1; p <= File.Places.Count; p++)
        {
            bool stands = p <= plaza.Pitches.Count && plaza.Pitches[p - 1] != null;
            if (stands) story.Set(KeeperFlag(p)); else story.Unset(KeeperFlag(p));
        }
        for (int g = 1; g <= File.Spots.Count; g++)
        {
            if (g <= plaza.Guests.Count) story.Set(GuestFlag(g)); else story.Unset(GuestFlag(g));
        }
    }

    /// <summary>The number of a pitch's keeper or a spot's guest from their id on the map (<c>keeper_3</c>, <c>guest_2</c>), 0 for anyone else.</summary>
    public static int NumberIn(string? id, string prefix) =>
        id != null && id.StartsWith(prefix, StringComparison.Ordinal) && int.TryParse(id[prefix.Length..], out int n) ? n : 0;
}

/// <summary>What a game's plaza holds, saved as <c>SaveData.Plaza</c> (null until the manager is first spoken to).</summary>
public sealed class PlazaSave
{
    public int Coins { get; set; }
    public int Earned { get; set; }
    public List<string?> Pitches { get; set; } = new();
    public int GuestDay { get; set; }
    public List<string> Guests { get; set; } = new();
    public List<int> Greeted { get; set; } = new();
    public List<int> Battled { get; set; } = new();
}

/// <summary>
/// A game's Pennant Hall (plan 41 · EG3): the pennants held and earned (coins never under nought), the stall at each pitch,
/// and the day's guests with whom the player has greeted and battled today. Plain data, no window.
/// </summary>
public sealed class PlazaState
{
    /// <summary>Whether the hall has been opened, by its manager or by pennants paid in from elsewhere: until then nothing is saved.</summary>
    public bool Opened { get; set; }

    /// <summary>The pennants held, to spend at the stalls.</summary>
    public int Coins { get; private set; }

    /// <summary>Every pennant earned, which ranks the plaza (spending never lowers it).</summary>
    public int Earned { get; private set; }

    /// <summary>The stall at each pitch by its id, null for an empty pitch; as many entries as the hall has pitches at its top.</summary>
    public List<string?> Pitches { get; } = new();

    /// <summary>The day the guests were drawn for (<see cref="Plaza.DayOf"/>), and the guests, by trainer id, spot 1 first.</summary>
    public int GuestDay { get; private set; } = -1;
    public List<string> Guests { get; } = new();

    /// <summary>The spots (1 first) whose guest has given today's word, and whose guest has been battled today.</summary>
    public HashSet<int> Greeted { get; } = new();
    public HashSet<int> Battled { get; } = new();

    public int Rank => Plaza.RankOf(Earned);

    /// <summary>The pennants earned: added to those held and to those that rank the plaza. Nothing for nought or less.</summary>
    public void Give(int n)
    {
        if (n <= 0) return;
        Opened = true;
        Coins += n;
        Earned += n;
    }

    /// <summary>Pennants spent at a stall: false, and nothing spent, when there aren't enough.</summary>
    public bool Spend(int n)
    {
        if (n < 0 || n > Coins) return false;
        Coins -= n;
        return true;
    }

    /// <summary>The stall standing at a pitch (1 first), or null.</summary>
    public string? At(int pitch) => pitch >= 1 && pitch <= Pitches.Count ? Pitches[pitch - 1] : null;

    /// <summary>
    /// A stall set out at a pitch, or the pitch cleared (null). False where the pitch is beyond the rank's, or the stall isn't
    /// offered. A stall already standing elsewhere moves here: each stands at one pitch.
    /// </summary>
    public bool Set(int pitch, string? stall, StoryState story)
    {
        if (pitch < 1 || pitch > Plaza.PitchesAt(Rank)) return false;
        if (stall != null && (Plaza.Stall(stall) is not { } known || !Plaza.Offered(known, story, Rank))) return false;
        while (Pitches.Count < Plaza.MostPitches) Pitches.Add(null);
        if (stall != null)
            for (int k = 0; k < Pitches.Count; k++)
                if (Pitches[k] == stall) Pitches[k] = null;
        Pitches[pitch - 1] = stall;
        Opened = true;
        return true;
    }

    /// <summary>
    /// The guests of a day: drawn once a day on <paramref name="chance"/> (the hall come to again the same day finds the same
    /// guests), with the day's words and battles forgotten as a new day draws. True when they were drawn now.
    /// </summary>
    public bool GuestsFor(DateTime date, StoryState story, Random chance)
    {
        int day = Plaza.DayOf(date);
        if (day == GuestDay) return false;
        GuestDay = day;
        Guests.Clear();
        Guests.AddRange(Plaza.DrawGuests(story, Rank, chance));
        Greeted.Clear();
        Battled.Clear();
        return true;
    }

    /// <summary>The guest standing at a spot (1 first), or null.</summary>
    public TrainerRecord? GuestAt(int spot) =>
        spot >= 1 && spot <= Guests.Count ? TrainerDatabase.Get(Guests[spot - 1]) : null;

    /// <summary>A guest's first word of the day: the pennants it gives, once a day; 0 afterwards and for no guest.</summary>
    public int Greet(int spot)
    {
        if (GuestAt(spot) == null || !Greeted.Add(spot)) return 0;
        Give(Plaza.File.GuestWord);
        return Plaza.File.GuestWord;
    }

    public void Clear() => Load(null);

    public void Load(PlazaSave? save)
    {
        Opened = save != null;
        Coins = Math.Max(0, save?.Coins ?? 0);
        Earned = Math.Max(Coins, save?.Earned ?? 0);
        Pitches.Clear();
        if (save != null)
            foreach (string? stall in save.Pitches.Take(Plaza.MostPitches)) Pitches.Add(Plaza.Stall(stall) != null ? stall : null);
        GuestDay = save?.GuestDay ?? -1;
        Guests.Clear();
        if (save != null) Guests.AddRange(save.Guests.Where(id => TrainerDatabase.Get(id) != null));
        Greeted.Clear();
        Battled.Clear();
        if (save != null)
        {
            Greeted.UnionWith(save.Greeted);
            Battled.UnionWith(save.Battled);
        }
    }

    /// <summary>The plaza as the save keeps it; null until it has been opened.</summary>
    public PlazaSave? ToSave() => !Opened ? null : new PlazaSave
    {
        Coins = Coins,
        Earned = Earned,
        Pitches = Pitches.ToList(),
        GuestDay = GuestDay,
        Guests = Guests.ToList(),
        Greeted = Greeted.OrderBy(n => n).ToList(),
        Battled = Battled.OrderBy(n => n).ToList()
    };
}
