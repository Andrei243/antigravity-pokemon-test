using System.Collections.Generic;

namespace PokemonPlatinumEngine.UI.Kit;

/// <summary>
/// The measures of the field's text box, in layout units (style guide, "Field menus and notices"): the panel near the
/// bottom of the screen that lines of dialogue are written in, and the panel of a question's answers that stands on
/// it. No drawing: <c>ModernUi.DrawDialogue</c> and <c>DrawChoices</c> draw by these numbers, and whoever asks whether
/// a written line fits (<c>tools/ContentCheck</c>, plan 16 · T8) reads the same ones through <see cref="TextMetrics"/>.
/// <para>
/// A line is written whole into the box: it is wrapped to the box's width and nothing scrolls or pages, so a line
/// that wraps to more rows than <see cref="Rows"/> runs out under the panel and off the screen.
/// </para>
/// </summary>
public static class DialogueBox
{
    /// <summary>The panel: its distance from the screen's sides and from its bottom, and its height.</summary>
    public const float Side = 96, Foot = 40, Height = 222;

    /// <summary>Where the words begin in the panel, and how much of its width they leave free (the advance arrow's corner).</summary>
    public const float TextLeft = 60, TextTop = 58, TextMargins = 170;

    /// <summary>The words' size, the distance from one row of them to the next, and their weight.</summary>
    public const float TextSize = 40, RowHeight = 58;
    public const UiWeight TextWeight = UiWeight.ExtraBold;

    /// <summary>How many rows of words the panel holds: the last one that still ends inside it.</summary>
    public const int Rows = (int)((Height - TextTop - TextSize) / RowHeight) + 1;

    // Under the options' large text (plan 12 · Q8; style guide, "Typography"): bigger words on rows further apart, in a
    // box tall enough for a fourth row, so a line written to fill Normal's three rows still ends inside it

    /// <summary>The words' size, the distance between rows and the panel's height under large text.</summary>
    public const float LargeTextSize = 46, LargeRowHeight = 62, LargeHeight = 300;

    /// <summary>How many rows of words the panel holds under large text.</summary>
    public const int LargeRows = (int)((LargeHeight - TextTop - LargeTextSize) / LargeRowHeight) + 1;

    /// <summary>The words' size, the row height, the panel's height and its rows as the options have them now.</summary>
    public static float SizeNow => Core.Seeing.LargeText ? LargeTextSize : TextSize;
    public static float RowHeightNow => Core.Seeing.LargeText ? LargeRowHeight : RowHeight;
    public static float HeightNow => Core.Seeing.LargeText ? LargeHeight : Height;
    public static int RowsNow => Core.Seeing.LargeText ? LargeRows : Rows;

    /// <summary>A line as the box breaks it into rows at a given size, measured with no window.</summary>
    public static List<string> RowsOf(string line, int screenWidth, bool large) =>
        TextMetrics.Wrap(line, TextWidth(screenWidth), large ? LargeTextSize : TextSize, TextWeight);

    /// <summary>The width a line is wrapped to on a screen of a given width.</summary>
    public static float TextWidth(int screenWidth) => screenWidth - Side * 2 - TextMargins;

    /// <summary>A line as the box breaks it into rows, measured with no window.</summary>
    public static List<string> RowsOf(string line, int screenWidth) => TextMetrics.Wrap(line, TextWidth(screenWidth), TextSize, TextWeight);

    // The panel of a question's answers (ModernUi.DrawChoices): a row to an answer, as wide as its longest answer and
    // no narrower than ChoiceWidth, its right edge the text box's

    /// <summary>An answer's size and weight, what the panel adds to its longest answer's width, and its least width.</summary>
    public const float ChoiceSize = 34, ChoicePadding = 116, ChoiceWidth = 260;
    public const UiWeight ChoiceWeight = UiWeight.Black;

    /// <summary>The widest an answer may be for its panel to stay on a screen of a given width: the text box's own width.</summary>
    public static float WidestChoice(int screenWidth) => screenWidth - Side * 2 - ChoicePadding;
}
