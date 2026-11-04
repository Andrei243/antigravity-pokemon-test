# Plan 33 · Galar, with the Isle of Armor and the Crown Tundra

Written 2026-11-04, before any session, at the user's request. It takes over the Galar parts of [plan 23](23-later-regions.md) (its Galar design and sessions Z9 to Z12, which are not reused), rebuilt on [plan 32](32-open-world-engine.md)'s open-world engine in place of the tile grid.

**Goal**: Galar from Sword and Shield as a fully 3D region, hand-built, taking inspiration from the original games. That covers Postwick to Wyndon, the Wild Area with its Pokémon seen in the field and its weather by zone and day, the Max Raid dens, the gym challenge in stadiums, the Champion Cup, the Darkest Day and the post-game. The two expansions are the Isle of Armor and the Crown Tundra, last and optional until the user asks for them. The originals' scenes keep their beats, and the words, places and art are our own.

## Where we are

- **Galar is a `Region`** in `Data/RegionDatabase.cs` with `Generation` 8 and no file of its own yet (`Data/regions/galar.json`). It is after Alola in the chain, and the link to it is plan 23's: `Transport.Plane` from a room of ours in Hau'oli City, built in plan 23 · Z8.
- **Its mechanics are built.** Dynamax and Gigantamax (plan 06 · R22) with the Max Moves, the trainer AI's use (R30), the look and the giant layer of the battle themes (plan Y). Mega Dynamax is ours (R20c), and so are the Mega Max Band and the Champions' `_megamax` teams. `CoreSetup.MegaMaxDen` is the rules' side of a den, waiting for `BattleFormat.Raid`. Galar's forms are hand-built (`PokemonModels.Regional.cs`, all 20 Galarian forms; `PokemonModels.Gigantamax.cs`). `EvolutionContext.Region` keeps Koffing's and Mime Jr.'s Galarian evolutions to Galar. Every Galar species is hand-built (`PokemonModels.Galar1.cs` to `Galar4.cs`), and so are batch 8's species of ours and its supplement of regional forms (plan 03 · D14).
- **What it needs from other plans**: plan 32's engine (all of it before GL1's areas open), plan 30's grown kit and `RotomBike` mode (its region table already names Galar `Free`, `Analog`, `RotomBike`, `Grown`), plan 10 · F5 (`FieldPokemon`), F6 to F8 (camping and curry), plan 14 · B8's `Bracket` (built), plan 15 · E3's Poké Jobs (built), plan 31's richer sound for the region's music.
- **Two decompilations of Pokémon Sword** (found by the user on 2026-10-25 and 2026-10-26; plan 23 recorded them):
  - [notyourav/pokesword](https://github.com/notyourav/pokesword) is a matching decompilation in its first commits, with no licence stated.
  - Its fork [charlieduzstuf/pokesword](https://github.com/charlieduzstuf/pokesword) has a quarter of the functions and the game's 691 Pawn event scripts decompiled, which would give a scene's order, flags and conditions, the beats and never the lines.
  - Neither holds layouts, collision or encounters, and the fork commits the retail game's own executables. With that and no licence stated, **nothing of either is pinned without the user's word** (decision 4). The region is hand-built as plan 32 · decision 1 rules.

## Design

**Hand-built, at a scale of our own.** The region is written as plan 32's data (`Data/world3d/galar/`): the places, their order and their landmarks are taken from public descriptions of the games and from memory of play, never a picture traced, a model, a texture or a line of the originals. It is built **at about half the original's distances** (decision 1): a route that takes a minute to walk in Sword takes about thirty seconds here, and the Wild Area is about 1.2 km by 2 km. A town keeps its landmarks, its streets' plan and its sense of height: Motostoke's lifts and terraces, Hammerlocke's walls over the Wild Area, Circhester's hot spring, Wyndon's tower. The user judges Postwick and Wedgehurst before the rest is built (GL1), and Motostoke before the gyms (GL4).

**The region's file** (`Data/regions/galar.json`):
- `field: open` and plan 30's `movement` record.
- Professor Magnolia, with Sonia beside her from the second chapter.
- The rival Hop.
- Grookey, Scorbunny and Sobble.
- Eight badges: Grass, Water, Fire, Fighting (Sword) or Ghost (Shield), Fairy, Rock (Sword) or Ice (Shield), Dark and Dragon.
- The `Arrival` a Champion crossing in lands on, a marker before the player's new home in Postwick.
- `levels` and `caps` as plan 18 · W4b-1 sets every later region's. The D14 Galar proposals' `[[2, 224], [65, 242]]` are kept, or moved with the file, and `SpeciesPastHundredTests.Climb` and `LevelLadderTests` say so.
- The Galar Pokédex with the two expansions' as further dexes (plan 18 · W3's ids `galar`, `isle-of-armor`, `crown-tundra`).

**Sword or Shield.** Where the versions differ (two gym leaders, version-exclusive Pokémon, the legendary of the box), the game is **one of them by the player's choice at the crossing or the new game** (decision 2), saved as `VAR_GALAR_VERSION`, as Unova chose Black 2. The tables carry both versions' rows, chosen by that variable.

**The Wild Area** is one area of the open map with zones of its own. Each zone has its tables (plan 32's encounter zones), its Pokémon seen in the field, and **rustling grass**: a patch that shakes now and then and, touched, meets a Pokémon of the zone's other table, as Sword meets half its Pokémon (decision 3). The weather is by zone and day: a `calendar.json` of Galar's in plan 01 · M7's `WorldCalendarFile` shape, a column per zone, from a table of ours (decision 4 says why not the decompilation's), read by `MapArea.WeatherOn(GameClock.Today)` (the harness fixes the day). The **strong Pokémon** Sword shows near the start are kept, with its rule that a Pokémon above a level set by the badges held can't be caught yet (a Galar row of the catch's refusals, beside `Obedience.cs`'s levels). The **Watt traders**, the Digging Duo and the Ball Guy are its people.

**Max Raid dens** are `BattleFormat.Raid` (built here, for plan 34's Tera Raids too): four places against one Dynamaxed foe, on plan 14's `PlacesOf` with the format's own count. The three allies are trainers of their own that `TrainerAi` thinks for as it does for a `CoreSetup.Partner`. The foe has its shields (a rule of Sword's: a barrier of hits after half its HP), takes two actions in a turn at its stars' level, and ends on the catch. A den's beam (a column of light, our own) says what waits, and a Wishing Piece lights one. The dens' species and stars are tables of ours by zone and version. `CoreSetup.MegaMaxDen` is the post-game den where a Mega Dynamax boss waits (R20c's decision 14).

**The gym challenge** is eight stadiums: a `BuildingKind.Stadium` in the open world, and a grid room behind its door for the lobby, the locker room and the mission. Each **mission** is the original's beat as a grid room's puzzle or a script: Milo's sheep, Nessa's switches, Kabu's catch count, Bea's spinning floor or Allister's masks, Opal's quiz, Gordie's or Melony's holes, and Raihan's double battles. The pitch is the battle itself, on `BattleArena.Stadium` with a crowd that is a painted ring (the style guide's "Arenas (G8)" first). Leaders Dynamax their last Pokémon (R22's AI use). Two leaders are the version's (decision 2).

**The Champion Cup** is plan 14 · B8's `Bracket` with Galar's entrants (Marnie, Hop, Bede and the leaders), fought in Wyndon's stadium. The Darkest Day is a scene in the open world with the sky's light changed (`ArtLook`'s rig for it), and Eternatus is a raid that can't fail. Leon ends it and `GalarHallOfFame` is set.

**What a player brings and is given.** The Dynamax Band is given by the professor only to a bag without one (plan 23's design kept), and the Rotom Bike is the Bicycle item in Galar. Its speed is plan 30's `RotomBike` mode and its water upgrade is a `Swim` grant (plan 32's `MovementGrants`) from Route 9's shop. **Camping and curry** are plan 10 · F6 to F8 in the Wild Area and on routes. The **Stone Arch** stands in the Wild Area's `EvolutionSites` (rulings.md's Runerigus line). The **Battle Tower** is Galar's own ranked ladder in Rose Tower after the Hall of Fame, built on the Frontier's rules (`FrontierRules`, a facility of its own beside the five, decision 5). Poké Jobs are plan 15 · E3's board in each Pokémon Center.

**The expansions** are optional and last (plan 23 · decision 8, kept):
- **The Isle of Armor** is a second open map: the Master Dojo with Mustard and the trial of three, Kubfu and the Towers of Two Fists with Urshifu's two styles, Max Mushrooms and Max Soup for the Gigantamax Factor, Galarian Slowpoke and its two evolutions, and the cram-o-matic.
- **The Crown Tundra** is a third: Freezington and Peony, Calyrex and its two steeds, the Regis' ruins with Regieleki and Regidrago, the Galarian birds, and the Dynamax Adventures in the Max Lair as a string of raids with rentals on `BattleFormat.Raid`.

Each expansion is reached by train from Wedgehurst's station once Galar's Hall of Fame is set.

**Music** is `Data/music/galar/`, written on plan 31's fuller sound, with its roles by `MusicDirector`: the routes, the towns, the Wild Area by day and night, the stadium's crowd and the gym battles with the Dynamax layer, the Champion Cup, the Darkest Day, and Eternatus.

## Sessions

Each session: the places built as plan 32's data and baked, their rooms as grid map files, the story as scripts on the original's beats with `StoryTests` and `OpenWalk`, a chapter's itinerary in `Itineraries` played by `PlaythroughTests`, shots in a `galar` mode of the harness (`ga*` places, `gas*` story), `profile` in the busiest scene, and `CHANGELOG.md`.

### GL1 · Galar's file, Postwick, Wedgehurst and Routes 1 and 2 (2 sessions)
- `galar.json`, `Architecture.Galar` (stone, brick and slate, its row of the style guide's "Buildings" first), Postwick and the player's house, Hop's house, the Slumbering Weald's first visit in its fog, Route 1, Wedgehurst with its station and the lab, Route 2, the professor's house and the lake, Sonia.
- The chapter: Leon's homecoming and the starters, the Weald's shadow, the Pokédex and the Dynamax Band, Hop's first battles.
- **Done when** the user judges Postwick and Wedgehurst, and a Galar game plays from the bedroom to the train in `PlaythroughTests`.

### GL2 · The Wild Area (2 sessions; after plan 32 · OW7)
- The Wild Area's southern half: its zones, tables, rustling grass, weather by zone and day, Pokémon over the player's level, the lake and the Watt traders. The Rotom Bike in Galar. Camping and curry (plan 10 · F6 to F8). The Stone Arch.
- Tests: the calendar's weather for a day is the same twice; the zone's Pokémon follow its weather; a Galarian Yamask evolves under the arch. `profile` with twelve field Pokémon in rain.
- **Done when** the Wild Area shows its Pokémon under the day's weather within 8 ms at 1080p, and a catch is made from the field.

### GL3 · Max Raid dens (1 to 2 sessions)
- `BattleFormat.Raid`, `Models/Raids.cs` (the den's table and stars by its beam, the shields, the two actions, the catch at the end, the allies), the Wishing Piece, the den's beam, `CoreSetup.MegaMaxDen`'s den, raid battles in `BattleRecord` and the Vs. Recorder's file.
- Tests: a raid fills four places, ends on the catch and replays; the shields break by the hits Sword's rule counts. Shots `ft70_raid_four`, `ft71_raid_catch`, and the beam by day and night.

### GL4 · Motostoke, the opening ceremony and the first three gyms (2 sessions)
- Motostoke on its terraces, the ceremony, Route 3, the Galar Mine, Turffield (Milo), Route 4, Route 5 and Hulbury (Nessa), Galar Mine No. 2 and Motostoke's gym (Kabu): the stadium, its lobby and missions, Team Yell, Bede.
- **Done when** the user judges Motostoke, and three badges are won in stadiums in the harness.

### GL5 · Stow-on-Side to Hammerlocke (2 sessions)
- Route 6, Stow-on-Side (Bea or Allister), Glimwood Tangle in its glow, Ballonlea (Opal), Route 7, Route 8 and Circhester (Gordie or Melony), Route 9 by the bike over water, Spikemuth (Piers), Hammerlocke (Raihan) and the Wild Area's northern half.
- **Done when** eight badges are won, and the bike crosses Route 9's water with the grant and stops at its edge without.

### GL6 · The Champion Cup, the Darkest Day and Leon (1 to 2 sessions)
- Wyndon, the Cup on `Bracket` with Galar's entrants, the Energy Plant and its tower, the Darkest Day's sky, Eternatus as a raid that can't fail, Leon, `GalarHallOfFame`, the credits and home.
- **Done when** a Galar game runs from Postwick to the Hall of Fame in `PlaythroughTests`.

### GL7 · After the Hall of Fame (1 session)
- Sordward and Shielbert, the Dynamaxing at the stadiums, the Slumbering Weald's legendary of the version, the Battle Tower in Rose Tower, Leon's rematches, the plane on to Paldea (its departure a room of ours by Wyndon's station).
- **Done when** the version's legendary is caught in the Weald and the plane lands in Paldea.

### GL8 · The Isle of Armor (optional; 2 sessions)
- The island as a second open map, the train from Wedgehurst, the Master Dojo and Mustard's three trials, Kubfu and the two towers, Urshifu's styles, Max Mushrooms and the Gigantamax Factor, Galarian Slowpoke, the cram-o-matic.

### GL9 · The Crown Tundra (optional; 2 sessions)
- The tundra as a third, Freezington, Peony, Calyrex and its steeds, the Regis' ruins, the Galarian birds, the Max Lair's Dynamax Adventures on `Raid`.

## Decisions for the user

1. **Scale.** *Recommended:* about half the original's distances, the Wild Area about 1.2 km by 2 km, so a region built by hand stays buildable and the frame holds. The alternative is the original's distances, about four times the ground to describe and to bake.
2. **Sword or Shield.** *Recommended:* the player chooses at the new game or the crossing, and the save keeps it. The alternative is one version fixed (Sword) for everyone.
3. **Rustling grass.** *Recommended:* kept, as a spawn of its own that meets the zone's other table, since it is half of how Sword meets Pokémon. The alternative is only Pokémon seen walking about (plan 32 · decision 7 with nothing added).
4. **The decompilations of Sword.** *Recommended:* neither pinned, the region hand-built from public descriptions alone, and the dens' tables, the Wild Area's weather and the trainers' teams ours. The alternative pins the fork for the event scripts' beats and conditions after the user has looked at what it commits.
5. **The Battle Tower.** *Recommended:* Galar's ranked ladder on the Frontier's rules, as a facility of its own. The alternative is to send a Champion to Sinnoh's Battle Frontier, as Hoenn's Scott does.
6. **The expansions.** *Recommended:* optional and last, built only if the user asks after GL7. The alternative builds them right after the Hall of Fame as part of the region.

## Needs and gives

- **Needs** plan 32 (all of it before GL1's areas; OW7 before GL2), plan 30 · FM3 to FM9 (the grown kit, free movement in rooms, the `RotomBike` mode), plan 10 · F5 to F8, plan 18 · W1, W3, W4b-1 and W5, plan 23 · Z8's plane from Alola (or a crossing from the region before it, whichever is built), plan 31's sound.
- **Gives** plan 34 `BattleFormat.Raid` for its Tera Raids and the plane that lands in Paldea, plan 06 · R22's dens and `MegaMaxDen` a home, plan 03 · D12 a region to meet every Galarian form in, and plan 13 · V3 a cap table for Galar.

## Status

- [ ] GL1 Galar's file, Postwick, Wedgehurst and Routes 1 and 2
- [ ] GL2 The Wild Area
- [ ] GL3 Max Raid dens
- [ ] GL4 Motostoke, the opening ceremony and the first three gyms
- [ ] GL5 Stow-on-Side to Hammerlocke
- [ ] GL6 The Champion Cup, the Darkest Day and Leon
- [ ] GL7 After the Hall of Fame
- [ ] GL8 The Isle of Armor (optional)
- [ ] GL9 The Crown Tundra (optional)
