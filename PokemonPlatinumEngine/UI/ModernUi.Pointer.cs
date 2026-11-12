using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The pointer's arrow, and where the things it can be used on stand (plan 12 · Q6, Q7; style guide, "Shapes and
/// components", "The pointer"). Each rectangle here is the one its screen draws with, so a hit test and the drawing
/// never part; all of it but <see cref="PointerArrow"/> is GPU-free.
/// </summary>
internal static partial class ModernUi
{
    // ------------------------------------------------------------------ the arrow

    /// <summary>The arrow's height from its tip to its tail, in layout units.</summary>
    public const float PointerHeight = 34f;

    /// <summary>
    /// The pointer's arrow, its tip at <paramref name="tip"/>: white, rimmed in Ink, a short tail down and to the right,
    /// on a soft shadow; drawn over everything while the mouse is in use.
    /// </summary>
    public static void PointerArrow(Vector2 tip)
    {
        Vector2 P(float x, float y) => tip + new Vector2(x, y);
        // The shadow, a little down and to the right
        UiShapes.Triangle(P(3, 4), P(3, 34), P(27, 26), new Color(6, 14, 34, 70), 3f);
        // The tail, then the head over it, each rimmed in Ink
        UiShapes.Line(P(11, 22), P(18, 36), 9f, Ink);
        UiShapes.Triangle(P(0, 0), P(0, 30), P(24, 22), Ink, 2.5f);
        UiShapes.Line(P(11, 22), P(18, 36), 4.5f, Color.White);
        UiShapes.Triangle(P(0, 0), P(0, 30), P(24, 22), Color.White);
    }

    // ------------------------------------------------------------------ shared layouts

    /// <summary>The buttons of <see cref="Prompt"/> once it has settled, in their order.</summary>
    public static Rectangle[] PromptButtons(int sw, int sh, int count, float appear = 1f)
    {
        float width = count > 2 ? 1320 : 1040;
        var r = new Rectangle(sw / 2f - width / 2f, sh / 2f - 170 + (1f - appear) * 60f, width, 340);
        const float gap = 32;
        float bw = (r.Width - 112 - gap * (count - 1)) / count;
        var rects = new Rectangle[count];
        for (int i = 0; i < count; i++) rects[i] = new Rectangle(r.X + 56 + i * (bw + gap), r.Y + 216, bw, 84);
        return rects;
    }

    /// <summary>
    /// The rows of the field's answers (<see cref="DrawChoices"/>), settled, as (index of the answer, its rectangle):
    /// only those the window shows.
    /// </summary>
    public static List<(int Index, Rectangle Rect)> ChoiceRows(int sw, int sh, IReadOnlyList<string> options, int selected, float shown = 1f)
    {
        const float row = 72, pad = 16, size = Kit.DialogueBox.ChoiceSize;
        float widest = 0f;
        // Measured as the game measures with no window (TextMetrics reckons as UiFonts does), so a test can point too
        foreach (string option in options) widest = Math.Max(widest, TextMetrics.Measure(option, size, Kit.DialogueBox.ChoiceWeight));
        float width = Math.Max(Kit.DialogueBox.ChoiceWidth, widest + Kit.DialogueBox.ChoicePadding);
        int rows = Math.Min(options.Count, MostChoiceRows), first = Math.Clamp(selected - MostChoiceRows / 2, 0, Math.Max(0, options.Count - rows));
        float height = pad * 2 + rows * row;
        var panel = new Rectangle(sw - 96 - width + (1f - shown) * (width + 140), sh - Kit.DialogueBox.Foot - Kit.DialogueBox.HeightNow - 22 - height, width, height);
        var list = new List<(int, Rectangle)>(rows);
        for (int i = first; i < first + rows; i++)
            list.Add((i, new Rectangle(panel.X + 14, panel.Y + pad + (i - first) * row + 4, width - 28, row - 8)));
        return list;
    }

