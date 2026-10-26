using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>
/// A bout's verdict (plan 14 · B9; <see cref="Judged"/>): each side's Mind, Skill and Body, and who won, null on a draw.
/// Each count is won by the side ahead on it, worth two, and shared on a tie, worth one each (Emerald's
/// <c>ShowJudgmentSprite</c>); the side with the more points wins.
/// </summary>
public sealed record ArenaVerdict(int MindMine, int MindTheirs, int SkillMine, int SkillTheirs, int BodyMine, int BodyTheirs)
{
    /// <summary>What one count is worth to a side: two for being ahead, one for a tie, none for being behind.</summary>
    public static int Points(int own, int other) => own > other ? 2 : own == other ? 1 : 0;

    public int ScoreMine => Points(MindMine, MindTheirs) + Points(SkillMine, SkillTheirs) + Points(BodyMine, BodyTheirs);
    public int ScoreTheirs => Points(MindTheirs, MindMine) + Points(SkillTheirs, SkillMine) + Points(BodyTheirs, BodyMine);

    /// <summary>The side the judge gives the bout to; null when the scores are level, and both go down.</summary>
    public BattleSide? Winner => ScoreMine > ScoreTheirs ? BattleSide.Player : ScoreMine < ScoreTheirs ? BattleSide.Enemy : null;
}

/// <summary>
/// The Battle Arena's judge (plan 14 · B9; <c>docs/mechanics/rulings.md</c>, "The Battle Arena"): Emerald's
/// <c>battle_arena.c</c>. A bout is the two Pokémon facing each other from when they met; the core tells the judge of
/// every move either uses (<see cref="Moved"/>, <see cref="Skilled"/>) and, once the bout has run
/// <see cref="Turns"/> turns with both still standing, asks for the verdict (<see cref="Verdict"/>). Mind is how boldly
/// each fought (<see cref="MindOf"/>), Skill how well its moves came off (<see cref="SkillOf"/>), Body the share of its
/// HP it kept from the bout's start. GPU-free; it keeps plain numbers and the two Pokémon the bout is between.
/// </summary>
public sealed class ArenaJudge
{
    /// <summary>The turns a bout runs before it is judged (Emerald's three).</summary>
    public const int Turns = 3;

    /// <summary>The two the bout is between, and the HP each had as it began (<c>arenaStartHp</c>).</summary>
    public Pokemon? Mine { get; private set; }
    public Pokemon? Theirs { get; private set; }
    public int StartMine { get; private set; }
    public int StartTheirs { get; private set; }

    /// <summary>The turns of this bout played to their end.</summary>
    public int TurnsPlayed { get; set; }

    private readonly int[] mind = new int[2], skill = new int[2];

    public int Mind(BattleSide side) => mind[(int)side];
    public int Skill(BattleSide side) => skill[(int)side];

    /// <summary>A bout begins between these two (<c>BattleArena_InitPoints</c>): the counts start again from nothing.</summary>
    public void Begin(Pokemon mine, Pokemon theirs)
    {
        (Mine, Theirs) = (mine, theirs);
        (StartMine, StartTheirs) = (Math.Max(1, mine.CurrentHP), Math.Max(1, theirs.CurrentHP));
        TurnsPlayed = 0;
        Array.Clear(mind);
        Array.Clear(skill);
    }

    /// <summary>The bout is over (judged, or one of the two went down): the next two to face each other begin another.</summary>
    public void End() => (Mine, Theirs) = (null, null);

    /// <summary>Whether a bout is under way between these two.</summary>
    public bool Between(Pokemon? mine, Pokemon? theirs) => Mine != null && Mine == mine && Theirs == theirs;

    /// <summary>A side used a move (<c>BattleArena_AddMindPoints</c>).</summary>
    public void Moved(BattleSide side, MoveData move) => mind[(int)side] += MindOf(move);

    /// <summary>What a move it used came to (<c>BattleArena_AddSkillPoints</c>; <see cref="SkillOf"/>).</summary>
    public void Skilled(BattleSide side, int points) => skill[(int)side] += points;

    /// <summary>
    /// What a move is worth to Mind (<c>sMindRatings</c>): a damaging move one, but Counter, Mirror Coat and Bide nothing
    /// and Fake Out one less; a status move nothing, but Protect, Detect and Endure one less. The later games' moves are
    /// ours to place: Metal Burst and Comeuppance go with Counter, and the later protections with Protect.
    /// </summary>
    public static int MindOf(MoveData move)
    {
        if (move.Category == MoveCategory.Status) return Shields.Contains(move.Name) ? -1 : 0;
        if (Answers.Contains(move.Name)) return 0;
        return move.Name == "Fake Out" ? -1 : 1;
    }

    private static readonly HashSet<string> Shields = new(StringComparer.OrdinalIgnoreCase)
        { "Protect", "Detect", "Endure", "King's Shield", "Spiky Shield", "Baneful Bunker", "Obstruct", "Silk Trap", "Burning Bulwark", "Max Guard" };

    private static readonly HashSet<string> Answers = new(StringComparer.OrdinalIgnoreCase)
        { "Counter", "Mirror Coat", "Bide", "Metal Burst", "Comeuppance" };

    /// <summary>
    /// What a move that was used comes to for Skill (<c>BattleArena_AddSkillPoints</c>): one that failed, missed or did
    /// nothing two less, unless a protection stopped it, which counts nothing; one super effective two, one both super and
    /// not very effective (two targets) one, one not very effective one less, and anything else that came off one.
    /// </summary>
    public static int SkillOf(bool failed, bool stoppedByProtection, bool superEffective, bool notVeryEffective)
    {
        if (stoppedByProtection) return 0;
        if (failed) return -2;
        if (superEffective && notVeryEffective) return 1;
        if (superEffective) return 2;
        if (notVeryEffective) return -1;
        return 1;
    }

    /// <summary>The share of its bout's starting HP a Pokémon has kept, in hundredths (Emerald's Body).</summary>
    public static int Body(int hp, int start) => hp * 100 / Math.Max(1, start);

    /// <summary>The verdict on the bout as it stands, the two left with these HP.</summary>
    public ArenaVerdict Verdict(int hpMine, int hpTheirs) =>
        new(mind[0], mind[1], skill[0], skill[1], Body(hpMine, StartMine), Body(hpTheirs, StartTheirs));
}
