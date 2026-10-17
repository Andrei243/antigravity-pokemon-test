using static PokemonPlatinumEngine.Audio.SoundDesign;

namespace PokemonPlatinumEngine.Audio;

// Plan 25 - Y10, its second part (Y10b), second half: the sounds of the moves the story shows nowhere, from Tail Glow
// to Shadow Force. As the batches before: each move's beats are the original's (when it rings, how often, what it is
// made of, read from its animation script), its sound our own, built from the bank's shared layers. Howl has no
// sound here: it is the user's own cry.
public static partial class SoundBank
{
    /// <summary>A sound of Y10's second part, second half, or null for a name that isn't one.</summary>
    private static float[]? MakeMoveSoundY10c(string name)
    {
        SoundDesign d;
        switch (name)
        {
            case "use_tail_glow":
                d = new SoundDesign(1.0f, 0x7A61u);
                d.Tone(0, 0.9f, Wave.Sine, N(72), N(84), 0.1f, 0.3f, 1.3f, vibratoHz: 8f, vibratoDepth: 0.03f);
                for (int i = 0; i < 6; i++) d.Bell(0.1f + i * 0.14f, 0.3f, N(91 + i * 2), 2.01f, 0.6f, 0.07f, 3f);
                d.Noise(0.2f, 0.7f, Filter.High, 4000f, 8000f, 0.1f, 1f, 0.2f, 1.3f, 12f, 0.6f);
                return Finish(d.S, 0.32f);
            case "use_luster_purge":
                d = new SoundDesign(1.0f, 0x1057u);
                d.Tone(0, 0.5f, Wave.Sine, N(69), N(88), 0.11f, 0.4f, 1.2f, vibratoHz: 7f, vibratoDepth: 0.03f);
                d.Notes(0.1f, 0.06f, 0.2f, Wave.Triangle, Pitches(88, 91, 95, 100, 103, 107), 0.07f, 0.005f);
                d.Noise(0.3f, 0.3f, Filter.High, 3000f, 8000f, 0.4f, 0.8f, 0.2f, 1.4f);
                d.Thump(0.4f, 0.4f, 170f, 36f, 0.9f, 0.6f);
                d.Bell(0.4f, 0.55f, N(95), 3.01f, 1.0f, 0.12f, 2.8f);
                d.Crackle(0.45f, 0.4f, 12, 4500f, 0.2f);
                return Finish(d.S, 0.42f);
            case "use_mist_ball":
                d = new SoundDesign(1.0f, 0x3157u);
                d.Noise(0, 0.5f, Filter.Band, 1500f, 3200f, 0.25f, 0.8f, 0.4f, 1.2f, 8f, 0.5f);
                d.Tone(0.05f, 0.4f, Wave.Triangle, N(60), N(72), 0.07f, 0.3f, 1.4f);
                d.Thump(0.36f, 0.3f, 180f, 50f, 0.8f, 0.4f);
                d.Noise(0.38f, 0.5f, Filter.Band, 2500f, 900f, 0.35f, 0.8f, 0.01f, 1.5f, 18f, 0.5f);
                d.Notes(0.45f, 0.07f, 0.25f, Wave.Sine, Pitches(91, 88, 84, 79), 0.06f, 0.01f);
                return Finish(d.S, 0.38f);
            case "use_feather_dance":
                d = new SoundDesign(1.0f, 0xFEA7u);
                d.Noise(0, 0.9f, Filter.High, 3500f, 6500f, 0.15f, 0.8f, 0.3f, 1.1f, 7f, 0.5f);
                d.Notes(0.1f, 0.14f, 0.35f, Wave.Sine, Pitches(88, 84, 86, 79, 81, 76), 0.07f, 0.03f);
                d.Tone(0.5f, 0.45f, Wave.Triangle, N(67), N(60), 0.06f, 0.2f, 1.5f);
                return Finish(d.S, 0.32f);
            case "use_ice_ball":
                d = new SoundDesign(0.9f, 0x1CBAu);
                d.Noise(0, 0.32f, Filter.High, 2500f, 6500f, 0.25f, 1f, 0.3f, 1.3f, 24f, 0.5f);
                for (int i = 0; i < 4; i++) d.Thump(0.05f + i * 0.08f, 0.1f, 400f, 200f, 0.25f, 0.4f);
                d.Thump(0.34f, 0.3f, 220f, 55f, 0.9f, 0.7f);
                d.Noise(0.34f, 0.06f, Filter.High, 5500f, 3500f, 0.7f, 1.2f, 0.01f, 2.4f);
                d.Bell(0.35f, 0.4f, N(98), 3.01f, 0.9f, 0.1f, 3f);
                d.Crackle(0.4f, 0.25f, 9, 5000f, 0.2f);
                return Finish(d.S, 0.4f);
            case "use_blast_burn":
                d = new SoundDesign(1.0f, 0xB1A5u);
                d.Noise(0, 0.35f, Filter.Band, 400f, 2600f, 0.4f, 1.1f, 0.3f, 1.2f, 20f, 0.6f);
                d.Tone(0, 0.35f, Wave.Saw, 80f, 260f, 0.1f, 0.3f, 1.3f);
                d.Thump(0.34f, 0.65f, 140f, 24f, 1.0f, 1.0f);
                d.Noise(0.34f, 0.08f, Filter.Band, 1500f, 700f, 1.0f, 1f, 0.01f, 2.4f);
                d.Noise(0.4f, 0.55f, Filter.Low, 1400f, 150f, 0.5f, 1f, 0.02f, 1.4f);
                d.Crackle(0.45f, 0.5f, 18, 1800f, 0.3f);
                return Finish(d.S, 0.48f);
            case "use_hydro_cannon":
                d = new SoundDesign(1.0f, 0x4CA7u);
                d.Tone(0, 0.3f, Wave.Sine, 200f, 480f, 0.12f, 0.4f, 1.3f, vibratoHz: 16f, vibratoDepth: 0.06f);
                d.Noise(0, 0.32f, Filter.Band, 300f, 2000f, 0.3f, 1.2f, 0.5f, 1.2f, 22f, 0.5f);
                d.Noise(0.3f, 0.35f, Filter.Band, 600f, 3000f, 0.4f, 0.9f, 0.05f, 1.2f, 30f, 0.4f);
                d.Thump(0.36f, 0.5f, 170f, 30f, 1.0f, 0.9f);
                d.Noise(0.4f, 0.5f, Filter.Band, 2800f, 600f, 0.4f, 0.8f, 0.01f, 1.5f, 28f, 0.6f);
                d.Bubbles(0.45f, 0.5f, 10, 150f, 600f, 0.3f);
                return Finish(d.S, 0.48f);
            case "use_weather_ball":
                d = new SoundDesign(0.9f, 0x3EA7u);
                d.Tone(0, 0.3f, Wave.Triangle, 200f, 700f, 0.1f, 0.3f, 1.3f, vibratoHz: 14f, vibratoDepth: 0.08f);
                d.Noise(0.05f, 0.25f, Filter.Band, 800f, 3500f, 0.25f, 1.2f, 0.4f, 1.3f, 18f, 0.5f);
                d.Noise(0.28f, 0.1f, Filter.High, 3500f, 7000f, 0.45f, 1f, 0.02f, 2f);
                d.Thump(0.36f, 0.3f, 200f, 45f, 0.9f, 0.8f);
                d.Noise(0.36f, 0.45f, Filter.Band, 1200f, 500f, 0.3f, 1f, 0.01f, 1.5f, 24f, 0.5f);
                d.Bell(0.38f, 0.4f, N(79), 2.01f, 0.7f, 0.09f, 3f);
                return Finish(d.S, 0.4f);
            case "use_fake_tears":
                d = new SoundDesign(1.0f, 0xFA7Eu);
                d.Notes(0, 0.2f, 0.4f, Wave.Sine, Pitches(79, 76, 72, 74, 71), 0.1f, 0.05f);
                d.Tone(0, 0.9f, Wave.Triangle, N(55), N(52), 0.05f, 0.3f, 1.4f, vibratoHz: 6f, vibratoDepth: 0.15f);
                for (int i = 0; i < 6; i++) d.Bubble(0.3f + i * 0.1f, 500f + i * 80f, 0.2f);
                return Finish(d.S, 0.32f);
            case "use_frenzy_plant":
                d = new SoundDesign(1.0f, 0xF2E7u);
                for (int i = 0; i < 4; i++)
                {
                    float at = 0.02f + i * 0.07f;
                    d.Noise(at, 0.2f, Filter.Band, 300f, 1500f + i * 300f, 0.3f, 1.4f, 0.3f, 1.3f, 14f, 0.6f);
                    d.Thump(at + 0.05f, 0.14f, 150f, 60f, 0.4f, 0.4f);
                }
                d.Thump(0.34f, 0.5f, 190f, 28f, 1.0f, 1.0f);
                d.Noise(0.34f, 0.06f, Filter.Band, 1500f, 700f, 0.9f, 1f, 0.01f, 2.4f);
                d.Noise(0.4f, 0.5f, Filter.High, 2500f, 5500f, 0.3f, 0.9f, 0.02f, 1.5f, 30f, 0.7f);
                d.Crackle(0.42f, 0.4f, 12, 1500f, 0.25f);
                return Finish(d.S, 0.46f);
            case "use_bulk_up":
                d = new SoundDesign(1.0f, 0xB01Cu);
                for (int i = 0; i < 3; i++)
                {
                    float at = 0.05f + i * 0.26f;
                    d.Tone(at, 0.28f, Wave.Saw, N(40 + i * 3), N(46 + i * 3), 0.1f, 0.2f, 1.4f, vibratoHz: 10f, vibratoDepth: 0.05f);
                    d.Thump(at + 0.05f, 0.2f, 160f, 60f, 0.6f, 0.4f);
                }
                d.Notes(0.7f, 0.07f, 0.2f, Wave.Triangle, Pitches(64, 67, 71, 76), 0.08f, 0.01f);
                return Finish(d.S, 0.38f);
            case "use_poison_tail":
                d = new SoundDesign(1.0f, 0x2017u);
                d.Noise(0, 0.28f, Filter.Band, 700f, 3500f, 0.3f, 1.3f, 0.4f, 1.2f);
                d.Thump(0.3f, 0.3f, 220f, 60f, 0.9f, 0.9f);
                d.Noise(0.3f, 0.06f, Filter.High, 3000f, 1800f, 0.6f, 1f, 0.01f, 2.4f);
                foreach (float at in new[] { 0.42f, 0.5f, 0.58f, 0.66f })
                    d.Bubble(at, 260f + at * 200f, 0.3f);
                d.Noise(0.4f, 0.4f, Filter.Band, 400f, 1200f, 0.2f, 1.2f, 0.02f, 1.5f, 16f, 0.6f);
                return Finish(d.S, 0.4f);
            case "use_volt_tackle":
                d = new SoundDesign(1.0f, 0x701Au);
                d.Tone(0, 0.35f, Wave.Saw, 120f, 800f, 0.1f, 0.3f, 1.3f, vibratoHz: 40f, vibratoDepth: 0.1f);
                d.Crackle(0, 0.4f, 18, 3000f, 0.3f);
                d.Noise(0.1f, 0.25f, Filter.Band, 1000f, 4500f, 0.3f, 1.2f, 0.4f, 1.2f);
                d.Thump(0.34f, 0.5f, 190f, 30f, 1.0f, 1.0f);
                d.Noise(0.34f, 0.07f, Filter.Band, 1800f, 900f, 0.9f, 1f, 0.01f, 2.4f);
                d.Noise(0.4f, 0.5f, Filter.Band, 1500f, 4000f, 0.3f, 1.2f, 0.02f, 1.4f, 50f, 0.7f);
                d.Crackle(0.4f, 0.5f, 18, 3200f, 0.3f);
                d.Thump(0.6f, 0.25f, 160f, 50f, 0.5f, 0.4f);
                return Finish(d.S, 0.46f);
            case "use_doom_desire":
                d = new SoundDesign(1.0f, 0xD00Du);
                d.Tone(0, 0.9f, Wave.Sine, N(48), N(60), 0.13f, 0.3f, 1.3f, vibratoHz: 5f, vibratoDepth: 0.04f);
                d.Notes(0.15f, 0.12f, 0.45f, Wave.Triangle, Pitches(67, 72, 76, 79, 84, 88), 0.08f, 0.04f);
                d.Bell(0.3f, 0.6f, N(60), 1.41f, 1.0f, 0.12f, 2.6f);
                d.Bell(0.7f, 0.3f, N(96), 2.01f, 0.5f, 0.08f, 3f);
                d.Noise(0.5f, 0.45f, Filter.High, 3000f, 8000f, 0.08f, 1f, 0.2f, 1.4f);
                return Finish(d.S, 0.34f);
            case "use_psycho_boost":
                d = new SoundDesign(1.0f, 0x59B0u);
                d.Tone(0, 0.35f, Wave.Sine, N(60), N(84), 0.13f, 0.4f, 1.2f, vibratoHz: 9f, vibratoDepth: 0.05f);
                d.Notes(0.05f, 0.05f, 0.14f, Wave.Triangle, Pitches(72, 76, 79, 84, 88, 91, 96), 0.07f, 0.005f);
                d.Thump(0.34f, 0.6f, 170f, 28f, 1.0f, 0.9f);
                d.Noise(0.34f, 0.07f, Filter.Band, 1700f, 800f, 0.9f, 1f, 0.01f, 2.4f);
                d.Tone(0.36f, 0.55f, Wave.Sine, N(84), N(48), 0.12f, 0.02f, 1.5f, vibratoHz: 9f, vibratoDepth: 0.08f);
                d.Bell(0.38f, 0.5f, N(84), 2.41f, 0.9f, 0.1f, 2.8f);
                return Finish(d.S, 0.46f);
            case "use_trump_card":
                d = new SoundDesign(0.9f, 0x7C4Du);
                for (int i = 0; i < 4; i++)
                {
                    float at = 0.02f + i * 0.06f;
                    d.Noise(at, 0.1f, Filter.High, 3000f, 6500f, 0.25f, 1f, 0.2f, 1.6f);
                    d.Tone(at, 0.1f, Wave.Sine, 1200f, 2400f, 0.04f, 0.1f, 2f);
                    d.Thump(at + 0.12f, 0.08f, 500f, 250f, 0.3f + i * 0.1f, 0.6f);
                }
                d.Notes(0.36f, 0.05f, 0.14f, Wave.Triangle, Pitches(91, 95, 100, 103), 0.08f, 0.005f);
                d.Bell(0.38f, 0.4f, N(96), 3.01f, 0.9f, 0.11f, 3f);
                return Finish(d.S, 0.4f);
            case "use_power_trick":
                d = new SoundDesign(1.0f, 0x7071u);
                foreach (float at in new[] { 0.0f, 0.4f })
                {
                    d.Tone(at, 0.3f, Wave.Triangle, N(55), N(67), 0.1f, 0.2f, 1.4f, vibratoHz: 8f, vibratoDepth: 0.04f);
                    d.Bell(at + 0.2f, 0.4f, N(79), 2.01f, 0.8f, 0.11f, 2.8f);
                }
                d.Notes(0.25f, 0.06f, 0.14f, Wave.Sine, Pitches(84, 88, 91, 88), 0.06f, 0.005f);
                d.Notes(0.65f, 0.06f, 0.16f, Wave.Sine, Pitches(91, 96, 100, 103), 0.07f, 0.005f);
                return Finish(d.S, 0.34f);
            case "use_me_first":
                d = new SoundDesign(1.0f, 0x3EF1u);
                d.Tone(0, 0.4f, Wave.Sine, N(84), N(60), 0.1f, 0.3f, 1.2f, vibratoHz: 9f, vibratoDepth: 0.04f);
                d.Noise(0, 0.4f, Filter.Band, 3500f, 900f, 0.2f, 1f, 0.3f, 1.3f);
                d.Notes(0.4f, 0.05f, 0.14f, Wave.Triangle, Pitches(79, 84, 88, 91), 0.08f, 0.005f);
                d.Thump(0.68f, 0.3f, 200f, 50f, 0.8f, 0.7f);
                d.Bell(0.7f, 0.3f, N(91), 2.01f, 0.6f, 0.09f, 3f);
                return Finish(d.S, 0.36f);
            case "use_heart_swap":
                d = new SoundDesign(1.0f, 0x4EA7u);
                foreach (float at in new[] { 0.0f, 0.15f, 0.3f, 0.45f, 0.6f, 0.75f })
                    d.Bell(at, 0.3f, N(79 + (int)(at * 12f) % 10), 2.01f, 0.6f, 0.09f, 3f);
                d.Notes(0.1f, 0.12f, 0.35f, Wave.Triangle, Pitches(84, 88, 91, 96, 100, 103, 108), 0.06f, 0.02f);
                d.Notes(0.85f, 0.05f, 0.14f, Wave.Sine, Pitches(96, 100, 103), 0.05f, 0.005f);
                return Finish(d.S, 0.32f);
            case "use_vacuum_wave":
                d = new SoundDesign(0.8f, 0xAC0Eu);
                for (int i = 0; i < 3; i++)
                    d.Noise(0.02f + i * 0.06f, 0.2f, Filter.Band, 600f + i * 400f, 3500f, 0.25f, 1.4f, 0.3f, 1.3f, 22f, 0.5f);
                d.Thump(0.34f, 0.35f, 200f, 40f, 1.0f, 0.9f);
                d.Noise(0.34f, 0.06f, Filter.Band, 1600f, 800f, 0.8f, 1f, 0.01f, 2.4f);
                d.Noise(0.4f, 0.3f, Filter.High, 3000f, 6000f, 0.15f, 1f, 0.01f, 1.5f);
                return Finish(d.S, 0.4f);
            case "use_switcheroo":
                d = new SoundDesign(1.0f, 0x5017u);
                d.Notes(0.02f, 0.07f, 0.14f, Wave.Triangle, Pitches(76, 79, 83, 88, 83, 79), 0.09f, 0.005f);
                foreach (float at in new[] { 0.55f, 0.65f, 0.75f, 0.85f, 0.92f })
                    d.Bell(at, 0.2f, N(88 + (int)(at * 10f) % 5), 2.01f, 0.7f, 0.1f, 3f);
                d.Noise(0.2f, 0.3f, Filter.High, 3000f, 6000f, 0.12f, 1f, 0.3f, 1.4f, 12f, 0.7f);
                return Finish(d.S, 0.34f);
            case "use_draco_meteor":
                d = new SoundDesign(1.0f, 0xD2A0u);
                d.Tone(0, 0.5f, Wave.Saw, N(34), N(46), 0.12f, 0.4f, 1.2f, vibratoHz: 6f, vibratoDepth: 0.06f);
                foreach (float at in new[] { 0.2f, 0.26f, 0.32f, 0.37f, 0.43f, 0.5f })
                {
                    d.Noise(at - 0.1f, 0.12f, Filter.Band, 3000f, 800f, 0.25f, 1f, 0.3f, 1.4f);
                    d.Thump(at, 0.3f, 170f, 36f, 0.7f, 0.7f);
                    d.Noise(at, 0.3f, Filter.Low, 1400f, 200f, 0.35f, 1f, 0.01f, 1.7f);
                }
                d.Thump(0.55f, 0.4f, 130f, 26f, 1.0f, 0.7f);
                return Finish(d.S, 0.46f);
            case "use_judgment":
                d = new SoundDesign(1.0f, 0x10D6u);
                d.Tone(0, 0.5f, Wave.Sine, N(60), N(72), 0.12f, 0.3f, 1.3f, vibratoHz: 5f, vibratoDepth: 0.02f);
                d.Notes(0.1f, 0.1f, 0.4f, Wave.Triangle, Pitches(72, 76, 79, 84, 88), 0.08f, 0.04f);
                d.Thump(0.34f, 0.55f, 180f, 28f, 1.0f, 0.8f);
                d.Bell(0.34f, 0.65f, N(72), 2.41f, 1.0f, 0.14f, 2.6f);
                d.Noise(0.4f, 0.5f, Filter.High, 3000f, 7500f, 0.2f, 0.9f, 0.02f, 1.5f);
                return Finish(d.S, 0.44f);
            case "use_roar_of_time":
                d = new SoundDesign(1.0f, 0x2070u);
                d.Tone(0, 0.9f, Wave.Saw, N(33), N(36), 0.14f, 0.2f, 1.2f, vibratoHz: 4f, vibratoDepth: 0.06f);
                for (int i = 0; i < 6; i++) d.Bell(0.05f + i * 0.1f, 0.3f, N(70), 1.41f, 0.8f, 0.07f, 3f);
                d.Noise(0.2f, 0.4f, Filter.Band, 400f, 2800f, 0.4f, 1f, 0.4f, 1.2f, 18f, 0.5f);
                d.Thump(0.55f, 0.45f, 150f, 24f, 1.0f, 1.0f);
                d.Noise(0.55f, 0.07f, Filter.Band, 1500f, 700f, 1.0f, 1f, 0.01f, 2.4f);
                d.Bell(0.57f, 0.4f, N(46), 1.41f, 1.0f, 0.12f, 2.6f);
                return Finish(d.S, 0.48f);
            case "use_spacial_rend":
                d = new SoundDesign(1.0f, 0x5BEEu);
                d.Tone(0, 0.5f, Wave.Sine, N(60), N(84), 0.12f, 0.4f, 1.2f, vibratoHz: 8f, vibratoDepth: 0.04f);
                d.Noise(0.28f, 0.1f, Filter.High, 3000f, 9000f, 0.6f, 1.4f, 0.01f, 2.4f);
                d.Thump(0.35f, 0.5f, 170f, 28f, 1.0f, 0.8f);
                d.Tone(0.38f, 0.55f, Wave.Sine, N(84), N(40), 0.1f, 0.02f, 1.4f, vibratoHz: 12f, vibratoDepth: 0.08f);
                d.Crackle(0.4f, 0.4f, 12, 4000f, 0.2f);
                d.Bell(0.4f, 0.5f, N(98), 3.01f, 1.0f, 0.1f, 2.8f);
                return Finish(d.S, 0.46f);
            case "use_lunar_dance":
                d = new SoundDesign(1.0f, 0x10DAu);
                d.Notes(0, 0.12f, 0.38f, Wave.Triangle, Pitches(72, 76, 79, 76, 79, 84, 79, 84), 0.09f, 0.03f);
                d.Tone(0, 0.95f, Wave.Sine, N(60), N(72), 0.07f, 0.3f, 1.3f, vibratoHz: 5f, vibratoDepth: 0.03f);
                d.Bell(0.6f, 0.4f, N(100), 2.01f, 0.5f, 0.09f, 3f);
                d.Notes(0.7f, 0.07f, 0.2f, Wave.Sine, Pitches(103, 107, 112), 0.05f, 0.01f);
                return Finish(d.S, 0.32f);
            case "use_crush_grip":
                d = new SoundDesign(1.0f, 0xC2B1u);
                d.Noise(0, 0.3f, Filter.Band, 400f, 2200f, 0.25f, 1.2f, 0.5f, 1.3f);
                d.Thump(0.33f, 0.4f, 170f, 30f, 1.0f, 0.9f);
                d.Tone(0.35f, 0.5f, Wave.Saw, N(46), N(36), 0.1f, 0.05f, 1.5f, vibratoHz: 20f, vibratoDepth: 0.05f);
                d.Crackle(0.36f, 0.4f, 12, 1500f, 0.3f);
                d.Thump(0.55f, 0.35f, 150f, 36f, 0.8f, 0.6f);
                d.Noise(0.55f, 0.06f, Filter.Band, 1500f, 700f, 0.6f, 1f, 0.01f, 2.4f);
                return Finish(d.S, 0.46f);
            case "use_magma_storm":
                d = new SoundDesign(1.0f, 0x3A62u);
                d.Tone(0, 0.9f, Wave.Saw, N(36), N(43), 0.13f, 0.2f, 1.2f, vibratoHz: 7f, vibratoDepth: 0.06f);
                d.Noise(0, 0.95f, Filter.Band, 400f, 2200f, 0.4f, 1.1f, 0.3f, 1.1f, 18f, 0.6f);
                d.Crackle(0.1f, 0.8f, 28, 1700f, 0.3f);
                d.Thump(0.34f, 0.5f, 140f, 28f, 0.9f, 0.6f);
                d.Bubbles(0.5f, 0.4f, 6, 100f, 300f, 0.3f);
                return Finish(d.S, 0.46f);
            case "use_dark_void":
                d = new SoundDesign(1.0f, 0xDA7Fu);
                d.Tone(0, 0.95f, Wave.Sine, N(40), N(33), 0.16f, 0.3f, 1.2f, vibratoHz: 4f, vibratoDepth: 0.08f);
                d.Noise(0.1f, 0.8f, Filter.Band, 300f, 900f, 0.2f, 1.6f, 0.4f, 1.1f, 6f, 0.7f);
                d.Notes(0.2f, 0.18f, 0.45f, Wave.Triangle, Pitches(64, 62, 59, 57, 55), 0.07f, 0.05f);
                d.Thump(0.4f, 0.4f, 100f, 28f, 0.7f, 0.3f);
                d.Bell(0.45f, 0.5f, N(52), 1.41f, 0.8f, 0.09f, 2.4f);
                return Finish(d.S, 0.36f);
            case "use_seed_flare":
                d = new SoundDesign(1.0f, 0x5EF1u);
                d.Tone(0, 0.4f, Wave.Sine, N(67), N(91), 0.12f, 0.4f, 1.2f, vibratoHz: 8f, vibratoDepth: 0.04f);
                d.Notes(0.05f, 0.06f, 0.2f, Wave.Triangle, Pitches(84, 88, 91, 96, 100, 103), 0.07f, 0.005f);
                d.Noise(0.28f, 0.2f, Filter.High, 3000f, 9000f, 0.5f, 1f, 0.02f, 2f);
                d.Thump(0.36f, 0.55f, 170f, 30f, 1.0f, 0.9f);
                d.Bell(0.38f, 0.55f, N(91), 3.01f, 1.0f, 0.12f, 2.8f);
                d.Notes(0.45f, 0.05f, 0.15f, Wave.Sine, Pitches(103, 108, 112), 0.05f, 0.005f);
                return Finish(d.S, 0.44f);
            case "use_shadow_force":
                d = new SoundDesign(1.0f, 0x5F0Cu);
                d.Tone(0, 0.35f, Wave.Saw, N(52), N(38), 0.12f, 0.1f, 1.5f, vibratoHz: 14f, vibratoDepth: 0.08f);
                d.Noise(0.02f, 0.3f, Filter.Band, 1500f, 400f, 0.3f, 1.2f, 0.2f, 1.3f);
                d.Noise(0.28f, 0.1f, Filter.High, 3000f, 6500f, 0.25f, 1f, 0.2f, 1.6f);
                d.Thump(0.35f, 0.5f, 190f, 30f, 1.0f, 0.9f);
                d.Noise(0.35f, 0.07f, Filter.Band, 1700f, 800f, 0.9f, 1f, 0.01f, 2.4f);
                d.Tone(0.4f, 0.5f, Wave.Saw, N(40), N(34), 0.09f, 0.05f, 1.5f, vibratoHz: 9f, vibratoDepth: 0.06f);
                d.Crackle(0.45f, 0.35f, 8, 1200f, 0.25f);
                return Finish(d.S, 0.46f);
        }
        return null;
    }
}
