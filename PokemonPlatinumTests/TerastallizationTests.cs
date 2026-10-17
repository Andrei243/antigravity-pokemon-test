using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Terastallization (plan 06 · R23), each by Pokémon Showdown's rule at the pinned commit (sim/battle-actions.ts'
/// canTerastallize, terastallize, getDamage's power of 60 and modifyDamage's same-type bonus; sim/pokemon.ts' getTypes
/// and runEffectiveness; data/moves.ts' Tera Blast and Tera Starstorm; data/abilities.ts' Tera Shift, Tera Shell,
/// Teraform Zero and Embody Aspect). One rule to a test, under the modern rules and on the rules alone, both sides
/// answered for from outside unless the trainer AI is what is tested; the rolls fixed by kind (<see cref="CoreScenario.Calm"/>:
/// no critical hit, the strongest roll). The Pokémon have no IVs, EVs or nature (<see cref="Scenario.Mon"/>): a stat
/// at level 50 is base + 5, HP base + 60, and the moves have the values the data gives in force (Platinum's for its
/// moves: Tackle's 35, Flamethrower's 95). The modern formula: floor(floor(22 × power × A / D) / 50) + 2, then the
/// same-type bonus in 4096ths rounded half down.
/// </summary>
public class TerastallizationTests
{
    private static readonly Ruleset Modern = Ruleset.Modern;

