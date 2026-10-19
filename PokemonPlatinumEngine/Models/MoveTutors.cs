using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>A move one of the three shard tutors teaches, and the Shards it costs (the original's <c>res/pokemon/move_tutors.json</c>).</summary>
public sealed record TutorMove(string Move, int Red, int Blue, int Yellow, int Green)
{
    /// <summary>The price as it is listed: "2 Red, 4 Blue, 2 Yellow".</summary>
    public string Price => string.Join(", ", new[] { (Red, "Red"), (Blue, "Blue"), (Yellow, "Yellow"), (Green, "Green") }
        .Where(c => c.Item1 > 0).Select(c => $"{c.Item1} {c.Item2}"));
}

/// <summary>Why a tutor won't teach a Pokémon a move, or that they will.</summary>
public enum TutorAnswer { Able, Nobody, Egg, NotAble, Knows, NotClose }

/// <summary>
/// The move tutors of Platinum (plan 01 · M11, part d; no drawing and no input): the three who teach for Shards
/// (Route 212's, Snowpoint City's and the Survival Area's, each with moves of their own and a price in Shards of four
/// colours: the original's <c>MoveTutor</c> table), Grandma Wilma on Route 210, who teaches Draco Meteor to a
/// Dragon-type Pokémon that trusts its Trainer fully, and the old man of Route 228, who teaches the three strongest
/// moves of the starters to the twelve of them fully grown, on the same condition. Who can learn a tutor's move is
/// the species' own list (<see cref="PokemonSpecies.TutorMoves"/>); the four moves the last two teach are on no list,
/// as in the original, whose scripts ask the type and the species themselves. Teaching is
/// <see cref="MoveTeaching.Learn"/>, as a TM's is.
/// </summary>
public static class MoveTutors
{
    public const string Route212 = "route_212", Snowpoint = "snowpoint_city", SurvivalArea = "survival_area";

    private static readonly Dictionary<string, TutorMove[]> ByPlace = new()
    {
        // TUTOR_LOCATION_ROUTE_212: the Blue Shards' tutor
        [Route212] = new TutorMove[]
        {
            new("Dive", 2, 4, 2, 0), new("Fury Cutter", 0, 8, 0, 0), new("Icy Wind", 0, 6, 0, 2), new("Thunder Punch", 2, 6, 0, 0),
            new("Fire Punch", 2, 6, 0, 0), new("Ice Punch", 2, 6, 0, 0), new("Ominous Wind", 0, 6, 0, 2), new("Air Cutter", 2, 4, 0, 2),
            new("Zen Headbutt", 0, 4, 4, 0), new("Vacuum Wave", 2, 4, 0, 2), new("Trick", 0, 4, 4, 0), new("Knock Off", 4, 4, 0, 0),
            new("Sucker Punch", 0, 6, 2, 0),
        },
        // TUTOR_LOCATION_SURVIVAL_AREA: the Red Shards' tutor
        [SurvivalArea] = new TutorMove[]
        {
            new("Mud-Slap", 4, 4, 0, 0), new("Rollout", 4, 2, 0, 2), new("Superpower", 8, 0, 0, 0), new("Iron Head", 6, 0, 2, 0),
            new("Aqua Tail", 6, 0, 0, 2), new("Gastro Acid", 4, 0, 2, 2), new("Endeavor", 4, 0, 4, 0), new("Outrage", 6, 0, 2, 0),
            new("Ancient Power", 6, 0, 0, 2), new("Signal Beam", 2, 2, 2, 2), new("Earth Power", 6, 0, 0, 2), new("Gunk Shot", 4, 2, 0, 2),
            new("Twister", 6, 0, 0, 2), new("Seed Bomb", 4, 0, 0, 4), new("Iron Defense", 4, 2, 2, 0), new("Bounce", 4, 0, 2, 2),
            new("Heat Wave", 4, 2, 0, 2),
        },
        // TUTOR_LOCATION_SNOWPOINT_CITY: the Yellow and Green Shards' tutor
        [Snowpoint] = new TutorMove[]
        {
            new("Snore", 2, 0, 4, 2), new("Spite", 0, 0, 8, 0), new("Helping Hand", 2, 0, 4, 2), new("Synthesis", 0, 0, 2, 6),
            new("Magnet Rise", 0, 2, 4, 2), new("Last Resort", 0, 0, 0, 8), new("Swift", 0, 2, 2, 4), new("Uproar", 0, 0, 6, 2),
        },
    };

    /// <summary>The places that have a shard tutor.</summary>
    public static IReadOnlyCollection<string> Places => ByPlace.Keys;

    /// <summary>Every move a place's tutor teaches, in the original's order.</summary>
    public static IReadOnlyList<TutorMove> At(string place) =>
        ByPlace.TryGetValue(place, out var moves) ? moves : throw new ArgumentException($"No shard tutor lives at '{place}'", nameof(place));

    /// <summary>
    /// The moves a place's tutor can teach this Pokémon (the original's <c>CheckHasLearnableTutorMoves</c> and its
    /// list): those on its species' list that it doesn't know. An Egg learns none.
    /// </summary>
    public static List<TutorMove> Teachable(Pokemon p, string place) =>
        p.IsEgg ? new() : At(place).Where(m => p.Species.TutorMoves?.Contains(m.Move) == true && !p.Knows(m.Move)).ToList();

    private static IEnumerable<(string Shard, int Count)> Cost(TutorMove move) =>
        new[] { ("Red Shard", move.Red), ("Blue Shard", move.Blue), ("Yellow Shard", move.Yellow), ("Green Shard", move.Green) }.Where(c => c.Item2 > 0);

    /// <summary>Whether the bag holds the Shards a move costs (<c>CheckCanAffordMove</c>).</summary>
    public static bool CanAfford(Inventory bag, TutorMove move) => Cost(move).All(c => bag.GetQuantity(ItemDatabase.Get(c.Shard)!) >= c.Count);

    /// <summary>Takes the Shards a move costs (<c>PayShardCost</c>); false, and nothing taken, when the bag hasn't them all.</summary>
    public static bool Pay(Inventory bag, TutorMove move)
    {
        if (!CanAfford(bag, move)) return false;
        foreach (var (shard, count) in Cost(move)) bag.RemoveItem(ItemDatabase.Get(shard)!, count);
        return true;
    }

    public const string DracoMeteor = "Draco Meteor";

    /// <summary>The friendship the two tutors of the strongest moves ask (the original's <c>MAX_FRIENDSHIP_VALUE</c>).</summary>
    public const int FullTrust = 255;

    /// <summary>
    /// Grandma Wilma's answer (the original's Route 210 script): a Dragon-type Pokémon that doesn't know Draco Meteor
    /// and trusts its Trainer fully.
    /// </summary>
    public static TutorAnswer ForDracoMeteor(Pokemon? p) =>
        p == null ? TutorAnswer.Nobody
        : p.IsEgg ? TutorAnswer.Egg
        : !p.HasType(PokemonType.Dragon) ? TutorAnswer.NotAble
        : p.Knows(DracoMeteor) ? TutorAnswer.Knows
        : p.Friendship < FullTrust ? TutorAnswer.NotClose
        : TutorAnswer.Able;

    private static readonly Dictionary<string, string> Ultimate = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Charizard"] = "Blast Burn", ["Typhlosion"] = "Blast Burn", ["Blaziken"] = "Blast Burn", ["Infernape"] = "Blast Burn",
        ["Blastoise"] = "Hydro Cannon", ["Feraligatr"] = "Hydro Cannon", ["Swampert"] = "Hydro Cannon", ["Empoleon"] = "Hydro Cannon",
        ["Venusaur"] = "Frenzy Plant", ["Meganium"] = "Frenzy Plant", ["Sceptile"] = "Frenzy Plant", ["Torterra"] = "Frenzy Plant",
    };

    /// <summary>The twelve the old man of Route 228 teaches, as his poster lists them.</summary>
    public static IReadOnlyCollection<string> UltimateSpecies => Ultimate.Keys;

    /// <summary>The strongest move of a starter fully grown (the original's <c>CheckFinalStageStarter</c>), or null for any other Pokémon.</summary>
    public static string? UltimateMoveOf(Pokemon p) => p.IsEgg ? null : Ultimate.GetValueOrDefault(p.Species.Name);

    /// <summary>The strongest move of a species of the twelve, or null for any other (plan 14 · B14's legality check).</summary>
    public static string? UltimateMoveOf(string species) => Ultimate.GetValueOrDefault(species);

    /// <summary>The old man's answer (the original's Route 228 script): one of the twelve, that doesn't know its move and trusts its Trainer fully.</summary>
    public static TutorAnswer ForUltimateMove(Pokemon? p) =>
        p == null ? TutorAnswer.Nobody
        : p.IsEgg ? TutorAnswer.Egg
        : UltimateMoveOf(p) is not { } move ? TutorAnswer.NotAble
        : p.Knows(move) ? TutorAnswer.Knows
        : p.Friendship < FullTrust ? TutorAnswer.NotClose
        : TutorAnswer.Able;

    /// <summary>
    /// Kanto's tutors of one move each (plan 19 · K5; <c>docs/mechanics/rulings.md</c>, "Kanto's tutors"), as FireRed
    /// places them: each teaches its move free, to one Pokémon, once in a game. FireRed's own list of who may learn a
    /// tutor's move is not read (it is outside what the importer fetches), so the table is ours: a Pokémon may learn the
    /// move if its species could learn the move itself by Platinum's data, or could learn the move of Platinum's machine
    /// named beside it, the nearest kin of the move among the machines (a punch or a kick for one who can learn Focus
    /// Punch, Rock Slide for one who can learn it from TM80).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> Kanto = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Route 4's two Black Belts (FireRed's Route4_EventScript_MegaPunchTutor, _MegaKickTutor)
        ["Mega Punch"] = "Focus Punch",
        ["Mega Kick"] = "Focus Punch",
        // Rock Tunnel's youngster (RockTunnel_B1F_EventScript_RockSlideTutor)
        ["Rock Slide"] = "Rock Slide",
        // Fuchsia City's boy by the Mart (FireRed's FuchsiaCity_EventScript_SubstituteTutor; plan 19 · K6)
        ["Substitute"] = "Substitute",
        // The researcher in Cinnabar Island's lab (FireRed's CinnabarIsland_PokemonLab_ResearchRoom_EventScript_
        // MetronomeTutor; plan 19 · K6, part b). Platinum has no machine of Metronome: its nearest kin among them is
        // Psych Up (TM77), another move that takes on what isn't the user's own
        ["Metronome"] = "Psych Up",
    };

    /// <summary>The flag a Kanto tutor sets once their move is taught (FireRed's <c>FLAG_TUTOR_MEGA_PUNCH</c>).</summary>
    public static string KantoFlag(string move) =>
        "FLAG_TUTOR_" + new string(move.ToUpperInvariant().Select(c => char.IsLetter(c) ? c : '_').ToArray());

    /// <summary>A Kanto tutor's answer for a Pokémon: whether it may learn their move (ours: <see cref="Kanto"/>).</summary>
    public static TutorAnswer ForKanto(Pokemon? p, string move) =>
        p == null ? TutorAnswer.Nobody
        : p.IsEgg ? TutorAnswer.Egg
        : p.Knows(move) ? TutorAnswer.Knows
        : !Kanto.TryGetValue(move, out var kin) || !(Legality.CanLearn(p.Species, move) || Legality.CanLearn(p.Species, kin)) ? TutorAnswer.NotAble
        : TutorAnswer.Able;

    /// <summary>
    /// The Move Deleter's rule (the original's Canalave script): any move may go, an HM's too, but a Pokémon's last.
    /// </summary>
    public static bool CanForget(Pokemon p) => !p.IsEgg && p.Moves.Count > 1;

    /// <summary>Takes the move in a place out of a Pokémon's moves (<c>ForgetMove</c>) and says which it was.</summary>
    public static string Forget(Pokemon p, int slot)
    {
        if (!CanForget(p) || slot < 0 || slot >= p.Moves.Count) throw new ArgumentOutOfRangeException(nameof(slot), "A Pokémon keeps its last move");
        string name = p.Moves[slot].Name;
        p.Moves.RemoveAt(slot);
        return name;
    }
}
