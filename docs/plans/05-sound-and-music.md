# Plan 05 · Sound and music

**Goal**: make the game sound like a DS Pokémon game. Music for every place and situation that switches exactly when Platinum's does (day and night versions, bike and surf themes, trainer "eye" music, a battle and victory theme per kind of opponent, fanfares that pause the music), a sound for every action, and a cry for every species.

## Where we are

Since 2026-10-01 (the "better music" session):
- **Music engine** (`Audio/`): our own synthesiser (40 voices, band-limited wavetables with brightness envelopes, FM, vibrato, unison, synthesised drum kit, reverb) at the DS's 32,768 Hz, fed to a raylib audio stream from its audio thread. `MusicMixer` fades between areas, keeps the same theme running, cuts in for battles, and pauses the music under fanfares.
- **Songs** are text files in a small MML format under `Data/music/<region>/` and `Data/music/common/` (`docs/music-format.md`): 19 original tracks. Kanto: Pallet Town, a Kanto wild battle. Sinnoh: Twinleaf, Route 201, Routes 202–204, Sandgem, Jubilife, the lakes, Rowan's lab. Shared: title, Pokémon Center, wild, trainer and gym leader battles, wild and trainer victories, and the heal, item, level-up and new-Pokémon fanfares. Area themes have night arrangements (softer instruments, slower, quieter drums).
- **Director**: maps name their theme (`bgmTrack`); battles pick the wild, trainer, rival or gym theme from the opponent; roles look in the region's folder before `common`. Healing, level-ups (battle and Rare Candy) and receiving a starter play fanfares.
- **Checks**: `tools/MusicRender` renders WAVs and reports levels, clipping and clashing notes; `MusicTests` covers the director, the mixer and the parser, and renders every song.
- Still from before: about a dozen synthesised sound effects played through raylib `Sound`s; no cries, ambience or volume settings.

**Decision (2026-10-01)**: the music is synthesised by our own code from MML text rather than MIDI through MeltySynth and a SoundFont, so the game keeps generating all of its audio in code with no downloaded assets. The override folder and a SoundFont remain possible later if the built-in instruments fall short.

## What "like the original" means

- **Not copied**: Platinum's soundtrack, sound effects and cries are copyrighted recordings and compositions. They are not extracted, copied or transcribed note for note, and the decomp's audio data (`res/sound/SEQ`, `BANK`, `WAVARC`, the `cry.wav` files) is not used.
- **Recreated**:
  - the **sound map**: every moment the original plays a sound or changes the music, with the same kind of sound;
  - the **musical roles**: a day and a night theme for each town, city and family of routes; eye music per trainer type; separate battle and victory themes for wild Pokémon, trainers, gym leaders, Team Galactic, Cyrus, the rival, the Elite Four, the Champion and the legendaries; short fanfares for items, badges, healing, evolution and catches;
  - the **DS sound**: sequenced music played through a sampled instrument bank (brass, strings, piano, bells, marimba, flutes, synth leads, bass, drums), a handful of voices at once, an optional handheld-speaker output mode;
  - **original compositions** for each role, matching its tempo, mood, key and instrumentation;
  - **sound effects** designed to feel like the originals (a crisp menu blip, a soft bump, a door sliding, a ledge hop).
