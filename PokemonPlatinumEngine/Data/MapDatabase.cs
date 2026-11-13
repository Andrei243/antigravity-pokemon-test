using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// Every map: the hand-made ones, read from the files in <c>Data/maps</c> (one <see cref="MapFile"/> per map), and
/// the maps of the imported world, made from <c>Data/world</c> (see <see cref="World"/>).
/// </summary>
public static partial class MapDatabase
{
    public const string Folder = "maps";

    /// <summary>The map of Sinnoh's overworld; also what an unknown map name falls back to.</summary>
    public const string StartMap = "Sinnoh";

    // Replaced as a whole on reload, so a reader never sees a half-loaded set
    private static volatile Dictionary<string, Map>? maps;

    private static Dictionary<string, Map> Maps => maps ?? Load();

    /// <summary>
    /// Told how far a load has come, a step for the rooms and one for each region's world: what the game's start-up
    /// draws its bar by. Only the game itself sets it, round its own load.
    /// </summary>
    public static System.Action<int, int>? Loading { get; set; }

    /// <summary>(Re)loads every map from its file, with fresh NPCs and trainers.</summary>
    public static void Initialize() => Load();

    /// <summary>
    /// The same, with the regions' worlds built several at once (<see cref="BuildWorlds"/>): what the game itself asks
    /// for as it starts. The maps are the same every run from one seed, but not the maps <see cref="Initialize()"/>
    /// makes from it, since each region draws from a generator of its own: a recording says which way it was made.
    /// </summary>
    public static void Initialize(bool together) => Load(together);

    /// <summary>
    /// Each world's maps, every region drawing its chance from a generator of its own made from its seed
    /// (<see cref="Core.Dice.Scope"/>), so the maps are the same whether the regions are built one after another or,
    /// <paramref name="together"/>, several at once. Together, the region with the overworld every game starts on is
    /// built alone first, which also reads whatever tables the builders share before two of them could ask at once;
    /// the rest are built on the workers, while this thread tells <paramref name="built"/> of each one done (the game
    /// draws its bar there, which only this thread may). A region that can't be built stops the load with what it
    /// said, the first in the regions' order.
    /// </summary>
    internal static List<List<Map>> BuildWorlds(IReadOnlyList<World> worlds, IReadOnlyList<int> seeds, bool together, System.Action? built = null, long[]? took = null)
    {
        var made = new List<Map>?[worlds.Count];
        var failed = new System.Exception?[worlds.Count];
        void Build(int i)
        {
            var clock = System.Diagnostics.Stopwatch.StartNew();
            using var timed = new Timed(() => { if (took != null) took[i] = clock.ElapsedMilliseconds; });
            try
            {
                using (Core.Dice.Scope(new System.Random(seeds[i]))) made[i] = worlds[i].BuildMaps();
            }
            catch (System.Exception e) when (e is not System.OutOfMemoryException)
            {
                failed[i] = e;
            }
        }

        if (!together)
            for (int i = 0; i < worlds.Count; i++)
            {
                Build(i);
                built?.Invoke();
            }
        else if (worlds.Count > 0)
        {
            int first = System.Math.Max(0, Enumerable.Range(0, worlds.Count).ToList().FindIndex(i => worlds[i].Index.Maps.Any(m => m.Name == StartMap)));
            Build(first);
            built?.Invoke();
            var rest = Enumerable.Range(0, worlds.Count).Where(i => i != first)
                .Select(i => System.Threading.Tasks.Task.Run(() => Build(i))).ToArray();
            int told = 0;
            while (told < rest.Length)
            {
                var running = rest.Where(t => !t.IsCompleted).ToArray();
                if (running.Length > 0) System.Threading.Tasks.Task.WaitAny(running, 100);
                for (int done = rest.Count(t => t.IsCompleted); told < done; told++) built?.Invoke();
            }
        }

        if (failed.FirstOrDefault(e => e != null) is { } why)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(why).Throw();
        return made.Select(m => m!).ToList();
    }

    private sealed class Timed(System.Action done) : System.IDisposable
    {
        public void Dispose() => done();
    }

    /// <summary>
    /// A freshly loaded set of maps of the caller's own, found by name as <see cref="Get"/> finds them. What a test that
    /// walks the whole game holds on to: <see cref="Get"/> answers from whatever set was loaded last, so a walk that
    /// asks it twice can be handed two different maps of one name when another test reloads the set in between.
    /// </summary>
    internal static System.Func<string, Map> Fresh()
    {
        var own = Load();
        return name => own.TryGetValue(name, out var map) ? map : own[StartMap];
    }

