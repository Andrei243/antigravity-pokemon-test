# Plan 34 · Paldea, with the Teal Mask, the Indigo Disk and Mochi Mayhem

Written 2026-11-04, before any session, at the user's request. It takes over the Paldea parts of [plan 23](23-later-regions.md) (its Paldea design and sessions Z13 to Z16, which are not reused), rebuilt on [plan 32](32-open-world-engine.md)'s open-world engine. It also moves [plan 30](30-free-movement.md)'s mount (FM12 to FM14) from the tile grid onto that engine.

**Goal**: Paldea from Scarlet and Violet as one continuous open region, hand-built, taking inspiration from the original games:
- Cabo Poco and the academy in Mesagoza;
- the three paths in any order (Victory Road's eight gyms, the Path of Legends' five Titans, Starfall Street's five bases);
- Koraidon or Miraidon as the mount, its powers given by the Titans;
- Terastallization at home, the Tera Raids, picnics and Let's Go;
- Area Zero and the Way Home.

Then, optional and last, the Teal Mask's Kitakami, the Indigo Disk's Blueberry Academy and its Terarium, and the epilogue of Mochi Mayhem. The originals' scenes keep their beats, and the words, places and art are our own.

## Where we are

- **Paldea is a `Region`** with `Generation` 9 and no file yet. It comes after Galar, and Galar's GL7 builds the plane that lands here.
- **Its mechanics are built.** Terastallization (plan 06 · R23) with Tera Types, shards, Tera Blast, Stellar, Ogerpon's masks and Terapagos's forms, the AI's use (R30) and their looks (plan 25). Every Paldea species and form is hand-built (`PokemonModels.Paldea1.cs` to `Paldea5.cs`, with Koraidon's builds and Miraidon's modes), and so are batch 9's species of ours and the Paldea supplement (plan 03 · D14). The D14 Paldea proposals put the region's climb at 244 to 256, waiting for its file's `levels`.
- **The mount is designed, on the grid.** Plan 30's "Paldea's mount" section and FM12 to FM14 have it: `TravelMode.Mount`, its forms as the species' own battle models, its powers granted after the Titans, the rider seated, the dismount at a battle, a script, a crossing and indoors, and `VAR_PALDEA_MOUNT`. Its rules were written against `FieldMovement.Step` and a `ClimbableFace` tile behaviour. This plan keeps every decision of that section (13 to 17) and re-expresses the rules on plan 32's capsule (below).
- **What it needs from other plans**: plan 32's engine, plan 33 · GL3's `BattleFormat.Raid`, plan 10 · F5 to F8 (field Pokémon, the camp and the kitchen), plan 12 · Q12's AUTO choice of moves (for Let's Go), plan 30 · FM3 to FM9 and FM13's rig, plan 31's sound.

## Design

**Hand-built, at a scale of our own.** The region is written as plan 32's data (`Data/world3d/paldea/`). Its provinces, towns, landmarks and the Great Crater are taken from public descriptions of the games and from memory of play, never a traced picture, a model, a texture or a line of the originals. It is built **at about a third of the original's distances** (decision 1), the whole region within about 3 km by 3 km, with the Great Crater in its middle, the sea round it and the mountains of the north. Every town keeps its landmark and its plan: Mesagoza's stairs up to the academy, Levincia's lights, Cascarrafa's falls and bridges, Medali's market, Montenevera's snow, Alfornada's ruins. The user judges Cabo Poco and Mesagoza before the rest is built (PD1).

