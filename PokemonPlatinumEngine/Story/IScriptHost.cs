using System.Collections.Generic;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Story;

/// <summary>How a battle a script started came out.</summary>
public enum BattleOutcome { None, Won, Lost, Fled, Caught }

/// <summary>Who shows the catching lesson when it isn't the assistant, and the Pokémon they show it with (plan 21 · H2).</summary>
public sealed record LessonCatcher(string Name, Pokemon Own);

/// <summary>The screens a script can open and wait for.</summary>
public enum ScriptScreen { Starter, Shop, Pc, Travel, ChoosePokemon, HallOfFame, Wardrobe, ChooseItem, PoffinCooking, BallCapsules, SealShop, ContestPhoto, Ceremony, Credits, AlphPuzzle, TownMap, Phrase, NameRival, Crossing, BugContestResults, Athlon, Apricorns, ContestPainting, Bracket, Tournament, Jobs, PhotoAlbum, GlobalLink, Crafting, Training, GiftBox, TradeBoard, MissionBoard, DressingRoom, StageShow, PhotoBook, PhotoStudio, PhotoTour, TourReport, Delve, DelveBoard }

/// <summary>
/// What a script needs of the game it runs in. The <see cref="ScriptRunner"/> decides what happens and in what
/// order; the host is where it happens: the field with its people, the text box, a battle, a screen. The game's own
/// host is part of <see cref="GameEngine"/>; <see cref="HeadlessScriptHost"/> is one with no screen, where
/// everything happens at once, for tests. A person is an <see cref="NPC"/>, and null is the player.
/// </summary>
public interface IScriptHost
{
    // ------------------------------------------------------------------ what conditions read and commands change

    StoryState Story { get; }
    Party Party { get; }
    Inventory Bag { get; }

    /// <summary>
    /// Whether the player has a Pokémon able to fight. A battle is never started without one (<see cref="ScriptRunner"/>):
    /// the story gives the first Pokémon before any battle, and a battle with none ended the game.
    /// </summary>
    bool CanBattle => Party.HasUsablePokemon;

    /// <summary>
    /// The region the player is in, by its id (plan 18 · W2): whose badges <c>givebadge</c>, <c>if badge</c>,
    /// <c>if badges</c> and <c>BADGE_COUNT</c> mean. Sinnoh's unless a host says otherwise.
    /// </summary>
    string Region => RegionDatabase.Sinnoh;

    /// <summary>
    /// Whether the game is played by the modern rules (plan 15 · E3): the script's <c>modern</c>, which the PC asks before
    /// it offers Poké Jobs. The rules in force, unless a host says otherwise (a test's).
    /// </summary>
    bool Modern => Ruleset.Current.Preset == RulesPreset.Modern;

    /// <summary>
    /// The region the game began in, by its id (plan 18 · W5): the built-in variable <c>CROSSED_IN</c> is 1 where the
    /// script plays in another, which a region's opening asks before it names the region's rival with <c>namerival</c>.
    /// The region the player is in unless a host says otherwise.
    /// </summary>
    string BeganIn => Region;

    /// <summary>
    /// Whether the player's options let the game say what to do next (plan 12 · Q14): <c>if objective</c> is false when
    /// they do not. On unless a host says otherwise.
    /// </summary>
    bool ShowsObjectives => true;

    /// <summary>
    /// The ways to play chosen at NEW GAME (plan 13 · V5): what <c>if rule …</c> asks. Normal, with nothing on, unless a host
    /// says otherwise; the game's host gives the run in force, the headless host the one a test sets.
    /// </summary>
    RunRules Rules => RunRules.Normal;

    /// <summary>The player's Pokétch: whether they have it and its apps (plan 02 · S2).</summary>
    Poketch Poketch { get; }

    /// <summary>The Great Marsh's Safari Game, while one is under way (plan 01 · M7).</summary>
    SafariGame Safari { get; }

    /// <summary>The National Park's Bug-Catching Contest, while one is under way (plan 15 · E5).</summary>
    BugContest BugContest { get; }

    /// <summary>The Pokéathlon's Athlete Points and records (plan 15 · E6).</summary>
    AthlonRecords Athlon { get; }

    /// <summary>
    /// A Pokémon of the player's own that the team has no room for goes to the PC as it is: the Bug-Catching Contest's
    /// catch as the contest ends (plan 15 · E5), met where it was met and never made again.
    /// </summary>
    void PutInBox(Pokemon pokemon);

    /// <summary>
    /// A level of the game's own as the player meets it here: the region crossed into maps it (plan 18 · W4), and
    /// anywhere else it is as it is. The Bug-Catching Contest judges a catch against its table's highest so.
    /// </summary>
    int LevelHere(int level) => level;

    /// <summary>How many species of the Sinnoh Pokédex the player has seen (the original's <c>GetLocalDexSeenCount</c>).</summary>
    int SeenInSinnoh { get; }

    /// <summary>How many species the player has caught, every one the Pokédex has (plan 19 · K3, <c>CAUGHT</c>: the original's <c>GetPokedexCount</c>, which Oak's aides ask).</summary>
    int Caught => 0;

    /// <summary>
    /// The player's Pokédex, which the charms a designer hands over are read from (plan 40 · GE5, <c>charm due</c>,
    /// <see cref="Charms.Due"/>); null for a host that has none, where nothing is due.
    /// </summary>
    Pokedex? Pokedex => null;

    /// <summary>
    /// The gift box (plan 40 · GE7, <c>gift</c>): what it has handed over; null for a host that has none, where nothing waits.
    /// Its screen is <see cref="ScriptScreen.GiftBox"/>: <see cref="Answer"/> the gift chosen of those due (from 1),
    /// <see cref="GiftBoxAnswers.Words"/> for TELL WORDS, 0 backed out of.
    /// </summary>
    GiftBox? Gifts => null;

    /// <summary>
    /// The stage show's records (plan 42 · PS1, PS2, <c>stageshow</c>): the trunk, the dressings, the shows and the fans'
    /// gifts; null for a host that has none, where no trunk was given. Its screens are <see cref="ScriptScreen.DressingRoom"/>
    /// (<see cref="Answer"/> 1 when a dressing was kept) and <see cref="ScriptScreen.StageShow"/> (the place, 1 to 4, or 0).
    /// </summary>
    Models.StageShow.ShowRecords? StageShow => null;

    /// <summary>
    /// The film studio's records (plan 42 · PS4, <c>studio</c>): each film's takes and best, the studio's rank; null for a
    /// host that has none, where nothing can be shot.
    /// </summary>
    Models.Studio.StudioRecords? Studio => null;

    /// <summary>
    /// A take of a film shot on its own battle (plan 42 · PS4): the film's cast lent, its foe, its rolls and its beats; no
    /// EXP, money or Pokédex, and nothing of the player's own team. The take is kept in <see cref="Studio"/> (its
    /// <c>LastTake</c>) once the battle is over; the script waits.
    /// </summary>
    void StudioShoot(Models.Studio.StudioFilm film) { }

    /// <summary>
    /// A film's best take shown at the premiere (plan 42 · PS4, part b): its Vs. Recorder file watched, the take's lines
    /// said again, the crowd and the box office, which goes into <see cref="Studio"/>'s <c>LastBoxOffice</c>. False,
    /// with nothing shown, where there is no take to show; the script waits otherwise.
    /// </summary>
    bool StudioPremiere(Models.Studio.StudioFilm film) => false;

