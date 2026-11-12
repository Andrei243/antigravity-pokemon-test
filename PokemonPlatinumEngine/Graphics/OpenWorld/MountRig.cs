using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using PokemonPlatinumEngine.OpenWorld;

namespace PokemonPlatinumEngine.Graphics.OpenWorld;

/// <summary>
/// Where a rider sits on a ride form's model (plan 30 · FM13, the plan's <c>PokeModel.Seat</c>), in the model's own
/// units at rest: on the top of the body's surface on its seat line, carried through the clips by <paramref name="Bone"/>
/// (the body's bone), with the body's half width there for the rider's thighs.
/// </summary>
internal readonly record struct MountSeat(Vector3 Rest, float HalfWidth, int Bone);

/// <summary>What the field tells the mount's rig each frame (plan 30 · FM13): who rides, what the body does, where its feet are drawn.</summary>
internal readonly record struct MountInput(bool Mounted, int Mount, OpenStance Stance, Vector3 Feet, float Yaw, (float X, float Z) Cling, double Time);

/// <summary>
/// A frame of the mount (plan 30 · FM13): the form shown, how it is turned (no scale), where its root stands and at what
/// size, its pose and its flash; and the rider: how far between the ground and the seat (1 seated), where they stand on
/// the ground, their stance and their crouch.
/// </summary>
internal readonly record struct MountFrame(
    string Form, Matrix4x4 Turn, Vector3 Origin, float Scale, PokePose Pose, float Flash, bool Afloat,
    float Seated, Vector3 Ground, RideStance Stance, float RunBlend)
{
    /// <summary>The model's root: its scale, its turn, its place.</summary>
    public Matrix4x4 Root => Matrix4x4.CreateScale(Scale) * Turn * Matrix4x4.CreateTranslation(Origin);

    /// <summary>The way up of the mount's back, in the world.</summary>
    public Vector3 Up => Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitY, Turn));
}

/// <summary>
/// The mount as a rig (plan 30 · FM13; style guide, "Open world", "The mount"): the ridden Pokémon drawn as its battle
/// model in the form <see cref="MountRules.FormFor"/> names, at one size for the whole species, its clips by its speed,
/// a change of form a short flash with the swap at its middle, its way of standing by the body's stance (level, afloat,
/// nose down in a glide, belly to the face climbing), and the rider's hop on and off. The picture's alone: the rules
/// never read it. GPU-free.
/// </summary>
internal sealed class MountRig
{
    /// <summary>Metres a model unit, one for every form of a species, so a change of form never jumps in size: the riding forms' backs about 1.15 m off the ground.</summary>
    public static float MetresPerUnit(int mount) => mount == MountRules.Miraidon ? 2.4f : 2.3f;

    /// <summary>A change of form: its length, the flash at its middle, and how much the body is squashed there.</summary>
    public const float ChangeSeconds = 0.3f, ChangeFlash = 0.7f, ChangeSquash = 0.12f;

    /// <summary>Getting on: the mount comes out of its ball over <see cref="OutSeconds"/> (from half its size, white), is ridden from <see cref="RiddenAfter"/>, and the rider's hop runs between <see cref="HopFrom"/> and <see cref="OnSeconds"/>.</summary>
    public const float OutSeconds = 0.2f, RiddenAfter = 0.2f, HopFrom = 0.1f, OnSeconds = 0.45f;

    /// <summary>Getting off: the rider's hop down over <see cref="HopDown"/>, the mount going back between <see cref="BackFrom"/> and <see cref="OffSeconds"/>.</summary>
    public const float HopDown = 0.3f, BackFrom = 0.15f, OffSeconds = 0.4f;

    /// <summary>How high a hop on or off rises over the higher of its two ends, and how far beside the mount the rider stands before getting on.</summary>
    public const float HopRise = 0.5f, Beside = 0.9f;

    /// <summary>A glide's nose down, in radians (6°); a climb's tilt up from level, in radians (75°).</summary>
    public const float GlidePitch = 6f * MathF.PI / 180f, ClimbTilt = 75f * MathF.PI / 180f;

    /// <summary>How far the mount's belly stands off the face it climbs: the capsule's radius less this.</summary>
    public const float ClimbIn = 0.12f;

    /// <summary>A stride of the mount's clip (two steps) by what it does, in metres.</summary>
    public const float TrotStride = 2.6f, SwimStride = 2.2f, ClimbStride = 1.4f;

    /// <summary>Over this speed (metres a second) the rider crouches into a dash; the crouch runs from <see cref="CrouchFrom"/> to it.</summary>
    public const float DashOver = 8f, CrouchFrom = 2f;

