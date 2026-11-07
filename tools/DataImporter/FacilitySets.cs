using System.Text.Json;
using System.Text.Json.Nodes;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace DataImporter;

/// <summary>
/// The later sets of plan 37 · FC1, written to <c>Data/facility-sets.json</c>: a battle set for each role of every species
/// of the games' 1,025 that Pokémon Showdown's random-battle files name (MIT; the files <see cref="MasterTrainers"/> reads),
/// the newest generation's file that gives the species a role that fits first, so the later battle places draw their
/// trainers from species past Platinum's 493.
/// <para>
/// From Showdown are read a role's name, its move pool, its abilities and the species' level, nothing else. A role fits
/// when as many of its moves as it has, up to four, are moves the species' own data can teach it (<see cref="Legality"/>)
/// and the engine runs in full; its four are drawn from those on Platinum's generator (<see cref="BattleRandom"/>) seeded
/// by the national number and the role's place, so every import writes the same file. Where no role of any file fits whole,
/// the newest role that leaves two such moves or more is made up to four with the highest moves the species' level taught
/// it, as the masters' are. Its ability is the first of the role's that is the species' own (the hidden one among them),
/// or the species' first. Its item, nature and effort are a rule of ours from the four moves and the species' base stats
/// (<see cref="ItemOf"/>, <see cref="NatureOf"/>, <see cref="EffortOf"/>), its tier the level's band
/// (<see cref="FacilitySetDatabase.TierOfLevel"/>). A species on the Frontier's banlist has none, and the file lists every
/// species without a set and why. <c>Overrides/facility-sets.json</c> corrects a set by its id, or adds one that says
/// <c>"new": true</c>.
/// </para>
/// </summary>
public static class FacilitySets
{
    public const string OverridesFile = "facility-sets.json";

    /// <summary>The level the masters' made-up moves are taken to, the level the places battle at.</summary>
    public const int Level = 50;

    /// <summary>What an import made: the file and how many of its species' sets were made up with what their level taught them.</summary>
    public sealed record Result(FacilitySetData Data, int MadeUp, int Species);

    // A role of one generation's file: its name, its move pool (Showdown ids), its abilities' names, the species' level
    private sealed record Role(string Name, List<string> Moves, List<string> Abilities, int? Level);

    private static bool Runs(MoveData m) => m.Kind == MoveKind.Standard && m.Support == MoveEffectSupport.Full;

