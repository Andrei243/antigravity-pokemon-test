using System;
using System.Collections.Generic;
using System.Globalization;
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

    private const float BoxCell = 116, BoxGap = 10;

    /// <summary>
    /// The storage system in three columns: the party, the box (its wallpaper, its name, six places by five), and
    /// the Pokémon under the cursor or carried; over them the screen's menus and questions, and in place of it all
    /// the keyboard while a box is being named.
    /// </summary>
    public static void DrawStorage(int sw, int sh, PCScreen pc, Party party, PcBoxes stored, float appear = 1f)
    {
        var current = stored.Boxes[pc.Box];
        if (pc.Naming is { } naming)
        {
            DrawBoxNaming(sw, sh, naming, current.Name, current.Wallpaper);
            return;
        }

        Backdrop(sw, sh);
        ScreenTitle("PC BOXES");
        var under = pc.Under(party, stored);
        if (pc.AskingRelease) Hints(sw - Margin, 44, ("Z", "Release"), ("Esc", "Keep it"));
        else if (pc.Marking != null) Hints(sw - Margin, 44, ("Arrows", "Choose"), ("Z", "Mark"), ("Esc", "Back"));
        else if (pc.Menu != null || pc.BoxMenu != null) Hints(sw - Margin, 44, ("Up / Down", "Choose"), ("Z", "Do it"), ("Esc", "Back"));
        else if (pc.Held != null) Hints(sw - Margin, 44, ("Z", "Put down"), ("Esc", "Put back"));
        else if (pc.Zone == StorageZone.BoxName) Hints(sw - Margin, 44, ("Left / Right", "Change box"), ("Z", "Box"), ("Esc", "Close"));
        else if (under != null) Hints(sw - Margin, 44, ("Z", "Choose"), ("Esc", "Close"));
        else Hints(sw - Margin, 44, ("Esc", "Close"));

        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        float height = ContentBottom - ContentTop;

        // ---- The party
        var team = new Rectangle(Margin - slide, ContentTop, 500, height);
        Panel(team, 34);
        Label($"PARTY  {party.Count} / {Party.MaxSize}", team.X + 40, team.Y + 30);
        Rectangle PartyCard(int i) => new(team.X + 20, team.Y + 66 + i * 136, team.Width - 40, 124);
        for (int i = 0; i < Party.MaxSize; i++)
        {
            var r = PartyCard(i);
            bool selected = pc.Zone == StorageZone.Party && pc.PartyIndex == i;
            if (i >= party.Count)
            {
                UiShapes.Shape(r, 26, SlotTop, SlotBottom, selected ? Selection : Rule, selected ? 5f : 3f);
                continue;
            }
            var p = party.Members[i];
            Card(r, 26, selected);
            var c = new Vector2(r.X + 70, r.Y + r.Height / 2f);
            UiShapes.Circle(c, 50, Disc);
            PixelArt(PixelArtGenerator.IconOf(p), c + new Vector2(0, -3), 2, Hop(selected, p.IsFainted) * 2,
                p.IsFainted ? new Color(170, 170, 190, 255) : Color.White);
            UiFonts.DrawCentered(p.DisplayName, r.X + 136, r.Y + 42, 32, Ink, UiWeight.Black);
            Level(r.X + r.Width - 28, r.Y + 42, p.Level, 30);
            HpBar(r.X + 136, r.Y + 78, r.Width - 136 - 28, 22, (float)p.CurrentHP / Math.Max(1, p.MaxHP));
        }

        // ---- The box: its wallpaper, its name over the places, which box it is, the places
        var box = new Rectangle(team.X + team.Width + Gutter + slide, ContentTop, 792, height);
        Panel(box, 34);
        Wallpaper(new Rectangle(box.X + 12, box.Y + 12, box.Width - 24, box.Height - 24), current.Wallpaper);

        bool onName = pc.Zone == StorageZone.BoxName;
        string name = current.Name.ToUpperInvariant();
        float nameW = UiFonts.Measure(name, 44, UiWeight.Black);
        var plate = new Rectangle(box.X + box.Width / 2f - 190, box.Y + 32, 380, 76);
        if (onName)
        {
            UiShapes.Shadow(plate, 38, 18, new Vector2(0, 5), Selection with { A = 120 });
            UiShapes.Shape(plate, 38, Lighter(Selection, 0.14f), Darker(Selection, 0.06f), Darker(Selection, 0.3f), 3);
        }
        else
        {
            UiShapes.Shadow(plate, 38, 14, new Vector2(0, 5), ShadowColor);
            UiShapes.Shape(plate, 38, SlotTop, SlotBottom, Rule, 3);
        }
        UiFonts.DrawCentered(name, plate.X + (plate.Width - nameW) / 2f, plate.Y + plate.Height / 2f, 44, onName ? Color.White : Ink, UiWeight.Black);
        for (int side = -1; side <= 1; side += 2)
        {
            var at = new Vector2(side < 0 ? plate.X - 46 : plate.X + plate.Width + 46, plate.Y + plate.Height / 2f);
            UiShapes.Circle(at, 28, new Color(255, 255, 255, 215));
            UiIcons.ArrowH(at + new Vector2(side * 2, 0), 20, side, onName ? Selection : new Color(150, 162, 186, 255));
        }

        // Eighteen dots: which box this is
        const float dotStep = 20;
        var dots = new Rectangle(box.X + box.Width / 2f - (dotStep * (PCScreen.BoxCount - 1)) / 2f - 18, plate.Y + plate.Height + 14, dotStep * (PCScreen.BoxCount - 1) + 36, 24);
        UiShapes.Fill(dots, 12, new Color(255, 255, 255, 190));
        for (int b = 0; b < PCScreen.BoxCount; b++)
        {
            var c = new Vector2(dots.X + 18 + b * dotStep, dots.Y + dots.Height / 2f);
            if (b == pc.Box) UiShapes.Circle(c, 6.5f, Selection);
            else UiShapes.Circle(c, 4.5f, new Color(Frame.R, Frame.G, Frame.B, (byte)110));
        }

        float gridX = box.X + (box.Width - (BoxCell * PCScreen.Columns + BoxGap * (PCScreen.Columns - 1))) / 2f, gridY = box.Y + 164;
        Rectangle CellRect(int i) => new(gridX + i % PCScreen.Columns * (BoxCell + BoxGap), gridY + i / PCScreen.Columns * (BoxCell + BoxGap), BoxCell, BoxCell);
        for (int i = 0; i < PCScreen.BoxSize; i++)
        {
            var r = CellRect(i);
            bool selected = pc.Zone == StorageZone.Box && pc.Cell == i;
            if (selected)
            {
                UiShapes.Shadow(r, 24, 22, Vector2.Zero, Selection with { A = 190 });
                UiShapes.Shape(r, 24, new Color(255, 255, 255, 170), new Color(255, 255, 255, 140), Selection, 6f);
            }
            else UiShapes.Shape(r, 24, new Color(255, 255, 255, 105), new Color(255, 255, 255, 80), new Color(255, 255, 255, 160), 3f);
            if (current.Slots[i] is not { } p) continue;
            bool lifted = selected && pc.Held != null;
            PixelArt(PixelArtGenerator.IconOf(p), new Vector2(r.X + BoxCell / 2f, r.Y + BoxCell / 2f - 2), 2,
                lifted ? 0 : Hop(selected) * 2, lifted ? new Color(255, 255, 255, 150) : null);
        }

        // How many it holds and its wallpaper, under the places
        float tagY = gridY + PCScreen.Rows * (BoxCell + BoxGap) + 12;
        var tagInk = Frame with { A = 225 };
        Tag(gridX, tagY, $"{current.Count} / {PCScreen.BoxSize}", tagInk, 44);
        string paper = PcBoxes.WallpaperNames[Math.Clamp(current.Wallpaper, 0, PcBoxes.WallpaperNames.Count - 1)].ToUpperInvariant();
        float paperW = UiFonts.Measure(paper, 44 * 0.58f, UiWeight.Black) + 44 * 0.9f;
        float gridRight = gridX + BoxCell * PCScreen.Columns + BoxGap * (PCScreen.Columns - 1);
        Tag(gridRight - paperW, tagY, paper, tagInk, 44);

        // ---- The Pokémon under the cursor, or the one carried
        var detail = new Rectangle(box.X + box.Width + Gutter + slide, ContentTop, sw - Margin - (box.X + box.Width + Gutter + slide), height);
        Panel(detail, 34);
        if ((pc.Held ?? under) is { } shown) StoredPokemon(detail, shown);
        else
        {
            string note = onName ? "Left and right change box." : "An empty place.";
            UiFonts.DrawCentered(note, detail.X + (detail.Width - UiFonts.Measure(note, 26, UiWeight.ExtraBold)) / 2f, detail.Y + detail.Height / 2f, 26, Muted, UiWeight.ExtraBold);
        }

        // ---- Where the cursor is, for whatever goes with it
        Rectangle cursor = pc.Zone switch
        {
            StorageZone.Party => PartyCard(Math.Clamp(pc.PartyIndex, 0, Party.MaxSize - 1)),
            StorageZone.Box => CellRect(pc.Cell),
            _ => plate
        };

        // The Pokémon carried, lifted over the place under the cursor
        if (pc.Held is { } held)
        {
            var spot = pc.Zone switch
            {
                StorageZone.Party => new Vector2(cursor.X + 70, cursor.Y + cursor.Height / 2f),
                StorageZone.Box => new Vector2(cursor.X + cursor.Width / 2f, cursor.Y + cursor.Height / 2f),
                _ => new Vector2(plate.X + plate.Width / 2f, plate.Y + plate.Height + 40)
            };
            UiShapes.Glow(spot + new Vector2(0, 30), 38, 11, new Color(10, 16, 40, 110));
            PixelArt(PixelArtGenerator.IconOf(held), spot - new Vector2(0, 46), 2, Hop(false) * 2);
        }

        // ---- The menus, beside the cursor on the side with more room
        float Beside(float width) => pc.Zone == StorageZone.Party || (pc.Zone == StorageZone.Box && pc.Cell % PCScreen.Columns < PCScreen.Columns / 2)
            ? cursor.X + cursor.Width + 16
            : cursor.X - 16 - width;
        float Within(float y, float h) => Math.Clamp(y, ContentTop, ContentBottom - h);

        if (pc.Menu is { } menu)
        {
            var rows = new List<string>();
            foreach (var action in menu) rows.Add(PcActionLabel(action));
            MenuPanel(Beside(340), Within(cursor.Y - 8, MenuPanelHeight(rows.Count)), rows, pc.MenuIndex);
        }
        else if (pc.BoxMenu is { } boxMenu)
        {
            var rows = new List<string>();
            foreach (var action in boxMenu) rows.Add(action switch { PcBoxAction.Name => "NAME", PcBoxAction.Wallpaper => "WALLPAPER", _ => "CANCEL" });
            MenuPanel(plate.X + plate.Width / 2f - 170, plate.Y + plate.Height + 16, rows, pc.MenuIndex);
        }
        else if (pc.Marking is { } marks)
        {
            float width = MarksPanelWidth;
            MarksPanel(new Vector2(Beside(width), Within(cursor.Y - 8, MarksPanelHeight)), marks, pc.MarkIndex);
        }

        if (pc.AskingRelease && under != null)
            Prompt(sw, sh, $"Release {under.DisplayName}?", "Once it goes back to the wild, it won't come back to you.",
                new[] { ("RELEASE  (Z)", Red), ("KEEP IT  (ESC)", Blue) }, -1);
    }

    private static string PcActionLabel(PcAction action) => action switch
    {
        PcAction.Move => "MOVE",
        PcAction.Withdraw => "WITHDRAW",
        PcAction.Store => "STORE",
        PcAction.Mark => "MARK",
        PcAction.Release => "RELEASE",
        _ => "CANCEL"
    };

    /// <summary>
    /// A stored Pokémon told in the detail column: its sprite on a disc, its marks, name, level, types and condition,
    /// HP and nature, moves, where and when it was met, and what it holds.
    /// </summary>
    private static void StoredPokemon(Rectangle detail, Pokemon shown)
    {
        var disc = new Vector2(detail.X + detail.Width / 2f, detail.Y + 24 + 112);
        BallDisc(disc, 112);
        PixelArt(PixelArtGenerator.SpriteOf(shown), disc, 2);

        // The six marks, the ones set in Ink
        const float markStep = 42;
        for (int m = 0; m < MarkNames.Length; m++)
        {
            bool set = (shown.Marks & (1 << m)) != 0;
            MarkShape(new Vector2(detail.X + detail.Width / 2f + (m - 2.5f) * markStep, detail.Y + 268), 24, m, set ? Ink : Rule);
        }

        float x = detail.X + 36, y = detail.Y;
        float wide = detail.Width - 72;
        NameWithGender(shown, x, y + 318, 38);
        // An Egg tells only how near it is to hatching (plan 06 · R15)
        if (shown.IsEgg)
        {
            var said = Wrap(Breeding.Watch(shown), wide, 26, UiWeight.ExtraBold);
            for (int i = 0; i < said.Count && i < 4; i++) UiFonts.Draw(said[i], x, y + 352 + i * 34, 26, Ink, UiWeight.ExtraBold);
            return;
        }
        Level(detail.X + detail.Width - 36, y + 318, shown.Level, 32);
        float tx = TypePills(x, y + 344, shown, 34);
        StatusPill(tx + 2, y + 344, shown.IsFainted ? StatusCondition.Faint : shown.Status, 34);
        HpBar(x, y + 394, wide, 22, (float)shown.CurrentHP / Math.Max(1, shown.MaxHP));
        string hp = $"{shown.CurrentHP} / {shown.MaxHP}";
        UiFonts.DrawCentered(hp, detail.X + detail.Width - 36 - UiFonts.Measure(hp, 26, UiWeight.Black), y + 442, 26, Ink, UiWeight.Black);
        UiFonts.DrawCentered(shown.Nature.ToString(), x, y + 442, 26, Muted, UiWeight.ExtraBold);

        UiShapes.Fill(new Rectangle(x, y + 470, wide, 3), 1.5f, Rule);
        Label("MOVES", x, y + 486);
        for (int i = 0; i < shown.Moves.Count && i < 4; i++)
        {
            float my = y + 530 + i * 40;
            UiShapes.Circle(new Vector2(x + 10, my), 9, Palette.GetTypeColor(shown.Moves[i].Type.ToString()));
            UiFonts.DrawCentered(shown.Moves[i].Name, x + 32, my, 26, Ink, UiWeight.ExtraBold);
        }

        if (shown.MetLocation != null || shown.HeldItem != null) UiShapes.Fill(new Rectangle(x, y + 680, wide, 3), 1.5f, Rule);
        if (shown.MetLocation is { } place)
        {
            Label(shown.MetDate is { } day ? $"MET  {day.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant()}" : "MET", x, y + 696);
            var lines = Wrap($"Met {PlaceWords.In(place)} at Lv. {shown.MetLevel}.", wide, 26, UiWeight.ExtraBold);
            for (int i = 0; i < lines.Count && i < 2; i++) UiFonts.Draw(lines[i], x, y + 722 + i * 34, 26, Ink, UiWeight.ExtraBold);
        }
        if (shown.HeldItem is { } item)
        {
            Label("HOLDING", x, y + 806);
            UiFonts.Draw(item.Name, x, y + 830, 28, Ink, UiWeight.ExtraBold);
        }
    }

    private const float MarkKey = 72, MarkGap = 12, MarksPanelWidth = 24 * 2 + MarkKey * 6 + MarkGap * 5, MarksPanelHeight = 24 + 40 + MarkKey + 16 + 62 + 22;

    /// <summary>The marks being chosen: the six on keys, those set tinted, the one under the cursor ringed, then OK.</summary>
    private static void MarksPanel(Vector2 at, Markings marks, int cursor)
    {
        var panel = new Rectangle(at.X, at.Y, MarksPanelWidth, MarksPanelHeight);
        Panel(panel, 30);
        UiFonts.DrawCentered("MARKS", panel.X + 28, panel.Y + 40, 26, Ink, UiWeight.Black);
        int count = 0;
        for (int m = 0; m < MarkNames.Length; m++) if (((int)marks & (1 << m)) != 0) count++;
        string set = count == 0 ? "None set" : $"{count} set";
        UiFonts.DrawCentered(set, panel.X + panel.Width - 28 - UiFonts.Measure(set, 22, UiWeight.ExtraBold), panel.Y + 40, 22, Muted, UiWeight.ExtraBold);

        for (int m = 0; m < MarkNames.Length; m++)
        {
            var key = new Rectangle(panel.X + 24 + m * (MarkKey + MarkGap), panel.Y + 64, MarkKey, MarkKey);
            var ring = new Rectangle(key.X - 6, key.Y - 6, key.Width + 12, key.Height + 12);
            if (cursor == m) UiShapes.Shadow(ring, 26, 14, Vector2.Zero, Selection with { A = 110 });
            bool on = ((int)marks & (1 << m)) != 0;
            if (on) UiShapes.Shape(key, 20, Lighter(Selection, 0.74f), Lighter(Selection, 0.62f), Lighter(Selection, 0.3f), 3);
            else UiShapes.Shape(key, 20, SlotTop, SlotBottom, Rule, 3);
            MarkShape(new Vector2(key.X + MarkKey / 2f, key.Y + MarkKey / 2f), 36, m, on ? Ink : new Color(186, 194, 212, 255));
            if (cursor == m) UiShapes.Shape(ring, 26, Selection with { A = 0 }, Selection with { A = 0 }, Selection, 5);
        }

        var ok = new Rectangle(panel.X + 14, panel.Y + 64 + MarkKey + 16, panel.Width - 28, 62);
        float okW = UiFonts.Measure("OK", 32, UiWeight.Black);
        ListRow(ok, cursor >= MarkNames.Length, "OK", nameX: (ok.Width - okW) / 2f, size: 32);
    }

    /// <summary>The keyboard for a box's name, with the box's wallpaper and name on the left where a portrait would be.</summary>
    private static void DrawBoxNaming(int sw, int sh, NameEntry entry, string boxName, int wallpaper)
    {
        Backdrop(sw, sh);
        ScreenTitle("BOX NAME");
        Hints(sw - Margin, 44, ("Z", "Pick"), ("X", "Delete"), ("Enter", "To OK"));

        var who = new Rectangle(Margin, ContentTop, 500, ContentBottom - ContentTop);
        Panel(who, 34);
        var plate = new Rectangle(who.X + 50, who.Y + 50, who.Width - 100, 470);
        UiShapes.Shape(plate, 30, Disc, Disc, Rule, 4);
        float paperH = plate.Height - 20, paperW = paperH * BoxWallpapers.Width / BoxWallpapers.Height;
        Wallpaper(new Rectangle(plate.X + (plate.Width - paperW) / 2f, plate.Y + 10, paperW, paperH), wallpaper);
        string shown = boxName.ToUpperInvariant();
        var tag = new Rectangle(plate.X + 40, plate.Y + 34, plate.Width - 80, 64);
        UiShapes.Shadow(tag, 32, 12, new Vector2(0, 4), ShadowColor);
        UiShapes.Shape(tag, 32, SlotTop, SlotBottom, Rule, 3);
        UiFonts.DrawCentered(shown, tag.X + (tag.Width - UiFonts.Measure(shown, 34, UiWeight.Black)) / 2f, tag.Y + tag.Height / 2f, 34, Ink, UiWeight.Black);
        DrawWrapped($"Eight letters at most. Leave it empty to keep the name {boxName}.", who.X + 50, plate.Y + plate.Height + 36, who.Width - 100,
            28, Muted, 40, UiWeight.ExtraBold);

        NameBoard(new Rectangle(who.X + who.Width + Gutter, ContentTop, sw - Margin - (who.X + who.Width + Gutter), ContentBottom - ContentTop), entry);
    }

    // ------------------------------------------------------------------ wallpapers

    private static readonly Dictionary<int, Texture2D> wallpapers = new();

    /// <summary>A box's wallpaper over a rectangle (painted the first time it is shown, then kept), smoothed.</summary>
    public static void Wallpaper(Rectangle r, int index)
    {
        index = Math.Clamp(index, 0, PcBoxes.WallpaperNames.Count - 1);
        if (!wallpapers.TryGetValue(index, out var texture))
        {
            texture = BoxWallpapers.Paint(index).ToTexture();
            Raylib.SetTextureFilter(texture, TextureFilter.Bilinear);
            wallpapers[index] = texture;
        }
        Raylib.DrawTexturePro(texture, new Rectangle(0, 0, texture.Width, texture.Height), r, Vector2.Zero, 0f, Color.White);
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
