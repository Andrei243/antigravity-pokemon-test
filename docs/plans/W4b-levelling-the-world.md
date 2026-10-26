# Levelling the world for a team that crosses every region

*Plan 18 · W4b, a proposal. Written 2026-10-26 (wave 62) for the user's decision. It changes no data, no engine and no test. Every number below was worked out by a throwaway script against this repository's data at `bc882e7a` (the region files, `Data/trainers.json`, each `Data/world/<region>/trainers.json`, `Data/our-trainers.json`, the area files, `species.json` and `PokemonPlatinumTests/Golden/levels.txt`); none was typed from memory. Follows [W4-level-curve-study.md](W4-level-curve-study.md), whose option B the user chose on 2026-10-21.*

## The short answer

- **What exists today (W4 part a) is a straight line per region.** Each later region's file maps its own game's levels onto the climb with two points: Kanto `[[2, 62], [63, 104]]`, Johto `[[2, 104], [50, 136]]`, Hoenn `[[2, 136], [58, 162]]`, Unova `[[2, 162], [59, 185]]`. The mapping is applied as a Pokémon is met, only in a region the game didn't begin in (`GameEngine.LevelHere`, `Trainer.Relevelled`, `Pokemon.Relevel`).
- **It works, but it leaves seven gaps.** The four that matter most:
  1. **Trainers' Pokémon never evolve with their new level.** A Youngster's Rattata of level 4 meets a Sinnoh Champion as a Rattata of level 105. In Johto 773 trainer Pokémon of 1,776 have passed a plain level evolution of the games and are still in their old stage; in Hoenn 864 of 1,828. Wild Pokémon are the same (Johto: 1,705 of 2,022 wild slots).
  2. **The post-game is not designed, only extrapolated.** Past the Champion the straight line simply goes on: Kanto's post-game tops out at 112, Johto's at 161 (Red), which is level with Hoenn's Champion (162), Hoenn's at 171 and Unova's at 193.
  3. **The players spread apart.** A player who fights about as much as the W4 study's player stands 5% under Kanto's Champion at the Hall of Fame, and falls further behind in each region, to 12% under Unova's. A player who fights everything and plays each post-game first stands 14% to 18% over. Neither end is kept in check.
  4. **The Challenge difficulty fades.** Its +4 levels are 6.5% of Cynthia's 62 but 2.2% of Unova's 185.
