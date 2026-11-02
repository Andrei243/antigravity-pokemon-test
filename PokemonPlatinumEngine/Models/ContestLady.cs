using System;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The Contest Lady of Lilycove's Pokémon Center (plan 21 · H12, part n; <c>docs/mechanics/rulings.md</c>, "The quiz and
/// contest ladies"): Emerald's <c>src/lilycove_lady.c</c>, the lady whose Pokémon is getting ready for a contest of one
/// condition and who asks for a Pokéblock to feed it. A Pokéblock with any of the condition's flavour is a good one, and the
/// largest such flavour given is kept as the best (<c>GivePokeblockToContestLady</c>); five good ones, or five of any
/// other, and her Pokémon is ready (<c>ShouldContestLadyShowGoOnAir</c>). What she remembers is the story's own:
/// <see cref="CategoryVariable"/>, <see cref="GoodVariable"/>, <see cref="OtherVariable"/> and <see cref="SheenVariable"/>.
/// No drawing or input.
/// </summary>
public static class ContestLady
{
    /// <summary>The condition her Pokémon trains for, one more than its place in <see cref="Categories"/> (0 before one is drawn).</summary>
    public const string CategoryVariable = "VAR_HOENN_CONTEST_LADY";
    public const string GoodVariable = "VAR_HOENN_CONTEST_LADY_GOOD";
    public const string OtherVariable = "VAR_HOENN_CONTEST_LADY_OTHER";
    /// <summary>The most of the condition's flavour any Pokéblock given her held (<c>maxSheen</c>).</summary>
    public const string SheenVariable = "VAR_HOENN_CONTEST_LADY_SHEEN";

    /// <summary>Emerald's <c>LILYCOVE_LADY_GIFT_THRESHOLD</c>.</summary>
    public const int Threshold = 5;

    /// <summary>The five conditions in Emerald's order.</summary>
    public static readonly string[] Categories = { "cool", "beauty", "cute", "smart", "tough" };

    /// <summary>Her Pokémon by the condition (<c>sContestLadyMonGfxId</c>): Zigzagoon, Skitty, Poochyena, Kecleon, Pikachu.</summary>
    public static readonly string[] Species = { "Zigzagoon", "Skitty", "Poochyena", "Kecleon", "Pikachu" };

    /// <summary>A new condition (<c>ResetContestLadyContestData</c>: <c>Random() % CONTEST_CATEGORIES_COUNT</c>), on the caller's sixteen-bit draw.</summary>
    public static int Pick(int next16) => (next16 & 0xFFFF) % Categories.Length;

    /// <summary>
    /// <c>GivePokeblockToContestLady</c>: whether a Pokéblock has the condition's flavour (spicy for cool, dry for beauty,
    /// sweet for cute, bitter for smart, sour for tough), and how much of it.
    /// </summary>
    public static (bool Good, int Sheen) Judge(int category, Pokeblock block)
    {
        int flavour = category switch
        {
            0 => block.Spicy,
            1 => block.Dry,
            2 => block.Sweet,
            3 => block.Bitter,
            _ => block.Sour
        };
        return (flavour != 0, flavour);
    }

    /// <summary><c>ShouldContestLadyShowGoOnAir</c>: five good Pokéblocks, or five of any other.</summary>
    public static bool Ready(int good, int other) => good >= Threshold || other >= Threshold;
}

/// <summary>
/// The Quiz Lady of Lilycove's Pokémon Center (plan 21 · H12, part n; <c>docs/mechanics/rulings.md</c>, "The quiz and
/// contest ladies"): Emerald's lady who sets a quiz with a prize. Emerald's sixteen questions are easy-chat sentences with
/// one word for the answer and a prize each (<c>sQuizLadyPrizes</c>, kept here); the questions are our own, asked with
/// three answers to choose from, in <c>lilycove_city_pokemon_center_1f.txt</c>. She draws a question the first time she
/// is spoken to on a day after the last was answered (<see cref="QuestionVariable"/>). No drawing or input.
/// </summary>
public static class QuizLady
{
    /// <summary>The question set, one more than its place (0 before one is drawn).</summary>
    public const string QuestionVariable = "VAR_HOENN_QUIZ_LADY";

    /// <summary>Emerald's sixteen prizes, a question each, in its order.</summary>
    public static readonly string[] Prizes =
    {
        "Glitter Mail", "Bead Mail", "Tropic Mail", "Max Ether", "Max Ether", "Max Ether", "Watmel Berry", "Belue Berry",
        "Durin Berry", "Luxury Ball", "TM15", "Big Pearl", "Star Piece", "Rare Candy", "Rare Candy", "Premier Ball"
    };

    /// <summary><c>QuizLadyPickQuestion</c>: <c>Random() % ARRAY_COUNT(sQuizLadyQuizQuestions)</c>, on the caller's sixteen-bit draw.</summary>
    public static int Pick(int next16) => (next16 & 0xFFFF) % Prizes.Length;
}
