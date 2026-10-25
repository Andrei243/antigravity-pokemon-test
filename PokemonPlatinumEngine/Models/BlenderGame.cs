using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models;

/// <summary>How a press of the Berry Blender's arm came out.</summary>
public enum BlenderGrade { Miss, Good, Best }

/// <summary>
/// The Berry Blender's game (plan 21 · H11): the arm turns round the drum, and a seat's player presses as it passes
/// their pocket; a press in the middle of the pocket (best) and one near it (good) speed the arm up, one outside it
/// (a miss) slows it, and the drum's drag takes a little speed off every frame. The fastest the arm turned is the
/// Pokéblock's speed (<see cref="MaxRpm"/>, in hundredths of a turn a minute), which <see cref="Pokeblocks.Blend"/>
/// multiplies the flavours by. Emerald's blender asks for the same (a press as the arm passes, the game over at a time
/// limit); its numbers are ours, since the original's are the console's timers and touch panel. Thirty frames to the
/// second, like <see cref="PoffinPot"/>; every other seat is an opponent that presses by a draw on the generator it is
/// given. No drawing, no input.
/// </summary>
public sealed class BlenderGame
{
    public const int FramesPerSecond = 30;

    /// <summary>The game lasts this many frames (twenty-four seconds).</summary>
    public const int Duration = 24 * FramesPerSecond;

    /// <summary>The arm's speed at the start, the most it can have and the least it keeps (hundredths of a turn a minute).</summary>
    public const int StartRpm = 3000, MaxSpeed = 30000, MinRpm = 1000;

    /// <summary>The gains and losses of a press, and the drum's drag (the speed divided by this each frame).</summary>
    public const int BestGain = 2200, GoodGain = 1000, MissLoss = 1200, DragDivisor = 520;

    /// <summary>How far from the middle of its pocket the arm may be (degrees) for a best press, a good press, and for the pocket to be open at all.</summary>
    public const float BestArc = 12f, GoodArc = 26f, PocketArc = 34f;

    /// <summary>The seats: 1 to 4, the first the player's, spread evenly round the drum.</summary>
    public int Seats { get; }

    public int Frame { get; private set; }
    public bool IsOver => Frame >= Duration;

    /// <summary>The arm's angle in degrees (0 at the player's pocket, turning clockwise) and its speed.</summary>
    public float Angle { get; private set; }
    public int Rpm { get; private set; } = StartRpm;

    /// <summary>The fastest the arm has turned (the Pokéblock's speed).</summary>
    public int MaxRpm { get; private set; } = StartRpm;

    /// <summary>The player's presses by grade.</summary>
    public int Bests { get; private set; }
    public int Goods { get; private set; }
    public int Misses { get; private set; }

    /// <summary>The grade of the last press, and for how many frames ago.</summary>
    public BlenderGrade? LastGrade { get; private set; }
    public int LastGradeAge { get; private set; }

    private readonly Random rng;
    private readonly bool[] used;

    /// <param name="seats">How many take part, the player and up to three others.</param>
    public BlenderGame(int seats, Random chance)
    {
        Seats = Math.Clamp(seats, 1, 4);
        rng = chance;
        used = new bool[Seats];
    }

    /// <summary>The angle of a seat's pocket, 0 for the player's.</summary>
    public float PocketAngle(int seat) => 360f * seat / Seats;

    private static float Distance(float a, float b)
    {
        float d = Math.Abs(a - b) % 360f;
        return d > 180f ? 360f - d : d;
    }

    /// <summary>The seat whose pocket the arm is in now, or -1.</summary>
    public int PocketUnderArm()
    {
        for (int s = 0; s < Seats; s++)
            if (Distance(Angle, PocketAngle(s)) <= PocketArc) return s;
        return -1;
    }

    /// <summary>The player presses (<c>A</c>): the grade, and the arm's speed changed by it.</summary>
    public BlenderGrade Press()
    {
        if (IsOver) return BlenderGrade.Miss;
        float d = Distance(Angle, PocketAngle(0));
        BlenderGrade grade;
        if (!used[0] && d <= BestArc) grade = BlenderGrade.Best;
        else if (!used[0] && d <= GoodArc) grade = BlenderGrade.Good;
        else grade = BlenderGrade.Miss;
        if (d <= PocketArc) used[0] = true;
        Apply(grade);
        switch (grade)
        {
            case BlenderGrade.Best: Bests++; break;
            case BlenderGrade.Good: Goods++; break;
            default: Misses++; break;
        }
        LastGrade = grade;
        LastGradeAge = 0;
        return grade;
    }

    private void Apply(BlenderGrade grade)
    {
        Rpm = grade switch
        {
            BlenderGrade.Best => Math.Min(MaxSpeed, Rpm + BestGain),
            BlenderGrade.Good => Math.Min(MaxSpeed, Rpm + GoodGain),
            _ => Math.Max(MinRpm, Rpm - MissLoss)
        };
        MaxRpm = Math.Max(MaxRpm, Rpm);
    }

    /// <summary>One frame: the arm turns, the drum drags, the other seats press as it passes.</summary>
    public void Advance()
    {
        if (IsOver) return;
        float before = Angle;
        Angle = (Angle + 3f + Rpm / 2000f) % 360f;
        Rpm = Math.Max(MinRpm, Rpm - Math.Max(1, Rpm / DragDivisor));
        LastGradeAge++;
        for (int s = 0; s < Seats; s++)
        {
            // A pocket is open again once the arm has left it
            if (Distance(Angle, PocketAngle(s)) > PocketArc) used[s] = false;
            else if (s > 0 && !used[s] && Crossed(before, Angle, PocketAngle(s)))
            {
                // The others press as the arm reaches the middle of their pocket
                used[s] = true;
                int r = rng.Next(100);
                if (r < 42) Apply(BlenderGrade.Best);
                else if (r < 78) Apply(BlenderGrade.Good);
                else Apply(BlenderGrade.Miss);
            }
        }
        Frame++;
    }

    private static bool Crossed(float from, float to, float target)
    {
        float span = to >= from ? to - from : to + 360f - from;
        float into = target >= from ? target - from : target + 360f - from;
        return into <= span;
    }

    /// <summary>The fastest speed as it is shown: hundredths cut to turns with two decimals ("236.31").</summary>
    public static string RpmText(int rpm) => $"{rpm / 100}.{rpm % 100:00}";

    /// <summary>
    /// Plays a whole game as a player who presses in the best of every pocket, for tests and the screenshot harness
    /// (<paramref name="pressing"/> false is one who never presses).
    /// </summary>
    public static BlenderGame Played(int seats, Random chance, bool pressing)
    {
        var game = new BlenderGame(seats, chance);
        while (!game.IsOver)
        {
            if (pressing && Distance(game.Angle, game.PocketAngle(0)) <= BestArc && !game.used[0]) game.Press();
            game.Advance();
        }
        return game;
    }
}
