using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>How well two Pokémon get on at the Day Care, as the original scores it (<c>PARENTS_*</c>): the chance in a hundred that an Egg is found each 256 steps.</summary>
public enum Compatibility
{
    None = 0,
    Low = 20,
    Medium = 50,
    High = 70
}

/// <summary>
/// Eggs and what they inherit, by Platinum's rules (plan 06 · R15; <c>src/overlay005/daycare.c</c>,
/// <c>src/egg_hatch.c</c>, <c>ScrCmd_GiveEgg</c>): how two Pokémon get on, the species an Egg of theirs is and the
/// form it takes, its personality (the Everstone's nature, the Masuda method), its IVs, its moves (the father's egg
/// moves and machines, the level-up moves both parents know, Pichu's Volt Tackle), Eggs given by people, the steps
/// an Egg is counted down by, and hatching. The modern rules' differences are the <see cref="Ruleset"/>'s. No drawing
/// or input.
/// </summary>
public static class Breeding
{
    /// <summary>An Egg's level, as made (<c>EGG_POKEMON_LEVEL</c>).</summary>
    public const int EggLevel = 1;

    /// <summary>How many IVs a parent hands down by Platinum's rules (<c>NUM_INHERITED_IVS</c>).</summary>
    public const int InheritedIvs = 3;

    /// <summary>A hatched Pokémon's friendship (<c>Egg_CreateHatchedMonInternal</c>).</summary>
    public const int HatchedFriendship = 120;

    /// <summary>Who an Egg from the Day Care is received from (the original's special met location "Day-Care Couple").</summary>
    public const string DayCareCouple = "the Day-Care Couple";

    /// <summary>The ball an Egg is in, and a hatched Pokémon with it.</summary>
    public const string EggBall = "Poké Ball";

    /// <summary>The steps of an egg cycle (<c>Daycare_GetEggCycleLength</c>): 255, or 230 on the original's twelve days.</summary>
    public const int CycleSteps = 255, ShortCycleSteps = 230;

    /// <summary>
    /// The days an egg cycle is short, as month × 100 + day (<c>sEggCycleSpecialDates</c>; its comment calls the
    /// first New Year's Day, but the number it holds is the twelfth of January, and the number is what runs).
    /// </summary>
    private static readonly int[] ShortCycleDays = { 112, 214, 303, 401, 501, 611, 707, 821, 907, 928, 1121, 1214 };

    private const string Undiscovered = "Undiscovered", DittoGroup = "Ditto";

    /// <summary>
    /// The babies an incense brings (<c>sIncenseBabyTable</c>): an Egg of the first species is the third instead
    /// unless a parent holds the incense.
    /// </summary>
    private static readonly (string Baby, string Incense, string Adult)[] IncenseBabies =
    {
        ("Wynaut", "Lax Incense", "Wobbuffet"),
        ("Azurill", "Sea Incense", "Marill"),
        ("Mime Jr.", "Odd Incense", "Mr. Mime"),
        ("Bonsly", "Rock Incense", "Sudowoodo"),
        ("Munchlax", "Full Incense", "Snorlax"),
        ("Mantyke", "Wave Incense", "Mantine"),
        ("Budew", "Rose Incense", "Roselia"),
        ("Happiny", "Luck Incense", "Chansey"),
        ("Chingling", "Pure Incense", "Chimecho")
    };

    /// <summary>The Power items' stats, for the modern rules (from HeartGold and SoulSilver: the holder's IV of that stat is handed down).</summary>
    private static readonly Dictionary<string, StatType> PowerItems = new()
    {
        ["Power Weight"] = StatType.HP,
        ["Power Bracer"] = StatType.Attack,
        ["Power Belt"] = StatType.Defense,
        ["Power Anklet"] = StatType.Speed,
        ["Power Lens"] = StatType.SpAttack,
        ["Power Band"] = StatType.SpDefense
    };

    // ------------------------------------------------------------------ getting on

