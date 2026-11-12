using System;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Models.SuperContests;
using PokemonPlatinumEngine.Models.Underground;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// What a game keeps beside the field's objects (plan 16 · T11g): the save's own records and cases, the slot it is
/// saved in, who walks behind the player and the Battle Frontier's challenge under way. The flow holds them, so a game
/// with no window keeps them as the game does; the engine reads and writes them through the flow.
/// </summary>
public sealed partial class GameFlow
{
    /// <summary>
    /// The slot this game is saved in (plan 12 · Q2): the one it was continued from, or the empty one a new game took.
    /// Every save the game writes goes there (<see cref="GameEngine.WriteSave"/>): the save screen's, SAVE AND QUIT's,
    /// the close button's, a script's.
    /// </summary>
    public int SaveSlot { get; set; } = 1;

    /// <summary>The time played, in seconds (saved).</summary>
    public float PlayTime { get; set; }

    /// <summary>The Trainer Card's ID (saved).</summary>
    public int TrainerId { get; set; }

    /// <summary>
    /// The region the game began in (plan 18 · W4; saved): every other region's levels are its file's mapping
    /// (<see cref="Data.Region.Levels"/>), met by a team that crossed into it. Sinnoh for any game begun from the title.
    /// </summary>
    public string BeganIn { get; set; } = Data.RegionDatabase.Sinnoh;

    /// <summary>How many ids the game has given its Pokémon (plan 07 · O2, <see cref="PokemonIds"/>; saved).</summary>
    public uint PokemonIdsIssued { get; set; }

    /// <summary>The link battles' record (plan 07 · O2; saved).</summary>
    public LinkBattleRecord LinkBattles { get; set; } = new();

    /// <summary>The teams kept by name at the PC (plan 12 · Q27; saved as <c>SaveData.Teams</c>).</summary>
    public System.Collections.Generic.List<SavedTeam> Teams { get; set; } = new();

    /// <summary>The Trainer Card's score (plan 06 · R12), and the badges it has counted.</summary>
    public int TrainerScore { get; set; }
    public int ScoredBadges { get; set; }

    /// <summary>The day the adventure began (saved).</summary>
    public DateTime? AdventureStarted { get; set; }

    /// <summary>The ways to play chosen at NEW GAME (plan 13 · V1; saved as <c>SaveData.Run</c>, Normal when none).</summary>
    public Models.RunRules Run { get; set; } = Models.RunRules.Normal;

    /// <summary>What the run remembers (plan 13 · V4: the places met, the graveyard, its end; saved as <c>SaveData.RunState</c>).</summary>
    public Models.RunState RunState { get; set; } = new();

    /// <summary>What the game has counted (plan 13 · V8; saved as <c>SaveData.Counters</c>).</summary>
    public Models.Statistics Statistics { get; set; } = new();

    /// <summary>Who of the friends' hall has been beaten today (plan 13 · V23); saved as <c>SaveData.FriendsBeaten</c>.</summary>
    public Models.FriendsHallState FriendsHall { get; set; } = new();

    /// <summary>The friends' trainers standing in the friends' hall now, by the person who stands for them (plan 13 · V23).</summary>
    public Dictionary<Overworld.NPC, Models.TrainerFile> FriendsStanding { get; } = new();

    /// <summary>The Pokémon away on Poké Jobs (plan 15 · E3; saved as <c>SaveData.Jobs</c>).</summary>
    public Models.JobBoard Jobs { get; set; } = new();

    /// <summary>The medals won (plan 13 · V9; saved as <c>SaveData.Medals</c>).</summary>
    public Models.Medals Medals { get; set; } = new();

    /// <summary>A Pokémon just caught under the Nuzlocke, whose name the keyboard asks once the field is back (plan 13 · V4).</summary>
    public Models.Pokemon? NameAfterBattle { get; set; }

    /// <summary>Where the Pokémon's keyboard goes back to: the party menu after RENAME, the field after a catch.</summary>
    public GameState NicknameReturn { get; set; } = GameState.PartyMenu;

    /// <summary>The teams entered into the Hall of Fame (plan 06 · R12).</summary>
    public HallOfFame HallOfFame { get; set; } = new();

