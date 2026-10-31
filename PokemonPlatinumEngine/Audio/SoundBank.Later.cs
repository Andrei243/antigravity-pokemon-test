using System;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using static PokemonPlatinumEngine.Audio.SoundDesign;

namespace PokemonPlatinumEngine.Audio;

// The later mechanics' sounds (plan 25 · Y14; docs/sound-effects.md, "Battles"): a Mega Evolution, a Primal
// Reversion, an Ultra Burst, the Z-Power and a Z-Move of each type. Our own, timed to their cues (the change of shape
// 0.8 s into a Mega Evolution, a Z-Move's landing at 0.35 s), and longer than a move's sound: set pieces of up to 2 s.
// A species' own Z-Move is heard as its type's.
public static partial class SoundBank
{
    /// <summary>The sound a Z-Move of this type makes as it sets off and lands.</summary>
    public static string ZMoveSound(PokemonType type) => "z_move_" + type.ToString().ToLowerInvariant();

    /// <summary>The sounds of the later mechanics' set pieces, which may run to 2 s (a move's own runs to 1).</summary>
    public static bool IsSetPiece(string name) =>
        name is "mega_evolve" or "primal_reversion" or "ultra_burst" or "z_power"
            or "dynamax" or "gigantamax" or "dynamax_end" or "dynamax_call" or "max_guard" or "max_land" or "terastallize" or "tera_stellar"
        || name.StartsWith("z_move_", StringComparison.OrdinalIgnoreCase) || name.StartsWith("max_move_", StringComparison.OrdinalIgnoreCase);

    /// <summary>The sound a Max Move of this type makes (a G-Max Move is heard as its type's; plan 25 · Y15).</summary>
    public static string MaxMoveSound(PokemonType type) => "max_move_" + type.ToString().ToLowerInvariant();

    /// <summary>Whether a move lands with <c>max_land</c> under it (plan Y · YD5): every Max Move and G-Max Move but Max Guard, which strikes nothing.</summary>
    public static bool LandsAsAGiant(string move) => move != "Max Guard" && IsMaxMove(move);

    /// <summary>Whether a move is a Max Move or a G-Max Move, heard by its type's Max Move sound (Max Guard by its own).</summary>
    private static bool IsMaxMove(string move) => move.Length > 0 && MoveDatabase.Get(move) is { Kind: MoveKind.MaxMove or MoveKind.GMaxMove };

    private static void AddLaterSounds(Action<string, SoundGroup, string?, string> add)
    {
        add("mega_evolve", SoundGroup.Battle, null, "A Pokémon Mega Evolves: light gathering round it, the change of shape and a ringing burst (plan 25 · Y14).");
        add("primal_reversion", SoundGroup.Battle, null, "Kyogre or Groudon undergoes Primal Reversion: a deep swell and a roaring burst.");
        add("ultra_burst", SoundGroup.Battle, null, "Necrozma Ultra Bursts: a dark drone cracking open into a blaze of light.");
        add("z_power", SoundGroup.Battle, null, "A Pokémon surrounds itself with its Z-Power: a rising hum and a chord as the Z is drawn.");
        foreach (var type in Enum.GetValues<PokemonType>())
            add(ZMoveSound(type), SoundGroup.Battle, null, $"A Z-Move of the {type} type (and a species' own Z-Move of that type) sets off and lands.");
        // Dynamax and Terastallization (plan 25 · Y15)
        add("dynamax", SoundGroup.Battle, null, "A Pokémon Dynamaxes: a deep swell rising as it grows, and a booming burst of cloud (plan 25 · Y15).");
        add("gigantamax", SoundGroup.Battle, null, "A Pokémon Gigantamaxes: the Dynamax's swell with thunder in its clouds and a bright chord.");
        add("dynamax_end", SoundGroup.Battle, null, "A Dynamax ends: a falling sigh as the Pokémon shrinks back and its clouds blow away.");
        add("dynamax_call", SoundGroup.Battle, null, "A trainer calls a Dynamax: the red beam drawing the Pokémon back, a hum swelling with the ball, a whoosh as it is thrown (plan 25 · YD1).");
        add("max_guard", SoundGroup.Battle, null, "Max Guard: a dome of light closing with a deep ring.");
        foreach (var type in Enum.GetValues<PokemonType>())
            add(MaxMoveSound(type), SoundGroup.Battle, null, $"A Max Move of the {type} type (and a G-Max Move of that type) sets off and lands.");
        // The giant's punch (plan Y · YD5): one sub-bass hit under every Max Move's impact, whatever its type
        add("max_land", SoundGroup.Battle, null, "A Max Move lands: a sub-bass hit and a deep boom felt under the type's own sound (plan Y · YD5).");
        add("terastallize", SoundGroup.Battle, null, "A Pokémon Terastallizes: crystal ringing as it forms, shattering, and a chime as the crown stands.");
        add("tera_stellar", SoundGroup.Battle, null, "A Stellar Terastallization: the crystal's ringing in a rising run of seven notes, and a shimmering chord.");
    }

