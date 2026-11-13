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
    /// Where the studio keeps its best takes (plan 42 · PS4, part b): a folder of their own inside the Vs. Recorder's, so
    /// the Vs. Recorder's list never shows them (a take is watched at its premiere, with its film's beats and ending).
    /// </summary>
    internal string StudioTakesFolder => System.IO.Path.Combine(ReplaysFolder, "studio");

    /// <summary>A film's best take written over the one before it, beside itself first and moved into place.</summary>
    private void KeepTheTakeFile(BattleRecordFile file)
    {
        try
        {
            System.IO.Directory.CreateDirectory(StudioTakesFolder);
            string path = System.IO.Path.Combine(StudioTakesFolder, StudioFilms.TakeFileName(file.StudioFilm!));
            string temporary = SaveManager.TempOf(path);
            System.IO.File.WriteAllText(temporary, file.Serialize());
            System.IO.File.Move(temporary, path, overwrite: true);
        }
        catch (System.Exception e) when (e is System.IO.IOException or System.UnauthorizedAccessException)
        {
            Diagnostics.Log.Warn($"The studio's take couldn't be kept: {e.Message}");
        }
    }

    /// <summary>The film whose best take is showing at the premiere; null outside one.</summary>
    private StudioFilm? premiereShowing;
    private bool premiereEnding;

    /// <summary>
    /// The premiere of a film's best take (part b): its file watched as the Vs. Recorder watches one, with a director
    /// who says the take's lines again. False, with nothing shown, where there is no take to show or it can't be read.
    /// </summary>
    internal bool StartPremiere(StudioFilm film)
    {
        string path = System.IO.Path.Combine(StudioTakesFolder, StudioFilms.TakeFileName(film.Id));
        if (!System.IO.File.Exists(path)) return false;
        if (BattleRecordFile.ReadFile(path).File is not { } file || file.StudioFilm != film.Id) return false;
        flow.Studio.LastBoxOffice = 0;
        premiereShowing = film;
        WatchReplay(file);
        return true;
    }

    /// <summary>
    /// A frame of the premiere: once the reel is over its ending, crowd and box office are said; once they are read, or
    /// Menu leaves, the theatre comes back and the script goes on. True while a premiere shows.
    /// </summary>
    private bool KeepThePremiere()
    {
        if (premiereShowing is not { } film || flow.Battle is not { Watching: true } battle) return false;
        // Over, and the theatre coming back behind the fade: nothing else of a battle watched may run on it
        if (premiereEnding) return true;
        if (battle.ReelOver) battle.WrapUpStudio();
        bool over = battle.WatchLeft || battle.WatchBroke || battle.StudioDone;
        if (!over) return true;
        premiereEnding = true;
        if (battle.StudioTake is { } take && !battle.WatchLeft) flow.Studio.LastBoxOffice = StudioFilms.BoxOffice(film, take.Stars);
        AudioManager.LowHp = false;
        AudioManager.Giant = false;
        AudioManager.Tale = null;
        StartTransition(GameState.Overworld, () =>
        {
            premiereShowing = null;
            premiereEnding = false;
            flow.Battle = null;
            PokemonModels.Trim(flow.Party.Members.Select(p => p.LookName));
            PlayFieldMusic();
        });
        return true;
    }

    /// <summary>
    /// A frame of a take: once the battle is over, or its turns are used up and the menu is open again, the ending's lines
    /// and stars are said; once they are read the take is kept and the field comes back. True while a take is shot.
    /// </summary>
    private bool KeepTheStudioBattle()
    {
        if (flow.Battle is not { Studio: not null } battle) return false;
        // Kept already: the battle stays on screen behind the fade, and nothing else of a battle's ending may run on it
        if (studioShooting is not { } film) return true;
        if (battle.StudioTake == null)
        {
            bool outOfTurns = battle.Result == BattleResult.None && battle.HUD.MenuState == BattleMenuState.Main && battle.Core.Turn >= film.Battle.Turns;
            if (battle.IsBattleOver || outOfTurns) battle.WrapUpStudio();
            return true;
        }
        if (!battle.StudioDone) return true;
        studioShooting = null;
        // A new best is kept as a Vs. Recorder file for the premiere (part b)
        if (flow.Studio.Keep(battle.StudioTake) && battle.RecordFile(GameClock.Today) is { } file) KeepTheTakeFile(file);
        flow.ScriptOutcome = battle.Result == BattleResult.PlayerVictory ? BattleOutcome.Won : BattleOutcome.Lost;
        flow.BattleMayBeLost = false;
        AudioManager.LowHp = false;
        AudioManager.Giant = false;
        AudioManager.Tale = null;
        StartTransition(GameState.Overworld, () =>
        {
            PokemonModels.Trim(flow.Party.Members.Select(p => p.LookName));
            PlayFieldMusic();
        });
        return true;
    }
}
