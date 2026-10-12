using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Battle.Sim;

// The moments a trainer of the other side speaks at (plan 11 · C12): their last Pokémon comes out, the first of
// theirs falls, their last one is in the red. Black and White's trainers speak there; Platinum's never do, so the
// rules only say when (TrainerReacts) and the face decides whether anything is said. Nothing here rolls a die, and
// nothing is written once the battle has a result, so a recorded battle replays the same.
public sealed partial class BattleCore
{
    /// <summary>
    /// A Pokémon is in the red at this share of its HP or under: a fifth, the HP bar's red
    /// (<see cref="BattleAnimator.LowHpRatio"/>, which a test holds equal to it). Whole numbers, as the rules count.
    /// </summary>
    public const int RedShare = 5;

    /// <summary>Whether a Pokémon with this HP is in the red: standing, at a fifth of its HP or under.</summary>
    public static bool InTheRed(int hp, int maxHp) => hp > 0 && hp * RedShare <= maxHp;

    /// <summary>The moments each trainer has reacted to already, by their place among <see cref="Trainers"/>.</summary>
    private readonly HashSet<(int Trainer, TrainerCue Cue)> reacted = new();

    /// <summary>
    /// The trainer of a place reacts to a moment, if they are a trainer of the other side, the battle is still on
    /// and they haven't reacted to it already; null otherwise. Marks the moment as had.
    /// </summary>
    private TrainerReacts? Reacts(Battler place, TrainerCue cue)
    {
        if (Result != BattleResult.None || place.IsPlayerSide || place.Trainer == null) return null;
        int who = -1;
        for (int i = 0; i < Trainers.Count; i++)
            if (ReferenceEquals(Trainers[i], place.Trainer)) who = i;
        if (who < 0 || !reacted.Add((who, cue))) return null;
        return new TrainerReacts(place.Place, cue);
    }

    /// <summary>
    /// A trainer's Pokémon has just come into a place in the middle of the battle (a replacement, a switch, a move
    /// that brings it in): if nobody else of theirs is left to come, it is their last, and the line that sent it out
    /// says so. The opening's send-outs never count, so a trainer of one has no such moment.
    /// </summary>
    private void SentOutTheLast(Battler place, Said line)
    {
        if (place.IsPlayerSide || place.Roster == null || place.Pokemon == null) return;
        var onField = EnemySlots.Select(b => b.Pokemon).Where(p => p != null).ToHashSet();
        if (place.Roster.Members.Any(p => !p.IsFainted && !onField.Contains(p))) return;
        if (Reacts(place, TrainerCue.LastPokemon) is { } reacts) line.With(reacts);
    }

    /// <summary>A trainer's Pokémon has fallen: the first of theirs to, while others can still fight, is a moment of its own.</summary>
    private void FellFirst(Battler down, Said line)
    {
        if (down.IsPlayerSide || down.Roster == null) return;
        if (!down.Roster.Members.Any(p => !p.IsFainted)) return;
        if (Reacts(down, TrainerCue.FirstDown) is { } reacts) line.With(reacts);
    }

    /// <summary>
    /// After each action and after the end of a turn: a trainer whose last Pokémon able to fight is standing in the
    /// red reacts, with the last line said (the hit's, or the effectiveness and the critical hit after it, so the
    /// trainer speaks once the move has been told whole). With no line of this answer still unread, on its own.
    /// </summary>
    private void WatchTheRed()
    {
        if (Result != BattleResult.None) return;
        foreach (var place in EnemySlots)
        {
            var p = place.Pokemon;
            if (!place.IsActive || p == null || place.Roster == null || !InTheRed(p.CurrentHP, p.MaxHP)) continue;
            if (place.Roster.Members.Any(m => m != p && !m.IsFainted)) continue;
            if (Reacts(place, TrainerCue.LowHp) is not { } reacts) continue;
            Said? last = null;
            for (int i = log.Count - 1; i >= taken && last == null; i--) last = log[i] as Said;
            if (last != null) last.With(reacts);
            else Emit(reacts);
        }
    }
}
