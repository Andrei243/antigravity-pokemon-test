using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core.Autoplay;

/// <summary>
/// The playthrough bot and the key fuzzer at work (plan 16 · T7): a frame at a time it decides what a player would
/// press, hands the game those actions (<see cref="InputManager.Feed"/>) and its steering, runs the game's own
/// <see cref="GameEngine.Update"/>, and then checks what the frame left. It needs no window: the tests run it on a
/// headless game (plan 16 · T12) and the harness's <c>play</c> mode on the drawn one, taking a shot at every first
/// arrival.
/// <para>
/// In the field with nothing else going on it follows <see cref="Policy"/> (steps through <see cref="GameEngine.Steering"/>,
/// people and things through the game's own <c>TryInteract</c>, every way and trigger by stepping onto it, running
/// once the Running Shoes are had); a line or a question is read on with the confirm button (a question's first
/// answer); a battle's menu is answered with <see cref="BattleEngine.Suggest"/>, a fainted Pokémon replaced by the
/// first that can fight; a screen the bot has nothing to do on is confirmed, and backed out of once it has stood
/// for a while. With <c>keys</c> it is the fuzzer instead: a seeded stream of actions (<see cref="KeyFuzzer"/>) and
/// nothing decided.
/// </para>
/// <para>
/// What a frame is checked for, each a finding of <see cref="Report"/>: an exception (the run stops there), a
/// softlock (<see cref="Softlock"/>: whatever the game holds, unchanged for ten seconds of game time while input went
/// in; thirty on a screen of its own, whose scenes run long), a script gone wrong, and a value out of range (HP, PP,
/// the bag's counts, the money). A goal that is not reached within <see cref="GoalSeconds"/> fails, and one that
/// fails three times is dropped.
/// </para>
/// </summary>
internal sealed class Autoplayer
{
    /// <summary>How long the bot tries for one goal before it counts as failed.</summary>
    public const float GoalSeconds = 8f;

    /// <summary>How long a screen the bot has no business on is confirmed before it is backed out of too.</summary>
    public const float ScreenPatience = 8f;

    private readonly GameEngine engine;
    private readonly GameEngine.Driver game;
    private readonly KeyFuzzer? fuzzer;
    private readonly Softlock watch = new();
    private readonly HashSet<string> outOfRange = new(StringComparer.Ordinal);
    private int frame;
    private GameState lastState;
    private float stateSeconds;
    private string? goalKey;
    private float goalSeconds;
    private int scriptsWrong;
    private BattleEngine? battleSeen;

    public AutoplayMemory Memory { get; } = new();
    public AutoplayReport Report { get; } = new();

    /// <summary>Called with a place's key the first time the run comes to it (the harness shoots it).</summary>
    public Action<string>? FirstArrival { get; set; }

    /// <summary>The run has stopped on an exception.</summary>
    public bool Stopped { get; private set; }

    /// <summary>The goal being gone for, if any.</summary>
    public Goal? Goal { get; private set; }

    public Autoplayer(GameEngine engine, int seed = 1, bool keys = false)
    {
        this.engine = engine;
        game = engine.Drive;
        fuzzer = keys ? new KeyFuzzer(seed) : null;
        lastState = game.State;
        scriptsWrong = engine.ScriptsGoneWrong.Count;
    }

    /// <summary>Plays frames for this many seconds of game time, or until the run stops; nothing is done once it has.</summary>
    public void Play(double seconds, float dt = 1f / 60f)
    {
        for (double t = 0; t < seconds && !Stopped && !engine.QuitRequested; t += dt) Frame(dt);
    }

    /// <summary>One frame: decided, played and checked.</summary>
    public void Frame(float dt)
    {
        if (Stopped) return;
        frame++;
        try
        {
            var input = fuzzer?.Next() ?? Decide(dt);
            if (fuzzer != null) engine.Steering = null;
            InputManager.Feed(input);
            engine.Update(dt);
            Report.Seconds += dt;
            Check(dt);
        }
        catch (Exception e)
        {
            Report.Exceptions.Add($"{game.State} on {game.Map?.Name} at {game.Player?.GridX},{game.Player?.GridY}: {e}");
            Stopped = true;
            Release();
        }
    }

    /// <summary>Gives the keys back: no steering and nothing fed.</summary>
    public void Release()
    {
        engine.Steering = null;
        InputManager.Feed(null);
    }

    // ---------------------------------------------------------------- deciding

