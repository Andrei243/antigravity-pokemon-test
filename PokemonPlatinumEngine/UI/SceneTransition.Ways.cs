using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Graphics;

namespace PokemonPlatinumEngine.UI;

/// <summary>What rides over the picture as a way into a battle closes.</summary>
public enum TransitionPieceKind
{
    /// <summary>A Poké Ball of our own drawing.</summary>
    Ball,
    /// <summary>The Battle Frontier's mark: a ring with a five-pointed star in it.</summary>
    FrontierMark,
    /// <summary>The Galactic mark, whole (<see cref="GalacticMark"/>).</summary>
    GalacticMark,
    /// <summary>One facet of the Galactic mark, on its way in.</summary>
    GalacticShard
}

/// <summary>
/// One thing riding over the picture: where its middle is on a screen of the size asked about, how large it is (a
/// ball's and a mark's width across; for a shard, the width the whole mark would have at the shard's size), how far
/// it is turned, in radians and clockwise as the screen shows it, which facet a shard is, how opaque it is, and
/// whether the dark closes over it rather than under it.
/// </summary>
public readonly record struct TransitionPiece(TransitionPieceKind Kind, Vector2 At, float Size, float Turn, int Part = -1, int Alpha = 255, bool Under = false);

// The ways into a battle by place and by whom (plan 09 · L13; style guide, "Life", "Into battle", "By place and by
// whom"): twelve more shapes of the dark, each our own after the beats of the original's (the functions of
// src/overlay005/encounter_effect_core.c named beside each), with what leads in and rides over the picture as the
// field closes. Everything here is a function of the kind and the time, as the first four ways are.
public static partial class SceneTransition
{
    /// <summary>How wide a Poké Ball is at its full size, and the two marks.</summary>
    public const float BallSize = 96f, FrontierMarkSize = 300f, GruntMarkSize = 300f, BossMarkSize = 420f;

    // The water's edge: a wave this long and this high, in blocks this wide and this tall
    private const float WaveLength = 320f, WaveHeight = 18f, WaveBlock = 6f, WaveStep = 3f;

    // A cave's toothed ring, and the lobes of the dark that wells out from under a ball on water
    private const int Teeth = 24, Lobes = 12;
    private const float ToothDepth = 0.2f, LobeDepth = 0.05f;

    // How far the lids' opening reaches past the picture's head and foot before they begin to shut
    private const float LidReach = 1.16f;

    // Three balls rising on water: where across the picture, and when in the closing each sets out and how long it
    // takes (EncounterEffect_Trainer_Water_HigherLevel: at 43, 215 and 129 of 256; the second five frames after the
    // first and the third three after that, six frames each, of fourteen)
    private static readonly float[] RiseAt = { 1f / 6f, 5f / 6f, 1f / 2f }, RiseFrom = { 0f, 0.36f, 0.57f };
    private const float RiseTakes = 0.43f;

    // Three balls falling in a cave (EncounterEffect_Trainer_Cave_HigherLevel: at 128, 208 and 48 of 256, a frame and
    // then three apart, five frames each); once they are through, their strips widen
    private static readonly float[] FallAt = { 1f / 2f, 13f / 16f, 3f / 16f }, FallFrom = { 0f, 0.08f, 0.2f };
    private const float FallTakes = 0.25f, FallsOver = 0.45f, StripHalf = 48f, StripTooth = 12f, ToothLength = 24f;

    private const int IrisBlades = 5;

    // The Galactic mark's six shards (EncounterEffect_GalacticGrunt: six sprites set out four, three, four, two,
    // three and three frames apart, each eight frames on its way, turning two, one, three, two, three and one turns):
    // which facet comes when, how long it takes, and how many turns it makes, clockwise or not
    private static readonly int[] ShardOrder = { 0, 3, 5, 2, 1, 4 }, ShardTurns = { 2, 1, -3, -2, -3, 1 };
    private static readonly float[] ShardFrom = { 0f, 0.11f, 0.24f, 0.32f, 0.43f, 0.54f };
    private const float ShardTakes = 0.3f;

    private static readonly Color BallRed = new(236, 64, 56, 255), BallWhite = new(250, 250, 252, 255);

