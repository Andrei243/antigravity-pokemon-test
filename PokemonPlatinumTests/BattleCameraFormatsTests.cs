using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Graphics;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 45 · BC7: the manners over the battle's formats and the rest: a triple, a rotation, a Battle Royal in four
/// quarters, an SOS battle's ally hopping in, the Frontier's walled halls and its Brains, a photo composed over the
/// cycle, and the options' edge cases. Link battles and recordings watched are in their own files.
/// </summary>
[Collection("Shared clocks and dice")]
public class BattleCameraFormatsTests
{
    private static readonly CameraManner[] Moving = CameraManners.All.Where(m => !m.IsStill).ToArray();

    public static IEnumerable<object[]> Manners => Moving.Select(m => new object[] { m.Name });

    private static CameraManner Galar => CameraManners.Find("galar")!;

    /// <summary>Who stands where in a battle of these many places a side, each of this height, everyone out.</summary>
    internal static (BattleAnimator Anim, FxPlaces Places) Battle(int foes, int own, float foeHeight, float ownHeight, bool present = true)
    {
        var anim = new BattleAnimator { LeadsMayDivide = true };
        if (foes > 1 || own > 1) anim.SetSlots(own, foes);
        var places = new FxPlaces();
        for (int slot = 0; slot < foes; slot++)
        {
            places.Set(BattleSide.Enemy, slot, BattlePlaces.Spot(BattleSide.Enemy, slot, foes) + Vector3.UnitY * BattleStage.PlatformHeight, foeHeight * BattlePlaces.Fit(foes));
            anim[BattleSide.Enemy, slot].Present = present;
        }
        for (int slot = 0; slot < own; slot++)
        {
            places.Set(BattleSide.Player, slot, BattlePlaces.Spot(BattleSide.Player, slot, own) + Vector3.UnitY * BattleStage.PlatformHeight, ownHeight * BattlePlaces.Fit(own));
            anim[BattleSide.Player, slot].Present = present;
        }
        anim.Update(2f, null, null);
        anim.NoteMenu(true);
        return (anim, places);
    }

    private static void Advance(BattleAnimator anim, float seconds)
    {
        for (float t = 0f; t < seconds - 1e-4f; t += 1f / 60f) anim.Update(1f / 60f, null, null);
    }

    // ------------------------------------------------------------------ the formats

    public static IEnumerable<object[]> TripleShapes()
    {
        foreach (var m in Moving)
            foreach (var (foeH, ownH) in new[] { (0.6f, 0.6f), (1.4f, 1.2f), (2.6f, 2.2f) })
                yield return new object[] { m.Name, foeH, ownH };
    }

    [Theory]
    [MemberData(nameof(TripleShapes))]
    public void ATriplesCycleAndSplitHoldAllThreeOfASideInThePicturesFreePart(string name, float foeH, float ownH)
    {
        var manner = CameraManners.Find(name)!;
        var (anim, places) = Battle(3, 3, foeH, ownH);
        var panels = IdleFrame.Panels(3, 3);
        foreach (var idle in manner.Cycle.Where(s => s is IdleShot.FoeFace or IdleShot.OwnFace))
        {
            if (BattleCamera.IdleShotOf(idle, anim, places, manner) is not { } shot) continue;
            var side = idle == IdleShot.FoeFace ? BattleSide.Enemy : BattleSide.Player;
            for (int slot = 0; slot < 3; slot++)
                Assert.True(IdleFrame.BoxOf(shot, places, side, slot) is { } b && IdleFrame.Holds(b, panels), $"{name} {idle} slot {slot}");
        }
        var split = BattleCamera.SplitOf(anim, places, manner, 1f);
        Assert.False(split.Royal);
        for (int slot = 0; slot < 3; slot++)
        {
            var foe = IdleFrame.BoxOf(split.Foe, places, BattleSide.Enemy, slot)!.Value;
            var own = IdleFrame.BoxOf(split.Own, places, BattleSide.Player, slot)!.Value;
            Assert.True(IdleFrame.Holds(foe, panels) && SplitBand.InRegion(true, 1f, new Vector2(foe.X + foe.Width, foe.Y + foe.Height), SplitBand.BandWidth / 2f), $"{name} foe {slot}: {foe}");
            Assert.True(IdleFrame.Holds(own, panels) && SplitBand.InRegion(false, 1f, new Vector2(own.X, own.Y), SplitBand.BandWidth / 2f), $"{name} own {slot}: {own}");
        }
    }

