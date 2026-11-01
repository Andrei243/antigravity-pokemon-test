# LS11 · The event legendaries: where each is met, and how

Plan 29 · LS11, written 2026-11-01 after the user answered plan 29's decision 1 ("yes, open them"). The plan's own text: open each event-only species "by a post-game condition of our own (a region's Hall of Fame plus a place-specific step), with a staged scene by the five-beat recipe and the unchanged `wildbattle`; one proposal per region for the user's approval first". This document is those proposals, all four regions at once so the user can answer them together; nothing here is built. The species are Darkrai, Shaymin, Arceus, Manaphy and Phione (Sinnoh), Celebi (Johto), Jirachi (Hoenn), Victini, Keldeo, Meloetta and Genesect (Unova).

**What is settled already** (plan 29 and CLAUDE.md, applied without asking again): every meeting is the recipe's five beats (hush, look, sign, voice, hand over: plan 29's "The legendary's beat" and the style guide's "Legendary scenes"), the scene ends before the battle and the camera is released before `wildbattle` (decision 2); lines are our own, one or two, measured by `tools/ContentCheck`; nothing of the games' is taken (the original's flags, variables, tiles and levels are numbers, and may be read); a script keeps its own game's levels (1 to 100) and `Pokemon.Relevel` maps them in a region the game didn't begin in (Johto 50 → 136, Hoenn 50 → about 158, Unova 65 → about 190 by the regions' `levels`); a place the story hides someone in is a flag the place's own `OnEnter` keeps.

Every map key, tile, flag and variable named below was read from `PokemonPlatinumEngine/Data/world/<region>/areas`, `overlays` and `Data/scripts` on `main` at `1bcbf580`; where a tile is a guess it says "to check with `ContentCheck --plan`".

## The table

