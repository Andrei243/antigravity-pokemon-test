using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// Ball Capsules and Seals on screen (plan 06 · R17b; style guide, "Ball Capsules and Seals"): a Seal as a round
/// sticker in its own colour with its sign on it, a capsule as a pale ball with its stickers where they were put,
/// the capsules' screen, the burst a capsule gives (the battle's own, flat: <see cref="SealFx"/>), and the Seal
/// merchant's stall.
/// </summary>
internal static partial class ModernUi
{
    private static readonly List<SealMote> BurstScratch = new();

    // ------------------------------------------------------------------ small signs

    /// <summary>A shape of the battle effects' atlas, flat, <paramref name="size"/> across, turned anticlockwise by <paramref name="rotation"/> radians.</summary>
    public static void FxSign(FxShape shape, Vector2 c, float size, Color tint, float rotation = 0f)
    {
        var atlas = FxTextures.Atlas;
        var (min, max) = FxTextures.UV(shape);
        var source = new Rectangle(min.X * atlas.Width, min.Y * atlas.Height, (max.X - min.X) * atlas.Width, (max.Y - min.Y) * atlas.Height);
        Raylib.DrawTexturePro(atlas, source, new Rectangle(c.X, c.Y, size, size), new Vector2(size / 2f, size / 2f), -rotation * 180f / MathF.PI, tint);
    }

    /// <summary>
    /// The colour of a Seal's sign on its sticker: white on a deep colour, a deep tone of the Seal's own on a pale one
    /// (a white smoke, a yellow star), so every sticker keeps its Seal's own colour.
    /// </summary>
    public static Color StickerInk(int seal)
    {
        var color = SealFx.ColorOf(seal);
        float light = (color.R * 0.3f + color.G * 0.59f + color.B * 0.11f) / 255f;
        return light > 0.66f ? Darker(color, 0.62f) : Color.White;
    }

    /// <summary>
    /// A Seal as a sticker of radius <paramref name="r"/>: a disc of its colour with a white rim and its sign in
    /// <see cref="StickerInk"/>, a letter Seal its letter. <paramref name="alpha"/> fades one that is off the ball or used up.
    /// </summary>
    public static void SealSticker(Vector2 c, float r, int seal, float alpha = 1f)
    {
        byte a = (byte)Math.Clamp((int)(255 * alpha), 0, 255);
        var color = SealFx.ColorOf(seal);
        var ink = StickerInk(seal) with { A = a };
        UiShapes.Circle(c + new Vector2(0, r * 0.1f), r, new Color(14, 22, 46, (int)(70 * alpha)));
        UiShapes.Circle(c, r, Darker(color, 0.34f) with { A = a });
        UiShapes.Circle(c, r * 0.9f, Color.White with { A = a });
        UiShapes.Circle(c, r * 0.8f, color with { A = a });
        var family = SealFx.FamilyOf(seal);
        if (family == SealFamily.Letter)
        {
            string letter = SealFx.LetterOf(seal).ToString();
            float size = r * 1.34f;
            UiFonts.DrawCentered(letter, c.X - UiFonts.Measure(letter, size, UiWeight.Black) / 2f, c.Y + r * 0.02f, size, ink, UiWeight.Black);
            return;
        }
        // A line Seal's ray lies along its length; the rest stand upright
        FxSign(SealFx.ShapeOf(seal), c, r * (family == SealFamily.Line ? 2.4f : 1.7f), ink, family == SealFamily.Line ? MathF.PI / 4f : 0f);
    }