    [Theory]
    [MemberData(nameof(Manners))]
    public void ARotationBattleIsFilmedByItsFrontSeatAlone(string name)
    {
        var manner = CameraManners.Find(name)!;
        // One place a side, with the two seats behind it on the turntable and not in the battle
        var (anim, places) = Battle(1, 1, 1.3f, 1.1f);
        anim.Turntables = true;
        var panels = IdleFrame.Panels(1, 1);
        foreach (var (shot, kind) in BattleCamera.CycleFor(anim, places, manner).Where(c => c.Kind is ShotKind.Idle))
            foreach (var (side, slot) in new[] { (BattleSide.Enemy, 0) })
                Assert.True(IdleFrame.BoxOf(shot, places, side, slot) is { } b && (IdleFrame.Holds(b, panels) || IdleFrame.Outside(b)), $"{name}");
        var split = BattleCamera.SplitOf(anim, places, manner, 1f);
        Assert.False(split.Royal);
        Assert.True(IdleFrame.Holds(IdleFrame.BoxOf(split.Foe, places, BattleSide.Enemy, 0)!.Value, panels));
        Assert.True(IdleFrame.Holds(IdleFrame.BoxOf(split.Own, places, BattleSide.Player, 0)!.Value, panels));

        // A turn's own shot has the camera while the table turns, whatever the manner
        anim.Rotate(BattleSide.Player, new PokemonPlatinumEngine.Models.Pokemon(PokemonPlatinumEngine.Data.PokemonDatabase.Get("Golem")!, 20), 1);
        Assert.Equal(ShotKind.Rotation, BattleCamera.Direct(anim, places, false, manner).Kind);
    }

    // ------------------------------------------------------------------ the Battle Royal in four quarters

    private static (BattleAnimator Anim, FxPlaces Places) Royal(float foeHeight, float ownHeight)
    {
        var anim = new BattleAnimator { LeadsMayDivide = true, Royal = true };
        anim.SetSlots(1, 3);
        var places = new FxPlaces();
        for (int slot = 0; slot < 3; slot++)
        {
            // The ring the renderer stands a Royal's three in (BattlePlaces.RingSpots), so the quarters are framed on the places the game draws
            places.Set(BattleSide.Enemy, slot, BattlePlaces.Spot(BattleSide.Enemy, slot, 3, royal: true) + Vector3.UnitY * BattleStage.PlatformHeight, foeHeight * BattlePlaces.Fit(3, true, BattleSide.Enemy));
            anim[BattleSide.Enemy, slot].Present = true;
        }
        places.Set(BattleSide.Player, 0, BattlePlaces.Spot(BattleSide.Player, 0, 1) + Vector3.UnitY * BattleStage.PlatformHeight, ownHeight);
        anim[BattleSide.Player, 0].Present = true;
        anim.Update(2f, null, null);
        return (anim, places);
    }

    public static IEnumerable<object[]> RoyalShapes()
    {
        foreach (var (foeH, ownH) in new[] { (0.6f, 0.6f), (1.4f, 1.2f), (2.6f, 2.2f), (3.4f, 3.0f) })
            yield return new object[] { foeH, ownH };
    }

    [Theory]
    [MemberData(nameof(RoyalShapes))]
    public void EveryCampOfARoyalStandsWhollyInItsQuarter(float foeH, float ownH)
    {
        var (anim, places) = Royal(foeH, ownH);
        Assert.True(BattleCamera.RoyalStands(anim));
        var split = BattleCamera.SplitOf(anim, places, Galar, 1f);
        Assert.True(split.Royal);
        Assert.Equal(RoyalBand.Camps, split.Camps!.Length);
        Assert.Equal(split.Camps[0], split.Foe);
        Assert.Equal(split.Camps[RoyalBand.Own], split.Own);
        var panels = IdleFrame.Panels(3, 1);
        for (int q = 0; q < RoyalBand.Camps; q++)
        {
            var (side, slot) = q < 3 ? (BattleSide.Enemy, q) : (BattleSide.Player, 0);
            var box = IdleFrame.BoxOf(split.Camps[q], places, side, slot)!.Value;
            Assert.True(IdleFrame.Holds(box, panels), $"quarter {q}: {box} is under a panel or off the picture's free part");
            Assert.True(RoyalBand.InColumn(q, new Vector2(box.X, box.Y), SplitBand.BandWidth / 2f)
                && RoyalBand.InColumn(q, new Vector2(box.X + box.Width, box.Y + box.Height), SplitBand.BandWidth / 2f), $"quarter {q}: {box} crosses its quarter's edge");
        }
    }

