using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// A game with no screen for scripts to run in (plan 02 · S1): everything a script starts happens at once.
/// Messages are confirmed as they come and kept in <see cref="Transcript"/>, questions take the next of
/// <see cref="Answers"/> (or <see cref="Decide"/>'s answer, or the first), battles come out as <see cref="Fight"/>
/// says (won, unless told otherwise), and walks put people where they end. It is how a test plays a script, or a
/// whole chapter's, to the end and then looks at the flags, the bag and the team; and with
/// <see cref="ShowsNothing"/> it is where a script that may only set things up is run for a new game.
/// </summary>
public sealed class HeadlessScriptHost : IScriptHost
{
    public StoryState Story { get; }
    public Party Party { get; }
    public Inventory Bag { get; }
    public Poketch Poketch { get; } = new();
    public SafariGame Safari { get; } = new();
    public BugContest BugContest { get; } = new();
    public AthlonRecords Athlon { get; } = new();

    /// <summary>
    /// The place a test's course of the Pokéathlon comes to (plan 15 · E6): 1 to 4, or 0 for the screen backed out
    /// of. A course run earns <see cref="AthlonScore"/> points, recorded under the Practice Course.
    /// </summary>
    public int AthlonPlace { get; set; } = 1;
    public int AthlonScore { get; set; } = 120;
    public void PutInBox(Pokemon pokemon) => Box.Add(pokemon);
    public int SeenInSinnoh { get; set; }
    public int Caught { get; set; }
    public int Money { get; set; } = 3000;
    public string PlayerName { get; set; } = PlayerIdentity.DefaultName(PlayerLook.Boy);
    public PlayerLook PlayerLook { get; set; }
    public string RivalName { get; set; } = PlayerIdentity.DefaultRivalName;

    /// <summary>The region whose badges the script means (plan 18 · W2): the one set, else that of <see cref="Map"/>, else Sinnoh.</summary>
    public string Region
    {
        get => region ?? (Map != null ? RegionDatabase.RegionOfMap(Map.Name)?.Id : null) ?? RegionDatabase.Sinnoh;
        set => region = value;
    }
    private string? region;

    /// <summary>The options' row for field move helpers (plan 12 · Q18); off here, so a test turns it on.</summary>
    public RulesDefault FieldHelpers { get; set; } = RulesDefault.Off;
    public IReadOnlyCollection<FieldMove> Granted => FieldHelp.Granted(FieldHelpers, Ruleset.Current.Preset, Story, Bag, Region);
    /// <summary>
    /// The region the game began in (plan 18 · W5: what <c>CROSSED_IN</c> asks): the one a test sets, else the one the
    /// first crossing left, else the region the script plays in.
    /// </summary>
    public string? BeganInRegion { get; set; }
    public string BeganIn => BeganInRegion ?? Region;

    /// <summary>Whether the player's options let the game say what to do next (plan 12 · Q14); a test turns it off.</summary>
    public bool ShowObjectives { get; set; } = true;
    public bool ShowsObjectives => ShowObjectives;

    /// <summary>The ways to play a test sets (plan 13 · V5); Normal unless it does.</summary>
    public RunRules Rules { get; set; } = RunRules.Normal;

    /// <summary>Where Pokémon go when the team is full.</summary>
    public List<Pokemon> Box { get; } = new();

    /// <summary>The map the player is on; null when a script is run that needs none.</summary>
    public Map? Map { get; set; }
    public (int X, int Y) PlayerTile { get; set; }
    public Direction PlayerFacing { get; set; } = Direction.Down;

    /// <summary>Looks a map up by name for a warp; left out, a warp only moves the player.</summary>
    public Func<string, Map?>? MapNamed { get; set; }

    /// <summary>Everything said, in order: who (null for the game's own voice) and what, with names filled in.</summary>
    public List<(string? Speaker, string Text)> Transcript { get; } = new();

    /// <summary>The answers to the questions to come, by their place among the answers offered.</summary>
    public Queue<int> Answers { get; } = new();

    /// <summary>What to answer once <see cref="Answers"/> has run out; left out, the first answer (yes).</summary>
    public Func<string, IReadOnlyList<string>, int>? Decide { get; set; }

    /// <summary>The questions asked, in order, with the answer each got.</summary>
    public List<(string Question, string Answer)> Asked { get; } = new();

    /// <summary>How a battle comes out: given the trainer (null for a wild Pokémon). Left out, the player wins.</summary>
    public Func<NPC?, BattleOutcome>? Fight { get; set; }

    /// <summary>Which of the three starters is taken when the briefcase is opened.</summary>
    public int StarterChoice { get; set; }

    /// <summary>Everything else that happened, in order, a word and its details each: <c>fanfare FanfareHeal</c>, <c>open Shop</c>, <c>warp PlayerHouse 4 5</c>.</summary>
    public List<string> Log { get; } = new();

    /// <summary>Where chance comes from (Turnback Cave's doors): the same every run unless a test hands its own.</summary>
    public Random Rng { get; set; } = new(0);

    /// <summary>What the game remembers of its wild Pokémon (plan 06 · R13).</summary>
    public SpecialEncounters Encounters { get; set; } = SpecialEncounters.NewGame(new Random(0));

    /// <summary>The honey tree the player faces; null unless a test says (on a map, the one the player faces from the south).</summary>
    public int? HoneyTreeFaced { get => honeyTree ?? (Map != null ? HoneyTrees.Faced(Map, PlayerTile.X, PlayerTile.Y, PlayerFacing) : null); set => honeyTree = value; }
    private int? honeyTree;

    public uint TrainerNumber { get; set; } = 12345;

    public Random Chance => Rng;

    /// <summary>The time of day and the day of the week the script sees (plan 06 · R14a): a weekday morning unless a test says.</summary>
    public TimeOfDay TimeOfDay { get; set; } = TimeOfDay.Morning;
    public DayOfWeek Weekday { get; set; } = DayOfWeek.Monday;

    /// <summary>The clock's hour and minute and the time played, which a television's programme tells (plan 08 · P8).</summary>
    public int Hour { get; set; } = 10;

    /// <summary>Whether the player has come in from outdoors, which Shoal Cave's <c>tide</c> asks (plan 21 · H8, part d): yes unless a test says.</summary>
    public bool CameInFromOutdoors { get; set; } = true;
    public int Minute { get; set; }
    public float PlayTimeSeconds { get; set; }

    /// <summary>What a television's programme may speak of: what this host says, with its clock and time played.</summary>
    public BroadcastFacts Broadcast() => Broadcasts.FactsOf(this) with { Hour = Hour, Minute = Minute, PlayTimeSeconds = PlayTimeSeconds };

    /// <summary>Whether a map on a wall opens (<c>townmap</c>, plan 08 · P8): the region has one, unless a test says not.</summary>
    public bool TownMapOpens { get; set; } = true;

    /// <summary>The Pokémon in the PC's boxes the lottery reads; none unless a test puts some there.</summary>
    public List<Pokemon> Boxes { get; } = new();
    public IEnumerable<Pokemon> Boxed => Boxes;

    public BerryPatches Berries { get; set; } = BerryPatches.NewGame();
    public ApricornBox Apricorns { get; set; } = new();

    /// <summary>
    /// What the bag chooses when a script asks (<c>chooseitem</c>): the item named, or the first in the bag that fits;
    /// an empty name backs out.
    /// </summary>
    public string? ItemChoice { get; set; }

    public string? ChosenItem { get; private set; }

    /// <summary>
    /// The Poffin Case (plan 06 · R14c), empty unless a test fills it. Cooking (<c>poffin cook</c>) only notes that it
    /// opened: no pot is stirred here, as if the player had backed out of the bag at once.
    /// </summary>
    public PoffinCase Poffins { get; } = new();

    /// <summary>The Day Care (plan 06 · R15), empty unless a test fills it.</summary>
    public DayCare DayCare { get; } = new();

    /// <summary>The Fashion Case (plan 06 · R17a), empty unless a test or a script fills it.</summary>
    public Models.SuperContests.FashionCase FashionCase { get; } = new();

    /// <summary>
    /// The place a contest held here comes out at (plan 06 · R17a), 0 for a win: no screen plays, the place is the
    /// test's to say, and what a contest leaves is left as the game leaves it (<see cref="Models.SuperContests.ContestAwards.Conclude"/>).
    /// </summary>
    public int ContestPlacement { get; set; }
    public Models.SuperContests.ContestOutcome? ContestOutcome { get; private set; }

