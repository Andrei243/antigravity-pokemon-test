# Plan 44 · Modes beside the journey

Written 2026-11-07, before any session, at the user's request: one of ten plans (35 to 44) from [the study of what else to add](studies/what-else-to-add-study.md).

**Goal**: three larger modes the spin-off games made, each ours and each reached from the game without the journey needing it. A side story in which a syndicate of ours locks Pokémon's hearts so that they obey without bond, and the player takes them back from its trainers and opens their hearts over time (Colosseum and XD as a mechanic). A photo tour that rides a rail through courses of our own, where wild Pokémon go about their day and a guide of ours judges the photographs into a report (Pokémon Snap as a mechanic). And a delve through hollows whose floors are made from a seed, where the team's own Pokémon go down without the player, a leader and a partner taking turns on the floor's grid (Mystery Dungeon as a mechanic, on plan 14 · B10's floor generator). Each is rules without a screen that a test plays to its end, a screen on the kit, a harness mode of its own and content we write. Of the spin-offs' other modes, a loop catch (Ranger) is kept as an optional session; kingdoms (Conquest), research by the night (Sleep), the card game and the real-time brawlers are left out, with the reasons under "Left out".

## Where we are

**Locked hearts**: nothing of the mechanic is built (no `snag`, `purif` or locked state anywhere; only the move Purify and the ability Purifying Salt match).
- A ball thrown at a trainer's Pokémon is refused by one line of `BattleCore.WhyNotItem` (`battle.theTrainerBlockedThe`, `Battle/Sim/BattleCore.Items.cs`); a ball that catches goes through `ThrowBall`, which ends the battle, since only a wild Pokémon is ever caught.
- A Pokémon that won't obey is `BattleCore.Disobeys` (`BattleCore.Moves.cs`) on `Models/Obedience.cs`'s rule for an outsider over the badges' cap, rolled as `RollKind.Obedience`: it loafs, uses another of its moves or hurts itself. `RollKind` ends at `Affection`, `ChoiceKind` at `GoNear`.
- Trainers of ours are `Data/our-trainers.json` (`TrainerDatabase.Ours`, 51 records, each member a species, level, IV scale, personality and moves). A crossed region's trainers are relevelled by plan 18 · W4b's ladder (`Trainer.Relevelled`), and grown into later stages and species of ours (`Models/OursGrowth.cs`).
- What lasts on a Pokémon in battle is set by the log's events alone: `ConditionLooks` (`Battle/ConditionLook.cs`, plan 09 · L2), its marks in `MoveFx.Conditions.cs`, held by `BattleConditionLookTests`.
- Friendship (`FriendshipRules`) and its perks (`Models/Affection.cs`, plan 10 · F7, read from friendship: there is no `Pokemon.Affection`), the camp's pat and meal once a visit (`CampVisit`, plan 10 · F6, open with the FOLLOWER row, `GameEngine.HasCamp`), the steps counted in `GameEngine.OnStep`.
- Five finished regions, each with its post-game, its harbour sailing both ways and Fly between them (plan 18 · W8, W9); the requests' log (plan 12 · Q21, `Story/Requests.cs`) for an errand's next step; the Nuzlocke's gifts clause (`Nuzlocke.Gift`) and the randomiser's trainers mapped on a copy (`Trainer.WithTeamOf`), which every new way of giving a Pokémon asks.
- Plan 41 holds the gathering of the villains the game already has and Looker's investigations; plan 35 the care book and close-up (`Models/Care.cs`, `GameState.Care`).

**The photo tour**: photographs are taken and kept, never judged.
- Photo mode in the field and in battle (plan 12 · Q16 part b, Q17): `Overworld/PhotoView.cs`, `Core/GameEngine.Photo.cs`, `Graphics/PhotoFilters.cs`, `Graphics/BattlePhoto.cs` and `BattleCamera.Hold`; the screenshot key writes the 4K frame (`CaptureScreen`, `SavePaths.Screenshots`).
- Cameron's album in Cianwood (plan 20, parts q and t): `Models/PhotoAlbum.cs` (36 photographs as plain values, `SaveData.Photos`, null while empty), `Models/PhotoScenes.cs` (a print drawn from plain shapes by setting), `UI/PhotoAlbumScreen.cs`, `GameState.PhotoAlbum`, the script's `photo`: a photograph kept as numbers and redrawn.
- Models outside a battle: the gallery (plan 12 · Q30, `PokemonSprites.Render` with a turn and a zoom), the camp's six (F6), the title's Giratina (`TitleScene`). A battle's stage is built without the GPU in the smooth battle style (`BattleArenas.Build(MeshBatches, ArenaSpec)`, `BattleStage`, lit by `ArtLook.ArenaRig`), and a body is posed by `PokePose` (`Walk`, `Moving`, `Asleep`, `Cheer`, `Attack` with its `Kind`, `Dodge`, `Tired`, `Charge`, `Stat`); plan 35 · BN2 adds `LookAt`, `Happy`, `Eating` and `Sleepy`.
- Every species and form has one of Platinum's nine habitats (`DexEntries.HabitatOf`, 2,059 written entries), which the Pyramid's wild tables already draw from (`BattlePyramidChallenge.WildTable`).
- Wild Pokémon in sight (plan 10 · F5, `Overworld/FieldPokemon.cs`) are field sprites of four facings; the research record (plan 15 · E1) counts seven kinds of deed (`ResearchKind`), none a photograph. Plan 43 holds the player's own photographs: poses, a photo book of pictures (`PhotoBook`) and photo spots.

