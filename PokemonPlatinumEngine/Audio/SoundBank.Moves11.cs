using System;
using static PokemonPlatinumEngine.Audio.SoundDesign;

namespace PokemonPlatinumEngine.Audio;

// Plan 25 - Y11, its first part (Y11a): the sounds of the later games' moves, Generation 5 from Hone Claws to Bulldoze.
// No game's sound is the model: each is our own, built from the bank's shared layers and timed to its look (the blow at
// 0.35 s). Every one runs to at most a second.
public static partial class SoundBank
{
    /// <summary>A sound of Y11's first part, or null for a name that isn't one.</summary>
    private static float[]? MakeMoveSoundY11(string name)
    {
        SoundDesign d;
        switch (name)
        {
            case "use_hone_claws":
                d = new SoundDesign(0.9f, 0x401Eu);
                for (int i = 0; i < 3; i++) d.Noise(0.05f + i * 0.17f, 0.16f, Filter.High, 3500f + i * 700f, 7500f, 0.5f, 1.4f, 0.1f, 1.8f, 30f, 0.5f);
                d.Tone(0, 0.7f, Wave.Sine, N(60), N(72), 0.1f, 0.3f, 1.3f);
                d.Bell(0.55f, 0.35f, N(96), 3.01f, 1.0f, 0.1f, 3f);
                return Finish(d.S, 0.36f);
            case "use_wide_guard":
                d = new SoundDesign(0.9f, 0x61D3u);
                for (int i = 0; i < 5; i++) d.Thump(0.02f + i * 0.05f, 0.14f, 190f - i * 12f, 80f, 0.5f, 0.5f);
                d.Tone(0.1f, 0.6f, Wave.Triangle, N(52), N(64), 0.1f, 0.2f, 1.3f);
                d.Bell(0.3f, 0.5f, N(76), 1.41f, 0.9f, 0.1f, 2.5f);
                d.Noise(0.25f, 0.4f, Filter.High, 4000f, 8000f, 0.1f, 1f, 0.2f, 1.6f);
                return Finish(d.S, 0.4f);
            case "use_guard_split":
                d = new SoundDesign(0.95f, 0x6591u);
                d.Tone(0, 0.38f, Wave.Sine, N(64), N(72), 0.13f, 0.3f, 1.2f, vibratoHz: 6f, vibratoDepth: 0.03f);
                d.Tone(0, 0.38f, Wave.Sine, N(76), N(72), 0.11f, 0.3f, 1.2f);
                d.Bell(0.36f, 0.5f, N(84), 2.01f, 1.0f, 0.13f, 2.6f);
                d.Notes(0.45f, 0.08f, 0.2f, Wave.Triangle, Pitches(79, 76, 72, 67), 0.07f, 0.01f);
                return Finish(d.S, 0.38f);
            case "use_power_split":
                d = new SoundDesign(0.95f, 0x9051u);
                d.Tone(0, 0.38f, Wave.Saw, N(48), N(60), 0.12f, 0.3f, 1.2f);
                d.Tone(0, 0.38f, Wave.Saw, N(66), N(60), 0.09f, 0.3f, 1.2f);
                d.Thump(0.36f, 0.4f, 170f, 50f, 0.9f, 0.7f);
                d.Notes(0.45f, 0.08f, 0.2f, Wave.Pulse, Pitches(72, 76, 79, 84), 0.07f, 0.01f);
                return Finish(d.S, 0.4f);
            case "use_wonder_room":
                d = new SoundDesign(1.0f, 0x30D5u);
                d.Tone(0, 0.95f, Wave.Sine, N(55), N(67), 0.14f, 0.3f, 0.9f, vibratoHz: 5f, vibratoDepth: 0.05f);
                d.Notes(0.1f, 0.12f, 0.3f, Wave.Triangle, Pitches(84, 79, 76, 72, 79, 84), 0.07f, 0.02f);
                d.Bell(0.35f, 0.6f, N(88), 1.5f, 1.0f, 0.1f, 2.4f);
                d.Noise(0.2f, 0.7f, Filter.Band, 1200f, 2800f, 0.12f, 1.6f, 0.4f, 1.1f, 7f, 0.4f);
                return Finish(d.S, 0.38f);
            case "use_psyshock":
                d = new SoundDesign(0.85f, 0x75E0u);
                d.Tone(0, 0.35f, Wave.Sine, N(72), N(91), 0.12f, 0.2f, 1.2f, vibratoHz: 9f, vibratoDepth: 0.05f);
                d.Notes(0.05f, 0.04f, 0.1f, Wave.Triangle, Pitches(84, 88, 91, 96, 100, 103, 108), 0.07f, 0.005f);
                d.Thump(0.35f, 0.3f, 220f, 60f, 0.9f, 0.7f);
                d.Crackle(0.36f, 0.4f, 12, 6500f, 0.3f);
                d.Bell(0.38f, 0.45f, N(91), 2.76f, 1.2f, 0.1f, 3f);
                return Finish(d.S, 0.42f);
            case "use_venoshock":
                d = new SoundDesign(0.9f, 0x7E40u);
                d.Noise(0, 0.35f, Filter.Band, 900f, 600f, 0.4f, 1.1f, 0.3f, 1.2f, 20f, 0.4f);
                d.Bubbles(0.2f, 0.6f, 9, 220f, 520f, 0.35f);
                d.Thump(0.34f, 0.3f, 160f, 45f, 0.8f, 0.5f);
                d.Tone(0.35f, 0.5f, Wave.Saw, N(46), N(40), 0.08f, 0.1f, 1.4f, vibratoHz: 8f, vibratoDepth: 0.05f);
                return Finish(d.S, 0.4f);
            case "use_autotomize":
                d = new SoundDesign(0.9f, 0xA070u);
                d.Crackle(0.02f, 0.4f, 14, 4200f, 0.4f);
                d.Noise(0.1f, 0.4f, Filter.High, 3000f, 7500f, 0.2f, 1f, 0.2f, 1.5f);
                d.Tone(0.2f, 0.6f, Wave.Sine, N(72), N(91), 0.1f, 0.3f, 1.3f);
                d.Bell(0.4f, 0.45f, N(96), 3.01f, 1.0f, 0.1f, 3f);
                return Finish(d.S, 0.36f);
            case "use_rage_powder":
                d = new SoundDesign(0.9f, 0x4A6Eu);
                d.Noise(0, 0.6f, Filter.Band, 2800f, 1500f, 0.35f, 0.9f, 0.1f, 1.2f, 14f, 0.5f);
                d.Tone(0.05f, 0.5f, Wave.Saw, N(43), N(50), 0.1f, 0.2f, 1.3f, vibratoHz: 11f, vibratoDepth: 0.06f);
                d.Thump(0.3f, 0.3f, 130f, 45f, 0.7f, 0.4f);
                d.Notes(0.45f, 0.07f, 0.15f, Wave.Pulse, Pitches(64, 64, 67), 0.06f, 0.01f);
                return Finish(d.S, 0.38f);
            case "use_telekinesis":
                d = new SoundDesign(1.0f, 0x7E1Eu);
                d.Tone(0, 0.9f, Wave.Sine, N(60), N(79), 0.14f, 0.3f, 0.9f, vibratoHz: 5f, vibratoDepth: 0.04f);
                d.Notes(0.15f, 0.1f, 0.35f, Wave.Triangle, Pitches(76, 79, 83, 88, 91), 0.07f, 0.03f);
                d.Noise(0.1f, 0.7f, Filter.Band, 600f, 2200f, 0.12f, 1.5f, 0.5f, 1.1f);
                d.Bell(0.7f, 0.3f, N(95), 2.01f, 0.8f, 0.08f, 3f);
                return Finish(d.S, 0.36f);
            case "use_magic_room":
                d = new SoundDesign(1.0f, 0x3A61u);
                d.Tone(0, 0.95f, Wave.Triangle, N(57), N(62), 0.12f, 0.3f, 0.9f, vibratoHz: 6f, vibratoDepth: 0.05f);
                for (int i = 0; i < 6; i++) d.Bell(0.12f + i * 0.1f, 0.3f, N(84 + (i * 5) % 12), 2.76f, 0.9f, 0.07f, 3f);
                d.Noise(0.6f, 0.35f, Filter.High, 5000f, 9000f, 0.15f, 1f, 0.1f, 1.6f);
                return Finish(d.S, 0.38f);
            case "use_smack_down":
                d = new SoundDesign(0.9f, 0x54D0u);
                d.Noise(0, 0.3f, Filter.Band, 1200f, 300f, 0.5f, 1f, 0.7f, 1.2f);
                d.Thump(0.34f, 0.5f, 150f, 28f, 1.0f, 0.9f);
                d.Crackle(0.36f, 0.4f, 14, 1800f, 0.4f);
                d.Noise(0.4f, 0.4f, Filter.Low, 2500f, 600f, 0.3f, 0.9f, 0.05f, 1.6f);
                return Finish(d.S, 0.44f);
            case "use_storm_throw":
                d = new SoundDesign(0.9f, 0x5701u);
                d.Noise(0, 0.3f, Filter.Band, 500f, 3000f, 0.5f, 1f, 0.6f, 1.2f, 18f, 0.4f);
                d.Thump(0.33f, 0.4f, 200f, 36f, 1.0f, 0.9f);
                d.Noise(0.34f, 0.08f, Filter.Band, 1800f, 900f, 0.9f, 1f, 0.01f, 2.2f);
                d.Notes(0.4f, 0.06f, 0.15f, Wave.Pulse, Pitches(72, 76, 79), 0.06f, 0.005f);
                return Finish(d.S, 0.46f);
            case "use_flame_burst":
                d = new SoundDesign(0.95f, 0xF1B0u);
                d.Noise(0, 0.33f, Filter.Low, 800f, 3200f, 0.4f, 1f, 0.5f, 1.2f);
                d.Thump(0.33f, 0.4f, 160f, 38f, 0.9f, 0.7f);
                d.Noise(0.35f, 0.5f, Filter.Band, 2800f, 1000f, 0.4f, 1f, 0.01f, 1.5f);
                d.Crackle(0.38f, 0.5f, 14, 3000f, 0.3f);
                return Finish(d.S, 0.42f);
            case "use_sludge_wave":
                d = new SoundDesign(1.0f, 0x51D6u);
                d.Noise(0, 0.9f, Filter.Low, 400f, 1500f, 0.5f, 0.9f, 0.5f, 0.9f, 4f, 0.4f);
                d.Bubbles(0.15f, 0.8f, 12, 150f, 420f, 0.4f);
                d.Thump(0.36f, 0.4f, 120f, 38f, 0.9f, 0.5f);
                d.Tone(0.4f, 0.55f, Wave.Saw, N(41), N(36), 0.08f, 0.1f, 1.3f, vibratoHz: 7f, vibratoDepth: 0.06f);
                return Finish(d.S, 0.4f);
            case "use_quiver_dance":
                d = new SoundDesign(1.0f, 0x9D3Cu);
                d.Notes(0, 0.1f, 0.3f, Wave.Triangle, Pitches(72, 76, 79, 84, 79, 76, 83, 88), 0.09f, 0.02f);
                d.Tone(0, 0.9f, Wave.Sine, N(48), N(55), 0.1f, 0.3f, 1.0f, vibratoHz: 7f, vibratoDepth: 0.04f);
                d.Noise(0.1f, 0.8f, Filter.High, 5000f, 9000f, 0.1f, 1f, 0.4f, 1.1f, 20f, 0.5f);
                d.Bell(0.7f, 0.3f, N(96), 3.01f, 0.9f, 0.08f, 3f);
                return Finish(d.S, 0.36f);
            case "use_heavy_slam":
                d = new SoundDesign(1.0f, 0x4E0Au);
                d.Tone(0, 0.32f, Wave.Saw, N(50), N(34), 0.12f, 0.1f, 1.4f);
                d.Thump(0.34f, 0.6f, 130f, 24f, 1.0f, 0.9f);
                d.Noise(0.34f, 0.07f, Filter.Band, 1500f, 700f, 1.0f, 1f, 0.01f, 2.4f);
                d.Bell(0.36f, 0.6f, N(52), 2.76f, 1.4f, 0.14f, 2.2f);
                d.Crackle(0.4f, 0.4f, 8, 1500f, 0.3f);
                return Finish(d.S, 0.46f);
            case "use_synchronoise":
                d = new SoundDesign(1.0f, 0x5DC0u);
                for (int i = 0; i < 3; i++)
                {
                    d.Bell(0.02f + i * 0.11f, 0.3f, N(72 + i * 2), 1.5f, 1.0f, 0.1f, 2.6f);
                    d.Bell(0.02f + i * 0.11f, 0.3f, N(76 + i * 2), 1.5f, 1.0f, 0.09f, 2.6f);
                }
                d.Tone(0.1f, 0.6f, Wave.Sine, N(60), N(72), 0.12f, 0.3f, 1.2f, vibratoHz: 9f, vibratoDepth: 0.05f);
                d.Thump(0.35f, 0.35f, 190f, 55f, 0.8f, 0.6f);
                d.Bell(0.4f, 0.5f, N(88), 2.01f, 1.0f, 0.1f, 2.6f);
                return Finish(d.S, 0.4f);
            case "use_electro_ball":
                d = new SoundDesign(0.9f, 0xE1BAu);
                d.Tone(0, 0.34f, Wave.Pulse, N(60), N(84), 0.1f, 0.3f, 1.2f, 0.3f, 14f, 0.06f);
                d.Crackle(0.02f, 0.3f, 14, 5500f, 0.35f);
                d.Thump(0.34f, 0.3f, 220f, 55f, 0.9f, 0.7f);
                d.Crackle(0.36f, 0.45f, 18, 6500f, 0.4f);
                d.Bell(0.38f, 0.4f, N(96), 3.01f, 1.0f, 0.1f, 3f);
                return Finish(d.S, 0.42f);
            case "use_soak":
                d = new SoundDesign(1.0f, 0x50A1u);
                d.Noise(0, 0.8f, Filter.High, 3500f, 1800f, 0.35f, 0.8f, 0.2f, 1.1f, 40f, 0.5f);
                d.Bubbles(0.15f, 0.7f, 10, 500f, 1300f, 0.4f);
                d.Thump(0.3f, 0.2f, 180f, 80f, 0.4f, 0.3f);
                d.Notes(0.5f, 0.09f, 0.25f, Wave.Sine, Pitches(79, 76, 72, 67), 0.07f, 0.01f);
                return Finish(d.S, 0.38f);
            case "use_flame_charge":
                d = new SoundDesign(0.85f, 0xFC5Au);
                d.Noise(0, 0.33f, Filter.Band, 600f, 3500f, 0.5f, 1f, 0.5f, 1.2f, 22f, 0.4f);
                d.Crackle(0.05f, 0.28f, 12, 3000f, 0.35f);
                d.Thump(0.33f, 0.25f, 190f, 60f, 0.9f, 0.7f);
                d.Noise(0.34f, 0.4f, Filter.Low, 2800f, 800f, 0.3f, 0.9f, 0.02f, 1.6f);
                return Finish(d.S, 0.42f);
            case "use_coil":
                d = new SoundDesign(0.95f, 0xC011u);
                d.Noise(0, 0.6f, Filter.Band, 1200f, 3200f, 0.3f, 1.6f, 0.4f, 1.1f, 9f, 0.6f);
                d.Tone(0.05f, 0.6f, Wave.Saw, N(45), N(57), 0.1f, 0.3f, 1.3f, vibratoHz: 10f, vibratoDepth: 0.06f);
                d.Thump(0.55f, 0.25f, 150f, 55f, 0.6f, 0.5f);
                d.Bell(0.58f, 0.35f, N(79), 2.01f, 0.9f, 0.1f, 3f);
                return Finish(d.S, 0.38f);
            case "use_low_sweep":
                d = new SoundDesign(0.75f, 0x10E5u);
                d.Noise(0.05f, 0.25f, Filter.Band, 700f, 3000f, 0.7f, 1f, 0.5f, 1.2f);
                d.Thump(0.3f, 0.2f, 210f, 80f, 0.8f, 0.7f);
                d.Noise(0.32f, 0.3f, Filter.Low, 2000f, 600f, 0.3f, 0.9f, 0.05f, 1.6f);
                return Finish(d.S, 0.4f);
            case "use_acid_spray":
                d = new SoundDesign(0.95f, 0xAC1Du);
                d.Noise(0, 0.35f, Filter.Band, 1600f, 3500f, 0.4f, 1f, 0.2f, 1.3f, 26f, 0.4f);
                d.Bubbles(0.2f, 0.6f, 10, 300f, 900f, 0.4f);
                d.Thump(0.34f, 0.2f, 190f, 70f, 0.5f, 0.4f);
                d.Noise(0.38f, 0.55f, Filter.High, 5000f, 7500f, 0.3f, 1f, 0.1f, 1.1f, 35f, 0.7f);
                return Finish(d.S, 0.38f);
            case "use_foul_play":
                d = new SoundDesign(0.9f, 0xF0C1u);
                d.Tone(0, 0.3f, Wave.Saw, N(45), N(55), 0.1f, 0.3f, 1.2f, vibratoHz: 9f, vibratoDepth: 0.07f);
                d.Noise(0.05f, 0.25f, Filter.Band, 500f, 1800f, 0.4f, 1.1f, 0.5f, 1.2f);
                d.Thump(0.34f, 0.35f, 170f, 40f, 1.0f, 0.8f);
                d.Tone(0.4f, 0.45f, Wave.Saw, N(52), N(40), 0.09f, 0.05f, 1.4f, vibratoHz: 8f, vibratoDepth: 0.06f);
                d.Bell(0.38f, 0.4f, N(55), 1.41f, 1.2f, 0.08f, 2.6f);
                return Finish(d.S, 0.42f);
            case "use_simple_beam":
                d = new SoundDesign(0.85f, 0x51B3u);
                d.Tone(0, 0.45f, Wave.Sine, N(72), N(76), 0.12f, 0.1f, 0.8f);
                d.Noise(0, 0.45f, Filter.High, 5000f, 6500f, 0.1f, 1f, 0.2f, 1f);
                d.Bell(0.38f, 0.35f, N(79), 1.0f, 0.5f, 0.12f, 2.6f);
                d.Bell(0.5f, 0.3f, N(79), 1.0f, 0.5f, 0.09f, 2.6f);
                return Finish(d.S, 0.34f);
            case "use_entrainment":
                d = new SoundDesign(1.0f, 0xE177u);
                d.Notes(0, 0.09f, 0.22f, Wave.Triangle, Pitches(72, 76, 79, 76, 72, 79), 0.1f, 0.01f);
                foreach (float at in new[] { 0.35f, 0.52f, 0.69f }) d.Thump(at, 0.12f, 170f, 70f, 0.45f, 0.4f);
                d.Bell(0.35f, 0.6f, N(84), 2.0f, 0.8f, 0.09f, 2.6f);
                return Finish(d.S, 0.38f);
            case "use_after_you":
                d = new SoundDesign(0.8f, 0xAF7Eu);
                d.Noise(0, 0.15f, Filter.Band, 1000f, 2400f, 0.3f, 1f, 0.5f, 1.4f);
                d.Notes(0.1f, 0.1f, 0.25f, Wave.Triangle, Pitches(76, 79, 84), 0.1f, 0.01f);
                d.Bell(0.36f, 0.4f, N(91), 2.01f, 0.9f, 0.1f, 2.8f);
                return Finish(d.S, 0.34f);
            case "use_round":
                d = new SoundDesign(1.0f, 0x2040u);
                d.Notes(0, 0.1f, 0.3f, Wave.Triangle, Pitches(72, 74, 76, 72), 0.1f, 0.02f);
                d.Notes(0.2f, 0.1f, 0.3f, Wave.Triangle, Pitches(76, 79, 81, 76), 0.08f, 0.02f);
                d.Notes(0.4f, 0.1f, 0.3f, Wave.Triangle, Pitches(79, 83, 84, 79), 0.07f, 0.02f);
                d.Bell(0.38f, 0.55f, N(84), 2.0f, 0.9f, 0.09f, 2.6f);
                return Finish(d.S, 0.38f);
            case "use_echoed_voice":
                d = new SoundDesign(1.0f, 0xEC40u);
                d.Tone(0, 0.3f, Wave.Triangle, N(67), N(70), 0.14f, 0.02f, 1.6f, vibratoHz: 6f, vibratoDepth: 0.03f);
                d.Echo(0.17f, 0.55f);
                d.Thump(0.34f, 0.2f, 190f, 80f, 0.5f, 0.5f);
                return Finish(d.S, 0.38f);
            case "use_chip_away":
                d = new SoundDesign(0.7f, 0xC819u);
                for (int i = 0; i < 3; i++)
                {
                    d.Noise(0.12f + i * 0.06f, 0.07f, Filter.High, 4500f, 7000f, 0.7f, 1.4f, 0.05f, 2f);
                    d.Thump(0.16f + i * 0.06f, 0.1f, 260f - i * 20f, 110f, 0.5f, 0.6f);
                }
                d.Crackle(0.3f, 0.3f, 10, 5000f, 0.35f);
                return Finish(d.S, 0.42f);
            case "use_clear_smog":
                d = new SoundDesign(0.95f, 0xC135u);
                d.Noise(0, 0.5f, Filter.Low, 2000f, 600f, 0.4f, 0.8f, 0.2f, 1.1f, 5f, 0.4f);
                d.Thump(0.33f, 0.3f, 150f, 50f, 0.7f, 0.5f);
                d.Noise(0.4f, 0.5f, Filter.High, 3000f, 8500f, 0.25f, 1f, 0.4f, 1.1f);
                d.Notes(0.5f, 0.08f, 0.25f, Wave.Sine, Pitches(84, 88, 91, 96), 0.07f, 0.02f);
                return Finish(d.S, 0.38f);
            case "use_stored_power":
                d = new SoundDesign(1.0f, 0x5702u);
                d.Tone(0, 0.34f, Wave.Sine, N(64), N(88), 0.11f, 0.5f, 1.0f, vibratoHz: 8f, vibratoDepth: 0.04f);
                d.Notes(0.04f, 0.045f, 0.12f, Wave.Triangle, Pitches(84, 88, 91, 96, 100, 103, 108, 112), 0.06f, 0.005f);
                d.Thump(0.35f, 0.4f, 200f, 45f, 1.0f, 0.8f);
                d.Notes(0.4f, 0.06f, 0.2f, Wave.Triangle, Pitches(100, 96, 91, 88, 84), 0.07f, 0.01f);
                d.Bell(0.38f, 0.5f, N(91), 2.76f, 1.0f, 0.1f, 2.8f);
                return Finish(d.S, 0.42f);
            case "use_quick_guard":
                d = new SoundDesign(0.7f, 0x6A4Du);
                d.Noise(0, 0.08f, Filter.High, 5000f, 7500f, 0.8f, 1.4f, 0.02f, 2.2f);
                d.Thump(0.02f, 0.2f, 230f, 100f, 0.6f, 0.6f);
                d.Bell(0.03f, 0.5f, N(79), 1.41f, 1.0f, 0.14f, 2.4f);
                for (int i = 0; i < 3; i++) d.Noise(0.2f + i * 0.08f, 0.06f, Filter.High, 6000f, 8000f, 0.35f, 1.2f, 0.01f, 2.4f);
                return Finish(d.S, 0.4f);
            case "use_ally_switch":
                d = new SoundDesign(0.85f, 0xA115u);
                d.Tone(0, 0.5f, Wave.Sine, N(67), N(79), 0.12f, 0.3f, 1.1f, vibratoHz: 7f, vibratoDepth: 0.04f);
                d.Tone(0, 0.5f, Wave.Sine, N(79), N(67), 0.1f, 0.3f, 1.1f);
                d.Noise(0.2f, 0.3f, Filter.Band, 900f, 2800f, 0.25f, 1.4f, 0.4f, 1.2f);
                d.Bell(0.5f, 0.35f, N(91), 2.01f, 0.9f, 0.1f, 3f);
                return Finish(d.S, 0.36f);
            case "use_scald":
                d = new SoundDesign(1.0f, 0x5CA1u);
                d.Noise(0, 0.34f, Filter.Band, 1800f, 3800f, 0.45f, 1f, 0.3f, 1.2f, 24f, 0.4f);
                d.Thump(0.34f, 0.25f, 170f, 60f, 0.7f, 0.5f);
                d.Noise(0.35f, 0.6f, Filter.High, 6000f, 4000f, 0.35f, 0.8f, 0.05f, 1.0f, 28f, 0.6f);
                d.Bubbles(0.4f, 0.5f, 7, 400f, 1100f, 0.3f);
                return Finish(d.S, 0.4f);
            case "use_shell_smash":
                d = new SoundDesign(0.9f, 0x5E11u);
                d.Tone(0, 0.2f, Wave.Sine, N(52), N(60), 0.1f, 0.5f, 1.2f);
                d.Thump(0.12f, 0.35f, 180f, 38f, 1.0f, 0.9f);
                d.Crackle(0.13f, 0.5f, 18, 3200f, 0.5f);
                d.Noise(0.15f, 0.3f, Filter.High, 3500f, 7500f, 0.4f, 1f, 0.02f, 1.8f);
                d.Notes(0.35f, 0.07f, 0.2f, Wave.Pulse, Pitches(72, 79, 84, 91), 0.06f, 0.01f);
                return Finish(d.S, 0.44f);
            case "use_heal_pulse":
                d = new SoundDesign(1.0f, 0x4EA1u);
                d.Bell(0.02f, 0.5f, N(79), 2.0f, 0.8f, 0.14f, 2.4f);
                d.Notes(0.1f, 0.1f, 0.3f, Wave.Triangle, Pitches(76, 79, 83, 88), 0.09f, 0.02f);
                d.Bell(0.35f, 0.6f, N(84), 2.0f, 0.9f, 0.12f, 2.4f);
                d.Bell(0.5f, 0.5f, N(88), 2.0f, 0.9f, 0.09f, 2.4f);
                d.Tone(0.1f, 0.8f, Wave.Sine, N(55), N(60), 0.09f, 0.4f, 1.0f);
                return Finish(d.S, 0.36f);
            case "use_hex":
                d = new SoundDesign(1.0f, 0x4E80u);
                d.Tone(0, 0.8f, Wave.Saw, N(46), N(43), 0.1f, 0.3f, 1.0f, vibratoHz: 6f, vibratoDepth: 0.08f);
                d.Notes(0.1f, 0.11f, 0.3f, Wave.Triangle, Pitches(70, 67, 63, 70, 67, 63), 0.07f, 0.02f);
                d.Thump(0.35f, 0.4f, 150f, 36f, 0.9f, 0.7f);
                d.Bell(0.38f, 0.55f, N(58), 2.76f, 1.2f, 0.11f, 2.4f);
                d.Noise(0.4f, 0.5f, Filter.Band, 1400f, 500f, 0.2f, 1.4f, 0.1f, 1.3f);
                return Finish(d.S, 0.4f);
            case "use_sky_drop":
                d = new SoundDesign(1.0f, 0x5D40u);
                d.Noise(0, 0.28f, Filter.Band, 400f, 2400f, 0.4f, 1f, 0.7f, 1.0f, 10f, 0.5f);
                d.Tone(0.04f, 0.25f, Wave.Triangle, N(60), N(79), 0.08f, 0.5f, 1.2f);
                d.Noise(0.28f, 0.05f, Filter.High, 5000f, 7000f, 0.6f, 1.4f, 0.01f, 2.2f);
                d.Thump(0.34f, 0.5f, 170f, 30f, 1.0f, 0.8f);
                d.Noise(0.4f, 0.4f, Filter.Low, 2200f, 500f, 0.3f, 0.9f, 0.05f, 1.6f);
                return Finish(d.S, 0.42f);
            case "use_shift_gear":
                d = new SoundDesign(0.9f, 0x5E6Au);
                for (int i = 0; i < 6; i++) d.Noise(0.02f + i * 0.07f, 0.05f, Filter.High, 4500f + i * 300f, 6500f, 0.6f, 1.4f, 0.01f, 2.2f);
                d.Tone(0.1f, 0.6f, Wave.Saw, N(45), N(66), 0.1f, 0.2f, 1.2f, vibratoHz: 14f, vibratoDepth: 0.05f);
                d.Bell(0.5f, 0.35f, N(88), 3.01f, 1.0f, 0.1f, 3f);
                d.Noise(0.5f, 0.3f, Filter.Band, 1000f, 5000f, 0.25f, 1f, 0.6f, 1.2f);
                return Finish(d.S, 0.4f);
            case "use_circle_throw":
                d = new SoundDesign(0.95f, 0xC17Cu);
                d.Noise(0, 0.33f, Filter.Band, 500f, 3000f, 0.55f, 1.1f, 0.6f, 1.0f, 16f, 0.5f);
                d.Tone(0, 0.33f, Wave.Sine, N(50), N(74), 0.07f, 0.5f, 1.2f, vibratoHz: 12f, vibratoDepth: 0.06f);
                d.Thump(0.34f, 0.4f, 190f, 40f, 1.0f, 0.8f);
                d.Noise(0.34f, 0.07f, Filter.Band, 1800f, 900f, 0.8f, 1f, 0.01f, 2.4f);
                d.Noise(0.45f, 0.4f, Filter.Low, 2500f, 700f, 0.25f, 0.9f, 0.05f, 1.6f);
                return Finish(d.S, 0.44f);
            case "use_incinerate":
                d = new SoundDesign(1.0f, 0x14C1u);
                d.Noise(0, 0.38f, Filter.Low, 900f, 3500f, 0.45f, 1f, 0.4f, 1.1f, 12f, 0.4f);
                d.Crackle(0.05f, 0.6f, 16, 3200f, 0.4f);
                d.Thump(0.34f, 0.3f, 160f, 50f, 0.7f, 0.5f);
                d.Noise(0.4f, 0.5f, Filter.High, 5000f, 7000f, 0.18f, 1f, 0.2f, 1.3f, 30f, 0.7f);
                return Finish(d.S, 0.4f);
            case "use_quash":
                d = new SoundDesign(0.95f, 0x70A5u);
                d.Tone(0, 0.34f, Wave.Saw, N(52), N(34), 0.12f, 0.1f, 1.3f, vibratoHz: 9f, vibratoDepth: 0.06f);
                d.Thump(0.34f, 0.55f, 120f, 24f, 1.0f, 0.8f);
                d.Noise(0.34f, 0.5f, Filter.Low, 1500f, 300f, 0.4f, 0.9f, 0.02f, 1.3f);
                d.Tone(0.45f, 0.5f, Wave.Sine, N(38), N(34), 0.1f, 0.1f, 1.3f);
                return Finish(d.S, 0.44f);
            case "use_acrobatics":
                d = new SoundDesign(0.9f, 0xAC70u);
                d.Noise(0, 0.33f, Filter.Band, 800f, 4000f, 0.5f, 1f, 0.4f, 1.0f, 8f, 0.5f);
                d.Notes(0.02f, 0.06f, 0.15f, Wave.Triangle, Pitches(76, 83, 88, 83, 79), 0.06f, 0.005f);
                d.Noise(0.3f, 0.22f, Filter.High, 3500f, 7000f, 0.8f, 1.2f, 0.05f, 1.8f, 38f, 0.6f);
                d.Thump(0.34f, 0.2f, 230f, 90f, 0.7f, 0.6f);
                return Finish(d.S, 0.4f);
            case "use_reflect_type":
                d = new SoundDesign(1.0f, 0x2EF1u);
                d.Bell(0.02f, 0.45f, N(86), 3.01f, 1.0f, 0.13f, 2.6f);
                d.Noise(0.1f, 0.3f, Filter.High, 4000f, 9000f, 0.2f, 1f, 0.5f, 1.2f);
                d.Notes(0.3f, 0.07f, 0.2f, Wave.Triangle, Pitches(84, 79, 76, 72, 69), 0.08f, 0.01f);
                d.Tone(0.35f, 0.55f, Wave.Sine, N(60), N(72), 0.1f, 0.4f, 1.1f);
                d.Bell(0.7f, 0.25f, N(98), 3.01f, 0.9f, 0.09f, 3f);
                return Finish(d.S, 0.36f);
            case "use_retaliate":
                d = new SoundDesign(1.0f, 0x2E7Au);
                d.Tone(0, 0.3f, Wave.Sine, N(64), N(70), 0.1f, 0.5f, 1.0f, vibratoHz: 6f, vibratoDepth: 0.06f);
                d.Notes(0.04f, 0.08f, 0.25f, Wave.Triangle, Pitches(81, 79, 76), 0.06f, 0.03f);
                d.Noise(0.12f, 0.22f, Filter.Band, 700f, 3500f, 0.5f, 1f, 0.6f, 1.2f);
                d.Thump(0.34f, 0.35f, 200f, 40f, 1.0f, 0.8f);
                d.Bell(0.38f, 0.5f, N(72), 2.0f, 1.0f, 0.1f, 2.6f);
                return Finish(d.S, 0.42f);
            case "use_final_gambit":
                d = new SoundDesign(1.0f, 0xF14Du);
                d.Tone(0, 0.35f, Wave.Saw, N(48), N(84), 0.12f, 0.6f, 1.0f, vibratoHz: 10f, vibratoDepth: 0.05f);
                d.Notes(0.05f, 0.04f, 0.1f, Wave.Triangle, Pitches(84, 88, 91, 96, 100, 103, 108), 0.07f, 0.005f);
                d.Noise(0.3f, 0.5f, Filter.High, 3000f, 9000f, 0.5f, 1f, 0.02f, 1.3f);
                d.Thump(0.35f, 0.6f, 190f, 28f, 1.0f, 0.9f);
                d.Bell(0.38f, 0.6f, N(91), 3.01f, 1.2f, 0.13f, 2.4f);
                d.Tone(0.5f, 0.5f, Wave.Sine, N(79), N(48), 0.1f, 0.02f, 1.3f);
                return Finish(d.S, 0.46f);
            case "use_bestow":
                d = new SoundDesign(0.95f, 0xBE57u);
                d.Noise(0, 0.15f, Filter.Band, 1000f, 2600f, 0.3f, 1f, 0.5f, 1.4f);
                d.Notes(0.1f, 0.09f, 0.3f, Wave.Triangle, Pitches(79, 83, 88, 91, 95), 0.09f, 0.01f);
                d.Thump(0.35f, 0.15f, 180f, 90f, 0.3f, 0.3f);
                d.Bell(0.36f, 0.55f, N(91), 2.0f, 0.8f, 0.12f, 2.4f);
                d.Notes(0.5f, 0.08f, 0.2f, Wave.Sine, Pitches(96, 100, 103), 0.05f, 0.01f);
                return Finish(d.S, 0.34f);
            case "use_inferno":
                d = new SoundDesign(1.0f, 0x14F3u);
                d.Noise(0, 0.9f, Filter.Low, 700f, 2800f, 0.5f, 0.9f, 0.3f, 0.9f, 9f, 0.5f);
                d.Tone(0.02f, 0.8f, Wave.Saw, N(38), N(45), 0.1f, 0.3f, 1.0f, vibratoHz: 12f, vibratoDepth: 0.06f);
                d.Thump(0.34f, 0.5f, 140f, 30f, 1.0f, 0.8f);
                d.Crackle(0.2f, 0.7f, 22, 3400f, 0.4f);
                return Finish(d.S, 0.46f);
            case "use_water_pledge":
                d = new SoundDesign(1.0f, 0xAA1Du);
                d.Noise(0.05f, 0.45f, Filter.Low, 500f, 2200f, 0.45f, 0.9f, 0.6f, 1.0f, 6f, 0.4f);
                d.Tone(0.1f, 0.5f, Wave.Sine, N(48), N(64), 0.1f, 0.5f, 1.1f);
                d.Thump(0.35f, 0.4f, 150f, 36f, 0.8f, 0.5f);
                d.Noise(0.38f, 0.55f, Filter.High, 5000f, 3000f, 0.3f, 0.9f, 0.02f, 1.4f, 30f, 0.5f);
                d.Bubbles(0.4f, 0.55f, 9, 400f, 1200f, 0.3f);
                return Finish(d.S, 0.42f);
            case "use_fire_pledge":
                d = new SoundDesign(1.0f, 0xF12Du);
                d.Noise(0.04f, 0.4f, Filter.Low, 600f, 2800f, 0.45f, 1f, 0.6f, 1.0f, 12f, 0.4f);
                d.Tone(0.1f, 0.45f, Wave.Saw, N(41), N(55), 0.09f, 0.5f, 1.1f, vibratoHz: 10f, vibratoDepth: 0.05f);
                d.Thump(0.35f, 0.4f, 150f, 32f, 0.9f, 0.7f);
                d.Noise(0.37f, 0.55f, Filter.Band, 2600f, 1000f, 0.35f, 1f, 0.01f, 1.4f);
                d.Crackle(0.4f, 0.5f, 16, 3000f, 0.35f);
                return Finish(d.S, 0.42f);
            case "use_grass_pledge":
                d = new SoundDesign(1.0f, 0x6A2Du);
                d.Noise(0.03f, 0.4f, Filter.Band, 500f, 1500f, 0.4f, 1.2f, 0.6f, 1.0f, 18f, 0.6f);
                d.Crackle(0.05f, 0.3f, 10, 1400f, 0.35f);
                d.Thump(0.34f, 0.35f, 140f, 40f, 0.7f, 0.5f);
                d.Noise(0.36f, 0.55f, Filter.High, 3000f, 6500f, 0.35f, 0.9f, 0.05f, 1.3f, 26f, 0.7f);
                d.Notes(0.5f, 0.07f, 0.2f, Wave.Triangle, Pitches(72, 76, 79, 84), 0.06f, 0.01f);
                return Finish(d.S, 0.4f);
            case "use_volt_switch":
                d = new SoundDesign(0.9f, 0x701Eu);
                d.Crackle(0, 0.36f, 14, 5800f, 0.4f);
                d.Tone(0, 0.3f, Wave.Pulse, N(72), N(100), 0.1f, 0.05f, 1.4f, 0.25f);
                d.Thump(0.34f, 0.25f, 230f, 60f, 0.9f, 0.7f);
                d.Tone(0.42f, 0.35f, Wave.Pulse, N(98), N(64), 0.1f, 0.02f, 1.3f, 0.3f);
                d.Noise(0.42f, 0.3f, Filter.Band, 4500f, 800f, 0.35f, 1f, 0.02f, 1.4f);
                return Finish(d.S, 0.42f);
            case "use_struggle_bug":
                d = new SoundDesign(0.95f, 0x5B06u);
                d.Noise(0, 0.34f, Filter.High, 3000f, 6500f, 0.4f, 1.3f, 0.3f, 1.1f, 46f, 0.8f);
                for (int i = 0; i < 8; i++) d.Thump(0.05f + i * 0.035f, 0.05f, 300f, 160f, 0.2f, 0.4f);
                d.Thump(0.34f, 0.25f, 190f, 60f, 0.6f, 0.5f);
                d.Noise(0.36f, 0.5f, Filter.High, 4000f, 7000f, 0.3f, 1.2f, 0.1f, 1.1f, 50f, 0.8f);
                d.Notes(0.5f, 0.06f, 0.15f, Wave.Pulse, Pitches(79, 76, 79), 0.05f, 0.01f);
                return Finish(d.S, 0.4f);
            case "use_bulldoze":
                d = new SoundDesign(1.0f, 0xB014u);
                d.Tone(0, 0.5f, Wave.Saw, N(33), N(36), 0.12f, 0.2f, 0.9f, vibratoHz: 16f, vibratoDepth: 0.08f);
                d.Noise(0, 0.5f, Filter.Low, 1200f, 500f, 0.5f, 0.9f, 0.3f, 0.9f, 20f, 0.5f);
                d.Crackle(0.05f, 0.35f, 18, 1500f, 0.5f);
                d.Thump(0.34f, 0.55f, 130f, 26f, 1.0f, 0.9f);
                d.Crackle(0.36f, 0.5f, 16, 1800f, 0.4f);
                return Finish(d.S, 0.46f);
        }
        return MakeMoveSoundY11b(name);
    }
}
