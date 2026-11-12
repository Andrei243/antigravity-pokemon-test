using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Studio;

/// <summary>Who says a beat's or an ending's lines: the co-star lent to the player, the film's foe, or the player.</summary>
public enum StudioSpeaker { CoStar, Foe, Player }

/// <summary>
/// When a beat comes (plan 42 · PS4): every condition set must hold on the battle as a menu opens. A share is of the
/// lead's HP in hundredths; a count of Pokémon down is of that side's whole team.
/// </summary>
public sealed class StudioWhen
{
    /// <summary>The turn reached (the battle's own count: 0 as it opens, 1 after the first turn).</summary>
    public int? Turn { get; set; }
    /// <summary>The foe's Pokémon on the field is under this share of its HP.</summary>
    public int? FoeHpUnder { get; set; }
    /// <summary>The player's Pokémon on the field is under this share of its HP.</summary>
    public int? PlayerHpUnder { get; set; }
    /// <summary>At least this many of the foe's Pokémon are down.</summary>
    public int? FoeDown { get; set; }
    /// <summary>At least this many of the player's Pokémon are down.</summary>
    public int? PlayerDown { get; set; }
    /// <summary>The player has used a move of this type.</summary>
    public PokemonType? TypeUsed { get; set; }
    /// <summary>This weather is out.</summary>
    public BattleWeather? Weather { get; set; }
}

/// <summary>A line the player may say at a beat: the script's (its <see cref="Script"/>) or an ad-lib.</summary>
public sealed class StudioChoice
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    public bool Script { get; set; }
}

/// <summary>
/// A beat of a film: when it comes, who speaks, its lines (said in turn) and, where the player answers, the lines to
/// choose among (two or three). Each beat comes once, at the first menu its condition holds.
/// </summary>
public sealed class StudioBeat
{
    public string Id { get; set; } = "";
    public StudioWhen When { get; set; } = new();
    public StudioSpeaker Speaker { get; set; }
    public List<string> Lines { get; set; } = new();
    public List<StudioChoice> Choices { get; set; } = new();
}

/// <summary>
/// An ending: the first of a film's endings whose conditions hold on the take is the take's. <see cref="Result"/> is
/// <c>won</c>, <c>lost</c> (lost, run from, or out of turns) or null for either.
/// </summary>
public sealed class StudioEnding
{
    public string Id { get; set; } = "";
    public string? Result { get; set; }
    /// <summary>Won (or lost) by this turn at the latest: the battle's count of turns done as it ended.</summary>
    public int? ByTurn { get; set; }
    public List<string> Beats { get; set; } = new();
    public List<string> Chosen { get; set; } = new();
    public StudioSpeaker Speaker { get; set; }
    public List<string> Lines { get; set; } = new();
    public int Stars { get; set; }
}

/// <summary>A take written for an ending: the player's answers in the battle and the lines picked, in order.</summary>
public sealed class StudioTakeScript
{
    public string Ending { get; set; } = "";
    public List<PuzzleAnswer> Answers { get; set; } = new();
    public List<string> Picks { get; set; } = new();
}

/// <summary>
/// A film of the studio (plan 42 · PS4; <c>Data/studio-films/*.json</c>, ours): its genre and the rank it opens at, the
/// co-star lent (a name and a look of <c>characters.json</c>), the battle in a battle puzzle's shape (the cast the
/// player is lent, the foe and their moves turn by turn, the fixed rolls, the turns the take may run), its beats and its
/// endings, and a take written for each ending. Its title is <c>ui.json</c>'s <c>studio.film.&lt;id&gt;.title</c>.
/// </summary>
public sealed class StudioFilm
{
    public string Id { get; set; } = "";
    public string Genre { get; set; } = "";
    public int Rank { get; set; }
    public string CoStar { get; set; } = "";
    public string CoStarLook { get; set; } = "";
    public PuzzleFile Battle { get; set; } = new();
    public List<StudioBeat> Beats { get; set; } = new();
    public List<StudioEnding> Endings { get; set; } = new();
    public List<StudioTakeScript> Takes { get; set; } = new();

    public string TitleKey => $"studio.film.{Id}.title";
    public string GenreKey => $"studio.genre.{Genre}";

    /// <summary>Who a speaker is, by name, in this film.</summary>
    public string NameOf(StudioSpeaker speaker) => speaker switch
    {
        StudioSpeaker.CoStar => CoStar,
        StudioSpeaker.Foe => Battle.FoeName,
        _ => "{player}"
    };
}

/// <summary>What a take did: the beats it met, the lines chosen, its ending and its stars.</summary>
public sealed class StudioTake
{
    public string Film { get; set; } = "";
    public List<string> Met { get; set; } = new();
    public List<string> Chosen { get; set; } = new();
    public string Ending { get; set; } = "";
    public int Stars { get; set; }
}