    /// <summary>
    /// How well two Pokémon get on (<c>BoxMon_GetPairDaycareCompatibilityScore</c>): never when either is of the
    /// Undiscovered group or both are Ditto; with one Ditto, low from the same trainer and medium from two; never when
    /// their genders are the same or either has none, or they share no egg group; then high for the same species from
    /// two trainers, medium for the same species from one or two species from two, low for two species from one. The
    /// egg groups are the species' own, whatever its form; an Egg gets on with nobody.
    /// </summary>
    public static Compatibility Compatibility(Pokemon a, Pokemon b)
    {
        if (a.IsEgg || b.IsEgg) return Models.Compatibility.None;
        var groupsA = a.Species.EggGroups ?? new List<string>();
        var groupsB = b.Species.EggGroups ?? new List<string>();
        string firstA = groupsA.FirstOrDefault() ?? Undiscovered, firstB = groupsB.FirstOrDefault() ?? Undiscovered;
        bool sameTrainer = TrainerOf(a) == TrainerOf(b);

        if (firstA == Undiscovered || firstB == Undiscovered) return Models.Compatibility.None;
        if (firstA == DittoGroup && firstB == DittoGroup) return Models.Compatibility.None;
        if (firstA == DittoGroup || firstB == DittoGroup) return sameTrainer ? Models.Compatibility.Low : Models.Compatibility.Medium;
        if (a.Gender == b.Gender) return Models.Compatibility.None;
        if (a.Gender == Gender.Genderless || b.Gender == Gender.Genderless) return Models.Compatibility.None;
        if (!groupsA.Intersect(groupsB).Any()) return Models.Compatibility.None;
        if (a.Species == b.Species) return sameTrainer ? Models.Compatibility.Medium : Models.Compatibility.High;
        return sameTrainer ? Models.Compatibility.Low : Models.Compatibility.Medium;
    }

    /// <summary>A Pokémon's first trainer's number as the Day Care compares them: the player's own are all one (none marked).</summary>
    private static int? TrainerOf(Pokemon p) => p.OriginalTrainer?.Id;

    /// <summary>The Day-Care Man's word for it (<c>DaycareCompatibilityScoreToLevel</c>): 0 for the best, 3 for none.</summary>
    public static int Level(Compatibility compatibility) => compatibility switch
    {
        Models.Compatibility.High => 0,
        Models.Compatibility.Medium => 1,
        Models.Compatibility.Low => 2,
        _ => 3
    };

    // ------------------------------------------------------------------ the Egg

    /// <summary>
    /// Which of the two is the mother and which the father (<c>Egg_DetermineEggSpeciesAndParentSlots</c>): the female,
    /// or the one that isn't Ditto, is the mother; but beside a male (or a Pokémon with no gender) Ditto takes the
    /// mother's place, so the other is the father, whose moves an Egg learns.
    /// </summary>
    public static (int Mother, int Father) Parents(Pokemon first, Pokemon second)
    {
        int mother = 0, father = 1;
        var pair = new[] { first, second };
        for (int i = 0; i < 2; i++)
        {
            if (IsDitto(pair[i])) (mother, father) = (i ^ 1, i);
            else if (pair[i].Gender == Gender.Female) (mother, father) = (i, i ^ 1);
        }
        if (IsDitto(pair[father]) && pair[mother].Gender != Gender.Female) (mother, father) = (father, mother);
        return (mother, father);
    }

    private static bool IsDitto(Pokemon p) => p.Species.Name == "Ditto";

