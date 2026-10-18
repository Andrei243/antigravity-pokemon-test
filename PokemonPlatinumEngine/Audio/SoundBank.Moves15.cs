using System;
using static PokemonPlatinumEngine.Audio.SoundDesign;

namespace PokemonPlatinumEngine.Audio;

// Plan 25 - Y13b: the sounds of the later games' moves, the rest of Generation 8 (Clangorous Soul to Take Heart) and all
// of Generation 9 (Tera Blast to Malignant Chain). No game's sound is the model: each is our own, built from the bank's
// shared layers by the move's type and kind (a blow, a beam, a volley, a buff, a storm) and timed to its look (the blow
// at 0.35 s), with a pitch and a seed of its own. Every one runs to at most a second.
public static partial class SoundBank
{
    /// <summary>A sound of Y13b, or null for a name that isn't one.</summary>
    private static float[]? MakeMoveSoundY13b(string name)
    {
        SoundDesign d;
        switch (name)
        {
            case "use_clangorous_soul":
                d = new SoundDesign(1.0f, 0x1872Bu);
                {
                    int R = 53;
                d.Notes(0f, 0.1f, 0.22f, Wave.Triangle, Pitches(R + 12, R + 16, R + 19, R + 24, R + 28), 0.07f, 0.03f);
                d.Tone(0f, 0.35f, Wave.Saw, N(R - 10), N(R + 2), 0.09f, 0.1f, 1.0f, vibratoHz: 18f, vibratoDepth: 0.06f);
                d.Thump(0.36f, 0.2f, 153.0f, 60f, 0.4f, 0.3f);
                d.Bell(0.5f, 0.45f, N(R + 24), 2.01f, 0.8f, 0.06f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_body_press":
                d = new SoundDesign(1.0f, 0x110C6u);
                {
                    int R = 58;
                d.Thump(0.2f, 0.15f, 190f, 50f, 0.6f, 0.6f);
                d.Thump(0.33f, 0.3f, 186.0f, 36.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 29), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_decorate":
                d = new SoundDesign(1.0f, 0x12786u);
                {
                    int R = 56;
                d.Notes(0f, 0.1f, 0.22f, Wave.Sine, Pitches(R + 12, R + 16, R + 19, R + 24, R + 28), 0.07f, 0.03f);
                d.Notes(0.4f, 0.07f, 0.14f, Wave.Triangle, Pitches(R + 24, R + 28, R + 31, R + 36), 0.05f, 0.01f);
                d.Thump(0.36f, 0.2f, 188.0f, 60f, 0.4f, 0.3f);
                d.Bell(0.5f, 0.45f, N(R + 24), 2.01f, 0.8f, 0.06f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_drum_beating":
                d = new SoundDesign(1.0f, 0x1527Du);
                {
                    int R = 59;
                d.Noise(0f, 0.4f, Filter.High, 3500f, 6500f, 0.3f, 1f, 0.1f, 1.2f, 18f, 0.5f);
                for (int i = 0; i < 4; i++) d.Thump(0.2f + i * 0.08f, 0.2f, 237.0f - i * 12f, 45f, 0.6f + i * 0.12f, 0.7f);
                d.Bell(0.55f, 0.4f, N(R + 14), 1.8f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_snap_trap":
                d = new SoundDesign(1.0f, 0x17C90u);
                {
                    int R = 54;
                d.Noise(0f, 0.4f, Filter.High, 3500f, 6500f, 0.3f, 1f, 0.1f, 1.2f, 18f, 0.5f);
                d.Thump(0.33f, 0.4f, 236.0f, 46.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_pyro_ball":
                d = new SoundDesign(1.0f, 0x1439Au);
                {
                    int R = 56;
                d.Noise(0.05f, 0.5f, Filter.Band, 500f, 2400f, 0.4f, 1f, 0.15f, 1.1f, 14f, 0.5f);
                d.Thump(0.33f, 0.35f, 226.0f, 36.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 29), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_behemoth_blade":
                d = new SoundDesign(1.0f, 0x1B131u);
                {
                    int R = 63;
                d.Bell(0.34f, 0.5f, N(R + 12), 2.76f, 1.0f, 0.09f, 3f);
                d.Thump(0.33f, 0.4f, 209.0f, 39.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 2.7f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_behemoth_bash":
                d = new SoundDesign(1.0f, 0x1857Fu);
                {
                    int R = 49;
                d.Bell(0.34f, 0.5f, N(R + 12), 2.76f, 1.0f, 0.09f, 3f);
                d.Thump(0.33f, 0.3f, 207.0f, 37.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 2.1f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_aura_wheel":
                d = new SoundDesign(1.0f, 0x138B1u);
                {
                    int R = 59;
                d.Crackle(0.0f, 0.4f, 14, 6500f, 0.4f);
                d.Thump(0.33f, 0.35f, 181.0f, 31.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 1.8f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_breaking_swipe":
                d = new SoundDesign(1.0f, 0x1EA0Au);
                {
                    int R = 46;
                d.Tone(0f, 0.35f, Wave.Saw, N(R - 10), N(R + 2), 0.09f, 0.1f, 1.0f, vibratoHz: 18f, vibratoDepth: 0.06f);
                d.Thump(0.33f, 0.3f, 222.0f, 32.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 29), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_branch_poke":
                d = new SoundDesign(1.0f, 0x1F626u);
                {
                    int R = 58;
                d.Noise(0f, 0.4f, Filter.High, 3500f, 6500f, 0.3f, 1f, 0.1f, 1.2f, 18f, 0.5f);
                d.Thump(0.33f, 0.3f, 222.0f, 32.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 29), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_overdrive":
                d = new SoundDesign(1.0f, 0x193D5u);
                {
                    int R = 55;
                d.Tone(0f, 0.35f, Wave.Sine, N(R), N(R + 12), 0.08f, 0.1f, 1.1f);
                d.Noise(0.12f, 0.5f, Filter.Band, 769.0f, 3769.0f, 0.4f, 1f, 0.08f, 1.0f, 22f, 0.5f);
                d.Crackle(0.0f, 0.4f, 14, 6500f, 0.4f);
                d.Thump(0.36f, 0.35f, 199.0f, 35f, 0.9f, 0.8f);
                d.Bell(0.45f, 0.5f, N(R + 19), 2.0f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_apple_acid":
                d = new SoundDesign(1.0f, 0x1EF00u);
                {
                    int R = 50;
                d.Tone(0f, 0.35f, Wave.Sine, N(R), N(R + 12), 0.08f, 0.1f, 1.1f);
                d.Noise(0.12f, 0.5f, Filter.Band, 884.0f, 3884.0f, 0.4f, 1f, 0.08f, 1.0f, 22f, 0.5f);
                d.Noise(0f, 0.4f, Filter.High, 3500f, 6500f, 0.3f, 1f, 0.1f, 1.2f, 18f, 0.5f);
                d.Thump(0.36f, 0.35f, 194.0f, 35f, 0.9f, 0.8f);
                d.Bell(0.45f, 0.5f, N(R + 19), 2.0f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_grav_apple":
                d = new SoundDesign(1.0f, 0x14EABu);
                {
                    int R = 53;
                d.Noise(0f, 0.4f, Filter.High, 3500f, 6500f, 0.3f, 1f, 0.1f, 1.2f, 18f, 0.5f);
                d.Thump(0.33f, 0.35f, 223.0f, 33.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 2.4f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_spirit_break":
                d = new SoundDesign(1.0f, 0x1E817u);
                {
                    int R = 61;
                d.Notes(0.4f, 0.07f, 0.14f, Wave.Triangle, Pitches(R + 24, R + 28, R + 31, R + 36), 0.05f, 0.01f);
                d.Thump(0.33f, 0.3f, 207.0f, 37.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 2.1f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_strange_steam":
                d = new SoundDesign(1.0f, 0x1366Bu);
                {
                    int R = 49;
                d.Tone(0f, 0.35f, Wave.Sine, N(R), N(R + 12), 0.08f, 0.1f, 1.1f);
                d.Noise(0.12f, 0.5f, Filter.Band, 1207.0f, 3307.0f, 0.4f, 1f, 0.08f, 1.0f, 22f, 0.5f);
                d.Notes(0.4f, 0.07f, 0.14f, Wave.Triangle, Pitches(R + 24, R + 28, R + 31, R + 36), 0.05f, 0.01f);
                d.Thump(0.36f, 0.35f, 197.0f, 35f, 0.9f, 0.8f);
                d.Bell(0.45f, 0.5f, N(R + 19), 2.0f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_life_dew":
                d = new SoundDesign(1.0f, 0x16347u);
                {
                    int R = 63;
                d.Notes(0f, 0.1f, 0.22f, Wave.Triangle, Pitches(R + 12, R + 16, R + 19, R + 26, R + 28), 0.07f, 0.03f);
                d.Bubbles(0.1f, 0.5f, 7, 300f, 1100f, 0.3f);
                d.Thump(0.36f, 0.2f, 165.0f, 60f, 0.4f, 0.3f);
                d.Bell(0.5f, 0.45f, N(R + 24), 2.01f, 0.8f, 0.06f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_obstruct":
                d = new SoundDesign(1.0f, 0x1B2A5u);
                {
                    int R = 61;
                d.Notes(0f, 0.1f, 0.22f, Wave.Triangle, Pitches(R + 12, R + 16, R + 19, R + 22, R + 28), 0.07f, 0.03f);
                d.Tone(0f, 0.4f, Wave.Saw, N(R - 14), N(R - 22), 0.09f, 0.1f, 1.1f, vibratoHz: 12f, vibratoDepth: 0.05f);
                d.Thump(0.36f, 0.2f, 171.0f, 60f, 0.4f, 0.3f);
                d.Bell(0.5f, 0.45f, N(R + 24), 2.01f, 0.8f, 0.06f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_false_surrender":
                d = new SoundDesign(1.0f, 0x1B87Eu);
                {
                    int R = 56;
                d.Tone(0f, 0.4f, Wave.Saw, N(R - 14), N(R - 22), 0.09f, 0.1f, 1.1f, vibratoHz: 12f, vibratoDepth: 0.05f);
                d.Thump(0.33f, 0.35f, 226.0f, 36.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 29), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_meteor_assault":
                d = new SoundDesign(1.0f, 0x1940Eu);
                {
                    int R = 56;
                d.Thump(0.2f, 0.15f, 190f, 50f, 0.6f, 0.6f);
                d.Thump(0.33f, 0.35f, 214.0f, 44.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 29), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_eternabeam":
                d = new SoundDesign(1.0f, 0x1891Du);
                {
                    int R = 49;
                d.Tone(0f, 0.35f, Wave.Sine, N(R), N(R + 12), 0.08f, 0.1f, 1.1f);
                d.Noise(0.12f, 0.5f, Filter.Band, 769.0f, 3769.0f, 0.4f, 1f, 0.08f, 1.0f, 22f, 0.5f);
                d.Tone(0f, 0.35f, Wave.Saw, N(R - 10), N(R + 2), 0.09f, 0.1f, 1.0f, vibratoHz: 18f, vibratoDepth: 0.06f);
                d.Thump(0.36f, 0.35f, 199.0f, 35f, 0.9f, 0.8f);
                d.Bell(0.45f, 0.5f, N(R + 19), 2.0f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_steel_beam":
                d = new SoundDesign(1.0f, 0x1DC3Bu);
                {
                    int R = 61;
                d.Tone(0f, 0.35f, Wave.Sine, N(R), N(R + 12), 0.08f, 0.1f, 1.1f);
                d.Noise(0.12f, 0.5f, Filter.Band, 847.0f, 4147.0f, 0.4f, 1f, 0.08f, 1.0f, 22f, 0.5f);
                d.Bell(0.34f, 0.5f, N(R + 12), 2.76f, 1.0f, 0.09f, 3f);
                d.Thump(0.36f, 0.35f, 197.0f, 35f, 0.9f, 0.8f);
                d.Bell(0.45f, 0.5f, N(R + 19), 2.0f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_expanding_force":
                d = new SoundDesign(1.0f, 0x1BB8Cu);
                {
                    int R = 52;
                d.Tone(0f, 0.4f, Wave.Sine, N(R + 8), N(R + 20), 0.08f, 0.08f, 1.2f);
                d.Thump(0.33f, 0.3f, 216.0f, 46.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_steel_roller":
                d = new SoundDesign(1.0f, 0x146E4u);
                {
                    int R = 48;
                d.Bell(0.34f, 0.5f, N(R + 12), 2.76f, 1.0f, 0.09f, 3f);
                d.Thump(0.33f, 0.4f, 188.0f, 38.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_scale_shot":
                d = new SoundDesign(1.0f, 0x1C2A3u);
                {
                    int R = 63;
                d.Tone(0f, 0.35f, Wave.Saw, N(R - 10), N(R + 2), 0.09f, 0.1f, 1.0f, vibratoHz: 18f, vibratoDepth: 0.06f);
                for (int i = 0; i < 4; i++) d.Thump(0.2f + i * 0.08f, 0.2f, 219.0f - i * 12f, 45f, 0.6f + i * 0.12f, 0.7f);
                d.Bell(0.55f, 0.4f, N(R + 14), 1.8f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_meteor_beam":
                d = new SoundDesign(1.0f, 0x13A64u);
                {
                    int R = 48;
                d.Tone(0f, 0.35f, Wave.Sine, N(R), N(R + 12), 0.08f, 0.1f, 1.1f);
                d.Noise(0.12f, 0.5f, Filter.Band, 1152.0f, 2952.0f, 0.4f, 1f, 0.08f, 1.0f, 22f, 0.5f);
                d.Crackle(0.36f, 0.5f, 10, 2400f, 0.4f);
                d.Thump(0.36f, 0.35f, 182.0f, 35f, 0.9f, 0.8f);
                d.Bell(0.45f, 0.5f, N(R + 19), 2.0f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_shell_side_arm":
                d = new SoundDesign(1.0f, 0x187F2u);
                {
                    int R = 58;
                d.Tone(0f, 0.35f, Wave.Sine, N(R), N(R + 12), 0.08f, 0.1f, 1.1f);
                d.Noise(0.12f, 0.5f, Filter.Band, 1198.0f, 2998.0f, 0.4f, 1f, 0.08f, 1.0f, 22f, 0.5f);
                d.Bubbles(0.1f, 0.6f, 6, 160f, 520f, 0.3f);
                d.Thump(0.36f, 0.35f, 188.0f, 35f, 0.9f, 0.8f);
                d.Bell(0.45f, 0.5f, N(R + 19), 2.0f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_misty_explosion":
                d = new SoundDesign(1.0f, 0x155E8u);
                {
                    int R = 54;
                d.Notes(0.4f, 0.07f, 0.14f, Wave.Triangle, Pitches(R + 24, R + 28, R + 31, R + 36), 0.05f, 0.01f);
                d.Thump(0.33f, 0.4f, 236.0f, 46.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_grassy_glide":
                d = new SoundDesign(1.0f, 0x13A45u);
                {
                    int R = 61;
                d.Noise(0f, 0.4f, Filter.High, 3500f, 6500f, 0.3f, 1f, 0.1f, 1.2f, 18f, 0.5f);
                d.Thump(0.33f, 0.3f, 213.0f, 43.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 2.4f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_rising_voltage":
                d = new SoundDesign(1.0f, 0x10C49u);
                {
                    int R = 57;
                d.Crackle(0.0f, 0.4f, 14, 6500f, 0.4f);
                d.Thump(0.33f, 0.4f, 185.0f, 35.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 1.5f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_terrain_pulse":
                d = new SoundDesign(1.0f, 0x183EBu);
                {
                    int R = 49;
                d.Notes(0f, 0.1f, 0.22f, Wave.Triangle, Pitches(R + 12, R + 16, R + 19, R + 22, R + 28), 0.07f, 0.03f);
                d.Noise(0.1f, 0.2f, Filter.Band, 800f, 2800f, 0.3f, 1f, 0.05f, 1.3f);
                d.Thump(0.36f, 0.2f, 153.0f, 60f, 0.4f, 0.3f);
                d.Bell(0.5f, 0.45f, N(R + 24), 2.01f, 0.8f, 0.06f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_skitter_smack":
                d = new SoundDesign(1.0f, 0x19C38u);
                {
                    int R = 56;
                d.Notes(0f, 0.04f, 0.06f, Wave.Pulse, Pitches(R + 12, R + 14, R + 12, R + 15, R + 12), 0.05f, 0.003f);
                d.Thump(0.33f, 0.35f, 196.0f, 46.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_burning_jealousy":
                d = new SoundDesign(1.0f, 0x1A5CCu);
                {
                    int R = 48;
                d.Noise(0.05f, 0.5f, Filter.Band, 500f, 2400f, 0.4f, 1f, 0.15f, 1.1f, 14f, 0.5f);
                d.Thump(0.33f, 0.4f, 236.0f, 46.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_lash_out":
                d = new SoundDesign(1.0f, 0x182C9u);
                {
                    int R = 63;
                d.Tone(0f, 0.4f, Wave.Saw, N(R - 14), N(R - 22), 0.09f, 0.1f, 1.1f, vibratoHz: 12f, vibratoDepth: 0.05f);
                for (int i = 0; i < 4; i++) d.Thump(0.2f + i * 0.08f, 0.2f, 201.0f - i * 12f, 45f, 0.6f + i * 0.12f, 0.7f);
                d.Bell(0.55f, 0.4f, N(R + 14), 1.8f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_poltergeist":
                d = new SoundDesign(1.0f, 0x15F35u);
                {
                    int R = 63;
                d.Tone(0f, 0.6f, Wave.Sine, N(R - 6), N(R - 14), 0.08f, 0.1f, 1.2f, vibratoHz: 6f, vibratoDepth: 0.08f);
                d.Thump(0.33f, 0.4f, 185.0f, 35.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 1.5f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_corrosive_gas":
                d = new SoundDesign(1.0f, 0x15F50u);
                {
                    int R = 46;
                d.Notes(0f, 0.1f, 0.22f, Wave.Sine, Pitches(R + 12, R + 16, R + 19, R + 22, R + 28), 0.07f, 0.03f);
                d.Bubbles(0.1f, 0.6f, 6, 160f, 520f, 0.3f);
                d.Thump(0.36f, 0.2f, 174.0f, 60f, 0.4f, 0.3f);
                d.Bell(0.5f, 0.45f, N(R + 24), 2.01f, 0.8f, 0.06f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_coaching":
                d = new SoundDesign(1.0f, 0x1BAEFu);
                {
                    int R = 57;
                d.Notes(0f, 0.1f, 0.22f, Wave.Triangle, Pitches(R + 12, R + 16, R + 19, R + 26, R + 28), 0.07f, 0.03f);
                d.Thump(0.2f, 0.15f, 190f, 50f, 0.6f, 0.6f);
                d.Thump(0.36f, 0.2f, 165.0f, 60f, 0.4f, 0.3f);
                d.Bell(0.5f, 0.45f, N(R + 24), 2.01f, 0.8f, 0.06f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_flip_turn":
                d = new SoundDesign(1.0f, 0x170E1u);
                {
                    int R = 49;
                d.Bubbles(0.05f, 0.5f, 7, 300f, 1100f, 0.3f);
                d.Thump(0.33f, 0.3f, 225.0f, 35.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 1.5f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_triple_axel":
                d = new SoundDesign(1.0f, 0x1EB7Fu);
                {
                    int R = 61;
                d.Bell(0.38f, 0.5f, N(R + 24), 1.41f, 1.0f, 0.08f, 3f);
                for (int i = 0; i < 4; i++) d.Thump(0.2f + i * 0.08f, 0.2f, 215.0f - i * 12f, 45f, 0.6f + i * 0.12f, 0.7f);
                d.Bell(0.55f, 0.4f, N(R + 14), 1.8f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_dual_wingbeat":
                d = new SoundDesign(1.0f, 0x1294Au);
                {
                    int R = 54;
                d.Noise(0f, 0.35f, Filter.High, 2000f, 7000f, 0.4f, 1f, 0.1f, 1.4f, 10f, 0.6f);
                for (int i = 0; i < 3; i++) d.Thump(0.2f + i * 0.08f, 0.2f, 218.0f - i * 12f, 45f, 0.6f + i * 0.12f, 0.7f);
                d.Bell(0.55f, 0.4f, N(R + 14), 1.8f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_scorching_sands":
                d = new SoundDesign(1.0f, 0x17728u);
                {
                    int R = 62;
                d.Crackle(0.36f, 0.5f, 12, 1600f, 0.4f);
                d.Thump(0.33f, 0.35f, 196.0f, 46.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_jungle_healing":
                d = new SoundDesign(1.0f, 0x1537Au);
                {
                    int R = 54;
                d.Notes(0f, 0.1f, 0.22f, Wave.Sine, Pitches(R + 12, R + 16, R + 19, R + 26, R + 28), 0.07f, 0.03f);
                d.Noise(0f, 0.4f, Filter.High, 3500f, 6500f, 0.3f, 1f, 0.1f, 1.2f, 18f, 0.5f);
                d.Thump(0.36f, 0.2f, 152.0f, 60f, 0.4f, 0.3f);
                d.Bell(0.5f, 0.45f, N(R + 24), 2.01f, 0.8f, 0.06f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_wicked_blow":
                d = new SoundDesign(1.0f, 0x1958Eu);
                {
                    int R = 58;
                d.Tone(0f, 0.4f, Wave.Saw, N(R - 14), N(R - 22), 0.09f, 0.1f, 1.1f, vibratoHz: 12f, vibratoDepth: 0.05f);
                d.Thump(0.33f, 0.3f, 210.0f, 40.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 29), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_surging_strikes":
                d = new SoundDesign(1.0f, 0x16BA5u);
                {
                    int R = 59;
                d.Bubbles(0.05f, 0.5f, 7, 300f, 1100f, 0.3f);
                for (int i = 0; i < 4; i++) d.Thump(0.2f + i * 0.08f, 0.2f, 237.0f - i * 12f, 45f, 0.6f + i * 0.12f, 0.7f);
                d.Bell(0.55f, 0.4f, N(R + 14), 1.8f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_thunder_cage":
                d = new SoundDesign(1.0f, 0x1291Bu);
                {
                    int R = 53;
                d.Crackle(0.0f, 0.4f, 14, 6500f, 0.4f);
                d.Thump(0.33f, 0.35f, 235.0f, 45.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 1.5f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_dragon_energy":
                d = new SoundDesign(1.0f, 0x14CA2u);
                {
                    int R = 52;
                d.Tone(0f, 0.35f, Wave.Sine, N(R), N(R + 12), 0.08f, 0.1f, 1.1f);
                d.Noise(0.12f, 0.5f, Filter.Band, 814.0f, 3214.0f, 0.4f, 1f, 0.08f, 1.0f, 22f, 0.5f);
                d.Tone(0f, 0.35f, Wave.Saw, N(R - 10), N(R + 2), 0.09f, 0.1f, 1.0f, vibratoHz: 18f, vibratoDepth: 0.06f);
                d.Thump(0.36f, 0.35f, 204.0f, 35f, 0.9f, 0.8f);
                d.Bell(0.45f, 0.5f, N(R + 19), 2.0f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_freezing_glare":
                d = new SoundDesign(1.0f, 0x1C7EEu);
                {
                    int R = 48;
                d.Tone(0f, 0.35f, Wave.Sine, N(R), N(R + 12), 0.08f, 0.1f, 1.1f);
                d.Noise(0.12f, 0.5f, Filter.Band, 1170.0f, 3270.0f, 0.4f, 1f, 0.08f, 1.0f, 22f, 0.5f);
                d.Tone(0f, 0.4f, Wave.Sine, N(R + 8), N(R + 20), 0.08f, 0.08f, 1.2f);
                d.Thump(0.36f, 0.35f, 200.0f, 35f, 0.9f, 0.8f);
                d.Bell(0.45f, 0.5f, N(R + 19), 2.0f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_fiery_wrath":
                d = new SoundDesign(1.0f, 0x1B542u);
                {
                    int R = 58;
                d.Tone(0f, 0.4f, Wave.Saw, N(R - 14), N(R - 22), 0.09f, 0.1f, 1.1f, vibratoHz: 12f, vibratoDepth: 0.05f);
                d.Thump(0.33f, 0.3f, 234.0f, 44.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 29), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_thunderous_kick":
                d = new SoundDesign(1.0f, 0x13EB8u);
                {
                    int R = 58;
                d.Thump(0.2f, 0.15f, 190f, 50f, 0.6f, 0.6f);
                d.Thump(0.33f, 0.3f, 192.0f, 42.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_glacial_lance":
                d = new SoundDesign(1.0f, 0x1DBD3u);
                {
                    int R = 61;
                d.Bell(0.38f, 0.5f, N(R + 24), 1.41f, 1.0f, 0.08f, 3f);
                d.Thump(0.33f, 0.3f, 195.0f, 45.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 1.5f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_astral_barrage":
                d = new SoundDesign(1.0f, 0x16012u);
                {
                    int R = 46;
                d.Tone(0f, 0.35f, Wave.Sine, N(R), N(R + 12), 0.08f, 0.1f, 1.1f);
                d.Noise(0.12f, 0.5f, Filter.Band, 1270.0f, 3370.0f, 0.4f, 1f, 0.08f, 1.0f, 22f, 0.5f);
                d.Tone(0f, 0.6f, Wave.Sine, N(R - 6), N(R - 14), 0.08f, 0.1f, 1.2f, vibratoHz: 6f, vibratoDepth: 0.08f);
                d.Thump(0.36f, 0.35f, 180.0f, 35f, 0.9f, 0.8f);
                d.Bell(0.45f, 0.5f, N(R + 19), 2.0f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_eerie_spell":
                d = new SoundDesign(1.0f, 0x18BC3u);
                {
                    int R = 49;
                d.Tone(0f, 0.4f, Wave.Sine, N(R + 8), N(R + 20), 0.08f, 0.08f, 1.2f);
                d.Thump(0.33f, 0.3f, 183.0f, 33.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 2.4f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_dire_claw":
                d = new SoundDesign(1.0f, 0x1CEF6u);
                {
                    int R = 60;
                d.Bubbles(0.1f, 0.6f, 6, 160f, 520f, 0.3f);
                d.Thump(0.33f, 0.4f, 230.0f, 40.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 29), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_psyshield_bash":
                d = new SoundDesign(1.0f, 0x1BF68u);
                {
                    int R = 50;
                d.Tone(0f, 0.4f, Wave.Sine, N(R + 8), N(R + 20), 0.08f, 0.08f, 1.2f);
                d.Thump(0.33f, 0.35f, 196.0f, 46.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_power_shift":
                d = new SoundDesign(1.0f, 0x1E34Au);
                {
                    int R = 48;
                d.Notes(0f, 0.1f, 0.22f, Wave.Sine, Pitches(R + 12, R + 16, R + 19, R + 26, R + 28), 0.07f, 0.03f);
                d.Noise(0.1f, 0.2f, Filter.Band, 800f, 2800f, 0.3f, 1f, 0.05f, 1.3f);
                d.Thump(0.36f, 0.2f, 184.0f, 60f, 0.4f, 0.3f);
                d.Bell(0.5f, 0.45f, N(R + 24), 2.01f, 0.8f, 0.06f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_stone_axe":
                d = new SoundDesign(1.0f, 0x1EB6Eu);
                {
                    int R = 52;
                d.Crackle(0.36f, 0.5f, 10, 2400f, 0.4f);
                d.Thump(0.33f, 0.3f, 210.0f, 40.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 29), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_springtide_storm":
                d = new SoundDesign(1.0f, 0x18B19u);
                {
                    int R = 63;
                d.Noise(0f, 0.9f, Filter.Band, 617.0f, 3017.0f, 0.45f, 1f, 0.2f, 1.0f, 8f, 0.6f);
                d.Notes(0.4f, 0.07f, 0.14f, Wave.Triangle, Pitches(R + 24, R + 28, R + 31, R + 36), 0.05f, 0.01f);
                d.Thump(0.35f, 0.4f, 150f, 30f, 0.9f, 0.7f);
                d.Crackle(0.4f, 0.5f, 12, 3017.0f, 0.35f);
                }
                return Finish(d.S, 0.38f);
            case "use_mystical_power":
                d = new SoundDesign(1.0f, 0x1AC3Cu);
                {
                    int R = 56;
                d.Tone(0f, 0.4f, Wave.Sine, N(R + 8), N(R + 20), 0.08f, 0.08f, 1.2f);
                d.Thump(0.33f, 0.35f, 208.0f, 38.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_raging_fury":
                d = new SoundDesign(1.0f, 0x18859u);
                {
                    int R = 47;
                d.Noise(0.05f, 0.5f, Filter.Band, 500f, 2400f, 0.4f, 1f, 0.15f, 1.1f, 14f, 0.5f);
                d.Thump(0.33f, 0.35f, 217.0f, 47.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 2.1f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_wave_crash":
                d = new SoundDesign(1.0f, 0x1A6C7u);
                {
                    int R = 63;
                d.Bubbles(0.05f, 0.5f, 7, 300f, 1100f, 0.3f);
                d.Thump(0.33f, 0.4f, 203.0f, 33.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 2.4f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_chloroblast":
                d = new SoundDesign(1.0f, 0x1DF9Eu);
                {
                    int R = 60;
                d.Tone(0f, 0.35f, Wave.Sine, N(R), N(R + 12), 0.08f, 0.1f, 1.1f);
                d.Noise(0.12f, 0.5f, Filter.Band, 858.0f, 3258.0f, 0.4f, 1f, 0.08f, 1.0f, 22f, 0.5f);
                d.Noise(0f, 0.4f, Filter.High, 3500f, 6500f, 0.3f, 1f, 0.1f, 1.2f, 18f, 0.5f);
                d.Thump(0.36f, 0.35f, 208.0f, 35f, 0.9f, 0.8f);
                d.Bell(0.45f, 0.5f, N(R + 19), 2.0f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_mountain_gale":
                d = new SoundDesign(1.0f, 0x10ED1u);
                {
                    int R = 53;
                d.Bell(0.38f, 0.5f, N(R + 24), 1.41f, 1.0f, 0.08f, 3f);
                d.Thump(0.33f, 0.35f, 229.0f, 39.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 2.7f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_victory_dance":
                d = new SoundDesign(1.0f, 0x1C87Au);
                {
                    int R = 52;
                d.Notes(0f, 0.1f, 0.22f, Wave.Sine, Pitches(R + 12, R + 16, R + 19, R + 22, R + 28), 0.07f, 0.03f);
                d.Thump(0.2f, 0.15f, 190f, 50f, 0.6f, 0.6f);
                d.Thump(0.36f, 0.2f, 168.0f, 60f, 0.4f, 0.3f);
                d.Bell(0.5f, 0.45f, N(R + 24), 2.01f, 0.8f, 0.06f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_headlong_rush":
                d = new SoundDesign(1.0f, 0x1925Du);
                {
                    int R = 63;
                d.Crackle(0.36f, 0.5f, 12, 1600f, 0.4f);
                d.Thump(0.33f, 0.4f, 197.0f, 47.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 2.1f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_barb_barrage":
                d = new SoundDesign(1.0f, 0x1EAC4u);
                {
                    int R = 62;
                d.Bubbles(0.1f, 0.6f, 6, 160f, 520f, 0.3f);
                for (int i = 0; i < 3; i++) d.Thump(0.2f + i * 0.08f, 0.2f, 228.0f - i * 12f, 45f, 0.6f + i * 0.12f, 0.7f);
                d.Bell(0.55f, 0.4f, N(R + 14), 1.8f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_esper_wing":
                d = new SoundDesign(1.0f, 0x13CF3u);
                {
                    int R = 57;
                d.Tone(0f, 0.4f, Wave.Sine, N(R + 8), N(R + 20), 0.08f, 0.08f, 1.2f);
                d.Thump(0.33f, 0.4f, 203.0f, 33.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 2.4f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_bitter_malice":
                d = new SoundDesign(1.0f, 0x189F4u);
                {
                    int R = 58;
                d.Tone(0f, 0.6f, Wave.Sine, N(R - 6), N(R - 14), 0.08f, 0.1f, 1.2f, vibratoHz: 6f, vibratoDepth: 0.08f);
                d.Thump(0.33f, 0.3f, 216.0f, 46.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_shelter":
                d = new SoundDesign(1.0f, 0x1B2F2u);
                {
                    int R = 56;
                d.Notes(0f, 0.1f, 0.22f, Wave.Sine, Pitches(R + 12, R + 16, R + 19, R + 24, R + 28), 0.07f, 0.03f);
                d.Bell(0.34f, 0.5f, N(R + 12), 2.76f, 1.0f, 0.09f, 3f);
                d.Thump(0.36f, 0.2f, 168.0f, 60f, 0.4f, 0.3f);
                d.Bell(0.5f, 0.45f, N(R + 24), 2.01f, 0.8f, 0.06f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_triple_arrows":
                d = new SoundDesign(1.0f, 0x1806Du);
                {
                    int R = 63;
                d.Thump(0.2f, 0.15f, 190f, 50f, 0.6f, 0.6f);
                for (int i = 0; i < 4; i++) d.Thump(0.2f + i * 0.08f, 0.2f, 205.0f - i * 12f, 45f, 0.6f + i * 0.12f, 0.7f);
                d.Bell(0.55f, 0.4f, N(R + 14), 1.8f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_infernal_parade":
                d = new SoundDesign(1.0f, 0x1937Fu);
                {
                    int R = 47;
                d.Tone(0f, 0.6f, Wave.Sine, N(R - 6), N(R - 14), 0.08f, 0.1f, 1.2f, vibratoHz: 6f, vibratoDepth: 0.08f);
                d.Thump(0.33f, 0.35f, 199.0f, 49.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 2.7f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_ceaseless_edge":
                d = new SoundDesign(1.0f, 0x140BEu);
                {
                    int R = 62;
                d.Tone(0f, 0.4f, Wave.Saw, N(R - 14), N(R - 22), 0.09f, 0.1f, 1.1f, vibratoHz: 12f, vibratoDepth: 0.05f);
                d.Thump(0.33f, 0.35f, 214.0f, 44.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 29), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_bleakwind_storm":
                d = new SoundDesign(1.0f, 0x16DA5u);
                {
                    int R = 47;
                d.Noise(0f, 0.9f, Filter.Band, 725.0f, 2725.0f, 0.45f, 1f, 0.2f, 1.0f, 8f, 0.6f);
                d.Noise(0f, 0.35f, Filter.High, 2000f, 7000f, 0.4f, 1f, 0.1f, 1.4f, 10f, 0.6f);
                d.Thump(0.35f, 0.4f, 150f, 30f, 0.9f, 0.7f);
                d.Crackle(0.4f, 0.5f, 12, 3325.0f, 0.35f);
                }
                return Finish(d.S, 0.38f);
            case "use_wildbolt_storm":
                d = new SoundDesign(1.0f, 0x16EFBu);
                {
                    int R = 57;
                d.Noise(0f, 0.9f, Filter.Band, 651.0f, 2651.0f, 0.45f, 1f, 0.2f, 1.0f, 8f, 0.6f);
                d.Crackle(0.0f, 0.4f, 14, 6500f, 0.4f);
                d.Thump(0.35f, 0.4f, 150f, 30f, 0.9f, 0.7f);
                d.Crackle(0.4f, 0.5f, 12, 3851.0f, 0.35f);
                }
                return Finish(d.S, 0.38f);
            case "use_sandsear_storm":
                d = new SoundDesign(1.0f, 0x10191u);
                {
                    int R = 59;
                d.Noise(0f, 0.9f, Filter.Band, 401.0f, 2401.0f, 0.45f, 1f, 0.2f, 1.0f, 8f, 0.6f);
                d.Crackle(0.36f, 0.5f, 12, 1600f, 0.4f);
                d.Thump(0.35f, 0.4f, 150f, 30f, 0.9f, 0.7f);
                d.Crackle(0.4f, 0.5f, 12, 5401.0f, 0.35f);
                }
                return Finish(d.S, 0.38f);
            case "use_lunar_blessing":
                d = new SoundDesign(1.0f, 0x1BF18u);
                {
                    int R = 52;
                d.Notes(0f, 0.1f, 0.22f, Wave.Sine, Pitches(R + 12, R + 16, R + 19, R + 22, R + 28), 0.07f, 0.03f);
                d.Tone(0f, 0.4f, Wave.Sine, N(R + 8), N(R + 20), 0.08f, 0.08f, 1.2f);
                d.Thump(0.36f, 0.2f, 150.0f, 60f, 0.4f, 0.3f);
                d.Bell(0.5f, 0.45f, N(R + 24), 2.01f, 0.8f, 0.06f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_take_heart":
                d = new SoundDesign(1.0f, 0x1B366u);
                {
                    int R = 62;
                d.Notes(0f, 0.1f, 0.22f, Wave.Sine, Pitches(R + 12, R + 16, R + 19, R + 24, R + 28), 0.07f, 0.03f);
                d.Tone(0f, 0.4f, Wave.Sine, N(R + 8), N(R + 20), 0.08f, 0.08f, 1.2f);
                d.Thump(0.36f, 0.2f, 164.0f, 60f, 0.4f, 0.3f);
                d.Bell(0.5f, 0.45f, N(R + 24), 2.01f, 0.8f, 0.06f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_tera_blast":
                d = new SoundDesign(1.0f, 0x11EC5u);
                {
                    int R = 57;
                d.Tone(0f, 0.35f, Wave.Sine, N(R), N(R + 12), 0.08f, 0.1f, 1.1f);
                d.Noise(0.12f, 0.5f, Filter.Band, 1041.0f, 3741.0f, 0.4f, 1f, 0.08f, 1.0f, 22f, 0.5f);
                d.Noise(0.1f, 0.2f, Filter.Band, 800f, 2800f, 0.3f, 1f, 0.05f, 1.3f);
                d.Thump(0.36f, 0.35f, 191.0f, 35f, 0.9f, 0.8f);
                d.Bell(0.45f, 0.5f, N(R + 19), 2.0f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_silk_trap":
                d = new SoundDesign(1.0f, 0x10DD7u);
                {
                    int R = 57;
                d.Notes(0f, 0.1f, 0.22f, Wave.Triangle, Pitches(R + 12, R + 16, R + 19, R + 26, R + 28), 0.07f, 0.03f);
                d.Notes(0f, 0.04f, 0.06f, Wave.Pulse, Pitches(R + 12, R + 14, R + 12, R + 15, R + 12), 0.05f, 0.003f);
                d.Thump(0.36f, 0.2f, 173.0f, 60f, 0.4f, 0.3f);
                d.Bell(0.5f, 0.45f, N(R + 24), 2.01f, 0.8f, 0.06f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_axe_kick":
                d = new SoundDesign(1.0f, 0x1C72Bu);
                {
                    int R = 53;
                d.Thump(0.2f, 0.15f, 190f, 50f, 0.6f, 0.6f);
                d.Thump(0.33f, 0.35f, 187.0f, 37.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 2.1f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_last_respects":
                d = new SoundDesign(1.0f, 0x184F8u);
                {
                    int R = 60;
                d.Tone(0f, 0.6f, Wave.Sine, N(R - 6), N(R - 14), 0.08f, 0.1f, 1.2f, vibratoHz: 6f, vibratoDepth: 0.08f);
                d.Thump(0.33f, 0.4f, 212.0f, 42.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_lumina_crash":
                d = new SoundDesign(1.0f, 0x185BAu);
                {
                    int R = 54;
                d.Tone(0f, 0.4f, Wave.Sine, N(R + 8), N(R + 20), 0.08f, 0.08f, 1.2f);
                d.Thump(0.33f, 0.4f, 182.0f, 32.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 29), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_order_up":
                d = new SoundDesign(1.0f, 0x10CF0u);
                {
                    int R = 46;
                d.Tone(0f, 0.35f, Wave.Saw, N(R - 10), N(R + 2), 0.09f, 0.1f, 1.0f, vibratoHz: 18f, vibratoDepth: 0.06f);
                d.Thump(0.33f, 0.3f, 192.0f, 42.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_jet_punch":
                d = new SoundDesign(1.0f, 0x1C0DCu);
                {
                    int R = 48;
                d.Bubbles(0.05f, 0.5f, 7, 300f, 1100f, 0.3f);
                d.Thump(0.33f, 0.4f, 224.0f, 34.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_spicy_extract":
                d = new SoundDesign(1.0f, 0x143E8u);
                {
                    int R = 56;
                d.Notes(0f, 0.1f, 0.22f, Wave.Sine, Pitches(R + 12, R + 16, R + 19, R + 24, R + 28), 0.07f, 0.03f);
                d.Noise(0f, 0.4f, Filter.High, 3500f, 6500f, 0.3f, 1f, 0.1f, 1.2f, 18f, 0.5f);
                d.Thump(0.36f, 0.2f, 158.0f, 60f, 0.4f, 0.3f);
                d.Bell(0.5f, 0.45f, N(R + 24), 2.01f, 0.8f, 0.06f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_spin_out":
                d = new SoundDesign(1.0f, 0x1208Du);
                {
                    int R = 59;
                d.Bell(0.34f, 0.5f, N(R + 12), 2.76f, 1.0f, 0.09f, 3f);
                d.Thump(0.33f, 0.35f, 181.0f, 31.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 1.8f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_population_bomb":
                d = new SoundDesign(1.0f, 0x1D04Au);
                {
                    int R = 54;
                d.Noise(0.1f, 0.2f, Filter.Band, 800f, 2800f, 0.3f, 1f, 0.05f, 1.3f);
                for (int i = 0; i < 3; i++) d.Thump(0.2f + i * 0.08f, 0.2f, 234.0f - i * 12f, 45f, 0.6f + i * 0.12f, 0.7f);
                d.Bell(0.55f, 0.4f, N(R + 14), 1.8f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_ice_spinner":
                d = new SoundDesign(1.0f, 0x12A25u);
                {
                    int R = 51;
                d.Bell(0.38f, 0.5f, N(R + 24), 1.41f, 1.0f, 0.08f, 3f);
                d.Thump(0.33f, 0.4f, 197.0f, 47.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 2.1f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_glaive_rush":
                d = new SoundDesign(1.0f, 0x1B7F1u);
                {
                    int R = 47;
                d.Tone(0f, 0.35f, Wave.Saw, N(R - 10), N(R + 2), 0.09f, 0.1f, 1.0f, vibratoHz: 18f, vibratoDepth: 0.06f);
                d.Thump(0.33f, 0.35f, 193.0f, 43.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 2.4f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_revival_blessing":
                d = new SoundDesign(1.0f, 0x156E9u);
                {
                    int R = 49;
                d.Notes(0f, 0.1f, 0.22f, Wave.Triangle, Pitches(R + 12, R + 16, R + 19, R + 22, R + 28), 0.07f, 0.03f);
                d.Noise(0.1f, 0.2f, Filter.Band, 800f, 2800f, 0.3f, 1f, 0.05f, 1.3f);
                d.Thump(0.36f, 0.2f, 151.0f, 60f, 0.4f, 0.3f);
                d.Bell(0.5f, 0.45f, N(R + 24), 2.01f, 0.8f, 0.06f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_salt_cure":
                d = new SoundDesign(1.0f, 0x19F36u);
                {
                    int R = 52;
                d.Crackle(0.36f, 0.5f, 10, 2400f, 0.4f);
                d.Thump(0.33f, 0.3f, 210.0f, 40.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 29), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_triple_dive":
                d = new SoundDesign(1.0f, 0x1A36Du);
                {
                    int R = 49;
                d.Bubbles(0.05f, 0.5f, 7, 300f, 1100f, 0.3f);
                for (int i = 0; i < 4; i++) d.Thump(0.2f + i * 0.08f, 0.2f, 237.0f - i * 12f, 45f, 0.6f + i * 0.12f, 0.7f);
                d.Bell(0.55f, 0.4f, N(R + 14), 1.8f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_mortal_spin":
                d = new SoundDesign(1.0f, 0x108A2u);
                {
                    int R = 48;
                d.Bubbles(0.1f, 0.6f, 6, 160f, 520f, 0.3f);
                d.Thump(0.33f, 0.4f, 218.0f, 48.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 29), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_doodle":
                d = new SoundDesign(1.0f, 0x1DF24u);
                {
                    int R = 62;
                d.Notes(0f, 0.1f, 0.22f, Wave.Sine, Pitches(R + 12, R + 16, R + 19, R + 24, R + 28), 0.07f, 0.03f);
                d.Noise(0.1f, 0.2f, Filter.Band, 800f, 2800f, 0.3f, 1f, 0.05f, 1.3f);
                d.Thump(0.36f, 0.2f, 162.0f, 60f, 0.4f, 0.3f);
                d.Bell(0.5f, 0.45f, N(R + 24), 2.01f, 0.8f, 0.06f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_fillet_away":
                d = new SoundDesign(1.0f, 0x1A795u);
                {
                    int R = 55;
                d.Notes(0f, 0.1f, 0.22f, Wave.Triangle, Pitches(R + 12, R + 16, R + 19, R + 22, R + 28), 0.07f, 0.03f);
                d.Noise(0.1f, 0.2f, Filter.Band, 800f, 2800f, 0.3f, 1f, 0.05f, 1.3f);
                d.Thump(0.36f, 0.2f, 163.0f, 60f, 0.4f, 0.3f);
                d.Bell(0.5f, 0.45f, N(R + 24), 2.01f, 0.8f, 0.06f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_kowtow_cleave":
                d = new SoundDesign(1.0f, 0x1D2FAu);
                {
                    int R = 62;
                d.Tone(0f, 0.4f, Wave.Saw, N(R - 14), N(R - 22), 0.09f, 0.1f, 1.1f, vibratoHz: 12f, vibratoDepth: 0.05f);
                d.Thump(0.33f, 0.35f, 202.0f, 32.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 29), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_flower_trick":
                d = new SoundDesign(1.0f, 0x10FC9u);
                {
                    int R = 57;
                d.Noise(0f, 0.4f, Filter.High, 3500f, 6500f, 0.3f, 1f, 0.1f, 1.2f, 18f, 0.5f);
                d.Thump(0.33f, 0.4f, 185.0f, 35.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 1.5f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_torch_song":
                d = new SoundDesign(1.0f, 0x1606Au);
                {
                    int R = 48;
                d.Tone(0f, 0.35f, Wave.Sine, N(R), N(R + 12), 0.08f, 0.1f, 1.1f);
                d.Noise(0.12f, 0.5f, Filter.Band, 870.0f, 3270.0f, 0.4f, 1f, 0.08f, 1.0f, 22f, 0.5f);
                d.Noise(0.15f, 0.4f, Filter.Band, 500f, 2400f, 0.4f, 1f, 0.15f, 1.1f, 14f, 0.5f);
                d.Thump(0.36f, 0.35f, 180.0f, 35f, 0.9f, 0.8f);
                d.Bell(0.45f, 0.5f, N(R + 19), 2.0f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_aqua_step":
                d = new SoundDesign(1.0f, 0x18E7Du);
                {
                    int R = 57;
                d.Bubbles(0.05f, 0.5f, 7, 300f, 1100f, 0.3f);
                d.Thump(0.33f, 0.4f, 221.0f, 31.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 1.8f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_raging_bull":
                d = new SoundDesign(1.0f, 0x15290u);
                {
                    int R = 50;
                d.Noise(0.1f, 0.2f, Filter.Band, 800f, 2800f, 0.3f, 1f, 0.05f, 1.3f);
                d.Thump(0.33f, 0.35f, 184.0f, 34.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_make_it_rain":
                d = new SoundDesign(1.0f, 0x13F76u);
                {
                    int R = 52;
                d.Bell(0.34f, 0.5f, N(R + 12), 2.76f, 1.0f, 0.09f, 3f);
                d.Thump(0.33f, 0.3f, 186.0f, 36.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 29), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_psyblade":
                d = new SoundDesign(1.0f, 0x1EA93u);
                {
                    int R = 57;
                d.Tone(0f, 0.4f, Wave.Sine, N(R + 8), N(R + 20), 0.08f, 0.08f, 1.2f);
                d.Thump(0.33f, 0.4f, 191.0f, 41.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 1.8f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_hydro_steam":
                d = new SoundDesign(1.0f, 0x15D85u);
                {
                    int R = 49;
                d.Tone(0f, 0.35f, Wave.Sine, N(R), N(R + 12), 0.08f, 0.1f, 1.1f);
                d.Noise(0.12f, 0.5f, Filter.Band, 1057.0f, 3457.0f, 0.4f, 1f, 0.08f, 1.0f, 22f, 0.5f);
                d.Bubbles(0.15f, 0.4f, 7, 300f, 1100f, 0.3f);
                d.Thump(0.36f, 0.35f, 207.0f, 35f, 0.9f, 0.8f);
                d.Bell(0.45f, 0.5f, N(R + 19), 2.0f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_ruination":
                d = new SoundDesign(1.0f, 0x18144u);
                {
                    int R = 54;
                d.Tone(0f, 0.4f, Wave.Saw, N(R - 14), N(R - 22), 0.09f, 0.1f, 1.1f, vibratoHz: 12f, vibratoDepth: 0.05f);
                d.Thump(0.33f, 0.4f, 188.0f, 38.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_collision_course":
                d = new SoundDesign(1.0f, 0x1A9E0u);
                {
                    int R = 54;
                d.Thump(0.2f, 0.15f, 190f, 50f, 0.6f, 0.6f);
                d.Thump(0.33f, 0.4f, 212.0f, 42.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_electro_drift":
                d = new SoundDesign(1.0f, 0x1DE3Cu);
                {
                    int R = 54;
                d.Crackle(0.0f, 0.4f, 14, 6500f, 0.4f);
                d.Thump(0.33f, 0.4f, 212.0f, 42.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_shed_tail":
                d = new SoundDesign(1.0f, 0x1EB6Bu);
                {
                    int R = 61;
                d.Notes(0f, 0.1f, 0.22f, Wave.Triangle, Pitches(R + 12, R + 16, R + 19, R + 22, R + 28), 0.07f, 0.03f);
                d.Noise(0.1f, 0.2f, Filter.Band, 800f, 2800f, 0.3f, 1f, 0.05f, 1.3f);
                d.Thump(0.36f, 0.2f, 169.0f, 60f, 0.4f, 0.3f);
                d.Bell(0.5f, 0.45f, N(R + 24), 2.01f, 0.8f, 0.06f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_chilly_reception":
                d = new SoundDesign(1.0f, 0x11349u);
                {
                    int R = 49;
                d.Notes(0f, 0.1f, 0.22f, Wave.Triangle, Pitches(R + 12, R + 16, R + 19, R + 22, R + 28), 0.07f, 0.03f);
                d.Bell(0.38f, 0.5f, N(R + 24), 1.41f, 1.0f, 0.08f, 3f);
                d.Thump(0.36f, 0.2f, 167.0f, 60f, 0.4f, 0.3f);
                d.Bell(0.5f, 0.45f, N(R + 24), 2.01f, 0.8f, 0.06f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_tidy_up":
                d = new SoundDesign(1.0f, 0x1005Au);
                {
                    int R = 58;
                d.Notes(0f, 0.1f, 0.22f, Wave.Sine, Pitches(R + 12, R + 16, R + 19, R + 22, R + 28), 0.07f, 0.03f);
                d.Noise(0.1f, 0.2f, Filter.Band, 800f, 2800f, 0.3f, 1f, 0.05f, 1.3f);
                d.Thump(0.36f, 0.2f, 184.0f, 60f, 0.4f, 0.3f);
                d.Bell(0.5f, 0.45f, N(R + 24), 2.01f, 0.8f, 0.06f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_snowscape":
                d = new SoundDesign(1.0f, 0x11DC4u);
                {
                    int R = 60;
                d.Notes(0f, 0.1f, 0.22f, Wave.Sine, Pitches(R + 12, R + 16, R + 19, R + 26, R + 28), 0.07f, 0.03f);
                d.Bell(0.38f, 0.5f, N(R + 24), 1.41f, 1.0f, 0.08f, 3f);
                d.Thump(0.36f, 0.2f, 178.0f, 60f, 0.4f, 0.3f);
                d.Bell(0.5f, 0.45f, N(R + 24), 2.01f, 0.8f, 0.06f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_pounce":
                d = new SoundDesign(1.0f, 0x1784Fu);
                {
                    int R = 53;
                d.Notes(0f, 0.04f, 0.06f, Wave.Pulse, Pitches(R + 12, R + 14, R + 12, R + 15, R + 12), 0.05f, 0.003f);
                d.Thump(0.33f, 0.35f, 187.0f, 37.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 2.1f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_trailblaze":
                d = new SoundDesign(1.0f, 0x19D8Du);
                {
                    int R = 61;
                d.Noise(0f, 0.4f, Filter.High, 3500f, 6500f, 0.3f, 1f, 0.1f, 1.2f, 18f, 0.5f);
                d.Thump(0.33f, 0.3f, 213.0f, 43.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 2.4f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_chilling_water":
                d = new SoundDesign(1.0f, 0x1BD0Cu);
                {
                    int R = 50;
                d.Tone(0f, 0.35f, Wave.Sine, N(R), N(R + 12), 0.08f, 0.1f, 1.1f);
                d.Noise(0.12f, 0.5f, Filter.Band, 1016.0f, 3416.0f, 0.4f, 1f, 0.08f, 1.0f, 22f, 0.5f);
                d.Bubbles(0.15f, 0.4f, 7, 300f, 1100f, 0.3f);
                d.Thump(0.36f, 0.35f, 206.0f, 35f, 0.9f, 0.8f);
                d.Bell(0.45f, 0.5f, N(R + 19), 2.0f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_hyper_drill":
                d = new SoundDesign(1.0f, 0x12124u);
                {
                    int R = 62;
                d.Noise(0.1f, 0.2f, Filter.Band, 800f, 2800f, 0.3f, 1f, 0.05f, 1.3f);
                d.Thump(0.33f, 0.35f, 220.0f, 30.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_twin_beam":
                d = new SoundDesign(1.0f, 0x1325Eu);
                {
                    int R = 46;
                d.Tone(0f, 0.35f, Wave.Sine, N(R), N(R + 12), 0.08f, 0.1f, 1.1f);
                d.Noise(0.12f, 0.5f, Filter.Band, 946.0f, 3346.0f, 0.4f, 1f, 0.08f, 1.0f, 22f, 0.5f);
                d.Tone(0f, 0.4f, Wave.Sine, N(R + 8), N(R + 20), 0.08f, 0.08f, 1.2f);
                d.Thump(0.36f, 0.35f, 176.0f, 35f, 0.9f, 0.8f);
                d.Bell(0.45f, 0.5f, N(R + 19), 2.0f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_rage_fist":
                d = new SoundDesign(1.0f, 0x1E39Au);
                {
                    int R = 50;
                d.Tone(0f, 0.6f, Wave.Sine, N(R - 6), N(R - 14), 0.08f, 0.1f, 1.2f, vibratoHz: 6f, vibratoDepth: 0.08f);
                d.Thump(0.33f, 0.35f, 238.0f, 48.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 29), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_armor_cannon":
                d = new SoundDesign(1.0f, 0x16499u);
                {
                    int R = 47;
                d.Tone(0f, 0.35f, Wave.Sine, N(R), N(R + 12), 0.08f, 0.1f, 1.1f);
                d.Noise(0.12f, 0.5f, Filter.Band, 1229.0f, 3629.0f, 0.4f, 1f, 0.08f, 1.0f, 22f, 0.5f);
                d.Noise(0.15f, 0.4f, Filter.Band, 500f, 2400f, 0.4f, 1f, 0.15f, 1.1f, 14f, 0.5f);
                d.Thump(0.36f, 0.35f, 179.0f, 35f, 0.9f, 0.8f);
                d.Bell(0.45f, 0.5f, N(R + 19), 2.0f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_bitter_blade":
                d = new SoundDesign(1.0f, 0x165DDu);
                {
                    int R = 47;
                d.Noise(0.05f, 0.5f, Filter.Band, 500f, 2400f, 0.4f, 1f, 0.15f, 1.1f, 14f, 0.5f);
                d.Thump(0.33f, 0.35f, 181.0f, 31.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 1.8f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_double_shock":
                d = new SoundDesign(1.0f, 0x1BDD2u);
                {
                    int R = 50;
                d.Crackle(0.0f, 0.4f, 14, 6500f, 0.4f);
                for (int i = 0; i < 3; i++) d.Thump(0.2f + i * 0.08f, 0.2f, 226.0f - i * 12f, 45f, 0.6f + i * 0.12f, 0.7f);
                d.Bell(0.55f, 0.4f, N(R + 14), 1.8f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_gigaton_hammer":
                d = new SoundDesign(1.0f, 0x1FF1Cu);
                {
                    int R = 46;
                d.Bell(0.34f, 0.5f, N(R + 12), 2.76f, 1.0f, 0.09f, 3f);
                d.Thump(0.33f, 0.3f, 228.0f, 38.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_comeuppance":
                d = new SoundDesign(1.0f, 0x1EF67u);
                {
                    int R = 51;
                d.Tone(0f, 0.4f, Wave.Saw, N(R - 14), N(R - 22), 0.09f, 0.1f, 1.1f, vibratoHz: 12f, vibratoDepth: 0.05f);
                d.Thump(0.33f, 0.4f, 203.0f, 33.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 2.4f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_aqua_cutter":
                d = new SoundDesign(1.0f, 0x1B670u);
                {
                    int R = 60;
                d.Bubbles(0.05f, 0.5f, 7, 300f, 1100f, 0.3f);
                d.Thump(0.33f, 0.4f, 200.0f, 30.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_blazing_torque":
                d = new SoundDesign(1.0f, 0x1B3E2u);
                {
                    int R = 58;
                d.Noise(0.05f, 0.5f, Filter.Band, 500f, 2400f, 0.4f, 1f, 0.15f, 1.1f, 14f, 0.5f);
                d.Thump(0.33f, 0.3f, 186.0f, 36.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 29), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_wicked_torque":
                d = new SoundDesign(1.0f, 0x178FCu);
                {
                    int R = 58;
                d.Tone(0f, 0.4f, Wave.Saw, N(R - 14), N(R - 22), 0.09f, 0.1f, 1.1f, vibratoHz: 12f, vibratoDepth: 0.05f);
                d.Thump(0.33f, 0.3f, 216.0f, 46.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_noxious_torque":
                d = new SoundDesign(1.0f, 0x14FA0u);
                {
                    int R = 58;
                d.Bubbles(0.1f, 0.6f, 6, 160f, 520f, 0.3f);
                d.Thump(0.33f, 0.3f, 192.0f, 42.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_combat_torque":
                d = new SoundDesign(1.0f, 0x1B671u);
                {
                    int R = 63;
                d.Thump(0.2f, 0.15f, 190f, 50f, 0.6f, 0.6f);
                d.Thump(0.33f, 0.4f, 197.0f, 47.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 2.1f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_magical_torque":
                d = new SoundDesign(1.0f, 0x13C71u);
                {
                    int R = 51;
                d.Notes(0.4f, 0.07f, 0.14f, Wave.Triangle, Pitches(R + 24, R + 28, R + 31, R + 36), 0.05f, 0.01f);
                d.Thump(0.33f, 0.4f, 185.0f, 35.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 1.5f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_blood_moon":
                d = new SoundDesign(1.0f, 0x1D620u);
                {
                    int R = 54;
                d.Noise(0.1f, 0.2f, Filter.Band, 800f, 2800f, 0.3f, 1f, 0.05f, 1.3f);
                d.Thump(0.33f, 0.4f, 188.0f, 38.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_matcha_gotcha":
                d = new SoundDesign(1.0f, 0x19ECBu);
                {
                    int R = 47;
                d.Tone(0f, 0.35f, Wave.Sine, N(R), N(R + 12), 0.08f, 0.1f, 1.1f);
                d.Noise(0.12f, 0.5f, Filter.Band, 1079.0f, 3179.0f, 0.4f, 1f, 0.08f, 1.0f, 22f, 0.5f);
                d.Noise(0f, 0.4f, Filter.High, 3500f, 6500f, 0.3f, 1f, 0.1f, 1.2f, 18f, 0.5f);
                d.Thump(0.36f, 0.35f, 189.0f, 35f, 0.9f, 0.8f);
                d.Bell(0.45f, 0.5f, N(R + 19), 2.0f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_syrup_bomb":
                d = new SoundDesign(1.0f, 0x14554u);
                {
                    int R = 46;
                d.Noise(0f, 0.4f, Filter.High, 3500f, 6500f, 0.3f, 1f, 0.1f, 1.2f, 18f, 0.5f);
                d.Thump(0.33f, 0.3f, 192.0f, 42.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_ivy_cudgel":
                d = new SoundDesign(1.0f, 0x10ED7u);
                {
                    int R = 63;
                d.Noise(0f, 0.4f, Filter.High, 3500f, 6500f, 0.3f, 1f, 0.1f, 1.2f, 18f, 0.5f);
                d.Thump(0.33f, 0.4f, 227.0f, 37.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 2.1f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_electro_shot":
                d = new SoundDesign(1.0f, 0x174ADu);
                {
                    int R = 63;
                d.Tone(0f, 0.35f, Wave.Sine, N(R), N(R + 12), 0.08f, 0.1f, 1.1f);
                d.Noise(0.12f, 0.5f, Filter.Band, 825.0f, 4125.0f, 0.4f, 1f, 0.08f, 1.0f, 22f, 0.5f);
                d.Crackle(0.0f, 0.4f, 14, 6500f, 0.4f);
                d.Thump(0.36f, 0.35f, 175.0f, 35f, 0.9f, 0.8f);
                d.Bell(0.45f, 0.5f, N(R + 19), 2.0f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_tera_starstorm":
                d = new SoundDesign(1.0f, 0x14DBEu);
                {
                    int R = 50;
                d.Noise(0.1f, 0.2f, Filter.Band, 800f, 2800f, 0.3f, 1f, 0.05f, 1.3f);
                d.Thump(0.33f, 0.35f, 202.0f, 32.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 29), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_fickle_beam":
                d = new SoundDesign(1.0f, 0x1A28Cu);
                {
                    int R = 46;
                d.Tone(0f, 0.35f, Wave.Sine, N(R), N(R + 12), 0.08f, 0.1f, 1.1f);
                d.Noise(0.12f, 0.5f, Filter.Band, 760.0f, 3460.0f, 0.4f, 1f, 0.08f, 1.0f, 22f, 0.5f);
                d.Tone(0f, 0.35f, Wave.Saw, N(R - 10), N(R + 2), 0.09f, 0.1f, 1.0f, vibratoHz: 18f, vibratoDepth: 0.06f);
                d.Thump(0.36f, 0.35f, 190.0f, 35f, 0.9f, 0.8f);
                d.Bell(0.45f, 0.5f, N(R + 19), 2.0f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_burning_bulwark":
                d = new SoundDesign(1.0f, 0x15570u);
                {
                    int R = 56;
                d.Notes(0f, 0.1f, 0.22f, Wave.Sine, Pitches(R + 12, R + 16, R + 19, R + 24, R + 28), 0.07f, 0.03f);
                d.Noise(0.1f, 0.5f, Filter.Band, 500f, 2400f, 0.4f, 1f, 0.15f, 1.1f, 14f, 0.5f);
                d.Thump(0.36f, 0.2f, 166.0f, 60f, 0.4f, 0.3f);
                d.Bell(0.5f, 0.45f, N(R + 24), 2.01f, 0.8f, 0.06f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_thunderclap":
                d = new SoundDesign(1.0f, 0x1EDB7u);
                {
                    int R = 55;
                d.Crackle(0.0f, 0.4f, 14, 6500f, 0.4f);
                d.Thump(0.33f, 0.3f, 207.0f, 37.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 2.1f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_mighty_cleave":
                d = new SoundDesign(1.0f, 0x1C14Du);
                {
                    int R = 47;
                d.Crackle(0.36f, 0.5f, 10, 2400f, 0.4f);
                d.Thump(0.33f, 0.35f, 205.0f, 35.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 1.5f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_tachyon_cutter":
                d = new SoundDesign(1.0f, 0x12878u);
                {
                    int R = 48;
                d.Bell(0.34f, 0.5f, N(R + 12), 2.76f, 1.0f, 0.09f, 3f);
                for (int i = 0; i < 3; i++) d.Thump(0.2f + i * 0.08f, 0.2f, 232.0f - i * 12f, 45f, 0.6f + i * 0.12f, 0.7f);
                d.Bell(0.55f, 0.4f, N(R + 14), 1.8f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_hard_press":
                d = new SoundDesign(1.0f, 0x1B6EBu);
                {
                    int R = 47;
                d.Bell(0.34f, 0.5f, N(R + 12), 2.76f, 1.0f, 0.09f, 3f);
                d.Thump(0.33f, 0.35f, 235.0f, 45.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 1.5f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_dragon_cheer":
                d = new SoundDesign(1.0f, 0x1E933u);
                {
                    int R = 59;
                d.Notes(0f, 0.1f, 0.22f, Wave.Triangle, Pitches(R + 12, R + 16, R + 19, R + 24, R + 28), 0.07f, 0.03f);
                d.Tone(0f, 0.35f, Wave.Saw, N(R - 10), N(R + 2), 0.09f, 0.1f, 1.0f, vibratoHz: 18f, vibratoDepth: 0.06f);
                d.Thump(0.36f, 0.2f, 177.0f, 60f, 0.4f, 0.3f);
                d.Bell(0.5f, 0.45f, N(R + 24), 2.01f, 0.8f, 0.06f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_alluring_voice":
                d = new SoundDesign(1.0f, 0x1F613u);
                {
                    int R = 55;
                d.Tone(0f, 0.35f, Wave.Sine, N(R), N(R + 12), 0.08f, 0.1f, 1.1f);
                d.Noise(0.12f, 0.5f, Filter.Band, 919.0f, 3619.0f, 0.4f, 1f, 0.08f, 1.0f, 22f, 0.5f);
                d.Notes(0.4f, 0.07f, 0.14f, Wave.Triangle, Pitches(R + 24, R + 28, R + 31, R + 36), 0.05f, 0.01f);
                d.Thump(0.36f, 0.35f, 189.0f, 35f, 0.9f, 0.8f);
                d.Bell(0.45f, 0.5f, N(R + 19), 2.0f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_temper_flare":
                d = new SoundDesign(1.0f, 0x1C428u);
                {
                    int R = 46;
                d.Noise(0.05f, 0.5f, Filter.Band, 500f, 2400f, 0.4f, 1f, 0.15f, 1.1f, 14f, 0.5f);
                d.Thump(0.33f, 0.3f, 204.0f, 34.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_supercell_slam":
                d = new SoundDesign(1.0f, 0x1086Du);
                {
                    int R = 57;
                d.Crackle(0.0f, 0.4f, 14, 6500f, 0.4f);
                d.Thump(0.33f, 0.4f, 209.0f, 39.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 2.7f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_psychic_noise":
                d = new SoundDesign(1.0f, 0x1B67Eu);
                {
                    int R = 60;
                d.Tone(0f, 0.35f, Wave.Sine, N(R), N(R + 12), 0.08f, 0.1f, 1.1f);
                d.Noise(0.12f, 0.5f, Filter.Band, 1290.0f, 4290.0f, 0.4f, 1f, 0.08f, 1.0f, 22f, 0.5f);
                d.Tone(0f, 0.4f, Wave.Sine, N(R + 8), N(R + 20), 0.08f, 0.08f, 1.2f);
                d.Thump(0.36f, 0.35f, 200.0f, 35f, 0.9f, 0.8f);
                d.Bell(0.45f, 0.5f, N(R + 19), 2.0f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_upper_hand":
                d = new SoundDesign(1.0f, 0x11AC0u);
                {
                    int R = 54;
                d.Thump(0.2f, 0.15f, 190f, 50f, 0.6f, 0.6f);
                d.Thump(0.33f, 0.4f, 236.0f, 46.0f, 0.95f, 0.9f);
                d.Notes(0.45f, 0.06f, 0.1f, Wave.Pulse, Pitches(R + 24, R + 27), 0.05f, 0.005f);
                }
                return Finish(d.S, 0.38f);
            case "use_malignant_chain":
                d = new SoundDesign(1.0f, 0x18F5Fu);
                {
                    int R = 53;
                d.Bubbles(0.1f, 0.6f, 6, 160f, 520f, 0.3f);
                d.Thump(0.33f, 0.35f, 235.0f, 45.0f, 0.95f, 0.9f);
                d.Bell(0.4f, 0.4f, N(R + 12), 1.5f, 0.8f, 0.07f, 3f);
                }
                return Finish(d.S, 0.38f);
        }
        return null;
    }
}
