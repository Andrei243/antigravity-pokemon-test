using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The stage's light while a move plays (plan 25 · Y16; style guide, "Move effects (G8)", "The stage's light"): the
/// arena, its sky and its platforms washed toward a colour by an amount and drawn toward their own grey, while the
/// Pokémon, the dolls, the balls and the effects stay in the scene's own light. It is the original's
/// <c>Func_FadeBg</c>, <c>SwitchBg</c> and <c>Func_SetBgGrayscale</c> as this game draws them: no picture is
/// switched in (the original's are art), only the light changes. GPU-free: the renderer hands the result to the
/// shaders as two uniforms (<see cref="FieldShaders.SetStageWash"/>), so it costs no pass of its own.
/// </summary>
/// <param name="Color">What the stage is washed toward, 0..1 a channel.</param>
/// <param name="Amount">How far, 0..1 (0: the stage as it is).</param>
/// <param name="Grey">How far the stage is drawn toward its own grey before the wash, 0..1.</param>
internal readonly record struct StageLight(Vector3 Color, float Amount, float Grey = 0f)
{
    public static readonly StageLight None = new(Vector3.Zero, 0f);

    /// <summary>Whether the stage is changed at all.</summary>
    public bool Any => Amount > 0.0005f || Grey > 0.0005f;

    /// <summary>The light at <paramref name="strength"/> (0..1) of the way in.</summary>
    public StageLight Scaled(float strength)
    {
        strength = Math.Clamp(strength, 0f, 1f);
        return new StageLight(Color, Amount * strength, Grey * strength);
    }

    /// <summary>
    /// <paramref name="over"/> laid over <paramref name="under"/>, as two washes one after the other come to: the
    /// colour weighed by what each adds, the amount the share neither leaves, the grey the stronger.
    /// </summary>
    public static StageLight Compose(StageLight under, StageLight over)
    {
        if (!over.Any) return under;
        if (!under.Any) return over;
        float a1 = under.Amount, a2 = over.Amount;
        float total = 1f - (1f - a1) * (1f - a2);
        var color = total <= 0f ? over.Color : (under.Color * a1 * (1f - a2) + over.Color * a2) / total;
        return new StageLight(color, total, Math.Max(under.Grey, over.Grey));
    }

    /// <summary>A colour of the stage as this light shows it (the shader's own sum: grey first, then the wash).</summary>
    public Vector3 Apply(Vector3 c)
    {
        float luma = 0.299f * c.X + 0.587f * c.Y + 0.114f * c.Z;
        var grey = Vector3.Lerp(c, new Vector3(luma), Math.Clamp(Grey, 0f, 1f));
        return Vector3.Lerp(grey, Color, Math.Clamp(Amount, 0f, 1f));
    }

    /// <summary>The same for a colour of 0..255 (the painted sky).</summary>
    public Color Apply(Color c)
    {
        var v = Apply(new Vector3(c.R, c.G, c.B) / 255f) * 255f;
        return new Color((int)Math.Clamp(MathF.Round(v.X), 0f, 255f), (int)Math.Clamp(MathF.Round(v.Y), 0f, 255f), (int)Math.Clamp(MathF.Round(v.Z), 0f, 255f), (int)c.A);
    }

    /// <summary>The painted sky in this light (its stars dim with it).</summary>
    public SkyColors Apply(SkyColors sky) => !Any ? sky :
        new SkyColors(Apply(sky.Zenith), Apply(sky.Middle), Apply(sky.Horizon), Apply(sky.CloudTint), sky.Stars * (1f - Amount));

    /// <summary>How much of the stage's own lights (crystals, lamps, windows) still burn: what the wash leaves.</summary>
    public float LightsLeft => 1f - Math.Clamp(Amount, 0f, 1f);
}

/// <summary>
/// Which moves change the stage's light, and how it comes and goes over a cue (plan 25 · Y16). The table is ours,
/// read from the original's move scripts: a fade of the background to <i>n</i>/16 toward a colour is an amount of
/// <i>n</i>/16 toward our colour of that name (black kept under <see cref="MostDark"/>, white under
/// <see cref="MostPale"/>, so a Pokémon is always read against its stage); a background switched for another picture
/// is a wash in a colour of the move's own at <see cref="Switched"/>; a background made grey is
/// <see cref="StageLight.Grey"/> with a little dark.
/// </summary>
internal static class StageLights
{
    /// <summary>How long the light takes to come in from the cue's start, and to go before its end.</summary>
    public const float InTime = 0.18f, OutTime = 0.35f;

    /// <summary>The cue's last seconds, over which the stage is back to itself.</summary>
    public const float BackBy = 0.05f;

    /// <summary>The darkest a stage is washed toward black, and the palest toward white.</summary>
    public const float MostDark = 0.7f, MostPale = 0.55f;

