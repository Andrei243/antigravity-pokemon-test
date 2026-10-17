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
/// The field of the later games (plan 06 · R19b): snow in hail's place under the modern rules, the strong weathers,
/// the four terrains, Aurora Veil and Sticky Web, each set through the core's own entry points (the moves and
/// abilities that set them are R24 to R27's) and each told of by the line that sets or clears it, so a look that
/// follows the events alone is the rules' field. The rules are Showdown's (data/conditions.ts, data/moves.ts).
/// </summary>
public class LaterFieldTests
{
    /// <summary>A look that knows the field only by what the log's events tell, held to the rules' field.</summary>
    private static void Follows(BattleCore core, FieldLook look, ref int read)
    {
        for (; read < core.Log.Count; read++)
        {
            if (core.Log[read] is Said said)
            {
                foreach (var shown in said.Shows) look.Tell(shown);
                foreach (var landing in said.OnImpact) look.Tell(landing);
            }
            else look.Tell(core.Log[read]);
        }
        Assert.True(look.Difference(core.Field) == null, look.Difference(core.Field));
    }

    private static List<string> Said(BattleCore core) => Lines(core.TakeLog());

    // ------------------------------------------------------------------ snow

    [Fact]
    public void UnderTheModernRulesHailIsSnowWhichHurtsNobody()
    {
        var core = Wild(Mon("Glalie", 50, "Hail"), Idling(Mon("Snorlax", 50)), rules: Ruleset.Modern);
        var said = Turn(core, foe: 0);
        Assert.Contains("It started to snow!", said);
        Assert.Equal(BattleWeather.Snow, core.Field.Weather);
        // The rules that ask after hail ask after snow too (Blizzard, Weather Ball, Ice Body, Aurora Veil)
        Assert.Equal(BattleWeather.Hail, core.Field.WeatherInEffect);
        said = Turn(core, foe: 0);
        Assert.Contains("Snow continues to fall.", said);
        Assert.DoesNotContain(said, l => l.Contains("pelted"));

        // Platinum's rules keep their hail, and its wear
        var platinum = Wild(Mon("Glalie", 50, "Hail"), Idling(Mon("Snorlax", 50)));
        said = Turn(platinum, foe: 0);
        Assert.Contains("It started to hail!", said);
        Assert.Contains("Foe Snorlax is pelted by the hail!", said);
    }

    [Fact]
    public void SnowWarningBringsSnowUnderTheModernRules()
    {
        var core = Wild(Idling(Mon("Snorlax", 50)), With(Mon("Snover", 50), ability: "Snow Warning"), rules: Ruleset.Modern);
        Assert.Contains("Foe Snover's Snow Warning brought the snow!", Lines(core.Log));
        Assert.Equal(BattleWeather.Snow, core.Field.Weather);
    }

    [Fact]
    public void SnowHardensAnIceTypeAgainstPhysicalMovesByHalf()
    {
        // Showdown's snow: onModifyDef 1.5 for an Ice type
        int Hit(BattleWeather sky)
        {
            var core = Wild(Mon("Snorlax", 50, "Tackle", "Ember"), Mon("Glalie", 50), rules: Ruleset.Modern, sky: sky);
            var snorlax = core.At(Mine);
            var physical = DamageCalculator.Calculate(snorlax, core.At(Foe), snorlax.Pokemon!.Moves[0], Steady(), spread: false, rules: Ruleset.Modern).Damage;
            return physical;
        }
        int clear = Hit(BattleWeather.None), snowing = Hit(BattleWeather.Snow);
        Assert.True(snowing < clear, $"{snowing} under snow, {clear} under a clear sky");
        Assert.InRange(snowing * 3, clear * 2 - 3, clear * 2 + 3);
    }

    // ------------------------------------------------------------------ the strong weathers

