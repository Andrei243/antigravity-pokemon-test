using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>The seven colours of Apricorn (plan 20 · J6), in HeartGold's order.</summary>
public enum Apricorn { Red, Blue, Yellow, Green, Pink, White, Black }

/// <summary>
/// An Apricorn tree as the game keeps it: where it stands and the colour it bears. Those colours are ours
/// (<c>Data/apricorns.json</c>): HeartGold's trees hold a colour each too, but the pinned sources of this game don't
/// carry which, so each of Johto's twenty-four trees was given one by the route it stands on.
/// </summary>
public sealed class ApricornTreeEntry
{
    public string Area { get; set; } = "";
    public string Id { get; set; } = "";
    public string Colour { get; set; } = "";
}

/// <summary>One colour of <c>apricorns.json</c>: its ball, and the colour its fruit and its card are drawn in.</summary>
public sealed class ApricornColourEntry
{
    public string Name { get; set; } = "";
    public string Ball { get; set; } = "";
    public int[] Rgb { get; set; } = { 200, 60, 60 };
}

/// <summary>The file <c>Data/apricorns.json</c>.</summary>
public sealed class ApricornFile
{
    public List<ApricornColourEntry> Colours { get; set; } = new();
    public List<ApricornTreeEntry> Trees { get; set; } = new();
}

/// <summary>
/// The tables of the Apricorns (plan 20 · J6; <c>Data/apricorns.json</c>): which ball each colour becomes at Kurt's, and
/// the colour of each tree of Johto. No drawing or input.
/// </summary>
public static class ApricornData
{
    private static readonly Lazy<ApricornFile> file = new(() => GameDataFiles.Load<ApricornFile>("apricorns.json"));

    public static ApricornFile File => file.Value;

    /// <summary>The ball a colour is made into at Kurt's: Red a Level Ball, Blue a Lure Ball, Yellow a Moon Ball, Green a Friend Ball, Pink a Love Ball, White a Fast Ball, Black a Heavy Ball.</summary>
    public static string BallOf(Apricorn colour) => File.Colours.First(c => c.Name == colour.ToString()).Ball;

    /// <summary>The colour an Apricorn tree of an area bears, by the tree's id in the area's file; null for an object that is no tree of the table.</summary>
    public static Apricorn? ColourOf(string area, string id) =>
        File.Trees.FirstOrDefault(t => t.Area == area && t.Id == id) is { } tree && Enum.TryParse<Apricorn>(tree.Colour, out var colour) ? colour : null;

    /// <summary>The colour an Apricorn is named by, as the chosen language writes it ("Red Apricorn").</summary>
    public static string NameOf(Apricorn colour)
    {
        string which = colour.ToString().ToLowerInvariant();
        return Text.Of("apricorn.name", ("colour", Text.Of($"apricorn.colour.{which}")));
    }

    public static Apricorn? Parse(string name) => Enum.TryParse<Apricorn>(name, ignoreCase: true, out var colour) ? colour : null;
}

/// <summary>What Kurt is making: the colour, how many, and the day it was ordered.</summary>
public sealed class ApricornOrder
{
    public string Colour { get; set; } = "";
    public int Count { get; set; }
    public int Day { get; set; }
    /// <summary>Whether Kurt has rung to say it is ready (once).</summary>
    public bool Announced { get; set; }
}

/// <summary>What the game keeps of the Apricorns, as a save has it.</summary>
public sealed class ApricornSave
{
    public Dictionary<string, int>? Counts { get; set; }
    public Dictionary<string, int>? Picked { get; set; }
    public ApricornOrder? Order { get; set; }
}

/// <summary>
/// The Apricorn Box and the trees (plan 20 · J6; <c>docs/mechanics/rulings.md</c>, "Apricorns, the Box and Kurt's
/// balls"): how many Apricorns of each colour the player holds (99 at most, as HeartGold's Box does), the day each
/// tree was last picked, and the one order Kurt can be making. A tree bears one Apricorn, once a calendar day: it is
/// bare from the moment it is picked until the next day. Kurt turns every Apricorn of the colour he is given into a
/// ball of its kind (<see cref="ApricornData.BallOf"/>) and has them ready on the next day. No drawing or input; the
/// day is always the one handed over, so a tool with a fixed date can move it.
/// </summary>
public sealed class ApricornBox
{
    /// <summary>The most the Box holds of one colour.</summary>
    public const int Limit = 99;