    /// <summary>What the gifts' terms read of the player now (the day, the Halls of Fame, the counts, the run): nothing held unless a host says.</summary>
    GiftFacts GiftFacts => new();

    /// <summary>
    /// The trade board and blind trades (plan 40 · GE8, <c>trade board</c>, <c>trade blind</c>); null for a host that has none.
    /// Its screen is <see cref="ScriptScreen.TradeBoard"/> (the counter the region's id): <see cref="Answer"/> the request
    /// chosen, 1 to 3, 4 for a blind trade, 0 backed out of.
    /// </summary>
    TradeBoard? Board => null;

    /// <summary>The regions the player has entered, by id: what a blind trade and a request's offer are drawn from. The region the player is in unless a host says.</summary>
    IReadOnlyList<string> RegionsEntered => new[] { Region };

    /// <summary>Whether the run under way holds its Pokémon, so that none is traded away (plan 13's Nuzlocke and restrictions, as the Home box asks).</summary>
    bool RunHoldsTrades => false;

    /// <summary>
    /// A Pokémon received by a trade of the board in the given one's place (plan 40 · GE8): the game's own way in,
    /// <c>ReceiveTradedPokemon</c>, which plays its trade evolution; a host with none swaps it into the team.
    /// </summary>
    void ReceiveTraded(Pokemon received, Pokemon given)
    {
        int at = Party.Members.IndexOf(given);
        if (at >= 0) Party.Members[at] = received;
    }
    int Money { get; set; }
    string PlayerName { get; }
    PlayerLook PlayerLook { get; }

    // ------------------------------------------------------------------ the field

    /// <summary>
    /// Someone of the map the player is on, by what scripts call them or by their name, whether or not a flag
    /// hides them; null if nobody is called that. <paramref name="place"/> is the file the script is written in
    /// (an area's key), which says whose "clown_1" is meant on a map of many towns.
    /// </summary>
    NPC? FindNpc(string name, string? place);

    (int X, int Y) TileOf(NPC? who);
    Direction FacingOf(NPC? who);
    void Face(NPC? who, Direction direction);
    void Place(NPC? who, int x, int y, Direction? facing);

    /// <summary>Takes someone off the map or puts them back, whatever their flags say, until the map is come to again.</summary>
    void SetVisible(NPC who, bool visible);

    /// <summary>
    /// Starts someone walking; the script carries on meanwhile unless it waits (<see cref="Walking"/>). A step may be
    /// on the spot, or keep the facing (plan 11 · C11).
    /// </summary>
    void Walk(NPC? who, IReadOnlyList<ScriptStep> steps, bool fast);

    /// <summary>Whether anyone a script set walking is still on their way.</summary>
    bool Walking { get; }

    void Emote(NPC? who, EmoteBubble bubble, float seconds);

    /// <summary>
    /// Someone plays a gesture <paramref name="times"/> times in a row (plan 11 · C11); the script waits for its end
    /// (<see cref="Busy"/>) as it waits for a walk.
    /// </summary>
    void Act(NPC? who, Gesture gesture, int times);

    /// <summary>Someone holds a face (plan 11 · C11) until a script changes it or the place is come to again.</summary>
    void Mood(NPC? who, Mood mood);

    /// <summary>
    /// Someone is set to sit, to sit on the ground, to lie or to stand (plan 11 · C7), whatever is under them, until the
    /// place is come to again (nobody is the player). Nothing is waited for: a stance is taken at once.
    /// </summary>
    void Pose(NPC? who, Stance stance);

    /// <summary>
    /// The field's camera is told something (<c>CameraRig</c>; plan 09 · L4 for all but the first three): to look at a
    /// tile (<see cref="CameraMove.Pan"/>), to come back and bring the lens, the bars and the field's pace back with it
    /// (<c>Release</c>), to shake, to take its lens to a <paramref name="factor"/> (<c>Zoom</c>), to keep on
    /// <paramref name="who"/>'s feet (<c>Follow</c>; nobody is the player, which brings the camera home and leaves the
    /// rest), or to travel through the tiles of a <paramref name="path"/> at <paramref name="factor"/> tiles a second
    /// (<c>Path</c>). Returns how long the move takes, which the runner itself waits unless the line says otherwise:
    /// a path's time depends on where the camera is when it begins, which only the host knows.
    /// </summary>
    float Camera(CameraMove move, int x, int y, float seconds, float factor = 1f, NPC? who = null, IReadOnlyList<(int X, int Y)>? path = null);

    /// <summary>The cinema bars slide in or away over <paramref name="seconds"/>, under whatever the script does next (plan 09 · L4).</summary>
    void Letterbox(bool on, float seconds);

    /// <summary>
    /// The field's light held at an <paramref name="hour"/> for a scene, or given back to the clock (null; plan 20, part
    /// w): the picture's alone, never the hour the rules read. A hold lasts until the player next changes place.
    /// </summary>
    void HoldLight(float? hour) { }

    /// <summary>
    /// The field runs at a <paramref name="share"/> of its pace for <paramref name="seconds"/> and is back by itself
    /// (plan 09 · L4): people, life and weather are slowed, never the camera, the text or the script's own waits.
    /// </summary>
    void SlowMotion(float share, float seconds);

    /// <summary>
    /// A scene effect lit on a tile (plan 09 · L11; style guide, "Life", "Scene effects"): it plays for
    /// <paramref name="seconds"/> under whatever the script does next, or round after round until
    /// <see cref="StopEffect"/> when <paramref name="hold"/>; a shadow crosses the ground <paramref name="way"/>
    /// (east when none is given). The runner itself waits for one the script waits for.
    /// </summary>
    void Effect(SceneEffect kind, int x, int y, float seconds, Direction? way, bool hold);

    /// <summary>Every effect held on a tile goes as it would at its end.</summary>
    void StopEffect(int x, int y);

    /// <summary>
    /// A set piece the original's scripts call by name is played where the player stands (plan 09 · L12; style guide,
    /// "Rooms", "Machines at work"): a Pokémon Center's machine or the Hall of Fame's with a ball for each of
    /// <paramref name="count"/> Pokémon, the PC before the player switched on or the one that is on switched off, a
    /// lift's ride of <paramref name="count"/> hums. Returns how long it takes, which the runner itself waits; nothing
    /// where the place has nothing to play it on, so a script is never left waiting.
    /// </summary>
    float Anim(SetPiece piece, int count);

    /// <summary>
    /// Someone is shaken so many times where they stand (plan 09 · L12; the original's <c>ShakeObject</c>): drawn a few
    /// texels to either side, their tile unchanged. Returns how long it takes, which the runner waits; nothing for
    /// someone who isn't on the map. Nobody is the player.
    /// </summary>
    float Shake(NPC? who, int times);

    /// <summary>
    /// Someone flickers (plan 09 · L12; the original's <c>FlickerObject</c>): unseen and seen by turns, changing so
    /// many times in all, and seen again at the end. Returns how long it takes; nothing for someone who isn't on the map.
    /// </summary>
    float Flicker(NPC? who, int times);

    // ------------------------------------------------------------------ a scene skipped (plan 12 · Q20)

