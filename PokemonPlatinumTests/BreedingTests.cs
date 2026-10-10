using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Breeding (plan 06 · R15): what a personality decides (<c>src/pokemon.c</c>), the Day Care and the Eggs it finds
/// (<c>src/overlay005/daycare.c</c>), Eggs given by people (<c>ScrCmd_GiveEgg</c>), egg cycles and hatching
/// (<c>Daycare_Update</c>, <c>Egg_CreateHatchedMon</c>), the couple's scripts and the save. Every number is worked out
/// from the original's code in a comment beside it.
/// </summary>
public class BreedingTests
{
    private static PokemonSpecies S(string name) => PokemonDatabase.Get(name)!;

    /// <summary>A Pokémon of a gender, made as Cute Charm makes one (so its personality agrees), with a known nature.</summary>
    private static Pokemon Mon(string species, Gender gender, int level = 20, Nature nature = Nature.Hardy) =>
        new(S(species), level, new Random(1), gender, nature);

    /// <summary>A generator whose draws are the numbers given, in order, each taken modulo what is asked.</summary>
    private sealed class Draws(params int[] values) : Random
    {
        private int next;
        public override int Next(int maxValue) => values[next++ % values.Length] % maxValue;
        public override int Next(int minValue, int maxValue) => minValue + Next(maxValue - minValue);
    }

    private static readonly DateTime OrdinaryDay = new(2026, 3, 10), Valentines = new(2026, 2, 14);

    // ---------------------------------------------------------------- what a personality decides

    [Fact]
    public void APersonalityGivesTheNatureTheGenderAndTheAbility()
    {
        // 0x12345678 = 305,419,896: % 25 = 21, Gentle (Pokemon_GetNatureOf); its low byte is 0x78 = 120, under Shinx's
        // 127 (half female) so female, over Togepi's 31 (an eighth female) so male (SpeciesData_GetGenderOf); its bit 0
        // is clear, so the first ability (sub_02073E18)
        const uint personality = 0x12345678;
        Assert.Equal(Nature.Gentle, Personality.NatureOf(personality));
        Assert.Equal(Gender.Female, Personality.GenderOf(S("Shinx"), personality));
        Assert.Equal(Gender.Male, Personality.GenderOf(S("Togepi"), personality));
        Assert.Equal(Gender.Genderless, Personality.GenderOf(S("Magnemite"), personality));
        Assert.Equal(Gender.Female, Personality.GenderOf(S("Happiny"), personality));
        Assert.Equal("Rivalry", Personality.AbilityOf(S("Shinx").Abilities, personality));
        Assert.Equal("Intimidate", Personality.AbilityOf(S("Shinx").Abilities, personality | 1));

        var shinx = new Pokemon(S("Shinx"), 10, personality, new Random(3));
        Assert.Equal((Nature.Gentle, Gender.Female, "Rivalry", personality), (shinx.Nature, shinx.Gender, shinx.AbilityName, shinx.Personality));
    }

    [Fact]
    public void ShininessIsThePersonalityAgainstTheTrainersNumber()
    {
        // Pokemon_IsPersonalityShiny: the trainer's halves 0x1234 and 0x5678 against the personality's 0x1234 and
        // 0x567F exclusive-or to 7, under 8: shiny. Against 0x5670 they come to 8: not shiny by Platinum's rules, but
        // under the modern rules' 16 (1 in 4,096)
        const uint trainer = 0x1234_5678;
        Assert.Equal(7, Personality.ShinyValue(trainer, 0x1234_567F));
        Assert.True(Personality.IsShiny(trainer, 0x1234_567F));
        Assert.False(Personality.IsShiny(trainer, 0x1234_5670));
        Assert.True(Personality.IsShiny(trainer, 0x1234_5670, Ruleset.Modern.ShinyOdds));
    }

    [Fact]
    public void AShinyPersonalityIsShinyForItsTrainer()
    {
        // Pokemon_FindShinyPersonality: the top thirteen bits of the halves cancel the trainer's, the low three are drawn
        foreach (uint trainer in new uint[] { 0, 0x1234_5678, 0xFFFF_0001, 0x0BAD_F00D })
            for (int seed = 0; seed < 20; seed++)
                Assert.True(Personality.IsShiny(trainer, Personality.Shiny(trainer, new Random(seed))));
        // And with Synchronize's nature or Cute Charm's gender asked for besides
        var found = Personality.ShinyWith(S("Shinx"), 77, new Random(4), Gender.Male, null);
        Assert.True(Personality.IsShiny(77, found) && Personality.GenderOf(S("Shinx"), found) == Gender.Male);
        found = Personality.ShinyWith(S("Shinx"), 77, new Random(4), null, Nature.Timid);
        Assert.True(Personality.IsShiny(77, found) && Personality.NatureOf(found) == Nature.Timid);
    }

    [Fact]
    public void CuteCharmBuildsItsPersonality()
    {
        // sub_02074128: a female's is its nature; a male's 25 × (ratio / 25 + 1) + nature, so Shinx's (127) male Adamant
        // (3) is 25 × 6 + 3 = 153, whose low byte is over 127 and whose remainder by 25 is 3
        Assert.Equal(3u, Personality.CuteCharm(S("Shinx"), Gender.Female, Nature.Adamant));
        Assert.Equal(153u, Personality.CuteCharm(S("Shinx"), Gender.Male, Nature.Adamant));
        Assert.Equal(Gender.Male, Personality.GenderOf(S("Shinx"), 153));
        // Togepi's 31: 25 × 2 + nature
        Assert.Equal(50u + 10u, Personality.CuteCharm(S("Togepi"), Gender.Male, Nature.Timid));
    }

