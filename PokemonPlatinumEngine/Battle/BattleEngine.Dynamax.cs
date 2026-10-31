using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

// Dynamax on screen (plan 06 · R22): the move menu's item button arms it where neither a Mega Evolution nor a Z-Move
// can be had, the screen's Pokémon takes its raised HP and its Gigantamax form from the events, and its moves are
// named as Max Moves while it lasts (MaxMoveAt). The looks are plan 25 · Y15's: the growth and the red clouds
// (BattleAnimator.Dynamax, MoveFx.Giants.cs), each Max Move's set piece (MoveFx.MaxMoves.cs) and the button.
public partial class BattleEngine
{
    /// <summary>
    /// The move menu's Dynamax is armed (plan 06 · R22): the move chosen next goes with it, and is used as its Max
    /// Move. Toggled with the item button in the move menu where no Mega Evolution or Z-Move can be had
    /// (<see cref="ToggleLaterMechanic"/>).
    /// </summary>
    public bool DynamaxArmed { get; private set; }

    /// <summary>The Pokémon whose moves are being chosen can Dynamax, and no other of the player's has been chosen to this turn.</summary>
    public bool CanDynamaxNow =>
        core.Request is ActionRequest && !playingTheLog && MenuBattler.Pokemon != null && !choices.Values.Any(c => c.Dynamax) && core.CanDynamax(InCore(MenuBattler));

    /// <summary>The Pokémon whose moves are being chosen is Dynamaxed.</summary>
    public bool MenuDynamaxed => MenuBattler.Pokemon != null && BattleCore.IsDynamaxed(InCore(MenuBattler));

    /// <summary>The Max Move the move in this position is (Dynamaxed) or would be (Dynamax armed): what the move menu may name in its place (plan 25 · Y15).</summary>
    public MoveData? MaxMoveAt(int move) => MenuDynamaxed || DynamaxArmed ? core.MaxMoveFor(InCore(MenuBattler), move, MegaDynamaxArmed) : null;

    /// <summary>The Pokémon whose moves are being chosen may Mega Evolve and Mega Dynamax in one choice (plan 06 · R20c), and no other of the player's has chosen either this turn.</summary>
    public bool CanMegaDynamaxNow =>
        core.Request is ActionRequest && !playingTheLog && MenuBattler.Pokemon != null && !choices.Values.Any(c => c.Dynamax || c.Mega) && core.CanMegaDynamax(InCore(MenuBattler));

    /// <summary>The Pokémon whose moves are being chosen is in a Mega form (its Dynamax is a Mega Dynamax).</summary>
    public bool MenuIsMega => MenuBattler.Pokemon != null && InCore(MenuBattler).Pokemon?.FormData?.Kind == FormKind.Mega;

    /// <summary>The move menu's Mega Dynamax is armed (plan 06 · R20c): with the Mega Evolution, or alone for a Mega.</summary>
    public bool MegaDynamaxArmed => DynamaxArmed && (MegaArmed || MenuIsMega);

    /// <summary>Arms or disarms the Dynamax for the move to be chosen; false (and disarmed) where it can't be had.</summary>
    public bool ToggleDynamax()
    {
        if (!CanDynamaxNow)
        {
            DynamaxArmed = false;
            // A Terastallization where no Dynamax can be had either (plan 06 · R23)
            return ToggleTera();
        }
        DynamaxArmed = !DynamaxArmed;
        return true;
    }

    /// <summary>A move chosen with the Dynamax armed goes with it.</summary>
    private BattleChoice WithDynamaxArmed(BattleChoice choice)
    {
        bool armed = DynamaxArmed;
        DynamaxArmed = false;
        if (!armed || choice.Kind != ChoiceKind.Fight || choice.Move < 0 || choice.ZMove || choice.UltraBurst) return choice;
        // With a Mega Evolution only as a Mega Dynamax (plan 06 · R20c)
        if (choice.Mega ? !CanMegaDynamaxNow : !CanDynamaxNow) return choice;
        return choice with { Dynamax = true };
    }

