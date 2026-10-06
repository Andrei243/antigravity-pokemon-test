# Plan 02 · The story, start to finish

**Goal**: Platinum's whole story, from the TV in Lucas's bedroom to the Hall of Fame and the post-game, paced the way the original paces it: HM obstacles that open the map as you earn badges (trees to Cut, rocks to Smash, boulders to push, water to Surf, fog to Defog, walls to climb, waterfalls), story blockers (Team Galactic grunts, the Psyduck on Route 210, the locked Valley Windworks), key items, the rival, Team Galactic's plot and the legendaries.

## Where we are

- *Since S1 (2026-10-05):* the story has its state and its scripts. `Story/StoryState.cs` holds flags, variables, beaten trainers, taken items, badges and the starters, and is saved; scripts are text files in `Data/scripts/` ([`docs/scripts.md`](../scripts.md) is the language's reference), run over the field by `Story/ScriptRunner.cs`, with a question box, scripted walks, bubbles, fades, the camera, battles and the screens that exist. What a nurse, a clerk, a PC, the briefcase, a trainer and a signboard do is written in `common.txt`; people can be hidden by flags; tiles can start scripts. No chapter of the story was written then (`common.NewGame` set nothing, and the only scripts of a place were Jubilife's three quiz clowns); S4 wrote the first.
- `Core/GameEngine.cs` switches between overworld, dialogue, battle and menu states. NPCs (`Overworld/NPC.cs`) have lines of their own or a script, and can be trainers (line of sight, "!" and a battle), a nurse, a clerk, a PC or the starter briefcase.
- *Since S4 (2026-10-06):* the first chapter is played. A new game begins in the player's room with nothing, to the end of a television special on Professor Rowan; the rival, named in the introduction, bursts in; the professor's briefcase on Route 201 gives the first Pokémon and the rival battles at once; Mom gives the Running Shoes (running needs them now), Cyrus speaks to Lake Verity, the lab gives the Pokédex (the start menu shows the Pokédex and the team only once the player has them), the professor a TM, Mom the Journal and the rival's mother his parcel, and the assistant's catching lesson on Route 202 gives five Poké Balls.
- *Since 2026-10-05, after S1:* items lie on the ground. The 37 item balls and 21 hidden items of the open areas are where Platinum has them, with what Platinum puts in them (the Potion by Route 202's grass, a Poké Ball on Route 203, TMs in Oreburgh Gate's cellar, a Rare Candy in Floaroma Meadow), and every area opened later brings its own.
- *Since S2 (2026-10-06):* the field moves work, from the party menu and from what the player faces, with Platinum's badges: trees are cut, rocks smashed, boulders pushed, Wayward Cave lit, the water surfed, waterfalls and rock faces climbed, and Fly, Teleport, Dig and Sweet Scent do what they do. The Bicycle is ridden (and the Cycling Road's gates let only riders through), the rods fish, an Escape Rope leads out of the caves, a key item can be kept on the item button, and the Pokétch has its clock, pedometer and team. Nothing gives any of them yet: that is the chapters'.
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

**Outcome of S1 (2026-10-05).** All of it is in, and one place's scripts beside the common ones.

