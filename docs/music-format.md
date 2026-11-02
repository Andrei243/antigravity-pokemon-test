# Music

The game's music is original, written for this game in the style of the DS soundtracks: a small sampled-sounding orchestra (flutes and reeds, brass, strings, piano, bells and mallets, a few synth leads, bass and drums) and short loops. No melody, recording or sequence is taken from the games. Songs are text files in `PokemonPlatinumEngine/Data/music/`, synthesised in code at run time like everything else, so there are no audio files in the repository.

## How it plays

- **`Audio/Synthesizer`**: up to 40 voices at the DS's 32,768 Hz output rate (`Synthesizer.DsRate`), which the mix takes into its own 48 kHz (below, "The mix's rate"). Each voice is a band-limited wavetable that crossfades from a bright spectrum to a mellow one, with optional FM, breath noise, vibrato, a glide into the note, detuned unison and an ADSR envelope; the drum kit is synthesised separately. A Freeverb-style reverb is shared by all voices.
- **`Audio/Instrument`**: the instrument bank. Gains are balanced so every instrument plays a middle-C phrase at about the same loudness (`MusicRender --calibrate` prints the levels).
- **`Audio/SongPlayer`**: plays one song, starting notes on a 1 ms grid, wrapping at the loop point, and applying the night arrangement.
- **`Audio/AudioMixer`**: what the game hears, on five buses (`AudioBus`: music, fanfares, sound effects, cries, ambience), each with its own volume. A new song fades the old one out first (or cuts in, for battles); the same song carries on without restarting; a fanfare pauses the music, plays once and lets the music come back where it stopped; sound effects play over the music, up to eight at once (a further one takes the place of the one nearest its end), panned; the music dips to 45% under anything on the cry bus and comes back once it is over; the field's ambience beds loop on their own bus, each gliding to the gain and pan it is set to (`SetAmbience`, [`sound-effects.md`](sound-effects.md), "Ambience"). It ends with a 30 Hz high-pass and a soft limiter, and, with the options' Speakers set to Handheld, the handheld's own speakers (below). It runs at 48,000 Hz (`AudioMixer.Rate`), the device's own rate; the buses above are mixed at the DS's rate and resampled into it ("The mix's rate").
- **`Audio/SoundBank`**: the sound effects (plan 05 · A3), synthesised in code the first time each is asked for with the layers of `Audio/SoundDesign` (band-limited oscillators with pitch glides, FM bells, noise through a gliding resonant filter, clicks, bubbles, an echo), brought to their peak level and given rounded ends so they never click. `SoundBank.Entries` lists each with its group, the original's sound it stands for and when it plays; [`sound-effects.md`](sound-effects.md) is that list. `SoundBank.Names` is what a script's `sound` may name.
- **`Audio/Cries`**: every species' and form's cry (plan 05 · A4), synthesised from its size, name, types and family, in the original's modes (pinch, faint, half, field echo), played on the cry bus with `AudioManager.PlayCry`; the "Cries" section of [`sound-effects.md`](sound-effects.md).
- **`Audio/MusicDirector`**: decides what plays, with no audio device. Maps name their theme in `bgmTrack`. Roles (title, battles, victories, fanfares) look in the current region's folder first, then `common`, then a more general role (`battle_gym` falls back to `battle_trainer`).
- **`Core/AudioManager`**: opens a raylib audio stream whose callback runs the mixer on raylib's audio thread, so music keeps playing while the game thread loads a map. `PlayMusic(id)`, `PlayMusic(role)`, `PlayFanfare(role)`, `PlaySound(name, pan)`, `PlayCry`, `SetAmbience(layers)`, `Region`, `Handheld`. The options' four volumes (the music, the sound effects, the cries and the ambience, 0 to 100) reach the buses through `SetVolumes`; the fanfares follow the music's, since a fanfare is the music's own pause.

At night (`TimeOfDay.Night` and `LateNight`) area themes play their night arrangement: each track may name a softer `night=` instrument, the tempo drops by the song's `nighttempo`, drums are quieter and the reverb a little wetter. The arrangement is chosen when the song starts, as Platinum does on entering an area. A song whose header says `night none` has one arrangement for day and night alike, as the original's caves and dungeons do (their map headers give the same `SEQ_D_*` for day and night): the cave and the mine play the same at dusk, and carry on across it without a restart.

