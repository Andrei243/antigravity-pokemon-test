using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Pokéblock Case (plan 21 · H11), opened from the bag or the item button: the Pokéblocks in their places, each
/// with its level and feel, the one under the cursor with its five flavours; one given to a Pokémon of the team, which
/// shows its condition first, eats it as its nature takes it (<see cref="Pokeblocks.Feed"/>) and shows the change; or
/// thrown away. As Emerald's case (<c>src/pokeblock.c</c>), in our own words, on the Poffin Case's steps
/// (<see cref="CaseStep"/>). Its steps take no input of their own, so tests drive it.
/// </summary>
public sealed class PokeblockCaseScreen
{
    private const float AppearTime = 0.35f;

    /// <summary>The rows the case's list shows.</summary>
    public const int VisibleRows = 8;

    public static string[] ActionNames => new[] { Text.Of("pokeblockCase.action.give"), Text.Of("pokeblockCase.action.toss"), Text.Of("pokeblockCase.action.cancel") };

    public bool IsActive { get; private set; }
    public CaseStep Step { get; private set; }

    /// <summary>The Pokéblocks listed (with their places in the case), and the cursor and the first row shown.</summary>
    public IReadOnlyList<(int Slot, Pokeblock Block)> Listed { get; private set; } = Array.Empty<(int, Pokeblock)>();
    public int Cursor { get; private set; }
    public int First { get; private set; }
    public int ActionIndex { get; private set; }

    /// <summary>"Throw away?": 0 YES, 1 NO.</summary>
    public int TrashChoice { get; private set; }

    /// <summary>The Pokémon of the team under the cursor, or being fed.</summary>
    public int Target { get; private set; }

    /// <summary>The Pokéblock chosen, the Pokémon's condition before it ate, and how it took it.</summary>
    public Pokeblock? Chosen { get; private set; }
    public int[]? Before { get; private set; }
    public PoffinTaste Taste { get; private set; }

    /// <summary>A line in place of the prompt until the next button.</summary>
    public string? Message { get; private set; }

    public float Age { get; private set; }
    public float StepAge { get; private set; }

    private PokeblockCase blocks = new();
    private Party party = new();

    public void Open(PokeblockCase into, Party team)
    {
        blocks = into;
        party = team;
        IsActive = true;
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
        Listed = blocks.Listed();
        Cursor = Math.Clamp(Cursor, 0, Math.Max(0, Listed.Count - 1));
        First = UiNav.Window(First, Cursor, Listed.Count, VisibleRows);
    }

    public Pokeblock? Current => Cursor < Listed.Count ? Listed[Cursor].Block : null;
    public PokeblockCase Case => blocks;
    public Party Team => party;

    /// <summary>The prompt at the foot of the screen.</summary>
    public string Prompt => Message ?? Step switch
    {
        CaseStep.Actions => Text.Of("pokeblockCase.prompt.doWhat", ("block", Current?.Name)),
        CaseStep.Trash => Text.Of("pokeblockCase.prompt.throwAway", ("block", Current?.Name)),
        CaseStep.Choose => Text.Of("pokeblockCase.prompt.giveWhich", ("block", Chosen?.Name)),
        CaseStep.Condition => Text.Of("pokeblockCase.prompt.giveTo", ("block", Chosen?.Name), ("name", party.Members[Target].DisplayName)),
        CaseStep.Eating => EatenLine(party.Members[Target].DisplayName, Chosen?.Name ?? "", Taste),
        CaseStep.After => Text.Of("pokeblockCase.prompt.wentUp", ("name", party.Members[Target].DisplayName)),
        _ => Text.Of(Listed.Count == 0 ? "pokeblockCase.prompt.none" : "pokeblockCase.prompt.choose")
    };

    /// <summary>What is said as a Pokémon eats, by how it takes the Pokéblock (our own words on Emerald's three).</summary>
    public static string EatenLine(string name, string block, PoffinTaste taste) => taste switch
    {
        PoffinTaste.Liked => Text.Of("pokeblockCase.eaten.liked", ("name", name), ("block", block)),
        PoffinTaste.Disliked => Text.Of("pokeblockCase.eaten.disliked", ("name", name), ("block", block)),
        _ => Text.Of("pokeblockCase.eaten.plain", ("name", name), ("block", block))
    };

    // ---------------------------------------------------------------- the buttons

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
                    blocks.RemoveAt(Listed[Cursor].Slot);
                    Relist();
                    Message = Text.Of("pokeblockCase.thrown", ("block", thrown.Name));
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

    // A Pokémon whose sheen is full won't eat; one that does takes the Pokéblock out of the case
    private void Feed()
    {
        var p = party.Members[Target];
        if (Chosen == null || Current != Chosen) return;
        if (!Pokeblocks.WouldEat(p))
        {
            Message = Text.Of("pokeblockCase.full", ("name", p.DisplayName));
            AudioManager.PlaySound("error");
            return;
        }
        Before = new[] { p.Cool, p.Beauty, p.Cute, p.Smart, p.Tough, p.Sheen };
        Taste = Pokeblocks.TasteFor(p.Nature, Chosen);
        Pokeblocks.Feed(Chosen, p);
        blocks.RemoveAt(Listed[Cursor].Slot);
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
        else if (dy != 0) MoveCursor(dy);
        else if (Step == CaseStep.Trash && dx != 0) MoveCursor(dx);
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        ModernUi.DrawPokeblockCase(screenWidth, screenHeight, this, Math.Clamp(Age / AppearTime, 0f, 1f));
    }
}
