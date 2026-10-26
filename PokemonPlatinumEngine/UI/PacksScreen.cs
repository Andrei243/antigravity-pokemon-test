using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.UI;

/// <summary>A pack on the packs screen: the pack found, and whether the options have it on.</summary>
public sealed class PackRow
{
    public PackRow(DataLayer layer, bool enabled)
    {
        Layer = layer;
        Enabled = enabled;
    }

    public DataLayer Layer { get; }
    public PackManifest Pack => Layer.Pack!;
    public bool Enabled { get; set; }
}

/// <summary>
/// The packs screen under OPTIONS (plan 16 Â· T15; style guide, "Menu screens", Packs): every pack found beside the game
/// and beside the saves, on or off, in the order they load, with what each says of itself and the credit it carries.
/// A turns the pack under the cursor on or off, left and right move it earlier or later in the order, and B goes
/// back. Changes are written into the options at once (<see cref="GameSettings.Packs"/>) and take hold at the next
/// start, which the screen says while what would load differs from what is in use (<see cref="Pending"/>): the data
/// is read once, before anything else, and a pack can't be laid over a game already running. The state moves through
/// methods that take no input (<see cref="Open"/>, <see cref="Move"/>, <see cref="Toggle"/>, <see cref="Shift"/>), so
/// tests and the harness drive it; <see cref="Update"/> maps the keys on.
/// </summary>
public sealed class PacksScreen
{
    /// <summary>How many packs the list shows at once.</summary>
    public const int VisibleRows = 4;

    private readonly List<PackRow> rows = new();

    public bool IsActive { get; private set; }
    public int Selected { get; private set; }
    public int First { get; private set; }

    /// <summary>Every pack found, in the options' order (the rest by name).</summary>
    public IReadOnlyList<PackRow> Rows => rows;

    /// <summary>The packs that couldn't be read, each with what is wrong: said under the list.</summary>
    public IReadOnlyList<string> Problems { get; private set; } = Array.Empty<string>();

    /// <summary>The folders packs are looked for in, said when there is none.</summary>
    public IReadOnlyList<string> Folders { get; private set; } = Array.Empty<string>();

    /// <summary>The names of the packs in use this run, in load order.</summary>
    public IReadOnlyList<string> InUse { get; private set; } = Array.Empty<string>();

    /// <summary>The pack under the cursor; null when there is none.</summary>
    public PackRow? Current => Selected < rows.Count ? rows[Selected] : null;

    /// <summary>
    /// Opens on the packs found (on or off), ordered as the options have them and the rest by name, the cursor on the
    /// first. <paramref name="inUse"/> is what this run loaded, against which <see cref="Pending"/> is told.
    /// </summary>
    public void Open(IReadOnlyList<DataLayer> found, IReadOnlyList<PackChoice>? chosen, IReadOnlyList<DataLayer> inUse,
        IReadOnlyList<string>? problems = null, IReadOnlyList<string>? folders = null)
    {
        chosen ??= Array.Empty<PackChoice>();
        int Rank(DataLayer l)
        {
            int at = chosen.ToList().FindIndex(c => string.Equals(c.Name, l.Pack!.Name, StringComparison.OrdinalIgnoreCase));
            return at < 0 ? int.MaxValue : at;
        }
        rows.Clear();
        foreach (var layer in found.Where(l => l.Pack != null).OrderBy(Rank).ThenBy(l => l.Pack!.Name, StringComparer.Ordinal))
            rows.Add(new PackRow(layer, !chosen.Any(c => !c.Enabled && string.Equals(c.Name, layer.Pack!.Name, StringComparison.OrdinalIgnoreCase))));
        InUse = inUse.Where(l => l.Pack != null).Select(l => l.Pack!.Name).ToList();
        Problems = problems?.ToList() ?? new List<string>();
        Folders = folders?.ToList() ?? new List<string>();
        Selected = 0;
        First = 0;
        IsActive = true;
    }

    /// <summary>Opens on the packs of this machine: those beside the game and beside the saves (<see cref="DataLayers.PackFolders"/>).</summary>
    public void OpenHere(GameSettings settings)
    {
        var folders = DataLayers.PackFolders(SavePaths.Current.Folder);
        var problems = new List<string>();
        var found = DataLayers.Found(folders, problems);
        Open(found, settings.Packs, DataLayers.Packs, problems, folders);
    }

    public void Close() => IsActive = false;

    /// <summary>One step up or down the list, wrapping round; the window follows.</summary>
    public void Move(int dy)
    {
        if (rows.Count == 0) return;
        Selected = (Selected + dy + rows.Count) % rows.Count;
        First = Kit.UiNav.Window(First, Selected, rows.Count, VisibleRows);
    }

    /// <summary>A: the pack under the cursor on or off. False when there is none.</summary>
    public bool Toggle()
    {
        if (Current is not { } row) return false;
        row.Enabled = !row.Enabled;
        return true;
    }

    /// <summary>Left or right: the pack under the cursor a place earlier or later in the order, the cursor with it. False at an end.</summary>
    public bool Shift(int dir)
    {
        int to = Selected + Math.Sign(dir);
        if (Current == null || dir == 0 || to < 0 || to >= rows.Count) return false;
        (rows[Selected], rows[to]) = (rows[to], rows[Selected]);
        Selected = to;
        First = Kit.UiNav.Window(First, Selected, rows.Count, VisibleRows);
        return true;
    }

    /// <summary>The order and the on and off as the options keep them.</summary>
    public List<PackChoice> Choices => rows.Select(r => new PackChoice { Name = r.Pack.Name, Enabled = r.Enabled }).ToList();

    /// <summary>Writes the choices into the options.</summary>
    public void WriteTo(GameSettings settings) => settings.Packs = Choices;

    /// <summary>The packs the next start would load, in load order (each after those it names).</summary>
    public IReadOnlyList<string> WillLoad =>
        DataLayers.InLoadOrder(rows.Select(r => r.Layer).ToList(), Choices, new List<string>()).Select(l => l.Pack!.Name).ToList();

    /// <summary>Whether what the next start would load differs from what this run loaded: the screen says the change waits for it.</summary>
    public bool Pending => !WillLoad.SequenceEqual(InUse, StringComparer.OrdinalIgnoreCase);

    /// <summary>The place of the pack under the cursor in the order, as its stepper shows it: 2 / 3.</summary>
    public string Place => rows.Count == 0 ? "" : $"{Selected + 1} / {rows.Count}";

    /// <summary>One frame. Returns true when the choices changed and the options should be saved.</summary>
    public bool Update(GameSettings settings)
    {
        if (!IsActive) return false;
        bool changed = false;
        int dy = InputManager.Axis(GameAction.Up, GameAction.Down), dx = InputManager.Axis(GameAction.Left, GameAction.Right);
        if (dy != 0)
        {
            Move(dy);
            AudioManager.PlaySound("cursor");
        }
        else if (dx != 0)
        {
            changed = Shift(dx);
            AudioManager.PlaySound(changed ? "select" : "error");
        }
        else if (InputManager.IsActionPressed(GameAction.Confirm))
        {
            changed = Toggle();
            AudioManager.PlaySound(changed ? "select" : "error");
        }
        else if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Menu))
        {
            Close();
            AudioManager.PlaySound("cancel");
        }
        if (changed) WriteTo(settings);
        return changed;
    }

    public void Draw(int sw, int sh)
    {
        if (IsActive) ModernUi.DrawPacks(this, sw, sh);
    }
}
