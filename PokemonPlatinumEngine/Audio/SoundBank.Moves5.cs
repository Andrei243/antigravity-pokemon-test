using static PokemonPlatinumEngine.Audio.SoundDesign;

namespace PokemonPlatinumEngine.Audio;

// Plan 25 · Y8, its second part (Y8b): the sounds of the moves the story shows from Route 210's northern half to the
// Seven Stars Restaurant. As the batches before: each move's beats are the original's (when it rings, how often, what
// it is made of, read from its animation script), its sound our own, built from the bank's shared layers.
public static partial class SoundBank
{
    /// <summary>A sound of Y8's second part, or null for a name that isn't one.</summary>
    private static float[]? MakeMoveSoundY8b(string name)
    {
        SoundDesign d;
        switch (name)
        {
            case "use_air_slash":
                d = new SoundDesign(0.7f, 0xA15Au);
                d.Noise(0, 0.3f, Filter.Band, 600f, 2400f, 0.3f, 1.4f, 0.5f, 1.2f);
                d.Noise(0.28f, 0.2f, Filter.High, 3200f, 6500f, 0.7f, 1.2f, 0.05f, 1.6f, 36f, 0.5f);
                d.Tone(0.28f, 0.3f, Wave.Triangle, 1800f, 900f, 0.1f, 0.02f, 2f);
                d.Bell(0.34f, 0.3f, N(93), 2.76f, 1.0f, 0.12f, 3f);
                return Finish(d.S, 0.4f);
            case "use_ancient_power":
                d = new SoundDesign(1.0f, 0xA9C1u);
                d.Tone(0, 0.35f, Wave.Saw, 70f, 110f, 0.18f, 0.3f, 1.2f, vibratoHz: 8f, vibratoDepth: 0.05f);
                d.Crackle(0.05f, 0.3f, 9, 800f, 0.4f);
                d.Noise(0.38f, 0.2f, Filter.Band, 500f, 1500f, 0.4f, 1.2f, 0.2f, 1.4f);
                d.Thump(0.55f, 0.18f, 150f, 60f, 0.9f, 0.7f);
                d.Crackle(0.57f, 0.3f, 10, 650f, 0.5f);
                d.Noise(0.57f, 0.35f, Filter.Low, 1400f, 300f, 0.4f, 1f, 0.01f, 1.6f);
                return Finish(d.S, 0.44f);
            case "use_aqua_tail":
                d = new SoundDesign(0.95f, 0xA917u);
                d.Noise(0, 0.5f, Filter.Band, 500f, 1800f, 0.3f, 1.3f, 0.4f, 1.2f, 14f, 0.5f);
                d.Bubbles(0.05f, 0.4f, 8, 400f, 1100f, 0.3f);
                d.Noise(0.55f, 0.12f, Filter.High, 3000f, 5000f, 0.8f, 1f, 0.03f, 2f);
                d.Thump(0.58f, 0.2f, 260f, 90f, 0.9f, 0.5f);
                d.Noise(0.6f, 0.35f, Filter.Band, 1200f, 2800f, 0.4f, 1f, 0.01f, 1.8f, 28f, 0.5f);
                return Finish(d.S, 0.44f);
            case "use_assurance":
                d = new SoundDesign(0.5f, 0xA55Au);
                d.Thump(0.02f, 0.14f, 230f, 100f, 0.7f, 0.9f);
                d.Noise(0.02f, 0.05f, Filter.Band, 1500f, 900f, 0.6f, 1f, 0.01f, 2.4f);
                d.Thump(0.19f, 0.22f, 170f, 60f, 1.0f, 0.9f);
                d.Tone(0.19f, 0.25f, Wave.Saw, 120f, 70f, 0.14f, 0.02f, 1.6f);
                d.Noise(0.19f, 0.07f, Filter.Band, 1800f, 1000f, 0.8f, 1f, 0.01f, 2.4f);
                return Finish(d.S, 0.44f);
            case "use_calm_mind":
                d = new SoundDesign(1.0f, 0xCA1Du);
                d.Tone(0, 0.95f, Wave.Sine, N(55), N(60), 0.3f, 0.4f, 1.3f, vibratoHz: 3f, vibratoDepth: 0.015f);
                d.Tone(0.1f, 0.85f, Wave.Triangle, N(67), N(72), 0.1f, 0.4f, 1.3f);
                d.Bell(0.3f, 0.5f, N(86), 2.01f, 0.6f, 0.14f, 2.6f);
                d.Bell(0.6f, 0.4f, N(91), 2.01f, 0.5f, 0.1f, 2.8f);
                return Finish(d.S, 0.34f);
            case "use_double_hit":
                d = new SoundDesign(0.55f, 0xD0B1u);
                foreach (float at in new[] { 0.02f, 0.27f })
                {
                    d.Noise(at, 0.09f, Filter.Band, 600f, 3000f, 0.6f, 1.2f, 0.4f, 1.2f);
                    d.Thump(at + 0.09f, 0.14f, 240f, 110f, 0.85f, 0.9f);
                    d.Noise(at + 0.09f, 0.04f, Filter.High, 3000f, 2200f, 0.5f, 1f, 0.01f, 2.4f);
                }
                return Finish(d.S, 0.44f);
            case "use_dragon_claw":
                d = new SoundDesign(1.0f, 0xDC1Au);
                d.Tone(0, 0.5f, Wave.Saw, 90f, 150f, 0.2f, 0.3f, 1.2f, vibratoHz: 9f, vibratoDepth: 0.08f);
                d.Noise(0, 0.5f, Filter.Band, 400f, 1400f, 0.3f, 1.2f, 0.5f, 1.2f, 20f, 0.5f);
                foreach (float at in new[] { 0.5f, 0.72f })
                {
                    d.Noise(at, 0.16f, Filter.High, 3500f, 6500f, 0.7f, 1.2f, 0.05f, 1.6f, 40f, 0.5f);
                    d.Tone(at, 0.2f, Wave.Saw, 220f, 90f, 0.14f, 0.02f, 1.6f);
                    d.Thump(at + 0.05f, 0.14f, 200f, 90f, 0.4f, 0.5f);
                }
                d.Bell(0.76f, 0.24f, N(55), 1.41f, 1.2f, 0.12f, 3f);
                return Finish(d.S, 0.42f);
            case "use_hyper_fang":
                d = new SoundDesign(0.6f, 0x4F4Eu);
                d.Noise(0, 0.08f, Filter.High, 2500f, 4500f, 0.5f, 1f, 0.2f, 1.4f);
                d.Thump(0.09f, 0.2f, 280f, 90f, 1.0f, 0.9f);
                d.Noise(0.09f, 0.06f, Filter.High, 4200f, 3000f, 0.9f, 1f, 0.01f, 2.4f);
                d.Tone(0.1f, 0.4f, Wave.Triangle, N(76), N(76), 0.1f, 0.01f, 3f);
                d.Thump(0.2f, 0.18f, 200f, 70f, 0.5f, 0.6f);
                return Finish(d.S, 0.44f);
            case "use_ingrain":
                d = new SoundDesign(1.0f, 0x1A91u);
                foreach (float at in new[] { 0.1f, 0.28f, 0.46f })
                {
                    d.Noise(at, 0.22f, Filter.Band, 300f, 900f, 0.4f, 1.4f, 0.3f, 1.3f, 22f, 0.5f);
                    d.Thump(at + 0.06f, 0.12f, 120f, 70f, 0.3f, 0.2f);
                }
                d.Tone(0.2f, 0.7f, Wave.Sine, N(60), N(64), 0.14f, 0.4f, 1.4f, vibratoHz: 4f, vibratoDepth: 0.02f);
                d.Notes(0.55f, 0.1f, 0.25f, Wave.Sine, Pitches(84, 88, 91), 0.07f, 0.02f);
                return Finish(d.S, 0.34f);
            case "use_iron_defense":
                d = new SoundDesign(0.95f, 0x1DEFu);
                foreach (float at in new[] { 0.04f, 0.5f })
                {
                    d.Noise(at, 0.4f, Filter.Band, 1800f, 5000f, 0.3f, 2.5f, 0.3f, 1.4f);
                    d.Bell(at + 0.08f, 0.4f, N(84), 3.7f, 1.4f, 0.2f, 2.6f);
                    d.Bell(at + 0.22f, 0.3f, N(91), 3.01f, 1.2f, 0.14f, 3f);
                }
                d.Tone(0, 0.9f, Wave.Triangle, 150f, 150f, 0.07f, 0.2f, 1.6f);
                return Finish(d.S, 0.38f);
            case "use_mist":
                d = new SoundDesign(1.0f, 0x3157u);
                d.Noise(0, 0.95f, Filter.Band, 1500f, 3800f, 0.4f, 0.8f, 0.4f, 1.3f, 9f, 0.4f);
                d.Noise(0.05f, 0.9f, Filter.High, 4500f, 6500f, 0.1f, 1f, 0.5f, 1.3f);
                d.Tone(0.1f, 0.8f, Wave.Sine, N(76), N(79), 0.07f, 0.5f, 1.4f, vibratoHz: 5f, vibratoDepth: 0.02f);
                return Finish(d.S, 0.34f);
            case "use_poison_powder":
                d = new SoundDesign(1.0f, 0x9A0Du);
                d.Noise(0, 0.35f, Filter.Band, 700f, 1800f, 0.4f, 1f, 0.3f, 1.2f);
                d.Noise(0.2f, 0.75f, Filter.High, 3800f, 6000f, 0.25f, 1f, 0.2f, 1.2f, 24f, 0.7f);
                d.Crackle(0.3f, 0.6f, 14, 5000f, 0.2f);
                d.Tone(0.3f, 0.65f, Wave.Saw, 130f, 100f, 0.07f, 0.3f, 1.4f, vibratoHz: 7f, vibratoDepth: 0.06f);
                return Finish(d.S, 0.34f);
            case "use_submission":
                d = new SoundDesign(1.0f, 0x5B51u);
                d.Noise(0, 0.15f, Filter.Band, 500f, 1500f, 0.5f, 1f, 0.3f, 1.4f);
                d.Tone(0.1f, 0.5f, Wave.Saw, 140f, 260f, 0.12f, 0.3f, 1.3f, vibratoHz: 12f, vibratoDepth: 0.1f);
                for (int k = 0; k < 5; k++)
                {
                    d.Noise(0.3f + k * 0.12f, 0.06f, Filter.Band, 1500f, 900f, 0.6f, 1f, 0.01f, 2.4f);
                    d.Thump(0.31f + k * 0.12f, 0.1f, 230f, 110f, 0.6f, 0.5f);
                }
                return Finish(d.S, 0.42f);
            case "use_vital_throw":
                d = new SoundDesign(1.0f, 0x7177u);
                d.Noise(0, 0.45f, Filter.Band, 400f, 2200f, 0.4f, 1.3f, 0.5f, 1.2f, 16f, 0.5f);
                d.Tone(0, 0.45f, Wave.Sine, 180f, 420f, 0.1f, 0.4f, 1.2f);
                d.Noise(0.45f, 0.25f, Filter.Band, 2200f, 500f, 0.35f, 1.2f, 0.1f, 1.4f);
                d.Thump(0.7f, 0.28f, 170f, 50f, 1.0f, 0.8f);
                d.Noise(0.7f, 0.25f, Filter.Low, 1200f, 250f, 0.5f, 1f, 0.01f, 1.6f);
                return Finish(d.S, 0.44f);
            case "use_whirlpool":
                d = new SoundDesign(1.0f, 0x331Fu);
                d.Noise(0, 0.95f, Filter.Band, 400f, 1600f, 0.45f, 1.5f, 0.35f, 1.1f, 7f, 0.7f);
                d.Noise(0.2f, 0.75f, Filter.Band, 1600f, 600f, 0.3f, 1.3f, 0.2f, 1.3f, 11f, 0.5f);
                d.Bubbles(0.1f, 0.8f, 12, 250f, 800f, 0.25f);
                d.Tone(0, 0.9f, Wave.Sine, 130f, 230f, 0.1f, 0.5f, 1.3f, vibratoHz: 6f, vibratoDepth: 0.08f);
                return Finish(d.S, 0.4f);
            case "use_beat_up":
                d = new SoundDesign(0.5f, 0xBEA7u);
                for (int k = 0; k < 3; k++)
                {
                    d.Noise(0.04f + k * 0.04f, 0.05f, Filter.Band, 1400f, 900f, 0.6f, 1f, 0.01f, 2.4f);
                    d.Thump(0.05f + k * 0.04f, 0.1f, 220f + k * 30f, 100f, 0.6f, 0.8f);
                }
                d.Thump(0.2f, 0.18f, 190f, 70f, 0.9f, 0.8f);
                d.Notes(0.28f, 0.05f, 0.1f, Wave.Sine, Pitches(88, 91, 95), 0.07f, 0.01f);
                return Finish(d.S, 0.42f);
            case "use_fire_fang":
                d = new SoundDesign(0.8f, 0xF1F4u);
                d.Noise(0, 0.12f, Filter.High, 2500f, 4500f, 0.4f, 1f, 0.2f, 1.4f);
                d.Thump(0.13f, 0.16f, 260f, 90f, 0.9f, 0.9f);
                d.Noise(0.16f, 0.55f, Filter.Band, 600f, 2400f, 0.55f, 1.2f, 0.05f, 1.5f, 25f, 0.6f);
                d.Crackle(0.2f, 0.45f, 14, 2800f, 0.3f);
                d.Tone(0.18f, 0.5f, Wave.Saw, 100f, 70f, 0.1f, 0.05f, 1.5f);
                return Finish(d.S, 0.42f);
            case "use_odor_sleuth":
                d = new SoundDesign(0.95f, 0x0D05u);
                for (int k = 0; k < 4; k++)
                    d.Noise(0.04f + k * 0.14f, 0.1f, Filter.Band, 1200f, 2600f, 0.35f, 1.5f, 0.3f, 1.6f);
                d.Tone(0, 0.6f, Wave.Sine, N(72), N(76), 0.07f, 0.3f, 1.4f, vibratoHz: 8f, vibratoDepth: 0.04f);
                d.Thump(0.62f, 0.12f, 300f, 160f, 0.5f, 0.6f);
                d.Notes(0.7f, 0.09f, 0.18f, Wave.Triangle, Pitches(81, 88), 0.1f, 0.01f);
                return Finish(d.S, 0.38f);
            case "use_kinesis":
                d = new SoundDesign(1.0f, 0x41E5u);
                d.Tone(0, 0.5f, Wave.Sine, N(62), N(66), 0.2f, 0.2f, 1.3f, vibratoHz: 5f, vibratoDepth: 0.05f);
                foreach (float at in new[] { 0.12f, 0.4f, 0.68f })
                    d.Bell(at, 0.3f, N(at > 0.5f ? 91 : 84), 2.01f, 0.9f, 0.2f, 2.6f);
                d.Tone(0.6f, 0.35f, Wave.Triangle, N(70), N(58), 0.12f, 0.02f, 1.6f, vibratoHz: 9f, vibratoDepth: 0.05f);
                return Finish(d.S, 0.36f);
            case "use_metal_sound":
                d = new SoundDesign(0.85f, 0x3E7Fu);
                d.Noise(0, 0.6f, Filter.Band, 3500f, 6200f, 0.5f, 3f, 0.05f, 1.2f, 40f, 0.6f);
                d.Tone(0, 0.6f, Wave.Saw, 2400f, 3000f, 0.08f, 0.05f, 1.4f, vibratoHz: 30f, vibratoDepth: 0.03f);
                foreach (float at in new[] { 0.01f, 0.1f, 0.25f })
                    d.Bell(at, 0.4f, N(96), 3.7f, 1.5f, 0.15f, 2.4f);
                d.Noise(0.6f, 0.2f, Filter.High, 5500f, 3500f, 0.2f, 1f, 0.02f, 2f);
                return Finish(d.S, 0.38f);
            case "use_miracle_eye":
                d = new SoundDesign(1.0f, 0x31E7u);
                d.Tone(0, 0.5f, Wave.Sine, N(67), N(79), 0.18f, 0.4f, 1.2f, vibratoHz: 10f, vibratoDepth: 0.03f);
                d.Notes(0.1f, 0.08f, 0.2f, Wave.Triangle, Pitches(84, 88, 91), 0.08f, 0.01f);
                d.Bell(0.45f, 0.5f, N(96), 2.01f, 1.0f, 0.22f, 2.4f);
                d.Bell(0.45f, 0.5f, N(84), 3.01f, 0.8f, 0.12f, 2.4f);
                d.Noise(0.45f, 0.4f, Filter.High, 5000f, 7500f, 0.1f, 1f, 0.05f, 1.6f, 20f, 0.6f);
                return Finish(d.S, 0.36f);
            case "use_ominous_wind":
                d = new SoundDesign(1.0f, 0x03F1u);
                foreach (float at in new[] { 0f, 0.45f })
                {
                    d.Noise(at, 0.5f, Filter.Band, 400f, 1500f, 0.45f, 1.5f, 0.4f, 1.2f, 9f, 0.5f);
                    d.Tone(at, 0.5f, Wave.Saw, 120f, 90f, 0.1f, 0.3f, 1.3f, vibratoHz: 7f, vibratoDepth: 0.1f);
                }
                d.Bell(0.38f, 0.4f, N(58), 1.41f, 1.2f, 0.12f, 2.8f);
                d.Thump(0.4f, 0.12f, 140f, 60f, 0.3f, 0.2f);
                return Finish(d.S, 0.4f);
            case "use_psychic":
                d = new SoundDesign(1.0f, 0x7512u);
                d.Tone(0, 0.9f, Wave.Sine, N(60), N(72), 0.2f, 0.3f, 1.2f, vibratoHz: 12f, vibratoDepth: 0.1f);
                d.Tone(0.05f, 0.9f, Wave.Triangle, N(67), N(79), 0.1f, 0.3f, 1.2f, vibratoHz: 9f, vibratoDepth: 0.12f);
                d.Noise(0.1f, 0.7f, Filter.Band, 900f, 3200f, 0.2f, 1.6f, 0.4f, 1.2f, 16f, 0.5f);
                d.Bell(0.6f, 0.35f, N(88), 2.01f, 1.0f, 0.2f, 2.8f);
                d.Thump(0.62f, 0.25f, 150f, 55f, 0.6f, 0.3f);
                return Finish(d.S, 0.4f);
            case "use_wrap":
                d = new SoundDesign(1.0f, 0x3A09u);
                foreach (float at in new[] { 0.02f, 0.5f })
                {
                    d.Noise(at, 0.4f, Filter.Band, 500f, 1600f, 0.4f, 1.3f, 0.4f, 1.3f, 14f, 0.6f);
                    d.Tone(at, 0.4f, Wave.Triangle, 180f, 120f, 0.1f, 0.3f, 1.4f, vibratoHz: 6f, vibratoDepth: 0.06f);
                    d.Thump(at + 0.3f, 0.12f, 200f, 100f, 0.3f, 0.3f);
                }
                return Finish(d.S, 0.4f);
            case "use_embargo":
                d = new SoundDesign(0.8f, 0xE3B4u);
                d.Tone(0, 0.2f, Wave.Pulse, N(64), N(60), 0.16f, 0.02f, 1.5f, 0.3f);
                d.Tone(0.17f, 0.3f, Wave.Pulse, N(57), N(52), 0.18f, 0.02f, 1.5f, 0.3f);
                d.Thump(0.3f, 0.2f, 180f, 60f, 0.9f, 0.7f);
                d.Bell(0.32f, 0.4f, N(52), 2.4f, 1.2f, 0.16f, 2.6f);
                d.Noise(0.3f, 0.3f, Filter.Low, 900f, 250f, 0.3f, 1f, 0.01f, 1.8f);
                return Finish(d.S, 0.4f);
            case "use_gravity":
                d = new SoundDesign(1.0f, 0x64A7u);
                d.Tone(0, 0.45f, Wave.Sine, 240f, 70f, 0.3f, 0.2f, 1.2f, vibratoHz: 5f, vibratoDepth: 0.05f);
                d.Tone(0.05f, 0.4f, Wave.Saw, 160f, 55f, 0.1f, 0.2f, 1.3f);
                d.Noise(0.1f, 0.4f, Filter.Low, 1500f, 200f, 0.35f, 1f, 0.3f, 1.3f);
                d.Thump(0.55f, 0.3f, 120f, 40f, 1.0f, 0.5f);
                d.Noise(0.55f, 0.4f, Filter.Low, 600f, 150f, 0.45f, 1f, 0.01f, 1.6f);
                d.Bell(0.6f, 0.35f, N(46), 1.41f, 1.0f, 0.1f, 3f);
                return Finish(d.S, 0.44f);
            case "use_psycho_cut":
                d = new SoundDesign(0.9f, 0x75C7u);
                d.Tone(0, 0.3f, Wave.Sine, N(72), N(79), 0.14f, 0.3f, 1.3f, vibratoHz: 10f, vibratoDepth: 0.05f);
                foreach (float at in new[] { 0.28f, 0.4f, 0.52f })
                {
                    d.Noise(at, 0.1f, Filter.High, 3500f, 6500f, 0.7f, 1.2f, 0.05f, 1.8f, 40f, 0.5f);
                    d.Bell(at + 0.03f, 0.25f, N(at > 0.5f ? 100 : 93), 2.76f, 1.0f, 0.14f, 3f);
                }
                d.Thump(0.54f, 0.2f, 220f, 90f, 0.5f, 0.5f);
                return Finish(d.S, 0.4f);
            case "use_torment":
                d = new SoundDesign(1.0f, 0x70A7u);
                for (int k = 0; k < 6; k++)
                {
                    d.Tone(0.03f + k * 0.07f, 0.06f, Wave.Pulse, 520f + k * 30f, 380f, 0.15f, 0.01f, 2f, 0.25f);
                    d.Noise(0.03f + k * 0.07f, 0.03f, Filter.High, 3500f, 3000f, 0.35f, 1f, 0.01f, 2.4f);
                }
                d.Tone(0.5f, 0.45f, Wave.Saw, 110f, 80f, 0.16f, 0.1f, 1.4f, vibratoHz: 13f, vibratoDepth: 0.1f);
                d.Noise(0.5f, 0.4f, Filter.Band, 500f, 1200f, 0.3f, 1.5f, 0.1f, 1.4f, 18f, 0.6f);
                return Finish(d.S, 0.4f);
            case "use_assist":
                d = new SoundDesign(0.8f, 0xA555u);
                for (int k = 0; k < 4; k++)
                {
                    d.Noise(0.02f + k * 0.1f, 0.08f, Filter.Band, 1500f, 900f, 0.4f, 1f, 0.02f, 2f);
                    d.Bell(0.02f + k * 0.1f, 0.2f, N(79 + k * 2), 2.01f, 0.8f, 0.12f, 3f);
                }
                d.Bell(0.45f, 0.35f, N(96), 2.01f, 1.0f, 0.22f, 2.4f);
                d.Notes(0.5f, 0.06f, 0.14f, Wave.Sine, Pitches(88, 91, 95), 0.08f, 0.01f);
                return Finish(d.S, 0.38f);
            case "use_bone_rush":
                d = new SoundDesign(0.7f, 0xB04Eu);
                foreach (float at in new[] { 0.04f, 0.2f })
                {
                    d.Noise(at, 0.1f, Filter.Band, 600f, 2400f, 0.4f, 1.2f, 0.4f, 1.2f, 22f, 0.5f);
                    d.Thump(at + 0.1f, 0.12f, 280f, 140f, 0.8f, 0.9f);
                    d.Tone(at + 0.1f, 0.14f, Wave.Triangle, 1100f, 700f, 0.1f, 0.01f, 2.4f, vibratoHz: 30f, vibratoDepth: 0.05f);
                }
                d.Crackle(0.32f, 0.25f, 8, 1500f, 0.3f);
                return Finish(d.S, 0.42f);
            case "use_captivate":
                d = new SoundDesign(0.95f, 0xCA97u);
                d.Notes(0.04f, 0.09f, 0.2f, Wave.Sine, Pitches(88, 91, 95, 100), 0.14f, 0.01f);
                d.Bell(0.04f, 0.3f, N(100), 2.01f, 0.8f, 0.14f, 2.8f);
                d.Noise(0.3f, 0.5f, Filter.High, 4500f, 7000f, 0.12f, 1f, 0.3f, 1.4f, 20f, 0.6f);
                d.Tone(0.3f, 0.5f, Wave.Triangle, N(84), N(88), 0.09f, 0.2f, 1.6f, vibratoHz: 6f, vibratoDepth: 0.03f);
                d.Notes(0.6f, 0.07f, 0.15f, Wave.Sine, Pitches(91, 95), 0.09f, 0.01f);
                return Finish(d.S, 0.36f);
            case "use_drain_punch":
                d = new SoundDesign(1.0f, 0xD4A1u);
                d.Thump(0.04f, 0.14f, 250f, 100f, 0.85f, 0.9f);
                d.Noise(0.04f, 0.05f, Filter.Band, 1600f, 900f, 0.7f, 1f, 0.01f, 2.4f);
                d.Tone(0.2f, 0.7f, Wave.Sine, N(55), N(76), 0.2f, 0.3f, 1.3f, vibratoHz: 11f, vibratoDepth: 0.06f);
                d.Noise(0.2f, 0.65f, Filter.Band, 500f, 2400f, 0.25f, 1.4f, 0.4f, 1.3f, 24f, 0.5f);
                d.Bell(0.8f, 0.18f, N(91), 2.01f, 0.8f, 0.14f, 3f);
                return Finish(d.S, 0.4f);
            case "use_fly":
                d = new SoundDesign(1.0f, 0xF17Eu);
                d.Noise(0, 0.3f, Filter.Band, 500f, 2200f, 0.4f, 1.4f, 0.4f, 1.2f, 16f, 0.5f);
                d.Tone(0, 0.3f, Wave.Sine, 220f, 640f, 0.08f, 0.3f, 1.3f);
                d.Noise(0.3f, 0.3f, Filter.Band, 2800f, 700f, 0.5f, 1.2f, 0.3f, 1.1f);
                d.Tone(0.3f, 0.3f, Wave.Sine, 900f, 200f, 0.1f, 0.05f, 1.2f);
                d.Thump(0.6f, 0.22f, 200f, 60f, 1.0f, 0.8f);
                d.Noise(0.6f, 0.3f, Filter.Band, 1500f, 400f, 0.4f, 1f, 0.01f, 1.8f);
                return Finish(d.S, 0.44f);
            case "use_recover":
                d = new SoundDesign(1.0f, 0x2EC0u);
                d.Tone(0, 0.8f, Wave.Sine, N(60), N(67), 0.2f, 0.3f, 1.3f, vibratoHz: 4f, vibratoDepth: 0.02f);
                d.Noise(0.05f, 0.6f, Filter.High, 5000f, 7500f, 0.1f, 1f, 0.3f, 1.3f, 16f, 0.5f);
                d.Notes(0.1f, 0.1f, 0.3f, Wave.Sine, Pitches(79, 84, 88), 0.12f, 0.01f);
                d.Bell(0.6f, 0.4f, N(96), 2.01f, 0.8f, 0.2f, 2.4f);
                d.Bell(0.62f, 0.38f, N(88), 3.01f, 0.6f, 0.1f, 2.6f);
                return Finish(d.S, 0.36f);
            case "use_snatch":
                d = new SoundDesign(0.9f, 0x54A7u);
                d.Noise(0, 0.25f, Filter.Band, 600f, 2200f, 0.4f, 1.3f, 0.5f, 1.2f);
                d.Thump(0.26f, 0.1f, 300f, 140f, 0.5f, 0.7f);
                d.Tone(0.28f, 0.2f, Wave.Pulse, N(67), N(79), 0.14f, 0.02f, 1.6f, 0.3f);
                d.Noise(0.4f, 0.3f, Filter.Band, 2000f, 700f, 0.3f, 1.2f, 0.1f, 1.4f);
                d.Notes(0.6f, 0.07f, 0.14f, Wave.Sine, Pitches(91, 95, 100), 0.1f, 0.01f);
                return Finish(d.S, 0.38f);
            case "use_strength":
                d = new SoundDesign(0.95f, 0x57E9u);
                d.Tone(0, 0.3f, Wave.Saw, 80f, 140f, 0.18f, 0.4f, 1.2f, vibratoHz: 10f, vibratoDepth: 0.06f);
                d.Noise(0, 0.28f, Filter.Band, 400f, 1200f, 0.3f, 1.2f, 0.5f, 1.2f);
                d.Thump(0.32f, 0.3f, 160f, 45f, 1.0f, 0.9f);
                d.Noise(0.32f, 0.08f, Filter.Band, 1600f, 800f, 0.7f, 1f, 0.01f, 2.4f);
                d.Crackle(0.38f, 0.3f, 9, 700f, 0.4f);
                d.Noise(0.4f, 0.45f, Filter.Low, 1100f, 250f, 0.4f, 1f, 0.01f, 1.6f);
                return Finish(d.S, 0.44f);
            case "use_ice_beam":
                d = new SoundDesign(1.0f, 0x1CEBu);
                d.Noise(0, 0.5f, Filter.Band, 2400f, 5200f, 0.35f, 2f, 0.1f, 1.1f, 22f, 0.5f);
                d.Tone(0.02f, 0.5f, Wave.Triangle, N(88), N(96), 0.12f, 0.1f, 1.2f, vibratoHz: 18f, vibratoDepth: 0.03f);
                d.Crackle(0.4f, 0.5f, 16, 4200f, 0.3f);
                d.Bell(0.42f, 0.5f, N(100), 3.01f, 1.0f, 0.2f, 2.4f);
                d.Thump(0.45f, 0.15f, 220f, 90f, 0.4f, 0.4f);
                return Finish(d.S, 0.4f);
            case "use_poison_fang":
                d = new SoundDesign(0.95f, 0x9FA4u);
                d.Noise(0, 0.1f, Filter.High, 2500f, 4500f, 0.4f, 1f, 0.2f, 1.4f);
                d.Thump(0.1f, 0.16f, 250f, 90f, 0.85f, 0.9f);
                d.Noise(0.1f, 0.05f, Filter.High, 4000f, 3000f, 0.7f, 1f, 0.01f, 2.4f);
                d.Bubbles(0.2f, 0.6f, 9, 300f, 900f, 0.3f);
                d.Tone(0.2f, 0.6f, Wave.Saw, 110f, 80f, 0.08f, 0.2f, 1.4f, vibratoHz: 8f, vibratoDepth: 0.08f);
                return Finish(d.S, 0.4f);
            case "use_poison_jab":
                d = new SoundDesign(0.65f, 0x9AB1u);
                foreach (float at in new[] { 0.02f, 0.18f })
                {
                    d.Noise(at, 0.06f, Filter.Band, 1500f, 700f, 0.6f, 1f, 0.01f, 2.2f);
                    d.Thump(at, 0.1f, 260f, 120f, 0.7f, 0.8f);
                    d.Bubbles(at + 0.04f, 0.15f, 3, 400f, 1000f, 0.3f);
                }
                d.Tone(0.2f, 0.4f, Wave.Saw, 140f, 100f, 0.09f, 0.1f, 1.5f, vibratoHz: 10f, vibratoDepth: 0.08f);
                return Finish(d.S, 0.42f);
            case "use_signal_beam":
                d = new SoundDesign(1.0f, 0x516Bu);
                for (int k = 0; k < 6; k++)
                    d.Tone(0.04f + k * 0.14f, 0.16f, Wave.Pulse, k % 3 == 0 ? 880f : k % 3 == 1 ? 1175f : 1480f, k % 3 == 0 ? 880f : k % 3 == 1 ? 1175f : 1480f, 0.1f, 0.02f, 1.8f, 0.35f);
                d.Noise(0.05f, 0.8f, Filter.Band, 1500f, 3500f, 0.2f, 1.6f, 0.2f, 1.2f, 20f, 0.5f);
                d.Bell(0.82f, 0.18f, N(91), 2.01f, 0.8f, 0.14f, 3f);
                return Finish(d.S, 0.36f);
            case "use_sludge_bomb":
                d = new SoundDesign(0.95f, 0x5B0Bu);
                d.Tone(0, 0.3f, Wave.Sine, 300f, 160f, 0.2f, 0.1f, 1.4f, vibratoHz: 9f, vibratoDepth: 0.1f);
                d.Bubbles(0, 0.3f, 6, 250f, 700f, 0.3f);
                d.Thump(0.33f, 0.2f, 200f, 70f, 0.9f, 0.5f);
                d.Noise(0.33f, 0.45f, Filter.Band, 600f, 1500f, 0.5f, 1f, 0.02f, 1.6f, 20f, 0.6f);
                d.Bubbles(0.4f, 0.5f, 10, 200f, 900f, 0.3f);
                return Finish(d.S, 0.42f);
            case "use_tri_attack":
                d = new SoundDesign(1.0f, 0x7A1Au);
                d.Tone(0, 0.35f, Wave.Sine, N(60), N(72), 0.16f, 0.4f, 1.2f, vibratoHz: 10f, vibratoDepth: 0.04f);
                d.Notes(0.1f, 0.07f, 0.16f, Wave.Triangle, Pitches(72, 76, 79), 0.08f, 0.01f);
                d.Noise(0.4f, 0.25f, Filter.Band, 500f, 2400f, 0.5f, 1.2f, 0.05f, 1.6f, 28f, 0.5f);
                d.Noise(0.58f, 0.2f, Filter.High, 3500f, 6500f, 0.5f, 1.4f, 0.05f, 1.8f, 40f, 0.5f);
                d.Bell(0.6f, 0.2f, N(91), 2.01f, 1.0f, 0.14f, 3f);
                d.Crackle(0.75f, 0.2f, 8, 4800f, 0.3f);
                d.Bell(0.78f, 0.22f, N(100), 3.01f, 1.0f, 0.16f, 3f);
                return Finish(d.S, 0.4f);
            case "use_memento":
                d = new SoundDesign(1.0f, 0x3E3Eu);
                d.Tone(0, 0.8f, Wave.Saw, 150f, 70f, 0.16f, 0.3f, 1.2f, vibratoHz: 6f, vibratoDepth: 0.1f);
                d.Noise(0, 0.6f, Filter.Band, 800f, 300f, 0.35f, 1.5f, 0.4f, 1.2f, 10f, 0.5f);
                d.Bell(0.4f, 0.55f, N(57), 1.41f, 1.3f, 0.16f, 2.6f);
                d.Tone(0.55f, 0.4f, Wave.Sine, N(55), N(46), 0.2f, 0.05f, 1.5f);
                return Finish(d.S, 0.38f);
            case "use_drill_peck":
                d = new SoundDesign(0.95f, 0xD111u);
                d.Tone(0, 0.45f, Wave.Saw, 500f, 1500f, 0.12f, 0.5f, 1.2f, vibratoHz: 40f, vibratoDepth: 0.06f);
                d.Noise(0, 0.4f, Filter.Band, 1200f, 3800f, 0.3f, 1.5f, 0.6f, 1.2f, 40f, 0.5f);
                for (int k = 0; k < 8; k++)
                {
                    d.Noise(0.4f + k * 0.045f, 0.03f, Filter.High, 3200f, 2600f, 0.45f, 1f, 0.02f, 2.4f);
                    d.Tone(0.4f + k * 0.045f, 0.04f, Wave.Pulse, 1200f + k * 40f, 800f, 0.1f, 0.01f, 2f, 0.3f);
                }
                d.Thump(0.4f, 0.14f, 230f, 90f, 0.7f, 0.5f);
                return Finish(d.S, 0.42f);
            case "use_ice_punch":
                d = new SoundDesign(0.85f, 0x1C9Eu);
                d.Noise(0, 0.1f, Filter.Band, 1000f, 3000f, 0.4f, 1.2f, 0.4f, 1.3f);
                d.Thump(0.1f, 0.14f, 260f, 110f, 0.85f, 0.9f);
                d.Noise(0.1f, 0.04f, Filter.High, 4000f, 3000f, 0.7f, 1f, 0.01f, 2.4f);
                d.Crackle(0.14f, 0.5f, 14, 4500f, 0.3f);
                d.Bell(0.16f, 0.5f, N(98), 3.01f, 1.0f, 0.2f, 2.6f);
                d.Bell(0.3f, 0.4f, N(103), 2.01f, 0.8f, 0.1f, 3f);
                return Finish(d.S, 0.4f);
            case "use_gyro_ball":
                d = new SoundDesign(1.0f, 0x6190u);
                d.Tone(0, 0.45f, Wave.Saw, 120f, 900f, 0.14f, 0.5f, 1.2f, vibratoHz: 24f, vibratoDepth: 0.05f);
                d.Noise(0, 0.45f, Filter.Band, 800f, 4200f, 0.3f, 1.8f, 0.5f, 1.2f, 30f, 0.6f);
                d.Thump(0.5f, 0.2f, 220f, 70f, 0.9f, 0.8f);
                d.Noise(0.5f, 0.25f, Filter.High, 3500f, 5500f, 0.6f, 1.2f, 0.03f, 1.8f, 40f, 0.6f);
                d.Bell(0.52f, 0.45f, N(88), 3.7f, 1.4f, 0.2f, 2.4f);
                return Finish(d.S, 0.42f);
            case "use_x_scissor":
                d = new SoundDesign(0.65f, 0x55C1u);
                foreach (float at in new[] { 0.04f, 0.18f })
                {
                    d.Noise(at, 0.13f, Filter.High, 3500f, 6800f, 0.7f, 1.2f, 0.05f, 1.6f, 40f, 0.5f);
                    d.Bell(at + 0.03f, 0.2f, N(at > 0.1f ? 103 : 98), 3.7f, 1.0f, 0.18f, 3f);
                }
                d.Thump(0.2f, 0.14f, 230f, 100f, 0.4f, 0.5f);
                d.Bell(0.22f, 0.4f, N(67), 2.76f, 1.0f, 0.1f, 2.6f);
                return Finish(d.S, 0.4f);
            case "use_horn_drill":
                d = new SoundDesign(1.0f, 0x4D91u);
                foreach (float at in new[] { 0.02f, 0.3f, 0.58f })
                {
                    d.Tone(at, 0.24f, Wave.Saw, 300f + at * 200f, 1100f + at * 400f, 0.12f, 0.4f, 1.2f, vibratoHz: 38f, vibratoDepth: 0.06f);
                    d.Noise(at, 0.24f, Filter.Band, 1200f, 3500f, 0.3f, 1.6f, 0.4f, 1.3f, 38f, 0.5f);
                    d.Thump(at + 0.2f, 0.12f, 240f, 90f, 0.7f, 0.5f);
                }
                d.Bell(0.8f, 0.2f, N(60), 1.41f, 1.0f, 0.12f, 3f);
                return Finish(d.S, 0.42f);
            case "use_last_resort":
                d = new SoundDesign(1.0f, 0x1A57u);
                d.Notes(0, 0.05f, 0.12f, Wave.Triangle, Pitches(72, 76, 79, 84), 0.1f, 0.01f);
                d.Tone(0, 0.4f, Wave.Sine, N(48), N(60), 0.2f, 0.5f, 1.2f);
                d.Thump(0.4f, 0.2f, 210f, 80f, 0.8f, 0.8f);
                d.Noise(0.4f, 0.05f, Filter.Band, 1500f, 800f, 0.7f, 1f, 0.01f, 2.4f);
                d.Thump(0.62f, 0.3f, 170f, 45f, 1.0f, 0.9f);
                d.Noise(0.62f, 0.07f, Filter.Band, 1700f, 800f, 0.8f, 1f, 0.01f, 2.4f);
                d.Noise(0.65f, 0.35f, Filter.Low, 1300f, 250f, 0.4f, 1f, 0.01f, 1.6f);
                d.Bell(0.66f, 0.3f, N(55), 1.41f, 1.2f, 0.12f, 3f);
                return Finish(d.S, 0.44f);
            case "use_magnet_rise":
                d = new SoundDesign(1.0f, 0x3A61u);
                d.Tone(0, 0.9f, Wave.Sine, 110f, 220f, 0.22f, 0.4f, 1.2f, vibratoHz: 30f, vibratoDepth: 0.03f);
                d.Tone(0, 0.9f, Wave.Triangle, 165f, 330f, 0.08f, 0.4f, 1.2f, vibratoHz: 24f, vibratoDepth: 0.04f);
                foreach (float at in new[] { 0.1f, 0.5f })
                    d.Bell(at, 0.35f, N(at > 0.3f ? 91 : 84), 2.01f, 0.8f, 0.16f, 2.6f);
                d.Crackle(0.3f, 0.5f, 10, 3500f, 0.15f);
                return Finish(d.S, 0.36f);
            case "use_smog":
                d = new SoundDesign(1.0f, 0x5306u);
                for (int k = 0; k < 3; k++)
                    d.Noise(0.02f + k * 0.12f, 0.2f, Filter.Band, 300f, 900f, 0.4f, 1.2f, 0.3f, 1.4f, 20f, 0.6f);
                d.Noise(0.3f, 0.65f, Filter.Band, 500f, 1500f, 0.35f, 1f, 0.3f, 1.2f, 9f, 0.5f);
                d.Tone(0.3f, 0.6f, Wave.Saw, 100f, 70f, 0.09f, 0.3f, 1.4f, vibratoHz: 6f, vibratoDepth: 0.1f);
                d.Bubbles(0.45f, 0.5f, 8, 200f, 600f, 0.2f);
                return Finish(d.S, 0.38f);
            case "use_vine_whip":
                d = new SoundDesign(0.75f, 0x71E1u);
                d.Noise(0, 0.15f, Filter.Band, 800f, 2800f, 0.4f, 1.2f, 0.5f, 1.2f);
                d.Noise(0.17f, 0.05f, Filter.High, 3500f, 5500f, 0.9f, 1f, 0.01f, 2.4f);
                d.Thump(0.18f, 0.12f, 280f, 120f, 0.6f, 0.9f);
                d.Noise(0.26f, 0.3f, Filter.High, 3000f, 5500f, 0.4f, 1f, 0.05f, 1.4f, 30f, 0.8f);
                d.Crackle(0.3f, 0.3f, 10, 3200f, 0.3f);
                return Finish(d.S, 0.42f);
            case "use_dream_eater":
                d = new SoundDesign(1.0f, 0xD3EAu);
                d.Notes(0, 0.12f, 0.3f, Wave.Sine, Pitches(79, 83, 86, 83), 0.08f, 0.05f);
                d.Tone(0.2f, 0.6f, Wave.Sine, N(55), N(74), 0.2f, 0.3f, 1.3f, vibratoHz: 8f, vibratoDepth: 0.08f);
                d.Noise(0.2f, 0.55f, Filter.Band, 500f, 2400f, 0.25f, 1.4f, 0.4f, 1.3f, 14f, 0.5f);
                d.Thump(0.55f, 0.15f, 200f, 100f, 0.4f, 0.3f);
                d.Notes(0.7f, 0.08f, 0.2f, Wave.Sine, Pitches(88, 91, 95), 0.11f, 0.01f);
                return Finish(d.S, 0.36f);
            case "use_grass_whistle":
                d = new SoundDesign(1.0f, 0x6A55u);
                d.Tone(0, 0.2f, Wave.Sine, N(91), N(96), 0.2f, 0.05f, 1.4f, vibratoHz: 28f, vibratoDepth: 0.02f);
                d.Notes(0.2f, 0.1f, 0.22f, Wave.Sine, Pitches(88, 91, 95, 91, 88), 0.14f, 0.02f);
                d.Noise(0, 0.9f, Filter.High, 4500f, 6500f, 0.1f, 1f, 0.2f, 1.3f, 14f, 0.6f);
                d.Tone(0.6f, 0.35f, Wave.Triangle, N(76), N(67), 0.1f, 0.2f, 1.6f, vibratoHz: 5f, vibratoDepth: 0.03f);
                return Finish(d.S, 0.34f);
            case "use_reversal":
                d = new SoundDesign(1.0f, 0x2E7Au);
                foreach (float at in new[] { 0f, 0.15f })
                    d.Tone(at, 0.17f, Wave.Sine, N(67), N(79), 0.2f, 0.1f, 1.4f);
                d.Noise(0, 0.35f, Filter.Band, 600f, 2200f, 0.3f, 1.3f, 0.5f, 1.2f, 20f, 0.5f);
                d.Thump(0.4f, 0.14f, 230f, 100f, 0.7f, 0.9f);
                d.Noise(0.4f, 0.05f, Filter.Band, 1500f, 900f, 0.6f, 1f, 0.01f, 2.4f);
                d.Thump(0.58f, 0.3f, 180f, 45f, 1.0f, 0.9f);
                d.Noise(0.58f, 0.07f, Filter.Band, 1700f, 800f, 0.8f, 1f, 0.01f, 2.4f);
                d.Bell(0.6f, 0.38f, N(55), 1.41f, 1.2f, 0.12f, 3f);
                d.Noise(0.62f, 0.35f, Filter.Low, 1200f, 250f, 0.4f, 1f, 0.01f, 1.6f);
                return Finish(d.S, 0.44f);
        }
        return null;
    }
}
