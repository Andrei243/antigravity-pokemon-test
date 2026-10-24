using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// Field moves without a party member who knows them (plan 12 · Q18): the eight moves Platinum's HMs teach (Cut, Fly,
/// Surf, Strength, Defog, Rock Smash, Waterfall, Rock Climb) can be used with a wild helper of the game's own once the
/// region's Badge for the move is held and the HM that teaches it is in the bag, as Brilliant Diamond and Shining Pearl
/// answered Sinnoh's HMs. GPU-free, and nothing is stored: the grants are worked out from the story, the bag, the
/// options' row and the rules whenever they are asked, so switching the row off takes them back and a save holds none.
/// Flash, Teleport, Dig, Sweet Scent and the moves that share HP stay with the Pokémon that know them.
/// </summary>
public static class FieldHelp
{
    /// <summary>The moves a helper can do, in the field moves' order.</summary>
    public static readonly IReadOnlyList<FieldMove> Helped = new[]
    {
        FieldMove.Cut, FieldMove.Fly, FieldMove.Surf, FieldMove.Strength, FieldMove.Defog, FieldMove.RockSmash, FieldMove.Waterfall, FieldMove.RockClimb
    };

    /// <summary>Whether the row gives helpers in a game played by these rules.</summary>
    public static bool On(RulesDefault row, RulesPreset preset) => row.Holds(preset);

    /// <summary>The HM that teaches a move, if the data has one.</summary>
    public static ItemData? HmOf(FieldMove move)
    {
        string name = FieldMoveRules.MoveName(move);
        return ItemDatabase.GetAll().FirstOrDefault(i => i.TeachesMove == name && i.Name.StartsWith("HM", StringComparison.Ordinal));
    }

    /// <summary>
    /// The moves granted now: those of <see cref="Helped"/> whose Badge in the region is held and whose HM is in the bag.
    /// Empty when the row is off for these rules.
    /// </summary>
    public static IReadOnlyList<FieldMove> Granted(RulesDefault row, RulesPreset preset, StoryState story, Inventory bag, string region)
    {
        if (!On(row, preset)) return Array.Empty<FieldMove>();
        var granted = new List<FieldMove>();
        foreach (var move in Helped)
        {
            if (FieldMoveRules.BadgeFor(move, region) is { } badge && !story.HasBadge(region, badge)) continue;
            if (HmOf(move) is not { } hm || bag.GetQuantity(hm) <= 0) continue;
            granted.Add(move);
        }
        return granted;
    }

    /// <summary>What the granted moves let the player do in the field, as <see cref="FieldMovement.MovesOf"/> says for a team.</summary>
    public static FieldMoves Moves(IEnumerable<FieldMove> granted)
    {
        var moves = FieldMoves.None;
        foreach (var move in granted)
            moves |= move switch
            {
                FieldMove.Surf => FieldMoves.Surf,
                FieldMove.Waterfall => FieldMoves.Waterfall,
                FieldMove.RockClimb => FieldMoves.RockClimb,
                _ => FieldMoves.None
            };
        return moves;
    }

    /// <summary>Whether a move can be done now: a Pokémon of the team knows it, or it is granted.</summary>
    public static bool CanUse(FieldMove move, Party party, IReadOnlyCollection<FieldMove> granted) =>
        FieldMoveRules.Knower(party, move) != null || granted.Contains(move);

    /// <summary>The wild species that stands in for a move's user (a Bidoof, a Bibarel or a Staravia, after the remake's app).</summary>
    public static string SpeciesFor(FieldMove move) => move switch
    {
        FieldMove.Fly => "Staravia",
        FieldMove.Surf or FieldMove.Waterfall => "Bibarel",
        _ => "Bidoof"
    };

    /// <summary>
    /// The helper that does a granted move: a Pokémon of the species for it that knows the move, made from a generator
    /// of its own (a fixed seed per move), so that nothing is drawn from <see cref="Dice"/> and the same helper comes every time.
    /// </summary>
    public static Pokemon HelperFor(FieldMove move)
    {
        var species = PokemonDatabase.Get(SpeciesFor(move)) ?? throw new InvalidOperationException("no species for a field helper");
        var helper = new Pokemon(species, 20, new Random(7000 + (int)move));
        string name = FieldMoveRules.MoveName(move);
        if (!helper.Moves.Any(m => m.Name == name) && MoveDatabase.Get(name) is { } data)
        {
            if (helper.Moves.Count >= 4) helper.Moves.RemoveAt(3);
            helper.Moves.Add(new Move(data));
        }
        return helper;
    }
}
