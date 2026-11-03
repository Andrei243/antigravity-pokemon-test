using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Link;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumTests.Golden;
using BattleText = PokemonPlatinumTests.Golden.BattleText;

namespace PokemonPlatinumTests.Link;

/// <summary>Plan 28 · I2: what each side may see of a battle, held over every golden battle from both sides.</summary>
public class LinkViewTests
{
    private static readonly BattleSide[] Sides = { BattleSide.Player, BattleSide.Enemy };

    [Fact]
    public void EachSidesViewOfEveryGoldenBattleKeepsTheFoesHiddenThings()
    {
        var moveNames = MoveDatabase.GetAll().Select(m => m.DisplayName).ToHashSet(StringComparer.Ordinal);
        var speciesNames = PokemonDatabase.Everyone().Select(s => s.DisplayName).ToHashSet(StringComparer.Ordinal);
        var faults = new List<string>();
        for (int seed = 1; seed <= BattleCorpus.Battles; seed++)
        {
            var corpus = new BattleCorpus(seed);
            corpus.Core.Start();
            foreach (var side in Sides)
            {
                var view = SideView.Of(corpus.Core, side);
                var shown = view.Filter(corpus.Core.Log);
                string where = $"battle {seed}, {side}'s view";

                // What only the Pokémon's own trainer is told, and no foe but as its stand-in
                foreach (var e in LinkBench.Flatten(shown))
                {
                    switch (e)
                    {
                        case ExpGained or LevelRose or MoveWanted:
                            var p = e switch { ExpGained x => x.Pokemon, LevelRose x => x.Pokemon, _ => ((MoveWanted)e).Pokemon };
                            if (!view.IsOwn(p)) faults.Add($"{where}: {e.GetType().Name} of a foe");
                            break;
                        case Entered entered when entered.Place.Side != side && !view.FoeStandIns.Contains(entered.Pokemon):
                            faults.Add($"{where}: a foe sent out as itself, not its stand-in");
                            break;
                        case Struck struck when struck.Place.Side != side && struck.Hp is < 0 or > SideView.BarPixels:
                            faults.Add($"{where}: a foe's HP {struck.Hp}, finer than the bar");
                            break;
                        case HpChanged changed when changed.Place.Side != side && changed.Hp is < 0 or > SideView.BarPixels:
                            faults.Add($"{where}: a foe's HP {changed.Hp}, finer than the bar");
                            break;
                        case Dynamaxed big when big.Place.Side != side && big.MaxHp != SideView.BarPixels:
                            faults.Add($"{where}: a foe's most HP");
                            break;
                    }
                }

                // An item or ability of a foe named in a line only with, or after, the event that shows it
                // The teams as they went in (a berry eaten is gone from the battle's own by its end)
                var before = new BattleCorpus(seed).Core;
                var foes = side == BattleSide.Player
                    ? before.Trainers.SelectMany(t => t.Party.Members).ToList()
                    : before.PlayerParty.Members.ToList();
                var mine = side == BattleSide.Player ? before.PlayerParty.Members.ToList() : before.Trainers.SelectMany(t => t.Party.Members).ToList();
                var known = mine.SelectMany(p => new[] { p.HeldItem?.DisplayName, p.AbilityName == null ? null : LocalNames.Ability(p.AbilityName) })
                    .Concat(mine.SelectMany(p => p.Moves.Select(m => m.DisplayName)))
                    .Where(n => !string.IsNullOrEmpty(n)).ToHashSet(StringComparer.Ordinal)!;
                var hidden = foes.SelectMany(p => new[] { p.HeldItem?.DisplayName, p.AbilityName == null ? null : LocalNames.Ability(p.AbilityName) })
                    .Where(n => !string.IsNullOrEmpty(n) && !known.Contains(n!) && !moveNames.Contains(n!) && !speciesNames.Contains(n!))
                    .Select(n => n!).ToHashSet(StringComparer.Ordinal);
                var longer = moveNames.Concat(speciesNames).Concat(ItemDatabase.GetAll().Select(i => i.DisplayName))
                    .Where(n => hidden.Any(h => n.Length > h.Length && n.Contains(h))).OrderByDescending(n => n.Length).ToList();
                var revealed = new HashSet<string>(StringComparer.Ordinal);
                foreach (var said in shown.OfType<Said>())
                {
                    foreach (var inner in said.Shows.Concat(said.OnImpact))
                    {
                        if (inner is ItemShown item) revealed.Add(ItemDatabase.Get(item.Item)?.DisplayName ?? item.Item);
                        if (inner is AbilityShown ability) revealed.Add(LocalNames.Ability(ability.Ability));
                    }
                    // A name inside a longer name the line says (Blaze in Blaze Kick) is that name, not this one
                    string text = longer.Aggregate(said.Text, (s, n) => s.Replace(n, " "));
                    foreach (var name in hidden.Where(n => !revealed.Contains(n) && Regex.IsMatch(text, $@"(?<!\p{{L}}){Regex.Escape(n)}(?!\p{{L}})")))
                        faults.Add($"{where}: \"{said.Text}\" names {name} before anything shows it");
                }

                // And the view crosses the wire as it is
                string json = BattleWire.WriteEvents(shown, view.Roster);
                Assert.Equal(json, BattleWire.WriteEvents(BattleWire.ReadEvents(json, view.Roster), view.Roster));
            }
        }
        Assert.True(faults.Count == 0, string.Join("\n", faults.Distinct().Take(40)));
    }