    [Fact]
    public void TheHarshSunBurnsWaterMovesAwayKeepsOtherWeathersOffAndGoesWithItsHolder()
    {
        var groudon = With(Mon("Groudon", 50), ability: "Desolate Land");
        var core = Wild(groudon, Mon("Kyogre", 50, "Water Gun", "Rain Dance"), bench: Mon("Snorlax", 50));
        var look = new FieldLook();
        int read = 0;
        // Desolate Land brings it as Groudon comes in (plan 06 · R20), on a line of its own; asked again it is there already
        Assert.Equal(BattleWeather.HarshSun, core.Field.Weather);
        Assert.Contains(core.Log.OfType<Said>().SelectMany(s => s.Shows), e => e is WeatherChanged { Weather: BattleWeather.HarshSun });
        Assert.False(core.SetStrongWeather(core.At(Mine), BattleWeather.HarshSun));
        Assert.Equal(BattleWeather.Sun, core.Field.WeatherInEffect);
        Follows(core, look, ref read);

        var said = Turn(core, foe: 0);
        Assert.Contains("The Water-type attack evaporated in the harsh sunlight!", said);
        Assert.Equal(core.At(Mine).Pokemon!.MaxHP, core.At(Mine).Pokemon!.CurrentHP);
        Assert.Contains("The sunlight is extremely harsh.", said);

        said = Turn(core, foe: 1);
        Assert.Contains("But it failed!", said);
        Assert.Equal(BattleWeather.HarshSun, core.Field.Weather);

        // Its holder goes, and so does the sun
        said = SwitchTo(core, 1);
        Assert.Contains("The extremely harsh sunlight faded.", said);
        Assert.Equal(BattleWeather.None, core.Field.Weather);
    }

    [Fact]
    public void TheHeavyRainPutsFireMovesOut()
    {
        var core = Wild(With(Mon("Kyogre", 50), ability: "Primordial Sea"), Mon("Charizard", 50, "Ember"));
        core.SetStrongWeather(core.At(Mine), BattleWeather.HeavyRain);
        Assert.Contains("The Fire-type attack fizzled out in the heavy rain!", Turn(core));
    }

    [Fact]
    public void TheStrongWindsTakeTheFlyingTypesWeaknessesAway()
    {
        // A holder with an ability that brings nothing, so the winds come only when they are asked for
        var core = Wild(With(Mon("Rayquaza", 50), ability: "Pressure"), Mon("Glalie", 50, "Ice Beam", "Rock Throw"));
        var zapdos = core.At(Mine);
        var iceBeam = core.At(Foe).Pokemon!.Moves[0];
        Assert.Equal(4f, DamageCalculator.Effectiveness(core.At(Foe), zapdos, iceBeam));
        core.SetStrongWeather(zapdos, BattleWeather.StrongWinds);
        // Rayquaza is Dragon and Flying: the Dragon type's weakness stands, the Flying type's goes
        Assert.Equal(2f, DamageCalculator.Effectiveness(core.At(Foe), zapdos, iceBeam));
        Assert.Equal(BattleWeather.None, core.Field.WeatherInEffect);
    }

    // ------------------------------------------------------------------ the terrains

    [Fact]
    public void ATerrainLastsFiveTurnsAndIsToldOfAsItComesAndGoes()
    {
        var core = Wild(Idling(Mon("Snorlax", 50)), Idling(Mon("Shuckle", 50)));
        var look = new FieldLook();
        int read = 0;
        Assert.True(core.SetTerrain(FieldCondition.ElectricTerrain, core.At(Mine)));
        Assert.False(core.SetTerrain(FieldCondition.ElectricTerrain, core.At(Mine)));
        Assert.Contains("An electric current ran across the battlefield!", Lines(core.Log));
        Follows(core, look, ref read);
        Assert.True(look.Of(FieldCondition.ElectricTerrain).Up);

        // Another terrain takes its place on the same line
        core.SetTerrain(FieldCondition.GrassyTerrain, null);
        var last = core.Log.OfType<Said>().Last();
        Assert.Equal(new BattleEvent[] { new FieldConditionChanged(FieldCondition.ElectricTerrain, false), new FieldConditionChanged(FieldCondition.GrassyTerrain, true) },
            last.Shows.OfType<FieldConditionChanged>());
        Follows(core, look, ref read);

        for (int turn = 0; turn < 4; turn++)
        {
            Turn(core);
            Assert.True(core.Field.Has(FieldCondition.GrassyTerrain));
        }
        Assert.Contains("The grass disappeared from the battlefield.", Turn(core));
        Assert.Null(core.Field.Terrain);
        Follows(core, look, ref read);
        Assert.False(look.Of(FieldCondition.GrassyTerrain).Up);
    }

