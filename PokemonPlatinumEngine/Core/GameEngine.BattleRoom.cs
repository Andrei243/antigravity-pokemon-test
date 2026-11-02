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
/// The battle room (plan 13 · V13, V26; rulings, "The team builder, the gauntlet and the battle puzzles"): the title's
/// BATTLE ROOM opens it with no game loaded. Its teams are <c>teams.json</c> beside the saves, its records are this
/// machine's (<see cref="GameSettings.BattleRoom"/>), and its battles are fought on Pokémon made for each battle and let
/// go after it, with a bag of their own, a Pokédex of their own and nothing paid: no save is read or written by any of
/// it. A run's battles and a puzzle's come back to the room behind a plain fade.
/// </summary>
public partial class GameEngine
{
    /// <summary>The run under way, between its battles and in them; null outside one.</summary>
    private RoomRun? roomRun;

    /// <summary>The puzzle being played; null outside one.</summary>
    private PuzzleFile? roomPuzzle;

    /// <summary>A battle of the battle room is being fought (a run's or a puzzle's).</summary>
    internal bool InRoomBattle => flow.State == GameState.Battle && (roomRun != null || roomPuzzle != null) && roomBattle;
    private bool roomBattle;

    private string TeamsFile => System.IO.Path.Combine(SavePaths.Current.Folder, BuiltTeams.FileName);

    /// <summary>The room from the title, on the teams of its file.</summary>
    private void OpenBattleRoom()
    {
        roomRun = null;
        roomPuzzle = null;
        roomBattle = false;
        flow.BattleRoomScreen.Open(BuiltTeams.Read(TeamsFile).Teams, Puzzles.All, Settings.BattleRoom);
        flow.State = GameState.BattleRoom;
    }

    private void UpdateBattleRoom(float dt)
    {
        var room = flow.BattleRoomScreen;
        room.Update(dt);
        switch (room.TakeRequest())
        {
            case OpenBuilder:
                flow.TeamBuilderScreen.Open(BuiltTeams.Read(TeamsFile));
                flow.State = GameState.TeamBuilder;
                break;
            case BeginRun begin:
                // The run's own seed: drawn from the game's chance once, everything after it from the seed
                roomRun = new RoomRun(begin.Kind, room.Teams[begin.Team], (uint)Dice.Shared.Next());
                StartRoomBattle();
                break;
            case NextBattle:
                StartRoomBattle();
                break;
            case GiveUpRun when roomRun != null:
                roomRun.Lose();
                bool best = roomRun.Keep(Settings.BattleRoom);
                Settings.Save();
                room.AfterRunBattle(roomRun, best);
                roomRun = null;
                break;
            case PlayPuzzle play when Puzzles.All.FirstOrDefault(p => p.Id == play.Id) is { } puzzle:
                roomPuzzle = puzzle;
                StartRoomBattle();
                break;
            case null:
                if (room.Left) LeaveBattleRoom();
                break;
        }
    }

    private void LeaveBattleRoom()
    {
        flow.BattleRoomScreen.Close();
        roomRun = null;
        roomPuzzle = null;
        flow.State = GameState.Title;
    }

    private void UpdateTeamBuilder(float dt)
    {
        var builder = flow.TeamBuilderScreen;
        builder.Update(dt);
        if (builder.IsActive) return;
        if (builder.Changed) BuiltTeams.Write(TeamsFile, builder.File);
        flow.BattleRoomScreen.Resume(builder.File.Teams);
        flow.State = GameState.BattleRoom;
    }