    /// <summary>
    /// The species of an Egg (<c>Egg_DetermineEggSpeciesAndParentSlots</c>, <c>Daycare_AlterEggSpeciesWithIncenseItem</c>):
    /// the offspring of the parent that isn't Ditto (the female one), Nidoran♀ and Illumise being the male species
    /// when bit 15 of the Egg's personality is set, Manaphy's Phione, and a baby an incense brings only when a parent
    /// holds it (else the species it grows into).
    /// </summary>
    public static PokemonSpecies EggSpecies(Pokemon first, Pokemon second, uint offspring)
    {
        var pair = new[] { first, second };
        var (mother, _) = Parents(first, second);
        // The parent whose species the Egg is, before Ditto may be put in the mother's place: never Ditto
        var source = IsDitto(pair[mother]) ? pair[mother ^ 1] : pair[mother];
        string name = source.Species.Offspring ?? source.Species.Name;
        const uint maleBit = 0x8000;
        if (name == "Nidoran♀" && (offspring & maleBit) != 0) name = "Nidoran♂";
        if (name == "Illumise" && (offspring & maleBit) != 0) name = "Volbeat";
        if (name == "Manaphy") name = "Phione";
        foreach (var (baby, incense, adult) in IncenseBabies)
        {
            if (name != baby) continue;
            if (first.HeldItem?.Name != incense && second.HeldItem?.Name != incense) name = adult;
            break;
        }
        return PokemonDatabase.Get(name) ?? source.Species;
    }

    /// <summary>
    /// The form an Egg takes (<c>Daycare_GiveEggFromDaycare</c>: the mother's): the same form of the Egg's species
    /// where it has one (a sandy Wormadam lays a sandy Burmy, an east-sea Gastrodon an east-sea Shellos, an Alolan
    /// Ninetales an Alolan Vulpix), else its own. Only lasting forms are carried, never one taken for a battle.
    /// </summary>
    public static string? EggForm(Pokemon mother, PokemonSpecies egg)
    {
        if (mother.FormData is not { } form || form.Kind is FormKind.Mega or FormKind.Gigantamax) return null;
        string own = mother.Species.Name + "-";
        if (!form.Name.StartsWith(own, StringComparison.Ordinal)) return null;
        return egg.Form(egg.Name + "-" + form.Name[own.Length..])?.Name;
    }

    /// <summary>
    /// Which parent's nature an Egg takes, or -1 for none (<c>Daycare_GetParentToInheritNature</c>): by Platinum's rules
    /// the female, or Ditto (a coin between two), and only one time in two when that one holds an Everstone; by the
    /// modern rules whichever holds one, always (a coin when both do).
    /// </summary>
    public static int NatureParent(Pokemon first, Pokemon second, Random rng, Ruleset rules)
    {
        var pair = new[] { first, second };
        if (rules.EverstoneAlwaysPassesNature)
        {
            bool a = HoldsEverstone(first), b = HoldsEverstone(second);
            if (a && b) return rng.Next(1 << 16) >= 0xFFFF / 2 ? 0 : 1;
            return a ? 0 : b ? 1 : -1;
        }
        int slot = -1, dittos = 0;
        for (int i = 0; i < 2; i++)
            if (pair[i].Gender == Gender.Female) slot = i;
        for (int i = 0; i < 2; i++)
            if (IsDitto(pair[i]))
            {
                dittos++;
                slot = i;
            }
        if (dittos == 2) slot = rng.Next(1 << 16) >= 0xFFFF / 2 ? 0 : 1;
        if (slot < 0 || !HoldsEverstone(pair[slot])) return -1;
        // One time in two it is passed on (LCRNG_Next() >= 0xffff / 2 hands down nothing)
        return rng.Next(1 << 16) >= 0xFFFF / 2 ? -1 : slot;
    }

    private static bool HoldsEverstone(Pokemon p) => p.HeldItem?.Name == "Everstone";

    /// <summary>
    /// The personality an Egg will have, drawn as the couple finds it (<c>Daycare_SetInheritedNature</c>): any, or with a
    /// parent's Everstone personalities drawn until one gives that parent's nature (and isn't nought), at most 2,400
    /// times. Nought would be no Egg at all, as in the original.
    /// </summary>
    public static uint OffspringPersonality(Pokemon first, Pokemon second, Random rng, Ruleset rules)
    {
        int parent = NatureParent(first, second, rng, rules);
        if (parent < 0) return Personality.Draw(rng);
        var nature = Personality.NatureOf((parent == 0 ? first : second).Personality);
        uint personality = 0;
        for (int tries = 0; tries <= 2400; tries++)
        {
            personality = Personality.Draw(rng);
            if (Personality.NatureOf(personality) == nature && personality != 0) break;
        }
        return personality;
    }