    /// <summary>
    /// A way's lead and the closing of its dark, in seconds: about what the original takes, counted from its frames
    /// at thirty a second (its two flashes are sixteen or so; ours a quarter of a second).
    /// </summary>
    private static (float Lead, float Shut) Pace(TransitionKind kind) => kind switch
    {
        // EncounterEffect_Water_…: the shake alone for thirteen frames, then eight of fading
        TransitionKind.WildWater or TransitionKind.WildWaterStrong => (0.35f, 0.6f),
        // EncounterEffect_Cave_…: a circle closing in twelve frames
        TransitionKind.WildCave or TransitionKind.WildCaveStrong => (0f, 0.75f),
        // EncounterEffect_Trainer_Water_LowerLevel: the ball fades in turning for eight frames, then eight of shrinking
        TransitionKind.TrainerWater => (0.4f, 0.55f),
        // …_HigherLevel: six frames' wait, then the three balls over fourteen
        TransitionKind.TrainerWaterStrong => (0.2f, 0.9f),
        // EncounterEffect_Trainer_Cave_LowerLevel: the ball falls for twelve frames, then eight of closing
        TransitionKind.TrainerCave => (0.45f, 0.6f),
        // …_HigherLevel: the three balls over ten frames, then the wipe
        TransitionKind.TrainerCaveStrong => (0f, 1.2f),
        // EncounterEffect_Double: four frames of pieces, eight of closing
        TransitionKind.Double => (0f, 0.8f),
        // EncounterEffect_Frontier: the mark fades in over twelve frames, six of shrinking
        TransitionKind.Frontier => (0.45f, 0.55f),
        // EncounterEffect_GalacticGrunt: the shards over thirty-three frames, twelve of closing
        TransitionKind.Grunt => (0f, 1.75f),
        // EncounterEffect_GalacticBoss: the mark fades in over fifteen frames, stands for sixteen, sixteen of closing
        TransitionKind.GalacticBoss => (1f, 0.85f),
        // EncounterEffect_GymLeader: the banner wiped in over six frames, ten of waiting, the mark's sixteen, the
        // portrait's four, ten more, three up to white and three back, twenty-six held (eighty or so); fifteen of white
        TransitionKind.LeaderBand => (2.7f, 0.5f),
        // EncounterEffect_EliteFourChampion: the two portraits' six frames, the mark and the sparks behind it (thirty or
        // so in all), three up to white and six back, eight held, thirty-two of trembling (nine for the Champion, which
        // is not told apart here); eight of white
        TransitionKind.EliteBand => (2.6f, 0.3f),
        _ => (0f, CloseSeconds - FlashSeconds)
    };

    /// <summary>The ways in which several things come one after another close at an even pace; the others with an ease.</summary>
    private static bool EvenPace(TransitionKind kind) =>
        kind is TransitionKind.TrainerWaterStrong or TransitionKind.TrainerCaveStrong or TransitionKind.Grunt;

    private static bool OnWater(TransitionKind kind) => kind is TransitionKind.WildWater or TransitionKind.WildWaterStrong
        or TransitionKind.TrainerWater or TransitionKind.TrainerWaterStrong;

    private static bool WaversFurther(TransitionKind kind) => kind is TransitionKind.WildWaterStrong or TransitionKind.TrainerWaterStrong;

    /// <summary>How far through its lead a way is, 0 to 1, <paramref name="seconds"/> into closing: 1 from the end of the flashes for a way with none.</summary>
    public static float Lead(TransitionKind kind, float seconds)
    {
        if (seconds < FlashSeconds) return 0f;
        float lead = LeadSeconds(kind);
        return lead <= 0f ? 1f : Math.Clamp((seconds - FlashSeconds) / lead, 0f, 1f);
    }

    /// <summary>
    /// How the field under a way into a battle wavers <paramref name="seconds"/> into its closing (the scene
    /// effects' ripple, which the field's own composite reads): on water, from the end of the flashes until the
    /// dark is shut, a texel further every tenth of a second up to four, or six for a stronger foe, the wave
    /// travelling two lengths a second, or three. None on any other ground, and none as the battle opens.
    /// </summary>
    public static SceneRipple Ripple(TransitionKind kind, bool closing, float seconds)
    {
        if (!closing || !OnWater(kind) || seconds < FlashSeconds || seconds >= OutSeconds(kind)) return default;
        bool further = WaversFurther(kind);
        int reach = Math.Min(further ? 6 : 4, 1 + (int)((seconds - FlashSeconds) / 0.1f));
        float phase = MathF.Floor(seconds * 30f) / 30f * MathF.Tau * (further ? 3f : 2f);
        return new SceneRipple(reach, phase);
    }

    /// <summary>
    /// How dark the veil over the field is, 0 to 255, <paramref name="seconds"/> into closing: the boss's way dims
    /// the picture in three steps as the mark rises, a sixth of a second apart, and keeps it so.
    /// </summary>
    public static int Veil(TransitionKind kind, bool closing, float seconds)
    {
        if (!closing || kind != TransitionKind.GalacticBoss || seconds < FlashSeconds) return 0;
        float since = seconds - FlashSeconds;
        return since < 1f / 6f ? 70 : since < 1f / 3f ? 130 : 180;
    }

