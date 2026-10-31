# Voice coverage

Written by `dotnet run --project tools/VoiceTool -- catalogue` (plan 26 · V1); `VoiceTests` fails when this is not what the files give. Do not edit by hand.

A **line** is one string a character says in a text box (a script's `say` and the question of an `ask` or `choose`, a person's `dialog`, `before` and `after`, the introduction's professor), under the key plan 17 · N4 gave it. **Spoken** lines have a spoken text (the line with the names the player types left out, which `Data/voice/lines.json` holds); **shown only** lines have none yet, because a placeholder cannot be left out gracefully, and show their text alone until one is written by hand. No audio exists yet: V3 to V5 make it, V2 plays it.

## Lines by kind

| Kind | Lines | Spoken | Shown only | With variants | Written by hand |
|---|---|---|---|---|---|
| The introduction (`intro:`) | 80 | 80 | 0 | 0 | 0 |
| People of the hand-made maps (`map:`) | 1276 | 1236 | 40 | 1 | 0 |
| Scripts (`script:`) | 9126 | 8698 | 428 | 17 | 0 |
| People of the world (`world:`) | 6845 | 6786 | 59 | 4 | 0 |
| **All** | 17327 | 16800 | 527 | 22 | 0 |

## Not voiced, by decision

Plan 26's decision 7: the game's own voice, a question's answers, signs and a Pokémon's cry.

| What | Lines |
|---|---|
| a Pokémon's cry | 229 |
| a question's answers | 551 |
| a sign | 1516 |
| the game's own voice (speaker none) | 250 |
| the game's own voice (text) | 1302 |

## The cast

- Named characters: 337 voices (300 provisional: added for a name nobody has cast yet, with nothing said about how they sound).
- Cloned from the user's samples: `rival`.
- Archetypes in use: 224, with 677 voices to cast (V5).
- Lines whose voice is the person spoken to, decided as the talk starts (`@self`): 1854.

## Lines by voice

The voices with twenty lines or more; archetypes are summed over their voices.

| Voice | Lines | Shown only |
|---|---|---|
| `@self` | 1854 | 132 |
| `look:gentleman (any)` | 687 | 13 |
| `look:grunt (any)` | 582 | 0 |
| `rival` | 577 | 17 |
| `look:lady (any)` | 396 | 1 |
| `look:youngster (any)` | 373 | 18 |
| `look:clerk (any)` | 329 | 7 |
| `look:lass (any)` | 308 | 11 |
| `look:nurse (any)` | 252 | 0 |
| `look:attendant (any)` | 218 | 7 |
| `look:scientist (any)` | 189 | 4 |
| `look:gym_guide (any)` | 182 | 1 |
| `cynthia` | 157 | 1 |
| `look:receptionist (any)` | 157 | 19 |
| `class:ace_trainer (any)` | 150 | 0 |
| `rowan` | 147 | 12 |
| `class:hiker (any)` | 140 | 0 |
| `look:sailor (any)` | 136 | 0 |
| `class:fisherman (any)` | 134 | 0 |
| `class:team_rocket (any)` | 132 | 0 |
| `look:woman (any)` | 129 | 13 |
| `class:swimmer_m (any)` | 126 | 0 |
| `class:picnicker (any)` | 116 | 0 |
| `class:youngster (any)` | 112 | 0 |
| `assistant` | 111 | 5 |
| `class:black_belt (any)` | 110 | 0 |
| `look:guard (any)` | 110 | 0 |
| `look:old_man (any)` | 109 | 4 |
| `class:psychic (any)` | 108 | 0 |
| `mom` | 105 | 6 |
| `look:boy (any)` | 104 | 4 |
| `class:swimmer_f (any)` | 102 | 0 |
| `look:man (any)` | 101 | 4 |
| `bianca` | 99 | 3 |
| `black_belt` | 96 | 14 |
| `class:cooltrainer (any)` | 96 | 0 |
| `class:lass (any)` | 96 | 0 |
| `looker` | 91 | 1 |
| `class:bug_catcher (any)` | 84 | 0 |
| `class:galactic (any)` | 84 | 0 |
| `cyrus` | 83 | 7 |
| `look:aqua_grunt (any)` | 83 | 3 |
| `class:bird_keeper (any)` | 82 | 0 |
| `look:trainer (any)` | 81 | 0 |
| `look:worker (any)` | 79 | 0 |
| `prof_oak` | 79 | 3 |
| `class:camper (any)` | 76 | 0 |
| `ace_trainer` | 75 | 0 |
| `look:hiker (any)` | 75 | 0 |
| `look:old_woman (any)` | 75 | 5 |
| `look:fisherman (any)` | 73 | 4 |
| `class:twins (any)` | 72 | 0 |
| `class:swimmer (any)` | 70 | 0 |
| `steven` | 65 | 0 |
| `class:biker (any)` | 64 | 0 |
| `class:sailor (any)` | 62 | 0 |
| `prof_juniper` | 62 | 0 |
| `underground_man` | 62 | 0 |
| `class:pokémon_ranger (any)` | 60 | 0 |
| `beauty` | 59 | 4 |
| `lance` | 58 | 0 |
| `prof_elm` | 58 | 1 |
| `cheren` | 53 | 1 |
| `archie` | 52 | 3 |
| `class:scientist (any)` | 52 | 0 |
| `class:worker (any)` | 52 | 0 |
| `bill` | 50 | 2 |
| `officer` | 50 | 5 |
| `look:girl (any)` | 49 | 0 |
| `class:beauty (any)` | 48 | 0 |
| `artist` | 47 | 1 |
| `class:young_couple (any)` | 46 | 0 |
| `drayden` | 46 | 0 |
| `elder` | 46 | 0 |
| `maxie` | 46 | 1 |
| `scott` | 46 | 0 |
| `wallace` | 46 | 2 |
| `class:bird_keeper_gs (any)` | 44 | 0 |
| `class:team_aqua (any)` | 44 | 0 |
| `class:team_magma (any)` | 44 | 0 |
| `crasher_wake` | 44 | 1 |
| `mystery` | 43 | 4 |
| `buck` | 42 | 1 |
| `class:battle_girl (any)` | 42 | 0 |
| `riley` | 42 | 0 |
| `flint` | 41 | 1 |
| `look:host (any)` | 40 | 16 |
| `volkner` | 40 | 1 |
| `collector` | 39 | 0 |
| `colress` | 39 | 1 |
| `wally` | 38 | 0 |
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
| `class:gentleman (any)` | 32 | 0 |
| `class:ruin_maniac (any)` | 32 | 0 |
| `gardenia` | 32 | 0 |
| `look:pokefan_m (any)` | 32 | 0 |
| `norman` | 32 | 0 |
| `candice` | 31 | 0 |
| `jasmine` | 31 | 0 |
| `kurt` | 31 | 6 |
| `breeder` | 30 | 4 |
| `class:ace_trainer_m (any)` | 30 | 0 |
| `class:backpacker (any)` | 30 | 0 |
| `clay` | 30 | 3 |
| `policeman` | 30 | 0 |
| `zinzolin` | 30 | 0 |
| `celio` | 29 | 1 |
| `jupiter` | 29 | 0 |
| `look:aide (any)` | 29 | 4 |
| `class:school_kid_m (any)` | 28 | 0 |
| `class:triathlete (any)` | 28 | 0 |
| `saturn` | 28 | 0 |
| `school_kid` | 28 | 0 |
| `chairman` | 27 | 0 |
| `look:expert_m (any)` | 27 | 0 |
| `president` | 27 | 0 |
| `captain` | 26 | 0 |
| `class:aroma_lady (any)` | 26 | 0 |
| `class:guitarist (any)` | 26 | 0 |
| `look:battle_girl (any)` | 26 | 0 |
| `look:twin (any)` | 26 | 0 |
| `lottery_clerk` | 26 | 2 |
| `roxie` | 26 | 1 |
| `shadow_triad` | 26 | 7 |
| `byron` | 25 | 1 |
| `giovanni` | 25 | 2 |
| `roughneck` | 25 | 0 |
| `class:ace_trainer_f (any)` | 24 | 0 |
| `class:cyclist (any)` | 24 | 0 |
| `class:sis_and_bro (any)` | 24 | 0 |
| `class:super_nerd (any)` | 24 | 0 |
| `class:team_plasma_grunt (any)` | 24 | 0 |
| `day_care_lady` | 24 | 3 |
| `marlon` | 24 | 0 |
| `move_deleter` | 24 | 3 |
| `owner` | 24 | 0 |
| `cook` | 23 | 0 |
| `look:galacticdoor (any)` | 23 | 23 |
| `look:plasma_grunt (any)` | 23 | 0 |
| `valet` | 23 | 5 |
| `wattson` | 23 | 0 |
| `class:dragon_tamer (any)` | 22 | 0 |
| `class:pokemaniac (any)` | 22 | 0 |
| `class:pokémon_breeder (any)` | 22 | 0 |
| `class:psychic_m (any)` | 22 | 0 |
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
| `class:double_team (any)` | 20 | 0 |
| `class:juggler (any)` | 20 | 0 |
| `class:poke_maniac (any)` | 20 | 0 |
| `class:pokefan (any)` | 20 | 0 |
| `class:skier (any)` | 20 | 0 |
| `class:tuber (any)` | 20 | 0 |
| `juan` | 20 | 0 |
| `look:announcer (any)` | 20 | 12 |
| `look:pokemon_breeder_f (any)` | 20 | 0 |
| `n` | 20 | 0 |

## Casting problems

None: every speaker of every script has a cast entry.
