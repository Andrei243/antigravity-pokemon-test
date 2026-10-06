using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The new-game introduction and the name keyboard (style guide, "The new-game introduction").</summary>
internal static partial class ModernUi
{
    private static readonly Color IntroTop = new(34, 104, 132, 255), IntroBottom = new(16, 28, 66, 255);

    /// <summary>The introduction's backdrop: deep teal to night blue, with slow discs of light drifting across it.</summary>
    public static void IntroBackdrop(int sw, int sh, float time, float amount)
    {
        Raylib.DrawRectangle(0, 0, sw, sh, Color.Black);
        if (amount <= 0f) return;
        byte a = (byte)(255 * Math.Clamp(amount, 0f, 1f));
        Raylib.DrawRectangleGradientV(0, 0, sw, sh, IntroTop with { A = a }, IntroBottom with { A = a });
        for (int i = 0; i < 9; i++)
        {
            float radius = 110 + (i * 53) % 190;
            float span = sw + radius * 4;
            float x = ((i * 397f + time * (9f + i % 4 * 5f)) % span) - radius * 2;
            float y = 90 + (i * 211) % (sh - 240);
            var disc = new Rectangle(x - radius, y - radius, radius * 2, radius * 2);
            UiShapes.Shadow(disc, radius, radius * 0.7f, Vector2.Zero, new Color(190, 236, 255, (int)((16 + i % 3 * 8) * amount)));
        }
        // A pool of light where whoever is speaking stands
        var floor = new Rectangle(sw / 2f - 520, sh - 330, 1040, 190);
        UiShapes.Shadow(floor, 95, 70, Vector2.Zero, new Color(200, 240, 255, (int)(34 * amount)));
    }

    /// <summary>
    /// A figure rendered into its own picture (the picture is upside down, as render textures are), standing
    /// with the picture's bottom edge at <paramref name="bottom"/> and centred on <paramref name="centerX"/>.
    /// </summary>
    public static void IntroFigure(Texture2D picture, float centerX, float bottom, float height, float alpha)
    {
        if (alpha <= 0.004f) return;
        float width = height * picture.Width / picture.Height;
        Raylib.DrawTexturePro(picture, new Rectangle(0, 0, picture.Width, -picture.Height),
            new Rectangle(centerX - width / 2f, bottom - height, width, height), Vector2.Zero, 0f,
            new Color(255, 255, 255, (int)(255 * Math.Clamp(alpha, 0f, 1f))));
    }