    /// <summary>A sound of the later mechanics, or null for a name that isn't one.</summary>
    private static float[]? MakeLaterSound(string name)
    {
        SoundDesign d;
        switch (name)
        {
            case "mega_evolve":
                d = new SoundDesign(1.6f, 0x3E6Au);
                d.Tone(0, 0.8f, Wave.Sine, N(60), N(84), 0.14f, 0.85f, 0.6f, vibratoHz: 7f, vibratoDepth: 0.02f);
                d.Tone(0, 0.8f, Wave.Triangle, N(67), N(91), 0.07f, 0.85f, 0.6f);
                d.Noise(0.1f, 0.7f, Filter.Band, 600f, 5000f, 0.12f, 1.2f, 0.85f, 0.5f, 10f, 0.4f);
                d.Thump(0.8f, 0.5f, 160f, 40f, 0.7f, 0.6f);
                d.Bell(0.8f, 0.8f, N(84), 2.01f, 1.0f, 0.2f, 2.2f);
                d.Bell(0.82f, 0.75f, N(88), 2.01f, 0.8f, 0.14f, 2.2f);
                d.Bell(0.84f, 0.7f, N(91), 3.01f, 0.8f, 0.12f, 2.2f);
                d.Notes(1.0f, 0.06f, 0.4f, Wave.Sine, Pitches(96, 100, 103, 108), 0.05f, 0.01f);
                return Finish(d.S, 0.45f);
            case "primal_reversion":
                d = new SoundDesign(1.6f, 0x9814u);
                d.Tone(0, 0.8f, Wave.Saw, 55f, 110f, 0.12f, 0.85f, 0.5f);
                d.Noise(0, 0.8f, Filter.Low, 200f, 1600f, 0.25f, 0.9f, 0.85f, 0.5f, 6f, 0.4f);
                d.Thump(0.8f, 0.7f, 120f, 28f, 1.0f, 0.8f);
                d.Noise(0.8f, 0.8f, Filter.Band, 1200f, 300f, 0.35f, 1f, 0.01f, 1.6f, 14f, 0.5f);
                d.Bell(0.82f, 0.7f, N(72), 2.01f, 1.2f, 0.12f, 2.5f);
                return Finish(d.S, 0.48f);
            case "ultra_burst":
                d = new SoundDesign(1.8f, 0x0B57u);
                d.Tone(0, 0.85f, Wave.Saw, 70f, 90f, 0.12f, 0.6f, 0.4f, vibratoHz: 4f, vibratoDepth: 0.04f);
                d.Crackle(0.5f, 0.35f, 14, 3200f, 0.25f);
                d.Thump(0.85f, 0.6f, 180f, 30f, 0.9f, 0.8f);
                d.Noise(0.85f, 0.7f, Filter.High, 2500f, 7000f, 0.3f, 1f, 0.01f, 1.8f);
                d.Bell(0.86f, 0.9f, N(86), 2.01f, 1.2f, 0.22f, 2f);
                d.Bell(0.9f, 0.85f, N(93), 3.01f, 1.0f, 0.14f, 2f);
                d.Notes(1.05f, 0.08f, 0.5f, Wave.Sine, Pitches(98, 102, 105), 0.05f, 0.01f);
                return Finish(d.S, 0.46f);
            case "z_power":
                d = new SoundDesign(1.3f, 0x2A0Eu);
                d.Tone(0, 1.1f, Wave.Triangle, N(55), N(67), 0.16f, 0.4f, 0.7f, vibratoHz: 9f, vibratoDepth: 0.03f);
                d.Noise(0, 0.5f, Filter.Band, 300f, 2400f, 0.14f, 1.5f, 0.5f, 1f);
                d.Notes(0.35f, 0.1f, 0.6f, Wave.Sine, Pitches(79, 83, 86), 0.1f, 0.01f, 1.2f);
                d.Bell(0.65f, 0.6f, N(91), 2.01f, 0.8f, 0.12f, 2.5f);
                return Finish(d.S, 0.42f);
            case "dynamax":
            case "gigantamax":
                d = new SoundDesign(1.6f, name == "dynamax" ? 0xD7A1u : 0x6A11u);
                d.Tone(0, 0.9f, Wave.Saw, 45f, 110f, 0.14f, 0.85f, 0.4f, vibratoHz: 5f, vibratoDepth: 0.03f);
                d.Tone(0, 0.9f, Wave.Sine, N(43), N(55), 0.16f, 0.85f, 0.4f);
                d.Noise(0.1f, 0.8f, Filter.Low, 300f, 1800f, 0.2f, 1.2f, 0.85f, 0.4f, 7f, 0.4f);
                d.Thump(0.9f, 0.6f, 110f, 26f, 1.0f, 0.9f);
                d.Noise(0.9f, 0.6f, Filter.Band, 900f, 250f, 0.3f, 1f, 0.01f, 1.6f, 9f, 0.5f);
                if (name == "gigantamax")
                {
                    for (int i = 0; i < 3; i++) d.Noise(0.95f + i * 0.14f, 0.18f, Filter.High, 3000f, 6000f, 0.25f, 1.2f, 0.01f, 2f, 40f, 0.6f);
                    d.Bell(0.92f, 0.65f, N(79), 2.01f, 1.0f, 0.14f, 2.2f);
                    d.Bell(0.95f, 0.6f, N(86), 3.01f, 0.8f, 0.1f, 2.2f);
                }
                return Finish(d.S, 0.48f);
            case "dynamax_call":
                // The beam's draw (0.05 to 0.35 s), a hum swelling with the ball (to 0.65 s), the throw's whoosh at 0.82 s
                d = new SoundDesign(1.0f, 0xCA11u);
                d.Noise(0.03f, 0.35f, Filter.Band, 3200f, 900f, 0.14f, 2f, 0.3f, 0.6f, 12f, 0.3f);
                d.Tone(0.05f, 0.3f, Wave.Triangle, N(76), N(64), 0.1f, 0.2f, 0.6f);
                d.Tone(0.32f, 0.5f, Wave.Saw, N(36), N(43), 0.12f, 0.75f, 0.25f, vibratoHz: 6f, vibratoDepth: 0.04f);
                d.Tone(0.32f, 0.5f, Wave.Sine, N(48), N(55), 0.14f, 0.75f, 0.25f);
                d.Noise(0.78f, 0.2f, Filter.Band, 600f, 2400f, 0.2f, 1.2f, 0.4f, 0.8f);
                return Finish(d.S, 0.42f);
            case "dynamax_end":
                d = new SoundDesign(1.0f, 0xE4D0u);
                d.Tone(0, 0.6f, Wave.Sine, N(60), N(43), 0.16f, 0.1f, 0.8f, vibratoHz: 4f, vibratoDepth: 0.03f);
                d.Noise(0.05f, 0.8f, Filter.Band, 1600f, 300f, 0.22f, 1.4f, 0.2f, 1.2f, 5f, 0.4f);
                return Finish(d.S, 0.4f);
            case "max_guard":
                d = new SoundDesign(1.2f, 0x6A2Du);
                d.Tone(0, 0.3f, Wave.Triangle, N(48), N(60), 0.14f, 0.7f, 0.5f);
                d.Bell(0.25f, 0.9f, N(67), 2.01f, 1.4f, 0.2f, 2f);
                d.Bell(0.27f, 0.85f, N(74), 3.01f, 1.0f, 0.12f, 2f);
                d.Noise(0.25f, 0.6f, Filter.Band, 2000f, 1200f, 0.12f, 2f, 0.1f, 1.4f, 6f, 0.4f);
                return Finish(d.S, 0.42f);
            case "max_land":
                // Played at the impact (the Max Move's own blow is at 0.35 s of its sound, so this starts at once):
                // a kick of sub-bass falling under a long low boom, with a dull body in the low mids so it is heard
                // on small speakers too, and the air of the blow thrown out after it
                d = new SoundDesign(1.3f, 0x3A7Du);
                d.Thump(0, 0.45f, 95f, 32f, 1.0f, 0.35f);
                d.Tone(0, 1.25f, Wave.Sine, 52f, 30f, 0.55f, 0.01f, 1.3f);
                d.Tone(0, 0.5f, Wave.Triangle, 140f, 70f, 0.22f, 0.01f, 2.5f);
                d.Noise(0, 0.6f, Filter.Low, 700f, 150f, 0.3f, 1.4f, 0.02f, 1.8f);
                return Finish(d.S, 0.48f);
            case "terastallize":
            case "tera_stellar":
            {
                bool stellar = name == "tera_stellar";
                d = new SoundDesign(1.7f, stellar ? 0x57E1u : 0x7E2Au);
                // Crystal ringing as the facets lock on, one after another
                for (int i = 0; i < 6; i++) d.Bell(0.15f + i * 0.11f, 0.35f, N(84 + (i % 3) * 3), 3.01f, 0.6f, 0.07f, 3f);
                d.Tone(0.1f, 0.8f, Wave.Sine, N(67), N(79), 0.1f, 0.6f, 0.4f);
                // The shell shattering at 0.9 s, and the crown's chime
                d.Crackle(0.9f, 0.4f, 24, 6500f, 0.3f);
                d.Noise(0.9f, 0.3f, Filter.High, 4000f, 8000f, 0.3f, 1f, 0.01f, 2.2f);
                d.Thump(0.9f, 0.3f, 220f, 80f, 0.4f, 0.4f);
                if (stellar) d.Notes(0.95f, 0.07f, 0.5f, Wave.Sine, Pitches(84, 86, 88, 91, 93, 96, 100), 0.08f, 0.01f);
                else d.Bell(1.0f, 0.65f, N(91), 2.01f, 1.0f, 0.16f, 2.4f);
                d.Bell(1.05f, 0.6f, N(96), 3.01f, 0.8f, 0.1f, 2.4f);
                return Finish(d.S, 0.44f);
            }
        }
        if (name.StartsWith("max_move_", StringComparison.Ordinal)) return MakeMaxMoveSound(name);
        if (!name.StartsWith("z_move_", StringComparison.Ordinal)) return null;
        PokemonType? found = null;
        foreach (var type in Enum.GetValues<PokemonType>())
            if (ZMoveSound(type) == name) found = type;
        if (found is not { } t) return null;

        // Every type's: a rush gathering to the landing at 0.35 s and a great blow there; then the type's own layer
        d = new SoundDesign(1.5f, 0x2000u + (uint)t * 0x11u);
        d.Noise(0, 0.36f, Filter.Band, 400f, 3000f, 0.25f, 1.1f, 0.85f, 0.4f);
        d.Tone(0, 0.36f, Wave.Saw, 90f, 260f, 0.06f, 0.8f, 0.5f);
        d.Thump(0.35f, 0.6f, 150f, 30f, 0.9f, 0.9f);
        d.Noise(0.35f, 0.1f, Filter.Band, 1800f, 700f, 0.6f, 1f, 0.01f, 2.4f);
        TypeLayer(d, t);
        return Finish(d.S, 0.48f);
    }

