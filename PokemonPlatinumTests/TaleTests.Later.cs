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
/// The seven tales of CP11's second session (plan 46; <c>docs/mechanics/rulings.md</c>, "The Tales"): the Miorița's
/// strength, Muma Pădurii's forest, Făt-Frumos' youth, the Uriași' weight, the Spiriduș' item, the Sânziene's healing and
/// the Draco's rising banner, each where its rule bites, gone when the tale is, and weighed by the trainer AI.
/// </summary>
public partial class TaleTests
{
    // ---------------------------------------------------------------- the Sânziene's: healing doubled

    [Fact]
    public void TheSanzienesMidsummerDoublesAHeal()
    {
        int Healed(string? tale)
        {
            var mine = Mon("Porygon", 50, "Recover");
            mine.CurrentHP = 1;
            var core = Tales(new[] { mine }, new[] { Mon("Snorlax", 50, "Splash") }, myTales: All);
            Tell(core, mine: tale);
            return mine.CurrentHP - 1;
        }
        int plain = Healed(null), told = Healed("sanziene");
        var porygon = Mon("Porygon", 50, "Recover");
        Assert.Equal(porygon.MaxHP / 2, plain);
        Assert.Equal(porygon.MaxHP - 1, told);
    }

    // ---------------------------------------------------------------- Făt-Frumos': no stat may fall

    [Fact]
    public void FatFrumosYouthLetsNoStatFallNotEvenAPokemonsOwn()
    {
        var mine = Mon("Monferno", 15, "Close Combat", "Splash");
        var foe = Mon("Snorlax", 90, "Growl");
        var core = Tales(new[] { mine }, new[] { foe }, myTales: All);
        var (said, _) = Tell(core, mine: "fatfrumos");
        Assert.Equal(0, mine.StatStages.GetValueOrDefault(StatType.Attack));
        Assert.Equal(0, mine.StatStages.GetValueOrDefault(StatType.Defense));
        Assert.Equal(0, mine.StatStages.GetValueOrDefault(StatType.SpDefense));
        Assert.Contains("The tale of Făt-Frumos keeps Monferno's stats from falling!", said);
        // And once it has ended, they fall again
        while (core.Field.Tale != null) Tell(core, myMove: 1);
        Tell(core, myMove: 1);
        Assert.True(mine.StatStages.GetValueOrDefault(StatType.Attack) < 0);
    }

    // ---------------------------------------------------------------- the Uriași': the heaviest hit hardest

    [Fact]
    public void TheUriasiGiantsLetTheHeavierHitHarderEitherWay()
    {
        (int ByHeavy, int ByLight) Lost(string? tale)
        {
            var heavy = Mon("Snorlax", 50, "Tackle");
            var light = Mon("Pikachu", 80, "Tackle");
            var core = Tales(new[] { heavy }, new[] { light }, myTales: All);
            int lightBefore = light.CurrentHP, heavyBefore = heavy.CurrentHP;
            Tell(core, mine: tale);
            Assert.Equal(tale == null ? 100 : 130, BattleCore.TaleSidePercent(core.Field, core.At(Mine), core.At(Foe)));
            Assert.Equal(100, BattleCore.TaleSidePercent(core.Field, core.At(Foe), core.At(Mine)));
            return (lightBefore - light.CurrentHP, heavyBefore - heavy.CurrentHP);
        }
        var plain = Lost(null);
        var told = Lost("uriasi");
        Assert.InRange(told.ByHeavy / (double)plain.ByHeavy, 1.2, 1.4);
        Assert.Equal(plain.ByLight, told.ByLight);
    }

    // ---------------------------------------------------------------- the Spiriduș': items work twice

    [Fact]
    public void TheSpiridusKeepsAHeldItemTheFirstTimeItWouldBeUsedUp()
    {
        // A weak foe's Tackle takes the Bidoof under half its HP, and its Oran Berry goes off
        var mine = With(Mon("Bidoof", 30, "Splash"), item: "Oran Berry");
        mine.CurrentHP = mine.MaxHP / 2 + 1;
        var core = Tales(new[] { mine }, new[] { Mon("Snorlax", 8, "Tackle") }, myTales: All);
        var (said, _) = Tell(core, mine: "spiridus");
        Assert.Contains("The tale of the Spiriduș kept Bidoof's Oran Berry for another time!", said);
        Assert.Contains(said, l => l.Contains("Oran Berry") && l.Contains("restored"));
        Assert.Equal("Oran Berry", mine.HeldItem?.Name);
        // The second time it is eaten as ever
        mine.CurrentHP = mine.MaxHP / 2 + 1;
        Tell(core);
        Assert.Null(mine.HeldItem);
    }

    // ---------------------------------------------------------------- Muma Pădurii's: Grass heals, the foe is held