    /// <summary>
    /// The Masuda method (<c>Egg_SetInitialData</c>): when the parents come from games of different languages and the
    /// Egg's personality isn't shiny for its trainer, it is drawn again with the original's other generator up to four
    /// times (five by the modern rules) until one is.
    /// </summary>
    public static uint Masuda(uint personality, uint trainer, Pokemon first, Pokemon second, Ruleset rules)
    {
        if (first.Language == second.Language || Personality.IsShiny(trainer, personality, rules.ShinyOdds)) return personality;
        for (int i = 0; i < rules.MasudaRerolls; i++)
        {
            personality = Personality.NextAlternate(personality);
            if (Personality.IsShiny(trainer, personality, rules.ShinyOdds)) break;
        }
        return personality;
    }

    /// <summary>
    /// The Egg the Day-Care Man hands over (<c>Daycare_GiveEggFromDaycare</c>): the species and form above, made with
    /// the personality the couple found (Masuda's draws applied) and random IVs, three of which are then the parents'
    /// (<see cref="InheritIvs"/>), knowing its species' moves at level 1 and then its father's egg moves and machines and
    /// the level-up moves both parents know (<see cref="BuildMoveset"/>), a Pichu Volt Tackle when a parent holds a
    /// Light Ball; in a Poké Ball, its egg cycles in its friendship, the player's own, met at level nought.
    /// </summary>
    public static Pokemon MakeEgg(Pokemon first, Pokemon second, uint offspring, uint trainer, Random rng, Ruleset rules)
    {
        var species = EggSpecies(first, second, offspring);
        var pair = new[] { first, second };
        var (mother, father) = Parents(first, second);
        uint personality = Masuda(offspring, trainer, first, second, rules);
        var egg = NewEgg(species, personality, trainer, rng, rules);
        if (EggForm(pair[mother], species) is { } form) egg.RestoreForm(form);
        InheritIvs(egg, first, second, rng, rules);
        BuildMoveset(egg, pair[father], pair[mother]);
        if (species.Name == "Pichu" && (first.HeldItem?.Name == "Light Ball" || second.HeldItem?.Name == "Light Ball"))
            Push(egg, "Volt Tackle");
        egg.RecalculateStats();
        egg.CurrentHP = egg.MaxHP;
        return egg;
    }

    /// <summary>
    /// An Egg a person gives (<c>ScrCmd_GiveEgg</c>, <c>Egg_CreateEgg</c>: Cynthia's Togepi, Riley's Riolu): any
    /// personality, random IVs, its species' moves at level 1, in a Poké Ball, its egg cycles in its friendship.
    /// </summary>
    public static Pokemon GiftEgg(PokemonSpecies species, uint trainer, Random rng, Ruleset rules) =>
        NewEgg(species, Personality.Draw(rng), trainer, rng, rules);

    private static Pokemon NewEgg(PokemonSpecies species, uint personality, uint trainer, Random rng, Ruleset rules)
    {
        var egg = new Pokemon(species, EggLevel, personality, rng)
        {
            IsEgg = true,
            Ball = EggBall,
            MetLevel = 0
        };
        egg.IsShiny = Personality.IsShiny(trainer, personality, rules.ShinyOdds);
        egg.Friendship = Math.Clamp(species.HatchCycles, 0, 255);
        return egg;
    }