    /// <summary>
    /// A Ball Capsule: a pale ball of radius <paramref name="radius"/> (the capsule's sixty dots) with its Seals where
    /// they were put, the first in front. <paramref name="marked"/> is the Seal in hand or under the cursor.
    /// </summary>
    public static void CapsuleBall(Vector2 c, float radius, BallCapsule capsule, int marked = -1, bool holding = false, bool edge = false)
    {
        float unit = radius / BallSeal.Reach;
        UiShapes.Circle(c + new Vector2(0, radius * 0.05f), radius * 1.02f, new Color(14, 22, 46, 46));
        UiShapes.Circle(c, radius, Color.White);
        UiShapes.Circle(c, radius - MathF.Max(3f, radius * 0.03f), Disc);
        float line = MathF.Max(3f, radius * 0.035f);
        UiShapes.Fill(new Rectangle(c.X - radius + line, c.Y - line / 2f, radius * 2 - line * 2, line), line / 2f, Rule);
        UiShapes.Circle(c, radius * 0.2f, Rule);
        UiShapes.Circle(c, radius * 0.13f, Color.White);
        // The capsule's own edge: where a Seal still sticks
        if (edge) UiShapes.Ring(c, radius + line, line * 0.8f, new Color(255, 255, 255, 70));

        float r = MathF.Max(5f, 10f * unit);
        for (int i = capsule.Seals.Count - 1; i >= 0; i--)
        {
            var seal = capsule.Seals[i];
            var at = c + new Vector2(seal.X - BallSeal.CentreX, seal.Y - BallSeal.CentreY) * unit;
            if (i != marked)
            {
                SealSticker(at, r, seal.Seal);
                continue;
            }
            // In hand it is lifted off the ball, and pales where it would not stick
            if (holding)
            {
                bool sticks = seal.OnTheBall;
                UiShapes.Circle(at + new Vector2(0, r * 0.5f), r * 1.1f, new Color(14, 22, 46, 60));
                SealSticker(at - new Vector2(0, r * 0.3f), r * 1.2f, seal.Seal, sticks ? 1f : 0.55f);
                UiShapes.Ring(at - new Vector2(0, r * 0.3f), r * 1.34f, MathF.Max(3f, r * 0.14f), sticks ? Selection : Muted);
            }
            else
            {
                SealSticker(at, r, seal.Seal);
                UiShapes.Ring(at, r * 1.2f, MathF.Max(3f, r * 0.14f), Selection);
            }
        }
    }

    /// <summary>
    /// What a capsule gives as its Pokémon comes out, flat (the battle's own pieces): on a dark stage, a dot of the
    /// capsule <paramref name="unit"/> layout units, round <paramref name="feet"/> as round a Pokémon's place. A piece's
    /// size is half its width, as the battle's quads have it.
    /// </summary>
    public static void SealBurst(Vector2 feet, float unit, BallCapsule capsule, float age, int seed)
    {
        BurstScratch.Clear();
        SealFx.Motes(capsule, age, seed, BurstScratch);
        // What is matter first, then what is light on top of it
        foreach (var mote in BurstScratch)
            if (mote.Blend == FxBlend.Alpha) FxSign(mote.Shape, feet + new Vector2(mote.At.X, -mote.At.Y) * unit, mote.Size * unit * 2f, mote.Color, mote.Rotation);
        Raylib.BeginBlendMode(BlendMode.Additive);
        foreach (var mote in BurstScratch)
            if (mote.Blend == FxBlend.Add) FxSign(mote.Shape, feet + new Vector2(mote.At.X, -mote.At.Y) * unit, mote.Size * unit * 2f, mote.Color, mote.Rotation);
        Raylib.EndBlendMode();
    }

    // The little stage a capsule is tried out on: a Pokémon coming out of its ball, over and over
    private static void CapsuleStage(Rectangle r, BallCapsule capsule, Pokemon? pokemon, float age, int seed)
    {
        UiShapes.Shape(r, 28, new Color(40, 46, 78, 255), new Color(26, 30, 54, 255), Frame, 3);
        float loop = SealFx.Length + SealFx.Pause;
        float t = age % loop;
        var feet = new Vector2(r.X + r.Width / 2f, r.Y + r.Height - 44);
        float unit = 1.6f;
        // The pieces stay on the stage
        Raylib.BeginScissorMode((int)((r.X + 4) * GameEngine.RenderScale), (int)((r.Y + 4) * GameEngine.RenderScale), (int)((r.Width - 8) * GameEngine.RenderScale), (int)((r.Height - 8) * GameEngine.RenderScale));
        UiShapes.Circle(feet + new Vector2(0, 150), 190, new Color(255, 255, 255, 14));
        if (pokemon != null)
        {
            // It grows out of the ball as the ball opens
            float grow = UiMotion.EaseOut(Math.Clamp(t / 0.3f, 0f, 1f));
            var sprite = PixelArtGenerator.SpriteOf(pokemon);
            float w = sprite.Width * 2 * grow, h = sprite.Height * 2 * grow;
            Raylib.DrawTexturePro(sprite, new Rectangle(0, 0, sprite.Width, sprite.Height),
                new Rectangle(MathF.Round(feet.X - w / 2f), MathF.Round(feet.Y - h), w, h), Vector2.Zero, 0f, Color.White);
        }
        if (!capsule.IsEmpty) SealBurst(feet - new Vector2(0, 80f * unit * 0.35f), unit, capsule, t, seed);
        Raylib.EndScissorMode();
    }

