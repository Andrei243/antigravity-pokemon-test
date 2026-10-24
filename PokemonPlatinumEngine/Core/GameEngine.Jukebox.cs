using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The jukebox (plan 12 · Q15; style guide, "Menu screens", "Jukebox"): JUKEBOX on the title's menu and the options' row of the same
/// name open <see cref="JukeboxScreen"/> over whatever they were opened from, and B goes back there with the music the
/// screen found put back. What it unlocks is the songs the game has played (<see cref="AudioManager.Heard"/>); on the
/// title, where no game is loaded, that is also what each readable slot has kept.
/// </summary>
public partial class GameEngine
{
    private string? jukeboxResumeMusic;

    private void OpenJukebox(GameState returnTo)
    {
        flow.JukeboxReturn = returnTo;
        jukeboxResumeMusic = AudioManager.CurrentMusic;
        var heard = new HashSet<string>(AudioManager.Heard, StringComparer.Ordinal);
        if (returnTo == GameState.Title)
            foreach (var slot in titleSlots)
                if (slot.Save?.Heard is { } kept) heard.UnionWith(kept);
        var species = PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).ToList();
        flow.JukeboxScreen.Open(heard, species);
        flow.State = GameState.Jukebox;
    }

    private void UpdateJukebox(float dt)
    {
        flow.JukeboxScreen.Update(dt);
        if (!flow.JukeboxScreen.IsActive) CloseJukebox();
    }

    /// <summary>Back to where the jukebox was opened from, with the song that was playing there playing again.</summary>
    private void CloseJukebox()
    {
        flow.JukeboxScreen.Close();
        if (jukeboxResumeMusic != null) AudioManager.PlayMusic(jukeboxResumeMusic, immediate: true);
        else AudioManager.StopMusic();
        jukeboxResumeMusic = null;
        flow.State = flow.JukeboxReturn;
    }
}