    /// <summary>
    /// Three IVs handed down (<c>Egg_InheritIVs</c>): a stat is picked from a list of the six three times, but the
    /// original takes out of the list the place of the draw rather than the stat drawn (the first place, then the
    /// second), so HP can only be the first pick, Defense only the first or second, and a stat can be picked twice;
    /// kept, as Platinum breeds by it. Each pick then comes from either parent on a coin. By the modern rules five are
    /// handed down when a parent holds a Destiny Knot, a Power item's stat comes from its holder first, and no stat is
    /// picked twice.
    /// </summary>
    public static void InheritIvs(Pokemon egg, Pokemon first, Pokemon second, Random rng, Ruleset rules)
    {
        var pair = new[] { first, second };
        if (!rules.ModernInheritance)
        {
            var available = new List<StatType>(IvOrder);
            var picked = new StatType[InheritedIvs];
            for (int i = 0; i < InheritedIvs; i++)
            {
                picked[i] = available[rng.Next(1 << 16) % (IvOrder.Length - i)];
                available.RemoveAt(i);
            }
            var from = new int[InheritedIvs];
            for (int i = 0; i < InheritedIvs; i++) from[i] = rng.Next(1 << 16) % 2;
            for (int i = 0; i < InheritedIvs; i++) SetIv(egg, picked[i], IvOf(pair[from[i]], picked[i]));
            return;
        }

        int count = first.HeldItem?.Name == "Destiny Knot" || second.HeldItem?.Name == "Destiny Knot" ? 5 : InheritedIvs;
        var left = new List<StatType>(IvOrder);
        var powered = Enumerable.Range(0, 2).Where(i => pair[i].HeldItem?.Name is { } item && PowerItems.ContainsKey(item)).ToList();
        if (powered.Count > 0)
        {
            int holder = powered[powered.Count == 1 ? 0 : rng.Next(2)];
            var stat = PowerItems[pair[holder].HeldItem!.Name];
            SetIv(egg, stat, IvOf(pair[holder], stat));
            left.Remove(stat);
            count--;
        }
        for (int i = 0; i < count && left.Count > 0; i++)
        {
            var stat = left[rng.Next(left.Count)];
            left.Remove(stat);
            SetIv(egg, stat, IvOf(pair[rng.Next(2)], stat));
        }
    }

    /// <summary>The six stats in the original's order (<c>STAT_HP</c> to <c>STAT_SPECIAL_DEFENSE</c>: Speed before the special stats).</summary>
    public static readonly StatType[] IvOrder =
        { StatType.HP, StatType.Attack, StatType.Defense, StatType.Speed, StatType.SpAttack, StatType.SpDefense };

    public static int IvOf(Pokemon p, StatType stat) => stat switch
    {
        StatType.HP => p.IvHP,
        StatType.Attack => p.IvAttack,
        StatType.Defense => p.IvDefense,
        StatType.Speed => p.IvSpeed,
        StatType.SpAttack => p.IvSpAttack,
        _ => p.IvSpDefense
    };

    private static void SetIv(Pokemon p, StatType stat, int value)
    {
        switch (stat)
        {
            case StatType.HP: p.IvHP = value; break;
            case StatType.Attack: p.IvAttack = value; break;
            case StatType.Defense: p.IvDefense = value; break;
            case StatType.Speed: p.IvSpeed = value; break;
            case StatType.SpAttack: p.IvSpAttack = value; break;
            default: p.IvSpDefense = value; break;
        }
    }

    /// <summary>
    /// An Egg's moves (<c>Egg_BuildMoveset</c>), on top of its species' at level 1, each one learned into a free place
    /// or, with four known, pushing the first out (<c>Pokemon_ReplaceMove</c>): the father's moves that are its egg
    /// moves, in his order; then his moves a TM or HM teaches that its species can learn by that machine; then the moves
    /// both parents know that its species learns by level at any level.
    /// </summary>
    public static void BuildMoveset(Pokemon egg, Pokemon father, Pokemon mother)
    {
        var fatherMoves = father.Moves.Select(m => m.Name).ToList();
        var motherMoves = mother.Moves.Select(m => m.Name).ToList();
        var eggMoves = egg.Species.EggMoves ?? new List<string>();
        foreach (string move in fatherMoves)
            if (eggMoves.Contains(move)) Push(egg, move);

        foreach (string move in fatherMoves)
            foreach (var machine in Machines.Value.Where(m => m.TeachesMove == move))
                if (egg.Species.TmMoves?.Contains(machine.Name) == true) Push(egg, move);

        var shared = fatherMoves.Where(motherMoves.Contains).ToList();
        foreach (string move in shared)
            if (egg.Learnset.Any(l => l.MoveName == move)) Push(egg, move);
    }

