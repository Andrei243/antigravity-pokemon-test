using static PokemonPlatinumEngine.Audio.SoundDesign;

namespace PokemonPlatinumEngine.Audio;

// Plan 25 - Y10, its first part (Y10a): the sounds of the moves the post-game shows, from the Fuego Ironworks' sea to
// Stark Mountain. As the batches before: each move's beats are the original's (when it rings, how often, what it is
// made of, read from its animation script), its sound our own, built from the bank's shared layers. Hyper Voice has no
// sound here: it is the user's own cry.
public static partial class SoundBank
{
    /// <summary>A sound of Y10's first part, or null for a name that isn't one.</summary>
    private static float[]? MakeMoveSoundY10(string name)
    {
        SoundDesign d;
        switch (name)
        {
            case "use_clamp":
                d = new SoundDesign(0.8f, 0xC1A3u);
                d.Noise(0.02f, 0.06f, Filter.High, 3500f, 2200f, 0.6f, 1f, 0.05f, 2.2f);
                d.Thump(0.04f, 0.14f, 700f, 300f, 0.6f, 0.9f);
                d.Thump(0.2f, 0.2f, 240f, 80f, 0.9f, 0.7f);
                d.Noise(0.33f, 0.05f, Filter.High, 4000f, 2600f, 0.7f, 1f, 0.02f, 2.4f);
                d.Thump(0.34f, 0.2f, 620f, 260f, 0.7f, 0.8f);
                d.Bubbles(0.38f, 0.3f, 4, 250f, 700f, 0.25f);
                return Finish(d.S, 0.42f);
            case "use_crabhammer":
                d = new SoundDesign(1.0f, 0xCBA4u);
                d.Noise(0, 0.3f, Filter.Band, 400f, 1800f, 0.3f, 1.2f, 0.4f, 1.2f, 14f, 0.4f);
                d.Tone(0.05f, 0.3f, Wave.Saw, 120f, 300f, 0.09f, 0.3f, 1.3f);
                d.Thump(0.33f, 0.45f, 170f, 34f, 1.0f, 0.9f);
                d.Noise(0.33f, 0.07f, Filter.Band, 1600f, 800f, 0.9f, 1f, 0.01f, 2.4f);
                d.Noise(0.38f, 0.5f, Filter.Band, 500f, 2400f, 0.35f, 0.9f, 0.01f, 1.6f, 22f, 0.5f);
                d.Bubbles(0.42f, 0.4f, 7, 200f, 650f, 0.3f);
                return Finish(d.S, 0.46f);
            case "use_double_kick":
                d = new SoundDesign(0.7f, 0xD0C1u);
                foreach (float at in new[] { 0.14f, 0.31f })
                {
                    d.Noise(at - 0.1f, 0.12f, Filter.Band, 600f, 2800f, 0.3f, 1.2f, 0.4f, 1.2f);
                    d.Thump(at, 0.28f, 210f, 48f, 1.0f, 0.9f);
                    d.Noise(at, 0.06f, Filter.Band, 1600f, 800f, 0.8f, 1f, 0.01f, 2.4f);
                }
                return Finish(d.S, 0.44f);
            case "use_icicle_spear":
                d = new SoundDesign(0.9f, 0x1C1Eu);
                foreach (float at in new[] { 0.02f, 0.08f, 0.16f, 0.22f })
                {
                    d.Noise(at, 0.14f, Filter.High, 2500f, 7000f, 0.25f, 1f, 0.2f, 1.6f);
                    d.Tone(at, 0.12f, Wave.Sine, 1800f, 3200f, 0.05f, 0.1f, 2f);
                }
                foreach (float at in new[] { 0.2f, 0.26f, 0.32f, 0.38f })
                {
                    d.Noise(at, 0.06f, Filter.High, 5500f, 3500f, 0.55f, 1.2f, 0.02f, 2.4f);
                    d.Bell(at, 0.3f, N(100 + (int)(at * 20f) % 5), 3.01f, 0.8f, 0.1f, 3f);
                }
                d.Thump(0.38f, 0.25f, 200f, 70f, 0.6f, 0.5f);
                return Finish(d.S, 0.4f);
            case "use_rapid_spin":
                d = new SoundDesign(0.9f, 0x5A91u);
                d.Noise(0, 0.4f, Filter.Band, 500f, 3200f, 0.35f, 1.4f, 0.5f, 1.1f, 22f, 0.5f);
                d.Tone(0, 0.4f, Wave.Triangle, 300f, 1100f, 0.07f, 0.5f, 1.2f);
                foreach (float at in new[] { 0.36f, 0.48f, 0.6f })
                {
                    d.Thump(at, 0.2f, 190f, 55f, 0.7f, 0.6f);
                    d.Noise(at, 0.2f, Filter.Band, 1800f, 700f, 0.3f, 1f, 0.02f, 2f, 40f, 0.6f);
                }
                return Finish(d.S, 0.4f);
            case "use_water_spout":
                d = new SoundDesign(1.0f, 0x3A70u);
                d.Tone(0, 0.22f, Wave.Sine, 220f, 400f, 0.1f, 0.3f, 1.3f);
                d.Noise(0.1f, 0.35f, Filter.Band, 300f, 2200f, 0.4f, 1f, 0.3f, 1.2f, 18f, 0.5f);
                d.Thump(0.3f, 0.2f, 160f, 50f, 0.6f, 0.4f);
                d.Noise(0.36f, 0.55f, Filter.Band, 2500f, 600f, 0.45f, 0.8f, 0.01f, 1.5f, 26f, 0.6f);
                d.Bubbles(0.4f, 0.5f, 10, 180f, 650f, 0.3f);
                return Finish(d.S, 0.44f);
            case "use_belly_drum":
                d = new SoundDesign(1.0f, 0xBE11u);
                for (int i = 0; i < 6; i++)
                {
                    float at = 0.04f + i * 0.14f;
                    d.Thump(at, 0.18f, 180f - i * 6f, 70f, 0.9f, 0.5f);
                    d.Noise(at, 0.04f, Filter.Band, 400f, 250f, 0.4f, 1f, 0.05f, 2.4f);
                }
                d.Tone(0.5f, 0.45f, Wave.Triangle, N(48), N(60), 0.1f, 0.4f, 1.4f);
                return Finish(d.S, 0.46f);
            case "use_covet":
                d = new SoundDesign(0.9f, 0xC07Eu);
                d.Notes(0, 0.07f, 0.14f, Wave.Sine, Pitches(84, 88, 91, 88, 91, 96), 0.12f, 0.01f);
                d.Thump(0.35f, 0.15f, 380f, 140f, 0.5f, 0.6f);
                d.Notes(0.42f, 0.05f, 0.15f, Wave.Triangle, Pitches(96, 100, 103, 108), 0.08f, 0.005f);
                d.Bell(0.55f, 0.35f, N(103), 2.01f, 0.6f, 0.1f, 3f);
                return Finish(d.S, 0.34f);
            case "use_slack_off":
                d = new SoundDesign(1.0f, 0x51AFu);
                d.Tone(0, 0.5f, Wave.Triangle, N(60), N(50), 0.12f, 0.15f, 1.3f, vibratoHz: 4f, vibratoDepth: 0.04f);
                d.Noise(0.05f, 0.45f, Filter.Band, 500f, 250f, 0.15f, 1f, 0.3f, 1.3f);
                d.Notes(0.5f, 0.1f, 0.3f, Wave.Sine, Pitches(79, 84, 88, 91), 0.1f, 0.02f);
                d.Bell(0.82f, 0.18f, N(96), 2.01f, 0.4f, 0.08f, 3f);
                return Finish(d.S, 0.34f);
            case "use_superpower":
                d = new SoundDesign(1.0f, 0x5FE7u);
                d.Tone(0, 0.32f, Wave.Saw, 90f, 320f, 0.13f, 0.4f, 1.3f, vibratoHz: 16f, vibratoDepth: 0.05f);
                d.Noise(0, 0.32f, Filter.Band, 300f, 2400f, 0.3f, 1.4f, 0.5f, 1.1f, 20f, 0.5f);
                d.Thump(0.34f, 0.6f, 150f, 28f, 1.0f, 1.0f);
                d.Noise(0.34f, 0.08f, Filter.Band, 1500f, 700f, 1.0f, 1f, 0.01f, 2.4f);
                d.Noise(0.4f, 0.5f, Filter.Low, 1100f, 150f, 0.45f, 1f, 0.01f, 1.5f);
                d.Crackle(0.45f, 0.3f, 8, 1100f, 0.3f);
                return Finish(d.S, 0.48f);
            case "use_blaze_kick":
                d = new SoundDesign(0.9f, 0xB1A2u);
                d.Noise(0, 0.32f, Filter.Band, 600f, 3000f, 0.35f, 1f, 0.3f, 1.3f, 30f, 0.6f);
                d.Thump(0.33f, 0.35f, 200f, 38f, 1.0f, 0.9f);
                d.Noise(0.33f, 0.07f, Filter.Band, 1600f, 800f, 0.9f, 1f, 0.01f, 2.4f);
                d.Noise(0.36f, 0.45f, Filter.Band, 2200f, 700f, 0.35f, 0.9f, 0.01f, 1.4f, 45f, 0.7f);
                d.Crackle(0.4f, 0.35f, 9, 2200f, 0.25f);
                return Finish(d.S, 0.44f);
            case "use_eruption":
                d = new SoundDesign(1.0f, 0xE407u);
                foreach (float at in new[] { 0.0f, 0.16f })
                {
                    d.Tone(at, 0.2f, Wave.Saw, 70f, 150f, 0.1f, 0.2f, 1.3f);
                    d.Noise(at, 0.2f, Filter.Low, 900f, 300f, 0.4f, 1f, 0.1f, 1.3f);
                }
                foreach (float at in new[] { 0.36f, 0.45f, 0.52f, 0.61f, 0.68f, 0.78f })
                {
                    d.Thump(at, 0.2f, 160f, 36f, 0.8f, 0.8f);
                    d.Noise(at, 0.2f, Filter.Low, 1400f, 250f, 0.45f, 1f, 0.01f, 1.8f);
                }
                d.Crackle(0.4f, 0.5f, 14, 1500f, 0.25f);
                return Finish(d.S, 0.48f);
            case "use_facade":
                d = new SoundDesign(0.9f, 0xFACAu);
                for (int i = 0; i < 4; i++)
                {
                    d.Tone(i * 0.08f, 0.1f, Wave.Triangle, N(60 + i * 3), N(64 + i * 3), 0.1f, 0.1f, 1.8f);
                    d.Thump(i * 0.08f, 0.1f, 180f, 80f, 0.4f, 0.3f);
                }
                d.Noise(0.2f, 0.12f, Filter.Band, 500f, 2400f, 0.3f, 1.2f, 0.4f, 1.2f);
                d.Thump(0.33f, 0.35f, 190f, 40f, 1.0f, 0.9f);
                d.Noise(0.33f, 0.06f, Filter.Band, 1600f, 800f, 0.8f, 1f, 0.01f, 2.4f);
                return Finish(d.S, 0.44f);
            case "use_seed_bomb":
                d = new SoundDesign(1.0f, 0x5EEDu);
                for (int i = 0; i < 5; i++)
                {
                    float at = 0.02f + i * 0.035f;
                    d.Noise(at, 0.1f, Filter.Band, 1200f + i * 250f, 3200f, 0.2f, 1.4f, 0.1f, 2f);
                    d.Bell(at, 0.1f, N(79 + i * 2), 2.4f, 0.5f, 0.05f, 3f);
                }
                foreach (float at in new[] { 0.35f, 0.52f, 0.68f })
                {
                    d.Thump(at, 0.22f, 190f, 40f, 0.8f, 0.8f);
                    d.Noise(at, 0.22f, Filter.Low, 1500f, 300f, 0.4f, 1f, 0.01f, 1.8f);
                }
                d.Crackle(0.33f, 0.15f, 6, 2000f, 0.25f);
                return Finish(d.S, 0.46f);
            case "use_triple_kick":
                d = new SoundDesign(0.7f, 0x3C1Cu);
                foreach (float at in new[] { 0.1f, 0.2f, 0.31f })
                {
                    d.Noise(at - 0.07f, 0.09f, Filter.Band, 700f, 3000f, 0.3f, 1.2f, 0.4f, 1.2f);
                    d.Thump(at, 0.22f, 200f - at * 40f, 45f, 0.7f + at, 0.8f);
                    d.Noise(at, 0.05f, Filter.Band, 1500f, 800f, 0.7f, 1f, 0.01f, 2.4f);
                }
                return Finish(d.S, 0.44f);
            case "use_mega_punch":
                d = new SoundDesign(0.9f, 0x3E6Au);
                d.Noise(0.08f, 0.22f, Filter.Band, 500f, 2600f, 0.35f, 1.2f, 0.5f, 1.2f);
                d.Thump(0.33f, 0.55f, 170f, 28f, 1.0f, 1.0f);
                d.Noise(0.33f, 0.07f, Filter.Band, 1500f, 700f, 1.0f, 1f, 0.01f, 2.4f);
                d.Noise(0.38f, 0.45f, Filter.Low, 1000f, 150f, 0.4f, 1f, 0.01f, 1.5f);
                d.Bell(0.34f, 0.5f, N(55), 1.41f, 0.9f, 0.14f, 2.6f);
                return Finish(d.S, 0.46f);
            case "use_skull_bash":
                d = new SoundDesign(0.9f, 0x5C0Bu);
                d.Noise(0, 0.14f, Filter.Band, 400f, 2000f, 0.25f, 1.2f, 0.4f, 1.3f);
                d.Noise(0.14f, 0.18f, Filter.Band, 600f, 3000f, 0.3f, 1.4f, 0.4f, 1.2f, 25f, 0.4f);
                d.Thump(0.33f, 0.45f, 220f, 36f, 1.0f, 1.0f);
                d.Noise(0.33f, 0.06f, Filter.Band, 1900f, 900f, 1.0f, 1f, 0.01f, 2.4f);
                d.Bell(0.34f, 0.5f, N(72), 3.2f, 1.0f, 0.12f, 2.8f);
                return Finish(d.S, 0.46f);
            case "use_glare":
                d = new SoundDesign(1.0f, 0x61AEu);
                d.Tone(0.05f, 0.5f, Wave.Saw, N(42), N(46), 0.12f, 0.2f, 1.3f, vibratoHz: 7f, vibratoDepth: 0.06f);
                d.Notes(0.1f, 0.12f, 0.4f, Wave.Pulse, Pitches(70, 71, 70, 71), 0.06f, 0.02f, 2f, 0.3f);
                d.Noise(0.4f, 0.45f, Filter.High, 3000f, 6500f, 0.15f, 1f, 0.05f, 1.4f, 60f, 0.9f);
                d.Crackle(0.45f, 0.4f, 8, 3500f, 0.25f);
                return Finish(d.S, 0.34f);
            case "use_healing_wish":
                d = new SoundDesign(1.0f, 0x4EA1u);
                d.Tone(0, 0.8f, Wave.Sine, N(67), N(79), 0.1f, 0.3f, 1.3f, vibratoHz: 5f, vibratoDepth: 0.03f);
                d.Notes(0.05f, 0.1f, 0.4f, Wave.Triangle, Pitches(79, 83, 86, 91, 95, 98), 0.09f, 0.03f);
                d.Bell(0.65f, 0.35f, N(103), 2.01f, 0.5f, 0.1f, 3f);
                d.Notes(0.7f, 0.06f, 0.2f, Wave.Sine, Pitches(103, 107, 110), 0.05f, 0.01f);
                return Finish(d.S, 0.32f);
            case "use_lava_plume":
                d = new SoundDesign(1.0f, 0x1A7Au);
                d.Noise(0, 0.6f, Filter.Band, 400f, 2400f, 0.4f, 1f, 0.15f, 1.2f, 16f, 0.5f);
                d.Thump(0.04f, 0.3f, 140f, 40f, 0.9f, 0.5f);
                d.Noise(0.3f, 0.55f, Filter.Low, 1200f, 250f, 0.4f, 1f, 0.03f, 1.4f);
                d.Crackle(0.2f, 0.7f, 18, 1800f, 0.25f);
                d.Bubbles(0.4f, 0.4f, 6, 100f, 300f, 0.3f);
                return Finish(d.S, 0.44f);
            case "use_sky_uppercut":
                d = new SoundDesign(0.9f, 0x5C7Eu);
                d.Noise(0, 0.3f, Filter.Band, 600f, 3500f, 0.35f, 1f, 0.5f, 1.2f);
                d.Tone(0, 0.3f, Wave.Sine, 400f, 1200f, 0.06f, 0.5f, 1.3f);
                d.Thump(0.33f, 0.4f, 200f, 36f, 1.0f, 1.0f);
                d.Noise(0.33f, 0.06f, Filter.Band, 1700f, 800f, 1.0f, 1f, 0.01f, 2.4f);
                d.Thump(0.5f, 0.25f, 160f, 50f, 0.6f, 0.5f);
                d.Notes(0.5f, 0.06f, 0.2f, Wave.Triangle, Pitches(88, 91, 95), 0.05f, 0.01f);
                return Finish(d.S, 0.44f);
            case "use_cotton_spore":
                d = new SoundDesign(1.0f, 0xC077u);
                d.Noise(0, 0.85f, Filter.Band, 1800f, 4200f, 0.2f, 0.8f, 0.3f, 1.1f, 8f, 0.5f);
                d.Noise(0.05f, 0.8f, Filter.High, 3000f, 7000f, 0.08f, 1f, 0.4f, 1.1f);
                d.Notes(0.2f, 0.14f, 0.3f, Wave.Sine, Pitches(88, 84, 91, 86), 0.05f, 0.03f);
                return Finish(d.S, 0.32f);
            case "use_crush_claw":
                d = new SoundDesign(0.8f, 0xC2C1u);
                foreach (float at in new[] { 0.14f, 0.3f })
                {
                    d.Noise(at - 0.08f, 0.12f, Filter.High, 2500f, 6000f, 0.4f, 1f, 0.3f, 1.4f);
                    d.Thump(at, 0.2f, 240f, 70f, 0.8f, 0.7f);
                    d.Noise(at, 0.07f, Filter.Band, 1400f, 700f, 0.7f, 1f, 0.01f, 2.4f);
                }
                d.Crackle(0.32f, 0.2f, 6, 2000f, 0.25f);
                return Finish(d.S, 0.42f);
            case "use_fissure":
                d = new SoundDesign(1.0f, 0xF155u);
                d.Tone(0, 0.9f, Wave.Saw, N(30), N(27), 0.16f, 0.1f, 1.2f, vibratoHz: 11f, vibratoDepth: 0.08f);
                d.Noise(0, 0.9f, Filter.Low, 700f, 200f, 0.5f, 1f, 0.1f, 1.1f, 15f, 0.6f);
                d.Crackle(0.1f, 0.5f, 18, 1400f, 0.35f);
                d.Thump(0.3f, 0.5f, 120f, 26f, 1.0f, 0.5f);
                d.Noise(0.5f, 0.45f, Filter.Low, 1000f, 150f, 0.3f, 1f, 0.01f, 1.4f);
                return Finish(d.S, 0.48f);
            case "use_needle_arm":
                d = new SoundDesign(0.9f, 0x4EE1u);
                for (int i = 0; i < 4; i++)
                {
                    float at = 0.1f + i * 0.065f;
                    d.Noise(at, 0.05f, Filter.High, 3500f, 6500f, 0.4f, 1.2f, 0.05f, 2.2f);
                    d.Thump(at, 0.12f, 300f, 100f, 0.5f, 0.5f);
                }
                d.Thump(0.35f, 0.3f, 180f, 45f, 0.9f, 0.8f);
                d.Noise(0.35f, 0.06f, Filter.High, 4500f, 2500f, 0.8f, 1f, 0.01f, 2.4f);
                d.Crackle(0.37f, 0.2f, 6, 4000f, 0.25f);
                return Finish(d.S, 0.42f);
            case "use_spikes":
                d = new SoundDesign(0.9f, 0x5B1Eu);
                for (int i = 0; i < 5; i++)
                {
                    float at = 0.0f + i * 0.045f;
                    d.Noise(at, 0.1f, Filter.Band, 1500f + i * 200f, 3500f, 0.2f, 1.4f, 0.1f, 2f);
                    float land = 0.2f + i * 0.045f;
                    d.Bell(land, 0.3f, N(86 + i * 2), 3.01f, 0.9f, 0.1f, 3.4f);
                    d.Noise(land, 0.03f, Filter.High, 5000f, 4000f, 0.35f, 1.2f, 0.02f, 2.4f);
                }
                return Finish(d.S, 0.34f);
            case "use_teeter_dance":
                d = new SoundDesign(1.0f, 0x7EE7u);
                d.Notes(0, 0.12f, 0.2f, Wave.Triangle, Pitches(72, 76, 72, 67, 72, 76, 79, 76), 0.11f, 0.01f);
                d.Tone(0, 0.95f, Wave.Sine, N(48), N(48), 0.06f, 0.2f, 1.3f, vibratoHz: 6f, vibratoDepth: 0.2f);
                d.Crackle(0.1f, 0.8f, 6, 2500f, 0.15f, true);
                d.Notes(0.7f, 0.07f, 0.18f, Wave.Sine, Pitches(84, 88, 91), 0.07f, 0.01f);
                return Finish(d.S, 0.34f);
            case "use_guillotine":
                d = new SoundDesign(1.0f, 0x6071u);
                d.Noise(0, 0.3f, Filter.Band, 500f, 2800f, 0.3f, 1.2f, 0.5f, 1.2f);
                d.Thump(0.08f, 0.1f, 600f, 300f, 0.5f, 0.8f);
                d.Noise(0.33f, 0.05f, Filter.High, 4500f, 2500f, 0.9f, 1.4f, 0.01f, 2.4f);
                d.Bell(0.33f, 0.5f, N(88), 3.01f, 1.0f, 0.14f, 2.8f);
                d.Thump(0.34f, 0.35f, 190f, 40f, 0.9f, 0.8f);
                d.Noise(0.4f, 0.4f, Filter.High, 5000f, 8000f, 0.15f, 1f, 0.01f, 1.4f);
                return Finish(d.S, 0.44f);
            case "use_present":
                d = new SoundDesign(1.0f, 0x9E5Eu);
                d.Notes(0, 0.05f, 0.1f, Wave.Triangle, Pitches(84, 88, 91, 96, 91, 88), 0.09f, 0.005f);
                d.Noise(0.05f, 0.25f, Filter.Band, 800f, 2200f, 0.15f, 1.2f, 0.4f, 1.3f);
                d.Thump(0.35f, 0.35f, 220f, 45f, 0.9f, 0.8f);
                d.Noise(0.35f, 0.08f, Filter.Band, 1500f, 700f, 0.7f, 1f, 0.01f, 2.4f);
                d.Notes(0.4f, 0.06f, 0.2f, Wave.Sine, Pitches(91, 95, 98, 103), 0.08f, 0.01f);
                d.Crackle(0.38f, 0.3f, 8, 3500f, 0.2f);
                return Finish(d.S, 0.4f);
            case "use_heal_block":
                d = new SoundDesign(1.0f, 0x4EB1u);
                d.Tone(0, 0.8f, Wave.Saw, N(46), N(41), 0.12f, 0.15f, 1.3f, vibratoHz: 5f, vibratoDepth: 0.05f);
                d.Notes(0.15f, 0.14f, 0.3f, Wave.Triangle, Pitches(79, 78, 76, 72), 0.08f, 0.02f);
                d.Thump(0.55f, 0.25f, 150f, 50f, 0.8f, 0.5f);
                d.Bell(0.56f, 0.4f, N(58), 1.41f, 1.0f, 0.12f, 2.6f);
                d.Noise(0.2f, 0.7f, Filter.Band, 500f, 200f, 0.1f, 1.6f, 0.3f, 1.2f);
                return Finish(d.S, 0.38f);
            case "use_psycho_shift":
                d = new SoundDesign(1.0f, 0x5E5Fu);
                d.Tone(0, 0.45f, Wave.Sine, N(72), N(84), 0.12f, 0.2f, 1.3f, vibratoHz: 9f, vibratoDepth: 0.06f);
                d.Notes(0.05f, 0.07f, 0.2f, Wave.Triangle, Pitches(84, 88, 91, 96, 91, 88), 0.06f, 0.01f);
                d.Tone(0.4f, 0.5f, Wave.Sine, N(84), N(66), 0.12f, 0.1f, 1.4f, vibratoHz: 9f, vibratoDepth: 0.06f);
                d.Bell(0.45f, 0.4f, N(96), 2.01f, 0.8f, 0.1f, 3f);
                d.Noise(0.4f, 0.4f, Filter.Band, 1200f, 3000f, 0.1f, 1f, 0.05f, 1.4f, 30f, 0.7f);
                return Finish(d.S, 0.34f);
        }
        return MakeMoveSoundY10b(name);
    }
}