    [Fact]
    public void MumaPaduriisForestHealsGrassAndHoldsTheTellersFoesButNotAGhost()
    {
        var turtwig = Mon("Turtwig", 30, "Splash");
        turtwig.CurrentHP = turtwig.MaxHP / 2;
        var core = Tales(new[] { turtwig, Mon("Bidoof", 30, "Splash") }, new[] { Mon("Snorlax", 50, "Splash"), Mon("Bidoof", 30, "Splash") }, myTales: All);
        var (said, _) = Tell(core, mine: "mumapadurii");
        Assert.Equal(turtwig.MaxHP / 2 + turtwig.MaxHP / 16, turtwig.CurrentHP);
        Assert.Contains("The forest of Muma Pădurii restored Turtwig's HP!", said);
        // The foe is held; the teller's own side isn't
        Assert.False(core.CanSwitchOut(core.At(Foe)));
        Assert.True(core.CanSwitchOut(core.At(Mine)));
        Assert.Equal("The forest of Muma Pădurii holds Foe Snorlax fast!", core.WhyNot(BattleChoice.Switch(Foe, 1)));

        var ghostly = Tales(new[] { Mon("Turtwig", 30, "Splash") }, new[] { Mon("Gastly", 30, "Splash"), Mon("Bidoof", 30, "Splash") }, myTales: All);
        Tell(ghostly, mine: "mumapadurii");
        Assert.True(ghostly.CanSwitchOut(ghostly.At(Foe)));
    }

    // ---------------------------------------------------------------- the Miorița's: an ally takes its strength

    [Fact]
    public void TheMioritasLamentGivesTheFallensStrengthToTheNextToComeIn()
    {
        var bidoof = Mon("Bidoof", 30, "Splash");
        bidoof.CurrentHP = 1;
        var monferno = Mon("Monferno", 30, "Splash");
        var core = Tales(new[] { bidoof, monferno }, new[] { Mon("Snorlax", 50, "Tackle") }, myTales: All);
        Tell(core, mine: "miorita");
        Assert.True(bidoof.IsFainted);
        var said = SendIn(core, 1);
        Assert.Contains("Monferno took up the strength of Bidoof!", said);
        // Bidoof's Attack is the higher of its two
        Assert.Equal(1, monferno.StatStages.GetValueOrDefault(StatType.Attack));
        Assert.Equal(0, monferno.StatStages.GetValueOrDefault(StatType.SpAttack));
    }

    [Fact]
    public void WithNoMioritaToldAFallIsOnlyAFall()
    {
        var bidoof = Mon("Bidoof", 30, "Splash");
        bidoof.CurrentHP = 1;
        var monferno = Mon("Monferno", 30, "Splash");
        var core = Tales(new[] { bidoof, monferno }, new[] { Mon("Snorlax", 50, "Tackle") }, myTales: All);
        Tell(core, mine: "zmeu");
        SendIn(core, 1);
        Assert.Equal(0, monferno.StatStages.GetValueOrDefault(StatType.Attack));
    }

    // ---------------------------------------------------------------- the Draco's: the teller's attacks rise each turn

    [Fact]
    public void TheDracosBannerRaisesTheTellersAttacksEveryTurnAndNotTheFoes()
    {
        var core = Tales(new[] { Mon("Monferno", 50, "Splash") }, new[] { Mon("Snorlax", 60, "Splash") }, myTales: All);
        // Told, its turn's moves hit at 110%; each turn end raises the next turn's by a tenth, to 150% on its fifth
        Tell(core, mine: "draco");
        Assert.Equal(120, BattleCore.TaleSidePercent(core.Field, core.At(Mine), core.At(Foe)));
        Assert.Equal(100, BattleCore.TaleSidePercent(core.Field, core.At(Foe), core.At(Mine)));
        Tell(core);
        Assert.Equal(130, BattleCore.TaleSidePercent(core.Field, core.At(Mine), core.At(Foe)));
        Tell(core);
        Tell(core);
        Assert.Equal(150, BattleCore.TaleSidePercent(core.Field, core.At(Mine), core.At(Foe)));
        Tell(core);
        Assert.Null(core.Field.Tale);

        int Lost(string? tale)
        {
            var foe = Mon("Snorlax", 60, "Splash");
            var c = Tales(new[] { Mon("Monferno", 50, "Mega Kick") }, new[] { foe }, myTales: All);
            int before = foe.CurrentHP;
            Tell(c, mine: tale);
            return before - foe.CurrentHP;
        }
        Assert.InRange(Lost("draco") / (double)Lost(null), 1.05, 1.15);
    }

    // ---------------------------------------------------------------- the trainer AI

    [Fact]
    public void TheAiWeighsTheLaterTales()
    {
        // A foe with attacks tells the Draco's, which favours its teller alone
        var core = Tales(new[] { Mon("Snorlax", 50, "Splash") }, new[] { Mon("Ponyta", 30, "Tackle") }, theirTales: new[] { "draco" }, ai: true);
        Assert.Equal("draco", TrainerAi.TaleNow(core, core.At(Foe)));
        // Muma Pădurii's told by the player holds the foe: worth less than nothing to it, so it answers
        var held = Tales(new[] { Mon("Turtwig", 30, "Splash") }, new[] { Mon("Snorlax", 50, "Splash") }, myTales: All, theirTales: new[] { "draco" });
        Tell(held, mine: "mumapadurii");
        Assert.True(TrainerAi.TaleWorth(held, BattleSide.Enemy, held.Field.Tale!.Rule, held.Field.Tale.Teller) < 0);
        // The Draco's told by the other side is worth less than nothing to the one it doesn't favour
        var rising = Tales(new[] { Mon("Monferno", 30, "Ember") }, new[] { Mon("Snorlax", 50, "Splash") }, myTales: All);
        Tell(rising, mine: "draco");
        Assert.True(TrainerAi.TaleWorth(rising, BattleSide.Enemy, rising.Field.Tale!.Rule, BattleSide.Player) < 0);
    }
}