    /// <summary>The whole introduction at one moment.</summary>
    public static void DrawIntro(int sw, int sh, IntroScreen intro, IntroLook look, Texture2D? professor, Texture2D? boy, Texture2D? girl,
        Texture2D? pokemon, Texture2D? ball, Texture2D? sprite, Texture2D? friend = null)
    {
        IntroBackdrop(sw, sh, intro.Time, look.Backdrop);
        var phase = intro.Phase;

        if (phase is IntroPhase.EnterRival or IntroPhase.ConfirmRival)
        {
            DrawNameEntry(sw, sh, intro.RivalEntry!, sprite, intro.Look, UiMotion.EaseOut(Math.Clamp(intro.PhaseTime / 0.3f, 0f, 1f)),
                "HIS NAME", PlayerIdentity.DefaultRivalName);
            if (phase == IntroPhase.ConfirmRival)
                Prompt(sw, sh, $"So his name is {intro.RivalName}?", "Your friend from next door, and your rival.",
                    new[] { ("YES, THAT'S HIM", Green), ("NO, CHANGE IT", Blue) }, intro.AnswerYes ? 0 : 1,
                    UiMotion.EaseOut(Math.Clamp(intro.PhaseTime / 0.18f, 0f, 1f)));
            return;
        }
        if (phase is IntroPhase.EnterName or IntroPhase.ConfirmName)
        {
            DrawNameEntry(sw, sh, intro.Entry!, sprite, intro.Look, UiMotion.EaseOut(Math.Clamp(intro.PhaseTime / 0.3f, 0f, 1f)));
            if (phase == IntroPhase.ConfirmName)
                Prompt(sw, sh, $"So you're {intro.Name}?", "This is the name everyone will call you by.",
                    new[] { ("YES, THAT'S ME", Green), ("NO, CHANGE IT", Blue) }, intro.AnswerYes ? 0 : 1,
                    UiMotion.EaseOut(Math.Clamp(intro.PhaseTime / 0.18f, 0f, 1f)));
            return;
        }

        // ---- The professor, and the Pokémon beside him
        float centre = sw / 2f - look.ProfessorShift * 300f;
        if (professor is { } figure) IntroFigure(figure, centre, 1010, 990, look.Professor * look.Backdrop);
        // The friend from next door comes up where the Pokémon stood
        if (friend is { } neighbour && look.Friend > 0.004f) IntroFigure(neighbour, sw / 2f + 340 + (1f - look.Friend) * 80f, 1010, 900, look.Friend);

        // Where the Pokémon stands: its picture is 600 across with its feet a little above the dialogue panel
        const float pokemonSize = 600, pokemonBottom = 856;
        var place = new Vector2(sw / 2f + 340, pokemonBottom - pokemonSize * 0.45f);
        if (ball is { } sphere && look.Ball > 0.004f)
        {
            float size = 420 * (0.5f + 0.5f * look.Ball);
            Raylib.DrawTexturePro(sphere, new Rectangle(0, 0, sphere.Width, -sphere.Height),
                new Rectangle(place.X - size / 2f, place.Y - size / 2f + (1f - look.Ball) * 60f, size, size), Vector2.Zero, 0f,
                new Color(255, 255, 255, (int)(255 * Math.Min(1f, look.Ball * 2f))));
        }
        if (pokemon is { } shown && look.Pokemon > 0.004f)
        {
            // It grows out of the light about its middle, hops as it lands, as in the games, then stands
            float hop = phase == IntroPhase.Alongside && intro.PhaseTime < 1.6f ? MathF.Abs(MathF.Sin(intro.PhaseTime * 6.5f)) * 46f : 0f;
            float size = pokemonSize * look.Pokemon;
            float bottom = place.Y + (pokemonBottom - place.Y) * look.Pokemon + size * 0.45f * (1f - look.Pokemon);
            Raylib.DrawTexturePro(shown, new Rectangle(0, 0, shown.Width, -shown.Height),
                new Rectangle(place.X - size / 2f, bottom - size - hop, size, size), Vector2.Zero, 0f, Color.White);
        }
        if (look.Flash > 0.004f)
        {
            // A soft halo and a hard white heart, over the ball and whatever is coming out of it
            float r = 90 + 150 * look.Flash;
            UiShapes.Shadow(new Rectangle(place.X - r * 1.6f, place.Y - r * 1.6f, r * 3.2f, r * 3.2f), r * 1.6f, r * 0.9f, Vector2.Zero, new Color(255, 255, 255, (int)(120 * look.Flash)));
            UiShapes.Shadow(new Rectangle(place.X - r, place.Y - r, r * 2, r * 2), r, r * 0.35f, Vector2.Zero, new Color(255, 255, 255, (int)(255 * look.Flash)));
        }

        // ---- The two the player can be
        if (look.Choice > 0.004f && boy is { } first && girl is { } second)
        {
            const float w = 500, h = 640;
            float rise = (1f - look.Choice) * 50f;
            for (int i = 0; i < 2; i++)
            {
                var which = i == 0 ? PlayerLook.Boy : PlayerLook.Girl;
                var r = new Rectangle(sw / 2f + (i == 0 ? -w - 30 : 30), 96 + rise, w, h);
                bool selected = intro.Look == which;
                Card(r, 40, selected);
                var plate = new Rectangle(r.X + 30, r.Y + 30, r.Width - 60, r.Height - 150);
                UiShapes.Shape(plate, 26, Disc, Disc, Rule, 3);
                IntroFigure(i == 0 ? first : second, r.X + r.Width / 2f, plate.Y + plate.Height + 34, 560, look.Choice);
                string label = which == PlayerLook.Boy ? "A BOY" : "A GIRL";
                float lw = UiFonts.Measure(label, 44, UiWeight.Black);
                UiFonts.DrawCentered(label, r.X + (r.Width - lw) / 2f, r.Y + r.Height - 62, 44, selected ? Selection : Ink, UiWeight.Black);
            }
        }

        // ---- What is being said
        if (intro.Talking) DrawDialogue(sw, sh, intro.Speaker, intro.SpokenText, intro.LineComplete);
        else if (phase is IntroPhase.ChooseLook or IntroPhase.ConfirmLook)
            DrawDialogue(sw, sh, IntroScreen.Professor, "Are you a boy, or are you a girl?", false);

        if (phase == IntroPhase.ChooseLook) Hints(sw - Margin, 44, ("Left / Right", "Choose"), ("Z", "This one"));
        if (phase == IntroPhase.ConfirmLook)
            Prompt(sw, sh, intro.Look == PlayerLook.Boy ? "So you're a boy?" : "So you're a girl?", "This is who you will be for the whole adventure.",
                new[] { ("YES", Green), ("NO, GO BACK", Blue) }, intro.AnswerYes ? 0 : 1, UiMotion.EaseOut(Math.Clamp(intro.PhaseTime / 0.18f, 0f, 1f)));

        // ---- The send-off: the dark closes in round the player's own sprite, which shrinks away into the world
        if (look.Dark > 0.004f) Raylib.DrawRectangle(0, 0, sw, sh, new Color(0, 0, 0, (int)(255 * look.Dark)));
        if (sprite is { } small && look.SpriteScale > 0.004f)
        {
            // In whole steps, so it stays pixel art all the way down; a small pool of light goes with it
            int scale = Math.Max(1, (int)MathF.Round(8f * look.SpriteScale));
            var c = new Vector2(sw / 2f, sh / 2f);
            float pool = 20f * scale;
            float feet = c.Y + small.Height * scale / 2f - pool * 0.1f;
            UiShapes.Glow(new Vector2(c.X, feet), pool * 1.1f, pool * 0.3f, new Color(170, 220, 255, 110));
            PixelArt(small, c, scale);
        }
    }

