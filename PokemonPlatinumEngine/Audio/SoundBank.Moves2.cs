using static PokemonPlatinumEngine.Audio.SoundDesign;

namespace PokemonPlatinumEngine.Audio;

// Plan 25 · Y7, its first part: the sounds of the moves the story shows from Route 204 to Team Galactic's Eterna
// Building. As Y6's: each move's beats are the original's (when it rings, how often, what it is made of, read from its
// animation script), its sound our own, built from the bank's shared layers.
public static partial class SoundBank
{
    /// <summary>A sound of Y7's batch, or null for a name that isn't one.</summary>
    private static float[]? MakeMoveSoundY7(string name)
    {
        SoundDesign d;
        switch (name)
        {
            case "use_string_shot":
                d = new SoundDesign(0.7f, 0x57A1u);
                d.Noise(0, 0.4f, Filter.High, 4200f, 6800f, 0.45f, 1.4f, 0.1f, 1.2f, 30f, 0.4f);
                d.Tone(0.38f, 0.3f, Wave.Triangle, 600f, 300f, 0.25f, 0.05f, 1.6f, vibratoHz: 24f, vibratoDepth: 0.05f);
                return Finish(d.S, 0.36f);
            case "use_rock_tomb":
                d = new SoundDesign(0.7f, 0x70B5u);
                foreach (float at in new[] { 0.12f, 0.2f, 0.27f, 0.34f })
                {
                    d.Thump(at, 0.18f, 150f, 55f, 0.6f, 0.8f);
                    d.Crackle(at, 0.08f, 4, 1300f, 0.35f);
                }
                d.Noise(0.12f, 0.5f, Filter.Low, 700f, 250f, 0.35f, 1f, 0.05f, 1.4f);
                return Finish(d.S, 0.46f);
            case "use_water_pulse":
                d = new SoundDesign(0.65f, 0x3A9Cu);
                d.Tone(0, 0.36f, Wave.Sine, 300f, 360f, 0.35f, 0.1f, 1f, vibratoHz: 12f, vibratoDepth: 0.12f);
                d.Bubbles(0.05f, 0.3f, 6, 500f, 1000f, 0.25f);
                d.Noise(0.34f, 0.3f, Filter.Low, 2600f, 900f, 0.6f, 0.9f, 0.03f, 1.6f);
                return Finish(d.S, 0.4f);
            case "use_bullet_seed":
                d = new SoundDesign(0.5f, 0xB5EDu);
                for (int k = 0; k < 6; k++)
                {
                    d.Noise(k * 0.06f, 0.04f, Filter.Band, 1800f, 1200f, 0.6f, 1.4f, 0.05f, 2f);
                    d.Thump(k * 0.06f + 0.05f, 0.06f, 420f, 240f, 0.3f, 0.6f);
                }
                return Finish(d.S, 0.42f);
            case "use_charge":
                d = new SoundDesign(0.9f, 0xC4A6u);
                d.Tone(0, 0.85f, Wave.Saw, 110f, 190f, 0.25f, 0.6f, 0.8f, vibratoHz: 30f, vibratoDepth: 0.05f);
                d.Noise(0.1f, 0.75f, Filter.High, 3000f, 5000f, 0.25f, 1f, 0.6f, 1f, 45f, 0.7f);
                d.Crackle(0.2f, 0.6f, 12, 3600f, 0.35f);
                return Finish(d.S, 0.4f);
            case "use_charm":
                d = new SoundDesign(0.6f);
                d.Notes(0, 0.14f, 0.22f, Wave.Triangle, Pitches(84, 88), 0.3f);
                d.Bell(0.3f, 0.3f, N(91), 2.01f, 1.0f, 0.25f, 2.5f);
                return Finish(d.S, 0.36f);
            case "use_leech_seed":
                d = new SoundDesign(0.65f, 0x1EEDu);
                foreach (float at in new[] { 0f, 0.05f, 0.1f }) d.Tone(at, 0.06f, Wave.Sine, 500f, 900f, 0.3f, 0.1f, 2f);
                d.Thump(0.32f, 0.08f, 300f, 200f, 0.3f, 0.5f);
                d.Tone(0.36f, 0.25f, Wave.Triangle, 300f, 520f, 0.25f, 0.2f, 1.4f, vibratoHz: 14f, vibratoDepth: 0.03f);
                return Finish(d.S, 0.38f);
            case "use_poison_sting":
                d = new SoundDesign(0.5f, 0x9057u);
                d.Noise(0.15f, 0.05f, Filter.High, 5200f, 4000f, 0.8f, 1.4f, 0.05f, 2.5f);
                d.Thump(0.2f, 0.08f, 600f, 300f, 0.35f, 0.6f);
                d.Noise(0.22f, 0.25f, Filter.Band, 3000f, 1800f, 0.35f, 1.2f, 0.1f, 1.5f, 18f, 0.5f);
                return Finish(d.S, 0.4f);
            case "use_stun_spore":
                d = new SoundDesign(0.85f, 0x5709u);
                d.Noise(0, 0.25f, Filter.Band, 1400f, 2600f, 0.45f, 1f, 0.2f, 1.4f);
                d.Noise(0.25f, 0.55f, Filter.High, 5000f, 3800f, 0.25f, 1f, 0.1f, 1.2f, 22f, 0.6f);
                d.Crackle(0.5f, 0.3f, 8, 3400f, 0.35f);
                return Finish(d.S, 0.38f);
            case "use_wing_attack":
                d = new SoundDesign(0.5f, 0x3A17u);
                foreach (float at in new[] { 0f, 0.13f }) d.Noise(at, 0.13f, Filter.Band, 500f, 1500f, 0.7f, 1f, 0.4f, 1.4f);
                d.Thump(0.32f, 0.12f, 260f, 120f, 0.6f, 0.9f);
                return Finish(d.S, 0.42f);
            case "use_pluck":
                d = new SoundDesign(0.45f, 0x91C4u);
                foreach (float at in new[] { 0.1f, 0.18f, 0.28f })
                {
                    d.Noise(at, 0.03f, Filter.Band, 2600f, 2000f, 0.7f, 2f, 0.05f, 2.5f);
                    d.Thump(at, 0.07f, 520f, 300f, 0.35f, 0.5f);
                }
                return Finish(d.S, 0.4f);
            case "use_baton_pass":
                d = new SoundDesign(0.75f, 0xBA70u);
                d.Noise(0, 0.4f, Filter.Band, 800f, 1600f, 0.4f, 1.4f, 0.3f, 1f, 9f, 0.7f);
                d.Bell(0.35f, 0.4f, N(88), 2.76f, 1.4f, 0.3f, 2.4f);
                d.Tone(0.45f, 0.28f, Wave.Sine, N(84), N(96), 0.15f, 0.1f, 1.5f);
                return Finish(d.S, 0.36f);
            case "use_foresight":
                d = new SoundDesign(0.6f);
                d.Tone(0, 0.3f, Wave.Triangle, 900f, 1400f, 0.25f, 0.1f, 1.3f, vibratoHz: 20f, vibratoDepth: 0.02f);
                d.Bell(0.3f, 0.28f, N(98), 3.01f, 1.0f, 0.3f, 2.8f);
                return Finish(d.S, 0.34f);
            case "use_karate_chop":
                d = new SoundDesign(0.45f, 0x4A7Cu);
                d.Noise(0, 0.25f, Filter.Band, 700f, 3000f, 0.7f, 1.1f, 0.4f, 1.4f);
                d.Thump(0.3f, 0.12f, 280f, 120f, 0.7f, 0.9f);
                return Finish(d.S, 0.44f);
            case "use_mega_drain":
                d = new SoundDesign(0.85f, 0x3E6Au);
                d.Noise(0.3f, 0.45f, Filter.Band, 500f, 2800f, 0.35f, 1.4f, 0.5f, 1.2f);
                d.Tone(0.32f, 0.42f, Wave.Sine, N(62), N(81), 0.32f, 0.3f, 1.3f, vibratoHz: 7f, vibratoDepth: 0.01f);
                d.Bell(0.72f, 0.12f, N(93), 2.01f, 0.9f, 0.25f, 2.5f);
                return Finish(d.S, 0.36f);
            case "use_mud_bomb":
                d = new SoundDesign(0.6f, 0x3B0Bu);
                d.Noise(0, 0.3f, Filter.Band, 600f, 300f, 0.35f, 1f, 0.3f, 1.4f);
                d.Thump(0.34f, 0.2f, 180f, 70f, 0.75f, 0.4f);
                d.Noise(0.34f, 0.25f, Filter.Low, 1600f, 500f, 0.6f, 1f, 0.03f, 1.6f);
                return Finish(d.S, 0.44f);
            case "use_mud_slap":
                d = new SoundDesign(0.5f, 0x3E5Au);
                d.Noise(0, 0.2f, Filter.Low, 600f, 1400f, 0.6f, 1f, 0.2f, 1.4f);
                d.Thump(0.3f, 0.1f, 200f, 120f, 0.5f, 0.3f);
                d.Noise(0.3f, 0.18f, Filter.Low, 1800f, 600f, 0.5f, 1f, 0.03f, 1.8f);
                return Finish(d.S, 0.42f);
            case "use_peck":
                d = new SoundDesign(0.42f, 0x9EC4u);
                foreach (float at in new[] { 0.22f, 0.32f })
                {
                    d.Noise(at, 0.03f, Filter.Band, 3000f, 2200f, 0.7f, 2f, 0.05f, 2.5f);
                    d.Thump(at, 0.07f, 560f, 320f, 0.4f, 0.5f);
                }
                return Finish(d.S, 0.4f);
            case "use_rage":
                d = new SoundDesign(0.6f, 0x9A6Eu);
                d.Tone(0, 0.3f, Wave.Saw, 80f, 130f, 0.35f, 0.3f, 1f, vibratoHz: 26f, vibratoDepth: 0.08f);
                d.Thump(0.34f, 0.2f, 190f, 70f, 0.8f, 0.7f);
                return Finish(d.S, 0.44f);
            case "use_sand_attack":
                d = new SoundDesign(0.6f, 0x5A4Du);
                foreach (float at in new[] { 0f, 0.15f }) d.Noise(at, 0.25f, Filter.High, 2400f, 4200f, 0.5f, 1f, 0.15f, 1.5f, 40f, 0.6f);
                d.Crackle(0.05f, 0.4f, 14, 4200f, 0.25f);
                return Finish(d.S, 0.38f);
            case "use_sonic_boom":
                d = new SoundDesign(0.55f, 0x50B0u);
                foreach (float at in new[] { 0.18f, 0.26f, 0.34f })
                {
                    d.Thump(at, 0.14f, 120f, 50f, 0.5f, 0.3f);
                    d.Noise(at, 0.1f, Filter.Low, 900f, 300f, 0.4f, 1f, 0.02f, 2f);
                }
                d.Noise(0, 0.2f, Filter.Band, 600f, 2400f, 0.3f, 1f, 0.6f, 1.2f);
                return Finish(d.S, 0.44f);
            case "use_hypnosis":
                d = new SoundDesign(0.9f);
                d.Tone(0, 0.85f, Wave.Sine, 440f, 440f, 0.3f, 0.2f, 1f, vibratoHz: 3.5f, vibratoDepth: 0.12f);
                d.Tone(0.05f, 0.8f, Wave.Triangle, 660f, 660f, 0.15f, 0.2f, 1f, vibratoHz: 3f, vibratoDepth: 0.12f);
                return Finish(d.S, 0.34f);
            case "use_thunderbolt":
                d = new SoundDesign(0.8f, 0x7B01u);
                foreach (float at in new[] { 0.08f, 0.18f, 0.28f })
                {
                    d.Noise(at, 0.12f, Filter.High, 6000f, 2500f, 0.6f, 1f, 0.01f, 2.5f);
                    d.Crackle(at, 0.1f, 6, 3000f, 0.4f);
                }
                d.Noise(0.3f, 0.45f, Filter.Low, 500f, 120f, 0.6f, 1f, 0.02f, 1.6f);
                return Finish(d.S, 0.46f);
            case "use_bite":
                d = new SoundDesign(0.4f, 0xB17Eu);
                d.Noise(0.2f, 0.03f, Filter.Band, 2400f, 1600f, 0.8f, 1.6f, 0.05f, 2.5f);
                d.Thump(0.21f, 0.12f, 300f, 140f, 0.6f, 0.7f);
                return Finish(d.S, 0.42f);
            case "use_feint_attack":
                d = new SoundDesign(0.6f, 0xFE17u);
                d.Noise(0, 0.25f, Filter.Band, 2400f, 500f, 0.45f, 1.2f, 0.1f, 1.4f);
                d.Thump(0.34f, 0.16f, 220f, 90f, 0.7f, 0.6f);
                return Finish(d.S, 0.42f);
            case "use_toxic":
                d = new SoundDesign(0.85f, 0x70C1u);
                d.Bubbles(0.3f, 0.5f, 10, 150f, 340f, 0.4f);
                d.Noise(0.3f, 0.5f, Filter.Low, 500f, 300f, 0.3f, 1.2f, 0.1f, 1.2f, 6f, 0.4f);
                d.Noise(0, 0.3f, Filter.Band, 900f, 600f, 0.25f, 1f, 0.3f, 1.2f);
                return Finish(d.S, 0.4f);
            case "use_agility":
                d = new SoundDesign(0.75f, 0xA617u);
                foreach (float at in new[] { 0.04f, 0.42f })
                {
                    d.Noise(at, 0.12f, Filter.Band, 1500f, 4500f, 0.55f, 1.2f, 0.3f, 1.4f);
                    d.Noise(at + 0.12f, 0.12f, Filter.Band, 4500f, 1500f, 0.45f, 1.2f, 0.2f, 1.4f);
                }
                return Finish(d.S, 0.38f);
            case "use_aqua_jet":
                d = new SoundDesign(0.65f, 0xA0A7u);
                d.Noise(0.05f, 0.32f, Filter.Band, 1200f, 3600f, 0.7f, 1f, 0.2f, 1.1f);
                d.Noise(0.34f, 0.28f, Filter.Low, 3000f, 900f, 0.6f, 0.9f, 0.02f, 1.6f);
                d.Bubbles(0.36f, 0.2f, 4, 600f, 1100f, 0.25f);
                return Finish(d.S, 0.42f);
            case "use_body_slam":
                d = new SoundDesign(0.6f, 0xB0D5u);
                d.Tone(0, 0.2f, Wave.Sine, 220f, 330f, 0.25f, 0.2f, 1.4f);
                d.Thump(0.34f, 0.25f, 120f, 40f, 1f, 0.8f);
                d.Noise(0.34f, 0.25f, Filter.Low, 900f, 200f, 0.4f, 1f, 0.02f, 1.6f);
                return Finish(d.S, 0.46f);
            case "use_confuse_ray":
                d = new SoundDesign(0.9f);
                d.Tone(0, 0.85f, Wave.Sine, 700f, 900f, 0.3f, 0.15f, 1f, vibratoHz: 9f, vibratoDepth: 0.08f);
                d.Tone(0, 0.85f, Wave.Sine, 1050f, 1300f, 0.12f, 0.15f, 1f, vibratoHz: 7f, vibratoDepth: 0.08f);
                return Finish(d.S, 0.34f);
            case "use_crunch":
                d = new SoundDesign(0.5f, 0xC2C4u);
                d.Noise(0.2f, 0.03f, Filter.Band, 2200f, 1400f, 0.8f, 1.6f, 0.05f, 2.5f);
                d.Thump(0.21f, 0.14f, 260f, 110f, 0.7f, 0.8f);
                d.Crackle(0.24f, 0.18f, 12, 1600f, 0.5f);
                return Finish(d.S, 0.44f);
            case "use_fire_punch":
                d = new SoundDesign(0.6f, 0xF1E0u);
                d.Thump(0.34f, 0.16f, 220f, 90f, 0.7f, 0.8f);
                d.Noise(0.34f, 0.25f, Filter.Low, 1200f, 2800f, 0.6f, 1f, 0.05f, 1.4f, 14f, 0.3f);
                d.Crackle(0.36f, 0.2f, 8, 2600f, 0.35f);
                return Finish(d.S, 0.44f);
            case "use_fire_spin":
                d = new SoundDesign(0.95f, 0xF5B1u);
                d.Noise(0, 0.9f, Filter.Band, 600f, 1400f, 0.6f, 1.2f, 0.3f, 1.2f, 7f, 0.6f);
                d.Crackle(0.3f, 0.6f, 16, 2600f, 0.3f);
                return Finish(d.S, 0.42f);
            case "use_hidden_power":
                d = new SoundDesign(0.7f, 0x41D0u);
                d.Notes(0, 0.04f, 0.2f, Wave.Sine, Pitches(79, 83, 86, 91, 95, 98), 0.15f);
                d.Noise(0.2f, 0.15f, Filter.Band, 800f, 3000f, 0.4f, 1f, 0.7f, 1.2f);
                d.Thump(0.35f, 0.15f, 240f, 110f, 0.5f, 0.5f);
                return Finish(d.S, 0.4f);
            case "use_lock_on":
                d = new SoundDesign(0.65f);
                foreach (float at in new[] { 0f, 0.12f, 0.22f, 0.3f }) d.Tone(at, 0.05f, Wave.Pulse, 1760f, 1760f, 0.2f, 0.05f, 1.2f, duty: 0.3f);
                d.Tone(0.35f, 0.25f, Wave.Pulse, 1760f, 1760f, 0.2f, 0.02f, 0.8f, duty: 0.3f);
                return Finish(d.S, 0.32f);
            case "use_magnet_bomb":
                d = new SoundDesign(0.7f, 0x3A6Bu);
                d.Tone(0, 0.3f, Wave.Sine, 300f, 600f, 0.25f, 0.2f, 1f, vibratoHz: 40f, vibratoDepth: 0.03f);
                foreach (float at in new[] { 0.28f, 0.31f, 0.34f, 0.37f })
                {
                    d.Noise(at, 0.06f, Filter.High, 5000f, 3000f, 0.45f, 1.2f, 0.02f, 2.5f);
                    d.Bell(at, 0.12f, 1900f, 1.41f, 2.2f, 0.15f, 3f);
                }
                return Finish(d.S, 0.42f);
            case "use_rain_dance":
                d = new SoundDesign(1.0f, 0x7A1Du);
                d.Noise(0, 0.6f, Filter.Low, 180f, 120f, 0.5f, 1f, 0.3f, 1.4f, 3f, 0.3f);
                d.Noise(0.2f, 0.8f, Filter.High, 3000f, 3500f, 0.3f, 1f, 0.3f, 1f);
                d.Crackle(0.25f, 0.7f, 30, 4800f, 0.2f);
                return Finish(d.S, 0.4f);
            case "use_spark":
                d = new SoundDesign(0.55f, 0x59A4u);
                d.Crackle(0, 0.3f, 14, 3400f, 0.4f);
                d.Noise(0, 0.3f, Filter.High, 3000f, 5000f, 0.25f, 1f, 0.2f, 1f, 50f, 0.7f);
                d.Noise(0.34f, 0.12f, Filter.High, 6000f, 2500f, 0.6f, 1f, 0.01f, 2.5f);
                d.Thump(0.34f, 0.1f, 260f, 140f, 0.4f, 0.4f);
                return Finish(d.S, 0.44f);
            case "use_swift":
                d = new SoundDesign(0.6f);
                d.Notes(0, 0.04f, 0.18f, Wave.Triangle, Pitches(88, 91, 95, 98, 100, 103, 107), 0.16f);
                d.Bell(0.34f, 0.25f, N(100), 3.01f, 1f, 0.2f, 2.5f);
                return Finish(d.S, 0.34f);
            case "use_thunder_wave":
                d = new SoundDesign(0.85f, 0x7B0Au);
                d.Tone(0, 0.8f, Wave.Saw, 140f, 120f, 0.25f, 0.1f, 0.9f, vibratoHz: 32f, vibratoDepth: 0.04f);
                d.Crackle(0.35f, 0.45f, 12, 3600f, 0.35f);
                d.Noise(0.35f, 0.4f, Filter.High, 4000f, 3000f, 0.2f, 1f, 0.05f, 1.3f, 30f, 0.7f);
                return Finish(d.S, 0.4f);
            case "use_confusion":
                d = new SoundDesign(0.75f);
                d.Tone(0, 0.7f, Wave.Sine, 520f, 360f, 0.3f, 0.1f, 1.2f, vibratoHz: 11f, vibratoDepth: 0.06f);
                d.Tone(0.05f, 0.6f, Wave.Sine, 780f, 540f, 0.15f, 0.1f, 1.2f, vibratoHz: 13f, vibratoDepth: 0.06f);
                return Finish(d.S, 0.34f);
            case "use_gust":
                d = new SoundDesign(0.85f, 0x6057u);
                d.Noise(0, 0.8f, Filter.Band, 400f, 1800f, 0.7f, 1.4f, 0.3f, 1.2f, 4f, 0.4f);
                return Finish(d.S, 0.4f);
            case "use_horn_attack":
                d = new SoundDesign(0.45f, 0x4A0Eu);
                foreach (float at in new[] { 0.2f, 0.34f })
                {
                    d.Noise(at - 0.06f, 0.06f, Filter.Band, 900f, 2200f, 0.4f, 1f, 0.5f, 1.4f);
                    d.Thump(at, 0.1f, 400f, 180f, 0.55f, 0.8f);
                }
                return Finish(d.S, 0.42f);
            case "use_rollout":
                d = new SoundDesign(0.6f, 0x9011u);
                d.Noise(0, 0.36f, Filter.Low, 300f, 600f, 0.6f, 1f, 0.3f, 1f, 12f, 0.5f);
                d.Thump(0.35f, 0.18f, 170f, 70f, 0.7f, 0.7f);
                return Finish(d.S, 0.44f);
            case "use_sleep_talk":
                d = new SoundDesign(0.8f);
                d.Tone(0, 0.3f, Wave.Triangle, 200f, 170f, 0.3f, 0.2f, 1.2f, vibratoHz: 5f, vibratoDepth: 0.05f);
                d.Tone(0.3f, 0.35f, Wave.Triangle, 230f, 180f, 0.28f, 0.2f, 1.2f, vibratoHz: 5f, vibratoDepth: 0.05f);
                return Finish(d.S, 0.34f);
            case "use_curse":
                d = new SoundDesign(0.8f, 0xC025u);
                d.Tone(0, 0.7f, Wave.Saw, 70f, 55f, 0.3f, 0.3f, 1.1f, vibratoHz: 6f, vibratoDepth: 0.06f);
                d.Noise(0.25f, 0.4f, Filter.Band, 700f, 300f, 0.25f, 1.5f, 0.2f, 1.4f, 14f, 0.5f);
                return Finish(d.S, 0.4f);
            case "use_endure":
                d = new SoundDesign(0.75f, 0xE0D5u);
                d.Tone(0, 0.7f, Wave.Saw, 90f, 100f, 0.3f, 0.2f, 0.9f, vibratoHz: 3.3f, vibratoDepth: 0.06f);
                d.Thump(0.05f, 0.12f, 160f, 90f, 0.4f, 0.3f);
                d.Thump(0.35f, 0.12f, 160f, 90f, 0.4f, 0.3f);
                return Finish(d.S, 0.4f);
            case "use_frustration":
                d = new SoundDesign(0.6f, 0xF2A5u);
                d.Noise(0, 0.15f, Filter.Band, 500f, 900f, 0.5f, 1.2f, 0.2f, 1.5f);
                foreach (float at in new[] { 0.25f, 0.33f, 0.41f }) d.Thump(at, 0.12f, 220f, 90f, 0.6f, 0.7f);
                return Finish(d.S, 0.44f);
            case "use_mean_look":
                d = new SoundDesign(0.9f, 0x3EA1u);
                d.Tone(0, 0.85f, Wave.Sine, 110f, 98f, 0.4f, 0.3f, 1f);
                d.Noise(0.3f, 0.5f, Filter.Band, 1800f, 900f, 0.25f, 1.6f, 0.3f, 1.4f);
                return Finish(d.S, 0.38f);
            case "use_meditate":
                d = new SoundDesign(0.9f, 0x3ED1u);
                d.Noise(0, 0.5f, Filter.Low, 500f, 300f, 0.3f, 1f, 0.5f, 1f);
                d.Bell(0.35f, 0.5f, N(79), 2.0f, 0.6f, 0.3f, 2f);
                return Finish(d.S, 0.34f);
            case "use_protect":
                d = new SoundDesign(0.6f);
                d.Tone(0, 0.1f, Wave.Sine, 600f, 1200f, 0.3f, 0.1f, 1.5f);
                d.Bell(0.08f, 0.5f, N(96), 1.41f, 1.6f, 0.3f, 2.4f);
                d.Bell(0.08f, 0.5f, N(103), 2.0f, 0.8f, 0.15f, 2.4f);
                return Finish(d.S, 0.36f);
            case "use_spite":
                d = new SoundDesign(0.8f);
                d.Tone(0.3f, 0.45f, Wave.Triangle, 1000f, 620f, 0.3f, 0.2f, 1.2f, vibratoHz: 8f, vibratoDepth: 0.05f);
                d.Tone(0.3f, 0.45f, Wave.Sine, 1060f, 660f, 0.15f, 0.2f, 1.2f, vibratoHz: 9f, vibratoDepth: 0.05f);
                return Finish(d.S, 0.34f);
            case "use_night_shade":
                d = new SoundDesign(0.8f, 0x9195u);
                d.Tone(0, 0.7f, Wave.Saw, 65f, 72f, 0.3f, 0.2f, 1f, vibratoHz: 4f, vibratoDepth: 0.04f);
                d.Noise(0.15f, 0.5f, Filter.Band, 1500f, 400f, 0.4f, 1.3f, 0.2f, 1.4f);
                return Finish(d.S, 0.4f);
            case "use_substitute":
                d = new SoundDesign(0.55f, 0x5B57u);
                d.Noise(0, 0.35f, Filter.Low, 1200f, 500f, 0.5f, 1f, 0.05f, 1.6f);
                d.Tone(0.35f, 0.1f, Wave.Sine, 700f, 1400f, 0.35f, 0.05f, 2f);
                return Finish(d.S, 0.4f);
            case "use_cut":
                d = new SoundDesign(0.45f, 0xC07Fu);
                d.Noise(0.28f, 0.16f, Filter.High, 5600f, 3200f, 0.9f, 1.3f, 0.05f, 2f);
                d.Bell(0.3f, 0.18f, N(98), 3.7f, 1.1f, 0.2f, 3f);
                return Finish(d.S, 0.4f);
            case "use_thief":
                d = new SoundDesign(0.75f, 0x7E1Fu);
                d.Noise(0.2f, 0.15f, Filter.Band, 900f, 3000f, 0.5f, 1.2f, 0.3f, 1.5f);
                d.Thump(0.35f, 0.08f, 300f, 160f, 0.4f, 0.5f);
                d.Notes(0.45f, 0.05f, 0.15f, Wave.Sine, Pitches(96, 100), 0.18f);
                return Finish(d.S, 0.38f);
            case "use_grass_knot":
                d = new SoundDesign(0.6f, 0x6A55u);
                d.Noise(0, 0.32f, Filter.High, 2400f, 4200f, 0.4f, 1f, 0.3f, 1.2f, 26f, 0.6f);
                d.Tone(0.32f, 0.12f, Wave.Triangle, 300f, 180f, 0.3f, 0.05f, 1.6f);
                d.Thump(0.35f, 0.12f, 200f, 90f, 0.5f, 0.6f);
                return Finish(d.S, 0.42f);
            case "use_magical_leaf":
                d = new SoundDesign(0.6f, 0x3A6Fu);
                d.Noise(0, 0.36f, Filter.High, 2400f, 4000f, 0.35f, 1f, 0.2f, 1.2f, 18f, 0.6f);
                d.Notes(0.05f, 0.06f, 0.15f, Wave.Sine, Pitches(91, 95, 98, 103), 0.12f);
                d.Thump(0.35f, 0.08f, 300f, 160f, 0.3f, 0.5f);
                return Finish(d.S, 0.38f);
            case "use_razor_leaf":
                d = new SoundDesign(0.55f, 0x9A2Fu);
                d.Noise(0, 0.36f, Filter.Band, 1500f, 3500f, 0.5f, 2.2f, 0.2f, 1.2f, 32f, 0.7f);
                d.Noise(0.34f, 0.12f, Filter.High, 5200f, 3000f, 0.5f, 1.2f, 0.02f, 2f);
                return Finish(d.S, 0.4f);
            case "use_reflect":
                d = new SoundDesign(0.7f);
                for (int k = 0; k < 4; k++) d.Bell(k * 0.07f, 0.35f, N(86 + k * 3), 1.41f, 1.2f, 0.18f, 2.6f);
                d.Tone(0.35f, 0.3f, Wave.Sine, N(98), N(103), 0.12f, 0.05f, 1.6f);
                return Finish(d.S, 0.34f);
            case "use_safeguard":
                d = new SoundDesign(0.9f);
                d.Notes(0, 0.05f, 0.7f, Wave.Sine, Pitches(76, 80, 83, 88), 0.15f, 0.2f, 1.6f);
                return Finish(d.S, 0.34f);
            case "use_smokescreen":
                d = new SoundDesign(0.8f, 0x5303u);
                d.Noise(0, 0.25f, Filter.Band, 500f, 1500f, 0.25f, 1f, 0.3f, 1.4f);
                d.Thump(0.34f, 0.1f, 220f, 120f, 0.4f, 0.4f);
                d.Noise(0.35f, 0.45f, Filter.Low, 1200f, 300f, 0.5f, 0.8f, 0.05f, 1.2f);
                return Finish(d.S, 0.4f);
            case "use_sunny_day":
                d = new SoundDesign(1.0f);
                d.Tone(0, 0.95f, Wave.Triangle, N(60), N(67), 0.25f, 0.5f, 1.2f);
                d.Tone(0, 0.95f, Wave.Sine, N(76), N(79), 0.18f, 0.5f, 1.2f);
                d.Bell(0.45f, 0.45f, N(91), 2.0f, 0.7f, 0.15f, 2.2f);
                return Finish(d.S, 0.34f);
            case "use_recycle":
                d = new SoundDesign(0.7f, 0x7EC1u);
                d.Noise(0, 0.45f, Filter.Band, 600f, 1800f, 0.35f, 1.4f, 0.2f, 1f, 10f, 0.6f);
                d.Bell(0.45f, 0.22f, N(93), 2.0f, 1.0f, 0.25f, 2.6f);
                return Finish(d.S, 0.36f);
            case "use_taunt":
                d = new SoundDesign(0.7f);
                d.Tone(0, 0.18f, Wave.Pulse, N(74), N(76), 0.2f, 0.05f, 1.2f, duty: 0.25f);
                d.Tone(0.2f, 0.22f, Wave.Pulse, N(71), N(67), 0.2f, 0.05f, 1.4f, duty: 0.25f);
                d.Thump(0.4f, 0.1f, 300f, 180f, 0.25f, 0.4f);
                return Finish(d.S, 0.34f);
            case "use_giga_drain":
                d = new SoundDesign(0.9f, 0x61D4u);
                d.Noise(0.3f, 0.5f, Filter.Band, 400f, 3200f, 0.4f, 1.4f, 0.5f, 1.2f);
                d.Tone(0.32f, 0.48f, Wave.Sine, N(57), N(79), 0.32f, 0.3f, 1.3f, vibratoHz: 6f, vibratoDepth: 0.015f);
                d.Noise(0.4f, 0.35f, Filter.High, 3000f, 4500f, 0.18f, 1f, 0.2f, 1.3f, 20f, 0.6f);
                d.Bell(0.78f, 0.12f, N(91), 2.01f, 1.0f, 0.25f, 2.5f);
                return Finish(d.S, 0.38f);
            case "use_night_slash":
                d = new SoundDesign(0.6f, 0x9157u);
                d.Tone(0, 0.2f, Wave.Saw, 300f, 600f, 0.15f, 0.1f, 1.4f);
                foreach (float at in new[] { 0.29f, 0.43f })
                {
                    d.Noise(at, 0.13f, Filter.High, 5000f, 2600f, 0.75f, 1.3f, 0.05f, 2f);
                    d.Bell(at, 0.12f, N(86), 3.7f, 1.2f, 0.12f, 3f);
                }
                return Finish(d.S, 0.42f);
        }
        return null;
    }
}
