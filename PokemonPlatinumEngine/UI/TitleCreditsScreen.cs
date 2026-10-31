using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>A row of the credits as it is drawn: a section's heading, a wrapped line of its text, or the gap after a section.</summary>
public enum CreditsRowKind { Heading, Text, Gap }

/// <summary>One row the credits' panel draws, already wrapped to the panel's width.</summary>
public sealed record CreditsRow(CreditsRowKind Kind, string Text, int Section);

/// <summary>
/// The title's CREDITS (plan 16 · T1; style guide, "Menu screens", "Credits"): whose work is in the game, read from
/// the repository's two records (<see cref="TitleCredits"/>) after a first section of the project's own words, in one
/// panel that scrolls a row at a time. Up and down scroll, left and right go to the section before or after, B goes
/// back to the title. Its logic takes no input (<see cref="Move"/>, <see cref="Jump"/>, <see cref="Cancel"/>) and its
/// rows are wrapped with no window (<see cref="TextMetrics"/>), so tests and the harness drive it. Not the roll after
/// the Hall of Fame, which is <see cref="CreditsScreen"/>.
/// </summary>
public sealed class TitleCreditsScreen
{
    private const float AppearTime = 0.3f;

    /// <summary>The panel's text: its inner width, the sizes and the pitch every row takes.</summary>
    public const float TextWidth = 1792 - 88 - 48, TextSize = 28, HeadingSize = 36, RowPitch = 42;

    /// <summary>The rows the panel shows at once.</summary>
    public static int VisibleRows => (int)((ModernUi.ContentBottom - ModernUi.ContentTop - 48) / RowPitch);

    private float openAge;

    public bool IsActive { get; private set; }

    /// <summary>The sections shown: the project's own, then each of the records'.</summary>
    public IReadOnlyList<CreditsSection> Sections { get; private set; } = Array.Empty<CreditsSection>();

    /// <summary>Every row, wrapped, in order.</summary>
    public IReadOnlyList<CreditsRow> Rows { get; private set; } = Array.Empty<CreditsRow>();

    /// <summary>The first row in view.</summary>
    public int First { get; private set; }

    /// <summary>The furthest the list scrolls: its last row at the panel's foot.</summary>
    public int LastFirst => Math.Max(0, Rows.Count - VisibleRows);

    /// <summary>The section of the first row in view.</summary>
    public int Section => Rows.Count == 0 ? 0 : Rows[Math.Min(First, Rows.Count - 1)].Section;

    /// <summary>How far the screen has slid into place, 0 to 1.</summary>
    public float Appear => Math.Clamp(openAge / AppearTime, 0f, 1f);

    /// <summary>The first section, in the language of the moment: what this project made itself.</summary>
    public static CreditsSection OwnWork() => new(Text.Of("titleCredits.own.heading"), new[]
    {
        Text.Of("titleCredits.own.made"),
        Text.Of("titleCredits.own.code"),
        Text.Of("titleCredits.own.notice")
    });

    /// <summary>The sections wrapped into rows for the panel: a heading, its lines, and a gap after each but the last.</summary>
    public static List<CreditsRow> Lay(IReadOnlyList<CreditsSection> sections)
    {
        var rows = new List<CreditsRow>();
        for (int s = 0; s < sections.Count; s++)
        {
            if (s > 0) rows.Add(new CreditsRow(CreditsRowKind.Gap, "", s));
            foreach (var line in TextMetrics.Wrap(sections[s].Heading, TextWidth, HeadingSize, UiWeight.Black))
                rows.Add(new CreditsRow(CreditsRowKind.Heading, line, s));
            foreach (var paragraph in sections[s].Lines)
                foreach (var line in TextMetrics.Wrap(paragraph, TextWidth, TextSize, UiWeight.ExtraBold))
                    rows.Add(new CreditsRow(CreditsRowKind.Text, line, s));
        }
        return rows;
    }

    /// <summary>Opens at the top, with the records read from <paramref name="folder"/> (the build's own when null).</summary>
    public void Open(string? folder = null)
    {
        var sections = new List<CreditsSection> { OwnWork() };
        sections.AddRange(TitleCredits.Read(folder));
        Sections = sections;
        Rows = Lay(sections);
        First = 0;
        openAge = 0f;
        IsActive = true;
        AudioManager.PlaySound("menu_open");
    }

    public void Close() => IsActive = false;

    /// <summary>Up (−1) or down (+1) a row; nothing past either end.</summary>
    public void Move(int delta)
    {
        int next = Math.Clamp(First + delta, 0, LastFirst);
        if (next == First) return;
        First = next;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>To the heading of the section after (+1) or before (−1) the one in view.</summary>
    public void Jump(int delta)
    {
        if (Rows.Count == 0) return;
        int target = Math.Clamp(Section + delta, 0, Sections.Count - 1);
        // Back from part way into a section goes to its own heading first
        if (delta < 0 && Rows[First].Kind != CreditsRowKind.Heading && First > 0 && Rows[First - 1].Section == Section) target = Section;
        int row = 0;
        for (int i = 0; i < Rows.Count; i++)
            if (Rows[i].Section == target && Rows[i].Kind == CreditsRowKind.Heading)
            {
                row = i;
                break;
            }
        int next = Math.Min(row, LastFirst);
        if (next == First) return;
        First = next;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The B button: back to the title.</summary>
    public void Cancel()
    {
        Close();
        AudioManager.PlaySound("cancel");
    }

    public void Update(float dt = 1f / 60f)
    {
        if (!IsActive) return;
        openAge += dt;
        if (InputManager.IsActionPressed(GameAction.Up)) Move(-1);
        else if (InputManager.IsActionPressed(GameAction.Down)) Move(1);
        else if (InputManager.IsActionPressed(GameAction.Left) || InputManager.IsActionPressed(GameAction.PageLeft)) Jump(-1);
        else if (InputManager.IsActionPressed(GameAction.Right) || InputManager.IsActionPressed(GameAction.PageRight)) Jump(1);
        else if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Menu)) Cancel();
    }

    /// <summary>Draws the credits.</summary>
    public void Draw(int screenWidth, int screenHeight) => ModernUi.DrawTitleCredits(screenWidth, screenHeight, this, Appear);
}
