using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The moves a Pokémon can use outside battle (plan 02 · S2), in the original's order (<c>FieldMoveList</c> in
/// <c>field_move_tasks.h</c>), which is also the order the party menu offers them in when one Pokémon knows several.
/// </summary>
public enum FieldMove { Cut, Fly, Surf, Strength, Defog, RockSmash, Waterfall, RockClimb, Flash, Teleport, Dig, SweetScent, Chatter, MilkDrink, Softboiled }

/// <summary>Why a field move can't be used here and now: the original's <c>FieldMoveError</c>.</summary>
public enum FieldMoveError
{
    None,
    /// <summary>Nothing here for it: no tree in front, no water, no fog, a town for Teleport.</summary>
    Location,
    /// <summary>The badge that lets it be used outside battle is missing.</summary>
    Badge,
    /// <summary>Someone is walking with the player (a partner of the story's), who can't follow.</summary>
    Partner,
    /// <summary>The player is already doing it: surfing, for Surf.</summary>
    State
}

/// <summary>
/// Where the player stands, as far as the field moves care: what lies in front, the place's weather and what the
/// place allows. The original works the same out in <c>FieldMoves_SetUsableMoves</c> and from the map's header;
/// the game fills one in from the map (<see cref="FieldMoveRules.SpotOf"/>), and a test writes one by hand.
/// </summary>
public sealed record FieldSpot
{
    /// <summary>The obstacle right in front of the player, if there is one.</summary>
    public PropType? Obstacle { get; init; }

    /// <summary>Deep water in front that the player could set out onto.</summary>
    public bool Water { get; init; }

    /// <summary>A rock face in front, along its grain.</summary>
    public bool RockFace { get; init; }

    /// <summary>A waterfall in front.</summary>
    public bool Waterfall { get; init; }

    /// <summary>The place lies in fog that nobody has blown away.</summary>
    public bool Fog { get; init; }

    /// <summary>The place is a dark cave that nobody has lit.</summary>
    public bool Dark { get; init; }

    public bool Surfing { get; init; }

    /// <summary>The place's header lets one fly away from it (and so Teleport too, outside a town).</summary>
    public bool FlyAllowed { get; init; }

    /// <summary>A town or a city: Teleport isn't used there.</summary>
    public bool Town { get; init; }

    /// <summary>A cave whose header lets an Escape Rope be used in it: where Dig leads out.</summary>
    public bool CaveWithAWayOut { get; init; }

    /// <summary>Someone of the story is walking with the player.</summary>
    public bool Partner { get; init; }
}

/// <summary>
/// The rules of the field moves (plan 02 · S2): which a Pokémon knows, the badge each needs, and whether one can be
/// used where the player stands, by the original's checks in <c>src/field_move_tasks.c</c>. No drawing and no input:
/// the party menu asks it what to offer, the field asks it before it offers to cut a tree, and tests ask it.
/// </summary>
public static class FieldMoveRules
{
    /// <summary>
    /// The original's flags for what a field move leaves in force (<c>system_flags.c</c>): boulders that can be
    /// pushed, a dark cave lit, fog blown away. Scripts set them (<c>setflag FLAG_STRENGTH_ACTIVE</c>) and ask them.
    /// </summary>
    public const string StrengthFlag = "FLAG_STRENGTH_ACTIVE", FlashFlag = "FLAG_FLASH_ACTIVE", DefogFlag = "FLAG_DEFOG_ACTIVE";

    /// <summary>
    /// What is forgotten when the player comes to another place, by a step into another area or a warp
    /// (<c>FieldMapChange_UpdateGameData</c>): the place's local flags (a tree cut down grows back), Strength
    /// (<c>FieldSystem_InitFlagsOnMapChange</c>), and Flash and Defog unless the place is a cave too, so a cave's
    /// floors stay lit from one to the next (<c>field_map_change_flags.c</c>).
    /// </summary>
    public static void LeavePlace(StoryState story, bool intoCave)
    {
        story.ClearLocal();
        story.Unset(StrengthFlag);
        if (intoCave) return;
        story.Unset(FlashFlag);
        story.Unset(DefogFlag);
    }

