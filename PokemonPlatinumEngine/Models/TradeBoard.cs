using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// <c>Data/trade-board.json</c> (plan 40 · GE8; ours; <c>docs/data-files.md</c>, "The trade board"): the made-up trainers'
/// names and looks, the languages a trainer from abroad speaks and the items a Pokémon offered may hold. Read strictly.
/// </summary>
public sealed class TradeBoardFile
{
    [DataRequired] public List<string> Names { get; set; } = new();
    [DataRequired] public List<string> Looks { get; set; } = new();
    [DataRequired] public List<string> Languages { get; set; } = new();
    [DataRequired] public List<string> Items { get; set; } = new();
}

/// <summary>A made-up trainer of the board: a name, a look of <c>characters.json</c>, a number and the language they speak (null for the player's own).</summary>
public sealed record BoardTrainer(string Name, string Look, int Number, string? Language);

/// <summary>
/// A request of the day: a trainer, the species they want and its least level, and the Pokémon they offer with what it
/// holds. <see cref="Wants"/> is of the region's own Pokédex; the card hides it until the player has seen it.
/// </summary>
public sealed record BoardRequest(string Region, int Index, BoardTrainer Trainer, string Wants, int AtLeast, string Offers, string? Item);

/// <summary>What the save keeps of today's board (<see cref="SaveData.TradeBoard"/>): the day, the requests met and the blind trades made.</summary>
public sealed class TradeBoardSave
{
    /// <summary>The day these are of, <c>yyyy-MM-dd</c>; another day's are forgotten.</summary>
    public string Day { get; set; } = "";

    /// <summary>The requests met, each <c>region:index</c>.</summary>
    public List<string> Met { get; set; } = new();

    /// <summary>How many blind trades were made that day.</summary>
    public int Blind { get; set; }

    /// <summary>Whether the first board trade of the game has been made (the Global Terminal's backdrop lady asks it).</summary>
    public bool Traded { get; set; }
}

/// <summary>
/// The trade board and blind trades (plan 40 · GE8; <c>docs/mechanics/rulings.md</c>, "The trade board and blind trades"),
/// GPU-free: three requests a day for each region and five blind trades a day, drawn from the date and the region by
/// <see cref="SplitMix"/>, never <see cref="Dice"/>, so a day draws the same however often it is looked at and the next
/// day another. The Pokémon received is made here at the given one's level, with its trainer's mark, and arrives through
/// the engine's <c>ReceiveTradedPokemon</c> (its trade evolution plays, as the later games' blind trades do).
/// </summary>
public sealed class TradeBoard
{
    public const string FileName = "trade-board.json";

    /// <summary>Requests a day for each region, and blind trades a day (decision 8).</summary>
    public const int RequestsADay = 3, BlindADay = 5;

    /// <summary>The flag the backdrop lady's first-trade gift waits for (set by the board's first trade).</summary>
    public const string FirstTradeFlag = "FLAG_TRADE_BOARD_FIRST_TRADE";

    private static readonly Lazy<TradeBoardFile> file = new(() => GameDataFiles.Load<TradeBoardFile>(FileName, strict: true));

    public static TradeBoardFile Table => file.Value;

    private static readonly Lazy<HashSet<string>> evolved = new(() =>
        PokemonDatabase.GetAll().SelectMany(s => s.Evolutions ?? new()).Select(e => e.TargetSpecies).ToHashSet(StringComparer.Ordinal));

    /// <summary>Whether a species is no evolution of another (a first stage).</summary>
    public static bool IsFirstStage(PokemonSpecies species) => !evolved.Value.Contains(species.Name);

    /// <summary>Whether a species may be wanted, offered or sent blind: no legendary, no mythical, none of ours, nothing of its own form.</summary>
    public static bool Tradable(PokemonSpecies species) => !species.Legendary && !species.Mythical && !species.IsOurs;

    // ------------------------------------------------------------------ the day's draws

    /// <summary>A day as a number: days since the first of January of year 1, the same on every machine.</summary>
    public static int DayNumber(DateTime day) => (int)(day.Date.Ticks / TimeSpan.TicksPerDay);

