using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Story;

/// <summary>What one line of a script does. <c>docs/scripts.md</c> describes each as it is written.</summary>
public enum Op
{
    // What is said
    Say, Text, SayOwn, TrainerLine, Speaker, Ask, Choose,
    // Where the script goes next
    If, Goto, Call, Return, End,
    // What the story remembers
    SetFlag, ClearFlag, SetVar, AddVar,
    // What the player is given, finds and has taken back
    Give, Find, AddItem, Take, GivePokemon, GiveBadge, GiveMoney, TakeMoney, Heal,
    // Battles
    Battle, WildBattle, CatchingLesson,
    // People and the field
    Face, Walk, Move, WaitMoves, Emote, Show, Hide, Place, Warp, Fade, Wait, Camera,
    // Field moves (plan 02 · S2)
    UseMove, Surf, Climb, Fly, Teleport, Escape, SweetScent,
    // The Pokétch (plan 02 · S2)
    Poketch, PoketchApp,
    // The Great Marsh's Safari Game (plan 01 · M7)
    Safari,
    // Turnback Cave's doors (plan 01 · M8)
    Turnback,
    // Sound
    Music, Fanfare, Sound, Cry,
    // The screens that exist
    Starter, Shop, Pc, Travel,
    // The trainer tools (plan 06 · R12)
    ChoosePokemon, Trade, HallOfFame
}

/// <summary>How two numbers are compared in a condition.</summary>
public enum Compare { Equal, NotEqual, Less, LessOrEqual, Greater, GreaterOrEqual }

/// <summary>What a condition asks about.</summary>
public enum Query
{
    Flag, Var, Badge, Badges, Item, Party, Knows, Has, Yes, No, Won, Lost, Result, Defeated, Taken, Starter, Money, Facing, Boy, Girl, Poketch, Pokerus, Safari, Rematch
}

/// <summary>A question a script asks of the game before a line: <c>if [not] ...</c>.</summary>
public sealed class Condition
{
    public Query Query { get; init; }
    public bool Negated { get; init; }

    /// <summary>The flag, variable, badge, item, move, species, trainer or direction asked about.</summary>
    public string Name { get; init; } = "";
    public Compare Compare { get; init; } = Compare.GreaterOrEqual;

    /// <summary>What a number is held against: a number, or a variable's name in <see cref="Other"/>.</summary>
    public int Number { get; init; }
    public string? Other { get; init; }

    public static bool Holds(int left, Compare compare, int right) => compare switch
    {
        Compare.Equal => left == right,
        Compare.NotEqual => left != right,
        Compare.Less => left < right,
        Compare.LessOrEqual => left <= right,
        Compare.Greater => left > right,
        _ => left >= right
    };
}

/// <summary>The three things the field's camera can be told.</summary>
public enum CameraMove { Pan, Release, Shake }

/// <summary>One line of a script, checked and taken apart when its file was read.</summary>
public sealed class Instruction
{
    public Op Op { get; init; }

    /// <summary>The line of the file it was written on, for messages about it.</summary>
    public int Line { get; init; }

    /// <summary>Who or what the line is about: a flag, a variable, a person, an item, a species, a label, a script, a map, a song.</summary>
    public string Name { get; init; } = "";

    /// <summary>A second name: who a person turns to face, the second trainer of a battle against two.</summary>
    public string Other { get; init; } = "";

    /// <summary>
    /// Who battles beside the player (plan 06 · R9): a person of the map, or with <see cref="PartnerById"/> a trainer
    /// of Platinum's data by its id.
    /// </summary>
    public string Partner { get; init; } = "";
    public bool PartnerById { get; init; }

    /// <summary>A battle that is the game's first (the rival's on Route 201): no critical hits.</summary>
    public bool FirstBattle { get; init; }

    /// <summary>
    /// A battle against someone of the map with a team of Platinum's other than their own (<c>battle self as
    /// "rival_route_201_turtwig"</c>): the rival, whose team depends on the starter the player took. Empty for their own.
    /// </summary>
    public string AsTrainer { get; init; } = "";

    /// <summary>For <c>battle self rematch</c>: the trainer fights with the team the Vs. Seeker has them bring (plan 06 · R12).</summary>
    public bool Rematch { get; init; }

    /// <summary>A count, a level, an amount, a value; and a tile where the line names one.</summary>
    public int Number { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public float Seconds { get; init; }
    public Direction? Direction { get; init; }

    /// <summary>The lines said, or a question followed by its answers.</summary>
    public IReadOnlyList<string> Lines { get; init; } = Array.Empty<string>();

    /// <summary>The steps of a walk.</summary>
    public IReadOnlyList<Direction> Path { get; init; } = Array.Empty<Direction>();

    /// <summary>A word that changes how the line is carried out: a walk at a run, a battle that may be lost, a fade to black.</summary>
    public bool Option { get; init; }

    /// <summary>
    /// The line means what the script was started with rather than something it names: the item of the ball or
    /// of the hidden spot (<c>find own</c>), the flag that keeps it gone (<c>setflag own</c>).
    /// </summary>
    public bool Own { get; init; }

    public EmoteBubble Bubble { get; init; }
    public CameraMove Camera { get; init; }
    public Audio.MusicRole Role { get; init; }
    public Badge Badge { get; init; }

    /// <summary>For <see cref="Op.If"/>: the question, and the line carried out when the answer is yes.</summary>
    public Condition? Condition { get; init; }
    public Instruction? Then { get; init; }

    /// <summary>For <see cref="Op.Goto"/>: where in the script the label is, filled in once the whole script is read.</summary>
    public int Target { get; set; } = -1;
}

/// <summary>A script: a name and the lines carried out one after another when it is started.</summary>
public sealed class Script
{
    /// <summary>The file it is written in, without its ending: an area's key, a hand-made map's name, or <c>common</c>.</summary>
    public string File { get; }
    public string Name { get; }
    public int Line { get; }
    public IReadOnlyList<Instruction> Code { get; }
    public IReadOnlyDictionary<string, int> Labels { get; }

    /// <summary>How other files call it: <c>file.Name</c>.</summary>
    public string FullName => File + "." + Name;

    public Script(string file, string name, int line, IReadOnlyList<Instruction> code, IReadOnlyDictionary<string, int> labels)
    {
        File = file;
        Name = name;
        Line = line;
        Code = code;
        Labels = labels;
    }

    public override string ToString() => FullName;

    /// <summary>Every line of the script, with the line an <c>if</c> guards after the <c>if</c> itself.</summary>
    public IEnumerable<Instruction> Everything()
    {
        foreach (var i in Code)
        {
            yield return i;
            if (i.Then != null) yield return i.Then;
        }
    }
}

/// <summary>Something wrong with a script: where it is written, or what happened as it ran.</summary>
public sealed class ScriptException : Exception
{
    public ScriptException(string message) : base(message) { }
}