    private bool wasMounted;
    private int mount;
    private string? target, from;
    private float changeAge = float.PositiveInfinity, onAge = float.PositiveInfinity, offAge = float.PositiveInfinity;
    private float walk, moving;
    private Vector3? lastFeet;
    private MountFrame? last;

    /// <summary>The form the rig wants now (after a change it shows), or null.</summary>
    public string? Target => target;

    /// <summary>
    /// Forgets everything at once (a fade's midpoint took the player off, a map was come into): no dismount is played
    /// afterwards, and the next ride begins with the mount coming out of its ball.
    /// </summary>
    public void Reset()
    {
        wasMounted = false;
        last = null;
        target = from = null;
        changeAge = onAge = offAge = float.PositiveInfinity;
        walk = moving = 0f;
        lastFeet = null;
    }

    /// <summary>The frame of the mount and its rider, or null where nothing of the mount is drawn (on foot, and once a dismount is over).</summary>
    public MountFrame? Step(float dt, in MountInput input)
    {
        dt = Math.Max(0f, dt);
        if (input.Mounted && !wasMounted)
        {
            mount = input.Mount;
            onAge = 0f;
            offAge = float.PositiveInfinity;
            target = MountRules.FormFor(mount, OpenStance.Standing, ridden: false);
            from = null;
            changeAge = float.PositiveInfinity;
            walk = 0f;
            moving = 0f;
            lastFeet = null;
        }
        if (!input.Mounted && wasMounted)
            offAge = last != null ? 0f : float.PositiveInfinity;
        wasMounted = input.Mounted;

        if (!input.Mounted) return Dismounting(dt);
        if (MountRules.SpeciesOf(mount) == null) return null;

        // How fast the feet move: along the ground, and up and down a face while climbing
        float speed = 0f;
        if (lastFeet is { } before && dt > 0f)
        {
            var d = input.Feet - before;
            float metres = input.Stance == OpenStance.Climbing ? d.Length() : new Vector2(d.X, d.Z).Length();
            if (metres < 3f) speed = metres / dt;
        }
        lastFeet = input.Feet;

        // The form wanted, and a change toward it
        bool ridden = onAge >= RiddenAfter;
        string wanted = MountRules.FormFor(mount, input.Stance, ridden)!;
        if (wanted != target)
        {
            from = ShownForm();
            target = wanted;
            changeAge = 0f;
        }
        string shown = ShownForm()!;

        // The clip: a stride by what it does, eased in and out
        float stride = input.Stance switch { OpenStance.Swimming => SwimStride, OpenStance.Climbing => ClimbStride, _ => TrotStride };
        bool still = input.Stance is OpenStance.Gliding or OpenStance.Falling;
        walk = (walk + (still ? 0f : speed * dt / stride)) % 1f;
        float want = !still && speed > 0.3f ? 1f : 0f;
        moving += (want - moving) * Math.Min(1f, dt * 6f);
        var pose = new PokePose { Time = (float)(input.Time % 3600.0), Walk = walk, Moving = moving };

        // Its size and its flash: out of its ball, and through a change of form
        float scale = MetresPerUnit(mount), flash = 0f;
        if (onAge < OutSeconds)
        {
            float t = Smooth(onAge / OutSeconds);
            scale *= 0.5f + 0.5f * t;
            flash = 1f - t;
        }
        if (changeAge < ChangeSeconds)
        {
            float tri = 1f - MathF.Abs(changeAge / ChangeSeconds * 2f - 1f);
            scale *= 1f - ChangeSquash * tri;
            flash = MathF.Max(flash, ChangeFlash * tri);
        }

        var (turn, origin) = Stand(input);
        float run = Math.Clamp((speed - CrouchFrom) / (DashOver - CrouchFrom), 0f, 1f);
        var stance = input.Stance switch
        {
            OpenStance.Climbing => RideStance.Climb,
            OpenStance.Swimming => RideStance.Swim,
            OpenStance.Gliding => RideStance.Glide,
            _ => speed > DashOver ? RideStance.Dash : RideStance.Seated
        };
        // The rider's hop on: from beside the mount to the seat
        float seated = onAge < HopFrom ? 0f : onAge < OnSeconds ? Smooth((onAge - HopFrom) / (OnSeconds - HopFrom)) : 1f;
        var right = Vector3.TransformNormal(Vector3.UnitX, turn);
        var ground = input.Feet - new Vector3(right.X, 0f, right.Z) * Beside;
        var frame = new MountFrame(shown, turn, origin, scale, pose, flash, input.Stance == OpenStance.Swimming, seated, ground, stance, run);
        last = frame;

        onAge += dt;
        changeAge += dt;
        return frame;
    }