    [Fact]
    public void ANewPokemonsNatureGenderAbilityAndShininessAllComeFromItsPersonality()
    {
        for (int seed = 0; seed < 200; seed++)
        {
            var p = new Pokemon(S("Shinx"), 5, new Random(seed));
            Assert.Equal(Personality.NatureOf(p.Personality), p.Nature);
            Assert.Equal(Personality.GenderOf(p.Species, p.Personality), p.Gender);
            Assert.Equal(Personality.AbilityOf(p.Abilities, p.Personality), p.AbilityName);
            Assert.Equal(Personality.IsShiny(PlayerIdentity.Number, p.Personality), p.IsShiny);
        }
        // Synchronize's nature (sub_02074044: drawn until it comes) and Cute Charm's gender are the personality's too
        for (int seed = 0; seed < 50; seed++)
        {
            var synced = new Pokemon(S("Shinx"), 5, new Random(seed), nature: Nature.Modest);
            Assert.Equal(Nature.Modest, synced.Nature);
            Assert.Equal(Nature.Modest, Personality.NatureOf(synced.Personality));
            var charmed = new Pokemon(S("Shinx"), 5, new Random(seed), gender: Gender.Female);
            Assert.Equal(Gender.Female, charmed.Gender);
            Assert.Equal(Gender.Female, Personality.GenderOf(charmed.Species, charmed.Personality));
        }
        // A gender the species can't have is its own (Cute Charm leaves an always-female species alone)
        Assert.Equal(Gender.Female, new Pokemon(S("Happiny"), 5, new Random(2), gender: Gender.Male).Gender);
    }

    [Fact]
    public void ATrainersPokemonIsStillNeverShinyAndKeepsItsPersonality()
    {
        // TrainerData_BuildParty with OTID_NOT_SHINY: even a personality shiny for the player's number of nought
        var record = new TrainerPokemonRecord { Species = "Shinx", Level = 9, Personality = 3, IvScale = 0 };
        var built = TrainerDatabase.Build(record);
        Assert.False(built.IsShiny);
        Assert.Equal((3u, Nature.Adamant, Gender.Female), (built.Personality, built.Nature, built.Gender));
    }

    // ---------------------------------------------------------------- getting on

    [Fact]
    public void HowWellTwoGetOnIsTheOriginalsScore()
    {
        // BoxMon_GetPairDaycareCompatibilityScore: same species of two trainers 70, of one 50; two species of a group
        // from two trainers 50, from one 20; with Ditto 50 from two trainers and 20 from one; never for two Dittos, the
        // Undiscovered group, the same gender, no gender, no shared group, or an Egg
        var mark = new TrainerMark("Hilary", 25643, PlayerLook.Girl);
        var him = Mon("Shinx", Gender.Male);
        var her = Mon("Shinx", Gender.Female);
        var theirs = Mon("Shinx", Gender.Female);
        theirs.OriginalTrainer = mark;
        Assert.Equal(Compatibility.High, Breeding.Compatibility(him, theirs));
        Assert.Equal(Compatibility.Medium, Breeding.Compatibility(him, her));
        var luxio = Mon("Luxio", Gender.Female);
        Assert.Equal(Compatibility.Low, Breeding.Compatibility(him, luxio));
        var buneary = Mon("Buneary", Gender.Female);
        buneary.OriginalTrainer = mark;
        Assert.Equal(Compatibility.Medium, Breeding.Compatibility(him, buneary));
        var ditto = new Pokemon(S("Ditto"), 20, new Random(1));
        Assert.Equal(Compatibility.Low, Breeding.Compatibility(him, ditto));
        ditto.OriginalTrainer = mark;
        Assert.Equal(Compatibility.Medium, Breeding.Compatibility(ditto, her));
        Assert.Equal(Compatibility.None, Breeding.Compatibility(ditto, new Pokemon(S("Ditto"), 20, new Random(2))));
        Assert.Equal(Compatibility.None, Breeding.Compatibility(him, Mon("Shinx", Gender.Male)));
        Assert.Equal(Compatibility.None, Breeding.Compatibility(Mon("Pichu", Gender.Male), Mon("Pikachu", Gender.Female)));
        Assert.Equal(Compatibility.None, Breeding.Compatibility(him, Mon("Magikarp", Gender.Female)));
        Assert.Equal(Compatibility.Low, Breeding.Compatibility(new Pokemon(S("Magnemite"), 20, new Random(3)), new Pokemon(S("Ditto"), 20, new Random(1))));
        Assert.Equal(Compatibility.None, Breeding.Compatibility(new Pokemon(S("Magnemite"), 20, new Random(3)), him));
        var egg = Breeding.GiftEgg(S("Shinx"), 0, new Random(5), Ruleset.Platinum);
        Assert.Equal(Compatibility.None, Breeding.Compatibility(him, egg));
        // The Day-Care Man's word for each (DaycareCompatibilityScoreToLevel)
        Assert.Equal(new[] { 0, 1, 2, 3 }, new[] { Compatibility.High, Compatibility.Medium, Compatibility.Low, Compatibility.None }.Select(Breeding.Level));
    }

    // ---------------------------------------------------------------- the Egg's species, form and parents

