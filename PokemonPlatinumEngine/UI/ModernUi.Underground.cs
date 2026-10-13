using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Underground;
using PokemonPlatinumEngine.UI.Kit;
using Raylib_cs;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Underground's screens (plan 06 · R16a; style guide, "The Underground"): the wall's game, the lists of the bag
/// below ground, a hiker's trade and the base's PC, the room decorated from above, and what a trap puts over the
/// field. Everything pictured is our own: the wall's dirt in three tones by its thickness, each thing in the wall a
/// plain shape in its own colour, a Sphere a ball with a glint.
/// </summary>
internal static partial class ModernUi
{
    // ------------------------------------------------------------------ colours of the things below ground

    public static Color SphereColour(SphereKind kind) => kind switch
    {
        SphereKind.Prism => new Color(236, 232, 250, 255),
        SphereKind.Pale => new Color(196, 178, 224, 255),
        SphereKind.Red => new Color(226, 84, 76, 255),
        SphereKind.Blue => new Color(78, 132, 226, 255),
        SphereKind.Green => new Color(84, 184, 110, 255),
        _ => Muted
    };

    /// <summary>The colour a treasure is drawn in, by what it is (our own choice, to tell them apart in the wall).</summary>
    public static Color TreasureColour(int id)
    {
        string key = UndergroundDatabase.Treasure(id)?.Id ?? "";
        if (UndergroundDatabase.IsSphere(id)) return SphereColour(UndergroundDatabase.SphereOf(id));
        if (key.EndsWith("_FOSSIL", StringComparison.Ordinal) || key.EndsWith("RARE_BONE", StringComparison.Ordinal)) return new Color(214, 200, 168, 255);
        if (key.EndsWith("OLD_AMBER", StringComparison.Ordinal)) return new Color(232, 164, 60, 255);
        if (key.Contains("RED_SHARD") || key.Contains("FIRE_STONE") || key.Contains("FLAME") || key.Contains("HEAT") || key.Contains("DRACO")) return new Color(224, 96, 72, 255);
        if (key.Contains("BLUE_SHARD") || key.Contains("WATER_STONE") || key.Contains("SPLASH") || key.Contains("DAMP") || key.Contains("SKY")) return new Color(84, 140, 228, 255);
        if (key.Contains("YELLOW_SHARD") || key.Contains("THUNDER") || key.Contains("ZAP") || key.Contains("REVIVE") || key.Contains("STAR") || key.Contains("SUN")) return new Color(238, 196, 64, 255);
        if (key.Contains("GREEN_SHARD") || key.Contains("LEAF") || key.Contains("MEADOW") || key.Contains("INSECT")) return new Color(96, 186, 96, 255);
        if (key.Contains("HEART") || key.Contains("MIND")) return new Color(236, 128, 170, 255);
        if (key.Contains("ICY") || key.Contains("ICICLE") || key.Contains("MOON") || key.Contains("LIGHT_CLAY")) return new Color(176, 214, 232, 255);
        if (key.Contains("SPOOKY") || key.Contains("TOXIC") || key.Contains("DREAD") || key.Contains("ODD_KEYSTONE")) return new Color(140, 104, 176, 255);
        if (key.Contains("EARTH") || key.Contains("FIST") || key.Contains("SMOOTH") || key.Contains("STONE_PLATE")) return new Color(176, 134, 92, 255);
        return new Color(168, 172, 186, 255);
    }

    /// <summary>A small picture for a row of the Underground's lists: a Sphere, a trap's warning sign, a crate of goods, a gem, a flag, the way up.</summary>
    public static void UndergroundIcon(Vector2 c, float r, string icon)
    {
        UiShapes.Circle(c, r, Disc);
        if (icon.StartsWith(UndergroundRow.SpherePicture, StringComparison.Ordinal) && Enum.TryParse(icon[UndergroundRow.SpherePicture.Length..], out SphereKind kind))
        {
            SphereBall(c, r * 0.62f, kind);
            return;
        }
        if (icon.StartsWith(UndergroundRow.TreasurePicture, StringComparison.Ordinal))
        {
            var colour = TreasureColour(int.Parse(icon[UndergroundRow.TreasurePicture.Length..]));
            UiShapes.Turned(c, r * 0.46f, r * 0.46f, 5, 45, Darker(colour, 0.25f));
            UiShapes.Turned(c - new Vector2(1, 2), r * 0.36f, r * 0.36f, 4, 45, colour);
            return;
        }
        switch (icon)
        {
            case "trap":
                UiShapes.Triangle(c + new Vector2(0, -r * 0.6f), c + new Vector2(r * 0.6f, r * 0.45f), c + new Vector2(-r * 0.6f, r * 0.45f), Gold, 4);
                UiShapes.Fill(new Rectangle(c.X - 3, c.Y - r * 0.28f, 6, r * 0.42f), 3, Ink);
                UiShapes.Circle(c + new Vector2(0, r * 0.3f), 3.5f, Ink);
                break;
            case "flag":
                UiShapes.Fill(new Rectangle(c.X - r * 0.4f, c.Y - r * 0.6f, 5, r * 1.2f), 2, Frame);
                UiShapes.Triangle(c + new Vector2(-r * 0.4f + 5, -r * 0.6f), c + new Vector2(r * 0.55f, -r * 0.3f), c + new Vector2(-r * 0.4f + 5, 0), Red, 2);
                break;
            case "up":
                UiIcons.ArrowUp(c, r * 0.5f, Blue);
                break;
            default:
                UiShapes.Fill(new Rectangle(c.X - r * 0.5f, c.Y - r * 0.4f, r, r * 0.85f), 5, new Color(190, 142, 92, 255));
                UiShapes.Fill(new Rectangle(c.X - r * 0.5f, c.Y - r * 0.4f, r, r * 0.26f), 5, new Color(214, 170, 116, 255));
                break;
        }
    }

