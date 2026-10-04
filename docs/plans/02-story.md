# Plan 02 · The story, start to finish

**Goal**: Platinum's whole story, from the TV in Lucas's bedroom to the Hall of Fame and the post-game, paced the way the original paces it: HM obstacles that open the map as you earn badges (trees to Cut, rocks to Smash, boulders to push, water to Surf, fog to Defog, walls to climb, waterfalls), story blockers (Team Galactic grunts, the Psyduck on Route 210, the locked Valley Windworks), key items, the rival, Team Galactic's plot and the legendaries.

## Where we are

- `Core/GameEngine.cs` switches between overworld, dialogue, battle and menu states. NPCs (`Overworld/NPC.cs`) have fixed dialogue lines and can be trainers (line of sight, "!" and a battle), a nurse, a clerk, a PC or the starter briefcase.
- A fresh game already has a Turtwig, and the Running Shoes are always on.
- There are no story flags, scripted events, cutscenes, key items, field moves or rival; badges are only a bitmask in the save; `NPC.HasBattled` is not saved.
- Battles (`Battle/BattleEngine.cs`): single and double battles with physical/special damage, simple status and stat moves, catching, EXP, level-ups and evolution, switching, a small bag. Presentation (`BattleAnimator`, `BattleRenderer`) is driven by `QueueMessage(text, onComplete, onShow)`: `onShow` starts the animation a message describes. Abilities, held items and Gen 4 status rules are partly in (see plan 06 · R7–R9); no weather or tag battles.

## Design

### Story state
`StoryState`, saved with the game: flags (bool), variables (int), defeated trainers, picked-up items and hidden items, badges, key items, the rival's starter, and per-map object visibility (an NPC shows only while its flag allows, like the decomp's `hidden_flag`). Name flags after the decomp's (`FLAG_...`, `VAR_...`) so its scripts stay easy to follow.

### Event scripts
- A small script language in data files, one per map, mirroring the decomp's `res/field/scripts/scripts_<map>.s`. Commands: message, yes/no and menu choices, give item or Pokémon, trainer battle, wild battle (can't flee, for legendaries), move an NPC or the player along a path, face, emote bubble, wait, fade, warp, camera pan and shake, music and sound, set flag or variable, conditions (flag, variable, badge, item, party), show or hide an NPC, heal the party, money, call and jump, end.
- A `ScriptRunner` executes scripts as coroutines inside the overworld state, blocking player input while they run.
- Triggers: talking to an NPC, background events (signs, hidden items), stepping on a trigger tile while a variable has a given value, entering a map, using an item or field move.
- Port each map's scripts by reading the decomp and rewriting them in our language. Write dialogue in our own words, keeping each scene's beats; don't copy the game's text.

### Field moves and obstacles
- The party menu lists each Pokémon's field moves: Cut, Rock Smash, Strength, Surf, Fly, Defog, Rock Climb, Waterfall, Flash, plus optional extras (Teleport, Dig, Sweet Scent, Milk Drink/Softboiled).
- Facing an obstacle offers the move when a party member knows it and the badge allows it ("This tree looks like it can be cut down! Would you like to use Cut?").
- Badges that allow each move outside battle in **Platinum** (Relic and Fen are swapped compared with Diamond/Pearl):

  | Badge | Leader | Field move |
  |---|---|---|
  | Coal | Roark | Rock Smash |
  | Forest | Gardenia | Cut |
  | Relic | Fantina | Defog |
  | Cobble | Maylene | Fly |
  | Fen | Crasher Wake | Surf |
  | Mine | Byron | Strength |
  | Icicle | Candice | Rock Climb |
  | Beacon | Volkner | Waterfall |

