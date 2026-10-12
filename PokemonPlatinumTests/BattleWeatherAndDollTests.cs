using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Graphics;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The weather and the doll (plan 25 · Y1): the turn's weather line carries its event, the screen's doll goes up and
/// down with the lines that tell of it, and every look is a pure function of its ages and its seed.
/// </summary>
public class BattleWeatherAndDollTests
{
    private static readonly Place Mine = new(BattleSide.Player, 0);

    private static readonly Dictionary<BattleWeather, string> GoesOn = new()
    {
        [BattleWeather.Rain] = "Rain continues to fall.",
        [BattleWeather.Sun] = "The sunlight is strong.",
        [BattleWeather.Sandstorm] = "The sandstorm rages.",
        [BattleWeather.Hail] = "Hail continues to fall.",
        [BattleWeather.Fog] = "The fog is deep..."
    };

    public static IEnumerable<object[]> Weathers => GoesOn.Keys.Select(w => new object[] { w });

    // ------------------------------------------------------------------ the rules' event

    [Theory]
    [MemberData(nameof(Weathers))]
    public void EachLineOfTheWeatherGoingOnCarriesItsEvent(BattleWeather weather)
    {
        // The sun comes by Sunny Day; the rest are the field's weather the battle opens in
        BattleCore core = weather == BattleWeather.Sun
            ? CoreScenario.Wild(Mon("Snorlax", 50, "Sunny Day"), Mon("Snorlax", 50))
            : CoreScenario.Wild(Mon("Snorlax", 50), Mon("Snorlax", 50), sky: weather);
        CoreScenario.Turn(core);
        CoreScenario.Turn(core);

        // Fog's opening line says what its going on says, and carries no event of its own
        var lines = core.Log.OfType<Said>().Where(s => s.Text == GoesOn[weather]).ToList();
        if (weather == BattleWeather.Fog)
        {
            Assert.Equal(3, lines.Count);
            Assert.Empty(lines[0].Shows.OfType<WeatherContinues>());
            lines.RemoveAt(0);
        }
        Assert.Equal(2, lines.Count);
        foreach (var line in lines)
            Assert.Equal(new WeatherContinues(weather), Assert.Single(line.Shows.OfType<WeatherContinues>()));

        // And no other line carries it
        Assert.Equal(2, core.Log.OfType<Said>().SelectMany(s => s.Shows).OfType<WeatherContinues>().Count());
    }

    [Fact]
    public void TheWeatherGoingOnPlaysItsCueAgainOverTheLayer()
    {
        var anim = new BattleAnimator();
        var begins = anim.WeatherBegins(BattleWeather.Rain);
        Assert.Equal(CueKind.Weather, begins.Kind);
        Assert.Equal(BattleWeather.Rain, anim.Weather);
        Assert.Equal(0f, anim.WeatherStrength);

        anim.Update(BattleAnimator.WeatherFadeTime + 0.5f, null, null);
        Assert.Equal(1f, anim.WeatherStrength);
        Assert.DoesNotContain(begins, anim.Cues);

        var again = anim.WeatherGoesOn(BattleWeather.Rain);
        Assert.Contains(again, anim.Cues);
        Assert.NotEqual(begins.Seed, again.Seed);
        Assert.Equal(1f, anim.WeatherStrength);

        // Clearing eases the layer away
        anim.WeatherClears();
        Assert.Equal(BattleWeather.None, anim.Weather);
        Assert.Equal(BattleWeather.Rain, anim.FadingWeather);
        Assert.Equal(1f, anim.FadingStrength);
        anim.Update(BattleAnimator.WeatherFadeTime + 0.1f, null, null);
        Assert.Equal(0f, anim.FadingStrength);
    }

    // ------------------------------------------------------------------ the doll goes up and down by the lines