**The region's file** (`Data/regions/paldea.json`):
- `field: open` and plan 30's `movement` record (`Free`, `Analog`, `Mount`, `Grown`).
- Professor Sada or Turo by the version.
- The rival Nemona, beside Arven and Penny.
- Sprigatito, Fuecoco and Quaxly.
- Eight badges: Bug, Grass, Electric, Water, Normal, Ghost, Psychic and Ice.
- The `Arrival` a Champion crossing in lands on, a marker before the player's new home in Cabo Poco.
- `levels` and `caps` (plan 18 · W4b-1), which set the D14 proposals' band.
- The Paldea Pokédex with the Kitakami and Blueberry Pokédexes as further dexes (plan 18 · W3's ids).

The Tera Orb is given at the academy only to a bag without one (plan 23's design, kept).

**Scarlet or Violet** is the player's choice at the new game or the crossing (decision 2), saved as `VAR_PALDEA_VERSION`, as Galar's version is. It decides the professor, the legendary (Koraidon or Miraidon, and so the mount, plan 30 · decision 14), the version's Pokémon in the tables, and the Paradox Pokémon of Area Zero.

**The three paths in any order.** Each is a chain of scripts gated by nothing but its own flags (plan 23's design, kept), and the academy suggests an order.
- **Victory Road** has eight gyms, each a test before the leader as a grid room's puzzle or a script on the open map: Katy's olive roll, Brassius's hidden Sunflora, Iono's stream hunt, Kofu's errand, Larry's lunch order, Ryme's rap battle, Tulip's photos and Grusha's snowboard run. The leaders Terastallize their last Pokémon (R23's AI use). The leaders' levels are fixed as the original fixes them, and **the gyms after the third are scaled to the badges held** where the player chose another order (decision 3).
- **The Path of Legends** has five Titans, each a giant field Pokémon met twice (a chase across its place and a battle). Each is followed by the Herba Mystica scene with Arven and the mount's next power: dash, swim, high jump, glide and climb, in plan 30 · decision 16's order. The Titans are Klawf, Bombirdier, Orthworm, Great Tusk or Iron Treads, and Dondozo with Tatsugiri.
- **Starfall Street** has Team Star's five bases. At each the player's three lead Pokémon are sent at the gate's Pokémon by Let's Go against a clock, then a boss battle with the bus. The bases are Giacomo's (Dark), Mela's (Fire), Atticus's (Poison), Ortega's (Fairy) and Eri's (Fighting).

**The mount on the open world.** Plan 30's mount section stands, with its rules re-expressed on plan 32's capsule (`OpenWorld/MountRules.cs`, in place of plan 30's grid `Overworld/MountRules.cs`):
- The **trot and the sprint** are the capsule's speeds on the mount: 6 m/s and 11 m/s at the stick's full tilt, the sprint on the run button.
- **Swimming** lets the capsule enter a water volume on the mount once granted.
- The **high jump** is a vertical speed that clears a 2.5 m ledge.
- **Gliding** is a fall at a sink rate of 2 m/s with the stick's steering, held from any height until the ground.
- **Climbing** works on a surface the baker marks climbable. That is a cover of plan 32's (`ClimbableRock`), painted by brushes on cliffs in the region's data, in place of plan 30's `ClimbableFace` tile behaviour.
- The navigation mesh's edge flags (plan 32) carry each power, so `OpenWalk` knows which places a power opens and `PlaythroughTests` can ride the story in its order.
- **What the mount refuses** stays plan 30's: rooms, gyms' halls, scripted scenes, crossings and battles.
- **The rig** is plan 30 · FM13's.

**Let's Go** is a battle that `BattleCore` plays headless in the field (plan 23's design, kept): a new `Models/LetsGo.cs`.
- The player's lead is sent at a `FieldPokemon` with a press.
- Its moves are chosen as plan 12 · Q12's AUTO chooses them, and R9's AI is asked from outside for the wild side.
- The log is read for EXP and the faint, and never shown.
- The field shows the lead running out, a hit's flash on the target, and the target fading.
- A Let's Go battle never catches, and a Pokémon of a higher level than the lead's turns to a real battle.

The record of each is kept so that a test replays it.

**Tera Raids** are plan 33 · GL3's `BattleFormat.Raid` with a Terastallized foe:
- the foe's shield is Scarlet's (a crystal barrier at set HP shares that cuts the damage it takes);
- the allies Terastallize once their orb has charged;
- a time limit counts turns as the original's clock;
- the catch comes at the end.

The crystals are spread over the open map by a spawner of their own on `Dice` and the day, with their stars by the badges held, and the black crystals of six and seven stars after the Hall of Fame.

**Picnics** are plan 10 · F6's camp on the open map with **sandwiches** (decision 4). With the recommended answer they are a second section of plan 10 · F8's `recipes.json` on the same `Kitchen`, and their powers (Encounter, Egg, Catching, Sparkling…) change the spawner's draws in the area for half an hour of play. The picnic basket is where the Eggs of plan 06 · R15's breeding are found in Paldea (the Day Care is Sinnoh's and isn't in Paldea).

**Area Zero** is a cave-set open map under the Great Crater. It is plan 32's carved terrain at its deepest: the crater's walls with their ledges and waterfalls down to the Zero Lab, and the gliding and climbing of the mount along the way. Its Paradox Pokémon are of the version. The professor's AI is battled at its end, and Koraidon or Miraidon in its battle form is the story's last Pokémon, beside the mount (plan 30 · decision 15). `PaldeaHallOfFame` is the Pokémon League's: the interview with Rika, the Elite Four, Geeta, then Nemona's champion battle. The original has it before the Way Home, and so does this plan.

**After the Hall of Fame**: the Academy Ace Tournament (plan 14 · B8's `Bracket`, as Galar's Cup is), the eight shrines of the Treasures of Ruin with their stakes, Snacksworth's legendaries (with the Indigo Disk), Tera Raids of six and seven stars, and the rematches.

**The expansions** are optional and last (plan 23 · decision 8, kept), each reached by a school trip from the academy once Paldea's Hall of Fame is set:
- **The Teal Mask**: Kitakami as a second open map (Mossui Town, the Timeless Woods, the apple orchards, Paradise Barrens' mountain), Carmine and Kieran, the Loyal Three, and Ogerpon with its masks.
- **The Indigo Disk**: Blueberry Academy as a hub of grid rooms, and its Terarium as a third open map of four biomes under one dome, with its own Pokédex. Then the BB League's Elite Four and Kieran, double battles as its rule, Terapagos, and Area Zero's Underdepths.
- **Mochi Mayhem**: the epilogue in Kitakami with Pecharunt.

**Music** is `Data/music/paldea/`, written on plan 31's fuller sound, with its roles: the provinces by day and night, the academy, the riding theme, each path's battles, Team Star's, the Titans', Area Zero's descent, and the League.

## Sessions

Each session: the places built as plan 32's data and baked, their rooms as grid map files, the story as scripts on the original's beats with `StoryTests` and `OpenWalk`, a chapter's itinerary in `Itineraries` played by `PlaythroughTests`, shots in a `paldea` mode of the harness (`pd*` places, `pds*` story), `profile` in the busiest scene, and `CHANGELOG.md`.

### PD1 · Paldea's file, Cabo Poco, Los Platos and Mesagoza (2 sessions)
- `paldea.json`, `Architecture.Paldea` (whitewashed walls, terracotta roofs, tiled plazas; its row of the style guide's "Buildings" first), Cabo Poco with the player's house and Nemona's, the cliffside cave where the legendary is found (plan 30 · FM14's choice scene), the Poco Path, Inlet Grotto, Los Platos, the South Province's first area, Mesagoza's stairs and the academy's halls as grid rooms, the Tera Orb.
- **Done when** the user judges Cabo Poco and Mesagoza, and a Paldea game plays from the bedroom to the academy's door in `PlaythroughTests`.

### PD2 · The mount on the open world (1 to 2 sessions; after plan 32 · OW3 and OW4, in place of plan 30 · FM12)
- `OpenWorld/MountRules.cs`, `MovementGrants`' five powers, the climbable cover and its brushes, the navigation mesh's flags for each power, the dismount rules, the saved mode, `MountTests` on the open test world. Plan 30 · FM13's rig, ridden in the harness.
- **Done when** the open test world is crossed by trot, sprint, swim, high jump, glide and climb, and refused each without its grant.

### PD3 · The South and East Provinces and the first three gyms (2 sessions)
- Artazon, Levincia, Cortondo and the provinces round them; Katy, Brassius and Iono with their tests; the leaders at fixed levels and the order rule of decision 3.
- **Done when** three badges are won in two different orders.

### PD4 · The Path of Legends (2 sessions)
- The five Titans' places, chases and battles, the Herba Mystica scenes, each power granted in its order, Arven's lighthouse.
- **Done when** a Titan's grant lets the mount climb a cliff it couldn't before, and the five are beaten in the harness.

### PD5 · Starfall Street and Let's Go (1 to 2 sessions; after plan 32 · OW7)
- `Models/LetsGo.cs` and its key in the field, the five bases with their clock, the bosses and their Starmobiles as a battle on a stage of their own, Cassiopeia's calls.
- Tests: a Let's Go battle replays from its record and gives its EXP; a base's clock ends a raid on the gate. Shots `l80_lets_go`.

### PD6 · The West and North Provinces and the last five gyms (2 sessions)
- Cascarrafa, Porto Marinada, Medali, Montenevera, Alfornada, Glaseado Mountain and their provinces; Kofu, Larry, Ryme, Tulip and Grusha with their tests.
- **Done when** eight badges are won, and the region can be walked and ridden from Cabo Poco to Glaseado's top with no chunk waited for.

### PD7 · Tera Raids and picnics (1 to 2 sessions; after plan 33 · GL3 and plan 10 · F8)
- The Tera foe on `Raid` (the crystal shield, the allies' Terastallizing, the turn clock), the crystals' spawner by day and badges, the sandwiches' section of `recipes.json` and their powers on the spawner, the picnic basket's Eggs.
- Tests: a Tera raid ends on the catch and replays; a sandwich's power changes the draws of its area and wears off. Shots `ft72_tera_raid`, `27g_picnic`.

### PD8 · The League, Area Zero and the Way Home (2 sessions)
- The Pokémon League's interview, the Elite Four, Geeta and Nemona, `PaldeaHallOfFame`, the Zero Gate, Area Zero's four research stations down the crater, the Zero Lab, the professor's AI, the legendary's battle form, the credits and home.
- **Done when** a Paldea game runs from Cabo Poco to the Way Home in `PlaythroughTests`, riding where it can.

### PD9 · After the Hall of Fame (1 session)
- The Academy Ace Tournament on `Bracket`, the eight shrines and the four Treasures of Ruin, the black crystals, the rematches.

### PD10 · The Teal Mask (optional; 2 sessions)
- Kitakami as a second open map, the school trip, Carmine and Kieran, the Loyal Three, Ogerpon and its masks, its Pokédex.

### PD11 · The Indigo Disk (optional; 2 sessions)
- Blueberry Academy and the Terarium's four biomes, the BB League with its double battles, Kieran, Terapagos, Area Zero's Underdepths, Snacksworth's legendaries.

### PD12 · Mochi Mayhem (optional; 1 session)
- The epilogue in Kitakami and Pecharunt.

## Decisions for the user

1. **Scale.** *Recommended:* about a third of the original's distances, the whole region within about 3 km by 3 km. The alternative is a half, as Galar's (plan 33 · decision 1), at about twice the ground of this recommendation.
2. **Scarlet or Violet.** *Recommended:* the player chooses at the new game or the crossing, and the save keeps it. The alternative is one version fixed for everyone.
3. **The gyms' order.** *Recommended:* the original's fixed levels for the first three in the suggested order, and the later gyms scaled to the badges held where the player chose another order, so the three paths stay open. The alternative is the original's fixed levels everywhere, which makes some orders much harder.
4. **Sandwiches** (plan 23's decision 7, moved here). *Recommended:* a second section of `recipes.json` on plan 10 · F8's one `Kitchen`, amending plan 10 · decision 4. The alternative keeps curry alone and Paldea's picnic cooks it.
5. **Battles where the original fights them.** Scarlet and Violet fight on the field. *Recommended:* plan 32 · decision 3's stages, as everywhere. The alternative, battles on the open map itself, would be plan 32's second battle renderer and would start here.
6. **The expansions.** *Recommended:* optional and last, built only if the user asks after PD9. The alternative builds them right after the Hall of Fame as part of the region.

## Needs and gives

- **Needs** plan 32 (all of it before PD1's areas; OW3, OW4 and OW7 before PD2 and PD5), plan 33 · GL3's `Raid` and GL7's plane, plan 30 · FM3 to FM9 and FM13, plan 10 · F5 to F8, plan 12 · Q12, plan 14 · B8, plan 18 · W1, W3, W4b-1 and W5, plan 31's sound.
- **Gives** plan 06 · R23 its home and the Tera Raids, plan 30 its mount on the open world (FM12 and FM14 are this plan's PD2 and PD1 to PD8), plan 03 · D12 a region to meet the Paldean forms and the Paradox Pokémon in, and plan 13 · V3 a cap table for Paldea. The chain ended here, as plan 18 had it, until plan 46 (2026-11-09) put Carpathia after Paldea: Paldea's harbour gains its link.

## Status

- [x] PD1 Paldea's file, Cabo Poco, Los Platos and Mesagoza (2026-11-13, its first session: the file, the open map of five areas described and baked, six rooms, the chapter from the bedroom to the academy's door headless, the plane from Galar landing; "PD1 as built", below; its second session 2026-11-14: the harness's `paldea` mode with every owed shot, the bot's run from the bedroom to the academy's door in `PlaythroughTests`, `profile`'s Mesagoza plaza; "PD1's second session", below. **Waits for the user**: the look of Cabo Poco and Mesagoza, judged from the `paldea` mode's shots, as the done-when has it (the second session's findings, below, are the things to judge first); the bake's footprint for the whole region (the question below); `Architecture.Paldea` seen in the game. **Owed**: the academy's halls past the entrance hall, the voices of the new cast designed by ear, and whatever the user's judgment of the findings asks)
- [x] PD2 The mount on the open world (its rules built 2026-11-09 as plan 30 · FM12, its rig 2026-11-12 as FM13, and its last part 2026-11-13: the navigation mesh's hop, high jump, climb and glide links for a region whose `world.json` says `mount`, the lab's yard with a painted climbable face, the done-when ridden in `MountLabTests`, Paldea's mesh baked again with them; "PD2 as built", below. **Owed**: the playthrough bot riding the links; the user's word on the yard's shots `fwy0` to `fwy6`)
- [x] PD3 The South and East Provinces and the first three gyms (2026-11-14, its first session: the map's storage is the user's option b, the baker baking only the areas' land; Cortondo, Artazon and Levincia with the three ways between them, their Pokémon Centers and gyms, Katy, Brassius and Iono with their tests at fixed levels; the three badges won headless in two orders, the done-when; "PD3 as built", below; its second session the same day: the bot's run to three badges in `PlaythroughTests`, Brassius's and Iono's tests as searches about their towns on the open map and Katy's giant olives as cards, the towns' and the gym's songs, Paldea's Town Map with Fly's places, the new cast's voices designed; "PD3, second session", below. **Waits for the user**: the provinces' distances, decision 3 (taken at its recommendation), the songs by ear, the voices by ear, and how Fly is to be asked for in the open field)
- [x] PD4 The Path of Legends (its first session 2026-11-14: Arven at the lighthouse, the Stony Cliff and the Titan Klawf beaten twice, the Herba Mystica scene and the mount's dash, Paldea's first climbable crag; "PD4, first session", below; its second session the same day: the four other Titans in stand-in places beside the land that is built, their powers in order, the climb opening the Stony Cliff's crag, Arven's Mabosstiff and battle at the lighthouse, the five beaten by the bot headless and in the harness, the Titans' song; "PD4, second session", below. **Waits for the user**: the stand-ins (PD6 moves the Titans to their provinces), the Titans' fixed order, the song by ear. **Owed**: Arven's voice designed by ear)
- [x] PD5 Starfall Street and Let's Go (2026-11-15, one session: Let's Go on its key, the five bases in a stand-in valley north of Mesagoza with their raids' clock, the bosses and Starmobiles on a stage of their own, Cassiopeia's calls; "PD5 as built", below. **Waits for the user**: the stand-in valley (PD6 moves the bases), the raid's numbers (fifteen in five minutes), the song and the five bosses' looks by eye. **Owed**: the bot playing a raid, Let's Go's materials for the crafting kit, the new cast's voices by ear)
- [x] PD6 The West and North Provinces and the last five gyms (its first session 2026-11-15: Asado Desert, Cascarrafa, Porto Marinada, the West Province's two areas and Medali, Kofu and Larry with their tests and decision 3's scaling, the Open Sky and Quaking Earth Titans moved to their provinces; "PD6, first session", below; its second session 2026-11-23: the North Province, Glaseado Mountain and its snowslope, Montenevera, the East Province's third area, Alfornada through its cavern, Ryme's rap battle, Tulip's photographs and Grusha's run with the three leaders scaled, Lurking Steel and False Dragon moved to their provinces, the eight badges won in two orders headless, the bot to the fifth badge, through the last three and riding from Cabo Poco to Glaseado's top; "PD6, second session", below. **Waits for the user**: the north's distances and looks, the tests ours where the originals differ (Ryme's and Tulip's), the run with no clock, the songs by ear, the new cast's voices by ear, `profile`'s ride on the Windows machine. **Owed**: Starfall Street's bases still in their stand-in valley; Cascarrafa's falls drawn falling and its bridges; the open places' people's lines untranslated, as every open map's are)
- [x] PD7 Tera Raids and picnics (2026-11-23, one session: the Tera foe on Galar's `Raid` with its crystal shield, the raiders' orbs charged by their moves and a clock of turns; twelve crystals on Paldea's open map drawn by the day and the badges, their glow in their Tera Type's colour, the catch that never fails and the Tera Shards it leaves; the picnic's sandwiches as `recipes.json`'s second section on the camp's pot with no kit in Paldea, the Egg and Sparkling Powers, the Type and Encounter Powers on the open map's spawner; the picnic basket's Eggs; Mesagoza's deli; "PD7 as built", below. **Waits for the user**: the crystal's look and the raid's numbers (HP, shield, clock), the sandwiches and their powers' numbers, the picnic's look. **Owed**: the raid's clock and the orbs on the raid's screen, the bot playing a raid, the black crystals (PD9), the picnic table's own look)
- [ ] PD8 The League, Area Zero and the Way Home
- [ ] PD9 After the Hall of Fame
- [ ] PD10 The Teal Mask (optional)
- [ ] PD11 The Indigo Disk (optional)
- [ ] PD12 Mochi Mayhem (optional)

## PD3 as built (2026-11-14, its first session)

One session, on **the user's answer to the map-storage question of 2026-11-14: option b** (recorded under "PD1 as
built", below, and in the rulings' "The South and East Provinces and the first three gyms", which has every decision).
The session's worktree was deleted from outside it part-way through, by a clean-up of the full disk, and its work was
written again from the session's own record.

**What there is**
- **The baker bakes only the land the areas hold** (`world.json`'s `bakeRing`; `RegionBake.Held`): every chunk an
  area's polygon touches and the ring round it; the index lists only those, and the game streams by the index. A region
  naming no `bakeRing` bakes its whole rectangle as before.
- **The land**: `terrain/south_and_east.json` (hills 28 m over the floors, the floors cut into them, the seabed and sea
  wall off Levincia, the gates through PD1's banks on Mesagoza's avenue row), the sea off Levincia in `water.json`, the
  avenues, roads and towns' streets in `roads.json`; six areas, `south_province_area_two`, `cortondo`,
  `south_province_area_three`, `artazon`, `east_province_area_one`, `levincia`, each a place's file with its people,
  zones of wild Pokémon at levels 8 to 18, a student who battles, and in each town a Pokémon Center and a gym. The
  distances are PD1's compressed ones round Mesagoza, not decision 1's third (the rulings say how far apart).
- **Rooms**: `Cortondo_PokemonCenter`, `_Gym`, `_GymTest`, and the same of Artazon and Levincia, with three spawns.
- **The three gyms**: each test three Pokémon in its course's gaps sent on by a word (Smoliv for Katy's olive roll,
  Sunflora for Brassius's hidden Sunflora, Wattrel for Iono's stream hunt), a gym trainer, and the leader at the far end
  with the badge; leaders' looks `katy`, `brassius`, `iono`; their teams at the original's fixed levels, each
  Terastallizing its last (`teraType` of a team's member, new); objectives 807 to 809 (English and Romanian).
- **Decision 3 taken at its recommendation** (unanswered by the user): the first three keep their fixed levels in any
  order, each gated by its own badge alone; the scaling of gyms four to eight is PD6's.
- **The done-when**: `PaldeaTests.TheFirstThreeBadgesAreWonInAnyOrderAtTheLeadersOwnLevels` wins the three badges headless
  in the academy's order and in Levincia, Cortondo, Artazon.
- **Shots**: the `paldea` mode's `pd70` to `pd85` (the gates, the ways, the towns and their gyms, the nine rooms, each
  leader at the end of the test).

**Findings from the shots** (`pd70` to `pd85`, looked at; none changed here): the hills between the places stand as
sheer grey walls of layered rock round every way and town; the towns are lawn with their squares paved and their
buildings far apart; Cortondo's and Artazon's lanes stop short of the gyms' doors (the last ten metres are grass); the
tests' rooms draw no walls on the course's gap rows (the rows block, as Galar's mission rooms' do, but nothing shows
them), and the gym trainers stand in the worker's look.

**Owed** (PD3's second session): the playthrough bot's run to three badges in `PlaythroughTests`; the tests as the
originals' searches about the towns on the open map, and a giant olive's card; the towns' and the gyms' songs (the
gym song loops too soon for a room's theme); the voices of the new cast designed by ear; Fly's places on a Town Map of
Paldea; the user's word on the provinces' distances.

## PD3, second session (2026-11-14)

What PD3's first session owed, but for the user's word on the provinces' distances (left as they are) and decision 3
(still at its recommendation). The rulings' "PD3's second session" has the decisions.

- **The bot's run to three badges** (`Itineraries.PaldeaToTheThreeBadges`, `PaldeaToTheThirdBadge`;
  `PlaythroughTests.APaldeaGamePlaysFromTheBedroomInCaboPocoToTheThirdBadge`): from the bedroom on through the academy's
  welcome, then Cortondo, Artazon and Levincia in the academy's order, each test, the town's nurse and the leader, a
  strong team on hand as Galar's run has it.
- **The tests as searches**: Brassius's three Sunflora and Iono's three Wattrel stand about Artazon and Levincia on the
  open map (`species` of a place's person, new: drawn as the species' model, `GatherOpenFigures`), each gone once found;
  the two leaders battle only once all three are found. Katy's olive roll stays in her gym's room, its olives cards of a
  giant olive (`GiantOlive`, a thing of the map; style guide first). `PaldeaTests` holds both.
- **Songs**: `paldea/cortondo`, `paldea/artazon`, `paldea/levincia` (40, 38 and 33 s loops) and `paldea/gym` lengthened
  to a 30 s loop, played by the six gym rooms; `tools/MusicRender` reports no clash and each within the loudness window.
  **Open areas now play their songs** as they are crossed into (`GameEngine.ComeToTheOpenArea`), which they never had.
- **Paldea's Town Map** (`Data/world/paldea/townmap.json`, 36 by 28 cells from the bake) with Fly's five places on markers,
  the arrival flags set as the towns are crossed into, and the engine's Town Map taught an open map's region, cell and
  reachable towns; `PaldeaTests.PaldeasTownMapIsTheBakesGridAndFlyLandsOnEachTownsMarker`. **Fly still can't be asked
  for**: the open field has no start menu (`UpdateOpenField` runs none), so a session must add one or the flying taxi.
- **Voices**: briefs and designs for Katy, Brassius, Iono, Director Clavell and Nemona (`cast.json`), the catalogue run
  with `--add-missing`; no audio made. They wait for the user's ear.
- **Not done**: no harness shot of the searches, the olive cards or the Town Map was taken in this session (the
  `paldea` mode's PD3 shots stand as they were; its gym-test shots now show empty gaps in Artazon's and Levincia's rooms).

## PD2 as built (2026-11-13)

The rules (plan 30 · FM12) and the rig (FM13) were built before; this session built what PD2 still owed, all GPU-free
but the harness's shots. The rulings' "The mount on the open world" has the links' numbers. Nothing of the grid and
nothing of a walker's path changed.

- **The navigation mesh's power links** (`tools/OpenWorldBake/NavBake.cs`, `MountLinks`): `OpenNavFlags.Jump` and
  `HighJump` at the enum's end beside FM12's `Climb` and `Glide`, still a byte, so the file's format stays 1. Made only
  where `world.json` says `"mount": true` (`OpenWorldFile.Mount`, its schema and `docs/data-files.md`): a hop onto
  walked ground up to `HopHeight` over the feet, a high jump up to `HighJumpHeight`, a climb up a face climbable every
  half metre to its top, and a glide off a side to the furthest landing 6 to 20 m out that the glide reaches, each off
  the mesh, one of a kind for a pair of polygons, none where a walk or a step goes; the final sort compares the flags,
  so the bytes are the same whatever order the chunks come in. Every number is `OpenBody`'s own.
- **Who takes them**: `OpenNavMesh.Allowed(grants, mounted)`. On foot nothing of the mount's, whatever the grants; mounted
  the hop always and the rest with their grants. Every caller today (the people, the follower, the wild Pokémon, the
  bot's `Policy`, `WorldWalk`, `PaldeaTests`' walk) asks as a walker, so no path of theirs changed.
- **The lab's yard** (`tools/OpenWorldBake/worlds/lab`: three boxes of `carve/hill.json`'s additions on the west meadow,
  away from every marker, road, zone and the bot's way; the block's east face painted `ClimbableRock` in
  `terrain/ground.json`): a block 6 m high, a hop step 0.8 m and a high step 2.3 m. The lab's bake: 33 hops, 51 high
  jumps, 6 climbs, 83 glides. `--ride --nav` (the switch before `--nav`) draws the mesh as a mounted body with every power finds its paths (hops
  orange, high jumps brown, climbs magenta, glides sky blue), and the game's debug layer draws them in the same colours.
- **Paldea's mesh** baked again with `"mount": true` (17 hops, 40 high jumps, no climbs: no cliff is painted yet,
  674 glides); only `navmesh.bin` and `index.json` changed. Galar's is untouched to the byte.
- **The done-when** (`MountLabTests`, on the lab baked as the game reads it): each kind of link baked in the yard, none
  in a region not ridden; a path up the yard takes the hop mounted, the high jump and the climb only with their grants,
  never on foot; a glide off the block reaches further than any drop; the capsule rides each link from its takeoff to
  its landing (the hop, the high jump, the climb onto the block's top, the glide at least as far as its link's landing, softly: a whole glide off the block comes down past it, so a body that must land there lets go over it) and is
  refused each without its grant (a walker stays under the hop step, the high step and the face are not reached, a fall
  is short and hard); the trot (6 m/s), the sprint only with the dash (11), and the swim into the pond only with its grant.
- **Shots** (`freewalk`, `Modes/FreeWalkYard.cs`, `SHOTS_FILTER=fwy`): Koraidon half way up the painted face (`fwy0`),
  on the block's top (`fwy1`), gliding off it (`fwy2`), the high jump in the air and on the high step (`fwy3`, `fwy4`),
  the hop landing on its step (`fwy5`) and the yard from over the meadow (`fwy6`). They wait for the user's word.
- **Seen in other shots**: the lab's pictures that look west (`open`'s, `freewalk`'s) now show the yard on the west meadow.
- **Owed**: the playthrough bot riding the links (`Autoplayer.Open` steers walking paths; a mounted leg would press the
  jump at a hop's takeoff, hold into a climb's face, press once falling for a glide and press again over its landing to let go); a climbable cliff painted in
  Paldea itself (PD4's Titans and PD6's mountains).

## PD1 as built (2026-11-13, its first session)

One session, by an agent beside a full test run on the main checkout, so **no harness shot was taken**: the land was looked at with `OpenWorldBake paldea --plan` (the places' picture) and held by `PaldeaTests`' walk. The plan's six decisions are unanswered and were taken at their recommendations; the rulings' "Paldea's first chapter, on the open world" has each, and one of ours: **the version and the mount are one choice**, made in Inlet Grotto (plan 34 · decision 2 and plan 30 · decision 14 read together), so `VAR_PALDEA_VERSION` and `VAR_PALDEA_MOUNT` can't disagree.

**What there is**
- **The region's file** (`Data/regions/paldea.json`): `"field": "open"`, plan 30's movement record (`Free`, `Analog`, `Mount`) and the grown kit, the battle camera `paldea`, the modern sound, Director Clavell as the introduction's speaker (the professor is the version's, unknown when it runs), the rival Nemona, Sprigatito, Fuecoco and Quaxly, eight badges in the academy's suggested order, the `paldea` Pokédex, two spawns (home and Los Platos's Pokémon Center), `levels` `[[2, 242], [62, 258]]` (plan 03 · D14's Paldea question 1: Galar's Champion to two under the cap) and Scarlet and Violet's `caps`. `RegionDatabase`: `Start` in the bedroom, `Arrival` inside the house's door, `Harbour` inside Los Platos's Pokémon Center, six rooms; the plane from Galar's airfield lands here (plan 33 · GL7's link).
- **`Architecture.Paldea`** (whitewashed stucco under terracotta hip roofs, shutters, flower boxes; its row of the style guide first).
- **The open map** `Paldea` (`Data/world3d/paldea`, 6 by 14 chunks, `chunks: [-3, -12, 2, 1]`), five areas south to north: `cabo_poco`, `poco_path` (with the beach under the cliffs and Inlet Grotto's mouth), `los_platos`, `south_province_area_one`, `mesagoza` (the plaza and the academy's hill at the head of the great stairs). Baked: 84 chunks, 823,703 triangles at level 0, 6,658 navigation polygons, about 9.6 MB. Places: eleven solids built as houses, shops, a lighthouse, Pokémon Centers and the academy's hall, four doors, four triggers, five zones of Pokémon seen in the field (the species Scarlet and Violet show in those places, our weights), people (Nemona at each of her scenes behind flags, the director, townsfolk, two students who battle).
- **Rooms**: `CaboPoco_PlayersHouse_1F`, `_2F`, `CaboPoco_NemonasHouse`, `InletGrotto`, `LosPlatos_PokemonCenter`, `Mesagoza_AcademyHall`, laid out from Galar's rooms of the same shapes.
- **The chapter**, `VAR_PALDEA_STORY` 0 to 7 (`cabo_poco.txt`'s header), `common.PaldeaNewGame`, objectives 800 to 806 and `paldeaFree` 899 (English and Romanian); scripts `cabo_poco`, `poco_path`, `mesagoza` and the rooms'. Looks of ours: `nemona`, `clavell`, `paldea_mom`. Trainers in `Data/world/paldea/trainers.json`; the class `Student`.
- **The sample pack's Tidemark** joins after Paldea, its ferryman in Los Platos's Pokémon Center.
- **Tests**: `PaldeaTests` (the record and the plane's landing; the bake to the byte; the areas, songs, doors both ways, zones, trainers and Nemona's teams; the new game and the arrival; the walk from the bedroom reaching every room, marker, trigger and person, the beach by its track and the academy by its stairs, never the banks; the chapter headless from the bedroom to the academy's door, the Tera Orb given once; the grotto's first answer a Scarlet game with Koraidon; the four open scenes played and skipped), and the tests that knew Paldea as unbuilt now ask the hub for a region with no table, Pokédex or objectives.

**The question for the user before PD3**: the whole region at decision 1's third (about 3 km by 3 km) as one rectangle of chunks is some 8,800 chunks and about a gigabyte of bake at PD1's density. The choices: (a) a smaller scale (a fifth, about 1.8 km square, some 3,200 chunks); (b) the baker taught to bake only the chunks that hold land within a region's polygons (the sea and the mountains' backs left out); (c) the bake kept out of the repository and baked on the machine at the first start. *Recommended:* (b), which keeps the scale and the bake in the repository. **The user chose (b) on 2026-11-14**; PD3 built it ("PD3 as built", above).

**Shots owed** (the harness's `paldea` mode isn't written): `pd00` to `pd06` Cabo Poco from four yaws, the player's door, the president's terrace over the sea; `pd10` to `pd13` Poco Path, the lighthouse, the beach and the grotto's mouth; `pd20` Inlet Grotto; `pd30` Los Platos; `pd40` the South Province's fields with their Pokémon; `pd50` to `pd54` Mesagoza's gate, plaza, the stairs and the academy's door; `pd60` the six rooms; `pd*n` after dark; and the chapter's scenes `pds00` to `pds10`. `profile`'s busiest scene (Mesagoza's plaza) is owed with them. (Written by the second session, below.)

## PD1's second session (2026-11-14)

One session, which left the map-storage question (above) alone and built nothing of PD3.

**What there is**
- **The harness's `paldea` mode** (`tools/ShotHarness/Modes/Paldea.cs`, not part of `all`): a new game in Paldea, the player put down on the places' markers and shot as the open renderer draws them: `pd00` to `pd03` Cabo Poco's lane from four yaws, `pd04` the player's house and `pd05` the president's house faced from their doors' sides, `pd06` the terrace looking south, `pd10` to `pd13` Poco Path's south end, the lighthouse, the beach and the grotto's door in the cliff, `pd30`, `pd31` Los Platos and its Pokémon Center, `pd40`, `pd41` the South Province's fields six seconds after arriving with the camera turned to the nearest wild Pokémon (it prints the ones out), `pd50` to `pd54` Mesagoza's south gate, plaza, the stairs' foot, half way up and the academy's door; after dark `pd00_..n`, `pd06_..n`, `pd30_..n`, `pd51_..n`, `pd54_academy_door_n`; the rooms `pd20` Inlet Grotto and `pd60a` to `pd60f`. Then **the chapter in a new game of its own, played by the playthrough bot** on `Itineraries.PaldeaToTheAcademy`, a shot taken as each scene comes, told by the scene's own name (`Driver.Scene`), never by the leg: `pds00` the bedroom, `pds01` Mom and the director, `pds02` the terrace, `pds03` the starters, `pds04` Nemona's battle at its menu, `pds05` the roar, `pds06`, `pds07` the grotto and its question, `pds08` the lighthouse, `pds09` Mesagoza's gate, `pds10` the director's welcome. It prints the bot's legs and what the chapter left (`VAR_PALDEA_STORY` 7, one Tera Orb).
- **The bot's run** (`Core/Autoplay/Itineraries.Paldea.cs`, `PlaythroughTests.APaldeaGamePlaysFromTheBedroomInCaboPocoToTheAcademysDoor`): from the bedroom with a Pawmot of level 40 added and a Repel's steps, the stick steered over the open map and the rooms walked, every leg done in about 190 s of game time (10 s of test). **An arrival leg is done as its door is come through, while the room's `OnEnter` scene still plays**, so the last leg is the director spoken to after the welcome, held open until `VAR_PALDEA_STORY` 7.
- **`profile`'s `mesagoza_plaza`** (`MesagozaProfile`, after the open lab's scenes; `SHOTS_ONLY=mesagoza` alone): on the Windows machine at 1920×1080 High, median 7.36 ms, floor 6.68 ms, 90th percentile 9.09 (shadows 1.49 ms over 399k triangles, the people 0.56 ms over 749k, foliage 1.11 ms), under the 8 ms budget at the median.

**Findings from the shots, for the user's judgment of Cabo Poco and Mesagoza** (none changed here: each is the land's or the places' data, or the renderer's, and the user judges the look first)
1. **No sea from the terrace** (`pd06`, `pd00`): south of Cabo Poco the land rises to a flat green horizon; `water.json`'s sea starts at z 44 and the ground there stands above level 0, so no water shows. The "terrace over the sea" overlooks grass.
2. **Mesagoza is lawn** (`pd50` to `pd52`): its plaza and streets are grass with a dirt road; no tiled plaza, no walls; the houses stand loose on the lawn.
3. **The great stairs are a ramp** (`pd52`, `pd53`): a smooth dirt slope between rock walls with no steps; grass tufts hang on its rock flanks.
4. **The grotto's door has no mouth** (`pd13`): the cliff is plain rock where the door's box is; nothing says a cave is there.
5. **Inlet Grotto is drawn as a clearing in a forest** (`pd20`, `pds06`): its hand-made room (`interior` None, `trees` Round, from the Galar Mine) fills its `X` tiles with tree canopy round a dark floor, not cave rock; the Galar Mine has the same file and so the same look.
6. **A house's side wall is grey timber, not whitewash** (`pd03`): the player's house is stucco with shutters on its front and plain grey siding with two small windows on its side; the buildings drawn from every side (`allSides`) don't carry `Architecture.Paldea`'s wall round.
7. **The open map's west edge is a green wall** (`pd03`, top left): the bank at the bake's edge stands as a sheer lawn-coloured face.
8. **Paths' edges step tile by tile** (`pd03`, `pd02`): the cover's dirt meets the grass in a saw-tooth of square cells.
9. **The field's Pokémon are specks** (`pd40`, `pd41`): six to ten are out within six seconds (Tarountula, Starly, Nymble, Lechonk, Scatterbug, Buneary, Skwovet, Fletchling, Shinx, Hoppip), but they come out 12 to 56 m off and read as dots even with the camera turned to the nearest.
10. **The player's Sprigatito is pale** in Nemona's battle (`pds04`): flat and washed light green beside Fuecoco's full colours.

**Fixed after the findings (2026-11-14, a session of its own; the shots `pd*` before and after)**
- **1, the sea** and **7, the west wall**: one cause, the land beyond the region easing up from the border to the border's median (20 to 32 m of banks) over 124 m. `OpenHorizon` now makes the land beyond a run of the border 40 m long or more under water the sea's (`SeaShares`, eased over 64 m of border): its floor stays under the water, no hills or far trees rise there, and a level sea at the run's water is laid out to the horizon and drawn with the water (`Land.Sea`, `OpenRenderer.HorizonSeaTriangles`). A map with no such run is unchanged: Galar's border has none (`OpenSceneryTests.ARegionsBorderIsSeaOnlyWhereItsWaterRunsAlongIt` reads both regions' bakes as the renderer does) and the lab's one river is 9 m wide, so both draw as they did; `WhereTheBorderIsSeaTheLandBeyondIsTheSeasAndElsewhereAsItWas` holds the rule. What the sea costs a frame is unmeasured (the `paldea` mode prints no timings; `mesagoza_plaza` sees no sea). From the terrace the cape looks out over open sea, and Cabo Poco's west looks to sea along the coast. Style guide, "Beyond the region", "The sea beyond". `water.json` is unchanged: raising the sea would flood the beach, the track's end and the grotto's door.
- **2, Mesagoza's plaza** and **3, the stairs**: a new open cover, `Paving` (appended to `OpenCover`; the style guide's table and materials): flags 0.9 × 0.6 m in courses, and where the paving leans the treads and risers of a stair every 0.25 m of height. The city's level ground is painted with it, the avenue and the academy's stairs are paved by their roads' `surface`, the stairs' banks painted rock (no grass tufts on them), and the city's south wall and its gate's two towers are solids (`places/mesagoza.json`) drawn as stone. The stairs are still a slope underfoot (steps of a real rise would be under the bake's 0.5 m cells and over the capsule's 0.35 m step); they read as steps. `profile`'s `mesagoza_plaza`: median 7.21 ms, floor 6.52 (from 7.36 and 6.68).
- **4, the grotto's mouth**: `carve/mouths.json` cuts a tunnel into the cliff's foot (its roof 2.8 m thick by `--section`), the door at the tunnel's back.
- **5, Inlet Grotto**: its room is a cave (`"setting": "Cave"`, `"battleArena": "Cave"`): rock stood up round its floor, the cave's camera and stage. The Galar Mine has the same file and look, left for Galar's sessions.

**Left**: 6, the house's side is the same stucco in the afternoon's shade, with plainer windows than its front (unchanged: the open kit's sides are `BuildingArt.PaintSide`'s, shared with every grid town); 8, the paths' saw-tooth edges (the cover is a vertex's, decided on level 0's 0.5 m cells; a fix is the ground program's or the baker's, shared by every open region); 9, the field's Pokémon as specks (`OpenWild`'s distances, 12 to 56 m, are plan 32 · OW7's for every open map); 10, the pale Sprigatito: its green is `CatLeafLight` (176, 216, 146) in `PokemonModels.Paldea1.cs`, a sculpt file this session didn't touch; a deeper green there (about 120, 196, 104) is the one-line fix. The bake was made again; where a merge meets PD3's bake, baking again after it settles it.

## PD4, first session (2026-11-14)

(This section was lost in a merge with PD3's second session and put back by PD4's second session as it was written.)

One session of two, beside PD3's second session (the bot's run, the songs, the voices) in another worktree. The rulings'
"The Path of Legends, its first Titan" has every decision.

**What there is**
- **Arven's lighthouse**: Arven (`arven` of `characters.json`, our own look) at the foot of Poco Path's lighthouse from
  the academy's welcome on, whose director now sends the player to him; his scene begins the path
  (`VAR_PALDEA_LEGENDS` 1, `FLAG_PALDEA_LEGENDS_BEGUN`; `poco_path.txt`'s `ArvenLighthouse`).
- **The Stony Cliff** (`stony_cliff`, a new area of the open map: `terrain/titans.json`, `carve/titans.json`,
  `places/stony_cliff.json`, `scripts/stony_cliff.txt`): a gully cut north into the hills from the South Province's
  third area, a ramp at its head onto the den's ledge 6 m up, and a crag 6.5 m high with its south face painted
  climbable.
- **The first Titan**: Klawf at three times its height in the gully's mouth (a Pokémon may now stand in an open place:
  `species` and `size` of a place's person), beaten (`wildbattle … titan`, a totem's aura fought alone, `TotemAura.Titan`),
  fleeing up the ramp to its den, beaten again there under a stronger aura once it has eaten its Herba Mystica.
- **The Herba Mystica scene**: Arven up the ramp, the sandwich, the mount's first power, the dash
  (`FLAG_PALDEA_MOUNT_DASH`), `VAR_PALDEA_LEGENDS` 3.
- **Objectives** 810 `paldeaArven` and 811 `paldeaKlawf`, English and Romanian; the Titan's three battle lines.
- **Baked again**: Paldea's navigation mesh now has 12 climbs (all up the crag's face), 20 hops, 47 high jumps, 1431
  glides. **A merge with PD3's second session that touches the bake is settled by baking again after it**
  (`dotnet run --project tools/OpenWorldBake -- paldea`).
- **Shots** (the `paldea` mode, `Modes/PaldeaLegends.cs`, `SHOTS_FILTER=pd9`): `pd90` Arven at the lighthouse, `pd91`
  the Titan Klawf in the gully's mouth, `pd92` at its den on the ledge (shut in by rock on three sides), `pd93` the
  crag. Looked at: the Titan is asked at three times Klawf's height (3.9 m), drawn at the open figures' clamp of 3.5 m,
  and reads as a giant beside the player; the gully's floor is lawn and its west bank a green wall, as PD3's hills are
  (finding, unchanged).
- **Tests** (`PaldeaLegendsTests`): the place, its Titan and den; the path headless from Arven's word to the dash, the
  Titan's two battles fought as Titans; the three scenes played and skipped alike; the mount ridden up the gully on
  Paldea's own bake trotting before the Titan and sprinting after; and the crag climbed by the capsule and the
  navigation mesh only with the climb, never with every other power.

## PD4, second session (2026-11-14)

The four other Titans, their powers, Arven's end of the path and the done-when. The rulings' "The Path of Legends, its
last four Titans" has every decision; the user did not answer the stand-in question, so its recommendation was taken.

**What there is**
- **Four stand-in places** (`terrain/titans.json`, `water.json`'s False Dragon lake, `places/<key>.json`,
  `scripts/<key>.txt`), each an area of the open map shut in by hills but for its mouth, beside the land that is built:
  - `open_sky`: the Titan Bombirdier (20) in a gully north off the South Province's second area, its den a ledge; swim
  - `lurking_steel`: the Titan Orthworm (29) in a sandy cutting east off the East Province's first area, its den a
    ledge; high jump
  - `quaking_earth`: the Titan Great Tusk (Koraidon) or Iron Treads (Miraidon) (45) in a dusty basin north of Cortondo,
    its den a rise; glide
  - `false_dragon`: the Tatsugiri (57) on a lake's near shore, the Dondozo (56) with it in command on the far shore;
    climb
- **The order**: each waits once the one before it is beaten (`VAR_PALDEA_LEGENDS` 3 to 11); then Arven at the
  lighthouse, his Mabosstiff well again and his battle (`paldea_arven`, 12, `FLAG_PALDEA_LEGENDS_DONE`).
- **A Titan's flight is short** (to its ramp's foot), Klawf's too: the real-engine pass found Klawf's walk to its den
  holding the field still for over ten seconds.
- **The done-when**: `PaldeaLegendsTests.TheLastTitansHerbsLetTheMountClimbTheStonyCliffsCragItCouldntBefore` takes
  the grants from the story just before the False Dragon's herbs and just after, and on Paldea's own bake the
  navigation mesh and the capsule reach the Stony Cliff's crag only after. `TheFiveTitansAreBeatenInOrderAndEachGrantsTheNextPower`
  beats the five headless with each mount, `PlaythroughTests.APaldeaGameWalksThePathOfLegendsFromArvensWordToHisBattle`
  plays the path with the bot from the lighthouse on foot (`Itineraries.PaldeaPathOfLegends`), every trigger, Titan,
  battle and walk the game's own, and the scenes are played and skipped alike.
- **Shots** (`paldea` mode): `pd94` to `pd98` the four places' Titans where they wait; `pdl01` on, every battle the bot
  meets walking the path, at its menu (`PaldeaLegendsPlayed`, `SHOTS_FILTER=pdl`): the Titans under their aura, two
  students on the way, Arven. Looked at: the places read as gullies, a sand cutting and basin and a lake in rock walls,
  the Titans stand where they should and the aura glows in battle. Findings, unchanged: the floors are the same lawn
  and grass tufts as PD3's ways; the Tatsugiri is small even at four times its height; a Titan's battle is fought on the
  meadow's stage whatever its place's cover. The harness's bot (three Pokémon at 70) lost once to the Tatsugiri (57, its special attack raised), woke at a
  Pokémon Center and won on coming back (Arven's battle also came twice, most likely the same): the loss and the Titan waiting again work in the real engine.
  It prints how each battle ended.
- **Song** `paldea/titans` (37 s loop, no clash, in the loudness window), played by the five places.
- **Baked again**: 454 chunks (from 389; the bake 62.0 MB, from 52.9 MB); the mesh's 25 hops, 47 high jumps, 12 climbs (the crag's alone), 1429 glides.

**What PD6 replaces**: the four stand-ins `open_sky`, `lurking_steel`, `quaking_earth` and `false_dragon` (their
`world.json` areas, their brushes and paint in `terrain/titans.json`, False Dragon's lake in `water.json`, their places
and scripts), moving each Titan to its province (Bombirdier to the West Province, Orthworm to the East Province's third
area, Great Tusk and Iron Treads to Asado Desert, the Tatsugiri and Dondozo to Casseroya Lake), keeping the scripts'
names, flags and the count so the tests stand; the Stony Cliff is the South Province's own and stays.

**Owed**: Arven's voice designed by ear (provisional in the cast); the bot riding the mount's links (PD2's owed).

## PD5 as built (2026-11-15)

One session, beside PD6 in another worktree. The rulings' "Starfall Street and Let's Go" has every decision; the plan
asked nothing new of the user, and nothing was waited on.

**What there is**
- **Let's Go** (`Models/LetsGo.cs`; `Core/GameEngine.LetsGo.cs`): `GameAction.LetsGo` at the enum's end (R, the pad's
  right trigger, in every preset), on foot in an open map, sends the lead at the nearest wild Pokémon within 10 m; the
  battle is `BattleCore`'s with nobody watching, the lead's moves AUTO's (`Battle/Sim/MoveSuggestion.cs`, which
  `BattleEngine.Suggest` now asks: one scorer), its record a `BattleRecordFile` that replays. A higher level, a shiny or
  a Nuzlocke run meets it in a real battle. The field shows the lead running out, the target flickering with the hits
  and shrinking away, and the lead running back (`LetsGoShow`, figures of `GatherOpenFigures`).
- **Starfall Street** (`starfall_street`, a stand-in): `terrain/starfall.json` (a valley north of Mesagoza at the
  plaza's height, the hills round it, a pass through Mesagoza's north mountains east of the academy),
  `places/starfall_street.json` (five gates, each a hall of the crew, a yard's marker `<boss>_yard`, the gate's
  `<boss>_gate`, a grunt and the boss), `scripts/starfall_street.txt`, one area line in `world.json` (and its name in
  the map's list). Baked again: 486 chunks (from 454), the mesh's 25 hops, 47 high jumps, 12 climbs, 1597 glides.
- **The raids** (`Models/StarRaids.cs`): `starraid begin "<boss>"` (a new script command, both hosts, `docs/scripts.md`),
  fifteen to fell in five minutes of the field's ticks, the crew's Pokémon standing round the yard, the first three
  sent in turn; the corner's count and clock (`ModernUi.StarRaidCount`); the game starts `<Boss>RaidWon` or
  `<Boss>RaidLost` as it ends.
- **The bosses** (`Data/world/paldea/trainers.json`, `paldea_star_giacomo` … `paldea_star_eri`, class `Team Star Boss`,
  `format` `"starmobile"`), each team ending with its Starmobile, a Revavroom with its crew's Torque move, fought on
  `BattleArena.StarBase` (style guide's row first). Looks of ours for the five and the grunts.
- **Cassiopeia's calls**: the first as the street is first walked into, one after each boss, her thanks after the fifth
  (`VAR_PALDEA_STARFALL` 0 to 6).
- **Song** `paldea/team_star` (a garage band's bluster in E minor, 25.6 s loop; `tools/MusicRender`: no clash, -19.3 dB,
  in the loudness window).
- **Objectives** 850 to 855, English and Romanian; Romanian for every line (`ro/scripts/starfall_street.json`).
- **Tests**: `LetsGoTests` (a Let's Go battle replays from its record and gives its EXP; the same seed fights the same
  battle; a higher level or a shiny is no Let's Go; an outmatched lead faints; a base's clock ends a raid on the gate;
  a raid felled to its goal is won; the three leads in turn; the bases' species and bosses) and `LetsGoGameTests` (the
  key in the whole game on Paldea's bake: a wild Pokémon felled and gone, the lead's EXP; a raid's clock run out and the
  player put back on the gate by its script; a raid won bringing the boss's battle) and `StarfallStreetTests` (the
  street's places, Cassiopeia's first call only after the academy's welcome, the grunt's question, a raid lost and won
  headless, the five bosses counted, every scene played and skipped alike).
- **Shots** (`paldea` mode, `Modes/PaldeaStarfall.cs`): `pd99` the street from its mouth, `pd99b` Giacomo's gate,
  `l80_lets_go` (with `l80b` the hits and `l80c` the fade), `l81_star_raid` and `l81b`, `l82_star_boss` on the stage.
  Looked at: the lead runs out to the Lechonk from the player's side and it shrinks away; the raid's crew stand round
  the yard with the corner's count and clock over them, and a Let's Go in the raid fells one (1/15); Giacomo's battle
  stands on the star pads before the striped tent. **Findings, unchanged**: the five bases are the open kit's plain
  `hall` (pale walls, a straw-coloured hip roof, a STAR sign), nothing of the crews' black and pink; the valley's floor
  reads as sand and the yards' mud paint doesn't show; the crew's Pokémon face the yard's middle and never move. In a
  crossed game the Starmobile grows into Turbovroom with the rest of the boss's team (`levels-crossed.txt`).

**What PD6 replaces**: the stand-in valley (`terrain/starfall.json`, the `starfall_street` area of `world.json` and
`places/starfall_street.json`), moving each base to its province (Giacomo's to the West Province's first area, Mela's to
the East Province's first area, Atticus's to Tagtree Thicket, Ortega's to the North Province's third area, Eri's to its
second), keeping the scripts' names, flags, the markers `<boss>_yard` and `<boss>_gate` and `StarBases.File` (or
changing that one constant), so the tests stand. A merge of PD5 and PD6 that touches the bake is settled by baking again
(`dotnet run -c Release --project tools/OpenWorldBake -- paldea`).

**Owed**: the playthrough bot playing a raid (`Autoplayer.Open` presses no Let's Go); Let's Go's materials for the
crafting kit (`MaterialsAfterBattle` reads a `BattleEngine`); the five bosses' and Cassiopeia's voices designed by ear
(provisional in the cast); the song by ear; who Cassiopeia is (PD8).
## PD6, first session (2026-11-15)

One session of two, beside PD5 (Starfall Street, in its own files) in another worktree; the West's areas stop at `z` -740 and its hills (to -800) step round Starfall Street's, north of Mesagoza, so neither buries the other: **after merging the two, bake again and
run both sessions' walk tests**. The rulings' "The West Province and the
fourth and fifth gyms" has every decision. The user did not answer the provinces' distances or decision 3, so their
recommendations stand (PD3's compressed distances; the later gyms scaled to the badges held).

**What there is**
- **The land** (`terrain/west.json`, `water.json`, `roads.json`, `world.json`): six areas, `asado_desert`,
  `cascarrafa`, `west_province_area_two`, `porto_marinada`, `west_province_area_one` and `medali`, north and west of
  Cortondo, each a place's file with its people; the desert and the two areas with zones of wild Pokémon at 22 to 29
  and a student each; Cascarrafa's falls a river down a slot in the hills into the pool at the city's foot; Porto
  Marinada at 4 m on its quay over the western sea, come down to by a ramp. `chunks` start at `-26`.
- **The Titans moved**: Open Sky to a gully north off the West Province's first area, Quaking Earth to the basin at
  Asado Desert's south end (the road north runs through it), every key, script, flag and the count kept; their
  stand-ins' brushes are gone from `terrain/titans.json`. Arven's direction to Open Sky rewritten.
- **Rooms**: `Cascarrafa_PokemonCenter`, `_Gym`, `_GymTest`; `PortoMarinada_PokemonCenter`; `Medali_PokemonCenter`,
  `_Gym`, `_GymTest`, `Medali_TreasureEatery`; spawns 6 to 8.
- **Kofu** (Water): his errand to Porto Marinada's auction, won by bidding at the last call (`porto_marinada.txt`), a
  gym trainer, and his battle. **Larry** (Normal): three hints about Medali and the secret order at the Treasure Eatery,
  a gym trainer, and his battle. Looks `kofu`, `larry`.
- **Decision 3's scaling**: a team for each count of badges held (`paldea_kofu_b0` to `_b7`, `paldea_larry_b0` to
  `_b7`, the plain ids the original's), picked by the leader's script; the best at `paldea.json`'s cap for that count.
- **Songs** `paldea/west_province`, `cascarrafa`, `porto_marinada`, `medali` (33 to 43 s loops, no clash, in the
  loudness window). **Town Map** written again from the bake (45 by 28), Fly's eight places. **Objectives** 830
  `paldeaKofu`, 831 `paldeaLarry`. Romanian for every new line (`docs/localisation-review-ro.md`).
- **Tests**: `PaldeaWestTests` (the areas, towns, rooms and the Titans in their provinces; Kofu and Larry won in the
  academy's order at the originals' levels; taken first, scaled down to the badges held; nothing before the welcome),
  and `PaldeaTests`' lists grown by the new areas, zones, rooms and spawns.
- **Baked again**: 625 chunks (from 454), 84.6 MB (from 62.0 MB); about five and a half minutes to bake.
- **Shots** (the `paldea` mode, `Modes/PaldeaWest.cs`, `SHOTS_FILTER=pdw`): `pdw00` to `pdw13` the places from their
  markers and the gyms and the eatery faced from their squares, `pdw20` to `pdw27` the eight rooms, `pdw30` Kofu and `pdw31`
  Larry at the far end of their halls. Looked at: Porto Marinada reads as a port, its quay paved over the sea with the
  auction house and the Pokémon Center on it. The first shot of the falls (`pdw05`) showed only the pool: the river's slot ended inside the
  cliff's face, so its drop sat in the rock; the slot now cuts through the face to the floor's edge, and the river runs
  out of a cleft at the foot of the hills into the pool. **The falling water down the face is still not drawn** (the open
  renderer draws no falls; the river's steep stretch is hidden in the cleft). Findings, unchanged: the towns and ways are lawn as PD3's are, Cascarrafa's houses far apart.

**Left for session two**
- The North Province and Glaseado: Montenevera (Ryme, Ghost, her rap battle), Alfornada (Tulip, Psychic, her photos;
  in the originals reached from the south-west through a cavern), Glaseado Mountain with Grusha (Ice, his snowboard
  run), each town's Pokémon Center, rooms, trainers, songs, Fly places and objectives, the three leaders scaled to the
  badges held as Kofu and Larry are (their `_b0` to `_b7` teams); and the two Titans still in stand-ins, Lurking Steel
  (Orthworm) to the East Province's third area and False Dragon (Tatsugiri and Dondozo) to Casseroya Lake.
- The done-when: eight badges won (a test of the eight in the academy's order and in another, beside
  `PaldeaWestTests`), and the region walked and ridden from Cabo Poco to Glaseado's top with no chunk waited for (an
  itinerary in `Itineraries.Paldea.cs` and `PlaythroughTests`, and the harness's `paldea` mode timing the ride as the
  `world` mode times Sinnoh's walks).
- Owed from this session: the bot's run to the fifth badge in `PlaythroughTests` (only headless tests play Kofu's and
  Larry's tests); Cascarrafa's falls drawn falling (`pdw05`: the cleft and the river show, the drop down the face doesn't) and its bridges over its
  river; the new cast's voices by ear (Kofu, Larry and the auctioneer have designs, no audio).

## PD6, second session (2026-11-23)

One session, alone in its worktree beside a docs session and a Carpathia sculpts session (the second harness agent).
The rulings' "The North Province and the last three gyms" has every decision. The user had not answered the provinces'
distances or decision 3, so their recommendations stand, as in the first session.

**What there is**
- **The land** (`terrain/west_north.json`, read after `west.json`; `carve/alfornada.json`; `water.json`, `roads.json`,
  `world.json`): six areas, `north_province_area_one` (north out of Medali), `glaseado_mountain` (the foot east and west
  of the snowslope, the slope and the top, with the snow weather), `montenevera`, `east_province_area_three` (north out of
  Levincia to Montenevera's east gate), `alfornada_cavern` (a carved tunnel from behind Cortondo's Pokémon Center) and
  `alfornada` with its ruins' pillars; hills at 40 m over the whole north and crags at 72 m round the slope. Glaseado's
  top is 40 m over its foot, 140 m up an even slope of about 16°. `chunks` reach `-38`.
- **The last two Titans moved**: Lurking Steel (Orthworm) to a cutting east off the East Province's third area, north of
  Levincia; False Dragon (the Tatsugiri and the Dondozo) to Casseroya Lake (the `false_dragon` area's name), east off the
  North Province's first area; each its stand-in moved whole, keys, scripts, flags and the count kept. The stand-ins'
  brushes, paint and lake are gone from `titans.json` and `water.json`; Arven's directions (`open_sky.txt`,
  `quaking_earth.txt`, `poco_path.txt`) and the two objectives rewritten.
- **Rooms**: `Montenevera_PokemonCenter`, `_Gym`, `_GymTest`; `Alfornada_PokemonCenter`, `_Gym`, `_GymTest`;
  `Glaseado_PokemonCenter`, `_Gym`, `_GymTest`; spawns 9 to 11.
- **Ryme** (Ghost): her MC's rap battle in her hall (`Montenevera_GymTest.txt`, three lines, the rhyme the right answer,
  each kept as it is got right), a gym trainer and her battle. **Tulip** (Psychic): her photographer's three moods in her
  studio (`Alfornada_GymTest.txt`), a gym trainer and her battle. **Grusha** (Ice): the snowslope run
  (`glaseado_mountain.txt`: the coach at the top, three gates of young pines in order, the line at the foot; a gate
  missed sends the challenger back up), a gym trainer and his battle in the hall at the top. Looks `ryme`, `tulip`,
  `grusha`.
- **Decision 3's scaling** for the three: `paldea_ryme` (at five held), `paldea_tulip` (six), `paldea_grusha` (seven),
  and `_b0` to `_b7` for the other counts, picked by each leader's script; gym trainers Tas, Bruno and Yuki; students
  Marta, Iker, Aitana, Ines and Rosa; wild Pokémon of the north at 34 to 45.
- **Songs** `paldea/north_province`, `glaseado`, `montenevera`, `alfornada` (40 to 50 s loops, no clash, -17.6 to -19.4
  dB loud). **Town Map** written again from the bake (46 by 38 cells from chunk -25,-36), Fly's three new places.
  **Objectives** 832 `paldeaRyme`, 833 `paldeaTulip`, 834 `paldeaGrusha`. Romanian for every new line of the scripts,
  rooms, objectives and the Town Map (`docs/localisation-review-ro.md`); voices designed for Ryme, Tulip, Grusha, the MC
  and the coach (`Data/voice/cast.json`, briefs and Kokoro blends, unheard).
- **The done-when**: `PaldeaNorthTests.TheEightBadgesAreWonInTheAcademysOrderAndInAnother` wins the eight in the academy's
  order (each later leader with the original's team) and with the last three first and the first three last (each later
  leader with the team for the badges held); the region walked and ridden: `Itineraries.PaldeaToGlaseadosTop` played by
  `PlaythroughTests.APaldeaGameIsRiddenFromCaboPocoToGlaseadosTop` headless, and drawn by the harness's `paldea` mode
  (`pdn90`): **10,570 frames (176 s) from Cabo Poco's lane to Glaseado's top, ridden 97% of them, no chunk waited for**
  (4,611 chunks made ready on the way). Its milliseconds (9.8 ms a frame, the worst 277 ms in the South Province's second
  area; the stream's worst upload frame 2.3 s, most likely the first frames' uploads) were taken on a busy machine with a
  second harness agent running, so they are owed to the quiet Windows machine.
- **Owed by the first session, done**: the bot's run to the fifth badge (`Itineraries.PaldeaWestBadges`,
  `PlaythroughTests.APaldeaGamePlaysFromTheBedroomInCaboPocoToTheFifthBadge`; the auction answered otherwise on each try,
  within the bot's four); and on to the eighth (`Itineraries.PaldeaNorthBadges`,
  `PlaythroughTests.APaldeaGameWinsTheLastThreeBadgesFromMedali`).
- **Baked again**: 910 chunks (from 625), about 121 MB (from 84.6 MB); about 80 s on Release.
- **Tests**: `PaldeaNorthTests` (the areas, towns, rooms and the two Titans moved; the eight badges in two orders; the
  three leaders scaled; a rhyme missed, a pose wrong and a gate skipped each sending the challenger back; the run's two
  scenes played and skipped alike); `PaldeaTests`' lists grown by the new areas, zones, rooms and spawns, and its walk now
  reaching Glaseado's top and Alfornada through its cavern and never the crags or the hills over the cavern; the three
  `PlaythroughTests` above; `SceneSkipTests`' list of open scenes; goldens `scripts.txt`, `levels.txt` and
  `levels-crossed.txt` accepted (the new rooms' trainers and scripts only), `species-owed.md` regenerated.
- **Shots** (the `paldea` mode, `Modes/PaldeaNorth.cs`, `SHOTS_FILTER=pdn`): `pdn00` to `pdn15` the places (the slope from
  its foot and from the top, the gym on the top, a gate of the run, Montenevera's gym, Lurking Steel, Casseroya Lake, the
  cavern, Alfornada's gym and ruins), `pdn08n` Montenevera after dark, `pdn20` to `pdn28` the nine rooms, `pdn30` to
  `pdn32` the leaders, `pdn90` the ride's end on Koraidon at the top. Looked at: the cavern reads as a cave with daylight
  at its end; the ruins' pillars stand round the knoll; the run's gates are two young pines each on the slope; the top's
  terrace and gym stand in the snow. **Findings, unchanged**: the snowslope seen from its foot fills the picture with
  snow and nothing else (the camera sits low against a long white slope); the hills between the places read as cliffs
  with flat green tops, as everywhere in Paldea; the three gyms are the open kit's plain gym (Montenevera's no stage,
  Glaseado's no snow on its roof); Ryme's hall has the Ghost arena's purple and nothing of a stage.

**Owed**
- Starfall Street's five bases are still in PD5's stand-in valley (PD5's "What PD6 replaces"): the provinces exist now,
  and the move is a session's work of its own (the bases' places, scripts' markers and `StarBases.File`).
- Cascarrafa's falls drawn falling (the open renderer draws no falls) and its bridges, owed from the first session.
- The open places' people's lines (the hiker, the skier, the fan, the elder...) are untranslated, as every open map's
  are: no translation file covers an open place's people yet.
- The run keeps no clock; the gym tests of Ryme and Tulip are ours, not the originals'; no gym battle is a double.
- The new cast's voices by ear; the four songs by ear; `profile`'s ride and the north's scenes on the Windows machine.

**Waits for the user**: the north's distances and looks (from the `pdn` shots), the tests as built, the run with no
clock, the songs and voices by ear, the ride's milliseconds on the quiet Windows machine.

## PD7 as built (2026-11-23)

One session, in its own worktree beside the lead's full test run (so nothing was timed). The rulings' "Tera Raids and
picnics" has every decision. Taken at recommendations: decision 4 (the sandwiches a second section of ecipes.json on
the camp's one pot); the plan's "spawner on Dice and the day" read as the dens' day-drawn spawner (the day's number and
the crystal's id, never the dice), so the crystal drawn on the map and its script always agree and a test replays it.

**What there is**
- **Tera Raids** (Battle/Sim/BattleCore.Raid.cs): RaidTerms.Tera; the boss Terastallized into the crystal's type for
  the whole battle, its HP raised by its stars (RaidRules.TeraHpTimes), no Dynamax in a Tera Raid; the crystal shield
  from four stars (TeraShieldAt, TeraShields, a fifth through, three broken by a Terastallized raider's hit); the
  raiders' orbs charged by three moves (OrbCharge, OrbChargeOf, refused with attle.teraOrbNotCharged till then,
  the students carrying orbs of their own); the clock (TeraTurns, two more for every fall, TeraTurnsLeft,
  TheTeraClockRanOut) in place of the fourth fall; the boss acting once a turn. BattleRecordFile.RaidTera keeps it.
- **The crystals** (Data/world/paldea/crystals.json, Models/Raids.cs): twelve sites in the South, East, West and North
  Provinces, Poco Path and Glaseado's foot, each a trigger crystal_<id> of its place with its own script
  (aid crystal "<id>", call TeraCrystal; nine script files new, for places that had none); Raids.CrystalToday
  (lit one day in two, stars by Paldea's badges, TeraMostStars, levels by stars, the Tera Type the row's or drawn),
  seven students as raiders. common.TeraCrystal asks for a Tera Orb, fights, gives the shards (aid shards,
  Raids.ShardsOf, the shard items TR5's keeper reads) and throws a ball that never fails (aid catch, the Tera Type
  kept). The glow is DenBeams' crystal (DenBeam.Tera), the style guide's "Tera crystals" first.
- **Picnics**: ecipes.json's sandwiches (seven, ours; half an hour each), RecipeTable.Sandwiches, Meals taking
  its table, CampKitchen.Table, and in Paldea CampKitchenNow giving the picnic's table with no kit and HasCamp
  offering it; the screen titled PICNIC and THE PICNIC TABLE (campPot.picnicTitle, 	ableTitle, onTheTable, make).
  PowerKind.Egg and Sparkling at the enum's end (powers.json, PowerTable.TakesType); readers: the picnic basket,
  GameEngine.MakeWild's shiny rolls (SparklingRolls), and on the open map OpenWildField.Favoured and
  MoreStanding from the Type and Encounter Powers (FavouredBy, MoreStandingBy, set each frame by
  UpdateOpenField). Models/PicnicBasket.cs: the couple, a try a minute at the Day Care's chance and the Egg Power's
  strength, ten at most, given as the picnic is packed up (EmptyThePicnicBasket, picnic.eggs).
- **Mesagoza's deli**: a clerk before the café (mesagoza_deli, MesagozaDeli), craft deli opening the kit's PANTRY
  screen with no kit.
- **The harness**: Modes/PaldeaRaidsPicnics.cs in the paldea mode (SHOTS_FILTER=ft72,27g), with the driver's
  StartRaid: t72b_tera_crystal and t72c_tera_crystal_night (a crystal lit today, by day and after dark),
  t72a_tera_raid_begins, t72_tera_raid (a four-star Baxcalibur's raid at the menu with its crown and three shields
  standing, turn 3 of 12), 27g_picnic, 27g_picnic_table (butter and an Oran Berry making a Berry Jam Sandwich) and
  27g_picnic_sandwich (its Type Power for half an hour). Looked at; the picnic's title was "THE POT" at first and is
  now its own.
- **Tests**: PaldeaTeraRaidTests (the boss and its HP, the orb's charge, the shield broken three at a time, the clock
  losing a raid no fall ends, the replay from its file, a crystal's day, the crystal's script to its catch and shards and
  its need of an orb, every trigger a crystal and the glow) and PaldeaPicnicTests (the sandwiches held to the
  kitchen's rules, a sandwich's power for half an hour, the Type Power changing the meadow's draws and wearing off to
  the plain draws exactly, the Encounter Power's more and its wearing off, the basket's Eggs, the deli). Run beside them:
  GalarRaidTests, KitchenTests, TrainingPowersTests, OpenWorldWildTests, GalarWildAreaTests, ScriptTests,
  ContentTests, MealStandTests, the Tera, record, oracle and golden battle tests and RomanianTests, all passing;
  goldens accepted: scripts.txt (common.TeraCrystal's block) and the two schemas (ecipes, powers), no battle
  golden moved. ContentCheck finds nothing; the Paldea bake's --check is the same to the byte; Romanian for every new
  line (docs/localisation-review-ro.md), TextTool report rewritten.

**Waits for the user**: the crystal's look (t72b, t72c); the raid's numbers (HP times, the shield's shares and hits,
the clock's turns, the shards); the sandwiches, their makings and their powers' strengths; the picnic's look.

**Owed**: the raid's clock and each raider's orb drawn on the raid's screen; the bot playing a raid (PlaythroughTests);
the black crystals of six and seven stars (PD9); a sandwich stacked on the table rather than a pot stirred; the deli clerk's
name in Romanian (no open map's people are translated yet); profile of a crystal's glow is not needed (a few ribbons).
