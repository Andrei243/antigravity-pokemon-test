using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

public class NPC
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Townsperson";
    public string NpcType { get; set; } = "Trainer"; // "Rowan", "Rival", "Nurse", "Clerk", "Youngster", "Lass", "StarterBriefcase"
    public int GridX { get; set; }
    public int GridY { get; set; }
    public Direction Facing { get; set; } = Direction.Down;
    public List<string> DialogLines { get; set; } = new();

    // Trainer specific
    public bool IsTrainer { get; set; } = false;
    public Trainer? TrainerData { get; set; }
    public bool HasSpottedPlayer { get; set; } = false;
    public float ExclamationTimer { get; set; } = 0f;

    /// <summary>What shows in the bubble over this person's head, for how much longer, and for how long it has.</summary>
    public EmoteBubble Bubble { get; private set; }
    public float BubbleTimer { get; private set; }
    public float BubbleAge { get; private set; }

    public void ShowBubble(EmoteBubble bubble, float seconds = 0.9f)
    {
        Bubble = bubble;
        BubbleTimer = seconds;
        BubbleAge = 0f;
    }

    /// <summary>Runs the bubble's clock.</summary>
    public void TickBubble(float dt)
    {
        if (BubbleTimer <= 0f) return;
        BubbleTimer -= dt;
        BubbleAge += dt;
    }
    public bool HasBattled { get; set; } = false;

    // Walking (a trainer stepping up to the player): the grid position is where the current step ends
    public float StepOffsetX { get; set; }
    public float StepOffsetY { get; set; }
    public float WalkCycle { get; set; }
    public float WalkBlend { get; set; }

    /// <summary>Where the character is drawn, in tiles (differs from the grid position mid-step).</summary>
    public float DrawX => GridX + StepOffsetX;
    public float DrawY => GridY + StepOffsetY;

    private (int X, int Y, Direction Facing)? post;

    // Special Interactions
    public bool IsStarterBriefcase { get; set; } = false;
    public bool IsHealingNurse { get; set; } = false;
    public bool IsPokeMartClerk { get; set; } = false;
    public bool IsPCTerminal { get; set; } = false;

    /// <summary>Takes the player to the next region (a ferry sailor, say) once this region's story is finished.</summary>
    public bool IsTransportAttendant { get; set; } = false;

    // ---- The story (plan 02 · S1)

    /// <summary>What scripts call them: their id in the area's file or in the map's. Null for someone no script names.</summary>
    public string? Key { get; set; }

    /// <summary>
    /// The script that talking to them runs; null to go by what they are (a nurse, a trainer, someone with lines
    /// to say: <see cref="Story.FieldScripts.For"/>).
    /// </summary>
    public string? Script { get; set; }

    /// <summary>Where a script's bare name is looked for first: their area's key. Null on a hand-made map, whose name it is.</summary>
    public string? ScriptFile { get; set; }

    /// <summary>A flag that takes them off the map while it is set, as the original's objects have.</summary>
    public string? HiddenBy { get; set; }

    /// <summary>A flag they wait for: they are on the map only while it is set.</summary>
    public string? ShownBy { get; set; }

    /// <summary>Set when a script showed or hid them itself: it holds, whatever the flags say, until the map is come to again.</summary>
    public bool? Forced { get; set; }

    /// <summary>The order they were put on the map in, kept so that coming back puts them where they were in the list.</summary>
    internal int Order { get; set; } = -1;

    /// <summary>Whether they are on the map, given which flags are set.</summary>
    public bool IsPresent(Func<string, bool> flagSet) =>
        Forced ?? ((HiddenBy == null || !flagSet(HiddenBy)) && (ShownBy == null || flagSet(ShownBy)));

    /// <summary>Remembers where the trainer was standing, before they walk up to the player.</summary>
    public void LeavePost() => post ??= (GridX, GridY, Facing);

    /// <summary>
    /// A beaten trainer stays where the battle happened and doesn't challenge again. One who won goes back to
    /// their post with a healed team, so the player can return for a rematch.
    /// </summary>
    public void FinishBattle(bool playerWon)
    {
        if (playerWon)
        {
            HasBattled = true;
            return;
        }

        TrainerData?.Party.HealAll();
        if (post.HasValue)
        {
            (GridX, GridY, Facing) = post.Value;
        }
    }

    public void FaceTowards(int playerX, int playerY)
    {
        if (playerX < GridX) Facing = Direction.Left;
        else if (playerX > GridX) Facing = Direction.Right;
        else if (playerY < GridY) Facing = Direction.Up;
        else if (playerY > GridY) Facing = Direction.Down;
    }
}
