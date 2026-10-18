using System;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using Raylib_cs;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// A move's light over the stage and what a move does to a body (plan 25 · Y16): the tables name real moves and
/// every stage the owed lists named, the light comes and goes over its cue and leaves the stage as it was with
/// nothing playing, and a body moves only while something moves it.
/// </summary>
public class StageLightTests
{
    private static EffectCue Move(string move, bool charging = false, bool arriving = false, bool missed = false,
        BattleSide from = BattleSide.Player, BattleSide to = BattleSide.Enemy) =>
        new() { Kind = CueKind.Move, Move = move, Charging = charging, Arriving = arriving, Missed = missed, FromSide = from, ToSide = to };

    private static BattleAnimator Playing(string move, float age, bool missed = false)
    {
        var anim = new BattleAnimator();
        anim.Cue(Move(move, missed: missed));
        anim.Update(age, null, null);
        return anim;
    }

    // ------------------------------------------------------------------ the tables

    [Fact]
    public void EveryMoveOfTheTablesIsAMoveOfTheData()
    {
        var missing = StageLights.Table.Keys.Concat(BodyMotions.Users.Keys).Concat(BodyMotions.Targets.Keys)
            .Where(m => MoveDatabase.Get(m) == null).ToList();
        Assert.Empty(missing);
    }

    /// <summary>The stages plan 25's owed lists (Y7 to Y10) named as darkened, tinted, greyed or switched.</summary>
    [Theory]
    [InlineData("Charge"), InlineData("Thunderbolt"), InlineData("Swift"), InlineData("Hidden Power"), InlineData("Curse"), InlineData("Mean Look")]
    [InlineData("Spite"), InlineData("Night Shade"), InlineData("Thunder Wave"), InlineData("String Shot"), InlineData("Feint Attack"), InlineData("Meditate")]
    [InlineData("Rain Dance"), InlineData("Confuse Ray"), InlineData("Mega Drain"), InlineData("Fire Punch"), InlineData("Sunny Day"), InlineData("Spark")]
    [InlineData("Fury Cutter"), InlineData("Detect"), InlineData("Mind Reader"), InlineData("Encore"), InlineData("Ice Fang"), InlineData("Revenge")]
    [InlineData("Amnesia"), InlineData("Defog"), InlineData("Refresh"), InlineData("Scary Face"), InlineData("Shock Wave"), InlineData("Thunder Shock")]
    [InlineData("Future Sight"), InlineData("Cosmic Power"), InlineData("Shadow Sneak"), InlineData("Calm Mind"), InlineData("Metal Sound"), InlineData("Miracle Eye")]
    [InlineData("Drain Punch"), InlineData("Dream Eater"), InlineData("Ice Beam"), InlineData("Mist"), InlineData("Fire Fang"), InlineData("Tri Attack")]
    [InlineData("Grass Whistle"), InlineData("Reversal"), InlineData("Ice Punch"), InlineData("Whirlpool"), InlineData("Psychic")]
    [InlineData("Thunder"), InlineData("Fire Blast"), InlineData("Blizzard"), InlineData("Focus Blast"), InlineData("Hydro Pump"), InlineData("Flamethrower")]
    [InlineData("Trick Room"), InlineData("Wish"), InlineData("Thunder Punch"), InlineData("Metal Burst"), InlineData("Heal Bell")]
    [InlineData("Charge Beam"), InlineData("Dark Pulse"), InlineData("Hyper Beam"), InlineData("Solar Beam"), InlineData("Power Gem"), InlineData("Energy Ball")]
    [InlineData("Aura Sphere"), InlineData("Destiny Bond"), InlineData("Grudge"), InlineData("Perish Song"), InlineData("Sheer Cold"), InlineData("Outrage")]
    [InlineData("Overheat"), InlineData("Flare Blitz"), InlineData("Earth Power")]
    [InlineData("Superpower"), InlineData("Eruption"), InlineData("Fissure"), InlineData("Sky Uppercut"), InlineData("Glare"), InlineData("Healing Wish")]
    [InlineData("Heal Block"), InlineData("Nightmare"), InlineData("Sky Attack"), InlineData("Psycho Boost"), InlineData("Dark Void"), InlineData("Spacial Rend")]
    [InlineData("Roar of Time"), InlineData("Judgment"), InlineData("Shadow Force"), InlineData("Hydro Cannon"), InlineData("Powder Snow")]
    public void EveryStageTheOwedListsNamedChangesItsLight(string move)
    {
        Assert.True(StageLights.Table.TryGetValue(move, out var light), move);
        Assert.True(light.Any, move);
    }

