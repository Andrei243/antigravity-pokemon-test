# Plan 26 · Voice: every character's lines spoken, Barry in the user's own voice

Written 2026-10-07, before any session, at the user's request: "make all the characters' text also have audio
dubbing", with the rival, Barry, in the user's own voice (decided the same day: the player says nothing in Platinum,
so the user's voice goes to the most talkative male character instead).

**Goal**: every line a character says in a text box is also heard, in a voice that is that character's own. The rival,
Barry (or whatever the player names him), speaks in the user's voice, recorded by the user. The player stays silent,
as in Platinum. Everyone else is cast from a synthesised voice the user approves: the named cast one voice each,
everyone else from a set of voices by kind of person, so two lasses in one town don't sound alike. Turning voices off gives back today's game exactly. Nothing
is taken from the games or the anime, no real performer's voice is imitated, and the user's recordings stay theirs.

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
  - Every sound the game makes is synthesised in code, and the repository holds no audio file. Voice would be the first recorded sound: a deliberate exception, made only for voices (decision 5).
  - Platinum itself has no voice acting: this plan adds to the original rather than following it, so it is an option (decision 4).
- **Neighbours.**
  - Plan 17 · N4 numbers every line of a script (`Instruction.TextIndex`) and finds a person's lines by place and `NPC.Key`. That address is exactly what a recording needs, and this plan uses it rather than inventing a second one.
  - Plan 17's rule that an entry whose English changed is stale is the rule a recording needs too.
  - Plan 09 · L3 puts markup in lines (`{pause 0.4}`, colours).
  - Plan 08 · P2 brings hold-to-skip and auto-advance.
  - Plan 11 · C12 has leaders and rivals speak during their battles, and C5 and C6 give the named cast their faces.
  - Plan 12 · Q9's captions name sounds.
  - Plan 16 · T2 makes release builds.

## The user's part: Barry

**Decision (2026-10-07, the user's):** the player character has no lines, as in Platinum, and gets none, not even
short calls in battle. The user's voice goes to **the rival, Barry**: the most talkative male character, in nearly
every chapter from the first scene to the Pokémon League and after.

- **How much he says.** Today, 56 lines (about 780 words) from Twinleaf Town to Oreburgh City, spread over ten script files. They include his bedroom, Route 201's lake trip, Sandgem, the Trainers' School, Route 203's battle and Oreburgh. Across Platinum's whole story, in this game's own words, perhaps 400 to 600 lines. On top come:
  - his trainer lines before and after each rival battle;
  - plan 11 · C12's lines in battle (his last Pokémon, his first to fall, his last in the red).
- **His name.** The introduction lets the player rename him (`{rival}`). Other people's lines that say his name never speak it, unless the player kept "Barry": then a second take that says it can play. His own lines carry `{player}` often ("...Oh, it's you, {player}!"); the spoken version drops it.
- **His starter.** Lines that name his Pokémon (`{rivalstarter}`) are recorded once for each of the three.
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

  Directions are written by hand and kept across runs; everything else is regenerated. A line whose English changes keeps its key, its hash no longer matches, and every recording of it turns **stale**: it isn't played (the text shows alone) until it is redone. The tool lists new, changed and orphaned lines, as plan 17's report does for translations.
- **What is spoken can differ from what is shown.** A free placeholder is never spoken:
  - `{player}` and `{rival}` are typed by the player;
  - `{item}`, `{money}`, `{result}` and the variables can be anything.

  A line with one carries a spoken text that leaves it out ("Hey, {player}! Over here!" is shown, "Hey! Over here!" is spoken). The tool fills a first version by dropping the name and the comma round it and asks for the rest by hand. A line where the name can't be dropped gracefully can be marked shown-only, and the text then shows alone.
- **Variants.**
  - A placeholder with few values is recorded once per value: `{starter}` and `{rivalstarter}` three times, `{assistant}` once each for Lucas and Dawn (both synthesised).
  - Where the player kept a default name (Lucas, Dawn, Barry), a line may have a second take that says the name, played only then.

  The key of a variant is the line's key with `~starter=Piplup` or `~named` after it.
- **The cast** is `Data/voice/cast.json`. It maps whoever speaks to a voice:
  - **named characters** by an id: `rowan`, `rival`, `assistant_boy`, `assistant_girl`, `mom`, `rival_mom`, `looker`, `cyrus`, each gym leader and member of the Elite Four;
  - **archetypes**: by trainer class (Youngster, Lass, Hiker…) and by `npcType` (Nurse, Clerk, the townsfolk's ages and kinds), each with two to four voices. A person is given one of them by a hash of their place and key, the same on every run, so a town's people sound different and the same person always sounds the same.

  A script's `speaker` gains an optional id (`speaker rowan "Prof. Rowan"`, or a cast entry found by display name), so the display name stays free to change. `speaker none` lines (the game's own voice, signs, "Received the Potion!") are not voiced (decision 7).
- **Battle text** ("Turtwig used Tackle!") is the game's own voice and is not voiced. A trainer's lines are: before and after a battle (already in the overlays), and during it once plan 11 · C12 brings them.

### Where the voices come from

- **The user's recordings** for Barry (V3). Recorded per line, three takes of each, the best chosen. Optionally (decision 3), a model of the user's voice is trained on those same recordings, so lines written after a recording session can be heard in the user's voice until they record them for real; such a line is marked synthetic in the catalogue and replaced when they do.
- **Synthesised voices** for everyone else, made offline by a text-to-speech engine and baked into files, never generated while the game runs.
  - **Why offline:** a neural model in the game would add hundreds of megabytes and a second or more of delay per line, and would sound different on every machine.
  - **Candidates for V4's trial** (each licence to be checked when it is downloaded, with the user's OK as the project rules ask, and credited in `docs/art/CREDITS.md`):
    - Kokoro (Apache-2.0, a few dozen built-in English voices, fast on a CPU);
    - Piper (MIT engine, voices under per-voice licences, the fastest, and trainable on new voices);
    - Chatterbox (MIT, voice cloning from a short sample, with a control for how emotional a line is).

  The trial renders the same twenty lines in each, for the user to choose by ear. Voices are designed, not cloned, for everyone but the user: no anime or game performer is imitated, and no reference recording of anyone else is used without their consent.