A battle theme has a **low-HP arrangement** as well, switched on and off in the middle of the song while a Pokémon of the player's is in the red (its bar's colour, `BattleAnimator.LowHpRatio`): the song keeps its place, the next notes start on the tracks' `lowhp=` instruments, tracks marked `only=lowhp` join in (an alarm figure of two pips a beat) and tracks marked `unless=lowhp` fall silent (a pad), and the tempo rises by the song's `lowhptempo`. That is Black and White's way, chosen over Platinum's looping beep (2026-10-05): the music itself turns agitated. `AudioManager.LowHp` sets it; the engine sets it every frame of a battle from `BattleEngine.PlayerInDanger` and clears it when the battle is decided, so the victory theme plays calm.

A battle theme has a **giant arrangement** too (plan Y · YD5), switched the same way while a Dynamaxed Pokémon stands on the field, on either side (`BattleEngine.GiantStands`, read from the animator's views as the lines show them, never from the rules): tracks marked `only=giant` join in, tracks marked `unless=giant` fall silent, the next notes start on the tracks' `giant=` instruments and the tempo changes by `gianttempo` (1 unless written). `AudioManager.Giant` and `AudioMixer.Giant` set it; the engine sets it beside `LowHp` every frame of a battle and clears it with `LowHp`. The two are independent: a giant in the red takes both, its layer and the alarm, and both tempos multiply; an instrument of the low-HP arrangement wins over the giant's. Every battle theme (each region's battle roles, the tournament's and every legendary's own) ends in a layer of up to three tracks, each doubling a track of the theme's own (below): a choir on the chords (on the tune where a theme has no chords), brass or horns on the bass's roots and timpani on the bass's beats, each in the octave and on the beats where it adds no clash to the arrangement; where none is free of one the part is left out (Giratina's brass and timpani, Ting-Lu's brass). Out of the giant arrangement a theme plays exactly as it did before the layer was written, sample for sample (`MusicTests.TestABattleThemePlaysAsItDidWithNoGiant`). The Battle Scene off changes nothing of it: the layer is state of the giant standing.

**Which theme plays when** (`MusicDirector`, plan 05 · A2): a place's own theme by day or by night; the surf theme while the player rides over the water and the bicycle's while they cycle, kept across towns and routes (`GameEngine.PlayFieldMusic`); a trainer's eye theme by their class from the moment they spot the player until the battle theme cuts in (and the place's own again if their script brings no battle); the battle theme by the most important opponent and its victory theme after, chosen as the original's battle controller chooses it (`MusicDirector.VictoryRole`: the gym leaders', the Elite Four's, the Champion's, Team Galactic's, grunts and commanders alike, and their boss's have their own; the rival's is a trainer's); fanfares that pause the music. What a trainer class brings is `Data/audio/sound-map.json` (`Data/SoundMap.cs`): its eye theme, following the original's table (`src/field_bgm.c`) by our names for the classes, and a battle theme of its own for the few that have one (Gym Leader, Rival, Team Galactic, the Elite Four, the Champion). A class not listed gets the boy's eye theme and the trainer battle theme.

## Loudness

Every bus's content is held to a window of loudness (`Audio/Loudness`, plan 05 · A7's mixing pass), measured the same way for all of it: as the mixer plays it at its own volume, centred, the mean power of 100 ms blocks of the two sides' average, leaving out blocks under −50 dBFS (a fanfare's rests, the silence after a sound).

| Bus | Window | What sits there |
|---|---|---|
| Music | −20.5 to −15 dB | Area themes and scenes nearer the bottom (the cave, the mine, −19 to −20), battles nearer the top (−15 to −17) |
| Fanfares | −20.5 to −14.5 dB | |
| Sound effects | −38 to −20 dB | A menu's tick at the bottom, a blow in battle at the top: short sounds sound louder than they measure |
| Cries | −27 to −17 dB | |
| Ambience | −30 to −26 dB | Every bed at full gain is about −28 dB, ten under the music; a layer's gain only says how near or how strong it is |