    /// <summary>The start menu's rows once it has slid in (<see cref="DrawStartMenu"/>), in the entries' order.</summary>
    /// <summary>
    /// A start menu row's height: 74 up to ten entries, and less beyond, so the panel never runs off the screen (plan 35 ·
    /// BN2: CAMP by TEAM CARE beside the Pokégear or the PokéNav and REQUESTS).
    /// </summary>
    public static float StartMenuRowHeight(int count) => count <= 10 ? 74f : MathF.Max(56f, MathF.Floor(740f / count));

    public static Rectangle[] StartMenuRows(int sw, int count, float shown = 1f)
    {
        const float width = 440;
        float row = StartMenuRowHeight(count);
        var panel = new Rectangle(sw - 40 - width + (1f - shown) * (width + 80), 40, width, 40 + count * row + 18);
        var rects = new Rectangle[count];
        for (int i = 0; i < count; i++)
        {
            float y = panel.Y + 20 + i * row + (i == count - 1 ? 18 : 0);
            rects[i] = new Rectangle(panel.X + 16, y, width - 32, row - 8);
        }
        return rects;
    }

    /// <summary>Where each tab of <see cref="Tabs"/> stands across its strip.</summary>
    public static Rectangle[] TabRects(Rectangle strip, int count)
    {
        const float gap = 12;
        float w = (strip.Width - gap * (count - 1)) / count;
        var rects = new Rectangle[count];
        for (int i = 0; i < count; i++) rects[i] = new Rectangle(strip.X + i * (w + gap), strip.Y, w, strip.Height);
        return rects;
    }

    /// <summary>The visible rows of a list panel (<see cref="RowRect"/>), as (index in the list, rectangle).</summary>
    public static List<(int Index, Rectangle Rect)> ListRows(Rectangle panel, int first, int visible, int count, float top = 24, bool scrolls = false)
    {
        var rows = new List<(int, Rectangle)>();
        for (int row = 0; row < visible && first + row < count; row++) rows.Add((first + row, RowRect(panel, row, top, scrolls)));
        return rows;
    }

    /// <summary>Which of the listed rows a point lies on: its index in the list, or null.</summary>
    public static int? HitRow(IReadOnlyList<(int Index, Rectangle Rect)> rows, Vector2 p)
    {
        foreach (var (index, rect) in rows)
            if (UiPointer.Inside(rect, p)) return index;
        return null;
    }

    /// <summary>A card of the team in the party screen (<see cref="DrawParty"/>, <see cref="DrawPartyChoice"/>) once it has risen into place, two to a row.</summary>
    public static Rectangle PartyCardRect(int index) => new(64 + (index % 2) * 912, 132 + (index / 2) * 256, 880, 232);

    /// <summary>The rows of a Pokémon's menu in the party screen (<see cref="PartyActions"/>), beside the card at <paramref name="selected"/>.</summary>
    public static List<(int Index, Rectangle Rect)> PartyActionRows(int sw, int selected, int count)
    {
        const float width = 400, row = 70;
        float height = 20 + count * row + 12;
        var card = PartyCardRect(selected);
        bool onTheLeft = card.X > sw / 2f;
        var panel = new Rectangle(onTheLeft ? 64 + 880 - width : sw - 64 - width, 920 - 22 - height, width, height);
        var rows = new List<(int, Rectangle)>(count);
        for (int i = 0; i < count; i++) rows.Add((i, new Rectangle(panel.X + 14, panel.Y + 16 + i * row, width - 28, row - 8)));
        return rows;
    }

    /// <summary>The bag's strip of pocket tabs (<see cref="DrawBag"/>).</summary>
    public static Rectangle BagTabStrip(int sw) => new(Margin, ContentTop, sw - Margin * 2, 76);

    /// <summary>The bag's list of the pocket's items once it has slid in.</summary>
    public static Rectangle BagList() => new(Margin, ContentTop + 76 + 24, 920, ContentBottom - (ContentTop + 76 + 24));

