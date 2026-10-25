using System.Text;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

namespace DataImporter;

/// <summary>How much of something the engine runs.</summary>
public enum Works
{
    /// <summary>Everything it does in the original.</summary>
    Fully,
    /// <summary>Some of it: a move that hits without its own effect, a berry that works held but not from the bag.</summary>
    Partly,
    /// <summary>Nothing yet.</summary>
    NotYet,
    /// <summary>It has nothing to run: something to sell, to show or to hand to a character.</summary>
    NothingToRun
}

/// <summary>
/// The catalogue of plan 06: every move, ability and item the data holds, and how much of each the engine runs.
/// <see cref="Report"/> writes it out as docs/mechanics/coverage.md; <see cref="Tally"/> is the same in numbers,
/// which a test holds to a floor so the report can't get worse unnoticed.
/// </summary>
public static class Coverage
{
    public static string ReportPath(string repo) => Path.Combine(repo, "docs", "mechanics", "coverage.md");

    /// <summary>The catalogue in numbers: what there is of each kind and how much of it runs.</summary>
    public sealed record Numbers(
        int PlatinumMoves, int PlatinumMovesFully, int PlatinumMovesPartly,
        int LaterMoves, int LaterMovesFully, int LaterMovesPartly,
        int ZMoves, int MaxMoves, int GMaxMoves, int SpecialMovesRun,
        int PlatinumAbilities, int PlatinumAbilitiesRun, int LaterAbilities, int LaterAbilitiesRun,
        int PlatinumItems, int PlatinumItemsFully, int PlatinumItemsPartly, int PlatinumItemsNothingToRun,
        int HoldEffects, int HoldEffectsRun,
        int LaterItems, int MegaStones, int ZCrystals, int LaterItemsFully = 0, int LaterItemsPartly = 0, int LaterItemsNothingToRun = 0,
        int OurMoves = 0, int OurMovesFully = 0);

    public static Numbers Tally(IReadOnlyList<MoveData> moves, IReadOnlyList<AbilityDatabase.AbilityRecord> abilities, IReadOnlyList<ItemData> items)
    {
        var run = AbilityEffectTable.Names.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var standard = moves.Where(m => m.Kind == MoveKind.Standard).ToList();
        // Generation 0 is ours: the moves past level 100 (plan 06 · R31), counted apart and never in a number of the games'
        var platinum = standard.Where(m => m.Generation is >= 1 and <= 4).ToList();
        var ours = standard.Where(m => m.Generation == 0).ToList();
        var later = standard.Where(m => m.Generation > 4).ToList();
        var special = moves.Where(m => m.Kind != MoveKind.Standard).ToList();
        var own = items.Where(IsPlatinums).ToList();
        var held = own.Where(i => i.HoldEffect != null).ToList();
        return new Numbers(
            platinum.Count, platinum.Count(m => m.Support == MoveEffectSupport.Full), platinum.Count(m => m.Support == MoveEffectSupport.Partial),
            later.Count, later.Count(m => m.Support == MoveEffectSupport.Full), later.Count(m => m.Support == MoveEffectSupport.Partial),
            special.Count(m => m.Kind == MoveKind.ZMove), special.Count(m => m.Kind == MoveKind.MaxMove), special.Count(m => m.Kind == MoveKind.GMaxMove),
            special.Count(m => m.Support != MoveEffectSupport.None),
            abilities.Count(a => a.Generation <= 4), abilities.Count(a => a.Generation <= 4 && run.Contains(a.Name)),
            abilities.Count(a => a.Generation > 4), abilities.Count(a => a.Generation > 4 && run.Contains(a.Name)),
            own.Count, own.Count(i => Of(i) == Works.Fully), own.Count(i => Of(i) == Works.Partly), own.Count(i => Of(i) == Works.NothingToRun),
            held.Count, held.Count(HeldItemEffects.IsHoldable),
            items.Count(i => !IsPlatinums(i) && i.Id < 9000), items.Count(i => i.MegaStone != null), items.Count(i => i.ZCrystal != null),
            items.Count(i => IsLaters(i) && LaterOf(i) == Works.Fully), items.Count(i => IsLaters(i) && LaterOf(i) == Works.Partly),
            items.Count(i => IsLaters(i) && LaterOf(i) == Works.NothingToRun),
            ours.Count, ours.Count(m => m.Support == MoveEffectSupport.Full));
    }

