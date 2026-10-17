using static PokemonPlatinumEngine.Audio.SoundDesign;

namespace PokemonPlatinumEngine.Audio;

// Plan 25 · Y8, its first part (Y8a): the sounds of the moves the story shows from Hearthome City's gate to the Café
// Cabin on Route 210. As the batches before: each move's beats are the original's (when it rings, how often, what it
// is made of, read from its animation script), its sound our own, built from the bank's shared layers. Chatter is the
// user's cry and has none.
public static partial class SoundBank
{
    /// <summary>A sound of Y8's first part, or null for a name that isn't one.</summary>
    private static float[]? MakeMoveSoundY8(string name)
    {
        SoundDesign d;
        switch (name)
        {
            case "use_bubble_beam":
                d = new SoundDesign(0.95f, 0xBB31u);
                d.Bubbles(0, 0.7f, 16, 500f, 1500f, 0.5f);
                d.Noise(0, 0.6f, Filter.Band, 900f, 2200f, 0.15f, 1f, 0.3f, 1.2f);
                d.Noise(0.7f, 0.2f, Filter.High, 3500f, 5500f, 0.4f, 1f, 0.01f, 2f, 30f, 0.5f);
                return Finish(d.S, 0.38f);
            case "use_mach_punch":
                d = new SoundDesign(0.4f, 0x3AC1u);
                d.Noise(0, 0.14f, Filter.Band, 600f, 4200f, 0.8f, 1.2f, 0.4f, 1.2f);
                d.Thump(0.17f, 0.12f, 260f, 120f, 0.9f, 0.9f);
                d.Noise(0.17f, 0.04f, Filter.High, 3800f, 2800f, 0.6f, 1f, 0.01f, 2.5f);
                return Finish(d.S, 0.44f);
            case "use_metal_claw":
                d = new SoundDesign(0.7f, 0x3E7Cu);
                foreach (float at in new[] { 0.06f, 0.3f })
                {
                    d.Noise(at, 0.14f, Filter.High, 4000f, 6800f, 0.7f, 1.2f, 0.05f, 1.6f, 40f, 0.5f);
                    d.Bell(at + 0.04f, 0.3f, N(98), 3.7f, 1.0f, 0.22f, 2.6f);
                }
                d.Tone(0, 0.2f, Wave.Triangle, 1200f, 2400f, 0.06f, 0.03f, 1.4f);
                return Finish(d.S, 0.4f);
            case "use_future_sight":
                d = new SoundDesign(0.95f, 0xF05Au);
                d.Tone(0, 0.8f, Wave.Sine, N(62), N(67), 0.28f, 0.2f, 1.2f, vibratoHz: 5f, vibratoDepth: 0.03f);
                d.Tone(0.1f, 0.7f, Wave.Triangle, N(74), N(79), 0.1f, 0.3f, 1.3f, vibratoHz: 7f, vibratoDepth: 0.05f);
                d.Bell(0.35f, 0.6f, N(86), 2.76f, 1.0f, 0.2f, 2.4f);
                d.Noise(0.4f, 0.5f, Filter.Band, 1800f, 900f, 0.2f, 1.4f, 0.3f, 1.2f);
                return Finish(d.S, 0.38f);
            case "use_pain_split":
                d = new SoundDesign(0.85f, 0x9A15u);
                for (int k = 0; k < 6; k++)
                {
                    d.Tone(0.03f + k * 0.1f, 0.12f, Wave.Saw, k % 2 == 0 ? 520f : 400f, k % 2 == 0 ? 240f : 180f, 0.16f, 0.01f, 1.5f);
                    d.Bell(0.03f + k * 0.1f, 0.2f, N(k % 2 == 0 ? 76 : 72), 2.3f, 1.2f, 0.12f, 3f);
                }
                d.Thump(0.65f, 0.15f, 180f, 80f, 0.4f, 0.4f);
                return Finish(d.S, 0.38f);
            case "use_psybeam":
                d = new SoundDesign(0.9f, 0x75B3u);
                d.Tone(0, 0.5f, Wave.Sine, N(70), N(82), 0.26f, 0.05f, 1.2f, vibratoHz: 14f, vibratoDepth: 0.12f);
                d.Tone(0.04f, 0.5f, Wave.Triangle, N(77), N(89), 0.14f, 0.05f, 1.2f, vibratoHz: 11f, vibratoDepth: 0.1f);
                d.Noise(0.05f, 0.45f, Filter.Band, 1200f, 3600f, 0.2f, 1.6f, 0.2f, 1.2f, 18f, 0.4f);
                d.Bell(0.5f, 0.35f, N(91), 2.01f, 1.0f, 0.2f, 2.8f);
                d.Tone(0.5f, 0.3f, Wave.Sine, N(58), N(52), 0.18f, 0.02f, 1.6f);
                return Finish(d.S, 0.38f);
            case "use_shadow_ball":
                d = new SoundDesign(1.0f, 0x5BA1u);
                d.Tone(0, 0.4f, Wave.Saw, 70f, 140f, 0.2f, 0.3f, 1f, vibratoHz: 9f, vibratoDepth: 0.06f);
                d.Noise(0, 0.4f, Filter.Low, 600f, 1500f, 0.4f, 1f, 0.4f, 1f, 8f, 0.4f);
                d.Noise(0.4f, 0.3f, Filter.Band, 400f, 1400f, 0.45f, 1.2f, 0.2f, 1.4f);
                d.Thump(0.7f, 0.25f, 150f, 55f, 0.9f, 0.4f);
                d.Noise(0.7f, 0.25f, Filter.Low, 1200f, 300f, 0.5f, 1f, 0.01f, 1.4f);
                d.Bell(0.72f, 0.28f, N(55), 1.41f, 1.4f, 0.14f, 3f);
                return Finish(d.S, 0.44f);
            case "use_shadow_claw":
                d = new SoundDesign(0.75f, 0x5C1Au);
                foreach (float at in new[] { 0.03f, 0.2f })
                {
                    d.Noise(at, 0.17f, Filter.Band, 1600f, 5200f, 0.7f, 1.2f, 0.05f, 1.5f, 26f, 0.5f);
                    d.Tone(at, 0.22f, Wave.Saw, 180f, 90f, 0.12f, 0.02f, 1.4f, vibratoHz: 10f, vibratoDepth: 0.08f);
                }
                d.Bell(0.38f, 0.4f, N(60), 2.76f, 1.3f, 0.14f, 2.6f);
                d.Thump(0.2f, 0.12f, 200f, 90f, 0.4f, 0.5f);
                return Finish(d.S, 0.4f);
            case "use_shadow_sneak":
                d = new SoundDesign(0.8f, 0x5E4Au);
                d.Noise(0, 0.3f, Filter.Low, 500f, 1200f, 0.4f, 1f, 0.3f, 1.2f);
                d.Tone(0, 0.3f, Wave.Triangle, 110f, 80f, 0.12f, 0.1f, 1.4f);
                d.Thump(0.34f, 0.2f, 130f, 50f, 0.8f, 0.3f);
                d.Noise(0.34f, 0.18f, Filter.Band, 500f, 200f, 0.5f, 1f, 0.01f, 1.6f);
                d.Bell(0.38f, 0.35f, N(52), 1.41f, 1.0f, 0.1f, 2.8f);
                return Finish(d.S, 0.4f);
            case "use_spit_up":
                d = new SoundDesign(0.85f, 0x5B17u);
                d.Tone(0, 0.2f, Wave.Triangle, 200f, 120f, 0.2f, 0.05f, 1.2f);
                d.Thump(0.2f, 0.07f, 320f, 160f, 0.5f, 0.7f);
                d.Noise(0.2f, 0.28f, Filter.Band, 700f, 2000f, 0.4f, 1.2f, 0.02f, 1.4f);
                d.Bubbles(0.28f, 0.2f, 4, 300f, 700f, 0.3f);
                d.Thump(0.5f, 0.2f, 170f, 70f, 0.9f, 0.6f);
                d.Noise(0.5f, 0.24f, Filter.Band, 600f, 1800f, 0.55f, 1f, 0.01f, 1.6f);
                return Finish(d.S, 0.42f);
            case "use_stockpile":
                d = new SoundDesign(0.95f, 0x5701u);
                foreach (float at in new[] { 0f, 0.5f })
                {
                    d.Tone(at, 0.38f, Wave.Sine, 180f, 460f, 0.22f, 0.25f, 1.1f);
                    d.Noise(at, 0.36f, Filter.Band, 500f, 1800f, 0.2f, 1.2f, 0.5f, 1.2f);
                    d.Bell(at + 0.38f, 0.2f, N(at == 0f ? 84 : 91), 2.01f, 0.8f, 0.25f, 3f);
                }
                return Finish(d.S, 0.38f);
            case "use_sucker_punch":
                d = new SoundDesign(0.4f, 0x50C4u);
                d.Noise(0, 0.05f, Filter.Band, 2800f, 1800f, 0.4f, 1f, 0.004f, 2.5f);
                d.Thump(0.04f, 0.14f, 200f, 70f, 1f, 1f);
                d.Noise(0.04f, 0.1f, Filter.Low, 900f, 300f, 0.6f, 1f, 0.004f, 2f);
                d.Thump(0.2f, 0.1f, 220f, 100f, 0.5f, 0.8f);
                return Finish(d.S, 0.44f);
            case "use_swallow":
                d = new SoundDesign(0.95f, 0x5A11u);
                d.Noise(0, 0.4f, Filter.Band, 400f, 1800f, 0.3f, 1.2f, 0.5f, 1.2f);
                d.Tone(0.38f, 0.1f, Wave.Sine, 320f, 160f, 0.32f, 0.01f, 1.8f);
                d.Thump(0.42f, 0.08f, 200f, 100f, 0.5f, 0.5f);
                d.Bell(0.55f, 0.4f, N(88), 2.01f, 0.8f, 0.22f, 2.5f);
                d.Notes(0.6f, 0.1f, 0.3f, Wave.Sine, Pitches(79, 83, 88), 0.1f);
                return Finish(d.S, 0.36f);
            case "use_will_o_wisp":
                d = new SoundDesign(0.95f, 0x3115u);
                d.Noise(0, 0.5f, Filter.Band, 900f, 2400f, 0.35f, 1.4f, 0.1f, 1.2f, 24f, 0.7f);
                d.Tone(0, 0.5f, Wave.Triangle, N(76), N(80), 0.1f, 0.05f, 1.2f, vibratoHz: 15f, vibratoDepth: 0.1f);
                d.Noise(0.5f, 0.35f, Filter.High, 1500f, 5000f, 0.45f, 1f, 0.01f, 1.6f, 18f, 0.5f);
                d.Crackle(0.5f, 0.35f, 8, 2800f, 0.28f);
                d.Bell(0.52f, 0.4f, N(81), 3.01f, 0.9f, 0.14f, 2.8f);
                return Finish(d.S, 0.4f);
            case "use_minimize":
                d = new SoundDesign(0.9f, 0x3123u);
                d.Tone(0, 0.7f, Wave.Sine, N(88), N(64), 0.26f, 0.02f, 1.3f);
                d.Tone(0, 0.7f, Wave.Triangle, N(95), N(71), 0.1f, 0.02f, 1.3f);
                for (int k = 0; k < 4; k++) d.Bubble(0.1f + k * 0.16f, 900f + 100f * k, 0.28f);
                d.Bell(0.72f, 0.18f, N(100), 2.01f, 0.6f, 0.14f, 3.2f);
                return Finish(d.S, 0.36f);
            case "use_payback":
                d = new SoundDesign(1.0f, 0x9AB4u);
                d.Tone(0, 0.5f, Wave.Saw, 70f, 100f, 0.28f, 0.3f, 1f, vibratoHz: 6f, vibratoDepth: 0.05f);
                for (int k = 0; k < 4; k++)
                {
                    d.Thump(0.5f + k * 0.11f, 0.1f, 260f - 20f * k, 100f, 0.7f, 0.9f);
                    d.Bell(0.5f + k * 0.11f, 0.25f, N(88 + k), 3.7f, 1.0f, 0.12f, 3f);
                }
                d.Thump(0.94f, 0.06f, 160f, 80f, 0.4f, 0.3f);
                return Finish(d.S, 0.44f);
            case "use_attract":
                d = new SoundDesign(1.0f, 0xA77Au);
                d.Notes(0, 0.12f, 0.3f, Wave.Sine, Pitches(79, 84, 88, 91), 0.2f, 0.02f);
                foreach (float at in new[] { 0.5f, 0.68f })
                {
                    d.Noise(at, 0.05f, Filter.Band, 1800f, 1300f, 0.3f, 1.6f, 0.005f, 2.4f);
                    d.Bubble(at, 900f, 0.3f);
                }
                d.Bell(0.8f, 0.3f, N(96), 2.01f, 0.6f, 0.18f, 2.8f);
                return Finish(d.S, 0.36f);
            case "use_amnesia":
                d = new SoundDesign(0.95f, 0xA3E5u);
                d.Tone(0, 0.45f, Wave.Triangle, N(55), N(51), 0.26f, 0.1f, 1.2f, vibratoHz: 6f, vibratoDepth: 0.06f);
                d.Tone(0.1f, 0.4f, Wave.Sine, N(67), N(62), 0.14f, 0.1f, 1.3f);
                d.Bubbles(0.45f, 0.4f, 7, 400f, 1100f, 0.3f);
                d.Tone(0.5f, 0.4f, Wave.Sine, N(76), N(88), 0.08f, 0.2f, 1.5f);
                return Finish(d.S, 0.34f);
            case "use_aqua_ring":
                d = new SoundDesign(1.0f, 0xA912u);
                d.Bubbles(0, 0.5f, 8, 500f, 1500f, 0.35f);
                d.Notes(0.1f, 0.14f, 0.5f, Wave.Sine, Pitches(79, 83, 86, 91), 0.12f, 0.04f);
                d.Noise(0.1f, 0.7f, Filter.High, 3500f, 5500f, 0.1f, 1f, 0.4f, 1.2f, 16f, 0.6f);
                d.Bell(0.55f, 0.4f, N(98), 2.01f, 0.7f, 0.16f, 2.4f);
                return Finish(d.S, 0.34f);
            case "use_copycat":
                d = new SoundDesign(0.95f, 0xC0B7u);
                d.Noise(0, 0.4f, Filter.High, 1800f, 4500f, 0.3f, 1f, 0.3f, 1.2f);
                d.Notes(0.35f, 0.09f, 0.2f, Wave.Pulse, Pitches(72, 76, 79, 84), 0.14f, 0.01f, 2f, 0.3f);
                d.Notes(0.5f, 0.09f, 0.2f, Wave.Pulse, Pitches(72, 76, 79, 84), 0.08f, 0.01f, 2f, 0.3f);
                d.Bell(0.82f, 0.15f, N(96), 2.01f, 0.7f, 0.2f, 3f);
                return Finish(d.S, 0.36f);
            case "use_fury_attack":
                d = new SoundDesign(0.6f, 0xF0A7u);
                for (int k = 0; k < 4; k++)
                {
                    d.Noise(0.02f + k * 0.1f, 0.05f, Filter.High, 4500f, 3000f, 0.5f, 1f, 0.005f, 2.4f);
                    d.Thump(0.05f + k * 0.1f, 0.07f, 480f - 40f * k, 220f, 0.5f, 0.9f);
                }
                return Finish(d.S, 0.4f);
            case "use_jump_kick":
                d = new SoundDesign(0.85f, 0x3C41u);
                d.Tone(0, 0.2f, Wave.Sine, 220f, 640f, 0.22f, 0.02f, 1.2f);
                d.Noise(0.1f, 0.3f, Filter.Band, 500f, 3000f, 0.4f, 1f, 0.3f, 1.2f);
                d.Thump(0.42f, 0.18f, 240f, 80f, 1f, 1f);
                d.Noise(0.42f, 0.06f, Filter.High, 3500f, 2400f, 0.6f, 1f, 0.004f, 2.4f);
                d.Crackle(0.44f, 0.2f, 5, 1200f, 0.3f);
                return Finish(d.S, 0.44f);
            case "use_light_screen":
                d = new SoundDesign(0.95f, 0x1165u);
                d.Notes(0, 0.07f, 0.3f, Wave.Sine, Pitches(79, 83, 86, 88, 91, 95), 0.14f, 0.02f);
                d.Bell(0.05f, 0.6f, N(91), 2.01f, 0.7f, 0.2f, 2.4f);
                d.Noise(0.1f, 0.7f, Filter.High, 5000f, 7500f, 0.12f, 1f, 0.3f, 1.2f, 24f, 0.6f);
                d.Tone(0, 0.45f, Wave.Triangle, 300f, 700f, 0.08f, 0.15f, 1.2f);
                return Finish(d.S, 0.36f);
            case "use_nasty_plot":
                d = new SoundDesign(1.0f, 0x7A57u);
                d.Tone(0, 0.65f, Wave.Saw, 85f, 105f, 0.2f, 0.25f, 1f, vibratoHz: 7f, vibratoDepth: 0.08f);
                d.Tone(0.1f, 0.5f, Wave.Triangle, N(58), N(55), 0.12f, 0.1f, 1.2f, vibratoHz: 5f, vibratoDepth: 0.05f);
                d.Noise(0.2f, 0.5f, Filter.Band, 700f, 1500f, 0.18f, 1.4f, 0.4f, 1.2f, 12f, 0.5f);
                d.Tone(0.68f, 0.2f, Wave.Pulse, N(64), N(70), 0.14f, 0.02f, 1.6f, duty: 0.3f);
                d.Bell(0.75f, 0.2f, N(72), 2.76f, 1.2f, 0.12f, 3f);
                return Finish(d.S, 0.36f);
            case "use_refresh":
                d = new SoundDesign(0.95f, 0x2EF5u);
                d.Noise(0, 0.6f, Filter.Band, 1500f, 3500f, 0.2f, 1f, 0.3f, 1.2f);
                d.Notes(0.05f, 0.09f, 0.3f, Wave.Sine, Pitches(84, 88, 91, 96), 0.16f, 0.02f);
                d.Bell(0.45f, 0.4f, N(96), 2.01f, 0.7f, 0.22f, 2.4f);
                d.Notes(0.6f, 0.07f, 0.2f, Wave.Triangle, Pitches(91, 96, 100), 0.08f, 0.02f);
                return Finish(d.S, 0.34f);
            case "use_soft_boiled":
                d = new SoundDesign(1.0f, 0x50F7u);
                for (int k = 0; k < 4; k++) d.Tone(0.05f + k * 0.07f, 0.09f, Wave.Sine, k % 2 == 0 ? 260f : 330f, k % 2 == 0 ? 330f : 260f, 0.2f, 0.01f, 1.5f);
                d.Noise(0.45f, 0.06f, Filter.High, 3500f, 2500f, 0.5f, 1f, 0.004f, 2.4f);
                d.Thump(0.46f, 0.06f, 400f, 200f, 0.3f, 0.6f);
                d.Notes(0.52f, 0.1f, 0.4f, Wave.Sine, Pitches(79, 84, 88, 91), 0.14f, 0.02f);
                d.Bell(0.6f, 0.4f, N(95), 2.01f, 0.7f, 0.2f, 2.4f);
                return Finish(d.S, 0.34f);
            case "use_steel_wing":
                d = new SoundDesign(0.95f, 0x57E1u);
                foreach (float at in new[] { 0.02f, 0.2f })
                    d.Noise(at, 0.15f, Filter.Band, 600f, 3200f, 0.55f, 1f, 0.3f, 1.2f);
                foreach (var (at, n) in new[] { (0.4f, 88), (0.48f, 91), (0.56f, 93), (0.64f, 95) })
                {
                    d.Noise(at, 0.05f, Filter.High, 5000f, 3500f, 0.5f, 1f, 0.004f, 2.4f);
                    d.Bell(at, 0.3f, N(n), 3.7f, 1.0f, 0.2f, 2.8f);
                }
                return Finish(d.S, 0.4f);
            case "use_sweet_kiss":
                d = new SoundDesign(1.0f, 0x5E41u);
                d.Noise(0, 0.05f, Filter.Band, 1800f, 1200f, 0.5f, 1.6f, 0.005f, 2.4f);
                d.Bubble(0, 1100f, 0.5f);
                d.Notes(0.3f, 0.12f, 0.3f, Wave.Triangle, Pitches(84, 88, 84, 91, 88), 0.14f, 0.02f);
                d.Bell(0.7f, 0.3f, N(98), 2.01f, 0.6f, 0.14f, 2.4f);
                return Finish(d.S, 0.36f);
            case "use_whirlwind":
                d = new SoundDesign(1.0f, 0x331Du);
                d.Noise(0, 0.7f, Filter.Band, 400f, 2600f, 0.55f, 0.9f, 0.4f, 1.1f, 6f, 0.3f);
                d.Noise(0.2f, 0.7f, Filter.High, 1500f, 5000f, 0.2f, 1f, 0.4f, 1.2f, 9f, 0.5f);
                d.Tone(0.55f, 0.3f, Wave.Sine, 900f, 300f, 0.1f, 0.03f, 1.6f);
                d.Noise(0.6f, 0.35f, Filter.Band, 2800f, 700f, 0.5f, 1f, 0.01f, 1.6f);
                return Finish(d.S, 0.4f);
            case "use_mirror_move":
                d = new SoundDesign(0.7f, 0x3112u);
                d.Bell(0, 0.5f, N(96), 3.7f, 0.9f, 0.25f, 2.6f);
                d.Bell(0.12f, 0.5f, N(103), 2.76f, 0.8f, 0.14f, 2.8f);
                d.Noise(0.05f, 0.25f, Filter.High, 6000f, 8000f, 0.15f, 1f, 0.05f, 1.6f);
                d.Bell(0.34f, 0.28f, N(91), 3.7f, 0.9f, 0.12f, 3f);
                return Finish(d.S, 0.34f);
            case "use_haze":
                d = new SoundDesign(1.0f, 0x4A2Eu);
                d.Noise(0, 0.9f, Filter.Band, 600f, 1800f, 0.4f, 0.8f, 0.4f, 1f, 4f, 0.3f);
                d.Noise(0.1f, 0.8f, Filter.Low, 500f, 300f, 0.25f, 1f, 0.4f, 1.1f);
                d.Tone(0.2f, 0.7f, Wave.Sine, N(55), N(53), 0.1f, 0.3f, 1.3f, vibratoHz: 4f, vibratoDepth: 0.03f);
                return Finish(d.S, 0.36f);
            case "use_defog":
                d = new SoundDesign(1.0f, 0xDEF0u);
                d.Noise(0, 0.8f, Filter.Band, 500f, 3200f, 0.5f, 0.9f, 0.2f, 1.1f, 7f, 0.3f);
                d.Noise(0.3f, 0.6f, Filter.High, 2000f, 6000f, 0.25f, 1f, 0.3f, 1.2f, 12f, 0.5f);
                d.Bell(0.78f, 0.2f, N(98), 2.01f, 0.6f, 0.14f, 3f);
                return Finish(d.S, 0.4f);
            case "use_dig":
                d = new SoundDesign(1.0f, 0xD196u);
                foreach (float at in new[] { 0f, 0.1f, 0.2f })
                {
                    d.Noise(at, 0.1f, Filter.Band, 500f, 1400f, 0.5f, 1f, 0.01f, 1.8f);
                    d.Crackle(at, 0.1f, 4, 900f, 0.3f);
                }
                d.Thump(0.42f, 0.25f, 120f, 45f, 0.9f, 0.4f);
                d.Noise(0.42f, 0.3f, Filter.Low, 1200f, 300f, 0.5f, 1f, 0.01f, 1.4f);
                d.Crackle(0.45f, 0.35f, 9, 1000f, 0.35f);
                d.Thump(0.8f, 0.1f, 180f, 80f, 0.4f, 0.6f);
                return Finish(d.S, 0.44f);
            case "use_yawn":
                d = new SoundDesign(1.0f, 0x7A3Eu);
                d.Tone(0, 0.7f, Wave.Triangle, N(67), N(52), 0.22f, 0.3f, 1.1f, vibratoHz: 5f, vibratoDepth: 0.04f);
                d.Noise(0, 0.7f, Filter.Band, 500f, 1200f, 0.2f, 1.2f, 0.4f, 1.1f, 4f, 0.3f);
                d.Tone(0.55f, 0.35f, Wave.Sine, N(60), N(55), 0.1f, 0.1f, 1.4f);
                d.Notes(0.75f, 0.1f, 0.2f, Wave.Sine, Pitches(88, 84), 0.06f, 0.03f);
                return Finish(d.S, 0.34f);
            case "use_acupressure":
                d = new SoundDesign(0.9f, 0xACB1u);
                foreach (var (at, n) in new[] { (0.05f, 84), (0.19f, 88), (0.33f, 91) })
                {
                    d.Thump(at, 0.06f, 300f, 160f, 0.3f, 0.7f);
                    d.Bell(at, 0.3f, N(n), 2.01f, 0.8f, 0.2f, 2.6f);
                }
                d.Notes(0.5f, 0.09f, 0.4f, Wave.Sine, Pitches(79, 84, 88, 91), 0.1f, 0.03f);
                return Finish(d.S, 0.36f);
            case "use_air_cutter":
                d = new SoundDesign(0.6f, 0xA1C7u);
                d.Noise(0, 0.22f, Filter.Band, 700f, 3600f, 0.5f, 1f, 0.4f, 1.2f);
                d.Noise(0.12f, 0.2f, Filter.High, 3000f, 6000f, 0.7f, 1.2f, 0.05f, 1.6f, 36f, 0.5f);
                d.Noise(0.26f, 0.2f, Filter.High, 6000f, 3000f, 0.7f, 1.2f, 0.05f, 1.6f, 36f, 0.5f);
                return Finish(d.S, 0.4f);
            case "use_false_swipe":
                d = new SoundDesign(0.55f, 0xFA15u);
                d.Noise(0, 0.2f, Filter.Band, 600f, 3000f, 0.5f, 1f, 0.4f, 1.2f);
                d.Noise(0.2f, 0.1f, Filter.High, 4500f, 2800f, 0.5f, 1.2f, 0.02f, 2f);
                d.Bell(0.22f, 0.25f, N(88), 3.7f, 0.8f, 0.14f, 3f);
                d.Thump(0.24f, 0.05f, 300f, 200f, 0.1f, 0.3f);
                return Finish(d.S, 0.36f);
            case "use_feint":
                d = new SoundDesign(0.7f, 0xFE17u);
                foreach (float at in new[] { 0.02f, 0.16f })
                    d.Noise(at, 0.12f, Filter.Band, 800f, 2600f, 0.22f, 1f, 0.4f, 1.2f);
                d.Noise(0.34f, 0.08f, Filter.High, 5000f, 2800f, 0.8f, 1.2f, 0.01f, 2f);
                d.Thump(0.37f, 0.12f, 280f, 110f, 0.8f, 1f);
                return Finish(d.S, 0.42f);
            case "use_metronome":
                d = new SoundDesign(1.0f, 0x3E70u);
                for (int k = 0; k < 6; k++)
                {
                    d.Noise(0.02f + k * 0.13f, 0.03f, Filter.Band, k % 2 == 0 ? 2200f : 1700f, 1500f, 0.5f, 2f, 0.002f, 3f);
                    d.Thump(0.02f + k * 0.13f, 0.04f, k % 2 == 0 ? 900f : 700f, 500f, 0.3f, 0.5f);
                }
                d.Notes(0.78f, 0.06f, 0.2f, Wave.Sine, Pitches(84, 88, 91, 96), 0.12f, 0.01f);
                return Finish(d.S, 0.36f);
            case "use_pin_missile":
                d = new SoundDesign(0.7f, 0x91A1u);
                for (int k = 0; k < 3; k++)
                {
                    d.Tone(0.02f + k * 0.055f, 0.14f, Wave.Sine, 2400f, 1500f, 0.1f, 0.02f, 1.6f);
                    d.Noise(0.17f + k * 0.055f, 0.04f, Filter.High, 5000f, 3500f, 0.5f, 1f, 0.004f, 2.4f);
                    d.Thump(0.17f + k * 0.055f, 0.05f, 420f, 220f, 0.4f, 0.8f);
                }
                d.Thump(0.4f, 0.08f, 320f, 140f, 0.4f, 0.7f);
                return Finish(d.S, 0.38f);
            case "use_roost":
                d = new SoundDesign(1.0f, 0x2005u);
                foreach (float at in new[] { 0.02f, 0.2f, 0.38f })
                    d.Noise(at, 0.17f, Filter.Band, 400f, 1800f, 0.3f, 1f, 0.4f, 1.3f);
                d.Tone(0.55f, 0.35f, Wave.Sine, N(64), N(60), 0.12f, 0.1f, 1.4f);
                d.Bell(0.62f, 0.38f, N(91), 2.01f, 0.7f, 0.2f, 2.4f);
                d.Notes(0.68f, 0.1f, 0.25f, Wave.Sine, Pitches(84, 88, 91), 0.08f, 0.03f);
                return Finish(d.S, 0.34f);
            case "use_scary_face":
                d = new SoundDesign(0.95f, 0x5CA1u);
                d.Thump(0, 0.3f, 80f, 40f, 0.8f, 0.3f);
                d.Tone(0, 0.5f, Wave.Saw, 120f, 70f, 0.22f, 0.02f, 1.2f, vibratoHz: 18f, vibratoDepth: 0.15f);
                d.Noise(0, 0.4f, Filter.Band, 1500f, 700f, 0.35f, 1.4f, 0.02f, 1.2f, 20f, 0.5f);
                d.Tone(0.1f, 0.15f, Wave.Pulse, N(88), N(86), 0.15f, 0.005f, 2f, duty: 0.2f);
                d.Tone(0.45f, 0.4f, Wave.Saw, 90f, 60f, 0.14f, 0.05f, 1.4f);
                return Finish(d.S, 0.44f);
            case "use_shock_wave":
                d = new SoundDesign(0.95f, 0x5C0Eu);
                d.Noise(0, 0.14f, Filter.High, 3000f, 6500f, 0.3f, 1f, 0.1f, 1.2f, 40f, 0.6f);
                d.Tone(0.1f, 0.4f, Wave.Saw, 300f, 2400f, 0.12f, 0.02f, 1.5f, vibratoHz: 40f, vibratoDepth: 0.2f);
                d.Crackle(0.12f, 0.45f, 18, 3200f, 0.4f);
                d.Thump(0.14f, 0.1f, 300f, 130f, 0.6f, 0.9f);
                d.Noise(0.5f, 0.35f, Filter.Band, 1500f, 4000f, 0.3f, 1.2f, 0.01f, 1.6f, 36f, 0.6f);
                return Finish(d.S, 0.42f);
            case "use_slash":
                d = new SoundDesign(0.55f, 0x5105u);
                foreach (var (at, f) in new[] { (0f, 5000f), (0.11f, 5600f), (0.23f, 6400f) })
                    d.Noise(at, 0.12f, Filter.High, f, f * 0.55f, 0.75f, 1.3f, 0.03f, 1.8f);
                d.Bell(0.25f, 0.25f, N(90), 3.7f, 1.1f, 0.16f, 3f);
                return Finish(d.S, 0.42f);
            case "use_swagger":
                d = new SoundDesign(0.95f, 0x5A66u);
                d.Notes(0, 0.12f, 0.26f, Wave.Pulse, Pitches(67, 72, 76, 79), 0.16f, 0.02f, 2f, 0.4f);
                d.Bell(0.45f, 0.4f, N(84), 2.01f, 0.9f, 0.18f, 2.4f);
                foreach (float at in new[] { 0.58f, 0.74f })
                {
                    d.Noise(at, 0.08f, Filter.Band, 500f, 300f, 0.5f, 1.4f, 0.01f, 2f);
                    d.Thump(at, 0.1f, 160f, 70f, 0.6f, 0.4f);
                }
                return Finish(d.S, 0.38f);
            case "use_thunder_shock":
                d = new SoundDesign(0.6f, 0x7B05u);
                d.Noise(0, 0.05f, Filter.High, 4000f, 8000f, 0.9f, 1f, 0.002f, 2.4f);
                d.Tone(0, 0.15f, Wave.Saw, 2400f, 500f, 0.2f, 0.002f, 2f);
                d.Crackle(0.04f, 0.4f, 14, 3400f, 0.35f);
                d.Thump(0.04f, 0.1f, 240f, 100f, 0.5f, 0.9f);
                return Finish(d.S, 0.42f);
            case "use_toxic_spikes":
                d = new SoundDesign(1.0f, 0x7051u);
                for (int k = 0; k < 5; k++)
                {
                    d.Tone(0.02f + k * 0.07f, 0.12f, Wave.Sine, 2200f, 1400f, 0.08f, 0.02f, 1.6f);
                    d.Noise(0.17f + k * 0.07f, 0.04f, Filter.High, 5000f, 3500f, 0.4f, 1f, 0.004f, 2.4f);
                }
                d.Bubbles(0.45f, 0.45f, 8, 200f, 520f, 0.38f);
                d.Tone(0.6f, 0.3f, Wave.Triangle, N(52), N(50), 0.1f, 0.05f, 1.4f, vibratoHz: 6f, vibratoDepth: 0.06f);
                return Finish(d.S, 0.38f);
            case "use_wake_up_slap":
                d = new SoundDesign(0.8f, 0x3A4Bu);
                for (int k = 0; k < 4; k++)
                {
                    d.Noise(0.02f + k * 0.07f, 0.06f, Filter.Band, 1600f, 1000f, 0.8f, 1f, 0.004f, 2.4f);
                    d.Thump(0.03f + k * 0.07f, 0.1f, 220f, 100f, 0.7f, 0.9f);
                }
                d.Bell(0.34f, 0.4f, N(91), 2.01f, 0.9f, 0.2f, 2.4f);
                d.Notes(0.36f, 0.07f, 0.2f, Wave.Sine, Pitches(88, 91, 95), 0.08f, 0.01f);
                return Finish(d.S, 0.42f);
            case "use_cosmic_power":
                d = new SoundDesign(1.0f, 0xC05Eu);
                d.Tone(0, 0.9f, Wave.Sine, N(43), N(48), 0.3f, 0.3f, 1.1f, vibratoHz: 3f, vibratoDepth: 0.02f);
                d.Tone(0.1f, 0.8f, Wave.Triangle, N(55), N(60), 0.12f, 0.3f, 1.3f);
                foreach (var (at, n) in new[] { (0.2f, 91), (0.38f, 98), (0.55f, 95), (0.72f, 103) })
                    d.Bell(at, 0.3f, N(n), 2.01f, 0.8f, 0.14f, 3f);
                d.Noise(0.2f, 0.7f, Filter.High, 5000f, 7500f, 0.08f, 1f, 0.4f, 1.2f, 20f, 0.6f);
                return Finish(d.S, 0.36f);
            case "use_lick":
                d = new SoundDesign(0.6f, 0x11C4u);
                d.Noise(0, 0.3f, Filter.Band, 500f, 1800f, 0.4f, 1.4f, 0.3f, 1.2f, 18f, 0.6f);
                d.Bubbles(0.05f, 0.3f, 6, 250f, 650f, 0.4f);
                d.Tone(0.28f, 0.1f, Wave.Sine, 350f, 700f, 0.16f, 0.005f, 2f);
                d.Thump(0.3f, 0.06f, 260f, 130f, 0.25f, 0.4f);
                return Finish(d.S, 0.36f);
        }
        return MakeMoveSoundY8b(name);
    }
}