    [Fact]
    public void TheQuartersCoverThePictureOnceInAndNeverOverlapWithinATarget()
    {
        float whole = RoyalBand.Width * RoyalBand.Height;
        for (int q = 0; q < RoyalBand.Camps; q++) Assert.Empty(RoyalBand.Region(q, 0f));
        float total = 0f;
        for (int q = 0; q < RoyalBand.Camps; q++) total += SplitBand.Area(RoyalBand.Region(q, 1f));
        Assert.InRange(total, whole - 1f, whole + 1f);
        float last = 0f;
        for (float a = 0.1f; a <= 1f; a += 0.1f)
        {
            float area = Enumerable.Range(0, RoyalBand.Camps).Sum(q => SplitBand.Area(RoyalBand.Region(q, a)));
            Assert.True(area >= last, "the quarters only grow as they come in");
            last = area;
        }
        // No two quarters drawn into one target overlap, at any amount
        for (float a = 0.1f; a <= 1f; a += 0.1f)
            for (int i = 0; i < RoyalBand.Camps; i++)
                for (int j = i + 1; j < RoyalBand.Camps; j++)
                {
                    if (RoyalBand.InFoeTarget(i) != RoyalBand.InFoeTarget(j)) continue;
                    var a1 = SplitBand.Box(RoyalBand.Region(i, a));
                    var b1 = SplitBand.Box(RoyalBand.Region(j, a));
                    Assert.True(a1.X + a1.Width <= b1.X || b1.X + b1.Width <= a1.X, $"quarters {i} and {j} overlap at {a:0.0}");
                }
        // The foe's target takes two quarters and the player's two
        Assert.Equal(2, Enumerable.Range(0, RoyalBand.Camps).Count(RoyalBand.InFoeTarget));
        // Held whole, the four draw one picture of pixels: no more than the halves do (decision 4)
        float boxes = Enumerable.Range(0, RoyalBand.Camps).Sum(q => { var b = SplitBand.Box(RoyalBand.Region(q, 1f)); return b.Width * b.Height; });
        float halves = new[] { true, false }.Sum(f => { var b = SplitBand.Box(SplitBand.Region(f, 1f)); return b.Width * b.Height; });
        Assert.True(boxes <= whole + 1f && boxes <= halves);
    }

    [Fact]
    public void OnlyTheMannersWithQuartersSplitARoyalAndAnyOtherBattleIsUntouched()
    {
        var (anim, places) = Royal(1.2f, 1f);
        Advance(anim, 0.1f);
        Assert.Single(anim.SplitMoments);
        foreach (var m in CameraManners.All)
        {
            bool wants = (m.Splits & CameraSplits.RoyalQuarters) != 0;
            Assert.Equal(wants && (m.Splits & CameraSplits.Leads) != 0, BattleCamera.SplitNow(anim, places, m) is { Royal: true });
            if (!wants) Assert.Null(BattleCamera.SplitNow(anim, places, m));
        }
        Assert.Equal(new[] { "alola", "galar", "kalos", "paldea" }, CameraManners.All.Where(m => (m.Splits & CameraSplits.RoyalQuarters) != 0).Select(m => m.Name).OrderBy(n => n));

        // A Royal with a camp gone is split in halves, as any battle is; a battle that is no Royal never in quarters
        anim[BattleSide.Enemy, 1].Present = false;
        Assert.False(BattleCamera.RoyalStands(anim));
        Assert.False(BattleCamera.SplitOf(anim, places, Galar, 1f).Royal);
        var (double2, p2) = Battle(2, 2, 1.2f, 1f);
        Assert.False(BattleCamera.SplitOf(double2, p2, Galar, 1f).Royal);
        // The halves of such a battle are what they were: same shots with the quarters' code in the build
        var halves = BattleCamera.SplitOf(double2, p2, Galar, 0.5f);
        Assert.Null(halves.Camps);
    }

