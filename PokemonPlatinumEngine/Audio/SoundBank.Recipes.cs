using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using static PokemonPlatinumEngine.Audio.SoundDesign;

namespace PokemonPlatinumEngine.Audio;

// The recipes: how each sound is made. Levels are the peak each is brought to (Finish); a menu's blip sits lowest,
// a blow in battle highest, so the mixer's sound bus needs no balancing of its own.
public static partial class SoundBank
{
    private static List<SoundEntry> Catalogue()
    {
        var list = new List<SoundEntry>();
        void Add(string name, SoundGroup group, string? original, string when) => list.Add(new SoundEntry(name, group, original, when));

        // ---- Menus
        Add("cursor", SoundGroup.Menu, "SEQ_SE_DP_SELECT", "The cursor moves in a menu or a list.");
        Add("select", SoundGroup.Menu, "SEQ_SE_DP_DECIDE", "A choice is made.");
        Add("cancel", SoundGroup.Menu, null, "A menu is backed out of, an answer is no.");
        Add("error", SoundGroup.Menu, "SEQ_SE_DP_BEEP", "A choice that can't be made: a move with no PP, a Pokémon that can't battle, an item that can't be used here, money short.");
        Add("menu_open", SoundGroup.Menu, "SEQ_SE_DP_WIN_OPEN", "The start menu opens.");
        Add("menu_close", SoundGroup.Menu, null, "The start menu closes.");
        Add("page", SoundGroup.Menu, "SEQ_SE_DP_MEKURU", "A page turns: the bag's pockets, a Pokédex entry's pages, the PC's boxes.");
        Add("text", SoundGroup.Menu, null, "The text box moves on to its next line.");

        // ---- The field
        Add("bump", SoundGroup.Field, "SEQ_SE_DP_WALL_HIT", "Walking into something.");
        Add("ledge", SoundGroup.Field, "SEQ_SE_DP_DANSA4", "Hopping down a ledge.");
        Add("grass", SoundGroup.Field, "SEQ_SE_DP_KUSA", "A step through very tall grass (the original makes no sound in ordinary tall grass).");
        Add("step_snow", SoundGroup.Field, "SEQ_SE_PL_YUKI", "A step in snow.");
        Add("step_puddle", SoundGroup.Field, "SEQ_SE_DP_FOOT3_0", "A step through a puddle.");
        Add("step_shallows", SoundGroup.Field, "SEQ_SE_DP_FOOT3_1", "A step through ankle-deep water.");
        Add("step_mud", SoundGroup.Field, "SEQ_SE_DP_MARSH_WALK", "A step in mud that isn't deep.");
        Add("door_open", SoundGroup.Field, "SEQ_SE_DP_DOOR_OPEN", "A door opens as the player steps up to it.");
        Add("door_close", SoundGroup.Field, "SEQ_SE_DP_DOOR_CLOSE2", "The door shuts behind the player who came out of it.");
        Add("door_slide", SoundGroup.Field, "SEQ_SE_DP_DOOR10", "Glass doors slide open (a Pokémon Center, a Mart).");
        Add("stairs", SoundGroup.Field, "SEQ_SE_DP_KAIDAN2", "Going up or down stairs, or through a way out that isn't a door.");
        Add("warp", SoundGroup.Field, "SEQ_SE_DP_TELE2", "A warp panel.");
        Add("exclaim", SoundGroup.Field, null, "The \"!\" over a trainer who has seen the player, or anyone a script surprises.");
        Add("surf", SoundGroup.Field, null, "A Pokémon is ridden out onto the water.");
        Add("save", SoundGroup.Field, "SEQ_SE_DP_SAVE", "The game is saved.");
        Add("pc_on", SoundGroup.Field, "SEQ_SE_DP_PC_LOGIN", "A PC is switched on.");
        Add("pc_off", SoundGroup.Field, "SEQ_SE_DP_PC_LOGOFF", "A PC is switched off.");
        Add("heal", SoundGroup.Field, "SEQ_SE_DP_KAIFUKU", "A medicine used from the bag, HP restored in battle.");
        Add("levelup", SoundGroup.Menu, null, "A Pokédex completed, an evolution done: a bright arpeggio.");
        Add("bike_bell", SoundGroup.Field, "SEQ_SE_DP_JITENSYA", "Getting on the Bicycle (waits for the Bicycle, plan 02 · S2).");
        Add("gear", SoundGroup.Field, "SEQ_SE_DP_GEAR", "The Bicycle's gear changes (waits for the Bicycle, plan 02 · S2).");
        Add("boulder", SoundGroup.Field, null, "A boulder pushed with Strength (waits for plan 02 · S2).");
        Add("rock_smash", SoundGroup.Field, null, "A rock broken with Rock Smash (waits for plan 02 · S2).");
        Add("cut", SoundGroup.Field, "SEQ_SE_DP_FW015", "A tree cut down with Cut (waits for plan 02 · S2).");
        Add("fish_cast", SoundGroup.Field, null, "A rod cast (waits for fishing).");
        Add("fish_bite", SoundGroup.Field, "SEQ_SE_DP_FW104", "Something bites (waits for fishing).");
        Add("fish_reel", SoundGroup.Field, null, "The line reeled in (waits for fishing).");
        Add("poketch", SoundGroup.Field, "SEQ_SE_DP_POKETCH_003", "A Pokétch button (waits for the Pokétch).");
        Add("thunder", SoundGroup.Field, "SEQ_SE_DP_T_KAMI2", "Thunder cracking close, just after a storm's lightning (two strikes of three).");
        Add("thunder_rumble", SoundGroup.Field, "SEQ_SE_DP_T_KAMI", "Thunder rolling from further off, a second after the lightning (one strike of three).");

        // ---- Battles
        Add("send_out", SoundGroup.Battle, "SEQ_SE_DP_BOWA4", "A ball opens and a Pokémon comes out.");
        Add("recall", SoundGroup.Battle, null, "A Pokémon is called back into its ball.");
        Add("run_away", SoundGroup.Battle, "SEQ_SE_DP_NIGERU", "Getting away from a wild Pokémon.");
        Add("ball_throw", SoundGroup.Battle, "SEQ_SE_DP_NAGERU", "A ball thrown.");
        Add("ball_shake", SoundGroup.Battle, "SEQ_SE_DP_KON", "Each wobble of a thrown ball.");
        Add("ball_click", SoundGroup.Battle, "SEQ_SE_DP_GETTING", "The ball clicks shut: caught.");
        Add("ball_break", SoundGroup.Battle, null, "The ball bursts open: the Pokémon broke free.");
        Add("hit_normal", SoundGroup.Battle, "SEQ_SE_DP_KOUKA_M", "A hit lands.");
        Add("hit_super", SoundGroup.Battle, "SEQ_SE_DP_KOUKA_H", "A super-effective hit lands.");
        Add("hit_weak", SoundGroup.Battle, "SEQ_SE_DP_KOUKA_L", "A not very effective hit lands.");
        Add("stat_up", SoundGroup.Battle, null, "A stat rises.");
        Add("stat_down", SoundGroup.Battle, null, "A stat falls.");
        Add("status_poison", SoundGroup.Battle, null, "Poisoned, or badly poisoned.");
        Add("status_burn", SoundGroup.Battle, null, "Burned.");
        Add("status_paralysis", SoundGroup.Battle, null, "Paralysed.");
        Add("status_sleep", SoundGroup.Battle, null, "Fallen asleep.");
        Add("status_freeze", SoundGroup.Battle, null, "Frozen solid.");
        Add("status_confusion", SoundGroup.Battle, null, "Confused, and each turn it is confused.");
        Add("faint", SoundGroup.Battle, "SEQ_SE_DP_POKE_DEAD3", "A Pokémon faints.");
        Add("exp", SoundGroup.Battle, "SEQ_SE_DP_EXP", "The EXP bar fills.");

        // ---- A move's launch, one per type (the hit's sound comes as it lands)
        foreach (var type in Enum.GetValues<PokemonType>())
            Add("move_" + type.ToString().ToLowerInvariant(), SoundGroup.Move, null, $"A {type} move sets off.");
        return list;
    }

