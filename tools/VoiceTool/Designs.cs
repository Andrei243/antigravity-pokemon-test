using PokemonPlatinumEngine.Data;

/// <summary>
/// The first designs of the voices nobody has cast by hand (plan 26 · V5): an archetype's voices (<c>class:lass#2</c>) and
/// the named characters who are only a role or a first name. A voice gets a <see cref="CastBrief"/> from a table of
/// profiles by the archetype's (or the name's) stem, and a <see cref="CastDesign"/> drawn from pools of Kokoro-82M's own
/// voices by register and age: the blend, the pace and the pitch are chosen by a hash of the voice's id, so the same cast
/// file always gives the same voices, no two voices of the whole cast sound alike, and the voices of one archetype differ.
/// Never a clone of anyone. <c>VoiceTool design</c> writes them into <c>cast.json</c>; a voice with a design already is left
/// as it is, so a voice tuned by ear is never overwritten.
/// </summary>
public static class Designs
{
    /// <summary>A profile: register (<c>mixed</c> alternates man and woman from one voice to the next), age, pitch, pace and manner.</summary>
    private sealed record Profile(string Register, string Age, string Pitch, string Pace, string Manner);

    // stem|register|age|pitch|pace|manner. A stem is an archetype without its class:/look: prefix, or a named voice's id.
    private static readonly string[] Table =
    {
        // Trainer classes and looks
        "aroma_lady|woman|middle-aged|middle|slow|an Aroma Lady: gentle, dreamy, fond of scents",
        "artist|mixed|young|middle|even|an artist: wistful and a little theatrical",
        "battle_girl|girl|young|middle|quick|a Battle Girl: sporty and determined",
        "beauty|woman|young|high|even|a Beauty: poised and a touch vain",
        "belle_&_pa|mixed|middle-aged|middle|even|a doting father or his sweet daughter",
        "bird_keeper|mixed|young|middle|even|a Bird Keeper: outdoorsy and fond of birds",
        "black_belt|man|adult|low|slow|a Black Belt: gruff, disciplined, few words",
        "bug_catcher|boy|child|middle|quick|a Bug Catcher: bug-mad and excitable",
        "cameraman|man|adult|middle|even|a television cameraman: brisk and practical",
        "camper|boy|teen|middle|even|a Camper: cheerful and outdoorsy",
        "collector|man|middle-aged|middle|even|a Collector: fussy and proud of his things",
        "cowgirl|woman|young|middle|quick|a Cowgirl: easygoing country warmth",
        "cyclist|mixed|young|middle|quick|a Cyclist: breathless and sporty",
        "double_team|mixed|young|middle|quick|one of a pair who finish each other's sentences",
        "dragon_tamer|man|adult|low|slow|a Dragon Tamer: stern and proud",
        "fisherman|man|middle-aged|low|slow|a Fisherman: weathered and patient",
        "galactic|mixed|young|middle|even|a Team Galactic grunt: flat, drilled menace",
        "gentleman|man|elder|low|slow|a Gentleman: courtly and measured",
        "guitarist|man|young|middle|even|a Guitarist: laid-back and easy",
        "hiker|man|middle-aged|low|even|a Hiker: hearty and loud",
        "jogger|mixed|young|middle|quick|a Jogger: out of breath and upbeat",
        "lady|woman|middle-aged|middle|even|a Lady: refined and warm",
        "lass|girl|teen|high|quick|a Lass: bright and chatty",
        "ninja_boy|boy|teen|middle|quick|a Ninja Boy: dramatic and secretive",
        "parasol_lady|woman|young|high|even|a Parasol Lady: genteel and sunny",
        "pi|man|middle-aged|middle|even|a detective: dry and watchful",
        "picnicker|girl|teen|high|quick|a Picnicker: cheery and talkative",
        "poké_kid|girl|child|high|quick|a Poké Kid: sweet and curious",
        "pokéfan|mixed|middle-aged|middle|even|a Poké Fan: gushing about their Pokémon",
        "pokémon_breeder|mixed|adult|middle|slow|a Breeder: kind and patient",
        "pokémon_ranger|mixed|young|middle|even|a Ranger: upright and friendly",
        "policeman|man|adult|low|even|a Policeman: clipped and official",
        "psychic|mixed|adult|middle|slow|a Psychic: serene and faraway",
        "rancher|man|adult|low|slow|a Rancher: slow and country-warm",
        "rich_boy|boy|teen|middle|even|a Rich Boy: spoiled and superior",
        "roughneck|man|young|low|quick|a Roughneck: swaggering and loud",
        "ruin_maniac|man|middle-aged|low|even|a Ruin Maniac: thrilled by old things",
        "sailor|man|middle-aged|low|even|a Sailor: hearty and salt-weathered",
        "school_kid|mixed|teen|middle|quick|a School Kid: studious chatter",
        "scientist|mixed|adult|middle|even|a Scientist: precise and distracted",
        "skier|mixed|young|middle|quick|a Skier: bracing and breezy",
        "socialite|woman|middle-aged|high|even|a Socialite: lofty and gracious",
        "swimmer|mixed|young|middle|quick|a Swimmer: sunny and sporty",
        "tuber|mixed|child|middle|quick|a Tuber: splashy and giggly",
        "twins|girl|child|high|quick|twins: bubbly, talking over each other",
        "veteran|man|elder|low|slow|a Veteran: gravelly and firm",
        "waitress|woman|young|middle|even|a Waitress: polite and quick",
        "worker|man|adult|low|even|a Worker: practical and plain",
        "young_couple|mixed|young|middle|even|a young couple: warm and a little shy",
        "youngster|boy|teen|middle|quick|a Youngster: cocky and eager",
        "ace_trainer|mixed|young|middle|quick|an Ace Trainer: confident and keen",
        "ace_trainer_f|woman|young|middle|quick|a woman Ace Trainer: confident and keen",
        "ace_trainer_m|man|young|middle|quick|a man Ace Trainer: confident and keen",
        "announcer|woman|adult|middle|even|an announcer: bright and projecting",
        "attendant|mixed|adult|middle|even|an attendant: courteous and helpful",
        "bollard|man|middle-aged|low|slow|a barrier's guard: flat and unmoved",
        "boy|boy|child|middle|quick|a boy: eager and a little loud",
        "butler|man|elder|low|slow|a butler: impeccable and dry",
        "cashier_m|man|young|middle|even|a cashier: patient and bored",
        "clerk|mixed|adult|middle|even|a shop clerk: friendly and practised",
        "clown|mixed|adult|high|quick|a Clown: manic and jokey",
        "expert_m|man|elder|low|slow|an expert: scholarly and unhurried",
        "grunt|mixed|young|middle|even|a Team Galactic grunt: flat, drilled menace",
        "grunt_f|woman|young|middle|even|a Team Galactic grunt: cold and clipped",
        "grunt_m|man|young|low|even|a Team Galactic grunt: gruff and clipped",
        "guard|man|adult|low|even|a guard: firm and watchful",
        "gym_guide|mixed|young|middle|quick|a Gym guide: encouraging and proud",
        "host|man|adult|middle|quick|a television host: show-voiced and bright",
        "idol|girl|young|high|quick|an idol: sparkly and breathless",
        "kid_with_nds|boy|child|middle|quick|a kid with a game console: absorbed and loud",
        "little_girl|girl|child|high|quick|a little girl: shy and sweet",
        "man|man|adult|middle|even|a man of the town: plain and friendly",
        "nurse|woman|young|middle|even|a Nurse: warm and reassuring",
        "old_man|man|elder|low|slow|an old man: slow and wry",
        "old_woman|woman|elder|middle|slow|an old woman: kindly and slow",
        "person|mixed|adult|middle|even|a person of the town: plain and friendly",
        "pilot|man|adult|middle|even|a Pilot: calm and clipped",
        "pokefan_f|woman|middle-aged|middle|even|a Poké Fan: gushing about her Pokémon",
        "pokefan_m|man|middle-aged|middle|even|a Poké Fan: gushing about his Pokémon",
        "pokemon_breeder_f|woman|adult|middle|slow|a Breeder: kind and patient",
        "pokemon_breeder_m|man|adult|middle|slow|a Breeder: kind and patient",
        "receptionist|woman|young|middle|even|a receptionist: bright and professional",
        "scientist_m|man|adult|middle|even|a Scientist: precise and distracted",
        "snowpoint_npc_f|woman|middle-aged|middle|slow|a Snowpoint local: soft and wintry",
        "twin|girl|child|high|quick|a twin: bubbly, a step ahead of her sister",
        "woman|woman|adult|middle|even|a woman of the town: plain and friendly",
        // Named characters who are a role or a first name
        "abigail|woman|young|middle|even|Abigail: bright and sure of herself",
        "arthur|man|middle-aged|middle|even|Arthur: friendly and a little pompous",
        "assistant_2|woman|young|middle|quick|a second assistant: earnest and busy",
        "berry_master|man|elder|low|slow|the Berry Master: warm, slow and sure about plants",
        "catherine|woman|adult|middle|even|Catherine: capable and kind",
        "chairman|man|elder|low|slow|a chairman: weighty and deliberate",
        "clefairy|girl|child|high|quick|a Clefairy's squeaky words",
        "dalton|man|adult|middle|even|Dalton: steady and plain-spoken",
        "derek|man|young|middle|even|Derek: easy-going and cheerful",
        "dylan|boy|teen|middle|quick|Dylan: quick and sporty",
        "edwin|man|adult|low|even|Edwin: reserved and careful",
        "emma|woman|young|middle|even|Emma: friendly and lively",
        "engineer|man|adult|middle|even|an engineer: dry and exact",
        "ernest|man|middle-aged|low|slow|Ernest: formal and gruff",
        "fan|woman|young|high|quick|a fan: starry-eyed and gushing",
        "farmer|man|middle-aged|low|slow|a farmer: slow and warm",
        "florist|woman|adult|middle|slow|a florist: gentle and fragrant",
        "grant|man|adult|middle|even|Grant: calm and thoughtful",
        "isaiah|man|young|middle|even|Isaiah: earnest and soft-spoken",
        "jackson|boy|teen|middle|quick|Jackson: loud and keen",
        "jeffrey|man|adult|low|even|Jeffrey: dry and sensible",
        "jerry|man|middle-aged|middle|even|Jerry: chatty and kind",
        "jessica|woman|young|middle|quick|Jessica: bright and brisk",
        "juan|man|adult|low|slow|Juan: elegant, formal and unhurried",
        "katelyn|woman|young|middle|even|Katelyn: cheerful and curious",
        "kurt|man|elder|low|slow|Kurt: crusty, proud of his craft",
        "lift_attendant|woman|young|middle|even|a lift attendant: smooth and polite",
        "liz|woman|young|middle|quick|Liz: lively and sharp",
        "lydia|woman|adult|middle|even|Lydia: warm and level",
        "maid|woman|young|middle|even|a maid: crisp, courteous and a little stern",
        "maria|woman|adult|middle|even|Maria: warm and motherly",
        "mary|woman|adult|middle|slow|Mary: soft and kind",
        "matt|man|young|middle|quick|Matt: casual and quick",
        "miguel|man|adult|middle|even|Miguel: friendly and sincere",
        "mother|woman|middle-aged|middle|even|a mother: warm and gently fussing",
        "move_deleter|man|elder|low|slow|the Move Deleter: slow and a little forgetful",
        "mystery|man|adult|low|slow|a hidden speaker: low and strange",
        "neighbour|woman|middle-aged|middle|even|a neighbour: chatty and kind",
        "olivia|woman|young|middle|even|Olivia: sweet and bubbly",
        "pikachu|girl|child|high|quick|a Pikachu's squeaky words",
        "quinn|woman|adult|middle|even|Quinn: cool and brisk",
        "ricky|boy|teen|middle|quick|Ricky: cheeky and quick",
        "robert|man|middle-aged|low|even|Robert: stolid and plain-spoken",
        "sabrina|woman|young|low|slow|Sabrina: cool, unhurried and a little eerie",
        "scott|man|adult|middle|quick|Scott: friendly, bright and quick",
        "sign|man|adult|low|slow|a sign's voice: flat and plain",
        "tristan|man|young|middle|quick|Tristan: eager and polite",
        "ty|man|young|middle|even|Ty: easy and cheerful",
        "valerie|woman|adult|middle|even|Valerie: light and refined",
        "wade|man|young|middle|even|Wade: relaxed and pleasant",
        "waiter|man|young|middle|even|a waiter: smooth and polite",
        "walter|man|middle-aged|low|slow|Walter: slow and kind",
        "wesley|man|adult|middle|even|Wesley: calm and clear",
        "zac|boy|teen|middle|quick|Zac: cocky and quick",
        "captain|man|middle-aged|low|slow|a ship's captain: weathered and firm",
        "training_tutor|mixed|adult|middle|even|a training tutor: patient, encouraging and exact",
        "club_clerk|woman|young|middle|even|a club clerk: bright and efficient",
        "day_care_lady|woman|elder|middle|slow|the Day-Care Lady: kindly, slow and fond of Pokémon",
        "day_care_man|man|elder|low|slow|the Day-Care Man: gruff and fond of Pokémon",
        "head_coach|man|adult|low|quick|a head coach: booming and motivating",
        // Kanto (plan 26, Kanto's session)
        "team_rocket|mixed|adult|low|even|a Team Rocket grunt: gruff, smug and threatening",
        "rocket|mixed|young|middle|even|a Team Rocket grunt: sneering and clipped",
        "pkmn_ranger|mixed|young|middle|even|a Ranger: upright and friendly",
        "pokemaniac|man|middle-aged|low|slow|a Pokémaniac: obsessed, wheezy and fond of rare Pokémon",
        "poke_maniac|man|middle-aged|low|slow|a Pokémaniac: obsessed, wheezy and fond of rare Pokémon",
        "rocker|man|young|middle|quick|a Rocker: loud, laid-back and full of himself",
        "sis_and_bro|mixed|child|high|quick|a sister and a brother: bickering and bright",
        "super_nerd|man|teen|middle|quick|a Super Nerd: nasal, earnest and a know-it-all",
        "swimmer_f|woman|young|middle|quick|a Swimmer: sunny, splashy and sporty",
        "swimmer_m|man|young|middle|quick|a Swimmer: sunny, splashy and sporty",
        "tamer|man|adult|low|even|a Tamer: commanding and proud of his beasts",
        "aide|mixed|adult|middle|even|one of the Professor's aides: helpful and busy",
        "biker|man|young|low|quick|a Biker: rowdy, sneering and loud",
        "channeler|woman|elder|middle|slow|a Channeler: trance-like and wavering",
        "cooltrainer|mixed|young|middle|quick|a Cooltrainer: confident and keen",
        "cooltrainer_f|woman|young|middle|quick|a Cooltrainer: confident and keen",
        "cooltrainer_m|man|young|middle|quick|a Cooltrainer: confident and keen",
        "girl|girl|child|high|quick|a girl: chatty and bright",
        "trainer|mixed|young|middle|even|a trainer: keen and friendly",
        "pokedex|woman|adult|middle|even|a device's plain description",
        "townmap|woman|adult|middle|even|a map's plain description",
        "domefossil|man|adult|low|slow|a pedestal's plain description",
        "helixfossil|man|adult|low|slow|a pedestal's plain description",
        "oldamber|man|adult|low|slow|a pedestal's plain description",
        "clifton|man|adult|middle|even|Clifton: courteous and fond of a good trade",
        "garett|man|young|middle|even|Garett: friendly and fond of a trade",
        "norma|woman|middle-aged|middle|even|Norma: motherly and fond of a trade",
        "ramon|man|adult|middle|quick|Ramon: brisk and plain-spoken",
        "prospector|man|middle-aged|low|slow|a Prospector: dusty, slow and hopeful",
        "dusty|man|adult|middle|quick|Dusty: sly and sure he will win",
        "fishing_guru|man|elder|low|slow|the Fishing Guru: rambling and proud of his rod",
        "machop|man|adult|low|even|a Machop's grunting words",
        "slowbro|man|adult|low|slow|a Slowbro's sluggish words",
        "ghost|man|adult|low|slow|a ghost's hollow voice",
        "avery|man|middle-aged|low|even|Avery: stern and plain-spoken",
        "big_man|man|adult|low|even|a big man: hearty and proud of his strength",
        "chef|man|middle-aged|middle|quick|a chef: harried, loud and proud of his kitchen",
        "burglar|man|adult|middle|quick|a Burglar: sly and jumpy",
        "cool_couple|mixed|young|middle|even|a cool couple: confident and in step",
        "crush_girl|woman|young|middle|quick|a Crush Girl: fierce and sporty",
        "crush_kin|mixed|young|middle|quick|a Crush Kin: fierce and in tune with her sister",
        "cue_ball|man|young|low|quick|a Cue Ball: tough, swaggering and loud",
        "gamer|man|teen|middle|quick|a Gamer: absorbed, boastful and nasal",
        "juggler|man|adult|middle|quick|a Juggler: showy and a little smug",
        "painter|woman|young|middle|slow|a Painter: dreamy and absorbed",
        "pkmn_breeder|mixed|adult|middle|slow|a Breeder: kind and patient",
    };

