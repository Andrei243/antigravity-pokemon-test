using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The crafting kit on screen (plan 39 · CR1; style guide, "Menu screens (G10)", "The kit").</summary>
internal static partial class ModernUi
{
    private static (string Label, UiIcon Icon, Color Color)[] CraftingTabs(CraftingScreen screen) => screen.Tabs.Select(tab => tab switch
    {
        CraftingTab.Machines => (Text.Of("crafting.tab.machines"), UiIcon.Disc, Blue),
        CraftingTab.Make => (Text.Of("crafting.tab.make"), UiIcon.Gear, Green),
        CraftingTab.Pantry => (Text.Of("crafting.tab.pantry"), UiIcon.Pouch, PantryGreen),
        _ => (Text.Of("crafting.tab.materials"), UiIcon.Pouch, Gold)
    }).ToArray();

    /// <summary>The colour a material is drawn in: its type's for a Pokémon's, wood for the field's, green for the pantry's.</summary>
    internal static Color MaterialColour(MaterialData material) => material.Kind switch
    {
        MaterialKind.Pokemon when material.Type is { } type => Palette.GetTypeColor(type.ToString()),
        MaterialKind.Pantry => PantryGreen,
        _ => new Color(176, 132, 84, 255)
    };

    private static readonly Color PantryGreen = new(110, 168, 88, 255);

