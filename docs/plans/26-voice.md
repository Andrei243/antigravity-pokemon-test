# Plan 26 · Voice: every character's lines spoken, in designed voices

Written 2026-10-07, before any session, at the user's request: "make all the characters' text also have audio
dubbing". The plan first gave the rival, Barry, the user's own voice, cloned from samples the user would record.

**The user's ruling of 2026-11-07** (`docs/plans/decisions/USER-DECISIONS-2026-11-07.md`): **Kokoro-82M** (Apache-2.0,
CPU) is the engine, and **the rival gets a designed voice like everyone else: no cloning, no recordings, no
whisper.cpp**. Everything below that spoke of cloning Barry, of the user's samples or of the cloning trial is rewritten
to that; the sample kit V3 built (the script, the booth, `check-samples`) stays in the repository, unused, in case the
user ever changes their mind.

**Goal**: every line a character says in a text box is also heard, in a voice that is that character's own. The player
stays silent, as in Platinum. Everyone is cast from a designed voice the user approves by ear: the named cast one voice
each (Barry among them), everyone else from a set of voices by kind of person, so two lasses in one town don't sound
alike. Turning voices off gives back today's game exactly. Nothing is taken from the games or the anime, and no real
person's voice is imitated or cloned: every voice is a blend of the engine's own built-in voices.

## Where we are

