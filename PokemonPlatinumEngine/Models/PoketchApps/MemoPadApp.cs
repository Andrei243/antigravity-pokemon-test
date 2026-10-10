namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>The MemoPad app (plan 06 · R14b): to be built from the original's <c>src/applications/poketch</c>.</summary>
public sealed class MemoPadApp : PoketchAppState
{
    public override PoketchApp App => PoketchApp.MemoPad;
}
