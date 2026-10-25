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
/// The fifteen Megas of our own (plan 06 · R20b: docs/plans/R20b-mega-proposals.md, approved by the user in full on
/// 2026-10-26): each a form of its species (tools/DataImporter/Overrides/forms.json) brought out by a stone of its own
/// (Overrides/items.json), with the document's typing, ability and stats; and the gap of R20 the proposal found, closed:
/// a stone of a species' own works for no regional form of it.
/// </summary>
public class MegaOfOurOwnTests
{
    private static readonly Ruleset Modern = Ruleset.Modern;

    /// <summary>The document's table: species, stone, types (the species' own where null), ability, the six stats.</summary>
    public static readonly TheoryData<string, string, string?, string, int[]> Table = new()
    {
        { "Typhlosion", "Typhlosionite", "Fire/Ground", "Drought", new[] { 78, 84, 98, 149, 105, 120 } },
        { "Torterra", "Torterrite", null, "Solid Rock", new[] { 95, 129, 145, 75, 115, 66 } },
        { "Infernape", "Infernapite", null, "Iron Fist", new[] { 76, 129, 81, 129, 81, 138 } },
        { "Empoleon", "Empoleonite", null, "Competitive", new[] { 84, 86, 113, 146, 126, 75 } },
        { "Serperior", "Serperiorite", "Grass/Dragon", "Contrary", new[] { 75, 75, 110, 115, 110, 143 } },
        { "Samurott", "Samurottite", "Water/Steel", "Sharpness", new[] { 95, 140, 105, 108, 85, 95 } },
        { "Decidueye", "Decidueyite", null, "Infiltrator", new[] { 78, 142, 90, 100, 110, 110 } },
        { "Incineroar", "Incineroarite", null, "Intimidate", new[] { 95, 145, 110, 80, 110, 90 } },
        { "Primarina", "Primarinite", null, "Liquid Voice", new[] { 80, 74, 94, 156, 136, 90 } },
        { "Rillaboom", "Rillaboomite", null, "Grassy Surge", new[] { 100, 160, 110, 60, 90, 110 } },
        { "Cinderace", "Cinderacite", null, "Libero", new[] { 80, 151, 95, 65, 95, 144 } },
        { "Inteleon", "Inteleonite", null, "Sniper", new[] { 70, 95, 80, 160, 80, 145 } },
        { "Meowscarada", "Meowscaradite", null, "Magician", new[] { 76, 150, 85, 81, 90, 148 } },
        { "Skeledirge", "Skeledirgite", null, "Punk Rock", new[] { 104, 75, 115, 145, 105, 86 } },
        { "Quaquaval", "Quaquavalite", null, "Dancer", new[] { 85, 155, 100, 85, 90, 115 } },
    };

    private static int[] Six(StatSpread s) => new[] { s.HP, s.Attack, s.Defense, s.SpAttack, s.SpDefense, s.Speed };

    [Theory]
    [MemberData(nameof(Table))]
    public void EachStoneBringsOutItsMegaAsTheDocumentGivesIt(string species, string stoneName, string? types, string ability, int[] stats)
    {
        var owner = PokemonDatabase.Get(species)!;
        var form = owner.Form(species + "-Mega");
        Assert.NotNull(form);
        Assert.Equal(FormKind.Mega, form!.Kind);

        // The stone: an item of the Items pocket, for that species and form alone, and none other brings it out
        var stone = ItemDatabase.Get(stoneName);
        Assert.NotNull(stone);
        Assert.Equal(species, stone!.MegaStone!.Species);
        Assert.Equal(species + "-Mega", stone.MegaStone.Form);
        Assert.Null(stone.MegaStone.HeldByForm);
        Assert.Equal(ItemPocket.Items, stone.Pocket);
        Assert.Single(ItemDatabase.GetAll(), i => i.MegaStone?.Form == form.Name);

        // The stats: the document's, the species' total plus 100, HP unchanged
        var own = owner.Under(Modern);
        int[] before = { own.BaseHP, own.BaseAttack, own.BaseDefense, own.BaseSpAttack, own.BaseSpDefense, own.BaseSpeed };
        Assert.Equal(stats, Six(form.BaseStats!));
        Assert.Equal(before.Sum() + 100, stats.Sum());
        Assert.Equal(before[0], stats[0]);
        Assert.All(stats.Zip(before), p => Assert.True(p.First >= p.Second, $"{form.Name} loses a stat"));

        // The typing (kept where the document keeps it) and the one ability, already in abilities.json
        if (types == null) Assert.Null(form.Types);
        else Assert.Equal(types.Split('/').Select(Enum.Parse<PokemonType>), form.Types!);
        Assert.Equal(new[] { ability }, form.Abilities);
        Assert.NotNull(AbilityDatabase.Get(ability));
        Assert.Null(form.Learnset);

        // Its two written entries for the FORMS page
        Assert.Equal(2, DexEntries.EntriesOf(form.Name)!.Count);
    }

