using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The phrase picker (plan 08 · P9; style guide, "Field menus and notices", "Phrases"): over the field dimmed, one
/// panel with the phrase's two words and OK along its top, a rule, and under it the eight groups two to a row or, once
/// a group is open, its words three to a row.
/// </summary>
internal static partial class ModernUi
{
    /// <summary>The picker's panel, its slots, and the cells of its groups and words, in layout units.</summary>
    public const float PhrasePanelWidth = 1320, PhrasePanelHeight = 820, PhrasePanelTop = 160;
    public const float PhraseSlotWidth = 480, PhraseSlotHeight = 110, PhraseOkWidth = 240, PhraseGap = 24;
    public const float PhraseGroupWidth = 612, PhraseGroupHeight = 96, PhraseGroupGap = 14;
    public const float PhraseWordWidth = 400, PhraseWordHeight = 80, PhraseWordGap = 12;

    public static void DrawPhrases(int sw, int sh, PhraseEntry entry, float appear = 1f)
    {
        Dim(sw, sh, (int)(150 * appear));
        ScreenTitle(Text.Of("phrases.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.choose"), Hint("shared.keys.x", "shared.hints.back"),
            Hint("shared.keys.enter", "shared.hints.toOk"));

        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;
        var panel = new Rectangle((sw - PhrasePanelWidth) / 2f, PhrasePanelTop + rise, PhrasePanelWidth, PhrasePanelHeight);
        Panel(panel, 36);
        float left = panel.X + 36, inner = panel.Width - 72;

        // ---- The phrase: its two words and OK
        float y = panel.Y + 40;
        for (int i = 0; i < PhraseGroups.Length; i++)
        {
            var slot = new Rectangle(left + i * (PhraseSlotWidth + PhraseGap), y, PhraseSlotWidth, PhraseSlotHeight);
            bool here = entry.Slot == i;
            bool cursor = here && entry.Stage == PhraseStage.Slots;
            UiShapes.Shape(slot, 26, SlotTop, SlotBottom, here ? Selection : Rule, cursor ? 6f : here ? 4f : 3f);
            if (entry.Words[i] is { } word)
                FitCentred(PhraseGroups.Shown(word), slot, 44, Ink);
            else
                UiShapes.Fill(new Rectangle(slot.X + slot.Width / 2f - 60, slot.Y + slot.Height - 34, 120, 6), 3, here ? Selection : Rule);
        }
        var ok = new Rectangle(left + PhraseGroups.Length * (PhraseSlotWidth + PhraseGap), y, PhraseOkWidth, PhraseSlotHeight);
        var okColour = entry.Complete ? Green : new Color(150, 156, 174, 255);
        bool onOk = entry.Slot == PhraseEntry.OkSlot && entry.Stage == PhraseStage.Slots;
        if (onOk) UiShapes.Shadow(ok, 26, 20, Vector2.Zero, okColour with { A = 180 });
        UiShapes.Shape(ok, 26, Lighter(okColour, 0.14f), Darker(okColour, 0.06f), onOk ? Color.White : Darker(okColour, 0.3f), onOk ? 6f : 3f);
        FitCentred(Text.Of("shared.ok"), ok, 40, Color.White);

        // ---- A rule, then the groups or a group's words
        UiShapes.Fill(new Rectangle(left, panel.Y + 176, inner, 4), 2, Rule);
        float top = panel.Y + 204;
        if (entry.Stage != PhraseStage.Words)
        {
            var groups = PhraseGroups.All;
            for (int g = 0; g < groups.Count; g++)
            {
                var r = new Rectangle(left + g % PhraseEntry.GroupColumns * (PhraseGroupWidth + PhraseGap),
                    top + g / PhraseEntry.GroupColumns * (PhraseGroupHeight + PhraseGroupGap), PhraseGroupWidth, PhraseGroupHeight);
                int count = entry.Groups.WordsOf(groups[g]).Count;
                bool selected = entry.Stage == PhraseStage.Groups && (int)entry.Group == g;
                if (!selected) UiShapes.Shape(r, 26, SlotTop, SlotBottom, Rule, 3);
                var ink = ListRow(r, selected, PhraseGroups.GroupName(groups[g]), count > 0, nameX: 32, size: 34);
                RowValue(r, Text.Number(count), selected ? ink : Muted, 28);
            }
        }
        else
        {
            UiFonts.Draw(PhraseGroups.GroupName(entry.Group), left + 8, top, 32, Muted, UiWeight.ExtraBold);
            var words = entry.GroupWords;
            float gridTop = top + 48;
            for (int row = 0; row < PhraseEntry.WordRows; row++)
                for (int col = 0; col < PhraseEntry.WordColumns; col++)
                {
                    int i = (entry.FirstRow + row) * PhraseEntry.WordColumns + col;
                    if (i >= words.Count) break;
                    var r = new Rectangle(left + col * (PhraseWordWidth + PhraseGap), gridTop + row * (PhraseWordHeight + PhraseWordGap),
                        PhraseWordWidth, PhraseWordHeight);
                    bool selected = i == entry.Word;
                    if (selected) ListRow(r, true, "", nameX: 0);
                    else UiShapes.Shape(r, 24, SlotTop, SlotBottom, Rule, 3);
                    FitCentred(PhraseGroups.Shown(words[i]), r, 32, selected ? Color.White : Ink);
                }
            // Where the window of words is, when the group has more rows than show
            if (entry.WordRowCount > PhraseEntry.WordRows)
            {
                float height = PhraseEntry.WordRows * (PhraseWordHeight + PhraseWordGap) - PhraseWordGap;
                var track = new Rectangle(panel.X + panel.Width - 26, gridTop, 10, height);
                UiShapes.Fill(track, 5, Rule);
                float size = Math.Max(40f, height * PhraseEntry.WordRows / entry.WordRowCount);
                float at = (height - size) * entry.FirstRow / Math.Max(1, entry.WordRowCount - PhraseEntry.WordRows);
                UiShapes.Fill(new Rectangle(track.X, track.Y + at, track.Width, size), 5, Frame);
            }
        }

        // ---- Along the foot: what the cursor asks for
        string prompt = entry.Stage switch
        {
            PhraseStage.Groups => Text.Of("phrases.prompt.groups"),
            PhraseStage.Words => Text.Of("phrases.prompt.words"),
            _ => entry.Complete ? Text.Of("phrases.prompt.ready") : Text.Of("phrases.prompt.slots")
        };
        UiFonts.DrawCentered(prompt, left + 8, panel.Y + panel.Height - 56, 30, Muted, UiWeight.ExtraBold);
    }

    /// <summary>A word in the middle of its box, made smaller where it would not fit at its size.</summary>
    private static void FitCentred(string text, Rectangle r, float size, Color ink)
    {
        float room = r.Width - 40;
        float width = UiFonts.Measure(text, size, UiWeight.Black);
        if (width > room && width > 0) { size *= room / width; width = room; }
        UiFonts.DrawCentered(text, r.X + (r.Width - width) / 2f, r.Y + r.Height / 2f, size, ink, UiWeight.Black);
    }
}
