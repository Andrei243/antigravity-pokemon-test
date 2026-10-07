using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The screens where Pokémon are picked from a set: the PC's boxes and the choice of a partner.</summary>
internal static partial class ModernUi
{
    private static readonly Color SlotTop = new(244, 247, 252, 255), SlotBottom = new(232, 238, 246, 255);

    // ------------------------------------------------------------------ PC boxes

    /// <summary>
    /// The storage system in three columns: the party, the box (six slots by five under its name), and the
    /// Pokémon under the cursor.
    /// </summary>
    public static void DrawStorage(int sw, int sh, PCScreen pc, Party party, PcBoxes stored, float appear = 1f)
    {
        Backdrop(sw, sh);
        ScreenTitle("PC BOXES");
        if (pc.Zone == StorageZone.BoxName) Hints(sw - Margin, 44, ("Left / Right", "Change box"), ("Esc", "Back"));
        else Hints(sw - Margin, 44, ("Z", pc.Zone == StorageZone.Party ? "Deposit" : "Withdraw"), ("Esc", "Back"));

        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        float height = ContentBottom - ContentTop;

        // ---- The party
        var team = new Rectangle(Margin - slide, ContentTop, 500, height);
        Panel(team, 34);
        Label($"PARTY  {party.Count} / {Party.MaxSize}", team.X + 40, team.Y + 30);
        for (int i = 0; i < Party.MaxSize; i++)
        {
            var r = new Rectangle(team.X + 20, team.Y + 66 + i * 136, team.Width - 40, 124);
            if (i >= party.Count)
            {
                UiShapes.Shape(r, 26, SlotTop, SlotBottom, Rule, 3);
                continue;
            }
            var p = party.Members[i];
            bool selected = pc.Zone == StorageZone.Party && pc.PartyIndex == i;
            Card(r, 26, selected);
            var c = new Vector2(r.X + 70, r.Y + r.Height / 2f);
            UiShapes.Circle(c, 50, Disc);
            PixelArt(PixelArtGenerator.GetPokemonIcon(p.ModelName), c + new Vector2(0, -3), 2, Hop(selected, p.IsFainted) * 2,
                p.IsFainted ? new Color(170, 170, 190, 255) : Color.White);
            UiFonts.DrawCentered(p.DisplayName, r.X + 136, r.Y + 42, 32, Ink, UiWeight.Black);
            Level(r.X + r.Width - 28, r.Y + 42, p.Level, 30);
            HpBar(r.X + 136, r.Y + 78, r.Width - 136 - 28, 22, (float)p.CurrentHP / Math.Max(1, p.MaxHP));
        }

        // ---- The box
        var box = new Rectangle(team.X + team.Width + Gutter + slide, ContentTop, 792, height);
        Panel(box, 34);
        bool naming = pc.Zone == StorageZone.BoxName;
        string name = stored.Boxes[pc.Box].Name.ToUpperInvariant();
        float nameW = UiFonts.Measure(name, 44, UiWeight.Black);
        var plate = new Rectangle(box.X + box.Width / 2f - 190, box.Y + 30, 380, 76);
        if (naming)
        {
            UiShapes.Shadow(plate, 38, 18, new Vector2(0, 5), Selection with { A = 120 });
            UiShapes.Shape(plate, 38, Lighter(Selection, 0.14f), Darker(Selection, 0.06f), Darker(Selection, 0.3f), 3);
        }
        else UiShapes.Shape(plate, 38, SlotTop, SlotBottom, Rule, 3);
        UiFonts.DrawCentered(name, plate.X + (plate.Width - nameW) / 2f, plate.Y + plate.Height / 2f, 44, naming ? Color.White : Ink, UiWeight.Black);
        var arrow = naming ? Selection : new Color(170, 180, 200, 255);
        UiIcons.ArrowH(new Vector2(plate.X - 44, plate.Y + plate.Height / 2f), 22, -1, arrow);
        UiIcons.ArrowH(new Vector2(plate.X + plate.Width + 44, plate.Y + plate.Height / 2f), 22, 1, arrow);

        int inBox = stored.Boxes[pc.Box].Count;
        string count = $"{inBox} / {PCScreen.BoxSize}";
        UiFonts.DrawCentered(count, box.X + (box.Width - UiFonts.Measure(count, 24, UiWeight.Black)) / 2f, box.Y + 136, 24, Muted, UiWeight.Black);

        const float cell = 116, gap = 10;
        float gridX = box.X + (box.Width - (cell * PCScreen.Columns + gap * (PCScreen.Columns - 1))) / 2f, gridY = box.Y + 176;
        for (int i = 0; i < PCScreen.BoxSize; i++)
        {
            var r = new Rectangle(gridX + i % PCScreen.Columns * (cell + gap), gridY + i / PCScreen.Columns * (cell + gap), cell, cell);
            bool selected = pc.Zone == StorageZone.Box && pc.Cell == i;
            if (selected) UiShapes.Shadow(r, 24, 22, Vector2.Zero, Selection with { A = 190 });
            UiShapes.Shape(r, 24, SlotTop, SlotBottom, selected ? Selection : Rule, selected ? 6f : 3f);
            if (stored[pc.Box, i] is not { } p) continue;
            PixelArt(PixelArtGenerator.GetPokemonIcon(p.ModelName), new Vector2(r.X + cell / 2f, r.Y + cell / 2f - 2), 2, Hop(selected) * 2);
        }

        // ---- The Pokémon under the cursor
        var detail = new Rectangle(box.X + box.Width + Gutter + slide, ContentTop, sw - Margin - (box.X + box.Width + Gutter + slide), height);
        Panel(detail, 34);
        if (pc.Under(party, stored) is not { } shown)
        {
            string note = naming ? "Left and right change box." : "An empty slot.";
            UiFonts.DrawCentered(note, detail.X + (detail.Width - UiFonts.Measure(note, 26, UiWeight.ExtraBold)) / 2f, detail.Y + detail.Height / 2f, 26, Muted, UiWeight.ExtraBold);
            return;
        }

        var disc = new Vector2(detail.X + detail.Width / 2f, detail.Y + 40 + 150);
        BallDisc(disc, 150);
        PixelArt(PixelArtGenerator.GetPokemonSprite(shown.ModelName, isBack: false), disc, 2);
        float x = detail.X + 36, y = detail.Y + 376;
        NameWithGender(shown, x, y, 40);
        Level(detail.X + detail.Width - 36, y, shown.Level, 34);
        float tx = TypePills(x, y + 36, shown, 36);
        StatusPill(tx + 2, y + 36, shown.IsFainted ? StatusCondition.Faint : shown.Status, 36);
        HpBar(x, y + 96, detail.Width - 72, 22, (float)shown.CurrentHP / Math.Max(1, shown.MaxHP));
        string hp = $"{shown.CurrentHP} / {shown.MaxHP}";
        UiFonts.DrawCentered(hp, detail.X + detail.Width - 36 - UiFonts.Measure(hp, 26, UiWeight.Black), y + 142, 26, Ink, UiWeight.Black);
        UiFonts.DrawCentered(shown.Nature.ToString(), x, y + 142, 26, Muted, UiWeight.ExtraBold);

        UiShapes.Fill(new Rectangle(x, y + 176, detail.Width - 72, 3), 1.5f, Rule);
        Label("MOVES", x, y + 196);
        for (int i = 0; i < shown.Moves.Count && i < 4; i++)
        {
            float my = y + 244 + i * 46;
            UiShapes.Circle(new Vector2(x + 10, my), 10, Palette.GetTypeColor(shown.Moves[i].Type.ToString()));
            UiFonts.DrawCentered(shown.Moves[i].Name, x + 34, my, 28, Ink, UiWeight.ExtraBold);
        }
        if (shown.HeldItem is { } held)
        {
            Label("HOLDING", x, y + 436);
            UiFonts.Draw(held.Name, x, y + 460, 28, Ink, UiWeight.ExtraBold);
        }
    }

