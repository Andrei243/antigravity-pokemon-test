using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PokemonPlatinumEngine.UI.Kit;

/// <summary>The colour a stretch of a line is written in (style guide, "Text box markup").</summary>
public enum RunColour { Plain, Red, Blue, Name, Money }

/// <summary>A stretch of a line's plain text, <paramref name="Start"/> to <paramref name="End"/> (exclusive), in a colour.</summary>
public readonly record struct RunSpan(int Start, int End, RunColour Colour);

public enum TextEventKind { Pause, Speed, Sound }

/// <summary>What happens as the typing reaches <paramref name="At"/> (an index in the plain text).</summary>
public readonly record struct TextEvent(int At, TextEventKind Kind, float Amount, string Sound = "");

/// <summary>A line parsed: the text as it is read, the colours of its stretches and what happens while it is typed.</summary>
public sealed class RichLine
{
    public string Plain { get; }
    public IReadOnlyList<RunSpan> Spans { get; }
    public IReadOnlyList<TextEvent> Events { get; }

    public RichLine(string plain, IReadOnlyList<RunSpan> spans, IReadOnlyList<TextEvent> events)
    {
        Plain = plain;
        Spans = spans;
        Events = events;
    }

    /// <summary>The same line without its first <paramref name="count"/> characters (a speaker's "Name: " the tag already says).</summary>
    public RichLine Skip(int count)
    {
        if (count <= 0) return this;
        count = Math.Min(count, Plain.Length);
        var spans = new List<RunSpan>();
        foreach (var s in Spans)
            if (s.End > count) spans.Add(new RunSpan(Math.Max(s.Start, count) - count, s.End - count, s.Colour));
        var events = new List<TextEvent>();
        foreach (var e in Events) events.Add(e with { At = Math.Max(e.At - count, 0) });
        return new RichLine(Plain[count..], spans, events);
    }

    /// <summary>
    /// How long the line takes to type at a speed in characters a second: its characters at the speed in force, which
    /// <c>{speed N}</c> multiplies, and each <c>{pause S}</c> on top.
    /// </summary>
    public float TypingSeconds(float charactersPerSecond)
    {
        if (charactersPerSecond <= 0f) return 0f;
        float seconds = 0f, speed = 1f;
        int at = 0;
        foreach (var e in Events)
        {
            seconds += (e.At - at) / (charactersPerSecond * speed);
            at = e.At;
            if (e.Kind == TextEventKind.Pause) seconds += e.Amount;
            else if (e.Kind == TextEventKind.Speed) speed = e.Amount;
        }
        return seconds + (Plain.Length - at) / (charactersPerSecond * speed);
    }

    /// <summary>The colour of the character at an index.</summary>
    public RunColour ColourAt(int index)
    {
        foreach (var s in Spans) if (index >= s.Start && index < s.End) return s.Colour;
        return RunColour.Plain;
    }
}

/// <summary>
/// The text box's markup (plan 09 · L3; style guide, "Text box markup"), GPU-free. A line written with codes:
/// <c>{red:…}</c>, <c>{blue:…}</c> and <c>{name:…}</c> colour what is inside, <c>{money 300}</c> is an amount of money,
/// <c>{pause 0.4}</c> holds the typing, <c>{speed 2}</c> multiplies its pace from there on, <c>{br}</c> breaks the row and
/// <c>{sfx select}</c> plays a sound. Anything else in braces is no markup and stays as written (a placeholder some
/// other part fills). The markup lives in lines only: <see cref="Plain"/> strips it.
/// </summary>
public static class RichText
{
    private static readonly string[] Colours = { "red", "blue", "name" };

    private static RunColour ColourOf(string code) => code switch { "red" => RunColour.Red, "blue" => RunColour.Blue, _ => RunColour.Name };

    public static RichLine Parse(string line)
    {
        var plain = new StringBuilder();
        var spans = new List<RunSpan>();
        var events = new List<TextEvent>();
        int i = 0;
        while (i < line.Length)
        {
            if (line[i] == '{' && TryCode(line, i, out var code, out int end))
            {
                if (code.Colour is { } colour)
                {
                    int start = plain.Length;
                    plain.Append(code.Inside);
                    if (plain.Length > start) spans.Add(new RunSpan(start, plain.Length, colour));
                }
                else if (code.Name == "br") plain.Append('\n');
                else if (code.Name == "money")
                {
                    int start = plain.Length;
                    plain.Append('P').Append(code.Number.ToString("N0", CultureInfo.InvariantCulture));
                    spans.Add(new RunSpan(start, plain.Length, RunColour.Money));
                }
                else if (code.Name == "pause") events.Add(new TextEvent(plain.Length, TextEventKind.Pause, code.Amount));
                else if (code.Name == "speed") events.Add(new TextEvent(plain.Length, TextEventKind.Speed, code.Amount));
                else if (code.Name == "sfx") events.Add(new TextEvent(plain.Length, TextEventKind.Sound, 0f, code.Argument));
                i = end;
            }
            else plain.Append(line[i++]);
        }
        return new RichLine(plain.ToString(), spans, events);
    }

