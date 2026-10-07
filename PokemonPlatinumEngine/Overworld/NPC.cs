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

    /// <summary>
    /// The height they stand at where it isn't the ground's: on a bridge's deck, over ground others walk (plan 01 ·
    /// M6). Null on the ground. Only someone at about that height runs into them or speaks to them.
    /// </summary>
    public float? Level { get; set; }
    public int GridY { get; set; }
    public Direction Facing { get; set; } = Direction.Down;
    public List<string> DialogLines { get; set; } = new();

    // Trainer specific
    public bool IsTrainer { get; set; } = false;
    public Trainer? TrainerData { get; set; }
    public bool HasSpottedPlayer { get; set; } = false;
    public float ExclamationTimer { get; set; } = 0f;

    /// <summary>How they move about when nobody sends them anywhere (plan 02 · S6); null for someone who stands still.</summary>
    public PersonMovement? Movement { get; set; }

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

    /// <summary>
    /// A trainer the Vs. Seeker found wanting a rematch (plan 06 · R12): spins where they stand until spoken to, a
    /// hundred steps go by or the player goes somewhere else. Never saved, as in the original.
    /// </summary>
    public bool ReadyForRematch { get; set; }

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

    /// <summary>
    /// For a Poké Mart's clerk: the counter they keep, by the original's specialty id (<c>jubilife</c>,
    /// <c>eterna_house</c>; <see cref="Data.MartDatabase"/>); null keeps the common counter, whose stock grows with
    /// the badges (plan 06 · R11).
    /// </summary>
    public string? Mart { get; set; }

    // ---- Items on the ground (plan 02)

    /// <summary>The <see cref="NpcType"/> of an item lying on the ground in its ball: it is drawn as the ball, and picked up by speaking to it.</summary>
    public const string ItemBallType = "ItemBall";

    /// <summary>For an item ball: the item in it, by name, and how many.</summary>
    public string? Item { get; set; }
    public int ItemCount { get; set; } = 1;

    public bool IsItemBall => NpcType == ItemBallType;

    // ---- Obstacles a field move clears (plan 02 · S2)

    /// <summary>
    /// The <see cref="NpcType"/>s of the three obstacles, which are objects of the map as the original's are: a small
    /// tree for Cut, a cracked rock for Rock Smash, a boulder for Strength. Each is drawn as its card, stands in the
    /// way like a person, and runs its common script when the player faces it and presses the button.
    /// </summary>
    public const string CutTreeType = "CutTree", CrackedRockType = "CrackedRock", BoulderType = "StrengthBoulder";

    /// <summary>Which obstacle this is; null for anyone else.</summary>
    public PropType? Obstacle => NpcType switch
    {
        CutTreeType => PropType.CutTree,
        CrackedRockType => PropType.CrackedRock,
        BoulderType => PropType.StrengthBoulder,
        _ => null
    };

    public bool IsObstacle => Obstacle != null;

    /// <summary>The <see cref="NpcType"/> of an obstacle.</summary>
    public static string TypeOf(PropType obstacle) => obstacle switch
    {
        PropType.CutTree => CutTreeType,
        PropType.CrackedRock => CrackedRockType,
        PropType.StrengthBoulder => BoulderType,
        _ => throw new ArgumentException($"{obstacle} is no obstacle a field move clears.")
    };

    /// <summary>
    /// A thing rather than a person: an item's ball or an obstacle. It is drawn as a card, never as a character,
    /// and never turns to face anyone.
    /// </summary>
    public bool IsThing => IsItemBall || IsObstacle;

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
