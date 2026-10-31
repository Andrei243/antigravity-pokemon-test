using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The rows of the boxes' search panel (plan 12 · Q26), in the order it shows them; <see cref="Go"/> runs the search.</summary>
public enum BoxSearchRow { Species, Type, Move, Ability, Nature, MinLevel, MaxLevel, Item, Gender, Shiny, Mark, Go }

/// <summary>
/// The boxes' search panel (plan 12 · Q26; style guide, "PC boxes", "The boxes' tools"): a row for each part of a
/// <see cref="BoxFilter"/>, each offering ANY and then only what the boxes hold, worked out once as the panel opens.
/// GPU-free and with no input of its own: <see cref="Move"/>, <see cref="Change"/>, <see cref="Filter"/>.
/// </summary>
public sealed class BoxSearch
{
    public static IReadOnlyList<BoxSearchRow> Rows { get; } = Enum.GetValues<BoxSearchRow>();

    /// <summary>The row under the cursor.</summary>
    public int Row { get; private set; }

    // Each row's choices (index 0 is ANY) and the one chosen
    private readonly Dictionary<BoxSearchRow, IReadOnlyList<object?>> choices = new();
    private readonly Dictionary<BoxSearchRow, int> chosen = new();

    public BoxSearch(PcBoxes pc)
    {
        var stored = pc.All.Where(p => !p.IsEgg).ToList();
        IReadOnlyList<object?> Any(IEnumerable<object> values) => new object?[] { null }.Concat(values).ToList();
        choices[BoxSearchRow.Species] = Any(stored.OrderBy(p => p.Species.DexNumber).Select(p => p.Species.Name).Distinct());
        choices[BoxSearchRow.Type] = Any(Enum.GetValues<PokemonType>().Where(t => stored.Any(p => p.HasType(t))).Cast<object>());
        choices[BoxSearchRow.Move] = Any(stored.SelectMany(p => p.Moves.Select(m => m.Name)).Distinct().OrderBy(n => LocalNames.Move(n), StringComparer.CurrentCultureIgnoreCase));
        choices[BoxSearchRow.Ability] = Any(stored.Select(p => p.AbilityName).OfType<string>().Distinct().OrderBy(n => LocalNames.Ability(n), StringComparer.CurrentCultureIgnoreCase));
        choices[BoxSearchRow.Nature] = Any(Enum.GetValues<Nature>().Where(n => stored.Any(p => p.Nature == n)).Cast<object>());
        // Levels by fives, up to the highest stored
        int top = stored.Count == 0 ? 5 : (stored.Max(p => p.Level) + 4) / 5 * 5;
        var levels = new List<object> { 1 };
        for (int l = 5; l <= top; l += 5) levels.Add(l);
        choices[BoxSearchRow.MinLevel] = Any(levels);
        choices[BoxSearchRow.MaxLevel] = Any(levels);
        var items = new List<object> { BoxFilter.Anything, BoxFilter.Nothing };
        items.AddRange(stored.Select(p => p.HeldItem?.Name).OfType<string>().Distinct().OrderBy(n => LocalNames.Item(n), StringComparer.CurrentCultureIgnoreCase));
        choices[BoxSearchRow.Item] = Any(items);
        choices[BoxSearchRow.Gender] = Any(Enum.GetValues<Gender>().Where(g => stored.Any(p => p.Gender == g)).Cast<object>());
        choices[BoxSearchRow.Shiny] = Any(new object[] { true });
        choices[BoxSearchRow.Mark] = Any(Enum.GetValues<Markings>().Where(m => m != Markings.None).Cast<object>());
        choices[BoxSearchRow.Go] = new object?[] { null };
        foreach (var row in Rows) chosen[row] = 0;
    }

    /// <summary>Up and down the rows, round from the last to the first.</summary>
    public void Move(int dy)
    {
        if (dy != 0) Row = UiNav.Wrap(Row, Math.Sign(dy), Rows.Count);
    }

    /// <summary>Left and right on a row: its next or previous choice, round to ANY. False where there is nothing to change.</summary>
    public bool Change(int dx)
    {
        var row = Rows[Row];
        int count = choices[row].Count;
        if (dx == 0 || count < 2) return false;
        chosen[row] = UiNav.Wrap(chosen[row], Math.Sign(dx), count);
        return true;
    }

    /// <summary>Sets a row's choice by its value (a test's or the harness's way); false if the row doesn't offer it.</summary>
    public bool Choose(BoxSearchRow row, object value)
    {
        int at = choices[row].ToList().FindIndex(v => Equals(v, value));
        if (at < 0) return false;
        chosen[row] = at;
        return true;
    }

    /// <summary>What a row offers, ANY (null) first.</summary>
    public IReadOnlyList<object?> ChoicesOf(BoxSearchRow row) => choices[row];

    /// <summary>The value chosen on a row, null for ANY.</summary>
    public object? Value(BoxSearchRow row) => choices[row][chosen[row]];

    /// <summary>The value chosen on a row as the panel shows it.</summary>
    public string Shown(BoxSearchRow row) => Value(row) switch
    {
        null => Text.Of("boxTools.search.any"),
        string s when row == BoxSearchRow.Species => LocalNames.Species(s),
        string s when row == BoxSearchRow.Move => LocalNames.Move(s),
        string s when row == BoxSearchRow.Ability => LocalNames.Ability(s),
        BoxFilter.Anything => Text.Of("boxTools.search.holdsSomething"),
        BoxFilter.Nothing => Text.Of("boxTools.search.holdsNothing"),
        string s => LocalNames.Item(s),
        PokemonType t => Words.Type(t),
        Nature n => Words.Nature(n),
        int level => Text.Of("boxTools.search.level", ("level", level)),
        Gender g => GenderWord(g),
        bool => Text.Of("boxTools.search.shinyOnly"),
        Markings m => MarkWord(m),
        var other => other!.ToString() ?? ""
    };

    private static string GenderWord(Gender gender) => Text.Of($"boxTools.gender.{gender}");

    private static string MarkWord(Markings mark) => Text.Of($"boxTools.mark.{mark}");

    /// <summary>The search as the rows have it.</summary>
    public BoxFilter Filter => new()
    {
        Species = Value(BoxSearchRow.Species) as string,
        Type = Value(BoxSearchRow.Type) as PokemonType?,
        Move = Value(BoxSearchRow.Move) as string,
        Ability = Value(BoxSearchRow.Ability) as string,
        Nature = Value(BoxSearchRow.Nature) as Nature?,
        MinLevel = Value(BoxSearchRow.MinLevel) as int? ?? 0,
        MaxLevel = Value(BoxSearchRow.MaxLevel) as int? ?? 0,
        Item = Value(BoxSearchRow.Item) as string,
        Gender = Value(BoxSearchRow.Gender) as Gender?,
        Shiny = Value(BoxSearchRow.Shiny) as bool?,
        Marks = Value(BoxSearchRow.Mark) as Markings? ?? Markings.None
    };
}
