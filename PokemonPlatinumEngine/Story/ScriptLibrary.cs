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

    /// <summary>
    /// The script a place runs behind the warp's fade as the player comes to it, before anything of it is shown
    /// (plan 21 · H8, part d; Emerald's <c>MAP_SCRIPT_ON_TRANSITION</c>): it may set flags and variables and nothing else,
    /// and what it sets is laid down before the picture opens (Shoal Cave's tide, whose chunks the story swaps).
    /// </summary>
    public const string OnTransition = "OnTransition";

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

    /// <summary>
    /// Who comes and goes with the clock, as they stand before their place's arrival script has run (plan 06 · R14a):
    /// called by <see cref="ChapterThree"/>, and run for a save from before.
    /// </summary>
    public const string TimedPeople = "common.TimedPeople";
    public const string CampaignOver = "common.CampaignOver";

    /// <summary>Who the fourth chapter (plan 02 · S7, "Hearthome and Solaceon") keeps out of sight until their scenes:
    /// called by <see cref="ChapterThree"/>, and run for a save from before.</summary>
    public const string ChapterFour = "common.ChapterFour";

    /// <summary>Who the fifth chapter (plan 02 · S8, "Veilstone and Pastoria") keeps out of sight until their scenes:
    /// called by <see cref="ChapterFour"/>, and run for a save from before.</summary>
    public const string ChapterFive = "common.ChapterFive";

    /// <summary>Who the sixth chapter (plan 02 · S9, "Celestic and the sea") keeps out of sight until their scenes:
    /// called by <see cref="ChapterFive"/>, and run for a save from before.</summary>
    public const string ChapterSix = "common.ChapterSix";

    /// <summary>Who the seventh chapter (plan 02 · S10, "Iron Island and the lakes") keeps out of sight until their
    /// scenes: called by <see cref="ChapterSix"/>, and run for a save from before.</summary>
    public const string ChapterSeven = "common.ChapterSeven";

    /// <summary>Who the eighth chapter (plan 02 · S11, "Snowpoint and Galactic HQ") keeps out of sight until their
    /// scenes: called by <see cref="ChapterSeven"/>, and run for a save from before.</summary>
    public const string ChapterEight = "common.ChapterEight";

    /// <summary>The script that hides the people of Team Galactic's headquarters until their scenes (plan 02 · S11,
    /// part b): called by <see cref="ChapterEight"/>, and run for a save from before.</summary>
    public const string Headquarters = "common.Headquarters";

    /// <summary>The script that hides whoever the ninth chapter brings on later (plan 02 · S12, Spear Pillar and the
    /// Distortion World): called by <see cref="Headquarters"/>, and run for a save from before.</summary>
    public const string ChapterNine = "common.ChapterNine";

    /// <summary>The script that hides whoever the tenth chapter brings on later (plan 02 · S13, Sunyshore): called by
    /// <see cref="ChapterNine"/>, and run for a save from before.</summary>
    public const string ChapterTen = "common.ChapterTen";

    /// <summary>The script that hides whoever the eleventh chapter brings on later (plan 02 · S14, the Pokémon League):
    /// called by <see cref="ChapterTen"/>, and run for a save from before.</summary>
    public const string ChapterEleven = "common.ChapterEleven";

    /// <summary>The script that hides whoever the time after the Hall of Fame brings on later (plan 02 · S15): called
    /// by <see cref="ChapterEleven"/>, and run for a save from before.</summary>
    public const string ChapterTwelve = "common.ChapterTwelve";

    /// <summary>
    /// The script of everything a victory in the Hall of Fame sets (plan 02 · S14; the original's
    /// <c>SetHallOfFameVictoryFlagsAndVars</c>): called by the room's scene, and run with no screen for a save that
    /// entered the Hall of Fame before the scene was written.
    /// </summary>
    public const string HallOfFameVictory = "PokemonLeagueHallOfFame.Victory";

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
        .Where(i => i.Op is Op.SetFlag or Op.ClearFlag or Op.AddContact or Op.Tide && !i.Own)
        // (a number registered is kept by a flag of its own, plan 20 · J3; Shoal Cave's tide by Emerald's, plan 21 · H8, part d)
        .Select(i => i.Op switch
        {
            Op.AddContact => Models.Pokegear.ContactFlag(i.Name),
            Op.Tide => Overworld.ShoalTide.Flag,
            _ => i.Name
        }).ToHashSet(StringComparer.Ordinal);

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
                || i.Op == Op.Partner && i.Option
                // (whoever the camera is told to keep on, plan 09 · L4)
                || i.Op == Op.Camera && i.Camera == CameraMove.Follow
                // (whoever a set piece shakes or has flicker, plan 09 · L12)
                || i.Op is Op.Shake or Op.Flicker)
            {
                if (i.Name is not ("player" or "self" or "pair")) yield return (i.Name, i.Line);
                if (i.Op is Op.Face or Op.Battle && i.Other is not ("" or "player" or "self" or "pair")) yield return (i.Other, i.Line);
                if (i.Op == Op.Battle && !i.PartnerById && i.Partner is not ("" or "self" or "partner")) yield return (i.Partner, i.Line);
            }
        }
    }

    /// <summary>The scenes the scripts mark (plan 12 · Q20), each by its key: the file it is written in and its name.</summary>
    public IReadOnlySet<string> Scenes => All.SelectMany(s => s.Code.Where(i => i.Op == Op.Scene).Select(i => SceneSkip.Key(s.File, i.Name)))
        .ToHashSet(StringComparer.Ordinal);

    /// <summary>Whether a script begins a scene, itself or in anything it calls.</summary>
    public bool BeginsAScene(Script script) => BeginsAScene(script, new HashSet<string>(StringComparer.Ordinal));

    private bool BeginsAScene(Script script, HashSet<string> looked)
    {
        if (!looked.Add(script.FullName)) return false;
        foreach (var i in script.Everything())
        {
            if (i.Op == Op.Scene) return true;
            if (i.Op == Op.Call && Find(i.Name, script.File) is { } called && BeginsAScene(called, looked)) return true;
        }
        return false;
    }

    /// <summary>
    /// What is wrong with the scripts beyond their grammar, a line each: a script called that doesn't exist, an
    /// item, a species, a move, a song or a sound nobody has heard of, a map that isn't one; and a scene left open,
    /// an <c>endscene</c> with no scene, a scene inside a scene, two scenes of one name in a file (plan 12 · Q20).
    /// Empty when all is well.
    /// </summary>
    public List<string> Problems(Func<string, bool>? mapExists = null, Func<string, bool>? songExists = null, Func<string, bool>? soundExists = null)
    {
        var problems = new List<string>();
        // The scenes of each file by name (plan 12 · Q20): a scene's key in a save is its file and its name
        var scenes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var script in All.OrderBy(s => s.FullName, StringComparer.Ordinal))
        {
            void Wrong(Instruction at, string what) => problems.Add($"{script.File}.txt({at.Line}): {what}.");

            // A scene is begun and ended in one script, read from top to bottom: each scene has an endscene below it
            // before any other scene begins (and may have more, one to each way out), an endscene has a scene above
            // it, and nothing called from inside a scene begins one of its own
            Instruction? open = null;
            bool begun = false;
            foreach (var i in script.Code)
            {
                switch (i.Op)
                {
                    case Op.Scene:
                        if (open != null) Wrong(i, $"scene '{i.Name}' begins inside scene '{open.Name}', which has no endscene before it");
                        if (!scenes.Add(SceneSkip.Key(script.File, i.Name))) Wrong(i, $"{script.File}.txt has two scenes called '{i.Name}'");
                        open = i;
                        begun = true;
                        break;
                    case Op.EndScene:
                        if (!begun) Wrong(i, $"an endscene with no scene above it in script {script.Name}");
                        open = null;
                        break;
                    case Op.Call when open != null && Find(i.Name, script.File) is { } called && BeginsAScene(called, new HashSet<string>(StringComparer.Ordinal)):
                        Wrong(i, $"scene '{open.Name}' calls '{i.Name}', which begins a scene of its own: a scene inside a scene");
                        break;
                }
            }
            if (open != null) Wrong(open, $"scene '{open.Name}' is left open: script {script.Name} has no endscene below it");

            foreach (var i in script.Everything())
            {
                // The text box's markup (plan 09 · L3): a code written wrong, or a sound that isn't one
                foreach (string line in i.Lines)
                {
                    if (!line.Contains('{')) continue;
                    foreach (string what in UI.Kit.RichText.Problems(line)) Wrong(i, $"the text box's markup is wrong: {what}");
                    foreach (var e in UI.Kit.RichText.Parse(line).Events)
                        if (e.Kind == UI.Kit.TextEventKind.Sound && soundExists != null && !soundExists(e.Sound)) Wrong(i, $"there is no sound '{e.Sound}'");
                }
                switch (i.Op)
                {
                    case Op.Call when Find(i.Name, script.File) == null:
                        Wrong(i, $"script {script.Name} calls '{i.Name}', which doesn't exist");
                        break;
                    case Op.Speaker when i.VoiceId.Length > 0 && !Data.VoiceCast.Default.HasVoice(i.VoiceId):
                        Wrong(i, $"speaker '{i.VoiceId}' is no voice of Data/voice/cast.json");
                        break;
                    case Op.Give or Op.Find or Op.AddItem or Op.Take when !i.Own && ItemDatabase.Get(i.Name) == null:
                        Wrong(i, $"there is no item '{i.Name}'");
                        break;
                    case Op.Cry when !i.Own && PokemonDatabase.Get(i.Name) == null && PokemonDatabase.SpeciesOfForm(i.Name) == null:
                        Wrong(i, $"there is no species or form '{i.Name}'");
                        break;
                    case Op.WildBattle when PokemonDatabase.Get(i.Name) == null && PokemonDatabase.SpeciesOfForm(i.Name) == null:
                        Wrong(i, $"there is no species or form '{i.Name}'");
                        break;
                    case Op.ChoosePokemon when i.Name.Length > 0 && PokemonDatabase.Get(i.Name) == null:
                        Wrong(i, $"there is no species '{i.Name}'");
                        break;
                    case Op.Puzzle when !Models.AlphPuzzle.Pictures.ContainsKey(i.Name):
                        Wrong(i, $"there is no panel '{i.Name}' in the Ruins of Alph");
                        break;
                    case Op.AddContact when Models.PhoneBook.Get(i.Name) == null:
                        Wrong(i, $"the phone book has no contact '{i.Name}'");
                        break;
                    case Op.GivePokemon or Op.CatchingLesson or Op.GiveEgg or Op.Seen when PokemonDatabase.Get(i.Name) == null:
                        Wrong(i, $"there is no species '{i.Name}'");
                        break;
                    case Op.GivePokemon when i.Other.Length > 0 && ItemDatabase.Get(i.Other) == null:
                        Wrong(i, $"there is no item '{i.Other}'");
                        break;
                    case Op.GivePokemon when i.Move.Length > 0 && !MoveDatabase.Has(i.Move):
                        Wrong(i, $"there is no move '{i.Move}'");
                        break;
                    case Op.Team when i.Name == "size" && !i.Option && PokemonDatabase.Get(i.Other) == null:
                        Wrong(i, $"there is no species '{i.Other}'");
                        break;
                    case Op.Team when i.Name == "holds" && ItemDatabase.Get(i.Other) == null:
                        Wrong(i, $"there is no item '{i.Other}'");
                        break;
                    case Op.Tutor when i.Name == "once" && !Models.MoveTutors.Kanto.ContainsKey(i.Other):
                        Wrong(i, $"no tutor of Kanto teaches '{i.Other}'");
                        break;
                    case Op.Tutor when i.Name == "moves" && !Models.MoveTutors.Places.Contains(i.Other):
                        Wrong(i, $"no shard tutor lives at '{i.Other}'");
                        break;
                    case Op.GiveAccessory when SuperContestDatabase.Accessory(i.Name) < 0:
                        Wrong(i, $"there is no accessory '{i.Name}'");
                        break;
                    case Op.Battle when i.PartnerById && TrainerDatabase.Get(i.Partner) == null:
                        Wrong(i, $"there is no trainer '{i.Partner}'");
                        break;
                    case Op.Defeat when TrainerDatabase.Get(i.Name) == null:
                        Wrong(i, $"there is no trainer '{i.Name}'");
                        break;
                    case Op.Subway when i.Name == "foe" && TrainerDatabase.Get(i.Other) == null:
                        Wrong(i, $"there is no trainer '{i.Other}'");
                        break;
                    case Op.Battle when i.AsTrainer.Length > 0 && TrainerDatabase.Get(i.AsTrainer) == null:
                        Wrong(i, $"there is no trainer '{i.AsTrainer}'");
                        break;
                    case Op.Shop when i.Name.Length > 0 && !MartDatabase.Specialties.ContainsKey(i.Name):
                        Wrong(i, $"there is no counter '{i.Name}'");
                        break;
                    case Op.Wardrobe when i.Name.Length > 0 && !ClothingDatabase.Boutiques.ContainsKey(i.Name):
                        Wrong(i, $"there is no boutique '{i.Name}'");
                        break;
                    case Op.Warp when mapExists != null && !mapExists(i.Name):
                        Wrong(i, $"there is no map '{i.Name}'");
                        break;
                    case Op.Travel when i.Name == "to" && mapExists != null && !mapExists(i.Other):
                        Wrong(i, $"there is no map '{i.Other}'");
                        break;
                    case Op.Music when i.Name.Length > 0 && songExists != null && !songExists(i.Name):
                        Wrong(i, $"there is no song '{i.Name}'");
                        break;
                    case Op.Sound when soundExists != null && !soundExists(i.Name):
                        Wrong(i, $"there is no sound '{i.Name}'");
                        break;
                    case Op.GiveBadge when NoBadge(script.File, i.Name) is { } why:
                        Wrong(i, why);
                        break;
                }

                if (i.Condition is not { } c) continue;
                switch (c.Query)
                {
                    case Query.Item when ItemDatabase.Get(c.Name) == null:
                        Wrong(i, $"there is no item '{c.Name}'");
                        break;
                    // "has var VAR_X" names no species of its own (plan 19 · K9, part c)
                    case Query.Has or Query.Starter when c.Name.Length > 0 && PokemonDatabase.Get(c.Name) == null:
                        Wrong(i, $"there is no species '{c.Name}'");
                        break;
                    case Query.Granted when Overworld.FieldMoveRules.Of(c.Name) == null:
                        Wrong(i, $"'{c.Name}' is no field move");
                        break;
                    case Query.Knows when MoveDatabase.Get(c.Name) == null:
                        Wrong(i, $"there is no move '{c.Name}'");
                        break;
                    case Query.Badge when NoBadge(script.File, c.Name) is { } why:
                        Wrong(i, why);
                        break;
                }
            }
        }
        return problems;
    }

    /// <summary>
    /// What is wrong with a badge a script names (plan 18 · W2), or null: a file of a region's own (a room of its maps, an
    /// area of its world) names one of that region's badges, and a file every region shares (<c>common</c>) one that some
    /// region has.
    /// </summary>
    private static string? NoBadge(string file, string badge)
    {
        if (RegionOfFile(file) is { } region)
            return StoryState.FindBadge(region.Id, badge) >= 0 ? null : $"{region.Name} has no badge '{badge}'";
        return RegionDatabase.All.Any(r => StoryState.FindBadge(r.Id, badge) >= 0) ? null : $"no region has a badge '{badge}'";
    }

    /// <summary>
    /// The region a script file belongs to: the region that lists the map it is named for, or whose world has the area it
    /// is named for (<c>world/&lt;folder&gt;/areas/&lt;key&gt;.json</c>); null for a file of no one region (<c>common</c>).
    /// </summary>
    public static Region? RegionOfFile(string file) =>
        RegionDatabase.RegionOfMap(file)
        ?? RegionDatabase.All.FirstOrDefault(r => r.File != null && System.IO.File.Exists(GameDataFiles.PathOf($"world/{r.Folder}/areas/{file}.json")));
}
