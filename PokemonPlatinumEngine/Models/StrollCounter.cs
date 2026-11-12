using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// A course of the Stroll Counter (plan 35 · BN7; docs/mechanics/rulings.md, "The Stroll Counter"): a stretch of a
/// region's country of our own, open once the region has been entered and the counter has walked
/// <see cref="OpensAt"/> strides in all. Its seek finds one of <see cref="Finds"/>; its meet is one of
/// <see cref="Species"/>, every one met in that region's own grass, at a level from <see cref="MinLevel"/> to
/// <see cref="MaxLevel"/> of the region's own game (mapped as the region's levels are in a region the game crossed into).
/// </summary>
public sealed record StrollCourse(string Id, string Region, int OpensAt, IReadOnlyList<string> Finds, IReadOnlyList<string> Species, int MinLevel, int MaxLevel);

/// <summary>What the Stroll Counter holds as the save keeps it (plan 35 · BN7): null in a save with nothing in it, so no version was raised.</summary>
public sealed class StrollSave
{
    /// <summary>The Pokémon riding in it, held whole as a job holds one; null when it is empty.</summary>
    public SavedPokemonData? Pokemon { get; set; }

    /// <summary>The steps walked since this Pokémon went in (a stride is <see cref="StrollCounter.StepsPerStride"/> of them).</summary>
    public int Steps { get; set; }

    /// <summary>The sparks to spend on a seek or a meet: one a stride, at most <see cref="StrollCounter.MostSparks"/>.</summary>
    public int Sparks { get; set; }

    /// <summary>Every stride the counter has ever walked, with whoever was in it: what opens the further courses.</summary>
    public int StridesEver { get; set; }

    /// <summary>The course it walks.</summary>
    public string Course { get; set; } = "";

    /// <summary>The Pokémon met and caught on the way, held until a PC sends them to the boxes (at most <see cref="StrollCounter.MostHeld"/>).</summary>
    public List<SavedPokemonData> Caught { get; set; } = new();

    /// <summary>The things a seek found, held until a PC puts them in the bag (at most <see cref="StrollCounter.MostHeld"/>).</summary>
    public List<string> Found { get; set; } = new();

    /// <summary>How many seeks and meets it has made: what seeds the next one's generator, so no draw is taken from the dice.</summary>
    public int Draws { get; set; }

    public bool IsEmpty => Pokemon == null && Caught.Count == 0 && Found.Count == 0 && StridesEver == 0;

    public StrollSave Copy() => new()
    {
        Pokemon = Pokemon, Steps = Steps, Sparks = Sparks, StridesEver = StridesEver, Course = Course,
        Caught = Caught.ToList(), Found = Found.ToList(), Draws = Draws
    };
}

/// <summary>Why the counter won't do something.</summary>
public enum StrollRefusal { None, Empty, Occupied, Egg, Locked, NoSparks, HandsFull, NoCatching, NoCourse }

/// <summary>What a meet's choice is.</summary>
public enum StrollMove { Strike, Dodge, Catch }

/// <summary>How a meet stands.</summary>
public enum StrollMeetEnd { Going, Caught, Fled, Lost }

/// <summary>
/// A meet with a Pokémon of the course (plan 35 · BN7): at most <see cref="StrollCounter.MeetRounds"/> rounds, each the
/// player's STRIKE, DODGE or CATCH and the other's answer, every draw on the generator it was made with. A function of
/// that generator and the choices alone, so a seed and three presses are the whole meet.
/// </summary>
public sealed class StrollMeet
{
    private readonly Random random;

    public string Species { get; }
    public int Level { get; }
    public int Round { get; private set; }
    public int WildHearts { get; private set; } = StrollCounter.Hearts;
    public int OwnHearts { get; private set; } = StrollCounter.Hearts;
    public StrollMeetEnd End { get; private set; } = StrollMeetEnd.Going;

    /// <summary>The last round's two halves, for the screen: whether the player's move landed, and whether the other's did.</summary>
    public bool LastLanded { get; private set; }
    public bool LastHit { get; private set; }
    public StrollMove? LastMove { get; private set; }