    /// <summary>A stage whose picture the original switches for another: the amount of its wash.</summary>
    public const float Switched = 0.6f;

    // Our colours for the original's names
    private static readonly Vector3 Black = new(0.02f, 0.02f, 0.05f), White = new(1f, 1f, 1f),
        LightGreen = new(0.55f, 0.9f, 0.5f), Lime = new(0.72f, 0.95f, 0.35f), DarkRed = new(0.36f, 0.05f, 0.03f),
        TealGreen = new(0.1f, 0.5f, 0.46f), DarkBlue = new(0.04f, 0.06f, 0.3f), PaleYellow = new(1f, 0.94f, 0.55f),
        WarmYellow = new(1f, 0.88f, 0.6f), LightBlue = new(0.6f, 0.8f, 1f), LightCyan = new(0.65f, 0.95f, 1f),
        Purple = new(0.3f, 0.08f, 0.42f), Red = new(0.62f, 0.05f, 0.05f);

    private static StageLight Fade(Vector3 color, int sixteenths)
    {
        // Toward a pale colour no further than the pale cap, toward any other no further than the dark one
        float luma = 0.299f * color.X + 0.587f * color.Y + 0.114f * color.Z;
        float amount = Math.Min(sixteenths / 16f, luma > 0.8f ? MostPale : MostDark);
        return new StageLight(color, amount);
    }

    private static StageLight Switch(float r, float g, float b, float amount = Switched) => new(new Vector3(r, g, b), amount);

    /// <summary>A stage made grey (the original's grayscale), and a little darker.</summary>
    private static readonly StageLight Greyed = new(Black, 0.15f, 1f);

