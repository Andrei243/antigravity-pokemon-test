# Plan 31 · A richer sound for the later regions' music

Written 2026-11-02, before any session, at the user's request.

**Goal**: from Kalos on, the music sounds like the 3DS and Switch games' era of soundtrack and not the DS's: a fuller mix, more voices and instruments each region can call its own. The songs stay MML, written by us and synthesised at run time, so they keep everything the game does with them. Kanto to Unova keep the sound they have.

**Why not generated audio.** The user asked whether Google's Lyria would make the music better. It was weighed on 2026-11-02, from its documentation and not by listening, and it isn't used for the game's songs. Finished audio can't change arrangement in the middle of a song (night, low HP, the giant layer), its documentation offers fixed clips or songs of a couple of minutes with no word of seamless loops, separate generations won't line up or share motifs across hundreds of songs, and the repository holds no audio files. It may still be used as a sketchpad for ideas, as long as what is written is our own and close to neither the sketch nor the originals (decision 4).

## Where we are

- **The songs**: 398 `.mml` files in `PokemonPlatinumEngine/Data/music/`: `common` 148 (with the legendaries' themes in `common/legendary`), `hoenn` 61, `unova` 53, `johto` 52, `kanto` 48, `sinnoh` 36. Since 23 · Z1's first part `kalos` has two more (`vaniville`, `aquacorde`), in the DS sound; Alola, Galar and Paldea have no folder yet, and plan 23 writes their "music roles". **So almost nothing has to be rewritten for them.** Their legendaries' battle themes already exist and are written for the DS sound.
- **The synthesiser** (`Audio/Synthesizer.cs`): `SampleRate` is a constant, 32,768 Hz, and `MaxVoices` a constant, 40. Each voice is a band-limited wavetable crossfading from a bright spectrum to a mellow one, with optional FM, breath, vibrato, a glide, unison and an ADSR envelope; the drum kit is synthesised apart; one Freeverb-style reverb is shared by every voice, at a level the song sets.
- **The instruments** (`Audio/Instrument.cs`; `docs/music-format.md` lists them): woodwinds, brass, strings, keys and mallets, synths and the `kit`. Nothing plucked in a region's own way, no hand percussion beyond the kit, no distorted guitar.
- **The rate reaches a long way**: `SampleRate` is read by `AudioMixer`, `Cries`, `Loudness`, `SongPlayer`, `SoundBank`, `SoundDesign`, `AudioManager` and `tools/MusicRender`, and by `MusicTests`, `SoundTests`, `AmbienceTests`, `CryTests`, `MovePresentationTests` and `FocusAndFrameCapTests`. A bare change of the constant would change every song, sound and cry in the game.
- **What songs do that must keep working**: an exact loop point (`L`), the night arrangement, the low-HP arrangement and the giant layer switched in the middle of a song (`SongPlayer`), the five buses and their loudness windows (`Audio/Loudness`), the options' handheld speakers and mono, the jukebox, and `MusicRender`'s level, click and clash checks.
- **A region's file** (`Data/RegionFiles.cs`) has the folder of its music and no word for how it sounds.
- **Claude can't listen.** Levels, clashes and spectrograms can be checked by machine. Whether it sounds better is the user's to judge from rendered MP3s, at every step of this plan.

## Design

**Two sounds, chosen by data.** A region's file names its sound (`"sound": "modern"`; absent is today's), and a song may name its own in its header (`sound modern`), which wins. That is how a legendary of Kalos in `common/legendary` joins its region. A song with neither plays as it does today, **to the sample**: nothing about the existing songs changes, and a test holds a sample of them to what they rendered before the plan.

**The rate of the mix, and the rates of what goes into it.** The rate stops being one constant:

- The mixer runs at 48,000 Hz, the device's rate.
- A synthesiser takes its rate as it is made. The DS sound keeps 32,768 Hz and 40 voices, and its output is resampled into the mix. So do sound effects, cries and ambience, whose numbers are all in 32,768 Hz.
- The modern sound's synthesiser runs at 48,000 Hz with more voices (decision 2).

What was `Synthesizer.SampleRate` becomes `Synthesizer.DsRate` where it means the old sound and `AudioMixer.Rate` where it means the mix, each read site decided one by one. The resampler must be good enough that nobody can tell (MU1 measures it, and the user listens): the DS songs' character stays, and they gain nothing and lose nothing.

**What the modern sound adds**, each behind the song's sound so the DS one can't move:

- The wavetables and the drum kit rendered for the higher rate, so the top end is real and not empty.
- A reverb per song with a size, a pre-delay and damping (`room hall 2.1`), in place of one level.
- Stereo width: unison and section instruments spread across the field, and a track's `pan` that can move.
- A gentle compressor on the song's own mix, before its bus, so a full arrangement holds together inside the loudness window.
- Key velocity reaching tone as well as level: a harder note is brighter.

**Instruments by region.** New instruments go in the one table and are open to any song. Each region leans on its own:

| Region | Sound to aim for | Instruments it adds |
|---|---|---|
| Kalos | French, orchestral, elegant | solo cello, fuller string sections, musette (the `accordion` made fuller), harpsichord |
| Alola | Island, warm, relaxed | ukulele, steel drum, slide guitar, hand percussion, log drum |
| Galar | Stadium, British, big | electric guitar (clean and driven), big kit, crowd chant, brass band |
| Paldea | Spanish, open-air | nylon guitar, castanets, handclaps, trumpet with a mute, fuller orchestra |

Plucked strings and hand drums suit physical modelling (Karplus-Strong and the like) better than wavetables, so a new instrument may be a voice of a new kind. A driven guitar needs distortion per track. All of it stays code: no sample is needed unless decision 3 says otherwise.

