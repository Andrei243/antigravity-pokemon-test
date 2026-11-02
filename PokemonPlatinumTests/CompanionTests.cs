using System.Numerics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using static PokemonPlatinumTests.CoreScenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 10 · F2 to F5: the walk cycles, the lead walking behind the player, what it says when spoken to, and wild
/// Pokémon in sight with the back strike. The rules alone where they are GPU-free; the engine with no window where
/// the field must play them.
/// </summary>
public class CompanionTests
{
    // ------------------------------------------------------------------ F2 · walk cycles

    private static PokeModel ModelOf(string name) =>
        name.StartsWith("sample ") ? PokemonModels.Sample(Enum.Parse<BodyPlan>(name[7..])) : PokemonModels.Get(name);

    private static SkeletonPose Posed(PokeModel m, PokePose p)
    {
        var pose = new SkeletonPose(m.Bones.Count);
        PokemonAnimation.Apply(m, p, pose);
        return pose;
    }

    private static PokePose Walking(float phase) => new() { Time = 0.4f, Walk = phase, Moving = 1f };

    // How far a bone has turned forward about its joint (the pitch of its rotation)
    private static float Pitch(Quaternion q) => 2f * (q.W * q.X + q.Y * q.Z);

    private static float Yaw(Quaternion q) => 2f * (q.W * q.Y - q.Z * q.X);

    [Fact]
    public void ABipedsLegsAlternateAndItsRootStaysOnTheGround()
    {
        var m = ModelOf("Riolu");
        var legs = Enumerable.Range(0, m.Bones.Count).Where(b => m.Bones[b].Role == PokeRole.Leg && m.Bones[b].Side != 0f).ToList();
        Assert.True(legs.Count >= 2);
        int left = legs.First(b => m.Bones[b].Side < 0f), right = legs.First(b => m.Bones[b].Side > 0f);
        var idle = Posed(m, new PokePose { Time = 0.4f });
        foreach (float phase in new[] { 0.25f, 0.75f })
        {
            var walk = Posed(m, Walking(phase));
            Assert.True(Pitch(walk.Rotation[left]) * Pitch(walk.Rotation[right]) < 0f, $"the legs swing together at {phase}");
            Assert.Equal(idle.Offset[m.Find(PokeRole.Root)].Y, walk.Offset[m.Find(PokeRole.Root)].Y, 4);
        }
        // Half a stride on, each leg has swung the other way
        Assert.True(Pitch(Posed(m, Walking(0.25f)).Rotation[left]) * Pitch(Posed(m, Walking(0.75f)).Rotation[left]) < 0f);
    }

    [Fact]
    public void AQuadrupedsFrontAndHindLegsOppose()
    {
        var m = ModelOf("Shinx");
        int Leg(bool front, float side) => Enumerable.Range(0, m.Bones.Count).First(b =>
            m.Bones[b].Role == PokeRole.Leg && m.Bones[b].Front == front && MathF.Sign(m.Bones[b].Side) == MathF.Sign(side));
        var walk = Posed(m, Walking(0.25f));
        Assert.True(Pitch(walk.Rotation[Leg(true, -1)]) * Pitch(walk.Rotation[Leg(false, -1)]) < 0f, "a side's front and hind legs swing together");
        // and each goes with the one across from it (a trot)
        Assert.True(Pitch(walk.Rotation[Leg(true, -1)]) * Pitch(walk.Rotation[Leg(false, 1)]) > 0f);
        Assert.Equal(0f, walk.Offset[m.Find(PokeRole.Root)].Y, 4);
    }

    [Fact]
    public void AHoveringBodyKeepsItsHeightAndABirdHops()
    {
        var floater = ModelOf("sample Floating");
        int root = floater.Find(PokeRole.Root);
        foreach (float phase in new[] { 0f, 0.2f, 0.5f, 0.8f })
        {
            var idle = Posed(floater, new PokePose { Time = 0.4f, Moving = 1f });
            Assert.Equal(idle.Offset[root].Y, Posed(floater, Walking(phase)).Offset[root].Y, 4);
        }
        var bird = ModelOf("Starly");
        float idleY = Posed(bird, new PokePose { Time = 0.4f, Moving = 1f }).Offset[bird.Find(PokeRole.Root)].Y;
        float top = Enumerable.Range(0, 16).Max(k => Posed(bird, Walking(k / 16f)).Offset[bird.Find(PokeRole.Root)].Y) - idleY;
        Assert.InRange(top, 0.5f * PokemonAnimation.BirdHop * bird.Height, 1.01f * PokemonAnimation.BirdHop * bird.Height);
    }