    [Fact]
    public void ATerrainExtenderMakesItEightTurns()
    {
        var core = Wild(With(Idling(Mon("Snorlax", 50)), item: "Terrain Extender"), Idling(Mon("Shuckle", 50)));
        core.SetTerrain(FieldCondition.PsychicTerrain, core.At(Mine));
        Assert.Equal(8, core.Field.TerrainTurns);
    }

    [Fact]
    public void ElectricTerrainStrengthensElectricMovesOfThoseOnTheGroundAndKeepsThemAwake()
    {
        int Hit(string attacker, bool terrain)
        {
            var core = Wild(Mon(attacker, 50, "Thunder Shock"), Mon("Snorlax", 50));
            if (terrain) core.SetTerrain(FieldCondition.ElectricTerrain, null);
            var user = core.At(Mine);
            return DamageCalculator.Calculate(user, core.At(Foe), user.Pokemon!.Moves[0], Steady(), spread: false).Damage;
        }
        Assert.True(Hit("Pikachu", true) > Hit("Pikachu", false));
        // Zapdos flies over it
        Assert.Equal(Hit("Zapdos", false), Hit("Zapdos", true));

        var sleepy = Wild(Idling(Mon("Snorlax", 50)), Mon("Breloom", 50, "Spore"));
        sleepy.SetTerrain(FieldCondition.ElectricTerrain, null);
        Assert.Contains("Snorlax surrounds itself with electrified terrain!", Turn(sleepy));
        Assert.Equal(StatusCondition.None, sleepy.At(Mine).Pokemon!.Status);
    }

    [Fact]
    public void MistyTerrainKeepsEveryConditionAndConfusionOff()
    {
        var core = Wild(Idling(Mon("Snorlax", 50)), Mon("Pikachu", 50, "Thunder Wave", "Confuse Ray"));
        core.SetTerrain(FieldCondition.MistyTerrain, null);
        Assert.Contains("Snorlax surrounds itself with a protective mist!", Turn(core, foe: 0));
        Assert.Equal(StatusCondition.None, core.At(Mine).Pokemon!.Status);
        Assert.Contains("Snorlax surrounds itself with a protective mist!", Turn(core, foe: 1));
        Assert.False(core.At(Mine).IsConfused);
    }

    [Fact]
    public void GrassyTerrainGivesASixteenthBackAtEachTurnsEnd()
    {
        var core = Wild(Idling(Mon("Snorlax", 50)), Idling(Mon("Shuckle", 50)));
        var snorlax = core.At(Mine).Pokemon!;
        snorlax.CurrentHP = 50;
        core.SetTerrain(FieldCondition.GrassyTerrain, null);
        Assert.Contains("Snorlax's HP was restored by the grassy terrain.", Turn(core));
        Assert.Equal(50 + snorlax.MaxHP / 16, snorlax.CurrentHP);
    }

    [Fact]
    public void PsychicTerrainKeepsAMoveThatGoesFirstOffAFoeOnTheGround()
    {
        var core = Wild(Idling(Mon("Snorlax", 50)), Mon("Rattata", 50, "Quick Attack"));
        core.SetTerrain(FieldCondition.PsychicTerrain, null);
        Assert.Contains("Snorlax is protected by the Psychic Terrain!", Turn(core));
        Assert.Equal(core.At(Mine).Pokemon!.MaxHP, core.At(Mine).Pokemon!.CurrentHP);
    }

    [Fact]
    public void NaturePowerBecomesTheTerrainsMove()
    {
        var core = Wild(Mon("Snorlax", 50, "Nature Power"), Idling(Mon("Shuckle", 50)));
        core.SetTerrain(FieldCondition.MistyTerrain, null);
        Assert.Contains(Turn(core), l => l.Contains("turned into") && l.Contains("Moonblast"));
    }

