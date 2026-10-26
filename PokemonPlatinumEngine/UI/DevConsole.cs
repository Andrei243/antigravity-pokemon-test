using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.UI;

/// <summary>How a line of the console is shown: what was typed, what a command answered, a refusal, a script's trace.</summary>
internal enum ConsoleTone { Typed, Answer, Refused, Trace }

/// <summary>A line of the console's output.</summary>
internal readonly record struct ConsoleLine(string Text, ConsoleTone Tone);

/// <summary>
/// The developer's console (plan 16 · T6): a line being typed, the lines typed before it, and what was printed, with
/// the developer's commands (<see cref="DevCommands"/>, the table the harness's scenarios share) as its language.
/// Opened with the grave key in developer mode (<see cref="DevMode"/>); the engine reads the keys and hands them over
/// (<see cref="Type"/>, <see cref="Backspace"/>, <see cref="Submit"/>, <see cref="Older"/>, <see cref="Newer"/>), and
/// <c>ModernUi.Console</c> draws it. No drawing, no input; what it prints is for the developer and stays in English.
/// </summary>
internal sealed class DevConsole
{
    /// <summary>The most lines kept of the output, of the lines typed before, and of letters on the line.</summary>
    public const int MostLines = 300, MostHistory = 60, MostLength = 160;

    private readonly List<ConsoleLine> output = new();
    private readonly List<string> history = new();
    // Where Older and Newer have gone in the history: its count when on the line being typed
    private int recalled;
    private string draft = "";

    public bool IsOpen { get; private set; }

    /// <summary>The line being typed.</summary>
    public string Line { get; private set; } = "";

    public IReadOnlyList<ConsoleLine> Output => output;
    public IReadOnlyList<string> History => history;

    public void Open() => IsOpen = true;
    public void Close() => IsOpen = false;
    public void Toggle() => IsOpen = !IsOpen;

    /// <summary>A letter typed: anything but a control character and the grave that opens and shuts the console.</summary>
    public void Type(char c)
    {
        if (char.IsControl(c) || c == '`' || Line.Length >= MostLength) return;
        Line += c;
        recalled = history.Count;
    }

    public void Type(string text)
    {
        foreach (char c in text) Type(c);
    }

    public void Backspace()
    {
        if (Line.Length > 0) Line = Line[..^1];
    }

    /// <summary>The line typed before the one shown (up).</summary>
    public void Older()
    {
        if (history.Count == 0 || recalled == 0) return;
        if (recalled == history.Count) draft = Line;
        Line = history[--recalled];
    }

    /// <summary>The line typed after the one shown (down), and at the end the line that was being typed.</summary>
    public void Newer()
    {
        if (recalled >= history.Count) return;
        recalled++;
        Line = recalled == history.Count ? draft : history[recalled];
    }

    /// <summary>Prints a line, or several (split at line breaks), keeping the last <see cref="MostLines"/>.</summary>
    public void Print(string text, ConsoleTone tone = ConsoleTone.Answer)
    {
        foreach (string line in text.Replace("\r", "").Split('\n')) output.Add(new ConsoleLine(line, tone));
        if (output.Count > MostLines) output.RemoveRange(0, output.Count - MostLines);
    }

    public void Clear() => output.Clear();

    /// <summary>Runs the line being typed and empties it (Enter).</summary>
    public DevResult Submit(GameEngine engine)
    {
        string line = Line;
        Line = "";
        draft = "";
        return Run(engine, line);
    }

    /// <summary>
    /// Runs a line: it is printed as typed and kept in the history, read into words (a word in double quotes kept
    /// whole), and run as the command its first word names (<see cref="DevCommands.Run"/>), whose answer is printed.
    /// </summary>
    public DevResult Run(GameEngine engine, string line)
    {
        line = line.Trim();
        if (line.Length == 0) return DevResult.Done("");
        Print("> " + line, ConsoleTone.Typed);
        if (history.Count == 0 || history[^1] != line) history.Add(line);
        if (history.Count > MostHistory) history.RemoveAt(0);
        recalled = history.Count;

        DevResult result;
        if (DevCommands.Split(line) is not { Count: > 0 } words) result = DevResult.Refused("a quote is never closed");
        else result = DevCommands.Run(engine, words[0], words.Skip(1).ToList());
        if (result.Text.Length > 0) Print(result.Text, result.Ok ? ConsoleTone.Answer : ConsoleTone.Refused);
        return result;
    }
}