    private ActionFrame Decide(float dt)
    {
        var state = game.State;
        if (state != lastState)
        {
            lastState = state;
            stateSeconds = 0f;
        }
        stateSeconds += dt;

        if (state != GameState.Overworld) engine.Steering = null;
        switch (state)
        {
            case GameState.Overworld:
                return InTheField(dt);
            case GameState.Battle when game.Battle is { } battle:
                return InBattle(battle);
            case GameState.Transition:
                return ActionFrame.None;
            case GameState.Dialogue:
                return Pulse(GameAction.Confirm);
            default:
                // A screen: confirmed, and after a while backed out of as well
                return stateSeconds > ScreenPatience && frame % 12 >= 6 ? Pulse(GameAction.Cancel) : Pulse(GameAction.Confirm);
        }
    }

    // An action pressed every sixth frame: a press needs the frames between with nothing down
    private ActionFrame Pulse(GameAction action) => frame % 6 == 0 ? ActionFrame.Press(action) : ActionFrame.None;

    private ActionFrame InTheField(float dt)
    {
        var player = game.Player;
        var map = game.Map;
        if (engine.ScriptRunning || game.Flow.TrainerApproach != null)
        {
            engine.Steering = null;
            return Pulse(GameAction.Confirm);
        }
        if (game.StartMenu.IsActive)
        {
            engine.Steering = null;
            return Pulse(GameAction.Cancel);
        }
        // Mid-step, or carried by ice or a moving floor: the step in hand goes on
        if (player.IsMoving || player.IsSliding || player.IsPushing) return ActionFrame.None;

        Arrived(map, player.GridX, player.GridY);
        var walker = new Walker(player.Mode, player.HeightOn(map), false, player.FastGear, player.Moves, false, player.Bike);
        var team = game.Party.Members.Where(p => !p.IsEgg).ToList();
        bool low = team.Count > 0 && (team.Any(p => p.IsFainted) || team[0].CurrentHP * 3 < team[0].MaxHP);
        var plan = Policy.Next(map, player.GridX, player.GridY, walker, Memory, t => t.Variable == null || game.Story.Var(t.Variable) == t.Value, low);
        if (plan == null)
        {
            Goal = null;
            Report.Finished = true;
            engine.Steering = null;
            return ActionFrame.None;
        }
        Report.Finished = false;

        var goal = plan.Goal;
        Goal = goal;
        if (goal.Key != goalKey)
        {
            goalKey = goal.Key;
            goalSeconds = 0f;
        }
        goalSeconds += dt;
        if (goalSeconds > GoalSeconds)
        {
            if (Memory.Fail(goal.Key)) Report.Dropped.Add(goal.Key);
            goalKey = null;
            engine.Steering = null;
            return ActionFrame.None;
        }

        if (plan.Steps.Count > 0)
        {
            // The last step onto a way or a trigger takes it: noted as it is taken (a step the other way first only turns)
            if (plan.Steps.Count == 1 && goal.Kind is GoalKind.Warp or GoalKind.Trigger && player.Facing == plan.Steps[0]) Take(goal);
            engine.Steering = (plan.Steps[0], player.HasRunningShoes);
            return ActionFrame.None;
        }
        if (goal.IsStepOnto)
        {
            Take(goal);
            engine.Steering = null;
            return ActionFrame.None;
        }

        // Beside them: turn to face, then speak
        if (player.Facing != goal.Face)
        {
            engine.Steering = (goal.Face, false);
            return ActionFrame.None;
        }
        engine.Steering = null;
        Take(goal);
        game.Interact();
        return ActionFrame.None;
    }

    private void Take(Goal goal)
    {
        Memory.Complete(goal.Key);
        goalKey = null;
        switch (goal.Kind)
        {
            case GoalKind.Speak: Report.Spoken.Add(goal.Key); break;
            case GoalKind.Item: Report.Items.Add(goal.Key); break;
            case GoalKind.Obstacle: Report.Obstacles.Add(goal.Key); break;
            case GoalKind.Trigger: Report.Triggers.Add(goal.Key); break;
            case GoalKind.Warp: Report.Warps.Add(goal.Key); break;
        }
    }

    private void Arrived(Map map, int x, int y)
    {
        if (Memory.Visit(map.Name, x, y)) Report.Tiles++;
        Report.Maps.Add(map.Name);
        string place = map.IsStreamed ? map.AreaAt(x, y)?.Key is { Length: > 0 } key ? key : map.Name : map.Name;
        if (Report.Arrive(place)) FirstArrival?.Invoke(place);
    }