    /// <summary>
    /// A Max Move of a type (plan 25 · Y15): a deep red drone swelling to the landing at 0.35 s, a blow heavier and
    /// lower than a Z-Move's with a boom under it, then the type's own layer, the same as its Z-Move's.
    /// </summary>
    private static float[]? MakeMaxMoveSound(string name)
    {
        PokemonType? found = null;
        foreach (var type in Enum.GetValues<PokemonType>())
            if (MaxMoveSound(type) == name) found = type;
        if (found is not { } t) return null;
        var d = new SoundDesign(1.5f, 0x4D00u + (uint)t * 0x13u);
        d.Tone(0, 0.36f, Wave.Saw, 40f, 120f, 0.1f, 0.8f, 0.5f, vibratoHz: 6f, vibratoDepth: 0.04f);
        d.Noise(0, 0.36f, Filter.Low, 300f, 1600f, 0.2f, 1.1f, 0.85f, 0.4f, 8f, 0.4f);
        d.Thump(0.35f, 0.75f, 90f, 22f, 1.0f, 1.0f);
        d.Noise(0.35f, 0.14f, Filter.Band, 1200f, 400f, 0.55f, 1f, 0.01f, 2.2f);
        d.Tone(0.35f, 0.6f, Wave.Sine, 70f, 40f, 0.2f, 0.05f, 1f);
        TypeLayer(d, t);
        return Finish(d.S, 0.48f);
    }

