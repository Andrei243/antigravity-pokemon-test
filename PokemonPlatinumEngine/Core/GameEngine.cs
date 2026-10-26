using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.PoketchApps;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.Core;

public enum GameState
{
    Overworld,
    Battle,
    Dialogue,
    PartyMenu,
    BagMenu,
    PokedexMenu,
    TrainerCard,
    StarterSelect,
    Shop,
    PCStorage,
    Options,
    Title,
    Evolution,
    SaveMenu,
    Intro,
    /// <summary>
    /// The map of the region (plan 01 · M12): the Town Map out of the bag, a map on a wall, and Fly's choice of a
    /// place to land (plan 02 · S2), which gave the state its name.
    /// </summary>
    FlyMap,
    /// <summary>The PC's Hall of Fame (plan 06 · R12).</summary>
    HallOfFame,
    /// <summary>The Journal's pages (plan 06 · R12).</summary>
    Journal,
    /// <summary>The wardrobe at home, or a boutique's (plan 11 · C10).</summary>
    Wardrobe,
    /// <summary>The pot at the Poffin House, and the Poffin Case (plan 06 · R14c).</summary>
    PoffinCooking,
    PoffinCase,
    /// <summary>An Egg hatching (plan 06 · R15).</summary>
    Hatch,
    /// <summary>A Super Contest in the Contest Hall (plan 06 · R17a).</summary>
    SuperContest,
    /// <summary>The Ball Capsules, the Seal merchant's stall, and the Fashion Case looked into or a winner's photograph (plan 06 · R17b).</summary>
    SealCase,
    SealShop,
    FashionCase,
    /// <summary>The Underground (plan 06 · R16a): the wall's game, the lists of the bag below ground, a hiker's trade and the base's PC, and the base decorated.</summary>
    Mining,
    Underground,
    Decoration,
    /// <summary>A sitting at one of the Game Corner's slot machines (plan 06 · R16b).</summary>
    SlotMachine,
    /// <summary>A facility's records board in the Battle Frontier (plan 06 · R18a).</summary>
    FrontierRecords,
    /// <summary>The Battle Factory's rentals: the six on offer, or a trade after a battle won (plan 06 · R18b).</summary>
    FactoryRentals,
    /// <summary>The scene of a team's entry into the Hall of Fame, and the credits after it (plan 02 · S14).</summary>
    HallOfFameCeremony,
    Credits,
    /// <summary>The Battle Hall's board of types, and one of the Battle Castle's two counters (plan 06 · R18c).</summary>
    HallBoard,
    CastleCounter,
    /// <summary>The Battle Arcade's game board (plan 06 · R18d).</summary>
    ArcadeBoard,
    /// <summary>The Pokégear's cards: the phone, the map card and the radio (plan 20 · J3).</summary>
    Pokegear,
    /// <summary>The PokéNav's pages: the map, the condition, Match Call and the ribbons (plan 21 · H2, part b).</summary>
    PokeNav,
    /// <summary>A stone panel of the Ruins of Alph's chambers (plan 20 · J5).</summary>
    AlphPuzzle,
    /// <summary>The phrase picker over the field, a script's <c>phrase</c> waiting on it (plan 08 · P9).</summary>
    Phrases,
    /// <summary>The Vs. Recorder's list of the battles it kept (plan 08 · P11); a recording watched is a <see cref="Battle"/>.</summary>
    ReplayList,
    /// <summary>The title's MIGRATE: Pokémon from the player's own files, brought to a save's Pal Park (plan 08 · P21).</summary>
    Migrate,
    Transition,
    /// <summary>
    /// The window's close button asked (plan 12 · Q1): its question over whatever was on screen, which waits under it
    /// (<see cref="GameEngine.RequestClose"/>).
    /// </summary>
    WindowClosing,
    /// <summary>A script's <c>namerival</c>: the keyboard for the rival of the region it plays in, over the field (plan 18 · W4, part b).</summary>
    RivalNaming,
    /// <summary>The party menu's RENAME: the keyboard for a Pokémon's name, over the field (plan 12 · Q19).</summary>
    Nickname,
    /// <summary>The Bug-Catching Contest's board of results, a script's <c>bugcontest results</c> waiting on it (plan 15 · E5).</summary>
    BugContestResults,
    /// <summary>The Pokéathlon's courses, team and events, a script's <c>athlon open</c> waiting on it (plan 15 · E6).</summary>
    Athlon,
    /// <summary>The jukebox: every song, sound, cry and bed to play, from the title or the options (plan 12 · Q15).</summary>
    Jukebox,
    /// <summary>The Apricorn Box looked into, from the bag, the item button or a script's <c>apricorn box</c> (plan 20 · J6).</summary>
    ApricornBox,
    /// <summary>A Hoenn contest's appeals, a script's <c>appeals enter</c> waiting on it; the Pokéblock Case; the Berry Blender (plan 21 · H11).</summary>
    Appeals,
    PokeblockCase,
    Blender,
    /// <summary>The Battle Dome's bracket, a script's <c>bracket show</c> or <c>bracket pick</c> waiting on it (plan 14 · B8).</summary>
    Bracket,
    /// <summary>The first start's page of options, before the title's notice on the first run only (plan 16 · T24).</summary>
    FirstStart,
    /// <summary>The World Tournament's events or bracket, a script's <c>tournament events</c> or <c>tournament show</c> waiting on it (plan 14 · B11).</summary>
    Tournament
}

public partial class GameEngine
{
    // Interface code lays out in these units; the screen itself is rendered RenderScale times larger (3840x2160)
    public const int VirtualWidth = 1920;
    public const int VirtualHeight = 1080;
    public const int RenderScale = 2;

    private RenderTexture2D virtualScreen;

    /// <summary>The game's states without the renderers (plan 16 · T11): the state, a transition, the menus' return states and the evolutions waiting.</summary>
    private readonly GameFlow flow = new();

    private readonly RenderContext renderContext = new(VirtualWidth, VirtualHeight);
    private readonly WorldRenderer world;
    private readonly BattleRenderer battleRenderer;

    // Someone carried by a warp panel (plan 02 · S11, part b; Overworld/WarpPanels): the warp, the way they faced as
    // they stepped on, how long they have turned, and whether the picture has opened on the far panel yet
    private (Warp Warp, Direction Facing, float Clock, bool Arrived)? panelRide;

    // How the player was travelling when the field's music was last chosen, and whether a trainer's eye theme is on
    private TravelMode musicTravel = TravelMode.OnFoot;
    private bool eyeThemePlaying;

    // Where the map of the region goes back to when it closes: the bag it was taken out of, or the field
    private bool townMapFromBag;

    private (int, string?, bool) radarFollowed;

    // Whether the run button was down last frame (on the Bicycle a press changes gear)
    private bool runWasDown;

    private Random fieldRandom = Dice.New();

    // The opening and title menu, the slots' saves it offers to continue from, and where the options screen returns to
    private readonly TitleScene titleScene = new();
    private IReadOnlyList<SaveSlot> titleSlots = Array.Empty<SaveSlot>();

    // The slot the title's NEW GAME chose, until the game it is for begins
    private int? newGameSlot;
    // The ways to play the title asked for, kept between the title and the introduction (plan 13 · V1)
    private RunRules? newGameRun;

    /// <summary>The close button's question (plan 12 · Q1); the state it was asked over is the flow's.</summary>
    private readonly ClosePrompt closePrompt = new();
    private bool gameStarted;

    /// <summary>Player options (graphics, window, time of day, sound), kept in settings.json.</summary>
    public GameSettings Settings { get; }

    private float rematchSpin;

    // The chance of the people who move about of their own accord (GameFlow.Wandering): drawn from Dice the first
    // time anyone moves, so a run in which nobody does draws nothing
    private Random? peopleRandom;

    /// <summary>Keeps everyone standing where they are: the harness, whose shots count on people being where they first stood.</summary>
    public bool PeopleStayPut { get; set; }

    // The one of GameFlow.Along who battles beside the player, which is what everything that changes for a partner
    // asks. The tile the player was last seen heading for tells when they set off from another
    private Follower? partner => flow.Along is { TrainerId: not null } ? flow.Along : null;
    private (int X, int Y) partnerHeading;

    private static string playerName => PlayerIdentity.Name;

    /// <summary>
    /// The region a new game starts in; null for the first region in the chain (Sinnoh, plan 18 · W10: NEW GAME asks
    /// nothing about regions). Set from the command line (--region Kanto) or by a tool to test another region
    /// without playing through the ones before it.
    /// </summary>
    public string? NewGameRegion { get; set; }

    /// <summary>The record of the region a new game starts in: <see cref="NewGameRegion"/>'s, or the chain's first.</summary>
    private Region NewGameRegionRecord => (NewGameRegion != null ? RegionDatabase.Get(NewGameRegion) : null) ?? RegionDatabase.First;

    /// <summary>The strip of the party menu and the Pokétch's app: the granted moves nobody on the team knows (plan 12 · Q18).</summary>
    /// <summary>
    /// What the run chosen at NEW GAME asks of the screens (plan 13 · V5): the PC keeps the Monotype and the team's size, and
    /// the options' Battle Style stays Set under a run that forces it. A run with none of them changes nothing.
    /// </summary>
    private void KeepTheRunInView()
    {
        var run = flow.Run;
        flow.PCScreen.PartyLimit = flow.Party.Limit;
        flow.PCScreen.WhyNotCarry = run.Monotype == null ? null : p => run.MayCarry(p, flow.Story.PlayerStarter) ? null
            : Text.Of("run.monotype.notThis", ("type", Words.Type(run.Monotype.Value)));
        // (From the title no game is in force, whatever the last one set)
        flow.OptionsScreen.StyleLocked = run.SetStyle && flow.OptionsReturn != GameState.Title;
    }

    private void KeepTheHelpersInView()
    {
        KeepTheRunInView();
        flow.PartyScreen.Helpers = GrantedMoves.Where(m => FieldMoveRules.Knower(flow.Party, m) == null).ToList();
        flow.PartyScreen.PartyTools = PartyToolsOn;
        flow.BagScreen.Sorting = BoxLinkOn;
        flow.BagScreen.RunCap = CurrentLevelCap();
        flow.BagScreen.NoRevives = flow.Run.Nuzlocke;
        flow.PartyScreen.RunCap = flow.DayCare.LevelCap = flow.BagScreen.RunCap;
        flow.Poketch.ShowsHiddenMoves = FieldHelp.On(Settings.FieldHelpers, Ruleset.Current.Preset);
    }

    /// <summary>The field moves a wild helper does now (plan 12 · Q18), worked out afresh and never stored.</summary>
    public IReadOnlyCollection<FieldMove> GrantedMoves =>
        FieldHelp.Granted(Settings.FieldHelpers, Ruleset.Current.Preset, flow.Story, flow.Bag, Here.Id);

    /// <summary>
    /// The region the player stands in (plan 18 · W1): the one that lists the map they are on, the chain's first
    /// before a game is loaded. What belongs to a region (its starters, its professor, its spawns) is asked of this,
    /// and handed to the screens, never read from a static.
    /// </summary>
    public Region Here => (flow.Map != null ? RegionDatabase.RegionOfMap(flow.Map.Name) : null) ?? RegionDatabase.First;

    // The band a Gym Leader's, an Elite Four's or the Champion's battle opens on, and its portraits (plan 09 · L15):
    // begun with the transition, rendered offscreen as its frames find the rigs ready, ended at the midpoint
    private readonly BandPortraits band = new();

    /// <summary>Set when the player chose to leave the game from a menu; the main loop closes the window.</summary>
    public bool QuitRequested { get; private set; }

    public GameEngine(GameSettings? settings = null)
    {
        Settings = settings ?? GameSettings.Load();
        flow.ModelWanted += PokemonModels.Request;
        world = new WorldRenderer(renderContext);
        // Unova's seasons (plan 22 · U3): a map of a region with seasons is drawn in the field's; every other in none
        world.SeasonOf = SeasonOn;
        battleRenderer = new BattleRenderer(renderContext);
    }

    public void Initialize()
    {
        // Each step of the start-up is timed for the log (plan 16 · T3); the harness's `profile` mode prints them
        using var startUp = Log.Phase("start-up, in all");
        using (Log.Phase("the screen"))
        {
            virtualScreen = Gpu.Target(VirtualWidth * RenderScale, VirtualHeight * RenderScale, "screen");
            UiShapes.PixelScale = RenderScale;
            Raylib.SetTextureFilter(virtualScreen.Texture, TextureFilter.Bilinear);
        }

        using (Log.Phase("the sound")) AudioManager.Initialize();
        LoadTheData();

        var modelled = PokemonModels.Preloaded.ToList();
        using (Log.Phase("models asked for"))
        {
            // The characters of the introduction, sculpted and meshed in the background while the title screen plays; the
            // people of the field are made ready where the player is (KeepCharactersNear, plan 11 · C1)
            CharacterModels.Preload(new[] { "PLAYER", "DAWN", "ROWAN" });
            // The Pokémon the title and the introduction show at once are meshed meanwhile too. Only the stand-in is kept
            // for good: every other model, those of the story's opening among them, is made when a scene asks for it and
            // let go after it (plan 03 · D13: kept for the whole game, the opening's twenty-four held a few megabytes each)
            PokemonModels.Preload(new[] { PokemonSprites.Fallback });
            foreach (string shown in PokemonModels.ShownAtOnce) PokemonModels.Request(shown);
            // (The Pokémon that stand in the field, plan 10 · F1, are made ready where the player is, as the people are:
            // KeepCharactersNear. Asked for here, every start built the models of all of them, plan 03 · D13)
            // And the balls the battles throw
            BattleBall.Preload("Poké Ball", "Great Ball", "Ultra Ball", "Master Ball");
            SubstituteDoll.Preload();
        }

        using (Log.Phase("options")) ApplySettings(window: false);

        // The menu sprites of the species the story shows first are made up front (read from the sprite cache once
        // they have been baked); every other species' are made the first time a menu asks for them, showing the
        // stand-in (PokemonSprites.Fallback) for a frame or two meanwhile.
        using (Log.Phase("render targets and shaders")) renderContext.EnsureLoaded();
        using (Log.Phase("menu sprites")) PokemonSprites.BakeAll(renderContext, modelled);

        ReadTheSlots();
        using (Log.Phase("the title's music")) AudioManager.PlayMusic(MusicRole.Title);
    }

    /// <summary>
    /// The game with no window (plan 16 · T12): the data, the scripts, the options and the slots, and nothing that
    /// needs a screen or a sound card. No model or sprite is asked for up front and <see cref="Draw"/> is never to be
    /// called; <see cref="Update"/> runs the whole game, fed its actions as frames (<see cref="InputManager.Feed"/>).
    /// What it saves goes to <see cref="SavePaths.Current"/>, which a test points at a folder of its own first.
    /// </summary>
    public void InitializeHeadless()
    {
        IsHeadless = true;
        LoadTheData();
        ApplySettings(window: false);
        ReadTheSlots();
    }

    /// <summary>Whether this game was started with no window (<see cref="InitializeHeadless"/>).</summary>
    public bool IsHeadless { get; private set; }

    private void LoadTheData()
    {
        using (Log.Phase("moves")) MoveDatabase.Initialize();
        using (Log.Phase("species")) PokemonDatabase.Initialize();
        using (Log.Phase("items")) ItemDatabase.Initialize();
        using (Log.Phase("maps")) MapDatabase.Initialize();
        using (Log.Phase("scripts")) LoadScripts();
    }

    private void ReadTheSlots()
    {
        // The game opens on the title screen, which offers the saved game if there is one
        // A save that couldn't be read gives way to the one before it, and the title says so before its menu (plan 12 · Q1);
        // and before that, once, that the last run stopped on a crash, if it did (plan 16 · T3)
        // The saves are three slots in one folder (plan 12 · Q2), and each slot's notice names it
        using (Log.Phase("the save"))
        {
            var paths = SavePaths.Current;
            // An install from before there were slots kept one save: it becomes the first empty slot's, once
            var old = SaveSlots.AdoptOldSave(paths);
            if (old.Move == OldSaveMove.Moved) Log.Info($"The save from before there were slots ({SavePaths.OldSaveName}) is slot {old.Slot} now ({SavePaths.SlotName(old.Slot)}).");
            else if (old.Move == OldSaveMove.NoRoom) Log.Warn($"{SavePaths.OldSaveName} is from before there were slots, and every slot holds a save: it was left where it is.");
            else if (old.Move == OldSaveMove.Failed) Log.Warn($"{SavePaths.OldSaveName} is from before there were slots and couldn't be given one: {old.Problem}");

            // A move between slots cut short by a crash is finished (or undone) before any slot is read (plan 08 · P22)
            int finished = SlotMoves.Resolve(paths);
            if (finished > 0) Log.Info($"{finished} move(s) of Pokémon between slots, cut short by the last run, were finished or undone.");

            titleSlots = SaveSlots.Read(paths);
            var crashed = CrashReport.TakePending(paths.Folder);
            string read = string.Join(", ", titleSlots.Select(s => $"slot {s.Number} {s.Outcome}"));
            if (crashed != null) Log.Info($"The last run stopped on a crash ({crashed.Report}); the title says so. The saves read as: {read}.");
            else Log.Info($"The saves read as: {read}.");
            var notices = new List<TitleNotice?> { TitleNotice.ForCrash(crashed, titleSlots.Select(s => s.Outcome).ToList()), TitleNotice.ForOldSave(old) };
            notices.AddRange(titleSlots.Select(s => TitleNotice.ForSlot(s.Number, s.Outcome)));
            flow.TitleScreen = TitleScreen.WithSlots(TitleCards(), notices.ToArray());
            // The player's own files (plan 08 · P21): MIGRATE is on the menu while the folder holds one the readers know
            using (Log.Phase("migrate folder")) OfferMigration();
        }
        // On the first run only, a page of options comes before the title's notice (plan 16 · T24)
        OfferTheFirstStart();
        // ... and behind it and the notice, the measuring of the preset this machine keeps (plan 16 · T23)
        MeasureOnTheFirstRun();
    }

    /// <summary>Starts a new game at once as the boy with his own name: for tests and the screenshot harness.</summary>
    public void StartNewGame() => StartNewGame(null, PlayerLook.Boy);

