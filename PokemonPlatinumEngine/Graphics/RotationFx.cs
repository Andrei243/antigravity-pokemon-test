using System;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// A rotation battle's turntables (plan 14 · B7; style guide, "Stage", "A rotation's turntable"): on each platform a
/// ring a little inside its rim and three seats on it a third of a round apart, the front one (toward the other side)
/// lit, turning with the table as <see cref="BattleAnimator.TurntableTurn"/> says, and while a turn plays an arrow on
/// the rim on each side of the front seat pointing the way it turns. GPU-free: a pure function of the animator's turn,
/// its age and the places, all lying flat in the scene (depth-tested).
/// </summary>
internal static class RotationFx
{
    /// <summary>The ring's radius and a seat's, in the platform's radius.</summary>
    public const float RingRadius = 0.82f, SeatSize = 0.13f;

    /// <summary>The most quads a side's turntable draws: the ring, three seats with a glint each, two arrows.</summary>
    public const int MaxQuadsPerSide = 1 + 3 * 2 + 2;

    private static readonly Color RingColor = new(206, 218, 236, 150);
    private static readonly Color SeatColor = new(150, 164, 190, 200);
    private static readonly Color FrontColor = new(255, 214, 96, 235);
    private static readonly Color ArrowColor = new(255, 236, 170, 230);

    public static void Emit(BattleAnimator anim, FxPlaces places, FxList fx)
    {
        if (!anim.Turntables) return;
        for (int s = 0; s < 2; s++) Side(anim, (BattleSide)s, places, fx);
    }

    /// <summary>The angle of a seat, from the way toward the other side, after the table has turned <paramref name="turn"/> thirds.</summary>
    public static float SeatAngle(int seat, float turn) => (seat - turn) * MathF.Tau / 3f;

    private static void Side(BattleAnimator anim, BattleSide side, FxPlaces places, FxList fx)
    {
        bool player = side == BattleSide.Player;
        var centre = player ? BattleStage.PlayerSpot : BattleStage.EnemySpot;
        var other = player ? BattleStage.EnemySpot : BattleStage.PlayerSpot;
        float r = player ? BattleStage.PlayerPlatformRadius : BattleStage.EnemyPlatformRadius;
        var forward = Vector3.Normalize(new Vector3(other.X - centre.X, 0f, other.Z - centre.Z));
        var right = new Vector3(-forward.Z, 0f, forward.X);
        float top = places.Feet(side, 0).Y + r * 0.01f;
        var middle = new Vector3(centre.X, top, centre.Z);
        Vector3 At(float angle, float distance) => middle + (forward * MathF.Cos(angle) + right * MathF.Sin(angle)) * distance;

        fx.Flat(FxShape.Ring, middle, Vector3.UnitY, r * RingRadius * 1.08f, RingColor, FxBlend.Alpha);

        float turn = anim.TurntableTurn(side);
        for (int seat = 0; seat < 3; seat++)
        {
            float angle = SeatAngle(seat, turn - MathF.Round(turn));
            // The seat that stands at the front (toward the other side) is lit, and fades as it turns away
            float front = Math.Clamp(1f - MathF.Abs(Wrap(angle)) / (MathF.Tau / 3f), 0f, 1f);
            var at = At(angle, r * RingRadius);
            fx.Flat(FxShape.Hex, at, Vector3.UnitY, r * SeatSize, Mix(SeatColor, FrontColor, front), FxBlend.Alpha, angle);
            fx.Flat(FxShape.Glow, at, Vector3.UnitY, r * SeatSize * 0.7f, Fx.Fade(FrontColor, 0.5f * front), FxBlend.Add);
        }

        float age = anim.RotationAge(side);
        if (age < 0f) return;
        // While it turns: an arrow on the rim either side of the front, pointing round the way the table goes
        float k = MathF.Sin(Math.Clamp(age / BattleAnimator.RotationTime, 0f, 1f) * MathF.PI);
        int way = anim.RotationWay(side) >= 0 ? 1 : -1;
        for (int i = -1; i <= 1; i += 2)
        {
            float angle = i * MathF.Tau / 6f;
            var at = At(angle, r * RingRadius);
            float heading = angle + way * MathF.PI / 2f;
            fx.Flat(FxShape.Arrow, at, Vector3.UnitY, r * SeatSize * 1.2f, Fx.Fade(ArrowColor, k), FxBlend.Alpha, heading);
        }
    }

    private static float Wrap(float angle)
    {
        angle %= MathF.Tau;
        if (angle > MathF.PI) angle -= MathF.Tau;
        if (angle < -MathF.PI) angle += MathF.Tau;
        return angle;
    }

    private static Color Mix(Color a, Color b, float t) => new(
        (byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t), (byte)(a.A + (b.A - a.A) * t));
}