    [Fact]
    public void AnEggIsTheMothersFirstOfTheLineOrTheBabyAnIncenseBrings()
    {
        // Egg_DetermineEggSpeciesAndParentSlots: the female's offspring (pms.narc); an incense baby only when a parent
        // holds its incense (Daycare_AlterEggSpeciesWithIncenseItem), else the species it grows into
        var father = Mon("Marill", Gender.Male);
        var mother = Mon("Azumarill", Gender.Female);
        Assert.Equal("Marill", Breeding.EggSpecies(father, mother, 1).Name);
        father.HeldItem = ItemDatabase.Get("Sea Incense");
        Assert.Equal("Azurill", Breeding.EggSpecies(father, mother, 1).Name);
        Assert.Equal("Shinx", Breeding.EggSpecies(Mon("Shinx", Gender.Male), Mon("Luxray", Gender.Female), 1).Name);
        // Beside Ditto, the other one's line whatever its gender
        var ditto = new Pokemon(S("Ditto"), 20, new Random(1));
        Assert.Equal("Pichu", Breeding.EggSpecies(Mon("Raichu", Gender.Male), ditto, 1).Name);
        Assert.Equal("Phione", Breeding.EggSpecies(ditto, new Pokemon(S("Manaphy"), 50, new Random(1)), 1).Name);
        // Nidoran♀ and Illumise: bit 15 of the Egg's personality makes the male species (EGG_GENDER_MALE)
        var nidoranF = Mon("Nidoran♀", Gender.Female);
        var nidoranM = Mon("Nidoran♂", Gender.Male);
        Assert.Equal("Nidoran♀", Breeding.EggSpecies(nidoranM, nidoranF, 0x7FFF).Name);
        Assert.Equal("Nidoran♂", Breeding.EggSpecies(nidoranM, nidoranF, 0x8000).Name);
        Assert.Equal("Volbeat", Breeding.EggSpecies(Mon("Volbeat", Gender.Male), Mon("Illumise", Gender.Female), 0x8000).Name);
        Assert.Equal("Illumise", Breeding.EggSpecies(Mon("Volbeat", Gender.Male), Mon("Illumise", Gender.Female), 0).Name);
    }

    [Fact]
    public void BesideAMaleDittoTakesTheMothersPlaceAndTheEggTakesTheMothersForm()
    {
        var ditto = new Pokemon(S("Ditto"), 20, new Random(1));
        var male = Mon("Shinx", Gender.Male);
        Assert.Equal((1, 0), Breeding.Parents(male, ditto));
        Assert.Equal((1, 0), Breeding.Parents(Mon("Shinx", Gender.Male), Mon("Shinx", Gender.Female)));
        Assert.Equal((0, 1), Breeding.Parents(Mon("Shinx", Gender.Female), ditto));
        // The mother's form (Daycare_GiveEggFromDaycare: form of parentSlots[0]): a sandy Wormadam lays a sandy Burmy,
        // an east-sea Gastrodon an east-sea Shellos
        var wormadam = Mon("Wormadam", Gender.Female);
        wormadam.ChangeForm("Wormadam-Sandy");
        Assert.Equal("Burmy-Sandy", Breeding.EggForm(wormadam, S("Burmy")));
        var gastrodon = Mon("Gastrodon", Gender.Female);
        gastrodon.ChangeForm("Gastrodon-East");
        Assert.Equal("Shellos-East", Breeding.EggForm(gastrodon, S("Shellos")));
        Assert.Null(Breeding.EggForm(Mon("Gastrodon", Gender.Female), S("Shellos")));
    }

    // ---------------------------------------------------------------- the Egg's personality

    [Fact]
    public void AnEverstoneHandsTheMothersNatureDownOneTimeInTwo()
    {
        // Daycare_GetParentToInheritNature: the female (or Ditto) holding an Everstone, and a draw of at least 0x7FFF
        // hands nothing down; under it, personalities are drawn until one has her nature
        var mother = Mon("Shinx", Gender.Female, nature: Nature.Bold);
        var father = Mon("Shinx", Gender.Male, nature: Nature.Hasty);
        Assert.Equal(-1, Breeding.NatureParent(father, mother, new Draws(0), Ruleset.Platinum));
        mother.HeldItem = ItemDatabase.Get("Everstone");
        Assert.Equal(1, Breeding.NatureParent(father, mother, new Draws(0x7FFE), Ruleset.Platinum));
        Assert.Equal(-1, Breeding.NatureParent(father, mother, new Draws(0x7FFF), Ruleset.Platinum));
        var personality = Breeding.OffspringPersonality(father, mother, new Random(9), Ruleset.Platinum);
        // (a draw under the coin, then personalities until Bold's)
        personality = Breeding.OffspringPersonality(father, mother, new Draws(0, 5, 7, 5, 0), Ruleset.Platinum);
        Assert.Equal(Nature.Bold, Personality.NatureOf(personality));
        // The father's Everstone does nothing in Platinum; by the modern rules it always hands his nature down
        mother.HeldItem = null;
        father.HeldItem = ItemDatabase.Get("Everstone");
        Assert.Equal(-1, Breeding.NatureParent(father, mother, new Draws(0), Ruleset.Platinum));
        Assert.Equal(0, Breeding.NatureParent(father, mother, new Draws(0), Ruleset.Modern));
        for (int seed = 0; seed < 20; seed++)
            Assert.Equal(Nature.Hasty, Personality.NatureOf(Breeding.OffspringPersonality(father, mother, new Random(seed), Ruleset.Modern)));
    }

