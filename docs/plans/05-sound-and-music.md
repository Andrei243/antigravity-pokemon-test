# Plan 05 · Sound and music

**Goal**: make the game sound like a DS Pokémon game. Music for every place and situation that switches exactly when Platinum's does (day and night versions, bike and surf themes, trainer "eye" music, a battle and victory theme per kind of opponent, fanfares that pause the music), a sound for every action, and a cry for every species.

## Where we are

Since 2026-10-01 (the "better music" session):
- **Music engine** (`Audio/`): our own synthesiser (40 voices, band-limited wavetables with brightness envelopes, FM, vibrato, unison, synthesised drum kit, reverb) at the DS's 32,768 Hz, fed to a raylib audio stream from its audio thread. `AudioMixer` fades between areas, keeps the same theme running, cuts in for battles, pauses the music under fanfares, plays the sound effects over it (eight at once, panned) and dips it under a cry.
- **Buses and volumes** (2026-10-05, A1): the mixer has a bus with its own volume for the music, the fanfares, the sound effects, the cries and the ambience (`AudioBus`); the options screen sets the music's and the effects' (`GameSettings.MusicVolume`, `SoundVolume`, 0 to 100 in steps of 10), and the fanfares follow the music's, the cries and the ambience the effects', until A7 gives them settings of their own. The sound effects are `Audio/SoundBank`: synthesised in code on first use, at the synthesiser's rate, with rounded ends; `AudioManager.PlaySound(name, pan)` hands them to the mixer, and raylib's own `Sound`s are gone.
- **Songs** are text files in a small MML format under `Data/music/<region>/` and `Data/music/common/` (`docs/music-format.md`): 19 original tracks. Kanto: Pallet Town, a Kanto wild battle. Sinnoh: Twinleaf, Route 201, Routes 202–204, Sandgem, Jubilife, the lakes, Rowan's lab. Shared: title, Pokémon Center, wild, trainer and gym leader battles, wild and trainer victories, and the heal, item, level-up and new-Pokémon fanfares. Area themes have night arrangements (softer instruments, slower, quieter drums).
- **Director**: maps name their theme (`bgmTrack`); battles pick the wild, trainer, rival or gym theme from the opponent; roles look in the region's folder before `common`. Healing, level-ups (battle and Rare Candy) and receiving a starter play fanfares.
- **Every switching rule** (2026-10-05, A2): the surf and bicycle themes while the player rides, kept across areas; a trainer's eye theme by their class from the "!" until the battle cuts in; the battle theme by the most important opponent (`BattleRole` ranks the champion's over a grunt's) with roles for Team Galactic, Cyrus, the Elite Four, the Champion and the legendaries that fall back to the nearest song there is; and the **low-HP music**: a battle theme carries a second arrangement (`lowhp=` instruments, an `only=lowhp` alarm track of two pips a beat, an `unless=lowhp` pad, `lowhptempo` 1.08) that `SongPlayer` switches to in the middle of the song, keeping its place, while a Pokémon of the player's is in the red as its bar shows it (`BattleEngine.PlayerInDanger`, `BattleAnimator.LowHpRatio`). **Decision**: that is Black and White's way, the user's choice over Platinum's looping beep; the beep is not played. The sound map, `Data/audio/sound-map.json`, holds the original's trainer-class-to-eye-theme table (`src/field_bgm.c`) by our names for the classes and the battle themes the few special opponents bring; `MusicTests` holds every class the maps use to an entry. New songs: `eye_boy`, `eye_girl`, `surf`, `bicycle`; the thirteen other eye themes fall back to the boy's or the girl's until A5 and A6 write them.
- **Checks**: `tools/MusicRender` renders WAVs and reports levels, clipping and clashing notes, draws a spectrogram of each song with `--spectrogram`, and with `--sounds` renders every sound effect with its spectrogram and a click check; `MusicTests` covers the director, the mixer and the parser, and renders every song; `SoundTests` renders every sound and holds the buses to their jobs (a sound over the music, each bus's volume on its own, a cry's dip, a full bank, panning, mute).
- **Sound effects** (2026-10-06, A3): the full set, 74 sounds in four groups ([`docs/sound-effects.md`](../sound-effects.md)): menus (cursor, confirm, cancel, an error buzz, the start menu opening and closing, a page turning, the text box moving on), the field (doors that swing and doors that slide, stairs, warp panels, the bump, a ledge, footsteps in snow, puddles, shallows and mud, very tall grass, surf, saving, the PC on and off, the "!", healing, and the bicycle's bell and gear, Strength, Rock Smash, Cut, fishing and the Pokétch made ahead of their moments), battles (send-out and recall, the ball's throw, wobbles, click and burst, three strengths of hit, stats up and down, each status condition and confusion, fainting, the EXP bar, running away) and a launch sound for each of the eighteen types. Made with `Audio/SoundDesign`, a small toolkit of layers (band-limited oscillators with glides and vibrato, FM bells, noise through a gliding state-variable filter, clicks, bubbles, echo). Each names the original's sound it stands for where that is known. The battle's log gained three events for it: `HitSounded` says when a hit was not very effective, `Confused` and `GotAway` are attached to their lines. **Decisions**: footsteps follow the original's `player_move.c`, so ordinary tall grass and sand are silent and only very tall grass rustles (this game rustled in all tall grass before); the HP bar is silent, as in the original; the move sounds are one per type, until plan 04's move effects ask for sounds of their own; the old ledge hop and text box used the confirm blip and now have their own.
- **Cries** (2026-10-06, A4): every species and form (1025 and their forms) has a cry of its own, synthesised by `Audio/Cries`. Its pitch comes from its size, its syllables and their vowels and shapes from its name, and its timbre from its types, shared by its family. It plays in the original's modes (`POKECRY_*`): pinch (−1.5 semitones), faint (−3.5), half, field event (+1 with a detuned echo) and Pokédex, pitched as the DS pitches a sample. Cries are heard on send-out (in a pinch when hurt or ailing, as the original's `battle_lib.c` decides), on a wild Pokémon appearing, on fainting, at a Pokédex entry, in the party's summary, at the starter's question, before and after an evolution, at the title (Giratina), and from a script's new `cry` command. **Decision**: cries are generated from data rather than designed one by one, so all 1025 exist now and a hand-made override can come later through `overrides/audio`. The move-specific modes (Growl and Roar's howl, Hyper Voice, Uproar) wait for plan 04's move effects to ask for them.
- No ambience yet.

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