/// <summary>
/// The film's beats on a battle (plan 42 · PS4; GPU-free): asked as each menu opens, it gives the next beat due, once
/// each, in the film's order; told the lines chosen, it ends the take on the battle's result. It reads the battle's log
/// by an index of its own and never takes the log, so the screen that plays it is untouched, and the rules never know
/// a film is shot. The premiere (part b) hands a director the take's picks and asks it again on the same battle.
/// </summary>
public sealed class StudioDirector
{
    public StudioFilm Film { get; }

    private readonly HashSet<string> said = new(StringComparer.Ordinal);
    private readonly List<string> met = new();
    private readonly List<string> chosen = new();
    private readonly HashSet<PokemonType> typesUsed = new();
    private int logRead;
    private readonly Queue<string>? picks;

    public StudioDirector(StudioFilm film) => Film = film;

    /// <summary>
    /// A director at the premiere (part b): the take is watched, nobody answers, and each beat's line is the one the
    /// take picked, in turn (the script's line once they run out).
    /// </summary>
    public StudioDirector(StudioFilm film, IEnumerable<string> picks) : this(film) => this.picks = new Queue<string>(picks);

    /// <summary>Whether this director is the premiere's, which picks the take's own lines.</summary>
    public bool Premiere => picks != null;

    /// <summary>The premiere's next line at a beat: the take's pick, or the script's line once the picks run out.</summary>
    public StudioChoice PickAgain(StudioBeat beat) =>
        Choose(beat, picks is { Count: > 0 } ? picks.Dequeue() : beat.Choices.FirstOrDefault(c => c.Script)?.Id);

    /// <summary>The beats met so far, and the lines chosen, in order.</summary>
    public IReadOnlyList<string> Met => met;
    public IReadOnlyList<string> Chosen => chosen;

    /// <summary>The next beat due on the battle as it stands, now marked met; null when none is.</summary>
    public StudioBeat? Next(BattleCore core)
    {
        ReadLog(core);
        foreach (var beat in Film.Beats)
        {
            if (said.Contains(beat.Id) || !Holds(beat.When, core)) continue;
            said.Add(beat.Id);
            met.Add(beat.Id);
            return beat;
        }
        return null;
    }

    /// <summary>A line chosen at a beat (by its id); an id the beat doesn't offer is its first.</summary>
    public StudioChoice Choose(StudioBeat beat, string? id)
    {
        var choice = beat.Choices.FirstOrDefault(c => c.Id == id) ?? beat.Choices[0];
        chosen.Add(choice.Id);
        return choice;
    }

    private void ReadLog(BattleCore core)
    {
        var log = core.Log;
        for (; logRead < log.Count; logRead++)
            if (log[logRead] is MoveShown shown && shown.From.Side == BattleSide.Player) typesUsed.Add(shown.Type);
    }

    private bool Holds(StudioWhen when, BattleCore core)
    {
        if (when.Turn is { } turn && core.Turn < turn) return false;
        if (when.FoeHpUnder is { } foeShare && !Under(Lead(core, BattleSide.Enemy), foeShare)) return false;
        if (when.PlayerHpUnder is { } mineShare && !Under(Lead(core, BattleSide.Player), mineShare)) return false;
        if (when.FoeDown is { } foeDown && Down(core.Trainers.SelectMany(t => t.Party.Members)) < foeDown) return false;
        if (when.PlayerDown is { } mineDown && Down(core.PlayerParty.Members) < mineDown) return false;
        if (when.TypeUsed is { } type && !typesUsed.Contains(type)) return false;
        if (when.Weather is { } weather && core.Field.WeatherInEffect != weather) return false;
        return true;
    }

    private static Pokemon? Lead(BattleCore core, BattleSide side) => core.SlotsOf(side).FirstOrDefault()?.Pokemon;

    private static bool Under(Pokemon? p, int share) => p != null && !p.IsFainted && p.CurrentHP * 100 < p.MaxHP * share;

    private static int Down(IEnumerable<Pokemon> team) => team.Count(p => p.IsFainted);

    /// <summary>The take, ended on the battle's result and the turns done.</summary>
    public StudioTake End(BattleResult result, int turnsDone)
    {
        var ending = StudioFilms.EndingOf(Film, result, turnsDone, met, chosen);
        return new StudioTake { Film = Film.Id, Met = met.ToList(), Chosen = chosen.ToList(), Ending = ending.Id, Stars = ending.Stars };
    }
}

/// <summary>
/// The studio's films (plan 42 · PS4): reading them, what is wrong with one, the battle a take is shot on, the ending a
/// take comes to, and a take shot with no screen from written answers and picks (<see cref="Shoot"/>). GPU-free.
/// </summary>
public static class StudioFilms
{
    public const string Folder = "studio-films";

