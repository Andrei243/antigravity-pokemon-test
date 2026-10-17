using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokemonPlatinumEngine.Data;
using static PokemonPlatinumEngine.Audio.SoundDesign;

namespace PokemonPlatinumEngine.Audio;

// A move's own sound (plan 25 · Y6): one entry per move, use_<key>, built from the same layers as the rest of the
// bank. Its beats are the original's (when it rings, what it is made of, read from the move's animation script), its
// sound our own; the entry names the original's first sound of the move. A move without one sets off with its type's
// sound until its batch gives it its own.
public static partial class SoundBank
{
    /// <summary>The key a move's own sound goes by: its name in lower case, words joined by underscores ("Mud-Slap" is mud_slap).</summary>
    public static string MoveKey(string move)
    {
        var key = new StringBuilder();
        foreach (char c in move.ToLowerInvariant())
        {
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9') key.Append(c);
            else if (key.Length > 0 && key[^1] != '_') key.Append('_');
        }
        return key.ToString().TrimEnd('_');
    }

    /// <summary>The sound a move makes as it sets off: its own <c>use_</c> sound, or its type's.</summary>
    public static string MoveSound(string move, PokemonType type) =>
        move.Length > 0 && Exists("use_" + MoveKey(move)) ? "use_" + MoveKey(move) : MoveSound(type);

