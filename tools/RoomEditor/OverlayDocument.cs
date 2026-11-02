using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace RoomEditor;

/// <summary>
/// What a row of the overlay's list is: one of the area's objects, warps, triggers or signs, which the overlay says
/// something of (by the area file's id or number), or one of the overlay's own things, which it places itself (by its
/// place in the overlay's list): a person (<c>npcs</c>), a prop (<c>props</c>), a way out (<c>exits</c>), a trigger
/// with tiles of its own (<c>triggers</c> with no number) and a tile read (<c>readAt</c>).
/// </summary>
public enum OverlayRowKind { Person, Door, Trigger, Sign, OwnPerson, OwnProp, Exit, OwnTrigger, ReadAt }

/// <summary>
/// A row of the overlay's list: one of the area file's objects, warps or triggers (by its id or number), or one of the
/// overlay's own things (by its place in its list), with what the overlay says of it beside it, a line each for the
/// list, and the tile it stands on (<see cref="Width"/> by <see cref="Depth"/> from there for a trigger).
/// </summary>
public sealed record OverlayRow(OverlayRowKind Kind, string Key, string Area, string Overlay, int X = -1, int Z = -1, int Width = 1, int Depth = 1)
{
    /// <summary>Whether the row stands on a tile.</summary>
    public bool Covers(int x, int z) => x >= X && z >= Z && x < X + Width && z < Z + Depth;

    /// <summary>Whether the row is one of the overlay's own things, which the overlay can add and take away.</summary>
    public bool IsOwn => Kind is OverlayRowKind.OwnPerson or OverlayRowKind.OwnProp or OverlayRowKind.Exit or OverlayRowKind.OwnTrigger or OverlayRowKind.ReadAt;
}

/// <summary>
/// An area's overlay being edited (plan 16 · T9): the area file's objects, warps and triggers listed with the
/// overlay's entries beside them (who a person is and what they say, their script and trainer's lines; where a door
/// leads or that it is locked; a trigger's script), and the overlay written back. It is written back a value at a
/// time (<see cref="JsonSplice"/>), so the comments and the layout written by hand stay; and the text it writes is a
/// <see cref="WorldOverlayFile"/> the game reads.
/// </summary>
public sealed class OverlayDocument
{
    public string Key { get; }
    public WorldAreaFile Area { get; }

    /// <summary>The overlay's text as it stands.</summary>
    public string Text { get; private set; }

    /// <summary>The overlay as the game reads the text.</summary>
    public WorldOverlayFile Overlay => GameDataFiles.Deserialize<WorldOverlayFile>(Text);

    private readonly List<string> undo = new(), redo = new();
    private string saved;

    /// <param name="text">The overlay's file, or null for an area that has none yet.</param>
    public OverlayDocument(string key, WorldAreaFile area, string? text)
    {
        Key = key;
        Area = area;
        Text = text ?? JsonSplice.Serialize(new { area = key }) + "\n";
        saved = text ?? "";
        GameDataFiles.Deserialize<WorldOverlayFile>(Text);
    }

    public bool Dirty => Text != saved;
    public bool CanUndo => undo.Count > 0;
    public bool CanRedo => redo.Count > 0;

    public void Undo()
    {
        if (undo.Count == 0) return;
        redo.Add(Text);
        Text = undo[^1];
        undo.RemoveAt(undo.Count - 1);
    }

    public void Redo()
    {
        if (redo.Count == 0) return;
        undo.Add(Text);
        Text = redo[^1];
        redo.RemoveAt(redo.Count - 1);
    }

    private void Change(string path, string? json)
    {
        string next = JsonSplice.Set(Text, path, json);
        if (next == Text) return;
        GameDataFiles.Deserialize<WorldOverlayFile>(next);
        undo.Add(Text);
        redo.Clear();
        Text = next;
    }

    // ------------------------------------------------------------------ the list

