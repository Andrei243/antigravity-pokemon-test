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
    GiveUp
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

    /// <summary>A question about a move to learn is open and waits for an answer.</summary>
    public bool IsLearningMove => learning != null && HUD.MenuState == BattleMenuState.LearnMove;

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