    /// <summary>The rows of the item's menu in the bag (<see cref="ActionMenu"/>), at the foot of the detail panel.</summary>
    public static List<(int Index, Rectangle Rect)> BagActionRows(int sw, int count)
    {
        const float top = ContentTop + 76 + 24, width = 360, row = 74;
        var detail = new Rectangle(Margin + 920 + Gutter, top, sw - Margin * 2 - 920 - Gutter, ContentBottom - top);
        float height = 20 + count * row + 12;
        var panel = new Rectangle(detail.X + detail.Width - width - 28, detail.Y + detail.Height - height - 28, width, height);
        var rows = new List<(int, Rectangle)>(count);
        for (int i = 0; i < count; i++) rows.Add((i, new Rectangle(panel.X + 14, panel.Y + 16 + i * row, width - 28, row - 8)));
        return rows;
    }

    /// <summary>A shop's list of its stock (or of the bag's items while selling) once it has slid in.</summary>
    public static Rectangle ShopList() => new(Margin, ContentTop, 920, ContentBottom - ContentTop);

    /// <summary>BUY, SELL and SEE YA! (<see cref="ShopQuestion"/>).</summary>
    public static Rectangle[] ShopQuestionButtons(int sw, int sh)
    {
        var r = new Rectangle(sw / 2f - 300, sh / 2f - 230, 600, 460);
        var rects = new Rectangle[ShopScreen.Choices.Length];
        for (int i = 0; i < rects.Length; i++) rects[i] = new Rectangle(r.X + 48, r.Y + 120 + i * 104, r.Width - 96, 84);
        return rects;
    }

    /// <summary>A card of the team on the PC's left (<see cref="DrawStorage"/>), once it has slid in.</summary>
    public static Rectangle StorageTeamCard(int index) => new(Margin + 20, ContentTop + 66 + index * 136, 500 - 40, 124);

    /// <summary>The PC's box panel, once it has slid in.</summary>
    public static Rectangle StorageBox() => new(Margin + 500 + Gutter, ContentTop, 792, ContentBottom - ContentTop);

    /// <summary>The plate with the box's name over its places.</summary>
    public static Rectangle StoragePlate()
    {
        var box = StorageBox();
        return new Rectangle(box.X + box.Width / 2f - 190, box.Y + 32, 380, 76);
    }

    /// <summary>The round arrow beside the box's name, to the box before (-1) or after (+1), as a square round its disc.</summary>
    public static Rectangle StorageArrow(int side)
    {
        var plate = StoragePlate();
        float x = side < 0 ? plate.X - 46 : plate.X + plate.Width + 46, y = plate.Y + plate.Height / 2f;
        return new Rectangle(x - 28, y - 28, 56, 56);
    }

    /// <summary>A place of the box, <see cref="PCScreen.Columns"/> to a row.</summary>
    public static Rectangle StorageCell(int index)
    {
        var box = StorageBox();
        float gridX = box.X + (box.Width - (BoxCell * PCScreen.Columns + BoxGap * (PCScreen.Columns - 1))) / 2f, gridY = box.Y + 164;
        return new Rectangle(gridX + index % PCScreen.Columns * (BoxCell + BoxGap), gridY + index / PCScreen.Columns * (BoxCell + BoxGap), BoxCell, BoxCell);
    }

    /// <summary>The name board of <see cref="DrawNameEntry"/> once it has slid in.</summary>
    public static Rectangle NameBoardRect(int sw)
    {
        float x = Margin + 500 + Gutter;
        return new Rectangle(x, ContentTop, sw - Margin - x, ContentBottom - ContentTop);
    }

