using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumTests.Golden;

/// <summary>
/// A battle's log as text (plan 24 · X2): an event to a line, a line said with what it shows indented under it
/// (<c>+</c>) and what lands at its impact (<c>!</c>), a place as <c>P0</c> or <c>E1</c>, a Pokémon as its side, its
/// place in its party and its species (<c>mine#2 Luxio</c>), a species, a move or an item by its name. Events carry
/// the core's own Pokémon by reference, which a record's own text can't tell apart; this can, and two logs are the
/// same battle when their text is the same.
/// <para>
/// An event's fields are written in the order they are declared, so a new event of the log needs nothing here.
/// </para>
/// </summary>
internal sealed class BattleText
{
    private readonly Dictionary<Pokemon, string> names = new(ReferenceEqualityComparer.Instance);

    private BattleText() { }

    /// <summary>The names of a battle's Pokémon: the player's (<c>mine</c>), a partner's, each trainer's (<c>foe</c>, <c>foe2</c>) and the wild ones.</summary>
    public static BattleText Of(BattleCore core)
    {
        var text = new BattleText();
        text.Name("mine", core.PlayerParty.Members);
        if (core.Partner != null) text.Name("partner", core.Partner.Party.Members);
        for (int t = 0; t < core.Trainers.Count; t++) text.Name(t == 0 ? "foe" : $"foe{t + 1}", core.Trainers[t].Party.Members);
        text.Name("wild", core.WildPokemon);
        return text;
    }

    /// <summary>A whole battle's log as text.</summary>
    public static List<string> Written(BattleCore core) => Of(core).Lines(core.Log);

    private void Name(string side, IEnumerable<Pokemon> party)
    {
        int i = 0;
        foreach (var p in party) names.TryAdd(p, $"{side}#{i++} {p.Species.Name}");
    }

    /// <summary>A Pokémon by its side, place and species; one of no side the battle began with is its species after a question mark.</summary>
    public string NameOf(Pokemon p) => names.TryGetValue(p, out var name) ? name : $"?{p.Species.Name}";

    public List<string> Lines(IEnumerable<BattleEvent> log)
    {
        var lines = new List<string>();
        foreach (var e in log)
        {
            if (e is Said said)
            {
                lines.Add($"say {said.Text}");
                lines.AddRange(said.Shows.Select(shown => $"  + {Event(shown)}"));
                lines.AddRange(said.OnImpact.Select(landed => $"  ! {Event(landed)}"));
            }
            else lines.Add(Event(e));
        }
        return lines;
    }

    /// <summary>An event by its type's name and its fields, <c>Name=value</c>, but a place, which is written bare.</summary>
    public string Event(BattleEvent e)
    {
        var fields = e.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(p => p.MetadataToken)
            .Select(p => p.Name == "Place" ? Value(p.GetValue(e)) : $"{p.Name}={Value(p.GetValue(e))}");
        return string.Join(" ", fields.Prepend(e.GetType().Name));
    }

    private string Value(object? value) => value switch
    {
        null => "none",
        Place place => PlaceOf(place),
        Pokemon p => NameOf(p),
        PokemonSpecies species => species.Name,
        MoveData move => move.Name,
        Move move => move.Name,
        ItemData item => item.Name,
        bool b => b ? "true" : "false",
        string s => s.Length == 0 ? "\"\"" : s,
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        IEnumerable many => "[" + string.Join(", ", many.Cast<object?>().Select(Value)) + "]",
        _ => value.ToString() ?? ""
    };

    /// <summary>The player's side's places are <c>P0</c> and <c>P1</c>, the other side's <c>E0</c> and <c>E1</c>.</summary>
    public static string PlaceOf(Place place) => $"{(place.Side == BattleSide.Player ? "P" : "E")}{place.Slot}";

    /// <summary>
    /// A Pokémon as it is: its species or form, level, gender, nature, ability, held item, moves and six stats, which
    /// is everything a battle reads of it that a team's text should hold.
    /// </summary>
    public static string Member(Pokemon p) =>
        $"{p.Form ?? p.Species.Name} L{p.Level} {p.Gender} {p.Nature} {p.AbilityName ?? "(no ability)"} @{p.HeldItem?.Name ?? "nothing"}" +
        $" | {string.Join(", ", p.Moves.Select(m => m.Name))}" +
        $" | HP {p.MaxHP} Atk {p.Attack} Def {p.Defense} SpA {p.SpAttack} SpD {p.SpDefense} Spe {p.Speed}";
}