    /// <summary>A type's own layer of a Z-Move's and a Max Move's sound, after the landing at 0.35 s.</summary>
    private static void TypeLayer(SoundDesign d, PokemonType t)
    {
        switch (t)
        {
            case PokemonType.Fire:
                d.Noise(0.38f, 1.05f, Filter.Low, 1600f, 300f, 0.4f, 1f, 0.1f, 1.2f, 18f, 0.5f);
                d.Crackle(0.4f, 0.9f, 22, 1800f, 0.25f);
                break;
            case PokemonType.Water:
                d.Noise(0.38f, 1.05f, Filter.Band, 500f, 1500f, 0.35f, 1.1f, 0.1f, 1.2f, 7f, 0.6f);
                d.Bubbles(0.45f, 0.9f, 14, 200f, 700f, 0.25f);
                break;
            case PokemonType.Electric:
                for (int i = 0; i < 4; i++)
                {
                    d.Noise(0.35f + i * 0.18f, 0.2f, Filter.High, 3000f, 6000f, 0.45f, 1.2f, 0.01f, 2f, 40f, 0.6f);
                    d.Thump(0.35f + i * 0.18f, 0.25f, 220f, 60f, 0.4f, 0.4f);
                }
                break;
            case PokemonType.Grass:
                d.Noise(0.4f, 1.0f, Filter.High, 2500f, 5000f, 0.2f, 0.8f, 0.2f, 1.1f, 9f, 0.6f);
                d.Notes(0.45f, 0.12f, 0.4f, Wave.Sine, Pitches(76, 79, 83, 86, 88), 0.1f, 0.02f);
                break;
            case PokemonType.Ice:
                d.Bell(0.38f, 0.9f, N(96), 3.01f, 1.0f, 0.2f, 2.4f);
                d.Crackle(0.45f, 0.8f, 18, 5500f, 0.25f);
                break;
            case PokemonType.Fighting:
                for (int i = 0; i < 10; i++) d.Thump(0.4f + i * 0.07f, 0.12f, 200f, 90f, 0.5f, 0.7f);
                break;
            case PokemonType.Poison:
                d.Bubbles(0.4f, 1.0f, 18, 150f, 500f, 0.3f);
                d.Noise(0.4f, 1.0f, Filter.Low, 900f, 300f, 0.25f, 1.4f, 0.1f, 1.3f, 5f, 0.4f);
                break;
            case PokemonType.Ground:
                d.Noise(0.36f, 1.1f, Filter.Low, 500f, 120f, 0.5f, 1f, 0.02f, 1.2f, 12f, 0.5f);
                d.Crackle(0.4f, 0.9f, 16, 900f, 0.3f);
                break;
            case PokemonType.Flying:
                d.Noise(0.3f, 1.1f, Filter.Band, 3000f, 600f, 0.35f, 1.4f, 0.1f, 1.2f, 6f, 0.5f);
                break;
            case PokemonType.Psychic:
                d.Tone(0.38f, 1.0f, Wave.Sine, N(72), N(96), 0.16f, 0.3f, 1.2f, vibratoHz: 12f, vibratoDepth: 0.05f);
                d.Bell(0.9f, 0.55f, N(91), 2.01f, 1.0f, 0.15f, 2.4f);
                break;
            case PokemonType.Bug:
                d.Noise(0.38f, 1.0f, Filter.Band, 2200f, 2600f, 0.3f, 3f, 0.1f, 1.2f, 60f, 0.7f);
                break;
            case PokemonType.Rock:
                for (int i = 0; i < 6; i++) d.Thump(0.45f + i * 0.14f, 0.3f, 140f, 50f, 0.6f, 0.8f);
                break;
            case PokemonType.Ghost:
                d.Tone(0.38f, 1.0f, Wave.Sine, N(64), N(52), 0.18f, 0.3f, 1f, vibratoHz: 5f, vibratoDepth: 0.06f);
                d.Noise(0.4f, 1.0f, Filter.Band, 700f, 400f, 0.2f, 2f, 0.3f, 1.2f, 3f, 0.5f);
                break;
            case PokemonType.Dragon:
                d.Tone(0.38f, 1.0f, Wave.Saw, 80f, 60f, 0.12f, 0.2f, 1.2f, vibratoHz: 6f, vibratoDepth: 0.04f);
                d.Noise(0.38f, 1.0f, Filter.Low, 1400f, 400f, 0.35f, 1f, 0.1f, 1.2f, 16f, 0.5f);
                break;
            case PokemonType.Dark:
                d.Tone(0.38f, 1.05f, Wave.Sine, 90f, 45f, 0.3f, 0.2f, 1f);
                d.Noise(0.38f, 1.05f, Filter.Band, 2000f, 200f, 0.25f, 2f, 0.6f, 0.8f);
                break;
            case PokemonType.Steel:
                d.Bell(0.38f, 0.8f, N(79), 3.41f, 1.6f, 0.2f, 2.2f);
                d.Noise(0.38f, 0.9f, Filter.High, 3500f, 5000f, 0.25f, 1.6f, 0.05f, 1.5f, 30f, 0.6f);
                break;
            case PokemonType.Fairy:
                d.Notes(0.4f, 0.09f, 0.45f, Wave.Sine, Pitches(88, 91, 95, 100, 103, 107, 112), 0.08f, 0.01f);
                d.Bell(0.4f, 0.8f, N(96), 2.01f, 0.6f, 0.12f, 2.4f);
                break;
            default:
                d.Noise(0.38f, 0.9f, Filter.Band, 1200f, 400f, 0.3f, 1f, 0.05f, 1.6f);
                d.Bell(0.38f, 0.7f, N(84), 2.01f, 1.0f, 0.15f, 2.4f);
                break;
        }
    }

    /// <summary>Whether a move is a Z-Move, heard by its type's Z-Move sound.</summary>
    private static bool IsZMove(string move) => move.Length > 0 && MoveDatabase.Get(move) is { Kind: MoveKind.ZMove };
}
