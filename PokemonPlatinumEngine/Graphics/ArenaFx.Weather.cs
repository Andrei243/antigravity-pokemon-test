using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;

namespace PokemonPlatinumEngine.Graphics;

// The weather over a battle (plan 25 · Y1; style guide, "Weather in battle"): the layer that lasts while the weather
// does, and the cue that plays as it begins and on each turn it goes on. Both are pure functions of their ages, the
// battle's clock and a seed from the animator: nothing is simulated, and nothing draws on the battle's chance.
internal static partial class ArenaFx
{
    /// <summary>The box the weather fills: the whole field and the near hills, up past the camera's view.</summary>
    private static readonly Vector3 SkyMin = new(-24f, 0f, -14f), SkyMax = new(16f, 12f, 23f);

    /// <summary>
    /// The box rain falls in: what the overview's lens takes in, and a little round it (plan 25 · Y2). The sky's whole
    /// box is four times its size, and a shower spread over that left a handful of streaks in the picture; a
    /// sandstorm's own box is drawn in the same way.
    /// </summary>
    private static readonly Vector3 RainMin = new(-14f, 0f, -10f), RainMax = new(9f, 6f, 23f);

    /// <summary>
    /// How much wider than their first numbers the weather's streaks are drawn, at the same length (plan 25 · Y2, the
    /// first session to see them): the Streak shape's line is about a sixth of its quad's width, and at those numbers
    /// rain was a hair under a pixel wide and a sandstorm a dozen threads.
    /// </summary>
    private const float RainWidth = 2.5f, SandWidth = 2f, HailWidth = 2f;

    /// <summary>A streak of weather, alpha-blended in the scene, <paramref name="wider"/> times its width at its own length.</summary>
    private static void WeatherStreak(FxList fx, Vector3 at, Vector3 along, float size, float stretch, Color color, float wider) =>
        fx.Streak(FxShape.Streak, at, along, size * wider, stretch / wider, color, FxBlend.Alpha, depth: true);

    /// <summary>
    /// What lasts in the air while a weather does: rain in streaks with rings on the platforms, the harsh sun's motes
    /// and warm haze, sand blown across, hail bouncing off the platforms, fog lying in low layers. It eases in over
    /// <see cref="BattleAnimator.WeatherFadeTime"/> from <paramref name="age"/> nought, and <paramref name="strength"/>
    /// takes it away again as a weather clears. Everything sits in the scene (depth-tested), behind what stands
    /// in front of it.
    /// </summary>
    public static void Weather(BattleWeather weather, float age, float time, FxPlaces places, FxList fx, float strength = 1f)
    {
        float k = Math.Clamp(age / BattleAnimator.WeatherFadeTime, 0f, 1f) * Math.Clamp(strength, 0f, 1f);
        if (k <= 0f) return;
        switch (weather)
        {
            case BattleWeather.Rain:
                RainLayer(fx, time, places, k);
                break;
            case BattleWeather.Sun:
                SunLayer(fx, time, k);
                break;
            case BattleWeather.Sandstorm:
                SandLayer(fx, time, k);
                break;
            case BattleWeather.Hail:
                HailLayer(fx, time, places, k, 70, 0);
                break;
            case BattleWeather.Fog:
                FogLayer(fx, time, k);
                break;
        }
    }

