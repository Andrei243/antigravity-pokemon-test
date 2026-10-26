using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The game as a tool reads it, in words (plan 16 · T17): the state, where the player stands and how, the text box's
/// line, a question's answers and its cursor, the screen open and its cursor, the team, the battlers, the money, the
/// story's revision and whichever flags and variables are asked for. A flat list of fields in a fixed order, each a
/// name (<c>party.0</c>, <c>flag.FLAG_HAS_POKEDEX</c>) and a value, so <c>expect</c> checks one by its name and two
/// snapshots are compared field by field. Read from the flow alone, so it is the same with a window or without.
/// </summary>
public sealed class StateSnapshot
{
    private readonly List<KeyValuePair<string, string>> fields = new();

    // The first words of the team's and the battle's fields (party.0, battle.menu), kept apart from the literals so no test takes them for keys of the words on screen
    private const string Team = "party", Fight = "battle";

    /// <summary>The file a snapshot is written to beside a shot.</summary>
    public const string FileName = "state.json";

    public IReadOnlyList<KeyValuePair<string, string>> Fields => fields;

    /// <summary>A field's value, or null when the snapshot hasn't it (a battle's fields outside a battle).</summary>
    public string? this[string field] => fields.FirstOrDefault(f => f.Key == field).Value;

    private void Add(string name, object? value)
    {
        string text = value switch
        {
            null => "",
            bool b => b ? "true" : "false",
            float f => f.ToString("0.##", CultureInfo.InvariantCulture),
            IFormattable n => n.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? ""
        };
        fields.Add(new(name, text));
    }

    /// <summary>The flow as it stands, with the flags and variables named (<c>FLAG_...</c>, <c>VAR_...</c>) beside the rest.</summary>
    public static StateSnapshot Of(GameFlow flow, IEnumerable<string>? named = null)
    {
        var s = new StateSnapshot();
        s.Add("state", flow.State);

        var map = flow.Map;
        var player = flow.Player;
        if (map != null && player != null)
        {
            s.Add("map", map.Name);
            s.Add("area", map.AreaAt(player.GridX, player.GridY)?.Key);
            s.Add("x", player.GridX);
            s.Add("y", player.GridY);
            s.Add("facing", player.Facing.ToString().ToLowerInvariant());
            s.Add("height", player.HeightOn(map));
            s.Add("travel", player.Mode);
        }

        var box = flow.Dialogue;
        if (box.IsActive)
        {
            s.Add("text", box.CurrentLine);
            s.Add("speaker", box.Speaker);
        }
        if (flow.Choice.IsOpen)
        {
            s.Add("question", string.Join(" | ", flow.Choice.Options));
            s.Add("question.cursor", flow.Choice.Cursor);
        }
        if (flow.StartMenu.IsActive) s.Add("menu.cursor", flow.StartMenu.SelectedIndex);
        if (ScreenCursor(flow) is { } cursor) s.Add("screen.cursor", cursor);

        s.Add($"{Team}.count", flow.Party.Count);
        for (int i = 0; i < flow.Party.Count; i++) s.Add($"{Team}.{i}", Describe(flow.Party.Members[i]));

        if (flow.State == GameState.Battle && flow.Battle is { } battle)
        {
            s.Add($"{Fight}.menu", battle.HUD.MenuState);
            s.Add($"{Fight}.message", battle.CurrentMessage);
            for (int i = 0; i < battle.PlayerSlots.Count; i++) s.Add($"{Fight}.mine.{i}", Describe(battle.PlayerSlots[i].Pokemon));
            for (int i = 0; i < battle.EnemySlots.Count; i++) s.Add($"{Fight}.foe.{i}", Describe(battle.EnemySlots[i].Pokemon));
            s.Add($"{Fight}.over", battle.IsBattleOver);
        }

        s.Add("money", flow.Money);
        s.Add("revision", flow.Story.Revision);
        foreach (string name in (named ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal))
        {
            if (name.StartsWith("FLAG_", StringComparison.Ordinal) || name.StartsWith("flag.", StringComparison.Ordinal))
            {
                string flag = name.StartsWith("flag.", StringComparison.Ordinal) ? name[5..] : name;
                s.Add("flag." + flag, flow.Story.Has(flag) ? "on" : "off");
            }
            else
            {
                string variable = name.StartsWith("var.", StringComparison.Ordinal) ? name[4..] : name;
                s.Add("var." + variable, flow.Story.Var(variable));
            }
        }
        return s;
    }

    /// <summary>A Pokémon in a field's words: <c>Turtwig 5 20/20 none</c> (an Egg is <c>Egg</c>, nobody is empty).</summary>
    public static string Describe(Pokemon? p) => p == null ? "" : p.IsEgg ? "Egg"
        : $"{p.Species.Name} {p.Level.ToString(CultureInfo.InvariantCulture)} {p.CurrentHP.ToString(CultureInfo.InvariantCulture)}/{p.MaxHP.ToString(CultureInfo.InvariantCulture)} {p.Status.ToString().ToLowerInvariant()}";

    // The open screen's cursor, for the screens with one a scenario steers
    private static int? ScreenCursor(GameFlow flow) => flow.State switch
    {
        GameState.PartyMenu => flow.PartyScreen.SelectedIndex,
        GameState.BagMenu => flow.BagScreen.SelectedIndex,
        GameState.PokedexMenu => flow.PokedexScreen.SelectedIndex,
        GameState.Shop => flow.ShopScreen.SelectedIndex,
        GameState.Options => flow.OptionsScreen.SelectedIndex,
        GameState.StarterSelect => flow.StarterSelectScreen.SelectedIndex,
        _ => null
    };

    // ------------------------------------------------------------------ the file

    public string ToJson()
    {
        var o = new JsonObject();
        foreach (var (name, value) in fields) o[name] = value;
        return o.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    }

    public static StateSnapshot FromJson(string json)
    {
        var s = new StateSnapshot();
        if (JsonNode.Parse(json) is JsonObject o)
            foreach (var (name, value) in o) s.fields.Add(new(name, value?.ToString() ?? ""));
        return s;
    }

    /// <summary>Every field whose value differs between two snapshots, or that one has and the other hasn't, in the first's order and then the second's.</summary>
    public static IEnumerable<(string Field, string? Before, string? After)> Changes(StateSnapshot before, StateSnapshot after)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (name, value) in before.fields)
        {
            seen.Add(name);
            string? now = after[name];
            if (now != value) yield return (name, value, now);
        }
        foreach (var (name, value) in after.fields)
            if (seen.Add(name)) yield return (name, null, value);
    }
}
