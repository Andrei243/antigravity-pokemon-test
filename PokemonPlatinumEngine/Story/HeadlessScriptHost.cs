using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
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
    public int Money { get; set; } = 3000;
    public string PlayerName { get; set; } = PlayerIdentity.DefaultName(PlayerLook.Boy);
    public PlayerLook PlayerLook { get; set; }

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

    private string Named(string text) => PlayerIdentity.Fill(text, PlayerName, PlayerLook);

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

    public void Battle(NPC trainer, bool mayLose)
    {
        Shown("start a battle");
        Waits();
        Outcome = Fight?.Invoke(trainer) ?? BattleOutcome.Won;
        Log.Add($"battle {trainer.TrainerData?.Id} {Outcome}");
        if (Outcome == BattleOutcome.Won && trainer.TrainerData is { } beaten)
        {
            Money += beaten.PrizeMoney;
            Story.Defeat(beaten.Id);
            // Two people who battle as one trainer are beaten together
            foreach (var npc in (Map?.Everyone ?? new[] { trainer }).Append(trainer))
                if (npc == trainer || (beaten.Id.Length > 0 && npc.TrainerData?.Id == beaten.Id)) npc.FinishBattle(true);
        }
        else if (Outcome == BattleOutcome.Lost)
        {
            Party.HealAll();
        }
    }

    public void WildBattle(Pokemon wild)
    {
        Shown("start a battle");
        Waits();
        Outcome = Fight?.Invoke(null) ?? BattleOutcome.Won;
        Log.Add($"wildbattle {wild.Species.Name} {wild.Level} {Outcome}");
        if (Outcome == BattleOutcome.Caught) GivePokemon(wild);
        else if (Outcome == BattleOutcome.Lost) Party.HealAll();
    }

    public BattleOutcome Outcome { get; private set; }

    public void Open(ScriptScreen screen, NPC? subject)
    {
        Shown("open a screen");
        Waits();
        Log.Add($"open {screen}");
        Answer = 0;
        if (screen != ScriptScreen.Starter) return;

        // As the game does for now: the Pokémon chosen is the whole team (plan 02 · S4 takes the free Turtwig away)
        int choice = Math.Clamp(StarterChoice, 0, StoryState.Starters.Length - 1);
        string species = StoryState.Starters[choice];
        Party.Clear();
        Party.Add(new Pokemon(PokemonDatabase.Get(species)!, 5));
        Story.ChooseStarter(species);
        Answer = choice;
    }

    public bool GivePokemon(Pokemon pokemon)
    {
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