    /// <summary>The line as it reads, every code of the markup taken out.</summary>
    public static string Plain(string line) => line.Contains('{') ? Parse(line).Plain : line;

    /// <summary>
    /// What is wrong with the markup of a line: a code the markup owns (a colour, <c>pause</c>, <c>speed</c>, <c>sfx</c>,
    /// <c>br</c>, <c>money N</c>) written with a missing or bad argument, or never closed. Empty when it is sound.
    /// </summary>
    public static List<string> Problems(string line)
    {
        var found = new List<string>();
        int i = 0;
        while (i < line.Length && (i = line.IndexOf('{', i)) >= 0)
        {
            int close = line.IndexOf('}', i);
            if (close < 0) { found.Add("a '{' is never closed"); break; }
            string body = line[(i + 1)..close];
            string head = body.Split(' ', ':')[0];
            if (!TryCode(line, i, out _, out _))
            {
                if (Array.IndexOf(Colours, head) >= 0 && body.Contains(':')) found.Add($"{{{body}}} has no text to colour");
                else if (head is "pause" or "speed" || (head == "money" && body.Contains(' '))) found.Add($"{{{body}}} needs a good number after it");
                else if (head == "sfx") found.Add($"{{{body}}} needs a sound's name after it");
                else if (head == "br") found.Add($"{{{body}}}: a break takes no argument");
            }
            i = close + 1;
        }
        return found;
    }

    private readonly record struct Code(string Name, RunColour? Colour, string Inside, float Amount, int Number, string Argument);

    private static bool TryCode(string line, int open, out Code code, out int end)
    {
        code = default;
        end = open;
        int close = line.IndexOf('}', open);
        if (close < 0) return false;
        string body = line[(open + 1)..close];
        end = close + 1;
        if (body == "br") { code = new Code("br", null, "", 0, 0, ""); return true; }

        int colon = body.IndexOf(':');
        if (colon > 0)
        {
            string head = body[..colon];
            if (Array.IndexOf(Colours, head) >= 0 && colon < body.Length - 1)
            {
                code = new Code(head, ColourOf(head), body[(colon + 1)..], 0, 0, "");
                return true;
            }
            return false;
        }
        int space = body.IndexOf(' ');
        if (space <= 0) return false;
        string name = body[..space], arg = body[(space + 1)..].Trim();
        switch (name)
        {
            case "pause" or "speed":
                if (!float.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out float amount) || amount < 0f || (name == "speed" && amount <= 0f)) return false;
                code = new Code(name, null, "", amount, 0, "");
                return true;
            case "money":
                if (!int.TryParse(arg, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number) || number < 0) return false;
                code = new Code(name, null, "", 0, number, "");
                return true;
            case "sfx":
                if (arg.Length == 0 || arg.Contains(' ')) return false;
                code = new Code(name, null, "", 0, 0, arg);
                return true;
        }
        return false;
    }

    /// <summary>
    /// The rows a line is written in, each as pieces of one colour: <paramref name="rows"/> are the wrapped rows of
    /// <paramref name="text"/> (the visible part of the plain text), cut where a run begins or ends; the spans index
    /// that same text.
    /// </summary>
    public static List<List<(string Text, RunColour Colour)>> Pieces(string text, IReadOnlyList<RunSpan> spans, IReadOnlyList<string> rows)
    {
        var result = new List<List<(string, RunColour)>>();
        int pos = 0;
        foreach (string row in rows)
        {
            var pieces = new List<(string, RunColour)>();
            int from = 0;
            while (from < row.Length)
            {
                var colour = Colour(spans, pos + from);
                int to = from + 1;
                while (to < row.Length && Colour(spans, pos + to) == colour) to++;
                pieces.Add((row[from..to], colour));
                from = to;
            }
            result.Add(pieces);
            pos += row.Length;
            // The break between two rows is a space or a newline the wrap dropped
            if (pos < text.Length && (text[pos] == ' ' || text[pos] == '\n')) pos++;
        }
        return result;
    }

    private static RunColour Colour(IReadOnlyList<RunSpan> spans, int index)
    {
        foreach (var s in spans) if (index >= s.Start && index < s.End) return s.Colour;
        return RunColour.Plain;
    }
}