    private static float Share(float closed, float from, float takes) => Math.Clamp((closed - from) / takes, 0f, 1f);

    /// <summary>A wave between 0 and 1 with a point at every half: 0 at whole numbers, 1 between them.</summary>
    private static float Sawtooth(float along) => 1f - MathF.Abs(2f * (along - MathF.Floor(along)) - 1f);

    private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

    /// <summary>How far the furthest corner of the screen is from a point of it.</summary>
    private static float Reach(Vector2 from, float width, float height) =>
        MathF.Sqrt(MathF.Pow(MathF.Max(from.X, width - from.X), 2f) + MathF.Pow(MathF.Max(from.Y, height - from.Y), 2f));

    /// <summary>
    /// The water's edge at <paramref name="x"/> for a line resting at <paramref name="rest"/>: a sine that travels
    /// as the dark closes, cut into blocks.
    /// </summary>
    private static float WaveEdge(float x, float rest, float closed, float turned)
    {
        float block = MathF.Floor(x / WaveBlock) * WaveBlock + WaveBlock / 2f;
        float wave = WaveHeight * MathF.Sin(MathF.Tau * (block / WaveLength + closed * 2f) + turned);
        return MathF.Round((rest + wave) / WaveStep) * WaveStep;
    }

    /// <summary>The sixth of the picture a point lies in, counted clockwise from the one to the right of straight up: the Galactic mark's facets in their order.</summary>
    private static int WedgeOf(float dx, float dy)
    {
        float degrees = MathF.Atan2(dy, dx) * 180f / MathF.PI + 90f;
        if (degrees < 0f) degrees += 360f;
        return Math.Clamp((int)(degrees / 60f), 0, GalacticMark.Facets - 1);
    }

    /// <summary>How far the shard of a facet has come, 0 to 1.</summary>
    private static float ShardCome(int facet, float closed) => Share(closed, ShardFrom[Array.IndexOf(ShardOrder, facet)], ShardTakes);

    /// <summary>How wide half a strip of the cave's dark is, the dark being this far closed: a ball's width until the three are through, then widening until the strips meet.</summary>
    private static float StripWidth(float closed, float width)
    {
        if (closed <= FallsOver) return StripHalf;
        float meet = width * 3f / 16f + StripTooth + 1f;
        return StripHalf + (closed - FallsOver) / (1f - FallsOver) * (meet - StripHalf);
    }

