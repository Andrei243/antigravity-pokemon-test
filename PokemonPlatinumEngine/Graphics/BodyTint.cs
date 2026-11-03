using System;
using System.Collections.Generic;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// A move's tint on a body (plan 25 · Y17g, the table Y16 owed; style guide, "Bodies that move with a move", "A
/// body's tint"): where the original fades a battler's sprite toward a colour (<c>Func_FadeBattlerSprite</c>), the
/// body is tinted through the shader's flash, blended into its own colours. Whose body (the user or the target),
/// the colour's name and its strength in sixteenths are read from each move's script; the colours are ours.
/// GPU-free: a pure function of the cues.
/// </summary>
/// <param name="OnUser">On the move's user from its start (else on its target from the impact, when it landed).</param>
/// <param name="Color">What the body is tinted toward.</param>
/// <param name="Amount">How far at its full, 0..1.</param>
internal readonly record struct BodyTint(bool OnUser, Color Color, float Amount);

/// <summary>Which moves tint a body, and how a tint comes and goes over a cue.</summary>
internal static class BodyTints
{
    /// <summary>The most a tint takes of a body, and of a pale one (a body is never lost in its tint).</summary>
    public const float Most = 0.45f, MostPale = 0.3f;

    /// <summary>How long a tint takes to come in, and how long after the impact a target's is gone.</summary>
    public const float InTime = 0.12f, TargetLasts = 0.4f;

    /// <summary>How long before its cue's end a user's tint is gone.</summary>
    public const float UserOut = 0.25f;

    private const float I = BattleAnimator.ImpactTime;

    // Our colours for the original's names
    private static readonly Color Black = new(14, 12, 24, 255), White = new(255, 255, 255, 255), Gray = new(128, 128, 136, 255),
        DarkGray = new(60, 58, 66, 255), Purple = new(150, 60, 200, 255), DarkPurple = new(70, 30, 110, 255),
        Red = new(232, 48, 40, 255), DarkRed = new(110, 30, 20, 255), Rust = new(150, 46, 24, 255),
        LightRed = new(255, 130, 160, 255), LightOrange = new(255, 170, 90, 255), Yellow = new(250, 214, 40, 255),
        DarkYellow = new(170, 140, 30, 255), LightYellow = new(255, 240, 150, 255), LightGreen = new(140, 230, 120, 255),
        Lime = new(190, 245, 90, 255), TealGreen = new(30, 140, 128, 255), LightBlue = new(150, 200, 255, 255),
        LightCyan = new(165, 240, 255, 255);

    /// <summary>The share of light in a colour, 0..1.</summary>
    public static float Luma(Color c) => (0.299f * c.R + 0.587f * c.G + 0.114f * c.B) / 255f;

    private static BodyTint Tint(bool onUser, Color color, int sixteenths)
    {
        float amount = Math.Min(Most * Math.Clamp(sixteenths, 0, 16) / 16f, Luma(color) > 0.8f ? MostPale : Most);
        return new BodyTint(onUser, color, amount);
    }

    private static BodyTint User(Color color, int sixteenths) => Tint(true, color, sixteenths);
    private static BodyTint Target(Color color, int sixteenths) => Tint(false, color, sixteenths);

