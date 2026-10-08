using System;
using System.Numerics;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The Pokémon animation clips (plan 04 · G7), written in code: an idle loop, a physical move's strike, a special
/// move's cast, a status move's hop, the flinch from a hit, fainting and the arrival from the Poké Ball. They move
/// bones by role and side, so one clip serves every species of a body plan, and actions are layered over the idle
/// loop. No GPU calls, so tests can pose a model and check where it ends up.
/// </summary>
internal static class PokemonAnimation
{
    /// <summary>Radians per second of the idle loop: one breath every 1.7 s at tempo 1.</summary>
    internal const float Beat = MathF.Tau / 1.7f;

    /// <summary>Which eyes the pose shows.</summary>
    public static EyeState EyesOf(PokePose p)
    {
        if (p.Faint > 0.2f) return EyeState.Shut;
        if (p.Hurt > 0.02f && p.Hurt < 0.75f) return EyeState.Squeeze;
        if (p.Attack > 0.06f && p.Attack < 0.8f && p.Kind != MoveCategory.Status) return EyeState.Fierce;
        return p.Blink > 0.5f ? EyeState.Shut : EyeState.Open;
    }

    public static void Apply(PokeModel m, PokePose p, SkeletonPose pose)
    {
        pose.Reset();
        // A fainting Pokémon stops breathing and swaying as it goes down
        float down = p.Faint > 0f ? Smooth(0f, 0.7f, p.Faint) : 0f;
        if (!m.OwnIdle) Idle(m, p.Time * m.Tempo, 1f - down, pose);
        if (p.Entry > 0f && p.Entry < 1f) Entry(m, p.Entry, pose);
        if (p.Attack > 0f && p.Attack < 1f)
        {
            switch (p.Kind)
            {
                case MoveCategory.Physical: Physical(m, p.Attack, pose); break;
                case MoveCategory.Special: Special(m, p.Attack, pose); break;
                default: Status(m, p.Attack, pose); break;
            }
        }
        if (p.Hurt > 0f && p.Hurt < 1f) Hit(m, p.Hurt, pose);
        if (p.Faint > 0f) Faint(m, Math.Min(1f, p.Faint), pose);
    }

    /// <summary>0 until <paramref name="start"/>, rising smoothly to 1 at <paramref name="peak"/> and back to 0 at <paramref name="end"/>.</summary>
    internal static float Bump(float t, float start, float peak, float end)
    {
        if (t <= start || t >= end) return 0f;
        return t < peak ? Smooth(start, peak, t) : 1f - Smooth(peak, end, t);
    }

    internal static float Smooth(float from, float to, float t)
    {
        float x = Math.Clamp((t - from) / (to - from), 0f, 1f);
        return x * x * (3f - 2f * x);
    }

    private static float Frac(float x) => x - MathF.Floor(x);

    // Turning conventions (bones turn about their joints): pitch > 0 tips a part forward, so a hanging arm or leg
    // swings back, a raised head nods down, an upright ear tips forward and a tail lying back lifts; roll by
    // side * a > 0 lifts a wing or an arm out to its side.

    // ------------------------------------------------------------------ idle

