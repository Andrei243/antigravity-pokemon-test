using System.Text;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.UI.Kit;

namespace ContentCheck;

/// <summary>
/// Whether written lines fit the field's text box and can be drawn. A line (one string of a person's <c>dialog</c>, of
/// a sign, of a script's <c>say</c>) is written whole into the box, wrapped to its width, and nothing scrolls or
/// pages (<see cref="DialogueBox"/>): one that wraps to more rows than the box holds runs out under the panel and off
/// the screen. It is measured as the game measures it (<see cref="TextMetrics"/>, the typeface's own widths), with
/// the names it leaves open filled at their widest: <c>{player}</c> and <c>{rival}</c> with the widest name the
/// keyboard of <see cref="NameEntry"/> can make, <c>{assistant}</c> with the wider of the two it can be, and what a
/// script fills in as it runs (<c>{lead}</c>, <c>{item}</c>, <c>{money}</c>...) with the widest of its kind in the
/// game's data. A character outside what <see cref="UiFonts"/> loads is drawn as a question mark.
/// </summary>
public static class Lines
{
    private static readonly Lazy<string> longestName = new(() =>
    {
        // Every key of the name keyboard, in both its cases, as the keyboard itself gives them, with the letters of every language that has some of its own
        var keyboard = new NameEntry(extras: new string(Language.All.SelectMany(l => l.Keyboard).Distinct().Take(NameEntry.Columns).ToArray()));
        var keys = new StringBuilder();
        for (int row = 0; row < keyboard.RowCount; row++) keys.Append(keyboard.RowKeys(row));
        // One row up from the first, round to the wide keys, is the key that changes the case
        keyboard.Move(0, -1);
        if (keyboard.Press() != NameKey.Case) throw new InvalidOperationException("The name keyboard's case key is not where it was.");
        for (int row = 0; row < keyboard.RowCount; row++) keys.Append(keyboard.RowKeys(row));
        char widest = keys.ToString().Distinct().OrderByDescending(c => Width(c.ToString())).ThenBy(c => c).First();
        return new string(widest, PlayerIdentity.MaxNameLength);
    });

    /// <summary>The widest name the name keyboard takes: its widest key, as many times as a name is long.</summary>
    public static string LongestName => longestName.Value;

    private static float Width(string text) => TextMetrics.Measure(text, DialogueBox.TextSize, DialogueBox.TextWeight);

    private static string Widest(IEnumerable<string> names) => names.OrderByDescending(Width).ThenBy(n => n, StringComparer.Ordinal).FirstOrDefault() ?? "";

    private static readonly Lazy<string> widestSpecies = new(() => Widest(PokemonDatabase.GetAll().Select(s => s.Name)));
    private static readonly Lazy<string> widestItem = new(() => Widest(ItemDatabase.GetAll().Select(i => i.Name)));
    private static readonly Lazy<string> widestMove = new(() => Widest(MoveDatabase.GetAll().Select(m => m.Name)));
    private static readonly Lazy<string> widestTrainer = new(() => Widest(TrainerDatabase.All.Select(t => $"{t.Class} {t.Name}")));

    private static readonly Lazy<string> widestObjective = new(() => Widest(PokemonPlatinumEngine.Story.Objectives.All.Select(o => Filled(Text.Of(o.Key)))));

    private static readonly Regex Placeholder = new(@"\{([a-z]+)(?::([A-Z0-9_]+))?\}", RegexOptions.Compiled);

