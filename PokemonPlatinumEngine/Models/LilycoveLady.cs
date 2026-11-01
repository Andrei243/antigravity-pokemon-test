using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The lady of Lilycove's Pokémon Center (plan 21 · H11, part c; <c>docs/mechanics/rulings.md</c>, "The Lilycove Lady"):
/// Emerald's Favor Lady (<c>src/lilycove_lady.c</c>, its tables in <c>src/data/lilycove_lady.h</c>), who is taken with one
/// kind of thing (something slippery, roundish, whamish, shiny, sticky or pointy), keeps whatever the player hands her, and
/// counts the things that were the kind she wanted: five of them, or at once the one she wanted most, earn the favour's prize
/// and a new favour. Emerald's other two ladies (the quiz and the contest) are not built, so she is the Favor Lady in every
/// game. What she remembers is the story's own: <see cref="FavorVariable"/>, <see cref="BestVariable"/> and
/// <see cref="GivenVariable"/>. No drawing or input.
/// </summary>
public static class LilycoveLady
{
    /// <summary>The favour asked for, one more than its place in <see cref="Accepted"/> (0 before one is drawn).</summary>
    public const string FavorVariable = "VAR_HOENN_FAVOR_LADY";

    /// <summary>The place in the favour's list of the item she wants most.</summary>
    public const string BestVariable = "VAR_HOENN_FAVOR_LADY_BEST";

    /// <summary>The things of her favour's kind given since it was drawn.</summary>
    public const string GivenVariable = "VAR_HOENN_FAVOR_LADY_GIVEN";

    /// <summary>Emerald's <c>LILYCOVE_LADY_GIFT_THRESHOLD</c>: the things of her kind that earn the prize.</summary>
    public const int Threshold = 5;

    /// <summary>The six favours in Emerald's order: slippery, roundish, whamish, shiny, sticky, pointy.</summary>
    public static readonly string[] Favors = { "slippery", "roundish", "whamish", "shiny", "sticky", "pointy" };

    /// <summary>What she takes for each favour (Emerald's <c>sFavorLadyAcceptedItemLists</c>, by our items' names).</summary>
    public static readonly string[][] Accepted =
    {
        new[] { "Repel", "Super Repel", "Max Repel", "Antidote", "Paralyze Heal", "Burn Heal", "Belue Berry", "Awakening", "Ice Heal", "Revive", "Max Revive", "Energy Powder" },
        new[] { "Fluffy Tail", "Pearl", "Big Pearl", "Hard Stone", "Smoke Ball", "Shoal Shell", "Tiny Mushroom", "Big Mushroom", "Pecha Berry", "Aspear Berry", "Oran Berry", "Grepa Berry", "Magost Berry", "Watmel Berry", "Poké Ball", "Ultra Ball" },
        new[] { "Revival Herb", "Potion", "Fresh Water", "Soda Pop", "Lemonade", "Hard Stone", "Light Ball", "Lava Cookie", "Chesto Berry", "Nanab Berry", "Wepear Berry", "Kelpsy Berry", "Nomel Berry", "Durin Berry" },
        new[] { "Heal Powder", "X Speed", "X Attack", "X Defense", "Blue Flute", "Yellow Flute", "Red Flute", "Black Flute", "White Flute", "Nugget", "Sun Stone", "Stardust", "Star Piece", "Pearl", "Big Pearl", "Twisted Spoon", "Silver Powder", "Bright Powder", "Luxury Ball", "Premier Ball" },
        new[] { "Energy Root", "Full Restore", "Max Potion", "Dire Hit", "X Accuracy", "Guard Spec.", "Watmel Berry", "Leftovers", "Tiny Mushroom", "Heart Scale" },
        new[] { "Quick Claw", "Poison Barb", "Sharp Beak", "Dragon Fang", "Tamato Berry", "Durin Berry", "Petaya Berry", "Salac Berry", "Stardust", "Star Piece" }
    };

    /// <summary>Each favour's prize (Emerald's <c>sFavorLadyPrizes</c>).</summary>
    public static readonly string[] Prizes = { "Luxury Ball", "Nugget", "Protein", "Heart Scale", "Rare Candy", "PP Max" };

    /// <summary>
    /// Which lady a game has in Emerald (<c>InitLilycoveLady</c>): the trainer number's low half, modulo six, halved, so
    /// 0 the quiz, 1 the favour, 2 the contest. Kept for the day the other two are built; every game has the favour now.
    /// </summary>
    public static int EmeraldLadyOf(uint trainerNumber) => (int)(trainerNumber & 0xFFFF) % 6 >> 1;

    /// <summary>A new favour and the item she wants most in it (<c>FavorLadyPickFavorAndBestItem</c>), on the caller's sixteen-bit draws.</summary>
    public static (int Favor, int Best) Pick(Func<int> next16)
    {
        int favor = (next16() & 0xFFFF) % Favors.Length;
        int best = (next16() & 0xFFFF) % Accepted[favor].Length;
        return (favor, best);
    }

    /// <summary>Whether an item is of the favour's kind (<c>DoesFavorLadyLikeItem</c>).</summary>
    public static bool Likes(int favor, string item) => Accepted[favor].Contains(item);

    /// <summary>The item she wants most in a favour.</summary>
    public static string BestOf(int favor, int best) => Accepted[favor][Math.Clamp(best, 0, Accepted[favor].Length - 1)];

    /// <summary>
    /// What handing her an item does to the things counted (<c>DoesFavorLadyLikeItem</c>): one more of her kind, or the
    /// threshold at once for the one she wanted most; nothing for anything else.
    /// </summary>
    public static int CountAfter(int favor, int best, int given, string item) =>
        !Likes(favor, item) ? given : item == BestOf(favor, best) ? Threshold : given + 1;
}
