using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// Every script of the game, read from <c>Data/scripts/&lt;file&gt;.txt</c>. A file is named for where its scripts
/// belong: an area's key (<c>jubilife_city</c>), a hand-made map's name (<c>PokemonCenter</c>), or <c>common</c>
/// for what every place shares (the nurse, the clerk, a trainer's challenge). A script is found by its name in the
/// file of the place first and in <c>common</c> second, or anywhere by <c>file.Name</c>.
/// </summary>
public sealed class ScriptLibrary
{
    public const string Folder = "scripts";
    public const string Common = "common";

    /// <summary>The script a place runs when the player arrives in it, if its file has one.</summary>
    public const string OnEnter = "OnEnter";

    /// <summary>The script that sets what every new game starts with; it may say and show nothing.</summary>
    public const string NewGame = "common.NewGame";

    /// <summary>
    /// What the first chapter leaves behind once played (plan 02 · S4): run, with no screen, for a save from before
    /// it was written. Like <see cref="NewGame"/>, it may only set flags and variables.
    /// </summary>
    public const string OpeningDone = "common.OpeningDone";

    /// <summary>
    /// Who the second chapter keeps out of sight until their scenes (plan 02 · S5): called by <see cref="NewGame"/>,
    /// and run for a save from before the chapter was written. It may only set flags and variables.
    /// </summary>
    public const string ChapterTwo = "common.ChapterTwo";

    /// <summary>
    /// Who the third chapter keeps out of sight until their scenes (plan 02 · S6), and what Turnback Cave keeps
    /// hidden (plan 01 · M8): called by <see cref="ChapterTwo"/>, and run for a save from before.
    /// </summary>
    public const string ChapterThree = "common.ChapterThree";

    private readonly Dictionary<string, Script> scripts = new(StringComparer.Ordinal);

    public IReadOnlyCollection<Script> All => scripts.Values;

    /// <summary>The files read, without their ending.</summary>
    public IReadOnlyCollection<string> Files { get; }

    private ScriptLibrary(IEnumerable<(string File, string Text)> sources)
    {
        var files = new List<string>();
        foreach (var (file, text) in sources)
        {
            files.Add(file);
            foreach (var script in ScriptParser.Parse(file, text)) scripts[script.FullName] = script;
        }
        Files = files;
    }

    public static ScriptLibrary FromSources(params (string File, string Text)[] sources) => new(sources);

    /// <summary>Reads every script file of a folder; a file that can't be read as scripts stops the game with its line named.</summary>
    public static ScriptLibrary Load(string? folder = null)
    {
        folder ??= GameDataFiles.PathOf(Folder);
        if (!Directory.Exists(folder)) throw new DirectoryNotFoundException($"Script folder not found: {folder}");
        try
        {
            return new ScriptLibrary(Directory.GetFiles(folder, "*.txt").OrderBy(p => p, StringComparer.Ordinal)
                .Select(p => (Path.GetFileNameWithoutExtension(p), File.ReadAllText(p))));
        }
        catch (ScriptException e)
        {
            throw new InvalidDataException($"Script file is malformed: {e.Message}", e);
        }
    }

    private static ScriptLibrary? shared;

    /// <summary>The game's own scripts, read once.</summary>
    public static ScriptLibrary Default => shared ??= Load();

    /// <summary>
    /// The script a name means where it is used: <c>file.Name</c> exactly, or a bare name in the file of the
    /// place (<paramref name="file"/>) and then among the common ones.
    /// </summary>
    public Script? Find(string name, string? file = null)
    {
        if (name.Contains('.')) return scripts.GetValueOrDefault(name);
        if (file != null && scripts.TryGetValue(file + "." + name, out var local)) return local;
        return scripts.GetValueOrDefault(Common + "." + name);
    }

    /// <summary>A script of one file by its bare name, or null: for what a place has or hasn't (<see cref="OnEnter"/>).</summary>
    public Script? In(string file, string name) => scripts.GetValueOrDefault(file + "." + name);

    // ------------------------------------------------------------------ what the scripts name

    /// <summary>The flags any script sets or clears.</summary>
    public IReadOnlySet<string> FlagsWritten => All.SelectMany(s => s.Everything())
        .Where(i => i.Op is Op.SetFlag or Op.ClearFlag && !i.Own).Select(i => i.Name).ToHashSet(StringComparer.Ordinal);

    /// <summary>The flags any script asks about.</summary>
    public IReadOnlySet<string> FlagsRead => All.SelectMany(s => s.Everything())
        .Where(i => i.Condition?.Query == Query.Flag).Select(i => i.Condition!.Name).ToHashSet(StringComparer.Ordinal);

    /// <summary>The variables any script sets or adds to.</summary>
    public IReadOnlySet<string> VariablesWritten => All.SelectMany(s => s.Everything())
        .Where(i => i.Op is Op.SetVar or Op.AddVar).Select(i => i.Name).ToHashSet(StringComparer.Ordinal);