    /// <summary>
    /// Starts a new game as the character and under the name chosen in the introduction. Every game is played by the
    /// modern rules (plan 06 · R19): Platinum's are the baseline the code writes them against, and no game's.
    /// </summary>
    public void StartNewGame(string? name, PlayerLook look, string? rival = null, RunRules? run = null)
    {
        // The new game is saved in the slot the title chose for it, the first empty one; a game begun without the
        // title (a test, a tool) takes the first empty one too, and with none keeps the slot it had (plan 12 · Q2)
        flow.SaveSlot = newGameSlot ?? SaveSlots.FirstEmpty(SavePaths.Current) ?? flow.SaveSlot;
        newGameSlot = null;
        // Before any Pokémon is made: the moves they learn take their values from the rules
        Ruleset.Use(RulesPreset.Modern);
        flow.Party.Clear();
        flow.Party.Limit = Party.MaxSize;
        flow.Bag.Clear();
        flow.Pokedex.Clear();
        flow.Boxes = new PcBoxes();
        flow.Poketch.Clear();
        flow.PoketchView.Hide();
        flow.Safari.End();
        flow.BugContest.Reset();
        flow.Athlon = new AthlonRecords();
        flow.EncounterAids.RepelSteps = 0;
        flow.EncounterAids.Flute = Flute.None;
        // What the game remembers of its wild Pokémon starts over: the day's numbers drawn, no roamer loose (plan 06 · R13)
        flow.Encounters = SpecialEncounters.NewGame(fieldRandom);
        flow.Berries = BerryPatches.NewGame();
        flow.PoffinCase.Clear();
        phoneRinger.Reset();
        SilenceTheRadio();
        flow.FashionCase.Load(null);
        flow.SealCase.Load(null);
        LoadUnderground(null);
        LoadSecretBase(null, null);
        flow.Pastimes.Clear();
        flow.Apricorns.Clear();
        LoadFrontier(null);
        flow.ContestRecords.Load(null);
        flow.PokeblockCase.Clear();
        flow.HoennContests = new Models.Contest.HoennContestRecords();
        flow.ContestOutcome = null;
        flow.Radar.Clear();
        flow.RegisteredItem = null;
        flow.ExitSpot = null;
        flow.MapTrail.Clear();
        flow.LastDay = null;
        flow.Along = null;
        MapDatabase.RestoreDefeatedTrainers(Array.Empty<string>());
        // The story starts over: the flags and variables every game begins with, and everyone where those put them
        flow.Runner.Abort();
        // (Nothing of a scene's camera outlives the game it was played in, plan 09 · L4)
        world.Camera.Reset();
        flow.CameraSent = false;
        // (... nor of a scene being skipped, plan 12 · Q20)
        ResetTheSkip();
        flow.Story.Clear();
        StoryMigration.BeginNewGame(flow.Story, flow.Scripts);
        RefreshPresence(startOver: true);
        flow.Evolutions.Clear();
        flow.FriendshipSteps = 0;
        flow.Money = 3000;
        flow.PlayTime = 0f;
        // As in the games, the Trainer Card's number is drawn when the adventure begins
        PlayerIdentity.Set(name, look);
        // A new game names no other region's rival: forget those of any game played before it
        PlayerIdentity.SetRival(null);
        PlayerIdentity.SetRival(rival, NewGameRegionRecord.RivalFor(look), NewGameRegionRecord.Id);
        flow.Wardrobe.Clear();
        PlayerIdentity.SetOutfit(flow.Wardrobe.Worn);
        flow.TrainerId = fieldRandom.Next(0, 65536);
        // Whose Pokémon are shiny for whom: the card's number and its hidden half (plan 06 · R15)
        KeepTrainerNumber();
        flow.DayCare.Clear();
        flow.AdventureStarted = GameClock.Today;
        flow.Run = RunRules.Current = run ?? RunRules.Normal;
        flow.Party.Limit = RunRules.ClampLimit(flow.Run.PartyLimit);
        flow.RunState = new RunState();
        flow.NameAfterBattle = null;
        flow.TrainerScore = flow.ScoredBadges = 0;
        flow.HallOfFame = new HallOfFame();
        flow.Journal = new Journal();
        flow.PokemonIdsIssued = 0;
        flow.LinkBattles = new LinkBattleRecord();

        InitializeNewGame();
        GiveIds();
        flow.Journal.TakenUp(GameClock.Today, PlaceName());
        EnterGame();
    }

    /// <summary>
    /// Leaves the title screen into a slot's saved game, or into a fresh one when nothing is saved there. The title
    /// names the slot its card was for, and the game is the one that card showed; a tool names none and takes up
    /// the slot the game is saved in, read from its file as it is now.
    /// </summary>
    public void ContinueGame(int? slot = null, bool fromAutosave = false)
    {
        int from = slot ?? flow.SaveSlot;
        var save = fromAutosave && AutosaveFor(from) is { } autosaved ? autosaved
            : slot != null ? titleSlots.FirstOrDefault(s => s.Number == from)?.Save : SaveSlots.Read(SavePaths.Current, from).Save;
        if (save == null)
        {
            StartNewGame();
            return;
        }

        flow.SaveSlot = from;
        ApplySaveData(save);
        // The Journal opens by itself on a game left two days or more (Journal_CheckOpenOnContinue), and today's page is begun
        bool showJournal = flow.Bag.GetQuantity(ItemDatabase.Get("Journal")!) > 0 && flow.Journal.OpensOnContinue(GameClock.Today);
        flow.Journal.TakenUp(GameClock.Today, PlaceName());
        EnterGame();
        if (showJournal)
        {
            flow.After = GameState.Journal;
            flow.JournalScreen.Open();
        }
    }

    /// <summary>Fades in on the field from the black the title screen left behind.</summary>
    private void EnterGame()
    {
        // The log is told where a game begins or is taken up, so a report's last lines say what was being played
        Log.Info($"A game is entered at {PlaceKey()} {flow.Player.GridX},{flow.Player.GridY}, by the {Ruleset.Current.Preset} rules; it is saved in slot {flow.SaveSlot}.");
        gameStarted = true;
        // Nothing waits to be autosaved from another game, and the first arrival is not held to a gap (plan 12 · Q3)
        autosaveDue = null;
        autosaveDisc = -1f;
        lastAutosaveAt = float.NegativeInfinity;
        flow.TrainersLookOnArrival = true;
        flow.Arrived = true;
        flow.ScriptFade.Clear();
        flow.StartMenu.PlayerName = playerName;
        // The day's season, without a word: the game opens from black (plan 22 · U3)
        TakeTheSeason();
        KeepCharactersNear(trim: false);
        PlayAreaMusic(flow.Map, flow.Player.GridX, flow.Player.GridY);
        AnnounceLocation();
        // The region's map is painted behind the black the game opens from, so the Town Map never waits for it (plan 01 · M12)
        if (!IsHeadless && RegionMapNow is { } regionMap) RegionMapPictures.Get(regionMap);

        flow.OpenOn(GameState.Overworld);
    }

    /// <summary>Applies the options to the renderer, the clock and the sound, and optionally to the window.</summary>
    public void ApplySettings(bool window)
    {
        renderContext.SetQuality(Settings.Quality);
        // The language every later string is given in (plan 17 · N1)
        Text.Use(Settings.Language);
        // The keys and the pad as the options have them (plan 12 · Q4)
        InputManager.Use(Settings);
        GameClock.Fixed = Settings.TimeOfDay;
        // A season the options fix is the field's at once (plan 22 · U3), with nothing said
        GameClock.FixedSeason = Settings.Season;
        TakeTheSeason();
        if (AudioManager.IsMuted != Settings.Muted) AudioManager.ToggleMute();
        AudioManager.SetVolumes(Settings.MusicVolume / 100f, Settings.SoundVolume / 100f, Settings.CryVolume / 100f, Settings.AmbienceVolume / 100f);
        AudioManager.Handheld = Settings.Speakers == SpeakerMode.Handheld;
        AudioManager.Mono = Settings.Speakers == SpeakerMode.Mono;
        // A battle under way plays on with the scene as the options now have it (plan 08 · P1); the battle style is
        // the next battle's, since an offer already asked stays asked
        if (flow.Battle != null) flow.Battle.Scene = Settings.BattleScene;
        if (flow.Battle != null) flow.Battle.AutoAdvanceAfter = GameSettings.AutoAdvanceAfter(Settings.AutoAdvance);
        flow.Dialogue.CharactersPerSecond = GameSettings.CharactersPerSecond(Settings.TextSpeed);
        ModernUi.TextFrame = Settings.Frame;
        if (window && !WindowLocked) WindowSettings.Apply(Settings);
    }

    public void ToggleFullscreen()
    {
        if (WindowLocked) return; // a replay's window is the player's (plan 16 · T20)
        Settings.Fullscreen = !Settings.Fullscreen;
        ApplySettings(window: true);
        Settings.Save();
    }

    private void InitializeNewGame()
    {
        var region = NewGameRegionRecord;
        // Every other region's levels are mapped for a team that crossed into it (plan 18 · W4)
        flow.BeganIn = region.Id;
        var start = region.Start ?? RegionDatabase.First.Start!;
        flow.Map = MapDatabase.Get(start.Map);
        flow.Player = new Player(start.X, start.Y);
        flow.Player.Facing = start.Facing;
        // As in Platinum, the adventure begins with an empty bag and no Pokémon: the first comes from the
        // professor's briefcase on Route 201 (plan 02 · S4). A region whose story isn't written yet (Kanto) still
        // starts as every game did before: a Pokémon, its first starter, and a few items (plan 18 · W1: by the record's HasStory).
        if (!region.HasStory)
        {
            flow.Bag.AddItem(ItemDatabase.Get("Poké Ball")!, 10);
            flow.Bag.AddItem(ItemDatabase.Get("Potion")!, 5);
            flow.Bag.AddItem(ItemDatabase.Get("Revive")!, 2);
            var first = new Pokemon(PokemonDatabase.Get(region.Starters.FirstOrDefault() ?? "Turtwig")!, 5);
            flow.Party.Add(first);
            flow.Pokedex.RegisterSeen(first);
            flow.Pokedex.RegisterCaught(first);
        }
    }

    private void ApplySaveData(SaveData save)
    {
        // The modern rules, before any of its Pokémon are made (plan 06 · R19). A game begun under Platinum's is
        // brought over as it loads: its Pokémon take the modern moves' and species' values, effort over the modern
        // limit is cut to it and PP over a move's modern most to that (SavedPokemonData.ToPokemon), and its next
        // save says Modern. SaveData.Rules is read for nothing else.
        if (save.Rules != RulesPreset.Modern) Log.Info($"A game begun under {Ruleset.Of(save.Rules).Name} is brought over to the modern rules.");
        Ruleset.Use(RulesPreset.Modern);
        // The save's own records brought up to date (plan 07 · O2): an older save's Pokémon are given their ids
        int savedFrom = save.SaveVersion;
        int idsGiven = SaveMigration.Upgrade(save);
        if (idsGiven > 0) Log.Info($"A save of version {savedFrom} was brought up to date: {idsGiven} Pokémon were given ids.");

        // (A save made in a room laid out again since wakes on the tile the room is come into by: plan 01 · M11)
        var place = save.PlaceOn(MapDatabase.Get);
        flow.Map = MapDatabase.Get(place.Map);
        flow.Player = new Player(place.X, place.Y);
        flow.Player.Facing = place.Facing;
        // A save made out on the water wakes up there, still on its Pokémon's back; one made on a bridge, on its deck
        flow.Player.SetMode(save.Travel);
        // A save made on one of Emerald's bicycles rides the one in the bag (plan 21 · H3): the save keeps only that the
        // player was cycling
        flow.Player.Bike = save.Travel == TravelMode.Cycling ? BikeInTheBag() : BikeKind.Bicycle;
        if (save.PlayerHeight is { } standing) flow.Player.SetHeight(standing);
        flow.ExitSpot = save.Exit;
        flow.MapTrail.Restore(save.MapTrail);
        flow.LastDay = save.LastDay;
        flow.RegisteredItem = save.RegisteredItem;
        flow.Poketch.Load(save.Poketch);
        flow.PoketchView.Hide();
        if (save.Safari is { } game) flow.Safari.Resume(game.Balls, game.Steps, game.Caught);
        else flow.Safari.End();
        flow.EncounterAids.RepelSteps = Math.Max(0, save.RepelSteps);
        flow.EncounterAids.Flute = Flute.None;
        // What the game remembered of its wild Pokémon (a save from before plan 06 · R13 is given a new start); a
        // game come back to sends every roamer anywhere (the original's continue task)
        flow.Encounters = save.Encounters ?? SpecialEncounters.NewGame(fieldRandom);
        flow.Berries = (save.Berries ?? BerryPatches.NewGame()).Grown();
        flow.PoffinCase.Restore(save.Poffins);
        phoneRinger.Reset();
        SilenceTheRadio();
        // A save from before the Fashion Case that has been through the Contest Hall's first scene is owed its Glitter Powder (plan 06 · R17a)
        flow.FashionCase.Load(save.FashionCase, save.Backdrops);
        if (save.FashionCase == null && save.StoryFlags.Contains(ContestHallVisitedFlag)) flow.FashionCase.Add("Glitter Powder");
        flow.SealCase.Load(save.SealCase);
        LoadUnderground(save.Underground);
        LoadSecretBase(save.SecretBase, save.Decorations);
        flow.Pastimes.Load(save.Pastimes);
        flow.Apricorns.Load(save.Apricorns);
        LoadFrontier(save.Frontier);
        flow.ContestRecords.Load(save.ContestRecords);
        flow.PokeblockCase.Restore(save.Pokeblocks);
        flow.HoennContests = save.HoennContests ?? new Models.Contest.HoennContestRecords();
        flow.ContestOutcome = null;
        flow.Radar.Clear();
        Roamers.Scatter(flow.Encounters, fieldRandom);

        flow.Party.Clear();
        flow.Party.Limit = Party.MaxSize;
        foreach (var pData in save.Party)
        {
            var p = pData.ToPokemon();
            flow.Party.Add(p);
            flow.Pokedex.RegisterSeen(p);
            flow.Pokedex.RegisterCaught(p);
        }

        // (A save from before plan 02 · S4, whose game began with a Pokémon; one made in the opening has none yet)
        if (flow.Party.Count == 0 && save.StoryVersion < 2)
        {
            var fallback = new Pokemon(PokemonDatabase.Get("Turtwig")!, 5);
            flow.Party.Add(fallback);
            flow.Pokedex.RegisterSeen(fallback);
            flow.Pokedex.RegisterCaught(fallback);
        }

        // A Bug-Catching Contest under way comes back with its team set aside (plan 15 · E5)
        ResumeBugContest(save.BugContest);
        // The Pokéathlon's points and records (plan 15 · E6); none in an older save
        flow.Athlon = save.Athlon?.Copy() ?? new AthlonRecords();

        // Each stored Pokémon in its own place; a save from before the boxes had places lays its list out in them
        flow.Boxes = save.Boxes?.ToBoxes() ?? PcBoxes.FromList(save.BoxStorage.Select(p => p.ToPokemon()));

        flow.Bag.Clear();
        foreach (var itData in save.Inventory)
        {
            var itemDef = ItemDatabase.Get(itData.ItemName);
            if (itemDef != null)
            {
                flow.Bag.AddItem(itemDef, itData.Quantity);
            }
        }
        foreach (var (pocketName, orderName) in save.BagOrder)
            if (Enum.TryParse<ItemPocket>(pocketName, out var orderedPocket) && Enum.TryParse<BagOrder>(orderName, out var order)) flow.Bag.SetOrder(orderedPocket, order);

        flow.Pokedex.Clear();
        foreach (var seen in save.SeenSpecies) flow.Pokedex.RegisterSeen(seen);
        foreach (var caught in save.CaughtSpecies) flow.Pokedex.RegisterCaught(caught);
        foreach (var shiny in save.CaughtShiny ?? new List<int>()) flow.Pokedex.RegisterCaughtShiny(shiny);
        flow.Pokedex.Restore(save.NationalPokedex, save.Diplomas.Select(Pokedex.FromDiplomaKey), save.DexesEntered);
        // A save that had the National Pokédex before the PC grew with it (plan 03 · D13) is given the boxes now
        if (flow.Pokedex.NationalUnlocked) flow.Boxes.Grow();

        // The story as the save has it, brought up to date if an older game wrote it; then everyone where it puts them
        flow.Runner.Abort();
        world.Camera.Reset();
        flow.CameraSent = false;
        flow.Story.Restore(save.ToStory());
        StoryMigration.Upgrade(flow.Story, save.StoryVersion, flow.Party.Members.Concat(flow.Boxes.All), flow.Scripts, flow.Bag);
        flow.Story.RestoreGreetings(save.GreetedPeople);
        flow.Story.RestoreScenes(save.ScenesSeen);
        AudioManager.SetHeard(save.Heard);
        flow.Story.RestorePhrases(save.StoryPhrases);
        ResetTheSkip();
        OweTheFashionCase();
        MapDatabase.RestoreDefeatedTrainers(flow.Story.DefeatedTrainers);
        RefreshPresence(startOver: true);

        flow.Money = save.Money;
        flow.PlayTime = save.PlayTimeSeconds;
        PlayerIdentity.Set(save.PlayerName, save.Look);
        // Each region's rival (plan 18 · W4, part b): an older save's one name is the region it began in's, and the
        // rival of the region the player stands in is the one {rival} names
        PlayerIdentity.RestoreRivals(save.RivalNames, save.RivalName, save.BeganInOrHere());
        PlayerIdentity.InRegion(Here);
        // What the player wears: a save from before the wardrobe is the look's own clothes
        flow.Wardrobe.Restore(save.Outfit, save.Wardrobe);
        PlayerIdentity.SetOutfit(flow.Wardrobe.Worn);
        CharacterModels.Preload(new[] { PlayerIdentity.Character });
        // A save from before the card had a number gets one now, and keeps it
        flow.TrainerId = save.TrainerId != 0 ? save.TrainerId : fieldRandom.Next(1, 65536);
        KeepTrainerNumber();
        // The Day Care as it was left (plan 06 · R15): an older save's is empty
        DayCareSave.Restore(save.DayCare, flow.DayCare);
        // The looks the Pokédex has seen; a save from before them has its looks rebuilt from the Pokémon the player has (plan 08 · P19)
        flow.Pokedex.RestoreLooks(save.SeenLooks.Select(l => (SavedLook.SpeciesOf(l.Name)?.DexNumber ?? 0, l.Name, l.Gender)).Where(l => l.Item1 != 0), save.SpindaPersonality);
        flow.Pokedex.RebuildLooksFrom(OwnedPokemon());
        // Every Pokémon with its own id (plan 07 · O2): the save's count goes on, and one given here (the Turtwig of a
        // save from before the opening, two that came to hold one id) takes the next
        flow.PokemonIdsIssued = save.PokemonIdsIssued;
        GiveIds();
        // Where the game began, which decides whose levels are mapped (plan 18 · W4); an older save began where it stands
        flow.BeganIn = save.BeganInOrHere();
        flow.LinkBattles = LinkBattleRecord.From(save.LinkBattles);
        flow.AdventureStarted = save.Started;
        flow.Run = RunRules.Current = save.Run ?? RunRules.Normal;
        flow.Party.Limit = RunRules.ClampLimit(flow.Run.PartyLimit);
        flow.RunState = save.RunState ?? new RunState();
        flow.NameAfterBattle = null;
        flow.TrainerScore = save.TrainerScore;
        flow.HallOfFame = new HallOfFame();
        flow.HallOfFame.Restore(save.HallOfFameTotal, save.HallOfFameDebut, save.HallOfFame.Select(e => e.ToEntry()));
        flow.Journal = new Journal();
        flow.Journal.Restore(save.Journal);

        // Whoever was travelling with the player goes on doing so, behind them
        flow.Along = null;
        if (save.Partner is { } partnerId && save.PartnerPerson is { } person && flow.Map.FindPerson(person) is { } beside)
        {
            flow.Along = new Follower(beside, partnerId);
            KeepPartnerAlong();
        }
        // And so does the Pokémon out of its ball in Amity Square, which the story's own variables remember (plan 06 · R16c)
        else ResumeStroll();
        // A game saved below ground finds the tunnels laid out as it left them (plan 06 · R16a)
        if (IsUnderground) ArriveUnderground();
        // and one saved in a secret base finds it furnished (plan 21 · H10)
        if (SecretBaseLayouts.IsBaseMap(flow.Map.Name)) LayOutSecretBase(flow.Map);
    }