    private static void Idle(PokeModel m, float t, float amount, SkeletonPose pose)
    {
        if (amount <= 0f) return;
        float w = Beat;
        float breath = MathF.Sin(w * t) * amount;
        float h = m.Height;
        for (int b = 0; b < m.Bones.Count; b++)
        {
            var info = m.Bones[b];
            float ph = info.Phase, side = info.Side;
            switch (info.Role)
            {
                case PokeRole.Root:
                    if (m.Hovers) pose.Offset[b].Y += 0.045f * h * MathF.Sin(w * t * 0.8f) * amount;
                    break;
                case PokeRole.Body:
                    pose.Scale[b] *= new Vector3(1f + 0.012f * breath, 1f + 0.03f * breath, 1f + 0.012f * breath);
                    if (m.Hovers) pose.Turn(b, 0.04f * MathF.Sin(w * t * 0.8f + 1f) * amount);
                    if (m.Plan == BodyPlan.Fish) pose.Turn(b, 0f, 0.08f * MathF.Sin(w * t * 1.3f) * amount);
                    break;
                case PokeRole.Head:
                    pose.Turn(b, 0.06f * MathF.Sin(w * t + 0.7f) * amount, 0.1f * MathF.Sin(w * t * 0.45f + ph) * amount);
                    break;
                case PokeRole.Jaw:
                    pose.Turn(b, 0.04f * MathF.Max(0f, MathF.Sin(w * t)) * amount);
                    break;
                case PokeRole.Arm:
                    pose.Turn(b, 0.12f * MathF.Sin(w * t + ph) * amount, 0f, side * 0.05f * MathF.Sin(w * t) * amount);
                    break;
                case PokeRole.Wing:
                {
                    float flap = m.Hovers ? 0.5f * MathF.Sin(w * t * 2.2f + ph) : 0.12f * MathF.Sin(w * t + ph);
                    pose.Turn(b, 0f, 0f, side * flap * amount);
                    break;
                }
                case PokeRole.Leg:
                    // Legs stand still on the ground; in the air they dangle
                    if (m.Hovers) pose.Turn(b, 0.12f * MathF.Sin(w * t + ph) * amount);
                    break;
                case PokeRole.Tail:
                    pose.Turn(b, 0.1f * MathF.Sin(w * t + ph) * amount, 0.32f * MathF.Sin(w * t * 1.3f + ph) * amount);
                    break;
                case PokeRole.Ear:
                {
                    // A sway, and every few seconds a quick flick
                    float flick = Bump(Frac((t + ph * 0.7f + 2f) / 4.3f) * 4.3f, 0f, 0.08f, 0.22f) * amount;
                    pose.Turn(b, -0.25f * flick, 0f, -side * (0.08f * MathF.Sin(w * t * 0.9f + ph) * amount + 0.2f * flick));
                    break;
                }
                case PokeRole.Leaf:
                    pose.Turn(b, 0.08f * MathF.Sin(w * t * 0.7f + ph) * amount, 0f, 0.16f * MathF.Sin(w * t * 1.2f + ph) * amount);
                    break;
                case PokeRole.Flame:
                    // Flames flicker at their own quick pace, whatever the tempo, and die down when fainting
                    pose.Scale[b] *= new Vector3(1f + (0.08f * MathF.Sin(t * 11f + ph) + 0.05f * MathF.Sin(t * 17f)) * amount,
                        1f + (0.14f * MathF.Sin(t * 9f + ph) + 0.08f * MathF.Sin(t * 23f + 1f)) * amount - 0.5f * (1f - amount),
                        1f + 0.08f * MathF.Sin(t * 13f) * amount);
                    pose.Turn(b, 0f, 0f, 0.08f * MathF.Sin(t * 5f + ph) * amount);
                    break;
                case PokeRole.Fin:
                    if (side == 0f) pose.Turn(b, 0f, 0.35f * MathF.Sin(w * t * 1.6f + ph) * amount);
                    else pose.Turn(b, 0f, 0f, side * 0.25f * MathF.Sin(w * t * 1.6f + ph) * amount);
                    break;
                case PokeRole.Segment:
                    // A wave travels along the body from the tail to the head
                    pose.Turn(b, 0.04f * MathF.Sin(w * t - ph * 0.8f) * amount, 0.16f * MathF.Sin(w * t * 0.9f - ph * 0.9f) * amount);
                    break;
            }
        }
    }

    // ------------------------------------------------------------------ moves

