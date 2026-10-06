# Sound effects

Every sound effect of the game (plan 05 · A3), what it stands for in the original and when this game plays it. The sounds are our own: synthesised in code the first time each is asked for (`Audio/SoundBank`, built with the layers of `Audio/SoundDesign`: oscillators whose pitch glides, two-operator FM bells, noise through a resonant filter whose cutoff glides, scattered clicks and bubbles, an echo). Nothing is taken from the games; the second column names the original's sound effect (from the decompilation's `res/sound/pl_sound_data.json` and the code that plays it) only as the checklist of what needs a sound, and is empty where the original's name isn't known or there is none.

**How they are heard.** Call `AudioManager.PlaySound(name, pan)`; a script says `sound <name>`. Every sound is brought to its own peak (a menu's blip lowest, about −16 dBFS; a blow in battle highest, about −10 dBFS) and starts and ends at nothing, so the mixer's sound bus needs no balancing of its own; measured as the mixer plays it, each lies within the sound bus's loudness window (`Audio/Loudness`, −38 to −20 dB: [`music-format.md`](music-format.md), "Loudness"). In battle a sound comes from the side of the Pokémon it belongs to: the player's a little to the left, the foe's to the right (`BattleEngine.Sound`). In the field a sound made by someone comes from their side of the screen (`GameEngine.PanAt`, `FieldAmbience.PanOf`: a trainer's "!" or a script's surprise, eight tiles to the side leaning 0.8 of the way), and the player's own from the middle.

**Timing.** In battle a sound is heard when what it belongs to is seen: a move's sound as its line appears, the hit's as it lands (`HitDelay`), the send-out as the ball opens (`BattleAnimator.SendOutBallTime`), each of a thrown ball's wobbles as it begins (`BattleAnimator.BallWobbleAt`) and its click or burst as it settles. In the field a footstep's sound comes as the step begins, as the original's `player_move.c` plays it: in snow, a puddle, water ankle deep, shallow mud and **very** tall grass, and nowhere else (the original's ordinary tall grass and sand are silent). A door's sound comes as it begins to open (`FieldLife.OpenDoor` says so) and as it shuts behind someone who came out (`FieldLife.TakeShutting`); glass doors slide both ways. Going through any other warp is the original's footsteps on stairs, or a warp panel's own sound.

**Not played.** The original's looping low-HP beep: this game changes the battle music instead (plan 05 · A2). The HP bar is silent, as in the original. Sounds whose moment doesn't exist yet are made and checked already, and wait for it (the "waits for" entries below).

**Checking them.** `dotnet run --project tools/MusicRender -- <out dir> --sounds [name ...]` renders each as the mixer plays it, a WAV and a spectrogram, and reports its length, levels, loudest frequency, ends and offset. `SoundTests` holds every sound to the same, holds the set to the plan's checklist, finds every name the engine's code asks for in the bank, and hears a battle through `AudioManager.Listen` (a move, a hit of each strength, a thrown ball wobble by wobble, running away).

## Menus

| Sound | The original's | Played when |
|---|---|---|
| `cursor` | `SEQ_SE_DP_SELECT` | The cursor moves in a menu or a list. |
| `select` | `SEQ_SE_DP_DECIDE` | A choice is made. |
| `cancel` | — | A menu is backed out of, an answer is no. |
| `error` | `SEQ_SE_DP_BEEP` | A choice that can't be made: a move with no PP, a Pokémon that can't battle, an item that can't be used here, money short. |
| `menu_open` | `SEQ_SE_DP_WIN_OPEN` | The start menu opens. |
| `menu_close` | — | The start menu closes. |
| `page` | `SEQ_SE_DP_MEKURU` | A page turns: the bag's pockets, a Pokédex entry's pages, the PC's boxes. |
| `text` | — | The text box moves on to its next line. |
| `levelup` | — | A Pokédex completed, an evolution done: a bright arpeggio. |

## The field