    /// <summary>
    /// The field move that a move of battle is, by its name; Chatter is left out (in the original it records the
    /// player's voice through the handheld's microphone, which this game hasn't got: <c>docs/mechanics/rulings.md</c>).
    /// </summary>
    public static FieldMove? Of(string moveName) => moveName switch
    {
        "Cut" => FieldMove.Cut,
        "Fly" => FieldMove.Fly,
        "Surf" => FieldMove.Surf,
        "Strength" => FieldMove.Strength,
        "Defog" => FieldMove.Defog,
        "Rock Smash" => FieldMove.RockSmash,
        "Waterfall" => FieldMove.Waterfall,
        "Rock Climb" => FieldMove.RockClimb,
        "Flash" => FieldMove.Flash,
        "Teleport" => FieldMove.Teleport,
        "Dig" => FieldMove.Dig,
        "Sweet Scent" => FieldMove.SweetScent,
        "Milk Drink" => FieldMove.MilkDrink,
        "Soft-Boiled" => FieldMove.Softboiled,
        _ => null
    };

    /// <summary>The move's name as the move data has it.</summary>
    public static string MoveName(FieldMove move) => move switch
    {
        FieldMove.RockSmash => "Rock Smash",
        FieldMove.RockClimb => "Rock Climb",
        FieldMove.SweetScent => "Sweet Scent",
        FieldMove.MilkDrink => "Milk Drink",
        FieldMove.Softboiled => "Soft-Boiled",
        _ => move.ToString()
    };

    /// <summary>The badge a move needs before it can be used outside battle; null for the moves that need none.</summary>
    public static Badge? BadgeFor(FieldMove move) => move switch
    {
        FieldMove.Cut => Badge.Forest,
        FieldMove.Fly => Badge.Cobble,
        FieldMove.Surf => Badge.Fen,
        FieldMove.Strength => Badge.Mine,
        FieldMove.Defog => Badge.Relic,
        FieldMove.RockSmash => Badge.Coal,
        FieldMove.Waterfall => Badge.Beacon,
        FieldMove.RockClimb => Badge.Icicle,
        _ => null
    };

    /// <summary>The field moves a Pokémon knows, in the order of its moves, as the party menu lists them.</summary>
    public static IEnumerable<FieldMove> Known(Pokemon pokemon)
    {
        foreach (var move in pokemon.Moves)
            if (Of(move.Name) is { } field) yield return field;
    }

    /// <summary>The first Pokémon of the team that knows a move (the original's <c>FindPartySlotWithMove</c>), fainted or not.</summary>
    public static Pokemon? Knower(Party party, FieldMove move) =>
        party.Members.FirstOrDefault(p => p.Moves.Any(m => m.Name == MoveName(move)));

    /// <summary>
    /// Whether a move can be used where the player stands, by the original's check for it (<c>FieldMoves_Check*</c>):
    /// the badge first, then the place. Milk Drink and Soft-Boiled are judged in the party menu itself
    /// (<see cref="CanShareHp"/>).
    /// </summary>
    public static FieldMoveError Check(FieldMove move, FieldSpot spot, StoryState story)
    {
        if (BadgeFor(move) is { } badge && !story.HasBadge(badge)) return FieldMoveError.Badge;
        switch (move)
        {
            case FieldMove.Cut:
                return spot.Obstacle == PropType.CutTree ? FieldMoveError.None : FieldMoveError.Location;
            case FieldMove.RockSmash:
                return spot.Surfing || spot.Obstacle != PropType.CrackedRock ? FieldMoveError.Location : FieldMoveError.None;
            case FieldMove.Strength:
                return spot.Obstacle == PropType.StrengthBoulder ? FieldMoveError.None : FieldMoveError.Location;
            case FieldMove.Fly:
                if (!spot.FlyAllowed) return FieldMoveError.Location;
                return spot.Partner ? FieldMoveError.Partner : FieldMoveError.None;
            case FieldMove.Surf:
                if (spot.Surfing) return FieldMoveError.State;
                if (!spot.Water) return FieldMoveError.Location;
                return spot.Partner ? FieldMoveError.Partner : FieldMoveError.None;
            case FieldMove.Defog:
                return spot.Fog ? FieldMoveError.None : FieldMoveError.Location;
            case FieldMove.Waterfall:
                return spot.Waterfall ? FieldMoveError.None : FieldMoveError.Location;
            case FieldMove.RockClimb:
                if (!spot.RockFace) return FieldMoveError.Location;
                return spot.Partner ? FieldMoveError.Partner : FieldMoveError.None;
            case FieldMove.Flash:
                return spot.Dark ? FieldMoveError.None : FieldMoveError.Location;
            case FieldMove.Teleport:
                if (!spot.FlyAllowed || spot.Town) return FieldMoveError.Location;
                return spot.Partner ? FieldMoveError.Partner : FieldMoveError.None;
            case FieldMove.Dig:
                if (!spot.CaveWithAWayOut) return FieldMoveError.Location;
                return spot.Partner ? FieldMoveError.Partner : FieldMoveError.None;
            case FieldMove.SweetScent:
                return FieldMoveError.None;
            default:
                // Milk Drink and Soft-Boiled are used on another Pokémon of the team, from the party menu
                return FieldMoveError.None;
        }
    }

