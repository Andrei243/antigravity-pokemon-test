using System.Globalization;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace RoomEditor;

/// <summary>
/// A person of our own (<see cref="MapFile.NpcRecord"/>) as fields of text, and a field written back: one set of
/// labels for a room's people and for an overlay's own <c>npcs</c>, so the two panels read alike (plan 16 · T9). A list
/// (the lines) is its items joined by <c>|</c>; a field left empty is left out of the file.
/// </summary>
public static class NpcFields
{
    /// <param name="withPlace">Whether the tile is a field too: an overlay's person is moved by typing it, a room's by the grid.</param>
    public static IReadOnlyList<EditorField> Of(MapFile.NpcRecord n, bool withPlace = false)
    {
        var fields = new List<EditorField>();
        if (withPlace) fields.AddRange(new EditorField[] { new("x", Number(n.X)), new("y", Number(n.Y)) });
        fields.AddRange(new EditorField[]
        {
            new("id", n.Id ?? ""), new("name", n.Name), new("npcType", n.NpcType), new("facing", n.Facing.ToString()),
            new("dialog", string.Join("|", n.Dialog ?? new())), new("script", n.Script ?? ""),
            new("hiddenBy", n.HiddenBy ?? ""), new("shownBy", n.ShownBy ?? ""), new("trainer", n.Trainer?.Id ?? ""),
            new("nurse", Flag(n.IsHealingNurse)), new("clerk", Flag(n.IsPokeMartClerk)), new("pc", Flag(n.IsPCTerminal)), new("mart", n.Mart ?? "")
        });
        if (withPlace)
            fields.AddRange(new EditorField[]
            {
                new("species", n.Species ?? ""), new("item", n.Item ?? ""), new("count", n.Count is { } c ? Number(c) : ""),
                new("movement", n.Movement ?? ""), new("level", n.Level is { } l ? l.ToString(CultureInfo.InvariantCulture) : "")
            });
        return fields;
    }

    /// <summary>Writes one field; a label the record hasn't, or a value the field can't take, throws.</summary>
    public static void Write(MapFile.NpcRecord n, string label, string value)
    {
        switch (label)
        {
            case "x": n.X = ParseNumber(value); break;
            case "y": n.Y = ParseNumber(value); break;
            case "id": n.Id = Optional(value); break;
            case "name": n.Name = value; break;
            case "npcType": n.NpcType = value; break;
            case "facing": n.Facing = Enum.Parse<Direction>(value, true); break;
            case "dialog": n.Dialog = value.Length == 0 ? null : value.Split('|').ToList(); break;
            case "script": n.Script = Optional(value); break;
            case "hiddenBy": n.HiddenBy = Optional(value); break;
            case "shownBy": n.ShownBy = Optional(value); break;
            case "trainer":
                n.Trainer = value.Length == 0 ? null : n.Trainer ?? new MapFile.TrainerRecord();
                if (n.Trainer != null) n.Trainer.Id = value;
                break;
            case "nurse": n.IsHealingNurse = OptionalFlag(value); break;
            case "clerk": n.IsPokeMartClerk = OptionalFlag(value); break;
            case "pc": n.IsPCTerminal = OptionalFlag(value); break;
            case "mart": n.Mart = Optional(value); break;
            case "species": n.Species = Optional(value); break;
            case "item": n.Item = Optional(value); break;
            case "count": n.Count = value.Length == 0 ? null : ParseNumber(value); break;
            case "movement": n.Movement = Optional(value); break;
            case "level": n.Level = value.Length == 0 ? null : float.Parse(value, CultureInfo.InvariantCulture); break;
            default: throw new ArgumentException($"a person has no field '{label}'");
        }
    }

    internal static string Flag(bool? value) => value == true ? "true" : "";
    internal static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    internal static string? Optional(string value) => value.Length == 0 ? null : value;
    internal static bool? OptionalFlag(string value) => value.Length == 0 || value.Equals("false", StringComparison.OrdinalIgnoreCase) ? null : bool.Parse(value);
    internal static int ParseNumber(string value) => int.Parse(value, CultureInfo.InvariantCulture);
}