    /// <summary>
    /// True while the runner hurries through a scene the player is skipping (<see cref="ScriptRunner.Hurrying"/>),
    /// from <see cref="BeginSkipping"/> to <see cref="EndSkipping"/>. <b>Every method of a host that starts something
    /// the player sees through says what it does meanwhile</b>, one of three things. What is only shown or heard is
    /// dropped: lines (unread), a bubble, a gesture, a sound, a fanfare, a cry, a shake, an effect that passes, slow
    /// motion. What leaves something behind is ended at once in the state it leaves: a warp arrives; a fade, the
    /// script's black screen, a camera's move, the lens and the bars are where they were going; an effect that is
    /// held is lit, a machine's frame is the one its piece ends on, the music is the song asked for. A walk is
    /// neither: whoever is sent walking takes their own steps in the time the scene gives them, unseen
    /// (<see cref="Elapse"/>, <see cref="Arrive"/>), because where a walk ends can hang on who stood where when, so
    /// they end on the tile and facing the way a played scene leaves them. And what waits for the player (a
    /// question, a battle, a screen, a save) or can't be ended at once (a puzzle's pieces, a field move) is never
    /// reached in a hurry: its command is one <see cref="ScriptRunner.StopsAHurry"/> names, and the runner ends the
    /// hurry before it. A host that turns <see cref="Busy"/> in a hurry all the same ends the hurry there.
    /// Whatever a line does to the story, the bag, the team or the map happens exactly as when it is played.
    /// </summary>
    bool Skipping { get; }

    /// <summary>
    /// A hurry begins: whatever a script left going is brought to its end at once (text is put away unread, a fade
    /// and the camera's moves are over, bubbles, gestures, passing effects and set pieces are gone), but for whoever
    /// is walking, who goes on from where they are; and from here to <see cref="EndSkipping"/> the host shows nothing.
    /// </summary>
    void BeginSkipping();

    /// <summary>
    /// The hurry is over: at the scene's end, or before something that waits for the player. The host shows what it
    /// is told again; whoever is still walking walks on, as they would be walking there with the scene played.
    /// </summary>
    void EndSkipping();

    /// <summary>
    /// In a hurry, the time the scene would have waited here: a pause, a bubble, a camera's move, an effect waited
    /// for, a set piece. Nothing waits for it, but it passes all the same for whoever is walking, who take the steps
    /// they would have taken meanwhile: someone sent ahead before a pause is as far ahead after it as when the scene
    /// is played, and whoever follows finds nobody in their way. A host whose walks take no time has nothing to do.
    /// </summary>
    void Elapse(float seconds);

    /// <summary>
    /// In a hurry, where the scene waits for its walks (<c>walk</c>, <c>waitmoves</c>): everyone walking takes their
    /// steps to the end, all together and each their own (the player's are the player's, which a wall or someone
    /// standing in the way stops), so they stand where the scene played would have them.
    /// </summary>
    void Arrive();

    /// <summary>A scene begins (<c>scene "Name"</c>): its key is the file it is written in and its name (<see cref="SceneSkip.Key"/>).</summary>
    void SceneBegins(string key);

    /// <summary>
    /// A scene is over: <paramref name="seen"/> when it was played, or skipped, to its <c>endscene</c>, which the story
    /// has noted already (<see cref="StoryState.ScenesSeen"/>) and the host notes for its machine; not when the
    /// script ended some other way.
    /// </summary>
    void SceneEnds(string key, bool seen);

    // ------------------------------------------------------------------ what the script waits for

    /// <summary>
    /// True while something a script started is still going on and the script must wait: text on the screen, a
    /// question unanswered, a battle, a screen, a fade.
    /// </summary>
    bool Busy { get; }

    /// <summary>Shows lines one after another, each waiting for the player; <paramref name="speaker"/> is the name on the box, or null.</summary>
    void Say(string? speaker, IReadOnlyList<string> lines);

    /// <summary>
    /// Shows a question and its answers. <see cref="Answer"/> is the answer picked once the host is no longer busy;
    /// backing out picks <paramref name="cancel"/>.
    /// </summary>
    void Ask(string? speaker, string question, IReadOnlyList<string> answers, int cancel);
    int Answer { get; }

    /// <summary>
    /// <see cref="Say(string?, IReadOnlyList{string})"/> with each line's voice beside it (plan 26 · V2): one entry to a
    /// line, null for a line nobody speaks. A host with no voices shows the lines alone.
    /// </summary>
    void Say(string? speaker, IReadOnlyList<string> lines, IReadOnlyList<SpokenLine?> voices) => Say(speaker, lines);

    /// <summary><see cref="Ask(string?, string, IReadOnlyList{string}, int)"/> with the question's voice (its answers are never spoken).</summary>
    void Ask(string? speaker, string question, IReadOnlyList<string> answers, int cancel, SpokenLine? voice) => Ask(speaker, question, answers, cancel);

    /// <summary>Whether the last line's voice still speaks (plan 26 · V2): what a script's <c>waitvoice</c> waits for. Never, where there are no voices.</summary>
    bool VoiceSpeaking => false;

    /// <summary>
    /// Battles a trainer of the map, or two at once (<paramref name="second"/>), perhaps with a trainer at the
    /// player's side (<paramref name="partner"/>, a tag battle). <see cref="Outcome"/> is how it went once the host is
    /// no longer busy. The game's first battle (<paramref name="first"/>) has no critical hits. What the script asks
    /// of the battle itself comes as its <paramref name="terms"/> (plan 14 · B1: an inverse battle), which the host
    /// lays over the place's own conditions as it makes the battle; null asks nothing.
    /// </summary>
    void Battle(NPC trainer, NPC? second, Trainer? partner, bool mayLose, bool first, BattleTerms? terms = null);

    /// <summary>
    /// A Battle Royal against three trainers of the map, each for themselves (plan 14 · B13). <see cref="Outcome"/> is
    /// <see cref="BattleOutcome.Won"/> for first place and <see cref="BattleOutcome.None"/> for any other: a Royal is
    /// fought for its placing and gives nothing. A host that can't fight one fights the first two as a double.
    /// </summary>
    void Royal(NPC first, NPC second, NPC third, bool mayLose) => Battle(first, second, null, mayLose, false);

    /// <summary>A wild Pokémon in the player's way: one met as ever, one that can't be run from, or the assistant's catching lesson, which the player watches.</summary>
    /// <remarks>A lesson someone else shows names them and their Pokémon (<paramref name="catcher"/>; plan 21 · H2, Wally with his father's Zigzagoon).</remarks>
    void WildBattle(Pokemon wild, BattleKind kind, bool cannotFlee, LessonCatcher? catcher = null, BattleTerms? terms = null, bool mayLose = false);

    /// <summary>
    /// A trial's totem (plan 23 · Z6): a wild battle that can't be run from, fought as an SOS battle under the totem's
    /// <paramref name="aura"/>, in which nothing can be caught. A host that has no such battle fights it as a wild one.
    /// </summary>
    void TotemBattle(Pokemon wild, Battle.Sim.TotemAura aura) => WildBattle(wild, BattleKind.Normal, cannotFlee: true);

    /// <summary>
    /// A Max Raid den's battle (plan 33 · GL3, the script's <c>raid fight</c>): the den's boss with its stars and three
    /// raiders beside the player. A host with no raids fights it as a wild battle.
    /// </summary>
    void RaidBattle(Pokemon boss, Battle.Sim.RaidTerms raid) => WildBattle(boss, BattleKind.Normal, cannotFlee: false);

    /// <summary>The day's number (<see cref="Models.SpecialEncounters.DailyNumber"/>) a den's day is drawn from; nought for a host with none.</summary>
    uint DayNumber => 0;
    BattleOutcome Outcome { get; }