    /// <summary>The game as it stands, as a save.</summary>
    private SaveData BuildSave()
    {
        // A trainer marked as beaten on the map itself (a test, a tool) is beaten in the story too
        foreach (string id in MapDatabase.DefeatedTrainerIds()) flow.Story.Defeat(id);
        // Every Pokémon the player has joined since the last save is given its id now (plan 07 · O2)
        GiveIds();
        var told = flow.Story.Snapshot();
        return new()
        {
            PlayerName = playerName,
            Look = PlayerIdentity.Look,
            // The rival the introduction named, the region the game began in's; the others only once there are any (plan 18 · W4, part b)
            RivalName = PlayerIdentity.RivalOf(RegionDatabase.Get(flow.BeganIn) ?? RegionDatabase.First),
            RivalNames = PlayerIdentity.RivalNames.Keys.Any(r => !string.Equals(r, flow.BeganIn, StringComparison.OrdinalIgnoreCase))
                ? new Dictionary<string, string>(PlayerIdentity.RivalNames, StringComparer.OrdinalIgnoreCase) : null,
            Outfit =flow.Wardrobe.Worn.IsOwn ? null : flow.Wardrobe.Worn,
            Wardrobe = flow.Wardrobe.Owned.Count > 0 ? flow.Wardrobe.Owned.ToList() : null,
            TrainerId = flow.TrainerId,
            Started = flow.AdventureStarted,
            Run = flow.Run.IsNormal ? null : flow.Run,
            RunState = flow.RunState.IsEmpty ? null : flow.RunState,
            TrainerScore = flow.TrainerScore,
            HallOfFameTotal = flow.HallOfFame.Total,
            HallOfFameDebut = flow.HallOfFame.Debut,
            HallOfFame = flow.HallOfFame.Entries.Select(SavedHallOfFameEntry.From).ToList(),
            Journal = flow.Journal.All.ToList(),
            Rules = Ruleset.Current.Preset,
            CurrentMapName = flow.Map.Name,
            PlayerGridX = flow.Player.GridX,
            PlayerGridY = flow.Player.GridY,
            PlayerFacing = flow.Player.Facing,
            Travel = flow.Player.Mode,
            PlayerHeight = flow.Player.HeightOn(flow.Map),
            Exit = flow.ExitSpot,
            MapTrail = flow.MapTrail.IsEmpty ? null : flow.MapTrail.ToSave(),
            LastDay = flow.LastDay,
            RegisteredItem = flow.RegisteredItem,
            Partner = partner?.TrainerId,
            PartnerPerson = partner?.Who.Key,
            RepelSteps = flow.EncounterAids.RepelSteps,
            Poketch = flow.Poketch.Save(),
            Safari = flow.Safari.Active ? new SafariSave(flow.Safari.Balls, flow.Safari.Steps, flow.Safari.Caught) : null,
            BugContest = flow.BugContest.ToSave(),
            Athlon = flow.Athlon.Runs > 0 || flow.Athlon.Points > 0 ? flow.Athlon.Copy() : null,
            Encounters = flow.Encounters,
            Berries = flow.Berries,
            Poffins = flow.PoffinCase.Count > 0 ? flow.PoffinCase.All.ToList() : null,
            FashionCase = flow.FashionCase.ToSave(),
            Backdrops = flow.FashionCase.Backdrops.Count > 0 ? flow.FashionCase.BackdropsToSave() : null,
            SealCase = flow.SealCase.Total > 0 ? flow.SealCase.ToSave() : null,
            Underground = flow.Underground,
            SecretBase = flow.SecretBase,
            Decorations = flow.Decorations.Count > 0 ? flow.Decorations : null,
            Pastimes = flow.Pastimes.ToSave(),
            Apricorns = flow.Apricorns.ToSave(),
            Frontier = flow.Frontier,
            ContestRecords = flow.ContestRecords.IsEmpty ? null : flow.ContestRecords,
            Pokeblocks = flow.PokeblockCase.Count > 0 ? flow.PokeblockCase.ToSaved() : null,
            HoennContests = flow.HoennContests.IsEmpty ? null : flow.HoennContests,
            DayCare = DayCareSave.From(flow.DayCare),
            WorldVersion = SaveData.CurrentWorld,
            SaveVersion = SaveData.CurrentSave,
            PokemonIdsIssued = flow.PokemonIdsIssued,
            BeganIn = flow.BeganIn,
            LinkBattles = flow.LinkBattles.ToSave(),
            Party = flow.Party.Members.Select(SavedPokemonData.FromPokemon).ToList(),
            Boxes = SavedBoxes.From(flow.Boxes),
            Inventory = flow.Bag.AllItems.Select(i => new SavedItemData { ItemName = i.Name, Quantity = i.Quantity }).ToList(),
            BagOrder = flow.Bag.Orders.ToDictionary(o => o.Key.ToString(), o => o.Value.ToString()),
            SeenSpecies = flow.Pokedex.SeenSpecies.ToList(),
            CaughtSpecies = flow.Pokedex.CaughtSpecies.ToList(),
            CaughtShiny = flow.Pokedex.CaughtShiny.OrderBy(n => n).ToList(),
            SeenLooks = flow.Pokedex.AllLooks().Select(l => new SavedLook { Name = l.Look.Name, Gender = l.Look.Gender }).ToList(),
            SpindaPersonality = flow.Pokedex.SpindaPersonality,
            NationalPokedex = flow.Pokedex.NationalUnlocked,
            Diplomas = flow.Pokedex.Diplomas.Select(Pokedex.DiplomaKey).Order().ToList(),
            DexesEntered = flow.Pokedex.Entered.ToList(),
            DefeatedTrainers = told.DefeatedTrainers,
            StoryFlags = told.Flags,
            StoryVersion = StoryState.CurrentVersion,
            StoryVariables = told.Variables,
            TakenItems = told.TakenItems,
            GreetedPeople = flow.Story.Greeted.Order(StringComparer.Ordinal).ToList(),
            ScenesSeen = flow.Story.ScenesSeen.Order(StringComparer.Ordinal).ToList(),
            Heard = AudioManager.Heard.ToList(),
            StoryPhrases = flow.Story.PhrasesToSave(),
            PlayerStarter = told.PlayerStarter,
            RivalStarter = told.RivalStarter,
            Money = flow.Money,
            RegionBadges = told.Badges,
            PlayTimeSeconds = flow.PlayTime
        };
    }

    /// <summary>
    /// The game as it stands written into its slot (plan 12 · Q2): the one way the game writes a save, whoever asks
    /// (the save screen, SAVE AND QUIT, the close button's question, a script's <c>save</c>, a tool).
    /// </summary>
    private bool WriteSave(bool runsLastWord = false)
    {
        // A run that has ended (plan 13 · V4, a Nuzlocke's whiteout) is never written again: its save holds it as it
        // ended, to be loaded and looked at, and only the end itself writes it
        if (flow.RunState.Ended && !runsLastWord)
        {
            Log.Info($"The run in slot {flow.SaveSlot} has ended: the game was not saved.");
            return false;
        }
        bool written = SaveSlots.Write(SavePaths.Current, flow.SaveSlot, BuildSave());
        Log.Info(written ? $"The game was saved in slot {flow.SaveSlot} at {PlaceKey()} {flow.Player.GridX},{flow.Player.GridY}." : $"The game couldn't be saved in slot {flow.SaveSlot}.");
        // A save by hand makes this slot's autosave older than the game beside it: it goes (plan 12 · Q3)
        if (written) AutosaveFile.ClearFor(SavePaths.Current, flow.SaveSlot);
        return written;
    }

    /// <param name="quiet">True when a panel says the game was saved, so no notice is needed.</param>
    private void SaveCurrentGame(bool quiet = false)
    {
        if (!WriteSave() && flow.RunState.Ended)
        {
            ShowNotification(Text.Of("run.ended.noSave"));
            return;
        }
        if (quiet) return;
        ShowNotification(Text.Of("engine.saved"));
        AudioManager.PlaySound("save");
    }

    /// <summary>
    /// A slot deleted at the player's own word on the title (plan 12 · Q2): its save and its backup are gone for good
    /// and its card with them; where they couldn't be deleted nothing is changed and the title says so.
    /// </summary>
    private void DeleteSlot(int slot)
    {
        if (SaveSlots.Delete(SavePaths.Current, slot))
        {
            titleSlots = titleSlots.Select(s => s.Number == slot ? new SaveSlot(slot, SaveLoad.None, null) : s).ToList();
            // The slot's autosave goes with it: a game deleted at the player's word is not to come back (plan 12 · Q3)
            AutosaveFile.ClearFor(SavePaths.Current, slot);
            if (titleAutosave?.Slot == slot) titleAutosave = null;
            flow.TitleScreen.Forget(slot);
            Log.Info($"Slot {slot} was deleted at the player's word.");
        }
        else
        {
            flow.TitleScreen.Tell(TitleNotice.CouldNotDelete(slot));
            Log.Warn($"Slot {slot} couldn't be deleted: its files are still there.");
        }
    }

    /// <summary>
    /// The window's close button (plan 12 · Q1): on the title before any game, the game ends at once; anywhere else the
    /// question comes up over what is on screen (SAVE AND QUIT only where the game can be saved), and nothing under
    /// it moves until it is answered. Esc is never this: it is "back".
    /// </summary>
    public void RequestClose()
    {
        if (flow.State == GameState.WindowClosing || QuitRequested) return;
        if (ClosePrompt.QuitsAtOnce(gameStarted, flow.State))
        {
            QuitRequested = true;
            return;
        }
        flow.AskToClose();
        closePrompt.Open(CanSaveNow);
    }

    /// <summary>
    /// Whether the game can be saved this moment, as the start menu's SAVE would: in the field with nothing under way
    /// (no script, fade, trainer walking up, rod, trap, puzzle or ride on a panel).
    /// </summary>
    private bool CanSaveNow => gameStarted && flow.State == GameState.Overworld && !flow.Runner.IsRunning && !flow.Arrived
        && flow.TrainerApproach == null && flow.Fishing == null && flow.Trap == null && panelRide == null && !PuzzleHoldsField;

    /// <summary>The close button's question answered: back to the game, or out of it (after saving, for SAVE AND QUIT).</summary>
    internal void AnswerClose(CloseChoice choice)
    {
        if (flow.State != GameState.WindowClosing) return;
        switch (choice)
        {
            case CloseChoice.KeepPlaying:
                flow.KeepPlaying();
                break;
            case CloseChoice.SaveAndQuit:
                // A save that couldn't be written keeps the game open, so nothing is lost by quitting anyway
                if (WriteSave()) QuitRequested = true;
                else
                {
                    flow.KeepPlaying();
                    ShowNotification(Text.Of("engine.notSaved"));
                }
                break;
            case CloseChoice.Quit:
                QuitRequested = true;
                break;
        }
    }

    /// <summary>
    /// The factor battles, text and the evolution scene are sped up by this frame (plan 08 · P2): the options' speed-up
    /// while Run is held, 1 otherwise. A tool may set it outright (the harness shoots a battle at ×3); presentation is a
    /// function of time, so no roll changes.
    /// </summary>
    internal float Pace
    {
        get => speedUpSet ?? (Settings.SpeedUp != Core.SpeedUp.Off && InputManager.IsActionDown(GameAction.Run) ? GameSettings.SpeedFactor(Settings.SpeedUp) : 1f);
        set => speedUpSet = value;
    }
    private float? speedUpSet;

    /// <summary>Lets go of a factor a tool set, so the options and the run button decide again.</summary>
    internal void ClearSpeedUp() => speedUpSet = null;

    public void Update(float dt)
    {
        // The frame's actions are read once, as a frame (plan 16 · T11f), unless a tool or a test hands the game its own
        if (InputManager.Fed != null) { UpdateFrame(dt); return; }
        InputManager.Feed(InputManager.ReadKeys());
        try { UpdateFrame(dt); }
        finally { InputManager.Feed(null); }
    }