    [Fact]
    public void NoStageIsWashedSoFarThatAPokemonIsLostAgainstIt()
    {
        foreach (var (move, light) in StageLights.Table)
        {
            Assert.InRange(light.Amount, 0f, StageLights.MostDark);
            Assert.InRange(light.Grey, 0f, 1f);
            // Pale washes are kept under the pale cap: a white Pokémon must still stand out of them
            float luma = 0.299f * light.Color.X + 0.587f * light.Color.Y + 0.114f * light.Color.Z;
            if (luma > 0.8f) Assert.True(light.Amount <= StageLights.MostPale, move);
        }
    }

    // ------------------------------------------------------------------ the light over a cue

    [Fact]
    public void TheLightComesInHoldsAndIsGoneBeforeTheCueEnds()
    {
        float length = BattleAnimator.EffectTime;
        Assert.Equal(0f, StageLights.Envelope(0f, length));
        Assert.Equal(1f, StageLights.Envelope(BattleAnimator.ImpactTime, length));
        Assert.Equal(0f, StageLights.Envelope(length - StageLights.BackBy, length));
        Assert.Equal(0f, StageLights.Envelope(length, length));
        float last = 0f;
        for (float t = 0f; t <= StageLights.InTime; t += 0.01f)
        {
            float e = StageLights.Envelope(t, length);
            Assert.True(e >= last - 1e-6f);
            last = e;
        }
        for (float t = 0f; t < length + 0.5f; t += 0.017f)
            Assert.InRange(StageLights.Envelope(t, length), 0f, 1f);
    }

    [Fact]
    public void NothingPlayingLeavesTheStageExactlyAsItWas()
    {
        var anim = new BattleAnimator();
        Assert.Equal(StageLight.None, StageLights.Of(anim));
        var colour = new Vector3(0.31f, 0.52f, 0.77f);
        Assert.Equal(colour, StageLight.None.Apply(colour));
        var sky = new SkyColors(new Color(10, 20, 30, 255), new Color(40, 50, 60, 255), new Color(70, 80, 90, 255), new Color(200, 210, 220, 255), 0.4f);
        Assert.Equal(sky, StageLight.None.Apply(sky));
        // A move with no light of its own leaves it too
        Assert.Equal(StageLight.None, StageLights.Of(Playing("Tackle", BattleAnimator.ImpactTime)));
    }

    [Fact]
    public void AMoveThatDarkensTheStageDarkensItAndTheSameAgeGivesTheSameLight()
    {
        var a = StageLights.Of(Playing("Thunderbolt", BattleAnimator.ImpactTime));
        var b = StageLights.Of(Playing("Thunderbolt", BattleAnimator.ImpactTime));
        Assert.Equal(a, b);
        Assert.True(a.Amount > 0.5f);
        var lit = new Vector3(0.6f, 0.8f, 0.5f);
        Assert.True(a.Apply(lit).Length() < lit.Length() * 0.6f);
        // A miss darkens it as well: the original fades the background whatever the move does
        Assert.Equal(a, StageLights.Of(Playing("Thunderbolt", BattleAnimator.ImpactTime, missed: true)));
        // And it is gone once the cue is over
        Assert.Equal(StageLight.None, StageLights.Of(Playing("Thunderbolt", BattleAnimator.EffectTime + 0.1f)));
    }

    [Fact]
    public void ATintedStageTakesTheMovesColourAndAGreyedOneLosesItsOwn()
    {
        var fire = StageLights.Of(Playing("Flamethrower", BattleAnimator.ImpactTime)).Apply(new Vector3(0.5f, 0.5f, 0.5f));
        Assert.True(fire.X > fire.Z);
        var grey = StageLights.Of(Playing("Dark Pulse", BattleAnimator.ImpactTime)).Apply(new Vector3(0.2f, 0.8f, 0.2f));
        Assert.True(MathF.Abs(grey.X - grey.Y) < 0.02f);
    }