    private ActionFrame InBattle(BattleEngine battle)
    {
        if (battle.IsBattleOver) return Pulse(GameAction.Confirm);
        if (battle.IsWaitingForConfirm) return Pulse(GameAction.Confirm);
        switch (battle.HUD.MenuState)
        {
            case BattleMenuState.Main when battle.Kind is BattleKind.Safari or BattleKind.PalPark:
                battle.SelectMainMenuOption(0);
                return ActionFrame.None;
            case BattleMenuState.Main when battle.Kind == BattleKind.CatchingLesson:
                return Pulse(GameAction.Confirm);
            case BattleMenuState.Main:
            case BattleMenuState.Moves:
                int move = battle.Suggest();
                if (move < 0) battle.SelectMainMenuOption(0);
                else battle.SelectMove(move);
                return ActionFrame.None;
            case BattleMenuState.SelectTarget:
                battle.SelectTarget(0);
                return ActionFrame.None;
            case BattleMenuState.SwitchPokemon when battle.IsChoosingReplacement:
                var members = battle.PlayerParty.Members;
                int next = Enumerable.Range(0, members.Count)
                    .FirstOrDefault(i => !members[i].IsEgg && !members[i].IsFainted && !battle.PlayerSlots.Any(b => b.Pokemon == members[i]), -1);
                if (next >= 0) battle.SelectSwitch(next);
                return ActionFrame.None;
            case BattleMenuState.Message:
            case BattleMenuState.LearnMove:
                return Pulse(GameAction.Confirm);
            default:
                return Pulse(GameAction.Cancel);
        }
    }

    // ---------------------------------------------------------------- checking

    private void Check(float dt)
    {
        var state = game.State;

        // Battles: counted as they open, their outcome as they close
        if (game.Battle is { } battle && battle != battleSeen && state == GameState.Battle)
        {
            battleSeen = battle;
            Report.Battles++;
        }
        if (battleSeen != null && state != GameState.Battle && state != GameState.Transition)
        {
            if (battleSeen.Result is BattleResult.PlayerVictory or BattleResult.EnemyCaught) Report.BattlesWon++;
            battleSeen = null;
        }
        Report.TrainersBeaten = game.Story.DefeatedTrainers.Count;
        if (game.ScriptNow is { } script) Report.Scripts.Add(script);

        // Scripts that went wrong, as the game noted them
        var wrong = engine.ScriptsGoneWrong;
        for (; scriptsWrong < wrong.Count; scriptsWrong++) Report.ScriptsGoneWrong.Add(wrong[scriptsWrong]);

        // Values in range
        foreach (var p in game.Party.Members)
        {
            if (p.CurrentHP < 0 || p.CurrentHP > p.MaxHP) OutOfRange($"{p.Species.Name}'s HP {p.CurrentHP} of {p.MaxHP}");
            foreach (var m in p.Moves)
                if (m.CurrentPP < 0 || m.CurrentPP > m.MaxPP) OutOfRange($"{p.Species.Name}'s {m.Data.Name} at {m.CurrentPP} PP of {m.MaxPP}");
        }
        foreach (var item in game.Bag.AllItems)
            if (item.Quantity < 0 || item.Quantity > 999) OutOfRange($"{item.Name} x{item.Quantity} in the bag");
        if (game.Money < 0 || game.Money > 999_999) OutOfRange($"money {game.Money}");

        // A softlock: whatever the game holds, standing still while input goes in
        if (fuzzer == null)
        {
            bool free = state == GameState.Overworld && !engine.ScriptRunning && game.Flow.TrainerApproach == null && !game.StartMenu.IsActive;
            float limit = state is GameState.Overworld or GameState.Dialogue or GameState.Battle or GameState.Transition ? Softlock.Limit : 3 * Softlock.Limit;
            if (watch.Observe(Signature(), dt, watched: !free, limit))
                Report.Softlocks.Add($"{Softlock.Limit:0}+ seconds unchanged: {watch.Frozen}");
        }
    }

    private void OutOfRange(string what)
    {
        if (outOfRange.Add(what)) Report.OutOfRange.Add(what);
    }

    private string Signature()
    {
        var player = game.Player;
        var battle = game.Battle;
        var dialogue = game.Dialogue;
        return $"{game.State}|{game.Map.Name}|{player.GridX},{player.GridY}|{player.IsMoving}|{game.Story.Revision}|{game.ScriptNow}|"
            + $"{dialogue.IsCurrentLineComplete}:{dialogue.CurrentLine}|{battle?.CurrentMessage}|{battle?.HUD.MenuState}|{game.TransitionSeconds:0.0}";
    }
}
