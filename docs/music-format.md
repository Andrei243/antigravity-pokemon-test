# Music

The game's music is original, written for this game in the style of the DS soundtracks: a small sampled-sounding orchestra (flutes and reeds, brass, strings, piano, bells and mallets, a few synth leads, bass and drums) and short loops. No melody, recording or sequence is taken from the games. Songs are text files in `PokemonPlatinumEngine/Data/music/`, synthesised in code at run time like everything else, so there are no audio files in the repository.

## How it plays

- **`Audio/Synthesizer`**: up to 40 voices at the DS's 32,768 Hz output rate. Each voice is a band-limited wavetable that crossfades from a bright spectrum to a mellow one, with optional FM, breath noise, vibrato, a glide into the note, detuned unison and an ADSR envelope; the drum kit is synthesised separately. A Freeverb-style reverb is shared by all voices.
- **`Audio/Instrument`**: the instrument bank. Gains are balanced so every instrument plays a middle-C phrase at about the same loudness (`MusicRender --calibrate` prints the levels).
- **`Audio/SongPlayer`**: plays one song, starting notes on a 1 ms grid, wrapping at the loop point, and applying the night arrangement.
- **`Audio/AudioMixer`**: what the game hears, on five buses (`AudioBus`: music, fanfares, sound effects, cries, ambience), each with its own volume. A new song fades the old one out first (or cuts in, for battles); the same song carries on without restarting; a fanfare pauses the music, plays once and lets the music come back where it stopped; sound effects play over the music, up to eight at once (a further one takes the place of the one nearest its end), panned; the music dips to 45% under anything on the cry bus and comes back once it is over; the field's ambience beds loop on their own bus, each gliding to the gain and pan it is set to (`SetAmbience`, [`sound-effects.md`](sound-effects.md), "Ambience"). It ends with a 30 Hz high-pass and a soft limiter, and, with the options' Speakers set to Handheld, the handheld's own speakers (below).
- **`Audio/SoundBank`**: the sound effects (plan 05 · A3), synthesised in code the first time each is asked for with the layers of `Audio/SoundDesign` (band-limited oscillators with pitch glides, FM bells, noise through a gliding resonant filter, clicks, bubbles, an echo), brought to their peak level and given rounded ends so they never click. `SoundBank.Entries` lists each with its group, the original's sound it stands for and when it plays; [`sound-effects.md`](sound-effects.md) is that list. `SoundBank.Names` is what a script's `sound` may name.
- **`Audio/Cries`**: every species' and form's cry (plan 05 · A4), synthesised from its size, name, types and family, in the original's modes (pinch, faint, half, field echo), played on the cry bus with `AudioManager.PlayCry`; the "Cries" section of [`sound-effects.md`](sound-effects.md).
- **`Audio/MusicDirector`**: decides what plays, with no audio device. Maps name their theme in `bgmTrack`. Roles (title, battles, victories, fanfares) look in the current region's folder first, then `common`, then a more general role (`battle_gym` falls back to `battle_trainer`).
- **`Core/AudioManager`**: opens a raylib audio stream whose callback runs the mixer on raylib's audio thread, so music keeps playing while the game thread loads a map. `PlayMusic(id)`, `PlayMusic(role)`, `PlayFanfare(role)`, `PlaySound(name, pan)`, `PlayCry`, `SetAmbience(layers)`, `Region`, `Handheld`. The options' four volumes (the music, the sound effects, the cries and the ambience, 0 to 100) reach the buses through `SetVolumes`; the fanfares follow the music's, since a fanfare is the music's own pause.

At night (`TimeOfDay.Night` and `LateNight`) area themes play their night arrangement: each track may name a softer `night=` instrument, the tempo drops by the song's `nighttempo`, drums are quieter and the reverb a little wetter. The arrangement is chosen when the song starts, as Platinum does on entering an area. A song whose header says `night none` has one arrangement for day and night alike, as the original's caves and dungeons do (their map headers give the same `SEQ_D_*` for day and night): the cave and the mine play the same at dusk, and carry on across it without a restart.

A battle theme has a **low-HP arrangement** as well, switched on and off in the middle of the song while a Pokémon of the player's is in the red (its bar's colour, `BattleAnimator.LowHpRatio`): the song keeps its place, the next notes start on the tracks' `lowhp=` instruments, tracks marked `only=lowhp` join in (an alarm figure of two pips a beat) and tracks marked `unless=lowhp` fall silent (a pad), and the tempo rises by the song's `lowhptempo`. That is Black and White's way, chosen over Platinum's looping beep (2026-10-05): the music itself turns agitated. `AudioManager.LowHp` sets it; the engine sets it every frame of a battle from `BattleEngine.PlayerInDanger` and clears it when the battle is decided, so the victory theme plays calm.

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

An option (Speakers: Stereo or Handheld, `GameSettings.Speakers`, `AudioMixer.Handheld`), for a closer feel to the DS's own small speakers: the two sides are drawn together to 30% of their difference, nothing much under 350 Hz or over 6.5 kHz passes (a first-order high-pass and a second-order low-pass), and after the limiter the output is rounded to ten bits with a step of triangular dither. The output was at the DS's own rate already. `MusicRender --handheld` renders songs, sounds and beds that way.

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

track melody flute night=ocarina vol=0.85 pan=0 rev=0.35
  r1 | r1 | L o5 a4. g8 f4 c4 | d4 e8 f8 g2 |

track drums kit vol=0.5
  r1 | r1 | L [ b4 r4 b4 r4 | ]16
```

Track options: `night=<instrument>`, `lowhp=<instrument>` (a battle theme in the red), `only=lowhp` or `unless=lowhp` (a track of one arrangement alone), `vol` (0–1), `pan` (−1 left to 1 right), `rev` (reverb send).

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
dotnet run --project tools/MusicRender -- <out dir> [song id, folder or .mml file ...] [--night] [--lowhp] [--stems] [--passes N] [--spectrogram]
dotnet run --project tools/MusicRender -- --calibrate
```

`MusicRender` writes a WAV per song (the intro, the loop twice and a fade) and prints its length, loop length, peak and RMS level, CPU cost, and any **clashes**: two parts a minor second or minor ninth apart on a beat, which is usually a wrong note, among the tracks of the arrangement rendered (with `--lowhp`, the alarm and not the pad it replaces). A note too short to be checked itself, such as an alarm's pip, is still checked against whatever is struck with it. A song named by its `.mml` file is read from there, so one being written is checked without building the game. `--night` and `--lowhp` render the arrangements. `--stems` adds each track's level, for balancing the mix. `--spectrogram` also writes a picture of each song: time left to right, 0 to 8 kHz bottom to top, black through blue, magenta and orange to white over 80 dB, which the Read tool can look at (a quiet intro, the beat, the loop's seam and a part that is too loud all show).

```bash
dotnet run --project tools/MusicRender -- <out dir> --sounds [name ...]
```

`--sounds` renders every sound effect (or the ones named) through the mixer as the game plays it, a WAV and a spectrogram each (`sound_<name>`), and prints its length, peak and RMS level, the frequency with the most energy, its first and last sample (a sound that doesn't start and stop at nothing clicks, and the tool exits with 2) and its DC offset. `SoundTests` holds the same for every sound of the bank, and that the buses mix as they should.
