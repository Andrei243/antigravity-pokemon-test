using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Power Watch's panel (plan 36 · TR8; style guide, "Menu screens", "The Power Watch"): today's free power first where
/// there is one, then each kind learned with the level to use it at (Left and Right) and its cost, the charge as ten pips,
/// and what is in force with its minutes. Confirm on a kind uses it from the charge; on today's power takes it for nothing.
/// The rules are <see cref="PowerState"/>'s; the steps take no input of their own, so tests and the harness drive it.
/// </summary>
public sealed class PowersScreen
{
    private const float AppearTime = 0.35f;

    public bool IsActive { get; private set; }

    public float Age { get; private set; }

    public PowerState Powers { get; private set; } = new();

    /// <summary>The day's number today's free power is worked out from.</summary>
    public uint Day { get; private set; }

    /// <summary>The run bars powers (plan 13's NO POWERS): every use is refused.</summary>
    public bool NoPowers { get; private set; }

    /// <summary>The row under the cursor: 0 is today's power where there is one, then the kinds learned.</summary>
    public int Cursor { get; private set; }

    /// <summary>The level each kind's row is set to, by the kind.</summary>
    private readonly Dictionary<PowerKind, int> levels = new();

    /// <summary>A line under the rows: why the last confirm did nothing, or what it put in force; null for none.</summary>
    public string? Message { get; private set; }

    /// <summary>Whether the last line is a refusal (drawn muted) rather than a power put in force.</summary>
    public bool Refused { get; private set; }

    /// <summary>Today's free power, or null where there is none to take.</summary>
    public (PowerKind Kind, int Level)? Free => NoPowers ? null : Powers.FreeToday(Day);

    /// <summary>The kinds learned, in the kinds' order: the rows after today's.</summary>
    public IReadOnlyList<PowerKind> Kinds => Powers.LearnedKinds;

    /// <summary>How many rows the panel has.</summary>
    public int RowCount => (Free != null ? 1 : 0) + Kinds.Count;

    /// <summary>Whether the row under the cursor is today's free power.</summary>
    public bool OnFree => Free != null && Cursor == 0;

    /// <summary>The kind of a row (null for today's).</summary>
    public PowerKind? KindAt(int row)
    {
        int at = row - (Free != null ? 1 : 0);
        return at >= 0 && at < Kinds.Count ? Kinds[at] : null;
    }

    /// <summary>The level a kind's row is set to: the highest learned unless moved.</summary>
    public int LevelOf(PowerKind kind) => levels.TryGetValue(kind, out int l) ? Math.Clamp(l, 1, Math.Max(1, Powers.Learned(kind))) : Math.Max(1, Powers.Learned(kind));

    public void Open(PowerState powers, uint day, bool noPowers)
    {
        Powers = powers;
        Day = day;
        NoPowers = noPowers;
        levels.Clear();
        Cursor = 0;
        Message = null;
        Refused = false;
        Age = 0f;
        IsActive = true;
        AudioManager.PlaySound("menu_open");
    }

    public void Close()
    {
        IsActive = false;
        AudioManager.PlaySound("menu_close");
    }

    /// <summary>Up and down the rows.</summary>
    public void Move(int dy)
    {
        if (dy == 0 || RowCount == 0) return;
        Cursor = UiNav.Wrap(Math.Min(Cursor, RowCount - 1), Math.Sign(dy), RowCount);
        Message = null;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>Left and right: the level a kind's row is set to, one to the level learned.</summary>
    public void Change(int dx)
    {
        if (dx == 0 || KindAt(Cursor) is not { } kind || Powers.Learned(kind) <= 1) return;
        levels[kind] = Math.Clamp(LevelOf(kind) + Math.Sign(dx), 1, Powers.Learned(kind));
        Message = null;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>Confirm: today's power taken, or the kind under the cursor used at its row's level, or why not.</summary>
    public void Confirm()
    {
        if (RowCount == 0) return;
        if (OnFree && Free is { } free)
        {
            Powers.TakeFree(Day, NoPowers);
            Say(Text.Of("powerWatch.started", ("power", PowerTable.NameOf(free.Kind)), ("level", free.Level)), refused: false);
            AudioManager.PlaySound("select");
            if (Cursor >= RowCount) Cursor = Math.Max(0, RowCount - 1);
            return;
        }
        if (KindAt(Cursor) is not { } kind) return;
        int level = LevelOf(kind);
        var why = Powers.WhyNot(kind, level, NoPowers);
        if (why != PowerRefusal.None)
        {
            string word = Words(why);
            Say(Text.Of($"powerWatch.why.{word}"), refused: true);
            AudioManager.PlaySound("cancel");
            return;
        }
        Powers.Use(kind, level, NoPowers);
        Say(Text.Of("powerWatch.started", ("power", PowerTable.NameOf(kind)), ("level", level)), refused: false);
        AudioManager.PlaySound("select");
    }

    private void Say(string line, bool refused)
    {
        Message = line;
        Refused = refused;
    }

    /// <summary>A refusal's key word in <c>powerWatch.why.*</c>.</summary>
    internal static string Words(PowerRefusal why) => why switch
    {
        PowerRefusal.NoPowers => "run",
        PowerRefusal.NotLearned => "learned",
        _ => "charge"
    };

    public void Cancel() => Close();

    public void Update(float dt)
    {
        if (!IsActive) return;
        Age += dt;
        int dy = InputManager.Axis(GameAction.Up, GameAction.Down);
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right);
        if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (dy != 0) Move(dy);
        else if (dx != 0) Change(dx);
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        ModernUi.DrawPowers(screenWidth, screenHeight, this, Math.Clamp(Age / AppearTime, 0f, 1f));
    }
}
