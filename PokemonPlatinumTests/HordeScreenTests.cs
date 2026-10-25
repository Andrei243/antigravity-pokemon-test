using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 14 · B4: hordes on the screen and in the grass, with no window (<c>docs/mechanics/rulings.md</c>, "Hordes";
/// style guide, "Stage", "Places on a platform", and "Battle panels", "The five and the one", "A horde's boxes"). The
/// places' layout and their sizes, the face of a horde (its five views, what lasts on each, their bars), the target
/// menu's walk over five cards, a spread move's cues and the camera over them, the ball refused on the screen, and
/// the grass's rule: where hordes live, from when, and what Sweet Scent draws out there.
/// </summary>
public class HordeScreenTests
{
    private static Pokemon[] Five(string species, int level) => Enumerable.Range(0, 5).Select(_ => Mon(species, level)).ToArray();

    /// <summary>A horde on the screen, played up to the first choice of a move, under the modern rules that have hordes.</summary>
    private static BattleEngine HordeBattle(Pokemon mine, Pokemon[] theirs, Inventory? bag = null)
    {
        var party = new Party();
        party.Add(mine);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = bag ?? new Inventory(), Pokedex = new Pokedex(),
            WildPokemon = theirs.ToList(), Format = BattleFormat.Horde, Random = Steady(), Rules = Ruleset.Modern
        });
        Settle(battle);
        return battle;
    }

    // ------------------------------------------------------------------ the layout

    [Fact]
    public void ThePlacesStandOnAnArcAndShrinkByTheirCount()
    {
        // Alone and two to a platform the places are where they always were
        Assert.Equal(BattleStage.EnemySpot, BattlePlaces.Spot(BattleSide.Enemy, 0, 1));
        Assert.Equal(BattleStage.PlayerSpot, BattlePlaces.Spot(BattleSide.Player, 0, 1));
        Assert.Equal(BattleStage.EnemySpot + Vector3.UnitX * BattleStage.EnemyPlatformRadius * 0.5f, BattlePlaces.Spot(BattleSide.Enemy, 0, 2));
        Assert.Equal(BattleStage.EnemySpot - Vector3.UnitX * BattleStage.EnemyPlatformRadius * 0.5f, BattlePlaces.Spot(BattleSide.Enemy, 1, 2));
        Assert.Equal(BattleStage.PlayerSpot - Vector3.UnitX * BattleStage.PlayerPlatformRadius * 0.5f, BattlePlaces.Spot(BattleSide.Player, 0, 2));
        Assert.Equal(1f, BattlePlaces.Fit(1));
        Assert.Equal(0.78f, BattlePlaces.Fit(2));
        Assert.Equal(0.6f, BattlePlaces.Fit(3));
        Assert.Equal(0.45f, BattlePlaces.Fit(5));
        for (int n = 2; n <= BattleAnimator.MostSlots; n++) Assert.True(BattlePlaces.Fit(n) < BattlePlaces.Fit(n - 1));

        // Five: from the first's end (right, as the foe's first of two stands) across to the other, on the platform,
        // the middle furthest from the player's side and the ends level, the arc the same on both halves
        float r = BattleStage.EnemyPlatformRadius;
        var five = Enumerable.Range(0, 5).Select(s => BattlePlaces.Spot(BattleSide.Enemy, s, 5)).ToArray();
        for (int s = 1; s < 5; s++) Assert.True(five[s].X < five[s - 1].X - 0.9f, $"place {s} stands left of {s - 1} with room between");
        Assert.All(five, p => Assert.True(Vector2.Distance(new(p.X, p.Z), new(BattleStage.EnemySpot.X, BattleStage.EnemySpot.Z)) <= r, $"{p} is off the platform"));
        Assert.Equal(five.Min(p => p.Z), five[2].Z);
        Assert.True(five[2].Z < -r * 0.4f);
        Assert.Equal(0f, five[0].Z, 4);
        Assert.Equal(0f, five[4].Z, 4);
        Assert.Equal(five[0].X, -five[4].X, 4);
        Assert.Equal(five[1].Z, five[3].Z, 4);
        // Three stand the same way, further apart
        var three = Enumerable.Range(0, 3).Select(s => BattlePlaces.Spot(BattleSide.Enemy, s, 3)).ToArray();
        Assert.True(three[0].X > three[1].X && three[1].X > three[2].X);
        Assert.True(three[1].Z < three[0].Z);
    }

    [Fact]
    public void EveryLookKeptForAPlaceHasAHordesFive()
    {
        // Nothing is folded onto a double's second place any more: each of the five has its own
        var anim = new BattleAnimator();
        var views = Enumerable.Range(0, BattleAnimator.MostSlots).Select(s => anim[BattleSide.Enemy, s]).ToList();
        Assert.Equal(views.Count, views.Distinct().Count());
        var conditions = Enumerable.Range(0, BattleAnimator.MostSlots).Select(s => anim.Conditions.Of(BattleSide.Enemy, s)).ToList();
        Assert.Equal(conditions.Count, conditions.Distinct().Count());
        Assert.All(Enumerable.Range(0, BattleAnimator.MostSlots), s => Assert.Same(anim.Conditions.Of(BattleSide.Enemy, s), anim[BattleSide.Enemy, s].Condition));
        var waits = Enumerable.Range(0, BattleAnimator.MostSlots).Select(s => anim.FieldLook.Of(new Place(BattleSide.Enemy, s), PlaceCondition.FutureSight)).ToList();
        Assert.Equal(waits.Count, waits.Distinct().Count());

        var places = new FxPlaces();
        places.Set(BattleSide.Enemy, 4, new Vector3(-2f, 0.16f, 0f), 1f);
        places.Set(BattleSide.Enemy, 1, new Vector3(1f, 0.16f, -1f), 1.5f);
        Assert.Equal(new Vector3(-2f, 0.16f, 0f), places.Feet(BattleSide.Enemy, 4));
        Assert.Equal(1.5f, places.Height(BattleSide.Enemy, 1));
    }

    // ------------------------------------------------------------------ the face of a horde

    [Fact]
    public void FiveComeOutAndEachHasItsOwnView()
    {
        var battle = HordeBattle(Mon("Golem", 50, "Rock Slide"), Five("Zubat", 40));
        Assert.True(battle.IsHorde);
        Assert.False(battle.IsDouble);
        Assert.Equal(1, battle.Anim.SlotsOf(BattleSide.Player));
        Assert.Equal(5, battle.Anim.SlotsOf(BattleSide.Enemy));
        Assert.Equal(1, battle.Anim.Slots);
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);
        for (int slot = 0; slot < 5; slot++)
        {
            var view = battle.Anim[BattleSide.Enemy, slot];
            Assert.True(view.Present, $"the foe in place {slot} is out");
            Assert.Same(battle.EnemySlots[slot].Pokemon, view.Shown);
            Assert.Equal("Zubat", view.Shown!.Species.Name);
            // No ball: a wild horde is there as the battle opens, and each one's box slides in after the camera's sweep
            Assert.True(view.SendOutAge < 0f);
            Assert.True(BattleHUD.BoxSlide(battle.Anim, view) >= 0f);
        }
        // The bars follow each of the five
        battle.EnemySlots[3].Pokemon!.CurrentHP = 5;
        for (int i = 0; i < 240; i++) battle.Update(1f / 60f);
        Assert.Equal(5f, battle.Anim[BattleSide.Enemy, 3].DisplayedHp);
        Assert.Equal(battle.EnemySlots[4].Pokemon!.MaxHP, battle.Anim[BattleSide.Enemy, 4].DisplayedHp);
    }

    [Fact]
    public void TheTargetCursorWalksTheFiveCardsAndPassesTheFallen()
    {
        var battle = HordeBattle(Mon("Golem", 50, "Rock Throw"), Five("Zubat", 40));
        Battler[] foe = battle.EnemySlots.ToArray();
        var golem = battle.PlayerSlots[0];

        // The five foes in a row as the stage stands them (the first on the right), the player's own under the middle
        Assert.Equal(5, battle.TargetColumns);
        Assert.Equal(new Battler?[] { foe[4], foe[3], foe[2], foe[1], foe[0], null, null, golem, null, null }, battle.TargetLayout.ToArray());
        Battler Aimed() => battle.TargetChoices[battle.HUD.TargetMenuIndex];
        void Step(int dx, int dy, Battler expected)
        {
            battle.MoveCursor(dx, dy);
            Assert.Same(expected, Aimed());
        }

        battle.SelectMove(0);
        Assert.Equal(BattleMenuState.SelectTarget, battle.HUD.MenuState);
        Assert.Equal(5, battle.TargetChoices.Count);
        Assert.Same(foe[4], Aimed());
        Step(1, 0, foe[3]);
        Step(1, 0, foe[2]);
        Step(1, 0, foe[1]);
        Step(1, 0, foe[0]);
        Step(1, 0, foe[4]);    // round the row
        Step(-1, 0, foe[0]);
        Step(0, 1, foe[0]);    // under the foes is only the Golem itself: the cursor stays
        Step(0, -1, foe[0]);

        // Aimed at the middle one, the Rock Throw lands there
        battle.MoveCursor(-1, 0);
        battle.MoveCursor(-1, 0);
        Assert.Same(foe[2], Aimed());
        int hp = foe[2].Pokemon!.CurrentHP;
        battle.SelectTarget(battle.HUD.TargetMenuIndex);
        Settle(battle);
        Assert.True(foe[2].Pokemon!.CurrentHP < hp);
        Assert.All(new[] { 0, 1, 3, 4 }, s => Assert.Equal(foe[s].Pokemon!.MaxHP, foe[s].Pokemon!.CurrentHP));

        // A fallen foe's card is passed over
        var second = HordeBattle(Mon("Golem", 50, "Rock Throw"), Five("Zubat", 40));
        second.EnemySlots[2].Pokemon!.CurrentHP = 0;
        second.SelectMove(0);
        Assert.Equal(4, second.TargetChoices.Count);
        Battler Second() => second.TargetChoices[second.HUD.TargetMenuIndex];
        second.MoveCursor(1, 0);
        Assert.Same(second.EnemySlots[3], Second());
        second.MoveCursor(1, 0);
        Assert.Same(second.EnemySlots[1], Second());
    }

    [Fact]
    public void ADoublesTargetMenuIsLaidOutAsItWas()
    {
        // The grid's rule over two columns is the double's own: TestTheTargetCursorMovesOverTheCardsAsTheyLie holds the
        // walk; this holds the cards' rectangles
        Assert.Equal(new Raylib_cs.Rectangle(48, 836, 536, 96), PokemonPlatinumEngine.UI.ModernUi.TargetCard(0, 2));
        Assert.Equal(new Raylib_cs.Rectangle(604, 940, 536, 96), PokemonPlatinumEngine.UI.ModernUi.TargetCard(3, 2));
        // A horde's five fill the same width, side by side, and the player's card stands under the middle one
        var cards = Enumerable.Range(0, 10).Select(i => PokemonPlatinumEngine.UI.ModernUi.TargetCard(i, 5)).ToArray();
        Assert.Equal(48f, cards[0].X);
        Assert.Equal(1140f, cards[4].X + cards[4].Width, 1);
        for (int i = 1; i < 5; i++) Assert.True(cards[i].X >= cards[i - 1].X + cards[i - 1].Width);
        Assert.Equal(cards[2].X, cards[7].X);
        Assert.True(cards[7].Y > cards[2].Y + cards[2].Height);
        // And the five small boxes stack down the left, clear of one another and of the message box
        for (int s = 1; s < 5; s++) Assert.True(PokemonPlatinumEngine.UI.ModernUi.HordeBoxAt(s).Y >= PokemonPlatinumEngine.UI.ModernUi.HordeBoxAt(s - 1).Y + PokemonPlatinumEngine.UI.ModernUi.HordeBoxHeight);
        Assert.True(PokemonPlatinumEngine.UI.ModernUi.HordeBoxAt(4).Y + PokemonPlatinumEngine.UI.ModernUi.HordeBoxHeight < 836);
    }

    [Fact]
    public void ASpreadMoveCuesAllFiveAndTheCameraFramesThem()
    {
        var battle = HordeBattle(Mon("Golem", 50, "Rock Slide"), Five("Zubat", 40));
        var before = battle.EnemySlots.Select(b => b.Pokemon!.CurrentHP).ToArray();
        battle.SelectMove(0);
        var struck = battle.Anim.Cues.Where(c => c.Kind == CueKind.Move && c.Move == "Rock Slide").Select(c => (c.ToSide, c.ToSlot)).Distinct().ToList();
        Assert.Equal(5, struck.Count);
        Assert.All(struck, t => Assert.Equal(BattleSide.Enemy, t.ToSide));

        // Past the attacker's shot the camera frames whoever the rocks fall on, all five
        for (int i = 0; i < 18; i++) battle.Update(1f / 60f);
        var places = new FxPlaces();
        for (int s = 0; s < 5; s++) places.Set(BattleSide.Enemy, s, BattlePlaces.Spot(BattleSide.Enemy, s, 5), 1f);
        var (shot, kind, _) = BattleCamera.Direct(battle.Anim, places);
        Assert.Equal(ShotKind.Target, kind);
        var single = BattleCamera.Close(places, BattleSide.Enemy, 0, 3.0f);
        Assert.True(Vector3.Distance(shot.Position, shot.Target) > Vector3.Distance(single.Position, single.Target) || shot.Fov > single.Fov,
            "the shot of five stands further back, or looks wider, than the shot of one");

        Settle(battle);
        for (int s = 0; s < 5; s++) Assert.True(battle.EnemySlots[s].Pokemon!.CurrentHP < before[s], $"the Zubat in place {s} was hit");
    }

    [Fact]
    public void TheBallIsRefusedOnTheScreenWhileMoreThanTwoStand()
    {
        var bag = new Inventory();
        var ball = ItemDatabase.Get("Poké Ball")!;
        bag.AddItem(ball, 3);
        var battle = HordeBattle(Mon("Golem", 50, "Rock Slide"), Five("Zubat", 40), bag);
        battle.UseBagItem("Poké Ball");
        Assert.Equal(Text.Of("battle.thereAreTooManyPokemon"), battle.CurrentMessage);
        Assert.Equal(3, bag.GetQuantity(ball));
        Settle(battle);
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);
    }

    // ------------------------------------------------------------------ the grass

    [Fact]
    public void HordesComeOnlyAfterTheRegionsHallOfFameUnderRulesThatHaveThem()
    {
        var sinnoh = RegionDatabase.Get(RegionDatabase.Sinnoh)!;
        var story = new StoryState();
        Assert.Null(Hordes.Open(sinnoh, story, Ruleset.Modern));
        story.Set(sinnoh.StoryCompleteFlag);
        var open = Hordes.Open(sinnoh, story, Ruleset.Modern);
        Assert.NotNull(open);
        // Platinum's rules have no hordes, and another region's Hall of Fame opens none of Sinnoh's
        Assert.Null(Hordes.Open(sinnoh, story, Ruleset.Platinum));
        Assert.Null(Hordes.Open(RegionDatabase.Get(RegionDatabase.Kanto), story, Ruleset.Modern));

        var rows = open!.At("route_225")!;
        Assert.Equal(new[] { "Machop", "Geodude" }, rows.Select(r => r.SpeciesName));
        Assert.All(rows, r => Assert.Equal(Hordes.Size, r.Count));
        Assert.Null(open.At("twinleaf_town"));
        // Met from the table, a horde is five, each at a level of the row's own range
        var met = WildEncounterRules.Meet(rows, false, null, new Random(3), keptAway: false)!;
        Assert.Equal(5, met.Count);
        Assert.Equal(4, met.OthersLevels!.Length);
        Assert.All(met.OthersLevels.Append(met.MinLevel), l => Assert.InRange(l, 43, 45));
    }

    [Fact]
    public void AStepMeetsAHordeOneTimeInTwenty()
    {
        var rng = new Random(11);
        int hordes = Enumerable.Range(0, 20000).Count(_ => Hordes.Comes(rng));
        Assert.InRange(hordes, 850, 1150);
    }

    [Fact]
    public void EveryHordeIsOfItsRegionsOwnPokemonAndItsPlacesGrass()
    {
        // A family is every species joined to it by an evolution, either way
        var species = PokemonDatabase.GetAll().ToList();
        var links = species.ToDictionary(s => s.Name, _ => new HashSet<string>());
        foreach (var s in species)
            foreach (var e in s.Evolutions ?? new())
                if (links.ContainsKey(e.TargetSpecies)) { links[s.Name].Add(e.TargetSpecies); links[e.TargetSpecies].Add(s.Name); }
        HashSet<string> Family(string name)
        {
            var seen = new HashSet<string> { name };
            var next = new Queue<string>(new[] { name });
            while (next.Count > 0) foreach (var o in links[next.Dequeue()]) if (seen.Add(o)) next.Enqueue(o);
            return seen;
        }

        Assert.NotEmpty(Hordes.All);
        foreach (var (regionId, places) in Hordes.All)
        {
            var region = RegionDatabase.Get(regionId);
            Assert.True(region is { File: not null }, $"{regionId} is a region with a file");
            Assert.NotEmpty(places);
            foreach (var place in places)
            {
                string path = GameDataFiles.PathOf($"world/{region!.Folder}/areas/{place.Area}.json");
                Assert.True(File.Exists(path), $"{regionId}'s {place.Area} is an area of its world");
                using var area = JsonDocument.Parse(File.ReadAllText(path));
                Assert.True(area.RootElement.TryGetProperty("land", out var land) && land.GetArrayLength() > 0, $"{place.Area} has grass with Pokémon in it");
                var grass = land.EnumerateArray().Select(e => (Species: e.GetProperty("species").GetString()!, Level: e.GetProperty("level").GetInt32())).ToList();
                int lowest = grass.Min(g => g.Level);
                Assert.NotEmpty(place.Hordes);
                foreach (var horde in place.Hordes)
                {
                    var kind = PokemonDatabase.Get(horde.Species);
                    Assert.True(kind != null, $"{horde.Species} is a species");
                    Assert.True(kind!.DexNumbers.ContainsKey(region.File!.Dex), $"{horde.Species} of {place.Area} is in {regionId}'s own Pokédex");
                    Assert.True(grass.Any(g => Family(horde.Species).Contains(g.Species)), $"{horde.Species}'s family lives in {place.Area}'s grass");
                    Assert.InRange(horde.MinLevel, 1, horde.MaxLevel);
                    Assert.True(horde.MaxLevel <= lowest, $"{place.Area}'s {horde.Species} hordes ({horde.MaxLevel}) are no stronger than its grass's weakest ({lowest})");
                }
            }
        }
        // The region's tables themselves still meet one at a time (HordeTests.EveryPlacesTablesMeetOnePokemonAtATime)
    }

    [Fact]
    public void SweetScentInAHordesGrassDrawsOneOut()
    {
        var map = MapDatabase.Get("Sinnoh");
        var b = map.AreaBounds("route_225")!.Value;
        var area = map.FindArea("route_225");
        (int X, int Y)? grass = null;
        for (int y = b.Y; y < b.Y + b.Height && grass == null; y++)
            for (int x = b.X; x < b.X + b.Width; x++)
                if (map.AreaAt(x, y) == area && map.IsTallGrass(x, y)) { grass = (x, y); break; }
        Assert.NotNull(grass);
        var (gx, gy) = grass!.Value;

        var story = new StoryState();
        story.Set(RegionDatabase.Get(RegionDatabase.Sinnoh)!.StoryCompleteFlag);
        var hordes = Hordes.Open(RegionDatabase.Get(RegionDatabase.Sinnoh), story, Ruleset.Modern);
        var drawn = map.DrawOutWild(gx, gy, moment: new EncounterMoment { Hordes = hordes })!;
        Assert.Equal(5, drawn.Count);
        Assert.Contains(drawn.SpeciesName, new[] { "Machop", "Geodude" });
        // Nothing of the kind before the Hall of Fame, beside a partner, or on the water
        Assert.Equal(1, map.DrawOutWild(gx, gy, moment: new EncounterMoment())!.Count);
        Assert.Equal(1, map.DrawOutWild(gx, gy, moment: new EncounterMoment { Hordes = hordes, Partner = true })!.Count);
    }
}
