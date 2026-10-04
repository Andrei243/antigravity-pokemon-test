using System.Text;
using PokemonPlatinumEngine.Battle.Effects;
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
        int LaterItems, int MegaStones, int ZCrystals);

    public static Numbers Tally(IReadOnlyList<MoveData> moves, IReadOnlyList<AbilityDatabase.AbilityRecord> abilities, IReadOnlyList<ItemData> items)
    {
        var run = AbilityEffectTable.Names.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var standard = moves.Where(m => m.Kind == MoveKind.Standard).ToList();
        var platinum = standard.Where(m => m.Generation <= 4).ToList();
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
            items.Count(i => !IsPlatinums(i) && i.Id < 9000), items.Count(i => i.MegaStone != null), items.Count(i => i.ZCrystal != null));
    }

    /// <summary>One of Platinum's own 446 items (later games' items have ids from 1000; the game's own from 9000).</summary>
    public static bool IsPlatinums(ItemData item) => item.Id < 1000;

    // ------------------------------------------------------------------ what an item has to run

    // What the bag's medicine rule (Models/FieldItems.cs) carries out of an item's use parameters
    private static readonly HashSet<string> MedicineRuns = new()
    {
        "hpRestored", "healSleep", "healPoison", "healBurn", "healFreeze", "healParalysis", "healConfusion", "revive", "levelUp"
    };

    // Balls that catch like their Platinum selves: the rest have a condition or an aftermath not written yet
    private static readonly HashSet<string> PlainBalls = new() { "Poké Ball", "Great Ball", "Ultra Ball", "Master Ball", "Premier Ball", "Cherish Ball" };

    /// <summary>
    /// How much of an item the engine runs, judged on its two jobs: what using it does (from the bag and in
    /// battle) and what holding it does. Fling, Natural Gift and Pluck are counted with those moves, not here.
    /// </summary>
    public static Works Of(ItemData item)
    {
        var jobs = new List<Works>();
        if (item.Pocket == ItemPocket.PokeBalls) jobs.Add(PlainBalls.Contains(item.Name) ? Works.Fully : Works.Partly);
        else if (item.FieldUse != null || item.BattleUse != null) jobs.Add(UseOf(item));
        if (item.HoldEffect != null) jobs.Add(HeldItemEffects.IsHoldable(item) || Evolution.IsHeldForEvolution(item) ? Works.Fully : Works.NotYet);

        if (jobs.Count == 0) return Works.NothingToRun;
        if (jobs.All(j => j == Works.Fully)) return Works.Fully;
        return jobs.All(j => j == Works.NotYet) ? Works.NotYet : Works.Partly;
    }

    private static Works UseOf(ItemData item)
    {
        if (!BagScreen.CanUse(item)) return Works.NotYet;
        // In battle only HP is restored so far; outside it the medicine rule runs the parameters above
        bool allRun = item.Use == null || item.Use.Keys.All(MedicineRuns.Contains);
        bool battleRuns = item.BattleUse == null || item.EffectType == ItemEffectType.HealHP;
        return allRun && battleRuns ? Works.Fully : Works.Partly;
    }

    private static string Word(Works works) => works switch
    {
        Works.Fully => "works",
        Works.Partly => "partly",
        Works.NotYet => "not yet",
        _ => "nothing to run"
    };

    // ------------------------------------------------------------------ the report

    public static string Report(IReadOnlyList<PokemonSpecies> species, IReadOnlyList<MoveData> moves, IReadOnlyList<AbilityDatabase.AbilityRecord> abilities, IReadOnlyList<ItemData> items)
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
        Line($"| Z-Moves, Max Moves and G-Max Moves | {n.ZMoves} + {n.MaxMoves} + {n.GMaxMoves} | {n.SpecialMovesRun} | 0 | {n.ZMoves + n.MaxMoves + n.GMaxMoves - n.SpecialMovesRun} |");
        Line($"| Platinum's abilities | {n.PlatinumAbilities} | {n.PlatinumAbilitiesRun} | – | {n.PlatinumAbilities - n.PlatinumAbilitiesRun} |");
        Line($"| Abilities of Generations 5–9 | {n.LaterAbilities} | {n.LaterAbilitiesRun} | – | {n.LaterAbilities - n.LaterAbilitiesRun} |");
        Line($"| Platinum's items | {n.PlatinumItems} | {n.PlatinumItemsFully} | {n.PlatinumItemsPartly} | " +
             $"{n.PlatinumItems - n.PlatinumItemsFully - n.PlatinumItemsPartly - n.PlatinumItemsNothingToRun} (and {n.PlatinumItemsNothingToRun} with nothing to run) |");
        Line($"| of which hold effects | {n.HoldEffects} | {n.HoldEffectsRun} | – | {n.HoldEffects - n.HoldEffectsRun} |");
        Line($"| Items of later games | {n.LaterItems} | – | – | – |");
        Line();
        Line($"The data has {moves.Count} moves in all. The games number 919 of them, because each of the eighteen type Z-Moves has a physical and a special id; " +
             $"here each is one move, which with the {n.GMaxMoves} G-Max Moves makes {moves.Count}.");
        Line();

        Line("## By generation");
        Line();
        Line("| Generation | Species | Moves | Moves fully run | Moves partly run | Moves that do nothing yet | Abilities | Abilities with an effect |");
        Line("| --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (int gen in species.Select(s => s.Generation).Concat(standard.Select(m => m.Generation)).Distinct().Order())
        {
            var genMoves = standard.Where(m => m.Generation == gen).ToList();
            var genAbilities = abilities.Where(a => a.Generation == gen).ToList();
            Line($"| {gen} | {species.Count(s => s.Generation == gen)} | {genMoves.Count} | {genMoves.Count(m => m.Support == MoveEffectSupport.Full)} | " +
                 $"{genMoves.Count(m => m.Support == MoveEffectSupport.Partial)} | {genMoves.Count(m => m.Support == MoveEffectSupport.None)} | " +
                 $"{genAbilities.Count} | {genAbilities.Count(a => implementedAbilities.Contains(a.Name))} |");
        }
        Line($"| All | {species.Count} | {standard.Count} | {standard.Count(m => m.Support == MoveEffectSupport.Full)} | " +
             $"{standard.Count(m => m.Support == MoveEffectSupport.Partial)} | {standard.Count(m => m.Support == MoveEffectSupport.None)} | " +
             $"{abilities.Count} | {abilities.Count(a => implementedAbilities.Contains(a.Name))} |");
        Line();

        Line("## Move effects still to write");
        Line();
        Line("*Partly run*: the move hits (or its stat change, status or healing happens) but its own effect is missing. " +
             "*Does nothing yet*: using it says \"But nothing happened!\", and trainers only pick it when they have nothing else.");
        Line();
        Line("| Effect | Support | Moves |");
        Line("| --- | --- | --- |");
        foreach (var group in standard.Where(m => m.Effect != null).GroupBy(m => (m.Effect, m.Support))
                     .OrderByDescending(g => g.Count()).ThenBy(g => g.Key.Effect))
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
        Line("What using an item does runs for the effect types the bag and battle know (`HealHP`, `HealStatus`, `Revive`, `FullRestore`, `LevelUp`, " +
             "`CatchPokemon`) and for the items species evolve with; in battle only HP is restored so far. Balls with a condition (Net, Dusk, Timer and " +
             "the like) catch like a Poké Ball for now.");
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
             $"{items.Where(i => i.MegaStone != null).Select(i => i.MegaStone!.Species).Distinct().Count()} species. The forms themselves (stats, types, abilities) come with plan 03 · D11.");
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
                _ => group.Key == EvolutionMethod.LevelShedinja ? "Nincada evolving" : "nothing yet"
            };
            Line($"| {group.Key} | {group.Count()} | {trigger} | {WaitsFor(group.Key)} |");
        }
        return sb.ToString();
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
        EvolutionMethod.Other => "a rule of its own",
        _ => ""
    };
}
