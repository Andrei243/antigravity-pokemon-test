using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where the Poffin Case is.</summary>
public enum CaseStep
{
    /// <summary>The case's Poffins, under a flavour's tab or all.</summary>
    List,
    /// <summary>What to do with the Poffin chosen: GIVE, TRASH, CANCEL.</summary>
    Actions,
    /// <summary>"Throw away the …?" with YES and NO.</summary>
    Trash,
    /// <summary>The team, to give the Poffin to one of them.</summary>
    Choose,
    /// <summary>The chosen Pokémon's condition, and whether to feed it.</summary>
    Condition,
    /// <summary>It eats, and how it takes the Poffin is said.</summary>
    Eating,
    /// <summary>Its condition after the Poffin, the change shown.</summary>
    After
}

/// <summary>
/// The Poffin Case (plan 06 · R14c), opened from the bag: the Poffins newest first under six tabs (SPICY, DRY, SWEET,
/// BITTER, SOUR: those with any of the flavour; ALL), each with its level, and the one under the cursor with its
/// flavours and smoothness; a Poffin given to a Pokémon of the team, which shows its condition first, eats it as its
/// nature takes it (<see cref="Poffins.Feed"/>) and shows the change; or thrown away. As the original's case
/// (<c>src/applications/poffin_case/</c>), in our own words. Its steps take no input of their own, so tests drive it.
/// </summary>
public sealed class PoffinCaseScreen
{
    private const float AppearTime = 0.35f;

    /// <summary>The tabs in their order: the five flavours, then ALL (the original's flavour filter starts on ALL).</summary>
    public static readonly Flavor?[] Tabs = { Flavor.Spicy, Flavor.Dry, Flavor.Sweet, Flavor.Bitter, Flavor.Sour, null };

    /// <summary>The rows the case's list shows: as many as its panel holds over the prompt (<see cref="ModernUi.CasePrompt"/>).</summary>
    public const int VisibleRows = 7;

    public static readonly string[] ActionNames = { "GIVE", "TRASH", "CANCEL" };

    public bool IsActive { get; private set; }
    public CaseStep Step { get; private set; }

    /// <summary>The tab chosen (an index of <see cref="Tabs"/>), and the case's places it lists.</summary>
    public int Tab { get; private set; } = Tabs.Length - 1;
    public IReadOnlyList<int> Listed { get; private set; } = Array.Empty<int>();
    public int Cursor { get; private set; }
    public int First { get; private set; }

    public int ActionIndex { get; private set; }

    /// <summary>"Throw away?": 0 YES, 1 NO.</summary>
    public int TrashChoice { get; private set; }

    /// <summary>The Pokémon of the team under the cursor, or being fed.</summary>
    public int Target { get; private set; }

    /// <summary>The Poffin chosen, and the Pokémon's condition before it ate (cool to tough, then the sheen).</summary>
    public Poffin? Chosen { get; private set; }
    public int[]? Before { get; private set; }

    /// <summary>How the Pokémon took the Poffin it ate.</summary>
    public PoffinTaste Taste { get; private set; }

    /// <summary>A line in place of the prompt until the next button.</summary>
    public string? Message { get; private set; }

    public float Age { get; private set; }
    public float StepAge { get; private set; }

    private PoffinCase poffinCase = new();
    private Party party = new();

    public void Open(PoffinCase into, Party team)
    {
        poffinCase = into;
        party = team;
        IsActive = true;
        Tab = Tabs.Length - 1;
        Cursor = First = 0;
        Age = 0f;
        Message = null;
        Chosen = null;
        Before = null;
        Go(CaseStep.List);
        Relist();
        AudioManager.PlaySound("menu_open");
    }

    public void Close()
    {
        IsActive = false;
        AudioManager.PlaySound("menu_close");
    }

    private void Go(CaseStep step)
    {
        Step = step;
        StepAge = 0f;
    }

    private void Relist()
    {
        Listed = poffinCase.Listed(Tabs[Tab]);
        Cursor = Math.Clamp(Cursor, 0, Math.Max(0, Listed.Count - 1));
        First = UiNav.Window(First, Cursor, Listed.Count, VisibleRows);
    }

    /// <summary>The Poffin under the cursor, and its place in the case.</summary>
    public Poffin? Current => Cursor < Listed.Count ? poffinCase.All[Listed[Cursor]] : null;

    /// <summary>The prompt at the foot of the screen.</summary>
    public string Prompt => Message ?? Step switch
    {
        CaseStep.Actions => $"Do what with the {Current?.Name}?",
        CaseStep.Trash => $"Throw away the {Current?.Name}?",
        CaseStep.Choose => $"Give the {Chosen?.Name} to which Pokémon?",
        CaseStep.Condition => $"Give the {Chosen?.Name} to {party.Members[Target].DisplayName}?",
        CaseStep.Eating => EatenLine(party.Members[Target].DisplayName, Taste),
        CaseStep.After => $"{party.Members[Target].DisplayName}'s condition went up.",
        _ => Listed.Count == 0 ? "There are no Poffins here." : "Choose a Poffin."
    };

    /// <summary>What is said as a Pokémon eats, by how it takes the Poffin (our own words on the original's three).</summary>
    public static string EatenLine(string name, PoffinTaste taste) => taste switch
    {
        PoffinTaste.Liked => $"{name} gobbled up the Poffin happily!",
        PoffinTaste.Disliked => $"{name} ate the Poffin, but pulled a face...",
        _ => $"{name} ate the Poffin."
    };

