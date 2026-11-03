namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Stream-safe (plan 13 · V24, the options' STREAM-SAFE row; style guide, "The Trainer Card"): what a viewer shouldn't be
/// able to copy off a screen. With it on, a trainer ID is five dashes wherever it is drawn (the card, the Hall of Fame's
/// record and ceremony, a Pokémon's summary) and the user's own folder is left out of the paths the log writes. It is the
/// process's, like <see cref="Seeing"/>: <see cref="GameEngine.ApplySettings"/> sets it. The live feed
/// (<see cref="LiveFeed"/>) never carries an ID or a path whatever this says. GPU-free.
/// </summary>
public static class StreamSafe
{
    /// <summary>What a hidden ID is drawn as.</summary>
    public const string Hidden = "-----";

    public static bool On { get; private set; }

    public static void Use(bool on) => On = on;

    /// <summary>A trainer ID as it is shown: the digits, or dashes of the same length.</summary>
    public static string Id(string shown) => On ? Hidden : shown;
}