- **The checklist**: the decomp's `res/sound/pl_sound_data.json` names all of Platinum's ~230 music sequences and ~780 sound effects. Use the *names* as the list of roles and events, for example `SEQ_TOWN01_D` / `SEQ_TOWN01_N` (a town's day and night themes), `SEQ_ROAD_A_D` (a route family), `SEQ_EYE_BOY` … `SEQ_EYE_GINGA` (trainer eye music), `SEQ_BA_POKE`, `SEQ_BA_TRAIN`, `SEQ_BA_GYM`, `SEQ_BA_RIVAL`, `SEQ_WINPOKE`, `SEQ_FANFA1`–`6`, `SEQ_SHINKA` (evolution), `SEQ_BICYCLE`, `SEQ_NAMINORI` (surfing), `SEQ_PC_01`. The map headers say which theme each area plays; the scripts and source say where each sound effect is triggered.
- **Files the user supplies**: an `overrides/audio/` folder next to the game (ignored by git): a MIDI or audio file named after a sound id replaces the built-in sound. Only for files the user has the right to use.

## Architecture

- **Mixer**: one raylib `AudioStream` at 44.1 kHz stereo, refilled every frame (or from raylib's audio callback) with ~20–40 ms of audio. Buses for music, fanfares, sound effects, cries and ambience, each with a volume in the options menu; ducking (music dips while a cry plays).
- **Music**: MIDI sequences played by [MeltySynth](https://github.com/sinshu/meltysynth) (MIT licence, NuGet `MeltySynth`, a SoundFont synthesiser in C#) through a SoundFont instrument bank; loop start and end as MIDI markers; per-track volume, tempo and transpose.
- **Instrument bank**: start from a permissively licensed General MIDI SoundFont (for example FluidR3_GM, MIT: confirm the licence and credit it) and replace instruments that don't sound DS-like with our own samples, generated by a small `tools/SoundBank` synthesiser and written as a SoundFont.
- **Sound effects**: synthesised in code (layered oscillators, noise, filters, envelopes, pitch sweeps) into cached samples; CC0 packs (for example Kenney's) where they sound better, processed into the same style. Credits in `docs/art/CREDITS.md`.
- **Cries**: a procedural cry synthesiser, deterministic per species: pitch from height and weight, timbre from type, syllable count and contour from the name, a bit of seeded randomness. Variants follow the original's cry modes (`POKECRY_*` in the decomp's `include/constants/sound.h`): normal, half length, fainting (lower and slower), Pokédex, and the low-HP one.
- **Music director**: decides what plays. The area's theme (day or night) from its map header; the bike and surf themes; the eye theme when a trainer spots the player; the battle theme by opponent and the matching victory theme; fanfares that pause the music and resume it afterwards; fade out and in between areas, carrying on without a restart when the next area uses the same theme; silence where the original is silent.
- **Sound map**: `Data/audio/sound-map.json` maps game events to sound ids, and areas, trainer classes and species to music and cries, so scripts and code call `Audio.Play("door_open")` instead of synthesising sounds inline.
- **Handheld mode** (an option): resample to the DS's ~32.7 kHz with a gentle low-pass and a little quantisation noise, for a closer feel to the handheld's speakers.
- **Checking sound without ears**: Claude can't listen. The harness renders sounds and music to WAV files and spectrogram images; checks cover levels, clipping, timing, loop seams and pitch contours, and the user listens to approve each batch.

## Sessions

### A1 · Audio engine
Mixer stream, buses and volumes, MeltySynth with the instrument bank, MIDI loop points, fades; move the existing sounds over and drop the note-by-note re-synthesis. Harness mode `audio` that renders WAVs and spectrograms. Tests: rendering produces no clipping or NaNs; loops are seamless.

### A2 · Music director and sound map
Every switching rule: area themes with day and night, bike and surf, trainer eye music, battle and victory themes by opponent, fanfares that pause and resume, the low-HP alarm in battle, cries in battles and field events. Temporary placeholder tracks per role, so each rule can be heard. Tests for the director's decisions (which track should be playing in each situation) run without an audio device.

### A3 · Sound effects
The full set, by group:
- **Menus**: cursor, confirm, cancel, error buzz, open and close, page turn, text-box advance.
- **Field**: doors, stairs, warp tiles, bump, ledge hop, tall grass, the bike's bell and gear change, surf start, puddles, Strength boulders, Rock Smash, Cut, fishing (cast, bite, reel), the Pokétch, the PC switching on and off, saving, picking up an item, the "!" when a trainer spots the player.
- **Battle**: send-out and recall, ball throw, wobble, click and break-out, hits (normal, super effective, not very effective), stat up and down sweeps, each status condition, fainting, the HP bar, EXP gain, level-up, running away, and a sound per move type for the move effects of plan 04.

Check each against its checklist entry, render it, and have the user listen.

### A4 · Cries
The cry synthesiser and its variants for all species (1025 once plan 03 is done); cries on send-out, on appearing in the field, when fainting and in the Pokédex.

### A5 · Core music
Original compositions for the roles heard first: title screen, new-game introduction, Twinleaf Town (day and night), Route 201's family (day and night), Sandgem Town and Rowan's lab, Pokémon Center, Poké Mart, wild battle, trainer battle, both victory themes, the rival's theme and battle, the trainer eye themes, bike, surf, evolution, and the fanfares (item, key item, TM, badge, healing, level-up, catch). Written as note data (a compact text format turned into MIDI by `tools/MusicCompiler`), loops of 45–90 seconds, reviewed by the user as rendered WAVs.

### A6 · Region music
Alongside plan 01 and 02's chapters: the towns and cities (day and night), the other route families (including the snowy routes), caves and dungeons (the mine, Eterna Forest, Mt. Coronet, the lakes, the Galactic buildings, the ruins, the Great Marsh, Victory Road), the gym and gym-leader battle, Team Galactic and Cyrus, the legendaries (the lake guardians, Dialga and Palkia, Giratina), Spear Pillar and the Distortion World, the Elite Four, the Champion, the Hall of Fame, the credits, and the post-game areas.

### A7 · Ambience and polish
Rain, wind, snow, waterfalls, the sea, cave drips; positional sounds (panned by screen position); the handheld mode; a mixing pass with loudness targets per bus; the audio part of the options screen.

## Risks

- **Composition quality**: writing dozens of good tracks is the hardest part. Review every track, keep the themes short and strong, and remember any track can be replaced through the override folder.
- **No ears**: checks by analysis catch clipping and timing, not taste; the user's review is the real test.
- **Bank size**: a full SoundFont can be large; trim it to the instruments used.

## Needs and gives

- **Needs** plan 01's map headers for area themes, plan 02's trainer classes and scripts for eye music and fanfares, plan 03's species list for cries.
- **Gives** every scene its sound. A1–A3 can start at any time.

## Status

- [ ] A1 Audio engine — music part done (synth, mixer, stream, fades, render tool); still to do: sound effects through the mixer, buses and volume options
- [ ] A2 Music director and sound map — area, battle, victory and fanfare rules done; still to do: trainer eye music, bike and surf, the low-HP alarm, a sound map file
- [ ] A3 Sound effects
- [ ] A4 Cries
- [ ] A5 Core music — 19 tracks done (see above); still to do: new-game introduction, Poké Mart, rival theme and battle, trainer eye themes, bike, surf, evolution, the badge, TM and key-item fanfares
- [ ] A6 Region music
- [ ] A7 Ambience and polish