- **State** (`Story/StoryState.cs`): flags, integer variables, the trainers beaten, the items taken, the badges (one bit each, in the Trainer Card's order, which is the original's numbering: Coal, Forest, Cobble, Fen, Relic, Mine, Icicle, Beacon) and the player's and the rival's starters (he takes the one that beats the player's). The save has `StoryVersion`, `StoryVariables`, `TakenItems`, `PlayerStarter` and `RivalStarter` beside the `StoryFlags`, `DefeatedTrainers` and `Badges` it had. `Story/StoryMigration.cs` brings an older save up to date: version 0 (before the story was kept) is given what a new game starts with and has its starter read off its Pokémon. Key items are not a list of the story's: they are in the bag, and a script asks for one like any item (`if item "Works Key"`).
- **Scripts**: `Data/scripts/<place>.txt` and `common.txt`, in a small line-based language ([`docs/scripts.md`](../scripts.md)): 44 commands for text, questions and menus, jumps and calls, flags and variables, items, Pokémon, badges and money, trainer and wild battles, facing, walking, bubbles, showing and hiding people, warps, fades, pauses, the camera (pan, release, shake), music, fanfares and sounds, and the four screens that exist; 20 questions an `if` can ask. `ScriptParser` tells anything wrong with its file and line; `ScriptLibrary.Problems` lists names that mean nothing (an item, a species, a song, a map).
- **Running them**: `ScriptRunner` and an `IScriptHost`. In the game a script has the field to itself (`GameEngine.Scripts.cs`); in tests `HeadlessScriptHost` plays one to its end with messages confirmed as they come, answers queued and battles decided by the test, which is the "story walk" the chapters ask for.
- **Triggers**: talking (a person's own `script`, or the common one for what they are), signboards (plain, or with a script), tiles stepped on (`triggers` in a map file; an overlay binds the original's triggers by number and keeps their variable and value), and arriving (`OnEnter` in the place's file). Using an item or a field move as a trigger is S2's.
- **Who is on the map**: `hiddenBy` and `shownBy` on a person. The world's people take the original's hiding flag from the area file, so the eight of the 87 people placed so far whom the original hides at some point (Rowan and his briefcase on Route 201, the assistant in Sandgem and Jubilife, Looker, the three clowns) already carry theirs; nothing sets those flags yet, so everyone is where they were.
- **The question box**: Yes and No, or up to six answers, beside the text box (style guide, "Questions in the field").
- **What exists, as scripts** (`common.txt`): `Nurse` (she asks first now, and heals on a yes), `Healer` (Mom: her own words, then the team is well), `Clerk`, `PC`, `Briefcase`, `Attendant`, `Trainer` (the challenge, the battle, the last word afterwards), `Talk`, `Sign`, and `NewGame`, which is empty until S4.
- **A place's own, ahead of S5**: Jubilife's three clowns ask their questions with a real yes or no and give Coupons 1 to 3 once each (`scripts/jubilife_city.txt`, on the original's flags `FLAG_RECEIVED_COUPON_n`).
- **Checked by** 103 tests (`ScriptTests`, `StoryTests`; 1,909 in all): the grammar and every command, the runner's every branch, saves and the migration, presence, triggers, the question box, scripted walks, and the game's own files (every script belongs to a place, is started by something, names only what exists, and is played to its end on every way through it: each answer, each battle won and lost). The harness's `story` mode shows them in the game.

**Decisions taken in S1.**

1. **Scripts are plain text, a command to a line**, with the original's own words for the commonest things (`setflag`, `setvar`, `goto`, `call`, `end`), so a scene can be written with the decompilation's script open beside it. Not JSON: a scene is read from top to bottom.
2. **The runner decides, the host shows.** Nothing about where a script goes next is in the game's code, so a script does the same in a test as on screen.
3. **A script runs only while the game is in the field.** Text, battles, screens and fades are waited for by the state they put the game in, which is why none of them needed a second way of pausing.
4. **Roles stay, behaviour moved.** `isHealingNurse` and the rest still say what someone is (tests, the renderer and the importer read them); what they *do* is the common script of that name, and a `script` of their own wins.
5. **Hidden by a flag as the original has it** (`hiddenBy`), plus `shownBy` for our own people. People who appear later start hidden: their chapter sets the flag in `common.NewGame` and clears it in their scene, as the original's new-game script does.
6. **A trigger goes by a variable and a value**, as the original's do; the overlay only says which script.
7. **`OnEnter` runs on every arrival**, also from a save, so it asks before it acts.
8. **A battle lost ends the script** and the player wakes up in the last Pokémon Center (at home before any, since plan 06 · R10); `battle ... canlose` is for the battles the story lets the player lose.
9. **The cancel button answers No, and picks the last entry of a menu.**
10. **A script's names mean the people of its own place.** The map of Sinnoh has a `clown_1` in more than one town.
11. **The nurse's lines are new.** She had four lines close to the original's wording; she now has five of our own and asks before she heals.
12. **Old saves**: a save with no `StoryVersion` is taken to have chosen the starter it has (grown or not), or Turtwig, which every game was given.

**Left for later, on purpose.**
- Item balls and hidden items: the state is there (`TakenItems`, `if taken "..."`), but nothing is placed on the ground, because a ball needs its sprite and its common script (S2, with the obstacles, or S4 with the first one). **Done on 2026-10-05, at the user's request, ahead of S2** (below).

**Items on the ground (2026-10-05, after S1).**
- **Where they come from**: the map importer reads what each item ball's script sets (`scripts_visible_items.s`) and the table of hidden items (`hidden_items.h`), and writes the item and the number into the area's file beside the object or the sign. A hidden item's number is its flag's place in the original's list of flags, which has gaps the table lacks: reading the table by position gave an Oreburgh City with no Heart Scale.
- **In the game**: an item ball is someone of the map (`NPC.IsItemBall`, drawn as a Poké Ball 18 texels across; style guide, "Props"), in the way like a person. Speaking to it runs `common.ItemBall`: its own flag is set, which takes the ball off the map for good, and what was in it is found (`find own`: the item fanfare, "found the Potion!", where it was put, and for a TM the move it holds). A hidden item is `Map.HiddenItems`: looking at its tile runs `common.HiddenItem` once.
- **No list of its own in the save**: a ball is gone because its flag is set, as in the original (`FLAG_OBTAINED_ROUTE_202_POTION`), so `TakenItems` stays for whatever a later chapter gives no flag to.
- **Decisions**: every ball looks the same whatever is in it, as in Platinum; a hidden item is found by looking at its tile, never by standing on it; the bag is never full, so nothing is left lying. The Works Key's ball in Floaroma Meadow is no item ball (its script is the meadow's own) and waits for S6.
- **Not yet**: the Dowsing Machine (the ranges are imported; the Pokétch is S2's), and berries' soft soil.
- ~~A wild battle a script starts can be run from until plan 06 · R9 gives battles a "can't flee".~~ Done in R9 (2026-10-06): `wildbattle "Giratina" 47 nofleeing`, and with it `battle a and b with c` (two trainers, a partner), `battle self first` (the rival's first battle) and `catchinglesson "Bidoof" 2`.
- The rival's name and `{rival}` in lines (S4, see below).
- People don't wander: the area files keep each object's `movement` (`wander_around`, `look_south`…) and nothing reads it yet. It belongs with the first chapter that has a town full of people (S4), not with the scripts.
- A badge has no fanfare of its own (`fanfare` knows `heal`, `item`, `pokemon`, `levelup`): plan 05.
- The nurse's machine doesn't light up; the heal is a fanfare and a line.

### S2 · Field moves, obstacles and key items
Party-menu field moves, the badge checks, obstacle objects (cut tree, cracked rock, boulder, fog, darkness), Surf, Waterfall and Rock Climb hooked to plan 01's movement states, the Bicycle, fishing rods, a Key Items pocket, the Pokétch (clock and party apps first). Tests for each gate: blocked without the move or badge, open with both.

*Ready from S1:* the question ("Would you like to use Cut?") is `ask` in a script, with `if knows "Cut"` and `if badge forest` for the gate, and the common script an obstacle runs is one more entry of `common.txt`; an obstacle that gives way and comes back with the map is `hide` (it holds until the map is come to again). What is missing is the trigger: facing a prop and pressing the button, and choosing a move from the party menu, start no script yet.

*Ready from plan 01 · M3:* the movement states and their rules (`Overworld/FieldMovement.cs`): surfing with its start from the shore and its landing, waterfalls up and down, rock faces, the Bicycle's pace and where it can't go. Today each asks only that a party Pokémon knows the move (`FieldMovement.MovesOf`): the badge checks go there. Surf starts on the confirm button at the water's edge with no question asked; the party-menu way of using a move, and the yes-or-no, are this session's. The three obstacles are props that are drawn and block (`CutTree`, `CrackedRock`, `StrengthBoulder`, placed from the import); making them give way means removing the prop and rebuilding the chunk's scene. The Bicycle has rules but no item, no sprite and no key.

**Outcome of S2 (2026-10-06).** All of it is in; what gives the moves and the items is the chapters'.

- **The rules** (`Overworld/FieldMoveRules.cs`, GPU-free): the fifteen field moves of Platinum's party menu (`FieldMove`; Chatter is left out, `docs/mechanics/rulings.md`), the badge each asks for (Platinum's, Relic for Defog and Fen for Surf), and the original's check of where each can be used (`Check` on a `FieldSpot`: the obstacle or tile faced, water, a dark place, fog, a place one may fly from, a way out of the caves, a partner, what is in force already), with the original's reasons as our own words (`Why`).
- **Obstacles are things of the map**, as the original's are: the 18 small trees, 65 cracked rocks and 2 boulders of the open areas (`NPC.Obstacle`, `Map.AddObstacle`) are drawn as cards and stand in the way like a person. Each carries the original's flag that hides it once cleared, made its area's own (`FLAG_MAP_LOCAL_HIDE_OBSTACLE_1_ETERNA_CITY`); local flags are cleared whenever the player comes to another place (`StoryState.ClearLocal`), so trees and rocks are back on the next visit and pushed boulders stand where they stood.
- **Facing one** runs its common script (`CutTree`, `Rock`, `Boulder`): our own words for what it is, and a question when a Pokémon of the team knows the move and the badge allows it. The tile ahead does the same as the original's `Field_TileBehaviorToScript`: a rock face (`RockFace`), a waterfall from the water (`Waterfall`), deep water at the player's feet (`Water`, offered only to someone who may surf: Surf no longer starts without a question).
- **The party menu** lists each Pokémon's field moves under SUMMARY and SWITCH, in the order of its moves (`PartyScreen.ActionsFor`); a move that can't be used here says why, and one that can closes the menu and runs `common.Use<Move>` for that Pokémon. Soft-Boiled and Milk Drink choose a Pokémon to share HP with, in the menu itself.
- **Using a move** is `usemove` in a script: "{user} used Cut!", then the cut-in (`UI/FieldCutIn.cs`; style guide, "A field move's cut-in": the Pokémon on a band across the screen with its cry), and as the band closes the obstacle gives way (leaves, or a cloud of chips; `FieldLife.GiveWay`). Strength puts every boulder of the place within reach of a push (the original's slow walk, the player leaning in), until the player leaves; Flash lights a dark cave and Defog lifts the fog, until the player goes somewhere that isn't a cave (story flags, `FLAG_STRENGTH_ACTIVE`, `FLAG_FLASH_ACTIVE`, `FLAG_DEFOG_ACTIVE`). Surf rides out, Waterfall and Rock Climb climb (`Player.Climb`: the movement rules let one up only after a yes, `Walker.Climbing`; down a waterfall needs the move and no badge, as in the original). Fly opens the map of the towns arrived in (`UI/FlyScreen.cs`; the original's twenty fly spots, `Data/SpawnLocations.cs`), Teleport goes back to the town of the last Pokémon Center gone into, Dig and an Escape Rope out of the caves to where the player went in, and Sweet Scent draws out a wild Pokémon of the place.
- **Key items**: the bag uses the Bicycle, the rods and an Escape Rope in the field (`BagScreen.TakeFieldUse`) and keeps one key item on the item button (REGISTER; C, saved). The Bicycle has its rules (`Overworld/Bicycle.cs`: where one may ride or get off), its music, its bell and its gears on the run button; the Cycling Road's gates (`"bicycle": true` on an overlay's passage) send anyone on foot back (`common.CyclistsOnly`) and keep a rider on the Bicycle until the next warp. The rods (`Overworld/Fishing.cs`) cast, wait, bite and land by the original's timings, from each area's three rod tables, which the importer now writes (`oldRod`, `goodRod`, `superRod`). The Key Items pocket was there already.
- **The Pokétch** (`Models/Poketch.cs`, saved): given by `poketch on`, its apps by `poketchapp`, shown over the field on P with O for the next app (`UI/PoketchView.cs`; style guide, "The Pokétch"): the digital watch, the pedometer (every step counted while it is on the wrist) and the team's app.
- **Scripts**: `usemove`, `surf`, `climb`, `fly`, `teleport`, `escape`, `sweetscent`, `poketch on`, `poketchapp`, the question `poketch` and `{user}` in lines ([`docs/scripts.md`](../scripts.md), "Field moves and key items"), with both hosts.
- **Checked by** `FieldMoveTests` (every gate blocked without the move or the badge and open with both, the obstacles' scripts on their maps, the boulder's push, the climbs, Flash and Defog and how long they last, the party menu, Soft-Boiled, the cut-in, the fly spots, Fly's map, Teleport, Dig and Sweet Scent, the Bicycle's rules and the Cycling Road's gates, the bag's field use and the item button, fishing, the rod tables, the Pokétch, the new commands' errors), and the story's own tests play every new common script to its end on every way through it. The harness's `fieldmoves` mode shows them in the game (`fm01` to `fm26`).

**Decisions taken in S2.**

1. **An obstacle is a thing of the map, not a prop**, as in the original: it is found, talked to, hidden by its flag and pushed like someone of the map, and nothing of the scenery is rebuilt when one gives way.
2. **What lasts only in a place is a local flag**, cleared when the player comes to another area or through a warp, as the original clears its local flags when the map's header changes. One map of the world holds many areas, so an obstacle's flag is made its area's own.
3. **The question asks; the movement rules only follow.** The badge is asked by the script and by the menu, never by `FieldMovement`, which asks that the move is known and, for a climb up, that the player said yes.
4. **Surf asks.** M3's Surf on the confirm button with no question is gone: facing deep water asks, as the original does.
5. **The party menu checks before it closes**, so a move that can't be used says why without leaving the menu, and the script that runs can take the check as done.
6. **The cut-in plays the Pokémon's cry**, as the original's does, and the obstacle gives way as its band closes, not after it.
7. **The Pokétch shows three apps**: the clock, the pedometer and the team. The rest are kept in the save as they are given and wait for what they show.
8. **Partners never travel along yet**, so the partner check is written and never fires; the chapters with Cheryl, Mira, Riley and Buck turn it on.

**Left for later, on purpose.**
- Who gives what: the HMs, the Bicycle, the rods, the Pokétch and its apps are the chapters' (S5 to S13).
- The Bicycle isn't drawn: the player keeps walking pictures at its pace until a riding sprite is made (plan 04).
- ~~The whiteout still wakes the player at home~~: since plan 06 · R10 it wakes them in the last Pokémon Center, the spot Teleport goes to.
- Feebas's six tiles in Mt. Coronet's lake, with the lake's floor (plan 01).
- The Pokétch's other apps (the Dowsing Machine with the hidden items' ranges, the Day-Care Checker with the Day Care, the Marking Map with the roaming legendaries).
- Deep snow's slow walk and the mud slopes wait for the areas that have them (plan 01 · M7 and M8).

### S3 · Moved to plan 06
Double and tag battles, trainer AI, scripted wild battles, whiteout money, the EXP Share, the move-learning prompt and cancelling evolution are now [plan 06](06-game-mechanics.md) · R9 and R10. S5's tag battle in Jubilife needs R9 first. The number S3 stays unused so the chapters keep theirs.

### S4–S15 · Chapters
*Decided in plan 06 · R1 (2026-10-04):* Mega Evolution, Z-Moves, Dynamax and Terastallization come into Sinnoh's story if they can be balanced there, and after the Hall of Fame if not (`docs/mechanics/rulings.md`, "The later mechanics in the story"). The proposal to confirm chapter by chapter: each is shown by a boss and handed over after that battle (Mega Evolution with Maylene in S8, Z-Moves at Celestic Town in S9, Dynamax with the Distortion World in S12, Terastallization at the League's door in S14), and every boss after that point uses what the player has. A chapter written before its mechanic exists (plan 06 · R20–R23) leaves the beat out and notes where it goes. Also from R1: a game is played by Platinum's rules or the modern ones, chosen at its start, so a story-walk test that depends on a battle's numbers names its rules.

Follow Bulbapedia's Platinum walkthrough parts. For each chapter: port the scripts of its maps, place its trainers and items, set the flags that open the next area, and add a headless "story walk" test that plays the chapter's scripts with forced battle wins and checks the flags, items, badges and party at the end. Needs the chapter's areas from plan 01 and the species from plan 03.

  *Done ahead of S4 in plan 04 · G10 (2026-10-04):* the new-game introduction (`UI/IntroScreen.cs`: Rowan's welcome, the Pokémon out of its ball, boy or girl, the name, the send-off; our own lines on the original's beats) and who the player is (`Core/PlayerIdentity.cs`, saved). The character the player didn't choose is the professor's assistant, and written lines say `{player}` and `{assistant}` instead of a name (`docs/data-files.md`). *Ready from S1 (2026-10-05):* everything a scene is made of ([`docs/scripts.md`](../scripts.md)). For each chapter: write `scripts/<area>.txt`, give the overlay's people their `script`s and bind the area's triggers (`"triggers": [ { "trigger": 0, "script": "..." } ]`; the area file lists them with their variables), set the flags that hide the chapter's later arrivals in `common.NewGame`, and write the chapter's story walk with `HeadlessScriptHost`. If the chapter changes what a save already in play must know, raise `StoryState.CurrentVersion` and add the step to `StoryMigration`. The briefcase still makes the Pokémon chosen the whole team, as it did before S1, because a new game still starts with a Turtwig: both go together in S4.

- **S4 · Lake Verity and a Pokédex** (part 1): new-game intro with Professor Rowan (gender and name), the Twinleaf TV, Barry, Cyrus at Lake Verity, the briefcase and the wild Starly on Route 201, the first battle with Barry, the Pokédex in Sandgem, Running Shoes from Mom. Remove today's free Turtwig.
  *Done on 2026-10-06.* What was built, in the original's order (the beats read from its scripts, the lines our own):
  - **The rival's name** is the introduction's last question (`IntroPhase.AskRival`, `EnterRival`, `ConfirmRival`: he steps in beside the professor, and the same `NameEntry` takes his name, Platinum's own if none is typed, `PlayerIdentity.DefaultRivalName`). It is kept in `PlayerIdentity.RivalName` and the save (`SaveData.RivalName`), and written text says `{rival}`, people's names and speakers included. His teams are Platinum's (`trainers.json`'s `Rival` class, named after the starter the *player* took, as the original names them); a record of that class is named `{rival}` as it is filled (`TrainerDatabase.Fill`), and a script fights someone with such a team by `battle rival as "rival_route_201_turtwig"`.
  - **A new game** starts in the player's room (`PlayerHouse2F`, a hand-made room with a bed, a computer and the television; the props `Bed` and `Computer`) with no Pokémon, an empty bag and no Pokédex: Sinnoh's `RegionDatabase` start moved there and the free Turtwig is gone (Kanto's stand-in start keeps it). The Running Shoes are `FLAG_HAS_RUNNING_SHOES` (`Player.HasRunningShoes`, kept in force with the field moves), the start menu shows POKéDEX only with `FLAG_HAS_POKEDEX` and POKéMON only with a team (`StartMenu.HasPokedex`, `HasPokemon`), and the briefcase adds the Pokémon chosen to the team instead of replacing it.
  - **The scenes** (`scripts/PlayerHouse2F.txt`, `PlayerHouse.txt`, `RivalHouse.txt`, `RivalHouse2F.txt`, `twinleaf_town.txt`, `route_201.txt`, `verity_lakefront.txt`, `lake_verity_low_water.txt`, `sandgem_town.txt`, `RowanLab.txt`, `route_202.txt`): the television's special as the game begins; the rival up the stairs at the first step; Mom downstairs and her warning at the door; the guitarist turning the player back until the rival has left home; the rival running out of his door into the player; his room, where he is packing; Route 201, where the professor stops them at the grass, asks (until he hears yes) whether they love Pokémon, and the assistant brings the briefcase; the choice, the rival's choice of the one strong against it and the battle at once (`first`, `canlose`); home, healed, to the Running Shoes; the rival off to the lake, the roads home and to Sandgem turning the player back; the lakefront; Lake Verity, where Cyrus speaks to the water (the camera sent to him), something cries out and the rival races off; the assistant meeting the player in Sandgem and the rival bumping out of the lab door; the Pokédex (asked until the answer is yes) and the assistant's own first Pokémon, `{assistantstarter}`; TM27 from the professor at the door and the assistant's tour of the Pokémon Center and the Mart; Mom's Journal and the rival's mother's parcel; and Route 202's assistant, who sends the player home without it. Two themes of our own play in them, the professor's and the assistant's (`music/common/rowan.mml`, `assistant.mml`), and Cyrus has a look of his own (`CharacterStyle` `CYRUS`). The rival's room is a hand-made room too (`RivalHouse2F`).
  - **Ahead of S5**, because the parcel leads straight into it: the catching lesson on Route 202 (the assistant catches a Bidoof in a battle that plays itself, plan 06 · R9, and gives five Poké Balls).
  - **Saves from before**: `StoryState.CurrentVersion` is 2. A save of version 1 began with a Turtwig, a Pokédex and the Running Shoes in front of the house, so `StoryMigration.PastTheOpening` runs `common.OpeningDone`, which sets what the chapter leaves behind (`OpeningTests` holds that it is exactly what playing it leaves). `common.NewGame` ends in `return` now, so a script that calls it goes on.
  - **Tests**: `OpeningTests` plays the chapter in order on maps of its own, starting each scene as the game does (a step onto its trigger, arriving, talking), with each starter and the first battle won and lost. `WorldWalk.HiddenAtStart` lets the walks pass people a new game hides until their scene. The harness's `opening` mode shows the scenes.
  - **Decisions**: Platinum, not Diamond and Pearl: there is no wild Starly on Route 201, the first battle is the rival's (the lake's `STARLY` flag is the original's leftover and stays set). In the original the rival walks behind the player to the lake; with no one following the player yet, he runs ahead and waits at the lakefront (`FLAG_RIVAL_WAITS_AT_LAKEFRONT`; `docs/mechanics/rulings.md`, "The first chapter"). The assistant takes the player to the lab door behind a fade instead of walking them there. The television's special is lines under "TV" over the bedroom, not the original's caption before the field. The professor's question whether to give the Pokémon a nickname is left out until Pokémon can be named. Lake Verity is the map the original uses for this visit (`lake_verity_low_water`, matrix 101) and its TM38, reached only from the lake's later map, is held back; the other map waits for S10.
  - **Left for later**: someone following the player (the rival to the lake, later partners); a Pokémon's nickname; the Journal does nothing yet; the parcel goes to the rival in S5; people still don't wander.
- **S5 · Jubilife and Roark** (parts 2–3): Route 202 catching lesson (*done in S4*), Jubilife (Pokétch coupons, Trainers' School, the Galactic grunts and the tag battle), Route 203 and Barry, Oreburgh Gate (Rock Smash), Oreburgh Mine, the **Coal Badge**, Ravaged Path, Floaroma Town and Meadow.
- **S6 · Windworks and Eterna** (parts 4–5): Works Key, Valley Windworks and Commander Mars, Route 205, Eterna Forest with Cheryl, Eterna City (HM01 Cut from Cynthia), the **Forest Badge**, the Galactic building and Commander Jupiter, the Bicycle, the Old Chateau.
- **S7 · Hearthome and Solaceon** (parts 5–7): Cycling Road, Wayward Cave with Mira (optional), Route 207, Mt. Coronet, Route 208, Hearthome and Amity Square, the **Relic Badge**, Route 209, the Lost Tower, Solaceon Town and Ruins (HM05 Defog), the Psyduck blocking Route 210.
- **S8 · Veilstone and Pastoria** (parts 8–10): Route 215, Veilstone, the **Cobble Badge**, the Galactic warehouse (HM02 Fly), Route 214, Maniac Tunnel, Valor Lakefront, Route 213, Pastoria and the Great Marsh, the **Fen Badge**, Route 212.
  *Ready from plan 01 · M7 (2026-10-06):* the places are open, with their trainers and our own lines for their people, and the Great Marsh's Safari Game is played (`SafariGame`, `safari start`/`end`). Left for this chapter: Maniac Tunnel, which the Ruin Maniac digs on toward Solaceon once enough kinds of Unown are seen (his short cave, `RuinManiacCave`, is open; the tunnel's areas, its way into Solaceon Town and the Rare Candy over the ruins are held back), the warehouse, the lakefront's people and the drained lake, and the story's people on the routes.
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

- [x] S1 Story state and scripting (2026-10-05)
- [x] S2 Field moves, obstacles and key items (2026-10-06)
- S3 moved to plan 06 (R9, R10)
- [x] S4 Lake Verity and a Pokédex (2026-10-06)
- [ ] S5 Jubilife and Roark
  - 2026-10-01, ahead of S1–S3: Jubilife City is on the map (`Data/maps/JubilifeCity.json`, `TrainersSchool.json`, `PoketchCompany.json`, `JubilifePokemonCenter.json`, `JubilifePokeMart.json`), north of Route 202, with the Trainers' School (two School Kid trainers, Barry), the Pokétch Company, its own Pokémon Center and Poké Mart, Dawn at the entrance, Looker, and the three campaign clowns. Without story flags the clowns only ask their questions and no Pokétch is given yet; the TV station, Global Terminal and condominiums have no doors. Routes 203, 204 and 218 aren't built, so a sign and an NPC close each road out. The Galactic grunts and the tag battle with Dawn come after the Coal Badge in Platinum, north of the city by Route 204, so they wait for plan 06 · R9 (double battles) and that part of S5.
  - 2026-10-05, with plan 01 · M5: that hand-made city is gone. Jubilife City is part of the map of Sinnoh (the imported world), and so are Routes 203 and 204, Oreburgh Gate, Oreburgh City and its mine, the Ravaged Path, Floaroma Town and its meadow, the south of Route 205 and the Valley Windworks. The assistant, Looker and the three clowns stand where the original's objects do and keep the lines above until their scenes are written (`overlays/jubilife_city.json`); the two workers who closed the roads are gone, so until S1's flags and S5's scenes nothing keeps a new player from walking to Oreburgh City. Every area's file lists its objects with the flags that hide them (`hiddenBy`) and its triggers with their variables, and its overlay says which people are waiting for a scene (the hiker with Rock Smash in Oreburgh Gate, the trainer with Captivate on Route 204, the honey man in the meadow). The routes' trainers have Platinum's teams, moves, sight ranges and prize money already; Route 204's twins are one trainer who battles two at a time. Items on the ground, berry soil, the mine's Machop and whoever a flag hides are not placed.
  - 2026-10-05, with S1: the three clowns are scripted (`scripts/jubilife_city.txt`): a yes or no each, and Coupons 1 to 3. Still S5's: the president's scene that announces the campaign (the trigger waiting for `VAR_POKETCH_CAMPAIGN_STATE` 1) and hands over the Pokétch for the three coupons, with it the first two clowns turning up only then (their objects' `FLAG_HIDE_JUBILIFE_CITY_CLOWNS_1_AND_2`) and the third telling the player to stay around until it has happened. A save made meanwhile may already hold the coupons; the president takes them all the same.
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