    /// <summary>A physical move: drawn back, then a strike forward (the renderer adds the lunge across the field).</summary>
    private static void Physical(PokeModel m, float a, SkeletonPose pose)
    {
        float wind = Bump(a, 0f, 0.26f, 0.42f);
        float strike = Bump(a, 0.3f, 0.48f, 0.78f);
        float h = m.Height;
        bool airborne = m.Plan is BodyPlan.Bird or BodyPlan.Fish or BodyPlan.Floating;
        for (int b = 0; b < m.Bones.Count; b++)
        {
            var info = m.Bones[b];
            float side = info.Side;
            switch (info.Role)
            {
                case PokeRole.Root:
                    if (airborne) pose.Turn(b, 0.3f * strike - 0.1f * wind);
                    break;
                case PokeRole.Body:
                    pose.Turn(b, -0.16f * wind + 0.24f * strike);
                    pose.Offset[b].Y -= 0.04f * h * wind;
                    pose.Scale[b] *= new Vector3(1f + 0.04f * wind, 1f - 0.07f * wind, 1f + 0.04f * wind);
                    break;
                case PokeRole.Head:
                    pose.Turn(b, -0.28f * wind + 0.38f * strike);
                    break;
                case PokeRole.Jaw:
                    pose.Turn(b, 0.6f * strike);
                    break;
                case PokeRole.Arm:
                    pose.Turn(b, 0.9f * wind - 1.5f * strike, 0f, side * 0.25f * wind);
                    break;
                case PokeRole.Wing:
                    pose.Turn(b, 0f, -side * 0.7f * strike, side * 0.9f * wind);
                    break;
                case PokeRole.Leg:
                    if (m.Plan == BodyPlan.Quadruped) pose.Turn(b, info.Front ? -0.45f * strike + 0.15f * wind : 0.3f * strike);
                    else if (m.Plan == BodyPlan.Biped) pose.Turn(b, side < 0f ? -0.35f * strike : 0.2f * strike);
                    break;
                case PokeRole.Tail:
                    pose.Turn(b, 0.45f * strike - 0.15f * wind);
                    break;
                case PokeRole.Ear:
                    pose.Turn(b, -0.3f * (wind + strike));
                    break;
                case PokeRole.Flame:
                    pose.Scale[b] *= 1f + 0.35f * strike;
                    break;
                case PokeRole.Segment:
                    // Each segment adds its turn to the ones before it, so a little each makes a strike
                    pose.Turn(b, -0.06f * wind + 0.06f * strike);
                    break;
                case PokeRole.Fin:
                    pose.Turn(b, 0f, 0f, side * 0.4f * strike);
                    break;
            }
        }
    }

    /// <summary>A special move: rears up gathering power, then thrusts toward the target with a cry. It stays in place.</summary>
    private static void Special(PokeModel m, float a, SkeletonPose pose)
    {
        float charge = Bump(a, 0f, 0.36f, 0.64f);
        float release = Bump(a, 0.42f, 0.56f, 0.86f);
        float h = m.Height;
        for (int b = 0; b < m.Bones.Count; b++)
        {
            var info = m.Bones[b];
            float side = info.Side;
            switch (info.Role)
            {
                case PokeRole.Root:
                    pose.Offset[b].Y += (m.Hovers ? 0.06f : 0.02f) * h * charge;
                    break;
                case PokeRole.Body:
                    pose.Turn(b, -0.18f * charge + 0.12f * release);
                    pose.Scale[b] *= new Vector3(1f + 0.03f * charge, 1f + 0.05f * charge, 1f + 0.03f * charge);
                    break;
                case PokeRole.Head:
                    pose.Turn(b, -0.32f * charge + 0.3f * release);
                    break;
                case PokeRole.Jaw:
                    pose.Turn(b, 0.15f * charge + 0.55f * release);
                    break;
                case PokeRole.Arm:
                    pose.Turn(b, -0.5f * charge - 0.9f * release, 0f, side * 0.8f * charge);
                    break;
                case PokeRole.Wing:
                    pose.Turn(b, 0f, 0f, side * (1f * charge - 0.5f * release));
                    break;
                case PokeRole.Leg:
                    if (m.Plan == BodyPlan.Quadruped && info.Front) pose.Turn(b, -0.12f * charge);
                    break;
                case PokeRole.Tail:
                    pose.Turn(b, 0.4f * charge);
                    break;
                case PokeRole.Ear:
                    pose.Turn(b, -0.2f * charge);
                    break;
                case PokeRole.Flame:
                    pose.Scale[b] *= 1f + 0.5f * charge + 0.3f * release;
                    break;
                case PokeRole.Leaf:
                    pose.Turn(b, 0f, 0f, 0.25f * charge * MathF.Sin(info.Phase + 1f));
                    break;
                case PokeRole.Segment:
                    pose.Turn(b, -0.06f * charge + 0.04f * release);
                    break;
                case PokeRole.Fin:
                    pose.Turn(b, 0f, 0f, side * 0.5f * charge);
                    break;
            }
        }
    }