    [Fact]
    public void TheMasudaMethodDrawsAPersonalityAgainForAShinyOne()
    {
        // Egg_SetInitialData: parents of different languages and a personality not shiny, so ARNG_Next draws again up to
        // four times. 0x0BADF00D's next is 0xBD71AA22 (× 1,812,433,253 + 1), which is shiny for the trainer whose number
        // it is (its halves cancel), and 0x0BADF00D isn't for that trainer (the four halves come to 60,659)
        const uint first = 0x0BADF00D, next = 0xBD71AA22;
        Assert.Equal(next, Personality.NextAlternate(first));
        Assert.False(Personality.IsShiny(next, first));
        var mine = Mon("Shinx", Gender.Male);
        var foreign = Mon("Shinx", Gender.Female);
        Assert.Equal(first, Breeding.Masuda(first, next, mine, Mon("Shinx", Gender.Female), Ruleset.Platinum));
        foreign.Language = "German";
        Assert.Equal(next, Breeding.Masuda(first, next, mine, foreign, Ruleset.Platinum));
        // Four tries in Platinum, five by the modern rules
        Assert.Equal(4, Ruleset.Platinum.MasudaRerolls);
        Assert.Equal(5, Ruleset.Modern.MasudaRerolls);
    }

    // ---------------------------------------------------------------- IVs

    [Fact]
    public void ThreeIvsAreHandedDownTheOriginalsWayRepeatsAndAll()
    {
        // Egg_InheritIVs: picks of 0, 0, 0 take HP from [HP..SpD], then (the list's first place gone, not the stat drawn)
        // Attack from [Atk, Def, Spe, SpA, SpD], then (its second place gone) Attack again from [Atk, Spe, SpA, SpD];
        // coins 0, 1, 0 give HP from the first, Attack from the second and then the first
        var a = Mon("Shinx", Gender.Male);
        var b = Mon("Shinx", Gender.Female);
        (a.IvHP, a.IvAttack, a.IvDefense, a.IvSpeed, a.IvSpAttack, a.IvSpDefense) = (1, 2, 3, 4, 5, 6);
        (b.IvHP, b.IvAttack, b.IvDefense, b.IvSpeed, b.IvSpAttack, b.IvSpDefense) = (11, 12, 13, 14, 15, 16);
        var egg = Breeding.GiftEgg(S("Shinx"), 0, new Random(1), Ruleset.Platinum);
        (egg.IvHP, egg.IvAttack, egg.IvDefense, egg.IvSpeed, egg.IvSpAttack, egg.IvSpDefense) = (0, 0, 0, 0, 0, 0);
        Breeding.InheritIvs(egg, a, b, new Draws(0, 0, 0, 0, 1, 0), Ruleset.Platinum);
        Assert.Equal((1, 2, 0, 0, 0, 0), (egg.IvHP, egg.IvAttack, egg.IvDefense, egg.IvSpeed, egg.IvSpAttack, egg.IvSpDefense));

        // Picks of 5, 4, 3: Sp. Def from the six, then place 4 of [Atk, Def, Spe, SpA, SpD] is Sp. Def again, and place 3
        // of [Atk, Spe, SpA, SpD] once more: one stat three times, all from the second parent
        (egg.IvHP, egg.IvAttack, egg.IvDefense, egg.IvSpeed, egg.IvSpAttack, egg.IvSpDefense) = (0, 0, 0, 0, 0, 0);
        Breeding.InheritIvs(egg, a, b, new Draws(5, 4, 3, 1, 1, 1), Ruleset.Platinum);
        Assert.Equal((0, 0, 0, 0, 0, 16), (egg.IvHP, egg.IvAttack, egg.IvDefense, egg.IvSpeed, egg.IvSpAttack, egg.IvSpDefense));

        // A Power item does nothing in Platinum; by the modern rules its stat comes from its holder, and a Destiny Knot
        // hands five different stats down
        a.HeldItem = ItemDatabase.Get("Power Anklet");
        b.HeldItem = ItemDatabase.Get("Destiny Knot");
        for (int seed = 0; seed < 20; seed++)
        {
            (egg.IvHP, egg.IvAttack, egg.IvDefense, egg.IvSpeed, egg.IvSpAttack, egg.IvSpDefense) = (0, 0, 0, 0, 0, 0);
            Breeding.InheritIvs(egg, a, b, new Random(seed), Ruleset.Modern);
            Assert.Equal(4, egg.IvSpeed);
            Assert.Equal(5, Breeding.IvOrder.Count(stat => Breeding.IvOf(egg, stat) != 0));
        }
    }

    // ---------------------------------------------------------------- moves

    [Fact]
    public void AnEggLearnsTheFathersEggMovesAndMachinesAndWhatBothParentsKnow()
    {
        // Egg_BuildMoveset on Shinx (Tackle at level 1): the father's Ice Fang (an egg move of Shinx's), then his
        // Thunderbolt (TM24, which Shinx learns), then Bite, which both know and Shinx learns by level (17)
        var father = Mon("Luxio", Gender.Male, level: 30);
        father.Moves.Clear();
        foreach (string move in new[] { "Ice Fang", "Thunderbolt", "Bite", "Leer" }) father.TryLearn(move);
        var mother = Mon("Shinx", Gender.Female, level: 30);
        mother.Moves.Clear();
        foreach (string move in new[] { "Bite", "Tackle" }) mother.TryLearn(move);
        var egg = Breeding.GiftEgg(S("Shinx"), 0, new Random(2), Ruleset.Platinum);
        Assert.Equal(new[] { "Tackle" }, egg.Moves.Select(m => m.Name));
        Breeding.BuildMoveset(egg, father, mother);
        Assert.Equal(new[] { "Tackle", "Ice Fang", "Thunderbolt", "Bite" }, egg.Moves.Select(m => m.Name));

        // With four known, the next pushes the first out (Pokemon_ReplaceMove)
        Breeding.Push(egg, "Howl");
        Assert.Equal(new[] { "Ice Fang", "Thunderbolt", "Bite", "Howl" }, egg.Moves.Select(m => m.Name));
        Breeding.Push(egg, "Bite");
        Assert.Equal(new[] { "Ice Fang", "Thunderbolt", "Bite", "Howl" }, egg.Moves.Select(m => m.Name));
    }