    // ------------------------------------------------------------------ the capsules' screen

    public static void DrawBallCapsules(int sw, int sh, SealCaseScreen screen, Party party, float appear)
    {
        if (screen.Step == CapsuleStep.Attach)
        {
            DrawPartyChoice(sw, sh, party, screen.Target, screen.Prompt,
                p => p.IsEgg ? "NOT ABLE" : p.CapsuleId != 0 ? $"CAPSULE {p.CapsuleId}" : null, Math.Clamp(screen.StepAge / 0.35f, 0f, 1f), "Put on");
            return;
        }
        Backdrop(sw, sh);
        ScreenTitle("BALL CAPSULES");
        if (screen.Edit != null)
        {
            CapsuleEditing(sw, sh, screen, party);
            return;
        }
        Hints(sw - Margin, 44, ("Z", screen.Step == CapsuleStep.Swap ? "Swap" : "Choose"), ("Esc", "Back"));
        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        const float listW = 1000, promptH = 96;

        // ---- The twelve
        var list = new Rectangle(Margin - slide, ContentTop, listW, ContentBottom - ContentTop - promptH - Gutter);
        Panel(list, 34);
        const float pad = 28, gap = 16;
        float cw = (list.Width - pad * 2 - gap * (SealCaseScreen.Columns - 1)) / SealCaseScreen.Columns;
        float ch = (list.Height - pad * 2 - gap * 2) / 3f;
        for (int i = 0; i < SealCase.Capsules; i++)
        {
            var r = new Rectangle(list.X + pad + (i % SealCaseScreen.Columns) * (cw + gap), list.Y + pad + (i / SealCaseScreen.Columns) * (ch + gap), cw, ch);
            bool second = screen.Step == CapsuleStep.Swap && i == screen.Other;
            bool first = i == screen.Cursor;
            Card(r, 26, first || second, second ? Gold : Selection);
            var capsule = screen.Case.Capsule(i);
            UiFonts.Draw($"{i + 1}", r.X + 18, r.Y + 10, 26, first || second ? Ink : Muted, UiWeight.Black);
            var c = new Vector2(r.X + r.Width / 2f, r.Y + r.Height / 2f + 4);
            CapsuleBall(c, MathF.Min(r.Width, r.Height) * 0.36f, capsule);
            if (screen.HolderOf(i) is { } holder)
            {
                var at = new Vector2(r.X + r.Width - 42, r.Y + r.Height - 40);
                UiShapes.Circle(at, 34, Color.White);
                UiShapes.Ring(at, 34, 3, Rule);
                PixelArt(PixelArtGenerator.IconOf(holder), at, 1, Hop(first));
            }
        }

        // ---- The prompt
        var box = new Rectangle(Margin - slide, ContentBottom - promptH, listW, promptH);
        Panel(box, 30);
        UiFonts.DrawCentered(screen.Prompt, box.X + 52, box.Y + box.Height / 2f, 36, Ink, UiWeight.ExtraBold);

        // ---- The one under the cursor, large, and what it gives
        var detail = new Rectangle(Margin + listW + Gutter + slide, ContentTop, sw - Margin * 2 - listW - Gutter, ContentBottom - ContentTop);
        Panel(detail, 34);
        var shown = screen.Case.Capsule(screen.Cursor);
        Label("BALL CAPSULE", detail.X + 44, detail.Y + 30);
        UiFonts.Draw($"No. {screen.Cursor + 1}", detail.X + 44, detail.Y + 54, 46, Ink, UiWeight.Black);
        string count = shown.IsEmpty ? "EMPTY" : $"{shown.Seals.Count} / {BallCapsule.MostSeals} SEALS";
        Tag(detail.X + detail.Width - 44 - UiFonts.Measure(count, 24, UiWeight.Black) - 34, detail.Y + 40, count, shown.IsEmpty ? Muted : Frame, 42);
        CapsuleBall(new Vector2(detail.X + detail.Width / 2f, detail.Y + 300), 168, shown);

        var on = screen.HolderOf(screen.Cursor);
        float y = detail.Y + 500;
        UiShapes.Fill(new Rectangle(detail.X + 44, y, detail.Width - 88, 3), 1.5f, Rule);
        Label("ON THE BALL OF", detail.X + 44, y + 22);
        UiFonts.Draw(on?.DisplayName ?? "Nobody yet", detail.X + 44, y + 44, 34, on != null ? Ink : Muted, UiWeight.Black);
        var stage = new Rectangle(detail.X + 28, y + 106, detail.Width - 56, detail.Height - (y - detail.Y) - 106 - 28);
        CapsuleStage(stage, shown, on ?? party.Members.FirstOrDefault(p => !p.IsEgg), screen.Age, 7 + screen.Cursor);

        if (screen.Step == CapsuleStep.Menu)
        {
            var rows = screen.Menu.Select(SealCaseScreen.Label).ToList();
            float height = MenuPanelHeight(rows.Count);
            MenuPanel(box.X + box.Width - 360, box.Y - Gutter - height + 4, rows, screen.MenuIndex, 360);
        }
    }