    /// <summary>The form shown: the one changed from until the change's middle, the one wanted after.</summary>
    private string? ShownForm() => changeAge < ChangeSeconds / 2f && from != null ? from : target;

    /// <summary>How the mount stands: turned by the way the body faces, nose down gliding, belly to the face climbing; at the feet.</summary>
    private static (Matrix4x4 Turn, Vector3 Origin) Stand(in MountInput input)
    {
        switch (input.Stance)
        {
            case OpenStance.Climbing when input.Cling.X != 0f || input.Cling.Z != 0f:
            {
                // Its back out of the face, its nose up it
                var outward = Vector3.Normalize(new Vector3(input.Cling.X, 0f, input.Cling.Z));
                var forward = Vector3.UnitY * MathF.Sin(ClimbTilt) - outward * MathF.Cos(ClimbTilt);
                var up = outward * MathF.Sin(ClimbTilt) + Vector3.UnitY * MathF.Cos(ClimbTilt);
                var right = Vector3.Cross(up, forward);
                var turn = new Matrix4x4(right.X, right.Y, right.Z, 0, up.X, up.Y, up.Z, 0, forward.X, forward.Y, forward.Z, 0, 0, 0, 0, 1);
                return (turn, input.Feet - outward * (float)(OpenBody.Radius - ClimbIn));
            }
            case OpenStance.Gliding:
                return (Matrix4x4.CreateRotationX(GlidePitch) * Matrix4x4.CreateRotationY(input.Yaw), input.Feet);
            case OpenStance.Swimming:
                // Afloat: the field drops it so its back stands over the water (MountLooks.Place)
                return (Matrix4x4.CreateRotationY(input.Yaw), input.Feet + new Vector3(0f, (float)OpenBody.SwimDepth, 0f));
            default:
                return (Matrix4x4.CreateRotationY(input.Yaw), input.Feet);
        }
    }

    /// <summary>A dismount: the rider hops down beside, the mount flashes and shrinks back into its ball, then nothing.</summary>
    private MountFrame? Dismounting(float dt)
    {
        if (last is not { } was || offAge >= OffSeconds) return null;
        float seated = offAge < HopDown ? 1f - Smooth(offAge / HopDown) : 0f;
        float scale = was.Scale, flash = 0f;
        if (offAge > BackFrom)
        {
            float t = Smooth((offAge - BackFrom) / (OffSeconds - BackFrom));
            scale *= 1f - 0.5f * t;
            flash = t;
        }
        // The rider lands where the body stands (the capsule never moved off its feet)
        var frame = was with { Scale = scale, Flash = flash, Seated = seated, Ground = was.Afloat ? was.Origin - new Vector3(0f, (float)OpenBody.SwimDepth, 0f) : was.Origin };
        offAge += dt;
        return frame;
    }

    private static float Smooth(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}

/// <summary>
/// The seats of the eight ride forms and the rider placed on them (plan 30 · FM13; style guide, "Open world", "The
/// mount"): a form's seat line along its length, its seat found on its own mesh (<see cref="SeatOf"/>), carried by the
/// body's bone through its clip, and the rider's root under it by their saddle. GPU-free (the models are meshed on the CPU).
/// </summary>
internal static class MountLooks
{
    /// <summary>Where each form is sat, along its length in model units (+z ahead): just behind the middle of a riding form's back, behind the shoulders of an upright one, under Miraidon's sail.</summary>
    public static readonly IReadOnlyDictionary<string, float> SeatLines = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
    {
        ["Koraidon-Limited-Build"] = -0.13f,
        ["Koraidon-Sprinting-Build"] = -0.03f,
        ["Koraidon-Swimming-Build"] = -0.03f,
        ["Koraidon-Gliding-Build"] = -0.03f,
        ["Miraidon-Low-Power-Mode"] = -0.12f,
        ["Miraidon-Drive-Mode"] = -0.03f,
        ["Miraidon-Aquatic-Mode"] = -0.03f,
        ["Miraidon-Glide-Mode"] = -0.03f,
    };

    /// <summary>A form whose back has something over it that is the body's own (Miraidon's sail on its mast) is sat under it: nothing over this height (model units) is the seat.</summary>
    public static readonly IReadOnlyDictionary<string, float> SeatCeilings = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
    {
        ["Miraidon-Glide-Mode"] = 0.56f,
    };

    /// <summary>How far the seat's column reaches either side of the middle line and of the seat line, in model units.</summary>
    public const float SeatColumn = 0.035f, SeatAlong = 0.03f;

