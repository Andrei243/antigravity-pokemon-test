using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// One Pokémon of a team built in the battle room (plan 13 · V13): what the team builder chose for it, by the data's
/// own names. Nothing of a save: a built Pokémon is made afresh (<see cref="BuiltTeams.Make"/>) for every battle it
/// fights, so a battle in the room changes nothing kept anywhere.
/// </summary>
public sealed class BuiltPokemon
{
    public string Species { get; set; } = "";

    /// <summary>A form kept outside battle (Rotom's appliances, a regional form), by Showdown's name; null for the species' own.</summary>
    public string? Form { get; set; }

    public int Level { get; set; } = BuiltTeams.DefaultLevel;
    public Nature Nature { get; set; } = Nature.Hardy;

    /// <summary>The ability by its name; null for the first the species has.</summary>
    public string? Ability { get; set; }

    /// <summary>The held item by its name; null for none.</summary>
    public string? Item { get; set; }

    public List<string> Moves { get; set; } = new();

    public BuiltPokemon Copy() => new() { Species = Species, Form = Form, Level = Level, Nature = Nature, Ability = Ability, Item = Item, Moves = Moves.ToList() };
}

/// <summary>A team of the battle room by name: one to six Pokémon, in order.</summary>
public sealed class BuiltTeam
{
    public string Name { get; set; } = "";
    public List<BuiltPokemon> Members { get; set; } = new();

    public BuiltTeam Copy() => new() { Name = Name, Members = Members.Select(m => m.Copy()).ToList() };
}

/// <summary>The file the battle room's teams are kept in, beside the saves (<c>teams.json</c>): the teams in the order made.</summary>
public sealed class BuiltTeamsFile
{
    public const int CurrentFormat = 1;

    public int Format { get; set; } = CurrentFormat;
    public List<BuiltTeam> Teams { get; set; } = new();
}

/// <summary>
/// The battle room's teams (plan 13 · V13; style guide, "Menu screens", "The battle room"): the rules a built team is held to,
/// how a built Pokémon becomes one that battles, and the file the teams are kept in. GPU-free. The legality check is
/// <see cref="Legality"/>'s, the one plan 14 · B11's tournament and plan 14 · B14's masters share: a move the species
/// (or its forebears) can know by the data, an ability it has, an item that can be held, a level of 1 to 100.
/// </summary>
public static class BuiltTeams
{
    public const string FileName = "teams.json";

    /// <summary>The most teams kept, and the longest name one may have (the keyboard's).</summary>
    public const int Most = 12, NameLength = 10;

    /// <summary>The level a new member starts at, and the highest a built Pokémon may have (the games' own, not the cap past 100).</summary>
    public const int DefaultLevel = 50, HighestLevel = 100;

    /// <summary>
    /// The forms a team may be built with: those kept outside battle. A Mega, a Primal, a Gigantamax form, a Mega
    /// Dynamax or a form taken in battle is reached in battle, never chosen.
    /// </summary>
    public static bool Choosable(PokemonForm form) => form.Kind is FormKind.Look or FormKind.Alternate or FormKind.Regional;