| # | Pokémon | Region | Where (area key) | Unlocked by | Kind | Level | New pieces |
|---|---|---|---|---|---|---|---|
| 1 | Darkrai | Sinnoh | `newmoon_island_forest` (16, 13), by the Harbor Inn's dream | Sinnoh's Hall of Fame and Cresselia's quest closed (`VAR_LUNAR_WING_EVENT_STATE` 3) | battle | the original's (Platinum: 50) | the Harbor Inn's room; Member Card given |
| 2 | Shaymin | Sinnoh | `route_224`'s white rock, then `flower_paradise` (911, 203) | Hall of Fame, National Pokédex, Oak's Letter from Oak in `EternaCitySouthHouse` | battle | 30 (Platinum's) | none (the phrase picker writes the thanks) |
| 3 | Arceus | Sinnoh | the Hall of Origin, up from Spear Pillar's top step (31, 52) | Dialga, Palkia and Giratina caught; the Azure Flute from Celestic Town's elder | battle | 80 (Platinum's) | the Hall of Origin's map; a stair of light (LS10, or beams) |
| 4 | Manaphy | Sinnoh | `route_230`'s shore, a Pokémon Ranger | Hall of Fame and Mr. Backlot's book read (`FLAG_SKIP_POKEMON_MANSION_OFFICE_BOOK_SEEN_MANAPHY_CHECK`) | Egg | hatches at 1 | `LegendaryScenesTests` taught `giveegg` |
| 5 | Phione | Sinnoh | the Day Care | a Manaphy and a Ditto left there | Egg (bred) | 1 | none: `Breeding.cs` already makes Manaphy's Egg a Phione |
| 6 | Celebi | Johto | `ilex_forest`'s shrine (about 16, 54) | Johto's Hall of Fame; the GS Ball from Kurt (`azalea_kurt_house`) | battle | 50 | a GS Ball row in the items |
| 7 | Jirachi | Hoenn | `meteor_falls_stevens_cave`, the rock Steven studies, at night | Hoenn's Hall of Fame, Steven beaten there (`FLAG_DEFEATED_METEOR_FALLS_STEVEN`), Cozmo's TM given (`FLAG_RECEIVED_TM_RETURN`) | battle | 50 | none |
| 8 | Victini | Unova | the Liberty Garden, by the Liberty Pier off Castelia's Gym Street | Unova's Hall of Fame; a Liberty Pass | battle | 65 | the Liberty Garden opened from Black 2's data; the pier's ferry; a Liberty Pass row |
| 9 | Keldeo | Unova | Route 13's north end (684, 190), where Cobalion stood | Unova's Hall of Fame and all three Swords met (`FLAG_UNOVA_FOUGHT_COBALION`, `_VIRIZION`, `_TERRAKION`) | battle | 65 | none (or Pledge Grove with U10) |
| 10 | Meloetta | Unova | `musical_theater`'s lobby | Unova's Hall of Fame | battle | 65 | none |
| 11 | Genesect | Unova | the P2 Laboratory, when plan 22 · U10 opens Routes 17 and 18 | Unova's Hall of Fame and the lab come to | battle | 65 | waits for U10's place |

Ten battles and two Eggs. Arceus and Victini each want a place that isn't built; Genesect's waits for plan 22 · U10; the rest stand in places that are open today.

## Sinnoh

The four Sinnoh rows were all events of Platinum's own, and the decompilation keeps their places, flags and people: the island's Darkrai (`FLAG_HIDE_NEWMOON_ISLAND_FOREST_DARKRAI`), the two Shaymin (`FLAG_HIDE_ROUTE_224_SHAYMIN`, `FLAG_HIDE_FLOWER_PARADISE_SHAYMIN`), Professor Oak and Marley at the white rock (`FLAG_HIDE_ROUTE_224_PROF_OAK`, `_MARLEY`, set from the start by `common.ChapterTwelve`), `VAR_SHAYMIN_EVENT_STATE`, `VAR_ROUTE_224_STATE`, `FLAG_WROTE_ON_ROUTE_224_TABLET`, the Seabreak Path's swap (`world.json`, `VAR_HIDDEN_LOCATION_SEABREAK_PATH` 4136) and the Hall of Origin's trigger on Spear Pillar (31, 52, `VAR_HALL_OF_ORIGIN_STATE` 1, on all three summits). So what is ours is only **who hands over the key item, and when**: each key item (Member Card, Oak's Letter, Azure Flute) is in `items.json` already, and is given by someone the post-game already has, in place of the original's distribution. The levels are Platinum's own, read from the original's scripts.

### 1. Darkrai on Newmoon Island

- **Unlocked by**: `SinnohHallOfFame` and `VAR_LUNAR_WING_EVENT_STATE` 3 (the sailor's boy woken: Cresselia's quest closed, plan 02 · S15, part b). The next time the player comes into Sailor Eldritch's house, the boy says he dreamt again, of a dark shape on an island nobody sails to, and that the inn by the quay keeps a room for whoever has dreamt of it: he gives the **Member Card** (his father's, "for the inn"). Ours: the original's Member Card came by Wi-Fi.
- **The way there**: the Harbor Inn (`canalave_city`'s warp 5, `locked` today) opened by an overlay `openedBy` on a flag the gift sets. Its room is built on the original's plan as every other room of M11 (a map file, `CanalaveHarborInn`); its keeper asks for the card, the player sleeps in the bed (a fade and a line over black), and wakes on Newmoon Island's pier (152, 277, facing south, the overlay's own note) with the sailor away (`FLAG_HIDE_NEWMOON_ISLAND_SAILOR`, set by the inn's scene and cleared by the island's `OnEnter` once Darkrai is gone), as the original does. `WorldWalk.StoryArrivals` loses Newmoon Island's landing, its last entry.
- **The beat** (`newmoon_island_forest.txt`, new, `Darkrai`), the table's "time, space, the other world" row: hush with `music stop` (the forest's song is Turnback Cave's already); look, `camera pan 16 13 1.2` and `camera zoom 1.4 0.8`; sign, `effect rift 16 13 hold`, `ripple`, `slowmo 0.5 1.5`, and `flicker darkrai 4` (it is there and not there, as a dream is); voice, `cry "Darkrai"` with `camera shake 0.5`, one line ("A shadow stands at the pool's edge. Something in it is looking straight at {player}."); hand over, `effect stop`, release, `endscene`, `wildbattle "Darkrai" 50` (the original's level, read again from `scripts_newmoon_island_forest.s` when built).
- **Afterwards**: caught or beaten, Darkrai is gone for good (`setflag FLAG_HIDE_NEWMOON_ISLAND_FOREST_DARKRAI`); run from, it is back the next time the wood is come to. The sailor is back on the pier. A line over black in the inn ("{player} woke up. The bed was still warm... was it a dream?") only if the player sails back by the inn: ours, optional.
- **Risks**: a room to build (M11's recipe, `RebuiltRoomTests` not concerned, `InteriorTests`-style door test). Under a Nuzlocke with the gifts clause, Newmoon Island's wood has its own wild Pokémon, so a Pokémon met there first spends the place (as every static legendary does today: `Nuzlocke.MeetStatic`).

### 2. Shaymin at the white rock and in Flower Paradise

- **Unlocked by**: `SinnohHallOfFame` and the National Pokédex (the original's own two), and **Oak's Letter**, given by Professor Oak at his table in Eterna City (`EternaCitySouthHouse`, where he stands after the Hall of Fame: post-game part a) once the player has spoken to him there once: he wants to see Route 224's rock for himself and asks the player to meet him. Giving it sets `VAR_SHAYMIN_EVENT_STATE` and clears `FLAG_HIDE_ROUTE_224_PROF_OAK` and `_MARLEY` (the original's arrival rule, `Route224_OnTransition`, now read off the letter in the bag).
- **The white rock** (`route_224.txt` `WhiteRock`, kept as it is for anyone without the letter): Oak (909, 492) asks the player to write a word of thanks on the rock; **the thanks are `phrase TABLET`** (plan 08 · P9's picker, two words, kept by key), so no name keyboard is needed. Writing sets `FLAG_WROTE_ON_ROUTE_224_TABLET` and `VAR_HIDDEN_LOCATION_SEABREAK_PATH` 4136, and the Seabreak Path's chunks swap in behind a fade (`RefreshPresence`), as `world.json` already says. Shaymin (911, 480, `FLAG_HIDE_ROUTE_224_SHAYMIN` cleared) is the scene's guest: the beat by the "grass and woods" row (sparkles, `camera follow shaymin` as it runs north up the new path, then `hide`), no battle here, as in Platinum.
- **The battle** (`flower_paradise.txt` `Shaymin`): `FLAG_HIDE_FLOWER_PARADISE_SHAYMIN` cleared by the rock's scene; the beat: hush, `camera pan 911 203 1.2`, zoom, `effect sparkles` on its tile and the way to the player, `camera follow shaymin` (it turns among the flowers), `cry "Shaymin"`, one line; release; `wildbattle "Shaymin" 30`. Gone for good once caught or beaten.
- **The Gracidea**: the flower shop's girl in Floaroma Town gives it once a Shaymin is on the team (`if has "Shaymin"`), as Platinum's does, which closes `SinnohSpeciesTests`' row `["Shaymin-Sky"]`.
- **Risks**: Marley's coordinate event (`VAR_ROUTE_224_STATE` 1) is the original's and is kept; `StoryTests.AFlagOrAVariableAskedAboutIsSetSomewhere` then finds everything set. The Seabreak Path swap is built and tested (M10, part b) but never yet laid by a script in play: `SceneSkipTests` plays it both ways.

### 3. Arceus in the Hall of Origin

- **Unlocked by**: `FLAG_CAUGHT_DIALGA`, `FLAG_CAUGHT_PALKIA` and `FLAG_CAUGHT_GIRATINA`, all three (the three Arceus made, in the elder's own story), then **the Azure Flute** from Celestic Town's elder (`CelesticTownNorthHouse`, after her book: she reads one more page). Giving it sets `VAR_HALL_OF_ORIGIN_STATE` 1, which arms the trigger already standing at (31, 52) of `spear_pillar`, `spear_pillar_dialga` and `spear_pillar_palkia` (the three summit areas each have it).
- **The way there**: the trigger asks whether to play the flute; yes is the beat's hush and look (bars, `camera path` up the steps to the altar), then **a stair of light** rising from the altar to the sky: either a new held scene effect (LS10: `effect stair`, its look and sound by plan 05, style guide first) or, without LS10, a column of `effect beam` on each tile up the steps lit 0.3 s apart. The stair is a warp to the **Hall of Origin**, a map of the imported world (the original's own matrix, read by `tools/MapImporter`, a `world.json` entry and an overlay; `MapSetting` as the summit's); its trigger at the head of the stair plays the battle's beat.
- **The beat** (`hall_of_origin.txt`, new): hush; look (`camera pan` up the hall to its dais, zoom); sign, the "time, space" row without the rift (Arceus is no tear): `effect burst` on the dais, `ripple`, `slowmo 0.5 1.5`, `camera shake 0.8`; voice, cry and one line; hand over, `wildbattle "Arceus" 80`. Caught: gone for good (a flag of ours); beaten or run from: back with the next Hall of Fame (`Victory`, as Dialga's rift).
- **Risks**: a new map from the importer (the Hall of Origin is a matrix of its own in the decompilation; layouts and numbers only, as every map). The plates are already in the data; Arceus's type follows its held plate (`CheckShapes`), nothing to add. If the user prefers no new map, Arceus comes down onto Spear Pillar's altar itself (question 3).

### 4. Manaphy, an Egg from a Pokémon Ranger

- **Unlocked by**: `SinnohHallOfFame` and Mr. Backlot's book read in the Pokémon Mansion (`FLAG_SKIP_POKEMON_MANSION_OFFICE_BOOK_SEEN_MANAPHY_CHECK`, set by `PokemonMansionOffice.txt` today, which already puts Manaphy in the Pokédex as seen).
- **Where**: Route 230 (`route_230`), on the shore by the Resort Area's way down to the water: a **Pokémon Ranger** of ours (an overlay person behind a flag of ours, `FLAG_HIDE_ROUTE_230_RANGER`) who found an Egg drifting in the sea and wants a trainer who has read of the Prince of the Sea to raise it. Ours: Platinum's Manaphy came from another game's Egg.
- **The beat**: not a battle. Hush; look at the Egg in her hands (`camera follow` the ranger, zoom); sign, the "sea" row's `sparkles` on the water before her and a `ripple`; voice, no cry (an Egg), one line ("The Egg is cool to the touch, and something inside it seems to sway with the waves."); hand over, release, `endscene`, then `giveegg "Manaphy"`, the ranger's thanks and her leaving (`hide` and `setflag`).
- **Risks**: `LegendaryScenesTests` reads only `wildbattle`, so this one meeting is out of its sight unless the test also reads `giveegg` of a mythical species (question 4); `giveegg` is not among the randomiser's points of use in CLAUDE.md, so the session must have it ask (`Randomizer.For(run)`) or say why not in `ScriptLibrary.SpeciesAsked`; under a Nuzlocke the gifts clause (`Nuzlocke.Gift`) spends Route 230 as a place.

### 5. Phione, bred

Nothing to build: `Breeding.cs` already makes the Egg of a Manaphy (with a Ditto, Route 218's) a Phione, as Platinum does. `SinnohSpeciesTests` finds it through `SpeciesSources`' Eggs to a fixed point once Manaphy has a way. No scene (the Day Care's own `hatch`), so no row of `LegendaryScenesTests`.

**Sinnoh's tests**: `SinnohSpeciesTests.OnlyInPalPark` loses Darkrai, Shaymin, Arceus, Manaphy and Phione (the plan 03 · D12c list); with them the Pal Park visitors' pool shrinks by five, which moves the day's rotation (`PalParkState.VisitorSpecies`) and the `pastimes park` shots: the session `diff`s and says so. Four rows of `special.json` for the Pokédex's area page (Newmoon Island, Flower Paradise, the Hall of Origin, Route 230). `docs/mechanics/rulings.md`'s "Newmoon Island, the Harbor Inn and Darkrai are left as they are", the Hall of Origin's line and the table's "event legendaries' places" row are rewritten.

## Johto

### 6. Celebi at the Ilex Forest shrine

- **Unlocked by**: `JohtoHallOfFame`, then **the GS Ball**: Kurt (`azalea_kurt_house`) has been sent a strange ball he can't open (as Crystal's beat, in our words) and hands it to the player to "take where the forest's protector rests". The GS Ball is no item of the data yet: a key-item row in `tools/DataImporter/Overrides/items.json` (an id past the imported ones, as R20b's stones are), then the importer run.
- **Where**: the shrine of `ilex_forest`, whose sign is at (16, 55) ("Ilex Forest Shrine", overlay `D36R0101_012+1`); the shrine's own tile is above it, about (16, 54), to check with `ContentCheck --plan`. A tile script there (`readAt`) asks to place the GS Ball when it is in the bag.
- **The beat**, the "grass and woods" row: hush (the forest's song stopped); look, the camera on the shrine and a zoom; sign, `effect sparkles` on the shrine's tile, the ball taken, a `burst` as Celebi is `show`n before the shrine (`flicker celebi 6`, coming through time), `camera follow celebi` as it circles once; voice, `cry "Celebi"`, one line; hand over, `wildbattle "Celebi" 50`. Caught or beaten, gone for good (a flag of ours); run from, the GS Ball stays on the shrine and Celebi comes again on the next reading.
- **Level**: 50, HeartGold's post-game band (the Kanto birds, Lugia's and Ho-Oh's 45 and 70 round it); 136 in a game begun in Sinnoh.
- **Risks**: Ilex Forest has wild Pokémon, so under a Nuzlocke with the gifts clause a player who met anything there first can't catch Celebi (the same as every static legendary). The Johto overlay notes "the old man of the shrine and Cameron are later chapters'": the old man (`FLAG_HIDE_ILEX_FOREST_OLD_MAN`) is no part of this and stays. No `special.json` for Johto exists; question 5. HeartGold's time-travel scene with Giovanni is not proposed.

## Hoenn

### 7. Jirachi in Steven's cave

- **Unlocked by**: `HoennHallOfFame`, Steven beaten in his cave (`FLAG_DEFEATED_METEOR_FALLS_STEVEN`) and Cozmo's TM given (`FLAG_RECEIVED_TM_RETURN`, so he is home). Cozmo, spoken to afterwards, tells of a comet that passes once in a thousand years and of a stone in Meteor Falls that "hums at night" (a line of ours, `fallarbor_town_cozmos_house.txt`); Steven, spoken to again, says he has been listening to it.
- **Where**: `meteor_falls_stevens_cave`, the rock Steven stands looking at (he is at 19, 3 facing north; the rock is the tile before him, 19, 2, to check). Read at night (`if time night`): the rock answers.
- **The beat**, the "mind and dreams" row: hush; look, the camera on the rock, zoom (Steven's cave is a cave, so the pan and zoom both play); sign, `sparkles` on the rock and the way to the player, `ripple`, Jirachi `show`n with `flicker jirachi 6` as it wakes; voice, `cry "Jirachi"`, one line; hand over, `wildbattle "Jirachi" 50`. Steven, standing by, says a line after the battle (ours).
- **Level**: 50, beside Southern Island's Lati; about 158 in a game begun elsewhere.
- **Risks**: `HoennSpeciesTests` lists Jirachi among what Hoenn still owes ("no island of decision 5 gives"): the session takes it off. Steven stands in the cave for good, so the look must not be from behind his head (the field's camera: something one tile north of a person is hidden by them); the player is put beside him first.

## Unova

Unova's event Pokémon all had places in Black and White or Black 2 that this game hasn't opened: the Liberty Garden (Victini), Pledge Grove in the Moor of Icirrus (Keldeo), the P2 Laboratory (Genesect). Meloetta had none. Plan 22 · U10 owes the Moor and the P2 Laboratory; the Liberty Garden is in no plan. The proposals put three of the four where something open can hold them, and question 7 asks whether to wait for U10's places instead.

### 8. Victini in the Liberty Garden

- **Unlocked by**: `UnovaHallOfFame`, then a **Liberty Pass** (a key-item row of ours in `Overrides/items.json`, as the GS Ball) handed over by Castelia's Gym Street sailor of ours once the player has been to the World Tournament (`VAR_UNOVA_PWT` past its first tournament): a pass "for winners" (Victini is the Victory Pokémon).
- **Where**: the Liberty Garden island, Black 2's own zone, opened by `--game black2 --data` (an entry of `Data/world/unova/world.json` and an overlay; that the pinned fuddlesworth/pokebw2 commit has the zone's chunks must be checked first, `report.md`). The way is the Liberty Pier off Gym Street, closed today (`docs/mechanics/rulings.md`, Castelia's avenue): its tile becomes a ferry's sailor, as Canalave's.
- **The beat**, the "fire" row: hush; look, the camera up the lighthouse's foot (Victini hides in its basement in the original; ours, in its garden by the tower); sign, `burst` and the heat's `ripple`, `camera shake 0.6`; voice, `cry "Victini"`, one line; hand over, `wildbattle "Victini" 65`.
- **Risks**: a new area of Unova (its trainers, if the zone has any, are Black's and not Black 2's: none are proposed). If the zone isn't in the data, the fallback is the World Tournament's grounds (`pwt`): Victini drawn by a win (question 8).

### 9. Keldeo where Cobalion stood

- **Unlocked by**: `UnovaHallOfFame` and the three Swords met (`FLAG_UNOVA_FOUGHT_COBALION`, `FLAG_UNOVA_FOUGHT_VIRIZION`, `FLAG_UNOVA_FOUGHT_TERRAKION`, set by `unova_route_13.txt`, `_11.txt` and `_22.txt` whatever the battle's end).
- **Where**: Route 13's north end (`unova_route_13`, 684, 190), the tile Cobalion was met on: Keldeo has come looking for its teachers and found the road empty. A person of ours in the overlay, behind a flag of ours, shown by the route's `OnEnter` on the four flags above.
- **The beat**, the "stone and steel" row as Cobalion's (the camera's `path` in from four tiles north, then a zoom) with `sparkles` for water in place of the beam (Keldeo is Water/Fighting), `camera shake 0.4`; `cry "Keldeo"`, one line; `wildbattle "Keldeo" 65`.
- **Level**: 65, the Swords' own.
- **Risks**: under a Nuzlocke with the gifts clause, Route 13 is already spent by Cobalion (`Nuzlocke.MeetStatic`), so Keldeo can only be fought there; Pledge Grove (U10) would be a place of its own (question 7). Keldeo's Resolute Form (Secret Sword taught at Pledge Grove) is not proposed.

### 10. Meloetta in the Musical Theater

- **Unlocked by**: `UnovaHallOfFame`; the waitress's line "the owner is rehearsing a new number upstairs" (`musical_theater` overlay, person 16) becomes, after it, "someone has been singing on the dark stage at night...".
- **Where**: `musical_theater`'s lobby (Nimbasa City), at night (`if time night`), before the stage's door: a person of ours in the overlay behind a flag of ours, shown by the room's `OnEnter` at night. The musical itself stays left out (plan 22 · U6, part b).
- **The beat**, the "mind and dreams" row (Meloetta is Normal/Psychic): a room, so the zoom alone; `sparkles` on its tile, `ripple`, `flicker meloetta 6` (it sings and vanishes), the song's sound (`sound` of the kit, plan 05's to make if a new one is wanted); `cry "Meloetta"`, one line; `wildbattle "Meloetta" 65`.
- **Risks**: a wild battle in a lobby takes the room's battle stage (an indoor one); question 9 offers the Relic Castle's deepest room (`relic_castle_23`, once Volcarona is gone) instead. Relic Song as a tutor's move and the Pirouette Forme in battle (the rules have Relic Song's shape) are not part of this.

### 11. Genesect in the P2 Laboratory

- **Unlocked by**: `UnovaHallOfFame` and the lab come to: plan 22 · U10 opens Routes 17 and 18 and the P2 Laboratory ("the old roads", its list); Black 2 left the lab empty, so Genesect is ours there, in the lab's last room, behind a flag of ours.
- **The beat**, the "lightning" row (Genesect is a machine of Team Plasma's): two `burst`s 0.4 s apart with a shake each, the camera on its pod; `cry "Genesect"`, one line; `wildbattle "Genesect" 65`. A scientist of ours afterwards gives the Shock Drive (in the data already) and says where the others are kept; the other three drives are not proposed.
- **Risks**: nothing can be built until U10's place is: if the user wants Genesect now, the fallback is a room of the Plasma Frigate (`plasma_frigate_*`, built), which has its own story states to keep (question 10).

## What needs new engine pieces

- **Maps**: the Harbor Inn (a room on the original's plan, M11's recipe); the Hall of Origin (an imported Sinnoh matrix, `world.json` and an overlay); the Liberty Garden (a Black 2 zone, `--game black2 --data`); the P2 Laboratory (plan 22 · U10's, not LS11's).
- **Items**: the GS Ball and a Liberty Pass as rows of `tools/DataImporter/Overrides/items.json` (the Member Card, Oak's Letter, the Azure Flute, the Gracidea and the Shock Drive are in the data already).
- **Scene effects (LS10, optional, style guide first)**: a stair of light for the Hall of Origin (fallback: beams up the steps); petals for Shaymin, a dream's dark mist for Darkrai, notes for Meloetta (fallbacks: sparkles, rift with ripple, sparkles). None is needed to build LS11 with the fallbacks.
- **Script commands**: none. `phrase`, `giveegg`, `if has`, `if time`, `if nationaldex`, `flicker`, `effect … hold`, `slowmo` and the camera kit all exist.
- **Tests**: `LegendaryScenesTests` reading `giveegg` of a mythical (question 4); `SinnohSpeciesTests.OnlyInPalPark` loses five; `HoennSpeciesTests` loses Jirachi; the Shaymin-Sky source row is filled; `WorldWalk.StoryArrivals` loses Newmoon Island; `special.json` rows; `Golden` (`scripts.txt` and, through the Pal Park pool, any block that lists visitors) accepted with the blocks named.
- **Randomiser**: every `wildbattle` above is already a point of use, so under a species randomiser the battle is with the mapped species while the person on the map is the real one, as every legendary today; `giveegg` must be made one (Manaphy) or listed as asked in `ScriptLibrary.SpeciesAsked`.

## Sessions

LS11 in four parts, one a region, each after the user's answers for it: **LS11a** Sinnoh (rows 1 to 5; the Harbor Inn and the Hall of Origin), **LS11b** Johto (6), **LS11c** Hoenn (7), **LS11d** Unova (8 to 10, and 11 with U10 or with question 10's fallback). b, c and d touch different files and can run side by side with a; LS10's new effects, if wanted, come before the rows that use them. Shots: `legends` and `epilogue` (Sinnoh), `johto` (`JOHTO_FROM=partc`), `hoenn` (post-game), `unova` (`UNOVA_FROM=legends`), each beat at look, sign and voice.

## Questions for the user

Each with the recommended answer first.

1. **Levels**: (a) Sinnoh's at Platinum's own event levels (Darkrai 50, Shaymin 30, Arceus 80) and the other regions' at their post-game band (Celebi 50, Jirachi 50, Unova's four 65); (b) every one at its original event level (Celebi 30, Jirachi 5, Victini, Keldeo, Meloetta 15, Genesect 50); (c) every one at its region's post-game band, Sinnoh's included (Darkrai and Shaymin at 70).
2. **Who hands over Sinnoh's key items**: (a) the boy in Sailor Eldritch's house the Member Card, Professor Oak in Eterna City the Oak's Letter, Celestic Town's elder the Azure Flute; (b) all three from one person of ours (a "Pokémon Ranger" at the Pokémon League's Pokémon Center), with the same conditions.
3. **Arceus's place**: (a) the Hall of Origin as Platinum has it, a new map up a stair of light from Spear Pillar's top step; (b) no new map: Arceus comes down onto Spear Pillar's altar after the flute.
4. **Manaphy**: (a) an Egg from a Pokémon Ranger on Route 230, and `LegendaryScenesTests` taught to hold a mythical `giveegg` to the beat; (b) a wild battle at sea on Route 230 (Surf), level 50, with no test change.
5. **The Pokédex's area page outside Sinnoh**: (a) leave Celebi, Jirachi and Unova's four off it for now (only Sinnoh has a `special.json`), and note it in plan 29; (b) add a `special.json` for Johto, Hoenn and Unova in LS11 (and its reader for those regions if it lacks one).
6. **Arceus's condition**: (a) Dialga, Palkia and Giratina all caught; (b) all three met (caught or beaten), so a Nuzlocke or a player who knocked one out still gets the flute.
7. **Unova's places**: (a) build Victini, Keldeo and Meloetta now where this document puts them, and Genesect with U10's P2 Laboratory; (b) wait for U10 and put Keldeo in Pledge Grove and Genesect in the P2 Laboratory, building only Victini and Meloetta now.
8. **Victini if the Liberty Garden isn't in Black 2's data**: (a) on the World Tournament's grounds after a tournament won; (b) leave Victini for later.
9. **Meloetta's place**: (a) the Musical Theater's lobby, at night; (b) the Relic Castle's deepest room once Volcarona is gone.
10. **Genesect before U10**: (a) wait for U10's P2 Laboratory; (b) a room of the Plasma Frigate now.
11. **LS10's effects**: (a) build LS11 with the fallbacks (beams, sparkles, rift) and let the shots decide whether LS10 adds the stair, petals, mist and notes; (b) build LS10's four effects first.
