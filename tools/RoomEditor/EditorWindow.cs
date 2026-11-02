using System.Numerics;
using ContentCheck;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using Raylib_cs;

namespace RoomEditor;

/// <summary>
/// The editor's window (plan 16 · T9): it draws the documents and turns the mouse and the keys into their methods,
/// and nothing else. Three tabs: the room (painting and placing), the overlay (the area's list), and the preview,
/// the content check's plan of the map the game makes of the room (<see cref="Plans.OfRoom"/>).
/// </summary>
public sealed class EditorWindow
{
    public enum Tab { Room, Overlay, Preview }
    public enum Tool { Paint, Solid, Overhead, Prop, Warp, Sign, Person, Trigger, Select }

    private const int PaletteWidth = 230, FieldsWidth = 420, Bar = 28, FindingsHeight = 120, FontSize = 16;

    private static readonly Color Back = new(30, 32, 40, 255), PanelColor = new(44, 47, 58, 255), Ink = new(232, 232, 236, 255),
        Dim = new(150, 152, 166, 255), Accent = new(255, 196, 64, 255), Hatch = new(0, 0, 0, 110), Bad = new(255, 110, 110, 255);

    private readonly EditorDocument room;
    private readonly OverlayDocument? overlay;
    private readonly string dataFolder;
    private readonly string? overlayPath;
    private readonly Lazy<GameContent> content;

    private Tab tab = Tab.Room;
    private Tool tool = Tool.Paint;
    private TileType tile = TileType.Floor;
    private PropType prop = PropType.Table;
    private int paletteScroll, listScroll;
    private float zoom = 32;
    private Vector2 pan = new(PaletteWidth + 20, Bar + 20);
    private (int X, int Y)? selected;
    private ThingKind? selectedKind;
    private int selectedRow = -1;
    private int editing = -1;
    private string typed = "";
    private string status = "";
    private List<Finding> findings = new();
    private Texture2D? preview;
    private string previewOf = "";
    private bool stroking;

    public EditorWindow(EditorDocument room, string dataFolder, OverlayDocument? overlay = null, string? overlayPath = null)
    {
        this.room = room;
        this.dataFolder = dataFolder;
        this.overlay = overlay;
        this.overlayPath = overlayPath;
        content = new Lazy<GameContent>(() => GameContent.Load(dataFolder));
        status = $"{room.File.Name}: F1 room, F2 overlay, F3 preview. 1-9 tools, left paints or places, right takes away, Ctrl+Z, Ctrl+Y, Ctrl+S saves and checks.";
    }

