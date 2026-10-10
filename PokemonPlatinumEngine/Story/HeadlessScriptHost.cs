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

    public void Walk(NPC? who, IReadOnlyList<Direction> steps, bool fast)
    {
        Shown("walk someone");
        if (Pauses) Walking = true;
        var (x, y) = TileOf(who);
        foreach (var step in steps)
        {
            var (dx, dy) = FieldMovement.Delta(step);
            x += dx;
            y += dy;
            Check(who, x, y, "walks");
        }
        if (who == null) PlayerTile = (x, y);
        else (who.GridX, who.GridY) = (x, y);
        if (steps.Count > 0) Face(who, steps[^1]);
    }

    private void Check(NPC? who, int x, int y, string does)
    {
        if (Map == null) return;
        string name = who?.Name ?? "the player";
        if (!Map.InBounds(x, y)) Problems.Add($"{name} {does} off the map at {x},{y}");
        else if (Map.IsSolid(x, y)) Problems.Add($"{name} {does} into something solid at {x},{y}");
    }

    /// <summary>Never, unless a test holds the script up to see where it waits for a walk.</summary>
    public bool Walking { get; set; }

    public void Emote(NPC? who, EmoteBubble bubble, float seconds)
    {
        Shown("show a bubble");
        Log.Add($"emote {who?.Name ?? "player"} {bubble}");
    }

    public void Camera(CameraMove move, int x, int y, float seconds)
    {
        Shown("move the camera");
        Log.Add(move == CameraMove.Pan ? $"camera Pan {x} {y}" : $"camera {move}");
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
}