    /// <summary>One of the later games' items (ids from 1000 to the game's own 9000).</summary>
    public static bool IsLaters(ItemData item) => item.Id >= 1000 && item.Id < 9000;

    // The later items the mechanics of plan 06 · R20 to R23 run by name: the Key Stone, the Z-Ring, the Dynamax Band
    // and the Tera Orb that let a trainer use them, and the two orbs of Primal Reversion
    private static readonly HashSet<string> RunByTheirMechanic = new() { "Key Stone", "Z-Ring", "Dynamax Band", "Tera Orb", "Red Orb", "Blue Orb", "Shiny Charm" };

    // The later items only there to be had (plan 06 · R28b): Unova's Pass Orbs, which bought the Entralink's powers, of a
    // link this game has none of. docs/mechanics/where-items-are-found.md says where each later item comes from
    private static readonly HashSet<string> OnlyToHave = new() { "Pass Orb" };

    /// <summary>
    /// How much of a later item the engine runs (plan 06 · R28a), on the same jobs as Platinum's: a ball's catch, a
    /// medicine's or battle item's use, what holding it does, an evolution it brings, a Mega Stone, a Z-Crystal or a
    /// key item of a later mechanic. One that has none of those yet (a mint, a candy, a nectar) is *not yet*.
    /// </summary>
    public static Works LaterOf(ItemData item)
    {
        if (item.MegaStone != null || item.ZCrystal != null || RunByTheirMechanic.Contains(item.Name)) return Works.Fully;
        if (OnlyToHave.Contains(item.Name)) return Works.NothingToRun;
        // Hyper training's caps by its rules (plan 06 · R28b), and the coins and candies a species evolves on by the hundred
        if (HyperTraining.IsCap(item) || Evolution.IsGatheredForEvolution(item)) return Works.Fully;
        var jobs = new List<Works>();
        if (item.Pocket == ItemPocket.PokeBalls) jobs.Add(PokemonPlatinumEngine.Battle.CatchCalculator.KnowsBall(item.Name) ? Works.Fully : Works.NotYet);
        else
        {
            if (item.BattleUse != null || item.BattlePocket != null) jobs.Add(BattleCore.CanUseInBattle(item) ? Works.Fully : Works.NotYet);
            if (item.FieldUse != null) jobs.Add(FieldUseOf(item));
        }
        if (item.HoldEffect != null) jobs.Add(HeldItemEffects.IsHoldable(item) ? Works.Fully : Works.NotYet);
        if (Evolution.IsUsedToEvolve(item) || Evolution.IsHeldForEvolution(item)) jobs.Add(Works.Fully);
        if (jobs.Count == 0) return Works.NotYet;
        if (jobs.All(j => j == Works.Fully)) return Works.Fully;
        return jobs.All(j => j == Works.NotYet) ? Works.NotYet : Works.Partly;
    }

    /// <summary>One of Platinum's own 446 items (later games' items have ids from 1000; the game's own from 9000).</summary>
    public static bool IsPlatinums(ItemData item) => item.Id < 1000;

    // ------------------------------------------------------------------ what an item has to run

    // What using an item on a Pokémon from the bag (Models/ItemUse.cs) carries out of its use parameters. Confusion
    // and infatuation only exist in battle, so an item that cures them has nothing more to do in the field
    private static readonly HashSet<string> FieldRuns = new()
    {
        "hpRestored", "healSleep", "healPoison", "healBurn", "healFreeze", "healParalysis", "healConfusion", "healAttract",
        "revive", "reviveAll", "levelUp", "ppUp", "ppMax", "ppRestored", "restorePPAllMoves",
        "hpEVs", "atkEVs", "defEVs", "speedEVs", "spatkEVs", "spdefEVs", "friendshipLow", "friendshipMed", "friendshipHigh"
    };