    /// <summary>A status move: a little hop and a nod.</summary>
    private static void Status(PokeModel m, float a, SkeletonPose pose)
    {
        float hop = MathF.Sin(MathF.PI * Smooth(0.15f, 0.75f, a));
        float squash = Bump(a, 0f, 0.1f, 0.22f) + Bump(a, 0.72f, 0.84f, 1f);
        float h = m.Height;
        for (int b = 0; b < m.Bones.Count; b++)
        {
            var info = m.Bones[b];
            float side = info.Side;
            switch (info.Role)
            {
                case PokeRole.Root:
                    pose.Offset[b].Y += 0.06f * h * hop;
                    break;
                case PokeRole.Body:
                    pose.Scale[b] *= new Vector3(1f + 0.06f * squash, 1f - 0.1f * squash, 1f + 0.06f * squash);
                    break;
                case PokeRole.Head:
                    pose.Turn(b, 0.22f * MathF.Sin(MathF.Tau * a) * (1f - a));
                    break;
                case PokeRole.Arm:
                    pose.Turn(b, 0f, 0f, side * 0.4f * hop);
                    break;
                case PokeRole.Wing:
                    pose.Turn(b, 0f, 0f, side * 0.6f * hop);
                    break;
                case PokeRole.Tail:
                    pose.Turn(b, 0.3f * hop);
                    break;
                case PokeRole.Ear:
                    pose.Turn(b, -0.2f * hop);
                    break;
            }
        }
    }

    /// <summary>Knocked back by a hit: the body recoils and squashes, the head snaps back, limbs fly out.</summary>
    private static void Hit(PokeModel m, float t, SkeletonPose pose)
    {
        float env = t < 0.12f ? Smooth(0f, 0.12f, t) : 1f - Smooth(0.12f, 1f, t);
        float jolt = MathF.Sin(t * 38f) * env;
        float h = m.Height;
        for (int b = 0; b < m.Bones.Count; b++)
        {
            var info = m.Bones[b];
            float side = info.Side;
            switch (info.Role)
            {
                case PokeRole.Root:
                    pose.Turn(b, -0.06f * env);
                    pose.Offset[b].Z -= 0.035f * h * env;
                    break;
                case PokeRole.Body:
                    pose.Turn(b, -0.12f * env);
                    pose.Scale[b] *= new Vector3(1f + 0.05f * env, 1f - 0.08f * env, 1f + 0.05f * env);
                    break;
                case PokeRole.Head:
                    pose.Turn(b, -0.2f * env, 0.12f * jolt);
                    break;
                case PokeRole.Jaw:
                    pose.Turn(b, 0.25f * env);
                    break;
                case PokeRole.Arm:
                    pose.Turn(b, -0.3f * env, 0f, side * 0.45f * env);
                    break;
                case PokeRole.Wing:
                    pose.Turn(b, 0f, 0f, side * 0.6f * env);
                    break;
                case PokeRole.Leg:
                    if (m.Plan == BodyPlan.Quadruped && !info.Front) pose.Turn(b, -0.15f * env);
                    break;
                case PokeRole.Tail:
                    pose.Turn(b, -0.45f * env);
                    break;
                case PokeRole.Ear:
                    pose.Turn(b, -0.45f * env);
                    break;
                case PokeRole.Segment:
                    pose.Turn(b, -0.05f * env, 0.05f * jolt);
                    break;
                case PokeRole.Fin:
                    pose.Turn(b, 0f, 0f, side * 0.5f * env);
                    break;
            }
        }
    }

