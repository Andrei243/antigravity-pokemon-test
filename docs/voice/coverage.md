# Voice coverage

Written by `dotnet run --project tools/VoiceTool -- catalogue` (plan 26 · V1); `VoiceTests` fails when this is not what the files give. Do not edit by hand.

A **line** is one string a character says in a text box (a script's `say` and the question of an `ask` or `choose`, a person's `dialog`, `before` and `after`, the introduction's professor), under the key plan 17 · N4 gave it. **Spoken** lines have a spoken text (the line with the names the player types left out, which `Data/voice/lines.json` holds); **shown only** lines have none yet, because a placeholder cannot be left out gracefully, and show their text alone until one is written by hand. No audio exists yet: V3 to V5 make it, V2 plays it.

## Lines by kind

| Kind | Lines | Spoken | Shown only | With variants | Written by hand |
|---|---|---|---|---|---|
| The introduction (`intro:`) | 80 | 80 | 0 | 0 | 0 |
| People of the hand-made maps (`map:`) | 1276 | 1236 | 40 | 1 | 0 |
| Scripts (`script:`) | 8927 | 8504 | 423 | 17 | 0 |
| People of the world (`world:`) | 6287 | 6228 | 59 | 4 | 0 |
| **All** | 16570 | 16048 | 522 | 22 | 0 |

## Not voiced, by decision

Plan 26's decision 7: the game's own voice, a question's answers, signs and a Pokémon's cry.

| What | Lines |
|---|---|
| a Pokémon's cry | 210 |
| a question's answers | 542 |
| a sign | 1421 |
| the game's own voice (speaker none) | 248 |
| the game's own voice (text) | 1242 |

## The cast

- Named characters: 328 voices (291 provisional: added for a name nobody has cast yet, with nothing said about how they sound).
- Cloned from the user's samples: `rival`.
- Archetypes in use: 222, with 671 voices to cast (V5).
- Lines whose voice is the person spoken to, decided as the talk starts (`@self`): 1831.

## Lines by voice

The voices with twenty lines or more; archetypes are summed over their voices.

| Voice | Lines | Shown only |
|---|---|---|
| `@self` | 1831 | 132 |
| `look:gentleman (any)` | 626 | 13 |
| `look:grunt (any)` | 582 | 0 |
| `rival` | 571 | 17 |
| `look:youngster (any)` | 367 | 18 |
| `look:lady (any)` | 364 | 1 |
| `look:clerk (any)` | 309 | 7 |
| `look:lass (any)` | 300 | 11 |
| `look:attendant (any)` | 212 | 7 |
| `look:nurse (any)` | 201 | 0 |
| `look:gym_guide (any)` | 181 | 1 |
| `look:scientist (any)` | 176 | 4 |
| `cynthia` | 157 | 1 |
| `look:receptionist (any)` | 152 | 17 |
| `rowan` | 147 | 12 |
| `class:ace_trainer (any)` | 146 | 0 |
| `class:team_rocket (any)` | 132 | 0 |
| `look:woman (any)` | 129 | 13 |
| `class:hiker (any)` | 124 | 0 |
| `look:sailor (any)` | 123 | 0 |
| `class:fisherman (any)` | 118 | 0 |
| `class:psychic (any)` | 112 | 0 |
| `class:swimmer_m (any)` | 112 | 0 |
| `assistant` | 111 | 5 |
| `look:guard (any)` | 108 | 0 |
| `class:black_belt (any)` | 106 | 0 |
| `look:old_man (any)` | 106 | 4 |
| `look:boy (any)` | 104 | 4 |
| `mom` | 104 | 6 |
| `class:youngster (any)` | 102 | 0 |
| `look:man (any)` | 101 | 4 |
| `bianca` | 99 | 3 |
| `class:picnicker (any)` | 98 | 0 |
| `black_belt` | 96 | 14 |
| `class:cooltrainer (any)` | 96 | 0 |
| `looker` | 91 | 1 |
| `class:lass (any)` | 88 | 0 |
| `class:swimmer_f (any)` | 86 | 0 |
| `class:galactic (any)` | 84 | 0 |
| `cyrus` | 83 | 7 |
| `look:aqua_grunt (any)` | 83 | 3 |
| `class:bird_keeper (any)` | 82 | 0 |
| `look:trainer (any)` | 81 | 0 |
| `prof_oak` | 78 | 3 |
| `ace_trainer` | 75 | 0 |
| `look:old_woman (any)` | 75 | 5 |
| `look:hiker (any)` | 74 | 0 |
| `look:fisherman (any)` | 71 | 4 |
| `class:swimmer (any)` | 70 | 0 |
| `class:bug_catcher (any)` | 68 | 0 |
| `steven` | 65 | 0 |
| `class:camper (any)` | 64 | 0 |
| `class:twins (any)` | 64 | 0 |
| `look:worker (any)` | 63 | 0 |
| `prof_juniper` | 62 | 0 |
| `underground_man` | 62 | 0 |
| `beauty` | 59 | 4 |
| `lance` | 58 | 0 |
| `prof_elm` | 58 | 1 |
| `class:pokémon_ranger (any)` | 56 | 0 |
| `class:sailor (any)` | 54 | 0 |
| `cheren` | 53 | 1 |
| `archie` | 52 | 3 |
| `class:scientist (any)` | 52 | 0 |
| `bill` | 50 | 2 |
| `look:girl (any)` | 49 | 0 |
| `officer` | 47 | 5 |
| `class:beauty (any)` | 46 | 0 |
| `drayden` | 46 | 0 |
| `elder` | 46 | 0 |
| `maxie` | 46 | 1 |
| `scott` | 46 | 0 |
| `wallace` | 46 | 2 |
| `class:team_aqua (any)` | 44 | 0 |
| `class:team_magma (any)` | 44 | 0 |
| `crasher_wake` | 44 | 1 |
| `mystery` | 43 | 4 |
| `artist` | 42 | 1 |
| `buck` | 42 | 1 |
| `class:battle_girl (any)` | 42 | 0 |
| `class:worker (any)` | 42 | 0 |
| `riley` | 42 | 0 |
| `flint` | 41 | 1 |
| `look:host (any)` | 40 | 16 |
| `volkner` | 40 | 1 |
| `collector` | 39 | 0 |
| `colress` | 39 | 1 |
| `wally` | 38 | 0 |
| `class:biker (any)` | 36 | 0 |
| `class:ninja_boy (any)` | 36 | 0 |
| `depot_agent` | 36 | 5 |
| `look:magma_grunt (any)` | 36 | 0 |
| `clair` | 35 | 4 |
| `iris` | 35 | 0 |
| `look:pokefan_f (any)` | 35 | 0 |
| `class:veteran (any)` | 34 | 0 |
| `maylene` | 34 | 1 |
| `mr_briney` | 34 | 1 |
| `baoba` | 33 | 0 |
| `prof_birch` | 33 | 3 |
| `roark` | 33 | 0 |
| `rood` | 33 | 0 |
| `alder` | 32 | 1 |
| `class:channeler (any)` | 32 | 0 |
| `class:ruin_maniac (any)` | 32 | 0 |
| `gardenia` | 32 | 0 |
| `look:pokefan_m (any)` | 32 | 0 |
| `candice` | 31 | 0 |
| `jasmine` | 31 | 0 |
| `kurt` | 31 | 6 |
| `breeder` | 30 | 4 |
| `class:backpacker (any)` | 30 | 0 |
| `class:young_couple (any)` | 30 | 0 |
| `clay` | 30 | 3 |
| `zinzolin` | 30 | 0 |
| `celio` | 29 | 1 |
| `jupiter` | 29 | 0 |
| `look:aide (any)` | 29 | 4 |
| `class:triathlete (any)` | 28 | 0 |
| `look:expert_m (any)` | 28 | 0 |
| `policeman` | 28 | 0 |
| `saturn` | 28 | 0 |
| `school_kid` | 28 | 0 |
| `chairman` | 27 | 0 |
| `president` | 27 | 0 |
| `captain` | 26 | 0 |
| `class:aroma_lady (any)` | 26 | 0 |
| `class:gentleman (any)` | 26 | 0 |
| `class:guitarist (any)` | 26 | 0 |
| `look:battle_girl (any)` | 26 | 0 |
| `look:twin (any)` | 26 | 0 |
| `lottery_clerk` | 26 | 2 |
| `roxie` | 26 | 1 |
| `shadow_triad` | 26 | 7 |
| `byron` | 25 | 1 |
| `giovanni` | 25 | 2 |
| `norman` | 25 | 0 |
| `roughneck` | 25 | 0 |
| `class:ace_trainer_m (any)` | 24 | 0 |
| `class:cyclist (any)` | 24 | 0 |
| `class:sis_and_bro (any)` | 24 | 0 |
| `class:team_plasma_grunt (any)` | 24 | 0 |
| `day_care_lady` | 24 | 3 |
| `marlon` | 24 | 0 |
| `move_deleter` | 24 | 3 |
| `owner` | 24 | 0 |
| `look:galacticdoor (any)` | 23 | 23 |
| `look:plasma_grunt (any)` | 23 | 0 |
| `valet` | 23 | 5 |
| `wattson` | 23 | 0 |
| `class:ace_trainer_f (any)` | 22 | 0 |
| `class:dragon_tamer (any)` | 22 | 0 |
| `class:pokemaniac (any)` | 22 | 0 |
| `class:pokémon_breeder (any)` | 22 | 0 |
| `cook` | 22 | 0 |
| `look:ninja_boy (any)` | 22 | 0 |
| `marley` | 22 | 0 |
| `mars` | 22 | 0 |
| `schoolboy` | 22 | 0 |
| `cooltrainer` | 21 | 6 |
| `fantina` | 21 | 0 |
| `look:bug_catcher (any)` | 21 | 0 |
| `look:gymstatue (any)` | 21 | 0 |
| `backers` | 20 | 0 |
| `class:collector (any)` | 20 | 0 |
| `class:juggler (any)` | 20 | 0 |
| `class:poke_maniac (any)` | 20 | 0 |
| `class:skier (any)` | 20 | 0 |
| `class:tuber (any)` | 20 | 0 |
| `look:announcer (any)` | 20 | 12 |
| `look:pokemon_breeder_f (any)` | 20 | 0 |
| `n` | 20 | 0 |

## Casting problems

None: every speaker of every script has a cast entry.