    /// <summary>
    /// The keys of the keyboard on a name board (<see cref="NameBoard"/>, <see cref="Keyboard"/>), each as its row times
    /// 16 plus its column (a wide key's first column), with its rectangle.
    /// </summary>
    public static List<(int Index, Rectangle Rect)> KeyboardKeys(Rectangle board, NameEntry entry)
    {
        float keysWidth = NameEntry.Columns * KeyWidth + (NameEntry.Columns - 1) * KeyGap;
        var origin = new Vector2(board.X + (board.Width - keysWidth) / 2f, board.Y + 250);
        var keys = new List<(int, Rectangle)>();
        for (int row = 0; row < entry.RowCount; row++)
            for (int col = 0; col < entry.KeysIn(row); col++)
                keys.Add((row * 16 + col, new Rectangle(origin.X + col * (KeyWidth + KeyGap), origin.Y + row * (KeyHeight + KeyGap), KeyWidth, KeyHeight)));
        float y = origin.Y + entry.RowCount * (KeyHeight + KeyGap) + 14;
        for (int i = 0; i < NameEntry.WideKeys.Length; i++)
        {
            int start = NameEntry.WideStarts[i], end = i + 1 < NameEntry.WideStarts.Length ? NameEntry.WideStarts[i + 1] : NameEntry.Columns;
            keys.Add((entry.WideRow * 16 + start, new Rectangle(origin.X + start * (KeyWidth + KeyGap), y, (end - start) * KeyWidth + (end - start - 1) * KeyGap, KeyHeight)));
        }
        return keys;
    }

    /// <summary>A row of <see cref="MoveChoice"/> in its panel <paramref name="panel"/>: a move to forget (0 to 3), or (4) the row that keeps them all.</summary>
    public static Rectangle MoveChoiceRow(Rectangle panel, int index) => index < 4
        ? new Rectangle(panel.X + 28, panel.Y + 212 + index * 96, panel.Width - 56, 86)
        : new Rectangle(panel.X + 28, panel.Y + 212 + 4 * 96 + 14, panel.Width - 56, 78);

    /// <summary>Which row of a <see cref="MoveChoice"/> panel a point lies on, for a Pokémon with <paramref name="moves"/> moves.</summary>
    public static int? HitMoveChoice(Rectangle panel, int moves, Vector2 p)
    {
        for (int i = 0; i < Math.Min(4, moves); i++)
            if (UiPointer.Inside(MoveChoiceRow(panel, i), p)) return i;
        return UiPointer.Inside(MoveChoiceRow(panel, 4), p) ? 4 : null;
    }

    // ------------------------------------------------------------------ the battle's (plan 12 · Q7)

    /// <summary>FIGHT, tall on the left, and BAG, POKéMON and RUN stacked beside it (<see cref="DrawBattle"/>'s commands, 0 to 3).</summary>
    public static Rectangle CommandRect(int index) => index == 0
        ? new Rectangle(1140, 848, 432, 190)
        : new Rectangle(1600, 848 + (index - 1) * 66, 272, 58);

    /// <summary>A move card of the move menu, two to a row.</summary>
    public static Rectangle MoveCardRect(int index) => new(48 + (index % 2) * 556, 848 + (index / 2) * 100, 536, 88);

    /// <summary>A card of the team, three to a row, where the battle switches or picks a Pokémon for an item.</summary>
    public static Rectangle TeamCardRect(int index) => new(48 + (index % 3) * 616, 648 + (index / 3) * 196, 592, 180);

    /// <summary>A pocket of the battle's bag, two to a row, and under them (index 4) the item used last.</summary>
    public static Rectangle PocketButtonRect(int index) => index >= BattleBag.Pockets.Length
        ? new Rectangle(48, 912, 1824, 108)
        : new Rectangle(48 + (index % 2) * 920, 648 + (index / 2) * 132, 904, 116);

    /// <summary>A card of a pocket's items on its page, two to a row.</summary>
    public static Rectangle BagCardRect(int slot) => new(48 + (slot % 2) * 584, 648 + (slot / 2) * 96, 568, 86);
}
