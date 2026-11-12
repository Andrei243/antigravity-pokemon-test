# Voice coverage

Written by `dotnet run --project tools/VoiceTool -- catalogue` (plan 26 · V1); `VoiceTests` fails when this is not what the files give. Do not edit by hand.

A **line** is one string a character says in a text box (a script's `say` and the question of an `ask` or `choose`, a person's `dialog`, `before` and `after`, the introduction's professor), under the key plan 17 · N4 gave it. **Spoken** lines have a spoken text (the line with the names the player types left out, which `Data/voice/lines.json` holds); **shown only** lines have none yet, because a placeholder cannot be left out gracefully, and show their text alone until one is written by hand. No audio exists yet: V3 to V5 make it, V2 plays it.

## Lines by kind

| Kind | Lines | Spoken | Shown only | With variants | Written by hand |
|---|---|---|---|---|---|
| battle | 140 | 137 | 3 | 0 | 0 |
| The introduction (`intro:`) | 144 | 144 | 0 | 0 | 0 |
| People of the hand-made maps (`map:`) | 1770 | 1707 | 63 | 1 | 0 |
| Scripts (`script:`) | 13574 | 13013 | 561 | 17 | 36 |
| People of the world (`world:`) | 8183 | 8121 | 62 | 4 | 0 |
| **All** | 23811 | 23122 | 689 | 22 | 36 |

## Not voiced, by decision

Plan 26's decision 7: the game's own voice, a question's answers, signs and a Pokémon's cry.

| What | Lines |
|---|---|
| a Pokémon's cry | 260 |
| a question's answers | 1134 |
| a sign | 1771 |
| the game's own voice (speaker none) | 326 |
| the game's own voice (text) | 2237 |

## The cast

- Named characters: 980 voices (385 provisional: added for a name nobody has cast yet, with nothing said about how they sound).
- Designed for Kokoro-82M (a `design`: a blend of its voices, a speed and a pitch): 590 voices.
- Archetypes in use: 254, with 767 voices to cast (V5).
- Lines whose voice is the person spoken to, decided as the talk starts (`@self`): 391.

## Lines by voice

The voices with twenty lines or more; archetypes are summed over their voices.

| Voice | Lines | Shown only |
|---|---|---|
| `look:gentleman (any)` | 1216 | 52 |
| `look:grunt (any)` | 612 | 0 |
| `rival` | 605 | 20 |
| `look:clerk (any)` | 591 | 15 |
| `look:lady (any)` | 574 | 10 |
| `look:lass (any)` | 554 | 22 |
| `look:youngster (any)` | 521 | 32 |
| `@self` | 391 | 64 |
| `look:receptionist (any)` | 352 | 40 |
| `look:attendant (any)` | 335 | 12 |
| `look:scientist (any)` | 329 | 13 |
| `look:nurse (any)` | 297 | 2 |
| `look:gym_guide (any)` | 252 | 1 |
| `looker` | 248 | 1 |
| `class:ace_trainer (any)` | 210 | 0 |
| `class:hiker (any)` | 202 | 0 |
| `look:sailor (any)` | 189 | 0 |
| `cynthia` | 179 | 1 |
| `class:fisherman (any)` | 178 | 0 |
| `look:old_man (any)` | 171 | 4 |
| `class:youngster (any)` | 162 | 0 |
| `rowan` | 156 | 6 |
| `class:lass (any)` | 154 | 0 |
| `look:man (any)` | 152 | 9 |
| `class:picnicker (any)` | 140 | 0 |
| `mom` | 139 | 3 |
| `look:woman (any)` | 138 | 14 |
| `class:black_belt (any)` | 132 | 0 |
| `class:team_rocket (any)` | 132 | 0 |
| `class:psychic (any)` | 130 | 0 |
| `class:swimmer_m (any)` | 128 | 0 |
| `look:boy (any)` | 128 | 4 |
| `look:fisherman (any)` | 124 | 4 |
| `assistant` | 122 | 3 |
| `look:guard (any)` | 121 | 0 |
| `black_belt` | 115 | 14 |
| `look:old_woman (any)` | 112 | 5 |
| `class:swimmer_f (any)` | 108 | 0 |
| `bianca` | 103 | 3 |
| `prof_oak` | 103 | 2 |
| `class:cooltrainer (any)` | 102 | 0 |
| `look:hiker (any)` | 100 | 0 |
| `cyrus` | 99 | 7 |
| `ace_trainer` | 96 | 0 |
| `look:worker (any)` | 95 | 0 |
| `class:swimmer (any)` | 92 | 0 |
| `steven` | 91 | 1 |
| `class:bug_catcher (any)` | 90 | 0 |
| `class:bird_keeper (any)` | 88 | 0 |
| `gift_runner` | 87 | 0 |
| `class:galactic (any)` | 84 | 0 |
| `class:twins (any)` | 84 | 0 |
| `look:aqua_grunt (any)` | 83 | 3 |
| `beauty` | 82 | 6 |
| `class:camper (any)` | 82 | 0 |
| `kanto_rival` | 79 | 0 |
| `look:trainer (any)` | 77 | 0 |
| `class:veteran (any)` | 76 | 0 |
| `look:girl (any)` | 75 | 0 |
| `prof_kukui` | 71 | 1 |
| `class:pokémon_ranger (any)` | 70 | 0 |
| `prof_juniper` | 69 | 0 |
| `stall_keeper` | 69 | 12 |
| `class:backpacker (any)` | 68 | 0 |
| `class:biker (any)` | 68 | 0 |
| `class:sailor (any)` | 68 | 0 |
| `lance` | 68 | 0 |
| `class:scientist (any)` | 66 | 0 |
| `class:worker (any)` | 66 | 0 |
| `archie` | 62 | 3 |
| `artist` | 62 | 1 |
| `collector` | 62 | 0 |
| `underground_man` | 62 | 0 |
| `hop` | 61 | 0 |
| `class:battle_girl (any)` | 60 | 0 |
| `class:beauty (any)` | 60 | 0 |
| `cheren` | 59 | 1 |
| `prof_elm` | 59 | 1 |
| `officer` | 57 | 5 |
| `crasher_wake` | 55 | 0 |
| `elder` | 55 | 0 |
| `maxie` | 55 | 1 |
| `school_kid` | 54 | 0 |
| `bill` | 53 | 0 |
| `owner` | 51 | 5 |
| `class:gentleman (any)` | 50 | 0 |
| `class:young_couple (any)` | 50 | 0 |
| `drayden` | 50 | 0 |
| `clair` | 49 | 4 |
| `depot_agent` | 49 | 3 |
| `look:lockwork_agent_m (any)` | 49 | 0 |
| `volkner` | 49 | 1 |
| `wallace` | 49 | 2 |
| `scott` | 48 | 0 |
| `look:pokefan_m (any)` | 47 | 0 |
| `maid` | 47 | 4 |
| `prof_birch` | 47 | 4 |
| `buck` | 46 | 1 |
| `jasmine` | 46 | 0 |
| `maylene` | 46 | 1 |
| `prof_sycamore` | 45 | 1 |
| `roark` | 45 | 0 |
| `class:team_aqua (any)` | 44 | 0 |
| `class:team_magma (any)` | 44 | 0 |
| `gardenia` | 44 | 0 |
| `riley` | 44 | 1 |
| `flint` | 43 | 1 |
| `look:clown (any)` | 43 | 0 |
| `mystery` | 43 | 4 |
| `class:bird_keeper_gs (any)` | 42 | 0 |
| `class:ruin_maniac (any)` | 42 | 0 |
| `backers` | 41 | 0 |
| `policeman` | 41 | 0 |
| `rood` | 41 | 0 |
| `breeder` | 40 | 8 |
| `candice` | 40 | 0 |
| `class:pokémon_breeder (any)` | 40 | 0 |
| `director` | 40 | 1 |
| `iris` | 40 | 0 |
| `jupiter` | 40 | 0 |
| `look:host (any)` | 40 | 16 |
| `look:lockwork_agent_f (any)` | 40 | 0 |
| `look:pokefan_f (any)` | 40 | 0 |
| `look:twin (any)` | 40 | 0 |
| `colress` | 39 | 1 |
| `hau` | 39 | 2 |
| `prof_laventon` | 39 | 0 |
| `roughneck` | 38 | 0 |
| `wally` | 38 | 0 |
| `baoba` | 37 | 0 |
| `byron` | 37 | 0 |
| `class:ninja_boy (any)` | 36 | 0 |
| `class:school_kid (any)` | 36 | 0 |
| `look:battle_girl (any)` | 36 | 2 |
| `look:magma_grunt (any)` | 36 | 0 |
| `mars` | 36 | 0 |
| `president` | 36 | 0 |
| `kurt` | 35 | 6 |
| `move_deleter` | 35 | 5 |
| `class:super_nerd (any)` | 34 | 0 |
| `clay` | 34 | 3 |
| `look:artist (any)` | 34 | 0 |
| `mr_briney` | 34 | 1 |
| `psychic` | 34 | 1 |
| `saturn` | 34 | 0 |
| `backer` | 33 | 0 |
| `norman` | 33 | 0 |
| `socialite` | 33 | 0 |
| `waiter` | 33 | 0 |
| `alder` | 32 | 1 |
| `class:ace_trainer_m (any)` | 32 | 0 |
| `class:channeler (any)` | 32 | 0 |
| `giovanni` | 32 | 2 |
| `look:expert_m (any)` | 32 | 0 |
| `zinzolin` | 32 | 0 |
| `kimono_girl` | 31 | 0 |
| `lt_surge` | 31 | 0 |
| `marley` | 31 | 1 |
| `brock` | 30 | 0 |
| `class:dragon_tamer (any)` | 30 | 0 |
| `class:school_kid_m (any)` | 30 | 0 |
| `class:skier (any)` | 30 | 0 |
| `fantina` | 30 | 0 |
| `florist` | 30 | 0 |
| `head_coach` | 30 | 1 |
| `roxie` | 30 | 1 |
| `sabrina` | 30 | 0 |
| `celio` | 29 | 0 |
| `chairman` | 29 | 0 |
| `erika` | 29 | 0 |
| `little_boy` | 29 | 0 |
| `look:aide (any)` | 29 | 4 |
| `marlon` | 29 | 0 |
| `misty` | 29 | 0 |
| `captain` | 28 | 0 |
| `class:aroma_lady (any)` | 28 | 0 |
| `class:triathlete (any)` | 28 | 0 |
| `hala` | 28 | 0 |
| `ione` | 28 | 7 |
| `leon` | 28 | 1 |
| `lillie` | 28 | 0 |
| `blaine` | 27 | 0 |
| `sailor_eldritch` | 27 | 0 |
| `trick_master` | 27 | 0 |
| `waitress` | 27 | 0 |
| `blue` | 26 | 0 |
| `cheryl` | 26 | 1 |
| `class:guitarist (any)` | 26 | 0 |
| `class:gym_trainer (any)` | 26 | 0 |
| `class:lady (any)` | 26 | 0 |
| `class:preschooler (any)` | 26 | 0 |
| `look:butler (any)` | 26 | 0 |
| `look:middle_aged_woman (any)` | 26 | 0 |
| `lottery_clerk` | 26 | 2 |
| `n` | 26 | 0 |
| `shadow_triad` | 26 | 7 |
| `captain_stern` | 25 | 0 |
| `day_care_lady` | 25 | 3 |
| `emma` | 25 | 0 |
| `ghetsis` | 25 | 0 |
| `park_attendant` | 25 | 1 |
| `assistant_2` | 24 | 1 |
| `class:ace_trainer_f (any)` | 24 | 0 |
| `class:cyclist (any)` | 24 | 0 |
| `class:juggler (any)` | 24 | 0 |
| `class:pokefan (any)` | 24 | 0 |
| `class:psychic_m (any)` | 24 | 0 |
| `class:team_plasma_grunt (any)` | 24 | 0 |
| `cook` | 24 | 0 |
| `curator` | 24 | 0 |
| `korrina` | 24 | 0 |
| `look:camper (any)` | 24 | 0 |
| `lysandre` | 24 | 1 |
| `veteran` | 24 | 0 |
| `wattson` | 24 | 0 |
| `whitney` | 24 | 0 |
| `cup_clerk` | 23 | 0 |
| `eusine` | 23 | 0 |
| `fishing_guru` | 23 | 3 |
| `hapu` | 23 | 2 |
| `look:galacticdoor (any)` | 23 | 23 |
| `look:gymstatue (any)` | 23 | 0 |
| `look:plasma_grunt (any)` | 23 | 0 |
| `mira` | 23 | 5 |
| `nursery_aide` | 23 | 0 |
| `prof_magnolia` | 23 | 0 |
| `quiz_lady` | 23 | 0 |
| `skyla` | 23 | 1 |
| `valet` | 23 | 5 |
| `class:parasol_lady (any)` | 22 | 0 |
| `class:poke_maniac (any)` | 22 | 0 |
| `class:pokemaniac (any)` | 22 | 0 |
| `class:sis_and_bro (any)` | 22 | 0 |
| `fan` | 22 | 0 |
| `look:linebacker (any)` | 22 | 0 |
| `look:ninja_boy (any)` | 22 | 0 |
| `look:pokemon_breeder_f (any)` | 22 | 0 |
| `schoolboy` | 22 | 0 |
| `shauna` | 22 | 1 |
| `tierno` | 22 | 0 |
| `veyra` | 22 | 0 |
| `cooltrainer` | 21 | 6 |
| `cowgirl` | 21 | 0 |
| `juan` | 21 | 0 |
| `look:bug_catcher (any)` | 21 | 0 |
| `look:pokemon_breeder_m (any)` | 21 | 0 |
| `manager` | 21 | 0 |
| `traveller` | 21 | 0 |
| `bugsy` | 20 | 0 |
| `chuck` | 20 | 0 |
| `class:collector (any)` | 20 | 0 |
| `class:double_team (any)` | 20 | 0 |
| `class:pkmn_ranger (any)` | 20 | 0 |
| `class:tuber (any)` | 20 | 0 |
| `gladion` | 20 | 4 |
| `look:announcer (any)` | 20 | 12 |
| `look:rich_boy (any)` | 20 | 0 |
| `morty` | 20 | 0 |

## Casting problems

None: every speaker of every script has a cast entry.
