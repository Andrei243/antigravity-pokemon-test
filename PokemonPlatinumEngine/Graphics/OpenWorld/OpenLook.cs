using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics.OpenWorld;

/// <summary>
/// The open world's light (plan 32 · OW8; style guide, "Open world", "The light" and "Under rock"): the battle's rig of
/// the hour with its fog drawn out to the horizon and no tilt-shift, the field's weather over it with the sky drawn
/// toward the weather's grey as a battle's is, and the cave's air eased in under rock. GPU-free.
/// </summary>
internal static class OpenLook
{
    /// <summary>The fog of the open air: from 110 m to 760 m, 78 % at the far edge (the gate's).</summary>
    public const float FogNear = 110f, FogFar = 760f, FogAmount = 0.78f;

    /// <summary>How far the land's light is the battle scenery's two-tone band (its 0.85): a little, so a slope turns from the sun gradually as real ground does.</summary>
    public const float Ramp = 0.2f;

    /// <summary>The light about the player under rock: its colour and strength, how far over the head it hangs and how far it reaches.</summary>
    public static readonly Vector3 LampColour = new Vector3(1.0f, 0.78f, 0.50f) * 1.1f;
    public const float LampAbove = 0.5f, LampReach = 18f;

    /// <summary>The light from the rock all round under it: the cave's own, warmed and lifted, since an open world's caverns are larger than a battle's stage.</summary>
    public static readonly Vector3 CaveSky = new(0.34f, 0.35f, 0.42f), CaveGround = new(0.32f, 0.28f, 0.25f);

    /// <summary>The rig of the hour in the open air.</summary>
    public static LightRig Open(float hour)
    {
        var rig = ArtLook.BattleRig(hour);
        // Lit glass glows after dark as the field's does (plan 32 · OW9's buildings): a battle's rig lights none
        var field = ArtLook.FieldRig(hour, indoors: false);
        return rig with
        {
            FogNear = FogNear, FogFar = FogFar, FogAmount = FogAmount,
            // A camera low behind the player is no diorama: no tilt-shift, no wide blur
            // and the land is no drawing: its ink lines are a quarter of a battle's (people and Pokémon keep their own
            // shells), the corners and creases are shaded deeper, and the colours sit a little quieter
            Post = rig.Post with
            {
                TiltShift = 0f, Dof = 0f, OutlineStrength = rig.Post.OutlineStrength * 0.25f,
                AoStrength = MathF.Max(rig.Post.AoStrength, 0.6f), Saturation = rig.Post.Saturation * 0.94f
            },
            LampGlow = field.LampGlow, HomeGlow = field.HomeGlow
        };
    }

    /// <summary>
    /// The rig under a weather (style guide, "Open world", "Weather"): the field's light (<see cref="ArtLook.Weathered"/>)
    /// and the sky toward the weather's grey as a battle's (<see cref="ArtLook.InBattleWeather"/>); a cloudy sky is drawn
    /// half of the way a fog's is.
    /// </summary>
    public static LightRig Weathered(LightRig rig, FieldWeather weather)
    {
        if (weather == FieldWeather.Clear) return rig;
        var lit = ArtLook.Weathered(rig, weather);
        var battle = Weathers.InBattle(weather);
        var sky = battle != Battle.Sim.BattleWeather.None ? ArtLook.InBattleWeather(rig, battle).Sky
            : weather == FieldWeather.Cloudy ? ArtLook.InBattleWeather(rig, Battle.Sim.BattleWeather.Fog, 0.5f).Sky
            : rig.Sky;
        // What closes the view in (fog, a sandstorm, heavy snow, a blizzard) hides the sky in its own colour too, and
        // the outlines of what it hides
        var post = lit.Post with { TiltShift = 0f, Dof = 0f };
        if (weather is FieldWeather.Fog or FieldWeather.Sandstorm or FieldWeather.HeavySnow or FieldWeather.Blizzard)
        {
            var fog = new Color((int)(lit.FogColor.X * 255f), (int)(lit.FogColor.Y * 255f), (int)(lit.FogColor.Z * 255f), 255);
            sky = SkyColors.Lerp(sky, new SkyColors(fog, fog, fog, fog, 0f), 0.75f);
            post = post with { OutlineStrength = post.OutlineStrength * 0.3f };
        }
        return lit with { Sky = sky, Background = sky.Horizon, Post = post };
    }

    /// <summary>
    /// The cave's air (<see cref="ArtLook.CaveRig"/>) eased over the open rig by <paramref name="share"/> (smoothed):
    /// its light from the rock all round, its haze, its grade and vignette; the sun stays the sky's, so what the sun
    /// reaches through a mouth stays lit, and the rock under it is lit by the light about the player
    /// (<see cref="LampColour"/>) instead.
    /// </summary>
    public static LightRig UnderRock(LightRig open, float share)
    {
        share = Math.Clamp(share, 0f, 1f);
        if (share <= 0f) return open;
        float s = share * share * (3f - 2f * share);
        var cave = ArtLook.CaveRig;
        var under = open with
        {
            Light = open.Light with { SkyAmbient = CaveSky, GroundAmbient = CaveGround },
            FogColor = cave.FogColor, FogAmount = cave.FogAmount, FogNear = cave.FogNear, FogFar = cave.FogFar,
            Post = cave.Post with { TiltShift = 0f, Dof = 0f },
            CloudShade = 0f, Rim = cave.Rim
        };
        return LightRig.Lerp(open, under, s);
    }

    /// <summary>The light about the player's strength under a share of rock (nothing in the open).</summary>
    public static float LampStrength(float share)
    {
        share = Math.Clamp(share, 0f, 1f);
        return share * share * (3f - 2f * share);
    }
}