    private static float[] Make(string name)
    {
        SoundDesign d;
        switch (name)
        {
            // ---------------------------------------------------------------- menus
            case "cursor":
                // A short, round blip: a quarter-width pulse with a sine under it
                d = new SoundDesign(0.045f);
                d.Tone(0, 0.045f, Wave.Pulse, N(91), N(91), 0.5f, 0.05f, 2.2f, duty: 0.25f);
                d.Tone(0, 0.045f, Wave.Sine, N(79), N(79), 0.5f, 0.05f, 2f);
                return Finish(d.S, 0.32f);
            case "select":
                // Two notes a fifth apart, the second brighter: "yes"
                d = new SoundDesign(0.12f);
                d.Tone(0, 0.06f, Wave.Pulse, N(84), N(84), 0.5f, 0.04f, 1.6f, duty: 0.25f);
                d.Tone(0.045f, 0.075f, Wave.Pulse, N(91), N(91), 0.55f, 0.04f, 2f, duty: 0.25f);
                d.Tone(0.045f, 0.075f, Wave.Sine, N(79), N(79), 0.3f, 0.04f, 2f);
                return Finish(d.S, 0.38f);
            case "cancel":
                // The same two notes falling, softer: "back"
                d = new SoundDesign(0.12f);
                d.Tone(0, 0.06f, Wave.Triangle, N(81), N(81), 0.6f, 0.04f, 1.6f);
                d.Tone(0.05f, 0.07f, Wave.Triangle, N(74), N(74), 0.6f, 0.04f, 2f);
                d.Tone(0.05f, 0.07f, Wave.Pulse, N(74), N(74), 0.15f, 0.04f, 2f, duty: 0.5f);
                return Finish(d.S, 0.38f);
            case "error":
                // Two low buzzes, a little out of tune with each other
                d = new SoundDesign(0.2f);
                for (int k = 0; k < 2; k++)
                {
                    d.Tone(k * 0.1f, 0.08f, Wave.Pulse, 156f, 156f, 0.4f, 0.04f, 0.6f, duty: 0.4f);
                    d.Tone(k * 0.1f, 0.08f, Wave.Pulse, 166f, 166f, 0.4f, 0.04f, 0.6f, duty: 0.4f);
                }
                return Finish(d.S, 0.4f);
            case "menu_open":
                // A quick rising sweep with a swish of air
                d = new SoundDesign(0.11f);
                d.Tone(0, 0.11f, Wave.Triangle, N(72), N(86), 0.6f, 0.1f, 1.4f);
                d.Tone(0, 0.11f, Wave.Sine, N(84), N(96), 0.25f, 0.1f, 1.8f);
                d.Noise(0, 0.08f, Filter.Band, 2000f, 5000f, 0.25f, 1.2f, 0.2f, 1.5f);
                return Finish(d.S, 0.34f);
            case "menu_close":
                d = new SoundDesign(0.1f);
                d.Tone(0, 0.1f, Wave.Triangle, N(86), N(74), 0.6f, 0.05f, 1.6f);
                d.Noise(0, 0.07f, Filter.Band, 4500f, 1800f, 0.25f, 1.2f, 0.1f, 1.5f);
                return Finish(d.S, 0.32f);
            case "page":
                // A sheet flicked over: a band of noise sweeping down, and the paper's snap
                d = new SoundDesign(0.12f);
                d.Noise(0, 0.11f, Filter.Band, 4200f, 1600f, 0.8f, 1.1f, 0.25f, 1.6f);
                d.Noise(0.07f, 0.02f, Filter.Band, 3000f, 3000f, 0.5f, 2.5f, 0.1f, 2.5f);
                return Finish(d.S, 0.32f);
            case "text":
                // The lightest tick there is: it comes with every line
                d = new SoundDesign(0.035f);
                d.Tone(0, 0.035f, Wave.Sine, N(96), N(96), 0.6f, 0.05f, 3f);
                d.Tone(0, 0.035f, Wave.Triangle, N(84), N(84), 0.4f, 0.05f, 3f);
                return Finish(d.S, 0.3f);

            // ---------------------------------------------------------------- the field
            case "bump":
                // A soft, low thud: a sine whose pitch drops quickly, its second harmonic for small speakers
                d = new SoundDesign(0.14f, 7);
                d.Tone(0, 0.14f, Wave.Sine, 255f, 105f, 0.7f, 0.03f, 3f);
                d.Tone(0, 0.1f, Wave.Sine, 510f, 210f, 0.25f, 0.04f, 3f);
                d.Noise(0, 0.05f, Filter.Low, 1400f, 600f, 0.5f, 0.7f, 0.08f, 3f);
                return Finish(d.S, 0.42f);
            case "ledge":
                // Up and over (a springy rise), then the landing
                d = new SoundDesign(0.32f);
                d.Tone(0, 0.12f, Wave.Pulse, N(67), N(79), 0.4f, 0.05f, 1.2f, duty: 0.25f);
                d.Tone(0, 0.12f, Wave.Sine, N(55), N(67), 0.4f, 0.05f, 1.2f);
                d.Thump(0.2f, 0.11f, 180f, 90f, 0.6f, 0.6f);
                return Finish(d.S, 0.38f);
            case "grass":
                // Blades brushing past: two quick bursts of fluttering noise
                d = new SoundDesign(0.16f, 0x2545F491u);
                d.Noise(0, 0.09f, Filter.Band, 3400f, 2600f, 0.8f, 0.9f, 0.15f, 1.2f, 45f, 0.6f);
                d.Noise(0.06f, 0.1f, Filter.Band, 2400f, 1800f, 0.6f, 0.9f, 0.15f, 1.5f, 38f, 0.6f);
                return Finish(d.S, 0.32f);
            case "step_snow":
                // Packed snow: a dull crunch of many small breaks
                d = new SoundDesign(0.13f, 0x51A7u);
                d.Noise(0, 0.12f, Filter.Low, 2400f, 900f, 0.5f, 0.8f, 0.15f, 1.6f);
                d.Crackle(0, 0.08f, 9, 2600f, 0.6f);
                return Finish(d.S, 0.3f);
            case "step_puddle":
                d = new SoundDesign(0.18f, 0x9u);
                d.Noise(0, 0.12f, Filter.Band, 1900f, 700f, 0.8f, 1.2f, 0.05f, 2f);
                d.Bubbles(0.02f, 0.1f, 2, 500f, 800f, 0.4f);
                return Finish(d.S, 0.32f);
            case "step_shallows":
                // Wading: a softer wash than a puddle, lower and longer
                d = new SoundDesign(0.2f, 0x33u);
                d.Noise(0, 0.18f, Filter.Low, 1600f, 500f, 0.7f, 0.8f, 0.2f, 1.4f);
                d.Bubbles(0.04f, 0.12f, 2, 350f, 550f, 0.3f);
                return Finish(d.S, 0.3f);
            case "step_mud":
                // A squelch: low noise and a sagging tone
                d = new SoundDesign(0.17f, 0x77u);
                d.Noise(0, 0.15f, Filter.Low, 900f, 300f, 0.7f, 1.6f, 0.2f, 1.3f);
                d.Tone(0, 0.15f, Wave.Sine, 220f, 110f, 0.4f, 0.2f, 1.6f, vibratoHz: 22f, vibratoDepth: 0.06f);
                return Finish(d.S, 0.32f);
            case "door_open":
                // The latch clicks, the door swings in on its hinges
                d = new SoundDesign(0.34f, 0xD0u);
                d.Noise(0, 0.018f, Filter.Band, 2800f, 2800f, 0.9f, 4f, 0.1f, 2f);
                d.Noise(0.035f, 0.015f, Filter.Band, 2200f, 2200f, 0.6f, 4f, 0.1f, 2f);
                d.Noise(0.04f, 0.28f, Filter.Low, 800f, 400f, 0.4f, 0.7f, 0.3f, 1.3f);
                d.Tone(0.05f, 0.18f, Wave.Triangle, 300f, 380f, 0.12f, 0.4f, 1f, vibratoHz: 18f, vibratoDepth: 0.03f);
                return Finish(d.S, 0.38f);
            case "door_close":
                // The swing, then the door meets its frame and the latch catches
                d = new SoundDesign(0.3f, 0xDCu);
                d.Noise(0, 0.14f, Filter.Low, 500f, 900f, 0.35f, 0.7f, 0.4f, 0.8f);
                d.Thump(0.13f, 0.15f, 150f, 70f, 0.8f, 0.4f);
                d.Noise(0.15f, 0.015f, Filter.Band, 2600f, 2600f, 0.5f, 4f, 0.1f, 2f);
                return Finish(d.S, 0.4f);
            case "door_slide":
                // Glass doors on their runners: a smooth breath of air rising and falling, a soft chime as they part
                d = new SoundDesign(0.45f, 0x5Du);
                d.Noise(0, 0.42f, Filter.Band, 1300f, 2400f, 0.7f, 0.8f, 0.35f, 1.2f);
                d.Tone(0, 0.42f, Wave.Sine, 110f, 140f, 0.12f, 0.3f, 1.2f);
                d.Bell(0.02f, 0.3f, N(88), 2f, 1.2f, 0.12f);
                return Finish(d.S, 0.34f);
            case "stairs":
                // Three quick footfalls, each a little further away
                d = new SoundDesign(0.32f, 0x5Eu);
                for (int k = 0; k < 3; k++) d.Thump(k * 0.09f, 0.08f, 200f - 25f * k, 110f - 10f * k, 0.8f - 0.15f * k, 0.7f);
                return Finish(d.S, 0.38f);
            case "warp":
                // A sweep upward through a shimmer of high notes, echoing
                d = new SoundDesign(0.75f);
                d.Tone(0, 0.45f, Wave.Sine, 300f, 2400f, 0.5f, 0.2f, 1f);
                d.Notes(0.04f, 0.05f, 0.22f, Wave.Sine, Pitches(84, 88, 91, 96, 100, 103), 0.3f, 0.05f, 2f);
                d.Echo(0.11f, 0.35f);
                return Finish(d.S, 0.36f);
            case "exclaim":
                // Bright and sudden: two tones a fifth apart struck together over a quick upward flick
                d = new SoundDesign(0.22f);
                d.Tone(0, 0.03f, Wave.Pulse, N(76), N(88), 0.4f, 0.1f, 1f, duty: 0.25f);
                d.Tone(0.025f, 0.19f, Wave.Pulse, N(88), N(88), 0.45f, 0.03f, 1.8f, duty: 0.25f);
                d.Tone(0.025f, 0.19f, Wave.Pulse, N(95), N(95), 0.3f, 0.03f, 2f, duty: 0.125f);
                return Finish(d.S, 0.4f);
            case "surf":
                // The Pokémon takes the water: a splash, a rush of spray, bubbles
                d = new SoundDesign(0.6f, 0x5u);
                d.Noise(0, 0.55f, Filter.Low, 3500f, 700f, 0.8f, 0.8f, 0.05f, 1.4f);
                d.Noise(0, 0.25f, Filter.Band, 2500f, 1200f, 0.4f, 1.2f, 0.02f, 2f);
                d.Thump(0, 0.12f, 160f, 70f, 0.4f, 0f);
                d.Bubbles(0.1f, 0.4f, 6, 350f, 900f, 0.25f);
                return Finish(d.S, 0.4f);
            case "thunder":
                // A crack that splits into a long, falling roll
                d = new SoundDesign(2.6f, 0x7A11u);
                d.Noise(0, 0.12f, Filter.High, 2400f, 900f, 0.8f, 0.7f, 0.02f, 2f);
                d.Crackle(0, 0.25f, 40, 1800f, 0.5f);
                d.Noise(0.03f, 2.5f, Filter.Low, 900f, 90f, 0.9f, 0.8f, 0.02f, 1.6f, flutterHz: 6f, flutterDepth: 0.5f);
                d.Noise(0.05f, 2.3f, Filter.Band, 260f, 70f, 0.6f, 0.9f, 0.05f, 1.4f, flutterHz: 3.5f, flutterDepth: 0.6f);
                return Finish(d.S, 0.6f);
            case "thunder_rumble":
                // No crack: a low roll that swells and fades a long way off
                d = new SoundDesign(3.2f, 0x7A12u);
                d.Noise(0, 3.2f, Filter.Low, 420f, 80f, 0.9f, 0.8f, 0.25f, 1.3f, flutterHz: 4.5f, flutterDepth: 0.55f);
                d.Noise(0.2f, 2.8f, Filter.Band, 160f, 60f, 0.5f, 0.9f, 0.3f, 1.2f, flutterHz: 2.2f, flutterDepth: 0.6f);
                return Finish(d.S, 0.45f);
            case "save":
                // A soft chime of three bells: written and kept
                d = new SoundDesign(0.75f);
                d.Bell(0, 0.5f, N(76), 2f, 1.5f, 0.4f);
                d.Bell(0.12f, 0.5f, N(83), 2f, 1.5f, 0.4f);
                d.Bell(0.24f, 0.5f, N(88), 2f, 1.8f, 0.45f);
                return Finish(d.S, 0.36f);
            case "pc_on":
                // It boots: a hum, then four notes climbing in square waves
                d = new SoundDesign(0.42f);
                d.Tone(0, 0.4f, Wave.Sine, 120f, 120f, 0.15f, 0.2f, 1f);
                d.Notes(0.04f, 0.06f, 0.09f, Wave.Pulse, Pitches(72, 79, 84, 88), 0.35f, 0.05f, 1.4f, 0.25f);
                return Finish(d.S, 0.36f);
            case "pc_off":
                d = new SoundDesign(0.36f);
                d.Notes(0, 0.06f, 0.09f, Wave.Pulse, Pitches(88, 84, 79), 0.35f, 0.05f, 1.4f, 0.25f);
                d.Tone(0.15f, 0.2f, Wave.Sine, 600f, 120f, 0.25f, 0.05f, 1.5f);
                return Finish(d.S, 0.34f);
            case "heal":
                // A shimmer climbing upward
                d = new SoundDesign(0.62f);
                d.Notes(0, 0.06f, 0.3f, Wave.Sine, Pitches(72, 76, 79, 84, 88), 0.4f, 0.05f, 2f);
                d.Bell(0.24f, 0.36f, N(96), 3f, 1f, 0.2f);
                return Finish(d.S, 0.4f);
            case "levelup":
                d = new SoundDesign(0.5f);
                d.Notes(0, 0.07f, 0.2f, Wave.Pulse, Pitches(72, 76, 79), 0.35f, 0.04f, 1.8f, 0.25f);
                d.Tone(0.21f, 0.29f, Wave.Pulse, N(84), N(84), 0.4f, 0.03f, 1.6f, duty: 0.25f);
                d.Bell(0.21f, 0.29f, N(84), 2f, 1f, 0.25f);
                return Finish(d.S, 0.42f);
            case "bike_bell":
                // Ring-ring: two strikes of a small bright bell
                d = new SoundDesign(0.55f);
                d.Bell(0, 0.3f, 2400f, 1.414f, 3f, 0.5f, 3f);
                d.Bell(0.13f, 0.42f, 2400f, 1.414f, 3f, 0.5f, 3f);
                return Finish(d.S, 0.36f);
            case "gear":
                // A lever's two clicks and the chain taking up
                d = new SoundDesign(0.18f, 0x6Eu);
                d.Noise(0, 0.015f, Filter.Band, 3200f, 3200f, 0.9f, 5f, 0.1f, 2f);
                d.Noise(0.06f, 0.015f, Filter.Band, 2600f, 2600f, 0.8f, 5f, 0.1f, 2f);
                d.Crackle(0.08f, 0.09f, 6, 1800f, 0.35f, even: true);
                return Finish(d.S, 0.34f);
            case "boulder":
                // A great weight dragged over stone: a rumble with a grinding stutter
                d = new SoundDesign(0.6f, 0xB0u);
                d.Noise(0, 0.58f, Filter.Low, 320f, 220f, 1f, 1.2f, 0.15f, 0.9f, 16f, 0.5f);
                d.Tone(0, 0.55f, Wave.Sine, 62f, 52f, 0.4f, 0.15f, 1f);
                d.Crackle(0.05f, 0.45f, 10, 900f, 0.2f);
                return Finish(d.S, 0.45f);
            case "rock_smash":
                // The rock cracks and falls in pieces
                d = new SoundDesign(0.5f, 0x5Au);
                d.Noise(0, 0.12f, Filter.Low, 4000f, 900f, 0.9f, 0.9f, 0.01f, 2.5f);
                d.Thump(0, 0.2f, 140f, 50f, 0.8f, 0.8f);
                d.Crackle(0.08f, 0.35f, 14, 2200f, 0.4f);
                return Finish(d.S, 0.46f);
            case "cut":
                // A blade's swish, then leaves falling
                d = new SoundDesign(0.35f, 0xC0u);
                d.Noise(0, 0.13f, Filter.Band, 1200f, 5200f, 0.9f, 2f, 0.6f, 1.5f);
                d.Noise(0.12f, 0.22f, Filter.Band, 3000f, 2200f, 0.35f, 0.9f, 0.2f, 1.5f, 34f, 0.6f);
                return Finish(d.S, 0.38f);
            case "fish_cast":
                // The rod's whip, and the lure dropping in
                d = new SoundDesign(0.45f, 0xF1u);
                d.Noise(0, 0.16f, Filter.Band, 700f, 3600f, 0.8f, 1.6f, 0.5f, 1.5f);
                d.Tone(0.33f, 0.07f, Wave.Sine, 650f, 230f, 0.5f, 0.05f, 2f);
                d.Noise(0.33f, 0.08f, Filter.Band, 1600f, 800f, 0.3f, 1.4f, 0.05f, 2f);
                return Finish(d.S, 0.36f);
            case "fish_bite":
                // A tug on the line: an urgent pair of notes
                d = new SoundDesign(0.2f);
                d.Tone(0, 0.08f, Wave.Pulse, N(81), N(81), 0.5f, 0.04f, 1.2f, duty: 0.25f);
                d.Tone(0.09f, 0.11f, Wave.Pulse, N(86), N(86), 0.5f, 0.04f, 1.6f, duty: 0.25f);
                return Finish(d.S, 0.38f);
            case "fish_reel":
                // The reel's ratchet speeding up
                d = new SoundDesign(0.45f, 0xEEu);
                for (int k = 0; k < 14; k++)
                {
                    float at = 0.42f * (1f - MathF.Pow(1f - k / 14f, 1.4f));
                    d.Noise(at, 0.012f, Filter.Band, 2400f + 60f * k, 2400f + 60f * k, 0.8f, 4f, 0.1f, 2f);
                }
                return Finish(d.S, 0.34f);
            case "poketch":
                // A digital chirp from the watch on the wrist
                d = new SoundDesign(0.12f);
                d.Tone(0, 0.05f, Wave.Pulse, N(96), N(96), 0.4f, 0.03f, 0.8f, duty: 0.5f);
                d.Tone(0.055f, 0.06f, Wave.Pulse, N(100), N(100), 0.4f, 0.03f, 1.2f, duty: 0.5f);
                return Finish(d.S, 0.3f);

            // ---------------------------------------------------------------- battles
            case "send_out":
                // The ball pops open in a puff of light: a burst of air, a rising tone, a sparkle
                d = new SoundDesign(0.5f, 0xB0Au);
                d.Noise(0, 0.3f, Filter.Low, 2500f, 600f, 0.7f, 0.9f, 0.02f, 1.8f);
                d.Tone(0, 0.18f, Wave.Sine, 280f, 900f, 0.5f, 0.05f, 1.4f);
                d.Bell(0.08f, 0.38f, N(96), 2.01f, 1.4f, 0.18f);
                d.Bell(0.14f, 0.34f, N(100), 2.01f, 1.4f, 0.14f);
                return Finish(d.S, 0.45f);
            case "recall":
                // A beam of red light drawing in: a falling zap
                d = new SoundDesign(0.38f, 0xEC);
                d.Tone(0, 0.36f, Wave.Sine, 1100f, 260f, 0.5f, 0.08f, 1.2f);
                d.Tone(0, 0.36f, Wave.Pulse, 550f, 130f, 0.15f, 0.08f, 1.2f, duty: 0.25f);
                d.Noise(0, 0.3f, Filter.Band, 2600f, 700f, 0.25f, 1.4f, 0.1f, 1.2f);
                return Finish(d.S, 0.4f);
            case "run_away":
                // Quick footsteps hurrying off
                d = new SoundDesign(0.5f, 0x2Bu);
                for (int k = 0; k < 5; k++) d.Thump(k * 0.08f, 0.06f, 260f, 150f, 0.8f - 0.13f * k, 0.8f);
                d.Noise(0, 0.45f, Filter.Band, 900f, 2000f, 0.15f, 0.8f, 0.3f, 1.2f);
                return Finish(d.S, 0.38f);
            case "ball_throw":
                // The ball flies: a whoosh that passes by, rising and falling
                d = new SoundDesign(0.32f, 0x7Au);
                d.Noise(0, 0.3f, Filter.Band, 600f, 2400f, 0.9f, 1.4f, 0.55f, 1.4f);
                d.Tone(0.05f, 0.25f, Wave.Sine, 800f, 1300f, 0.15f, 0.5f, 1.5f);
                return Finish(d.S, 0.4f);
            case "ball_shake":
                // A wooden knock as the ball rocks over, and a smaller one as it rocks back
                d = new SoundDesign(0.2f, 0x5Bu);
                d.Tone(0, 0.07f, Wave.Sine, 820f, 700f, 0.7f, 0.02f, 4f);
                d.Noise(0, 0.02f, Filter.Band, 1600f, 1600f, 0.5f, 3f, 0.1f, 2f);
                d.Tone(0.1f, 0.06f, Wave.Sine, 760f, 660f, 0.4f, 0.02f, 4f);
                return Finish(d.S, 0.42f);
            case "ball_click":
                // Caught: the catch locks with two metal clicks and a small ping
                d = new SoundDesign(0.32f, 0xC1u);
                d.Noise(0, 0.014f, Filter.Band, 3600f, 3600f, 0.9f, 6f, 0.1f, 2f);
                d.Noise(0.05f, 0.014f, Filter.Band, 3000f, 3000f, 0.9f, 6f, 0.1f, 2f);
                d.Bell(0.06f, 0.26f, N(91), 3.5f, 1.2f, 0.35f);
                return Finish(d.S, 0.4f);
            case "ball_break":
                // The ball bursts open: a pop of air and light
                d = new SoundDesign(0.38f, 0xBBu);
                d.Noise(0, 0.25f, Filter.Low, 3500f, 700f, 0.8f, 0.9f, 0.01f, 2.2f);
                d.Tone(0, 0.12f, Wave.Sine, 200f, 650f, 0.5f, 0.05f, 1.6f);
                d.Bell(0.04f, 0.3f, N(93), 2.01f, 1f, 0.15f);
                return Finish(d.S, 0.45f);
            case "hit_normal":
                // A blow: a low body dropping and a slap of noise
                d = new SoundDesign(0.17f, 0xA511E9B3u);
                d.Tone(0, 0.16f, Wave.Sine, 170f, 60f, 0.8f, 0.02f, 2.2f);
                d.Noise(0, 0.1f, Filter.Low, 2400f, 700f, 0.7f, 0.8f, 0.02f, 2.5f);
                return Finish(d.S, 0.5f);
            case "hit_super":
                // The same blow doubled and heavier, with a crunch: it hurt
                d = new SoundDesign(0.3f, 0x5E5Eu);
                d.Tone(0, 0.28f, Wave.Sine, 150f, 42f, 0.8f, 0.02f, 2f);
                d.Tone(0, 0.18f, Wave.Pulse, 240f, 90f, 0.25f, 0.02f, 2.5f, duty: 0.3f);
                d.Noise(0, 0.2f, Filter.Low, 4200f, 800f, 0.8f, 0.9f, 0.01f, 2.2f);
                d.Noise(0.05f, 0.14f, Filter.Low, 2600f, 600f, 0.6f, 0.9f, 0.02f, 2.5f);
                d.Crackle(0.01f, 0.08f, 6, 2800f, 0.3f);
                return Finish(d.S, 0.55f);
            case "hit_weak":
                // A glancing tap: higher, shorter, quieter
                d = new SoundDesign(0.1f, 0x1Eu);
                d.Tone(0, 0.09f, Wave.Sine, 260f, 160f, 0.6f, 0.03f, 2.5f);
                d.Noise(0, 0.05f, Filter.Low, 1000f, 600f, 0.3f, 0.7f, 0.05f, 3f);
                return Finish(d.S, 0.4f);
            case "stat_up":
                // Three sweeps upward, each starting higher than the last
                d = new SoundDesign(0.52f);
                for (int k = 0; k < 3; k++)
                    d.Tone(k * 0.11f, 0.16f, Wave.Triangle, 400f * (1f + 0.25f * k), 1200f * (1f + 0.25f * k), 0.5f, 0.15f, 1.4f);
                d.Bell(0.3f, 0.22f, N(98), 2f, 1f, 0.15f);
                return Finish(d.S, 0.38f);
            case "stat_down":
                // Three sweeps downward, each starting lower
                d = new SoundDesign(0.5f);
                for (int k = 0; k < 3; k++)
                    d.Tone(k * 0.11f, 0.16f, Wave.Triangle, 1300f * (1f - 0.18f * k), 420f * (1f - 0.18f * k), 0.5f, 0.15f, 1.4f);
                return Finish(d.S, 0.38f);
            case "status_poison":
                // A sickly bubbling
                d = new SoundDesign(0.55f, 0x9050u);
                d.Noise(0, 0.5f, Filter.Low, 600f, 400f, 0.3f, 2.5f, 0.2f, 1.2f, 11f, 0.5f);
                d.Bubbles(0, 0.45f, 8, 260f, 640f, 0.5f);
                return Finish(d.S, 0.4f);
            case "status_burn":
                // Flames catch: a whoosh with crackling
                d = new SoundDesign(0.55f, 0xB012u);
                d.Noise(0, 0.5f, Filter.Band, 600f, 1800f, 0.8f, 0.7f, 0.35f, 1.3f);
                d.Crackle(0.05f, 0.45f, 18, 2600f, 0.5f);
                return Finish(d.S, 0.42f);
            case "status_paralysis":
                // A stuttering buzz of static
                d = new SoundDesign(0.48f, 0x9A2Au);
                for (int k = 0; k < 4; k++)
                {
                    float at = k * 0.11f;
                    d.Tone(at, 0.07f, Wave.Pulse, 95f, 95f, 0.35f, 0.05f, 0.7f, duty: 0.3f);
                    d.Tone(at, 0.07f, Wave.Saw, 191f, 187f, 0.2f, 0.05f, 0.7f);
                    d.Noise(at, 0.07f, Filter.Band, 3800f, 3000f, 0.5f, 1.5f, 0.05f, 1f, 70f, 0.8f);
                }
                return Finish(d.S, 0.4f);
            case "status_sleep":
                // Drowsy notes falling, wavering
                d = new SoundDesign(0.75f);
                var fall = Pitches(76, 72, 69);
                for (int k = 0; k < fall.Length; k++)
                    d.Tone(k * 0.2f, 0.32f, Wave.Triangle, fall[k], fall[k] * 0.97f, 0.5f, 0.2f, 1.6f, vibratoHz: 5f, vibratoDepth: 0.02f);
                return Finish(d.S, 0.36f);
            case "status_freeze":
                // Ice forming: bright tinkles over a cold hiss
                d = new SoundDesign(0.6f, 0x1CEu);
                d.Noise(0, 0.55f, Filter.High, 6000f, 4500f, 0.25f, 0.7f, 0.2f, 1.4f);
                var tinkles = Pitches(100, 103, 98, 105, 101);
                for (int k = 0; k < tinkles.Length; k++) d.Bell(k * 0.07f, 0.3f, tinkles[k], 3.7f, 1.6f, 0.3f, 3f);
                return Finish(d.S, 0.38f);
            case "status_confusion":
                // Birds circling: a wobbling tone and two chirps
                d = new SoundDesign(0.6f);
                d.Tone(0, 0.55f, Wave.Sine, 620f, 560f, 0.45f, 0.15f, 1.2f, vibratoHz: 8f, vibratoDepth: 0.2f);
                d.Tone(0.12f, 0.08f, Wave.Sine, 2400f, 3200f, 0.25f, 0.1f, 2f);
                d.Tone(0.32f, 0.08f, Wave.Sine, 2600f, 3400f, 0.25f, 0.1f, 2f);
                return Finish(d.S, 0.36f);
            case "faint":
                // A long slide down, wavering, and a soft fall at the end
                d = new SoundDesign(0.9f, 0xFAu);
                d.Tone(0, 0.75f, Wave.Pulse, 620f, 90f, 0.35f, 0.02f, 0.9f, duty: 0.25f, vibratoHz: 9f, vibratoDepth: 0.025f);
                d.Tone(0, 0.75f, Wave.Triangle, 620f, 90f, 0.4f, 0.02f, 0.9f, vibratoHz: 9f, vibratoDepth: 0.025f);
                d.Thump(0.7f, 0.18f, 110f, 55f, 0.5f, 0.4f);
                return Finish(d.S, 0.42f);
            case "exp":
                // Points flowing in: a pulse climbing in small steps, as the bar fills
                d = new SoundDesign(0.75f);
                for (int k = 0; k < 16; k++)
                {
                    float f = N(64 + k);
                    d.Tone(k * 0.045f, 0.05f, Wave.Pulse, f, f, 0.35f * (0.6f + 0.4f * k / 15f), 0.15f, 0.8f, duty: 0.25f);
                }
                return Finish(d.S, 0.32f);

            // ---------------------------------------------------------------- a move's launch, by type
            case "move_normal":
                d = new SoundDesign(0.3f, 0x401u);
                d.Noise(0, 0.22f, Filter.Band, 800f, 2200f, 0.8f, 1.2f, 0.5f, 1.4f);
                d.Tone(0, 0.2f, Wave.Sine, 220f, 330f, 0.25f, 0.4f, 1.5f);
                return Finish(d.S, 0.38f);
            case "move_fire":
                // A roar of flame
                d = new SoundDesign(0.65f, 0xF13Eu);
                d.Noise(0, 0.6f, Filter.Low, 700f, 2200f, 0.9f, 1.1f, 0.25f, 1.1f, 13f, 0.35f);
                d.Crackle(0.05f, 0.55f, 22, 2400f, 0.35f);
                d.Tone(0, 0.6f, Wave.Saw, 70f, 90f, 0.12f, 0.3f, 1.1f);
                return Finish(d.S, 0.42f);
            case "move_water":
                // A gush and its spray
                d = new SoundDesign(0.6f, 0x3A7Eu);
                d.Noise(0, 0.55f, Filter.Low, 1200f, 3200f, 0.8f, 0.9f, 0.2f, 1.2f, 9f, 0.3f);
                d.Bubbles(0.05f, 0.5f, 8, 400f, 1100f, 0.3f);
                return Finish(d.S, 0.4f);
            case "move_grass":
                // Leaves on a gust, and a green chime
                d = new SoundDesign(0.55f, 0x6A55u);
                d.Noise(0, 0.5f, Filter.Band, 2200f, 3600f, 0.7f, 1.2f, 0.3f, 1.3f, 28f, 0.7f);
                d.Bell(0.05f, 0.4f, N(84), 1.5f, 1.2f, 0.2f);
                d.Bell(0.15f, 0.35f, N(91), 1.5f, 1.2f, 0.15f);
                return Finish(d.S, 0.38f);
            case "move_electric":
                // A crackling zap
                d = new SoundDesign(0.5f, 0xE1Eu);
                d.Tone(0, 0.45f, Wave.Saw, 120f, 160f, 0.3f, 0.05f, 1.2f, vibratoHz: 60f, vibratoDepth: 0.15f);
                d.Tone(0, 0.45f, Wave.Pulse, 241f, 320f, 0.15f, 0.05f, 1.2f, duty: 0.2f);
                d.Noise(0, 0.45f, Filter.Band, 4000f, 2500f, 0.6f, 1.2f, 0.02f, 1.3f, 55f, 0.9f);
                d.Crackle(0, 0.45f, 16, 4200f, 0.4f);
                return Finish(d.S, 0.42f);
            case "move_ice":
                // A crystalline shimmer with a cold wind under it
                d = new SoundDesign(0.65f, 0x1CE2u);
                d.Noise(0, 0.6f, Filter.Band, 5200f, 3000f, 0.35f, 0.9f, 0.3f, 1.3f);
                var ice = Pitches(96, 103, 100, 108, 105, 98);
                for (int k = 0; k < ice.Length; k++) d.Bell(k * 0.06f, 0.32f, ice[k], 3.7f, 1.4f, 0.28f, 3f);
                return Finish(d.S, 0.38f);
            case "move_fighting":
                // A fist cutting the air
                d = new SoundDesign(0.25f, 0xF16u);
                d.Noise(0, 0.16f, Filter.Band, 500f, 1900f, 0.9f, 1.4f, 0.6f, 1.6f);
                d.Tone(0.02f, 0.14f, Wave.Sine, 180f, 320f, 0.3f, 0.6f, 1.6f);
                return Finish(d.S, 0.4f);
            case "move_poison":
                // A sludge's gurgle
                d = new SoundDesign(0.55f, 0x9051u);
                d.Noise(0, 0.5f, Filter.Low, 500f, 800f, 0.6f, 2.5f, 0.2f, 1.2f, 7f, 0.5f);
                d.Bubbles(0.02f, 0.48f, 9, 220f, 520f, 0.45f);
                return Finish(d.S, 0.4f);
            case "move_ground":
                // The earth rumbling
                d = new SoundDesign(0.7f, 0x6D0u);
                d.Noise(0, 0.68f, Filter.Low, 260f, 160f, 1f, 1.1f, 0.25f, 1.1f, 14f, 0.6f);
                d.Tone(0, 0.65f, Wave.Sine, 52f, 44f, 0.5f, 0.2f, 1.1f);
                d.Crackle(0.1f, 0.5f, 10, 1200f, 0.25f);
                return Finish(d.S, 0.46f);
            case "move_flying":
                // A gust of wind, swelling and passing
                d = new SoundDesign(0.6f, 0xF17u);
                d.Noise(0, 0.58f, Filter.Band, 450f, 1300f, 0.9f, 0.8f, 0.45f, 1.1f, 6f, 0.4f);
                d.Noise(0.1f, 0.4f, Filter.Band, 1200f, 600f, 0.4f, 1.2f, 0.4f, 1.2f);
                return Finish(d.S, 0.38f);
            case "move_psychic":
                // A wavering, beating tone that rises as it bends the air
                d = new SoundDesign(0.7f);
                d.Tone(0, 0.68f, Wave.Sine, 440f, 660f, 0.4f, 0.35f, 1.1f, vibratoHz: 6f, vibratoDepth: 0.03f);
                d.Tone(0, 0.68f, Wave.Sine, 446f, 669f, 0.4f, 0.35f, 1.1f, vibratoHz: 6.5f, vibratoDepth: 0.03f);
                d.Tone(0, 0.68f, Wave.Triangle, 880f, 1320f, 0.15f, 0.4f, 1.3f);
                return Finish(d.S, 0.36f);
            case "move_bug":
                // A buzz of wings
                d = new SoundDesign(0.5f, 0xB06u);
                d.Tone(0, 0.48f, Wave.Saw, 180f, 210f, 0.35f, 0.2f, 1.1f, vibratoHz: 30f, vibratoDepth: 0.06f);
                d.Tone(0, 0.48f, Wave.Pulse, 362f, 420f, 0.15f, 0.2f, 1.1f, duty: 0.2f, vibratoHz: 31f, vibratoDepth: 0.06f);
                return Finish(d.S, 0.36f);
            case "move_rock":
                // Stones grinding and tumbling
                d = new SoundDesign(0.5f, 0x50Cu);
                d.Noise(0, 0.45f, Filter.Low, 900f, 400f, 0.6f, 1f, 0.1f, 1.4f, 12f, 0.4f);
                d.Crackle(0, 0.45f, 16, 1500f, 0.6f);
                d.Thump(0.02f, 0.15f, 120f, 60f, 0.4f, 0.5f);
                return Finish(d.S, 0.44f);
            case "move_ghost":
                // An eerie voice swelling out of nothing and sliding away
                d = new SoundDesign(0.8f);
                d.Tone(0, 0.78f, Wave.Sine, 900f, 420f, 0.5f, 0.45f, 1.2f, vibratoHz: 7f, vibratoDepth: 0.05f);
                d.Tone(0, 0.78f, Wave.Triangle, 1350f, 630f, 0.15f, 0.5f, 1.2f, vibratoHz: 7f, vibratoDepth: 0.05f);
                d.Echo(0.13f, 0.3f);
                return Finish(d.S, 0.36f);
            case "move_dragon":
                // A deep, rough roar
                d = new SoundDesign(0.7f, 0xD8Au);
                d.Tone(0, 0.66f, Wave.Saw, 90f, 70f, 0.4f, 0.2f, 1.2f, vibratoHz: 24f, vibratoDepth: 0.05f);
                d.Tone(0, 0.66f, Wave.Saw, 135f, 104f, 0.25f, 0.2f, 1.2f, vibratoHz: 25f, vibratoDepth: 0.05f);
                d.Noise(0, 0.66f, Filter.Low, 1400f, 600f, 0.4f, 1.2f, 0.2f, 1.2f, 24f, 0.5f);
                return Finish(d.S, 0.42f);
            case "move_steel":
                // A metal clang
                d = new SoundDesign(0.65f, 0x57Eu);
                d.Bell(0, 0.62f, 620f, 1.41f, 4f, 0.5f, 2f);
                d.Bell(0, 0.5f, 1563f, 2.76f, 2f, 0.25f, 2.5f);
                d.Noise(0, 0.05f, Filter.High, 4000f, 4000f, 0.3f, 0.7f, 0.05f, 3f);
                return Finish(d.S, 0.4f);
            case "move_dark":
                // A low, menacing sweep
                d = new SoundDesign(0.6f, 0xDA2u);
                d.Tone(0, 0.58f, Wave.Pulse, 82f, 62f, 0.35f, 0.4f, 1.2f, duty: 0.35f);
                d.Noise(0, 0.58f, Filter.Band, 400f, 1100f, 0.5f, 1f, 0.55f, 1.4f);
                return Finish(d.S, 0.4f);
            case "move_fairy":
                // A twinkle, climbing
                d = new SoundDesign(0.62f);
                d.Notes(0, 0.055f, 0.3f, Wave.Sine, Pitches(91, 95, 98, 103, 107), 0.35f, 0.04f, 2f);
                d.Bell(0.2f, 0.4f, N(103), 2.01f, 1.4f, 0.2f);
                return Finish(d.S, 0.36f);

            default:
                throw new ArgumentOutOfRangeException(nameof(name), name, "not a sound of the bank");
        }
    }
}