    [Fact]
    public void AChargeHalfAnAttackLandingAndTheLaterSetPiecesKeepTheStage()
    {
        Assert.Equal(StageLight.None, StageLights.For(Move("Solar Beam", charging: true)));
        Assert.Equal(StageLight.None, StageLights.For(Move("Future Sight", arriving: true)));
        Assert.Equal(StageLight.None, StageLights.For(new EffectCue { Kind = CueKind.Move, Move = "Thunderbolt", ZMove = true }));
        Assert.Equal(StageLight.None, StageLights.For(new EffectCue { Kind = CueKind.Heal, Move = "Thunderbolt" }));
    }

    [Fact]
    public void TwoLightsAtOnceComeToNoMoreThanAWholeWash()
    {
        var dark = StageLights.Table["Thunder"];
        var both = StageLight.Compose(dark, dark);
        Assert.True(both.Amount > dark.Amount && both.Amount <= 1f);
        Assert.Equal(dark, StageLight.Compose(StageLight.None, dark));
        Assert.Equal(dark, StageLight.Compose(dark, StageLight.None));
    }

    // ------------------------------------------------------------------ bodies

    [Fact]
    public void ABodyNothingMovesStandsExactlyWhereItStood()
    {
        var anim = new BattleAnimator();
        Assert.Equal(BodyMotion.Still, BodyMotions.Of(anim, BattleSide.Player, 0));
        Assert.Equal((1f, 1f), BodyMotion.Still.Shape);
        Assert.False(BodyMotions.Of(Playing("Tackle", 0.3f), BattleSide.Player, 0).Moves);
    }

    [Fact]
    public void ARushReachesTheFoeAtTheImpactAndComesBack()
    {
        float length = BattleAnimator.EffectTime;
        var at = BodyMotions.OfUser("Take Down", BattleAnimator.ImpactTime, length);
        Assert.Equal(BodyMotions.RushReach, at.Toward, 3);
        Assert.Equal(0f, BodyMotions.OfUser("Take Down", 0f, length).Toward, 3);
        Assert.Equal(0f, BodyMotions.OfUser("Take Down", length - 0.01f, length).Toward, 3);
        for (float t = 0f; t < length; t += 0.02f)
            Assert.InRange(BodyMotions.OfUser("Take Down", t, length).Toward, 0f, BodyMotions.RushReach + 1e-4f);
        // The user moves; the target of a rush does not
        var anim = Playing("Take Down", BattleAnimator.ImpactTime);
        Assert.True(BodyMotions.Of(anim, BattleSide.Player, 0).Toward > 0.5f);
        Assert.False(BodyMotions.Of(anim, BattleSide.Enemy, 0).Moves);
    }

    [Fact]
    public void AWrapSqueezesItsTargetAndGravityPressesEveryBody()
    {
        var wrap = Playing("Wrap", BattleAnimator.ImpactTime + 0.15f);
        Assert.True(BodyMotions.Of(wrap, BattleSide.Enemy, 0).Narrow > 0.05f);
        Assert.False(BodyMotions.Of(wrap, BattleSide.Player, 0).Moves);
        // Nothing is squeezed by a miss
        Assert.False(BodyMotions.Of(Playing("Wrap", BattleAnimator.ImpactTime + 0.15f, missed: true), BattleSide.Enemy, 0).Moves);
        var gravity = Playing("Gravity", BattleAnimator.ImpactTime + 0.3f);
        Assert.True(BodyMotions.Of(gravity, BattleSide.Enemy, 0).Squash > 0.05f);
        Assert.True(BodyMotions.Of(gravity, BattleSide.Player, 0).Squash > 0.05f);
        for (float t = 0f; t < BattleAnimator.EffectTime; t += 0.02f)
        {
            var (w, h) = BodyMotions.OfTarget("Wrap", t, BattleAnimator.EffectTime).Shape;
            Assert.InRange(w, 0.8f, 1.01f);
            Assert.InRange(h, 0.99f, 1.1f);
        }
    }