    // A dodge makes the next strike sure
    private bool steady;

    public StrollMeet(string species, int level, Random random)
    {
        Species = species;
        Level = level;
        this.random = random;
    }

    /// <summary>The chance in a hundred that a CATCH holds now: thirty, and thirty more for each heart the other has lost.</summary>
    public int CatchChance => Math.Min(95, 30 + 30 * (StrollCounter.Hearts - WildHearts));

    /// <summary>Plays a round. Nothing once the meet is over.</summary>
    public void Play(StrollMove move)
    {
        if (End != StrollMeetEnd.Going) return;
        Round++;
        LastMove = move;
        LastLanded = false;
        LastHit = false;
        switch (move)
        {
            case StrollMove.Strike:
                LastLanded = steady || random.Next(100) < StrollCounter.StrikeChance;
                steady = false;
                if (LastLanded) WildHearts = Math.Max(0, WildHearts - 1);
                break;
            case StrollMove.Dodge:
                steady = true;
                break;
            case StrollMove.Catch:
                steady = false;
                LastLanded = random.Next(100) < CatchChance;
                if (LastLanded) { End = StrollMeetEnd.Caught; return; }
                break;
        }
        // The other's answer: nothing lands on a dodge; a Pokémon with no hearts left only stands
        if (move != StrollMove.Dodge && WildHearts > 0)
        {
            LastHit = random.Next(100) < StrollCounter.AnswerChance;
            if (LastHit) OwnHearts = Math.Max(0, OwnHearts - 1);
        }
        if (OwnHearts == 0) End = StrollMeetEnd.Lost;
        else if (Round >= StrollCounter.MeetRounds) End = StrollMeetEnd.Fled;
    }
}

/// <summary>
/// A seek on the course (plan 35 · BN7): three bushes, one of which hides the find, and two tries.
/// </summary>
public sealed class StrollSeek
{
    public int Hidden { get; }
    public string Find { get; }
    public int Tries { get; private set; }
    public IReadOnlyList<int> Looked => looked;
    private readonly List<int> looked = new();
    public bool Found { get; private set; }
    public bool Over => Found || Tries >= StrollCounter.SeekTries;

    public StrollSeek(StrollCourse course, Random random)
    {
        Hidden = random.Next(StrollCounter.Bushes);
        Find = course.Finds[random.Next(course.Finds.Count)];
    }

    /// <summary>Looks in a bush (0 to 2); false for one already looked in or once the seek is over.</summary>
    public bool Look(int bush)
    {
        if (Over || bush < 0 || bush >= StrollCounter.Bushes || looked.Contains(bush)) return false;
        looked.Add(bush);
        Tries++;
        if (bush == Hidden) Found = true;
        return true;
    }
}

/// <summary>
/// The Stroll Counter (plan 35 · BN7; docs/mechanics/rulings.md, "The Stroll Counter"; GPU-free): a key item of ours one
/// Pokémon of the boxes rides in while the player walks. Every <see cref="StepsPerStride"/> steps are a stride and a
/// spark; sparks are spent on a seek or a meet on the course it walks. Paid at a PC as it comes back: EXP by its strides,
/// a level's worth at most, and friendship. Every number is ours.
/// </summary>
public static class StrollCounter
{
    public const string Item = "Stroll Counter";
    public const string FieldUse = "StrollCounter";

    /// <summary>The flag set as the Pokétch Company's president hands it over.</summary>
    public const string GivenFlag = "FLAG_GOT_STROLL_COUNTER";

    public const int StepsPerStride = 20;
    public const int MostSparks = 999;
    public const int SparksPerTry = 10;
    public const int MostHeld = 3;
    public const int Bushes = 3, SeekTries = 2;
    public const int MeetRounds = 3, Hearts = 2;
    public const int StrikeChance = 70, AnswerChance = 40;

    /// <summary>Strides that pay a level's worth of EXP, the most a stroll pays.</summary>
    public const int StridesToALevel = 100;