- **A pronunciation list**, `Data/voice/lexicon.json`. Species, places, moves and people's names, with how to say each (in the phonemes the chosen engine takes), so "Pokémon", "Piplup", "Sandgem" and "Cyrus" are said the same way by every voice. The user's recordings follow it too: the recording script shows each name's pronunciation.

### Making the files

- **`tools/VoiceTool export <voice>`** writes the recording script for a voice: every line in story order, with its scene, the line before it and who said it, its direction, its variants, its pronunciations and its key.
- **The recording booth**, `tools/VoiceBooth/booth.html`. One page, opened in a browser on the user's own machine and needing no server. It:
  - shows a line with its context;
  - records from the microphone at 48 kHz;
  - plays the take back, keeps up to three, and marks the best;
  - writes each take as a WAV named after its key, into a folder the user picks.

  Nothing leaves the user's machine. Audacity or any recorder works as well, if the files are named by key.
- **`tools/VoiceTool bake`** turns raw takes or the engine's output into the game's files, all in .NET with no Python, as the user's machine has none:
  - cleaning: trims the silence at each end, high-passes at 80 Hz, softens plosives, and brings each line to the voice bus's loudness window (`Audio/Loudness`, a new `AudioBus.Voice` entry) with a gentle limiter;
  - format: resamples to the mixer's 32,768 Hz, mono, and encodes it as Ogg Vorbis;
  - checks, flagging any failure in a report: clipping, a noise floor too high, and a length that doesn't fit the text (a line spoken in a third of the time its words need is a wrong take).

  Optionally, a speech-to-text pass (whisper.cpp, MIT, run locally) compares what was said with the spoken text and flags a mismatch.
- **`tools/VoiceTool generate <voice>`** runs the chosen engine over a voice's lines, applying the cast entry's settings and the direction (speed, pitch, emotion), then bakes the output.
- **The coverage report**, `docs/voice/coverage.md`, written by the tool: per voice and per chapter, how many lines are recorded, generated, stale, missing or shown-only. A test holds it to what the files say, as `CoverageTests` does for moves.

### Where the files live (decision 5)

- **Size:** two hours of speech today, perhaps seven for the whole of Sinnoh's story and people, and several times that once the other regions of the chain have theirs. As Ogg Vorbis at about 48 kbit/s mono, that is about 20 MB an hour: some 40 MB today, 150 MB for Sinnoh.
- **Recommended: a voice pack outside git.**
  - A folder `voice/<language>/` beside the game, ignored by git like `overrides/`: `<voice>/<key>.ogg`, and a `pack.json` listing what is in it with each file's hash and the text hash it was made from.
  - Release builds (plan 16 · T2) zip it as a separate download.
  - The user's raw takes stay wherever they keep them.

  This keeps the repository light. It also keeps the user's voice, and any model trained on it, out of a public repository: a voice sample is personal data, and a model of it lets anyone make them say anything.