    public void HoldContest(int slot, ContestStat category, Models.SuperContests.ContestRank rank, Models.SuperContests.ContestMode mode)
    {
        Shown("hold a contest");
        NeverInAHurry("a contest");
        Waits();
        ContestOutcome = Models.SuperContests.ContestAwards.Conclude(Party.Members[slot], category, rank, mode, ContestPlacement, FashionCase);
        ContestRecords.Count(ContestOutcome, Party.Members[slot], Array.Empty<int>(), PlayerName, GameClock.Today);
        Log.Add($"supercontest {rank} {category} {mode} {Party.Members[slot].Species.Name} {ContestPlacement}");
    }

    /// <summary>The Seal Case and the contests' records (plan 06 · R17b), empty unless a test or a script fills them.</summary>
    public SealCase SealCase { get; } = new();
    public Models.SuperContests.ContestRecords ContestRecords { get; } = new();

    /// <summary>
    /// The Underground (plan 06 · R16a): its state, empty unless a test or a script fills it; what its commands in the
    /// tunnels come to, which a test may set by name (1 where it sets none); and whose base or flag is spoken of.
    /// </summary>
    public Models.Underground.UndergroundState Underground { get; } = new();
    public Dictionary<string, int> UndergroundResults { get; } = new();
    public string DiggerName { get; set; } = "Bram";

    /// <summary>What the script's dice come to, in turn (each taken within its die's sides); 0 once they run out.</summary>
    public Queue<int> Rolls { get; } = new();

    public int Random(int sides) => Rolls.Count > 0 ? Rolls.Dequeue() % Math.Max(1, sides) : 0;

    /// <summary>
    /// The species the Pokédex has seen, as a test says (<c>random seen</c>, plan 19 · K9, part c): one of these is
    /// drawn by the script's die, and 0 comes back while the list is empty.
    /// </summary>
    public List<string> PokedexSeen { get; } = new();

    public int SpeciesSeenAtRandom()
    {
        var seen = PokedexSeen.Select(name => Data.PokemonDatabase.Get(name)).Where(s => s != null).ToList();
        if (seen.Count == 0) return 0;
        return seen[Random(seen.Count)]!.DexNumber;
    }

    /// <summary>The turns the battle last fought took, as a test says (<see cref="Fight"/> may set it battle by battle).</summary>
    public int BattleTurns { get; set; }

    /// <summary>
    /// Hoenn's secret bases (plan 21 · H10): the decoration PC, empty unless a test or a script fills it, and what the
    /// base's commands in the field come to, which a test may set by name (1 where it sets none).
    /// </summary>
    public List<Models.StoredDecoration> Decorations { get; } = new();
    public Dictionary<string, int> SecretBaseResults { get; } = new();

    public int SecretBaseAct(string what)
    {
        Log.Add($"openbase {what}");
        // (what is only asked, check and kind, may be hurried through)
        if (what is "open" or "enter" or "pc" or "packup")
        {
            NeverInAHurry($"a secret base's '{what}'");
            Waits();
        }
        return SecretBaseResults.GetValueOrDefault(what, 1);
    }

    /// <summary>
    /// Hoenn's contests and Pokéblocks (plan 21 · H11): the records and the case, empty unless a test or a script fills
    /// them. No screen plays here: a contest comes out at the place <see cref="AppealPlacement"/> says (0 for a win) with
    /// <see cref="AppealPoints"/> points, and what it leaves is left as the game leaves it
    /// (<see cref="Models.Contest.HoennContestRules.Conclude(ContestStat, Models.Contest.AppealRank, Pokemon, int, int, int)"/>);
    /// the blender is opened and put away at once, as if the player had backed out of the bag.
    /// </summary>
    public Models.Contest.HoennContestRecords HoennContests { get; } = new();
    public PokeblockCase PokeblockCase { get; } = new();
    public int AppealPlacement { get; set; }
    public int AppealPoints { get; set; } = 600;
    public Models.Contest.AppealOutcome? AppealOutcome { get; private set; }
    public bool PokeblockKept { get; private set; }

    public void HoldAppeals(int slot, ContestStat category, Models.Contest.AppealRank rank)
    {
        Shown("hold a Hoenn contest");
        NeverInAHurry("a Hoenn contest");
        Waits();
        var pokemon = Party.Members[slot];
        AppealOutcome = Models.Contest.HoennContestRules.Conclude(category, rank, pokemon, AppealPlacement, AppealPoints);
        Models.Contest.HoennContestRules.Record(AppealOutcome, pokemon, PlayerName, HoennContests);
        Log.Add($"appeals {rank} {category} {pokemon.Species.Name} {AppealPlacement}");
    }

    public void BlendPokeblocks(int others)
    {
        Shown("blend Pokéblocks");
        NeverInAHurry("the Berry Blender");
        Waits();
        PokeblockKept = false;
        Log.Add($"pokeblock blend {others}");
    }

    public int UndergroundAct(string what, NPC? subject)
    {
        Log.Add($"underground {what}");
        NeverInAHurry($"the Underground's '{what}'");
        if (what is "down" or "up" or "mine" or "hiker" or "pc" or "bag") Waits();
        return UndergroundResults.GetValueOrDefault(what, 1);
    }

    /// <summary>
    /// Fossils, the Game Corner, the Villa and Pal Park (plan 06 · R16b): the coins, the six and the records, the
    /// Villa's records as a test sets them, and what a sitting at a machine is to come to (the coins it leaves, set
    /// by a test; the case as it is otherwise).
    /// </summary>
    public Models.Pastimes Pastimes { get; } = new();
    public Models.VillaRecords VillaRecords { get; set; }

    public int? SlotsLeave { get; set; }

    public int PastimeAct(string what, int number, NPC? subject)
    {
        Log.Add($"pastime {what} {number}");
        NeverInAHurry($"a pastime's '{what}'");
        switch (what)
        {
            case "slots":
                if (SlotsLeave is { } coins) Pastimes.Coins.Set(coins);
                return 1;
            case "boxroom":
                return (NationalDex ? Models.PcBoxes.NationalBoxCount : Models.PcBoxes.BoxCount) * Models.PcBoxes.BoxSize - Boxes.Count;
            case "store":
                if (!Pastimes.PalPark.Stocked) return 0;
                Boxes.AddRange(Pastimes.PalPark.TakeSix("Pal Park", GameClock.Today));
                return 1;
            default:
                return 1;
        }
    }

    /// <summary>The Pokémon hatched so far by <c>hatch</c>, in order: here an Egg hatches at once, with no scene.</summary>
    public List<Pokemon> Hatched { get; } = new();

    public bool Hatch()
    {
        Shown("hatch an Egg");
        NeverInAHurry("an Egg hatching");
        if (Party.Members.FirstOrDefault(p => p.IsEgg && p.EggCycles == 0) is not { } egg) return false;
        Breeding.Hatch(egg, Map?.DisplayNameAt(PlayerTile.X, PlayerTile.Y), GameClock.Today);
        Hatched.Add(egg);
        Log.Add($"hatch {egg.Species.Name}");
        return true;
    }

    /// <summary>The species a script told the Pokédex of (<c>seen</c>), in order.</summary>
    public List<string> SeenSpecies { get; } = new();

    public void Seen(PokemonSpecies species)
    {
        if (!SeenSpecies.Contains(species.Name)) SeenSpecies.Add(species.Name);
        Log.Add($"seen {species.Name}");
    }

    public void ContactAdded(Models.PhoneContact contact) => Log.Add($"addcontact {contact.Id}");

    /// <summary>What a walk or a placement ran into: off the map, or into something solid.</summary>
    public List<string> Problems { get; } = new();

    /// <summary>
    /// When set, the script may only change what the story remembers: anything that would be seen or heard, or
    /// would wait for the player, throws.
    /// </summary>
    public bool ShowsNothing { get; set; }

    /// <summary>
    /// When set, the host keeps a script waiting as the game would: it turns <see cref="Busy"/> at everything the
    /// player has to see through (text, a question, a battle, a screen, a fade) and <see cref="Walking"/> at a
    /// walk, until the test clears them. It is how a test looks at a script halfway.
    /// </summary>
    public bool Pauses { get; set; }