    // A capsule being changed: the case's Seals a page at a time on the left, the ball large on the right
    private static void CapsuleEditing(int sw, int sh, SealCaseScreen screen, Party party)
    {
        var edit = screen.Edit!;
        switch (screen.Step)
        {
            case CapsuleStep.Holding:
                Hints(sw - Margin, 44, ("Arrows", "Move"), ("Z", "Stick on"), ("Esc", "Put back"));
                break;
            case CapsuleStep.OnBall:
                Hints(sw - Margin, 44, ("Left / Right", "Seal"), ("Z", "Pick up"), ("C", "Case"), ("Enter", "Keep"));
                break;
            default:
                Hints(sw - Margin, 44, ("Z", "Take"), ("C", "Ball"), ("Enter", "Keep"), ("Esc", "Back"));
                break;
        }
        const float listW = 700, promptH = 96;

        // ---- The case's Seals
        var list = new Rectangle(Margin, ContentTop, listW, ContentBottom - ContentTop - promptH - Gutter);
        Panel(list, 34);
        Label("SEAL CASE", list.X + 44, list.Y + 30);
        string page = $"{screen.Page + 1} / {screen.Pages}";
        Tag(list.X + list.Width - 44 - UiFonts.Measure(page, 24, UiWeight.Black) - 34, list.Y + 22, page, Frame, 38);
        var kinds = screen.PageKinds;
        if (kinds.Count == 0) EmptyNote(list, "No Seals yet.");
        const float top = 78, pad = 28, gap = 14;
        float cw = (list.Width - pad * 2 - gap) / 2f, ch = (list.Height - top - pad - gap * 3) / 4f;
        bool here = screen.Step == CapsuleStep.Seals || screen.Step == CapsuleStep.Quit;
        for (int i = 0; i < kinds.Count; i++)
        {
            var r = new Rectangle(list.X + pad + (i % 2) * (cw + gap), list.Y + top + (i / 2) * (ch + gap), cw, ch);
            int loose = screen.Case.Count(kinds[i]);
            bool selected = here && i == screen.SealCursor;
            Card(r, 24, selected);
            SealSticker(new Vector2(r.X + 62, r.Y + r.Height / 2f), 40, kinds[i], loose > 0 ? 1f : 0.4f);
            string name = SealDatabase.Name(kinds[i]);
            float size = 28, room = r.Width - 124 - 20, nw = UiFonts.Measure(name, size, UiWeight.Black);
            if (nw > room) size = MathF.Floor(size * room / nw);
            UiFonts.DrawCentered(name, r.X + 122, r.Y + r.Height / 2f - 16, size, loose > 0 ? Ink : Muted, UiWeight.Black);
            UiFonts.DrawCentered($"×{loose}", r.X + 122, r.Y + r.Height / 2f + 20, 24, loose > 0 ? Muted : Red, UiWeight.Black);
        }

        var box = new Rectangle(Margin, ContentBottom - promptH, listW, promptH);
        Panel(box, 30);
        string prompt = screen.Prompt;
        float ps = 34, pw = UiFonts.Measure(prompt, ps, UiWeight.ExtraBold);
        if (pw > box.Width - 96) ps = MathF.Floor(ps * (box.Width - 96) / pw);
        UiFonts.DrawCentered(prompt, box.X + 48, box.Y + box.Height / 2f, ps, Ink, UiWeight.ExtraBold);

        // ---- The ball
        var ballPanel = new Rectangle(Margin + listW + Gutter, ContentTop, sw - Margin * 2 - listW - Gutter, ContentBottom - ContentTop);
        Panel(ballPanel, 34);
        Label("BALL CAPSULE", ballPanel.X + 44, ballPanel.Y + 30);
        UiFonts.Draw($"No. {edit.Capsule + 1}", ballPanel.X + 44, ballPanel.Y + 54, 46, Ink, UiWeight.Black);
        string on = $"{edit.Seals.Count} / {BallCapsule.MostSeals} SEALS";
        Tag(ballPanel.X + ballPanel.Width - 44 - UiFonts.Measure(on, 24, UiWeight.Black) - 34, ballPanel.Y + 40, on, edit.Full ? Red : Frame, 42);

        const float radius = 264;
        var c = new Vector2(ballPanel.X + 44 + SealCaseScreen.Range * radius / BallSeal.Reach + 6, ballPanel.Y + 500);
        // The ground a Seal in hand can be carried over
        UiShapes.Circle(c, SealCaseScreen.Range * radius / BallSeal.Reach + 26, new Color(226, 234, 246, 110));
        bool onBall = screen.Step is CapsuleStep.Holding or CapsuleStep.OnBall;
        CapsuleBall(c, radius, edit.Result, onBall && edit.Seals.Count > 0 ? screen.Held : -1, screen.Step == CapsuleStep.Holding);

        // ---- What it gives, tried out on the Pokémon it is on (or the team's first)
        float sx = c.X + SealCaseScreen.Range * radius / BallSeal.Reach + 44;
        var stage = new Rectangle(sx, ballPanel.Y + 140, ballPanel.X + ballPanel.Width - 28 - sx, 420);
        var holder = screen.HolderOf(edit.Capsule);
        CapsuleStage(stage, edit.Result, holder ?? party.Members.FirstOrDefault(p => !p.IsEgg), screen.Age, 7 + edit.Capsule);
        Label("COMES OUT LIKE THIS", stage.X + 16, stage.Y + stage.Height + 16);
        if (holder != null)
        {
            Label("ON THE BALL OF", stage.X + 16, stage.Y + stage.Height + 70);
            UiFonts.Draw(holder.DisplayName, stage.X + 16, stage.Y + stage.Height + 92, 30, Ink, UiWeight.Black);
        }

        if (screen.Step == CapsuleStep.Quit)
            Prompt(sw, sh, "Leave it as it was?", "The Seals you moved go back where they were.", new[] { ("YES", Red), ("NO", Blue) }, screen.QuitChoice);
    }

