using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>
/// The rules of two of the Battle Frontier annex's venues (plan 14 · B9; <c>docs/mechanics/rulings.md</c>, "The Battle
/// Palace" and "The Battle Arena"), Emerald's. In the Palace (<see cref="CoreSetup.Palace"/>) every Pokémon chooses by
/// its nature (<see cref="PalaceController"/>) and one at half its HP or less, awake, chooses by its nature's other
/// shares and says so once. In the Arena (<see cref="CoreSetup.TurnLimit"/>) a bout of the two facing each other is
/// judged once it has run its turns with both standing (<see cref="ArenaJudge"/>), and the loser falls.
/// </summary>
public sealed partial class BattleCore
{
    /// <summary>A battle of the Battle Palace (<see cref="CoreSetup.Palace"/>).</summary>
    public bool Palace { get; }

    /// <summary>The turns a bout of the Battle Arena runs before it is judged (<see cref="CoreSetup.TurnLimit"/>); 0 for none.</summary>
    public int TurnLimit { get; }

    /// <summary>The Battle Arena's judge (null in any other battle): the bout under way and its counts.</summary>
    public ArenaJudge? Judge => TurnLimit > 0 ? judge ??= new ArenaJudge() : null;
    private ArenaJudge? judge;

    /// <summary>The verdicts given so far, in order (for tests and the harness).</summary>
    public IReadOnlyList<ArenaVerdict> Verdicts => verdicts;
    private readonly List<ArenaVerdict> verdicts = new();

    // The Pokémon of the Palace that have fallen to half their HP or less (Emerald's palaceFlags, by Pokémon)
    private readonly HashSet<Pokemon> palaceInTheRed = new();

    /// <summary>
    /// Whether a Pokémon of the Palace chooses by its nature's second shares (Emerald's <c>palaceFlags</c>): set as it comes
    /// in at half its HP or less and awake, or at the end of the turn it first is, and cleared as it comes in otherwise.
    /// </summary>
    public bool PalaceInTheRed(Battler b) => b.Pokemon != null && palaceInTheRed.Contains(b.Pokemon);

    /// <summary>Emerald's test, at the switch-in and at the turn's end: half its HP or less, some left, and not asleep.</summary>
    private static bool AtHalf(Pokemon p) => p.MaxHP / 2 >= p.CurrentHP && p.CurrentHP != 0 && p.Status != StatusCondition.Sleep;

    private void EnteredThePalace(Battler place)
    {
        if (!Palace || place.Pokemon is not { } p) return;
        if (AtHalf(p)) palaceInTheRed.Add(p);
        else palaceInTheRed.Remove(p);
    }

    /// <summary>The line the battle says for the player's own as they choose by themselves (a line of ours: the menu stays closed).</summary>
    private void ChoosesForItself(IEnumerable<Battler> free)
    {
        if (!Palace) return;
        foreach (var b in free.Where(b => b.IsPlayerSide && b.Trainer == null && !b.IsHeldToItsMove))
            Say(Text.Of("battle.venue.choosing", ("pokemon", b.Name)));
    }

    /// <summary>A Pokémon that can't bring itself to use the move it chose (Emerald's "incapable of using its power"); false for one that is down.</summary>
    private bool Falters(Battler user)
    {
        if (!user.IsActive) return false;
        user.MovedThisTurn = true;
        Say(Text.Of("battle.venue.hesitates", ("pokemon", user.Name)));
        return true;
    }

    /// <summary>
    /// What a chooser inside may choose at a venue: in a bout of the Arena nobody switches, so a switch chosen is the
    /// first move the Pokémon can use instead (Emerald's Arena trainers never switch).
    /// </summary>
    private BattleChoice AsTheVenueLets(Battler b, BattleChoice chosen)
    {
        if (TurnLimit <= 0 || chosen.Kind != ChoiceKind.Switch) return chosen;
        var moves = b.Pokemon!.Moves;
        int first = Enumerable.Range(0, moves.Count).FirstOrDefault(i => WhyNotMove(b, moves[i]) == null, -1);
        return BattleChoice.Fight(b.Place, first);
    }

