using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumTests;

/// <summary>
/// The sign-off's fuzz (plan 06 Â· R30): random battles under the modern rules between random teams of every species
/// the game has, its own included, at any level to 250, holding anything that can be held, their own Mega Stone or a
/// Z-Crystal, some with the Gigantamax Factor, in single, double and triple battles, and both trainers carrying the
/// Key Stone, the Z-Ring, the Dynamax Band, the Mega Max Band and the Tera Orb, so the four later mechanics meet one
/// another and everything else. Both sides are chosen for by the trainer AI. Every battle must end, leave nothing out
/// of range, end with the side the result names fainted, and play the same again from the same seed. Forty battles in
/// every run; <c>BATTLE_FUZZ=n</c> plays n (the sign-off played 3,000: docs/mechanics/signoff.md), and
/// <c>BATTLE_FUZZ_FROM=n</c> starts at another seed.
/// </summary>
public class BattleFuzzTests(ITestOutputHelper output)
{
    private static readonly List<PokemonSpecies> Species =
        PokemonDatabase.Everyone().Where(s => s.DexNumber >= 1).OrderBy(s => s.DexNumber).Select(s => s.Under(Ruleset.Modern)).ToList();

    private static readonly Dictionary<string, MoveData> ModernMoves =
        MoveDatabase.GetAll().ToDictionary(m => m.Name, m => m.Under(Ruleset.Modern), StringComparer.OrdinalIgnoreCase);

    private static readonly List<ItemData> Holdable =
        ItemDatabase.GetAll().Where(HeldItemEffects.IsHoldable).OrderBy(i => i.Id).ToList();

    private static readonly List<ItemData> Crystals =
        ItemDatabase.GetAll().Where(i => i.ZCrystal != null).OrderBy(i => i.Id).ToList();