    [Fact]
    public void BothSidesViewsEndWithTheSameFaintedAndTheSameWinnerAsTheRules()
    {
        for (int seed = 1; seed <= BattleCorpus.Battles; seed += 3)
        {
            var corpus = new BattleCorpus(seed);
            corpus.Core.Start();
            // The rules' own: the whole log, every Pokémon as itself
            var whole = new ViewState();
            whole.Apply(corpus.Core.Log, BattleSide.Player);

            foreach (var side in Sides)
            {
                var view = SideView.Of(corpus.Core, side);
                var state = new ViewState();
                state.Apply(view.Filter(corpus.Core.Log), side);
                Assert.Equal(corpus.Core.Result, state.Result);

                // Each fainted Pokémon of the rules is fainted in the view, its own as itself and a foe as its stand-in
                var foes = side == BattleSide.Player ? corpus.Core.Trainers.SelectMany(t => t.Party.Members).ToList() : corpus.Core.PlayerParty.Members.ToList();
                var (_, placeOf) = TeamPreview.Of(foes);
                var expected = whole.Fainted.Select(p => view.IsOwn(p) ? p : view.FoeStandIns[placeOf[foes.FindIndex(f => ReferenceEquals(f, p))]]).ToHashSet(ReferenceEqualityComparer.Instance);
                Assert.True(expected.SetEquals(state.Fainted), $"battle {seed}, {side}'s view: {state.Fainted.Count} fainted where the rules have {whole.Fainted.Count}");

                // A foe's HP as the bar, its own exact
                foreach (var (p, hp) in whole.Hp.Where(kv => whole.Standing.ContainsValue(kv.Key)))
                {
                    if (view.IsOwn(p)) Assert.Equal(hp, state.Hp[p]);
                }
            }
        }
    }

    [Fact]
    public void ThePreviewSaysNothingOfTheOrderATeamWasBroughtIn()
    {
        var team = LinkBench.Team(42, LinkRoom.Default);
        var copies = LinkSetup.Copies(LinkRoom.Default, team);
        var forwards = TeamPreview.Of(copies).Preview;
        var backwards = TeamPreview.Of(copies.AsEnumerable().Reverse().ToList()).Preview;
        Assert.Equal(forwards, backwards);
        Assert.Equal(forwards.OrderBy(e => e.Dex).Select(e => e.Dex), forwards.Select(e => e.Dex));
        Assert.All(forwards, e => Assert.Equal(50, e.Level));

        // A stand-in is its preview and nothing more
        var standIn = TeamPreview.StandIn(forwards[0]);
        Assert.Empty(standIn.Moves);
        Assert.Null(standIn.HeldItem);
        Assert.Null(standIn.AbilityName);
        Assert.Equal(forwards[0], TeamPreview.EntryOf(standIn));
    }

    [Fact]
    public void AFoesBarIsPlatinums()
    {
        Assert.Equal(48, SideView.Bar(200, 200));
        Assert.Equal(24, SideView.Bar(100, 200));
        Assert.Equal(1, SideView.Bar(1, 200));
        Assert.Equal(0, SideView.Bar(0, 200));
        Assert.Equal(47, SideView.Bar(199, 200));
    }
}