    private void Waits()
    {
        if (Pauses) Busy = true;
    }

    /// <summary>
    /// Whether a battle needs a Pokémon of the player's able to fight, as it does in the game (<see cref="IScriptHost.CanBattle"/>).
    /// Off unless a test turns it on, so a test of the language battles with an empty team; a chapter played through
    /// turns it on.
    /// </summary>
    public bool NeedsPokemon { get; set; }

    public bool CanBattle => !NeedsPokemon || Party.HasUsablePokemon;

    public HeadlessScriptHost(StoryState? story = null, Party? party = null, Inventory? bag = null)
    {
        Story = story ?? new StoryState();
        Party = party ?? new Party();
        Bag = bag ?? new Inventory();
    }

    private void Shown(string what)
    {
        if (ShowsNothing) throw new ScriptException($"A script that may only set things up tried to {what}.");
    }

    // ------------------------------------------------------------------ a scene skipped (plan 12 · Q20)

    /// <summary>True while the runner hurries through a scene: what is only shown or heard is dropped (<see cref="Dropped"/>, <see cref="Unread"/>).</summary>
    public bool Skipping { get; private set; }

    /// <summary>How many hurries began: one for a scene skipped whole, one more for each thing in it that waited for the player.</summary>
    public int Skips { get; private set; }

    /// <summary>The lines a hurry put away unread, in order: who and what, as <see cref="Transcript"/> has the lines that were read.</summary>
    public List<(string? Speaker, string Text)> Unread { get; } = new();

    /// <summary>What a hurry dropped beside its lines, a word and its details each, as <see cref="Log"/> has what happened: <c>sound door_open</c>, <c>emote rival Exclaim</c>.</summary>
    public List<string> Dropped { get; } = new();

    /// <summary>The scenes this host's machine has seen to their end (the game's is <c>GameSettings.ScenesSeen</c>): what a second game may skip.</summary>
    public HashSet<string> MachineScenes { get; } = new(StringComparer.Ordinal);

    /// <summary>The seconds a hurry let pass for whoever was walking, in order, and how often it had its walks arrive.</summary>
    public List<float> Elapsed { get; } = new();

    public int Arrivals { get; private set; }

    // (Here a walk is over as it is told, so there is nobody to move: both are only written down)
    public void Elapse(float seconds) => Elapsed.Add(seconds);

    public void Arrive()
    {
        Arrivals++;
        Walking = false;
    }

    public void BeginSkipping()
    {
        Skipping = true;
        Skips++;
        // A test's hold on a walk is let go: nobody is waited for in a hurry
        Walking = false;
    }

    public void EndSkipping() => Skipping = false;

    public void SceneBegins(string key) => Log.Add($"scene {key}");

    public void SceneEnds(string key, bool seen)
    {
        Log.Add(seen ? "endscene" : "scene over, unseen");
        if (seen) MachineScenes.Add(key);
    }

    /// <summary>Notes what happened; in a hurry, something only shown or heard is dropped instead (true), and the caller does no more with it.</summary>
    private bool Passing(string entry)
    {
        if (Skipping) Dropped.Add(entry);
        else Log.Add(entry);
        return Skipping;
    }

    /// <summary>
    /// Something that waits for the player, or that no host ends at once, was reached in a hurry: its command must
    /// stop one (<see cref="ScriptRunner.StopsAHurry"/>), and a test that plays the game's scenes both ways fails on this.
    /// </summary>
    private void NeverInAHurry(string what)
    {
        if (Skipping) Problems.Add($"a hurry reached {what}: the command that does it must stop a hurry (ScriptRunner.StopsAHurry)");
    }

    // What a scene leaves on the screen once it is over, which a skip must leave too: the script's black screen, the
    // song playing, the camera kit (what the camera looks at, its lens, the bars), the effects held, a PC left on and
    // the balls on the Hall of Fame's machine

    /// <summary>Whether the script's fade has the field black.</summary>
    public bool ScreenBlack { get; private set; }

    /// <summary>The song a script asked for last: a song's id, <c>area</c> for the place's own, <c>stop</c> for silence; null before any.</summary>
    public string? Song { get; private set; }

    /// <summary>What the camera looks at: <c>player</c>, a tile (<c>31,26</c>) or whoever it keeps on.</summary>
    public string CameraOn { get; private set; } = "player";

    /// <summary>The lens's factor: 1 unless a scene zoomed.</summary>
    public float Lens { get; private set; } = 1f;

    /// <summary>Whether the cinema bars are in.</summary>
    public bool Bars { get; private set; }

    /// <summary>The effects held on their tiles: the kind and the tile each.</summary>
    public SortedSet<string> HeldEffects { get; } = new(StringComparer.Ordinal);

    /// <summary>Whether a PC's screen was left on, and the balls left on the Hall of Fame's machine.</summary>
    public bool PcOn { get; private set; }
    public int MachineBalls { get; private set; }

    private string Named(string text) => PokemonPlatinumEngine.UI.Kit.RichText.Plain(PlayerIdentity.Fill(text, PlayerName, PlayerLook, RivalName));

    // ------------------------------------------------------------------ the field

    public NPC? FindNpc(string name, string? place) => Map?.FindPerson(name, place);

    public (int X, int Y) TileOf(NPC? who) => who == null ? PlayerTile : (who.GridX, who.GridY);

    public Direction FacingOf(NPC? who) => who?.Facing ?? PlayerFacing;

    public void Face(NPC? who, Direction direction)
    {
        if (who == null) PlayerFacing = direction;
        else who.Facing = direction;
    }

    public void Place(NPC? who, int x, int y, Direction? facing)
    {
        Shown("move someone");
        Check(who, x, y, "is put");
        if (who == null) PlayerTile = (x, y);
        else (who.GridX, who.GridY) = (x, y);
        if (facing is { } direction) Face(who, direction);
    }

    public void SetVisible(NPC who, bool visible)
    {
        Shown("show or hide someone");
        who.Forced = visible;
        Map?.ApplyPresence(Story.Has);
    }

    public void Walk(NPC? who, IReadOnlyList<ScriptStep> steps, bool fast)
    {
        Shown("walk someone");
        if (Pauses && !Skipping) Walking = true;
        var (x, y) = TileOf(who);
        Direction? facing = null;
        foreach (var step in steps)
        {
            // A step on the spot stays on the tile, and a step back keeps the facing (plan 11 · C11)
            if (step.Turns) facing = step.Way;
            if (!step.Moves) continue;
            var (dx, dy) = FieldMovement.Delta(step.Way);
            x += dx;
            y += dy;
            Check(who, x, y, "walks");
        }
        // In a hurry someone of the map is put at the end of their walk by the game's own walk, taken to its end
        // unseen as the field's host takes it (plan 12 · Q20): where that and the steps counted here part, a skip
        // and a played scene part too
        if (Skipping && who != null) new NpcWalk(who, steps, fast).Finish();
        else
        {
            if (who == null) PlayerTile = (x, y);
            else (who.GridX, who.GridY) = (x, y);
            if (facing is { } way) Face(who, way);
        }
        if (steps.Any(s => s.Kind != ScriptStepKind.Walk))
            Log.Add($"walk {who?.Name ?? "player"} " + string.Join(" ", steps.Select(s => s.Kind switch
            {
                ScriptStepKind.OnSpot => "onspot " + s.Way.ToString().ToLowerInvariant(),
                ScriptStepKind.Back => "back " + s.Way.ToString().ToLowerInvariant(),
                _ => s.Way.ToString().ToLowerInvariant()
            })));
    }

    /// <summary>The face a script has the player hold (plan 11 · C11); someone of the map holds theirs in <see cref="NPC.Mood"/>.</summary>
    public Mood PlayerMood { get; private set; }

    public void Act(NPC? who, Gesture gesture, int times)
    {
        Shown("make someone act");
        Passing($"act {who?.Name ?? "player"} {gesture}" + (times > 1 ? $" {times}" : ""));
    }

    public void Mood(NPC? who, Mood mood)
    {
        Shown("give someone a face");
        if (who == null) PlayerMood = mood;
        else who.Mood = mood;
        Log.Add($"mood {who?.Name ?? "player"} {mood}");
    }

