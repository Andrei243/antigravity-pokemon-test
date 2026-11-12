using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>The places a locked Pokémon is turned away from (plan 44 · SD1), each asked through <see cref="HeartLock.WhyNot"/>.</summary>
public enum LockedUse
{
    /// <summary>Left at the Day Care.</summary>
    DayCare,
    /// <summary>Sent on a Poké Job.</summary>
    Jobs,
    /// <summary>Entered at the Battle Frontier or a venue of its rules.</summary>
    Frontier,
    /// <summary>Entered in the Pokéathlon.</summary>
    Athlon,
    /// <summary>Entered in a contest.</summary>
    Contest,
    /// <summary>Traded, or put in the Home box.</summary>
    Trade,
    /// <summary>Taught a move by a machine or a tutor.</summary>
    Teach,
    /// <summary>Given a Rare Candy, or anything else that raises a level outside a battle.</summary>
    RareCandy,
    /// <summary>Evolved.</summary>
    Evolve,
    /// <summary>Sent down a hollow (plan 44 · SD7).</summary>
    Delve
}

/// <summary>
/// A locked heart (plan 44 · SD1; <c>docs/mechanics/rulings.md</c>, "Locked hearts", our numbers): what Lockwork's
/// machine did to a Pokémon, so that it obeys without a bond. Its gauge starts full (<see cref="Full"/>, five bars of
/// <see cref="BarSize"/>) and falls by what the Pokémon does beside its trainer (<see cref="Fall"/>); its EXP is held
/// (<see cref="HeldExp"/>) and given when the heart opens; its moves come back one by one as the first three bars empty
/// (<see cref="MovesBack"/>). Immutable: whatever lowers it makes a new one, so a battle's copy of a Pokémon never
/// changes the screen's. A Pokémon carries it as <see cref="Pokemon.Lock"/>, null for every Pokémon but the locked.
/// GPU-free.
/// </summary>
/// <param name="Id">The lock's own name (<c>lock_sinnoh_gible</c>): which of Lockwork's thirty it is, kept by
/// <see cref="HeartsRecord"/> as reclaimed, missed or opened.</param>
/// <param name="Gauge">What is left of the lock, <see cref="Full"/> down to nought, where the heart opens.</param>
/// <param name="HeldExp">The EXP earned while locked and not yet given.</param>
public sealed record HeartLock(string Id, int Gauge = HeartLock.Full, int HeldExp = 0)
{
    /// <summary>The key item that lets a ball reach a trainer's locked Pokémon.</summary>
    public const string ReclaimerItem = "Reclaimer";

    /// <summary>A full lock.</summary>
    public const int Full = 100;

    /// <summary>One of the gauge's five bars.</summary>
    public const int BarSize = 20;

    /// <summary>How much each thing lowers the gauge (our numbers; about two hours' play empties one).</summary>
    public static class Fall
    {
        /// <summary>A battle it was sent out in.</summary>
        public const int Battle = 4;
        /// <summary>A foe it knocked out.</summary>
        public const int KnockOut = 3;
        /// <summary>Every 128 steps in the team.</summary>
        public const int Steps = 2;
        /// <summary>The camp's pat.</summary>
        public const int Pat = 6;
        /// <summary>The camp's meal.</summary>
        public const int Meal = 6;
        /// <summary>Calmed in battle after breaking loose.</summary>
        public const int Calm = 6;
        /// <summary>The spring's rite, once a day.</summary>
        public const int Spring = 25;
    }

    /// <summary>The steps between two falls of <see cref="Fall.Steps"/>.</summary>
    public const int StepsPerFall = 128;

    /// <summary>The bars still full or partly full, five down to nought.</summary>
    public int BarsLeft => (Math.Max(0, Gauge) + BarSize - 1) / BarSize;

    /// <summary>How many of its moves it may use: the first from the start, one more as each of the first three bars empties.</summary>
    public int MovesBack => 1 + Math.Min(3, (Full - Math.Clamp(Gauge, 0, Full)) / BarSize);

    /// <summary>Whether the move in this place is sealed.</summary>
    public bool Seals(int moveIndex) => moveIndex >= MovesBack;

    /// <summary>The gauge is empty: the heart opens.</summary>
    public bool IsOpen => Gauge <= 0;

    /// <summary>The lock lowered by so much, never under nought.</summary>
    public HeartLock Lower(int by) => by <= 0 ? this : this with { Gauge = Math.Max(0, Gauge - by) };

    /// <summary>The lock holding so much more EXP.</summary>
    public HeartLock Hold(int exp) => exp <= 0 ? this : this with { HeldExp = (int)Math.Min(int.MaxValue, (long)HeldExp + exp) };

    /// <summary>The courier's own id and class (plan 44 · SD4): he battles with whatever he carries.</summary>
    public const string CourierId = "lockwork_courier", CourierClass = "Lockwork Agent";

