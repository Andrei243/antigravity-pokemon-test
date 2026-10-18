using System;
using static PokemonPlatinumEngine.Audio.SoundDesign;

namespace PokemonPlatinumEngine.Audio;

// Plan 25 - Y12: the sounds of the later games' moves, Generation 6 from Flying Press to Hyperspace Fury. No game's
// sound is the model: each is our own, built from the bank's shared layers and timed to its look (the blow at 0.35 s).
// Every one runs to at most a second.
public static partial class SoundBank
{
    /// <summary>A sound of Y12, or null for a name that isn't one.</summary>
    private static float[]? MakeMoveSoundY12(string name)
    {
        SoundDesign d;
        switch (name)
        {
            case "use_flying_press":
                d = new SoundDesign(0.95f, 0xF1B0u);
                d.Noise(0, 0.3f, Filter.High, 2000f, 5500f, 0.4f, 1f, 0.2f, 1.2f, 22f, 0.5f);
                d.Tone(0, 0.34f, Wave.Sine, N(84), N(60), 0.09f, 0.1f, 1.3f);
                d.Thump(0.34f, 0.45f, 180f, 30f, 1.0f, 0.9f);
                d.Noise(0.36f, 0.4f, Filter.High, 3000f, 7000f, 0.3f, 1f, 0.01f, 1.6f, 30f, 0.6f);
                d.Notes(0.5f, 0.06f, 0.14f, Wave.Triangle, Pitches(84, 88, 91), 0.05f, 0.01f);
                return Finish(d.S, 0.44f);
            case "use_mat_block":
                d = new SoundDesign(0.85f, 0x3A7Bu);
                d.Noise(0.02f, 0.2f, Filter.Band, 500f, 1800f, 0.5f, 1f, 0.1f, 1.4f);
                d.Thump(0.2f, 0.3f, 150f, 50f, 0.8f, 0.7f);
                d.Thump(0.28f, 0.2f, 190f, 70f, 0.5f, 0.5f);
                d.Bell(0.34f, 0.5f, N(74), 1.41f, 0.9f, 0.1f, 2.5f);
                d.Notes(0.45f, 0.07f, 0.14f, Wave.Triangle, Pitches(67, 71, 74), 0.05f, 0.01f);
                return Finish(d.S, 0.4f);
            case "use_belch":
                d = new SoundDesign(1.0f, 0xBE1Cu);
                d.Tone(0, 0.5f, Wave.Saw, N(33), N(28), 0.16f, 0.15f, 1.0f, vibratoHz: 24f, vibratoDepth: 0.12f);
                d.Noise(0.05f, 0.5f, Filter.Band, 300f, 900f, 0.5f, 1.3f, 0.2f, 1.1f, 28f, 0.6f);
                d.Bubbles(0.3f, 0.5f, 8, 180f, 520f, 0.35f);
                d.Thump(0.35f, 0.35f, 120f, 40f, 0.8f, 0.5f);
                d.Noise(0.4f, 0.5f, Filter.Low, 1800f, 400f, 0.3f, 0.9f, 0.05f, 1.6f);
                return Finish(d.S, 0.44f);
            case "use_rototiller":
                d = new SoundDesign(1.0f, 0x2070u);
                d.Tone(0, 0.8f, Wave.Saw, N(38), N(40), 0.1f, 0.1f, 0.8f, vibratoHz: 26f, vibratoDepth: 0.09f);
                d.Noise(0, 0.8f, Filter.Band, 600f, 1500f, 0.4f, 1.2f, 0.2f, 0.9f, 32f, 0.7f);
                for (int i = 0; i < 6; i++) d.Thump(0.1f + i * 0.1f, 0.08f, 190f, 90f, 0.4f, 0.4f);
                d.Notes(0.7f, 0.06f, 0.16f, Wave.Triangle, Pitches(72, 76, 79), 0.05f, 0.01f);
                return Finish(d.S, 0.4f);
            case "use_sticky_web":
                d = new SoundDesign(0.9f, 0x57EBu);
                d.Noise(0, 0.35f, Filter.High, 3500f, 6500f, 0.3f, 1f, 0.1f, 1.2f, 40f, 0.5f);
                d.Tone(0.1f, 0.5f, Wave.Sine, N(84), N(76), 0.07f, 0.1f, 1.3f, vibratoHz: 12f, vibratoDepth: 0.03f);
                d.Thump(0.33f, 0.2f, 200f, 90f, 0.4f, 0.3f);
                d.Notes(0.4f, 0.05f, 0.18f, Wave.Sine, Pitches(91, 96, 100, 103), 0.04f, 0.005f);
                d.Noise(0.4f, 0.4f, Filter.Band, 2800f, 1500f, 0.25f, 1f, 0.05f, 1.4f, 30f, 0.6f);
                return Finish(d.S, 0.36f);
            case "use_fell_stinger":
                d = new SoundDesign(0.85f, 0xFE11u);
                d.Noise(0.05f, 0.1f, Filter.High, 4500f, 8000f, 0.5f, 1.4f, 0.1f, 1.8f);
                d.Thump(0.17f, 0.15f, 240f, 90f, 0.6f, 0.6f);
                d.Noise(0.2f, 0.1f, Filter.High, 5000f, 9000f, 0.5f, 1.4f, 0.1f, 1.8f);
                d.Thump(0.33f, 0.25f, 220f, 60f, 0.9f, 0.8f);
                d.Notes(0.45f, 0.06f, 0.15f, Wave.Pulse, Pitches(76, 79, 84), 0.05f, 0.01f);
                return Finish(d.S, 0.4f);
            case "use_phantom_force":
                d = new SoundDesign(1.0f, 0x9A47u);
                d.Tone(0, 0.5f, Wave.Sine, N(62), N(45), 0.14f, 0.15f, 1.0f, vibratoHz: 6f, vibratoDepth: 0.07f);
                d.Noise(0.05f, 0.3f, Filter.Band, 1500f, 400f, 0.4f, 1.3f, 0.2f, 1.2f);
                d.Thump(0.34f, 0.5f, 180f, 28f, 1.0f, 0.9f);
                d.Noise(0.36f, 0.3f, Filter.High, 3000f, 6500f, 0.3f, 1.2f, 0.01f, 1.8f);
                d.Tone(0.4f, 0.5f, Wave.Saw, N(40), N(34), 0.09f, 0.05f, 1.5f, vibratoHz: 9f, vibratoDepth: 0.06f);
                return Finish(d.S, 0.44f);
            case "use_trick_or_treat":
                d = new SoundDesign(1.0f, 0x7071u);
                d.Notes(0, 0.1f, 0.22f, Wave.Triangle, Pitches(64, 62, 64, 67, 64, 71), 0.07f, 0.01f);
                d.Tone(0.3f, 0.6f, Wave.Sine, N(55), N(50), 0.1f, 0.2f, 1.2f, vibratoHz: 6f, vibratoDepth: 0.08f);
                d.Bell(0.4f, 0.5f, N(84), 2.4f, 1.1f, 0.1f, 3f);
                d.Noise(0.5f, 0.4f, Filter.Band, 1800f, 700f, 0.25f, 1.1f, 0.1f, 1.4f, 14f, 0.5f);
                d.Notes(0.7f, 0.08f, 0.15f, Wave.Pulse, Pitches(76, 79, 83), 0.05f, 0.01f);
                return Finish(d.S, 0.38f);
            case "use_noble_roar":
                d = new SoundDesign(1.0f, 0x2024u);
                d.Tone(0, 0.6f, Wave.Saw, N(40), N(48), 0.15f, 0.1f, 1.0f, vibratoHz: 18f, vibratoDepth: 0.1f);
                d.Tone(0, 0.6f, Wave.Triangle, N(52), N(60), 0.1f, 0.15f, 1.0f);
                d.Noise(0, 0.5f, Filter.Band, 500f, 2200f, 0.4f, 1.1f, 0.15f, 1.2f);
                d.Thump(0.34f, 0.3f, 130f, 45f, 0.6f, 0.4f);
                d.Notes(0.5f, 0.09f, 0.22f, Wave.Triangle, Pitches(67, 72, 76, 79), 0.06f, 0.02f);
                return Finish(d.S, 0.42f);
            case "use_ion_deluge":
                d = new SoundDesign(1.0f, 0x10DEu);
                d.Noise(0, 0.9f, Filter.Band, 1200f, 3500f, 0.3f, 1.1f, 0.3f, 1.0f, 14f, 0.5f);
                d.Tone(0, 0.9f, Wave.Sine, N(48), N(52), 0.09f, 0.3f, 1.0f, vibratoHz: 7f, vibratoDepth: 0.05f);
                d.Crackle(0.1f, 0.8f, 26, 5200f, 0.35f);
                d.Notes(0.5f, 0.07f, 0.14f, Wave.Pulse, Pitches(91, 88, 84, 79), 0.04f, 0.005f);
                return Finish(d.S, 0.36f);
            case "use_parabolic_charge":
                d = new SoundDesign(0.95f, 0xBA2Cu);
                d.Crackle(0.02f, 0.34f, 18, 5400f, 0.4f);
                d.Tone(0.02f, 0.34f, Wave.Pulse, N(60), N(88), 0.08f, 0.05f, 1.3f, 0.2f, vibratoHz: 16f, vibratoDepth: 0.05f);
                d.Thump(0.34f, 0.3f, 210f, 55f, 0.8f, 0.7f);
                d.Crackle(0.35f, 0.3f, 14, 4800f, 0.4f);
                d.Tone(0.5f, 0.4f, Wave.Sine, N(67), N(79), 0.09f, 0.1f, 1.3f);
                d.Notes(0.6f, 0.06f, 0.14f, Wave.Triangle, Pitches(79, 84, 88), 0.05f, 0.01f);
                return Finish(d.S, 0.4f);
            case "use_forest_s_curse":
                d = new SoundDesign(1.0f, 0xFC12u);
                d.Noise(0, 0.5f, Filter.Band, 400f, 1400f, 0.4f, 1.1f, 0.3f, 1.0f, 12f, 0.5f);
                d.Tone(0, 0.8f, Wave.Sine, N(43), N(41), 0.12f, 0.3f, 1.0f, vibratoHz: 5f, vibratoDepth: 0.08f);
                d.Notes(0.3f, 0.14f, 0.3f, Wave.Triangle, Pitches(60, 58, 55, 52), 0.07f, 0.02f);
                d.Bell(0.55f, 0.4f, N(67), 2.4f, 1.1f, 0.09f, 3f);
                d.Noise(0.6f, 0.35f, Filter.High, 3000f, 5500f, 0.2f, 1f, 0.05f, 1.3f, 24f, 0.6f);
                return Finish(d.S, 0.38f);
            case "use_petal_blizzard":
                d = new SoundDesign(1.0f, 0x9E7Au);
                d.Noise(0, 0.9f, Filter.Band, 900f, 3200f, 0.45f, 1.0f, 0.3f, 0.9f, 10f, 0.5f);
                d.Tone(0, 0.8f, Wave.Sine, N(60), N(72), 0.07f, 0.3f, 1.0f);
                d.Notes(0.2f, 0.09f, 0.2f, Wave.Triangle, Pitches(79, 84, 88, 84, 91), 0.05f, 0.01f);
                d.Thump(0.35f, 0.25f, 150f, 50f, 0.5f, 0.3f);
                d.Noise(0.4f, 0.5f, Filter.High, 3000f, 6000f, 0.25f, 0.9f, 0.05f, 1.4f, 26f, 0.7f);
                return Finish(d.S, 0.38f);
            case "use_freeze_dry":
                d = new SoundDesign(0.9f, 0xF2D2u);
                d.Tone(0.02f, 0.4f, Wave.Sine, N(96), N(103), 0.08f, 0.05f, 1.3f);
                d.Noise(0.03f, 0.35f, Filter.High, 4000f, 7500f, 0.3f, 1.2f, 0.1f, 1.2f, 40f, 0.3f);
                d.Thump(0.35f, 0.25f, 200f, 60f, 0.7f, 0.5f);
                d.Crackle(0.37f, 0.45f, 16, 7500f, 0.3f);
                d.Noise(0.45f, 0.4f, Filter.Band, 2500f, 1200f, 0.2f, 1f, 0.05f, 1.4f, 18f, 0.5f);
                return Finish(d.S, 0.38f);
            case "use_disarming_voice":
                d = new SoundDesign(1.0f, 0xD15Au);
                d.Notes(0, 0.12f, 0.3f, Wave.Sine, Pitches(79, 83, 86, 91, 86), 0.1f, 0.03f, 1.6f);
                d.Notes(0.04f, 0.12f, 0.3f, Wave.Triangle, Pitches(67, 71, 74, 79, 74), 0.05f, 0.03f, 1.6f);
                d.Bell(0.35f, 0.6f, N(91), 2.01f, 0.7f, 0.09f, 3f);
                d.Thump(0.35f, 0.2f, 160f, 60f, 0.4f, 0.2f);
                d.Notes(0.6f, 0.08f, 0.2f, Wave.Sine, Pitches(96, 100, 103), 0.04f, 0.01f);
                return Finish(d.S, 0.34f);
            case "use_parting_shot":
                d = new SoundDesign(0.95f, 0x9A27u);
                d.Tone(0, 0.3f, Wave.Saw, N(50), N(58), 0.13f, 0.05f, 1.2f, vibratoHz: 20f, vibratoDepth: 0.08f);
                d.Noise(0, 0.3f, Filter.Band, 600f, 2000f, 0.4f, 1.2f, 0.05f, 1.3f);
                d.Thump(0.34f, 0.25f, 150f, 50f, 0.6f, 0.5f);
                d.Notes(0.4f, 0.07f, 0.16f, Wave.Pulse, Pitches(60, 57, 53), 0.05f, 0.01f);
                d.Noise(0.5f, 0.4f, Filter.Band, 400f, 3500f, 0.4f, 1f, 0.02f, 1.2f);
                return Finish(d.S, 0.4f);
            case "use_topsy_turvy":
                d = new SoundDesign(0.95f, 0x7092u);
                d.Tone(0, 0.5f, Wave.Sine, N(72), N(48), 0.12f, 0.05f, 1.2f, vibratoHz: 8f, vibratoDepth: 0.04f);
                d.Tone(0.3f, 0.5f, Wave.Sine, N(48), N(72), 0.1f, 0.05f, 1.2f);
                d.Notes(0.1f, 0.08f, 0.18f, Wave.Triangle, Pitches(76, 74, 72, 69, 67, 64), 0.06f, 0.01f);
                d.Thump(0.34f, 0.25f, 140f, 50f, 0.5f, 0.3f);
                d.Bell(0.55f, 0.35f, N(79), 2.01f, 0.8f, 0.08f, 3f);
                return Finish(d.S, 0.4f);
            case "use_draining_kiss":
                d = new SoundDesign(0.95f, 0xD41Cu);
                d.Notes(0, 0.08f, 0.2f, Wave.Sine, Pitches(84, 88, 91), 0.09f, 0.02f);
                d.Bell(0.3f, 0.4f, N(96), 2.01f, 0.7f, 0.1f, 3f);
                d.Thump(0.34f, 0.15f, 220f, 100f, 0.4f, 0.3f);
                d.Noise(0.34f, 0.08f, Filter.Band, 3500f, 1500f, 0.3f, 1f, 0.01f, 2f);
                d.Tone(0.45f, 0.5f, Wave.Sine, N(67), N(84), 0.09f, 0.1f, 1.3f, vibratoHz: 7f, vibratoDepth: 0.03f);
                d.Notes(0.7f, 0.05f, 0.15f, Wave.Sine, Pitches(91, 96, 100), 0.04f, 0.005f);
                return Finish(d.S, 0.36f);
            case "use_crafty_shield":
                d = new SoundDesign(0.95f, 0xC4AFu);
                d.Notes(0.02f, 0.04f, 0.14f, Wave.Sine, Pitches(84, 88, 91, 96, 100, 103), 0.07f, 0.005f);
                d.Tone(0.1f, 0.5f, Wave.Triangle, N(60), N(72), 0.07f, 0.1f, 1.2f);
                d.Bell(0.3f, 0.5f, N(98), 2.4f, 0.9f, 0.1f, 3f);
                d.Noise(0.25f, 0.4f, Filter.High, 4500f, 8000f, 0.15f, 1f, 0.2f, 1.6f);
                return Finish(d.S, 0.36f);
            case "use_flower_shield":
                d = new SoundDesign(1.0f, 0xF10Au);
                d.Notes(0, 0.1f, 0.28f, Wave.Sine, Pitches(72, 76, 79, 84, 88), 0.08f, 0.02f, 1.6f);
                d.Tone(0.1f, 0.8f, Wave.Triangle, N(52), N(60), 0.07f, 0.3f, 1.0f);
                d.Bell(0.5f, 0.45f, N(91), 2.01f, 0.8f, 0.09f, 3f);
                d.Noise(0.3f, 0.5f, Filter.High, 3500f, 6500f, 0.12f, 1f, 0.2f, 1.4f, 18f, 0.5f);
                return Finish(d.S, 0.34f);
            case "use_grassy_terrain":
                d = new SoundDesign(1.0f, 0x6A55u);
                d.Noise(0, 0.9f, Filter.Band, 800f, 2600f, 0.35f, 1.0f, 0.4f, 1.0f, 14f, 0.5f);
                d.Tone(0, 0.9f, Wave.Triangle, N(48), N(55), 0.09f, 0.4f, 1.0f);
                d.Notes(0.3f, 0.12f, 0.28f, Wave.Triangle, Pitches(67, 71, 74, 79), 0.07f, 0.02f);
                d.Bell(0.6f, 0.4f, N(86), 2.01f, 0.8f, 0.08f, 3f);
                return Finish(d.S, 0.34f);
            case "use_misty_terrain":
                d = new SoundDesign(1.0f, 0x3157u);
                d.Noise(0, 0.9f, Filter.Low, 2500f, 900f, 0.3f, 0.7f, 0.4f, 1.0f, 6f, 0.3f);
                d.Tone(0, 0.9f, Wave.Sine, N(55), N(62), 0.1f, 0.4f, 1.0f, vibratoHz: 4f, vibratoDepth: 0.04f);
                d.Notes(0.25f, 0.1f, 0.26f, Wave.Sine, Pitches(79, 84, 88, 91, 96), 0.06f, 0.02f);
                d.Crackle(0.4f, 0.5f, 10, 7500f, 0.12f);
                return Finish(d.S, 0.32f);
            case "use_electrify":
                d = new SoundDesign(0.9f, 0xE1EFu);
                d.Crackle(0, 0.34f, 16, 5200f, 0.4f);
                d.Tone(0, 0.34f, Wave.Pulse, N(60), N(84), 0.09f, 0.05f, 1.3f, 0.25f, vibratoHz: 20f, vibratoDepth: 0.06f);
                d.Thump(0.34f, 0.25f, 200f, 60f, 0.7f, 0.6f);
                d.Crackle(0.36f, 0.5f, 22, 4500f, 0.35f);
                d.Tone(0.45f, 0.4f, Wave.Pulse, N(72), N(72), 0.05f, 0.05f, 1.6f, 0.2f, vibratoHz: 26f, vibratoDepth: 0.08f);
                return Finish(d.S, 0.4f);
            case "use_play_rough":
                d = new SoundDesign(1.0f, 0x91A0u);
                d.Noise(0.05f, 0.3f, Filter.Band, 800f, 2500f, 0.4f, 1f, 0.1f, 1.2f, 22f, 0.6f);
                for (int i = 0; i < 5; i++) d.Thump(0.12f + i * 0.09f, 0.08f, 230f - i * 20f, 100f, 0.5f, 0.5f);
                d.Notes(0.2f, 0.07f, 0.12f, Wave.Pulse, Pitches(79, 84, 76, 88, 81), 0.05f, 0.01f);
                d.Thump(0.55f, 0.3f, 170f, 50f, 0.8f, 0.7f);
                d.Bell(0.57f, 0.4f, N(91), 2.01f, 0.8f, 0.08f, 3f);
                return Finish(d.S, 0.4f);
            case "use_fairy_wind":
                d = new SoundDesign(0.95f, 0xFA1Du);
                d.Noise(0, 0.6f, Filter.Band, 1200f, 3800f, 0.3f, 1.1f, 0.3f, 1.0f, 9f, 0.4f);
                d.Notes(0.05f, 0.07f, 0.16f, Wave.Sine, Pitches(84, 88, 91, 96, 91, 100), 0.06f, 0.01f);
                d.Thump(0.35f, 0.2f, 170f, 70f, 0.4f, 0.3f);
                d.Bell(0.37f, 0.45f, N(98), 2.01f, 0.7f, 0.08f, 3f);
                return Finish(d.S, 0.32f);
            case "use_moonblast":
                d = new SoundDesign(1.0f, 0x30B1u);
                d.Tone(0, 0.34f, Wave.Sine, N(72), N(96), 0.1f, 0.15f, 1.3f, vibratoHz: 6f, vibratoDepth: 0.03f);
                d.Notes(0.05f, 0.06f, 0.12f, Wave.Triangle, Pitches(84, 88, 91, 96), 0.05f, 0.01f);
                d.Thump(0.35f, 0.4f, 190f, 38f, 1.0f, 0.8f);
                d.Bell(0.36f, 0.6f, N(91), 2.76f, 1.1f, 0.12f, 3f);
                d.Crackle(0.4f, 0.4f, 12, 7000f, 0.25f);
                d.Tone(0.5f, 0.5f, Wave.Sine, N(79), N(67), 0.07f, 0.05f, 1.6f);
                return Finish(d.S, 0.42f);
            case "use_boomburst":
                d = new SoundDesign(1.0f, 0xB00Au);
                d.Tone(0, 0.34f, Wave.Saw, N(36), N(50), 0.14f, 0.1f, 1.2f, vibratoHz: 22f, vibratoDepth: 0.1f);
                d.Thump(0.34f, 0.65f, 150f, 22f, 1.0f, 1.0f);
                d.Noise(0.34f, 0.5f, Filter.Band, 2500f, 400f, 0.7f, 1f, 0.005f, 1.3f);
                for (int i = 0; i < 4; i++) d.Thump(0.4f + i * 0.07f, 0.3f, 130f - i * 10f, 30f, 0.5f - i * 0.1f, 0.3f);
                d.Crackle(0.36f, 0.3f, 14, 3500f, 0.4f);
                return Finish(d.S, 0.46f);
            case "use_fairy_lock":
                d = new SoundDesign(1.0f, 0xFA10u);
                d.Notes(0, 0.1f, 0.2f, Wave.Sine, Pitches(84, 88, 91), 0.08f, 0.01f);
                d.Thump(0.32f, 0.12f, 260f, 120f, 0.7f, 0.9f);
                d.Bell(0.33f, 0.6f, N(86), 3.0f, 1.0f, 0.12f, 3f);
                for (int i = 0; i < 5; i++) d.Thump(0.45f + i * 0.08f, 0.1f, 210f, 90f, 0.4f, 0.5f);
                d.Notes(0.55f, 0.06f, 0.2f, Wave.Triangle, Pitches(96, 100, 103), 0.04f, 0.01f);
                return Finish(d.S, 0.38f);
            case "use_king_s_shield":
                d = new SoundDesign(0.9f, 0x41C5u);
                d.Noise(0.02f, 0.2f, Filter.High, 2500f, 5500f, 0.5f, 1.2f, 0.1f, 1.4f);
                d.Thump(0.2f, 0.25f, 210f, 70f, 0.8f, 0.8f);
                d.Bell(0.22f, 0.7f, N(79), 2.76f, 1.2f, 0.13f, 3f);
                d.Notes(0.4f, 0.1f, 0.22f, Wave.Pulse, Pitches(72, 76, 79), 0.06f, 0.01f);
                d.Crackle(0.25f, 0.3f, 10, 6500f, 0.25f);
                return Finish(d.S, 0.4f);
            case "use_play_nice":
                d = new SoundDesign(0.95f, 0x91A1u);
                d.Notes(0, 0.1f, 0.22f, Wave.Triangle, Pitches(72, 76, 79), 0.08f, 0.02f);
                d.Thump(0.3f, 0.1f, 200f, 100f, 0.4f, 0.5f);
                d.Thump(0.37f, 0.1f, 200f, 100f, 0.4f, 0.5f);
                d.Bell(0.4f, 0.5f, N(88), 2.01f, 0.7f, 0.1f, 3f);
                d.Notes(0.5f, 0.09f, 0.2f, Wave.Sine, Pitches(84, 88, 91), 0.05f, 0.01f);
                return Finish(d.S, 0.36f);
            case "use_confide":
                d = new SoundDesign(0.95f, 0xC0FDu);
                d.Noise(0, 0.4f, Filter.Band, 2000f, 3200f, 0.25f, 1.3f, 0.2f, 1.2f, 14f, 0.6f);
                d.Notes(0.1f, 0.12f, 0.16f, Wave.Sine, Pitches(79, 79, 79), 0.06f, 0.01f);
                d.Bell(0.36f, 0.5f, N(84), 2.01f, 0.7f, 0.08f, 3f);
                d.Tone(0.45f, 0.4f, Wave.Sine, N(72), N(60), 0.08f, 0.05f, 1.5f);
                d.Notes(0.6f, 0.08f, 0.16f, Wave.Triangle, Pitches(67, 64, 60), 0.05f, 0.01f);
                return Finish(d.S, 0.32f);
            case "use_diamond_storm":
                d = new SoundDesign(1.0f, 0xD1A5u);
                d.Noise(0, 0.5f, Filter.High, 2500f, 6500f, 0.4f, 1.2f, 0.2f, 1.1f, 34f, 0.6f);
                d.Tone(0, 0.4f, Wave.Sine, N(76), N(100), 0.09f, 0.1f, 1.2f);
                d.Thump(0.35f, 0.4f, 190f, 36f, 0.9f, 0.8f);
                d.Notes(0.36f, 0.04f, 0.18f, Wave.Sine, Pitches(96, 100, 103, 108, 103, 100), 0.06f, 0.005f);
                d.Crackle(0.37f, 0.55f, 20, 6800f, 0.35f);
                d.Bell(0.4f, 0.55f, N(103), 3.01f, 1.0f, 0.09f, 3f);
                return Finish(d.S, 0.42f);
            case "use_steam_eruption":
                d = new SoundDesign(1.0f, 0x57E4u);
                d.Tone(0, 0.34f, Wave.Saw, N(33), N(40), 0.1f, 0.2f, 1.0f, vibratoHz: 20f, vibratoDepth: 0.1f);
                d.Noise(0.05f, 0.3f, Filter.Low, 1000f, 600f, 0.4f, 0.9f, 0.3f, 1.0f, 24f, 0.5f);
                d.Thump(0.34f, 0.45f, 150f, 28f, 1.0f, 0.9f);
                d.Noise(0.35f, 0.6f, Filter.High, 2500f, 5500f, 0.55f, 0.9f, 0.01f, 1.2f);
                d.Bubbles(0.45f, 0.4f, 6, 300f, 900f, 0.25f);
                return Finish(d.S, 0.44f);
            case "use_hyperspace_hole":
                d = new SoundDesign(0.95f, 0x4095u);
                d.Tone(0, 0.4f, Wave.Sine, N(55), N(84), 0.1f, 0.1f, 1.2f, vibratoHz: 10f, vibratoDepth: 0.05f);
                d.Notes(0.05f, 0.05f, 0.12f, Wave.Triangle, Pitches(88, 91, 96, 100), 0.05f, 0.005f);
                d.Thump(0.34f, 0.3f, 200f, 50f, 0.9f, 0.8f);
                d.Crackle(0.36f, 0.4f, 12, 5500f, 0.3f);
                d.Bell(0.38f, 0.5f, N(91), 3.14f, 1.1f, 0.1f, 3f);
                d.Tone(0.45f, 0.45f, Wave.Sine, N(84), N(48), 0.08f, 0.05f, 1.6f);
                return Finish(d.S, 0.42f);
            case "use_water_shuriken":
                d = new SoundDesign(0.9f, 0x5A21u);
                for (int i = 0; i < 3; i++)
                {
                    d.Noise(0.02f + i * 0.08f, 0.14f, Filter.High, 3000f, 7000f, 0.4f, 1.3f, 0.1f, 1.8f);
                    d.Thump(0.22f + i * 0.07f, 0.1f, 220f, 90f, 0.45f, 0.4f);
                    d.Noise(0.23f + i * 0.07f, 0.2f, Filter.Band, 2500f, 1000f, 0.25f, 1f, 0.01f, 1.8f);
                }
                d.Bubbles(0.45f, 0.35f, 5, 500f, 1300f, 0.22f);
                d.Bell(0.4f, 0.4f, N(91), 2.01f, 0.8f, 0.07f, 3f);
                return Finish(d.S, 0.38f);
            case "use_mystical_fire":
                d = new SoundDesign(1.0f, 0x3F1Eu);
                d.Noise(0, 0.4f, Filter.Band, 700f, 2200f, 0.4f, 1.1f, 0.3f, 1.0f, 18f, 0.5f);
                d.Tone(0, 0.4f, Wave.Triangle, N(55), N(72), 0.1f, 0.2f, 1.1f, vibratoHz: 11f, vibratoDepth: 0.07f);
                d.Thump(0.34f, 0.35f, 160f, 40f, 0.8f, 0.6f);
                d.Crackle(0.36f, 0.5f, 16, 3200f, 0.3f);
                d.Notes(0.5f, 0.08f, 0.2f, Wave.Sine, Pitches(79, 76, 72, 67), 0.06f, 0.02f);
                return Finish(d.S, 0.4f);
            case "use_spiky_shield":
                d = new SoundDesign(0.9f, 0x5B1Eu);
                for (int i = 0; i < 6; i++) d.Noise(0.03f + i * 0.04f, 0.08f, Filter.High, 3500f + i * 300f, 7000f, 0.35f, 1.4f, 0.05f, 1.8f);
                d.Tone(0.05f, 0.4f, Wave.Triangle, N(55), N(67), 0.08f, 0.1f, 1.3f);
                d.Thump(0.3f, 0.2f, 180f, 70f, 0.5f, 0.5f);
                d.Bell(0.33f, 0.45f, N(86), 2.76f, 1.0f, 0.1f, 3f);
                d.Notes(0.5f, 0.06f, 0.14f, Wave.Pulse, Pitches(72, 76, 79), 0.05f, 0.01f);
                return Finish(d.S, 0.38f);
            case "use_aromatic_mist":
                d = new SoundDesign(1.0f, 0xA20Au);
                d.Noise(0, 0.7f, Filter.Low, 2500f, 1200f, 0.3f, 0.7f, 0.3f, 1.0f, 6f, 0.3f);
                d.Notes(0.1f, 0.1f, 0.26f, Wave.Sine, Pitches(76, 79, 84, 88, 91), 0.07f, 0.02f);
                d.Bell(0.45f, 0.5f, N(96), 2.01f, 0.7f, 0.08f, 3f);
                d.Tone(0.5f, 0.45f, Wave.Triangle, N(60), N(67), 0.06f, 0.1f, 1.3f);
                return Finish(d.S, 0.32f);
            case "use_eerie_impulse":
                d = new SoundDesign(0.95f, 0xEE21u);
                d.Tone(0, 0.5f, Wave.Sine, N(67), N(60), 0.1f, 0.05f, 1.2f, vibratoHz: 9f, vibratoDepth: 0.1f);
                d.Notes(0.05f, 0.1f, 0.16f, Wave.Pulse, Pitches(76, 79, 76, 72), 0.05f, 0.01f, 2f, 0.25f);
                d.Crackle(0.1f, 0.3f, 10, 4800f, 0.3f);
                d.Thump(0.35f, 0.25f, 150f, 55f, 0.6f, 0.4f);
                d.Tone(0.4f, 0.5f, Wave.Triangle, N(60), N(48), 0.09f, 0.05f, 1.5f, vibratoHz: 12f, vibratoDepth: 0.08f);
                return Finish(d.S, 0.38f);
            case "use_venom_drench":
                d = new SoundDesign(0.95f, 0x7E2Du);
                d.Noise(0, 0.3f, Filter.High, 2500f, 5000f, 0.3f, 1f, 0.1f, 1.4f);
                d.Thump(0.33f, 0.2f, 220f, 100f, 0.6f, 0.9f);
                d.Noise(0.34f, 0.45f, Filter.Band, 1800f, 500f, 0.4f, 1f, 0.01f, 1.5f, 16f, 0.5f);
                d.Bubbles(0.4f, 0.5f, 9, 200f, 600f, 0.35f);
                d.Tone(0.5f, 0.45f, Wave.Saw, N(48), N(38), 0.07f, 0.1f, 1.4f, vibratoHz: 8f, vibratoDepth: 0.05f);
                return Finish(d.S, 0.4f);
            case "use_powder":
                d = new SoundDesign(0.95f, 0x90D3u);
                d.Noise(0, 0.5f, Filter.High, 3500f, 6500f, 0.35f, 0.9f, 0.2f, 1.2f, 20f, 0.6f);
                d.Notes(0.2f, 0.07f, 0.12f, Wave.Triangle, Pitches(84, 88, 91, 88), 0.05f, 0.01f);
                d.Crackle(0.4f, 0.5f, 9, 5200f, 0.2f);
                d.Tone(0.5f, 0.4f, Wave.Sine, N(72), N(76), 0.05f, 0.1f, 1.4f);
                return Finish(d.S, 0.32f);
            case "use_geomancy":
                d = new SoundDesign(1.0f, 0x6E03u);
                d.Tone(0, 0.9f, Wave.Sine, N(43), N(60), 0.12f, 0.3f, 1.0f, vibratoHz: 5f, vibratoDepth: 0.04f);
                d.Thump(0.02f, 0.4f, 90f, 40f, 0.6f, 0.2f);
                d.Notes(0.15f, 0.1f, 0.3f, Wave.Triangle, Pitches(60, 64, 67, 72, 76, 79), 0.06f, 0.02f);
                d.Noise(0.2f, 0.6f, Filter.High, 3000f, 6500f, 0.15f, 1f, 0.3f, 1.4f, 16f, 0.5f);
                d.Bell(0.65f, 0.35f, N(91), 2.01f, 0.8f, 0.1f, 3f);
                return Finish(d.S, 0.38f);
            case "use_magnetic_flux":
                d = new SoundDesign(0.95f, 0x3A6Fu);
                d.Tone(0, 0.7f, Wave.Sine, N(48), N(53), 0.12f, 0.2f, 1.0f, vibratoHz: 14f, vibratoDepth: 0.08f);
                d.Tone(0, 0.7f, Wave.Sine, N(55), N(50), 0.08f, 0.2f, 1.0f, vibratoHz: 11f, vibratoDepth: 0.08f);
                d.Crackle(0.2f, 0.5f, 14, 4800f, 0.25f);
                d.Notes(0.4f, 0.08f, 0.18f, Wave.Pulse, Pitches(76, 79, 83, 88), 0.05f, 0.01f);
                d.Bell(0.65f, 0.3f, N(88), 2.01f, 0.7f, 0.07f, 3f);
                return Finish(d.S, 0.36f);
            case "use_happy_hour":
                d = new SoundDesign(1.0f, 0x4A99u);
                for (int i = 0; i < 9; i++) d.Bell(0.03f + i * 0.08f, 0.25f, N(88 + (i * 3) % 12), 3.5f, 0.7f, 0.06f, 3.5f);
                d.Notes(0.1f, 0.12f, 0.2f, Wave.Pulse, Pitches(72, 76, 79, 84, 79, 84, 88), 0.05f, 0.01f, 2f, 0.3f);
                d.Tone(0, 0.9f, Wave.Triangle, N(48), N(52), 0.05f, 0.2f, 1.0f);
                return Finish(d.S, 0.36f);
            case "use_electric_terrain":
                d = new SoundDesign(1.0f, 0xE7E2u);
                d.Tone(0, 0.9f, Wave.Pulse, N(36), N(36), 0.08f, 0.3f, 1.0f, 0.3f, vibratoHz: 30f, vibratoDepth: 0.04f);
                d.Crackle(0.05f, 0.85f, 28, 5000f, 0.3f);
                d.Noise(0.1f, 0.7f, Filter.Band, 1500f, 3500f, 0.2f, 1.2f, 0.3f, 1.0f, 18f, 0.5f);
                d.Notes(0.4f, 0.07f, 0.12f, Wave.Pulse, Pitches(79, 83, 88, 91), 0.04f, 0.005f);
                return Finish(d.S, 0.34f);
            case "use_dazzling_gleam":
                d = new SoundDesign(1.0f, 0xDA22u);
                d.Tone(0, 0.34f, Wave.Sine, N(72), N(100), 0.1f, 0.1f, 1.2f);
                d.Noise(0, 0.34f, Filter.High, 3500f, 8000f, 0.2f, 1f, 0.3f, 1.2f);
                d.Thump(0.34f, 0.3f, 200f, 50f, 0.8f, 0.5f);
                d.Bell(0.35f, 0.65f, N(96), 2.4f, 1.2f, 0.14f, 3f);
                d.Notes(0.4f, 0.05f, 0.2f, Wave.Sine, Pitches(103, 108, 112, 108), 0.05f, 0.005f);
                d.Crackle(0.37f, 0.4f, 14, 7000f, 0.3f);
                return Finish(d.S, 0.42f);
            case "use_celebrate":
                d = new SoundDesign(0.95f, 0xCE1Eu);
                d.Thump(0.04f, 0.2f, 240f, 70f, 0.8f, 0.9f);
                d.Noise(0.04f, 0.5f, Filter.High, 3000f, 7500f, 0.5f, 1f, 0.01f, 1.2f, 40f, 0.7f);
                d.Notes(0.12f, 0.08f, 0.16f, Wave.Pulse, Pitches(72, 76, 79, 84, 88, 84, 91), 0.06f, 0.01f, 2f, 0.3f);
                d.Bell(0.2f, 0.5f, N(96), 2.01f, 0.8f, 0.09f, 3f);
                d.Bell(0.5f, 0.4f, N(100), 2.01f, 0.8f, 0.08f, 3f);
                return Finish(d.S, 0.4f);
            case "use_hold_hands":
                d = new SoundDesign(0.95f, 0x401Du);
                d.Notes(0, 0.12f, 0.26f, Wave.Triangle, Pitches(67, 72, 76, 79), 0.08f, 0.02f);
                d.Tone(0, 0.8f, Wave.Sine, N(55), N(60), 0.07f, 0.3f, 1.0f, vibratoHz: 5f, vibratoDepth: 0.03f);
                d.Bell(0.45f, 0.5f, N(88), 2.01f, 0.7f, 0.09f, 3f);
                d.Notes(0.55f, 0.1f, 0.22f, Wave.Sine, Pitches(84, 88, 91), 0.05f, 0.02f);
                return Finish(d.S, 0.34f);
            case "use_baby_doll_eyes":
                d = new SoundDesign(0.9f, 0xBAB1u);
                d.Notes(0, 0.07f, 0.16f, Wave.Sine, Pitches(88, 91, 96, 100), 0.08f, 0.01f);
                d.Bell(0.12f, 0.45f, N(103), 2.01f, 0.7f, 0.1f, 3f);
                d.Bell(0.2f, 0.4f, N(108), 2.01f, 0.7f, 0.08f, 3f);
                d.Tone(0.35f, 0.4f, Wave.Sine, N(79), N(67), 0.07f, 0.05f, 1.5f);
                d.Notes(0.45f, 0.09f, 0.18f, Wave.Triangle, Pitches(84, 79, 76), 0.05f, 0.01f);
                return Finish(d.S, 0.34f);
            case "use_nuzzle":
                d = new SoundDesign(0.85f, 0x2022u);
                d.Crackle(0.05f, 0.3f, 12, 5000f, 0.3f);
                d.Tone(0.05f, 0.3f, Wave.Pulse, N(72), N(88), 0.07f, 0.05f, 1.3f, 0.25f);
                d.Thump(0.33f, 0.2f, 200f, 90f, 0.5f, 0.4f);
                d.Crackle(0.35f, 0.4f, 16, 4800f, 0.3f);
                d.Notes(0.5f, 0.06f, 0.12f, Wave.Sine, Pitches(88, 91, 96), 0.05f, 0.01f);
                return Finish(d.S, 0.34f);
            case "use_hold_back":
                d = new SoundDesign(0.8f, 0x401Bu);
                d.Noise(0.1f, 0.2f, Filter.Band, 600f, 1800f, 0.3f, 1f, 0.2f, 1.4f);
                d.Thump(0.34f, 0.2f, 180f, 80f, 0.5f, 0.3f);
                d.Notes(0.4f, 0.1f, 0.2f, Wave.Triangle, Pitches(72, 67), 0.06f, 0.01f);
                d.Bell(0.45f, 0.3f, N(88), 2.01f, 0.5f, 0.06f, 3f);
                return Finish(d.S, 0.38f);
            case "use_infestation":
                d = new SoundDesign(1.0f, 0x1FE5u);
                d.Noise(0, 0.9f, Filter.High, 3500f, 6500f, 0.35f, 1.3f, 0.2f, 1.0f, 48f, 0.8f);
                for (int i = 0; i < 10; i++) d.Thump(0.05f + i * 0.045f, 0.05f, 300f, 160f, 0.2f, 0.4f);
                d.Thump(0.34f, 0.25f, 160f, 60f, 0.5f, 0.4f);
                d.Tone(0.35f, 0.6f, Wave.Saw, N(60), N(57), 0.05f, 0.1f, 1.4f, vibratoHz: 50f, vibratoDepth: 0.06f);
                d.Notes(0.7f, 0.06f, 0.12f, Wave.Pulse, Pitches(64, 62, 59), 0.04f, 0.01f);
                return Finish(d.S, 0.36f);
            case "use_power_up_punch":
                d = new SoundDesign(0.9f, 0x90F7u);
                d.Tone(0, 0.3f, Wave.Saw, N(48), N(64), 0.1f, 0.1f, 1.2f, vibratoHz: 16f, vibratoDepth: 0.06f);
                d.Noise(0.1f, 0.2f, Filter.High, 2500f, 6000f, 0.4f, 1.2f, 0.1f, 1.6f);
                d.Thump(0.34f, 0.4f, 180f, 36f, 1.0f, 0.9f);
                d.Notes(0.5f, 0.07f, 0.16f, Wave.Pulse, Pitches(72, 76, 79, 84), 0.06f, 0.01f);
                d.Bell(0.55f, 0.35f, N(91), 2.01f, 0.8f, 0.08f, 3f);
                return Finish(d.S, 0.42f);
            case "use_oblivion_wing":
                d = new SoundDesign(1.0f, 0x0B11u);
                d.Noise(0, 0.4f, Filter.Band, 500f, 1800f, 0.5f, 1.2f, 0.2f, 1.1f, 12f, 0.5f);
                d.Tone(0, 0.5f, Wave.Saw, N(43), N(55), 0.12f, 0.1f, 1.1f, vibratoHz: 10f, vibratoDepth: 0.06f);
                d.Thump(0.34f, 0.35f, 170f, 40f, 0.9f, 0.7f);
                d.Tone(0.4f, 0.55f, Wave.Sine, N(55), N(72), 0.1f, 0.1f, 1.2f, vibratoHz: 7f, vibratoDepth: 0.04f);
                d.Notes(0.6f, 0.08f, 0.18f, Wave.Triangle, Pitches(67, 72, 76), 0.06f, 0.02f);
                return Finish(d.S, 0.42f);
            case "use_thousand_arrows":
                d = new SoundDesign(1.0f, 0x7A55u);
                for (int i = 0; i < 9; i++)
                {
                    d.Noise(0.03f + i * 0.04f, 0.08f, Filter.High, 3000f, 6000f, 0.25f, 1.3f, 0.1f, 1.8f);
                    d.Thump(0.12f + i * 0.04f, 0.1f, 220f - i * 8f, 90f, 0.35f, 0.5f);
                }
                d.Thump(0.5f, 0.45f, 150f, 28f, 0.9f, 0.8f);
                d.Crackle(0.52f, 0.4f, 12, 1800f, 0.3f);
                return Finish(d.S, 0.4f);
            case "use_thousand_waves":
                d = new SoundDesign(1.0f, 0x7A5Eu);
                for (int i = 0; i < 5; i++) d.Noise(0.03f + i * 0.08f, 0.4f, Filter.Band, 500f + i * 150f, 1800f, 0.35f, 0.9f, 0.4f, 1.3f, 12f, 0.4f);
                d.Tone(0, 0.8f, Wave.Saw, N(38), N(36), 0.07f, 0.2f, 1.0f, vibratoHz: 12f, vibratoDepth: 0.06f);
                d.Thump(0.4f, 0.5f, 140f, 26f, 0.9f, 0.8f);
                d.Crackle(0.45f, 0.4f, 14, 1700f, 0.3f);
                return Finish(d.S, 0.4f);
            case "use_land_s_wrath":
                d = new SoundDesign(1.0f, 0x1A4Du);
                d.Tone(0, 0.5f, Wave.Saw, N(31), N(36), 0.14f, 0.2f, 0.9f, vibratoHz: 16f, vibratoDepth: 0.1f);
                d.Noise(0, 0.5f, Filter.Low, 1400f, 500f, 0.5f, 0.9f, 0.3f, 0.9f, 20f, 0.5f);
                d.Crackle(0.1f, 0.3f, 18, 1500f, 0.45f);
                d.Thump(0.34f, 0.65f, 120f, 22f, 1.0f, 1.0f);
                d.Noise(0.36f, 0.5f, Filter.Band, 1200f, 400f, 0.5f, 1f, 0.01f, 1.4f, 24f, 0.5f);
                d.Crackle(0.4f, 0.5f, 18, 1900f, 0.4f);
                d.Bell(0.45f, 0.45f, N(55), 1.41f, 0.8f, 0.08f, 3f);
                return Finish(d.S, 0.46f);
            case "use_light_of_ruin":
                d = new SoundDesign(1.0f, 0x110Fu);
                d.Notes(0, 0.08f, 0.2f, Wave.Sine, Pitches(72, 76, 79, 84, 88), 0.08f, 0.02f);
                d.Tone(0, 0.34f, Wave.Sine, N(60), N(96), 0.1f, 0.2f, 1.2f);
                d.Thump(0.34f, 0.5f, 170f, 26f, 1.0f, 0.9f);
                d.Noise(0.35f, 0.5f, Filter.Band, 3000f, 900f, 0.4f, 1f, 0.01f, 1.5f);
                d.Bell(0.36f, 0.6f, N(88), 2.76f, 1.2f, 0.13f, 3f);
                d.Tone(0.55f, 0.4f, Wave.Saw, N(55), N(40), 0.07f, 0.05f, 1.6f);
                return Finish(d.S, 0.46f);
            case "use_origin_pulse":
                d = new SoundDesign(1.0f, 0x021Du);
                d.Tone(0, 0.4f, Wave.Sine, N(55), N(79), 0.1f, 0.1f, 1.2f, vibratoHz: 8f, vibratoDepth: 0.04f);
                for (int i = 0; i < 3; i++) d.Noise(0.05f + i * 0.05f, 0.35f, Filter.Band, 1500f + i * 500f, 4000f, 0.35f, 1.4f, 0.1f, 1.2f);
                d.Thump(0.35f, 0.45f, 180f, 30f, 1.0f, 0.8f);
                d.Noise(0.36f, 0.5f, Filter.Band, 2500f, 700f, 0.4f, 1f, 0.01f, 1.5f, 20f, 0.4f);
                d.Bubbles(0.42f, 0.4f, 7, 350f, 1000f, 0.25f);
                d.Bell(0.4f, 0.55f, N(86), 2.01f, 1.0f, 0.1f, 3f);
                return Finish(d.S, 0.44f);
            case "use_precipice_blades":
                d = new SoundDesign(1.0f, 0x9BE1u);
                d.Tone(0, 0.34f, Wave.Saw, N(33), N(40), 0.12f, 0.2f, 1.0f, vibratoHz: 18f, vibratoDepth: 0.1f);
                d.Crackle(0.05f, 0.28f, 14, 1500f, 0.4f);
                for (int i = 0; i < 4; i++) d.Thump(0.26f + i * 0.04f, 0.15f, 190f - i * 20f, 50f, 0.7f, 0.8f);
                d.Thump(0.4f, 0.5f, 140f, 24f, 1.0f, 0.9f);
                d.Crackle(0.42f, 0.45f, 16, 2600f, 0.4f);
                d.Noise(0.45f, 0.4f, Filter.Band, 2200f, 700f, 0.3f, 1f, 0.02f, 1.6f, 22f, 0.5f);
                return Finish(d.S, 0.44f);
            case "use_dragon_ascent":
                d = new SoundDesign(1.0f, 0xDA5Cu);
                d.Noise(0, 0.5f, Filter.Band, 400f, 3500f, 0.45f, 1.1f, 0.2f, 1.1f, 12f, 0.5f);
                d.Tone(0, 0.5f, Wave.Saw, N(45), N(76), 0.11f, 0.1f, 1.1f, vibratoHz: 10f, vibratoDepth: 0.06f);
                d.Thump(0.35f, 0.45f, 190f, 30f, 1.0f, 0.9f);
                d.Tone(0.38f, 0.5f, Wave.Sine, N(72), N(48), 0.1f, 0.03f, 1.5f);
                d.Bell(0.4f, 0.5f, N(84), 2.01f, 0.9f, 0.1f, 3f);
                d.Noise(0.4f, 0.4f, Filter.High, 3000f, 6000f, 0.25f, 1f, 0.02f, 1.5f, 28f, 0.6f);
                return Finish(d.S, 0.44f);
            case "use_hyperspace_fury":
                d = new SoundDesign(1.0f, 0x4F0Fu);
                d.Tone(0, 0.4f, Wave.Sine, N(60), N(84), 0.09f, 0.1f, 1.2f, vibratoHz: 12f, vibratoDepth: 0.05f);
                for (int i = 0; i < 4; i++)
                {
                    d.Noise(0.05f + i * 0.08f, 0.1f, Filter.High, 3000f, 6500f, 0.35f, 1.3f, 0.1f, 1.8f);
                    d.Thump(0.14f + i * 0.08f, 0.12f, 220f - i * 15f, 60f, 0.6f + i * 0.1f, 0.7f);
                }
                d.Thump(0.4f, 0.5f, 160f, 26f, 1.0f, 0.9f);
                d.Crackle(0.42f, 0.4f, 12, 5200f, 0.3f);
                d.Bell(0.44f, 0.5f, N(84), 3.14f, 1.1f, 0.09f, 3f);
                return Finish(d.S, 0.46f);
        }
        return null;
    }
}