    /// <summary>How far under the seat a swimming form's water line is, in model units: the rider's knees clear of the water.</summary>
    public const float FloatUnder = 0.13f;

    private static readonly ConditionalWeakTable<PokeModel, StrongBox<MountSeat>> seats = new();

    /// <summary>A form's seat, found once on its model's own mesh: the top of the body's surface on the seat line, and its half width there.</summary>
    public static MountSeat SeatOf(PokeModel model)
    {
        if (seats.TryGetValue(model, out var box)) return box.Value;
        var seat = Measure(model, SeatLines.TryGetValue(model.Species, out float line) ? line : 0f, SeatCeilings.TryGetValue(model.Species, out float ceiling) ? ceiling : float.PositiveInfinity);
        seats.AddOrUpdate(model, new StrongBox<MountSeat>(seat));
        return seat;
    }

    private static MountSeat Measure(PokeModel model, float line, float ceiling)
    {
        int body = Math.Max(0, model.Find(PokeRole.Body));
        var mesh = model.Mesh;
        float top = float.NegativeInfinity;
        // On the body's own surface first (a wing, a tail or a plume over the back is no seat), anything at all after
        foreach (float least in new[] { 0.5f, 0f })
        {
            for (int v = 0; v < mesh.VertexCount; v++)
            {
                var p = mesh.Positions[v];
                if (MathF.Abs(p.X) > SeatColumn || MathF.Abs(p.Z - line) > SeatAlong || p.Y > ceiling) continue;
                if (least > 0f && mesh.WeightOf(v, body) < least) continue;
                top = MathF.Max(top, p.Y);
            }
            if (!float.IsNegativeInfinity(top)) break;
        }
        if (float.IsNegativeInfinity(top)) top = model.Height * 0.5f;
        float half = 0f;
        for (int v = 0; v < mesh.VertexCount; v++)
        {
            var p = mesh.Positions[v];
            if (MathF.Abs(p.Z - line) > SeatAlong * 2f || p.Y > top || p.Y < top - 0.14f || mesh.WeightOf(v, body) < 0.5f) continue;
            half = MathF.Max(half, MathF.Abs(p.X));
        }
        return new MountSeat(new Vector3(0f, top, line), half, body);
    }

    /// <summary>The mount's root as drawn: a swimming form dropped so its water line is the water's surface.</summary>
    public static Matrix4x4 Place(in MountFrame frame, in MountSeat seat)
    {
        if (!frame.Afloat) return frame.Root;
        var placed = frame with { Origin = frame.Origin - frame.Up * ((seat.Rest.Y - FloatUnder) * frame.Scale) };
        return placed.Root;
    }

    /// <summary>
    /// The rider's root on the mount: the seat carried by the body's bone through the pose (the model is posed here,
    /// on the calling thread), the root <paramref name="saddle"/> metres under it along the mount's way up, turned as the
    /// mount is; between the ground and the seat as the frame's hop has it, over an arc.
    /// </summary>
    public static Matrix4x4 RiderRoot(PokeModel model, in MountFrame frame, float saddle)
    {
        var seat = SeatOf(model);
        model.Animate(frame.Pose);
        var posed = seat.Bone < model.Skin.Length ? Vector3.Transform(seat.Rest, model.Skin[seat.Bone]) : seat.Rest;
        var atSeat = Vector3.Transform(posed, Place(frame, seat)) - frame.Up * saddle;
        var turn = frame.Turn;
        // Climbing, the rider leans forward about the saddle onto the mount's back (the clip turns the thighs back by as much)
        if (frame.Stance == RideStance.Climb && frame.Seated >= 1f)
            return Matrix4x4.CreateTranslation(0f, -saddle, 0f) * Matrix4x4.CreateRotationX(GrownAnimation.ClimbLean) * turn
                * Matrix4x4.CreateTranslation(atSeat + frame.Up * saddle);
        if (frame.Seated >= 1f) return turn * Matrix4x4.CreateTranslation(atSeat);
        // A hop: the yaw kept, the place over an arc from the ground to the seat
        float t = frame.Seated;
        var at = Vector3.Lerp(frame.Ground, atSeat, t) + new Vector3(0f, MountRig.HopRise * MathF.Sin(t * MathF.PI), 0f);
        return turn * Matrix4x4.CreateTranslation(at);
    }

    /// <summary>The mount's half width at its seat in metres (its seat's in the model times the frame's size).</summary>
    public static float HalfWidth(PokeModel model, in MountFrame frame) => SeatOf(model).HalfWidth * frame.Scale;
}
