using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>Which of the four signature shots a move takes (plan 25 · Y17; style guide, "Camera (G8)").</summary>
internal enum SignatureShot { None, ChargePush, FollowProjectile, ImpactPunch, Orbit }

/// <summary>
/// How a signature move is staged (plan 25 · Y17; style guide, "Move effects (G8)", "Signature moves"): a short
/// timeline laid over the move's own look. Every part is a pure function of the cue's (shown) age and seed, so a
/// direction holds no state and the same cue always plays the same way. GPU-free.
/// </summary>
/// <param name="Shot">The camera's shot through the move (<see cref="BattleCamera"/>).</param>
/// <param name="HitStop">How long the picture holds at the impact, seconds (scaled by <see cref="BattleCamera.Motion"/>).</param>
/// <param name="User">What the move does to its user's body at a shown age, beyond Y16's table (null: nothing).</param>
/// <param name="Target">What it does to its target's body at a shown age when it lands (null: a heavy hit's throw, or nothing).</param>
/// <param name="Light">A stage light laid over the move's own at its full (Y16's envelope eases it in and out).</param>
/// <param name="Effect">An effect laid over the move's own recipe (null: nothing).</param>
internal sealed record MoveDirection(
    SignatureShot Shot,
    float HitStop = 0f,
    Func<float, int, BodyMotion>? User = null,
    Func<float, int, BodyMotion>? Target = null,
    StageLight? Light = null,
    FxRecipe? Effect = null)
{
    /// <summary>Whether its target is thrown back at the impact (every move that stops the picture is heavy).</summary>
    public bool Heavy => HitStop > 0f;
}

/// <summary>The signature moves' directions, and the shared parts of them: the hit-stop's remap and a heavy hit's throw.</summary>
internal static class Directions
{
    private const float I = BattleAnimator.ImpactTime;

    /// <summary>A heavy hit's hold at the impact, seconds (shared with plan 25 · YD's Max Moves).</summary>
    public const float HeavyStop = 0.08f;

    /// <summary>When a signature move's camera lets go (an ordinary move's is <see cref="BattleCamera.MoveShotEnd"/>).</summary>
    public const float ShotEnd = 1.15f;

    /// <summary>How far a heavy hit throws its target back, in its height, and how flat it presses it.</summary>
    public const float ThrowReach = 0.12f, ThrowSquash = 0.08f;

    private static Color Rgb(int r, int g, int b, int a = 255) => new(r, g, b, a);

