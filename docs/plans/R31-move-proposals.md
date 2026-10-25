# R31 · Moves past level 100: the proposal

Plan 06 · R31, the design part. Drafted 2026-10-25, **awaiting the user's approval**. Nothing here is built: no move in the data, no learnset, no effect, no picture. Every move below waits for the user's yes, or for the user's change, before a later session writes it into the data. A move the user strikes out is simply left out; the learnset rule below takes whatever is left.

The need is plan 18 · W4's decision of 2026-10-21 ([the study](W4-level-curve-study.md), "The decision"): the level cap rises to about 260, and the team carried from Sinnoh plays every later region at full strength. Today no species learns anything past level 100, so a Pokémon would climb 160 levels and learn nothing. This proposal is the pool of moves of our own that fills those levels, and the rule that hands them out.

## How to read this

- **Name**: a name of our own. None is a move, an item or an ability of the data (`moves.json`, with its Z-Moves and Max Moves; `items.json`; `abilities.json`), compared without case, spaces or punctuation. None is longer than 16 letters, the longest name the data already has (`High Jump Kick`, `Hyperspace Fury`, `Precipice Blades`), so every screen that shows a move's name holds it. The check can only know the games the data knows: a name a later game takes is changed then.
- **Type, category, power, accuracy, PP, priority, target**: the move's row as `moves.json` would carry it. A status move's power is "—"; a status move that never misses has accuracy "—" (`0` in the data), as Swords Dance's.
- **Does**: what happens in battle, in one line.
- **Machinery**: how the engine already runs it. **Every move of the pool runs on machinery the engine has today.** 68 of the 72 are their data fields alone (`ByItsFields` for a status move and the side effects of a hit in `BattleCore.Moves.cs`: `inflictStatus`, `statusChancePercent`, `flinchChancePercent`, `targetStatChange` with `alsoChangesStats` and `statChangeTargetSelf`, `critStage`, `priority`, `drainPercent`, `recoilPercent`, `healPercent`, `target`), each as a move of the data already uses them; the other 4 name an `effect` the core's table already has (`HitTwice`, `SpiritShackle`, `Psyshock` and `StealHeldItem`, one each). No move needs new code in the core. Plan 06's text for R31 expected a file of their own (`BattleCore.Effects.Ours.cs`); with this pool it stays empty, and is only made if the user asks for an effect that needs it.
- **The trainer AI reads it as**: the AI's routines go by a move's Platinum battle effect (`battleEffect` in `moves.json`, `MoveEffectId`), never by its name. Each move names the battle effect of the move it copies. Where Platinum has none that fits exactly (three stats raised at once, a heal with a stat, two stats lowered on every foe) the nearest is named and marked ≈: the AI then weighs it as that move, which is close enough and needs no routine of its own.
- **Flavour**: a line in the voice of the Pokédex, for the move's description.
- **Learned by**: how many species the learnset rule (below) gives it to, at what levels, and some of them, all worked out by a script over `species.json`.
- **Look**: the `MoveFx` template it plays until it has a recipe of its own (`MoveFx.Template`: a type's special template, `Physical` for every physical move, `SelfAura` or `StatusWaves` for a status move). Nine moves are named for recipes of their own, in plan 25's way. Its sound is the type's (`SoundBank.MoveSound` falls back to it) until a `use_<key>` is written.

## The design rules

### Four moves to a type

Each of the 18 types has four moves, 72 in all (the plan asked for 50 to 100):

| Role | What it is | Power |
|---|---|---|
| **Status** | something the user does to itself, its side or the foes: two or three stats, or a heal with a stat | — |
| **Physical** | the type's physical attack, with a small side effect | 90 to 110 |
| **Special** | the type's special attack, with a small side effect | 85 to 105 |
| **Extra** | either a utility move (priority +1, a drain, two hits, a trap, a theft, a move that hits the foe's Defense) or a **capstone**, a big attack with a drawback, learned last | utility 45 to 110; capstone 120 to 140 |

There are eight capstones (Fighting, Flying, Ground, Rock, Fire, Water, Ice, Dragon), five of them physical and three special. By category the pool is 18 status, 30 physical and 24 special moves.

### The levels, region by region

The levels the moves are learned at follow the climb W4 decided ("What each later region's levels climb to": section 4's fair climb under the modern EXP). A Pokémon is learning something in every stretch of it:

| Level | Where the carried team is (W4, section 4: the region's Champion → the team at the Champion) |
|---|---|
| 101 to 110 | Kanto: its Champion about 104 → the team about 108 |
| 110 to 140 | Johto: 136 → 140 |
| 140 to 166 | Hoenn: 162 → 166 |
| 166 to 189 | Unova: 185 → 189 |
| 189 to 209 | Kalos: 205 → 209 |
| 209 to 228 | Alola: 224 → 228 |
| 228 to 245 | Galar: 242 → 245 |
| 245 to 262 | Paldea: 258 → 262 |

Every species with moves of its own learns its first at **110** (just past Kanto's League) and its last at **250** (Paldea), with the rest spaced evenly between, so the gap between two is 20 levels for a species that learns eight and about 47 for one that learns four (the last row below).

### Balance

All of it was checked by a script over `moves.json` under the modern rules (each move's `modern` values laid over its own), the Z-Moves and Max Moves left out:

- **Nothing much above the data.** The strongest move of the data that doesn't faint its user is V-create (180); 15 percent above it is 207. The strongest of the pool is 140 (Zenith Plunge, Sunforge Blast, Serpent Crash), with a drawback each, below Hyper Beam's and Eruption's 150. **No move one-hit-KOs**, and none faints its user.
- **Power by role.** A main attack is 85 to 110 with a side effect of 10 to 30 percent, like Flamethrower, Crunch and Earth Power. A capstone of 120 to 140 has a cost the data's moves of that power have: accuracy 85 to 95 and 5 PP, and a stat drop or recoil on the user, or nothing but a lower accuracy (Skyfall Stone, hitting both foes, as Rock Slide does).
- **PP by power**, within what the data has at each power: 10 to 20 for a move of 60 or less, 5 to 15 at 61 to 110 (most 10), 5 above 110. Status moves 5 to 20 by strength (Alloy Plating's two stages on two stats has 5).
- **Against each type's own moves** (the table after the glance below): each attack's power times its accuracy, against the strongest move of its type and category in the data. Five come out above it and one level with it (Zenith Plunge ×1.00, beside Sky Attack; Ki Burst ×1.06, Magma Vein ×1.06, Mind Blade ×1.06, Umbral Pulse ×1.06, Serpent Crash ×1.05), all within the 15 percent, and all in types whose best of that category is weak (Fighting's special moves, Ground's special, Psychic's physical, Dark's special, Dragon's physical capstone).
- **Fair at level 150.** Damage grows with the level on both sides (W4, section 6: "Battles at 260 against 260 play like battles at 100"), so a move of 100 does at 150 what it does at 50. The moves give a Pokémon past 100 the same tools a level-50 Pokémon has, not more: a STAB move of 90 to 110, a status move and one more. A trainer of a later region levelled by W4's mapping keeps their own game's moves, which are as strong.

### The learnset rule

A species learns a few of the pool by its types and how it fights. The rule, as the script applies it:

1. **How it fights.** Physical if its base Attack is 10 or more above its Sp. Atk, special if the other way round, both otherwise (modern base stats). Over the 1,025 species: 490 physical, 314 special, 221 both.
2. **The list, in order** (a move it can't use, by category, is left out):
   1. its first type's status move;
   2. its first type's attack of its own category (both, for a Pokémon that fights both ways);
   3. its first type's extra, if it is a utility move;
   4. for a second type: its status move, its attack or attacks of the category, its extra if a utility move;
   5. **at least four**: a species with fewer (a single type, or a category its types don't cover) takes its first type's extra and other-category attack, then Normal's moves, until it has four;
   6. **capstones last**: its first type's capstone, then its second type's.
3. **The levels.** The first move at 110 and the last at 250, the others spaced evenly between, each rounded to 5 (four moves: 110, 155, 205, 250; five: 110, 145, 180, 215, 250; six: 110, 140, 165, 195, 220, 250; eight: every 20 from 110).
4. **Its line.** Each species has its own list from its own types, so an evolution that changes a type (Bibarel gains Water) changes what is learned after it, as the original's learnsets do. A move learned stays. A form with its own types and moves (Rotom's appliances, the regional forms) gets its own list by the same rule; a form with neither shares its species'.
5. **None for six species**, which never learn by level as others do: Ditto, Unown, Smeargle, Wobbuffet, Wynaut and Shedinja.

What it gives, over the data (1,019 species):

| Moves learned past 100 | 4 | 5 | 6 | 8 |
|---|---|---|---|---|
| Species | 400 | 346 | 183 | 90 |

Every move is learned by 27 species at the least (Umbral Pulse and Permafrost Lance) and 333 at the most (Rally Cry, Normal's status move, which is also the fill-in), about 72 on average (5,148 learnings in all).

Some of them:

| Species | Fights | Learns |
|---|---|---|
| Torterra | physical | 110 Verdant Renewal, 140 Thornlash, 165 Root Snare, 195 Dust Veil, 220 Fault Line, 250 Tectonic Rend |
| Infernape | both | 110 Kindle, 130 Cinder Stomp, 150 Pyre Lance, 170 Centered Stance, 190 Crescent Kick, 210 Ki Burst, 230 Sunforge Blast, 250 Mountain Breaker |
| Empoleon | special | 110 Tidal Ward, 155 Abyssal Current, 205 Alloy Plating, 250 Magnet Lance |
| Garchomp | physical | 110 Wyrm Dance, 140 Scale Rend, 165 Dust Veil, 195 Fault Line, 220 Serpent Crash, 250 Tectonic Rend |
| Lucario | both | 110 Centered Stance, 130 Crescent Kick, 150 Ki Burst, 170 Alloy Plating, 190 Forge Hammer, 210 Magnet Lance, 230 Rivet Shot, 250 Mountain Breaker |
| Gengar | special | 110 Grave Chill, 140 Phantom Lantern, 165 Soul Siphon, 195 Corrosive Haze, 220 Miasma Torrent, 250 Caustic Drain |
| Togekiss | special | 110 Moonlit Grace, 145 Starlight Chorus, 180 Dew Kiss, 215 Updraft, 250 Jet Stream |
| Giratina | both | 110 Grave Chill, 130 Spectral Claw, 150 Phantom Lantern, 170 Soul Siphon, 190 Wyrm Dance, 210 Scale Rend, 230 Wyrmflare, 250 Serpent Crash |
| Tyranitar | physical | 110 Monolith Pose, 140 Boulder Crush, 165 Dread Aura, 195 Rending Fang, 220 Pilfer Strike, 250 Skyfall Stone |
| Blissey | special | 110 Rally Cry, 155 Resonant Hymn, 205 Snapstep, 250 Titan Tackle |
| Staraptor | physical | 110 Rally Cry, 140 Titan Tackle, 165 Snapstep, 195 Updraft, 220 Talon Dive, 250 Zenith Plunge |
| Alakazam | special | 110 Clairvoyance, 155 Astral Pulse, 205 Mindbreak Wave, 250 Mind Blade |

## The pool at a glance

"In short" is the side effect; "Learners" and "Levels" are the rule's.

| # | Move | Type | Category | Role | Power | Acc. | PP | Prio. | Target | In short | Learners | Levels |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Rally Cry | Normal | Status | status | — | — | 20 | 0 | user and allies | self/allies Atk+1 SpA+1 | 333 | 110-250 |
| 2 | Titan Tackle | Normal | Physical | physical | 110 | 100 | 10 | 0 | one | recoil 25% | 112 | 130-250 |
| 3 | Resonant Hymn | Normal | Special | special | 100 | 100 | 10 | 0 | all foes | - | 96 | 145-250 |
| 4 | Snapstep | Normal | Physical | extra | 60 | 100 | 15 | +1 | one | priority +1 | 112 | 165-250 |
| 5 | Centered Stance | Fighting | Status | status | — | — | 20 | 0 | user | Atk+1 SpD+1 | 73 | 110-205 |
| 6 | Crescent Kick | Fighting | Physical | physical | 100 | 95 | 10 | 0 | one | 20% target Def-1 | 72 | 130-220 |
| 7 | Ki Burst | Fighting | Special | special | 100 | 90 | 10 | 0 | one | 20% target SpD-1 | 39 | 150-250 |
| 8 | Mountain Breaker | Fighting | Physical | extra (capstone) | 130 | 95 | 5 | 0 | one | user Def-1 SpD-1 | 72 | 220-250 |
| 9 | Updraft | Flying | Status | status | — | — | 15 | 0 | user | Atk+1 Spe+1 Acc+1 | 109 | 110-215 |
| 10 | Talon Dive | Flying | Physical | physical | 100 | 95 | 10 | 0 | one | crit stage +1 | 73 | 130-220 |
| 11 | Jet Stream | Flying | Special | special | 90 | 100 | 15 | 0 | one | 20% flinch | 62 | 150-250 |
| 12 | Zenith Plunge | Flying | Physical | extra (capstone) | 140 | 90 | 5 | 0 | one | recoil 33% | 72 | 220-250 |
| 13 | Corrosive Haze | Poison | Status | status | — | 100 | 15 | 0 | all foes | foes Def-1 SpD-1 | 83 | 110-215 |
| 14 | Venom Lance | Poison | Physical | physical | 95 | 100 | 10 | 0 | one | 30% poison | 56 | 130-250 |
| 15 | Miasma Torrent | Poison | Special | special | 100 | 95 | 10 | 0 | one | 30% bad poison | 53 | 140-250 |
| 16 | Caustic Drain | Poison | Special | extra | 75 | 100 | 10 | 0 | one | drain 50% | 53 | 165-250 |
| 17 | Dust Veil | Ground | Status | status | — | 100 | 15 | 0 | all foes | foes Acc-1 | 75 | 110-215 |
| 18 | Fault Line | Ground | Physical | physical | 110 | 95 | 10 | 0 | all others | - | 69 | 130-220 |
| 19 | Magma Vein | Ground | Special | special | 100 | 95 | 10 | 0 | one | 20% burn | 32 | 150-250 |
| 20 | Tectonic Rend | Ground | Physical | extra (capstone) | 130 | 85 | 5 | 0 | one | crit stage +1 | 69 | 220-250 |
| 21 | Monolith Pose | Rock | Status | status | — | — | 10 | 0 | user | Atk+1 Def+1 SpD+1 | 74 | 110-205 |
| 22 | Boulder Crush | Rock | Physical | physical | 100 | 90 | 10 | 0 | one | 20% flinch | 63 | 130-220 |
| 23 | Crystal Lance | Rock | Special | special | 95 | 100 | 10 | 0 | one | crit stage +1 | 38 | 145-250 |
| 24 | Skyfall Stone | Rock | Physical | extra (capstone) | 120 | 85 | 5 | 0 | all foes | - | 63 | 220-250 |
| 25 | Molt Renewal | Bug | Status | status | — | — | 10 | 0 | user | heal 25% + Spe+1 | 91 | 110-180 |
| 26 | Mandible Crush | Bug | Physical | physical | 100 | 95 | 10 | 0 | one | 20% target Def-1 | 69 | 130-250 |
| 27 | Swarm Chorus | Bug | Special | special | 85 | 100 | 10 | 0 | all foes | 10% target SpA-1 | 54 | 145-250 |
| 28 | Darting Sting | Bug | Physical | extra | 50 | 100 | 20 | +1 | one | priority +1 | 69 | 165-250 |
| 29 | Grave Chill | Ghost | Status | status | — | 100 | 15 | 0 | one | target Atk-1 SpA-1 | 64 | 110-215 |
| 30 | Spectral Claw | Ghost | Physical | physical | 95 | 100 | 10 | 0 | one | crit stage +1 | 43 | 130-250 |
| 31 | Phantom Lantern | Ghost | Special | special | 100 | 95 | 10 | 0 | one | 20% burn | 43 | 140-250 |
| 32 | Soul Siphon | Ghost | Special | extra | 80 | 100 | 10 | 0 | one | drain 50% | 43 | 165-250 |
| 33 | Alloy Plating | Steel | Status | status | — | — | 5 | 0 | user | Def+2 SpD+2 | 65 | 110-205 |
| 34 | Forge Hammer | Steel | Physical | physical | 110 | 95 | 10 | 0 | one | user Spe-1 | 53 | 130-220 |
| 35 | Magnet Lance | Steel | Special | special | 95 | 100 | 10 | 0 | one | 20% target SpD-1 | 33 | 145-250 |
| 36 | Rivet Shot | Steel | Physical | extra | 45 | 100 | 10 | 0 | one | hits twice | 53 | 165-250 |
| 37 | Kindle | Fire | Status | status | — | — | 20 | 0 | user | SpA+1 Spe+1 | 81 | 110-215 |
| 38 | Cinder Stomp | Fire | Physical | physical | 100 | 95 | 10 | 0 | one | 30% burn | 57 | 130-250 |
| 39 | Pyre Lance | Fire | Special | special | 105 | 95 | 10 | 0 | one | 10% burn | 70 | 140-230 |
| 40 | Sunforge Blast | Fire | Special | extra (capstone) | 140 | 90 | 5 | 0 | one | user SpA-2 | 59 | 230-250 |
| 41 | Tidal Ward | Water | Status | status | — | — | 10 | 0 | user | heal 25% + SpD+1 | 154 | 110-205 |
| 42 | Riptide Slam | Water | Physical | physical | 100 | 95 | 10 | 0 | one | 20% target Spe-1 | 121 | 130-220 |
| 43 | Abyssal Current | Water | Special | special | 100 | 95 | 10 | 0 | one | 30% target Spe-1 | 121 | 145-250 |
| 44 | Maelstrom Crash | Water | Physical | extra (capstone) | 130 | 90 | 5 | 0 | one | recoil 33% | 99 | 220-250 |
| 45 | Verdant Renewal | Grass | Status | status | — | — | 10 | 0 | user | heal 33% + Def+1 | 127 | 110-205 |
| 46 | Thornlash | Grass | Physical | physical | 100 | 95 | 10 | 0 | one | 20% flinch | 96 | 130-250 |
| 47 | Sap Lance | Grass | Special | special | 85 | 100 | 10 | 0 | one | drain 50% | 99 | 145-250 |
| 48 | Root Snare | Grass | Physical | extra | 80 | 100 | 15 | 0 | one | target can't flee or switch | 96 | 165-250 |
| 49 | Overcharge | Electric | Status | status | — | — | 10 | 0 | user | SpA+1 SpD+1 Spe+1 | 69 | 110-205 |
| 50 | Arc Lance | Electric | Physical | physical | 100 | 95 | 10 | 0 | one | 20% paralysis | 47 | 130-250 |
| 51 | Ion Cascade | Electric | Special | special | 95 | 100 | 10 | 0 | one | 20% paralysis | 58 | 145-250 |
| 52 | Voltstep | Electric | Physical | extra | 50 | 100 | 20 | +1 | one | priority +1, 10% paralysis | 47 | 165-250 |
| 53 | Clairvoyance | Psychic | Status | status | — | — | 20 | 0 | user | SpA+1 Acc+1 | 99 | 110-215 |
| 54 | Mind Blade | Psychic | Physical | physical | 90 | 100 | 10 | 0 | one | crit stage +1 | 62 | 130-250 |
| 55 | Astral Pulse | Psychic | Special | special | 100 | 95 | 10 | 0 | one | 20% target SpD-1 | 85 | 140-230 |
| 56 | Mindbreak Wave | Psychic | Special | extra | 110 | 95 | 5 | 0 | one | hits the target's Defense | 85 | 165-250 |
| 57 | Hoarfrost | Ice | Status | status | — | 95 | 15 | 0 | all foes | foes Atk-1 Spe-1 | 48 | 110-215 |
| 58 | Glacier Ram | Ice | Physical | physical | 110 | 90 | 10 | 0 | one | 10% freeze | 36 | 130-250 |
| 59 | Whiteout Gale | Ice | Special | special | 90 | 95 | 10 | 0 | all foes | 10% freeze | 35 | 140-230 |
| 60 | Permafrost Lance | Ice | Special | extra (capstone) | 120 | 85 | 5 | 0 | one | crit stage +1 | 27 | 250-250 |
| 61 | Wyrm Dance | Dragon | Status | status | — | — | 10 | 0 | user | Atk+1 SpA+1 Spe+1 | 70 | 110-215 |
| 62 | Scale Rend | Dragon | Physical | physical | 100 | 95 | 10 | 0 | one | 20% target Def-1 | 48 | 130-220 |
| 63 | Wyrmflare | Dragon | Special | special | 100 | 95 | 10 | 0 | one | 10% burn | 42 | 145-250 |
| 64 | Serpent Crash | Dragon | Physical | extra (capstone) | 140 | 90 | 5 | 0 | one | user Atk-1 Def-1 | 47 | 220-250 |
| 65 | Dread Aura | Dark | Status | status | — | 100 | 15 | 0 | all foes | foes SpD-1 Spe-1 | 69 | 110-195 |
| 66 | Rending Fang | Dark | Physical | physical | 100 | 95 | 10 | 0 | one | 20% flinch | 61 | 130-250 |
| 67 | Umbral Pulse | Dark | Special | special | 100 | 95 | 10 | 0 | one | 30% target SpA-1 | 27 | 145-250 |
| 68 | Pilfer Strike | Dark | Physical | extra | 70 | 100 | 15 | 0 | one | takes the target's item | 61 | 165-250 |
| 69 | Moonlit Grace | Fairy | Status | status | — | — | 10 | 0 | user | heal 33% + SpA+1 | 64 | 110-215 |
| 70 | Fey Strike | Fairy | Physical | physical | 100 | 95 | 10 | 0 | one | 20% target Atk-1 | 38 | 130-250 |
| 71 | Starlight Chorus | Fairy | Special | special | 90 | 100 | 10 | 0 | all foes | 10% target SpA-1 | 55 | 145-250 |
| 72 | Dew Kiss | Fairy | Special | extra | 80 | 100 | 10 | 0 | one | drain 50% | 55 | 170-250 |

## Against each type’s own moves

Each attack beside the moves of the data nearest it in power (power/accuracy/PP, modern values), and its power times its accuracy against the strongest move of its type and category in the data (moves that faint their user included). Worked out by the script; nothing is above ×1.15.

| Move | Type | Category | Power | Acc. | PP | Nearest moves of the data (same type and category, power within 15) | Against the type’s best |
|---|---|---|---|---|---|---|---|
| Titan Tackle | Normal | Physical | 110 | 100 | 10 | Mega Kick 120/75/5; Thrash 120/100/10; Double-Edge 120/100/15 | ×0.44 |
| Resonant Hymn | Normal | Special | 100 | 100 | 10 | Judgment 100/100/10; Uproar 90/100/10; Hyper Voice 90/100/10 | ×0.71 |
| Snapstep | Normal | Physical | 60 | 100 | 15 | Covet 60/100/25; Vise Grip 55/100/30; Stomp 65/100/20 | ×0.24 |
| Crescent Kick | Fighting | Physical | 100 | 95 | 10 | Jump Kick 100/95/10; Dynamic Punch 100/50/5; Cross Chop 100/80/5 | ×0.63 |
| Ki Burst | Fighting | Special | 100 | 90 | 10 | Secret Sword 85/100/10 | ×1.06 |
| Mountain Breaker | Fighting | Physical | 130 | 95 | 5 | High Jump Kick 130/90/10; Superpower 120/100/5; Close Combat 120/100/5 | ×0.82 |
| Talon Dive | Flying | Physical | 100 | 95 | 10 | Beak Blast 100/100/15; Fly 90/95/15; Floaty Fall 90/95/15 | ×0.75 |
| Jet Stream | Flying | Special | 90 | 100 | 15 | Aeroblast 100/95/5; Oblivion Wing 80/100/10; Bleakwind Storm 100/80/10 | ×0.95 |
| Zenith Plunge | Flying | Physical | 140 | 90 | 5 | Sky Attack 140/90/5 | ×1.00 |
| Venom Lance | Poison | Physical | 95 | 100 | 10 | Noxious Torque 100/100/10; Poison Jab 80/100/20; Dire Claw 80/100/15 | ×0.95 |
| Miasma Torrent | Poison | Special | 100 | 95 | 10 | Malignant Chain 100/100/5; Sludge Wave 95/100/10; Sludge Bomb 90/100/10 | ×0.88 |
| Caustic Drain | Poison | Special | 75 | 100 | 10 | Sludge 65/100/20; Venoshock 65/100/10; Sludge Bomb 90/100/10 | ×0.69 |
| Fault Line | Ground | Physical | 110 | 95 | 10 | Earthquake 100/100/10; Precipice Blades 120/85/10; Headlong Rush 120/100/5 | ×0.87 |
| Magma Vein | Ground | Special | 100 | 95 | 10 | Sandsear Storm 100/80/10; Earth Power 90/100/10 | ×1.06 |
| Tectonic Rend | Ground | Physical | 130 | 85 | 5 | Precipice Blades 120/85/10; Headlong Rush 120/100/5 | ×0.92 |
| Boulder Crush | Rock | Physical | 100 | 90 | 10 | Stone Edge 100/80/5; Diamond Storm 100/95/5; Mighty Cleave 95/100/5 | ×0.67 |
| Crystal Lance | Rock | Special | 95 | 100 | 10 | Power Gem 80/100/20 | ×0.88 |
| Skyfall Stone | Rock | Physical | 120 | 85 | 5 | none | ×0.76 |
| Mandible Crush | Bug | Physical | 100 | 95 | 10 | Attack Order 90/100/15; First Impression 90/100/10 | ×0.93 |
| Swarm Chorus | Bug | Special | 85 | 100 | 10 | Bug Buzz 90/100/10; Pollen Puff 90/100/15; Signal Beam 75/100/15 | ×0.94 |
| Darting Sting | Bug | Physical | 50 | 100 | 20 | Fell Stinger 50/100/25; Pounce 50/100/20; Fury Cutter 40/95/20 | ×0.49 |
| Spectral Claw | Ghost | Physical | 95 | 100 | 10 | Phantom Force 90/100/10; Spectral Thief 90/100/10; Shadow Bone 85/100/10 | ×0.79 |
| Phantom Lantern | Ghost | Special | 100 | 95 | 10 | Moongeist Beam 100/100/5 | ×0.79 |
| Soul Siphon | Ghost | Special | 80 | 100 | 10 | Shadow Ball 80/100/15; Bitter Malice 75/100/10; Hex 65/100/10 | ×0.67 |
| Forge Hammer | Steel | Physical | 110 | 95 | 10 | Iron Tail 100/75/15; Sunsteel Strike 100/100/5; Behemoth Blade 100/100/5 | ×0.65 |
| Magnet Lance | Steel | Special | 95 | 100 | 10 | Flash Cannon 80/100/10 | ×0.68 |
| Rivet Shot | Steel | Physical | 45 | 100 | 10 | Metal Claw 50/95/35; Bullet Punch 40/100/30; Gear Grind 50/85/15 | ×0.28 |
| Cinder Stomp | Fire | Physical | 100 | 95 | 10 | Sacred Fire 100/95/5; Bitter Blade 90/100/10; Blaze Kick 85/90/10 | ×0.56 |
| Pyre Lance | Fire | Special | 105 | 95 | 10 | Fire Blast 110/85/5; Magma Storm 100/75/5; Inferno 100/50/5 | ×0.67 |
| Sunforge Blast | Fire | Special | 140 | 90 | 5 | Eruption 150/100/5; Blast Burn 150/90/5; Overheat 130/90/5 | ×0.84 |
| Riptide Slam | Water | Physical | 100 | 95 | 10 | Crabhammer 100/90/10; Aqua Tail 90/90/10; Liquidation 85/100/10 | ×0.79 |
| Abyssal Current | Water | Special | 100 | 95 | 10 | Hydro Pump 110/80/5; Surf 90/100/15; Muddy Water 90/85/10 | ×0.63 |
| Maelstrom Crash | Water | Physical | 130 | 90 | 5 | Wave Crash 120/100/10 | ×0.97 |
| Thornlash | Grass | Physical | 100 | 95 | 10 | Sappy Seed 100/90/10; Ivy Cudgel 100/100/10; Leaf Blade 90/100/15 | ×0.76 |
| Sap Lance | Grass | Special | 85 | 100 | 10 | Energy Ball 90/100/10; Grass Pledge 80/100/10; Apple Acid 80/100/10 | ×0.60 |
| Root Snare | Grass | Physical | 80 | 100 | 15 | Seed Bomb 80/100/15; Drum Beating 80/100/10; Grav Apple 80/100/10 | ×0.64 |
| Arc Lance | Electric | Physical | 100 | 95 | 10 | Fusion Bolt 100/100/5; Plasma Fists 100/100/15; Supercell Slam 100/95/15 | ×0.79 |
| Ion Cascade | Electric | Special | 95 | 100 | 10 | Thunderbolt 90/100/15; Wildbolt Storm 100/80/10; Electro Drift 100/100/5 | ×0.73 |
| Voltstep | Electric | Physical | 50 | 100 | 20 | Spark 65/100/20; Thunder Fang 65/95/15 | ×0.42 |
| Mind Blade | Psychic | Physical | 90 | 100 | 10 | Psychic Fangs 85/100/10; Zen Headbutt 80/90/15; Psyblade 80/100/15 | ×1.06 |
| Astral Pulse | Psychic | Special | 100 | 95 | 10 | Dream Eater 100/100/15; Psystrike 100/100/10; Photon Geyser 100/100/5 | ×0.59 |
| Mindbreak Wave | Psychic | Special | 110 | 95 | 5 | Dream Eater 100/100/15; Future Sight 120/100/10; Synchronoise 120/100/10 | ×0.65 |
| Glacier Ram | Ice | Physical | 110 | 90 | 10 | Ice Hammer 100/90/10; Glacial Lance 120/100/5; Mountain Gale 100/85/10 | ×0.79 |
| Whiteout Gale | Ice | Special | 90 | 95 | 10 | Ice Beam 90/100/10; Freezy Frost 100/90/10 | ×0.68 |
| Permafrost Lance | Ice | Special | 120 | 85 | 5 | Blizzard 110/70/5 | ×0.81 |
| Scale Rend | Dragon | Physical | 100 | 95 | 10 | Dragon Rush 100/75/10; Dragon Hammer 90/100/15 | ×0.79 |
| Wyrmflare | Dragon | Special | 100 | 95 | 10 | Spacial Rend 100/95/5; Core Enforcer 100/100/10; Dynamax Cannon 100/100/5 | ×0.63 |
| Serpent Crash | Dragon | Physical | 140 | 90 | 5 | none | ×1.05 |
| Rending Fang | Dark | Physical | 100 | 95 | 10 | Hyperspace Fury 100/-/5; Foul Play 95/100/15; Darkest Lariat 85/100/10 | ×0.95 |
| Umbral Pulse | Dark | Special | 100 | 95 | 10 | Fiery Wrath 90/100/10; Night Daze 85/95/10 | ×1.06 |
| Pilfer Strike | Dark | Physical | 70 | 100 | 15 | Sucker Punch 70/100/5; Night Slash 70/100/15; Knock Off 65/100/20 | ×0.70 |
| Fey Strike | Fairy | Physical | 100 | 95 | 10 | Magical Torque 100/100/10; Play Rough 90/90/10 | ×0.95 |
| Starlight Chorus | Fairy | Special | 90 | 100 | 10 | Strange Steam 90/95/10; Moonblast 95/100/15; Dazzling Gleam 80/100/10 | ×0.71 |
| Dew Kiss | Fairy | Special | 80 | 100 | 10 | Dazzling Gleam 80/100/10; Alluring Voice 80/100/10; Strange Steam 90/95/10 | ×0.63 |

## The seventy-two


### Normal

#### 1. Rally Cry

- **Normal · Status** · power — · accuracy — · 20 PP · priority 0 · target: user and allies · flags: Sound, Snatch
- **Does**: Raises the Attack and Sp. Atk of the user and its allies by one stage.
- **Machinery**: data fields alone (no `effect`): `targetStatChange` Attack +1, `alsoChangesStats` SpAttack, on the user's side (`target` UserAndAllies, as Lunar Blessing's). The trainer AI reads it as `AtkUp`≈.
- **Flavour**: A shout that steadies a whole team before the hard part of a journey.
- **Learned by** (the rule below): 333 species, at levels 110-250; for example Blastoise 215, Machamp 215, Snorlax 110, Staraptor 110.
- **Look**: `SelfAura`, on each Pokémon it raises; its sound the type's until a `use_<key>` of its own is written.

#### 2. Titan Tackle

- **Normal · Physical** · power 110 · accuracy 100 · 10 PP · priority 0 · target: one · flags: Contact
- **Does**: Hits; the user takes a quarter of the damage dealt as recoil.
- **Machinery**: data fields alone (no `effect`): `recoilPercent` 25, as Take Down. The trainer AI reads it as `RecoilQuarter`.
- **Flavour**: Everything the body has learned in a hundred levels, thrown at once.
- **Learned by** (the rule below): 112 species, at levels 130-250; for example Snorlax 155, Staraptor 140, Bibarel 140, Ambipom 155.
- **Look**: `Physical`; its sound the type's until a `use_<key>` of its own is written.

#### 3. Resonant Hymn

- **Normal · Special** · power 100 · accuracy 100 · 10 PP · priority 0 · target: all foes · flags: Sound
- **Does**: Hits every foe; a sound move.
- **Machinery**: data fields alone (no `effect`): `target` AllFoes, the Sound flag, as Hyper Voice. The trainer AI reads it as `Hit`.
- **Flavour**: A long, clear note that the foes feel in their bones.
- **Learned by** (the rule below): 96 species, at levels 145-250; for example Snorlax 250, Ambipom 250, Lopunny 250, Purugly 250.
- **Look**: Normal's special template (`PowerOrb`); its cry in a mode of its own as Hyper Voice's, listed in `SoundBank.CryModesOf`; its sound the type's until a `use_<key>` of its own is written.

#### 4. Snapstep

- **Normal · Physical** · power 60 · accuracy 100 · 15 PP · priority +1 · target: one · flags: Contact
- **Does**: Hits first (priority +1).
- **Machinery**: data fields alone (no `effect`): `priority` 1, as Jet Punch. The trainer AI reads it as `Priority1`.
- **Flavour**: A step taken before the foe has finished thinking.
- **Learned by** (the rule below): 112 species, at levels 165-250; for example Snorlax 205, Staraptor 165, Bibarel 165, Ambipom 205.
- **Look**: `Physical`; its sound the type's until a `use_<key>` of its own is written.

### Fighting

#### 5. Centered Stance

- **Fighting · Status** · power — · accuracy — · 20 PP · priority 0 · target: user · flags: Snatch
- **Does**: Raises the user's Attack and Sp. Def by one stage.
- **Machinery**: data fields alone (no `effect`): Attack +1, `alsoChangesStats` SpDefense, self. The trainer AI reads it as `AtkDefUp`≈.
- **Flavour**: A breath held low in the belly; the blow and the guard together.
- **Learned by** (the rule below): 73 species, at levels 110-205; for example Machamp 110, Infernape 170, Lucario 110, Toxicroak 180.
- **Look**: `SelfAura`; its sound the type's until a `use_<key>` of its own is written.

#### 6. Crescent Kick

- **Fighting · Physical** · power 100 · accuracy 95 · 10 PP · priority 0 · target: one · flags: Contact
- **Does**: Hits; 20% chance to lower the target's Defense by one stage.
- **Machinery**: data fields alone (no `effect`): `targetStatChange` Defense -1 at 20%, as Crunch. The trainer AI reads it as `LowerDefenseHit`.
- **Flavour**: A kick that sweeps up and round like the new moon.
- **Learned by** (the rule below): 72 species, at levels 130-220; for example Machamp 145, Infernape 190, Lucario 130, Toxicroak 215.
- **Look**: `Physical`; its sound the type's until a `use_<key>` of its own is written.

#### 7. Ki Burst

- **Fighting · Special** · power 100 · accuracy 90 · 10 PP · priority 0 · target: one
- **Does**: Hits; 20% chance to lower the target's Sp. Def by one stage.
- **Machinery**: data fields alone (no `effect`): SpDefense -1 at 20%, as Focus Blast. The trainer AI reads it as `LowerSpDefHit`.
- **Flavour**: The inner strength let go in one bright push.
- **Learned by** (the rule below): 39 species, at levels 150-250; for example Machamp 180, Infernape 210, Lucario 150.
- **Look**: Fighting's special template (`PowerOrb`); its sound the type's until a `use_<key>` of its own is written.

#### 8. Mountain Breaker

- **Fighting · Physical** · power 130 · accuracy 95 · 5 PP · priority 0 · target: one · flags: Contact, Punch
- **Does**: Hits; lowers the user's Defense and Sp. Def by one stage.
- **Machinery**: data fields alone (no `effect`): Defense -1 and SpDefense -1 on the user, as Close Combat. The trainer AI reads it as `DefSpdDownHit`.
- **Flavour**: A punch that would split a peak, and leaves its user open.
- **Learned by** (the rule below): 72 species, at levels 220-250; for example Machamp 250, Infernape 250, Lucario 250, Toxicroak 250.
- **Look**: a recipe of its own (a capstone); its sound the type's until a `use_<key>` of its own is written.

### Flying

#### 9. Updraft

- **Flying · Status** · power — · accuracy — · 15 PP · priority 0 · target: user · flags: Snatch
- **Does**: Raises the user's Attack, Speed and accuracy by one stage.
- **Machinery**: data fields alone (no `effect`): Attack +1, `alsoChangesStats` Speed and Accuracy, self. The trainer AI reads it as `AtkSpdUp`≈.
- **Flavour**: It rides the warm air up and picks its moment.
- **Learned by** (the rule below): 109 species, at levels 110-215; for example Charizard 180, Gyarados 165, Dragonite 165, Salamence 165.
- **Look**: `SelfAura`; its sound the type's until a `use_<key>` of its own is written.

#### 10. Talon Dive

- **Flying · Physical** · power 100 · accuracy 95 · 10 PP · priority 0 · target: one · flags: Contact
- **Does**: Hits; a high chance of a critical hit.
- **Machinery**: data fields alone (no `effect`): `critStage` 1, as Stone Edge. The trainer AI reads it as `HighCritical`.
- **Flavour**: A stoop from on high, talons first.
- **Learned by** (the rule below): 73 species, at levels 130-220; for example Gyarados 195, Dragonite 195, Salamence 195, Staraptor 220.
- **Look**: `Physical`; its sound the type's until a `use_<key>` of its own is written.

#### 11. Jet Stream

- **Flying · Special** · power 90 · accuracy 100 · 15 PP · priority 0 · target: one
- **Does**: Hits; 20% chance to make the target flinch.
- **Machinery**: data fields alone (no `effect`): `flinchChancePercent` 20, as Dark Pulse. The trainer AI reads it as `FlinchHit`.
- **Flavour**: A blade of wind fast enough to stop a foe in its tracks.
- **Learned by** (the rule below): 62 species, at levels 150-250; for example Charizard 215, Mothim 230, Vespiquen 230, Chatot 250.
- **Look**: Flying's special template (`WindStream`); its sound the type's until a `use_<key>` of its own is written.

#### 12. Zenith Plunge

- **Flying · Physical** · power 140 · accuracy 90 · 5 PP · priority 0 · target: one · flags: Contact
- **Does**: Hits; the user takes a third of the damage dealt as recoil.
- **Machinery**: data fields alone (no `effect`): `recoilPercent` 33, as Brave Bird. The trainer AI reads it as `RecoilThird`.
- **Flavour**: It climbs until the sky goes dark, then falls like a star.
- **Learned by** (the rule below): 72 species, at levels 220-250; for example Gyarados 250, Dragonite 250, Salamence 250, Staraptor 250.
- **Look**: a recipe of its own (a capstone); its sound the type's until a `use_<key>` of its own is written.

### Poison

#### 13. Corrosive Haze

- **Poison · Status** · power — · accuracy 100 · 15 PP · priority 0 · target: all foes · flags: Reflectable
- **Does**: Lowers every foe's Defense and Sp. Def by one stage.
- **Machinery**: data fields alone (no `effect`): `target` AllFoes, Defense -1 and SpDefense -1, as Growl's reach with Tickle's pair. The trainer AI reads it as `LowerDefenseHit`≈ (the AI's status routines read it as a Defense drop).
- **Flavour**: A sour mist that eats at scale and hide alike.
- **Learned by** (the rule below): 83 species, at levels 110-215; for example Venusaur 180, Gengar 195, Roserade 180, Skuntank 110.
- **Look**: `StatusWaves`; its sound the type's until a `use_<key>` of its own is written.

#### 14. Venom Lance

- **Poison · Physical** · power 95 · accuracy 100 · 10 PP · priority 0 · target: one
- **Does**: Hits; 30% chance to poison.
- **Machinery**: data fields alone (no `effect`): `inflictStatus` Poison at 30%, as Gunk Shot. The trainer AI reads it as `PoisonHit`.
- **Flavour**: A spine dripping with venom, driven home.
- **Learned by** (the rule below): 56 species, at levels 130-250; for example Skuntank 145, Drapion 145, Toxicroak 145.
- **Look**: `Physical`; its sound the type's until a `use_<key>` of its own is written.

#### 15. Miasma Torrent

- **Poison · Special** · power 100 · accuracy 95 · 10 PP · priority 0 · target: one
- **Does**: Hits; 30% chance to badly poison.
- **Machinery**: data fields alone (no `effect`): `inflictStatus` Toxic at 30%, as Poison Fang. The trainer AI reads it as `BadlyPoisonHit`.
- **Flavour**: A flood of poison thick enough to stay in the blood.
- **Learned by** (the rule below): 53 species, at levels 140-250; for example Venusaur 215, Gengar 220, Roserade 215.
- **Look**: Poison's special template (`SludgeArc`); its sound the type's until a `use_<key>` of its own is written.

#### 16. Caustic Drain

- **Poison · Special** · power 75 · accuracy 100 · 10 PP · priority 0 · target: one
- **Does**: Hits; the user heals half the damage dealt.
- **Machinery**: data fields alone (no `effect`): `drainPercent` 50, as Giga Drain. The trainer AI reads it as `RecoverHalfDamageDealt`.
- **Flavour**: Acid that dissolves strength and carries it home.
- **Learned by** (the rule below): 53 species, at levels 165-250; for example Venusaur 250, Gengar 250, Roserade 250.
- **Look**: Poison's special template (`SludgeArc`); its sound the type's until a `use_<key>` of its own is written.

### Ground

#### 17. Dust Veil

- **Ground · Status** · power — · accuracy 100 · 15 PP · priority 0 · target: all foes · flags: Reflectable
- **Does**: Lowers every foe's accuracy by one stage.
- **Machinery**: data fields alone (no `effect`): `target` AllFoes, Accuracy -1, as Sand Attack over a whole side. The trainer AI reads it as `AccDown`.
- **Flavour**: Dust thrown up in a wall the foes can't see through.
- **Learned by** (the rule below): 75 species, at levels 110-215; for example Torterra 195, Gastrodon 170, Garchomp 165, Hippowdon 110.
- **Look**: `StatusWaves`; its sound the type's until a `use_<key>` of its own is written.

#### 18. Fault Line

- **Ground · Physical** · power 110 · accuracy 95 · 10 PP · priority 0 · target: all others
- **Does**: Hits every other Pokémon on the field, its ally too.
- **Machinery**: data fields alone (no `effect`): `target` AllOthers, as Earthquake (without Dig's double). The trainer AI reads it as `Hit`.
- **Flavour**: The ground opens along a line and snaps shut.
- **Learned by** (the rule below): 69 species, at levels 130-220; for example Torterra 220, Gastrodon 190, Garchomp 195, Hippowdon 145.
- **Look**: `Physical`, with Earthquake's shake as its own recipe once plan 25 gets to it; its sound the type's until a `use_<key>` of its own is written.

#### 19. Magma Vein

- **Ground · Special** · power 100 · accuracy 95 · 10 PP · priority 0 · target: one
- **Does**: Hits; 20% chance to burn.
- **Machinery**: data fields alone (no `effect`): `inflictStatus` Burn at 20%. The trainer AI reads it as `BurnHit`.
- **Flavour**: It taps the molten rock under the field and lets it out.
- **Learned by** (the rule below): 32 species, at levels 150-250; for example Gastrodon 210, Hippowdon 180.
- **Look**: Ground's special template (`Eruption`); its sound the type's until a `use_<key>` of its own is written.

#### 20. Tectonic Rend

- **Ground · Physical** · power 130 · accuracy 85 · 5 PP · priority 0 · target: one · flags: Contact
- **Does**: Hits; a high chance of a critical hit.
- **Machinery**: data fields alone (no `effect`): `critStage` 1. The trainer AI reads it as `HighCritical`.
- **Flavour**: A blow that cracks the plate the foe stands on.
- **Learned by** (the rule below): 69 species, at levels 220-250; for example Torterra 250, Gastrodon 250, Garchomp 250, Hippowdon 250.
- **Look**: a recipe of its own (a capstone); its sound the type's until a `use_<key>` of its own is written.

### Rock

#### 21. Monolith Pose

- **Rock · Status** · power — · accuracy — · 10 PP · priority 0 · target: user · flags: Snatch
- **Does**: Raises the user's Attack, Defense and Sp. Def by one stage.
- **Machinery**: data fields alone (no `effect`): Attack +1, `alsoChangesStats` Defense and SpDefense, self. The trainer AI reads it as `AtkDefUp`≈.
- **Flavour**: It stands as still and as heavy as a standing stone.
- **Learned by** (the rule below): 74 species, at levels 110-205; for example Tyranitar 110, Rampardos 110, Bastiodon 110, Rhyperior 165.
- **Look**: `SelfAura`; its sound the type's until a `use_<key>` of its own is written.

#### 22. Boulder Crush

- **Rock · Physical** · power 100 · accuracy 90 · 10 PP · priority 0 · target: one
- **Does**: Hits; 20% chance to make the target flinch.
- **Machinery**: data fields alone (no `effect`): `flinchChancePercent` 20, as Rock Slide's. The trainer AI reads it as `FlinchHit`.
- **Flavour**: A boulder brought down from overhead.
- **Learned by** (the rule below): 63 species, at levels 130-220; for example Tyranitar 140, Rampardos 145, Bastiodon 130, Rhyperior 195.
- **Look**: `Physical`; its sound the type's until a `use_<key>` of its own is written.

#### 23. Crystal Lance

- **Rock · Special** · power 95 · accuracy 100 · 10 PP · priority 0 · target: one
- **Does**: Hits; a high chance of a critical hit.
- **Machinery**: data fields alone (no `effect`): `critStage` 1. The trainer AI reads it as `HighCritical`.
- **Flavour**: A shard of clear crystal that finds the weak point.
- **Learned by** (the rule below): 38 species, at levels 145-250; for example Rampardos 180, Bastiodon 150.
- **Look**: Rock's special template (`RockArc`); its sound the type's until a `use_<key>` of its own is written.

#### 24. Skyfall Stone

- **Rock · Physical** · power 120 · accuracy 85 · 5 PP · priority 0 · target: all foes
- **Does**: Hits every foe.
- **Machinery**: data fields alone (no `effect`): `target` AllFoes. The trainer AI reads it as `Hit`.
- **Flavour**: Stones that were thrown so high they come back as meteors.
- **Learned by** (the rule below): 63 species, at levels 220-250; for example Tyranitar 250, Rampardos 250, Bastiodon 250, Rhyperior 250.
- **Look**: a recipe of its own (a capstone); its sound the type's until a `use_<key>` of its own is written.

### Bug

#### 25. Molt Renewal

- **Bug · Status** · power — · accuracy — · 10 PP · priority 0 · target: user · flags: Snatch
- **Does**: Restores a quarter of the user's HP and raises its Speed by one stage.
- **Machinery**: data fields alone (no `effect`): `healPercent` 25 and Speed +1, self; the first move of the data with both, which `ByItsFields` runs one after the other (a scenario test holds it). The trainer AI reads it as `RestoreHalfHp`≈.
- **Flavour**: It sheds a worn skin and comes out lighter.
- **Learned by** (the rule below): 91 species, at levels 110-180; for example Scizor 110, Kricketune 110, Wormadam 110, Mothim 110.
- **Look**: `SelfAura`; its sound the type's until a `use_<key>` of its own is written.

#### 26. Mandible Crush

- **Bug · Physical** · power 100 · accuracy 95 · 10 PP · priority 0 · target: one · flags: Contact, Bite
- **Does**: Hits; 20% chance to lower the target's Defense by one stage.
- **Machinery**: data fields alone (no `effect`): Defense -1 at 20%; the Bite flag (Strong Jaw). The trainer AI reads it as `LowerDefenseHit`.
- **Flavour**: Jaws that close like a vice and don't let go at once.
- **Learned by** (the rule below): 69 species, at levels 130-250; for example Scizor 140, Kricketune 155, Mothim 130, Vespiquen 130.
- **Look**: `Physical`; its sound the type's until a `use_<key>` of its own is written.

#### 27. Swarm Chorus

- **Bug · Special** · power 85 · accuracy 100 · 10 PP · priority 0 · target: all foes · flags: Sound
- **Does**: Hits every foe; 10% chance to lower each target's Sp. Atk; a sound move.
- **Machinery**: data fields alone (no `effect`): `target` AllFoes, SpAttack -1 at 10%, the Sound flag, as Bug Buzz. The trainer AI reads it as `LowerSpAtkHit`.
- **Flavour**: A thousand wings beating in one note.
- **Learned by** (the rule below): 54 species, at levels 145-250; for example Kricketune 250, Wormadam 155, Mothim 150, Vespiquen 150.
- **Look**: Bug's special template (`Swarm`); its sound the type's until a `use_<key>` of its own is written.

#### 28. Darting Sting

- **Bug · Physical** · power 50 · accuracy 100 · 20 PP · priority +1 · target: one · flags: Contact
- **Does**: Hits first (priority +1).
- **Machinery**: data fields alone (no `effect`): `priority` 1. The trainer AI reads it as `Priority1`.
- **Flavour**: A sting that lands before the foe sees it coming.
- **Learned by** (the rule below): 69 species, at levels 165-250; for example Scizor 165, Kricketune 205, Mothim 170, Vespiquen 170.
- **Look**: `Physical`; its sound the type's until a `use_<key>` of its own is written.

### Ghost

#### 29. Grave Chill

- **Ghost · Status** · power — · accuracy 100 · 15 PP · priority 0 · target: one · flags: Reflectable
- **Does**: Lowers the target's Attack and Sp. Atk by one stage.
- **Machinery**: data fields alone (no `effect`): Attack -1 and SpAttack -1 on the target, as Noble Roar. The trainer AI reads it as `LowerAttackHit`≈.
- **Flavour**: A cold breath from somewhere that isn't there.
- **Learned by** (the rule below): 64 species, at levels 110-215; for example Gengar 110, Mismagius 110, Spiritomb 110, Dusknoir 110.
- **Look**: `StatusWaves`; its sound the type's until a `use_<key>` of its own is written.

#### 30. Spectral Claw

- **Ghost · Physical** · power 95 · accuracy 100 · 10 PP · priority 0 · target: one · flags: Contact
- **Does**: Hits; a high chance of a critical hit.
- **Machinery**: data fields alone (no `effect`): `critStage` 1. The trainer AI reads it as `HighCritical`.
- **Flavour**: Claws that pass through armour and catch the spirit.
- **Learned by** (the rule below): 43 species, at levels 130-250; for example Mismagius 250, Spiritomb 130, Dusknoir 155, Froslass 190.
- **Look**: `Physical`; its sound the type's until a `use_<key>` of its own is written.

#### 31. Phantom Lantern

- **Ghost · Special** · power 100 · accuracy 95 · 10 PP · priority 0 · target: one
- **Does**: Hits; 20% chance to burn.
- **Machinery**: data fields alone (no `effect`): `inflictStatus` Burn at 20%. The trainer AI reads it as `BurnHit`.
- **Flavour**: A will-o'-the-wisp's lamp swung into the foe's face.
- **Learned by** (the rule below): 43 species, at levels 140-250; for example Gengar 140, Mismagius 155, Spiritomb 150, Dusknoir 250.
- **Look**: Ghost's special template (`ShadowOrb`); its sound the type's until a `use_<key>` of its own is written.

#### 32. Soul Siphon

- **Ghost · Special** · power 80 · accuracy 100 · 10 PP · priority 0 · target: one
- **Does**: Hits; the user heals half the damage dealt.
- **Machinery**: data fields alone (no `effect`): `drainPercent` 50. The trainer AI reads it as `RecoverHalfDamageDealt`.
- **Flavour**: It draws a little of the foe's life out with the blow.
- **Learned by** (the rule below): 43 species, at levels 165-250; for example Gengar 165, Mismagius 205, Spiritomb 170, Dusknoir 205.
- **Look**: Ghost's special template (`ShadowOrb`); its sound the type's until a `use_<key>` of its own is written.

### Steel

#### 33. Alloy Plating

- **Steel · Status** · power — · accuracy — · 5 PP · priority 0 · target: user · flags: Snatch
- **Does**: Raises the user's Defense and Sp. Def by two stages.
- **Machinery**: data fields alone (no `effect`): Defense +2, `alsoChangesStats` SpDefense, self. The trainer AI reads it as `DefSpdUp`.
- **Flavour**: Fresh plates of alloy riveted over the old.
- **Learned by** (the rule below): 65 species, at levels 110-205; for example Scizor 195, Metagross 110, Empoleon 205, Bastiodon 170.
- **Look**: `SelfAura`; its sound the type's until a `use_<key>` of its own is written.

#### 34. Forge Hammer

- **Steel · Physical** · power 110 · accuracy 95 · 10 PP · priority 0 · target: one · flags: Contact
- **Does**: Hits; lowers the user's Speed by one stage.
- **Machinery**: data fields alone (no `effect`): Speed -1 on the user, as Hammer Arm. The trainer AI reads it as `SpeedDownHit`.
- **Flavour**: A smith's hammer, heavy enough to slow the arm that swings it.
- **Learned by** (the rule below): 53 species, at levels 130-220; for example Scizor 220, Metagross 145, Bastiodon 190, Bronzong 145.
- **Look**: `Physical`; its sound the type's until a `use_<key>` of its own is written.

#### 35. Magnet Lance

- **Steel · Special** · power 95 · accuracy 100 · 10 PP · priority 0 · target: one
- **Does**: Hits; 20% chance to lower the target's Sp. Def by one stage.
- **Machinery**: data fields alone (no `effect`): SpDefense -1 at 20%, as Flash Cannon. The trainer AI reads it as `LowerSpDefHit`.
- **Flavour**: A spear of iron filings drawn into one point by magnetism.
- **Learned by** (the rule below): 33 species, at levels 145-250; for example Empoleon 250, Bastiodon 210, Lucario 210, Magnezone 250.
- **Look**: Steel's special template (`EnergyBeam`); its sound the type's until a `use_<key>` of its own is written.

#### 36. Rivet Shot

- **Steel · Physical** · power 45 · accuracy 100 · 10 PP · priority 0 · target: one
- **Does**: Hits twice.
- **Machinery**: the `effect` `HitTwice` (`BattleCore.Families.cs`), as Double Kick. The trainer AI reads it as `HitTwice`.
- **Flavour**: Two rivets punched out one after the other.
- **Learned by** (the rule below): 53 species, at levels 165-250; for example Scizor 250, Metagross 180, Bastiodon 230, Bronzong 180.
- **Look**: `Physical`, played once for each hit as every multi-hit move is; its sound the type's until a `use_<key>` of its own is written.

### Fire

#### 37. Kindle

- **Fire · Status** · power — · accuracy — · 20 PP · priority 0 · target: user · flags: Snatch
- **Does**: Raises the user's Sp. Atk and Speed by one stage.
- **Machinery**: data fields alone (no `effect`): SpAttack +1, `alsoChangesStats` Speed, self. The trainer AI reads it as `SpAtkUp`≈.
- **Flavour**: It feeds its inner fire until it roars.
- **Learned by** (the rule below): 81 species, at levels 110-215; for example Charizard 110, Infernape 110, Magmortar 110, Heatran 110.
- **Look**: `SelfAura`; its sound the type's until a `use_<key>` of its own is written.

#### 38. Cinder Stomp

- **Fire · Physical** · power 100 · accuracy 95 · 10 PP · priority 0 · target: one · flags: Contact
- **Does**: Hits; 30% chance to burn.
- **Machinery**: data fields alone (no `effect`): `inflictStatus` Burn at 30%. The trainer AI reads it as `BurnHit`.
- **Flavour**: A stamp of a burning foot that leaves embers behind.
- **Learned by** (the rule below): 57 species, at levels 130-250; for example Infernape 130, Magmortar 180.
- **Look**: `Physical`; its sound the type's until a `use_<key>` of its own is written.

#### 39. Pyre Lance

- **Fire · Special** · power 105 · accuracy 95 · 10 PP · priority 0 · target: one
- **Does**: Hits; 10% chance to burn.
- **Machinery**: data fields alone (no `effect`): Burn at 10%, as Flamethrower. The trainer AI reads it as `BurnHit`.
- **Flavour**: A spear of white-hot flame.
- **Learned by** (the rule below): 70 species, at levels 140-230; for example Charizard 145, Infernape 150, Magmortar 145, Heatran 145.
- **Look**: Fire's special template (`Flames`); its sound the type's until a `use_<key>` of its own is written.

#### 40. Sunforge Blast

- **Fire · Special** · power 140 · accuracy 90 · 5 PP · priority 0 · target: one
- **Does**: Hits; lowers the user's Sp. Atk by two stages.
- **Machinery**: data fields alone (no `effect`): SpAttack -2 on the user, as Overheat. The trainer AI reads it as `UserSpAtkDown2`.
- **Flavour**: Fire hot enough to forge a sun, which takes everything it has.
- **Learned by** (the rule below): 59 species, at levels 230-250; for example Charizard 250, Infernape 230, Magmortar 250, Heatran 250.
- **Look**: a recipe of its own (a capstone); its sound the type's until a `use_<key>` of its own is written.

### Water

#### 41. Tidal Ward

- **Water · Status** · power — · accuracy — · 10 PP · priority 0 · target: user · flags: Snatch
- **Does**: Restores a quarter of the user's HP and raises its Sp. Def by one stage.
- **Machinery**: data fields alone (no `effect`): `healPercent` 25 and SpDefense +1, self (as Molt Renewal). The trainer AI reads it as `RestoreHalfHp`≈.
- **Flavour**: A cool tide washes round it and stays.
- **Learned by** (the rule below): 154 species, at levels 110-205; for example Blastoise 110, Gyarados 110, Empoleon 110, Bibarel 195.
- **Look**: `SelfAura`; its sound the type's until a `use_<key>` of its own is written.

#### 42. Riptide Slam

- **Water · Physical** · power 100 · accuracy 95 · 10 PP · priority 0 · target: one · flags: Contact
- **Does**: Hits; 20% chance to lower the target's Speed by one stage.
- **Machinery**: data fields alone (no `effect`): Speed -1 at 20%, as Bubble Beam's. The trainer AI reads it as `LowerSpeedHit`.
- **Flavour**: The pull of a current under the blow.
- **Learned by** (the rule below): 121 species, at levels 130-220; for example Blastoise 145, Gyarados 140, Bibarel 220, Floatzel 145.
- **Look**: `Physical`; its sound the type's until a `use_<key>` of its own is written.

#### 43. Abyssal Current

- **Water · Special** · power 100 · accuracy 95 · 10 PP · priority 0 · target: one
- **Does**: Hits; 30% chance to lower the target's Speed by one stage.
- **Machinery**: data fields alone (no `effect`): Speed -1 at 30%. The trainer AI reads it as `LowerSpeedHit`.
- **Flavour**: Cold black water from the deepest sea.
- **Learned by** (the rule below): 121 species, at levels 145-250; for example Blastoise 180, Empoleon 155, Floatzel 180, Gastrodon 150.
- **Look**: Water's special template (`WaterStream`); its sound the type's until a `use_<key>` of its own is written.

#### 44. Maelstrom Crash

- **Water · Physical** · power 130 · accuracy 90 · 5 PP · priority 0 · target: one · flags: Contact
- **Does**: Hits; the user takes a third of the damage dealt as recoil.
- **Machinery**: data fields alone (no `effect`): `recoilPercent` 33, as Wave Crash. The trainer AI reads it as `RecoilThird`.
- **Flavour**: It dives into a whirlpool of its own making and takes the foe with it.
- **Learned by** (the rule below): 99 species, at levels 220-250; for example Blastoise 250, Gyarados 220, Bibarel 250, Floatzel 250.
- **Look**: a recipe of its own (a capstone); its sound the type's until a `use_<key>` of its own is written.

### Grass

#### 45. Verdant Renewal

- **Grass · Status** · power — · accuracy — · 10 PP · priority 0 · target: user · flags: Snatch
- **Does**: Restores a third of the user's HP and raises its Defense by one stage.
- **Machinery**: data fields alone (no `effect`): `healPercent` 33 and Defense +1, self. The trainer AI reads it as `RestoreHalfHp`≈.
- **Flavour**: New growth closes over its wounds like bark.
- **Learned by** (the rule below): 127 species, at levels 110-205; for example Venusaur 110, Torterra 110, Roserade 110, Wormadam 205.
- **Look**: `SelfAura`; its sound the type's until a `use_<key>` of its own is written.

#### 46. Thornlash

- **Grass · Physical** · power 100 · accuracy 95 · 10 PP · priority 0 · target: one · flags: Contact
- **Does**: Hits; 20% chance to make the target flinch.
- **Machinery**: data fields alone (no `effect`): `flinchChancePercent` 20. The trainer AI reads it as `FlinchHit`.
- **Flavour**: A whip of thorned vine.
- **Learned by** (the rule below): 96 species, at levels 130-250; for example Torterra 140, Carnivine 155, Abomasnow 130, Leafeon 155.
- **Look**: `Physical`; its sound the type's until a `use_<key>` of its own is written.

#### 47. Sap Lance

- **Grass · Special** · power 85 · accuracy 100 · 10 PP · priority 0 · target: one
- **Does**: Hits; the user heals half the damage dealt.
- **Machinery**: data fields alone (no `effect`): `drainPercent` 50, as Giga Drain. The trainer AI reads it as `RecoverHalfDamageDealt`.
- **Flavour**: A green shaft that roots in the foe and drinks.
- **Learned by** (the rule below): 99 species, at levels 145-250; for example Venusaur 145, Roserade 145, Wormadam 250, Carnivine 250.
- **Look**: Grass's special template (`LeafStream`); its sound the type's until a `use_<key>` of its own is written.

#### 48. Root Snare

- **Grass · Physical** · power 80 · accuracy 100 · 15 PP · priority 0 · target: one · flags: Contact
- **Does**: Hits; the target can't flee or be switched out while the user stays.
- **Machinery**: the `effect` `SpiritShackle` (`BattleCore.Effects.Later2.cs`), as Anchor Shot. The trainer AI reads it as `Hit`≈ (Platinum has no such effect: the AI weighs it as a plain hit).
- **Flavour**: Roots burst up and wrap the foe's feet.
- **Learned by** (the rule below): 96 species, at levels 165-250; for example Torterra 165, Carnivine 205, Abomasnow 170, Leafeon 205.
- **Look**: `Physical`; its sound the type's until a `use_<key>` of its own is written.

### Electric

#### 49. Overcharge

- **Electric · Status** · power — · accuracy — · 10 PP · priority 0 · target: user · flags: Snatch
- **Does**: Raises the user's Sp. Atk, Sp. Def and Speed by one stage.
- **Machinery**: data fields alone (no `effect`): SpAttack +1, `alsoChangesStats` SpDefense and Speed, self, as Quiver Dance. The trainer AI reads it as `SpAtkSpDefUp`≈.
- **Flavour**: It stores more current than it was built to hold.
- **Learned by** (the rule below): 69 species, at levels 110-205; for example Pikachu 110, Luxray 110, Pachirisu 110, Magnezone 110.
- **Look**: `SelfAura`; its sound the type's until a `use_<key>` of its own is written.

#### 50. Arc Lance

- **Electric · Physical** · power 100 · accuracy 95 · 10 PP · priority 0 · target: one · flags: Contact
- **Does**: Hits; 20% chance to paralyse.
- **Machinery**: data fields alone (no `effect`): `inflictStatus` Paralyze at 20%. The trainer AI reads it as `ParalyzeHit`.
- **Flavour**: A charge that jumps the gap as a spear of light.
- **Learned by** (the rule below): 47 species, at levels 130-250; for example Pikachu 155, Luxray 155, Pachirisu 155, Electivire 155.
- **Look**: `Physical`; its sound the type's until a `use_<key>` of its own is written.

#### 51. Ion Cascade

- **Electric · Special** · power 95 · accuracy 100 · 10 PP · priority 0 · target: one
- **Does**: Hits; 20% chance to paralyse.
- **Machinery**: data fields alone (no `effect`): Paralyze at 20%. The trainer AI reads it as `ParalyzeHit`.
- **Flavour**: Sparks that pour down like a waterfall.
- **Learned by** (the rule below): 58 species, at levels 145-250; for example Pikachu 205, Luxray 250, Pachirisu 205, Magnezone 155.
- **Look**: Electric's special template (`ElectricBolt`); its sound the type's until a `use_<key>` of its own is written.

#### 52. Voltstep

- **Electric · Physical** · power 50 · accuracy 100 · 20 PP · priority +1 · target: one · flags: Contact
- **Does**: Hits first (priority +1); 10% chance to paralyse.
- **Machinery**: data fields alone (no `effect`): `priority` 1 and Paralyze at 10%. The trainer AI reads it as `Priority1`.
- **Flavour**: A step quicker than a spark.
- **Learned by** (the rule below): 47 species, at levels 165-250; for example Pikachu 250, Luxray 205, Pachirisu 250, Electivire 205.
- **Look**: `Physical`; its sound the type's until a `use_<key>` of its own is written.

### Psychic

#### 53. Clairvoyance

- **Psychic · Status** · power — · accuracy — · 20 PP · priority 0 · target: user · flags: Snatch
- **Does**: Raises the user's Sp. Atk and accuracy by one stage.
- **Machinery**: data fields alone (no `effect`): SpAttack +1, `alsoChangesStats` Accuracy, self, as Hone Claws's pair. The trainer AI reads it as `SpAtkUp`≈.
- **Flavour**: It sees the fight a moment before it happens.
- **Learned by** (the rule below): 99 species, at levels 110-215; for example Alakazam 110, Gardevoir 110, Metagross 215, Bronzong 215.
- **Look**: `SelfAura`; its sound the type's until a `use_<key>` of its own is written.

#### 54. Mind Blade

- **Psychic · Physical** · power 90 · accuracy 100 · 10 PP · priority 0 · target: one
- **Does**: Hits; a high chance of a critical hit.
- **Machinery**: data fields alone (no `effect`): `critStage` 1, as Psycho Cut. The trainer AI reads it as `HighCritical`.
- **Flavour**: An edge made of nothing but will.
- **Learned by** (the rule below): 62 species, at levels 130-250; for example Alakazam 250, Metagross 250, Bronzong 250, Gallade 145.
- **Look**: `Physical`; its sound the type's until a `use_<key>` of its own is written.

#### 55. Astral Pulse

- **Psychic · Special** · power 100 · accuracy 95 · 10 PP · priority 0 · target: one · flags: Pulse
- **Does**: Hits; 20% chance to lower the target's Sp. Def by one stage.
- **Machinery**: data fields alone (no `effect`): SpDefense -1 at 20%, the Pulse flag (Mega Launcher). The trainer AI reads it as `LowerSpDefHit`.
- **Flavour**: A wave from the far side of the stars.
- **Learned by** (the rule below): 85 species, at levels 140-230; for example Alakazam 155, Gardevoir 140, Cresselia 205.
- **Look**: Psychic's special template (`PsychicWaves`); its sound the type's until a `use_<key>` of its own is written.

#### 56. Mindbreak Wave

- **Psychic · Special** · power 110 · accuracy 95 · 5 PP · priority 0 · target: one
- **Does**: Hits; the damage is worked out against the target's Defense, not its Sp. Def.
- **Machinery**: the `effect` `Psyshock` (an empty entry; the rule is `DamageCalculator`'s, asked by the effect), as Psystrike. The trainer AI reads it as `Hit`≈.
- **Flavour**: A thought so heavy it lands like a fist.
- **Learned by** (the rule below): 85 species, at levels 165-250; for example Alakazam 205, Gardevoir 165, Cresselia 250.
- **Look**: a recipe of its own; its sound the type's until a `use_<key>` of its own is written.

### Ice

#### 57. Hoarfrost

- **Ice · Status** · power — · accuracy 95 · 15 PP · priority 0 · target: all foes · flags: Reflectable
- **Does**: Lowers every foe's Attack and Speed by one stage.
- **Machinery**: data fields alone (no `effect`): `target` AllFoes, Attack -1 and Speed -1. The trainer AI reads it as `LowerAttackHit`≈.
- **Flavour**: Frost that creeps over the foes' limbs and stiffens them.
- **Learned by** (the rule below): 48 species, at levels 110-215; for example Abomasnow 190, Weavile 215, Glaceon 110, Mamoswine 110.
- **Look**: `StatusWaves`; its sound the type's until a `use_<key>` of its own is written.

#### 58. Glacier Ram

- **Ice · Physical** · power 110 · accuracy 90 · 10 PP · priority 0 · target: one · flags: Contact
- **Does**: Hits; 10% chance to freeze.
- **Machinery**: data fields alone (no `effect`): `inflictStatus` Freeze at 10%. The trainer AI reads it as `FreezeHit`.
- **Flavour**: It charges like a glacier calving into the sea.
- **Learned by** (the rule below): 36 species, at levels 130-250; for example Abomasnow 210, Weavile 250, Glaceon 180, Mamoswine 145.
- **Look**: `Physical`; its sound the type's until a `use_<key>` of its own is written.

#### 59. Whiteout Gale

- **Ice · Special** · power 90 · accuracy 95 · 10 PP · priority 0 · target: all foes
- **Does**: Hits every foe; 10% chance to freeze each.
- **Machinery**: data fields alone (no `effect`): `target` AllFoes, Freeze at 10%. The trainer AI reads it as `FreezeHit`.
- **Flavour**: A blizzard so thick the field turns white.
- **Learned by** (the rule below): 35 species, at levels 140-230; for example Abomasnow 230, Glaceon 145, Froslass 150.
- **Look**: Ice's special template (`IceBeam`); its sound the type's until a `use_<key>` of its own is written.

#### 60. Permafrost Lance

- **Ice · Special** · power 120 · accuracy 85 · 5 PP · priority 0 · target: one
- **Does**: Hits; a high chance of a critical hit.
- **Machinery**: data fields alone (no `effect`): `critStage` 1. The trainer AI reads it as `HighCritical`.
- **Flavour**: Ice from the ground that never thaws.
- **Learned by** (the rule below): 27 species, at levels 250-250; for example Abomasnow 250, Glaceon 250, Froslass 250.
- **Look**: a recipe of its own (a capstone); its sound the type's until a `use_<key>` of its own is written.

### Dragon

#### 61. Wyrm Dance

- **Dragon · Status** · power — · accuracy — · 10 PP · priority 0 · target: user · flags: Dance, Snatch
- **Does**: Raises the user's Attack, Sp. Atk and Speed by one stage.
- **Machinery**: data fields alone (no `effect`): Attack +1, `alsoChangesStats` SpAttack and Speed, self; the Dance flag (Dancer). The trainer AI reads it as `AtkSpdUp`≈.
- **Flavour**: An old dance of dragons, older than the regions.
- **Learned by** (the rule below): 70 species, at levels 110-215; for example Dragonite 110, Salamence 110, Garchomp 110, Dialga 205.
- **Look**: `SelfAura`; its sound the type's until a `use_<key>` of its own is written.

#### 62. Scale Rend

- **Dragon · Physical** · power 100 · accuracy 95 · 10 PP · priority 0 · target: one · flags: Contact
- **Does**: Hits; 20% chance to lower the target's Defense by one stage.
- **Machinery**: data fields alone (no `effect`): Defense -1 at 20%. The trainer AI reads it as `LowerDefenseHit`.
- **Flavour**: Scales like blades, raked across the foe.
- **Learned by** (the rule below): 48 species, at levels 130-220; for example Dragonite 140, Salamence 140, Garchomp 140, Giratina 210.
- **Look**: `Physical`; its sound the type's until a `use_<key>` of its own is written.

#### 63. Wyrmflare

- **Dragon · Special** · power 100 · accuracy 95 · 10 PP · priority 0 · target: one
- **Does**: Hits; 10% chance to burn.
- **Machinery**: data fields alone (no `effect`): Burn at 10%. The trainer AI reads it as `BurnHit`.
- **Flavour**: Dragonfire, which burns a colour no other fire does.
- **Learned by** (the rule below): 42 species, at levels 145-250; for example Dialga 250, Palkia 250, Giratina 230, Hydreigon 250.
- **Look**: Dragon's special template (`EnergyBeam`); its sound the type's until a `use_<key>` of its own is written.

#### 64. Serpent Crash

- **Dragon · Physical** · power 140 · accuracy 90 · 5 PP · priority 0 · target: one · flags: Contact
- **Does**: Hits; lowers the user's Attack and Defense by one stage.
- **Machinery**: data fields alone (no `effect`): Attack -1 and Defense -1 on the user, as Superpower. The trainer AI reads it as `LowerOwnAtkAndDef`.
- **Flavour**: The whole length of a sky serpent brought down at once.
- **Learned by** (the rule below): 47 species, at levels 220-250; for example Dragonite 220, Salamence 220, Garchomp 220, Giratina 250.
- **Look**: a recipe of its own (a capstone); its sound the type's until a `use_<key>` of its own is written.

### Dark

#### 65. Dread Aura

- **Dark · Status** · power — · accuracy 100 · 15 PP · priority 0 · target: all foes · flags: Reflectable
- **Does**: Lowers every foe's Sp. Def and Speed by one stage.
- **Machinery**: data fields alone (no `effect`): `target` AllFoes, SpDefense -1 and Speed -1. The trainer AI reads it as `LowerSpDefHit`≈.
- **Flavour**: A presence that makes the foes' hearts sink.
- **Learned by** (the rule below): 69 species, at levels 110-195; for example Tyranitar 165, Honchkrow 110, Skuntank 180, Spiritomb 190.
- **Look**: `StatusWaves`; its sound the type's until a `use_<key>` of its own is written.

#### 66. Rending Fang

- **Dark · Physical** · power 100 · accuracy 95 · 10 PP · priority 0 · target: one · flags: Contact, Bite
- **Does**: Hits; 20% chance to make the target flinch.
- **Machinery**: data fields alone (no `effect`): `flinchChancePercent` 20; the Bite flag (Strong Jaw). The trainer AI reads it as `FlinchHit`.
- **Flavour**: Fangs from the dark, and a shock that freezes the foe.
- **Learned by** (the rule below): 61 species, at levels 130-250; for example Tyranitar 195, Honchkrow 140, Skuntank 215, Spiritomb 210.
- **Look**: `Physical`; its sound the type's until a `use_<key>` of its own is written.

#### 67. Umbral Pulse

- **Dark · Special** · power 100 · accuracy 95 · 10 PP · priority 0 · target: one · flags: Pulse
- **Does**: Hits; 30% chance to lower the target's Sp. Atk by one stage.
- **Machinery**: data fields alone (no `effect`): SpAttack -1 at 30%; the Pulse flag. The trainer AI reads it as `LowerSpAtkHit`.
- **Flavour**: A wave of shadow that drowns the foe's focus.
- **Learned by** (the rule below): 27 species, at levels 145-250; for example Spiritomb 230, Darkrai 155, Hydreigon 155.
- **Look**: Dark's special template (`DarkWaves`); its sound the type's until a `use_<key>` of its own is written.

#### 68. Pilfer Strike

- **Dark · Physical** · power 70 · accuracy 100 · 15 PP · priority 0 · target: one · flags: Contact
- **Does**: Hits; takes the target's held item if the user holds none.
- **Machinery**: the `effect` `StealHeldItem` (`BattleCore.Unique.cs`), as Thief. The trainer AI reads it as `StealHeldItem`.
- **Flavour**: A blow and a quick hand.
- **Learned by** (the rule below): 61 species, at levels 165-250; for example Tyranitar 220, Honchkrow 165, Skuntank 250, Spiritomb 250.
- **Look**: `Physical`; its sound the type's until a `use_<key>` of its own is written.

### Fairy

#### 69. Moonlit Grace

- **Fairy · Status** · power — · accuracy — · 10 PP · priority 0 · target: user · flags: Snatch
- **Does**: Restores a third of the user's HP and raises its Sp. Atk by one stage.
- **Machinery**: data fields alone (no `effect`): `healPercent` 33 and SpAttack +1, self. The trainer AI reads it as `RestoreHalfHp`≈.
- **Flavour**: It bathes in moonlight that only it can see.
- **Learned by** (the rule below): 64 species, at levels 110-215; for example Clefable 110, Gardevoir 195, Togekiss 110, Sylveon 110.
- **Look**: `SelfAura`; its sound the type's until a `use_<key>` of its own is written.

#### 70. Fey Strike

- **Fairy · Physical** · power 100 · accuracy 95 · 10 PP · priority 0 · target: one · flags: Contact
- **Does**: Hits; 20% chance to lower the target's Attack by one stage.
- **Machinery**: data fields alone (no `effect`): Attack -1 at 20%, as Play Rough. The trainer AI reads it as `LowerAttackHit`.
- **Flavour**: A blow that leaves the foe dazed and gentler.
- **Learned by** (the rule below): 38 species, at levels 130-250; for example Clefable 250, Sylveon 250, Mimikyu 250, Tinkaton 130.
- **Look**: `Physical`; its sound the type's until a `use_<key>` of its own is written.

#### 71. Starlight Chorus

- **Fairy · Special** · power 90 · accuracy 100 · 10 PP · priority 0 · target: all foes · flags: Sound
- **Does**: Hits every foe; 10% chance to lower each target's Sp. Atk; a sound move.
- **Machinery**: data fields alone (no `effect`): `target` AllFoes, SpAttack -1 at 10%, the Sound flag. The trainer AI reads it as `LowerSpAtkHit`.
- **Flavour**: A song the stars sing back.
- **Learned by** (the rule below): 55 species, at levels 145-250; for example Clefable 155, Gardevoir 220, Togekiss 145, Sylveon 155.
- **Look**: Fairy's special template (`Sparkles`); its sound the type's until a `use_<key>` of its own is written.

#### 72. Dew Kiss

- **Fairy · Special** · power 80 · accuracy 100 · 10 PP · priority 0 · target: one · flags: Contact
- **Does**: Hits; the user heals half the damage dealt.
- **Machinery**: data fields alone (no `effect`): `drainPercent` 50, the Contact flag, as Draining Kiss. The trainer AI reads it as `RecoverHalfDamageDealt`.
- **Flavour**: A kiss of morning dew that takes a little strength.
- **Learned by** (the rule below): 55 species, at levels 170-250; for example Clefable 205, Gardevoir 250, Togekiss 180, Sylveon 205.
- **Look**: Fairy's special template (`Sparkles`); its sound the type's until a `use_<key>` of its own is written.

## The technical plan (for after the user's answers)

All of it under the modern rules, which are the only ones (06 · decision 1). Nothing of Platinum's reaches level 101, so `battles-platinum.txt`, `battles-modern.txt` (whose Pokémon learn by levels of 100 or less), `trainers.txt` and `levels.txt` should not move; the session accepts them only if they do and says why.

### 1. The data (one sitting, about 3 hours)

- **The moves.** A move of our own carries no PokeAPI row, so the importer learns to keep one: `tools/DataImporter/Overrides/moves.json` gains whole rows for the 72 (an `id`, the fields above, `support: "Full"`, a `description` from the flavour line, `generation` 0 for "ours", `contest` null), which the importer adds after its own moves where today it only corrects fields. Ids **3001 to 3072**, past the data's last (2033, G-Max Wind Rage), in the order of this document. Each row's `battleEffect` is the one named above. `zPower` and `maxPower` by the data's own table of power to Z-power and Max power, so a move of ours under a Z-Crystal or a Dynamax does what a move of that power does.
- **The learnsets.** The rule above, written as code in the importer (`MovesPastHundred.cs`), run after the species are read and appending to each species' `learnset` (and to its `modern.learnset`, which is the one the modern rules read) and to each form's that has its own; a level-up move past 100 is never written by hand. The importer's report gains a line with the table of counts above, and `DataFileTests` a test that every species but the six learns at least four moves of the pool between 101 and 260 and every move of the pool is learned by someone.
- **`Pokemon.Learnset` reads past 100 already** (plan 06's R31 text); `GainExp`'s level-ups past 100 then offer the move as any level does (`MoveWanted`), and the Move Relearner and REMEMBER (`Models/Relearning.cs`) list them by the level the Pokémon has reached, with nothing new.
- **Then** run the importer (it rewrites `coverage.md`: 72 more moves, all fully run), `ContentCheck`, and `GOLDEN=accept … --filter Golden`, reading the diff (nothing should move).

### 2. The tests (the same sitting)

- **A scenario test to each kind of machinery a move uses** (`CoreScenario.cs`, both sides answered from outside, `Calm()`), rather than one a move, since 68 of them share fields with each other: a heal with a stat on the user (Molt Renewal: the first move of the data with both; at full HP it still raises the stat), three stats at once (Overcharge), two stats on every foe (Corrosive Haze, Hoarfrost, Dread Aura in a double battle), the user and its ally (Rally Cry), a side effect by chance on a hit (Cinder Stomp's burn, fixed by `RollKind.SideEffect`), recoil (Titan Tackle's quarter), a drain (Soul Siphon), priority (Snapstep against a faster foe), a hit on all others (Fault Line hitting its ally), and one each for the four named effects (Rivet Shot's two hits, Root Snare's trap, Mindbreak Wave against Defense, Pilfer Strike's theft). The numbers worked out by hand in a comment, as the scenario tests have them.
- **The trainer AI**: one test that a trainer holding a move of ours chooses it as it chooses the Platinum move whose battle effect it carries (`AiThinking` by `MoveEffectId`).

### 3. The looks and the words (a later sitting, plan 25's and 17's ways)

- **Looks.** Every move plays its template from the first day. Nine get recipes of their own in plan 25's way (`MoveFx.Ours.cs`, landing at `ImpactTime`, under `MaxQuads`, held by `MovePresentationTests`): the eight capstones and Mindbreak Wave. Their sounds (`use_<key>`) come with them; the rest keep the type's.
- **Names in Romanian**, by plan 17 · N3's way of hand-written names (`Data/text/ro/names.moves.json` beside the items' and abilities'), and the descriptions with them: shown to the user with the English, as this list is.

## Questions for the user

Each has a default the build goes ahead on unless the user says otherwise.

1. **The pool.** Are the 72 right: their names, numbers and effects, and four to a type? *Default*: as written, with any the user strikes out left out and the rule taking the rest.
2. **How many each species learns.** Four to eight (most four to six), the first at 110 and the last at 250. *Default*: as written. *Alternatives*: a fixed six for every species (more Normal fill-in), or the last at 255 so that Paldea's post-game teaches it.
3. **Contest data.** The plan leaves a move of ours without a contest type or effect (`contest` null), which Super Contests (plan 06 · R17a) can't use: a Pokémon would show nothing for it on stage. *Default*: null, as the plan says; a contest row for each can be proposed later beside Hoenn's (plan 21 · H11).
4. **TMs and tutors.** *Default*: learned by level only, no TM and no tutor, so they stay the reward of the levels past 100. *Alternative*: a tutor in each region's post-game who teaches the region's band of them early.
5. **The National count of moves.** The game counts Platinum's 467 and the data's later moves (the coverage report, the Pokédex's and the Trainer Card's records if any). *Default*: the 72 are counted apart, as "moves of our own", and never in a number the original games have.
6. **The Move Relearner and the Move Deleter.** *Default*: the Relearner (and the party menu's REMEMBER) offers a move of ours once the Pokémon's level has passed the move's, as for any move, and the Deleter forgets one like any other.
7. **The six left out.** Ditto, Unown, Smeargle, Wobbuffet, Wynaut and Shedinja learn none. *Default*: as written (Smeargle can Sketch them).
8. **The trainers of the later regions.** W4's mapping lifts their levels past 100 but keeps their own game's moves. *Default*: they keep them; a later region's plan may give its post-game trainers a move or two of the pool, which this proposal doesn't do.
