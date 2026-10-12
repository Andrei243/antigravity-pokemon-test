using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// The game's scripts played on every way through them (plan 02 · S1; moved out of <c>StoryTests</c> by plan 24 ·
/// X3): everyone, every signboard, every trigger and every tile of the game with the script each starts
/// (<see cref="Starters"/>), each played as whatever starts it would start it on maps of the walk's own
/// (<see cref="OwnMaps"/>), each question answered each way and each battle won and lost (<see cref="EveryWayThrough"/>),
/// and the common scripts played by someone who is everything at once (<see cref="PlayEveryScript"/>).
/// <para>
/// A walk builds its maps afresh, because playing scripts walks people about, beats trainers and takes people off the
/// map: each test class that plays them has a walk of its own (<c>StoryTests</c>, <c>GoldenScriptTests</c>), so that
/// two classes running side by side never play on the same map. The maps and starters come in a fixed order (the
/// map files sorted by name), so a walk plays the same in every run on every machine.
/// </para>
/// </summary>
internal sealed class ScriptWalk
{
    public static readonly ScriptLibrary Scripts = ScriptLibrary.Default;

    private readonly Lazy<World> world = new(() => World.LoadAll().Single(w => w.Index.Region == "Sinnoh"));
    private readonly Lazy<Dictionary<string, Map>> maps;

