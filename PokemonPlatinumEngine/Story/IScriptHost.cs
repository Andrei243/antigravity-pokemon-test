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
public enum ScriptScreen { Starter, Shop, Pc, Travel }

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
    void Open(ScriptScreen screen, NPC? subject);

    /// <summary>Adds a Pokémon to the team, or to the PC when the team is full; true if it went to the team.</summary>
    bool GivePokemon(Pokemon pokemon);

    void Warp(string map, int x, int y, Direction? facing);
    void Fade(bool toBlack, float seconds);

    // ------------------------------------------------------------------ sound

    /// <summary>Plays a song; null goes back to the theme of the place, and an empty name is silence.</summary>
    void Music(string? song);
    void Fanfare(MusicRole role);
    void Sound(string name);

    /// <summary>A Pokémon met in the field cries: a species, or one of its forms by name.</summary>
    void Cry(string species);
}
