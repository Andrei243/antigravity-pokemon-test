using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 14 · B6: triple battles (<c>docs/mechanics/rulings.md</c>, "Formats of the later games", "Triple battles"; style
/// guide, "Stage", "Places on a platform", and "Battle panels", "A triple's boxes", "The six cards", "SHIFT"). Three
/// places a side under the rules that have them (<see cref="Ruleset.Triples"/>), reach by position decided in one place
/// (<see cref="BattleCore.InReach(BattleFormat, Place, Place)"/>), a move that reaches anywhere
/// (<see cref="MoveFlags.Distance"/>), a shift to the centre that is the Pokémon's turn, the last two moved to the centre,
/// Ally Switch from end to end, a record and its replay, a hundred triples played to their end by the trainer AI, and
/// the face of a triple: its places on the stage, its boxes, the six target cards and the SHIFT command.
/// </summary>
public class TripleTests
{
    private static Pokemon[] Three(string species, int level, params string[] moves) =>
        Enumerable.Range(0, 3).Select(_ => Mon(species, level, moves)).ToArray();

    [Fact]
    public void ATripleIsThreeAgainstThree()
    {
        var core = Triples(Three("Golem", 40, "Tackle"), Three("Bidoof", 30, "Tackle"));
        Assert.Equal(BattleFormat.Triple, core.Format);
        Assert.True(core.IsTriple);
        Assert.False(core.IsSingle);
        Assert.False(core.IsDouble);
        Assert.Equal(3, core.PlayerSlots.Count);
        Assert.Equal(3, core.EnemySlots.Count);
        Assert.Equal(3, BattleFormat.Triple.PlacesOf(BattleSide.Player));
        Assert.Equal(3, BattleFormat.Triple.PlacesOf(BattleSide.Enemy));
        Assert.True(Foe3.Number < Place.MaxNumber);
        Assert.All(core.AllBattlers, b => Assert.True(b.IsActive));
        // Every place is asked for its choice
        Assert.Equal(new[] { Mine, Foe, Mine2, Foe2, Mine3, Foe3 }, Assert.IsType<ActionRequest>(core.Request).Places.OrderBy(p => p.Number));
    }

    [Fact]
    public void WhatCantBeATripleFallsBack()
    {
        // Platinum's rules have no triples: the double the sides can field
        var platinum = Triples(Three("Golem", 40, "Tackle"), Three("Bidoof", 30, "Tackle"), rules: Ruleset.Platinum);
        Assert.Equal(BattleFormat.Double, platinum.Format);
        Assert.Equal(2, platinum.PlayerSlots.Count);
        // Two of the player's able to fight make a double, one a single
        Assert.Equal(BattleFormat.Double, BattleFormats.FallBack(BattleFormat.Triple, 2, 0, false, 1, 3, 0, BattleKind.Normal, Ruleset.Modern, out _));
        Assert.Equal(BattleFormat.Single, BattleFormats.FallBack(BattleFormat.Triple, 1, 0, false, 1, 3, 0, BattleKind.Normal, Ruleset.Modern, out _));
        // So do a trainer with two, two trainers at once, a partner beside the player and the Great Marsh
        Assert.Equal(BattleFormat.Double, BattleFormats.FallBack(BattleFormat.Triple, 3, 0, false, 1, 2, 0, BattleKind.Normal, Ruleset.Modern, out _));
        Assert.Equal(BattleFormat.Double, BattleFormats.FallBack(BattleFormat.Triple, 3, 0, false, 2, 3, 0, BattleKind.Normal, Ruleset.Modern, out _));
        Assert.Equal(BattleFormat.Double, BattleFormats.FallBack(BattleFormat.Triple, 3, 3, true, 1, 3, 0, BattleKind.Normal, Ruleset.Modern, out bool stays));
        Assert.True(stays);
        Assert.Equal(BattleFormat.Single, BattleFormats.FallBack(BattleFormat.Triple, 3, 0, false, 0, 0, 3, BattleKind.Safari, Ruleset.Modern, out _));
        // Three wild Pokémon may be a triple, and what is asked for as anything else never is one
        Assert.Equal(BattleFormat.Triple, BattleFormats.FallBack(BattleFormat.Triple, 3, 0, false, 0, 0, 3, BattleKind.Normal, Ruleset.Modern, out _));
        Assert.Equal(BattleFormat.Double, BattleFormats.FallBack(BattleFormat.Double, 3, 0, false, 1, 3, 0, BattleKind.Normal, Ruleset.Modern, out _));
    }

    // ------------------------------------------------------------------ reach

    [Fact]
    public void ReachIsBesideAndAcross()
    {
        static bool R(int user, BattleSide side, int target) => BattleCore.InReach(BattleFormat.Triple, new Place(BattleSide.Player, user), new Place(side, target));
        // From the left end: the foe across (their third) and the centre, the ally beside; not the far foe, not the far ally
        Assert.True(R(0, BattleSide.Enemy, 2));
        Assert.True(R(0, BattleSide.Enemy, 1));
        Assert.False(R(0, BattleSide.Enemy, 0));
        Assert.True(R(0, BattleSide.Player, 1));
        Assert.False(R(0, BattleSide.Player, 2));
        // From the centre: everyone
        Assert.All(new[] { 0, 1, 2 }, s => Assert.True(R(1, BattleSide.Enemy, s)));
        Assert.All(new[] { 0, 2 }, s => Assert.True(R(1, BattleSide.Player, s)));
        // From the right end, the mirror of the left
        Assert.True(R(2, BattleSide.Enemy, 0));
        Assert.False(R(2, BattleSide.Enemy, 2));
        // The same both ways, and in every other format everything is in reach
        for (int a = 0; a < 3; a++)
            for (int b = 0; b < 3; b++)
                Assert.Equal(R(a, BattleSide.Enemy, b), BattleCore.InReach(BattleFormat.Triple, new Place(BattleSide.Enemy, b), new Place(BattleSide.Player, a)));
        foreach (var format in new[] { BattleFormat.Single, BattleFormat.Double, BattleFormat.Horde, BattleFormat.Sos })
            Assert.True(BattleCore.InReach(format, new Place(BattleSide.Player, 0), new Place(BattleSide.Enemy, 4)));
    }

    [Fact]
    public void AMoveFromAnEndCantReachTheFarFoeAndOneOfDistanceCan()
    {
        var core = Triples(new[] { Mon("Golem", 50, "Tackle", "Aura Sphere"), Mon("Golem", 50, "Tackle"), Mon("Golem", 50, "Tackle") },
            Three("Snorlax", 50));
        Assert.False(MoveDatabase.Get("Tackle").Flags.HasFlag(MoveFlags.Distance));
        Assert.True(MoveDatabase.Get("Aura Sphere").Flags.HasFlag(MoveFlags.Distance));
        // Tackle from the left end at the foe's first, which stands at the far end, is refused
        Assert.Equal(Text.Of("battle.isOutOfReach", ("pokemon", core.At(Foe).Pokemon!.DisplayName)), core.WhyNot(BattleChoice.Fight(Mine, 0, Foe)));
        Assert.Null(core.WhyNot(BattleChoice.Fight(Mine, 0, Foe3)));
        Assert.Null(core.WhyNot(BattleChoice.Fight(Mine, 0, Foe2)));
        // From the centre it is not
        Assert.Null(core.WhyNot(BattleChoice.Fight(Mine2, 0, Foe)));
        // Aura Sphere reaches anywhere: it lands on the far foe, and nobody else is touched
        Assert.Null(core.WhyNot(BattleChoice.Fight(Mine, 1, Foe)));
        TripleTurn(core, (Mine, 1, Foe), (Mine2, 0, Foe2), (Mine3, 0, Foe2), (Foe, 0, null), (Foe2, 0, null), (Foe3, 0, null));
        var far = core.At(Foe).Pokemon!;
        Assert.True(far.CurrentHP < far.MaxHP, "Aura Sphere reached the far end");
        Assert.Equal(core.At(Foe3).Pokemon!.MaxHP, core.At(Foe3).Pokemon!.CurrentHP);
    }

    [Fact]
    public void ASpreadMoveFromAnEndHitsOnlyThoseInReach()
    {
        // Earthquake from the left end: the foe across, the foe in the centre and the ally beside; not the far foe, not the far ally
        var quake = Triples(new[] { Mon("Golem", 50, "Earthquake"), Mon("Snorlax", 50), Mon("Snorlax", 50) }, Three("Snorlax", 50));
        TripleTurn(quake, (Mine, 0, null));
        bool Hurt(Place p) => quake.At(p).Pokemon!.CurrentHP < quake.At(p).Pokemon!.MaxHP;
        Assert.True(Hurt(Foe3));
        Assert.True(Hurt(Foe2));
        Assert.True(Hurt(Mine2));
        Assert.False(Hurt(Foe));
        Assert.False(Hurt(Mine3));

        // Rock Slide from the right end: the foe across (their first) and the centre
        var slide = Triples(new[] { Mon("Snorlax", 50), Mon("Snorlax", 50), Mon("Golem", 50, "Rock Slide") }, Three("Snorlax", 50));
        TripleTurn(slide, (Mine3, 0, null));
        Assert.True(slide.At(Foe).Pokemon!.CurrentHP < slide.At(Foe).Pokemon!.MaxHP);
        Assert.True(slide.At(Foe2).Pokemon!.CurrentHP < slide.At(Foe2).Pokemon!.MaxHP);
        Assert.Equal(slide.At(Foe3).Pokemon!.MaxHP, slide.At(Foe3).Pokemon!.CurrentHP);
    }

    // ------------------------------------------------------------------ the shift and the centre

    [Fact]
    public void AShiftIsThePokemonsTurnAndTradesPlacesWithTheCentre()
    {
        var left = Mon("Golem", 50, "Tackle");
        var centre = Mon("Bidoof", 50, "Tackle");
        var core = Triples(new[] { left, centre, Mon("Bidoof", 50) }, Three("Snorlax", 50));
        // Only an end may shift, and only in a triple battle
        Assert.Null(core.WhyNot(BattleChoice.Shift(Mine)));
        Assert.Null(core.WhyNot(BattleChoice.Shift(Mine3)));
        Assert.Equal(Text.Of("battle.cantShift", ("pokemon", core.At(Mine2).Name)), core.WhyNot(BattleChoice.Shift(Mine2)));
        var dbl = Doubles(new[] { Mon("Golem", 50, "Tackle"), Mon("Golem", 50, "Tackle") }, new[] { Mon("Bidoof", 5), Mon("Bidoof", 5) });
        Assert.NotNull(dbl.WhyNot(BattleChoice.Shift(Mine)));

        var said = TripleTurn(core, (Mine, ShiftMove, null), (Mine2, 0, Foe2), (Foe, 0, null), (Foe2, 0, null), (Foe3, 0, null));
        // The left end's Golem stands in the centre and the centre's Bidoof at the left end
        Assert.Same(left, core.At(Mine2).Pokemon);
        Assert.Same(centre, core.At(Mine).Pokemon);
        // It is said first of all, as a switch is, and the Golem uses no move this turn; the Bidoof still uses its own
        InOrder(said, Text.Of("battle.shiftedToTheCentre", ("pokemon", left.DisplayName)), Text.Of("battle.used", ("user", centre.DisplayName), ("move", "Tackle")));
        Assert.DoesNotContain(Text.Of("battle.used", ("user", left.DisplayName), ("move", "Tackle")), said);
        Assert.Contains(core.Log, e => e is Said s && s.Shows.OfType<PlacesSwapped>().Any(p => p.First == Mine && p.Second == Mine2));
        // The shift is kept in the record
        Assert.Contains(core.Record!.Answers.SelectMany(a => a), c => c.Kind == ChoiceKind.Shift && c.Who == Mine);
    }

    [Fact]
    public void TheLastTwoOutOfReachAreMovedToTheCentre()
    {
        // Each side's first stands at an end facing the other's third; each Golem's Rock Slide fells the two beside the
        // other's first, which leaves one a side, at the two ends that can't reach each other
        static Pokemon Weak(string species)
        {
            var p = Mon(species, 30);
            p.CurrentHP = 1;
            return p;
        }
        var mine = Mon("Golem", 60, "Rock Slide", "Tackle");
        var theirs = Mon("Golem", 60, "Rock Slide", "Tackle");
        var core = Triples(new[] { mine, Weak("Bidoof"), Weak("Bidoof") }, new[] { theirs, Weak("Bidoof"), Weak("Bidoof") });
        var said = TripleTurn(core, (Mine, 0, null), (Foe, 0, null));
        Assert.Equal(BattleResult.None, core.Result);
        Assert.Contains(Text.Of("battle.movedToTheCentre"), said);
        Assert.Same(mine, core.At(Mine2).Pokemon);
        Assert.Same(theirs, core.At(Foe2).Pokemon);
        Assert.True(core.InReach(core.At(Mine2), core.At(Foe2)));
        // And they reach each other from there
        Assert.Equal(new[] { Mine2, Foe2 }, Assert.IsType<ActionRequest>(core.Request).Places.OrderBy(p => p.Number));
        TripleTurn(core, (Mine2, 1, Foe2), (Foe2, 1, Mine2));
        Assert.True(theirs.CurrentHP < theirs.MaxHP);

    }

    [Fact]
    public void AllySwitchTradesTheTwoEnds()
    {
        var left = Mon("Kadabra", 50, "Ally Switch");
        var right = Mon("Bidoof", 50);
        var core = Triples(new[] { left, Mon("Bidoof", 50), right }, Three("Snorlax", 50));
        TripleTurn(core, (Mine, 0, null));
        Assert.Same(right, core.At(Mine).Pokemon);
        Assert.Same(left, core.At(Mine3).Pokemon);
        // From the centre it fails
        var middle = Mon("Kadabra", 50, "Ally Switch");
        var again = Triples(new[] { Mon("Bidoof", 50), middle, Mon("Bidoof", 50) }, Three("Snorlax", 50));
        TripleTurn(again, (Mine2, 0, null));
        Assert.Same(middle, again.At(Mine2).Pokemon);
    }

    // ------------------------------------------------------------------ a record, and the fuzz

    private static BattleCore Fresh(BattleFormat format)
    {
        var party = new Party();
        party.Add(Mon("Grotle", 30, "Razor Leaf", "Bite"));
        party.Add(Mon("Luxio", 30, "Spark", "Bite"));
        party.Add(Mon("Monferno", 30, "Ember", "Mach Punch"));
        party.Add(Mon("Bidoof", 28, "Tackle"));
        var trainer = new Trainer { Id = "ace", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 100, Format = "triple", Ai = PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.Basic };
        foreach (var p in new[] { Mon("Staravia", 28, "Wing Attack", "Quick Attack"), Mon("Kricketune", 28, "Fury Cutter"), Mon("Ponyta", 29, "Ember"), Mon("Shinx", 27, "Spark") })
            trainer.Party.Add(p);
        return new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = new List<Trainer> { trainer }, Format = format, Random = new BattleRandom(20261025), Rules = Ruleset.Modern,
            PlayerController = null, EnemyController = TrainerAi.Instance, PlayerName = "Lucas"
        });
    }

    [Fact]
    public void ARecordedTripleReplaysTheSameAndOnlyAsATriple()
    {
        var core = Fresh(BattleFormat.Triple);
        Assert.Equal(BattleFormat.Triple, core.Format);
        core.Start();
        for (int step = 0, turn = 0; step < 300 && core.Request != null; step++)
        {
            switch (core.Request)
            {
                case ActionRequest asked:
                    // Each of the player's Pokémon its moves in turn at a foe in its reach, and once a shift from an end
                    var answer = new List<BattleChoice>();
                    foreach (var place in asked.Places)
                    {
                        var me = core.At(place);
                        if (turn == 1 && core.CanShift(me)) { answer.Add(BattleChoice.Shift(place)); continue; }
                        var foes = core.EnemySlots.Where(b => b.IsActive && core.InReach(me, b)).ToList();
                        int move = turn % me.Pokemon!.Moves.Count;
                        answer.Add(BattleChoice.Fight(place, move, foes.Count == 0 ? null : foes[turn % foes.Count].Place));
                    }
                    core.Submit(answer);
                    turn++;
                    break;
                case ReplacementRequest replace:
                    core.Submit(BattleChoice.Switch(replace.Place, Enumerable.Range(0, core.PlayerParty.Count).First(i => core.CanSendIn(core.At(replace.Place), i))));
                    break;
            }
        }
        Assert.Null(core.Request);
        var record = core.Record!;
        Assert.Equal(BattleFormat.Triple, record.Format);
        Assert.Contains(record.Answers.SelectMany(a => a), c => c.Kind == ChoiceKind.Shift);

        var again = Fresh(BattleFormat.Triple);
        again.Replay(record);
        Assert.Equal(Lines(core.Log), Lines(again.Log));
        Assert.Equal(core.Result, again.Result);
        // The same seed and answers in a double are another battle: refused
        Assert.Throws<ArgumentException>(() => Fresh(BattleFormat.Double).Replay(record));
    }

    [Fact]
    public void AHundredTriplesAlwaysEndAndLeaveNothingOutOfRange()
    {
        // The fuzz of BattleCoreTests with triples: three to six of the player's against a trainer's three to six (every
        // fourth a wild three), at any level, both sides chosen for by the trainer AI, under the modern rules. Reach is
        // the net here (plan 14's risk note): a battle that stalls out of reach never ends, and a target out of reach
        // that is hit anyway is the rule read "the other side" without asking reach
        var species = PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).ToList();
        for (int battle = 0; battle < 100; battle++)
        {
            var dice = new Random(battle * 7919 + 3);
            var mine = new Party();
            int own = 3 + battle % 4;
            for (int i = 0; i < own; i++) mine.Add(new Pokemon(species[dice.Next(species.Count)], dice.Next(10, 80), dice));
            var theirs = Enumerable.Range(0, 3 + (battle / 4) % 4).Select(_ => new Pokemon(species[dice.Next(species.Count)], dice.Next(10, 80), dice)).ToList();
            bool wild = battle % 4 == 3;
            var trainer = new Trainer { Id = "fuzz", Name = "Fuzz", TrainerClass = "Ace Trainer", Format = "triple", Ai = PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.Basic | PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.EvalAttack };
            if (!wild) foreach (var p in theirs) trainer.Party.Add(p);
            var core = new BattleCore(new CoreSetup
            {
                PlayerParty = mine, Trainers = wild ? new List<Trainer>() : new List<Trainer> { trainer }, WildPokemon = wild ? theirs.Take(3).ToList() : new List<Pokemon>(),
                Random = new BattleRandom((uint)battle), Rules = Ruleset.Modern, Format = BattleFormat.Triple, PlayerController = TrainerAi.Instance
            });
            Assert.Equal(BattleFormat.Triple, core.Format);
            core.Start();

            Assert.Null(core.Request);
            Assert.True(core.Result is BattleResult.PlayerVictory or BattleResult.PlayerDefeat or BattleResult.PlayerRan or BattleResult.EnemyFled, $"triple {battle} ended as {core.Result}");
            Assert.True(core.Turn < 1000, $"triple {battle} took {core.Turn} turns");
            Assert.IsType<Ended>(core.TakeLog().Last());
            foreach (var p in mine.Members.Concat(theirs))
            {
                Assert.InRange(p.CurrentHP, 0, p.MaxHP);
                Assert.All(p.Moves, m => Assert.InRange(m.CurrentPP, 0, m.MaxPP));
                Assert.All(p.StatStages.Values, stage => Assert.InRange(stage, -6, 6));
            }
            if (core.Result is BattleResult.PlayerVictory or BattleResult.PlayerDefeat)
            {
                bool mineDown = mine.Members.All(p => p.IsFainted), theirsDown = (wild ? theirs.Take(3) : theirs).All(p => p.IsFainted);
                Assert.True(core.Result == BattleResult.PlayerDefeat ? mineDown : theirsDown, $"triple {battle}: {core.Result}");
            }
        }
    }

    // ------------------------------------------------------------------ the data and the world

    [Fact]
    public void TheMovesThatReachAnywhereAreShowdownsDistanceMoves()
    {
        foreach (var name in new[] { "Aerial Ace", "Aura Sphere", "Water Pulse", "Air Slash", "Dark Pulse", "Dragon Pulse", "Wing Attack", "Peck", "Gust", "Hurricane", "Acrobatics", "Fly", "Bounce", "Chatter" })
            Assert.True(MoveDatabase.Get(name).Flags.HasFlag(MoveFlags.Distance), $"{name} reaches anywhere");
        foreach (var name in new[] { "Tackle", "Flamethrower", "Earthquake", "Rock Slide", "Surf", "Thunderbolt" })
            Assert.False(MoveDatabase.Get(name).Flags.HasFlag(MoveFlags.Distance), $"{name} reaches only what is near");
    }

    [Fact]
    public void NoTrainerOfTheGamesOrOfTheWorldBattlesInALaterFormat()
    {
        // Plan 14, decision 1: Platinum's places keep Platinum's formats. Nobody of the tables, and nobody placed in any
        // region's world, battles in a triple (or anything else of the later games); the harness's `formats` mode
        // stages its own (ft30 to ft32)
        Assert.All(TrainerDatabase.All.Concat(TrainerDatabase.Masters).Concat(TrainerDatabase.Ours), t => Assert.Null(t.Format));
        var maps = MapDatabase.Fresh();
        var later = new List<string>();
        foreach (string name in MapDatabase.MapNames)
            foreach (var npc in maps(name).Everyone)
                if (npc.TrainerData is { Format: { } format }) later.Add($"{name}: {npc.TrainerData.Id} {format}");
        Assert.True(later.Count == 0, "Trainers of a later format:\n" + string.Join("\n", later));
        // And a trainer's format asks for a triple only by its word
        Assert.Equal(BattleFormat.Triple, new Trainer { Format = "triple" }.AskedFormat);
        Assert.Null(new Trainer().AskedFormat);
        Assert.Null(new Trainer { DoubleBattle = true }.AskedFormat);
    }

    // ------------------------------------------------------------------ the face

    /// <summary>A triple against a trainer on the screen, played up to the first choice, under the modern rules.</summary>
    private static BattleEngine TripleBattle(Pokemon[] mine, Pokemon[] theirs)
    {
        var party = new Party();
        foreach (var p in mine) party.Add(p);
        var trainer = new Trainer { Id = "ace", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 100, Format = "triple" };
        foreach (var p in theirs) trainer.Party.Add(p);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(), Trainers = new List<Trainer> { trainer },
            Format = BattleFormat.Triple, Random = Steady(), Rules = Ruleset.Modern
        });
        Settle(battle);
        return battle;
    }

    [Fact]
    public void ThePlayersThreeStandInARowFacingTheFoesThree()
    {
        var mine = Enumerable.Range(0, 3).Select(s => BattlePlaces.Spot(BattleSide.Player, s, 3)).ToArray();
        var theirs = Enumerable.Range(0, 3).Select(s => BattlePlaces.Spot(BattleSide.Enemy, s, 3)).ToArray();
        // The player's in a straight row across their platform, the first on the left as a double's first
        Assert.All(mine, p => Assert.Equal(BattleStage.PlayerSpot.Z, p.Z, 4));
        Assert.True(mine[0].X < mine[1].X && mine[1].X < mine[2].X);
        Assert.Equal(BattleStage.PlayerSpot, mine[1]);
        Assert.All(mine, p => Assert.True(MathF.Abs(p.X - BattleStage.PlayerSpot.X) <= BattleStage.PlayerPlatformRadius, $"{p} is off the platform"));
        // Each across from the foe the rules call across from it: the player's first and the foe's third on one side
        for (int s = 0; s < 3; s++)
            Assert.Equal(MathF.Sign(mine[s].X - BattleStage.PlayerSpot.X), MathF.Sign(theirs[2 - s].X - BattleStage.EnemySpot.X));
        // The boxes of the player's three stand above the commands and the target cards, on the picture
        for (int s = 0; s < 3; s++)
        {
            var at = ModernUi.TripleBoxAt(s);
            Assert.True(at.Y + ModernUi.HordeBoxHeight <= ModernUi.TargetCard(0, 3).Y, $"box {s} reaches the target cards");
            Assert.True(at.X + ModernUi.HordeBoxWidth <= 1920 - 48 && at.X > 960);
        }
    }

    [Fact]
    public void TheFaceOfATripleHasThreeASideSixCardsAndShift()
    {
        var battle = TripleBattle(Three("Golem", 50, "Tackle", "Aura Sphere"), Three("Bidoof", 40, "Tackle"));
        Assert.True(battle.IsTriple);
        Assert.Equal(3, battle.Anim.SlotsOf(BattleSide.Player));
        Assert.Equal(3, battle.Anim.SlotsOf(BattleSide.Enemy));
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);
        Battler[] foe = battle.EnemySlots.ToArray(), own = battle.PlayerSlots.ToArray();
        for (int s = 0; s < 3; s++) Assert.True(battle.Anim[BattleSide.Player, s].Present && battle.Anim[BattleSide.Enemy, s].Present);

        // The six cards: the foes as the stage stands them (the first on the right), the player's three under them
        Assert.Equal(3, battle.TargetColumns);
        Assert.Equal(new Battler?[] { foe[2], foe[1], foe[0], own[0], own[1], own[2] }, battle.TargetLayout.ToArray());

        // At the left end the fourth command is SHIFT
        Assert.Equal(0, battle.MenuBattler.Slot);
        Assert.True(battle.OffersShift);
        Assert.Equal(Text.Of("battleMenu.shift"), ModernUi.CommandLabels(battle).Small[2]);

        // Tackle offers only what it reaches: the foe across, the centre and the ally beside; Aura Sphere every one
        battle.SelectMainMenuOption(0);
        battle.SelectMove(0);
        Assert.Equal(BattleMenuState.SelectTarget, battle.HUD.MenuState);
        Assert.Equal(new[] { foe[2], foe[1], own[1] }, battle.TargetChoices.ToArray());
        battle.HUD.MenuState = BattleMenuState.Moves;
        battle.SelectMove(1);
        Assert.Equal(new[] { foe[2], foe[1], foe[0], own[1], own[2] }, battle.TargetChoices.ToArray());
        battle.SelectTarget(2);

        // The centre has RUN as ever
        Assert.Equal(1, battle.MenuBattler.Slot);
        Assert.False(battle.OffersShift);
        Assert.Equal(Text.Of("battleMenu.run"), ModernUi.CommandLabels(battle).Small[2]);
    }

    [Fact]
    public void ShiftOnTheScreenTradesTheViewsWithTheCentre()
    {
        var battle = TripleBattle(new[] { Mon("Golem", 50, "Tackle"), Mon("Bidoof", 50, "Tackle"), Mon("Bidoof", 50, "Tackle") }, Three("Bidoof", 5, "Tackle"));
        var golem = battle.PlayerSlots[0].Pokemon!;
        var centre = battle.PlayerSlots[1].Pokemon!;
        battle.SelectMainMenuOption(3);
        Assert.Equal(1, battle.MenuBattler.Slot);
        for (int slot = 1; slot < 3; slot++)
        {
            battle.SelectMainMenuOption(0);
            battle.SelectMove(0);
            if (battle.HUD.MenuState == BattleMenuState.SelectTarget) battle.SelectTarget(0);
        }
        var said = Settle(battle);
        Assert.Contains(Text.Of("battle.shiftedToTheCentre", ("pokemon", golem.DisplayName)), said);
        Assert.Same(golem, battle.PlayerSlots[1].Pokemon);
        Assert.Same(centre, battle.PlayerSlots[0].Pokemon);
        Assert.Same(golem, battle.Anim[BattleSide.Player, 1].Shown);
        Assert.Same(centre, battle.Anim[BattleSide.Player, 0].Shown);
    }
}
