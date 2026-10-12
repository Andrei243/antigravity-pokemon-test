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
    public int SeenInSinnoh { get; set; }
    public int Money { get; set; } = 3000;
    public string PlayerName { get; set; } = PlayerIdentity.DefaultName(PlayerLook.Boy);
    public PlayerLook PlayerLook { get; set; }
    public string RivalName { get; set; } = PlayerIdentity.DefaultRivalName;

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

    /// <summary>The Pokémon in the PC's boxes the lottery reads; none unless a test puts some there.</summary>
    public List<Pokemon> Boxes { get; } = new();
    public IEnumerable<Pokemon> Boxed => Boxes;

    public BerryPatches Berries { get; set; } = BerryPatches.NewGame();

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

    /// <summary>The turns the battle last fought took, as a test says (<see cref="Fight"/> may set it battle by battle).</summary>
    public int BattleTurns { get; set; }

    public int UndergroundAct(string what, NPC? subject)
    {
        Log.Add($"underground {what}");
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
        switch (what)
        {
            case "slots":
                if (SlotsLeave is { } coins) Pastimes.Coins.Set(coins);
                return 1;
            case "boxroom":
                return Models.PcBoxes.BoxCount * Models.PcBoxes.BoxSize - Boxes.Count;
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

    private string Named(string text) => PlayerIdentity.Fill(text, PlayerName, PlayerLook, RivalName);

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
        if (Pauses) Walking = true;
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
        if (who == null) PlayerTile = (x, y);
        else (who.GridX, who.GridY) = (x, y);
        if (facing is { } way) Face(who, way);
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
        Log.Add($"act {who?.Name ?? "player"} {gesture}" + (times > 1 ? $" {times}" : ""));
    }

    public void Mood(NPC? who, Mood mood)
    {
        Shown("give someone a face");
        if (who == null) PlayerMood = mood;
        else who.Mood = mood;
        Log.Add($"mood {who?.Name ?? "player"} {mood}");
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
        Log.Add($"emote {who?.Name ?? "player"} {bubble}");
    }

    public float Camera(CameraMove move, int x, int y, float seconds, float factor = 1f, NPC? who = null, IReadOnlyList<(int X, int Y)>? path = null)
    {
        Shown("move the camera");
        string Number(float value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        switch (move)
        {
            case CameraMove.Pan:
                Log.Add($"camera Pan {x} {y}");
                return seconds;
            case CameraMove.Zoom:
                Log.Add($"camera Zoom {Number(factor)}");
                return seconds;
            case CameraMove.Follow:
                Log.Add($"camera Follow {who?.Name ?? "player"}");
                return seconds;
            case CameraMove.Path:
            {
                // Everything happens at once here: the path's time is told from its own tiles, as if the camera began at the first
                var tiles = path ?? Array.Empty<(int X, int Y)>();
                Log.Add($"camera Path {string.Join(" ", tiles.Select(t => $"{t.X} {t.Y}"))} at {Number(factor)}");
                float length = 0f;
                for (int i = 1; i < tiles.Count; i++)
                    length += MathF.Sqrt((tiles[i].X - tiles[i - 1].X) * (tiles[i].X - tiles[i - 1].X) + (tiles[i].Y - tiles[i - 1].Y) * (tiles[i].Y - tiles[i - 1].Y));
                return CameraRig.PathSeconds(length, factor);
            }
            default:
                Log.Add($"camera {move}");
                return move == CameraMove.Release ? seconds : 0f;
        }
    }

    public void Letterbox(bool on, float seconds)
    {
        Shown("slide the bars");
        Log.Add(on ? "letterbox on" : "letterbox off");
    }

    public void SlowMotion(float share, float seconds)
    {
        Shown("slow the field");
        Log.Add($"slowmo {share.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    }

    public void Effect(SceneEffect kind, int x, int y, float seconds, Direction? way, bool hold)
    {
        Shown("show an effect");
        Log.Add($"effect {kind} {x} {y}" + (way is { } w ? $" {w}" : "") + (hold ? " hold" : ""));
    }

    public void StopEffect(int x, int y)
    {
        Shown("stop an effect");
        Log.Add($"effect stop {x} {y}");
    }

    // ------------------------------------------------------------------ what a script waits for

    /// <summary>Never, unless a test holds the script up to see where it waits for the player.</summary>
    public bool Busy { get; set; }

    /// <summary>How many times the text box was opened: lines said one after another are one talk.</summary>
    public int Talks { get; private set; }

    public void Say(string? speaker, IReadOnlyList<string> lines)
    {
        Shown("say something");
        Waits();
        Talks++;
        string? who = speaker != null ? Named(speaker) : null;
        foreach (string line in lines) Transcript.Add((who, Named(line)));
    }

    public void Ask(string? speaker, string question, IReadOnlyList<string> answers, int cancel)
    {
        Shown("ask something");
        Waits();
        string asked = Named(question);
        Transcript.Add((speaker != null ? Named(speaker) : null, asked));
        int answer = Answers.Count > 0 ? Answers.Dequeue() : Decide?.Invoke(asked, answers) ?? 0;
        Answer = Math.Clamp(answer, 0, answers.Count - 1);
        Asked.Add((asked, answers[Answer]));
    }

    public int Answer { get; private set; }

    public void Battle(NPC trainer, NPC? second, Trainer? partner, bool mayLose, bool first)
    {
        Shown("start a battle");
        Waits();
        Outcome = Fight?.Invoke(trainer) ?? BattleOutcome.Won;
        string with = (second != null ? $" and {second.TrainerData?.Id}" : "") + (partner != null ? $" with {partner.Id}" : "") + (first ? " first" : "");
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

    public void WildBattle(Pokemon wild, BattleKind kind, bool cannotFlee)
    {
        Shown("start a battle");
        Waits();
        // The lesson is the assistant's battle: their ball always catches, and what they catch is theirs
        Outcome = kind == BattleKind.CatchingLesson ? BattleOutcome.Caught : Fight?.Invoke(null) ?? BattleOutcome.Won;
        Log.Add($"{(kind == BattleKind.CatchingLesson ? "catchinglesson" : "wildbattle")} {wild.Species.Name} {wild.Level}{(cannotFlee ? " nofleeing" : "")} {Outcome}");
        if (Outcome == BattleOutcome.Caught && kind != BattleKind.CatchingLesson) GivePokemon(wild);
        else if (Outcome == BattleOutcome.Lost) Party.HealAll();
    }

    public BattleOutcome Outcome { get; private set; }

    /// <summary>The place of the team's Pokémon chosen when a script asks for one (<c>choosepokemon</c>); 255 backs out.</summary>
    public int PokemonChoice { get; set; }

    public void Open(ScriptScreen screen, NPC? subject, string? counter = null)
    {
        Shown("open a screen");
        Waits();
        Log.Add(screen == ScriptScreen.Shop && (counter ?? subject?.Mart) is { } mart ? $"open {screen} {mart}"
            : screen == ScriptScreen.Wardrobe && counter != null ? $"open {screen} {counter}"
            : $"open {screen}");
        Answer = 0;
        // The team's Pokémon a script asks to have chosen: the one the test says (none, 255, to back out)
        if (screen == ScriptScreen.ChoosePokemon) Answer = PokemonChoice;
        if (screen == ScriptScreen.ChooseItem)
        {
            string what = counter ?? "berries";
            ChosenItem = ItemChoice is { } named
                ? (named.Length > 0 && Bag.AllItems.Any(s => s.Name == named) ? named : null)
                : Bag.AllItems.FirstOrDefault(s => UI.BagScreen.Fits(what, s.Data))?.Name;
            Answer = ChosenItem != null ? 1 : 0;
            Log[^1] = $"open {screen} {what} {ChosenItem ?? "none"}";
        }
        if (screen != ScriptScreen.Starter) return;

        // As the game does: the Pokémon chosen joins the team (the player's first, since plan 02 · S4)
        int choice = Math.Clamp(StarterChoice, 0, StoryState.Starters.Length - 1);
        string species = StoryState.Starters[choice];
        GivePokemon(new Pokemon(PokemonDatabase.Get(species)!, 5));
        Story.ChooseStarter(species);
        Answer = choice;
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

    public bool GivePokemon(Pokemon pokemon)
    {
        pokemon.Met(Map?.DisplayNameAt(PlayerTile.X, PlayerTile.Y), GameClock.Today);
        if (Party.Add(pokemon)) return true;
        Box.Add(pokemon);
        return false;
    }

    public void Warp(string map, int x, int y, Direction? facing)
    {
        Shown("go somewhere else");
        Waits();
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

    public void Fade(bool toBlack, float seconds)
    {
        Shown("fade the screen");
        Waits();
        Log.Add(toBlack ? "fade out" : "fade in");
    }

    // ------------------------------------------------------------------ field moves

    /// <summary>How the player travels: on foot until a script has them surf.</summary>
    public TravelMode PlayerMode { get; set; }

    public void UseMove(FieldMove move, Pokemon user, NPC? subject)
    {
        Shown("use a field move");
        Waits();
        Log.Add($"usemove {FieldMoveRules.MoveName(move)} {user.Nickname}{(subject != null ? " on " + subject.Name : "")}");
    }

    public bool Surf()
    {
        Shown("go out onto the water");
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

    /// <summary>Where <c>fly</c> goes: the town chosen on the map. Null, and <c>fly</c> does nothing.</summary>
    public SpawnLocation? FlyTo { get; set; }

    /// <summary>Where <c>escape</c> leads out of the caves; null where no way out is known.</summary>
    public MapSpot? Exit { get; set; }

    /// <summary>What <c>sweetscent</c> draws out: a species and its level, or null where nothing lives.</summary>
    public (string Species, int Level)? Scented { get; set; }

    public bool Fly()
    {
        Shown("fly");
        if (FlyTo is not { } town) return false;
        Warp("Sinnoh", town.X, town.Y, Direction.Down);
        return true;
    }

    public bool Teleport()
    {
        Shown("teleport");
        var town = SpawnLocations.Respawn(Story);
        Warp("Sinnoh", town.X, town.Y, Direction.Down);
        return true;
    }

    public bool Escape()
    {
        Shown("escape");
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
        Log.Add($"flowerclock {from} {to}");
        // The clock is at rest at once: the map's tiles follow the story's new state
        if (Map?.Puzzle is EternaClock clock) clock.Apply(Map, Story);
    }

    public void PressButton(PastoriaWater.Button button)
    {
        Log.Add($"pressbutton {button.ToString().ToLowerInvariant()}");
        // The water is at its level at once
        if (Map?.Puzzle is PastoriaWater water) water.Settle(button);
    }

    public void PressGearButton(SunyshoreGears.Button kind)
    {
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
        if (Scented is not var (species, level)) return false;
        WildBattle(new Pokemon(PokemonDatabase.Get(species)!, level), BattleKind.Normal, cannotFlee: false);
        return true;
    }

    public bool Climb()
    {
        Shown("climb");
        if (Map == null)
        {
            Log.Add("climb");
            return true;
        }
        var (x, y) = PlayerTile;
        var step = FieldMovement.Step(Map, x, y, PlayerFacing,
            new Walker(PlayerMode, Map.HeightAt(x, y), Moves: FieldMovement.MovesOf(Party), Climbing: true));
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
        Log.Add(song == null ? "music area" : song.Length == 0 ? "music stop" : $"music {song}");
    }

    public void Fanfare(MusicRole role)
    {
        Shown("play a fanfare");
        Log.Add($"fanfare {role}");
    }

    public void Sound(string name)
    {
        Shown("play a sound");
        Log.Add($"sound {name}");
    }

    public void Cry(string species)
    {
        Shown("play a cry");
        Log.Add($"cry {species}");
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
}
