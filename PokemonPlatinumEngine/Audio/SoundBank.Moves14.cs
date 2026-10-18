using System;
using static PokemonPlatinumEngine.Audio.SoundDesign;

namespace PokemonPlatinumEngine.Audio;

// Plan 25 - Y13a: the sounds of the later games' moves, Generation 7's regular moves from Shore Up to Double Iron Bash.
// No game's sound is the model: each is our own, built from the bank's shared layers and timed to its look (the blow at
// 0.35 s). Every one runs to at most a second.
public static partial class SoundBank
{
    /// <summary>A sound of Y13, or null for a name that isn't one.</summary>
    private static float[]? MakeMoveSoundY13(string name)
    {
        SoundDesign d;
        switch (name)
        {
            case "use_shore_up":
                d = new SoundDesign(1.0f, 0x5B01u);
                d.Noise(0, 0.5f, Filter.Band, 600f, 1800f, 0.4f, 1f, 0.3f, 1.0f, 10f, 0.5f);
                d.Thump(0.28f, 0.2f, 160f, 60f, 0.5f, 0.3f);
                d.Notes(0.4f, 0.1f, 0.2f, Wave.Sine, Pitches(67, 71, 74, 79), 0.07f, 0.02f);
                d.Bell(0.55f, 0.4f, N(86), 2.01f, 0.8f, 0.07f, 3f);
                return Finish(d.S, 0.38f);
            case "use_first_impression":
                d = new SoundDesign(0.8f, 0xF1A2u);
                d.Noise(0.1f, 0.1f, Filter.High, 5000f, 8500f, 0.5f, 1.4f, 0.05f, 1.8f);
                d.Thump(0.3f, 0.3f, 220f, 50f, 0.9f, 0.9f);
                d.Noise(0.32f, 0.12f, Filter.High, 4000f, 8000f, 0.5f, 1.4f, 0.05f, 1.8f);
                d.Notes(0.4f, 0.05f, 0.1f, Wave.Pulse, Pitches(88, 93), 0.05f, 0.005f);
                return Finish(d.S, 0.42f);
            case "use_baneful_bunker":
                d = new SoundDesign(0.95f, 0xBB03u);
                d.Thump(0.04f, 0.3f, 130f, 50f, 0.6f, 0.4f);
                d.Tone(0.04f, 0.6f, Wave.Triangle, N(50), N(53), 0.09f, 0.1f, 1.1f, vibratoHz: 7f, vibratoDepth: 0.05f);
                d.Bubbles(0.2f, 0.6f, 7, 200f, 640f, 0.32f);
                d.Bell(0.34f, 0.5f, N(62), 1.41f, 0.9f, 0.08f, 2.5f);
                return Finish(d.S, 0.4f);
            case "use_spirit_shackle":
                d = new SoundDesign(1.0f, 0x5504u);
                d.Noise(0, 0.3f, Filter.High, 6000f, 2000f, 0.35f, 1f, 0.05f, 1.2f, 30f, 0.4f);
                d.Thump(0.33f, 0.3f, 200f, 40f, 0.9f, 0.8f);
                for (int i = 0; i < 3; i++) d.Bell(0.38f + i * 0.1f, 0.3f, N(58 - i * 2), 2.4f, 1.0f, 0.07f, 3f);
                d.Tone(0.35f, 0.6f, Wave.Sine, N(48), N(40), 0.08f, 0.1f, 1.2f, vibratoHz: 6f, vibratoDepth: 0.07f);
                return Finish(d.S, 0.42f);
            case "use_darkest_lariat":
                d = new SoundDesign(1.0f, 0xDA05u);
                d.Noise(0.05f, 0.35f, Filter.Band, 400f, 2400f, 0.5f, 1f, 0.2f, 1.1f, 18f, 0.7f);
                d.Tone(0.05f, 0.35f, Wave.Saw, N(40), N(52), 0.1f, 0.1f, 1.0f, vibratoHz: 20f, vibratoDepth: 0.08f);
                d.Thump(0.36f, 0.4f, 160f, 28f, 1.0f, 0.9f);
                d.Noise(0.4f, 0.4f, Filter.Low, 1600f, 400f, 0.35f, 1f, 0.03f, 1.3f);
                return Finish(d.S, 0.44f);
            case "use_sparkling_aria":
                d = new SoundDesign(1.0f, 0x5A06u);
                d.Notes(0, 0.1f, 0.24f, Wave.Sine, Pitches(72, 76, 79, 84, 83, 79), 0.08f, 0.03f);
                d.Bubbles(0.1f, 0.5f, 8, 400f, 1400f, 0.3f);
                d.Thump(0.36f, 0.2f, 170f, 70f, 0.5f, 0.3f);
                d.Bell(0.4f, 0.5f, N(91), 2.76f, 0.9f, 0.08f, 3f);
                d.Notes(0.6f, 0.07f, 0.15f, Wave.Triangle, Pitches(88, 91, 95), 0.04f, 0.01f);
                return Finish(d.S, 0.4f);
            case "use_ice_hammer":
                d = new SoundDesign(1.0f, 0x1C07u);
                d.Tone(0, 0.32f, Wave.Sine, N(90), N(60), 0.08f, 0.1f, 1.3f);
                d.Thump(0.34f, 0.5f, 170f, 26f, 1.0f, 1.0f);
                d.Crackle(0.36f, 0.5f, 18, 6500f, 0.4f);
                d.Bell(0.4f, 0.5f, N(88), 1.41f, 1.0f, 0.09f, 3f);
                d.Notes(0.6f, 0.07f, 0.12f, Wave.Sine, Pitches(79, 74, 70), 0.04f, 0.01f);
                return Finish(d.S, 0.44f);
            case "use_floral_healing":
                d = new SoundDesign(1.0f, 0xF108u);
                d.Notes(0, 0.12f, 0.3f, Wave.Sine, Pitches(72, 76, 79, 83, 88), 0.08f, 0.03f);
                d.Bell(0.1f, 0.6f, N(84), 3.0f, 0.7f, 0.07f, 2.5f);
                d.Noise(0.2f, 0.5f, Filter.High, 3500f, 6500f, 0.15f, 1f, 0.2f, 1.3f, 14f, 0.5f);
                d.Tone(0.35f, 0.6f, Wave.Triangle, N(60), N(72), 0.06f, 0.1f, 1.0f);
                return Finish(d.S, 0.34f);
            case "use_high_horsepower":
                d = new SoundDesign(0.95f, 0x4809u);
                d.Thump(0.2f, 0.2f, 190f, 60f, 0.7f, 0.7f);
                d.Thump(0.34f, 0.5f, 150f, 24f, 1.0f, 1.0f);
                d.Crackle(0.36f, 0.4f, 14, 1700f, 0.4f);
                d.Noise(0.36f, 0.45f, Filter.Band, 900f, 300f, 0.5f, 1f, 0.01f, 1.4f, 24f, 0.5f);
                d.Notes(0.5f, 0.05f, 0.12f, Wave.Pulse, Pitches(55, 52), 0.05f, 0.01f);
                return Finish(d.S, 0.46f);
            case "use_strength_sap":
                d = new SoundDesign(1.0f, 0x550Au);
                d.Tone(0.05f, 0.5f, Wave.Sine, N(76), N(52), 0.09f, 0.1f, 1.1f, vibratoHz: 9f, vibratoDepth: 0.05f);
                d.Noise(0.05f, 0.5f, Filter.Band, 1800f, 700f, 0.3f, 1f, 0.3f, 1.2f, 16f, 0.5f);
                d.Bubbles(0.2f, 0.4f, 5, 300f, 900f, 0.25f);
                d.Tone(0.55f, 0.4f, Wave.Triangle, N(60), N(72), 0.08f, 0.1f, 1.2f);
                d.Notes(0.7f, 0.07f, 0.14f, Wave.Sine, Pitches(79, 84), 0.05f, 0.01f);
                return Finish(d.S, 0.34f);
            case "use_solar_blade":
                d = new SoundDesign(1.0f, 0x5B0Bu);
                d.Tone(0, 0.3f, Wave.Sine, N(64), N(88), 0.1f, 0.1f, 1.2f);
                d.Notes(0.02f, 0.06f, 0.16f, Wave.Triangle, Pitches(76, 79, 83, 88), 0.05f, 0.01f);
                d.Noise(0.28f, 0.14f, Filter.High, 3000f, 9000f, 0.55f, 1.4f, 0.03f, 1.8f);
                d.Thump(0.35f, 0.4f, 200f, 40f, 0.8f, 0.8f);
                d.Bell(0.4f, 0.5f, N(86), 2.0f, 1.0f, 0.09f, 3f);
                return Finish(d.S, 0.44f);
            case "use_leafage":
                d = new SoundDesign(0.75f, 0x1E0Cu);
                for (int i = 0; i < 4; i++) d.Noise(0.05f + i * 0.07f, 0.1f, Filter.High, 4000f + i * 300f, 7000f, 0.3f, 1f, 0.05f, 1.6f, 30f, 0.4f);
                d.Thump(0.32f, 0.2f, 230f, 90f, 0.5f, 0.5f);
                d.Notes(0.4f, 0.05f, 0.1f, Wave.Triangle, Pitches(81, 84), 0.04f, 0.01f);
                return Finish(d.S, 0.36f);
            case "use_spotlight":
                d = new SoundDesign(0.95f, 0x500Du);
                d.Thump(0.02f, 0.12f, 200f, 90f, 0.7f, 0.9f);
                d.Tone(0.08f, 0.6f, Wave.Sine, N(72), N(79), 0.08f, 0.05f, 1.2f, vibratoHz: 6f, vibratoDepth: 0.03f);
                d.Notes(0.2f, 0.09f, 0.2f, Wave.Pulse, Pitches(84, 88, 91, 96), 0.05f, 0.01f);
                d.Bell(0.4f, 0.5f, N(96), 2.0f, 0.8f, 0.07f, 3f);
                return Finish(d.S, 0.36f);
            case "use_toxic_thread":
                d = new SoundDesign(0.95f, 0x7D0Eu);
                d.Noise(0, 0.35f, Filter.High, 3000f, 6500f, 0.3f, 1f, 0.1f, 1.2f, 40f, 0.4f);
                d.Tone(0.05f, 0.6f, Wave.Saw, N(70), N(64), 0.05f, 0.1f, 1.1f, vibratoHz: 14f, vibratoDepth: 0.05f);
                d.Bubbles(0.3f, 0.4f, 5, 220f, 700f, 0.28f);
                d.Thump(0.34f, 0.2f, 170f, 70f, 0.4f, 0.3f);
                d.Notes(0.5f, 0.07f, 0.14f, Wave.Pulse, Pitches(64, 62, 59), 0.05f, 0.01f);
                return Finish(d.S, 0.36f);
            case "use_laser_focus":
                d = new SoundDesign(0.95f, 0x1F0Fu);
                d.Tone(0, 0.45f, Wave.Sine, N(66), N(94), 0.08f, 0.1f, 1.2f);
                d.Noise(0.05f, 0.4f, Filter.High, 2500f, 7500f, 0.2f, 1f, 0.3f, 1.2f, 16f, 0.5f);
                d.Thump(0.45f, 0.1f, 260f, 120f, 0.5f, 0.6f);
                d.Tone(0.46f, 0.4f, Wave.Pulse, N(96), N(96), 0.05f, 0.01f, 1.8f);
                return Finish(d.S, 0.36f);
            case "use_gear_up":
                d = new SoundDesign(1.0f, 0x6E10u);
                for (int i = 0; i < 6; i++) d.Thump(0.04f + i * 0.07f, 0.06f, 300f, 160f, 0.35f, 0.9f);
                d.Tone(0.02f, 0.5f, Wave.Saw, N(46), N(58), 0.08f, 0.1f, 1.1f, vibratoHz: 24f, vibratoDepth: 0.07f);
                d.Bell(0.45f, 0.4f, N(77), 1.41f, 0.9f, 0.08f, 3f);
                d.Notes(0.6f, 0.07f, 0.14f, Wave.Triangle, Pitches(67, 72, 76), 0.05f, 0.01f);
                return Finish(d.S, 0.4f);
            case "use_throat_chop":
                d = new SoundDesign(0.85f, 0x7C11u);
                d.Noise(0.12f, 0.2f, Filter.High, 3500f, 7500f, 0.5f, 1.3f, 0.05f, 1.8f);
                d.Thump(0.34f, 0.35f, 190f, 40f, 0.95f, 0.9f);
                d.Tone(0.38f, 0.1f, Wave.Saw, N(70), N(34), 0.08f, 0.005f, 3f);
                d.Notes(0.5f, 0.06f, 0.1f, Wave.Pulse, Pitches(58, 55), 0.05f, 0.01f);
                return Finish(d.S, 0.42f);
            case "use_pollen_puff":
                d = new SoundDesign(1.0f, 0x9012u);
                d.Tone(0, 0.32f, Wave.Sine, N(62), N(78), 0.08f, 0.1f, 1.2f);
                d.Noise(0.05f, 0.3f, Filter.Band, 1500f, 900f, 0.3f, 1f, 0.2f, 1.1f, 12f, 0.5f);
                d.Thump(0.34f, 0.3f, 160f, 60f, 0.6f, 0.4f);
                d.Noise(0.36f, 0.5f, Filter.High, 2500f, 5500f, 0.25f, 1f, 0.05f, 1.3f, 22f, 0.5f);
                d.Notes(0.5f, 0.08f, 0.16f, Wave.Triangle, Pitches(79, 83, 88), 0.05f, 0.01f);
                return Finish(d.S, 0.38f);
            case "use_anchor_shot":
                d = new SoundDesign(1.0f, 0xA013u);
                d.Noise(0, 0.28f, Filter.Band, 800f, 3000f, 0.35f, 1f, 0.1f, 1.1f, 28f, 0.7f);
                d.Thump(0.33f, 0.55f, 150f, 22f, 1.0f, 1.0f);
                d.Bell(0.34f, 0.5f, N(52), 1.41f, 1.1f, 0.1f, 2.5f);
                for (int i = 0; i < 4; i++) d.Thump(0.5f + i * 0.07f, 0.05f, 320f - i * 30f, 200f, 0.3f, 0.8f);
                return Finish(d.S, 0.44f);
            case "use_psychic_terrain":
                d = new SoundDesign(1.0f, 0x7E14u);
                d.Tone(0, 0.9f, Wave.Sine, N(55), N(58), 0.09f, 0.3f, 1.0f, vibratoHz: 5f, vibratoDepth: 0.09f);
                d.Tone(0.1f, 0.8f, Wave.Sine, N(67), N(70), 0.06f, 0.3f, 1.0f, vibratoHz: 7f, vibratoDepth: 0.06f);
                d.Notes(0.25f, 0.15f, 0.3f, Wave.Triangle, Pitches(79, 84, 83, 88), 0.05f, 0.02f);
                d.Bell(0.5f, 0.45f, N(91), 3.0f, 0.7f, 0.07f, 2.5f);
                return Finish(d.S, 0.34f);
            case "use_lunge":
                d = new SoundDesign(0.85f, 0x1015u);
                d.Noise(0, 0.3f, Filter.High, 2500f, 6500f, 0.45f, 1f, 0.15f, 1.2f, 22f, 0.5f);
                d.Thump(0.33f, 0.3f, 210f, 50f, 0.9f, 0.9f);
                d.Notes(0.36f, 0.04f, 0.1f, Wave.Pulse, Pitches(72, 67), 0.05f, 0.005f);
                d.Tone(0.45f, 0.35f, Wave.Sine, N(64), N(52), 0.06f, 0.05f, 1.3f);
                return Finish(d.S, 0.42f);
            case "use_fire_lash":
                d = new SoundDesign(0.9f, 0xF016u);
                d.Noise(0, 0.3f, Filter.High, 2000f, 7500f, 0.5f, 1.3f, 0.05f, 1.6f, 34f, 0.7f);
                d.Noise(0.2f, 0.4f, Filter.Band, 700f, 2200f, 0.35f, 1f, 0.2f, 1.1f, 20f, 0.6f);
                d.Thump(0.34f, 0.3f, 210f, 60f, 0.8f, 0.8f);
                d.Crackle(0.36f, 0.4f, 16, 3000f, 0.35f);
                d.Tone(0.5f, 0.35f, Wave.Sine, N(72), N(55), 0.05f, 0.05f, 1.3f);
                return Finish(d.S, 0.42f);
            case "use_power_trip":
                d = new SoundDesign(1.0f, 0x7017u);
                d.Tone(0, 0.32f, Wave.Saw, N(40), N(62), 0.1f, 0.1f, 1.0f, vibratoHz: 16f, vibratoDepth: 0.07f);
                d.Notes(0.1f, 0.07f, 0.12f, Wave.Pulse, Pitches(60, 64, 67, 72), 0.05f, 0.01f);
                d.Thump(0.35f, 0.45f, 170f, 26f, 1.0f, 0.9f);
                d.Noise(0.37f, 0.4f, Filter.Band, 1500f, 400f, 0.4f, 1f, 0.02f, 1.4f, 22f, 0.5f);
                d.Tone(0.4f, 0.5f, Wave.Sine, N(48), N(38), 0.07f, 0.05f, 1.4f);
                return Finish(d.S, 0.44f);
            case "use_burn_up":
                d = new SoundDesign(1.0f, 0xB018u);
                d.Noise(0, 0.6f, Filter.Band, 400f, 3000f, 0.5f, 1.2f, 0.3f, 1.0f, 12f, 0.6f);
                d.Tone(0, 0.35f, Wave.Saw, N(36), N(55), 0.12f, 0.15f, 1.0f, vibratoHz: 12f, vibratoDepth: 0.08f);
                d.Thump(0.35f, 0.55f, 130f, 22f, 1.0f, 1.0f);
                d.Crackle(0.4f, 0.5f, 22, 2500f, 0.45f);
                d.Noise(0.7f, 0.3f, Filter.Low, 1600f, 300f, 0.3f, 1f, 0.05f, 1.3f);
                return Finish(d.S, 0.46f);
            case "use_speed_swap":
                d = new SoundDesign(0.9f, 0x5019u);
                d.Tone(0, 0.4f, Wave.Sine, N(60), N(90), 0.08f, 0.05f, 1.2f);
                d.Tone(0, 0.4f, Wave.Sine, N(90), N(60), 0.08f, 0.05f, 1.2f);
                d.Noise(0.05f, 0.35f, Filter.High, 3000f, 7000f, 0.3f, 1f, 0.2f, 1.2f, 30f, 0.5f);
                d.Bell(0.42f, 0.4f, N(84), 1.5f, 0.8f, 0.07f, 3f);
                d.Notes(0.55f, 0.06f, 0.12f, Wave.Triangle, Pitches(88, 84, 91), 0.04f, 0.01f);
                return Finish(d.S, 0.34f);
            case "use_smart_strike":
                d = new SoundDesign(0.85f, 0x501Au);
                d.Notes(0, 0.07f, 0.1f, Wave.Pulse, Pitches(91, 91, 96), 0.05f, 0.005f);
                d.Noise(0.2f, 0.12f, Filter.High, 5000f, 9000f, 0.5f, 1.4f, 0.03f, 1.8f);
                d.Thump(0.34f, 0.35f, 230f, 45f, 0.95f, 1.0f);
                d.Bell(0.36f, 0.5f, N(84), 3.14f, 1.0f, 0.1f, 3f);
                return Finish(d.S, 0.42f);
            case "use_purify":
                d = new SoundDesign(1.0f, 0xB01Bu);
                d.Bell(0, 0.5f, N(96), 2.0f, 0.6f, 0.08f, 2.5f);
                d.Notes(0.1f, 0.12f, 0.3f, Wave.Sine, Pitches(79, 83, 86, 91), 0.07f, 0.03f);
                d.Noise(0.2f, 0.5f, Filter.High, 3000f, 6000f, 0.18f, 1f, 0.25f, 1.3f, 12f, 0.4f);
                d.Tone(0.5f, 0.45f, Wave.Triangle, N(60), N(74), 0.07f, 0.1f, 1.1f);
                d.Notes(0.7f, 0.07f, 0.14f, Wave.Sine, Pitches(88, 93), 0.04f, 0.01f);
                return Finish(d.S, 0.34f);
            case "use_revelation_dance":
                d = new SoundDesign(1.0f, 0x4D1Cu);
                d.Notes(0, 0.1f, 0.2f, Wave.Triangle, Pitches(67, 71, 74, 79, 74, 71, 76), 0.07f, 0.01f);
                d.Thump(0.05f, 0.1f, 180f, 80f, 0.4f, 0.4f);
                d.Thump(0.25f, 0.1f, 180f, 80f, 0.4f, 0.4f);
                d.Thump(0.36f, 0.3f, 190f, 50f, 0.7f, 0.6f);
                d.Bell(0.45f, 0.5f, N(83), 2.0f, 0.9f, 0.08f, 3f);
                return Finish(d.S, 0.4f);
            case "use_core_enforcer":
                d = new SoundDesign(1.0f, 0xCE1Du);
                d.Tone(0, 0.5f, Wave.Saw, N(38), N(60), 0.11f, 0.1f, 1.1f, vibratoHz: 9f, vibratoDepth: 0.06f);
                d.Noise(0.05f, 0.45f, Filter.Band, 600f, 3000f, 0.45f, 1.1f, 0.2f, 1.1f, 14f, 0.5f);
                d.Thump(0.36f, 0.5f, 160f, 26f, 1.0f, 0.9f);
                d.Tone(0.4f, 0.4f, Wave.Pulse, N(72), N(48), 0.06f, 0.01f, 1.6f);
                d.Bell(0.45f, 0.45f, N(60), 2.4f, 1.0f, 0.08f, 3f);
                return Finish(d.S, 0.44f);
            case "use_trop_kick":
                d = new SoundDesign(0.9f, 0x701Eu);
                d.Noise(0.05f, 0.25f, Filter.High, 2000f, 5500f, 0.4f, 1f, 0.2f, 1.2f, 22f, 0.5f);
                d.Thump(0.33f, 0.35f, 220f, 45f, 0.95f, 0.9f);
                d.Notes(0.38f, 0.07f, 0.15f, Wave.Triangle, Pitches(76, 81, 84), 0.06f, 0.01f);
                d.Tone(0.48f, 0.35f, Wave.Sine, N(70), N(52), 0.05f, 0.05f, 1.3f);
                return Finish(d.S, 0.42f);
            case "use_instruct":
                d = new SoundDesign(0.95f, 0x1A1Fu);
                d.Notes(0, 0.08f, 0.1f, Wave.Pulse, Pitches(84, 91), 0.06f, 0.005f);
                d.Tone(0.15f, 0.6f, Wave.Sine, N(72), N(76), 0.07f, 0.1f, 1.1f, vibratoHz: 8f, vibratoDepth: 0.06f);
                d.Notes(0.3f, 0.09f, 0.16f, Wave.Triangle, Pitches(79, 83, 79, 83, 86), 0.05f, 0.01f);
                d.Bell(0.55f, 0.4f, N(88), 2.76f, 0.8f, 0.07f, 3f);
                return Finish(d.S, 0.36f);
            case "use_beak_blast":
                d = new SoundDesign(1.0f, 0xBB20u);
                d.Tone(0, 0.3f, Wave.Saw, N(52), N(76), 0.09f, 0.2f, 1.0f, vibratoHz: 18f, vibratoDepth: 0.09f);
                d.Noise(0, 0.3f, Filter.High, 3000f, 6000f, 0.25f, 1f, 0.3f, 1.1f, 20f, 0.6f);
                d.Thump(0.34f, 0.35f, 230f, 45f, 0.95f, 1.0f);
                d.Crackle(0.36f, 0.45f, 16, 4000f, 0.38f);
                d.Notes(0.5f, 0.05f, 0.1f, Wave.Pulse, Pitches(88, 91), 0.05f, 0.005f);
                return Finish(d.S, 0.44f);
            case "use_clanging_scales":
                d = new SoundDesign(1.0f, 0xC521u);
                for (int i = 0; i < 6; i++) d.Bell(0.03f + i * 0.055f, 0.25f, N(72 + (i % 3) * 3), 3.3f, 1.2f, 0.07f, 3f);
                d.Thump(0.36f, 0.5f, 160f, 26f, 1.0f, 0.9f);
                d.Bell(0.38f, 0.55f, N(60), 4.1f, 1.2f, 0.1f, 2.5f);
                d.Noise(0.4f, 0.5f, Filter.High, 3000f, 7000f, 0.25f, 1f, 0.02f, 1.5f, 26f, 0.5f);
                return Finish(d.S, 0.44f);
            case "use_dragon_hammer":
                d = new SoundDesign(1.0f, 0xD622u);
                d.Tone(0, 0.32f, Wave.Sine, N(76), N(48), 0.1f, 0.1f, 1.3f);
                d.Thump(0.34f, 0.65f, 130f, 20f, 1.0f, 1.0f);
                d.Crackle(0.36f, 0.5f, 18, 1900f, 0.42f);
                d.Tone(0.38f, 0.55f, Wave.Saw, N(34), N(28), 0.1f, 0.05f, 1.4f, vibratoHz: 10f, vibratoDepth: 0.07f);
                d.Bell(0.42f, 0.5f, N(62), 1.41f, 0.9f, 0.08f, 3f);
                return Finish(d.S, 0.46f);
            case "use_brutal_swing":
                d = new SoundDesign(1.0f, 0xB523u);
                d.Noise(0, 0.38f, Filter.Band, 300f, 2800f, 0.55f, 1f, 0.2f, 1.0f, 12f, 0.7f);
                d.Tone(0, 0.36f, Wave.Saw, N(34), N(50), 0.1f, 0.1f, 1.0f, vibratoHz: 14f, vibratoDepth: 0.07f);
                d.Thump(0.36f, 0.5f, 150f, 24f, 1.0f, 0.9f);
                d.Noise(0.4f, 0.45f, Filter.Low, 1500f, 300f, 0.4f, 1f, 0.02f, 1.3f);
                d.Notes(0.6f, 0.07f, 0.14f, Wave.Pulse, Pitches(48, 45, 41), 0.05f, 0.01f);
                return Finish(d.S, 0.44f);
            case "use_aurora_veil":
                d = new SoundDesign(1.0f, 0xA724u);
                d.Tone(0, 0.9f, Wave.Sine, N(67), N(72), 0.08f, 0.3f, 1.0f, vibratoHz: 5f, vibratoDepth: 0.06f);
                d.Tone(0.1f, 0.8f, Wave.Sine, N(76), N(79), 0.06f, 0.3f, 1.0f, vibratoHz: 6f, vibratoDepth: 0.05f);
                d.Notes(0.2f, 0.13f, 0.3f, Wave.Sine, Pitches(84, 88, 91, 96), 0.06f, 0.03f);
                d.Noise(0.2f, 0.7f, Filter.High, 4000f, 7500f, 0.12f, 1f, 0.3f, 1.3f, 12f, 0.4f);
                d.Bell(0.55f, 0.4f, N(96), 2.0f, 0.6f, 0.06f, 3f);
                return Finish(d.S, 0.32f);
            case "use_shell_trap":
                d = new SoundDesign(1.0f, 0x5725u);
                d.Thump(0.05f, 0.1f, 280f, 140f, 0.5f, 0.9f);
                d.Tone(0.1f, 0.22f, Wave.Pulse, N(62), N(62), 0.05f, 0.01f, 1.6f);
                d.Thump(0.33f, 0.6f, 140f, 20f, 1.0f, 1.0f);
                d.Noise(0.34f, 0.55f, Filter.Band, 500f, 3200f, 0.5f, 1.2f, 0.02f, 1.1f, 14f, 0.5f);
                d.Crackle(0.4f, 0.5f, 24, 2600f, 0.45f);
                return Finish(d.S, 0.46f);
            case "use_fleur_cannon":
                d = new SoundDesign(1.0f, 0xFC26u);
                d.Notes(0, 0.07f, 0.18f, Wave.Sine, Pitches(72, 76, 79, 84), 0.07f, 0.02f);
                d.Tone(0.1f, 0.3f, Wave.Saw, N(50), N(72), 0.1f, 0.1f, 1.1f);
                d.Noise(0.2f, 0.4f, Filter.Band, 1000f, 4000f, 0.4f, 1.1f, 0.1f, 1.1f, 14f, 0.5f);
                d.Thump(0.36f, 0.5f, 160f, 26f, 1.0f, 0.9f);
                d.Bell(0.4f, 0.5f, N(84), 2.0f, 0.9f, 0.09f, 3f);
                d.Tone(0.6f, 0.35f, Wave.Sine, N(64), N(50), 0.06f, 0.05f, 1.3f);
                return Finish(d.S, 0.44f);
            case "use_psychic_fangs":
                d = new SoundDesign(0.9f, 0xFA27u);
                d.Tone(0, 0.3f, Wave.Sine, N(84), N(70), 0.08f, 0.05f, 1.3f, vibratoHz: 8f, vibratoDepth: 0.05f);
                d.Thump(0.33f, 0.3f, 220f, 50f, 0.9f, 0.9f);
                d.Notes(0.35f, 0.03f, 0.05f, Wave.Pulse, Pitches(88, 88), 0.06f, 0.003f);
                d.Crackle(0.37f, 0.4f, 16, 6000f, 0.38f);
                d.Bell(0.42f, 0.45f, N(91), 2.4f, 0.9f, 0.08f, 3f);
                return Finish(d.S, 0.42f);
            case "use_stomping_tantrum":
                d = new SoundDesign(1.0f, 0x5728u);
                d.Tone(0, 0.2f, Wave.Saw, N(55), N(48), 0.08f, 0.02f, 1.5f, vibratoHz: 20f, vibratoDepth: 0.1f);
                d.Thump(0.2f, 0.3f, 170f, 32f, 0.9f, 0.9f);
                d.Thump(0.34f, 0.55f, 140f, 22f, 1.0f, 1.0f);
                d.Crackle(0.36f, 0.4f, 14, 1600f, 0.4f);
                d.Noise(0.36f, 0.4f, Filter.Band, 900f, 300f, 0.45f, 1f, 0.01f, 1.4f, 24f, 0.5f);
                return Finish(d.S, 0.46f);
            case "use_shadow_bone":
                d = new SoundDesign(1.0f, 0x5B29u);
                d.Tone(0, 0.32f, Wave.Triangle, N(52), N(64), 0.07f, 0.1f, 1.1f, vibratoHz: 24f, vibratoDepth: 0.1f);
                d.Noise(0.02f, 0.3f, Filter.Band, 800f, 2400f, 0.3f, 1f, 0.2f, 1.1f, 20f, 0.6f);
                d.Thump(0.34f, 0.3f, 240f, 70f, 0.85f, 1.0f);
                d.Notes(0.36f, 0.04f, 0.06f, Wave.Pulse, Pitches(76, 72), 0.05f, 0.005f);
                d.Tone(0.45f, 0.5f, Wave.Sine, N(50), N(42), 0.07f, 0.1f, 1.2f, vibratoHz: 6f, vibratoDepth: 0.07f);
                return Finish(d.S, 0.42f);
            case "use_accelerock":
                d = new SoundDesign(0.8f, 0xAC2Au);
                d.Noise(0, 0.28f, Filter.High, 2500f, 7500f, 0.5f, 1f, 0.1f, 1.2f, 30f, 0.5f);
                d.Tone(0, 0.28f, Wave.Sine, N(60), N(84), 0.07f, 0.05f, 1.2f);
                d.Thump(0.28f, 0.3f, 250f, 60f, 0.9f, 0.9f);
                d.Crackle(0.3f, 0.3f, 12, 2200f, 0.35f);
                return Finish(d.S, 0.4f);
            case "use_liquidation":
                d = new SoundDesign(0.95f, 0x112Bu);
                d.Noise(0.05f, 0.28f, Filter.Band, 1200f, 5500f, 0.5f, 1.1f, 0.1f, 1.2f, 26f, 0.6f);
                d.Thump(0.33f, 0.35f, 200f, 50f, 0.85f, 0.8f);
                d.Noise(0.34f, 0.5f, Filter.High, 3000f, 6500f, 0.3f, 1f, 0.02f, 1.5f, 28f, 0.5f);
                d.Bubbles(0.4f, 0.4f, 6, 300f, 1000f, 0.28f);
                d.Notes(0.55f, 0.07f, 0.14f, Wave.Sine, Pitches(79, 83, 86), 0.05f, 0.01f);
                return Finish(d.S, 0.42f);
            case "use_prismatic_laser":
                d = new SoundDesign(1.0f, 0x912Cu);
                d.Tone(0, 0.35f, Wave.Sine, N(60), N(96), 0.1f, 0.1f, 1.2f);
                d.Notes(0.1f, 0.05f, 0.14f, Wave.Triangle, Pitches(72, 76, 79, 84, 88), 0.05f, 0.01f);
                d.Noise(0.2f, 0.3f, Filter.High, 3000f, 9000f, 0.35f, 1.2f, 0.1f, 1.2f, 12f, 0.5f);
                d.Thump(0.36f, 0.6f, 150f, 24f, 1.0f, 0.9f);
                d.Crackle(0.4f, 0.4f, 14, 5200f, 0.35f);
                d.Bell(0.42f, 0.55f, N(91), 3.14f, 1.1f, 0.1f, 3f);
                return Finish(d.S, 0.46f);
            case "use_spectral_thief":
                d = new SoundDesign(1.0f, 0x5C2Du);
                d.Tone(0, 0.45f, Wave.Sine, N(56), N(46), 0.1f, 0.15f, 1.0f, vibratoHz: 6f, vibratoDepth: 0.08f);
                d.Noise(0.05f, 0.3f, Filter.Band, 1500f, 500f, 0.35f, 1f, 0.3f, 1.2f, 12f, 0.5f);
                d.Thump(0.34f, 0.35f, 190f, 40f, 0.85f, 0.7f);
                d.Notes(0.4f, 0.1f, 0.2f, Wave.Pulse, Pitches(60, 65, 70, 77), 0.05f, 0.01f);
                d.Bell(0.6f, 0.35f, N(79), 2.4f, 0.8f, 0.06f, 3f);
                return Finish(d.S, 0.4f);
            case "use_sunsteel_strike":
                d = new SoundDesign(1.0f, 0x552Eu);
                d.Tone(0, 0.34f, Wave.Saw, N(88), N(48), 0.1f, 0.05f, 1.5f);
                d.Noise(0, 0.34f, Filter.Band, 3500f, 900f, 0.5f, 1.2f, 0.1f, 1.2f, 18f, 0.5f);
                d.Thump(0.35f, 0.6f, 150f, 22f, 1.0f, 1.0f);
                d.Bell(0.36f, 0.55f, N(70), 3.14f, 1.2f, 0.11f, 2.5f);
                d.Crackle(0.4f, 0.5f, 22, 3000f, 0.42f);
                return Finish(d.S, 0.46f);
            case "use_moongeist_beam":
                d = new SoundDesign(1.0f, 0x302Fu);
                d.Tone(0, 0.5f, Wave.Sine, N(55), N(64), 0.1f, 0.2f, 1.0f, vibratoHz: 6f, vibratoDepth: 0.08f);
                d.Notes(0.1f, 0.1f, 0.25f, Wave.Triangle, Pitches(67, 70, 74, 79), 0.06f, 0.02f);
                d.Noise(0.15f, 0.4f, Filter.Band, 800f, 3000f, 0.4f, 1.1f, 0.15f, 1.1f, 14f, 0.5f);
                d.Thump(0.36f, 0.5f, 160f, 24f, 0.95f, 0.8f);
                d.Bell(0.4f, 0.55f, N(79), 2.0f, 1.0f, 0.1f, 3f);
                d.Tone(0.5f, 0.45f, Wave.Sine, N(60), N(46), 0.07f, 0.1f, 1.3f, vibratoHz: 7f, vibratoDepth: 0.07f);
                return Finish(d.S, 0.42f);
            case "use_tearful_look":
                d = new SoundDesign(1.0f, 0x7E30u);
                d.Tone(0, 0.5f, Wave.Sine, N(76), N(70), 0.08f, 0.15f, 1.0f, vibratoHz: 9f, vibratoDepth: 0.1f);
                d.Notes(0.1f, 0.14f, 0.3f, Wave.Triangle, Pitches(79, 76, 74, 71), 0.06f, 0.03f);
                d.Bubbles(0.35f, 0.5f, 5, 600f, 1500f, 0.2f);
                d.Bell(0.5f, 0.45f, N(86), 2.5f, 0.6f, 0.06f, 3f);
                return Finish(d.S, 0.32f);
            case "use_zing_zap":
                d = new SoundDesign(0.8f, 0x2531u);
                d.Noise(0.1f, 0.2f, Filter.High, 5000f, 9500f, 0.5f, 1.4f, 0.03f, 1.6f, 36f, 0.5f);
                d.Crackle(0.12f, 0.25f, 16, 6500f, 0.4f);
                d.Thump(0.34f, 0.3f, 240f, 55f, 0.9f, 1.0f);
                d.Notes(0.36f, 0.04f, 0.08f, Wave.Pulse, Pitches(96, 91, 98), 0.06f, 0.003f);
                return Finish(d.S, 0.42f);
            case "use_nature_s_madness":
                d = new SoundDesign(1.0f, 0x4A32u);
                d.Noise(0, 0.6f, Filter.Band, 500f, 2400f, 0.4f, 1f, 0.3f, 1.0f, 14f, 0.6f);
                d.Tone(0, 0.7f, Wave.Triangle, N(48), N(60), 0.09f, 0.2f, 1.0f, vibratoHz: 11f, vibratoDepth: 0.1f);
                d.Notes(0.2f, 0.09f, 0.18f, Wave.Sine, Pitches(79, 76, 84, 81, 88), 0.06f, 0.01f);
                d.Thump(0.36f, 0.4f, 170f, 36f, 0.85f, 0.7f);
                d.Bell(0.45f, 0.45f, N(84), 3.0f, 0.8f, 0.07f, 3f);
                return Finish(d.S, 0.4f);
            case "use_multi_attack":
                d = new SoundDesign(1.0f, 0x3A33u);
                d.Notes(0, 0.05f, 0.1f, Wave.Triangle, Pitches(72, 74, 76, 79, 81, 84), 0.06f, 0.005f);
                d.Tone(0.05f, 0.28f, Wave.Sine, N(60), N(90), 0.08f, 0.05f, 1.3f);
                d.Thump(0.35f, 0.6f, 160f, 24f, 1.0f, 1.0f);
                for (int i = 0; i < 4; i++) d.Bell(0.38f + i * 0.04f, 0.4f, N(72 + i * 4), 2.0f + i * 0.4f, 0.9f, 0.06f, 3f);
                d.Crackle(0.4f, 0.4f, 14, 3500f, 0.35f);
                return Finish(d.S, 0.46f);
            case "use_mind_blown":
                d = new SoundDesign(1.0f, 0xB134u);
                d.Tone(0, 0.26f, Wave.Saw, N(40), N(70), 0.1f, 0.2f, 1.0f, vibratoHz: 20f, vibratoDepth: 0.1f);
                d.Thump(0.26f, 0.5f, 140f, 22f, 1.0f, 1.0f);
                d.Crackle(0.28f, 0.4f, 20, 2800f, 0.45f);
                d.Noise(0.28f, 0.3f, Filter.Band, 600f, 3500f, 0.5f, 1.2f, 0.02f, 1.1f, 14f, 0.5f);
                d.Thump(0.46f, 0.45f, 160f, 28f, 0.9f, 0.9f);
                d.Crackle(0.5f, 0.4f, 16, 2400f, 0.38f);
                return Finish(d.S, 0.46f);
            case "use_plasma_fists":
                d = new SoundDesign(1.0f, 0x5F35u);
                d.Noise(0.05f, 0.2f, Filter.High, 3500f, 8000f, 0.4f, 1.3f, 0.05f, 1.6f, 30f, 0.5f);
                d.Thump(0.22f, 0.3f, 230f, 50f, 0.8f, 0.9f);
                d.Crackle(0.23f, 0.3f, 14, 6000f, 0.38f);
                d.Thump(0.35f, 0.5f, 170f, 26f, 1.0f, 1.0f);
                d.Crackle(0.36f, 0.45f, 18, 5000f, 0.4f);
                d.Tone(0.4f, 0.4f, Wave.Saw, N(76), N(88), 0.05f, 0.05f, 1.4f, vibratoHz: 30f, vibratoDepth: 0.08f);
                return Finish(d.S, 0.46f);
            case "use_photon_geyser":
                d = new SoundDesign(1.0f, 0x6636u);
                d.Tone(0, 0.34f, Wave.Sine, N(48), N(72), 0.1f, 0.1f, 1.2f);
                d.Noise(0.05f, 0.3f, Filter.Band, 400f, 2400f, 0.4f, 1.1f, 0.2f, 1.1f, 12f, 0.5f);
                d.Thump(0.35f, 0.6f, 150f, 24f, 1.0f, 0.9f);
                d.Tone(0.36f, 0.6f, Wave.Sine, N(72), N(96), 0.09f, 0.03f, 1.1f);
                d.Notes(0.4f, 0.07f, 0.16f, Wave.Triangle, Pitches(84, 88, 91, 96), 0.05f, 0.01f);
                d.Bell(0.45f, 0.5f, N(91), 2.4f, 1.0f, 0.09f, 3f);
                return Finish(d.S, 0.44f);
            case "use_zippy_zap":
                d = new SoundDesign(0.75f, 0x2237u);
                d.Noise(0, 0.28f, Filter.High, 4000f, 9000f, 0.5f, 1.3f, 0.02f, 1.5f, 40f, 0.6f);
                d.Crackle(0.02f, 0.3f, 18, 7000f, 0.4f);
                d.Thump(0.3f, 0.25f, 260f, 80f, 0.8f, 1.0f);
                d.Notes(0.33f, 0.04f, 0.07f, Wave.Pulse, Pitches(93, 98, 100), 0.06f, 0.003f);
                return Finish(d.S, 0.4f);
            case "use_splishy_splash":
                d = new SoundDesign(1.0f, 0x5538u);
                d.Noise(0, 0.5f, Filter.Band, 600f, 3000f, 0.5f, 1f, 0.3f, 1.0f, 10f, 0.6f);
                d.Bubbles(0.05f, 0.4f, 7, 300f, 1100f, 0.3f);
                d.Thump(0.34f, 0.4f, 180f, 36f, 0.9f, 0.8f);
                d.Noise(0.36f, 0.5f, Filter.High, 2500f, 6500f, 0.35f, 1f, 0.02f, 1.5f, 24f, 0.6f);
                d.Crackle(0.4f, 0.3f, 8, 5500f, 0.25f);
                d.Notes(0.6f, 0.06f, 0.1f, Wave.Triangle, Pitches(81, 86), 0.04f, 0.01f);
                return Finish(d.S, 0.42f);
            case "use_floaty_fall":
                d = new SoundDesign(1.0f, 0xF139u);
                d.Tone(0, 0.3f, Wave.Sine, N(62), N(78), 0.08f, 0.15f, 1.0f);
                d.Noise(0, 0.3f, Filter.High, 2000f, 4500f, 0.15f, 1f, 0.3f, 1.2f, 10f, 0.4f);
                d.Tone(0.2f, 0.15f, Wave.Sine, N(78), N(78), 0.05f, 0.08f, 1.3f);
                d.Noise(0.2f, 0.15f, Filter.Band, 1500f, 600f, 0.4f, 1f, 0.01f, 1.0f);
                d.Thump(0.34f, 0.4f, 190f, 55f, 0.8f, 0.5f);
                d.Notes(0.45f, 0.07f, 0.14f, Wave.Triangle, Pitches(84, 79, 76), 0.05f, 0.01f);
                return Finish(d.S, 0.4f);
            case "use_pika_papow":
                d = new SoundDesign(1.0f, 0x9A3Au);
                d.Notes(0, 0.07f, 0.15f, Wave.Pulse, Pitches(84, 88, 91, 96), 0.06f, 0.01f);
                d.Tone(0.05f, 0.3f, Wave.Sine, N(72), N(84), 0.07f, 0.1f, 1.1f, vibratoHz: 28f, vibratoDepth: 0.08f);
                d.Thump(0.35f, 0.4f, 200f, 35f, 0.9f, 0.9f);
                d.Crackle(0.36f, 0.4f, 16, 5500f, 0.38f);
                d.Notes(0.45f, 0.06f, 0.14f, Wave.Triangle, Pitches(91, 96, 100, 103), 0.05f, 0.01f);
                return Finish(d.S, 0.42f);
            case "use_bouncy_bubble":
                d = new SoundDesign(1.0f, 0xBB3Bu);
                for (int i = 0; i < 3; i++)
                {
                    d.Bubble(0.04f + i * 0.1f, 380f + i * 70f, 0.4f);
                    d.Thump(0.06f + i * 0.1f, 0.06f, 220f, 120f, 0.3f, 0.5f);
                }
                d.Thump(0.34f, 0.25f, 190f, 60f, 0.6f, 0.4f);
                d.Bubbles(0.35f, 0.3f, 6, 500f, 1500f, 0.3f);
                d.Notes(0.55f, 0.07f, 0.14f, Wave.Sine, Pitches(79, 84, 88), 0.05f, 0.01f);
                return Finish(d.S, 0.38f);
            case "use_buzzy_buzz":
                d = new SoundDesign(0.95f, 0xB53Cu);
                d.Tone(0, 0.7f, Wave.Saw, N(58), N(60), 0.08f, 0.1f, 1.0f, vibratoHz: 40f, vibratoDepth: 0.15f);
                d.Crackle(0.05f, 0.5f, 20, 6000f, 0.38f);
                d.Noise(0.1f, 0.4f, Filter.High, 4000f, 8000f, 0.2f, 1f, 0.2f, 1.3f, 40f, 0.6f);
                d.Thump(0.34f, 0.2f, 230f, 90f, 0.5f, 0.6f);
                d.Notes(0.5f, 0.06f, 0.1f, Wave.Pulse, Pitches(88, 93), 0.05f, 0.005f);
                return Finish(d.S, 0.36f);
            case "use_sizzly_slide":
                d = new SoundDesign(0.95f, 0x513Du);
                d.Noise(0, 0.4f, Filter.Band, 700f, 3500f, 0.5f, 1f, 0.15f, 1.1f, 16f, 0.7f);
                d.Tone(0, 0.35f, Wave.Saw, N(44), N(60), 0.08f, 0.1f, 1.1f, vibratoHz: 18f, vibratoDepth: 0.08f);
                d.Thump(0.33f, 0.3f, 190f, 55f, 0.8f, 0.7f);
                d.Crackle(0.35f, 0.5f, 18, 3200f, 0.4f);
                d.Notes(0.55f, 0.05f, 0.1f, Wave.Triangle, Pitches(72, 76), 0.05f, 0.01f);
                return Finish(d.S, 0.4f);
            case "use_glitzy_glow":
                d = new SoundDesign(1.0f, 0x613Eu);
                d.Notes(0, 0.06f, 0.14f, Wave.Sine, Pitches(88, 91, 96, 100, 103, 108), 0.06f, 0.01f);
                d.Noise(0.05f, 0.4f, Filter.High, 5000f, 9000f, 0.25f, 1.1f, 0.1f, 1.4f, 30f, 0.5f);
                d.Thump(0.34f, 0.3f, 190f, 55f, 0.7f, 0.5f);
                d.Bell(0.4f, 0.55f, N(95), 2.76f, 0.9f, 0.08f, 3f);
                d.Tone(0.5f, 0.45f, Wave.Triangle, N(67), N(79), 0.06f, 0.1f, 1.1f);
                return Finish(d.S, 0.36f);
            case "use_baddy_bad":
                d = new SoundDesign(1.0f, 0xBD3Fu);
                d.Tone(0, 0.3f, Wave.Saw, N(41), N(36), 0.12f, 0.1f, 1.1f, vibratoHz: 12f, vibratoDepth: 0.1f);
                d.Notes(0.08f, 0.1f, 0.18f, Wave.Pulse, Pitches(53, 52, 53, 56), 0.06f, 0.01f);
                d.Noise(0.2f, 0.4f, Filter.Band, 300f, 1100f, 0.45f, 1f, 0.2f, 1.1f, 14f, 0.6f);
                d.Thump(0.35f, 0.4f, 160f, 30f, 0.9f, 0.7f);
                d.Bell(0.5f, 0.4f, N(55), 2.4f, 0.9f, 0.07f, 3f);
                return Finish(d.S, 0.42f);
            case "use_sappy_seed":
                d = new SoundDesign(1.0f, 0x5D40u);
                d.Noise(0, 0.15f, Filter.High, 3000f, 5500f, 0.3f, 1f, 0.05f, 1.5f);
                d.Thump(0.2f, 0.12f, 200f, 90f, 0.5f, 0.6f);
                d.Tone(0.3f, 0.5f, Wave.Sine, N(55), N(79), 0.08f, 0.2f, 1.0f, vibratoHz: 8f, vibratoDepth: 0.05f);
                d.Noise(0.3f, 0.5f, Filter.Band, 900f, 2400f, 0.25f, 1f, 0.3f, 1.2f, 14f, 0.5f);
                d.Bubbles(0.5f, 0.4f, 5, 200f, 600f, 0.25f);
                d.Notes(0.65f, 0.07f, 0.12f, Wave.Triangle, Pitches(76, 79, 83), 0.05f, 0.01f);
                return Finish(d.S, 0.36f);
            case "use_freezy_frost":
                d = new SoundDesign(1.0f, 0xF241u);
                d.Noise(0, 0.6f, Filter.High, 3000f, 7500f, 0.35f, 1f, 0.3f, 1.0f, 10f, 0.5f);
                d.Tone(0.05f, 0.5f, Wave.Sine, N(90), N(84), 0.07f, 0.1f, 1.2f, vibratoHz: 10f, vibratoDepth: 0.04f);
                d.Thump(0.35f, 0.3f, 190f, 55f, 0.7f, 0.5f);
                d.Crackle(0.36f, 0.5f, 18, 7000f, 0.35f);
                d.Notes(0.5f, 0.06f, 0.16f, Wave.Sine, Pitches(96, 100, 103, 108), 0.04f, 0.01f);
                return Finish(d.S, 0.38f);
            case "use_sparkly_swirl":
                d = new SoundDesign(1.0f, 0x5A42u);
                d.Tone(0, 0.5f, Wave.Sine, N(64), N(88), 0.08f, 0.15f, 1.0f, vibratoHz: 14f, vibratoDepth: 0.05f);
                d.Notes(0.05f, 0.06f, 0.14f, Wave.Triangle, Pitches(84, 88, 91, 84, 88, 91, 96), 0.05f, 0.01f);
                d.Thump(0.35f, 0.3f, 180f, 55f, 0.7f, 0.5f);
                d.Bell(0.4f, 0.55f, N(91), 3.0f, 0.9f, 0.08f, 3f);
                d.Noise(0.4f, 0.5f, Filter.High, 5000f, 9000f, 0.2f, 1f, 0.1f, 1.4f, 26f, 0.5f);
                d.Notes(0.65f, 0.07f, 0.12f, Wave.Sine, Pitches(103, 100, 96), 0.04f, 0.01f);
                return Finish(d.S, 0.36f);
            case "use_veevee_volley":
                d = new SoundDesign(1.0f, 0xEE43u);
                for (int i = 0; i < 3; i++)
                {
                    d.Thump(0.14f + i * 0.1f, 0.1f, 240f - i * 20f, 90f, 0.55f + i * 0.1f, 0.7f);
                    d.Notes(0.15f + i * 0.1f, 0.04f, 0.07f, Wave.Triangle, Pitches(79 + i * 3), 0.05f, 0.005f);
                }
                d.Thump(0.35f, 0.3f, 200f, 50f, 0.8f, 0.8f);
                d.Notes(0.45f, 0.08f, 0.18f, Wave.Sine, Pitches(84, 88, 91, 96), 0.06f, 0.02f);
                return Finish(d.S, 0.4f);
            case "use_double_iron_bash":
                d = new SoundDesign(1.0f, 0xD144u);
                d.Thump(0.2f, 0.4f, 170f, 28f, 0.95f, 1.0f);
                d.Bell(0.21f, 0.4f, N(62), 3.3f, 1.1f, 0.09f, 3f);
                d.Thump(0.34f, 0.55f, 150f, 22f, 1.0f, 1.0f);
                d.Bell(0.35f, 0.5f, N(58), 3.3f, 1.2f, 0.1f, 3f);
                d.Crackle(0.37f, 0.4f, 14, 3800f, 0.35f);
                d.Noise(0.38f, 0.4f, Filter.High, 3000f, 7000f, 0.25f, 1f, 0.02f, 1.5f, 26f, 0.5f);
                return Finish(d.S, 0.46f);
            case "use_dynamax_cannon":
                d = new SoundDesign(1.0f, 0xDC50u);
                for (int i = 0; i < 3; i++) d.Bell(0.02f + i * 0.06f, 0.3f, N(52 + i * 5), 1.41f, 1.0f, 0.07f, 2.5f);
                d.Tone(0.1f, 0.3f, Wave.Saw, N(36), N(64), 0.12f, 0.1f, 1.0f, vibratoHz: 14f, vibratoDepth: 0.08f);
                d.Noise(0.15f, 0.35f, Filter.Band, 400f, 3500f, 0.5f, 1.2f, 0.1f, 1.1f, 12f, 0.6f);
                d.Thump(0.35f, 0.65f, 130f, 18f, 1.0f, 1.0f);
                d.Crackle(0.4f, 0.5f, 20, 2400f, 0.42f);
                d.Tone(0.4f, 0.55f, Wave.Sine, N(48), N(34), 0.1f, 0.05f, 1.4f);
                return Finish(d.S, 0.47f);
            case "use_snipe_shot":
                d = new SoundDesign(0.85f, 0x5E51u);
                d.Notes(0, 0.07f, 0.1f, Wave.Pulse, Pitches(91, 95, 98), 0.05f, 0.005f);
                d.Noise(0.2f, 0.12f, Filter.High, 6000f, 9500f, 0.5f, 1.4f, 0.02f, 2.0f);
                d.Thump(0.34f, 0.25f, 240f, 70f, 0.85f, 1.0f);
                d.Bubbles(0.36f, 0.3f, 5, 400f, 1300f, 0.25f);
                d.Notes(0.5f, 0.05f, 0.1f, Wave.Sine, Pitches(88, 93), 0.04f, 0.01f);
                return Finish(d.S, 0.42f);
            case "use_jaw_lock":
                d = new SoundDesign(1.0f, 0x7A52u);
                d.Tone(0, 0.3f, Wave.Saw, N(46), N(40), 0.1f, 0.1f, 1.1f, vibratoHz: 18f, vibratoDepth: 0.1f);
                d.Thump(0.3f, 0.4f, 200f, 36f, 1.0f, 1.0f);
                d.Notes(0.34f, 0.03f, 0.05f, Wave.Pulse, Pitches(64, 64), 0.06f, 0.003f);
                for (int i = 0; i < 5; i++) d.Thump(0.45f + i * 0.07f, 0.05f, 330f - i * 25f, 200f, 0.3f, 0.8f);
                d.Bell(0.5f, 0.4f, N(60), 2.76f, 0.9f, 0.06f, 3f);
                return Finish(d.S, 0.44f);
            case "use_stuff_cheeks":
                d = new SoundDesign(1.0f, 0x5C53u);
                for (int i = 0; i < 4; i++)
                {
                    d.Bubble(0.04f + i * 0.1f, 300f + i * 40f, 0.4f);
                    d.Noise(0.06f + i * 0.1f, 0.05f, Filter.Band, 600f, 1200f, 0.3f, 1f, 0.01f, 2f);
                }
                d.Tone(0.45f, 0.3f, Wave.Triangle, N(50), N(62), 0.08f, 0.1f, 1.1f);
                d.Notes(0.65f, 0.07f, 0.14f, Wave.Triangle, Pitches(72, 76, 79), 0.05f, 0.01f);
                return Finish(d.S, 0.36f);
            case "use_no_retreat":
                d = new SoundDesign(1.0f, 0x4054u);
                for (int i = 0; i < 4; i++) d.Thump(0.05f + i * 0.08f, 0.15f, 170f - i * 10f, 50f, 0.6f + i * 0.1f, 0.8f);
                d.Tone(0.1f, 0.5f, Wave.Saw, N(38), N(62), 0.1f, 0.2f, 1.0f, vibratoHz: 10f, vibratoDepth: 0.06f);
                d.Notes(0.45f, 0.07f, 0.16f, Wave.Pulse, Pitches(60, 64, 67, 72, 76), 0.05f, 0.01f);
                d.Bell(0.6f, 0.4f, N(67), 2.0f, 0.9f, 0.07f, 3f);
                return Finish(d.S, 0.4f);
            case "use_tar_shot":
                d = new SoundDesign(1.0f, 0x7A55u);
                d.Noise(0, 0.28f, Filter.Band, 400f, 1200f, 0.4f, 1f, 0.1f, 1.2f, 20f, 0.5f);
                d.Thump(0.33f, 0.35f, 150f, 50f, 0.8f, 0.4f);
                d.Bubbles(0.36f, 0.5f, 7, 120f, 420f, 0.35f);
                d.Tone(0.5f, 0.4f, Wave.Sine, N(48), N(38), 0.07f, 0.1f, 1.3f);
                return Finish(d.S, 0.4f);
            case "use_magic_powder":
                d = new SoundDesign(1.0f, 0x3F56u);
                d.Noise(0, 0.5f, Filter.High, 4500f, 8500f, 0.2f, 1f, 0.3f, 1.2f, 16f, 0.5f);
                d.Notes(0.05f, 0.08f, 0.2f, Wave.Sine, Pitches(84, 88, 91, 96, 100), 0.06f, 0.02f);
                d.Tone(0.2f, 0.6f, Wave.Sine, N(66), N(70), 0.07f, 0.2f, 1.0f, vibratoHz: 9f, vibratoDepth: 0.07f);
                d.Bell(0.45f, 0.45f, N(91), 3.0f, 0.7f, 0.07f, 3f);
                return Finish(d.S, 0.34f);
            case "use_dragon_darts":
                d = new SoundDesign(0.85f, 0xDD57u);
                d.Noise(0.05f, 0.1f, Filter.High, 5000f, 9000f, 0.5f, 1.4f, 0.03f, 1.8f);
                d.Thump(0.22f, 0.2f, 230f, 60f, 0.7f, 0.9f);
                d.Noise(0.14f, 0.1f, Filter.High, 5500f, 9500f, 0.5f, 1.4f, 0.03f, 1.8f);
                d.Thump(0.34f, 0.3f, 210f, 40f, 0.9f, 1.0f);
                d.Bell(0.36f, 0.4f, N(67), 2.4f, 0.9f, 0.08f, 3f);
                d.Notes(0.5f, 0.06f, 0.12f, Wave.Pulse, Pitches(72, 67), 0.05f, 0.01f);
                return Finish(d.S, 0.42f);
            case "use_teatime":
                d = new SoundDesign(1.0f, 0x7E58u);
                d.Bell(0.02f, 0.5f, N(88), 4.0f, 0.5f, 0.08f, 3f);
                d.Notes(0.2f, 0.13f, 0.3f, Wave.Triangle, Pitches(72, 76, 79, 76, 84), 0.06f, 0.02f);
                d.Noise(0.2f, 0.6f, Filter.High, 3000f, 6000f, 0.12f, 1f, 0.3f, 1.2f, 10f, 0.4f);
                d.Tone(0.5f, 0.45f, Wave.Sine, N(60), N(72), 0.06f, 0.1f, 1.1f);
                return Finish(d.S, 0.34f);
            case "use_octolock":
                d = new SoundDesign(1.0f, 0x0C59u);
                d.Tone(0, 0.5f, Wave.Saw, N(52), N(42), 0.1f, 0.1f, 1.1f, vibratoHz: 16f, vibratoDepth: 0.12f);
                d.Noise(0.05f, 0.5f, Filter.Band, 500f, 1800f, 0.4f, 1f, 0.2f, 1.1f, 20f, 0.7f);
                for (int i = 0; i < 3; i++) d.Thump(0.3f + i * 0.08f, 0.2f, 170f - i * 20f, 50f, 0.5f + i * 0.15f, 0.6f);
                d.Notes(0.6f, 0.08f, 0.16f, Wave.Pulse, Pitches(55, 52, 48), 0.05f, 0.01f);
                return Finish(d.S, 0.4f);
            case "use_bolt_beak":
                d = new SoundDesign(0.95f, 0xBB5Au);
                d.Crackle(0, 0.3f, 14, 6500f, 0.4f);
                d.Tone(0, 0.3f, Wave.Saw, N(70), N(88), 0.08f, 0.1f, 1.0f, vibratoHz: 30f, vibratoDepth: 0.1f);
                d.Thump(0.34f, 0.4f, 220f, 40f, 0.95f, 1.0f);
                d.Crackle(0.35f, 0.45f, 18, 5500f, 0.42f);
                d.Notes(0.5f, 0.05f, 0.1f, Wave.Pulse, Pitches(96, 100, 103), 0.05f, 0.005f);
                return Finish(d.S, 0.44f);
            case "use_fishious_rend":
                d = new SoundDesign(0.95f, 0xF15Bu);
                d.Noise(0, 0.28f, Filter.Band, 800f, 3200f, 0.4f, 1f, 0.15f, 1.1f, 20f, 0.6f);
                d.Thump(0.33f, 0.35f, 210f, 45f, 0.95f, 0.9f);
                d.Notes(0.35f, 0.03f, 0.05f, Wave.Pulse, Pitches(76, 76), 0.06f, 0.003f);
                d.Bubbles(0.38f, 0.45f, 7, 300f, 1100f, 0.28f);
                d.Notes(0.55f, 0.07f, 0.14f, Wave.Sine, Pitches(79, 83, 86), 0.05f, 0.01f);
                return Finish(d.S, 0.42f);
            case "use_court_change":
                d = new SoundDesign(1.0f, 0xC05Cu);
                d.Tone(0, 0.45f, Wave.Sine, N(60), N(84), 0.08f, 0.05f, 1.2f);
                d.Tone(0, 0.45f, Wave.Sine, N(84), N(60), 0.08f, 0.05f, 1.2f);
                d.Thump(0.4f, 0.25f, 190f, 60f, 0.7f, 0.6f);
                d.Thump(0.5f, 0.25f, 170f, 55f, 0.6f, 0.5f);
                d.Bell(0.55f, 0.4f, N(79), 1.5f, 0.8f, 0.07f, 3f);
                d.Notes(0.7f, 0.06f, 0.12f, Wave.Triangle, Pitches(84, 88, 91), 0.04f, 0.01f);
                return Finish(d.S, 0.36f);
        }
        return null;
    }
}
