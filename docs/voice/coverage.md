# Voice coverage

Written by `dotnet run --project tools/VoiceTool -- catalogue` (plan 26 · V1); `VoiceTests` fails when this is not what the files give. Do not edit by hand.

A **line** is one string a character says in a text box (a script's `say` and the question of an `ask` or `choose`, a person's `dialog`, `before` and `after`, the introduction's professor), under the key plan 17 · N4 gave it. **Spoken** lines have a spoken text (the line with the names the player types left out, which `Data/voice/lines.json` holds); **shown only** lines have none yet, because a placeholder cannot be left out gracefully, and show their text alone until one is written by hand. No audio exists yet: V3 to V5 make it, V2 plays it.

## Lines by kind

| Kind | Lines | Spoken | Shown only | With variants | Written by hand |
|---|---|---|---|---|---|
| battle | 122 | 119 | 3 | 0 | 0 |
| The introduction (`intro:`) | 144 | 144 | 0 | 0 | 0 |
| People of the hand-made maps (`map:`) | 1599 | 1536 | 63 | 1 | 0 |
| Scripts (`script:`) | 12192 | 11715 | 477 | 17 | 36 |
| People of the world (`world:`) | 8052 | 7990 | 62 | 4 | 0 |
| **All** | 22109 | 21504 | 605 | 22 | 36 |

## Not voiced, by decision

Plan 26's decision 7: the game's own voice, a question's answers, signs and a Pokémon's cry.

| What | Lines |
|---|---|
| a Pokémon's cry | 259 |
| a question's answers | 925 |
| a sign | 1765 |
| the game's own voice (speaker none) | 295 |
| the game's own voice (text) | 2018 |

## The cast

- Named characters: 895 voices (336 provisional: added for a name nobody has cast yet, with nothing said about how they sound).
- Designed for Kokoro-82M (a `design`: a blend of its voices, a speed and a pitch): 554 voices.
- Archetypes in use: 246, with 743 voices to cast (V5).
- Lines whose voice is the person spoken to, decided as the talk starts (`@self`): 322.

## Lines by voice

The voices with twenty lines or more; archetypes are summed over their voices.

| Voice | Lines | Shown only |
|---|---|---|
| `look:gentleman (any)` | 1205 | 50 |
| `look:grunt (any)` | 603 | 0 |
| `rival` | 600 | 20 |
| `look:clerk (any)` | 571 | 13 |
| `look:lady (any)` | 567 | 10 |
| `look:lass (any)` | 544 | 22 |
| `look:youngster (any)` | 515 | 32 |
| `look:scientist (any)` | 325 | 13 |
| `@self` | 322 | 38 |
| `look:attendant (any)` | 321 | 12 |
| `look:nurse (any)` | 296 | 2 |
| `look:receptionist (any)` | 287 | 34 |
| `look:gym_guide (any)` | 236 | 1 |
| `class:ace_trainer (any)` | 210 | 0 |
| `class:hiker (any)` | 198 | 0 |
| `class:fisherman (any)` | 176 | 0 |
| `cynthia` | 176 | 1 |
| `look:sailor (any)` | 172 | 0 |
| `class:youngster (any)` | 164 | 0 |
| `look:old_man (any)` | 162 | 4 |
| `rowan` | 156 | 6 |
| `class:lass (any)` | 154 | 0 |
| `class:picnicker (any)` | 140 | 0 |
| `look:woman (any)` | 138 | 14 |
| `looker` | 133 | 1 |
| `mom` | 133 | 3 |
| `class:black_belt (any)` | 132 | 0 |
| `class:team_rocket (any)` | 132 | 0 |
| `class:psychic (any)` | 130 | 0 |
| `class:swimmer_m (any)` | 128 | 0 |
| `look:boy (any)` | 128 | 4 |
| `look:man (any)` | 127 | 4 |
| `assistant` | 122 | 3 |
| `look:guard (any)` | 121 | 0 |
| `look:fisherman (any)` | 119 | 4 |
| `black_belt` | 115 | 14 |
| `look:old_woman (any)` | 112 | 5 |
| `class:swimmer_f (any)` | 108 | 0 |
| `prof_oak` | 103 | 2 |
| `class:cooltrainer (any)` | 102 | 0 |
| `look:hiker (any)` | 100 | 0 |
| `bianca` | 99 | 3 |
| `ace_trainer` | 95 | 0 |
| `look:worker (any)` | 93 | 0 |
| `class:swimmer (any)` | 92 | 0 |
| `cyrus` | 92 | 7 |
| `steven` | 91 | 1 |
| `class:bug_catcher (any)` | 90 | 0 |
| `class:bird_keeper (any)` | 88 | 0 |
| `class:galactic (any)` | 84 | 0 |
| `class:twins (any)` | 84 | 0 |
| `look:aqua_grunt (any)` | 83 | 3 |
| `beauty` | 82 | 6 |
| `class:camper (any)` | 82 | 0 |
| `kanto_rival` | 79 | 0 |
| `class:veteran (any)` | 76 | 0 |
| `look:girl (any)` | 75 | 0 |
| `class:pokémon_ranger (any)` | 70 | 0 |
| `class:backpacker (any)` | 68 | 0 |
| `class:biker (any)` | 68 | 0 |
| `class:sailor (any)` | 68 | 0 |
| `prof_juniper` | 67 | 0 |
| `class:scientist (any)` | 66 | 0 |
| `lance` | 65 | 0 |
| `prof_kukui` | 64 | 1 |
| `artist` | 62 | 1 |
| `class:worker (any)` | 62 | 0 |
| `collector` | 62 | 0 |
| `underground_man` | 62 | 0 |
| `class:battle_girl (any)` | 60 | 0 |
| `prof_elm` | 59 | 1 |
| `class:beauty (any)` | 58 | 0 |
| `officer` | 57 | 5 |
| `cheren` | 56 | 1 |
| `archie` | 55 | 3 |
| `elder` | 55 | 0 |
| `crasher_wake` | 54 | 0 |
| `bill` | 53 | 0 |
| `owner` | 51 | 5 |
| `class:gentleman (any)` | 50 | 0 |
| `class:young_couple (any)` | 50 | 0 |
| `depot_agent` | 49 | 3 |
| `drayden` | 49 | 0 |
| `school_kid` | 49 | 0 |
| `clair` | 48 | 4 |
| `maxie` | 48 | 1 |
| `scott` | 48 | 0 |
| `volkner` | 48 | 1 |
| `look:pokefan_m (any)` | 47 | 0 |
| `maid` | 47 | 4 |
| `prof_birch` | 47 | 4 |
| `buck` | 46 | 1 |
| `wallace` | 46 | 2 |
| `jasmine` | 45 | 0 |
| `maylene` | 45 | 1 |
| `class:team_aqua (any)` | 44 | 0 |
| `class:team_magma (any)` | 44 | 0 |
| `prof_sycamore` | 44 | 1 |
| `riley` | 44 | 1 |
| `roark` | 44 | 0 |
| `gardenia` | 43 | 0 |
| `look:clown (any)` | 43 | 0 |
| `look:trainer (any)` | 43 | 0 |
| `mystery` | 43 | 4 |
| `class:bird_keeper_gs (any)` | 42 | 0 |
| `class:ruin_maniac (any)` | 42 | 0 |
| `flint` | 42 | 1 |
| `backers` | 41 | 0 |
| `rood` | 41 | 0 |
| `class:pokémon_breeder (any)` | 40 | 0 |
| `director` | 40 | 1 |
| `look:host (any)` | 40 | 16 |
| `look:pokefan_f (any)` | 40 | 0 |
| `look:twin (any)` | 40 | 0 |
| `candice` | 39 | 0 |
| `colress` | 39 | 1 |
| `hau` | 39 | 2 |
| `prof_laventon` | 39 | 0 |
| `jupiter` | 38 | 0 |
| `wally` | 38 | 0 |
| `baoba` | 37 | 0 |
| `breeder` | 37 | 8 |
| `iris` | 37 | 0 |
| `roughneck` | 37 | 0 |
| `byron` | 36 | 0 |
| `class:ninja_boy (any)` | 36 | 0 |
| `class:school_kid (any)` | 36 | 0 |
| `look:magma_grunt (any)` | 36 | 0 |
| `kurt` | 35 | 6 |
| `move_deleter` | 35 | 5 |
| `class:super_nerd (any)` | 34 | 0 |
| `look:battle_girl (any)` | 34 | 2 |
| `mars` | 34 | 0 |
| `mr_briney` | 34 | 1 |
| `policeman` | 34 | 0 |
| `psychic` | 34 | 1 |
| `saturn` | 34 | 0 |
| `clay` | 33 | 3 |
| `president` | 33 | 0 |
| `alder` | 32 | 1 |
| `class:ace_trainer_m (any)` | 32 | 0 |
| `class:channeler (any)` | 32 | 0 |
| `look:expert_m (any)` | 32 | 0 |
| `norman` | 32 | 0 |
| `kimono_girl` | 31 | 0 |
| `marley` | 31 | 1 |
| `class:dragon_tamer (any)` | 30 | 0 |
| `class:school_kid_m (any)` | 30 | 0 |
| `class:skier (any)` | 30 | 0 |
| `head_coach` | 30 | 1 |
| `lt_surge` | 30 | 0 |
| `zinzolin` | 30 | 0 |
| `brock` | 29 | 0 |
| `celio` | 29 | 0 |
| `chairman` | 29 | 0 |
| `fantina` | 29 | 0 |
| `little_boy` | 29 | 0 |
| `look:aide (any)` | 29 | 4 |
| `roxie` | 29 | 1 |
| `sabrina` | 29 | 0 |
| `captain` | 28 | 0 |
| `class:aroma_lady (any)` | 28 | 0 |
| `class:triathlete (any)` | 28 | 0 |
| `erika` | 28 | 0 |
| `florist` | 28 | 0 |
| `lillie` | 28 | 0 |
| `misty` | 28 | 0 |
| `hala` | 27 | 0 |
| `look:artist (any)` | 27 | 0 |
| `marlon` | 27 | 0 |
| `sailor_eldritch` | 27 | 0 |
| `trick_master` | 27 | 0 |
| `blaine` | 26 | 0 |
| `cheryl` | 26 | 1 |
| `class:guitarist (any)` | 26 | 0 |
| `class:lady (any)` | 26 | 0 |
| `class:preschooler (any)` | 26 | 0 |
| `look:butler (any)` | 26 | 0 |
| `lottery_clerk` | 26 | 2 |
| `n` | 26 | 0 |
| `shadow_triad` | 26 | 7 |
| `waitress` | 26 | 0 |
| `blue` | 25 | 0 |
| `captain_stern` | 25 | 0 |
| `emma` | 25 | 0 |
| `giovanni` | 25 | 2 |
| `park_attendant` | 25 | 1 |
| `assistant_2` | 24 | 1 |
| `class:ace_trainer_f (any)` | 24 | 0 |
| `class:cyclist (any)` | 24 | 0 |
| `class:juggler (any)` | 24 | 0 |
| `class:pokefan (any)` | 24 | 0 |
| `class:psychic_m (any)` | 24 | 0 |
| `class:team_plasma_grunt (any)` | 24 | 0 |
| `cook` | 24 | 0 |
| `day_care_lady` | 24 | 3 |
| `socialite` | 24 | 0 |
| `curator` | 23 | 0 |
| `eusine` | 23 | 0 |
| `fishing_guru` | 23 | 3 |
| `korrina` | 23 | 0 |
| `look:galacticdoor (any)` | 23 | 23 |
| `look:gymstatue (any)` | 23 | 0 |
| `look:plasma_grunt (any)` | 23 | 0 |
| `mira` | 23 | 5 |
| `nursery_aide` | 23 | 0 |
| `prof_magnolia` | 23 | 0 |
| `quiz_lady` | 23 | 0 |
| `valet` | 23 | 5 |
| `wattson` | 23 | 0 |
| `whitney` | 23 | 0 |
| `class:parasol_lady (any)` | 22 | 0 |
| `class:poke_maniac (any)` | 22 | 0 |
| `class:pokemaniac (any)` | 22 | 0 |
| `class:sis_and_bro (any)` | 22 | 0 |
| `hapu` | 22 | 2 |
| `hop` | 22 | 0 |
| `look:ninja_boy (any)` | 22 | 0 |
| `look:pokemon_breeder_f (any)` | 22 | 0 |
| `schoolboy` | 22 | 0 |
| `skyla` | 22 | 1 |
| `veteran` | 22 | 0 |
| `cooltrainer` | 21 | 6 |
| `cowgirl` | 21 | 0 |
| `look:bug_catcher (any)` | 21 | 0 |
| `look:pokemon_breeder_m (any)` | 21 | 0 |
| `manager` | 21 | 0 |
| `shauna` | 21 | 1 |
| `tierno` | 21 | 0 |
| `class:collector (any)` | 20 | 0 |
| `class:double_team (any)` | 20 | 0 |
| `class:pkmn_ranger (any)` | 20 | 0 |
| `class:tuber (any)` | 20 | 0 |
| `gladion` | 20 | 4 |
| `juan` | 20 | 0 |
| `look:announcer (any)` | 20 | 12 |
| `look:middle_aged_woman (any)` | 20 | 0 |

## Casting problems

None: every speaker of every script has a cast entry.