    /// <summary>A bout begins between the two that face each other at the start of a turn when they aren't the two it was between.</summary>
    private void KeepTheBout()
    {
        if (Judge is not { } j) return;
        var mine = PlayerSlots[0];
        var theirs = EnemySlots[0];
        if (!mine.IsActive || !theirs.IsActive) return;
        if (!j.Between(mine.Pokemon, theirs.Pokemon)) j.Begin(mine.Pokemon!, theirs.Pokemon!);
    }

    /// <summary>A move used in a bout: its Mind, and once it went on, how it came off for Skill (<c>BattleArena_AddSkillPoints</c>).</summary>
    private void JudgeTheMove(MoveUse use, bool failed, bool wentOn)
    {
        if (Judge is not { } j || !j.Between(PlayerSlots[0].Pokemon, EnemySlots[0].Pokemon)) return;
        var side = use.User.Side;
        j.Moved(side, use.Own ?? use.Data);
        if (!wentOn) return;
        var hits = use.Hits.Where(h => h.Target != use.User).ToList();
        bool stopped = hits.Count > 0 && hits.All(h => h.Protected);
        bool super = hits.Any(h => h.Landed && h.SuperEffective), weak = hits.Any(h => h.Landed && h.NotVeryEffective);
        j.Skilled(side, ArenaJudge.SkillOf(failed, stopped, super, weak));
    }

    /// <summary>The Palace's lines for those first at half their HP, then the Arena's verdict on a bout that has run its turns.</summary>
    private void AtTheVenuesTurnEnd()
    {
        if (Palace)
            foreach (var b in BySpeed().Where(b => b.IsActive && !palaceInTheRed.Contains(b.Pokemon!) && AtHalf(b.Pokemon!)))
            {
                palaceInTheRed.Add(b.Pokemon!);
                var flavour = PalaceNatures.Of(b.Pokemon!.Nature).Flavour;
                Say(Text.Of($"battle.venue.flavour.{flavour}", ("pokemon", b.Name)));
            }
        JudgeTheBout();
    }

    /// <summary>
    /// The bout under way has run its turns with both standing: the judge's verdict on its line, and whoever lost falls
    /// (both on a draw), as Emerald's <c>BattleScript_ArenaDoJudgment</c> has it. A bout one side left early is over.
    /// </summary>
    private void JudgeTheBout()
    {
        if (Judge is not { } j) return;
        var mine = PlayerSlots[0];
        var theirs = EnemySlots[0];
        if (!mine.IsActive || !theirs.IsActive || !j.Between(mine.Pokemon, theirs.Pokemon))
        {
            j.End();
            return;
        }
        if (++j.TurnsPlayed < TurnLimit) return;

        var verdict = j.Verdict(mine.Pokemon!.CurrentHP, theirs.Pokemon!.CurrentHP);
        verdicts.Add(verdict);
        j.End();
        Say(Text.Of("battle.venue.timeUp")).With(new Judged(verdict));
        var losers = verdict.Winner switch
        {
            BattleSide.Player => new[] { theirs },
            BattleSide.Enemy => new[] { mine },
            _ => new[] { mine, theirs }
        };
        Say(verdict.Winner is { } side
            ? Text.Of("battle.venue.boutWon", ("pokemon", (side == BattleSide.Player ? mine : theirs).Name))
            : Text.Of("battle.venue.boutDrawn"));
        foreach (var loser in losers)
        {
            loser.Pokemon!.CurrentHP = 0;
            Say(Text.Of("battle.venue.boutLost", ("pokemon", loser.Name))).With(new HpChanged(loser.Place, 0, Healed: false));
        }
        ResolveFaints();
    }
}