    [Fact]
    public void AFishUndulatesItsSegmentsInPhaseOrder()
    {
        var fish = ModelOf("sample Fish");
        var segments = Enumerable.Range(0, fish.Bones.Count).Where(b => fish.Bones[b].Role == PokeRole.Segment).OrderBy(b => fish.Bones[b].Phase).ToList();
        if (segments.Count < 2) segments = Enumerable.Range(0, fish.Bones.Count).Where(b => fish.Bones[b].Role is PokeRole.Tail or PokeRole.Fin).ToList();
        Assert.True(segments.Count >= 2);
        // A wave: the segments are not all turned alike at one moment, and each comes to its peak a little after the one before
        var walk = Posed(fish, Walking(0.1f));
        Assert.True(segments.Select(b => Yaw(walk.Rotation[b])).Distinct().Count() > 1, "the segments move as one");
        var serpent = ModelOf("sample Serpent");
        var along = Enumerable.Range(0, serpent.Bones.Count).Where(b => serpent.Bones[b].Role == PokeRole.Segment).OrderBy(b => serpent.Bones[b].Phase).ToList();
        float PeakOf(int b) => Enumerable.Range(0, 64).OrderByDescending(k => Yaw(Posed(serpent, Walking(k / 64f)).Rotation[b])).First() / 64f;
        float first = PeakOf(along[0]), second = PeakOf(along[^1]);
        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData("Riolu")]
    [InlineData("Shinx")]
    [InlineData("Turtwig")]
    [InlineData("Starly")]
    [InlineData("Garchomp")]
    [InlineData("sample Serpent")]
    [InlineData("sample Fish")]
    [InlineData("sample Floating")]
    public void TheWalkIsPureAndKeepsEveryBoneNearItsIdle(string body)
    {
        var m = ModelOf(body);
        var idle = new PokePose { Time = 0.4f };
        m.Animate(idle);
        var rest = m.Skin.ToArray();
        foreach (float phase in new[] { 0f, 0.125f, 0.375f, 0.6f, 0.9f })
        {
            m.Animate(Walking(phase));
            var once = m.Skin.ToArray();
            m.Animate(Walking(phase));
            for (int b = 0; b < once.Length; b++)
            {
                Assert.Equal(once[b], m.Skin[b]);
                float moved = Vector3.Distance(Vector3.Transform(m.Skeleton[b].Joint, rest[b]), Vector3.Transform(m.Skeleton[b].Joint, once[b]));
                Assert.True(moved < 0.35f * Math.Max(1f, m.Height), $"{body}: bone {b} strays {moved:0.00} at {phase}");
            }
        }
        // Standing still, the walk adds nothing
        m.Animate(new PokePose { Time = 0.4f, Walk = 0.3f, Moving = 0f });
        for (int b = 0; b < rest.Length; b++) Assert.Equal(rest[b], m.Skin[b]);
    }

    [Fact]
    public void AnImportedModelsWalkOrRunPlaysForTheWalk()
    {
        Assert.Equal(1, ImportedModels.MatchClips(new[] { "Idle_Loop", "Walk_Loop" })[ClipRole.Walk]);
        Assert.Equal(1, ImportedModels.MatchClips(new[] { "Idle_Loop", "run01" })[ClipRole.Walk]);
        Assert.False(ImportedModels.MatchClips(new[] { "Idle", "Attack01" }).ContainsKey(ClipRole.Walk));
        Assert.Contains("walk", PokemonAnimation.Clips);
        Assert.True(PokemonAnimation.ClipLoops("walk"));
    }

