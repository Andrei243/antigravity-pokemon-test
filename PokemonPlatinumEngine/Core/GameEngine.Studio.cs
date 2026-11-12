using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Studio;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The film studio (plan 42 · PS4; rulings, "The film studio"): a take is a battle of the film's own, made as the battle
/// room makes a puzzle's (the cast lent, the foe and their moves turn by turn, the fixed rolls) under the Frontier's
/// rules (no EXP, money or bag, the Pokédex told nothing, RUN a question that ends the take) with the film's director
/// on the face. It never reaches <c>EndBattle</c>, the Vs. Recorder, the autosave or what a battle leaves behind: the
/// take is kept in the studio's records and the script that shot it goes on.
/// </summary>
public partial class GameEngine
{
    /// <summary>The film whose take is being shot; null outside one.</summary>
    private StudioFilm? studioShooting;

    internal void StartStudioFilm(StudioFilm film)
    {
        EndRadarChain();
        flow.ScriptOutcome = BattleOutcome.None;
        var puzzle = film.Battle;
        var foe = Puzzles.FoeTrainer(puzzle)!;
        var setup = new BattleSetup
        {
            PlayerParty = Puzzles.PlayerParty(puzzle),
            Inventory = Puzzles.Bag(puzzle),
            Pokedex = new Pokedex(),
            Trainers = new List<Trainer> { foe },
            Random = Puzzles.Random(puzzle),
            Rules = puzzle.RulesSet,
            Conditions = Puzzles.Conditions(puzzle),
            EnemyController = new PuzzleFoe(puzzle.FoeMoves),
            NoBattleItems = true,
            NoRewards = true,
            Frontier = true,
            PlayerName = PlayerIdentity.Name,
            MoveHints = MoveHintsOn,
            Scene = Settings.BattleScene,
            StudioFilm = new StudioDirector(film),
            StudioTakeNumber = flow.Studio.ShotOf(film.Id) + 1
        };
        var models = setup.PlayerParty.Members.Concat(foe.Party.Members).Select(p => p.LookName).Distinct().ToList();
        foreach (string name in models) flow.WantModel(name);
        CharacterModels.Preload(new[] { CharacterStyles.Default.InBattle(foe) });
        eyeThemePlaying = false;
        AudioManager.PlayMusic(MusicDirector.BattleRole(new[] { foe.TrainerClass }), immediate: true);
        studioShooting = film;
        StartTransition(GameState.Battle, () =>
        {
            AwaitModels(models);
            flow.Battle = new BattleEngine(setup);
            battleRenderer.SetArena(BattleArena.Indoors);
            flow.BattleTrainers.Clear();
        });
    }

    /// <summary>
    /// A frame of a take: once the battle is over, or its turns are used up and the menu is open again, the ending's lines
    /// and stars are said; once they are read the take is kept and the field comes back. True while a take is shot.
    /// </summary>
    private bool KeepTheStudioBattle()
    {
        if (studioShooting is not { } film || flow.Battle is not { Studio: not null } battle) return false;
        if (battle.StudioTake == null)
        {
            bool outOfTurns = battle.Result == BattleResult.None && battle.HUD.MenuState == BattleMenuState.Main && battle.Core.Turn >= film.Battle.Turns;
            if (battle.IsBattleOver || outOfTurns) battle.WrapUpStudio();
            return true;
        }
        if (!battle.StudioDone) return true;
        studioShooting = null;
        flow.Studio.Keep(battle.StudioTake);
        flow.ScriptOutcome = battle.Result == BattleResult.PlayerVictory ? BattleOutcome.Won : BattleOutcome.Lost;
        flow.BattleMayBeLost = false;
        AudioManager.LowHp = false;
        AudioManager.Giant = false;
        StartTransition(GameState.Overworld, () =>
        {
            PokemonModels.Trim(flow.Party.Members.Select(p => p.LookName));
            PlayFieldMusic();
        });
        return true;
    }
}