    // ------------------------------------------------------------------ the choice of a partner

    /// <summary>
    /// Sinnoh's three partners as three cards, the chosen one's entry under them, and "Choose …?" over them
    /// once one has been picked.
    /// </summary>
    public static void DrawStarters(int sw, int sh, StarterSelectScreen screen, float appear, float asking, bool askingVisible)
    {
        Backdrop(sw, sh);
        ScreenTitle("CHOOSE A PARTNER");
        Hints(sw - Margin, 44, ("Left / Right", "Look"), ("Z", "Choose"));

        const float gap = 40;
        float w = (sw - Margin * 2 - gap * 2) / 3f;
        var names = StarterSelectScreen.Starters;
        for (int i = 0; i < names.Length; i++)
        {
            var species = PokemonDatabase.Get(names[i])!;
            float rise = (1f - UiMotion.EaseOut(appear * 1.5f - i * 0.14f)) * 50f;
            var r = new Rectangle(Margin + i * (w + gap), ContentTop + rise, w, 616);
            bool selected = i == screen.SelectedIndex;
            Card(r, 40, selected);

            var disc = new Vector2(r.X + r.Width / 2f, r.Y + 44 + 196);
            BallDisc(disc, 196);
            PixelArt(PixelArtGenerator.GetPokemonSprite(species.Name, isBack: false), disc, 3, Hop(selected, !selected) * 3);

            float nameW = UiFonts.Measure(species.Name, 48, UiWeight.Black);
            UiFonts.DrawCentered(species.Name, r.X + (r.Width - nameW) / 2f, r.Y + 480, 48, Ink, UiWeight.Black);
            string kind = $"{species.Category} Pokémon";
            UiFonts.DrawCentered(kind, r.X + (r.Width - UiFonts.Measure(kind, 26, UiWeight.ExtraBold)) / 2f, r.Y + 524, 26, Muted, UiWeight.ExtraBold);
            float pillW = 150;
            TypePill(r.X + (r.Width - pillW) / 2f, r.Y + 548, species.PrimaryType, 38, pillW);
        }

        var chosen = PokemonDatabase.Get(names[screen.SelectedIndex])!;
        var text = new Rectangle(Margin, ContentTop + 616 + 28, sw - Margin * 2, ContentBottom - ContentTop - 644);
        Panel(text, 34);
        UiFonts.DrawCentered($"No. {chosen.DexNumber:D3}", text.X + 48, text.Y + 52, 28, Muted, UiWeight.Black);
        UiFonts.DrawCentered(chosen.Name, text.X + 48 + UiFonts.Measure($"No. {chosen.DexNumber:D3}", 28, UiWeight.Black) + 24, text.Y + 50, 40, Ink, UiWeight.Black);
        DrawWrapped(chosen.DexEntry, text.X + 48, text.Y + 96, text.Width - 96, 30, Ink, 42);

        if (askingVisible)
            Prompt(sw, sh, $"Choose {chosen.Name}?", $"The {chosen.Category} Pokémon will be your partner from here on.",
                new[] { ("NO, LOOK AGAIN", Blue), ($"YES, {chosen.Name.ToUpperInvariant()}", Green) }, screen.AnswerYes ? 1 : 0, asking);
    }
}