    /// <summary>The most stars an ending gives.</summary>
    public const int MostStars = 5;

    private static IReadOnlyList<StudioFilm>? all;

    /// <summary>Every film of <c>Data/studio-films</c>, in the order of their files' names.</summary>
    public static IReadOnlyList<StudioFilm> All => all ??= Read(DataLayers.FilesIn(Folder, "*.json").OrderBy(f => Path.GetFileName(f.Path), StringComparer.Ordinal).Select(f => f.Path));

    public static StudioFilm? Get(string id) => All.FirstOrDefault(f => f.Id == id);

    public static IReadOnlyList<StudioFilm> Read(IEnumerable<string> paths)
    {
        var list = new List<StudioFilm>();
        foreach (string path in paths)
            list.Add(GameDataFiles.ReadStrict<StudioFilm>(File.ReadAllText(path), $"{Folder}/{Path.GetFileName(path)}"));
        return list;
    }

    /// <summary>The ending a take comes to: the first whose conditions hold (the last is for any take, which <see cref="Problems"/> holds).</summary>
    public static StudioEnding EndingOf(StudioFilm film, BattleResult result, int turnsDone, IReadOnlyCollection<string> met, IReadOnlyCollection<string> chosen)
    {
        bool won = result == BattleResult.PlayerVictory;
        foreach (var ending in film.Endings)
        {
            if (ending.Result == "won" && !won) continue;
            if (ending.Result == "lost" && won) continue;
            if (ending.ByTurn is { } by && turnsDone > by) continue;
            if (ending.Beats.Any(b => !met.Contains(b))) continue;
            if (ending.Chosen.Any(c => !chosen.Contains(c))) continue;
            return ending;
        }
        return film.Endings[^1];
    }

    /// <summary>What is wrong with a film, a line each: nothing for one the studio can shoot as written.</summary>
    public static List<string> Problems(StudioFilm film)
    {
        var problems = Puzzles.Problems(film.Battle).Select(p => $"{film.Id}: {p}").ToList();
        if (film.Battle.Wild) problems.Add($"{film.Id}: a film's foe is a trainer");
        if (film.CoStar.Length == 0) problems.Add($"{film.Id}: no co-star");
        if (film.Endings.Count == 0) problems.Add($"{film.Id}: no ending");
        else
        {
            var last = film.Endings[^1];
            if (last.Result != null || last.ByTurn != null || last.Beats.Count > 0 || last.Chosen.Count > 0)
                problems.Add($"{film.Id}: the last ending isn't for any take");
        }
        var beatIds = film.Beats.Select(b => b.Id).ToList();
        if (beatIds.Distinct().Count() != beatIds.Count) problems.Add($"{film.Id}: two beats of one id");
        var choiceIds = film.Beats.SelectMany(b => b.Choices).Select(c => c.Id).ToList();
        if (choiceIds.Distinct().Count() != choiceIds.Count) problems.Add($"{film.Id}: two lines to choose of one id");
        foreach (var beat in film.Beats)
        {
            if (beat.Lines.Count == 0 && beat.Choices.Count == 0) problems.Add($"{film.Id}: beat {beat.Id} says nothing");
            if (beat.Choices.Count is 1 or > 3) problems.Add($"{film.Id}: beat {beat.Id} offers {beat.Choices.Count} lines");
            if (beat.Choices.Count > 0 && beat.Choices.Count(c => c.Script) != 1) problems.Add($"{film.Id}: beat {beat.Id} has no one line of the script");
        }
        foreach (var ending in film.Endings)
        {
            if (ending.Stars is < 1 or > MostStars) problems.Add($"{film.Id}: ending {ending.Id} gives {ending.Stars} stars");
            if (ending.Lines.Count == 0) problems.Add($"{film.Id}: ending {ending.Id} says nothing");
            foreach (string b in ending.Beats) if (!beatIds.Contains(b)) problems.Add($"{film.Id}: ending {ending.Id} asks for no beat {b}");
            foreach (string c in ending.Chosen) if (!choiceIds.Contains(c)) problems.Add($"{film.Id}: ending {ending.Id} asks for no line {c}");
            if (ending.Result is not (null or "won" or "lost")) problems.Add($"{film.Id}: ending {ending.Id} asks for a result '{ending.Result}'");
        }
        foreach (var ending in film.Endings)
            if (!film.Takes.Any(t => t.Ending == ending.Id)) problems.Add($"{film.Id}: no take written for ending {ending.Id}");
        return problems;
    }