    [Fact]
    public void ARoyalsSplitComesInHeldAndGoesOutByTheAnimatorsClock()
    {
        var (anim, places) = Royal(1.2f, 1f);
        // The leads' moment came at the first frame after the sweep: in over 0.3 s, held 1.4 s, out over 0.3 s
        float start = anim.SplitMoments[0].Start;
        Assert.Equal(0f, BattleCamera.SplitAmount(anim.SplitMoments[0], start - 0.01f));
        Assert.True(BattleCamera.SplitAmount(anim.SplitMoments[0], start + 0.15f) is > 0f and < 1f);
        Assert.Equal(1f, BattleCamera.SplitAmount(anim.SplitMoments[0], start + 1f));
        Assert.Equal(0f, BattleCamera.SplitAmount(anim.SplitMoments[0], start + 2.1f));
        Advance(anim, 0.15f);
        Assert.True(BattleCamera.SplitNow(anim, places, Galar) is { Royal: true, In: > 0f and < 1f });
        // Under REDUCE MOTION and with the scene off, no split at all
        PokemonPlatinumEngine.Graphics.BattleCamera.Motion = 0f;
        try { Assert.Null(BattleCamera.SplitNow(anim, places, Galar)); }
        finally { PokemonPlatinumEngine.Graphics.BattleCamera.Motion = 1f; }
        Assert.Null(BattleCamera.SplitNow(anim, places, Galar, scene: false));
    }

    // ------------------------------------------------------------------ SOS allies

    [Fact]
    public void AnSosBattlesWildSideBeforeItsAllyLandsIsOnePlace()
    {
        // SlotsOf says two (the ally's place is there), but only the first has a Pokémon out
        var (anim, places) = Battle(2, 1, 1.3f, 1.1f);
        anim[BattleSide.Enemy, 1].Present = false;
        // The ally's place stands where the first does; any shot framing "every place" would frame it too
        places.Set(BattleSide.Enemy, 1, new Vector3(40f, 0f, -30f), 1.3f);
        var (single, singlePlaces) = Battle(1, 1, 1.3f, 1.1f);
        // A single with its one foe standing where the first place stands
        singlePlaces.Set(BattleSide.Enemy, 0, places.Feet(BattleSide.Enemy, 0), places.Height(BattleSide.Enemy, 0));
        foreach (var manner in Moving)
        {
            // The foe's own shots frame the first place alone: standing where a single's would have it
            foreach (var idle in manner.Cycle.Where(s => s is IdleShot.FoeFace or IdleShot.Shoulder))
            {
                var a = BattleCamera.IdleShotOf(idle, anim, places, manner);
                var b = BattleCamera.IdleShotOf(idle, single, singlePlaces, manner);
                Assert.Equal(b is null, a is null);
                if (a is { } x && b is { } y) Assert.True(Vector3.Distance(x.Target, y.Target) < 0.05f, $"{manner.Name} {idle}: {x.Target} v {y.Target}");
            }
        }
    }

    [Fact]
    public void AnAllyHoppingInIsFilmedByAMannerThatReactsAndByNoOtherAndNeverLongerThanItsHop()
    {
        var (anim, places) = Battle(2, 1, 1.3f, 1.1f);
        anim[BattleSide.Enemy, 1].Present = false;
        anim.Gather(BattleSide.Enemy);
        anim.Update(0.1f, null, null);
        Assert.Null(BattleCamera.AllyNow(anim, places));
        Assert.NotEqual(ShotKind.Ally, BattleCamera.Direct(anim, places, false, Galar).Kind);

        anim.Arrive(BattleSide.Enemy, new PokemonPlatinumEngine.Models.Pokemon(PokemonPlatinumEngine.Data.PokemonDatabase.Get("Starly")!, 8), 1);
        Assert.NotNull(BattleCamera.AllyNow(anim, places));
        foreach (var m in CameraManners.All)
        {
            var kind = BattleCamera.Direct(anim, places, false, m).Kind;
            Assert.Equal(m.React && !m.IsStill ? ShotKind.Ally : kind, kind);
            if (m.React) Assert.Equal(ShotKind.Ally, kind);
            else Assert.NotEqual(ShotKind.Ally, kind);
        }
        // The shot holds both places of the foe's side
        var shot = BattleCamera.AllyNow(anim, places)!.Value;
        Assert.True(shot.Position.Z <= BattleCamera.FoeShotLimit + 1e-3f);
        // ...until it has landed
        Advance(anim, BattleAnimator.ArriveTime + 0.05f);
        Assert.Null(BattleCamera.AllyNow(anim, places));
        Assert.NotEqual(ShotKind.Ally, BattleCamera.Direct(anim, places, false, Galar).Kind);
    }

