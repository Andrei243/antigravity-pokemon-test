namespace ContentCheck;

/// <summary>What kind of mistake a finding is: what the summary counts by, and what a ceiling is kept for.</summary>
public enum Kind
{
    /// <summary>A file that can't be read at all, or a world or a room that would stop the game as it loads.</summary>
    Unreadable,
    /// <summary>A room's file that isn't what the game would write of the map it makes: something in it is lost on the way.</summary>
    RoundTrip,
    /// <summary>A field the file's format doesn't have: a slip of the pen the reader passes over without a word.</summary>
    UnknownField,
    /// <summary>An overlay for an area that isn't open, or whose <c>area</c> isn't its file's name.</summary>
    Misplaced,
    /// <summary>Someone the overlay names who is not among the area's objects, or whom the game never stands there.</summary>
    Person,
    /// <summary>A door, a locked warp, a switch or an exit that names no warp of the area, or leads to no map or no open tile.</summary>
    Way,
    /// <summary>A trigger, a sign, a tile read or a pit that names nothing of the area, or stands outside it.</summary>
    Place,
    /// <summary>A script nothing has: a person's, a sign's, a trigger's, a tile's.</summary>
    Script,
    /// <summary>A trainer the table lacks, or a species, a move, an item or a counter the data lacks.</summary>
    Data,
    /// <summary>A flag that hides or shows someone, or opens a way, and that nothing ever sets.</summary>
    Flag,
    /// <summary>A line that runs out of the text box.</summary>
    TooWide,
    /// <summary>A line with nothing in it.</summary>
    EmptyLine,
    /// <summary>A character the interface can't draw.</summary>
    Character,
    /// <summary>A song the place plays that isn't there.</summary>
    Music,
    /// <summary>What <c>ScriptLibrary.Problems</c> says of the scripts.</summary>
    ScriptProblem
}

/// <summary>
/// One thing wrong with the game's hand-written files: the file, relative to the data folder, the name of what is
/// wrong in it (a person, a door, a line of a script) and what is wrong, in words.
/// </summary>
public sealed record Finding(Kind Kind, string File, string Name, string What)
{
    public override string ToString() => $"{File}: {Name}: {What}";
}

/// <summary>
/// The findings of a check, with a count of what was measured on the way: how many lines of the text box were read
/// and how many rows each took, which says how near the box's edge the writing runs.
/// </summary>
public sealed class Findings : List<Finding>
{
    public Findings() { }
    public Findings(IEnumerable<Finding> found) : base(found) { }

    /// <summary>How many lines wrapped to each number of rows.</summary>
    public SortedDictionary<int, int> LinesByRows { get; } = new();
}