    // Balls that catch as they do in Platinum. The rest have an aftermath not written yet (a Luxury Ball's
    // friendship, a Heal Ball's cure) or belong to a place of their own (the Great Marsh, Pal Park)
    private static readonly HashSet<string> PlainBalls = new()
    {
        "Poké Ball", "Great Ball", "Ultra Ball", "Master Ball", "Premier Ball", "Cherish Ball", "Luxury Ball", "Heal Ball",
        "Net Ball", "Dive Ball", "Nest Ball", "Repeat Ball", "Timer Ball", "Dusk Ball", "Quick Ball"
    };

    /// <summary>
    /// How much of an item the engine runs, judged on its jobs: what using it does from the bag, what using it
    /// does in a battle, and what holding it does. Fling, Natural Gift and Pluck are counted with those moves, not here.
    /// </summary>
    public static Works Of(ItemData item)
    {
        var jobs = new List<Works>();
        if (item.Pocket == ItemPocket.PokeBalls) jobs.Add(PlainBalls.Contains(item.Name) ? Works.Fully : Works.Partly);
        else
        {
            if (item.FieldUse != null) jobs.Add(FieldUseOf(item));
            // The battle's bag knows every medicine and battle item of Platinum's (plan 06 · R8)
            if (item.BattleUse != null || (item.CanUseInBattle && item.Pocket == ItemPocket.BattleItems))
                jobs.Add(BattleCore.CanUseInBattle(item) ? Works.Fully : Works.NotYet);
        }
        if (item.HoldEffect != null) jobs.Add(HeldItemEffects.IsHoldable(item) || Evolution.IsHeldForEvolution(item) ? Works.Fully : Works.NotYet);

        if (jobs.Count == 0) return Works.NothingToRun;
        if (jobs.All(j => j == Works.Fully)) return Works.Fully;
        return jobs.All(j => j == Works.NotYet) ? Works.NotYet : Works.Partly;
    }

    /// <summary>
    /// What using it from the bag outside a battle does (plan 06 · R11): the field's own uses (the Bicycle, the rods,
    /// an Escape Rope, a Repel, a flute, Sacred Ash), teaching a TM or an HM, evolving, the Gracidea, and an item used
    /// on a Pokémon by the parameters above. A berry is also planted in soil, which waits for berry soil (R14).
    /// </summary>
    private static Works FieldUseOf(ItemData item)
    {
        if (BagScreen.UsedInField(item) || BagScreen.UsedInBag(item) || MoveTeaching.IsMachine(item) || item.FieldUse == "Gracidea" || LaterItemUse.Handles(item)
            || FormChanges.Handles(item))
            return Works.Fully;
        if (Evolution.IsUsedToEvolve(item) && !ItemUse.IsUsedOnPokemon(item)) return Works.Fully;
        if (!BagScreen.CanUse(item)) return Works.NotYet;
        bool runs = item.Use == null || item.Use.Keys.All(FieldRuns.Contains);
        return runs && item.FieldUse != "Berry" ? Works.Fully : Works.Partly;
    }

    private static string Word(Works works) => works switch
    {
        Works.Fully => "works",
        Works.Partly => "partly",
        Works.NotYet => "not yet",
        _ => "nothing to run"
    };

    // ------------------------------------------------------------------ the report

