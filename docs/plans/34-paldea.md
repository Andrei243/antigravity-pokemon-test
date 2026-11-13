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

- [x] PD1 Paldea's file, Cabo Poco, Los Platos and Mesagoza (2026-11-13, its first session: the file, the open map of five areas described and baked, six rooms, the chapter from the bedroom to the academy's door headless, the plane from Galar landing; "PD1 as built", below. **Waits for the user**: the look of Cabo Poco and Mesagoza, judged from the shots its second session owes, as the done-when has it; the bake's footprint for the whole region (the question below); `Architecture.Paldea` seen in the game. **Owed**: the bot's run to the academy's door in `PlaythroughTests`, the harness's `paldea` mode and its shots, the academy's halls past the entrance hall, the voices of the new cast designed by ear)
- [x] PD2 The mount on the open world (its rules built 2026-11-09 as plan 30 · FM12, its rig 2026-11-12 as FM13, and its last part 2026-11-13: the navigation mesh's hop, high jump, climb and glide links for a region whose `world.json` says `mount`, the lab's yard with a painted climbable face, the done-when ridden in `MountLabTests`, Paldea's mesh baked again with them; "PD2 as built", below. **Owed**: the playthrough bot riding the links; the user's word on the yard's shots `fwy0` to `fwy6`)
- [ ] PD3 The South and East Provinces and the first three gyms
- [ ] PD4 The Path of Legends
- [ ] PD5 Starfall Street and Let's Go
- [ ] PD6 The West and North Provinces and the last five gyms
- [ ] PD7 Tera Raids and picnics
- [ ] PD8 The League, Area Zero and the Way Home
- [ ] PD9 After the Hall of Fame
- [ ] PD10 The Teal Mask (optional)
- [ ] PD11 The Indigo Disk (optional)
- [ ] PD12 Mochi Mayhem (optional)

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

**The question for the user before PD3**: the whole region at decision 1's third (about 3 km by 3 km) as one rectangle of chunks is some 8,800 chunks and about a gigabyte of bake at PD1's density. The choices: (a) a smaller scale (a fifth, about 1.8 km square, some 3,200 chunks); (b) the baker taught to bake only the chunks that hold land within a region's polygons (the sea and the mountains' backs left out); (c) the bake kept out of the repository and baked on the machine at the first start. *Recommended:* (b), which keeps the scale and the bake in the repository.

**Shots owed** (the harness's `paldea` mode isn't written): `pd00` to `pd06` Cabo Poco from four yaws, the player's door, the president's terrace over the sea; `pd10` to `pd13` Poco Path, the lighthouse, the beach and the grotto's mouth; `pd20` Inlet Grotto; `pd30` Los Platos; `pd40` the South Province's fields with their Pokémon; `pd50` to `pd54` Mesagoza's gate, plaza, the stairs and the academy's door; `pd60` the six rooms; `pd*n` after dark; and the chapter's scenes `pds00` to `pds10`. `profile`'s busiest scene (Mesagoza's plaza) is owed with them.
