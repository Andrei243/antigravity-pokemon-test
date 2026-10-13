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
    public static string ShortName(PokemonType type) => Text.Of($"help.short.{type}");

    /// <summary>The notes page: a few short paragraphs, each a title and its lines.</summary>
    public static (string Title, string Text)[] Notes => new[]
    {
        (Text.Of("help.note.about.title"), Text.Of("help.note.about.text")),
        (Text.Of("help.note.battle.title"), Text.Of("help.note.battle.text")),
        (Text.Of("help.note.poketch.title"), Text.Of("help.note.poketch.text")),
        (Text.Of("help.note.saving.title"), Text.Of("help.note.saving.text"))
    };

    /// <summary>Keys the game reads beyond the actions: full screen and the sound.</summary>
    public static (string Name, string Key)[] OtherKeys => new[] { (Text.Of("help.key.fullScreen"), "F11"), (Text.Of("help.key.sound"), "M") };

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
        string matchup = Hint(attack, defend) switch
        {
            MoveHint.SuperEffective => "super",
            MoveHint.NotVeryEffective => "half",
            MoveHint.NoEffect => "none",
            _ => "plain"
        };
        return (Text.Of($"help.matchup.{matchup}.headline"),
            Text.Of($"help.matchup.{matchup}.line", ("attack", Words.Type(attack)), ("defend", Words.Type(defend))));
    }
}