- **The text.** Lines come from three kinds of file (plan 17 has the full inventory):
  - **Scripts** (`Data/scripts/*.txt`, 56 files today). About 410 strings are said or asked, roughly 5,100 words: 346 `say`, 26 `text`, 22 `ask`, 2 `choose`, 10 `sayown` and 5 `trainerline`.
  - **Overlays and hand-made map files** (145 overlays). About 880 strings, roughly 12,400 words:
    - 380 townsfolk lines (`dialog`);
    - 245 trainer lines before a battle and 245 after (`dialogueBefore`, `dialogueAfter`);
    - 13 signs.
  - **The introduction** (`IntroScreen`: Professor Rowan's welcome).

  In all, about 1,300 lines and 17,500 words. Spoken at an easy pace that is about **two hours of audio**, for the part of Sinnoh open so far.
- **Who speaks.**
  - A script sets the speaker by display name, and lines after it are theirs: `speaker "Prof. Rowan"`, `speaker "{rival}"`, `speaker "{assistant}"`. `speaker none` gives the game's own voice; `self` is the person spoken to.
  - The most frequent speakers today: the rival (23 switches), Rowan (17), the assistant (15), Galactic grunts (14), the player's mother, the rival's mother, Cheryl, Looker, Mars and Roark.
  - People of the world carry an id (`NPC.Key`), an `npcType` and, for trainers, one of 70 trainer classes (Platinum's 927 trainers, `Data/trainers.json`).
- **Names the player chooses.** `{player}` is whatever the player typed. `{rival}` is the name the introduction asks for the rival (plan 02 · S4), and `{assistant}` is the character the player isn't, always under the default name: Lucas or Dawn (`PlayerIdentity.DefaultName`). Other placeholders are filled as a line shows:
  - `{starter}` and `{rivalstarter}`: one of three species each;
  - `{lead}`: the team's first Pokémon;
  - `{item}`, `{money}`, `{result}` and `{var:...}`: anything at all.
- **Where a line reaches the screen.**
  - `ScriptRunner` batches a talk's lines and calls `IScriptHost.Say(speaker, lines)`.
  - `GameEngine.FieldHost` hands them to `DialogueManager.ShowDialogue`, which types them out at the options' speed and moves on when the player presses A.
  - `HeadlessScriptHost` writes them into its `Transcript`.
- **The audio.**
  - `AudioMixer` has five buses: music, fanfares, effects, cries and ambience. It dips the music under a cry.
  - Every sound the game makes is synthesised in code, and the repository holds no audio file; with no samples recorded (the ruling of 2026-11-07) it still holds none.
  - Platinum itself has no voice acting: this plan adds to the original rather than following it, so it is an option (decision 4).
- **Neighbours.**
  - Plan 17 · N4 numbers every line of a script (`Instruction.TextIndex`) and finds a person's lines by place and `NPC.Key`. That address is exactly what a line's audio needs, and this plan uses it rather than inventing a second one.
  - Plan 17's rule that an entry whose English changed is stale is the rule a line's audio needs too.
  - Plan 09 · L3 puts markup in lines (`{pause 0.4}`, colours).
  - Plan 08 · P2 brings hold-to-skip and auto-advance.
  - Plan 11 · C12 has leaders and rivals speak during their battles, and C5 and C6 give the named cast their faces.
  - Plan 12 · Q9's captions name sounds.
  - Plan 16 · T2 makes release builds.

## Barry: a designed voice

**Decision (2026-10-07, the user's):** the player character has no lines, as in Platinum, and gets none, not even
short calls in battle. **The rival, Barry**, is the most talkative male character, in nearly every chapter from the
first scene to the Pokémon League and after. The user first meant to give him their own voice; **since 2026-11-07 he
has a designed voice** (a `brief` and a Kokoro `design` in `cast.json`, like everyone else): a teenage boy, quick,
loud and warm-hearted.

- **How much he says.** Today, 56 lines (about 780 words) from Twinleaf Town to Oreburgh City, spread over ten script files. They include his bedroom, Route 201's lake trip, Sandgem, the Trainers' School, Route 203's battle and Oreburgh. Across Platinum's whole story, in this game's own words, perhaps 400 to 600 lines. On top come:
  - his trainer lines before and after each rival battle;
  - plan 11 · C12's lines in battle (his last Pokémon, his first to fall, his last in the red).
- **His name.** The introduction lets the player rename him (`{rival}`). Other people's lines that say his name never speak it, unless the player kept "Barry": then a second version that says it can play. His own lines carry `{player}` often ("...Oh, it's you, {player}!"); the spoken version drops it.
- **His starter.** Lines that name his Pokémon (`{rivalstarter}`) are generated once for each of the three.
- **The boy and the girl** are cast like everyone else: the assistant (the character the player isn't, always under the default name, Lucas or Dawn) gets a synthesised voice of their own.

## Design

### Lines and their keys

- **One key per line**, plan 17's:
  - a script line is `script:<file>.<script>#<TextIndex>.<k>` (the k-th string of the instruction);
  - a person's line is `world:<area>.<NPC.Key>.dialog.<k>`, `...before.<k>` or `...after.<k>`;
  - a hand-made map's person is `map:<Name>.<NPC.Key>...`;
  - the introduction's is `intro:<phase>.<k>`;

  If plan 17 · N4 hasn't run when V1 starts, V1 builds the numbering and the address exactly as N4 describes them, and N4 then only adds the sibling files.
- **The catalogue** is `Data/voice/lines.json`, written by a new `tools/VoiceTool catalogue`. It holds one entry per line:
  - its key;
  - the voice that speaks it (below);
  - the English as shown, and a hash of it;
  - the **spoken text** (below);
  - its variants;
  - a direction for the performance (`calm`, `excited`, `shouting`, `whispering`, `sad`, `smug`), plus free notes.

  Directions are written by hand and kept across runs; everything else is regenerated. A line whose English changes keeps its key, its hash no longer matches, and its audio turns **stale**: it isn't played (the text shows alone) until it is redone. The tool lists new, changed and orphaned lines, as plan 17's report does for translations.
- **What is spoken can differ from what is shown.** A free placeholder is never spoken:
  - `{player}` and `{rival}` are typed by the player;
  - `{item}`, `{money}`, `{result}` and the variables can be anything.

  A line with one carries a spoken text that leaves it out ("Hey, {player}! Over here!" is shown, "Hey! Over here!" is spoken). The tool fills a first version by dropping the name and the comma round it and asks for the rest by hand. A line where the name can't be dropped gracefully can be marked shown-only, and the text then shows alone.
- **Variants.**
  - A placeholder with few values is voiced once per value: `{starter}` and `{rivalstarter}` three times, `{assistant}` once each for Lucas and Dawn (both synthesised).
  - Where the player kept a default name (Lucas, Dawn, Barry), a line may have a second take that says the name, played only then.

  The key of a variant is the line's key with `~starter=Piplup` or `~named` after it.
- **The cast** is `Data/voice/cast.json`. It maps whoever speaks to a voice:
  - **named characters** by an id: `rowan`, `rival`, `assistant_boy`, `assistant_girl`, `mom`, `rival_mom`, `looker`, `cyrus`, each gym leader and member of the Elite Four;
  - **archetypes**: by trainer class (Youngster, Lass, Hiker…) and by `npcType` (Nurse, Clerk, the townsfolk's ages and kinds), each with two to four voices. A person is given one of them by a hash of their place and key, the same on every run, so a town's people sound different and the same person always sounds the same.

  A script's `speaker` gains an optional id (`speaker rowan "Prof. Rowan"`, or a cast entry found by display name), so the display name stays free to change. `speaker none` lines (the game's own voice, signs, "Received the Potion!") are not voiced (decision 7).
- **Battle text** ("Turtwig used Tackle!") is the game's own voice and is not voiced. A trainer's lines are: before and after a battle (already in the overlays), and during it once plan 11 · C12 brings them.

### Where the voices come from

- **Designed voices for everyone, Barry included** (the user's ruling of 2026-11-07), made offline by **Kokoro-82M**
  (Apache-2.0, 82 million parameters, fast on a CPU, 28 built-in English voices) and baked into files, never generated
  while the game runs.
  - **Why offline:** a neural model in the game would add hundreds of megabytes and a second or more of delay per line, and would sound different on every machine.
  - **A designed voice** is a `design` in `cast.json`: a weighted blend of Kokoro's built-in voices (`am_puck:0.6,am_michael:0.4`), a speed, a pitch (the take made slower and played faster, which raises pitch and formants together: a younger sound) and an accent (en-us, or en-gb for the British voices). It is tuned to the voice's `brief` (register, age, pitch, pace, manner). No anime or game performer is imitated, and no recording of anyone is used.
  - **New lines, rewritten lines and new chapters** are generated the moment their text is final.
  - The other candidates of the first draft (Piper, Chatterbox; and for cloning Barry, Chatterbox, F5-TTS and OpenVoice V2) were not tried: the user chose Kokoro without a trial.
- **A pronunciation list**, `Data/voice/lexicon.json`. Species, places, moves and people's names, with how to say each (a respelling the engine's script turns into phonemes syllable by syllable), so "Pokémon", "Piplup", "Sandgem" and "Cyrus" are said the same way by every voice.

### Making the files

- **`tools/VoiceTool export <voice>`** writes a voice's script: every line in story order, with its scene, the line before it and who said it, its direction, its variants, its pronunciations and its key. It is for reading through, and for a human actor if one ever voices a part.
- **The sample booth**, `tools/VoiceBooth/booth.html`, built in V3 for the user's recordings: kept, unused since the ruling of 2026-11-07 (no recordings). It shows the sample script one passage at a time, records at 48 kHz and writes each chosen take as a WAV.
- **`tools/VoiceTool bake`** turns the engine's output into the game's files, all in .NET with no Python:
  - cleaning: trims the silence at each end, high-passes at 80 Hz, softens plosives, and brings each line to the voice bus's loudness window (`Audio/Loudness`, a new `AudioBus.Voice` entry) with a gentle limiter;
  - format: resamples to the mixer's 32,768 Hz, mono, and encodes it as Ogg Vorbis;
  - checks, flagging any failure in a report: clipping, a noise floor too high, and a length that doesn't fit the text (a line spoken in a third of the time its words need has gone wrong).

  A speech-to-text pass (whisper.cpp) was planned to compare what was said with the spoken text; the user ruled it out on 2026-11-07. Kokoro reads the phonemes it is given, so it neither skips nor repeats words as a cloning engine can.
- **`tools/VoiceTool generate <voice>... [--region <id>]`** runs the engine over the voices' lines (with `--region`, only those of the region's places), applying the cast entry's design and the direction (a share of the speed and the pitch: Kokoro has no emotion control), then bakes the output.
  - Each line is generated three times, each under a seed of its own (as written, 4% slower, 4% faster), and the take whose bake has the fewest findings and the easiest pace is kept. Its seed and the engine's version are written into the pack's `pack.json`, so the same line comes out the same when it is made again.
  - `VoiceTool reroll <key>` makes new takes of one line the user didn't like.
- **Where the engine runs.**
  - Kokoro is run from Python (onnxruntime, eSpeak NG for the phonemes), and the game never depends on Python. `tools/VoiceGen/setup-kokoro.sh` makes the engine's own environment in `VOICEGEN_HOME` (`~/.cache/pokemon-voicegen`) from a pinned requirements file and fetches the model by checksum; `VoiceTool generate` runs `tools/VoiceGen/kokoro_speak.py` once for a whole run and hands it a take at a time.
  - **Time:** on the Windows machine's CPU about 1.5 s a take, 4.6 s a line with its three takes; Sinnoh's named cast (some 1,700 lines) is about an hour and a half with three runs side by side.
- **The coverage report**, `docs/voice/coverage.md`, written by the tool: per voice and per chapter, how many lines are generated, stale, missing or shown-only. A test holds it to what the files say, as `CoverageTests` does for moves.

### Where the files live (decision 5)

- **Size:** two hours of speech today, perhaps seven for the whole of Sinnoh's story and people, and several times that once the other regions of the chain have theirs. As Ogg Vorbis at about 48 kbit/s mono, that is about 20 MB an hour: some 40 MB today, 150 MB for Sinnoh.
- **No samples are in git**: the first draft committed the user's recordings for cloning; since the ruling of 2026-11-07 nothing is recorded. Only the passages' script (`samples.json`) is kept, unused.
- **The generated lines: a voice pack outside git.**
  - A folder `voice/<language>/` beside the game, ignored by git like `overrides/`: `<voice>/<key>.ogg` (`.wav` for now), and a `pack.json` listing what is in it, with each file's hash, the text hash it was made from, and for a generated line its engine and seed.
  - Anyone with the repository and the engine can make the pack again from the cast's designs and the catalogue.
  - Release builds (plan 16 · T2) zip it as a separate download.

  This keeps the repository light: tens to hundreds of megabytes of lines, regenerated whenever a line changes, would bloat git's history for good.
- **Alternatives for the pack:** Git LFS in this repository; or committing the generated lines.

### In the game

- **A sixth bus.** `AudioBus.Voice`, with its own volume in the options, carries one voice at a time. A new line cuts the one before it with a 30 ms fade.
  - While a voice speaks, the music dips (0.5 since 2026-11-09; was 0.6) and so does the ambience.
  - A cry still dips the music, and the voice not at all.
  - The handheld-speakers filter applies to the voice as it does to the rest.
- **Finding and loading a line.** `Audio/VoiceLibrary` is free of the GPU and of the audio device:
  1. It reads the pack's `pack.json`.
  2. It answers whether a key has a file that is current: its text hash must match the catalogue's, or the line is stale.
  3. It decodes Ogg Vorbis to samples with a pure C# decoder (NVorbis, MIT), so tests decode without raylib.
  4. It keeps the last 48 lines and decodes ahead on a worker: when a talk starts, `ScriptRunner` knows every line of the batch, and asks for them all.

  A missing or stale line is silent and the text shows as it does today, so a pack that lags the script never breaks the game, and the game runs without a pack at all.
- **The text box.**
  - `IScriptHost.Say` and `DialogueManager.ShowDialogue` carry each line's key and voice beside its text.
  - When a box shows its line, the voice starts. The text still types at the options' speed (the voice doesn't wait for it, nor it for the voice).
  - A at a half-written line finishes the text and leaves the voice speaking. A at a finished line moves on and cuts the voice, unless the option "Let voices finish" holds the box until the line ends.
  - Plan 08 · P2's auto-advance, when it comes, waits for the voice.
  - A question's voice plays as the question shows, and its answers are not spoken.
  - A script's own pauses (`wait`, plan 09 · L3's `{pause}`) are unchanged. A new script command, `waitvoice`, waits for the last line's voice, for scenes timed to speech.
- **Who is speaking.** The cast entry is found as the talk starts:
  - a script's speaker id;
  - else the person spoken to (`self`): their own cast entry, else their class's or type's archetype by hash;
  - else the display name looked up in the cast.

  `{assistant}` resolves to `assistant_boy` or `assistant_girl` by the player's look; `{rival}`, and the rival's `npcType` and trainer class, to `rival`, a designed voice like the rest (since 2026-11-07).
- **Mouths.** `AudioManager.VoiceLevel` (the envelope of the voice now playing, read once a frame) moves the speaker's mouth:
  - in the field, through the faces' open and shut mouth (`CharacterFaces`, plan 11's expressions);
  - in battle, the trainer on the stage (Barry at each rival battle);
  - in the introduction, Rowan.

  The speaker on screen is the person the talk belongs to.
