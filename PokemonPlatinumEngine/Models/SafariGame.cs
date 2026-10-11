namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The Great Marsh's Safari Game as Platinum plays it (plan 01 · M7): for the fee of 500, thirty Safari Balls and
/// five hundred steps. While it is on, every Pokémon met is met in a Safari battle (<c>BattleKind.Safari</c>,
/// plan 06 · R9), which throws the game's own balls and nothing else; the game ends when the steps or the balls
/// run out, or when the player walks out of the marsh. No drawing or input: the scripts start and end it
/// (<c>safari start</c>, <c>safari end</c>) and the engine counts the steps.
/// </summary>
public sealed class SafariGame
{
    public const int Fee = 500, StartBalls = 30, StartSteps = 500;

    /// <summary>A game is under way.</summary>
    public bool Active { get; private set; }

    public int Balls { get; set; }
    public int Steps { get; private set; }

    /// <summary>
    /// The Pokémon caught in this game (the original's <c>GetCurrentSafariGameCaughtNum</c>; plan 01 · M11, part b). It
    /// outlasts the game's end, until the next one starts: the observatory's cowgirl asks once the player is back.
    /// </summary>
    public int Caught { get; set; }

    public void Start()
    {
        Active = true;
        Balls = StartBalls;
        Steps = StartSteps;
        Caught = 0;
    }

    public void End()
    {
        Active = false;
        Balls = 0;
        Steps = 0;
    }

    /// <summary>A game read back from a save.</summary>
    public void Resume(int balls, int steps, int caught = 0)
    {
        Active = true;
        Balls = balls;
        Steps = steps;
        Caught = caught;
    }

    /// <summary>One step in the marsh; true when it was the last.</summary>
    public bool Step()
    {
        if (!Active || Steps <= 0) return false;
        Steps--;
        return Steps == 0;
    }

    /// <summary>The balls ran out in a battle: the game is over.</summary>
    public bool OutOfBalls => Active && Balls <= 0;
}
