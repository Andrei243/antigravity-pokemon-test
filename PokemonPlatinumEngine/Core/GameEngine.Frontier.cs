using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The Battle Frontier in the game (plan 06 · R18a): where the frame's rules (<see cref="FrontierRecords"/>,
/// <see cref="FrontierRules"/>, <see cref="FrontierTrainers"/>) and the Battle Tower's
/// (<see cref="BattleTowerChallenge"/>) meet the maps and the screens. The team is entered on the party screen
/// (<see cref="PartyScreen.OpenToEnter"/>), a battle is one of the Frontier's own (<see cref="BattleSetup.Frontier"/>),
/// fought with copies and leaving nothing behind, and a facility's board is <see cref="GameState.FrontierRecords"/>.
/// </summary>
public partial class GameEngine
{
    public const string TowerLobbyMap = "BattleTower";

    /// <summary>The people of the Tower's battle rooms the opponents walk in as (their map files' ids).</summary>
    public static readonly string[] TowerOpponentPeople = { "opponent", "opponent_2" };
    public const string TowerPartnerPerson = "partner";

    /// <summary>The variables the Tower's scripts keep its rooms' states in, which the game only reads; and the print's own.</summary>
    public const string TowerLobbyAction = "VAR_BATTLE_TOWER_LOBBY_LOAD_ACTION";

    private FrontierRecords frontier = new();
    private BattleTowerChallenge? tower;
    private IReadOnlyList<int> enteredSlots = Array.Empty<int>();
    private readonly FrontierRecordsScreen frontierRecordsScreen = new();
    private bool showBattlePoints;

    /// <summary>A new game's Frontier: nothing won, and the Tower's draws started from the day's number (<c>sub_0206C008</c> in <c>game_start.c</c>).</summary>
    private void LoadFrontier(FrontierRecords? saved)
    {
        tower = null;
        enteredSlots = Array.Empty<int>();
        if (saved != null)
        {
            frontier = saved;
            return;
        }
        frontier = new FrontierRecords();
        frontier.NewDay(encounters.DailyNumber);
    }

    private int FrontierAct(string what, int number, NPC? subject)
    {
        switch (what)
        {
            case "enter":
                // The team, for that many to be chosen in their order (BT_FUNC_UNK_30, the party menu's battle tower mode)
                scriptAnswer = 0;
                enteredSlots = Array.Empty<int>();
                partyScreen.ShowCondition = story.Has(ContestHallVisitedFlag);
                partyScreen.OpenToEnter(number, FrontierRules.MayEnter);
                currentState = GameState.PartyMenu;
                return 0;
            case "records":
                frontierRecordsScreen.Open(FrontierBoard.For((FrontierFacility)number, frontier, story));
                currentState = GameState.FrontierRecords;
                return 1;
            case "seen":
                return playerPokedex.IsSeen(number) ? 1 : 0;
            case "score":
                trainerScore = TrainerScore.Add(trainerScore, number);
                return 1;
            case "opponents":
                StandOpponents();
                return 1;
            case "partner":
                // Whoever stands as the partner in a room of the Tower takes the look and the name of the one chosen
                if (number >= 0 && number < BattleTowerChallenge.PartnerNames.Length && currentMap.FindPerson(TowerPartnerPerson) is { } beside)
                {
                    beside.NpcType = BattleTowerChallenge.PartnerLooks[number];
                    beside.Name = BattleTowerChallenge.PartnerNames[number];
                    KeepCharactersNear(trim: false);
                }
                return 1;
            case "points":
                showBattlePoints = number != 0;
                return frontier.BattlePoints;
            case "save":
                SaveCurrentGame(quiet: true);
                AudioManager.PlaySound("save");
                return 1;
            case "journal":
                journal.Tell(new JournalEvent(JournalEventKind.BattleTower));
                return 1;
            default:
                return 0;
        }
    }

    /// <summary>
    /// Whoever the Tower has made ready walks into the battle room as their class does (<c>BT_FUNC_GET_OPPONENT_OBJECT_ID</c>):
    /// the room's opponent takes their look and their name.
    /// </summary>
    private void StandOpponents()
    {
        var foes = runner.TowerFoes;
        for (int i = 0; i < foes.Count && i < TowerOpponentPeople.Length; i++)
        {
            if (currentMap.FindPerson(TowerOpponentPeople[i]) is not { } person) continue;
            person.NpcType = FrontierTrainers.LookOf(foes[i].Record);
            person.Name = $"{foes[i].Record.Class} {foes[i].Record.Name}";
        }
        KeepCharactersNear(trim: false);
    }