    /// <summary>
    /// Opens a screen and waits for it to close. The starters' sets <see cref="Answer"/> to the one chosen (0 to
    /// 2); the way to another region sets it to 1 where there is one from here and 0 where there is none.
    /// </summary>
    void Open(ScriptScreen screen, NPC? subject, string? counter = null);

    /// <summary>
    /// Trades the team's Pokémon at <paramref name="slot"/> for the given trade's (<see cref="NpcTrades"/>): false,
    /// with nothing changed, when it isn't the species the trade asks for.
    /// </summary>
    bool Trade(string trade, int slot);

    /// <summary>Writes a line in the Journal (plan 06 · R12).</summary>
    void Note(JournalEvent line);

    /// <summary>Enters the team into the Hall of Fame (<see cref="Models.HallOfFame"/>) on this day.</summary>
    void EnterHallOfFame();

    /// <summary>Adds a Pokémon to the team, or to the PC when the team is full; true if it went to the team.</summary>
    bool GivePokemon(Pokemon pokemon);

    void Warp(string map, int x, int y, Direction? facing);

    /// <summary>
    /// <c>travel to</c> (plan 20 · J13, part d): into another region at a tile of its map, as every crossing goes
    /// (<c>GameEngine.CrossTo</c>: the team given the region it leaves and taken along whole, the region's Pokédex and
    /// rival, its music and name sign). A map of the region the player is in is a plain warp; a host that knows no
    /// regions warps.
    /// </summary>
    void CrossInto(string map, int x, int y, Direction? facing) => Warp(map, x, y, facing);

    void Fade(bool toBlack, float seconds);

    // ------------------------------------------------------------------ field moves (plan 02 · S2)

    /// <summary>
    /// A Pokémon of the team uses a field move, once the line that says so has been read: its cut-in and its
    /// sound, and what it does to the obstacle the script belongs to (a tree falling, a rock breaking apart).
    /// The script waits while it plays (<see cref="Busy"/>). What the move leaves behind is the script's to say
    /// with flags: the obstacle's own flag, <c>FLAG_STRENGTH_ACTIVE</c>.
    /// </summary>
    void UseMove(FieldMove move, Pokemon user, NPC? subject);

    /// <summary>
    /// The field moves done by a wild helper now (plan 12 · Q18): worked out from the options' row, the rules, the Badges and
    /// the bag each time it is asked, and empty with the row off. <c>if granted</c> in a script asks it; <c>usemove</c> falls
    /// back to the helper when nobody on the team knows the move.
    /// </summary>
    IReadOnlyCollection<FieldMove> Granted => System.Array.Empty<FieldMove>();

    /// <summary>Sets out onto the water the player faces, on a Pokémon's back. False when there is no water there to surf on.</summary>
    bool Surf();

    /// <summary>Climbs the waterfall or the rock face the player faces, up or down. False when there is none to climb from here.</summary>
    bool Climb();

    /// <summary>Flies to the town chosen on the map before the script began. False when none was chosen.</summary>
    bool Fly();

    /// <summary>
    /// Dive (plan 21 · H5): down from the dark water the player surfs on to the same tile of the sea floor under it, or
    /// up from the sea floor to the same tile of the sea above, behind a fade. False when there is no floor below, or no
    /// sea above that may be come up to, where the player is.
    /// </summary>
    bool Dive();

    /// <summary>Back to the town of the Pokémon Center the player last went into (Teleport). False when there is no way there.</summary>
    bool Teleport();

    /// <summary>Out of the caves, to where the player went into them (Dig, an Escape Rope). False outside a cave or with no way out known.</summary>
    bool Escape();

    /// <summary>Turnback Cave aims the doors of the room the player has just come into (<see cref="TurnbackCave"/>).</summary>
    void Turnback();

    /// <summary>
    /// A trainer counts as beaten from now on, with no battle (the original's <c>SetTrainerFlag</c>, plan 01 · M9): a
    /// Gym's trainers once its Leader is. The story has noted it already; whoever carries the trainer is marked.
    /// </summary>
    void Defeat(string trainerId);

    /// <summary>
    /// The Eterna Gym's flower clock turns from the time of one state to the next's, with the camera on it, and a
    /// fountain drains where the new state says so (<see cref="EternaClock.Turn"/>). The story's state has moved on
    /// already; the script waits while the clock turns (<see cref="Busy"/>).
    /// </summary>
    void TurnClock(int from, int to);

    /// <summary>
    /// A button of the Pastoria Gym is stepped on (<c>PressPastoriaGymButton</c>): the water and the floor on it set off
    /// for its colour's level; the script waits while they move (<see cref="Busy"/>).
    /// </summary>
    void PressButton(PastoriaWater.Button button);

    /// <summary>
    /// A floor switch of the Mossdeep Gym is stepped on (plan 21 · H8; Emerald's <c>moverotatingtileobjects</c> and
    /// <c>turnrotatingtileobjects</c>): everyone standing on the colour's arrows steps the way their arrow points, keeping
    /// their facing, and turns a quarter anticlockwise (<see cref="MossdeepTiles.Turns"/>); the script waits for the
    /// steps as for a walk (<see cref="Walking"/>). Nothing where the place has no such floor.
    /// </summary>
    void RotateTiles(MossdeepTiles.Colour colour);

    /// <summary>
    /// A button of the Sunyshore Gym is stepped on (<c>PressSunyshoreGymButton</c>): every gear of the room turns on a
    /// quarter, back a quarter or on a half (<see cref="SunyshoreGears"/>); the script waits while they turn (<see cref="Busy"/>).
    /// </summary>
    void PressGearButton(SunyshoreGears.Button kind);

    /// <summary>
    /// The platform of the room's shaft sets off for the top with the player on it (<c>TriggerPlatformLift</c>;
    /// <see cref="LeagueLift"/>); the script waits while it rises (<see cref="Busy"/>). False where it is at the top
    /// already, or the room has none.
    /// </summary>
    bool TriggerLift();

    /// <summary>Whether the room's platform stood at the foot of its shaft as the player came in (<c>CheckPlatformLiftNotUsedWhenEnteredMap</c>).</summary>
    bool LiftNotUsed();

    /// <summary>
    /// Someone of the map starts travelling with the player, walking behind and battling beside them as the trainer
    /// of Platinum's data <paramref name="trainerId"/> (plan 02 · S6, <see cref="Follower"/>); null for both and they
    /// stop.
    /// </summary>
    void TravelWith(NPC? who, string? trainerId);

    /// <summary>The trainer id of whoever travels with the player now; null when nobody does.</summary>
    string? Partner { get; }

    /// <summary>
    /// Draws a wild Pokémon out where the player stands (Sweet Scent): true when one comes, and its battle starts
    /// (<see cref="Outcome"/> once the host is no longer busy); false where nothing lives.
    /// </summary>
    bool SweetScent();

    /// <summary>
    /// What a rock just smashed hid (plan 21 · H4; Emerald's <c>RockSmashWildEncounter</c>): true when a wild Pokémon of
    /// the place came out from under it, and its battle starts; false when it hid nothing, which is always so where the
    /// place has no such table (every place of Platinum's).
    /// </summary>
    bool SmashEncounter();

    /// <summary>
    /// The Headbutt tree in front shaken (plan 38 · EX4; <see cref="Overworld.HeadbuttTrees"/>): true when a wild
    /// Pokémon fell out of it, and its battle starts; false when nothing did, which is always so for a tree no area
    /// lists and in every region but Johto. The default shakes out nothing.
    /// </summary>
    bool Headbutt() => false;