    private void UpdateFrame(float dt)
    {
        // Full screen on F11 or Alt+Enter: the Fullscreen action (plan 12 · Q4), read even while the game waits behind another window
        if (!IsHeadless && !InputManager.CaptureRequested && InputManager.IsActionPressed(GameAction.Fullscreen)) ToggleFullscreen();
        KeepTheReportKey(); // F8: a report of the session (plan 16 · T20)
        // Behind another window, with the option on, the game waits where it is and its sound drops (plan 16 · T3)
        bool held = HeldForFocus;
        AudioManager.Away = held;
        if (held != wasHeldForFocus)
        {
            wasHeldForFocus = held;
            Log.Info(held ? "Another window is in front: the game waits (the options' Pause in the background)." : "The window is in front again: the game goes on.");
        }
        if (held) return;
        // The developer's console holds the game while it is open, and what was saved is read again (plan 16 · T6)
        if (UpdateDev(dt)) return;

        if (gameStarted && flow.State != GameState.WindowClosing) flow.PlayTime += dt;

        flow.Toast.Update(dt);
        KeepTheAutosave(dt);
        flow.StartMenu.Animate(dt);
        closePrompt.Animate(dt);
        flow.ScriptFade.Update(dt);
        flow.Choice.Update(dt);

        // The location sign waits while a fade or another screen covers the field
        if (flow.State is GameState.Overworld or GameState.Dialogue) flow.LocationSign.Update(dt);

        // The field's small life runs on while a dialogue or a fade covers it: dust settles, a door finishes opening
        if (gameStarted && flow.State is GameState.Overworld or GameState.Dialogue or GameState.Transition)
        {
            double before = world.Life.Now;
            // Slow motion slows the field's own time (plan 09 · L4); the camera's clock, the text, a bubble and a
            // script's waits keep the clock's
            world.Life.Advance(FieldTime(dt));
            // The titans' clash turns its weather by the field's own clock (plan 21 · H8, part b)
            flow.Map.WeatherClock = world.Life.Now;
            world.RunCamera(dt);
            // The set pieces playing are heard as they go, by the field's own clock (plan 09 · L12)
            RunTheSetPieces();
            // The poison's flash fades, and the Poké Radar's patches and the honey trees stir (plan 06 · R13)
            TickEncounters(dt);
            // The Pokégear's phone counts the minutes before a call can come (plan 20 · J3)
            RunThePhonesClock(dt);
            // Thunder follows a storm's lightning, a moment after the flash
            foreach (var thunder in WeatherFx.Thunder(flow.Map.WeatherAt(flow.Player.GridX, flow.Player.GridY), before, world.Life.Now))
                AudioManager.PlaySound(thunder);
            // Rain and hail land round the player while they stand in it
            world.Life.Rainfall(flow.Map, flow.Player.PixelX / Player.TileSize + 0.5f, flow.Player.PixelY / Player.TileSize + 0.5f,
                Weathers.LandsABeat(flow.Map.WeatherAt(flow.Player.GridX, flow.Player.GridY)));
            // A diver breathes out bubbles on the sea floor, and each breath is heard (plan 21 · H5)
            if (flow.Player.Mode == TravelMode.Diving
                && world.Life.Bubbles(flow.Map, flow.Player.PixelX / Player.TileSize + 0.5f, flow.Player.PixelY / Player.TileSize + 0.5f) > 0)
                AudioManager.PlaySound("bubbles");
            flow.Player.TickBubble(dt);
            flow.Player.TickGesture(FieldTime(dt));
            // Only the people near the player are looked at each frame (plan 01 · M12): a bubble is shown over
            // someone in sight, and the map of Sinnoh holds everyone. A gesture's clock runs as a bubble's does (plan 11 · C11)
            foreach (var npc in flow.Map.NPCs.Near(flow.Player.GridX, flow.Player.GridY, Wandering.Reach, peopleNear))
            {
                npc.TickBubble(dt);
                npc.TickGesture(FieldTime(dt));
            }
            KeepTheActorActing(FieldTime(dt), peopleNear);
            // A trainer waiting for a rematch spins where they stand (MOVEMENT_TYPE_VS_SEEKER_SPIN)
            rematchSpin += dt;
            if (rematchSpin >= 0.25f)
            {
                rematchSpin = 0f;
                foreach (var npc in peopleNear.Where(n => n.ReadyForRematch && !flow.Runner.IsRunning))
                    npc.Facing = npc.Facing switch { Direction.Down => Direction.Left, Direction.Left => Direction.Up, Direction.Up => Direction.Right, _ => Direction.Down };
            }
        }

        UpdateAmbience();

        // Global mute toggle: the Mute action (M), plan 12 · Q4
        if (!InputManager.CaptureRequested && InputManager.IsActionPressed(GameAction.Mute))
        {
            Settings.Muted = !Settings.Muted;
            ApplySettings(window: false);
            Settings.Save();
            ShowNotification(Settings.Muted ? Text.Of("engine.sound.off") : Text.Of("engine.sound.on"));
        }

        switch (flow.State)
        {
            case GameState.Title:
                flow.TitleScreen.Update(dt);
                // A save the player asked to delete, and said yes to
                if (flow.TitleScreen.TakeDeletion() is { } gone) DeleteSlot(gone);
                switch (flow.TitleScreen.TakeChoice())
                {
                    case TitleChoice.Continue:
                        ContinueGame(flow.TitleScreen.Slot, flow.TitleScreen.UseAutosave);
                        break;
                    case TitleChoice.NewGame:
                        // The professor's welcome comes first; the game begins when it ends, in the slot the title
                        // found empty
                        newGameSlot = flow.TitleScreen.Slot;
                        newGameRun = flow.TitleScreen.ChosenRun;
                        flow.State = GameState.Intro;
                        flow.IntroScreen.Open(GameSettings.CharactersPerSecond(Settings.TextSpeed), NewGameRegionRecord);
                        AudioManager.PlayMusic(MusicRole.Introduction);
                        break;
                    case TitleChoice.Migrate:
                        OpenMigrate();
                        break;
                    case TitleChoice.Jukebox:
                        OpenJukebox(GameState.Title);
                        break;
                    case TitleChoice.Options:
                        flow.OptionsReturn = GameState.Title;
                        flow.State = GameState.Options;
                        flow.OptionsScreen.Open();
                        break;
                    case TitleChoice.Quit:
                        QuitRequested = true;
                        break;
                }
                break;
            case GameState.Intro:
                flow.IntroScreen.Update(dt);
                if (flow.IntroScreen.Phase == IntroPhase.Done)
                {
                    flow.IntroScreen.Close();
                    StartNewGame(flow.IntroScreen.Name, flow.IntroScreen.Look, flow.IntroScreen.RivalName, newGameRun);
                    newGameRun = null;
                }
                break;
            case GameState.Overworld:
                UpdateOverworld(dt);
                // The Catching Show's clock (plan 06 · R16b)
                TickPalPark(dt);
                // The Bug-Catching Contest's minutes, and its end when they run out (plan 15 · E5)
                TickBugContest(dt);
                break;
            case GameState.Dialogue:
                UpdateDialogue(dt * Pace);
                break;
            case GameState.Battle:
                if (flow.Battle != null)
                {
                    flow.Battle.AutoAdvanceAfter = GameSettings.AutoAdvanceAfter(Settings.AutoAdvance);
                    flow.Battle.Update(dt * Pace);
                    // The Bug-Catching Contest's minutes go on in its battles (plan 15 · E5)
                    if (flow.Battle.OnlyBall != null) flow.BugContest.Tick(dt);
                    // The battle theme turns agitated while a Pokémon of the player's is in the red, and calms for the end
                    AudioManager.LowHp = flow.Battle.Result == BattleResult.None && flow.Battle.PlayerInDanger;
                    // A recording watched (plan 08 · P11) ends back on the Vs. Recorder's list, and leaves nothing behind
                    if (flow.Battle.Watching)
                    {
                        if (flow.Battle.WatchOver) EndWatching();
                    }
                    else if (flow.Battle.IsBattleOver && flow.Battle.IsFrontier)
                    {
                        // A battle of the Battle Frontier leaves nothing behind but how it went (plan 06 · R18a)
                        EndFrontierBattle();
                    }
                    else if (flow.Battle.IsBattleOver)
                    {
                        if (flow.Battle.Result == BattleResult.PlayerVictory && flow.Battle.IsTrainerBattle) flow.Money += flow.Battle.PrizeMoney;
                        if (flow.Battle.Result == BattleResult.PlayerVictory) flow.Money += flow.Battle.PayDayMoney;
                        flow.Money = Math.Max(0, flow.Money - flow.Battle.MoneyLost);
                        AfterBattle(flow.Battle);

                        // Only a beaten trainer is done; after a loss they wait for a rematch
                        bool beaten = flow.Battle.Result == BattleResult.PlayerVictory;
                        foreach (var battleTrainer in flow.BattleTrainers)
                        {
                            battleTrainer.FinishBattle(beaten);
                            // Two people who battle as one trainer (a pair of twins) are beaten together, and the story remembers it
                            if (beaten && battleTrainer.TrainerData is { Id.Length: > 0 } pair)
                            {
                                flow.Story.Defeat(pair.Id);
                                foreach (var other in flow.Map.Everyone.Where(n => n != battleTrainer && n.TrainerData?.Id == pair.Id)) other.FinishBattle(true);
                            }
                        }
                        flow.BattleTrainers.Clear();
                        // What a script that started the battle is told of it
                        flow.ScriptOutcome = flow.Battle.Result switch
                        {
                            BattleResult.PlayerVictory => BattleOutcome.Won,
                            BattleResult.PlayerDefeat => BattleOutcome.Lost,
                            BattleResult.EnemyCaught => BattleOutcome.Caught,
                            BattleResult.PlayerRan or BattleResult.EnemyFled => BattleOutcome.Fled,
                            _ => BattleOutcome.None
                        };
                        flow.ScriptTurns = flow.Battle.Core.Turn;
                        // The Safari Game counts what it caught (the Matchup Checker is given for five in one game)
                        if (flow.Safari.Active && flow.Battle.Kind == BattleKind.Safari && flow.Battle.Result == BattleResult.EnemyCaught) flow.Safari.Caught++;
                        EndBattle();
                    }
                }
                break;
            case GameState.PartyMenu:
                KeepTheHelpersInView();
                flow.PartyScreen.Update(flow.Party, dt);
                if (flow.PartyScreen.TakeFieldMove() is var (move, index)) UseFieldMoveFromMenu(move, index);
                else if (flow.PartyScreen.TakeRename() is { } toRename) OpenNickname(toRename);
                else if (!flow.PartyScreen.IsActive)
                {
                    // A script that asked for a Pokémon to be chosen hears which
                    if (flow.PartyScreen.Entering) TakeEntered();
                    else if (flow.PartyScreen.Choosing) flow.ScriptAnswer = flow.PartyScreen.Chosen;
                    flow.State = GameState.Overworld;
                }
                break;
            case GameState.FlyMap:
                flow.TownMapScreen.Update(dt);
                if (flow.TownMapScreen.TakeChoice() is { } town)
                {
                    flow.FlyTarget = town;
                    flow.State = GameState.Overworld;
                    StartScript("common.UseFly", pokemon: flow.TownMapScreen.Flier);
                }
                else if (!flow.TownMapScreen.IsActive) CloseTownMap();
                break;
            case GameState.BagMenu:
                // The level a Rare Candy may raise a Pokémon to is the cap where the player stands (plan 18 · W4b-3)
                KeepTheHelpersInView();
                flow.BagScreen.Update(flow.Bag, flow.Party, ShowNotification, EvolutionContextNow(), dt);
                flow.RegisteredItem = flow.BagScreen.Registered;
                if (flow.BagScreen.TakeEvolution() is { } fromBag) PlayEvolutions(new[] { fromBag }, GameState.BagMenu);
                else if (!flow.BagScreen.IsActive && choosingBerryToCook && flow.BagScreen.TakePick(out var toCook)) BerryChosenToCook(toCook);
                else if (!flow.BagScreen.IsActive && choosingBerryToBlend && flow.BagScreen.TakePick(out var toBlend)) BerryChosenToBlend(toBlend);
                else if (!flow.BagScreen.IsActive && flow.BagScreen.TakePick(out var picked))
                {
                    // A script that asked for an item hears which (plan 06 · R14a)
                    flow.ScriptItem = picked?.Name;
                    flow.ScriptAnswer = picked != null ? 1 : 0;
                    flow.State = GameState.Overworld;
                }
                else if (!flow.BagScreen.IsActive)
                {
                    flow.State = GameState.Overworld;
                    // The Poffin Case opened from the bag goes back to the bag when it closes (StartMenu_ExitPoffinCase)
                    if (flow.BagScreen.TakeFieldUse() is { } usedHere)
                    {
                        if (usedHere.FieldUse == "PoffinCase") OpenPoffinCase(fromBag: true);
                        else if (usedHere.FieldUse == "PokeblockCase") OpenPokeblockCase(fromBag: true);
                        // The Town Map is looked at and put back in the bag (plan 01 · M12)
                        else if (usedHere.FieldUse == "TownMap") OpenTownMap(TownMapMode.Item, fromBag: true);
                        // The Seal Case and the Fashion Case are looked into, and close back onto the bag (plan 06 · R17b)
                        else if (usedHere.Name == BagScreen.SealCaseItem) OpenBallCapsules(fromBag: true);
                        else if (usedHere.Name == BagScreen.FashionCaseItem) OpenFashionCase(fromBag: true);
                        // The Apricorn Box is looked into and closes back onto the bag (plan 20 · J6)
                        else if (usedHere.FieldUse == "ApricornBox") OpenApricornBox(fromBag: true);
                        // The Vs. Recorder's battles, and back to the bag (plan 08 · P11)
                        else if (usedHere.FieldUse == "VsRecorder") OpenReplays(fromBag: true);
                        else UseFieldItem(usedHere);
                    }
                }
                break;
            case GameState.Evolution:
                flow.EvolutionScreen.Update(dt * Pace);
                if (!flow.EvolutionScreen.IsActive) FinishEvolution();
                break;
            case GameState.Hatch:
                flow.HatchScreen.Update(dt);
                if (!flow.HatchScreen.IsActive) FinishHatch();
                break;
            case GameState.PokedexMenu:
                flow.PokedexScreen.Update(dt);
                if (!flow.PokedexScreen.IsActive) flow.State = GameState.Overworld;
                break;
            case GameState.TrainerCard:
                flow.TrainerCardScreen.Update(dt);
                if (!flow.TrainerCardScreen.IsActive) flow.State = GameState.Overworld;
                break;
            case GameState.SaveMenu:
                flow.SaveScreen.Update(dt);
                if (flow.SaveScreen.TakeRequest())
                {
                    SaveCurrentGame(quiet: true);
                    flow.SaveScreen.MarkSaved();
                }
                // Back to the field once the panel has slid away
                if (!flow.SaveScreen.Visible) flow.State = GameState.Overworld;
                break;
            case GameState.StarterSelect:
                var chosen = flow.StarterSelectScreen.Update(dt);
                if (chosen != null)
                {
                    chosen.Met(PlaceName(), GameClock.Today);
                    flow.Party.Add(chosen);
                    flow.Pokedex.RegisterSeen(chosen);
                    flow.Pokedex.RegisterCaught(chosen);
                    // The story remembers which was taken, and with it which the rival takes
                    // (by the card's slot: in a randomised run the Pokémon on it is another species, plan 13 · V6)
                    flow.Story.ChooseStarter(flow.StarterSelectScreen.ChosenSlot);
                    flow.ScriptAnswer = Math.Max(0, Here.Starters.ToList().IndexOf(flow.StarterSelectScreen.ChosenSlot));
                    flow.State = GameState.Overworld;
                }
                break;
            case GameState.Shop:
                flow.ShopScreen.Update(flow.Bag, ref flow.Money, ShowNotification, dt);
                if (!flow.ShopScreen.IsActive)
                {
                    // What the visit came to is a line of the Journal (shop_menu.c)
                    if (ShopLine(flow.ShopScreen.Purchases, flow.ShopScreen.UnitsSold) is { } line) flow.Journal.Tell(new JournalEvent(line));
                    flow.State = GameState.Overworld;
                }
                break;
            case GameState.Wardrobe:
                flow.WardrobeScreen.Update(ref flow.Money, ShowNotification, dt);
                if (!flow.WardrobeScreen.IsActive)
                {
                    // The field and the battle draw the player as the outfit names them; what was only tried on is let go
                    PlayerIdentity.SetOutfit(flow.Wardrobe.Worn);
                    foreach (string tried in flow.WardrobeScreen.TakeTried()) CharacterModels.Forget(tried);
                    flow.State = GameState.Overworld;
                }
                break;
            case GameState.PoffinCooking:
                UpdateCooking(dt);
                break;
            case GameState.PoffinCase:
                UpdatePoffinCase(dt);
                break;
            case GameState.Pokegear:
                UpdatePokegear(dt);
                break;
            case GameState.PokeNav:
                UpdatePokeNav(dt);
                break;
            case GameState.AlphPuzzle:
                // The script that opened the panel hears whether the picture was put back together (plan 20 · J5)
                flow.AlphPuzzleScreen.Update(dt);
                if (!flow.AlphPuzzleScreen.IsActive)
                {
                    flow.ScriptAnswer = flow.AlphPuzzleScreen.Outcome == true ? 1 : 0;
                    flow.State = GameState.Overworld;
                }
                break;
            case GameState.BugContestResults:
                // The Bug-Catching Contest's board (plan 15 · E5): the script that opened it goes on once it is read
                flow.BugContestResultsScreen.Update(dt);
                if (!flow.BugContestResultsScreen.IsActive) flow.State = GameState.Overworld;
                break;
            case GameState.Athlon:
                // The Pokéathlon (plan 15 · E6): the script that opened it hears the player's place, 0 when backed out of
                flow.AthlonScreen.Update(dt);
                if (!flow.AthlonScreen.IsActive)
                {
                    flow.ScriptAnswer = flow.AthlonScreen.Outcome;
                    flow.State = GameState.Overworld;
                }
                break;
            case GameState.Phrases:
                // The script that asked hears whether a phrase was given (plan 08 · P9), and the story keeps it
                flow.PhraseScreen.Update(dt);
                if (!flow.PhraseScreen.IsActive) ClosePhrases();
                break;
            case GameState.RivalNaming:
                // The script that asked hears 1 once the region's rival is named (plan 18 · W4, part b)
                flow.RivalNamingScreen.Update(dt);
                if (!flow.RivalNamingScreen.IsActive) CloseRivalNaming();
                break;
            case GameState.Nickname:
                // The party menu's RENAME (plan 12 · Q19): back to the menu with the new name
                flow.NicknameScreen.Update(dt);
                if (!flow.NicknameScreen.IsActive) CloseNickname();
                break;
            case GameState.SuperContest:
                UpdateContest(dt);
                break;
            case GameState.SealCase:
                UpdateBallCapsules(dt);
                break;
            case GameState.SealShop:
                UpdateSealShop(dt);
                break;
            case GameState.FashionCase:
                UpdateFashionCase(dt);
                break;
            case GameState.ApricornBox:
                UpdateApricornBox(dt);
                break;
            case GameState.Mining:
                UpdateMining(dt);
                break;
            case GameState.Underground:
                UpdateUndergroundScreen(dt);
                break;
            case GameState.Decoration:
                UpdateDecoration(dt);
                break;
            case GameState.SlotMachine:
                UpdateSlotMachine(dt);
                break;
            case GameState.FrontierRecords:
                UpdateFrontierRecords(dt);
                break;
            case GameState.FactoryRentals:
                UpdateFactoryRentals(dt);
                break;
            case GameState.HallOfFameCeremony:
                UpdateCeremony(dt);
                break;
            case GameState.Credits:
                UpdateCredits(dt);
                break;
            case GameState.HallBoard:
                UpdateHallBoard(dt);
                break;
            case GameState.CastleCounter:
                UpdateCastleCounter(dt);
                break;
            case GameState.ArcadeBoard:
                UpdateArcadeBoard(dt);
                break;
            case GameState.ReplayList:
                UpdateReplayList(dt);
                break;
            case GameState.Migrate:
                UpdateMigrate(dt);
                break;
            case GameState.Jukebox:
                UpdateJukebox(dt);
                break;
            case GameState.Appeals:
                UpdateAppeals(dt);
                break;
            case GameState.PokeblockCase:
                UpdatePokeblockCase(dt);
                break;
            case GameState.Blender:
                UpdateBlender(dt);
                break;
            case GameState.Bracket:
                UpdateBracket(dt);
                break;
            case GameState.Tournament:
                UpdateTournament(dt);
                break;
            case GameState.FirstStart:
                UpdateFirstStart(dt);
                break;
            case GameState.Journal:
                flow.JournalScreen.Update(flow.Journal, dt);
                if (!flow.JournalScreen.IsActive) flow.State = GameState.Overworld;
                break;
            case GameState.HallOfFame:
                flow.HallOfFameScreen.Update(flow.HallOfFame, dt);
                if (!flow.HallOfFameScreen.IsActive) flow.State = GameState.Overworld;
                break;
            case GameState.PCStorage:
                KeepTheRunInView();
                flow.PCScreen.Update(flow.Party, flow.Boxes, ShowNotification, dt);
                if (!flow.PCScreen.IsActive)
                {
                    if (flow.PCScreen.Changed) flow.Journal.Tell(new JournalEvent(JournalEventKind.UsedPcBox));
                    // A Deerling or Sawsbuck taken out of a box wears the season here (plan 22 · U4)
                    KeepTheCoats();
                    flow.State = GameState.Overworld;
                }
                break;
            case GameState.Options:
                KeepTheRunInView();
                flow.OptionsScreen.Animate(dt);
                if (flow.OptionsScreen.Update(Settings))
                {
                    ApplySettings(window: true);
                    Settings.Save();
                }
                if (flow.OptionsScreen.TakeJukeboxRequest()) OpenJukebox(GameState.Options);
                else if (!flow.OptionsScreen.IsActive) flow.State = flow.OptionsReturn;
                break;
            case GameState.Transition:
                UpdateTransition(dt);
                break;
            case GameState.WindowClosing:
                // Nothing under the question moves; the close button again does nothing more
                if (closePrompt.Update() is var answer and not CloseChoice.None) AnswerClose(answer);
                break;
        }
    }

    private List<AmbienceLayer> fieldAmbience = new();
    private (Map? Map, int X, int Y, FieldWeather Weather) ambienceAt;

    /// <summary>
    /// The field's ambience (plan 05 · A7): the beds heard where the player stands, worked out again when they step
    /// onto another tile or the weather changes, and silence while a battle, an evolution, the title or the
    /// introduction has the screen.
    /// </summary>
    private void UpdateAmbience()
    {
        bool heard = gameStarted && flow.Map != null && flow.Player != null
            && flow.Shown is not (GameState.Title or GameState.Intro or GameState.Battle or GameState.Evolution or GameState.Hatch
                or GameState.HallOfFameCeremony or GameState.Credits);
        if (!heard)
        {
            if (fieldAmbience.Count > 0) fieldAmbience = new List<AmbienceLayer>();
            ambienceAt = default;
        }
        else
        {
            var at = (flow.Map, flow.Player!.GridX, flow.Player.GridY, flow.Map!.WeatherAt(flow.Player.GridX, flow.Player.GridY));
            if (at != ambienceAt)
            {
                fieldAmbience = FieldAmbience.Around(flow.Map, flow.Player.GridX, flow.Player.GridY);
                ambienceAt = at;
            }
        }
        AudioManager.SetAmbience(fieldAmbience);
    }

    /// <summary>The pan of a sound made at a tile column of the field: from its side of the screen.</summary>
    internal float PanAt(int tileX) => FieldAmbience.PanOf(tileX - flow.Player.GridX);

