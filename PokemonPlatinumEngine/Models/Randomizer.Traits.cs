using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The randomiser's second half (plan 13 · V7; <c>docs/mechanics/rulings.md</c>, "The randomiser: abilities, moves and
/// types"): what belongs to a species itself (its abilities, the moves it learns by level and, behind its own toggle, its
/// types) is swapped in memory for the game in progress, the way <see cref="Ruleset.Use"/> swaps in the modern values,
/// and put back before the rules are next set (<see cref="PutBack"/>, which <see cref="Ruleset.Use"/> calls first), so a
/// new game, a load or a test that sets the rules back finds the data as it was loaded. Every choice is decided by the
/// seed and the species' or line's number, never by chance. Only the games' 1,025 and their forms are touched; a species
/// that changes its own shape by its ability (Arceus, Castform, Shedinja…) keeps its abilities and types.
/// </summary>
public sealed partial class Randomizer
{
    // ------------------------------------------------------------------ in force, and put back

    private sealed record SpeciesBefore(PokemonSpecies Species, List<string> Abilities, PokemonType Primary, PokemonType? Secondary, List<LearnableMove> Learnset);
    private sealed record FormBefore(PokemonForm Form, List<string>? Abilities, List<PokemonType>? Types, List<LearnableMove>? Learnset);

    private static readonly object UseGate = new();
    private static List<SpeciesBefore>? speciesBefore;
    private static List<FormBefore>? formsBefore;
    private static Dictionary<string, (PokemonType Primary, PokemonType? Secondary)>? typesBefore;

    /// <summary>The options whose swap is in force now; null when the data is as loaded.</summary>
    public static RandomizerOptions? InForce { get; private set; }

    /// <summary>
    /// Swaps the run's abilities, learnsets and types into the species, in place (the engine alone calls it, after
    /// <see cref="Ruleset.Use"/>, as a game begins or loads; a test that calls it runs alone and sets the rules back after).
    /// Puts back what an earlier call swapped first. A run that changes nothing of the species swaps nothing.
    /// </summary>
    public static void Use(RunRules? run)
    {
        lock (UseGate)
        {
            PutBack();
            if (run?.Random is not { ChangesSpecies: true } options) return;
            Of(options).Apply();
            InForce = options;
        }
    }

    /// <summary>Puts every species and form back as it was before the swap; nothing when none is in force.</summary>
    public static void PutBack()
    {
        lock (UseGate)
        {
            if (speciesBefore != null)
                foreach (var b in speciesBefore)
                {
                    b.Species.Abilities = b.Abilities;
                    b.Species.PrimaryType = b.Primary;
                    b.Species.SecondaryType = b.Secondary;
                    b.Species.Learnset = b.Learnset;
                }
            if (formsBefore != null)
                foreach (var f in formsBefore)
                {
                    f.Form.Abilities = f.Abilities;
                    f.Form.Types = f.Types;
                    f.Form.Learnset = f.Learnset;
                }
            speciesBefore = null;
            formsBefore = null;
            typesBefore = null;
            InForce = null;
        }
    }

    /// <summary>
    /// A species' or form's types as the data loaded them, whatever a swap has done (for whoever reads a species' look
    /// from its types: a generated model's genome must not change with a run).
    /// </summary>
    public static (PokemonType Primary, PokemonType? Secondary) TypesAsLoaded(string name, PokemonType primary, PokemonType? secondary)
    {
        var before = typesBefore;
        return before != null && before.TryGetValue(name, out var t) ? t : (primary, secondary);
    }

    private void Apply()
    {
        var species = new List<SpeciesBefore>();
        var forms = new List<FormBefore>();
        var types = new Dictionary<string, (PokemonType, PokemonType?)>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in Everything.Value)
        {
            var s = e.Species;
            species.Add(new SpeciesBefore(s, s.Abilities, s.PrimaryType, s.SecondaryType, s.Learnset));
            types[s.Name] = (s.PrimaryType, s.SecondaryType);
            foreach (var f in s.Forms ?? new())
            {
                forms.Add(new FormBefore(f, f.Abilities, f.Types, f.Learnset));
                if (f.Types is { Count: > 0 } ft) types[f.Name] = (ft[0], ft.Count > 1 ? ft[1] : null);
            }
        }
        speciesBefore = species;
        formsBefore = forms;
        typesBefore = types;