    /// <summary>The sets, in National Pokédex order, from the files in Showdown's folder, with the overrides at <paramref name="overrides"/> laid over them.</summary>
    public static Result Build(string showdownFolder, IReadOnlyList<PokemonSpecies> species, IReadOnlyList<MoveData> moves,
        IReadOnlyList<ItemData> items, IEnumerable<string> banlist, string? overrides = null)
    {
        var banned = new HashSet<string>(banlist, StringComparer.OrdinalIgnoreCase);
        var files = MasterTrainers.SetFiles.Select(f => Path.Combine(showdownFolder, f)).Where(File.Exists).Select(ReadRoles).ToList();
        var movesById = new Dictionary<string, MoveData>();
        foreach (var m in moves.Where(m => m.Kind == MoveKind.Standard)) movesById.TryAdd(MasterTrainers.ShowdownId(m.Name), m);
        var byName = moves.GroupBy(m => m.Name).ToDictionary(g => g.Key, g => g.First());
        var machines = items.Where(i => i.TeachesMove != null).GroupBy(i => i.Name).ToDictionary(g => g.Key, g => g.First().TeachesMove);

        var data = new FacilitySetData();
        int madeUp = 0, withSets = 0;
        foreach (var s in species.Where(s => !s.IsOurs).OrderBy(s => s.DexNumber))
        {
            if (banned.Contains(s.Name))
            {
                data.Without.Add(new FacilitySetGap { Species = s.Name, Why = FacilitySetGapReason.Banned });
                continue;
            }
            string id = MasterTrainers.ShowdownId(s.Name);
            var named = files.Where(f => f.ContainsKey(id)).Select(f => f[id]).ToList();
            if (named.Count == 0)
            {
                data.Without.Add(new FacilitySetGap { Species = s.Name, Why = FacilitySetGapReason.NotNamed });
                continue;
            }
            var learnable = Legality.Learnable(s, species, machine => machines.GetValueOrDefault(machine));
            List<string> Fit(Role role) => role.Moves.Select(p => movesById.GetValueOrDefault(p)).OfType<MoveData>()
                .Where(m => Runs(m) && learnable.Contains(m.Name)).Select(m => m.Name).Distinct().ToList();

            // The newest file with a role that fits whole: every role of it that does
            var made = new List<FrontierSetRecord>();
            foreach (var roles in named)
            {
                for (int at = 0; at < roles.Count; at++)
                {
                    var fit = Fit(roles[at]);
                    if (fit.Count == 0 || fit.Count < Math.Min(4, roles[at].Moves.Count)) continue;
                    made.Add(Make(s, roles[at], at, fit, byName, made));
                }
                if (made.Count > 0) break;
            }
            // Failing that, the newest role that leaves two moves or more, made up with what the level taught it
            if (made.Count == 0)
            {
                foreach (var roles in named)
                {
                    int best = -1;
                    List<string>? bestFit = null;
                    for (int at = 0; at < roles.Count; at++)
                    {
                        var fit = Fit(roles[at]);
                        if (fit.Count >= 2 && fit.Count > (bestFit?.Count ?? 0)) (best, bestFit) = (at, fit);
                    }
                    if (bestFit == null) continue;
                    var set = Make(s, roles[best], best, bestFit, byName, made);
                    foreach (var taught in s.Learnset.Select((m, i) => (m, i)).Where(x => x.m.Level <= Level)
                        .OrderByDescending(x => x.m.Level).ThenByDescending(x => x.i).Select(x => x.m.MoveName))
                    {
                        if (set.Moves.Count >= 4) break;
                        if (!set.Moves.Contains(taught) && byName.TryGetValue(taught, out var known) && Runs(known) && learnable.Contains(taught)) set.Moves.Add(taught);
                    }
                    Dress(set, s, byName);
                    made.Add(set);
                    madeUp++;
                    break;
                }
            }
            if (made.Count == 0)
            {
                data.Without.Add(new FacilitySetGap { Species = s.Name, Why = FacilitySetGapReason.NoFit });
                continue;
            }
            withSets++;
            data.Sets.AddRange(made);
        }
        if (overrides != null) Apply(data.Sets, overrides, species);
        return new Result(data, madeUp, withSets);
    }

    // The set of one role: its four drawn, its ability, and its item, nature, effort and tier by the rule of ours
    private static FrontierSetRecord Make(PokemonSpecies s, Role role, int at, List<string> fit, Dictionary<string, MoveData> byName, List<FrontierSetRecord> made)
    {
        var rng = new BattleRandom((uint)(s.DexNumber << 4 | at & 0xF));
        var left = fit.ToList();
        var moves = new List<string>();
        while (moves.Count < 4 && left.Count > 0)
        {
            int pick = rng.Roll(RollKind.Other, left.Count);
            moves.Add(left[pick]);
            left.RemoveAt(pick);
        }
        var own = s.Abilities.Append(s.HiddenAbility).OfType<string>().ToList();
        string ability = role.Abilities.Select(a => own.FirstOrDefault(o => MasterTrainers.ShowdownId(o) == MasterTrainers.ShowdownId(a)))
            .OfType<string>().FirstOrDefault() ?? s.Abilities.FirstOrDefault() ?? "";
        string slug = Slug(role.Name);
        string id = $"{Slug(s.Name)}_{slug}";
        for (int n = 2; made.Any(m => m.Id == id); n++) id = $"{Slug(s.Name)}_{slug}_{n}";
        var set = new FrontierSetRecord
        {
            Id = id,
            Species = s.Name,
            Moves = moves,
            Ability = ability,
            Tier = role.Level is { } level ? FacilitySetDatabase.TierOfLevel(level) : FacilitySetDatabase.TierOfTotal(Total(s))
        };
        Dress(set, s, byName);
        return set;
    }