    // ------------------------------------------------------------------ the Seal merchant's stall

    public static void DrawSealShop(int sw, int sh, SealShopScreen shop, int money, float appear, float asking, bool askingVisible)
    {
        Backdrop(sw, sh);
        ScreenTitle("SEAL MERCHANT");
        Hints(sw - Margin, 44, ("Z", "Buy"), ("Esc", "Leave"));
        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;

        var list = new Rectangle(Margin - slide, ContentTop, 920, ContentBottom - ContentTop);
        Panel(list, 34);
        Label("TODAY'S SEALS", list.X + 44, list.Y + 30);
        for (int row = 0; row < shop.Stock.Count; row++)
        {
            int seal = shop.Stock[row];
            var r = RowRect(list, row, 72);
            bool selected = row == shop.Cursor;
            var ink = ListRow(r, selected, SealDatabase.Name(seal));
            SealSticker(new Vector2(r.X + 52, r.Y + r.Height / 2f), 30, seal);
            int price = SealDatabase.Price(seal);
            MoneyRight(r.X + r.Width - 32, r.Y + r.Height / 2f, price, 30, selected ? Color.White : price <= money ? ink : Red);
        }

        float rx = Margin + 920 + Gutter + slide, rw = sw - Margin * 2 - 920 - Gutter;
        var purse = new Rectangle(rx, ContentTop, rw, 132);
        Panel(purse, 34);
        UiFonts.DrawCentered("MONEY", purse.X + 44, purse.Y + purse.Height / 2f, 26, Muted, UiWeight.Black);
        MoneyRight(purse.X + purse.Width - 44, purse.Y + purse.Height / 2f, money, 52, Ink);

        var detail = new Rectangle(rx, ContentTop + 132 + 24, rw, ContentBottom - ContentTop - 132 - 24);
        Panel(detail, 34);
        if (shop.Selected is > 0 and var chosen)
        {
            var disc = new Vector2(detail.X + 44 + 96, detail.Y + 44 + 96);
            UiShapes.Circle(disc, 96, Disc);
            SealSticker(disc, 64, chosen);
            float x = detail.X + 44 + 192 + 36;
            UiFonts.DrawCentered(SealDatabase.Name(chosen), x, detail.Y + 112, 44, Ink, UiWeight.Black);
            Tag(x, detail.Y + 152, $"IN CASE ×{shop.Case.Anywhere(chosen)}", Frame, 38);
            UiShapes.Fill(new Rectangle(detail.X + 44, detail.Y + 280, detail.Width - 88, 3), 1.5f, Rule);
            DrawWrapped(SealBlurb(chosen), detail.X + 44, detail.Y + 310, detail.Width - 88, 30, Ink, 42, UiWeight.ExtraBold);
            // Tried out on nobody: just what bursts from it
            var stage = new Rectangle(detail.X + 28, detail.Y + detail.Height - 28 - 240, detail.Width - 56, 240);
            CapsuleStage(stage, new BallCapsule(new[] { new BallSeal(chosen, BallSeal.CentreX, BallSeal.CentreY + 20) }), null, shop.Age, 3 + chosen);
        }

        if (askingVisible && shop.Selected is > 0 and var buying) SealQuantity(sw, sh, buying, shop.Quantity, asking);
    }

