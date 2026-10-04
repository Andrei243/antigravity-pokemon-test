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

    public bool IsActive => currentLine.Length > 0 || lineQueue.Count > 0;
    public bool IsCurrentLineComplete => charIndex >= currentLine.Length;

    /// <summary>The whole line being typed out, without the speaker's name.</summary>
    public string CurrentLine => currentLine;

    public void ShowDialogue(string speaker, IEnumerable<string> lines, Action? onComplete = null)
    {
        // Written lines name the player and the professor's assistant with {player} and {assistant}
        currentSpeaker = PlayerIdentity.Fill(speaker);
        lineQueue.Clear();
        foreach (var l in lines) lineQueue.Enqueue(l);
        onCompleteCallback = onComplete;
        AdvanceLine();
    }

    public void ShowDialogue(string speaker, string singleLine, Action? onComplete = null)
    {
        ShowDialogue(speaker, new[] { singleLine }, onComplete);
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
        else
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
        if (IsActive && IsCurrentLineComplete) AdvanceLine();
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        UI.ModernUi.DrawDialogue(screenWidth, screenHeight, currentSpeaker, currentLine[..charIndex], IsCurrentLineComplete);
    }
}
