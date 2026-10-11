using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.PoketchApps;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The engine as a tool drives it (plan 16 · T5): the screenshot harness puts the game into known states, starts
/// battles and scenes and reads what they left behind through <see cref="Drive"/>, never by reflection. Everything
/// here is the engine's own state or one of its own steps, as the game takes them; nothing is a second way of doing
/// what the game does.
/// </summary>
public partial class GameEngine
{
    private Driver? driver;

    /// <summary>The engine's state and steps for a tool that drives it.</summary>
    internal Driver Drive => driver ??= new Driver(this);

    internal sealed class Driver
    {
        private readonly GameEngine game;
        public Driver(GameEngine game) { this.game = game; }

        // ------------------------------------------------------------ where the game is

        public GameState State { get => game.currentState; set => game.currentState = value; }
        public Map Map { get => game.currentMap; set => game.currentMap = value; }
        public Player Player => game.player;
        public BattleEngine? Battle { get => game.battle; set => game.battle = value; }
        public StoryState Story => game.story;
        public bool TrainersLookOnArrival { get => game.trainersLookOnArrival; set => game.trainersLookOnArrival = value; }
        public bool SteppedOutOfWarp { get => game.steppedOutOfWarp; set => game.steppedOutOfWarp = value; }

        // ------------------------------------------------------------ what the player has

        public Party Party => game.playerParty;
        public Inventory Bag => game.playerInventory;
        public Pokedex Pokedex => game.playerPokedex;
        public PcBoxes Boxes { get => game.pcBoxStorage; set => game.pcBoxStorage = value; }
        public int Money { get => game.playerMoney; set => game.playerMoney = value; }
        public int TrainerScore { get => game.trainerScore; set => game.trainerScore = value; }
        public HallOfFame HallOfFame { get => game.hallOfFame; set => game.hallOfFame = value; }
        public Journal Journal { get => game.journal; set => game.journal = value; }
        public Wardrobe Wardrobe => game.wardrobe;
        public Poketch Poketch => game.poketch;
        /// <summary>The Great Marsh's Safari Game (plan 01 · M7; its catches, plan 01 · M11, part b).</summary>
        public SafariGame Safari => game.safari;
        public SpecialEncounters Encounters => game.encounters;
        public BerryPatches Berries => game.berries;
        public PoffinCase Poffins => game.poffinCase;

        /// <summary>The Fashion Case, the Super Contest's screen and how the last contest came out (plan 06 · R17a).</summary>
        public Models.SuperContests.FashionCase FashionCase => game.fashionCase;
        public SuperContestScreen SuperContest => game.contestScreen;
        public Models.SuperContests.ContestOutcome? ContestOutcome => game.contestOutcome;

        /// <summary>The Seal Case, its screens, the Fashion Case's viewer and the contests' records (plan 06 · R17b).</summary>
        public SealCase SealCase => game.sealCase;
        public SealCaseScreen BallCapsules => game.sealCaseScreen;

        /// <summary>The Underground (plan 06 · R16a): its state and rules, its screens, the trap that has the player, and the game's own ways down and up.</summary>
        public Models.Underground.UndergroundState Underground => game.underground;
        public Models.Pastimes Pastimes => game.pastimes;
        public UI.SlotMachineScreen Slots => game.slotScreen;
        /// <summary>A step of Pal Park's Catching Show where the player stands (the engine's own, as a step in the grass takes it).</summary>
        public bool ParkStep() => game.PalParkStep();
        /// <summary>Stands the Villa's furniture in its room as the flags say (the engine's own, as arriving there does).</summary>
        public void FurnishVilla() => game.FurnishVilla();
        /// <summary>The Battle Frontier (plan 06 · R18a): its records, the Tower's challenge under way and a facility's board.</summary>
        public Models.Frontier.FrontierRecords Frontier => game.frontier;
        public Models.Frontier.BattleTowerChallenge? Tower => game.tower;
        public FrontierRecordsScreen FrontierBoard => game.frontierRecordsScreen;
        /// <summary>The Battle Factory (plan 06 · R18b): the challenge under way and the screen its rentals are chosen and traded on.</summary>
        public Models.Frontier.BattleFactoryChallenge? Factory => game.factory;
        public FactoryScreen FactoryRentals => game.factoryScreen;
        /// <summary>The Battle Hall and the Battle Castle (plan 06 · R18c): the challenges under way, the board of types and the valet's counters.</summary>
        public Models.Frontier.BattleHallChallenge? Hall => game.hall;
        public HallBoardScreen HallBoard => game.hallBoard;
        public Models.Frontier.BattleCastleChallenge? Castle => game.castle;
        public CastleScreen CastleCounter => game.castleScreen;
        /// <summary>The Battle Arcade (plan 06 · R18d): the challenge under way and its game board.</summary>
        public Models.Frontier.BattleArcadeChallenge? Arcade => game.arcade;
        public ArcadeBoardScreen ArcadeBoard => game.arcadeBoard;
        public Models.Underground.UndergroundField Tunnels => game.Tunnels;
        public MiningScreen Mining => game.miningScreen;
        public UndergroundScreen UndergroundLists => game.undergroundScreen;
        public DecorationScreen Decoration => game.decorationScreen;
        public Models.Underground.TrapEffect? Trap => game.trap;
        public IReadOnlyList<Models.Underground.Digger> Diggers => game.diggers;
        public void GoUnderground() => game.GoUnderground();
        public void ComeUp() => game.ComeUp();
        public void PulseRadar() => game.PulseRadar();
        public void OpenUndergroundBag(int tab = 0) => game.OpenUndergroundBag(tab);
        public void SpringTrap(int trap) => game.SpringTrap(trap);
        public void OpenBaseDecoration() => game.OpenDecoration();
        public void UseDiggerDrill(int slot) => game.UseDiggerDrill(slot);
        public SealShopScreen SealShop => game.sealShopScreen;
        public FashionCaseScreen FashionCaseScreen => game.fashionScreen;
        public Models.SuperContests.ContestRecords ContestRecords => game.contestRecords;
        public void OpenBallCapsules() => game.OpenBallCapsules(fromBag: false);
        public void OpenFashionCase() => game.OpenFashionCase(fromBag: false);
        public DayCare DayCare => game.dayCare;
        public RadarChain Radar => game.radar;
        public FishingAttempt? Fishing { get => game.fishing; set => game.fishing = value; }