| Sound | The original's | Played when |
|---|---|---|
| `bump` | `SEQ_SE_DP_WALL_HIT` | Walking into something. |
| `ledge` | `SEQ_SE_DP_DANSA4` | Hopping down a ledge. |
| `grass` | `SEQ_SE_DP_KUSA` | A step through very tall grass (the original makes no sound in ordinary tall grass). |
| `step_snow` | `SEQ_SE_PL_YUKI` | A step in snow. |
| `step_puddle` | `SEQ_SE_DP_FOOT3_0` | A step through a puddle. |
| `step_shallows` | `SEQ_SE_DP_FOOT3_1` | A step through ankle-deep water. |
| `step_mud` | `SEQ_SE_DP_MARSH_WALK` | A step in mud that isn't deep. |
| `door_open` | `SEQ_SE_DP_DOOR_OPEN` | A door opens as the player steps up to it. |
| `door_close` | `SEQ_SE_DP_DOOR_CLOSE2` | The door shuts behind the player who came out of it. |
| `door_slide` | `SEQ_SE_DP_DOOR10` | Glass doors slide open (a Pokémon Center, a Mart). |
| `stairs` | `SEQ_SE_DP_KAIDAN2` | Going up or down stairs, or through a way out that isn't a door. |
| `warp` | `SEQ_SE_DP_TELE2` | A warp panel. |
| `exclaim` | — | The "!" over a trainer who has seen the player, or anyone a script surprises. |
| `surf` | — | A Pokémon is ridden out onto the water. |
| `save` | `SEQ_SE_DP_SAVE` | The game is saved. |
| `pc_on` | `SEQ_SE_DP_PC_LOGIN` | A PC is switched on. |
| `pc_off` | `SEQ_SE_DP_PC_LOGOFF` | A PC is switched off. |
| `heal` | `SEQ_SE_DP_KAIFUKU` | A medicine used from the bag, HP restored in battle. |
| `bike_bell` | `SEQ_SE_DP_JITENSYA` | Getting on the Bicycle (waits for the Bicycle, plan 02 · S2). |
| `gear` | `SEQ_SE_DP_GEAR` | The Bicycle's gear changes (waits for the Bicycle, plan 02 · S2). |
| `boulder` | — | A boulder pushed with Strength (waits for plan 02 · S2). |
| `rock_smash` | — | A rock broken with Rock Smash (waits for plan 02 · S2). |
| `cut` | `SEQ_SE_DP_FW015` | A tree cut down with Cut (waits for plan 02 · S2). |
| `fish_cast` | — | A rod cast (waits for fishing). |
| `fish_bite` | `SEQ_SE_DP_FW104` | Something bites (waits for fishing). |
| `fish_reel` | — | The line reeled in (waits for fishing). |
| `poketch` | `SEQ_SE_DP_POKETCH_003` | A Pokétch button (waits for the Pokétch). |
| `thunder` | `SEQ_SE_DP_T_KAMI2` | Thunder cracking close, a fifth of a second after a storm's lightning (two strikes of three, as the original's storm chooses). |
| `thunder_rumble` | `SEQ_SE_DP_T_KAMI` | Thunder rolling from further off, a second after the lightning (one strike of three). |

## Battles

| Sound | The original's | Played when |
|---|---|---|
| `send_out` | `SEQ_SE_DP_BOWA4` | A ball opens and a Pokémon comes out. |
| `recall` | — | A Pokémon is called back into its ball. |
| `run_away` | `SEQ_SE_DP_NIGERU` | Getting away from a wild Pokémon. |
| `ball_throw` | `SEQ_SE_DP_NAGERU` | A ball thrown. |
| `ball_shake` | `SEQ_SE_DP_KON` | Each wobble of a thrown ball. |
| `ball_click` | `SEQ_SE_DP_GETTING` | The ball clicks shut: caught. |
| `ball_break` | — | The ball bursts open: the Pokémon broke free. |
| `hit_normal` | `SEQ_SE_DP_KOUKA_M` | A hit lands. |
| `hit_super` | `SEQ_SE_DP_KOUKA_H` | A super-effective hit lands. |
| `hit_weak` | `SEQ_SE_DP_KOUKA_L` | A not very effective hit lands. |
| `stat_up` | — | A stat rises. |
| `stat_down` | — | A stat falls. |
| `status_poison` | — | Poisoned, or badly poisoned. |
| `status_burn` | — | Burned. |
| `status_paralysis` | — | Paralysed. |
| `status_sleep` | — | Fallen asleep. |
| `status_freeze` | — | Frozen solid. |
| `status_confusion` | — | Confused, and each turn it is confused. |
| `faint` | `SEQ_SE_DP_POKE_DEAD3` | A Pokémon faints. |
| `exp` | `SEQ_SE_DP_EXP` | The EXP bar fills. |

## Moves

One sound for each type, played as a move of that type sets off (`SoundBank.MoveSound`), from the side of its user; the hit has its own sound as it lands (`hit_normal`, `hit_super`, `hit_weak`). A move aimed at several Pokémon is heard once.

`move_normal`, `move_fire`, `move_water`, `move_grass`, `move_electric`, `move_ice`, `move_fighting`, `move_poison`, `move_ground`, `move_flying`, `move_psychic`, `move_bug`, `move_rock`, `move_ghost`, `move_dragon`, `move_steel`, `move_dark`, `move_fairy`.

## Cries

Every species and every form has a cry of its own (plan 05 · A4), synthesised by `Audio/Cries` from its data alone and the same on every run; none of the games' cries is copied or imitated.

