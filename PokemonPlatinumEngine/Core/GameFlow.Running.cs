using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models.SuperContests;
using PokemonPlatinumEngine.Models.Underground;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// What the field has going from one frame to the next (plan 16 · T11h): what a script set going and is waiting for,
/// a trainer walking up, a rod's cast, the people who wander, a trap in the Underground, a puzzle's turn, and what the
/// last arrival and the last battle left for the next step to read. None of it is saved but where a line says so. The
/// flow holds it, so a game with no window runs the field as the game does.
/// </summary>
public sealed partial class GameFlow
{
    /// <summary>A question's answers waiting for its line to be written out, and the answer its B button gives.</summary>
    public IReadOnlyList<string>? PendingAnswers { get; set; }
    public int PendingCancel { get; set; }

    /// <summary>The answer the script's last question, screen or chooser came back with.</summary>
    public int ScriptAnswer { get; set; }

    /// <summary>
    /// A battle the Vs. Recorder may keep (plan 08 · P10): its file, waiting for the field to come back to be asked
    /// about, and whether the question about it is the one on the screen.
    /// </summary>
    public BattleRecordFile? RecordOffered { get; set; }
    public bool RecordAsked { get; set; }

    /// <summary>The item the bag chose for a script's <c>chooseitem</c> (plan 06 · R14a).</summary>
    public string? ScriptItem { get; set; }

    /// <summary>What a script set going: people walking, the player walking, the screen gone black, the camera sent away.</summary>
    public List<NpcWalk> NpcWalks { get; } = new();
    public PlayerWalk? PlayerWalk { get; set; }
    public ScreenFade ScriptFade { get; } = new();
    public bool CameraSent { get; set; }

    /// <summary>The battle a script started: how it came out, and whether losing it is part of the story.</summary>
    public BattleOutcome ScriptOutcome { get; set; }
    public bool BattleMayBeLost { get; set; }

    /// <summary>The turns that battle went on for before its last (the original's VAR_TOTAL_TURNS_LAST_BATTLE; plan 01 · M11, part b).</summary>
    public int ScriptTurns { get; set; }

    /// <summary>The story as the maps last saw it, and whether the place the player came to has yet to run its own script.</summary>
    public int PresenceRevision { get; set; } = -1;
    public bool Arrived { get; set; }

    /// <summary>A step came into a new place, whose script runs first: the trigger under the player waits for it.</summary>
    public bool TriggerAfterEnter { get; set; }

    /// <summary>A field move being used (plan 02 · S2): its cut-in, and the obstacle it acts on as the band closes.</summary>
    public FieldCutIn? CutIn { get; set; }
    public NPC? CutInSubject { get; set; }

    /// <summary>A step a field move began (out onto the water, a climb), which the script waits for.</summary>
    public bool FieldMoveStep { get; set; }

    /// <summary>The town chosen on Fly's map, for the script that flies there.</summary>
    public SpawnLocation? FlyTarget { get; set; }

    /// <summary>Where the player was before the place they have come to: what the Journal says of leaving it.</summary>
    public string LeftMap { get; set; } = "";
    public string LeftPlace { get; set; } = "";
    public bool LeftCave { get; set; }

    /// <summary>The area of the overworld the player was last in; null on a small map, which is one place.</summary>
    public MapArea? CurrentArea { get; set; }

    /// <summary>The stretch of the area with music of its own the player was last in (plan 21 · H4: Route 111's desert); null outside one.</summary>
    public Data.OverlayZone? CurrentZone { get; set; }

    /// <summary>
    /// The tiles of ash-covered grass already brushed clean since the player came to the place (plan 21 · H4; Emerald's
    /// STEP_CB_ASH turns the grass bare until the map is loaded again): each gives the Soot Sack its ash once.
    /// </summary>
    public HashSet<(int X, int Y)> AshBrushed { get; } = new();

    /// <summary>A trainer who spotted the player and is walking up, and the second of two who saw the player at once (plan 02 · S6).</summary>
    public TrainerApproach? TrainerApproach { get; set; }
    public TrainerApproach? PairApproach { get; set; }

    /// <summary>The trainers of the battle under way.</summary>
    public List<NPC> BattleTrainers { get; } = new();

    /// <summary>The player came through a warp, onto the tile in front of it: a trigger there starts as a step onto it would.</summary>
    public bool SteppedOutOfWarp { get; set; }

    /// <summary>Set on arriving somewhere without walking (through a door, from a save, after a battle): trainers look once.</summary>
    public bool TrainersLookOnArrival { get; set; } = true;

    /// <summary>A rod's cast under way.</summary>
    public FishingAttempt? Fishing { get; set; }

    /// <summary>Steps since friendship last grew from walking, and the player's turns on the spot (one Pokémon evolves on a spin).</summary>
    public int FriendshipSteps { get; set; }
    public SpinTracker Spin { get; } = new();

    /// <summary>The people who move about of their own accord (plan 02 · S6).</summary>
    public Wandering Wandering { get; } = new();

    /// <summary>The Poké Radar's chain, which the original doesn't save either.</summary>
    public RadarChain Radar { get; } = new();

    /// <summary>
    /// Whether the battle under way was met in the field's grass, water or on a rod (not a script's, nor a honey
    /// tree's): only then do the roamers and the Poké Radar hear how it ended (FieldTask_WildEncounter).
    /// </summary>
    public bool FieldEncounter { get; set; }

    /// <summary>Whether the battle under way was hooked on a rod, and so is fought on the water (plan 06 · R13).</summary>
    public bool Hooked { get; set; }

    /// <summary>The day the clock was last looked at, for Pokérus's days (saved).</summary>
    public DateTime? LastDay { get; set; }

    /// <summary>The Eterna Gym's clock turning on, which the script that turned it waits for, and its ticks so far.</summary>
    public EternaClock.Turn? ClockTurn { get; set; }
    public int ClockTicks { get; set; }

    /// <summary>How the last contest came out (<c>supercontest ribbon</c> and <c>prize</c> ask it); nothing is kept of it in a save.</summary>
    public ContestOutcome? ContestOutcome { get; set; }

    /// <summary>The places in the team of the Pokémon entered for a Frontier challenge, in the order chosen.</summary>
    public IReadOnlyList<int> EnteredSlots { get; set; } = Array.Empty<int>();

    /// <summary>The Underground's tunnels as the player came down into them.</summary>
    public UndergroundField? Tunnels { get; set; }

    /// <summary>The trap that has the player, and the frames of it still owed at the console's thirty a second.</summary>
    public TrapEffect? Trap { get; set; }
    public float TrapFrames { get; set; }

    /// <summary>The day's other diggers.</summary>
    public List<Digger> Diggers { get; set; } = new();
}