    /// <summary>Every move that changes the stage's light, by its name.</summary>
    public static readonly IReadOnlyDictionary<string, StageLight> Table = new Dictionary<string, StageLight>
    {
        // Faded toward black
        ["Absorb"] = Fade(Black, 12), ["Aerial Ace"] = Fade(Black, 8), ["Amnesia"] = Fade(Black, 12), ["Aurora Beam"] = Fade(Black, 12),
        ["Calm Mind"] = Fade(Black, 12), ["Charge"] = Fade(Black, 12), ["Charge Beam"] = Fade(Black, 12), ["Confuse Ray"] = Fade(Black, 12),
        ["Curse"] = Fade(Black, 12), ["Defog"] = Fade(Black, 12), ["Destiny Bond"] = Fade(Black, 12), ["Detect"] = Fade(Black, 12),
        ["Discharge"] = Fade(Black, 12), ["Doom Desire"] = Fade(Black, 12), ["Dragon Pulse"] = Fade(Black, 12), ["Drain Punch"] = Fade(Black, 8),
        ["Dream Eater"] = Fade(Black, 12), ["Encore"] = Fade(Black, 12), ["Energy Ball"] = Fade(Black, 12), ["Extrasensory"] = Fade(Black, 12),
        ["Feint Attack"] = Fade(Black, 12), ["Flash Cannon"] = Fade(Black, 12), ["Fury Cutter"] = Fade(Black, 8), ["Future Sight"] = Fade(Black, 12),
        ["Glare"] = Fade(Black, 12), ["Grudge"] = Fade(Black, 12), ["Guillotine"] = Fade(Black, 12), ["Hail"] = Fade(Black, 12),
        ["Healing Wish"] = Fade(Black, 12), ["Hidden Power"] = Fade(Black, 12), ["Ice Shard"] = Fade(Black, 12), ["Icy Wind"] = Fade(Black, 12),
        ["Leer"] = Fade(Black, 12), ["Meditate"] = Fade(Black, 12), ["Mega Punch"] = Fade(Black, 12), ["Metal Burst"] = Fade(Black, 12),
        ["Metal Sound"] = Fade(Black, 12), ["Meteor Mash"] = Fade(Black, 12), ["Mind Reader"] = Fade(Black, 12), ["Miracle Eye"] = Fade(Black, 12),
        ["Night Shade"] = Fade(Black, 12), ["Perish Song"] = Fade(Black, 16), ["Powder Snow"] = Fade(Black, 12), ["Power Gem"] = Fade(Black, 12),
        ["Psych Up"] = Fade(Black, 12), ["Pursuit"] = Fade(Black, 12), ["Rain Dance"] = Fade(Black, 12), ["Revenge"] = Fade(Black, 12),
        ["Roar of Time"] = Fade(Black, 12), ["Rock Polish"] = Fade(Black, 12), ["Role Play"] = Fade(Black, 12), ["Scary Face"] = Fade(Black, 12),
        ["Sheer Cold"] = Fade(Black, 12), ["Shock Wave"] = Fade(Black, 12), ["Silver Wind"] = Fade(Black, 12), ["Skull Bash"] = Fade(Black, 12),
        ["Sky Attack"] = Fade(Black, 10), ["Solar Beam"] = Fade(Black, 12), ["Spite"] = Fade(Black, 12), ["String Shot"] = Fade(Black, 12),
        ["Supersonic"] = Fade(Black, 12), ["Swift"] = Fade(Black, 12), ["Switcheroo"] = Fade(Black, 12), ["Tail Glow"] = Fade(Black, 12),
        ["Teleport"] = Fade(Black, 12), ["Thunder Fang"] = Fade(Black, 12), ["Thunder Punch"] = Fade(Black, 12), ["Thunder Shock"] = Fade(Black, 12),
        ["Thunder Wave"] = Fade(Black, 12), ["Thunderbolt"] = Fade(Black, 12), ["Tri Attack"] = Fade(Black, 12), ["Trick"] = Fade(Black, 12),
        ["Zen Headbutt"] = Fade(Black, 12),
        // Faded toward white
        ["Avalanche"] = Fade(White, 12), ["Explosion"] = Fade(White, 15), ["Iron Defense"] = Fade(White, 12), ["Judgment"] = Fade(White, 15),
        ["Luster Purge"] = Fade(White, 16), ["Mist"] = Fade(White, 12), ["Refresh"] = Fade(White, 12), ["Reversal"] = Fade(White, 8),
        ["Seed Flare"] = Fade(White, 13), ["Sing"] = Fade(White, 12), ["Weather Ball"] = Fade(White, 8),
        // Faded toward a colour
        ["Aromatherapy"] = Fade(Lime, 8), ["Blast Burn"] = Fade(DarkRed, 12), ["Crabhammer"] = Fade(TealGreen, 8), ["Draco Meteor"] = Fade(DarkBlue, 12),
        ["Earth Power"] = Fade(DarkRed, 12), ["Eruption"] = Fade(DarkRed, 12), ["Fire Fang"] = Fade(DarkRed, 12), ["Fire Punch"] = Fade(DarkRed, 12),
        ["Flamethrower"] = Fade(DarkRed, 12), ["Grass Whistle"] = Fade(LightGreen, 8), ["Heal Bell"] = Fade(LightCyan, 8), ["Hyper Voice"] = Fade(PaleYellow, 8),
        ["Ice Beam"] = Fade(LightBlue, 8), ["Ice Fang"] = Fade(LightBlue, 8), ["Ice Punch"] = Fade(LightBlue, 8), ["Mean Look"] = Fade(Purple, 12),
        ["Mega Drain"] = Fade(LightGreen, 8), ["Morning Sun"] = Fade(WarmYellow, 12), ["Spark"] = Fade(PaleYellow, 8), ["Sunny Day"] = Fade(PaleYellow, 10),
        ["Outrage"] = Fade(Red, 10),
        // Made grey
        ["Dark Pulse"] = Greyed, ["Disable"] = Greyed, ["Heal Block"] = Greyed, ["Shadow Force"] = Greyed, ["Shadow Sneak"] = Greyed,
        // A picture of its own in the original: a wash in a colour of the move's
        ["Aeroblast"] = Switch(0.45f, 0.62f, 0.9f, 0.45f), ["Agility"] = Switch(0.62f, 0.7f, 0.82f, 0.35f), ["Aura Sphere"] = Switch(0.06f, 0.1f, 0.32f),
        ["Blizzard"] = Switch(0.78f, 0.88f, 1f, 0.5f), ["Brave Bird"] = Switch(0.22f, 0.26f, 0.55f, 0.55f), ["Close Combat"] = Switch(0.36f, 0.16f, 0.06f, 0.55f),
        ["Confusion"] = Switch(0.7f, 0.42f, 0.72f, 0.45f), ["Cosmic Power"] = Switch(0.05f, 0.03f, 0.16f, 0.65f), ["Dark Void"] = Switch(0.06f, 0.02f, 0.1f, MostDark),
        ["Double-Edge"] = Switch(0.18f, 0.18f, 0.2f, 0.5f), ["Dynamic Punch"] = Switch(0.36f, 0.14f, 0.04f, 0.5f), ["Extreme Speed"] = Switch(0.75f, 0.78f, 0.84f, 0.35f),
        ["Fire Blast"] = Switch(0.38f, 0.06f, 0.02f), ["Fissure"] = Switch(0.28f, 0.17f, 0.08f), ["Flare Blitz"] = Switch(0.4f, 0.08f, 0.02f),
        ["Fly"] = Switch(0.45f, 0.65f, 0.95f, 0.45f), ["Focus Blast"] = Switch(0.1f, 0.12f, 0.3f), ["Focus Punch"] = Switch(0.22f, 0.12f, 0.08f, 0.5f),
        ["Frenzy Plant"] = Switch(0.08f, 0.26f, 0.08f), ["Giga Impact"] = Switch(0.2f, 0.06f, 0.26f), ["Gunk Shot"] = Switch(0.24f, 0.08f, 0.28f, 0.55f),
        ["Head Smash"] = Switch(0.28f, 0.2f, 0.12f, 0.55f), ["Hydro Cannon"] = Switch(0.04f, 0.16f, 0.42f), ["Hydro Pump"] = Switch(0.06f, 0.2f, 0.46f),
        ["Hyper Beam"] = Switch(0.12f, 0.08f, 0.06f, 0.65f), ["Leaf Storm"] = Switch(0.22f, 0.5f, 0.18f, 0.5f), ["Lunar Dance"] = Switch(0.08f, 0.08f, 0.28f),
        ["Magma Storm"] = Switch(0.4f, 0.08f, 0.02f), ["Mega Kick"] = Switch(0.22f, 0.12f, 0.08f, 0.5f), ["Megahorn"] = Switch(0.12f, 0.24f, 0.06f, 0.55f),
        ["Mist Ball"] = Switch(0.95f, 0.88f, 0.95f, 0.45f), ["Moonlight"] = Switch(0.08f, 0.1f, 0.3f, 0.55f), ["Nightmare"] = Switch(0.1f, 0.02f, 0.14f, 0.65f),
        ["Overheat"] = Switch(0.42f, 0.1f, 0.02f), ["Power Whip"] = Switch(0.12f, 0.3f, 0.1f, 0.5f), ["Psychic"] = Switch(0.6f, 0.25f, 0.6f, 0.55f),
        ["Psycho Boost"] = Switch(0.36f, 0.12f, 0.42f), ["Rock Wrecker"] = Switch(0.3f, 0.22f, 0.12f, 0.55f), ["Sacred Fire"] = Switch(0.42f, 0.14f, 0.04f, 0.55f),
        ["Seismic Toss"] = Switch(0.45f, 0.65f, 0.95f, 0.45f), ["Self-Destruct"] = Switch(1f, 1f, 1f, 0.5f), ["Sky Uppercut"] = Switch(0.45f, 0.65f, 0.95f, 0.45f),
        ["Spacial Rend"] = Switch(0.36f, 0.12f, 0.36f), ["Superpower"] = Switch(0.36f, 0.16f, 0.06f, 0.55f), ["Thunder"] = Switch(0.05f, 0.06f, 0.18f, MostDark),
        ["Trick Room"] = Switch(0.3f, 0.12f, 0.42f, 0.45f), ["Volt Tackle"] = Switch(0.2f, 0.18f, 0.04f), ["Water Pulse"] = Switch(0.12f, 0.3f, 0.6f, 0.5f),
        ["Waterfall"] = Switch(0.12f, 0.3f, 0.6f, 0.5f), ["Whirlpool"] = Switch(0.08f, 0.24f, 0.5f, 0.55f), ["Wish"] = Switch(0.06f, 0.06f, 0.22f),
        ["Zap Cannon"] = Switch(0.14f, 0.14f, 0.06f),
    };