    // ---------------------------------------------------------------- the buttons

    /// <summary>Left and right on the list change the tab, wrapping round; each tab lists from its top.</summary>
    public void MoveTab(int step)
    {
        if (Step != CaseStep.List || step == 0) return;
        Tab = UiNav.Wrap(Tab, step, Tabs.Length);
        Cursor = First = 0;
        Message = null;
        Relist();
        AudioManager.PlaySound("page");
    }

    /// <summary>Up and down: the list, the actions, the team, YES and NO.</summary>
    public void MoveCursor(int step)
    {
        if (step == 0) return;
        Message = null;
        switch (Step)
        {
            case CaseStep.List when Listed.Count > 0:
                Cursor = UiNav.Wrap(Cursor, step, Listed.Count);
                First = UiNav.Window(First, Cursor, Listed.Count, VisibleRows);
                break;
            case CaseStep.Actions:
                ActionIndex = UiNav.Wrap(ActionIndex, step, ActionNames.Length);
                break;
            case CaseStep.Trash:
                TrashChoice = TrashChoice == 0 ? 1 : 0;
                break;
            case CaseStep.Choose when party.Count > 0:
                Target = UiNav.Wrap(Target, step, party.Count);
                break;
            default:
                return;
        }
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The team's cards are two columns: left and right move across them.</summary>
    public void MoveTarget(int dx, int dy)
    {
        if (Step != CaseStep.Choose || party.Count == 0 || (dx == 0 && dy == 0)) return;
        int next = UiNav.Grid(Target, party.Count, 2, dx, dy);
        if (next == Target) return;
        Target = next;
        AudioManager.PlaySound("cursor");
    }

    public void Confirm()
    {
        Message = null;
        switch (Step)
        {
            case CaseStep.List:
                if (Current == null) return;
                ActionIndex = 0;
                Go(CaseStep.Actions);
                AudioManager.PlaySound("select");
                break;
            case CaseStep.Actions:
                AudioManager.PlaySound("select");
                if (ActionIndex == 0)
                {
                    Chosen = Current;
                    Target = 0;
                    Go(CaseStep.Choose);
                }
                else if (ActionIndex == 1)
                {
                    TrashChoice = 1;
                    Go(CaseStep.Trash);
                }
                else Go(CaseStep.List);
                break;
            case CaseStep.Trash:
                if (TrashChoice == 0 && Current is { } thrown)
                {
                    poffinCase.RemoveAt(Listed[Cursor]);
                    Relist();
                    Message = $"The {thrown.Name} was thrown away.";
                    AudioManager.PlaySound("select");
                }
                else AudioManager.PlaySound("cancel");
                Go(CaseStep.List);
                break;
            case CaseStep.Choose:
                if (Target >= party.Count) return;
                AudioManager.PlaySound("select");
                AudioManager.PlayCry(party.Members[Target]);
                Go(CaseStep.Condition);
                break;
            case CaseStep.Condition:
                Feed();
                break;
            case CaseStep.Eating:
                Go(CaseStep.After);
                break;
            case CaseStep.After:
                Chosen = null;
                Before = null;
                Go(CaseStep.List);
                break;
        }
    }

    // TryFeedPoffin, then PoffinCase_UpdateMonContestStats: a Pokémon whose sheen is full won't eat; one that does
    // takes the Poffin out of the case
    private void Feed()
    {
        var p = party.Members[Target];
        if (Chosen == null || Current != Chosen) return;
        if (!Poffins.WouldEat(p))
        {
            Message = $"{p.DisplayName} won't eat any more...";
            AudioManager.PlaySound("error");
            return;
        }
        Before = new[] { p.Cool, p.Beauty, p.Cute, p.Smart, p.Tough, p.Sheen };
        Taste = Poffins.TasteFor(Chosen, p.Nature);
        Poffins.Feed(Chosen, p);
        poffinCase.RemoveAt(Listed[Cursor]);
        Relist();
        Go(CaseStep.Eating);
        AudioManager.PlaySound("stat_up");
    }

    public void Cancel()
    {
        Message = null;
        switch (Step)
        {
            case CaseStep.List:
                Close();
                return;
            case CaseStep.Actions:
            case CaseStep.Trash:
                Go(CaseStep.List);
                break;
            case CaseStep.Choose:
                Chosen = null;
                Go(CaseStep.List);
                break;
            case CaseStep.Condition:
                Go(CaseStep.Choose);
                break;
            case CaseStep.Eating:
            case CaseStep.After:
                Confirm();
                return;
        }
        AudioManager.PlaySound("cancel");
    }

    // ---------------------------------------------------------------- the keys and the picture

    public void Update(float dt)
    {
        if (!IsActive) return;
        Age += dt;
        StepAge += dt;
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right), dy = InputManager.Axis(GameAction.Up, GameAction.Down);
        if (Step == CaseStep.Choose && (dx != 0 || dy != 0)) MoveTarget(dx, dy);
        else if (Step == CaseStep.List && dx != 0) MoveTab(dx);
        else if (dy != 0) MoveCursor(dy);
        else if (Step == CaseStep.Trash && dx != 0) MoveCursor(dx);
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        ModernUi.DrawPoffinCase(screenWidth, screenHeight, this, poffinCase, party, Math.Clamp(Age / AppearTime, 0f, 1f));
    }
}
