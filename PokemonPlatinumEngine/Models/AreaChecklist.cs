using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// What is left in a place (plan 12 · Q22): the items in their balls and in the ground, the trainers not yet beaten and
/// the species of its tables not yet caught. Three numbers, never names: they hint without telling.
/// </summary>
public readonly record struct AreaCounts(int Items, int Trainers, int Species)
{
    /// <summary>True when nothing of any of the three is left.</summary>
    public bool Nothing => Items == 0 && Trainers == 0 && Species == 0;

    /// <summary>The counts as the sign and the start menu say them: "Items 2 · Trainers 3 · Pokémon 5", or that nothing is left.</summary>
    public string Line => Nothing ? Core.Text.Of("leftHere.nothing")
        : Core.Text.Of("leftHere.line", ("items", Items), ("trainers", Trainers), ("species", Species));
}

/// <summary>
/// What a player has, for <see cref="AreaChecklist"/>: the story (flags, the trainers beaten), what their team can do in the
/// field, which obstacles it can clear now, the rods in the bag and what the Pokédex has caught. Plain values, so a test
/// makes one with no game.
/// </summary>
public sealed class ChecklistTerms
{
    public required Story.StoryState Story { get; init; }

    /// <summary>What the team does that changes where one can go (Surf, Waterfall, Rock Climb…): the player's own <see cref="Walker.Moves"/>.</summary>
    public FieldMoves Moves { get; init; }

    /// <summary>The obstacles the team (or a helper of Q18's) can clear now, the move's Badge held: a cut tree, a cracked rock, a boulder.</summary>
    public IReadOnlySet<PropType> Clears { get; init; } = new HashSet<PropType>();

    /// <summary>The rods in the bag.</summary>
    public IReadOnlySet<FishingRod> Rods { get; init; } = new HashSet<FishingRod>();

    /// <summary>Whether the Pokédex has a species, by its National number, as caught.</summary>
    public Func<int, bool> Caught { get; init; } = _ => false;
}

/// <summary>
/// Counts what can still be had in the area underfoot (plan 12 · Q22). Only what can be had now counts: the area's tiles
/// the player can come to from where they stand by the field's own steps (<see cref="FieldMovement.Step"/>, with the
/// climbs they would say yes to), past item balls (picked up on the way) and the obstacles the team can clear now; the
/// water's table only with Surf, a rod's only with that rod in the bag. Something the overlay holds back is never on the
/// map and is never counted. GPU-free: worked out whenever asked, and stored nowhere.
/// </summary>
public static class AreaChecklist
{
    private static readonly Direction[] Ways = { Direction.Up, Direction.Right, Direction.Down, Direction.Left };

    /// <summary>The most tiles a walk visits, so that no place, however large, costs more than a few milliseconds.</summary>
    public const int MostTiles = 60_000;

    /// <summary>The counts for the area of the tile (<paramref name="x"/>, <paramref name="y"/>), for someone standing there.</summary>
    public static AreaCounts Count(Map map, int x, int y, float height, TravelMode mode, ChecklistTerms terms)
    {
        var area = map.AreaAt(x, y);
        var (reached, faced) = Reach(map, x, y, height, mode, terms);
        bool InArea(int tx, int ty) => area == null || map.AreaAt(tx, ty) == area;
        bool Near(int tx, int ty) => reached.Contains((tx, ty)) || faced.Contains((tx, ty));
        var story = terms.Story;

        // Items in their balls, and in the ground
        int items = 0;
        foreach (var person in map.Everyone)
            if (person.IsItemBall && Present(person, story) && InArea(person.GridX, person.GridY) && Near(person.GridX, person.GridY)) items++;
        foreach (var ((hx, hy), hidden) in map.HiddenItems)
            if (!story.Has(hidden.Flag) && InArea(hx, hy) && Near(hx, hy)) items++;

        // Trainers not yet beaten: a pair that battles as one (twins) is one trainer
        var trainers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var person in map.Everyone)
        {
            if (!person.IsTrainer || person.TrainerData is not { } trainer || !Present(person, story)) continue;
            string id = trainer.Id.Length > 0 ? trainer.Id : person.Name;
            if (trainer.IsDefeated || story.HasDefeated(id)) continue;
            if (InArea(person.GridX, person.GridY) && Near(person.GridX, person.GridY)) trainers.Add(id);
        }

