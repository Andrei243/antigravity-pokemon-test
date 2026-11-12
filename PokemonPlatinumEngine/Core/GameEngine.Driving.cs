using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
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

    internal sealed partial class Driver
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
        public bool TrainersLookOnArrival { get => game.flow.TrainersLookOnArrival; set => game.flow.TrainersLookOnArrival = value; }
        public bool SteppedOutOfWarp { get => game.flow.SteppedOutOfWarp; set => game.flow.SteppedOutOfWarp = value; }

        // ------------------------------------------------------------ what the player has

        public Party Party => Flow.Party;
        public Inventory Bag => Flow.Bag;
        public Pokedex Pokedex => Flow.Pokedex;

        /// <summary>The DexNav's state and the rustle it has found in the grass (plan 10 · F11, F12).</summary>
        public Overworld.DexNavState DexNav => Flow.DexNav;
        public Overworld.Rustle? DexNavRustle => game.DexNavRustle;

        /// <summary>A find put hiding in the grass at a tile, the DexNav switched on (false where nothing lives), and taken away again.</summary>
        public bool PlaceDexNavRustle(int x, int y) => game.PlaceDexNavRustle(x, y);
        public void ForgetDexNavRustle() => game.ForgetDexNavRustle();

        /// <summary>The Pokédex's HERE list for where the player stands, as the engine tells the screen (null without the DexNav).</summary>
        public IReadOnlyList<Overworld.DexNavEntry>? DexNavHere => game.DexNavHereList();
        public PcBoxes Boxes { get => Flow.Boxes; set => Flow.Boxes = value; }

        /// <summary>The teams kept at the PC (plan 12 · Q27).</summary>
        public System.Collections.Generic.List<SavedTeam> Teams { get => Flow.Teams; set => Flow.Teams = value; }
        public int Money { get => Flow.Money; set => Flow.Money = value; }
        public int TrainerScore { get => game.flow.TrainerScore; set => game.flow.TrainerScore = value; }
        public HallOfFame HallOfFame { get => game.flow.HallOfFame; set => game.flow.HallOfFame = value; }
        public Journal Journal { get => game.flow.Journal; set => game.flow.Journal = value; }
        public Wardrobe Wardrobe => game.flow.Wardrobe;
        public Poketch Poketch => game.flow.Poketch;
        /// <summary>Cameron's photo album, and the album opened as his father opens it (plan 20, part u).</summary>
        public PhotoAlbum Photos => game.flow.Photos;
        public PhotoAlbumScreen PhotoAlbumScreen => game.flow.PhotoAlbumScreen;
        public void OpenPhotoAlbum() => game.OpenPhotoAlbum();
        /// <summary>The Great Marsh's Safari Game (plan 01 · M7; its catches, plan 01 · M11, part b).</summary>
        public SafariGame Safari => game.flow.Safari;
        /// <summary>The National Park's Bug-Catching Contest, and its board of results (plan 15 · E5).</summary>
        public BugContest BugContest => game.flow.BugContest;
        /// <summary>The Pokéathlon's records and its screen (plan 15 · E6).</summary>
        public AthlonRecords Athlon => game.flow.Athlon;
        public AthlonScreen AthlonScreen => game.flow.AthlonScreen;
        // Hoenn's contests and Pokéblocks (plan 21 · H11)
        public AppealScreen AppealScreen => game.flow.AppealScreen;
        public BlenderScreen BlenderScreen => game.flow.BlenderScreen;
        public PokeblockCaseScreen PokeblockCaseScreen => game.flow.PokeblockCaseScreen;
        public Models.Contest.HoennContestRecords HoennContests => game.flow.HoennContests;
        public Models.PokeblockCase PokeblockCase => game.flow.PokeblockCase;
        public void OpenPokeblockCase() => game.OpenPokeblockCase(false);
        public void BeginBlender(ItemData berry, int others, Random chance) => game.BeginBlender(berry, others, chance);
        public void ShowContestPainting(ContestStat category) => game.ShowContestPainting(category);
        public void HoldAppeals(int slot, Models.ContestStat category, Models.Contest.AppealRank rank) => game.HoldAppeals(slot, category, rank);
        public BugContestResultsScreen BugContestResults => game.flow.BugContestResultsScreen;
        public SpecialEncounters Encounters => game.flow.Encounters;
        public BerryPatches Berries => game.flow.Berries;
        public PoffinCase Poffins => game.flow.PoffinCase;

        /// <summary>The Fashion Case, the Super Contest's screen and how the last contest came out (plan 06 · R17a).</summary>
        public Models.SuperContests.FashionCase FashionCase => game.flow.FashionCase;
        public SuperContestScreen SuperContest => game.flow.SuperContestScreen;
        public Models.SuperContests.ContestOutcome? ContestOutcome => game.flow.ContestOutcome;

        /// <summary>The Seal Case, its screens, the Fashion Case's viewer and the contests' records (plan 06 · R17b).</summary>
        public SealCase SealCase => game.flow.SealCase;
        public SealCaseScreen BallCapsules => game.flow.SealCaseScreen;

        /// <summary>The Underground (plan 06 · R16a): its state and rules, its screens, the trap that has the player, and the game's own ways down and up.</summary>
        public Models.Underground.UndergroundState Underground => game.flow.Underground;
        public Models.Pastimes Pastimes => game.flow.Pastimes;
        public UI.SlotMachineScreen Slots => game.flow.SlotMachineScreen;
        public UI.ReelMachineScreen Reels => game.flow.ReelMachineScreen;
        public UI.RouletteScreen Roulette => game.flow.RouletteScreen;
        /// <summary>A step of Pal Park's Catching Show where the player stands (the engine's own, as a step in the grass takes it).</summary>
        public bool ParkStep() => game.PalParkStep();
        /// <summary>Stands the Villa's furniture in its room as the flags say (the engine's own, as arriving there does).</summary>
        public void FurnishVilla() => game.FurnishVilla();
        /// <summary>The Battle Frontier (plan 06 · R18a): its records, the Tower's challenge under way and a facility's board.</summary>
        public Models.Frontier.FrontierRecords Frontier => game.flow.Frontier;
        public Models.Frontier.BattleTowerChallenge? Tower => game.flow.Tower;
        public FrontierRecordsScreen FrontierBoard => game.flow.FrontierRecordsScreen;
        /// <summary>The Battle Factory (plan 06 · R18b): the challenge under way and the screen its rentals are chosen and traded on.</summary>
        public Models.Frontier.BattleFactoryChallenge? Factory => game.flow.Factory;
        public FactoryScreen FactoryRentals => game.flow.FactoryScreen;
        /// <summary>The Battle Hall and the Battle Castle (plan 06 · R18c): the challenges under way, the board of types and the valet's counters.</summary>
        public Models.Frontier.BattleHallChallenge? Hall => game.flow.Hall;
        public HallBoardScreen HallBoard => game.flow.HallBoardScreen;
        /// <summary>The Battle Dome (plan 14 · B8): the tournament under way and its bracket.</summary>
        public Models.Frontier.BattleDomeChallenge? Dome => game.flow.Dome;
        public BracketScreen Bracket => game.flow.BracketScreen;
        public Models.Tournament? Tournament => game.flow.Tournament;
        public TournamentScreen TournamentScreen => game.flow.TournamentScreen;
        /// <summary>The Battle Palace's or the Battle Arena's round under way (plan 14 · B9).</summary>
        public Models.Frontier.BattleVenueChallenge? Venue => game.flow.Venue;
        /// <summary>A later battle place's challenge under way (plan 37 · FC2: the streak house).</summary>
        public Models.Facilities.StreakChallenge? Facility => game.flow.Facility;
        /// <summary>The Battle Pike's and the Battle Pyramid's round under way (plan 14 · B10).</summary>
        public Models.Frontier.BattlePikeChallenge? Pike => game.flow.Pike;
        public Models.Frontier.BattlePyramidChallenge? Pyramid => game.flow.Pyramid;
        /// <summary>A stone panel of the Ruins of Alph, open while a chamber's script waits on it (plan 20 · J5).</summary>
        public AlphPuzzleScreen AlphPuzzle => game.flow.AlphPuzzleScreen;
        /// <summary>The phrase picker, open while a script's <c>phrase</c> waits on it (plan 08 · P9).</summary>
        public PhraseScreen Phrases => game.flow.PhraseScreen;
        /// <summary>The gift box and the trade board (plan 40 · GE7, GE8): their screens, open while a script's <c>gift open</c> or <c>trade board open</c> waits, and what the save keeps of each.</summary>
        public GiftBoxScreen GiftBoxScreen => game.flow.GiftBoxScreen;
        public TradeBoardScreen TradeBoardScreen => game.flow.TradeBoardScreen;
        public Models.GiftBox Gifts => game.flow.Gifts;
        public Models.TradeBoard TradeBoardState => game.flow.Board;
        public Models.Frontier.BattleCastleChallenge? Castle => game.flow.Castle;
        public CastleScreen CastleCounter => game.flow.CastleScreen;
        /// <summary>The Battle Arcade (plan 06 · R18d): the challenge under way and its game board.</summary>
        public Models.Frontier.BattleArcadeChallenge? Arcade => game.flow.Arcade;
        public ArcadeBoardScreen ArcadeBoard => game.flow.ArcadeBoardScreen;
        public Models.Underground.UndergroundField Tunnels => game.Tunnels;
        public MiningScreen Mining => game.flow.MiningScreen;
        public UndergroundScreen UndergroundLists => game.flow.UndergroundScreen;
        public DecorationScreen Decoration => game.flow.DecorationScreen;
        public Models.Underground.TrapEffect? Trap => game.flow.Trap;
        public IReadOnlyList<Models.Underground.Digger> Diggers => game.flow.Diggers;
        public void GoUnderground() => game.GoUnderground();
        public void ComeUp() => game.ComeUp();
        public void PulseRadar() => game.PulseRadar();
        public void OpenUndergroundBag(int tab = 0) => game.OpenUndergroundBag(tab);
        public void SpringTrap(int trap) => game.SpringTrap(trap);
        public void OpenBaseDecoration() => game.OpenDecoration();
        /// <summary>Hoenn's secret bases (plan 21 · H10): the player's base, the decoration PC, and the base's decorating opened.</summary>
        public Models.SecretBaseSave? SecretBase => game.flow.SecretBase;
        public List<Models.StoredDecoration> Decorations => game.flow.Decorations;
        public void OpenSecretBaseDecoration() => game.OpenSecretBaseDecoration();
        public void UseDiggerDrill(int slot) => game.UseDiggerDrill(slot);
        public SealShopScreen SealShop => game.flow.SealShopScreen;
        public FashionCaseScreen FashionCaseScreen => game.flow.FashionCaseScreen;
        public Models.SuperContests.ContestRecords ContestRecords => game.flow.ContestRecords;
        public void OpenBallCapsules() => game.OpenBallCapsules(fromBag: false);
        public void OpenFashionCase() => game.OpenFashionCase(fromBag: false);
        public ApricornBoxScreen ApricornBoxScreen => game.flow.ApricornBoxScreen;
        public ApricornBox Apricorns => game.flow.Apricorns;
        public void OpenApricornBox() => game.OpenApricornBox(fromBag: false);
        public CraftingScreen CraftingScreen => game.flow.CraftingScreen;
        public CraftingState Crafting => game.flow.Crafting;
        /// <summary>Pennant Hall and the Week's Work (plan 41 · EG3, EG6): their state, the hall furnished afresh, the board's screen.</summary>
        public PlazaState Plaza => game.flow.Plaza;
        public MissionBoard Missions => game.flow.Missions;
        public MissionBoardScreen MissionBoardScreen => game.flow.MissionBoardScreen;
        public void FurnishPlaza() => game.FurnishPlaza();
        public void TellTheBoard(MissionEventKind kind, string area = "", Pokemon? caught = null) => game.TellTheBoard(kind, area, caught);
        public PowerState Powers => game.flow.Powers;
        public void OpenCrafting(bool atMaker = false) => game.OpenCrafting(fromBag: false, atMaker: atMaker);
        public void OpenCraftingAt(CraftingPlace place) => game.OpenCrafting(fromBag: false, place);
        public TrainingScreen TrainingScreen => game.flow.TrainingScreen;
        public TrainingRecords Training => game.flow.Training;
        public Toast Toast => game.flow.Toast;
        public Random FieldRandom { get => game.fieldRandom; set => game.fieldRandom = value; }
        public DayCare DayCare => game.flow.DayCare;
        public RadarChain Radar => game.flow.Radar;
        public FishingAttempt? Fishing { get => game.flow.Fishing; set => game.flow.Fishing = value; }

        /// <summary>The Eterna Gym's clock turning on, which its script waits for (plan 01 · M9).</summary>
        public EternaClock.Turn? ClockTurn => game.flow.ClockTurn;
        /// <summary>A Veilstone Gym bag is on its run, holding the field.</summary>
        public bool BagRunning => game.bagRun != null;

        // ------------------------------------------------------------ what draws it

        public RenderTexture2D VirtualScreen => game.virtualScreen;
        public RenderContext RenderContext => game.renderContext;
        public WorldRenderer World => game.world;
        public BattleRenderer BattleRenderer => game.battleRenderer;
        public DialogueManager Dialogue { get => game.flow.Dialogue; set => game.flow.Dialogue = value; }
        public LocationSign LocationSign => game.flow.LocationSign;
        /// <summary>What is left in the area underfoot (plan 12 · Q22), or null with the options' row off.</summary>
        public Models.AreaCounts? AreaCounts => game.AreaCountsNow();
        /// <summary>A script's own black screen (the crossing's dark, plan 18 · W5): how far it has closed.</summary>
        public UI.Kit.ScreenFade ScriptFade => game.flow.ScriptFade;
        public TitleScreen Title { get => game.flow.TitleScreen; set => game.flow.TitleScreen = value; }
        public IntroScreen Intro => game.flow.IntroScreen;
        public StartMenu StartMenu => game.flow.StartMenu;
        /// <summary>The window's close button and its question (plan 12 · Q1): asked as the button asks it, answered as a key would.</summary>
        public ClosePrompt ClosePrompt => game.closePrompt;
        public void RequestClose() => game.RequestClose();
        public void AnswerClose(CloseChoice choice)
        {
            if (game.closePrompt.IsOpen) game.closePrompt.Cancel();
            game.AnswerClose(choice);
        }
        /// <summary>A battle lost's TRY AGAIN or GIVE UP (plan 12 · Q25): the question, and an answer given as a key would give it.</summary>
        public RetryPrompt RetryPrompt => game.retryPrompt;
        public void AnswerRetry(RetryChoice choice)
        {
            var given = game.retryPrompt.IsOpen ? (choice == RetryChoice.GiveUp ? game.retryPrompt.Cancel() : game.retryPrompt.Confirm()) : choice;
            game.AnswerRetry(given);
        }
        /// <summary>Whether the close button would offer SUSPEND AND QUIT now (plan 12 · Q25).</summary>
        public bool CanSuspend => game.CanSuspendNow;
        /// <summary>A quit answered and then not acted on, as a tool that goes on after showing it must (the harness's suspended battle).</summary>
        public void ForgetQuit() => game.QuitRequested = false;
        public PartyScreen PartyScreen => game.flow.PartyScreen;
        public StarterSelectScreen StarterSelect => game.flow.StarterSelectScreen;
        public BagScreen BagScreen => game.flow.BagScreen;
        public TrainerCardScreen TrainerCard => game.flow.TrainerCardScreen;
        public SaveScreen SaveScreen => game.flow.SaveScreen;
        public ShopScreen Shop => game.flow.ShopScreen;
        public PCScreen PcScreen => game.flow.PCScreen;
        public HallOfFameScreen HallOfFameScreen => game.flow.HallOfFameScreen;
        public JournalScreen JournalScreen => game.flow.JournalScreen;
        public PokedexScreen PokedexScreen => game.flow.PokedexScreen;
        public EvolutionScreen Evolution => game.flow.EvolutionScreen;
        public OptionsScreen Options => game.flow.OptionsScreen;
        public WardrobeScreen WardrobeScreen => game.flow.WardrobeScreen;
        /// <summary>The map of the region: the Town Map, a wall's map, and Fly's choice of a place (plan 01 · M12).</summary>
        public TownMapScreen TownMapScreen => game.flow.TownMapScreen;

        /// <summary>Opens the map of the region as the game does, in a mode: out of the bag, on a wall, or for Fly with the first of the team.</summary>
        public bool OpenTownMap(TownMapMode mode) => game.OpenTownMap(mode, fromBag: false);

        /// <summary>The cells of the region's map the player last passed through.</summary>
        public MapTrail MapTrail => game.flow.MapTrail;
        public PoffinCookingScreen Cooking => game.flow.PoffinCookingScreen;
        public PoffinCaseScreen PoffinCaseScreen => game.flow.PoffinCaseScreen;
        public HatchScreen Hatch => game.flow.HatchScreen;

        // Locked hearts (plan 44 · SD2): the opening's scene, the record, and a heart opened as the game opens one
        public HeartOpeningScreen HeartOpening => game.flow.HeartOpeningScreen;
        public HeartsRecord Hearts => game.flow.Hearts;
        public HeartOpening OpenHeart(Pokemon pokemon) => game.OpenTheHeart(pokemon);
        public void LowerTheLock(Pokemon pokemon, int by) => game.LowerTheLock(pokemon, by);

        /// <summary>The Pokégear (plan 20 · J3): its screen, opened as the start menu opens it (on a card), and what the radio plays.</summary>
        public PokegearScreen PokegearScreen => game.flow.PokegearScreen;
        public void OpenPokegear(PokegearCard? on = null) => game.OpenPokegear(on);
        public PokeNavScreen PokeNavScreen => game.flow.PokeNavScreen;
        public void OpenPokeNav(PokeNavPage? on = null) => game.OpenPokeNav(on);
        public RadioSong RadioSong => game.radioSong;
        /// <summary>The phone's clock: a tool moves it on so that the next step may ring (the ten minutes HeartGold waits).</summary>
        public PhoneRinger PhoneRinger => game.phoneRinger;
        /// <summary>A call as the game makes it: the player ringing a contact, or a contact ringing the player.</summary>
        public void PlaceCall(PhoneContact contact, bool incoming) => game.PlaceCall(contact, incoming);
        /// <summary>A step's chance of a contact ringing, asked at once.</summary>
        public bool PhoneRingsOnAStep() => game.PhoneRingsOnAStep();

        /// <summary>The Hall of Fame's ceremony and the credits (plan 02 · S14).</summary>
        public HallOfFameCeremony Ceremony => game.flow.Ceremony;
        public CreditsScreen Credits => game.flow.Credits;
        public PoketchView PoketchView => game.flow.PoketchView;
        public PoketchContext PoketchContext => game.PoketchNow();

        // ------------------------------------------------------------ the game's own steps

        /// <summary>Puts the player on a tile of the map in the field.</summary>
        public void Place(Map map, int x, int y, Direction facing)
        {
            game.flow.Map = map;
            game.flow.Player.SetPosition(x, y, facing);
            game.flow.State = GameState.Overworld;
            // (on the grid: out of an open map, if the player was in one, plan 32 · OW1)
            game.flow.Open = null;
        }

        // ------------------------------------------------------------ the open world (plan 32 · OW1)

        /// <summary>The open field in force (the map, the player in it, the field's clock), or null on the grid.</summary>
        public OpenWorld.OpenField? OpenField => game.flow.Open;

        /// <summary>The open map the player is in, or null on the grid.</summary>
        public OpenWorld.OpenMap? OpenMap => game.flow.Open?.Map;

        /// <summary>The player in the open map, or null on the grid.</summary>
        public OpenWorld.OpenPlayer? OpenPlayer => game.flow.Open?.Player;

        /// <summary>An open map the game is to know by name, as a loaded save asks for it (a grid map's name is refused).</summary>
        public void AddOpenMap(OpenWorld.OpenMap map) => game.AddOpenMap(map);

        /// <summary>Puts the player in an open map at a place, facing a way (65,536ths of a turn), the field's clock begun afresh.</summary>
        public void PlaceOpen(OpenWorld.OpenMap map, OpenWorld.OpenPos pos, int yaw) => game.EnterOpenField(map, pos, yaw);

        /// <summary>Puts the player in an open map on one of its markers, facing as the marker does.</summary>
        public void PlaceOpen(OpenWorld.OpenMap map, string marker)
        {
            var at = map.Marker(marker) ?? throw new ArgumentException($"{map.Name} has no marker {marker}");
            game.EnterOpenField(map, at.Pos, at.Yaw);
        }

        /// <summary>Crosses to another region as the attendant's travel does (plan 18 · W4, part b), behind a fade.</summary>
        public void TravelTo(string region) => game.TravelTo(RegionDatabase.Get(region)!);
        /// <summary>The keyboard naming a region's rival (plan 18 · W4, part b).</summary>
        public UI.RivalNamingScreen RivalNaming => game.flow.RivalNamingScreen;
        /// <summary>The keyboard naming a Pokémon (plan 12 · Q19).</summary>
        public UI.NicknameScreen Nickname => game.flow.NicknameScreen;

        public void StartWildBattle(WildEncounterEntry entry) => game.StartWildBattle(entry);
        public void StartTrainerBattle(NPC trainer, NPC? second = null, Trainer? partner = null, bool firstBattle = false) =>
            game.StartTrainerBattle(trainer, second, partner, firstBattle);
        /// <summary>A Battle Royal against three trainers of the map, each for themselves (plan 14 · B13), begun as any trainer's battle.</summary>
        public void StartBattleRoyal(NPC first, NPC second, NPC third) =>
            game.StartTrainerBattle(first, second, thirdNpc: third);
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
        /// <summary>A field move chosen in the party menu by the Pokémon at that place of the team (-1: a wild helper), as the menu's own choice does.</summary>
        public void UseFieldMove(FieldMove move, int partyIndex) => game.UseFieldMoveFromMenu(move, partyIndex);
        /// <summary>What arriving on the current map does: its puzzle laid out, its <c>OnEnter</c>, its first arrival.</summary>
        public void ArriveOnMap() => game.ArriveOnMap();
        public void ChooseFromStartMenu(StartMenuChoice choice) => game.HandleStartMenuChoice(choice);
        public void UseFieldItem(ItemData item) => game.UseFieldItem(item);
        public EncounterMoment EncounterMomentNow() => game.EncounterMomentNow();
        /// <summary>
        /// What the field does behind a fade once the calendar has turned (plan 22 · U3, U4): the season taken, its
        /// chunks laid, the team's coats changed and the notice shown. A tool that moves the calendar calls it in place
        /// of waiting for the next door.
        /// </summary>
        public void TurnTheSeason() => game.TurnTheSeason();
        /// <summary>The season the field holds now, whatever region the player is in (plan 22 · U3).</summary>
        public Season FieldSeason => game.season;
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
        public Follower? Along => game.flow.Along;
        /// <summary>The lead the options' FOLLOWER row has out of its ball now (plan 10 · F3); null when none is.</summary>
        public Follower? Lead => game.Lead;
        /// <summary>The lead spoken to as the player faces it (plan 10 · F4): true when it answered.</summary>
        public bool TalkToTheLead() => game.leadBody is { } body && game.TalkToTheLead(body);
        /// <summary>The wild Pokémon standing in sight with the WILD POKéMON row at VISIBLE (plan 10 · F5).</summary>
        public FieldPokemon WildInSight => game.WildInSight;
        /// <summary>A frame of the wild Pokémon in sight (plan 10 · F5), as the field runs it: true when one was met.</summary>
        public bool KeepWildInSight(float dt) => game.KeepWildInSight(dt);
        /// <summary>One in sight met, as walking into it meets it (plan 10 · F5).</summary>
        public bool MeetSeen(FieldPokemon.Wild seen) => game.MeetSeen(seen);
        /// <summary>A Pokémon of the team walks behind the player as a thing of the map, as <c>amity walk</c> has it (plan 06 · R16c).</summary>
        public void WalkWith(NPC body, Pokemon pokemon) => game.WalkWith(body, pokemon);
        /// <summary>The game written to its slot's save where the player stands, as the save screen writes it.</summary>
        public bool Save() => game.WriteSave();

        /// <summary>The ways to play this game was begun with (plan 13 · V1).</summary>
        public Models.RunRules Run => game.flow.Run;
        /// <summary>The level the run's cap holds the player's Pokémon to now (plan 13 · V3), null for none.</summary>
        public int? LevelCap => game.CurrentLevelCap();
        /// <summary>What the run remembers (plan 13 · V4): the places met, the graveyard, its end.</summary>
        public Models.RunState RunState => game.flow.RunState;

        /// <summary>What the game has counted (plan 13 · V8).</summary>
        public Models.Statistics Statistics => game.flow.Statistics;

        /// <summary>The medals won (plan 13 · V9), and the notice that tells of one.</summary>
        public Models.Medals Medals { get => game.flow.Medals; set => game.flow.Medals = value; }
        public MedalNotice MedalNotice => game.flow.MedalNotice;
        /// <summary>What the medals are judged by now (plan 13 · V9).</summary>
        public Models.MedalFacts MedalFacts => game.MedalFactsNow();
        /// <summary>A Pokémon caught under the Nuzlocke whose name the keyboard will ask once the field is back (plan 13 · V4).</summary>
        public Pokemon? NameAfterBattle { get => game.flow.NameAfterBattle; set => game.flow.NameAfterBattle = value; }
        /// <summary>
        /// The slot the game is saved in (plan 12 · Q2). A tool that wants its saves in one place whatever an earlier
        /// run left in its folder sets it.
        /// </summary>
        public int SaveSlot { get => game.flow.SaveSlot; set => game.flow.SaveSlot = SavePaths.IsSlot(value) ? value : throw new ArgumentOutOfRangeException(nameof(value)); }
        /// <summary>The seconds played (plan 12 · Q3: a test moves the clock on past the autosave's gap).</summary>
        public float PlayTime { get => game.flow.PlayTime; set => game.flow.PlayTime = value; }
        /// <summary>The autosave's disc shown at a share (0 to 1) of its time, or put away with a negative one: a picture of it without a write (plan 12 · Q3).</summary>
        public void ShowAutosaveDisc(float share) => game.autosaveDisc = share < 0f ? -1f : share * AutosaveDiscSeconds;
        /// <summary>The autosaves this game has written (plan 12 · Q3).</summary>
        public int AutosavesWritten => game.AutosavesWritten;
        /// <summary>The file of the slot the game is saved in, in the folder the saves are kept in.</summary>
        public string SaveFile => SavePaths.Current.Slot(game.flow.SaveSlot);
        /// <summary>The slot the game is saved in deleted, its backup with it: a tool that saved takes its save away again.</summary>
        public bool DeleteSave() => SaveSlots.Delete(SavePaths.Current, game.flow.SaveSlot);
        /// <summary>The folder the Vs. Recorder writes into (plan 08 · P10): beside the saves unless a tool names another.</summary>
        public string ReplaysFolder { get => game.ReplaysFolder; set => game.ReplaysFolder = value; }
        /// <summary>The file the last battle recorded went into, or null.</summary>
        public string? LastRecorded => game.LastRecorded;
        /// <summary>The battle the Vs. Recorder is about to ask about, or is asking about now; null when there is none.</summary>
        public BattleRecordFile? RecordOffered => game.flow.RecordOffered;
        /// <summary>Whether the question on the screen is the Vs. Recorder's.</summary>
        public bool AskingToRecord => game.flow.RecordAsked;
        /// <summary>The Vs. Recorder's list of the battles it kept (plan 08 · P11).</summary>
        public ReplayListScreen ReplayList => game.flow.ReplayListScreen;
        /// <summary>The migration's screen (plan 08 · P21), the folder it reads and the clock it is done by.</summary>
        public MigrateScreen Migrate => game.flow.MigrateScreen;
        public string MigrateFolder { get => game.MigrateFolder; set => game.MigrateFolder = value; }
        public Func<DateTime>? MigrationClock { get => game.MigrationClock; set => game.MigrationClock = value; }
        /// <summary>The title's menu offers MIGRATE as the folder stands now.</summary>
        public void OfferMigration() => game.OfferMigration();
        /// <summary>Opens the migration as the title's MIGRATE does.</summary>
        public void OpenMigrate() => game.OpenMigrate();
        /// <summary>The jukebox (plan 12 · Q15), and opening it as the title (or the options) does.</summary>
        public JukeboxScreen Jukebox => game.flow.JukeboxScreen;
        /// <summary>The start menu's REQUESTS (plan 12 · Q21), the request pinned over the field and whether the menu offers the page.</summary>
        public RequestsScreen Requests => game.flow.RequestsScreen;
        public string? PinnedRequest { get => game.flow.PinnedRequest; set => game.flow.PinnedRequest = value; }
        public bool HasRequests => game.HasRequests;
        public void OpenRequests() => game.OpenRequests();
        public void PointOutThePinnedRequest() => game.PointOutThePinnedRequest();
        /// <summary>A PC's Poké Jobs board (plan 15 · E3), its jobs, and the board opened as the PC's script opens it.</summary>
        public JobsScreen Jobs => game.flow.JobsScreen;
        /// <summary>The camp (plan 10 · F6), opened from the start menu's CAMP.</summary>
        public CampScreen Camp => game.flow.CampScreen;
        /// <summary>Whether the start menu offers CAMP now.</summary>
        public bool HasCamp => game.HasCamp;
        public JobBoard JobBoard => game.flow.Jobs;
        /// <summary>The care book (plan 35 · BN1) and the day it counts by.</summary>
        public CareBook Care => game.flow.Care;
        public int CareDay => game.CareDay;
        public CampCare CampCare => game.flow.CampCare;
        /// <summary>The pasture (plan 35 · BN5): the visit under way (null outside), its herd by body, and whether the PC offers it here.</summary>
        public Overworld.PastureSave? PastureVisit => game.flow.Pasture;
        public IReadOnlyDictionary<string, Pokemon> Herd => game.flow.Herd;
        public bool PastureHere => game.PastureHere;
        /// <summary>An act of the pasture, as the script's <c>pasture</c> asks it (a warp is the script's own, behind its fade).</summary>
        public PastureAnswer Pasture(string act, int number = 0, Overworld.NPC? subject = null, string? item = null) =>
            game.PastureAct(act, number, subject, item, game.WarpTo);
        /// <summary>The care close-up (plan 35 · BN2), and the camp's way into it for the Pokémon under its cursor.</summary>
        public CareScreen CareCloseUp => game.flow.CareScreen;
        public void OpenCare() => game.OpenCare();
        public void OpenJobs() => game.OpenJobs();
        /// <summary>The Global Link counter (plan 07 · O5, O7), opened as its attendant's script opens it, on the options' server.</summary>
        public GlobalLinkScreen GlobalLink => game.flow.GlobalLinkScreen;
        public void OpenGlobalLink() => game.OpenGlobalLink();
        public void OpenJukebox(GameState returnTo) => game.OpenJukebox(returnTo);
        public void CloseJukebox() => game.CloseJukebox();
        /// <summary>The gallery (plan 12 · Q30), opened as the title's GALLERY or a Pokédex entry's A does, and closed as B does.</summary>
        public GalleryScreen Gallery => game.flow.GalleryScreen;
        public void OpenGallery(GameState returnTo, string? openOn = null) => game.OpenGallery(returnTo, openOn);
        public void CloseGallery() => game.CloseGallery();
        /// <summary>The title's CREDITS (plan 16 · T1).</summary>
        public TitleCreditsScreen TitleCredits => game.flow.TitleCreditsScreen;
        /// <summary>The last recording watched broke off before its end (its answers ran out or were refused).</summary>
        public bool ReplayBroke => game.ReplayBroke;
        /// <summary>The game as it stands, as the save screen would write it (plan 24 · X3: the harness's <c>saves</c>).</summary>
        public SaveData BuildSave() => game.BuildSave();
        /// <summary>Puts a save's contents into the game as loading it does, with no title or fade (plan 39 · CR4's tests).</summary>
        public void ApplySave(SaveData save) => game.ApplySaveData(save);
        /// <summary>The title's HOME (plan 12 · Q28, Q29).</summary>
        public HomeScreen Home => game.flow.HomeScreen;
        /// <summary>Opens HOME on the slots as their files now stand, as the title's row does.</summary>
        public void OpenHome()
        {
            game.titleSlots = SaveSlots.Read(SavePaths.Current);
            game.OpenHome();
        }
        /// <summary>The trades a continued save still has to welcome.</summary>
        public int TradesWaiting => game.TradesWaiting;
        /// <summary>Every Pokémon of the player's given its own id, as a save gives them (plan 07 · O2); how many were.</summary>
        public int GiveIds() => game.GiveIds();
        /// <summary>Every Pokémon of the player's: the team, the PC, the Day Care.</summary>
        public IEnumerable<Pokemon> OwnedPokemon => game.OwnedPokemon();
        /// <summary>How many ids the game has given its Pokémon, and the link battles' record (plan 07 · O2).</summary>
        public uint PokemonIdsIssued => game.flow.PokemonIdsIssued;
        public LinkBattleRecord LinkBattles => game.flow.LinkBattles;

        // ------------------------------------------------------------ a scene skipped (plan 12 · Q20)

        /// <summary>A tool's hand on the menu button: held down while set, as the key is, and read where the key is read.</summary>
        public bool SkipHeld { get => game.SkipHeld; set => game.SkipHeld = value; }
        /// <summary>The script running, by its file and name (<c>common.Television</c>); null when none is (plan 08 · P8).</summary>
        public string? ScriptNow => game.ScriptRunning ? Flow.Runner.Current?.FullName : null;
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
