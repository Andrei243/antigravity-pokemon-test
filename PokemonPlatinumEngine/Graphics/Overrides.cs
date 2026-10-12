namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Whether the hand-made files beside the game are read at all (plan 16 · T3): the models of
/// <c>overrides/models</c> (<see cref="ModelOverrides"/>), the people's sprites of <c>overrides/sprites</c>
/// (<see cref="CharacterSprites"/>) and the Pokémon's field sprites under it (<see cref="FieldSprites"/>). They are,
/// unless the game was started with <c>--safe-mode</c>, which shows the game as it is built: a file of the player's
/// own that the game can't take is then not what keeps it from starting. Whatever reads an override asks here first;
/// plan 16 · T14's packs will too.
/// </summary>
internal static class Overrides
{
    public static bool Enabled { get; set; } = true;
}
