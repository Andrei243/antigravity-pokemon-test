using System;
using System.Collections.Generic;
using System.Linq;
using DataImporter;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 06 · R31: the 72 moves of our own learned past level 100 (docs/plans/R31-move-proposals.md, approved by the
/// user on 2026-10-25). The data (whole rows of the importer's overrides), the learnsets the importer hands them out
/// by (<see cref="MovesPastHundred"/>), and a scenario to each kind of machinery they run on: none has code of its
/// own, every one is its data fields or an effect the core had already. The battles are under the modern rules, the
/// rolls fixed by kind (<see cref="CoreScenario.Calm"/>); the Pokémon have no IVs, EVs or nature, so a stat at level
/// 50 is base + 5 and HP base + 60.
/// </summary>
public class MovesPastHundredTests
{
    private static readonly Ruleset Modern = Ruleset.Modern;

    private static List<MoveData> Ours => MoveDatabase.GetAll().Where(MovesPastHundred.IsOurs).OrderBy(m => m.Id).ToList();

    private static BattleCore Duel(Pokemon mine, Pokemon foe, BattleRandom? rolls = null) => Wild(mine, foe, rolls, Modern);

    private static int Stage(BattleCore core, Place place, StatType stat) => core.At(place).Pokemon!.StatStages.GetValueOrDefault(stat);

    private static List<LearnableMove> PastHundred(IEnumerable<LearnableMove> learnset) =>
        learnset.Where(l => MovesPastHundred.IsOurs(MoveDatabase.Get(l.MoveName))).ToList();

    // ------------------------------------------------------------------ the data

    [Fact]
    public void TheSeventyTwoAreInTheDataFourToATypeAndAllRunInFull()
    {
        var ours = Ours;
        Assert.Equal(72, ours.Count);
        Assert.Equal(Enumerable.Range(MovesPastHundred.FirstId, 72), ours.Select(m => m.Id));
        Assert.All(ours, m =>
        {
            // Generation 0 is ours: never counted with the games' moves
            Assert.Equal(0, m.Generation);
            Assert.Equal(MoveKind.Standard, m.Kind);
            Assert.Equal(MoveEffectSupport.Full, m.Support);
            Assert.True(m.Effect == null || BattleCore.HasMoveEffect(m.Effect), m.Name);
            Assert.Null(m.Contest);
            Assert.Null(m.Modern);
            Assert.InRange(m.Name.Length, 1, 16);
            Assert.False(string.IsNullOrWhiteSpace(m.Description), m.Name);
            Assert.NotNull(m.BattleEffect);
            Assert.True(Enum.TryParse<MoveEffectId>(m.BattleEffect, out _), $"{m.Name}: {m.BattleEffect}");
        });
        foreach (var type in Enum.GetValues<PokemonType>().Where(t => ours.Any(m => m.Type == t)))
        {
            var four = ours.Where(m => m.Type == type).ToList();
            Assert.Equal(new[] { MoveCategory.Status, MoveCategory.Physical, MoveCategory.Special }, four.Take(3).Select(m => m.Category));
            Assert.Equal(4, four.Count);
        }
        Assert.Equal(18, ours.Select(m => m.Type).Distinct().Count());

        // Only four name an effect, the core's own; a capstone's power is the most (140), and none knocks out at once
        Assert.Equal(new[] { "Rivet Shot", "Root Snare", "Mindbreak Wave", "Pilfer Strike" }, ours.Where(m => m.Effect != null).Select(m => m.Name));
        Assert.Equal(140, ours.Max(m => m.Power));
        Assert.Equal(8, ours.Count(m => m.Power >= 120));
        // A move of ours under a Z-Crystal or a Dynamax does what one of its power does
        Assert.All(ours.Where(m => m.Category != MoveCategory.Status), m =>
            Assert.Equal((Importer.ZPowerOf(m.Power, false), Importer.MaxPowerOf(m.Power, m.Type.ToString())), (m.ZPower, m.MaxPower)));
    }

    // ------------------------------------------------------------------ who learns them

    [Fact]
    public void EverySpeciesButSixLearnsFourToEightOfThemBetween110And250()
    {
        var none = new[] { "Ditto", "Unown", "Smeargle", "Wobbuffet", "Wynaut", "Shedinja" };
        var counts = new Dictionary<int, int>();
        var learned = new HashSet<string>();
        foreach (var s in PokemonDatabase.GetAll())
        {
            // The list the game learns by (the newest games', or its own), and Platinum's beside it
            foreach (var list in new[] { s.Under(Modern).Learnset, s.Learnset })
            {
                var ours = PastHundred(list);
                if (none.Contains(s.Name))
                {
                    Assert.Empty(ours);
                    continue;
                }
                Assert.InRange(ours.Count, 4, 8);
                Assert.Equal(MovesPastHundred.FirstLevel, ours[0].Level);
                Assert.Equal(MovesPastHundred.LastLevel, ours[^1].Level);
                Assert.All(ours, l => Assert.Equal(0, l.Level % 5));
                // Learned in order, after everything the games teach it
                Assert.True(ours.Zip(ours.Skip(1)).All(p => p.First.Level < p.Second.Level), s.Name);
                Assert.True(list.Where(l => !MovesPastHundred.IsOurs(MoveDatabase.Get(l.MoveName))).All(l => l.Level <= 100), s.Name);
            }
            var own = PastHundred(s.Under(Modern).Learnset);
            if (own.Count == 0) continue;
            counts[own.Count] = counts.GetValueOrDefault(own.Count) + 1;
            foreach (var l in own) learned.Add(l.MoveName);
        }
        // The proposal's table: 400 learn four, 346 five, 183 six and 90 eight, 5,148 learnings in all
        Assert.Equal(new Dictionary<int, int> { [4] = 400, [5] = 346, [6] = 183, [8] = 90 }, counts);
        Assert.Equal(Ours.Select(m => m.Name).ToHashSet(), learned);
    }

    [Theory]
    [InlineData("Garchomp", "110 Wyrm Dance, 140 Scale Rend, 165 Dust Veil, 195 Fault Line, 220 Serpent Crash, 250 Tectonic Rend")]
    [InlineData("Infernape", "110 Kindle, 130 Cinder Stomp, 150 Pyre Lance, 170 Centered Stance, 190 Crescent Kick, 210 Ki Burst, 230 Sunforge Blast, 250 Mountain Breaker")]
    [InlineData("Empoleon", "110 Tidal Ward, 155 Abyssal Current, 205 Alloy Plating, 250 Magnet Lance")]
    [InlineData("Togekiss", "110 Moonlit Grace, 145 Starlight Chorus, 180 Dew Kiss, 215 Updraft, 250 Jet Stream")]
    [InlineData("Blissey", "110 Rally Cry, 155 Resonant Hymn, 205 Snapstep, 250 Titan Tackle")]
    [InlineData("Machamp", "110 Centered Stance, 145 Crescent Kick, 180 Ki Burst, 215 Rally Cry, 250 Mountain Breaker")]
    public void ASpeciesLearnsByTheRuleItsTypesAndStatsGive(string species, string expected)
    {
        // The proposal's examples: its first type's moves, its second's, at least four, capstones last (Machamp, a
        // Fighting type alone, takes Normal's status move to make four)
        var ours = PastHundred(PokemonDatabase.Get(species)!.Under(Modern).Learnset);
        Assert.Equal(expected, string.Join(", ", ours.Select(l => $"{l.Level} {l.MoveName}")));
    }

    [Fact]
    public void AFormWithMovesOfItsOwnLearnsByItsOwnTypes()
    {
        // Rotom-Heat is Electric and Fire under the modern rules, and fights with its Sp. Atk (105 against 65): it ends
        // on Fire's capstone, where Rotom (Electric and Ghost) ends on Ghost's special moves
        var rotom = PokemonDatabase.Get("Rotom")!.Under(Modern);
        var heat = rotom.Forms!.Single(f => f.Name == "Rotom-Heat");
        Assert.Equal("110 Overcharge, 145 Ion Cascade, 180 Kindle, 215 Pyre Lance, 250 Sunforge Blast",
            string.Join(", ", PastHundred(heat.Learnset!).Select(l => $"{l.Level} {l.MoveName}")));
        Assert.DoesNotContain(PastHundred(rotom.Learnset), l => l.MoveName == "Sunforge Blast");

        // A regional form too: Galarian Meowth is Steel
        var galar = PokemonDatabase.Get("Meowth")!.Forms!.Single(f => f.Name == "Meowth-Galar");
        Assert.Equal("Alloy Plating", PastHundred(galar.Learnset!)[0].MoveName);
    }

    [Fact]
    public void ALevelPast100OffersItsMoveAsAnyLevelDoes()
    {
        // Garchomp at 109 with four moves: the EXP to 110 asks which to forget for Wyrm Dance
        var garchomp = Mon("Garchomp", 109, "Dragon Claw", "Earthquake", "Crunch", "Swords Dance");
        int exp = Pokemon.GetExpForLevel(110, garchomp.Species.GrowthRate) - garchomp.CurrentExp;
        Assert.True(garchomp.GainExp(exp, out var learned, out var wanted, cap: Ruleset.HighestLevel));
        Assert.Equal(110, garchomp.Level);
        Assert.Empty(learned);
        Assert.Equal(new[] { "Wyrm Dance" }, wanted);

        // One made past them knows the last four it would have learned; the Move Relearner offers them
        var high = new Pokemon(PokemonDatabase.Get("Garchomp")!, 200, Gender.Male, Nature.Hardy, false);
        Assert.Equal(new[] { "Wyrm Dance", "Scale Rend", "Dust Veil", "Fault Line" }, high.Moves.Select(m => m.Name));
        Assert.Contains("Scale Rend", Relearning.MovesFor(Mon("Garchomp", 150, "Tackle")));
        Assert.DoesNotContain("Fault Line", Relearning.MovesFor(Mon("Garchomp", 150, "Tackle")));
    }

    [Fact]
    public void ATrainerOfALaterRegionKeepsTheirOwnGamesMoves()
    {
        // W4's mapping lifts a team past 100; what its level taught it stops at the games' own 100 (R31's question 8)
        var trainer = new Trainer { Id = "johto_lance", Name = "Lance", TrainerClass = "Champion" };
        trainer.Party.Add(new Pokemon(PokemonDatabase.Get("Dragonite")!, 62, Gender.Male, Nature.Hardy, false));
        var met = trainer.Relevelled(l => l + 74);
        var dragonite = met.Party.Members[0];
        Assert.Equal(136, dragonite.Level);
        Assert.DoesNotContain(dragonite.Moves, m => MovesPastHundred.IsOurs(m.Data));
        Assert.Equal(new Pokemon(PokemonDatabase.Get("Dragonite")!, 100, Gender.Male, Nature.Hardy, false).Moves.Select(m => m.Name), dragonite.Moves.Select(m => m.Name));
    }

    // ------------------------------------------------------------------ the machinery, a kind each

    [Fact]
    public void AHealWithAStatHealsAndRaisesItEvenAtFullHp()
    {
        // Molt Renewal: a quarter of Scizor's 130 HP (70 + 60) is 32, and Speed +1, one after the other (ByItsFields)
        var scizor = Mon("Scizor", 50, "Molt Renewal");
        scizor.CurrentHP = 50;
        var core = Duel(scizor, Idling(Mon("Snorlax", 50)));
        Assert.Contains("Scizor regained health!", Turn(core, 0, 0));
        Assert.Equal((82, 1), (scizor.CurrentHP, Stage(core, Mine, StatType.Speed)));

        // At full HP it still raises the stat
        Assert.Equal(130, scizor.MaxHP);
        scizor.CurrentHP = scizor.MaxHP;
        Assert.Contains("Scizor's HP is full!", Turn(core, 0, 0));
        Assert.Equal(2, Stage(core, Mine, StatType.Speed));
    }

    [Fact]
    public void ThreeStatsRiseAtOnce()
    {
        // Overcharge: Sp. Atk, Sp. Def and Speed by one
        var core = Duel(Mon("Jolteon", 50, "Overcharge"), Idling(Mon("Snorlax", 50)));
        Turn(core, 0, 0);
        Assert.Equal((1, 1, 1, 0), (Stage(core, Mine, StatType.SpAttack), Stage(core, Mine, StatType.SpDefense), Stage(core, Mine, StatType.Speed), Stage(core, Mine, StatType.Attack)));
    }

    [Theory]
    [InlineData("Corrosive Haze", StatType.Defense, StatType.SpDefense)]
    [InlineData("Hoarfrost", StatType.Attack, StatType.Speed)]
    [InlineData("Dread Aura", StatType.SpDefense, StatType.Speed)]
    public void TwoStatsFallOnEveryFoe(string move, StatType first, StatType second)
    {
        var core = Doubles(new[] { Mon("Gengar", 50, move), Mon("Blissey", 50) }, new[] { Mon("Snorlax", 50), Mon("Machamp", 50) }, rules: Modern);
        DoubleTurn(core, (Mine, 0, Foe), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        foreach (var foe in new[] { Foe, Foe2 }) Assert.Equal((-1, -1), (Stage(core, foe, first), Stage(core, foe, second)));
        Assert.Equal((0, 0), (Stage(core, Mine2, first), Stage(core, Mine2, second)));
    }

    [Fact]
    public void RallyCryRaisesItsUserAndItsAlly()
    {
        var core = Doubles(new[] { Mon("Snorlax", 50, "Rally Cry"), Mon("Blissey", 50) }, new[] { Mon("Machamp", 50), Mon("Gengar", 50) }, rules: Modern);
        DoubleTurn(core, (Mine, 0, null), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        foreach (var mine in new[] { Mine, Mine2 }) Assert.Equal((1, 1), (Stage(core, mine, StatType.Attack), Stage(core, mine, StatType.SpAttack)));
        Assert.Equal(0, Stage(core, Foe, StatType.Attack));
    }

    [Fact]
    public void ASideEffectComesByItsChance()
    {
        // Cinder Stomp burns three times in ten: a side-effect roll of 0 is under 30, Calm's 99 isn't
        var core = Duel(Mon("Infernape", 50, "Cinder Stomp"), Idling(Mon("Snorlax", 50)), Calm().Force(RollKind.SideEffect, 0));
        Turn(core, 0, 0);
        Assert.Equal(StatusCondition.Burn, core.At(Foe).Pokemon!.Status);
        core = Duel(Mon("Infernape", 50, "Cinder Stomp"), Idling(Mon("Snorlax", 50)));
        Turn(core, 0, 0);
        Assert.Equal(StatusCondition.None, core.At(Foe).Pokemon!.Status);
    }

    [Fact]
    public void TitanTackleCostsAQuarterOfWhatItDoes()
    {
        var snorlax = Mon("Snorlax", 50, "Titan Tackle");
        var core = Duel(snorlax, Idling(Mon("Blissey", 50)));
        var foe = core.At(Foe).Pokemon!;
        Turn(core, 0, 0);
        int dealt = foe.MaxHP - foe.CurrentHP;
        Assert.True(dealt > 0);
        Assert.Equal(snorlax.MaxHP - dealt / 4, snorlax.CurrentHP);
    }

    [Fact]
    public void SoulSiphonDrainsHalfOfWhatItDoes()
    {
        var gengar = Mon("Gengar", 50, "Soul Siphon");
        gengar.CurrentHP = 20;
        var core = Duel(gengar, Idling(Mon("Machamp", 50)));
        var foe = core.At(Foe).Pokemon!;
        Turn(core, 0, 0);
        int dealt = foe.MaxHP - foe.CurrentHP;
        Assert.True(dealt > 0);
        Assert.Equal(20 + dealt / 2, gengar.CurrentHP);
    }

    [Fact]
    public void SnapstepGoesFirstAgainstAFasterFoe()
    {
        // Snorlax (Speed 35) before Jolteon (135): priority +1
        var said = Turn(Duel(Mon("Snorlax", 50, "Snapstep"), Mon("Jolteon", 50, "Tackle")), 0, 0);
        InOrder(said, "Snorlax used Snapstep!", "Foe Jolteon used Tackle!");
    }

    [Fact]
    public void FaultLineHitsItsUsersAllyToo()
    {
        var core = Doubles(new[] { Mon("Garchomp", 50, "Fault Line"), Mon("Snorlax", 50) }, new[] { Mon("Machamp", 50), Mon("Blissey", 50) }, rules: Modern);
        DoubleTurn(core, (Mine, 0, null), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        foreach (var hit in new[] { Mine2, Foe, Foe2 })
            Assert.True(core.At(hit).Pokemon!.CurrentHP < core.At(hit).Pokemon!.MaxHP, $"{hit} wasn't hit");
        Assert.Equal(core.At(Mine).Pokemon!.MaxHP, core.At(Mine).Pokemon!.CurrentHP);
    }

    [Fact]
    public void RivetShotHitsTwice()
    {
        // Two hits of 45, each the strongest roll: twice what one does
        var lucario = Mon("Lucario", 50, "Rivet Shot");
        var snorlax = Mon("Snorlax", 50);
        int once = DamageCalculator.CalculateDamage(lucario, snorlax, lucario.Moves[0], Steady(), Modern).Damage;
        var core = Duel(lucario, Idling(snorlax));
        Turn(core, 0, 0);
        Assert.Equal(snorlax.MaxHP - 2 * once, snorlax.CurrentHP);
    }

    [Fact]
    public void RootSnareKeepsItsTargetFromLeaving()
    {
        var core = Duel(Mon("Torterra", 50, "Root Snare"), Idling(Mon("Machamp", 50)));
        Assert.True(core.CanSwitchOut(core.At(Foe)));
        Assert.Contains("Foe Machamp can no longer escape!", Turn(core, 0, 0));
        Assert.False(core.CanSwitchOut(core.At(Foe)));
    }

    [Fact]
    public void MindbreakWaveHitsTheTargetsDefense()
    {
        // Alakazam (Sp. Atk 140) against Blissey (Defense 15, Sp. Def 140), level 50, power 110: floor(22 × 110 × 140
        // / 15) = 22586, / 50 = 451, + 2 = 453; × 1.5 for its own type = 679.5, rounded half down to 679. Against the
        // Sp. Def it would be floor(22 × 110 × 140 / 140) / 50 + 2 = 50, and 75
        var alakazam = Mon("Alakazam", 50, "Mindbreak Wave");
        Assert.Equal(679, DamageCalculator.CalculateDamage(alakazam, Mon("Blissey", 50), alakazam.Moves[0], Steady(), Modern).Damage);
    }

    [Fact]
    public void PilferStrikeTakesTheTargetsItem()
    {
        var tyranitar = Mon("Tyranitar", 50, "Pilfer Strike");
        var blastoise = With(Mon("Blastoise", 50), item: "Leftovers");
        Assert.Contains("Tyranitar stole Foe Blastoise's Leftovers!", Turn(Duel(tyranitar, Idling(blastoise))));
        Assert.Equal(("Leftovers", null), (tyranitar.HeldItem?.Name, blastoise.HeldItem?.Name));
    }

    // ------------------------------------------------------------------ the trainer AI

    [Theory]
    [InlineData("Alloy Plating", "Cosmic Power")]
    [InlineData("Kindle", "Growth")]
    [InlineData("Dust Veil", "Sand Attack")]
    [InlineData("Updraft", "Dragon Dance")]
    [InlineData("Molt Renewal", "Recover")]
    public void ATrainerWeighsAMoveOfOursAsThePlatinumMoveItsBattleEffectNames(string ours, string platinums)
    {
        // The AI's routines go by a move's battle effect (MoveEffectId), never by its name: a trainer with both moves
        // scores them alike, whatever the routines and the rolls
        Assert.Equal(MoveDatabase.Get(platinums).EffectId, MoveDatabase.Get(ours).EffectId);
        foreach (int roll in new[] { 0, 128, 255 })
        {
            var user = Of(Gender.Male, "Metagross", 60, ours, platinums);
            user.CurrentHP = user.MaxHP / 3;
            var core = Against(new[] { Of(Gender.Male, "Garchomp", 60, "Dragon Claw") }, new[] { user }, Calm().Force(RollKind.AiChoice, roll), Modern);
            core.AiMemory.CatchUp(core);
            var look = new AiThinking(core, core.AiMemory, core.EnemySlots[0], core.PlayerSlots[0], AiFlags.Basic | AiFlags.EvalAttack | AiFlags.Expert);
            look.Think();
            Assert.True(look.Scores[0] == look.Scores[1], $"{ours} {look.Scores[0]}, {platinums} {look.Scores[1]} at a roll of {roll}");
        }
    }
}