    /// <summary>The species the builder lists: the games' own 1,025, in National order.</summary>
    public static IReadOnlyList<PokemonSpecies> Species() => PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).ToList();

    /// <summary>The forms of a species the builder offers, in the data's order.</summary>
    public static IReadOnlyList<PokemonForm> FormsOf(PokemonSpecies species) => (species.Forms ?? new()).Where(Choosable).ToList();

    /// <summary>The abilities a built Pokémon may have: its species' (or form's) two and the hidden one.</summary>
    public static IReadOnlyList<string> AbilitiesOf(BuiltPokemon built)
    {
        if (PokemonDatabase.Get(built.Species) is not { } species) return Array.Empty<string>();
        var form = built.Form == null ? null : species.Form(built.Form);
        var list = (form?.Abilities ?? species.Abilities).ToList();
        if ((form?.HiddenAbility ?? species.HiddenAbility) is { Length: > 0 } hidden) list.Add(hidden);
        return list.Distinct().ToList();
    }

    /// <summary>Every move the built Pokémon may know, in the order of their display names.</summary>
    public static IReadOnlyList<string> MovesOf(BuiltPokemon built)
    {
        if (PokemonDatabase.Get(built.Species) is not { } species) return Array.Empty<string>();
        var moves = Legality.Learnable(species);
        if (built.Form != null && species.Form(built.Form)?.Learnset is { } own)
            foreach (var m in own) moves.Add(m.MoveName);
        // Only what the data has and the rules run (a move of the later games whose effect is still to come is left off)
        return moves.Where(m => MoveDatabase.Has(m) && MoveDatabase.Runs(m)).OrderBy(m => LocalNames.Move(m), StringComparer.CurrentCulture).ToList();
    }

    /// <summary>The items that can be held: those with a hold effect, in the order of their display names.</summary>
    public static IReadOnlyList<string> HoldableItems() =>
        ItemDatabase.GetAll().Where(i => !string.IsNullOrEmpty(i.HoldEffect)).Select(i => i.Name).Distinct()
            .OrderBy(n => LocalNames.Item(n), StringComparer.CurrentCulture).ToList();

    /// <summary>
    /// A new member of the species: level 50, its first ability, nothing held, the four latest moves its level-up
    /// learnset teaches it by that level (or the first it learns).
    /// </summary>
    public static BuiltPokemon Fresh(string speciesName)
    {
        var built = new BuiltPokemon { Species = speciesName };
        if (PokemonDatabase.Get(speciesName) is { } species)
        {
            built.Ability = species.Abilities.FirstOrDefault();
            var runs = species.Learnset.Where(m => MoveDatabase.Has(m.MoveName) && MoveDatabase.Runs(m.MoveName)).ToList();
            var learnt = runs.Where(m => m.Level <= built.Level).Select(m => m.MoveName).Distinct().ToList();
            built.Moves = (learnt.Count > 0 ? learnt.TakeLast(4) : runs.Select(m => m.MoveName).Take(1)).ToList();
        }
        return built;
    }

    /// <summary>
    /// Why a built Pokémon can't battle, as the key of its words and the values they name; null for one that can. One
    /// reason, the first found, in the order a player would fix them.
    /// </summary>
    public static (string Key, string Value)? WhyNot(BuiltPokemon built)
    {
        if (PokemonDatabase.Get(built.Species) is not { } species) return ("battleRoom.illegal.species", built.Species);
        if (built.Form != null && (species.Form(built.Form) is not { } form || !Choosable(form))) return ("battleRoom.illegal.form", built.Form);
        if (built.Level is < 1 or > HighestLevel) return ("battleRoom.illegal.level", built.Level.ToString());
        if (built.Moves.Count is < 1 or > 4) return ("battleRoom.illegal.moveCount", built.Moves.Count.ToString());
        if (built.Moves.Distinct(StringComparer.OrdinalIgnoreCase).Count() != built.Moves.Count) return ("battleRoom.illegal.twice", LocalNames.Move(built.Moves.GroupBy(m => m).First(g => g.Count() > 1).Key));
        var learnable = new HashSet<string>(MovesOf(built), StringComparer.OrdinalIgnoreCase);
        foreach (string move in built.Moves)
            if (!learnable.Contains(move)) return ("battleRoom.illegal.move", LocalNames.Move(move));
        var abilities = AbilitiesOf(built);
        if (built.Ability != null && !abilities.Contains(built.Ability)) return ("battleRoom.illegal.ability", LocalNames.Ability(built.Ability));
        if (built.Item != null && (ItemDatabase.Get(built.Item) is not { } item || string.IsNullOrEmpty(item.HoldEffect))) return ("battleRoom.illegal.item", built.Item);
        return null;
    }

    /// <summary>Why a team can't battle: none in it, more than six, or the first of its Pokémon that can't.</summary>
    public static (string Key, string Value)? WhyNot(BuiltTeam team)
    {
        if (team.Members.Count == 0) return ("battleRoom.illegal.empty", "");
        if (team.Members.Count > Party.MaxSize) return ("battleRoom.illegal.tooMany", team.Members.Count.ToString());
        foreach (var m in team.Members)
            if (WhyNot(m) is { } why) return why;
        return null;
    }

    public static bool IsLegal(BuiltTeam team) => WhyNot(team) == null;

    /// <summary>
    /// The Pokémon a built one battles as: every IV 31, no effort, the nature and ability chosen, holding its item,
    /// knowing its moves at full PP, never shiny. Its personality is drawn from a generator of its place in the team, so
    /// the same team makes the same Pokémon every time, on every machine.
    /// </summary>
    public static Pokemon Make(BuiltPokemon built, int place = 0)
    {
        var species = PokemonDatabase.Get(built.Species) ?? throw new InvalidOperationException($"No species called {built.Species}");
        var rng = new Random(1 + place);
        var p = built.Form != null ? Pokemon.InForm(species, built.Form, built.Level, rng) : new Pokemon(species, built.Level, rng, null, built.Nature);
        p.IsShiny = false;
        p.Nature = built.Nature;
        p.IvHP = p.IvAttack = p.IvDefense = p.IvSpAttack = p.IvSpDefense = p.IvSpeed = 31;
        if (built.Ability != null) p.AbilityName = built.Ability;
        p.HeldItem = built.Item == null ? null : ItemDatabase.Get(built.Item);
        p.Moves.Clear();
        foreach (string move in built.Moves) p.Moves.Add(MoveDatabase.Create(move));
        p.RecalculateStats();
        p.CurrentHP = p.MaxHP;
        return p;
    }

    /// <summary>The team as a party, each member made afresh.</summary>
    public static Party PartyOf(BuiltTeam team)
    {
        var party = new Party();
        for (int i = 0; i < team.Members.Count && i < Party.MaxSize; i++) party.Add(Make(team.Members[i], i));
        return party;
    }

    // ------------------------------------------------------------------ the file

    /// <summary>The teams kept in a file; none for a file that isn't there or can't be read (it is never written over then: <see cref="Write"/> keeps a copy).</summary>
    public static BuiltTeamsFile Read(string path)
    {
        try
        {
            if (!File.Exists(path)) return new BuiltTeamsFile();
            var file = JsonSerializer.Deserialize<BuiltTeamsFile>(File.ReadAllText(path), GameDataFiles.Json);
            if (file == null || file.Format > BuiltTeamsFile.CurrentFormat) return new BuiltTeamsFile();
            file.Teams = file.Teams.Where(t => t != null).Take(Most).ToList();
            return file;
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return new BuiltTeamsFile();
        }
    }

    /// <summary>Writes the teams: to a file beside it first, then over the old one, so a reader never finds half a file.</summary>
    public static void Write(string path, BuiltTeamsFile file)
    {
        if (Path.GetDirectoryName(path) is { Length: > 0 } folder) Directory.CreateDirectory(folder);
        string temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(file, GameDataFiles.Json));
        File.Move(temp, path, overwrite: true);
    }
}