    private static SplitMix Draw(DateTime day, string what, int index) =>
        new(SplitMix.Mix((uint)DayNumber(day), SplitMix.Fnv(what), (uint)index * 0x9E3779B1u + 1u));

    /// <summary>The species a region's Pokédexes number that may be traded, in National order.</summary>
    public static IReadOnlyList<PokemonSpecies> SpeciesOf(string region) =>
        (RegionDatabase.Get(region)?.Dexes ?? Array.Empty<string>()).SelectMany(Pokedex.Entries).Select(e => e.Species)
            .Where(Tradable).DistinctBy(s => s.Name).OrderBy(s => s.DexNumber).ToList();

    /// <summary>The species of the regions entered that may be traded, in National order.</summary>
    public static IReadOnlyList<PokemonSpecies> SpeciesOf(IEnumerable<string> regions) =>
        regions.SelectMany(SpeciesOf).DistinctBy(s => s.Name).OrderBy(s => s.DexNumber).ToList();

    private static BoardTrainer Trainer(SplitMix rng)
    {
        var t = Table;
        string name = t.Names[rng.Int(t.Names.Count)];
        string look = t.Looks[rng.Int(t.Looks.Count)];
        int number = 10000 + rng.Int(55536);
        // One time in four a trainer from abroad, so the Masuda method's foreign parent can be had with nobody else
        string? language = rng.Int(4) == 0 ? t.Languages[rng.Int(t.Languages.Count)] : null;
        return new BoardTrainer(name, look, number, language);
    }

    /// <summary>
    /// A region's requests of a day: <see cref="RequestsADay"/>, each wanting a species of the region's Pokédex at a least
    /// level and offering one of the regions entered, one time in two holding an item. None for a region with no Pokédex.
    /// </summary>
    public static IReadOnlyList<BoardRequest> Requests(DateTime day, string region, IReadOnlyList<string> entered)
    {
        var wanted = SpeciesOf(region);
        var offered = SpeciesOf(entered.Count > 0 ? entered : new[] { region });
        var requests = new List<BoardRequest>();
        if (wanted.Count == 0 || offered.Count == 0) return requests;
        for (int i = 0; i < RequestsADay; i++)
        {
            var rng = Draw(day, "board:" + region, i);
            var trainer = Trainer(rng);
            var wants = wanted[rng.Int(wanted.Count)];
            int atLeast = 5 + 5 * rng.Int(8);
            int pick = rng.Int(offered.Count);
            var offers = offered[pick];
            if (offers.Name == wants.Name) offers = offered[(pick + 1) % offered.Count];
            string? item = rng.Int(2) == 0 ? Table.Items[rng.Int(Table.Items.Count)] : null;
            requests.Add(new BoardRequest(region, i, trainer, wants.Name, atLeast, offers.Name, item));
        }
        return requests;
    }

    /// <summary>
    /// The day's <paramref name="n"/>th blind trade (from 0): a trainer and a species of the regions entered, a first stage
    /// three times in four.
    /// </summary>
    public static (BoardTrainer Trainer, string Species) Blind(DateTime day, int n, IReadOnlyList<string> entered)
    {
        var all = SpeciesOf(entered);
        if (all.Count == 0) throw new InvalidOperationException("No region entered has a Pokédex to draw from.");
        var rng = Draw(day, "blind", n);
        var trainer = Trainer(rng);
        var firsts = all.Where(IsFirstStage).ToList();
        var pool = rng.Int(4) < 3 && firsts.Count > 0 ? firsts : all;
        return (trainer, pool[rng.Int(pool.Count)].Name);
    }

    // ------------------------------------------------------------------ the player's day

    private DateTime day = DateTime.MinValue;
    private readonly HashSet<string> met = new(StringComparer.Ordinal);

    /// <summary>The blind trades made today.</summary>
    public int BlindMade { get; private set; }

    /// <summary>Whether any board or blind trade has ever been made.</summary>
    public bool Traded { get; private set; }

    /// <summary>The blind trades left today.</summary>
    public int BlindLeft(DateTime today)
    {
        Turn(today);
        return Math.Max(0, BlindADay - BlindMade);
    }

