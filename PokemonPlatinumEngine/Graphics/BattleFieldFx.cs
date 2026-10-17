using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// What lies on the field while it lasts (plan 25 · Y2; style guide, "What lies on the field"): caltrops and
/// hovering stones on the side they were scattered on, panes and veils on the side that put them up, Trick Room's
/// grid and Gravity's sinking dust over the whole field, a star and a mote over the places a Wish and an attack sent
/// ahead wait for. All of it is our own: Platinum draws these only as their moves are used.
/// <para>
/// Everything is small and low and sits in the scene (depth-tested), so a Pokémon's body hides what is behind it;
/// what stands up keeps off the arc of a platform's rim that faces the camera, and the panes, which do stand there,
/// stay under a ninth of the platform's radius, so nothing comes over a body. Each look is a pure
/// function of its <see cref="ConditionView"/> (its layers, ages and seed) and the battle's clock: nothing is
/// simulated, and nothing draws on the battle's chance. GPU-free.
/// </para>
/// </summary>
internal static class BattleFieldFx
{
    /// <summary>Half the arc of a platform's rim that faces the camera, where nothing stands up (radians).</summary>
    public const float CameraArc = 65f * MathF.PI / 180f;

    /// <summary>Half the arc of the rim a pane stands along, facing the other side.</summary>
    public const float PaneArc = 50f * MathF.PI / 180f;

    /// <summary>The rings of caltrops, in the platform's radius: Spikes' three, and Toxic Spikes' two between them.</summary>
    public static readonly float[] SpikeRings = { 0.95f, 0.84f, 0.73f }, ToxicRings = { 0.895f, 0.785f };

    public const int CaltropsPerRing = 10, Stones = 5, GridLines = 7;

    /// <summary>How high a pane stands over its platform, in the platform's radius: Reflect from the top, Light Screen on it.</summary>
    public const float ReflectTop = 0.05f, LightScreenFoot = 0.055f, LightScreenTop = 0.105f;

    /// <summary>
    /// The highest anything stands on the arc of a rim that faces the camera, over the platform's top and in its
    /// radius: the panes' upper edge. What stands taller (the stones, the charms) keeps to the rest of the rim.
    /// </summary>
    public const float TallestBeforeTheCamera = LightScreenTop;

    /// <summary>Trick Room's grid: the ground it covers, and how high its walls rise.</summary>
    public static readonly Vector2 GridMin = new(-13f, -7f), GridMax = new(6f, 21f);
    public const float WallHeight = 3.2f;

    // The Ring shape's circle is this much smaller than the quad it is drawn on
    private const float RingScale = 1.143f / 0.82f;

    private static Color Rgba(int r, int g, int b, float a = 255f) => new(r, g, b, (int)Math.Clamp(a, 0f, 255f));

    private static float Frac(float v) => v - MathF.Floor(v);

    // ------------------------------------------------------------------ the caps

    /// <summary>The most quads and ribbons a side's look ever draws (a test holds each to it at every age).</summary>
    public static int Cap(SideCondition what) => what switch
    {
        SideCondition.Spikes => SpikeRings.Length * CaltropsPerRing * 2,
        SideCondition.ToxicSpikes => ToxicRings.Length * CaltropsPerRing * 2 + 3,
        SideCondition.StealthRock => Stones * 3,
        SideCondition.Reflect or SideCondition.LightScreen => 3,
        SideCondition.Safeguard => 7,
        SideCondition.Mist => 16,
        SideCondition.Tailwind => 10,
        SideCondition.LuckyChant => 9,
        SideCondition.AuroraVeil => 7,
        SideCondition.StickyWeb => WebSpokes + WebRings + 3,
        // What two Pledges leave (plan 06 · R25's leftovers)
        SideCondition.Rainbow => RainbowBands,
        SideCondition.SeaOfFire => 2 + Flames,
        SideCondition.Swamp => 2 + SwampBubbles * 2,
        _ => 0
    };

    /// <summary>The pieces of what two Pledges leave: a rainbow's bands, a sea of fire's tongues, a swamp's bubbles.</summary>
    public const int RainbowBands = 5, Flames = 6, SwampBubbles = 3;

    public static int Cap(FieldCondition what) => what switch
    {
        FieldCondition.TrickRoom => GridLines * 2 + 3 * (GridLines + 2),
        FieldCondition.Gravity => 16,
        // The terrains, on both platforms: a wash each and their pieces (plan 06 · R19b)
        FieldCondition.ElectricTerrain => 2 * (2 + Sparks),
        FieldCondition.GrassyTerrain => 2 * (2 + Blades),
        FieldCondition.MistyTerrain => 2 * (2 + Wisps + 2),
        FieldCondition.PsychicTerrain => 2 * (2 + 2 + Motes),
        // Wonder Room's shields and Magic Room's motes, three on each platform (plan 06 · R25)
        FieldCondition.WonderRoom or FieldCondition.MagicRoom => 2 * RoomPieces,
        _ => 0
    };

    /// <summary>The pieces of the later games' looks (plan 06 · R19b): Sticky Web's threads, and each terrain's on a platform.</summary>
    public const int WebSpokes = 8, WebRings = 2, Sparks = 6, Blades = 9, Wisps = 5, Motes = 4;

    /// <summary>Wonder Room's shields and Magic Room's motes on a platform (plan 06 · R25).</summary>
    public const int RoomPieces = 3;

    public static int Cap(PlaceCondition what) => what switch
    {
        PlaceCondition.Wish => 3,
        PlaceCondition.FutureSight => 3,
        PlaceCondition.DoomDesire => 4,
        _ => 0
    };

    /// <summary>The most a whole field ever draws: everything on both sides, both rooms, and both waits over all four places.</summary>
    public static int MaxQuads
    {
        get
        {
            int total = 0;
            foreach (var kind in Enum.GetValues<SideCondition>()) total += 2 * Cap(kind);
            // The field has one terrain at a time: the heaviest of the four counts
            foreach (var kind in Enum.GetValues<FieldCondition>()) if (!FieldState.IsTerrain(kind)) total += Cap(kind);
            int terrain = 0;
            foreach (var kind in Enum.GetValues<FieldCondition>()) if (FieldState.IsTerrain(kind)) terrain = Math.Max(terrain, Cap(kind));
            total += terrain;
            // A place waits for one attack at a time: Doom Desire's is the heavier of the two
            total += 4 * (Cap(PlaceCondition.Wish) + Cap(PlaceCondition.DoomDesire));
            return total;
        }
    }

    // ------------------------------------------------------------------ everything