    /// <summary>
    /// Fainting: the legs give way, then the body goes down (a biped topples forward, a quadruped sinks onto its
    /// belly with its legs splayed, a flier drops onto its side, a serpent lays its head down).
    /// </summary>
    private static void Faint(PokeModel m, float f, SkeletonPose pose)
    {
        float give = Smooth(0f, 0.45f, f);
        float fall = Smooth(0.25f, 0.85f, f);
        float h = m.Height;
        int body = m.Find(PokeRole.Body);
        float bodyHeight = body >= 0 ? m.Skeleton[body].Joint.Y : h * 0.4f;
        bool airborne = m.Hovers || m.Plan is BodyPlan.Bird or BodyPlan.Fish or BodyPlan.Floating;
        for (int b = 0; b < m.Bones.Count; b++)
        {
            var info = m.Bones[b];
            float side = info.Side;
            switch (info.Role)
            {
                case PokeRole.Root:
                    if (m.Plan == BodyPlan.Biped) pose.Turn(b, 1.2f * fall);
                    else if (m.Plan == BodyPlan.Quadruped) pose.Turn(b, 0f, 0f, 0.25f * fall);
                    else if (airborne)
                    {
                        pose.Offset[b].Y -= 0.04f * h * give;
                        pose.Turn(b, 0.35f * fall, 0f, 1f * fall);
                    }
                    break;
                case PokeRole.Body:
                    if (m.Plan == BodyPlan.Biped)
                    {
                        pose.Offset[b].Y -= 0.25f * bodyHeight * give;
                        pose.Turn(b, 0.25f * give - 0.1f * fall);
                    }
                    else if (m.Plan == BodyPlan.Quadruped)
                    {
                        pose.Offset[b].Y -= 0.55f * bodyHeight * fall;
                        pose.Turn(b, 0.1f * fall);
                    }
                    else pose.Turn(b, 0.2f * give);
                    break;
                case PokeRole.Head:
                    pose.Turn(b, m.Plan switch
                    {
                        BodyPlan.Serpent => 0.7f * fall,
                        BodyPlan.Biped => 0.35f * give - 0.1f * fall,
                        _ => 0.5f * give + 0.2f * fall
                    });
                    break;
                case PokeRole.Jaw:
                    pose.Turn(b, 0.15f * give);
                    break;
                case PokeRole.Arm:
                    pose.Turn(b, -0.4f * give, 0f, -side * 0.2f * give);
                    break;
                case PokeRole.Wing:
                    pose.Turn(b, 0f, 0f, -side * 0.9f * give);
                    break;
                case PokeRole.Leg:
                    if (m.Plan == BodyPlan.Quadruped) pose.Turn(b, info.Front ? -0.7f * fall : 0.7f * fall, 0f, side * 0.25f * fall);
                    else if (m.Plan == BodyPlan.Biped) pose.Turn(b, -0.3f * fall);
                    else pose.Turn(b, 0.35f * give);
                    break;
                case PokeRole.Tail:
                    pose.Turn(b, -0.3f * give);
                    break;
                case PokeRole.Ear:
                    pose.Turn(b, 0.6f * give);
                    break;
                case PokeRole.Segment:
                    pose.Turn(b, 0.12f * fall);
                    break;
                case PokeRole.Fin:
                    pose.Turn(b, 0f, 0f, -side * 0.4f * give);
                    break;
            }
        }
    }

    /// <summary>Out of the ball: it lands with a squash, then stands tall with a cry, arms and wings spread.</summary>
    private static void Entry(PokeModel m, float e, SkeletonPose pose)
    {
        float land = Bump(e, 0.22f, 0.4f, 0.62f);
        float proud = Bump(e, 0.45f, 0.72f, 1f);
        for (int b = 0; b < m.Bones.Count; b++)
        {
            var info = m.Bones[b];
            float side = info.Side;
            switch (info.Role)
            {
                case PokeRole.Body:
                    pose.Scale[b] *= new Vector3(1f + 0.08f * land, 1f - 0.12f * land + 0.04f * proud, 1f + 0.08f * land);
                    break;
                case PokeRole.Head:
                    pose.Turn(b, -0.28f * proud);
                    break;
                case PokeRole.Jaw:
                    pose.Turn(b, 0.5f * proud);
                    break;
                case PokeRole.Arm:
                    pose.Turn(b, -0.4f * proud, 0f, side * 0.7f * proud);
                    break;
                case PokeRole.Wing:
                    pose.Turn(b, 0f, 0f, side * 0.9f * proud);
                    break;
                case PokeRole.Tail:
                    pose.Turn(b, 0.3f * proud);
                    break;
                case PokeRole.Ear:
                    pose.Turn(b, -0.25f * proud);
                    break;
                case PokeRole.Flame:
                    pose.Scale[b] *= 1f + 0.4f * proud;
                    break;
                case PokeRole.Fin:
                    pose.Turn(b, 0f, 0f, side * 0.4f * proud);
                    break;
            }
        }
    }
}
