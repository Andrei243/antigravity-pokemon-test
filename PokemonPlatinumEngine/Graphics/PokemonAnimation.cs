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

    /// <summary>The seconds of one breath of a model's idle loop: 1.7 s at tempo 1 (plan 09 · L5's strip spans one).</summary>
    internal static float BreathSeconds(PokeModel m) => MathF.Tau / Beat / MathF.Max(0.1f, m.Tempo);

    /// <summary>
    /// Frame <paramref name="frame"/> of <paramref name="frames"/> of the menus' strip (plan 09 · L5; style guide,
    /// "Pokémon in menus"): the idle loop at <paramref name="start"/> plus that share of a breath, blended toward the
    /// loop a breath earlier by the same share. The breath repeats, but the slower sways, the ears' flicks and the
    /// flames don't: the blend folds them back, so frame <paramref name="frames"/> (the one after the last) is frame 0
    /// exactly, and the strip loops with no jump. Frame 0 is the still's pose. <paramref name="behind"/> is scratch.
    /// </summary>
    internal static void StripPose(PokeModel m, int frame, int frames, float start, SkeletonPose into, SkeletonPose behind)
    {
        float period = BreathSeconds(m), share = Math.Clamp(frame / (float)frames, 0f, 1f);
        float t = start + period * share;
        Apply(m, new PokePose { Time = t }, into);
        if (share <= 0f) return;
        Apply(m, new PokePose { Time = t - period }, behind);
        into.BlendToward(behind, share);
    }

    /// <summary>
    /// Which eyes the pose shows: the first that holds, in the order the style guide keeps ("The pose's fields and
    /// the eyes' order"; plan 09 · L2). Plan 25 · Y5's and plan 10 · F6's and F10's cases go in the places it names.
    /// </summary>
    public static EyeState EyesOf(PokePose p)
    {
        // 1. A faint
        if (p.Faint > 0.2f) return EyeState.Shut;
        // 2. Asleep: shut whatever else happens to it (hit in its sleep, striking with Snore or Sleep Talk)
        if (p.Asleep) return EyeState.Shut;
        // 3. A hit
        if (p.Hurt > 0.02f && p.Hurt < 0.75f) return EyeState.Squeeze;
        // 4. An attack that is no status move, or a charge (Y5)
        if (p.Attack > 0.06f && p.Attack < 0.8f && p.Kind != MoveCategory.Status) return EyeState.Fierce;
        if (p.Charge > 0.3f) return EyeState.Fierce;
        // 5. A held fierce look (F10) goes here
        // 6. A stat falling, squeezed, or rising, fierce, for its 0.6 s (Y5)
        if (p.Stat < 0f) return EyeState.Squeeze;
        if (p.Stat > 0f) return EyeState.Fierce;
        // 7. Frozen: open and held; nothing below reaches a frozen face
        if (p.Frozen) return EyeState.Open;
        // 8. A cheer (Y5): smiling; the camp's faces (F6) go here too
        if (p.Cheer > 0.04f && p.Cheer < 0.96f) return EyeState.Smile;
        // 9. Tiredness: longer, slower blinks in place of the blink below (Y5)
        if (p.Tired > 0.5f) return Frac(p.Time / TiredBlinkEvery) * TiredBlinkEvery < TiredBlinkLength ? EyeState.Shut : EyeState.Open;
        // 10. A blink, 11. open
        return p.Blink > 0.5f ? EyeState.Shut : EyeState.Open;
    }

    /// <summary>How often a tired Pokémon blinks, and how long its eyes stay shut (plan 25 · Y5): one slow breath each.</summary>
    internal const float TiredBlinkEvery = TiredBreath, TiredBlinkLength = 0.45f;

    /// <summary>Seconds of a tired Pokémon's slow, deep breath (plan 25 · Y5).</summary>
    internal const float TiredBreath = 2.6f;

    /// <summary>How far through its dodge a body is furthest aside: the miss passes then.</summary>
    internal const float DodgePeak = Battle.BattleAnimator.ImpactTime / Battle.BattleAnimator.DodgeTime;

    /// <summary>
    /// The way a dodge goes, level with the ground: away from the side a miss flies past on (<see cref="FxCtx.Aim"/>
    /// goes wide along the cross of the move's way with the up axis; the dodge takes the other side).
    /// </summary>
    internal static Vector3 DodgeAway(Vector3 from, Vector3 to)
    {
        var dir = new Vector3(to.X - from.X, 0f, to.Z - from.Z);
        if (dir.LengthSquared() < 1e-6f) return Vector3.UnitX;
        return -Vector3.Normalize(Vector3.Cross(dir, Vector3.UnitY));
    }

    /// <summary>How far aside a dodge has taken the body at <paramref name="d"/> of its clip: 0 at rest, 1 at its furthest.</summary>
    internal static float DodgeAmount(float d) => d <= 0f || d >= 1f ? 0f : Bump(d, 0f, DodgePeak, 1f);

    /// <summary>
    /// How far aside a dodge takes the whole body, in its own height (style guide, "Bodies that answer"): a third for
    /// one that hops, a quarter for one that banks or dips, a fifth for a serpent, which mostly coils. The renderer
    /// moves it this far away from the miss's path, whatever clip plays: which side the miss passes on is the
    /// battle's, so an imported model's own dodge clip plays the body's part only.
    /// </summary>
    internal static float DodgeAside(PokeModel m, float d)
    {
        float share = m.Plan switch
        {
            BodyPlan.Biped or BodyPlan.Quadruped when !m.Hovers => 0.34f,
            BodyPlan.Serpent => 0.2f,
            _ => 0.25f
        };
        return share * DodgeAmount(d);
    }

    /// <summary>The moment of its idle loop a frozen Pokémon is held at: the loop's start, whatever the clock says.</summary>
    internal const float HeldMoment = 0f;

    /// <summary>The time the idle loop is played at: the pose's clock, or one moment for a frozen Pokémon (plan 09 · L2).</summary>
    internal static float IdleTime(PokePose p) => p.Frozen ? HeldMoment : p.Time;

    public static void Apply(PokeModel m, PokePose p, SkeletonPose pose)
    {
        pose.Reset();
        // A fainting Pokémon stops breathing and swaying as it goes down
        float down = p.Faint > 0f ? Smooth(0f, 0.7f, p.Faint) : 0f;
        // A tired one's slow, deep breath takes over most of the idle's; a charge stills it (plan 25 · Y5)
        float tired = Math.Clamp(p.Tired, 0f, 1f) * (1f - down), charge = Math.Clamp(p.Charge, 0f, 1f) * (1f - down);
        if (!m.OwnIdle) Idle(m, IdleTime(p) * m.Tempo, (1f - down) * (1f - 0.6f * tired) * (1f - 0.5f * charge), pose);
        if (tired > 0f && !(m.Imported?.Has(ClipRole.Tired) == true)) Tired(m, tired, IdleTime(p), pose);
        if (charge > 0f) Charge(m, charge, IdleTime(p), pose);
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
        if (p.Dodge > 0f && p.Dodge < 1f) Dodge(m, p.Dodge, pose);
        if (p.Cheer > 0f && p.Cheer < 1f) Cheer(m, p.Cheer, pose);
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

    // ------------------------------------------------------------------ a battle's answers (plan 25 · Y5)

    /// <summary>
    /// A dodge from a move that misses: a biped or a quadruped hops (aside, then back), a bird banks, a fish or a
    /// floater dips, a serpent coils. The renderer moves the whole body aside, away from the miss's path, by
    /// <see cref="DodgeAside"/>; this is what the body does meanwhile. It starts and ends at rest.
    /// </summary>
    private static void Dodge(PokeModel m, float d, SkeletonPose pose)
    {
        float amt = DodgeAmount(d);
        if (amt <= 0f) return;
        float h = m.Height;
        bool hops = m.Plan is BodyPlan.Biped or BodyPlan.Quadruped && !m.Hovers;
        // Two hops for one that stands: off to the side as the miss comes, and back after it
        float hop = Bump(d, 0f, DodgePeak * 0.6f, DodgePeak * 1.15f) + 0.55f * Bump(d, 0.6f, 0.76f, 0.94f);
        float land = Bump(d, DodgePeak * 0.95f, DodgePeak * 1.3f, 0.62f);
        for (int b = 0; b < m.Bones.Count; b++)
        {
            var info = m.Bones[b];
            float side = info.Side;
            switch (info.Role)
            {
                case PokeRole.Root:
                    if (hops) pose.Offset[b].Y += 0.13f * h * hop;
                    else if (m.Plan == BodyPlan.Bird) pose.Turn(b, -0.08f * amt, 0f, 0.55f * amt);
                    else if (m.Plan is BodyPlan.Fish or BodyPlan.Floating || m.Hovers)
                    {
                        pose.Offset[b].Y -= 0.12f * h * amt;
                        pose.Turn(b, 0.12f * amt, 0f, 0.15f * amt);
                    }
                    else if (m.Plan == BodyPlan.Serpent) pose.Offset[b].Y -= 0.02f * h * amt;
                    break;
                case PokeRole.Body:
                    if (hops) pose.Scale[b] *= new Vector3(1f + 0.06f * land, 1f - 0.09f * land, 1f + 0.06f * land);
                    pose.Turn(b, -0.08f * amt, 0f, 0.12f * amt);
                    break;
                case PokeRole.Head:
                    // It pulls its head back from what passes
                    pose.Turn(b, -0.25f * amt, 0.15f * amt);
                    break;
                case PokeRole.Arm:
                    pose.Turn(b, -0.2f * amt, 0f, side * 0.5f * amt);
                    break;
                case PokeRole.Wing:
                    // Banking: one wing up, the other down
                    pose.Turn(b, 0f, 0f, (m.Plan == BodyPlan.Bird ? 0.7f : side * 0.5f) * amt);
                    break;
                case PokeRole.Leg:
                    // Tucked up while off the ground
                    if (hops) pose.Turn(b, (info.Front || m.Plan == BodyPlan.Biped ? -0.3f : 0.3f) * hop);
                    break;
                case PokeRole.Tail:
                    pose.Turn(b, 0.2f * amt, 0.35f * amt);
                    break;
                case PokeRole.Ear:
                    pose.Turn(b, -0.35f * amt);
                    break;
                case PokeRole.Segment:
                    // A coil: each segment bends a little the same way, which adds up along the body
                    pose.Turn(b, -0.04f * amt, 0.14f * amt);
                    break;
                case PokeRole.Fin:
                    pose.Turn(b, 0f, 0f, side * 0.45f * amt);
                    break;
            }
        }
    }

    /// <summary>
    /// Tired, while its HP bar is in the red: the head lowers, the limbs droop, a flier hangs lower, and a slow, deep
    /// breath (one every <see cref="TiredBreath"/> s) heaves the body. Played at the idle's time, so a frozen body holds it.
    /// </summary>
    private static void Tired(PokeModel m, float a, float t, SkeletonPose pose)
    {
        float breath = MathF.Sin(MathF.Tau * t / TiredBreath) * a;
        float h = m.Height;
        for (int b = 0; b < m.Bones.Count; b++)
        {
            var info = m.Bones[b];
            float side = info.Side;
            switch (info.Role)
            {
                case PokeRole.Root:
                    if (m.Hovers) pose.Offset[b].Y -= 0.06f * h * a;
                    break;
                case PokeRole.Body:
                    pose.Offset[b].Y -= 0.02f * h * a;
                    pose.Turn(b, 0.1f * a + 0.02f * breath);
                    pose.Scale[b] *= new Vector3(1f + 0.025f * breath, 1f + 0.055f * breath, 1f + 0.025f * breath);
                    break;
                case PokeRole.Head:
                    pose.Turn(b, 0.3f * a - 0.05f * breath);
                    break;
                case PokeRole.Jaw:
                    // A breath taken through the mouth
                    pose.Turn(b, 0.12f * MathF.Max(0f, breath));
                    break;
                case PokeRole.Arm:
                    pose.Turn(b, 0.15f * a, 0f, -side * 0.1f * a);
                    break;
                case PokeRole.Wing:
                    pose.Turn(b, 0f, 0f, -side * (0.4f * a - 0.06f * breath));
                    break;
                case PokeRole.Leg:
                    if (m.Hovers) pose.Turn(b, 0.2f * a);
                    break;
                case PokeRole.Tail:
                    pose.Turn(b, -0.35f * a);
                    break;
                case PokeRole.Ear:
                    pose.Turn(b, 0.4f * a);
                    break;
                case PokeRole.Leaf:
                    pose.Turn(b, 0.25f * a);
                    break;
                case PokeRole.Flame:
                    pose.Scale[b] *= 1f - 0.25f * a;
                    break;
                case PokeRole.Segment:
                    pose.Turn(b, 0.04f * a);
                    break;
                case PokeRole.Fin:
                    pose.Turn(b, 0f, 0f, -side * 0.2f * a);
                    break;
            }
        }
    }

    /// <summary>
    /// Through a charge turn: the body gathers itself low, its head down and drawn in, its arms and wings folded, a
    /// little tremble running through it. Played at the idle's time, so a frozen body holds it.
    /// </summary>
    private static void Charge(PokeModel m, float a, float t, SkeletonPose pose)
    {
        float tremble = MathF.Sin(t * 41f) * 0.02f * a;
        float pulse = 0.5f + 0.5f * MathF.Sin(t * 7f);
        float h = m.Height;
        for (int b = 0; b < m.Bones.Count; b++)
        {
            var info = m.Bones[b];
            float side = info.Side;
            switch (info.Role)
            {
                case PokeRole.Root:
                    if (m.Hovers) pose.Offset[b].Y -= 0.04f * h * a;
                    break;
                case PokeRole.Body:
                    pose.Offset[b].Y -= 0.06f * h * a;
                    pose.Turn(b, 0.14f * a, tremble);
                    pose.Scale[b] *= new Vector3(1f + 0.05f * a, 1f - 0.08f * a, 1f + 0.05f * a);
                    break;
                case PokeRole.Head:
                    pose.Turn(b, 0.28f * a, tremble);
                    break;
                case PokeRole.Arm:
                    pose.Turn(b, -0.55f * a, 0f, -side * 0.25f * a);
                    break;
                case PokeRole.Wing:
                    pose.Turn(b, 0f, 0f, -side * 0.55f * a);
                    break;
                case PokeRole.Tail:
                    pose.Turn(b, 0.3f * a, tremble * 3f);
                    break;
                case PokeRole.Ear:
                    pose.Turn(b, -0.35f * a);
                    break;
                case PokeRole.Flame:
                    pose.Scale[b] *= 1f + (0.25f + 0.15f * pulse) * a;
                    break;
                case PokeRole.Leaf:
                    pose.Turn(b, -0.2f * a);
                    break;
                case PokeRole.Segment:
                    pose.Turn(b, -0.05f * a, tremble);
                    break;
                case PokeRole.Fin:
                    pose.Turn(b, 0f, 0f, side * 0.3f * a);
                    break;
            }
        }
    }

    /// <summary>A cheer: two hops with the head thrown back and the arms, wings and tail up. It starts and ends at rest.</summary>
    private static void Cheer(PokeModel m, float c, SkeletonPose pose)
    {
        float env = Smooth(0f, 0.12f, c) * (1f - Smooth(0.86f, 1f, c));
        if (env <= 0f) return;
        // Two hops, landing at the clip's middle and end
        float hop = MathF.Abs(MathF.Sin(MathF.Tau * c)) * env;
        float squash = env * MathF.Pow(1f - MathF.Abs(MathF.Sin(MathF.Tau * c)), 4f);
        float h = m.Height;
        for (int b = 0; b < m.Bones.Count; b++)
        {
            var info = m.Bones[b];
            float side = info.Side;
            switch (info.Role)
            {
                case PokeRole.Root:
                    pose.Offset[b].Y += 0.12f * h * hop;
                    break;
                case PokeRole.Body:
                    pose.Turn(b, -0.1f * env);
                    pose.Scale[b] *= new Vector3(1f + 0.06f * squash, 1f - 0.09f * squash, 1f + 0.06f * squash);
                    break;
                case PokeRole.Head:
                    pose.Turn(b, -0.35f * env);
                    break;
                case PokeRole.Jaw:
                    pose.Turn(b, 0.45f * env);
                    break;
                case PokeRole.Arm:
                    pose.Turn(b, -1.7f * env, 0f, side * 0.5f * env);
                    break;
                case PokeRole.Wing:
                    pose.Turn(b, 0f, 0f, side * (0.8f + 0.3f * hop) * env);
                    break;
                case PokeRole.Leg:
                    if (m.Plan == BodyPlan.Quadruped) pose.Turn(b, (info.Front ? -0.25f : 0.2f) * hop);
                    break;
                case PokeRole.Tail:
                    pose.Turn(b, 0.5f * env, 0.3f * MathF.Sin(MathF.Tau * c * 3f) * env);
                    break;
                case PokeRole.Ear:
                    pose.Turn(b, -0.3f * env);
                    break;
                case PokeRole.Flame:
                    pose.Scale[b] *= 1f + 0.4f * env;
                    break;
                case PokeRole.Segment:
                    pose.Turn(b, -0.06f * env);
                    break;
                case PokeRole.Fin:
                    pose.Turn(b, 0f, 0f, side * 0.4f * env);
                    break;
            }
        }
    }
}