    /// <summary>
    /// The weather's cue (<see cref="CueKind.Weather"/>, <see cref="BattleAnimator.WeatherCueTime"/> long): a gust of
    /// it over the layer as it begins and on each turn it goes on. Rain sweeps in heavier and splashes on both
    /// platforms; the sun flares warm with rays slanting down; sand comes across in a wall; hail comes down in a
    /// flurry that bounces; fog rolls in from the left.
    /// </summary>
    public static void WeatherCue(BattleWeather weather, float age, int seed, FxPlaces places, FxList fx)
    {
        float env = Fx.Envelope(age, 0f, 0.15f, 0.75f, BattleAnimator.WeatherCueTime);
        if (env <= 0f) return;
        switch (weather)
        {
            case BattleWeather.Rain:
            {
                // A heavier shower over the layer, and drops bursting on both platforms
                for (int i = 0; i < 80; i++)
                {
                    var speed = new Vector3(2.2f, -19f, 0.8f);
                    var p = Drift(i, age + Fx.Rand(seed, i, 1) * 3f, RainMin, RainMax, speed, 300 + seed % 97);
                    WeatherStreak(fx, p, speed, 0.03f, 16f, Rgba(214, 226, 244, (int)(150 * env)), RainWidth);
                }
                for (int side = 0; side < 2; side++)
                {
                    var (centre, radius, top) = Platform(places, (BattleSide)side);
                    for (int i = 0; i < 10; i++)
                    {
                        float t0 = Fx.Rand(seed, i + side * 20, 2) * 0.7f;
                        float u = (age - t0) / 0.35f;
                        if (u < 0f || u > 1f) continue;
                        var at = OnDisc(centre, radius * 0.9f, Fx.Rand(seed, i + side * 20, 3), Fx.Rand(seed, i + side * 20, 4)) + Vector3.UnitY * (top + 0.02f);
                        fx.Flat(FxShape.Ring, at, Vector3.UnitY, radius * (0.04f + 0.14f * Fx.EaseOut(u)), Rgba(226, 236, 250, (int)(190 * (1f - u))));
                        fx.Sprite(FxShape.Drop, at + Vector3.UnitY * radius * 0.12f * MathF.Sin(u * MathF.PI), radius * 0.03f, Rgba(220, 234, 252, (int)(200 * (1f - u))), FxBlend.Alpha, depth: true);
                    }
                }
                break;
            }
            case BattleWeather.Sun:
            {
                // A warm flare, softer and longer than a move's flash, with rays slanting down across the field
                fx.ScreenFlash(Rgba(255, 226, 164), 0.22f * Fx.Envelope(age, 0f, 0.25f, 0.45f, 1.1f));
                var down = Vector3.Normalize(new Vector3(0.55f, -1f, 0.25f));
                for (int i = 0; i < 7; i++)
                {
                    var at = new Vector3(-18f + i * 5.2f + Fx.Signed(seed, i, 5) * 1.5f, 6f, -8f + Fx.Rand(seed, i, 6) * 16f);
                    float breathe = 0.7f + 0.3f * MathF.Sin(age * 5f + i);
                    fx.Streak(FxShape.Glow, at, down, 1.1f + Fx.Rand(seed, i, 7), 4.5f, Rgba(255, 228, 160, (int)(52 * env * breathe)), depth: true);
                }
                for (int i = 0; i < 24; i++)
                {
                    var p = Drift(i, age, NearMin, NearMax with { Y = 6f }, new Vector3(0.2f, 1.2f, 0f), 320 + seed % 89);
                    fx.Sprite(FxShape.Spark, p, 0.09f + Fx.Rand(seed, i, 8) * 0.06f, Rgba(255, 238, 180, (int)(220 * env)), rotation: Fx.Rand(seed, i, 9) * MathF.Tau, depth: true);
                }
                break;
            }
            case BattleWeather.Sandstorm:
            {
                // A wall of sand sweeping across from the left
                float front = SkyMin.X + (SkyMax.X - SkyMin.X + 12f) * Math.Clamp(age / BattleAnimator.WeatherCueTime, 0f, 1f);
                for (int i = 0; i < 90; i++)
                {
                    var p = new Vector3(front - Fx.Rand(seed, i, 10) * 12f, Fx.Rand(seed, i, 11) * 4.5f, SkyMin.Z + Fx.Rand(seed, i, 12) * (SkyMax.Z - SkyMin.Z));
                    WeatherStreak(fx, p, Vector3.UnitX, 0.035f + Fx.Rand(seed, i, 13) * 0.03f, 8f + Fx.Rand(seed, i, 14) * 6f,
                        Rgba(226, 196, 140, (int)(190 * env)), SandWidth);
                }
                for (int i = 0; i < 8; i++)
                {
                    var p = new Vector3(front - Fx.Rand(seed, i, 15) * 10f, 0.8f + Fx.Rand(seed, i, 16) * 2.2f, SkyMin.Z + Fx.Rand(seed, i, 17) * (SkyMax.Z - SkyMin.Z));
                    fx.Sprite(FxShape.Smoke, p, 2.2f + Fx.Rand(seed, i, 18) * 1.4f, Rgba(230, 204, 156, (int)(110 * env)), FxBlend.Alpha, Fx.Rand(seed, i, 19) * MathF.Tau, depth: true);
                }
                break;
            }
            case BattleWeather.Hail:
                // A flurry over the layer, every stone of it bouncing
                HailLayer(fx, age * 1.6f + Fx.Rand(seed, 0, 20) * 4f, places, env, 44, 600 + seed % 83);
                break;
            case BattleWeather.Fog:
            {
                // Mist rolling in low from the left
                for (int i = 0; i < 12; i++)
                {
                    float x = SkyMin.X - 6f + Fx.Rand(seed, i, 21) * 10f + age * 9f;
                    var p = new Vector3(x, 0.5f + Fx.Rand(seed, i, 22) * 1.6f, -10f + Fx.Rand(seed, i, 23) * 30f);
                    fx.Sprite(FxShape.Smoke, p, 3.2f + Fx.Rand(seed, i, 24) * 2f, Rgba(232, 236, 240, (int)(90 * env)), FxBlend.Alpha, Fx.Rand(seed, i, 25) * MathF.Tau, depth: true);
                }
                break;
            }
        }
    }

