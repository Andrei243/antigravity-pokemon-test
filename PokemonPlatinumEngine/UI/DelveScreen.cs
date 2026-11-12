using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Delve;

namespace PokemonPlatinumEngine.UI;

/// <summary>Which panel of the delve's screen has the buttons.</summary>
public enum DelveMenu
{
    /// <summary>The floor: a way steps, Confirm strikes the way faced.</summary>
    Field,
    /// <summary>The leader's moves, used the way faced.</summary>
    Moves,
    /// <summary>The run's bag.</summary>
    Bag,
    /// <summary>Who an item of the bag is given to.</summary>
    BagTarget,
    /// <summary>A resident knocked out asks to join: YES or NO.</summary>
    Asking
}

/// <summary>
/// The delve on screen (plan 44 · SD8; style guide, "The delve"): the run's panels and its log, GPU-free. A turn is
/// worked out at once by the rules (<see cref="DelveRun.Act"/>), and its events are played a line at a time
/// (<see cref="LineSeconds"/>) before the next action is read, as a battle reads its log. Its logic takes no input:
/// <see cref="Steer"/>, <see cref="Confirm"/>, <see cref="Cancel"/>, <see cref="OpenMoves"/>, <see cref="OpenBag"/>,
/// <see cref="Wait"/>, <see cref="Advance"/>; what the rules are to do comes out of <see cref="TakeAction"/>.
/// </summary>
public sealed class DelveScreen
{
    /// <summary>How long a line of the log is shown before the next one comes.</summary>
    public const float LineSeconds = 0.28f;

    /// <summary>How many lines of the log are shown.</summary>
    public const int LogShown = 4;

    public DelveRun? Run { get; private set; }
    public DelveMenu Menu { get; private set; }
    public int Cursor { get; private set; }

    /// <summary>The item chosen to give in <see cref="DelveMenu.BagTarget"/>.</summary>
    public string? Chosen { get; private set; }

    /// <summary>The lines shown, the newest last.</summary>
    public List<string> Log { get; } = new();

    /// <summary>The events of the last turns still to be shown, and the one being shown.</summary>
    private readonly Queue<DelveEvent> pending = new();
    public DelveEvent? Showing { get; private set; }
    private float lineAge;
    private int seen;
    private DelveAction? action;

    /// <summary>Why the last action was refused, until the next is taken (the screen says it).</summary>
    public DelveRefusal? Refused { get; private set; }

    public bool IsActive => Run != null;

    /// <summary>Whether lines of the last turn are still being shown (nothing is read meanwhile).</summary>
    public bool Playing => Showing != null || pending.Count > 0;

    /// <summary>The run is over and every line of it has been shown.</summary>
    public bool Ended => Run is { Outcome: not DelveOutcome.Going } && !Playing;

    public void Open(DelveRun run)
    {
        Run = run;
        Menu = DelveMenu.Field;
        Cursor = 0;
        Chosen = null;
        Log.Clear();
        pending.Clear();
        Showing = null;
        seen = 0;
        action = null;
        Refused = null;
        Feed();
    }

    public void Close()
    {
        Run = null;
        pending.Clear();
        Showing = null;
        action = null;
    }

    /// <summary>The run's distinct items, in the order the bag shows them.</summary>
    public IReadOnlyList<string> BagItems => Run?.Bag.Distinct().OrderBy(i => i, StringComparer.Ordinal).ToList() ?? new List<string>();

    /// <summary>How many of an item the bag holds.</summary>
    public int CountOf(string item) => Run?.Bag.Count(i => i == item) ?? 0;

    /// <summary>The run's events not yet queued are queued.</summary>
    private void Feed()
    {
        if (Run == null) return;
        for (; seen < Run.Events.Count; seen++)
            if (Run.Events[seen].Kind != DelveEventKind.Stepped) pending.Enqueue(Run.Events[seen]);
    }

    /// <summary>A frame: the next line of the log comes once the last has been shown its time.</summary>
    public void Advance(float dt)
    {
        if (Run == null) return;
        if (Showing == null)
        {
            if (pending.Count == 0) return;
            Showing = pending.Dequeue();
            lineAge = 0f;
            Say(Showing);
        }
        lineAge += dt;
        if (lineAge >= LineSeconds) Showing = null;
        if (!Playing && Run.Asking != null && Menu != DelveMenu.Asking)
        {
            Menu = DelveMenu.Asking;
            Cursor = 0;
        }
    }

    /// <summary>Every line still to come shown at once (a press while they play).</summary>
    public void Hurry()
    {
        while (Showing != null || pending.Count > 0)
        {
            if (Showing == null) Say(Showing = pending.Dequeue());
            Showing = null;
        }
        if (Run?.Asking != null)
        {
            Menu = DelveMenu.Asking;
            Cursor = 0;
        }
    }

    private void Say(DelveEvent e)
    {
        Log.Add(LineOf(e));
        while (Log.Count > LogShown) Log.RemoveAt(0);
    }

