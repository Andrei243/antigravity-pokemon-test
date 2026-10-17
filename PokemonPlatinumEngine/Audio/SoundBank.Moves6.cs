using static PokemonPlatinumEngine.Audio.SoundDesign;

namespace PokemonPlatinumEngine.Audio;

// Plan 25 Â· Y9, its first part (Y9a): the sounds of the moves the story shows from Lake Valor to Route 222. As the
// batches before: each move's beats are the original's (when it rings, how often, what it is made of, read from its
// animation script), its sound our own, built from the bank's shared layers.
public static partial class SoundBank
{
    /// <summary>A sound of Y9's first part, or null for a name that isn't one.</summary>
    private static float[]? MakeMoveSoundY9a(string name)
    {
        SoundDesign d;
        switch (name)
        {
            case "use_psych_up":
                d = new SoundDesign(0.9f, 0x9591u);
                d.Tone(0, 0.5f, Wave.Sine, N(79), N(91), 0.2f, 0.1f, 1.3f, vibratoHz: 9f, vibratoDepth: 0.03f);
                d.Notes(0.1f, 0.06f, 0.2f, Wave.Triangle, Pitches(84, 88, 91, 95), 0.12f, 0.01f);
                d.Bell(0.55f, 0.35f, N(95), 2.01f, 0.8f, 0.14f, 3f);
                return Finish(d.S, 0.34f);
            case "use_thunder":
                d = new SoundDesign(1.0f, 0x7D0Au);
                d.Noise(0, 0.3f, Filter.Low, 900f, 200f, 0.3f, 1f, 0.4f, 1.3f, 10f, 0.5f);
                d.Noise(0.28f, 0.1f, Filter.High, 2500f, 7000f, 0.9f, 1f, 0.01f, 2.6f);
                d.Crackle(0.3f, 0.5f, 16, 3000f, 0.5f);
                d.Thump(0.32f, 0.5f, 110f, 40f, 1.0f, 0.7f);
                d.Noise(0.35f, 0.6f, Filter.Low, 1400f, 200f, 0.4f, 1f, 0.02f, 1.5f);
                return Finish(d.S, 0.46f);
            case "use_aurora_beam":
                d = new SoundDesign(0.9f, 0xA0BEu);
                d.Tone(0, 0.7f, Wave.Sine, N(72), N(91), 0.2f, 0.1f, 1.3f, vibratoHz: 6f, vibratoDepth: 0.03f);
                d.Tone(0.1f, 0.6f, Wave.Triangle, N(79), N(98), 0.12f, 0.1f, 1.3f);
                d.Notes(0.2f, 0.07f, 0.2f, Wave.Sine, Pitches(88, 91, 95, 98), 0.08f, 0.01f);
                d.Noise(0.3f, 0.3f, Filter.High, 4500f, 7000f, 0.3f, 1f, 0.05f, 1.6f, 30f, 0.5f);
                return Finish(d.S, 0.36f);
            case "use_barrier":
                d = new SoundDesign(0.8f, 0xBA77u);
                foreach (float at in new[] { 0.02f, 0.1f, 0.18f, 0.26f, 0.34f })
                    d.Bell(at, 0.3f, N(84 + (int)(at * 20f)), 3.01f, 1.2f, 0.14f, 3f);
                d.Tone(0, 0.7f, Wave.Triangle, 300f, 300f, 0.06f, 0.2f, 1.5f);
                return Finish(d.S, 0.36f);
            case "use_double_edge":
                d = new SoundDesign(0.9f, 0xDE00u);
                d.Noise(0, 0.3f, Filter.Band, 500f, 2200f, 0.35f, 1.3f, 0.6f, 1.2f, 18f, 0.5f);
                d.Thump(0.32f, 0.3f, 190f, 50f, 1.0f, 0.9f);
                d.Noise(0.32f, 0.07f, Filter.Band, 1700f, 800f, 0.9f, 1f, 0.01f, 2.4f);
                d.Thump(0.5f, 0.2f, 240f, 90f, 0.5f, 0.6f);
                d.Tone(0.5f, 0.3f, Wave.Saw, 200f, 90f, 0.1f, 0.02f, 1.6f);
                return Finish(d.S, 0.44f);
            case "use_thrash":
                d = new SoundDesign(1.0f, 0x7455u);
                d.Tone(0, 0.7f, Wave.Saw, 90f, 130f, 0.12f, 0.3f, 1.3f, vibratoHz: 11f, vibratoDepth: 0.1f);
                foreach (float at in new[] { 0.15f, 0.38f, 0.6f })
                {
                    d.Noise(at, 0.08f, Filter.Band, 600f, 2800f, 0.5f, 1.2f, 0.3f, 1.2f);
                    d.Thump(at + 0.08f, 0.16f, 230f - at * 80f, 60f, 0.8f, 0.9f);
                }
                return Finish(d.S, 0.44f);
            case "use_trick_room":
                d = new SoundDesign(1.0f, 0x7210u);
                d.Tone(0, 0.8f, Wave.Sine, N(55), N(67), 0.2f, 0.3f, 1.3f, vibratoHz: 4f, vibratoDepth: 0.04f);
                d.Notes(0.1f, 0.12f, 0.25f, Wave.Triangle, Pitches(79, 74, 81, 76, 83), 0.1f, 0.02f);
                d.Noise(0.1f, 0.8f, Filter.Band, 1200f, 3000f, 0.15f, 1.5f, 0.4f, 1.3f, 8f, 0.5f);
                return Finish(d.S, 0.34f);
            case "use_brine":
                d = new SoundDesign(0.8f, 0xB21Eu);
                d.Noise(0, 0.4f, Filter.Band, 1000f, 3000f, 0.4f, 1.3f, 0.1f, 1.4f, 22f, 0.6f);
                d.Bubbles(0.05f, 0.3f, 7, 400f, 1200f, 0.3f);
                d.Noise(0.34f, 0.1f, Filter.High, 3500f, 5500f, 0.8f, 1f, 0.01f, 2.4f);
                d.Thump(0.35f, 0.2f, 250f, 100f, 0.7f, 0.6f);
                return Finish(d.S, 0.42f);
            case "use_mud_shot":
                d = new SoundDesign(0.8f, 0x30D5u);
                foreach (float at in new[] { 0.02f, 0.12f, 0.22f })
                {
                    d.Noise(at, 0.2f, Filter.Band, 300f, 900f, 0.35f, 1.2f, 0.1f, 1.4f);
                    d.Bubbles(at, 0.15f, 3, 200f, 500f, 0.3f);
                }
                d.Thump(0.36f, 0.2f, 180f, 70f, 0.7f, 0.4f);
                d.Noise(0.36f, 0.3f, Filter.Band, 400f, 1200f, 0.35f, 1.1f, 0.02f, 1.6f, 20f, 0.5f);
                return Finish(d.S, 0.42f);
            case "use_waterfall":
                d = new SoundDesign(1.0f, 0x3A7Fu);
                d.Noise(0, 0.5f, Filter.Band, 800f, 3000f, 0.4f, 1f, 0.1f, 1.2f, 40f, 0.9f);
                d.Tone(0.2f, 0.25f, Wave.Sine, 200f, 500f, 0.1f, 0.2f, 1.3f);
                d.Noise(0.45f, 0.1f, Filter.High, 3000f, 5500f, 0.8f, 1f, 0.01f, 2.4f);
                d.Thump(0.46f, 0.3f, 220f, 60f, 1.0f, 0.5f);
                d.Noise(0.5f, 0.45f, Filter.Band, 600f, 2400f, 0.4f, 1f, 0.01f, 1.6f, 26f, 0.6f);
                d.Bubbles(0.55f, 0.3f, 8, 300f, 900f, 0.3f);
                return Finish(d.S, 0.44f);
            case "use_bind":
                d = new SoundDesign(0.85f, 0xB1D0u);
                foreach (float at in new[] { 0.05f, 0.45f })
                {
                    d.Noise(at, 0.2f, Filter.Band, 400f, 1400f, 0.4f, 1.3f, 0.5f, 1.2f);
                    d.Tone(at + 0.1f, 0.25f, Wave.Saw, 220f, 130f, 0.1f, 0.05f, 1.5f, vibratoHz: 14f, vibratoDepth: 0.08f);
                    d.Thump(at + 0.22f, 0.12f, 170f, 80f, 0.5f, 0.4f);
                }
                return Finish(d.S, 0.4f);
            case "use_silver_wind":
                d = new SoundDesign(1.0f, 0x51A9u);
                d.Noise(0, 0.9f, Filter.Band, 1500f, 4500f, 0.3f, 0.9f, 0.3f, 1.2f, 6f, 0.6f);
                d.Notes(0.1f, 0.05f, 0.18f, Wave.Sine, Pitches(96, 100, 103, 107, 100, 103, 108, 112), 0.07f, 0.01f);
                d.Bell(0.55f, 0.4f, N(103), 2.76f, 1.0f, 0.1f, 3f);
                d.Tone(0, 0.8f, Wave.Triangle, 400f, 650f, 0.05f, 0.4f, 1.4f);
                return Finish(d.S, 0.34f);
            case "use_synthesis":
                d = new SoundDesign(1.0f, 0x5A57u);
                d.Tone(0, 0.9f, Wave.Sine, N(60), N(67), 0.2f, 0.3f, 1.3f, vibratoHz: 4f, vibratoDepth: 0.02f);
                d.Tone(0.1f, 0.8f, Wave.Triangle, N(72), N(79), 0.1f, 0.3f, 1.3f);
                d.Noise(0.1f, 0.6f, Filter.High, 4500f, 7000f, 0.1f, 1f, 0.3f, 1.3f, 12f, 0.6f);
                d.Notes(0.45f, 0.1f, 0.25f, Wave.Sine, Pitches(84, 88, 91), 0.1f, 0.02f);
                return Finish(d.S, 0.34f);
            case "use_thunder_punch":
                d = new SoundDesign(0.7f, 0x7C9Bu);
                d.Noise(0.02f, 0.1f, Filter.Band, 1500f, 900f, 0.7f, 1f, 0.01f, 2.4f);
                d.Thump(0.02f, 0.18f, 250f, 90f, 1.0f, 0.9f);
                d.Crackle(0.08f, 0.35f, 14, 3000f, 0.5f);
                d.Noise(0.1f, 0.12f, Filter.High, 2500f, 6500f, 0.7f, 1f, 0.01f, 2.4f);
                d.Tone(0.1f, 0.3f, Wave.Saw, 600f, 250f, 0.08f, 0.01f, 1.8f, vibratoHz: 40f, vibratoDepth: 0.2f);
                return Finish(d.S, 0.42f);
            case "use_helping_hand":
                d = new SoundDesign(0.95f, 0x4E1Du);
                foreach (float at in new[] { 0.15f, 0.45f })
                {
                    d.Noise(at, 0.06f, Filter.Band, 700f, 1500f, 0.4f, 1f, 0.01f, 2f);
                    d.Thump(at + 0.02f, 0.1f, 200f, 120f, 0.3f, 0.2f);
                    d.Bell(at + 0.05f, 0.3f, N(at < 0.3f ? 88 : 95), 2.01f, 0.9f, 0.14f, 3f);
                }
                d.Notes(0.6f, 0.07f, 0.2f, Wave.Sine, Pitches(88, 91, 95, 100), 0.08f, 0.01f);
                return Finish(d.S, 0.34f);
            case "use_bounce":
                d = new SoundDesign(1.0f, 0xB0CEu);
                d.Tone(0.04f, 0.2f, Wave.Sine, 200f, 700f, 0.2f, 0.01f, 1.8f);
                d.Noise(0.04f, 0.3f, Filter.Band, 600f, 2400f, 0.3f, 1f, 0.1f, 1.4f);
                d.Noise(0.5f, 0.25f, Filter.Band, 2400f, 600f, 0.3f, 1f, 0.2f, 1.3f);
                d.Tone(0.55f, 0.2f, Wave.Sine, 700f, 200f, 0.15f, 0.02f, 1.6f);
                d.Thump(0.74f, 0.3f, 170f, 45f, 1.0f, 0.9f);
                d.Noise(0.74f, 0.07f, Filter.Band, 1700f, 800f, 0.8f, 1f, 0.01f, 2.4f);
                return Finish(d.S, 0.44f);
            case "use_egg_bomb":
                d = new SoundDesign(0.9f, 0xE99Bu);
                d.Noise(0, 0.3f, Filter.Band, 500f, 1800f, 0.25f, 1.2f, 0.4f, 1.2f);
                d.Tone(0.02f, 0.3f, Wave.Sine, 300f, 600f, 0.1f, 0.05f, 1.6f);
                d.Noise(0.32f, 0.08f, Filter.High, 3000f, 6000f, 0.7f, 1f, 0.01f, 2.4f);
                d.Thump(0.33f, 0.4f, 150f, 40f, 1.0f, 0.8f);
                d.Crackle(0.35f, 0.3f, 8, 1000f, 0.4f);
                d.Noise(0.36f, 0.5f, Filter.Low, 1400f, 250f, 0.45f, 1f, 0.01f, 1.6f);
                return Finish(d.S, 0.46f);
            case "use_fling":
                d = new SoundDesign(0.7f, 0xF117u);
                d.Noise(0, 0.3f, Filter.High, 2000f, 5500f, 0.3f, 1f, 0.2f, 1.2f, 30f, 0.7f);
                d.Tone(0, 0.3f, Wave.Sine, 900f, 450f, 0.1f, 0.02f, 1.4f);
                d.Thump(0.32f, 0.18f, 260f, 100f, 0.9f, 0.9f);
                d.Noise(0.32f, 0.06f, Filter.Band, 1800f, 1000f, 0.7f, 1f, 0.01f, 2.4f);
                d.Bell(0.34f, 0.25f, N(88), 1.41f, 0.9f, 0.08f, 3f);
                return Finish(d.S, 0.42f);
            case "use_meteor_mash":
                d = new SoundDesign(1.0f, 0x3E70u);
                d.Noise(0, 0.25f, Filter.Band, 1500f, 400f, 0.35f, 1.2f, 0.5f, 1.2f);
                d.Tone(0, 0.25f, Wave.Sine, 1100f, 220f, 0.14f, 0.02f, 1.5f);
                d.Thump(0.25f, 0.3f, 140f, 40f, 1.0f, 0.8f);
                d.Crackle(0.27f, 0.25f, 8, 900f, 0.4f);
                d.Noise(0.5f, 0.07f, Filter.Band, 1600f, 900f, 0.8f, 1f, 0.01f, 2.4f);
                d.Thump(0.5f, 0.3f, 190f, 55f, 1.0f, 0.9f);
                d.Bell(0.52f, 0.4f, N(60), 1.41f, 1.2f, 0.12f, 3f);
                return Finish(d.S, 0.46f);
            case "use_petal_dance":
                d = new SoundDesign(1.0f, 0x9E7Au);
                d.Noise(0, 0.8f, Filter.Band, 1500f, 4000f, 0.25f, 0.9f, 0.1f, 1.2f, 9f, 0.7f);
                d.Notes(0, 0.1f, 0.25f, Wave.Sine, Pitches(84, 88, 91, 88, 84, 88, 91, 96), 0.1f, 0.02f);
                foreach (float at in new[] { 0.3f, 0.52f, 0.74f })
                {
                    d.Noise(at, 0.1f, Filter.High, 3000f, 5500f, 0.4f, 1f, 0.02f, 2f);
                    d.Thump(at + 0.04f, 0.12f, 220f, 110f, 0.4f, 0.4f);
                }
                return Finish(d.S, 0.36f);
            case "use_wish":
                d = new SoundDesign(1.0f, 0x3158u);
                d.Tone(0, 0.5f, Wave.Sine, N(100), N(79), 0.15f, 0.02f, 1.4f, vibratoHz: 6f, vibratoDepth: 0.01f);
                d.Noise(0, 0.4f, Filter.High, 5000f, 8000f, 0.12f, 1f, 0.1f, 1.3f);
                d.Bell(0.35f, 0.5f, N(91), 2.01f, 0.8f, 0.2f, 2.6f);
                d.Notes(0.5f, 0.08f, 0.25f, Wave.Sine, Pitches(88, 91, 95, 100, 103), 0.08f, 0.02f);
                d.Tone(0.35f, 0.6f, Wave.Triangle, N(55), N(60), 0.1f, 0.3f, 1.4f);
                return Finish(d.S, 0.34f);
            case "use_u_turn":
                d = new SoundDesign(1.0f, 0x07A2u);
                d.Noise(0, 0.25f, Filter.Band, 500f, 2400f, 0.35f, 1.2f, 0.4f, 1.2f, 20f, 0.5f);
                d.Thump(0.28f, 0.14f, 230f, 100f, 0.8f, 0.9f);
                d.Noise(0.28f, 0.06f, Filter.Band, 1500f, 900f, 0.7f, 1f, 0.01f, 2.4f);
                d.Noise(0.45f, 0.35f, Filter.Band, 2400f, 500f, 0.35f, 1.2f, 0.3f, 1.2f, 16f, 0.5f);
                d.Tone(0.5f, 0.3f, Wave.Sine, 900f, 300f, 0.08f, 0.02f, 1.6f);
                d.Thump(0.85f, 0.1f, 200f, 120f, 0.25f, 0.2f);
                return Finish(d.S, 0.42f);
            case "use_flash_cannon":
                d = new SoundDesign(1.0f, 0xFCA0u);
                d.Tone(0, 0.35f, Wave.Saw, 120f, 480f, 0.12f, 0.3f, 1.3f, vibratoHz: 16f, vibratoDepth: 0.05f);
                d.Noise(0, 0.35f, Filter.High, 3000f, 7000f, 0.12f, 1f, 0.6f, 1.2f);
                d.Noise(0.3f, 0.12f, Filter.High, 3000f, 7000f, 0.7f, 1f, 0.01f, 2f);
                d.Tone(0.3f, 0.5f, Wave.Triangle, 900f, 500f, 0.14f, 0.02f, 1.6f);
                d.Thump(0.4f, 0.3f, 170f, 50f, 0.9f, 0.8f);
                d.Bell(0.42f, 0.4f, N(96), 2.76f, 1.0f, 0.12f, 3f);
                return Finish(d.S, 0.44f);
            case "use_iron_tail":
                d = new SoundDesign(0.85f, 0x1A07u);
                d.Bell(0, 0.4f, N(88), 3.7f, 1.4f, 0.18f, 2.6f);
                d.Noise(0, 0.3f, Filter.Band, 2000f, 5500f, 0.25f, 2.5f, 0.3f, 1.4f);
                d.Noise(0.3f, 0.2f, Filter.Band, 600f, 2400f, 0.35f, 1.2f, 0.4f, 1.2f);
                d.Thump(0.5f, 0.25f, 200f, 55f, 1.0f, 0.9f);
                d.Bell(0.5f, 0.4f, N(60), 1.41f, 1.4f, 0.16f, 3f);
                d.Noise(0.5f, 0.06f, Filter.High, 3500f, 2500f, 0.8f, 1f, 0.01f, 2.4f);
                return Finish(d.S, 0.44f);
            case "use_metal_burst":
                d = new SoundDesign(1.0f, 0x3E7Bu);
                d.Tone(0, 0.4f, Wave.Saw, 180f, 700f, 0.1f, 0.3f, 1.3f, vibratoHz: 18f, vibratoDepth: 0.05f);
                d.Bell(0.1f, 0.3f, N(84), 3.7f, 1.4f, 0.14f, 2.6f);
                d.Bell(0.22f, 0.3f, N(91), 3.01f, 1.2f, 0.14f, 3f);
                d.Noise(0.42f, 0.2f, Filter.Band, 2000f, 600f, 0.4f, 1.5f, 0.05f, 1.4f);
                d.Thump(0.55f, 0.28f, 190f, 45f, 1.0f, 0.9f);
                d.Crackle(0.56f, 0.3f, 10, 2400f, 0.5f);
                return Finish(d.S, 0.44f);
            case "use_skill_swap":
                d = new SoundDesign(0.95f, 0x5C11u);
                foreach (float at in new[] { 0.05f, 0.2f, 0.35f, 0.5f, 0.65f, 0.8f })
                    d.Bell(at, 0.22f, N(at < 0.4f ? 88 : 95), 2.01f, 0.9f, 0.14f, 3f);
                d.Tone(0, 0.8f, Wave.Triangle, N(60), N(72), 0.06f, 0.3f, 1.6f);
                return Finish(d.S, 0.34f);
            case "use_stone_edge":
                d = new SoundDesign(1.0f, 0x57E9u);
                for (int k = 0; k < 6; k++)
                {
                    d.Thump(0.04f + k * 0.07f, 0.1f, 160f + k * 8f, 70f, 0.5f, 0.6f);
                    d.Crackle(0.04f + k * 0.07f, 0.1f, 3, 1200f, 0.4f);
                }
                d.Noise(0.45f, 0.1f, Filter.High, 3000f, 5500f, 0.6f, 1f, 0.01f, 2.4f);
                d.Thump(0.5f, 0.4f, 150f, 40f, 1.0f, 0.8f);
                d.Crackle(0.52f, 0.4f, 12, 800f, 0.5f);
                return Finish(d.S, 0.46f);
            case "use_dragon_breath":
                d = new SoundDesign(1.0f, 0xD7BEu);
                d.Tone(0, 0.4f, Wave.Saw, 100f, 180f, 0.14f, 0.2f, 1.3f, vibratoHz: 10f, vibratoDepth: 0.08f);
                foreach (float at in new[] { 0.05f, 0.4f })
                {
                    d.Noise(at, 0.4f, Filter.Band, 500f, 2400f, 0.4f, 1.1f, 0.2f, 1.2f, 24f, 0.6f);
                    d.Crackle(at + 0.1f, 0.3f, 10, 1000f, 0.3f);
                }
                d.Thump(0.35f, 0.15f, 200f, 90f, 0.4f, 0.3f);
                return Finish(d.S, 0.42f);
            case "use_close_combat":
                d = new SoundDesign(1.0f, 0xCC0Au);
                for (int k = 0; k < 8; k++)
                {
                    d.Noise(0.03f + k * 0.1f, 0.05f, Filter.Band, 1500f, 800f, 0.6f, 1f, 0.01f, 2.4f);
                    d.Thump(0.03f + k * 0.1f, 0.1f, 260f - k * 12f, 90f, 0.7f, 0.9f);
                }
                d.Thump(0.85f, 0.15f, 180f, 50f, 1.0f, 0.9f);
                return Finish(d.S, 0.44f);
            case "use_dizzy_punch":
                d = new SoundDesign(0.95f, 0xD177u);
                foreach (float at in new[] { 0.1f, 0.4f })
                {
                    d.Noise(at, 0.1f, Filter.Band, 800f, 2600f, 0.5f, 1.2f, 0.3f, 1.2f);
                    d.Thump(at + 0.1f, 0.16f, 240f, 100f, 0.9f, 0.9f);
                }
                d.Notes(0.55f, 0.07f, 0.18f, Wave.Triangle, Pitches(88, 84, 88, 84, 88, 84), 0.1f, 0.01f);
                d.Tone(0.5f, 0.4f, Wave.Sine, 600f, 400f, 0.05f, 0.1f, 1.5f, vibratoHz: 12f, vibratoDepth: 0.2f);
                return Finish(d.S, 0.42f);
            case "use_high_jump_kick":
                d = new SoundDesign(1.0f, 0x417Au);
                d.Noise(0, 0.2f, Filter.Band, 400f, 1600f, 0.3f, 1.2f, 0.2f, 1.3f);
                d.Tone(0.05f, 0.3f, Wave.Sine, 250f, 900f, 0.14f, 0.02f, 1.6f);
                d.Noise(0.45f, 0.25f, Filter.Band, 2800f, 500f, 0.35f, 1.2f, 0.3f, 1.2f, 14f, 0.5f);
                d.Thump(0.72f, 0.3f, 190f, 45f, 1.0f, 0.9f);
                d.Noise(0.72f, 0.07f, Filter.Band, 1700f, 800f, 0.9f, 1f, 0.01f, 2.4f);
                d.Crackle(0.74f, 0.2f, 6, 1200f, 0.3f);
                return Finish(d.S, 0.44f);
            case "use_cross_chop":
                d = new SoundDesign(1.0f, 0xC805u);
                d.Noise(0.12f, 0.12f, Filter.High, 3000f, 6000f, 0.5f, 1.2f, 0.05f, 1.6f, 36f, 0.5f);
                d.Thump(0.2f, 0.1f, 200f, 90f, 0.4f, 0.5f);
                d.Noise(0.5f, 0.12f, Filter.High, 3000f, 6000f, 0.5f, 1.2f, 0.05f, 1.6f, 36f, 0.5f);
                d.Thump(0.58f, 0.1f, 200f, 90f, 0.4f, 0.5f);
                d.Noise(0.8f, 0.14f, Filter.High, 3500f, 6500f, 0.8f, 1.2f, 0.01f, 2.2f);
                d.Thump(0.82f, 0.2f, 220f, 70f, 1.0f, 0.9f);
                d.Bell(0.84f, 0.2f, N(60), 1.41f, 1.2f, 0.1f, 3f);
                return Finish(d.S, 0.44f);
            case "use_ice_shard":
                d = new SoundDesign(1.0f, 0x1C54u);
                d.Noise(0, 0.3f, Filter.High, 3500f, 7000f, 0.15f, 1f, 0.5f, 1.2f, 20f, 0.5f);
                d.Notes(0.1f, 0.05f, 0.18f, Wave.Sine, Pitches(98, 102, 105, 109), 0.1f, 0.01f);
                d.Bell(0.3f, 0.3f, N(100), 2.76f, 1.0f, 0.12f, 3f);
                d.Noise(0.52f, 0.15f, Filter.High, 3000f, 7500f, 0.7f, 1f, 0.01f, 2.4f);
                d.Thump(0.54f, 0.14f, 280f, 120f, 0.8f, 0.9f);
                d.Crackle(0.55f, 0.3f, 10, 4500f, 0.4f);
                return Finish(d.S, 0.4f);
            case "use_swords_dance":
                d = new SoundDesign(1.0f, 0x5D0Cu);
                for (int k = 0; k < 4; k++)
                    d.Noise(0.05f + k * 0.15f, 0.12f, Filter.High, 3500f, 6500f, 0.4f, 1.2f, 0.1f, 1.8f);
                d.Tone(0.05f, 0.6f, Wave.Triangle, N(60), N(72), 0.1f, 0.2f, 1.4f, vibratoHz: 6f, vibratoDepth: 0.03f);
                d.Bell(0.68f, 0.35f, N(91), 3.01f, 1.2f, 0.16f, 3f);
                d.Notes(0.7f, 0.06f, 0.2f, Wave.Sine, Pitches(88, 91, 95, 100), 0.08f, 0.01f);
                return Finish(d.S, 0.36f);
            case "use_wood_hammer":
                d = new SoundDesign(0.9f, 0x300Du);
                d.Noise(0, 0.3f, Filter.Band, 400f, 1800f, 0.3f, 1.2f, 0.6f, 1.2f);
                d.Thump(0.3f, 0.3f, 150f, 55f, 1.0f, 0.9f);
                d.Crackle(0.3f, 0.25f, 8, 700f, 0.5f);
                d.Noise(0.31f, 0.07f, Filter.Band, 1200f, 700f, 0.8f, 1f, 0.01f, 2.4f);
                d.Noise(0.4f, 0.35f, Filter.High, 2500f, 5500f, 0.2f, 1f, 0.1f, 1.4f, 30f, 0.7f);
                d.Thump(0.55f, 0.15f, 200f, 100f, 0.3f, 0.3f);
                return Finish(d.S, 0.44f);
            case "use_heal_bell":
                d = new SoundDesign(1.0f, 0x4EA1u);
                foreach (float at in new[] { 0.02f, 0.3f, 0.58f })
                {
                    d.Bell(at, 0.55f, N(84), 2.01f, 0.9f, 0.2f, 2.4f);
                    d.Bell(at + 0.01f, 0.4f, N(91), 3.01f, 0.6f, 0.1f, 3f);
                }
                d.Notes(0.25f, 0.1f, 0.25f, Wave.Sine, Pitches(96, 100, 103), 0.05f, 0.02f);
                return Finish(d.S, 0.34f);
            case "use_psywave":
                d = new SoundDesign(1.0f, 0x95A4u);
                foreach (float at in new[] { 0.02f, 0.2f })
                    d.Tone(at, 0.4f, Wave.Sine, N(67), N(79), 0.16f, 0.1f, 1.4f, vibratoHz: 9f, vibratoDepth: 0.06f);
                d.Tone(0.38f, 0.3f, Wave.Triangle, N(72), N(84), 0.1f, 0.1f, 1.5f, vibratoHz: 9f, vibratoDepth: 0.06f);
                d.Thump(0.48f, 0.2f, 200f, 90f, 0.4f, 0.4f);
                d.Notes(0.5f, 0.06f, 0.2f, Wave.Sine, Pitches(91, 95, 98), 0.08f, 0.01f);
                return Finish(d.S, 0.36f);
            case "use_fire_blast":
                d = new SoundDesign(1.0f, 0xF1B1u);
                d.Noise(0, 0.5f, Filter.Band, 400f, 1800f, 0.3f, 1.1f, 0.5f, 1.2f, 22f, 0.6f);
                d.Tone(0, 0.5f, Wave.Saw, 90f, 160f, 0.1f, 0.4f, 1.3f, vibratoHz: 12f, vibratoDepth: 0.08f);
                d.Noise(0.3f, 0.1f, Filter.High, 2500f, 6500f, 0.8f, 1f, 0.01f, 2.4f);
                d.Thump(0.35f, 0.45f, 140f, 40f, 1.0f, 0.8f);
                d.Crackle(0.36f, 0.4f, 14, 1400f, 0.5f);
                d.Noise(0.38f, 0.55f, Filter.Band, 600f, 2400f, 0.35f, 1f, 0.02f, 1.5f, 24f, 0.6f);
                return Finish(d.S, 0.46f);
            case "use_flamethrower":
                d = new SoundDesign(1.0f, 0xF1A3u);
                d.Noise(0, 0.95f, Filter.Band, 500f, 2600f, 0.4f, 1.1f, 0.1f, 1.2f, 26f, 0.7f);
                d.Tone(0, 0.9f, Wave.Saw, 100f, 130f, 0.08f, 0.2f, 1.3f, vibratoHz: 14f, vibratoDepth: 0.1f);
                d.Crackle(0.3f, 0.6f, 20, 1300f, 0.3f);
                d.Thump(0.35f, 0.15f, 170f, 80f, 0.3f, 0.2f);
                return Finish(d.S, 0.4f);
            case "use_punishment":
                d = new SoundDesign(0.9f, 0x9091u);
                d.Tone(0, 0.2f, Wave.Saw, 160f, 110f, 0.12f, 0.02f, 1.5f);
                d.Noise(0.1f, 0.06f, Filter.High, 4000f, 7000f, 0.9f, 1f, 0.01f, 2.6f);
                d.Thump(0.12f, 0.14f, 220f, 90f, 0.8f, 0.9f);
                d.Noise(0.4f, 0.06f, Filter.High, 4000f, 7000f, 0.9f, 1f, 0.01f, 2.6f);
                d.Thump(0.42f, 0.2f, 190f, 60f, 1.0f, 0.9f);
                d.Notes(0.6f, 0.1f, 0.15f, Wave.Pulse, Pitches(62, 62, 62), 0.07f, 0.005f);
                return Finish(d.S, 0.42f);
            case "use_conversion_2":
                d = new SoundDesign(1.0f, 0xC0A2u);
                d.Tone(0, 0.45f, Wave.Sine, N(84), N(96), 0.14f, 0.1f, 1.4f, vibratoHz: 20f, vibratoDepth: 0.02f);
                d.Notes(0.45f, 0.1f, 0.2f, Wave.Triangle, Pitches(72, 76, 79, 84, 88), 0.12f, 0.01f);
                d.Bell(0.9f, 0.1f, N(96), 2.01f, 0.6f, 0.12f, 4f);
                d.Noise(0.05f, 0.4f, Filter.High, 5000f, 7500f, 0.08f, 1f, 0.2f, 1.4f);
                return Finish(d.S, 0.34f);
            case "use_icy_wind":
                d = new SoundDesign(1.0f, 0x1C71u);
                d.Noise(0, 0.95f, Filter.Band, 1200f, 4200f, 0.35f, 0.9f, 0.2f, 1.2f, 10f, 0.6f);
                d.Noise(0.2f, 0.75f, Filter.High, 4500f, 7000f, 0.12f, 1f, 0.3f, 1.3f, 20f, 0.6f);
                d.Notes(0.35f, 0.08f, 0.2f, Wave.Sine, Pitches(100, 103, 107, 103), 0.05f, 0.01f);
                d.Crackle(0.5f, 0.4f, 8, 4500f, 0.3f);
                return Finish(d.S, 0.34f);
            case "use_shadow_punch":
                d = new SoundDesign(0.85f, 0x5A90u);
                d.Tone(0, 0.45f, Wave.Saw, 140f, 70f, 0.12f, 0.1f, 1.5f, vibratoHz: 7f, vibratoDepth: 0.1f);
                d.Noise(0.05f, 0.3f, Filter.Band, 500f, 1500f, 0.3f, 1.2f, 0.5f, 1.2f, 14f, 0.5f);
                d.Thump(0.36f, 0.2f, 160f, 55f, 1.0f, 0.7f);
                d.Tone(0.38f, 0.4f, Wave.Sine, N(48), N(43), 0.14f, 0.05f, 1.8f, vibratoHz: 5f, vibratoDepth: 0.05f);
                return Finish(d.S, 0.42f);
            case "use_hail":
                d = new SoundDesign(1.0f, 0x4A11u);
                d.Noise(0, 0.95f, Filter.Band, 1500f, 5000f, 0.2f, 0.9f, 0.2f, 1.2f, 12f, 0.7f);
                for (int k = 0; k < 14; k++)
                    d.Thump(0.1f + k * 0.06f + (k % 3) * 0.01f, 0.05f, 700f + (k % 4) * 150f, 300f, 0.22f, 0.8f);
                d.Crackle(0.2f, 0.7f, 12, 5000f, 0.2f);
                return Finish(d.S, 0.36f);
            case "use_rock_climb":
                d = new SoundDesign(1.0f, 0x2C11u);
                for (int k = 0; k < 4; k++)
                {
                    d.Thump(0.04f + k * 0.1f, 0.1f, 140f, 70f, 0.4f, 0.4f);
                    d.Crackle(0.04f + k * 0.1f, 0.1f, 3, 900f, 0.3f);
                }
                d.Noise(0.45f, 0.25f, Filter.Band, 500f, 2200f, 0.35f, 1.2f, 0.4f, 1.2f, 18f, 0.5f);
                d.Thump(0.7f, 0.3f, 180f, 45f, 1.0f, 0.9f);
                d.Crackle(0.71f, 0.3f, 10, 800f, 0.5f);
                return Finish(d.S, 0.44f);
            case "use_avalanche":
                d = new SoundDesign(1.0f, 0xA7A1u);
                d.Noise(0, 0.95f, Filter.Low, 1800f, 300f, 0.45f, 1f, 0.15f, 1.2f, 14f, 0.8f);
                for (int k = 0; k < 8; k++)
                    d.Thump(0.15f + k * 0.07f, 0.12f, 170f, 60f, 0.5f, 0.5f);
                d.Crackle(0.2f, 0.7f, 20, 1200f, 0.4f);
                d.Thump(0.7f, 0.3f, 130f, 35f, 1.0f, 0.7f);
                return Finish(d.S, 0.46f);
            case "use_blizzard":
                d = new SoundDesign(1.0f, 0xB112u);
                d.Noise(0, 0.95f, Filter.Band, 800f, 4800f, 0.45f, 0.9f, 0.25f, 1.1f, 8f, 0.8f);
                d.Noise(0.1f, 0.8f, Filter.High, 4500f, 7500f, 0.18f, 1f, 0.3f, 1.2f, 22f, 0.7f);
                d.Tone(0, 0.9f, Wave.Sine, 180f, 260f, 0.08f, 0.4f, 1.3f, vibratoHz: 5f, vibratoDepth: 0.1f);
                d.Thump(0.45f, 0.25f, 150f, 55f, 0.7f, 0.5f);
                d.Crackle(0.45f, 0.5f, 14, 4500f, 0.4f);
                d.Notes(0.55f, 0.07f, 0.2f, Wave.Sine, Pitches(103, 107, 110), 0.06f, 0.01f);
                return Finish(d.S, 0.4f);
            case "use_focus_blast":
                d = new SoundDesign(1.0f, 0xFB1Au);
                d.Tone(0, 0.45f, Wave.Saw, 80f, 300f, 0.14f, 0.3f, 1.2f, vibratoHz: 14f, vibratoDepth: 0.06f);
                d.Noise(0, 0.4f, Filter.Band, 300f, 1800f, 0.2f, 1.2f, 0.7f, 1.2f);
                d.Noise(0.42f, 0.2f, Filter.Band, 1800f, 400f, 0.3f, 1.2f, 0.05f, 1.4f);
                d.Thump(0.6f, 0.4f, 140f, 35f, 1.0f, 0.8f);
                d.Crackle(0.61f, 0.3f, 12, 1400f, 0.5f);
                d.Noise(0.62f, 0.35f, Filter.Low, 1600f, 250f, 0.5f, 1f, 0.01f, 1.6f);
                return Finish(d.S, 0.46f);
            case "use_discharge":
                d = new SoundDesign(1.0f, 0xD15Cu);
                d.Tone(0, 0.3f, Wave.Saw, 200f, 900f, 0.1f, 0.4f, 1.3f, vibratoHz: 30f, vibratoDepth: 0.1f);
                d.Crackle(0.25f, 0.6f, 24, 3200f, 0.5f);
                d.Noise(0.28f, 0.12f, Filter.High, 2500f, 7000f, 0.8f, 1f, 0.01f, 2.4f);
                d.Tone(0.3f, 0.5f, Wave.Saw, 700f, 300f, 0.08f, 0.01f, 1.8f, vibratoHz: 50f, vibratoDepth: 0.2f);
                d.Thump(0.32f, 0.25f, 130f, 45f, 0.8f, 0.6f);
                return Finish(d.S, 0.44f);
            case "use_dragon_dance":
                d = new SoundDesign(1.0f, 0xDD4Eu);
                foreach (float at in new[] { 0.05f, 0.35f, 0.65f })
                {
                    d.Thump(at, 0.2f, 130f, 55f, 0.8f, 0.5f);
                    d.Bell(at + 0.04f, 0.4f, N(55), 1.41f, 1.0f, 0.14f, 2.6f);
                }
                d.Tone(0, 0.95f, Wave.Saw, 100f, 150f, 0.1f, 0.3f, 1.3f, vibratoHz: 6f, vibratoDepth: 0.08f);
                d.Notes(0.1f, 0.15f, 0.3f, Wave.Triangle, Pitches(67, 70, 74, 79, 74), 0.1f, 0.03f);
                return Finish(d.S, 0.38f);
            case "use_hydro_pump":
                d = new SoundDesign(1.0f, 0x4D90u);
                d.Noise(0, 0.8f, Filter.Band, 400f, 2800f, 0.5f, 1.1f, 0.15f, 1.2f, 40f, 0.9f);
                d.Tone(0, 0.7f, Wave.Sine, 120f, 220f, 0.12f, 0.2f, 1.3f);
                d.Bubbles(0.1f, 0.6f, 14, 250f, 900f, 0.3f);
                d.Noise(0.4f, 0.1f, Filter.High, 3000f, 5500f, 0.8f, 1f, 0.01f, 2.4f);
                d.Thump(0.42f, 0.35f, 180f, 50f, 1.0f, 0.6f);
                d.Noise(0.45f, 0.5f, Filter.Band, 700f, 2600f, 0.4f, 1f, 0.01f, 1.6f, 24f, 0.6f);
                return Finish(d.S, 0.46f);
            case "use_muddy_water":
                d = new SoundDesign(1.0f, 0x30DDu);
                d.Noise(0, 0.9f, Filter.Band, 250f, 1400f, 0.5f, 1.4f, 0.4f, 1.1f, 8f, 0.7f);
                d.Tone(0, 0.8f, Wave.Sine, 90f, 170f, 0.14f, 0.4f, 1.3f, vibratoHz: 5f, vibratoDepth: 0.1f);
                d.Bubbles(0.2f, 0.7f, 12, 150f, 500f, 0.35f);
                d.Thump(0.55f, 0.35f, 150f, 45f, 0.9f, 0.4f);
                d.Noise(0.55f, 0.4f, Filter.Band, 300f, 1200f, 0.4f, 1f, 0.02f, 1.6f, 20f, 0.6f);
                return Finish(d.S, 0.44f);
            case "use_thunder_fang":
                d = new SoundDesign(0.8f, 0x7FA9u);
                d.Noise(0.05f, 0.08f, Filter.High, 2500f, 4500f, 0.5f, 1f, 0.2f, 1.4f);
                d.Thump(0.14f, 0.2f, 270f, 90f, 1.0f, 0.9f);
                d.Noise(0.14f, 0.06f, Filter.High, 4200f, 3000f, 0.9f, 1f, 0.01f, 2.4f);
                d.Crackle(0.16f, 0.45f, 16, 3200f, 0.5f);
                d.Tone(0.16f, 0.35f, Wave.Saw, 650f, 260f, 0.08f, 0.01f, 1.8f, vibratoHz: 45f, vibratoDepth: 0.2f);
                return Finish(d.S, 0.42f);
            case "use_zen_headbutt":
                d = new SoundDesign(1.0f, 0x2E4Bu);
                d.Tone(0, 0.5f, Wave.Sine, N(72), N(84), 0.16f, 0.2f, 1.3f, vibratoHz: 6f, vibratoDepth: 0.03f);
                d.Bell(0.15f, 0.3f, N(91), 2.01f, 0.8f, 0.1f, 3f);
                d.Noise(0.4f, 0.2f, Filter.Band, 600f, 2400f, 0.3f, 1.2f, 0.5f, 1.2f);
                d.Thump(0.58f, 0.25f, 200f, 55f, 1.0f, 0.9f);
                d.Bell(0.6f, 0.4f, N(79), 1.41f, 1.0f, 0.14f, 2.8f);
                d.Notes(0.62f, 0.07f, 0.2f, Wave.Sine, Pitches(88, 84, 79), 0.07f, 0.01f);
                return Finish(d.S, 0.42f);
        }
        return null;
    }
}
