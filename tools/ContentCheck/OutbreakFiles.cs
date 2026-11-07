using PokemonPlatinumEngine.Data;

namespace ContentCheck;

/// <summary>
/// A region's outbreak rows (plan 38 · EX1): the ones its game announces (<c>encounters.json</c>'s <c>outbreaks</c>, written by the
/// map importer) and those of our own (<c>outbreaks.json</c>, written by hand and read strictly). Each is held to what the game
/// needs of a row: an open area of the region with a land table of twelve slots, a species and moves the data has, a level of
/// nothing (the slots' own) or of a Pokémon, and a share in a hundred.
/// </summary>
public static class OutbreakFiles
{
    public static void Check(GameContent content, List<Finding> found)
    {
        foreach (var region in content.Regions)
        {
            string folder = region.Folder;
            string own = content.Relative(Path.Combine(folder, WorldOutbreaksFile.FileName));
            WorldOutbreaksFile? ours = null;
            try
            {
                ours = region.World.OwnOutbreaks;
            }
            catch (Exception e) when (e is InvalidDataException or System.Text.Json.JsonException or UnknownKeyException)
            {
                found.Add(new Finding(Kind.Unreadable, own, "outbreaks", e.Message));
            }
            Rows(content, region, (region.World.Encounters.Outbreaks ?? new()), content.Relative(Path.Combine(folder, WorldEncountersFile.FileName)), found);
            if (ours != null) Rows(content, region, ours.Rows, own, found);
        }
    }

    private static void Rows(GameContent content, GameContent.Region region, List<OutbreakRow> rows, string file, List<Finding> found)
    {
        void Add(string name, string what) => found.Add(new Finding(Kind.Data, file, name, what));
        foreach (var row in rows)
        {
            string name = $"{row.Area}: {row.Species}";
            if (!region.World.Index.Areas.Contains(row.Area, StringComparer.OrdinalIgnoreCase)) Add(name, "is at an area that is not open");
            else if (region.World.Area(row.Area)?.Land is not { Count: 12 }) Add(name, "is at an area with no land table of twelve slots");
            var species = PokemonDatabase.Get(row.Species);
            if (species == null) Add(name, "is no species");
            else if (species.Legendary || species.Mythical || species.IsOurs) Add(name, "is a legendary, a mythical or a species of ours");
            if (row.Level < 0 || row.Level > 100) Add(name, $"has level {row.Level}: nought for the slots' own, or 1 to 100");
            if (row.Share is < 1 or > 100) Add(name, $"has a share of {row.Share} in a hundred");
            foreach (string move in row.Moves ?? new())
                if (!MoveDatabase.Has(move)) Add(name, $"gives the move {move}, which is none");
            if (row.Moves is { Count: > 4 }) Add(name, "gives more than four moves");
        }
        foreach (var twice in rows.GroupBy(r => (r.Area, r.Species, r.Level)).Where(g => g.Count() > 1))
            Add($"{twice.Key.Area}: {twice.Key.Species}", "is a row twice");
    }
}