    private static readonly Lazy<List<ItemData>> Machines = new(() =>
        ItemDatabase.GetAll().Where(MoveTeaching.IsMachine).OrderBy(i => i.Id).ToList());

    /// <summary>A move learned as the original's eggs and Day Care learn one: a free place, or the first move pushed out; one known already is passed over.</summary>
    public static void Push(Pokemon p, string move)
    {
        if (p.Knows(move) || MoveDatabase.Get(move) == null) return;
        if (p.Moves.Count < 4)
        {
            p.Moves.Add(MoveDatabase.Create(move));
            return;
        }
        p.Moves.RemoveAt(0);
        p.Moves.Add(MoveDatabase.Create(move));
    }

    // ------------------------------------------------------------------ cycles and hatching

    /// <summary>The steps of an egg cycle on a day (<c>Daycare_GetEggCycleLength</c>).</summary>
    public static int CycleLength(DateTime day) =>
        Array.IndexOf(ShortCycleDays, day.Month * 100 + day.Day) >= 0 ? ShortCycleSteps : CycleSteps;

    /// <summary>
    /// How many cycles each cycle takes off the team's Eggs (<c>Party_GetEggCyclesToSubtract</c>): two when a Pokémon
    /// of the team that isn't an Egg has Flame Body or Magma Armor, one otherwise.
    /// </summary>
    public static int CyclesPerCycle(Party party) =>
        party.Members.Any(p => !p.IsEgg && p.AbilityName is "Flame Body" or "Magma Armor") ? 2 : 1;

    /// <summary>
    /// The end of an egg cycle for the team's Eggs (<c>Daycare_Update</c>'s second half): each Egg in turn loses its
    /// cycles (or the one it has left); the first found with none left hatches, and the Eggs after it wait for the
    /// next cycle. The Egg that hatches is returned; null when none does.
    /// </summary>
    public static Pokemon? CountDown(Party party)
    {
        int less = CyclesPerCycle(party);
        foreach (var egg in party.Members.Where(p => p.IsEgg))
        {
            int cycles = egg.EggCycles;
            if (cycles == 0) return egg;
            egg.EggCycles = cycles >= less ? cycles - less : cycles - 1;
        }
        return null;
    }

    /// <summary>The steps an Egg of this many cycles takes to hatch, at most: the cycles and one more to find it at nought.</summary>
    public static int StepsToHatch(int cycles, int stepsPerCycle = CycleSteps) => (cycles + 1) * stepsPerCycle;

    /// <summary>
    /// An Egg hatched (<c>Egg_CreateHatchedMon</c>): the Pokémon its personality and IVs make at level 1, with the
    /// Egg's moves, friendship 120, its own species' name, in a Poké Ball, met here today at level nought.
    /// </summary>
    public static void Hatch(Pokemon egg, string? place, DateTime day)
    {
        if (!egg.IsEgg) return;
        egg.IsEgg = false;
        egg.Friendship = HatchedFriendship;
        egg.Nickname = egg.Species.Name;
        egg.Ball = EggBall;
        egg.Met(place, day);
        egg.MetLevel = 0;
        int oldMax = egg.MaxHP;
        egg.RecalculateStats();
        egg.CurrentHP = egg.MaxHP;
        egg.Status = StatusCondition.None;
        foreach (var m in egg.Moves) m.RestorePP();
        _ = oldMax;
    }

    /// <summary>
    /// What the summary says of an Egg by the cycles it has left (the original's four sentences, by its thresholds:
    /// five or fewer, ten, forty, more), in our own words.
    /// </summary>
    public static string Watch(Pokemon egg) => egg.EggCycles switch
    {
        <= 5 => "Sounds are coming from inside! It's going to hatch any moment now!",
        <= 10 => "It wobbles now and then. It can't be far from hatching.",
        <= 40 => "What could be inside? It doesn't seem ready to hatch yet.",
        _ => "It looks as though this Egg will take a long time to hatch."
    };
}
