namespace PokemonPlatinumEngine.Models;

/// <summary>
/// Mauville's roulette (plan 21 · H12, part k; Emerald's <c>roulette.c</c>), GPU-free: a board of twelve squares, three
/// colours (rows: orange, green, purple) by four Pokémon (columns: Wynaut, Azurill, Skitty, Makuhita), six balls a round.
/// A wager of the table's minimum (1 or 3 coins) is placed on a colour, a Pokémon or one square; the ball lands on a square
/// not yet hit this round, and a hit pays the minimum times Emerald's <c>GetMultiplier</c>, which grows as the round's
/// hits leave fewer squares of the wager (a colour 3, 4, 6, 12; a Pokémon 4, 6, 12; a square 12; nought once nothing of
/// it is left). This is the board alone: where the ball lands is <see cref="RouletteWheel"/>'s, the wheel it rolls round
/// (plan 21 · H12, part o).
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
    /// The ball has landed on <paramref name="square"/> (row * 4 + column; Emerald's <c>RecordHit</c>, which counts a
    /// square landed on twice twice). Returns the coins the wager wins, nought on a miss: its odds are taken before the
    /// hit is counted, as <c>Task_PlaceBet</c> takes them before the ball is thrown.
    /// </summary>
    public int Land(Kind kind, int row, int column, int square)
    {
        int multiplier = Multiplier(kind, row, column);
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

    /// <summary>Whether a wager can still be placed: Emerald plays a buzzer for one whose squares have all been hit.</summary>
    public bool Open(Kind kind, int row, int column) => Multiplier(kind, row, column) > 0;
}
