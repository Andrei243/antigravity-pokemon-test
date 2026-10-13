using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

/// <summary>Where the question of a move to learn has got to (plan 06 · R10).</summary>
public enum LearnStep
{
    /// <summary>"Should a move be forgotten for it?" Yes (0) or No (1).</summary>
    Forget,
    /// <summary>Which of its four moves to forget (0 to 3), or none of them (4).</summary>
    Choose,
    /// <summary>"Give up on learning it?" Yes (0) or No (1).</summary>
    GiveUp,
    /// <summary>The Battle Frontier's question on RUN (plan 06 · R18a): whether to give the match up. Yes (0) or No (1).</summary>
    Forfeit,
    /// <summary>
    /// Battle Style Shift's offer (plan 08 · P1): the trainer's next Pokémon is coming; switch first? Yes (0) opens the
    /// team to choose from, No (1) stays in.
    /// </summary>
    Shift
}

/// <summary>
/// The question a battle asks when a Pokémon that knows four moves reaches a level with another to learn (plan 06 ·
/// R10; the original's <c>SEQ_GET_EXP_WANTS_TO_LEARN_MOVE</c> to <c>SEQ_GET_EXP_LEARNED_MOVE</c>): it wants the
/// move; should one be forgotten for it? Yes asks which, No (or keeping them all) asks whether to give up on it, and
/// not giving up asks again. The rules changed nothing (<see cref="MoveWanted"/>): what is chosen is given to the
/// game's Pokémon and to the rules' copy of it at once.
/// <para>
/// <see cref="ConfirmMessage"/> answers with whatever keeps the moves as they are (No, keep them all, give up), so
/// anything that reads a battle on with it (the tests, the harness) gets through without forgetting a move; the
/// player answers with the keys, or a tool with <see cref="ChooseLearn"/>.
/// </para>
/// </summary>
public partial class BattleEngine
{
    // The question waiting, and lines of its own to show before anything else the log has
    private (Pokemon Pokemon, string Move)? learning;
    private readonly Queue<Step> interlude = new();

    /// <summary>
    /// A question is open and waits for an answer: about a move to learn, the Frontier's forfeit, or Battle Style
    /// Shift's offer (plan 08 · P1), which <see cref="ConfirmMessage"/> answers No to, staying in.
    /// </summary>
    public bool IsLearningMove => (learning != null || forfeiting || offered != null && !offerPanel) && HUD.MenuState == BattleMenuState.LearnMove;

    // Battle Style Shift's offer (plan 08 · P1): the rules' request while it waits, and whether its Yes has opened the team
    private SwitchOfferRequest? offered;
    private bool offerPanel;

    /// <summary>Battle Style Shift's offer is open (plan 08 · P1): its question, or the team opened by its Yes.</summary>
    public bool IsOfferingSwitch => offered != null;

    /// <summary>The trainer's Pokémon that comes next while the offer is open, as the screen holds it.</summary>
    public Pokemon? OfferedFoe => offered == null ? null : mirror.Shown(offered.Upcoming);

    /// <summary>The rules ask whether to switch before the trainer's next Pokémon: the line is the question, with the cursor on Yes, as the original's.</summary>
    private void OfferSwitch(SwitchOfferRequest offer)
    {
        offered = offer;
        offerPanel = false;
        menuSlot = offer.Place.Slot;
        LearnStep = LearnStep.Shift;
        LearnCursor = 0;
        CurrentSpeaker = null;
        HUD.MenuState = BattleMenuState.LearnMove;
    }

    /// <summary>Answers Battle Style Shift's offer by staying in (its No, or a cancel on the team it opened).</summary>
    public void StayIn()
    {
        if (offered is not { } offer) return;
        offered = null;
        offerPanel = false;
        Submit(new[] { BattleChoice.Stay(offer.Place) });
    }

    /// <summary>Answers Battle Style Shift's offer with a switch to the party member at <paramref name="partyIndex"/>.</summary>
    private void SwitchAtOffer(int partyIndex)
    {
        if (offered is not { } offer) return;
        offered = null;
        offerPanel = false;
        Submit(new[] { BattleChoice.Switch(offer.Place, partyIndex) });
    }

    // The Battle Frontier's question on RUN is open (the same Yes and No as a move to learn)
    private bool forfeiting;

    /// <summary>The Battle Frontier asks whether the match is to be given up: the original's own question, with the cursor on No.</summary>
    private void AskToForfeit()
    {
        forfeiting = true;
        LearnStep = LearnStep.Forfeit;
        LearnCursor = 1;
        currentMessage = "Give the match up and end the challenge here?";
        CurrentSpeaker = null;
        HUD.MenuState = BattleMenuState.LearnMove;
    }

    public LearnStep LearnStep { get; private set; }

    /// <summary>The answer under the cursor: Yes (0) or No (1), or a move to forget (0 to 3) and keeping them all (4).</summary>
    public int LearnCursor { get; private set; }

    /// <summary>The Pokémon that wants a move, and the move.</summary>
    public Pokemon? Learner => learning?.Pokemon;
    public string? MoveToLearn => learning?.Move;

