using System;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.UI;

/// <summary>The help pages' tabs, in their order.</summary>
public enum HelpPage { Types, Controls, Notes }

/// <summary>
/// The help pages under OPTIONS (plan 12 · Q10; style guide, "Menu screens"): the type chart the game is played by,
/// the controls as the bindings have them, and a few notes of our own. Its state moves through methods that take no
/// input (<see cref="Move"/>, <see cref="Cancel"/>), so tests and the harness drive it; <see cref="Update"/> maps the
/// keys onto them and <c>ModernUi.Help</c> draws it.
/// </summary>
public sealed class HelpScreen
{
    public static readonly HelpPage[] Pages = Enum.GetValues<HelpPage>();

    /// <summary>The chart's types, attacking down its side and defending across its top.</summary>
    public static readonly PokemonType[] Types = Enum.GetValues<PokemonType>();

    /// <summary>The types' three-letter names over the chart's columns.</summary>
    public static string ShortName(PokemonType type) => type switch
    {
        PokemonType.Normal => "NOR",
        PokemonType.Fire => "FIR",
        PokemonType.Water => "WAT",
        PokemonType.Grass => "GRS",
        PokemonType.Electric => "ELE",
        PokemonType.Ice => "ICE",
        PokemonType.Fighting => "FIG",
        PokemonType.Poison => "PSN",
        PokemonType.Ground => "GRD",
        PokemonType.Flying => "FLY",
        PokemonType.Psychic => "PSY",
        PokemonType.Bug => "BUG",
        PokemonType.Rock => "RCK",
        PokemonType.Ghost => "GHO",
        PokemonType.Dragon => "DRA",
        PokemonType.Steel => "STL",
        PokemonType.Dark => "DRK",
        _ => "FAI"
    };

    /// <summary>The notes page: a few short paragraphs, each a title and its lines.</summary>
    public static readonly (string Title, string Text)[] Notes =
    {
        ("Getting about", "Hold the run button once you have the Running Shoes. Talk to people and read signs with the confirm button: many have something to give or to say about the road ahead."),
        ("In battle", "A move of a type the foe is weak to does twice the damage, and one its two types are both weak to four times. The TYPES page has every matchup; the move hints in the options can say it on the move's card."),
        ("The Pokétch", "Once you have it, put it on the screen and change its app with their buttons, and take it in hand to touch it: the arrows move a cursor over its buttons."),
        ("Saving", "Save from the menu whenever you like. A Pokémon Center heals your team for nothing, and the last one you visited is where you wake up after losing a battle.")
    };

    /// <summary>Keys the game reads beyond the actions: full screen and the sound.</summary>
    public static readonly (string Name, string Key)[] OtherKeys = { ("Full screen", "F11"), ("Sound on or off", "M") };

    public bool IsActive { get; private set; }
    public HelpPage Page { get; private set; }

    /// <summary>The cursor is on the tabs rather than in the chart.</summary>
    public bool OnTabs { get; private set; }

    /// <summary>The chart's cursor: the attacking type's row and the defending type's column.</summary>
    public int Attack { get; private set; }
    public int Defend { get; private set; }

    /// <summary>Opens on the chart, its cursor on Fire against Grass.</summary>
    public void Open()
    {
        IsActive = true;
        Page = HelpPage.Types;
        OnTabs = false;
        Attack = (int)PokemonType.Fire;
        Defend = (int)PokemonType.Grass;
    }

    public void Close() => IsActive = false;

    public void Draw(int sw, int sh)
    {
        if (IsActive) ModernUi.DrawHelp(this, sw, sh);
    }

    /// <summary>
    /// One step of the arrows: in the chart the cursor (up from its top row to the tabs, its columns wrapping
    /// round); on the tabs, or on a page without a cursor, left and right change page and down goes into the chart.
    /// </summary>
    public void Move(int dx, int dy)
    {
        if (Page == HelpPage.Types && !OnTabs)
        {
            if (dy < 0 && Attack == 0) OnTabs = true;
            else Attack = Math.Clamp(Attack + dy, 0, Types.Length - 1);
            Defend = (Defend + dx + Types.Length) % Types.Length;
            return;
        }
        if (dx != 0)
        {
            Page = Pages[(Array.IndexOf(Pages, Page) + dx + Pages.Length) % Pages.Length];
            OnTabs = true;
        }
        else if (dy > 0 && Page == HelpPage.Types) OnTabs = false;
    }

    /// <summary>B: back to the options.</summary>
    public void Cancel() => IsActive = false;

    public void Update()
    {
        if (!IsActive) return;
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right), dy = InputManager.Axis(GameAction.Up, GameAction.Down);
        if (dx != 0 || dy != 0)
        {
            Move(dx, dy);
            AudioManager.PlaySound("cursor");
        }
        else if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Menu))
        {
            Cancel();
            AudioManager.PlaySound("cancel");
        }
    }

    /// <summary>The chart's matchup of one type against another, by the rules of the game in progress.</summary>
    public static float Effectiveness(PokemonType attack, PokemonType defend) =>
        TypeChart.GetEffectiveness(attack, defend, null, Ruleset.Current);

    /// <summary>A cell of the chart as the battle's hint pill would have it.</summary>
    public static MoveHint Hint(PokemonType attack, PokemonType defend) => MoveHints.Of(Effectiveness(attack, defend));

    /// <summary>What the chart's cursor is on, in words: a headline and a line under it.</summary>
    public (string Headline, string Line) Describe()
    {
        var attack = Types[Attack];
        var defend = Types[Defend];
        return Hint(attack, defend) switch
        {
            MoveHint.SuperEffective => ("Super effective", $"A {attack} move against a {defend} Pokémon does twice the damage."),
            MoveHint.NotVeryEffective => ("Not very effective", $"A {attack} move against a {defend} Pokémon does half the damage."),
            MoveHint.NoEffect => ("No effect", $"A {attack} move does nothing at all to a {defend} Pokémon."),
            _ => ("A plain matchup", $"A {attack} move against a {defend} Pokémon does its usual damage.")
        };
    }
}