- **Options** (the options screen scrolls already):
  - `Voices`: Off, Story only (the named cast), All;
  - `Voice volume`;
  - `Let voices finish`: Off or On.

  With `Voices` off, nothing new is loaded, played or drawn.
- **Tests hear voices** as they hear sounds: `AudioManager.Listen` gets a line `voice <key> <voice>` for each one asked for, with or without a pack or a device, and `HeadlessScriptHost` writes the key beside each line of its `Transcript`.
- **Languages** (with plan 17).
  - A pack has a language: `voice/en/`, and later `voice/fr/` cast from that language's voices.
  - The catalogue's keys are the same in every language, and its text hash is that language's line.
  - A language without a pack is silent, or optionally speaks English under the translated text.
  - A designed voice can speak another language with the same blend where the engine has that language's phonemes (Kokoro has a few), so Barry can keep his voice in a translation.

### Checks

- **Catalogue in step:** every line of every script, overlay and map has a catalogue entry, and the catalogue holds no entry for a line that no longer exists. `VoiceTests` runs `catalogue` in memory and compares, as `CoverageTests` does.
- **Everyone has a voice:** every speaker resolves to a cast entry. A `speaker` whose display name matches no cast entry and has no id is an error that names its file and line.
- **Spoken texts are safe:** no spoken text contains a free placeholder, and a variant exists for each value of a few-valued one.
- **Files fit the game:** every file in a pack decodes, is within the voice bus's loudness window, has its peak under −1 dB, starts and ends at nothing, and matches the text hash it claims. These run when a pack is present (on the user's machine and in the release build) and are skipped, and say so, when none is.
- **What tests hear:** a talk asks for its lines' voices in order. A at a finished line cuts the voice; A at a half-written one doesn't. Voices off asks for nothing. A recorded battle replays the same log with voices on or off. A stale or missing line plays nothing and shows its text.

## The user's samples (not wanted since 2026-11-07)

The user ruled on 2026-11-07 that Barry gets a designed voice: nothing is recorded. The kit V3 built for it is kept as it
is (the script below, `docs/voice/recording.md`, the booth, `check-samples` and `bake-samples`), unused, should the user
ever want a recorded or cloned part. What it was written for:

- **What to record: a sample script written for cloning** (`docs/voice/samples.md`, about 20 passages of a sentence or two each). Altogether it covers:
  - every sound of English, with the names the game says most ("Pokémon", "Professor Rowan", "Twinleaf", the three starters);
  - Barry's moods, a few passages each. Cloning takes the delivery from the reference, so each mood the game needs must be in the samples:
    - calm and friendly;
    - excited and fast (most of his lines);
    - shouting;
    - frustrated;
    - quiet and serious, for the second half of the story;
    - laughing;
  - questions and exclamations, for their rising and falling ends.

  The passages are this game's own words in Barry's manner, so the samples are in character from the start. None of them says the user's name, where they live, or anything personal.