    // (Kept, so that a frame with something on the field makes no lists of its own)
    private static readonly SideCondition[] SideKinds = Enum.GetValues<SideCondition>();
    private static readonly FieldCondition[] FieldKinds = Enum.GetValues<FieldCondition>();
    private static readonly PlaceCondition[] PlaceKinds = Enum.GetValues<PlaceCondition>();

    /// <summary>Everything the field's look has standing or fading, for a battle of <paramref name="slots"/> Pokémon a side.</summary>
    public static void Emit(FieldLook look, float time, FxPlaces places, int slots, FxList fx)
    {
        if (!look.Any) return;
        for (int s = 0; s < 2; s++)
        {
            var side = (BattleSide)s;
            foreach (var kind in SideKinds)
                Side(side, kind, look.Of(side, kind), time, places, fx);
            for (int slot = 0; slot < Math.Clamp(slots, 1, 2); slot++)
                foreach (var kind in PlaceKinds)
                    Place(new Place(side, slot), kind, look.Of(new Place(side, slot), kind), time, places, fx);
        }
        foreach (var kind in FieldKinds) Field(kind, look.Of(kind), time, places, fx);
    }

    /// <summary>One look of a side, as it stands or fades.</summary>
    public static void Side(BattleSide side, SideCondition what, ConditionView view, float time, FxPlaces places, FxList fx)
    {
        if (!view.Visible) return;
        var p = PlatformOf(side, places);
        switch (what)
        {
            case SideCondition.Spikes:
                Caltrops(p, view, time, fx, SpikeRings, Rgba(196, 204, 220), Rgba(120, 128, 150), bubbles: false);
                break;
            case SideCondition.ToxicSpikes:
                Caltrops(p, view, time, fx, ToxicRings, Rgba(186, 120, 236), Rgba(120, 64, 160), bubbles: true);
                break;
            case SideCondition.StealthRock:
                HoveringStones(p, view, time, fx);
                break;
            case SideCondition.Reflect:
                Pane(p, view, time, fx, 0f, ReflectTop, Rgba(255, 196, 110), Rgba(255, 236, 190));
                break;
            case SideCondition.LightScreen:
                Pane(p, view, time, fx, LightScreenFoot, LightScreenTop, Rgba(140, 214, 255), Rgba(220, 244, 255));
                break;
            case SideCondition.Safeguard:
                Veil(p, view, time, fx);
                break;
            case SideCondition.Mist:
                MistWisps(p, view, time, fx);
                break;
            case SideCondition.Tailwind:
                Wind(p, view, time, fx);
                break;
            case SideCondition.LuckyChant:
                Charms(p, view, time, fx);
                break;
            case SideCondition.AuroraVeil:
                Aurora(p, view, time, fx);
                break;
            case SideCondition.StickyWeb:
                Web(p, view, time, fx);
                break;
            case SideCondition.Rainbow:
                Rainbow(p, view, time, fx);
                break;
            case SideCondition.SeaOfFire:
                Mire(p, view, time, fx, fire: true);
                break;
            case SideCondition.Swamp:
                Mire(p, view, time, fx, fire: false);
                break;
        }
    }

    /// <summary>Trick Room's grid, Gravity's sinking dust (its light is <see cref="ArtLook.UnderGravity"/>), or a terrain on both platforms.</summary>
    public static void Field(FieldCondition what, ConditionView view, float time, FxPlaces places, FxList fx)
    {
        if (!view.Visible) return;
        if (what == FieldCondition.TrickRoom) Grid(view, time, fx);
        else if (what == FieldCondition.Gravity) SinkingDust(view, time, places, fx);
        else if (what is FieldCondition.WonderRoom or FieldCondition.MagicRoom)
            for (int s = 0; s < 2; s++) Room(what, PlatformOf((BattleSide)s, places), s, view, time, fx);
        else if (FieldState.IsTerrain(what))
            for (int s = 0; s < 2; s++) Terrain(what, PlatformOf((BattleSide)s, places), s, view, time, fx);
    }

    /// <summary>What waits over a place: a Wish's star, or the mote of an attack sent ahead.</summary>
    public static void Place(Place place, PlaceCondition what, ConditionView view, float time, FxPlaces places, FxList fx)
    {
        if (!view.Visible) return;
        float radius = place.Side == BattleSide.Player ? BattleStage.PlayerPlatformRadius : BattleStage.EnemyPlatformRadius;
        var feet = places.Feet(place.Side, place.Slot);
        // Over the head of whoever stands there, and never out of the picture over a giant
        float over = Math.Min(places.Height(place.Side, place.Slot) * 1.04f + radius * 0.1f, radius * 1.5f);
        float k = view.Strength, arrive = Fx.EaseOut(view.Arrival);
        if (what == PlaceCondition.Wish)
        {
            var at = feet + new Vector3(-radius * 0.13f, over + radius * 0.03f * MathF.Sin(time * 1.5f + view.Seed), 0f);
            float size = radius * 0.085f;
            var gold = Rgba(255, 226, 110);
            // The star itself is matter, so it keeps its gold over a bright sky; its halo and its rays are light
            fx.Sprite(FxShape.Glow, at, size * 2.2f, Fx.Fade(gold, 0.4f * k * arrive), depth: true);
            fx.Sprite(FxShape.Star, at, size * (0.4f + 0.6f * arrive), Fx.Fade(gold, k), FxBlend.Alpha, 0.15f * MathF.Sin(time * 0.9f), depth: true);
            fx.Sprite(FxShape.Spark, at, size * 1.7f * (0.5f + 0.5f * MathF.Sin(time * 2.6f + view.Seed)), Fx.Fade(Color.White, 0.4f * k * arrive), rotation: time * 0.4f, depth: true);
        }
        else
        {
            bool doom = what == PlaceCondition.DoomDesire;
            var colour = doom ? Rgba(206, 214, 236) : Rgba(214, 96, 240);
            // It circles on the spot, beside where a Wish's star would hang
            float turn = time * 2.1f + view.Seed;
            var at = feet + new Vector3(radius * (0.13f + 0.04f * MathF.Cos(turn)), over + radius * 0.04f * MathF.Sin(turn), 0f);
            float size = radius * 0.07f;
            fx.Sprite(FxShape.Glow, at, size * 2f, Fx.Fade(colour, 0.35f * k * arrive), depth: true);
            fx.Sprite(FxShape.Orb, at, size * (0.4f + 0.6f * arrive), Fx.Fade(colour, k), FxBlend.Alpha, time, depth: true);
            float ring = Frac(time / 1.5f + Fx.Rand(view.Seed, 0, 7));
            fx.Sprite(FxShape.Ring, at, size * (1f + 1.8f * ring) * RingScale, Fx.Fade(colour, (1f - ring) * 0.8f * k * arrive), FxBlend.Alpha, depth: true);
            if (doom) fx.Sprite(FxShape.Cross, at, size * 1.4f, Fx.Fade(Color.White, 0.6f * k * arrive), rotation: MathF.PI / 4f, depth: true);
        }
    }

