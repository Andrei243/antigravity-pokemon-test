using static PokemonPlatinumEngine.Audio.SoundDesign;

namespace PokemonPlatinumEngine.Audio;

// Plan 25 - Y10, its second part (Y10b), first half: the sounds of the moves the story shows nowhere, from Comet Punch
// to Arm Thrust. As the batches before: each move's beats are the original's (when it rings, how often, what it is
// made of, read from its animation script), its sound our own, built from the bank's shared layers.
public static partial class SoundBank
{
    /// <summary>A sound of Y10's second part, first half, or the next half's, or null for a name that isn't one.</summary>
    private static float[]? MakeMoveSoundY10b(string name)
    {
        SoundDesign d;
        switch (name)
        {
            case "use_comet_punch":
                d = new SoundDesign(0.8f, 0xC0E1u);
                for (int i = 0; i < 4; i++)
                {
                    float at = 0.1f + i * 0.1f;
                    d.Noise(at - 0.05f, 0.07f, Filter.Band, 600f, 2600f, 0.2f, 1.2f, 0.4f, 1.3f);
                    d.Thump(at, 0.16f, 220f - i * 15f, 60f, 0.8f, 0.8f);
                }
                d.Bell(0.4f, 0.3f, N(86), 2.41f, 0.7f, 0.07f, 3f);
                return Finish(d.S, 0.42f);
            case "use_pay_day":
                d = new SoundDesign(0.9f, 0xDA7Du);
                d.Noise(0, 0.25f, Filter.Band, 800f, 3000f, 0.25f, 1.2f, 0.3f, 1.3f);
                for (int i = 0; i < 6; i++)
                    d.Bell(0.05f + i * 0.04f, 0.2f, N(91 + (i % 3) * 3), 3.4f, 0.8f, 0.07f, 3.4f);
                d.Thump(0.33f, 0.25f, 260f, 70f, 0.8f, 0.7f);
                for (int i = 0; i < 6; i++)
                    d.Bell(0.38f + i * 0.06f, 0.25f, N(95 + (i * 2) % 7), 3.4f, 0.9f, 0.08f, 3.2f);
                return Finish(d.S, 0.4f);
            case "use_vise_grip":
                d = new SoundDesign(0.8f, 0x715Eu);
                d.Noise(0, 0.28f, Filter.Band, 500f, 2400f, 0.2f, 1.2f, 0.4f, 1.3f);
                d.Thump(0.33f, 0.3f, 600f, 150f, 0.9f, 0.9f);
                d.Noise(0.33f, 0.05f, Filter.High, 3500f, 2200f, 0.8f, 1.2f, 0.01f, 2.4f);
                d.Tone(0.36f, 0.35f, Wave.Saw, N(52), N(50), 0.08f, 0.1f, 1.6f, vibratoHz: 24f, vibratoDepth: 0.04f);
                return Finish(d.S, 0.4f);
            case "use_rolling_kick":
                d = new SoundDesign(0.8f, 0x401Cu);
                d.Noise(0, 0.32f, Filter.Band, 400f, 3200f, 0.35f, 1.2f, 0.5f, 1.2f, 22f, 0.5f);
                d.Thump(0.33f, 0.4f, 210f, 40f, 1.0f, 0.9f);
                d.Noise(0.33f, 0.07f, Filter.Band, 1600f, 800f, 0.9f, 1f, 0.01f, 2.4f);
                d.Noise(0.4f, 0.3f, Filter.Low, 900f, 150f, 0.25f, 1f, 0.01f, 1.5f);
                return Finish(d.S, 0.42f);
            case "use_twineedle":
                d = new SoundDesign(0.8f, 0x7E1Eu);
                foreach (float at in new[] { 0.02f, 0.14f })
                {
                    d.Noise(at, 0.12f, Filter.High, 3000f, 7000f, 0.25f, 1f, 0.2f, 1.6f);
                    d.Tone(at, 0.12f, Wave.Sine, 1500f, 3000f, 0.05f, 0.1f, 2f);
                    d.Thump(at + 0.14f, 0.1f, 420f, 150f, 0.5f, 0.8f);
                }
                d.Notes(0.3f, 0.06f, 0.2f, Wave.Triangle, Pitches(70, 68, 66), 0.07f, 0.01f);
                d.Bubbles(0.35f, 0.3f, 4, 200f, 500f, 0.2f);
                return Finish(d.S, 0.36f);
            case "use_sleep_powder":
                d = new SoundDesign(1.0f, 0x51EEu);
                d.Noise(0, 0.9f, Filter.Band, 2200f, 4800f, 0.15f, 0.8f, 0.3f, 1.1f, 6f, 0.5f);
                d.Notes(0.15f, 0.2f, 0.45f, Wave.Sine, Pitches(79, 76, 72, 67), 0.09f, 0.05f);
                d.Tone(0.5f, 0.45f, Wave.Triangle, N(55), N(52), 0.07f, 0.2f, 1.4f, vibratoHz: 4f, vibratoDepth: 0.05f);
                return Finish(d.S, 0.32f);
            case "use_sludge":
                d = new SoundDesign(0.9f, 0x5100u);
                d.Noise(0, 0.25f, Filter.Band, 300f, 1300f, 0.3f, 1.3f, 0.1f, 1.4f);
                d.Bubbles(0.02f, 0.3f, 5, 150f, 450f, 0.3f);
                d.Thump(0.34f, 0.25f, 180f, 55f, 0.8f, 0.4f);
                d.Noise(0.36f, 0.4f, Filter.Band, 400f, 1200f, 0.4f, 1f, 0.01f, 1.6f, 18f, 0.6f);
                d.Bubbles(0.4f, 0.4f, 8, 120f, 400f, 0.3f);
                return Finish(d.S, 0.4f);
            case "use_bone_club":
                d = new SoundDesign(0.8f, 0xB0C1u);
                d.Noise(0, 0.25f, Filter.Band, 600f, 2800f, 0.3f, 1.2f, 0.5f, 1.3f);
                d.Thump(0.3f, 0.35f, 320f, 90f, 1.0f, 0.9f);
                d.Tone(0.3f, 0.3f, Wave.Triangle, 520f, 380f, 0.1f, 0.01f, 3f);
                d.Noise(0.3f, 0.06f, Filter.High, 3000f, 1800f, 0.7f, 1f, 0.01f, 2.4f);
                d.Thump(0.4f, 0.2f, 140f, 60f, 0.5f, 0.4f);
                return Finish(d.S, 0.42f);
            case "use_spike_cannon":
                d = new SoundDesign(0.8f, 0x591Cu);
                for (int i = 0; i < 12; i++)
                {
                    float at = 0.02f + i * 0.02f;
                    d.Noise(at, 0.04f, Filter.Band, 2500f, 5000f, 0.3f, 1.5f, 0.05f, 2.4f);
                }
                d.Thump(0.02f, 0.25f, 200f, 70f, 0.7f, 0.4f);
                for (int i = 0; i < 6; i++) d.Noise(0.2f + i * 0.025f, 0.04f, Filter.High, 4500f, 3000f, 0.35f, 1.2f, 0.02f, 2.4f);
                d.Thump(0.36f, 0.25f, 180f, 50f, 0.7f, 0.5f);
                return Finish(d.S, 0.4f);
            case "use_barrage":
                d = new SoundDesign(0.9f, 0xBA44u);
                for (int i = 0; i < 5; i++)
                {
                    float at = 0.0f + i * 0.07f;
                    d.Tone(at, 0.1f, Wave.Triangle, 500f, 260f, 0.07f, 0.05f, 2f);
                    d.Thump(at + 0.2f, 0.18f, 200f - i * 10f, 50f, 0.65f, 0.7f);
                    d.Noise(at + 0.2f, 0.12f, Filter.Low, 1200f, 300f, 0.3f, 1f, 0.01f, 2f);
                }
                return Finish(d.S, 0.44f);
            case "use_lovely_kiss":
                d = new SoundDesign(1.0f, 0x10E1u);
                d.Notes(0, 0.06f, 0.12f, Wave.Sine, Pitches(84, 88, 91, 96), 0.1f, 0.01f);
                d.Tone(0.3f, 0.12f, Wave.Sine, 900f, 500f, 0.14f, 0.02f, 2f);
                d.Noise(0.34f, 0.04f, Filter.High, 3000f, 5000f, 0.35f, 1f, 0.05f, 2.4f);
                d.Notes(0.5f, 0.15f, 0.4f, Wave.Triangle, Pitches(79, 76, 72), 0.08f, 0.04f);
                d.Bell(0.45f, 0.4f, N(103), 2.01f, 0.4f, 0.07f, 3f);
                return Finish(d.S, 0.32f);
            case "use_sky_attack":
                d = new SoundDesign(1.0f, 0x5CA7u);
                d.Tone(0, 0.3f, Wave.Saw, 160f, 700f, 0.1f, 0.4f, 1.2f, vibratoHz: 18f, vibratoDepth: 0.05f);
                d.Noise(0, 0.32f, Filter.Band, 600f, 4000f, 0.35f, 1.2f, 0.5f, 1.1f, 25f, 0.5f);
                d.Thump(0.34f, 0.5f, 200f, 32f, 1.0f, 1.0f);
                d.Noise(0.34f, 0.07f, Filter.Band, 1700f, 800f, 1.0f, 1f, 0.01f, 2.4f);
                d.Noise(0.4f, 0.55f, Filter.High, 3000f, 6500f, 0.2f, 1f, 0.02f, 1.5f, 40f, 0.6f);
                d.Bell(0.36f, 0.5f, N(91), 2.01f, 0.8f, 0.1f, 2.8f);
                return Finish(d.S, 0.46f);
            case "use_transform":
                d = new SoundDesign(1.0f, 0x7F0Au);
                d.Tone(0, 0.5f, Wave.Pulse, 300f, 1200f, 0.06f, 0.2f, 1.3f, 0.25f, vibratoHz: 20f, vibratoDepth: 0.1f);
                d.Crackle(0.05f, 0.45f, 16, 3000f, 0.25f);
                d.Tone(0.5f, 0.4f, Wave.Pulse, 1200f, 400f, 0.06f, 0.1f, 1.5f, 0.25f, vibratoHz: 20f, vibratoDepth: 0.1f);
                d.Bell(0.55f, 0.35f, N(88), 2.01f, 0.5f, 0.09f, 3f);
                d.Notes(0.6f, 0.05f, 0.14f, Wave.Sine, Pitches(84, 88, 91), 0.06f, 0.005f);
                return Finish(d.S, 0.34f);
            case "use_bonemerang":
                d = new SoundDesign(1.0f, 0xB0E7u);
                d.Noise(0.05f, 0.3f, Filter.Band, 500f, 2000f, 0.25f, 1.4f, 0.3f, 1.3f, 12f, 0.5f);
                d.Thump(0.33f, 0.22f, 300f, 90f, 0.9f, 0.9f);
                d.Noise(0.4f, 0.3f, Filter.Band, 2000f, 600f, 0.25f, 1.4f, 0.1f, 1.3f, 12f, 0.5f);
                d.Thump(0.68f, 0.25f, 280f, 80f, 1.0f, 0.9f);
                d.Noise(0.68f, 0.05f, Filter.High, 3000f, 1800f, 0.6f, 1f, 0.01f, 2.4f);
                return Finish(d.S, 0.42f);
            case "use_sharpen":
                d = new SoundDesign(0.8f, 0x54A9u);
                d.Noise(0, 0.5f, Filter.High, 3500f, 8000f, 0.2f, 1f, 0.2f, 1.3f, 14f, 0.6f);
                d.Notes(0.1f, 0.08f, 0.2f, Wave.Triangle, Pitches(88, 91, 95, 100, 107), 0.08f, 0.005f);
                d.Bell(0.55f, 0.25f, N(110), 3.01f, 0.5f, 0.09f, 3.4f);
                return Finish(d.S, 0.32f);
            case "use_conversion":
                d = new SoundDesign(1.0f, 0xC0A7u);
                for (int i = 0; i < 4; i++)
                    d.Tone(i * 0.18f, 0.2f, Wave.Triangle, N(60 + i * 4), N(64 + i * 4), 0.09f, 0.1f, 1.8f, vibratoHz: 12f, vibratoDepth: 0.03f);
                d.Notes(0.7f, 0.07f, 0.2f, Wave.Sine, Pitches(84, 88, 91, 96), 0.08f, 0.01f);
                d.Bell(0.78f, 0.22f, N(96), 2.01f, 0.5f, 0.09f, 3f);
                return Finish(d.S, 0.32f);
            case "use_struggle":
                d = new SoundDesign(0.8f, 0x5712u);
                for (int i = 0; i < 4; i++) d.Thump(0.02f + i * 0.07f, 0.1f, 280f, 130f, 0.3f, 0.4f);
                d.Noise(0.12f, 0.2f, Filter.Band, 500f, 1800f, 0.2f, 1.2f, 0.4f, 1.3f);
                d.Thump(0.34f, 0.3f, 190f, 60f, 0.7f, 0.7f);
                d.Thump(0.45f, 0.25f, 150f, 50f, 0.4f, 0.4f);
                return Finish(d.S, 0.38f);
            case "use_sketch":
                d = new SoundDesign(1.0f, 0x5CE7u);
                for (int i = 0; i < 12; i++)
                    d.Noise(0.05f + i * 0.04f, 0.05f, Filter.Band, 2500f + (i % 3) * 600f, 4200f, 0.2f, 1.5f, 0.1f, 2f);
                d.Notes(0.5f, 0.06f, 0.15f, Wave.Triangle, Pitches(84, 88, 91), 0.06f, 0.005f);
                d.Bell(0.62f, 0.3f, N(96), 2.01f, 0.6f, 0.09f, 3f);
                return Finish(d.S, 0.32f);
            case "use_spider_web":
                d = new SoundDesign(1.0f, 0x59DEu);
                foreach (float at in new[] { 0.0f, 0.3f })
                {
                    d.Noise(at, 0.25f, Filter.High, 3500f, 6500f, 0.2f, 1f, 0.3f, 1.5f);
                    d.Thump(at + 0.2f, 0.2f, 300f, 120f, 0.4f, 0.4f);
                    d.Tone(at + 0.22f, 0.4f, Wave.Triangle, 1800f, 1400f, 0.05f, 0.05f, 2.5f, vibratoHz: 30f, vibratoDepth: 0.03f);
                }
                d.Crackle(0.5f, 0.4f, 8, 3500f, 0.2f);
                return Finish(d.S, 0.32f);
            case "use_nightmare":
                d = new SoundDesign(1.0f, 0x41E7u);
                d.Tone(0, 0.9f, Wave.Saw, N(38), N(34), 0.12f, 0.2f, 1.2f, vibratoHz: 5f, vibratoDepth: 0.07f);
                d.Noise(0.1f, 0.8f, Filter.Band, 400f, 1100f, 0.25f, 1.6f, 0.3f, 1.1f, 7f, 0.7f);
                d.Notes(0.3f, 0.15f, 0.35f, Wave.Triangle, Pitches(63, 62, 61, 58), 0.08f, 0.04f);
                d.Thump(0.55f, 0.3f, 100f, 36f, 0.6f, 0.3f);
                return Finish(d.S, 0.36f);
            case "use_aeroblast":
                d = new SoundDesign(1.0f, 0xAE40u);
                d.Noise(0, 0.5f, Filter.Band, 700f, 3500f, 0.4f, 0.9f, 0.3f, 1.1f, 28f, 0.5f);
                d.Tone(0.05f, 0.45f, Wave.Saw, 180f, 520f, 0.08f, 0.4f, 1.3f);
                d.Noise(0.38f, 0.5f, Filter.High, 2500f, 7000f, 0.35f, 0.8f, 0.01f, 1.5f, 40f, 0.5f);
                d.Thump(0.36f, 0.4f, 190f, 32f, 1.0f, 0.8f);
                d.Bell(0.38f, 0.5f, N(86), 2.41f, 0.8f, 0.1f, 2.8f);
                return Finish(d.S, 0.46f);
            case "use_powder_snow":
                d = new SoundDesign(1.0f, 0x5A0Eu);
                d.Noise(0, 0.85f, Filter.Band, 3000f, 6000f, 0.2f, 0.7f, 0.2f, 1.1f, 9f, 0.5f);
                d.Noise(0.05f, 0.8f, Filter.High, 4500f, 8000f, 0.1f, 1f, 0.3f, 1.2f);
                d.Notes(0.3f, 0.12f, 0.25f, Wave.Sine, Pitches(95, 98, 103, 107), 0.06f, 0.01f);
                d.Bell(0.8f, 0.2f, N(103), 3.01f, 0.4f, 0.07f, 3.2f);
                return Finish(d.S, 0.32f);
            case "use_zap_cannon":
                d = new SoundDesign(1.0f, 0x2A9Cu);
                d.Tone(0, 0.3f, Wave.Saw, 150f, 900f, 0.1f, 0.4f, 1.3f, vibratoHz: 30f, vibratoDepth: 0.1f);
                d.Crackle(0.02f, 0.3f, 14, 3200f, 0.3f);
                d.Thump(0.3f, 0.4f, 170f, 36f, 1.0f, 0.9f);
                d.Noise(0.3f, 0.07f, Filter.Band, 1800f, 900f, 0.9f, 1f, 0.01f, 2.4f);
                d.Noise(0.35f, 0.55f, Filter.Band, 1200f, 3500f, 0.3f, 1.2f, 0.01f, 1.5f, 50f, 0.7f);
                d.Crackle(0.38f, 0.5f, 16, 3000f, 0.3f);
                return Finish(d.S, 0.44f);
            case "use_milk_drink":
                d = new SoundDesign(1.0f, 0x31E0u);
                d.Bubbles(0, 0.35f, 7, 250f, 700f, 0.25f);
                d.Noise(0.05f, 0.3f, Filter.Band, 400f, 1200f, 0.2f, 1f, 0.2f, 1.3f, 14f, 0.5f);
                d.Tone(0.4f, 0.1f, Wave.Sine, 180f, 100f, 0.15f, 0.02f, 2f);
                d.Notes(0.5f, 0.09f, 0.35f, Wave.Sine, Pitches(79, 83, 86, 91), 0.1f, 0.02f);
                d.Bell(0.8f, 0.2f, N(96), 2.01f, 0.4f, 0.08f, 3f);
                return Finish(d.S, 0.32f);
            case "use_sacred_fire":
                d = new SoundDesign(1.0f, 0x5AC0u);
                d.Tone(0, 0.5f, Wave.Triangle, N(60), N(67), 0.1f, 0.2f, 1.3f, vibratoHz: 6f, vibratoDepth: 0.03f);
                d.Noise(0, 0.9f, Filter.Band, 500f, 2400f, 0.3f, 1.1f, 0.3f, 1.2f, 20f, 0.5f);
                d.Notes(0.1f, 0.1f, 0.35f, Wave.Sine, Pitches(79, 84, 88, 91), 0.08f, 0.02f);
                d.Thump(0.5f, 0.3f, 120f, 40f, 0.9f, 0.4f);
                d.Crackle(0.5f, 0.45f, 16, 1800f, 0.3f);
                d.Bell(0.55f, 0.4f, N(86), 2.41f, 0.7f, 0.1f, 2.8f);
                return Finish(d.S, 0.4f);
            case "use_morning_sun":
                d = new SoundDesign(1.0f, 0x305Eu);
                d.Tone(0, 0.8f, Wave.Sine, N(60), N(72), 0.13f, 0.3f, 1.3f, vibratoHz: 5f, vibratoDepth: 0.03f);
                d.Notes(0.1f, 0.13f, 0.4f, Wave.Triangle, Pitches(79, 84, 88, 91, 96), 0.1f, 0.03f);
                d.Bell(0.65f, 0.35f, N(100), 2.01f, 0.5f, 0.09f, 3f);
                d.Notes(0.7f, 0.07f, 0.2f, Wave.Sine, Pitches(103, 108), 0.05f, 0.01f);
                return Finish(d.S, 0.32f);
            case "use_flatter":
                d = new SoundDesign(1.0f, 0xF1A7u);
                d.Notes(0, 0.1f, 0.22f, Wave.Triangle, Pitches(84, 88, 91, 96, 91, 96, 100), 0.1f, 0.01f);
                d.Noise(0.05f, 0.6f, Filter.High, 3500f, 7000f, 0.1f, 1f, 0.2f, 1.2f, 20f, 0.7f);
                d.Tone(0.5f, 0.45f, Wave.Sine, N(76), N(70), 0.1f, 0.1f, 1.5f, vibratoHz: 10f, vibratoDepth: 0.1f);
                d.Bell(0.78f, 0.2f, N(100), 2.01f, 0.4f, 0.07f, 3f);
                return Finish(d.S, 0.32f);
            case "use_smelling_salts":
                d = new SoundDesign(0.9f, 0x5A17u);
                d.Noise(0, 0.3f, Filter.Band, 1500f, 3500f, 0.15f, 1f, 0.3f, 1.3f, 10f, 0.5f);
                d.Tone(0.05f, 0.25f, Wave.Sine, 700f, 1000f, 0.05f, 0.3f, 1.5f, vibratoHz: 12f, vibratoDepth: 0.05f);
                d.Crackle(0.3f, 0.1f, 5, 3000f, 0.35f);
                d.Thump(0.35f, 0.2f, 240f, 70f, 0.8f, 0.8f);
                d.Notes(0.4f, 0.04f, 0.1f, Wave.Pulse, Pitches(88, 95, 100), 0.07f, 0.005f);
                return Finish(d.S, 0.38f);
            case "use_nature_power":
                d = new SoundDesign(1.0f, 0x4A70u);
                d.Noise(0, 0.4f, Filter.Band, 600f, 3000f, 0.3f, 1.1f, 0.4f, 1.2f, 16f, 0.5f);
                d.Tone(0, 0.4f, Wave.Triangle, N(55), N(67), 0.09f, 0.3f, 1.3f);
                d.Notes(0.1f, 0.07f, 0.2f, Wave.Sine, Pitches(79, 83, 86, 91), 0.07f, 0.01f);
                d.Thump(0.35f, 0.35f, 190f, 40f, 0.9f, 0.8f);
                d.Noise(0.35f, 0.4f, Filter.Band, 400f, 2200f, 0.3f, 1f, 0.01f, 1.5f, 24f, 0.5f);
                d.Bell(0.38f, 0.35f, N(79), 2.01f, 0.6f, 0.08f, 3f);
                return Finish(d.S, 0.4f);
            case "use_role_play":
                d = new SoundDesign(1.0f, 0x401Eu);
                d.Tone(0, 0.45f, Wave.Sine, N(64), N(72), 0.11f, 0.2f, 1.4f, vibratoHz: 7f, vibratoDepth: 0.05f);
                d.Notes(0.1f, 0.08f, 0.2f, Wave.Triangle, Pitches(76, 79, 83, 79, 83, 88), 0.07f, 0.01f);
                d.Tone(0.45f, 0.4f, Wave.Sine, N(72), N(60), 0.1f, 0.1f, 1.4f, vibratoHz: 7f, vibratoDepth: 0.05f);
                d.Bell(0.5f, 0.35f, N(91), 2.01f, 0.6f, 0.09f, 3f);
                return Finish(d.S, 0.32f);
            case "use_magic_coat":
                d = new SoundDesign(0.9f, 0x3A60u);
                d.Notes(0, 0.05f, 0.14f, Wave.Sine, Pitches(84, 88, 91, 96, 100, 103, 108, 112), 0.08f, 0.005f);
                d.Tone(0.1f, 0.5f, Wave.Triangle, N(72), N(79), 0.06f, 0.2f, 1.5f, vibratoHz: 14f, vibratoDepth: 0.05f);
                d.Bell(0.45f, 0.35f, N(103), 2.01f, 0.7f, 0.1f, 3f);
                d.Noise(0.4f, 0.3f, Filter.High, 4000f, 8000f, 0.1f, 1f, 0.1f, 1.5f);
                return Finish(d.S, 0.32f);
            case "use_arm_thrust":
                d = new SoundDesign(0.8f, 0xA27Au);
                for (int i = 0; i < 5; i++)
                {
                    float at = 0.08f + i * 0.055f;
                    d.Noise(at - 0.03f, 0.05f, Filter.Band, 600f, 2400f, 0.15f, 1.2f, 0.4f, 1.3f);
                    d.Thump(at, 0.1f, 260f - i * 12f, 100f, 0.5f + i * 0.08f, 0.6f);
                }
                d.Thump(0.36f, 0.25f, 200f, 60f, 0.7f, 0.6f);
                return Finish(d.S, 0.4f);
        }
        return MakeMoveSoundY10c(name);
    }
}