    [Fact]
    public void APichuEggKnowsVoltTackleWhenAParentHoldsALightBall()
    {
        // Egg_TryGiveVoltTackle: Pichu at level 1 knows Thunder Shock and Charm, and Volt Tackle joins them
        var father = Mon("Pikachu", Gender.Male);
        var mother = Mon("Pikachu", Gender.Female);
        var plain = Breeding.MakeEgg(father, mother, 0x00070005, 0, new Random(3), Ruleset.Platinum);
        Assert.Equal("Pichu", plain.Species.Name);
        Assert.DoesNotContain(plain.Moves, m => m.Name == "Volt Tackle");
        mother.HeldItem = ItemDatabase.Get("Light Ball");
        var charged = Breeding.MakeEgg(father, mother, 0x00070005, 0, new Random(3), Ruleset.Platinum);
        Assert.Contains(charged.Moves, m => m.Name == "Volt Tackle");
    }

    [Fact]
    public void AnEggFromTheDayCareIsMadeAsTheOriginalMakesIt()
    {
        // Egg_SetInitialData: the couple's personality, level 1, a Poké Ball, its hatch cycles in its friendship (Shinx's
        // 20), met at level nought, an Egg
        var egg = Breeding.MakeEgg(Mon("Shinx", Gender.Male), Mon("Luxray", Gender.Female), 0x00070005, 0, new Random(1), Ruleset.Platinum);
        Assert.True(egg.IsEgg);
        Assert.Equal(("Shinx", 1, 0x00070005u, 20, 20), (egg.Species.Name, egg.Level, egg.Personality, egg.Friendship, egg.EggCycles));
        Assert.Equal((Breeding.EggBall, 0), (egg.Ball, egg.MetLevel));
        Assert.Equal(Personality.NatureOf(0x00070005), egg.Nature);
        Assert.Equal(Pokemon.EggName, egg.DisplayName);
        Assert.True(egg.IsFainted);
    }

    // ---------------------------------------------------------------- cycles and hatching

    [Fact]
    public void AnEggIsCountedDownEachCycleAndHatchesTheCycleAfterItReachesNought()
    {
        // Daycare_Update: a cycle takes one from each Egg, two with Flame Body or Magma Armor on the team (but not on an
        // Egg); one with fewer left than that loses one; an Egg already at nought hatches, and those after it wait
        var party = new Party();
        party.Add(Mon("Shinx", Gender.Male));
        var egg = Breeding.GiftEgg(S("Togepi"), 0, new Random(1), Ruleset.Platinum);
        party.Add(egg);
        Assert.Equal(10, egg.EggCycles);
        Assert.Null(Breeding.CountDown(party));
        Assert.Equal(9, egg.EggCycles);
        var ponyta = Mon("Ponyta", Gender.Male);
        ponyta.AbilityName = "Flame Body";
        party.Add(ponyta);
        Assert.Equal(2, Breeding.CyclesPerCycle(party));
        Breeding.CountDown(party);
        Assert.Equal(7, egg.EggCycles);
        egg.EggCycles = 1;
        Assert.Null(Breeding.CountDown(party));
        Assert.Equal(0, egg.EggCycles);
        Assert.Same(egg, Breeding.CountDown(party));
        // An Egg's own Magma Armor counts for nothing
        Assert.Equal(1, Breeding.CyclesPerCycle(new Party { Members = { Breeding.GiftEgg(S("Slugma"), 0, new Random(1), Ruleset.Platinum) } }));
        // So an Egg of n cycles hatches at the end of cycle n + 1: Togepi's 10 take 2,805 steps
        Assert.Equal(2805, Breeding.StepsToHatch(10));
    }

    [Fact]
    public void ACycleIs255StepsAnd230OnTheOriginalsDays()
    {
        // Daycare_GetEggCycleLength: 230 on the twelve days of sEggCycleSpecialDates (February 14th among them), else 255
        Assert.Equal(255, Breeding.CycleLength(OrdinaryDay));
        Assert.Equal(230, Breeding.CycleLength(Valentines));
        Assert.Equal(230, Breeding.CycleLength(new DateTime(2026, 1, 12)));
        Assert.Equal(255, Breeding.CycleLength(new DateTime(2026, 1, 1)));

        var party = new Party();
        party.Add(Mon("Shinx", Gender.Male));
        var egg = Breeding.GiftEgg(S("Togepi"), 0, new Random(1), Ruleset.Platinum);
        party.Add(egg);
        var dayCare = new DayCare();
        for (int i = 0; i < 229; i++) dayCare.Step(party, new Random(1), Valentines, Ruleset.Platinum);
        Assert.Equal(10, egg.EggCycles);
        dayCare.Step(party, new Random(1), Valentines, Ruleset.Platinum);
        Assert.Equal((9, 0), (egg.EggCycles, dayCare.StepCounter));
        for (int i = 0; i < 254; i++) dayCare.Step(party, new Random(1), OrdinaryDay, Ruleset.Platinum);
        Assert.Equal(9, egg.EggCycles);
        dayCare.Step(party, new Random(1), OrdinaryDay, Ruleset.Platinum);
        Assert.Equal(8, egg.EggCycles);
    }

