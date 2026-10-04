using System;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.UI.Kit;

/// <summary>What a key of the name keyboard does.</summary>
public enum NameKey { Letter, Case, Delete, Done }

/// <summary>
/// Entering a name on the on-screen keyboard, as the games do it: four rows of ten keys (letters, a few signs
/// and the digits) and a bottom row of three wide keys for upper and lower case, deleting and OK. No drawing or
/// input: the cursor is moved and the key under it pressed, so tests drive it. The game's own keys are letters
/// (W A S D Z X), which is why a name isn't typed on the real keyboard.
/// </summary>
public sealed class NameEntry
{
    public const int Columns = 10, LetterRows = 4;

    /// <summary>The bottom row's three keys, and the first column each starts at (they are 3, 3 and 4 columns wide).</summary>
    public static readonly NameKey[] WideKeys = { NameKey.Case, NameKey.Delete, NameKey.Done };
    public static readonly int[] WideStarts = { 0, 3, 6 };

    private static readonly string[] Upper = { "ABCDEFGHIJ", "KLMNOPQRST", "UVWXYZ.-' ", "0123456789" };
    private static readonly string[] Lower = { "abcdefghij", "klmnopqrst", "uvwxyz.-' ", "0123456789" };

    public NameEntry(string? initial = null, int maxLength = PlayerIdentity.MaxNameLength)
    {
        MaxLength = maxLength;
        Text = PlayerIdentity.Clean(initial);
        if (Text.Length > maxLength) Text = Text[..maxLength];
        UpperCase = Text.Length == 0;
    }

    public int MaxLength { get; }
    public string Text { get; private set; }

    /// <summary>Where the cursor is: rows 0 to 3 are the keys, row 4 the wide keys.</summary>
    public int Row { get; private set; }
    public int Column { get; private set; }

    public bool UpperCase { get; private set; } = true;

    /// <summary>True once OK has been pressed.</summary>
    public bool Done { get; private set; }

    /// <summary>The characters on the keys of a row, as they stand now.</summary>
    public string RowKeys(int row) => (UpperCase ? Upper : Lower)[row];

    /// <summary>Which wide key the cursor is on, when it is on the bottom row.</summary>
    public int WideIndex => Column >= WideStarts[2] ? 2 : Column >= WideStarts[1] ? 1 : 0;

    /// <summary>What the key under the cursor is.</summary>
    public NameKey KeyUnderCursor => Row < LetterRows ? NameKey.Letter : WideKeys[WideIndex];

    /// <summary>One step of the cursor; rows and columns wrap round. On the bottom row a step sideways goes to the next wide key.</summary>
    public void Move(int dx, int dy)
    {
        if (Done) return;
        if (dy != 0) Row = UiNav.Wrap(Row, Math.Sign(dy), LetterRows + 1);
        if (dx == 0) return;
        if (Row < LetterRows) Column = UiNav.Wrap(Column, Math.Sign(dx), Columns);
        else Column = WideStarts[UiNav.Wrap(WideIndex, Math.Sign(dx), WideKeys.Length)];
    }

    /// <summary>The A button: presses the key under the cursor. Returns what the key was.</summary>
    public NameKey Press()
    {
        var key = KeyUnderCursor;
        if (Done) return key;
        switch (key)
        {
            case NameKey.Letter:
                Type(RowKeys(Row)[Column]);
                break;
            case NameKey.Case:
                UpperCase = !UpperCase;
                break;
            case NameKey.Delete:
                Backspace();
                break;
            default:
                Done = true;
                break;
        }
        return key;
    }

    /// <summary>
    /// Adds a character if there is room. A name doesn't begin with a space, and after its first capital the
    /// keyboard goes to lower case by itself. Once the name is full the cursor goes to OK.
    /// </summary>
    public bool Type(char c)
    {
        if (Done || Text.Length >= MaxLength || (c == ' ' && Text.Length == 0)) return false;
        Text += c;
        if (Text.Length == 1 && char.IsUpper(c)) UpperCase = false;
        if (Text.Length >= MaxLength) ToDone();
        return true;
    }

    /// <summary>The B button, or the delete key: takes the last character away. False when there was none.</summary>
    public bool Backspace()
    {
        if (Done || Text.Length == 0) return false;
        Text = Text[..^1];
        if (Text.Length == 0) UpperCase = true;
        return true;
    }

    /// <summary>The Start button: the cursor jumps to OK, as in the games.</summary>
    public void ToDone()
    {
        Row = LetterRows;
        Column = WideStarts[2];
    }

    /// <summary>The name as it will be kept: what was entered, or <paramref name="fallback"/> if nothing was.</summary>
    public string Result(string fallback) => PlayerIdentity.Clean(Text) is { Length: > 0 } name ? name : fallback;

    /// <summary>Opens the keyboard again on the same name (the answer to "is that right?" was no).</summary>
    public void Reopen() => Done = false;
}