- **How long.** About two to three minutes of chosen takes, which is twenty to thirty minutes at the microphone, once. A second short session only if a mood comes out wrong in the generated lines, adding a sample for it.
- **Equipment and setting.**
  - Any decent USB microphone (a dynamic one forgives an untreated room). A phone's recorder in a quiet room is enough to start with: the clone copies the room and the microphone along with the voice, so cleaner samples make cleaner lines.
  - A quiet room with soft furnishings, the same place and distance throughout (about a hand's span), and a pop filter. No music or other voices behind.
  - 48 kHz, 24-bit WAV, peaks round −12 dB, no effects, no noise reduction (the bake cleans them evenly).
  - A short guide goes in `docs/voice/recording.md`: warming up, reading the mood, leaving half a second of silence before and after each passage.
- **Performance.** Barry is impatient, loud and warm-hearted: he runs everywhere, threatens fines he never collects, and talks faster than he thinks. Mostly high energy, with a few quiet moments in the second half of the story (after the lakes, at Spear Pillar). The user decides how he sounds (decision 2), and the samples are where that decision is made: the clone speaks as they did.

## Sessions

### V1 · The catalogue, the cast and the keys
- Line keys: N4's numbering and address, built here if N4 hasn't run.
- `speaker` with an id.
- `Data/voice/cast.json`: the named cast of the open areas and the archetypes by class and type.
- `tools/VoiceTool catalogue` writes `Data/voice/lines.json` (spoken texts filled where they can be, free placeholders flagged) and `docs/voice/coverage.md`.
- Tests: the catalogue in step, every speaker cast, spoken texts safe.
- **Done when** every line of the game has a key, a voice and a safe spoken text, and changing a line in a script makes the report say which lines' audio went stale.

### V2 · Playing a voice
- `AudioBus.Voice` and its dip; `Loudness`'s voice window.
- `VoiceLibrary` with NVorbis (with the user's OK for the package), the pack's `pack.json`, the cache and the decoding ahead.
- `Say`, `ShowDialogue` and `HeadlessScriptHost` carrying keys; A's two behaviours.
- The three option rows; `waitvoice`; `Listen`'s voice lines.
- A pack of test voices made in the tests themselves: a few lines of synthesised vowels, enough to measure timing, the dip and the cut.
- **Done when** a talk in the field speaks its lines from a pack, cuts and finishes as the player presses A, dips the music, and plays exactly as today with no pack or voices off.

### V3 · A designed voice for Barry (reshaped by the user's ruling of 2026-11-07)
- First written as the user's recordings and a cloning trial; the user chose instead a designed voice, no cloning, no recordings, no speech-to-text check.
- Kokoro-82M set up as the engine (`tools/VoiceGen/setup-kokoro.sh`, `kokoro_speak.py`, `engine.kokoro.json`), its licence in `docs/art/CREDITS.md` and `THIRD-PARTY-NOTICES.md`.
- Barry's `brief` and `design` in `cast.json`; `VoiceTool generate`, `bake` and `reroll` over Kokoro.
- Barry's lines generated with their variants (Sinnoh's first); a sample reel the user listens to, and rerolls or a changed design where wanted.
- **Done when** a new game hears Barry in his designed voice from his bedroom door to Oreburgh City and in his Route 203 battle's lines, and the user has approved the voice by ear.

### V4 · The designed cast
- `Data/voice/lexicon.json` for every name the open areas say.
- A design for each named character of the open areas (Sinnoh's first: Rowan, the two mothers, Lucas and Dawn as the assistant, Cynthia, Looker, Cyrus and his commanders, the Gym Leaders, the Elite Four), their lines generated.
- A sample reel of each, for the user to approve by ear; a voice they don't like gets another design.
- **Done when** the first chapter is fully voiced and the user has approved each character's voice.

### V5 · Everyone else
- The archetypes' voices, two to four per trainer class and townsfolk kind.
- The open areas' 380 townsfolk lines and 490 trainer lines generated and baked; the common scripts' nurses, clerks and attendants.
- Spot listening by the user from a reel per town.
- **Done when** "All" leaves no person of an open area silent but the shown-only lines.

### V6 · The face and the stage
- Mouths in the field, in battle and in the introduction; Rowan's introduction voiced.
- Plan 11 · C12's battle lines voiced as they arrive.
- Plan 08 · P2's auto-advance waiting for voices.
- The scenes of plan 02 retimed where `waitvoice` reads better than fixed waits.
- **Done when** a speaker's mouth moves only while they speak, and the harness's `story` and `intro` modes show it.

### V7 onward · Each chapter as it is written
- Every chapter of plan 02, and later each region's (plans 19 to 23), is voiced as part of its own work, as plan 05 · A6 gives each new area its music:
  - its new people's archetypes, generated;
  - its named characters, generated;
  - Barry's lines, generated in his designed voice once the chapter's text is final.
- The report keeps the gap visible.

### V8 · Other languages (with plan 17 · N6)
- A pack per language, cast from that language's voices, with the same keys and checks.

## Risks

- **Designed voices that sound flat or uncanny.** The user's ear is the gate: each character's reel is approved, and a voice that isn't gets another design (another blend, speed or pitch). Kokoro has no emotion control, so a line's direction moves only its pace and pitch; directions written by hand and three takes a line help most. If Kokoro can't carry a part, the plan stops at "Story only" with fewer characters rather than shipping voices the user doesn't like.
- **Lines that keep changing.** Every rewrite makes a line's audio stale, and the stale rule keeps an old take from contradicting new text. A rewrite costs seconds of the engine's time.
- **Twenty-eight voices for hundreds of characters.** Blends, speeds and pitches give each named character a voice of their own, but archetypes (V5) will share Kokoro's colours; a pitch moved too far sounds processed (kept between 0.9 and 1.2).
- **Names the player types.** They are never spoken: the spoken text drops them and a default name gets its own take. A few lines read oddly without the name; those are rewritten or shown only.
- **Licences.** Kokoro-82M's weights are Apache-2.0; its phonemiser, eSpeak NG, is GPL-3.0 and runs only as a separate tool on the developer's machine, never shipped. Both are recorded in `docs/art/CREDITS.md` and `THIRD-PARTY-NOTICES.md`.
- **The game's size and its start.** The pack is a separate download, and only the lines of the talk under way are decoded. Nothing is loaded at start-up but `pack.json`.
- **Faithfulness.** Platinum has no voices: everything here is behind an option, and with it off the game is today's.

## Needs and gives

- **Needs**:
  - plan 17 · N4's line address (or builds it, as above);
  - plan 02's chapters, for lines to voice;
  - plan 11 · C5 and C6 for the cast's faces that V6's mouths move, and C12's battle lines;
  - plan 08 · P2 for auto-advance;
  - plan 16 · T2 for the pack's download;
  - the user's OK for each engine, voice and package (Kokoro-82M approved 2026-11-07), and their ear for each voice.
- **Gives**:
  - plan 17 a second reader of its keys and a reason to keep them stable;
  - plan 12 · Q9 a voice already captioned by its own text;
  - plan 13 a streamer's "voices off" for broadcasts;
  - plan 16 a second download beside the game.

## Decisions for the user

1. **Whose voice is the user's.** *Decided 2026-10-07:* Barry's; *changed 2026-11-07:* nobody's. The player stays silent with no calls.
2. **Barry's character**: impatient, loud and warm-hearted, a teenager; his `brief` says it and his `design` is approved by the user's ear.
3. **How Barry is made.** *Decided 2026-11-07 (replacing cloning):* a designed voice, as everyone's: no cloning, no recordings, no whisper.cpp.
4. **Voices on by default.** *Recommended:* "Story only" once chapter 1 is fully voiced, "All" once V5 has run; off until then. Alternative: off by default always, as the original has none.
5. **Where the files live.** *Decided:* nothing recorded is in the repository. For the generated lines, *recommended:* a pack outside git, released as its own download and remade from the cast's designs by anyone with the engine. Alternatives: Git LFS; or the lines committed.
6. **The voices' engine.** *Decided 2026-11-07:* Kokoro-82M (Apache-2.0, CPU), with no trial.
7. **What stays unvoiced.** *Recommended:*
   - the game's own voice (`speaker none`: "Received the Potion!", signs, the battle's messages);
   - a question's answers;
   - the introduction's name prompts.

   Alternative: a narrator voice for the game's own lines.

## Status

- [x] V1 The catalogue, the cast and the keys
- [x] V2 Playing a voice
- [ ] V3 A designed voice for Barry (reshaped 2026-11-07)
  - [x] the sample script, `docs/voice/recording.md`, the booth page, `check-samples`, `bake-samples` (kept, unused since the ruling)
  - [x] `bake`, `jobs`, `generate`, `reroll`, `trial`, tested with synthesised stand-ins
  - [x] Kokoro-82M set up (`tools/VoiceGen/setup-kokoro.sh`, `kokoro_speak.py`, `engine.kokoro.json`), credited
  - [x] Barry's brief and design; his Sinnoh lines generated with their variants
  - [ ] the user listens to his reel and approves the voice (or asks for another design), and in the game; rerolls where wanted
  - ~~the user records the 21 passages~~, ~~the cloning trial~~, ~~whisper.cpp~~: not wanted (2026-11-07)
- [ ] V4 The designed cast
  - [x] `Data/voice/lexicon.json` (50 names, Sinnoh's first places, people and species) and `VoiceTool lexicon` for the rest
  - [x] briefs and Kokoro designs for Sinnoh's named cast (95 voices: the professor, both assistants, both mothers, Cynthia, Looker, Cyrus and his commanders and Charon, the eight Gym Leaders, the Elite Four and the rest of the story's people, and Sinnoh's role-named speakers)
  - [x] Sinnoh's lines of those voices generated into the pack (`generate <voices> --region sinnoh`): 2,098 lines, 2,197 takes (the assistant's in both voices), no take with a finding, `VoiceTool check` clean; 647 MB of WAV, kept in `~/.cache/pokemon-voicegen/voice/en` on the Windows machine
  - [ ] the user approves each main character's voice by ear from its reel (`reel --voice <id>`); a voice turned down gets another design
- [ ] V5 Everyone else (Sinnoh 2026-11-09 and Kanto 2026-11-09 voiced)
  - [x] `reel <prefix>`: a town's takes one after another for spot listening; `jobs`/`generate` take an archetype (`look:lass`) for all its voices
  - [x] Sinnoh's archetypes and role-named people designed (`VoiceTool design --region sinnoh`: 313 voices, a brief and a Kokoro design each, tools/VoiceTool/Designs.cs) and every Sinnoh line generated (2026-11-09: 6,580 takes in 408 voices, none missing or stale, `check` clean; the pack is `~/.cache/pokemon-voicegen/voice/en`)
  - [ ] the user approves the archetypes' voices by ear (`reel --voice look:lass#1`...); a voice turned down gets another design (or a profile edited in Designs.cs and `reroll`)
  - [ ] the other regions' archetypes and named people (`design --region kanto`...)
- [ ] V6 The face and the stage
  - [x] mouths in the field, in battle and in the introduction, moved by the take's own level (`MouthFlap`); Rowan's introduction voiced
  - [x] plan 11 · C12's battle lines (and plan 25 · YD6's giant lines) catalogued and voiced; auto-advance waits for a voice in battle as in the field
  - [x] the harness's `voices` mode shows a mouth open and shut (stand-in takes)
  - [ ] the scenes of plan 02 retimed with `waitvoice` (needs real takes to judge by ear)