    private static readonly Dictionary<string, Profile> Profiles = Table.Select(r => r.Split('|'))
        .ToDictionary(p => p[0], p => new Profile(p[1], p[2], p[3], p[4], p[5]), StringComparer.Ordinal);

    // Pools of Kokoro-82M's own voices by register and age (a boy's and a girl's are a man's and a woman's raised in pitch)
    private static readonly Dictionary<string, string[]> Pools = new()
    {
        ["girl"] = new[] { "af_nova", "af_sky", "af_jessica", "af_bella", "af_sarah", "bf_lily", "af_nicole", "af_aoede" },
        ["boy"] = new[] { "am_puck", "am_liam", "am_eric", "am_echo", "am_michael", "bm_lewis" },
        ["woman-young"] = new[] { "af_nova", "af_jessica", "af_aoede", "af_bella", "af_sarah", "af_river", "bf_lily", "bf_alice", "af_alloy" },
        ["woman-adult"] = new[] { "af_kore", "af_heart", "af_river", "af_alloy", "af_sarah", "bf_emma", "bf_alice", "af_nicole" },
        ["woman-old"] = new[] { "af_kore", "bf_isabella", "bf_emma", "af_heart", "af_river", "af_nicole" },
        ["man-young"] = new[] { "am_liam", "am_puck", "am_michael", "am_echo", "am_eric", "bm_lewis", "bm_daniel" },
        ["man-adult"] = new[] { "am_adam", "am_michael", "am_eric", "am_echo", "am_fenrir", "bm_daniel", "bm_lewis" },
        ["man-old"] = new[] { "am_adam", "am_onyx", "am_santa", "bm_george", "bm_fable", "am_fenrir", "am_echo" },
    };