    // ------------------------------------------------------------------ a side's platform

    /// <summary>
    /// A side's platform as the looks stand on it: its middle on the ground, its radius, the height of its top, the
    /// way to the other side's platform and to its right, and the angle (from the way forward) at which the camera stands.
    /// </summary>
    private readonly record struct Platform(Vector3 Centre, float R, float Top, Vector3 Forward, Vector3 Right, float CameraAngle)
    {
        /// <summary>The way out from the middle at an angle from the way forward, level with the ground.</summary>
        public Vector3 Out(float angle) => Forward * MathF.Cos(angle) + Right * MathF.Sin(angle);

        /// <summary>A point on the ground's plane at an angle and a distance from the middle, this high over the ground.</summary>
        public Vector3 At(float angle, float distance, float height) => Centre + Out(angle) * distance + Vector3.UnitY * height;

        /// <summary>An angle on the arc that doesn't face the camera, spread evenly over it by 0..1.</summary>
        public float Open(float u) => CameraAngle + CameraArc + Frac(u) * (MathF.Tau - 2f * CameraArc);
    }

    private static Platform PlatformOf(BattleSide side, FxPlaces places)
    {
        bool player = side == BattleSide.Player;
        var centre = player ? BattleStage.PlayerSpot : BattleStage.EnemySpot;
        var other = player ? BattleStage.EnemySpot : BattleStage.PlayerSpot;
        var forward = Vector3.Normalize(new Vector3(other.X - centre.X, 0f, other.Z - centre.Z));
        var right = new Vector3(-forward.Z, 0f, forward.X);
        var toCamera = new Vector3(BattleStage.CameraPosition.X - centre.X, 0f, BattleStage.CameraPosition.Z - centre.Z);
        float cameraAngle = MathF.Atan2(Vector3.Dot(toCamera, right), Vector3.Dot(toCamera, forward));
        return new Platform(centre with { Y = 0f }, player ? BattleStage.PlayerPlatformRadius : BattleStage.EnemyPlatformRadius,
            places.Feet(side, 0).Y, forward, right, cameraAngle);
    }

    // ------------------------------------------------------------------ what lies in wait