        /// <summary>The Eterna Gym's clock turning on, which its script waits for (plan 01 · M9).</summary>
        public EternaClock.Turn? ClockTurn => game.clockTurn;
        /// <summary>A Veilstone Gym bag is on its run, holding the field.</summary>
        public bool BagRunning => game.bagRun != null;

        // ------------------------------------------------------------ what draws it

        public RenderTexture2D VirtualScreen => game.virtualScreen;
        public RenderContext RenderContext => game.renderContext;
        public WorldRenderer World => game.world;
        public BattleRenderer BattleRenderer => game.battleRenderer;
        public DialogueManager Dialogue { get => game.dialogue; set => game.dialogue = value; }
        public LocationSign LocationSign => game.locationSign;
        public TitleScreen Title { get => game.titleScreen; set => game.titleScreen = value; }
        public IntroScreen Intro => game.introScreen;
        public StartMenu StartMenu => game.startMenu;
        public PartyScreen PartyScreen => game.partyScreen;
        public StarterSelectScreen StarterSelect => game.starterSelectScreen;
        public BagScreen BagScreen => game.bagScreen;
        public TrainerCardScreen TrainerCard => game.trainerCardScreen;
        public SaveScreen SaveScreen => game.saveScreen;
        public ShopScreen Shop => game.shopScreen;
        public PCScreen PcScreen => game.pcScreen;
        public HallOfFameScreen HallOfFameScreen => game.hallOfFameScreen;
        public JournalScreen JournalScreen => game.journalScreen;
        public PokedexScreen PokedexScreen => game.pokedexScreen;
        public EvolutionScreen Evolution => game.evolutionScreen;
        public OptionsScreen Options => game.optionsScreen;
        public WardrobeScreen WardrobeScreen => game.wardrobeScreen;
        public FlyScreen FlyScreen => game.flyScreen;
        public PoffinCookingScreen Cooking => game.cookingScreen;
        public PoffinCaseScreen PoffinCaseScreen => game.poffinCaseScreen;
        public HatchScreen Hatch => game.hatchScreen;

        /// <summary>The Hall of Fame's ceremony and the credits (plan 02 · S14).</summary>
        public HallOfFameCeremony Ceremony => game.ceremony;
        public CreditsScreen Credits => game.credits;
        public PoketchView PoketchView => game.poketchView;
        public PoketchContext PoketchContext => game.PoketchNow();

        // ------------------------------------------------------------ the game's own steps

        /// <summary>Puts the player on a tile of the map in the field.</summary>
        public void Place(Map map, int x, int y, Direction facing)
        {
            game.currentMap = map;
            game.player.SetPosition(x, y, facing);
            game.currentState = GameState.Overworld;
        }

        public void StartWildBattle(WildEncounterEntry entry) => game.StartWildBattle(entry);
        public void StartTrainerBattle(NPC trainer, NPC? second = null, Trainer? partner = null, bool firstBattle = false) =>
            game.StartTrainerBattle(trainer, second, partner, firstBattle);
        public void StartTransition(GameState next, Action? onMidpoint = null, TransitionKind kind = TransitionKind.Fade) =>
            game.StartTransition(next, onMidpoint, kind);
        /// <summary>The player presses the confirm key in the field: talking, reading, a field move's question.</summary>
        public void Interact() => game.TryInteract();
        /// <summary>What arriving on the current map does: its puzzle laid out, its <c>OnEnter</c>, its first arrival.</summary>
        public void ArriveOnMap() => game.ArriveOnMap();
        public void ChooseFromStartMenu(StartMenuChoice choice) => game.HandleStartMenuChoice(choice);
        public void UseFieldItem(ItemData item) => game.UseFieldItem(item);
        public EncounterMoment EncounterMomentNow() => game.EncounterMomentNow();
        public void KeepPartnerAlong() => game.KeepPartnerAlong();
        /// <summary>The trainer who travels with the player now (plan 02 · S6), or null with nobody along.</summary>
        public string? PartnerAlong => game.partner?.TrainerId;
        /// <summary>The Day Care's part of a step (plan 06 · R15): the couple walk it, and an Egg may be ready to hatch.</summary>
        public bool DayCareStep() => game.DayCareStep();
    }
}