        // Species of the tables that can be fished, surfed or walked for now
        var species = new HashSet<int>();
        void Table(IEnumerable<WildEncounterEntry> table)
        {
            foreach (var entry in table)
                foreach (string? name in new[] { entry.SpeciesName, entry.DaySpecies, entry.NightSpecies })
                    if (name != null && PokemonDatabase.Get(name) is { } kind && !terms.Caught(kind.DexNumber)) species.Add(kind.DexNumber);
        }
        if (area != null)
        {
            Table(area.WildEncounters);
            if (terms.Moves.HasFlag(FieldMoves.Surf)) Table(area.WaterEncounters);
            foreach (var rod in terms.Rods) Table(area.RodEncounters[(int)rod]);
        }
        else Table(map.WildEncounters);

        return new AreaCounts(items, trainers.Count, species.Count);
    }

    /// <summary>Whether someone of the map is on it as the story stands: not hidden by their flag, and shown by theirs if they have one.</summary>
    public static bool Present(NPC person, Story.StoryState story) =>
        (person.HiddenBy == null || !story.Has(person.HiddenBy)) && (person.ShownBy == null || story.Has(person.ShownBy));

    /// <summary>
    /// The tiles that can be come to from a tile, and the tiles faced from them (where a ball, a hidden item or a trainer
    /// is spoken to or looked at). The walk keeps to the area's chunks and one chunk round them, since a route's halves
    /// may be joined only through its neighbour's ground.
    /// </summary>
    public static (HashSet<(int X, int Y)> Reached, HashSet<(int X, int Y)> Faced) Reach(Map map, int x, int y, float height, TravelMode mode, ChecklistTerms terms)
    {
        var area = map.AreaAt(x, y);
        var reached = new HashSet<(int X, int Y)>();
        var faced = new HashSet<(int X, int Y)>();
        var seen = new HashSet<(int X, int Y, TravelMode Mode, int Level)>();
        var queue = new Queue<(int X, int Y, TravelMode Mode, float Height)>();

        bool Inside(int tx, int ty)
        {
            if (!map.InBounds(tx, ty)) return false;
            if (area == null) return true;
            int cx = tx / Map.ChunkTiles, cy = ty / Map.ChunkTiles;
            for (int oy = -1; oy <= 1; oy++)
                for (int ox = -1; ox <= 1; ox++)
                {
                    int ax = (cx + ox) * Map.ChunkTiles, ay = (cy + oy) * Map.ChunkTiles;
                    if (map.InBounds(ax, ay) && map.AreaAt(ax, ay) == area) return true;
                }
            return false;
        }

        void Visit(int tx, int ty, TravelMode how, float level)
        {
            if (!Inside(tx, ty) || seen.Count >= MostTiles) return;
            if (!seen.Add((tx, ty, how, (int)MathF.Round(level * 2f)))) return;
            reached.Add((tx, ty));
            queue.Enqueue((tx, ty, how, level));
        }

        if (mode is not (TravelMode.Surfing or TravelMode.Diving)) mode = TravelMode.OnFoot;
        Visit(x, y, mode, height);
        while (queue.Count > 0)
        {
            var (cx, cy, how, level) = queue.Dequeue();
            var walker = new Walker(how, level, Moves: terms.Moves, Climbing: true);
            foreach (var way in Ways)
            {
                var (dx, dy) = FieldMovement.Delta(way);
                int nx = cx + dx, ny = cy + dy;
                faced.Add((nx, ny));
                var step = FieldMovement.Step(map, cx, cy, way, walker);
                if (step.Moves)
                {
                    Visit(step.X, step.Y, step.Mode, step.Height);
                    continue;
                }
                if (step.Obstacle == Obstacle.Water && terms.Moves.HasFlag(FieldMoves.Surf) && FieldMovement.CanStartSurf(map, cx, cy, way, walker))
                    Visit(nx, ny, TravelMode.Surfing, map.SurfaceAt(nx, ny, level).Height);
                // A ball is picked up on the way, and an obstacle the team can clear now is cleared
                else if (step.Obstacle == Obstacle.Person && how == TravelMode.OnFoot && map.GetNpcAt(nx, ny) is { IsThing: true } thing
                         && (thing.IsItemBall || thing.Obstacle is { } kind && terms.Clears.Contains(kind))
                         && !map.IsSolid(nx, ny) && MathF.Abs(map.HeightAt(nx, ny) - level) < FieldMovement.StepLimit)
                    Visit(nx, ny, TravelMode.OnFoot, map.HeightAt(nx, ny));
            }
        }
        return (reached, faced);
    }
}
