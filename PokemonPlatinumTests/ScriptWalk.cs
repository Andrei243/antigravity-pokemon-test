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
            // The other regions' worlds after Sinnoh's (plan 19 · K1: Kanto's), whose people start the common scripts too
            foreach (var other in others.Value)
                foreach (var map in other.BuildMaps()) built[map.Name] = map;
            // Everyone as a new game finds them, as each way through a script leaves the maps afterwards: someone shown
            // only by a flag (Fuchsia's Omanyte, plan 19 · K6) is off the map from the first way on, not only after one
            foreach (var map in built.Values) map.ApplyPresence(_ => false);
            return built;
        });
    }

    /// <summary>The imported world of Sinnoh, loaded for this walk alone.</summary>
    public World World => world.Value;

    /// <summary>Every map of the game, the walk's own: the hand-made ones and those of the imported world.</summary>
    public Dictionary<string, Map> OwnMaps => maps.Value;

    /// <summary>The map a script file belongs to: the one its area is part of, or the hand-made map it is named for. Null for the common file and for a file of nowhere.</summary>
    public Map? MapOfFile(string file) =>
        Worlds.FirstOrDefault(w => w.Index.Areas.Contains(file)) is { } world && world.MapOf(file) is { } entry ? OwnMaps[entry.Name]
        : OwnMaps.GetValueOrDefault(file);

    /// <summary>Every region's imported world, Sinnoh's first (plan 19 · K1: Kanto's areas have scripts of their own too).</summary>
    private IEnumerable<World> Worlds => new[] { World }.Concat(others.Value);

    private readonly Lazy<List<World>> others = new(() =>
        World.LoadAll().Where(w => w.Index.Region != "Sinnoh").OrderBy(w => w.Index.Region, StringComparer.Ordinal).ToList());

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
    /// asked for (how much had been said, asked and logged by then), the story as it stood before the script began, and
    /// (when it was asked for) what the way left behind beside the story: <see cref="Left"/>.
    /// </summary>
    public sealed record Way(HeadlessScriptHost Host, ScriptRunner Runner, IReadOnlyList<int> Answers,
        IReadOnlyList<(int Said, int Asked, int Logged)> Marks, StorySnapshot Before, string Left = "");

    /// <summary>
    /// What a way through a script left behind, as text (plan 12 · Q20): where the player stands and how, the money,
    /// the bag, the team and the boxes, who travels along, what is left on the screen (the script's black screen, the
    /// song, the camera kit, the effects held, a machine's frame), and everyone of the map who moved, turned, was
    /// beaten, changed their face, came or went. A scene skipped must tell the same as the scene played.
    /// </summary>
    private static string Tell(HeadlessScriptHost host, IReadOnlyList<(NPC Person, int GridX, int GridY, Direction Facing, bool HasBattled)> kept, HashSet<NPC> present,
        IReadOnlyList<Map> stages)
    {
        var text = new System.Text.StringBuilder();
        string Number(float value) => value.ToString(CultureInfo.InvariantCulture);
        text.Append($"on {host.Map?.Name ?? "no map"} at {host.PlayerTile.X},{host.PlayerTile.Y} facing {host.PlayerFacing}, {host.PlayerMode}, {host.PlayerMood}\n");
        text.Append($"money {host.Money}; black screen {host.ScreenBlack}; song {host.Song ?? "none asked for"}; camera on {host.CameraOn}, lens {Number(host.Lens)}, bars {host.Bars}; " +
            $"held effects [{string.Join("; ", host.HeldEffects)}]; a PC on {host.PcOn}; the Hall of Fame's balls {host.MachineBalls}\n");
        text.Append("bag: " + string.Join(", ", host.Bag.AllItems.Where(i => i.Quantity > 0).OrderBy(i => i.Name, StringComparer.Ordinal).Select(i => $"{i.Name} x{i.Quantity}")) + "\n");
        // (A Pokémon a script makes draws its own numbers, so its HP is told as whole, hurt or fainted and not as a number)
        string Told(Pokemon p) => $"{p.Species.Name}{(p.Form != null ? "/" + p.Form : "")} Lv{p.Level} {(p.CurrentHP >= p.MaxHP ? "whole" : p.CurrentHP <= 0 ? "fainted" : "hurt")}" +
            $"{(p.IsEgg ? " Egg" : "")} holding {p.HeldItem?.Name ?? "nothing"} [{string.Join("/", p.Moves.Select(m => m.Name))}]";
        text.Append("team: " + string.Join(", ", host.Party.Members.Select(Told)) + "\n");
        text.Append("boxes: " + string.Join(", ", host.Box.Select(Told)) + "\n");
        text.Append($"along: {host.Travelling?.Name ?? "nobody"} as {host.Partner ?? "-"}, strolling {host.Strolling?.Species.Name ?? "nobody"}; " +
            $"Pokétch {host.Poketch.Enabled}, Safari Game {host.Safari.Active}, Journal {host.Log.Count(l => l.StartsWith("journal ", StringComparison.Ordinal))} lines; " +
            $"Hall of Fame {host.HallOfFame.Total}, National Pokédex {host.NationalDex}, saves {host.Saves.Count}, seen [{string.Join(", ", host.SeenSpecies)}]\n");
        var now = stages.SelectMany(m => m.NPCs).ToHashSet();
        foreach (var (person, x, y, facing, battled) in kept)
        {
            bool was = present.Contains(person), there = now.Contains(person);
            if (person.GridX == x && person.GridY == y && person.Facing == facing && person.HasBattled == battled && was == there
                && person.Mood == Mood.Neutral && person.Forced == null) continue;
            text.Append($"  {person.Key ?? person.Name} of {x},{y}: at {person.GridX},{person.GridY} facing {person.Facing}" +
                $"{(person.HasBattled ? ", beaten" : "")}{(person.Mood != Mood.Neutral ? $", {person.Mood}" : "")}, {(there ? "on the map" : "off the map")}" +
                $"{(person.Forced is { } forced ? (forced ? " (shown)" : " (hidden)") : "")}\n");
        }
        return text.ToString();
    }

    /// <summary>A script played on every way through it, and what started it.</summary>
    public sealed record Played(string What, Script Script, IReadOnlyList<Way> Ways);

    /// <summary>
    /// Plays a script on every way through it: each question answered each way, each battle won and lost. Whoever
    /// the script belongs to and the map are put back as they were between one way and the next.
    /// </summary>
    /// <param name="hurry">
    /// Played by someone who holds the skip button the whole time (plan 12 · Q20): every scene of the script is
    /// hurried through, up to each thing that waits for the player and on again after it.
    /// </param>
    /// <param name="tell">Whether each way notes what it left behind (<see cref="Way.Left"/>).</param>
    public List<Way> EveryWayThrough(Script script, Map? map, NPC? subject, IReadOnlyList<string>? own = null,
        Action<HeadlessScriptHost>? before = null, (string Item, int Count)? item = null, string? flag = null, NPC? pair = null,
        bool hurry = false, bool tell = false)
    {
        var ways = new List<Way>();
        var toTry = new Stack<List<int>>();
        toTry.Push(new List<int>());
        // The maps whose people are put back between the ways: the script's own, and, where what each way left is
        // told, every map a warp of the script (or of one it calls) leads to, since a scene goes on there
        var stages = new List<Map>();
        if (map != null) stages.Add(map);
        if (tell)
            foreach (string name in WarpsOf(script, new HashSet<string>(StringComparer.Ordinal)))
                if (OwnMaps.TryGetValue(name, out var target) && !stages.Contains(target)) stages.Add(target);
        var kept = stages.SelectMany(m => m.Everyone).Append(subject).OfType<NPC>().Distinct()
            .Select(n => (Person: n, n.GridX, n.GridY, n.Facing, n.HasBattled)).ToList();
        var present = tell ? stages.SelectMany(m => m.NPCs).ToHashSet() : new HashSet<NPC>();

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

            var runner = new ScriptRunner(Scripts, host) { Impatient = hurry };
            try
            {
                runner.Start(script, subject, own, item, flag, pair: pair);
                runner.RunToEnd();
                ways.Add(new Way(host, runner, taken, marks, had, tell ? Tell(host, kept, present, stages) : ""));
            }
            finally
            {
                // (Also when a way goes wrong: the next script finds everyone where they stood)
                foreach (var (person, x, y, facing, battled) in kept)
                {
                    (person.GridX, person.GridY, person.Facing, person.HasBattled, person.Forced) = (x, y, facing, battled, null);
                    // (the face a scene left them with goes with the scene, as it does when the place is come to again)
                    person.Mood = Mood.Neutral;
                }
                foreach (var stage in stages) stage.ApplyPresence(_ => false);
            }
        }
        return ways;
    }

    /// <summary>The maps a script's warps lead to, its own and those of the scripts it calls.</summary>
    private static IEnumerable<string> WarpsOf(Script script, HashSet<string> looked)
    {
        if (!looked.Add(script.FullName)) yield break;
        foreach (var i in script.Everything())
        {
            if (i.Op == Op.Warp) yield return i.Name;
            if (i.Op == Op.Call && Scripts.Find(i.Name, script.File) is { } called)
                foreach (string name in WarpsOf(called, looked)) yield return name;
        }
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
        host.Story.SetBadges("Sinnoh", 0xFF);
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

    private static (int X, int Y) StandingOn(Map map, StepTrigger trigger)
    {
        for (int y = trigger.Y; y < trigger.Y + trigger.Depth; y++)
            for (int x = trigger.X; x < trigger.X + trigger.Width; x++)
                if (map.InBounds(x, y) && !map.IsSolid(x, y)) return (x, y);
        return (trigger.X, trigger.Y);
    }

    /// <summary>
    /// A script of a place on every way through it, as whatever starts it would start it on the place's own map: a
    /// trigger's with the player on the trigger, a thing read from the open tile below it, facing it.
    /// </summary>
    private List<Way> AsStarted(Script script, (string What, string Script, string File, Map Map, NPC? Person) starter, bool hurry = false, bool tell = false,
        Action<HeadlessScriptHost>? given = null)
    {
        var (what, name, _, map, person) = starter;
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
        // A place's own arrival script starts with the player where a door of a room puts them (its first way out's
        // own tile, which is the mat inside it); out in the open the walk has no better place than the map's corner
        var door = person == null && what.StartsWith(Arriving, StringComparison.Ordinal) && !map.IsStreamed ? map.Warps.FirstOrDefault() : null;
        // (on its first tile that can be stood on: the original's triggers sometimes reach over a wall, which nobody
        // steps onto, as the Team Rocket Hideout's way past its generator does, plan 20 · J11)
        var onTrigger = trigger == null ? (0, 0) : StandingOn(map, trigger);
        Action<HeadlessScriptHost>? before = trigger != null ? host => host.PlayerTile = onTrigger
            : read is { } below ? host => { host.PlayerTile = below; host.PlayerFacing = Direction.Up; }
            : door != null ? host => { host.PlayerTile = (door.SourceX, door.SourceY); host.PlayerFacing = Direction.Up; }
            : null;
        Action<HeadlessScriptHost>? both = given == null ? before : host => { before?.Invoke(host); given(host); };
        return EveryWayThrough(script, map, person, before: both, hurry: hurry, tell: tell);
    }

    /// <summary>
    /// What a script asks of the story before it does anything, each as something a game could be given to answer
    /// it otherwise than a new game does: a flag set, a variable at the value asked about (or just past it either
    /// way), so many badges, the National Pokédex, the girl, a starter, an item, a Pokémon. A place's arrival script
    /// plays its scene only when the story has come to it; these are how a walk that begins every script with a new
    /// game's story gets there (<see cref="PlayEveryScene"/>).
    /// </summary>
    private static List<(string What, Action<HeadlessScriptHost> Give)> Givens(Script script, HashSet<string> looked)
    {
        var givens = new List<(string What, Action<HeadlessScriptHost> Give)>();
        if (!looked.Add(script.FullName)) return givens;
        void Add(string what, Action<HeadlessScriptHost> give)
        {
            if (givens.All(g => g.What != what)) givens.Add((what, give));
        }
        foreach (var i in script.Everything())
        {
            if (i.Op == Op.Call && Scripts.Find(i.Name, script.File) is { } called)
                foreach (var (what, give) in Givens(called, looked)) Add(what, give);
            if (i.Condition is not { } c) continue;
            switch (c.Query)
            {
                case Query.Flag:
                    Add($"{c.Name} set", host => host.Story.Set(c.Name));
                    break;
                case Query.Var when c.Other == null && !ScriptParser.BuiltInVariables.Contains(c.Name):
                    foreach (int value in new[] { c.Number, c.Number + 1, c.Number - 1 }.Where(v => v > 0).Distinct())
                        Add($"{c.Name} = {value}", host => host.Story.SetVar(c.Name, value));
                    break;
                case Query.Var when c.Name == "SEEN":
                    Add($"{c.Number} species seen", host => host.SeenInSinnoh = c.Number);
                    break;
                case Query.Var when c.Name == "CAUGHT":
                    Add($"{c.Number} species caught", host => host.Caught = c.Number);
                    break;
                // Where the player stands as the script begins: a scene that plays only for someone who came in by
                // a door asks the tile in front of it
                case Query.Var when c.Other == null && c.Name is "PLAYER_X" or "PLAYER_Y":
                    foreach (int at in new[] { c.Number, c.Number + 1 })
                        Add($"the player at {(c.Name == "PLAYER_X" ? "x" : "y")} {at}", c.Name == "PLAYER_X"
                            ? host => host.PlayerTile = (at, host.PlayerTile.Y)
                            : host => host.PlayerTile = (host.PlayerTile.X, at));
                    break;
                // A team of none: a region's opening that takes an older save with a Pokémon past itself plays only
                // for someone who has none yet (plan 22 · U5, common.UnovaCatchUp)
                case Query.Var when c.Other == null && c.Name == "PARTY_COUNT" && c.Number == 0:
                    Add("no Pokémon", host => host.Party.Clear());
                    break;
                case Query.Party when c.Number is > 1 and <= 6:
                    Add($"a team of {c.Number}", host =>
                    {
                        while (host.Party.Count < c.Number) host.Party.Add(new Pokemon(PokemonDatabase.Get("Bidoof")!, 5, new Random(host.Party.Count)));
                    });
                    break;
                case Query.Facing:
                    Add($"facing {c.Name}", host => host.PlayerFacing = Enum.Parse<Direction>(c.Name));
                    break;
                case Query.Badges:
                    foreach (int badges in new[] { c.Number, c.Number - 1 }.Where(b => b is > 0 and <= 8))
                        Add($"{badges} badges", host => host.Story.SetBadges(host.Region, (1 << badges) - 1));
                    break;
                case Query.Badge:
                    // (of the region the script plays in: a Johto Leader asks for a Johto badge, plan 20 · J9)
                    Add($"the {c.Name} badge", host => host.Story.GiveBadge(host.Region, c.Name));
                    break;
                case Query.NationalDex:
                    Add("the National Pokédex", host => host.NationalDex = true);
                    break;
                case Query.Girl or Query.Boy:
                    Add("the girl", host => host.PlayerLook = PokemonPlatinumEngine.Core.PlayerLook.Girl);
                    break;
                case Query.Starter:
                    Add($"{c.Name} for a starter", host => host.Story.ChooseStarter(c.Name));
                    break;
                case Query.Item when ItemDatabase.Get(c.Name) is { } item:
                    Add($"{c.Name} in the bag", host => host.Bag.AddItem(item, Math.Max(1, c.Number)));
                    break;
                case Query.Has when PokemonDatabase.Get(c.Name) is { } species:
                    Add(c.Place == null ? $"a {c.Name} on the team" : $"a {c.Name} met at {c.Place} on the team",
                        host => host.Party.Add(new Pokemon(species, 30, new Random(1)) { MetLocation = c.Place }));
                    break;
                case Query.Defeated when c.Name != "self":
                    Add($"{c.Name} beaten", host => host.Story.Defeat(c.Name));
                    break;
            }
        }
        return givens;
    }

    /// <summary>The scenes a script marks, its own and those of the scripts it calls, by their keys.</summary>
    private static IEnumerable<string> ScenesOf(Script script, HashSet<string> looked)
    {
        if (!looked.Add(script.FullName)) yield break;
        foreach (var i in script.Everything())
        {
            if (i.Op == Op.Scene) yield return SceneSkip.Key(script.File, i.Name);
            if (i.Op == Op.Call && Scripts.Find(i.Name, script.File) is { } called)
                foreach (string key in ScenesOf(called, looked)) yield return key;
        }
    }

    /// <summary>The most stories one script is tried with before its scenes are given up as out of this walk's reach.</summary>
    private const int MostGivens = 4000;

    /// <summary>How the walk names a place's own arrival script as a starter: "arriving in route_201".</summary>
    private const string Arriving = "arriving in ";

    /// <summary>A script's scenes played and skipped under one story: what the story was given first, and each way both ways.</summary>
    public sealed record Scene(string What, Script Script, string Given, IReadOnlyList<Way> Played, IReadOnlyList<Way> Skipped);

    /// <summary>
    /// Every script of a place that marks a scene (plan 12 · Q20: <c>scene</c> … <c>endscene</c>, in itself or in a
    /// script it calls), played on every way through it and then, at once, on every way through it again by someone
    /// who holds the skip button the whole time; each way tells what it left (<see cref="Way.Left"/>). The two are
    /// played back to back so that both begin with the maps as the same scripts before them left them.
    /// <para>
    /// A script that keeps its scene for a later moment of the story (an arrival script, which runs every time) is
    /// tried again with the story given what its questions ask about, one thing and then two at a time
    /// (<see cref="Givens"/>), until each of its scenes has been played to its end or <see cref="MostGivens"/> stories
    /// have been tried; only a story that plays a scene not yet seen is played both ways. A story given so may be one
    /// no game ever has, which is as it should be: a skip leaves what the scene leaves whatever the story holds.
    /// </para>
    /// </summary>
    /// <param name="reached">Filled with every scene some way played to its end.</param>
    public IEnumerable<Scene> PlayEveryScene(HashSet<string> reached)
    {
        // Whatever of a map starts a script, and each place's own arrival script, which most scenes of the story are
        var starters = Starters().ToList();
        foreach (var arrival in Scripts.All.Where(s => s.Name == ScriptLibrary.OnEnter && s.File != ScriptLibrary.Common).OrderBy(s => s.File, StringComparer.Ordinal))
            if (MapOfFile(arrival.File) is { } place) starters.Add((Arriving + arrival.File, arrival.Name, arrival.File, place, null));
        foreach (var starter in starters)
        {
            var script = Scripts.Find(starter.Script, starter.File)!;
            if (script.File == ScriptLibrary.Common || !Scripts.BeginsAScene(script)) continue;
            var wanted = ScenesOf(script, new HashSet<string>(StringComparer.Ordinal)).ToHashSet(StringComparer.Ordinal);
            var ended = new HashSet<string>(StringComparer.Ordinal);

            Scene? Try(string what, Action<HeadlessScriptHost>? given)
            {
                List<Way> played;
                try
                {
                    played = AsStarted(script, starter, tell: true, given: given);
                }
                catch (ScriptException) when (given != null)
                {
                    // (A story no game has can ask a script for something it never meets: that story is no use)
                    return null;
                }
                var now = played.SelectMany(way => way.Host.Story.ScenesSeen).ToHashSet(StringComparer.Ordinal);
                if (given != null && now.IsSubsetOf(ended)) return null;
                ended.UnionWith(now);
                return new Scene(starter.What, script, what, played, AsStarted(script, starter, hurry: true, tell: true, given: given));
            }

            if (Try("a new game's story", null) is { } first) yield return first;
            if (!wanted.IsSubsetOf(ended))
            {
                var givens = Givens(script, new HashSet<string>(StringComparer.Ordinal));
                int tried = 0;
                foreach (var (what, give) in givens)
                {
                    if (wanted.IsSubsetOf(ended) || ++tried > MostGivens) break;
                    if (Try(what, give) is { } one) yield return one;
                }
                for (int a = 0; a < givens.Count && !wanted.IsSubsetOf(ended) && tried <= MostGivens; a++)
                    for (int b = a + 1; b < givens.Count && !wanted.IsSubsetOf(ended) && ++tried <= MostGivens; b++)
                    {
                        var (one, two) = (givens[a], givens[b]);
                        if (Try($"{one.What}, {two.What}", host => { one.Give(host); two.Give(host); }) is { } pair) yield return pair;
                    }
                // ... and three, for a scene kept for someone who stands on one tile at one moment of the story
                for (int a = 0; a < givens.Count && !wanted.IsSubsetOf(ended) && tried <= MostGivens; a++)
                    for (int b = a + 1; b < givens.Count && !wanted.IsSubsetOf(ended) && tried <= MostGivens; b++)
                        for (int c = b + 1; c < givens.Count && !wanted.IsSubsetOf(ended) && ++tried <= MostGivens; c++)
                        {
                            var (one, two, three) = (givens[a], givens[b], givens[c]);
                            if (Try($"{one.What}, {two.What}, {three.What}", host => { one.Give(host); two.Give(host); three.Give(host); }) is { } triple) yield return triple;
                        }
            }
            reached.UnionWith(ended);
        }
    }

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
        foreach (var starter in Starters().ToList())
        {
            var script = Scripts.Find(starter.Script, starter.File)!;
            if (script.File == ScriptLibrary.Common) continue;
            yield return new Played(starter.What, script, AsStarted(script, starter));
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
                // A Kecleon of Hoenn seen through the Devon Scope (plan 21 · H6): a thing of the map nobody sees, with
                // its own flag, which its battle sets however it ends
                case "common.HoennKecleonFound":
                    yield return new Played("a Kecleon seen through the Devon Scope", script, EveryWayThrough(script, new Map(8, 8),
                        new NPC { NpcType = NPC.UnseenType, Name = "Kecleon", HiddenBy = "FLAG_HIDE_TEST_KECLEON" }));
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