    public static void DrawCrafting(int sw, int sh, CraftingScreen screen, float appear)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("crafting.title"));
        var keys = new System.Collections.Generic.List<HintItem>();
        if (screen.HowMany > 0)
        {
            keys.Add(Hint("shared.keys.arrows", "crafting.hint.howMany"));
            keys.Add(Hint("shared.keys.z", screen.Tab == CraftingTab.Pantry ? "crafting.hint.buy" : "crafting.hint.make"));
        }
        else
        {
            keys.Add(Hint("shared.keys.upDown", "shared.hints.move"));
            if (screen.Tabs.Count > 1) keys.Add(Hint("shared.keys.leftRight", "crafting.hint.tab"));
            if (screen.Tab is CraftingTab.Machines or CraftingTab.Make) keys.Add(Hint("shared.keys.z", "crafting.hint.make"));
            if (screen.Tab == CraftingTab.Pantry) keys.Add(Hint("shared.keys.z", "crafting.hint.buy"));
        }
        keys.Add(Hint("shared.keys.esc", "shared.hints.back"));
        Hints(sw - Margin, 44, keys.ToArray());
        Tabs(new Rectangle(Margin, ContentTop, sw - Margin * 2, 76), CraftingTabs(screen), screen.Tabs.ToList().IndexOf(screen.Tab));

        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        const float top = ContentTop + 76 + 24;
        const float listW = 860;
        var list = new Rectangle(Margin - slide, top, listW, ContentBottom - top);
        var detail = new Rectangle(Margin + listW + Gutter + slide, top, sw - Margin * 2 - listW - Gutter, ContentBottom - top);
        Panel(list, 34);
        Panel(detail, 34);
        if (screen.Tab == CraftingTab.Machines)
        {
            DrawMachines(screen, list, detail);
            return;
        }
        if (screen.Tab == CraftingTab.Make)
        {
            DrawRecipes(screen, list, detail);
            return;
        }
        if (screen.Tab == CraftingTab.Pantry)
        {
            DrawPantry(screen, list, detail);
            return;
        }

        // The materials, a row each: its sign on a disc in its colour, its name, and how many the pouch holds
        var entries = screen.Entries;
        bool scrolls = entries.Count > CraftingScreen.VisibleRows;
        for (int row = 0; row < CraftingScreen.VisibleRows && screen.First + row < entries.Count; row++)
        {
            int index = screen.First + row;
            var material = entries[index];
            var r = RowRect(list, row, 24, scrolls);
            bool selected = index == screen.Cursor;
            int count = screen.Pouch.Count(material.Id);
            var ink = ListRow(r, selected, MaterialTable.NameOf(material.Id), enabled: count > 0, nameX: 112, size: 32);
            MaterialSign(new Vector2(r.X + 56, r.Y + r.Height / 2f), 28, material, count > 0);
            RowValue(r, $"×{count}", ink);
        }
        ScrollBar(list, screen.First, CraftingScreen.VisibleRows, entries.Count, 24);

        // The one under the cursor: large, what it is and where it comes from
        if (screen.Selected is not { } chosen) return;
        float x = detail.X + 44, room = detail.Width - 88;
        int held = screen.Pouch.Count(chosen.Id);
        Label(MaterialTable.KindName(chosen.Kind), x, detail.Y + 30);
        string kinds = Text.Of("crafting.kinds", ("n", screen.Pouch.Kinds));
        Tag(detail.X + detail.Width - 44 - UiFonts.Measure(kinds, 24, UiWeight.Black) - 34, detail.Y + 26, kinds, Frame, 40);
        var disc = new Vector2(detail.X + detail.Width / 2f, detail.Y + 210);
        UiShapes.Circle(disc, 120, Disc);
        UiShapes.Ring(disc, 120, 5, Rule);
        MaterialSign(disc, 78, chosen, held > 0);
        string name = MaterialTable.NameOf(chosen.Id);
        float size = 44, nameW = UiFonts.Measure(name, size, UiWeight.Black);
        if (nameW > room) size = MathF.Floor(size * room / nameW);
        UiFonts.DrawCentered(name, detail.X + (detail.Width - Math.Min(nameW, room)) / 2f, detail.Y + 380, size, Ink, UiWeight.Black);
        string has = held > 0 ? Text.Of("crafting.has", ("count", held), ("limit", MaterialTable.Limit)) : Text.Of("crafting.none");
        UiFonts.DrawCentered(has, detail.X + (detail.Width - UiFonts.Measure(has, 28, UiWeight.ExtraBold)) / 2f, detail.Y + 432, 28, Muted, UiWeight.ExtraBold);
        string worth = Text.Of("crafting.worth", ("money", chosen.Worth));
        UiFonts.DrawCentered(worth, detail.X + (detail.Width - UiFonts.Measure(worth, 26, UiWeight.ExtraBold)) / 2f, detail.Y + 476, 26, Muted, UiWeight.ExtraBold);
        UiShapes.Fill(new Rectangle(x, detail.Y + 520, room, 3), 1.5f, Rule);
        DrawWrapped(MaterialTable.DescriptionOf(chosen.Id), x, detail.Y + 548, room, 28, Ink, 36);
        DrawWrapped(MaterialSource(chosen), x, detail.Y + 548 + 36 * 3 + 12, room, 26, Muted, 34, UiWeight.ExtraBold);
    }

    /// <summary>
    /// The TM maker's tab (plan 39 · CR4; style guide, "The kit"): a machine to a row in its move's type colour with how
    /// many are in the bag, dimmed when it can't be made now; for the one under the cursor, the move, and the cost against
    /// what is held, each need in green when held and red when short, and the last press's words under them.
    /// </summary>
    private static void DrawMachines(CraftingScreen screen, Rectangle list, Rectangle detail)
    {
        var machines = screen.Machines;
        if (machines.Count == 0)
        {
            DrawWrapped(Text.Of("crafting.tm.empty"), list.X + 44, list.Y + 60, list.Width - 88, 32, Muted, 42);
        }
        bool scrolls = machines.Count > CraftingScreen.VisibleRows;
        for (int row = 0; row < CraftingScreen.VisibleRows && screen.MachineFirst + row < machines.Count; row++)
        {
            int index = screen.MachineFirst + row;
            var machine = machines[index];
            var r = RowRect(list, row, 24, scrolls);
            bool selected = index == screen.MachineCursor;
            bool can = TmMaker.WhyNot(machine, screen.Bag, screen.Pouch, screen.Money) == null;
            string label = $"{machine.DisplayName}  {LocalNames.Move(machine.TeachesMove!)}";
            var ink = ListRow(r, selected, label, enabled: can, nameX: 112, size: 32);
            var colour = Palette.GetTypeColor(MoveDatabase.Get(machine.TeachesMove!).Type.ToString());
            var at = new Vector2(r.X + 56, r.Y + r.Height / 2f);
            UiShapes.Circle(at, 26, can ? colour : Lighter(colour, 0.55f));
            UiShapes.Ring(at, 26, 3, Darker(colour, 0.25f));
            RowValue(r, $"×{screen.Bag.GetQuantity(machine)}", ink);
        }
        ScrollBar(list, screen.MachineFirst, CraftingScreen.VisibleRows, machines.Count, 24);

        // The money in hand, over the detail panel's corner
        float x = detail.X + 44, room = detail.Width - 88;
        Label(Text.Of("crafting.tm.money"), x, detail.Y + 30);
        MoneyRight(detail.X + detail.Width - 44, detail.Y + 40, screen.Money, 30, Ink);
        if (screen.SelectedMachine is not { } chosen || screen.SelectedCost is not { } cost) return;
        var move = MoveDatabase.Get(chosen.TeachesMove!);
        var typeColour = Palette.GetTypeColor(cost.Type.ToString());
        var disc = new Vector2(detail.X + detail.Width / 2f, detail.Y + 180);
        UiShapes.Circle(disc, 100, typeColour);
        UiShapes.Ring(disc, 100, 5, Darker(typeColour, 0.3f));
        UiFonts.DrawCentered(chosen.DisplayName, disc.X - UiFonts.Measure(chosen.DisplayName, 40, UiWeight.Black) / 2f, disc.Y, 40, Color.White, UiWeight.Black);
        string name = LocalNames.Move(chosen.TeachesMove!);
        float size = 44, nameW = UiFonts.Measure(name, size, UiWeight.Black);
        if (nameW > room) size = MathF.Floor(size * room / nameW);
        UiFonts.DrawCentered(name, detail.X + (detail.Width - Math.Min(nameW, room)) / 2f, detail.Y + 330, size, Ink, UiWeight.Black);
        float pillW = UiFonts.Measure(Words.Type(cost.Type).ToUpperInvariant(), 34 * 0.6f, UiWeight.Black) + 34 * 1.05f + (Seeing.Signs ? 34 * 0.8f : 0f);
        string power = move.Category == MoveCategory.Status ? Text.Of("crafting.tm.noPower")
            : move.Power <= 0 ? Text.Of("crafting.tm.varies")
            : Text.Of("crafting.tm.power", ("n", move.Power));
        float powerW = UiFonts.Measure(power, 26, UiWeight.ExtraBold);
        float startX = detail.X + (detail.Width - pillW - 18 - powerW) / 2f;
        TypePill(startX, detail.Y + 360, cost.Type, 34, pillW);
        UiFonts.DrawCentered(power, startX + pillW + 18, detail.Y + 377, 26, Muted, UiWeight.ExtraBold);
        UiShapes.Fill(new Rectangle(x, detail.Y + 420, room, 3), 1.5f, Rule);
        Label(Text.Of("crafting.tm.needs"), x, detail.Y + 440);

        // What it takes, a line each: the material's sign, its name and held against needed
        float y = detail.Y + 490;
        void Need(string materialId, int need)
        {
            var material = MaterialTable.Get(materialId)!;
            int have = screen.Pouch.Count(materialId);
            var ink = have >= need ? Green : Red;
            MaterialSign(new Vector2(x + 24, y), 26, material, have > 0);
            UiFonts.DrawCentered(MaterialTable.NameOf(materialId), x + 64, y, 30, Ink, UiWeight.ExtraBold);
            string held = $"{have} / {need}";
            UiFonts.DrawCentered(held, x + room - UiFonts.Measure(held, 30, UiWeight.Black), y, 30, ink, UiWeight.Black);
            y += 56;
        }
        Need(cost.CommonId, cost.Common);
        if (cost.RareId != null) Need(cost.RareId, cost.Rare);
        bool enough = screen.Money >= cost.Money;
        UiFonts.DrawCentered(Text.Of("crafting.tm.price"), x + 64, y, 30, Ink, UiWeight.ExtraBold);
        MoneyRight(x + room, y, cost.Money, 30, enough ? Green : Red);
        y += 70;
        if (screen.Notice.Length > 0)
            DrawWrapped(screen.Notice, x, y, room, 28, screen.LastMade ? Green : Red, 36, UiWeight.ExtraBold);
    }

    /// <summary>The colour of a thing the kit makes: a ball's Red, medicine's Green, anything else Gold.</summary>
    internal static Color RecipeColour(CraftRecipe recipe) => Crafting.ItemOf(recipe)?.Pocket switch
    {
        ItemPocket.PokeBalls => Red,
        ItemPocket.Medicine => Green,
        _ => Gold
    };

    /// <summary>
    /// The kit's MAKE tab (plan 39 · CR3; style guide, "The kit's MAKE tab"): a recipe to a row in the colour of what it
    /// makes, dimmed when it can't be made now; for the one under the cursor what it makes and each need against what is
    /// held, the "how many?" stepper while it is asked, and the last press's words.
    /// </summary>
    private static void DrawRecipes(CraftingScreen screen, Rectangle list, Rectangle detail)
    {
        var recipes = screen.Recipes;
        if (recipes.Count == 0) DrawWrapped(Text.Of("crafting.make.empty"), list.X + 44, list.Y + 60, list.Width - 88, 32, Muted, 42);
        bool scrolls = recipes.Count > CraftingScreen.VisibleRows;
        for (int row = 0; row < CraftingScreen.VisibleRows && screen.RecipeFirst + row < recipes.Count; row++)
        {
            int index = screen.RecipeFirst + row;
            var recipe = recipes[index];
            var item = Crafting.ItemOf(recipe);
            var r = RowRect(list, row, 24, scrolls);
            bool can = Crafting.WhyNot(recipe, 1, screen.Bag, screen.Pouch) == null;
            var ink = ListRow(r, index == screen.RecipeCursor, item?.DisplayName ?? recipe.Makes, enabled: can, nameX: 112, size: 32);
            var colour = RecipeColour(recipe);
            var at = new Vector2(r.X + 56, r.Y + r.Height / 2f);
            UiShapes.Circle(at, 26, can ? colour : Lighter(colour, 0.55f));
            UiShapes.Ring(at, 26, 3, Darker(colour, 0.25f));
            if (item != null) RowValue(r, $"×{screen.Bag.GetQuantity(item)}", ink);
        }
        ScrollBar(list, screen.RecipeFirst, CraftingScreen.VisibleRows, recipes.Count, 24);

        if (screen.SelectedRecipe is not { } chosen || Crafting.ItemOf(chosen) is not { } made) return;
        float x = detail.X + 44, room = detail.Width - 88;
        Label(Text.Of("crafting.make.makes"), x, detail.Y + 30);
        var tint = RecipeColour(chosen);
        var disc = new Vector2(detail.X + detail.Width / 2f, detail.Y + 160);
        UiShapes.Circle(disc, 100, tint);
        UiShapes.Ring(disc, 100, 5, Darker(tint, 0.3f));
        string each = Text.Of("crafting.make.each", ("n", Math.Max(1, chosen.Count)));
        // Fitted inside the disc: at 40 the words ran to its rim
        float eachSize = 40, eachW = UiFonts.Measure(each, eachSize, UiWeight.Black);
        if (eachW > 150) { eachSize = MathF.Floor(eachSize * 150 / eachW); eachW = UiFonts.Measure(each, eachSize, UiWeight.Black); }
        UiFonts.DrawCentered(each, disc.X - eachW / 2f, disc.Y, eachSize, Color.White, UiWeight.Black);
        string name = made.DisplayName;
        float size = 44, nameW = UiFonts.Measure(name, size, UiWeight.Black);
        if (nameW > room) size = MathF.Floor(size * room / nameW);
        UiFonts.DrawCentered(name, detail.X + (detail.Width - Math.Min(nameW, room)) / 2f, detail.Y + 300, size, Ink, UiWeight.Black);
        UiShapes.Fill(new Rectangle(x, detail.Y + 340, room, 3), 1.5f, Rule);
        Label(Text.Of("crafting.tm.needs"), x, detail.Y + 360);

        int times = Math.Max(1, screen.HowMany);
        float y = detail.Y + 410;
        foreach (var need in chosen.Takes)
        {
            int have = Crafting.Held(need, screen.Bag, screen.Pouch), want = need.Count * times;
            if (need.Material != null && MaterialTable.Get(need.Material) is { } material) MaterialSign(new Vector2(x + 24, y), 26, material, have > 0);
            else UiShapes.Circle(new Vector2(x + 24, y), 21, Frame);
            UiFonts.DrawCentered(Crafting.NameOf(need), x + 64, y, 30, Ink, UiWeight.ExtraBold);
            string held = $"{have} / {want}";
            UiFonts.DrawCentered(held, x + room - UiFonts.Measure(held, 30, UiWeight.Black), y, 30, have >= want ? Green : Red, UiWeight.Black);
            y += 52;
        }
        DrawHowManyAndNotice(screen, detail, y + 10);
    }

    /// <summary>
    /// The kit's PANTRY tab (plan 39 · CR6; style guide, "The kit's PANTRY tab"): the pantry's goods with how many the pouch
    /// holds, and for the one under the cursor its price against the purse, the "how many?" stepper and the last press's words.
    /// </summary>
    private static void DrawPantry(CraftingScreen screen, Rectangle list, Rectangle detail)
    {
        var goods = screen.Pantry;
        bool scrolls = goods.Count > CraftingScreen.VisibleRows;
        for (int row = 0; row < CraftingScreen.VisibleRows && screen.PantryFirst + row < goods.Count; row++)
        {
            int index = screen.PantryFirst + row;
            var good = goods[index];
            var r = RowRect(list, row, 24, scrolls);
            var ink = ListRow(r, index == screen.PantryCursor, MaterialTable.NameOf(good.Id), nameX: 112, size: 32);
            MaterialSign(new Vector2(r.X + 56, r.Y + r.Height / 2f), 28, good, true);
            RowValue(r, $"×{screen.Pouch.Count(good.Id)}", ink);
        }
        ScrollBar(list, screen.PantryFirst, CraftingScreen.VisibleRows, goods.Count, 24);

        float x = detail.X + 44, room = detail.Width - 88;
        Label(Text.Of("crafting.tm.money"), x, detail.Y + 30);
        MoneyRight(detail.X + detail.Width - 44, detail.Y + 40, screen.Money, 30, Ink);
        if (screen.SelectedGood is not { } chosen) return;
        var disc = new Vector2(detail.X + detail.Width / 2f, detail.Y + 190);
        UiShapes.Circle(disc, 120, Disc);
        UiShapes.Ring(disc, 120, 5, Rule);
        MaterialSign(disc, 78, chosen, true);
        string name = MaterialTable.NameOf(chosen.Id);
        float size = 44, nameW = UiFonts.Measure(name, size, UiWeight.Black);
        if (nameW > room) size = MathF.Floor(size * room / nameW);
        UiFonts.DrawCentered(name, detail.X + (detail.Width - Math.Min(nameW, room)) / 2f, detail.Y + 350, size, Ink, UiWeight.Black);
        string price = Text.Of("crafting.pantry.price", ("money", chosen.Worth));
        UiFonts.DrawCentered(price, detail.X + (detail.Width - UiFonts.Measure(price, 26, UiWeight.ExtraBold)) / 2f, detail.Y + 400, 26, screen.Money >= chosen.Worth ? Muted : Red, UiWeight.ExtraBold);
        UiShapes.Fill(new Rectangle(x, detail.Y + 436, room, 3), 1.5f, Rule);
        DrawWrapped(MaterialTable.DescriptionOf(chosen.Id), x, detail.Y + 462, room, 28, Ink, 36);
        DrawHowManyAndNotice(screen, detail, detail.Y + 560);
    }

    // The "how many?" stepper while it is asked, and the words of the last press under it
    private static void DrawHowManyAndNotice(CraftingScreen screen, Rectangle detail, float y)
    {
        float x = detail.X + 44, room = detail.Width - 88;
        if (screen.HowMany > 0)
        {
            string ask = Text.Of("crafting.make.howMany");
            UiFonts.DrawCentered(ask, detail.X + (detail.Width - UiFonts.Measure(ask, 26, UiWeight.ExtraBold)) / 2f, y + 10, 26, Muted, UiWeight.ExtraBold);
            Stepper(new Vector2(detail.X + detail.Width / 2f, y + 76), Text.Of("crafting.make.times", ("n", screen.HowMany)));
            return;
        }
        if (screen.Notice.Length > 0)
            DrawWrapped(screen.Notice, x, y + 10, room, 28, screen.LastMade ? Green : Red, 36, UiWeight.ExtraBold);
    }

    /// <summary>Where a material comes from, in a sentence: a type's leavings, or the field's, or the pantry's.</summary>
    internal static string MaterialSource(MaterialData material) => material switch
    {
        { Kind: MaterialKind.Pokemon, Type: { } type, Rare: false } => Text.Of("crafting.leftBy", ("type", Words.Type(type))),
        { Kind: MaterialKind.Pokemon, Type: { } type } => Text.Of("crafting.leftByRare", ("type", Words.Type(type))),
        { Kind: MaterialKind.Pantry } => Text.Of("crafting.pantryGoods"),
        _ => Text.Of("crafting.fieldFinds")
    };

    /// <summary>
    /// A material's sign (style guide, "The kit"): a lump in its colour with a highlight and a rim, a rare one with a
    /// glint star over it; dimmed when none is held. It is drawn with shapes, one picture for every material.
    /// </summary>
    private static void MaterialSign(Vector2 at, float radius, MaterialData material, bool held)
    {
        var body = MaterialColour(material);
        if (!held) body = new Color((int)(body.R * 0.45f + 120), (int)(body.G * 0.45f + 120), (int)(body.B * 0.45f + 120), 255);
        var rim = new Color((int)(body.R * 0.55f), (int)(body.G * 0.55f), (int)(body.B * 0.55f), 255);
        UiShapes.Circle(at + new Vector2(0, radius * 0.06f), radius * 0.78f + 3, rim);
        UiShapes.Circle(at, radius * 0.78f, body);
        UiShapes.Circle(at + new Vector2(-radius * 0.28f, -radius * 0.3f), radius * 0.2f, new Color(255, 255, 255, 150));
        if (!material.Rare) return;
        // The glint of a rare one: two crossed slivers at its upper right
        var star = at + new Vector2(radius * 0.52f, -radius * 0.52f);
        float s = radius * 0.34f;
        var glint = held ? Color.White : new Color(255, 255, 255, 150);
        UiShapes.Fill(new Rectangle(star.X - s, star.Y - s * 0.12f, s * 2, s * 0.24f), s * 0.1f, glint);
        UiShapes.Fill(new Rectangle(star.X - s * 0.12f, star.Y - s, s * 0.24f, s * 2), s * 0.1f, glint);
    }
}