- Where the HMs come from in Platinum: HM01 Cut, Cynthia in Eterna City · HM02 Fly, Team Galactic's warehouse in Veilstone · HM03 Surf, Cynthia's grandmother in Celestic Town · HM04 Strength, Riley on Iron Island · HM05 Defog, deepest room of the Solaceon Ruins · HM06 Rock Smash, a Hiker in Oreburgh Gate · HM07 Waterfall, Jasmine in Sunyshore after Volkner · HM08 Rock Climb, Route 217.
- Obstacles: cuttable trees and cracked rocks (come back when the map reloads), Strength boulders (stay pushed until the map reloads; boulder puzzles in Victory Road and elsewhere), water, waterfalls, rock walls, fog (Defog), dark caves (Flash), mud slopes and Cycling Road (bike), deep snow (slow), plus story blockers that step aside once a flag is set.

### Battle features the story needs
These are built in [plan 06](06-game-mechanics.md); the story only uses them.
- Double battles (Galactic pairs, twins) and tag battles with a partner: Dawn or Lucas in Jubilife, Cheryl in Eterna Forest, Mira in Wayward Cave, Riley on Iron Island, Barry at Spear Pillar, Buck at Stark Mountain (06 · R9).
- Moves, abilities and held items with their real effects (06 · R3–R8), trainer AI that switches and uses items, scripted wild battles you can't run from (06 · R9), prize money and losing money on a whiteout, the EXP Share, the "forget a move?" prompt, cancelling evolution (06 · R10).

### Key items and systems
Running Shoes from Mom, the Pokétch and its apps (from the Pokétch Company in Jubilife), the Works Key (Valley Windworks), the Bicycle (Eterna's cycle shop), the fishing rods, the Galactic Key (Galactic HQ), the SecretPotion (clears the Psyduck on Route 210), the Vs. Seeker, the Poké Radar (post-game). Check where each one is given in Platinum against the decomp's scripts or Bulbapedia's walkthrough when you get to it.

## Pacing targets

Platinum's gym leaders and the League (first battles; check against the decomp's `res/trainers/data` when entering the teams):

| Battle | Team (level) |
|---|---|
| Roark, Oreburgh | Geodude 12, Onix 12, Cranidos 14 |
| Gardenia, Eterna | Turtwig 20, Cherrim 20, Roserade 22 |
| Fantina, Hearthome | Duskull 24, Haunter 24, Mismagius 26 |
| Maylene, Veilstone | Meditite 28, Machoke 29, Lucario 32 |
| Crasher Wake, Pastoria | Gyarados 33, Quagsire 34, Floatzel 37 |
| Byron, Canalave | Magneton 37, Steelix 38, Bastiodon 41 |
| Candice, Snowpoint | Sneasel 40, Piloswine 40, Abomasnow 42, Froslass 44 |
| Volkner, Sunyshore | Jolteon 46, Raichu 46, Luxray 48, Electivire 50 |
| Aaron (Bug) | Yanmega 49, Scizor 49, Vespiquen 50, Heracross 51, Drapion 53 |
| Bertha (Ground) | Whiscash 50, Golem 52, Rhyperior 52, Gliscor 53, Hippowdon 55 |
| Flint (Fire) | Houndoom 52, Rapidash 53, Flareon 55, Infernape 55, Magmortar 57 |
| Lucian (Psychic) | Mr. Mime 53, Bronzong 54, Espeon 55, Alakazam 56, Gallade 59 |
| Cynthia (Champion) | Spiritomb 58, Milotic 58, Roserade 58, Togekiss 60, Lucario 60, Garchomp 62 |

Wild levels and every other trainer's team come from the decomp (`res/field/encounters`, `res/trainers/data`). Barry's team depends on the player's starter (he takes the one with the type advantage).

## Sessions

### S1 · Story state and scripting
`StoryState` (saved, with a migration for old saves), the script language and its parser, `ScriptRunner`, the triggers, NPC visibility by flag, a yes/no box. Convert what exists (briefcase, nurse, clerk, PC, signs, trainers) to scripts. Test that scripts run to completion headlessly with auto-confirmed messages.

### S2 · Field moves, obstacles and key items
Party-menu field moves, the badge checks, obstacle objects (cut tree, cracked rock, boulder, fog, darkness), Surf, Waterfall and Rock Climb hooked to plan 01's movement states, the Bicycle, fishing rods, a Key Items pocket, the Pokétch (clock and party apps first). Tests for each gate: blocked without the move or badge, open with both.

