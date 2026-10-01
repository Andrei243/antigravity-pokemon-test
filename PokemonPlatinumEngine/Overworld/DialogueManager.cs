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
    private float charTimer = 0f;
    private const float CharSpeed = 0.02f;
    private Action? onCompleteCallback;

    public bool IsActive => currentLine.Length > 0 || lineQueue.Count > 0;
    public bool IsCurrentLineComplete => charIndex >= currentLine.Length;

    /// <summary>The whole line being typed out, without the speaker's name.</summary>
    public string CurrentLine => currentLine;

    public void ShowDialogue(string speaker, IEnumerable<string> lines, Action? onComplete = null)
    {
        currentSpeaker = speaker;
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
            currentLine = lineQueue.Dequeue();

            // The speaker's name is on the tag above the box, so a line written as "Barry: Hey!" drops the prefix
            string prefix = currentSpeaker + ": ";
            if (currentSpeaker.Length > 0 && currentLine.StartsWith(prefix, StringComparison.Ordinal)) currentLine = currentLine[prefix.Length..];
            charIndex = 0;
            charTimer = 0f;
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
            charTimer += dt;
            if (charTimer >= CharSpeed)
            {
                charTimer = 0f;
                charIndex = Math.Min(charIndex + 1, currentLine.Length);
            }

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

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        UI.ModernUi.DrawDialogue(screenWidth, screenHeight, currentSpeaker, currentLine[..charIndex], IsCurrentLineComplete);
    }
}