    private static readonly Dictionary<string, List<ItemData>> StonesBySpecies = ItemDatabase.GetAll()
        .Where(i => i.MegaStone != null).GroupBy(i => i.MegaStone!.Species, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(g => g.Key, g => g.OrderBy(i => i.Id).ToList(), StringComparer.OrdinalIgnoreCase);

    private static int Count(string name, int otherwise) =>
        int.TryParse(Environment.GetEnvironmentVariable(name), out int n) && n >= 0 ? n : otherwise;

    [Fact]
    public void RandomBattlesWithTheFourMechanicsAlwaysEndInRangeAndPlayTheSameTwice()
    {
        int from = Count("BATTLE_FUZZ_FROM", 1), battles = Count("BATTLE_FUZZ", 40);
        var failures = new List<string>();
        var counts = new Dictionary<string, int>();
        var seen = counts.Keys;
        void Note(BattleEvent e) => counts[e.GetType().Name] = counts.GetValueOrDefault(e.GetType().Name) + 1;
        for (int seed = from; seed < from + battles; seed++)
        {
            try
            {
                var (core, mine, foe) = Make(seed);
                core.Start();
                Check(seed, core, mine, foe, failures);
                foreach (var e in core.Log)
                {
                    Note(e);
                    if (e is Said said) foreach (var attached in said.Shows.Concat(said.OnImpact)) Note(attached);
                }

                // The same seed again: the same log, line for line
                var (again, _, _) = Make(seed);
                again.Start();
                var first = core.Log.OfType<Said>().Select(s => s.Text).ToList();
                var second = again.Log.OfType<Said>().Select(s => s.Text).ToList();
                if (!first.SequenceEqual(second)) failures.Add($"battle {seed}: played otherwise the second time");
            }
            catch (Exception e)
            {
                failures.Add($"battle {seed}: {e.GetType().Name}: {e.Message}");
            }
        }
        output.WriteLine($"{battles} battles from seed {from}: " + string.Join(", ",
            new[] { nameof(MegaEvolved), nameof(UltraBursted), nameof(ZPowerUsed), nameof(Dynamaxed), nameof(MaxMoveUsed), nameof(Terastallized), nameof(Ended) }
                .Select(k => $"{k} {counts.GetValueOrDefault(k)}")));
        Assert.True(failures.Count == 0, string.Join("\n", failures.Take(20)));
        // Forty battles are enough for each of the four mechanics to have happened somewhere
        if (battles >= 40)
            foreach (string mechanic in new[] { nameof(MegaEvolved), nameof(ZPowerUsed), nameof(Dynamaxed), nameof(Terastallized) })
                Assert.True(seen.Contains(mechanic), $"no {mechanic} in {battles} battles");
    }

    private static void Check(int seed, BattleCore core, Party mine, Trainer foe, List<string> failures)
    {
        void Fail(string what) => failures.Add($"battle {seed}: {what}");
        if (core.Request != null) Fail($"waits for {core.Request.GetType().Name}");
        if (core.Result is not (BattleResult.PlayerVictory or BattleResult.PlayerDefeat)) Fail($"ended as {core.Result}");
        if (core.Turn >= 1000) Fail($"took {core.Turn} turns");
        if (core.Log.LastOrDefault() is not Ended) Fail("the log's last event isn't its end");
        foreach (var p in mine.Members.Concat(foe.Party.Members))
        {
            if (p.CurrentHP < 0 || p.CurrentHP > p.MaxHP) Fail($"{p.Species.Name} has {p.CurrentHP} of {p.MaxHP} HP");
            if (p.Moves.Any(m => m.CurrentPP < 0 || m.CurrentPP > m.MaxPP)) Fail($"{p.Species.Name}'s PP out of range");
            if (p.StatStages.Values.Any(s => s is < -6 or > 6)) Fail($"{p.Species.Name}'s stages out of range");
        }
        bool mineDown = mine.Members.All(p => p.IsFainted), theirsDown = foe.Party.Members.All(p => p.IsFainted);
        if (core.Result == BattleResult.PlayerDefeat ? !mineDown : !theirsDown) Fail($"{core.Result} with the loser still standing");
    }

    /// <summary>The battle of a seed, drawn from a generator of the seed alone, and its two teams.</summary>
    private static (BattleCore Core, Party Mine, Trainer Foe) Make(int seed)
    {
        var dice = new BattleRandom((uint)seed * 2654435761u);
        var format = (seed % 3) switch { 0 => BattleFormat.Double, 1 => BattleFormat.Single, _ => seed % 6 == 2 ? BattleFormat.Triple : BattleFormat.Single };
        int size = format == BattleFormat.Triple ? 6 : format == BattleFormat.Double ? 4 : 3;
        var mine = Team(dice, size);
        var foe = new Trainer
        {
            Name = "Tester", TrainerClass = "Ace Trainer", Party = Team(dice, size),
            KeyStone = true, ZRing = true, DynamaxBand = true, MegaMaxBand = true, TeraOrb = true
        };
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = mine, Trainers = new List<Trainer> { foe }, Random = new BattleRandom((uint)seed), Rules = Ruleset.Modern,
            Format = format, PlayerController = TrainerAi.Instance, PlayerName = "Lucas",
            KeyStone = true, ZRing = true, DynamaxBand = true, MegaMaxBand = true, TeraOrb = true
        });
        return (core, mine, foe);
    }

    private static Party Team(BattleRandom dice, int size)
    {
        var party = new Party();
        for (int i = 0; i < size; i++)
        {
            var p = new Pokemon(Species[dice.Next(Species.Count)], dice.Next(5, 251), dice);
            switch (dice.Next(6))
            {
                case 0 or 1:
                    p.HeldItem = Holdable[dice.Next(Holdable.Count)];
                    break;
                case 2 when StonesBySpecies.TryGetValue(p.Species.Name, out var stones):
                    p.HeldItem = stones[dice.Next(stones.Count)];
                    break;
                case 3:
                    p.HeldItem = Crystals[dice.Next(Crystals.Count)];
                    break;
            }
            if (dice.Next(4) == 0) p.GigantamaxFactor = true;
            for (int m = 0; m < p.Moves.Count; m++)
                if (ModernMoves.TryGetValue(p.Moves[m].Name, out var modern)) p.Moves[m] = new Move(modern);
            party.Add(p);
        }
        return party;
    }
}