    // ------------------------------------------------------------------ the layers

    private static void RainLayer(FxList fx, float time, FxPlaces places, float k)
    {
        // Streaks falling a little toward the camera's right, as the field's rain is blown east
        for (int i = 0; i < 150; i++)
        {
            var speed = new Vector3(1.6f, -15f - R(i, 81) * 4f, 0.6f);
            var p = Drift(i, time, RainMin, RainMax, speed, 80);
            float u = (p.Y - RainMin.Y) / (RainMax.Y - RainMin.Y);
            WeatherStreak(fx, p, speed, 0.022f + R(i, 82) * 0.012f, 12f + R(i, 83) * 6f,
                Rgba(206, 218, 236, (int)(120 * k * EdgeFade(u))), RainWidth);
        }

        // Rings spreading where drops land on the two platforms and the ground between
        for (int side = 0; side < 2; side++)
        {
            var (centre, radius, top) = Platform(places, (BattleSide)side);
            int count = side == 0 ? 10 : 14;
            for (int i = 0; i < count; i++)
                Ring(fx, time, i + side * 40, centre, radius * 0.92f, top + 0.015f, radius * 0.16f, k);
        }
        var between = Vector3.Lerp(BattleStage.EnemySpot, BattleStage.PlayerSpot, 0.5f);
        for (int i = 0; i < 10; i++) Ring(fx, time, i + 100, between, 6f, 0.02f, 0.45f, k * 0.8f);

        // A grey veil over the far hills
        for (int i = 0; i < 4; i++)
        {
            var p = new Vector3(-20f + i * 11f + MathF.Sin(time * 0.1f + i) * 2f, 2.2f, -13f);
            fx.Sprite(FxShape.Smoke, p, 7f, Rgba(196, 204, 216, (int)(46 * k)), FxBlend.Alpha, R(i, 84) * MathF.Tau, depth: true);
        }
    }

    /// <summary>A ring spreading where a drop landed: a new place each time it comes round, from its own number and round.</summary>
    private static void Ring(FxList fx, float time, int i, Vector3 centre, float radius, float y, float size, float k)
    {
        float period = 0.55f + R(i, 85) * 0.45f;
        float phase = time / period + R(i, 86);
        int round = (int)MathF.Floor(phase);
        float u = phase - round;
        var at = OnDisc(centre, radius, SoftCanvas.Rand(i, round, 87), SoftCanvas.Rand(i, round, 88)) + Vector3.UnitY * y;
        fx.Flat(FxShape.Ring, at, Vector3.UnitY, size * (0.2f + 0.8f * Fx.EaseOut(u)), Rgba(222, 232, 248, (int)(150 * k * (1f - u) * (1f - u))));
    }