- **The voice** (`CryVoice.Of(species, form)`):
  - **Pitch** comes from size. A Pokémon of a few kilograms cries around 600–1000 Hz, a whale or a legendary under 200 Hz (1150 Hz × (weight + 1)^−0.26 × (height + 0.3)^−0.18, nudged by up to 12% by its own name). Legendary and mythical Pokémon are a fifth lower, longer, and ring with an echo.
  - **Length** grows with weight and with each evolution, from 0.35 to 1.45 seconds.
  - **Syllables** follow the vowel groups of the name, one to four: Pikachu cries three, Mew one or two. Each syllable has its vowel's two formants and a shape (rising, falling, an arch, a dip, a trill) chosen by the letters around it. The last syllable is drawn out.
  - **Timbre** comes from its types and is seeded by the first species of its line (`PokemonGenomes.Family`), so a line sounds related and deepens as it grows. The source is a mix of saw, pulse, sine and FM, with breath, a growl and a vibrato. Fire breathes and growls, Electric buzzes, Water gurgles, Steel rings, Ghost wavers with an echo, Bug whirs, Dragon roars.
  - **A form** takes its own size and types and tunes the voice by its name: Giratina's Origin Forme keeps Giratina's syllables at its own pitch.
- **Modes** (`CryMode`, after the original's `POKECRY_*` in `src/sound_playback.c`). The DS pitches a sample by playing it faster or slower, and so do we:

| Mode | What it does | Heard |
|---|---|---|
| `Normal` | As it is. | A wild Pokémon appearing; the party's summary; the starter's question; an evolution, before and after; Giratina at the title. |
| `Pinch` | 1.5 semitones lower. | Sent out with a status condition or with its HP bar not green (24 of its 48 pixels or fewer), unless its HP is full (`Cries.SendOutMode`). |
| `Faint` | 3.5 semitones lower. | As it faints, before its fall. |
| `Half`, `PinchHalf` | Twenty frames of sixty, the last ten fading. | Kept for the moments the original uses them. |
| `FieldEvent` | A semitone higher, with an echo a third of a semitone up beside it at half the volume. | A script's `cry`. |
| `Pokedex` | As it is. | An entry of the Pokédex opening. |

- **Playing one**: `AudioManager.PlayCry(pokemon or species, form, mode, pan)` plays it on the cry bus, which dips the music under it. A battle asks for its Pokémon's cries ahead (`AudioManager.RequestCries`), and they are made on a worker. The last 48 are kept (`Cries.Kept`); a cry takes about ten milliseconds to make.
- **Checking them**: `dotnet run --project tools/MusicRender -- <out dir> --cries [species or form ...] [--modes] [--all]` renders cries with their spectrograms (a sample of every size and type when no species is named; `--all` checks every species and form without writing files). `CryTests` makes every species' and form's cry and holds it clean and unlike any other.

## Ambience

The field's background sound (plan 05 · A7): loops ("beds") on the mixer's ambience bus, synthesised in code by `Audio/Ambience` like the sound effects, each made a second longer than it lasts and its last second crossfaded into its first, so it loops without a seam. Every bed is brought to the ambience bus's level (about ten dB under the music), so two can play under a song. What is heard where the player stands is `Overworld/FieldAmbience` (no drawing or audio device), worked out again on each step and whenever the weather changes; the mixer glides each bed to its gain and pan over 1.2 seconds (`AudioMixer.SetAmbience`), so walking up to a waterfall or out of the rain is smooth. A battle, an evolution, the title and the introduction silence it.

| Bed | The original's | Heard |
|---|---|---|
| `Rain` | `SEQ_SE_DP_T_AME` | Rain where the player stands. |
| `HeavyRain` | `SEQ_SE_DP_T_OOAME` | Heavy rain, or a thunderstorm (with `thunder` after its lightning). |
| `Hail` | — | Hail. |
| `Wind` | `SEQ_SE_DP_KAZE` | Snow: softly in light snow, harder in heavy snow. |
| `Blizzard` | `SEQ_SE_DP_KAZE2` | A blizzard: a gale with a whistle in it. |
| `Sandstorm` | — | A sandstorm. |
| `Waterfall` | — | A waterfall within nine tiles: louder the nearer, from its side of the screen. |
| `River` | — | Running water (river tiles) within reach. |
| `Waves` | `SEQ_SE_DP_NAMI` | The sea, or a lake: louder the more of the view is water, from its side. |
| `Cave` | — | In a cave (a map whose setting is a cave): a low rumble, and drips that ring round the walls. |

Clear skies, clouds, fog and ash make no sound, and a room none at all. Water is heard by its tiles' behaviour (`Waterfall`, `River`, `Sea`), each tile within reach counting less the further off it is; the original's `Sea` is also the still water of lakes and ponds, so a lake's shore laps more softly than the open sea only by being smaller. **Checking them**: `dotnet run --project tools/MusicRender -- <out dir> --ambience [bed ...] [--handheld]` renders each bed looped twice, with its spectrogram, its loudness against the bus's window and the size of its seam against its largest step; `AmbienceTests` holds the same, and the field's rules (the weather, the cave, a waterfall walked up to, the sea from the shore, thunder after each flash).