        foreach (var b in species)
        {
            var s = b.Species;
            var e = ByName.Value[s.Name];
            int root = RootOf(e).Dex;
            bool fixedShape = KeepsItsShape(s.Abilities) || (s.Forms ?? new()).Any(f => KeepsItsShape(f.Abilities));
            if (Options.Types && !fixedShape)
            {
                s.PrimaryType = TypeIn(root, b.Primary);
                s.SecondaryType = b.Secondary is { } second ? TypeIn(root, second) : null;
            }
            if (Options.Abilities && !fixedShape) s.Abilities = AbilitiesFor(root, b.Abilities.Count);
            if (Options.Moves) s.Learnset = Relearned(b.Learnset, (uint)e.Dex, b.Primary, b.Secondary, fixedShape ? null : root);
        }
        foreach (var f in forms)
        {
            var owner = species.First(b => b.Species.Forms?.Contains(f.Form) == true);
            var e = ByName.Value[owner.Species.Name];
            int root = RootOf(e).Dex;
            bool fixedShape = KeepsItsShape(owner.Abilities) || KeepsItsShape(f.Abilities);
            if (Options.Types && !fixedShape && f.Types is { Count: > 0 } ft) f.Form.Types = ft.Select(t => TypeIn(root, t)).ToList();
            if (Options.Abilities && !fixedShape && f.Abilities is { Count: > 0 } fa)
                f.Form.Abilities = AbilitiesFor((int)(SplitMix.Fnv(f.Form.Name) & 0x7FFFFFFF), fa.Count);
            if (Options.Moves && f.Learnset != null)
            {
                var ft2 = f.Types is { Count: > 0 } t ? t : new List<PokemonType> { owner.Primary };
                f.Form.Learnset = Relearned(f.Learnset, SplitMix.Fnv(f.Form.Name), ft2[0], ft2.Count > 1 ? ft2[1] : null, fixedShape ? null : root);
            }
        }
    }

    // ------------------------------------------------------------------ the types

    private readonly Dictionary<int, PokemonType[]> shuffles = new();

    /// <summary>
    /// What a type is in a line's run: one shuffle of the eighteen types for each line (by its first stage's number), so a
    /// line's stages keep what they share and two types of one species stay two. Itself when types aren't randomised.
    /// </summary>
    public PokemonType TypeIn(int lineRoot, PokemonType type)
    {
        if (!Options.Types) return type;
        lock (shuffles)
        {
            if (!shuffles.TryGetValue(lineRoot, out var order))
            {
                order = Enum.GetValues<PokemonType>();
                var r = new SplitMix(SplitMix.Mix(seed, (uint)lineRoot, 0x545950u));
                for (int i = order.Length - 1; i > 0; i--)
                {
                    int j = r.Int(i + 1);
                    (order[i], order[j]) = (order[j], order[i]);
                }
                shuffles[lineRoot] = order;
            }
            return order[(int)type];
        }
    }

    /// <summary>The number of the first stage of a species' line, which its line's types and abilities are decided by.</summary>
    public static int LineOf(string species) =>
        ByName.Value.TryGetValue(species, out var e) ? RootOf(e).Dex : PokemonDatabase.Get(species)?.DexNumber ?? 0;

    // ------------------------------------------------------------------ the abilities

    /// <summary>
    /// The abilities that make or keep a species' own shape or depend on it, which no species is given and whose holders
    /// keep theirs (and their types): Wonder Guard among them, which would make a Pokémon nothing can touch.
    /// </summary>
    public static readonly IReadOnlySet<string> ShapeAbilities = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Wonder Guard", "Multitype", "Forecast", "Flower Gift", "Stance Change", "Zen Mode", "Schooling", "Shields Down",
        "Disguise", "Gulp Missile", "Ice Face", "Hunger Switch", "Zero to Hero", "Power Construct", "Battle Bond",
        "RKS System", "Comatose", "Commander", "Tera Shift", "Protosynthesis", "Quark Drive", "Mimicry", "Poison Puppeteer"
    };

    private static bool KeepsItsShape(IEnumerable<string>? abilities) => abilities?.Any(ShapeAbilities.Contains) == true;

    /// <summary>The abilities a species may be given: Platinum's own, which the game runs in full, less the ones of a shape.</summary>
    private static readonly Lazy<IReadOnlyList<string>> AbilityPool = new(() =>
        Enumerable.Range(1, 123).Select(AbilityDatabase.NameOf).OfType<string>()
            .Where(n => !ShapeAbilities.Contains(n)).ToList());

    /// <summary>A line's abilities: as many as the species had, two never the same, decided by the line's number and the slot.</summary>
    private List<string> AbilitiesFor(int line, int count)
    {
        var pool = AbilityPool.Value;
        var chosen = new List<string>();
        for (int slot = 0; slot < Math.Max(1, count); slot++)
        {
            int at = (int)(SplitMix.Mix(seed, (uint)line, 0x41420000u + (uint)slot) % (uint)pool.Count);
            while (chosen.Contains(pool[at], StringComparer.OrdinalIgnoreCase)) at = (at + 1) % pool.Count;
            chosen.Add(pool[at]);
        }
        return chosen;
    }

    // ------------------------------------------------------------------ the moves

    private sealed record MoveFacts(MoveData Data, PokemonType Type, bool Status, int Power);

    /// <summary>
    /// The moves a species may learn or a trainer's Pokémon be taught instead, by Platinum's values whatever the rules:
    /// Platinum's own (all run in full) but Struggle, and since Platinum has no Fairy move, the later games' Fairy moves
    /// that run in full, so a Fairy line keeps a move of its type.
    /// </summary>
    private static readonly Lazy<IReadOnlyList<MoveFacts>> MovePool = new(() =>
        MoveDatabase.GetAll().Where(m => m.Kind == MoveKind.Standard && m.Name != "Struggle"
                && (m.Id >= 1 && m.Id <= LastPlatinumMove
                    || m.Type == PokemonType.Fairy && m.Id < OursMovesFrom && m.Generation > 0 && m.Support == MoveEffectSupport.Full))
            .Select(Facts).OrderBy(f => f.Data.Id).ToList());

    /// <summary>The last move of Platinum's own table.</summary>
    public const int LastPlatinumMove = 467;

    /// <summary>Where the moves of our own learned past level 100 begin (plan 06 · R31).</summary>
    private const int OursMovesFrom = 3000;

    private static MoveFacts Facts(MoveData move)
    {
        var p = move.Under(Ruleset.Platinum);
        return new MoveFacts(move, p.Type, p.Category == MoveCategory.Status, p.Power <= 1 ? 60 : p.Power);
    }

    /// <summary>
    /// A move in place of another: a status move for a status move, otherwise a damaging move of about its power (within a
    /// fifth, the margin widening until a few are found) and, where <paramref name="type"/> is given, of that type. One of
    /// <paramref name="taken"/> only when nothing else is left. Decided by the seed and the two numbers.
    /// </summary>
    private MoveData MoveLike(MoveData move, PokemonType? type, uint who, uint slot, ICollection<string> taken)
    {
        var was = Facts(move);
        var pool = MovePool.Value.Where(m => m.Status == was.Status && !taken.Contains(m.Data.Name)).ToList();
        if (type is { } t)
        {
            // A move of the type, one already taken where every move of it is (a type keeps its moves before it keeps them apart)
            var typed = pool.Where(m => m.Type == t).ToList();
            if (typed.Count == 0) typed = MovePool.Value.Where(m => m.Status == was.Status && m.Type == t).ToList();
            if (typed.Count > 0) pool = typed;
        }
        if (pool.Count == 0) return move;
        var near = pool;
        if (!was.Status)
            foreach (int margin in new[] { 20, 40, 60, 100, 1000 })
            {
                near = pool.Where(m => Math.Abs(m.Power - was.Power) * 100 <= was.Power * margin).ToList();
                if (near.Count >= 4 || margin == 1000) break;
            }
        if (near.Count == 0) near = pool;
        return near[(int)(SplitMix.Mix(seed, who, 0x4D560000u ^ slot) % (uint)near.Count)].Data;
    }

    /// <summary>
    /// A learnset with every move another (at the same level, in the same order): a move of one of the species' own types
    /// stays a move of that type (which the line's shuffle moves with it when types are randomised), any other may be of
    /// any type, and no move is learned twice. Moves not in the data are left as they are.
    /// </summary>
    private List<LearnableMove> Relearned(List<LearnableMove> learnset, uint who, PokemonType primary, PokemonType? secondary, int? line)
    {
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<LearnableMove>(learnset.Count);
        uint slot = 0;
        foreach (var l in learnset)
        {
            slot++;
            if (!MoveDatabase.Has(l.MoveName)) { result.Add(l); continue; }
            var move = MoveDatabase.Get(l.MoveName);
            // Our own moves learned past level 100 (plan 06 · R31) are a line's own climb, and stay
            if (move.Id >= OursMovesFrom) { result.Add(l); continue; }
            var facts = Facts(move);
            PokemonType? own = !facts.Status && (facts.Type == primary || facts.Type == secondary) ? (line is { } shuffled ? TypeIn(shuffled, facts.Type) : facts.Type) : null;
            var to = MoveLike(move, own, who, slot, taken);
            taken.Add(to.Name);
            result.Add(new LearnableMove { Level = l.Level, MoveName = to.Name });
        }
        return result;
    }

    /// <summary>
    /// A trainer's Pokémon as a randomised run fights it: its ability picked again from what its species has now (when
    /// abilities are randomised) and, when moves are and its species stayed, each chosen move another of its type and about
    /// its power, so a leader keeps a move of their type. Decided by the trainer's id and the member's place.
    /// </summary>
    internal Pokemon Retaught(Pokemon p, string trainerId, int member, bool sameSpecies)
    {
        if (p.IsEgg) return p;
        if (Options.Abilities) p.AbilityName = Models.Personality.AbilityOf(p.Abilities, p.Personality);
        if (Options.Moves && sameSpecies)
        {
            uint who = SplitMix.Mix(SplitMix.Fnv(trainerId), (uint)member, 0x54524Eu);
            var taken = new HashSet<string>(p.Moves.Select(m => m.Data.Name), StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < p.Moves.Count; i++)
            {
                var old = p.Moves[i];
                taken.Remove(old.Data.Name);
                var facts = Facts(old.Data);
                var to = MoveLike(old.Data, facts.Status ? null : facts.Type, who, (uint)i + 1, taken);
                taken.Add(to.Name);
                if (to != old.Data) p.Moves[i] = new Move(to);
            }
        }
        return p;
    }
}