    /// <summary>
    /// A ring of caltrops round the rim for each layer: an upright point and a crossed one each. The newest layer
    /// falls from above, each caltrop in its own moment, and settles with a hop; fading, they sink into the ground.
    /// </summary>
    private static void Caltrops(Platform p, ConditionView view, float time, FxList fx, float[] rings, Color point, Color cross, bool bubbles)
    {
        int layers = Math.Min(view.Shown, rings.Length);
        float k = view.Strength;
        int seed = view.Seed;
        for (int layer = 0; layer < layers; layer++)
        {
            float arrive = view.Up && layer == view.Layers - 1 ? view.Arrival : 1f;
            for (int i = 0; i < CaltropsPerRing; i++)
            {
                int id = layer * CaltropsPerRing + i;
                // Each waits its own moment, then falls for half of the arrival
                float u = Math.Clamp((arrive - Fx.Rand(seed, id, 3) * 0.45f) / 0.55f, 0f, 1f);
                if (u <= 0f) continue;
                float angle = (i + 0.5f * (layer % 2) + (bubbles ? 0.25f : 0f) + Fx.Signed(seed, id, 1) * 0.2f) * MathF.Tau / CaltropsPerRing;
                float size = p.R * 0.06f * (0.85f + 0.3f * Fx.Rand(seed, id, 4));
                float fall = u < 0.7f ? p.R * 0.5f * (1f - (u / 0.7f) * (u / 0.7f)) : p.R * 0.03f * MathF.Sin((u - 0.7f) / 0.3f * MathF.PI);
                var at = p.At(angle, p.R * (rings[layer] + Fx.Signed(seed, id, 2) * 0.018f), p.Top + size * 0.8f + fall - (1f - k) * size * 1.2f);
                float alpha = k * Math.Min(1f, u * 4f);
                fx.Sprite(FxShape.Shard, at - Vector3.UnitY * size * 0.45f, size * 0.8f, Fx.Fade(cross, alpha), FxBlend.Alpha, 1.9f + Fx.Signed(seed, id, 6) * 0.4f, depth: true);
                fx.Sprite(FxShape.Shard, at, size, Fx.Fade(point, alpha), FxBlend.Alpha, Fx.Signed(seed, id, 5) * 0.25f, depth: true);
            }
        }

        if (!bubbles || layers == 0) return;
        // A bubble of poison rises from a caltrop, a new one each time it comes round
        for (int i = 0; i < 3; i++)
        {
            float phase = time / (1.5f + 0.4f * i) + Fx.Rand(seed, i, 31);
            int round = (int)MathF.Floor(phase);
            float u = phase - round;
            int from = (int)(Fx.Rand(seed + round, i, 32) * layers * CaltropsPerRing);
            int layer = Math.Min(from / CaltropsPerRing, layers - 1), slot = from % CaltropsPerRing;
            float angle = (slot + 0.5f * (layer % 2) + 0.25f + Fx.Signed(seed, from, 1) * 0.2f) * MathF.Tau / CaltropsPerRing;
            var at = p.At(angle, p.R * rings[layer], p.Top + p.R * (0.06f + 0.14f * u));
            fx.Sprite(FxShape.Bubble, at, p.R * (0.022f + 0.02f * u), Rgba(214, 150, 255, 220f * k * view.Arrival * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, depth: true);
        }
    }

    /// <summary>Stealth Rock: pointed stones hovering over the rim on the flanks and the far side, each bobbing and turning slowly, a blot of shade under each.</summary>
    private static void HoveringStones(Platform p, ConditionView view, float time, FxList fx)
    {
        float k = view.Strength;
        int seed = view.Seed;
        for (int i = 0; i < Stones; i++)
        {
            // They rise out of the ground one after another
            float u = Fx.EaseOut(Math.Clamp((view.Arrival - i * 0.1f) / 0.6f, 0f, 1f));
            if (u <= 0f) continue;
            float angle = p.Open((i + 0.5f + Fx.Signed(seed, i, 1) * 0.2f) / Stones);
            float size = p.R * (0.085f + 0.035f * Fx.Rand(seed, i, 3));
            float hover = p.R * (0.22f + 0.14f * Fx.Rand(seed, i, 2)) + p.R * 0.03f * MathF.Sin(time * (0.9f + 0.4f * Fx.Rand(seed, i, 4)) + i * 1.7f);
            float turn = Fx.Signed(seed, i, 5) * 0.5f + 0.2f * MathF.Sin(time * 0.5f + i);
            var ground = p.At(angle, p.R * 1.08f, 0.02f);
            var at = ground + Vector3.UnitY * (p.Top + hover * u - (1f - k) * size);
            fx.Flat(FxShape.Glow, ground, Vector3.UnitY, size * 1.3f, Rgba(18, 14, 28, 80f * k * u), FxBlend.Alpha);
            fx.Sprite(FxShape.Rock, at, size, Rgba(168, 150, 122, 255f * k * u), FxBlend.Alpha, turn, depth: true);
            // Its point, a splinter of the same stone standing out of it
            var tip = at + new Vector3(-MathF.Sin(turn), MathF.Cos(turn), 0f) * size * 0.6f;
            fx.Sprite(FxShape.Shard, tip, size * 0.7f, Rgba(196, 180, 150, 255f * k * u), FxBlend.Alpha, turn, depth: true);
        }
    }

    // ------------------------------------------------------------------ screens and veils

    /// <summary>
    /// Reflect's or Light Screen's pane: a band of coloured glass along the arc of the rim that faces the other side,
    /// between two heights over the platform's top, with a bright upper edge and a glint sliding along it. It rises
    /// from its foot and spreads from its middle as it comes in.
    /// </summary>
    private static void Pane(Platform p, ConditionView view, float time, FxList fx, float foot, float top, Color glass, Color light)
    {
        float k = view.Strength, grow = Fx.EaseOut(view.Arrival);
        if (grow <= 0f) return;
        const int segments = 12;
        float arc = PaneArc * (0.35f + 0.65f * grow);
        float low = p.Top + p.R * foot, high = p.Top + p.R * (foot + (top - foot) * grow);
        var body = new Vector3[segments + 1];
        var edge = new Vector3[segments + 1];
        for (int i = 0; i <= segments; i++)
        {
            float angle = -arc + 2f * arc * i / segments;
            body[i] = p.At(angle, p.R, (low + high) * 0.5f);
            // The edge's own soft sides end at the pane's top
            edge[i] = p.At(angle, p.R, high - p.R * 0.004f);
        }
        float breathe = 0.85f + 0.15f * MathF.Sin(time * 1.6f + view.Seed);
        // The ribbon's shape is soft at its sides: what shows of it is a band of about three fifths of its width
        fx.Ribbon(body, (high - low) * 1.7f, Fx.Fade(glass, 0.85f * k * breathe), FxBlend.Alpha, depth: true);
        fx.Ribbon(edge, p.R * 0.012f, Fx.Fade(light, 0.7f * k), depth: true);
        // A glint running the length of the upper edge
        float along = Frac(time / 2.4f + Fx.Rand(view.Seed, 0, 9));
        fx.Sprite(FxShape.Spark, p.At(-arc + 2f * arc * along, p.R, high), p.R * 0.06f, Fx.Fade(Color.White, k * grow * MathF.Sin(along * MathF.PI)), rotation: time, depth: true);
    }

    /// <summary>Safeguard: a pale ring lying round the platform's foot, breathing, with motes rising a little way from it.</summary>
    private static void Veil(Platform p, ConditionView view, float time, FxList fx)
    {
        float k = view.Strength * Fx.EaseOut(view.Arrival);
        var pale = Rgba(190, 255, 214);
        float breathe = MathF.Sin(time * 1.3f + view.Seed);
        fx.Flat(FxShape.Ring, p.At(0f, 0f, 0.03f), Vector3.UnitY, p.R * 1.12f * RingScale * (1f + 0.02f * breathe), Fx.Fade(pale, k * (0.45f + 0.12f * breathe)));
        for (int i = 0; i < 6; i++)
        {
            float phase = time * (0.45f + 0.1f * Fx.Rand(view.Seed, i, 2)) + Fx.Rand(view.Seed, i, 3);
            int round = (int)MathF.Floor(phase);
            float u = phase - round;
            float angle = (i + Fx.Rand(view.Seed + round, i, 4)) * MathF.Tau / 6f;
            fx.Sprite(FxShape.Glow, p.At(angle, p.R * 1.12f, 0.03f + p.R * 0.2f * u), p.R * 0.035f, Fx.Fade(pale, k * MathF.Sin(u * MathF.PI)), depth: true);
        }
    }

    /// <summary>
    /// Mist: wisps lying flat round the rim in two thin layers, the upper drifting the other way, and four low puffs
    /// standing on the rim where they come before nobody.
    /// </summary>
    private static void MistWisps(Platform p, ConditionView view, float time, FxList fx)
    {
        float k = view.Strength * Fx.EaseOut(view.Arrival);
        for (int layer = 0; layer < 2; layer++)
            for (int i = 0; i < 6; i++)
            {
                int id = layer * 6 + i;
                float angle = (i + 0.5f * layer + Fx.Rand(view.Seed, id, 1) * 0.4f) * MathF.Tau / 6f + time * (layer == 0 ? 0.12f : -0.17f);
                float breathe = 0.8f + 0.2f * MathF.Sin(time * 0.9f + id * 1.3f);
                var at = p.At(angle, p.R * (0.9f + 0.12f * layer), p.Top + 0.02f + p.R * 0.035f * layer);
                fx.Flat(FxShape.Smoke, at, Vector3.UnitY, p.R * (0.28f + 0.1f * Fx.Rand(view.Seed, id, 2)), Rgba(240, 244, 250, 215f * k * breathe), FxBlend.Alpha,
                    Fx.Rand(view.Seed, id, 3) * MathF.Tau + time * 0.1f);
            }
        for (int i = 0; i < 4; i++)
        {
            float angle = p.Open((i + 0.5f) / 4f + 0.02f * MathF.Sin(time * 0.25f + i * 1.9f));
            float breathe = 0.75f + 0.25f * MathF.Sin(time * 0.7f + i * 2.3f + view.Seed);
            fx.Sprite(FxShape.Smoke, p.At(angle, p.R * 0.98f, p.Top + p.R * 0.07f), p.R * 0.12f, Rgba(240, 244, 250, 150f * k * breathe), FxBlend.Alpha,
                Fx.Rand(view.Seed, i, 5) * MathF.Tau + time * 0.08f, depth: true);
        }
    }

    /// <summary>Tailwind: streaks of wind running low past both flanks of the platform, from behind the side toward the other.</summary>
    private static void Wind(Platform p, ConditionView view, float time, FxList fx)
    {
        float k = view.Strength * Fx.EaseOut(view.Arrival);
        for (int i = 0; i < 10; i++)
        {
            float flank = i % 2 == 0 ? 1f : -1f;
            float phase = Frac(time / (0.9f + 0.5f * Fx.Rand(view.Seed, i, 2)) + Fx.Rand(view.Seed, i, 3));
            var at = p.Centre + p.Forward * p.R * (-1.7f + 3.4f * phase) + p.Right * flank * p.R * (1.1f + 0.45f * Fx.Rand(view.Seed, i, 1))
                + Vector3.UnitY * (0.04f + p.R * (0.03f + 0.15f * Fx.Rand(view.Seed, i, 4)));
            float ends = Math.Clamp(Math.Min(phase, 1f - phase) * 5f, 0f, 1f);
            fx.Streak(FxShape.Streak, at, p.Forward, p.R * 0.07f, 4.5f, Rgba(214, 236, 255, 210f * k * ends), FxBlend.Alpha, depth: true);
        }
    }

    /// <summary>Lucky Chant: three small charms hovering over the rim on the flanks and the far side, bobbing, each with a twinkle now and then.</summary>
    private static void Charms(Platform p, ConditionView view, float time, FxList fx)
    {
        float k = view.Strength, arrive = Fx.EaseOut(view.Arrival);
        var pink = Rgba(255, 186, 224);
        for (int i = 0; i < 3; i++)
        {
            float angle = p.Open((i + 0.5f) / 3f + 0.03f * MathF.Sin(time * 0.3f + i * 2f));
            float size = p.R * 0.055f;
            var at = p.At(angle, p.R, p.Top + p.R * (0.16f * arrive - 0.03f) + p.R * 0.02f * MathF.Sin(time * 1.7f + i * 2.1f) - size);
            float twinkle = MathF.Pow(MathF.Max(0f, MathF.Sin(time * 2.2f + i * 2.4f + view.Seed)), 4f);
            fx.Sprite(FxShape.Glow, at, size * 1.8f, Fx.Fade(pink, 0.4f * k * arrive), depth: true);
            fx.Sprite(FxShape.Star, at, size, Fx.Fade(pink, k * arrive), FxBlend.Alpha, 0.2f * MathF.Sin(time * 1.1f + i), depth: true);
            fx.Sprite(FxShape.Spark, at, size * 1.9f * twinkle, Fx.Fade(Color.White, twinkle * k * arrive), depth: true);
        }
    }

    // ------------------------------------------------------------------ the rooms

    /// <summary>One line of Trick Room's grid as it stands whole: where it grows from, its two ends, its place in the wave and how strong it is.</summary>
    private readonly record struct GridLine(Vector3 From, Vector3[] Ends, int Wave, float Strength);

    /// <summary>The grid laid once: the floor's lines, the three walls' uprights and their rails. A frame only reads it.</summary>
    private static readonly GridLine[] GridOfLight = LayGrid();

    private static GridLine[] LayGrid()
    {
        const float y = 0.03f;
        float midX = (GridMin.X + GridMax.X) * 0.5f, midZ = (GridMin.Y + GridMax.Y) * 0.5f;
        var lines = new System.Collections.Generic.List<GridLine>();
        void Line(Vector3 from, Vector3 a, Vector3 b, int wave, float strength) => lines.Add(new GridLine(from, new[] { a, b }, wave, strength));

        for (int i = 0; i < GridLines; i++)
        {
            float u = i / (float)(GridLines - 1);
            float x = GridMin.X + (GridMax.X - GridMin.X) * u, z = GridMin.Y + (GridMax.Y - GridMin.Y) * u;
            // The floor, each line growing out from its middle
            Line(new Vector3(x, y, midZ), new Vector3(x, y, GridMin.Y), new Vector3(x, y, GridMax.Y), i, 170f);
            Line(new Vector3(midX, y, z), new Vector3(GridMin.X, y, z), new Vector3(GridMax.X, y, z), i + GridLines, 170f);
            // The walls' uprights, rising from their feet: behind the foe, and at the left and the right
            Line(new Vector3(x, 0f, GridMin.Y), new Vector3(x, 0f, GridMin.Y), new Vector3(x, WallHeight, GridMin.Y), i + 3, 110f);
            Line(new Vector3(GridMin.X, 0f, z), new Vector3(GridMin.X, 0f, z), new Vector3(GridMin.X, WallHeight, z), i + 5, 110f);
            Line(new Vector3(GridMax.X, 0f, z), new Vector3(GridMax.X, 0f, z), new Vector3(GridMax.X, WallHeight, z), i + 7, 110f);
        }
        for (int rail = 1; rail <= 2; rail++)
        {
            // Two rails along each wall, rising with it from the middle of its foot
            float height = WallHeight * rail * 0.5f;
            Line(new Vector3(midX, 0f, GridMin.Y), new Vector3(GridMin.X, height, GridMin.Y), new Vector3(GridMax.X, height, GridMin.Y), rail, 110f);
            Line(new Vector3(GridMin.X, 0f, midZ), new Vector3(GridMin.X, height, GridMin.Y), new Vector3(GridMin.X, height, GridMax.Y), rail + 2, 110f);
            Line(new Vector3(GridMax.X, 0f, midZ), new Vector3(GridMax.X, height, GridMin.Y), new Vector3(GridMax.X, height, GridMax.Y), rail + 4, 110f);
        }
        return lines.ToArray();
    }

    /// <summary>
    /// Trick Room: a grid of violet lines on the ground under the whole field and rising as walls behind the foe's
    /// platform and at both sides, never between the camera and the field. Each is a line of the world from end to
    /// end (a ribbon, not a sprite turned to the camera: the grid runs away from the lens, and narrows as it goes),
    /// in its own colour, since added light would wash out to white over a bright field. The lines grow out as it
    /// comes in, and a wave passes along them while it lasts.
    /// </summary>
    private static void Grid(ConditionView view, float time, FxList fx)
    {
        float k = view.Strength, grow = Fx.EaseOut(view.Arrival);
        if (grow <= 0f) return;
        const float width = 0.11f;
        foreach (var line in GridOfLight)
        {
            // Standing whole, the line's own ends are handed over as they are; growing, they are worked out for the frame
            var ends = grow >= 1f ? line.Ends : new[] { Vector3.Lerp(line.From, line.Ends[0], grow), Vector3.Lerp(line.From, line.Ends[1], grow) };
            var colour = Rgba(214, 120, 255, line.Strength * k * (0.7f + 0.3f * MathF.Sin(time * 1.4f - line.Wave * 0.8f)));
            fx.Ribbon(ends, width, colour, FxBlend.Alpha, depth: true);
        }
    }

    /// <summary>Gravity: short streaks sinking to the ground round both platforms, faster as they fall.</summary>
    private static void SinkingDust(ConditionView view, float time, FxPlaces places, FxList fx)
    {
        float k = view.Strength * Fx.EaseOut(view.Arrival);
        for (int s = 0; s < 2; s++)
        {
            var p = PlatformOf((BattleSide)s, places);
            for (int i = 0; i < 8; i++)
            {
                int id = s * 8 + i;
                float phase = time / (1.6f + 0.8f * Fx.Rand(view.Seed, id, 1)) + Fx.Rand(view.Seed, id, 2);
                int round = (int)MathF.Floor(phase);
                float u = phase - round;
                float angle = p.Open((i + Fx.Rand(view.Seed + round, id, 3)) / 8f);
                var at = p.At(angle, p.R * (1.25f + 0.5f * Fx.Rand(view.Seed + round, id, 4)), 0.05f + p.R * 0.9f * (1f - u * u));
                float ends = Math.Clamp(Math.Min(u * 4f, (1f - u) * 8f), 0f, 1f);
                fx.Streak(FxShape.Streak, at, -Vector3.UnitY, p.R * 0.09f, 2.4f, Rgba(170, 150, 210, 220f * k * ends), FxBlend.Alpha, depth: true);
            }
        }
    }

    // ------------------------------------------------------------------ the later games' (plan 06 · R19b)

    /// <summary>
    /// Wonder Room and Magic Room (plan 06 · R25): three pieces over the rim on the flanks and the far side, clear of
    /// the camera. Wonder Room's are hexagonal shields turning over, teal on one face and indigo on the other;
    /// Magic Room's pink motes dimming and brightening. They rise into place as the room comes in.
    /// </summary>
    private static void Room(FieldCondition what, Platform p, int side, ConditionView view, float time, FxList fx)
    {
        float k = view.Strength, grow = Fx.EaseOut(view.Arrival);
        if (grow <= 0f) return;
        for (int i = 0; i < RoomPieces; i++)
        {
            float bob = 0.03f * MathF.Sin(time * 1.3f + i * 2.1f + side + view.Seed);
            var at = p.At(p.Open((i + 0.5f) / RoomPieces), p.R * 1.06f, p.Top + p.R * (0.26f + 0.06f * i + bob) * grow);
            if (what == FieldCondition.WonderRoom)
            {
                float turn = time * MathF.PI / 2f + i * 1.3f + view.Seed * 0.1f;
                float face = MathF.Cos(turn);
                var colour = face >= 0f ? Rgba(110, 230, 220, 230f * k) : Rgba(130, 120, 255, 230f * k);
                fx.Sprite(FxShape.Hex, at, p.R * 0.09f * (0.35f + 0.65f * MathF.Abs(face)), colour, FxBlend.Alpha, turn * 0.5f, depth: true);
            }
            else
            {
                float pulse = 0.65f + 0.35f * MathF.Sin(time * 1.1f + i * 1.7f + side + view.Seed);
                fx.Sprite(FxShape.Orb, at, p.R * 0.07f, Rgba(236, 140, 214, 230f * k * pulse), FxBlend.Alpha, time * 0.3f, depth: true);
            }
        }
    }

    /// <summary>
    /// Aurora Veil: two curtains of the northern lights hanging over the rim on the flanks and the far side, green
    /// over pink, rippling as they hang, with a few glints running down them. They keep to the arc that faces nobody,
    /// as the stones do, and rise into being as it comes in.
    /// </summary>
    private static void Aurora(Platform p, ConditionView view, float time, FxList fx)
    {
        float k = view.Strength, grow = Fx.EaseOut(view.Arrival);
        if (grow <= 0f) return;
        const int segments = 14;
        var colours = new[] { Rgba(120, 255, 196), Rgba(236, 150, 255) };
        for (int band = 0; band < 2; band++)
        {
            var points = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float u = i / (float)segments;
                float wave = MathF.Sin(u * 9f + time * (1.1f + 0.3f * band) + view.Seed * 0.37f + band * 1.4f);
                points[i] = p.At(p.Open(0.04f + 0.92f * u), p.R * (1.04f + 0.04f * band + 0.02f * wave),
                    p.Top + p.R * (0.3f + 0.16f * band + 0.04f * wave) * grow);
            }
            float breathe = 0.75f + 0.25f * MathF.Sin(time * 0.8f + band * 2.1f + view.Seed);
            fx.Ribbon(points, p.R * (0.42f - 0.12f * band) * grow, Fx.Fade(colours[band], 0.6f * k * breathe), FxBlend.Alpha, depth: true);
        }
        // Glints running down the curtains
        for (int i = 0; i < 5; i++)
        {
            float phase = time / (1.3f + 0.4f * Fx.Rand(view.Seed, i, 1)) + Fx.Rand(view.Seed, i, 2);
            int round = (int)MathF.Floor(phase);
            float u = phase - round;
            float angle = p.Open(0.1f + 0.8f * Fx.Rand(view.Seed + round, i, 3));
            var at = p.At(angle, p.R * 1.05f, p.Top + p.R * (0.46f - 0.3f * u) * grow);
            fx.Sprite(FxShape.Spark, at, p.R * 0.05f, Fx.Fade(Color.White, k * grow * MathF.Sin(u * MathF.PI)), rotation: time + i, depth: true);
        }
    }