**Waiting for a theme of their own** (2026-10-05, plan 01 · M5): the south-west is open, and its areas play the nearest of the seven Sinnoh themes there are. Each overlay (`Data/world/sinnoh/overlays/<key>.json`) names the role it waits for in a comment beside its `bgmTrack`, and the area's file gives the original's role by day and by night:

| The original's role | Areas | Plays for now |
|---|---|---|
| `ROAD_B` | Routes 203 and 204 | `sinnoh/route202` |
| `ROAD_C` | Route 205 (south), Valley Windworks, Fuego Ironworks | `sinnoh/route201` |
| `ROAD_A` | Route 219, Verity Lakefront (and Routes 201 and 202, which have theirs) | `sinnoh/route201` |
| `CITY03` | Oreburgh City (and its Mart) | `sinnoh/jubilife` |
| `TOWN03` | Floaroma Town (and its Mart), Floaroma Meadow | `sinnoh/sandgem` |
| `D_05` | Oreburgh Gate, the Ravaged Path | `sinnoh/lake` |
| `D_04` | Oreburgh Mine | `sinnoh/lake` |

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

- [x] A1 Audio engine — done 2026-10-05: synth, mixer, stream, fades, render tool (2026-10-01); sound effects through the mixer, buses, the options' volumes, the render tool's sounds mode and spectrograms, and the tests (2026-10-05). Decided against MeltySynth and a SoundFont (decision of 2026-10-01, above), so the "instrument bank" of the architecture is `Audio/Instrument`.
- [x] A2 Music director and sound map — done 2026-10-05: area, battle, victory and fanfare rules (2026-10-01); trainer eye music, bike and surf, the low-HP arrangement (Black and White's way, see "Every switching rule" above) and the sound map file (2026-10-05). Cries in battles and field events wait for A4.
- [x] A3 Sound effects — done 2026-10-06: the full set of the plan's checklist (see "Sound effects" above) and where each plays; the user listens to approve the batch (rendered with `tools/MusicRender --sounds`). The field sounds whose moment comes with plan 02 · S2 (the Bicycle, Strength, Rock Smash, Cut), fishing and the Pokétch are made and wait for their code.
- [x] A4 Cries — done 2026-10-06: the cry synthesiser, its modes and every place the original cries (see "Cries" above); the user listens to approve them (`tools/MusicRender --cries`). The move-specific modes wait for the moves that use them.
- [ ] A5 Core music — 23 tracks done (see above; bike, surf and the boy's and girl's eye themes 2026-10-05); still to do: new-game introduction, Poké Mart, rival theme and battle, the thirteen other trainer eye themes, evolution, the badge, TM and key-item fanfares. The evolution scene exists since 2026-10-02 (`UI/EvolutionScreen.cs`): it plays over whatever music is on and uses the catch fanfare for its congratulations, so the evolution theme starts in `EvolutionScreen.Begin` and its own fanfare replaces `FanfarePokemon` there
- [ ] A6 Region music
- [ ] A7 Ambience and polish
