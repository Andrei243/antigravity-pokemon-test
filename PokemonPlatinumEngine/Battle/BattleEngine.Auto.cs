using System;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

/// <summary>
/// What AUTO chooses for one of the player's Pokémon (plan 12 · Q12): the slot of its move (<see cref="BattleEngine.Suggest"/>;
/// -1 for Struggle) and, where the move asks where it goes, the place of the foe it is aimed at (null where it asks nothing).
/// It is worked out from the battle's own numbers and draws nothing, so a battle played by AUTO rolls exactly as one whose
/// player chose the same, and its record replays the same.
/// </summary>
public readonly record struct AutoCommand(int Move, Place? Target);

/// <summary>
/// Auto-battle (plan 12 · Q12; style guide, "Battle panels", "AUTO"): the commands' fifth button, offered where the options'
/// AUTO-BATTLE row is on, plays the turn for the player and goes on turn after turn until something wants the player
/// (a faint on the player's side, a move to learn, Battle Style Shift's offer, the battle's end) or Back stops it. Every
/// answer goes through the menus' own <see cref="SelectMove"/> and <see cref="SelectTarget"/>, so the rules and the record
/// see a player's choices; it never opens the bag, so it never throws a ball.
/// </summary>
public partial class BattleEngine
{
    /// <summary>The commands' index of the AUTO button, beside FIGHT's four.</summary>
    public const int AutoIndex = 4;

    /// <summary>How long a whole line stays before AUTO reads it on (sooner where the options' auto-advance is quicker).</summary>
    public const float AutoReadSeconds = 0.9f;

    /// <summary>The options' AUTO-BATTLE row, set by the game each frame (as <see cref="AutoAdvanceAfter"/> is): off unless chosen.</summary>
    public bool AutoOn { get; set; }

    /// <summary>The commands carry AUTO: the row is on and the player chooses in this battle.</summary>
    public bool AutoOffered => AutoOn && !Watching && !Palace && Kind is not (BattleKind.Safari or BattleKind.PalPark or BattleKind.CatchingLesson);

    /// <summary>AUTO is playing the battle for the player.</summary>
    public bool AutoRunning { get; private set; }

    /// <summary>Why AUTO last stopped by itself (for a tool to print): "faint", "learn", "offer", "replace", "end", "back"; null while it runs or never ran.</summary>
    public string? AutoStoppedFor { get; private set; }

    private float autoLineWaited;

    /// <summary>
    /// What AUTO would choose now for the Pokémon whose menu is open: the move <see cref="Suggest"/> names and, for a
    /// move that asks where it goes in a battle of several places, the foe <see cref="Suggest"/> scored it against
    /// (the first foe standing). Draws nothing.
    /// </summary>
    public AutoCommand AutoCommandNow()
    {
        int move = Suggest();
        if (move < 0) return new AutoCommand(-1, null);
        var mine = InCore(MenuBattler);
        var foe = core.ActiveFoes(mine).FirstOrDefault();
        bool aimed = (IsDouble || IsHorde || IsSos || IsTriple) && Fighter.Moves[move].Target == MoveTarget.Selected;
        return new AutoCommand(move, aimed ? foe?.Place : null);
    }

    /// <summary>
    /// Answers the commands for the Pokémon whose menu is open as AUTO would; returns false where nothing was asked
    /// (no menu open). A move aimed at a foe out of reach goes to the first foe it does reach.
    /// </summary>
    public bool AutoChoose()
    {
        if (HUD.MenuState != BattleMenuState.Main || core.Request is not ActionRequest || playingTheLog) return false;
        var command = AutoCommandNow();
        if (command.Move < 0)
        {
            // Nothing left to use: FIGHT says so and it struggles
            SelectMainMenuOption(0);
            return true;
        }
        SelectMove(command.Move);
        if (HUD.MenuState == BattleMenuState.SelectTarget)
        {
            int target = command.Target is { } place ? TargetChoices.ToList().FindIndex(b => b.Place == place) : -1;
            if (target < 0) target = TargetChoices.ToList().FindIndex(b => b.Place.Side == BattleSide.Enemy);
            SelectTarget(Math.Max(0, target));
        }
        // A move the rules refused leaves the menu open: AUTO gives the battle back rather than ask again
        if (HUD.MenuState is BattleMenuState.Moves or BattleMenuState.SelectTarget) StopAuto("back");
        return true;
    }

    /// <summary>Plays one whole turn's choices as AUTO would (each of the player's Pokémon asked), without going on to the next turn.</summary>
    public bool AutoTurn()
    {
        bool any = false;
        for (int guard = 0; guard < 6 && AutoChoose(); guard++) any = true;
        return any;
    }

    /// <summary>Starts AUTO: it plays this turn now and every turn after, reading the lines on, until it is stopped.</summary>
    public void AutoUntilStopped()
    {
        if (!AutoOffered) return;
        AutoRunning = true;
        AutoStoppedFor = null;
        autoLineWaited = 0f;
        AudioManager.PlaySound("select");
        AutoChoose();
    }

    /// <summary>Stops AUTO; the commands are the player's again.</summary>
    public void StopAuto(string why)
    {
        if (!AutoRunning) return;
        AutoRunning = false;
        AutoStoppedFor = why;
    }

    /// <summary>
    /// AUTO's frame, called from <see cref="Update"/> before anything reads a key: Back stops it, a whole line is read on
    /// after its pause, and an open menu is answered. True where it took the frame.
    /// </summary>
    private bool AutoFrame(float dt)
    {
        if (!AutoRunning) return false;
        if (!AutoOffered)
        {
            StopAuto("back");
            return false;
        }
        if (InputManager.IsActionPressed(GameAction.Cancel))
        {
            StopAuto("back");
            AudioManager.PlaySound("cancel");
            return true;
        }
        if (IsLearningMove)
        {
            StopAuto(offered != null ? "offer" : "learn");
            return false;
        }
        if (waitingForMessageConfirm)
        {
            float pause = Math.Min(AutoReadSeconds, AutoAdvanceAfter ?? AutoReadSeconds);
            autoLineWaited += dt;
            if (autoLineWaited >= pause)
            {
                autoLineWaited = 0f;
                ConfirmMessage();
            }
            return true;
        }
        autoLineWaited = 0f;
        if (IsBattleOver)
        {
            StopAuto("end");
            return false;
        }
        if (HUD.MenuState == BattleMenuState.SwitchPokemon && replacing)
        {
            StopAuto("replace");
            return false;
        }
        if (HUD.MenuState == BattleMenuState.Main && core.Request is ActionRequest) return AutoChoose();
        return false;
    }

    /// <summary>AUTO stops on a faint on the player's side (called as the faint's event is shown).</summary>
    private void AutoSeesAFaint(Place place)
    {
        if (place.Side == BattleSide.Player) StopAuto("faint");
    }
}
