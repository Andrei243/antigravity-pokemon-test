using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The Fortree Gym's rotating gates (plan 21 · H6; Emerald's <c>src/rotating_gate.c</c>, its Fortree puzzle): eight
/// turnstiles of wooden rails, each turning on a pivot at the corner of four tiles, its arms lying along the lines
/// between tiles. Someone who walks into an arm pushes it round a quarter turn (clockwise or anticlockwise, by which
/// side of the pivot the arm was met on), unless an arm would sweep into a wall on the way, in which case the gate
/// holds and the step is refused. The gates are laid out afresh each time the player comes in, as the original's
/// <c>RotatingGate_ResetAllGateOrientations</c> does; nothing of them is saved. GPU-free: the field's rules ask
/// <see cref="Bars"/> before a step and <see cref="Crossed"/> as one is taken, and the room's pieces are drawn from
/// <see cref="Gates"/>.
/// </summary>
public sealed class FortreeGates : GymPuzzle
{
    public const string PuzzleName = "FortreeGates";

    public override string Name => PuzzleName;

    /// <summary>The shapes of gate, by their arms (north, east, south, west at no turn, each short and long): the original's order.</summary>
    public enum Shape { L1, L2, L3, L4, T1, T2, T3, T4 }

    /// <summary>One gate: the tile whose north-west corner is its pivot, its shape, and its first quarter turns clockwise.</summary>
    public readonly record struct Gate(int X, int Y, Shape Shape, int Start);

    /// <summary>The Fortree Gym's eight gates (<c>sRotatingGate_FortreePuzzleConfig</c>), in the original's order.</summary>
    public static readonly Gate[] Layout =
    {
        new(6, 7, Shape.T2, 1), new(9, 15, Shape.T2, 2), new(3, 19, Shape.T2, 1), new(2, 6, Shape.T1, 1),
        new(9, 12, Shape.T1, 0), new(6, 23, Shape.T1, 0), new(12, 22, Shape.T1, 0), new(6, 3, Shape.L4, 2),
    };

    /// <summary>
    /// Each shape's arms (<c>sRotatingGate_ArmLayout</c>): for north, east, south and west at no turn, whether it has
    /// the arm's first tile and its second.
    /// </summary>
    private static readonly bool[][] Arms =
    {
        new[] { true, false, true, false, false, false, false, false },
        new[] { true, true, true, false, false, false, false, false },
        new[] { true, false, true, true, false, false, false, false },
        new[] { true, true, true, true, false, false, false, false },
        new[] { true, false, true, false, true, false, false, false },
        new[] { true, true, true, false, true, false, false, false },
        new[] { true, false, true, true, true, false, false, false },
        new[] { true, false, true, false, true, true, false, false },
    };

    private const int Clockwise = 1, Anticlockwise = -1, None = 0;

    /// <summary>
    /// What a step onto a tile near a gate meets (<c>sRotatingGate_RotationInfo*</c>): by the way of the step and the
    /// tile's place in the four by four tiles round the pivot (x then y, from two west and two north of it), the way
    /// the gate would turn, the arm met (0 north, 1 east, 2 south, 3 west) and whether it is the arm's far tile.
    /// </summary>
    private static (int Turn, int Arm, bool Far) Meeting(Direction dir, int cx, int cy)
    {
        (int, int, bool) none = (None, 0, false);
        switch (dir)
        {
            case Direction.Up when cy == 1:
                return cx switch { 0 => (Clockwise, 3, true), 1 => (Clockwise, 3, false), 2 => (Anticlockwise, 1, false), _ => (Anticlockwise, 1, true) };
            case Direction.Down when cy == 2:
                return cx switch { 0 => (Anticlockwise, 3, true), 1 => (Anticlockwise, 3, false), 2 => (Clockwise, 1, false), _ => (Clockwise, 1, true) };
            case Direction.Left when cx == 1:
                return cy switch { 0 => (Anticlockwise, 0, true), 1 => (Anticlockwise, 0, false), 2 => (Clockwise, 2, false), _ => (Clockwise, 2, true) };
            case Direction.Right when cx == 2:
                return cy switch { 0 => (Clockwise, 0, true), 1 => (Clockwise, 0, false), 2 => (Anticlockwise, 2, false), _ => (Anticlockwise, 2, true) };
            default:
                return none;
        }
    }