    /// <summary>Strides that pay a point of friendship, and the most a stroll pays.</summary>
    public const int StridesPerFriendship = 10, MostFriendship = 20;

    /// <summary>
    /// The courses (ours), two to each region with a Town Map of its own built by the plan: the first open once the region
    /// is entered, the second after <c>OpensAt</c> strides in all. Every species is met in its region's own grass
    /// (<c>StrollCounterTests</c> holds it), so no region's list of the ways to a species moves.
    /// </summary>
    public static readonly IReadOnlyList<StrollCourse> Courses = new StrollCourse[]
    {
        new("sinnoh_meadow", RegionDatabase.Sinnoh, 0, new[] { "Oran Berry", "Pecha Berry", "Potion", "Poké Ball", "Honey" }, new[] { "Bidoof", "Starly", "Kricketot", "Shinx", "Budew" }, 6, 10),
        new("sinnoh_heights", RegionDatabase.Sinnoh, 300, new[] { "Sitrus Berry", "Super Potion", "Great Ball", "Stardust", "Star Piece" }, new[] { "Ponyta", "Geodude", "Gligar", "Chatot", "Buizel" }, 18, 24),
        new("kanto_meadow", RegionDatabase.Kanto, 0, new[] { "Oran Berry", "Cheri Berry", "Potion", "Poké Ball", "Antidote" }, new[] { "Pidgey", "Rattata", "Caterpie", "Weedle", "Spearow" }, 3, 7),
        new("kanto_heights", RegionDatabase.Kanto, 300, new[] { "Sitrus Berry", "Super Potion", "Great Ball", "Stardust", "Nugget" }, new[] { "Oddish", "Ekans", "Mankey", "Growlithe", "Abra" }, 14, 20),
        new("johto_meadow", RegionDatabase.Johto, 0, new[] { "Oran Berry", "Chesto Berry", "Potion", "Poké Ball", "Antidote" }, new[] { "Sentret", "Hoothoot", "Pidgey", "Rattata", "Hoppip" }, 3, 7),
        new("johto_heights", RegionDatabase.Johto, 300, new[] { "Sitrus Berry", "Super Potion", "Great Ball", "Stardust", "Star Piece" }, new[] { "Mareep", "Growlithe", "Stantler", "Girafarig", "Snubbull" }, 16, 22),
        new("hoenn_meadow", RegionDatabase.Hoenn, 0, new[] { "Oran Berry", "Rawst Berry", "Potion", "Poké Ball", "Antidote" }, new[] { "Poochyena", "Zigzagoon", "Wurmple", "Taillow", "Ralts" }, 3, 7),
        new("hoenn_heights", RegionDatabase.Hoenn, 300, new[] { "Sitrus Berry", "Super Potion", "Great Ball", "Stardust", "Heart Scale" }, new[] { "Electrike", "Spinda", "Swablu", "Oddish", "Gulpin" }, 16, 22),
        new("unova_meadow", RegionDatabase.Unova, 0, new[] { "Oran Berry", "Aspear Berry", "Potion", "Poké Ball", "Antidote" }, new[] { "Patrat", "Lillipup", "Purrloin", "Pidove", "Sewaddle" }, 3, 7),
        new("unova_heights", RegionDatabase.Unova, 300, new[] { "Sitrus Berry", "Super Potion", "Great Ball", "Stardust", "Pearl" }, new[] { "Deerling", "Minccino", "Herdier", "Foongus", "Cottonee" }, 16, 22),
    };