    /// <summary>
    /// The screen's Pokémon takes the HP a Dynamax raises or its end gives back, and its bar keeps its share: the
    /// numbers the box shows move with it at once, never easing up like a heal or showing more than the most (plan 25 · Y15).
    /// </summary>
    private void TakeRaisedHp(Place place, Pokemon? shown, int maxHp, int hp)
    {
        if (shown == null) return;
        int before = Math.Max(1, shown.MaxHP);
        shown.TakeHp(maxHp, hp);
        var v = Anim[place.Side, place.Slot];
        if (v.Shown == shown) v.DisplayedHp = Math.Min(v.DisplayedHp * maxHp / before, maxHp);
    }

    // ------------------------------------------------------------------ the Champions with the Mega Max Band (plan 25 · YD6)

    /// <summary>How long the boss box takes to widen from the growth's burst, and to narrow back over the fall, in seconds.</summary>
    public const float BossWidens = 0.4f;

    /// <summary>A single battle against one trainer whose team is a Champion's Mega Max team: its Mega Dynamax ace is the boss.</summary>
    public bool AgainstAMegaMaxTeam =>
        Format == BattleFormat.Single && Trainers.Count == 1 && Data.TrainerDatabase.IsMegaMaxTeam(Trainers[0].Id);

    /// <summary>
    /// How far the foe's HP box is widened into the boss box (plan 25 · YD6; style guide, "The boss box"), 0 to 1: a
    /// foe of a Mega Max team that has Mega Dynamaxed, widened over <see cref="BossWidens"/> from its growth's burst
    /// (at once with no growth playing, as with the scene off) and narrowed back over the fall as its end plays;
    /// nought for anyone else. GPU-free: the animator's state alone, never the rules'.
    /// </summary>
    public float BossShare(CombatantView v)
    {
        if (!AgainstAMegaMaxTeam || v.Shown == null || !v.Present) return 0f;
        // A single battle: the foe is the one place across
        if (!ReferenceEquals(v, Anim[BattleSide.Enemy, 0])) return 0f;
        if (v.Dynamaxed && v.MegaMax)
        {
            if (v.GiantAge < 0f) return 1f;
            return Smooth((v.GiantAge - v.GiantLead - BattleAnimator.DynamaxGrown) / BossWidens);
        }
        // The fall of a Mega Dynamax: the end's cue on that place, told a Mega Dynamax's
        var end = Anim.Cues.FirstOrDefault(c => c.Kind == CueKind.DynamaxEnd && c.MegaMax && c.ToSide == BattleSide.Enemy && c.ToSlot == 0);
        if (end == null) return 0f;
        return 1f - Smooth(end.Age / BattleAnimator.DynamaxShrinkTime);
    }

    /// <summary>Whether a Pokémon's HP box is the boss box now (any share of it).</summary>
    public bool IsBoss(CombatantView v) => BossShare(v) > 0f;

    private static float Smooth(float k)
    {
        k = Math.Clamp(k, 0f, 1f);
        return k * k * (3f - 2f * k);
    }

    /// <summary>
    /// How long the lines must still wait for a growth on screen (plan 25 · YD6; style guide, "The lines wait for
    /// it"): what is left of the longest growth playing (its trainer's call included), nought for none and with the
    /// scene off. A line read before then goes on only once it is over, as a thrown ball holds the lines, so the next
    /// line, turn and menu never come while a Pokémon is still in its ball or growing (YD3's finding).
    /// </summary>
    /// <summary>A line has been read and the next waits for a growth (<see cref="GrowthHold"/>): the box shows no arrow then.</summary>
    public bool HeldForAGrowth => !waitingForMessageConfirm && messageWaitTimer > 0f && GrowthHold > 0f;

    public float GrowthHold =>
        !Scene ? 0f : Anim.Cues.Where(c => c.Kind == CueKind.Dynamax).Select(c => c.Length - c.Age).DefaultIfEmpty(0f).Max();