*Ready from plan 01 · M3:* the movement states and their rules (`Overworld/FieldMovement.cs`): surfing with its start from the shore and its landing, waterfalls up and down, rock faces, the Bicycle's pace and where it can't go. Today each asks only that a party Pokémon knows the move (`FieldMovement.MovesOf`): the badge checks go there. Surf starts on the confirm button at the water's edge with no question asked; the party-menu way of using a move, and the yes-or-no, are this session's. The three obstacles are props that are drawn and block (`CutTree`, `CrackedRock`, `StrengthBoulder`, placed from the import); making them give way means removing the prop and rebuilding the chunk's scene. The Bicycle has rules but no item, no sprite and no key.

### S3 · Moved to plan 06
Double and tag battles, trainer AI, scripted wild battles, whiteout money, the EXP Share, the move-learning prompt and cancelling evolution are now [plan 06](06-game-mechanics.md) · R9 and R10. S5's tag battle in Jubilife needs R9 first. The number S3 stays unused so the chapters keep theirs.

### S4–S15 · Chapters
Follow Bulbapedia's Platinum walkthrough parts. For each chapter: port the scripts of its maps, place its trainers and items, set the flags that open the next area, and add a headless "story walk" test that plays the chapter's scripts with forced battle wins and checks the flags, items, badges and party at the end. Needs the chapter's areas from plan 01 and the species from plan 03.

  *Done ahead of S4 in plan 04 · G10 (2026-10-04):* the new-game introduction (`UI/IntroScreen.cs`: Rowan's welcome, the Pokémon out of its ball, boy or girl, the name, the send-off; our own lines on the original's beats) and who the player is (`Core/PlayerIdentity.cs`, saved). The character the player didn't choose is the professor's assistant, and written lines say `{player}` and `{assistant}` instead of a name (`docs/data-files.md`). Still to do in S4: the rival's name, which Platinum asks last in the introduction. Add the step to `IntroScreen` with the same `NameEntry`, keep the name in `PlayerIdentity` and the save, and give written lines a `{rival}`.
