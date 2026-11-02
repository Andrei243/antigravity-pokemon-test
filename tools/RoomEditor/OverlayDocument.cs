using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace RoomEditor;

/// <summary>What a row of the overlay's list is: one of the area's objects, warps or triggers.</summary>
public enum OverlayRowKind { Person, Door, Trigger }

/// <summary>
/// A row of the overlay's list: one of the area file's objects, warps or triggers (by its id or number) with what the
/// overlay says of it beside it, a line each for the list.
/// </summary>
public sealed record OverlayRow(OverlayRowKind Kind, string Key, string Area, string Overlay);

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
            rows.Add(new OverlayRow(OverlayRowKind.Person, o.Id, here, said));
        }
        for (int i = 0; i < Area.Warps.Count; i++)
        {
            var w = Area.Warps[i];
            string key = i.ToString(CultureInfo.InvariantCulture);
            string said = overlay.Doors?.FirstOrDefault(d => d.Warp == i) is { } door
                ? $"to {door.Map} {door.X},{door.Y}{(door.OpenedBy != null ? ", opened by " + door.OpenedBy : "")}"
                : overlay.Locked?.Contains(i) == true ? "locked" : "";
            rows.Add(new OverlayRow(OverlayRowKind.Door, key, $"warp {i} at {w.X},{w.Z} to {w.To} {w.ToWarp}", said));
        }
        for (int i = 0; i < Area.Triggers.Count; i++)
        {
            var t = Area.Triggers[i];
            string said = overlay.Triggers?.FirstOrDefault(x => x.Trigger == i)?.Script ?? "";
            rows.Add(new OverlayRow(OverlayRowKind.Trigger, i.ToString(CultureInfo.InvariantCulture),
                $"trigger {i} at {t.X},{t.Z} ({t.Width}x{t.Depth}) when {t.Variable} is {t.Value}", said));
        }
        return rows;
    }

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
                    new("openedBy", d?.OpenedBy ?? ""), new("locked", overlay.Locked?.Contains(warp) == true ? "true" : "")
                };
            case OverlayRowKind.Trigger:
                int number = int.Parse(key, CultureInfo.InvariantCulture);
                return new EditorField[] { new("script", overlay.Triggers?.FirstOrDefault(x => x.Trigger == number)?.Script ?? "") };
        }
        return Array.Empty<EditorField>();
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
