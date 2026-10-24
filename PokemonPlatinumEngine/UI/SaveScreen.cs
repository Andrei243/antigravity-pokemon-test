using System;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// Saving, as a question over the field: the save as it will be (the summary the title's CONTINUE card shows)
/// and SAVE or CANCEL under it. Once saved it says so for a moment and goes. Its logic takes no input
/// (<see cref="Move"/>, <see cref="Confirm"/>, <see cref="Cancel"/>, <see cref="Advance"/>), so tests and the
/// harness drive it; the game does the saving when <see cref="TakeRequest"/> says so.
/// </summary>
public class SaveScreen
{
    /// <summary>How long "saved" stays up before the panel goes by itself.</summary>
    public const float SavedTime = 1.3f;

    public static string[] Answers => new[] { Text.Of("saving.answer.save"), Text.Of("saving.answer.cancel") };

    private readonly UiReveal reveal = new(0.2f, 0.12f);
    private bool requested;
    private float savedAge;

    public bool IsActive { get; private set; }

    /// <summary>True once the game has been saved and the panel is saying so.</summary>
    public bool Saved { get; private set; }

    /// <summary>The answer the cursor is on: SAVE first, since nothing is lost by saving.</summary>
    public int AnswerIndex { get; private set; }

    /// <summary>The line that says what to do next, shown under the summary (plan 12 · Q14); empty for none. Set by the engine as the panel opens.</summary>
    public string Objective { get; set; } = "";

    /// <summary>What the panel shows: the game as it is being saved.</summary>
    public SaveData? Summary { get; private set; }

    /// <summary>
    /// The slot the game is saved in (plan 12 · Q2), which the panel names: the one it was continued from, or the
    /// empty one a new game took.
    /// </summary>
    public int Slot { get; private set; } = 1;

    /// <summary>The panel's question: it names the slot.</summary>
    public static string Question(int slot) => Text.Of("saving.question", ("slot", slot));

    public void Open(SaveData summary, int slot = 1)
    {
        Slot = slot;
        IsActive = true;
        Saved = false;
        requested = false;
        AnswerIndex = 0;
        savedAge = 0f;
        Summary = summary;
        reveal.Open();
        AudioManager.PlaySound("select");
    }

    private void Close()
    {
        IsActive = false;
        reveal.Close();
    }

    public void Move(int step)
    {
        if (!IsActive || Saved || step == 0) return;
        AnswerIndex = Math.Clamp(AnswerIndex + step, 0, Answers.Length - 1);
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The A button: saves, or backs out; once it says "saved", any button sends it away.</summary>
    public void Confirm()
    {
        if (!IsActive) return;
        if (Saved || AnswerIndex != 0)
        {
            Cancel();
            return;
        }
        requested = true;
    }

    public void Cancel()
    {
        if (!IsActive) return;
        if (!Saved) AudioManager.PlaySound("cancel");
        Close();
    }

    /// <summary>True, once, when the player has asked for the game to be saved: the game saves and calls <see cref="MarkSaved"/>.</summary>
    public bool TakeRequest()
    {
        bool taken = requested;
        requested = false;
        return taken;
    }

    public void MarkSaved()
    {
        Saved = true;
        savedAge = 0f;
        AudioManager.PlaySound("save");
    }

    /// <summary>Runs the panel's slide and the moment it stays up after saving.</summary>
    public void Advance(float dt)
    {
        reveal.Update(dt);
        if (!IsActive || !Saved) return;
        savedAge += dt;
        if (savedAge >= SavedTime) Close();
    }

    /// <summary>True while any of the panel is still on screen (it slides away after it closes).</summary>
    public bool Visible => reveal.Visible;

    public void Update(float dt)
    {
        Advance(dt);
        if (!IsActive) return;
        if (InputManager.IsActionPressed(GameAction.Left) || InputManager.IsActionPressed(GameAction.Up)) Move(-1);
        else if (InputManager.IsActionPressed(GameAction.Right) || InputManager.IsActionPressed(GameAction.Down)) Move(1);
        else if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Menu)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!reveal.Visible || Summary == null) return;
        ModernUi.DrawSave(screenWidth, screenHeight, Summary, Slot, AnswerIndex, Saved, reveal.Shown, Objective);
    }
}