    // ------------------------------------------------------------------ the rule of ours for item, nature and effort

    /// <summary>How a set means to fight, by its moves and the species' stats.</summary>
    public enum Leaning { Physical, Special, Mixed }

    private static int Total(PokemonSpecies s) => s.BaseHP + s.BaseAttack + s.BaseDefense + s.BaseSpAttack + s.BaseSpDefense + s.BaseSpeed;

    /// <summary>A species is fast from a base Speed of 90, bulky from 270 of HP, Defense and Sp. Def together, frail under 230.</summary>
    public static bool Fast(PokemonSpecies s) => s.BaseSpeed >= 90;
    public static bool Bulky(PokemonSpecies s) => s.BaseHP + s.BaseDefense + s.BaseSpDefense >= 270;
    public static bool Frail(PokemonSpecies s) => s.BaseHP + s.BaseDefense + s.BaseSpDefense < 230;

    /// <summary>Physical or special by the attacking moves (mixed with two of each), the higher attacking stat where they are even.</summary>
    public static Leaning LeaningOf(PokemonSpecies s, IReadOnlyList<MoveData> moves)
    {
        int physical = moves.Count(m => m.Category == MoveCategory.Physical), special = moves.Count(m => m.Category == MoveCategory.Special);
        if (physical >= 2 && special >= 2) return Leaning.Mixed;
        if (physical != special) return physical > special ? Leaning.Physical : Leaning.Special;
        return s.BaseAttack >= s.BaseSpAttack ? Leaning.Physical : Leaning.Special;
    }

    /// <summary>A support set: two of its moves or more are status moves.</summary>
    public static bool Support(IReadOnlyList<MoveData> moves) => moves.Count(m => m.Category == MoveCategory.Status) >= 2;

    /// <summary>
    /// The item, the first rule that holds: Rest a Chesto Berry; both screens a Light Clay; a support set a Black Sludge for a
    /// Poison type, Leftovers when bulky and a Sitrus Berry when not; one status move a Focus Sash when frail, a Life Orb when
    /// fast, Leftovers when bulky, a Lum Berry when physical and an Expert Belt otherwise; four attacks an Expert Belt when
    /// mixed, a Choice Band or Choice Specs from a base Speed of 100, a Choice Scarf from 70, an Assault Vest when bulky, and a
    /// Muscle Band or Wise Glasses otherwise.
    /// </summary>
    public static string ItemOf(PokemonSpecies s, IReadOnlyList<MoveData> moves)
    {
        var names = moves.Select(m => m.Name).ToHashSet();
        if (names.Contains("Rest")) return "Chesto Berry";
        if (names.Contains("Reflect") && names.Contains("Light Screen")) return "Light Clay";
        var leaning = LeaningOf(s, moves);
        int status = moves.Count(m => m.Category == MoveCategory.Status);
        if (status >= 2)
        {
            if (s.PrimaryType == PokemonType.Poison || s.SecondaryType == PokemonType.Poison) return "Black Sludge";
            return Bulky(s) ? "Leftovers" : "Sitrus Berry";
        }
        if (status == 1)
        {
            if (Frail(s)) return "Focus Sash";
            if (Fast(s)) return "Life Orb";
            if (Bulky(s)) return "Leftovers";
            return leaning == Leaning.Physical ? "Lum Berry" : "Expert Belt";
        }
        if (leaning == Leaning.Mixed) return "Expert Belt";
        if (s.BaseSpeed >= 100) return leaning == Leaning.Physical ? "Choice Band" : "Choice Specs";
        if (s.BaseSpeed >= 70) return "Choice Scarf";
        if (Bulky(s)) return "Assault Vest";
        return leaning == Leaning.Physical ? "Muscle Band" : "Wise Glasses";
    }