    /// <summary>The Champions' circuit (plan 41 · EG4; saved as <c>SaveData.Circuit</c>, null until first begun).</summary>
    public CircuitState Circuit { get; set; } = new();

    /// <summary>The Journal's pages (plan 06 · R12).</summary>
    public Journal Journal { get; set; } = new();

    /// <summary>The cells of the region's map the player last passed through, which the Town Map marks (plan 01 · M12).</summary>
    public MapTrail MapTrail { get; } = new();

    /// <summary>
    /// Where Dig and an Escape Rope lead out of the caves: outside the way the player went into them (the original's
    /// exit location), saved with the game.
    /// </summary>
    public MapSpot? ExitSpot { get; set; }

    /// <summary>The key item kept on the item button (saved).</summary>
    public string? RegisteredItem { get; set; }

    /// <summary>The request pinned over the field, by its id (plan 12 · Q21; saved).</summary>
    public string? PinnedRequest { get; set; }

    /// <summary>The trainer tips shown and those waiting for a quiet moment (plan 12 · Q23; the shown are saved).</summary>
    public Models.TipBook Tips { get; } = new();

    /// <summary>The tip on screen, and how long it has been up; null while none is.</summary>
    public Models.Tip? ShownTip { get; set; }
    public float ShownTipAge { get; set; }

    /// <summary>The clothes the player owns and wears (plan 11 · C9).</summary>
    public Wardrobe Wardrobe { get; } = new();

    /// <summary>
    /// Whoever walks behind the player (Overworld.Follower), one at a time: someone of the story who travels along and
    /// battles beside the player (plan 02 · S6: Cheryl through Eterna Forest), or a Pokémon of the team out of its
    /// ball, which only walks (plan 06 · R16c: Amity Square).
    /// </summary>
    public Follower? Along { get; set; }

    /// <summary>The Pokétch (plan 02 · S2).</summary>
    public Poketch Poketch { get; } = new();

    /// <summary>The Great Marsh's Safari Game, while one is on (plan 01 · M7; saved).</summary>
    public SafariGame Safari { get; } = new();

    /// <summary>The National Park's Bug-Catching Contest, while one is on (plan 15 · E5; saved).</summary>
    public BugContest BugContest { get; } = new();

    /// <summary>The Pokéathlon's Athlete Points and records (plan 15 · E6; saved).</summary>
    public AthlonRecords Athlon { get; set; } = new();

    /// <summary>The Repel's steps and the flute played here (plan 06 · R11); the Repel's steps are saved.</summary>
    public EncounterAids EncounterAids { get; } = new();

    /// <summary>What the game remembers of its wild Pokémon (saved as <see cref="SaveData.Encounters"/>).</summary>
    public SpecialEncounters Encounters { get; set; } = new();

    /// <summary>The catch streak of Pokémon met in sight and the chain of hooks on a rod (plan 38 · EX3).</summary>
    public Overworld.CatchStreak CatchStreak { get; set; } = new();

    /// <summary>Sinnoh's berry patches (saved as <see cref="SaveData.Berries"/>).</summary>
    public BerryPatches Berries { get; set; } = BerryPatches.NewGame();

    /// <summary>Solaceon Town's Day Care (saved).</summary>
    public DayCare DayCare { get; } = new();

    /// <summary>The Seal Case's Seals and capsules (saved).</summary>
    public SealCase SealCase { get; } = new();

    /// <summary>The contests' records and photographs (saved).</summary>
    public ContestRecords ContestRecords { get; } = new();

    /// <summary>The Fashion Case's accessories (saved).</summary>
    public FashionCase FashionCase { get; } = new();

    /// <summary>The Poffin Case's Poffins (saved).</summary>
    public PoffinCase PoffinCase { get; } = new();

    /// <summary>The Pokéblock Case (plan 21 · H11; saved) and Hoenn's contests entered, won and hung in the museum (saved).</summary>
    public PokeblockCase PokeblockCase { get; } = new();
    public Models.Contest.HoennContestRecords HoennContests { get; set; } = new();