    /// <summary>A wild battle with the player holding a Tera Orb (or not), under the rules given.</summary>
    private static BattleCore Wild(Pokemon[] mine, Pokemon foe, bool orb = true, Ruleset? rules = null, bool band = false)
    {
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = PartyOf(mine), WildPokemon = new List<Pokemon> { foe }, Random = Calm(), Rules = rules ?? Modern,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas", TeraOrb = orb, DynamaxBand = band
        });
        core.Start();
        return core;
    }

    /// <summary>A battle against a trainer with a Tera Orb of their own; the player's side has one too.</summary>
    private static BattleCore Against(Pokemon[] mine, Pokemon[] theirs, IBattleController? foeMind = null, Ruleset? rules = null,
        BattleFormat format = BattleFormat.Single)
    {
        var trainer = new Trainer { Id = "ace", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 100, TeraOrb = true, DoubleBattle = format == BattleFormat.Double };
        foreach (var p in theirs) trainer.Party.Add(p);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = PartyOf(mine), Trainers = new List<Trainer> { trainer }, Random = Calm(), Rules = rules ?? Modern, Format = format,
            PlayerController = null, EnemyController = foeMind, PlayerName = "Lucas", TeraOrb = true
        });
        core.Start();
        return core;
    }

    private static Party PartyOf(params Pokemon[] mine)
    {
        var party = new Party();
        foreach (var p in mine) party.Add(p);
        return party;
    }

    /// <summary>A Pokémon given a Tera Type.</summary>
    private static Pokemon Tera(Pokemon p, PokemonType type)
    {
        p.TeraType = TeraType.Of(type);
        return p;
    }

    private static Pokemon Stellar(Pokemon p)
    {
        p.TeraType = TeraType.Stellar;
        return p;
    }

    private static List<string> TTurn(BattleCore core, int mine = 0, int foe = 0)
    {
        core.Submit(BattleChoice.Fight(Mine, mine) with { Terastallize = true }, BattleChoice.Fight(Foe, foe));
        return Lines(core.TakeLog());
    }

    // ---------------------------------------------------------------- the change

    [Fact]
    public void ATerastallizationIsSaidBeforeTheMovesAndCarriesItsEvent()
    {
        var mine = Tera(Mon("Snorlax", 50, "Splash"), PokemonType.Ghost);
        var core = Wild(new[] { mine }, Mon("Jolteon", 50, "Tackle"));
        core.Submit(BattleChoice.Fight(Mine, 0) with { Terastallize = true }, BattleChoice.Fight(Foe, 0));
        var log = core.TakeLog();
        var said = Lines(log);
        // Jolteon is faster, but every Terastallization comes before the turn's first move
        InOrder(said, "Snorlax Terastallized into the Ghost type!", "Foe Jolteon used Tackle!");
        var line = log.OfType<Said>().Single(s => s.Text == "Snorlax Terastallized into the Ghost type!");
        Assert.Contains(new Terastallized(Mine, "Ghost", null, mine.MaxHP, mine.CurrentHP), line.Shows);
        Assert.Equal(TeraType.Of(PokemonType.Ghost), BattleCore.TeraOf(core.At(Mine)));
    }

    [Fact]
    public void ATerastallizedPokemonIsItsTeraTypeForDefence()
    {
        // A Ghost Tera Type: the foe's Tackle doesn't affect a Snorlax any more
        var mine = Tera(Mon("Snorlax", 50, "Splash"), PokemonType.Ghost);
        var said = TTurn(Wild(new[] { mine }, Mon("Jolteon", 50, "Tackle")));
        Assert.Contains("It doesn't affect Snorlax...", said);
        Assert.Equal(mine.MaxHP, mine.CurrentHP);
    }

    [Fact]
    public void TheTeraTypeLastsThroughASwitchAndEndsWithTheBattle()
    {
        var mine = Tera(Mon("Snorlax", 50, "Splash", "Body Slam"), PokemonType.Ghost);
        var core = Wild(new[] { mine, Mon("Bidoof", 50, "Splash") }, Mon("Jolteon", 30, "Tackle"));
        TTurn(core);
        SwitchTo(core, 1);
        SwitchTo(core, 0);
        Assert.Contains("It doesn't affect Snorlax...", Turn(core));
        Assert.Equal(TeraType.Of(PokemonType.Ghost), mine.Terastallized);
        // Body Slam brings the Jolteon down and the battle ends
        Turn(core, mine: 1);
        Assert.Equal(BattleResult.PlayerVictory, core.Result);
        Assert.Null(mine.Terastallized);
    }

    // ---------------------------------------------------------------- the same-type bonus and the power of 60

    [Fact]
    public void AMoveOfANewTeraTypeTakesTheBonusAndAWeakOneHitsAsSixty()
    {
        // Snorlax (Sp. Atk 70) with a Fire Tera Type uses Ember (40) on a Snorlax (Sp. Def 115). Ember is under 60, of
        // its Tera Type, of no priority and one hit: power 60. floor(floor(22 × 60 × 70 / 115) / 50) + 2 = 16 + 2 = 18;
        // the Tera Type's bonus: 18 × 6144 / 4096 = 27. Without it: power 40, floor(floor(61600 / 115) / 50) + 2 = 12
        var foe = Mon("Snorlax", 50, "Splash");
        TTurn(Wild(new[] { Tera(Mon("Snorlax", 50, "Ember"), PokemonType.Fire) }, foe));
        Assert.Equal(foe.MaxHP - 27, foe.CurrentHP);

        var plain = Mon("Snorlax", 50, "Splash");
        Turn(Wild(new[] { Tera(Mon("Snorlax", 50, "Ember"), PokemonType.Fire) }, plain));
        Assert.Equal(plain.MaxHP - 12, plain.CurrentHP);
    }

    [Fact]
    public void ATeraTypeThatIsItsOwnDoublesTheBonus()
    {
        // Body Slam (85) from Snorlax (Attack 115) on a Snorlax (Defense 70): floor(floor(22 × 85 × 115 / 70) / 50) + 2
        // = 61 + 2 = 63. Terastallized into Normal, its own type: 63 × 2 = 126; not: 63 × 6144 / 4096 = 94.5, half down 94
        var foe = Mon("Snorlax", 50, "Splash");
        TTurn(Wild(new[] { Tera(Mon("Snorlax", 50, "Body Slam"), PokemonType.Normal) }, foe));
        Assert.Equal(foe.MaxHP - 126, foe.CurrentHP);
    }

    [Fact]
    public void ItsOwnTypesKeepTheirBonusUnderAnotherTeraType()
    {
        // The same Body Slam from a Snorlax Terastallized into Fire: Normal is still its own type, 1.5: 94
        var foe = Mon("Snorlax", 50, "Splash");
        TTurn(Wild(new[] { Tera(Mon("Snorlax", 50, "Body Slam"), PokemonType.Fire) }, foe));
        Assert.Equal(foe.MaxHP - 94, foe.CurrentHP);
    }

    [Fact]
    public void AMoveWithPriorityKeepsItsOwnPower()
    {
        // Quick Attack (40, +1) of its Tera Type isn't raised to 60: floor(floor(22 × 40 × 115 / 70) / 50) + 2 = 30,
        // twice for the Normal Tera Type that is its own: 60. Tackle (35) is raised: power 60, 45 × 2 = 90
        var foe = Mon("Snorlax", 50, "Splash");
        TTurn(Wild(new[] { Tera(Mon("Snorlax", 50, "Quick Attack"), PokemonType.Normal) }, foe));
        Assert.Equal(foe.MaxHP - 60, foe.CurrentHP);
        var other = Mon("Snorlax", 50, "Splash");
        TTurn(Wild(new[] { Tera(Mon("Snorlax", 50, "Tackle"), PokemonType.Normal) }, other));
        Assert.Equal(other.MaxHP - 90, other.CurrentHP);
    }

    [Fact]
    public void AdaptabilityGoesByTheTeraTypeAlone()
    {
        // Porygon-Z (Sp. Atk 140, Adaptability) uses Hyper Voice (90) on a Snorlax (Sp. Def 115):
        // floor(floor(22 × 90 × 140 / 115) / 50) + 2 = 48 + 2 = 50. Terastallized into Normal: 2 becomes 2.25, 50 × 9216
        // / 4096 = 112.5, half down 112. Into Fire: Normal is its own type (1.5) but not its type now, so Adaptability
        // has no say: 75
        var foe = Mon("Snorlax", 50, "Splash");
        TTurn(Wild(new[] { Tera(With(Mon("Porygon-Z", 50, "Hyper Voice"), ability: "Adaptability"), PokemonType.Normal) }, foe));
        Assert.Equal(foe.MaxHP - 112, foe.CurrentHP);
        var other = Mon("Snorlax", 50, "Splash");
        TTurn(Wild(new[] { Tera(With(Mon("Porygon-Z", 50, "Hyper Voice"), ability: "Adaptability"), PokemonType.Fire) }, other));
        Assert.Equal(other.MaxHP - 75, other.CurrentHP);
    }

    // ---------------------------------------------------------------- Stellar

    [Fact]
    public void StellarBoostsAreWorkedOutByHand()
    {
        // Against a Snorlax, as above: Body Slam 126 then 94; Flamethrower 32 then 27
        var foe = Mon("Snorlax", 50, "Splash");
        foe.HeldItem = null;
        var core = Wild(new[] { Stellar(Mon("Snorlax", 50, "Flamethrower")) }, foe);
        TTurn(core);
        Assert.Equal(foe.MaxHP - 32, foe.CurrentHP);
        int before = foe.CurrentHP;
        Turn(core);
        Assert.Equal(before - 27, foe.CurrentHP);

        var target = Mon("Snorlax", 50, "Splash");
        var slam = Wild(new[] { Stellar(Mon("Snorlax", 50, "Body Slam")) }, target);
        TTurn(slam);
        Assert.Equal(target.MaxHP - 126, target.CurrentHP);
        before = target.CurrentHP;
        Turn(slam);
        Assert.Equal(before - 94, target.CurrentHP);
    }

    [Fact]
    public void AStellarPokemonKeepsItsOwnTypesForDefence()
    {
        var mine = Stellar(Mon("Gengar", 50, "Splash"));
        var said = TTurn(Wild(new[] { mine }, Mon("Jolteon", 50, "Tackle")));
        Assert.Contains("Gengar Terastallized into the Stellar type!", said);
        Assert.Contains("It doesn't affect Gengar...", said);
    }

    // ---------------------------------------------------------------- Tera Blast

    [Fact]
    public void TeraBlastTakesTheTeraTypeAndHitsPhysicallyWithTheHigherAttack()
    {
        // Snorlax (Attack 115 against Sp. Atk 70) Terastallized into Fire: a physical Fire move of 80 on a Snorlax
        // (Defense 70): floor(floor(22 × 80 × 115 / 70) / 50) + 2 = 57 + 2 = 59, × 1.5 = 88.5, half down 88
        var foe = Mon("Snorlax", 50, "Splash");
        var core = Wild(new[] { Tera(Mon("Snorlax", 50, "Tera Blast"), PokemonType.Fire) }, foe);
        core.Submit(BattleChoice.Fight(Mine, 0) with { Terastallize = true }, BattleChoice.Fight(Foe, 0));
        var log = core.TakeLog();
        Assert.Contains(log.OfType<Said>().SelectMany(s => s.Shows), e => e is Lunged { Category: MoveCategory.Physical });
        Assert.Equal(foe.MaxHP - 88, foe.CurrentHP);
    }

    [Fact]
    public void AStellarTeraBlastHitsAGhostAndLowersItsUsersAttacks()
    {
        // Stellar: power 100, no type in its way: Gengar (Defense 65) from Attack 115:
        // floor(floor(22 × 100 × 115 / 65) / 50) + 2 = 77 + 2 = 79; its one boost, 1.2: 79 × 4915 / 4096 = 94.8, 95
        var foe = Mon("Gengar", 50, "Splash");
        var mine = Stellar(Mon("Snorlax", 50, "Tera Blast"));
        TTurn(Wild(new[] { mine }, foe));
        Assert.Equal(foe.MaxHP - 95, foe.CurrentHP);
        Assert.Equal(-1, mine.StatStages.GetValueOrDefault(StatType.Attack));
        Assert.Equal(-1, mine.StatStages.GetValueOrDefault(StatType.SpAttack));
    }

    [Fact]
    public void AStellarMoveIsSuperEffectiveAgainstATerastallizedTarget()
    {
        var mine = Stellar(Mon("Snorlax", 50, "Tera Blast"));
        var foe = Tera(Mon("Snorlax", 50, "Splash"), PokemonType.Normal);
        var core = Against(new[] { mine }, new[] { foe });
        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0) with { Terastallize = true });
        core.TakeLog();
        core.Submit(BattleChoice.Fight(Mine, 0) with { Terastallize = true }, BattleChoice.Fight(Foe, 0));
        Assert.Contains("It's super effective!", Lines(core.TakeLog()));
    }

    [Fact]
    public void TeraBlastBeforeATerastallizationIsANormalSpecialMove()
    {
        // Sp. Atk 70 on Sp. Def 115, power 80: floor(floor(22 × 80 × 70 / 115) / 50) + 2 = 21 + 2 = 23, × 1.5 = 34.5, 34
        var foe = Mon("Snorlax", 50, "Splash");
        Turn(Wild(new[] { Tera(Mon("Snorlax", 50, "Tera Blast"), PokemonType.Fire) }, foe));
        Assert.Equal(foe.MaxHP - 34, foe.CurrentHP);
    }

    // ---------------------------------------------------------------- who may

    [Fact]
    public void ATrainerTerastallizesOnlyOnceInABattle()
    {
        var core = Wild(new[] { Tera(Mon("Snorlax", 50, "Splash"), PokemonType.Fire), Mon("Bidoof", 50, "Splash") }, Mon("Magikarp", 50, "Splash"));
        TTurn(core);
        Assert.Equal("Snorlax can't Terastallize!", core.WhyNot(BattleChoice.Fight(Mine, 0) with { Terastallize = true }));
        SwitchTo(core, 1);
        Assert.Equal("You can Terastallize only once in a battle!", core.WhyNot(BattleChoice.Fight(Mine, 0) with { Terastallize = true }));
    }

    [Fact]
    public void TwoTerastallizationsOfOneTrainersInATurnAreRefused()
    {
        var core = Against(new[] { Mon("Bidoof", 50, "Tackle"), Mon("Bidoof", 50, "Tackle") },
            new[] { Mon("Magikarp", 50, "Splash"), Mon("Magikarp", 50, "Splash") }, format: BattleFormat.Double);
        Assert.Throws<ArgumentException>(() => core.Submit(BattleChoice.Fight(Mine, 0) with { Terastallize = true }, BattleChoice.Fight(Mine2, 0) with { Terastallize = true },
            BattleChoice.Fight(Foe, 0), BattleChoice.Fight(Foe2, 0)));
    }

    [Fact]
    public void WithoutATeraOrbOrUnderPlatinumsRulesNobodyTerastallizes()
    {
        var core = Wild(new[] { Mon("Bidoof", 50, "Tackle") }, Mon("Snorlax", 50, "Splash"), orb: false);
        Assert.Equal("Bidoof can't Terastallize!", core.WhyNot(BattleChoice.Fight(Mine, 0) with { Terastallize = true }));
        var platinum = Wild(new[] { Mon("Bidoof", 50, "Tackle") }, Mon("Snorlax", 50, "Splash"), rules: Ruleset.Platinum);
        Assert.False(platinum.CanTerastallize(platinum.At(Mine)));
    }

    [Fact]
    public void AMegaStonesHolderAZCrystalsHolderAndADynamaxCantGoWithIt()
    {
        var charizard = Mon("Charizard", 50, "Ember");
        charizard.HeldItem = ItemDatabase.Get("Charizardite X");
        var mega = Wild(new[] { charizard }, Mon("Magikarp", 50, "Splash"));
        Assert.False(mega.CanTerastallize(mega.At(Mine)));
        var bidoof = Mon("Bidoof", 50, "Tackle");
        bidoof.HeldItem = ItemDatabase.Get("Normalium Z");
        var z = Wild(new[] { bidoof }, Mon("Magikarp", 50, "Splash"));
        Assert.False(z.CanTerastallize(z.At(Mine)));
        // A choice that would Dynamax and Terastallize at once is refused; a Terastallized Pokémon can't Dynamax after
        var both = Wild(new[] { Mon("Snorlax", 50, "Splash") }, Mon("Magikarp", 50, "Splash"), band: true);
        Assert.NotNull(both.WhyNot(BattleChoice.Fight(Mine, 0) with { Terastallize = true, Dynamax = true }));
        TTurn(both);
        Assert.False(both.CanDynamax(both.At(Mine)));
    }

    [Fact]
    public void ATeraTypeIsTheFirstTypeUntilOneIsGiven()
    {
        var gengar = Mon("Gengar", 50, "Splash");
        Assert.Equal(TeraType.Of(PokemonType.Ghost), gengar.TeraType);
        Assert.False(gengar.HasOwnTeraType);
        gengar.TeraType = TeraType.Of(PokemonType.Fairy);
        Assert.Equal(TeraType.Of(PokemonType.Fairy), gengar.TeraType);
        // Ogerpon's is its mask's, Terapagos's Stellar, whatever is given
        var ogerpon = Mon("Ogerpon", 50, "Splash");
        ogerpon.TeraType = TeraType.Of(PokemonType.Dragon);
        Assert.Equal(TeraType.Of(PokemonType.Grass), ogerpon.TeraType);
        ogerpon.ChangeForm("Ogerpon-Hearthflame-Mask");
        Assert.Equal(TeraType.Of(PokemonType.Fire), ogerpon.TeraType);
        Assert.Equal(TeraType.Stellar, Mon("Terapagos", 50, "Splash").TeraType);
    }

    [Fact]
    public void ATeraTypeIsSavedAndCopied()
    {
        var p = Mon("Bidoof", 50, "Tackle");
        p.TeraType = TeraType.Stellar;
        var back = SavedPokemonData.FromPokemon(p).ToPokemon();
        Assert.Equal(TeraType.Stellar, back.TeraType);
        Assert.True(back.HasOwnTeraType);
        Assert.Null(SavedPokemonData.FromPokemon(Mon("Bidoof", 50, "Tackle")).TeraType);
        var copy = Mon("Bidoof", 50, "Tackle");
        copy.CopyStateFrom(p);
        Assert.Equal(TeraType.Stellar, copy.TeraType);
    }

    // ---------------------------------------------------------------- Ogerpon and Terapagos

    [Fact]
    public void OgerponsMaskShinesAsItTerastallizes()
    {
        // The Teal Mask's Embody Aspect raises its Speed; its type is Grass whatever was given
        var ogerpon = Mon("Ogerpon", 50, "Splash");
        var said = TTurn(Wild(new[] { ogerpon }, Mon("Magikarp", 50, "Splash")));
        InOrder(said, "Ogerpon Terastallized into the Grass type!", "Ogerpon's Speed rose!");
        Assert.Equal(1, ogerpon.StatStages.GetValueOrDefault(StatType.Speed));
        Assert.Equal("Embody Aspect", ogerpon.AbilityName);
    }

    [Fact]
    public void TerapagosTakesItsTerastalFormAsItComesInAndItsStellarFormAsItTerastallizes()
    {
        // Tera Shift: 90 + 60 = 150 HP becomes the Terastal Form's 95 + 60 = 155, its HP raised with it; Terastallized,
        // the Stellar Form's 160 + 60 = 220, and Teraform Zero ends the rain the foe brought
        var terapagos = Mon("Terapagos", 50, "Splash");
        var core = Wild(new[] { terapagos }, Mon("Politoed", 50, "Rain Dance", "Splash"));
        Assert.Contains("Terapagos transformed!", Lines(core.Log));
        Assert.Equal(("Terapagos-Terastal", 155, 155), (terapagos.Form, terapagos.MaxHP, terapagos.CurrentHP));
        Assert.Equal("Tera Shell", terapagos.AbilityName);
        Turn(core);
        Assert.Equal(BattleWeather.Rain, core.Field.Weather);
        var said = TTurn(core, foe: 1);
        InOrder(said, "Terapagos Terastallized into the Stellar type!", "Terapagos's Teraform Zero cleared the weather and the terrain away!");
        Assert.Equal(("Terapagos-Stellar", 220, 220), (terapagos.Form, terapagos.MaxHP, terapagos.CurrentHP));
        Assert.NotEqual(BattleWeather.Rain, core.Field.Weather);
        core.Submit(BattleChoice.Run(Mine), BattleChoice.Fight(Foe, 0));
        Assert.Null(terapagos.Form);
    }

    [Fact]
    public void TeraShellTurnsAHitAsideAtFullHp()
    {
        // Terapagos in its Terastal Form (Defense 115) takes Jolteon's Tackle (Attack 70, 35): floor(floor(22 × 35 × 70
        // / 115) / 50) + 2 = 9 + 2 = 11, not very effective: 5
        var terapagos = Mon("Terapagos", 50, "Splash");
        var said = Turn(Wild(new[] { terapagos }, Mon("Jolteon", 50, "Tackle")));
        Assert.Contains("It's not very effective...", said);
        Assert.Equal(terapagos.MaxHP - 5, terapagos.CurrentHP);
    }

    [Fact]
    public void TeraStarstormIsAStellarMoveOnlyForTerapagosInItsStellarForm()
    {
        // From anyone else Tera Starstorm is a Normal move: it doesn't affect a Gengar
        var smeargle = Mon("Smeargle", 50, "Tera Starstorm");
        Assert.Contains("It doesn't affect Foe Gengar...", Turn(Wild(new[] { smeargle }, Mon("Gengar", 50, "Splash"))));
        // From a Stellar Terapagos it is of the Stellar type and reaches it
        var terapagos = Mon("Terapagos", 50, "Tera Starstorm");
        var gengar = Mon("Gengar", 50, "Splash");
        var said = TTurn(Wild(new[] { terapagos }, gengar));
        Assert.DoesNotContain("It doesn't affect Foe Gengar...", said);
        Assert.True(gengar.CurrentHP < gengar.MaxHP);
    }

    // ---------------------------------------------------------------- the AI, a replay and the screen

    [Fact]
    public void TheTrainerAiTerastallizesWheneverItCan()
    {
        var said = Turn(Against(new[] { Mon("Snorlax", 50, "Splash") }, new[] { Mon("Bidoof", 50, "Tackle") }, TrainerAi.Instance));
        Assert.Contains("Foe Bidoof Terastallized into the Normal type!", said);
    }

    [Fact]
    public void TheTrainerAiNeverTerastallizesUnderPlatinumsRules()
    {
        var said = Turn(Against(new[] { Mon("Snorlax", 50, "Splash") }, new[] { Mon("Bidoof", 50, "Tackle") }, TrainerAi.Instance, Ruleset.Platinum));
        Assert.DoesNotContain(said, l => l.Contains("Terastallized"));
    }

    [Fact]
    public void ABattleWithATerastallizationReplaysTheSame()
    {
        BattleCore Make() => new(new CoreSetup
        {
            PlayerParty = PartyOf(Stellar(Mon("Snorlax", 50, "Body Slam", "Flamethrower"))),
            WildPokemon = new List<Pokemon> { Mon("Snorlax", 50, "Tackle") }, Random = new BattleRandom(7), Rules = Modern,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas", TeraOrb = true
        });
        var first = Make();
        first.Start();
        first.Submit(BattleChoice.Fight(Mine, 0) with { Terastallize = true }, BattleChoice.Fight(Foe, 0));
        first.Submit(BattleChoice.Fight(Mine, 1), BattleChoice.Fight(Foe, 0));
        first.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        var again = Make();
        again.Replay(first.Record!);
        Assert.Equal(Lines(first.Log), Lines(again.Log));
        Assert.Contains("Snorlax Terastallized into the Stellar type!", Lines(again.Log));
    }

    [Fact]
    public void TheMoveMenusItemButtonArmsTheTerastallizationAndTheScreenTakesTheType()
    {
        // A Tera Orb in the bag is the player's; with no other mechanic to be had the item button arms it
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get(BattleCore.TeraOrbItem)!);
        var snorlax = Tera(Mon("Snorlax", 50, "Splash"), PokemonType.Ghost);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = PartyOf(snorlax), Inventory = bag, Pokedex = new Pokedex(),
            WildPokemon = new List<Pokemon> { Mon("Jolteon", 50, "Tackle") }, Random = Calm(), Rules = Modern
        });
        Settle(battle);
        battle.SelectMainMenuOption(0);
        Assert.False(battle.CanDynamaxNow);
        Assert.True(battle.CanTerastallizeNow);
        Assert.True(battle.ToggleLaterMechanic());
        Assert.True(battle.TeraArmed);
        Assert.Equal(TeraType.Of(PokemonType.Ghost), battle.MenuTeraType);
        battle.SelectMove(0);
        var said = Settle(battle);
        Assert.Contains("Snorlax Terastallized into the Ghost type!", said);
        Assert.Contains("It doesn't affect Snorlax...", said);
        Assert.Equal(TeraType.Of(PokemonType.Ghost), snorlax.Terastallized);
    }
}