    /// <summary>The moves whose animation is the user's own voice, with the cry modes the original plays them in, in order.</summary>
    private static readonly Dictionary<string, CryMode[]> CryMoves = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Growl"] = new[] { CryMode.Uproar1, CryMode.Uproar2 },
        ["Roar"] = new[] { CryMode.Howl1, CryMode.Howl2 },
        ["Hyper Voice"] = new[] { CryMode.HyperVoice1, CryMode.HyperVoice2 },
        ["Howl"] = new[] { CryMode.MidMove },
        ["Chatter"] = new[] { CryMode.Normal }
    };

    /// <summary>The cry modes a move is heard in (it is the user's voice), or null for a move with a sound.</summary>
    public static CryMode[]? CryModesOf(string move) => CryMoves.TryGetValue(move, out var modes) ? modes : null;

    /// <summary>The moves heard as the user's voice.</summary>
    public static IReadOnlyCollection<string> MovesHeardAsCries => CryMoves.Keys;

    /// <summary>
    /// The moves with a sound of their own, by batch (plan 25): the move's name, the original's first sound, what it
    /// is. A property, not a field: the catalogue is read as the bank's own fields are made, in whatever order the
    /// files are read.
    /// </summary>
    private static (string Move, string? Original, string What)[] MoveSounds => new (string, string?, string)[]
    {
        // ---- Y6, the first chapter
        ("Leer", "SEQ_SE_DP_W043", "a glint and a low hum as the eyes narrow, twice"),
        ("Pound", "SEQ_SE_DP_030", "a flat slap"),
        ("Scratch", "SEQ_SE_DP_W010", "claws raked once, a dry scrape"),
        ("Tackle", "SEQ_SE_DP_050", "a run's rush and a body's thud"),
        ("Withdraw", "SEQ_SE_DP_W029", "a shell drawn shut: a slide down and a hollow knock"),
        ("Bide", "SEQ_SE_DP_W025B", "a low swell held under a pulse, then a thump"),
        ("Return", "SEQ_SE_DP_W039", "a warm rising chime and a soft blow"),
        ("Quick Attack", "SEQ_SE_DP_W104", "a whip of air and a quick knock"),
        ("Absorb", "SEQ_SE_DP_W071B", "a soft draw upward and a little bell"),
        ("Bubble", "SEQ_SE_DP_W145", "a string of bubbles popping"),
        ("Ember", "SEQ_SE_DP_W052", "a small flame's puff and crackle"),
        ("Fake Out", "SEQ_SE_DP_W166", "a sharp clap and its ring"),
        ("Fury Swipes", "SEQ_SE_DP_W010", "two scrapes, quick on one another"),
        ("Poison Gas", "SEQ_SE_DP_W054", "a slow hiss of gas, bubbling"),
        ("Screech", "SEQ_SE_DP_020", "a grating squeal that wavers"),
        ("Splash", "SEQ_SE_DP_023", "a flop and its splash"),
        ("Growth", "SEQ_SE_DP_W036", "a green swell climbing"),
        ("Leech Life", "SEQ_SE_DP_W071", "a needle's tick and a sip drawn back"),
        ("Low Kick", "SEQ_SE_DP_007", "a low sweep of air and a knock at the feet"),
        ("Supersonic", "SEQ_SE_DP_W048", "a thin, high wobbling whine"),
        ("Tail Whip", "SEQ_SE_DP_112", "a tail swished twice"),
        ("Teleport", "SEQ_SE_PL_W100", "a shimmer rising out of hearing"),
        ("Water Sport", "SEQ_SE_DP_W057", "water thrown up and pattering down"),
        ("Defense Curl", "SEQ_SE_DP_W161", "a body rolled up: a soft curl and a pop"),
        ("Mud Sport", "SEQ_SE_DP_W091", "wet mud flung up and slapping down"),
        ("Rock Smash", "SEQ_SE_DP_W233B", "a punch and stone cracking"),
        ("Aerial Ace", "SEQ_SE_DP_W013B", "a rush of air and a clean slash"),
        ("Astonish", "SEQ_SE_DP_W280B", "a sudden boo: a jump of pitch and a shiver"),
        ("Brick Break", "SEQ_SE_DP_W233", "a chop and something breaking"),
        ("Counter", "SEQ_SE_DP_BASI", "a turn caught and struck back"),
        ("Earthquake", "SEQ_SE_DP_W089", "the ground heaving, a long rumble"),
        ("Flash", "SEQ_SE_DP_W043", "a bright pop of light ringing"),
        ("Focus Punch", "SEQ_SE_DP_W082", "focus gathering, a rush and a heavy blow"),
        ("Force Palm", "SEQ_SE_DP_208", "a palm's push and a ringing shock"),
        ("Rock Polish", "SEQ_SE_DP_BRADE", "stone rubbed bright, a glint at the end"),
        ("Rock Slide", "SEQ_SE_DP_W088", "rocks tumbling down"),
        ("Take Down", "SEQ_SE_DP_W029", "a step back, a charge and a hard crash"),
        ("Water Gun", "SEQ_SE_DP_W055", "a squirt of water"),
        ("Harden", "SEQ_SE_DP_W231", "a body set hard: a metal ring"),
        ("Headbutt", "SEQ_SE_DP_W029", "a run and a hard knock of heads"),
        ("Pursuit", "SEQ_SE_DP_030", "a dark rush closing in and a blow"),
        ("Rock Throw", "SEQ_SE_DP_W088", "stones thrown, landing one, two"),
        ("Stealth Rock", "SEQ_SE_DP_W070", "stones scattered, settling round the foe"),
        ("Focus Energy", "SEQ_SE_DP_W082", "breath drawn in and held, rising"),
        ("Secret Power", null, "a hidden blow: a quick rising whisper and a knock"),
        ("Camouflage", null, "colours washing over the user: a soft shimmer sliding down"),
        // ---- Y7, Route 204 to Team Galactic's Eterna Building
        ("String Shot", "SEQ_SE_DP_W081B", "silk spat out in a thin hiss, then wound tight"),
        ("Rock Tomb", "SEQ_SE_DP_W070", "rocks dropped one after another, four heavy knocks"),
        ("Water Pulse", "SEQ_SE_DP_W145C", "a ball of water throbbing as it rolls, and its break"),
        ("Bullet Seed", "SEQ_SE_DP_W202B", "seeds spat in a quick rattle"),
        ("Charge", "SEQ_SE_DP_W360", "a hum of electricity gathering, buzzing louder"),
        ("Charm", "SEQ_SE_DP_W204", "two light hops of a chime, sweetly"),
        ("Leech Seed", "SEQ_SE_DP_001", "seeds tossed with a soft pop and a sprout's creak"),
        ("Poison Sting", "SEQ_SE_DP_W004", "a needle's jab and a fizz"),
        ("Stun Spore", "SEQ_SE_DP_W077B", "a puff of powder sifting down, crackling"),
        ("Wing Attack", "SEQ_SE_DP_W017", "two beats of wings and a slap"),
        ("Pluck", "SEQ_SE_DP_030", "three quick pecks"),
        ("Baton Pass", "SEQ_SE_DP_SLOT02", "a baton whirled, a bright ring as it is passed"),
        ("Foresight", "SEQ_SE_DP_100", "a lens turning into focus, a ping"),
        ("Karate Chop", "SEQ_SE_DP_HURU", "a hand cutting the air and a crack"),
        ("Mega Drain", "SEQ_SE_DP_W145C", "a long draw upward with a bell at its end"),
        ("Mud Bomb", "SEQ_SE_DP_W145C", "a wet lob and a heavy splat"),
        ("Mud-Slap", "SEQ_SE_DP_W028", "mud flung, a wet slap"),
        ("Peck", "SEQ_SE_DP_W030", "two hard pecks"),
        ("Rage", "SEQ_SE_DP_131", "a low snarl rising and a blow"),
        ("Sand Attack", "SEQ_SE_DP_W028", "two dry scatters of sand"),
        ("Sonic Boom", "SEQ_SE_DP_146", "three quick booms of air"),
        ("Hypnosis", "SEQ_SE_DP_154", "a slow wavering tone, rising and falling"),
        ("Thunderbolt", "SEQ_SE_DP_KAMI", "thunder cracking down three times"),
        ("Bite", "SEQ_SE_DP_W044", "jaws snapping shut"),
        ("Feint Attack", "SEQ_SE_DP_061", "a vanishing whoosh, then a blow from behind"),
        ("Toxic", "SEQ_SE_DP_161", "a thick bubbling, fouler and lower"),
        ("Agility", "SEQ_SE_PL_W097", "two quick swishes side to side"),
        ("Aqua Jet", "SEQ_SE_DP_W029", "a rushing jet of water and a splash"),
        ("Body Slam", "SEQ_SE_DP_W036", "a heavy leap and a crash"),
        ("Confuse Ray", "SEQ_SE_DP_153", "an eerie wavering tone circling"),
        ("Crunch", "SEQ_SE_DP_W044", "jaws snapping hard and something breaking"),
        ("Fire Punch", "SEQ_SE_DP_BASI", "a punch and a burst of flame"),
        ("Fire Spin", "SEQ_SE_DP_W221B", "flames roaring round in a whirl"),
        ("Hidden Power", "SEQ_SE_DP_W036", "a soft swell of chimes, then a gathering rush"),
        ("Lock-On", "SEQ_SE_DP_GETTING", "beeps quickening to a steady tone"),
        ("Magnet Bomb", "SEQ_SE_DP_290", "metal humming in and four sharp pops"),
        ("Rain Dance", "SEQ_SE_DP_W240", "clouds rumbling and rain pattering down"),
        ("Spark", "SEQ_SE_DP_W209", "a crackle round the user and a zap"),
        ("Swift", "SEQ_SE_DP_165", "a twinkling run of stars"),
        ("Thunder Wave", "SEQ_SE_DP_W085C", "a buzzing ring tightening, crackling"),
        ("Confusion", "SEQ_SE_DP_480", "a warbling ripple of the mind"),
        ("Gust", "SEQ_SE_DP_W239", "a gust of wind whirling up"),
        ("Horn Attack", "SEQ_SE_DP_W030", "two hard jabs of a horn"),
        ("Rollout", "SEQ_SE_DP_W025B", "a rumbling roll and a knock"),
        ("Sleep Talk", "SEQ_SE_DP_W173B", "a sleepy mumble, low and wobbling"),
        ("Curse", "SEQ_SE_DP_W185", "a dark low swell and a shudder"),
        ("Endure", "SEQ_SE_DP_W082", "a body braced: a held low pulse"),
        ("Frustration", "SEQ_SE_DP_W082", "an angry huff and a flurry of blows"),
        ("Mean Look", "SEQ_SE_DP_W060", "a deep stare: a low tone and a hiss"),
        ("Meditate", "SEQ_SE_DP_W029", "a calm breath and a soft bell"),
        ("Protect", "SEQ_SE_DP_W115", "a shield springing up with a glassy ring"),
        ("Spite", "SEQ_SE_DP_W060", "a thin, sour wail drawn out"),
        ("Night Shade", "SEQ_SE_DP_W109", "a dark drone and a ghostly whoosh"),
        ("Substitute", "SEQ_SE_DP_061", "a puff of smoke and a pop"),
        ("Cut", "SEQ_SE_DP_W015", "one clean slicing swish"),
        ("Thief", "SEQ_SE_DP_W233", "a quick snatch and a jingle"),
        ("Grass Knot", "SEQ_SE_DP_300", "grass rustling up and pulling tight"),
        ("Magical Leaf", "SEQ_SE_DP_PASA2", "leaves rustling round with a twinkle"),
        ("Razor Leaf", "SEQ_SE_DP_KAZE", "leaves whirring through the air"),
        ("Reflect", "SEQ_SE_DP_W115", "panels of a wall chiming up one after another"),
        ("Safeguard", "SEQ_SE_DP_W208", "a soft, shining chord settling"),
        ("Smokescreen", "SEQ_SE_DP_W104", "a lobbed pop and smoke billowing"),
        ("Sunny Day", "SEQ_SE_DP_W080", "a warm swell of light"),
        ("Recycle", "SEQ_SE_DP_W278", "a whirring loop and a ding"),
        ("Taunt", "SEQ_SE_DP_W118", "a mocking two-note call"),
        ("Giga Drain", "SEQ_SE_DP_W202", "a great draw upward, leafy, with a bell"),
        ("Night Slash", "SEQ_SE_DP_BRADE", "a dark blade drawn and two slashes"),
        // ---- Y7b, Route 206 to Route 208 (Roar is the user's voice and has no entry)
        ("Double Team", "SEQ_SE_DP_053", "four quick swishes, one after another, rising"),
        ("Endeavor", "SEQ_SE_DP_W039", "four quick flat blows"),
        ("Flame Wheel", "SEQ_SE_DP_W172", "a fiery whirr winding up, then a crackling break"),
        ("Fury Cutter", "SEQ_SE_DP_W043", "three thin cuts of the air, the last with a ring"),
        ("Knock Off", "SEQ_SE_DP_W233", "a smack and a small thing popping off with a chirp"),
        ("Magnitude", "SEQ_SE_DP_W070", "the ground heaving: four low thumps and a rumble"),
        ("Seismic Toss", "SEQ_SE_DP_W327", "a whoosh up, a whoosh down, a heavy slam and a rebound"),
        ("Self-Destruct", "SEQ_SE_DP_W120", "a fuse hissing higher and a great boom"),
        ("Slam", "SEQ_SE_DP_W025B", "a whip through the air and a hard slap"),
        ("Stomp", "SEQ_SE_DP_W036", "a heavy thud and a knock"),
        ("Block", "SEQ_SE_DP_050", "two hard clanks of metal and a low ring"),
        ("Detect", "SEQ_SE_DP_081", "a bright rising chime and a soft ring"),
        ("Mind Reader", "SEQ_SE_DP_W109", "a scanning whir sweeping up and down, a ping"),
        ("Disable", "SEQ_SE_DP_081", "a tone sagging out of tune and a dull pop"),
        ("Dragon Rage", "SEQ_SE_DP_W082C", "a roaring blaze rushing over and its blast, twice"),
        ("Extrasensory", "SEQ_SE_DP_W020", "three throbbing pulses rising and a ring"),
        ("Imprison", "SEQ_SE_DP_050", "five clinks of bars dropped in turn and a deep lock"),
        ("Sand Tomb", "SEQ_SE_DP_W028", "gritty sand swirling round and round"),
        ("Sandstorm", "SEQ_SE_DP_W201", "a long wind howling with sand in it"),
        ("Tickle", "SEQ_SE_DP_W197", "four squeaky giggles over a sparkle"),
        ("Uproar", "SEQ_SE_DP_W253", "a loud blaring din, three times"),
        ("Double Slap", "SEQ_SE_DP_030C", "two flat slaps"),
        ("Follow Me", "SEQ_SE_DP_W213", "a bright call for attention and three bouncy pops"),
        ("Sing", "SEQ_SE_DP_W047", "a soft lullaby of three long notes"),
        ("Bug Bite", "SEQ_SE_DP_W025B", "four quick crunching bites"),
        ("Encore", "SEQ_SE_DP_W227B", "five claps in a row over a little tune"),
        ("Explosion", "SEQ_SE_DP_SHUSHU", "a fuse hissing higher, a huge boom and a long roll"),
        ("Flail", "SEQ_SE_DP_050", "six frantic flaps and a thump"),
        ("Ice Fang", "SEQ_SE_DP_W044", "a chomp and an icy crackle with a chime"),
        ("Lucky Chant", "SEQ_SE_DP_163", "bright bells climbing in a chant"),
        ("Mimic", "SEQ_SE_DP_W107", "a warbling tone copied a beat later and a pop"),
        ("Rest", "SEQ_SE_DP_W173B", "two slow snoring breaths and a soft bell"),
        ("Revenge", "SEQ_SE_DP_W036", "a low growl gathering and one heavy punch"),
        ("Rock Blast", "SEQ_SE_DP_W207", "four rocks flung with knocks and rubble"),
        ("Snore", "SEQ_SE_DP_W173B", "two big rumbling snores"),
        ("Sweet Scent", "SEQ_SE_DP_W230", "a soft sweep of chimes with a shimmer"),
        ("Twister", "SEQ_SE_DP_060", "a wind spiralling up and round"),
        ("Worry Seed", "SEQ_SE_DP_001", "a seed tossed with a plop and two uneasy notes"),
        // ---- Y8a, Hearthome City's gate to the Café Cabin (Chatter is the user's voice and has no entry)
        ("Bubble Beam", "SEQ_SE_DP_AWA", "a stream of bubbling pops and a final burst"),
        ("Mach Punch", "SEQ_SE_DP_W010", "a fast whoosh and a flat crack"),
        ("Metal Claw", "SEQ_SE_DP_W231", "two metallic rakes, each with a ring"),
        ("Future Sight", "SEQ_SE_DP_W060", "a pulsing hum and a foggy chime"),
        ("Pain Split", "SEQ_SE_DP_W025B", "six pangs, one side and the other in turn"),
        ("Psybeam", "SEQ_SE_DP_W060C", "a wobbling beam crossing the field and a dull bang"),
        ("Shadow Ball", "SEQ_SE_DP_W028", "a dark swirl building, a throw and a dull burst"),
        ("Shadow Claw", "SEQ_SE_DP_161", "two ghostly rakes and a hollow toll"),
        ("Shadow Sneak", "SEQ_SE_DP_161", "a hush along the ground and a muffled strike"),
        ("Spit Up", "SEQ_SE_DP_W036", "a heave, a pop and a wet splat"),
        ("Stockpile", "SEQ_SE_DP_W025", "two swelling breaths, each ending in a soft ping"),
        ("Sucker Punch", "SEQ_SE_DP_143", "a sudden dull smack and a second, smaller"),
        ("Swallow", "SEQ_SE_DP_W036", "a gathering rush, a gulp and a soft chime"),
        ("Will-O-Wisp", "SEQ_SE_DP_W052", "a flickering cold wind that ignites with a whoosh"),
        ("Minimize", "SEQ_SE_DP_W107", "a tone sliding down and shrinking, with little pops"),
        ("Payback", "SEQ_SE_DP_W185", "a heavy growl gathering and four hard knocks with a jingle"),
        ("Attract", "SEQ_SE_DP_W204", "a sweet lilt, two kisses and a bell"),
        ("Amnesia", "SEQ_SE_DP_W118", "a hollow mutter and a bubbling drift away"),
        ("Aqua Ring", "SEQ_SE_DP_W392", "a gentle watery shimmer closing into a ring"),
        ("Copycat", "SEQ_SE_DP_SHUSHU", "a rising hiss and a tune repeated a beat later"),
        ("Fury Attack", "SEQ_SE_DP_W030", "four quick jabs"),
        ("Jump Kick", "SEQ_SE_DP_131", "a spring, a whoosh and one hard kick"),
        ("Light Screen", "SEQ_SE_DP_W115", "a curtain of chimes shimmering up"),
        ("Nasty Plot", "SEQ_SE_DP_W118", "a low scheming murmur and a sly rising note"),
        ("Refresh", "SEQ_SE_DP_W287", "a clean breeze of chimes"),
        ("Soft-Boiled", "SEQ_SE_DP_112", "a wobbling egg, a crack and a warm bell"),
        ("Steel Wing", "SEQ_SE_DP_W231", "two metal whooshes and four ringing strikes"),
        ("Sweet Kiss", "SEQ_SE_DP_041", "a kiss and a dizzy little tune"),
        ("Whirlwind", "SEQ_SE_DP_KAZE", "a great wind rising and a gust carrying off"),
        ("Mirror Move", null, "a bright silver ping and a glassy ring (the original plays none of its own)"),
        ("Haze", "SEQ_SE_DP_W114", "a long cold hiss and a murmur of mist"),
        ("Defog", "SEQ_SE_DP_BFBRADE", "a wind rushing across the field and clearing"),
        ("Dig", "SEQ_SE_DP_W091", "dirt scrabbled three times, a burst out of the ground and a thud"),
        ("Yawn", "SEQ_SE_DP_W281", "a long drowsy sigh carried across"),
        ("Acupressure", "SEQ_SE_DP_081", "three presses, each with a ping, and a warm chime"),
        ("Air Cutter", "SEQ_SE_DP_W104", "a whoosh and two cuts, crossing"),
        ("False Swipe", "SEQ_SE_DP_W104", "a whoosh and a soft tap of a blade"),
        ("Feint", "SEQ_SE_DP_W104", "two faint whooshes, then one sharp quick hit"),
        ("Metronome", "SEQ_SE_DP_W118", "a pendulum ticking and a bright flourish"),
        ("Pin Missile", "SEQ_SE_DP_W026", "three light whistling needles, each with a prick"),
        ("Roost", "SEQ_SE_DP_210", "wings settling, a soft sigh and a chime"),
        ("Scary Face", "SEQ_SE_DP_W060", "a sudden low roar-hiss and a stab of high notes"),
        ("Shock Wave", "SEQ_SE_DP_W082", "a rising crackle and a crooked zap"),
        ("Slash", "SEQ_SE_DP_W013", "three quick sharp swishes, the last with a ring"),
        ("Swagger", "SEQ_SE_DP_W207D", "a cocky rising fanfare and two hard snorts"),
        ("Thunder Shock", "SEQ_SE_DP_W085", "a short sharp zap and a crackle"),
        ("Toxic Spikes", "SEQ_SE_DP_W026", "five spikes thrown with a prick each, and bubbling"),
        ("Wake-Up Slap", "SEQ_SE_DP_030C", "four slaps and a ring"),
        ("Cosmic Power", "SEQ_SE_DP_W322", "a deep space hum with twinkling bells"),
        ("Lick", "SEQ_SE_DP_W122", "a wet slurp"),
        // ---- Y8b, Route 210 north to the Seven Stars Restaurant
        ("Air Slash", "SEQ_SE_DP_BRADE", "a gathering wind and one sharp slash with a ring"),
        ("Ancient Power", "SEQ_SE_DP_W082", "a low rumble, rocks clattering up and a heavy burst of stones"),
        ("Aqua Tail", "SEQ_SE_DP_W057", "a coil of rushing water, a crack of the whip and a splash"),
        ("Assurance", "SEQ_SE_DP_050", "a dull blow and a heavier one behind it"),
        ("Calm Mind", "SEQ_SE_DP_W048", "a still, low hum and soft bells settling"),
        ("Double Hit", "SEQ_SE_DP_030", "two swings, each with a thud"),
        ("Dragon Claw", "SEQ_SE_DP_W221B", "a growling roar rising and two ringing rakes"),
        ("Hyper Fang", "SEQ_SE_DP_W044", "a snap of teeth and a heavy crunch"),
        ("Ingrain", "SEQ_SE_DP_W010", "roots creeping into the ground with three soft thuds"),
        ("Iron Defense", "SEQ_SE_DP_W231", "two bright metallic rings, each sweeping up"),
        ("Mist", "SEQ_SE_DP_W109", "a long, soft hiss of mist and a faint shimmer"),
        ("Poison Powder", "SEQ_SE_DP_W077B", "a puff of powder and a long sprinkling hiss"),
        ("Submission", "SEQ_SE_DP_W104", "a grip and a strained wrench, then five blows"),
        ("Vital Throw", "SEQ_SE_DP_W233", "a spinning whoosh up and over, and a heavy slam"),
        ("Whirlpool", "SEQ_SE_DP_W250", "water churning round and round with bubbles"),
        ("Beat Up", "SEQ_SE_DP_W233B", "three quick smacks and a heavier one"),
        ("Fire Fang", "SEQ_SE_DP_W044", "a snap of teeth that ignites into a crackling roar"),
        ("Odor Sleuth", "SEQ_SE_DP_W207", "four quick sniffs and a bark"),
        ("Kinesis", "SEQ_SE_DP_W060", "a wavering tone and a bell struck at each swing"),
        ("Metal Sound", "SEQ_SE_DP_W103", "a shrieking scrape of metal"),
        ("Miracle Eye", "SEQ_SE_DP_183", "a rising tone, three notes and a bright bell"),
        ("Ominous Wind", "SEQ_SE_DP_W466", "a cold wind moaning past twice"),
        ("Psychic", "SEQ_SE_DP_480", "a wobbling tone climbing and a heavy crush"),
        ("Wrap", "SEQ_SE_DP_W020B", "a rustling coil winding round, twice"),
        ("Embargo", "SEQ_SE_DP_W373", "two falling buzzes and a hard stamp"),
        ("Gravity", "SEQ_SE_DP_W356", "a deep tone sinking and a heavy press"),
        ("Psycho Cut", "SEQ_SE_DP_020", "a humming gather and three sharp cuts rising"),
        ("Torment", "SEQ_SE_DP_W029", "six sharp jabs and a sour growl"),
        ("Assist", "SEQ_SE_DP_W010", "four quick ticks, each with a bell, and a bright pop"),
        ("Bone Rush", "SEQ_SE_DP_W155", "a bone whirring in, two hollow knocks and rattling"),
        ("Captivate", "SEQ_SE_DP_164", "a sweet rising lilt and a shimmer of sparkles"),
        ("Drain Punch", "SEQ_SE_DP_050", "a solid punch and a long draw upward"),
        ("Fly", "SEQ_SE_DP_W327", "a whoosh up, a whistle down and a hard landing"),
        ("Recover", "SEQ_SE_DP_W025", "a warm swelling tone, three bells climbing and a chime"),
        ("Snatch", "SEQ_SE_DP_W036", "a swift grab, a small pop and a sly jingle"),
        ("Strength", "SEQ_SE_DP_W036", "a straining groan and a heavy shove with rubble"),
        ("Ice Beam", "SEQ_SE_DP_025", "a thin icy beam and frost crackling to a chime"),
        ("Poison Fang", "SEQ_SE_DP_W044", "a snap of teeth and a bubbling ooze"),
        ("Poison Jab", "SEQ_SE_DP_030", "two quick jabs, each with a wet pop"),
        ("Signal Beam", "SEQ_SE_DP_W062", "a beam of pulsing tones changing in turn"),
        ("Sludge Bomb", "SEQ_SE_DP_W145B", "a gurgling lob and a wet splat"),
        ("Tri Attack", "SEQ_SE_DP_W161", "a rising gather and three different bursts in turn"),
        ("Memento", "SEQ_SE_DP_W060", "a dark sinking drone and a tolling bell"),
        ("Drill Peck", "SEQ_SE_DP_W029", "a rising whir and eight quick pecks"),
        ("Ice Punch", "SEQ_SE_DP_BASI", "a hard punch and a crackle of frost with a chime"),
        ("Gyro Ball", "SEQ_SE_DP_W360", "a whirring spin rising and a metallic crash"),
        ("X-Scissor", "SEQ_SE_DP_W015", "two crossing cuts, each with a ring"),
        ("Horn Drill", "SEQ_SE_DP_W030", "a drilling whine, three times, each ending in a knock"),
        ("Last Resort", "SEQ_SE_DP_W387", "four notes drawn in and two crushing blows"),
        ("Magnet Rise", "SEQ_SE_DP_W085B", "a humming field rising with two bells"),
        ("Smog", "SEQ_SE_DP_W109", "three wheezing belches and a thick hiss"),
        ("Vine Whip", "SEQ_SE_PL_WHIP", "a whistle through the air, a crack and rustling leaves"),
        ("Dream Eater", "SEQ_SE_DP_153", "soft dreamy notes drawn in with a gulp and a chime"),
        ("Grass Whistle", "SEQ_SE_DP_W320", "a bright whistle, a lilting tune and a hush"),
        ("Reversal", "SEQ_SE_DP_W197", "two bright surges, a blow and a crushing second blow"),
        // ---- Y9a, Lake Valor to Route 222
        ("Psych Up", "SEQ_SE_DP_W060B", "a rising shimmer, a run of bells and a chime as it settles"),
        ("Thunder", "SEQ_SE_DP_W161B", "a rumble, a flash-crack of lightning and a long rolling boom"),
        ("Aurora Beam", "SEQ_SE_DP_W062", "a bright rising glide with a ripple of bells and a sparkle"),
        ("Barrier", "SEQ_SE_DP_W112B", "five panes of glass set one after another, each with a ring"),
        ("Double-Edge", "SEQ_SE_DP_W025", "a rush, a crash and a rebounding knock on the user"),
        ("Thrash", "SEQ_SE_DP_007", "a growling rage and three wild blows"),
        ("Trick Room", "SEQ_SE_DP_061", "a low hum with a strange run of notes turning back on themselves"),
        ("Brine", "SEQ_SE_DP_W362", "a stinging spray and a splash"),
        ("Mud Shot", "SEQ_SE_DP_W250", "three wet squelches and a heavy splat"),
        ("Waterfall", "SEQ_SE_DP_W152", "a pouring torrent, a rise and a crashing splash"),
        ("Bind", "SEQ_SE_DP_W020B", "two creaking squeezes"),
        ("Silver Wind", "SEQ_SE_DP_W016", "a long gust with a glitter of high notes"),
        ("Synthesis", "SEQ_SE_DP_W025", "a warm rising tone with soft chimes"),
        ("Thunder Punch", "SEQ_SE_DP_BASI", "a heavy punch and a crackle of lightning"),
        ("Helping Hand", "SEQ_SE_DP_W227", "two pats and a rising chime"),
        ("Bounce", "SEQ_SE_PL_W100_1", "a spring up, a hush in the air and a heavy landing"),
        ("Egg Bomb", "SEQ_SE_DP_W039", "a lobbed whistle and a rolling blast"),
        ("Fling", "SEQ_SE_DP_W374", "a fast whip through the air and a hard knock"),
        ("Meteor Mash", "SEQ_SE_DP_W112", "a falling comet, a boom and a ringing steel punch"),
        ("Petal Dance", "SEQ_SE_DP_W080", "a rustling whirl of petals and three flung gusts"),
        ("Wish", "SEQ_SE_DP_W115", "a falling star's whistle, a warm bell and a rising twinkle"),
        ("U-turn", "SEQ_SE_DP_080", "a dash, a hit and a swerving dash back"),
        ("Flash Cannon", "SEQ_SE_DP_W082", "a rising whine, a flash of high light and a boom"),
        ("Iron Tail", "SEQ_SE_DP_W231", "a ring of steel, a heavy swing and a clang"),
        ("Metal Burst", "SEQ_SE_DP_W368", "two rising rings of metal and a bursting boom"),
        ("Skill Swap", "SEQ_SE_DP_W179", "six bell pings that change pitch halfway"),
        ("Stone Edge", "SEQ_SE_DP_W088", "six stones thrust up one after another and a heavy shatter"),
        ("Dragon Breath", "SEQ_SE_DP_400", "a growl and two puffs of roaring fire"),
        ("Close Combat", "SEQ_SE_DP_003", "eight quick blows, each a notch heavier, and a last thud"),
        ("Dizzy Punch", "SEQ_SE_DP_W004", "two punches and a wobbling run of dizzy notes"),
        ("High Jump Kick", "SEQ_SE_DP_W026", "a spring up, a whoosh down and a heavy kick"),
        ("Cross Chop", "SEQ_SE_DP_W025", "two chops in turn and a hard cross-chop with a ring"),
        ("Ice Shard", "SEQ_SE_DP_KAZE", "an icy gathering, a chime and a sharp cracking hit"),
        ("Swords Dance", "SEQ_SE_DP_SHUSHU", "four blade-swishes round a rising hum and a bright ring"),
        ("Wood Hammer", "SEQ_SE_DP_W452", "a swing, a hollow wooden crash and a rustle of leaves"),
        ("Heal Bell", "SEQ_SE_DP_W234", "three ringing bells with a soft run of high notes over them"),
        ("Psywave", "SEQ_SE_DP_181", "three wobbling psychic waves and a ringing break"),
        ("Fire Blast", "SEQ_SE_DP_W172", "a roaring gather, a flash and a great burning boom"),
        ("Flamethrower", "SEQ_SE_DP_W053", "a long steady roar of flame with a crackle"),
        ("Punishment", "SEQ_SE_DP_W104", "a whip-crack, a thud and three stern beeps"),
        ("Conversion 2", "SEQ_SE_DP_W112", "a scanning sweep and a run of changing notes"),
        ("Icy Wind", "SEQ_SE_DP_W016B", "a long cold wind with chimes of frost"),
        ("Shadow Punch", "SEQ_SE_DP_W026", "a hollow ghostly rush and a cold dull blow"),
        ("Hail", "SEQ_SE_DP_W258", "a rattle of ice and many small stones hitting"),
        ("Rock Climb", "SEQ_SE_DP_W088", "four stone steps heaving up and a charge into a crash"),
        ("Avalanche", "SEQ_SE_DP_W419", "a long rumbling slide of snow and a heavy pile-up"),
        ("Blizzard", "SEQ_SE_DP_KAZE", "a howling snowstorm, bells of frost and a freezing crack"),
        ("Focus Blast", "SEQ_SE_DP_W062", "a rising gather of power, a throw and a great blast"),
        ("Discharge", "SEQ_SE_DP_W085B", "a charging whine and a long crackle of lightning every way"),
        ("Dragon Dance", "SEQ_SE_DP_CLIMAX09_1", "three deep beats with bells and a winding dragon tune"),
        ("Hydro Pump", "SEQ_SE_DP_SUIRYU", "a hammering jet of water and a great splash"),
        ("Muddy Water", "SEQ_SE_DP_W057", "a thick heaving surge of brown water and bubbling mud"),
        ("Thunder Fang", "SEQ_SE_DP_W044", "a snap of teeth and a crackle of lightning"),
        ("Zen Headbutt", "SEQ_SE_DP_293", "a gathering hum, a bell and a heavy ringing butt")
    };

    /// <summary>The moves that have a sound of their own, by name.</summary>
    public static IEnumerable<string> MovesWithOwnSounds => MoveSounds.Select(s => s.Move);

    private static void AddMoveSounds(Action<string, SoundGroup, string?, string> add)
    {
        foreach (var (move, original, what) in MoveSounds)
            add("use_" + MoveKey(move), SoundGroup.Move, original, $"{move}: {what}.");
    }

    /// <summary>A move's own sound, or null for a name that isn't one.</summary>
    private static float[]? MakeMoveSound(string name)
    {
        SoundDesign d;
        switch (name)
        {
            case "use_leer":
                d = new SoundDesign(0.75f);
                foreach (float at in new[] { 0f, 0.36f })
                {
                    d.Bell(at, 0.22f, N(93), 3.01f, 1.1f, 0.35f, 3f);
                    d.Tone(at, 0.3f, Wave.Saw, 98f, 92f, 0.18f, 0.2f, 1.3f);
                }
                return Finish(d.S, 0.38f);
            case "use_pound":
                d = new SoundDesign(0.3f, 0x9017u);
                d.Noise(0, 0.08f, Filter.Band, 1400f, 900f, 0.8f, 1f, 0.05f, 2f);
                d.Thump(0.01f, 0.2f, 190f, 90f, 0.7f, 0.6f);
                return Finish(d.S, 0.42f);
            case "use_scratch":
                d = new SoundDesign(0.3f, 0x5C7Au);
                d.Noise(0, 0.22f, Filter.High, 3200f, 6000f, 0.8f, 1.2f, 0.1f, 1.6f, 40f, 0.6f);
                d.Crackle(0.02f, 0.18f, 8, 4500f, 0.4f, true);
                return Finish(d.S, 0.4f);
            case "use_tackle":
                d = new SoundDesign(0.5f, 0x7AC1u);
                d.Noise(0, 0.32f, Filter.Band, 500f, 1500f, 0.7f, 1f, 0.6f, 1.2f);
                d.Thump(0.32f, 0.18f, 150f, 60f, 0.9f, 0.7f);
                return Finish(d.S, 0.44f);
            case "use_withdraw":
                d = new SoundDesign(0.5f);
                d.Tone(0, 0.3f, Wave.Triangle, 520f, 260f, 0.4f, 0.1f, 1.2f);
                d.Bell(0.3f, 0.2f, 330f, 1.41f, 2f, 0.4f, 3f);
                return Finish(d.S, 0.4f);
            case "use_bide":
                d = new SoundDesign(0.8f, 0xB1DEu);
                d.Tone(0, 0.6f, Wave.Saw, 70f, 82f, 0.35f, 0.5f, 1f, vibratoHz: 6f, vibratoDepth: 0.06f);
                d.Tone(0, 0.6f, Wave.Sine, 140f, 165f, 0.3f, 0.5f, 1f);
                d.Thump(0.6f, 0.2f, 140f, 55f, 0.7f, 0.5f);
                return Finish(d.S, 0.42f);
            case "use_return":
                d = new SoundDesign(0.6f);
                d.Notes(0, 0.07f, 0.25f, Wave.Triangle, Pitches(72, 76, 79, 84), 0.3f);
                d.Thump(0.35f, 0.2f, 200f, 90f, 0.5f, 0.4f);
                return Finish(d.S, 0.38f);
            case "use_quick_attack":
                d = new SoundDesign(0.45f, 0x9A1Cu);
                d.Noise(0, 0.25f, Filter.Band, 900f, 4200f, 0.8f, 1.2f, 0.3f, 1.4f);
                d.Thump(0.28f, 0.14f, 230f, 110f, 0.6f, 0.6f);
                return Finish(d.S, 0.4f);
            case "use_absorb":
                d = new SoundDesign(0.65f, 0xAB50u);
                d.Noise(0, 0.5f, Filter.Band, 600f, 2400f, 0.35f, 1.4f, 0.5f, 1.2f);
                d.Tone(0.05f, 0.45f, Wave.Sine, N(67), N(79), 0.35f, 0.3f, 1.4f);
                d.Bell(0.45f, 0.2f, N(91), 2.01f, 0.9f, 0.3f, 2.5f);
                return Finish(d.S, 0.36f);
            case "use_bubble":
                d = new SoundDesign(0.55f, 0xB0B1u);
                d.Bubbles(0, 0.5f, 12, 500f, 1400f, 0.6f);
                return Finish(d.S, 0.38f);
            case "use_ember":
                d = new SoundDesign(0.45f, 0xE3B3u);
                d.Noise(0, 0.38f, Filter.Low, 900f, 2400f, 0.7f, 1f, 0.1f, 1.4f, 14f, 0.3f);
                d.Crackle(0.03f, 0.35f, 12, 2800f, 0.4f);
                return Finish(d.S, 0.4f);
            case "use_fake_out":
                d = new SoundDesign(0.45f, 0xFA0Eu);
                d.Noise(0, 0.05f, Filter.Band, 2600f, 1800f, 1f, 1.4f, 0.02f, 2.5f);
                d.Bell(0.01f, 0.4f, N(84), 2.76f, 1.6f, 0.35f, 3f);
                return Finish(d.S, 0.42f);
            case "use_fury_swipes":
                d = new SoundDesign(0.4f, 0xF5A1u);
                foreach (float at in new[] { 0f, 0.15f })
                    d.Noise(at, 0.16f, Filter.High, 3000f, 6400f, 0.8f, 1.2f, 0.1f, 1.6f, 44f, 0.6f);
                return Finish(d.S, 0.4f);
            case "use_poison_gas":
                d = new SoundDesign(0.85f, 0x9A55u);
                d.Noise(0, 0.8f, Filter.Band, 2400f, 1600f, 0.5f, 0.8f, 0.3f, 1.1f, 3f, 0.3f);
                d.Bubbles(0.1f, 0.6f, 7, 200f, 420f, 0.35f);
                return Finish(d.S, 0.38f);
            case "use_screech":
                d = new SoundDesign(0.7f);
                d.Tone(0, 0.65f, Wave.Saw, 2100f, 2600f, 0.3f, 0.05f, 1.1f, vibratoHz: 13f, vibratoDepth: 0.04f);
                d.Tone(0, 0.65f, Wave.Pulse, 2230f, 2760f, 0.2f, 0.05f, 1.1f, duty: 0.3f, vibratoHz: 11f, vibratoDepth: 0.05f);
                return Finish(d.S, 0.36f);
            case "use_splash":
                d = new SoundDesign(0.55f, 0x5B1Au);
                d.Thump(0, 0.12f, 220f, 140f, 0.4f, 0.3f);
                d.Noise(0.06f, 0.4f, Filter.Low, 2800f, 900f, 0.8f, 0.9f, 0.05f, 1.6f);
                d.Bubbles(0.1f, 0.35f, 5, 600f, 1200f, 0.3f);
                return Finish(d.S, 0.4f);
            case "use_growth":
                d = new SoundDesign(0.75f);
                d.Tone(0, 0.7f, Wave.Triangle, N(60), N(72), 0.35f, 0.5f, 1.2f);
                d.Tone(0, 0.7f, Wave.Sine, N(67), N(79), 0.25f, 0.5f, 1.2f);
                d.Bell(0.5f, 0.25f, N(91), 1.5f, 1f, 0.2f, 2.5f);
                return Finish(d.S, 0.36f);
            case "use_leech_life":
                d = new SoundDesign(0.65f, 0x1EE7u);
                d.Noise(0, 0.04f, Filter.High, 5000f, 5000f, 0.9f, 1.4f, 0.05f, 2.5f);
                d.Noise(0.12f, 0.45f, Filter.Band, 1800f, 500f, 0.4f, 1.6f, 0.4f, 1.2f);
                d.Tone(0.12f, 0.45f, Wave.Sine, N(79), N(67), 0.3f, 0.3f, 1.3f);
                return Finish(d.S, 0.38f);
            case "use_low_kick":
                d = new SoundDesign(0.4f, 0x10C1u);
                d.Noise(0, 0.2f, Filter.Band, 400f, 1200f, 0.7f, 1f, 0.5f, 1.5f);
                d.Thump(0.2f, 0.18f, 130f, 60f, 0.8f, 0.6f);
                return Finish(d.S, 0.44f);
            case "use_supersonic":
                d = new SoundDesign(0.7f);
                d.Tone(0, 0.68f, Wave.Sine, 3400f, 3000f, 0.4f, 0.1f, 1.2f, vibratoHz: 18f, vibratoDepth: 0.03f);
                d.Tone(0, 0.68f, Wave.Sine, 3460f, 3050f, 0.3f, 0.1f, 1.2f, vibratoHz: 15f, vibratoDepth: 0.03f);
                return Finish(d.S, 0.34f);
            case "use_tail_whip":
                d = new SoundDesign(0.45f, 0x7A11u);
                foreach (float at in new[] { 0f, 0.2f })
                    d.Noise(at, 0.17f, Filter.Band, 600f, 2600f, 0.8f, 1.1f, 0.4f, 1.5f);
                return Finish(d.S, 0.38f);
            case "use_teleport":
                d = new SoundDesign(0.75f);
                d.Tone(0, 0.7f, Wave.Sine, N(76), N(100), 0.35f, 0.2f, 1.1f, vibratoHz: 9f, vibratoDepth: 0.02f);
                d.Tone(0, 0.7f, Wave.Sine, N(83), N(107), 0.2f, 0.2f, 1.1f, vibratoHz: 10f, vibratoDepth: 0.02f);
                return Finish(d.S, 0.34f);
            case "use_water_sport":
                d = new SoundDesign(0.8f, 0x3A5Eu);
                d.Noise(0, 0.25f, Filter.Low, 1400f, 3000f, 0.7f, 0.9f, 0.2f, 1.4f);
                d.Crackle(0.2f, 0.55f, 18, 3000f, 0.4f);
                return Finish(d.S, 0.38f);
            case "use_defense_curl":
                d = new SoundDesign(0.45f);
                d.Tone(0, 0.3f, Wave.Triangle, 300f, 500f, 0.4f, 0.2f, 1.3f);
                d.Tone(0.3f, 0.12f, Wave.Sine, 900f, 1400f, 0.4f, 0.05f, 2f);
                return Finish(d.S, 0.38f);
            case "use_mud_sport":
                d = new SoundDesign(0.75f, 0x3D5Au);
                d.Noise(0, 0.22f, Filter.Low, 500f, 1200f, 0.8f, 1f, 0.2f, 1.4f);
                foreach (float at in new[] { 0.3f, 0.45f, 0.58f }) d.Thump(at, 0.1f, 160f, 110f, 0.4f, 0.8f);
                return Finish(d.S, 0.42f);
            case "use_rock_smash":
                d = new SoundDesign(0.5f, 0x5A0Cu);
                d.Thump(0, 0.15f, 180f, 80f, 0.7f, 0.6f);
                d.Crackle(0.06f, 0.3f, 14, 1600f, 0.6f);
                d.Noise(0.06f, 0.35f, Filter.Low, 1200f, 400f, 0.5f, 1f, 0.05f, 1.5f);
                return Finish(d.S, 0.44f);
            case "use_aerial_ace":
                d = new SoundDesign(0.5f, 0xAE1Au);
                d.Noise(0, 0.25f, Filter.Band, 800f, 3600f, 0.7f, 1f, 0.4f, 1.4f);
                d.Noise(0.27f, 0.18f, Filter.High, 5200f, 3000f, 0.9f, 1.4f, 0.05f, 2f);
                d.Bell(0.27f, 0.2f, N(96), 3.7f, 1.2f, 0.25f, 3f);
                return Finish(d.S, 0.4f);
            case "use_astonish":
                d = new SoundDesign(0.5f);
                d.Tone(0, 0.08f, Wave.Sine, 300f, 1200f, 0.5f, 0.05f, 1.5f);
                d.Tone(0.08f, 0.38f, Wave.Triangle, 1100f, 900f, 0.35f, 0.05f, 1.4f, vibratoHz: 16f, vibratoDepth: 0.05f);
                return Finish(d.S, 0.36f);
            case "use_brick_break":
                d = new SoundDesign(0.55f, 0xB21Cu);
                d.Noise(0, 0.12f, Filter.Band, 600f, 2400f, 0.7f, 1f, 0.5f, 1.4f);
                d.Thump(0.12f, 0.16f, 200f, 90f, 0.7f, 0.8f);
                d.Crackle(0.14f, 0.3f, 16, 2200f, 0.55f);
                return Finish(d.S, 0.44f);
            case "use_counter":
                d = new SoundDesign(0.55f, 0xC0E7u);
                d.Tone(0, 0.2f, Wave.Triangle, 400f, 700f, 0.35f, 0.1f, 1.4f);
                d.Thump(0.3f, 0.2f, 170f, 70f, 0.8f, 0.6f);
                return Finish(d.S, 0.42f);
            case "use_earthquake":
                d = new SoundDesign(0.95f, 0xEA27u);
                d.Noise(0, 0.9f, Filter.Low, 220f, 120f, 1f, 1.1f, 0.1f, 1f, 9f, 0.5f);
                d.Tone(0, 0.9f, Wave.Sine, 48f, 38f, 0.5f, 0.1f, 1f);
                d.Crackle(0.1f, 0.7f, 18, 900f, 0.3f);
                return Finish(d.S, 0.46f);
            case "use_flash":
                d = new SoundDesign(0.6f);
                d.Tone(0, 0.08f, Wave.Sine, 1200f, 2400f, 0.4f, 0.05f, 2f);
                d.Bell(0.04f, 0.5f, N(98), 3.01f, 1.4f, 0.35f, 2.2f);
                d.Bell(0.04f, 0.5f, N(103), 2.01f, 1f, 0.25f, 2.2f);
                return Finish(d.S, 0.36f);
            case "use_focus_punch":
                d = new SoundDesign(0.75f, 0xF0C5u);
                d.Tone(0, 0.4f, Wave.Saw, 90f, 140f, 0.3f, 0.6f, 1f, vibratoHz: 20f, vibratoDepth: 0.05f);
                d.Noise(0.35f, 0.08f, Filter.Band, 600f, 2600f, 0.8f, 1f, 0.5f, 1.4f);
                d.Thump(0.42f, 0.28f, 160f, 45f, 1f, 0.8f);
                return Finish(d.S, 0.46f);
            case "use_force_palm":
                d = new SoundDesign(0.6f, 0xF0A1u);
                d.Thump(0, 0.16f, 220f, 110f, 0.6f, 0.5f);
                d.Tone(0.02f, 0.45f, Wave.Triangle, 600f, 400f, 0.3f, 0.02f, 1.4f, vibratoHz: 30f, vibratoDepth: 0.06f);
                d.Crackle(0.05f, 0.3f, 8, 3800f, 0.3f);
                return Finish(d.S, 0.42f);
            case "use_rock_polish":
                d = new SoundDesign(0.7f, 0x9015u);
                d.Noise(0, 0.5f, Filter.Band, 1600f, 3000f, 0.5f, 1.6f, 0.2f, 1.2f, 22f, 0.6f);
                d.Bell(0.5f, 0.2f, N(100), 3.7f, 1.2f, 0.35f, 3f);
                return Finish(d.S, 0.38f);
            case "use_rock_slide":
                d = new SoundDesign(0.75f, 0x5A1Du);
                d.Noise(0, 0.7f, Filter.Low, 800f, 300f, 0.6f, 1f, 0.05f, 1.2f, 10f, 0.4f);
                foreach (float at in new[] { 0.15f, 0.3f, 0.42f, 0.55f }) d.Thump(at, 0.14f, 140f, 70f, 0.4f, 0.8f);
                return Finish(d.S, 0.44f);
            case "use_take_down":
                d = new SoundDesign(0.6f, 0x7AD0u);
                d.Tone(0, 0.12f, Wave.Sine, 300f, 220f, 0.3f, 0.1f, 1.5f);
                d.Noise(0.15f, 0.22f, Filter.Band, 500f, 1600f, 0.8f, 1f, 0.6f, 1.2f);
                d.Thump(0.37f, 0.22f, 130f, 45f, 1f, 0.8f);
                return Finish(d.S, 0.46f);
            case "use_water_gun":
                d = new SoundDesign(0.45f, 0x3A6Eu);
                d.Noise(0, 0.4f, Filter.Band, 1800f, 3000f, 0.8f, 1.2f, 0.1f, 1.4f, 18f, 0.3f);
                d.Bubbles(0.05f, 0.3f, 4, 700f, 1200f, 0.25f);
                return Finish(d.S, 0.4f);
            case "use_harden":
                d = new SoundDesign(0.6f);
                d.Bell(0, 0.55f, 780f, 1.41f, 3f, 0.45f, 2.2f);
                d.Bell(0.05f, 0.45f, 1760f, 2.76f, 1.5f, 0.2f, 2.6f);
                return Finish(d.S, 0.38f);
            case "use_headbutt":
                d = new SoundDesign(0.5f, 0x4EADu);
                d.Noise(0, 0.25f, Filter.Band, 500f, 1300f, 0.6f, 1f, 0.6f, 1.3f);
                d.Thump(0.3f, 0.18f, 210f, 80f, 0.9f, 0.9f);
                d.Bell(0.3f, 0.15f, 520f, 1.41f, 2f, 0.15f, 3f);
                return Finish(d.S, 0.44f);
            case "use_pursuit":
                d = new SoundDesign(0.5f, 0x9025u);
                d.Tone(0, 0.3f, Wave.Pulse, 110f, 160f, 0.3f, 0.2f, 1.2f, duty: 0.3f);
                d.Noise(0, 0.3f, Filter.Band, 400f, 1800f, 0.5f, 1f, 0.5f, 1.3f);
                d.Thump(0.32f, 0.15f, 180f, 80f, 0.6f, 0.6f);
                return Finish(d.S, 0.42f);
            case "use_rock_throw":
                d = new SoundDesign(0.6f, 0x50C7u);
                d.Noise(0, 0.15f, Filter.Band, 800f, 2000f, 0.5f, 1f, 0.3f, 1.5f);
                foreach (float at in new[] { 0.3f, 0.42f }) d.Thump(at, 0.14f, 160f, 70f, 0.6f, 0.9f);
                d.Crackle(0.3f, 0.2f, 8, 1400f, 0.4f);
                return Finish(d.S, 0.44f);
            case "use_stealth_rock":
                d = new SoundDesign(0.8f, 0x57E1u);
                foreach (float at in new[] { 0.1f, 0.2f, 0.32f, 0.42f, 0.52f })
                {
                    d.Thump(at, 0.1f, 260f, 150f, 0.35f, 0.9f);
                    d.Crackle(at, 0.06f, 3, 2000f, 0.3f);
                }
                d.Bell(0.6f, 0.2f, N(88), 3.7f, 1f, 0.2f, 3f);
                return Finish(d.S, 0.42f);
            case "use_focus_energy":
                d = new SoundDesign(0.75f, 0xF0E4u);
                d.Noise(0, 0.45f, Filter.Band, 400f, 1400f, 0.4f, 1.2f, 0.8f, 1f);
                d.Tone(0.05f, 0.65f, Wave.Saw, 110f, 220f, 0.3f, 0.6f, 1f, vibratoHz: 18f, vibratoDepth: 0.04f);
                d.Bell(0.55f, 0.2f, N(86), 2.01f, 1.2f, 0.25f, 3f);
                return Finish(d.S, 0.4f);
            case "use_secret_power":
                d = new SoundDesign(0.5f, 0x5EC2u);
                d.Noise(0, 0.28f, Filter.Band, 700f, 3000f, 0.5f, 1.4f, 0.5f, 1.3f);
                d.Tone(0, 0.28f, Wave.Sine, N(67), N(74), 0.25f, 0.3f, 1.3f);
                d.Thump(0.3f, 0.16f, 200f, 90f, 0.7f, 0.6f);
                return Finish(d.S, 0.42f);
            case "use_camouflage":
                d = new SoundDesign(0.7f);
                d.Tone(0, 0.65f, Wave.Sine, N(84), N(72), 0.3f, 0.2f, 1.2f, vibratoHz: 7f, vibratoDepth: 0.02f);
                d.Tone(0, 0.65f, Wave.Triangle, N(91), N(79), 0.15f, 0.2f, 1.2f, vibratoHz: 6f, vibratoDepth: 0.02f);
                return Finish(d.S, 0.34f);
        }
        return MakeMoveSoundY7(name);
    }
}