    private static void SunLayer(FxList fx, float time, float k)
    {
        // Warm motes drifting up through the light
        for (int i = 0; i < 40; i++)
        {
            var p = Drift(i, time, NearMin, NearMax with { Y = 7f }, new Vector3(0.12f, 0.28f + R(i, 91) * 0.2f, 0.02f), 90);
            p.X += MathF.Sin(time * 0.6f + i) * 0.3f;
            float twinkle = 0.55f + 0.45f * MathF.Sin(time * 2.3f + i * 2.7f);
            fx.Sprite(FxShape.Glow, p, 0.05f + R(i, 92) * 0.05f, Rgba(255, 226, 150, (int)(150 * k * twinkle)), depth: true);
        }
        // A warm haze lying over the far ground, breathing slowly
        for (int i = 0; i < 6; i++)
        {
            var p = new Vector3(-20f + i * 7f, 1.4f + R(i, 93) * 1.2f, -12f + R(i, 94) * 4f);
            float breathe = 0.75f + 0.25f * MathF.Sin(time * 0.35f + i * 1.9f);
            fx.Sprite(FxShape.Glow, p, 6f + R(i, 95) * 3f, Rgba(255, 210, 150, (int)(30 * k * breathe)), depth: true);
        }
        // The glare of the sun high to the left
        float pulse = 0.85f + 0.15f * MathF.Sin(time * 0.8f);
        fx.Sprite(FxShape.Glow, new Vector3(-13f, 5f, -18f), 4.5f, Rgba(255, 240, 200, (int)(70 * k * pulse)), depth: true);
    }

    private static void SandLayer(FxList fx, float time, float k)
    {
        // Thicker and faster than a desert's own drift, and higher
        var min = new Vector3(-16f, 0f, -10f);
        var max = new Vector3(10f, 4.5f, 22f);
        for (int i = 0; i < 150; i++)
        {
            var speed = new Vector3(9f + R(i, 101) * 7f, 0.3f * MathF.Sin(i), 0.5f);
            var p = Drift(i, time, min, max, speed, 100);
            p.Y += MathF.Sin(time * 3.5f + i) * 0.2f;
            float u = (p.X - min.X) / (max.X - min.X);
            WeatherStreak(fx, p, Vector3.UnitX, 0.03f + R(i, 102) * 0.03f, 7f + R(i, 103) * 7f,
                Rgba(226, 196, 140, (int)(180 * EdgeFade(u) * k)), SandWidth);
        }
        for (int i = 0; i < 10; i++)
        {
            var p = Drift(i, time, min, max, new Vector3(5f, 0.1f, 0f), 104);
            float u = (p.X - min.X) / (max.X - min.X);
            fx.Sprite(FxShape.Smoke, p + Vector3.UnitY * 0.8f, 1.8f + R(i, 105) * 1.4f, Rgba(232, 204, 156, (int)(80 * EdgeFade(u) * k)), FxBlend.Alpha, R(i, 106) * 6f, depth: true);
        }
    }