- [ ] V7 onward: each chapter as it is written
- [ ] V8 Other languages
  - [x] the options' ENGLISH VOICES row: a language with no pack speaks English's under its translation
  - [ ] a pack per language

## V1 as built (2026-10-24)

- **Keys** are plan 17 · N4's, as it was built (`Text.ScriptLineKey`, `Text.PersonLineKey`); the introduction's are `intro:<region>.<Property>.0` (`RegionIntroLines`' property names; V6 maps them to the screen's phases).
- **Catalogue**: `Data/VoiceCatalogue.cs` (GPU-free, in the engine so the tests run it in memory) writes `Data/voice/lines.json` (a line to a row; about 12,400 lines over every region's scripts, overlays and maps) and `docs/voice/coverage.md` through `tools/VoiceTool catalogue`. Regenerate with `dotnet run --project tools/VoiceTool -- catalogue --add-missing` after anything that adds or rewrites a line; it lists new, stale, recast and gone keys. `--check` writes nothing.
- **Kept across runs**: `direction`, `notes`, and a spoken text (or a decision to leave a line shown only) written by hand with `"manual": true`, while the line's `hash` holds. Everything else is made afresh.
- **Spoken text** (`Data/SpokenText.cs`): `{player}`, `{rival}` and any free placeholder are dropped with the comma round them where that reads well; a line that cannot lose one is `shownOnly` (about 3% today) until written by hand. `{assistant}` (Lucas, Dawn) and `{starter}`/`{rivalstarter}` (the file's region's three) stay in the spoken text and get a variant per value (`~starter=Piplup`). The text box's markup (plan 09 · L3) is stripped, `{money N}` says its number. `~named` takes (default names kept) are by hand only; nothing generates them yet.
- **Cast** (`Data/voice/cast.json`, `Data/VoiceCast.cs`): `voices` (named ids; `rival` carries `clone`, `assistant` `chooses` boy or girl at run time), `names` (a script's speaker name, or a person's name, to a voice id or an archetype), `looks` (an `npcType` that stands for a named character), `people` (`place.key` overrides), `archetypes` (voice counts; default 3). An archetype voice is `look:lass#2` or `class:youngster#1`, picked by an FNV-1a hash of place and key. A person is resolved in the order people, named look, named name, rival class, trainer's own name, class, look, generic name, `look:person`. **A name nobody has cast gets a provisional voice of its own** (`catalogue --add-missing`, 200 today, one per display name across all regions: two regions' "Mom" share one until a `speaker <id>` or `people` entry splits them), and the test fails until that has run.
- **`speaker <id> "Name"`** is in the language (`Instruction.VoiceId`; `docs/scripts.md`); no script uses it yet, the names do the work.
- **Static speaker rule**: a line's voice is the last `speaker` written before it in the script; a script starts as `self`, which is the one voice of the people whose `script` it is, else `@self`, decided at run time (V2) by the person spoken to. Called scripts are not followed. `text` lines, `speaker none`, answers, signs and Pokémon (`npcType` Pokemon) are unvoiced and counted. `sayown`, `trainerline` and `broadcast` lines are not catalogued (a person's own before/after lines are); `Data/trainer-lines.json` (plan 11 · C12) is V6's.
- **Left for V2**: nothing plays; `IScriptHost.Say` does not carry keys yet; `Listen` has no voice lines.

## V2 as finished (2026-11-03)

- Ogg Vorbis is decoded by `Audio/OggVoiceDecoder.cs` over NVorbis 0.10.5 (MIT, approved by the user 2026-11-03), registered as `.ogg` in `VoiceLibrary.Decoders`; any rate, mono or stereo, taken to mono at the DS's rate like WAV. A damaged file is a take that can't be read (null), never a crash.
- `VoicePlaybackTests.AnOggTakeIsDecodedToMonoAtTheDsRateAndPlaysFromAPack` makes its Ogg in memory from the synthesised vowels with `OggVorbisEncoder` 1.2.2 (MIT), referenced by the test project alone; no audio is in the repository. (A new test-only package: the user may prefer another way to produce a test file.)
- The three options rows (VOICES, VOICE VOLUME, LET VOICES FINISH), `waitvoice`, the bus and its dip, the keys on `Say` and `Listen`'s voice lines were already in place.

## V3 to V6 without a recording (2026-11-04)