    /// <summary>
    /// Sticky Web: a web of silver threads strung over the platform, its spokes running out to the rim and its rings
    /// between them, lying just over the top so a body hides what is under it; dew glints along a thread now and
    /// then. The spokes run out from the middle as it comes in; fading, it thins away.
    /// </summary>
    private static void Web(Platform p, ConditionView view, float time, FxList fx)
    {
        float k = view.Strength, grow = Fx.EaseOut(view.Arrival);
        if (grow <= 0f) return;
        var silver = Rgba(226, 230, 240);
        float turn = Fx.Rand(view.Seed, 0, 1) * MathF.Tau / WebSpokes;
        float lift = p.Top + p.R * 0.012f;
        for (int i = 0; i < WebSpokes; i++)
        {
            float angle = turn + i * MathF.Tau / WebSpokes;
            fx.Ribbon(new[] { p.At(angle, p.R * 0.08f, lift), p.At(angle, p.R * 0.98f * grow, lift) }, p.R * 0.014f, Fx.Fade(silver, 0.8f * k), FxBlend.Alpha, depth: true);
        }
        for (int ring = 0; ring < WebRings; ring++)
        {
            float radius = p.R * (0.42f + 0.36f * ring);
            if (radius > p.R * 0.98f * grow) continue;
            // A ring of a spider's web sags between its spokes
            var points = new Vector3[WebSpokes * 2 + 1];
            for (int i = 0; i < points.Length; i++)
            {
                float angle = turn + i * MathF.PI / WebSpokes;
                float sag = i % 2 == 1 ? 0.9f : 1f;
                points[i] = p.At(angle, radius * sag, lift + p.R * 0.002f);
            }
            fx.Ribbon(points, p.R * 0.012f, Fx.Fade(silver, 0.7f * k), FxBlend.Alpha, depth: true);
        }
        for (int i = 0; i < 3; i++)
        {
            float phase = time / (1.7f + 0.5f * i) + Fx.Rand(view.Seed, i, 4);
            int round = (int)MathF.Floor(phase);
            float u = phase - round;
            int spoke = (int)(Fx.Rand(view.Seed + round, i, 5) * WebSpokes);
            var at = p.At(turn + spoke * MathF.Tau / WebSpokes, p.R * (0.3f + 0.6f * Fx.Rand(view.Seed + round, i, 6)) * grow, lift + p.R * 0.02f);
            fx.Sprite(FxShape.Spark, at, p.R * 0.05f, Fx.Fade(Color.White, k * grow * MathF.Sin(u * MathF.PI)), rotation: time, depth: true);
        }
    }