    /// <summary>
    /// The room's next battle behind the battle's own way in: the run's (the built team made afresh against the run's next
    /// trainer, from the battle's seed) or the puzzle's (its two sides, its field, its fixed rolls and its foe's moves).
    /// </summary>
    private void StartRoomBattle()
    {
        BattleSetup setup;
        Trainer? foe;
        if (roomPuzzle is { } puzzle)
        {
            foe = Puzzles.FoeTrainer(puzzle);
            setup = new BattleSetup
            {
                PlayerParty = Puzzles.PlayerParty(puzzle),
                Inventory = Puzzles.Bag(puzzle),
                Pokedex = new Pokedex(),
                Trainers = foe == null ? new List<Trainer>() : new List<Trainer> { foe },
                WildPokemon = Puzzles.WildPokemon(puzzle),
                Random = Puzzles.Random(puzzle),
                Rules = puzzle.RulesSet,
                Conditions = Puzzles.Conditions(puzzle),
                EnemyController = new PuzzleFoe(puzzle.FoeMoves),
                NoBattleItems = puzzle.Balls == 0,
                // A puzzle is no part of a game: no EXP, prize or white-out
                NoRewards = true,
                PlayerName = PlayerIdentity.Name,
                MoveHints = MoveHintsOn,
                Scene = Settings.BattleScene
            };
        }
        else if (roomRun is { } run)
        {
            foe = run.NextFoe();
            setup = new BattleSetup
            {
                PlayerParty = run.PlayerParty(),
                Inventory = new Inventory(),
                Pokedex = new Pokedex(),
                Trainers = new List<Trainer> { foe },
                Random = new BattleRandom(run.BattleSeed),
                Rules = Ruleset.Modern,
                Conditions = new BattleConditions { Terrain = BattleTerrain.Building },
                // The Frontier's rules: no EXP, no money, no bag, every Pokémon obeys
                Frontier = true,
                PlayerName = PlayerIdentity.Name,
                MoveHints = MoveHintsOn,
                TrainerLines = TrainerLinesOn,
                Scene = Settings.BattleScene
            };
        }
        else return;

        var shown = setup.PlayerParty.Members.Concat(setup.WildPokemon).Concat(setup.Trainers.SelectMany(t => t.Party.Members)).ToList();
        var models = shown.Select(p => p.LookName).Distinct().ToList();
        foreach (string name in models) flow.WantModel(name);
        if (foe != null) CharacterModels.Preload(new[] { CharacterStyles.Default.InBattle(foe) });
        eyeThemePlaying = false;
        if (foe != null) AudioManager.PlayMusic(MusicDirector.BattleRole(new[] { foe.TrainerClass }), immediate: true);
        else if (setup.WildPokemon.Count > 0) AudioManager.PlayWildBattleMusic(setup.WildPokemon[0].Species);
        StartTransition(GameState.Battle, () =>
        {
            AwaitModels(models);
            flow.Battle = new BattleEngine(setup);
            battleRenderer.SetArena(BattleArena.Indoors);
            flow.BattleTrainers.Clear();
            roomBattle = true;
        });
    }

    /// <summary>
    /// A frame of a battle of the room: nothing of the game's own ending runs, and a puzzle's battle stops as soon as its
    /// goal can no longer be met (its turns are used up and the menu is open again). True when it handled the frame's end.
    /// </summary>
    private bool KeepTheRoomsBattle()
    {
        if (!roomBattle || flow.Battle is not { } battle) return false;
        bool over = battle.IsBattleOver;
        if (!over && roomPuzzle is { } puzzle && battle.HUD.MenuState == BattleMenuState.Main && battle.Core.Turn >= puzzle.Turns) over = true;
        if (over) EndRoomBattle(battle);
        return true;
    }

    private void EndRoomBattle(BattleEngine battle)
    {
        roomBattle = false;
        AudioManager.LowHp = false;
        AudioManager.Giant = false;
        if (roomPuzzle is { } puzzle)
        {
            bool solved = Puzzles.Judge(puzzle, battle.Core);
            if (solved && Settings.BattleRoom.Solve(puzzle.Id)) Settings.Save();
            roomPuzzle = null;
            StartTransition(GameState.BattleRoom, () => LeaveTheRoomsBattle(() => flow.BattleRoomScreen.AfterPuzzle(puzzle, solved)));
            return;
        }
        if (roomRun is { } run)
        {
            if (battle.Result == BattleResult.PlayerVictory) run.Won();
            else run.Lose();
            bool best = run.Over && run.Keep(Settings.BattleRoom);
            if (run.Over) Settings.Save();
            if (run.Over) roomRun = null;
            StartTransition(GameState.BattleRoom, () => LeaveTheRoomsBattle(() => flow.BattleRoomScreen.AfterRunBattle(run, best)));
        }
    }

    // Behind the fade: the battle and its models go, the title's music comes back, the room shows what happened
    private void LeaveTheRoomsBattle(System.Action tell)
    {
        flow.Battle = null;
        PokemonModels.Trim(PokemonModels.ShownAtOnce);
        AudioManager.PlayMusic(MusicRole.Title);
        tell();
    }

    internal sealed partial class Driver
    {
        public BattleRoomScreen BattleRoom => game.flow.BattleRoomScreen;
        public TeamBuilderScreen TeamBuilder => game.flow.TeamBuilderScreen;
        public void OpenBattleRoom() => game.OpenBattleRoom();

        /// <summary>A run of the room begun with its next battle at once, as BEGIN does (the harness builds the run, to start it where it wants).</summary>
        public void StartRoomRun(RoomRun run)
        {
            game.roomRun = run;
            game.roomPuzzle = null;
            game.StartRoomBattle();
        }

        /// <summary>A puzzle's battle begun, as the room's PLAY does.</summary>
        public void PlayPuzzle(PuzzleFile puzzle)
        {
            game.roomRun = null;
            game.roomPuzzle = puzzle;
            game.StartRoomBattle();
        }

        /// <summary>Whether a battle of the room is being fought.</summary>
        public bool InRoomBattle => game.roomBattle;
    }

    /// <summary>The goal of the puzzle being played, over its battle.</summary>
    private void DrawRoomGoal()
    {
        if (roomBattle && roomPuzzle is { } puzzle && flow.Battle is { } battle) ModernUi.PuzzleGoalLine(VirtualWidth, puzzle, battle.Core.Turn);
    }
}
