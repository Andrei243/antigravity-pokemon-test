using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Battle;

/// <summary>Why the picture may split in two at a moment of a battle (plan 45 · BC3).</summary>
public enum SplitReason
{
    /// <summary>Both sides' leads out at a battle's opening: a trainer's battle, or a wild legendary or mythical Pokémon.</summary>
    Leads,
    /// <summary>A trainer's last Pokémon out of its ball.</summary>
    LastPokemon,
}

/// <summary>
/// A moment of the battle the picture may split at (plan 45 · BC3): noted by the animator whatever the manner, and
/// filmed or not by the camera's manner (<see cref="Graphics.CameraSplits"/>). Its times are the animator's clock.
/// </summary>
public sealed class SplitMoment
{
    public SplitMoment(SplitReason reason, float start, bool boss)
    {
        Reason = reason;
        Start = start;
        Boss = boss;
    }

    public SplitReason Reason { get; }

    /// <summary>When the moment came, in <see cref="BattleAnimator.Time"/>.</summary>
    public float Start { get; }

    /// <summary>A Gym Leader's, an Elite Four's or a Champion's last Pokémon.</summary>
    public bool Boss { get; }

    /// <summary>
    /// When something that waits on the player (a menu, a question) opened, or a trainer stepped back to speak, after
    /// the moment came: the split goes out from then if it hasn't yet; -1 for nothing.
    /// </summary>
    public float CutShortAt { get; internal set; } = -1f;
}

public sealed partial class BattleAnimator
{
    /// <summary>
    /// Whether the leads' coming out is a moment the picture may split at (plan 45 · BC3): a trainer's battle, or a
    /// wild legendary or mythical Pokémon (decision 3). The face sets it as the battle is made; the camera decides.
    /// </summary>
    public bool LeadsMayDivide { get; set; }

    private readonly List<SplitMoment> splitMoments = new();
    private bool leadsNoted;
    private (BattleSide Side, int Slot, bool Boss, float Since)? lastComing;

    /// <summary>The moments the picture may split at, oldest first (plan 45 · BC3).</summary>
    public IReadOnlyList<SplitMoment> SplitMoments => splitMoments;

    /// <summary>
    /// A trainer's last Pokémon is coming out at a place (plan 45 · BC3, the core's <c>TrainerReacts</c> of
    /// <c>LastPokemon</c>): its moment comes once it has come out of its ball.
    /// </summary>
    public void NoteLastPokemon(BattleSide side, int slot, bool boss) => lastComing = (side, slot, boss, Time);

    /// <summary>
    /// Notes whether anything waits on the player's choice now (the face, every frame): a split under way is cut short
    /// from the moment it opens.
    /// </summary>
    public void NoteChoosing(bool choosing)
    {
        if (!choosing) return;
        foreach (var m in splitMoments)
            if (m.CutShortAt < 0f && m.Start <= Time) m.CutShortAt = Time;
    }

    /// <summary>Notes the moments that have come this frame: the leads out, a last Pokémon out of its ball, a trainer stepping back to speak.</summary>
    private void NoteSplitMoments()
    {
        if (Speaking != null)
            foreach (var m in splitMoments)
                if (m.CutShortAt < 0f && m.Start <= Time - Speaking.Age) m.CutShortAt = Math.Max(m.Start, Time - Speaking.Age);

        // The leads: every place of both sides filled and nobody still coming out, once the opening's sweep is over
        if (LeadsMayDivide && !leadsNoted && Time >= 1.8f && SidesStand())
        {
            leadsNoted = true;
            splitMoments.Add(new SplitMoment(SplitReason.Leads, Time, false));
        }

        if (lastComing is { } coming)
        {
            var v = this[coming.Side, coming.Slot];
            if (v.Present && v.SendOutAge < 0f && Time > coming.Since)
            {
                splitMoments.Add(new SplitMoment(SplitReason.LastPokemon, Time, coming.Boss));
                lastComing = null;
            }
            // One that never comes out (nobody fills the place) is forgotten
            else if (Time - coming.Since > 6f) lastComing = null;
        }
    }

    /// <summary>
    /// The newest super-effective or critical hit that landed on another Pokémon (plan 45 · BC4), and when its cue
    /// began in <see cref="Time"/>: what a manner that reacts films once the move's own shots are over. Null for none.
    /// </summary>
    public (EffectCue Cue, float Start)? LastStrike { get; private set; }

    private void NoteStrike(EffectCue cue)
    {
        if (cue.Kind != CueKind.Move || cue.Charging || cue.OnSelf || !cue.Landed) return;
        if (cue.SuperEffective || cue.Critical) LastStrike = (cue, Time);
    }

    private bool SidesStand()
    {
        for (int side = 0; side < 2; side++)
        {
            int slots = SlotsOf((BattleSide)side);
            if (slots == 0) return false;
            for (int slot = 0; slot < slots; slot++)
            {
                var v = this[(BattleSide)side, slot];
                if (!v.Present || v.SendOutAge >= 0f || v.ArriveAge >= 0f) return false;
            }
        }
        return true;
    }
}