    // ------------------------------------------------------------------ autosave (plan 12 · Q3)

    /// <summary>
    /// A moment worth an autosave has come in a script (<see cref="AutosaveMoment.Heal"/>, <see cref="AutosaveMoment.Badge"/>):
    /// the game's host notes it and writes once the script has ended, never in the middle of one. The default does
    /// nothing (a host with no screen has nothing to save).
    /// </summary>
    void Milestone(AutosaveMoment moment) { }

    /// <summary>
    /// A trainer tip raised (plan 12 · Q23, <see cref="Models.Tips"/>): the game's host waits for the next quiet moment
    /// (the script ended) and shows it once, while the options' TIPS row holds. The default does nothing.
    /// </summary>
    void Tip(string id) { }

    /// <summary>
    /// A photograph is taken (plan 20 · J13: Clair's in the Dragon's Den, HeartGold's photo with her): the game writes the
    /// next frame drawn into the player's screenshots, as the screenshot key does, once the line before it is put away.
    /// The default takes none (a host with no screen); a hurry takes none either.
    /// </summary>
    void Photograph() { }

    // ------------------------------------------------------------------ sound

    /// <summary>Plays a song; null goes back to the theme of the place, and an empty name is silence.</summary>
    void Music(string? song);
    void Fanfare(MusicRole role);
    void Sound(string name);

    /// <summary>A Pokémon met in the field cries: a species, or one of its forms by name.</summary>
    void Cry(string species);

    // ------------------------------------------------------------------ wild Pokémon (plan 06 · R13)

    /// <summary>What the game remembers of its wild Pokémon: swarms, the Trophy Garden, the honey trees, the roamers.</summary>
    SpecialEncounters Encounters { get; }

    /// <summary>The honey tree the player faces from the south (<see cref="HoneyTrees.Faced"/>); null when none.</summary>
    int? HoneyTreeFaced { get; }

    /// <summary>The trainer's whole number, the card's and its hidden half (the original's 32 bits): it picks the Munchlax trees.</summary>
    uint TrainerNumber { get; }

    /// <summary>Where a script's own draws come from: a honey tree slathered, a Trophy Garden Pokémon, a roamer set loose.</summary>
    System.Random Chance { get; }

    // ------------------------------------------------------------------ the clock (plan 06 · R14a)

    /// <summary>The time of day now, one of Platinum's five (<see cref="GameClock.Now"/>).</summary>
    TimeOfDay TimeOfDay { get; }

    /// <summary>The day of the week today (<see cref="GameClock.Today"/>).</summary>
    System.DayOfWeek Weekday { get; }

    /// <summary>The game's date today (<see cref="GameClock.Today"/>): what the Battle Institute's ladder's seasons go by (plan 37 · FC8).</summary>
    System.DateTime Today => GameClock.Today;

    /// <summary>The hour of the day now, 0 to 23 (<see cref="GameClock.Hour"/>): what Shoal Cave's <c>tide</c> reads (plan 21 · H8, part d).</summary>
    int Hour { get; }

    /// <summary>
    /// Whether the player has just come to this place from outdoors (Emerald's <c>IsMapTypeOutdoors(GetLastUsedWarpMapType())</c>):
    /// only then does <c>tide</c> read the hour (plan 21 · H8, part d).
    /// </summary>
    bool CameInFromOutdoors { get; }

    /// <summary>Every Pokémon in the PC's boxes, box by box and place by place (the lottery's draw reads them).</summary>
    IEnumerable<Pokemon> Boxed { get; }

    // ------------------------------------------------------------------ the television (plan 08 · P8)

    /// <summary>
    /// What is true of the game just now that a television's programme may speak of (<c>broadcast</c>,
    /// <see cref="Broadcasts"/>). The default reads what the host already says; the game's host fills in the clock,
    /// the play time and the whole Pokédex's counts.
    /// </summary>
    BroadcastFacts Broadcast() => Broadcasts.FactsOf(this);

    // ------------------------------------------------------------------ berry patches (plan 06 · R14a)

    /// <summary>Sinnoh's berry patches.</summary>
    BerryPatches Berries { get; }

    /// <summary>
    /// The patches of the soils on the eight tiles round a soil of the map the player is on (plan 39 · CR5): what a berry
    /// planted there may cross with. None where a host has no map.
    /// </summary>
    IEnumerable<int> PatchesBeside(NPC soil) => System.Array.Empty<int>();

    /// <summary>The Apricorn Box, the trees picked and Kurt's order (plan 20 · J6).</summary>
    ApricornBox Apricorns { get; }

    /// <summary>The Laurel Circuit (plan 41 · EG4); none where a host keeps none, and <c>circuit</c> then answers 0.</summary>
    CircuitState? Circuit => null;

    /// <summary>The crafting kit's pouch of materials (plan 39 · CR1); none where a host keeps none, and <c>craft</c> then does nothing.</summary>
    CraftingState? Crafting => null;

    /// <summary>Lockwork's locked Pokémon reclaimed, missed and opened (plan 44 · SD2); none where a host keeps none, and <c>heart</c>'s counts then answer 0.</summary>
    HeartsRecord? Hearts => null;

    /// <summary>A locked heart whose gauge has emptied opens (plan 44 · SD2): the game plays its scene; a host with no screen opens it at once.</summary>
    void OpenHeart(Pokemon pokemon)
    {
        var opening = HeartLock.Open(pokemon);
        Hearts?.Open(opening.LockId);
    }

    /// <summary>The PC offers the pasture here (plan 35 · BN5): the region the player stands in has one, under the modern rules. None where a host keeps none.</summary>
    bool PastureHere => false;

    /// <summary>
    /// An act of the pasture (plan 35 · BN5; the script's <c>pasture</c>): the way in and out, the box shown, and what is done
    /// with the Pokémon of the meadow the script belongs to (<paramref name="subject"/>), the item last chosen given for a meal.
    /// A host with no pasture answers 0 to everything.
    /// </summary>
    PastureAnswer Pasture(string act, int number, NPC? subject, string? item) => new(0);

    /// <summary>
    /// An act of the Stroll Counter at a PC (plan 35 · BN7; the script's <c>stroll</c>): its Pokémon back and paid, what it met
    /// and found counted, the PC's boxes opened to pick another, and that one put in. A host with no counter answers 0.
    /// </summary>
    StrollAnswer Stroll(string act) => new(0);

    /// <summary>Pennant Hall, the hub's plaza (plan 41 · EG3); none where a host keeps none, and <c>plaza</c> then does nothing.</summary>
    PlazaState? Plaza => null;

    /// <summary>
    /// The plaza changed as it stands (a stall set out, the day's guests drawn): the game furnishes the hall afresh and
    /// stands its people (plan 41 · EG3). Nothing where a host draws no hall.
    /// </summary>
    void PlazaChanged() { }

    /// <summary>Whether the pennants held stand over the field while a script speaks of them (plan 41 · EG3), as the Battle Points do.</summary>
    void ShowPennants(bool shown) { }

    /// <summary>The Week's Work, the hub's board of missions (plan 41 · EG6); none where a host keeps none, and <c>missions</c> then does nothing.</summary>
    MissionBoard? Missions => null;

    /// <summary>The timed powers in force (plan 36; plan 39 · CR6's dishes start them); none where a host keeps none, and a meal then starts nothing.</summary>
    PowerState? Powers => null;