    public static StrollCourse? Course(string id) => Courses.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.Ordinal));

    /// <summary>The courses open to a counter that has walked so many strides, of the regions entered, in the table's order.</summary>
    public static IReadOnlyList<StrollCourse> Open(int stridesEver, Func<string, bool> entered) =>
        Courses.Where(c => entered(c.Region) && stridesEver >= c.OpensAt).ToList();

    /// <summary>Why a Pokémon can't go in: something already rides in it, an Egg, a locked heart.</summary>
    public static StrollRefusal WhyNotPut(StrollSave save, Pokemon pokemon)
    {
        if (save.Pokemon != null) return StrollRefusal.Occupied;
        if (pokemon.IsEgg) return StrollRefusal.Egg;
        if (HeartLock.Refuses(pokemon, LockedUse.Jobs)) return StrollRefusal.Locked;
        return StrollRefusal.None;
    }

    /// <summary>Puts a Pokémon in (the caller has taken it out of its box): its strides start from nought.</summary>
    public static bool Put(StrollSave save, Pokemon pokemon, string course)
    {
        if (WhyNotPut(save, pokemon) != StrollRefusal.None) return false;
        save.Pokemon = SavedPokemonData.FromPokemon(pokemon);
        save.Steps = 0;
        if (save.Course.Length == 0 || Course(save.Course) == null) save.Course = course;
        return true;
    }

    /// <summary>
    /// A step of the player's: a stride every <see cref="StepsPerStride"/>, a spark with each. Only while a Pokémon rides
    /// in it; nothing is drawn. True on the step that makes a stride.
    /// </summary>
    public static bool Step(StrollSave save)
    {
        if (save.Pokemon == null) return false;
        save.Steps++;
        if (save.Steps % StepsPerStride != 0) return false;
        save.StridesEver++;
        save.Sparks = Math.Min(MostSparks, save.Sparks + 1);
        return true;
    }

    /// <summary>The strides of the Pokémon riding in it.</summary>
    public static int Strides(StrollSave save) => save.Steps / StepsPerStride;

    /// <summary>Why a seek can't be made now.</summary>
    public static StrollRefusal WhyNotSeek(StrollSave save)
    {
        if (save.Pokemon == null) return StrollRefusal.Empty;
        if (Course(save.Course) == null) return StrollRefusal.NoCourse;
        if (save.Sparks < SparksPerTry) return StrollRefusal.NoSparks;
        if (save.Found.Count >= MostHeld) return StrollRefusal.HandsFull;
        return StrollRefusal.None;
    }

    /// <summary>Why a meet can't be made now: also never in a run that forbids catching or under a Nuzlocke, whose catches are the places'.</summary>
    public static StrollRefusal WhyNotMeet(StrollSave save, RunRules? run)
    {
        if (save.Pokemon == null) return StrollRefusal.Empty;
        if (Course(save.Course) == null) return StrollRefusal.NoCourse;
        if (run != null && (run.NoCatching || run.Nuzlocke)) return StrollRefusal.NoCatching;
        if (save.Sparks < SparksPerTry) return StrollRefusal.NoSparks;
        if (save.Caught.Count >= MostHeld) return StrollRefusal.HandsFull;
        return StrollRefusal.None;
    }

    /// <summary>The generator a seek's or a meet's draws come from: the save's count of them and the trainer's number, never the dice.</summary>
    public static Random GeneratorFor(StrollSave save, uint trainer) =>
        new((int)(SplitMix.Mix(trainer, (uint)save.Draws, 0x5157_7A11u) & 0x7FFF_FFFF));

    /// <summary>Where a Pokémon met on a stroll says it was met (the save keeps English, as every place's name).</summary>
    public const string MetPlace = "Stroll Counter";

    /// <summary>Begins a seek: its sparks spent and its generator taken. Null when it can't be made.</summary>
    public static StrollSeek? BeginSeek(StrollSave save, uint trainer)
    {
        if (WhyNotSeek(save) != StrollRefusal.None) return null;
        var random = GeneratorFor(save, trainer);
        save.Draws++;
        save.Sparks -= SparksPerTry;
        return new StrollSeek(Course(save.Course)!, random);
    }

    /// <summary>A seek over: its find held, if found.</summary>
    public static void EndSeek(StrollSave save, StrollSeek seek)
    {
        if (seek.Found && save.Found.Count < MostHeld) save.Found.Add(seek.Find);
    }

    /// <summary>Begins a meet: its sparks spent, its Pokémon and level drawn on its own generator. Null when it can't be made.</summary>
    public static StrollMeet? BeginMeet(StrollSave save, RunRules? run, uint trainer)
    {
        if (WhyNotMeet(save, run) != StrollRefusal.None) return null;
        var random = GeneratorFor(save, trainer);
        save.Draws++;
        save.Sparks -= SparksPerTry;
        var course = Course(save.Course)!;
        string species = course.Species[random.Next(course.Species.Count)];
        int level = random.Next(course.MinLevel, course.MaxLevel + 1);
        return new StrollMeet(species, level, random);
    }

    /// <summary>
    /// A meet over: a Pokémon caught is made on the meet's own generator (its personality first, then its IVs) at its level
    /// as <paramref name="level"/> maps it, the randomiser's species where a run has one, and held. Null for none.
    /// </summary>
    public static Pokemon? EndMeet(StrollSave save, StrollMeet meet, uint trainer, Func<int, int> level, Randomizer? randomizer, DateTime day)
    {
        if (meet.End != StrollMeetEnd.Caught || save.Caught.Count >= MostHeld) return null;
        string name = randomizer?.Species(meet.Species) ?? meet.Species;
        if (PokemonDatabase.Get(name) is not { } species) return null;
        var random = new Random((int)(SplitMix.Mix(trainer, (uint)save.Draws, 0xC47C_4000u) & 0x7FFF_FFFF));
        var caught = new Pokemon(species, Math.Clamp(level(meet.Level), 1, Ruleset.HighestLevel), random) { Ball = "Poké Ball" };
        caught.Met(MetPlace, day);
        save.Caught.Add(SavedPokemonData.FromPokemon(caught));
        return caught;
    }

    /// <summary>What a stroll paid as its Pokémon came back.</summary>
    public sealed record Pay(Pokemon Pokemon, int Strides, int Exp, int LevelsGained, int Friendship, IReadOnlyList<string> MovesLearned);

    /// <summary>
    /// Takes the Pokémon back out and pays it for its strides: EXP as a share of its level, a level's worth at most and never
    /// past <paramref name="cap"/>, and a point of friendship every ten strides. Null when nothing rides in it.
    /// </summary>
    public static Pay? TakeBack(StrollSave save, int? cap)
    {
        if (save.Pokemon is not { } held) return null;
        var pokemon = held.ToPokemon();
        int strides = Strides(save);
        save.Pokemon = null;
        save.Steps = 0;
        int before = pokemon.Level;
        int limit = Math.Min(cap ?? Pokemon.Cap, Pokemon.Cap);
        int span = Math.Max(0, pokemon.ExpForNextLevel - pokemon.ExpForCurrentLevel);
        int exp = pokemon.Level >= limit ? 0 : ExpFor(strides, span);
        var learned = new List<string>();
        if (exp > 0)
        {
            pokemon.GainExp(exp, out var moves, out _, limit);
            learned.AddRange(moves);
        }
        int friendshipBefore = pokemon.Friendship;
        int friendship = Math.Min(MostFriendship, strides / StridesPerFriendship);
        if (friendship > 0) FriendshipRules.ChangeByScript(pokemon, friendship);
        return new Pay(pokemon, strides, exp, pokemon.Level - before, pokemon.Friendship - friendshipBefore, learned);
    }

    /// <summary>EXP for these strides: a share of a level's span, a level's worth at most.</summary>
    public static int ExpFor(int strides, int levelSpan) =>
        strides <= 0 || levelSpan <= 0 ? 0 : Math.Min(levelSpan, (int)((long)levelSpan * strides / StridesToALevel));

    /// <summary>The Pokémon caught on the way, taken out to go to the boxes.</summary>
    public static List<Pokemon> TakeCaught(StrollSave save)
    {
        var list = save.Caught.Select(c => c.ToPokemon()).ToList();
        save.Caught.Clear();
        return list;
    }

    /// <summary>The things found on the way, taken out to go to the bag.</summary>
    public static List<string> TakeFound(StrollSave save)
    {
        var list = save.Found.ToList();
        save.Found.Clear();
        return list;
    }
}
