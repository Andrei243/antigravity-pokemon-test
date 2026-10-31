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
            or "dynamax" or "gigantamax" or "dynamax_end" or "dynamax_call" or "max_guard" or "max_land" or "max_guard_hit" or "giant_fall"
            or "terastallize" or "tera_stellar"
        || name.StartsWith("z_move_", StringComparison.OrdinalIgnoreCase) || name.StartsWith("max_move_", StringComparison.OrdinalIgnoreCase)
        || name.StartsWith("gmax_", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The name a G-Max Move's own sound would go by (plan 25 · YD7): <c>gmax_</c> and its name after "G-Max", as a move's
    /// key ("G-Max Volt Crash" is <c>gmax_volt_crash</c>); empty for a move that is no G-Max Move. Only the signatures
    /// have one; the rest are heard as their type's Max Move.
    /// </summary>
    public static string GMaxSound(string move) =>
        move.StartsWith("G-Max ", StringComparison.OrdinalIgnoreCase) ? "gmax_" + MoveKey(move[6..]) : "";

    /// <summary>The G-Max Moves with a sound of their own (plan 25 · YD7, parts a and b). A property: the catalogue is built in the class's static constructor, before a field of this file would be.</summary>
    private static (string Move, string What)[] GMaxSounds => new[]
    {
        ("G-Max Wildfire", "a roar of fire springing up all round, crackling as the walls burn and dying to a hiss"),
        ("G-Max Volt Crash", "a thunderclap out of the sky on the landing, then six cracks running out along the ground"),
        ("G-Max Terror", "a hollow moan rising under the landing, a whisper of wind circling and a low thrum as the eye opens"),
        ("G-Max Resonance", "a slow chord of bells and a high shimmering drone, as of an aurora, over a cold ringing"),
        ("G-Max Replenish", "a soft burst, then berries falling and pattering round, and a bright little run as the user's side is given its berries back"),
        // Part b
        ("G-Max Chi Strike", "four heavy punches landing one after another, a ringing gold chime at the fourth and a bright shimmer as the stars circle"),
        ("G-Max Gold Rush", "a shower of coins jingling down and chinking as they land in a heap"),
        ("G-Max Cuddle", "a warm swelling chord as the hearts gather, two soft squeezes and little pops as the hearts rise"),
        ("G-Max Befuddle", "a fluttering of great wings, a glittering shimmer of falling scales and three little notes as the signs pop up"),
        ("G-Max Depletion", "an electric hum powering down in steps as the panels go out, with clicks, to a dead low drone")
    };

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
        // The Gigantamax signatures (plan 25 · YD7, part a): a G-Max Move with a set piece of its own has a sound of its own
        foreach (var (move, what) in GMaxSounds)
            add(GMaxSound(move), SoundGroup.Battle, null, $"{move} sets off and lands: the Max Moves' drone and blow, then {what} (plan 25 · YD7).");
        // The giant's punch (plan Y · YD5): one sub-bass hit under every Max Move's impact, whatever its type
        add("max_land", SoundGroup.Battle, null, "A Max Move lands: a sub-bass hit and a deep boom felt under the type's own sound (plan Y · YD5).");
        // Max Guard struck and a giant's fall (plan 25 · YD3)
        add("max_guard_hit", SoundGroup.Battle, null, "A blow meets Max Guard's dome: a hard ring of struck glass over a short thud (plan 25 · YD3).");
        add("giant_fall", SoundGroup.Battle, null, "A giant falls in on itself at its Dynamax's end, or sinks into its clouds as it faints: a heavy falling groan and a low crash (plan 25 · YD3).");
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
            case "max_guard_hit":
                // Played at the blow's impact: glass struck hard, a ring that dies quickly, and a dull thud under it
                d = new SoundDesign(0.9f, 0x6A2Eu);
                d.Thump(0, 0.25f, 180f, 70f, 0.6f, 0.6f);
                d.Bell(0, 0.8f, N(79), 2.76f, 1.6f, 0.22f, 3.2f);
                d.Bell(0.01f, 0.7f, N(86), 3.41f, 1.2f, 0.12f, 3.2f);
                d.Noise(0, 0.25f, Filter.High, 4000f, 2500f, 0.2f, 1.2f, 0.005f, 2.5f);
                return Finish(d.S, 0.42f);
            case "giant_fall":
                // A falling groan under the shudder's end, and the low crash of the collapse 0.15 s in
                d = new SoundDesign(1.3f, 0xFA11u);
                d.Tone(0, 0.7f, Wave.Saw, 110f, 40f, 0.12f, 0.1f, 0.8f, vibratoHz: 7f, vibratoDepth: 0.05f);
                d.Tone(0, 0.7f, Wave.Sine, N(43), N(31), 0.16f, 0.1f, 0.8f);
                d.Thump(0.15f, 0.7f, 90f, 26f, 1.0f, 0.6f);
                d.Noise(0.15f, 0.9f, Filter.Low, 900f, 120f, 0.35f, 1.2f, 0.02f, 1.6f, 9f, 0.4f);
                return Finish(d.S, 0.46f);
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
        if (name.StartsWith("gmax_", StringComparison.Ordinal)) return MakeGMaxSound(name);
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

    /// <summary>
    /// A G-Max Move's own sound (plan 25 · YD7): the Max Moves' red drone to the landing at 0.35 s and its blow (as
    /// <see cref="MakeMaxMoveSound"/>), then a layer of its own in place of its type's, timed to its set piece.
    /// </summary>
    private static float[]? MakeGMaxSound(string name)
    {
        SoundDesign d = null!;
        void Base(uint seed)
        {
            d = new SoundDesign(1.6f, seed);
            d.Tone(0, 0.36f, Wave.Saw, 40f, 120f, 0.1f, 0.8f, 0.5f, vibratoHz: 6f, vibratoDepth: 0.04f);
            d.Noise(0, 0.36f, Filter.Low, 300f, 1600f, 0.2f, 1.1f, 0.85f, 0.4f, 8f, 0.4f);
            d.Thump(0.35f, 0.75f, 90f, 22f, 1.0f, 1.0f);
            d.Noise(0.35f, 0.14f, Filter.Band, 1200f, 400f, 0.55f, 1f, 0.01f, 2.2f);
        }
        switch (name)
        {
            case "gmax_wildfire":
                // The walls springing up one after another, a roar under them, crackling, dying to a hiss by 1.4 s
                Base(0x6F17u);
                for (int i = 0; i < 5; i++) d.Noise(0.36f + i * 0.05f, 0.35f, Filter.Band, 500f + i * 120f, 1400f, 0.18f, 1f, 0.02f, 1.6f, 14f, 0.5f);
                d.Noise(0.4f, 1.0f, Filter.Low, 1800f, 500f, 0.35f, 1f, 0.15f, 1.2f, 20f, 0.5f);
                d.Crackle(0.45f, 0.9f, 28, 2200f, 0.28f);
                d.Noise(1.0f, 0.5f, Filter.High, 3500f, 2000f, 0.12f, 1f, 0.05f, 1.4f);
                return Finish(d.S, 0.48f);
            case "gmax_volt_crash":
                // A thunderclap at the landing, then six cracks running out one after another (0.43 s on, 0.05 s apart)
                Base(0x7C0Au);
                d.Noise(0.35f, 0.5f, Filter.High, 2500f, 5000f, 0.6f, 1f, 0.005f, 2f, 50f, 0.7f);
                d.Thump(0.36f, 0.6f, 160f, 35f, 0.7f, 0.8f);
                for (int i = 0; i < 6; i++)
                {
                    d.Noise(0.43f + i * 0.05f, 0.2f, Filter.High, 3500f, 7000f, 0.3f, 1.2f, 0.005f, 2.4f, 60f, 0.7f);
                    d.Crackle(0.43f + i * 0.05f, 0.25f, 6, 5000f, 0.2f);
                }
                d.Noise(0.7f, 0.7f, Filter.Band, 4000f, 2500f, 0.12f, 1.5f, 0.1f, 1.4f, 70f, 0.6f);
                return Finish(d.S, 0.48f);
            case "gmax_terror":
                // A hollow moan rising under the landing, a whisper circling, a low thrum as the eye opens (0.55 s)
                Base(0x7E44u);
                d.Tone(0.36f, 1.0f, Wave.Sine, N(48), N(55), 0.2f, 0.3f, 1.0f, vibratoHz: 4f, vibratoDepth: 0.07f);
                d.Tone(0.36f, 1.0f, Wave.Triangle, N(60), N(67), 0.08f, 0.3f, 1.0f, vibratoHz: 4.5f, vibratoDepth: 0.06f);
                d.Noise(0.4f, 1.0f, Filter.Band, 900f, 1600f, 0.16f, 3f, 0.3f, 1.2f, 2.6f, 0.6f);
                d.Tone(0.55f, 0.8f, Wave.Sine, 55f, 50f, 0.3f, 0.1f, 1.2f, vibratoHz: 9f, vibratoDepth: 0.03f);
                return Finish(d.S, 0.46f);
            case "gmax_resonance":
                // A slow chord of bells over a shimmering drone, a cold ring falling in at the end
                Base(0x2E50u);
                d.Bell(0.38f, 1.1f, N(79), 2.01f, 0.9f, 0.14f, 1.6f);
                d.Bell(0.5f, 1.0f, N(83), 2.01f, 0.9f, 0.12f, 1.6f);
                d.Bell(0.62f, 0.9f, N(86), 2.01f, 0.9f, 0.11f, 1.6f);
                d.Bell(0.74f, 0.8f, N(91), 3.01f, 0.8f, 0.09f, 1.8f);
                d.Tone(0.4f, 1.1f, Wave.Sine, N(91), N(91), 0.05f, 0.4f, 0.8f, vibratoHz: 7f, vibratoDepth: 0.02f);
                d.Crackle(0.6f, 0.8f, 12, 6000f, 0.15f);
                return Finish(d.S, 0.44f);
            case "gmax_replenish":
                // A soft burst, berries pattering as they land (0.6 to 0.95 s), a bright run at the user's side (0.8 s)
                Base(0x8E91u);
                d.Noise(0.36f, 0.3f, Filter.Band, 600f, 1500f, 0.2f, 1f, 0.02f, 1.6f);
                for (int i = 0; i < 9; i++) d.Thump(0.62f + i * 0.04f, 0.1f, 420f + (i % 3) * 60f, 220f, 0.3f, 0.6f);
                d.Notes(0.8f, 0.07f, 0.35f, Wave.Sine, Pitches(79, 83, 86, 91), 0.08f, 0.01f);
                d.Noise(0.8f, 0.6f, Filter.High, 2500f, 4500f, 0.08f, 0.8f, 0.2f, 1.2f, 9f, 0.5f);
                return Finish(d.S, 0.44f);
            // Part b
            case "gmax_chi_strike":
                // Four punches landing 0.07 s apart from 0.49 s, a gold chime at the fourth (0.7 s), a shimmer after
                Base(0xC415u);
                for (int i = 0; i < 4; i++)
                {
                    d.Thump(0.49f + i * 0.07f, 0.22f, 200f - i * 15f, 60f, 0.55f, 0.7f);
                    d.Noise(0.49f + i * 0.07f, 0.06f, Filter.Band, 1500f, 700f, 0.25f, 1f, 0.005f, 2.5f);
                }
                d.Bell(0.7f, 0.8f, N(84), 2.01f, 1.2f, 0.16f, 2.2f);
                d.Bell(0.72f, 0.75f, N(91), 3.01f, 0.9f, 0.1f, 2.2f);
                d.Notes(0.9f, 0.06f, 0.3f, Wave.Sine, Pitches(96, 100, 103, 108), 0.04f, 0.01f);
                return Finish(d.S, 0.48f);
            case "gmax_gold_rush":
                // Coins jingling down from 0.37 s (0.035 s apart), each chinking as it lands 0.4 s later
                Base(0x601Du);
                for (int i = 0; i < 18; i++)
                {
                    float at = 0.77f + i * 0.035f;
                    d.Bell(at, 0.22f, N(96 + (i * 5) % 9), 2.76f, 1.4f, 0.05f, 3f);
                }
                d.Crackle(0.4f, 0.5f, 18, 7000f, 0.12f);
                d.Noise(0.4f, 0.9f, Filter.High, 5000f, 7000f, 0.05f, 1f, 0.2f, 1.2f, 22f, 0.6f);
                return Finish(d.S, 0.44f);
            case "gmax_cuddle":
                // A warm chord swelling as the hearts gather, two soft squeezes (0.73, 0.9 s), little pops rising from 1.1 s
                Base(0xC0DDu);
                d.Tone(0.36f, 1.0f, Wave.Sine, N(64), N(64), 0.1f, 0.4f, 1.0f, vibratoHz: 5f, vibratoDepth: 0.01f);
                d.Tone(0.36f, 1.0f, Wave.Sine, N(68), N(68), 0.08f, 0.4f, 1.0f, vibratoHz: 5f, vibratoDepth: 0.01f);
                d.Tone(0.36f, 1.0f, Wave.Triangle, N(71), N(71), 0.05f, 0.4f, 1.0f);
                d.Thump(0.73f, 0.18f, 260f, 140f, 0.3f, 0f);
                d.Thump(0.9f, 0.18f, 260f, 140f, 0.3f, 0f);
                for (int i = 0; i < 4; i++) d.Bubble(1.1f + i * 0.06f, N(84 + i * 3), 0.12f);
                return Finish(d.S, 0.42f);
            case "gmax_befuddle":
                // Wings beating (a fluttering rush), scales glittering as they fall, three notes as the signs pop (0.9 s on)
                Base(0xBEF0u);
                d.Noise(0.36f, 0.9f, Filter.Band, 400f, 900f, 0.25f, 1.2f, 0.1f, 1.2f, 3.6f, 0.8f);
                d.Crackle(0.45f, 0.8f, 24, 8000f, 0.1f);
                d.Tone(0.45f, 0.8f, Wave.Sine, N(88), N(86), 0.04f, 0.3f, 1f, vibratoHz: 11f, vibratoDepth: 0.03f);
                d.Notes(0.9f, 0.12f, 0.25f, Wave.Triangle, Pitches(79, 86, 83), 0.1f, 0.01f);
                return Finish(d.S, 0.44f);
            case "gmax_depletion":
                // An electric hum stepping down as the panels go out (0.6 to 1.05 s), a click at each, a dead drone at the end
                Base(0xDE91u);
                for (int i = 0; i < 6; i++)
                {
                    float at = 0.52f + i * 0.09f;
                    d.Tone(at, 0.14f, Wave.Saw, 220f - i * 25f, 210f - i * 25f, 0.06f, 0.05f, 0.6f);
                    d.Crackle(0.6f + i * 0.09f, 0.01f, 1, 3000f, 0.25f);
                }
                d.Tone(0.9f, 0.6f, Wave.Saw, 70f, 45f, 0.08f, 0.1f, 1.4f);
                d.Noise(0.4f, 0.8f, Filter.Band, 2400f, 600f, 0.06f, 2f, 0.1f, 1.4f, 60f, 0.5f);
                return Finish(d.S, 0.44f);
        }
        return null;
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