    /// <summary>The training drills' records (plan 36 · TR2); none where a host keeps none, and <c>train prize</c> then gives nothing.</summary>
    TrainingRecords? Training => null;

    /// <summary>Cameron's photo album (plan 20, part q); none where a host keeps no album, and <c>photo take</c> then takes nothing.</summary>
    PhotoAlbum? Photos => null;

    /// <summary>The player's photo book (plan 43 · LF4); none in a host that keeps none.</summary>
    PhotoBook? PhotoBook => null;

    /// <summary>The photo tour's report (plan 44 · SD5); none in a host that keeps none, and <c>tour ride</c> then rides nothing.</summary>
    TourReport? PhotoTour => null;

    /// <summary>The delve's record (plan 44 · SD9); none in a host that keeps none, and <c>delve go</c> then goes nowhere.</summary>
    Models.Delve.DelveRecord? DelveRecord => null;

    /// <summary>
    /// A photo spot read (plan 43 · LF4, <c>photo spot "&lt;id&gt;"</c>): photo mode opened on the spot's view, the script
    /// waiting until it is left and hearing 1 for a photograph taken; a host with no picture keeps a record at once. False
    /// where nothing opened (no such spot, a full book).
    /// </summary>
    bool PhotoAtSpot(string spot) => false;

    /// <summary>
    /// Whether the options name an online server (plan 07 · O7: <c>globallink check</c>), which the Global Link counter's
    /// attendant asks before opening it. None unless a host says otherwise.
    /// </summary>
    bool OnlineServerSet => false;

    /// <summary>The item the bag chose last for <c>chooseitem</c> (<see cref="ScriptScreen.ChooseItem"/>); null for none.</summary>
    string? ChosenItem { get; }

    // ------------------------------------------------------------------ Poffins (plan 06 · R14c)

    /// <summary>The player's Poffin Case.</summary>
    PoffinCase Poffins { get; }

    // ------------------------------------------------------------------ the Day Care and Eggs (plan 06 · R15)

    /// <summary>Solaceon Town's Day Care.</summary>
    DayCare DayCare { get; }

    // ------------------------------------------------------------------ the Super Contests (plan 06 · R17a)

    /// <summary>The player's Fashion Case.</summary>
    Models.SuperContests.FashionCase FashionCase { get; }

    /// <summary>
    /// Holds a Super Contest for the team's Pokémon at a place (<c>supercontest enter</c>): its screen plays and the
    /// script waits for it; afterwards <see cref="ContestOutcome"/> says how it came out.
    /// </summary>
    void HoldContest(int slot, ContestStat category, Models.SuperContests.ContestRank rank, Models.SuperContests.ContestMode mode);

    /// <summary>How the last contest held came out; null before any.</summary>
    Models.SuperContests.ContestOutcome? ContestOutcome { get; }

    // ------------------------------------------------------------------ Ball Capsules, Seals and ribbons (plan 06 · R17b)

    /// <summary>The player's Seal Case: its loose Seals and its twelve Ball Capsules.</summary>
    SealCase SealCase { get; }

    /// <summary>The contests the player has entered and won, and the winners' photographs in the Contest Hall's frames.</summary>
    Models.SuperContests.ContestRecords ContestRecords { get; }

    // ------------------------------------------------------------------ the Underground (plan 06 · R16a)

    /// <summary>What the game keeps of the Underground: its bag and PC, what lies buried, the base, the records.</summary>
    Models.Underground.UndergroundState Underground { get; }

    /// <summary>
    /// What the Underground's commands do where the player stands (<c>underground down</c>, <c>up</c>, <c>mine</c>,
    /// <c>hiker</c>, <c>pc</c>, <c>bag</c>, <c>cantake</c>, <c>takeflag</c>, <c>home</c>); the number is the script's
    /// RESULT. A screen it opens keeps the script waiting (<see cref="Busy"/>).
    /// </summary>
    int UndergroundAct(string what, NPC? subject);

    // ------------------------------------------------------------------ Hoenn's secret bases (plan 21 · H10)

    /// <summary>The decoration PC: every decoration the player has, and whether it stands in the base.</summary>
    List<Models.StoredDecoration> Decorations { get; }

    /// <summary>
    /// What a secret base's commands do where the player stands (<c>openbase</c>, <c>check</c>, <c>kind</c>,
    /// <c>enter</c>, <c>pc</c>, <c>packup</c>); the number is the script's RESULT. Going in or out is a warp, and the
    /// decorating a screen, which keep the script waiting (<see cref="Busy"/>).
    /// </summary>
    int SecretBaseAct(string what);

    // ------------------------------------------------------------------ Hoenn's contests and Pokéblocks (plan 21 · H11)

    /// <summary>The contests Hoenn's hall has held for the player and the winners it has hung.</summary>
    Models.Contest.HoennContestRecords HoennContests { get; }

    /// <summary>The Pokéblock Case.</summary>
    PokeblockCase PokeblockCase { get; }

    /// <summary>
    /// Holds a Hoenn contest for the team's Pokémon (<c>appeals enter</c>): its screen plays and the script waits for
    /// it; afterwards <see cref="AppealOutcome"/> says how it came out.
    /// </summary>
    void HoldAppeals(int slot, ContestStat category, Models.Contest.AppealRank rank);

    /// <summary>How the last Hoenn contest came out; null before any.</summary>
    Models.Contest.AppealOutcome? AppealOutcome { get; }

    /// <summary>The Berry Blender with so many others (<c>pokeblock blend</c>): the bag chooses a berry, the screen plays, the script waits.</summary>
    void BlendPokeblocks(int others);

    /// <summary>Whether the last blend put a Pokéblock in the case.</summary>
    bool PokeblockKept { get; }

    // ------------------------------------------------------------------ fossils, the Game Corner, the Villa, Pal Park (plan 06 · R16b)

    /// <summary>The Coin Case's coins, Pal Park's six and its show, and the game records the Villa's catalogue asks about.</summary>
    Models.Pastimes Pastimes { get; }

    /// <summary>What the Villa's catalogue reads of the game's records before it lists its later pieces.</summary>
    Models.VillaRecords VillaRecords { get; }

    /// <summary>
    /// What those commands need of the game itself: <c>slots</c> sits the player at a machine of the Game Corner (the
    /// script waits; the number is the machine's), <c>furnish</c> stands the Villa's furniture in its room,
    /// <c>boxroom</c> is how many free places the boxes have, <c>store</c> sends Pal Park's six to them (1 when done).
    /// </summary>
    int PastimeAct(string what, int number, NPC? subject);

    /// <summary>Whose base the player is in, or whose flag they carry: <c>{digger}</c> in a line.</summary>
    string DiggerName { get; }

    /// <summary>
    /// Hatches the team's first Egg with no cycles left (<c>HatchEgg</c>): its scene plays and the script waits for it
    /// (<see cref="Busy"/>); then the Pokémon is in the Pokédex. False when no Egg is ready.
    /// </summary>
    bool Hatch();

    // ------------------------------------------------------------------ the Pokédex (plan 02 · S12)

    /// <summary>
    /// The Pokédex records a species as seen (<c>seen</c>; the original's <c>SetSpeciesSeen</c>): a Pokémon the story
    /// shows without a battle, as Dialga and Palkia come at Spear Pillar.
    /// </summary>
    void Seen(PokemonSpecies species);