    /// <summary>The table, by move name: the 153 of Platinum's moves whose script fades a battler's sprite.</summary>
    public static readonly IReadOnlyDictionary<string, BodyTint> Table = new Dictionary<string, BodyTint>
    {
        ["Absorb"] = User(White, 10), ["Acid"] = Target(Purple, 10), ["Acupressure"] = User(LightYellow, 10),
        ["Air Slash"] = Target(White, 14), ["Aqua Ring"] = User(White, 10), ["Aromatherapy"] = User(White, 10),
        ["Attract"] = Target(LightRed, 14), ["Avalanche"] = Target(White, 10), ["Bide"] = User(Red, 10),
        ["Blast Burn"] = Target(Red, 10), ["Blaze Kick"] = Target(White, 8), ["Brave Bird"] = Target(Red, 14),
        ["Charge Beam"] = Target(LightYellow, 10), ["Confusion"] = User(White, 4), ["Copycat"] = User(DarkYellow, 10),
        ["Cosmic Power"] = Target(Black, 16), ["Cotton Spore"] = Target(White, 10), ["Cross Poison"] = Target(Purple, 10),
        ["Curse"] = User(Red, 10), ["Dark Pulse"] = Target(DarkPurple, 14), ["Defend Order"] = User(Red, 10),
        ["Destiny Bond"] = User(White, 12), ["Detect"] = User(White, 12), ["Disable"] = Target(DarkGray, 14),
        ["Discharge"] = Target(LightYellow, 14), ["Doom Desire"] = Target(Gray, 10), ["Dragon Breath"] = Target(Red, 12),
        ["Dragon Claw"] = User(Red, 10), ["Dragon Dance"] = User(Red, 10), ["Drain Punch"] = User(White, 10),
        ["Dream Eater"] = User(White, 10), ["Dynamic Punch"] = Target(Rust, 14), ["Earth Power"] = Target(Rust, 10),
        ["Embargo"] = Target(Black, 10), ["Ember"] = Target(Red, 10), ["Endeavor"] = User(Yellow, 6),
        ["Endure"] = User(Red, 10), ["Energy Ball"] = Target(LightGreen, 14), ["Eruption"] = Target(Rust, 14),
        ["Explosion"] = User(White, 15), ["Extrasensory"] = User(TealGreen, 10), ["Facade"] = User(Red, 10),
        ["Feather Dance"] = Target(Gray, 14), ["Fire Blast"] = Target(Red, 10), ["Fire Punch"] = Target(Rust, 14),
        ["Fire Spin"] = Target(Red, 10), ["Flamethrower"] = Target(Red, 10), ["Flame Wheel"] = User(Red, 12),
        ["Flare Blitz"] = Target(Rust, 14), ["Focus Blast"] = Target(LightOrange, 10), ["Focus Energy"] = User(White, 10),
        ["Frustration"] = User(Red, 10), ["Future Sight"] = User(White, 4), ["Gastro Acid"] = Target(LightGreen, 10),
        ["Giga Drain"] = User(White, 10), ["Grudge"] = Target(DarkPurple, 14), ["Gunk Shot"] = Target(Purple, 10),
        ["Head Smash"] = Target(Red, 14), ["Healing Wish"] = User(White, 10), ["Heal Bell"] = User(White, 8),
        ["Heal Block"] = Target(Black, 10), ["Heal Order"] = User(White, 10), ["Heart Swap"] = User(LightRed, 10),
        ["Helping Hand"] = Target(Lime, 8), ["Hydro Pump"] = Target(TealGreen, 10), ["Hypnosis"] = Target(Purple, 8),
        ["Ice Ball"] = Target(White, 10), ["Ice Punch"] = Target(LightBlue, 14), ["Ice Shard"] = Target(White, 10),
        ["Icy Wind"] = Target(White, 14), ["Lava Plume"] = Target(Red, 10), ["Leech Life"] = User(White, 10),
        ["Lucky Chant"] = User(White, 10), ["Lunar Dance"] = User(White, 10), ["Luster Purge"] = Target(White, 16),
        ["Magma Storm"] = Target(Red, 10), ["Mega Drain"] = User(White, 10), ["Metal Burst"] = User(White, 10),
        ["Meteor Mash"] = Target(White, 14), ["Me First"] = User(TealGreen, 10), ["Milk Drink"] = User(White, 10),
        ["Mimic"] = User(White, 12), ["Miracle Eye"] = Target(White, 10), ["Mist"] = User(White, 10),
        ["Mist Ball"] = Target(White, 10), ["Muddy Water"] = Target(Rust, 10), ["Mud Shot"] = Target(DarkRed, 14),
        ["Mud-Slap"] = Target(Gray, 14), ["Outrage"] = Target(Red, 10), ["Overheat"] = User(Red, 10),
        ["Perish Song"] = User(Gray, 16), ["Poison Fang"] = Target(Purple, 12), ["Poison Gas"] = Target(Purple, 10),
        ["Poison Jab"] = Target(Purple, 10), ["Poison Powder"] = Target(Purple, 10), ["Poison Sting"] = Target(Purple, 10),
        ["Poison Tail"] = Target(Purple, 12), ["Powder Snow"] = Target(White, 14), ["Psybeam"] = Target(Purple, 8),
        ["Psychic"] = User(LightYellow, 10), ["Psycho Boost"] = User(White, 10), ["Psycho Shift"] = User(White, 10),
        ["Rage"] = User(Red, 10), ["Recover"] = User(LightYellow, 10), ["Recycle"] = User(White, 12),
        ["Roost"] = User(White, 10), ["Sacred Fire"] = Target(Red, 10), ["Safeguard"] = User(White, 12),
        ["Sand Tomb"] = Target(Yellow, 12), ["Shadow Ball"] = Target(DarkPurple, 14), ["Shadow Punch"] = User(Black, 10),
        ["Shadow Sneak"] = Target(Black, 14), ["Sheer Cold"] = Target(White, 10), ["Shock Wave"] = Target(LightYellow, 10),
        ["Signal Beam"] = Target(Red, 8), ["Sing"] = Target(LightRed, 14), ["Sky Attack"] = User(Black, 10),
        ["Slack Off"] = User(White, 10), ["Sleep Powder"] = Target(LightGreen, 10), ["Sludge"] = Target(Purple, 10),
        ["Sludge Bomb"] = Target(Purple, 14), ["Smog"] = Target(Black, 10), ["Soft-Boiled"] = User(White, 10),
        ["Solar Beam"] = User(LightYellow, 10), ["Spacial Rend"] = Target(White, 10), ["Spark"] = User(LightYellow, 10),
        ["Spite"] = User(White, 10), ["Spore"] = Target(LightGreen, 10), ["Stockpile"] = User(White, 16),
        ["Stun Spore"] = Target(LightYellow, 10), ["Surf"] = Target(TealGreen, 10), ["Swagger"] = Target(Red, 14),
        ["Swallow"] = User(White, 10), ["Sweet Kiss"] = Target(LightRed, 14), ["Sweet Scent"] = Target(LightRed, 12),
        ["Synthesis"] = User(LightYellow, 14), ["Taunt"] = Target(Red, 14), ["Thief"] = Target(Gray, 14),
        ["Thunder"] = Target(Black, 15), ["Thunderbolt"] = Target(Black, 15), ["Thunder Fang"] = Target(LightYellow, 14),
        ["Thunder Punch"] = Target(LightYellow, 14), ["Thunder Shock"] = Target(Black, 15), ["Thunder Wave"] = Target(LightYellow, 14),
        ["Tickle"] = User(Black, 12), ["Torment"] = Target(Red, 14), ["Toxic Spikes"] = Target(Purple, 10),
        ["Twineedle"] = Target(Purple, 10), ["Waterfall"] = Target(LightBlue, 14), ["Water Pulse"] = Target(LightCyan, 14),
        ["Whirlpool"] = Target(LightCyan, 14), ["Wish"] = User(White, 10), ["Zap Cannon"] = Target(LightYellow, 14),
    };