    [Fact]
    public void ARotationBattlesFrontSeatHoppingOnKeepsTheRotationsShotNotTheAllys()
    {
        var (anim, places) = Battle(1, 1, 1.3f, 1.1f);
        anim.Turntables = true;
        anim.Rotate(BattleSide.Enemy, new PokemonPlatinumEngine.Models.Pokemon(PokemonPlatinumEngine.Data.PokemonDatabase.Get("Staravia")!, 20), 1);
        Assert.Null(BattleCamera.AllyNow(anim, places));
        Assert.Equal(ShotKind.Rotation, BattleCamera.Direct(anim, places, false, Galar).Kind);
    }

    // ------------------------------------------------------------------ the Frontier

    [Theory]
    [InlineData("Leader", true)]
    [InlineData("Elite Four", true)]
    [InlineData("Champion", true)]
    [InlineData("Tower Tycoon", true)]
    [InlineData("Hall Matron", true)]
    [InlineData("Factory Head", true)]
    [InlineData("Arcade Star", true)]
    [InlineData("Castle Valet", true)]
    [InlineData("Ace Trainer", false)]
    [InlineData("Galactic", false)]
    [InlineData("Rival", false)]
    public void ABossIsALeaderAnEliteFourTheChampionOrAFrontierBrain(string trainerClass, bool boss)
    {
        Assert.Equal(boss, BattleMeeting.IsBoss(trainerClass));
    }