    /// <summary>The variables any script asks about, the game's own left out.</summary>
    public IReadOnlySet<string> VariablesRead => All.SelectMany(s => s.Everything())
        .Where(i => i.Condition?.Query == Query.Var)
        .SelectMany(i => i.Condition!.Other is { } other ? new[] { i.Condition.Name, other } : new[] { i.Condition.Name })
        .Where(v => !ScriptParser.BuiltInVariables.Contains(v)).ToHashSet(StringComparer.Ordinal);

    /// <summary>The people a script names besides <c>player</c> and <c>self</c>, each with the line that names them.</summary>
    public static IEnumerable<(string Who, int Line)> PeopleNamed(Script script)
    {
        foreach (var i in script.Everything())
        {
            // "pair" is the second of two trainers who came together, and a battle "with partner" is beside whoever
            // travels with the player: the game's own, named by nobody's file (plan 02 · S6)
            if (i.Op is Op.Battle or Op.Face or Op.Walk or Op.Move or Op.Emote or Op.Show or Op.Hide or Op.Place
                || i.Op == Op.Partner && i.Option)
            {
                if (i.Name is not ("player" or "self" or "pair")) yield return (i.Name, i.Line);
                if (i.Op is Op.Face or Op.Battle && i.Other is not ("" or "player" or "self" or "pair")) yield return (i.Other, i.Line);
                if (i.Op == Op.Battle && !i.PartnerById && i.Partner is not ("" or "self" or "partner")) yield return (i.Partner, i.Line);
            }
        }
    }

    /// <summary>
    /// What is wrong with the scripts beyond their grammar, a line each: a script called that doesn't exist, an
    /// item, a species, a move, a song or a sound nobody has heard of, a map that isn't one. Empty when all is well.
    /// </summary>
    public List<string> Problems(Func<string, bool>? mapExists = null, Func<string, bool>? songExists = null, Func<string, bool>? soundExists = null)
    {
        var problems = new List<string>();
        foreach (var script in All.OrderBy(s => s.FullName, StringComparer.Ordinal))
        {
            void Wrong(Instruction at, string what) => problems.Add($"{script.File}.txt({at.Line}): {what}.");

            foreach (var i in script.Everything())
            {
                switch (i.Op)
                {
                    case Op.Call when Find(i.Name, script.File) == null:
                        Wrong(i, $"script {script.Name} calls '{i.Name}', which doesn't exist");
                        break;
                    case Op.Give or Op.Find or Op.AddItem or Op.Take when !i.Own && ItemDatabase.Get(i.Name) == null:
                        Wrong(i, $"there is no item '{i.Name}'");
                        break;
                    case Op.Cry when PokemonDatabase.Get(i.Name) == null && PokemonDatabase.SpeciesOfForm(i.Name) == null:
                        Wrong(i, $"there is no species or form '{i.Name}'");
                        break;
                    case Op.GivePokemon or Op.WildBattle or Op.CatchingLesson when PokemonDatabase.Get(i.Name) == null:
                        Wrong(i, $"there is no species '{i.Name}'");
                        break;
                    case Op.Battle when i.PartnerById && TrainerDatabase.Get(i.Partner) == null:
                        Wrong(i, $"there is no trainer '{i.Partner}'");
                        break;
                    case Op.Defeat when TrainerDatabase.Get(i.Name) == null:
                        Wrong(i, $"there is no trainer '{i.Name}'");
                        break;
                    case Op.Battle when i.AsTrainer.Length > 0 && TrainerDatabase.Get(i.AsTrainer) == null:
                        Wrong(i, $"there is no trainer '{i.AsTrainer}'");
                        break;
                    case Op.Shop when i.Name.Length > 0 && !MartDatabase.Specialties.ContainsKey(i.Name):
                        Wrong(i, $"there is no counter '{i.Name}'");
                        break;
                    case Op.Warp when mapExists != null && !mapExists(i.Name):
                        Wrong(i, $"there is no map '{i.Name}'");
                        break;
                    case Op.Music when i.Name.Length > 0 && songExists != null && !songExists(i.Name):
                        Wrong(i, $"there is no song '{i.Name}'");
                        break;
                    case Op.Sound when soundExists != null && !soundExists(i.Name):
                        Wrong(i, $"there is no sound '{i.Name}'");
                        break;
                }

                if (i.Condition is not { } c) continue;
                switch (c.Query)
                {
                    case Query.Item when ItemDatabase.Get(c.Name) == null:
                        Wrong(i, $"there is no item '{c.Name}'");
                        break;
                    case Query.Has or Query.Starter when PokemonDatabase.Get(c.Name) == null:
                        Wrong(i, $"there is no species '{c.Name}'");
                        break;
                    case Query.Knows when MoveDatabase.Get(c.Name) == null:
                        Wrong(i, $"there is no move '{c.Name}'");
                        break;
                }
            }
        }
        return problems;
    }
}