`MusicTests` (every song's first ten seconds), `SoundTests` (every sound), `AmbienceTests` (every bed, a sample of cries) hold them there, and `MusicRender` prints each one's loudness and flags any outside its window (`QUIET by …`, `LOUD by …`).

## The handheld speakers

An option (Speakers: Stereo or Handheld, `GameSettings.Speakers`, `AudioMixer.Handheld`), for a closer feel to the DS's own small speakers: the two sides are drawn together to 30% of their difference, nothing much under 350 Hz or over 6.5 kHz passes (a first-order high-pass and a second-order low-pass), and after the limiter the output is rounded to ten bits with a step of triangular dither. The filters are set for the mix's rate, 48 kHz since plan 31 · MU1, so their corners are where they were. `MusicRender --handheld` renders songs, sounds and beds that way.

## The mix's rate (plan 31 · MU1)

The mixer runs at **48,000 Hz** (`AudioMixer.Rate`), the rate raylib's device runs at, so the audio stream is opened at it and raylib passes the mix through untouched (before MU1 the stream was 32,768 Hz and miniaudio, under raylib, resampled it with its own linear resampler). Everything in the DS sound keeps its own rate, `Synthesizer.DsRate`, 32,768 Hz: the DS songs and fanfares, and every sound effect, cry and ambience bed, whose numbers are all written at it. So the mixer has two stages:

- **The DS stage**, at 32,768 Hz, is the mixer as it always was: the music, the fanfare, the sounds and the beds, with their fades, the cry's duck and the bus volumes. It is always mixed 256 frames at a time, however many frames the device asks for, so a song's notes start on the very samples they did before; a command (a sound, a song) reaches it at most a chunk and the resampler's look-ahead later, about 10 ms.
- **The resampler** (`Audio/Resampler`) takes that one stream into 48 kHz. The two rates are 256 into 375, so the output's place between two input samples cycles through 375 exact phases and never drifts; each phase is a Kaiser-windowed sinc of 128 input taps (β 11, half down at 16.2 kHz), its taps adding up to exactly one. The output is not delayed: sample *k* is the input at *k* · 256/375, which takes 64 input samples of look-ahead.
- **The last stage**, at 48 kHz: the handheld's speakers, mono, the volume, the glide behind another window, the 30 Hz high-pass and the limiter.

`new AudioMixer(Synthesizer.DsRate)` is the path before MU1: it resamples nothing and renders the very samples the old mixer did, which `MixRateTests` holds to hashes measured before the change. The modern sound (MU2) renders at 48 kHz and joins the mix after the resampler ("The modern sound in the mixer", below).

What MU1 measured, on the Windows machine (`MusicRender <out dir> --mix` prints all of it):

| | |
|---|---|
| Response, 20 Hz to 15 kHz | flat within 0.0001 dB; −0.02 dB at 15.5 kHz, −2.3 dB at 16 kHz (the DS sound has nothing over 16.4 kHz) |
| Images and THD+N, sines of −6 dBFS, 20 Hz to 15.5 kHz | −112 dB or better (−126 dB to 14 kHz) |
| The resampler's cost | 0.57 ms of CPU a second of stereo out |
| A busy scene (a battle theme in the red, eight sounds, two cries, six beds) | 5.3 ms of CPU a second before, 5.9 ms after: 0.6% of a core |
| A song from its first sample, against the DS-rate mix taken to 48 kHz | −60 dB: the 30 Hz high-pass's own shape at the two rates, 0.008 dB of level; under 15 kHz the same against an ideal 1,024-tap resampling |

`MixRateTests` holds the resampler to it (within 0.025 dB and under −105 dB to 15.5 kHz, exact phases, silence and an offset untouched, the same samples however the output is asked for) and the mix at 48 kHz to the DS-rate mix (each scene's loudness within 0.1 dB, a song nulling under −55 dB). Whatever renders through the mixer counts its frames at `AudioMixer.Rate` and measures with `Loudness.Of(samples, channels, AudioMixer.Rate)`; a sound, a cry, a bed or a DS player's song is at `Synthesizer.DsRate`, which `Loudness.Of`'s default rate is.

Its third value (plan 08 · P1, Platinum's MONO; `AudioMixer.Mono`, `AudioManager.Mono`) plays everything in one channel: after the mix (and the handheld's speakers, were both set) each pair of samples becomes their middle, so a sound panned to one side is heard in both ears, and one in the middle is as it was.

## Folders and roles

```
Data/music/
  common/      title, intro, pokecenter, pokemart, rival, evolution, surf, bicycle,
               eye_boy, eye_girl, eye_kid, eye_lady, eye_rich, eye_mountain, eye_fighter, eye_sport, eye_fun,
               eye_mystery, eye_sailor, eye_galactic, eye_ace, eye_elite_four, eye_champion,
               battle_wild, battle_trainer, battle_gym, battle_rival, battle_galactic, battle_galactic_boss,
               battle_elite_four, battle_champion, battle_legendary,
               victory_wild, victory_trainer, victory_gym, victory_galactic, victory_galactic_boss,
               victory_elite_four, victory_champion,
               fanfare_heal, fanfare_item, fanfare_levelup, fanfare_pokemon, fanfare_evolution,
               fanfare_badge, fanfare_tm, fanfare_keyitem
    legendary/ articuno, zapdos, … dialga, palkia, giratina, … pecharunt: a battle theme of its own for each of
               the 94 legendary and mythical species, by its name in lower case (ho_oh, type_null, tapu_koko)
  kanto/       pallet, battle_wild
  sinnoh/      twinleaf, route201 (and Route 219, Verity Lakefront), route202, route203 (Routes 203, 204, 218 and
               Iron Island), route205 (Route 205, the Valley Windworks, the Fuego Ironworks), sandgem, jubilife,
               oreburgh, floaroma (and the meadow), lake (Lakes Verity and Valor), lab, cave (Oreburgh Gate, the Ravaged
               Path, Wayward Cave, Mt. Coronet), mine (Oreburgh Mine, Iron Island's tunnels, the Ruin Maniac's cave),
               route206 (Routes 206 to 208, 220 and 221), route209 (and Route 212, the Trophy Garden), eterna,
               hearthome, solaceon, forest (Eterna Forest, the Lost Tower, the Solaceon Ruins), amity_square,
               route210 (Routes 210, 214 and 215, 223), celestic, veilstone, valor (Valor Lakefront, Route 213), pastoria,
               great_marsh, canalave, mt_coronet (its upper floors and faces), spear_pillar, route216 (Routes 216 and
               217, Acuity Lakefront), snowpoint, sunyshore, victory_road, pokemon_league
  common/      ... gym (inside every Gym, plan 02 · S5)
```

A region gets its own version of a shared theme by adding a file with the role's name to its folder (`johto/battle_wild.mml`). A wild legendary or mythical Pokémon brings a battle theme of its own (`MusicDirector.WildBattleTheme`, played by `AudioManager.PlayWildBattleMusic`): `legendary/<key>` in the region's folder, then in `common`, then the legendary battle theme; its forms share it (Giratina's Origin Forme, Kyurem's fusions), and the victory after it is the wild one, as in the original. Families share a motif, each member its own tune round it: the birds a wingbeat, the beasts a gallop, the Regis a tapped code in bare fifths, the lake guardians a three-note figure, the Swords of Justice a march, the Forces of Nature a whirling figure, the Tao trio a tolling bell, the Tapus a drum pattern, and so on. Role file names are in `MusicDirector.FileName`, and a role without a file falls back to a more general one (`MusicDirector.Fallback`): every eye theme to `eye_boy` (the lady's and the rich one's through `eye_girl`), the Galactic, rival and gym battles to `battle_trainer`, the Elite Four's and the Champion's to `battle_gym`, a legendary's to `battle_wild`, the gym's, the Elite Four's and the Champion's victories through one another to `victory_trainer`, and the Galactic boss's victory through the grunts'. Since A6 every role has its own file; the fallbacks are kept for a region that brings only some of its own. The introduction plays over the professor's welcome (`MusicRole.Introduction`), the evolution theme over the evolution scene and its fanfare at the end (the field's music comes back after, `GameEngine.FinishEvolution`), the rival's when a script calls it (Barry in Twinleaf Town), and an item is received to the fanfare of its kind (`ScriptRunner.FanfareFor`: a TM's, a key item's, or the item's), a badge to the badge's. `MusicTests` checks that every map's song exists, that every role resolves in every region, and that every song renders without clipping.