    /// <summary>
    /// A Champion's words as their Pokémon grows (plan 25 · YD6): read straight after the growth's line, once the
    /// growth is over, stepping back onto the platform as for any of their lines. The screen's alone: no moment of the
    /// rules asks for it. Nothing with the trainers' lines off.
    /// </summary>
    private void SayAsItGrows(Place place)
    {
        if (place.Side != BattleSide.Enemy || !SpeaksTrainerLines || At(place).Trainer is not { } speaker) return;
        if (TrainerLines.GiantFor(speaker.Id) is not { } said) return;
        bool second = Trainers.Count > 1 && (ReferenceEquals(Trainers[1], speaker) || Trainers[1].Id == speaker.Id && Trainers[0].Id != speaker.Id);
        Action? stepBack = Scene ? () => Anim.StepBack(second, said.Face, said.Gesture) : null;
        interlude.Enqueue(new Line(PlayerIdentity.Fill(Text.OfData(said.Text)), stepBack, PlayerIdentity.Fill(speaker.Name)));
        if (Scene) interlude.Enqueue(new Happening(Anim.SendOffSpeaker));
    }

    /// <summary>
    /// A Dynamax or its end on screen (plan 25 · Y15): its sound and its cue; the screen's Pokémon takes the HP and
    /// the most HP the event says at once (the box shows the raised numbers from the line on), and the form (a
    /// Gigantamax's, or back from one) inside the cue's light; at once with the scene off, where nothing plays.
    /// </summary>
    private void ShowDynamax(BattleEvent happening)
    {
        switch (happening)
        {
            case Dynamaxed grown:
            {
                var big = At(grown.Place).Pokemon;
                TakeRaisedHp(grown.Place, big, grown.MaxHp, grown.Hp);
                // The first Dynamax of a single battle is called by its trainer first (plan 25 · YD1); every sound,
                // the cry at the burst and the form wait for the call
                string? caller = Scene ? Anim.CallerFor(grown.Place.Side, grown.Place.Slot, IsFrontier) : null;
                // A Mega Dynamax is told by its form's kind, the log having no event of its own for it (plan 25 · YD4; R20c's question 12)
                Anim.Dynamax(grown.Place.Side, grown.Place.Slot, grown.Gigantamax, played: Scene, caller, megaMax: IsMegaDynamaxForm(grown.Form));
                float lead = caller != null ? BattleAnimator.DynamaxCallTime : 0f;
                string sound = grown.Gigantamax ? "gigantamax" : "dynamax";
                var side = grown.Place.Side;
                if (lead > 0f)
                {
                    Sound("dynamax_call", side);
                    After(lead, () => Sound(sound, side));
                }
                else Sound(sound, side);
                if (big == null) break;
                // Its own cry, an octave down in a long hall, at the burst; at once with the scene off (plan 25 · YD1)
                var pan = Pan(side);
                if (Scene) After(lead + BattleAnimator.DynamaxGrown, () => AudioManager.PlayCry(big, CryMode.Giant, pan));
                else AudioManager.PlayCry(big, CryMode.Giant, pan);
                if (Scene && big.Form != grown.Form) After(lead + BattleAnimator.DynamaxSwapTime, () => big.Form = grown.Form);
                else big.Form = grown.Form;
                SayAsItGrows(grown.Place);
                break;
            }
            case DynamaxEnded ended:
            {
                var small = At(ended.Place).Pokemon;
                TakeRaisedHp(ended.Place, small, ended.MaxHp, ended.Hp);
                var v = Anim[ended.Place.Side, ended.Place.Slot];
                bool standing = v.Dynamaxed && v.Present && v.FaintAge < 0f && v.RecallAge < 0f;
                if (standing)
                {
                    Sound("dynamax_end", ended.Place.Side);
                    // The collapse after the shudder (plan 25 · YD3); at once with the scene off
                    var side = ended.Place.Side;
                    if (Scene) After(BattleAnimator.DynamaxFallStart, () => Sound("giant_fall", side));
                    else Sound("giant_fall", side);
                }
                Anim.Undynamax(ended.Place.Side, ended.Place.Slot, played: Scene);
                if (small == null) break;
                if (Scene && standing && small.Form != ended.Form) After(BattleAnimator.DynamaxEndSwapTime, () => small.Form = ended.Form);
                else small.Form = ended.Form;
                break;
            }
        }
    }
}
