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

        int boxHeight = 180;
        int boxWidth = screenWidth - 80;
        int boxX = 40;
        int boxY = screenHeight - boxHeight - 36;

        // Platinum double-border panel
        RenderHelper.DrawPlatinumPanel(boxX, boxY, boxWidth, boxHeight, Palette.UiPanelBg);

        // Speaker name tag
        if (!string.IsNullOrEmpty(currentSpeaker))
        {
            int tagWidth = Math.Max(180, Raylib.MeasureText(currentSpeaker, 24) + 40);
            int tagHeight = 44;
            int tagX = boxX + 28;
            int tagY = boxY - 32;
            RenderHelper.DrawPlatinumPanel(tagX, tagY, tagWidth, tagHeight, Palette.UiBackground);
            RenderHelper.DrawTextWithShadow(currentSpeaker, tagX + 18, tagY + 8, 24, Palette.UiAccent);
        }

        // Revealed text (Crisp Full HD font)
        string visibleText = currentLine[..charIndex];
        RenderHelper.DrawTextWithShadow(visibleText, boxX + 36, boxY + 36, 28, Palette.TextDark, Palette.TextShadow);

        // Blinking advance triangle
        if (IsCurrentLineComplete)
        {
            if ((int)(Raylib.GetTime() * 4) % 2 == 0)
            {
                int triX = boxX + boxWidth - 44;
                int triY = boxY + boxHeight - 38;
                Raylib.DrawTriangle(
                    new System.Numerics.Vector2(triX, triY),
                    new System.Numerics.Vector2(triX + 18, triY),
                    new System.Numerics.Vector2(triX + 9, triY + 16),
                    Palette.UiAccent);
            }
        }
    }
}
