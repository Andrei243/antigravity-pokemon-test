# Voice coverage

Written by `dotnet run --project tools/VoiceTool -- catalogue` (plan 26 · V1); `VoiceTests` fails when this is not what the files give. Do not edit by hand.

A **line** is one string a character says in a text box (a script's `say` and the question of an `ask` or `choose`, a person's `dialog`, `before` and `after`, the introduction's professor), under the key plan 17 · N4 gave it. **Spoken** lines have a spoken text (the line with the names the player types left out, which `Data/voice/lines.json` holds); **shown only** lines have none yet, because a placeholder cannot be left out gracefully, and show their text alone until one is written by hand. No audio exists yet: V3 to V5 make it, V2 plays it.

## Lines by kind

| Kind | Lines | Spoken | Shown only | With variants | Written by hand |
|---|---|---|---|---|---|
| The introduction (`intro:`) | 80 | 80 | 0 | 0 | 0 |
| People of the hand-made maps (`map:`) | 1214 | 1185 | 29 | 1 | 0 |
| Scripts (`script:`) | 7570 | 7214 | 356 | 17 | 0 |
| People of the world (`world:`) | 5269 | 5217 | 52 | 4 | 0 |
| **All** | 14133 | 13696 | 437 | 22 | 0 |

## Not voiced, by decision

Plan 26's decision 7: the game's own voice, a question's answers, signs and a Pokémon's cry.

| What | Lines |
|---|---|
| a Pokémon's cry | 168 |
| a question's answers | 362 |
| a sign | 1264 |
| the game's own voice (speaker none) | 223 |
| the game's own voice (text) | 969 |

## The cast

- Named characters: 282 voices (245 provisional: added for a name nobody has cast yet, with nothing said about how they sound).
- Cloned from the user's samples: `rival`.
- Archetypes in use: 206, with 623 voices to cast (V5).
- Lines whose voice is the person spoken to, decided as the talk starts (`@self`): 1770.

## Lines by voice

The voices with twenty lines or more; archetypes are summed over their voices.

| Voice | Lines | Shown only |
|---|---|---|
| `@self` | 1770 | 127 |
| `look:gentleman (any)` | 581 | 12 |
| `look:grunt (any)` | 533 | 0 |
| `rival` | 469 | 17 |
| `look:youngster (any)` | 324 | 11 |
| `look:lady (any)` | 319 | 1 |
| `look:lass (any)` | 270 | 8 |
| `look:clerk (any)` | 224 | 5 |
| `look:nurse (any)` | 174 | 0 |
| `look:attendant (any)` | 165 | 2 |
| `cynthia` | 157 | 1 |
| `rowan` | 147 | 12 |
| `look:gym_guide (any)` | 133 | 0 |
| `look:scientist (any)` | 133 | 4 |
| `class:team_rocket (any)` | 132 | 0 |
| `look:woman (any)` | 123 | 13 |
| `class:swimmer_m (any)` | 112 | 0 |
| `assistant` | 111 | 5 |
| `class:fisherman (any)` | 106 | 0 |
| `look:guard (any)` | 103 | 0 |
| `class:hiker (any)` | 102 | 0 |
| `class:ace_trainer (any)` | 98 | 0 |
| `class:picnicker (any)` | 98 | 0 |
| `class:youngster (any)` | 98 | 0 |
| `class:cooltrainer (any)` | 96 | 0 |
| `mom` | 95 | 6 |
| `look:boy (any)` | 94 | 4 |
| `look:man (any)` | 93 | 4 |
| `looker` | 91 | 1 |
| `class:psychic (any)` | 90 | 0 |
| `class:black_belt (any)` | 88 | 0 |
| `class:lass (any)` | 86 | 0 |
| `class:swimmer_f (any)` | 86 | 0 |
| `class:galactic (any)` | 84 | 0 |
| `cyrus` | 83 | 7 |
| `look:aqua_grunt (any)` | 83 | 3 |
| `class:bird_keeper (any)` | 82 | 0 |
| `look:old_man (any)` | 82 | 4 |
| `black_belt` | 78 | 12 |
| `prof_oak` | 78 | 3 |
| `look:sailor (any)` | 76 | 0 |
| `look:trainer (any)` | 74 | 0 |
| `class:bug_catcher (any)` | 70 | 0 |
| `look:old_woman (any)` | 70 | 5 |
| `class:camper (any)` | 62 | 0 |
| `underground_man` | 62 | 0 |
| `look:fisherman (any)` | 60 | 4 |
| `steven` | 59 | 0 |
| `lance` | 58 | 0 |
| `prof_elm` | 58 | 1 |
| `beauty` | 57 | 4 |
| `class:twins (any)` | 56 | 0 |
| `look:hiker (any)` | 55 | 0 |
| `class:sailor (any)` | 54 | 0 |
| `class:swimmer (any)` | 54 | 0 |
| `archie` | 52 | 3 |
| `bianca` | 50 | 2 |
| `bill` | 50 | 2 |
| `ace_trainer` | 49 | 0 |
| `class:beauty (any)` | 46 | 0 |
| `elder` | 46 | 0 |
| `maxie` | 46 | 1 |
| `class:team_aqua (any)` | 44 | 0 |
| `class:team_magma (any)` | 44 | 0 |
| `crasher_wake` | 44 | 1 |
| `buck` | 42 | 1 |
| `class:scientist (any)` | 42 | 0 |
| `riley` | 42 | 0 |
| `wallace` | 42 | 2 |
| `flint` | 41 | 1 |
| `look:host (any)` | 40 | 16 |
| `mystery` | 40 | 4 |
| `volkner` | 40 | 1 |
| `officer` | 38 | 5 |
| `class:ninja_boy (any)` | 36 | 0 |
| `look:girl (any)` | 36 | 0 |
| `look:magma_grunt (any)` | 36 | 0 |
| `look:receptionist (any)` | 36 | 3 |
| `clair` | 35 | 4 |
| `class:ruin_maniac (any)` | 34 | 0 |
| `maylene` | 34 | 1 |
| `mr_briney` | 34 | 1 |
| `baoba` | 33 | 0 |
| `prof_birch` | 33 | 3 |
| `roark` | 33 | 0 |
| `wally` | 33 | 0 |
| `alder` | 32 | 1 |
| `class:biker (any)` | 32 | 0 |
| `class:channeler (any)` | 32 | 0 |
| `gardenia` | 32 | 0 |
| `candice` | 31 | 0 |
| `collector` | 31 | 0 |
| `jasmine` | 31 | 0 |
| `class:young_couple (any)` | 30 | 0 |
| `scott` | 30 | 0 |
| `celio` | 29 | 1 |
| `jupiter` | 29 | 0 |
| `look:aide (any)` | 29 | 4 |
| `look:worker (any)` | 29 | 0 |
| `class:triathlete (any)` | 28 | 0 |
| `class:worker (any)` | 28 | 0 |
| `saturn` | 28 | 0 |
| `president` | 27 | 0 |
| `captain` | 26 | 0 |
| `class:aroma_lady (any)` | 26 | 0 |
| `class:battle_girl (any)` | 26 | 0 |
| `look:expert_m (any)` | 26 | 0 |
| `lottery_clerk` | 26 | 2 |
| `roxie` | 26 | 1 |
| `byron` | 25 | 1 |
| `giovanni` | 25 | 2 |
| `class:ace_trainer_m (any)` | 24 | 0 |
| `class:dragon_tamer (any)` | 24 | 0 |
| `class:sis_and_bro (any)` | 24 | 0 |
| `day_care_lady` | 24 | 3 |
| `look:pokefan_m (any)` | 24 | 0 |
| `move_deleter` | 24 | 3 |
| `owner` | 24 | 0 |
| `policeman` | 24 | 0 |
| `look:galacticdoor (any)` | 23 | 23 |
| `look:pokefan_f (any)` | 23 | 0 |
| `valet` | 23 | 5 |
| `cheren` | 22 | 0 |
| `class:ace_trainer_f (any)` | 22 | 0 |
| `class:gentleman (any)` | 22 | 0 |
| `class:guitarist (any)` | 22 | 0 |
| `class:poke_maniac (any)` | 22 | 0 |
| `class:pokemaniac (any)` | 22 | 0 |
| `marley` | 22 | 0 |
| `mars` | 22 | 0 |
| `school_kid` | 22 | 0 |
| `schoolboy` | 22 | 0 |
| `cooltrainer` | 21 | 6 |
| `fantina` | 21 | 0 |
| `look:battle_girl (any)` | 21 | 0 |
| `look:bug_catcher (any)` | 21 | 0 |
| `look:gymstatue (any)` | 21 | 0 |
| `norman` | 21 | 0 |
| `class:collector (any)` | 20 | 0 |
| `class:pokémon_ranger (any)` | 20 | 0 |
| `class:skier (any)` | 20 | 0 |
| `class:tuber (any)` | 20 | 0 |
| `cook` | 20 | 0 |

## Casting problems

None: every speaker of every script has a cast entry.