    // ------------------------------------------------------------------ Aurora Veil and Sticky Web

    [Fact]
    public void AuroraVeilGoesUpOnlyInTheSnowHalvesBothKindsOfHitAndWearsOff()
    {
        var core = Wild(Idling(Mon("Glalie", 50)), Mon("Snorlax", 50, "Tackle"), rules: Ruleset.Modern);
        var look = new FieldLook();
        int read = 0;
        Assert.False(core.RaiseAuroraVeil(core.At(Mine)));

        int Hit() => DamageCalculator.Calculate(core.At(Foe), core.At(Mine), core.At(Foe).Pokemon!.Moves[0], Steady(), spread: false, rules: Ruleset.Modern).Damage;
        core.SetWeather(BattleWeather.Hail, 5, "It started to hail!");
        Assert.Equal(BattleWeather.Snow, core.Field.Weather);
        int bare = Hit();
        Assert.True(core.RaiseAuroraVeil(core.At(Mine)));
        Assert.InRange(Hit(), bare / 2 - 1, bare / 2 + 1);
        Follows(core, look, ref read);
        Assert.True(look.Of(BattleSide.Player, SideCondition.AuroraVeil).Up);

        for (int turn = 0; turn < 4; turn++) Turn(core, foe: 0);
        Assert.Contains("Your team's Aurora Veil wore off!", Turn(core, foe: 0));
        Follows(core, look, ref read);
        Assert.False(look.Of(BattleSide.Player, SideCondition.AuroraVeil).Up);
    }

    [Fact]
    public void BrickBreakShattersAuroraVeil()
    {
        var core = Wild(Idling(Mon("Glalie", 50)), Mon("Machamp", 50, "Brick Break"), rules: Ruleset.Modern, sky: BattleWeather.Snow);
        core.RaiseAuroraVeil(core.At(Mine));
        Turn(core);
        Assert.False(core.Field.Side(BattleSide.Player).AuroraVeil);
        var shattered = core.Log.OfType<Said>().Single(s => s.Text == "It shattered the barrier!");
        Assert.Contains(new SideConditionChanged(BattleSide.Player, SideCondition.AuroraVeil, 0), shattered.Shows);
    }

    [Fact]
    public void StickyWebSlowsWhoeverComesInOnTheGround()
    {
        var core = Wild(Idling(Mon("Snorlax", 50)), Idling(Mon("Shuckle", 50)), bench: new[] { Idling(Mon("Machop", 50)), Idling(Mon("Pidgey", 50)) });
        var look = new FieldLook();
        int read = 0;
        Assert.True(core.LayStickyWeb(BattleSide.Player));
        Assert.False(core.LayStickyWeb(BattleSide.Player));
        Follows(core, look, ref read);
        Assert.True(look.Of(BattleSide.Player, SideCondition.StickyWeb).Up);

        var said = SwitchTo(core, 1);
        Assert.Contains("Machop was caught in a sticky web!", said);
        Assert.Equal(-1, core.At(Mine).Pokemon!.StatStages.GetValueOrDefault(StatType.Speed));
        // A Flying type flies over it
        said = SwitchTo(core, 2);
        Assert.DoesNotContain(said, l => l.Contains("sticky web"));
        Follows(core, look, ref read);
    }

    [Fact]
    public void RapidSpinAndDefogTakeTheWebAway()
    {
        var spin = Wild(Mon("Donphan", 50, "Rapid Spin"), Idling(Mon("Shuckle", 50)));
        spin.LayStickyWeb(BattleSide.Player);
        var look = new FieldLook();
        int read = 0;
        Assert.Contains("Donphan blew away the sticky web!", Turn(spin));
        Assert.False(spin.Field.Side(BattleSide.Player).StickyWeb);
        Follows(spin, look, ref read);

        var defog = Wild(Mon("Togekiss", 50, "Defog"), Idling(Mon("Shuckle", 50)));
        defog.LayStickyWeb(BattleSide.Enemy);
        Assert.Contains("The sticky web around the foe's team was blown away!", Turn(defog));
        Assert.False(defog.Field.Side(BattleSide.Enemy).StickyWeb);
    }

