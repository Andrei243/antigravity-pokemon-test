using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// What is left here (plan 12 · Q22): the counts of the area underfoot, for the location sign and the start menu, worked
/// out from <see cref="AreaChecklist"/> whenever they are shown and stored nowhere.
/// </summary>
public partial class GameEngine
{
    /// <summary>The obstacles, the field move that clears each, for what the team can clear now.</summary>
    private static readonly (PropType Obstacle, FieldMove Move)[] Clearing =
    {
        (PropType.CutTree, FieldMove.Cut), (PropType.CrackedRock, FieldMove.RockSmash), (PropType.StrengthBoulder, FieldMove.Strength)
    };

    /// <summary>What the player has, as the checklist asks it: the story, the team's field moves, the obstacles it can clear (the Badge held), the rods and the Pokédex.</summary>
    internal ChecklistTerms ChecklistTermsNow()
    {
        var granted = GrantedMoves;
        string region = Here.Id;
        var clears = new HashSet<PropType>();
        foreach (var (obstacle, move) in Clearing)
            if (FieldHelp.CanUse(move, flow.Party, granted)
                && (FieldMoveRules.BadgeFor(move, region) is not { } badge || flow.Story.HasBadge(region, badge)))
                clears.Add(obstacle);
        var rods = new HashSet<FishingRod>();
        foreach (var stack in flow.Bag.AllItems)
            if (stack.Quantity > 0 && FishingAttempt.RodOf(stack.Data) is { } rod) rods.Add(rod);
        return new ChecklistTerms
        {
            Story = flow.Story,
            Moves = FieldMovement.MovesOf(flow.Party) | FieldHelp.Moves(granted),
            Clears = clears,
            Rods = rods,
            Caught = flow.Pokedex.IsCaught
        };
    }

    /// <summary>The counts for where the player stands now, or null with the options' row off or no game in force.</summary>
    internal AreaCounts? AreaCountsNow()
    {
        if (!Settings.AreaChecklist || flow.Map == null || flow.Player == null) return null;
        return AreaChecklist.Count(flow.Map, flow.Player.GridX, flow.Player.GridY, flow.Player.HeightOn(flow.Map), flow.Player.Mode, ChecklistTermsNow());
    }

    /// <summary>The counts' line for the sign and the start menu, or null with the row off.</summary>
    internal string? AreaCountsLine() => AreaCountsNow()?.Line;
}
