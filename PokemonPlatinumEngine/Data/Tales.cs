using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Data;

// The Tales (plan 46 · CP11; docs/mechanics/rulings.md, "The Tales"): Carpathia's primary gimmick, a basm told once a
// battle that changes the field for both sides for five turns. What each tale does is data, Data/tales.json, ours and read
// strictly: one effect each, laid where the field's rules already bite (a move's power in the damage formula, accuracy, a
// weather through the core's own SetWeather), so the core reads a tale's rule and never its name. No drawing or input.

/// <summary>A tale's bonus to the moves it favours: of these types, or with these flags (a sound move), by this much.</summary>
public sealed class TaleBoost
{
    /// <summary>The types whose moves it strengthens (any of them).</summary>
    public List<PokemonType> Types { get; set; } = new();

    /// <summary>The move flags it strengthens as well (a sound move of any type, the Ielele's).</summary>
    public List<MoveFlags> Flags { get; set; } = new();

    /// <summary>The power the moves it favours hit with, in hundredths of their own (130: a third as strong again).</summary>
    [DataRequired] public int Percent { get; set; } = 100;

    /// <summary>Whether a move of this type and flags is one the tale favours.</summary>
    public bool Favours(PokemonType type, MoveFlags flags) => Types.Contains(type) || Flags.Any(f => f != MoveFlags.None && (flags & f) != 0);
}

/// <summary>A tale's sky over the battle (a <c>StageLight</c> the screen eases in): a colour of 0..255, how far, how grey.</summary>
public sealed class TaleSky
{
    [DataRequired] public int[] Color { get; set; } = { 0, 0, 0 };
    [DataRequired] public float Amount { get; set; }
    public float Grey { get; set; }
}

/// <summary>A Pokémon bound to a tale (plan 46 · CP11, CP12's sculpts): a species, and the form it wakes into while its tale is told.</summary>
public sealed class TaleBinding
{
    [DataRequired] public string Species { get; set; } = "";
    [DataRequired] public string Form { get; set; } = "";
}

/// <summary>
/// One tale of <c>Data/tales.json</c>: an id of letters (its words are <c>basme.&lt;id&gt;.name</c>, <c>.told</c> and
/// <c>.story</c> in <c>ui.json</c>), its one rule, its sky, the name of its song layer and who wakes to it.
/// </summary>
public sealed class TaleRule
{
    [DataRequired] public string Id { get; set; } = "";

    /// <summary>The moves it favours hit harder, whoever uses them.</summary>
    public TaleBoost? Boost { get; set; }

    /// <summary>Every move's accuracy, in hundredths of its own (80 for the Vârcolac's eclipse); 100 changes nothing.</summary>
    public int Accuracy { get; set; } = 100;

    /// <summary>The weather the tale brings for its turns (the Solomonar's storm, Baba Dochia's cold), through the core's own SetWeather.</summary>
    public BattleWeather? Weather { get; set; }

    /// <summary>
    /// The sun can't set (the Zmeu's): whatever a battle reads of the night reads day while the tale is told. Nothing in a
    /// battle has a night form yet; the Dusk Ball is what reads it today (CP12 owes the rest).
    /// </summary>
    public bool NoNight { get; set; }

    public TaleSky? Sky { get; set; }

    /// <summary>The battle song's tale layer by name (data only: CP12 writes the layers, as the giant layer is written).</summary>
    public string? SongLayer { get; set; }

    /// <summary>Who wakes into a Tale Form while it is told on their trainer's side.</summary>
    public List<TaleBinding> Forms { get; set; } = new();

    /// <summary>The form a Pokémon of this species wakes into, or null.</summary>
    public string? FormFor(string species) => Forms.FirstOrDefault(f => f.Species == species)?.Form;
}

/// <summary>The file <c>Data/tales.json</c>.</summary>
public sealed class TalesFile
{
    /// <summary>The turn ends a tale holds for (five).</summary>
    [DataRequired] public int Turns { get; set; } = 5;

    public List<TaleRule> Tales { get; set; } = new();
}

/// <summary>
/// A table of tales (plan 46 · CP11): the game's, read from <c>Data/tales.json</c> (<see cref="Default"/>), or one a test
/// hands a battle (<c>CoreSetup.TaleTable</c>, never the shared species), so a binding can be tried on a species
/// that has none of its own.
/// </summary>
public sealed class TaleTable
{
    private static readonly Lazy<TaleTable> loaded = new(() => new TaleTable(GameDataFiles.Load<TalesFile>("tales.json", strict: true)));

    /// <summary>The game's tales.</summary>
    public static TaleTable Default => loaded.Value;

    /// <summary>The key item that lets the player tell the tales learned, by name (<c>items.json</c>).</summary>
    public const string BookItem = "Book of Tales";

    /// <summary>The flag a tale learned is kept by (no save field: the story's own flags).</summary>
    public static string FlagOf(string id) => "FLAG_TALE_" + id.ToUpperInvariant();

    public TalesFile File { get; }

    public TaleTable(TalesFile file) => File = file;

    public int Turns => File.Turns;

    public IReadOnlyList<TaleRule> All => File.Tales;

    public TaleRule? Get(string? id) => id == null ? null : All.FirstOrDefault(t => t.Id == id);

    /// <summary>The name a tale is shown by, in the language chosen.</summary>
    public static string NameOf(string id) => Text.Of($"basme.{id}.name");

    /// <summary>A tale's page in the Book: its story in a few lines.</summary>
    public static string StoryOf(string id) => Text.Of($"basme.{id}.story");

    /// <summary>What a tale does in a battle, a line for the Book.</summary>
    public static string RuleOf(string id) => Text.Of($"basme.{id}.rule");
}