    /// <summary>Runs the window until it is closed.</summary>
    public void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(1600, 900, "Room editor");
        Raylib.SetTargetFPS(60);
        while (!Raylib.WindowShouldClose())
        {
            Update();
            Draw();
        }
        Close();
    }

    /// <summary>Draws each tab once in a hidden window and writes it as <c>editor_&lt;tab&gt;.png</c> (the plan's <c>--shot</c>).</summary>
    public void Shots(string folder)
    {
        Directory.CreateDirectory(folder);
        Raylib.SetConfigFlags(ConfigFlags.HiddenWindow);
        Raylib.InitWindow(1600, 900, "Room editor");
        foreach (var t in Enum.GetValues<Tab>())
        {
            if (t == Tab.Overlay && overlay == null) continue;
            tab = t;
            for (int i = 0; i < 2; i++) Draw();
            var image = Raylib.LoadImageFromScreen();
            string path = Path.Combine(folder, $"editor_{t.ToString().ToLowerInvariant()}.png");
            Raylib.ExportImage(image, path);
            Raylib.UnloadImage(image);
            Console.WriteLine(path);
        }
        Close();
    }

    private void Close()
    {
        if (preview is { } texture) Raylib.UnloadTexture(texture);
        Raylib.CloseWindow();
    }

    // ------------------------------------------------------------------ input

    private void Update()
    {
        bool ctrl = Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl);
        if (editing >= 0)
        {
            Typing();
            return;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.F1)) tab = Tab.Room;
        if (Raylib.IsKeyPressed(KeyboardKey.F2) && overlay != null) tab = Tab.Overlay;
        if (Raylib.IsKeyPressed(KeyboardKey.F3)) tab = Tab.Preview;
        if (ctrl && Raylib.IsKeyPressed(KeyboardKey.Z)) Act(() => { if (tab == Tab.Overlay) overlay?.Undo(); else room.Undo(); });
        if (ctrl && Raylib.IsKeyPressed(KeyboardKey.Y)) Act(() => { if (tab == Tab.Overlay) overlay?.Redo(); else room.Redo(); });
        if (ctrl && Raylib.IsKeyPressed(KeyboardKey.S)) Act(Save);
        if (!ctrl)
            for (int i = 0; i < 9; i++)
                if (Raylib.IsKeyPressed(KeyboardKey.One + i)) tool = (Tool)i;

        var mouse = Raylib.GetMousePosition();
        float wheel = Raylib.GetMouseWheelMove();
        if (tab == Tab.Room) RoomInput(mouse, wheel);
        else if (tab == Tab.Overlay) OverlayInput(mouse, wheel);
        FieldsInput(mouse);
    }

    private void Act(Action action)
    {
        try
        {
            action();
        }
        catch (Exception e) when (e is ArgumentException or InvalidDataException or FormatException or System.Text.Json.JsonException)
        {
            status = e.Message;
        }
    }

    private (int X, int Y) TileUnder(Vector2 mouse) => ((int)MathF.Floor((mouse.X - pan.X) / zoom), (int)MathF.Floor((mouse.Y - pan.Y) / zoom));

    private bool InGrid(Vector2 mouse) => mouse.X > PaletteWidth && mouse.X < Raylib.GetScreenWidth() - FieldsWidth
        && mouse.Y > Bar && mouse.Y < Raylib.GetScreenHeight() - FindingsHeight;

    private void RoomInput(Vector2 mouse, float wheel)
    {
        if (mouse.X < PaletteWidth)
        {
            paletteScroll = Math.Max(0, paletteScroll - (int)wheel * 3);
            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                int row = (int)(mouse.Y - Bar - 8) / 22 + paletteScroll;
                var items = Palette();
                if (row >= 0 && row < items.Count)
                {
                    if (tool == Tool.Prop) prop = Enum.Parse<PropType>(items[row]);
                    else tile = Enum.Parse<TileType>(items[row]);
                }
            }
            return;
        }
        if (!InGrid(mouse)) return;

        if (wheel != 0)
        {
            float before = zoom;
            zoom = Math.Clamp(zoom * (wheel > 0 ? 1.25f : 0.8f), 6, 96);
            pan = mouse - (mouse - pan) * (zoom / before);
        }
        if (Raylib.IsMouseButtonDown(MouseButton.Middle)) pan += Raylib.GetMouseDelta();

        var (x, y) = TileUnder(mouse);
        if (Raylib.IsMouseButtonPressed(MouseButton.Right) && room.Inside(x, y))
            Act(() => { status = room.Remove(x, y) is { } gone ? $"took away the {gone} at {x},{y}" : "nothing there"; });

        bool painting = tool is Tool.Paint or Tool.Solid or Tool.Overhead;
        if (painting && Raylib.IsMouseButtonDown(MouseButton.Left) && room.Inside(x, y))
        {
            if (!stroking) { room.BeginStroke(); stroking = true; }
            if (tool == Tool.Paint) room.Paint(x, y, tile);
            else if (tool == Tool.Solid) room.SetSolid(x, y, !Raylib.IsKeyDown(KeyboardKey.LeftShift));
            else room.SetOverhead(x, y, Raylib.IsKeyDown(KeyboardKey.LeftShift) ? null : tile);
        }
        if (stroking && !Raylib.IsMouseButtonDown(MouseButton.Left)) { room.EndStroke(); stroking = false; }

        if (!painting && Raylib.IsMouseButtonPressed(MouseButton.Left) && room.Inside(x, y))
            Act(() =>
            {
                switch (tool)
                {
                    case Tool.Prop: room.PlaceProp(prop, x, y); Select(x, y, ThingKind.Prop); break;
                    case Tool.Warp: room.PlaceWarp(x, y, "", 0, 0); Select(x, y, ThingKind.Warp); break;
                    case Tool.Sign: room.PlaceSign(x, y, "..."); Select(x, y, ThingKind.Sign); break;
                    case Tool.Person:
                        room.PlacePerson(new MapFile.NpcRecord { Name = "Someone", NpcType = "Youngster", X = x, Y = y, Dialog = new() { "..." } });
                        Select(x, y, ThingKind.Person);
                        break;
                    case Tool.Trigger: room.PlaceTrigger(x, y, "Script"); Select(x, y, ThingKind.Trigger); break;
                    case Tool.Select: Select(x, y, room.ThingsAt(x, y).Cast<ThingKind?>().FirstOrDefault()); break;
                }
            });
    }

    private void Select(int x, int y, ThingKind? kind)
    {
        selected = (x, y);
        selectedKind = kind;
        editing = -1;
    }

    private void OverlayInput(Vector2 mouse, float wheel)
    {
        if (overlay == null || mouse.X > Raylib.GetScreenWidth() - FieldsWidth) return;
        listScroll = Math.Max(0, listScroll - (int)wheel * 3);
        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && mouse.Y > Bar)
        {
            int row = (int)(mouse.Y - Bar - 8) / 22 + listScroll;
            if (row >= 0 && row < overlay.Rows().Count) { selectedRow = row; editing = -1; }
        }
    }

    private IReadOnlyList<EditorField> CurrentFields()
    {
        if (tab == Tab.Overlay && overlay != null)
        {
            var rows = overlay.Rows();
            return selectedRow >= 0 && selectedRow < rows.Count ? overlay.Fields(rows[selectedRow].Kind, rows[selectedRow].Key) : Array.Empty<EditorField>();
        }
        if (tab != Tab.Room) return Array.Empty<EditorField>();
        return selected is { } at && selectedKind is { } kind ? room.Fields(at.X, at.Y, kind) : room.RoomFields();
    }

    private void FieldsInput(Vector2 mouse)
    {
        int left = Raylib.GetScreenWidth() - FieldsWidth;
        if (mouse.X < left || !Raylib.IsMouseButtonPressed(MouseButton.Left)) return;
        int row = (int)(mouse.Y - Bar - 40) / 44;
        var fields = CurrentFields();
        if (row >= 0 && row < fields.Count)
        {
            editing = row;
            typed = fields[row].Value;
        }
    }

    private void Typing()
    {
        for (int c = Raylib.GetCharPressed(); c != 0; c = Raylib.GetCharPressed()) typed += char.ConvertFromUtf32(c);
        if ((Raylib.IsKeyPressed(KeyboardKey.Backspace) || Raylib.IsKeyPressedRepeat(KeyboardKey.Backspace)) && typed.Length > 0) typed = typed[..^1];
        if (Raylib.IsKeyPressed(KeyboardKey.Escape)) { editing = -1; return; }
        if (!Raylib.IsKeyPressed(KeyboardKey.Enter)) return;
        var fields = CurrentFields();
        if (editing >= fields.Count) { editing = -1; return; }
        string label = fields[editing].Label;
        Act(() =>
        {
            if (tab == Tab.Overlay && overlay != null)
            {
                var r = overlay.Rows()[selectedRow];
                overlay.SetField(r.Kind, r.Key, label, typed);
            }
            else if (selected is { } at && selectedKind is { } kind) room.SetField(at.X, at.Y, kind, label, typed);
            else room.SetRoomField(label, typed);
            status = $"{label} set";
        });
        editing = -1;
    }

    private void Save()
    {
        if (tab == Tab.Overlay && overlay != null && overlayPath != null)
        {
            findings = EditorChecks.Overlay(content.Value, overlay);
            overlay.Save(overlayPath);
            status = $"saved {overlayPath}: {findings.Count} finding{(findings.Count == 1 ? "" : "s")}";
            return;
        }
        findings = EditorChecks.Room(content.Value, room);
        string path = room.Save(dataFolder);
        status = $"saved {path}: {findings.Count} finding{(findings.Count == 1 ? "" : "s")}";
    }

    private List<string> Palette() => tool == Tool.Prop
        ? Enum.GetNames<PropType>().ToList()
        : TileCodes.AllTypes.Select(t => t.ToString()).ToList();

    // ------------------------------------------------------------------ drawing

    private void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Back);
        switch (tab)
        {
            case Tab.Room: DrawRoom(); DrawPalette(); break;
            case Tab.Overlay: DrawList(); break;
            case Tab.Preview: DrawPreview(); break;
        }
        DrawFields();
        DrawBars();
        Raylib.EndDrawing();
    }

    private void DrawBars()
    {
        int w = Raylib.GetScreenWidth(), h = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(0, 0, w, Bar, PanelColor);
        string dirty = room.Dirty || overlay?.Dirty == true ? " *" : "";
        Raylib.DrawText($"[{tab}] {room.File.Name}{(overlay != null ? " / " + overlay.Key : "")}{dirty}   tool {tool}: {(tool == Tool.Prop ? prop.ToString() : tile.ToString())}", 8, 6, FontSize, Ink);
        Raylib.DrawRectangle(0, h - FindingsHeight, w, FindingsHeight, PanelColor);
        Raylib.DrawText(status, 8, h - FindingsHeight + 6, FontSize, Accent);
        for (int i = 0; i < Math.Min(findings.Count, 5); i++)
            Raylib.DrawText($"{findings[i].Name}: {findings[i].What}", 8, h - FindingsHeight + 28 + i * 18, FontSize - 2, Bad);
    }

    private void DrawPalette()
    {
        Raylib.DrawRectangle(0, Bar, PaletteWidth, Raylib.GetScreenHeight() - Bar - FindingsHeight, PanelColor);
        var items = Palette();
        string chosen = tool == Tool.Prop ? prop.ToString() : tile.ToString();
        for (int i = paletteScroll; i < items.Count; i++)
        {
            int y = Bar + 8 + (i - paletteScroll) * 22;
            if (y > Raylib.GetScreenHeight() - FindingsHeight - 20) break;
            if (tool != Tool.Prop) Raylib.DrawRectangle(8, y, 16, 16, ColorOf(Enum.Parse<TileType>(items[i])));
            Raylib.DrawText(items[i], 30, y, FontSize, items[i] == chosen ? Accent : Ink);
        }
    }

    private void DrawRoom()
    {
        int s = (int)zoom;
        for (int y = 0; y < room.Height; y++)
            for (int x = 0; x < room.Width; x++)
            {
                int px = (int)pan.X + x * s, py = (int)pan.Y + y * s;
                Raylib.DrawRectangle(px, py, s, s, ColorOf(room.GroundAt(x, y)));
                if (room.OverheadAt(x, y) is { } top) Raylib.DrawRectangle(px + s / 4, py + s / 4, s / 2, s / 2, ColorOf(top));
                if (room.SolidAt(x, y)) Raylib.DrawLine(px, py + s, px + s, py, Hatch);
                Raylib.DrawRectangleLines(px, py, s, s, new Color(0, 0, 0, 40));
            }
        foreach (var p in room.File.Props)
            Raylib.DrawRectangleLinesEx(new Rectangle(pan.X + p.X * s + 2, pan.Y + p.Y * s + 2, p.Width * s - 4, p.Depth * s - 4), 2, Plans.PropColor);
        foreach (var t in room.File.Triggers ?? new())
            Raylib.DrawRectangleLinesEx(new Rectangle(pan.X + t.X * s, pan.Y + t.Y * s, t.Width * s, t.Depth * s), 2, Plans.TriggerColor);
        foreach (var w in room.File.Warps) Mark(w.SourceX, w.SourceY, "W", Plans.WarpColor);
        foreach (var sign in room.File.Signboards) Mark(sign.X, sign.Y, "S", Plans.SignColor);
        foreach (var n in room.File.Npcs) Mark(n.X, n.Y, n.Trainer != null ? "T" : "P", n.Trainer != null ? Plans.TrainerColor : Plans.PersonColor);
        if (selected is { } at)
            Raylib.DrawRectangleLinesEx(new Rectangle(pan.X + at.X * s - 2, pan.Y + at.Y * s - 2, s + 4, s + 4), 3, Accent);
    }

    private void Mark(int x, int y, string letter, Color color)
    {
        int s = (int)zoom, px = (int)pan.X + x * s, py = (int)pan.Y + y * s;
        Raylib.DrawCircle(px + s / 2, py + s / 2, s * 0.35f, color);
        Raylib.DrawText(letter, px + s / 2 - 4, py + s / 2 - 7, 14, Ink);
    }

    private void DrawList()
    {
        if (overlay == null) return;
        var rows = overlay.Rows();
        int width = Raylib.GetScreenWidth() - FieldsWidth;
        for (int i = listScroll; i < rows.Count; i++)
        {
            int y = Bar + 8 + (i - listScroll) * 22;
            if (y > Raylib.GetScreenHeight() - FindingsHeight - 20) break;
            if (i == selectedRow) Raylib.DrawRectangle(0, y - 2, width, 22, PanelColor);
            Raylib.DrawText(rows[i].Area, 8, y, FontSize, Dim);
            Raylib.DrawText(rows[i].Overlay, width / 2, y, FontSize, rows[i].Overlay.Length > 0 ? Ink : Dim);
        }
    }

    private void DrawPreview()
    {
        // The content check's plan of the map the game makes of the room, painted afresh when the room has changed
        string text = room.Text;
        if (text != previewOf)
        {
            previewOf = text;
            if (preview is { } old) Raylib.UnloadTexture(old);
            preview = null;
            try
            {
                var plan = Plans.OfRoom(room.ToMap());
                var image = plan.Picture.ToImage();
                preview = Raylib.LoadTextureFromImage(image);
                Raylib.UnloadImage(image);
            }
            catch (Exception e) when (e is InvalidDataException or ArgumentException)
            {
                status = "the game would not load the room: " + e.Message;
            }
        }
        if (preview is not { } texture) return;
        float scale = Math.Min((Raylib.GetScreenWidth() - FieldsWidth - 20f) / texture.Width, (Raylib.GetScreenHeight() - Bar - FindingsHeight - 20f) / texture.Height);
        Raylib.DrawTextureEx(texture, new Vector2(10, Bar + 10), 0, scale, Color.White);
    }

    private void DrawFields()
    {
        int left = Raylib.GetScreenWidth() - FieldsWidth;
        Raylib.DrawRectangle(left, Bar, FieldsWidth, Raylib.GetScreenHeight() - Bar - FindingsHeight, PanelColor);
        string title = tab == Tab.Overlay
            ? selectedRow >= 0 && overlay != null && selectedRow < overlay.Rows().Count ? overlay.Rows()[selectedRow].Key : "pick a row"
            : selected is { } at ? $"{selectedKind?.ToString() ?? "nothing"} at {at.X},{at.Y}" : "the room";
        Raylib.DrawText(title, left + 10, Bar + 10, FontSize + 2, Accent);
        var fields = CurrentFields();
        for (int i = 0; i < fields.Count; i++)
        {
            int y = Bar + 40 + i * 44;
            Raylib.DrawText(fields[i].Label, left + 10, y, FontSize - 2, Dim);
            string value = i == editing ? typed + "_" : fields[i].Value;
            Raylib.DrawText(value.Length > 44 ? value[..44] + "..." : value, left + 10, y + 18, FontSize, i == editing ? Accent : Ink);
        }
    }

    /// <summary>A tile's look as a flat colour: enough to tell floor from wall and water from grass while editing.</summary>
    public static Color ColorOf(TileType type) => type switch
    {
        TileType.Floor => new Color(196, 170, 128, 255),
        TileType.Wall => new Color(110, 104, 120, 255),
        TileType.Door => new Color(150, 96, 56, 255),
        TileType.Water => new Color(70, 130, 210, 255),
        TileType.Grass or TileType.TallGrass or TileType.FlowerGrass => new Color(96, 170, 80, 255),
        TileType.Tree or TileType.TreeTrunk => new Color(40, 100, 50, 255),
        TileType.Path or TileType.Dirt or TileType.Sand => new Color(210, 190, 140, 255),
        TileType.Paving => new Color(170, 170, 176, 255),
        TileType.Void => new Color(10, 10, 14, 255),
        TileType.Stairs => new Color(160, 130, 90, 255),
        TileType.PC => new Color(80, 200, 220, 255),
        TileType.Ice or TileType.Snow => new Color(220, 236, 248, 255),
        _ => new Color((byte)(60 + (int)type * 37 % 160), (byte)(60 + (int)type * 71 % 160), (byte)(60 + (int)type * 113 % 160), (byte)255)
    };
}