    /// <summary>The log reached a move wanted: its question opens once the lines before it have been read.</summary>
    private void WantMove(MoveWanted wanted)
    {
        learning = (mirror.Shown(wanted.Pokemon), wanted.Move);
        AskToLearn();
    }

    private void AskToLearn()
    {
        var (p, move) = learning!.Value;
        Interlude($"{p.DisplayName} is trying to learn {move}.");
        Interlude($"But {p.DisplayName} already knows four moves.");
        Open(LearnStep.Forget, $"Forget one of them to make room for {move}?");
    }

    private void Interlude(string line, Action? then = null)
    {
        interlude.Enqueue(new Line(line, null));
        if (then != null) interlude.Enqueue(new Happening(then));
    }

    /// <summary>Opens a step of the question once the lines before it are read, with the cursor on its first answer.</summary>
    private void Open(LearnStep step, string question) => interlude.Enqueue(new Happening(() =>
    {
        LearnStep = step;
        LearnCursor = 0;
        currentMessage = question;
        CurrentSpeaker = null;
        HUD.MenuState = BattleMenuState.LearnMove;
    }));

    /// <summary>Moves the cursor among the answers (wrapping).</summary>
    public void MoveLearnCursor(int dy)
    {
        if (!IsLearningMove || dy == 0) return;
        int count = LearnStep == LearnStep.Choose ? 5 : 2;
        LearnCursor = ((LearnCursor + dy) % count + count) % count;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>
    /// Answers the open question: for <see cref="LearnStep.Forget"/> and <see cref="LearnStep.GiveUp"/> 0 is Yes and
    /// 1 No; for <see cref="LearnStep.Choose"/> 0 to 3 forgets that move and 4 keeps them all.
    /// </summary>
    public void ChooseLearn(int answer)
    {
        if (!IsLearningMove) return;
        if (offered != null)
        {
            if (answer == 0)
            {
                // Yes: the team, to choose who goes in; a cancel there stays in all the same
                AudioManager.PlaySound("select");
                offerPanel = true;
                HUD.SwitchMenuIndex = 0;
                HUD.MenuState = BattleMenuState.SwitchPokemon;
                return;
            }
            AudioManager.PlaySound("cancel");
            StayIn();
            return;
        }
        if (forfeiting)
        {
            forfeiting = false;
            AudioManager.PlaySound(answer == 0 ? "select" : "cancel");
            HUD.MenuState = BattleMenuState.Main;
            if (answer == 0) Commit(BattleChoice.Run(MenuBattler.Place));
            return;
        }
        var (p, move) = learning!.Value;
        HUD.MenuState = BattleMenuState.Message;
        switch (LearnStep)
        {
            case LearnStep.Forget when answer == 0:
                AudioManager.PlaySound("select");
                Interlude($"Which move should {p.DisplayName} forget?");
                Open(LearnStep.Choose, $"Which move should {p.DisplayName} forget?");
                break;
            case LearnStep.Choose when answer >= 0 && answer < p.Moves.Count:
            {
                AudioManager.PlaySound("select");
                string forgotten = p.Moves[answer].Name;
                // The game's Pokémon and the rules' copy learn it together (the original's Pokemon_SetMoveSlot,
                // with the place's PP Ups gone)
                p.ReplaceMove(answer, move);
                mirror.Copy(p).ReplaceMove(answer, move);
                Interlude("One... two... three... and...");
                Interlude($"{p.DisplayName} has forgotten {forgotten}.");
                Interlude("And in its place...");
                Interlude($"{p.DisplayName} learned {move}!");
                learning = null;
                break;
            }
            case LearnStep.Forget:
            case LearnStep.Choose:
                AudioManager.PlaySound("cancel");
                Interlude("Then...");
                Open(LearnStep.GiveUp, $"Give up on learning {move}?");
                break;
            case LearnStep.GiveUp when answer == 0:
                AudioManager.PlaySound("select");
                Interlude($"{p.DisplayName} did not learn {move}.");
                learning = null;
                break;
            case LearnStep.GiveUp:
                AudioManager.PlaySound("cancel");
                AskToLearn();
                break;
        }
        Pump();
    }

    /// <summary>The answer that keeps every move: No to forgetting, none of them, Yes to giving up.</summary>
    private int KeepingAnswer => LearnStep switch
    {
        LearnStep.Forget => 1,
        LearnStep.Choose => 4,
        LearnStep.Forfeit => 1,
        LearnStep.Shift => 1,
        _ => 0
    };

    /// <summary>The keys while a question is open: up and down move the cursor, confirm answers with it, cancel keeps the moves.</summary>
    private void HandleLearnInput()
    {
        if (InputManager.IsActionPressed(GameAction.Up)) MoveLearnCursor(-1);
        else if (InputManager.IsActionPressed(GameAction.Down)) MoveLearnCursor(1);
        else if (ConfirmPressed) ChooseLearn(LearnCursor);
        else if (InputManager.IsActionPressed(GameAction.Cancel)) ChooseLearn(KeepingAnswer);
    }
}