**The delve**: floors from a seed are built, and nothing else of a delve.
- `Models/Frontier/PyramidFloor.cs` (plan 14 · B10): a floor made from its seed alone, four by four squares of eight from eight pieces of ours, Emerald's templates of trainers and finds, the way in and the stairs in two squares, `Reachable`, and `ToFile` to a `MapFile`. `BattlePyramidChallenge.MakeFloor` stands trainers on it, its dark is one Flash doesn't lift (`Map.FlashLifts`, `DarkReach`), and `GameEngine.MapNamed` answers a warp to `BattlePyramidFloor` with the round's floor. Its meetings are fought on the battle screen.
- Field sprites with walk strips (`FieldSprites.Get`, `GetWalk`; plan 10 · F1, F2), a thing of the map dressed as a Pokémon (`Follower.Dress`), the surfer drawn on a species' sprite (`Player.Carrier`), wild ones wandering a tile at a time (`FieldPokemon`).
- `DamageCalculator.CalculateDamage(attacker, defender, move, rng, rules)` works on two `Pokemon` with no battle; `Formulas.ModernBaseDamage`, `ModernFinalDamage` and `HitRate` are the formula's own steps; `MoveData` says in fields what a move does (`InflictStatus` and its chance, `TargetStatChange`, `StatStageAmount`, `RecoilPercent`, `DrainPercent`, `HealPercent`, `FlinchChancePercent`, `ConfuseChancePercent`, `CritStage`, `Target`), as the core's `ByItsFields` reads them; `LevelScaling.At` (plan 14 · B11) makes a copy at a level.
- No turns on a grid, hunger or recruiting exist anywhere. The Black Tower (plan 22 · U10) and the Trainer Hill (plan 21) are climbs of battles with the Frontier's rules.

**The others**: the pointer (plan 12 · Q6, Q7: `PointerFrame`, its `Held`) and the stick (`ActionFrame.MoveX`, `MoveY`), which a loop catch would read; the wall clock (`WallClock`, `SaveData.LastSaved`) and Poké Jobs (plan 15 · E3), which already pay for real hours away.

## Design