- **Three designs are proposed.** (A) keep the climb and finish it (a post-game point, trainers' stages, a cap table per region). (B) refit the climb to the typical player, which lowers every Champion and means re-levelling the 400 evolutions of D14 and the 72 moves of R31. (C) keep the climb and add two rules of EXP in a crossed region: half as much again from its foes, and a soft ceiling that stops EXP (never a level) at 10% over the next Leader until that Badge is won.
- **Recommended: C.** In the simulation it holds every player from the light one to the one who fights everything within 4% under to 10% over every Champion, keeps D14's and R31's levels as built, and costs three sessions of about the size of W4's part a.

## 1. What the code does today

| Piece | Where | What it does |
|---|---|---|
| The mapping | `Data/LevelMapping.cs`, a region file's `levels` | Straight lines between pairs of "own level, level met"; past the last pair the last line goes on; clamped to 1..260. |
| Who it applies to | `GameEngine.CrossedLevels`, `LevelHere` | Every region but the one the game began in (`GameFlow.BeganIn`); Sinnoh has no pairs, so Sinnoh is always Platinum's. |
| Wild Pokémon | `GameEngine.Battles.cs` (`LevelHere(rng.Next(...))`), a script's `wildbattle` | The drawn level is mapped; the species stays. |
| Trainers | `StartTrainerBattle`, `Trainer.Relevelled` | The battle gets copies at the mapped level; moves learned stop at 100 (R31's question 8: a later region's trainer keeps their own game's moves); the species stays; the prize money is the copy's own game's. |
| Gifts | a script's `givepokemon` (`Pokemon.Relevel(LevelHere(...))`) | Mapped; the species stays, so a starter of level 5 comes at the region's first-route level in its first stage and evolves at its next level-ups. |
| The level cap of a run | `Models/LevelCaps.cs` (plan 13 · V3) | A table for Sinnoh alone; every other region is uncapped. |
| Difficulty | `DifficultyRules.Prepare` | Challenge +4/+3/+1 levels by tier, Easy the same down, `ScaleTo` the lead under the run's option; all applied after the mapping. |
| EXP | `Formulas.ScaledExp`, `BattleCore.Give` | The modern formula; Easy gives three halves (`DifficultyRules.ScaleExp`); the run's cap clamps it (`CoreSetup.RunLevelCap`). |
| Past 100 | `Pokemon.GetExpForLevel` | Erratic and Fluctuating grow as the level's cube times their 100's (W4 part a's rule); the others by their formula. |
| Fixed levels | Frontier (50, or the Pokémon's own if lower), Battle Subway and Dome 50, Master Trainers 75 | Untouched by the mapping. |

### The four built regions, own levels and mapped levels

Leaders are each Leader's first battle, by Badge order; "post-game top" is the strongest trainer the region's data has.

| Region | First route | Leaders 1 to 8 (own → today) | Champion | Post-game top (own → today) | Wild |
|---|---|---|---|---|---|
| Sinnoh | 2 | 14, 22, 26, 32, 37, 41, 44, 50 | 62 | Barry 85 | 1–100 (the Resort Area's Magikarp 1–100) |
| Kanto | 62 | 14 21 24 29 43 43 47 50 → 70 75 77 81 90 90 93 95 | 63 → 104 | the Champion's rematch 75 → 112 | 2–67 → 62–107 |
| Johto | 104 | 13 17 19 25 31 34 35 41 → 111 114 115 119 123 125 126 130 | 50 → 136 | Red 88 → 161 | 2–50 → 104–136 |
| Hoenn | 136 | 15 19 24 29 31 33 42 46 → 142 144 146 149 149 150 155 156 | 58 → 162 | Steven 78 → 171 | 2–50 → 136–158 |
| Unova | 162 | 13 18 24 30 33 39 48 51 → 166 168 171 173 175 177 181 182 | 59 → 185 | Benga and the last Boss Trainers 80 → 193 | 2–70 → 162–189 |

`levels.txt` agrees with the area files on every wild range above. It lists only the trainers placed on a map (Kanto 416, Johto 213, Hoenn 437, Unova 256, Sinnoh 799 ids), whose tops are lower (Kanto 54, Johto 75, Hoenn 45, Unova 55, Sinnoh 61); the Leaders, Elite Four, Champions and rivals are scripted and are read from the trainers' files.

### The seven gaps

1. **No stage follows the level.** Counted along plain level evolutions only (no stone, trade, item or time), at the mapped level:

   | Region | Trainer Pokémon | Past an evolution of the games | Past one into a species of ours (D14) | Leaders', Elite Four's and Champions' past any evolution | Wild slots past an evolution (into ours) |
   |---|---|---|---|---|---|
   | Kanto | 1,650 | 766 | 4 | 29 of 103 | 1,356 of 2,080 (0) |
   | Johto | 1,776 | 773 | 279 | 125 of 216 | 1,705 of 2,022 (606) |
   | Hoenn | 1,828 | 864 | 174 | 180 of 238 | 1,287 of 1,741 (172) |
   | Unova | 1,808 | 564 | 117 | 110 of 160 | 836 of 2,160 (159) |

   Examples: Johto's Joey's Rattata 4 → 105 (would be Raticate, then our Ratchisel at 105); Falkner's Pidgeotto 13 → 111; Bugsy's Metapod 15 → 113; Hoenn's Sidney's Mightyena 46 → 156 (our Hyaenarch, by a condition). Counting every evolution (a line with one way and no level taken at 30, as `DifficultyRules.StageAt` does going down) the trainers' numbers rise to 1,104, 1,357, 1,595 and 1,407.
2. **The post-game runs into the next region.** See the table: Johto's Red is at 161, Hoenn's Champion at 162.
3. **The players spread apart** (section 3).
4. **The Challenge fades** (+4 is 3.8% of Kanto's Champion, 2.9% of Johto's, 2.5% of Hoenn's, 2.2% of Unova's, 1.6% of Paldea's 258).
5. **No cap table past Sinnoh.** A run with the level cap (V3, the Nuzlocke's companion) is uncapped from Kanto on.
6. **Rematches of our own sit under their region's own post-game.** Hoenn's Elite Four's rematch teams of ours are written with aces of 57 to 63, so they meet the player at 162 to 164, the first Champion's level; Wallace's rematch (66) at 166, under Steven (171). The Master Trainers stay at 75 against a chosen Pokémon that may be 200.
7. **Only a game begun in Sinnoh is levelled.** The pairs assume a team that comes from Sinnoh's Hall of Fame: a game begun in Kanto with `--region Kanto` (a development switch) meets Johto at 104 with a team of about 67; nothing ever levels a region the player comes back to by a harbour (plan 18 · W8).

## 2. The simulation

The script plays every region's foes in the order of their levels (the story's order is not in the data): each trainer once, starter variants folded, a rematch tier counted as post-game, and five wild battles in every area with land encounters at the middle of its range. The team is six Pokémon of the Medium Slow rate; the lowest fights each foe and the rest share by `Formulas.ScaledExp` (fought in full, the rest half), levels by `GetExpForLevel`. It leaves out faints, Lucky Eggs, Rare Candies and the Easy difficulty.

**Calibration.** Fighting every foe it lists, it brings a Sinnoh team from 5 to 79–80 at the Hall of Fame, more than the W4 study's careful run (66–67), because the data holds trainers the story never puts in the way. Fighting a share of them, evenly spread, it gives:

| Share of the battles | Sinnoh at the Hall of Fame | After Sinnoh's post-game |
|---|---|---|
| 0.30 | 60–61 | 70 |
| **0.45** | **66–67** | **77** |
| 0.60 | 71 | 82 |
| 1.00 | 79–80 | 92 |

0.45 reproduces the study's player (66–67, and 76–77 after the Battle Zone) and is called **the typical player** below; 1.00 is **the player who fights everything**; 0.30 a light player. Two habits are run for each: crossing at each Hall of Fame, or playing each post-game first. The numbers are within a few levels and are for comparing designs, not promises.

**A level gap matters less as levels rise.** At level 62 a Pokémon 20 levels above its foe hits 68% harder than an equal one (35.8% of the foe's HP against 21.3%); at 162, 25% harder (25.9% against 20.7%); at 200, 20%. So gaps below are given as a share of the Champion's level, which reads the same in every region.

### Today's mapping, simulated

Team at each Hall of Fame against the region's Champion:

| Player | Kanto (104) | Johto (136) | Hoenn (162) | Unova (185) |
|---|---|---|---|---|
| light, crossing at once | 90 (−13%) | 112 (−18%) | 131 (−19%) | 147 (−21%) |
| typical, crossing at once | 99 (−5%) | 123 (−10%) | 145 (−10%) | 162 (−12%) |
| typical, post-game first | 102 (−2%) | 129 (−5%) | 157 (−3%) | 179 (−3%) |
| fights everything, at once | 119 (+14%) | 149 (+10%) | 174 (+7%) | 195 (+5%) |
| fights everything, post-game first | 123 (+18%) | 155 (+14%) | 189 (+17%) | 215 (+16%) |

And that last player after Unova's post-game is at 232. (The study projected 108 at Kanto's Hall of Fame for its player against these Champions; with Kanto's own trainers the typical player reaches 99. Kanto's FireRed trainers are fewer and lighter than Sinnoh's re-levelled ones, which the study used.)

## 3. The designs

Each design keeps what part a built: the pairs in the region file, the mapping applied on meeting, the cap of 260.

### Design A: keep the climb and finish it

The Champions stay where they are (104, 136, 162, 185), and the gaps 1, 2, 4, 5, 6 and 7 are closed by the common pieces of section 4.

- **The post-game is a third pair**, chosen, not extrapolated: the region's post-game top is met at `min(1.15 × Champion, next Champion − 2, 260)`. Kanto `[[2,62],[63,104],[75,120]]`, Johto `[[2,104],[50,136],[88,156]]`, Hoenn `[[2,136],[58,162],[78,183]]`, Unova `[[2,162],[59,185],[80,203]]`; with the study's later Champions, Kalos 205 → 222, Alola 224 → 240, Galar 242 → 256, Paldea 258 → 260. `LevelMapping` already takes any number of pairs, so this is data alone.
- **Simulated:** the story is today's (the table above); post-game first, the typical player stands at −2%, −5%, −3%, −3%, the one who fights everything at +18%, +14%, +17%, +16%, and ends Unova's post-game at 232.
- **Cost:** the smallest. **Risk:** the spread of section 2 stays; a light player arrives in Unova 21% under.

### Design B: refit the climb to the typical player

Each Champion is put where the typical player, crossing at once, ends 4 levels over it, as Sinnoh's does (66–67 against 62). The first route of each region is the last Champion's level; the post-game the Champion + 12.

| Region | Pairs | Typical arrives | Typical at the Hall of Fame |
|---|---|---|---|
| Kanto | `[[2,62],[63,91],[75,103]]` | 66–67 | 95–96 |
| Johto | `[[2,91],[50,111],[88,123]]` | 95–96 | 115 |
| Hoenn | `[[2,111],[58,128],[78,140]]` | 115 | 132–133 |
| Unova | `[[2,128],[59,142],[80,154]]` | 132–133 | 146 |

- **Simulated:** the typical player is +4 at every Champion, as designed; the one who fights everything is +23, +27, +31 and +33 levels over (crossing at once) and +28 to +51 with the post-game first, ending Unova's at 206–207.
- **Cost:** heavy. D14's evolutions were placed on today's climb ("a region's post-game reaches them"): under B Hoenn's 51 (140 to 160) and Unova's 57 (165 to 239) are out of reach in their own region, so most of the 400 levels of D14 and the 72 of R31 would be moved and approved again. It leaves room under the cap (Unova's Champion 142).
- **Risk:** it serves one player and widens the spread.

### Design C: keep the climb, and give a crossed region its own EXP (recommended)

Design A's pairs, plus two rules of EXP that only apply in a region the game crossed into:

1. **Its foes give half as much again** (×1.5, after the formula, like a Lucky Egg). This pulls the typical player up to the climb.
2. **A soft ceiling.** A Pokémon gains no EXP above 110% of the region's next milestone (the mapped level of the next Leader whose Badge isn't held, then the Champion's, then after the Hall of Fame the post-game top's). It keeps its level and fights at full strength: the user's option B stands. It only stops growing until the region catches up. This is V3's cap (`CoreSetup.RunLevelCap`, `LevelCaps.Allowed`, the bag's Rare Candy), switched on in a crossed region with a margin.

The ceilings this gives (Leaders 1 to 8, then the Champion; after the Hall of Fame the post-game's):

| Region | Ceiling by Badges held, 0 to 8 | After the Hall of Fame |
|---|---|---|
| Kanto | 77 82 84 89 99 99 102 104 114 | 132 |
| Johto | 122 125 126 130 135 137 138 143 149 | 171 |
| Hoenn | 156 158 160 163 163 165 170 171 178 | 201 |
| Unova | 182 184 188 190 192 194 199 200 203 | 223 |

**Simulated** (team at each Hall of Fame against the Champion, and after the post-game where it is played first):

| Player | Kanto (104) | Johto (136) | Hoenn (162) | Unova (185) |
|---|---|---|---|---|
| light, at once | 97–98 (−7%) | 123 (−10%) | 144–145 (−11%) | 162 (−12%) |
| light, post-game first | 100 (−4%) → 107–108 | 128 (−6%) → 141–142 | 156–157 (−4%) → 166–167 | 178–179 (−4%) → 193 |
| typical, at once | 105 (+1%) | 133–134 (−2%) | 158 (−2%) | 177–178 (−4%) |
| typical, post-game first | 105 (+1%) → 114 | 138 (+1%) → 153–154 | 170 (+5%) → 181 | 194–195 (+5%) → 211 |
| 0.60, post-game first | 106 (+2%) → 117 | 145–146 (+7%) → 162–163 | 171–172 (+6%) → 185–186 | 200–201 (+8%) → 220 |
| fights everything, at once | 107 (+3%) | 149 (+10%) | 172–173 (+6%) | 202–203 (+9%) |
| fights everything, post-game first | 107 (+3%) → 123–124 | 149 (+10%) → 171 | 172–173 (+6%) → 194 | 203 (+10%) → 223 |

Every player from 0.45 up is within −4% and +10% of every Champion, against −12% to +18% today. The light player is still behind (−12% at Unova, a little better than today's −21%).

The two rules were tried apart and at other strengths, to see that each earns its place (this comparison was run with every post-game top at 1.15 × the Champion, so Hoenn's at 186 and Unova's at 213; the rest of the section uses Design A's pairs):

| Variant | Typical, at once (Unova) | Fights everything, post-game first (Unova) |
|---|---|---|
| neither (Design A) | −12% | +16%, 232 after |
| ceiling +10% alone | −12% | +9%, 223 after |
| bonus ×1.5 alone | −4% | +27%, 255 after |
| bonus ×1.25 and ceiling +10% | −8% | +10%, 228 after |
| **bonus ×1.5 and ceiling +10%** | **−4%** | **+10%, 232 after** |
| bonus ×1.5 and ceiling +8% | −4% | +8%, 230 after |

**D14's evolutions stay where they are and stay in reach.** Under C the team after each post-game is Kanto 114 to 124 (38 of Kanto's 54 evolutions are at or under 120), Johto 153 to 171 (34 of 35 at or under 160), Hoenn 181 to 194 (all 51), Unova 211 to 223 (52 of 57 at or under 200). Sinnoh's 39 (101 to 150) come in Kanto and Johto, as D14 planned.

**Cost:** three sessions (section 5). **Risks:** a ceiling is close in spirit to the study's C2, which the user turned down; it differs in that it never lowers a level or makes a copy, so a carried Pokémon still fights at its own level. A player who arrives far over a region's first routes (the post-game-first player arrives in Hoenn at 171 against routes of 136) still finds the first half easy; the ceiling only stops it pulling further ahead. The bonus and Easy's three halves stack unless the user says otherwise.

### Two designs tried and set aside

- **The ladder shifted to meet the team on arrival** (the region's pairs moved up on the first arrival by however far the team is over the first route, saved). It compounds: a higher region gives more EXP, so the next arrival is higher still. The player who fights everything and plays post-games first meets shifts of 25, 46, 71 and 91 levels and reaches 260 at the end of Unova's post-game, with five regions to go. Held to +10 a region it changes little (+50 over Unova's Champion).
- **Every foe scaled to the lead** (`ScaleTrainers`, made the rule in a crossed region). Every battle is even, so nothing is a climb, and the team still grows: the typical player ends Unova at 149, the player who fights everything at 212–213, and nothing in the world says where a region is hard. It stays what it is: a run option.

## 4. Pieces every design needs

1. **Trainers take the stage their level allows.** `Trainer.Relevelled` gains a forward rule beside `DifficultyRules.StageAt` (which already goes the other way): along the species' own evolutions, a plain level evolution when the mapped level reaches it, and a line with one way and no level at 30 (`UnleveledStage`) or 16 for friendship (`FriendshipStage`), as `StageAt` reads them; a line with several ways and no level (Eevee, Gloom, Poliwhirl, Slowpoke) is left as it is; the moves chosen in the data are kept, the rest are the new species' up to 100 (part a's rule); a form keeps its form where the next stage has it. Whether this reaches species of ours is question 3.
2. **Wild Pokémon keep their species** (recommended), so each region's Pokédex can still be completed in a crossed game; one caught evolves at its first level up, and again at the next if it has passed two (`Evolution.Find` takes one step a level). Question 4.
3. **A cap table for every region**, own levels by Badge in the region file (`caps`: Kanto `[14,21,24,29,43,43,47,50,63]`, Johto `[13,17,19,25,31,34,35,41,50]`, Hoenn `[15,19,24,29,31,33,42,46,58]`, Unova `[13,18,24,30,33,39,48,51,59]`, held to the trainers' files by `LevelCapTests` as Sinnoh's is), read by `LevelCaps.TableOf` and mapped where the region is crossed into. V3's run cap uses it as it is; Design C's ceiling uses it with the margin.
4. **Challenge by share, not by levels**, in a crossed region: Chief +6.5%, Boss +5%, everyone +2% of the Pokémon's level (Sinnoh's +4, +3, +1 at its levels), so +12 for Unova's Champion's ace instead of +4; Easy the same down. Question 6.
5. **Rematches and fixed-level battles.** The rematch teams of our own for a crossed region are written at its post-game's own levels (Hoenn's Elite Four's and Wallace's between Wallace's 58 and Steven's 78, so between 162 and 183 under the pairs of Design A, where today they are 162 to 166). The Master Trainers are met at the higher of 75 and the chosen Pokémon's level (`DifficultyRules.ScaleTo` on the copy). The Frontier, the Battle Subway and the Dome keep their flat 50: level-flat rules are already fair at any level.
6. **What is left alone.** Prize money stays the copy's own game's (a crossed region pays as its own game did and its Marts cost the same); Eggs hatch at 1; gifts are mapped as today (question 8 for starters).
7. **The later regions.** Plan 23's Champions stay the study's (Kalos 205, Alola 224, Galar 242, Paldea 258) with the post-game rule above: their post-games are 17, 16, 14 and 2 levels long. Paldea's is squeezed against the cap. Question 7.
8. **The guards.** A new golden file, `levels-crossed.txt`, writes every crossed region's places and trainers as a game begun in Sinnoh meets them (mapped level and stage), beside `levels.txt`, which keeps the games' own; `LevelLadderTests` holds every region's pairs rising, each region's first route at or above the last Champion's level less 5, each post-game top under the next Champion and 260, and the cap tables equal to the trainers' files. No existing golden moves: the battle corpus and `trainers.txt` are Platinum's own levels, unmapped.

## 5. Sessions (Design C)

| Session | What | Files | Golden files that move |
|---|---|---|---|
| **W4b-1, the ladder's data** | The post-game pairs; `caps` in the four region files; `LevelCaps.TableOf` from the region files, mapped in a crossed region; `levels-crossed.txt` and `LevelLadderTests`. | `Data/regions/*.json`, `Data/RegionFiles.cs`, `Models/LevelCaps.cs`, tests | none (one new) |
| **W4b-2, stages** | The forward stage rule in `Trainer.Relevelled`, by tier (`DifficultyRules.TierOf`) for species of ours; Challenge and Easy by share in a crossed region; Masters at the higher of 75 and the chosen level. | `Models/Trainer.cs`, `Models/DifficultyRules.cs`, `Core/GameEngine.Battles.cs`, tests | `levels-crossed.txt` |
| **W4b-3, a crossed region's EXP** | `CoreSetup.CrossedExp` (×1.5, kept in a `BattleRecord` and its file, plan 08 · P10's rule) applied in `BattleCore.Give`; the ceiling through `GameEngine.CurrentLevelCap` (+10%, Normal runs, crossed regions) reaching the battle, the bag and the Day Care (`LevelCaps.Allowed`, which the Day Care doesn't ask yet); a line under the EXP bar where a Pokémon is held (`ui.json` with its Romanian, the style guide first); `docs/mechanics/rulings.md`. | `Battle/Sim/BattleCore.cs`, `Battle/BattleSetup.cs`, `Battle/Sim/BattleRecordFile.cs`, `Core/GameEngine.Battles.cs`, `Models/DayCare.cs`, `UI/ModernUi.Party.cs`, tests | none (the corpus fights no crossed region) |
| later, with each region's plan | Hoenn's rematch teams of ours at post-game levels; plan 23's pairs and caps as each region is imported. | `Data/our-trainers.json`, region files | `levels-crossed.txt` |

Design A is W4b-1 and W4b-2 alone. Design B is W4b-1 with its own pairs, then a session each to move D14's and R31's levels (with the user's approval of every moved level).

## 6. Questions

1. **Which design?** *Recommended: C* (the climb kept, ×1.5 EXP and a ceiling of +10% in a crossed region). A is cheaper and leaves the spread; B lowers every Champion and moves D14 and R31.
2. **The post-game's height.** *Recommended:* `min(1.15 × Champion, next Champion − 2, 260)`: Kanto 120, Johto 156, Hoenn 183, Unova 203. The original games put theirs 19% to 76% over their Champions (FireRed 75/63, HeartGold's Red 88/50); higher post-games mean higher arrivals in the next region.
3. **Do trainers' Pokémon evolve into species of ours?** *Recommended:* every trainer's Pokémon takes the games' own stages, and only ordinary trainers (tier Everyone) go on into species of ours; Leaders, the Elite Four, Champions, rivals and bosses keep their games' last stage, so their teams are still theirs. (Otherwise Hoenn's Sidney fights with Hyaenarch, Saguarox and Crayronin.)
4. **Do wild Pokémon evolve with their level?** *Recommended: no*, so every regional Pokédex can be completed; a catch evolves as it levels.
5. **The ceiling and the bonus: in every run, or Normal only?** *Recommended:* both in every difficulty, the bonus stacking with Easy's three halves; under a run with V3's cap the run's (lower) cap wins.
6. **Challenge by share past 100?** *Recommended: yes*, +6.5% for Chiefs, +5% for Bosses, +2% for everyone, in a crossed region only (Sinnoh keeps +4, +3, +1).
7. **The later regions and the cap.** *Recommended:* keep the study's Champions and the post-game rule (Paldea's post-game 2 levels long), and settle it in plan 23; the alternatives are lower Champions from Galar on, or a cap above 260.
8. **A later region's starter and gifts.** *Recommended:* given at the mapped level and evolving over their first level-ups, as today; the alternative is to give the stage the level allows.
9. **Coming back to a region by a harbour.** *Recommended:* its levels stay as they were met (as Gold and Silver's Kanto), but its League's rematch and its Vs. Seeker or phone rematches take the levels of the furthest region whose Hall of Fame is entered, mapped by that region's post-game pair.
10. **A game begun outside Sinnoh** (`--region`, a development switch). *Recommended: leave it*: every game the title starts begins in Sinnoh.

## 7. Status

Approved by the user on 2026-10-26 (design C; [APPROVAL-levelling-and-our-species.md](APPROVAL-levelling-and-our-species.md)).

- [x] **W4b-1, the ladder's data** (wave 64). Every number checked again by a script against the data at `48df8ff0`.
  - Each later region file's `levels` has its post-game pair, the region's strongest trainer met at `min(1.15 × Champion, next Champion − 2, 260)` (rounded to the nearest): Kanto `[75, 120]` (the Champion's rematch; 119.6), Johto `[88, 156]` (Red; 156.4), Hoenn `[78, 183]` (Steven; 186.3 against Unova's 185 − 2), Unova `[80, 203]` (Benga and the last Boss Trainers; 212.75 against Kalos's 205 − 2, the study's Champion until plan 23).
  - Every region file has `caps` (`RegionFile.Caps`), its own game's levels by Badges held: Sinnoh's table moved out of `LevelCaps` into `sinnoh.json` unchanged; Kanto `[14,21,24,29,43,43,47,50,63]`, Johto `[13,17,19,25,31,34,35,41,50]`, Hoenn `[15,19,24,29,31,33,42,46,58]`, Unova `[13,18,24,30,33,39,48,51,59]`. *Decided*: a table is the Leaders' best **in rising order**, then the Champion's, since Johto's Chuck (31), Jasmine (35) and Pryce (34) need not come in one order; the section 4 numbers were already so.
  - `LevelCaps.TableOf(region, crossed)` reads the file and maps it by `levels` in a crossed region (Kanto 70 75 77 81 90 90 93 95 104, Johto 111 114 115 119 123 125 126 130 136, Hoenn 142 144 146 149 149 150 155 156 162, Unova 166 168 171 173 175 177 181 182 185, section 3's table); `LevelCaps.For(..., crossed)` and `GameEngine.CurrentLevelCap` pass it, so V3's capped runs are capped in every built region now. W4b-3's ceiling is these tables with its margin.
  - `levels-crossed.txt` (`GoldenLevelTests.TheLevelsOfEveryCrossedPlaceAreWhatTheyWere`): a block per crossed region (pairs, caps met and own, its trainers that stand on no map), then every place of those regions, each level as met with its own in brackets. `levels.txt` does not move.
  - `LevelLadderTests`: the pairs rise on both sides, each first route at or above the Champion before it less 5, each post-game pair its rule and the file's strongest trainer and under the next Champion, each cap table its trainers' files', the mapped tables, a capped run held at the mapped cap.
- [x] **W4b-2, stages, with D15-A** (wave 64). `Models/OursGrowth.cs`; `docs/mechanics/rulings.md`, "Stages as they are met".
  - `Trainer.Relevelled(levelOf, grow: true)` (in `StartTrainerBattle`, foes and partner, only in a crossed region): the games' stages by section 4's piece 1 (`GamesStageAt`; a way the Pokémon decides itself, gender, stats or personality, only where it holds), then ours (`OursAt`, every evolution with its condition taken as met, never into a legendary set). *Decided* (the user's answer): no tier exception, the named trainers grow too. Not the first battle, a Master's, the Frontier's or the Subway's (whose `Relevelled` doesn't grow).
  - Wild: `OursGrowth.GrowWild`, ours only, one in four by `Personality % 4` (no new draw), in `StartWildBattle` for the first, a horde's others and a partner's second; the games' stage stays the table's (L4).
  - Challenge and Easy by share in a crossed region (`DifficultyRules.PerMilleOf`, `LevelsOf(tier, level, crossed)`, `Prepare(..., crossed)`; never under Sinnoh's levels), the rematch's one more grown too. Masters: `DifficultyRules.FollowThePlayer`, at the higher of 75 and the chosen Pokémon's level, no longer mapped by a crossed region.
  - The lines: `tools/dev/grown-names.mjs` (node) lists every crossed region's trainers whose Pokémon change and every line of theirs that names a forebear, and with `--write` rewrites 166 of them in English and Romanian (`tools/dev/grown-names.json` keeps 14 that mean another Pokémon and writes 4 by hand); `GrownNamesTests` holds them to the engine. *Decided*: no placeholder of the text box carries a speaker's team, so the lines are written for the game every title starts (begun in Sinnoh).
  - `levels-crossed.txt` gains each trainer's changed species in square brackets; `OursGrowthTests`, `HeadlessGameTests.ATrainerOfARegionCrossedIntoIsMetAtTheStageItsLevelAllows`.
- [ ] W4b-3, a crossed region's EXP.
