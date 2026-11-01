using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

// The gallery (plan 12 · Q30; style guide, "Menu screens", "Gallery", and "Pokémon in menus")
internal static partial class ModernUi
{
    private static (string Label, UiIcon Icon, Color Color)[] GalleryTabs => new[]
    {
        (Text.Of("gallery.tab.pokemon"), UiIcon.Dex, Green),
        (Text.Of("gallery.tab.people"), UiIcon.Trainer, Blue)
    };

    /// <summary>How wide the gallery's list is, and how big the model's picture is drawn on its stage.</summary>
    public const float GalleryListWidth = 620, GalleryFigure = 1120;

    public static void DrawGallery(int sw, int sh, GalleryScreen screen, float appear)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("gallery.title"));
        Hints(sw - Margin, 44,
            Hint("shared.keys.z", screen.Tab == GalleryTab.Pokemon ? "gallery.hint.cry" : "shared.hints.play"),
            Hint("shared.keys.leftRight", "gallery.hint.turn"),
            Hint("shared.keys.c", screen.Tab == GalleryTab.Pokemon ? "gallery.hint.clip" : "gallery.hint.emote"),
            Hint("gallery.keys.p", "gallery.hint.view"),
            Hint("gallery.keys.shoulders", "gallery.hint.zoom"),
            Hint("shared.keys.enter", "gallery.hint.tab"),
            Hint("shared.keys.esc", "shared.hints.back"));
        Tabs(new Rectangle(Margin, ContentTop, GalleryListWidth, 76), GalleryTabs, (int)screen.Tab);

        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        const float top = ContentTop + 76 + 24;
        var colour = GalleryTabs[(int)screen.Tab].Color;

        // ---- The list
        var list = new Rectangle(Margin - slide, top, GalleryListWidth, ContentBottom - top);
        Panel(list, 34);
        int count = screen.Count;
        bool scrolls = count > GalleryScreen.VisibleRows;
        for (int row = 0; row < GalleryScreen.VisibleRows && screen.First + row < count; row++)
        {
            int index = screen.First + row;
            var r = RowRect(list, row, 24, scrolls);
            bool selected = index == screen.Cursor;
            if (screen.Tab == GalleryTab.Pokemon)
            {
                var entry = screen.Pokemon[index];
                // A form is indented under its species, Muted until chosen
                ListRow(r, selected, GalleryScreen.LabelOf(entry), !entry.IsForm || selected, entry.IsForm ? 84 : 52, entry.IsForm ? 28 : 32);
                int number = entry.Species.DexNumber;
                if (!entry.IsForm) RowValue(r, number.ToString("D4"), selected ? Color.White : Muted, 24);
            }
            else ListRow(r, selected, screen.People[index].Name, true, 52, 32);
        }
        ScrollBar(list, screen.First, GalleryScreen.VisibleRows, count, 24);

        // ---- The stage: the evolution's hall, its pool of light and slow rays, no glow
        var stage = new Rectangle(Margin + GalleryListWidth + Gutter + slide, ContentTop, sw - Margin * 2 - GalleryListWidth - Gutter, ContentBottom - ContentTop);
        Panel(stage, 34);
        var inner = new Rectangle(stage.X + 8, stage.Y + 8, stage.Width - 16, stage.Height - 16);
        var c = new Vector2(inner.X + inner.Width / 2f, inner.Y + inner.Height / 2f + 10);
        Rlgl.DrawRenderBatchActive();
        Raylib.BeginScissorMode((int)(inner.X * GameEngine.RenderScale), (int)(inner.Y * GameEngine.RenderScale),
            (int)(inner.Width * GameEngine.RenderScale), (int)(inner.Height * GameEngine.RenderScale));
        EvolutionStage(sw, sh, c - new Vector2(0, 40), (float)FrameClock.Now, 0.12f);

        string? shows = screen.PictureShows;
        string? want = screen.Selected?.Look ?? screen.SelectedPerson?.Look;
        if (shows != null && shows == want && screen.Picture is { } picture)
        {
            float size = GalleryFigure;
            Raylib.DrawTexturePro(picture, new Rectangle(0, 0, picture.Width, -picture.Height),
                new Rectangle(c.X - size / 2f, c.Y - size / 2f, size, size), Vector2.Zero, 0f, Color.White);
        }
        else if (screen.Selected is { } standing && PokemonSprites.GetBaked(standing.Look, SpriteView.Front) is { } still)
        {
            // Until its model is built the still sprite stands in, at a whole-number scale and unturned
            float size = still.Width * 4f;
            Raylib.DrawTexturePro(still, new Rectangle(0, 0, still.Width, still.Height),
                new Rectangle(MathF.Round(c.X - size / 2f), MathF.Round(c.Y - size / 2f), size, size), Vector2.Zero, 0f, Color.White);
        }
        Rlgl.DrawRenderBatchActive();
        Raylib.EndScissorMode();

        // ---- Its name over the stage, and the strip of tags along its foot
        string name = screen.Selected is { } e ? GalleryScreen.LabelOf(e) : screen.SelectedPerson?.Name ?? "";
        if (name.Length > 0)
        {
            float nx = stage.X + 44;
            UiFonts.DrawCentered(name, nx, stage.Y + 64, 46, Color.White, UiWeight.Black);
            if (screen.Selected is { } en)
                UiFonts.DrawCentered(Text.Of("gallery.number", ("n", en.Species.DexNumber)), nx, stage.Y + 112, 26, new Color(196, 206, 236, 255), UiWeight.ExtraBold);
        }
        float tx = stage.X + 44, ty = stage.Y + stage.Height - 44 - 40;
        string clip = screen.Tab == GalleryTab.Pokemon ? GalleryScreen.ClipName(screen.Clip) : GalleryScreen.EmoteName(screen.Emote);
        tx += Tag(tx, ty, clip, colour, 44) + 12;
        tx += Tag(tx, ty, Text.Of(screen.Back ? "gallery.view.back" : "gallery.view.front"), Frame, 44) + 12;
        int step = screen.ZoomStep;
        tx += Tag(tx, ty, Text.Of($"gallery.zoom.{step}"), Frame, 44) + 12;
        if (want != null && (shows == null || shows != want)) Tag(tx, ty, Text.Of("gallery.building"), Muted, 44);
        if (count == 0)
            UiFonts.DrawCentered(Text.Of("gallery.empty"), stage.X + 44, c.Y, 32, Color.White, UiWeight.ExtraBold);
    }
}
