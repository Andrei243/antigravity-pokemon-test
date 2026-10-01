# Music

The game's music is original, written for this game in the style of the DS soundtracks: a small sampled-sounding orchestra (flutes and reeds, brass, strings, piano, bells and mallets, a few synth leads, bass and drums) and short loops. No melody, recording or sequence is taken from the games. Songs are text files in `PokemonPlatinumEngine/Data/music/`, synthesised in code at run time like everything else, so there are no audio files in the repository.

## How it plays

- **`Audio/Synthesizer`**: up to 40 voices at the DS's 32,768 Hz output rate. Each voice is a band-limited wavetable that crossfades from a bright spectrum to a mellow one, with optional FM, breath noise, vibrato, a glide into the note, detuned unison and an ADSR envelope; the drum kit is synthesised separately. A Freeverb-style reverb is shared by all voices.
- **`Audio/Instrument`**: the instrument bank. Gains are balanced so every instrument plays a middle-C phrase at about the same loudness (`MusicRender --calibrate` prints the levels).
- **`Audio/SongPlayer`**: plays one song, starting notes on a 1 ms grid, wrapping at the loop point, and applying the night arrangement.
- **`Audio/MusicMixer`**: what the game hears. A new song fades the old one out first (or cuts in, for battles); the same song carries on without restarting; a fanfare pauses the music, plays once and lets the music come back where it stopped. It ends with a 30 Hz high-pass and a soft limiter.
- **`Audio/MusicDirector`**: decides what plays, with no audio device. Maps name their theme in `bgmTrack`. Roles (title, battles, victories, fanfares) look in the current region's folder first, then `common`, then a more general role (`battle_gym` falls back to `battle_trainer`).
- **`Core/AudioManager`**: opens a raylib audio stream whose callback runs the mixer on raylib's audio thread, so music keeps playing while the game thread loads a map. `PlayMusic(id)`, `PlayMusic(role)`, `PlayFanfare(role)`, `Region`.

At night (`TimeOfDay.Night` and `LateNight`) area themes play their night arrangement: each track may name a softer `night=` instrument, the tempo drops by the song's `nighttempo`, drums are quieter and the reverb a little wetter. The arrangement is chosen when the song starts, as Platinum does on entering an area.

## Folders and roles

```
Data/music/
  common/      title, pokecenter, battle_wild, battle_trainer, battle_gym, victory_wild, victory_trainer,
               fanfare_heal, fanfare_item, fanfare_levelup, fanfare_pokemon
  kanto/       pallet, battle_wild
  sinnoh/      twinleaf, route201, route202 (Routes 202–204), sandgem, jubilife, lake, lab
```

A region gets its own version of a shared theme by adding a file with the role's name to its folder (`johto/battle_wild.mml`). Role file names are in `MusicDirector.FileName`. `MusicTests` checks that every map's song exists, that every role resolves in every region, and that every song renders without clipping.

## The format

A song is a header, then one block per track. Track bodies are indented.

```
# Twinleaf Town: a comment
title Twinleaf Town
tempo 88            # quarter notes per minute
meter 4/4           # for the bar checks; 6/8 and 3/4 work too
reverb 0.32         # how much reverb returns to the mix
nighttempo 0.93     # the night arrangement plays this much slower

track melody flute night=ocarina vol=0.85 pan=0 rev=0.35
  r1 | r1 | L o5 a4. g8 f4 c4 | d4 e8 f8 g2 |

track drums kit vol=0.5
  r1 | r1 | L [ b4 r4 b4 r4 | ]16
```

Track options: `night=<instrument>`, `vol` (0–1), `pan` (−1 left to 1 right), `rev` (reverb send).

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

In the drum kit the letters are drums: `b` kick, `s` snare, `h` closed hi-hat, `o` open hi-hat, `c` crash, `t` `m` `f` high, mid and low toms, `x` clap, `k` shaker. Lengths, `v`, `[ ]`, `( )`, `L` and `|` work as above.

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

## Rendering and checking

Claude can't listen, so songs are checked by numbers and the user listens.

```bash
dotnet run --project tools/MusicRender -- <out dir> [song id or folder ...] [--night] [--stems] [--passes N]
dotnet run --project tools/MusicRender -- --calibrate
```

`MusicRender` writes a WAV per song (the intro, the loop twice and a fade) and prints its length, loop length, peak and RMS level, CPU cost, and any **clashes**: two parts a minor second or minor ninth apart on a beat, which is usually a wrong note. `--stems` adds each track's level, for balancing the mix. `ffmpeg -i song.wav -lavfi showspectrumpic=s=1400x600:fscale=log spec.png` draws a spectrogram.