## The format

A song is a header, then one block per track. Track bodies are indented.

```
# Twinleaf Town: a comment
title Twinleaf Town
tempo 88            # quarter notes per minute
meter 4/4           # for the bar checks; 6/8 and 3/4 work too
reverb 0.32         # how much reverb returns to the mix
nighttempo 0.93     # the night arrangement plays this much slower (or "night none": one arrangement, day and night)
lowhptempo 1.08     # a battle theme: the low-HP arrangement plays this much faster
gianttempo 1.0      # a battle theme: the giant arrangement plays this much faster (1 unless written)

track melody flute night=ocarina vol=0.85 pan=0 rev=0.35
  r1 | r1 | L o5 a4. g8 f4 c4 | d4 e8 f8 g2 |

track drums kit vol=0.5
  r1 | r1 | L [ b4 r4 b4 r4 | ]16
```

Two header words belong to the **modern sound** (plan 31; below): `sound modern` (or `sound ds`) names the sound the song is written for, and `room <kind> <seconds> [predelay=<seconds>] [damp=<0 to 1>]` its reverb: a `room`, `chamber`, `hall` or `plate`, how long it rings before falling 60 dB, and, if not its kind's own, the gap before it answers and how quickly its highs die (`room hall 1.8`). The DS sound ignores `room` and keeps its one `reverb`; in the modern sound `reverb` is how much of the room returns. A song's `sound` wins over its region's (below, "Which sound a song plays in"); no song of `Data/music` names a sound in its header yet, and Kalos's play in the modern one by their region's file.