    // ------------------------------------------------------------------ shapes that change in battle

    [Fact]
    public void AegislashDrawsItsBladeToAttackAndSheathesItForKingsShield()
    {
        var aegislash = With(Mon("Aegislash", 50, "Iron Head", "King's Shield"), ability: "Stance Change");
        int shieldAttack = aegislash.Attack;
        var core = Wild(aegislash, Idling(Mon("Steelix", 60)), bench: Idling(Mon("Shuckle", 50)));
        Assert.Contains("Aegislash changed to Blade Forme!", Turn(core, mine: 0));
        var fighting = core.At(Mine).Pokemon!;
        Assert.Equal("Aegislash-Blade", fighting.Form);
        Assert.True(fighting.Attack > shieldAttack);
        Assert.Contains("Aegislash changed to Shield Forme!", Turn(core, mine: 1));
        Assert.Null(fighting.Form);

        // A blade drawn is sheathed again as it leaves
        Turn(core, mine: 0);
        SwitchTo(core, 1);
        Assert.Null(fighting.Form);
        Assert.Equal(shieldAttack, fighting.Attack);
    }

    [Fact]
    public void DarmanitanEntersZenModeAtHalfItsHpAndLeavesItAbove()
    {
        var core = Wild(Idling(With(Mon("Darmanitan", 50), ability: "Zen Mode")), Idling(Mon("Shuckle", 50)));
        var darmanitan = core.At(Mine).Pokemon!;
        darmanitan.CurrentHP = darmanitan.MaxHP / 2;
        Assert.Contains("Darmanitan's Zen Mode triggered!", Turn(core));
        Assert.Equal("Darmanitan-Zen", darmanitan.Form);
        darmanitan.CurrentHP = darmanitan.MaxHP;
        Assert.Contains("Darmanitan's Zen Mode ended!", Turn(core));
        Assert.Null(darmanitan.Form);
    }

    [Fact]
    public void WishiwashiSchoolsAboveAQuarterOfItsHp()
    {
        var core = Wild(Idling(With(Mon("Wishiwashi", 30), ability: "Schooling")), Idling(Mon("Shuckle", 50)));
        var wishiwashi = core.At(Mine).Pokemon!;
        Assert.Contains("Wishiwashi formed a school!", Lines(core.Log));
        Assert.Equal("Wishiwashi-School", wishiwashi.Form);
        wishiwashi.CurrentHP = wishiwashi.MaxHP / 4;
        Assert.Contains("Wishiwashi stopped schooling!", Turn(core));
        Assert.Null(wishiwashi.Form);

        // Under level 20 it never schools
        var young = Wild(Idling(With(Mon("Wishiwashi", 15), ability: "Schooling")), Idling(Mon("Shuckle", 50)));
        Assert.Null(young.At(Mine).Pokemon!.Form);
    }

    [Fact]
    public void MiniorsShellFallsAwayAtHalfItsHp()
    {
        var minior = With(Mon("Minior", 50), ability: "Shields Down");
        minior.ChangeForm("Minior-Blue-Meteor");
        var core = Wild(Idling(minior), Idling(Mon("Shuckle", 50)));
        var fighting = core.At(Mine).Pokemon!;
        fighting.CurrentHP = fighting.MaxHP / 2;
        Assert.Contains("Minior's shell fell away!", Turn(core));
        Assert.Equal("Minior-Blue", fighting.Form);
        Assert.Equal("Minior-Red", BattleCore.CoreOf(null));
        Assert.Null(BattleCore.MeteorOf("Minior-Red"));
    }

    [Fact]
    public void MimikyusDisguiseTakesTheFirstHitAndCostsAnEighth()
    {
        // (Bite: a Normal move can't touch a Ghost type)
        var core = Wild(Idling(With(Mon("Mimikyu", 50), ability: "Disguise")), Mon("Snorlax", 50, "Bite"));
        var mimikyu = core.At(Mine).Pokemon!;
        var said = Turn(core);
        Assert.Contains("Its disguise served it as a decoy!", said);
        Assert.Contains("Mimikyu's disguise was busted!", said);
        Assert.Equal("Mimikyu-Busted", mimikyu.Form);
        Assert.Equal(mimikyu.MaxHP - mimikyu.MaxHP / 8, mimikyu.CurrentHP);
        // The next hit lands
        Turn(core);
        Assert.True(mimikyu.CurrentHP < mimikyu.MaxHP - mimikyu.MaxHP / 8);
    }