    [Fact]
    public void HatchingMakesTheEggThePokemonItsPersonalityAndIvsSay()
    {
        // Egg_CreateHatchedMon: friendship 120, its own species' name, a Poké Ball, met at level nought where it hatched
        var egg = Breeding.MakeEgg(Mon("Shinx", Gender.Male), Mon("Luxray", Gender.Female), 0x12345678, 0, new Random(1), Ruleset.Platinum);
        egg.IvHP = 31;
        var moves = egg.Moves.Select(m => m.Name).ToList();
        Breeding.Hatch(egg, "Solaceon Town", OrdinaryDay);
        Assert.False(egg.IsEgg);
        Assert.Equal(("Shinx", 1, 120, "Shinx", "Shinx"), (egg.Species.Name, egg.Level, egg.Friendship, egg.Nickname, egg.DisplayName));
        Assert.Equal(("Solaceon Town", 0, OrdinaryDay), (egg.MetLocation, egg.MetLevel, egg.MetDate!.Value));
        Assert.Equal((Nature.Gentle, Gender.Female), (egg.Nature, egg.Gender));
        Assert.Equal(moves, egg.Moves.Select(m => m.Name));
        Assert.False(egg.IsFainted);
        Assert.Equal(egg.MaxHP, egg.CurrentHP);
    }

    [Fact]
    public void TheEggWatchSaysHowNearItIs()
    {
        // The summary's four sentences by the cycles left: up to 5, 10, 40, and more (sub_02092494's thresholds)
        var egg = Breeding.GiftEgg(S("Togepi"), 0, new Random(1), Ruleset.Platinum);
        var said = new[] { 5, 6, 10, 11, 40, 41 }.Select(n => { egg.EggCycles = n; return Breeding.Watch(egg); }).ToList();
        Assert.Equal(said[0], Breeding.Watch(new Func<Pokemon>(() => { var e = Breeding.GiftEgg(S("Togepi"), 0, new Random(1), Ruleset.Platinum); e.EggCycles = 0; return e; })()));
        Assert.NotEqual(said[0], said[1]);
        Assert.Equal(said[1], said[2]);
        Assert.NotEqual(said[2], said[3]);
        Assert.Equal(said[3], said[4]);
        Assert.NotEqual(said[4], said[5]);
    }

    [Fact]
    public void AnEggIsKeptOutOfEverythingThatAsksWhoCanFight()
    {
        var party = new Party();
        party.Add(Breeding.GiftEgg(S("Togepi"), 0, new Random(1), Ruleset.Platinum));
        Assert.False(party.HasUsablePokemon);
        Assert.Null(party.FirstUsable);
        party.Add(Mon("Shinx", Gender.Male));
        Assert.Equal("Shinx", party.FirstUsable!.Species.Name);
        var egg = party.Members[0];
        // No item, no walking friendship, no field move, no Poffin, no ability at the head of the team
        Assert.False(ItemUse.WouldHelp(ItemDatabase.Get("Max Revive")!, egg));
        Assert.False(ItemUse.WouldHelp(ItemDatabase.Get("Potion")!, egg));
        FriendshipRules.Apply(egg, FriendshipEvent.WalkCycle);
        Assert.Equal(10, egg.EggCycles);
        Assert.DoesNotContain(PartyScreen.ActionsFor(egg), a => a.Kind == PartyActionKind.FieldMove);
        Assert.False(Poffins.WouldEat(egg));
        Assert.Null(PokemonPlatinumEngine.Overworld.WildLead.Of(party)!.Value.Ability);
        // No machine teaches it anything: the party shows NOT ABLE beside it
        var tm = ItemDatabase.GetAll().First(i => MoveTeaching.IsMachine(i) && S("Togepi").TmMoves?.Contains(i.Name) == true);
        Assert.False(MoveTeaching.CanLearn(egg, tm));
        Assert.Equal(TeachAnswer.NotAble, MoveTeaching.Answer(egg, tm));
        // The lottery reads no Egg's trainer (ScrCmd_CheckForJubilifeLotteryWinner): this one's would match all five
        Assert.Null(Lottery.Check(12345, 12345, new[] { egg }, Array.Empty<Pokemon>()).Winner);
    }

    // ---------------------------------------------------------------- the Day Care

    [Fact]
    public void TheDayCareGivesEachStepAsExpAndAFeeOfAHundredALevel()
    {
        // Daycare_Update adds a step to each; DaycareMon_BufferDaycarePrice is 100 + 100 for each level grown;
        // Daycare_MoveToPartyFromDaycareMon adds the steps to the EXP and grows it (no friendship for it)
        var party = new Party();
        var bidoof = Mon("Bidoof", Gender.Male, level: 5);
        party.Add(bidoof);
        party.Add(Mon("Shinx", Gender.Female));
        party.Add(Mon("Shinx", Gender.Male));
        int friendship = bidoof.Friendship;
        Assert.Equal(125, bidoof.CurrentExp);
        var dayCare = new DayCare();
        Assert.Null(DayCare.WhyNot(party, 0));
        Assert.True(dayCare.Leave(party, 0));
        Assert.Equal((DayCareState.OnePokemon, 2), (dayCare.State, party.Count));
        // Bidoof grows Medium Fast: 216 EXP for level 6, 343 for 7, 512 for 8; 300 steps bring 125 to 425
        for (int i = 0; i < 300; i++) dayCare.Step(party, new Random(1), OrdinaryDay, Ruleset.Platinum);
        Assert.Equal((300, 7, 2, 300), (dayCare.StepsOf(0), dayCare.LevelNow(0), dayCare.LevelsGained(0), dayCare.Fee(0)));
        Assert.Equal(5, bidoof.Level);
        var back = dayCare.TakeBack(party, 0);
        Assert.Same(bidoof, back);
        Assert.Equal((7, 425, friendship), (bidoof.Level, bidoof.CurrentExp, bidoof.Friendship));
        Assert.Equal(DayCareState.Empty, dayCare.State);
    }

