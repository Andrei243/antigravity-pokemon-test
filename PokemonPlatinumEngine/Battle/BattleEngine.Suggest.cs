using PokemonPlatinumEngine.Battle.Sim;

namespace PokemonPlatinumEngine.Battle;

public partial class BattleEngine
{
    /// <summary>
    /// The move a player with nothing else in mind would use now (plan 16 · T7, the playthrough bot; T12's headless
    /// game): <see cref="MoveSuggestion.For"/> for <see cref="MenuBattler"/>, the one scorer the bot, AUTO and Let's Go
    /// share. It draws nothing: a choice made outside the rules must not move what they roll next, or a recorded battle
    /// would no longer replay.
    /// </summary>
    public int Suggest() => MoveSuggestion.For(core, InCore(MenuBattler));
}