    /// <summary>Whether a point is under the dark of one of the ways by place and by whom, <paramref name="closed"/> between 0 and 1.</summary>
    private static bool CoversByWay(TransitionKind kind, float closed, float x, float y, float width, float height, Vector2? focus)
    {
        float cx = width / 2f, cy = height / 2f;
        float dx = x - cx, dy = y - cy;
        var middle = new Vector2(cx, cy);
        switch (kind)
        {
            case TransitionKind.WildWater:
                return y >= WaveEdge(x, height * (1f - closed), closed, 0f);
            case TransitionKind.WildWaterStrong:
                return y >= WaveEdge(x, height - closed * cy, closed, 0f) || y < WaveEdge(x, closed * cy, closed, MathF.PI);
            case TransitionKind.WildCave:
            {
                var on = focus ?? middle;
                return Vector2.Distance(new Vector2(x, y), on) > (1f - closed) * Reach(on, width, height);
            }
            case TransitionKind.WildCaveStrong:
            {
                var on = focus ?? middle;
                float tooth = Sawtooth(MathF.Atan2(y - on.Y, x - on.X) / MathF.Tau * Teeth);
                float edge = (1f - closed) * Reach(on, width, height) / (1f - ToothDepth) * (1f - ToothDepth * tooth);
                return Vector2.Distance(new Vector2(x, y), on) > edge;
            }
            case TransitionKind.TrainerWater:
            {
                float rim = closed * Reach(middle, width, height) / (1f - LobeDepth)
                    * (1f + LobeDepth * MathF.Sin(Lobes * MathF.Atan2(dy, dx) + closed * MathF.PI));
                return MathF.Sqrt(dx * dx + dy * dy) < rim;
            }
            case TransitionKind.TrainerWaterStrong:
            {
                // The left third is the first ball's, the right the second's, the middle the last's
                int ball = x < width / 3f ? 0 : x < width * 2f / 3f ? 2 : 1;
                float risen = Share(closed, RiseFrom[ball], RiseTakes);
                return risen > 0f && y > height * (1f - risen);
            }
            case TransitionKind.TrainerCave:
            {
                // What is left open is an ellipse twice as wide as the picture, flattening to a line
                float tall = (1f - closed) * LidReach * cy;
                float u = dx / width, v = dy / tall;
                return u * u + v * v > 1f;
            }
            case TransitionKind.TrainerCaveStrong:
            {
                float half = StripWidth(closed, width) - StripTooth * Sawtooth(y / ToothLength);
                for (int i = 0; i < FallAt.Length; i++)
                {
                    float fallen = Share(closed, FallFrom[i], FallTakes);
                    if (fallen <= 0f) continue;
                    float foot = -BallSize / 2f + fallen * (height + BallSize);
                    if (y < foot && MathF.Abs(x - FallAt[i] * width) < half) return true;
                }
                return false;
            }
            case TransitionKind.Double:
                return MathF.Abs(dx) > cx * (1f - closed) && MathF.Abs(dy) > cy * (1f - closed);
            case TransitionKind.Frontier:
            {
                float apothem = (1f - closed) * Reach(middle, width, height);
                float turned = -MathF.PI / 2f + closed * MathF.Tau / IrisBlades;
                for (int k = 0; k < IrisBlades; k++)
                {
                    float a = turned + k * MathF.Tau / IrisBlades;
                    if (dx * MathF.Cos(a) + dy * MathF.Sin(a) > apothem) return true;
                }
                return false;
            }
            case TransitionKind.Grunt:
            {
                float come = ShardCome(WedgeOf(dx, dy), closed);
                return come > 0f && MathF.Sqrt(dx * dx + dy * dy) > (1f - come) * Reach(middle, width, height);
            }
            case TransitionKind.GalacticBoss:
                return MathF.Abs(dx) + MathF.Abs(dy) < closed * (cx + cy) * 1.001f;
            case TransitionKind.LeaderBand:
                // The white: a bar widening from the band's middle row to the head and the foot of the picture
                return MathF.Abs(dy) < closed * cy * 1.001f;
            case TransitionKind.EliteBand:
                // The white: a slash along the rising diagonal, widening to the two corners the portraits fly to
                return MathF.Abs(dx / cx + dy / cy) < closed * 2.002f;
            default:
                return false;
        }
    }