    /// <summary>Whether a request of today has been met.</summary>
    public bool IsMet(DateTime today, BoardRequest request)
    {
        Turn(today);
        return met.Contains(request.Region + ":" + request.Index);
    }

    private void Turn(DateTime today)
    {
        if (day == today.Date) return;
        day = today.Date;
        met.Clear();
        BlindMade = 0;
    }

    public void Clear()
    {
        day = DateTime.MinValue;
        met.Clear();
        BlindMade = 0;
        Traded = false;
    }

    public void Load(TradeBoardSave? save)
    {
        Clear();
        if (save == null) return;
        Traded = save.Traded;
        if (!DateTime.TryParseExact(save.Day, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return;
        day = d;
        met.UnionWith(save.Met);
        BlindMade = save.Blind;
    }

    /// <summary>What a save keeps: null while no trade has ever been made.</summary>
    public TradeBoardSave? ToSave() => !Traded && met.Count == 0 && BlindMade == 0 ? null : new TradeBoardSave
    {
        Day = day == DateTime.MinValue ? "" : day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        Met = met.OrderBy(m => m, StringComparer.Ordinal).ToList(),
        Blind = BlindMade,
        Traded = Traded
    };

    /// <summary>
    /// Why the Pokémon given can't go to a request (or to a blind trade, with none), or null: an Egg, the last of the team
    /// able to fight, a species or level the request doesn't ask, a request met already, no blind trade left today, a run
    /// under way that holds its Pokémon (<see cref="RunHolds"/>, as the Home box asks of a save).
    /// </summary>
    public string? WhyNot(DateTime today, BoardRequest? request, Pokemon given, Party party, bool runHolds)
    {
        Turn(today);
        if (given.IsEgg) return "egg";
        if (party.Members.Contains(given) && party.Members.Count(p => !p.IsEgg && !p.IsFainted) <= 1 && !given.IsFainted) return "last";
        if (runHolds) return "run";
        if (request == null) return BlindMade >= BlindADay ? "spent" : null;
        if (met.Contains(request.Region + ":" + request.Index)) return "met";
        if (given.Species.Name != request.Wants) return "species";
        if (given.Level < request.AtLeast) return "level";
        return null;
    }

    /// <summary>Whether a run holds its Pokémon (<see cref="HomeMoves.RunHolds"/>'s rule): a Nuzlocke or a challenge's restrictions under way, not ended and not finished.</summary>
    public static bool RunHolds(RunRules run, bool ended, bool finished) =>
        (run.Nuzlocke || BattleRetry.Restricts(run.BattleTerms())) && !ended && !finished;

    /// <summary>The Pokémon a trade sends: of its species at the given one's level, with its trainer's mark, language and item, met in a trade today.</summary>
    public static Pokemon Make(BoardTrainer trainer, string species, string? item, int level, DateTime today, Random chance)
    {
        var s = PokemonDatabase.Get(species) ?? throw new InvalidOperationException($"No species {species}.");
        var mon = new Pokemon(s, Math.Clamp(level, 1, 100), chance)
        {
            Ball = "Poké Ball",
            HeldItem = item != null ? ItemDatabase.Get(item) : null,
            OriginalTrainer = new TrainerMark(trainer.Name, trainer.Number, CharacterLook(trainer.Look)),
            Language = trainer.Language
        };
        mon.Met(NpcTrade.MetBy, today);
        return mon;
    }

    private static PlayerLook CharacterLook(string look) => look.Contains("girl", StringComparison.OrdinalIgnoreCase)
        || look.Contains("lass", StringComparison.OrdinalIgnoreCase) || look.Contains("woman", StringComparison.OrdinalIgnoreCase)
        || look.Contains("_f", StringComparison.OrdinalIgnoreCase) ? PlayerLook.Girl : PlayerLook.Boy;

    /// <summary>Notes a request met today.</summary>
    public void Meet(DateTime today, BoardRequest request)
    {
        Turn(today);
        met.Add(request.Region + ":" + request.Index);
        Traded = true;
    }

    /// <summary>Notes a blind trade made today.</summary>
    public void MakeBlind(DateTime today)
    {
        Turn(today);
        BlindMade++;
        Traded = true;
    }
}