- **Alternatives:** Git LFS in the same repository (simple, but public if the repository is); or a private repository for the pack alone.

### In the game

- **A sixth bus.** `AudioBus.Voice`, with its own volume in the options, carries one voice at a time. A new line cuts the one before it with a 30 ms fade.
  - While a voice speaks, the music dips (about 0.6, like a cry's dip but gentler) and so does the ambience.
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

  `{assistant}` resolves to `assistant_boy` or `assistant_girl` by the player's look; `{rival}`, and the rival's `npcType` and trainer class, to `rival`, whose cast entry says "recorded": the user's takes, never a synthesised voice (or the stand-in model, decision 3).
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
  - The user's voice exists in the languages they record.

### Checks

- **Catalogue in step:** every line of every script, overlay and map has a catalogue entry, and the catalogue holds no entry for a line that no longer exists. `VoiceTests` runs `catalogue` in memory and compares, as `CoverageTests` does.
- **Everyone has a voice:** every speaker resolves to a cast entry. A `speaker` whose display name matches no cast entry and has no id is an error that names its file and line.
- **Spoken texts are safe:** no spoken text contains a free placeholder, and a variant exists for each value of a few-valued one.
- **Files fit the game:** every file in a pack decodes, is within the voice bus's loudness window, has its peak under −1 dB, starts and ends at nothing, and matches the text hash it claims. These run when a pack is present (on the user's machine and in the release build) and are skipped, and say so, when none is.
- **What tests hear:** a talk asks for its lines' voices in order. A at a finished line cuts the voice; A at a half-written one doesn't. Voices off asks for nothing. A recorded battle replays the same log with voices on or off. A stale or missing line plays nothing and shows its text.

## The user's recording