    [Fact]
    public void AFrontierBrainsBattleIsFilmedWithTheCraneAndABossSplit()
    {
        var brain = new PokemonPlatinumEngine.Models.Trainer { Name = "Palmer", TrainerClass = "Tower Tycoon", Id = "tower_tycoon" };
        brain.Party.Add(new PokemonPlatinumEngine.Models.Pokemon(PokemonPlatinumEngine.Data.PokemonDatabase.Get("Staraptor")!, 50));
        var mine = new PokemonPlatinumEngine.Models.Party();
        mine.Add(new PokemonPlatinumEngine.Models.Pokemon(PokemonPlatinumEngine.Data.PokemonDatabase.Get("Luxray")!, 50));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = mine, Inventory = new PokemonPlatinumEngine.Models.Inventory(), Pokedex = new PokemonPlatinumEngine.Models.Pokedex(),
            Trainers = new List<PokemonPlatinumEngine.Models.Trainer> { brain }, WildPokemon = new List<PokemonPlatinumEngine.Models.Pokemon>(),
            Random = new Random(3), Frontier = true
        });
        Assert.True(battle.Anim.BossBattle);
        Assert.False(battle.Anim.Royal);
        var (anim, places) = Battle(1, 1, 1.3f, 1.1f);
        anim.BossBattle = true;
        Assert.Contains(BattleCamera.CycleFor(anim, places, Galar), c => c.Kind == ShotKind.Idle && c.Shot.Position.Y > 15f);
        Assert.True(BattleCamera.SplitsAt(CameraManners.Sinnoh, new SplitMoment(SplitReason.LastPokemon, 0f, boss: true)));
    }

    public static IEnumerable<object[]> Halls()
    {
        foreach (var kind in new[] { PokemonPlatinumEngine.Overworld.BattleArena.Indoors, PokemonPlatinumEngine.Overworld.BattleArena.Gym, PokemonPlatinumEngine.Overworld.BattleArena.League })
            foreach (var m in Moving)
                yield return new object[] { kind, m.Name };
    }

    [Theory]
    [MemberData(nameof(Halls))]
    public void NoShotOfAnyMannerStandsOutsideAWalledHall(PokemonPlatinumEngine.Overworld.BattleArena kind, string name)
    {
        var manner = CameraManners.Find(name)!;
        var (minX, maxX, minZ, maxZ, _) = BattleStage.Enclosure(kind)!.Value;
        var outside = new List<string>();
        void Check(string what, CameraShot shot)
        {
            var p = shot.Position;
            if (p.X < minX + 0.4f || p.X > maxX - 0.4f || p.Z < minZ + 0.4f || p.Z > maxZ)
                outside.Add($"{what}: {p}");
        }
        foreach (var (foes, own) in new[] { (1, 1), (2, 2), (3, 3), (5, 1) })
            foreach (var (foeH, ownH) in new[] { (0.6f, 0.6f), (1.4f, 1.2f), (2.6f, 2.2f) })
            {
                var (anim, places) = Battle(foes, own, foeH, ownH);
                anim.BossBattle = true;
                foreach (var (shot, k) in BattleCamera.CycleFor(anim, places, manner)) Check($"{foes}v{own} {foeH} cycle {k}", shot);
                var split = BattleCamera.SplitOf(anim, places, manner, 1f);
                Check($"{foes}v{own} {foeH} split foe", split.Foe);
                Check($"{foes}v{own} {foeH} split own", split.Own);
                // The director frame by frame: a menu opened and left open through two cycles, the opening and the closing
                for (float t = 0f; t < 40f; t += 0.25f)
                {
                    anim.NoteMenu(true);
                    Advance(anim, 0.25f);
                    Check($"{foes}v{own} {foeH} menu at {t}", BattleCamera.Direct(anim, places, true, manner).Shot);
                }
            }
        var (royal, royalPlaces) = Royal(1.4f, 1.2f);
        foreach (var shot in BattleCamera.SplitOf(royal, royalPlaces, manner, 1f).Camps ?? Array.Empty<CameraShot>()) Check("royal quarter", shot);
        foreach (bool boss in new[] { false, true })
            foreach (var trainer in new[] { "LEADER", "LASS" })
            {
                var (open, openPlaces) = Battle(1, 1, 1.4f, 1.2f, present: false);
                open.EnemyTrainer = trainer;
                open.BossBattle = boss;
                for (float t = 0f; t < 4f; t += 0.1f)
                {
                    Check($"opening {trainer} boss {boss} at {t:0.0}", BattleCamera.Direct(open, openPlaces, false, manner).Shot);
                    Advance(open, 0.1f);
                }
                var (close, closePlaces) = Battle(1, 1, 1.4f, 1.2f);
                close.BossBattle = boss;
                close.NoteClosing(BattleClosing.Won);
                for (float t = 0f; t < 8f; t += 0.1f)
                {
                    Check($"closing boss {boss} at {t:0.0}", BattleCamera.Direct(close, closePlaces, false, manner).Shot);
                    Advance(close, 0.1f);
                }
            }
        Assert.True(outside.Count == 0, $"{kind} {name}: {outside.Count} shots outside the walls, the first: {string.Join("; ", outside.Take(4))}");
    }

    [Fact]
    public void TheOpenArenasHaveNoWalls()
    {
        foreach (var kind in Enum.GetValues<PokemonPlatinumEngine.Overworld.BattleArena>())
        {
            bool walled = kind is PokemonPlatinumEngine.Overworld.BattleArena.Indoors or PokemonPlatinumEngine.Overworld.BattleArena.Gym or PokemonPlatinumEngine.Overworld.BattleArena.League;
            Assert.Equal(walled, BattleStage.Enclosure(kind) != null);
        }
    }

    // ------------------------------------------------------------------ a photo over the cycle

    [Theory]
    [MemberData(nameof(Manners))]
    public void APhotoBeginsFromTheShotTheCycleHadOnScreenNotTheOverview(string name)
    {
        var manner = CameraManners.Find(name)!;
        var (anim, places) = Battle(1, 1, 1.3f, 1.1f);
        anim.BossBattle = true;
        var director = new BattleCamera { Manner = manner };
        float span = manner.Cycle.Count > 0 ? manner.Cycle.Count * (manner.CycleHold + manner.CycleEase) : manner.DriftPeriod;
        bool sawAnIdleShot = false;
        for (float t = 0f; t < span; t += 1f / 60f)
        {
            anim.NoteMenu(true);
            anim.Update(1f / 60f, null, null);
            director.Update(1f / 60f, anim, places, 0f, choosing: true);
            if (director.Kind is not (ShotKind.Idle or ShotKind.OwnFront)) continue;
            sawAnIdleShot = true;
            // The photo's held shot starts exactly where the camera stands
            var view = new BattlePhotoView(BattleCamera.PhotoStart(director.Current, null));
            var held = view.Shot;
            var now = director.Current;
            // Same stand (but a camera under the photo's lowest height is lifted to it), same way of looking, same lens
            Assert.True(Vector3.Distance(held.Position, now.Position) < 0.05f
                || now.Position.Y < BattlePhotoView.LowestHeight + 0.05f, $"{name} at {t:0.00}: {held.Position} v {now.Position}");
            if (now.Position.Y >= BattlePhotoView.LowestHeight)
                Assert.True(Vector3.Dot(Vector3.Normalize(held.Target - held.Position), Vector3.Normalize(now.Target - now.Position)) > 0.9999f, $"{name} at {t:0.00}");
            Assert.Equal(now.Fov, held.Fov);
            // ...and it is the cycle's own shot, not the overview's: another stand or a narrower lens
            var overview = BattleCamera.Overview(anim.Time);
            Assert.True(Vector3.Distance(overview.Position, held.Position) > 0.05f || held.Fov < BattleCamera.OverviewFov - 0.5f, $"{name} at {t:0.00}");
        }
        Assert.True(sawAnIdleShot || manner.Cycle.Count == 0, name);
    }

    [Fact]
    public void APhotoDuringASplitBeginsFromTheFoesHalfAndOtherwiseFromWhereTheCameraStands()
    {
        var (anim, places) = Battle(1, 1, 1.3f, 1.1f);
        var split = BattleCamera.SplitOf(anim, places, Galar, 1f);
        var standing = BattleCamera.Overview(0f);
        Assert.Equal(split.Foe, BattleCamera.PhotoStart(standing, split));
        Assert.Equal(split.Foe, BattleCamera.PhotoStart(standing, BattleCamera.SplitOf(anim, places, Galar, 0.5f)));
        Assert.Equal(standing, BattleCamera.PhotoStart(standing, BattleCamera.SplitOf(anim, places, Galar, 0.4f)));
        Assert.Equal(standing, BattleCamera.PhotoStart(standing, null));
        var (royal, royalPlaces) = Royal(1.2f, 1f);
        var quarters = BattleCamera.SplitOf(royal, royalPlaces, Galar, 1f);
        Assert.Equal(quarters.Camps![0], BattleCamera.PhotoStart(standing, quarters));
        // The photo turns round what the half looks at, as any photo does
        var view = new BattlePhotoView(BattleCamera.PhotoStart(standing, split));
        Assert.True(Vector3.Distance(view.Pivot, split.Foe.Target) < 0.05f);
    }

    // ------------------------------------------------------------------ the options' edge cases

    [Fact]
    public void WithTheBattleSceneOffEveryMannerIsFilmedAsClassicFrameByFrame()
    {
        foreach (var manner in Moving)
        {
            var (anim, places) = Battle(1, 1, 1.3f, 1.1f);
            var (twin, twinPlaces) = Battle(1, 1, 1.3f, 1.1f);
            var off = new BattleCamera { Manner = manner, SceneOn = false };
            var classic = new BattleCamera { Manner = CameraManners.Classic };
            for (int i = 0; i < 1200; i++)
            {
                bool menu = i % 400 > 100;
                anim.NoteMenu(menu);
                twin.NoteMenu(menu);
                if (i == 700) { anim.NoteClosing(BattleClosing.Won); twin.NoteClosing(BattleClosing.Won); }
                var a = off.Update(1f / 60f, anim, places, 0f, menu);
                var b = classic.Update(1f / 60f, twin, twinPlaces, 0f, menu);
                Assert.Equal(b.Position, a.Position);
                Assert.Equal(b.Target, a.Target);
                Assert.Equal(b.FovY, a.FovY);
                anim.Update(1f / 60f, null, null);
                twin.Update(1f / 60f, null, null);
            }
        }
    }

    [Fact]
    public void UnderReduceMotionAPhotoAndARoyalAndAnAllyAreFilmedAsClassicToo()
    {
        BattleCamera.Motion = 0f;
        try
        {
            var (royal, royalPlaces) = Royal(1.2f, 1f);
            Assert.Null(BattleCamera.SplitNow(royal, royalPlaces, Galar));
            var (anim, places) = Battle(2, 1, 1.3f, 1.1f);
            anim.Arrive(BattleSide.Enemy, new PokemonPlatinumEngine.Models.Pokemon(PokemonPlatinumEngine.Data.PokemonDatabase.Get("Starly")!, 8), 1);
            Assert.NotEqual(ShotKind.Ally, BattleCamera.Direct(anim, places, false, Galar).Kind);
            // A photo of such a battle begins from the overview the menu shows
            anim.Update(2f, null, null);
            anim.NoteMenu(true);
            anim.Update(8f, null, null);
            var (shot, kind, _) = BattleCamera.Direct(anim, places, true, Galar);
            Assert.Equal(ShotKind.Overview, kind);
            Assert.Equal(BattleCamera.Overview(anim.Time), shot);
        }
        finally { BattleCamera.Motion = 1f; }
    }
}