    [Fact]
    public void EiscuesIcyFaceTakesAPhysicalHitAndFormsAgainInHail()
    {
        var hitter = Wild(Mon("Snorlax", 50, "Tackle", "Hail", "Ember"), Idling(With(Mon("Eiscue", 50), ability: "Ice Face")));
        var eiscue = hitter.At(Foe).Pokemon!;
        // A special move goes past it
        Turn(hitter, mine: 2);
        Assert.Null(eiscue.Form);
        Assert.True(eiscue.CurrentHP < eiscue.MaxHP);
        eiscue.CurrentHP = eiscue.MaxHP;
        Assert.Contains("Foe Eiscue's icy face shattered!", Turn(hitter, mine: 0));
        Assert.Equal("Eiscue-Noice", eiscue.Form);
        Assert.Equal(eiscue.MaxHP, eiscue.CurrentHP);
        Assert.Contains("Foe Eiscue's icy face formed again!", Turn(hitter, mine: 1));
        Assert.Null(eiscue.Form);
    }

    [Fact]
    public void MorpekoTurnsAboutEveryTurn()
    {
        var core = Wild(Idling(With(Mon("Morpeko", 50), ability: "Hunger Switch")), Idling(Mon("Shuckle", 50)));
        var morpeko = core.At(Mine).Pokemon!;
        Turn(core);
        Assert.Equal("Morpeko-Hangry", morpeko.Form);
        Turn(core);
        Assert.Null(morpeko.Form);
    }

    [Fact]
    public void PalafinComesBackAHero()
    {
        var palafin = Idling(With(Mon("Palafin", 50), ability: "Zero to Hero"));
        int zeroAttack = palafin.Attack;
        var core = Wild(palafin, Idling(Mon("Shuckle", 50)), bench: Idling(Mon("Snorlax", 50)));
        SwitchTo(core, 1);
        Assert.Equal("Palafin-Hero", palafin.Form);
        var said = SwitchTo(core, 0);
        Assert.Contains("Palafin underwent a heroic transformation!", said);
        Assert.Equal("Palafin-Hero", core.At(Mine).Pokemon!.Form);
        Assert.True(core.At(Mine).Pokemon!.Attack > zeroAttack);
        // Told once a battle
        SwitchTo(core, 1);
        Assert.DoesNotContain(SwitchTo(core, 0), l => l.Contains("heroic"));
    }

    [Fact]
    public void CramorantCatchesSomethingWithSurfAndSpitsItAtWhoeverHitsIt()
    {
        var core = Wild(With(Mon("Cramorant", 50, "Surf"), ability: "Gulp Missile"), Mon("Snorlax", 50, "Tackle"));
        var cramorant = core.At(Mine).Pokemon!;
        var snorlax = core.At(Foe).Pokemon!;
        int before = snorlax.MaxHP;
        // Cramorant is the faster: its Surf brings up an Arrokuda (it is above half its HP), and Snorlax's Tackle finds it
        var said = Turn(core);
        Assert.Contains("Cramorant spat out what it had caught!", said);
        Assert.Contains("Foe Snorlax was hit by the catch!", said);
        Assert.Equal(-1, snorlax.StatStages.GetValueOrDefault(StatType.Defense));
        Assert.Null(cramorant.Form);
        Assert.True(snorlax.CurrentHP <= before - before / 4);
    }

    [Fact]
    public void CramorantWithItsCatchIsTheGulpingForm()
    {
        var core = Wild(With(Mon("Cramorant", 50, "Surf"), ability: "Gulp Missile"), Idling(Mon("Snorlax", 50)));
        Turn(core);
        Assert.Equal("Cramorant-Gulping", core.At(Mine).Pokemon!.Form);
    }
}
