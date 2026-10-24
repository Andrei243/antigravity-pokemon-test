using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The Vs. Recorder's viewer (plan 08 · P11; style guide, "Menu screens", "Vs. Recorder" and "Watching"): USE on the
/// Vs. Recorder, from the bag or the item button, opens the list of the battles kept in <see cref="ReplaysFolder"/>;
/// a recording chosen is watched as a battle (<see cref="BattleEngine.ForReplay"/>) behind a plain fade, and the list
/// comes back once it is left or over. A battle watched changes nothing of the game: it is never handed the team, the
/// bag, the Pokédex, the boxes or the money, its end runs none of <c>EndBattle</c>, and it is never recorded again.
/// </summary>
public partial class GameEngine
{
    /// <summary>Where the list goes back to: the bag, if the Vs. Recorder was used from it.</summary>
    private bool replaysFromBag;

    /// <summary>The recording broke off the last time one was watched: the list says so as it comes back.</summary>
    internal bool ReplayBroke { get; private set; }

    /// <summary>Opens the Vs. Recorder's list on the files of <see cref="ReplaysFolder"/>.</summary>
    private void OpenReplays(bool fromBag)
    {
        replaysFromBag = fromBag;
        ReplayBroke = false;
        flow.ReplayListScreen.Open(ReplayListScreen.Read(ReplaysFolder));
        flow.State = GameState.ReplayList;
    }

    private void UpdateReplayList(float dt)
    {
        flow.ReplayListScreen.Update(dt);
        if (flow.ReplayListScreen.TakeChoice() is { File: { } file }) WatchReplay(file);
        else if (!flow.ReplayListScreen.IsActive) CloseReplays();
    }

    /// <summary>Back to where the Vs. Recorder was used: the bag as it was left, or the field.</summary>
    private void CloseReplays()
    {
        flow.ReplayListScreen.Close();
        if (replaysFromBag)
        {
            flow.State = GameState.BagMenu;
            flow.BagScreen.Resume();
        }
        else flow.State = GameState.Overworld;
    }

    /// <summary>
    /// A recording watched: the music and the models of the battle it was asked for, and behind a plain fade the
    /// battle made from the file alone, on the stage of the ground it was fought on.
    /// </summary>
    private void WatchReplay(BattleRecordFile file)
    {
        // The Pokémon the battle will show, with the shapes they may take in it, asked for before the screen is dark
        var setup = file.ToSetup();
        var shown = setup.PlayerParty.Members.Concat(setup.WildPokemon).Concat(setup.Trainers.SelectMany(t => t.Party.Members))
            .Concat(setup.Partner?.Party.Members ?? Enumerable.Empty<Pokemon>()).ToList();
        var models = shown.Select(p => p.LookName).Concat(shown.SelectMany(p => BattleShapesOf(p).Select(shape => PokemonLook.Of(shape, p.IsShiny)))).Distinct().ToList();
        foreach (string name in models) flow.WantModel(name);
        CharacterModels.Preload(setup.Trainers.Concat(setup.Partner == null ? Enumerable.Empty<Trainer>() : new[] { setup.Partner })
            .Select(t => CharacterStyles.Default.InBattle(t)));

        eyeThemePlaying = false;
        if (setup.Trainers.Count > 0) AudioManager.PlayMusic(MusicDirector.BattleRole(setup.Trainers.Select(t => t.TrainerClass)), immediate: true);
        else if (setup.WildPokemon.Count > 0) AudioManager.PlayWildBattleMusic(setup.WildPokemon[0].Species);

        StartTransition(GameState.Battle, () =>
        {
            AwaitModels(models);
            flow.Battle = BattleEngine.ForReplay(file, Settings.BattleScene, TrainerLinesOn, PlayerIdentity.Character);
            battleRenderer.SetArena(ArenaOf(file.Conditions.Terrain));
        });
    }

    /// <summary>
    /// The watching is over (left, broken off, or shown to its end): behind a plain fade the battle goes, the models
    /// the field doesn't need with it, the field's music comes back and the list is as it was left.
    /// </summary>
    private void EndWatching()
    {
        ReplayBroke = flow.Battle?.WatchBroke == true;
        AudioManager.LowHp = false;
        StartTransition(GameState.ReplayList, () =>
        {
            flow.Battle = null;
            PokemonModels.Trim(flow.Party.Members.Select(p => p.LookName));
            PlayFieldMusic();
            flow.ReplayListScreen.Resume();
            if (ReplayBroke) ShowNotification(Text.Of("replays.broke"));
        });
    }

    /// <summary>The stage of a battle watched: the ground it was fought on, as the field's stages go by it.</summary>
    internal static BattleArena ArenaOf(BattleTerrain terrain) => terrain switch
    {
        BattleTerrain.Sand => BattleArena.Sand,
        BattleTerrain.Mountain or BattleTerrain.Cave => BattleArena.Cave,
        BattleTerrain.Snow or BattleTerrain.Ice => BattleArena.Snow,
        BattleTerrain.Water => BattleArena.Water,
        BattleTerrain.Building or BattleTerrain.Special => BattleArena.Indoors,
        _ => BattleArena.Grass
    };
}