    /// <summary>
    /// A take shot with no screen: the film's battle fought with these answers, a beat's line picked from
    /// <paramref name="picks"/> in turn (the first offered once they run out), the beats asked as each menu opens,
    /// until the battle is over, its turns are done or an answer doesn't fit.
    /// </summary>
    public static StudioTake Shoot(StudioFilm film, IEnumerable<PuzzleAnswer> answers, IEnumerable<string> picks)
    {
        var core = new BattleCore(Puzzles.Setup(film.Battle));
        core.Start();
        var director = new StudioDirector(film);
        var pickQueue = new Queue<string>(picks);
        var answerQueue = new Queue<PuzzleAnswer>(answers);
        while (core.Result == BattleResult.None && core.Request != null)
        {
            if (core.Request is ActionRequest)
            {
                if (core.Turn >= film.Battle.Turns) break;
                while (director.Next(core) is { } beat)
                    if (beat.Choices.Count > 0) director.Choose(beat, pickQueue.Count > 0 ? pickQueue.Dequeue() : null);
            }
            if (answerQueue.Count == 0) break;
            if (Puzzles.Answer(core, answerQueue.Dequeue()) is not { } choice || core.WhyNot(choice) != null) break;
            core.Submit(choice);
        }
        return director.End(core.Result, core.Turn);
    }

    /// <summary>A take written for the film shot as written.</summary>
    public static StudioTake Shoot(StudioFilm film, StudioTakeScript take) => Shoot(film, take.Answers, take.Picks);

    /// <summary>
    /// The premiere's box office (plan 42 · PS4, part b; rulings, "The film studio"): tickets sold, a number of ours
    /// from the take's stars (squared, so a fine take far outsells a poor one) and the film's genre.
    /// </summary>
    public static int BoxOffice(StudioFilm film, int stars) =>
        stars * stars * 1200 + film.Genre switch { "ghost" => 900, "heist" => 1500, "detective" => 1100, "race" => 1800, _ => 1000 };

    /// <summary>The file a film's best take is kept in, in the Vs. Recorder's folder: <c>studio-&lt;id&gt;.json</c>.</summary>
    public static string TakeFileName(string film) => $"studio-{film}.json";
}

/// <summary>What a save keeps of the studio (<see cref="Core.SaveData.Studio"/>): null while no film has been shot.</summary>
public sealed class StudioSave
{
    public Dictionary<string, int> Shot { get; set; } = new();
    public Dictionary<string, int> Best { get; set; } = new();
    public Dictionary<string, StudioTake> BestTakes { get; set; } = new();
}

/// <summary>
/// The studio's records (plan 42 · PS4; GPU-free): each film's takes shot, its best stars and best take, and the
/// studio's rank, the best stars summed, which opens the next films (<see cref="StudioFilm.Rank"/>).
/// </summary>
public sealed class StudioRecords
{
    private readonly Dictionary<string, int> shot = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> best = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StudioTake> bestTakes = new(StringComparer.Ordinal);

    /// <summary>The take shot last this session (not saved): what a script reads after <c>studio shoot</c>.</summary>
    public StudioTake? LastTake { get; private set; }

    /// <summary>The last premiere's box office this session (not saved): what a script reads after <c>studio premiere</c>.</summary>
    public int LastBoxOffice { get; set; }

    public int Rank => best.Values.Sum();
    public int Films => shot.Count;
    public int ShotOf(string film) => shot.GetValueOrDefault(film);
    public int BestOf(string film) => best.GetValueOrDefault(film);
    public StudioTake? BestTakeOf(string film) => bestTakes.GetValueOrDefault(film);

    /// <summary>Whether a film may be shot at the studio's rank.</summary>
    public bool Opens(StudioFilm film) => Rank >= film.Rank;

    /// <summary>A take kept: counted, and its film's best when it beats the best (a tie keeps the first). True for a new best.</summary>
    public bool Keep(StudioTake take)
    {
        LastTake = take;
        shot[take.Film] = ShotOf(take.Film) + 1;
        if (take.Stars <= BestOf(take.Film) && bestTakes.ContainsKey(take.Film)) return false;
        best[take.Film] = take.Stars;
        bestTakes[take.Film] = take;
        return true;
    }

    public void Clear()
    {
        shot.Clear();
        best.Clear();
        bestTakes.Clear();
        LastTake = null;
    }

    public StudioSave? ToSave() => shot.Count == 0 ? null : new StudioSave
    {
        Shot = new Dictionary<string, int>(shot),
        Best = new Dictionary<string, int>(best),
        BestTakes = new Dictionary<string, StudioTake>(bestTakes)
    };

    public void Load(StudioSave? save)
    {
        Clear();
        if (save == null) return;
        foreach (var (k, v) in save.Shot) shot[k] = v;
        foreach (var (k, v) in save.Best) best[k] = v;
        foreach (var (k, v) in save.BestTakes) bestTakes[k] = v;
    }
}