    private void UpdateOverworld(float dt)
    {
        // What the field itself does runs at the field's pace, which a scene's slow motion lowers (plan 09 · L4)
        float fieldDt = FieldTime(dt);
        // What field moves left in force (Strength, Flash, Defog), and boulders sliding on from a push
        KeepFieldMovesInForce();
        if (flow.Map.Name == DewfordLights.GymMap) DewfordLights.Keep(flow.Map, flow.Story, fieldDt);
        KeepThePyramidsLight(fieldDt);
        KeepPuzzleInForce(fieldDt);
        ShatterSnowball();
        KeepTheClock();
        KeepBerriesInView();
        KeepApricornTreesInView();
        SlideBoulders(fieldDt);
        flow.PoketchView.Update(dt);
        // The app on the Pokétch runs on whether it is out or not, as the original's lower screen does
        flow.Poketch.ShowsHiddenMoves = FieldHelp.On(Settings.FieldHelpers, Ruleset.Current.Preset);
        if (flow.Poketch.Enabled) flow.Poketch.State?.Update(dt, PoketchNow());
        // The radar's chains count toward the Trainer Counter's records whichever app is showing
        if (flow.Poketch.Has(PoketchApp.TrainerCounter) && (flow.Radar.Count, flow.Radar.Species, flow.Radar.Active) != radarFollowed)
        {
            radarFollowed = (flow.Radar.Count, flow.Radar.Species, flow.Radar.Active);
            TrainerCounterApp.Follow(flow.Poketch, flow.Radar);
        }
        // Who is on the map follows the story's flags as soon as they change, whatever changed them (a script, a
        // first arrival, a field move, a tool): only a script's end and an arrival looked before, so a flag set
        // anywhere else left people where they were until the next of those
        RefreshPresence();
        // A Pokémon caught under the Nuzlocke is named before the field goes on (plan 13 · V4)
        if (NameTheCatch()) return;

        // Whoever travels with the player keeps up, whatever else has the field
        flow.Along?.Update(fieldDt, flow.Player.Stride);
        KeepTheStroll(dt);

        // A warp panel has the player: they turn on the spot until the picture closes, and again as it opens
        if (panelRide != null)
        {
            RideThePanel(dt);
            return;
        }

        // A scene being skipped has the field while its dark closes and opens (plan 12 · Q20)
        if (UpdateSkip(dt)) return;

        // The battle just fought, asked about before a script that started it goes on (plan 08 · P10)
        if (AskToRecord()) return;

        // A script has the field: the keys do nothing until it ends, and people moving of their own accord only
        // finish the step they are on
        if (flow.Runner.IsRunning)
        {
            flow.Wandering.Continue(fieldDt);
            UpdateScript(dt);
            return;
        }

        // A Gym's puzzle has the field for a moment: a punching bag on its run (plan 01 · M9)
        if (PuzzleHoldsField) return;

        // A rod is out: the keys are the rod's until it is put away
        if (flow.Fishing != null)
        {
            UpdateFishing(dt);
            return;
        }

        // A trap of the Underground has the player until they are free of it (plan 06 · R16a)
        if (flow.Trap != null && !flow.Arrived)
        {
            UpdateTrap(dt);
            return;
        }

        // The place just come to runs its own script first, if it has one
        if (flow.Arrived)
        {
            flow.Arrived = false;
            RefreshPresence();
            // Out of a warp the original walks the player a step onto the tile in front, and that step can start a
            // trigger, once the place's own script is done (Cheryl at Eterna Forest's edge, plan 02 · S6)
            bool stepped = flow.SteppedOutOfWarp;
            flow.SteppedOutOfWarp = false;
            if (StartEnterScript())
            {
                flow.TriggerAfterEnter |= stepped;
                return;
            }
            if (stepped && TryStepTrigger()) return;
        }

        // A trainer has spotted the player: nothing else happens until they have walked up (both of them, when two
        // came together)
        if (flow.TrainerApproach != null)
        {
            flow.TrainerApproach.Update(dt, flow.Player);
            flow.PairApproach?.Update(dt, flow.Player);
            if (flow.TrainerApproach.IsDone && flow.PairApproach?.IsDone != false)
            {
                var trainer = flow.TrainerApproach.Trainer;
                var pair = flow.PairApproach?.Trainer;
                flow.TrainerApproach = null;
                flow.PairApproach = null;
                if (pair != null) StartScript(FieldScripts.TrainerPair, trainer, pair: pair);
                else ChallengeTrainer(trainer);
            }
            return;
        }

        if (flow.TrainersLookOnArrival)
        {
            flow.TrainersLookOnArrival = false;
            if (CheckTrainerSight()) return;
        }

        // Check Start Menu
        if (flow.StartMenu.IsActive)
        {
            HandleStartMenuChoice(flow.StartMenu.Update());
            return;
        }

        // The Pokétch in hand: the arrows and buttons are its, and the player stands still
        if (flow.PoketchView.InHand)
        {
            UpdatePoketchInHand(dt);
            return;
        }

        if (InputManager.IsActionPressed(GameAction.Menu))
        {
            flow.StartMenu.HasPokedex = flow.Story.Has(StoryState.PokedexFlag);
            flow.StartMenu.HasPokemon = flow.Party.Count > 0;
            flow.StartMenu.HasBoxes = BoxLinkOn;
            flow.StartMenu.HasPokegear = HasPokegear;
            flow.StartMenu.HasPokeNav = HasPokeNav;
            flow.StartMenu.Objective = NextObjectiveLine;
            flow.StartMenu.Open();
            // The team's menu sprites are asked for as the menu opens, so that they are there by the time a screen
            // shows them: only the opening's are made at the start, and a sprite long unshown is let go (plan 03 · D13)
            foreach (var member in flow.Party.Members.Where(p => !p.IsEgg)) PokemonSprites.Request(member.LookName);
            return;
        }

        // The Pokétch comes out or goes away; its side button changes the app
        if (InputManager.IsActionPressed(GameAction.Poketch)) flow.PoketchView.Toggle(flow.Poketch);
        if (InputManager.IsActionPressed(GameAction.PoketchApp)) flow.PoketchView.NextApp(flow.Poketch);
        if (InputManager.IsActionPressed(GameAction.PoketchTouch) && !flow.Player.IsMoving && flow.PoketchView.TakeInHand(flow.Poketch, PoketchNow())) return;

        // The key item kept on the item button (the original's Y button)
        if (InputManager.IsActionPressed(GameAction.Item) && !flow.Player.IsMoving)
        {
            if (flow.RegisteredItem != null && ItemDatabase.Get(flow.RegisteredItem) is { } kept && flow.Bag.GetQuantity(kept) > 0) UseFieldItem(kept);
            else ShowNotification(Text.Of("engine.registerOne"));
            return;
        }

        // On the Bicycle the run button changes gear (the original's B button)
        bool runDown = InputManager.IsActionDown(GameAction.Run);
        // Below ground the run button pressed while standing still sends out the radar's pulse (the original's touch on
        // the lower screen; plan 06 · R16a). Held on the move it runs, as the original's B does there
        if (IsUnderground && !InTheBase && runDown && !runWasDown && !flow.Player.IsMoving) PulseRadar();
        if (flow.Player.Mode == TravelMode.Cycling && flow.Player.Bike == BikeKind.Bicycle && runDown && !runWasDown)
        {
            flow.Player.FastGear = !flow.Player.FastGear;
            AudioManager.PlaySound("gear");
            ShowNotification(flow.Player.FastGear ? Text.Of("engine.gear.top") : Text.Of("engine.gear.low"));
        }
        runWasDown = runDown;

        // People move about of their own accord, kept out of the player's way
        if (!PeopleStayPut) flow.Wandering.Update(dt, flow.Map, (flow.Player.GridX, flow.Player.GridY), flow.Player.Heading, peopleRandom ??= Dice.New());

        // Overworld Player Movement
        flow.Player.Moves = FieldMovement.MovesOf(flow.Party) | FieldHelp.Moves(GrantedMoves);
        flow.Player.Autorun = Settings.Autorun;
        flow.Player.Lead = WildLead.Of(flow.Party, flow.EncounterAids);
        flow.Player.Moment = momentOf ??= EncounterMomentNow;
        if (Steering is { } steer) flow.Player.Advance(dt, flow.Map, steer.Want, steer.Run, MeetInTheField, HandleWarp, OnStep);
        else flow.Player.Update(dt, flow.Map, MeetInTheField, HandleWarp, OnStep);
        // A snowball the slide broke bursts as it happens (plan 01 · M9, part 2b)
        ShatterSnowball();

        TrailBehind();

        // A door that leads somewhere opens as the player steps up to it: the door's own tile, or the open way
        // into a porch with the door in the wall behind
        var (headX, headY) = flow.Player.Heading;
        if (flow.Player.IsMoving && flow.Map.GetWarpAt(headX, headY) != null && WorldRenderer.DoorPlace(flow.Map, headX, headY) is { } opening
            && world.Life.OpenDoor(flow.Map, headX, headY))
            AudioManager.PlaySound(opening.Glass ? "door_slide" : "door_open");

        // The door the player came out of shuts behind them
        if (world.Life.TakeShutting() is { } shutting && WorldRenderer.DoorPlace(shutting.Map, shutting.X, shutting.Y) is { } shut)
            AudioManager.PlaySound(shut.Glass ? "door_slide" : "door_close");

        if (flow.TrainerApproach != null || flow.State != GameState.Overworld) return;

        // Turning a full circle is how one Pokémon evolves
        if (flow.Spin.Update(flow.Player.Facing, dt) && PlayEvolutions(FindEvolutions(flow.Party.Members, EvolutionTrigger.Spin, cancellable: false), GameState.Overworld))
            return;

        // Interaction (Z / Space)
        if (!flow.Player.IsMoving && InputManager.IsActionPressed(GameAction.Confirm))
        {
            TryInteract();
        }
    }

    /// <summary>
    /// After each step: the party walks along (Platinum raises friendship every 128 steps, and the Pokémon at
    /// the head of the party counts its steps for the evolutions that ask for a long walk), then trainers look.
    /// </summary>
    // The people within reach of the player this frame: a list of the engine's own, so a frame makes none
    private readonly List<NPC> peopleNear = new();

    // Where the people round the player were last made ready: the map and the chunk (plan 11 · C1)
    private (string Map, int X, int Y) charactersReadyAt;

    /// <summary>How far from the player people's looks are made ready, in tiles: two chunks, well beyond what is in view.</summary>
    private const int CharacterReach = 64;

    /// <summary>
    /// Starts building, in the background, the looks of everyone within reach of the player (those a flag hides for
    /// now too) and the player's own, so none holds up a frame as it comes into sight (plan 11 · C1). On a warp
    /// (<paramref name="trim"/>), every other look's rig is let go: the map left behind is behind a fade.
    /// </summary>
    private void KeepCharactersNear(bool trim)
    {
        charactersReadyAt = (flow.Map.Name, flow.Player.GridX >> 5, flow.Player.GridY >> 5);
        var looks = flow.Map.Everyone
            .Where(n => !n.IsThing && !n.IsPokemon && Math.Abs(n.GridX - flow.Player.GridX) <= CharacterReach && Math.Abs(n.GridY - flow.Player.GridY) <= CharacterReach)
            .Select(n => PlayerIdentity.CharacterFor(n.NpcType))
            .Append(PlayerIdentity.Character).Append("PLAYER").Append("DAWN")
            .ToList();
        // (and the player's look holding a rod, which a cast builds: plan 11 · C8)
        if (trim) CharacterModels.Trim(looks.Append(CharacterStyle.Holding(PlayerIdentity.Character, HeldProp.Rod)));
        CharacterModels.Preload(looks);
        // The Pokémon that stand in the field near the player (plan 10 · F1), whose sprites are baked from their
        // models: only those no earlier run has baked (plan 03 · D13), so that they are there before they are in view
        foreach (string species in flow.Map.Everyone
                     .Where(n => n.IsPokemon && n.Species != null && Math.Abs(n.GridX - flow.Player.GridX) <= CharacterReach && Math.Abs(n.GridY - flow.Player.GridY) <= CharacterReach)
                     .Select(n => n.Species!).Distinct())
            if (!FieldSprites.Ready(species)) flow.WantModel(species);
    }

    private readonly HeldKey poketchAcross = new(), poketchDown = new();
    private PoketchContext? poketchContext;

    /// <summary>What the Pokétch's apps read of the game this frame (plan 06 · R14b).</summary>
    internal PoketchContext PoketchNow()
    {
        var c = poketchContext ??= new PoketchContext
        {
            Poketch = flow.Poketch,
            Sound = name => AudioManager.PlaySound(name),
            Cry = p => AudioManager.PlayCry(p)
        };
        c.Party = flow.Party;
        c.Now = GameClock.Clock();
        c.Map = flow.Map;
        c.X = flow.Player?.GridX ?? 0;
        c.Y = flow.Player?.GridY ?? 0;
        c.Outside = flow.ExitSpot is { } outside ? (outside.X, outside.Y) : null;
        c.Story = flow.Story;
        c.Pokedex = flow.Pokedex;
        c.Encounters = flow.Encounters;
        c.Radar = flow.Radar;
        c.Berries = flow.Berries;
        c.Rng = fieldRandom;
        c.Granted = GrantedMoves.ToList();
        // The Day Care's two, at the levels their steps have brought them to, and whether an Egg waits (plan 06 · R15)
        c.DayCare = flow.DayCare.Left;
        c.DayCareLevels = Enumerable.Range(0, DayCare.Places).Where(i => flow.DayCare[i] != null).Select(flow.DayCare.LevelNow).ToList();
        c.DayCareEgg = flow.DayCare.HasEgg;
        return c;
    }