    /// <summary>
    /// What rides over the picture <paramref name="seconds"/> into the closing of a way into a battle, added to
    /// <paramref name="into"/> in the order it is drawn: nothing during the flashes, nothing for the first four
    /// ways, and nothing as the battle opens, which is the dark's shape alone. (What a band shows is not pieces
    /// but one look, <see cref="BandAt"/>.)
    /// </summary>
    public static void Pieces(TransitionKind kind, float seconds, float width, float height, List<TransitionPiece> into)
    {
        if (Plain(kind) || IsBand(kind) || seconds < FlashSeconds || seconds >= OutSeconds(kind)) return;
        float lead = Lead(kind, seconds), closed = Closed(kind, true, seconds), since = seconds - FlashSeconds;
        var middle = new Vector2(width / 2f, height / 2f);
        switch (kind)
        {
            case TransitionKind.TrainerWater:
                // It grows as it spins in, one turn for the lead, and turns on at that pace until all is dark
                into.Add(new TransitionPiece(TransitionPieceKind.Ball, middle, BallSize * EaseOut(lead), MathF.Tau * since / LeadSeconds(kind)));
                break;
            case TransitionKind.TrainerWaterStrong:
                for (int i = 0; i < RiseAt.Length; i++)
                {
                    float risen = Share(closed, RiseFrom[i], RiseTakes);
                    if (risen > 0f && risen < 1f)
                        into.Add(new TransitionPiece(TransitionPieceKind.Ball, new Vector2(RiseAt[i] * width, height * (1f - risen)), BallSize, MathF.Tau * risen));
                }
                break;
            case TransitionKind.TrainerCave:
                // It falls faster as it falls, from just above the picture, and then shrinks away between the lids
                if (closed <= 0f)
                    into.Add(new TransitionPiece(TransitionPieceKind.Ball, new Vector2(middle.X, -12f + (middle.Y + 12f) * lead * lead), 24f + (BallSize - 24f) * lead, MathF.Tau * lead));
                else
                    into.Add(new TransitionPiece(TransitionPieceKind.Ball, middle, BallSize * (1f - closed), 0f));
                break;
            case TransitionKind.TrainerCaveStrong:
                for (int i = 0; i < FallAt.Length; i++)
                {
                    float fallen = Share(closed, FallFrom[i], FallTakes);
                    // (The second turns the other way)
                    if (fallen > 0f && fallen < 1f)
                        into.Add(new TransitionPiece(TransitionPieceKind.Ball, new Vector2(FallAt[i] * width, -BallSize / 2f + fallen * (height + BallSize)), BallSize, MathF.Tau * fallen * (i == 1 ? -1f : 1f)));
                }
                break;
            case TransitionKind.Frontier:
                into.Add(new TransitionPiece(TransitionPieceKind.FrontierMark, middle, FrontierMarkSize * (1f - closed), closed * MathF.Tau / IrisBlades,
                    Alpha: since < 0.1f ? 90 : since < 0.2f ? 170 : 255));
                break;
            case TransitionKind.Grunt:
            {
                // Once the last has come the mark stands whole; until then each shard is on its way or in its place
                if (closed >= ShardFrom[^1] + ShardTakes)
                {
                    into.Add(new TransitionPiece(TransitionPieceKind.GalacticMark, middle, GruntMarkSize, 0f));
                    break;
                }
                float reach = Reach(middle, width, height);
                for (int k = 0; k < ShardOrder.Length; k++)
                {
                    float come = Share(closed, ShardFrom[k], ShardTakes);
                    if (come <= 0f) continue;
                    int facet = ShardOrder[k];
                    // From the rim along the middle of its sixth of the picture, to its place in the mark
                    float around = (-60f + 60f * facet) * MathF.PI / 180f;
                    var rim = middle + new Vector2(MathF.Cos(around), MathF.Sin(around)) * reach;
                    var place = middle + GalacticMark.MiddleOf(facet) * (GruntMarkSize / 2f);
                    into.Add(new TransitionPiece(TransitionPieceKind.GalacticShard, Vector2.Lerp(rim, place, come), GruntMarkSize * (3f - 2f * come),
                        ShardTurns[k] * MathF.Tau * (1f - come), Part: facet));
                }
                break;
            }
            case TransitionKind.GalacticBoss:
                // It rises in half a second and stands; its own core will swallow it
                into.Add(new TransitionPiece(TransitionPieceKind.GalacticMark, middle, BossMarkSize * EaseOut(MathF.Min(1f, since / 0.5f)), 0f, Under: true));
                break;
        }
    }

    // ------------------------------------------------------------------ drawing

    private static readonly List<TransitionPiece> shown = new();

    /// <summary>A way by place or by whom as the field closes: the veil, what the dark closes over, the dark, and what rides on it.</summary>
    private static void DrawWay(TransitionKind kind, float seconds, float closed, int width, int height, Vector2? focus)
    {
        if (closed >= 1f)
        {
            Raylib.DrawRectangle(0, 0, width, height, Dark);
            return;
        }
        if (Veil(kind, true, seconds) is > 0 and var veil)
            Raylib.DrawRectangle(0, 0, width, height, new Color(Dark.R, Dark.G, Dark.B, (byte)veil));
        shown.Clear();
        Pieces(kind, seconds, width, height, shown);
        foreach (var piece in shown)
            if (piece.Under) DrawPiece(piece);
        if (closed > 0f) DrawDark(kind, closed, width, height, focus);
        foreach (var piece in shown)
            if (!piece.Under) DrawPiece(piece);
    }

    private static void Tri(Vector2 a, Vector2 b, Vector2 c, Color colour)
    {
        // The order raylib wants: counter-clockwise as the screen shows it
        float turn = (b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y);
        if (turn < 0f) Raylib.DrawTriangle(a, b, c, colour);
        else Raylib.DrawTriangle(a, c, b, colour);
    }

