using System;
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

    /// <summary>A friend's trainer being fought and the team that fights them (plan 13 · V22); null outside such a battle.</summary>
    private TrainerFile? roomFriend;
    private BuiltTeam? roomFriendTeam;

    /// <summary>The daily run under way (plan 13 · V14), between its battles and in them; null outside one.</summary>
    private DailyRun? dailyRun;

    /// <summary>This machine's daily results (<c>daily.json</c>), read as the room opens.</summary>
    private DailyRecords dailyRecords = new();

    /// <summary>What the Factory's screen is open for in the room: the day's six to rent, or a trade; null when the field's Factory has it.</summary>
    private FactoryScreenMode? dailyScreen;

    /// <summary>A battle of the battle room is being fought (a run's or a puzzle's).</summary>
    internal bool InRoomBattle => flow.State == GameState.Battle && (roomRun != null || roomPuzzle != null || roomFriend != null || dailyRun != null) && roomBattle;
    private bool roomBattle;

    private string TeamsFile => System.IO.Path.Combine(SavePaths.Current.Folder, BuiltTeams.FileName);

    /// <summary>The room from the title, on the teams of its file.</summary>
    private void OpenBattleRoom()
    {
        roomRun = null;
        roomPuzzle = null;
        roomFriend = null;
        roomFriendTeam = null;
        roomBattle = false;
        dailyRun = null;
        dailyScreen = null;
        dailyRecords = DailyRecords.Read(SavePaths.Current.Daily);
        flow.BattleRoomScreen.Open(BuiltTeams.Read(TeamsFile).Teams, Puzzles.All, Settings.BattleRoom, dailyRecords, DailyRun.Today());
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
            case OpenFriends:
                OpenTheFriendsPage();
                break;
            case BeginRun { Kind: RoomChallenge.Friend } friend:
                roomFriend = friendToFight;
                roomFriendTeam = room.Teams[friend.Team];
                StartRoomBattle();
                break;
            case BeginRun begin:
                // The run's own seed: drawn from the game's chance once, everything after it from the seed
                roomRun = new RoomRun(begin.Kind, room.Teams[begin.Team], (uint)Dice.Shared.Next());
                StartRoomBattle();
                break;
            case BeginVersus pair:
                BeginTheVersus(room.Teams[pair.First], room.Teams[pair.Second]);
                break;
            case BeginDaily:
                BeginTheDailyRun();
                break;
            case TradeDaily when dailyRun is { CanTrade: true } daily:
                OpenTheDailysScreen(FactoryScreenMode.Trade, daily);
                break;
            case NextBattle:
                StartRoomBattle();
                break;
            case GiveUpRun when dailyRun != null:
                dailyRun.Lose();
                bool dailyBest = KeepTheDay(dailyRun);
                room.AfterDailyBattle(dailyRun, dailyBest);
                dailyRun = null;
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
        roomFriend = null;
        roomFriendTeam = null;
        dailyRun = null;
        dailyScreen = null;
        flow.State = GameState.Title;
    }

    /// <summary>
    /// The day's run (plan 13 · V14): drawn from today's seed, counted only when the day has no result yet, its six on
    /// offer on the Factory's own screen. Nothing is kept until three are rented, so the six may be looked at and put back.
    /// </summary>
    private void BeginTheDailyRun()
    {
        var today = DailyRun.Today();
        dailyRun = DailyRun.For(today, counts: !dailyRecords.Played(today));
        OpenTheDailysScreen(FactoryScreenMode.Rent, dailyRun);
    }

    private void OpenTheDailysScreen(FactoryScreenMode mode, DailyRun daily)
    {
        var factory = daily.Factory;
        foreach (var rental in factory.Offer.Concat(factory.Team).Concat(factory.Foe))
        {
            flow.WantModel(rental.Species);
            Graphics.PokemonSprites.Request(rental.Species);
        }
        dailyScreen = mode;
        if (mode == FactoryScreenMode.Rent) flow.FactoryScreen.OpenToRent(factory.Offer.Select(factory.Build).ToList(), Models.Frontier.BattleFactoryChallenge.TeamSize);
        else
        {
            // The trainer just beaten is the one before the battle to come
            var beaten = FrontierDatabase.Trainer(factory.Trainers[Math.Max(0, factory.Battle - 1)]);
            flow.FactoryScreen.OpenToTrade(factory.Team.Select(factory.Build).ToList(), factory.Foe.Select(factory.Build).ToList(), beaten.Name);
        }
        flow.State = GameState.FactoryRentals;
    }

    /// <summary>The Factory's screen closed over the room: three rented and the first battle, a trade made or not, or the six put back.</summary>
    private void CloseTheDailysScreen()
    {
        var outcome = flow.FactoryScreen.Outcome;
        var mode = dailyScreen;
        dailyScreen = null;
        flow.State = GameState.BattleRoom;
        if (dailyRun is not { } daily) { flow.BattleRoomScreen.AfterDailyRentals(); return; }
        if (mode == FactoryScreenMode.Rent)
        {
            if (outcome.Count != Models.Frontier.BattleFactoryChallenge.TeamSize)
            {
                dailyRun = null;
                flow.BattleRoomScreen.AfterDailyRentals();
                return;
            }
            daily.Rent(outcome);
            // The day is spent from here: a run left half way is kept where it stood
            KeepTheDay(daily);
            StartRoomBattle();
            return;
        }
        if (outcome.Count == 2) daily.Trade(outcome[0], outcome[1]);
        flow.BattleRoomScreen.AfterDailyBattle(daily, false);
    }

    /// <summary>The day's result written as it stands; true on a new best. A practice run writes nothing.</summary>
    private bool KeepTheDay(DailyRun daily)
    {
        if (!daily.Counts) return false;
        bool best = dailyRecords.Keep(daily);
        DailyRecords.Write(SavePaths.Current.Daily, dailyRecords);
        return best;
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
        else if (roomFriend is { } friend && roomFriendTeam is { } mine)
        {
            // A friend's trainer: both sides made afresh and brought to level 50, the trainer thinking with every routine of the AI
            foe = friend.ToTrainer();
            foe.DialogueBefore = Text.Of($"friends.line.{friend.Before}");
            foe.DialogueAfter = Text.Of($"friends.line.{friend.After}");
            setup = new BattleSetup
            {
                PlayerParty = LevelScaling.TeamAt(BuiltTeams.PartyOf(mine).Members, TrainerFile.FlatLevel),
                Inventory = new Inventory(),
                Pokedex = new Pokedex(),
                Trainers = new List<Trainer> { foe },
                Random = new BattleRandom((uint)Dice.Shared.Next()),
                Rules = Ruleset.Modern,
                Conditions = new BattleConditions { Terrain = BattleTerrain.Building },
                Frontier = true,
                PlayerName = PlayerIdentity.Name,
                MoveHints = MoveHintsOn,
                TrainerLines = TrainerLinesOn,
                Scene = Settings.BattleScene
            };
        }
        else if (dailyRun is { Rented: true } daily)
        {
            daily.Ready();
            foe = daily.NextFoe();
            setup = new BattleSetup
            {
                PlayerParty = daily.PlayerParty(),
                Inventory = new Inventory(),
                Pokedex = new Pokedex(),
                Trainers = new List<Trainer> { foe },
                // The day's battle on the day's generator: the same battle for everyone who answers it the same
                Random = new BattleRandom(daily.BattleSeed),
                Rules = Ruleset.Modern,
                Conditions = new BattleConditions { Terrain = BattleTerrain.Building },
                Frontier = true,
                PlayerName = PlayerIdentity.Name,
                MoveHints = MoveHintsOn,
                TrainerLines = TrainerLinesOn,
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
        AudioManager.Tale = null;
        if (roomPuzzle is { } puzzle)
        {
            bool solved = Puzzles.Judge(puzzle, battle.Core);
            if (solved && Settings.BattleRoom.Solve(puzzle.Id)) Settings.Save();
            roomPuzzle = null;
            StartTransition(GameState.BattleRoom, () => LeaveTheRoomsBattle(() => flow.BattleRoomScreen.AfterPuzzle(puzzle, solved)));
            return;
        }
        if (roomFriend != null)
        {
            // One battle and no run: nothing is kept, and the friends' row is the room's cursor again
            roomFriend = null;
            roomFriendTeam = null;
            StartTransition(GameState.BattleRoom, () => LeaveTheRoomsBattle(() => flow.BattleRoomScreen.AfterFriend(BuiltTeams.Read(TeamsFile).Teams)));
            return;
        }
        if (dailyRun is { } daily)
        {
            if (battle.Result == BattleResult.PlayerVictory) daily.Won();
            else daily.Lose();
            bool dayBest = KeepTheDay(daily);
            if (daily.Over) dailyRun = null;
            StartTransition(GameState.BattleRoom, () => LeaveTheRoomsBattle(() => flow.BattleRoomScreen.AfterDailyBattle(daily, dayBest)));
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

        /// <summary>The daily run begun, as the room's DAILY RUN does, but on a day of the tool's (the harness's fixed date by default) and with its six on offer.</summary>
        public void BeginDailyRun(DailyRun daily)
        {
            game.roomRun = null;
            game.roomPuzzle = null;
            game.dailyRun = daily;
            game.OpenTheDailysScreen(FactoryScreenMode.Rent, daily);
        }

        /// <summary>The daily run under way, if any.</summary>
        public DailyRun? DailyRun => game.dailyRun;

        /// <summary>Whether a battle of the room is being fought.</summary>
        public bool InRoomBattle => game.roomBattle;
    }

    /// <summary>The goal of the puzzle being played, over its battle.</summary>
    private void DrawRoomGoal()
    {
        if (roomBattle && roomPuzzle is { } puzzle && flow.Battle is { } battle) ModernUi.PuzzleGoalLine(VirtualWidth, puzzle, battle.Core.Turn);
    }
}
