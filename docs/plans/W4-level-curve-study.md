# The level curve for a team carried from region to region

*Plan 18 · W4, the study part. Written 2026-10-18 for the user's decision; decided 2026-10-21 (option B, below). Built as W4's part a on 2026-10-24: the cap of 260 and the four built regions' mappings of section 4's fair climb ([plan 18](18-regions.md), W4; `docs/mechanics/rulings.md`, "The level cap past 100 and a region's levels").*

## The decision (2026-10-21)

**The user chose B: the cap rises well above 100**, and the old team plays every new region's story at full strength. The study's recommendation, C2, is not taken. Two pieces of work come with it, so that the levels past 100 have something in them:

1. **New moves to learn past level 100.** Moves of our own, so that a Pokémon still learns something as it climbs to the new cap. Placed as plan 06 · R31.
2. **New evolutions past level 100** for a species that has no evolution, and for the last stage of a line of only two. Species of our own, so that a level past 100 can still change a Pokémon. Placed as plan 03 · D14.

What the building sessions settle, each going ahead on its default unless the user says otherwise:

| Question | Default | Who settles it |
|---|---|---|
| The new cap | about **260**, the "fair climb (modern EXP)" column of section 4, since the modern rules are the only ones (06 · decision 1); the exact number fixed by W4 after the simulation is run again with the new moves and evolutions in it | 18 · W4 |
| What each later region's levels climb to | section 4's fair climb: Kanto's Champion about 104, Johto's 136, Hoenn's 162, up to Paldea's about 258; every trainer and wild table of the eight regions re-levelled by a "from, to" mapping in the region file, as each region's plan imports it | 18 · W4 writes the mapping; plans 19 to 23 apply it |
| Erratic and Fluctuating past 100 | a rule of our own that keeps rising (the Erratic formula turns negative at 160); written into `docs/mechanics/rulings.md` | 18 · W4 |
| Which species get an evolution past 100 | every species with none (205 in the data; of the legendaries and mythicals only some, the user said on 2026-10-21: D14 proposes which, region by region, and the user picks) and the last stage of every two-stage line (221 lines, more where a line branches): about 450 new species, each a design shown to the user first, in batches by region as the Megas of our own are (06 · R20b) | 03 · D14 |
| At what levels the new evolutions happen | between 101 and the cap, set so a region's post-game reaches them; a three-stage line's last stage gains new moves only | 03 · D14 |
| How many new moves | a pool of our own (a few per type, about 50 to 100), shared between species, each species learning a few between 101 and the cap by its type and line | 06 · R31 |

Section 6's recommendation and its question are kept below as the study was written.

## The study

On 2026-10-12 you decided that **the team comes along** from region to region. Your worry was the levels: every region after Sinnoh has to be levelled for a team that has already won a League, as Gold and Silver levelled Kanto after Red and Blue, and the level cap of 100 may have to rise. On 2026-10-17 you said to study it when we got to it. This is that study. It ends with four options and a recommendation, and one question for you.

## The short answer

- **A Sinnoh Champion's team is about level 64 to 71.** Under the modern EXP rules every member of the team gets a share of every win. A player who beats every trainer on the way to the Hall of Fame ends with the whole team at about 64 to 71. Cynthia's strongest Pokémon is level 62. After the Battle Zone and the League rematch the team is about 78 to 81.
- **One region of battles is worth about 25 to 35 levels.** A region the size of Sinnoh, fought from end to end, raises a team by that much wherever the team starts, as long as the trainers are close to its level. Even a region left at its own beginner's levels (level 3 on the first route) raises a level-67 team to about 78, and every fight on the way is too easy.
- **A climb from region to region does not fit under 100.** In every climbing plan we tried, a team that keeps the modern EXP reaches 100 in **Johto**, the second region after Sinnoh. Six regions would then be left with nowhere higher to go. For the trainers to stay a step ahead of the team all the way to Paldea, the cap would have to be about **260** under the modern EXP, or about **150** under Platinum's own EXP rule.
- **Recommendation: option C2, "the region's ceiling".** Every region keeps its own game's levels, as the region plans already intend to import them. A carried Pokémon fights the region's story at no more than the level of the region's next Gym Leader, the way the Battle Frontier already brings a Pokémon down to level 50. It gains no EXP above that ceiling until the region's Hall of Fame. After that, the region's post-game is where the old team's full strength counts, and the post-games climb gently from Sinnoh's 78 to 100 in Paldea. The cap stays at 100.