    /// <summary>The light a cue asks for at its full: none for a cue that isn't a move's strike or casting.</summary>
    public static StageLight For(EffectCue cue)
    {
        // A charge half, an attack sent ahead landing and the later mechanics' set pieces keep the stage as it is
        if (cue.Kind != CueKind.Move || cue.Charging || cue.Arriving || cue.ZMove || cue.MaxMove) return StageLight.None;
        return Table.TryGetValue(cue.Move, out var light) ? light : StageLight.None;
    }

    /// <summary>How far in a cue's light is at <paramref name="age"/> of <paramref name="length"/>: in, held, out, gone before the end.</summary>
    public static float Envelope(float age, float length)
    {
        if (age <= 0f || age >= length - BackBy) return 0f;
        float inward = Math.Clamp(age / InTime, 0f, 1f);
        float outward = Math.Clamp((length - BackBy - age) / OutTime, 0f, 1f);
        float k = Math.Min(inward, outward);
        return k * k * (3f - 2f * k);
    }

    /// <summary>A cue's light at its age.</summary>
    public static StageLight At(EffectCue cue) => For(cue).Scaled(Envelope(cue.ShownAge, cue.Length));

    /// <summary>The stage's light now: every cue playing, the older under the newer, a signature move's own over its move's (plan 25 · Y17).</summary>
    public static StageLight Of(BattleAnimator anim)
    {
        var light = StageLight.None;
        foreach (var cue in anim.Cues)
        {
            var lit = StageLight.Compose(light, At(cue));
            // A signature's light changes the colour over a stage already dark, never darkens it past the cap
            light = StageLight.Compose(lit, Directions.LightAt(cue));
            float most = Math.Max(MostDark, lit.Amount);
            if (light.Amount > most) light = light with { Amount = most };
        }
        return light;
    }
}
