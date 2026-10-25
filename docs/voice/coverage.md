# Voice coverage

Written by `dotnet run --project tools/VoiceTool -- catalogue` (plan 26 · V1); `VoiceTests` fails when this is not what the files give. Do not edit by hand.

A **line** is one string a character says in a text box (a script's `say` and the question of an `ask` or `choose`, a person's `dialog`, `before` and `after`, the introduction's professor), under the key plan 17 · N4 gave it. **Spoken** lines have a spoken text (the line with the names the player types left out, which `Data/voice/lines.json` holds); **shown only** lines have none yet, because a placeholder cannot be left out gracefully, and show their text alone until one is written by hand. No audio exists yet: V3 to V5 make it, V2 plays it.

## Lines by kind

| Kind | Lines | Spoken | Shown only | With variants | Written by hand |
|---|---|---|---|---|---|
| The introduction (`intro:`) | 80 | 80 | 0 | 0 | 0 |
| People of the hand-made maps (`map:`) | 1228 | 1199 | 29 | 1 | 0 |
| Scripts (`script:`) | 7710 | 7350 | 360 | 17 | 0 |
| People of the world (`world:`) | 5382 | 5330 | 52 | 4 | 0 |
| **All** | 14400 | 13959 | 441 | 22 | 0 |

## Not voiced, by decision

Plan 26's decision 7: the game's own voice, a question's answers, signs and a Pokémon's cry.

| What | Lines |
|---|---|
| a Pokémon's cry | 171 |
| a question's answers | 454 |
| a sign | 1288 |
| the game's own voice (speaker none) | 229 |
| the game's own voice (text) | 987 |

## The cast

- Named characters: 285 voices (248 provisional: added for a name nobody has cast yet, with nothing said about how they sound).
- Cloned from the user's samples: `rival`.
- Archetypes in use: 212, with 641 voices to cast (V5).
- Lines whose voice is the person spoken to, decided as the talk starts (`@self`): 1785.

## Lines by voice

The voices with twenty lines or more; archetypes are summed over their voices.

| Voice | Lines | Shown only |
|---|---|---|
| `@self` | 1785 | 128 |
| `look:gentleman (any)` | 570 | 12 |
| `look:grunt (any)` | 537 | 0 |
| `rival` | 485 | 17 |
| `look:youngster (any)` | 326 | 11 |
| `look:lady (any)` | 319 | 1 |
| `look:lass (any)` | 273 | 8 |
| `look:clerk (any)` | 261 | 7 |
| `look:nurse (any)` | 176 | 0 |
| `look:attendant (any)` | 170 | 2 |
| `cynthia` | 157 | 1 |
| `rowan` | 147 | 12 |
| `look:gym_guide (any)` | 140 | 0 |
| `look:scientist (any)` | 134 | 4 |
| `class:team_rocket (any)` | 132 | 0 |
| `look:woman (any)` | 123 | 13 |
| `class:swimmer_m (any)` | 112 | 0 |
| `assistant` | 111 | 5 |
| `class:fisherman (any)` | 106 | 0 |
| `look:guard (any)` | 103 | 0 |
| `class:hiker (any)` | 102 | 0 |
| `mom` | 102 | 6 |
| `look:boy (any)` | 99 | 4 |
| `class:ace_trainer (any)` | 98 | 0 |
| `class:picnicker (any)` | 98 | 0 |
| `class:youngster (any)` | 98 | 0 |
| `class:cooltrainer (any)` | 96 | 0 |
| `look:man (any)` | 93 | 4 |
| `looker` | 91 | 1 |
| `class:psychic (any)` | 90 | 0 |
| `class:black_belt (any)` | 88 | 0 |
| `class:lass (any)` | 86 | 0 |
| `class:swimmer_f (any)` | 86 | 0 |
| `look:sailor (any)` | 86 | 0 |
| `class:galactic (any)` | 84 | 0 |
| `cyrus` | 83 | 7 |
| `look:aqua_grunt (any)` | 83 | 3 |
| `look:old_man (any)` | 83 | 4 |
| `class:bird_keeper (any)` | 82 | 0 |
| `black_belt` | 78 | 12 |
| `prof_oak` | 78 | 3 |
| `look:trainer (any)` | 72 | 0 |
| `class:bug_catcher (any)` | 70 | 0 |
| `look:old_woman (any)` | 70 | 5 |
| `look:fisherman (any)` | 66 | 4 |
| `class:camper (any)` | 62 | 0 |
| `underground_man` | 62 | 0 |
| `ace_trainer` | 61 | 0 |
| `look:hiker (any)` | 59 | 0 |
| `steven` | 59 | 0 |
| `lance` | 58 | 0 |
| `prof_elm` | 58 | 1 |
| `beauty` | 57 | 4 |
| `bianca` | 57 | 3 |
| `class:twins (any)` | 56 | 0 |
| `class:sailor (any)` | 54 | 0 |
| `class:swimmer (any)` | 54 | 0 |
| `archie` | 52 | 3 |
| `bill` | 50 | 2 |
| `class:beauty (any)` | 46 | 0 |
| `elder` | 46 | 0 |
| `maxie` | 46 | 1 |
| `class:team_aqua (any)` | 44 | 0 |
| `class:team_magma (any)` | 44 | 0 |
| `crasher_wake` | 44 | 1 |
| `look:receptionist (any)` | 44 | 3 |
| `buck` | 42 | 1 |
| `class:scientist (any)` | 42 | 0 |
| `class:worker (any)` | 42 | 0 |
| `riley` | 42 | 0 |
| `wallace` | 42 | 2 |
| `flint` | 41 | 1 |
| `look:host (any)` | 40 | 16 |
| `mystery` | 40 | 4 |
| `volkner` | 40 | 1 |
| `look:worker (any)` | 38 | 0 |
| `officer` | 38 | 5 |
| `class:ninja_boy (any)` | 36 | 0 |
| `look:girl (any)` | 36 | 0 |
| `look:magma_grunt (any)` | 36 | 0 |
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
| `class:triathlete (any)` | 28 | 0 |
| `look:pokefan_m (any)` | 28 | 0 |
| `saturn` | 28 | 0 |
| `president` | 27 | 0 |
| `captain` | 26 | 0 |
| `class:aroma_lady (any)` | 26 | 0 |
| `class:battle_girl (any)` | 26 | 0 |
| `look:expert_m (any)` | 26 | 0 |
| `look:pokefan_f (any)` | 26 | 0 |
| `look:twin (any)` | 26 | 0 |
| `lottery_clerk` | 26 | 2 |
| `roxie` | 26 | 1 |
| `byron` | 25 | 1 |
| `giovanni` | 25 | 2 |
| `norman` | 25 | 0 |
| `policeman` | 25 | 0 |
| `class:ace_trainer_m (any)` | 24 | 0 |
| `class:dragon_tamer (any)` | 24 | 0 |
| `class:sis_and_bro (any)` | 24 | 0 |
| `day_care_lady` | 24 | 3 |
| `move_deleter` | 24 | 3 |
| `owner` | 24 | 0 |
| `look:galacticdoor (any)` | 23 | 23 |
| `valet` | 23 | 5 |
| `cheren` | 22 | 0 |
| `class:ace_trainer_f (any)` | 22 | 0 |
| `class:gentleman (any)` | 22 | 0 |
| `class:guitarist (any)` | 22 | 0 |
| `class:poke_maniac (any)` | 22 | 0 |
| `class:pokemaniac (any)` | 22 | 0 |
| `class:pokémon_ranger (any)` | 22 | 0 |
| `look:battle_girl (any)` | 22 | 0 |
| `marley` | 22 | 0 |
| `mars` | 22 | 0 |
| `school_kid` | 22 | 0 |
| `schoolboy` | 22 | 0 |
| `cooltrainer` | 21 | 6 |
| `fantina` | 21 | 0 |
| `look:bug_catcher (any)` | 21 | 0 |
| `look:gymstatue (any)` | 21 | 0 |
| `look:pokemon_breeder_f (any)` | 21 | 0 |
| `roughneck` | 21 | 0 |
| `class:collector (any)` | 20 | 0 |
| `class:skier (any)` | 20 | 0 |
| `class:tuber (any)` | 20 | 0 |
| `cook` | 20 | 0 |

## Casting problems

None: every speaker of every script has a cast entry.
