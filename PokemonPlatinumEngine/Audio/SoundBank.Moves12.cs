using System;
using static PokemonPlatinumEngine.Audio.SoundDesign;

namespace PokemonPlatinumEngine.Audio;

// Plan 25 - Y11, its second part (Y11b): the sounds of the later games' moves, the other 36 of Generation 5 (Frost
// Breath to Fusion Bolt). No game's sound is the model: each is our own, built from the bank's shared layers and timed
// to its look (the blow at 0.35 s). Every one runs to at most a second.
public static partial class SoundBank
{
    /// <summary>A sound of Y11's second part, or null for a name that isn't one.</summary>
    private static float[]? MakeMoveSoundY11b(string name)
    {
        SoundDesign d;
        switch (name)
        {
            case "use_frost_breath":
                d = new SoundDesign(0.9f, 0xF105u);
                d.Noise(0, 0.4f, Filter.High, 2500f, 6500f, 0.4f, 0.9f, 0.1f, 1.2f, 30f, 0.3f);
                d.Tone(0.05f, 0.35f, Wave.Sine, N(84), N(96), 0.08f, 0.1f, 1.3f, vibratoHz: 9f, vibratoDepth: 0.03f);
                d.Thump(0.35f, 0.25f, 200f, 60f, 0.8f, 0.5f);
                d.Bell(0.36f, 0.5f, N(103), 3.01f, 1.0f, 0.13f, 3f);
                d.Crackle(0.38f, 0.3f, 8, 7000f, 0.25f);
                return Finish(d.S, 0.4f);
            case "use_dragon_tail":
                d = new SoundDesign(0.9f, 0xD74Eu);
                d.Noise(0.04f, 0.3f, Filter.Band, 600f, 2200f, 0.5f, 1.3f, 0.1f, 1.2f);
                d.Tone(0.04f, 0.3f, Wave.Saw, N(45), N(57), 0.1f, 0.1f, 1.2f, vibratoHz: 12f, vibratoDepth: 0.05f);
                d.Thump(0.34f, 0.4f, 160f, 40f, 1.0f, 0.8f);
                d.Tone(0.38f, 0.4f, Wave.Saw, N(52), N(40), 0.08f, 0.03f, 1.6f);
                d.Noise(0.4f, 0.3f, Filter.High, 5000f, 2000f, 0.25f, 1f, 0.01f, 1.8f);
                return Finish(d.S, 0.44f);
            case "use_work_up":
                d = new SoundDesign(0.9f, 0x2007u);
                d.Tone(0, 0.7f, Wave.Triangle, N(48), N(64), 0.12f, 0.2f, 1.2f, vibratoHz: 10f, vibratoDepth: 0.04f);
                d.Notes(0.1f, 0.1f, 0.2f, Wave.Pulse, Pitches(60, 64, 67, 72), 0.06f, 0.01f);
                d.Thump(0.3f, 0.3f, 130f, 50f, 0.7f, 0.4f);
                d.Thump(0.55f, 0.25f, 150f, 60f, 0.6f, 0.4f);
                d.Bell(0.5f, 0.4f, N(84), 2.01f, 0.9f, 0.1f, 3f);
                return Finish(d.S, 0.38f);
            case "use_electroweb":
                d = new SoundDesign(0.9f, 0xE1EBu);
                d.Noise(0, 0.3f, Filter.Band, 1500f, 3500f, 0.3f, 1.2f, 0.1f, 1.2f);
                d.Crackle(0.12f, 0.5f, 22, 5500f, 0.35f);
                d.Tone(0.2f, 0.4f, Wave.Pulse, N(88), N(76), 0.07f, 0.05f, 1.4f, 0.2f, vibratoHz: 18f, vibratoDepth: 0.06f);
                d.Thump(0.34f, 0.25f, 210f, 70f, 0.7f, 0.6f);
                d.Notes(0.45f, 0.06f, 0.14f, Wave.Pulse, Pitches(91, 88, 84), 0.05f, 0.01f);
                return Finish(d.S, 0.4f);
            case "use_wild_charge":
                d = new SoundDesign(0.9f, 0x3C11u);
                d.Crackle(0, 0.34f, 20, 5200f, 0.4f);
                d.Tone(0, 0.34f, Wave.Saw, N(43), N(67), 0.12f, 0.1f, 1.2f, vibratoHz: 20f, vibratoDepth: 0.08f);
                d.Thump(0.34f, 0.4f, 190f, 40f, 1.0f, 0.9f);
                d.Crackle(0.35f, 0.4f, 14, 4200f, 0.4f);
                d.Tone(0.42f, 0.3f, Wave.Pulse, N(88), N(60), 0.08f, 0.02f, 1.4f, 0.3f);
                return Finish(d.S, 0.46f);
            case "use_drill_run":
                d = new SoundDesign(0.95f, 0xD41Au);
                d.Tone(0, 0.45f, Wave.Saw, N(50), N(74), 0.11f, 0.1f, 1.0f, vibratoHz: 28f, vibratoDepth: 0.1f);
                d.Noise(0, 0.4f, Filter.Band, 900f, 3200f, 0.35f, 1.5f, 0.2f, 1.0f, 36f, 0.7f);
                d.Thump(0.34f, 0.35f, 170f, 40f, 0.9f, 0.7f);
                d.Crackle(0.36f, 0.4f, 16, 1700f, 0.4f);
                d.Noise(0.4f, 0.4f, Filter.Band, 1800f, 700f, 0.3f, 1.2f, 0.02f, 1.8f, 30f, 0.6f);
                return Finish(d.S, 0.44f);
            case "use_dual_chop":
                d = new SoundDesign(0.85f, 0xDC02u);
                d.Noise(0.05f, 0.14f, Filter.High, 3000f, 7000f, 0.5f, 1.4f, 0.1f, 1.8f);
                d.Thump(0.2f, 0.2f, 200f, 70f, 0.7f, 0.6f);
                d.Noise(0.28f, 0.14f, Filter.High, 3500f, 7500f, 0.5f, 1.4f, 0.1f, 1.8f);
                d.Thump(0.35f, 0.3f, 190f, 50f, 0.9f, 0.7f);
                d.Tone(0.38f, 0.4f, Wave.Triangle, N(62), N(54), 0.08f, 0.02f, 1.6f);
                d.Bell(0.4f, 0.4f, N(79), 2.01f, 0.9f, 0.08f, 3f);
                return Finish(d.S, 0.42f);
            case "use_heart_stamp":
                d = new SoundDesign(0.9f, 0x4E57u);
                d.Tone(0, 0.32f, Wave.Sine, N(84), N(60), 0.1f, 0.02f, 1.4f);
                d.Thump(0.33f, 0.3f, 210f, 55f, 1.0f, 0.8f);
                d.Bell(0.35f, 0.5f, N(88), 1.5f, 0.8f, 0.12f, 3f);
                d.Notes(0.45f, 0.09f, 0.2f, Wave.Triangle, Pitches(79, 84, 88), 0.07f, 0.01f);
                d.Noise(0.34f, 0.12f, Filter.Band, 2000f, 900f, 0.3f, 1f, 0.01f, 2f);
                return Finish(d.S, 0.4f);
            case "use_horn_leech":
                d = new SoundDesign(0.95f, 0x401Eu);
                d.Noise(0, 0.3f, Filter.High, 2500f, 5000f, 0.3f, 1f, 0.1f, 1.4f);
                d.Thump(0.33f, 0.3f, 180f, 50f, 0.9f, 0.7f);
                d.Tone(0.38f, 0.5f, Wave.Sine, N(55), N(72), 0.1f, 0.1f, 1.2f, vibratoHz: 7f, vibratoDepth: 0.04f);
                d.Notes(0.5f, 0.07f, 0.2f, Wave.Triangle, Pitches(72, 76, 79, 84), 0.06f, 0.01f);
                d.Noise(0.4f, 0.4f, Filter.Band, 1500f, 2800f, 0.2f, 1f, 0.05f, 1.2f, 22f, 0.5f);
                return Finish(d.S, 0.4f);
            case "use_sacred_sword":
                d = new SoundDesign(1.0f, 0x5A6Du);
                d.Tone(0, 0.3f, Wave.Sine, N(79), N(96), 0.1f, 0.1f, 1.2f);
                d.Noise(0.2f, 0.12f, Filter.High, 3500f, 7500f, 0.5f, 1.4f, 0.1f, 1.8f);
                d.Noise(0.3f, 0.12f, Filter.High, 4000f, 8000f, 0.5f, 1.4f, 0.1f, 1.8f);
                d.Thump(0.35f, 0.3f, 190f, 55f, 0.8f, 0.6f);
                d.Bell(0.36f, 0.6f, N(91), 2.76f, 1.2f, 0.12f, 3f);
                d.Crackle(0.4f, 0.4f, 10, 6500f, 0.3f);
                return Finish(d.S, 0.4f);
            case "use_razor_shell":
                d = new SoundDesign(0.9f, 0x5E11u);
                d.Noise(0.05f, 0.2f, Filter.High, 2500f, 6500f, 0.5f, 1.2f, 0.1f, 1.6f);
                d.Noise(0.2f, 0.14f, Filter.High, 3000f, 7000f, 0.45f, 1.2f, 0.1f, 1.6f);
                d.Thump(0.34f, 0.25f, 200f, 60f, 0.7f, 0.6f);
                d.Noise(0.36f, 0.5f, Filter.Band, 2500f, 900f, 0.3f, 1f, 0.02f, 1.5f, 30f, 0.5f);
                d.Bubbles(0.4f, 0.4f, 6, 400f, 1100f, 0.25f);
                d.Bell(0.37f, 0.4f, N(96), 3.01f, 0.9f, 0.09f, 3f);
                return Finish(d.S, 0.4f);
            case "use_heat_crash":
                d = new SoundDesign(1.0f, 0x4EA7u);
                d.Tone(0, 0.34f, Wave.Saw, N(60), N(36), 0.12f, 0.1f, 1.2f, vibratoHz: 14f, vibratoDepth: 0.06f);
                d.Noise(0, 0.34f, Filter.Low, 3000f, 700f, 0.5f, 1f, 0.2f, 1.2f, 24f, 0.5f);
                d.Thump(0.34f, 0.6f, 150f, 26f, 1.0f, 0.9f);
                d.Noise(0.36f, 0.6f, Filter.Band, 1800f, 600f, 0.4f, 1.1f, 0.01f, 1.6f, 26f, 0.6f);
                d.Crackle(0.4f, 0.55f, 18, 2200f, 0.4f);
                return Finish(d.S, 0.46f);
            case "use_leaf_tornado":
                d = new SoundDesign(0.95f, 0x1EAFu);
                d.Noise(0, 0.5f, Filter.Band, 600f, 2400f, 0.4f, 1.1f, 0.3f, 1.0f, 16f, 0.6f);
                d.Tone(0, 0.5f, Wave.Triangle, N(55), N(70), 0.07f, 0.2f, 1.2f, vibratoHz: 12f, vibratoDepth: 0.06f);
                d.Thump(0.34f, 0.3f, 150f, 50f, 0.7f, 0.5f);
                d.Noise(0.36f, 0.55f, Filter.High, 3000f, 6500f, 0.35f, 0.9f, 0.05f, 1.3f, 28f, 0.7f);
                d.Notes(0.5f, 0.07f, 0.2f, Wave.Triangle, Pitches(76, 72, 69), 0.05f, 0.01f);
                return Finish(d.S, 0.4f);
            case "use_steamroller":
                d = new SoundDesign(1.0f, 0x57EAu);
                d.Tone(0, 0.4f, Wave.Saw, N(38), N(50), 0.12f, 0.2f, 1.0f, vibratoHz: 22f, vibratoDepth: 0.1f);
                for (int i = 0; i < 6; i++) d.Thump(0.05f + i * 0.055f, 0.08f, 180f, 90f, 0.4f, 0.3f);
                d.Thump(0.34f, 0.5f, 140f, 28f, 1.0f, 0.9f);
                d.Noise(0.35f, 0.4f, Filter.Low, 1500f, 400f, 0.4f, 0.9f, 0.01f, 1.8f);
                d.Notes(0.5f, 0.06f, 0.14f, Wave.Pulse, Pitches(67, 64, 60), 0.05f, 0.01f);
                return Finish(d.S, 0.44f);
            case "use_cotton_guard":
                d = new SoundDesign(0.95f, 0xC077u);
                d.Noise(0, 0.6f, Filter.Low, 2200f, 900f, 0.35f, 0.7f, 0.3f, 1.0f, 8f, 0.3f);
                d.Tone(0, 0.8f, Wave.Triangle, N(55), N(67), 0.09f, 0.3f, 1.0f);
                d.Notes(0.2f, 0.09f, 0.25f, Wave.Sine, Pitches(72, 76, 79, 84), 0.07f, 0.02f);
                d.Bell(0.55f, 0.4f, N(91), 2.01f, 0.8f, 0.09f, 3f);
                return Finish(d.S, 0.34f);
            case "use_night_daze":
                d = new SoundDesign(1.0f, 0x2D4Eu);
                d.Tone(0, 0.5f, Wave.Sine, N(40), N(33), 0.14f, 0.1f, 1.2f, vibratoHz: 6f, vibratoDepth: 0.05f);
                d.Thump(0.1f, 0.3f, 100f, 40f, 0.6f, 0.2f);
                d.Thump(0.22f, 0.3f, 110f, 40f, 0.7f, 0.2f);
                d.Thump(0.34f, 0.5f, 130f, 28f, 1.0f, 0.6f);
                d.Noise(0.35f, 0.5f, Filter.Band, 1200f, 300f, 0.45f, 1.1f, 0.02f, 1.6f);
                d.Notes(0.5f, 0.1f, 0.25f, Wave.Triangle, Pitches(60, 57, 53), 0.06f, 0.02f);
                return Finish(d.S, 0.42f);
            case "use_psystrike":
                d = new SoundDesign(0.85f, 0x75C1u);
                d.Tone(0, 0.34f, Wave.Sine, N(76), N(103), 0.12f, 0.05f, 1.2f, vibratoHz: 10f, vibratoDepth: 0.04f);
                d.Noise(0.05f, 0.28f, Filter.High, 3000f, 7000f, 0.3f, 1.4f, 0.1f, 1.4f);
                d.Thump(0.34f, 0.3f, 230f, 55f, 0.9f, 0.8f);
                d.Crackle(0.35f, 0.45f, 14, 6800f, 0.35f);
                d.Bell(0.37f, 0.5f, N(95), 3.14f, 1.1f, 0.1f, 3f);
                return Finish(d.S, 0.42f);
            case "use_tail_slap":
                d = new SoundDesign(0.8f, 0x7A11u);
                for (int i = 0; i < 4; i++)
                {
                    d.Noise(0.08f + i * 0.08f, 0.08f, Filter.Band, 1800f + i * 200f, 5000f, 0.4f, 1.3f, 0.05f, 1.8f);
                    d.Thump(0.14f + i * 0.08f, 0.1f, 260f - i * 15f, 110f, 0.5f + i * 0.1f, 0.6f);
                }
                d.Notes(0.5f, 0.05f, 0.1f, Wave.Pulse, Pitches(76, 79, 84), 0.04f, 0.005f);
                return Finish(d.S, 0.4f);
            case "use_hurricane":
                d = new SoundDesign(1.0f, 0x4C1Au);
                d.Noise(0, 0.9f, Filter.Band, 500f, 3000f, 0.5f, 1.2f, 0.4f, 0.9f, 10f, 0.5f);
                d.Tone(0, 0.6f, Wave.Sine, N(55), N(79), 0.1f, 0.3f, 1.0f, vibratoHz: 8f, vibratoDepth: 0.06f);
                d.Thump(0.34f, 0.4f, 160f, 40f, 0.9f, 0.5f);
                d.Notes(0.5f, 0.08f, 0.2f, Wave.Triangle, Pitches(84, 79, 84, 76), 0.05f, 0.01f);
                d.Noise(0.4f, 0.55f, Filter.High, 2500f, 6000f, 0.3f, 1f, 0.05f, 1.4f, 22f, 0.5f);
                return Finish(d.S, 0.44f);
            case "use_head_charge":
                d = new SoundDesign(0.9f, 0x4EADu);
                d.Noise(0, 0.34f, Filter.Band, 500f, 2200f, 0.4f, 1.2f, 0.2f, 1.2f);
                d.Tone(0, 0.34f, Wave.Saw, N(40), N(60), 0.1f, 0.1f, 1.2f);
                d.Thump(0.34f, 0.5f, 170f, 30f, 1.0f, 0.9f);
                d.Tone(0.36f, 0.3f, Wave.Sine, N(55), N(40), 0.1f, 0.01f, 1.6f);
                d.Thump(0.52f, 0.2f, 140f, 50f, 0.5f, 0.4f);
                d.Crackle(0.37f, 0.3f, 8, 3000f, 0.3f);
                return Finish(d.S, 0.46f);
            case "use_gear_grind":
                d = new SoundDesign(0.9f, 0x6EA2u);
                d.Noise(0, 0.2f, Filter.High, 3000f, 6000f, 0.35f, 1.2f, 0.1f, 1.4f, 50f, 0.8f);
                d.Thump(0.2f, 0.2f, 200f, 70f, 0.7f, 0.7f);
                d.Crackle(0.21f, 0.2f, 12, 4800f, 0.4f);
                d.Thump(0.35f, 0.35f, 190f, 50f, 0.9f, 0.8f);
                d.Crackle(0.36f, 0.4f, 16, 5200f, 0.4f);
                d.Tone(0.2f, 0.5f, Wave.Saw, N(70), N(76), 0.05f, 0.05f, 1.6f, vibratoHz: 40f, vibratoDepth: 0.1f);
                d.Bell(0.38f, 0.5f, N(88), 3.01f, 1f, 0.09f, 3f);
                return Finish(d.S, 0.42f);
            case "use_searing_shot":
                d = new SoundDesign(0.95f, 0x5EA2u);
                for (int i = 0; i < 6; i++) d.Noise(0.02f + i * 0.04f, 0.15f, Filter.Band, 900f + i * 300f, 2500f, 0.3f, 1f, 0.1f, 1.6f);
                d.Tone(0, 0.4f, Wave.Saw, N(45), N(62), 0.09f, 0.1f, 1.2f, vibratoHz: 14f, vibratoDepth: 0.06f);
                d.Thump(0.34f, 0.4f, 150f, 40f, 0.9f, 0.7f);
                d.Crackle(0.35f, 0.55f, 22, 2800f, 0.4f);
                d.Noise(0.4f, 0.5f, Filter.High, 3500f, 5500f, 0.2f, 1f, 0.05f, 1.4f, 24f, 0.7f);
                return Finish(d.S, 0.42f);
            case "use_techno_blast":
                d = new SoundDesign(0.9f, 0x7EC4u);
                d.Notes(0, 0.04f, 0.08f, Wave.Pulse, Pitches(84, 88, 91, 96, 91, 96, 100, 103), 0.07f, 0.005f, 2f, 0.25f);
                d.Tone(0.1f, 0.3f, Wave.Pulse, N(60), N(84), 0.08f, 0.05f, 1.2f, 0.12f);
                d.Thump(0.34f, 0.3f, 220f, 55f, 0.8f, 0.7f);
                d.Notes(0.36f, 0.035f, 0.08f, Wave.Pulse, Pitches(103, 100, 96, 91, 88, 84), 0.06f, 0.005f, 2f, 0.25f);
                d.Bell(0.4f, 0.4f, N(100), 2.01f, 0.8f, 0.07f, 3f);
                return Finish(d.S, 0.4f);
            case "use_relic_song":
                d = new SoundDesign(1.0f, 0x2E11u);
                d.Notes(0, 0.18f, 0.4f, Wave.Triangle, Pitches(67, 71, 74, 79, 74), 0.08f, 0.03f);
                d.Tone(0, 0.9f, Wave.Sine, N(43), N(43), 0.1f, 0.3f, 1.0f, vibratoHz: 4f, vibratoDepth: 0.02f);
                d.Bell(0.34f, 0.6f, N(86), 2.01f, 0.9f, 0.1f, 3f);
                d.Thump(0.35f, 0.25f, 120f, 50f, 0.5f, 0.3f);
                d.Notes(0.55f, 0.1f, 0.25f, Wave.Sine, Pitches(79, 74, 71), 0.05f, 0.02f);
                return Finish(d.S, 0.36f);
            case "use_secret_sword":
                d = new SoundDesign(0.95f, 0x5EC2u);
                d.Tone(0, 0.3f, Wave.Sine, N(72), N(91), 0.1f, 0.08f, 1.2f);
                d.Noise(0.1f, 0.25f, Filter.High, 3000f, 7000f, 0.45f, 1.3f, 0.2f, 1.6f);
                d.Thump(0.34f, 0.35f, 190f, 45f, 1.0f, 0.8f);
                d.Noise(0.35f, 0.2f, Filter.High, 3500f, 8000f, 0.5f, 1.4f, 0.01f, 1.8f);
                d.Bell(0.37f, 0.55f, N(84), 2.4f, 1.2f, 0.12f, 3f);
                d.Notes(0.5f, 0.06f, 0.15f, Wave.Pulse, Pitches(79, 84, 88), 0.05f, 0.01f);
                return Finish(d.S, 0.42f);
            case "use_glaciate":
                d = new SoundDesign(1.0f, 0x61AAu);
                d.Noise(0, 0.5f, Filter.High, 1800f, 5500f, 0.4f, 0.9f, 0.3f, 1.1f, 12f, 0.4f);
                d.Tone(0, 0.5f, Wave.Sine, N(70), N(62), 0.08f, 0.2f, 1.2f);
                d.Thump(0.34f, 0.3f, 180f, 50f, 0.7f, 0.5f);
                d.Crackle(0.36f, 0.5f, 18, 6500f, 0.4f);
                d.Notes(0.45f, 0.06f, 0.2f, Wave.Sine, Pitches(96, 100, 103, 108), 0.05f, 0.005f);
                d.Bell(0.4f, 0.5f, N(103), 3.01f, 0.9f, 0.08f, 3f);
                return Finish(d.S, 0.38f);
            case "use_bolt_strike":
                d = new SoundDesign(1.0f, 0xB017u);
                d.Tone(0, 0.14f, Wave.Pulse, N(55), N(100), 0.08f, 0.02f, 1.4f, 0.2f);
                d.Crackle(0.12f, 0.24f, 16, 6200f, 0.4f);
                d.Thump(0.34f, 0.65f, 170f, 24f, 1.0f, 1.0f);
                d.Crackle(0.35f, 0.55f, 26, 5000f, 0.5f);
                d.Noise(0.36f, 0.5f, Filter.Band, 3500f, 700f, 0.5f, 1f, 0.01f, 1.5f);
                d.Tone(0.4f, 0.55f, Wave.Saw, N(36), N(31), 0.1f, 0.05f, 1.6f, vibratoHz: 30f, vibratoDepth: 0.05f);
                return Finish(d.S, 0.46f);
            case "use_blue_flare":
                d = new SoundDesign(1.0f, 0xB1FAu);
                d.Noise(0, 0.4f, Filter.Band, 700f, 2200f, 0.5f, 1.1f, 0.3f, 1.0f, 18f, 0.5f);
                d.Tone(0, 0.4f, Wave.Saw, N(40), N(57), 0.12f, 0.2f, 1.1f, vibratoHz: 13f, vibratoDepth: 0.06f);
                d.Thump(0.34f, 0.55f, 150f, 28f, 1.0f, 0.9f);
                d.Noise(0.35f, 0.6f, Filter.Band, 2200f, 500f, 0.5f, 1f, 0.01f, 1.5f, 20f, 0.5f);
                d.Bell(0.38f, 0.55f, N(79), 2.01f, 1.0f, 0.1f, 3f);
                d.Crackle(0.45f, 0.45f, 14, 2500f, 0.3f);
                return Finish(d.S, 0.46f);
            case "use_fiery_dance":
                d = new SoundDesign(0.95f, 0xF1D2u);
                d.Notes(0, 0.07f, 0.14f, Wave.Pulse, Pitches(64, 67, 71, 67, 71, 74), 0.06f, 0.01f, 2f, 0.3f);
                d.Noise(0, 0.4f, Filter.Band, 800f, 2500f, 0.35f, 1f, 0.2f, 1.2f, 20f, 0.6f);
                d.Thump(0.34f, 0.35f, 160f, 40f, 0.8f, 0.6f);
                d.Crackle(0.36f, 0.5f, 18, 2800f, 0.35f);
                d.Notes(0.5f, 0.07f, 0.18f, Wave.Triangle, Pitches(79, 83, 86), 0.06f, 0.01f);
                return Finish(d.S, 0.4f);
            case "use_freeze_shock":
                d = new SoundDesign(1.0f, 0xF52Cu);
                d.Tone(0, 0.32f, Wave.Sine, N(96), N(72), 0.09f, 0.05f, 1.4f);
                d.Crackle(0.05f, 0.3f, 14, 6000f, 0.3f);
                d.Thump(0.34f, 0.5f, 170f, 30f, 1.0f, 0.9f);
                d.Crackle(0.35f, 0.5f, 22, 5500f, 0.45f);
                d.Noise(0.36f, 0.4f, Filter.High, 4000f, 8000f, 0.3f, 1f, 0.01f, 1.6f);
                d.Bell(0.38f, 0.5f, N(100), 2.76f, 1.0f, 0.09f, 3f);
                return Finish(d.S, 0.44f);
            case "use_ice_burn":
                d = new SoundDesign(1.0f, 0x1CEBu);
                d.Noise(0, 0.45f, Filter.Band, 1000f, 3200f, 0.4f, 1.2f, 0.3f, 1.0f, 20f, 0.6f);
                d.Tone(0, 0.45f, Wave.Saw, N(60), N(72), 0.08f, 0.2f, 1.1f, vibratoHz: 11f, vibratoDepth: 0.05f);
                d.Thump(0.34f, 0.4f, 170f, 40f, 0.8f, 0.6f);
                d.Crackle(0.36f, 0.5f, 18, 4000f, 0.35f);
                d.Bell(0.38f, 0.6f, N(98), 3.01f, 1f, 0.1f, 3f);
                d.Noise(0.4f, 0.5f, Filter.High, 5000f, 7500f, 0.2f, 1f, 0.05f, 1.4f);
                return Finish(d.S, 0.42f);
            case "use_snarl":
                d = new SoundDesign(0.95f, 0x54A2u);
                d.Tone(0, 0.5f, Wave.Saw, N(43), N(38), 0.14f, 0.05f, 1.0f, vibratoHz: 30f, vibratoDepth: 0.14f);
                d.Noise(0, 0.5f, Filter.Band, 400f, 1200f, 0.4f, 1.4f, 0.05f, 1.0f, 26f, 0.7f);
                d.Thump(0.34f, 0.3f, 140f, 45f, 0.7f, 0.5f);
                d.Tone(0.2f, 0.4f, Wave.Pulse, N(62), N(55), 0.05f, 0.05f, 1.4f, 0.2f, vibratoHz: 22f, vibratoDepth: 0.08f);
                d.Notes(0.5f, 0.1f, 0.25f, Wave.Triangle, Pitches(55, 52, 48), 0.06f, 0.02f);
                return Finish(d.S, 0.42f);
            case "use_icicle_crash":
                d = new SoundDesign(0.9f, 0x1C1Cu);
                for (int i = 0; i < 3; i++)
                {
                    d.Noise(0.12f + i * 0.05f, 0.14f, Filter.High, 5000f, 2500f, 0.35f, 1.2f, 0.2f, 1.8f);
                    d.Thump(0.26f + i * 0.05f, 0.12f, 220f - i * 15f, 70f, 0.6f, 0.6f);
                    d.Bell(0.27f + i * 0.05f, 0.35f, N(96 + i * 4), 3.01f, 0.9f, 0.07f, 3f);
                }
                d.Thump(0.36f, 0.3f, 190f, 40f, 0.9f, 0.8f);
                d.Crackle(0.38f, 0.45f, 18, 6800f, 0.35f);
                return Finish(d.S, 0.42f);
            case "use_v_create":
                d = new SoundDesign(1.0f, 0x7C4Eu);
                d.Tone(0, 0.34f, Wave.Saw, N(48), N(72), 0.12f, 0.1f, 1.0f, vibratoHz: 12f, vibratoDepth: 0.05f);
                d.Noise(0, 0.34f, Filter.Band, 600f, 2800f, 0.5f, 1.1f, 0.2f, 1.0f, 18f, 0.5f);
                d.Thump(0.34f, 0.7f, 140f, 22f, 1.0f, 1.0f);
                d.Noise(0.35f, 0.6f, Filter.Low, 2500f, 500f, 0.6f, 1f, 0.01f, 1.4f);
                d.Crackle(0.38f, 0.55f, 22, 2500f, 0.4f);
                d.Tone(0.5f, 0.45f, Wave.Sine, N(67), N(48), 0.09f, 0.05f, 1.6f);
                return Finish(d.S, 0.46f);
            case "use_fusion_flare":
                d = new SoundDesign(1.0f, 0xF51Au);
                d.Tone(0, 0.34f, Wave.Saw, N(70), N(45), 0.1f, 0.05f, 1.2f, vibratoHz: 10f, vibratoDepth: 0.06f);
                d.Noise(0, 0.34f, Filter.Band, 2500f, 800f, 0.45f, 1f, 0.1f, 1.2f, 20f, 0.5f);
                d.Thump(0.34f, 0.6f, 150f, 26f, 1.0f, 0.9f);
                d.Noise(0.35f, 0.55f, Filter.Band, 2200f, 600f, 0.45f, 1f, 0.01f, 1.5f, 22f, 0.5f);
                d.Crackle(0.38f, 0.5f, 18, 3000f, 0.4f);
                d.Bell(0.4f, 0.4f, N(88), 1.41f, 0.9f, 0.07f, 3f);
                return Finish(d.S, 0.44f);
            case "use_fusion_bolt":
                d = new SoundDesign(1.0f, 0xF580u);
                d.Tone(0, 0.34f, Wave.Pulse, N(60), N(96), 0.08f, 0.05f, 1.2f, 0.2f, vibratoHz: 22f, vibratoDepth: 0.06f);
                d.Crackle(0.02f, 0.32f, 18, 5500f, 0.35f);
                d.Thump(0.34f, 0.45f, 190f, 34f, 1.0f, 0.9f);
                d.Crackle(0.35f, 0.5f, 22, 4800f, 0.45f);
                d.Bell(0.37f, 0.55f, N(96), 2.01f, 1.0f, 0.1f, 3f);
                d.Noise(0.4f, 0.4f, Filter.Band, 3000f, 800f, 0.25f, 1f, 0.02f, 1.5f);
                return Finish(d.S, 0.44f);
        }
        return MakeMoveSoundY12(name);
    }
}
