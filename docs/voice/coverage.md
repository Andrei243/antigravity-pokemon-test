# Voice coverage

Written by `dotnet run --project tools/VoiceTool -- catalogue` (plan 26 · V1); `VoiceTests` fails when this is not what the files give. Do not edit by hand.

A **line** is one string a character says in a text box (a script's `say` and the question of an `ask` or `choose`, a person's `dialog`, `before` and `after`, the introduction's professor), under the key plan 17 · N4 gave it. **Spoken** lines have a spoken text (the line with the names the player types left out, which `Data/voice/lines.json` holds); **shown only** lines have none yet, because a placeholder cannot be left out gracefully, and show their text alone until one is written by hand. No audio exists yet: V3 to V5 make it, V2 plays it.

## Lines by kind

| Kind | Lines | Spoken | Shown only | With variants | Written by hand |
|---|---|---|---|---|---|
| The introduction (`intro:`) | 80 | 80 | 0 | 0 | 0 |
| People of the hand-made maps (`map:`) | 1241 | 1208 | 33 | 1 | 0 |
| Scripts (`script:`) | 8135 | 7751 | 384 | 17 | 0 |
| People of the world (`world:`) | 5684 | 5629 | 55 | 4 | 0 |
| **All** | 15140 | 14668 | 472 | 22 | 0 |

## Not voiced, by decision

Plan 26's decision 7: the game's own voice, a question's answers, signs and a Pokémon's cry.

| What | Lines |
|---|---|
| a Pokémon's cry | 182 |
| a question's answers | 496 |
| a sign | 1330 |
| the game's own voice (speaker none) | 235 |
| the game's own voice (text) | 1065 |

## The cast

- Named characters: 302 voices (265 provisional: added for a name nobody has cast yet, with nothing said about how they sound).
- Cloned from the user's samples: `rival`.
- Archetypes in use: 217, with 656 voices to cast (V5).
- Lines whose voice is the person spoken to, decided as the talk starts (`@self`): 1812.

## Lines by voice

The voices with twenty lines or more; archetypes are summed over their voices.

| Voice | Lines | Shown only |
|---|---|---|
| `@self` | 1812 | 132 |
| `look:gentleman (any)` | 575 | 12 |
| `look:grunt (any)` | 544 | 0 |
| `rival` | 498 | 17 |
| `look:youngster (any)` | 339 | 13 |
| `look:lady (any)` | 331 | 1 |
| `look:lass (any)` | 284 | 8 |
| `look:clerk (any)` | 267 | 7 |
| `look:attendant (any)` | 185 | 3 |
| `look:nurse (any)` | 178 | 0 |
| `cynthia` | 157 | 1 |
| `rowan` | 147 | 12 |
| `look:gym_guide (any)` | 146 | 0 |
| `look:scientist (any)` | 143 | 4 |
| `class:team_rocket (any)` | 132 | 0 |
| `look:woman (any)` | 125 | 13 |
| `class:hiker (any)` | 114 | 0 |
| `class:swimmer_m (any)` | 112 | 0 |
| `assistant` | 111 | 5 |
| `class:psychic (any)` | 108 | 0 |
| `class:fisherman (any)` | 106 | 0 |
| `class:ace_trainer (any)` | 104 | 0 |
| `look:boy (any)` | 104 | 4 |
| `look:guard (any)` | 103 | 0 |
| `mom` | 102 | 6 |
| `look:man (any)` | 101 | 4 |
| `look:old_man (any)` | 101 | 4 |
| `class:youngster (any)` | 100 | 0 |
| `class:picnicker (any)` | 98 | 0 |
| `class:cooltrainer (any)` | 96 | 0 |
| `look:sailor (any)` | 92 | 0 |
| `looker` | 91 | 1 |
| `class:black_belt (any)` | 88 | 0 |
| `class:lass (any)` | 88 | 0 |
| `class:swimmer_f (any)` | 86 | 0 |
| `class:galactic (any)` | 84 | 0 |
| `cyrus` | 83 | 7 |
| `look:aqua_grunt (any)` | 83 | 3 |
| `class:bird_keeper (any)` | 82 | 0 |
| `black_belt` | 80 | 12 |
| `prof_oak` | 78 | 3 |
| `look:trainer (any)` | 75 | 0 |
| `look:old_woman (any)` | 72 | 5 |
| `class:bug_catcher (any)` | 70 | 0 |
| `look:hiker (any)` | 70 | 0 |
| `bianca` | 69 | 3 |
| `look:receptionist (any)` | 69 | 5 |
| `look:fisherman (any)` | 67 | 4 |
| `ace_trainer` | 66 | 0 |
| `class:camper (any)` | 62 | 0 |
| `underground_man` | 62 | 0 |
| `class:twins (any)` | 60 | 0 |
| `steven` | 59 | 0 |
| `lance` | 58 | 0 |
| `prof_elm` | 58 | 1 |
| `beauty` | 57 | 4 |
| `class:sailor (any)` | 54 | 0 |
| `class:scientist (any)` | 54 | 0 |
| `class:swimmer (any)` | 54 | 0 |
| `archie` | 52 | 3 |
| `look:worker (any)` | 51 | 0 |
| `bill` | 50 | 2 |
| `class:beauty (any)` | 46 | 0 |
| `elder` | 46 | 0 |
| `maxie` | 46 | 1 |
| `wallace` | 46 | 2 |
| `look:girl (any)` | 45 | 0 |
| `prof_juniper` | 45 | 0 |
| `cheren` | 44 | 1 |
| `class:team_aqua (any)` | 44 | 0 |
| `class:team_magma (any)` | 44 | 0 |
| `crasher_wake` | 44 | 1 |
| `buck` | 42 | 1 |
| `class:worker (any)` | 42 | 0 |
| `riley` | 42 | 0 |
| `flint` | 41 | 1 |
| `mystery` | 41 | 4 |
| `look:host (any)` | 40 | 16 |
| `volkner` | 40 | 1 |
| `collector` | 38 | 0 |
| `officer` | 38 | 5 |
| `wally` | 38 | 0 |
| `scott` | 37 | 0 |
| `class:ninja_boy (any)` | 36 | 0 |
| `depot_agent` | 36 | 5 |
| `look:magma_grunt (any)` | 36 | 0 |
| `clair` | 35 | 4 |
| `class:ruin_maniac (any)` | 34 | 0 |
| `maylene` | 34 | 1 |
| `mr_briney` | 34 | 1 |
| `artist` | 33 | 1 |
| `baoba` | 33 | 0 |
| `prof_birch` | 33 | 3 |
| `roark` | 33 | 0 |
| `alder` | 32 | 1 |
| `class:biker (any)` | 32 | 0 |
| `class:channeler (any)` | 32 | 0 |
| `class:pokémon_ranger (any)` | 32 | 0 |
| `gardenia` | 32 | 0 |
| `candice` | 31 | 0 |
| `jasmine` | 31 | 0 |
| `kurt` | 31 | 6 |
| `look:twin (any)` | 31 | 0 |
| `class:young_couple (any)` | 30 | 0 |
| `clay` | 30 | 3 |
| `celio` | 29 | 1 |
| `jupiter` | 29 | 0 |
| `look:aide (any)` | 29 | 4 |
| `look:pokefan_m (any)` | 29 | 0 |
| `class:triathlete (any)` | 28 | 0 |
| `look:pokefan_f (any)` | 28 | 0 |
| `saturn` | 28 | 0 |
| `look:expert_m (any)` | 27 | 0 |
| `president` | 27 | 0 |
| `captain` | 26 | 0 |
| `class:aroma_lady (any)` | 26 | 0 |
| `class:battle_girl (any)` | 26 | 0 |
| `class:guitarist (any)` | 26 | 0 |
| `look:battle_girl (any)` | 26 | 0 |
| `lottery_clerk` | 26 | 2 |
| `policeman` | 26 | 0 |
| `roxie` | 26 | 1 |
| `byron` | 25 | 1 |
| `giovanni` | 25 | 2 |
| `norman` | 25 | 0 |
| `class:ace_trainer_m (any)` | 24 | 0 |
| `class:gentleman (any)` | 24 | 0 |
| `class:sis_and_bro (any)` | 24 | 0 |
| `day_care_lady` | 24 | 3 |
| `move_deleter` | 24 | 3 |
| `owner` | 24 | 0 |
| `school_kid` | 24 | 0 |
| `look:galacticdoor (any)` | 23 | 23 |
| `rood` | 23 | 0 |
| `valet` | 23 | 5 |
| `class:ace_trainer_f (any)` | 22 | 0 |
| `class:dragon_tamer (any)` | 22 | 0 |
| `class:poke_maniac (any)` | 22 | 0 |
| `class:pokemaniac (any)` | 22 | 0 |
| `marley` | 22 | 0 |
| `mars` | 22 | 0 |
| `roughneck` | 22 | 0 |
| `schoolboy` | 22 | 0 |
| `cooltrainer` | 21 | 6 |
| `fantina` | 21 | 0 |
| `look:bug_catcher (any)` | 21 | 0 |
| `look:gymstatue (any)` | 21 | 0 |
| `class:backpacker (any)` | 20 | 0 |
| `class:collector (any)` | 20 | 0 |
| `class:cyclist (any)` | 20 | 0 |
| `class:skier (any)` | 20 | 0 |
| `class:tuber (any)` | 20 | 0 |
| `class:veteran (any)` | 20 | 0 |
| `colress` | 20 | 1 |
| `cook` | 20 | 0 |

## Casting problems

None: every speaker of every script has a cast entry.