- **S4 · Lake Verity and a Pokédex** (part 1): new-game intro with Professor Rowan (gender and name), the Twinleaf TV, Barry, Cyrus at Lake Verity, the briefcase and the wild Starly on Route 201, the first battle with Barry, the Pokédex in Sandgem, Running Shoes from Mom. Remove today's free Turtwig.
- **S5 · Jubilife and Roark** (parts 2–3): Route 202 catching lesson, Jubilife (Pokétch coupons, Trainers' School, the Galactic grunts and the tag battle), Route 203 and Barry, Oreburgh Gate (Rock Smash), Oreburgh Mine, the **Coal Badge**, Ravaged Path, Floaroma Town and Meadow.
- **S6 · Windworks and Eterna** (parts 4–5): Works Key, Valley Windworks and Commander Mars, Route 205, Eterna Forest with Cheryl, Eterna City (HM01 Cut from Cynthia), the **Forest Badge**, the Galactic building and Commander Jupiter, the Bicycle, the Old Chateau.
- **S7 · Hearthome and Solaceon** (parts 5–7): Cycling Road, Wayward Cave with Mira (optional), Route 207, Mt. Coronet, Route 208, Hearthome and Amity Square, the **Relic Badge**, Route 209, the Lost Tower, Solaceon Town and Ruins (HM05 Defog), the Psyduck blocking Route 210.
- **S8 · Veilstone and Pastoria** (parts 8–10): Route 215, Veilstone, the **Cobble Badge**, the Galactic warehouse (HM02 Fly), Route 214, Maniac Tunnel, Valor Lakefront, Route 213, Pastoria and the Great Marsh, the **Fen Badge**, Route 212.
- **S9 · Celestic and the sea** (parts 11–12): the SecretPotion and the Psyduck, northern Route 210 in the fog, Celestic Town (Team Galactic, Cyrus, HM03 Surf), Fuego Ironworks, Routes 219–221 and 218 by sea, Canalave City and Barry.
- **S10 · Iron Island and the lakes** (parts 13–14): Iron Island with Riley (HM04 Strength), the **Mine Badge**, the Canalave Library meeting, the explosion at Lake Valor, Commander Saturn at Lake Valor, Commander Mars at Lake Verity, Route 211, Mt. Coronet, Routes 216–217 (HM08 Rock Climb).
- **S11 · Snowpoint and Galactic HQ** (parts 14–15): Acuity Lakefront and Commander Jupiter at Lake Acuity, Snowpoint City, the **Icicle Badge**, Galactic HQ in Veilstone (Galactic Key, Cyrus, the Master Ball, freeing Uxie, Mesprit and Azelf).
- **S12 · Spear Pillar and the Distortion World** (parts 16–17): Mt. Coronet to Spear Pillar, the tag battle with Barry against Mars and Jupiter, Dialga and Palkia, Giratina taking Cyrus, the Distortion World with Cynthia, Cyrus, Giratina (Origin Forme) as a catchable encounter that comes back later if knocked out.
- **S13 · Sunyshore** (part 18): Sendoff Spring, the lake trio back home (Uxie and Azelf waiting to be caught, Mesprit roaming), Route 222, Sunyshore City, Volkner, the **Beacon Badge**, HM07 Waterfall from Jasmine.
- **S14 · The Pokémon League** (part 19 onward): Route 223, Victory Road (it needs several field moves), the Elite Four, Cynthia, the Hall of Fame and credits, returning home afterwards.
- **S15 · Post-game**: the National Pokédex from Rowan (unlock rule in plan 03), the ship to the Battle Zone, the Fight Area, Survival and Resort Areas, Routes 224–230, Stark Mountain with Buck (Heatran), Snowpoint Temple (Regigigas and the three Regis), the roaming legendaries, Rotom in the Old Chateau, Cresselia on Fullmoon Island, gym leader rematches, the Battle Frontier (optional).

## Risks

- **Script volume**: hundreds of scripts. Port by chapter, reuse common patterns (item balls, trainers, doors, nurses) as templates, and only transpile the decomp's scripts automatically if hand-porting proves too slow.
- **Battle scope**: double battles and abilities are large pieces of work and belong to plan 06; schedule its R2–R9 before the chapters that need them (Jubilife's tag battle in S5).
- **Save compatibility**: every new saved field needs a default and a migration.

## Status

- [ ] S1 Story state and scripting
- [ ] S2 Field moves, obstacles and key items
- S3 moved to plan 06 (R9, R10)
- [ ] S4 Lake Verity and a Pokédex
- [ ] S5 Jubilife and Roark
  - 2026-10-01, ahead of S1–S3: Jubilife City is on the map (`Data/maps/JubilifeCity.json`, `TrainersSchool.json`, `PoketchCompany.json`, `JubilifePokemonCenter.json`, `JubilifePokeMart.json`), north of Route 202, with the Trainers' School (two School Kid trainers, Barry), the Pokétch Company, its own Pokémon Center and Poké Mart, Dawn at the entrance, Looker, and the three campaign clowns. Without story flags the clowns only ask their questions and no Pokétch is given yet; the TV station, Global Terminal and condominiums have no doors. Routes 203, 204 and 218 aren't built, so a sign and an NPC close each road out. The Galactic grunts and the tag battle with Dawn come after the Coal Badge in Platinum, north of the city by Route 204, so they wait for plan 06 · R9 (double battles) and that part of S5.
- [ ] S6 Windworks and Eterna
- [ ] S7 Hearthome and Solaceon
- [ ] S8 Veilstone and Pastoria
- [ ] S9 Celestic and the sea
- [ ] S10 Iron Island and the lakes
- [ ] S11 Snowpoint and Galactic HQ
- [ ] S12 Spear Pillar and the Distortion World
- [ ] S13 Sunyshore
- [ ] S14 The Pokémon League
- [ ] S15 Post-game