- **How much.** Barry's 56 lines today are a first session of about half an hour. The whole story is perhaps 400 to 600 lines. With his starter's three variants, his "Barry" takes and his battle lines, that is some 600 to 800 recordings. At a comfortable two to three finished lines a minute, with retakes, that is four to seven hours at the microphone, spread over sessions as chapters are written. A chapter's lines are recorded once its text is final, never before, so as little as possible goes stale.
- **Equipment and setting.**
  - Any decent USB microphone (a dynamic one forgives an untreated room).
  - A quiet room with soft furnishings, the same place and distance every session (about a hand's span), and a pop filter.
  - 48 kHz, 24-bit, peaks round −12 dB.
  - A short guide goes in `docs/voice/recording.md`: warming up, reading the direction, matching energy across sessions by listening back to the last session's first lines.
- **Performance.** The direction column and the line before each line give the scene. Barry is impatient, loud and warm-hearted: he runs everywhere, threatens fines he never collects, and talks faster than he thinks. Mostly high energy, with a few quiet moments in the second half of the story (after the lakes, at Spear Pillar). The user decides how he sounds (decision 2).
- **A model of the user's voice** (decision 3) needs only these recordings, about half an hour of clean speech, fine-tuned locally and kept wherever the user keeps the takes. It is never committed and never sent to a service the user hasn't chosen.

## Sessions

### V1 · The catalogue, the cast and the keys
- Line keys: N4's numbering and address, built here if N4 hasn't run.
- `speaker` with an id.
- `Data/voice/cast.json`: the named cast of the open areas and the archetypes by class and type.
- `tools/VoiceTool catalogue` writes `Data/voice/lines.json` (spoken texts filled where they can be, free placeholders flagged) and `docs/voice/coverage.md`.
- Tests: the catalogue in step, every speaker cast, spoken texts safe.
- **Done when** every line of the game has a key, a voice and a safe spoken text, and changing a line in a script makes the report say which recordings went stale.

### V2 · Playing a voice
- `AudioBus.Voice` and its dip; `Loudness`'s voice window.
- `VoiceLibrary` with NVorbis (with the user's OK for the package), the pack's `pack.json`, the cache and the decoding ahead.
- `Say`, `ShowDialogue` and `HeadlessScriptHost` carrying keys; A's two behaviours.
- The three option rows; `waitvoice`; `Listen`'s voice lines.
- A pack of test voices made in the tests themselves: a few lines of synthesised vowels, enough to measure timing, the dip and the cut.
- **Done when** a talk in the field speaks its lines from a pack, cuts and finishes as the player presses A, dips the music, and plays exactly as today with no pack or voices off.

### V3 · The recording kit and Barry's first chapter
- `VoiceTool export` and `bake`, and the booth page; `docs/voice/recording.md`.
- The user records Barry's 56 lines so far (and his two starters' other takes); the bake's report, then listening in the game.
- **Done when** a new game hears Barry in the user's voice from his bedroom door to Oreburgh City, and in his Route 203 battle's lines.

### V4 · The synthesised cast, the trial
- The three engines' trial on twenty lines, with the user choosing by ear and approving each licence and download.
- `Data/voice/lexicon.json` for every name the open areas say.
- `VoiceTool generate`, and voice design for the named cast of the open areas: Rowan, the two mothers, Lucas and Dawn as the assistant, Looker, Cheryl, Roark, Mars and the grunts.
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
  - Barry's lines, recorded by the user once the chapter's text is final.
- The report keeps the gap visible.

### V8 · Other languages (with plan 17 · N6)
- A pack per language, cast from that language's voices, with the same keys and checks.

## Risks

- **Synthesised voices that sound flat or uncanny.** The trial (V4) is the gate: if no engine satisfies the user, the plan stops at the user's own part and "Story only" with fewer characters, rather than shipping voices the user doesn't like. Directions per line and a few takes per line, choosing the best, help most.
- **Lines that keep changing.** Every rewrite makes recordings stale. Recording a chapter only once its text is final, and the stale rule that keeps an old take from contradicting new text, contain it. Synthesised lines are cheap to redo; the user's aren't, so theirs are recorded last.
- **Names the player types.** They are never spoken: the spoken text drops them and a default name gets its own take. A few lines read oddly without the name; those are rewritten or shown only.
- **Privacy.** The user's voice is personal data; a model of it doubly so. They stay out of git, and the plan never uploads them anywhere without their say-so.
- **Licences.** Each engine and each voice has its own terms (some of the freely downloadable voices are for non-commercial use, or ask that listeners be told the speech is synthetic). V4 records the terms of what is chosen in `docs/art/CREDITS.md`, and a voice with terms the project can't keep isn't used.
- **The game's size and its start.** The pack is a separate download, and only the lines of the talk under way are decoded. Nothing is loaded at start-up but `pack.json`.
- **Faithfulness.** Platinum has no voices: everything here is behind an option, and with it off the game is today's.

## Needs and gives

- **Needs**:
  - plan 17 · N4's line address (or builds it, as above);
  - plan 02's chapters, for lines to voice;
  - plan 11 · C5 and C6 for the cast's faces that V6's mouths move, and C12's battle lines;
  - plan 08 · P2 for auto-advance;
  - plan 16 · T2 for the pack's download;
  - the user's OK for each engine, voice and package, and their recordings.
- **Gives**:
  - plan 17 a second reader of its keys and a reason to keep them stable;
  - plan 12 · Q9 a voice already captioned by its own text;
  - plan 13 a streamer's "voices off" for broadcasts;
  - plan 16 a second download beside the game.

## Decisions for the user

1. **Whose voice is the user's.** *Decided 2026-10-07:* Barry's, recorded; the player stays silent with no calls.
2. **Barry's character**: how he sounds (age, energy, pace) is the user's to perform; the direction column carries whatever the user decides.
3. **A model of the user's voice.** *Recommended:* only as a stand-in for lines not yet recorded, marked as such and replaced, trained and kept on the user's machine. Alternative: no model, and unrecorded lines stay text only until they record them.
4. **Voices on by default.** *Recommended:* "Story only" once chapter 1 is fully voiced, "All" once V5 has run; off until then. Alternative: off by default always, as the original has none.
5. **Where the files live.** *Recommended:* a pack outside git, released as its own download, the user's takes and any model only on their machine. Alternatives:
   - Git LFS in this repository (public if the repository is);
   - a private repository for the pack.
6. **The synthesised voices' engine**: chosen by ear in V4's trial, among engines whose licences the user approves.
7. **What stays unvoiced.** *Recommended:*
   - the game's own voice (`speaker none`: "Received the Potion!", signs, the battle's messages);
   - a question's answers;
   - the introduction's name prompts.

   Alternative: a narrator voice for the game's own lines.

## Status

- [ ] V1 The catalogue, the cast and the keys
- [ ] V2 Playing a voice
- [ ] V3 The recording kit and Barry's first chapter
- [ ] V4 The synthesised cast, the trial
- [ ] V5 Everyone else
- [ ] V6 The face and the stage
- [ ] V7 onward: each chapter as it is written
- [ ] V8 Other languages
