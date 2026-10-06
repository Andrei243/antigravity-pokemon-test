using System;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>The three rods, in the original's order (<c>EncounterFishingRodType</c>).</summary>
public enum FishingRod { Old, Good, Super }

/// <summary>Where a cast has got to (the original's fishing task, <c>src/overlay005/fishing.c</c>).</summary>
public enum FishingStage
{
    /// <summary>The rod is cast: 34 frames of the original's 30 a second.</summary>
    Casting,
    /// <summary>The line is in the water: until something bites, or for four seconds if nothing will.</summary>
    Waiting,
    /// <summary>Something has bitten: the button must be pressed before the rod's moment passes.</summary>
    Hooked,
    /// <summary>It was pressed in time: the Pokémon is reeled in.</summary>
    Landed,
    /// <summary>The button was pressed before anything bit.</summary>
    TooSoon,
    /// <summary>The moment passed.</summary>
    GotAway,
    /// <summary>Nothing bit at all.</summary>
    NoNibble,
    /// <summary>The rod is put away.</summary>
    Done
}

/// <summary>
/// One cast of a rod (plan 02 · S2), by the original's fishing task: whether anything will bite is decided as the
/// rod is cast (<c>WildEncounters_TryFishingEncounter</c>: the rod's rate, then a slot), something bites after one
/// to four seconds, and the button must be pressed within the rod's moment: a second and a half with the Old Rod,
/// one with the Good Rod, half a second with the Super Rod. Pressed too soon, the line comes back empty. A function
/// of the time it is given and the presses, so tests play it through without a window.
/// </summary>
public sealed class FishingAttempt
{
    public const float CastSeconds = 34f / 30f, NothingSeconds = 4f, ReelSeconds = 15f / 30f;

    /// <summary>The moment the button must be pressed in, by rod (<c>sRodTypeHookTimingWindow</c>: 45, 30 and 15 frames).</summary>
    public static float HookSeconds(FishingRod rod) => rod switch { FishingRod.Old => 45f / 30f, FishingRod.Good => 30f / 30f, _ => 15f / 30f };

    private float clock;
    private readonly float biteAfter;

    public FishingRod Rod { get; }

    /// <summary>What will bite, decided as the rod is cast; null when nothing will.</summary>
    public WildEncounterEntry? Fish { get; }

    public FishingStage Stage { get; private set; } = FishingStage.Casting;

    /// <summary>The Pokémon reeled in, once it has been: the game then starts its battle.</summary>
    public WildEncounterEntry? Caught => Stage is FishingStage.Done && landed ? Fish : null;
    private bool landed;

    /// <param name="random">What decides how long the bite takes (the original's <c>(LCRNG_Next() % 4 + 1) * 30</c> frames).</param>
    public FishingAttempt(FishingRod rod, WildEncounterEntry? fish, Random random)
    {
        Rod = rod;
        Fish = fish;
        biteAfter = random.Next(4) + 1;
    }

    /// <summary>
    /// Time passes and perhaps the button is pressed. A stage that says something (landed, too soon, got away, no
    /// nibble) waits there for <see cref="Read"/>, as the original waits for its message to be read.
    /// </summary>
    public void Update(float dt, bool pressed)
    {
        clock += dt;
        switch (Stage)
        {
            case FishingStage.Casting:
                if (clock < CastSeconds) return;
                Next(FishingStage.Waiting);
                return;
            case FishingStage.Waiting:
                if (pressed)
                {
                    Next(FishingStage.TooSoon);
                    return;
                }
                if (clock >= (Fish != null ? biteAfter : NothingSeconds)) Next(Fish != null ? FishingStage.Hooked : FishingStage.NoNibble);
                return;
            case FishingStage.Hooked:
                if (pressed)
                {
                    landed = true;
                    Next(FishingStage.Landed);
                }
                else if (clock >= HookSeconds(Rod)) Next(FishingStage.GotAway);
                return;
        }
    }

    /// <summary>Whether the stage says something to the player, which is read before the rod is put away.</summary>
    public bool Says => Stage is FishingStage.Landed or FishingStage.TooSoon or FishingStage.GotAway or FishingStage.NoNibble;

    /// <summary>What is said, in our own words.</summary>
    public string Message => Stage switch
    {
        FishingStage.Landed => "Hooked one! Here it comes!",
        FishingStage.TooSoon => "The line came up too soon, with nothing on it.",
        FishingStage.GotAway => "Whatever it was, it slipped away.",
        FishingStage.NoNibble => "Nothing is biting.",
        _ => ""
    };

    /// <summary>The message has been read: the rod is put away.</summary>
    public void Read()
    {
        if (Says) Next(FishingStage.Done);
    }

    private void Next(FishingStage stage)
    {
        Stage = stage;
        clock = 0f;
    }

    /// <summary>
    /// Whether a rod can be cast where the player stands, by the original's check (<c>CanUseFishingRod</c>): the
    /// tile faced is water one could surf on, and the player isn't up on a bridge's deck over it.
    /// </summary>
    public static bool CanCast(Map map, int x, int y, Direction facing, float height)
    {
        var (dx, dy) = FieldMovement.Delta(facing);
        int nx = x + dx, ny = y + dy;
        if (!map.InBounds(nx, ny) || !TileBehaviors.IsSurfable(map.BehaviourAt(nx, ny))) return false;
        return !map.SurfaceAt(x, y, height).OnDeck;
    }

    /// <summary>The rod an item is, by the original's use of it in the field; null for anything else.</summary>
    public static FishingRod? RodOf(ItemData item) => item.FieldUse switch
    {
        "OldRod" => FishingRod.Old,
        "GoodRod" => FishingRod.Good,
        "SuperRod" => FishingRod.Super,
        _ => null
    };
}