    /// <summary>
    /// A number has been registered in the Pokégear's phone (<c>addcontact</c>, plan 20 · J3): the story keeps it, and
    /// the host tells the player (the original's "Registered … in the Pokégear!" with its jingle).
    /// </summary>
    void ContactAdded(Models.PhoneContact contact);

    /// <summary>
    /// A die of so many sides, rolled on the game's own chance (plan 01 · M11; the original's <c>GetRandom</c>): 0 to one
    /// less than <paramref name="sides"/>. The Berry Master's berry of the day, a ghost of the Old Chateau.
    /// </summary>
    int Random(int sides);

    /// <summary>
    /// A species the Pokédex has seen, drawn on the game's own chance (<c>random seen</c>; plan 19 · K9, part c:
    /// FireRed's <c>SampleResortGorgeousMon</c>, which Selphy's wish is drawn with): its National Pokédex number, or 0
    /// where the Pokédex has seen none. The original tries a hundred species at random and then counts down to one it
    /// has seen; this draws among those seen, which comes to the same spread without the fallback's bias.
    /// </summary>
    int SpeciesSeenAtRandom() => 0;

    // ------------------------------------------------------------------ the Battle Frontier (plan 06 · R18a)

    /// <summary>The Battle Frontier's records: every facility's numbers, the streaks running, the Battle Points.</summary>
    Models.Frontier.FrontierRecords Frontier { get; }

    /// <summary>The Battle Tower challenge under way; null while none is (the original's <c>fieldSystem->battleTower</c>).</summary>
    Models.Frontier.BattleTowerChallenge? Tower { get; set; }

    /// <summary>
    /// What the Frontier's commands need of the game itself. <c>enter</c> opens the team for that many Pokémon to be
    /// chosen in order (the script waits; <see cref="Answer"/> is 1 when they were, and <see cref="Entered"/> their
    /// places); <c>records</c> opens the board of the facility of that number and waits; <c>seen</c> is 1 when the
    /// Pokédex has seen the species of that number; <c>score</c> adds to the Trainer Card's score; <c>opponents</c>
    /// stands whoever the Battle Tower has made ready in its battle room, and <c>partner</c> whoever battles beside the
    /// player (the number is which of the five); <c>points</c> shows the Battle Points over the field until the script
    /// ends; <c>save</c> saves the game where the player stands; <c>journal</c> notes a challenge begun.
    /// </summary>
    int FrontierAct(string what, int number, NPC? subject);

    /// <summary>The places in the team of the Pokémon last chosen with <c>enter</c>, in the order chosen.</summary>
    IReadOnlyList<int> Entered { get; }

    /// <summary>
    /// A battle of the Battle Frontier (<see cref="BattleSetup.Frontier"/>): the team is the battle's own copy, the
    /// foes are trainers made for it, and nothing it does reaches the player's Pokémon, bag or money. Lost, the script
    /// goes on. <see cref="Outcome"/> is how it went once the host is no longer busy. <paramref name="conditions"/>
    /// are the weather or the Trick Room the battle opens under, where a facility sets one (the Battle Arcade's
    /// board, plan 06 · R18d).
    /// </summary>
    void FrontierBattle(Party team, IReadOnlyList<Trainer> foes, Trainer? partner, bool doubles, Models.Frontier.FrontierConditions? conditions = null);

    /// <summary>
    /// The friend's trainer a person of the friends' hall stands for (plan 13 · V23), read from their file; null for
    /// anyone else. A host with no hall has none.
    /// </summary>
    TrainerFile? FriendOf(NPC? person) => null;

    /// <summary>Who of the friends' hall has been beaten, and when (plan 13 · V23); null for a host with no hall.</summary>
    FriendsHallState? FriendsHall => null;

    /// <summary>A friend's trainer beaten (plan 13 · V23): a trainer's battle won, for the game's counters. Nothing by default.</summary>
    void FriendWon() { }

    // ------------------------------------------------------------------ records mixed (plan 40 · GE10)

    /// <summary>The records this game has mixed in (<see cref="RecordMixState"/>); null for a host that keeps none.</summary>
    RecordMixState? MixedRecords => null;

    /// <summary>
    /// The player's other saves, read and never written, in slot order: every slot but the game's own that reads whole or
    /// from its backup, a damaged one passed over. None for a host with no slots.
    /// </summary>
    System.Collections.Generic.IReadOnlyList<Core.SaveData> OtherSaves() => System.Array.Empty<Core.SaveData>();

    /// <summary>The game in play, as a record knows a game: its trainer's number and the day it began.</summary>
    (int TrainerId, System.DateTime? Started) OwnGame => (0, null);

    /// <summary>
    /// How many turns the battle last fought went on for before the one that ended it (plan 01 · M11, part b; the
    /// original's <c>VAR_TOTAL_TURNS_LAST_BATTLE</c>, which a battle won on its first turn leaves at nought): what the
    /// Pokémon Mansion's maids count.
    /// </summary>
    int BattleTurns { get; }

    /// <summary>
    /// The Battle Factory challenge under way (plan 06 · R18b); null while none is. <see cref="FrontierAct"/> takes two
    /// more words for it: <c>rent</c> opens the six on offer for three to be chosen (the script waits;
    /// <see cref="Entered"/> is their places among the six, in the order chosen), and <c>trade</c> the three in hand and
    /// the beaten trainer's three (<see cref="Entered"/> is the place given up and the place taken, or nothing when no
    /// trade was made).
    /// </summary>
    Models.Frontier.BattleFactoryChallenge? Factory { get; set; }

    // ------------------------------------------------------------------ the Hall of Fame's end (plan 02 · S14)

    /// <summary>
    /// Whether the National Pokédex is the player's (<c>if nationaldex</c>; the original's <c>GetNationalDexEnabled</c>):
    /// the Hall of Fame's script and the scenes after it ask.
    /// </summary>
    bool NationalDex { get; }

    /// <summary>
    /// Writes the game to its save where the player stands, without asking (<c>save</c>; the original's
    /// <c>ClearGame</c> saves so before the credits). False when it couldn't be written.
    /// </summary>
    bool Save();

    // ------------------------------------------------------------------ after the Hall of Fame (plan 02 · S15)

    /// <summary>
    /// Upgrades the Pokédex to the National one, without a word (<c>nationaldex</c>; the original's
    /// <c>SetNationalDexEnabled</c>): Professor Oak's visit to the lab in Sandgem Town. Whether it may be had is the
    /// script's to ask first (<see cref="Models.Pokedex.CanUnlockNational"/>: the Hall of Fame, and every species of
    /// the Sinnoh Pokédex seen).
    /// </summary>
    void GiveNationalDex();

    /// <summary>How many times a team has entered the Hall of Fame (<c>LEAGUE_VICTORIES</c>; the original's <c>GetLeagueVictories</c>).</summary>
    int LeagueVictories { get; }

    /// <summary>
    /// The Battle Hall challenge under way (plan 06 · R18c); null while none is. <see cref="FrontierAct"/> takes two
    /// more words for it: <c>enterhall</c> opens the team for one Pokémon of level 30 or more to be chosen, or two
    /// (the script waits; <see cref="Entered"/> as for <c>enter</c>), and <c>board</c> the board of types (the script
    /// waits; <see cref="Entered"/> is the place on the board of the type chosen).
    /// </summary>
    Models.Frontier.BattleHallChallenge? Hall { get; set; }