    [Fact]
    public void TheWalkStripsFrameFollowsTheStride()
    {
        Assert.Equal(8, FieldSprites.WalkFrames);
        Assert.Equal(0, FieldSprites.WalkFrameAt(0f));
        Assert.Equal(4, FieldSprites.WalkFrameAt(0.5f));
        Assert.Equal(7, FieldSprites.WalkFrameAt(0.99f));
        Assert.Equal(1, FieldSprites.WalkFrameAt(3.13f));
        Assert.Equal("down_walk_3.png", FieldSprites.OverrideName(0, 3, walk: true));
        Assert.Equal("left_idle_1.png", FieldSprites.OverrideName(3, 1));
    }

    // ------------------------------------------------------------------ F3 · who follows, and where

    private static readonly LeadFollower.Place Open = new(TravelMode.OnFoot, false, false, false, false);

    [Fact]
    public void TheLeadWalksOnlyOnFootAndWhereNothingElseHoldsIt()
    {
        var walker = new Pokemon(PokemonDatabase.Get("Turtwig")!, 10);
        Assert.True(LeadFollower.MayWalk(true, walker, Open));
        Assert.False(LeadFollower.MayWalk(false, walker, Open));
        Assert.False(LeadFollower.MayWalk(true, null, Open));
        Assert.False(LeadFollower.MayWalk(true, walker, Open with { Mode = TravelMode.Surfing }));
        Assert.False(LeadFollower.MayWalk(true, walker, Open with { Mode = TravelMode.Cycling }));
        Assert.False(LeadFollower.MayWalk(true, walker, Open with { Mode = TravelMode.Diving }));
        Assert.False(LeadFollower.MayWalk(true, walker, Open with { Dark = true }));
        Assert.False(LeadFollower.MayWalk(true, walker, Open with { Underground = true }));
        Assert.False(LeadFollower.MayWalk(true, walker, Open with { AmitySquare = true }));
        Assert.False(LeadFollower.MayWalk(true, walker, Open with { SomeoneElse = true }));
    }

    [Fact]
    public void TheWalkerIsTheTeamsFirstThatCanStand()
    {
        var party = new Party();
        var egg = new Pokemon(PokemonDatabase.Get("Togepi")!, 1) { IsEgg = true };
        var down = new Pokemon(PokemonDatabase.Get("Starly")!, 10) { CurrentHP = 0 };
        var up = new Pokemon(PokemonDatabase.Get("Shinx")!, 10);
        party.Add(egg);
        party.Add(down);
        party.Add(up);
        Assert.Same(up, LeadFollower.Walker(party));
        // A swap puts another first
        party.Members.Remove(up);
        party.Members.Insert(0, up);
        Assert.Same(up, LeadFollower.Walker(party));
        // and a faint passes the walk on
        down.CurrentHP = down.MaxHP;
        up.CurrentHP = 0;
        Assert.Same(down, LeadFollower.Walker(party));
    }

    [Fact]
    public void AFollowerHopsTheLedgeThePlayerHopped()
    {
        var body = new NPC { Key = "lead", GridX = 5, GridY = 4 };
        var follower = new Follower(body, new Pokemon(PokemonDatabase.Get("Turtwig")!, 10));
        follower.Behind(5, 5, Direction.Down);
        Assert.Equal((5, 4), (body.GridX, body.GridY));
        // The player walks a tile, then hops a ledge two tiles on
        follower.PlayerLeft(5, 5, (5, 6));
        follower.PlayerLeft(5, 6, (5, 8));
        // and walks on from where they landed: the follower takes the ledge after them
        follower.PlayerLeft(5, 8, (5, 9));
        bool hopped = false, landed = false;
        float highest = 0f;
        for (int f = 0; f < 240 && !landed; f++)
        {
            follower.Update(1f / 60f, 4f);
            hopped |= follower.Hopping;
            landed = follower.KeepHopping(1f / 60f) && hopped;
            highest = MathF.Max(highest, body.Hop);
        }
        Assert.True(hopped && landed, "the follower never hopped");
        Assert.InRange(highest, 0.5f, 1f);
        Assert.Equal((5, 8), (body.GridX, body.GridY));
        Assert.Equal(0f, body.Hop);
    }

