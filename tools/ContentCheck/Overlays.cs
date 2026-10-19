using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using static ContentCheck.Checks;

namespace ContentCheck;

/// <summary>
/// An overlay held against the area it is laid over (<c>world/&lt;region&gt;/overlays/&lt;key&gt;.json</c> against
/// <c>areas/&lt;key&gt;.json</c>). An overlay names things by their place in the imported files, and
/// <c>WorldMapBuilder.PlaceEvents</c> goes through the area's own lists: whatever the overlay names that isn't in
/// them is passed over without a word, and something quietly doesn't appear.
/// </summary>
public static class Overlays
{
    /// <summary>Every open area has its file and its overlay: without the one the game stops, without the other the place is silent scenery with nobody in it.</summary>
    public static void CheckOpenAreas(GameContent content, GameContent.Region region, List<Finding> found)
    {
        string index = $"{region.Folder}/{World.IndexFile}";
        // The index is written by hand too: a field it doesn't have ("swap" for "swaps") is skipped like any other
        foreach (var (path, what) in JsonShape.UnknownFields(File.ReadAllText(Path.Combine(content.Root, region.Folder, World.IndexFile)), typeof(WorldIndexFile)))
            found.Add(new Finding(Kind.UnknownField, index, path, what));
        foreach (string key in region.World.Index.Areas)
        {
            if (region.World.Area(key) == null)
                found.Add(new Finding(Kind.Misplaced, index, key, "is open and has no area file: run tools/MapImporter with --data"));
            else if (region.World.MapOf(key) == null)
                found.Add(new Finding(Kind.Misplaced, index, key, $"lies on matrix {region.World.Area(key)!.Matrix}, which no map is made of"));
            if (region.Overlays.All(o => !string.Equals(o.Key, key, StringComparison.OrdinalIgnoreCase)))
                found.Add(new Finding(Kind.Misplaced, index, key, "is open and has no overlay: it would have no music and nobody in it"));
        }
    }

    /// <summary>
    /// The ways the world joins by itself (a warp of an open area onto a warp of another: <c>WorldMapBuilder.Join</c>)
    /// land on a map that exists and on open ground, as a door and an exit must: what <c>DataFileTests</c> held for
    /// every warp of every map. A door's or an exit's own landing is told of with its overlay.
    /// </summary>
    public static void CheckWays(GameContent content, GameContent.Region region, List<Finding> found)
    {
        var written = region.Overlays.Where(o => o.Overlay != null)
            .SelectMany(o => (o.Overlay!.Doors ?? new()).Select(d => (d.Map, d.X, d.Y)).Concat((o.Overlay.Exits ?? new()).Select(e => (e.Map, X: e.ToX, Y: e.ToY)))).ToHashSet();
        foreach (var map in region.Maps ?? Array.Empty<Map>())
            foreach (var warp in map.Warps)
                if (!written.Contains((warp.TargetMap, warp.TargetX, warp.TargetY)))
                    Landing(content, found, $"{region.Folder}/areas/{map.AreaAt(warp.SourceX, warp.SourceY)?.Key ?? map.Name}.json", $"the warp at {warp.SourceX},{warp.SourceY} of {map.Name}", warp.TargetMap, warp.TargetX, warp.TargetY);
    }

