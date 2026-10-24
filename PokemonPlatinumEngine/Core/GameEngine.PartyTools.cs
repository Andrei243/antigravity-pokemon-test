using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The party menu's conveniences (plan 12 · Q19): REMEMBER is the screen's own (<see cref="UI.PartyScreen.OpenRemember"/>,
/// <see cref="Relearning"/>); RENAME opens the Pokémon's keyboard over the field and comes back to the menu.
/// </summary>
public partial class GameEngine
{
    /// <summary>Whether the options' PARTY TOOLS row is on under the rules in force.</summary>
    internal bool PartyToolsOn => Overworld.FieldHelp.On(Settings.PartyTools, Ruleset.Current.Preset);

    /// <summary>The keyboard for the team's Pokémon at a place, on its name now; the menu waits under it.</summary>
    internal void OpenNickname(int place)
    {
        if (place < 0 || place >= flow.Party.Count) return;
        flow.NicknameScreen.Open(flow.Party.Members[place]);
        flow.State = GameState.Nickname;
    }

    /// <summary>The keyboard closed: the Pokémon keeps the name given (none, for its species' own), and the menu says so.</summary>
    private void CloseNickname()
    {
        var screen = flow.NicknameScreen;
        var pokemon = screen.Target;
        if (pokemon != null && screen.Nickname is { } name)
        {
            pokemon.Nickname = name;
            flow.PartyScreen.Message = Text.Of(name.Length == 0 ? "party.rename.reset" : "party.rename.done", ("name", pokemon.DisplayName));
        }
        flow.State = GameState.PartyMenu;
    }
}