    private readonly Dictionary<Apricorn, int> counts = new();
    private readonly Dictionary<string, int> picked = new(StringComparer.Ordinal);

    /// <summary>What Kurt is making, or null.</summary>
    public ApricornOrder? Order { get; private set; }

    /// <summary>Counts every change, so the screen and the trees know to look again. Not saved.</summary>
    public int Revision { get; private set; }

    public int Count(Apricorn colour) => counts.GetValueOrDefault(colour);

    public int Total => counts.Values.Sum();

    /// <summary>The colours held, in order.</summary>
    public IEnumerable<Apricorn> Held => Enum.GetValues<Apricorn>().Where(c => Count(c) > 0);

    /// <summary>The number of a day, to count them by (days since 1 January 2000).</summary>
    public static int DayOf(DateTime date) => (int)(date.Date - new DateTime(2000, 1, 1)).TotalDays;

    // ---- the trees

    /// <summary>The key a tree is kept under: its area and its id in the area's file.</summary>
    public static string TreeKey(string area, string id) => area + "/" + id;

    /// <summary>Whether a tree has an Apricorn on it on <paramref name="day"/>: it was never picked, or was picked on an earlier day.</summary>
    public bool InFruit(string treeKey, int day) => !picked.TryGetValue(treeKey, out int last) || last < day;

    /// <summary>
    /// Takes the Apricorn off a tree on <paramref name="day"/> into the Box. False, and nothing changes, if the tree
    /// is bare or the Box already holds the most of that colour.
    /// </summary>
    public bool Pick(string treeKey, Apricorn colour, int day)
    {
        if (!InFruit(treeKey, day) || Count(colour) >= Limit) return false;
        picked[treeKey] = day;
        counts[colour] = Count(colour) + 1;
        Revision++;
        return true;
    }

    /// <summary>Whether the Box is too full of a colour to take another.</summary>
    public bool IsFull(Apricorn colour) => Count(colour) >= Limit;

    // ---- Kurt

    /// <summary>0 nothing ordered, 1 Kurt is at work, 2 the balls are ready: they are, from the day after the order.</summary>
    public int Status(int day) => Order == null ? 0 : day > Order.Day ? 2 : 1;

    /// <summary>
    /// Kurt takes every Apricorn of a colour the player holds to make into balls. Refused (0) with an order already
    /// in hand, whether he is at work or the balls are waiting to be collected, or with none of the colour.
    /// </summary>
    public int PlaceOrder(Apricorn colour, int day)
    {
        if (Order != null || Count(colour) <= 0) return 0;
        int n = Count(colour);
        counts[colour] = 0;
        Order = new ApricornOrder { Colour = colour.ToString(), Count = n, Day = day };
        Revision++;
        return n;
    }

    /// <summary>Hands over the finished order, once: the ball's name and how many. Null while there is nothing ready.</summary>
    public (string Ball, int Count)? Collect(int day)
    {
        if (Status(day) != 2 || Order == null) return null;
        var colour = ApricornData.Parse(Order.Colour)!.Value;
        var done = (ApricornData.BallOf(colour), Order.Count);
        Order = null;
        Revision++;
        return done;
    }

    /// <summary>The balls are ready and Kurt has not yet rung to say so; marks him as having rung.</summary>
    public bool TakeReadyCall(int day)
    {
        if (Status(day) != 2 || Order is not { Announced: false }) return false;
        Order.Announced = true;
        return true;
    }

    // ---- the save

    public void Clear() => Load(null);

    public void Load(ApricornSave? save)
    {
        counts.Clear();
        picked.Clear();
        foreach (var (name, n) in save?.Counts ?? new())
            if (ApricornData.Parse(name) is { } colour && n > 0) counts[colour] = Math.Min(n, Limit);
        foreach (var (key, day) in save?.Picked ?? new()) picked[key] = day;
        Order = save?.Order is { Count: > 0 } order && ApricornData.Parse(order.Colour) != null
            ? new ApricornOrder { Colour = order.Colour, Count = order.Count, Day = order.Day, Announced = order.Announced } : null;
        Revision++;
    }

    public ApricornSave? ToSave()
    {
        if (counts.Count == 0 && picked.Count == 0 && Order == null) return null;
        return new ApricornSave
        {
            Counts = counts.Count > 0 ? counts.Where(c => c.Value > 0).ToDictionary(c => c.Key.ToString(), c => c.Value) : null,
            Picked = picked.Count > 0 ? new Dictionary<string, int>(picked) : null,
            Order = Order
        };
    }
}