    /// <summary>The stance a script set the player in (plan 11 · C7), standing when none is.</summary>
    public Stance PlayerStance { get; private set; }

    public void Pose(NPC? who, Stance stance)
    {
        if (who == null) PlayerStance = stance;
        else who.Posed = stance;
        Log.Add($"pose {who?.Name ?? "player"} {stance}");
    }

    private void Check(NPC? who, int x, int y, string does)
    {
        if (Map == null) return;
        string name = who?.Name ?? "the player";
        if (!Map.InBounds(x, y)) Problems.Add($"{name} {does} off the map at {x},{y}");
        // Whoever the story floats at a height of their own (a Pokémon over the Distortion World's nothing) is over the
        // ground, not on it; and someone a script sends across a gap of nothing crosses it, as the original's people
        // jump the Distortion World's gaps (plan 02 · S12). The player takes their own steps, and is held to the ground.
        else if (who is { Level: not null } || (who != null && Map.GetGroundTile(x, y) == TileType.Void)) return;
        else if (Map.IsSolid(x, y)) Problems.Add($"{name} {does} into something solid at {x},{y}");
    }

    /// <summary>Never, unless a test holds the script up to see where it waits for a walk.</summary>
    public bool Walking { get; set; }

    public void Emote(NPC? who, EmoteBubble bubble, float seconds)
    {
        Shown("show a bubble");
        Passing($"emote {who?.Name ?? "player"} {bubble}");
    }

