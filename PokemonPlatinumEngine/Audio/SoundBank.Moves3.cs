using static PokemonPlatinumEngine.Audio.SoundDesign;

namespace PokemonPlatinumEngine.Audio;

// Plan 25 · Y7, its second part (Y7b): the sounds of the moves the story shows from Route 206 to Route 208. As Y6's and
// Y7's first part: each move's beats are the original's (when it rings, how often, what it is made of, read from its
// animation script), its sound our own, built from the bank's shared layers. Roar is the user's cry and has none.
public static partial class SoundBank
{
    /// <summary>A sound of Y7's second part, or null for a name that isn't one.</summary>
    private static float[]? MakeMoveSoundY7b(string name)
    {
        SoundDesign d;
        switch (name)
        {
            case "use_double_team":
                d = new SoundDesign(0.75f, 0xD07Eu);
                for (int k = 0; k < 4; k++)
                    d.Noise(k * 0.12f, 0.11f, Filter.Band, 500f + 250f * k, 1800f + 300f * k, 0.6f, 1.2f, 0.3f, 1.4f);
                d.Tone(0.5f, 0.2f, Wave.Sine, 900f, 1500f, 0.12f, 0.05f, 2f);
                return Finish(d.S, 0.4f);
            case "use_endeavor":
                d = new SoundDesign(0.75f, 0xE4DEu);
                for (int k = 0; k < 4; k++)
                {
                    d.Thump(0.1f + k * 0.14f, 0.08f, 420f, 230f, 0.5f, 0.8f);
                    d.Noise(0.1f + k * 0.14f, 0.05f, Filter.High, 3500f, 2500f, 0.45f, 1f, 0.01f, 2.5f);
                }
                d.Tone(0, 0.55f, Wave.Triangle, 220f, 340f, 0.1f, 0.2f, 1.2f);
                return Finish(d.S, 0.42f);
            case "use_flame_wheel":
                d = new SoundDesign(0.85f, 0xF1A3u);
                d.Noise(0, 0.38f, Filter.Band, 500f, 1600f, 0.45f, 1.2f, 0.3f, 1f, 20f, 0.35f);
                d.Tone(0, 0.38f, Wave.Saw, 120f, 260f, 0.14f, 0.3f, 1f, vibratoHz: 16f, vibratoDepth: 0.05f);
                d.Noise(0.4f, 0.4f, Filter.Band, 1800f, 500f, 0.55f, 1.1f, 0.01f, 1.8f);
                d.Crackle(0.4f, 0.35f, 8, 2600f, 0.3f);
                d.Thump(0.4f, 0.14f, 200f, 90f, 0.5f, 0.6f);
                return Finish(d.S, 0.42f);
            case "use_fury_cutter":
                d = new SoundDesign(0.65f, 0xF0C7u);
                foreach (float at in new[] { 0f, 0.14f, 0.3f })
                {
                    d.Noise(at, 0.12f, Filter.High, 6000f, 3000f, 0.7f, 1.3f, 0.03f, 2f);
                    d.Tone(at, 0.1f, Wave.Saw, 500f, 1100f, 0.1f, 0.05f, 1.6f);
                }
                d.Bell(0.32f, 0.2f, N(88), 3.7f, 1.1f, 0.18f, 3f);
                return Finish(d.S, 0.4f);
            case "use_knock_off":
                d = new SoundDesign(0.7f, 0x4B0Fu);
                d.Thump(0.08f, 0.1f, 380f, 160f, 0.6f, 0.9f);
                d.Noise(0.08f, 0.06f, Filter.High, 3500f, 2200f, 0.4f, 1f, 0.01f, 2f);
                d.Tone(0.2f, 0.18f, Wave.Triangle, 600f, 1500f, 0.2f, 0.02f, 1.6f);
                d.Bell(0.38f, 0.25f, N(90), 2.01f, 0.9f, 0.2f, 2.8f);
                return Finish(d.S, 0.4f);
            case "use_magnitude":
                d = new SoundDesign(0.95f, 0x3A61u);
                foreach (float at in new[] { 0.05f, 0.2f, 0.35f, 0.5f })
                {
                    d.Thump(at, 0.2f, 100f, 40f, 0.8f, 0.4f);
                    d.Crackle(at, 0.12f, 4, 900f, 0.35f);
                }
                d.Noise(0, 0.9f, Filter.Low, 300f, 150f, 0.4f, 1f, 0.2f, 1f, 14f, 0.5f);
                return Finish(d.S, 0.46f);
            case "use_seismic_toss":
                d = new SoundDesign(1.0f, 0x5E15u);
                d.Noise(0, 0.28f, Filter.Band, 300f, 2400f, 0.5f, 1.2f, 0.1f, 1.1f);
                d.Noise(0.28f, 0.3f, Filter.Band, 2400f, 400f, 0.45f, 1.2f, 0.05f, 1.2f);
                d.Thump(0.6f, 0.25f, 130f, 45f, 0.95f, 0.6f);
                d.Crackle(0.6f, 0.3f, 8, 1200f, 0.4f);
                d.Thump(0.85f, 0.14f, 150f, 70f, 0.4f, 0.4f);
                return Finish(d.S, 0.46f);
            case "use_self_destruct":
                d = new SoundDesign(1.0f, 0x5DE5u);
                d.Noise(0, 0.34f, Filter.High, 2500f, 6500f, 0.4f, 1f, 0.2f, 1f, 28f, 0.5f);
                d.Tone(0, 0.34f, Wave.Saw, 140f, 520f, 0.12f, 0.3f, 1f);
                d.Thump(0.35f, 0.35f, 120f, 35f, 1f, 0.5f);
                d.Noise(0.35f, 0.6f, Filter.Low, 1800f, 200f, 0.7f, 0.9f, 0.01f, 1.4f);
                return Finish(d.S, 0.48f);
            case "use_slam":
                d = new SoundDesign(0.6f, 0x51A7u);
                d.Noise(0.05f, 0.18f, Filter.Band, 700f, 2600f, 0.55f, 1.2f, 0.4f, 1.3f);
                d.Noise(0.2f, 0.05f, Filter.High, 5000f, 3500f, 0.7f, 1.3f, 0.005f, 3f);
                d.Thump(0.22f, 0.2f, 220f, 70f, 0.9f, 0.9f);
                return Finish(d.S, 0.46f);
            case "use_stomp":
                d = new SoundDesign(0.6f, 0x570Du);
                d.Thump(0.05f, 0.25f, 110f, 42f, 1f, 0.5f);
                d.Crackle(0.05f, 0.1f, 4, 800f, 0.35f);
                d.Thump(0.2f, 0.12f, 260f, 120f, 0.5f, 0.8f);
                return Finish(d.S, 0.46f);
            case "use_block":
                d = new SoundDesign(0.75f, 0xB10Cu);
                foreach (float at in new[] { 0.05f, 0.2f })
                {
                    d.Thump(at, 0.1f, 300f, 140f, 0.6f, 0.9f);
                    d.Bell(at, 0.3f, N(60), 3.7f, 1.3f, 0.3f, 3f);
                }
                d.Bell(0.38f, 0.3f, N(72), 2.4f, 0.8f, 0.14f, 2.8f);
                return Finish(d.S, 0.42f);
            case "use_detect":
                d = new SoundDesign(0.8f, 0xDE7Eu);
                d.Tone(0, 0.22f, Wave.Sine, 500f, 1300f, 0.22f, 0.05f, 1.3f);
                d.Bell(0.2f, 0.4f, N(88), 2.0f, 0.9f, 0.3f, 2.4f);
                d.Bell(0.45f, 0.3f, N(95), 2.01f, 0.7f, 0.15f, 3f);
                return Finish(d.S, 0.38f);
            case "use_mind_reader":
                d = new SoundDesign(0.85f, 0x3D2Eu);
                d.Tone(0, 0.3f, Wave.Sine, 400f, 1200f, 0.2f, 0.1f, 1f, vibratoHz: 22f, vibratoDepth: 0.03f);
                d.Tone(0.35f, 0.3f, Wave.Sine, 1200f, 500f, 0.18f, 0.05f, 1.2f, vibratoHz: 22f, vibratoDepth: 0.03f);
                d.Bell(0.62f, 0.2f, N(91), 2.01f, 1f, 0.25f, 3f);
                return Finish(d.S, 0.36f);
            case "use_disable":
                d = new SoundDesign(0.8f, 0xD15Au);
                d.Tone(0, 0.5f, Wave.Triangle, N(79), N(60), 0.25f, 0.05f, 1.2f, vibratoHz: 9f, vibratoDepth: 0.06f);
                d.Tone(0, 0.5f, Wave.Triangle, N(78), N(58), 0.2f, 0.05f, 1.2f, vibratoHz: 9f, vibratoDepth: 0.06f);
                d.Noise(0.4f, 0.3f, Filter.Low, 1500f, 300f, 0.3f, 1f, 0.05f, 1.6f);
                d.Thump(0.55f, 0.1f, 280f, 150f, 0.35f, 0.5f);
                return Finish(d.S, 0.38f);
            case "use_dragon_rage":
                d = new SoundDesign(0.95f, 0xD8A6u);
                d.Noise(0, 0.5f, Filter.Band, 300f, 1500f, 0.5f, 1.1f, 0.15f, 1f, 18f, 0.4f);
                d.Tone(0, 0.5f, Wave.Saw, 90f, 220f, 0.2f, 0.2f, 0.9f, vibratoHz: 12f, vibratoDepth: 0.08f);
                d.Thump(0.35f, 0.25f, 180f, 50f, 0.8f, 0.6f);
                d.Noise(0.35f, 0.5f, Filter.Band, 1800f, 400f, 0.5f, 1f, 0.02f, 1.5f);
                d.Thump(0.57f, 0.2f, 160f, 55f, 0.6f, 0.5f);
                return Finish(d.S, 0.46f);
            case "use_extrasensory":
                d = new SoundDesign(0.95f, 0xE575u);
                foreach (float at in new[] { 0.04f, 0.2f, 0.36f })
                {
                    d.Tone(at, 0.2f, Wave.Sine, 220f + at * 300f, 520f + at * 500f, 0.25f, 0.04f, 1.2f, vibratoHz: 20f, vibratoDepth: 0.08f);
                    d.Noise(at, 0.18f, Filter.Band, 600f, 2200f, 0.3f, 1.4f, 0.1f, 1.3f);
                }
                d.Bell(0.62f, 0.3f, N(86), 2.01f, 1.1f, 0.25f, 2.6f);
                d.Thump(0.5f, 0.2f, 160f, 70f, 0.5f, 0.4f);
                return Finish(d.S, 0.4f);
            case "use_imprison":
                d = new SoundDesign(0.9f, 0x1B15u);
                for (int k = 0; k < 5; k++)
                {
                    d.Thump(0.04f + k * 0.045f, 0.06f, 500f, 250f, 0.35f, 0.9f);
                    d.Bell(0.04f + k * 0.045f, 0.3f, N(76 + k), 3.7f, 1.0f, 0.15f, 3f);
                }
                d.Bell(0.34f, 0.5f, N(52), 2.4f, 0.9f, 0.3f, 2.4f);
                d.Tone(0.34f, 0.4f, Wave.Sine, N(64), N(60), 0.15f, 0.05f, 1.4f, vibratoHz: 6f, vibratoDepth: 0.02f);
                return Finish(d.S, 0.4f);
            case "use_sand_tomb":
                d = new SoundDesign(1.0f, 0x5A7Du);
                d.Noise(0, 0.95f, Filter.Band, 1500f, 3200f, 0.4f, 1.2f, 0.3f, 1f, 26f, 0.7f);
                d.Noise(0.1f, 0.8f, Filter.Low, 700f, 300f, 0.35f, 1f, 0.3f, 1f, 9f, 0.5f);
                d.Crackle(0.3f, 0.6f, 12, 3000f, 0.25f);
                return Finish(d.S, 0.38f);
            case "use_sandstorm":
                d = new SoundDesign(1.0f, 0x5A57u);
                d.Noise(0, 0.98f, Filter.Band, 600f, 1800f, 0.5f, 1.3f, 0.4f, 1f, 7f, 0.6f);
                d.Noise(0.1f, 0.85f, Filter.High, 3500f, 5200f, 0.28f, 1f, 0.4f, 1f, 30f, 0.5f);
                d.Tone(0.1f, 0.85f, Wave.Sine, 180f, 260f, 0.08f, 0.5f, 1f, vibratoHz: 3f, vibratoDepth: 0.1f);
                return Finish(d.S, 0.38f);
            case "use_tickle":
                d = new SoundDesign(0.85f, 0x71C1u);
                foreach (float at in new[] { 0.15f, 0.27f, 0.39f, 0.51f })
                    d.Tone(at, 0.09f, Wave.Triangle, 900f + 130f * (at * 10f % 3f), 1500f, 0.18f, 0.01f, 1.6f, vibratoHz: 30f, vibratoDepth: 0.04f);
                d.Notes(0, 0.05f, 0.05f, Wave.Sine, Pitches(96, 100, 103), 0.1f);
                d.Noise(0.2f, 0.45f, Filter.High, 5500f, 4500f, 0.15f, 1f, 0.1f, 1.2f, 25f, 0.6f);
                return Finish(d.S, 0.34f);
            case "use_uproar":
                d = new SoundDesign(1.0f, 0x0B20u);
                for (int k = 0; k < 3; k++)
                {
                    d.Tone(k * 0.3f, 0.26f, Wave.Saw, N(60 + k), N(62 + k), 0.2f, 0.02f, 0.7f, vibratoHz: 14f, vibratoDepth: 0.05f);
                    d.Tone(k * 0.3f, 0.26f, Wave.Pulse, N(67 + k), N(69 + k), 0.12f, 0.02f, 0.7f, duty: 0.3f);
                    d.Noise(k * 0.3f, 0.22f, Filter.Band, 900f, 2200f, 0.25f, 1f, 0.05f, 1.1f);
                }
                return Finish(d.S, 0.42f);
            case "use_double_slap":
                d = new SoundDesign(0.6f, 0xD51Au);
                foreach (float at in new[] { 0.1f, 0.35f })
                {
                    d.Noise(at, 0.06f, Filter.High, 4000f, 2500f, 0.6f, 1.2f, 0.003f, 2.5f);
                    d.Thump(at + 0.01f, 0.1f, 360f, 160f, 0.6f, 0.9f);
                }
                return Finish(d.S, 0.44f);
            case "use_follow_me":
                d = new SoundDesign(0.85f, 0xF011u);
                d.Tone(0, 0.2f, Wave.Sine, 600f, 1400f, 0.2f, 0.03f, 1.3f);
                foreach (var (at, midi) in new[] { (0.23f, 84), (0.4f, 88), (0.57f, 91) })
                {
                    d.Tone(at, 0.1f, Wave.Triangle, N(midi), N(midi + 2), 0.22f, 0.01f, 1.8f);
                    d.Bell(at, 0.2f, N(midi + 12), 2.01f, 0.8f, 0.14f, 3f);
                }
                return Finish(d.S, 0.36f);
            case "use_sing":
                d = new SoundDesign(1.0f);
                d.Tone(0, 0.32f, Wave.Sine, N(72), N(72), 0.25f, 0.05f, 1.2f, vibratoHz: 6f, vibratoDepth: 0.02f);
                d.Tone(0.3f, 0.32f, Wave.Sine, N(76), N(76), 0.25f, 0.05f, 1.2f, vibratoHz: 6f, vibratoDepth: 0.02f);
                d.Tone(0.6f, 0.38f, Wave.Sine, N(79), N(77), 0.25f, 0.05f, 1.2f, vibratoHz: 6f, vibratoDepth: 0.02f);
                d.Tone(0, 0.98f, Wave.Triangle, N(60), N(60), 0.08f, 0.2f, 1f);
                d.Bell(0.65f, 0.3f, N(91), 2.01f, 0.7f, 0.12f, 3f);
                return Finish(d.S, 0.36f);
            case "use_bug_bite":
                d = new SoundDesign(0.6f, 0xB06Bu);
                for (int k = 0; k < 4; k++)
                {
                    d.Noise(k * 0.1f, 0.05f, Filter.Band, 2500f, 1500f, 0.6f, 1.4f, 0.004f, 2.5f);
                    d.Thump(k * 0.1f, 0.05f, 700f, 400f, 0.3f, 0.8f);
                }
                d.Crackle(0.1f, 0.25f, 5, 2800f, 0.25f);
                return Finish(d.S, 0.4f);
            case "use_encore":
                d = new SoundDesign(0.95f, 0xE7C0u);
                for (int k = 0; k < 5; k++)
                {
                    d.Thump(0.08f + k * 0.14f, 0.07f, 320f, 160f, 0.35f, 0.9f);
                    d.Noise(0.08f + k * 0.14f, 0.05f, Filter.High, 3800f, 2800f, 0.4f, 1f, 0.005f, 2.5f);
                }
                d.Notes(0.1f, 0.14f, 0.12f, Wave.Triangle, Pitches(79, 83, 86, 83, 91), 0.14f);
                d.Bell(0.7f, 0.3f, N(95), 2.01f, 0.8f, 0.14f, 3f);
                return Finish(d.S, 0.38f);
            case "use_explosion":
                d = new SoundDesign(1.0f, 0xE8F1u);
                d.Noise(0, 0.32f, Filter.High, 2000f, 6800f, 0.45f, 1f, 0.2f, 1f, 30f, 0.5f);
                d.Tone(0, 0.32f, Wave.Saw, 100f, 600f, 0.12f, 0.3f, 1f);
                d.Thump(0.33f, 0.45f, 100f, 28f, 1f, 0.5f);
                d.Noise(0.33f, 0.66f, Filter.Low, 2200f, 150f, 0.8f, 0.9f, 0.01f, 1.3f);
                d.Crackle(0.38f, 0.5f, 14, 1400f, 0.35f);
                d.Thump(0.68f, 0.28f, 90f, 35f, 0.5f, 0.3f);
                return Finish(d.S, 0.48f);
            case "use_flail":
                d = new SoundDesign(0.8f, 0xF1A1u);
                for (int k = 0; k < 6; k++)
                {
                    d.Thump(0.02f + k * 0.075f, 0.06f, 300f + 30f * (k % 3), 150f, 0.4f, 0.8f);
                    d.Noise(0.02f + k * 0.075f, 0.07f, Filter.Band, 600f, 1400f, 0.35f, 1.1f, 0.2f, 1.4f);
                }
                d.Thump(0.5f, 0.15f, 280f, 110f, 0.8f, 0.9f);
                return Finish(d.S, 0.42f);
            case "use_ice_fang":
                d = new SoundDesign(0.8f, 0x1CEFu);
                d.Noise(0.1f, 0.06f, Filter.Band, 2800f, 1600f, 0.6f, 1.4f, 0.004f, 2.5f);
                d.Thump(0.14f, 0.1f, 600f, 250f, 0.5f, 0.9f);
                d.Crackle(0.2f, 0.3f, 7, 5000f, 0.3f);
                d.Bell(0.22f, 0.5f, N(95), 3.7f, 0.9f, 0.25f, 2.6f);
                d.Bell(0.3f, 0.4f, N(100), 2.7f, 0.7f, 0.15f, 3f);
                return Finish(d.S, 0.4f);
            case "use_lucky_chant":
                d = new SoundDesign(1.0f, 0x10C4u);
                d.Notes(0, 0.12f, 0.35f, Wave.Sine, Pitches(79, 83, 86, 91), 0.16f);
                d.Bell(0.1f, 0.5f, N(88), 2.01f, 0.7f, 0.2f, 2.4f);
                d.Bell(0.45f, 0.5f, N(95), 2.01f, 0.8f, 0.18f, 2.4f);
                d.Noise(0.3f, 0.6f, Filter.High, 5000f, 7000f, 0.12f, 1f, 0.2f, 1.2f, 22f, 0.6f);
                return Finish(d.S, 0.36f);
            case "use_mimic":
                d = new SoundDesign(0.95f, 0x31C1u);
                d.Tone(0, 0.38f, Wave.Triangle, N(74), N(81), 0.2f, 0.05f, 1.1f, vibratoHz: 11f, vibratoDepth: 0.07f);
                d.Tone(0.12f, 0.38f, Wave.Triangle, N(74), N(81), 0.14f, 0.05f, 1.1f, vibratoHz: 11f, vibratoDepth: 0.07f);
                d.Tone(0.55f, 0.25f, Wave.Sine, N(86), N(74), 0.2f, 0.03f, 1.4f);
                d.Bell(0.78f, 0.15f, N(93), 2.01f, 0.8f, 0.15f, 3f);
                return Finish(d.S, 0.36f);
            case "use_rest":
                d = new SoundDesign(1.0f, 0x2E57u);
                foreach (float at in new[] { 0f, 0.45f })
                {
                    d.Noise(at, 0.22f, Filter.Low, 500f, 900f, 0.4f, 1f, 0.5f, 1f, 13f, 0.5f);
                    d.Tone(at, 0.22f, Wave.Triangle, 110f, 150f, 0.12f, 0.5f, 1f, vibratoHz: 8f, vibratoDepth: 0.06f);
                    d.Noise(at + 0.24f, 0.24f, Filter.Low, 800f, 350f, 0.35f, 1f, 0.3f, 1f, 11f, 0.5f);
                    d.Tone(at + 0.24f, 0.24f, Wave.Triangle, 150f, 90f, 0.1f, 0.3f, 1f, vibratoHz: 8f, vibratoDepth: 0.06f);
                }
                d.Bell(0.8f, 0.2f, N(84), 2.01f, 0.6f, 0.1f, 3f);
                return Finish(d.S, 0.34f);
            case "use_revenge":
                d = new SoundDesign(1.0f, 0x2E7Eu);
                d.Tone(0, 0.55f, Wave.Saw, 70f, 170f, 0.22f, 0.4f, 0.8f, vibratoHz: 11f, vibratoDepth: 0.06f);
                d.Noise(0.05f, 0.5f, Filter.Band, 300f, 1500f, 0.35f, 1.1f, 0.4f, 1f, 15f, 0.5f);
                d.Thump(0.6f, 0.3f, 160f, 45f, 1f, 0.7f);
                d.Noise(0.6f, 0.2f, Filter.High, 4500f, 2200f, 0.55f, 1.2f, 0.005f, 2f);
                return Finish(d.S, 0.46f);
            case "use_rock_blast":
                d = new SoundDesign(0.9f, 0x20C8u);
                for (int k = 0; k < 4; k++)
                {
                    d.Noise(k * 0.1f, 0.08f, Filter.Band, 700f, 1500f, 0.45f, 1.2f, 0.1f, 1.5f);
                    d.Thump(0.12f + k * 0.1f, 0.12f, 180f + 20f * k, 70f, 0.55f, 0.8f);
                    d.Crackle(0.12f + k * 0.1f, 0.08f, 3, 1500f, 0.3f);
                }
                d.Noise(0.5f, 0.4f, Filter.Low, 900f, 300f, 0.3f, 1f, 0.05f, 1.6f);
                return Finish(d.S, 0.46f);
            case "use_snore":
                d = new SoundDesign(1.0f, 0x5307u);
                foreach (float at in new[] { 0f, 0.5f })
                {
                    d.Noise(at, 0.25f, Filter.Band, 300f, 700f, 0.55f, 1.2f, 0.4f, 1f, 17f, 0.6f);
                    d.Tone(at, 0.25f, Wave.Saw, 90f, 130f, 0.16f, 0.4f, 1f, vibratoHz: 16f, vibratoDepth: 0.08f);
                    d.Noise(at + 0.26f, 0.22f, Filter.Band, 700f, 250f, 0.5f, 1.2f, 0.1f, 1.3f, 17f, 0.6f);
                    d.Tone(at + 0.26f, 0.22f, Wave.Saw, 130f, 80f, 0.14f, 0.1f, 1f, vibratoHz: 16f, vibratoDepth: 0.08f);
                }
                return Finish(d.S, 0.42f);
            case "use_sweet_scent":
                d = new SoundDesign(1.0f, 0x5C3Eu);
                d.Noise(0, 0.9f, Filter.High, 4500f, 6500f, 0.14f, 1f, 0.4f, 1f, 18f, 0.6f);
                d.Notes(0.05f, 0.14f, 0.4f, Wave.Sine, Pitches(84, 88, 91, 95), 0.14f);
                d.Tone(0, 0.9f, Wave.Triangle, N(67), N(72), 0.1f, 0.5f, 1f, vibratoHz: 5f, vibratoDepth: 0.02f);
                d.Bell(0.5f, 0.4f, N(96), 2.01f, 0.6f, 0.1f, 2.6f);
                return Finish(d.S, 0.34f);
            case "use_twister":
                d = new SoundDesign(1.0f, 0x7357u);
                d.Noise(0, 0.95f, Filter.Band, 500f, 2300f, 0.5f, 1.4f, 0.4f, 1f, 11f, 0.7f);
                d.Noise(0.3f, 0.65f, Filter.Band, 2300f, 700f, 0.4f, 1.3f, 0.2f, 1.2f, 15f, 0.5f);
                d.Tone(0.1f, 0.8f, Wave.Sine, 220f, 480f, 0.1f, 0.5f, 1f, vibratoHz: 6f, vibratoDepth: 0.1f);
                d.Thump(0.33f, 0.15f, 160f, 70f, 0.4f, 0.3f);
                return Finish(d.S, 0.4f);
            case "use_worry_seed":
                d = new SoundDesign(0.85f, 0x3077u);
                d.Tone(0, 0.22f, Wave.Sine, 400f, 900f, 0.22f, 0.05f, 1.2f);
                d.Thump(0.34f, 0.08f, 350f, 180f, 0.4f, 0.6f);
                d.Tone(0.4f, 0.18f, Wave.Pulse, N(70), N(69), 0.16f, 0.03f, 1.4f, duty: 0.3f);
                d.Tone(0.58f, 0.22f, Wave.Pulse, N(66), N(64), 0.16f, 0.03f, 1.4f, duty: 0.3f, vibratoHz: 12f, vibratoDepth: 0.04f);
                return Finish(d.S, 0.36f);
        }
        return null;
    }
}
