# Voice coverage

Written by `dotnet run --project tools/VoiceTool -- catalogue` (plan 26 · V1); `VoiceTests` fails when this is not what the files give. Do not edit by hand.

A **line** is one string a character says in a text box (a script's `say` and the question of an `ask` or `choose`, a person's `dialog`, `before` and `after`, the introduction's professor), under the key plan 17 · N4 gave it. **Spoken** lines have a spoken text (the line with the names the player types left out, which `Data/voice/lines.json` holds); **shown only** lines have none yet, because a placeholder cannot be left out gracefully, and show their text alone until one is written by hand. No audio exists yet: V3 to V5 make it, V2 plays it.

## Lines by kind

| Kind | Lines | Spoken | Shown only | With variants | Written by hand |
|---|---|---|---|---|---|
| The introduction (`intro:`) | 96 | 96 | 0 | 0 | 0 |
| People of the hand-made maps (`map:`) | 1354 | 1312 | 42 | 1 | 0 |
| Scripts (`script:`) | 10310 | 9846 | 464 | 17 | 0 |
| People of the world (`world:`) | 7653 | 7591 | 62 | 4 | 0 |
| **All** | 19413 | 18845 | 568 | 22 | 0 |

## Not voiced, by decision

Plan 26's decision 7: the game's own voice, a question's answers, signs and a Pokémon's cry.

| What | Lines |
|---|---|
| a Pokémon's cry | 250 |
| a question's answers | 780 |
| a sign | 1658 |
| the game's own voice (speaker none) | 283 |
| the game's own voice (text) | 1670 |

## The cast

- Named characters: 396 voices (359 provisional: added for a name nobody has cast yet, with nothing said about how they sound).
- Cloned from the user's samples: `rival`.
- Archetypes in use: 240, with 725 voices to cast (V5).
- Lines whose voice is the person spoken to, decided as the talk starts (`@self`): 1896.

## Lines by voice

The voices with twenty lines or more; archetypes are summed over their voices.

| Voice | Lines | Shown only |
|---|---|---|
| `@self` | 1896 | 138 |
| `look:gentleman (any)` | 732 | 15 |
| `rival` | 605 | 18 |
| `look:grunt (any)` | 587 | 0 |
| `look:lady (any)` | 442 | 1 |
| `look:youngster (any)` | 414 | 21 |
| `look:clerk (any)` | 399 | 10 |
| `look:lass (any)` | 340 | 11 |
| `look:attendant (any)` | 321 | 12 |
| `look:nurse (any)` | 261 | 0 |
| `look:gym_guide (any)` | 203 | 1 |
| `look:scientist (any)` | 201 | 4 |
| `class:ace_trainer (any)` | 160 | 0 |
| `class:fisherman (any)` | 160 | 0 |
| `look:receptionist (any)` | 159 | 19 |
| `class:hiker (any)` | 158 | 0 |
| `cynthia` | 157 | 1 |
| `look:sailor (any)` | 151 | 0 |
| `class:youngster (any)` | 148 | 0 |
| `rowan` | 147 | 12 |
| `look:old_man (any)` | 146 | 4 |
| `look:woman (any)` | 133 | 14 |
| `class:team_rocket (any)` | 132 | 0 |
| `class:lass (any)` | 128 | 0 |
| `class:picnicker (any)` | 128 | 0 |
| `class:swimmer_m (any)` | 126 | 0 |
| `class:black_belt (any)` | 120 | 0 |
| `look:man (any)` | 118 | 4 |
| `class:psychic (any)` | 116 | 0 |
| `mom` | 116 | 7 |
| `assistant` | 115 | 5 |
| `look:guard (any)` | 115 | 0 |
| `look:boy (any)` | 104 | 4 |
| `class:swimmer_f (any)` | 102 | 0 |
| `class:cooltrainer (any)` | 100 | 0 |
| `bianca` | 99 | 3 |
| `black_belt` | 99 | 14 |
| `looker` | 91 | 1 |
| `look:old_woman (any)` | 90 | 5 |
| `look:worker (any)` | 90 | 0 |
| `prof_oak` | 90 | 3 |
| `steven` | 89 | 1 |
| `class:bug_catcher (any)` | 88 | 0 |
| `look:hiker (any)` | 85 | 0 |
| `class:bird_keeper (any)` | 84 | 0 |
| `class:galactic (any)` | 84 | 0 |
| `class:twins (any)` | 84 | 0 |
| `cyrus` | 83 | 7 |
| `look:aqua_grunt (any)` | 83 | 3 |
| `look:fisherman (any)` | 80 | 4 |
| `class:camper (any)` | 78 | 0 |
| `ace_trainer` | 77 | 0 |
| `class:swimmer (any)` | 76 | 0 |
| `class:pokémon_ranger (any)` | 72 | 0 |
| `class:biker (any)` | 64 | 0 |
| `class:sailor (any)` | 64 | 0 |
| `prof_elm` | 63 | 1 |
| `class:worker (any)` | 62 | 0 |
| `prof_juniper` | 62 | 0 |
| `underground_man` | 62 | 0 |
| `lance` | 60 | 0 |
| `artist` | 59 | 1 |
| `beauty` | 59 | 4 |
| `class:battle_girl (any)` | 58 | 0 |
| `class:scientist (any)` | 58 | 0 |
| `class:veteran (any)` | 58 | 0 |
| `bill` | 53 | 2 |
| `cheren` | 53 | 1 |
| `archie` | 52 | 3 |
| `look:girl (any)` | 51 | 0 |
| `class:beauty (any)` | 50 | 0 |
| `class:young_couple (any)` | 50 | 0 |
| `elder` | 50 | 0 |
| `officer` | 50 | 5 |
| `depot_agent` | 49 | 3 |
| `clair` | 48 | 4 |
| `class:backpacker (any)` | 48 | 0 |
| `crasher_wake` | 46 | 1 |
| `drayden` | 46 | 0 |
| `maxie` | 46 | 1 |
| `scott` | 46 | 0 |
| `wallace` | 46 | 2 |
| `class:team_aqua (any)` | 44 | 0 |
| `class:team_magma (any)` | 44 | 0 |
| `jasmine` | 43 | 0 |
| `look:trainer (any)` | 43 | 0 |
| `mystery` | 43 | 4 |
| `backers` | 42 | 0 |
| `buck` | 42 | 1 |
| `class:bird_keeper_gs (any)` | 42 | 0 |
| `look:pokefan_f (any)` | 42 | 0 |
| `riley` | 42 | 0 |
| `flint` | 41 | 1 |
| `look:host (any)` | 40 | 16 |
| `volkner` | 40 | 1 |
| `collector` | 39 | 0 |
| `colress` | 39 | 1 |
| `look:pokefan_m (any)` | 39 | 0 |
| `owner` | 39 | 5 |
| `class:gentleman (any)` | 38 | 0 |
| `prof_birch` | 38 | 3 |
| `wally` | 38 | 0 |
| `baoba` | 37 | 0 |
| `school_kid` | 37 | 0 |
| `class:ninja_boy (any)` | 36 | 0 |
| `look:magma_grunt (any)` | 36 | 0 |
| `iris` | 35 | 0 |
| `kurt` | 35 | 6 |
| `class:ruin_maniac (any)` | 34 | 0 |
| `look:twin (any)` | 34 | 0 |
| `maylene` | 34 | 1 |
| `mr_briney` | 34 | 1 |
| `roark` | 33 | 0 |
| `rood` | 33 | 0 |
| `alder` | 32 | 1 |
| `class:ace_trainer_m (any)` | 32 | 0 |
| `class:channeler (any)` | 32 | 0 |
| `class:school_kid (any)` | 32 | 0 |
| `gardenia` | 32 | 0 |
| `norman` | 32 | 0 |
| `roughneck` | 32 | 0 |
| `candice` | 31 | 0 |
| `kimono_girl` | 31 | 0 |
| `look:expert_m (any)` | 31 | 0 |
| `policeman` | 31 | 0 |
| `prof_sycamore` | 31 | 0 |
| `breeder` | 30 | 4 |
| `captain` | 30 | 0 |
| `class:school_kid_m (any)` | 30 | 0 |
| `clay` | 30 | 3 |
| `look:battle_girl (any)` | 30 | 0 |
| `zinzolin` | 30 | 0 |
| `celio` | 29 | 1 |
| `chairman` | 29 | 0 |
| `jupiter` | 29 | 0 |
| `look:aide (any)` | 29 | 4 |
| `class:triathlete (any)` | 28 | 0 |
| `little_boy` | 28 | 0 |
| `saturn` | 28 | 0 |
| `director` | 27 | 1 |
| `lt_surge` | 27 | 0 |
| `marley` | 27 | 0 |
| `president` | 27 | 0 |
| `trick_master` | 27 | 0 |
| `brock` | 26 | 0 |
| `class:ace_trainer_f (any)` | 26 | 0 |
| `class:aroma_lady (any)` | 26 | 0 |
| `class:guitarist (any)` | 26 | 0 |
| `class:super_nerd (any)` | 26 | 0 |
| `lottery_clerk` | 26 | 2 |
| `roxie` | 26 | 1 |
| `sabrina` | 26 | 0 |
| `shadow_triad` | 26 | 7 |
| `blue` | 25 | 0 |
| `byron` | 25 | 1 |
| `captain_stern` | 25 | 0 |
| `erika` | 25 | 0 |
| `giovanni` | 25 | 2 |
| `misty` | 25 | 0 |
| `class:cyclist (any)` | 24 | 0 |
| `class:pokefan (any)` | 24 | 0 |
| `class:psychic_m (any)` | 24 | 0 |
| `class:team_plasma_grunt (any)` | 24 | 0 |
| `day_care_lady` | 24 | 3 |
| `marlon` | 24 | 0 |
| `move_deleter` | 24 | 3 |
| `blaine` | 23 | 0 |
| `cook` | 23 | 0 |
| `eusine` | 23 | 0 |
| `fishing_guru` | 23 | 3 |
| `look:galacticdoor (any)` | 23 | 23 |
| `look:gymstatue (any)` | 23 | 0 |
| `look:plasma_grunt (any)` | 23 | 0 |
| `nursery_aide` | 23 | 0 |
| `quiz_lady` | 23 | 0 |
| `valet` | 23 | 5 |
| `wattson` | 23 | 0 |
| `whitney` | 23 | 0 |
| `class:dragon_tamer (any)` | 22 | 0 |
| `class:juggler (any)` | 22 | 0 |
| `class:parasol_lady (any)` | 22 | 0 |
| `class:poke_maniac (any)` | 22 | 0 |
| `class:pokemaniac (any)` | 22 | 0 |
| `class:pokémon_breeder (any)` | 22 | 0 |
| `class:preschooler (any)` | 22 | 0 |
| `class:skier (any)` | 22 | 0 |
| `look:ninja_boy (any)` | 22 | 0 |
| `look:pokemon_breeder_f (any)` | 22 | 0 |
| `mars` | 22 | 0 |
| `schoolboy` | 22 | 0 |
| `cooltrainer` | 21 | 6 |
| `fantina` | 21 | 0 |
| `look:bug_catcher (any)` | 21 | 0 |
| `manager` | 21 | 0 |
| `tierno` | 21 | 0 |
| `veteran` | 21 | 0 |
| `class:collector (any)` | 20 | 0 |
| `class:double_team (any)` | 20 | 0 |
| `class:sis_and_bro (any)` | 20 | 0 |
| `class:tuber (any)` | 20 | 0 |
| `juan` | 20 | 0 |
| `look:announcer (any)` | 20 | 12 |
| `look:camper (any)` | 20 | 0 |
| `n` | 20 | 0 |

## Casting problems

None: every speaker of every script has a cast entry.
