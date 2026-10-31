using System;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Signature moves (plan 25 · Y17a): the direction table names real moves, the hit-stop is a shown age that holds at
/// the impact and ends on time, what a direction lays over a look stays under the caps, the four shots keep clear of
/// the player's platform and come home, a heavy hit throws its target, and nothing of it changes an ordinary move.
/// </summary>
public class SignatureMoveTests
{
    private const float I = BattleAnimator.ImpactTime;

    private static EffectCue Cue(string move, float age, BattleSide from = BattleSide.Player, bool missed = false, bool charging = false)
    {
        var data = MoveDatabase.Get(move)!;
        var to = from == BattleSide.Player ? BattleSide.Enemy : BattleSide.Player;
        return new EffectCue
        {
            Kind = CueKind.Move, Move = move, Type = data.Type, Category = data.Category, FromSide = from,
            ToSide = charging ? from : to, Missed = missed, Charging = charging, Seed = 7, Age = age
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

    public static TheoryData<string> Signatures()
    {
        var data = new TheoryData<string>();
        foreach (var move in Directions.Table.Keys) data.Add(move);
        return data;
    }

    // ------------------------------------------------------------------ the table

    [Fact]
    public void EveryMoveOfTheTableIsAMoveOfTheData()
    {
        Assert.DoesNotContain(Directions.Table.Keys, m => MoveDatabase.Get(m) == null);
        Assert.True(Directions.Table.Count >= 6);
        // Each of the four shots is shown by at least one move
        foreach (var shot in new[] { SignatureShot.ChargePush, SignatureShot.FollowProjectile, SignatureShot.ImpactPunch, SignatureShot.Orbit })
            Assert.Contains(Directions.Table.Values, d => d.Shot == shot);
    }

    // ------------------------------------------------------------------ the hit-stop

    [Theory]
    [InlineData(BattleAnimator.EffectTime), InlineData(BattleAnimator.MaxMoveTime)]
    public void TheRemapHoldsAtTheImpactAndEndsOnTime(float length)
    {
        const float stop = Directions.HeavyStop;
        float last = -1f;
        for (float age = 0f; age <= length + 0.2f; age += 0.005f)
        {
            float shown = Directions.Remap(age, stop, length);
            Assert.True(shown >= last - 1e-5f, $"backward at {age}");
            last = shown;
            if (age <= I) Assert.Equal(age, shown);
            else if (age < I + stop) Assert.Equal(I, shown);
            Assert.InRange(shown, 0f, Math.Max(age, length) + 1e-4f);
        }
        Assert.Equal(length, Directions.Remap(length, stop, length), 4);
        // No stop: the age itself, to the bit
        Assert.Equal(0.731f, Directions.Remap(0.731f, 0f, length));
    }

    [Fact]
    public void AHeavySignatureHitHoldsThePictureAtItsImpact()
    {
        var cue = Cue("Hyper Beam", I + 0.04f);
        Assert.Equal(I, cue.ShownAge);
        Assert.Equal(0.2f, Cue("Hyper Beam", 0.2f).ShownAge);
        Assert.True(Cue("Hyper Beam", 0.8f).ShownAge > 0.8f - Directions.HeavyStop);
    }

    [Theory]
    [InlineData("Tackle", false, false), InlineData("Ice Beam", false, false), InlineData("Hyper Beam", true, false), InlineData("Solar Beam", false, true)]
    [InlineData("Hurricane", false, false)]
    public void NothingButALandedHeavyStrikeIsHeld(string move, bool missed, bool charging)
    {
        // An ordinary move, a miss, a charge turn and a signature without a stop all draw at their own age
        for (float age = 0f; age < 1.3f; age += 0.05f)
            Assert.Equal(age, Cue(move, age, missed: missed, charging: charging).ShownAge);
    }

    // ------------------------------------------------------------------ the looks laid over

    [Theory]
    [MemberData(nameof(Signatures))]
    public void WhatADirectionLaysOverALookStaysUnderTheCaps(string move)
    {
        foreach (var from in new[] { BattleSide.Player, BattleSide.Enemy })
            for (float age = 0f; age < 1.4f; age += 0.01f)
            {
                var cue = Cue(move, age, from);
                var all = new FxList();
                MoveFx.Play(cue, new FxPlaces(), all);
                Assert.True(all.Count <= MoveFx.MaxQuads, $"{move} at {age:0.00}: {all.Count}");

                var layer = new FxList();
                Directions.PlayEffect(new FxCtx(cue, new FxPlaces()), MoveFx.ColorsOf(cue.Type), layer);
                Assert.True(layer.Flash <= 0.35f, $"{move}'s flash {layer.Flash}");
                Assert.True(layer.Shake <= 0.5f, $"{move}'s shake {layer.Shake}");
            }
    }

    [Theory]
    [MemberData(nameof(Signatures))]
    public void ASignatureMoveIsAPureFunctionOfItsAgeAndSeed(string move)
    {
        foreach (float age in new[] { 0.1f, I + 0.03f, 0.7f, 1.1f })
        {
            var a = new FxList();
            var b = new FxList();
            MoveFx.Play(Cue(move, age), new FxPlaces(), a);
            MoveFx.Play(Cue(move, age), new FxPlaces(), b);
            Assert.Equal(a.Count, b.Count);
            for (int i = 0; i < a.Quads.Count; i++) Assert.Equal(a.Quads[i].Center, b.Quads[i].Center);
            Assert.True(BodyMotions.Of(Playing(Cue(move, age)), BattleSide.Enemy, 0)
                == BodyMotions.Of(Playing(Cue(move, age)), BattleSide.Enemy, 0));
        }
    }

    [Fact]
    public void RoarOfTimesRingsComeAfterItsImpactAndAreGone()
    {
        var before = new FxList();
        Directions.PlayEffect(new FxCtx(Cue("Roar of Time", 0.2f), new FxPlaces()), MoveFx.ColorsOf(PokemonType.Dragon), before);
        Assert.Empty(before.Quads);
        var after = new FxList();
        Directions.PlayEffect(new FxCtx(Cue("Roar of Time", 0.6f), new FxPlaces()), MoveFx.ColorsOf(PokemonType.Dragon), after);
        Assert.NotEmpty(after.Quads);
        var gone = new FxList();
        Directions.PlayEffect(new FxCtx(Cue("Roar of Time", 1.29f), new FxPlaces()), MoveFx.ColorsOf(PokemonType.Dragon), gone);
        Assert.Empty(gone.Quads);
    }

    // ------------------------------------------------------------------ the bodies and the light

    [Fact]
    public void AHeavyHitThrowsItsTargetBackAndItSettles()
    {
        Assert.False(BodyMotions.Of(Playing(Cue("Close Combat", 0.2f)), BattleSide.Enemy, 0).Moves);
        var thrown = BodyMotions.Of(Playing(Cue("Close Combat", I + 0.12f)), BattleSide.Enemy, 0);
        Assert.True(thrown.Toward < 0f && thrown.Toward >= -Directions.ThrowReach);
        Assert.True(thrown.Squash > 0f && thrown.Squash <= Directions.ThrowSquash);
        // Back where it stood once the throw is over, and never by a miss
        Assert.False(BodyMotions.Of(Playing(Cue("Close Combat", 1.2f)), BattleSide.Enemy, 0).Moves);
        Assert.False(BodyMotions.Of(Playing(Cue("Close Combat", I + 0.12f, missed: true)), BattleSide.Enemy, 0).Moves);
        // An ordinary hit throws nobody
        Assert.False(BodyMotions.Of(Playing(Cue("Ice Beam", I + 0.12f)), BattleSide.Enemy, 0).Moves);
    }

    [Fact]
    public void ABeamRocksItsUserBack()
    {
        var user = BodyMotions.Of(Playing(Cue("Hyper Beam", I)), BattleSide.Player, 0);
        Assert.True(user.Toward < 0f);
        Assert.False(BodyMotions.Of(Playing(Cue("Hyper Beam", 1.25f)), BattleSide.Player, 0).Moves);
    }

    [Fact]
    public void ASignatureLightComesOverTheMovesOwnAndGoes()
    {
        Assert.False(Directions.LightAt(Cue("Hurricane", 0f)).Any);
        Assert.True(Directions.LightAt(Cue("Hurricane", 0.6f)).Any);
        Assert.False(Directions.LightAt(Cue("Hurricane", 1.29f)).Any);
        // Roar of Time's own darkening and its signature's steel-blue, together never darker than the cap allows
        var both = StageLights.Of(Playing(Cue("Roar of Time", 0.6f)));
        Assert.True(both.Any && both.Amount <= StageLights.MostDark + 1e-4f);
        Assert.False(Directions.LightAt(Cue("Solar Beam", 0.5f, charging: true)).Any);
    }

    // ------------------------------------------------------------------ the camera

    private static bool Finite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);

    [Theory]
    [MemberData(nameof(Signatures))]
    public void EverySignatureShotKeepsClearOfThePlayersPlatform(string move)
    {
        var platform = BattleStage.PlayerSpot + new Vector3(0, 1.2f, 0);
        foreach (var from in new[] { BattleSide.Player, BattleSide.Enemy })
            for (float age = 0f; age < Directions.ShotEnd; age += 0.02f)
            {
                var cue = Cue(move, age, from);
                var anim = Playing(cue);
                var (shot, kind, _) = BattleCamera.Direct(anim, new FxPlaces());
                Assert.True(Finite(shot.Position) && Finite(shot.Target), $"{move} at {age:0.00}");
                Assert.InRange(shot.Fov, 5f, BattleCamera.WidestFov + 0.01f);
                // The player's own Pokémon's close shots stand behind it, as every move's attacker shot does; any other
                // shot of the four stays out of the platform's way
                if (kind is ShotKind.Follow or ShotKind.Orbit or ShotKind.Punch)
                    Assert.True(Vector3.Distance(shot.Position, platform) >= BattleCamera.Clearance, $"{move} {kind} from {from} at {age:0.00}: {shot.Position}");
            }
    }

    [Fact]
    public void EachShotIsTheOneItsMoveAsks()
    {
        ShotKind At(string move, float age) => BattleCamera.Direct(Playing(Cue(move, age)), new FxPlaces()).Kind;
        Assert.Equal(ShotKind.Push, At("Hyper Beam", 0.1f));
        Assert.Equal(ShotKind.Target, At("Hyper Beam", 0.5f));
        Assert.Equal(ShotKind.Follow, At("Roar of Time", 0.2f));
        Assert.Equal(ShotKind.Punch, At("Close Combat", I + 0.06f));
        Assert.Equal(ShotKind.Target, At("Close Combat", 1.1f));
        Assert.Equal(ShotKind.Orbit, At("Hurricane", 0.6f));
        // Held past an ordinary move's end, so the pull-back is seen
        Assert.NotEqual(ShotKind.Overview, At("Close Combat", BattleCamera.MoveShotEnd + 0.05f));
        Assert.Contains(At("Close Combat", Directions.ShotEnd + 0.01f), new[] { ShotKind.Intro, ShotKind.Overview });
        // A charge turn pushes in on its user
        Assert.Equal(ShotKind.Push, BattleCamera.Direct(Playing(Cue("Solar Beam", 0.5f, charging: true)), new FxPlaces()).Kind);
        // An ordinary move keeps its own shots
        Assert.Equal(ShotKind.Attacker, At("Ice Beam", 0.1f));
    }

    [Fact]
    public void ThePushCloses_ThePunchIsTighter_AndTheOrbitComesHome()
    {
        var places = new FxPlaces();
        var platform = places.Body(BattleSide.Enemy, 0);
        float Distance(string move, float age, BattleSide from = BattleSide.Player)
        {
            var (shot, _, _) = BattleCamera.Direct(Playing(Cue(move, age, from)), places);
            return Vector3.Distance(shot.Position, shot.Target);
        }
        Assert.True(Distance("Hyper Beam", 0.2f, BattleSide.Enemy) < Distance("Hyper Beam", 0.01f, BattleSide.Enemy));
        Assert.True(Distance("Close Combat", I + 0.1f) < Distance("Close Combat", 0.3f));
        // The orbit's swing is a sine: round and back to the plain target shot by its end
        var (start, _, _) = BattleCamera.Direct(Playing(Cue("Hurricane", BattleCamera.CutTime)), places);
        var (mid, _, _) = BattleCamera.Direct(Playing(Cue("Hurricane", 0.7f)), places);
        var (end, _, _) = BattleCamera.Direct(Playing(Cue("Hurricane", Directions.ShotEnd - 0.001f)), places);
        Assert.True(Vector3.Distance(mid.Position, start.Position) > 3f);
        Assert.True(Vector3.Distance(end.Position, start.Position) < 0.2f);
        // Swung round the foe, still looking at it
        Assert.True(Vector3.Distance(mid.Target, platform) < 0.5f);
    }
}

/// <summary>The motion number turned down (plan 12 · Q9's reduce motion): a static, so these run alone.</summary>
[Collection("Shared clocks and dice")]
public class SignatureMotionTests
{
    private static EffectCue Cue(string move, float age) => new()
    {
        Kind = CueKind.Move, Move = move, Type = PokemonType.Normal, Category = MoveCategory.Special,
        FromSide = BattleSide.Player, ToSide = BattleSide.Enemy, Seed = 3, Age = age
    };

    private static BattleAnimator Playing(EffectCue cue)
    {
        var anim = new BattleAnimator();
        float age = cue.Age;
        cue.Age = 0f;
        anim.Cue(cue);
        cue.Age = age;
        return anim;
    }

    [Fact]
    public void WithNoMotionASignatureMovePlaysAsAnOrdinaryOne()
    {
        float was = BattleCamera.Motion;
        try
        {
            BattleCamera.Motion = 0f;
            Assert.Equal(BattleAnimator.ImpactTime + 0.04f, Cue("Hyper Beam", BattleAnimator.ImpactTime + 0.04f).ShownAge);
            foreach (float age in new[] { 0.1f, 0.3f, 0.6f, 0.9f, 1.05f })
            {
                var signature = BattleCamera.Direct(Playing(Cue("Hyper Beam", age)), new FxPlaces());
                var plain = BattleCamera.Direct(Playing(Cue("Ice Beam", age)), new FxPlaces());
                Assert.Equal(plain.Kind, signature.Kind);
                Assert.Equal(plain.Shot, signature.Shot);
            }
        }
        finally { BattleCamera.Motion = was; }
    }
}