    public static void Check(GameContent content, GameContent.Region region, GameContent.OverlaySource source, Flags flags, List<Finding> found)
    {
        if (source.Overlay is not { } overlay) return;
        string file = source.File, key = source.Key;
        var world = region.World;
        void Add(Kind kind, string name, string what) => found.Add(new Finding(kind, file, name, what));

        foreach (var (path, what) in JsonShape.UnknownFields(source.Text, typeof(WorldOverlayFile))) Add(Kind.UnknownField, path, what);

        if (overlay.Area != key) Add(Kind.Misplaced, "area", $"the overlay says it is of '{overlay.Area}' and its file is called {key}.json: it is read as {key}'s");
        if (world.Area(key) is not { } area)
        {
            Add(Kind.Misplaced, key, "the world has no area of that name: nothing reads this overlay");
            return;
        }
        if (!world.IsOpen(key)) Add(Kind.Misplaced, key, $"the area isn't among the open areas of {World.IndexFile}: nothing reads this overlay");

        // The area's own matrix says which area a tile is in, whatever is built of it: the tile's own where its chunk
        // names one (a GBA game's maps share chunks, plan 18 · W6: Dewford Town and Route 107), else the cell's
        var matrix = world.Matrix(area.Matrix);
        string? AreaAt(int x, int z)
        {
            if (x < 0 || z < 0 || x >= matrix.Width * WorldChunkFile.Tiles || z >= matrix.Height * WorldChunkFile.Tiles) return null;
            int cx = x / WorldChunkFile.Tiles, cz = z / WorldChunkFile.Tiles;
            int chunk = matrix.ChunkAt(cx, cz);
            if (chunk != WorldMatrixFile.NoChunk && matrix.AreaKeys != null
                && world.Chunk(chunk)?.AreaIndexAt(x % WorldChunkFile.Tiles, z % WorldChunkFile.Tiles) is { } own && own < matrix.AreaKeys.Count)
                return matrix.AreaKeys[own];
            return matrix.AreaAt(cx, cz) ?? world.MapOf(key)?.Area;
        }
        bool Inside(int x, int z) => string.Equals(AreaAt(x, z), key, StringComparison.OrdinalIgnoreCase);
        // The map the game makes of it, where the world loads
        var map = world.MapOf(key) is { } entry ? region.Maps?.FirstOrDefault(m => m.Name == entry.Name) : null;
        NPC? Built(string id) => map?.Everyone.FirstOrDefault(n => n.Key == id && n.ScriptFile == key);

        if (string.IsNullOrEmpty(overlay.BgmTrack)) Add(Kind.Music, "bgmTrack", "the area has no music");
        Song(found, file, "bgmTrack", overlay.BgmTrack);
        EvolutionSites(found, file, "evolutionSites", overlay.EvolutionSites);

        // ---- our own wild Pokémon with the National Pokédex (plan 03 · D12a): species of the data, as many as the slots
        if (overlay.National is { } national)
        {
            if (national.Land != null && national.Land.Count != 2) Add(Kind.Data, "national.land", $"names {national.Land.Count} species; the grass's slots 8 and 9 take two");
            if (national.Land != null && (area.Land?.Count ?? 0) != WorldAreaFile.LandSlotWeights.Length) Add(Kind.Data, "national.land", "the area has no grass table of twelve slots to put them in");
            if (national.SuperRod != null && national.SuperRod.Count != 1) Add(Kind.Data, "national.superRod", $"names {national.SuperRod.Count} species; the Super Rod's slot 3 takes one");
            if (national.SuperRod != null && (area.SuperRod?.Count ?? 0) <= OverlayNational.SuperRodSlot) Add(Kind.Data, "national.superRod", "the area has no Super Rod table to put it in");
            foreach (string s in (national.Land ?? new()).Concat(national.SuperRod ?? new()))
                if (PokemonDatabase.Get(s) == null) Add(Kind.Data, "national", $"names '{s}', which is no species of the data");
        }

        // ---- the people

        var objects = area.Objects.GroupBy(o => o.Id).ToDictionary(g => g.Key, g => g.First());
        string Nearest(string id) => objects.Keys.OrderBy(k => Math.Abs(k.Length - id.Length) + k.Zip(id).Count(p => p.First != p.Second)).FirstOrDefault() is { } near ? $" ('{near}'?)" : "";
        foreach (var (id, person) in overlay.People ?? new())
        {
            string name = $"people.{id}";
            if (!objects.TryGetValue(id, out var o))
            {
                Add(Kind.Person, name, $"nobody in areas/{key}.json is called '{id}', so they never appear{Nearest(id)}");
                continue;
            }
            bool heldBack = overlay.HeldBack?.Contains(id) == true;

            if (map != null && !heldBack)
            {
                var npc = Built(id);
                if (npc == null)
                    Add(Kind.Person, name, "the game never stands them there: " + WhyNot(world, map, key, o));
                // (an obstacle may take a script of the area's own, and only that: plan 21 · H3, part b, Rusturf Tunnel's rocks)
                else if (npc.IsObstacle && person.Script != null && person.Dialog == null && person.Trainer == null && person.NpcType == null) { }
                else if (npc.IsObstacle || npc.IsBerrySoil || npc.IsItemBall && o.Item != null && o.HiddenBy != null)
                    Add(Kind.Person, name, "is a thing the game lays out from the area's own file (an obstacle, a patch of soil, an item in its ball): the overlay's entry for it is passed over");
            }
            // (A Pokémon cries when spoken to, if nothing else: plan 10 · F1)
            if (!(person.Dialog?.Count > 0) && person.Trainer == null && person.IsStarterBriefcase != true && person.Script == null && person.Item == null
                && !(person.NpcType == null && WorldMapBuilder.SpeciesFor(o.Looks) != null) && !(person.NpcType == NPC.PokemonType && person.Species != null))
                Add(Kind.Person, name, "has nothing to say: no dialog, no script, no trainer's lines");

            Look(found, file, name, person.NpcType);
            Script(content, found, file, name, person.Script, key);
            if (person.HiddenBy is { Length: > 0 }) flags.Check(found, file, name, person.HiddenBy, "is hidden by");
            flags.Check(found, file, name, person.ShownBy, "waits for");
            if (person.Item != null && ItemDatabase.Get(person.Item) == null) Add(Kind.Data, name, $"holds '{person.Item}', which is no item");
            if (person.Species != null && PokemonDatabase.Get(person.Species) == null && PokemonDatabase.SpeciesOfForm(person.Species) == null)
                Add(Kind.Data, name, $"is the Pokémon '{person.Species}', which is no species or form");
            if (person.At != null && (person.At.Length != 2 || !Inside(person.At[0], person.At[1])))
                Add(Kind.Place, name, $"\"at\" must be a tile of the area, [x, z]; it is [{string.Join(", ", person.At)}]");

            Lines.CheckName(found, file, name, person.Name);
            foreach (string line in person.Dialog ?? new()) Lines.Check(found, file, name, line, person.Name);
            if (person.Trainer is not { } lines) continue;

            // A trainer is the table's: by the id their object's script gives, or the one the overlay names for
            // someone a script battles (plan 08 · P7). With neither the game stops as the world loads
            var own = TrainerDatabase.Get(o.Script, world.Index.Region);
            var named = TrainerDatabase.Get(lines.Id, world.Index.Region);
            if (own == null && named == null)
                Add(Kind.Data, name, lines.Id == null
                    ? $"battles, and its object's script '{o.Script}' names no trainer of trainers.json: the overlay must give the table's \"id\" (the game would not load)"
                    : $"battles as '{lines.Id}', which trainers.json lacks, and its object's script '{o.Script}' names no trainer either (the game would not load)");
            else if (lines.Id != null && named == null)
                Add(Kind.Data, name, $"is given the trainer id '{lines.Id}', which trainers.json lacks (the object's own, {own!.Id}, is used)");
            else if (own != null && named != null && own.Id != named.Id)
                Add(Kind.Data, name, $"is given the trainer id '{lines.Id}', and its object's script names {own.Id}, which is the one used");
            TrainerLines(found, file, name, lines.DialogueBefore, lines.DialogueAfter, person.Name, null, null, own ?? named);
        }

        foreach (string held in overlay.HeldBack ?? new())
            if (!objects.ContainsKey(held)) Add(Kind.Person, $"heldBack.{held}", $"nothing in areas/{key}.json is called '{held}'{Nearest(held)}");

        // ---- signs, and what is read

        foreach (var (id, text) in overlay.Signs ?? new())
        {
            string name = $"signs.{id}";
            // A GBA game's sign is a sign event, named by the script it runs (plan 19 · K1): every tile of it is read
            var read = area.Signs.Where(s => s.Type != AreaSign.HiddenItem && s.Script == id).ToList();
            if (read.Count > 0)
            {
                if (map != null && read.Any(s => map.GetSignboardAt(s.X, s.Z) != text))
                    Add(Kind.Place, name, $"'{id}' is read on a tile whose words aren't these");
            }
            else if (!objects.TryGetValue(id, out var o)) Add(Kind.Place, name, $"the area has no sign called '{id}'{Nearest(id)}");
            else if (map != null && map.GetSignboardAt(o.X, o.Z) != text) Add(Kind.Place, name, $"'{id}' is no signpost and no mailbox: nobody can read what is written on it");
            // A sign's words are one line of the box, with the breaks written into it
            SignText(content, found, file, name, text, overlay.SignScripts?.GetValueOrDefault(id), key);
        }
        foreach (var (id, script) in overlay.SignScripts ?? new())
        {
            string name = $"signScripts.{id}";
            if (!objects.TryGetValue(id, out var o)) Add(Kind.Place, name, $"the area has no sign called '{id}'{Nearest(id)}");
            else if (map != null && !map.SignScripts.ContainsKey((o.X, o.Z))) Add(Kind.Place, name, $"'{id}' is no signpost and no mailbox: its script never starts");
            Script(content, found, file, name, script, key, needed: true);
        }
        foreach (var (number, script) in overlay.Read ?? new())
        {
            string name = $"read.{number}";
            if (!area.Signs.Any(s => s.Script == number && !(s.Type == AreaSign.HiddenItem && s.Item != null && s.Flag != null)))
                Add(Kind.Place, name, $"nothing read in the area runs the original's script {number} (its signs run: {string.Join(", ", area.Signs.Where(s => s.Type != AreaSign.HiddenItem).Select(s => s.Script).Distinct())})");
            Script(content, found, file, name, script, key, needed: true);
        }
        foreach (var (list, tiles) in new[] { ("readAt", overlay.ReadAt), ("pits", overlay.Pits) })
            foreach (var tile in tiles ?? new())
            {
                string name = $"{list} {tile.X},{tile.Z}";
                if (!Inside(tile.X, tile.Z)) Add(Kind.Place, name, "is not a tile of the area");
                Script(content, found, file, name, tile.Script, key, needed: true);
            }

        // ---- the ways in and out

        var doors = overlay.Doors ?? new();
        foreach (var door in doors)
        {
            string name = $"doors: warp {door.Warp}";
            if (door.Warp < 0 || door.Warp >= area.Warps.Count)
                Add(Kind.Way, name, $"the area has {area.Warps.Count} warps (0 to {area.Warps.Count - 1}): this door is never built");
            if (doors.Count(d => d.Warp == door.Warp) > 1) Add(Kind.Way, name, "is given twice: the first is the one built");
            Landing(content, found, file, name, door.Map, door.X, door.Y);
            flags.Check(found, file, name, door.OpenedBy, "is opened by");
        }
        foreach (int locked in overlay.Locked ?? new())
            if (locked < 0 || locked >= area.Warps.Count)
                Add(Kind.Way, $"locked: warp {locked}", $"the area has {area.Warps.Count} warps (0 to {area.Warps.Count - 1})");
        foreach (var switched in overlay.Switches ?? new())
        {
            string name = $"switches: warp {switched.Warp}";
            if (switched.Warp < 0 || switched.Warp >= area.Warps.Count)
            {
                Add(Kind.Way, name, $"the area has {area.Warps.Count} warps (0 to {area.Warps.Count - 1})");
                continue;
            }
            if (switched.Until == null && switched.From == null) Add(Kind.Way, name, "names neither \"until\" nor \"from\": it switches nothing");
            flags.Check(found, file, name, switched.Until, "is there until");
            flags.Check(found, file, name, switched.From, "is there from");
            var warp = area.Warps[switched.Warp];
            if (map != null && !map.WarpsAt(warp.X, warp.Z).Any(w => w.Until == switched.Until && w.From == switched.From))
                Add(Kind.Way, name, $"the warp at {warp.X},{warp.Z} leads nowhere (it is locked, or its far side isn't open), so there is nothing to switch");
        }
        foreach (var exit in overlay.Exits ?? new())
        {
            string name = $"exits {exit.X},{exit.Z}";
            if (!Inside(exit.X, exit.Z)) Add(Kind.Way, name, "is not a tile of the area");
            Landing(content, found, file, name, exit.Map, exit.ToX, exit.ToY);
        }

        // ---- triggers

        var triggers = overlay.Triggers ?? new();
        for (int i = 0; i < triggers.Count; i++)
        {
            var bound = triggers[i];
            string name = bound.Trigger is { } n ? $"triggers: trigger {n}" : $"triggers[{i}]";
            Script(content, found, file, name, bound.Script, key, needed: true);
            if (bound.Trigger is { } number)
            {
                if (number < 0 || number >= area.Triggers.Count)
                    Add(Kind.Place, name, $"the area has {area.Triggers.Count} triggers (0 to {area.Triggers.Count - 1}) (the game would not load)");
                else if (!int.TryParse(area.Triggers[number].Value, out _))
                    Add(Kind.Place, name, $"the area's trigger waits for the value '{area.Triggers[number].Value}', which is no number (the game would not load)");
                if (triggers.Count(t => t.Trigger == number) > 1) Add(Kind.Place, name, "is given a script twice: only the first ever starts");
            }
            else if (bound.X is not { } x || bound.Z is not { } z)
                Add(Kind.Place, name, "gives neither the original's number (\"trigger\") nor its own tiles (\"x\", \"z\") (the game would not load)");
            else if (!Inside(x, z) || !Inside(x + bound.Width - 1, z + bound.Depth - 1))
                Add(Kind.Place, name, $"the tiles {x},{z} to {x + bound.Width - 1},{z + bound.Depth - 1} are not all the area's");
        }

        // ---- what the overlay adds of its own

        var npcs = overlay.Npcs ?? new();
        for (int i = 0; i < npcs.Count; i++)
        {
            string name = $"npcs: {(npcs[i].Id ?? npcs[i].Name)} at {npcs[i].X},{npcs[i].Y}";
            if (!Inside(npcs[i].X, npcs[i].Y)) Add(Kind.Place, name, "stands outside the area");
            Person(content, flags, found, file, name, npcs[i], key);
        }
        foreach (var prop in overlay.Props ?? new())
            if (!Inside(prop.X, prop.Y) || !Inside(prop.X + prop.Width - 1, prop.Y + prop.Depth - 1))
                Add(Kind.Place, $"props: {prop.Type} at {prop.X},{prop.Y}", "stands outside the area");
    }

    /// <summary>Why someone of the area's file is nowhere on the map though the overlay names them, in words.</summary>
    private static string WhyNot(World world, Map map, string key, AreaObject o)
    {
        if (o.HiddenBy != null && o.HiddenBy.StartsWith("MAP_HEADER_", StringComparison.Ordinal))
            return "the object is a copy of a neighbouring area's, there so it shows across the border, and that area places it";
        if (map.Signboards.ContainsKey((o.X, o.Z))) return "it is a signpost or a mailbox, whose words go under \"signs\"";
        if (map.InBounds(o.X, o.Z) && map.AreaAt(o.X, o.Z) is { Open: true } there && there.Key != key)
            return $"they stand in {there.Key}, whose file lists them too and which places them: their lines belong in overlays/{there.Key}.json";
        return "the builder passes the object over";
    }
}
