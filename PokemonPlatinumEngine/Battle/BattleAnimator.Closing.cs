namespace PokemonPlatinumEngine.Battle;

/// <summary>How a battle ended, as the face showed it (plan 45 · BC5): what a manner's closing films.</summary>
public enum BattleClosing
{
    /// <summary>The player won: the log's <c>Won</c>.</summary>
    Won,
    /// <summary>The player lost: the foe's side cheering.</summary>
    Lost,
    /// <summary>A wild Pokémon was caught: the ball's own shots, as ever.</summary>
    Caught,
}

public sealed partial class BattleAnimator
{
    /// <summary>
    /// Whether the battle is a Gym Leader's, an Elite Four's or a Champion's (plan 45 · BC5: the crane over their stage).
    /// The face sets it as the battle is made, from the first trainer's class; the camera's manner decides what of it to film.
    /// </summary>
    public bool BossBattle { get; set; }

    /// <summary>
    /// How the battle ended and when, in <see cref="Time"/> (plan 45 · BC5): noted once by the face as the line that
    /// tells of it is shown (with the Battle Scene on), whatever the manner; null while the battle goes on.
    /// </summary>
    public (BattleClosing Kind, float Start)? Closing { get; private set; }

    /// <summary>Notes how the battle ended, the first time only: a battle ends once.</summary>
    public void NoteClosing(BattleClosing kind) => Closing ??= (kind, Time);

    /// <summary>Seconds since the closing came, -1 before it.</summary>
    public float ClosingAge => Closing is { } c ? Time - c.Start : -1f;
}