    // ------------------------------------------------------------------ F4 · spoken to

    [Fact]
    public void EachFriendshipBandHasItsOwnLineAndBubble()
    {
        Assert.Equal(new[] { 0, 0, 1, 1, 2, 3, 4, 4 }, new[] { 0, 49, 50, 99, 100, 150, 220, 255 }.Select(LeadFollower.BandOf));
        var lines = Enumerable.Range(0, 5).Select(b => Text.Of(LeadFollower.LineKey(b), ("name", "Turtwig"))).ToList();
        Assert.Equal(5, lines.Distinct().Count());
        Assert.All(lines, l => Assert.Contains("Turtwig", l));
        Assert.Equal(EmoteBubble.Dots, LeadFollower.BubbleOf(0));
        Assert.Equal(EmoteBubble.Heart, LeadFollower.BubbleOf(4));
    }

    [Fact]
    public void AFindIsDueOnlyAfterAWalkAndIsDrawnFromItsOwnTable()
    {
        Assert.All(LeadFollower.Finds, f => Assert.NotNull(ItemDatabase.Get(f.Item)));
        // Nothing before the walk, and no draw made
        var rng = new Random(5);
        Assert.Null(LeadFollower.Find(LeadFollower.FindSteps - 1, rng));
        Assert.Equal(new Random(5).Next(), rng.Next());
        // After it, one talk in three and from the table; the same seed finds the same
        var found = Enumerable.Range(0, 300).Select(k => LeadFollower.Find(LeadFollower.FindSteps, new Random(k))).ToList();
        Assert.InRange(found.Count(f => f != null), 60, 140);
        Assert.All(found.Where(f => f != null), f => Assert.Contains(LeadFollower.Finds, t => t.Item == f));
        Assert.Equal(LeadFollower.Find(999, new Random(42)), LeadFollower.Find(999, new Random(42)));
    }

    [Fact]
    public void TheFollowerFoundScriptGivesItsFind()
    {
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        var body = new NPC { Key = LeadFollower.BodyKey, NpcType = NPC.PokemonType, Species = "Turtwig" };
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.FollowerFound, "common")!, body, new[] { "Turtwig is holding something." }, ("Oran Berry", 1));
        runner.RunToEnd();
        Assert.Contains(host.Transcript, l => l.Text.Contains("holding something"));
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Oran Berry")!));
    }

    // ------------------------------------------------------------------ F5 · the back strike

    [Fact]
    public void AWildPokemonMetFromBehindLosesItsFirstTurnUnderTheModernRulesAlone()
    {
        Pokemon Mine() => Of(Gender.Male, "Turtwig", 20, "Growl");
        Pokemon Foe() => Of(Gender.Male, "Bidoof", 5, "Growl");
        BattleCore Met(Ruleset rules, bool surprised)
        {
            var party = new Party();
            party.Add(Mine());
            var core = new BattleCore(new CoreSetup
            {
                PlayerParty = party, WildPokemon = new List<Pokemon> { Foe() }, Random = Calm(), Rules = rules,
                Conditions = new BattleConditions { Surprised = surprised }, PlayerName = "Lucas"
            });
            core.Start();
            core.TakeLog();
            return core;
        }
        bool CaughtOffGuard(List<string> said) => said.Any(l => l.Contains("caught off guard"));

        var modern = Met(Ruleset.Modern, true);
        var first = Turn(modern);
        Assert.True(CaughtOffGuard(first));
        Assert.Single(first, l => l.Contains("used Growl"));
        // The second turn it moves
        var second = Turn(modern);
        Assert.False(CaughtOffGuard(second));
        Assert.Equal(2, second.Count(l => l.Contains("used Growl")));

        Assert.False(CaughtOffGuard(Turn(Met(Ruleset.Platinum, true))));
        Assert.False(CaughtOffGuard(Turn(Met(Ruleset.Modern, false))));
        Assert.True(FieldPokemon.FromBehind(Direction.Up, Direction.Up));
        Assert.False(FieldPokemon.FromBehind(Direction.Up, Direction.Down));
    }
}