The rest of this document gives the numbers behind each point.

## 1. The levels the game has, and the levels the plans intend

### Sinnoh (this game's own data)

The figures come from `PokemonPlatinumTests/Golden/levels.txt` (every place's wild and trainer levels) and from `Data/trainers.json` (the Gym Leaders, the Elite Four, Cynthia, the rival and Team Galactic, who are battled by scripts).

| Part of the story | Wild Pokémon | Trainers | Leader or boss (strongest Pokémon) |
|---|---|---|---|
| Routes 201–203, Oreburgh | 2–10 | 4–11 | Roark 14 |
| Floaroma, the Windworks, Eterna Forest | 3–15 | 7–17 | Mars 17, Jupiter 23, Gardenia 22 |
| Routes 206–208, Hearthome | 16–20 | 17–23 | Fantina 26 |
| Routes 209–215, Veilstone | 17–24 | 20–33 | Maylene 32 |
| Pastoria, the Great Marsh, Route 212 | 23–31 | 24–35 | Wake 37 |
| Celestic, Canalave, Iron Island | 27–35 | 28–40 | Cyrus 36, Byron 41 |
| The lakes, Routes 216–217, Snowpoint | 32–41 | 33–44 | Saturn 40, Mars 40, Candice 44 |
| Team Galactic HQ, Mt. Coronet, Spear Pillar | 36–41 | 37–46 | Saturn 44, Cyrus 46 and 48, Giratina 47 |
| Routes 219–223, Sunyshore | 38–41 | 40–48 | Volkner 50 |
| Victory Road, the League | 40–44 | 43–50 | rival 51, Elite Four 53–59, **Cynthia 62** |
| Battle Zone (post-game) | 47–55 | 51–61 | rival 56 and 65, Mars and Jupiter 60, Heatran 50 |
| Rematches (post-game) | | | Leaders 61–65, Elite Four 65–75, **Cynthia 78**, Barry **85** (the highest trainer in Sinnoh) |

Plan 02's pacing targets are the leaders' levels: 14, 22, 26, 32, 37, 41, 44, 50, then 62 for the League. Plan 13 · V3's level cap table reads them.

### The later regions (what the plans intend)

Plans 19 to 23 write no level ranges of their own. Each one imports or rebuilds its source game's trainers and wild Pokémon at **that game's own levels**. Kanto comes from FireRed, Johto from HeartGold, Hoenn from Emerald, and Unova from Black 2 and White 2. Kalos, Alola, Galar and Paldea are drawn from public numbers. So as the plans stand, every region starts again near level 2 and its Champion sits somewhere in the 50s or 60s.

The figures below are from memory of the original games, not from this repository. **Approximate** means within a few levels.

| Region (source game) | First route | First leader | Last leader | Champion's strongest | Post-game peak | Sure? |
|---|---|---|---|---|---|---|
| **Sinnoh** (Platinum, in the game) | 2–3 | 14 | 50 | **62** | 85 (Barry) | from the data |
| **Kanto** (FireRed) | 2–5 | 14 | 50 | **63** | about 75 (League rematch) | leaders and Champion sure; rematch approximate |
| **Johto** (HeartGold) | 2–4 | 13 | 41 | **50** (Lance) | about 60 (Kanto's leaders), **88** (Red) | Lance and Red sure; Kanto leaders approximate |
| **Hoenn** (Emerald) | 2–3 | 15 | 46 | **58** | about 75–80 (League rematch) | leaders and Champion sure; rematch approximate |
| **Unova** (Black 2 and White 2) | 2–4 | 13 | about 50 | **59** | about 70s | approximate |
| **Kalos** (X and Y) | 2–4 | 12 | 59 | **68** | about 70 | Champion sure; post-game approximate |
| **Alola** (Ultra Sun and Ultra Moon) | 2–4 | about 12 | about 55 | about **60–65** | about 70 (Battle Tree aside) | approximate |
| **Galar** (Sword and Shield) | 2–5 | 20 | 48 | **65** | about 80 (Champion Cup again) | Champion sure; post-game approximate |
| **Paldea** (Scarlet and Violet) | 2–5 | about 15 | about 45 | **62** | about 66 (Nemona), 70s–80s in the DLC | approximate |

Every one of these regions was written for a team that starts from nothing. A carried Sinnoh Champion team at 64–71 is already above every one of their Champions except Kalos's (68).

## 2. How Gold and Silver levelled Kanto after Red and Blue

These facts are public knowledge about the original games. Where a number is uncertain, the table says so.

| | Johto (the new region) | Kanto (the old region, after Johto's League) | The last trainer |
|---|---|---|---|
| Gold and Silver (2000) | Route 29 at 2–4; Falkner 9; Lance's Dragonite **50** | Leaders about **41–58** (Brock about 41–44, Janine about 36–39, Blue's strongest **58**). Wild Pokémon stayed low: Route 1 is still level 2–4. | **Red: Pikachu 81**, the rest 73–77 |
| HeartGold and SoulSilver (2009) | Similar; Lance 50 | Leaders raised to about **50–60** | **Red: Pikachu 88**, the rest about 80–84 |

**What Gold and Silver actually did.** Johto was a whole journey from level 2 to 50. Kanto was **not** a second journey. It was the post-game: eight Gym Leaders about 10 levels above Lance, then one last trainer about 30 levels above him. The new region's wild Pokémon were never raised (players have complained about Kanto's level-2 routes ever since). The climb across both regions went 50, then about 60, then 81 to 88, and stayed under 100.

**FireRed and LeafGreen, and Emerald,** did the same with their post-games. The Sevii Islands and the League rematch sit in the 60s to 70s, after a Champion in the low 60s. **No main-series game has ever put a story trainer above level 88.** The series raises levels after a League by about 10 to 25 levels **once**, in one post-game. It has never had to do it eight times in a row.

So the user's model, "levelled for a team that has already won a League", is the shape of **a post-game**. Plan 18 chains nine whole stories one after another, which is the hard part.

## 3. The modern EXP in this game

This section explains the code in plain words. The code is in `Battle/Sim/BattleCore.cs`, `AwardExp`, and in `Battle/Sim/Formulas.cs`, `ScaledExp`. `docs/mechanics/rulings.md` has the EXP rows.

- **Who gets EXP.** Every Pokémon of the team that is still standing gets EXP from every foe that faints. A Pokémon that fought gets the full amount; the rest get half. This is the "Exp. Share always on" of the newer games. The game has no switch for it: the modern rules are the only rules in play since plan 06 · R19.
- **How much.** The formula is the foe's base EXP times its level, divided by 5, then multiplied by a scale that compares the two levels: `((2 × foe level + 10) / (foe level + own level + 10))` to the power 2.5. There are bonuses of half again for a traded Pokémon or a Lucky Egg, and a fifth more for a Pokémon past the level where it would have evolved. A trainer's Pokémon gives no bonus under these rules (Platinum gave half again). A Pokémon at level 100 gets nothing.
- **What the scale does.** A Pokémon **above** its foe gets less EXP, and one **below** gets more. For example, a Champion's ace (base EXP 250) at level 60 gives:

| Gainer's level | 50 | 60 | 70 | 80 | 90 | 100 | 120 |
|---|---|---|---|---|---|---|---|
| EXP from that one Pokémon | 3,665 | 3,001 | 2,493 | 2,098 | 1,786 | 1,535 | 1,162 |
| EXP to the next level (Medium Slow) | 7,766 | 11,462 | 15,878 | 21,014 | 26,870 | 33,446 | 48,758 |

The scale slows an over-levelled team, but it does not stop it. A team 20 levels above its foes still gains about two thirds of what a team at their level would. Hundreds of easy fights still add up.

- **Platinum's own EXP, for comparison.** Platinum gives base EXP × level / 7, shared only among the Pokémon that fought (unless one holds an Exp. Share). Trainers' Pokémon give half again, and there is no level scale. The code still has it (`Formulas.ExpShares`), since `Ruleset.Platinum` remains the tested baseline.

## 4. The simulation

### How it was made

A throwaway test, which is not in the repository, played Sinnoh's story with this game's own data and formulas:

- **The foes.** The simulation took every trainer the area files and the scripts put on the way to the Hall of Fame, in the story's order and in Platinum's Gym order. That is every route trainer, the Gym trainers, the rivals, Team Galactic, the Elite Four and Cynthia: **797 trainer Pokémon**. It also took 0, 5 or 15 wild battles in every area with grass, drawn from the area's own table.
- **The team.** The team was Turtwig, Starly and Shinx from the start, Buizel from Route 205, and Ponyta and Gible from Route 206 and Wayward Cave. Each one evolved at its level by the species data.
- **Who fights.** In the "balanced" runs, the lowest-levelled member fights each foe. In the "ace" runs, the starter fights everything. Every member gets its share by `Formulas.ScaledExp`, and the level comes from `Pokemon.GetExpForLevel`.
- **What is left out.** Faints, held items (a Lucky Egg), Rare Candies and the later games' EXP Candies are not counted. A player who skips optional trainers ends a few levels lower, roughly like the "0 wild" column.
- **The later regions.** These were modelled as Sinnoh's own story again, with every foe's level moved in a straight line onto the region's range. The region has the same number of fights in the same order, and the same species at the same stage of the story.

### Sinnoh: where the team stands at each badge

| | Leader's strongest | Modern EXP, 0 wild | Modern, 5 wild per area | Modern, 15 wild per area | Modern, starter fights all (5 wild) | Platinum's EXP, 5 wild |
|---|---|---|---|---|---|---|
| Badge 1 (Roark) | 14 | 14 | 15 | 17 | 14–17 | 13–14 |
| Badge 2 (Gardenia) | 22 | 23–24 | 24–25 | 25–26 | 22–28 | 21 |
| Badge 3 (Fantina) | 26 | 28–30 | 30–32 | 32–34 | 27–36 | 24–25 |
| Badge 4 (Maylene) | 32 | 34–35 | 36–37 | 39–40 | 33–43 | 28–29 |
| Badge 5 (Wake) | 37 | 39–40 | 41–42 | 44–45 | 37–48 | 32 |
| Badge 6 (Byron) | 41 | 46–47 | 48–49 | 51–52 | 44–55 | 37–38 |
| Badge 7 (Candice) | 44 | 51–52 | 54–55 | 58–59 | 50–62 | 41–42 |
| Badge 8 (Volkner) | 50 | 58–59 | 61–62 | 65–66 | 56–70 | 47–48 |
| **Hall of Fame (Cynthia)** | **62** | **63–64** | **66–67** | **70–71** | **62–76** | **51** |
| Battle Zone done | 61 | 73–74 | 76–77 | | | |
| League rematch (Cynthia) | 78 | 78 | 80–81 | | | |

Under the modern EXP the team is a few levels **above** every leader from the third badge on, and level with or above Cynthia. Under Platinum's EXP it is about 10 levels **below** Cynthia (which matches the common experience of Platinum, where players grind before the League).

### How many levels one region gives

The team arrives at a level, every member at the same level, and fights a whole Sinnoh-sized region whose foes run from 10 below that level up to the figure shown. Modern EXP, 5 wild battles per area, no cap:

| Team arrives at | Foes up to arrival level | Foes up to +10 | Foes up to +20 |
|---|---|---|---|
| 50 | ends 79 | ends 83 | ends 87 |
| 60 | ends 89 | ends 93 | ends 96 |
| 70 | ends 98 | ends 102 | ends 105 |
| 80 | ends 108 | ends 111 | ends 113 |
| 90 | ends 117 | ends 119 | ends 122 |

**Every region adds about 30 levels**, whatever the trainers' levels within reason. If the trainers stay level, the team pulls 20 or more levels ahead of them by the region's end.

### The regions one after another

These are the levels at each region's Champion. "Team" is the carried team's level when it reaches that Champion. All runs use the modern EXP and 5 wild battles per area, and start from the Sinnoh Champion team at 66–67.

| Region | Original levels (cap 100) | Stacked +12 a region (no cap) | Stacked +12, cap 100 | Compressed under 100 (+4 to +5 a region) | Fair climb (modern EXP) | Fair climb (Platinum's EXP) |
|---|---|---|---|---|---|---|
| | foes → team | foes → team | foes → team | foes → team | foes → team | foes → team |
| Sinnoh | 62 → 67 | 62 → 67 | 62 → 67 | 62 → 67 | 62 → 67 | 62 → 51 |
| Kanto | 63 → 78 | 74 → 98 | 74 → 98 | 67 → 95 | 104 → 108 | 66 → 70 |
| Johto | 50 → 83 | 86 → 117 | 86 → **100** | 71 → **100** | 136 → 140 | 82 → 86 |
| Hoenn | 58 → 88 | 98 → 134 | 98 → 100 | 76 → 100 | 162 → 166 | 95 → 98 |
| Unova | 54 → 92 | 110 → 148 | 110 → 100 | 81 → 100 | 185 → 189 | 106 → 110 |
| Kalos | 68 → 98 | 122 → 162 | 122 → 100 | 86 → 100 | 205 → 209 | 116 → 120 |
| Alola | 63 → **100** | 134 → 175 | 134 → 100 | 90 → 100 | 224 → 228 | 126 → 130 |
| Galar | 65 → 100 | 146 → 188 | 146 → 100 | 95 → 100 | 242 → 245 | 135 → 139 |
| Paldea | 66 → 100 | 158 → 200 | 158 → 100 | 100 → 100 | **258 → 262** | **143 → 147** |

How to read it:

- **Original levels.** If each region keeps its own game's levels, the carried team is far above every trainer from Kanto's first route on. The last regions add nothing because the team is already at 100.
- **Stacked +12 a region.** Each region starts 5 below the last Champion and ends 12 above it, which is the "+10 to 15 a region" idea. Under 100 that fits Kanto (74), Johto (86) and Hoenn (98), three regions. But the team outruns the trainers anyway: it is 24 levels above Kanto's Champion and 31 above Johto's. With a cap of 100, it is at 100 from Johto's end.
- **Compressed under 100.** Eight Champions spread between 67 and 100 give regions only 4 or 5 levels each. The team reaches 100 at Johto's end, and every region after that is a team of six 100s against trainers in their 70s to 90s.
- **Fair climb.** For each region, the Champion is set so that the team ends about 4 above it, as Sinnoh's does (67 against 62). Under the modern EXP the climb needs Champions at 104, 136, 162 and so on, up to about **258** in Paldea. Under Platinum's EXP it needs about **143**.

## 5. The options

### (A) Keep the cap at 100 and fit the later regions under it

Every later region's trainers and wild Pokémon are moved into a band between Sinnoh's levels and 100. For example, Kanto 56–67, Johto 61–71, and so on up to Paldea 89–100.

- **What happens:** the team reaches 100 in Johto. From Hoenn on, every region is six level-100 Pokémon against trainers in their 70s to 90s, and wild Pokémon in the 70s to 90s on every first route. A region's challenge would have to come from the trainers' teams, items and AI (plan 13 · V2's Challenge), not from levels.
- **Work:** every imported trainer and wild table of eight regions needs its levels mapped. That is one table per region (a "from, to" pair in the region file) applied as the region is imported or loaded. It is cheap in code, but it changes every level the region plans import.
- **Cost:** levels stop meaning anything after Johto. New Pokémon caught in a later region arrive at 70 to 90 and never need levelling. This is not how Gold and Silver felt.

### (B) Raise the cap above 100

The later regions climb as in the "fair climb" columns: to about 260 under the modern EXP, or about 150 if the later regions use Platinum's EXP rule.

What changes in the code, from a search of the engine for where 100 is assumed:

| Place | What assumes 100 |
|---|---|
| `Models/Pokemon.cs` | The level is clamped to 1–100 in three places: the two constructors and `LevelTo` (lines about 711, 743, 788). `GainExp` stops at 100 (about 1014, 1019). The EXP bar is full at 100 (about 609). A save holding level 150 would be cut to 100 as it loads, through the constructor. |
| `Models/Pokemon.cs`, `GetExpForLevel` | **The Erratic growth formula breaks above 100.** Its last branch, `level³ × (160 − level) / 100`, falls after 100 and turns negative at 160. Fluctuating has no official meaning past 100. Both need a rule of our own. Fast, Medium Fast, Medium Slow and Slow just keep growing; for example, level 200 costs 9.0 million EXP at Medium Slow. |
| `Battle/Sim/BattleCore.cs`, `AwardExp` | Nobody at 100 gains EXP (about lines 1205, 1224). The `EvsAtLevel100` rule is named for 100. |
| `Models/Effort.cs` | "Effort only under 100" (Platinum's rule, in the comment and the code it describes). |
| `Models/DayCare.cs` | Growth stops at 100 (about lines 120, 138). |
| `Models/ItemUse.cs` | The Rare Candy is refused at 100 (about lines 97, 175). |
| `UI/ModernUi.Party.cs` | The EXP bar is drawn full at 100 (about line 213). |
| `Models/PoketchApps/DayCareCheckerApp.cs` | Clamps the level shown to 100 (about lines 54, 57). |
| `Battle/Sim/Formulas.cs` and the damage calculator | The damage formula multiplies attack × power × (2 × level / 5 + 2) in whole numbers. At level 260 this is about 144 million, which is still under the limit, but it needs a check for overflow at whatever new cap is chosen. A stat at level 260 is about 1,400 at most, which is fine. |
| Tests | `GoldenLevelTests` holds every level between 1 and 100; others make Pokémon at 100. |
| Plan 13 · V3, plan 07 · O5 and O6 | The cap table, obedience by badges, and Flat 50 for online rooms all read the level and work at any cap. A Pokémon above 100 could never be traded to another game, but this game trades only with itself. |
| The screen | "Lv100" already has three digits, so levels up to 999 need no wider text. |
| The save | Levels and EXP are whole numbers already, so the format doesn't change. Only the clamp on loading does. |

- **What happens:** the curve keeps climbing in all nine regions. The level numbers become unlike any Pokémon game (Paldea's Champion at about 260). Battles at 260 against 260 play like battles at 100, because stats and damage both grow in step with the level. Without a climb, the over-levelling of option A comes back.
- **Work:** the code changes in the table above, a growth rule past 100 for Erratic and Fluctuating, and **re-levelling every trainer of eight regions** to the climb (the same mapping as in A, with higher numbers).

### (C) Scale on the crossing, or scale the carried team

- **C1, trainers scaled to the team** (plan 13 · V3's `ScaleTrainers`). Each trainer is raised to the team's level as the battle starts. The team still gains about 30 levels a region, so it reaches 100 in Johto. After that every trainer is at 100 too, and the result is the same as option A from Hoenn on.
- **C2, the region's ceiling** (recommended). In a region before its Hall of Fame, a Pokémon above the region's **ceiling** (the next leader's strongest level, by the region's badges: plan 13 · V3's cap table, a row per region) fights at the ceiling. The battle gets a copy brought down to that level, exactly as the Battle Frontier already brings Pokémon down to 50 (`FrontierRules.ForBattle`). The Pokémon also gains no EXP while it is at or above the ceiling (V3's clamp). Its real level never goes down. Once the region's Hall of Fame is set, the ceiling lifts, and the region's post-game is fought at full strength. Pokémon caught in the region level normally up to the ceiling.
  - **What happens:** every region's story is played at its own game's levels, as plans 19 to 23 already import them, and stays a real challenge. The old team is still useful, as a strong but capped partner. The carried team's real level only rises in post-games. The post-games are where "levelled for a Champion" applies, and they can climb gently across the chain: Sinnoh's rematch is 78 and Barry is 85, then each region's post-game a few levels higher, reaching 100 at Paldea's. That fits under 100, and it is Gold and Silver's shape: the higher levels come in the content after a League.
  - **Work:** V3's cap table with a row per region (plan 23 already asks for Alola's and Paldea's), the copy at a level for battles in a region before its Hall of Fame (code that exists for the Frontier), the EXP clamp (V3), and a line in the party screen and the battle saying a Pokémon is held to the ceiling (style guide first). Only the post-games' trainers would be re-levelled, a few dozen per region rather than every trainer.
  - **Things to settle:** whether a capped Pokémon keeps every move it knows. The Frontier keeps them, so a level-14 Torterra could still know Earthquake, which is strong in Kanto's first Gym. The other choice is to allow only moves it could know at the ceiling. Also whether the ceiling applies to Pokémon caught in the region itself (it should, as V3's cap does).
- **C3, levels reset on crossing.** The carried Pokémon go back to a low level for good. This is destructive, and no Pokémon game does it to the player's own team. It is listed only to be complete.

### (D) The team does not come along: excluded

This was plan 18's original recommendation: a fresh journey, with the party resting in the boxes until the region's Hall of Fame. **You decided on 2026-10-12 that the team comes along**, so it is not offered here. Option C2 is the nearest thing that keeps your decision: the team does come along, into every battle, but within the region's ceiling.

## 6. Recommendation

**C2, the region's ceiling, with post-games that climb to 100.** In numbers:

| Region | Story levels (its own game's) | Ceiling during the story | Post-game levels (re-levelled) | Carried team after the region's post-game |
|---|---|---|---|---|
| Sinnoh | 2 → 62 | 14, 22, 26, 32, 37, 41, 44, 50, 62 | 51 → 85 (as it is) | about 78–81 |
| Kanto | 2 → 63 | FireRed's leaders, then 63 | about 80 → 88 | about 85 |
| Johto | 2 → 50 | HeartGold's leaders, then 50 | about 84 → 90 | about 88 |
| Hoenn | 2 → 58 | Emerald's, then 58 | about 87 → 92 | about 90 |
| Unova | 2 → 59 | Black 2 and White 2's, then 59 | about 89 → 94 | about 92 |
| Kalos | 2 → 68 | X and Y's, then 68 | about 91 → 96 | about 94 |
| Alola | 2 → about 63 | the trials' and Kahunas', then the Champion's | about 93 → 97 | about 96 |
| Galar | 2 → 65 | Sword and Shield's, then 65 | about 95 → 98 | about 98 |
| Paldea | 2 → 62 | Scarlet and Violet's, then 62 | about 97 → 100 | 100 |

The post-game column is a proposal to be fixed region by region when each plan comes. It continues the series' own post-game step: 10 to 25 levels above the Champion, and never past 100.

Why this option:

1. It keeps the cap at 100 and every level number familiar.
2. It keeps every region's story as its own game balanced it, with no re-levelling of thousands of imported trainers.
3. Pokémon caught in each region matter, because the old team can't carry the story at level 70 or more.
4. It uses rules the game already has: the Frontier's copy at a level, V3's cap and clamp, and W2's badges per region.
5. It gives "levelled for a team that has already won a League" a place, the post-game, which is where Gold and Silver put it.

**If you want the old team to play every new story at full strength instead,** the only honest way is option B. The cap rises to about 150 with Platinum's EXP rule in the later regions, or about 260 with the modern one. Every later region's trainers are re-levelled to climb. Option A, a cap of 100 with compressed regions, is the cheapest, but levels stop mattering after Johto.

**The question for you:** should the old team play each new region's story at that region's own levels, held to its ceiling (C2, recommended), or at full strength with a raised cap (B)?

## 7. The rest of W4 (not part of this study)

Plan 18's W4 still lists, for the session that builds the crossing:

- `Pokemon.Region`, saved, and set on catch, gift, hatch and trade.
- `Models/Crossing.cs`, with `CanTake`, `Depart` and `Arrive`.
- `PCScreen`'s greyed rows with the reason. With "the team comes along" these are only needed under C2, if it greys nothing, or not at all. That is to be rewritten from your answer.
- `RegionLink.Vessel`, `Transport.Plane` and the links' proposal.
- `AttendantLines` with the vessel.
- The rival's name per region: `SaveData.RivalNames`, `PlayerIdentity.SetRival`, and the script command `namerival` with its parser and runner cases, both hosts, its line in `ScriptTests` and its row in `docs/scripts.md`.
- `HeadlessScriptHost.Open` playing the travel it skips today.
- `RegionTests`' attendant rule made strict for built links.
- `docs/mechanics/rulings.md`'s "Regions" section.
- The tests, the `30e_pc_resting` shot, and "done when" the headless host crosses with the rules holding.

Plan 18's design text and its decision 2 still describe the fresh journey you turned down. The building session rewrites them from your choice here. Under C2 it also adds the ceiling to plan 13 · V3, which becomes a dependency of W4.