    private static Dictionary<string, Map> Load(bool together = false)
    {
        string folder = GameDataFiles.PathOf(Folder);
        if (!Directory.Exists(folder))
            throw new DirectoryNotFoundException($"Map folder not found: {folder}");

        var loaded = new Dictionary<string, Map>(System.StringComparer.OrdinalIgnoreCase);
        // The game's own rooms and every pack's, a pack's map replacing the game's of the same name (plan 16 · T14)
        // The files are read on every core and made into maps one after another, in their order: a map's trainers draw
        // their teams from the game's chance as it is made. A file that can't be read is read again in its turn, so
        // the first of them in the order stops the start, with the message it always had
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var files = DataLayers.FilesIn(Folder, "*.json");
        var ahead = files.AsParallel().AsOrdered().Select(f =>
        {
            try { return GameDataFiles.Load<MapFile>(Path.Combine(Folder, f.Key), strict: true); }
            catch (System.Exception e) when (e is InvalidDataException or IOException) { return null; }
        }).ToList();
        for (int i = 0; i < files.Count; i++)
        {
            var (key, path) = files[i];
            // Written by hand, so read strictly (plan 24 · X4): a mistyped key stops the start with the file and the key
            var file = ahead[i] ?? GameDataFiles.Load<MapFile>(Path.Combine(Folder, key), strict: true);
            string expected = Path.GetFileNameWithoutExtension(path);
            if (file.Name != expected)
                throw new InvalidDataException($"Map file {path} names its map '{file.Name}'; the file must be called {file.Name}.json.");
            loaded[file.Name] = file.ToMap();
        }

        long rooms = clock.ElapsedMilliseconds;
        var worlds = World.LoadAll();
        int steps = worlds.Count + 1, done = 0;
        Loading?.Invoke(++done, steps);
        void Keep(World world, List<Map> made)
        {
            foreach (var map in made)
            {
                if (loaded.ContainsKey(map.Name))
                    throw new InvalidDataException($"The world of {world.Index.Region} makes a map called {map.Name}, and so does {folder}.");
                loaded[map.Name] = map;
            }
        }

        if (together)
        {
            // A seed to each region, drawn in the regions' order before any is built; the maps are kept in that order too
            var seeds = worlds.Select(_ => Core.Dice.Shared.Next()).ToList();
            var took = new long[worlds.Count];
            var built = BuildWorlds(worlds, seeds, together: true, () => Loading?.Invoke(++done, steps), took);
            for (int i = 0; i < worlds.Count; i++) Keep(worlds[i], built[i]);
            Core.Diagnostics.Log.Info($"The maps: the rooms in {rooms} ms, then the regions together in {clock.ElapsedMilliseconds - rooms} ms ("
                + string.Join(", ", worlds.Select((w, i) => $"{w.Index.Region} {took[i]}")) + ").");
        }
        else
            foreach (var world in worlds)
            {
                Keep(world, world.BuildMaps());
                Loading?.Invoke(++done, steps);
            }
        if (!loaded.ContainsKey(StartMap))
            throw new InvalidDataException($"No map is called {StartMap}: the world of Sinnoh is missing from {GameDataFiles.PathOf(World.Folder)}.");

        // The hideaways under the Underground (plan 38 · EX6): laid by rule from their seeds, and their ways down in the tunnels
        if (loaded.TryGetValue(Models.Underground.Hideaways.UndergroundMapName, out var tunnels))
        {
            Models.Underground.Hideaways.LayWaysDown(tunnels);
            foreach (var hideaway in Models.Underground.Hideaways.All)
                loaded[hideaway.Map] = Models.Underground.Hideaways.ToFile(hideaway).ToMap();
        }

        // A region a pack adds names who takes the player there from the region before it (plan 16 · T14): laid onto
        // the map they leave from, so the pack never copies a file of the game's region to stand them on it
        foreach (var link in RegionDatabase.Links)
            if (link.Attendant is { } who && link.DepartureMap != null && loaded.TryGetValue(link.DepartureMap, out var departure))
                departure.Add(MapFile.BuildNpc(who, departure.Name));

        maps = loaded;
        return loaded;
    }

    /// <summary>
    /// The map called this, or null for a name no world has. <see cref="Get"/> answers the map of Sinnoh instead;
    /// it becomes an exception once a second world folder exists (plan 18 · W1, decision 4), so a caller that means
    /// "if there is one" asks here.
    /// </summary>
    public static Map? TryGet(string name) => Maps.GetValueOrDefault(name);

    public static Map Get(string name)
    {
        var all = Maps;
        if (all.TryGetValue(name, out var map)) return map;
        return all[StartMap];
    }

    public static IReadOnlyCollection<string> MapNames => Maps.Keys;
}