    public static string Report(IReadOnlyList<PokemonSpecies> species, IReadOnlyList<MoveData> moves, IReadOnlyList<AbilityDatabase.AbilityRecord> abilities, IReadOnlyList<ItemData> items, string? textFolder = null)
    {
        var implementedAbilities = AbilityEffectTable.Names.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var n = Tally(moves, abilities, items);
        var standard = moves.Where(m => m.Kind == MoveKind.Standard).ToList();
        var sb = new StringBuilder();
        void Line(string text = "") => sb.Append(text).Append('\n');

        Line("# Coverage");
        Line();
        Line("Generated by `tools/DataImporter` (don't edit by hand): the catalogue of plan 06. Every move, ability and item the data holds, " +
             "and how much of each the engine runs. Generations 1–4 use Platinum's own data; later generations use their newest games' data. " +
             "`CoverageTests` fails when this file is out of date or when a number below falls under its floor.");
        Line();

        Line("## The catalogue");
        Line();
        Line("| | In the data | Fully run | Partly run | Not yet |");
        Line("| --- | --- | --- | --- | --- |");
        Line($"| Platinum's moves | {n.PlatinumMoves} | {n.PlatinumMovesFully} | {n.PlatinumMovesPartly} | {n.PlatinumMoves - n.PlatinumMovesFully - n.PlatinumMovesPartly} |");
        Line($"| Moves of Generations 5–9 | {n.LaterMoves} | {n.LaterMovesFully} | {n.LaterMovesPartly} | {n.LaterMoves - n.LaterMovesFully - n.LaterMovesPartly} |");
        Line($"| Moves of our own, learned past level 100 | {n.OurMoves} | {n.OurMovesFully} | 0 | {n.OurMoves - n.OurMovesFully} |");
        Line($"| Z-Moves, Max Moves and G-Max Moves | {n.ZMoves} + {n.MaxMoves} + {n.GMaxMoves} | {n.SpecialMovesRun} | 0 | {n.ZMoves + n.MaxMoves + n.GMaxMoves - n.SpecialMovesRun} |");
        Line($"| Platinum's abilities | {n.PlatinumAbilities} | {n.PlatinumAbilitiesRun} | – | {n.PlatinumAbilities - n.PlatinumAbilitiesRun} |");
        Line($"| Abilities of Generations 5–9 | {n.LaterAbilities} | {n.LaterAbilitiesRun} | – | {n.LaterAbilities - n.LaterAbilitiesRun} |");
        Line($"| Platinum's items | {n.PlatinumItems} | {n.PlatinumItemsFully} | {n.PlatinumItemsPartly} | " +
             $"{n.PlatinumItems - n.PlatinumItemsFully - n.PlatinumItemsPartly - n.PlatinumItemsNothingToRun} (and {n.PlatinumItemsNothingToRun} with nothing to run) |");
        Line($"| of which hold effects | {n.HoldEffects} | {n.HoldEffectsRun} | – | {n.HoldEffects - n.HoldEffectsRun} |");
        Line($"| Items of later games | {n.LaterItems} | {n.LaterItemsFully} | {n.LaterItemsPartly} | {n.LaterItems - n.LaterItemsFully - n.LaterItemsPartly - n.LaterItemsNothingToRun} (and {n.LaterItemsNothingToRun} with nothing to run) |");
        Line();
        Line($"The data has {moves.Count} moves in all. The games number 919 of them, because each of the eighteen type Z-Moves has a physical and a special id; " +
             $"here each is one move, which with the {n.GMaxMoves} G-Max Moves makes {moves.Count - n.OurMoves}, and the {n.OurMoves} moves of our own (plan 06 · R31) make {moves.Count}.");
        Line();

        Line("## By generation");
        Line();
        Line("| Generation | Species | Moves | Moves fully run | Moves partly run | Moves that do nothing yet | Abilities | Abilities with an effect |");
        Line("| --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (int gen in species.Select(s => s.Generation).Concat(standard.Select(m => m.Generation)).Where(g => g > 0).Distinct().Order())
        {
            var genMoves = standard.Where(m => m.Generation == gen).ToList();
            var genAbilities = abilities.Where(a => a.Generation == gen).ToList();
            Line($"| {gen} | {species.Count(s => s.Generation == gen)} | {genMoves.Count} | {genMoves.Count(m => m.Support == MoveEffectSupport.Full)} | " +
                 $"{genMoves.Count(m => m.Support == MoveEffectSupport.Partial)} | {genMoves.Count(m => m.Support == MoveEffectSupport.None)} | " +
                 $"{genAbilities.Count} | {genAbilities.Count(a => implementedAbilities.Contains(a.Name))} |");
        }
        var ourMoves = standard.Where(m => m.Generation == 0).ToList();
        // Plan 03 · D14's species of our own are generation 0 too
        int ourSpecies = species.Count(s => s.Generation == 0);
        if (ourMoves.Count > 0 || ourSpecies > 0)
            Line($"| Ours | {ourSpecies} | {ourMoves.Count} | {ourMoves.Count(m => m.Support == MoveEffectSupport.Full)} | {ourMoves.Count(m => m.Support == MoveEffectSupport.Partial)} | " +
                 $"{ourMoves.Count(m => m.Support == MoveEffectSupport.None)} | 0 | 0 |");
        Line($"| All | {species.Count} | {standard.Count} | {standard.Count(m => m.Support == MoveEffectSupport.Full)} | " +
             $"{standard.Count(m => m.Support == MoveEffectSupport.Partial)} | {standard.Count(m => m.Support == MoveEffectSupport.None)} | " +
             $"{abilities.Count} | {abilities.Count(a => implementedAbilities.Contains(a.Name))} |");
        Line();
        var forms = species.SelectMany(s => s.Forms ?? new()).ToList();
        int Forms(FormKind kind) => forms.Count(f => f.Kind == kind);
        Line($"**Forms** (plan 03 · D11): {forms.Count}, of {species.Count(s => s.Forms != null)} species: {Forms(FormKind.Mega)} Mega, {Forms(FormKind.Primal)} Primal, " +
             $"{Forms(FormKind.Gigantamax)} Gigantamax, {Forms(FormKind.Regional)} regional, {Forms(FormKind.Battle)} for battle only, {Forms(FormKind.Alternate)} others " +
             $"with values of their own and {Forms(FormKind.Look)} that only look different. {species.Count(s => s.Modern is { Types: not null } or { BaseStats: not null })} species and " +
             $"{forms.Count(f => f.Modern is { Types: not null } or { BaseStats: not null })} forms have other types or base stats in the newest games, and {species.Count(s => s.Modern?.Learnset != null)} species and {forms.Count(f => f.Modern?.Learnset != null)} forms learn another list of moves by level there, which every game learns by (plan 06 · R19).");
        Line();

        Line("## Move effects still to write");
        Line();
        Line("*Partly run*: the move hits (or its stat change, status or healing happens) but its own effect is missing. " +
             "*Does nothing yet*: using it says \"But nothing happened!\", and trainers only pick it when they have nothing else.");
        Line();
        // Only what doesn't run in full (until plan 06 · R31 the list named every move with an effect of its own)
        var toWrite = standard.Where(m => m.Effect != null && m.Support != MoveEffectSupport.Full).ToList();
        if (toWrite.Count == 0) Line("None: every move's effect runs in full.");
        else
        {
            Line("| Effect | Support | Moves |");
            Line("| --- | --- | --- |");
        }
        foreach (var group in toWrite.GroupBy(m => (m.Effect, m.Support)).OrderByDescending(g => g.Count()).ThenBy(g => g.Key.Effect))
        {
            string support = group.Key.Support == MoveEffectSupport.Partial ? "partly run" : "does nothing yet";
            Line($"| {group.Key.Effect} | {support} | {string.Join(", ", group.Select(m => m.Name))} |");
        }
        Line();

        Line("## Abilities without an effect");
        Line();
        foreach (var gen in abilities.Where(a => !implementedAbilities.Contains(a.Name)).GroupBy(a => a.Generation).OrderBy(g => g.Key))
            Line($"- Generation {gen.Key} ({gen.Count()}): {string.Join(", ", gen.Select(a => a.Name))}");
        Line();

        Line("## Platinum's items");
        Line();
        Line("An item has up to two jobs: what using it does (from the bag and in battle) and what holding it does. It *works* when all of that runs, " +
             "*partly* when some does, and has *nothing to run* when it is only there to be sold, shown or handed over. " +
             "What Fling, Natural Gift and Pluck do with an item is counted with those moves.");
        Line();
        var own = items.Where(IsPlatinums).ToList();
        Line("| Pocket | Items | Works | Partly | Not yet | Nothing to run |");
        Line("| --- | --- | --- | --- | --- | --- |");
        foreach (var pocket in own.GroupBy(i => i.Pocket).OrderBy(g => g.Key))
            Line($"| {pocket.Key} | {pocket.Count()} | {pocket.Count(i => Of(i) == Works.Fully)} | {pocket.Count(i => Of(i) == Works.Partly)} | " +
                 $"{pocket.Count(i => Of(i) == Works.NotYet)} | {pocket.Count(i => Of(i) == Works.NothingToRun)} |");
        Line();
        foreach (var status in new[] { Works.Fully, Works.Partly, Works.NotYet })
        {
            Line($"**{char.ToUpperInvariant(Word(status)[0])}{Word(status)[1..]}**");
            Line();
            foreach (var pocket in own.Where(i => Of(i) == status).GroupBy(i => i.Pocket).OrderBy(g => g.Key))
                Line($"- {pocket.Key} ({pocket.Count()}): {string.Join(", ", pocket.Select(i => i.Name))}");
            Line();
        }

        var held = own.Where(i => i.HoldEffect != null).ToList();
        Line($"Hold effects not yet run ({held.Count(i => !HeldItemEffects.IsHoldable(i))} of {held.Count} items):");
        Line();
        foreach (var group in held.Where(i => !HeldItemEffects.IsHoldable(i)).GroupBy(i => i.HoldEffect).OrderBy(g => g.Key))
            Line($"- {group.Key}: {string.Join(", ", group.Select(i => i.Name))}");
        Line();
        Line("Outside a battle, using an item runs for the effect types the bag knows (`HealHP`, `HealStatus`, `Revive`, `FullRestore`, `LevelUp`) " +
             "and for the items species evolve with. In a battle every medicine and battle item of Platinum's runs (`BattleCore.CanUseInBattle`): HP, " +
             "a condition, confusion, infatuation, a revival, PP, the X items, Dire Hit, Guard Spec., and a Poké Doll's or Fluffy Tail's escape. " +
             "Balls catch by Platinum's own formula, the ones with a condition (Net, Dusk, Timer and the like) included.");
        Line();

        Line("## The later games' items");
        Line();
        Line("Judged as Platinum's are (plan 06 · R28a): a ball by its catch, medicine and battle items by their use, a held item by its hold effect " +
             "(`Battle/Effects/HeldItemEffects.Later.cs`), an evolution item by the evolution it brings, a Mega Stone, a Z-Crystal and a later mechanic's key " +
             "item by that mechanic; since R28b a mint, a candy, a feather, a nectar or a capsule by what using it from the bag does (`Models/LaterItemUse.cs`, `EffortRules`), " +
             "a TR by the species that learn it, a bottle cap by hyper training's rules (`Models/HyperTraining.cs`) and a coin or candy gathered for an evolution by that evolution. " +
             "An item only there to be had has *nothing to run*; where each is found is `docs/mechanics/where-items-are-found.md`.");
        Line();
        var later = items.Where(IsLaters).ToList();
        Line("| Pocket | Items | Works | Partly | Not yet | Nothing to run |");
        Line("| --- | --- | --- | --- | --- | --- |");
        foreach (var pocket in later.GroupBy(i => i.Pocket).OrderBy(g => g.Key))
            Line($"| {pocket.Key} | {pocket.Count()} | {pocket.Count(i => LaterOf(i) == Works.Fully)} | {pocket.Count(i => LaterOf(i) == Works.Partly)} | " +
                 $"{pocket.Count(i => LaterOf(i) == Works.NotYet)} | {pocket.Count(i => LaterOf(i) == Works.NothingToRun)} |");
        Line();
        foreach (var status in new[] { Works.Partly, Works.NotYet, Works.NothingToRun })
        {
            Line($"**{char.ToUpperInvariant(Word(status)[0])}{Word(status)[1..]}**");
            Line();
            foreach (var pocket in later.Where(i => LaterOf(i) == status).GroupBy(i => i.Pocket).OrderBy(g => g.Key))
                Line($"- {pocket.Key} ({pocket.Count()}): {string.Join(", ", pocket.Select(i => i.Name))}");
            Line();
        }
        Line($"Held items that work (of {later.Count(i => i.HoldEffect != null)} with a hold effect): " +
             string.Join(", ", later.Where(i => i.HoldEffect != null && HeldItemEffects.IsHoldable(i) && i.MegaStone == null && i.ZCrystal == null).Select(i => i.Name)) + ".");
        Line();

        Line("## The later mechanics' data");
        Line();
        Line("Nothing here runs yet: it is what plan 06 · R20–R23 write their rules from.");
        Line();
        Line($"- **Z-Moves**: {n.ZMoves} ({moves.Count(m => m.Kind == MoveKind.ZMove && m.Power == 0)} that take their power from the move they are made of, " +
             $"{moves.Count(m => m.Kind == MoveKind.ZMove && m.Power > 0)} with a power of their own). {standard.Count(m => m.ZPower > 0)} moves have a Z-Move power, " +
             $"{standard.Count(m => m.ZBonus != null)} status moves a Z-Power bonus.");
        Line($"- **Max Moves**: {n.MaxMoves}, and {n.GMaxMoves} G-Max Moves for {moves.Where(m => m.GigantamaxOf != null).Select(m => m.GigantamaxOf).Distinct().Count()} Gigantamax forms. " +
             $"{standard.Count(m => m.MaxPower > 0)} moves have a Max Move power.");
        Line($"- **Mega Stones**: {n.MegaStones}, for {items.Where(i => i.MegaStone != null).Select(i => i.MegaStone!.Form).Distinct().Count()} forms of " +
             $"{items.Where(i => i.MegaStone != null).Select(i => i.MegaStone!.Species).Distinct().Count()} species, each a form in `species.json`; Mega Evolving in battle is plan 06's.");
        Line($"- **Z-Crystals**: {n.ZCrystals} ({items.Count(i => i.ZCrystal?.Type != null)} for a type, {items.Count(i => i.ZCrystal?.Move != null)} for one species' move).");
        Line($"- **Modern values**: {standard.Count(m => m.Modern != null)} of Platinum's moves have other values in the newest games, which a game played by the modern rules uses.");
        Line();

        Line("## Evolutions");
        Line();
        var evolutions = species.SelectMany(s => s.Evolutions ?? new()).ToList();
        Line($"{evolutions.Count} evolutions. `Models/Evolution.cs` has a rule for every method (docs/mechanics/evolution.md explains each); " +
             "some can't be met in the game until something else is built:");
        Line();
        Line("| Method | Evolutions | Set off by | Waits for |");
        Line("| --- | --- | --- | --- |");
        foreach (var group in evolutions.GroupBy(e => e.Method).OrderByDescending(g => g.Count()).ThenBy(g => g.Key.ToString()))
        {
            string trigger = Evolution.TriggerOf(group.Key) switch
            {
                EvolutionTrigger.LevelUp => "a level-up",
                EvolutionTrigger.UseItem => "an item used on it",
                EvolutionTrigger.Trade => "a trade",
                EvolutionTrigger.Spin => "the player spinning",
                EvolutionTrigger.BattleEnd => "the end of a battle",
                _ => group.Key == EvolutionMethod.LevelShedinja ? "Nincada evolving" : "nothing yet"
            };
            Line($"| {group.Key} | {group.Count()} | {trigger} | {WaitsFor(group.Key)} |");
        }
        if (textFolder != null) Languages(sb, textFolder, species, moves, abilities, items);
        return sb.ToString();
    }

    /// <summary>
    /// Plan 17 · N3: the names each language has (<c>Data/text/&lt;tag&gt;/names.json</c>, from the importer) against what the
    /// data holds, and the descriptions PokeAPI's contributors wrote where a language has them. English is the data itself;
    /// a language with no file shows the English names, and a language's hand-written <c>names.&lt;kind&gt;.json</c> are counted
    /// with its <c>names.json</c> (Romanian has those alone: PokeAPI has no Romanian).
    /// </summary>
    private static void Languages(StringBuilder sb, string textFolder, IReadOnlyList<PokemonSpecies> species, IReadOnlyList<MoveData> moves,
        IReadOnlyList<AbilityDatabase.AbilityRecord> abilities, IReadOnlyList<ItemData> items)
    {
        sb.Append("\n## Languages\n\n");
        sb.Append("Names and descriptions per language (plan 17 · N3), counted from `Data/text/<tag>/names.json`, which the importer writes from PokeAPI's own tables: ");
        sb.Append("a name the file lacks is shown in English. Descriptions are PokeAPI's short prose where a language has it; the games' own text is never read. ");
        sb.Append("A language marked (own) also has hand-written `names.<kind>.json` files beside it (plan 17 · N6: Romanian's names and descriptions, made up by us), which the importer never writes and whose entries win.\n\n");
        sb.Append($"| Language | Species ({species.Count}) | Forms | Moves ({moves.Count}) | Abilities ({abilities.Count}) | Items ({items.Count}) | Places | Move descriptions | Ability descriptions | Item descriptions |\n");
        sb.Append("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |\n");
        foreach (var dir in Directory.GetDirectories(textFolder).OrderBy(d => Path.GetFileName(d), StringComparer.Ordinal))
        {
            // The importer's names.json and the hand-written names.<kind>.json beside it, read as LocalNames reads them
            var files = new List<string>();
            string main = Path.Combine(dir, NameFiles.FileName);
            if (File.Exists(main)) files.Add(main);
            files.AddRange(Directory.GetFiles(dir, LocalNames.OwnFilePattern).Where(f => Path.GetFileName(f) != NameFiles.FileName)
                .OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal));
            if (files.Count == 0) continue;
            var keys = new Dictionary<string, HashSet<string>>();
            void Take(System.Text.Json.JsonElement element, string name)
            {
                if (!keys.TryGetValue(name, out var set)) keys[name] = set = new HashSet<string>(StringComparer.Ordinal);
                foreach (var entry in element.EnumerateObject()) set.Add(entry.Name);
            }
            foreach (string file in files)
            {
                using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(file));
                foreach (var section in document.RootElement.EnumerateObject())
                    if (section.Name == "descriptions") foreach (var d in section.Value.EnumerateObject()) Take(d.Value, "descriptions." + d.Name);
                    else Take(section.Value, section.Name);
            }
            int Count(string name) => keys.TryGetValue(name, out var set) ? set.Count : 0;
            string own = files.Count > 1 || Path.GetFileName(files[0]) != NameFiles.FileName ? " (own)" : "";
            sb.Append($"| {Path.GetFileName(dir)}{own} | {Count("species")} | {Count("forms")} | {Count("moves")} | {Count("abilities")} | {Count("items")} | {Count("places")} | " +
                      $"{Count("descriptions.moves")} | {Count("descriptions.abilities")} | {Count("descriptions.items")} |\n");
        }
    }

    /// <summary>What has to exist before an evolution method can be met in play (empty: it already can).</summary>
    private static string WaitsFor(EvolutionMethod method) => method switch
    {
        EvolutionMethod.Trade or EvolutionMethod.TradeHoldingItem or EvolutionMethod.TradeWithSpecies
            => "trading (plan 07 · O5; in-game trades in plan 06 · R12). A Linking Cord used on the Pokémon does the same today",
        EvolutionMethod.Beauty => "Poffins (plan 06 · R14)",
        EvolutionMethod.LevelAtLocation => "maps with a Moss Rock, an Ice Rock or a magnetic field (plan 01)",
        EvolutionMethod.LevelInRain => "weather in the field (plan 01)",
        EvolutionMethod.LevelWithItemsInBag => "a way to collect the items (plan 03 · D12)",
        EvolutionMethod.LevelAfterDamage => "a map with a Stone Arch (plan 03 · D12)",
        EvolutionMethod.Other => "a rule of its own",
        _ => ""
    };
}