    /// <summary>
    /// The tiles an arm sweeps through as the gate turns, which must all be open (<c>sRotatingGate_ArmPositions*Rotation</c>):
    /// by the arm's way now (north, east, south, west) and its first or far tile, from the pivot's tile.
    /// </summary>
    private static readonly (int X, int Y)[] SweptClockwise = { (0, -1), (1, -2), (0, 0), (1, 0), (-1, 0), (-1, 1), (-1, -1), (-2, -1) };
    private static readonly (int X, int Y)[] SweptAnticlockwise = { (-1, -1), (-1, -2), (0, -1), (1, -1), (0, 0), (0, 1), (-1, 0), (-2, 0) };

    private readonly Gate[] gates;
    private readonly int[] turns;

    public FortreeGates() : this(Layout) { }

    /// <summary>A room of gates of its own (a test's).</summary>
    public FortreeGates(Gate[] layout)
    {
        gates = layout;
        turns = new int[layout.Length];
        Reset();
    }

    /// <summary>The gates, each with its quarter turns clockwise now (0 to 3).</summary>
    public IEnumerable<(Gate Gate, int Turn)> Gates
    {
        get
        {
            for (int i = 0; i < gates.Length; i++) yield return (gates[i], turns[i]);
        }
    }

    /// <summary>A gate's quarter turns clockwise now.</summary>
    public int TurnOf(int gate) => turns[gate];

    /// <summary>Whether a gate's arm lies the way <paramref name="arm"/> (0 north to 3 west) now, its first tile or its far one.</summary>
    public bool HasArm(int gate, int arm, bool far)
    {
        int own = ((arm - turns[gate]) % 4 + 4) % 4;
        return Arms[(int)gates[gate].Shape][own * 2 + (far ? 1 : 0)];
    }

    /// <summary>The ways (0 north to 3 west) a gate's arms lie now, each with whether it is long.</summary>
    public IEnumerable<(int Way, bool Long)> ArmsOf(int gate)
    {
        for (int arm = 0; arm < 4; arm++)
            if (HasArm(gate, arm, far: false)) yield return (arm, HasArm(gate, arm, far: true));
    }

    public override void Apply(Map map, StoryState story) { }

    public override void Arrive(Map map, StoryState story, Random rng) => Reset();

    /// <summary>Every gate back as the room begins.</summary>
    public void Reset()
    {
        for (int i = 0; i < gates.Length; i++) turns[i] = gates[i].Start;
    }

    public override bool Bars(Map map, int x, int y, Direction dir) => Meet(map, x, y, dir) is { Turns: false };

    public override void Crossed(Map map, int x, int y, Direction dir)
    {
        if (Meet(map, x, y, dir) is { Turns: true } met) turns[met.Gate] = ((turns[met.Gate] + met.Way) % 4 + 4) % 4;
    }

    /// <summary>
    /// The gate a step onto (<paramref name="x"/>, <paramref name="y"/>) going <paramref name="dir"/> pushes, the way
    /// it would turn and whether it can (<c>CheckForRotatingGatePuzzleCollision</c>: the first gate whose arm is met,
    /// in the original's order); null where the step meets no arm.
    /// </summary>
    private (int Gate, int Way, bool Turns)? Meet(Map map, int x, int y, Direction dir)
    {
        for (int i = 0; i < gates.Length; i++)
        {
            var g = gates[i];
            if (x < g.X - 2 || x > g.X + 1 || y < g.Y - 2 || y > g.Y + 1) continue;
            var (turn, arm, far) = Meeting(dir, x - g.X + 2, y - g.Y + 2);
            if (turn == None || !HasArm(i, arm, far)) continue;
            return (i, turn, CanTurn(map, i, turn));
        }
        return null;
    }

    /// <summary>Whether no arm of a gate would sweep into a wall turning that way (<c>RotatingGate_CanRotate</c>).</summary>
    private bool CanTurn(Map map, int gate, int way)
    {
        var swept = way == Clockwise ? SweptClockwise : SweptAnticlockwise;
        var g = gates[gate];
        var shape = Arms[(int)g.Shape];
        for (int own = 0; own < 4; own++)
            for (int j = 0; j < 2; j++)
            {
                if (!shape[own * 2 + j]) continue;
                var (dx, dy) = swept[2 * ((turns[gate] + own) % 4) + j];
                int tx = g.X + dx, ty = g.Y + dy;
                if (!map.InBounds(tx, ty) || map.IsSolid(tx, ty)) return false;
            }
        return true;
    }
}