    // ------------------------------------------------------------------ the name keyboard

    private const float KeyWidth = 96, KeyHeight = 84, KeyGap = 10;

    /// <summary>
    /// Entering a name: who is being named on the left (their field sprite at 6×), and on the right the name so
    /// far in seven slots over the keyboard.
    /// </summary>
    public static void DrawNameEntry(int sw, int sh, NameEntry entry, Texture2D? sprite, PlayerLook look, float appear = 1f,
        string title = "YOUR NAME", string? fallbackName = null)
    {
        ScreenTitle(title);
        Hints(sw - Margin, 44, ("Z", "Pick"), ("X", "Delete"), ("Enter", "To OK"));
        float slide = (1f - appear) * 60f;

        // ---- Who
        var who = new Rectangle(Margin - slide, ContentTop, 500, ContentBottom - ContentTop);
        Panel(who, 34);
        var plate = new Rectangle(who.X + 50, who.Y + 50, who.Width - 100, 470);
        UiShapes.Shape(plate, 30, Disc, Disc, Rule, 4);
        UiShapes.Fill(new Rectangle(plate.X + 36, plate.Y + plate.Height - 76, plate.Width - 72, 16), 8, Rule);
        if (sprite is { } figure)
        {
            const int scale = 6;
            float w = figure.Width * scale, h = figure.Height * scale;
            Raylib.DrawTexturePro(figure, new Rectangle(0, 0, figure.Width, figure.Height),
                new Rectangle(MathF.Round(plate.X + (plate.Width - w) / 2f), MathF.Round(plate.Y + plate.Height - 52 - h), w, h), Vector2.Zero, 0f, Color.White);
        }
        string fallback = fallbackName ?? PlayerIdentity.DefaultName(look);
        DrawWrapped($"Seven letters at most. Leave it empty to be called {fallback}.", who.X + 50, plate.Y + plate.Height + 36, who.Width - 100,
            28, Muted, 40, UiWeight.ExtraBold);

        // ---- The name and the keyboard
        var board = new Rectangle(who.X + who.Width + Gutter + slide * 2, ContentTop, sw - Margin - (who.X + who.Width + Gutter + slide * 2), ContentBottom - ContentTop);
        Panel(board, 34);
        float keysWidth = NameEntry.Columns * KeyWidth + (NameEntry.Columns - 1) * KeyGap;
        float x0 = board.X + (board.Width - keysWidth) / 2f;

        const float slotW = 96, slotH = 116, slotGap = 14;
        float slotsWidth = entry.MaxLength * slotW + (entry.MaxLength - 1) * slotGap;
        float sx = board.X + (board.Width - slotsWidth) / 2f, sy = board.Y + 64;
        bool blink = (int)(FrameClock.Now * 2.5) % 2 == 0;
        for (int i = 0; i < entry.MaxLength; i++)
        {
            var slot = new Rectangle(sx + i * (slotW + slotGap), sy, slotW, slotH);
            bool next = i == entry.Text.Length && !entry.Done;
            UiShapes.Shape(slot, 22, SlotTop, SlotBottom, next ? Selection : Rule, next ? 5f : 3f);
            if (i < entry.Text.Length)
            {
                string ch = entry.Text[i].ToString();
                float cw = UiFonts.Measure(ch, 72, UiWeight.Black);
                UiFonts.DrawCentered(ch, slot.X + (slot.Width - cw) / 2f, slot.Y + slot.Height / 2f, 72, Ink, UiWeight.Black);
            }
            else if (next && blink)
                UiShapes.Fill(new Rectangle(slot.X + 26, slot.Y + slot.Height - 30, slot.Width - 52, 6), 3, Selection);
        }

        Keyboard(new Vector2(x0, board.Y + 250), entry);
    }

