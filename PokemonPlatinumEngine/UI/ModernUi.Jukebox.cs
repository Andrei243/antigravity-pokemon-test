using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

// The jukebox (plan 12 · Q15; style guide, "Menu screens", "Jukebox")
internal static partial class ModernUi
{
    private static (string Label, UiIcon Icon, Color Color)[] JukeboxTabs => new[]
    {
        (Text.Of("jukebox.tab.songs"), UiIcon.Radio, Blue),
        (Text.Of("jukebox.tab.sounds"), UiIcon.Boost, Red),
        (Text.Of("jukebox.tab.cries"), UiIcon.Dex, Green),
        (Text.Of("jukebox.tab.beds"), UiIcon.MapCard, Gold)
    };

    private static Color JukeboxColour(JukeboxTab tab) => JukeboxTabs[(int)tab].Color;

    public static void DrawJukebox(int sw, int sh, JukeboxScreen screen, float appear)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("jukebox.title"));
        var keys = new System.Collections.Generic.List<HintItem>
        {
            Hint("shared.keys.leftRight", "jukebox.hint.tab"),
            Hint("shared.keys.z", "shared.hints.play")
        };
        if (screen.Tab == JukeboxTab.Songs) keys.Add(Hint("jukebox.keys.shoulders", "jukebox.hint.arrangement"));
        if (screen.Tab == JukeboxTab.Cries) keys.Add(Hint("jukebox.keys.shoulders", "jukebox.hint.species"));
        keys.Add(Hint("shared.keys.esc", "shared.hints.back"));
        Hints(sw - Margin, 44, keys.ToArray());
        Tabs(new Rectangle(Margin, ContentTop, sw - Margin * 2, 76), JukeboxTabs, (int)screen.Tab);

        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        const float top = ContentTop + 76 + 24;
        var colour = JukeboxColour(screen.Tab);

        // ---- The list
        var list = new Rectangle(Margin - slide, top, 1000, ContentBottom - top);
        Panel(list, 34);
        var entries = screen.Entries;
        bool scrolls = entries.Count > JukeboxScreen.VisibleRows;
        for (int row = 0; row < JukeboxScreen.VisibleRows && screen.First + row < entries.Count; row++)
        {
            int index = screen.First + row;
            var entry = entries[index];
            var r = RowRect(list, row, 24, scrolls);
            bool selected = index == screen.Cursor;
            var ink = ListRow(r, selected, JukeboxScreen.LabelOf(screen.Tab, entry), entry.Unlocked, 52, 32);
            bool on = screen.PlayingTab == screen.Tab && screen.PlayingId == entry.Id;
            if (on)
            {
                var sign = new Vector2(r.X + r.Width - 48, r.Y + r.Height / 2f);
                UiShapes.Circle(sign, 20, selected ? Color.White : colour);
                UiIcons.Draw(UiIcon.Radio, sign, 11, selected ? colour : Color.White, selected ? Color.White : colour);
            }
            else if (screen.Tab == JukeboxTab.Songs && entry.Unlocked)
                RowValue(r, JukeboxScreen.RegionOf(entry.Id), selected ? Color.White : Muted, 24);
        }
        ScrollBar(list, screen.First, JukeboxScreen.VisibleRows, entries.Count, 24);

        // ---- The one under the cursor
        var detail = new Rectangle(Margin + 1000 + Gutter + slide, top, sw - Margin * 2 - 1000 - Gutter, ContentBottom - top);
        Panel(detail, 34);
        if (screen.Selected is not { } chosen) return;
        float x = detail.X + 44, room = detail.Width - 88;
        string name = JukeboxScreen.LabelOf(screen.Tab, chosen);
        float size = 46, nameW = UiFonts.Measure(name, size, UiWeight.Black);
        if (nameW > room) size = MathF.Floor(size * room / nameW);
        UiFonts.DrawCentered(name, x, detail.Y + 70, size, Ink, UiWeight.Black);
        string count = Text.Of("jukebox.count", ("n", screen.Cursor + 1), ("total", entries.Count));
        UiFonts.DrawCentered(count, x, detail.Y + 124, 26, Muted, UiWeight.ExtraBold);
        UiShapes.Fill(new Rectangle(x, detail.Y + 160, room, 3), 1.5f, Rule);

        float y = detail.Y + 196;
        switch (screen.Tab)
        {
            case JukeboxTab.Songs:
                SongDetail(screen, chosen, x, y, room);
                break;
            case JukeboxTab.Sounds:
                if (SoundBank.Entries.FirstOrDefault(e => e.Name == chosen.Id) is { } sound)
                {
                    string group = sound.Group.ToString();
                    Tag(x, y, Text.Of($"jukebox.group.{group}"), colour, 38);
                    DrawWrapped(sound.PlayedWhen, x, y + 72, room, 28, Ink, 34);
                }
                break;
            case JukeboxTab.Cries:
                if (screen.CryOf is { } species)
                {
                    UiFonts.DrawCentered(Text.Of("jukebox.cryOf"), x, y + 12, 24, Muted, UiWeight.ExtraBold);
                    UiFonts.DrawCentered(species.DisplayName, x, y + 62, 40, Ink, UiWeight.Black);
                    UiFonts.DrawCentered(Text.Of("jukebox.cryNumber", ("n", species.DexNumber)), x, y + 108, 24, Muted, UiWeight.ExtraBold);
                }
                string mode = chosen.Id;
                DrawWrapped(Text.Of($"jukebox.cryNote.{mode}"), x, y + 170, room, 28, Ink, 34);
                break;
            default:
                if (Ambience.Entries.FirstOrDefault(e => e.Bed.ToString() == chosen.Id) is { } bed)
                    DrawWrapped(bed.HeardWhen, x, y, room, 28, Ink, 34);
                break;
        }
    }

    private static void SongDetail(JukeboxScreen screen, JukeboxEntry chosen, float x, float y, float room)
    {
        var colour = JukeboxColour(JukeboxTab.Songs);
        if (!chosen.Unlocked)
        {
            DrawWrapped(Text.Of("jukebox.lockedNote"), x, y, room, 28, Ink, 34);
            return;
        }
        Tag(x, y, JukeboxScreen.RegionOf(chosen.Id), Frame, 38);
        var song = MusicLibrary.Get(chosen.Id);
        if (song != null)
        {
            var arrangement = screen.Arrangement;
            double seconds = song.Duration(arrangement == JukeboxArrangement.Night);
            int whole = (int)Math.Round(seconds);
            string length = Text.Of(song.Loops ? "jukebox.lengthLoops" : "jukebox.length", ("time", $"{whole / 60}:{whole % 60:00}"));
            UiFonts.DrawCentered(length, x, y + 76, 28, Ink, UiWeight.ExtraBold);
        }
        UiFonts.DrawCentered(Text.Of("jukebox.arrangements"), x, y + 140, 24, Muted, UiWeight.ExtraBold);
        float cx = x;
        foreach (var a in chosen.Arrangements)
        {
            bool current = a == screen.Arrangement;
            cx += Tag(cx, y + 170, JukeboxScreen.ArrangementName(a), current ? colour : Muted, 44) + 12;
        }
    }
}