    /// <summary>What a Seal gives, in our own words, by its kind.</summary>
    public static string SealBlurb(int seal) => SealFx.FamilyOf(seal) switch
    {
        SealFamily.Heart => "Hearts float up as the Pokémon comes out of its ball.",
        SealFamily.Star => "Stars fly out all round as the Pokémon comes out of its ball.",
        SealFamily.Line => "Rays shoot out as the Pokémon comes out of its ball.",
        SealFamily.Smoke => "Puffs of smoke roll out as the Pokémon comes out of its ball.",
        SealFamily.Ele => "Sparks crackle as the Pokémon comes out of its ball.",
        SealFamily.Foamy => "Bubbles drift up as the Pokémon comes out of its ball.",
        SealFamily.Fire => "Flames lick up as the Pokémon comes out of its ball.",
        SealFamily.Party => "Confetti is thrown as the Pokémon comes out of its ball.",
        SealFamily.Flora => "Petals scatter as the Pokémon comes out of its ball.",
        SealFamily.Song => "Notes dance out as the Pokémon comes out of its ball.",
        SealFamily.Letter => "Its letter pops out as the Pokémon comes out of its ball. Several spell a word.",
        SealFamily.Liquid => "Drops splash out as the Pokémon comes out of its ball.",
        SealFamily.Burst => "It goes off with a bang as the Pokémon comes out of its ball.",
        _ => "Twinkles wink as the Pokémon comes out of its ball."
    };

    private static void SealQuantity(int sw, int sh, int seal, int quantity, float appear)
    {
        Dim(sw, sh, (int)(150 * appear));
        var r = new Rectangle(sw / 2f - 480, sh / 2f - 220 + (1f - appear) * 60f, 960, 440);
        Panel(r, 36);
        UiFonts.Draw("Buy how many?", r.X + 56, r.Y + 44, 48, Ink, UiWeight.Black);
        HintsDark(r.X + r.Width - 40, r.Y + 46, ("Z", "Buy"), ("Esc", "Back"));
        int each = SealDatabase.Price(seal);
        var c = new Vector2(r.X + 56 + 52, r.Y + 176);
        UiShapes.Circle(c, 52, Disc);
        SealSticker(c, 36, seal);
        UiFonts.DrawCentered(SealDatabase.Name(seal), r.X + 56 + 128, r.Y + 160, 40, Ink, UiWeight.Black);
        float width = Money(r.X + 56 + 128, r.Y + 204, each, 26, Muted);
        UiFonts.DrawCentered("each", r.X + 56 + 128 + width + 10, r.Y + 204, 24, Muted, UiWeight.ExtraBold);
        Stepper(new Vector2(r.X + r.Width - 56 - 44 - 120 - 24, r.Y + 180), $"× {quantity}");
        UiShapes.Fill(new Rectangle(r.X + 56, r.Y + 272, r.Width - 112, 3), 1.5f, Rule);
        UiFonts.DrawCentered("TOTAL", r.X + 56, r.Y + 350, 28, Muted, UiWeight.Black);
        MoneyRight(r.X + r.Width - 56, r.Y + 350, each * quantity, 60, Ink);
    }
}