    private static List<(string Line, bool Doll)> ReadOn(BattleEngine battle)
    {
        var seen = new List<(string, bool)>();
        for (int i = 0; i < 400 && !battle.IsBattleOver; i++)
        {
            if (battle.HUD.MenuState is not (BattleMenuState.Message or BattleMenuState.LearnMove)) break;
            if (battle.IsWaitingForConfirm) seen.Add((battle.CurrentMessage, battle.Anim[BattleSide.Player].BehindDoll));
            battle.ConfirmMessage();
            battle.Update(1f / 60f);
        }
        return seen;
    }

    [Fact]
    public void TheDollGoesUpWithTheLineThatTellsOfIt()
    {
        var battle = Battle(Mon("Blastoise", 50, "Substitute"), Mon("Snorlax", 50));
        Assert.False(battle.Anim[BattleSide.Player].BehindDoll);

        battle.SelectMove(0);
        // The rules have it at once; the screen doesn't until the line is read
        Assert.True(battle.Core.At(Mine).HasSubstitute);
        Assert.False(battle.Anim[BattleSide.Player].BehindDoll);

        var seen = ReadOn(battle);
        Assert.Contains(("Blastoise used Substitute!", false), seen);
        Assert.Contains(("Blastoise made a substitute!", true), seen);
        Assert.True(battle.Anim[BattleSide.Player].BehindDoll);
    }

    [Fact]
    public void TheDollFadesWithTheLineThatTellsOfIt()
    {
        // Machamp's Strength breaks Blastoise's Substitute of 34 HP (BattleFieldTests' numbers): the hit that breaks it
        // takes the doll down with its own line
        var battle = Battle(Mon("Blastoise", 50, "Substitute"), Mon("Machamp", 50, "Strength"));
        battle.SelectMove(0);
        var seen = ReadOn(battle);
        Assert.Contains(("Blastoise made a substitute!", true), seen);
        Assert.Contains(("The substitute took damage for Blastoise!", false), seen);
        Assert.Contains(("Blastoise's substitute faded!", false), seen);
        Assert.False(battle.Anim[BattleSide.Player].BehindDoll);
    }

    // ------------------------------------------------------------------ what shows behind a doll

    [Fact]
    public void AMarkOnAPokemonBehindADollIsHiddenAndTheWeatherShows()
    {
        // BattleSystem_ShouldShowStatusEffect: behind a Substitute only the weather's animation plays
        var anim = new BattleAnimator();
        anim.SubstituteUp(BattleSide.Player);
        Assert.True(anim[BattleSide.Player].BehindDoll);

        foreach (var kind in new[] { CueKind.Status, CueKind.StatUp, CueKind.StatDown, CueKind.Heal })
        {
            var mark = anim.Cue(new EffectCue { Kind = kind, ToSide = BattleSide.Player, FromSide = BattleSide.Player });
            Assert.DoesNotContain(mark, anim.Cues);
        }
        var weather = anim.WeatherBegins(BattleWeather.Hail);
        Assert.Contains(weather, anim.Cues);

        // The other side's marks still show, and so do this side's once the doll is gone
        var foes = anim.Cue(new EffectCue { Kind = CueKind.Status, ToSide = BattleSide.Enemy });
        Assert.Contains(foes, anim.Cues);
        anim.SubstituteDown(BattleSide.Player);
        var after = anim.Cue(new EffectCue { Kind = CueKind.Status, ToSide = BattleSide.Player });
        Assert.Contains(after, anim.Cues);
    }

    // ------------------------------------------------------------------ the looks are pure

    private static readonly FxPlaces Places = new();

    private static FxList Layer(BattleWeather weather, float age, float time)
    {
        var fx = new FxList();
        ArenaFx.Weather(weather, age, time, Places, fx);
        return fx;
    }

    private static FxList WeatherCue(BattleWeather weather, float age, int seed)
    {
        var fx = new FxList();
        ArenaFx.WeatherCue(weather, age, seed, Places, fx);
        return fx;
    }

