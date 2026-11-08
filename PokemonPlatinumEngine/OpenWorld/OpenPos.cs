using System;
using System.Globalization;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// A place in the open world (plan 32 · OW1): whole millimetres in three <c>int</c>s, ±2,147 km, as Platinum kept its
/// places in fixed point. <see cref="X"/> runs east, <see cref="Z"/> south (as the grid's rows do) and <see cref="Y"/>
/// up, the feet's height. It is what is <b>kept and named</b>: a marker, a door, a volume's corner, the save's place
/// and what a script or the harness writes. The body's own state between ticks is the capsule's (plan 32 · OW3), which
/// may hold finer steps than this (decision 11 asks whether micrometres) and tells its place as one of these. GPU-free.
/// </summary>
public readonly record struct OpenPos(int X, int Y, int Z)
{
    /// <summary>A millimetre's share of a metre.</summary>
    public const int PerMetre = 1000;

    /// <summary>A place given in metres, each rounded to the nearest millimetre, half away from nought.</summary>
    public static OpenPos FromMetres(double x, double y, double z) => new(Millimetres(x), Millimetres(y), Millimetres(z));

    /// <summary>A length in metres as whole millimetres, rounded to the nearest, half away from nought.</summary>
    public static int Millimetres(double metres) =>
        checked((int)Math.Round(metres * PerMetre, MidpointRounding.AwayFromZero));

    public double MetresX => X / (double)PerMetre;
    public double MetresY => Y / (double)PerMetre;
    public double MetresZ => Z / (double)PerMetre;

    /// <summary>This place moved by so many millimetres on each axis.</summary>
    public OpenPos Plus(int dx, int dy, int dz) => new(checked(X + dx), checked(Y + dy), checked(Z + dz));

    /// <summary>
    /// The square of the distance on the ground (x and z), in square millimetres: whole, so two machines agree. Two
    /// places more than about 2,000 km apart overflow it, and are refused (an <see cref="OverflowException"/>).
    /// </summary>
    public long GroundDistanceSquared(OpenPos other)
    {
        long dx = (long)other.X - X, dz = (long)other.Z - Z;
        return checked(dx * dx + dz * dz);
    }

    /// <summary>The square of the distance in all three axes, in square millimetres (as <see cref="GroundDistanceSquared"/>).</summary>
    public long DistanceSquared(OpenPos other)
    {
        long dy = (long)other.Y - Y;
        return checked(GroundDistanceSquared(other) + dy * dy);
    }

    /// <summary>As the baker and the harness print it: <c>(1500 mm, 0 mm, -250 mm)</c>.</summary>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"({X} mm, {Y} mm, {Z} mm)");
}

/// <summary>
/// A way of facing in the open world (plan 32 · OW1): 65,536ths of a turn, as Platinum kept its angles. Nought faces
/// south (+z), a quarter east (+x), a half north and three quarters west, so a facing of the grid is a quarter turn
/// each (<see cref="Of(Direction)"/>). Kept as an <c>int</c> from 0 to 65,535 wherever it is stored. GPU-free.
/// </summary>
public static class OpenYaw
{
    /// <summary>A whole turn.</summary>
    public const int Turn = 65536;

    public const int South = 0, East = Turn / 4, North = Turn / 2, West = 3 * Turn / 4;

    /// <summary>Any count of 65,536ths brought into 0 to 65,535 (a turn and a half is a half).</summary>
    public static int Normalize(int yaw) => yaw & (Turn - 1);

    /// <summary>How far <paramref name="to"/> is turned from <paramref name="from"/>, the shorter way: −32,767 to 32,768 (plan 32 · OW6).</summary>
    public static int Between(int from, int to)
    {
        int d = Normalize(to - from);
        return d > Turn / 2 ? d - Turn : d;
    }

    /// <summary>The grid's facing as a yaw.</summary>
    public static int Of(Direction facing) => facing switch
    {
        Direction.Right => East,
        Direction.Up => North,
        Direction.Left => West,
        _ => South
    };

    /// <summary>The grid's facing nearest a yaw (an eighth either side of each; a yaw exactly between two takes the one of the higher yaw, south-east east).</summary>
    public static Direction ToDirection(int yaw)
    {
        int quarter = ((Normalize(yaw) + Turn / 8) & (Turn - 1)) / (Turn / 4);
        return quarter switch { 1 => Direction.Right, 2 => Direction.Up, 3 => Direction.Left, _ => Direction.Down };
    }

    /// <summary>The yaw in radians, from nought to just under 2π.</summary>
    public static double Radians(int yaw) => Normalize(yaw) * (Math.PI * 2 / Turn);

    /// <summary>The yaw nearest an angle in radians (nought south, a quarter turn east).</summary>
    public static int FromRadians(double radians) =>
        Normalize((int)Math.Round(radians * (Turn / (Math.PI * 2)), MidpointRounding.AwayFromZero));

    /// <summary>The yaw of a way along the ground, east <paramref name="x"/> and south <paramref name="z"/>; south where there is no way.</summary>
    public static int Toward(double x, double z) => x == 0 && z == 0 ? South : FromRadians(Math.Atan2(x, z));
}