    /// <summary>
    /// A line with everything it leaves open filled in at its widest: the three names <see cref="PlayerIdentity.Fill(string)"/>
    /// fills, and what <c>ScriptRunner.Fill</c> fills as a script runs, each by the kind of thing it names.
    /// </summary>
    public static string Filled(string line) => !line.Contains('{') ? line : Placeholder.Replace(line, match => match.Groups[1].Value switch
    {
        "player" or "rival" => LongestName,
        "assistant" => Widest(new[] { PlayerIdentity.DefaultName(PlayerLook.Boy), PlayerIdentity.DefaultName(PlayerLook.Girl) }),
        // A Pokémon by its name
        "lead" or "starter" or "rivalstarter" or "assistantstarter" or "user" or "member" or "swarm" or "trophygarden" or "marsh" or "winner"
            or "daycare" or "left" or "taken" or "wearer" or "rental" or "entrant" or "walker" or "contestant" or "ally" or "chosen" => widestSpecies.Value,
        // A move
        "forgotten" or "allymove" => widestMove.Value,
        // What to do next (plan 12 · Q14): the widest of the story's lines, with the names in it at their widest
        "objective" => widestObjective.Value,
        // A phrase of two words (plan 08 · P9), whose widest are a Pokémon's name and a move's
        "phrase" => widestSpecies.Value + " " + widestMove.Value,
        // A trainer by class and name, or two of them
        "foe" or "struck" => widestTrainer.Value,
        // A number: a variable, an amount of money or of points, a level, a price
        "var" or "money" or "result" or "yield" or "ticket" or "level" or "grown" or "fee" or "seals" or "coins" or "price" or "number" or "points" or "rank" or "cp" => "888,888",
        // Anything else is a thing by its name: an item, a berry, an accessory, a Seal, a ribbon, a place
        _ => widestItem.Value
    });

    /// <summary>
    /// A line of the text box: its characters, and the rows it wraps to. <paramref name="speakers"/> are the names the
    /// box may show on its tag as the line is said: a line that begins with one and a colon is written without it
    /// (<c>DialogueManager</c> drops it, since the tag says who speaks).
    /// </summary>
    public static void Check(List<Finding> found, string file, string name, string line, params string?[] speakers)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            found.Add(new Finding(Kind.EmptyLine, file, name, "an empty line: the text box would come up with nothing in it"));
            return;
        }
        string filled = Filled(line);
        foreach (string? speaker in speakers)
            if (speaker is { Length: > 0 } && filled.StartsWith(Filled(speaker) + ": ", StringComparison.Ordinal))
            {
                filled = filled[(Filled(speaker).Length + 2)..];
                break;
            }
        Characters(found, file, name, filled, DialogueBox.TextWeight);

        var rows = DialogueBox.RowsOf(filled, GameEngine.VirtualWidth);
        float width = DialogueBox.TextWidth(GameEngine.VirtualWidth);
        if (found is Findings tally) tally.LinesByRows[rows.Count] = tally.LinesByRows.GetValueOrDefault(rows.Count) + 1;
        if (rows.Count > DialogueBox.Rows)
            found.Add(new Finding(Kind.TooWide, file, name, $"the line wraps to {rows.Count} rows and the text box holds {DialogueBox.Rows}: \"{Short(line)}\""));
        else if (rows.FirstOrDefault(row => Width(row) > width) is { } word)
            found.Add(new Finding(Kind.TooWide, file, name, $"\"{Short(word)}\" is one word wider than the text box"));
    }

    /// <summary>An answer of a question's panel: its characters, and that the panel it widens stays on the screen.</summary>
    public static void CheckAnswer(List<Finding> found, string file, string name, string answer)
    {
        string filled = Filled(answer);
        Characters(found, file, name, filled, DialogueBox.ChoiceWeight);
        if (TextMetrics.Measure(filled, DialogueBox.ChoiceSize, DialogueBox.ChoiceWeight) > DialogueBox.WidestChoice(GameEngine.VirtualWidth))
            found.Add(new Finding(Kind.TooWide, file, name, $"the answer \"{Short(answer)}\" makes its panel wider than the screen"));
    }

    /// <summary>A name shown on the box's tag, or anywhere a name is drawn: its characters.</summary>
    public static void CheckName(List<Finding> found, string file, string name, string text) =>
        Characters(found, file, name, Filled(text), UiWeight.Black);

    private static void Characters(List<Finding> found, string file, string name, string text, UiWeight weight)
    {
        var undrawable = TextMetrics.Undrawable(text, weight);
        if (undrawable.Count > 0)
            found.Add(new Finding(Kind.Character, file, name,
                $"the interface can't draw {string.Join(", ", undrawable.Select(r => $"'{r}' (U+{r.Value:X4})"))} and shows a question mark, or nothing, in its place: \"{Short(text)}\""));
    }

    /// <summary>The beginning of a text on one line, to know it by.</summary>
    private static string Short(string text)
    {
        text = text.Replace("\n", " / ");
        return text.Length <= 60 ? text : text[..57] + "...";
    }
}