    public float Camera(CameraMove move, int x, int y, float seconds, float factor = 1f, NPC? who = null, IReadOnlyList<(int X, int Y)>? path = null)
    {
        Shown("move the camera");
        string Number(float value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        switch (move)
        {
            case CameraMove.Pan:
                Log.Add($"camera Pan {x} {y}");
                CameraOn = $"{x},{y}";
                return seconds;
            case CameraMove.Zoom:
                Log.Add($"camera Zoom {Number(factor)}");
                Lens = factor;
                return seconds;
            case CameraMove.Follow:
                Log.Add($"camera Follow {who?.Name ?? "player"}");
                CameraOn = who?.Name ?? "player";
                return seconds;
            case CameraMove.Path:
            {
                // Everything happens at once here: the path's time is told from its own tiles, as if the camera began at the first
                var tiles = path ?? Array.Empty<(int X, int Y)>();
                Log.Add($"camera Path {string.Join(" ", tiles.Select(t => $"{t.X} {t.Y}"))} at {Number(factor)}");
                if (tiles.Count > 0) CameraOn = $"{tiles[^1].X},{tiles[^1].Y}";
                float length = 0f;
                for (int i = 1; i < tiles.Count; i++)
                    length += MathF.Sqrt((tiles[i].X - tiles[i - 1].X) * (tiles[i].X - tiles[i - 1].X) + (tiles[i].Y - tiles[i - 1].Y) * (tiles[i].Y - tiles[i - 1].Y));
                return CameraRig.PathSeconds(length, factor);
            }
            case CameraMove.Release:
                // Everything the kit was told is back with it
                Log.Add($"camera {move}");
                CameraOn = "player";
                Lens = 1f;
                Bars = false;
                return seconds;
            default:
                // (A shake passes: a hurry drops it)
                Passing($"camera {move}");
                return 0f;
        }
    }

    public void Letterbox(bool on, float seconds)
    {
        Shown("slide the bars");
        Log.Add(on ? "letterbox on" : "letterbox off");
        Bars = on;
    }

    public void SlowMotion(float share, float seconds)
    {
        Shown("slow the field");
        Passing($"slowmo {share.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    }

    public void Effect(SceneEffect kind, int x, int y, float seconds, Direction? way, bool hold)
    {
        Shown("show an effect");
        string entry = $"effect {kind} {x} {y}" + (way is { } w ? $" {w}" : "") + (hold ? " hold" : "");
        // An effect that passes is dropped by a hurry; one that is held is lit all the same, since it is there afterwards
        if (!hold) Passing(entry);
        else
        {
            Log.Add(entry);
            HeldEffects.Add($"{kind} {x} {y}");
        }
    }

    public void StopEffect(int x, int y)
    {
        Shown("stop an effect");
        Log.Add($"effect stop {x} {y}");
        HeldEffects.RemoveWhere(held => held.EndsWith($" {x} {y}", StringComparison.Ordinal));
    }

    // The original's set pieces (plan 09 · L12): everything happens at once here, so each is a line of the log with
    // the count it was played with, and the time it would take is told as the game tells it

    public float Anim(SetPiece piece, int count)
    {
        Shown("play a set piece");
        bool counted = piece is not (SetPiece.PcOn or SetPiece.PcOff);
        string entry = $"anim {SetPieces.WordsOf(piece)}" + (counted ? $" {count}" : "");
        // What a piece leaves behind is left by a hurry too: a PC's screen on or dark, the balls in the Hall of Fame's
        // sockets. A healing machine's tray is empty again and a lift's ride is over, so a hurry drops those
        switch (piece)
        {
            case SetPiece.PcOn:
            case SetPiece.PcOff:
                PcOn = piece == SetPiece.PcOn;
                Log.Add(entry);
                break;
            case SetPiece.HallOfFame:
                MachineBalls = Math.Clamp(count, 0, SetPieces.MostBalls);
                Log.Add(entry);
                break;
            default:
                Passing(entry);
                break;
        }
        return SetPieces.Length(piece, count);
    }

    public float Shake(NPC? who, int times)
    {
        Shown("shake someone");
        Passing($"shake {who?.Name ?? "player"} {times}");
        return SetPieces.ShakeLength(times);
    }

    public float Flicker(NPC? who, int times)
    {
        Shown("make someone flicker");
        Passing($"flicker {who?.Name ?? "player"} {times}");
        return SetPieces.FlickerLength(times);
    }

    // ------------------------------------------------------------------ what a script waits for

    /// <summary>Never, unless a test holds the script up to see where it waits for the player.</summary>
    public bool Busy { get; set; }

    /// <summary>How many times the text box was opened: lines said one after another are one talk.</summary>
    public int Talks { get; private set; }

    public void Say(string? speaker, IReadOnlyList<string> lines)
    {
        Shown("say something");
        string? who = speaker != null ? Named(speaker) : null;
        // In a hurry the lines are put away unread: no box opens and nothing is waited for
        if (Skipping)
        {
            foreach (string line in lines) Unread.Add((who, Named(line)));
            return;
        }
        Waits();
        Talks++;
        foreach (string line in lines) Transcript.Add((who, Named(line)));
    }

    public void Ask(string? speaker, string question, IReadOnlyList<string> answers, int cancel)
    {
        Shown("ask something");
        NeverInAHurry("a question");
        Waits();
        string asked = Named(question);
        Transcript.Add((speaker != null ? Named(speaker) : null, asked));
        int answer = Answers.Count > 0 ? Answers.Dequeue() : Decide?.Invoke(asked, answers) ?? 0;
        Answer = Math.Clamp(answer, 0, answers.Count - 1);
        Asked.Add((asked, answers[Answer]));
    }

    public int Answer { get; private set; }

    public void Battle(NPC trainer, NPC? second, Trainer? partner, bool mayLose, bool first, BattleTerms? terms = null)
    {
        Shown("start a battle");
        NeverInAHurry("a battle");
        Waits();
        Outcome = Fight?.Invoke(trainer) ?? BattleOutcome.Won;
        string with = (second != null ? $" and {second.TrainerData?.Id}" : "") + (partner != null ? $" with {partner.Id}" : "") + (first ? " first" : "")
            + (terms is { Inverse: true } ? " inverse" : "")
            // A Master Trainer's battle (plan 14 · B14): the one Pokémon put forward, and the bag closed
            + (terms?.Alone is { } alone ? $" alone {alone.Species.Name}" : "") + (terms is { NoBattleItems: true } ? " noitems" : "");
        Log.Add($"battle {trainer.TrainerData?.Id}{with} {Outcome}");
        if (Outcome == BattleOutcome.Won)
        {
            foreach (var foe in second == null ? new[] { trainer } : new[] { trainer, second })
            {
                if (foe.TrainerData is not { } beaten) continue;
                Money += beaten.PrizeMoney;
                Story.Defeat(beaten.Id);
                // Two people who battle as one trainer are beaten together
                foreach (var npc in (Map?.Everyone ?? new[] { foe }).Append(foe))
                    if (npc == foe || (beaten.Id.Length > 0 && npc.TrainerData?.Id == beaten.Id)) npc.FinishBattle(true);
            }
        }
        else if (Outcome == BattleOutcome.Lost)
        {
            Party.HealAll();
        }
    }

    public void WildBattle(Pokemon wild, BattleKind kind, bool cannotFlee, LessonCatcher? catcher = null)
    {
        if (catcher != null) Log.Add($"catcher {catcher.Name} {catcher.Own.Species.Name} {catcher.Own.Level}");
        Shown("start a battle");
        NeverInAHurry("a battle");
        Waits();
        // The lesson is the assistant's battle: their ball always catches, and what they catch is theirs
        Outcome = kind == BattleKind.CatchingLesson ? BattleOutcome.Caught : Fight?.Invoke(null) ?? BattleOutcome.Won;
        Log.Add($"{(kind == BattleKind.CatchingLesson ? "catchinglesson" : "wildbattle")} {wild.Species.Name} {wild.Level}{(cannotFlee ? " nofleeing" : "")} {Outcome}");
        if (Outcome == BattleOutcome.Caught && kind != BattleKind.CatchingLesson) GivePokemon(wild);
        else if (Outcome == BattleOutcome.Lost) Party.HealAll();
    }

    public BattleOutcome Outcome { get; private set; }

    /// <summary>
    /// The place of the team's Pokémon chosen when a script asks for one (<c>choosepokemon</c>); 255 backs out. A place
    /// whose Pokémon a species named on the line refuses is handed back as it is, and the runner refuses it, as the
    /// game's screen would never let it be chosen (plan 14 · B14).
    /// </summary>
    public int PokemonChoice { get; set; }

    /// <summary>Whether a stone panel of the Ruins of Alph a script opens is put back together (plan 20 · J5); left, it is not.</summary>
    public bool PuzzleSolved { get; set; } = true;

    /// <summary>
    /// The phrase given when a script asks for one (<c>phrase</c>, plan 08 · P9), its two words as their keys
    /// (<see cref="PhraseGroups"/>); null backs out of the picker, as B on the phrase would. A phrase that isn't whole
    /// is backed out of too, since the picker's OK takes only a whole one.
    /// </summary>
    public IReadOnlyList<string>? PhraseChoice { get; set; } = DefaultPhrase;

    /// <summary>The phrase a test gives unless it says otherwise: two of our own words.</summary>
    public static IReadOnlyList<string> DefaultPhrase => new[] { "feelings:happy", "small:together" };

    public void Open(ScriptScreen screen, NPC? subject, string? counter = null)
    {
        Shown("open a screen");
        NeverInAHurry($"a screen ({screen})");
        Waits();
        Log.Add(screen == ScriptScreen.Shop && (counter ?? subject?.Mart) is { } mart ? $"open {screen} {mart}"
            : screen == ScriptScreen.Wardrobe && counter != null ? $"open {screen} {counter}"
            : screen == ScriptScreen.ChoosePokemon && counter != null ? $"open {screen} {counter}"
            : screen == ScriptScreen.AlphPuzzle ? $"open {screen} {counter} {(PuzzleSolved ? "solved" : "left")}"
            : screen == ScriptScreen.Phrase ? $"open {screen} {counter} {(PhraseGroups.IsWhole(PhraseChoice) ? string.Join(" ", PhraseChoice!) : "none")}"
            : $"open {screen}");
        Answer = 0;
        // The team's Pokémon a script asks to have chosen: the one the test says (none, 255, to back out)
        if (screen == ScriptScreen.ChoosePokemon) Answer = PokemonChoice;
        // A panel of the Ruins of Alph: put together, unless the test says it was left (plan 20 · J5)
        if (screen == ScriptScreen.AlphPuzzle) Answer = PuzzleSolved ? 1 : 0;
        // A map on a wall (plan 08 · P8): it opens where the region has one, unless the test says it has none
        if (screen == ScriptScreen.TownMap) Answer = TownMapOpens ? 1 : 0;
        // A phrase (plan 08 · P9): the one the test gives is kept under the script's name, as the game's picker keeps it
        if (screen == ScriptScreen.Phrase && PhraseGroups.IsWhole(PhraseChoice) && counter is { Length: > 0 } phrase)
        {
            Story.SetPhrase(phrase, PhraseChoice!);
            Answer = 1;
        }
        if (screen == ScriptScreen.ChooseItem)
        {
            string what = counter ?? "berries";
            ChosenItem = ItemChoice is { } named
                ? (named.Length > 0 && Bag.AllItems.Any(s => s.Name == named) ? named : null)
                : Bag.AllItems.FirstOrDefault(s => UI.BagScreen.Fits(what, s.Data))?.Name;
            Answer = ChosenItem != null ? 1 : 0;
            Log[^1] = $"open {screen} {what} {ChosenItem ?? "none"}";
        }
        // The way to the next region (plan 18 · W4, part b): played as the game plays it, so tests cross
        if (screen == ScriptScreen.Travel) Travel(subject);
        // The crossing itself, over the dark (plan 18 · W5)
        if (screen == ScriptScreen.Crossing) Cross();
        // The Pokéathlon (plan 15 · E6): the place the test says, the points recorded as a course run would
        if (screen == ScriptScreen.Athlon)
        {
            Answer = AthlonRun.CanEnter(Party) ? Math.Clamp(AthlonPlace, 0, AthlonRun.TeamCount) : 0;
            if (Answer > 0) Athlon.Record(AthlonCourses.Practice.Key, AthlonScore, Answer);
            Log[^1] = $"open {screen} {Answer}";
        }
        // A region's rival named: the name the test gives, or the region's own (plan 18 · W4, part b)
        if (screen == ScriptScreen.NameRival)
        {
            var here = RegionDatabase.Get(Region) ?? RegionDatabase.First;
            RivalName = PlayerIdentity.Clean(RivalNameGiven) is { Length: > 0 } given ? given : here.RivalFor(PlayerLook) ?? PlayerIdentity.DefaultRivalName;
            RivalNames[here.Id] = RivalName;
            Log[^1] = $"open {screen} {here.Id} {RivalName}";
            Answer = 1;
        }
        if (screen != ScriptScreen.Starter) return;

        // As the game does: the Pokémon chosen joins the team (the player's first, since plan 02 · S4)
        // From the briefcase of the region the script plays in (Oak's table in Kanto, plan 19 · K3)
        var three = RegionDatabase.Get(Region)?.Starters is { Count: > 0 } own ? own : StoryState.Starters;
        int choice = Math.Clamp(StarterChoice, 0, three.Count - 1);
        // The card's slot is what the story goes by; in a randomised run the Pokémon given is the species the slot goes to (plan 13 · V6)
        string slot = three[choice];
        string species = Randomizer.For(Rules)?.Starters(three)[choice] ?? slot;
        Give(new Pokemon(PokemonDatabase.Get(species)!, 5), theStarter: true);
        Story.ChooseStarter(slot);
        Answer = choice;
    }

    /// <summary>The name a test gives a region's rival at <c>namerival</c>; null (or empty) takes the region's own.</summary>
    public string? RivalNameGiven { get; set; }

    /// <summary>Each region's rival as the player named them, by the region's id (plan 18 · W4, part b).</summary>
    public Dictionary<string, string> RivalNames { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The regions crossed into, in order (plan 18 · W4, part b).</summary>
    public List<string> Crossings { get; } = new();

    /// <summary>
    /// The attendant's travel as the game plays it (plan 18 · W4, part b; W5): with no way on from the map's region the
    /// script hears 0 and says its own lines; otherwise the attendant says how things stand and the script hears 2 once
    /// the region's Hall of Fame is entered and the next region is built, when <c>common.Crossing</c> asks and crosses
    /// (<see cref="Cross"/>), and 1 before.
    /// </summary>
    private void Travel(NPC? subject)
    {
        if (RegionDatabase.Get(Region) is not { } here || RegionDatabase.LinkFrom(here.Id) is not { } link) return;
        var check = RegionDatabase.CheckTravel(link, Story);
        Answer = check == TravelCheck.Ready ? 2 : 1;
        Talks++;
        foreach (string line in RegionDatabase.AttendantLines(link, check)) Transcript.Add((subject != null ? Named(Text.PersonName(subject.LinesPlace, subject.LinesKey, subject.Name)) : null, Named(line)));
    }

    /// <summary>
    /// <c>travel go</c> as the game plays it (plan 18 · W5): the vessel's sound and its line over the dark, then the
    /// crossing: every Pokémon given the region it leaves, the team departing by <see cref="Crossing.CanTake"/> (whole,
    /// under option B), and the player put down on the next region's arrival, with its rival the one <c>{rival}</c> names.
    /// Where no way leads on now nothing happens.
    /// </summary>
    private void Cross()
    {
        if (RegionDatabase.Get(Region) is not { } here || RegionDatabase.LinkFrom(here.Id) is not { } link
            || RegionDatabase.CheckTravel(link, Story) != TravelCheck.Ready) return;
        Log.Add($"sound {RegionDatabase.CrossingSound(link)}");
        Transcript.Add((null, Named(RegionDatabase.VoyageLine(link))));
        BeganInRegion ??= here.Id;

        var to = RegionDatabase.Get(link.To)!;
        Log.Add($"travel {to.Id}");
        CrossTo(here, to, to.ArrivalSpot!);
    }

    /// <summary>
    /// Into another region at a place of it, as the game's <c>CrossTo</c> goes (plan 18 · W9: the vessel's crossing and
    /// a flight to a town of another region): the Pokémon given the region they leave, the team departing by
    /// <see cref="Crossing.CanTake"/>, the region noted in <see cref="Crossings"/>, and the player put down there.
    /// </summary>
    private void CrossTo(Region here, Region to, MapSpot spot)
    {
        Crossing.Belong(Party.Members.Concat(Boxes), here.Id);
        foreach (var member in Party.Members.ToList())
        {
            if (Crossing.CanTake(member, to, Story)) continue;
            Party.Remove(member);
            Boxes.Add(member);
        }
        Crossings.Add(to.Id);
        if (MapNamed?.Invoke(spot.Map) is { } target)
        {
            Map = target;
            Map.ForgetForced();
            Map.ApplyPresence(Story.Has);
        }
        PlayerTile = (spot.X, spot.Y);
        PlayerFacing = spot.Facing;
        if (region != null) region = to.Id;
        RivalName = RivalNames.TryGetValue(to.Id, out var named) ? named : to.RivalFor(PlayerLook) ?? PlayerIdentity.DefaultRivalName;
    }

    /// <summary>The Hall of Fame the script entered teams into.</summary>
    public HallOfFame HallOfFame { get; } = new();

    /// <summary>The Journal the script wrote in.</summary>
    public Journal Journal { get; } = new();

    public void Note(JournalEvent line)
    {
        if (Journal.Today == null) Journal.TakenUp(GameClock.Today, "");
        Journal.Tell(line);
        Log.Add($"journal {line.Kind} {line.Subject}");
    }

    public void EnterHallOfFame()
    {
        HallOfFame.Enter(Party, p => p.OriginalTrainer is { } mark ? (mark.Name, mark.Id) : (PlayerName, 0), GameClock.Today);
        Log.Add("halloffame");
    }

    public bool Trade(string trade, int slot)
    {
        bool done = NpcTrades.Get(trade) is { } t && NpcTrades.Trade(t, Party, slot, GameClock.Today) != null;
        Log.Add($"trade {trade} {(done ? "done" : "refused")}");
        return done;
    }

    public bool GivePokemon(Pokemon pokemon) => Give(pokemon, theStarter: false);

    /// <summary>A Pokémon onto the team, or to the box when the team is full or the run's Monotype keeps it off it (the player's starter is exempt, plan 13 · V5).</summary>
    private bool Give(Pokemon pokemon, bool theStarter)
    {
        pokemon.Met(Map?.DisplayNameAt(PlayerTile.X, PlayerTile.Y), GameClock.Today);
        ShinyCharm.Roll(pokemon, Bag, Dice.New());
        if (!theStarter && !Rules.MayCarry(pokemon, Story.PlayerStarter))
        {
            Box.Add(pokemon);
            Log.Add($"givepokemon {pokemon.Species.Name} kept out by the run's type");
            return false;
        }
        if (Party.Add(pokemon)) return true;
        Box.Add(pokemon);
        return false;
    }

    public void Warp(string map, int x, int y, Direction? facing)
    {
        Shown("go somewhere else");
        // (In a hurry the player arrives at once, with no fade to see through)
        if (!Skipping) Waits();
        Log.Add($"warp {map} {x} {y}");
        if (MapNamed?.Invoke(map) is { } target)
        {
            Map = target;
            Map.ForgetForced();
            Map.ApplyPresence(Story.Has);
        }
        PlayerTile = (x, y);
        if (facing is { } direction) PlayerFacing = direction;
    }

    /// <summary>
    /// <c>travel to</c> as the game plays it (plan 20 · J13, part d): a tile of another region's map is a crossing there
    /// (<see cref="CrossTo"/>), one of this region's a warp.
    /// </summary>
    public void CrossInto(string map, int x, int y, Direction? facing)
    {
        var here = RegionDatabase.Get(Region);
        if (here == null || RegionDatabase.RegionOfMap(map) is not { } to || string.Equals(to.Id, here.Id, StringComparison.OrdinalIgnoreCase))
        {
            Warp(map, x, y, facing);
            return;
        }
        Shown("cross into another region");
        if (!Skipping) Waits();
        BeganInRegion ??= here.Id;
        Log.Add($"travel to {to.Id} {map} {x} {y}");
        CrossTo(here, to, new MapSpot(map, x, y, facing ?? Direction.Down));
    }

    public void Fade(bool toBlack, float seconds)
    {
        Shown("fade the screen");
        // (In a hurry the screen is black, or the field again, at once)
        if (!Skipping) Waits();
        Log.Add(toBlack ? "fade out" : "fade in");
        ScreenBlack = toBlack;
    }

    // ------------------------------------------------------------------ field moves

    /// <summary>How the player travels: on foot until a script has them surf.</summary>
    public TravelMode PlayerMode { get; set; }

    public void UseMove(FieldMove move, Pokemon user, NPC? subject)
    {
        Shown("use a field move");
        // (A hurry begun while the move's line was being read finds its cut-in still to come: the move is used
        // without one, and nothing is waited for)
        if (!Skipping) Waits();
        Log.Add($"usemove {FieldMoveRules.MoveName(move)} {user.DisplayName}{(subject != null ? " on " + subject.Name : "")}");
    }

    public bool Surf()
    {
        Shown("go out onto the water");
        NeverInAHurry("a step out onto the water");
        var (x, y) = PlayerTile;
        if (Map != null && !FieldMovement.CanStartSurf(Map, x, y, PlayerFacing, new Walker(PlayerMode, Map.HeightAt(x, y))))
        {
            Log.Add("surf nowhere");
            return false;
        }
        var (dx, dy) = FieldMovement.Delta(PlayerFacing);
        PlayerTile = (x + dx, y + dy);
        PlayerMode = TravelMode.Surfing;
        Log.Add("surf");
        return true;
    }

    public bool Dive()
    {
        Shown("dive");
        NeverInAHurry("a dive");
        var (x, y) = PlayerTile;
        // With no map the dive is taken on trust: down from the water, up from the floor
        if (Map == null)
        {
            Log.Add(PlayerMode == TravelMode.Diving ? "dive up" : "dive down");
            PlayerMode = PlayerMode == TravelMode.Diving ? TravelMode.Surfing : TravelMode.Diving;
            return true;
        }
        bool up = Map.IsUnderwater;
        var onto = up
            ? FieldMovement.CanSurface(Map, x, y) ? Map.EmergeTargetAt(x, y) : null
            : FieldMovement.CanDive(Map, x, y, new Walker(TravelMode.Surfing, Map.HeightAt(x, y), Moves: FieldMoves.Dive)) ? Map.DiveTargetAt(x, y) : null;
        if (onto == null)
        {
            Log.Add("dive nowhere");
            return false;
        }
        // The same tile of the floor or the sea, or the one place the original names (plan 21 · H8, part b)
        Log.Add($"dive {(up ? "up" : "down")} {onto.Map} {onto.X} {onto.Y}");
        PlayerTile = (onto.X, onto.Y);
        if (MapNamed?.Invoke(onto.Map) is { } target)
        {
            Map = target;
            Map.ForgetForced();
            Map.ApplyPresence(Story.Has);
        }
        PlayerMode = up ? TravelMode.Surfing : TravelMode.Diving;
        return true;
    }

    /// <summary>Where <c>fly</c> goes: the town chosen on the map. Null, and <c>fly</c> does nothing.</summary>
    public SpawnLocation? FlyTo { get; set; }

    /// <summary>Where <c>escape</c> leads out of the caves; null where no way out is known.</summary>
    public MapSpot? Exit { get; set; }

    /// <summary>What <c>sweetscent</c> draws out: a species and its level, or null where nothing lives.</summary>
    public (string Species, int Level)? Scented { get; set; }

    /// <summary>What <c>smashencounter</c> finds under the rock: a species and its level, or null when it hid nothing.</summary>
    public (string Species, int Level)? Smashed { get; set; }

    public bool SmashEncounter()
    {
        Shown("a rock's hiding place");
        NeverInAHurry("a rock's Pokémon");
        if (Smashed is not var (species, level)) return false;
        WildBattle(new Pokemon(PokemonDatabase.Get(species)!, level), BattleKind.Normal, cannotFlee: false);
        return true;
    }

    public bool Fly()
    {
        Shown("fly");
        NeverInAHurry("a flight");
        if (FlyTo is not { } town) return false;
        // A town of another region is a crossing there (plan 18 · W9)
        if (RegionDatabase.RegionOfMap(town.Map) is { } to && !string.Equals(to.Id, Region, StringComparison.OrdinalIgnoreCase)
            && RegionDatabase.Get(Region) is { } here)
        {
            Log.Add($"fly {to.Id}");
            CrossTo(here, to, new MapSpot(town.Map, town.X, town.Y, Direction.Down));
            return true;
        }
        Warp(town.Map, town.X, town.Y, Direction.Down);
        return true;
    }

    public bool Teleport()
    {
        Shown("teleport");
        NeverInAHurry("Teleport");
        if (SpawnLocations.Respawn(Story, Region) is not { } town) return false;
        Warp(town.Map, town.X, town.Y, Direction.Down);
        return true;
    }

    public bool Escape()
    {
        Shown("escape");
        NeverInAHurry("a way out of the caves");
        if (Exit is not { } exit) return false;
        Warp(exit.Map, exit.X, exit.Y, exit.Facing);
        return true;
    }

    /// <summary>Where Turnback Cave's doors were last aimed (null before any).</summary>
    public string? TurnbackChose { get; private set; }

    public void Turnback()
    {
        Log.Add("turnback");
        if (Map != null) TurnbackChose = TurnbackCave.Reaim(Map, PlayerTile.X, PlayerTile.Y, Story, Rng);
    }

    public void Defeat(string trainerId)
    {
        Log.Add($"defeat {trainerId}");
        if (Map == null) return;
        foreach (var npc in Map.Everyone)
            if (npc.TrainerData?.Id == trainerId) npc.HasBattled = true;
    }

    public void TurnClock(int from, int to)
    {
        NeverInAHurry("the flower clock's turn");
        Log.Add($"flowerclock {from} {to}");
        // The clock is at rest at once: the map's tiles follow the story's new state
        if (Map?.Puzzle is EternaClock clock) clock.Apply(Map, Story);
    }

    public void PressButton(PastoriaWater.Button button)
    {
        NeverInAHurry("the Pastoria Gym's water");
        Log.Add($"pressbutton {button.ToString().ToLowerInvariant()}");
        // The water is at its level at once
        if (Map?.Puzzle is PastoriaWater water) water.Settle(button);
    }

    public void RotateTiles(MossdeepTiles.Colour colour)
    {
        NeverInAHurry("the Mossdeep Gym's arrows");
        Log.Add($"rotatetiles {colour.ToString().ToLowerInvariant()}");
        if (Map?.Puzzle is not MossdeepTiles tiles) return;
        // Everyone steps at once: where each is going is worked out before anyone moves
        foreach (var (who, step, facing) in MossdeepTiles.Turns(colour, (x, y) => Map.NPCs.At(x, y)))
        {
            tiles.Remember(who);
            var (dx, dy) = FieldMovement.Delta(step);
            (who.GridX, who.GridY) = (who.GridX + dx, who.GridY + dy);
            Face(who, facing);
        }
    }

    public void PressGearButton(SunyshoreGears.Button kind)
    {
        NeverInAHurry("the Sunyshore Gym's gears");
        Log.Add($"gearbutton {kind.ToString().ToLowerInvariant()}");
        // The gears are in their new state at once
        if (Map?.Puzzle is SunyshoreGears gears)
        {
            gears.Press(kind);
            gears.Finish();
        }
    }

    public bool TriggerLift()
    {
        NeverInAHurry("a platform's ride");
        Log.Add("platformlift");
        // The platform is at the top at once, and the player on it stands at its height
        if (Map?.Puzzle is not LeagueLift lift || !lift.Trigger()) return false;
        lift.Finish();
        return true;
    }

    public bool LiftNotUsed() => Map?.Puzzle is not LeagueLift lift || lift.NotUsedWhenEntered;

    /// <summary>Who travels with the player, and as which trainer (plan 02 · S6).</summary>
    public NPC? Travelling { get; private set; }
    public string? Partner { get; private set; }

    public void TravelWith(NPC? who, string? trainerId)
    {
        Travelling = who;
        Partner = who == null ? null : trainerId;
        Log.Add(who == null ? "partner off" : $"partner {who.Name} {trainerId}");
    }

    public bool SweetScent()
    {
        Shown("draw a Pokémon out");
        NeverInAHurry("Sweet Scent");
        if (Scented is not var (species, level)) return false;
        WildBattle(new Pokemon(PokemonDatabase.Get(species)!, level), BattleKind.Normal, cannotFlee: false);
        return true;
    }

    /// <summary>The same rock's Pokémon under J9's older name (plan 20 · J9): what <c>Smashed</c> holds.</summary>
    public (string Species, int Level)? InTheRock { get => Smashed; set => Smashed = value; }

    public bool Climb()
    {
        Shown("climb");
        NeverInAHurry("a climb");
        if (Map == null)
        {
            Log.Add("climb");
            return true;
        }
        var (x, y) = PlayerTile;
        var step = FieldMovement.Step(Map, x, y, PlayerFacing,
            new Walker(PlayerMode, Map.HeightAt(x, y), Moves: FieldMovement.MovesOf(Party) | FieldHelp.Moves(Granted), Climbing: true));
        if (step.Kind != StepKind.Climb)
        {
            Log.Add("climb nowhere");
            return false;
        }
        PlayerTile = (step.X, step.Y);
        Log.Add($"climb {step.X} {step.Y}");
        return true;
    }

    // ------------------------------------------------------------------ sound

    public void Music(string? song)
    {
        Shown("change the music");
        // (A change of music is kept by a hurry: the song is the one asked for last)
        Song = song == null ? "area" : song.Length == 0 ? "stop" : song;
        Log.Add(song == null ? "music area" : song.Length == 0 ? "music stop" : $"music {song}");
    }

    public void Fanfare(MusicRole role)
    {
        Shown("play a fanfare");
        Passing($"fanfare {role}");
    }

    public void Sound(string name)
    {
        Shown("play a sound");
        Passing($"sound {name}");
    }

    public void Cry(string species)
    {
        Shown("play a cry");
        Passing($"cry {species}");
    }

    // ------------------------------------------------------------------ the Battle Frontier (plan 06 · R18a)

    /// <summary>
    /// The Battle Frontier (plan 06 · R18a): its records, the Tower's challenge under way, the Pokémon a test has
    /// entered when the team is opened for it (left empty, the first of the team that may go in together), the
    /// teams its battles were fought with and against, and what was added to the Trainer Card's score.
    /// </summary>
    public Models.Frontier.FrontierRecords Frontier { get; } = new();
    public Models.Frontier.BattleTowerChallenge? Tower { get; set; }
    public Models.Frontier.BattleFactoryChallenge? Factory { get; set; }
    public Models.Frontier.BattleHallChallenge? Hall { get; set; }
    public Models.Frontier.BattleCastleChallenge? Castle { get; set; }
    public Models.Frontier.BattleArcadeChallenge? Arcade { get; set; }
    public Models.Frontier.BattleDomeChallenge? Dome { get; set; }

    /// <summary>The Pokémon of the team that walks behind the player, and the thing of the map it is (plan 06 · R16c).</summary>
    public Pokemon? Strolling { get; private set; }
    public NPC? StrollingAs { get; private set; }

    public void WalkWith(NPC? body, Pokemon? pokemon)
    {
        if (body != null && pokemon != null) Follower.Dress(body, pokemon);
        (StrollingAs, Strolling) = body != null && pokemon != null ? (body, pokemon) : (null, null);
        Log.Add(Strolling == null ? "walk with nobody" : $"walk with {Strolling.DisplayName}");
    }

    /// <summary>What a test does at one of the Battle Castle's counters ("castleself", "castlefoe") while the script waits; nothing when left empty.</summary>
    public Action<Models.Frontier.BattleCastleChallenge, string>? AtCastleCounter { get; set; }

    /// <summary>
    /// How a test plays the Battle Arcade's game board while the script waits: it is handed the board as it comes
    /// up and stops its cursor by the frame (<see cref="Models.Frontier.ArcadeBoard.Advance"/>, or
    /// <see cref="Models.Frontier.ArcadeBoard.PlayTo"/> for a panel). Left empty, the cursor is stopped on the first
    /// frame it runs.
    /// </summary>
    public Action<Models.Frontier.ArcadeBoard>? AtArcadeBoard { get; set; }

    /// <summary>The weather or the room each Frontier battle opened under, beside <see cref="FrontierBattles"/>; null where a facility set none.</summary>
    public List<Models.Frontier.FrontierConditions?> FrontierFields { get; } = new();
    public Queue<IReadOnlyList<int>> Entering { get; } = new();
    public IReadOnlyList<int> Entered { get; private set; } = Array.Empty<int>();
    public List<(Party Team, IReadOnlyList<Trainer> Foes, Trainer? Partner, bool Doubles)> FrontierBattles { get; } = new();
    public int FrontierScore { get; private set; }

    public int FrontierAct(string what, int number, NPC? subject)
    {
        Log.Add($"frontier {what} {number}");
        NeverInAHurry($"the Battle Frontier's '{what}'");
        switch (what)
        {
            case "enter":
                Waits();
                Entered = Entering.Count > 0 ? Entering.Dequeue() : FirstThatMayEnter(number);
                Answer = Entered.Count == number ? 1 : 0;
                return Answer;
            case "rent":
                // The Battle Factory's six: what a test chose, or the first three
                Waits();
                Entered = Entering.Count > 0 ? Entering.Dequeue() : Enumerable.Range(0, number).ToList();
                Answer = Entered.Count == number ? 1 : 0;
                return Answer;
            case "trade":
                // A trade a test asked for (the place given up and the place taken); none otherwise
                Waits();
                Entered = Entering.Count > 0 ? Entering.Dequeue() : Array.Empty<int>();
                Answer = Entered.Count == 2 ? 1 : 0;
                return Answer;
            case "enterhall":
                // The Battle Hall's one Pokémon, or its two of a species: what a test chose, or the first that may go in
                Waits();
                Entered = Entering.Count > 0 ? Entering.Dequeue() : FirstForTheHall(number);
                Answer = Entered.Count == number ? 1 : 0;
                return Answer;
            case "board":
                // The Battle Hall's board: the type a test chose, or the first that can still be chosen
                Waits();
                Entered = Entering.Count > 0 ? Entering.Dequeue()
                    : new[] { Enumerable.Range(0, Models.Frontier.BattleHallChallenge.Board.Length).FirstOrDefault(c => Hall == null || !Hall.Beaten(c)) };
                Answer = 1;
                return Answer;
            case "castleself":
            case "castlefoe":
                Waits();
                if (Castle != null) AtCastleCounter?.Invoke(Castle, what);
                Answer = 1;
                return Answer;
            case "arcadeboard":
                // The Battle Arcade's game board: played as a test plays it, or stopped at once
                Waits();
                if (Arcade?.Board is { } board)
                {
                    AtArcadeBoard?.Invoke(board);
                    if (!board.IsDone) board.PlayTo();
                }
                Answer = 1;
                return Answer;
            case "seen":
                return SeenSpecies.Any(s => PokemonDatabase.Get(s)?.DexNumber == number) ? 1 : 0;
            case "score":
                FrontierScore += number;
                return 1;
            case "records":
                Waits();
                return 1;
            default:
                return 1;
        }
    }

    // The first of the team that could go in together, in the team's order; none when too few can
    private List<int> FirstThatMayEnter(int needed)
    {
        var chosen = new List<int>();
        for (int i = 0; i < Party.Count && chosen.Count < needed; i++)
        {
            var p = Party.Members[i];
            if (!Models.Frontier.FrontierRules.MayEnter(p)) continue;
            if (chosen.Any(c => Party.Members[c].Species.Name == p.Species.Name || (p.HeldItem != null && Party.Members[c].HeldItem?.Name == p.HeldItem.Name))) continue;
            chosen.Add(i);
        }
        return chosen.Count == needed ? chosen : new List<int>();
    }

    // The first of the team the Battle Hall lets in: one, or the first two of one species; none when there are none
    private List<int> FirstForTheHall(int needed)
    {
        var allowed = Enumerable.Range(0, Party.Count).Where(i => Models.Frontier.BattleHallChallenge.MayEnter(Party.Members[i])).ToList();
        if (needed == 1) return allowed.Take(1).ToList();
        return allowed.GroupBy(i => Party.Members[i].Species.Name).Where(g => g.Count() >= needed).Select(g => g.Take(needed).ToList()).FirstOrDefault() ?? new List<int>();
    }

    public void FrontierBattle(Party team, IReadOnlyList<Trainer> foes, Trainer? partner, bool doubles, Models.Frontier.FrontierConditions? conditions = null)
    {
        Shown("start a battle");
        NeverInAHurry("a battle");
        Waits();
        FrontierBattles.Add((team, foes, partner, doubles));
        FrontierFields.Add(conditions);
        Outcome = Fight?.Invoke(null) ?? BattleOutcome.Won;
        Log.Add($"frontierbattle {string.Join(" and ", foes.Select(f => f.Id))}{(partner != null ? $" with {partner.Id}" : "")} {Outcome}");
    }

    // ------------------------------------------------------------------ the Hall of Fame's end (plan 02 · S14)

    /// <summary>Whether the National Pokédex is the player's (<c>if nationaldex</c>); not unless a test says so.</summary>
    public bool NationalDex { get; set; }

    /// <summary>Whether <c>save</c> can write the game; it can, unless a test says otherwise.</summary>
    public bool SaveFails { get; set; }

    /// <summary>
    /// What the story held each time a script saved the game (<c>save</c>), and where the player stood: how a test
    /// reads what a save made in the middle of a scene would come back to.
    /// </summary>
    public List<(StorySnapshot Story, string? Map, (int X, int Y) Tile)> Saves { get; } = new();

    public bool Save()
    {
        Shown("save the game");
        NeverInAHurry("a save");
        Log.Add(SaveFails ? "save failed" : "save");
        if (SaveFails) return false;
        Saves.Add((Story.Snapshot(), Map?.Name, PlayerTile));
        return true;
    }

    // ------------------------------------------------------------------ after the Hall of Fame (plan 02 · S15)

    public void GiveNationalDex()
    {
        NationalDex = true;
        Log.Add("nationaldex");
    }

    /// <summary>How many teams have entered the Hall of Fame (<c>LEAGUE_VICTORIES</c>): those this host recorded, unless a test says otherwise.</summary>
    public int LeagueVictories
    {
        get => leagueVictories ?? HallOfFame.Total;
        set => leagueVictories = value;
    }

    private int? leagueVictories;

    // ------------------------------------------------------------------ the Cycling Road's gates

    public bool Cycling => PlayerMode == TravelMode.Cycling;

    public void Ride(bool on)
    {
        Log.Add(on ? "bicycle on" : "bicycle off");
        if (PlayerMode is not (TravelMode.Surfing or TravelMode.Diving)) PlayerMode = on ? TravelMode.Cycling : TravelMode.OnFoot;
    }
}
