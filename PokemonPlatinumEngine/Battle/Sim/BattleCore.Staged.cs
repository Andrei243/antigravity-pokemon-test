using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Battle.Sim;

// Staged battles (docs/mechanics/rulings.md, "Staged battles"): a foe the story brings that the player isn't meant to
// beat, by three terms a script's battle line may ask for, each by itself: its level drawn as ??? (the screen's
// alone), every move aimed at it failing ("untouchable"), and it choosing nothing ("passive"); and a battle that ends
// by itself after so many turns. Sword and Shield's first meeting with Zacian in the Slumbering Weald is all of them
// at once; Carpathia's Furtunaur, met first at a level nobody can match, is the level alone. A staged battle gives
// no EXP, prize or find, takes no money, can't be caught in and tells the Pokédex nothing; with none of the terms
// nothing here runs, so the golden battles stand as they were.
public sealed partial class BattleCore
{
    /// <summary>The foe's level is drawn as ??? (<see cref="CoreSetup.HideLevel"/>): the face's alone, kept so a record shows it.</summary>
    public bool HideLevel { get; }

    /// <summary>Every move aimed at the other side fails (<see cref="CoreSetup.Untouchable"/>).</summary>
    public bool Untouchable { get; }

    /// <summary>The other side chooses nothing and only watches (<see cref="CoreSetup.Passive"/>).</summary>
    public bool Passive { get; }

    /// <summary>The turns after which the battle ends by itself (<see cref="CoreSetup.EndAfterTurns"/>), 0 for never.</summary>
    public int EndAfterTurns { get; }

    /// <summary>A staged battle: any of the four terms. It gives nothing, takes nothing and can't be caught in.</summary>
    public bool Staged => HideLevel || Untouchable || Passive || EndAfterTurns > 0;

    /// <summary>What a ball thrown in a staged battle is told: a line of our own.</summary>
    public static string StagedBallRefusal => Text.Of("battle.stagedNoCatch");

    /// <summary>Whether a place is one the player's side may not touch: the other side's, in an untouchable battle.</summary>
    internal bool IsUntouched(Battler user, Battler target) =>
        Untouchable && user.Side == BattleSide.Player && target.Side == BattleSide.Enemy;

    /// <summary>
    /// The one gate of the untouchable term, in <see cref="Perform"/> once the move is paid for and its line said: every
    /// target of the other side is taken off the move, with the move shown blocked on it and the fixed line said. True
    /// when nobody is left, and the move ends there as one with no target does: nothing lands, no side effect, no recoil,
    /// no crash, nothing set on the field.
    /// </summary>
    private bool Untouched(MoveUse use)
    {
        if (!Untouchable) return false;
        var user = use.User;
        var untouched = use.Targets.Where(t => IsUntouched(user, t)).ToList();
        if (untouched.Count == 0) return false;
        foreach (var t in untouched) use.Line?.With(Shown(user, t, use.Move, new MoveHit { Target = t, Protected = true }));
        Say(Text.Of("battle.cantBeTouched"));
        use.Targets.RemoveAll(untouched.Contains);
        if (use.Targets.Count > 0) return false;
        Unlock(user);
        failedNow = true;
        return true;
    }

    /// <summary>Whether a place chooses nothing this turn: the other side's, in a passive battle.</summary>
    private bool WatchesOnly(Battler b) => Passive && b.Side == BattleSide.Enemy;

    /// <summary>The passive term's line, once a turn while the other side has anyone out, after the turn's actions.</summary>
    private void SayItOnlyWatches()
    {
        if (Passive && EnemySlots.Any(b => b.IsActive)) Say(Text.Of("battle.onlyWatches"));
    }

    /// <summary>
    /// The staged end (<see cref="EndAfterTurns"/>): once the turns are played the other side's Pokémon leave the field,
    /// and the battle ends as a wild Pokémon's flight, which a script reads as <c>fled</c>.
    /// </summary>
    private void EndIfTheTurnsAreOver()
    {
        if (Result != BattleResult.None || EndAfterTurns <= 0 || Turn < EndAfterTurns) return;
        foreach (var b in EnemySlots.Where(b => b.IsActive))
            Say(Text.Of("battle.stagedLeaves", ("pokemon", b.Name))).With(new Left(b.Place, Ran: true));
        End(BattleResult.EnemyFled);
    }

    /// <summary>The places that choose for themselves this turn, with whoever only watches left out.</summary>
    private IEnumerable<Battler> ThatChoose(IEnumerable<Battler> free) => free.Where(b => !WatchesOnly(b));
}