    /// <summary>The Pokétch in the player's hand: the arrows move its cursor (held, they go on), confirm touches, cancel lets go.</summary>
    private void UpdatePoketchInHand(float dt)
    {
        var context = PoketchNow();
        int across = poketchAcross.Advance(dt, InputManager.Axis(GameAction.Left, GameAction.Right), InputManager.Axis(GameAction.Left, GameAction.Right, held: true));
        int down = poketchDown.Advance(dt, InputManager.Axis(GameAction.Up, GameAction.Down), InputManager.Axis(GameAction.Up, GameAction.Down, held: true));
        for (int i = 0; i < Math.Abs(across); i++) flow.PoketchView.MoveCursor(flow.Poketch, context, Math.Sign(across), 0);
        for (int i = 0; i < Math.Abs(down); i++) flow.PoketchView.MoveCursor(flow.Poketch, context, 0, Math.Sign(down));
        if (InputManager.IsActionPressed(GameAction.Confirm)) flow.PoketchView.Touch(flow.Poketch, context);
        else if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.PoketchTouch)) flow.PoketchView.LetGo();
    }

    /// <summary>The player has set off from a tile: whoever walks behind them goes there next.</summary>
    private void TrailBehind()
    {
        if (flow.Along == null || flow.Map.Follower != flow.Along.Who) return;
        var head = flow.Player.Heading;
        if (head != (flow.Player.GridX, flow.Player.GridY) && head != partnerHeading) flow.Along.PlayerLeft(flow.Player.GridX, flow.Player.GridY, head);
        partnerHeading = head;
    }

    private bool OnStep()
    {
        if (charactersReadyAt != (flow.Map.Name, flow.Player.GridX >> 5, flow.Player.GridY >> 5)) KeepCharactersNear(trim: false);
        // A step below ground: a base's way in or out, a trap (plan 06 · R16a)
        if (IsUnderground && UndergroundStep()) return true;
        // The steps the game has counted (RECORD_STEPS), and a step of Pal Park's Catching Show (plan 06 · R16b)
        flow.Pastimes.Steps++;
        // And the steps a Pokémon out of its ball has walked since it last turned something up (plan 06 · R16c;
        // SystemVars_IncrementAmitySquareStepCount, which the original counts everywhere and reads only in the square)
        if (flow.Story.Var(AmitySquare.ActiveVar) == 1) AmitySquare.CountStep(flow.Story);
        if (PalParkStep()) return true;
        if (++flow.FriendshipSteps >= FriendshipRules.WalkCycleSteps)
        {
            flow.FriendshipSteps = 0;
            foreach (var p in flow.Party.Members) FriendshipRules.Apply(p, FriendshipEvent.WalkCycle, fieldRandom);
        }
        if (flow.Party.Count > 0) Evolution.CountStep(flow.Party.Members[0]);
        flow.Poketch.Step();
        // The cell of the region's map stood in is written down as it changes (OverworldMapHistory_Push, plan 01 · M12)
        if (RegionMap.ForMap(flow.Map.Name) is { } region && region.Map == flow.Map.Name)
        {
            var (cellX, cellY) = RegionMap.CellOf(flow.Player.GridX, flow.Player.GridY);
            flow.MapTrail.Push(cellX, cellY, flow.Player.Facing);
        }
        // The Vs. Seeker charges in the bag, and the rematches it found end after a hundred steps (plan 06 · R12)
        if (VsSeeker.Step(flow.Story, flow.Bag.GetQuantity(ItemDatabase.Get(VsSeeker.Item)!) > 0)) StopRematches(Direction.Down);
        // Riding out onto the water or back onto land, or getting on or off the bicycle, changes the music
        if (flow.Player.Mode != musicTravel) PlayFieldMusic();
        bool entered = EnterArea();
        // A step into or out of a stretch with weather and music of its own (plan 21 · H4: the desert's storm, as
        // Emerald's sandstorm and sun triggers change the song) changes the music, as an area's border does
        var zone = flow.Map.ZoneAt(flow.Player.GridX, flow.Player.GridY);
        if (zone != flow.CurrentZone)
        {
            flow.CurrentZone = zone;
            if (!entered) PlayFieldMusic();
        }
        GatherAsh();

        // What the step leaves behind: a print, dust, leaves, a ring on the water, a splash where they rode out
        // onto it, dust where they came down from a hop
        StepLeavesItsMark();

        // A step onto one of the Canalave Gym's platforms is a ride, and nothing more (plan 01 · M9)
        if (TryRideLift()) return true;
        // A cracked floor gives way under whoever walks onto it (plan 21 · H8, part c: Sky Pillar's; Emerald's cracked
        // floor step callback): down to the floor below, onto the same tile, unless ridden over on the Bicycle
        if (FallThroughCrackedFloor()) return true;
        // Poison bites every fourth step, the Poké Radar charges and its patches out of sight go still (plan 06 · R13)
        if (EncountersStep()) return true;
        // The Safari Game counts its steps, and its last one ends it (plan 01 · M7)
        if (flow.Safari.Step() && StartScript(FieldScripts.SafariTimeUp)) return true;
        // The Day Care's couple walk the step too, and an Egg of the team may hatch (Field_UpdateDaycare, plan 06 · R15)
        if (DayCareStep()) return true;
        // A Repel's last step says it has worn off (Repel_UpdateSteps, plan 06 · R11)
        if (flow.EncounterAids.Step() && StartScript(FieldScripts.RepelWoreOff)) return true;

        // The story comes first: the script of the place walked into, then of the tiles stepped on; only then do trainers
        // look. A step into a place onto a trigger does both, the place's first (as the original runs a map's own
        // script as it loads, before its coordinate events): the trigger is tried once that script has ended
        if (entered)
        {
            flow.TriggerAfterEnter = true;
            return true;
        }
        if (TryStepTrigger()) return true;
        if (CheckTrainerSight()) return true;
        // Last, a contact may ring the Pokégear (plan 20 · J3)
        return KurtRingsOnAStep() || PhoneRingsOnAStep();
    }

    /// <summary>
    /// On the overworld a town runs into its routes with no door between them: stepping over the border brings
    /// up the new place's name and its music, as arriving through a door does.
    /// </summary>
    /// <returns>True when the place walked into has a script of its own, which has been started.</returns>
    /// <summary>The name of the place the player stands in, with whoever it names filled in: where a Pokémon met here was met.</summary>
    private string PlaceName() => PlayerIdentity.Fill(flow.Map.DisplayNameAt(flow.Player.GridX, flow.Player.GridY));

    /// <summary>
    /// A step through grass where volcanic ash falls (plan 21 · H4; Emerald's STEP_CB_ASH on Route 113) brushes the ash off
    /// it, and with the Soot Sack in the bag the ash goes into it: VAR_ASH_GATHER_COUNT, never over 9,999, which the glass
    /// workshop trades for its flutes. Each tile gives its ash once until the player comes to the place again.
    /// </summary>
    private void GatherAsh()
    {
        int x = flow.Player.GridX, y = flow.Player.GridY;
        if (flow.Map.WeatherAt(x, y) != FieldWeather.Ash || !TileBehaviors.HasEncounters(flow.Map.BehaviourAt(x, y))) return;
        if (!flow.AshBrushed.Add((x, y))) return;
        if (ItemDatabase.Get(SootSack) is not { } sack || flow.Bag.GetQuantity(sack) == 0) return;
        flow.Story.SetVar(AshCountVariable, Math.Min(MostAsh, flow.Story.Var(AshCountVariable) + 1));
    }

    /// <summary>The bag's key item that gathers ash, the story's count of it, and the most it holds (Emerald's numbers).</summary>
    public const string SootSack = "Soot Sack";
    public const string AshCountVariable = "VAR_ASH_GATHER_COUNT";
    public const int MostAsh = 9999;

    private bool EnterArea()
    {
        var area = flow.Map.AreaAt(flow.Player.GridX, flow.Player.GridY);
        if (area == flow.CurrentArea) return false;
        bool another = flow.CurrentArea != null && area != null;
        flow.CurrentArea = area;
        if (area == null) return false;
        // Another place: what lasted only while the player stayed in the last one is forgotten, and the roamers move on
        // (FieldSystem_InitFlagsOnMapChange, not during a Safari Game; plan 06 · R13)
        if (another) ChangePlace();
        if (another && !flow.Safari.Active) Roamers.PlayerWalkedInto(flow.Encounters, area.Key, fieldRandom);
        NoteArrival();
        PlayFieldMusic();
        // As the language writes it (plan 17 · N6b): the area's own name was shown in English under every language
        if (!string.IsNullOrEmpty(area.DisplayName)) flow.LocationSign.Show(flow.Map.ShownNameAt(flow.Player.GridX, flow.Player.GridY), SeasonHere);
        return StartEnterScript();
    }

    // ---------------------------------------------------------------- evolution

    /// <summary>
    /// What the evolution rules need to know about this moment: the team, the bag, the hour and the place.
    /// There is no weather in the field yet, so it never rains.
    /// </summary>
    private EvolutionContext EvolutionContextNow() => new()
    {
        Party = flow.Party,
        Bag = flow.Bag,
        IsNight = GameClock.IsNight,
        IsDusk = GameClock.Now == TimeOfDay.Twilight,
        IsRaining = Weathers.IsRain(flow.Map.WeatherAt(flow.Player.GridX, flow.Player.GridY)),
        Sites = flow.Map.EvolutionSitesAt(flow.Player.GridX, flow.Player.GridY),
        Region = RegionDatabase.RegionOfMap(flow.Map.Name)?.Name,
        // The spin just ended, for Alcremie's cream (plan 06 · R29)
        Spin = flow.Spin.Last
    };

    /// <summary>The evolutions a trigger sets off among these Pokémon right now.</summary>
    private List<EvolutionRequest> FindEvolutions(IEnumerable<Pokemon> candidates, EvolutionTrigger trigger, bool cancellable, PokemonSpecies? tradedFor = null)
    {
        var context = EvolutionContextNow();
        context.TradedFor = tradedFor;
        var found = new List<EvolutionRequest>();
        foreach (var p in candidates)
        {
            if (flow.Party.Members.Contains(p) && Evolution.Find(p, trigger, context) is { } evolution)
                found.Add(new EvolutionRequest(p, evolution, cancellable));
        }
        return found;
    }

    /// <summary>
    /// Fades into the evolution scene and plays these one after the other, then fades to <paramref name="returnTo"/>.
    /// False (and nothing happens) when there are none.
    /// </summary>
    private bool PlayEvolutions(IReadOnlyCollection<EvolutionRequest> requests, GameState returnTo)
    {
        if (requests.Count == 0) return false;
        // The scene shows each Pokémon before and after: their models are built while the screen fades out
        var shown = new List<string>();
        foreach (var request in requests)
        {
            flow.Evolutions.Enqueue(request);
            shown.Add(request.Pokemon.LookName);
            shown.Add(PokemonLook.Of(Evolution.ModelAfter(request.Pokemon, request.Evolution), request.Pokemon.Gender == Gender.Female, request.Pokemon.IsShiny));
        }
        foreach (var name in shown) flow.WantModel(name);
        flow.EvolutionReturn = returnTo;
        StartTransition(GameState.Evolution, () =>
        {
            AwaitModels(shown);
            BeginNextEvolution();
        });
        return true;
    }

    private bool BeginNextEvolution()
    {
        while (flow.Evolutions.Count > 0)
        {
            var request = flow.Evolutions.Dequeue();
            var context = EvolutionContextNow();
            var evolution = request.Evolution;

            // An earlier evolution in the queue may have changed things (the party, the bag): look again
            if (request.Cancellable)
            {
                var trigger = Evolution.TriggerOf(evolution.Method) ?? EvolutionTrigger.LevelUp;
                if (Evolution.Find(request.Pokemon, trigger, context) is not { } still) continue;
                evolution = still;
            }
            flow.EvolutionScreen.Begin(request.Pokemon, evolution, context, request.Cancellable);
            return true;
        }
        return false;
    }

    /// <summary>The scene has ended: the new species goes into the Pokédex, then the next evolution or the way back.</summary>
    private void FinishEvolution()
    {
        if (flow.EvolutionScreen.Outcome is { } outcome)
        {
            if (flow.EvolutionScreen.Pokemon is { } evolved) flow.Pokedex.RegisterCaught(evolved);
            else flow.Pokedex.RegisterCaught(outcome.Into.DexNumber);
            if (outcome.Shed != null) flow.Pokedex.RegisterCaught(outcome.Shed);
        }
        if (BeginNextEvolution()) return;

        // The evolution's theme gives way to the field's again, wherever the scene goes back to
        StartTransition(flow.EvolutionReturn, PlayFieldMusic);
    }

    /// <summary>
    /// A Pokémon arrives by trade: the one call a trade needs to make here, whether it is with a friend over the
    /// internet (plan 07) or with a character in the game. It takes the place of the Pokémon given for it
    /// (<paramref name="given"/>; null when nothing left the team), starts over at its species' base friendship,
    /// goes into the Pokédex, and evolves there and then if trading is what it was waiting for, which can't be
    /// stopped. Saving and what the other side receives are the caller's.
    /// </summary>
    public bool ReceiveTradedPokemon(Pokemon received, Pokemon? given = null)
    {
        // A Pokémon whose id the player holds already is one received before (a receipt played twice): it is
        // refused, and nothing changes (plan 07 · O2). One that comes with no id (a character's) is given one.
        if (received.Id != Guid.Empty && OwnedPokemon().Any(p => p != given && p.Id == received.Id)) return false;

        int place = given != null ? flow.Party.Members.IndexOf(given) : -1;
        if (place >= 0) flow.Party.Members[place] = received;
        else
        {
            if (given != null) flow.Boxes.Remove(given);
            if (!flow.Party.Add(received)) flow.Boxes.Store(received);
        }

        received.Friendship = received.Species.BaseFriendship;
        flow.Pokedex.RegisterCaught(received);
        // A Pokémon traded joins the receiver's region, wherever it came from (plan 18 · W4, part b)
        Crossing.Join(received, Here.Id);

        GiveIds();

        var returnTo = flow.State == GameState.Transition ? flow.After : flow.State;
        PlayEvolutions(FindEvolutions(new[] { received }, EvolutionTrigger.Trade, cancellable: false, given?.Species), returnTo);
        return true;
    }

    /// <summary>
    /// Every Pokémon of the player's, in the order ids are given (plan 07 · O2): the team, the PC box by box, the Day
    /// Care. Not the Battle Frontier's copies, nor whoever is in a battle's own copies, which keep the id of the
    /// Pokémon they stand for and are thrown away.
    /// </summary>
    internal IEnumerable<Pokemon> OwnedPokemon()
    {
        foreach (var p in flow.Party.Members) yield return p;
        foreach (var p in flow.Boxes.All) yield return p;
        for (int i = 0; i < DayCare.Places; i++)
            if (flow.DayCare[i] is { } left) yield return left;
    }

    /// <summary>
    /// Gives every Pokémon of the player's that has no id one, and a new one to a second that holds the same id as
    /// another (plan 07 · O2, <see cref="PokemonIds"/>): the trainer's number and the game's count, which goes on. Run
    /// as a game begins, loads and saves and as a Pokémon is received; a later session that keeps Pokémon by their id
    /// while the game is played (a team kept, plan 12 · Q27; a trade's offer, plan 07 · O5) runs it first.
    /// </summary>
    internal int GiveIds()
    {
        uint next = flow.PokemonIdsIssued;
        int given = PokemonIds.Give(OwnedPokemon(), TrainerNumber, ref next);
        flow.PokemonIdsIssued = next;
        // and the region it belongs to, the one the player stands in, to every one that has none (plan 18 · W4, part b)
        Crossing.Belong(OwnedPokemon(), Here.Id);
        return given;
    }

    /// <summary>After each step: a trainer looking this way notices the player and comes over to battle.</summary>
    private bool CheckTrainerSight()
    {
        // Nobody challenges a player with no Pokémon able to fight (StartWildBattle says why)
        if (!flow.Party.HasUsablePokemon) return false;
        var trainer = TrainerApproach.FindSpotter(flow.Map, flow.Player.GridX, flow.Player.GridY, height: flow.Player.HeightOn(flow.Map));
        if (trainer == null) return false;

        flow.Wandering.Settle(trainer);
        flow.TrainerApproach = new TrainerApproach(trainer, flow.Player);
        // Two trainers who see the player at once come together (APPROACH_TYPE_VS2): two against one when the
        // player has two Pokémon able to fight, or against the player and whoever travels with them
        flow.PairApproach = null;
        if (TrainerApproach.FindSpotter(flow.Map, flow.Player.GridX, flow.Player.GridY, except: trainer, height: flow.Player.HeightOn(flow.Map)) is { } second
            && (partner != null || flow.Party.Members.Count(p => !p.IsFainted) >= 2))
        {
            flow.Wandering.Settle(second);
            flow.PairApproach = new TrainerApproach(second, flow.Player, turnPlayer: false);
        }
        AudioManager.PlaySound("exclaim", PanAt(trainer.GridX));

        // Their eye theme, by their class, plays while they walk up and talk, until the battle theme cuts in
        AudioManager.Region = RegionDatabase.RegionOfMap(flow.Map.Name)?.Id;
        if (MusicDirector.EyeSong(trainer.TrainerData?.TrainerClass, AudioManager.Region, MusicLibrary.Exists) is { } eyeSong)
            AudioManager.PlayMusic(eyeSong, immediate: true);
        eyeThemePlaying = true;
        return true;
    }

    /// <summary>A trainer who came up to the player says their piece and battles: their own script, or the common one for trainers.</summary>
    private void ChallengeTrainer(NPC npc) => StartScript(FieldScripts.For(npc) ?? FieldScripts.Trainer, npc);

    /// <summary>Who someone is for <see cref="StoryState.Greet"/>: their place and their key, the same in every game.</summary>
    internal static string GreetingKey(Map map, NPC npc) => $"{npc.ScriptFile ?? map.Name}.{npc.Key ?? npc.Name}";

    private void TryInteract()
    {
        // A button of the Blackthorn Gym's slabs underfoot works before anything ahead is spoken to (plan 20 · J11, part c)
        if (TryWorkSlab()) return;
        int dx = 0, dy = 0;
        switch (flow.Player.Facing)
        {
            case Direction.Up: dy = -1; break;
            case Direction.Down: dy = 1; break;
            case Direction.Left: dx = -1; break;
            case Direction.Right: dx = 1; break;
        }

        int targetX = flow.Player.GridX + dx;
        int targetY = flow.Player.GridY + dy;

        // Check NPC interaction; reception and shop counters can be talked across
        // Someone on a bridge's deck is out of reach from the ground under it
        float standing = flow.Player.HeightOn(flow.Map);
        var npc = flow.Map.NpcIn(targetX, targetY, flow.Map.FootingAt(targetX, targetY, standing))
            // Whoever the story hangs over nothing, level with the player (Giratina in the Distortion World, plan 02 · S12)
            ?? flow.Map.NpcIn(targetX, targetY, standing);
        if (npc == null && flow.Map.IsCounter(targetX, targetY))
        {
            npc = flow.Map.GetNpcAt(targetX + dx, targetY + dy);
        }
        // A punching bag of the Veilstone Gym is kicked, not spoken to (plan 01 · M9)
        if (npc != null && TryKickBag(npc)) return;
        if (npc != null)
        {
            // Face player (a trainer goes back to looking the old way if they win); a thing stays as it is, and so does
            // someone seated or lying, who keeps the seat's facing (plan 11 · C7)
            if (npc.IsTrainer) npc.LeavePost();
            if (!npc.IsThing && flow.Map.StanceOf(npc) is Overworld.Stance.Stand or Overworld.Stance.Swim) npc.FaceTowards(flow.Player.GridX, flow.Player.GridY);
            // Someone spoken to counts toward the Hallowed Tower's stirring, once each (plan 08 · P12)
            if (!npc.IsThing && !npc.IsPokemon) flow.Story.Greet(GreetingKey(flow.Map, npc));

            // What happens next is theirs to say: their own script, or the common one for what they are
            // (a nurse, a clerk, a PC, the briefcase, a trainer, someone with lines)
            if (FieldScripts.For(npc) is { } theirs && StartScript(theirs, npc)) return;
        }

        // Below ground: the wall ahead, a trap or a Sphere in the floor, the base's PC and goods (plan 06 · R16a)
        if (TryInteractUnderground(targetX, targetY)) return;

        // A secret base's spot, and in a base its PC (plan 21 · H10)
        if (TryInteractSecretBase(targetX, targetY)) return;

        // A signboard is read, or runs the script it has
        var sign = flow.Map.GetSignboardAt(targetX, targetY);
        if (sign != null)
        {
            string read = flow.Map.SignScripts.GetValueOrDefault((targetX, targetY)) ?? FieldScripts.Sign;
            string signPlace = flow.Map.ScriptFileAt(targetX, targetY);
            if (StartScript(read, own: new[] { sign }, file: signPlace, sign: flow.Map.SignKeys.TryGetValue((targetX, targetY), out var signKey) ? (signPlace, signKey) : null)) return;
        }

        // A tile that is read when faced: an inscription, a pillar
        if (flow.Map.TileScripts.GetValueOrDefault((targetX, targetY)) is { } faced
            && StartScript(faced, file: flow.Map.ScriptFileAt(targetX, targetY))) return;

        // Something hidden where the player is looking: it is found, once
        // (a place's own HiddenItem stands in for the common one: the Battle Pyramid's finds, plan 14 · B10)
        if (FieldScripts.HiddenAt(flow.Map, targetX, targetY, flow.Story) is { } hidden
            && StartScript(flow.Scripts.In(flow.Map.ScriptFileAt(targetX, targetY), "HiddenItem") != null ? "HiddenItem" : FieldScripts.HiddenItem,
                file: flow.Map.ScriptFileAt(targetX, targetY), item: (hidden.Item, hidden.Count), flag: hidden.Flag)) return;

        // A honey tree faced from the south (HoneyTree_TryInteract, plan 06 · R13)
        if (HoneyTrees.Faced(flow.Map, flow.Player.GridX, flow.Player.GridY, flow.Player.Facing) != null && StartScript(FieldScripts.HoneyTree)) return;

        // Furniture with nothing of its own to say (plan 08 · P8): a television's programme, a bookshelf, a bin, a map
        // on a wall (the original's TileBehavior_IsTV and its kin)
        if (FieldScripts.ForFurniture(flow.Map, targetX, targetY, flow.Player.Facing) is { } furniture
            && StartScript(furniture, file: flow.Map.ScriptFileAt(targetX, targetY))) return;

        // What the tile ahead is (Field_TileBehaviorToScript): a waterfall faced from the water, a rock face along
        // its grain, and deep water at the player's feet, offered only to someone who may surf (the move and the
        // Fen Badge), as the original offers it
        var walker = new Walker(flow.Player.Mode, standing, Moves: flow.Player.Moves);
        var spot = FieldMoveRules.SpotOf(flow.Map, flow.Player.GridX, flow.Player.GridY, flow.Player.Facing, walker) with { Partner = partner != null };
        if (spot.Waterfall && spot.Surfing) StartScript(FieldScripts.Waterfall);
        // (and a whirlpool faced from the water: HeartGold's, plan 20 · J8)
        else if (spot.Whirlpool && spot.Surfing) StartScript(FieldScripts.Whirlpool);
        // (and Dive, Emerald's, plan 21 · H5: the dark water under a surfer, or the floor under a diver, offered only once
        // the region's Badge for it is had, as TrySetupDiveDownScript and TrySetupDiveEmergeScript ask FLAG_BADGE07_GET)
        else if ((spot.Dive || spot.Surface) && FieldMoveRules.Check(FieldMove.Dive, spot, flow.Story) == FieldMoveError.None)
            StartScript(spot.Surface ? FieldScripts.Surface : FieldScripts.Dive);
        else if (spot.RockFace && flow.Player.Mode == TravelMode.OnFoot) StartScript(FieldScripts.RockFace);
        else if (spot.Water && FieldHelp.CanUse(FieldMove.Surf, flow.Party, GrantedMoves)
                 && FieldMoveRules.Check(FieldMove.Surf, spot, flow.Story) == FieldMoveError.None) StartScript(FieldScripts.Water);
    }

    private void HandleWarp(Warp warp)
    {
        // A trigger waiting on the tile goes first, as the original's coordinate events come before the step off a
        // mat that takes its warp: Cheryl turns the player back at the forest's edge (plan 02 · S6)
        if (FieldScripts.TriggerAt(flow.Map, warp.SourceX, warp.SourceY, flow.Story) != null && TryStepTrigger()) return;
        // Onto the Cycling Road only a rider is let through: the gate keeper turns anyone else back
        if (warp.CyclistsOnly && flow.Player.Mode != TravelMode.Cycling)
        {
            StartScript(FieldScripts.CyclistsOnly);
            return;
        }
        NoteTheWayIn(warp);

        // A warp panel carries whoever ends a step on it: they turn where they stand, and the picture closes on that
        if (WarpPanels.Carries(flow.Map, warp))
        {
            AudioManager.PlaySound("warp");
            panelRide = (warp, flow.Player.Facing, 0f, false);
            return;
        }

        // A door has made its own sound as it opened; any other way through is the original's footsteps on stairs
        if (WorldRenderer.DoorPlace(flow.Map, warp.SourceX, warp.SourceY) == null) AudioManager.PlaySound("stairs");
        // The music fades with the screen, so a building with its own theme starts as the door opens on it
        PlayAreaMusic(MapDatabase.Get(warp.TargetMap), warp.TargetX, warp.TargetY);
        StartTransition(GameState.Overworld, () =>
        {
            flow.Map = MapDatabase.Get(warp.TargetMap);
            flow.Player.SetPosition(warp.TargetX, warp.TargetY, warp.TargetFacing);
            // Down a hole onto water: riding on it, and on with any current (plan 19 · K6, part b)
            flow.Player.ComeDownOn(flow.Map);
            ArriveOnMap();
            flow.SteppedOutOfWarp = true;
            AnnounceLocation();
            // Through a gate onto the Cycling Road, the Bicycle stays under the player until the next warp
            if (warp.CyclistsOnly) flow.Story.Set(BicycleRules.OnCyclingRoadFlag);

            // Coming out of a building, the door stands open behind the player for a moment, then shuts
            world.Life.Clear();
            var (dx, dy) = FieldMovement.Delta(warp.TargetFacing);
            int doorX = warp.TargetX - dx, doorY = warp.TargetY - dy;
            if (!flow.Map.IsIndoors && WorldRenderer.DoorPlace(flow.Map, doorX, doorY) != null)
                world.Life.LeaveDoor(flow.Map, doorX, doorY);
        });
    }

    /// <summary>
    /// A warp panel's ride (the original's warp animation; <see cref="WarpPanels"/>): the player turns on the spot,
    /// the picture closes, and on the far panel they turn on a moment and come to rest facing as they did when they
    /// stepped on. Arriving is no step: nothing on the far tile starts, and the panel there carries nobody back.
    /// </summary>
    private void RideThePanel(float dt)
    {
        var (warp, facing, clock, arrived) = panelRide!.Value;
        clock += dt;
        panelRide = (warp, facing, clock, arrived);
        if (arrived)
        {
            // The spin goes on from where the dark took it, and ends the way the traveller faced
            flow.Player.Facing = WarpPanels.Spin(facing, WarpPanels.SpinUp + clock);
            if (clock < WarpPanels.SpinDown) return;
            flow.Player.Facing = facing;
            panelRide = null;
            return;
        }
        flow.Player.Facing = WarpPanels.Spin(facing, clock);
        if (clock < WarpPanels.SpinUp) return;

        PlayAreaMusic(MapDatabase.Get(warp.TargetMap), warp.TargetX, warp.TargetY);
        var from = flow.Map;
        StartTransition(GameState.Overworld, () =>
        {
            var (x, y, way) = WarpPanels.Arrival(from, warp, facing);
            flow.Map = MapDatabase.Get(warp.TargetMap);
            flow.Player.SetPosition(x, y, way);
            ArriveOnMap();
            AnnounceLocation();
            world.Life.Clear();
            panelRide = (warp, facing, 0f, true);
        });
    }

    /// <summary>
    /// Walks the player in place of the keys while it is set: the way they are steered each frame (null stands
    /// still) and whether they run. For the story's scripted walks and for the screenshot harness.
    /// </summary>
    public (Direction? Want, bool Run)? Steering { get; set; }

    /// <summary>Puts a bubble over someone's head for a moment: a person of the map by name, or the player (null).</summary>
    public void ShowEmote(string? npcName, EmoteBubble bubble, float seconds = 0.9f)
    {
        if (npcName == null) flow.Player.ShowBubble(bubble, seconds);
        else flow.Map.NPCs.FirstOrDefault(n => n.Name == npcName)?.ShowBubble(bubble, seconds);
    }

    /// <summary>Whether lightning is whitening the field at this moment (a thunderstorm where the player stands).</summary>
    public bool LightningNow =>
        gameStarted && WeatherFx.Lightning(flow.Map.WeatherAt(flow.Player.GridX, flow.Player.GridY), world.Life.Now) > 0.6f;

    /// <summary>Sends the field's camera to look at a tile, easing there over the given time; it stays until released.</summary>
    public void PanCamera(int x, int y, float seconds) => world.PanCamera(x + 0.5f, y + 0.5f, seconds);

    /// <summary>Brings the field's camera back to the player.</summary>
    public void ReleaseCamera(float seconds) => world.ReleaseCamera(seconds);

    /// <summary>
    /// Crosses to another region, landing where arrivals from the previous region come in (plan 18 · W4, part b): every
    /// Pokémon is given the region it is leaving if it has none, the party departs by <see cref="Crossing.Depart"/>
    /// (whole, under option B) and the region is arrived in by <see cref="Crossing.Arrive"/>.
    /// </summary>
    private void TravelTo(Region region) => CrossTo(region, region.ArrivalSpot!);

    /// <summary>
    /// Goes into another region at a place of it (plan 18 · W9): the one way both the vessel (<see cref="TravelTo"/>,
    /// at the region's arrival), Fly (at a town's spawn) and a script's <c>travel to</c> (plan 20 · J13, part d: Route 40's
    /// road to the Battle Frontier and back) cross, under one fade. The region's music and name sign
    /// come with the arrival, and its levels follow the region by themselves (<see cref="LevelHere"/>).
    /// </summary>
    private void CrossTo(Region region, MapSpot spot)
    {
        GiveIds();
        Crossing.Depart(flow.Party, flow.Boxes, region, flow.Story);
        PlayAreaMusic(MapDatabase.Get(spot.Map), spot.X, spot.Y);
        StartTransition(GameState.Overworld, () =>
        {
            flow.Map = MapDatabase.Get(spot.Map);
            flow.Player.SetPosition(spot.X, spot.Y, spot.Facing);
            flow.TrainersLookOnArrival = true;
            // Nothing of the field left behind comes along (a door, a print), as on a script's warp
            world.Life.Clear();
            Crossing.Arrive(region, flow.Pokedex);
            ArriveOnMap();
            AnnounceLocation();
        });
    }

    /// <summary>
    /// The field's music now: the surf theme while the player rides a Pokémon over the water, the bicycle's while
    /// they cycle (Platinum keeps those across towns and routes), and otherwise the theme of the place they stand in.
    /// </summary>
    private void PlayFieldMusic()
    {
        eyeThemePlaying = false;
        musicTravel = flow.Player.Mode;
        AudioManager.Region = RegionDatabase.RegionOfMap(flow.Map.Name)?.Id;
        // The Pokégear's radio plays over the field's theme while it is on (plan 20 · J3)
        if (RadioDial.SongFile(radioSong) is { } radio)
        {
            AudioManager.LowHp = false;
            AudioManager.PlayMusic(radio);
            return;
        }
        var ride = flow.Player.Mode switch { TravelMode.Surfing => MusicRole.Surf, TravelMode.Cycling => MusicRole.Bicycle, _ => (MusicRole?)null };
        if (ride != null && MusicDirector.Resolve(ride.Value, AudioManager.Region, MusicLibrary.Exists) != null) AudioManager.PlayMusic(ride.Value);
        else PlayAreaMusic(flow.Map, flow.Player.GridX, flow.Player.GridY);
    }

    /// <summary>
    /// Plays the theme of a place on a map (its night arrangement at night) with its region's versions of the
    /// shared themes. A place that names no theme keeps whatever is playing; the same theme carries on without a
    /// restart.
    /// </summary>
    private static void PlayAreaMusic(Map map, int x, int y)
    {
        AudioManager.LowHp = false;
        AudioManager.Region = RegionDatabase.RegionOfMap(map.Name)?.Id;
        string track = map.BgmTrackAt(x, y);
        if (!string.IsNullOrEmpty(track)) AudioManager.PlayMusic(track);
    }

    /// <summary>
    /// Shows the place's name on arriving outdoors, as the games do on entering a town or route or stepping out
    /// of a building. Rooms have no sign.
    /// </summary>
    private void AnnounceLocation()
    {
        flow.CurrentArea = flow.Map.AreaAt(flow.Player.GridX, flow.Player.GridY);
        flow.CurrentZone = flow.Map.ZoneAt(flow.Player.GridX, flow.Player.GridY);
        NoteArrival();
        if (flow.Map.IsIndoors) flow.LocationSign.Hide();
        else flow.LocationSign.Show(flow.Map.ShownNameAt(flow.Player.GridX, flow.Player.GridY), SeasonHere);
    }

    private void HandleStartMenuChoice(StartMenuChoice choice)
    {
        switch (choice)
        {
            case StartMenuChoice.None:
                return;
            case StartMenuChoice.Pokedex:
                flow.State = GameState.PokedexMenu;
                flow.Pokedex.Enter(Here);
                flow.PokedexScreen.FormsKnown = flow.Story.Has(StoryState.FormDetectionFlag);
                flow.PokedexScreen.Random = Randomizer.For(flow.Run);
        flow.PokedexScreen.Open(flow.Pokedex, (IsHeadless ? null : world.Portrait(PlayerIdentity.Character)), Here);
                break;
            case StartMenuChoice.Pokemon:
                flow.State = GameState.PartyMenu;
                flow.PartyScreen.Open();
                flow.PartyScreen.Region = Here.Id;
                // The summary's condition page, once the Contest Hall has been visited (plan 06 · R14c)
                flow.PartyScreen.ShowCondition = flow.Story.Has(ContestHallVisitedFlag);
                break;
            case StartMenuChoice.Boxes:
                OpenBoxesFromMenu();
                return;
            case StartMenuChoice.Bag:
                flow.State = GameState.BagMenu;
                flow.BagScreen.Registered = flow.RegisteredItem;
                flow.BagScreen.Aids = flow.EncounterAids;
                flow.BagScreen.PlayerName = PlayerIdentity.Name;
                flow.BagScreen.NoRevives = flow.Run.Nuzlocke;
                flow.BagScreen.Open();
                break;
            case StartMenuChoice.Pokegear:
                OpenPokegear();
                break;
            case StartMenuChoice.PokeNav:
                OpenPokeNav();
                break;
            case StartMenuChoice.Trainer:
                flow.State = GameState.TrainerCard;
                flow.TrainerCardScreen.Open((IsHeadless ? null : world.Portrait(PlayerIdentity.Character)), TrainerCardNow().RegionPages.Count, TrainerCardNow().Run != null);
                break;
            case StartMenuChoice.Options:
                flow.OptionsReturn = GameState.Overworld;
                flow.State = GameState.Options;
                flow.OptionsScreen.Open();
                break;
            case StartMenuChoice.Save:
                // A run that has ended is saved no more (plan 13 · V4): the menu says so and stays
                if (flow.RunState.Ended)
                {
                    ShowNotification(Text.Of("run.ended.noSave"));
                    AudioManager.PlaySound("error");
                    return;
                }
                // The save as it will be, with the question under it
                flow.State = GameState.SaveMenu;
                flow.SaveScreen.Open(BuildSave(), flow.SaveSlot);
                flow.SaveScreen.Objective = NextObjectiveLine;
                break;
            case StartMenuChoice.SaveAndQuit:
                SaveCurrentGame();
                QuitRequested = true;
                return;
            case StartMenuChoice.Quit:
                QuitRequested = true;
                return;
        }

        // A full screen takes over: the menu is gone when the player comes back
        flow.StartMenu.Hide();
    }

    /// <summary>The trainers who were waiting for a rematch give up (the original turns them to look about).</summary>
    private void StopRematches(Direction facing)
    {
        foreach (var npc in flow.Map.Everyone.Where(n => n.ReadyForRematch))
        {
            npc.ReadyForRematch = false;
            npc.Facing = facing;
        }
    }

    /// <summary>The Journal's line for a visit to a Mart: bought and sold, bought plenty, sold plenty, bought, sold, or nothing.</summary>
    public static JournalEventKind? ShopLine(int purchases, int unitsSold) =>
        purchases > 0 && unitsSold > 0 ? JournalEventKind.BusinessAtMart
        : purchases > 1 ? JournalEventKind.LotsOfShopping
        : unitsSold > 1 ? JournalEventKind.SoldALot
        : purchases > 0 ? JournalEventKind.ShoppedAtMart
        : unitsSold > 0 ? JournalEventKind.SoldALittle
        : null;

    /// <summary>
    /// The regions the Trainer Card has a word for (plan 18 · W2), in the journey's order: the one the player is in, and
    /// every other they have been in (its Pokédex entered, a badge won there or its Hall of Fame entered), each with its
    /// badges.
    /// </summary>
    private List<CardRegion> CardRegions()
    {
        var here = Here;
        return RegionDatabase.All
            .Where(r => r == here || flow.Story.BadgeMask(r.Id) != 0 || flow.Story.IsRegionComplete(r.Id)
                || (r.Dex is { } dex && flow.Pokedex.Entered.Contains(dex)))
            .Select(r => new CardRegion(r.Id, r.Generation, r.BadgeNames, flow.Story.BadgeMask(r.Id), flow.Story.IsRegionComplete(r.Id))
            {
                Pokedex = r.Dex is { } dex && flow.Pokedex.Regional.Contains(dex)
                    ? (flow.Pokedex.SeenIn(dex), flow.Pokedex.CaughtIn(dex), Pokedex.Entries(dex).Count) : null
            })
            .ToList();
    }

    /// <summary>What the Trainer Card shows now: who the player is, what they have, and its colour by what they have done.</summary>
    private TrainerCardInfo TrainerCardNow()
    {
        bool famous = flow.HallOfFame.Total > 0 || RegionDatabase.All.Any(r => flow.Story.Has(r.StoryCompleteFlag));
        int stars = TrainerCardRules.Level(famous, TrainerCardRules.NationalCaught(flow.Pokedex.CaughtSpecies), TowerBestStreak, IsContestMaster, 0);
        var regions = CardRegions();
        return new TrainerCardInfo(playerName, flow.TrainerId, flow.Money, flow.Pokedex.SeenCount, flow.Pokedex.CaughtCount, flow.PlayTime,
            regions.First(r => r.Id == Here.Id), flow.AdventureStarted)
        {
            Regions = regions,
            Run = flow.Run.IsNormal ? null : flow.Run,
            Graveyard = flow.RunState.Graveyard.Select(f => new CardFallen(
                f.Pokemon.Nickname.Length > 0 ? f.Pokemon.Nickname : LocalNames.Species(f.Pokemon.SpeciesName),
                LocalNames.Species(f.Pokemon.SpeciesName), f.Pokemon.Level,
                PlayerIdentity.Fill(LocalNames.PlaceShown(f.PlaceKey, f.Place)))).ToList(),
            RunEnded = flow.RunState.Ended,
            Score = flow.TrainerScore,
            Stars = stars,
            NextObjective = NextObjectiveLine,
            Colour = TrainerCardRules.Colour(flow.Story.Has(StoryState.PokedexFlag), stars),
            HallOfFameDebut = flow.HallOfFame.Debut,
            ContestsEntered = flow.ContestRecords.Entered,
            ContestsWon = flow.ContestRecords.Won
        };
    }

    /// <summary>
    /// What a battle writes in the Journal (encounter.c, UpdateJournal): a Gym Leader, one of the Elite Four or the
    /// Champion beaten has a line of their own; any other trainer is the day's trainer; a Pokémon caught is the day's
    /// Pokémon, and so is one knocked out once five have been in one place.
    /// </summary>
    private void NoteBattle()
    {
        if (flow.Battle == null) return;
        string place = PlaceName();
        if (flow.Battle.Result == BattleResult.PlayerVictory && flow.Battle.IsTrainerBattle)
        {
            var foe = flow.Battle.Trainers[0];
            if (foe.TrainerClass == "Leader") flow.Journal.Tell(new JournalEvent(JournalEventKind.BeatGymLeader, foe.Name));
            else if (foe.TrainerClass == "Elite Four") flow.Journal.Tell(new JournalEvent(JournalEventKind.BeatEliteFourMember, foe.Name));
            else if (foe.TrainerClass == "Champion") flow.Journal.Tell(new JournalEvent(JournalEventKind.BeatChampion, foe.Name));
            else flow.Journal.BeatTrainer(PlayerIdentity.Fill(foe.FullTitle), place);
        }
        else if (flow.Battle.Result == BattleResult.EnemyCaught && flow.Battle.Caught is { } caught)
        {
            flow.Journal.Caught(caught.Species.Name, place);
            flow.Poketch.Remember(caught);
        }
        else if (flow.Battle.Result == BattleResult.PlayerVictory && flow.Battle.EnemyPokemon is { } wild) flow.Journal.Defeated(wild.Species.Name, place);
    }

    /// <summary>
    /// What a battle adds to the Trainer Card's score (the original's <c>GameRecords_IncrementTrainerScore</c>): a won
    /// trainer battle, a wild Pokémon knocked out, or one caught (more for one of the National Pokédex than of
    /// Sinnoh's, and twenty for a species never caught before).
    /// </summary>
    private void ScoreBattle()
    {
        if (flow.Battle == null || flow.Battle.Kind == BattleKind.CatchingLesson) return;
        NoteBattle();
        int points = flow.Battle.Result switch
        {
            BattleResult.PlayerVictory => flow.Battle.IsTrainerBattle ? TrainerScore.WonTrainerBattle : TrainerScore.WonWildBattle,
            BattleResult.EnemyCaught when flow.Battle.Caught is { } caught =>
                // Of the regional Pokédex of the region the player is in (plan 18 · W2), or not
                (Here.Dex is { } dex && caught.Species.DexNumbers.ContainsKey(dex) ? TrainerScore.CaughtRegional : TrainerScore.CaughtNational)
                + (flow.Battle.CaughtNewSpecies ? TrainerScore.CaughtNewSpecies : 0),
            _ => 0
        };
        flow.TrainerScore = TrainerScore.Add(flow.TrainerScore, points);
    }

    /// <summary>
    /// Whiting out (plan 06 · R10; the original's <c>FieldTask_BlackOutFromBattle</c>): the player comes round in
    /// front of the nurse of the Pokémon Center they last went into, or beside Mom at home before any, and the one
    /// who looks after them heals the team (<c>common.BlackOutCenter</c>, <c>common.BlackOutHome</c>). Giratina takes
    /// the form its item gives it, the Bicycle and the water are left behind, and Dig and an Escape Rope lead out to
    /// the town. The money was taken by the battle (<see cref="BattleEngine.MoneyLost"/>).
    /// </summary>
    private void WhiteOut()
    {
        // Whoever travelled with the player stays behind (FieldSystem_ClearPartnerTrainer)
        SetPartner(null, null);
        foreach (var p in flow.Party.Members) FormRules.ByHeldItem(p);
        var (room, home) = WhiteOutRoom();
        flow.Map = MapDatabase.Get(room);
        var healer = flow.Map.Everyone.FirstOrDefault(n => n.IsHealingNurse);
        // In front of the nurse, the counter between them; at home, beside Mom
        // With nobody there who heals, inside the room's door (a Kanto house's own room, plan 19 · K1)
        var (x, y, facing) = healer == null
            ? SaveData.ArrivalIn(flow.Map, MapDatabase.TryGet) is { } inside ? (inside.X, inside.Y, inside.Facing) : (4, 5, Direction.Down)
            : home ? HomeSpot(healer) : (healer.GridX, healer.GridY + 2, Direction.Up);
        flow.Player.SetMode(TravelMode.OnFoot);
        flow.Player.SetPosition(x, y, facing);
        if (SpawnLocations.Get(Here.Id, flow.Story.Var(SpawnLocations.VariableOf(Here.Id))) is { } town) flow.ExitSpot = new MapSpot(Here.OverworldMap ?? "Sinnoh", town.X, town.Y, Direction.Down);
        // A scene the battle was lost in left its camera, its bars and its pace behind with the place (plan 09 · L4)
        world.Camera.Reset();
        flow.CameraSent = false;
        ArriveOnMap();
        AnnounceLocation();
        if (healer == null || !StartScript(home ? FieldScripts.BlackOutHome : FieldScripts.BlackOutCenter, healer)) flow.Party.HealAll();
    }

    /// <summary>
    /// Where a white-out wakes the player at home: on the tile east of Mom, turned to her. In the player's house that
    /// is the cushion beside hers before the television, 8, 8, the tile the original's table of spawn locations gives
    /// (<c>sSpawnLocations</c>' first row).
    /// </summary>
    internal static (int X, int Y, Direction Facing) HomeSpot(NPC healer) => (healer.GridX + 1, healer.GridY, Direction.Left);

    /// <summary>The room the player comes round in: in Sinnoh the last Pokémon Center's (<see cref="SpawnLocations.Respawn"/>), elsewhere the region's home.</summary>
    private (string Room, bool Home) WhiteOutRoom()
    {
        var region = RegionDatabase.RegionOfMap(flow.Map.Name);
        if (region?.Id == RegionDatabase.Sinnoh)
        {
            var spawn = SpawnLocations.Respawn(flow.Story);
            if (MapDatabase.MapNames.Contains(spawn.Room)) return (spawn.Room, spawn.Room == "PlayerHouse");
            return ("PlayerHouse", true);
        }
        // Elsewhere the region's first spawn, its home (Kanto's: the player's house in Pallet Town, plan 19 · K1)
        if (region != null && SpawnLocations.Of(region) is { Count: > 0 } spawns && MapDatabase.MapNames.Contains(spawns[0].Room))
            return (spawns[0].Room, true);
        string house = region?.Maps.FirstOrDefault(m => m.EndsWith("PlayerHouse", StringComparison.Ordinal)) ?? "PlayerHouse";
        return (house, true);
    }

    private void StartTransition(GameState nextState, Action? onMidpoint = null, TransitionKind kind = TransitionKind.Fade)
    {
        flow.StartTransition(nextState, onMidpoint, kind);
        // (A band left over from a transition that was never played out goes; whoever starts one begins it after this.
        // So does a lens a way into a battle held, plan 09 · L14)
        band.End();
        world.Camera.LetGo();
    }

    /// <summary>
    /// What a way into a battle does to the field's camera (plan 09 · L14): while the legendary's or the mythical's
    /// closes, the rig holds the lens <see cref="SceneTransition.LensAt"/> gives for the moment; at any other time it
    /// holds none, so the field after the battle is looked at exactly as before. The transition's own timer, which
    /// is the clock's time, never the field's: slow motion slows no way in.
    /// </summary>
    internal static void DriveTheLens(CameraRig rig, TransitionKind kind, bool closing, float seconds)
    {
        if (closing && SceneTransition.MovesTheCamera(kind))
        {
            var lens = SceneTransition.LensAt(kind, seconds);
            rig.Hold(lens.Zoom, lens.Step, lens.Widest);
        }
        else if (rig.Holding) rig.LetGo();
    }

    private void UpdateTransition(float dt)
    {
        // The band's show is over once the picture is shut: its portraits' targets are freed; and the camera a
        // legendary's or a mythical's way moved is let go under its white, before the midpoint's callback runs
        switch (flow.Advance(dt, () =>
                {
                    band.End();
                    DriveTheLens(world.Camera, flow.Kind, closing: false, 0f);
                    // A season the calendar has turned to is taken behind the dark (Unova's, plan 22 · U3)
                    TurnTheSeason();
                }))
        {
            case TransitionStep.Closing:
                DriveTheLens(world.Camera, flow.Kind, closing: true, flow.Clock);
                break;
            case TransitionStep.Opened:
                flow.TrainersLookOnArrival = flow.State == GameState.Overworld;
                break;
        }
    }

    public void ShowNotification(string message)
    {
        flow.Toast.Show(message);
    }

    public void Draw()
    {
        if (IsHeadless) throw new InvalidOperationException("A game with no window draws nothing (InitializeHeadless).");
        FrameProfiler.LapCpu(FrameSection.Update);
        renderContext.OutputWidth = Raylib.GetScreenWidth();
        // Under the close button's question, what was on screen when it was asked (plan 12 · Q1)
        GameState shown = flow.Shown;
        // During a fade, show the screen being left while fading out and the new one while fading in
        GameState scene = flow.Scene;
        bool showWorld = scene is GameState.Overworld or GameState.Dialogue or GameState.SaveMenu or GameState.Phrases or GameState.RivalNaming or GameState.Nickname;
        bool showBattle = scene == GameState.Battle && flow.Battle != null;
        bool showTitle = scene == GameState.Title;

        // Menu sprites asked for since the last frame are loaded or baked first, offscreen like the 3D scenes
        PokemonSprites.Service(renderContext);
        // ... and so are a band's portraits (plan 09 · L15): each once, in the first frame of the closing that finds
        // its rig ready, and before the scene, which sets the uniforms it needs after them
        if (shown == GameState.Transition && flow.Closing) band.Render(renderContext);

        // The first run's measuring, a frame of it, and the share of the preset this frame's scene is drawn at (plan 16 · T23)
        MeasureAFrame();
        ChooseTheResolution();

        // The 3D scenes render into their own targets first (texture modes can't nest)
        if (showWorld)
        {
            world.Render(flow.Map, flow.Player);
        }
        else if (showBattle)
        {
            battleRenderer.Render(flow.Battle!);
        }
        else if (showTitle && !MeasuringQuality)
        {
            flow.TitleScreen.Render(renderContext, world, titleScene);
        }
        else if (scene == GameState.Evolution)
        {
            flow.EvolutionScreen.Render(renderContext);
        }
        else if (scene == GameState.Hatch)
        {
            flow.HatchScreen.Render(renderContext);
        }
        else if (scene == GameState.Intro)
        {
            flow.IntroScreen.Render(renderContext);
        }
        else if (scene == GameState.Wardrobe)
        {
            flow.WardrobeScreen.Render(renderContext);
        }
        else if (scene == GameState.HallOfFameCeremony)
        {
            flow.Ceremony.Render(renderContext);
        }
        else if (scene == GameState.Credits)
        {
            flow.Credits.Render(renderContext);
        }
        // (The field and the battle have told the profiler of their own passes; this takes whatever else was rendered)
        FrameProfiler.Lap(FrameSection.Scene);

        // Render scene to native 1920x1080 Full HD buffer
        Raylib.BeginTextureMode(virtualScreen);
        Raylib.ClearBackground(Color.Black);
        // The picture stays opaque whatever is drawn on it: raylib's usual blending blends alpha like a colour, so
        // anything translucent lowered the alpha of what it covered, and the window, which shows the picture over
        // black, showed every translucent panel darker than it is. The clear is the only thing that writes alpha.
        Rlgl.ColorMask(true, true, true, false);
        Raylib.BeginMode2D(new Camera2D { Zoom = RenderScale });

        switch (shown)
        {
            case GameState.Title:
                flow.TitleScreen.Draw(VirtualWidth, VirtualHeight, renderContext, world);
                break;
            case GameState.Overworld:
            case GameState.Dialogue:
                world.DrawToScreen(VirtualWidth, VirtualHeight);
                DrawPoisonFlash();
                DrawTrap();
                // A script's black screen covers the field and leaves the text over it
                DrawScriptFade();
                // ... and so do a scene's cinema bars (plan 09 · L4)
                DrawLetterbox();
                DrawCutIn();
                // The Pokétch shows over the field while nothing else is on the screen
                if (shown == GameState.Overworld && !flow.StartMenu.IsActive && !ScriptRunning) flow.PoketchView.Draw(VirtualWidth, VirtualHeight, flow.Poketch, PoketchNow());
                if (flow.Safari.Active) ModernUi.SafariCount(VirtualWidth, flow.Safari.Balls, flow.Safari.Steps, flow.Safari.Endless);
                DrawBugContestCount();
                DrawParkCount();
                DrawBattlePoints();
                DrawCastlePoints();
                flow.Dialogue.Draw(VirtualWidth, VirtualHeight);
                DrawChoice();
                // A scene being skipped: its dark over all of that, and the hold's ring while the button is down (plan 12 · Q20)
                DrawSkip();
                flow.LocationSign.Draw();
                flow.StartMenu.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.Battle:
                DrawBattle();
                break;
            case GameState.PartyMenu:
                flow.PartyScreen.Draw(VirtualWidth, VirtualHeight, flow.Party);
                break;
            case GameState.FlyMap:
                flow.TownMapScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.BagMenu:
                flow.BagScreen.Draw(VirtualWidth, VirtualHeight, flow.Bag, flow.Party, EvolutionContextNow());
                break;
            case GameState.Evolution:
                flow.EvolutionScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.Hatch:
                flow.HatchScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.PokedexMenu:
                flow.PokedexScreen.Draw(VirtualWidth, VirtualHeight, flow.Pokedex);
                break;
            case GameState.TrainerCard:
                flow.TrainerCardScreen.Draw(VirtualWidth, VirtualHeight, TrainerCardNow());
                break;
            case GameState.SaveMenu:
                world.DrawToScreen(VirtualWidth, VirtualHeight);
                flow.SaveScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.Intro:
                flow.IntroScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.StarterSelect:
                flow.StarterSelectScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.Shop:
                flow.ShopScreen.Draw(VirtualWidth, VirtualHeight, flow.Money, flow.Bag);
                break;
            case GameState.Wardrobe:
                flow.WardrobeScreen.Draw(VirtualWidth, VirtualHeight, flow.Money);
                break;
            case GameState.HallOfFame:
                flow.HallOfFameScreen.Draw(VirtualWidth, VirtualHeight, flow.HallOfFame);
                break;
            case GameState.ReplayList:
                flow.ReplayListScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.Migrate:
                flow.MigrateScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.Jukebox:
                flow.JukeboxScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.Appeals:
                flow.AppealScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.PokeblockCase:
                flow.PokeblockCaseScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.Blender:
                flow.BlenderScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.Bracket:
                flow.BracketScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.Tournament:
                flow.TournamentScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.FirstStart:
                flow.FirstStartScreen.Draw(VirtualWidth, VirtualHeight, Settings);
                break;
            case GameState.Journal:
                flow.JournalScreen.Draw(VirtualWidth, VirtualHeight, flow.Journal);
                break;
            case GameState.PoffinCooking:
                flow.PoffinCookingScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.PoffinCase:
                flow.PoffinCaseScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.Pokegear:
                flow.PokegearScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.PokeNav:
                flow.PokeNavScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.AlphPuzzle:
                flow.AlphPuzzleScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.BugContestResults:
                flow.BugContestResultsScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.Athlon:
                flow.AthlonScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.Phrases:
                // Over the field, which stays under it as under a question (plan 08 · P9)
                world.DrawToScreen(VirtualWidth, VirtualHeight);
                flow.PhraseScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.RivalNaming:
                // Over the field, as the phrase picker is
                world.DrawToScreen(VirtualWidth, VirtualHeight);
                flow.RivalNamingScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.Nickname:
                world.DrawToScreen(VirtualWidth, VirtualHeight);
                flow.NicknameScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.SuperContest:
                flow.SuperContestScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.SealCase:
                flow.SealCaseScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.SealShop:
                flow.SealShopScreen.Draw(VirtualWidth, VirtualHeight, flow.Money);
                break;
            case GameState.FashionCase:
                flow.FashionCaseScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.ApricornBox:
                flow.ApricornBoxScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.Mining:
                flow.MiningScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.Underground:
                flow.UndergroundScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.Decoration:
                flow.DecorationScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.FrontierRecords:
                flow.FrontierRecordsScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.FactoryRentals:
                flow.FactoryScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.HallBoard:
                flow.HallBoardScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.CastleCounter:
                flow.CastleScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.ArcadeBoard:
                flow.ArcadeBoardScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.SlotMachine:
                flow.SlotMachineScreen.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.HallOfFameCeremony:
                flow.Ceremony.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.Credits:
                flow.Credits.Draw(VirtualWidth, VirtualHeight);
                break;
            case GameState.PCStorage:
                flow.PCScreen.Draw(VirtualWidth, VirtualHeight, flow.Party, flow.Boxes);
                break;
            case GameState.Options:
                flow.OptionsScreen.Draw(VirtualWidth, VirtualHeight, Settings);
                break;
            case GameState.Transition:
                if (showBattle) DrawBattle();
                else if (showWorld)
                {
                    // On water the field wavers under the way into a battle as it closes (plan 09 · L13)
                    world.DrawToScreen(VirtualWidth, VirtualHeight, SceneTransition.Ripple(flow.Kind, flow.Closing, flow.Clock));
                    DrawScriptFade();
                    DrawLetterbox();
                }
                else if (scene == GameState.Evolution) flow.EvolutionScreen.Draw(VirtualWidth, VirtualHeight);
                else if (scene == GameState.Hatch) flow.HatchScreen.Draw(VirtualWidth, VirtualHeight);
                else if (scene == GameState.BagMenu) flow.BagScreen.Draw(VirtualWidth, VirtualHeight, flow.Bag, flow.Party, EvolutionContextNow());
                else if (scene == GameState.ReplayList) flow.ReplayListScreen.Draw(VirtualWidth, VirtualHeight);
                else if (scene == GameState.SuperContest) flow.SuperContestScreen.Draw(VirtualWidth, VirtualHeight);
                else if (scene == GameState.Appeals) flow.AppealScreen.Draw(VirtualWidth, VirtualHeight);
                else if (scene == GameState.HallOfFameCeremony) flow.Ceremony.Draw(VirtualWidth, VirtualHeight);
                else if (scene == GameState.Credits) flow.Credits.Draw(VirtualWidth, VirtualHeight);
                break;
        }

        flow.Toast.Draw(VirtualWidth);
        DrawStandingNotice();
        DrawAutosaveDisc();

        // The scene closing, or the next one opening; a cave's ring closes on the player, wherever the field shows them,
        // and a band is drawn with whose it is and the portraits rendered for it so far
        if (shown == GameState.Transition)
            SceneTransition.Draw(flow.Kind, flow.Closing, flow.Clock, VirtualWidth, VirtualHeight,
                flow.Closing && showWorld ? world.PlayerOnPicture * new Vector2(VirtualWidth, VirtualHeight) : null,
                flow.Closing ? band.Picture : null);

        // The close button's question over all of it, sliding away once answered
        closePrompt.Draw(VirtualWidth, VirtualHeight);
        // The developer's console and frame time over all of it (plan 16 · T6)
        DrawDev();

        Raylib.EndMode2D();
        // (Ending the 2D mode drew what was batched; every other target is written whole)
        Rlgl.ColorMask(true, true, true, true);
        Raylib.EndTextureMode();
        TakeDevShot();
        TakeReportScreen();
        FrameProfiler.Lap(FrameSection.Interface);
        SampleTheFrame();

        // Fit the 4K screen into the window (at 1080p this halves it, which also anti-aliases it)
        int screenW = Raylib.GetScreenWidth();
        int screenH = Raylib.GetScreenHeight();

        float scale = MathF.Min((float)screenW / VirtualWidth, (float)screenH / VirtualHeight);
        int destW = (int)(VirtualWidth * scale);
        int destH = (int)(VirtualHeight * scale);
        int destX = (screenW - destW) / 2;
        int destY = (screenH - destH) / 2;

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);

        Rectangle src = new(0, 0, virtualScreen.Texture.Width, -virtualScreen.Texture.Height);
        Rectangle dst = new(destX, destY, destW, destH);
        Raylib.DrawTexturePro(virtualScreen.Texture, src, dst, Vector2.Zero, 0f, Color.White);
        FrameProfiler.Lap(FrameSection.Present);

        Raylib.EndDrawing();
        FrameProfiler.Lap(FrameSection.Swap);
        FrameProfiler.EndFrame();
        BeginTheNextFrame();
        // The first frame since a place was come to has been drawn: its arrival is timed to here (plan 16 · T3)
        EndTheArrival();
    }

    /// <summary>The 3D battle field and Pokémon, then the HUD and menus on top.</summary>
    private void DrawBattle()
    {
        if (flow.Battle == null) return;
        battleRenderer.DrawField(flow.Battle, VirtualWidth, VirtualHeight);
        flow.Battle.Draw(VirtualWidth, VirtualHeight);
    }

    public void Close()
    {
        if (IsHeadless) return;
        band.End();
        renderContext.Unload();
        RegionMapPictures.Unload();
        Gpu.Unload(virtualScreen);
        AudioManager.Close();
    }
}