    /// <summary>
    /// A battle of the Battle Frontier (<c>FieldBattleDTO_NewBattleTower</c>): the team is the battle's own, the foes
    /// are the trainers made for it, the ground is the Frontier's own, and nothing is won, lost or learnt by it.
    /// </summary>
    private void StartFrontierBattle(Party team, IReadOnlyList<Trainer> foes, Trainer? beside, bool doubles)
    {
        EndRadarChain();
        scriptOutcome = BattleOutcome.None;
        var trainers = foes.ToList();
        CharacterModels.Preload(trainers.Append(beside).OfType<Trainer>()
            .Select(t => CharacterStyles.Default.InBattle(t.TrainerClass, PlayerIdentity.CharacterFor(t.Look ?? "Trainer"))));
        eyeThemePlaying = false;
        AudioManager.PlayMusic(MusicDirector.BattleRole(trainers.Select(t => t.TrainerClass)), immediate: true);
        var shown = PrepareModels(trainers.SelectMany(t => t.Party.Members).Concat(beside?.Party.Members ?? Enumerable.Empty<Pokemon>()).Concat(team.Members));
        StartTransition(GameState.Battle, () =>
        {
            AwaitModels(shown);
            battle = new BattleEngine(new BattleSetup
            {
                PlayerParty = team,
                Inventory = playerInventory,
                Pokedex = playerPokedex,
                Place = PlaceName(),
                Format = doubles ? BattleFormat.Double : BattleFormat.Single,
                Trainers = trainers,
                Partner = beside,
                Frontier = true,
                // BACKGROUND_BATTLE_TOWER and TERRAIN_BATTLE_TOWER: one of the grounds of their own
                Conditions = new Battle.Sim.BattleConditions
                {
                    Terrain = Battle.Sim.BattleTerrain.Special,
                    Night = GameClock.IsNight,
                    HasCaught = species => playerPokedex.IsCaught(species.DexNumber),
                    Badges = story.BadgeCount,
                    Player = PlayerMark
                },
                MoveHints = MoveHintsOn
            });
            battleRenderer.SetArena(currentMap, player.GridX, player.GridY);
            battleTrainers.Clear();
        }, SceneTransition.ForBattle(trainer: true, leader: trainers.Any(t => t.TrainerClass == "Tower Tycoon"), trainers[0].Party.Members[0].Level, team.Members.FirstOrDefault()?.Level ?? 0));
    }

    /// <summary>
    /// A Frontier battle is over: the script that started it hears how it went, and the field comes back. No money,
    /// no EXP, no evolution, no waking up in a Pokémon Center: the battle's copies are let go and the team is as it was.
    /// </summary>
    private void EndFrontierBattle()
    {
        scriptOutcome = battle?.Result == BattleResult.PlayerVictory ? BattleOutcome.Won : BattleOutcome.Lost;
        battleMayBeLost = false;
        StartTransition(GameState.Overworld, () =>
        {
            PokemonModels.Trim(playerParty.Members.Select(p => p.ModelName));
            PlayFieldMusic();
        });
    }

    /// <summary>The party screen closed after the team was opened for a challenge: the script hears who was entered.</summary>
    private void TakeEntered()
    {
        enteredSlots = partyScreen.EnteredAll ? partyScreen.Entered.ToList() : Array.Empty<int>();
        scriptAnswer = partyScreen.EnteredAll ? 1 : 0;
    }

    private void UpdateFrontierRecords(float dt)
    {
        frontierRecordsScreen.Update(dt);
        if (!frontierRecordsScreen.IsActive) currentState = GameState.Overworld;
    }

    /// <summary>The Battle Points stand over the field while a script speaks of them, and go as it ends.</summary>
    private void DrawBattlePoints()
    {
        if (!ScriptRunning) showBattlePoints = false;
        if (showBattlePoints) ModernUi.PointsCount(frontier.BattlePoints);
    }

    /// <summary>The longest streak of any way the Tower is taken: the Trainer Card's star asks for a hundred (<c>TrainerCase_CalculateTrainerCardLevel</c>).</summary>
    private int TowerBestStreak => new[] { TowerMode.Single, TowerMode.Double, TowerMode.Multi, TowerMode.LinkMulti, TowerMode.Wifi }
        .Max(mode => frontier.Get(BattleTowerChallenge.RecordStat(mode)));

    private sealed partial class FieldHost
    {
        public FrontierRecords Frontier => game.frontier;
        public BattleTowerChallenge? Tower { get => game.tower; set => game.tower = value; }
        public int FrontierAct(string what, int number, NPC? subject) => game.FrontierAct(what, number, subject);
        public IReadOnlyList<int> Entered => game.enteredSlots;
        public void FrontierBattle(Party team, IReadOnlyList<Trainer> foes, Trainer? partner, bool doubles) => game.StartFrontierBattle(team, foes, partner, doubles);
    }
}