    /// <summary>
    /// The nature: an attacker raises its attacking stat (Adamant, Modest) or, fast, its Speed (Jolly, Timid), a mixed one
    /// Hasty or Mild; a support set raises its better defence and lowers the attacking stat it doesn't use (Impish, Bold,
    /// Careful, Calm), or, from a base Speed of 100, its Speed (Jolly, Timid).
    /// </summary>
    public static Nature NatureOf(PokemonSpecies s, IReadOnlyList<MoveData> moves)
    {
        var leaning = LeaningOf(s, moves);
        bool physical = leaning != Leaning.Special;
        if (Support(moves))
        {
            if (s.BaseSpeed >= 100) return physical ? Nature.Jolly : Nature.Timid;
            bool defence = s.BaseDefense >= s.BaseSpDefense;
            return (defence, physical) switch
            {
                (true, true) => Nature.Impish,
                (true, false) => Nature.Bold,
                (false, true) => Nature.Careful,
                _ => Nature.Calm
            };
        }
        return leaning switch
        {
            Leaning.Mixed => Fast(s) ? Nature.Hasty : Nature.Mild,
            Leaning.Physical => Fast(s) ? Nature.Jolly : Nature.Adamant,
            _ => Fast(s) ? Nature.Timid : Nature.Modest
        };
    }

    /// <summary>
    /// The stats its effort goes into (<see cref="FrontierSetRecord.EvFlags"/>'s bits: HP 1, Attack 2, Defense 4, Speed 8,
    /// Sp. Atk 16, Sp. Def 32): an attacker its attacking stats and Speed when fast, HP and its attacking stats when not; a
    /// support set HP and its better defence.
    /// </summary>
    public static int EffortOf(PokemonSpecies s, IReadOnlyList<MoveData> moves)
    {
        const int hp = 1, attack = 2, defense = 4, speed = 8, spAttack = 16, spDefense = 32;
        if (Support(moves)) return hp | (s.BaseDefense >= s.BaseSpDefense ? defense : spDefense);
        int attacking = LeaningOf(s, moves) switch
        {
            Leaning.Physical => attack,
            Leaning.Special => spAttack,
            _ => attack | spAttack
        };
        return attacking | (Fast(s) ? speed : hp);
    }

    // The item, nature and effort of a set's moves as they now stand
    private static void Dress(FrontierSetRecord set, PokemonSpecies s, Dictionary<string, MoveData> byName)
    {
        var moves = set.Moves.Select(m => byName[m]).ToList();
        set.Item = ItemOf(s, moves);
        set.Nature = NatureOf(s, moves);
        set.EvFlags = EffortOf(s, moves);
    }

    // ------------------------------------------------------------------ reading

    /// <summary>
    /// One generation's file: each species' Showdown id to its roles, in the file's order. Generation 1's file has one role
    /// to a species, its moves, essential, exclusive and combo moves together, which is named "Random Battle"; a file whose
    /// sets name no role names each "Set".
    /// </summary>
    private static Dictionary<string, List<Role>> ReadRoles(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var bySpecies = new Dictionary<string, List<Role>>();
        foreach (var entry in doc.RootElement.EnumerateObject())
        {
            int? level = entry.Value.TryGetProperty("level", out var l) && l.ValueKind == JsonValueKind.Number ? l.GetInt32() : null;
            var roles = new List<Role>();
            if (entry.Value.TryGetProperty("sets", out var list))
            {
                foreach (var set in list.EnumerateArray())
                {
                    if (!set.TryGetProperty("movepool", out var pool)) continue;
                    string name = set.TryGetProperty("role", out var r) ? r.GetString() ?? "Set" : "Set";
                    var abilities = set.TryGetProperty("abilities", out var a) ? a.EnumerateArray().Select(x => x.GetString()!).ToList() : new List<string>();
                    roles.Add(new Role(name, pool.EnumerateArray().Select(m => MoveId(m.GetString()!)).Distinct().ToList(), abilities, level));
                }
            }
            else
            {
                var pool = new List<string>();
                foreach (string field in new[] { "moves", "essentialMoves", "exclusiveMoves", "comboMoves" })
                    if (entry.Value.TryGetProperty(field, out var moves)) pool.AddRange(moves.EnumerateArray().Select(m => MoveId(m.GetString()!)));
                if (pool.Count > 0) roles.Add(new Role("Random Battle", pool.Distinct().ToList(), new List<string>(), level));
            }
            if (roles.Count > 0) bySpecies[entry.Name] = roles;
        }
        return bySpecies;
    }

