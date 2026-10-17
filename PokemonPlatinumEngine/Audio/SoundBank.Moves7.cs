using static PokemonPlatinumEngine.Audio.SoundDesign;

namespace PokemonPlatinumEngine.Audio;

// Plan 25 - Y9, its second part (Y9b): the sounds of the moves the story shows from the Sunyshore Gym to Route 224. As
// the batches before: each move's beats are the original's (when it rings, how often, what it is made of, read from
// its animation script), its sound our own, built from the bank's shared layers.
public static partial class SoundBank
{
    /// <summary>A sound of Y9's second part, or null for a name that isn't one.</summary>
    private static float[]? MakeMoveSoundY9b(string name)
    {
        SoundDesign d;
        switch (name)
        {
            case "use_super_fang":
                d = new SoundDesign(0.8f, 0x5FA9u);
                d.Noise(0.05f, 0.08f, Filter.High, 2500f, 4500f, 0.5f, 1f, 0.2f, 1.4f);
                d.Thump(0.14f, 0.22f, 260f, 80f, 1.0f, 0.9f);
                d.Noise(0.14f, 0.06f, Filter.High, 4000f, 2500f, 0.9f, 1f, 0.01f, 2.4f);
                d.Thump(0.26f, 0.2f, 200f, 70f, 0.6f, 0.6f);
                return Finish(d.S, 0.42f);
            case "use_mirror_shot":
                d = new SoundDesign(0.9f, 0x3125u);
                d.Notes(0, 0.04f, 0.16f, Wave.Sine, Pitches(96, 100, 103, 108), 0.14f, 0.005f);
                d.Tone(0.1f, 0.4f, Wave.Triangle, N(84), N(100), 0.12f, 0.1f, 1.3f);
                d.Noise(0.3f, 0.1f, Filter.High, 3000f, 7000f, 0.8f, 1f, 0.01f, 2.4f);
                d.Bell(0.32f, 0.5f, N(96), 3.01f, 1.0f, 0.2f, 3f);
                d.Thump(0.33f, 0.2f, 200f, 70f, 0.6f, 0.5f);
                return Finish(d.S, 0.38f);
            case "use_charge_beam":
                d = new SoundDesign(1.0f, 0xC4B3u);
                d.Tone(0, 0.25f, Wave.Saw, 200f, 900f, 0.1f, 0.4f, 1.3f, vibratoHz: 30f, vibratoDepth: 0.1f);
                d.Crackle(0.05f, 0.25f, 10, 3000f, 0.35f);
                d.Tone(0.2f, 0.7f, Wave.Saw, 650f, 520f, 0.1f, 0.03f, 1.4f, vibratoHz: 55f, vibratoDepth: 0.2f);
                d.Noise(0.2f, 0.7f, Filter.Band, 1500f, 2600f, 0.3f, 1.4f, 0.03f, 1.3f, 40f, 0.6f);
                d.Thump(0.35f, 0.2f, 170f, 60f, 0.7f, 0.5f);
                return Finish(d.S, 0.4f);
            case "use_giga_impact":
                d = new SoundDesign(1.0f, 0x6160u);
                d.Tone(0, 0.3f, Wave.Saw, 100f, 260f, 0.12f, 0.3f, 1.3f);
                d.Noise(0.1f, 0.25f, Filter.Band, 500f, 3000f, 0.4f, 1.3f, 0.5f, 1.2f, 18f, 0.5f);
                d.Thump(0.33f, 0.4f, 180f, 38f, 1.0f, 0.9f);
                d.Noise(0.33f, 0.07f, Filter.Band, 1700f, 800f, 0.9f, 1f, 0.01f, 2.4f);
                d.Noise(0.38f, 0.5f, Filter.Low, 1200f, 200f, 0.4f, 1f, 0.01f, 1.5f);
                d.Thump(0.55f, 0.3f, 240f, 70f, 0.6f, 0.5f);
                return Finish(d.S, 0.46f);
            case "use_octazooka":
                d = new SoundDesign(0.9f, 0x0C7Au);
                foreach (float at in new[] { 0.03f, 0.1f, 0.17f })
                {
                    d.Noise(at, 0.12f, Filter.Band, 400f, 1500f, 0.35f, 1.2f, 0.1f, 1.4f);
                    d.Bubbles(at, 0.1f, 2, 200f, 600f, 0.3f);
                }
                d.Thump(0.35f, 0.25f, 190f, 55f, 0.9f, 0.5f);
                d.Bubbles(0.38f, 0.4f, 8, 180f, 600f, 0.3f);
                d.Noise(0.38f, 0.4f, Filter.Band, 300f, 1000f, 0.35f, 1f, 0.02f, 1.6f, 16f, 0.5f);
                return Finish(d.S, 0.42f);
            case "use_wring_out":
                d = new SoundDesign(1.0f, 0x771Cu);
                foreach (float at in new[] { 0.02f, 0.32f, 0.62f })
                {
                    d.Tone(at, 0.28f, Wave.Triangle, 300f, 160f, 0.14f, 0.1f, 1.3f, vibratoHz: 24f, vibratoDepth: 0.08f);
                    d.Noise(at, 0.28f, Filter.Band, 900f, 400f, 0.25f, 2.2f, 0.2f, 1.3f, 20f, 0.5f);
                    d.Thump(at + 0.2f, 0.1f, 160f, 80f, 0.4f, 0.3f);
                }
                return Finish(d.S, 0.4f);
            case "use_dark_pulse":
                d = new SoundDesign(1.0f, 0xDA4Bu);
                d.Tone(0, 0.8f, Wave.Saw, N(34), N(38), 0.14f, 0.2f, 1.3f, vibratoHz: 7f, vibratoDepth: 0.06f);
                foreach (float at in new[] { 0.05f, 0.13f, 0.21f })
                    d.Tone(at, 0.3f, Wave.Sine, N(46), N(58), 0.14f, 0.05f, 1.6f);
                d.Noise(0.2f, 0.4f, Filter.Low, 1800f, 200f, 0.4f, 1f, 0.05f, 1.4f);
                d.Thump(0.34f, 0.45f, 130f, 36f, 1.0f, 0.7f);
                d.Bell(0.36f, 0.5f, N(58), 1.41f, 0.8f, 0.1f, 2.6f);
                return Finish(d.S, 0.44f);
            case "use_dragon_pulse":
                d = new SoundDesign(1.0f, 0xD9B1u);
                d.Tone(0, 0.6f, Wave.Saw, N(43), N(55), 0.14f, 0.1f, 1.3f, vibratoHz: 6f, vibratoDepth: 0.05f);
                d.Notes(0.05f, 0.08f, 0.25f, Wave.Sine, Pitches(67, 71, 74, 79), 0.1f, 0.01f);
                d.Noise(0.1f, 0.5f, Filter.Band, 600f, 2200f, 0.3f, 1.4f, 0.1f, 1.3f, 20f, 0.5f);
                d.Thump(0.34f, 0.4f, 150f, 40f, 1.0f, 0.7f);
                d.Bell(0.36f, 0.55f, N(67), 2.01f, 0.9f, 0.14f, 2.8f);
                return Finish(d.S, 0.44f);
            case "use_dragon_rush":
                d = new SoundDesign(0.9f, 0xD4A5u);
                d.Noise(0, 0.14f, Filter.Band, 1500f, 4000f, 0.6f, 1.1f, 0.05f, 1.6f);
                d.Tone(0.02f, 0.6f, Wave.Saw, N(46), N(55), 0.14f, 0.1f, 1.3f, vibratoHz: 9f, vibratoDepth: 0.08f);
                d.Tone(0.1f, 0.25f, Wave.Sine, 900f, 300f, 0.1f, 0.01f, 1.8f);
                d.Thump(0.34f, 0.35f, 200f, 45f, 1.0f, 0.9f);
                d.Noise(0.34f, 0.07f, Filter.Band, 1500f, 800f, 0.8f, 1f, 0.01f, 2.4f);
                d.Noise(0.4f, 0.4f, Filter.Low, 900f, 200f, 0.35f, 1f, 0.01f, 1.5f);
                return Finish(d.S, 0.44f);
            case "use_hammer_arm":
                d = new SoundDesign(0.9f, 0x4A4Du);
                d.Tone(0, 0.3f, Wave.Triangle, 250f, 450f, 0.1f, 0.2f, 1.3f);
                d.Noise(0.2f, 0.12f, Filter.Band, 600f, 2200f, 0.4f, 1.2f, 0.3f, 1.2f);
                d.Thump(0.33f, 0.45f, 150f, 32f, 1.0f, 1.0f);
                d.Noise(0.33f, 0.07f, Filter.Band, 1200f, 600f, 0.9f, 1f, 0.01f, 2.4f);
                d.Noise(0.38f, 0.4f, Filter.Low, 1000f, 150f, 0.45f, 1f, 0.01f, 1.5f);
                d.Thump(0.6f, 0.2f, 120f, 50f, 0.5f, 0.5f);
                return Finish(d.S, 0.46f);
            case "use_hyper_beam":
                d = new SoundDesign(1.0f, 0x4B30u);
                d.Tone(0, 0.22f, Wave.Saw, 160f, 1000f, 0.12f, 0.5f, 1.3f);
                d.Noise(0.05f, 0.18f, Filter.Band, 800f, 4000f, 0.3f, 1.4f, 0.4f, 1.2f);
                d.Noise(0.2f, 0.7f, Filter.Band, 600f, 2800f, 0.6f, 1.1f, 0.03f, 1.1f, 28f, 0.7f);
                d.Tone(0.2f, 0.7f, Wave.Saw, 140f, 110f, 0.14f, 0.03f, 1.2f, vibratoHz: 18f, vibratoDepth: 0.1f);
                d.Thump(0.34f, 0.45f, 170f, 34f, 1.0f, 0.8f);
                d.Noise(0.6f, 0.4f, Filter.Low, 1400f, 200f, 0.4f, 1f, 0.05f, 1.6f);
                return Finish(d.S, 0.48f);
            case "use_natural_gift":
                d = new SoundDesign(0.9f, 0x6A71u);
                d.Notes(0, 0.07f, 0.22f, Wave.Triangle, Pitches(72, 76, 79, 84), 0.12f, 0.01f);
                d.Bell(0.25f, 0.4f, N(88), 2.01f, 0.7f, 0.12f, 3f);
                d.Noise(0.34f, 0.1f, Filter.High, 2000f, 4500f, 0.5f, 1f, 0.01f, 2.4f);
                d.Thump(0.35f, 0.2f, 230f, 90f, 0.7f, 0.6f);
                d.Notes(0.45f, 0.06f, 0.18f, Wave.Sine, Pitches(91, 88, 84), 0.07f, 0.01f);
                return Finish(d.S, 0.38f);
            case "use_power_whip":
                d = new SoundDesign(0.8f, 0x30E9u);
                d.Noise(0, 0.14f, Filter.Band, 1500f, 4500f, 0.7f, 1.2f, 0.05f, 1.8f);
                d.Noise(0.1f, 0.04f, Filter.High, 3500f, 6000f, 0.9f, 1f, 0.01f, 2.4f);
                d.Thump(0.14f, 0.16f, 230f, 90f, 0.8f, 0.7f);
                d.Noise(0.28f, 0.14f, Filter.Band, 1500f, 4500f, 0.7f, 1.2f, 0.05f, 1.8f);
                d.Thump(0.35f, 0.25f, 190f, 60f, 1.0f, 0.9f);
                d.Noise(0.35f, 0.3f, Filter.Band, 2500f, 6000f, 0.3f, 1f, 0.01f, 1.8f, 30f, 0.6f);
                return Finish(d.S, 0.44f);
            case "use_brave_bird":
                d = new SoundDesign(1.0f, 0xB4A9u);
                d.Noise(0, 0.3f, Filter.Band, 400f, 3500f, 0.5f, 1.2f, 0.5f, 1.2f, 14f, 0.5f);
                d.Tone(0.02f, 0.3f, Wave.Sine, 500f, 1400f, 0.12f, 0.2f, 1.5f);
                d.Thump(0.33f, 0.4f, 220f, 40f, 1.0f, 0.9f);
                d.Noise(0.33f, 0.08f, Filter.Band, 1800f, 900f, 0.9f, 1f, 0.01f, 2.4f);
                d.Tone(0.45f, 0.3f, Wave.Saw, 240f, 90f, 0.1f, 0.02f, 1.6f);
                d.Thump(0.5f, 0.25f, 150f, 50f, 0.5f, 0.4f);
                return Finish(d.S, 0.46f);
            case "use_head_smash":
                d = new SoundDesign(1.0f, 0x45A5u);
                d.Noise(0, 0.28f, Filter.Band, 400f, 2000f, 0.35f, 1.2f, 0.6f, 1.2f);
                d.Thump(0.3f, 0.5f, 140f, 30f, 1.0f, 1.0f);
                d.Noise(0.3f, 0.08f, Filter.Band, 1000f, 500f, 1.0f, 1f, 0.01f, 2.4f);
                d.Crackle(0.32f, 0.4f, 12, 1100f, 0.5f);
                d.Noise(0.4f, 0.45f, Filter.Low, 1100f, 150f, 0.45f, 1f, 0.01f, 1.5f);
                d.Thump(0.52f, 0.3f, 200f, 70f, 0.6f, 0.6f);
                d.Tone(0.55f, 0.35f, Wave.Saw, 220f, 80f, 0.08f, 0.02f, 1.6f);
                return Finish(d.S, 0.48f);
            case "use_leaf_storm":
                d = new SoundDesign(1.0f, 0x1EAFu);
                d.Noise(0, 0.9f, Filter.Band, 800f, 3000f, 0.5f, 1.1f, 0.2f, 1.1f, 34f, 0.9f);
                d.Tone(0, 0.4f, Wave.Sine, 200f, 500f, 0.08f, 0.3f, 1.3f);
                d.Noise(0.3f, 0.6f, Filter.High, 3500f, 6000f, 0.25f, 1f, 0.05f, 1.4f, 50f, 0.8f);
                d.Thump(0.35f, 0.3f, 190f, 60f, 0.7f, 0.5f);
                return Finish(d.S, 0.42f);
            case "use_attack_order":
                d = new SoundDesign(1.0f, 0xA770u);
                d.Tone(0, 0.8f, Wave.Saw, 190f, 230f, 0.1f, 0.15f, 1.2f, vibratoHz: 45f, vibratoDepth: 0.15f);
                d.Tone(0.05f, 0.75f, Wave.Saw, 260f, 300f, 0.07f, 0.15f, 1.2f, vibratoHz: 52f, vibratoDepth: 0.15f);
                d.Noise(0.1f, 0.6f, Filter.Band, 1500f, 3500f, 0.15f, 1.5f, 0.1f, 1.2f, 60f, 0.7f);
                d.Thump(0.33f, 0.15f, 220f, 100f, 0.5f, 0.4f);
                d.Notes(0.45f, 0.07f, 0.1f, Wave.Triangle, Pitches(88, 91, 88), 0.06f, 0.005f);
                return Finish(d.S, 0.38f);
            case "use_bug_buzz":
                d = new SoundDesign(1.0f, 0xB496u);
                d.Tone(0, 0.9f, Wave.Saw, 300f, 310f, 0.12f, 0.1f, 1.1f, vibratoHz: 70f, vibratoDepth: 0.2f);
                d.Tone(0, 0.9f, Wave.Saw, 450f, 480f, 0.08f, 0.1f, 1.1f, vibratoHz: 90f, vibratoDepth: 0.2f);
                d.Noise(0.05f, 0.85f, Filter.Band, 1200f, 3000f, 0.2f, 1.6f, 0.1f, 1.1f, 90f, 0.8f);
                d.Thump(0.33f, 0.15f, 200f, 90f, 0.4f, 0.3f);
                return Finish(d.S, 0.38f);
            case "use_cross_poison":
                d = new SoundDesign(0.8f, 0xC95Au);
                d.Noise(0.05f, 0.1f, Filter.High, 3000f, 7000f, 0.9f, 1f, 0.01f, 2.2f);
                d.Noise(0.17f, 0.1f, Filter.High, 3500f, 7500f, 0.9f, 1f, 0.01f, 2.2f);
                d.Thump(0.25f, 0.18f, 250f, 90f, 0.7f, 0.5f);
                d.Bubbles(0.28f, 0.4f, 8, 250f, 800f, 0.3f);
                d.Tone(0.2f, 0.5f, Wave.Sine, 300f, 160f, 0.08f, 0.05f, 1.6f, vibratoHz: 9f, vibratoDepth: 0.1f);
                return Finish(d.S, 0.4f);
            case "use_defend_order":
                d = new SoundDesign(1.0f, 0xDEF0u);
                d.Tone(0, 0.6f, Wave.Saw, 190f, 230f, 0.09f, 0.15f, 1.2f, vibratoHz: 45f, vibratoDepth: 0.15f);
                d.Noise(0.05f, 0.55f, Filter.Band, 1500f, 3500f, 0.12f, 1.5f, 0.1f, 1.2f, 60f, 0.7f);
                d.Notes(0.55f, 0.06f, 0.2f, Wave.Triangle, Pitches(79, 83), 0.1f, 0.005f);
                d.Bell(0.62f, 0.35f, N(86), 3.01f, 1.0f, 0.16f, 3f);
                d.Thump(0.7f, 0.25f, 150f, 60f, 0.6f, 0.4f);
                return Finish(d.S, 0.38f);
            case "use_heal_order":
                d = new SoundDesign(1.0f, 0x4EA1u);
                d.Tone(0, 0.6f, Wave.Saw, 190f, 230f, 0.08f, 0.15f, 1.2f, vibratoHz: 45f, vibratoDepth: 0.15f);
                d.Noise(0.05f, 0.55f, Filter.Band, 1500f, 3500f, 0.1f, 1.5f, 0.1f, 1.2f, 60f, 0.7f);
                d.Notes(0.55f, 0.08f, 0.25f, Wave.Sine, Pitches(79, 84, 88, 91), 0.12f, 0.01f);
                d.Bell(0.65f, 0.3f, N(96), 2.01f, 0.6f, 0.12f, 3f);
                return Finish(d.S, 0.36f);
            case "use_iron_head":
                d = new SoundDesign(0.8f, 0x1404u);
                d.Bell(0, 0.3f, N(86), 2.76f, 1.4f, 0.16f, 3f);
                d.Noise(0.1f, 0.15f, Filter.Band, 600f, 2400f, 0.3f, 1.2f, 0.4f, 1.2f);
                d.Thump(0.33f, 0.3f, 220f, 60f, 1.0f, 0.9f);
                d.Bell(0.34f, 0.4f, N(70), 3.76f, 1.6f, 0.2f, 3f);
                d.Noise(0.34f, 0.06f, Filter.High, 2500f, 5000f, 0.8f, 1f, 0.01f, 2.4f);
                return Finish(d.S, 0.44f);
            case "use_megahorn":
                d = new SoundDesign(0.9f, 0x3E40u);
                d.Tone(0, 0.3f, Wave.Saw, 120f, 300f, 0.1f, 0.3f, 1.3f);
                d.Noise(0.1f, 0.2f, Filter.Band, 700f, 3000f, 0.4f, 1.2f, 0.4f, 1.2f);
                d.Thump(0.33f, 0.4f, 200f, 38f, 1.0f, 0.9f);
                d.Noise(0.33f, 0.06f, Filter.High, 2000f, 5000f, 0.9f, 1f, 0.01f, 2.4f);
                d.Tone(0.35f, 0.4f, Wave.Triangle, 600f, 240f, 0.1f, 0.01f, 1.8f);
                d.Crackle(0.36f, 0.3f, 10, 1800f, 0.4f);
                return Finish(d.S, 0.46f);
            case "use_power_gem":
                d = new SoundDesign(1.0f, 0x96E4u);
                d.Tone(0, 0.4f, Wave.Sine, N(72), N(96), 0.14f, 0.2f, 1.3f);
                d.Notes(0.1f, 0.06f, 0.2f, Wave.Triangle, Pitches(84, 88, 91, 96, 100), 0.1f, 0.005f);
                d.Bell(0.25f, 0.4f, N(91), 2.76f, 1.0f, 0.16f, 3f);
                d.Noise(0.34f, 0.08f, Filter.High, 3000f, 6500f, 0.8f, 1f, 0.01f, 2.4f);
                d.Thump(0.35f, 0.35f, 170f, 45f, 1.0f, 0.6f);
                d.Crackle(0.4f, 0.4f, 12, 2600f, 0.35f);
                d.Bell(0.45f, 0.5f, N(84), 3.76f, 1.0f, 0.12f, 3f);
                return Finish(d.S, 0.44f);
            case "use_earth_power":
                d = new SoundDesign(1.0f, 0xEA47u);
                foreach (float at in new[] { 0.02f, 0.16f, 0.3f })
                {
                    d.Thump(at, 0.35f, 120f, 32f, 1.0f, 0.5f);
                    d.Noise(at, 0.3f, Filter.Low, 1500f, 200f, 0.4f, 1f, 0.01f, 1.5f);
                    d.Crackle(at + 0.02f, 0.2f, 6, 900f, 0.35f);
                }
                d.Tone(0, 0.8f, Wave.Saw, 55f, 70f, 0.1f, 0.2f, 1.3f, vibratoHz: 12f, vibratoDepth: 0.1f);
                d.Noise(0.45f, 0.5f, Filter.Low, 900f, 150f, 0.3f, 1f, 0.05f, 1.5f);
                return Finish(d.S, 0.46f);
            case "use_rock_wrecker":
                d = new SoundDesign(1.0f, 0x40C4u);
                d.Crackle(0, 0.3f, 8, 700f, 0.3f);
                d.Tone(0.02f, 0.3f, Wave.Saw, 80f, 160f, 0.12f, 0.3f, 1.3f);
                d.Noise(0.1f, 0.2f, Filter.Band, 400f, 1800f, 0.4f, 1.2f, 0.4f, 1.2f);
                d.Thump(0.34f, 0.5f, 140f, 28f, 1.0f, 1.0f);
                d.Crackle(0.35f, 0.4f, 14, 1000f, 0.5f);
                d.Noise(0.36f, 0.5f, Filter.Low, 1200f, 150f, 0.45f, 1f, 0.01f, 1.5f);
                return Finish(d.S, 0.48f);
            case "use_flare_blitz":
                d = new SoundDesign(1.0f, 0xF1A8u);
                d.Noise(0, 0.5f, Filter.Band, 500f, 2500f, 0.5f, 1.1f, 0.3f, 1.1f, 40f, 0.8f);
                d.Tone(0.02f, 0.35f, Wave.Saw, 100f, 240f, 0.1f, 0.3f, 1.3f);
                d.Thump(0.33f, 0.45f, 180f, 34f, 1.0f, 0.9f);
                d.Noise(0.33f, 0.08f, Filter.Band, 1600f, 800f, 0.9f, 1f, 0.01f, 2.4f);
                d.Noise(0.38f, 0.5f, Filter.Band, 600f, 1800f, 0.4f, 1f, 0.01f, 1.5f, 36f, 0.8f);
                d.Thump(0.52f, 0.3f, 220f, 80f, 0.5f, 0.4f);
                return Finish(d.S, 0.46f);
            case "use_overheat":
                d = new SoundDesign(1.0f, 0x0E4Eu);
                d.Tone(0, 0.35f, Wave.Saw, 90f, 180f, 0.1f, 0.4f, 1.3f, vibratoHz: 20f, vibratoDepth: 0.1f);
                d.Noise(0.05f, 0.3f, Filter.Band, 500f, 2000f, 0.3f, 1.4f, 0.5f, 1.2f, 30f, 0.6f);
                d.Noise(0.28f, 0.1f, Filter.High, 2500f, 6000f, 0.9f, 1f, 0.01f, 2.4f);
                d.Thump(0.3f, 0.5f, 150f, 30f, 1.0f, 0.8f);
                d.Noise(0.32f, 0.65f, Filter.Band, 500f, 2200f, 0.55f, 1.1f, 0.02f, 1.3f, 38f, 0.8f);
                return Finish(d.S, 0.48f);
            case "use_solar_beam":
                d = new SoundDesign(1.0f, 0x504Au);
                d.Tone(0, 0.3f, Wave.Sine, N(72), N(100), 0.14f, 0.3f, 1.3f, vibratoHz: 8f, vibratoDepth: 0.03f);
                d.Notes(0.05f, 0.05f, 0.18f, Wave.Triangle, Pitches(84, 88, 91, 96, 100), 0.08f, 0.005f);
                d.Noise(0.2f, 0.7f, Filter.Band, 1000f, 3500f, 0.5f, 1.1f, 0.03f, 1.1f, 26f, 0.7f);
                d.Tone(0.2f, 0.7f, Wave.Saw, 220f, 200f, 0.1f, 0.03f, 1.2f, vibratoHz: 20f, vibratoDepth: 0.08f);
                d.Thump(0.34f, 0.4f, 180f, 36f, 1.0f, 0.7f);
                d.Bell(0.38f, 0.5f, N(88), 2.01f, 0.8f, 0.1f, 2.8f);
                return Finish(d.S, 0.44f);
            case "use_energy_ball":
                d = new SoundDesign(0.9f, 0xE4BAu);
                d.Tone(0, 0.25f, Wave.Sine, N(60), N(84), 0.16f, 0.3f, 1.3f, vibratoHz: 8f, vibratoDepth: 0.03f);
                d.Notes(0.05f, 0.05f, 0.15f, Wave.Triangle, Pitches(79, 83, 86, 91), 0.08f, 0.005f);
                d.Tone(0.22f, 0.15f, Wave.Sine, 500f, 900f, 0.12f, 0.02f, 1.5f);
                d.Noise(0.34f, 0.1f, Filter.High, 2500f, 5500f, 0.7f, 1f, 0.01f, 2.4f);
                d.Thump(0.35f, 0.3f, 200f, 60f, 0.9f, 0.6f);
                d.Notes(0.42f, 0.06f, 0.2f, Wave.Sine, Pitches(91, 86, 83, 79), 0.07f, 0.01f);
                return Finish(d.S, 0.4f);
            case "use_leaf_blade":
                d = new SoundDesign(0.7f, 0x1EAEu);
                d.Noise(0.03f, 0.12f, Filter.Band, 2000f, 6000f, 0.8f, 1.1f, 0.03f, 1.8f);
                d.Noise(0.15f, 0.12f, Filter.Band, 2500f, 6500f, 0.8f, 1.1f, 0.03f, 1.8f);
                d.Thump(0.24f, 0.18f, 270f, 90f, 0.7f, 0.5f);
                d.Noise(0.25f, 0.3f, Filter.High, 3500f, 6500f, 0.25f, 1f, 0.02f, 1.6f, 40f, 0.8f);
                return Finish(d.S, 0.4f);
            case "use_aura_sphere":
                d = new SoundDesign(1.0f, 0xA4A5u);
                d.Tone(0, 0.5f, Wave.Sine, N(55), N(79), 0.18f, 0.3f, 1.3f, vibratoHz: 6f, vibratoDepth: 0.04f);
                d.Notes(0.05f, 0.07f, 0.25f, Wave.Triangle, Pitches(79, 83, 86, 91), 0.08f, 0.01f);
                d.Tone(0.35f, 0.15f, Wave.Sine, 800f, 300f, 0.1f, 0.01f, 1.8f);
                d.Thump(0.4f, 0.4f, 160f, 36f, 1.0f, 0.7f);
                d.Noise(0.4f, 0.1f, Filter.High, 2500f, 6000f, 0.8f, 1f, 0.01f, 2.4f);
                d.Bell(0.43f, 0.5f, N(79), 2.01f, 0.9f, 0.14f, 2.8f);
                return Finish(d.S, 0.44f);
            case "use_extreme_speed":
                d = new SoundDesign(0.9f, 0xE55Du);
                d.Noise(0, 0.3f, Filter.Band, 1500f, 5000f, 0.6f, 1.1f, 0.03f, 1.6f, 40f, 0.5f);
                foreach (float at in new[] { 0.24f, 0.3f, 0.36f })
                {
                    d.Noise(at, 0.05f, Filter.Band, 1500f, 900f, 0.8f, 1f, 0.01f, 2.2f);
                    d.Thump(at + 0.01f, 0.16f, 240f - (at - 0.24f) * 300f, 80f, 0.7f, 0.6f);
                }
                d.Noise(0.45f, 0.35f, Filter.Band, 3000f, 1000f, 0.4f, 1f, 0.02f, 1.6f);
                return Finish(d.S, 0.44f);
            case "use_mirror_coat":
                d = new SoundDesign(0.9f, 0x3C07u);
                d.Bell(0, 0.5f, N(96), 3.01f, 1.2f, 0.2f, 3f);
                d.Bell(0.08f, 0.5f, N(103), 2.01f, 1.0f, 0.14f, 3f);
                d.Notes(0.12f, 0.04f, 0.12f, Wave.Sine, Pitches(100, 103, 108), 0.1f, 0.003f);
                d.Tone(0.2f, 0.4f, Wave.Triangle, N(79), N(96), 0.12f, 0.05f, 1.4f);
                d.Noise(0.3f, 0.1f, Filter.High, 3000f, 7000f, 0.8f, 1f, 0.01f, 2.4f);
                d.Thump(0.34f, 0.3f, 190f, 60f, 0.9f, 0.6f);
                return Finish(d.S, 0.4f);
            case "use_surf":
                d = new SoundDesign(1.0f, 0x5072u);
                d.Noise(0, 0.9f, Filter.Band, 300f, 2000f, 0.5f, 1.1f, 0.3f, 1.1f, 6f, 0.6f);
                d.Tone(0, 0.5f, Wave.Sine, 80f, 160f, 0.14f, 0.3f, 1.3f);
                d.Bubbles(0.1f, 0.6f, 10, 200f, 800f, 0.3f);
                d.Noise(0.33f, 0.1f, Filter.High, 3000f, 5500f, 0.8f, 1f, 0.01f, 2.4f);
                d.Thump(0.35f, 0.4f, 160f, 40f, 1.0f, 0.6f);
                d.Noise(0.4f, 0.55f, Filter.Band, 500f, 2400f, 0.45f, 1f, 0.01f, 1.5f, 20f, 0.6f);
                return Finish(d.S, 0.46f);
            case "use_heat_wave":
                d = new SoundDesign(1.0f, 0x4EA7u);
                d.Noise(0, 0.95f, Filter.Band, 400f, 1800f, 0.5f, 1.1f, 0.25f, 1.0f, 5f, 0.6f);
                d.Tone(0, 0.9f, Wave.Saw, 70f, 110f, 0.1f, 0.3f, 1.1f, vibratoHz: 5f, vibratoDepth: 0.1f);
                d.Crackle(0.2f, 0.7f, 18, 2000f, 0.3f);
                d.Noise(0.3f, 0.5f, Filter.High, 2500f, 5000f, 0.2f, 1f, 0.1f, 1.4f, 30f, 0.8f);
                return Finish(d.S, 0.42f);
            case "use_acid":
                d = new SoundDesign(0.8f, 0xAC1Du);
                d.Noise(0, 0.25f, Filter.Band, 500f, 1600f, 0.35f, 1.2f, 0.1f, 1.4f);
                d.Bubbles(0.02f, 0.2f, 4, 300f, 800f, 0.3f);
                d.Thump(0.35f, 0.2f, 220f, 90f, 0.6f, 0.4f);
                d.Noise(0.36f, 0.4f, Filter.High, 3000f, 7000f, 0.25f, 1f, 0.01f, 1.6f, 50f, 0.9f);
                d.Bubbles(0.38f, 0.4f, 8, 250f, 900f, 0.3f);
                return Finish(d.S, 0.4f);
            case "use_constrict":
                d = new SoundDesign(0.9f, 0xC045u);
                for (int i = 0; i < 5; i++)
                    d.Tone(0.02f + i * 0.04f, 0.1f, Wave.Triangle, 400f - i * 25f, 300f - i * 25f, 0.1f, 0.01f, 1.8f);
                foreach (float at in new[] { 0.3f, 0.42f })
                {
                    d.Noise(at, 0.12f, Filter.Band, 800f, 350f, 0.4f, 2f, 0.1f, 1.4f);
                    d.Thump(at + 0.05f, 0.15f, 180f, 70f, 0.6f, 0.4f);
                }
                return Finish(d.S, 0.38f);
            case "use_razor_wind":
                d = new SoundDesign(1.0f, 0x4A20u);
                d.Noise(0, 0.4f, Filter.Band, 400f, 2500f, 0.45f, 1.2f, 0.5f, 1.2f, 14f, 0.6f);
                d.Tone(0, 0.35f, Wave.Sine, 250f, 700f, 0.08f, 0.4f, 1.3f);
                foreach (float at in new[] { 0.28f, 0.45f })
                {
                    d.Noise(at, 0.15f, Filter.High, 2500f, 7000f, 0.7f, 1f, 0.02f, 1.8f);
                    d.Thump(at + 0.08f, 0.15f, 250f, 90f, 0.6f, 0.4f);
                }
                return Finish(d.S, 0.4f);
            case "use_tailwind":
                d = new SoundDesign(1.0f, 0x7A1Du);
                d.Noise(0, 0.95f, Filter.Band, 600f, 2800f, 0.5f, 1.2f, 0.3f, 1.0f, 6f, 0.6f);
                d.Noise(0.1f, 0.8f, Filter.High, 2500f, 5000f, 0.25f, 1f, 0.2f, 1.1f, 10f, 0.7f);
                d.Tone(0.1f, 0.6f, Wave.Sine, 300f, 650f, 0.06f, 0.3f, 1.4f);
                d.Notes(0.5f, 0.07f, 0.2f, Wave.Triangle, Pitches(79, 83, 86), 0.06f, 0.01f);
                return Finish(d.S, 0.38f);
            case "use_bullet_punch":
                d = new SoundDesign(0.7f, 0xB011u);
                foreach (float at in new[] { 0.1f, 0.22f })
                {
                    d.Noise(at - 0.05f, 0.06f, Filter.Band, 2000f, 5000f, 0.5f, 1f, 0.01f, 2f);
                    d.Thump(at, 0.15f, 280f, 100f, 0.9f, 0.9f);
                    d.Bell(at, 0.25f, N(79), 3.76f, 1.4f, 0.14f, 3.2f);
                }
                return Finish(d.S, 0.42f);
            case "use_dive":
                d = new SoundDesign(1.0f, 0xD1E0u);
                d.Noise(0, 0.2f, Filter.Band, 600f, 2800f, 0.5f, 1.1f, 0.05f, 1.6f);
                d.Bubbles(0.02f, 0.3f, 8, 200f, 900f, 0.35f);
                d.Tone(0.1f, 0.3f, Wave.Sine, 400f, 120f, 0.1f, 0.05f, 1.5f);
                d.Noise(0.28f, 0.1f, Filter.High, 3000f, 6000f, 0.8f, 1f, 0.01f, 2.4f);
                d.Thump(0.33f, 0.35f, 170f, 45f, 1.0f, 0.6f);
                d.Noise(0.35f, 0.5f, Filter.Band, 500f, 2400f, 0.45f, 1f, 0.01f, 1.5f, 22f, 0.6f);
                d.Bubbles(0.4f, 0.4f, 8, 250f, 900f, 0.3f);
                return Finish(d.S, 0.46f);
            case "use_dynamic_punch":
                d = new SoundDesign(1.0f, 0xD4B0u);
                d.Tone(0, 0.3f, Wave.Saw, 120f, 260f, 0.12f, 0.4f, 1.3f);
                d.Noise(0.1f, 0.22f, Filter.Band, 600f, 2800f, 0.35f, 1.2f, 0.5f, 1.2f, 16f, 0.5f);
                d.Thump(0.33f, 0.5f, 170f, 30f, 1.0f, 1.0f);
                d.Noise(0.33f, 0.07f, Filter.Band, 1600f, 700f, 1.0f, 1f, 0.01f, 2.4f);
                d.Crackle(0.35f, 0.3f, 10, 1500f, 0.4f);
                d.Thump(0.5f, 0.3f, 130f, 45f, 0.7f, 0.5f);
                d.Noise(0.5f, 0.4f, Filter.Low, 1200f, 150f, 0.4f, 1f, 0.01f, 1.5f);
                return Finish(d.S, 0.5f);
            case "use_guard_swap":
                d = new SoundDesign(1.0f, 0x6504u);
                for (int i = 0; i < 6; i++)
                    d.Bell(0.02f + i * 0.14f, 0.3f, N(i < 3 ? 86 : 91), 2.01f, 0.8f, 0.14f, 3f);
                d.Tone(0.05f, 0.8f, Wave.Triangle, N(74), N(79), 0.05f, 0.2f, 1.4f, vibratoHz: 6f, vibratoDepth: 0.03f);
                return Finish(d.S, 0.34f);
            case "use_gunk_shot":
                d = new SoundDesign(0.9f, 0x6A55u);
                d.Noise(0, 0.3f, Filter.Band, 300f, 1000f, 0.4f, 1.2f, 0.2f, 1.3f);
                d.Bubbles(0.02f, 0.3f, 7, 150f, 500f, 0.35f);
                d.Thump(0.35f, 0.3f, 160f, 40f, 1.0f, 0.6f);
                d.Noise(0.35f, 0.5f, Filter.Band, 250f, 1200f, 0.5f, 1.2f, 0.02f, 1.5f, 18f, 0.7f);
                d.Bubbles(0.4f, 0.4f, 9, 150f, 600f, 0.35f);
                return Finish(d.S, 0.44f);
            case "use_outrage":
                d = new SoundDesign(1.0f, 0x0072u);
                d.Tone(0, 0.9f, Wave.Saw, 80f, 140f, 0.14f, 0.2f, 1.2f, vibratoHz: 12f, vibratoDepth: 0.12f);
                d.Noise(0, 0.9f, Filter.Band, 400f, 2200f, 0.45f, 1.2f, 0.2f, 1.1f, 30f, 0.8f);
                foreach (float at in new[] { 0.3f, 0.6f })
                {
                    d.Thump(at, 0.3f, 170f, 40f, 1.0f, 0.8f);
                    d.Noise(at, 0.07f, Filter.Band, 1500f, 700f, 0.8f, 1f, 0.01f, 2.4f);
                }
                return Finish(d.S, 0.46f);
            case "use_perish_song":
                d = new SoundDesign(1.0f, 0x9E41u);
                d.Notes(0.02f, 0.2f, 0.4f, Wave.Sine, Pitches(69, 66, 64, 62), 0.15f, 0.03f);
                d.Notes(0.02f, 0.2f, 0.4f, Wave.Triangle, Pitches(57, 54, 52, 50), 0.1f, 0.03f);
                d.Bell(0.5f, 0.5f, N(45), 1.41f, 0.8f, 0.14f, 2.4f);
                d.Tone(0, 0.95f, Wave.Sine, N(33), N(32), 0.1f, 0.3f, 1.2f, vibratoHz: 4f, vibratoDepth: 0.03f);
                return Finish(d.S, 0.36f);
            case "use_power_swap":
                d = new SoundDesign(1.0f, 0x9505u);
                for (int i = 0; i < 6; i++)
                    d.Bell(0.02f + i * 0.14f, 0.3f, N(i < 3 ? 74 : 79), 2.76f, 1.0f, 0.14f, 3f);
                d.Tone(0.05f, 0.8f, Wave.Triangle, N(62), N(67), 0.06f, 0.2f, 1.4f, vibratoHz: 6f, vibratoDepth: 0.03f);
                d.Thump(0.4f, 0.2f, 160f, 70f, 0.4f, 0.3f);
                return Finish(d.S, 0.34f);
            case "use_sheer_cold":
                d = new SoundDesign(1.0f, 0x54C0u);
                d.Noise(0, 0.5f, Filter.High, 2500f, 6500f, 0.4f, 1f, 0.3f, 1.1f, 14f, 0.7f);
                d.Tone(0, 0.5f, Wave.Sine, N(84), N(72), 0.1f, 0.3f, 1.3f);
                foreach (float at in new[] { 0.1f, 0.2f, 0.3f })
                    d.Bell(at, 0.3f, N(91 + (int)(at * 20f)), 3.01f, 1.0f, 0.12f, 3f);
                d.Crackle(0.3f, 0.35f, 14, 3000f, 0.45f);
                d.Thump(0.4f, 0.3f, 200f, 60f, 0.8f, 0.5f);
                d.Notes(0.5f, 0.08f, 0.3f, Wave.Sine, Pitches(103, 100, 96, 91), 0.07f, 0.005f);
                return Finish(d.S, 0.42f);
            case "use_spore":
                d = new SoundDesign(1.0f, 0x5906u);
                d.Noise(0, 0.9f, Filter.Band, 1500f, 3500f, 0.3f, 1.2f, 0.2f, 1.1f, 28f, 0.9f);
                d.Tone(0.05f, 0.8f, Wave.Sine, N(76), N(72), 0.06f, 0.3f, 1.4f, vibratoHz: 5f, vibratoDepth: 0.04f);
                d.Notes(0.3f, 0.12f, 0.3f, Wave.Triangle, Pitches(88, 84, 86, 81), 0.05f, 0.02f);
                return Finish(d.S, 0.3f);
            case "use_acid_armor":
                d = new SoundDesign(0.9f, 0xAC4Au);
                d.Tone(0, 0.5f, Wave.Sine, 300f, 160f, 0.12f, 0.1f, 1.3f, vibratoHz: 14f, vibratoDepth: 0.15f);
                d.Noise(0, 0.5f, Filter.Band, 600f, 250f, 0.3f, 2f, 0.1f, 1.2f, 16f, 0.7f);
                foreach (float at in new[] { 0.35f, 0.4f, 0.5f, 0.55f })
                    d.Bubble(at, 450f + at * 300f, 0.35f);
                d.Bubbles(0.5f, 0.3f, 5, 200f, 600f, 0.3f);
                return Finish(d.S, 0.36f);
            case "use_aromatherapy":
                d = new SoundDesign(1.0f, 0xA40Au);
                d.Noise(0, 0.8f, Filter.Band, 2000f, 4500f, 0.15f, 1f, 0.3f, 1.1f, 10f, 0.5f);
                d.Notes(0.1f, 0.12f, 0.35f, Wave.Sine, Pitches(79, 83, 86, 91, 95), 0.12f, 0.02f);
                d.Bell(0.6f, 0.4f, N(91), 2.01f, 0.5f, 0.12f, 3f);
                d.Notes(0.7f, 0.06f, 0.2f, Wave.Triangle, Pitches(95, 98, 103), 0.07f, 0.01f);
                return Finish(d.S, 0.34f);
            case "use_destiny_bond":
                d = new SoundDesign(1.0f, 0xDE57u);
                d.Tone(0, 0.5f, Wave.Sine, N(48), N(53), 0.16f, 0.1f, 1.3f, vibratoHz: 6f, vibratoDepth: 0.06f);
                d.Notes(0.1f, 0.2f, 0.5f, Wave.Triangle, Pitches(65, 63), 0.1f, 0.03f);
                foreach (float at in new[] { 0.3f, 0.65f })
                {
                    d.Bell(at, 0.4f, N(60), 1.41f, 1.0f, 0.16f, 2.6f);
                    d.Thump(at, 0.3f, 100f, 40f, 0.7f, 0.3f);
                }
                d.Noise(0.2f, 0.7f, Filter.Band, 400f, 1200f, 0.12f, 2f, 0.3f, 1.2f, 6f, 0.6f);
                return Finish(d.S, 0.4f);
            case "use_gastro_acid":
                d = new SoundDesign(0.9f, 0x6A5Au);
                d.Noise(0, 0.3f, Filter.Band, 500f, 1800f, 0.3f, 1.3f, 0.1f, 1.4f);
                d.Bubbles(0.02f, 0.5f, 10, 250f, 900f, 0.3f);
                d.Thump(0.35f, 0.2f, 180f, 70f, 0.5f, 0.3f);
                d.Noise(0.36f, 0.5f, Filter.High, 3000f, 6500f, 0.2f, 1f, 0.01f, 1.4f, 50f, 0.9f);
                d.Tone(0.4f, 0.4f, Wave.Sine, 350f, 150f, 0.07f, 0.05f, 1.6f, vibratoHz: 10f, vibratoDepth: 0.1f);
                return Finish(d.S, 0.38f);
            case "use_grudge":
                d = new SoundDesign(1.0f, 0x6F0Du);
                d.Tone(0, 0.9f, Wave.Saw, N(36), N(38), 0.14f, 0.2f, 1.2f, vibratoHz: 6f, vibratoDepth: 0.08f);
                d.Noise(0.05f, 0.8f, Filter.Band, 500f, 1500f, 0.3f, 1.6f, 0.2f, 1.1f, 24f, 0.8f);
                d.Notes(0.3f, 0.2f, 0.4f, Wave.Triangle, Pitches(62, 61, 60), 0.08f, 0.03f);
                d.Thump(0.5f, 0.35f, 90f, 36f, 0.8f, 0.3f);
                d.Crackle(0.55f, 0.35f, 8, 1200f, 0.3f);
                return Finish(d.S, 0.4f);
            case "use_mega_kick":
                d = new SoundDesign(0.9f, 0x3E61u);
                d.Noise(0.05f, 0.25f, Filter.Band, 500f, 2800f, 0.4f, 1.2f, 0.4f, 1.2f);
                d.Thump(0.33f, 0.5f, 190f, 30f, 1.0f, 1.0f);
                d.Noise(0.33f, 0.07f, Filter.Band, 1500f, 700f, 1.0f, 1f, 0.01f, 2.4f);
                d.Thump(0.48f, 0.25f, 140f, 55f, 0.6f, 0.4f);
                d.Noise(0.4f, 0.4f, Filter.Low, 1000f, 150f, 0.35f, 1f, 0.01f, 1.5f);
                return Finish(d.S, 0.46f);
            case "use_moonlight":
                d = new SoundDesign(1.0f, 0x300Eu);
                d.Tone(0, 0.8f, Wave.Sine, N(64), N(76), 0.14f, 0.3f, 1.3f, vibratoHz: 5f, vibratoDepth: 0.03f);
                d.Notes(0.1f, 0.14f, 0.45f, Wave.Triangle, Pitches(84, 88, 91, 96), 0.1f, 0.03f);
                d.Bell(0.6f, 0.4f, N(100), 2.01f, 0.5f, 0.1f, 3f);
                d.Notes(0.65f, 0.07f, 0.25f, Wave.Sine, Pitches(103, 107), 0.05f, 0.01f);
                return Finish(d.S, 0.32f);
            case "use_trick":
                d = new SoundDesign(1.0f, 0x7210u);
                d.Noise(0.03f, 0.2f, Filter.High, 2500f, 6000f, 0.35f, 1f, 0.05f, 1.6f);
                d.Notes(0.1f, 0.05f, 0.14f, Wave.Triangle, Pitches(76, 79, 83, 88, 83, 79), 0.1f, 0.005f);
                foreach (float at in new[] { 0.5f, 0.6f, 0.7f, 0.8f })
                    d.Bell(at, 0.2f, N(88 + (int)(at * 10f) - 5), 2.01f, 0.7f, 0.11f, 3f);
                return Finish(d.S, 0.34f);
        }
        return null;
    }
}
