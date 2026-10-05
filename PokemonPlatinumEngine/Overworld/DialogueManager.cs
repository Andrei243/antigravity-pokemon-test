using System;
using System.Collections.Generic;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;

namespace PokemonPlatinumEngine.Overworld;

public class DialogueManager
{
    private readonly Queue<string> lineQueue = new();
    private string currentLine = "";
    private string currentSpeaker = "";
    private int charIndex = 0;
    private float charProgress = 0f;
    private Action? onCompleteCallback;

    /// <summary>How fast lines are written out, in characters a second: the options' text speed.</summary>
    public float CharactersPerSecond { get; set; } = GameSettings.CharactersPerSecond(TextSpeed.Normal);

    // A question stays on the box, fully written, until it has its answer
    private bool question;

    public bool IsActive => currentLine.Length > 0 || lineQueue.Count > 0;
    public bool IsCurrentLineComplete => charIndex >= currentLine.Length;

    /// <summary>True once a question is written out and waits for its answer: the box stays as it is.</summary>
    public bool AwaitingAnswer => question && IsActive && IsCurrentLineComplete;

    /// <summary>True while the line shown is a question, written out or not.</summary>
    public bool IsQuestion => question && IsActive;

    /// <summary>The whole line being typed out, without the speaker's name.</summary>
    public string CurrentLine => currentLine;

    public void ShowDialogue(string speaker, IEnumerable<string> lines, Action? onComplete = null)
    {
        // Written lines name the player and the professor's assistant with {player} and {assistant}
        currentSpeaker = PlayerIdentity.Fill(speaker);
        question = false;
        lineQueue.Clear();
        foreach (var l in lines) lineQueue.Enqueue(l);
        onCompleteCallback = onComplete;
        AdvanceLine();
    }

    public void ShowDialogue(string speaker, string singleLine, Action? onComplete = null)
    {
        ShowDialogue(speaker, new[] { singleLine }, onComplete);
    }

    /// <summary>
    /// Shows a line that is a question: it is written out like any other and then stays, with no arrow to go on,
    /// until <see cref="Close"/>. Whoever asked shows the answers beside it and closes the box on the one picked.
    /// </summary>
    public void ShowQuestion(string speaker, string line)
    {
        ShowDialogue(speaker, new[] { line });
        question = true;
    }

    /// <summary>Takes the box away at once, whatever was on it. Nothing is called.</summary>
    public void Close()
    {
        question = false;
        lineQueue.Clear();
        currentLine = "";
        currentSpeaker = "";
        onCompleteCallback = null;
    }

    private void AdvanceLine()
    {
        if (lineQueue.Count > 0)
        {
            currentLine = PlayerIdentity.Fill(lineQueue.Dequeue());

            // The speaker's name is on the tag above the box, so a line written as "Barry: Hey!" drops the prefix
            string prefix = currentSpeaker + ": ";
            if (currentSpeaker.Length > 0 && currentLine.StartsWith(prefix, StringComparison.Ordinal)) currentLine = currentLine[prefix.Length..];
            charIndex = 0;
            charProgress = 0f;
            AudioManager.PlaySound("select");
        }
        else
        {
            currentLine = "";
            currentSpeaker = "";
            var cb = onCompleteCallback;
            onCompleteCallback = null;
            cb?.Invoke();
        }
    }

    public void Update(float dt)
    {
        if (!IsActive) return;

        if (!IsCurrentLineComplete)
        {
            Type(dt);
            if (InputManager.IsActionPressed(GameAction.Confirm) || InputManager.IsActionPressed(GameAction.Cancel))
            {
                charIndex = currentLine.Length;
            }
        }
        else if (!question)
        {
            if (InputManager.IsActionPressed(GameAction.Confirm))
            {
                AdvanceLine();
            }
        }
    }

    /// <summary>Writes out as much of the line as the time allows, at the same pace whatever the frame rate.</summary>
    public void Type(float dt)
    {
        if (IsCurrentLineComplete) return;
        charProgress += dt * CharactersPerSecond;
        int whole = (int)charProgress;
        charProgress -= whole;
        charIndex = Math.Min(charIndex + whole, currentLine.Length);
    }

    /// <summary>How much of the line shows so far, and who is speaking.</summary>
    public string VisibleText => currentLine[..charIndex];
    public string Speaker => currentSpeaker;

    /// <summary>Shows the whole line at once (the A button while it is being written).</summary>
    public void FinishLine() => charIndex = currentLine.Length;

    /// <summary>Goes on to the next line, or ends the talk after the last (the A button once a line is complete).</summary>
    public void Advance()
    {
        if (IsActive && IsCurrentLineComplete && !question) AdvanceLine();
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        UI.ModernUi.DrawDialogue(screenWidth, screenHeight, currentSpeaker, currentLine[..charIndex], IsCurrentLineComplete && !question);
    }
}
