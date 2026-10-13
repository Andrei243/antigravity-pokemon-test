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

        /// <summary>The game's states without the renderers, which everything below that is the game's and not its pictures reads (plan 16 · T11).</summary>
        public GameFlow Flow => game.flow;
        public GameState State { get => Flow.State; set => Flow.State = value; }
        public Map Map { get => Flow.Map; set => Flow.Map = value; }
        public Player Player => Flow.Player;
        public BattleEngine? Battle { get => Flow.Battle; set => Flow.Battle = value; }
        public StoryState Story => Flow.Story;
        public bool TrainersLookOnArrival { get => game.trainersLookOnArrival; set => game.trainersLookOnArrival = value; }
        public bool SteppedOutOfWarp { get => game.steppedOutOfWarp; set => game.steppedOutOfWarp = value; }

        // ------------------------------------------------------------ what the player has

        public Party Party => Flow.Party;
        public Inventory Bag => Flow.Bag;
        public Pokedex Pokedex => Flow.Pokedex;
        public PcBoxes Boxes { get => Flow.Boxes; set => Flow.Boxes = value; }
        public int Money { get => Flow.Money; set => Flow.Money = value; }
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
        public DialogueManager Dialogue { get => game.flow.Dialogue; set => game.flow.Dialogue = value; }
        public LocationSign LocationSign => game.flow.LocationSign;
        public TitleScreen Title { get => game.titleScreen; set => game.titleScreen = value; }
        public IntroScreen Intro => game.introScreen;
        public StartMenu StartMenu => game.flow.StartMenu;
        /// <summary>The window's close button and its question (plan 12 · Q1): asked as the button asks it, answered as a key would.</summary>
        public ClosePrompt ClosePrompt => game.closePrompt;
        public void RequestClose() => game.RequestClose();
        public void AnswerClose(CloseChoice choice)
        {
            if (game.closePrompt.IsOpen) game.closePrompt.Cancel();
            game.AnswerClose(choice);
        }
        public PartyScreen PartyScreen => game.flow.PartyScreen;
        public StarterSelectScreen StarterSelect => game.flow.StarterSelectScreen;
        public BagScreen BagScreen => game.flow.BagScreen;
        public TrainerCardScreen TrainerCard => game.flow.TrainerCardScreen;
        public SaveScreen SaveScreen => game.flow.SaveScreen;
        public ShopScreen Shop => game.flow.ShopScreen;
        public PCScreen PcScreen => game.flow.PCScreen;
        public HallOfFameScreen HallOfFameScreen => game.hallOfFameScreen;
        public JournalScreen JournalScreen => game.journalScreen;
        public PokedexScreen PokedexScreen => game.flow.PokedexScreen;
        public EvolutionScreen Evolution => game.evolutionScreen;
        public OptionsScreen Options => game.flow.OptionsScreen;
        public WardrobeScreen WardrobeScreen => game.wardrobeScreen;
        /// <summary>The map of the region: the Town Map, a wall's map, and Fly's choice of a place (plan 01 · M12).</summary>
        public TownMapScreen TownMapScreen => game.flow.TownMapScreen;

        /// <summary>Opens the map of the region as the game does, in a mode: out of the bag, on a wall, or for Fly with the first of the team.</summary>
        public bool OpenTownMap(TownMapMode mode) => game.OpenTownMap(mode, fromBag: false);

        /// <summary>The cells of the region's map the player last passed through.</summary>
        public MapTrail MapTrail => game.mapTrail;
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
            game.flow.Map = map;
            game.flow.Player.SetPosition(x, y, facing);
            game.flow.State = GameState.Overworld;
        }

        public void StartWildBattle(WildEncounterEntry entry) => game.StartWildBattle(entry);
        public void StartTrainerBattle(NPC trainer, NPC? second = null, Trainer? partner = null, bool firstBattle = false) =>
            game.StartTrainerBattle(trainer, second, partner, firstBattle);
        public void StartTransition(GameState next, Action? onMidpoint = null, TransitionKind kind = TransitionKind.Fade) =>
            game.StartTransition(next, onMidpoint, kind);
        /// <summary>How the scene last closed, or is closing: the way into a battle the game picked for whom it met and where (plan 09 · L13).</summary>
        public TransitionKind TransitionKind => game.flow.Kind;
        /// <summary>How long the scene has been closing, or opening on the next one, in seconds; and which of the two.</summary>
        public float TransitionSeconds => game.flow.Clock;
        public bool TransitionClosing => game.flow.State == GameState.Transition && game.flow.Closing;
        /// <summary>
        /// The band the scene is closing on (plan 09 · L15): whose it is with the portraits rendered so far, the look
        /// their portrait is of and the player's (null in a Leader's band), and how many of them are rendered; no
        /// band when the game opened the battle any other way.
        /// </summary>
        public BandPicture? Band => game.band.Picture;
        public string? BandTheirLook => game.band.TheirLook;
        public string? BandOwnLook => game.band.OwnLook;
        public int BandRendered => game.band.Rendered;
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
        /// <summary>
        /// The player has come to the area they stand in, as far as the game knows (plan 06 · R16c): its name, its music and
        /// its own arrival script, as a step across its border brings them. A tool that puts the player down in the middle
        /// of a place calls it, or the first step taken there counts as walking in and goes to that script, which comes
        /// before the step's wild Pokémon. True when the place had a script to start.
        /// </summary>
        public bool EnterArea() => game.EnterArea();
        /// <summary>Whoever walks behind the player now (plan 06 · R16c): a partner of the story or a Pokémon out of its ball; null with nobody.</summary>
        public Follower? Along => game.along;
        /// <summary>A Pokémon of the team walks behind the player as a thing of the map, as <c>amity walk</c> has it (plan 06 · R16c).</summary>
        public void WalkWith(NPC body, Pokemon pokemon) => game.WalkWith(body, pokemon);
        /// <summary>The game written to its slot's save where the player stands, as the save screen writes it.</summary>
        public bool Save() => game.WriteSave();
        /// <summary>
        /// The slot the game is saved in (plan 12 · Q2). A tool that wants its saves in one place whatever an earlier
        /// run left in its folder sets it.
        /// </summary>
        public int SaveSlot { get => game.saveSlot; set => game.saveSlot = SavePaths.IsSlot(value) ? value : throw new ArgumentOutOfRangeException(nameof(value)); }
        /// <summary>The file of the slot the game is saved in, in the folder the saves are kept in.</summary>
        public string SaveFile => SavePaths.Current.Slot(game.saveSlot);
        /// <summary>The slot the game is saved in deleted, its backup with it: a tool that saved takes its save away again.</summary>
        public bool DeleteSave() => SaveSlots.Delete(SavePaths.Current, game.saveSlot);
        /// <summary>The game as it stands, as the save screen would write it (plan 24 · X3: the harness's <c>saves</c>).</summary>
        public SaveData BuildSave() => game.BuildSave();

        // ------------------------------------------------------------ a scene skipped (plan 12 · Q20)

        /// <summary>A tool's hand on the menu button: held down while set, as the key is, and read where the key is read.</summary>
        public bool SkipHeld { get => game.SkipHeld; set => game.SkipHeld = value; }
        /// <summary>The key of the scene the script running is in (its file and its name); null outside one.</summary>
        public string? Scene => Flow.Runner.Scene;
        /// <summary>Whether that scene may be skipped now: the options allow it and nothing waits for the player.</summary>
        public bool SceneMayBeSkipped => game.MaySkipNow;
        /// <summary>How far the hold has come, 0 to 1: the ring's fill.</summary>
        public float SkipFill => game.skipHold.Fill;
        /// <summary>True from the hold's end until the skip's dark has opened again.</summary>
        public bool Skipping => game.skipPhase != SkipPhase.None;
        /// <summary>How many times a scene was hurried through since the game was made: once for each hold that did its work.</summary>
        public int ScenesSkipped => game.scenesSkipped;
    }
}