    /// <summary>
    /// Hailstones: each falls from the top of the field, strikes a platform or the ground and bounces once before it
    /// fades, landing in a new place each time it comes round.
    /// </summary>
    private static void HailLayer(FxList fx, float time, FxPlaces places, float k, int count, int salt)
    {
        var (enemy, enemyRadius, enemyTop) = Platform(places, BattleSide.Enemy);
        var (player, playerRadius, playerTop) = Platform(places, BattleSide.Player);
        const float fall = 0.62f, top = 9f;
        for (int i = 0; i < count; i++)
        {
            float period = 1.05f + R(i, 111 + salt) * 0.5f;
            float phase = time / period + R(i, 112 + salt);
            int round = (int)MathF.Floor(phase);
            float u = phase - round;
            var spot = new Vector3(SkyMin.X + SoftCanvas.Rand(i, round, 113 + salt) * (SkyMax.X - SkyMin.X - 6f), 0f,
                SkyMin.Z + SoftCanvas.Rand(i, round, 114 + salt) * (SkyMax.Z - SkyMin.Z - 4f));
            // A few of every round land on the platforms, where they are seen bouncing
            float pick = SoftCanvas.Rand(i, round, 115 + salt);
            if (pick < 0.22f) spot = OnDisc(enemy, enemyRadius * 0.9f, SoftCanvas.Rand(i, round, 116 + salt), SoftCanvas.Rand(i, round, 117 + salt));
            else if (pick < 0.32f) spot = OnDisc(player, playerRadius * 0.9f, SoftCanvas.Rand(i, round, 116 + salt), SoftCanvas.Rand(i, round, 117 + salt));
            float ground = Within(spot, enemy, enemyRadius) ? enemyTop : Within(spot, player, playerRadius) ? playerTop : 0f;
            float size = 0.05f + R(i, 118 + salt) * 0.04f;
            var drift = new Vector3(0.8f, 0f, 0.3f);
            Vector3 at;
            if (u < fall)
            {
                float f = u / fall;
                at = spot - drift * (1f - f) + Vector3.UnitY * (ground + (top - ground) * (1f - f * f));
                WeatherStreak(fx, at, new Vector3(0.1f, -1f, 0.04f), size * 0.7f, 5f, Rgba(236, 244, 255, (int)(150 * k)), HailWidth);
            }
            else
            {
                // One bounce off what it struck, a little onward, then it fades
                float b = (u - fall) / (1f - fall);
                float hop = 0.35f * (1f - 0.5f * R(i, 119 + salt));
                at = spot + drift * b * 0.4f + Vector3.UnitY * (ground + size + hop * 4f * b * (1f - b));
                fx.Sprite(FxShape.Dot, at, size, Rgba(240, 248, 255, (int)(230 * k * (1f - b * b))), FxBlend.Alpha, depth: true);
            }
        }
    }

    private static void FogLayer(FxList fx, float time, float k)
    {
        // Three layers low over the ground, the lowest the thickest, each drifting at its own pace
        for (int layer = 0; layer < 3; layer++)
        {
            float y = 0.35f + layer * 0.7f;
            float speed = 0.25f + layer * 0.18f;
            for (int i = 0; i < 10; i++)
            {
                int n = layer * 10 + i;
                var p = Drift(n, time, new Vector3(-28f, y, -12f), new Vector3(20f, y + 0.4f, 22f), new Vector3(speed, 0f, 0f), 120);
                float u = (p.X + 28f) / 48f;
                fx.Sprite(FxShape.Smoke, p, 3.2f + R(n, 121) * 2.2f, Rgba(226, 230, 236, (int)((58 - layer * 14) * k * EdgeFade(u))),
                    FxBlend.Alpha, R(n, 122) * MathF.Tau + time * 0.03f, depth: true);
            }
        }
    }

    // ------------------------------------------------------------------ places

    /// <summary>A side's platform: its middle, its radius and the height of its top, as the renderer measured them.</summary>
    private static (Vector3 Centre, float Radius, float Top) Platform(FxPlaces places, BattleSide side) => side == BattleSide.Player
        ? (BattleStage.PlayerSpot, BattleStage.PlayerPlatformRadius, places.Feet(BattleSide.Player, 0).Y)
        : (BattleStage.EnemySpot, BattleStage.EnemyPlatformRadius, places.Feet(BattleSide.Enemy, 0).Y);

    /// <summary>A point on a disc on the ground, spread evenly over it by two numbers in 0..1.</summary>
    private static Vector3 OnDisc(Vector3 centre, float radius, float a, float b)
    {
        float angle = a * MathF.Tau, r = MathF.Sqrt(b) * radius;
        return new Vector3(centre.X + MathF.Cos(angle) * r, 0f, centre.Z + MathF.Sin(angle) * r);
    }

    private static bool Within(Vector3 p, Vector3 centre, float radius) =>
        (p.X - centre.X) * (p.X - centre.X) + (p.Z - centre.Z) * (p.Z - centre.Z) <= radius * radius;
}