    private static void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color colour)
    {
        Tri(a, b, c, colour);
        Tri(a, c, d, colour);
    }

    private static Vector2 Around(Vector2 middle, float radius, float angle) => middle + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;

    /// <summary>The dark of one of the ways by place and by whom, <paramref name="closed"/> between 0 and 1: the shapes <see cref="CoversByWay"/> answers for.</summary>
    private static void DrawDark(TransitionKind kind, float closed, int width, int height, Vector2? focus)
    {
        float cx = width / 2f, cy = height / 2f;
        var middle = new Vector2(cx, cy);
        switch (kind)
        {
            case TransitionKind.WildWater or TransitionKind.WildWaterStrong:
            {
                bool both = kind == TransitionKind.WildWaterStrong;
                for (float x = 0f; x < width; x += WaveBlock)
                {
                    float foot = WaveEdge(x, both ? height - closed * cy : height * (1f - closed), closed, 0f);
                    Raylib.DrawRectangleRec(new Rectangle(x, foot, WaveBlock, height - foot), Dark);
                    if (!both) continue;
                    float head = WaveEdge(x, closed * cy, closed, MathF.PI);
                    if (head > 0f) Raylib.DrawRectangleRec(new Rectangle(x, 0f, WaveBlock, head), Dark);
                }
                break;
            }
            case TransitionKind.WildCave:
            {
                var on = focus ?? middle;
                float reach = Reach(on, width, height);
                Raylib.DrawRing(on, (1f - closed) * reach, reach + 8f, 0f, 360f, 96, Dark);
                break;
            }
            case TransitionKind.WildCaveStrong:
            {
                var on = focus ?? middle;
                float reach = Reach(on, width, height), open = (1f - closed) * reach / (1f - ToothDepth);
                for (int j = 0; j < Teeth * 2; j++)
                {
                    // Between two teeth the edge is at its widest; a tooth's tip reaches a fifth further in
                    float a0 = j * MathF.PI / Teeth, a1 = (j + 1) * MathF.PI / Teeth;
                    float r0 = open * (j % 2 == 0 ? 1f : 1f - ToothDepth), r1 = open * (j % 2 == 0 ? 1f - ToothDepth : 1f);
                    Quad(Around(on, r0, a0), Around(on, r1, a1), Around(on, reach * 1.4f, a1), Around(on, reach * 1.4f, a0), Dark);
                }
                break;
            }
            case TransitionKind.TrainerWater:
            {
                const int steps = 96;
                float full = closed * Reach(middle, width, height) / (1f - LobeDepth);
                for (int j = 0; j < steps; j++)
                {
                    float a0 = j * MathF.Tau / steps, a1 = (j + 1) * MathF.Tau / steps;
                    float r0 = full * (1f + LobeDepth * MathF.Sin(Lobes * a0 + closed * MathF.PI));
                    float r1 = full * (1f + LobeDepth * MathF.Sin(Lobes * a1 + closed * MathF.PI));
                    Tri(middle, Around(middle, r0, a0), Around(middle, r1, a1), Dark);
                }
                break;
            }
            case TransitionKind.TrainerWaterStrong:
            {
                float third = width / 3f;
                for (int column = 0; column < 3; column++)
                {
                    float risen = Share(closed, RiseFrom[column == 0 ? 0 : column == 1 ? 2 : 1], RiseTakes);
                    if (risen > 0f) Raylib.DrawRectangleRec(new Rectangle(column * third, height * (1f - risen), third + 1f, height * risen + 1f), Dark);
                }
                break;
            }
            case TransitionKind.TrainerCave:
            {
                const int steps = 96;
                float tall = (1f - closed) * LidReach * cy;
                float Lid(float x) => tall * MathF.Sqrt(MathF.Max(0f, 1f - (x - cx) / width * ((x - cx) / width)));
                for (int j = 0; j < steps; j++)
                {
                    float x0 = j * (float)width / steps, x1 = (j + 1) * (float)width / steps;
                    float h0 = Lid(x0), h1 = Lid(x1);
                    Quad(new Vector2(x0, 0f), new Vector2(x1, 0f), new Vector2(x1, cy - h1), new Vector2(x0, cy - h0), Dark);
                    Quad(new Vector2(x0, cy + h0), new Vector2(x1, cy + h1), new Vector2(x1, height), new Vector2(x0, height), Dark);
                }
                break;
            }
            case TransitionKind.TrainerCaveStrong:
            {
                float wide = StripWidth(closed, width);
                for (int i = 0; i < FallAt.Length; i++)
                {
                    float fallen = Share(closed, FallFrom[i], FallTakes);
                    if (fallen <= 0f) continue;
                    float foot = MathF.Min(height, -BallSize / 2f + fallen * (height + BallSize)), x = FallAt[i] * width;
                    for (float y0 = 0f; y0 < foot; y0 += ToothLength / 2f)
                    {
                        float y1 = MathF.Min(foot, y0 + ToothLength / 2f);
                        float w0 = wide - StripTooth * Sawtooth(y0 / ToothLength), w1 = wide - StripTooth * Sawtooth(y1 / ToothLength);
                        Quad(new Vector2(x - w0, y0), new Vector2(x + w0, y0), new Vector2(x + w1, y1), new Vector2(x - w1, y1), Dark);
                    }
                }
                break;
            }
            case TransitionKind.Double:
            {
                float w = cx * closed, h = cy * closed;
                Raylib.DrawRectangleRec(new Rectangle(0f, 0f, w, h), Dark);
                Raylib.DrawRectangleRec(new Rectangle(width - w, 0f, w, h), Dark);
                Raylib.DrawRectangleRec(new Rectangle(0f, height - h, w, h), Dark);
                Raylib.DrawRectangleRec(new Rectangle(width - w, height - h, w, h), Dark);
                break;
            }
            case TransitionKind.Frontier:
            {
                float reach = Reach(middle, width, height), apothem = (1f - closed) * reach, far = reach * 3f;
                float turned = -MathF.PI / 2f + closed * MathF.Tau / IrisBlades;
                for (int k = 0; k < IrisBlades; k++)
                {
                    // A blade: everything beyond one side of the opening
                    float a = turned + k * MathF.Tau / IrisBlades;
                    var outward = new Vector2(MathF.Cos(a), MathF.Sin(a));
                    var side = new Vector2(-outward.Y, outward.X);
                    var edge = middle + outward * apothem;
                    Quad(edge - side * far, edge + side * far, edge + side * far + outward * far, edge - side * far + outward * far, Dark);
                }
                break;
            }
            case TransitionKind.Grunt:
            {
                float reach = Reach(middle, width, height);
                for (int facet = 0; facet < GalacticMark.Facets; facet++)
                {
                    float come = ShardCome(facet, closed);
                    if (come > 0f) Raylib.DrawRing(middle, (1f - come) * reach, reach + 8f, -90f + 60f * facet, -30f + 60f * facet, 16, Dark);
                }
                break;
            }
            case TransitionKind.GalacticBoss:
            {
                float r = closed * (cx + cy) * 1.001f;
                Tri(new Vector2(cx, cy - r), new Vector2(cx - r, cy), new Vector2(cx + r, cy), Dark);
                Tri(new Vector2(cx - r, cy), new Vector2(cx, cy + r), new Vector2(cx + r, cy), Dark);
                break;
            }
            case TransitionKind.LeaderBand:
            {
                float half = closed * cy * 1.001f;
                Raylib.DrawRectangleRec(new Rectangle(0f, cy - half, width, 2f * half), White);
                break;
            }
            case TransitionKind.EliteBand:
            {
                // Between the two lines dx / cx + dy / cy = ±2·closed, each running up and to the right
                var along = new Vector2(cx, -cy) * 3f;
                var aside = new Vector2(closed * 2.002f * cx, 0f);
                Quad(middle - aside - along, middle - aside + along, middle + aside + along, middle + aside - along, White);
                break;
            }
        }
    }

    private static void DrawPiece(TransitionPiece piece)
    {
        if (piece.Size < 4f) return;
        switch (piece.Kind)
        {
            case TransitionPieceKind.Ball: DrawBall(piece.At, piece.Size, piece.Turn); break;
            case TransitionPieceKind.FrontierMark: DrawFrontierMark(piece.At, piece.Size, piece.Turn, piece.Alpha); break;
            case TransitionPieceKind.GalacticMark: DrawGalacticMark(piece.At, piece.Size); break;
            case TransitionPieceKind.GalacticShard: DrawGalacticShard(piece.Part, piece.At, piece.Size, piece.Turn); break;
        }
    }

    /// <summary>Our own Poké Ball: red over white, a band and a rim of the dark, a white button ringed in the dark.</summary>
    private static void DrawBall(Vector2 c, float size, float turn)
    {
        float r = size / 2f, inner = r - MathF.Max(2f, r * 0.09f), degrees = turn * 180f / MathF.PI;
        Raylib.DrawCircleSector(c, r, 0f, 360f, 48, Dark);
        Raylib.DrawCircleSector(c, inner, degrees + 180f, degrees + 360f, 24, BallRed);
        Raylib.DrawCircleSector(c, inner, degrees, degrees + 180f, 24, BallWhite);
        var along = new Vector2(MathF.Cos(turn), MathF.Sin(turn));
        Raylib.DrawLineEx(c - along * inner, c + along * inner, MathF.Max(2f, r * 0.16f), Dark);
        Raylib.DrawCircleSector(c, r * 0.34f, 0f, 360f, 24, Dark);
        Raylib.DrawCircleSector(c, r * 0.2f, 0f, 360f, 24, BallWhite);
    }

    /// <summary>The Battle Frontier's mark: a ring in the print's gold with a five-pointed star in it, rimmed in the dark.</summary>
    private static void DrawFrontierMark(Vector2 c, float size, float turn, int alpha)
    {
        float r = size / 2f, line = MathF.Max(2f, r * 0.04f);
        var gold = new Color(232, 180, 60, alpha);
        var rim = new Color(Dark.R, Dark.G, Dark.B, (byte)alpha);
        Raylib.DrawRing(c, r * 0.82f - line, r + line, 0f, 360f, 64, rim);
        Raylib.DrawRing(c, r * 0.82f, r, 0f, 360f, 64, gold);
        Star(c, r * 0.62f + line * 1.6f, turn, rim);
        Star(c, r * 0.62f, turn, gold);
    }

    private static void Star(Vector2 c, float radius, float turn, Color colour)
    {
        Span<Vector2> points = stackalloc Vector2[10];
        for (int i = 0; i < 10; i++)
            points[i] = Around(c, i % 2 == 0 ? radius : radius * 0.4f, turn - MathF.PI / 2f + i * MathF.PI / 5f);
        // Each point with the two notches beside it, then the five-sided middle
        for (int i = 0; i < 10; i += 2) Tri(points[i], points[(i + 1) % 10], points[(i + 9) % 10], colour);
        for (int i = 1; i < 10; i += 2) Tri(c, points[i], points[(i + 2) % 10], colour);
    }

    private static Vector2 Turned(Vector2 p, float turn)
    {
        float cos = MathF.Cos(turn), sin = MathF.Sin(turn);
        return new Vector2(p.X * cos - p.Y * sin, p.X * sin + p.Y * cos);
    }

    /// <summary>The Galactic mark whole, <paramref name="size"/> across: its facets in their shades, the seams and the rim in the dark, the core and its spark.</summary>
    private static void DrawGalacticMark(Vector2 c, float size)
    {
        float half = size / 2f, line = MathF.Max(2f, 2f * GalacticMark.SeamHalf * half);
        Span<Vector2> corners = stackalloc Vector2[4];
        for (int facet = 0; facet < GalacticMark.Facets; facet++)
        {
            var shape = GalacticMark.Facet(facet);
            for (int i = 0; i < shape.Length; i++) corners[i] = c + shape[i] * half;
            for (int i = 1; i + 1 < shape.Length; i++) Tri(corners[0], corners[i], corners[i + 1], GalacticMark.ShadeOf(facet));
        }
        // A seam from the middle to the first corner of each facet's rim, and the rim round the four points
        for (int facet = 0; facet < GalacticMark.Facets; facet++)
            Raylib.DrawLineEx(c, c + GalacticMark.Facet(facet)[1] * half, line, Dark);
        Span<Vector2> points = stackalloc Vector2[] { new(0f, -1f), new(1f, 0f), new(0f, 1f), new(-1f, 0f) };
        for (int i = 0; i < 4; i++)
        {
            Raylib.DrawLineEx(c + points[i] * half, c + points[(i + 1) % 4] * half, line * 1.5f, Dark);
            Raylib.DrawCircleSector(c + points[i] * half, line * 0.75f, 0f, 360f, 12, Dark);
        }
        Lozenge(c, GalacticMark.CoreReach * half, Dark);
        Lozenge(c, GalacticMark.HeartReach * half, GalacticMark.Teal);
    }

    private static void Lozenge(Vector2 c, float reach, Color colour)
    {
        Tri(new Vector2(c.X, c.Y - reach), new Vector2(c.X - reach, c.Y), new Vector2(c.X + reach, c.Y), colour);
        Tri(new Vector2(c.X - reach, c.Y), new Vector2(c.X, c.Y + reach), new Vector2(c.X + reach, c.Y), colour);
    }

    /// <summary>
    /// One facet of the mark on its way in: its middle at <paramref name="at"/>, as large as it would be in a mark
    /// <paramref name="size"/> across, turned about its own middle, with a rim of the dark.
    /// </summary>
    private static void DrawGalacticShard(int facet, Vector2 at, float size, float turn)
    {
        float half = size / 2f, line = MathF.Max(2f, 3f * GalacticMark.SeamHalf * half);
        var shape = GalacticMark.Facet(facet);
        var own = GalacticMark.MiddleOf(facet);
        Span<Vector2> corners = stackalloc Vector2[shape.Length];
        for (int i = 0; i < shape.Length; i++) corners[i] = at + Turned((shape[i] - own) * half, turn);
        for (int i = 1; i + 1 < shape.Length; i++) Tri(corners[0], corners[i], corners[i + 1], GalacticMark.ShadeOf(facet));
        for (int i = 0; i < shape.Length; i++)
        {
            Raylib.DrawLineEx(corners[i], corners[(i + 1) % shape.Length], line, Dark);
            Raylib.DrawCircleSector(corners[i], line / 2f, 0f, 360f, 12, Dark);
        }
    }
}