    public static void SphereBall(Vector2 c, float r, SphereKind kind)
    {
        var colour = SphereColour(kind);
        UiShapes.Circle(c, r, Darker(colour, 0.3f));
        UiShapes.Circle(c - new Vector2(r * 0.06f, r * 0.08f), r * 0.86f, colour);
        UiShapes.Circle(c - new Vector2(r * 0.34f, r * 0.36f), r * 0.22f, Lighter(colour, 0.7f));
        if (kind != SphereKind.Prism) return;
        // A Prism Sphere's colours run round its rim
        UiShapes.Ring(c, r * 0.7f, r * 0.12f, new Color(236, 150, 190, 150));
        UiShapes.Ring(c, r * 0.5f, r * 0.1f, new Color(130, 200, 236, 150));
    }

    // ------------------------------------------------------------------ the wall

    private static readonly Color Bedrock = new(58, 50, 60, 255), DarkRock = new(84, 86, 104, 255);

    private static Color DirtColour(int layers) => layers switch
    {
        <= 2 => new Color(186, 150, 104, 255),
        <= 4 => new Color(140, 110, 80, 255),
        _ => new Color(104, 92, 92, 255)
    };

    public static void DrawMining(int sw, int sh, MiningScreen screen)
    {
        var game = screen.Game;
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("digging.title"));
        if (screen.Phase == MiningPhase.Digging) Hints(sw - Margin, 44, Hint("shared.keys.z", "digging.hint.strike"), Hint("shared.keys.x", "digging.hint.changeTool"));
        else Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.next"));

        const float cell = 68;
        float wallW = MiningGame.Width * cell, wallH = MiningGame.Height * cell;
        // A blow shakes the wall, the harder the nearer it is to falling in (Mining_QueueScreenShake)
        float shakeFor = (1f - game.Integrity / (float)MiningGame.InitialIntegrity) * 0.4f, amount = screen.SinceBlow < shakeFor ? 3f + game.Crack * 6f : 0f;
        var shake = new Vector2(MathF.Round(MathF.Sin(screen.Age * 90f) * amount), MathF.Round(MathF.Cos(screen.Age * 70f) * amount));
        var frame = new Rectangle(Margin, ContentTop, wallW + 32, wallH + 32 + 56);
        Panel(frame, 30);
        var wall = new Rectangle(frame.X + 16 + shake.X, frame.Y + 16 + 56 + shake.Y, wallW, wallH);

        // The crack that runs along the top of the wall, from the left, as it weakens
        var strip = new Rectangle(frame.X + 16, frame.Y + 14, wallW, 44);
        UiShapes.Fill(strip, 10, new Color(84, 74, 80, 255));
        float cracked = (strip.Width - 290) * game.Crack;
        if (cracked > 4)
        {
            var from = new Vector2(strip.X + 4, strip.Y + 22);
            int steps = Math.Max(1, (int)(cracked / 22));
            for (int i = 0; i < steps; i++)
            {
                var to = new Vector2(strip.X + 4 + cracked * (i + 1) / steps, strip.Y + 22 + (i % 2 == 0 ? -9 : 9) * (i == steps - 1 ? 0.3f : 1f));
                UiShapes.Line(from, to, 5, new Color(24, 18, 26, 255));
                from = to;
            }
        }
        UiFonts.DrawCentered(game.Integrity == 0 ? Text.Of("digging.gaveWay") : Text.Of("digging.holds"), strip.X + strip.Width - 230, strip.Y + 22, 20, new Color(214, 204, 196, 255), UiWeight.Black);

