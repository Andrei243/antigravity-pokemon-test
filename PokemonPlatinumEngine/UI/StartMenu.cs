using System;
using System.Collections.Generic;
using System.Linq;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>What the player picked from the start menu.</summary>
public enum StartMenuChoice { None, Pokedex, Pokemon, Bag, Trainer, Save, Options, SaveAndQuit, Quit }

/// <summary>
/// The field's start menu, in Platinum's order (Pokédex, Pokémon, Bag, the player's card, Save, Options, then
/// closing the menu) plus an entry to leave the game, which asks first and offers to save.
/// </summary>
public class StartMenu
{
    private enum Entry { Pokedex, Pokemon, Bag, Trainer, Save, Options, Close, Quit }

    private static readonly Entry[] Entries = Enum.GetValues<Entry>();

    /// <summary>The answers to "Quit the game?", in button order; the first (staying) is highlighted when it opens.</summary>
    public static readonly string[] QuitAnswers = { "KEEP PLAYING", "SAVE AND QUIT", "QUIT" };

    private readonly UiReveal reveal = new(0.2f, 0.12f);
    private readonly UiReveal prompt = new(0.18f, 0.1f);

    public int SelectedIndex { get; set; }
    public bool IsActive { get; private set; }

    /// <summary>True while "Quit the game?" is up.</summary>
    public bool AskingToQuit { get; private set; }
    public int QuitIndex { get; private set; }

    /// <summary>Shown on the entry that opens the Trainer Card, as in the games.</summary>
    public string PlayerName { get; set; } = "Trainer";

    public int EntryCount => Entries.Length;

    public void Open()
    {
        IsActive = true;
        SelectedIndex = 0;
        AskingToQuit = false;
        reveal.Open();
        AudioManager.PlaySound("select");
    }

    public void Close()
    {
        IsActive = false;
        AskingToQuit = false;
        reveal.Close();
        prompt.Close();
        AudioManager.PlaySound("cancel");
    }

    /// <summary>Puts the menu away at once, when a screen it opened takes over.</summary>
    public void Hide()
    {
        IsActive = false;
        AskingToQuit = false;
        reveal.Snap(false);
        prompt.Snap(false);
    }

    // ------------------------------------------------------------------ logic

    /// <summary>Up and down in the menu; left and right (or up and down) between the answers of the quit prompt.</summary>
    public void Move(int step)
    {
        if (!IsActive || step == 0) return;
        if (AskingToQuit) QuitIndex = Math.Clamp(QuitIndex + step, 0, QuitAnswers.Length - 1);
        else SelectedIndex = ((SelectedIndex + step) % Entries.Length + Entries.Length) % Entries.Length;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The A button: picks the highlighted entry or answer. Returns what the game should do, if anything.</summary>
    public StartMenuChoice Confirm()
    {
        if (!IsActive) return StartMenuChoice.None;
        AudioManager.PlaySound("select");

        if (AskingToQuit)
        {
            switch (QuitIndex)
            {
                case 1: return StartMenuChoice.SaveAndQuit;
                case 2: return StartMenuChoice.Quit;
                default:
                    AskingToQuit = false;
                    prompt.Close();
                    return StartMenuChoice.None;
            }
        }

        switch (Entries[SelectedIndex])
        {
            case Entry.Pokedex: return StartMenuChoice.Pokedex;
            case Entry.Pokemon: return StartMenuChoice.Pokemon;
            case Entry.Bag: return StartMenuChoice.Bag;
            case Entry.Trainer: return StartMenuChoice.Trainer;
            case Entry.Save: return StartMenuChoice.Save;
            case Entry.Options: return StartMenuChoice.Options;
            case Entry.Quit:
                AskingToQuit = true;
                QuitIndex = 0;
                prompt.Open();
                return StartMenuChoice.None;
            default:
                Close();
                return StartMenuChoice.None;
        }
    }

    /// <summary>The B button: backs out of the quit prompt, then closes the menu.</summary>
    public void Cancel()
    {
        if (!IsActive) return;
        if (AskingToQuit)
        {
            AskingToQuit = false;
            prompt.Close();
            AudioManager.PlaySound("cancel");
        }
        else Close();
    }

    /// <summary>Moves the slide-in and slide-out along; call every frame, open or not.</summary>
    public void Animate(float dt)
    {
        reveal.Update(dt);
        prompt.Update(dt);
    }

    public StartMenuChoice Update()
    {
        if (!IsActive) return StartMenuChoice.None;

        if (InputManager.IsActionPressed(GameAction.Up)) Move(-1);
        else if (InputManager.IsActionPressed(GameAction.Down)) Move(1);
        else if (AskingToQuit && InputManager.IsActionPressed(GameAction.Left)) Move(-1);
        else if (AskingToQuit && InputManager.IsActionPressed(GameAction.Right)) Move(1);
        else if (InputManager.IsActionPressed(GameAction.Cancel) || (!AskingToQuit && InputManager.IsActionPressed(GameAction.Menu))) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) return Confirm();
        return StartMenuChoice.None;
    }

    // ------------------------------------------------------------------ drawing

    private (string Label, UiIcon Icon, Color Color) Describe(Entry entry) => entry switch
    {
        Entry.Pokedex => ("POKÉDEX", UiIcon.Dex, ModernUi.Red),
        Entry.Pokemon => ("POKÉMON", UiIcon.Ball, ModernUi.Green),
        Entry.Bag => ("BAG", UiIcon.Bag, ModernUi.Gold),
        Entry.Trainer => (PlayerName.ToUpperInvariant(), UiIcon.Trainer, ModernUi.Blue),
        Entry.Save => ("SAVE", UiIcon.Save, new Color(126, 110, 222, 255)),
        Entry.Options => ("OPTIONS", UiIcon.Options, ModernUi.Muted),
        Entry.Close => ("CLOSE", UiIcon.Close, ModernUi.Frame),
        _ => ("QUIT GAME", UiIcon.Quit, new Color(196, 58, 84, 255))
    };

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!reveal.Visible) return;
        ModernUi.DrawStartMenu(screenWidth, Entries.Select(Describe).ToList(), SelectedIndex, reveal.Shown);

        if (!prompt.Visible) return;
        ModernUi.Prompt(screenWidth, screenHeight, "Quit the game?", "Anything since your last save will be lost unless you save first.",
            new[] { (QuitAnswers[0], ModernUi.Blue), (QuitAnswers[1], ModernUi.Green), (QuitAnswers[2], ModernUi.Red) },
            QuitIndex, prompt.Shown);
    }
}