    [Fact]
    public void TheFifteenAreAllThereIsOfOurs()
    {
        var names = Table.Select(r => r.Data.Item1 + "-Mega").ToHashSet();
        Assert.Equal(15, names.Count);
        // No two share an ability, and only Typhlosion's Drought is shared with a starter Mega of the games' (Charizard Y)
        Assert.Equal(15, Table.Select(r => r.Data.Item4).Distinct().Count());
        // The stones' names fit the bag, as Chesnaughtite's 13 letters do
        Assert.All(Table, r => Assert.True(r.Data.Item2.Length <= 13, r.Data.Item2));
    }

    [Fact]
    public void AMegaOfOurOwnMegaEvolvesInBattle()
    {
        // Typhlosion at 50 (no IVs, EVs or nature): Sp. Atk 109 + 5 = 114. Mega Typhlosion (149, Drought, Fire and
        // Ground): 149 + 5 = 154, and the sun comes out as the Mega Evolution happens, as on entry
        var mine = Holding(Mon("Typhlosion", 50, "Splash"), "Typhlosionite");
        var core = Wild(mine, Mon("Magikarp", 50, "Splash"));
        Assert.Equal(114, mine.SpAttack);
        Assert.True(core.CanMegaEvolve(core.At(Mine)));
        core.Submit(BattleChoice.Fight(Mine, 0) with { Mega = true }, BattleChoice.Fight(Foe, 0));
        var said = Lines(core.TakeLog());
        InOrder(said, "Typhlosion's Typhlosionite is reacting to Lucas's Key Stone!", "Typhlosion has Mega Evolved into Mega Typhlosion!",
            "Typhlosion used Splash!");
        Assert.Equal("Typhlosion-Mega", mine.Form);
        Assert.Equal(154, mine.SpAttack);
        Assert.Equal("Drought", mine.AbilityName);
        Assert.True(core.At(Mine).HasType(PokemonType.Ground));
        Assert.Equal(BattleWeather.Sun, core.Field.Weather);
    }

    [Fact]
    public void TheTrainerAiMegaEvolvesAMegaOfOurOwn()
    {
        var foe = Holding(Mon("Torterra", 50, "Splash"), "Torterrite");
        var party = new Party();
        party.Add(Mon("Bidoof", 50, "Splash"));
        var trainer = new Trainer { Id = "ace", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 100, KeyStone = true };
        trainer.Party.Add(foe);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = new List<Trainer> { trainer }, Random = Calm(), Rules = Modern,
            PlayerController = null, EnemyController = TrainerAi.Instance, PlayerName = "Lucas", KeyStone = true
        });
        core.Start();
        Assert.Contains("Foe Torterra has Mega Evolved into Mega Torterra!", Turn(core));
        Assert.Equal("Torterra-Mega", foe.Form);
        Assert.Equal("Solid Rock", foe.AbilityName);
    }

    [Fact]
    public void AStoneOfTheSpeciesOwnDoesNothingForItsRegionalForm()
    {
        // Showdown keys a stone by the species' own name: a Hisuian Typhlosion holding Typhlosionite, a Galarian
        // Slowbro holding Slowbronite and an Alolan Raichu holding Raichunite X don't Mega Evolve (plan 06 · R20b)
        Assert.Null(BattleCore.MegaFormOf(InForm(Holding(Mon("Typhlosion", 50, "Splash"), "Typhlosionite"), "Typhlosion-Hisui")));
        Assert.Null(BattleCore.MegaFormOf(InForm(Holding(Mon("Slowbro", 50, "Splash"), "Slowbronite"), "Slowbro-Galar")));
        Assert.Null(BattleCore.MegaFormOf(InForm(Holding(Mon("Raichu", 50, "Splash"), "Raichunite X"), "Raichu-Alola")));
        // Their own species still do
        Assert.Equal("Typhlosion-Mega", BattleCore.MegaFormOf(Holding(Mon("Typhlosion", 50, "Splash"), "Typhlosionite")));
        Assert.Equal("Slowbro-Mega", BattleCore.MegaFormOf(Holding(Mon("Slowbro", 50, "Splash"), "Slowbronite")));
    }

    private static Pokemon Holding(Pokemon p, string item)
    {
        p.HeldItem = ItemDatabase.Get(item) ?? throw new ArgumentException(item);
        return p;
    }

    private static Pokemon InForm(Pokemon p, string form)
    {
        p.ChangeForm(form);
        return p;
    }

    private static BattleCore Wild(Pokemon mine, Pokemon foe)
    {
        var party = new Party();
        party.Add(mine);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { foe }, Random = Calm(), Rules = Modern,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas", KeyStone = true
        });
        core.Start();
        return core;
    }
}