    /// <summary>The coins, Pal Park's six, the Villa and the game records kept beside the story (saved).</summary>
    public Pastimes Pastimes { get; } = new();

    /// <summary>The Apricorn Box, the trees picked and Kurt's order (plan 20 · J6; saved).</summary>
    public ApricornBox Apricorns { get; } = new();

    /// <summary>The crafting kit's pouch of materials (plan 39 · CR1; saved).</summary>
    public CraftingState Crafting { get; } = new();

    /// <summary>The gift box: what it has handed over and the words told (plan 40 · GE7; saved).</summary>
    public GiftBox Gifts { get; } = new();

    /// <summary>The stage show's trunk, dressings and shows (plan 42 · PS1, PS2; saved).</summary>
    public Models.StageShow.ShowRecords StageShow { get; } = new();

    /// <summary>The film studio's takes and bests (plan 42 · PS4; saved).</summary>
    public Models.Studio.StudioRecords Studio { get; } = new();

    /// <summary>The trade board: today's requests met and blind trades made (plan 40 · GE8; saved).</summary>
    public TradeBoard Board { get; } = new();
    /// <summary>Pennant Hall, the hub's plaza (plan 41 · EG3; saved once opened).</summary>
    public PlazaState Plaza { get; } = new();

    /// <summary>The Week's Work, the hub's board of missions (plan 41 · EG6; saved once read).</summary>
    public MissionBoard Missions { get; } = new();

    /// <summary>The timed powers in force (plan 36's powers, granted by plan 39 · CR6's dishes; saved).</summary>
    public PowerState Powers { get; } = new();

    /// <summary>The training drills' records and the prizes they have earned (plan 36 · TR2; saved).</summary>
    public TrainingRecords Training { get; } = new();

    /// <summary>The DexNav's state (plan 10 · F11, F12).</summary>
    public Overworld.DexNavState DexNav { get; set; } = new();

    /// <summary>The care book (plan 35 · BN1): a page for each Pokémon of the player's, by its id; saved as <c>SaveData.Care</c>.</summary>
    public CareBook Care { get; } = new();

    /// <summary>
    /// The camp (plan 10 · F6), a view over the care book since plan 35 · BN1: each Pokémon's cure and the day's pats and
    /// meals are its page's, on the game's own day.
    /// </summary>
    public CampCare CampCare => campCare ??= new CampCare(Care, () => CareRules.DayOf(GameClock.Today));
    private CampCare? campCare;

    /// <summary>Cameron's photo album (plan 20, part q).</summary>
    public PhotoAlbum Photos { get; } = new();

    /// <summary>Lockwork's locked Pokémon reclaimed, missed and opened (plan 44 · SD1).</summary>
    public HeartsRecord Hearts { get; set; } = new();

    /// <summary>The Underground's own: the bag below ground, the base, the buried Spheres, the day's walls (saved).</summary>
    public UndergroundState Underground { get; set; } = new();

    /// <summary>The player's secret base of Hoenn's, null with none, and the decoration PC (plan 21 · H10; saved).</summary>
    public SecretBaseSave? SecretBase { get; set; }
    public List<StoredDecoration> Decorations { get; set; } = new();

    /// <summary>The Battle Frontier's records (saved).</summary>
    public FrontierRecords Frontier { get; set; } = new();

    /// <summary>A challenge of each facility under way, which lives between the scripts that play it.</summary>
    public BattleTowerChallenge? Tower { get; set; }
    public BattleFactoryChallenge? Factory { get; set; }
    public BattleHallChallenge? Hall { get; set; }
    public BattleCastleChallenge? Castle { get; set; }
    public BattleArcadeChallenge? Arcade { get; set; }
    public BattleDomeChallenge? Dome { get; set; }
    public BattleVenueChallenge? Venue { get; set; }
    /// <summary>A later battle place's challenge under way (plan 37 · FC2); lives between the desk and the battle room as the venues' round does.</summary>
    public Models.Facilities.StreakChallenge? Facility { get; set; }
    public BattlePikeChallenge? Pike { get; set; }
    public BattlePyramidChallenge? Pyramid { get; set; }
    public Models.Tournament? Tournament { get; set; }
}
