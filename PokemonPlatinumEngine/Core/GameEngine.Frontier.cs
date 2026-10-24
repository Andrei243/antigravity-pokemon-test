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

    private bool showBattlePoints;

    /// <summary>A new game's Frontier: nothing won, and the Tower's draws started from the day's number (<c>sub_0206C008</c> in <c>game_start.c</c>).</summary>
    private void LoadFrontier(FrontierRecords? saved)
    {
        flow.Tower = null;
        flow.Factory = null;
        flow.Hall = null;
        flow.Castle = null;
        flow.Arcade = null;
        flow.EnteredSlots = Array.Empty<int>();
        if (saved != null)
        {
            flow.Frontier = saved;
            return;
        }
        flow.Frontier = new FrontierRecords();
        flow.Frontier.NewDay(flow.Encounters.DailyNumber);
    }

    private int FrontierAct(string what, int number, NPC? subject)
    {
        switch (what)
        {
            case "enter":
                // The team, for that many to be chosen in their order (BT_FUNC_UNK_30, the party menu's battle tower mode)
                flow.ScriptAnswer = 0;
                flow.EnteredSlots = Array.Empty<int>();
                flow.PartyScreen.ShowCondition = flow.Story.Has(ContestHallVisitedFlag);
                flow.PartyScreen.OpenToEnter(number, FrontierRules.MayEnter);
                flow.State = GameState.PartyMenu;
                return 0;
            case "records":
                flow.FrontierRecordsScreen.Open(FrontierBoard.For((FrontierFacility)number, flow.Frontier, flow.Story));
                flow.State = GameState.FrontierRecords;
                return 1;
            case "seen":
                return flow.Pokedex.IsSeen(number) ? 1 : 0;
            case "score":
                flow.TrainerScore = TrainerScore.Add(flow.TrainerScore, number);
                return 1;
            case "opponents":
                StandOpponents();
                return 1;
            case "partner":
                // Whoever stands as the partner in a room of the Tower takes the look and the name of the one chosen
                if (number >= 0 && number < BattleTowerChallenge.PartnerNames.Length && flow.Map.FindPerson(TowerPartnerPerson) is { } beside)
                {
                    beside.NpcType = BattleTowerChallenge.PartnerLooks[number];
                    beside.Name = BattleTowerChallenge.PartnerNames[number];
                    KeepCharactersNear(trim: false);
                }
                return 1;
            case "points":
                showBattlePoints = number != 0;
                return flow.Frontier.BattlePoints;
            case "save":
                SaveCurrentGame(quiet: true);
                AudioManager.PlaySound("save");
                return 1;
            case "journal":
                // The facility whose challenge goes up, by its number; the Tower's commands hand over none
                flow.Journal.Tell(new JournalEvent((FrontierFacility)number switch
                {
                    FrontierFacility.Factory => JournalEventKind.BattleFactory,
                    FrontierFacility.Hall => JournalEventKind.BattleHall,
                    FrontierFacility.Castle => JournalEventKind.BattleCastle,
                    FrontierFacility.Arcade => JournalEventKind.BattleArcade,
                    _ => JournalEventKind.BattleTower
                }));
                if (number == (int)FrontierFacility.Factory) AskForRentalPictures();
                if (number == (int)FrontierFacility.Castle) AskForCastlePictures();
                return 1;
            case "enterhall":
                // The team, for the Battle Hall's one Pokémon of level 30 or more to be chosen, or its two (PARTY_MENU_MODE_BATTLE_HALL)
                flow.ScriptAnswer = 0;
                flow.EnteredSlots = Array.Empty<int>();
                flow.PartyScreen.ShowCondition = flow.Story.Has(ContestHallVisitedFlag);
                flow.PartyScreen.OpenToEnter(number, BattleHallChallenge.MayEnter);
                flow.State = GameState.PartyMenu;
                return 0;
            case "board":
                OpenHallBoard();
                return 0;
            case "castleself":
                OpenCastleCounter(own: true);
                return 0;
            case "castlefoe":
                OpenCastleCounter(own: false);
                return 0;
            case "castlepoints":
                showCastlePoints = number != 0;
                return flow.Castle?.Points(flow.Frontier) ?? 0;
            case "rent":
                OpenFactoryRentals(number);
                return 0;
            case "trade":
                OpenFactoryTrade();
                return 0;
            case "arcadeboard":
                OpenArcadeBoard();
                return 0;
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
        var foes = flow.Runner.TowerFoes;
        for (int i = 0; i < foes.Count && i < TowerOpponentPeople.Length; i++)
        {
            if (flow.Map.FindPerson(TowerOpponentPeople[i]) is not { } person) continue;
            person.NpcType = FrontierTrainers.LookOf(foes[i].Record);
            person.Name = $"{foes[i].Record.Class} {foes[i].Record.Name}";
        }
        KeepCharactersNear(trim: false);
    }

    /// <summary>
    /// A battle of the Battle Frontier (<c>FieldBattleDTO_NewBattleTower</c>): the team is the battle's own, the foes
    /// are the trainers made for it, the ground is the Frontier's own, and nothing is won, lost or learnt by it.
    /// </summary>
    private void StartFrontierBattle(Party team, IReadOnlyList<Trainer> foes, Trainer? beside, bool doubles, FrontierConditions? conditions = null)
    {
        EndRadarChain();
        flow.ScriptOutcome = BattleOutcome.None;
        var trainers = foes.ToList();
        CharacterModels.Preload(trainers.Append(beside).OfType<Trainer>()
            .Select(t => CharacterStyles.Default.InBattle(t.TrainerClass, PlayerIdentity.CharacterFor(t.Look ?? "Trainer"))));
        eyeThemePlaying = false;
        AudioManager.PlayMusic(MusicDirector.BattleRole(trainers.Select(t => t.TrainerClass)), immediate: true);
        var shown = PrepareModels(trainers.SelectMany(t => t.Party.Members).Concat(beside?.Party.Members ?? Enumerable.Empty<Pokemon>()).Concat(team.Members));
        // BATTLE_TYPE_FRONTIER: the Frontier's own way in, a Brain's even in a double, and the double's for anyone else's (plan 09 · L13)
        var meeting = new BattleMeeting
        {
            Trainers = BattleMeeting.Of(trainers),
            Double = doubles,
            Frontier = true,
            Terrain = Battle.Sim.BattleTerrain.Special,
            TheirLevel = trainers[0].Party.Members[0].Level,
            OwnLevel = team.Members.FirstOrDefault()?.Level ?? 0
        };
        StartTransition(GameState.Battle, () =>
        {
            AwaitModels(shown);
            flow.Battle = new BattleEngine(new BattleSetup
            {
                PlayerParty = team,
                Inventory = flow.Bag,
                Pokedex = flow.Pokedex,
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
                    HasCaught = species => flow.Pokedex.IsCaught(species.DexNumber),
                    Badges = flow.Story.BadgeCount(Here.Id),
                    Player = PlayerMark,
                    // The weather or the Trick Room a facility's own rules open the battle under (the Battle Arcade's board, plan 06 · R18d)
                    Weather = conditions?.Weather ?? Battle.Sim.BattleWeather.None,
                    TrickRoom = conditions?.TrickRoom ?? false
                },
                MoveHints = MoveHintsOn,
                TrainerLines = TrainerLinesOn,
                // The Frontier never offers a switch (BATTLE_TYPE_FORCED_SET_MODE; plan 08 · P1), but plays as the scene says
                Scene = Settings.BattleScene
            });
            battleRenderer.SetArena(flow.Map, flow.Player.GridX, flow.Player.GridY);
            flow.BattleTrainers.Clear();
        }, SceneTransition.ForBattle(meeting));
    }

    /// <summary>
    /// A Frontier battle is over: the script that started it hears how it went, and the field comes back. No money,
    /// no EXP, no evolution, no waking up in a Pokémon Center: the battle's copies are let go and the team is as it was.
    /// </summary>
    private void EndFrontierBattle()
    {
        flow.ScriptOutcome = flow.Battle?.Result == BattleResult.PlayerVictory ? BattleOutcome.Won : BattleOutcome.Lost;
        // The turns it took, which the Battle Arcade scores a battle by (dto->totalTurnsElapsed)
        flow.ScriptTurns = flow.Battle?.Core.Turn ?? 0;
        flow.BattleMayBeLost = false;
        // The Vs. Recorder asks about it once the field is back, before the facility's script goes on (plan 08 · P11)
        KeepTheRecord(flow.Battle);
        StartTransition(GameState.Overworld, () =>
        {
            PokemonModels.Trim(flow.Party.Members.Select(p => p.LookName));
            PlayFieldMusic();
        });
    }

    /// <summary>The party screen closed after the team was opened for a challenge: the script hears who was entered.</summary>
    private void TakeEntered()
    {
        flow.EnteredSlots = flow.PartyScreen.EnteredAll ? flow.PartyScreen.Entered.ToList() : Array.Empty<int>();
        flow.ScriptAnswer = flow.PartyScreen.EnteredAll ? 1 : 0;
    }

    private void UpdateFrontierRecords(float dt)
    {
        flow.FrontierRecordsScreen.Update(dt);
        if (!flow.FrontierRecordsScreen.IsActive) flow.State = GameState.Overworld;
    }

    /// <summary>The Battle Points stand over the field while a script speaks of them, and go as it ends.</summary>
    private void DrawBattlePoints()
    {
        if (!ScriptRunning) showBattlePoints = false;
        if (showBattlePoints) ModernUi.PointsCount(flow.Frontier.BattlePoints);
    }

    /// <summary>The longest streak of any way the Tower is taken: the Trainer Card's star asks for a hundred (<c>TrainerCase_CalculateTrainerCardLevel</c>).</summary>
    private int TowerBestStreak => new[] { TowerMode.Single, TowerMode.Double, TowerMode.Multi, TowerMode.LinkMulti, TowerMode.Wifi }
        .Max(mode => flow.Frontier.Get(BattleTowerChallenge.RecordStat(mode)));

    private sealed partial class FieldHost
    {
        public FrontierRecords Frontier => game.flow.Frontier;
        public BattleTowerChallenge? Tower { get => game.flow.Tower; set => game.flow.Tower = value; }
        public int FrontierAct(string what, int number, NPC? subject) => game.FrontierAct(what, number, subject);
        public IReadOnlyList<int> Entered => game.flow.EnteredSlots;
        public void FrontierBattle(Party team, IReadOnlyList<Trainer> foes, Trainer? partner, bool doubles, FrontierConditions? conditions = null) =>
            game.StartFrontierBattle(team, foes, partner, doubles, conditions);
    }
}