    /// <summary>A way pressed: on the floor a step (or, with <paramref name="turnOnly"/>, a turn on the spot); in a list, the cursor.</summary>
    public void Steer(Direction way, bool turnOnly = false)
    {
        if (Run == null || Playing) return;
        switch (Menu)
        {
            case DelveMenu.Field:
                if (turnOnly) Run.Face(way);
                else action = DelveAction.Step(way);
                break;
            case DelveMenu.Moves:
                Cursor = Wrap(Cursor + Delta(way), Run.Leader.Pokemon.Moves.Count);
                break;
            case DelveMenu.Bag:
                Cursor = Wrap(Cursor + Delta(way), BagItems.Count);
                break;
            case DelveMenu.BagTarget:
                Cursor = Wrap(Cursor + Delta(way), Run.Team.Count);
                break;
            case DelveMenu.Asking:
                Cursor = Wrap(Cursor + Delta(way), 2);
                break;
        }
    }

    private static int Delta(Direction way) => way is Direction.Up or Direction.Left ? -1 : 1;
    private static int Wrap(int i, int n) => n <= 0 ? 0 : ((i % n) + n) % n;

    /// <summary>Confirm: on the floor a strike the way faced; in a list, its row.</summary>
    public void Confirm()
    {
        if (Run == null) return;
        if (Playing)
        {
            Hurry();
            return;
        }
        switch (Menu)
        {
            case DelveMenu.Field:
                action = DelveAction.Strike(Run.Leader.Facing);
                break;
            case DelveMenu.Moves:
                action = DelveAction.UseMove(Cursor, Run.Leader.Facing);
                break;
            case DelveMenu.Bag:
                if (BagItems.Count == 0) return;
                Chosen = BagItems[Math.Min(Cursor, BagItems.Count - 1)];
                // An item goes to the leader at once while nobody else is on the team
                if (Run.Team.Count == 1) action = DelveAction.Use(Chosen, 0);
                else
                {
                    Menu = DelveMenu.BagTarget;
                    Cursor = 0;
                }
                break;
            case DelveMenu.BagTarget:
                action = DelveAction.Use(Chosen!, Cursor);
                break;
            case DelveMenu.Asking:
                action = DelveAction.Answer(Cursor == 0);
                break;
        }
    }

    /// <summary>Back: a list closes to the floor (a recruit's question is answered no).</summary>
    public void Cancel()
    {
        if (Run == null || Playing) return;
        if (Menu == DelveMenu.Asking) action = DelveAction.Answer(false);
        else if (Menu == DelveMenu.BagTarget) { Menu = DelveMenu.Bag; Cursor = 0; }
        else { Menu = DelveMenu.Field; Cursor = 0; }
    }

    public void OpenMoves()
    {
        if (Run == null || Playing || Menu == DelveMenu.Asking) return;
        Menu = DelveMenu.Moves;
        Cursor = 0;
    }

    public void OpenBag()
    {
        if (Run == null || Playing || Menu == DelveMenu.Asking) return;
        Menu = DelveMenu.Bag;
        Cursor = 0;
    }

    /// <summary>A turn passed on the spot.</summary>
    public void Wait()
    {
        if (Run == null || Playing || Menu != DelveMenu.Field) return;
        action = DelveAction.Wait;
    }

    /// <summary>The action the buttons asked for, once: the engine hands it to <see cref="Act"/>.</summary>
    public DelveAction? TakeAction()
    {
        var a = action;
        action = null;
        return a;
    }

    /// <summary>
    /// An action handed to the rules: its refusal kept for the screen, or its turn's events queued and the lists closed.
    /// </summary>
    public DelveRefusal? Act(DelveAction a)
    {
        if (Run == null) return DelveRefusal.NotNow;
        var refused = Run.Act(a);
        Refused = refused;
        if (refused != null)
        {
            // A step into a wall is said by nothing but the leader turning that way
            if (a.Kind == DelveActionKind.Step && refused == DelveRefusal.Blocked) Run.Face(a.Way);
            return refused;
        }
        if (Menu != DelveMenu.Field)
        {
            Menu = DelveMenu.Field;
            Cursor = 0;
        }
        Feed();
        return null;
    }

    /// <summary>A line of the log for an event: words of <c>delve.event.*</c>, the names in the language chosen.</summary>
    public static string LineOf(DelveEvent e)
    {
        string who = LocalNames.Species(e.Who);
        string target = e.Target != null ? LocalNames.Species(e.Target) : "";
        return Text.Of($"delve.event.{e.Kind}", ("who", who), ("target", target), ("what", WhatOf(e)), ("amount", e.Amount.ToString()));
    }

    /// <summary>What an event names, in words: a move, an item, a condition, a stat or a snare.</summary>
    public static string WhatOf(DelveEvent e)
    {
        if (e.What is not { } what) return "";
        if (Enum.TryParse<StatusCondition>(what, out var status) && !int.TryParse(what, out _)) return Words.Status(status);
        if (Enum.TryParse<StatType>(what, out var stat) && !int.TryParse(what, out _)) return Words.Stat(stat);
        if (Enum.TryParse<SnareKind>(what, out var snare) && !int.TryParse(what, out _)) return Text.Of($"delve.snare.{snare}");
        if (what == DelveTurns.FlinchWord) return Text.Of("delve.flinch");
        if (MoveDatabase.Has(what)) return LocalNames.Move(what);
        return LocalNames.Item(what);
    }
}
