using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Core;

/// <summary>How a language chooses between the forms of a word that goes with a number.</summary>
public enum PluralRule
{
    /// <summary>One for exactly one, other for the rest (English, German, Spanish, Italian).</summary>
    English,
    /// <summary>One for nought and one, other for the rest (French).</summary>
    French,
    /// <summary>Other for every number (Japanese, Korean, Chinese).</summary>
    None
}

/// <summary>
/// A language of the game (plan 17 · N1): its IETF tag, its own name for itself, its script, how it chooses a
/// word's form by a number and the mark between a number's whole part and its fraction. The list is
/// <c>Data/text/languages.json</c>; what each says is <c>Data/text/&lt;tag&gt;/ui.json</c>, read by <see cref="Text"/>.
/// GPU-free.
/// </summary>
public sealed record Language
{
    public string Tag { get; init; } = Text.English;
    /// <summary>The language's own name for itself, which the options' first row shows.</summary>
    public string Name { get; init; } = "English";
    public string Script { get; init; } = "Latin";
    public PluralRule Plural { get; init; } = PluralRule.English;
    public string Decimal { get; init; } = ".";
    /// <summary>Where a line may break: <c>spaces</c>, or between any two characters for a script without them (plan 17 · N5).</summary>
    public string Breaks { get; init; } = "spaces";
    /// <summary>The pseudo-language, which brackets every string for the tests and is never offered to a player.</summary>
    public bool Pseudo { get; init; }

    /// <summary>The scripts the interface's faces can draw; another language is listed but not offered until plan 17 · N5.</summary>
    public static readonly IReadOnlySet<string> DrawnScripts = new HashSet<string> { "Latin" };

    private static IReadOnlyList<Language>? all;

    /// <summary>Every language of <c>languages.json</c>, English first.</summary>
    public static IReadOnlyList<Language> All => all ??= GameDataFiles.Load<List<Language>>("text/languages.json");

    /// <summary>The languages the options' row goes through: those a player can read with the faces the game has.</summary>
    public static IReadOnlyList<Language> Offered => All.Where(l => !l.Pseudo && DrawnScripts.Contains(l.Script)).ToList();

    /// <summary>The language of a tag, or null for one the list doesn't have.</summary>
    public static Language? Of(string? tag) => All.FirstOrDefault(l => string.Equals(l.Tag, tag, StringComparison.OrdinalIgnoreCase));

    /// <summary>The form of a word that goes with <paramref name="n"/>: <c>one</c> or <c>other</c>.</summary>
    public string FormFor(long n) => Plural switch
    {
        PluralRule.English => n == 1 ? "one" : "other",
        PluralRule.French => n is 0 or 1 ? "one" : "other",
        _ => "other"
    };
}