Everything of V3 to V6 that needs neither the user's voice nor a download, built and tested with synthesised stand-ins
(`VoicePacks.Vowels`, `VoiceSamples.StandIn`: vowels over room noise, nobody's voice). The boxes left open above are
the user's.

- **The recording kit** (V3). `docs/voice/samples.md` is the script: 21 passages in Barry's manner, our own words, six
  moods at least twice each (calm, excited, shouting, frustrated, quiet, laughing), questions and exclamations, the
  names the game says most and a passage for the sounds the others lack ("path", "judge", "azure", "measure"), with a
  pronunciation guide that is `Data/voice/lexicon.json`'s. Nothing in it names or describes the user. The same passages
  are `PokemonPlatinumEngine/Data/voice/samples/rival/samples.json` (id, mood, text, direction), which the tools read and
  `VoiceMakingTests` holds the page to. `docs/voice/recording.md` is the how-to (gear, room, 48 kHz mono 24-bit WAV, the
  half second of quiet each end, naming, the folder, `check-samples`, committing). `tools/VoiceBooth/booth.html` is the
  booth page: it reads `samples.json` from the folder chosen (Chrome/Edge's folder access; a download elsewhere), records
  at 48 kHz with the browser's processing off, keeps three takes and writes the best as 24-bit WAV. No test runs it (a
  browser page); the user is the first to open it.
- **`check-samples`** (`Audio/VoiceSamples.cs`, GPU-free): errors a clone would copy (missing file, not WAV, not 48 kHz,
  clipping, a peak under −24 dB, the room's noise over −40 dB, under 3 or over 35 s, no voice to be told from noise) and
  warnings to look at (stereo, 16-bit, a peak over −3 dB, noise over −50 dB, less than 0.3 s or more than 3 s of quiet at
  an end, a pace outside 1 to 5.5 words a second, a DC offset, a loose file, under 90 or over 300 s in all, a mood with
  no take). A take with no quiet in it is told apart from a noisy room by whether its quietest windows hold steady.
  `WavDecoder.Read` gives a file's own rate, depth, channels and peak (`Decode` is built on it).
- **The bake** (`Audio/VoiceBake.cs`, no Python): DC off, an 80 Hz Butterworth high-pass, a plosive softener (the band
  under 200 Hz turned down by up to 9 dB where it carries most of a loud moment, looking 10 ms ahead), the silence
  trimmed to 0.08 s before and 0.15 s after, the mixer's band-limited resampler (plan 31) to the DS's rate, levelled to
  the middle of the voice bus's window, a look-ahead limiter at −1.5 dB, 6 ms fades. Findings: a clip, a noisy floor, a
  limiter that left the line under the window, a length that doesn't fit the words (0.8 to 6 words a second). **Packs are
  written as 16-bit WAV at 32,768 Hz** for now: about 230 MB an hour, against the plan's 20 MB as Ogg (decision 5 below).
- **Generation without an engine** (`Audio/VoiceGeneration.cs`): `Plan` lists a voice's missing, stale or all lines as
  `VoiceJob`s (spoken text, its respelling, the direction written or guessed by `VoiceMoods.Guess`, the sample of the
  matching mood spread over a mood's samples by the key's hash, three seeds); `SeedOf(key, round, take)` makes the same
  seeds on every machine and a reroll takes the next round's (`RoundOf` reads a kept seed's round); `Generate` runs any
  `IVoiceEngine` (the tool's `CommandEngine` over `tools/VoiceGen/engine.json`, the tests' vowels, or `FolderEngine`,
  takes made by hand) and keeps the best baked take (fewest findings, then the pace nearest 2.8 words a second), stamping
  `pack.json` with the engine, seed and sample. A line said by whoever is spoken to (`@self`) is left out of a plan: its
  voices are V5's archetypes, decided at run time. `TrialLines` picks a trial's twenty lines (five to thirty words, no
  placeholder, spread over directions, the same every run); `Reel` strings a place's current takes together.
- **The lexicon and the briefs** (V4): `Data/voice/lexicon.json` respells 50 names in plain letters with the stressed
  syllable in capitals (`VoiceLexicon.Respell` hands an engine that reads text the respelled line; the engine chosen may
  want phonemes beside them). `cast.json`'s voices gained a `brief` (register, age, pitch, pace, manner), written for the
  first chapter's named cast (Rowan, both mothers, Lucas and Dawn as the assistant, Looker, Cheryl, Roark, Mars, the
  grunts); the rival has none, being the user's.
- **The mouth** (V6): `MouthFlap` (`Core/Voices.cs`) opens at about −32 dB RMS of a 40 ms window and shuts under −40,
  held at least 70 ms each way, read from the take's own samples where it is playing (`VoiceTalk.LevelNow`), so tests and
  the harness see it with no sound card. A face has an open mouth for every expression (`CharacterFaces.Paint(...,
  mouthOpen)`, painted under a moustache too; the battle's textures are made the first time a speaker needs one,
  `CharacterRig.FaceMaterial(expression, blink, open)`), and the field's pixel face a two-texel dark mouth
  (`Stamp(..., mouthOpen)`, `SpriteKey.Open`, baked apart). Whose mouth: in the field the person spoken to when the line
  is theirs, else the nearest person within 12 tiles whose voice the line is (`GameEngine.SpeakerNow`, read as the frame
  is drawn); in battle the trainer standing back on the platform (`SpeakingTrainer.MouthOpen`); in the introduction the
  professor.
- **Rowan's introduction voiced**: `IntroScreen.Voices` gives each line the catalogue's `intro:<region>.<line>.0` key
  (`VoiceCatalogue.IntroKey`, `IntroVoice`), and the game sets the introduction's box by the same option rows.
- **Battle lines**: every set of `trainer-lines.json` is in the catalogue as `battle:<set key>.<moment>.0` (lastPokemon,
  firstDown, lowHp, giant; a stem's key keeps its `*`), voiced by `VoiceCatalogue.BattleVoice` (the rival's class is the
  rival, a Leader by name, else the class's archetype); `BattleEngine.Voice` speaks them as their line appears, a press
  cuts one (LET VOICES FINISH holds it), auto-advance waits for it. 122 lines; the few that say `{player}` where it can't
  be dropped are shown only until a spoken text is written by hand. "Wake" was added to the cast's names (Crasher Wake).
- **ENGLISH VOICES** (V8's first piece): a row after LET VOICES FINISH, off unless chosen; `VoiceLibrary.Choose` gives a
  language with no pack English's, whose keys and hashes are the same under every translation.
- **The harness's `voices` mode** (not part of `all`) writes stand-in takes for the introduction and the nurse into its
  out dir and shoots the professor and a Pokémon Center's nurse with the mouth open and shut (`vo01`, `vo02`, `vo10`,
  `vo11`); looked at on the Windows machine, the open mouth showing under Rowan's moustache and on the nurse's sprite.

### What the user decides or supplies (left open)

1. **Record the 21 passages** of `docs/voice/samples.md` as `docs/voice/recording.md` says, into
   `PokemonPlatinumEngine/Data/voice/samples/rival/`, run `check-samples` until it reports no error, and commit them (or
   say so and a session commits them). The repository is public: this is decision 5 as already made.
2. **The cloning engine** (V3): approve, one at a time, the licence and download of each candidate to try (Chatterbox,
   F5-TTS, OpenVoice V2), then listen to each one's `trial rival` and choose. Its install goes in `tools/VoiceGen/env/`
   and `engine.json` (`tools/VoiceGen/README.md`).
3. **The synthesised voices' engine** (V4, decision 6): the same for Kokoro, Piper or the clone's own engine, and approve
   or change each brief in `cast.json` by ear.
4. **The speech-to-text check**: whether to download whisper.cpp (MIT) and a model for it.
5. **The pack's format**: Ogg Vorbis needs an encoder in `VoiceTool` (OggVorbisEncoder 1.2.2, MIT, is already used by
   the tests; using it in the tool is a new use to approve), or packs stay WAV, about eleven times larger.
6. **Decision 4, voices on by default**: still off; the recommendation stands (Story only once chapter 1 is voiced).
7. Kanto's champion's battle lines (`kanto_champion_megamax_*`) are spoken by a Champion archetype, since the cast has
   no voice for Kanto's rival apart from Barry's; a `people` entry or a voice of his own would change it.