    [Theory]
    [MemberData(nameof(Weathers))]
    public void TheWeathersLooksArePureInTheirAgesAndSeed(BattleWeather weather)
    {
        var a = Layer(weather, 2f, 7.3f);
        var b = Layer(weather, 2f, 7.3f);
        Assert.True(a.Count > 0);
        Assert.Equal(a.Quads, b.Quads);
        Assert.All(a.Quads, q => Assert.True(q.Depth, "the weather's layer sits in the scene"));
        Assert.NotEqual(a.Quads, Layer(weather, 2f, 7.6f).Quads);
        // It eases in from nothing
        Assert.Equal(0, Layer(weather, 0f, 7.3f).Count);

        var c = WeatherCue(weather, 0.5f, 7919);
        var d = WeatherCue(weather, 0.5f, 7919);
        Assert.True(c.Count > 0 || c.Flash > 0f);
        Assert.Equal(c.Quads, d.Quads);
        Assert.Equal(c.Flash, d.Flash);
        Assert.Equal(0, WeatherCue(weather, BattleAnimator.WeatherCueTime + 0.01f, 7919).Count);
    }

    [Fact]
    public void TheDollsLookIsPureInItsAgesAndSeed()
    {
        SubstituteView Doll(bool up, float age, float hit = -1f, float aside = -1f) =>
            new() { Up = up, Age = age, HitAge = hit, AsideAge = aside, Seed = 4099 };

        Assert.Equal(DollLook.None, SubstituteDoll.LookOf(null));
        Assert.Equal(SubstituteDoll.LookOf(Doll(true, 0.4f, 0.1f)), SubstituteDoll.LookOf(Doll(true, 0.4f, 0.1f)));

        // Dropping in: the Pokémon shrinks away and the doll is down on its platform by the end
        Assert.Equal(1f, SubstituteDoll.LookOf(Doll(true, 0f)).PokemonGrow);
        var landed = SubstituteDoll.LookOf(Doll(true, BattleAnimator.SubstituteInTime + 0.5f));
        Assert.True(landed.Visible);
        Assert.Equal(0f, landed.PokemonGrow);
        Assert.Equal(0f, landed.Drop);
        // A doll passed by Baton Pass waits for its Pokémon
        Assert.False(SubstituteDoll.LookOf(Doll(true, -0.2f)).Visible);

        // Aside for its owner's move, the Pokémon shows
        var aside = SubstituteDoll.LookOf(Doll(true, 2f, aside: BattleAnimator.SubstituteAsideTime * 0.5f));
        Assert.Equal(1f, aside.Aside, 3);
        Assert.Equal(1f, aside.PokemonGrow, 3);
        Assert.NotEqual(0f, SubstituteDoll.LookOf(Doll(true, 2f, hit: 0.05f)).Flinch);

        // Fading: gone at the end, the Pokémon back
        var gone = SubstituteDoll.LookOf(Doll(false, BattleAnimator.SubstituteOutTime));
        Assert.False(gone.Visible);
        Assert.Equal(1f, gone.PokemonGrow);

        // Its dust and its puff are the same at the same ages
        foreach (var doll in new[] { Doll(true, BattleAnimator.SubstituteInTime * 0.9f), Doll(false, 0.2f) })
        {
            var a = new FxList();
            var b = new FxList();
            SubstituteDoll.Effects(doll, Vector3.Zero, 1f, a);
            SubstituteDoll.Effects(doll, Vector3.Zero, 1f, b);
            Assert.True(a.Count > 0);
            Assert.Equal(a.Quads, b.Quads);
        }
    }

    [Fact]
    public void EachWeatherAndTheDollHaveTheirSounds()
    {
        foreach (var weather in GoesOn.Keys) Assert.True(SoundBank.Exists(SoundBank.WeatherSound(weather)), weather.ToString());
        Assert.True(SoundBank.Exists("substitute_in"));
        Assert.True(SoundBank.Exists("substitute_out"));
    }
}
