using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Models.Delve;

/// <summary>What a request of the camp's board asks (plan 44 · SD9).</summary>
public enum DelveRequestKind
{
    /// <summary>Someone lost on a floor: reach it and come back up on foot or by the rope.</summary>
    Rescue,
    /// <summary>Bring an item up from the hollow.</summary>
    Find,
    /// <summary>Bring a recruit of a species home from the hollow's end.</summary>
    Befriend,
    /// <summary>Clear the hollow.</summary>
    Clear
}

/// <summary>A request of the board: its kind, its hollow, and the floor (from one), the species or the item it names.</summary>
public sealed record DelveRequest(DelveRequestKind Kind, string Hollow, int Floor = 0, string? Species = null, string? Item = null)
{
    /// <summary>Whether a run of its hollow does what it asks.</summary>
    public bool DoneBy(DelveRun run)
    {
        if (run.Hollow.Key != Hollow) return false;
        return Kind switch
        {
            DelveRequestKind.Rescue => run.Outcome != DelveOutcome.Fallen && run.DeepestFloor >= Floor,
            DelveRequestKind.Find => run.Outcome != DelveOutcome.Fallen && run.Bag.Contains(Item ?? ""),
            DelveRequestKind.Befriend => run.Outcome == DelveOutcome.Cleared && run.RecruitsAtTheEnd.Any(p => p.Species.Name == Species),
            DelveRequestKind.Clear => run.Outcome == DelveOutcome.Cleared,
            _ => false
        };
    }
}

/// <summary>
/// The camp's board (plan 44 · SD9; <c>docs/mechanics/rulings.md</c>, "The delve"): three requests a day, drawn by
/// <see cref="SplitMix"/> from the day's number over the hollows the camp's rank has opened, so a day's board is the
/// same however often it is read. No drawing or input.
/// </summary>
public static class DelveBoard
{
    public const int RequestsADay = 3;

    public static IReadOnlyList<DelveRequest> Of(uint dailyNumber, int rank, IReadOnlyList<DelveHollow>? hollows = null)
    {
        var open = (hollows ?? DelveHollows.All).Where(h => h.Rank <= rank).ToList();
        var list = new List<DelveRequest>();
        if (open.Count == 0) return list;
        var mix = new SplitMix(SplitMix.Hash((int)dailyNumber, 0xDE17E));
        for (int i = 0; i < RequestsADay; i++)
        {
            var h = open[mix.Int(open.Count)];
            var kind = (DelveRequestKind)mix.Int(4);
            var request = kind switch
            {
                DelveRequestKind.Rescue => new DelveRequest(kind, h.Key, Floor: 2 + mix.Int(Math.Max(1, h.Floors - 1))),
                DelveRequestKind.Find => new DelveRequest(kind, h.Key, Item: h.Finds[mix.Int(h.Finds.Count)]),
                DelveRequestKind.Befriend => new DelveRequest(kind, h.Key, Species: h.Residents[mix.Int(h.Residents.Count)]),
                _ => new DelveRequest(kind, h.Key)
            };
            // (a rescue on a hollow of one floor would be done by going down: it asks for the hollow's end instead)
            if (request is { Kind: DelveRequestKind.Rescue } r && r.Floor > h.Floors) request = r with { Floor = h.Floors };
            list.Add(request);
        }
        return list;
    }
}

/// <summary>
/// What the game keeps of the delve (plan 44 · SD9; <c>SaveData.Delve</c>, null while nothing is kept): each hollow's
/// clears, the board's requests done today and the prizes waiting at the camp. The camp's rank is the hollows cleared.
/// No drawing or input.
/// </summary>
public sealed class DelveRecord
{
    public Dictionary<string, int> Clears { get; } = new();
    public int Runs { get; set; }
    public int RecruitsHome { get; set; }

    /// <summary>The day (the calendar day the board was read on) the board's requests in <see cref="Done"/> belong to.</summary>
    public int BoardDay { get; set; } = -1;
    public HashSet<int> Done { get; } = new();

    /// <summary>Prizes earned and not yet handed over, by the board's prize table.</summary>
    public List<(string Item, int Count)> Due { get; } = new();

    public bool IsEmpty => Clears.Count == 0 && Runs == 0 && RecruitsHome == 0 && Done.Count == 0 && Due.Count == 0;

    /// <summary>The camp's rank: how many hollows have been cleared.</summary>
    public int Rank => Clears.Count(c => c.Value > 0);

    public bool IsOpen(DelveHollow hollow) => hollow.Rank <= Rank;

    public bool Cleared(string key) => Clears.GetValueOrDefault(key) > 0;

    /// <summary>
    /// A run's end written down: a clear counted, and each of today's requests it does marked done with its prize due.
    /// Returns the requests it did.
    /// </summary>
    public List<DelveRequest> Take(DelveRun run, IReadOnlyList<DelveRequest> board, int day)
    {
        Runs++;
        if (BoardDay != day)
        {
            BoardDay = day;
            Done.Clear();
        }
        if (run.Outcome == DelveOutcome.Cleared) Clears[run.Hollow.Key] = Clears.GetValueOrDefault(run.Hollow.Key) + 1;
        var did = new List<DelveRequest>();
        for (int i = 0; i < board.Count; i++)
        {
            if (Done.Contains(i) || !board[i].DoneBy(run)) continue;
            Done.Add(i);
            did.Add(board[i]);
            foreach (var prize in DelveHollows.Prizes.Where(p => p.Kind == board[i].Kind)) Due.Add((prize.Item, prize.Count));
        }
        return did;
    }

    /// <summary>Whether a request of today's board is done.</summary>
    public bool IsDone(int index, int day) => BoardDay == day && Done.Contains(index);

    public DelveSave? ToSave() => IsEmpty ? null : new DelveSave
    {
        Clears = new Dictionary<string, int>(Clears),
        Runs = Runs,
        RecruitsHome = RecruitsHome,
        BoardDay = BoardDay,
        Done = Done.OrderBy(i => i).ToList(),
        Due = Due.Select(d => new DelveSave.Prize { Item = d.Item, Count = d.Count }).ToList()
    };

    public static DelveRecord From(DelveSave? save)
    {
        var r = new DelveRecord();
        if (save == null) return r;
        foreach (var (k, v) in save.Clears) r.Clears[k] = v;
        r.Runs = save.Runs;
        r.RecruitsHome = save.RecruitsHome;
        r.BoardDay = save.BoardDay;
        foreach (var i in save.Done) r.Done.Add(i);
        foreach (var d in save.Due) r.Due.Add((d.Item, d.Count));
        return r;
    }
}

/// <summary>The delve's record as the save writes it.</summary>
public sealed class DelveSave
{
    public Dictionary<string, int> Clears { get; set; } = new();
    public int Runs { get; set; }
    public int RecruitsHome { get; set; }
    public int BoardDay { get; set; } = -1;
    public List<int> Done { get; set; } = new();
    public List<Prize> Due { get; set; } = new();

    public sealed class Prize
    {
        public string Item { get; set; } = "";
        public int Count { get; set; } = 1;
    }
}