    public ScriptWalk()
    {
        maps = new(() =>
        {
            var built = new Dictionary<string, Map>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in Directory.GetFiles(GameDataFiles.PathOf(MapDatabase.Folder), "*.json").Order(StringComparer.Ordinal))
            {
                var map = GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, Path.GetFileName(path))).ToMap();
                built[map.Name] = map;
            }
            foreach (var map in World.BuildMaps()) built[map.Name] = map;
            return built;
        });
    }

    /// <summary>The imported world of Sinnoh, loaded for this walk alone.</summary>
    public World World => world.Value;

    /// <summary>Every map of the game, the walk's own: the hand-made ones and those of the imported world.</summary>
    public Dictionary<string, Map> OwnMaps => maps.Value;

    /// <summary>The map a script file belongs to: the one its area is part of, or the hand-made map it is named for. Null for the common file and for a file of nowhere.</summary>
    public Map? MapOfFile(string file) =>
        World.Index.Areas.Contains(file) && World.MapOf(file) is { } entry ? OwnMaps[entry.Name]
        : OwnMaps.GetValueOrDefault(file);

    /// <summary>Everyone, every signboard and every trigger of the game, with the script each starts and the file it is looked for in.</summary>
    public IEnumerable<(string What, string Script, string File, Map Map, NPC? Person)> Starters()
    {
        foreach (var map in OwnMaps.Values)
        {
            foreach (var npc in map.Everyone)
                if (FieldScripts.For(npc) is { } script)
                    yield return ($"{npc.Name} at {npc.GridX},{npc.GridY} of {map.Name}", script, npc.ScriptFile ?? map.ScriptFileAt(npc.GridX, npc.GridY), map, npc);
            foreach (var (at, script) in map.SignScripts)
                yield return ($"the sign at {at.X},{at.Y} of {map.Name}", script, map.ScriptFileAt(at.X, at.Y), map, null);
            foreach (var (at, script) in map.TileScripts)
                yield return ($"the tile read at {at.X},{at.Y} of {map.Name}", script, map.ScriptFileAt(at.X, at.Y), map, null);
            foreach (var trigger in map.Triggers)
                yield return ($"the trigger at {trigger.X},{trigger.Y} of {map.Name}", trigger.Script, trigger.ScriptFile ?? map.Name, map, null);
            foreach (var (at, hidden) in map.HiddenItems)
                yield return ($"the hidden {hidden.Item} at {at.X},{at.Y} of {map.Name}", FieldScripts.HiddenItem, map.ScriptFileAt(at.X, at.Y), map, null);
            // A pit a pushed boulder comes to rest on (plan 02 · S12): its script runs with a boulder of the place
            foreach (var (at, script) in map.BoulderPits)
                yield return ($"the pit at {at.X},{at.Y} of {map.Name}", script, map.ScriptFileAt(at.X, at.Y), map,
                    map.Everyone.First(n => n.Obstacle == PropType.StrengthBoulder && n.ScriptFile == map.ScriptFileAt(at.X, at.Y)));
        }
    }

    /// <summary>
    /// One way through a script: the host it was played on (with its <c>Transcript</c>, <c>Asked</c>, <c>Log</c> and
    /// story), the answers taken (a question's answer, or 0 for a battle won and 1 for one lost), where each answer was
    /// asked for (how much had been said, asked and logged by then), and the story as it stood before the script began.
    /// </summary>
    public sealed record Way(HeadlessScriptHost Host, ScriptRunner Runner, IReadOnlyList<int> Answers,
        IReadOnlyList<(int Said, int Asked, int Logged)> Marks, StorySnapshot Before);

    /// <summary>A script played on every way through it, and what started it.</summary>
    public sealed record Played(string What, Script Script, IReadOnlyList<Way> Ways);

    /// <summary>
    /// Plays a script on every way through it: each question answered each way, each battle won and lost. Whoever
    /// the script belongs to and the map are put back as they were between one way and the next.
    /// </summary>
    public List<Way> EveryWayThrough(Script script, Map? map, NPC? subject, IReadOnlyList<string>? own = null,
        Action<HeadlessScriptHost>? before = null, (string Item, int Count)? item = null, string? flag = null, NPC? pair = null)
    {
        var ways = new List<Way>();
        var toTry = new Stack<List<int>>();
        toTry.Push(new List<int>());
        var kept = (map?.Everyone ?? Enumerable.Empty<NPC>()).Append(subject).OfType<NPC>().Distinct()
            .Select(n => (Person: n, n.GridX, n.GridY, n.Facing, n.HasBattled)).ToList();

        while (toTry.Count > 0)
        {
            Assert.True(ways.Count < 300, $"{script.FullName} has more than 300 ways through it");
            var planned = toTry.Pop();
            var taken = new List<int>();
            var marks = new List<(int Said, int Asked, int Logged)>();
            var asked = new HashSet<string>();
            var host = new HeadlessScriptHost { Map = map, MapNamed = name => OwnMaps.GetValueOrDefault(name) };

            // A question asked again on the same way through (the professor asking until he hears yes) is answered
            // with the first answer, or a loop would make the ways through it endless
            int Decide(int answers, string? question = null)
            {
                int choice = taken.Count < planned.Count ? planned[taken.Count] : 0;
                bool again = question != null && !asked.Add(question);
                if (taken.Count >= planned.Count && !again)
                    for (int other = 1; other < answers; other++) toTry.Push(taken.Append(other).ToList());
                marks.Add((host.Transcript.Count, host.Asked.Count, host.Log.Count));
                taken.Add(choice);
                return choice;
            }

            // The Pokémon of the team is the same in every run, so that nothing a script says of it moves
            host.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 5, new Random(0)));
            host.PlayerTile = subject != null ? (subject.GridX, subject.GridY + 1) : (0, 0);
            host.Decide = (question, answers) => Decide(answers.Count, question);
            host.Fight = _ => Decide(2) == 0 ? BattleOutcome.Won : BattleOutcome.Lost;
            before?.Invoke(host);
            var had = host.Story.Snapshot();

            var runner = new ScriptRunner(Scripts, host);
            runner.Start(script, subject, own, item, flag, pair: pair);
            runner.RunToEnd();
            ways.Add(new Way(host, runner, taken, marks, had));

            foreach (var (person, x, y, facing, battled) in kept)
            {
                (person.GridX, person.GridY, person.Facing, person.HasBattled, person.Forced) = (x, y, facing, battled, null);
            }
            map?.ApplyPresence(_ => false);
        }
        return ways;
    }

    /// <summary>The common scripts of the field moves (plan 02 · S2), and those they call.</summary>
    private static readonly HashSet<string> FieldMoveScripts = new HashSet<string>
    {
        FieldScripts.CutTree, FieldScripts.Rock, FieldScripts.Boulder, FieldScripts.Water, FieldScripts.Waterfall, FieldScripts.RockFace,
        "common.StrengthOn"
    }.Concat(Enum.GetValues<FieldMove>().Select(FieldScripts.FromMenu).OfType<string>()).ToHashSet();

    /// <summary>A team between them knowing every move a Pokémon uses in the field, and every badge.</summary>
    private static void KnowsEveryFieldMove(HeadlessScriptHost host)
    {
        host.Party.Clear();
        var moves = Enum.GetValues<FieldMove>().Where(m => m != FieldMove.Chatter).Select(FieldMoveRules.MoveName).ToList();
        for (int i = 0; i < moves.Count; i += 4)
        {
            var pokemon = new Pokemon(PokemonDatabase.Get("Bibarel")!, 40, new Random(i));
            pokemon.Moves.Clear();
            foreach (string move in moves.Skip(i).Take(4)) pokemon.Moves.Add(new Move(MoveDatabase.Get(move)!));
            host.Party.Add(pokemon);
        }
        host.Story.SetBadges(0xFF);
    }

    /// <summary>A trainer of no place, with lines and a team of one.</summary>
    public static NPC AnyTrainer() => MapFile.BuildNpc(new MapFile.NpcRecord
    {
        Id = "anyone", Name = "Anyone", NpcType = "Youngster", X = 2, Y = 2,
        Dialog = new List<string> { "Something to say." },
        Trainer = new MapFile.TrainerRecord
        {
            Id = "anyone", Name = "Anyone", TrainerClass = "Youngster", PrizeMoney = 64,
            Party = new List<MapFile.PartyMember> { new() { Species = "Starly", Level = 2 } },
            DialogueBefore = "Before.", DialogueAfter = "After."
        }
    }, "Test");

    /// <summary>
    /// Every script of the game played on every way through it, one starter at a time: first each script of a place,
    /// as whatever starts it would start it on the place's own map (a trigger with the player on it, a thing read from
    /// the open tile below it), then each common script, by someone who is everything at once (an item's two scripts
    /// by a ball and by a place in the ground, a field move's by an obstacle, the rest by a trainer with lines of their
    /// own). The common ones are never played by each of the hundreds who share them.
    /// </summary>
    public IEnumerable<Played> PlayEveryScript()
    {
        // Taken whole first: the scripts played take people off the maps and put them back
        foreach (var (what, name, file, map, person) in Starters().ToList())
        {
            var script = Scripts.Find(name, file)!;
            if (script.File == ScriptLibrary.Common) continue;
            // A trigger's script starts with the player on the trigger, as stepping onto it starts it
            var trigger = person == null ? map.Triggers.FirstOrDefault(t => t.Script == name && what.StartsWith("the trigger", StringComparison.Ordinal)
                && what.Contains($" at {t.X},{t.Y} ", StringComparison.Ordinal)) : null;
            // A thing read where it stands starts with the player on the open tile below it, facing it, as a painting
            // on a cave's back wall is read (plan 02 · S9: Celestic Town's ruins walk the player about from there)
            (int X, int Y)? read = null;
            if (person == null && what.StartsWith("the tile read at ", StringComparison.Ordinal))
            {
                var at = what["the tile read at ".Length..what.IndexOf(" of ", StringComparison.Ordinal)].Split(',');
                (int x, int y) = (int.Parse(at[0], CultureInfo.InvariantCulture), int.Parse(at[1], CultureInfo.InvariantCulture));
                if (map.InBounds(x, y + 1) && !map.IsSolid(x, y + 1)) read = (x, y + 1);
            }
            Action<HeadlessScriptHost>? before = trigger != null ? host => host.PlayerTile = (trigger.X, trigger.Y)
                : read is { } below ? host => { host.PlayerTile = below; host.PlayerFacing = Direction.Up; }
                : null;
            yield return new Played(what, script, EveryWayThrough(script, map, person, before: before));
        }

        foreach (var script in Scripts.All.Where(s => s.File == ScriptLibrary.Common && s.FullName != ScriptLibrary.NewGame))
        {
            // A field move's with a team that knows every field move and has every badge, and with one that has
            // neither, but for the party menu's, which only a Pokémon that knows the move starts
            var obstacle = new NPC { NpcType = NPC.CutTreeType, Name = "Tree", HiddenBy = "FLAG_MAP_LOCAL_HIDE_OBSTACLE_1_TEST", GridX = 2, GridY = 2 };
            bool fieldMove = FieldMoveScripts.Contains(script.FullName);
            switch (script.FullName)
            {
                case FieldScripts.ItemBall:
                    yield return new Played("an item's ball", script, EveryWayThrough(script, new Map(8, 8), new NPC { NpcType = NPC.ItemBallType, Name = "Potion", Item = "Potion", HiddenBy = "FLAG_OBTAINED_TEST_POTION" }));
                    break;
                case FieldScripts.HiddenItem:
                    yield return new Played("a hidden item", script, EveryWayThrough(script, new Map(8, 8), null, item: ("Stardust", 1), flag: "FLAG_OBTAINED_HIDDEN_TEST_STARDUST"));
                    break;
                // A Pokémon standing in the field cries as itself (plan 10 · F1)
                case FieldScripts.Pokemon:
                    yield return new Played("a Pokémon standing in the field", script, EveryWayThrough(script, new Map(8, 8), new NPC { NpcType = NPC.PokemonType, Name = "Machop", Species = "Machop" }, new[] { "Machop!" }));
                    break;
                // Two trainers who came together, without anyone and beside whoever travels with the player
                case FieldScripts.TrainerPair:
                    yield return new Played("two trainers together", script, EveryWayThrough(script, new Map(8, 8), AnyTrainer(), pair: AnyTrainer()));
                    yield return new Played("two trainers together, with Cheryl along", script, EveryWayThrough(script, new Map(8, 8), AnyTrainer(), pair: AnyTrainer(),
                        before: host => host.TravelWith(new NPC { Name = "Cheryl" }, "cheryl_eterna_forest")));
                    break;
                // A honey tree faced, bare, slathered or with a Pokémon waiting, with Honey in the bag or none (plan 06 · R13)
                case FieldScripts.HoneyTree:
                    foreach (int minutes in new[] { 0, 1440, 600 })
                        foreach (int honey in new[] { 0, 2 })
                            yield return new Played($"a honey tree with {minutes} minutes left and {honey} Honey in the bag", script, EveryWayThrough(script, new Map(8, 8), null, before: host =>
                            {
                                host.HoneyTreeFaced = 0;
                                host.Encounters.Trees[0].MinutesLeft = minutes;
                                host.Encounters.Trees[0].Group = 1;
                                if (honey > 0) host.Bag.AddItem(ItemDatabase.Get("Honey")!, honey);
                            }));
                    break;
                // A berry patch at each stage, mulched and not, faced from below or the side, with a bag of everything it
                // can take or an empty one (plan 06 · R14a)
                case FieldScripts.BerryPatch:
                    foreach (var stage in Enum.GetValues<BerryStage>())
                        foreach (bool full in new[] { false, true })
                            foreach (var facing in new[] { Direction.Up, Direction.Left })
                                foreach (var mulch in new[] { Mulch.None, Mulch.Growth })
                                    yield return new Played($"a berry patch at {stage}, {mulch} mulch, faced {facing}, {(full ? "a bag of what it takes" : "an empty bag")}", script,
                                        EveryWayThrough(script, new Map(8, 8), new NPC { NpcType = NPC.BerrySoilType, Name = "Soft soil", BerryPatch = 0 }, before: host =>
                                        {
                                            host.Berries = new BerryPatches();
                                            if (stage != BerryStage.None)
                                            {
                                                host.Berries[0].Berry = "Oran Berry";
                                                host.Berries[0].Stage = stage;
                                                host.Berries[0].Yield = stage == BerryStage.Fruit ? 3 : 0;
                                            }
                                            host.Berries[0].Mulch = mulch;
                                            host.PlayerFacing = facing;
                                            if (full)
                                                foreach (var item in new[] { "Pecha Berry", "Growth Mulch", "Sprayduck" }) host.Bag.AddItem(ItemDatabase.Get(item)!, 1);
                                        }));
                    break;
                default:
                    if (fieldMove) yield return new Played("an obstacle, with every field move and every badge", script, EveryWayThrough(script, new Map(8, 8), obstacle, before: KnowsEveryFieldMove));
                    else yield return new Played("a trainer with lines of their own", script, EveryWayThrough(script, new Map(8, 8), AnyTrainer(), new[] { "A line of its own." }));
                    break;
            }
            if (script.FullName is FieldScripts.CutTree or FieldScripts.Rock or FieldScripts.Boulder or FieldScripts.Waterfall or FieldScripts.RockFace)
                yield return new Played("an obstacle, with no field move and no badge", script, EveryWayThrough(script, new Map(8, 8), obstacle));
        }
    }
}
