# D15 · The species of our own in the world

Plan 03 · D15, a proposal written in wave 62 for the user to answer. It changes no data, no engine and no test. The user asked how the 400 species of our own (plan 03 · D14, Nos. 2000 and up, each evolving past level 100) could be met **in the wild of the other regions** and **in trainers' teams**. Today none of them is met anywhere: a player gets one only by evolving its forebear past its level, and no trainer has one.

Every number below was counted by three scripts (kept in the session's scratchpad, not shipped) that read only the repository: `tools/DataImporter/Overrides/species-past-hundred.json`, `species.json`, `dex-entries.json`, every region's `areas/*.json` and `trainers.json`, `our-trainers.json`, `master-trainers.json`, `PokemonPlatinumTests/Golden/levels.txt` (each of its 649 blocks given its region by the area files and `RegionDatabase`'s map lists, none left over) and the region files' `levels`, mapped with `LevelMapping.Map`'s own arithmetic (a straight line, rounded half up).

## What there is today

### The species

400 species of ours, 48 of them legendary or mythical (catch rate 3: the sets that rise to 660, the Ultra Beasts, the paradox Pokémon, the Loyal Three) and 352 that are not (catch rate 45). The 48 are left out of everything below: each is a set's end that its own condition gives (a Regigigas in the party, a ring), and putting one in the grass or in a trainer's hands would undo the set. So **352 species to place**.

| Batch | All | Legendary | To place | Evolve at (not legendary) | Legendary at |
|---|---|---|---|---|---|
| Kanto | 46 | 3 | 43 | 101 to 130 | 150 |
| Johto | 34 | 3 | 31 | 104 to 120 | 150 |
| Hoenn | 51 | 5 | 46 | 140 to 158 | 160 |
| Sinnoh | 39 | 3 | 36 | 101 to 120 | 150 |
| Unova | 52 | 3 | 49 | 165 to 183 | 190 |
| Kalos | 25 | 1 | 24 | 120 to 203 | 205 |
| Alola | 38 | 14 | 24 | 209 to 221 | 225 |
| Galar | 52 | 9 | 43 | 226 to 240 | 244 |
| Hisui | 6 | 0 | 6 | 238 to 239 | none |
| Paldea | 57 | 7 | 50 | 244 to 256 | 258 |
| **All** | **400** | **48** | **352** | | |

(Galar's 52 are batch 8's 42, Ursensei and the supplement's Galarian forms; Hisui's 6 are the supplement's Hisuian forms numbered 6000 and up. Each batch's own region is the region of its forebear.)

The five built regions' batches hold **205** to place; the five regions not built yet (plan 23) hold **147**.

### The levels the world reaches

A game begins in Sinnoh (`RegionDatabase.First`), and every other region meets it at its mapped levels (plan 18 · W4: `LevelMapping`, applied by `GameEngine.LevelHere` and `Trainer.Relevelled` only in a region the game didn't begin in). The tables keep their game's own levels; what is met is mapped.

| Region | Grass at its highest (its own → met) | Trainers at their highest in the table (own → met) | Its ours evolve at |
|---|---|---|---|
| Sinnoh | 55 (Stark Mountain), not mapped | 85, not mapped (the rival in the Survival Area; 61 on the maps, the Champion's rematch 78) | 101 to 120 |
| Kanto | 67 → 107 (Cerulean Cave B1F) | 75 → 112 (the Champion's rematch) | 101 to 130 |
| Johto | 40 → 129 (Route 47) | 88 → 161 (Red, whose mountain isn't built; Lance in the Dragon's Den 75 → 153) | 104 to 120 |
| Hoenn | 50 → 158 (Route 130) | 78 → 171 (Steven) | 140 to 158 |
| Unova | 51 → 182 (Route 23) | 80 → 193 (Benga; Iris's fourth team 79 → 193) | 165 to 183 |

So W4's climb and D14's bands were made to meet: **Johto, Hoenn and Unova are already played at the levels where their own species of ours evolve**, and Kanto's late caves just reach its lowest. **Sinnoh never does**: its world tops at 55 and its trainers at 78, and its 36 evolve at 101 to 120.

### What already stands at those levels unevolved

Because a crossed region's trainers are relevelled as they are met (`Trainer.Relevelled`, moves held to what level 100 teaches), a Sinnoh game already meets, in Johto, a Whitney whose Miltank is level 115 and stays a Miltank, though Pastorine is Miltank's evolution at 115. Counted over each region's `trainers.json`, the Pokémon whose met level is at or past an evolution of ours (the legendary sets left out):

| Region | Trainer Pokémon | Of a species with an evolution of ours | At or past its level | Of which by level alone (no condition) |
|---|---|---|---|---|
| Sinnoh | 1,878 | 561 | 0 | 0 |
| Kanto | 1,650 | 414 | 1 (Agatha's second Arbok, 68 → 107) | 1 |
| Johto | 1,776 | 557 | 498 (28%) | 144 |
| Hoenn | 1,828 | 646 | 603 (33%) | 70 |
| Unova | 1,808 | 762 | 733 (41%) | 65 |

The same holds in the grass: 29 forebears in Johto's tables, 32 in Hoenn's and 31 in Unova's are met at or past their evolution of ours (Route 47's Raticate at 125, Ratchisel's level 105; Sky Pillar's Altaria at 153, Nimbusong's 152; Route 23's Bouffalant at 182, Bouffalanx's 176), and one in Kanto's (Cerulean Cave's Electrode at 105, Dynamorb's 105).

### What the engine has to build on

- **A slot of ours in a table** (plan 03 · D12a): an overlay's `national` (`OverlayNational`: `land` for the grass's slots 8 and 9, `superRod` for the Super Rod's slot 3), laid by `EncounterSlots.Grass` and `SuperRod` once the National Pokédex is had. Used by 5 of Sinnoh's overlays; plan 08 · P23's dual slot uses slots 8 and 9 too and wins them. A slot keeps the table's level and weight. Every grass table of the five regions has twelve slots weighted 20, 20, 10, 10, 10, 10, 5, 5, 4, 4, 1, 1 (`WorldAreaFile.LandSlotWeights`), but for Johto's 12 Safari Zone tables of ten, which `Grass` leaves alone. **Slots 10 and 11, a 1 in 100 each, are used by nothing yet.**
- **A team swapped by its id** (plan 06 · R20c): `BattleCore.MegaMaxTeamOf(id, bag)` hands a battle `<id>_megamax` from `our-trainers.json` in place of a Champion's team once the player holds the band. A team of ours for the League would be found the same way.
- **The region's end**: `Region.StoryCompleteFlag` (`<Id>HallOfFame`), which every gate of a region's post-game asks.
- **What counts and what doesn't**: `PokemonDatabase.GetAll()` is the games' 1,025 (the Frontier's trainers, the masters, the battle corpus, Pal Park, the randomiser's pools), `Everyone()` and `Ours()` are asked on purpose; the Pokédex lists ours after Pecharunt and counts them toward nothing; `FrontierRules.IsBannedOfOurs` bans only the legendary 48; `Randomizer.Species` leaves a name that isn't one of the games' as it is.
- **The goldens**: `levels.txt` holds every table's own levels and asserts they are 100 or less; `trainers.txt` is each trainer's team as `PartyOf` builds it, unmapped; the battle corpus uses neither the world nor the mapping.

## The designs

Three for the wild and one for trainers beyond Design A's. They are not exclusive: the recommendation (at the end) is A with B.

### Design A · Grown as they are met (a rule, no data)

**What it is.** Where the region's mapping lifts a Pokémon to or past the level of an evolution of ours, it is met as that species of ours: what the original games do for every trainer (their Pokémon are evolved by level, whatever the evolution asks for), applied to the levels W4 made.

- **Trainers**: in `GameEngine.StartTrainerBattle`, right after `Relevelled(levels.Map)` (so after the randomiser and before the difficulty, as the copies are made), each Pokémon of each foe and of a partner whose species has an evolution of ours at or below its new level becomes it (`Pokemon.EvolveInto`), its moves as `Relevelled` left them (to level 100: R31's moves past 100 stay the player's to earn). The condition (an item held, a move known, a partner in the party, the time) is taken as met, as the games take it for trainers. Never into a legendary set. Not for a Master Trainer's battle (`terms.Alone`), the first battle, the Battle Frontier or the Battle Subway (they don't relevel).
- **The wild**: in `StartWildBattle`, after the level is drawn and mapped, a forebear at or past its level of ours is met as it **one time in four**, chosen by the Pokémon's own personality (`Personality % 4 == 0`, drawn already), so no new draw is made and nothing seeded moves. The same in the horde's others and a partner's second. Not for a roamer, a script's `wildbattle` or a gift.
- **A new helper**, GPU-free: `Models/OursGrowth.cs`, `GrownAt(species, level)`: the first evolution of ours (in `species.json`'s order) whose level `level` reaches, null for a legendary set or none. One question, asked in the two places.

**Numbers.** Over the four crossed regions it shows **203 of the 205** species of the built batches (Kanto's 43, Johto's 31, Hoenn's 46, Unova's 48 and Sinnoh's 35: its forebears live in the other regions too), 203 in trainers' teams and 73 in the wild:

| Region | Of ours in trainers' teams | In the wild | Trainer Pokémon that grow |
|---|---|---|---|
| Kanto | 1 | 1 | 1 |
| Johto | 96 | 29 | 498 of 1,776 |
| Hoenn | 75 | 32 | 603 of 1,828 |
| Unova | 184 | 31 | 733 of 1,808 |

Of the trainer Pokémon that grow, those of the Leaders, Elite Four, Champions, rivals and team bosses are Johto's 106, Hoenn's 171 and Unova's 146; the rest are the routes' (392, 432, 587).

**What it changes.** One session: the helper, its two calls, `HeadlessGameTests`-style tests (a Sinnoh game crossing to Johto meets Whitney's Pastorine at 115 and a Johto game a Miltank at 19; a Route 47 Raticate is Ratchisel when its personality says so; no legendary set, no Master, no first battle), and a shot or two in the harness's `johto` and `unova` modes. **No golden file moves** (the tables and `PartyOf` are untouched, the battle corpus never crosses), and no data is written.

**Risks.**
- It is heavy: a third of Johto's and Hoenn's trainer Pokémon and two fifths of Unova's become ours, and the Gym Leaders' signature Pokémon with them (Whitney's Miltank, Jasmine's Steelix, Wallace's Milotic). Questions 2 and 3 can make it lighter: by level alone it is 144, 70 and 65; keeping the named trainers as they are takes away 106, 171 and 146.
- It shows nothing in Sinnoh, nothing to speak of in Kanto, and nothing of Kalos to Paldea's 147 (their forebears live in regions not built).
- A game begun with `--region` (the harness's) meets its own region unmapped, so none there.
- Models: every one of the 400 is sculpted, so a battle shows each as it is; `PrepareModels` asks for them behind the fade as for anyone.

### Design B · A rare slot of their own (data in the overlays)

**What it is.** Each species of ours lives in a few late places of its batch's region, in the grass's two rare slots (10 and 11, 1 in 100 each), on the water's rarest slot (4, 1 in 100) or on the Super Rod's (4, 1 in 100), **at its own level** (its evolution level to four above, never mapped, never below its evolution), once the region's Hall of Fame is entered. They are placed where the forebear lives or by the habitat its written Pokédex entry gives (`dex-entries.json`: Grassland, Forest, Water's Edge, Sea, Cave, Mountain, Rough Terrain, Urban, Rare).

**Data.** An overlay key beside `national`:

```json
"ours": {
  "land": [ ["Ratchisel", 105, 109], ["Lancerow", 104, 108] ],
  "surf": [ ["Sabrefin", 106, 110] ],
  "superRod": [ ["Medusarch", 115, 119] ]
}
```

two at most on land (slots 10 and 11), one on the water and one on the Super Rod, each a species and its two levels.

**Engine.** `OverlayOurs` in `WorldFiles.cs` and `MapArea.OursLand`, `OursSurf`, `OursSuperRod` (as `NationalSlots`); `EncounterMoment.OursOpen` (the region's `StoryCompleteFlag`, or question 6's gate for Sinnoh); `EncounterSlots.Grass`, `Surf` and `SuperRod` lay them after everything else; a slot of ours carries its own levels and is not mapped (a flag on `WildEncounterEntry`, read by `StartWildBattle` before `LevelHere`). The Pokédex's area page shows them as a way of their own (a `HabitatWays` value read from the overlays) if question 8 says so. The content check (plan 16 · T8) gains: a species of ours that exists and is not legendary, levels from its evolution level to 260, at most two on land. A test holds that every species of ours of a built batch is placed at least once in its region and none of another batch's. `levels.txt` gains an `ours` line in each area that has one (`GoldenLevelTests`' assert of 100 widened for that line alone): that is the one golden file expected to move.

**Room.** The latest third of each region's twelve-slot grass areas (by its table's highest level), two slots each, and Sinnoh's post-game areas (grass 47 and up: the Battle Zone, Stark Mountain, Snowpoint Temple's lower floors), before the water and the rods are counted:

| Region | Areas | Slots | Species to place |
|---|---|---|---|
| Sinnoh | 19 post-game | 38 | 36 |
| Kanto | 35 (from 42 own, 90 met) | 70 | 43 |
| Johto | 17 (from 24 own, 119 met) | 34 | 31 |
| Hoenn | 26 (from 33 own, 150 met) | 52 | 46 |
| Unova | 23 (from 35 own, 175 met) | 46 | 49 |

Every region has room with the water and the Super Rod; each species can stand in two places.

**Sessions.** One for the engine, the content check and the tests; then the placing, which a script can draft from the forebears' and habitats' places for a person to look over: about **three** (Sinnoh; Kanto and Johto; Hoenn and Unova). Kalos to Paldea's 147 wait for their regions (plan 23), unless question 9 says otherwise.

**Risks.**
- Sinnoh's 36 at 101 to 124 beside the Battle Zone's 47 to 55, met by a team that has just beaten a Champion of 62 to 78: a 1 in 100 slot that hits harder than anything else in Sinnoh. Question 6's default gates Sinnoh's behind another region's Hall of Fame, when the team comes back by the harbour at 100 and more.
- Kanto's 43 at 101 to 134 in places met at 90 to 107: ours are the strongest thing there, which is the point, but a first visit may meet one at 130.
- 1 in 100 is rare: a species placed once is a long hunt. Question 5 offers 4 and 4 (slots 8 and 9, which D12a and the dual slot use in Sinnoh only).

### Design C · The deep places (a table swapped)

**What it is.** One post-game place in each region, its whole grass table (and water) swapped for a table of ours after a gate: Cerulean Cave in Kanto (three areas, 36 slots), the Giant Chasm in Unova (five areas), Sky Pillar or Meteor Falls' back rooms in Hoenn, Cliff Cave and Route 47 in Johto (Mt. Silver isn't built), Stark Mountain's rooms or Turnback Cave in Sinnoh. Every species of ours of the batch is in its place's table, at its own levels, so each is a matter of an hour's walking, not luck.

**Data.** An overlay key `oursTable`: a whole twelve-slot `land` (and five-slot `surf`) with levels, and `from`, the flag it waits for. **Engine**: the table chosen by a flag, as a season chooses one (`MapArea.TablesFor`); the levels unmapped as in B.

**Sessions.** One for the engine, one or two for the tables. **Risks**: the original's post-game tables are gone once the gate is passed (Cerulean Cave's Kadabra and Ditto, Mewtwo's cave's own Pokémon) unless a flag toggles back; a place full of ours reads as a different game; Johto's natural place isn't built.

### Design T · Teams beyond the League (data, for trainers)

**What it is.** A third team for each region's Elite Four and Champion, of species of ours at levels past their evolution, fought once the region's Hall of Fame is entered **and** the player's team has been past the batch's band (question 10). Sinnoh's five (Aaron, Bertha, Flint, Lucian, Cynthia), Kanto's five, Johto's five, Hoenn's six (the four, Wallace and Steven) and Unova's five: **26 teams, 144 Pokémon** (counted from their rematch teams: Sinnoh's `_rematch`, Kanto's `_2` and the Champion's rematch, Johto's `_2`, Hoenn's `_rematch` of `our-trainers.json` and Steven, Unova's `_4`), each team the rematch team with every member that has an evolution of ours grown into it and the levels raised to the band's top, the rest kept: written by a script, looked over by hand.

**Data.** `our-trainers.json` rows `<id>_beyond`; `BattleCore.BeyondTeamOf(id, story)` beside `MegaMaxTeamOf`, asked where the rematch team is asked. **Sessions**: one. **Risks**: it is the one design that touches Sinnoh's trainers at all, and the one that asks a second rule for when the team is ready.

Master Trainers of ours (plan 14 · B14's `master_<species>`, made from Showdown's sets, which ours lack) and the Battle Frontier's sets for ours are left out: both want sets written by a rule of our own, which is a proposal of its own.

## What none of them changes

- The Pokédex's counts, goals and diplomas (ours count toward nothing, D14's rule); a species of ours met is marked seen as any.
- The randomiser: ours stay themselves (`Randomizer.Species`); under A a randomised forebear can still grow into its own species of ours.
- The legendary 48: never wild, never in a team.
- The player's own Pokémon and the trade evolutions: nothing evolves but by its own condition.
- Nothing of Platinum's: no Sinnoh table changes under A, and B and C change Sinnoh only after its Hall of Fame.

## Recommendation

**A and B together, T afterwards if wanted.** A costs one session and no data, shows 203 of the 205 built batches' species where W4's levels already stand, and moves no golden file; B gives every region, Sinnoh most of all, a place of its own for its own species, which A can't, at a cost of about four sessions. C is the alternative to B for a player who should catch them all without luck; T, a session, for a League worth coming back to.

| Design | Sessions | Data | Engine | Golden files | Shows (of 352) |
|---|---|---|---|---|---|
| A | 1 | none | `OursGrowth`, two calls | none | 203, in four crossed regions |
| B | 1 + about 3 | overlays' `ours` | an overlay key, three slot rules, a level flag, a check | `levels.txt` (an `ours` line) | 205, each in its own region (147 later) |
| C | 1 + 1 or 2 | overlays' `oursTable` | a table by a flag | `levels.txt` | 205, five places |
| T | 1 | 26 teams | `BeyondTeamOf` | none | up to 144 Pokémon in the Leagues |

## Questions for the user

1. **Which designs?** *Default*: **A and B**, T later. *Alternatives*: A alone; B alone; C in place of B; T too now.
2. **A's trainers: which evolutions count?** *Default*: **every one, the condition taken as met** (Johto 498, Hoenn 603, Unova 733 Pokémon). *Alternative*: **by level alone** (144, 70, 65), so an item, a move or a partner keeps the forebear.
3. **A's named trainers** (the Gym Leaders, Elite Four, Champions, rivals and team bosses). *Default*: **they grow like everyone** (Johto 106, Hoenn 171, Unova 146 of theirs). *Alternative*: **they keep their teams** as their games gave them.
4. **A's wild share.** *Default*: **one in four**, by the Pokémon's personality (no new draw). *Alternatives*: every one past its level; one in ten; none (A for trainers only).
5. **B's rarity.** *Default*: **slots 10 and 11 on land (1 in 100 each), the water's slot 4 and the Super Rod's slot 4 (1 in 100 each)**. *Alternative*: slots 8 and 9 (4 in 100 each) outside Sinnoh, where nothing else uses them.
6. **B's gate.** *Default*: **the region's own Hall of Fame**, and for Sinnoh **Kanto's Hall of Fame** (the team comes back by the harbour at 100 and more). *Alternatives*: Sinnoh's own Hall of Fame for Sinnoh too; the National Pokédex as well, as D12a's `national` asks.
7. **B's levels.** *Default*: **the evolution level to four above**, never mapped. *Alternative*: the slot's own level mapped, raised to the evolution level where it is below (a gentler Kanto).
8. **The Pokédex's area page.** *Default*: **shows where B places them**, as a way of their own. *Alternative*: not shown, as D12a's `national` slots aren't.
9. **Kalos to Paldea's 147.** *Default*: **they wait for their regions** (plan 23), each region's plan placing its batch by B's rule. *Alternative*: placed now, by habitat, in the five built regions' post-game (at 120 to 256, far above anything there).
10. **T, if chosen: when is the team ready?** *Default*: **the region's Hall of Fame and a Pokémon of the team at or past the batch's lowest evolution level**. *Alternative*: the region's Hall of Fame alone; every built region's.

## Status

Approved by the user on 2026-10-26 (`APPROVAL-levelling-and-our-species.md`): A + B + T, T now; S5 to S8 at their defaults.

- [x] **D15-B, the engine** (wave 64). An overlay's `ours` (`OverlayOurs`, `OursSlot` written `["Species", min, max]`, `WorldFiles.cs`) is read into `MapArea.OursLand`, `OursSurf` and `OursSuperRod`; `Overworld/OursSlots.cs` (GPU-free) is the gate (`GateFlag`: the region's `StoryCompleteFlag`, Sinnoh's Kanto's; `Open`), what may be wild (`Placeable`: ours, not legendary or mythical, catch rate above 3; 352), a species' batch region (`RegionOf`: the forebear's generation, or the number's block of a thousand for one grown from a regional form, `Hisui` among them) and its evolution level (`EvolutionLevel`). `EncounterMoment.OursOpen` (set by `GameEngine.EncounterMomentNow`) lets `EncounterSlots.Grass` lay them in slots 10 and 11 after everything else, `Surf` in the water's slot 4 and `SuperRod` in the rod's slot 4, each slot `Unmapped` (`WildEncounterEntry.Unmapped`, carried by `WildEncounterRules.Meet` and the Poké Radar), which `StartWildBattle` (and a partner's second) passes by `LevelHere`; a radar's hard shake gives slots 10 and 11 back their table's levels, and a fishing swarm leaves the rod's slot of ours alone. The Pokédex's area page shows them (`HabitatWays.Ours`, read from the region's overlays by `Habitats.For`; `pokedex.way.ours`). The content check holds an `ours` to its rules (a species of the data, one of ours, no legendary set's end, of the region's batch, from its evolution level to 260, at most two, one and one, where the tables are); `GoldenLevelTests` writes an `ours` line in each area that has one (levels past 100 allowed there alone). Tests: `OursSlotsTests` (the gate, the slots, the unmapped level, the radar and the swarm, the JSON, the 352 and their regions, the area page, every placed species of its region's batch, and `Placed`, the regions whose whole batch must be placed, empty until the placement sessions) and `ContentTests.AnOverlaysSlotsOfOursAreHeldToTheirRules`. **No species is placed yet**, so `levels.txt` does not move.
- [ ] D15-A, growth as they are met (after W4b-1; it will touch `StartWildBattle` beside the `Unmapped` line: a slot of ours is already its own species and needs no growing).
- [x] **D15-T, the Leagues' 26 `<id>_beyond` teams** (wave 64). `our-trainers.json` holds 26 records `<id>_beyond`, 144 Pokémon, each the rematch team it replaces slot for slot (Sinnoh's `_rematch`, Kanto's `_2` and the Champion's `_rematch_squirtle`, Johto's `_2`, Hoenn's `_rematch` and `hoenn_steven`, Unova's `_4`): the same name, class, AI, items, prize money, and each member's personality, IVs and held item, its species one of ours of the League's own region's batch (no species twice in a region), chosen by the member's type theme, its forebear's own evolution where that is of the batch, and its moves what its level teaches (none written). *Decided*: Sinnoh's batch has no Fire or Dragon type, so Flint brings the batch's fighters and the sun's Sakurabloom and Cynthia a Fangliscor; Johto's has no Dragon, so Lance brings its wings and its wyrm (Ironwyrm); Kanto's Champion is one team whatever starter the rival took (`kanto_champion_beyond`). *Levels*: the rematch levels mapped by the region's `levels` (W4b-1's post-game pair; Sinnoh unmapped), lifted where the region's batch would stand above them so that the Champion's best is the batch's highest evolution and four (Sinnoh +46, so Cynthia's best is 124; Kanto +14, its Champion's 134; Johto, Hoenn and Unova none: Lance 149, Wallace 170, Steven 183, Iris 202), and never under the member's own evolution level. **The engine**: `Battle/Sim/BattleCore.Beyond.cs`: `BeyondIdOf` (the id with `_beyond` in place of what follows `_rematch` or `_first`, or of a rematch's number, or after it; none for a Mega Dynamax or a beyond id), `BeyondTeamOf(id, story, party)` asked in the script's `battle … as` after `MegaMaxTeamOf` (so it wins over a Champion's Mega Dynamax team), ready (question 10's default) once the batch's region's `StoryCompleteFlag` is set and a Pokémon of the team, no Egg, stands at or above `ReadyLevel` (the batch's lowest evolution level of a species that may be met: Sinnoh and Kanto 101, Johto 104, Hoenn 140, Unova 165); `GameEngine.StartTrainerBattle` relevels no `_beyond` team in a crossed region, since its levels are already as met. No script, class, line or ui.json key changed. Tests: `BeyondTeamsTests` (the 26 and the 144, slot for slot, each species of its region's batch and at or past its evolution, the Champions' best, every League id the scripts battle with, the gate, Aaron's room played). The harness's `league` mode ends with Aaron's battle beyond (`lg_beyond_aaron_battle`, `league beyond` alone; written, not run). No golden file moves (`trainers.txt` is Platinum's table alone).
- [ ] D15-B's placement: Sinnoh and Kanto; Johto and Hoenn; Unova. Each session writes its region's overlays' `ours` (at most two on land, one on the water, one on the Super Rod, the evolution level to four above), adds the region to `OursSlotsTests.Placed`, runs the content check and accepts `levels.txt`'s new `ours` lines.
- [x] **D15-B's placement, Sinnoh and Kanto** (wave 64): 63 overlays' `ours` (Sinnoh 24, Kanto 39), each species at its evolution level to four above. Sinnoh's 36 (behind Kanto's Hall of Fame) stand in its late places by where their forebears live and what their entries say: the ice and ghosts of Snowpoint Temple's 1F to B3F and Turnback Cave's last pillar, Stark Mountain's foot and two rooms and Victory Road's back rooms for the rock, steel and cave kinds, the Great Marsh for Carnivine's, Croagunk's, Yanmega's and Tangrowth's, the Battle Zone's routes 224 to 230 and Route 222 for the rest, the Water ones on the water (Tidestoat, Gastromire, Dambarel at Lakes Valor and Acuity) and Lumifathom on the Super Rod. Kanto's 43 stand in Cerulean Cave, the Sevii Islands' late places (Sevault Canyon, the Lost Cave, Icefall Cave, Berry Forest, Kindle Road, the meadow, Cape Brink, the Water Path and Labyrinth, Outcast Island, Tanoby), Victory Road, Mt. Ember, Seafoam Islands, the Safari Zone (Kangaskhan's, Tauros's and Pinsir's), the Power Plant, the Pokémon Mansion, Diglett's Cave and Snorlax's Routes 12 and 16; its 13 Water species on the water and the Super Rod (Lapras's Ferrisle in Icefall Cave, the fossils' on Tanoby's and the islands' rods). Every species is placed at least once, many twice; `OursSlotsTests.Placed` holds Sinnoh and Kanto; `levels.txt` gained 63 `ours` lines and nothing else.
- [x] **D15-B's placement, Johto and Hoenn** (wave 64). All 31 placeable of Johto's batch and all 46 of Hoenn's, each once, at its evolution level to four above, in 51 overlays (20 Johto, 31 Hoenn): where its forebear lives where that place is late (Mt. Silver's floors and mountainsides take most of Johto's land species; Route 47, Route 28, Route 43, the Ruins of Alph, Route 39's Pastorine by the farm, the Dark Cave's Wobbulwark), the water species on the surf or the Super Rod of their forebear's waters (Slowpoke Well's Slowmonarch, Route 41's Raysail, Routes 32, 34 and 44's rods); in Hoenn the Victory Road, Meteor Falls, Sky Pillar, Mt. Pyre's summit, Jagged Pass and the late routes, Plusle's and Minun's two together on Route 110, Zangoose's and Seviper's on Route 114, the fossils' two in the Desert Underpass, Milotic's Prismaria on Route 119's water, Clamperl's two on the floors under Routes 124 and 126, Relicanth's Aeonfin on Route 124's rod (Mirage Island's grass on Route 130 left alone, since it is seldom there). Chosen by a scratch script (forebear's tables, the dex entry's habitat, the table's top level) and twelve by hand. `OursSlotsTests.Placed` names both regions; the content check finds nothing; `levels.txt` gains 51 `ours` lines and nothing else.
- [x] **D15-B's placement, Unova** (wave 64): all 49 placeable species of Unova's batch, each once, at its evolution level to four above, in 26 overlays: 43 on land, two to an area at most, by the forebear's home or the entry's habitat (Victory Road's eight areas, Route 23, the Giant Chasm's forest and cave, Reversal Mountain, the Strange House, the Relic Castle, the Desert Resort, Lostlorn Forest, Routes 6, 7, 9, 11, 12 and 22, Village Bridge); the six of the water on the surf (Bascurage on Route 23, Cygnelle in Victory Road, Tortugarch in Undella Bay, Alomatron on Route 21) and the Super Rod (Bascurage on Route 11, Fluxflounder at Village Bridge, Jellicrown in Undella Bay, Tortugarch on Route 21). The desert's and the castle's places are early by their tables' levels but are the only homes of Darumaka's, Maractus's, Dwebble's, Sigilyph's and Cofagrigus's lines; the gate is Unova's Hall of Fame all the same. The slots of ours are laid over whichever season's table a step meets. Unova is in `OursSlotsTests.Placed`; `levels.txt` gained 26 `ours` lines.