    /// <summary>The keyboard: four rows of ten keys and a bottom row of three wide ones; the key under the cursor is a Selection-coloured pill.</summary>
    public static void Keyboard(Vector2 origin, NameEntry entry)
    {
        void Key(Rectangle r, string label, bool selected, float size)
        {
            if (selected)
            {
                UiShapes.Shadow(r, 22, 18, new Vector2(0, 5), Selection with { A = 130 });
                UiShapes.Shape(r, 22, Lighter(Selection, 0.14f), Darker(Selection, 0.06f), Darker(Selection, 0.3f), 3);
            }
            else UiShapes.Shape(r, 22, SlotTop, SlotBottom, Rule, 3);
            var ink = selected ? Color.White : Ink;
            if (label == " ")
            {
                // The space key: a low bar
                UiShapes.Fill(new Rectangle(r.X + 26, r.Y + r.Height - 30, r.Width - 52, 7), 3.5f, ink);
                return;
            }
            float w = UiFonts.Measure(label, size, UiWeight.Black);
            UiFonts.DrawCentered(label, r.X + (r.Width - w) / 2f, r.Y + r.Height / 2f, size, ink, UiWeight.Black);
        }

        for (int row = 0; row < NameEntry.LetterRows; row++)
        {
            string keys = entry.RowKeys(row);
            for (int col = 0; col < NameEntry.Columns; col++)
            {
                var r = new Rectangle(origin.X + col * (KeyWidth + KeyGap), origin.Y + row * (KeyHeight + KeyGap), KeyWidth, KeyHeight);
                Key(r, keys[col].ToString(), entry.Row == row && entry.Column == col && !entry.Done, 44);
            }
        }

        float y = origin.Y + NameEntry.LetterRows * (KeyHeight + KeyGap) + 14;
        for (int i = 0; i < NameEntry.WideKeys.Length; i++)
        {
            int start = NameEntry.WideStarts[i], end = i + 1 < NameEntry.WideStarts.Length ? NameEntry.WideStarts[i + 1] : NameEntry.Columns;
            var r = new Rectangle(origin.X + start * (KeyWidth + KeyGap), y, (end - start) * KeyWidth + (end - start - 1) * KeyGap, KeyHeight);
            string label = NameEntry.WideKeys[i] switch
            {
                NameKey.Case => entry.UpperCase ? "abc" : "ABC",
                NameKey.Delete => "DELETE",
                _ => "OK"
            };
            bool selected = (entry.Row == NameEntry.LetterRows && entry.WideIndex == i) || (entry.Done && NameEntry.WideKeys[i] == NameKey.Done);
            Key(r, label, selected, 34);
        }
    }
}
