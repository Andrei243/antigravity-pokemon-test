# Voice coverage

Written by `dotnet run --project tools/VoiceTool -- catalogue` (plan 26 · V1); `VoiceTests` fails when this is not what the files give. Do not edit by hand.

A **line** is one string a character says in a text box (a script's `say` and the question of an `ask` or `choose`, a person's `dialog`, `before` and `after`, the introduction's professor), under the key plan 17 · N4 gave it. **Spoken** lines have a spoken text (the line with the names the player types left out, which `Data/voice/lines.json` holds); **shown only** lines have none yet, because a placeholder cannot be left out gracefully, and show their text alone until one is written by hand. No audio exists yet: V3 to V5 make it, V2 plays it.

## Lines by kind

| Kind | Lines | Spoken | Shown only | With variants | Written by hand |
|---|---|---|---|---|---|
| battle | 122 | 119 | 3 | 0 | 0 |
| The introduction (`intro:`) | 112 | 112 | 0 | 0 | 0 |
| People of the hand-made maps (`map:`) | 1491 | 1449 | 42 | 1 | 0 |
| Scripts (`script:`) | 11237 | 10757 | 480 | 17 | 0 |
| People of the world (`world:`) | 7930 | 7868 | 62 | 4 | 0 |
| **All** | 20892 | 20305 | 587 | 22 | 0 |

## Not voiced, by decision

Plan 26's decision 7: the game's own voice, a question's answers, signs and a Pokémon's cry.

| What | Lines |
|---|---|
| a Pokémon's cry | 251 |
| a question's answers | 839 |
| a sign | 1736 |
| the game's own voice (speaker none) | 287 |
| the game's own voice (text) | 1844 |

## The cast

- Named characters: 460 voices (423 provisional: added for a name nobody has cast yet, with nothing said about how they sound).
- Cloned from the user's samples: `rival`.
- Archetypes in use: 243, with 734 voices to cast (V5).
- Lines whose voice is the person spoken to, decided as the talk starts (`@self`): 1911.

## Lines by voice

The voices with twenty lines or more; archetypes are summed over their voices.

| Voice | Lines | Shown only |
|---|---|---|
| `@self` | 1911 | 138 |
| `look:gentleman (any)` | 730 | 15 |
| `rival` | 657 | 20 |
| `look:grunt (any)` | 594 | 0 |
| `look:lady (any)` | 448 | 1 |
| `look:clerk (any)` | 433 | 11 |
| `look:youngster (any)` | 417 | 21 |
| `look:lass (any)` | 358 | 11 |
| `look:attendant (any)` | 321 | 12 |
| `look:nurse (any)` | 258 | 0 |
| `look:scientist (any)` | 232 | 6 |
| `look:gym_guide (any)` | 216 | 1 |
| `look:receptionist (any)` | 209 | 23 |
| `class:ace_trainer (any)` | 194 | 0 |
| `class:hiker (any)` | 194 | 0 |
| `class:fisherman (any)` | 172 | 0 |
| `cynthia` | 170 | 2 |
| `look:sailor (any)` | 166 | 0 |
| `class:youngster (any)` | 160 | 0 |
| `look:old_man (any)` | 157 | 4 |
| `rowan` | 147 | 12 |
| `class:lass (any)` | 146 | 0 |
| `class:picnicker (any)` | 136 | 0 |
| `look:woman (any)` | 133 | 14 |
| `class:team_rocket (any)` | 132 | 0 |
| `class:black_belt (any)` | 130 | 0 |
| `look:boy (any)` | 129 | 4 |
| `look:man (any)` | 128 | 4 |
| `class:psychic (any)` | 126 | 0 |
| `class:swimmer_m (any)` | 126 | 0 |
| `mom` | 125 | 7 |
| `look:guard (any)` | 121 | 0 |
| `looker` | 121 | 2 |
| `assistant` | 115 | 5 |
| `class:swimmer_f (any)` | 102 | 0 |
| `look:worker (any)` | 102 | 0 |
| `look:old_woman (any)` | 101 | 5 |
| `class:cooltrainer (any)` | 100 | 0 |
| `bianca` | 99 | 3 |
| `black_belt` | 99 | 14 |
| `cyrus` | 92 | 7 |
| `look:fisherman (any)` | 90 | 4 |
| `look:hiker (any)` | 90 | 0 |
| `prof_oak` | 90 | 3 |
| `steven` | 89 | 1 |
| `class:bug_catcher (any)` | 88 | 0 |
| `ace_trainer` | 84 | 0 |
| `class:bird_keeper (any)` | 84 | 0 |
| `class:galactic (any)` | 84 | 0 |
| `look:aqua_grunt (any)` | 83 | 3 |
| `class:twins (any)` | 82 | 0 |
| `class:camper (any)` | 78 | 0 |
| `class:swimmer (any)` | 78 | 0 |
| `class:veteran (any)` | 72 | 0 |
| `look:girl (any)` | 71 | 0 |
| `class:pokémon_ranger (any)` | 70 | 0 |
| `prof_juniper` | 67 | 0 |
| `class:backpacker (any)` | 64 | 0 |
| `class:biker (any)` | 64 | 0 |
| `class:sailor (any)` | 64 | 0 |
| `lance` | 64 | 0 |
| `beauty` | 63 | 4 |
| `prof_elm` | 63 | 1 |
| `class:scientist (any)` | 62 | 0 |
| `class:worker (any)` | 62 | 0 |
| `underground_man` | 62 | 0 |
| `prof_kukui` | 61 | 1 |
| `class:battle_girl (any)` | 60 | 0 |
| `artist` | 59 | 1 |
| `archie` | 55 | 3 |
| `bill` | 53 | 2 |
| `cheren` | 53 | 1 |
| `class:beauty (any)` | 52 | 0 |
| `elder` | 51 | 0 |
| `officer` | 51 | 5 |
| `class:young_couple (any)` | 50 | 0 |
| `crasher_wake` | 49 | 1 |
| `depot_agent` | 49 | 3 |
| `clair` | 48 | 4 |
| `maxie` | 48 | 1 |
| `drayden` | 46 | 0 |
| `scott` | 46 | 0 |
| `wallace` | 46 | 2 |
| `class:gentleman (any)` | 44 | 0 |
| `class:team_aqua (any)` | 44 | 0 |
| `class:team_magma (any)` | 44 | 0 |
| `flint` | 44 | 1 |
| `jasmine` | 43 | 0 |
| `look:trainer (any)` | 43 | 0 |
| `mystery` | 43 | 4 |
| `volkner` | 43 | 1 |
| `backers` | 42 | 0 |
| `buck` | 42 | 1 |
| `class:bird_keeper_gs (any)` | 42 | 0 |
| `riley` | 42 | 0 |
| `look:pokefan_f (any)` | 41 | 0 |
| `prof_sycamore` | 41 | 1 |
| `rood` | 41 | 0 |
| `look:host (any)` | 40 | 16 |
| `collector` | 39 | 0 |
| `colress` | 39 | 1 |
| `iris` | 39 | 0 |
| `look:pokefan_m (any)` | 39 | 0 |
| `owner` | 39 | 5 |
| `jupiter` | 38 | 0 |
| `prof_birch` | 38 | 3 |
| `wally` | 38 | 0 |
| `baoba` | 37 | 0 |
| `maylene` | 37 | 1 |
| `school_kid` | 37 | 0 |
| `class:ninja_boy (any)` | 36 | 0 |
| `class:ruin_maniac (any)` | 36 | 0 |
| `class:school_kid (any)` | 36 | 0 |
| `look:magma_grunt (any)` | 36 | 0 |
| `roark` | 36 | 0 |
| `roughneck` | 36 | 0 |
| `gardenia` | 35 | 0 |
| `kurt` | 35 | 6 |
| `candice` | 34 | 0 |
| `class:pokémon_breeder (any)` | 34 | 0 |
| `look:twin (any)` | 34 | 0 |
| `mars` | 34 | 0 |
| `mr_briney` | 34 | 1 |
| `saturn` | 34 | 0 |
| `hau` | 33 | 0 |
| `alder` | 32 | 1 |
| `class:ace_trainer_m (any)` | 32 | 0 |
| `class:channeler (any)` | 32 | 0 |
| `look:battle_girl (any)` | 32 | 0 |
| `look:expert_m (any)` | 32 | 0 |
| `norman` | 32 | 0 |
| `policeman` | 32 | 0 |
| `kimono_girl` | 31 | 0 |
| `breeder` | 30 | 4 |
| `captain` | 30 | 0 |
| `class:school_kid_m (any)` | 30 | 0 |
| `class:skier (any)` | 30 | 0 |
| `clay` | 30 | 3 |
| `zinzolin` | 30 | 0 |
| `celio` | 29 | 1 |
| `chairman` | 29 | 0 |
| `look:aide (any)` | 29 | 4 |
| `byron` | 28 | 1 |
| `class:dragon_tamer (any)` | 28 | 0 |
| `class:triathlete (any)` | 28 | 0 |
| `lillie` | 28 | 0 |
| `little_boy` | 28 | 0 |
| `director` | 27 | 1 |
| `lt_surge` | 27 | 0 |
| `marley` | 27 | 0 |
| `president` | 27 | 0 |
| `trick_master` | 27 | 0 |
| `brock` | 26 | 0 |
| `class:aroma_lady (any)` | 26 | 0 |
| `class:guitarist (any)` | 26 | 0 |
| `class:lady (any)` | 26 | 0 |
| `class:super_nerd (any)` | 26 | 0 |
| `lottery_clerk` | 26 | 2 |
| `roxie` | 26 | 1 |
| `sabrina` | 26 | 0 |
| `shadow_triad` | 26 | 7 |
| `blue` | 25 | 0 |
| `captain_stern` | 25 | 0 |
| `emma` | 25 | 0 |
| `erika` | 25 | 0 |
| `giovanni` | 25 | 2 |
| `misty` | 25 | 0 |
| `n` | 25 | 0 |
| `class:ace_trainer_f (any)` | 24 | 0 |
| `class:cyclist (any)` | 24 | 0 |
| `class:pokefan (any)` | 24 | 0 |
| `class:preschooler (any)` | 24 | 0 |
| `class:psychic_m (any)` | 24 | 0 |
| `class:team_plasma_grunt (any)` | 24 | 0 |
| `cook` | 24 | 0 |
| `day_care_lady` | 24 | 3 |
| `fantina` | 24 | 0 |
| `hala` | 24 | 0 |
| `marlon` | 24 | 0 |
| `move_deleter` | 24 | 3 |
| `blaine` | 23 | 0 |
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
| `class:collector (any)` | 22 | 0 |
| `class:juggler (any)` | 22 | 0 |
| `class:parasol_lady (any)` | 22 | 0 |
| `class:poke_maniac (any)` | 22 | 0 |
| `class:pokemaniac (any)` | 22 | 0 |
| `curator` | 22 | 0 |
| `look:ninja_boy (any)` | 22 | 0 |
| `look:pokemon_breeder_f (any)` | 22 | 0 |
| `schoolboy` | 22 | 0 |
| `cooltrainer` | 21 | 6 |
| `look:bug_catcher (any)` | 21 | 0 |
| `manager` | 21 | 0 |
| `tierno` | 21 | 0 |
| `veteran` | 21 | 0 |
| `class:double_team (any)` | 20 | 0 |
| `class:sis_and_bro (any)` | 20 | 0 |
| `class:tuber (any)` | 20 | 0 |
| `head_coach` | 20 | 0 |
| `juan` | 20 | 0 |
| `look:announcer (any)` | 20 | 12 |

## Casting problems

None: every speaker of every script has a cast entry.
