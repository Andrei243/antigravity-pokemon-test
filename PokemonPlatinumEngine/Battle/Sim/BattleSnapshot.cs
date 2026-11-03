using System;
using System.Text.Json;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>
/// A battle written out where it stands, to be carried on from (plan 28 · I5): the battle as it began, with every answer
/// given so far (a <see cref="BattleRecordFile"/>, the Vs. Recorder's own file), and what the battle had come to there,
/// as a check: the turn, the dice's state and how many numbers they had drawn, how long the log was, and the question it
/// was waiting on. <see cref="BattleCore.Resume"/> rebuilds the battle from the opening and plays the answers through,
/// then holds it to the check, so a snapshot that would resume as another battle (other data, another build) is refused
/// rather than played on.
/// <para>
/// It is not the core's whole state field by field: a battle's course is one iterator (<see cref="BattleCore"/>'s
/// <c>Course</c>), which no serializer can write, so the state is rebuilt by playing the answers again, which the
/// golden battles already hold to be exact. What that leaves (a server restarted in the middle of a long battle replays
/// it from its start, in milliseconds) is in the plan's notes. GPU-free; its text is JSON.
/// </para>
/// </summary>
public sealed class BattleSnapshot
{
    /// <summary>The shape of the snapshot this game writes.</summary>
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;

    /// <summary>The battle as it began and every answer up to the snapshot.</summary>
    public BattleRecordFile Battle { get; set; } = new();

    /// <summary>The check: turns played, the dice's state and draws, the log's length, the question waited on (its wire name).</summary>
    public int Turn { get; set; }
    public uint DiceState { get; set; }
    public long DiceDrawn { get; set; }
    public int LogLength { get; set; }
    public string? Waiting { get; set; }

    /// <summary>
    /// A battle where it stands. <paramref name="opening"/> is its <see cref="BattleRecordFile.Opening"/>, taken before
    /// the core was made; the battle must roll with a <see cref="BattleRandom"/>, or nothing comes out the same again.
    /// </summary>
    public static BattleSnapshot Take(BattleRecordFile opening, BattleCore core)
    {
        var dice = core.Random as BattleRandom ?? throw new InvalidOperationException("A battle whose chance is no BattleRandom can't be resumed");
        return new BattleSnapshot
        {
            Battle = opening.Closing(core, DateTime.MinValue) ?? throw new InvalidOperationException("A battle with no record can't be resumed"),
            Turn = core.Turn,
            DiceState = dice.State,
            DiceDrawn = dice.Drawn,
            LogLength = core.Log.Count,
            Waiting = core.Request?.GetType().Name
        };
    }

    public string Serialize() => GameDataFiles.Serialize(this);

    /// <summary>A snapshot's text; null for one that isn't one, or is of a later shape.</summary>
    public static BattleSnapshot? Read(string text)
    {
        try
        {
            var snapshot = JsonSerializer.Deserialize<BattleSnapshot>(text, GameDataFiles.Json);
            return snapshot is { Version: >= 1 and <= CurrentVersion } ? snapshot : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

public sealed partial class BattleCore
{
    /// <summary>
    /// Carries a battle on from a snapshot (plan 28 · I5): the battle rebuilt from its opening, the answers played through,
    /// and held to the snapshot's check (turn, dice, log, question), so it stands exactly where the snapshot was taken and
    /// goes on as the battle would have. A snapshot it can't match is refused.
    /// </summary>
    public static BattleCore Resume(BattleSnapshot snapshot)
    {
        var core = new BattleCore(snapshot.Battle.ToSetup());
        core.Replay(snapshot.Battle.Record);
        var dice = (BattleRandom)core.rng;
        if (core.Turn != snapshot.Turn || dice.State != snapshot.DiceState || dice.Drawn != snapshot.DiceDrawn
            || core.Log.Count != snapshot.LogLength || core.Request?.GetType().Name != snapshot.Waiting)
            throw new InvalidOperationException("The snapshot resumes as another battle: other data, or another build of the rules");
        // What was shown before the snapshot was sent already
        core.taken = core.log.Count;
        return core;
    }
}