    /// <summary>A move cue's tint, if it has one (a charge half, an attack sent ahead and the set pieces have none).</summary>
    public static BodyTint? For(EffectCue cue)
    {
        if (cue.Kind != CueKind.Move || cue.Charging || cue.Arriving || cue.ZMove || cue.MaxMove) return null;
        return Table.TryGetValue(cue.Move, out var tint) ? tint : null;
    }

    /// <summary>How far in a cue's tint is at its shown age (0..1): a user's over the whole cue, a target's from the impact for <see cref="TargetLasts"/>.</summary>
    public static float Strength(EffectCue cue, BodyTint tint)
    {
        float age = cue.ShownAge, length = cue.Length;
        if (tint.OnUser)
        {
            if (age <= 0f || age >= length) return 0f;
            return Math.Min(Math.Clamp(age / InTime, 0f, 1f), Math.Clamp((length - age) / UserOut, 0f, 1f));
        }
        if (!cue.Landed) return 0f;
        float k = age - I;
        if (k <= 0f || k >= TargetLasts) return 0f;
        return Math.Min(Math.Clamp(k / InTime, 0f, 1f), Math.Clamp((TargetLasts - k) / 0.2f, 0f, 1f));
    }

    /// <summary>The strongest tint the cues playing lay on the body at this place (none: an amount of nought).</summary>
    public static (Color Color, float Amount) At(BattleAnimator anim, BattleSide side, int slot)
    {
        (Color Color, float Amount) strongest = (default, 0f);
        foreach (var cue in anim.Cues)
        {
            if (For(cue) is not { } tint) continue;
            bool here = tint.OnUser ? cue.FromSide == side && cue.FromSlot == slot : cue.ToSide == side && cue.ToSlot == slot;
            if (!here) continue;
            float amount = tint.Amount * Strength(cue, tint);
            if (amount > strongest.Amount) strongest = (tint.Color, amount);
        }
        return strongest;
    }
}