Track options: `width=<0 to 1>` (the modern sound: how far the track's unison voices spread across the field; 1 unless written), `night=<instrument>`, `lowhp=<instrument>` (a battle theme in the red), `giant=<instrument>` (a battle theme while a giant stands), `only=lowhp`, `unless=lowhp`, `only=giant` or `unless=giant` (a track of one arrangement alone), `vol` (0–1), `pan` (−1 left to 1 right), `rev` (reverb send). A track may also **double** another (`double=<track>`, a track above it and of the same kind, pitched or drums): it has no body of its own and plays the other's notes, moved by `shift=<semitones>` and, with `beats=<n>`, only those that start on every 1/*n* of a whole note (`beats=4` the beats, `beats=1` the first of each bar in 4/4); it loops where its source does. The giant layers are written so: `track gianttimpani timpani only=giant double=bass beats=4 shift=12 vol=0.5 rev=0.2`.

| Write | Means |
| --- | --- |
| `c d e f g a b` | A note; `+` or `#` sharpens, `-` flattens (`f+`, `b-`) |
| `4`, `8.`, `12` | Length after a note or rest: 4 is a quarter, a dot adds half, 12 is a triplet eighth. Without a number, the `l` length |
| `r` | A rest |
| `o5`, `>`, `<` | Set the octave (`o4 c` is middle C), or move it up or down one |
| `l8` | The default length |
| `v100` | Velocity, 0–127 |
| `q7` | How much of each note sounds, in eighths (8 is legato, 4 staccato; default 7) |
| `c4^16`, `c4&c16` | Tie: one note lasting both lengths |
| `(c e g)2` | A chord; octave moves inside it are local to it |
| `[ … ]3` | Repeat three times (twice without a number); repeats nest |
| `L` | The loop point. Every track that has one must put it at the same place; a song without one plays once (fanfares) |
| `\|` | A bar line. The parser checks that a bar really ends there, which catches most miscounted rhythms |

In the drum kit the letters are drums: `b` kick, `s` snare, `h` closed hi-hat, `o` open hi-hat, `c` crash, `t` `m` `f` high, mid and low toms, `x` clap, `k` shaker, and the hand percussion of plan 31: `g` high conga, `d` low conga, `n` bongo, `w` wood block. Lengths, `v`, `[ ]`, `( )`, `L` and `|` work as above.

Bass instruments (`bass`, `synthbass`, `tuba`, `contrabass`, `timpani`) sound an octave lower than written, `piccolo` and `glockenspiel` an octave higher. All tracks of a song must be the same length.

## Instruments

| Family | Instruments |
| --- | --- |
| Woodwinds | `flute`, `ocarina`, `piccolo`, `clarinet`, `oboe`, `accordion` |
| Brass | `trumpet`, `brass` (a section), `horn`, `tuba` |
| Strings | `strings` (a section), `violin`, `pizzicato`, `contrabass`, `harp`, `guitar` |
| Keys and mallets | `piano`, `epiano`, `organ`, `bell`, `glockenspiel`, `celesta`, `marimba`, `vibraphone`, `timpani` |
| Synths | `square`, `pulse`, `sawlead`, `pad`, `choir`, `bass`, `synthbass` |
| Drums | `kit` |
| Alola (plan 31 · MU0) | `ukulele` and `slideguitar` (plucked strings), `steeldrum` and `logdrum` (struck modes; the log drum sounds an octave lower than written), `warmpad` (a slow pad of four detuned voices spread wide) |
| Kalos (plan 31 · MU3) | `musette` (three reeds a few cents apart, the accordion fuller and wider), `harpsichord` and `nylonguitar` (plucked strings: a quill near the string's end, and a thumb on nylon), `cello` (one bowed voice, darker than the violin), `orchestra` (a string orchestra of five voices spread wide, slower to speak than `strings`) |

The Alola and Kalos instruments are open to any song and play in both sounds, but are written for the modern one. They are voices of two new kinds (`VoiceKind` in `Audio/Instrument.cs`): a **plucked string** is Karplus–Strong, a burst of noise as bright as the instrument and the note's velocity make it, hollowed by where the pluck lands, travelling round a loop that dulls it a little each time and reads the string at a fractional length, so a note can slide into place (the slide guitar's glide up, its vibrato); **struck modes** are a few decaying sines at the instrument's own ratios, the upper ones woken more by a harder strike, with a burst of noise for the stick or the mallet. Neither is a sample.

## The modern sound (plan 31)

The later regions' music is to sound like its own era rather than the DS's. [Plan 31](plans/31-later-music.md) adds a second sound to the synthesiser, chosen by data, beside the DS one, which renders every existing song, sound and cry to the sample as before (`ModernMusicTests` holds a sample of songs to hashes measured before the plan).

- **Its own rate and voices**: `new Synthesizer(SynthSound.Modern)` runs at 48,000 Hz (`Synthesizer.ModernRate`) with 96 voices (`ModernVoices`, plan 31's decision 2, to be measured on the Windows machine); `new Synthesizer()` is the DS one, 32,768 Hz and 40 voices. `SongPlayer(sound)` plays a song in either, and its `Rate`, `PeakVoices` and `VoicesStolen` (notes that cut a sounding voice short) say what it needed.
- **The top end**: its wavetables keep harmonics up to 20 kHz (the DS sound's stop at 14 kHz), and the drum kit's filters are moved to the rate so a drum keeps its colour.
- **Width**: a unison instrument plays `Unison` detuned voices (2 when not given), spread across the field by its `Spread` and the track's `width`; `strings`, `choir` and `warmpad` play three or four. Every voice sends to the reverb in stereo, from where it stands.
- **Velocity reaches tone**: a harder note keeps its bright spectrum longer and drives its FM deeper; in the DS sound velocity is level alone.
- **The room**: a stereo reverb with a pre-delay, early reflections tapped from it (none in a plate), eight damped combs and four all-passes a side whose feedback is set from the room's length and whose spacing grows with its size, the send high-passed at 140 Hz so the bass stays dry. At night the room rings 15% longer.

**Voices** (decision 2, measured in MU1 on the Windows machine): the modern synthesiser with every voice sounding costs from 0.03% of a core a voice (`ukulele`) to 0.15% (`steeldrum`), so its 96 take 3% to 15% of a core at the most, and half a core would hold over 300 of the dearest; MU0's tune needs at most 37 at once. 96 stays.

### Which sound a song plays in (plan 31 · MU2)

- **A region's file** may name its sound: `"sound": "modern"` in `Data/regions/<id>.json` (`RegionFile.Sound`); left out, or `"ds"`, is today's DS sound. Every song in that region's music folder (`music/<folder>/`) plays in it.
- **A song's header** may name its own (`sound modern`, `sound ds`), which wins over its region's: how a legendary's theme in `common/legendary` joins the region it belongs to, or an old song of a modern region keeps the DS sound. A song of `common` that names none is the DS sound.
- `Song.Sound` is what the header says, `Song.RegionSound` what its folder's region says (set as `MusicLibrary` reads the songs, `Mml.Parse(id, text, regionSound)`), and `Song.PlaysIn` the one that wins. No song of the game names a sound in its header yet; Kalos's region file says `"sound": "modern"` since MU3, so `music/kalos/` is the modern sound and every other folder the DS one.

### The modern sound in the mixer

The mixer keeps a player of each sound for the music and for the fanfares. A DS song plays in the DS stage exactly as before (`MixRateTests`' hashes); a modern song plays on the modern player at 48 kHz and goes straight into the last stage, beside the resampled DS stage and never through the resampler. The two are kept in step a DS chunk at a time: each 256 frames of the DS stage are 375 frames at 48 kHz, exactly, so for every chunk the DS stage mixes, the modern players render its 375 frames into a queue with that chunk's fade, cry duck and bus volume (eased across the chunk rather than stepped), and the queue is taken out one frame for one frame of the resampled DS stage. Everything the mixer does to the music is therefore the same in both sounds: a new song fades the old one out first (or cuts in, for a battle), the fanfare pauses the music and lets it back, a cry dips it, the arrangements switch.

- **From one sound to the other**: a song of the other sound waits for the old one to fade out (or cuts in), as any new song does; the old player is stopped once it is silent and the new one starts. A battle theme of one sound over a field theme of the other is no different from two songs of one sound.
- **The arrangements**: night is chosen as a song starts, the low-HP arrangement and the giant layer switch in the middle of a song, on whichever player has it (`AudioMixer.LowHp`, `Giant` set both).
- **The last stage**: the handheld's speakers, mono, the volume, the glide behind another window, the 30 Hz high-pass and the limiter act on the sum of the two, at 48 kHz, so both sounds are taken through them alike.
- A mixer made at the DS's rate (`new AudioMixer(Synthesizer.DsRate)`, the path before MU1) has no modern player: a modern song plays there in the DS sound.

### The compressor

A modern song's own mix goes through a gentle compressor before anything of the mixer touches it (`Audio/Compressor`, in `SongPlayer` for the modern sound alone; the DS sound has none), so a full arrangement holds together inside the music's loudness window without the limiter at the end doing the work:

| | |
|---|---|
| Detector | stereo-linked: the larger of the two sides' power, followed by a one-pole of 10 ms rising and 200 ms falling, so it hears the mix's body and not each transient |
| Threshold | −10 dBFS of that power (a song's body sits under it; its loudest bars reach it) |
| Ratio | 2 : 1, over a soft knee 8 dB wide (from −14 to −6 dBFS) |
| Most it takes | 6 dB |
| Make-up | +0.5 dB, about what it takes on average, so a song measures as loud as it did without |
| Gain | worked out once every 32 samples (the player's block) and eased across the next, so it costs about one logarithm a block and never steps |

It holds no randomness and no clock: a song renders to the same samples every time. On MU0's island route it takes 0.3 to 0.4 dB on average and 1.4 to 1.7 dB at the most (day, night, low HP), and the route measures −19.2 dB by day as the DS sound's does. `ModernMusicTests` and `ModernMixTests` hold the modern songs to the music bus's window with it, and `MusicRender --sound modern` prints how hard it worked.

The songs written for the modern sound are Kalos's (`Data/music/kalos/`, plan 31 · MU3) and MU0's test tune, `tools/MusicRender/songs/mu0_island_route.mml`, which is no song of the game. `MusicRender --sound modern` renders a song in the modern sound with no mixer (`Audio/OfflineMaster`: the song at the music bus's full volume and the mixer's own last stage, its 30 Hz high-pass and its limiter, at the sound's rate); without `--sound`, a song plays through the game's mixer in the sound it plays in there. Two modes play songs through the game's mixer as the game changes them (`tools/MusicRender/Handover.cs`), a song named by its id or its `.mml` file, with `@modern` or `@ds` after it to play it in that sound for the render (`common/battle_wild@modern`: a header line read before its own):

```bash
dotnet run --project tools/MusicRender -- <out dir> --handover <first> <second> [--at 20] [--cut] [--seconds 40] [--night]
dotnet run --project tools/MusicRender -- <out dir> --switch <song> [--lowhp 16-30] [--giant 16-30] [--night-at 40] [--seconds 60]
```

`--handover` asks for the second song at `--at` seconds, faded across as a new area's theme is or cut in as a battle's (`--cut`); `--switch` turns the low-HP arrangement or the giant layer on and off at those seconds and asks for the night arrangement at `--night-at`. Each prints the loudness on either side of every change and the largest step between two samples at each change beside the largest anywhere else (`CLICKS` and an exit of 2 where it is over by half again).

## Rendering and checking

Claude can't listen, so songs are checked by numbers and the user listens.

```bash
dotnet run --project tools/MusicRender -- <out dir> [song id, folder or .mml file ...] [--night] [--lowhp] [--giant] [--stems] [--passes N] [--spectrogram] [--wide] [--sound ds|modern]
dotnet run --project tools/MusicRender -- --calibrate [--sound modern]
dotnet run --project tools/MusicRender -- <out dir> --mix
```

Without `--sound`, a song plays through the game's mixer and is written at the mix's 48 kHz. `--mix` (plan 31 · MU1) measures the resampler and the audio thread's cost and writes each bus before and after the mix at 48 kHz (above, "The mix's rate"). `--sound ds` or `--sound modern` renders the songs in that sound with no mixer (above), at its own rate, the files named `<song>_<sound>`, and adds a line of the rate, the most voices sounding at once and how many notes cut one short; `--wide` draws the spectrogram to 16 kHz, where the two sounds' top ends differ. Every looping song's report also gives its **join**: the largest step between two samples within 3 ms of the place it wraps, beside the same where the loop starts the first time through; a join more than half as large again (and 0.02 over) is told as `CLICKS` and the tool exits with 2.

`MusicRender` writes a WAV per song (the intro, the loop twice and a fade) and prints its length, loop length, peak and RMS level, CPU cost, and any **clashes**: two parts a minor second or minor ninth apart on a beat, which is usually a wrong note, among the tracks of the arrangement rendered (with `--lowhp`, the alarm and not the pad it replaces). A note too short to be checked itself, such as an alarm's pip, is still checked against whatever is struck with it. A song named by its `.mml` file is read from there, so one being written is checked without building the game. `--night`, `--lowhp` and `--giant` render the arrangements (`--giant --lowhp` the two together; with `--giant` the clashes are told among the giant layer's tracks too). `--stems` adds each track's level, for balancing the mix. `--spectrogram` also writes a picture of each song: time left to right, 0 to 8 kHz bottom to top, black through blue, magenta and orange to white over 80 dB, which the Read tool can look at (a quiet intro, the beat, the loop's seam and a part that is too loud all show).

```bash
dotnet run --project tools/MusicRender -- <out dir> --sounds [name ...]
```

`--sounds` renders every sound effect (or the ones named) through the mixer as the game plays it, a WAV and a spectrogram each (`sound_<name>`), and prints its length, peak and RMS level, the frequency with the most energy, its first and last sample (a sound that doesn't start and stop at nothing clicks, and the tool exits with 2) and its DC offset. `SoundTests` holds the same for every sound of the bank, and that the buses mix as they should.
