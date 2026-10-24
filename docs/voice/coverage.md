# Voice coverage

Written by `dotnet run --project tools/VoiceTool -- catalogue` (plan 26 · V1); `VoiceTests` fails when this is not what the files give. Do not edit by hand.

A **line** is one string a character says in a text box (a script's `say` and the question of an `ask` or `choose`, a person's `dialog`, `before` and `after`, the introduction's professor), under the key plan 17 · N4 gave it. **Spoken** lines have a spoken text (the line with the names the player types left out, which `Data/voice/lines.json` holds); **shown only** lines have none yet, because a placeholder cannot be left out gracefully, and show their text alone until one is written by hand. No audio exists yet: V3 to V5 make it, V2 plays it.

## Lines by kind

| Kind | Lines | Spoken | Shown only | With variants | Written by hand |
|---|---|---|---|---|---|
| The introduction (`intro:`) | 80 | 80 | 0 | 0 | 0 |
| People of the hand-made maps (`map:`) | 1209 | 1180 | 29 | 1 | 0 |
| Scripts (`script:`) | 7068 | 6735 | 333 | 17 | 0 |
| People of the world (`world:`) | 4901 | 4853 | 48 | 4 | 0 |
| **All** | 13258 | 12848 | 410 | 22 | 0 |

## Not voiced, by decision

Plan 26's decision 7: the game's own voice, a question's answers, signs and a Pokémon's cry.

| What | Lines |
|---|---|
| a Pokémon's cry | 161 |
| a question's answers | 362 |
| a sign | 1221 |
| the game's own voice (speaker none) | 223 |
| the game's own voice (text) | 935 |

## The cast

- Named characters: 255 voices (218 provisional: added for a name nobody has cast yet, with nothing said about how they sound).
- Cloned from the user's samples: `rival`.
- Archetypes in use: 191, with 578 voices to cast (V5).
- Lines whose voice is the person spoken to, decided as the talk starts (`@self`): 1757.

## Lines by voice

The voices with twenty lines or more; archetypes are summed over their voices.

| Voice | Lines | Shown only |
|---|---|---|
| `@self` | 1757 | 123 |
| `look:gentleman (any)` | 569 | 12 |
| `look:grunt (any)` | 500 | 0 |
| `rival` | 405 | 16 |
| `look:youngster (any)` | 315 | 11 |
| `look:lady (any)` | 308 | 1 |
| `look:lass (any)` | 254 | 8 |
| `look:clerk (any)` | 203 | 5 |
| `look:nurse (any)` | 171 | 0 |
| `look:attendant (any)` | 165 | 2 |
| `cynthia` | 157 | 1 |
| `rowan` | 147 | 12 |
| `class:team_rocket (any)` | 132 | 0 |
| `look:scientist (any)` | 127 | 4 |
| `look:gym_guide (any)` | 120 | 0 |
| `look:woman (any)` | 119 | 13 |
| `assistant` | 111 | 5 |
| `class:fisherman (any)` | 102 | 0 |
| `class:hiker (any)` | 102 | 0 |
| `class:ace_trainer (any)` | 98 | 0 |
| `class:picnicker (any)` | 98 | 0 |
| `mom` | 95 | 6 |
| `class:swimmer_m (any)` | 92 | 0 |
| `class:youngster (any)` | 92 | 0 |
| `look:guard (any)` | 92 | 0 |
| `look:boy (any)` | 91 | 4 |
| `looker` | 91 | 1 |
| `class:psychic (any)` | 86 | 0 |
| `class:black_belt (any)` | 84 | 0 |
| `class:galactic (any)` | 84 | 0 |
| `cyrus` | 83 | 7 |
| `look:aqua_grunt (any)` | 83 | 3 |
| `class:lass (any)` | 82 | 0 |
| `class:bird_keeper (any)` | 78 | 0 |
| `prof_oak` | 78 | 3 |
| `look:man (any)` | 76 | 0 |
| `look:sailor (any)` | 76 | 0 |
| `look:trainer (any)` | 72 | 0 |
| `black_belt` | 71 | 12 |
| `look:old_man (any)` | 71 | 4 |
| `class:bug_catcher (any)` | 70 | 0 |
| `class:swimmer_f (any)` | 70 | 0 |
| `look:old_woman (any)` | 68 | 5 |
| `class:camper (any)` | 62 | 0 |
| `underground_man` | 62 | 0 |
| `look:fisherman (any)` | 59 | 4 |
| `class:cooltrainer (any)` | 58 | 0 |
| `prof_elm` | 58 | 1 |
| `beauty` | 57 | 4 |
| `class:twins (any)` | 56 | 0 |
| `steven` | 55 | 0 |
| `class:swimmer (any)` | 54 | 0 |
| `look:hiker (any)` | 52 | 0 |
| `archie` | 50 | 3 |
| `bianca` | 50 | 2 |
| `bill` | 50 | 2 |
| `class:sailor (any)` | 50 | 0 |
| `lance` | 49 | 0 |
| `elder` | 46 | 0 |
| `class:team_aqua (any)` | 44 | 0 |
| `class:team_magma (any)` | 44 | 0 |
| `crasher_wake` | 44 | 1 |
| `maxie` | 44 | 1 |
| `buck` | 42 | 1 |
| `riley` | 42 | 0 |
| `ace_trainer` | 41 | 0 |
| `flint` | 41 | 1 |
| `look:host (any)` | 40 | 16 |
| `volkner` | 40 | 1 |
| `class:beauty (any)` | 38 | 0 |
| `class:scientist (any)` | 38 | 0 |
| `officer` | 38 | 5 |
| `class:ninja_boy (any)` | 36 | 0 |
| `look:magma_grunt (any)` | 36 | 0 |
| `look:receptionist (any)` | 36 | 3 |
| `class:ruin_maniac (any)` | 34 | 0 |
| `maylene` | 34 | 1 |
| `mr_briney` | 34 | 1 |
| `baoba` | 33 | 0 |
| `look:girl (any)` | 33 | 0 |
| `roark` | 33 | 0 |
| `alder` | 32 | 1 |
| `class:channeler (any)` | 32 | 0 |
| `gardenia` | 32 | 0 |
| `candice` | 31 | 0 |
| `collector` | 31 | 0 |
| `jasmine` | 31 | 0 |
| `class:biker (any)` | 30 | 0 |
| `class:young_couple (any)` | 30 | 0 |
| `celio` | 29 | 1 |
| `jupiter` | 29 | 0 |
| `look:aide (any)` | 29 | 4 |
| `prof_birch` | 29 | 3 |
| `saturn` | 28 | 0 |
| `mystery` | 27 | 1 |
| `president` | 27 | 0 |
| `class:aroma_lady (any)` | 26 | 0 |
| `lottery_clerk` | 26 | 2 |
| `byron` | 25 | 1 |
| `clair` | 25 | 2 |
| `giovanni` | 25 | 2 |
| `scott` | 25 | 0 |
| `class:ace_trainer_m (any)` | 24 | 0 |
| `class:battle_girl (any)` | 24 | 0 |
| `class:dragon_tamer (any)` | 24 | 0 |
| `day_care_lady` | 24 | 3 |
| `look:expert_m (any)` | 24 | 0 |
| `look:galacticdoor (any)` | 23 | 23 |
| `owner` | 23 | 0 |
| `valet` | 23 | 5 |
| `wally` | 23 | 0 |
| `cheren` | 22 | 0 |
| `class:ace_trainer_f (any)` | 22 | 0 |
| `class:gentleman (any)` | 22 | 0 |
| `class:poke_maniac (any)` | 22 | 0 |
| `class:pokemaniac (any)` | 22 | 0 |
| `class:triathlete (any)` | 22 | 0 |
| `class:worker (any)` | 22 | 0 |
| `look:pokefan_f (any)` | 22 | 0 |
| `look:pokefan_m (any)` | 22 | 0 |
| `look:worker (any)` | 22 | 0 |
| `mars` | 22 | 0 |
| `schoolboy` | 22 | 0 |
| `cooltrainer` | 21 | 6 |
| `fantina` | 21 | 0 |
| `look:bug_catcher (any)` | 21 | 0 |
| `look:gymstatue (any)` | 21 | 0 |
| `norman` | 21 | 0 |
| `class:collector (any)` | 20 | 0 |
| `class:guitarist (any)` | 20 | 0 |
| `class:sis_and_bro (any)` | 20 | 0 |
| `class:skier (any)` | 20 | 0 |
| `class:tuber (any)` | 20 | 0 |
| `cook` | 20 | 0 |
| `marley` | 20 | 0 |
| `school_kid` | 20 | 0 |

## Casting problems

None: every speaker of every script has a cast entry.