Items 1 to 4 were answered on 2026-11-07 (no recordings, no clone, Kokoro-82M, no whisper.cpp): the section below.

## Sinnoh with voice, V5 (2026-11-09)

The goal of this session: the whole of Sinnoh (the story from the bedroom to the Hall of Fame and the post-game) playable with voice.

- **Who speaks "the person spoken to"** (`@self`): the catalogue now counts a person of an overlay that has a `script` but no lines of their own as the script's owner (`VoiceCatalogue.Build`), so 1,650 lines of scripts that one person runs (the Day Care, the Frontier's counters, the museum's researcher...) have their person's voice; `@self` fell from 1,968 lines to 318. What is left is a script several people run (`common.Nurse`, `Clerk`, `Rumour`...) or none: `tools/VoiceTool/SelfVoices.cs` walks every person of every map, finds the script talking to them runs (`FieldScripts.For`, and the scripts it calls) and the voice the game itself gives them (`ScriptRunner.VoiceOf`), and `generate` makes such a line once in each of those voices.
- **What belongs to a region**: `VoiceCatalogue.RegionOf` gives the common scripts written for one region (`JohtoTakeoverCall`, `UnovaBiancaAsks`, `HoennFeeder`, the Bug-Catching Contest's desk...) that region, the Pokégear's and Match Call's calls (`phone.txt`) their own `phone`, and the other `common` scripts none; `--region sinnoh` takes a line of no region as every region's (the Day Care, the training hall).
- **Designs for everyone**: `VoiceTool design [--region <id>]` gives a brief and a Kokoro design to each voice that speaks the catalogue (or the region) and has none, from a table of profiles by archetype or name (`Designs.cs`: register, age, pitch, pace, manner) and pools of Kokoro's own voices; the blend, pace and pitch are drawn by a hash of the voice's id, so the same cast file always makes the same voices, no two voices of the whole cast are alike (`VoiceTests`), and the voices of one archetype differ (a `mixed` archetype alternates man and woman). A voice with a design is never touched, so a voice tuned by ear stays tuned. `status` (`--voices`, `--missing`, `--shown`) says what a pack has of the catalogue voice by voice.
- **Making the pack**: four `generate` runs side by side (`KOKORO_THREADS=4` each, a pack folder each, merged), the first 650 lines at three takes a line and the rest at one (`generate --takes 1`: the machine was shared and a take every 12 seconds was too slow; a take with findings is reported, none was). 4,350 new lines for Sinnoh in all, 33 more for the hand-written spoken texts below.
- **Spoken texts written by hand** (`VoiceTool speak <file>`): 31 lines whose text names the player or the rival (Mom, the rival's mother, the guitarist, the assistant at Sandgem, Rowan's questions...) say the line without the name ("your friend", "that boy"); the rest of the 271 lines still shown only are the ones that name a Pokémon, a move, a number, a price or a ribbon (`{wearer}`, `{thing}`, `{number}`, `{lead}`...), which need a take per value and are not voiced.
- **Finding the pack**: `--voices <folder>` (a `LaunchOptions.VoicePack`, `VoiceLibrary.FoldersFor`) points the game at a pack anywhere and sets VOICES to ALL for the run alone; `docs/voice/playing.md` says what to run. VOICES stays OFF by default (decision 4): no pack ships with the game, and the voices wait for the user's ear.
- **Checked**: `VoicePackSwitchTests.TheFirstChapterOfANewGameResolvesEveryLineToATake` plays the first chapter with the bot and the real pack (105 lines asked, all with current takes; it passes quietly when there is no pack).

## Kanto with voice (2026-11-09)

The goal of this session: the whole of Kanto (FireRed's story from Pallet Town to the League, the Sevii Islands, Cerulean Cave and the post-game: its scripts, overlays, rooms and the common scripts that run there) playable with voice, `--region Kanto --voices <pack>`.

- **Kanto's own casting** (`cast.json`'s `regions.kanto`, `CastRegion`: `names`, `looks`, `classes`, `rival`, `trainerNamesByClass`; looked at before the cast's own tables, for a script or person whose place `VoiceCatalogue.RegionOf` says is Kanto's; a region with no entry is cast exactly as before, so no line of Sinnoh changed its voice, hash or take): `{rival}` and the Champion's class are `kanto_rival` (smug and cocky, not Sinnoh's Barry), Mom is `kanto_mom`, the rival's sister `daisy`; Kanto's trainers are cast by their class and never by a name another region gave a voice (a Youngster called Aaron is a Youngster, not Sinnoh's Elite Four), except the Leaders, the Elite Four and the named few (`names`). Oak, Brock, Misty, Lt. Surge, Erika, Koga, Sabrina, Blaine, Giovanni, Lorelei, Bruno, Agatha, Lance, Bill, Celio and Mr. Fuji are one voice each in every region they speak in (Johto's HeartGold Kanto has the same people), designed by hand with a brief; 27 voices designed by hand in all, the rest (Team Rocket's grunts, channelers, bikers, swimmers, jugglers, sailors...) by `design --region kanto` from new rows of `Designs.cs`.
- **Who speaks a line, by the way through the script** (`Data/VoiceFlow.cs`): the catalogue read a script in the file's order, so a speaker held across a label that is jumped to (Oak's `ProfOak` after the rival's lines) was the wrong one; `VoiceFlow` follows `goto`, `call` and `return` and gives each line the voices that can be in force at it. A line with one way takes it for every region but Sinnoh (whose pack was made by the file's order) and the regions' common scripts; `catalogue --ways` lists where the two part (Kanto 6 lines, the other regions' 14, Sinnoh's 14 left as they were, each a line the game has been silent on) and the 288 lines with several ways (a script called by several people).
- **Trainers' tags are not spoken** outside Sinnoh (`Youngster Tristan: Our eyes met!` is spoken `Our eyes met!`): 860 of Kanto's lines; Sinnoh's pack still says the tag, and a later session may make it again without.
- **The lexicon** gained 90 names (Kanto's towns, Leaders and the species its lines say); `status`, `export all --tsv --region` and `generate --into <folder>` (new takes written elsewhere, the missing ones still decided by `--pack`, so runs side by side never meet) were added to the tool.
- **Spoken texts by hand**: 5 lines that name the player or the rival (Bill, Celio, Oak, the rival).
- **The pack**: 2,394 new takes in four runs side by side (`KOKORO_THREADS=4`, 0.9 to 1.7 s a line at one take, the main voices at three takes a line: 346 lines at 4 to 5 s), merged into `en`: 8,974 takes in all (Sinnoh 6,580, Kanto 2,394), `check` clean. Kanto: 2,584 lines, 2,507 with a spoken text, 77 shown only (the tutors' `{wearer} learned {thing}`, Aides' `{var:CAUGHT}`, Magikarp's `{size}`), 60 said by whoever is spoken to in scripts no person runs; 2,494 of 2,497 takes' worth current (the three left are `common.Crossing` in a second captain voice, where the game plays the one take there is).
- **Checked**: `VoiceKantoTests` (the regional casting, a script's ways, the tag, every Kanto voice designed, and with a pack that has Kanto in it the first chapter played by the bot from the player's room in Pallet Town: 31 lines asked, all with current takes; every Kanto line with a voice of its own has a take).
- **What is left**: the user's ear on the reels (below); the 14 Sinnoh lines and the 288 multi-way lines the flow could fix; the other regions; `{player}` lines with a Pokémon or a number in them.

## V3 to V5 with Kokoro (2026-11-07)

The user's answers of 2026-11-07 approved Kokoro-82M and gave the rival a designed voice; this session set the engine
up and made Sinnoh's named cast.

- **The engine** (`tools/VoiceGen/`): `setup-kokoro.sh` makes a Python environment of its own (from Python 3.9, the
  Windows machine's; onnxruntime 1.19.2 and NumPy 2.0.2 are the last for it, pinned in `requirements-kokoro.txt`) and
  fetches by checksum the model's ONNX export (fp32, 325 MB, `onnx-community/Kokoro-82M-v1.0-ONNX`, the same Apache-2.0
  weights), `hexgrad/Kokoro-82M`'s `config.json` (the phoneme vocabulary) and the 28 English voices, all into
  `VOICEGEN_HOME` (`~/.cache/pokemon-voicegen`). **Not `POKEMON_CACHE`**: another session removed that folder whole
  while this one was using it, which the dev scripts allow. The package `kokoro-onnx` wants Python 3.10, so
  `kokoro_speak.py` is our own short runner over onnxruntime and `phonemizer-fork` with `espeakng-loader`'s eSpeak NG
  (GPL-3.0, a separate program on the developer's machine, never shipped). It speaks a take from a design, a direction
  (a share of speed and pitch: `calm` 1 and 1, `excited` 1.08 and 1.03, `shouting` 1.10 and 1.05, `quiet` 0.92 and
  0.98...) and a seed (three takes a line: as written, 4% slower, 4% faster; Kokoro itself draws nothing); names of the
  lexicon are said by their respellings syllable by syllable; a pitch is the take made slower by its share and its WAV
  stamped at a rate higher by the same share. Its output is brought under 0.89 of full scale before it is written, so
  the bake's clip check sees the take and not the 16 bits.
- **The tool**: `EngineConfig.Serve` keeps one engine process for a whole `generate` (a take is a JSON line on its
  input), `{voicegen}` names the engine's folder; `generate` takes several voices and `--region <id>`
  (`VoiceCatalogue.RegionOf`: an area by its world folder, a map or a script named after one by the region that lists
  it, an introduction by its region, a battle line by its trainer id's prefix); `reel --voice <id> [--count n]` is a
  voice's sample reel, its takes spread over their directions, with the words beside it in a `.txt`;
  `VoiceGeneration.Plan` makes a voice chosen at run time (`assistant`) in each voice it chooses from, since the game
  plays `assistant_boy`'s or `assistant_girl`'s take.
- **The designs** (`cast.json`'s `design`, `CastDesign`; `VoiceTests.EveryDesignIsABlendOfKokorosOwnVoicesWithinReason`
  holds each to Kokoro's voices, a speed of 0.7 to 1.4, a pitch of 0.85 to 1.25, a brief and no two alike): 42 written
  one by one for the story's people (the rival `am_puck:0.6,am_michael:0.4` at 1.14 and pitch 1.10; Rowan
  `bm_george:0.7,am_onyx:0.3` at 0.88 and 0.94 in en-gb; Cynthia `af_kore:0.5,af_heart:0.3,bf_emma:0.2`...) and 53 for
  Sinnoh's role-named speakers (an Ace Trainer, a Beauty, the Battle Castle's valet...), their blends drawn from pools by
  register and age. A name the cast shares across regions and that speaks mostly elsewhere (a trainer named Kurt, Juan,
  Sabrina, Scott...) was left provisional: its design would be wrong for the other region's famous one.
- **Time**: on the Ryzen 7 9800X3D about 1.5 s a take, 4.6 s a line with its three takes, on 8 threads; four runs side
  by side (`KOKORO_THREADS` 4 or 5 each, a pack folder each, merged after) took 5.5 to 6 s a line each, about 40 lines
  a minute together: Sinnoh's 2,197 takes in under an hour.
- **What is left**: the user's ear on each reel; the archetypes (V5) and every other region's voices; `{player}` lines
  written by hand where they are shown only; Ogg packs (decision 5 of the list above); splitting the voices one name
  gives several regions (`rival` speaks 432 lines outside Sinnoh, `mom` 97: each region's rival and mother want a
  `speaker <id>` or `people` entry of their own before their regions are generated); and a Romanian pack wants another
  engine, since Kokoro has no Romanian.

## Voice loudness (2026-11-09)

The user found the voices too quiet beside the music. Measured on the real pack (553 takes in 40-odd voices, a sample of
about 200): see the numbers in the close-out below. A take is levelled by `VoiceBake` to the voice window (-24 to -18 dB,
mono) and was then played centred at -3 dB (`cos(pi/4)`), 4 to 6 dB under the music's window of -20.5 to -15 and, with
the music dipped by only 0.6 (-4.4 dB), level with it. Fixed in the voice bus alone, with no pack file rewritten:
- **Make-up gain** `AudioMixer.VoiceMakeUp` 1.4 (+2.9 dB) in place of the pan law's 0.707 (a 6 dB lift in all), followed by a
  soft knee (`VoiceLimit`: transparent to 0.7, bent toward 1) so a take at the file check's -1 dBFS peak never clips.
  `Loudness.VoicePlayed` (-21.5 to -14.5) is the window a voice must measure in as the mixer plays it at VOICE VOLUME 100.
- **Ducking** `VoiceDuckLevel` 0.6 -> 0.5 (-6 dB) for the music and the ambience.
- **VOICE VOLUME runs 0 to 200** (`GameSettings.MaxVoiceVolume`, the bus clamps at `VoiceMaxVolume` 2), its own row with its
  own words; MUSIC stays 0 to 100. 100 is the normal level; the default stays 100 (the lift is in the bus).
- Tests: `VoicePlaybackTests` (played window, 200 never clips, the row's range, the dip).

## Romanian voices with Gemini (2026-11-12)

The user chose **Gemini TTS** for the Romanian pack (Kokoro has no Romanian; Kokoro stays the English engine).
`generate --language ro --engine gemini` (`tools/VoiceGen/README.md`, "Other languages: Gemini"):
- **Text**: each line's Romanian translation, found by the line's key in `Data/text/ro` (`tools/VoiceTool/Translated.cs`);
  a line with no current translation is left out (144 of Sinnoh's 6,623). The take is stamped with the English hash, so
  it goes stale with the English.
- **Voices**: designed, never cloned: one of Gemini's 30 prebuilt voices and a few tags of delivery
  (`tools/VoiceGen/gemini.json`; thirteen of the main cast chosen by hand, the rest by rule from their `cast.json` brief,
  `GeminiEngine.ByRule`, which nobody has listened to yet). The tags go in brackets before the line: Gemini follows them
  and doesn't say them, while a sentence of direction is read aloud. One take a line.
- **Model** `gemini-3.8-flash-tts`, key `GEMINI_API_KEY`. **The free tier allows 100 requests a day** for the model
  (quota `GenerateRequestsPerDayPerProjectPerModel`), so Sinnoh's 6,479 lines are some 65 days of the free tier: the
  engine stops asking once the day's quota is spent (`OutForTheDay`) and the next run picks up the lines still missing.
  A whole region wants a paid tier on the key (the user's decision).
- **Made so far**: a trial reel of twelve lines (Rowan, the moms, the rivals) the user approved, and the rival's first
  68 Sinnoh lines in `voice/ro` (git-ignored, as every pack).