    /// <summary>The most the courier carries at once.</summary>
    public const int CourierCarries = 6;

    /// <summary>
    /// The courier's team (plan 44 · SD4): the first six locks missed, each built afresh from the trainer of ours whose team
    /// it was in (<c>our-trainers.json</c>), full, as it went back to Lockwork's crates. A lock no trainer carries is passed over.
    /// </summary>
    public static List<Pokemon> CourierTeam(IEnumerable<string> missed)
    {
        var team = new List<Pokemon>();
        foreach (string id in missed)
        {
            if (team.Count >= CourierCarries) break;
            var member = TrainerDatabase.Ours.SelectMany(t => t.Party).FirstOrDefault(m => m.Lock == id);
            if (member != null) team.Add(TrainerDatabase.Build(member));
        }
        return team;
    }

    /// <summary>A full lock of this name, as a syndicate trainer's Pokémon carries it.</summary>
    public static HeartLock Locked(string id) => new(id);

    /// <summary>
    /// Why a Pokémon is turned away from a use; null when it isn't locked. The one rule every place that takes a
    /// Pokémon asks (the plan's "one rule, asked at each place").
    /// </summary>
    public static string? WhyNot(Pokemon? pokemon, LockedUse use) =>
        pokemon?.Lock == null ? null : Text.Of("hearts.refused", ("pokemon", pokemon.DisplayName));

    /// <summary>Whether a Pokémon is turned away from a use.</summary>
    public static bool Refuses(Pokemon? pokemon, LockedUse use) => pokemon?.Lock != null;

    /// <summary>
    /// Lowers a Pokémon's gauge, if it has one; true when the heart is open now (the gauge reached nought). Never
    /// opens it: <see cref="Open"/> is the opening.
    /// </summary>
    public static bool Lower(Pokemon pokemon, int by)
    {
        if (pokemon.Lock is not { } locked || pokemon.IsEgg) return false;
        pokemon.Lock = locked.Lower(by);
        return pokemon.Lock.IsOpen;
    }

    /// <summary>
    /// The heart opens (decision 4: at once, wherever its gauge empties): the lock is taken off, its held EXP given
    /// through <see cref="Pokemon.GainExp(int, out List{string}, out List{string}, int?)"/> up to the cap, and its
    /// friendship made its species' base. What the opening's scene shows comes back: the stats before and after and
    /// the moves the levels want.
    /// </summary>
    public static HeartOpening Open(Pokemon pokemon, int? levelCap = null)
    {
        var locked = pokemon.Lock ?? throw new InvalidOperationException("Only a locked heart opens");
        int levelBefore = pokemon.Level;
        var before = pokemon.Stats();
        pokemon.Lock = null;
        pokemon.GainExp(locked.HeldExp, out var learned, out var wanted, levelCap);
        pokemon.Friendship = pokemon.Species.BaseFriendship;
        return new HeartOpening(pokemon, locked.Id, levelBefore, pokemon.Level, before, pokemon.Stats(), learned, wanted);
    }
}

/// <summary>A heart opened (<see cref="HeartLock.Open"/>): what the opening's scene and its level panel show.</summary>
public sealed record HeartOpening(Pokemon Pokemon, string LockId, int LevelBefore, int LevelAfter, StatSpread Before, StatSpread After,
    IReadOnlyList<string> Learned, IReadOnlyList<string> Wanted)
{
    public bool Levelled => LevelAfter > LevelBefore;
}

/// <summary>
/// What the game remembers of Lockwork's locked Pokémon (plan 44 · SD1; <c>SaveData.Hearts</c>, null while empty): the
/// locks reclaimed and missed (knocked out, so the courier carries them back), by id, and how many hearts have opened.
/// GPU-free.
/// </summary>
public sealed class HeartsRecord
{
    public List<string> Reclaimed { get; set; } = new();
    public List<string> Missed { get; set; } = new();
    public List<string> Opened { get; set; } = new();

    /// <summary>The last day the spring's rite was done, as the Apricorns count days (<see cref="ApricornBox.DayOf"/>); null before the first.</summary>
    public int? SpringDay { get; set; }

    public bool IsEmpty => Reclaimed.Count == 0 && Missed.Count == 0 && Opened.Count == 0 && SpringDay == null;

    /// <summary>Noted as reclaimed: off the missed list, if it was on it.</summary>
    public void Reclaim(string id)
    {
        Missed.Remove(id);
        if (!Reclaimed.Contains(id)) Reclaimed.Add(id);
    }

    /// <summary>Noted as missed, unless it was reclaimed already.</summary>
    public void Miss(string id)
    {
        if (!Reclaimed.Contains(id) && !Missed.Contains(id)) Missed.Add(id);
    }

    public void Open(string id)
    {
        if (!Opened.Contains(id)) Opened.Add(id);
    }
}