        Raylib.DrawRectangle((int)wall.X, (int)wall.Y, (int)wall.Width, (int)wall.Height, Bedrock);
        // What is set in the wall, under the dirt
        foreach (var o in game.Objects)
        {
            if (!o.IsRock && UndergroundDatabase.IsSphere(o.Id))
            {
                var centre = new Vector2(wall.X + (o.X + o.Object.Width / 2f) * cell, wall.Y + (o.Y + o.Object.Height / 2f) * cell);
                SphereBall(centre, o.Object.Width * cell * 0.46f, UndergroundDatabase.SphereOf(o.Id));
                continue;
            }
            var colour = o.IsRock ? DarkRock : TreasureColour(o.Id);
            for (int y = o.Y; y < o.Y + o.Object.Height; y++)
                for (int x = o.X; x < o.X + o.Object.Width; x++)
                {
                    if (!o.Covers(x, y)) continue;
                    var r = new Rectangle(wall.X + x * cell, wall.Y + y * cell, cell, cell);
                    Raylib.DrawRectangle((int)r.X, (int)r.Y, (int)cell, (int)cell, Darker(colour, 0.22f));
                    // Lit from the upper left: a lighter face inside a darker rim on the sides that are the thing's edge
                    float l = o.Covers(x - 1, y) ? 0 : 6, t = o.Covers(x, y - 1) ? 0 : 6, rr = o.Covers(x + 1, y) ? 0 : 8, b = o.Covers(x, y + 1) ? 0 : 8;
                    Raylib.DrawRectangle((int)(r.X + l), (int)(r.Y + t), (int)(cell - l - rr), (int)(cell - t - b), colour);
                    if (!o.IsRock && (x + y) % 2 == 0) Raylib.DrawRectangle((int)(r.X + 14), (int)(r.Y + 12), 12, 6, Lighter(colour, 0.4f));
                    if (o.IsRock && (x * 3 + y) % 2 == 0) Raylib.DrawRectangle((int)(r.X + 34), (int)(r.Y + 40), 16, 6, Darker(colour, 0.2f));
                }
        }
        // The dirt over it, lighter where it lies thin
        for (int y = 0; y < MiningGame.Height; y++)
            for (int x = 0; x < MiningGame.Width; x++)
            {
                int layers = game.DirtAt(x, y);
                if (layers == 0) continue;
                var colour = DirtColour(layers);
                int px = (int)(wall.X + x * cell), py = (int)(wall.Y + y * cell);
                Raylib.DrawRectangle(px, py, (int)cell, (int)cell, colour);
                Raylib.DrawRectangle(px, py, (int)cell, 4, Lighter(colour, 0.14f));
                Raylib.DrawRectangle(px, py + (int)cell - 5, (int)cell, 5, Darker(colour, 0.14f));
                // Stones in the dirt, at places of the square's own; more of them the thicker it lies
                for (int i = 0; i < (layers + 1) / 2; i++)
                {
                    int h = (x * 31 + y * 17 + i * 13) % 47;
                    Raylib.DrawRectangle(px + 8 + h % 5 * 11, py + 10 + h % 4 * 13, 10, 7, Darker(colour, 0.2f + (layers % 2) * 0.06f));
                }
            }
        // What lies wholly bare glints
        for (int i = 0; i < game.ItemCount; i++)
        {
            if (!game.IsBare(i)) continue;
            var o = game.Objects[i];
            float pulse = 0.6f + 0.4f * MathF.Sin(screen.Age * 6f + i);
            Star(new Vector2(wall.X + (o.X + o.Object.Width / 2f) * cell + 14, wall.Y + (o.Y + o.Object.Height / 2f) * cell - 16), 14 * pulse, Color.White);
        }
        if (screen.Phase == MiningPhase.Digging)
        {
            var at = new Rectangle(wall.X + screen.CursorX * cell, wall.Y + screen.CursorY * cell, cell, cell);
            UiShapes.Shape(at, 10, new Color(255, 255, 255, 30), new Color(255, 255, 255, 30), Color.White, 5);
            UiShapes.Shape(new Rectangle(at.X - 4, at.Y - 4, cell + 8, cell + 8), 12, new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), Selection, 4);
        }

        // Beside the wall: the two tools, and what the wall holds
        float rx = frame.X + frame.Width + Gutter, rw = sw - Margin - rx;
        var tools = new Rectangle(rx, ContentTop, rw, 330);
        Panel(tools, 30);
        Label(Text.Of("digging.tools"), tools.X + 36, tools.Y + 26);
        ToolButton(new Rectangle(tools.X + 28, tools.Y + 66, tools.Width - 56, 108), Text.Of("digging.pick.name"), Text.Of("digging.pick.note"), game.PickSelected, hammer: false);
        ToolButton(new Rectangle(tools.X + 28, tools.Y + 192, tools.Width - 56, 108), Text.Of("digging.hammer.name"), Text.Of("digging.hammer.note"), !game.PickSelected, hammer: true);

        var held = new Rectangle(rx, tools.Y + tools.Height + 24, rw, ContentBottom - 150 - (tools.Y + tools.Height + 24) - 24);
        Panel(held, 30);
        Label(Text.Of("digging.inTheWall"), held.X + 36, held.Y + 26);
        for (int i = 0; i < game.ItemCount; i++)
        {
            var row = new Rectangle(held.X + 28, held.Y + 66 + i * 82, held.Width - 56, 70);
            bool bare = game.IsBare(i);
            UiShapes.Fill(row, 20, bare ? new Color(232, 244, 232, 255) : Disc);
            int id = game.Objects[i].Id;
            // What a thing is isn't told until it lies bare
            if (bare)
            {
                UndergroundIcon(new Vector2(row.X + 40, row.Y + 35), 26, UndergroundDatabase.IsSphere(id) ? UndergroundRow.SphereIcon(UndergroundDatabase.SphereOf(id)) : UndergroundRow.TreasureIcon(id));
                string name = UndergroundDatabase.IsSphere(id) ? UndergroundDatabase.SphereName(UndergroundDatabase.SphereOf(id)) : UndergroundDatabase.TreasureName(id);
                UiFonts.DrawCentered(name, row.X + 82, row.Y + 35, 28, Ink, UiWeight.Black);
            }
            else
            {
                UiShapes.Circle(new Vector2(row.X + 40, row.Y + 35), 26, Rule);
                UiFonts.DrawCentered("?", row.X + 32, row.Y + 35, 30, Muted, UiWeight.Black);
                UiFonts.DrawCentered(Text.Of("digging.something"), row.X + 82, row.Y + 35, 28, Muted, UiWeight.ExtraBold);
            }
        }

        var box = new Rectangle(Margin, ContentBottom - 126 + 30, sw - Margin * 2, 126 - 30);
        if (frame.Y + frame.Height + 12 > box.Y) box = new Rectangle(rx, ContentBottom - 126, rw, 126);
        Panel(box, 28);
        string line = screen.Line.Length > 0 ? screen.Line : game.PickSelected ? Text.Of("digging.inHand.pick") : Text.Of("digging.inHand.hammer");
        DrawWrapped(line, box.X + 36, box.Y + 22, box.Width - 72, 28, Ink, 36, UiWeight.ExtraBold);
    }

    private static void ToolButton(Rectangle r, string name, string says, bool selected, bool hammer)
    {
        var colour = hammer ? new Color(206, 96, 70, 255) : new Color(70, 136, 200, 255);
        if (selected)
        {
            UiShapes.Shadow(r, 24, 18, new Vector2(0, 5), colour with { A = 120 });
            UiShapes.Shape(r, 24, Lighter(colour, 0.14f), Darker(colour, 0.08f), Darker(colour, 0.3f), 3);
        }
        else UiShapes.Shape(r, 24, PanelTop, PanelBottom, Rule, 3);
        var c = new Vector2(r.X + 54, r.Y + r.Height / 2f);
        UiShapes.Circle(c, 34, selected ? Color.White : colour);
        var ink = selected ? colour : Color.White;
        // A handle and a head: the pick's is a thin curve, the hammer's a block
        UiShapes.Line(c + new Vector2(-14, 16), c + new Vector2(10, -10), 6, ink);
        if (hammer) UiShapes.Turned(c + new Vector2(10, -10), 14, 9, 3, -45, ink);
        else
        {
            UiShapes.Line(c + new Vector2(-6, -16), c + new Vector2(18, -4), 6, ink);
            UiShapes.Line(c + new Vector2(-6, -16), c + new Vector2(-12, -8), 5, ink);
        }
        UiFonts.DrawCentered(name, r.X + 104, r.Y + 34, 30, selected ? Color.White : Ink, UiWeight.Black);
        UiFonts.DrawCentered(says, r.X + 104, r.Y + 72, 19, selected ? new Color(255, 255, 255, 230) : Muted, UiWeight.ExtraBold);
    }

    // ------------------------------------------------------------------ the bag below ground, a hiker, the base's PC

    public static void DrawUnderground(int sw, int sh, UndergroundScreen screen)
    {
        Backdrop(sw, sh);
        ScreenTitle(screen.Mode switch { UndergroundMode.Hiker => Text.Of("belowGround.title.hiker"), UndergroundMode.Pc => Text.Of("belowGround.title.pc"), _ => Text.Of("belowGround.title.bag") });
        Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.choose"), Hint("shared.keys.esc", "shared.hints.back"));

        var strip = new Rectangle(Margin, ContentTop, Math.Min(sw - Margin * 2, 300f * screen.Tabs.Count + 60), 76);
        var colours = new[] { Gold, Blue, new Color(190, 142, 92, 255), Green, Frame };
        Tabs(strip, screen.Tabs.Select((t, i) => (t, i switch { 0 => UiIcon.Cross, 1 => UiIcon.Disc, 2 => UiIcon.Bag, 3 => UiIcon.Pouch, _ => UiIcon.Pin }, colours[i % colours.Length])).ToList(), screen.Tab);

        float top = ContentTop + 76 + 20;
        var list = new Rectangle(Margin, top, 940, ContentBottom - top);
        Panel(list, 34);
        var rows = screen.Rows;
        if (rows.Count == 0) EmptyNote(list, Text.Of("belowGround.nothingHere"));
        for (int i = 0; i < UndergroundScreen.VisibleRows && screen.First + i < rows.Count; i++)
        {
            int index = screen.First + i;
            var row = rows[index];
            bool selected = index == screen.Cursor && !screen.Paying;
            var r = RowRect(list, i, 24, rows.Count > UndergroundScreen.VisibleRows);
            var ink = ListRow(r, selected && row.Enabled, row.Name, row.Enabled || !selected);
            if (selected && !row.Enabled) UiShapes.Shape(r, 26, new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), Rule, 3);
            UndergroundIcon(new Vector2(r.X + 52, r.Y + r.Height / 2f), 30, row.Icon);
            if (row.Value.Length > 0) RowValue(r, row.Value, ink);
        }
        if (rows.Count > UndergroundScreen.VisibleRows) ScrollBar(list, screen.First, UndergroundScreen.VisibleRows, rows.Count);

        float rx = Margin + 940 + Gutter, rw = sw - Margin - rx;
        var detail = new Rectangle(rx, top, rw, 380);
        Panel(detail, 34);
        if (screen.Selected is { } chosen)
        {
            UndergroundIcon(new Vector2(detail.X + 92, detail.Y + 96), 56, chosen.Icon);
            UiFonts.DrawCentered(chosen.Name, detail.X + 172, detail.Y + 80, 40, Ink, UiWeight.Black);
            if (chosen.Value.Length > 0) Tag(detail.X + 172, detail.Y + 112, chosen.Value.ToUpperInvariant(), Frame, 36);
            UiShapes.Fill(new Rectangle(detail.X + 36, detail.Y + 176, detail.Width - 72, 3), 1.5f, Rule);
            DrawWrapped(chosen.Detail, detail.X + 36, detail.Y + 200, detail.Width - 72, 26, Ink, 36, UiWeight.ExtraBold);
        }

        // The Spheres on hand, which is what everything is paid in
        var purse = new Rectangle(rx, top + 380 + 24, rw, ContentBottom - top - 380 - 24);
        Panel(purse, 34);
        Label(screen.Paying ? Text.Of("belowGround.payWith") : Text.Of("belowGround.spheresOnHand"), purse.X + 36, purse.Y + 26);
        var spheres = screen.State.Spheres;
        int perRow = Math.Max(1, (int)((purse.Width - 56) / 150)), shown = Math.Min(spheres.Count, perRow * 4);
        int firstSphere = screen.Paying ? Math.Max(0, Math.Min(screen.PayCursor / perRow * perRow - perRow, Math.Max(0, spheres.Count - shown))) : 0;
        if (spheres.Count == 0) UiFonts.Draw(Text.Of("belowGround.noSpheresYet"), purse.X + 36, purse.Y + 74, 26, Muted, UiWeight.ExtraBold);
        for (int i = 0; i < shown && firstSphere + i < spheres.Count; i++)
        {
            var sphere = spheres[firstSphere + i];
            var r = new Rectangle(purse.X + 28 + i % perRow * 150, purse.Y + 64 + i / perRow * 74, 140, 64);
            bool selected = screen.Paying && firstSphere + i == screen.PayCursor;
            UiShapes.Shape(r, 22, selected ? Lighter(Selection, 0.14f) : Disc, selected ? Darker(Selection, 0.06f) : Disc, selected ? Darker(Selection, 0.3f) : Rule, 3);
            SphereBall(new Vector2(r.X + 34, r.Y + 32), 22, sphere.Kind);
            UiFonts.DrawCentered(sphere.Size.ToString(), r.X + 70, r.Y + 32, 30, selected ? Color.White : Ink, UiWeight.Black);
        }

        if (screen.Actions is { } actions)
        {
            int at = screen.Cursor - screen.First;
            MenuPanel(list.X + list.Width - 380, Math.Min(list.Y + 24 + at * RowPitch + 40, sh - 64 - MenuPanelHeight(actions.Count)), actions, screen.ActionCursor);
        }
        if (screen.Question is { } question)
            Prompt(sw, sh, question, "", new[] { (Text.Of("belowGround.answer.yes"), Blue), (Text.Of("belowGround.answer.no"), Red) }, screen.Answer);
        else if (screen.Message is { } message) Prompt(sw, sh, message, "", new[] { (Text.Of("shared.ok"), Blue) }, 0);
    }

    // ------------------------------------------------------------------ a room being decorated

    public static void DrawDecoration(int sw, int sh, DecorationScreen screen)
    {
        var session = screen.Session;
        var room = session.Room;
        Backdrop(sw, sh);
        ScreenTitle(screen.Title);
        Hints(sw - Margin, 44, Hint("shared.keys.z", session.Step is DecoratingStep.Placing or DecoratingStep.Moving ? "decorating.setDown" : "shared.hints.choose"), Hint("shared.keys.esc", "shared.hints.back"));

        float cell = MathF.Floor(Math.Min((ContentBottom - ContentTop - 56) / room.Height, 900f / room.Width));
        var plan = new Rectangle(Margin, ContentTop, room.Width * cell + 56, room.Height * cell + 56);
        Panel(plan, 34);
        var floor = new Rectangle(plan.X + 28, plan.Y + 28, room.Width * cell, room.Height * cell);
        Raylib.DrawRectangle((int)floor.X, (int)floor.Y, (int)floor.Width, (int)floor.Height, new Color(150, 132, 120, 255));
        for (int y = 0; y < room.Height; y++)
            for (int x = 0; x < room.Width; x++)
            {
                bool kept = room.KeptClear.Contains((room.Left + x, room.Top + y));
                var tile = (x + y) % 2 == 0 ? new Color(160, 142, 128, 255) : new Color(152, 134, 122, 255);
                Raylib.DrawRectangle((int)(floor.X + x * cell) + 1, (int)(floor.Y + y * cell) + 1, (int)cell - 2, (int)cell - 2, kept ? new Color(176, 160, 140, 255) : tile);
            }

        Rectangle RectOf(int x, int y, int w, int d) => new(floor.X + (x - room.Left) * cell + 3, floor.Y + (y - room.Top) * cell + 3, w * cell - 6, d * cell - 6);
        void Piece(RoomPiece piece, int x, int y, Color? rim)
        {
            var (colour, mark) = screen.Look(piece);
            var r = RectOf(x, y, piece.Width, piece.Depth);
            UiShapes.Shadow(r, 12, 10, new Vector2(0, 4), new Color(20, 14, 20, 90));
            UiShapes.Shape(r, 12, Lighter(colour, 0.12f), Darker(colour, 0.1f), rim ?? Darker(colour, 0.36f), rim != null ? 5 : 3);
            // Its mark, or the last word of its name (the Plain Table's "Table"), as large as the piece has room for
            string label = mark.Length > 0 ? mark : piece.Name[(piece.Name.LastIndexOf(' ') + 1)..];
            if (label.Length == 0) return;
            float wide = UiFonts.Measure(label, 22, UiWeight.Black);
            int size = (int)Math.Clamp(22f * (r.Width - 10) / Math.Max(1f, wide), 11f, 22f);
            bool dark = colour.R * 0.3f + colour.G * 0.59f + colour.B * 0.11f < 140f;
            UiFonts.DrawCentered(label, r.X + r.Width / 2f - UiFonts.Measure(label, size, UiWeight.Black) / 2f, r.Y + r.Height / 2f, size,
                dark ? Lighter(colour, 0.7f) : Darker(colour, 0.62f), UiWeight.Black);
        }
        foreach (var piece in room.Pieces)
            if (piece != session.Held) Piece(piece, piece.X, piece.Y, session.Under == piece ? Selection : null);
        if (session.Held is { } held) Piece(held, session.X, session.Y, session.Fits ? Green : Red);
        else if (session.Step is DecoratingStep.PuttingAway or DecoratingStep.Picking)
        {
            var at = RectOf(session.X, session.Y, 1, 1);
            UiShapes.Shape(new Rectangle(at.X - 5, at.Y - 5, at.Width + 10, at.Height + 10), 14, new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), Selection, 5);
        }

        float rx = plan.X + plan.Width + Gutter, rw = sw - Margin - rx;
        var side = new Rectangle(rx, ContentTop, rw, ContentBottom - ContentTop - 150);
        Panel(side, 34);
        switch (session.Step)
        {
            case DecoratingStep.Menu:
                Label(room.Limit < int.MaxValue ? Text.Of("decorating.countOf", ("out", room.Out), ("limit", room.Limit)) : Text.Of("decorating.count", ("out", room.Out)), side.X + 40, side.Y + 30);
                var menu = DecoratingSession.MenuRows;
                for (int i = 0; i < menu.Length; i++)
                    ListRow(RowRect(side, i, 72), i == session.MenuCursor, menu[i], nameX: 44);
                break;
            case DecoratingStep.Choosing:
                Label(Text.Of("decorating.inStore"), side.X + 40, side.Y + 30);
                for (int i = 0; i < DecorationScreen.VisibleRows && screen.First + i < session.Store.Count; i++)
                {
                    int slot = screen.First + i;
                    var entry = session.Store[slot];
                    var r = RowRect(side, i, 72, session.Store.Count > DecorationScreen.VisibleRows);
                    var kind = session.KindOf(entry.Kind);
                    var ink = ListRow(r, slot == session.StoreCursor, kind.Name, !entry.Out, 44);
                    RowValue(r, entry.Out ? Text.Of("decorating.out") : $"{kind.Width}×{kind.Depth}", ink, 26);
                }
                if (session.Store.Count > DecorationScreen.VisibleRows) ScrollBar(side, screen.First, DecorationScreen.VisibleRows, session.Store.Count, 72);
                break;
            default:
            {
                string doing = session.Step switch
                {
                    DecoratingStep.Placing => Text.Of("decorating.doing.placing"),
                    DecoratingStep.Moving => Text.Of("decorating.doing.moving"),
                    DecoratingStep.PuttingAway => Text.Of("decorating.doing.puttingAway"),
                    _ => Text.Of("decorating.doing.picking")
                };
                Label(session.Step is DecoratingStep.Placing or DecoratingStep.Moving ? Text.Of("decorating.inHand") : Text.Of("decorating.inTheRoom"), side.X + 40, side.Y + 30);
                var shown = session.Held ?? session.Under;
                if (shown != null) UiFonts.Draw(shown.Name, side.X + 40, side.Y + 76, 40, Ink, UiWeight.Black);
                DrawWrapped(doing, side.X + 40, side.Y + (shown != null ? 150 : 84), side.Width - 80, 28, Muted, 38, UiWeight.ExtraBold);
                break;
            }
        }

        var box = new Rectangle(rx, ContentBottom - 126, rw, 126);
        Panel(box, 28);
        DrawWrapped(session.Message ?? "", box.X + 36, box.Y + 24, box.Width - 72, 28, Ink, 36, UiWeight.ExtraBold);
    }

    // ------------------------------------------------------------------ a trap over the field

    /// <summary>
    /// What a trap gone off puts over the field (plan 06 · R16a), in the console's own dots stretched to the screen:
    /// smoke in blocks, bubbles, leaves, a boulder, a flame, and a word of what to do.
    /// </summary>
    public static void DrawTrap(int sw, int sh, TrapEffect trap, float time)
    {
        float ux = sw / (float)TrapEffect.ScreenWidth, uy = sh / (float)TrapEffect.ScreenHeight;
        switch (trap.Family)
        {
            case TrapFamily.Smoke:
            {
                float w = sw / (float)TrapEffect.SmokeColumns, h = sh / (float)TrapEffect.SmokeRows;
                // Two layers of puffs, a tile's smoke a puff of each: the under one wider and greyer, the upper one
                // paler, each nudged off its tile's middle by the tile's own place and breathing a little
                for (int layer = 0; layer < 2; layer++)
                    for (int y = 0; y < TrapEffect.SmokeRows; y++)
                        for (int x = 0; x < TrapEffect.SmokeColumns; x++)
                        {
                            int v = trap.Smoke[y, x];
                            if (v == 0) continue;
                            int mix = (x * 7 + y * 13 + layer * 5) % 11;
                            float thin = 1f - (v - 1) / 6f;
                            float jx = (mix % 5 - 2) * 5f + MathF.Sin(time * 0.7f + mix) * 4f, jy = (mix % 3 - 1) * 5f + MathF.Cos(time * 0.6f + mix * 2) * 3f;
                            var at = new Vector2((x + 0.5f) * w + jx, (y + 0.5f) * h + jy);
                            if (layer == 0) UiShapes.Circle(at, w * 0.92f, new Color((byte)150, (byte)150, (byte)164, (byte)(230 * thin)));
                            else UiShapes.Circle(at - new Vector2(6, 8), w * (0.6f + mix % 4 * 0.05f), new Color((byte)(214 + mix % 3 * 8), (byte)(214 + mix % 3 * 8), (byte)226, (byte)(215 * thin)));
                        }
                var hand = new Rectangle((trap.HandX - 1) * w, (trap.HandY - TrapEffect.HandReach) * h, w * 3, h * (TrapEffect.HandReach * 2 + 1));
                UiShapes.Shape(hand, 26, new Color(255, 255, 255, 46), new Color(255, 255, 255, 46), Color.White, 5);
                break;
            }
            case TrapFamily.Bubbles:
                foreach (var f in trap.Floaters.Where(f => !f.Gone))
                {
                    var c = new Vector2(f.X * ux, f.Y * uy);
                    float r = TrapEffect.BubbleRadius(f.Size) * uy * 0.9f;
                    UiShapes.Circle(c, r, new Color(170, 214, 244, 150));
                    UiShapes.Ring(c, r, 6, new Color(236, 248, 255, 230));
                    UiShapes.Circle(c - new Vector2(r * 0.36f, r * 0.4f), r * 0.16f, new Color(255, 255, 255, 230));
                }
                break;
            case TrapFamily.Leaves:
            {
                bool flowers = trap.Floaters.Count > 16;
                foreach (var f in trap.Floaters.Where(f => !f.Gone))
                {
                    var c = new Vector2(f.X * ux, f.Y * uy);
                    float turn = f.Angle / 65536f * 360f + MathF.Sin(time * 2f + f.Angle) * 14f;
                    if (flowers)
                    {
                        for (int p = 0; p < 5; p++)
                        {
                            float a = (turn + p * 72f) * MathF.PI / 180f;
                            UiShapes.Circle(c + new Vector2(MathF.Cos(a), MathF.Sin(a)) * 30, 24, new Color(244, 150, 190, 255));
                        }
                        UiShapes.Circle(c, 18, new Color(250, 222, 110, 255));
                    }
                    else
                    {
                        UiShapes.Turned(c, 66, 30, 28, turn, new Color(60, 138, 72, 255));
                        UiShapes.Turned(c, 58, 22, 22, turn, new Color(104, 190, 96, 255));
                    }
                }
                break;
            }
            case TrapFamily.Rock:
            {
                var c = new Vector2(sw / 2f, sh / 2f - 30);
                float r = 250;
                UiShapes.Glow(c + new Vector2(0, r * 0.9f), r * 1.1f, r * 0.26f, new Color(10, 8, 16, 150));
                UiShapes.Circle(c, r, new Color(84, 80, 96, 255));
                UiShapes.Circle(c - new Vector2(18, 22), r * 0.9f, new Color(126, 120, 136, 255));
                UiShapes.Circle(c - new Vector2(70, 90), r * 0.42f, new Color(156, 150, 164, 255));
                // It cracks a little more with every third blow (the original's seven pictures of it)
                int cracks = Math.Clamp((trap.BoulderDamage + 1) / 3, 0, 5);
                for (int i = 0; i < cracks; i++)
                {
                    float a = i * 1.3f + 0.4f;
                    var from = c + new Vector2(MathF.Cos(a), MathF.Sin(a)) * 30;
                    var mid = c + new Vector2(MathF.Cos(a + 0.2f), MathF.Sin(a + 0.2f)) * r * 0.5f;
                    var to = c + new Vector2(MathF.Cos(a - 0.1f), MathF.Sin(a - 0.1f)) * r * 0.92f;
                    UiShapes.Line(from, mid, 9, new Color(34, 30, 42, 255));
                    UiShapes.Line(mid, to, 7, new Color(34, 30, 42, 255));
                }
                break;
            }
            case TrapFamily.Fire:
            {
                var foot = new Vector2(sw / 2f, sh / 2f + 190);
                float size = trap.Flame * 0.5f + (trap.BlowFrames > 0 ? 0.1f * MathF.Sin(time * 40f) : 0.06f * MathF.Sin(time * 9f));
                float lean = trap.BlowFrames > 0 ? 50 : 0;
                foreach (var (scale, colour) in new[] { (1f, new Color(226, 84, 44, 255)), (0.72f, new Color(244, 150, 50, 255)), (0.42f, new Color(252, 226, 120, 255)) })
                {
                    float h = 300 * size * scale, w = 130 * size * scale;
                    UiShapes.Circle(foot - new Vector2(0, w * 0.7f), w, colour);
                    UiShapes.Triangle(foot + new Vector2(-w * 0.94f, -w * 1.0f), foot + new Vector2(w * 0.94f, -w * 1.0f), foot + new Vector2(lean, -h - w), colour, 6);
                }
                break;
            }
            case TrapFamily.Hole:
                UiShapes.Glow(new Vector2(sw / 2f, sh / 2f + 40), 300, 150, new Color(6, 4, 12, 210));
                break;
            case TrapFamily.Alert when trap.Call != null:
            {
                var bubble = new Rectangle(sw / 2f - 420, 150, 840, 110);
                Panel(bubble, 40);
                UiFonts.DrawCentered(trap.Call, bubble.X + bubble.Width / 2f - UiFonts.Measure(trap.Call, 36, UiWeight.Black) / 2f, bubble.Y + 55, 36, Ink, UiWeight.Black);
                break;
            }
        }
        if (trap.Hint.Length == 0) return;
        float width = UiFonts.Measure(trap.Hint, 30, UiWeight.Black) + 96;
        var pill = new Rectangle(sw / 2f - width / 2f, sh - 150, width, 72);
        UiShapes.Shadow(pill, 36, 18, new Vector2(0, 6), ShadowColor);
        UiShapes.Shape(pill, 36, PanelTop, PanelBottom, Frame, 4);
        UiFonts.DrawCentered(trap.Hint, pill.X + 48, pill.Y + 36, 30, Ink, UiWeight.Black);
    }
}
