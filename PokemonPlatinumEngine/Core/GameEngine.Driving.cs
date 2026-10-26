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
        public PcBoxes Boxes { get => Flow.Boxes; set => Flow.Boxes = value; }
        public int Money { get => Flow.Money; set => Flow.Money = value; }
        public int TrainerScore { get => game.flow.TrainerScore; set => game.flow.TrainerScore = value; }
        public HallOfFame HallOfFame { get => game.flow.HallOfFame; set => game.flow.HallOfFame = value; }
        public Journal Journal { get => game.flow.Journal; set => game.flow.Journal = value; }
        public Wardrobe Wardrobe => game.flow.Wardrobe;
        public Poketch Poketch => game.flow.Poketch;
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
        /// <summary>The Battle Palace's or the Battle Arena's round under way (plan 14 · B9).</summary>
        public Models.Frontier.BattleVenueChallenge? Venue => game.flow.Venue;
        /// <summary>The Battle Pike's and the Battle Pyramid's round under way (plan 14 · B10).</summary>
        public Models.Frontier.BattlePikeChallenge? Pike => game.flow.Pike;
        public Models.Frontier.BattlePyramidChallenge? Pyramid => game.flow.Pyramid;
        /// <summary>A stone panel of the Ruins of Alph, open while a chamber's script waits on it (plan 20 · J5).</summary>
        public AlphPuzzleScreen AlphPuzzle => game.flow.AlphPuzzleScreen;
        /// <summary>The phrase picker, open while a script's <c>phrase</c> waits on it (plan 08 · P9).</summary>
        public PhraseScreen Phrases => game.flow.PhraseScreen;
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
        public void OpenJukebox(GameState returnTo) => game.OpenJukebox(returnTo);
        public void CloseJukebox() => game.CloseJukebox();
        /// <summary>The last recording watched broke off before its end (its answers ran out or were refused).</summary>
        public bool ReplayBroke => game.ReplayBroke;
        /// <summary>The game as it stands, as the save screen would write it (plan 24 · X3: the harness's <c>saves</c>).</summary>
        public SaveData BuildSave() => game.BuildSave();
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