    /// <summary>What the party menu says when a field move can't be used, in our own words.</summary>
    public static string Why(FieldMoveError error) => error switch
    {
        FieldMoveError.Badge => "A new Badge is needed before it can be used outside battle.",
        FieldMoveError.Partner => "Not while someone is travelling with you.",
        FieldMoveError.State => "That's already being done.",
        _ => "There's nothing here to use it on."
    };

    /// <summary>
    /// Whether a Pokémon has the HP to give some away with Milk Drink or Soft-Boiled: more than a fifth of its
    /// most (<c>PartyMenu_StartFieldMoveHPTransfer</c>).
    /// </summary>
    public static bool CanShareHp(Pokemon giver) => giver.CurrentHP > giver.MaxHP / 5;

    /// <summary>
    /// Milk Drink or Soft-Boiled from one Pokémon to another: a fifth of the giver's most HP, or what the other is
    /// missing if that is less. Zero, with nothing changed, when it can't be done: itself, an Egg, a fainted
    /// Pokémon or one already at full health (<c>CheckCanUseHPTransferFieldMove</c>), or a giver too weak.
    /// </summary>
    public static int ShareHp(Pokemon giver, Pokemon taker)
    {
        if (!CanShareHp(giver) || taker == giver || taker.CurrentHP == 0 || taker.CurrentHP >= taker.MaxHP) return 0;
        int amount = Math.Min(giver.MaxHP / 5, taker.MaxHP - taker.CurrentHP);
        giver.CurrentHP -= amount;
        taker.CurrentHP += amount;
        return amount;
    }

    /// <summary>
    /// What lies where the player stands and in front of them, for <see cref="Check"/>: the obstacle, the water, a
    /// rock face or a waterfall ahead, and the place's weather and rules (fog that Defog has lifted reads as clear,
    /// a dark place Flash has lit as lit: <see cref="Map.FogLifted"/>, <see cref="Map.Lit"/>).
    /// </summary>
    public static FieldSpot SpotOf(Map map, int x, int y, Direction facing, Walker walker)
    {
        var (dx, dy) = FieldMovement.Delta(facing);
        int nx = x + dx, ny = y + dy;
        var area = map.AreaAt(x, y);
        var ahead = map.InBounds(nx, ny) ? map.BehaviourAt(nx, ny) : TileBehavior.None;
        var obstacle = map.InBounds(nx, ny) ? map.NpcIn(nx, ny, map.SurfaceAt(nx, ny, walker.Height).Height)?.Obstacle : null;
        return new FieldSpot
        {
            Obstacle = obstacle,
            Water = FieldMovement.CanStartSurf(map, x, y, facing, walker),
            RockFace = FieldMovement.IsRockFace(ahead, facing),
            Waterfall = ahead == TileBehavior.Waterfall,
            Fog = map.WeatherAt(x, y) == FieldWeather.Fog,
            Dark = Darkness.Covers(map),
            Surfing = walker.Mode == TravelMode.Surfing,
            FlyAllowed = area?.FlyAllowed ?? !map.IsIndoors,
            Town = area?.IsTown ?? false,
            CaveWithAWayOut = (area?.IsCave ?? map.Setting == MapSetting.Cave) && (area?.EscapeRopeAllowed ?? false)
        };
    }
}
