using System;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 25 · Y17g, the last of the battle presentation's owed parts: a move's tint on a body (Y16's owed table),
/// the ground blows' punch-in framed lower so the feet and the cracks are in (Y17b to Y17f's owed framing), and the
/// weather and terrain moves spreading from their user over the field.
/// </summary>
public class SignatureFinishTests
{
    private const float I = BattleAnimator.ImpactTime;

    private static EffectCue Cue(string move, float age, BattleSide from = BattleSide.Player, bool missed = false, bool charging = false, bool onSelf = false)
    {
        var data = MoveDatabase.Get(move)!;
        var to = from == BattleSide.Player ? BattleSide.Enemy : BattleSide.Player;
        return new EffectCue
        {
            Kind = CueKind.Move, Move = move, Type = data.Type, Category = data.Category, FromSide = from,
            ToSide = charging || onSelf ? from : to, Missed = missed, Charging = charging, Seed = 7, Age = age
        };
    }

    private static BattleAnimator Playing(EffectCue cue)
    {
        var anim = new BattleAnimator();
        float age = cue.Age;
        cue.Age = 0f;
        anim.Cue(cue);
        cue.Age = age;
        return anim;
    }

    // ------------------------------------------------------------------ a body's tint

    [Fact]
    public void EveryTintIsAMoveOfTheDataAndUnderItsCap()
    {
        Assert.Equal(153, BodyTints.Table.Count);
        foreach (var (move, tint) in BodyTints.Table)
        {
            Assert.NotNull(MoveDatabase.Get(move));
            Assert.InRange(tint.Amount, 0.05f, BodyTints.Most);
            if (BodyTints.Luma(tint.Color) > 0.8f) Assert.True(tint.Amount <= BodyTints.MostPale, $"{move}'s pale tint {tint.Amount}");
        }
    }

    [Fact]
    public void AUsersTintComesInOverTheCueAndIsGoneByItsEnd()
    {
        var tint = BodyTints.Table["Dragon Dance"];
        Assert.True(tint.OnUser);
        foreach (float age in new[] { 0.05f, 0.3f, 0.7f })
        {
            var at = BodyTints.At(Playing(Cue("Dragon Dance", age, onSelf: true)), BattleSide.Player, 0);
            Assert.True(at.Amount > 0f, $"at {age}");
            Assert.Equal(tint.Color, at.Color);
        }
        var full = BodyTints.At(Playing(Cue("Dragon Dance", 0.5f, onSelf: true)), BattleSide.Player, 0);
        Assert.Equal(tint.Amount, full.Amount, 3);
        var cue = Cue("Dragon Dance", 0f, onSelf: true);
        cue.Age = cue.Length;
        Assert.Equal(0f, BodyTints.At(Playing(cue), BattleSide.Player, 0).Amount);
        // Nothing on the foe's body
        Assert.Equal(0f, BodyTints.At(Playing(Cue("Dragon Dance", 0.5f, onSelf: true)), BattleSide.Enemy, 0).Amount);
    }

    [Fact]
    public void ATargetsTintComesFromTheImpactOnlyOnAHit()
    {
        Assert.False(BodyTints.Table["Surf"].OnUser);
        Assert.Equal(0f, BodyTints.At(Playing(Cue("Surf", I - 0.05f)), BattleSide.Enemy, 0).Amount);
        Assert.True(BodyTints.At(Playing(Cue("Surf", I + 0.15f)), BattleSide.Enemy, 0).Amount > 0f);
        Assert.Equal(0f, BodyTints.At(Playing(Cue("Surf", I + 0.15f, missed: true)), BattleSide.Enemy, 0).Amount);
        Assert.Equal(0f, BodyTints.At(Playing(Cue("Surf", I + BodyTints.TargetLasts + 0.01f)), BattleSide.Enemy, 0).Amount);
        // Not on the user, nor through a charge half
        Assert.Equal(0f, BodyTints.At(Playing(Cue("Surf", I + 0.15f)), BattleSide.Player, 0).Amount);
        Assert.Equal(0f, BodyTints.At(Playing(Cue("Solar Beam", 0.3f, charging: true)), BattleSide.Player, 0).Amount);
    }

    [Fact]
    public void AMoveWithNoTintTintsNothing()
    {
        for (float age = 0f; age < 1.4f; age += 0.05f)
        {
            var anim = Playing(Cue("Tackle", age));
            Assert.Equal(0f, BodyTints.At(anim, BattleSide.Enemy, 0).Amount);
            Assert.Equal(0f, BodyTints.At(anim, BattleSide.Player, 0).Amount);
        }
    }

    // ------------------------------------------------------------------ the ground blows' punch-in

    [Fact]
    public void AGroundBlowsPunchInLooksLowerFromTheImpact()
    {
        var places = new FxPlaces();
        float h = places.Height(BattleSide.Enemy, 0);
        foreach (var move in new[] { "Earthquake", "Headlong Rush", "Mountain Breaker", "Sacred Sword" })
            Assert.True(Directions.Of(move)!.Low, move);
        Assert.False(Directions.Of("Close Combat")!.Low);
        Assert.All(Directions.Table.Where(d => d.Value.Low), d => Assert.Equal(SignatureShot.ImpactPunch, d.Value.Shot));

        (CameraShot Shot, ShotKind Kind) Shot(string move, float age)
        {
            var cue = Cue(move, age);
            return BattleCamera.SignatureShotOf(SignatureShot.ImpactPunch, Playing(cue), places, cue);
        }
        foreach (float age in new[] { I + 0.05f, I + 0.3f, 1.0f })
        {
            var low = Shot("Earthquake", age);
            var plain = Shot("Close Combat", age);
            Assert.Equal(plain.Kind, low.Kind);
            Assert.Equal(plain.Shot.Target.Y - h * BattleCamera.LowPunch, low.Shot.Target.Y, 3);
            Assert.Equal(plain.Shot.Position.Y - h * BattleCamera.LowPunch, low.Shot.Position.Y, 3);
            Assert.Equal(plain.Shot.Fov, low.Shot.Fov, 3);
        }
        // Before the impact it is the plain punch's
        Assert.Equal(Shot("Close Combat", 0.3f).Shot, Shot("Earthquake", 0.3f).Shot);
    }

    // ------------------------------------------------------------------ the weather and the terrains spreading

    private static readonly string[] Spreading =
    {
        "Rain Dance", "Sunny Day", "Sandstorm", "Hail", "Snowscape", "Chilly Reception",
        "Electric Terrain", "Grassy Terrain", "Misty Terrain", "Psychic Terrain",
    };

    [Fact]
    public void TheWeatherAndTheTerrainsAreInTheTableWithNoShot()
    {
        foreach (var move in Spreading)
        {
            var d = Directions.Of(move);
            Assert.NotNull(d);
            Assert.Equal(SignatureShot.None, d!.Shot);
            Assert.False(d.Heavy);
            Assert.NotNull(d.Effect);
        }
    }

    private static FxList Layer(string move, float age, bool missed = false)
    {
        var cue = Cue(move, age, missed: missed, onSelf: true);
        var fx = new FxList();
        Directions.PlayEffect(new FxCtx(cue, new FxPlaces()), MoveFx.ColorsOf(cue.Type), fx);
        return fx;
    }

    [Fact]
    public void ASpreadSetsOffFromItsUserReachesTheFoesAndIsGone()
    {
        var places = new FxPlaces();
        var user = places.Body(BattleSide.Player, 0);
        var foe = places.Body(BattleSide.Enemy, 0);
        foreach (var move in Spreading)
        {
            Assert.Equal(0, Layer(move, Directions.SpreadFrom - 0.01f).Count);
            Assert.Equal(0, Layer(move, 0.6f, missed: true).Count);
            var early = Layer(move, Directions.SpreadFrom + 0.06f);
            var late = Layer(move, Directions.SpreadFrom + Directions.SpreadTime + 0.1f);
            Assert.True(early.Count > 0 && late.Count > 0, move);
            Assert.True(early.Count <= 10 && late.Count <= 10, $"{move}: {early.Count}, {late.Count}");
            float Toward(FxList fx, Vector3 at) => fx.Quads.Average(q => Vector2.Distance(Flat(q.Center), Flat(at)));
            Assert.True(Toward(early, user) < Toward(early, foe), $"{move} sets off from its user");
            Assert.True(Toward(late, foe) < Toward(late, user), $"{move} reaches the foes");
            var cue = Cue(move, 0f, onSelf: true);
            Assert.Equal(0, Layer(move, cue.Length - Directions.SpreadGone + 0.001f).Count);
        }
    }

    [Fact]
    public void ATerrainRunsLowOverTheGround()
    {
        var places = new FxPlaces();
        float ground = MathF.Max(places.Feet(BattleSide.Player, 0).Y, places.Feet(BattleSide.Enemy, 0).Y);
        float tallest = MathF.Max(places.Height(BattleSide.Player, 0), places.Height(BattleSide.Enemy, 0));
        foreach (var move in Spreading.Where(m => m.EndsWith("Terrain")))
            for (float age = Directions.SpreadFrom; age < 1.2f; age += 0.05f)
                foreach (var q in Layer(move, age).Quads)
                    Assert.True(q.Center.Y < ground + tallest * 0.5f, $"{move} at {age:0.00}: {q.Center.Y}");
    }

    private static Vector2 Flat(Vector3 v) => new(v.X, v.Z);
}