**Content of our own.** Each mode takes a spin-off's mechanic and makes everything else ours: the syndicate (Lockwork, in this plan's words), its agents, admins and head, the device's giver, the spring's rite, the tour's guide and courses, the delve's camp keeper and hollows, and every line, look, song and number. Nothing is recreated from those games: no region, town, villain team, host, mascot, item name or rank of theirs, no new type or move made for the locked (they fight with their own moves), no professor of theirs judging photographs, no guild of theirs. Characters this game already has (the regions' Champions and Leaders) may appear as they stand. Each feature has a plain name of ours: a locked Pokémon and its lock, reclaiming, breaking loose and calming, a heart opened; the photo tour, its courses and its report; the delve, its hollows and its camp. The games' names appear here only to say where an idea came from.

**One shape for all three.** Rules in GPU-free classes that take plain inputs, their chance from a generator they are handed (seeded from `Dice` as a ride or a run begins, a seed in a test) and their time from `FrameClock` or their own clock; screens whose logic is methods that take no input, drawn with `ModernUi`, each on a `GameState` of its own (appended); data written by hand under `Data/` and read through `GameDataFiles`; words as `ui.json` keys (sections `hearts`, `photoTour` and `delve`, free today); a script command each appended to `Op` with both hosts; save fields null while empty, so no save's version is raised; every target and mesh through `Graphics/Gpu.cs` under owner words of their own; a harness mode each, none part of `all`.

### Locked hearts

**The lock** (`Models/HeartLock.cs`; `docs/mechanics/rulings.md`, "Locked hearts", our numbers). `Pokemon.Lock` is a `HeartLock` or null: its gauge (100, five bars of 20), the EXP earned and not yet given (`HeldExp`), how many of its moves have come back (the first from the start, one more as each of the first three bars empties) and the id it was reclaimed under. It is saved as `SavedPokemonData.Lock` (null in every older save) and copied by `CopyStateFrom` and `Clone`. While locked a Pokémon gains no level, no friendship and no evolution, is taught no move by a machine or a tutor and takes no Rare Candy, and can't be left at the Day Care, sent on a job, entered at the Frontier, the Pokéathlon or a contest, or traded (the Home box, online). One rule, `HeartLock.WhyNot(LockedUse)`, is asked at each of those places. The gauge falls for a battle fought in, a knock-out made, every 128 steps in the team, the camp's pat and meal (and plan 35's care once it exists), a calm in battle and the spring once a day, so a heart opens in about two hours' play with it in the team. At nought the heart opens (decision 4): the haze lifts, its moves come back, its held EXP is given through `GainExp` up to `GameEngine.CurrentLevelCap()` with a level's moves asked as the bag's Rare Candy asks them, and its friendship is its species' base.

**In battle** (`Battle/Sim/BattleCore.Locks.cs`). `CoreSetup.Reclaimer` (and `BattleSetup`, `BattleRecord` and the Vs. Recorder's file, since it changes how a battle goes) lets a ball be thrown at a trainer's locked Pokémon, which `WhyNotItem`'s line still refuses for any other. Caught, it leaves the trainer's team on a line of ours (`Caught`, then the trainer's next by `PickReplacement`), and reclaiming the last wins the battle and its prize. Lockwork's people battle one on one, so a ball has one target (the content check holds that no double has a locked member). A locked Pokémon knocked out instead goes back to the syndicate, noted as missed, and is met again with the courier (below), so none is lost. A locked Pokémon of the player's holds its EXP (`BattleCore.Give`), has its sealed moves refused (`WhyNot`), and may break loose: a roll at its action (`RollKind.Lock`, appended), likelier on the turn after a critical or super-effective hit taken. Loose, it heeds an order one time in two (the rest as `Disobeys` has it) and loses a sixteenth of its HP at the turn's end. `ChoiceKind.Calm` (appended, `BattleChoice.Calm`, refused for anyone not loose) ends it with a line and lowers the gauge. Events appended at `BattleLog.cs`'s end (`LockShown` on the send-out's line as a condition rides on it, `BrokeLoose`, `Calmed`, `ExpHeld`) are all the face learns from. A member of a syndicate trainer's team says `"lock": "<id>"` in `our-trainers.json`; `TrainerDatabase.PartyOf` gives it a full lock, and relevelling never grows a locked member into another species. No battle without a lock or the Reclaimer moves: the golden battles hold still.

**On screen.** The style guide first: "A locked heart" under the battle's marks, the summary's gauge, the opening. A grey haze of our own hangs about a locked body (a mark of `ConditionLook`, its look in `MoveFx.Conditions.cs`, under the conditions' cap), a lock stands beside its name on the HP box and the party's cards, the summary shows the gauge as five bars and the sealed moves as locks, and CALM stands where RUN does while the player's Pokémon there is loose (as SHIFT stands in a triple). The opening is a scene of its own (`UI/HeartOpeningScreen.cs`, `GameState.HeartOpening`), a function of time and a button as the evolution's is: the model offscreen before the virtual screen, the haze lifting, its cry, then the level panel for the held EXP (`ModernUi.LevelUpOverParty`). The Reclaimer is a key item of ours (`tools/DataImporter/Overrides/items.json`).

**The story** (decision 2). Lockwork ships locked Pokémon on the sea lanes the regions' harbours share, so its cells stand at the ports. After `SinnohHallOfFame` an agent on a Battle Zone route sends out the first; Looker, who has followed Lockwork's shipments since the Game Corner, gives the Reclaimer in the Survival Area (a device his bureau's technicians made from a part seized at the quay) and shows the spring (decision 10) (a tile of Sendoff Spring's water, read with `heart spring`). Each finished region's cell opens after its Hall of Fame: agents at its port (the Fight Area's quay, Vermilion City's harbour in Kanto, Olivine's port, Lilycove City's harbour, Castelia City's piers) and an admin in a room of ours behind a way the overlay opens (behind the quay's containers first). Thirty locked Pokémon, six a cell, each a species of that region's own games, none legendary or mythical, each species once (decision 3). The head office is a tower on Castelia City's avenue (rooms of ours from a door of the hand-made `CasteliaCity` map): its floors, a head of ours with the last locked Pokémon, the machine stopped. Then a courier of ours stands in its lobby with every locked Pokémon the player knocked out, six at a time, until all are reclaimed. Each cell is a request of plan 12 · Q21's log. A cell's people are hidden until it opens by `common.LockworkBegun` and its fellows, run by `common.NewGame` and by each place's arrival for an older save, so no story version is raised. Teams are written at their region's own levels and relevelled by the ladder as its other trainers are; looks are rows of `characters.json`, the class's eye and battle songs of ours are in `sound-map.json`, and the admins' lines are in `trainer-lines.json`.

### The photo tour

**The ride** (`Models/PhotoTour.cs`). A course (`Data/photo-tour.json`, written by hand) is a rail of points the cart follows at its pace for about three minutes, its scenery's pieces, its residents and the report's points that open it. A resident is a species, where it starts and its day: a loop of actions (idle, walk to a point, sleep, eat at a spot, play with another, show off a move as a physical, special or status attack) and one rare action with what brings it on (a fruit landing within reach, the whistle, another resident's action, a time on the rail). A ride (`TourRide`) is a function of its clock, its seed and what the player did: the aim (yaw within 60° of the rail's way, pitch within 25°, zoom 1 to 3), ten fruit thrown, the whistle (it wakes sleepers, turns heads to the lens, startles the shy) and sixty frames of film.

**The judge** (`Models/PhotoJudge.cs`). The shutter records each resident in the frame: its look, action and pose time, place and facing, and its box on the frame from `TourCamera.Project` (its feet, its height from `FieldSprites.MetresOf` at the course's scale, its width by body plan), less what a nearer resident or the course's screens cover. `PhotoJudge.Score` gives each subject points for its size (best at a third to two thirds of the frame's height), placement, facing the lens, being uncovered and others of its kind beside it, and stars for what it was doing: one for a habit, two for its day (eating, sleeping, playing), three for a reaction (to a fruit, the whistle, another resident), four for the rare action. The best subject is the photograph's.

**The report** (`TourReport`, `SaveData.PhotoTour`). The best photograph of each species at each star, each course's best ride, and the total, which opens the next course and brings the guide's rewards (the game's items and money, a table of ours). A photograph is kept as its record and redrawn as a print when its page is shown (the course's scenery from its camera, the subjects at their poses, offscreen), as Cameron's prints are kept as numbers; the screenshot key writes the frame itself for a player who wants a file, and once plan 43's photo book exists a photograph the player keeps from the report goes into it as a picture (`PhotoBook`). The Pokédex and its research are not told, so no completion is made easier by a photograph.

**The scene.** A course is no map of a region: a scene of its own, built without the GPU in the battle's smooth style from the arena kit's pieces (`Graphics/CourseScene.cs` beside `BattleArenas`: ground along the rail, water, trees, rocks, hills, a cave's walls), lit by a rig of the course's hour, the residents drawn by `PokemonRenderer` and posed by `PokemonAnimation` (eating with plan 35 · BN2's `Eating` where it exists, the bow of `Charge` until then), the fruit and the whistle's rings in an `FxList`. `Graphics/CourseRenderer.cs` draws it through `RenderContext` as `BattleRenderer` does, one 3D scene a frame, its residents' models asked for (`PokemonModels.Request`) behind the ride's opening fade. The viewfinder (`UI/PhotoTourScreen.cs`, `GameState.PhotoTour`) aims with the arrows, the stick or the pointer, shoots on Confirm, throws on the item button, whistles on Run and zooms on the page buttons.

**Six courses of ours**, of Platinum's nine habitats and species of every region: a reed river (grassland, water's edge), a deep wood (forest), a reef shore (sea), a cavern (cave), a high pass (mountain, rough terrain) and a garden at night (urban, rare), the last opened by the report; about fourteen residents each. A guide of ours stands on Pastoria City's observatory floor by the binoculars (`PastoriaCityObservatoryGate2F`) once the National Pokédex is had (decision 7), with the script command `tour`.

### The delve

**The floors** (`Models/Delve/FloorLayout.cs`, `DelveFloor.cs`). B10's way of laying a floor (squares of pieces, a way in and stairs, reachable ground) moves out of `PyramidFloor` into `FloorLayout`, which both use; every Pyramid seed still gives the floor it gave (a golden of a hundred floors, written before the move). A hollow's floor adds its theme's pieces (a cave's rock, a wood's trees, a sea cave's pools that only a Water type crosses), its residents (drawn from the hollow's table at its level, some asleep), its items, snares of ours (sleep, poison, a drop to the next floor) and its stairs; `ToFile` makes it the map `DelveFloor` (a template in `Data/maps`, in Sinnoh's list, answered by `GameEngine.MapNamed` as the Pyramid's floor is).

**The team** (`DelveRun`). The player stays at the camp; a leader the player steers and a partner, chosen from the team, go down as copies at the hollow's level (`LevelScaling.At`) with up to two recruits, and nothing a run does reaches the player's own Pokémon. One hunger for the team falls a point every ten turns and bites the leader's HP at nought; food is the game's berries. The run's bag holds what is found: an Escape Rope leaves with it, and a run whose leader falls keeps none of it, but a Revive in the bag is used at once as the leader falls.

**The turns** (`DelveTurns`, `DelveMoves`). Every step or action of the leader is a turn, and then everyone acts once in Speed order (ties by the run's seed): the partner and recruits follow and strike what is in reach; residents wake as the team comes near, come by the floor's paths, strike, and some flee when low. A move reaches by its `MoveTarget` (the tile ahead, the tiles round, the room), hits by `Formulas.HitRate`, damages by `DamageCalculator.CalculateDamage` under the modern rules on the run's generator, and does what its data's fields say (a condition, confusion, a stat's stages, healing, draining, recoil, a flinch as a lost action); a move that needs a battle (a switch, a weather, a field) is refused (`DelveMoves.WhyNot`). A plain strike costs no PP and goes through `Formulas.ModernBaseDamage` and `ModernFinalDamage` with an effectiveness of one, so no type is immune to it. Abilities and held items do nothing on a floor (decision 5 and the rulings).

**Recruits** (`Recruits`). A resident knocked out may ask to join (a roll of ours by its catch rate and the leader's level against its own), up to two at a time. One that reaches the hollow's end may come home to the boxes at the hollow's level (decision 6): a gift the Nuzlocke counts, its species asked of the randomiser, an id from `GiveIds`, met at the hollow.

**On screen.** The floor is drawn by the field's renderer with the leader in the player's place (`Player.Avatar`: its field sprite and walk strips, drawn by `WorldRenderer.GatherActors`; null everywhere but a floor), the team and the residents as things of the map dressed (`Follower.Dress`) and moved by the turns, never by `Wandering`. A strike plays a lunge, a burst of the move's type on the target's tile (`SceneEffect`), the target shaken (the set pieces' shake) and its HP over it; the dark closes round the team where a hollow is dark. `GameState.Delve` and `Core/GameEngine.Delve.cs` play a turn to its end before the next is read; `UI/DelveScreen.cs` holds the moves with their reach, the bag, the team and leaving, drawn by `ModernUi.Delve.cs` with the team's cards, the hunger, the floor and the log's last lines.

**The camp** (`Models/Delve/HollowCamp.cs`, `Data/hollows.json`). A keeper of ours in the Survival Area (decision 7) with a board of requests drawn from the day's number (`SpecialEncounters`): someone's Pokémon lost on a floor, a thing to find, a client to see to the stairs, a strong resident to beat. Ranks of ours by points, and four hollows of ours (a sea cave, a wood's roots, a mountain's heart, an old tower) at levels 25, 50, 75 and 100, each with a guardian on its last floor. `SaveData.Delve` keeps the records, the rank and the day's requests taken; the script command `delve` reaches it.

### The loop catch (optional)

A warden's station of ours lends a looping disc. On a screen of its own (`Models/LoopCatch.cs`, `UI/LoopScreen.cs`, `GameState.Loop`) a wild Pokémon moves by its speed and body plan, and each loop drawn round it with the pointer or the stick calms it a step, until a strike of its breaks the line; calmed, it does one task for the station and goes free. A second check judged it low value: it is a stylus game, harder with a pad's stick than with a mouse, and the game already catches and has field moves without a knower (plan 12 · Q18).

### Left out

- **Kingdoms and links (Conquest)**: a strategy game of its own (a campaign map, armies, warriors linked to Pokémon, battles on terrain grids) whose battles are not this game's; even on the delve's grid it would want a campaign and an AI of its own, for a journey that has no use for it. The delve's grid is where it would start if the user asks after SD9.
- **Research by the night (Sleep)**: it measures the player's sleep, which a PC game can't. What is left, a reward for real hours away, is what Poké Jobs pay (plan 15 · E3); plan 40's calendar gifts, plan 35's care and plan 39's cooking hold the rest.
- **The trading card game**: a second game (cards, decks, rules, an AI and its clubs) for little in a long journey; plan 42's card-flip game is the card pastime.
- **The brawlers and the team arena (Rumble, Pokkén, Unite)**: real-time fighting needs an engine of its own and adds nothing to the journey; a cabinet of the kind would be plan 42's.

## Sessions

Every session: the style guide first for what it draws and the rulings for what it rules; the rules without a screen and their tests (a seed, a fixed clock, `CoreScenario`, `HeadlessScriptHost`); the screen's logic in methods that take no input, drawn with the kit; its harness mode run and its shots looked at, a second run diffed empty; words as `ui.json` keys in English, Romanian in plan 17 · N6's next slice; `ContentCheck` and `VoiceTool catalogue --add-missing` for what it adds to the world, its script command's row in `docs/scripts.md`, `ScriptTests.EveryCommand` and `ScriptRunner.StopsAHurry` (which names every `Op`); `CHANGELOG.md`.

### SD1 · Locked hearts: the lock and the battle
- `Models/HeartLock.cs` (the gauge, bars, held EXP, moves come back, `WhyNot(LockedUse)`, every way it falls, the opening), `Pokemon.Lock`, `SavedPokemonData.Lock`, `CopyStateFrom` and `Clone`; the refusals at the Day Care, Poké Jobs, `FrontierRules`, the Pokéathlon, contests, the Home box's trades, evolution, friendship, TMs and tutors and Rare Candy; the falls from `OnStep`, `EndBattle` and `CampVisit`.
- `Battle/Sim/BattleCore.Locks.cs`: `CoreSetup.Reclaimer` (with `BattleSetup`, `BattleRecord`, `BattleRecordFile`), the catch in a trainer's battle, `RollKind.Lock`, `ChoiceKind.Calm`, the held EXP, the sealed moves, the four events; `TrainerDatabase.PartyOf` reading `"lock"`; relevelling that never grows a locked member; `SaveData.Hearts` (reclaimed and missed by id, opened; null while empty); the rulings' "Locked hearts".
- Tests (`HeartLockTests`, on `CoreScenario`): a ball refused without the Reclaimer and at an unlocked Pokémon with it; a reclaim catches and the trainer sends the next; reclaiming the last wins and pays; a knock-out goes to the missed list; EXP held and given on opening under the level cap; a sealed move refused; breaking loose with the roll fixed by kind; CALM's line, event and gauge; a reclaim battle replays from its record; `ACopyTakesOnEverythingThatCanChange` with `Lock`; a save round trip; each refusal; a gauge empties by steps alone within the bound.
- Shots: none (the face is SD2's); the `battle` mode's `diff` is empty.
- **Done when** a test reclaims a locked Pokémon from a trainer, takes it into the team and opens its heart once its gauge empties, with its held EXP given, and `battles-platinum.txt` and `battles-modern.txt` don't move.

### SD2 · Locked hearts on screen
- The style guide's "A locked heart" first; the haze (`ConditionLook`'s mark, `MoveFx.Conditions.cs`), the HP box's and the party's lock, the summary's gauge and sealed moves, CALM in RUN's place, the reclaim's line and throw, the follower's field sprite tinted while locked; `UI/HeartOpeningScreen.cs` and `GameState.HeartOpening`, drawn by `ModernUi.Hearts.cs`; the Reclaimer's item, icon and coverage floor; `Op.Heart` (`heart give`, `spring`, `open`, `count`, `missed`) with both hosts.
- Tests: `HeartLookTests` (the haze set by events alone through a reclaim battle, as `BattleConditionLookTests` do), `MenuScreenTests` (the summary's page, CALM in its place), the opening a function of time and a button with the level panel's two presses, `ScriptTests`.
- Shots: the `hearts` mode (new, not part of `all`): `lh01_locked_foe`, `lh02_locked_foe_close`, `lh03_reclaim_throw`, `lh04_reclaimed`, `lh05_summary_gauge`, `lh06_breaking_loose`, `lh07_calm`, `lh08_opening`, `lh09_opening_levels`.
- **Done when** the harness shows a locked foe, a reclaim, the gauge, a Pokémon breaking loose and calmed and a heart opened, the same pictures on a second run.

### SD3 · The syndicate: Sinnoh, Kanto and Johto (1 to 2 sessions)
- The style guide's "Rooms" first for the cells' rooms and the head office. Sinnoh's post-game: the first sighting, Looker and the Reclaimer in the Survival Area, the spring's tile at Sendoff Spring, Sinnoh's cell on the Battle Zone's routes and its admin behind the Fight Area quay's containers; then Kanto's cell at Vermilion City's harbour and Johto's at Olivine's port, each after its Hall of Fame. Eighteen locked teams, rooms of ours, `common.LockworkBegun` and its fellows, three requests, the class's looks and songs (rendered for the user's ear), the admins' lines.
- Tests: `HeartsStoryTests` (headless: the first battle, the Reclaimer, every cell's six reclaimed, a cell shut before its Hall of Fame), `StoryTests`, `SceneSkipTests`, `ContentTests`, `MusicTests`, `RequestsTests`; the golden `scripts.txt`, `levels.txt` and `levels-crossed.txt` accepted.
- Shots: `lh10` to `lh29` (each scene, room and admin).
- **Done when** a headless game after Sinnoh's Hall of Fame is given the Reclaimer and reclaims Sinnoh's six, and Kanto's and Johto's cells open after their Halls of Fame.

### SD4 · The syndicate: Hoenn, Unova and the head office (1 to 2 sessions)
- Hoenn's cell at Lilycove City's harbour, Unova's at Castelia City's piers, the head office's floors, the head and the machine stopped, Looker's last scene, the courier of the missed in the lobby; medals of plan 13 · V9 for the first reclaim and the thirtieth heart opened.
- Tests: `HeartsStoryTests` (all thirty reclaimed headless; the courier carries exactly the missed; a missed one reclaimed from him), the suites of SD3.
- Shots: `lh30` to `lh49`.
- **Done when** a headless game reclaims all thirty and stops the machine, and the courier stands with every one missed.

### SD5 · The photo tour: the ride, the judge and the first course (1 to 2 sessions)
- The style guide first ("The photo tour" under the battle's 3D section; "Do and don't" naming the course and the heart's opening among the scenes that show a model); `Models/PhotoTour.cs`, `Models/PhotoJudge.cs`, `Data/photo-tour.json` with the reed river, `Graphics/CourseScene.cs` and `CourseRenderer.cs`, `UI/PhotoTourScreen.cs`, `GameState.PhotoTour`, `ModernUi.PhotoTour.cs`; `SaveData.PhotoTour`; `Op.Tour` and the guide's script.
- Tests (`PhotoTourTests`): a ride is the same twice for a seed and the same throws; a fruit's landing turns the nearest resident's day; a box projected as worked out by hand; points and stars by hand; the film and the fruit counted; the report keeps the best; a save round trip.
- Shots: the `tour` mode (new, not part of `all`): `tu01_guide`, `tu02_ride_start`, `tu03_viewfinder`, `tu04_fruit`, `tu05_whistle`, `tu06_rare`, `tu07_judged`, `tu08_ride_end`; `profile` gains the course.
- **Done when** a ride on the reed river is played to its end in the harness with the same pictures twice, a photograph is judged as a test works it out, and the course is within 8 ms on High.

### SD6 · The photo tour: the courses and the report
- The style guide's "The photo tour" gains the report and its prints first. The other five courses and their residents' days; `UI/TourReportScreen.cs` and `GameState.TourReport` (species by course, stars, the best print redrawn offscreen, the guide's word by band); the rewards; a medal for a four-star photograph.
- Tests: every resident of its course's habitats (`DexEntries.HabitatOf`); every rare action reached by a headless ride with its trigger; every course opened by the report; a print's record names the subjects it redraws.
- Shots: `tu10_report`, `tu11_print`, and `tu20` to `tu25`, each course's rare action.
- **Done when** every course is ridden in the harness with a four-star photograph, and the report shows each as its print.

### SD7 · The delve's rules
- The rulings' "The delve" first (hunger, the plain strike, what moves do, recruits). `Models/Delve/`: `FloorLayout` taken out of `PyramidFloor` behind a golden of a hundred floors, `DelveFloor`, `DelveRun`, `DelveTurns`, `DelveMoves`, `DelveItems`, `Recruits`; a test hollow.
- Tests (`DelveTests`): the Pyramid's floors unchanged; a hollow's floor walkable from its way in to its stairs over a hundred seeds; turns in Speed order; each reach; damage equal to `CalculateDamage` on the same roll; the plain strike landing on a Ghost; hunger's fall and bite; a recruit with the roll fixed; a fallen leader's run keeping nothing; a run played headless to the last floor, the same twice for a seed and the same actions.
- Shots: none (the floors are drawn in SD8).
- **Done when** a run of the test hollow is played headless from its first floor to its last, the same twice for a seed, and every Pyramid floor is what it was.

### SD8 · The delve on screen
- The style guide's "The delve" first (the leader in the player's place, the team's trail, a strike, the cards, the hunger, the log's lines, the moves' reach, the dark); `Player.Avatar` and `WorldRenderer.GatherActors`; `GameState.Delve`, `Core/GameEngine.Delve.cs`, `UI/DelveScreen.cs`, `ModernUi.Delve.cs`; `DelveFloor` in `GameEngine.MapNamed`.
- Tests: the screen's logic (a move's reach, the bag, leaving); a turn played to its end before the next is read; the leader drawn as the avatar (through the `Driver`); the key fuzzer reaches a floor in a game with no window and touches no GPU.
- Shots: the `delve` mode (new, not part of `all`): `dv01_first_floor`, `dv02_trail`, `dv03_strike`, `dv04_reach`, `dv05_foe_down`, `dv06_recruit`, `dv07_hunger`, `dv08_stairs`, `dv09_dark`; `profile` gains a floor with eight Pokémon.
- **Done when** the harness plays a floor: steps, a strike, a foe down, a recruit asked, the stairs and the next floor made from its seed.

### SD9 · The hollow camp, its board and the hollows
- The style guide's "The delve" gains the camp's board first. The camp keeper in the Survival Area, the board drawn from the day's number, ranks, the four hollows (`Data/hollows.json`) with their guardians and songs, recruits brought home (`GiveIds`, `Nuzlocke.Gift`, the randomiser); `SaveData.Delve`; `Op.Delve`; a medal for each hollow cleared.
- Tests (`HollowTests`): every hollow cleared headless by a plain policy; a day's board the same twice; each kind of request played; a recruit home with its id, met place and the run's rules asked; `ContentTests`, `StoryTests`.
- Shots: `dv10_camp`, `dv11_board`, `dv12_rescue`, `dv13_guardian`, `dv14_cleared`, `dv15_recruit_home`, and `dv20` to `dv23`, each hollow's first floor.
- **Done when** each hollow is cleared headless, a day's board offers the same requests twice, and a recruit comes home to the boxes.

### SD10 · The loop catch (optional: a second check judged it low value)
- The style guide's "The loop catch" first. The station of ours and its warden, `Models/LoopCatch.cs`, `UI/LoopScreen.cs`, `GameState.Loop`, `ModernUi.Loop.cs`, `Op.Loop`, the station's tasks and rewards.
- Tests (`LoopCatchTests`): a loop closes round a point; the target's path is the same for a seed; a strike breaks the line; three loops calm it.
- Shots: the `loop` mode (new, not part of `all`): `lp01_station` to `lp06_calmed`.
- **Done when** a wild Pokémon is calmed by three loops in a test and in the harness.

## Risks

- **A catch in a trainer's battle moves the core's end.** It is new ground for `BattleCore`: a battle with no lock and no Reclaimer must run exactly as before, which the two golden corpora hold, and a reclaim battle must replay from its record.
- **No locked Pokémon is lost or stuck.** A missed one goes to the courier; every way the gauge falls works in every region, and a test empties a gauge by steps alone within its bound.
- **The lock's refusals are spread over many places.** One rule, `HeartLock.WhyNot`, and a test that walks each place; a later place that takes a Pokémon (plan 37's facilities, plan 07's trades) asks it too.
- **The delve's grid is a second battle game.** It is kept small: moves do what their fields say, abilities and items do nothing, one action a turn; the rulings list what it leaves out, and nothing of `BattleCore` is touched.
- **Two new scenes of models.** The course and the heart's opening are scenes like the evolution's: one 3D scene a frame, every uniform set by its own renderer each frame, models asked for behind a fade and never by a frame first, targets and meshes counted by the GPU ledger, `profile` and the soak run (plan 16 · T22) for each.
- **Content is most of the work.** Thirty locked teams in five cells, six courses of residents' days, four hollows and a board; each session's notes say what each team and course is built round, all ours.
- **The run's rules.** The randomiser maps a cell's teams on the copy and a recruit's species, the Nuzlocke counts a reclaim and a recruit home as gifts, `CurrentLevelCap` caps the EXP an opening gives, and plan 13's restrictions refuse what they refuse.
- **Shared places.** The finished regions' post-games and ports gain people of ours beside plans 37, 38 and 41's; overlays merge by the hot files' rule, keeping both.
- **Two runs draw the same pictures.** Every draw is on a generator seeded from `Dice` as a ride or a run begins, every clock `FrameClock` or the mode's own; the harness fixes both.

## Needs and gives

- **Needs** (all built): plan 06 (the core, `ThrowBall`, `Disobeys`, `TrainerAi`, the record and the Vs. Recorder's file), plan 09 · L2 (the lasting looks), plan 10 · F1, F2, F5 and F6, plan 12 · Q16, Q17, Q21 and Q30, plan 13 · V4 and V6, plan 14 · B10 and B11, plan 16 · T8 and T9, plan 18 · W4b, W8 and W9, plans 19 to 22 (the five finished regions). Open but not waited for: plan 35's care book and close-up (the gauge falls by them once they exist, by the camp until then) and its `Eating` pose (the tour's eaters bow with `Charge` until then), plan 41's hub (a second door, never the first), plan 43's photo book (where a kept photograph also goes once it exists), plan 17 · N6 for Romanian, plan 26 · V1's catalogue for the voices, the user's ear for the songs.
- **Gives** plan 41 a door for each mode in its hub and Lockwork's head for its gathering of villains if the user wants him; plan 43 `PhotoJudge` and a print redrawn from its record for its photo spots; plan 35 a reason its care lowers a gauge; plans 23, 33 and 34 a cell of Lockwork's in each later region by the same rules; plan 38 `FloorLayout` for the hideaways it lays from a seed (whichever of SD7 and plan 38 · EX6 lands first makes the move out of `PyramidFloor`, behind the golden of a hundred floors); plan 13 · V9 its medals; `Online/PokemonCheck.cs` (plan 07 · O4) a line refusing a locked Pokémon.
- **Lanes**: SD1 in lane 3 · Rules, in a wave with no other session that changes `BattleCore`; SD2 in lane 4 · Battle on screen after SD1; SD3, SD4 and SD9 in lane 1 · Story; SD5, SD6 and SD8 in lane 5 · Field and people; SD7 in lane 3 or beside (new files, and B10's floor behind its golden); the optional SD10 in lane 6. Everything they need is built, so SD1, SD5 and SD7 can start in the first wave after the plan is indexed, side by side. At most two sessions adding script commands share a wave (`heart`, `tour`, `delve`, `loop`).

## Decisions for the user

1. **The three modes and their order.** *Recommended:* all three, the locked hearts first (the most value), the photo tour and the delve beside them. The alternative builds the locked hearts alone and leaves the delve (three sessions and a second game of turns) out.
2. **Where the locked hearts' story runs.** *Recommended:* at the five finished regions' ports, a cell after each Hall of Fame and the head office in Castelia City, so the story sends the carried team back through the regions it won. The alternative is a small region of ours reached by a side link (`Region.Side` and `RegionLink.Kind`, written by whichever of plan 41 · EG1 and plan 23 · Z17 or Z21 lands first), drawn by hand with `tools/MapDraw`: two or three sessions more.
3. **How many locked Pokémon.** *Recommended:* thirty, six a cell, each of its region's own games, none legendary or mythical. The alternative is fifty with a legendary at each cell's end, which plan 03 · D12c's one of each would have to allow.
4. **Where a heart opens.** *Recommended:* at once, wherever its gauge empties. The alternative is only at the spring, so Sendoff Spring is a place to come back to (one flight with Fly between regions).
5. **How a delve fights.** *Recommended:* on the floor's grid, a turn a step, with the moves' reach and the game's own damage. The alternative fights each meeting on the battle screen: a session cheaper, but a second Pyramid.
6. **What a delve gives.** *Recommended:* a recruit that reaches the end may come home to the boxes. The alternative sends recruits off at the end, with items, money and rank the only rewards.
7. **Where the tour and the camp stand.** *Recommended:* the tour's guide on Pastoria City's observatory floor once the National Pokédex is had, the camp in the Survival Area after Sinnoh's Hall of Fame, and plan 41's hub a second door for each. The alternative puts both in plan 41's hub alone, which waits for it.
8. **The loop catch.** *Recommended:* not built; SD10 stays optional. The alternative builds it after SD9.
9. **The words.** *Recommended:* as written (Lockwork, the Reclaimer, a locked Pokémon, the photo tour, the delve and its hollows). The alternative is the user's own, chosen before SD3 writes its lines.
10. **Who gives the Reclaimer.** *Recommended:* Looker (Platinum's own, plan 02), who has tracked the syndicate and has his bureau make the device from a part seized at the quay; it ties to plan 41's agency and leaves no defector who built the machine. The alternative is a figure of ours with no tie to the syndicate's making (a ferry-keeper who carried its crates and learned the spring's rite from an old tale), at the cost of one more person to write.

## Status

- [x] SD1 Locked hearts: the lock and the battle (2026-11-12, as built below)
- [x] SD2 Locked hearts on screen (2026-11-12, as built below)
- [x] SD3 The syndicate: Sinnoh, Kanto and Johto (2026-11-12, as built below; what it owes is listed there)
- [x] SD4 The syndicate: Hoenn, Unova and the head office (2026-11-12, as built below; the courier and the medals owed)
- [ ] SD5 The photo tour: the ride, the judge and the first course
- [ ] SD6 The photo tour: the courses and the report
- [ ] SD7 The delve's rules
- [ ] SD8 The delve on screen
- [ ] SD9 The hollow camp, its board and the hollows
- [ ] SD10 The loop catch (optional)

## SD1 to SD4 as built (2026-11-12)

The user's answers held (39a to 46a, 41c: Lockwork, the Reclaimer, Orsolya Keld the clockmaker with her **Ampharos**, the
five admins Tamsin Crake, Dov Harlan, Elspeth Moor, Rafe Ottoline and Juno Barrow, the thirty species as listed, the
spring's rite of water over the ball, Looker the giver, the grey glass tower; 63a the grey drifting haze; 65a's march is
owed, below). The rulings' "Locked hearts" has the numbers.

- **SD1.** `Models/HeartLock.cs` (an immutable record: id, gauge, held EXP; `MovesBack`, `BarsLeft`, `Lower`, `Hold`, `Open`,
  `WhyNot`; `HeartsRecord`, `SaveData.Hearts`, null while empty), `Pokemon.Lock` (saved, copied, held by the copy test),
  `Ruleset.LockedHearts` (modern only), `Battle/Sim/BattleCore.Locks.cs` (the reclaim through `ThrowBall` and `Reclaim`,
  the place emptied by `Withdraw` and the roster, `RollKind.Lock`, `ChoiceKind.Calm` with the items and switches, `Unruly`
  after `Disobeys`, `Stir`, `LooseBites` at the turn's end, `HoldsTheExp` in `Give`, `SealedByTheLock` in `WhyNotMove`,
  `CountLockKnockOut`), four events (`LockShown` on every send-out through `Brought`, `BrokeLoose`, `Calmed`, `ExpHeld`) with
  their wire names, `CoreSetup.Reclaimer` through `BattleSetup`, `BattleRecord`, the Vs. Recorder's file, the retry and a
  battle watched. The face takes a reclaim aside (`BattleEngine.Reclaimed`) and the game takes it in at `EndBattle`
  (`GameEngine.Hearts.cs`: ids, the Pokédex, the Nuzlocke's gifts clause, the team or the PC), notes the knocked out as
  missed, and lowers the team's locks for the battle, its knock-outs, every 128 steps and the camp's pat and meal; a heart
  whose gauge has emptied opens once the field is free (`KeepTheHearts`). Refused at the Day Care (the script's result 5),
  Poké Jobs, the Frontier's `MayEnter`, the Pokéathlon, the contests, the Home box, TMs and tutors, Rare Candy, evolution,
  friendship and `GainExp`. The Reclaimer is item 9480 of `Overrides/items.json`. `HeartLockTests` hold it all; both golden
  corpora held still.
- **SD2.** The haze is a `ConditionLook` mark (`Haze`, `Loose`, `LockBars`; `LockRulesHave`, `SettleLock`) drawn by
  `MoveFx.LockHaze` (seven quads), the padlock after a name (`ModernUi.LockAfter`, in `NameWithGender`, so the HP box and
  the party's cards), the summary's gauge where the EXP bar is and its sealed moves, CALM where RUN stands
  (`BattleEngine.OffersCalm`, `SelectCalm`), the opening's scene (`UI/HeartOpeningScreen.cs`, `GameState.HeartOpening`,
  `ModernUi.Hearts.cs`, the level panel's two presses), the script command `heart` (`has`, `give`, `spring`, `open`,
  `locked`, `count`, `missed`, `opened`), the harness's `hearts` mode (`lh01` to `lh09`, looked at), `HeartLookTests`.
- **SD3 and SD4** (one pass): twenty-six trainers of ours in `our-trainers.json` (four agents and an admin a cell, the
  head), each locked member marked `"lock"`; the agents stand as overlay people of their port (shown by the region's Hall
  of Fame flag), Unova's on the hand-made `CasteliaCity`; each admin in a room of ours (`LockworkCellSinnoh`, `Kanto`,
  `Johto`, `Hoenn`, behind an overlay exit opened by the Hall of Fame flag; the Sinnoh cell's at the Fight Area's south
  quay), Unova's in the tower's lobby (`LockworkTower1F`, a door at 54, 5 of Castelia's avenue) and the head on its second
  floor (`LockworkTower2F`), whose battle stops the machine (`FLAG_LOCKWORK_MACHINE_STOPPED`) and brings Looker's last
  scene. Looker gives the Reclaimer in the Survival Area; the spring's rite is read at Sendoff Spring's water (30, 31). The
  looks (`lockwork_agent_m`, `_f` and one each for the admins and the head) and the classes Lockwork Agent, Admin and Head
  are in `characters.json`, `classes.*` in English and Romanian, the admins' and the head's lines in `trainer-lines.json`.
  `HeartsStoryTests` hold the thirty and the head's, every lock reclaimed in its trainer's battle, who waits for which Hall
  of Fame, Looker's gift once, the rite once a day and the head waiting on the admin.
- **Owed** (each with its session): the courier in the tower's lobby with the missed (a battle whose team is built from
  `HeartsRecord.Missed`, which needs a host hook of its own); the medals of plan 13 · V9 for the first reclaim and the
  thirtieth heart; the requests of plan 12 · Q21 for each cell; the class's own eye and battle songs (65a, a clipped
  march: the class plays Team Galactic's for now, `sound-map.json`); the follower's field sprite tinted while locked;
  `lh10` to `lh49`'s shots of the cells and the tower; a headless game walked to each cell (the tests play the scripts and
  the battles on their own). The cells' trainers are spoken to, not walked up to by sight.
