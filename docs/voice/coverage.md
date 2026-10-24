# Voice coverage

Written by `dotnet run --project tools/VoiceTool -- catalogue` (plan 26 · V1); `VoiceTests` fails when this is not what the files give. Do not edit by hand.

A **line** is one string a character says in a text box (a script's `say` and the question of an `ask` or `choose`, a person's `dialog`, `before` and `after`, the introduction's professor), under the key plan 17 · N4 gave it. **Spoken** lines have a spoken text (the line with the names the player types left out, which `Data/voice/lines.json` holds); **shown only** lines have none yet, because a placeholder cannot be left out gracefully, and show their text alone until one is written by hand. No audio exists yet: V3 to V5 make it, V2 plays it.

## Lines by kind

| Kind | Lines | Spoken | Shown only | With variants | Written by hand |
|---|---|---|---|---|---|
| The introduction (`intro:`) | 80 | 80 | 0 | 0 | 0 |
| People of the hand-made maps (`map:`) | 1209 | 1180 | 29 | 1 | 0 |
| Scripts (`script:`) | 6505 | 6191 | 314 | 13 | 0 |
| People of the world (`world:`) | 4700 | 4652 | 48 | 4 | 0 |
| **All** | 12494 | 12103 | 391 | 18 | 0 |

## Not voiced, by decision

Plan 26's decision 7: the game's own voice, a question's answers, signs and a Pokémon's cry.

| What | Lines |
|---|---|
| a Pokémon's cry | 152 |
| a question's answers | 353 |
| a sign | 1190 |
| the game's own voice (speaker none) | 221 |
| the game's own voice (text) | 898 |

## The cast

- Named characters: 238 voices (201 provisional: added for a name nobody has cast yet, with nothing said about how they sound).
- Cloned from the user's samples: `rival`.
- Archetypes in use: 191, with 578 voices to cast (V5).
- Lines whose voice is the person spoken to, decided as the talk starts (`@self`): 1754.

## Lines by voice

The voices with twenty lines or more; archetypes are summed over their voices.

| Voice | Lines | Shown only |
|---|---|---|
| `@self` | 1754 | 123 |
| `look:gentleman (any)` | 554 | 12 |
| `look:grunt (any)` | 489 | 0 |
| `rival` | 359 | 15 |
| `look:youngster (any)` | 312 | 11 |
| `look:lady (any)` | 298 | 1 |
| `look:lass (any)` | 249 | 8 |
| `look:clerk (any)` | 191 | 4 |
| `look:attendant (any)` | 165 | 2 |
| `look:nurse (any)` | 165 | 0 |
| `cynthia` | 157 | 1 |
| `rowan` | 147 | 12 |
| `class:team_rocket (any)` | 132 | 0 |
| `look:scientist (any)` | 120 | 4 |
| `look:gym_guide (any)` | 112 | 0 |
| `assistant` | 111 | 5 |
| `class:ace_trainer (any)` | 102 | 0 |
| `class:hiker (any)` | 102 | 0 |
| `class:picnicker (any)` | 100 | 0 |
| `class:fisherman (any)` | 94 | 0 |
| `look:woman (any)` | 91 | 11 |
| `looker` | 91 | 1 |
| `class:youngster (any)` | 90 | 0 |
| `look:guard (any)` | 87 | 0 |
| `class:black_belt (any)` | 84 | 0 |
| `class:galactic (any)` | 84 | 0 |
| `cyrus` | 83 | 7 |
| `class:lass (any)` | 80 | 0 |
| `class:swimmer_m (any)` | 80 | 0 |
| `look:aqua_grunt (any)` | 79 | 3 |
| `prof_oak` | 77 | 3 |
| `class:bird_keeper (any)` | 74 | 0 |
| `look:boy (any)` | 74 | 4 |
| `mom` | 74 | 5 |
| `look:man (any)` | 71 | 0 |
| `class:bug_catcher (any)` | 70 | 0 |
| `class:psychic (any)` | 70 | 0 |
| `look:trainer (any)` | 70 | 0 |
| `look:sailor (any)` | 68 | 0 |
| `look:old_woman (any)` | 65 | 5 |
| `look:old_man (any)` | 63 | 4 |
| `class:camper (any)` | 62 | 0 |
| `underground_man` | 62 | 0 |
| `class:swimmer_f (any)` | 58 | 0 |
| `class:twins (any)` | 58 | 0 |
| `prof_elm` | 58 | 1 |
| `beauty` | 57 | 4 |
| `class:swimmer (any)` | 56 | 0 |
| `black_belt` | 53 | 8 |
| `class:cooltrainer (any)` | 52 | 0 |
| `bill` | 50 | 2 |
| `class:sailor (any)` | 50 | 0 |
| `lance` | 49 | 0 |
| `elder` | 46 | 0 |
| `look:fisherman (any)` | 45 | 4 |
| `crasher_wake` | 44 | 1 |
| `buck` | 42 | 1 |
| `riley` | 42 | 0 |
| `ace_trainer` | 41 | 0 |
| `flint` | 41 | 1 |
| `look:host (any)` | 40 | 16 |
| `volkner` | 40 | 1 |
| `class:beauty (any)` | 38 | 0 |
| `class:scientist (any)` | 38 | 0 |
| `look:hiker (any)` | 37 | 0 |
| `class:ninja_boy (any)` | 36 | 0 |
| `class:team_magma (any)` | 36 | 0 |
| `look:receptionist (any)` | 36 | 3 |
| `class:ruin_maniac (any)` | 34 | 0 |
| `class:team_aqua (any)` | 34 | 0 |
| `maylene` | 34 | 1 |
| `baoba` | 33 | 0 |
| `mr_briney` | 33 | 1 |
| `roark` | 33 | 0 |
| `class:biker (any)` | 32 | 0 |
| `class:channeler (any)` | 32 | 0 |
| `gardenia` | 32 | 0 |
| `candice` | 31 | 0 |
| `collector` | 31 | 0 |
| `jasmine` | 31 | 0 |
| `class:young_couple (any)` | 30 | 0 |
| `celio` | 29 | 1 |
| `jupiter` | 29 | 0 |
| `look:aide (any)` | 29 | 4 |
| `look:girl (any)` | 29 | 0 |
| `prof_birch` | 29 | 3 |
| `saturn` | 28 | 0 |
| `president` | 27 | 0 |
| `class:aroma_lady (any)` | 26 | 0 |
| `lottery_clerk` | 26 | 2 |
| `archie` | 25 | 2 |
| `byron` | 25 | 1 |
| `clair` | 25 | 2 |
| `giovanni` | 25 | 2 |
| `class:ace_trainer_m (any)` | 24 | 0 |
| `class:battle_girl (any)` | 24 | 0 |
| `class:dragon_tamer (any)` | 24 | 0 |
| `day_care_lady` | 24 | 3 |
| `look:expert_m (any)` | 23 | 0 |
| `look:galacticdoor (any)` | 23 | 23 |
| `mystery` | 23 | 1 |
| `schoolboy` | 23 | 0 |
| `valet` | 23 | 5 |
| `wally` | 23 | 0 |
| `class:ace_trainer_f (any)` | 22 | 0 |
| `class:poke_maniac (any)` | 22 | 0 |
| `class:pokemaniac (any)` | 22 | 0 |
| `class:worker (any)` | 22 | 0 |
| `look:worker (any)` | 22 | 0 |
| `mars` | 22 | 0 |
| `scott` | 22 | 0 |
| `steven` | 22 | 0 |
| `cooltrainer` | 21 | 6 |
| `fantina` | 21 | 0 |
| `look:pokefan_f (any)` | 21 | 0 |
| `look:pokefan_m (any)` | 21 | 0 |
| `norman` | 21 | 0 |
| `officer` | 21 | 1 |
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
