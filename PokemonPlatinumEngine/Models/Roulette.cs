namespace PokemonPlatinumEngine.Models;

/// <summary>
/// Mauville's roulette (plan 21 · H12, part k; Emerald's <c>roulette.c</c>), GPU-free: a board of twelve squares, three
/// colours (rows: orange, green, purple) by four Pokémon (columns: Wynaut, Azurill, Skitty, Makuhita), six balls a round.
/// A wager of the table's minimum (1 or 3 coins) is placed on a colour, a Pokémon or one square; the ball lands on a square
/// not yet hit this round, and a hit pays the minimum times Emerald's <c>GetMultiplier</c>, which grows as the round's
/// hits leave fewer squares of the wager (a colour 3, 4, 6, 12; a Pokémon 4, 6, 12; a square 12; nought once nothing of
/// it is left). Ours: the ball is drawn among the free squares on the script's die, where Emerald rolls it round a wheel
/// the player throws it onto; Shroomish and Taillow's tricks are not played.
/// </summary>
public sealed class RouletteRound
{
    public const int Colours = 3, Pokemon = 4, Squares = Colours * Pokemon, BallsPerRound = 6;

    /// <summary>The Pokémon of the board's columns, by their species' names (the data's).</summary>
    public static readonly string[] Columns = { "Wynaut", "Azurill", "Skitty", "Makuhita" };

    private static readonly int[] Multipliers = { 0, 3, 4, 6, 12 };

    private readonly bool[] hit = new bool[Squares];
    private readonly int[] colourHits = new int[Colours], pokemonHits = new int[Pokemon];

    public RouletteRound(int minBet) => MinBet = minBet;

    /// <summary>The table's wager (Emerald's <c>sTableMinBets</c>: 1 on the left table, 3 on the right).</summary>
    public int MinBet { get; }

    /// <summary>The balls thrown this round.</summary>
    public int Balls { get; private set; }

    public bool Over => Balls >= BallsPerRound;

    /// <summary>The square last landed on (row * 4 + column), or -1.</summary>
    public int Landed { get; internal set; } = -1;

    public bool IsHit(int square) => hit[square];

    /// <summary>A wager: on a colour's row, a Pokémon's column, or one square.</summary>
    public enum Kind { Colour, Pokemon, Square }

    /// <summary>Emerald's <c>GetMultiplier</c>: what a wager pays, times the table's minimum, before the ball is thrown.</summary>
    public int Multiplier(Kind kind, int row, int column) => kind switch
    {
        Kind.Colour => colourHits[row] >= Pokemon ? 0 : Multipliers[colourHits[row] + 1],
        Kind.Pokemon => pokemonHits[column] >= Colours ? 0 : Multipliers[pokemonHits[column] + 2],
        _ => hit[row * Pokemon + column] ? 0 : Multipliers[^1]
    };

    /// <summary>
    /// The ball thrown: <paramref name="roll"/> (0 to the free squares less one) picks the free square it lands on.
    /// Returns the coins the wager wins, nought on a miss.
    /// </summary>
    public int Throw(Kind kind, int row, int column, int roll)
    {
        int multiplier = Multiplier(kind, row, column);
        var free = Enumerable.Range(0, Squares).Where(s => !hit[s]).ToList();
        int square = free[Math.Clamp(roll, 0, free.Count - 1)];
        hit[square] = true;
        colourHits[square / Pokemon]++;
        pokemonHits[square % Pokemon]++;
        Balls++;
        Landed = square;
        bool won = kind switch
        {
            Kind.Colour => square / Pokemon == row,
            Kind.Pokemon => square % Pokemon == column,
            _ => square == row * Pokemon + column
        };
        return won ? MinBet * multiplier : 0;
    }

    /// <summary>How many squares are left free: the die the next ball is drawn on.</summary>
    public int Free => Squares - Balls;
}