    [Fact]
    public void TheDayCareTakesNoEggAndNotTheLastPokemonAbleToFight()
    {
        var party = new Party();
        party.Add(Mon("Shinx", Gender.Male));
        party.Add(Breeding.GiftEgg(S("Togepi"), 0, new Random(1), Ruleset.Platinum));
        Assert.Equal("last", DayCare.WhyNot(party, 0));
        Assert.Equal("egg", DayCare.WhyNot(party, 1));
        party.Add(Mon("Bidoof", Gender.Male));
        Assert.Null(DayCare.WhyNot(party, 0));
    }

    [Fact]
    public void TheCoupleFindAnEggEvery256StepsOfTheSecondByHowWellTheyGetOn()
    {
        // Daycare_Update: when the second's steps reach 0xFF in their low byte, LCRNG_Next() × 100 / 0xFFFF against the
        // score; a draw of 0 is 0 (under 50: an Egg, its personality the next two draws, 5 and 7), 0x8000 is 50 (not)
        var party = new Party();
        party.Add(Mon("Shinx", Gender.Female));
        party.Add(Mon("Shinx", Gender.Male));
        party.Add(Mon("Bidoof", Gender.Male));
        var dayCare = new DayCare();
        dayCare.Leave(party, 0);
        dayCare.Leave(party, 0);
        Assert.Equal(Compatibility.Medium, dayCare.Compatibility);
        for (int i = 0; i < 254; i++) dayCare.Step(party, new Draws(0x8000), OrdinaryDay, Ruleset.Platinum);
        Assert.False(dayCare.HasEgg);
        dayCare.Step(party, new Draws(0x8000), OrdinaryDay, Ruleset.Platinum);
        Assert.False(dayCare.HasEgg);
        // The next try is 256 steps on, at the second's 511th
        for (int i = 0; i < 256; i++) dayCare.Step(party, new Draws(0, 5, 7), OrdinaryDay, Ruleset.Platinum);
        Assert.True(dayCare.HasEgg);
        Assert.Equal((0x00070005u, DayCareState.EggWaiting), (dayCare.Offspring, dayCare.State));

        // Handed over: Shinx, the couple's personality, and the cycle begun again
        var egg = dayCare.GiveEgg(party, 0, new Random(1), Ruleset.Platinum);
        Assert.NotNull(egg);
        Assert.Equal(("Shinx", 0x00070005u, true), (egg!.Species.Name, egg.Personality, egg.IsEgg));
        Assert.Equal((false, 0, 2), (dayCare.HasEgg, dayCare.StepCounter, party.Count));
    }

    [Fact]
    public void TakingTheFirstBackMovesTheSecondIntoItsPlace()
    {
        var party = new Party();
        party.Add(Mon("Shinx", Gender.Female));
        party.Add(Mon("Bidoof", Gender.Male));
        party.Add(Mon("Luxio", Gender.Male));
        var dayCare = new DayCare();
        dayCare.Leave(party, 0);
        dayCare.Leave(party, 0);
        for (int i = 0; i < 10; i++) dayCare.Step(party, new Random(1), OrdinaryDay, Ruleset.Platinum);
        dayCare.TakeBack(party, 0);
        Assert.Equal(("Bidoof", 10), (dayCare[0]!.Species.Name, dayCare.StepsOf(0)));
        Assert.Null(dayCare[1]);
    }

    // ---------------------------------------------------------------- saving