    private static float Smooth(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    /// <summary>The table, by move name. Y17a seeds it with six; Y17b on fill it.</summary>
    public static readonly IReadOnlyDictionary<string, MoveDirection> Table = new Dictionary<string, MoveDirection>
    {
        // A beam wound up and let go: the camera pushes in on the gathering, cuts to the blow; the user rocked back by it
        ["Hyper Beam"] = new(SignatureShot.ChargePush, HeavyStop, User: Recoil),
        // The same on its strike, and on its charge turn the push runs through the gathering of sunlight
        ["Solar Beam"] = new(SignatureShot.ChargePush, HeavyStop, User: Recoil),
        // A wave of time sent across: the camera rides it; the stage turns to the colour of old steel; rings of time on the target
        ["Roar of Time"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: new StageLight(new Vector3(0.16f, 0.3f, 0.42f), 0.35f), Effect: TimeRings),
        // A flurry of blows: a punch-in held on the hit
        ["Close Combat"] = new(SignatureShot.ImpactPunch, HeavyStop),
        // The dive from the sky: a punch-in on the landing
        ["Fly"] = new(SignatureShot.ImpactPunch, HeavyStop),
        // A cyclone round the target: the camera swings round it; the stage goes the grey-blue of a storm
        ["Hurricane"] = new(SignatureShot.Orbit, Light: new StageLight(new Vector3(0.3f, 0.36f, 0.46f), 0.4f, 0.25f)),
    };

    public static MoveDirection? Of(string move) => Table.TryGetValue(move, out var d) ? d : null;

    public static bool Has(string move) => Table.ContainsKey(move);

    /// <summary>Whether a cue is staged by its direction at all: a move's strike or its charge turn, nothing else.</summary>
    public static MoveDirection? For(EffectCue cue) =>
        cue.Kind == CueKind.Move && !cue.Arriving && !cue.ZMove && !cue.MaxMove ? Of(cue.Move) : null;

    /// <summary>How long a cue holds at its impact now: a landed strike of a heavy signature move, under the motion number.</summary>
    public static float StopOf(EffectCue cue)
    {
        if (cue.Charging || !cue.Landed || For(cue) is not { HitStop: > 0f } d) return 0f;
        return d.HitStop * Math.Clamp(BattleCamera.Motion, 0f, 1f);
    }

    /// <summary>
    /// The age a cue is drawn at (the hit-stop): its own age up to the impact, held there for <paramref name="stop"/>,
    /// then caught up so that it ends at <paramref name="length"/> as it would have. With no stop it is the age itself.
    /// </summary>
    public static float Remap(float age, float stop, float length)
    {
        if (stop <= 0f || age <= I || length <= I + stop) return age;
        if (age < I + stop) return I;
        if (age >= length) return age;
        return I + (age - I - stop) * (length - I) / (length - I - stop);
    }

    /// <summary>The shown age of a cue (<see cref="EffectCue.ShownAge"/>).</summary>
    public static float ShownAge(EffectCue cue)
    {
        float stop = StopOf(cue);
        return stop <= 0f ? cue.Age : Remap(cue.Age, stop, cue.Length);
    }

    /// <summary>A heavy hit's throw: back from the user and pressed a little at the impact, settling over 0.5 s.</summary>
    public static BodyMotion Thrown(float age)
    {
        if (age < I || age >= I + 0.55f) return BodyMotion.Still;
        float t = age - I;
        float k = t < 0.05f ? Smooth(t / 0.05f) : 1f - Smooth((t - 0.05f) / 0.5f);
        return new BodyMotion(Toward: -ThrowReach * k, Squash: ThrowSquash * k);
    }

    /// <summary>The user rocked back as a beam leaves it, and settling.</summary>
    private static BodyMotion Recoil(float age, int seed)
    {
        if (age < I - 0.12f || age >= I + 0.6f) return BodyMotion.Still;
        float t = age - (I - 0.12f);
        float k = t < 0.08f ? Smooth(t / 0.08f) : 1f - Smooth((t - 0.08f) / 0.55f);
        return new BodyMotion(Toward: -0.08f * k);
    }

    /// <summary>What a direction does to the user's body at a cue's shown age.</summary>
    public static BodyMotion UserBody(EffectCue cue)
    {
        if (cue.Charging || For(cue) is not { User: { } user }) return BodyMotion.Still;
        return user(cue.ShownAge, cue.Seed);
    }

    /// <summary>What a direction does to the target's body at a cue's shown age: its own, or a heavy hit's throw.</summary>
    public static BodyMotion TargetBody(EffectCue cue)
    {
        if (cue.Charging || cue.OnSelf || !cue.Landed || For(cue) is not { } d) return BodyMotion.Still;
        if (d.Target != null) return d.Target(cue.ShownAge, cue.Seed);
        return d.Heavy ? Thrown(cue.ShownAge) : BodyMotion.Still;
    }

    /// <summary>A direction's stage light at a cue's shown age (none for a charge turn).</summary>
    public static StageLight LightAt(EffectCue cue)
    {
        if (cue.Charging || For(cue) is not { Light: { } light }) return StageLight.None;
        return light.Scaled(StageLights.Envelope(cue.ShownAge, cue.Length));
    }

    /// <summary>Plays a direction's effect over the move's own recipe (nothing for a charge turn).</summary>
    public static void PlayEffect(in FxCtx c, FxColors col, FxList fx)
    {
        if (c.Cue.Charging || For(c.Cue) is not { Effect: { } effect }) return;
        effect(c, col, fx);
    }

    // ------------------------------------------------------------------ effects laid over

    /// <summary>Roar of Time: three rings of pale steel-blue standing round the target and slowing as they widen, after the impact.</summary>
    private static void TimeRings(in FxCtx c, FxColors col, FxList fx)
    {
        if (c.Cue.Missed) return;
        for (int i = 0; i < 3; i++)
        {
            float t0 = I + 0.04f + i * 0.12f;
            float k = (c.Age - t0) / 0.6f;
            if (k <= 0f || k >= 1f) continue;
            float grow = 1f - (1f - k) * (1f - k) * (1f - k);
            float alpha = 0.85f * (1f - k);
            fx.Sprite(FxShape.Ring, c.To, c.ToSize * (0.35f + 0.9f * grow), Fx.Fade(Rgb(170, 220, 255), alpha), FxBlend.Alpha);
        }
    }
}
