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

/// <summary>The screens a script can open and wait for.</summary>
public enum ScriptScreen { Starter, Shop, Pc, Travel, ChoosePokemon, HallOfFame }

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

    /// <summary>The player's Pokétch: whether they have it and its apps (plan 02 · S2).</summary>
    Poketch Poketch { get; }

    /// <summary>The Great Marsh's Safari Game, while one is under way (plan 01 · M7).</summary>
    SafariGame Safari { get; }
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

    /// <summary>Starts someone walking; the script carries on meanwhile unless it waits (<see cref="Walking"/>).</summary>
    void Walk(NPC? who, IReadOnlyList<Direction> steps, bool fast);

    /// <summary>Whether anyone a script set walking is still on their way.</summary>
    bool Walking { get; }

    void Emote(NPC? who, EmoteBubble bubble, float seconds);
    void Camera(CameraMove move, int x, int y, float seconds);

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
    /// Battles a trainer of the map, or two at once (<paramref name="second"/>), perhaps with a trainer at the
    /// player's side (<paramref name="partner"/>, a tag battle). <see cref="Outcome"/> is how it went once the host is
    /// no longer busy. The game's first battle (<paramref name="first"/>) has no critical hits.
    /// </summary>
    void Battle(NPC trainer, NPC? second, Trainer? partner, bool mayLose, bool first);

    /// <summary>A wild Pokémon in the player's way: one met as ever, one that can't be run from, or the assistant's catching lesson, which the player watches.</summary>
    void WildBattle(Pokemon wild, BattleKind kind, bool cannotFlee);
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
    void Fade(bool toBlack, float seconds);

    // ------------------------------------------------------------------ field moves (plan 02 · S2)

    /// <summary>
    /// A Pokémon of the team uses a field move, once the line that says so has been read: its cut-in and its
    /// sound, and what it does to the obstacle the script belongs to (a tree falling, a rock breaking apart).
    /// The script waits while it plays (<see cref="Busy"/>). What the move leaves behind is the script's to say
    /// with flags: the obstacle's own flag, <c>FLAG_STRENGTH_ACTIVE</c>.
    /// </summary>
    void UseMove(FieldMove move, Pokemon user, NPC? subject);

    /// <summary>Sets out onto the water the player faces, on a Pokémon's back. False when there is no water there to surf on.</summary>
    bool Surf();

    /// <summary>Climbs the waterfall or the rock face the player faces, up or down. False when there is none to climb from here.</summary>
    bool Climb();

    /// <summary>Flies to the town chosen on the map before the script began. False when none was chosen.</summary>
    bool Fly();

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
}