    [Fact]
    public void TheDayCareAndEggsAreSaved()
    {
        var party = new Party();
        party.Add(Mon("Shinx", Gender.Female));
        party.Add(Mon("Bidoof", Gender.Male));
        party.Add(Mon("Luxio", Gender.Male));
        var dayCare = new DayCare();
        dayCare.Leave(party, 0);
        for (int i = 0; i < 40; i++) dayCare.Step(party, new Random(1), OrdinaryDay, Ruleset.Platinum);
        var egg = Breeding.GiftEgg(S("Togepi"), 0, new Random(1), Ruleset.Platinum);
        egg.EggCycles = 7;

        var save = new SaveData { DayCare = DayCareSave.From(dayCare), Party = { SavedPokemonData.FromPokemon(egg) } };
        var back = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(save))!;
        var restored = new DayCare();
        DayCareSave.Restore(back.DayCare, restored);
        Assert.Equal(("Shinx", 40, 40), (restored[0]!.Species.Name, restored.StepsOf(0), restored.StepCounter));
        var egged = back.Party[0].ToPokemon();
        Assert.Equal((true, 7, "Togepi", egg.Personality), (egged.IsEgg, egged.EggCycles, egged.Species.Name, egged.Personality));
        // An empty Day Care isn't written at all, and an older save reads as one
        Assert.Null(DayCareSave.From(new DayCare()));
        DayCareSave.Restore(null, restored);
        Assert.Equal(DayCareState.Empty, restored.State);
        Assert.False(JsonSerializer.Deserialize<SavedPokemonData>("{\"SpeciesName\":\"Shinx\"}")!.IsEgg);
    }

    // ---------------------------------------------------------------- the scripts

    private static HeadlessScriptHost Run(string name, string place, HeadlessScriptHost host)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(name, place)!, null);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    [Fact]
    public void TheDayCareLadyTakesAPokemonAndGivesItBackForItsFee()
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon("Shinx", Gender.Male));
        host.Party.Add(Mon("Bidoof", Gender.Male, level: 5));
        host.Party.Add(Mon("Luxio", Gender.Female));
        // Yes to raising one, Bidoof chosen, no to raising another
        host.Answers.Enqueue(0);
        host.PokemonChoice = 1;
        host.Answers.Enqueue(1);
        Run("DayCareLady", "PokemonDayCare", host);
        Assert.Equal(DayCareState.OnePokemon, host.DayCare.State);
        Assert.Equal("Bidoof", host.DayCare[0]!.Species.Name);
        Assert.True(host.Story.Has("FLAG_STORED_POKEMON_AT_DAY_CARE"));
        Assert.Contains(host.Log, l => l == "cry Bidoof");

        // 300 steps later: grown 2 levels, $300 to take back. No to another, yes to taking it back, yes to the fee
        for (int i = 0; i < 300; i++) host.DayCare.Step(host.Party, new Random(1), OrdinaryDay, Ruleset.Platinum);
        host.Answers.Enqueue(1);
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(0);
        Run("DayCareLady", "PokemonDayCare", host);
        Assert.Equal(DayCareState.Empty, host.DayCare.State);
        Assert.Equal((3000 - 300, 7), (host.Money, host.Party.Members[2].Level));
        Assert.Contains(host.Transcript, l => l.Text.Contains("grown by about 2 levels"));
    }

    [Fact]
    public void TheDayCareLadyWontTakeAnEggOrYourOnlyPokemon()
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon("Shinx", Gender.Male));
        host.Answers.Enqueue(0);
        Run("DayCareLady", "PokemonDayCare", host);
        Assert.Equal(DayCareState.Empty, host.DayCare.State);

        host.Party.Add(Breeding.GiftEgg(S("Togepi"), 0, new Random(1), Ruleset.Platinum));
        host.Party.Add(Mon("Bidoof", Gender.Male));
        host.Party.Add(Mon("Luxio", Gender.Female));
        host.Answers.Enqueue(0);
        host.PokemonChoice = 1;
        Run("DayCareLady", "PokemonDayCare", host);
        Assert.Equal(DayCareState.Empty, host.DayCare.State);
        Assert.Contains(host.Transcript, l => l.Text.Contains("only an Egg"));
    }

    [Fact]
    public void TheDayCareManHandsOverTheEggOrKeepsIt()
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon("Bidoof", Gender.Male));
        host.Party.Add(Mon("Shinx", Gender.Female));
        host.Party.Add(Mon("Luxio", Gender.Male));
        host.DayCare.Leave(host.Party, 1);
        host.DayCare.Leave(host.Party, 1);
        for (int i = 0; i < 255; i++) host.DayCare.Step(host.Party, new Draws(0, 5, 7), OrdinaryDay, Ruleset.Platinum);
        Assert.True(host.DayCare.HasEgg);

        // Twice no: the couple keep it, and nobody sees it again
        host.Answers.Enqueue(1);
        host.Answers.Enqueue(1);
        Run("DayCareMan", "solaceon_town", host);
        Assert.False(host.DayCare.HasEgg);
        Assert.Single(host.Party.Members);

        for (int i = 0; i < 256; i++) host.DayCare.Step(host.Party, new Draws(0, 5, 7), OrdinaryDay, Ruleset.Platinum);
        Assert.True(host.DayCare.HasEgg);
        host.Answers.Enqueue(0);
        Run("DayCareMan", "solaceon_town", host);
        var egg = host.Party.Members[1];
        Assert.True(egg.IsEgg);
        Assert.Equal(("Shinx", Breeding.DayCareCouple), (egg.Species.Name, egg.MetLocation));
        // How the two get on, in his words: Shinx and Luxio of one trainer, low (2)
        host.Transcript.Clear();
        Run("DayCareMan", "solaceon_town", host);
        Assert.Contains(host.Transcript, l => l.Text.Contains("don't seem to care for each other"));
    }

    [Fact]
    public void HatchingFromAScriptPutsThePokemonInTheTeam()
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon("Bidoof", Gender.Male));
        var egg = Breeding.GiftEgg(S("Togepi"), 0, new Random(1), Ruleset.Platinum);
        egg.EggCycles = 0;
        host.Party.Add(egg);
        Run("HatchEgg", "common", host);
        Assert.False(egg.IsEgg);
        Assert.Equal("Togepi", Assert.Single(host.Hatched).Species.Name);
    }

    [Fact]
    public void CynthiaGivesHerTogepiEggOrWaitsWithIt()
    {
        // EternaCity_CynthiaTryGiveEgg: no makes her wait (state 4); a full team does too; yes gives the Egg (state 5)
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon("Bidoof", Gender.Male));
        host.Story.SetVar("VAR_ETERNA_CITY_STATE", 3);
        host.Answers.Enqueue(1);
        Run("CynthiaOffer", "eterna_city", host);
        Assert.Equal(4, host.Story.Var("VAR_ETERNA_CITY_STATE"));
        for (int i = 0; i < 5; i++) host.Party.Add(Mon("Bidoof", Gender.Male));
        host.Answers.Enqueue(0);
        Run("CynthiaOffer", "eterna_city", host);
        Assert.Equal(4, host.Story.Var("VAR_ETERNA_CITY_STATE"));
        while (host.Party.Count > 1) host.Party.RemoveAt(1);
        host.Answers.Enqueue(0);
        Run("CynthiaOffer", "eterna_city", host);
        Assert.Equal(5, host.Story.Var("VAR_ETERNA_CITY_STATE"));
        var egg = host.Party.Members[1];
        Assert.Equal(("Togepi", true, 10, "Cynthia"), (egg.Species.Name, egg.IsEgg, egg.EggCycles, egg.MetLocation));
    }
}