**The songs are still original.** The rule of `docs/music-format.md` holds: no melody, recording or sequence from the games. A region's "sound" is its instruments and manner, never a tune.

## Decisions for the user

Each has a recommended answer first. MU0 can start on the recommended answers, and it is made so that decision 1 is answered by ear.

1. **Is the richer sound worth having?** Answered from MU0's pair of MP3s: one tune, in the DS sound and in the modern one. If it isn't better, the plan stops there, having changed no existing song.
2. **How many voices?** Recommended: 96, then measured on the Windows machine. The mixer runs on raylib's audio thread and an underrun is heard as a click, so the number follows the measurement.
3. **Free-licensed samples for what synthesis does badly** (a choir's words, a real drum kit)? Recommended: no, until the user hears a synthesised attempt. Samples would be the repository's first audio files, and each needs the user's OK before downloading and a line in `docs/art/CREDITS.md`.
4. **May generated music be used as a sketchpad?** Recommended: only for the user's own listening, to find a direction in words ("what should Alola's towns feel like"). Nothing generated enters the repository, and no song is transcribed from one. Check the service's terms before anything is released.
5. **Do the DS-era regions ever change?** Recommended: no. Unova's soundtrack sits close to the line, and a song of an earlier region may be moved one at a time if the user asks (a header line), but no session does so on its own.

## Sessions

- **MU0 · The test, before anything else.** The smallest path through: a synthesiser whose rate and voices are its own, the modern sound's reverb and width, the Alola instruments, and one original route theme with night and low-HP arrangements. Rendered by `tools/MusicRender` only, not yet in the game's mixer, as MP3s of the same tune in both sounds, for decision 1. Changes no existing song.
- **MU1 · The mix at 48 kHz.** `AudioMixer` at its own rate, the resampler, every read of `SampleRate` decided, `MusicRender` and the tests moved over. Golden samples of existing songs, sounds and cries, compared before resampling, must not move. Measures the resampler, the audio thread's time and the voices (decision 2). The user hears a before and after of each bus.
- **MU2 · The modern sound in the game.** The region file's and the song's `sound`, `MusicLibrary` and `SongPlayer` choosing the synthesiser, changing from one sound to the other across a fade, the compressor, velocity to tone, night, low-HP and giant arrangements switching as they do now, the handheld speakers, mono and the jukebox. Each new header word and instrument goes in `docs/music-format.md` first.
- **MU3 · Kalos's instruments and first songs**, with plan 23's Kalos sessions, which then write the rest of its music roles on them. Vaniville's and Aquacorde's songs, written in the DS sound before this plan's test, are arranged again for it.
- **MU4 · Alola's**, the instruments from MU0 finished, with plan 23's Alola sessions.
- **MU5 · Galar's**, with its sessions: the driven guitar, the big kit, the crowd chant.
- **MU6 · Paldea's**, with its sessions.
- **MU7 · The later legendaries' themes.** Those of Kalos on in `common/legendary` given `sound modern` and arranged again for it, each by ear, once their region's instruments exist.

Hisui and Lumiose take their sound from plan 23's decisions when their sessions come; the recommended default is modern.

## What must hold

- Every test of `MusicTests`, `SoundTests`, `CryTests` and `AmbienceTests` stays green, with the modern songs held to the same loudness windows and clash checks.
- Existing songs, sounds and cries render to the sample as they did, before the mix's resampling.
- No audio file enters the repository, unless decision 3 is answered yes.
- A loop's join stays silent to `MusicRender`'s click check in both sounds.
- The audio thread keeps up on the Windows machine with the largest modern song at night, in low HP and with the giant layer.

## Status

- [x] MU0 · The test (2026-11-02); decision 1 waits for the user's ear. Built: `Synthesizer(SynthSound.Modern)` at 48 kHz with 96 voices beside the untouched DS one (`new Synthesizer()`: 32,768 Hz, 40 voices; `SampleRate` and `MaxVoices` unchanged, the DS path's per-sample drum filters moved to the rate by a factor that is the very number at the DS's rate), wavetables to 20 kHz, unison voices spread by `Instrument.Unison`/`Spread` and the track's `width=`, velocity to tone, a stereo room reverb (`room hall 1.8`: pre-delay, early reflections, combs set from the length, damping), the header's `sound`, and the Alola instruments: `ukulele` and `slideguitar` (Karplus–Strong, `VoiceKind.Pluck`), `steeldrum` and `logdrum` (`VoiceKind.Modal`), `warmpad`, and hand drums in the kit (`g` `d` `n` `w`). The tune is `tools/MusicRender/songs/mu0_island_route.mml` (no song of the game) with night and low-HP arrangements, rendered by `MusicRender --sound ds|modern` through `Audio/OfflineMaster` (no mixer). Both sounds measure about −19 dB by day, −18 at night, −19.5 in the red, no clashes, joins no larger a step than the loop's own start, no voice cut short (the most at once 37). `ModernMusicTests` holds six DS renders to hashes measured before any line was written (Windows only: the maths library's last bit), and the modern renders to the music window, the clash and join checks and the voices. The compressor waits for MU2. The MP3s were made with the libmp3lame.dll Audacity installs, by a scratch program outside the repository, into the git-ignored `tools/MusicRender/out/music-test`.
- [ ] MU1 · The mix at 48 kHz
- [ ] MU2 · The modern sound in the game
- [ ] MU3 · Kalos
- [ ] MU4 · Alola
- [ ] MU5 · Galar
- [ ] MU6 · Paldea
- [ ] MU7 · The later legendaries' themes