    /// <summary>
    /// The Battle Castle challenge under way (plan 06 · R18c); null while none is. <see cref="FrontierAct"/> takes
    /// three more words for it: <c>castleself</c> opens the valet's counter for one's own three (the script waits),
    /// <c>castlefoe</c> the one about the trainer to come, and <c>castlepoints</c> shows the Castle Points over the
    /// field until the script ends.
    /// </summary>
    Models.Frontier.BattleCastleChallenge? Castle { get; set; }

    /// <summary>
    /// Whether the player is riding the Bicycle (the built-in variable <c>CYCLING</c>; the original's
    /// <c>CheckPlayerOnBike</c>, which the Cycling Road's gate keepers ask: plan 01 · M11, part d).
    /// </summary>
    bool Cycling { get; }

    /// <summary>
    /// Which bicycle is ridden (the built-in variable <c>BIKE</c>; Emerald's <c>GetPlayerAvatarBike</c>: 0 none, 1 the
    /// Acro Bike, 2 the Mach Bike; Platinum's own Bicycle is 3). The default reads <see cref="Cycling"/> as Platinum's.
    /// </summary>
    int Bike => Cycling ? 3 : 0;

    /// <summary>
    /// The Seaside Cycling Road's run so far (plan 21 · H12, part k; Emerald's <c>FinishCyclingRoadChallenge</c>): the
    /// frames since it began (sixty a second) and the collisions on the way, at most 100. Nothing where no run is timed.
    /// </summary>
    (int Frames, int Collisions) CyclingRun => (0, 0);

    /// <summary>
    /// Puts the player on the Bicycle or takes them off it (<c>bicycle on</c>, <c>bicycle off</c>; plan 19 · K6:
    /// FireRed's <c>ForcePlayerOntoBike</c>, which the Cycling Road does to whoever comes onto it from its gates). Not
    /// on the water. A host with no field does nothing.
    /// </summary>
    void Ride(bool on) { }

    /// <summary>
    /// The Ride Pager's Pokémon the player rides on land (plan 23, wave 91: Alola's Tauros, Stoutland, Mudsdale), by its
    /// species; null on foot, on the Bicycle or on the water. The built-in variable <c>RIDING</c> is 1 while it is set.
    /// </summary>
    string? Riding => null;

    /// <summary>
    /// Gets on a Ride Pager's Pokémon (<c>ride "Tauros"</c>) or off it (<c>ride off</c>, null). True where it happened: a
    /// ride is refused where the region offers none (<see cref="Data.RegionMode.Ride"/>), where the Pokémon isn't
    /// registered (<see cref="Overworld.RidePager.CanRide"/>), indoors, on the water or in the middle of a step. A host
    /// with no field refuses.
    /// </summary>
    bool RidePokemon(string? species) => false;

    // ------------------------------------------------------------------ the open world's places (plan 32 · OW5)

    /// <summary>
    /// Starts <paramref name="who"/> (<c>player</c>, or someone of the open map, OW6) walking to a marker of the open map
    /// the player is in (<c>walkto</c>), waited for as a walk is (<see cref="Walking"/>); in a hurry the walk is taken to
    /// its end unseen (<see cref="Arrive"/>) and ends on the marker, facing its way, as a walk played does. A host
    /// with no open map can't: the grid's places are tiles (<c>walk</c>).
    /// </summary>
    void WalkTo(string who, string marker, bool fast) => throw new ScriptException($"walkto {who} {marker}: the player is in no open map here; a grid map's walk is 'walk'");

    /// <summary>Puts someone on a marker of the open map, facing its way (<c>placeat</c>). A host with no open map can't.</summary>
    void PlaceAt(string who, string marker) => throw new ScriptException($"placeat {who} {marker}: the player is in no open map here; a grid map's is 'place'");

    /// <summary>Turns someone toward a marker of the open map or toward someone (<c>faceto</c>). A host with no open map can't.</summary>
    void FaceTo(string who, string target) => throw new ScriptException($"faceto {who} {target}: the player is in no open map here; a grid map's is 'face'");

    /// <summary>
    /// Goes to a marker of an open map through a fade, as a door does (<c>warpto</c>), from a grid map or an open one; in
    /// a hurry the player arrives at once. A host that knows no open map can't.
    /// </summary>
    void WarpTo(string map, string marker) => throw new ScriptException($"warpto \"{map}\" {marker}: there is no open map called {map}");

    /// <summary>
    /// Sends the open map's camera to look at a marker (<c>camera look</c>, OW8's orbit camera reads it); returns how
    /// long the move takes, which the runner waits unless the line says <c>nowait</c>. A host with no open map can't.
    /// </summary>
    float LookAt(string marker, float seconds) => throw new ScriptException($"camera look {marker}: the player is in no open map here; a grid map's is 'camera pan'");

    /// <summary>
    /// The Battle Arcade challenge under way (plan 06 · R18d); null while none is. <see cref="FrontierAct"/> takes one
    /// more word for it: <c>arcadeboard</c> opens the game board the challenge has brought up
    /// (<see cref="Models.Frontier.BattleArcadeChallenge.Board"/>) and plays it until its cursor is stopped (the script
    /// waits).
    /// </summary>
    Models.Frontier.BattleArcadeChallenge? Arcade { get; set; }

    /// <summary>
    /// The Battle Dome tournament under way (plan 14 · B8); null while none is. A script's <c>bracket</c> plays it, and
    /// its screen is <see cref="ScriptScreen.Bracket"/> (the counter <c>pick</c> to send two of the three in, the script
    /// waiting; <see cref="Entered"/> is then the two places of the three, and anything else sends the first two).
    /// </summary>
    Models.Frontier.BattleDomeChallenge? Dome { get; set; }

    /// <summary>The round of the Battle Palace or the Battle Arena under way (plan 14 · B9); null while none is. A script's <c>venue</c> plays it.</summary>
    Models.Frontier.BattleVenueChallenge? Venue { get; set; }

    /// <summary>
    /// The challenge of a later battle place under way (plan 37 · FC2: the streak house and the places after it); null
    /// while none is. A script's <c>facility</c> plays it; <see cref="FrontierAct"/> takes one more word for it,
    /// <c>facilityboard</c>, the place's records' board by its number of <see cref="Models.Facilities.FacilityPlaces.Names"/>.
    /// </summary>
    Models.Facilities.StreakChallenge? Facility { get; set; }

    /// <summary>The round of the Battle Pike and of the Battle Pyramid under way (plan 14 · B10); null while none is. A script's <c>pike</c> and <c>pyramid</c> play them.</summary>
    Models.Frontier.BattlePikeChallenge? Pike { get; set; }
    Models.Frontier.BattlePyramidChallenge? Pyramid { get; set; }

    /// <summary>
    /// The World Tournament under way (plan 14 · B11); null while none is. A script's <c>tournament</c> plays it, and its
    /// screen is <see cref="ScriptScreen.Tournament"/>: the counter <c>events</c> for an event to be chosen (<see cref="Answer"/>
    /// the event counted from 1, 0 when backed out of), <c>bracket</c> for the bracket to be looked at.
    /// </summary>
    Models.Tournament? Tournament { get; set; }

    /// <summary>
    /// A Pokémon of the team is out of its ball and walks behind the player as a thing of the map (plan 06 · R16c:
    /// Amity Square's <c>follower_mon</c>, dressed as it by <see cref="Follower.Dress"/>); null for both and it stops
    /// following, where it stands. It is the same follower a partner is (<see cref="TravelWith"/>), and the game has
    /// one at a time.
    /// </summary>
    void WalkWith(NPC? body, Pokemon? pokemon);
}