    [Fact]
    public void AGoneBodyLeavesTheWayItsMoveGoesAndComesBackTheSameWay()
    {
        Assert.Equal(GoneWay.Up, BodyMotions.WayOf("Fly"));
        Assert.Equal(GoneWay.Up, BodyMotions.WayOf("Bounce"));
        Assert.Equal(GoneWay.Under, BodyMotions.WayOf("Dig"));
        Assert.Equal(GoneWay.Under, BodyMotions.WayOf("Dive"));
        Assert.Equal(GoneWay.Dark, BodyMotions.WayOf("Shadow Force"));
        Assert.Equal(BodyMotion.Still, BodyMotions.Leaving(GoneWay.Up, 0f) with { Squash = 0f });
        Assert.True(BodyMotions.Leaving(GoneWay.Up, 1f).Up >= 3f);
        Assert.True(BodyMotions.Leaving(GoneWay.Under, 1f).Sink >= 1f);
        Assert.Equal(0f, BodyMotions.Leaving(GoneWay.Dark, 1f).Grow, 3);
        foreach (var way in Enum.GetValues<GoneWay>())
        {
            Assert.Equal(BodyMotion.Still, BodyMotions.Returning(way, 1f));
            Assert.Equal(BodyMotions.Leaving(way, 1f).Up, BodyMotions.Returning(way, 0f).Up, 3);
        }

        var anim = new BattleAnimator();
        anim.Appear(BattleSide.Player, Scenario.Mon("Staravia", 30));
        anim.GoAway(BattleSide.Player, 0, "Fly");
        anim.Update(BodyMotions.LeaveTime * 0.5f, null, null);
        var v = anim[BattleSide.Player, 0];
        Assert.True(v.Away && v.AwayAge > 0f);
        Assert.True(BodyMotions.Of(anim, BattleSide.Player, 0).Up > 0.3f);
        anim.Update(BodyMotions.LeaveTime, null, null);
        Assert.True(v.Away && v.AwayAge < 0f);
        anim.ComeBack(BattleSide.Player, 0);
        Assert.False(v.Away);
        Assert.True(BodyMotions.Of(anim, BattleSide.Player, 0).Up > 3f);
        anim.Update(BodyMotions.ReturnTime + 0.01f, null, null);
        Assert.Equal(BodyMotion.Still, BodyMotions.Of(anim, BattleSide.Player, 0));
    }

    [Fact]
    public void ASpentBodyStaysDownWhileItLiesFaintedAndRisesIfItStands()
    {
        Assert.Equal(BodyMotion.Still, BodyMotions.Spent(0f, fainted: false));
        var down = BodyMotions.Spent(BattleAnimator.EffectTime + 1f, fainted: true);
        Assert.Equal(BodyMotions.SpentGrow, down.Grow, 3);
        Assert.Equal(BodyMotion.Still, BodyMotions.Spent(BattleAnimator.EffectTime + 0.4f, fainted: false));

        var anim = new BattleAnimator();
        var user = Scenario.Mon("Drifblim", 40);
        anim.Appear(BattleSide.Player, user);
        anim.Cue(Move("Memento"));
        anim.Update(BattleAnimator.EffectTime - 0.1f, null, null);
        Assert.True(BodyMotions.Of(anim, BattleSide.Player, 0).Grow < 0.75f);
        user.CurrentHP = 0;
        anim.Update(1f, null, null);
        Assert.Equal(BodyMotions.SpentGrow, BodyMotions.Of(anim, BattleSide.Player, 0).Grow, 3);
    }

    [Fact]
    public void EveryMotionIsPureAndBounded()
    {
        foreach (var move in BodyMotions.Users.Keys.Concat(BodyMotions.Targets.Keys))
            for (float t = 0f; t < BattleAnimator.EffectTime; t += 0.013f)
            {
                var a = BodyMotions.OfUser(move, t, BattleAnimator.EffectTime) + BodyMotions.OfTarget(move, t, BattleAnimator.EffectTime);
                Assert.Equal(a, BodyMotions.OfUser(move, t, BattleAnimator.EffectTime) + BodyMotions.OfTarget(move, t, BattleAnimator.EffectTime));
                Assert.InRange(a.Toward, -0.01f, 1f);
                Assert.InRange(a.Up, -0.1f, 0.7f);
                Assert.InRange(a.Grow, 0.5f, 1f);
                var (w, h) = a.Shape;
                Assert.InRange(w, 0.8f, 1.2f);
                Assert.InRange(h, 0.8f, 1.2f);
            }
    }
}
