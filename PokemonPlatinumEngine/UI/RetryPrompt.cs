using System;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>An answer to the question after a battle lost (plan 12 · Q25).</summary>
public enum RetryChoice { None, TryAgain, GiveUp }

/// <summary>
/// The question a battle lost asks with the options' RETRY BATTLES row on (plan 12 · Q25; style guide, "Battle panels",
/// "TRY AGAIN and GIVE UP"): over the battle, which stands still under it, TRY AGAIN (highlighted) and GIVE UP. Its logic
/// takes no input (<see cref="Open"/>, <see cref="Move"/>, <see cref="Confirm"/>, <see cref="Cancel"/>), so tests and the
/// harness answer it.
/// </summary>
public sealed class RetryPrompt
{
    private static readonly RetryChoice[] Choices = { RetryChoice.TryAgain, RetryChoice.GiveUp };
    private readonly UiReveal reveal = new(0.18f, 0.1f);

    /// <summary>True while the question is up.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>The answer under the cursor: TRY AGAIN, the first, when it opens.</summary>
    public int Index { get; private set; }

    /// <summary>Whom the battle was against, as its line names them.</summary>
    public string Foe { get; private set; } = "";

    public bool Visible => reveal.Visible;

    public static string Label(RetryChoice choice) => Text.Of(choice == RetryChoice.GiveUp ? "retry.giveUp" : "retry.tryAgain");

    /// <summary>What the question says under its heading.</summary>
    public string Body => Foe.Length > 0 ? Text.Of("retry.body", ("foe", Foe)) : Text.Of("retry.bodyNoFoe");

    public void Open(string foe)
    {
        Foe = foe;
        Index = 0;
        IsOpen = true;
        reveal.Open();
        AudioManager.PlaySound("menu_open");
    }

    public void Move(int step)
    {
        if (!IsOpen || step == 0) return;
        Index = Math.Clamp(Index + step, 0, Choices.Length - 1);
        AudioManager.PlaySound("cursor");
    }

    public RetryChoice Confirm()
    {
        if (!IsOpen) return RetryChoice.None;
        var choice = Choices[Index];
        AudioManager.PlaySound("select");
        Shut();
        return choice;
    }

    /// <summary>The B button: GIVE UP.</summary>
    public RetryChoice Cancel()
    {
        if (!IsOpen) return RetryChoice.None;
        AudioManager.PlaySound("cancel");
        Shut();
        return RetryChoice.GiveUp;
    }

    private void Shut()
    {
        IsOpen = false;
        reveal.Close();
    }

    public void Animate(float dt) => reveal.Update(dt);

    /// <summary>The keys, while it is open: the answer given this frame, if any.</summary>
    public RetryChoice Update()
    {
        if (!IsOpen) return RetryChoice.None;
        if (InputManager.IsActionPressed(GameAction.Up) || InputManager.IsActionPressed(GameAction.Left)) Move(-1);
        else if (InputManager.IsActionPressed(GameAction.Down) || InputManager.IsActionPressed(GameAction.Right)) Move(1);
        else if (InputManager.IsActionPressed(GameAction.Cancel)) return Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) return Confirm();
        return RetryChoice.None;
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!reveal.Visible) return;
        ModernUi.Prompt(screenWidth, screenHeight, Text.Of("retry.heading"), Body,
            new[] { (Label(RetryChoice.TryAgain), ModernUi.Green), (Label(RetryChoice.GiveUp), ModernUi.Red) }, Index, reveal.Shown);
    }
}