    /// <summary>Every object, warp and trigger of the area, each with what the overlay says of it.</summary>
    public IReadOnlyList<OverlayRow> Rows()
    {
        var overlay = Overlay;
        var rows = new List<OverlayRow>();
        foreach (var o in Area.Objects)
        {
            string here = $"{o.Id} ({o.Looks} at {o.X},{o.Z}{(o.Trainer != null ? ", trainer" : "")}{(o.Item != null ? ", " + o.Item : "")})";
            string said = overlay.People?.GetValueOrDefault(o.Id) is { } p
                ? $"{p.Name}{(p.NpcType != null ? " as " + p.NpcType : "")}{(p.Script != null ? ", runs " + p.Script : "")}{(p.Dialog is { Count: > 0 } d ? $", {d.Count} line{(d.Count == 1 ? "" : "s")}" : "")}{(p.Trainer != null ? ", a trainer's lines" : "")}"
                : overlay.HeldBack?.Contains(o.Id) == true ? "held back" : "";
            rows.Add(new OverlayRow(OverlayRowKind.Person, o.Id, here, said, o.X, o.Z));
        }
        for (int i = 0; i < Area.Warps.Count; i++)
        {
            var w = Area.Warps[i];
            string key = i.ToString(CultureInfo.InvariantCulture);
            string said = overlay.Doors?.FirstOrDefault(d => d.Warp == i) is { } door
                ? $"to {door.Map} {door.X},{door.Y}{(door.OpenedBy != null ? ", opened by " + door.OpenedBy : "")}"
                : overlay.Locked?.Contains(i) == true ? "locked" : "";
            if (overlay.Switches?.FirstOrDefault(s => s.Warp == i) is { } switched)
                said += (said.Length > 0 ? ", " : "") + (switched.Until != null ? "until " + switched.Until : "") + (switched.From != null ? " from " + switched.From : "");
            rows.Add(new OverlayRow(OverlayRowKind.Door, key, $"warp {i} at {w.X},{w.Z} to {w.To} {w.ToWarp}", said.Trim(), w.X, w.Z));
        }
        for (int i = 0; i < Area.Triggers.Count; i++)
        {
            var t = Area.Triggers[i];
            string said = overlay.Triggers?.FirstOrDefault(x => x.Trigger == i)?.Script ?? "";
            rows.Add(new OverlayRow(OverlayRowKind.Trigger, i.ToString(CultureInfo.InvariantCulture),
                $"trigger {i} at {t.X},{t.Z} ({t.Width}x{t.Depth}) when {t.Variable} is {t.Value}", said, t.X, t.Z, t.Width, t.Depth));
        }
        foreach (var (key, x, z, what) in SignKeys())
        {
            string said = string.Join(", ", new[]
            {
                overlay.Signs?.GetValueOrDefault(key) is { } text ? $"\"{Short(text)}\"" : null,
                overlay.SignScripts?.GetValueOrDefault(key) is { } runs ? "runs " + runs : null,
                overlay.Read?.GetValueOrDefault(key) is { } read ? "read as " + read : null
            }.Where(s => s != null));
            rows.Add(new OverlayRow(OverlayRowKind.Sign, key, $"{what} at {x},{z}", said, x, z));
        }

        // The overlay's own things, by their place in its lists
        for (int i = 0; i < (overlay.Npcs?.Count ?? 0); i++)
        {
            var n = overlay.Npcs![i];
            rows.Add(new OverlayRow(OverlayRowKind.OwnPerson, Number(i), $"our person {i} at {n.X},{n.Y}",
                $"{(n.Id != null ? n.Id + " " : "")}\"{n.Name}\" {n.NpcType}{(n.Script != null ? ", runs " + n.Script : "")}", n.X, n.Y));
        }
        for (int i = 0; i < (overlay.Props?.Count ?? 0); i++)
        {
            var p = overlay.Props![i];
            rows.Add(new OverlayRow(OverlayRowKind.OwnProp, Number(i), $"our prop {i} at {p.X},{p.Y}", $"{p.Type} {p.Width}x{p.Depth}", p.X, p.Y, p.Width, p.Depth));
        }
        for (int i = 0; i < (overlay.Exits?.Count ?? 0); i++)
        {
            var e = overlay.Exits![i];
            rows.Add(new OverlayRow(OverlayRowKind.Exit, Number(i), $"our way out {i} at {e.X},{e.Z}", $"to {e.Map} {e.ToX},{e.ToY} facing {e.Facing}", e.X, e.Z));
        }
        for (int i = 0; i < (overlay.Triggers?.Count ?? 0); i++)
        {
            var t = overlay.Triggers![i];
            if (t.Trigger != null || t.X is not { } tx || t.Z is not { } tz) continue;
            rows.Add(new OverlayRow(OverlayRowKind.OwnTrigger, Number(i), $"our trigger at {tx},{tz} ({t.Width}x{t.Depth})",
                t.Script + (t.Variable != null ? $" when {t.Variable} is {t.Value}" : ""), tx, tz, t.Width, t.Depth));
        }
        for (int i = 0; i < (overlay.ReadAt?.Count ?? 0); i++)
        {
            var r = overlay.ReadAt![i];
            rows.Add(new OverlayRow(OverlayRowKind.ReadAt, Number(i), $"our tile read {i} at {r.X},{r.Z}", "runs " + r.Script, r.X, r.Z));
        }
        return rows;
    }