    /// <summary>
    /// A rainbow (plan 06 · R25's leftovers): five thin bands of colour arching over the rim from end to end of the arc
    /// that faces nobody, the outermost red and highest, shimmering band by band; they rise from their feet as it
    /// comes in. Matter in its own colours, so the day's light doesn't wash it out.
    /// </summary>
    private static void Rainbow(Platform p, ConditionView view, float time, FxList fx)
    {
        float k = view.Strength, grow = Fx.EaseOut(view.Arrival);
        if (grow <= 0f) return;
        const int segments = 16;
        var colours = new[] { Rgba(255, 120, 120), Rgba(255, 184, 96), Rgba(255, 236, 120), Rgba(128, 220, 136), Rgba(128, 170, 255) };
        for (int band = 0; band < RainbowBands; band++)
        {
            var points = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float u = i / (float)segments;
                float arch = MathF.Sin(u * MathF.PI);
                points[i] = p.At(p.Open(0.03f + 0.94f * u), p.R * (1.08f - 0.012f * band),
                    p.Top + p.R * (0.2f + (0.42f - 0.035f * band) * arch * grow));
            }
            float shimmer = 0.7f + 0.3f * MathF.Sin(time * 0.9f - band * 0.8f + view.Seed * 0.29f);
            fx.Ribbon(points, p.R * 0.045f * (0.4f + 0.6f * grow), Fx.Fade(colours[band], 0.6f * k * shimmer), FxBlend.Alpha, depth: true);
        }
    }

    /// <summary>
    /// A sea of fire or a swamp (plan 06 · R25's leftovers), round the side it envelops: a wash of its colour on the
    /// platform's top and a low band of it standing round the far rim, as a terrain's; tongues of flame flickering up
    /// from the far rim for the sea, bubbles of mud rising slowly and bursting for the swamp.
    /// </summary>
    private static void Mire(Platform p, ConditionView view, float time, FxList fx, bool fire)
    {
        float k = view.Strength, grow = Fx.EaseOut(view.Arrival);
        if (grow <= 0f) return;
        int seed = view.Seed;
        var wash = fire ? Rgba(232, 84, 40) : Rgba(112, 120, 64);
        var rim = fire ? Rgba(232, 84, 40) : Rgba(88, 96, 52);
        float breathe = 0.8f + 0.2f * MathF.Sin(time * (fire ? 2.3f : 0.7f) + seed);
        fx.Flat(FxShape.Glow, p.At(0f, 0f, p.Top + 0.01f), Vector3.UnitY, p.R * 2.3f * (0.6f + 0.4f * grow), Fx.Fade(wash, 0.45f * k * breathe), FxBlend.Alpha);
        const int segments = 12;
        var band = new Vector3[segments + 1];
        for (int i = 0; i <= segments; i++) band[i] = p.At(p.Open(i / (float)segments), p.R * 1.01f, p.Top + p.R * 0.05f * grow);
        fx.Ribbon(band, p.R * 0.12f * grow, Fx.Fade(rim, 0.6f * k * breathe), FxBlend.Alpha, depth: true);
        if (fire)
        {
            for (int i = 0; i < Flames; i++)
            {
                // Each tongue licks up on its own beat, a little way round the rim from the last
                float flicker = 0.5f + 0.5f * MathF.Sin(time * (6f + 1.5f * Fx.Rand(seed, i, 1)) + i * 1.9f);
                float angle = p.Open((i + 0.5f + Fx.Signed(seed, i, 2) * 0.25f) / Flames);
                float height = p.R * (0.1f + 0.1f * flicker) * grow;
                var at = p.At(angle, p.R * 1.0f, p.Top + height * 0.5f);
                fx.Sprite(FxShape.Flame, at, height * 0.6f, Rgba(255, 150, 60, 230f * k), FxBlend.Alpha, Fx.Signed(seed, i, 3) * 0.2f, depth: true);
            }
            return;
        }
        for (int i = 0; i < SwampBubbles; i++)
        {
            float phase = time / (2.2f + 0.6f * Fx.Rand(seed, i, 4)) + Fx.Rand(seed, i, 5);
            int round = (int)MathF.Floor(phase);
            float u = phase - round;
            var at = p.At(p.Open(Fx.Rand(seed + round, i, 6)), p.R * 0.98f, p.Top + p.R * (0.04f + 0.12f * u));
            float size = p.R * (0.03f + 0.025f * u), alpha = k * grow * MathF.Sin(u * MathF.PI);
            fx.Sprite(FxShape.Dot, at, size * 1.3f, Rgba(70, 76, 40, 150f * alpha), FxBlend.Alpha, depth: true);
            fx.Sprite(FxShape.Bubble, at, size, Rgba(196, 206, 150, 255f * alpha), FxBlend.Alpha, depth: true);
        }
    }

    /// <summary>
    /// A terrain on one platform: a wash of its colour lying on the platform's top (matter, alpha-blended, so it keeps
    /// its colour under the day's light), and its own pieces round the rim, standing only on the arc that faces
    /// nobody. Electric Terrain crackles with sparks along the ground, Grassy Terrain grows blades of grass that
    /// sway, Misty Terrain lies in pink wisps, Psychic Terrain ripples in violet rings with motes rising.
    /// </summary>
    private static void Terrain(FieldCondition what, Platform p, int side, ConditionView view, float time, FxList fx)
    {
        float k = view.Strength, grow = Fx.EaseOut(view.Arrival);
        if (grow <= 0f) return;
        int seed = view.Seed + side * 97;
        var (wash, piece) = what switch
        {
            FieldCondition.ElectricTerrain => (Rgba(255, 236, 110), Rgba(255, 250, 170)),
            FieldCondition.GrassyTerrain => (Rgba(96, 196, 92), Rgba(120, 214, 96)),
            FieldCondition.MistyTerrain => (Rgba(255, 176, 226), Rgba(255, 214, 240)),
            _ => (Rgba(196, 110, 255), Rgba(226, 170, 255))
        };
        float breathe = 0.8f + 0.2f * MathF.Sin(time * 1.2f + seed);
        fx.Flat(FxShape.Glow, p.At(0f, 0f, p.Top + 0.01f), Vector3.UnitY, p.R * 2.3f * (0.6f + 0.4f * grow), Fx.Fade(wash, 0.45f * k * breathe), FxBlend.Alpha);
        // A low band of the terrain's colour standing round the far rim, which is what the overview sees of a flat
        // thing on the foe's platform (a wash alone is a thread at its seven degrees)
        const int segments = 12;
        var band = new Vector3[segments + 1];
        for (int i = 0; i <= segments; i++) band[i] = p.At(p.Open(i / (float)segments), p.R * 1.01f, p.Top + p.R * 0.05f * grow);
        fx.Ribbon(band, p.R * 0.12f * grow, Fx.Fade(wash, 0.55f * k * breathe), FxBlend.Alpha, depth: true);
        switch (what)
        {
            case FieldCondition.ElectricTerrain:
                for (int i = 0; i < Sparks; i++)
                {
                    // A spark leaps along the ground for a moment, somewhere new each time
                    float phase = time / (0.5f + 0.3f * Fx.Rand(seed, i, 1)) + Fx.Rand(seed, i, 2);
                    int round = (int)MathF.Floor(phase);
                    float u = phase - round;
                    if (u > 0.5f) continue;
                    float angle = p.Open(Fx.Rand(seed + round, i, 3));
                    var at = p.At(angle, p.R * (0.9f + 0.25f * Fx.Rand(seed + round, i, 4)), p.Top + p.R * 0.08f);
                    fx.Sprite(FxShape.Spark, at, p.R * 0.13f, Fx.Fade(piece, k * grow * MathF.Sin(u / 0.5f * MathF.PI)), FxBlend.Alpha, Fx.Rand(seed + round, i, 5) * MathF.Tau, depth: true);
                }
                break;
            case FieldCondition.GrassyTerrain:
                for (int i = 0; i < Blades; i++)
                {
                    float angle = p.Open((i + 0.5f + Fx.Signed(seed, i, 1) * 0.3f) / Blades);
                    float height = p.R * (0.16f + 0.08f * Fx.Rand(seed, i, 2)) * grow;
                    float sway = 0.22f * MathF.Sin(time * 1.6f + i * 1.3f);
                    var foot = p.At(angle, p.R * (1.0f + 0.06f * Fx.Rand(seed, i, 3)), p.Top);
                    var along = Vector3.Normalize(new Vector3(sway, 1f, 0f));
                    fx.Streak(FxShape.Leaf, foot + along * height * 0.5f, along, height * 0.5f, 2.6f, Fx.Fade(piece, k), FxBlend.Alpha, depth: true);
                }
                break;
            case FieldCondition.MistyTerrain:
                for (int i = 0; i < Wisps; i++)
                {
                    // Puffs of pink mist standing on the far rim, drifting round it
                    float angle = p.Open((i + 0.5f) / Wisps + 0.04f * MathF.Sin(time * 0.3f + i * 1.7f));
                    var at = p.At(angle, p.R * 0.98f, p.Top + p.R * 0.08f);
                    fx.Sprite(FxShape.Smoke, at, p.R * 0.2f, Fx.Fade(piece, 0.75f * k * grow * (0.8f + 0.2f * MathF.Sin(time + i))), FxBlend.Alpha, time * 0.15f + i + Fx.Rand(seed, i, 1) * MathF.Tau, depth: true);
                }
                for (int i = 0; i < 2; i++)
                {
                    float phase = time / 1.9f + Fx.Rand(seed, i, 2);
                    float u = Frac(phase);
                    var at = p.At(p.Open(Fx.Rand(seed + (int)MathF.Floor(phase), i, 3)), p.R * 0.95f, p.Top + p.R * (0.08f + 0.2f * u));
                    fx.Sprite(FxShape.Spark, at, p.R * 0.06f, Fx.Fade(Color.White, k * grow * MathF.Sin(u * MathF.PI)), rotation: time, depth: true);
                }
                break;
            default:
                for (int i = 0; i < 2; i++)
                {
                    // Rings spreading out over the top, one after the other
                    float u = Frac(time / 2.2f + i * 0.5f + Fx.Rand(seed, i, 1) * 0.1f);
                    fx.Flat(FxShape.Ring, p.At(0f, 0f, p.Top + 0.015f), Vector3.UnitY, p.R * (0.4f + 1.1f * u) * RingScale, Fx.Fade(piece, 0.8f * k * grow * (1f - u)), FxBlend.Alpha);
                }
                for (int i = 0; i < Motes; i++)
                {
                    float phase = time / (2f + 0.6f * Fx.Rand(seed, i, 3)) + Fx.Rand(seed, i, 4);
                    int round = (int)MathF.Floor(phase);
                    float u = phase - round;
                    var at = p.At(p.Open(Fx.Rand(seed + round, i, 5)), p.R * 1.02f, p.Top + p.R * (0.05f + 0.35f * u));
                    fx.Sprite(FxShape.Orb, at, p.R * 0.07f, Fx.Fade(piece, k * grow * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, time, depth: true);
                }
                break;
        }
    }
}