    private static string MoveId(string name)
    {
        string id = MasterTrainers.ShowdownId(name);
        return id.StartsWith("hiddenpower", StringComparison.Ordinal) ? "hiddenpower" : id;
    }

    /// <summary>A name as an id's part: lower-case letters and digits, words joined by underscores (<c>Fast Attacker</c> is <c>fast_attacker</c>).</summary>
    public static string Slug(string name)
    {
        var words = new List<string>();
        var word = new System.Text.StringBuilder();
        foreach (char c in name.Normalize(System.Text.NormalizationForm.FormD))
        {
            if (c == '♀') word.Append('f');
            else if (c == '♂') word.Append('m');
            else if (char.IsAsciiLetterOrDigit(c)) word.Append(char.ToLowerInvariant(c));
            else if (c is ' ' or '-' or '_' && word.Length > 0)
            {
                words.Add(word.ToString());
                word.Clear();
            }
        }
        if (word.Length > 0) words.Add(word.ToString());
        return string.Join("_", words);
    }

    // ------------------------------------------------------------------ the overrides

    /// <summary>
    /// Lays <c>Overrides/facility-sets.json</c> over the sets: an object of entries by set id, each the set's fields to
    /// change (read strictly as a set, so a mistyped field stops the importer), and one the import lacks added, after its
    /// species' sets, only when it says <c>"new": true</c>.
    /// </summary>
    public static void Apply(List<FrontierSetRecord> sets, string path, IReadOnlyList<PokemonSpecies> species)
    {
        if (!File.Exists(path)) return;
        string file = "Overrides/" + Path.GetFileName(path);
        var patches = JsonNode.Parse(File.ReadAllText(path), documentOptions: new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true })!.AsObject();
        foreach (var (id, entry) in patches)
        {
            var patch = entry!.DeepClone().AsObject();
            bool isNew = patch["new"] is JsonValue flag && flag.GetValueKind() == JsonValueKind.True;
            patch.Remove("new");
            GameDataFiles.ReadStrict<FrontierSetRecord>(patch.ToJsonString(), $"{file}: {id}");
            int at = sets.FindIndex(s => s.Id == id);
            if (at < 0 && !isNew) throw new InvalidDataException($"{file}: {id}: the import has no set of that id; a set the patch adds says \"new\": true");
            if (at >= 0 && isNew) throw new InvalidDataException($"{file}: {id}: says \"new\": true, but the import has a set of that id; take the key off to patch it");
            var node = at >= 0 ? JsonNode.Parse(GameDataFiles.Serialize(sets[at]))!.AsObject() : new JsonObject { ["id"] = id };
            foreach (var (key, value) in patch) node[key] = value?.DeepClone();
            var set = GameDataFiles.ReadStrict<FrontierSetRecord>(node.ToJsonString(), $"{file}: {id}");
            if (species.All(s => s.Name != set.Species)) throw new InvalidDataException($"{file}: {id}: {set.Species} is no species");
            if (at >= 0) sets[at] = set;
            else
            {
                // After the species' own, before the next species of the Pokédex
                int number = species.First(s => s.Name == set.Species).DexNumber;
                var numbers = species.ToDictionary(s => s.Name, s => s.DexNumber);
                int next = sets.FindIndex(s => numbers.GetValueOrDefault(s.Species) > number);
                sets.Insert(next < 0 ? sets.Count : next, set);
            }
        }
    }
}