    /// <summary>The stem a voice id is looked up by: <c>class:lass#2</c> is <c>lass</c>, <c>abigail</c> is itself.</summary>
    public static string StemOf(string voice)
    {
        int hash = voice.IndexOf('#');
        string id = hash < 0 ? voice : voice[..hash];
        int colon = id.IndexOf(':');
        return colon < 0 ? id : id[(colon + 1)..];
    }

    /// <summary>The number of an archetype voice (<c>class:lass#2</c> is 2), 1 for a named voice.</summary>
    private static int NumberOf(string voice) => voice.IndexOf('#') is > 0 and var h && int.TryParse(voice[(h + 1)..], out int n) ? n : 1;

    public static bool HasProfile(string voice) => Profiles.ContainsKey(StemOf(voice));

    /// <summary>
    /// A brief and a design for <paramref name="voice"/>, or null where no profile is known for its stem (the caller then
    /// uses a plain one). <paramref name="taken"/> holds the (blend, speed, pitch) of every voice designed so far, and is added to.
    /// </summary>
    public static (CastBrief Brief, CastDesign Design, bool Generic) Make(string voice, ISet<(string, double, double)> taken)
    {
        string stem = StemOf(voice);
        bool generic = !Profiles.TryGetValue(stem, out var profile);
        profile ??= Profiles["person"];
        int n = NumberOf(voice);
        uint seed = VoiceCast.Hash32("design:" + voice);

        // 'mixed' alternates man and woman from one voice of an archetype to the next, starting where the stem's hash says
        string register = profile.Register;
        if (register == "mixed") register = ((VoiceCast.Hash32("start:" + stem) & 1) == 0) == (n % 2 == 1) ? "man" : "woman";
        string age = profile.Age;
        // A mixed archetype's voices are not all one age: the later ones are a step older
        if (profile.Register == "mixed" && n >= 3 && age is "young" or "adult") age = age == "young" ? "adult" : "middle-aged";

        string poolKey = register switch
        {
            "girl" => "girl",
            "boy" => "boy",
            "woman" => age is "child" or "teen" or "young" ? "woman-young" : age == "adult" ? "woman-adult" : "woman-old",
            _ => age is "child" or "teen" or "young" ? "man-young" : age == "adult" ? "man-adult" : "man-old"
        };
        if (age == "child" && register is "man") poolKey = "boy";
        if (age == "child" && register is "woman") poolKey = "girl";
        var pool = Pools[poolKey];

        double basePitch = age switch { "child" => register is "boy" or "man" ? 1.18 : 1.12, "teen" => register is "boy" or "man" ? 1.12 : 1.07, "young" => 1.04, "adult" => 1.0, "middle-aged" => 0.97, _ => 0.93 };
        basePitch += profile.Pitch switch { "high" => 0.03, "low" => -0.03, _ => 0 };
        double baseSpeed = profile.Pace switch { "quick" => 1.08, "slow" => 0.92, _ => 1.0 };
        if (age == "elder") baseSpeed -= 0.03;
        if (age == "child") baseSpeed += 0.02;

        for (int attempt = 0; ; attempt++)
        {
            uint h = VoiceCast.Hash32($"{seed}:{attempt}");
            int i = (int)(h % (uint)pool.Length), j = (int)((h / 7) % (uint)(pool.Length - 1));
            if (j >= i) j++;
            double weight = 0.5 + 0.1 * ((h / 53) % 3);
            double speed = Math.Round(baseSpeed + ((int)((h / 101) % 7) - 3) * 0.01, 2);
            double pitch = Math.Round(basePitch + ((int)((h / 701) % 7) - 3) * 0.008, 3);
            pitch = Math.Clamp(pitch, 0.86, 1.24);
            speed = Math.Clamp(speed, 0.75, 1.35);
            string blend = $"{pool[i]}:{weight:0.0},{pool[j]}:{1 - weight:0.0}";
            if (!taken.Add((blend, speed, pitch))) continue;
            var brief = new CastBrief
            {
                Register = register, Age = age, Pitch = profile.Pitch, Pace = profile.Pace,
                Manner = profile.Manner
            };
            var design = new CastDesign
            {
                Engine = CastDesign.Kokoro, Blend = blend, Speed = speed, Pitch = pitch,
                Accent = pool[i].StartsWith("b", StringComparison.Ordinal) && pool[j].StartsWith("b", StringComparison.Ordinal) ? "en-gb" : "en-us"
            };
            return (brief, design, generic);
        }
    }
}
