using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Mega Dynamax (plan 06 · R20c, our own; docs/plans/R20c-mega-dynamax-proposals.md, approved 2026-10-25;
/// docs/mechanics/rulings.md, "Mega Dynamax"): a Mega that Dynamaxes with the Mega Max Band. One rule to a test, under the
/// modern rules and on the rules alone, both sides answered for from outside unless the trainer AI is what is tested; the
/// rolls fixed by kind (<see cref="CoreScenario.Calm"/>). The Pokémon have no IVs, EVs or nature (<see cref="Scenario.Mon"/>):
/// at level 50 HP is base + 60, and every Mega keeps its species' base HP.
/// </summary>
public class MegaDynamaxTests
{
    private static readonly Ruleset Modern = Ruleset.Modern;

    private static Party PartyOf(params Pokemon[] mine)
    {
        var party = new Party();
        foreach (var p in mine) party.Add(p);
        return party;
    }

    private static Pokemon Holding(Pokemon p, string item, int dynamaxLevel = 10, bool gigantamax = false)
    {
        p.HeldItem = ItemDatabase.Get(item) ?? throw new ArgumentException(item);
        p.DynamaxLevel = dynamaxLevel;
        p.GigantamaxFactor = gigantamax;
        return p;
    }

    /// <summary>A wild battle with the player holding the Key Stone, the Dynamax Band and the Mega Max Band (or not).</summary>
    private static BattleCore Wild(Pokemon[] mine, Pokemon foe, bool megaMaxBand = true, Ruleset? rules = null, bool den = false, IBattleController? foeMind = null)
    {
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = PartyOf(mine), WildPokemon = new List<Pokemon> { foe }, Random = Calm(), Rules = rules ?? Modern,
            PlayerController = null, EnemyController = foeMind, PlayerName = "Lucas", KeyStone = true, DynamaxBand = true, MegaMaxBand = megaMaxBand,
            MegaMaxDen = den
        });
        core.Start();
        return core;
    }

    /// <summary>A battle against a trainer with the three items (or only the Key Stone and the Dynamax Band).</summary>
    private static BattleCore Against(Pokemon[] mine, Pokemon[] theirs, bool band = true, Ruleset? rules = null)
    {
        var trainer = new Trainer { Id = "ace", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 100, KeyStone = true, DynamaxBand = true, MegaMaxBand = band };
        foreach (var p in theirs) trainer.Party.Add(p);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = PartyOf(mine), Trainers = new List<Trainer> { trainer }, Random = Calm(), Rules = rules ?? Modern,
            PlayerController = null, EnemyController = TrainerAi.Instance, PlayerName = "Lucas"
        });
        core.Start();
        return core;
    }

    private static BattleChoice MegaMax(int move = 0) => BattleChoice.Fight(Mine, move) with { Mega = true, Dynamax = true };

    [Fact]
    public void AMegaEvolutionAndADynamaxInOneChoiceComeInShowdownsOrderAndTakeTheMegaDynamaxForm()
    {
        // Charizard's base HP 78: 138 at level 50, its Mega's too; at Dynamax Level 10, twice that, 276 of 276
        var mine = Holding(Mon("Charizard", 50, "Flamethrower"), "Charizardite Y");
        var core = Wild(new[] { mine }, Mon("Snorlax", 50, "Splash"));
        core.Submit(MegaMax(), BattleChoice.Fight(Foe, 0));
        var log = core.TakeLog();
        var said = Lines(log);
        InOrder(said, "Charizard has Mega Evolved into Mega Charizard Y!", "Charizard swelled into its Mega Dynamax form!", "Charizard used Max Flare!");
        var line = log.OfType<Said>().Single(s => s.Text == "Charizard swelled into its Mega Dynamax form!");
        Assert.Contains(new Dynamaxed(Mine, "Charizard-Mega-Y-Mmax", false, 276, 276), line.Shows);
        Assert.Equal("Charizard-Mega-Y-Mmax", mine.Form);
        Assert.Equal((276, 276), (mine.MaxHP, mine.CurrentHP));
        // Its stats, types and ability are its Mega's (Drought acted as it Mega Evolved)
        Assert.Equal("Drought", mine.AbilityName);
        Assert.Equal(PokemonType.Flying, mine.SecondaryType);
    }

    [Fact]
    public void ItEndsAtItsThirdTurnBackInItsMegaFormWhichStays()
    {
        var mine = Holding(Mon("Charizard", 50, "Flamethrower", "Growl"), "Charizardite X");
        var core = Wild(new[] { mine }, Mon("Snorlax", 50, "Splash"));
        core.Submit(MegaMax(1), BattleChoice.Fight(Foe, 0));
        core.Submit(BattleChoice.Fight(Mine, 1), BattleChoice.Fight(Foe, 0));
        core.Submit(BattleChoice.Fight(Mine, 1), BattleChoice.Fight(Foe, 0));
        var log = core.TakeLog();
        Assert.Equal(new DynamaxEnded(Mine, "Charizard-Mega-X", 138, 138), log.OfType<Said>().SelectMany(s => s.Shows).Concat(log).OfType<DynamaxEnded>().Single());
        Assert.Equal("Charizard-Mega-X", mine.Form);
        // Its trainer's Dynamax is spent: no second one, though it is a Mega still
        Assert.Equal("You can Dynamax only once in a battle!", core.WhyNot(BattleChoice.Fight(Mine, 0) with { Dynamax = true }));
    }

    [Fact]
    public void AMegaOfAnEarlierTurnMayDynamaxLater()
    {
        var mine = Holding(Mon("Charizard", 50, "Flamethrower"), "Charizardite X");
        var core = Wild(new[] { mine }, Mon("Snorlax", 50, "Splash"));
        core.Submit(BattleChoice.Fight(Mine, 0) with { Mega = true }, BattleChoice.Fight(Foe, 0));
        Assert.Equal("Charizard-Mega-X", mine.Form);
        Assert.True(core.CanDynamax(core.At(Mine)));
        core.Submit(BattleChoice.Fight(Mine, 0) with { Dynamax = true }, BattleChoice.Fight(Foe, 0));
        Assert.Equal("Charizard-Mega-X-Mmax", mine.Form);
    }

    [Fact]
    public void AMegaWithNoFormOfItsOwnGrowsInItsMegaForm()
    {
        // Mega Clefable has no Mega Dynamax form (the table's "no"): it Mega Dynamaxes all the same, in its Mega form
        var mine = Holding(Mon("Clefable", 50, "Moonblast"), "Clefablite");
        var core = Wild(new[] { mine }, Mon("Snorlax", 50, "Splash"));
        core.Submit(MegaMax(), BattleChoice.Fight(Foe, 0));
        var log = core.TakeLog();
        Assert.Contains("Clefable swelled into its Mega Dynamax form!", Lines(log));
        Assert.Contains(log.OfType<Said>().SelectMany(s => s.Shows), e => e is Dynamaxed { Form: "Clefable-Mega" });
    }

    [Fact]
    public void WithoutTheMegaMaxBandOrUnderPlatinumsRulesAMegaCantDynamax()
    {
        var mine = Holding(Mon("Charizard", 50, "Flamethrower"), "Charizardite X");
        var core = Wild(new[] { mine }, Mon("Snorlax", 50, "Splash"), megaMaxBand: false);
        Assert.Equal("Charizard can't Mega Dynamax!", core.WhyNot(MegaMax()));
        // A Mega Evolution alone is still there, and a plain Dynamax is refused to a stone's holder as before
        Assert.Null(core.WhyNot(BattleChoice.Fight(Mine, 0) with { Mega = true }));
        Assert.False(core.CanDynamax(core.At(Mine)));
        var platinum = Wild(new[] { Holding(Mon("Charizard", 50, "Flamethrower"), "Charizardite X") }, Mon("Snorlax", 50, "Splash"), rules: Ruleset.Platinum);
        Assert.False(platinum.CanMegaDynamax(platinum.At(Mine)));
        Assert.False(Ruleset.Platinum.MegaDynamax);
        Assert.True(Ruleset.Modern.MegaDynamax);
    }

    [Fact]
    public void ALegendarysMegaAndAPrimalFormCantMegaDynamax()
    {
        var latios = Wild(new[] { Holding(Mon("Latios", 50, "Dragon Pulse"), "Latiosite") }, Mon("Snorlax", 50, "Splash"));
        Assert.Equal("Latios can't Mega Dynamax!", latios.WhyNot(MegaMax()));
        var kyogre = Holding(Mon("Kyogre", 50, "Surf"), "Blue Orb");
        var primal = Wild(new[] { kyogre }, Mon("Snorlax", 50, "Splash"));
        Assert.Equal("Kyogre-Primal", kyogre.Form);
        Assert.False(primal.CanDynamax(primal.At(Mine)));
    }

    [Fact]
    public void HavingDynamaxedAnotherPokemonATrainerCantMegaDynamax()
    {
        var snorlax = Mon("Snorlax", 50, "Splash");
        snorlax.DynamaxLevel = 10;
        var charizard = Holding(Mon("Charizard", 50, "Flamethrower"), "Charizardite X");
        var trainer = new Trainer { Id = "foe", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 100 };
        trainer.Party.Add(Mon("Magikarp", 50, "Splash"));
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = PartyOf(snorlax, charizard), Trainers = new List<Trainer> { trainer }, Random = Calm(), Rules = Modern,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas", KeyStone = true, DynamaxBand = true, MegaMaxBand = true
        });
        core.Start();
        core.Submit(BattleChoice.Fight(Mine, 0) with { Dynamax = true }, BattleChoice.Fight(Foe, 0));
        core.Submit(BattleChoice.Switch(Mine, 1), BattleChoice.Fight(Foe, 0));
        Assert.Equal("You can Dynamax only once in a battle!", core.WhyNot(MegaMax()));
        Assert.Null(core.WhyNot(BattleChoice.Fight(Mine, 0) with { Mega = true }));
    }

    [Fact]
    public void AGigantamaxCharizardHoldingItsStoneMegaDynamaxesWithItsTypesMaxMove()
    {
        // The Gigantamax Factor goes unused for the battle: Max Flare, never G-Max Wildfire
        var mine = Holding(Mon("Charizard", 50, "Flamethrower"), "Charizardite Y", gigantamax: true);
        var core = Wild(new[] { mine }, Mon("Snorlax", 50, "Splash"));
        core.Submit(MegaMax(), BattleChoice.Fight(Foe, 0));
        var said = Lines(core.TakeLog());
        Assert.Contains("Charizard used Max Flare!", said);
        Assert.DoesNotContain(said, l => l.Contains("G-Max"));
        Assert.Equal("Charizard-Mega-Y-Mmax", mine.Form);
    }

    [Fact]
    public void ParentalBondLeavesAMaxMoveAlone()
    {
        // Showdown's parentalbond returns for move.isMax: Mega Kangaskhan's Max Strike hits once
        var core = Wild(new[] { Holding(Mon("Kangaskhan", 50, "Tackle"), "Kangaskhanite") }, Mon("Snorlax", 50, "Splash"));
        core.Submit(MegaMax(), BattleChoice.Fight(Foe, 0));
        var said = Lines(core.TakeLog());
        Assert.Contains("Kangaskhan used Max Strike!", said);
        Assert.DoesNotContain(said, l => l.StartsWith("Hit "));
    }

    [Fact]
    public void AnAteAbilitysTypeChoosesTheMaxMove()
    {
        // Showdown's runMove types the move by the ability before it picks the Max Move: Mega Salamence's Aerilate
        // makes Tackle a Flying move, so Max Airstream, and its side's Speed a stage up
        var mine = Holding(Mon("Salamence", 50, "Tackle"), "Salamencite");
        var core = Wild(new[] { mine }, Mon("Snorlax", 50, "Splash"));
        core.Submit(MegaMax(), BattleChoice.Fight(Foe, 0));
        Assert.Contains("Salamence used Max Airstream!", Lines(core.TakeLog()));
        Assert.Equal(1, mine.StatStages.GetValueOrDefault(StatType.Speed));
    }

    [Fact]
    public void TheTrainerAiMegaDynamaxesItsMegaTheFirstTurnItCan()
    {
        var foe = Holding(Mon("Charizard", 50, "Flamethrower"), "Charizardite X");
        var said = Turn(Against(new[] { Mon("Snorlax", 50, "Splash") }, new[] { foe }));
        InOrder(said, "Foe Charizard has Mega Evolved into Mega Charizard X!", "Foe Charizard swelled into its Mega Dynamax form!");
        Assert.Equal("Charizard-Mega-X-Mmax", foe.Form);
    }

    [Fact]
    public void WithTheBandTheTrainerAiKeepsItsDynamaxForItsMega()
    {
        // Bidoof leads; Charizard, holding its stone, waits in the party: the band keeps the Dynamax for it
        Pokemon Bidoof() { var p = Mon("Bidoof", 50, "Tackle"); p.DynamaxLevel = 10; return p; }
        var kept = Turn(Against(new[] { Mon("Snorlax", 50, "Splash") }, new[] { Bidoof(), Holding(Mon("Charizard", 50, "Flamethrower"), "Charizardite X") }));
        Assert.DoesNotContain(kept, l => l.Contains("Dynamax"));
        // Without the band the first that can Dynamax does, as before
        var today = Turn(Against(new[] { Mon("Snorlax", 50, "Splash") }, new[] { Bidoof(), Holding(Mon("Charizard", 50, "Flamethrower"), "Charizardite X") }, band: false));
        Assert.Contains("Foe Bidoof swelled to a giant size with Dynamax energy!", today);
    }

    [Fact]
    public void ABattleWithAMegaDynamaxReplaysTheSame()
    {
        BattleCore Make() => new(new CoreSetup
        {
            PlayerParty = PartyOf(Holding(Mon("Charizard", 50, "Flamethrower", "Growl"), "Charizardite X")),
            WildPokemon = new List<Pokemon> { Mon("Snorlax", 50, "Tackle") }, Random = new BattleRandom(7), Rules = Modern,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas", KeyStone = true, DynamaxBand = true, MegaMaxBand = true
        });
        var first = Make();
        first.Start();
        first.Submit(MegaMax(), BattleChoice.Fight(Foe, 0));
        first.Submit(BattleChoice.Fight(Mine, 1), BattleChoice.Fight(Foe, 0));
        var again = Make();
        again.Replay(first.Record!);
        Assert.Equal(Lines(first.Log), Lines(again.Log));
        Assert.Contains("Charizard swelled into its Mega Dynamax form!", Lines(again.Log));
    }

    [Fact]
    public void TheVsRecordersFileKeepsTheBandAndTheDen()
    {
        var setup = new CoreSetup
        {
            PlayerParty = PartyOf(Mon("Bidoof", 50, "Tackle")), WildPokemon = new List<Pokemon> { Mon("Snorlax", 50, "Tackle") },
            Random = new BattleRandom(7), Rules = Modern, PlayerName = "Lucas", MegaMaxBand = true, MegaMaxDen = true
        };
        var file = BattleRecordFile.Opening(setup);
        Assert.True(file.MegaMaxBand);
        Assert.True(file.MegaMaxDen);
        var trainer = new Trainer { Id = "t", Name = "Vera", TrainerClass = "Ace Trainer", MegaMaxBand = true };
        Assert.True(RecordedTrainer.Of(trainer).ToTrainer(Modern).MegaMaxBand);
    }

    [Fact]
    public void AWildPokemonMegaDynamaxesByItselfOnlyInADen()
    {
        // Question 14: a Galar den's boss may Mega Evolve and Mega Dynamax alone; anywhere else a wild Pokémon never does
        var boss = Holding(Mon("Charizard", 50, "Flamethrower"), "Charizardite X");
        var den = Wild(new[] { Mon("Snorlax", 50, "Splash") }, boss, den: true, foeMind: TrainerAi.Instance);
        den.Submit(BattleChoice.Fight(Mine, 0));
        var said = Lines(den.TakeLog());
        InOrder(said, "Foe Charizard's Charizardite X is reacting to the den's light!", "Foe Charizard swelled into its Mega Dynamax form!");
        Assert.Equal("Charizard-Mega-X-Mmax", boss.Form);

        var plain = Holding(Mon("Charizard", 50, "Flamethrower"), "Charizardite X");
        var field = Wild(new[] { Mon("Snorlax", 50, "Splash") }, plain, foeMind: TrainerAi.Instance);
        field.Submit(BattleChoice.Fight(Mine, 0));
        Assert.Null(plain.Form);
    }

    [Fact]
    public void AChampionsMegaDynamaxTeamIsFoughtOnceThePlayerHoldsTheBand()
    {
        var bag = new Inventory();
        Assert.Null(BattleCore.MegaMaxTeamOf("champion_cynthia_rematch", bag, Modern));
        bag.AddItem(ItemDatabase.Get(BattleCore.MegaMaxBandItem)!);
        Assert.Equal("champion_cynthia_megamax", BattleCore.MegaMaxTeamOf("champion_cynthia_rematch", bag, Modern)?.Id);
        Assert.Equal("kanto_champion_megamax_squirtle", BattleCore.MegaMaxTeamOf("kanto_champion_rematch_squirtle", bag, Modern)?.Id);
        Assert.Equal("johto_champion_lance_megamax", BattleCore.MegaMaxTeamOf("johto_champion_lance", bag, Modern)?.Id);
        Assert.Null(BattleCore.MegaMaxTeamOf("champion_cynthia_rematch", bag, Ruleset.Platinum));
        Assert.Null(BattleCore.MegaMaxTeamOf("leader_roark", bag, Modern));
        // Each team has the three items and its ace holds its stone (3.2's table)
        foreach (var (id, ace, stone) in new[]
        {
            ("champion_cynthia_megamax", "Garchomp", "Garchompite"), ("kanto_champion_megamax_squirtle", "Pidgeot", "Pidgeotite"),
            ("kanto_champion_megamax_bulbasaur", "Pidgeot", "Pidgeotite"), ("kanto_champion_megamax_charmander", "Pidgeot", "Pidgeotite"),
            ("johto_champion_lance_megamax", "Dragonite", "Dragoninite"), ("hoenn_steven_megamax", "Metagross", "Metagrossite"),
            ("unova_champion_iris_megamax", "Aggron", "Aggronite")
        })
        {
            var record = TrainerDatabase.Get(id)!;
            Assert.True(record.KeyStone && record.DynamaxBand && record.MegaMaxBand, id);
            Assert.Contains(record.Party, p => p.Species == ace && p.Item == stone);
        }
    }

    [Fact]
    public void EveryMegaDynamaxFormIsItsMegasDataGrown()
    {
        var forms = PokemonDatabase.GetAll().SelectMany(s => (s.Forms ?? new()).Select(f => (s, f))).Where(x => x.f.Kind == FormKind.MegaDynamax).ToList();
        Assert.Equal(63, forms.Count);
        foreach (var (species, form) in forms)
        {
            Assert.EndsWith("-Mmax", form.Name);
            var mega = species.Form(form.Name[..^"-Mmax".Length])!;
            Assert.Equal(FormKind.Mega, mega.Kind);
            Assert.Equal(mega.Types, form.Types);
            Assert.Equal(mega.BaseStats?.ToString(), form.BaseStats?.ToString());
            Assert.Equal(mega.Abilities, form.Abilities);
            Assert.Equal(1000f, form.Weight);
            Assert.InRange(form.Height ?? 0, 20f, 60f);
            Assert.False(species.Legendary || species.Mythical, form.Name);
            // A Pokémon in it shows its Mega's model until it has a sculpt of its own
            var p = new Pokemon(species, 50) { Form = form.Name };
            Assert.Equal(PokemonPlatinumEngine.Models.MegaDynamaxLooks.HasSculpt(form.Name) ? form.Name : mega.Name, p.ModelName);
        }
    }

    [Fact]
    public void TheMegaMaxBandIsAKeyItemOfOurs()
    {
        var band = ItemDatabase.Get(BattleCore.MegaMaxBandItem)!;
        Assert.Equal(ItemPocket.KeyItems, band.Pocket);
    }
}