    /// <summary>The rows standing on a tile, the overlay's own things first: what a pick of the tile finds.</summary>
    public IReadOnlyList<OverlayRow> RowsAt(int x, int z) =>
        Rows().Where(r => r.Covers(x, z)).OrderBy(r => r.IsOwn ? 0 : 1).ToList();

    /// <summary>
    /// The area's signs the overlay may give words: a signpost or a mailbox of the area's objects, by its id, and a sign
    /// the area file reads by a script (a GBA game's signpost, a pillar), by that script, as the world reads them.
    /// </summary>
    private IEnumerable<(string Key, int X, int Z, string What)> SignKeys()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var o in Area.Objects)
            if (SignLooks.Contains(o.Looks) && seen.Add(o.Id)) yield return (o.Id, o.X, o.Z, $"{o.Looks} {o.Id}");
        foreach (var s in Area.Signs)
            if (s.Type is not (AreaSign.HiddenItem or AreaSign.SecretBase) && s.Script.Length > 0 && seen.Add(s.Script))
                yield return (s.Script, s.X, s.Z, $"sign {s.Script}");
    }

    /// <summary>The looks of the area's objects that are read as signs (the world's own list, <c>World.IsSignpost</c>, and the mailbox).</summary>
    private static readonly HashSet<string> SignLooks = new(StringComparer.Ordinal)
        { "map_signpost", "arrow_signpost", "signboard", "trainer_tips_signpost", "gym_signpost", "mailbox" };

    private bool IsAreaSignKey(string key) => Area.Signs.Any(s => s.Script == key) && Area.Objects.All(o => o.Id != key);

    private static string Short(string text) => text.Length > 40 ? text[..40] + "..." : text;
    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    private static int Index(string key) => int.Parse(key, CultureInfo.InvariantCulture);

    // ------------------------------------------------------------------ fields

    /// <summary>The fields the overlay gives a row: what the editor's panel shows for it and lets be typed. Lines are joined by <c>|</c>.</summary>
    public IReadOnlyList<EditorField> Fields(OverlayRowKind kind, string key)
    {
        var overlay = Overlay;
        switch (kind)
        {
            case OverlayRowKind.Person:
                var p = overlay.People?.GetValueOrDefault(key);
                return new EditorField[]
                {
                    new("name", p?.Name ?? ""), new("npcType", p?.NpcType ?? ""), new("dialog", string.Join("|", p?.Dialog ?? new())),
                    new("script", p?.Script ?? ""), new("hiddenBy", p?.HiddenBy ?? ""), new("shownBy", p?.ShownBy ?? ""),
                    new("trainer.dialogueBefore", p?.Trainer?.DialogueBefore ?? ""), new("trainer.dialogueAfter", p?.Trainer?.DialogueAfter ?? ""),
                    new("still", p?.Still == true ? "true" : "")
                };
            case OverlayRowKind.Door:
                int warp = int.Parse(key, CultureInfo.InvariantCulture);
                var d = overlay.Doors?.FirstOrDefault(x => x.Warp == warp);
                return new EditorField[]
                {
                    new("map", d?.Map ?? ""), new("x", d != null ? d.X.ToString(CultureInfo.InvariantCulture) : ""),
                    new("y", d != null ? d.Y.ToString(CultureInfo.InvariantCulture) : ""), new("facing", d?.Facing.ToString() ?? ""),
                    new("openedBy", d?.OpenedBy ?? ""), new("locked", overlay.Locked?.Contains(warp) == true ? "true" : ""),
                    new("until", overlay.Switches?.FirstOrDefault(s => s.Warp == warp)?.Until ?? ""),
                    new("from", overlay.Switches?.FirstOrDefault(s => s.Warp == warp)?.From ?? "")
                };
            case OverlayRowKind.Trigger:
                int number = int.Parse(key, CultureInfo.InvariantCulture);
                return new EditorField[] { new("script", overlay.Triggers?.FirstOrDefault(x => x.Trigger == number)?.Script ?? "") };
            case OverlayRowKind.Sign:
                var sign = new List<EditorField> { new("text", overlay.Signs?.GetValueOrDefault(key) ?? ""), new("script", overlay.SignScripts?.GetValueOrDefault(key) ?? "") };
                if (IsAreaSignKey(key)) sign.Add(new("read", overlay.Read?.GetValueOrDefault(key) ?? ""));
                return sign;
            case OverlayRowKind.OwnPerson when overlay.Npcs?.ElementAtOrDefault(Index(key)) is { } n:
                return NpcFields.Of(n, withPlace: true);
            case OverlayRowKind.OwnProp when overlay.Props?.ElementAtOrDefault(Index(key)) is { } prop:
                return new EditorField[] { new("type", prop.Type.ToString()), new("x", Number(prop.X)), new("y", Number(prop.Y)), new("width", Number(prop.Width)), new("depth", Number(prop.Depth)) };
            case OverlayRowKind.Exit when overlay.Exits?.ElementAtOrDefault(Index(key)) is { } e:
                return new EditorField[]
                {
                    new("x", Number(e.X)), new("z", Number(e.Z)), new("map", e.Map), new("toX", Number(e.ToX)), new("toY", Number(e.ToY)), new("facing", e.Facing.ToString())
                };
            case OverlayRowKind.OwnTrigger when overlay.Triggers?.ElementAtOrDefault(Index(key)) is { } t:
                return new EditorField[]
                {
                    new("script", t.Script), new("x", t.X is { } tx ? Number(tx) : ""), new("z", t.Z is { } tz ? Number(tz) : ""),
                    new("width", Number(t.Width)), new("depth", Number(t.Depth)), new("variable", t.Variable ?? ""), new("value", t.Variable != null ? Number(t.Value) : "")
                };
            case OverlayRowKind.ReadAt when overlay.ReadAt?.ElementAtOrDefault(Index(key)) is { } r:
                return new EditorField[] { new("x", Number(r.X)), new("z", Number(r.Z)), new("script", r.Script) };
        }
        return Array.Empty<EditorField>();
    }

    // ------------------------------------------------------------------ the overlay's own things

    /// <summary>Someone of the overlay's own on a tile of the area's matrix (<c>npcs</c>), a line said; their key in the list.</summary>
    public string AddPerson(int x, int z, string name = "Someone", string npcType = "Youngster") =>
        Append("npcs", Overlay.Npcs?.Count, new MapFile.NpcRecord { Name = name, NpcType = npcType, X = x, Y = z, Dialog = new() { "..." } });

    /// <summary>Street furniture of the overlay's own (<c>props</c>).</summary>
    public string AddProp(PropType type, int x, int z, int width = 1, int depth = 1) =>
        Append("props", Overlay.Props?.Count, new MapFile.PropRecord { Type = type, X = x, Y = z, Width = width, Depth = depth });

    /// <summary>A way out of the overlay's own (<c>exits</c>) onto a tile of a map; a map left empty is for the panel to fill.</summary>
    public string AddExit(int x, int z, string map = "", int toX = 0, int toY = 0, Direction facing = Direction.Up) =>
        Append("exits", Overlay.Exits?.Count, new OverlayExit { X = x, Z = z, Map = map, ToX = toX, ToY = toY, Facing = facing });

    /// <summary>A trigger with tiles of its own (<c>triggers</c> with no number), firing every time until a variable is given.</summary>
    public string AddTrigger(int x, int z, string script = "Script", int width = 1, int depth = 1) =>
        Append("triggers", Overlay.Triggers?.Count, new OverlayTrigger { X = x, Z = z, Width = width, Depth = depth, Script = script });

    /// <summary>A tile read by facing it that the original keeps nothing on (<c>readAt</c>).</summary>
    public string AddReadAt(int x, int z, string script = "Script") =>
        Append("readAt", Overlay.ReadAt?.Count, new OverlayTileScript { X = x, Z = z, Script = script });

    private string Append<T>(string list, int? count, T record)
    {
        Change(list + ".+", JsonSplice.Serialize(record));
        return Number(count ?? 0);
    }

    /// <summary>The overlay's own thing of a row taken out of its list, as one step. A row of the area's isn't the overlay's to take away.</summary>
    public void Remove(OverlayRowKind kind, string key)
    {
        string list = kind switch
        {
            OverlayRowKind.OwnPerson => "npcs", OverlayRowKind.OwnProp => "props", OverlayRowKind.Exit => "exits",
            OverlayRowKind.OwnTrigger => "triggers", OverlayRowKind.ReadAt => "readAt",
            _ => throw new ArgumentException("only the overlay's own things are taken away: clear an area's row's fields instead")
        };
        Change($"{list}.{Index(key)}", null);
    }

    /// <summary>
    /// Sets one field of a row as one step. A person given no name and nothing else leaves the overlay; a door
    /// given no map leaves the doors; a trigger given no script leaves the triggers.
    /// </summary>
    public void SetField(OverlayRowKind kind, string key, string label, string value)
    {
        var overlay = Overlay;
        switch (kind)
        {
            case OverlayRowKind.Person: SetPerson(overlay, key, label, value); break;
            case OverlayRowKind.Door: SetDoor(overlay, int.Parse(key, CultureInfo.InvariantCulture), label, value); break;
            case OverlayRowKind.Trigger:
                if (label != "script") throw new ArgumentException($"a trigger has no field '{label}'");
                int number = int.Parse(key, CultureInfo.InvariantCulture);
                int at = overlay.Triggers?.FindIndex(t => t.Trigger == number) ?? -1;
                var trigger = at >= 0 ? overlay.Triggers![at] : new OverlayTrigger { Trigger = number };
                trigger.Script = value;
                Change(at >= 0 ? $"triggers.{at}" : "triggers.+", value.Length == 0 ? null : JsonSplice.Serialize(trigger));
                break;
            case OverlayRowKind.Sign: SetSign(key, label, value); break;
            case OverlayRowKind.OwnPerson:
                Own(overlay.Npcs, "npcs", key, n => NpcFields.Write(n, label, value));
                break;
            case OverlayRowKind.OwnProp:
                Own(overlay.Props, "props", key, p =>
                {
                    switch (label)
                    {
                        case "type": p.Type = Enum.Parse<PropType>(value, true); break;
                        case "x": p.X = Index(value); break;
                        case "y": p.Y = Index(value); break;
                        case "width": p.Width = Index(value); break;
                        case "depth": p.Depth = Index(value); break;
                        default: throw new ArgumentException($"a prop has no field '{label}'");
                    }
                });
                break;
            case OverlayRowKind.Exit:
                Own(overlay.Exits, "exits", key, e =>
                {
                    switch (label)
                    {
                        case "x": e.X = Index(value); break;
                        case "z": e.Z = Index(value); break;
                        case "map": e.Map = value; break;
                        case "toX": e.ToX = Index(value); break;
                        case "toY": e.ToY = Index(value); break;
                        case "facing": e.Facing = Enum.Parse<Direction>(value, true); break;
                        default: throw new ArgumentException($"a way out has no field '{label}'");
                    }
                });
                break;
            case OverlayRowKind.OwnTrigger:
                Own(overlay.Triggers, "triggers", key, t =>
                {
                    switch (label)
                    {
                        case "script": t.Script = value; break;
                        case "x": t.X = Index(value); break;
                        case "z": t.Z = Index(value); break;
                        case "width": t.Width = Index(value); break;
                        case "depth": t.Depth = Index(value); break;
                        case "variable": t.Variable = value.Length == 0 ? null : value; if (t.Variable == null) t.Value = 0; break;
                        case "value": t.Value = value.Length == 0 ? 0 : Index(value); break;
                        default: throw new ArgumentException($"a trigger has no field '{label}'");
                    }
                });
                break;
            case OverlayRowKind.ReadAt:
                Own(overlay.ReadAt, "readAt", key, r =>
                {
                    switch (label)
                    {
                        case "x": r.X = Index(value); break;
                        case "z": r.Z = Index(value); break;
                        case "script": r.Script = value; break;
                        default: throw new ArgumentException($"a tile read has no field '{label}'");
                    }
                });
                break;
        }
    }

    /// <summary>One of the overlay's own records changed and written back where it stands.</summary>
    private void Own<T>(List<T>? list, string name, string key, Action<T> change) where T : class
    {
        int index = Index(key);
        if (list == null || index < 0 || index >= list.Count) throw new ArgumentException($"the overlay has no {name} {key}");
        var record = list[index];
        change(record);
        Change($"{name}.{index}", JsonSplice.Serialize(record));
    }

    private void SetSign(string key, string label, string value)
    {
        if (SignKeys().All(s => s.Key != key)) throw new ArgumentException($"the area has no sign '{key}'");
        string? json = value.Length == 0 ? null : JsonSplice.Serialize(value);
        switch (label)
        {
            case "text": Change($"signs.{key}", json); break;
            case "script": Change($"signScripts.{key}", json); break;
            case "read" when IsAreaSignKey(key): Change($"read.{key}", json); break;
            default: throw new ArgumentException($"a sign has no field '{label}'");
        }
    }

    private void SetPerson(WorldOverlayFile overlay, string id, string label, string value)
    {
        if (Area.Objects.All(o => o.Id != id)) throw new ArgumentException($"the area has no object '{id}'");
        var person = overlay.People?.GetValueOrDefault(id) ?? new OverlayPerson();
        string? optional = value.Length == 0 ? null : value;
        switch (label)
        {
            case "name": person.Name = value; break;
            case "npcType": person.NpcType = optional; break;
            case "dialog": person.Dialog = optional?.Split('|').ToList(); break;
            case "script": person.Script = optional; break;
            case "hiddenBy": person.HiddenBy = optional; break;
            case "shownBy": person.ShownBy = optional; break;
            case "trainer.dialogueBefore": (person.Trainer ??= new()).DialogueBefore = value; break;
            case "trainer.dialogueAfter": (person.Trainer ??= new()).DialogueAfter = value; break;
            case "still": person.Still = value.Equals("true", StringComparison.OrdinalIgnoreCase); break;
            default: throw new ArgumentException($"a person has no field '{label}'");
        }
        if (person.Trainer is { DialogueBefore.Length: 0, DialogueAfter.Length: 0, Id: null }) person.Trainer = null;
        bool empty = Clean(JsonSplice.Serialize(person)) == Clean(JsonSplice.Serialize(new OverlayPerson()));
        Change($"people.{id}", empty ? null : Clean(JsonSplice.Serialize(person)));
    }

    private void SetDoor(WorldOverlayFile overlay, int warp, string label, string value)
    {
        if (warp < 0 || warp >= Area.Warps.Count) throw new ArgumentException($"the area has no warp {warp}");
        if (label is "until" or "from")
        {
            // The story's switch of a warp (plan 02 · S10): there until a flag, or from one; neither, and it is no switch
            int where = overlay.Switches?.FindIndex(s => s.Warp == warp) ?? -1;
            var switched = where >= 0 ? overlay.Switches![where] : new OverlaySwitch { Warp = warp };
            if (label == "until") switched.Until = value.Length == 0 ? null : value;
            else switched.From = value.Length == 0 ? null : value;
            bool none = switched.Until == null && switched.From == null;
            if (where >= 0 || !none) Change(where >= 0 ? $"switches.{where}" : "switches.+", none ? null : JsonSplice.Serialize(switched));
            return;
        }
        if (label == "locked")
        {
            bool locked = value.Equals("true", StringComparison.OrdinalIgnoreCase);
            int at = overlay.Locked?.IndexOf(warp) ?? -1;
            if (locked && at < 0) Change("locked.+", warp.ToString(CultureInfo.InvariantCulture));
            else if (!locked && at >= 0) Change($"locked.{at}", null);
            return;
        }
        int index = overlay.Doors?.FindIndex(d => d.Warp == warp) ?? -1;
        var door = index >= 0 ? overlay.Doors![index] : new OverlayDoor { Warp = warp };
        switch (label)
        {
            case "map": door.Map = value; break;
            case "x": door.X = int.Parse(value, CultureInfo.InvariantCulture); break;
            case "y": door.Y = int.Parse(value, CultureInfo.InvariantCulture); break;
            case "facing": door.Facing = Enum.Parse<Direction>(value, true); break;
            case "openedBy": door.OpenedBy = value.Length == 0 ? null : value; break;
            default: throw new ArgumentException($"a door has no field '{label}'");
        }
        Change(index >= 0 ? $"doors.{index}" : "doors.+", door.Map.Length == 0 ? null : JsonSplice.Serialize(door));
    }

    /// <summary>A record with its <c>false</c> flags left out, as a hand writes it (the format reads a flag left out as false).</summary>
    private static string Clean(string json)
    {
        var node = JsonNode.Parse(json)!.AsObject();
        foreach (var name in node.Where(m => m.Value is JsonValue v && v.GetValueKind() == JsonValueKind.False).Select(m => m.Key).ToList())
            node.Remove(name);
        return node.ToJsonString(GameDataFiles.Json);
    }

    // ------------------------------------------------------------------ saving

    /// <summary>Writes the overlay to a path and remembers it as saved.</summary>
    public void Save(string path)
    {
        GameDataFiles.Deserialize<WorldOverlayFile>(Text);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, Text);
        saved = Text;
    }
}
