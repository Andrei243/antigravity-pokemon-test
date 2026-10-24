# Scripts

The story is written as scripts (plan 02 · S1): what a person does when spoken to, what happens when the player
steps onto certain tiles or arrives somewhere. They are text files in `PokemonPlatinumEngine/Data/scripts/`, read
by `Story/ScriptParser.cs`, kept in `Story/ScriptLibrary.cs` and carried out by `Story/ScriptRunner.cs`. The
dialogue in them is our own, written on the original's beats (see the project rules in `CLAUDE.md`).

## Files

One file per place, named for it:

| File | Whose scripts |
|---|---|
| `<area key>.txt` (`jubilife_city.txt`) | An open area of the imported world |
| `<map name>.txt` (`PokemonCenter.txt`) | A hand-made map |
| `common.txt` | What every place shares: the nurse, the clerk, a PC, a signboard, a trainer's challenge |

A script's name is looked for in the file of the place first and in `common.txt` second, so a place can have
its own `Sign`. `file.Name` names a script of another file outright. `StoryTests` holds every file to a place
that exists, every script to something that starts it, and plays each to its end on every way through it.

## What starts a script

- **Talking to someone.** Their own script when they have one (`"script"` in a map file's NPC record or in an
  overlay's person); otherwise the common one for what they are (`FieldScripts.For`): `Nurse`, `Clerk`, `PC`,
  `Briefcase`, `Attendant`, `Trainer`, or `Talk` for someone who only has lines. A trainer who spots the player
  walks up and then runs the same script.
- **Picking an item up.** An item lying in its ball is someone of the map (`npcType` "ItemBall") and runs
  `ItemBall` when spoken to; something hidden in the ground runs `HiddenItem` when the player looks at its tile
  and it hasn't been found. Both are two lines long: `setflag own`, `find own`.
- **Talking to a Pokémon.** A Pokémon standing in the field (`npcType` "Pokemon", plan 10 · F1: the mine's
  Machop) runs its own script, or `Pokemon`: `cry own`, then `sayown`.
- **Reading a signboard.** `Sign`, or the script the sign names (`"script"` on a map file's signboard,
  `signScripts` in an overlay).
- **Stepping onto a trigger.** A rectangle of tiles with a script and, usually, a story variable and the value it
  waits for. It fires only while the variable has that value, so the script it starts moves the variable on. A
  map file lists its own (`triggers`); an overlay gives the original's triggers their scripts by number
  (`triggers`: the tiles, the variable and the value stay the import's).
- **Arriving.** A file's `OnEnter` runs when the player comes to its place by a door, a warp, a loaded save, or
  by walking across the border of its area. It runs every time, so it asks a variable or a flag before it does
  anything. It may carry the player on by itself: the Vista Lighthouse's lift is a room whose arrival walks its
  rider in, rides and warps them to the other end (plan 02 · S13).
- **Facing an obstacle or the water** (plan 02 · S2). A thin tree, a cracked rock and a boulder are things of the
  map (`npcType` `CutTree`, `CrackedRock`, `StrengthBoulder`) and run `CutTree`, `Rock` and `Boulder` when the
  player faces one and presses the button. The tile ahead starts one too, as the original's
  `Field_TileBehaviorToScript` does: a rock face along its grain runs `RockFace`, a waterfall faced from the water
  `Waterfall`, and deep water at the player's feet `Water`, which the field offers only to someone who may surf
  (the move and the Fen Badge).
- **Looking at furniture** (plan 08 · P8). A television, a bookshelf, a bin and a map on a wall with nothing of
  their own to say run `Television`, `Bookshelf`, `TrashCan` and `TownMap` (`FieldScripts.ForFurniture`): a
  room's prop that covers the tile ahead (`Television`, `Bookshelf`, `TrashCan`), from any side, or else the
  tile's behaviour (`Television`, the three bookshelves, `TrashCan`, `TownMap`: the imported games' rooms), read
  as the GBA games read it, a bin from any side and the rest only from the south, facing north. A signboard, a
  tile script (an overlay's `read`, a room file's `tileScripts`) or a hidden item on the tile goes first, so a
  television with a programme of the room's own keeps it (the player's living room, Rotom's room).
- **A boulder pushed onto a pit** (plan 02 · S12). A tile an overlay lists under `pits` takes a pushed boulder whatever
  the tile is (`FieldMovement.CanPush`), and as the boulder comes to rest there the pit's script starts with the
  boulder as its subject (`self`): it says where the boulder has gone (the Distortion World's fifth and sixth
  floors). Something read on a tile where nothing shows is an overlay's `readAt`, as a door or a statue is its
  `read`.
- **A field move chosen from the party menu.** The menu checks the move where the player stands
  (`FieldMoveRules.Check`: the place, the badge, a partner, what is in force already) and then runs `Use` and the
  move's name (`UseCut`, `UseFly`, `UseSweetScent`), for the Pokémon chosen. Cut, Rock Smash and Strength act on the
  obstacle in front, which is whose script it is (its `own` flag is the obstacle's). Fly's map is chosen before
  `UseFly` runs.
- **An item used from the bag or the item button.** An Escape Rope runs `EscapeRope` and Honey `UseHoney`; the
  Bicycle, the rods and the Poké Radar are the game's own, with no script.
- **Wild Pokémon** (plan 06 · R13). A honey tree faced from the south runs `HoneyTree` (it has no person of its own:
  the tree is a prop, found by its place, `HoneyTrees.Faced`); poison that leaves a Pokémon with one hit point in
  the field runs `PoisonSurvived`. `SwarmNews` is the assistant's sister's, for when her house is built.
- **The Underground** (plan 06 · R16a). The game starts four scripts of `underground.txt` itself: `Wall` when the
  player faces a wall that holds something and presses the button, `BasePC` and `DiggerPC` at the PC of the
  player's own base and of another digger's, and `GoUp` from the last tab of the bag below ground. The hikers'
  are their own (`HikerGoods`, `HikerTraps`, `HikerTreasures`), and Roark's welcome is the place's `OnEnter`.
- **The Villa and Pal Park** (plan 06 · R16b). Looking at a piece of the Villa's furniture runs its `Look…` script
  of `ResortAreaVilla.txt` (`LookTable`, which is the catalogue's order form, `LookPiano`, `LookPokemonBustSilver`:
  the piece's flag in words, `GameEngine.VillaLookScript`), and the sixth Pokémon caught in a Catching Show runs
  `CaughtAll` of `pal_park.txt`.
- **A gate onto the Cycling Road on foot.** A warp marked for riders only (`"cyclistsOnly": true` on a warp of a
  map file: the doors onto the road of the Cycling Road's two gate houses, plan 01 · M11, part d) runs
  `CyclistsOnly` instead of being taken. Inside the gates a row of the lane turns a walker back before the door is
  ever reached (the rooms' own triggers, which ask the built-in variable `CYCLING`).
- **Waking up after a lost battle** (plan 06 · R10). The player comes round in the Pokémon Center last gone into,
  in front of the nurse, who runs `BlackOutCenter`, or at home before any, beside Mom, who runs `BlackOutHome`.
  Both heal the team.
- **A new game.** `common.NewGame` runs once, with no screen: it may set flags and variables and nothing else.
  It is also run for a save from before the story was kept (`Story/StoryMigration.cs`).

While a script runs the player's keys do nothing, trainers don't look and no wild Pokémon appears.

## The language

One command to a line. `#` starts a remark. Texts are in double quotes (`\"` for a quote inside one, `\n` for a
line break). Names are bare words, or quoted where they have spaces.

```
script Clown1
  if flag FLAG_RECEIVED_COUPON_1 goto Done
  say "Ta-da! Welcome to the Pokétch campaign! Here's my question..."
  ask "When a Pokémon wins a battle, does it grow stronger for it?"
  if no goto Wrong
  sound "levelup"
  say "Yes! Exactly right!"
  setflag FLAG_RECEIVED_COUPON_1
  give "Coupon 1"
  end
label Wrong
  sound "bump"
  say "Bzzt! Not this time."
  end
label Done
  say "Battle, win, grow stronger."
  end
```

`script Name` opens a script; it runs to its last line, to `end`, or (when another script called it) to `return`.
`label Name` marks a place in a script for `goto`.

**People** are `player`, `self` (whoever the script belongs to: the person spoken to, the trainer who came up),
`pair` (the second of two trainers who saw the player at once and came up together, in `common.TrainerPair`),
or a person's name: their id in the area's file (`clown_1`, `looker`) or in the map file, or failing that their
displayed name. A script's names mean the people of its own place: `clown_1` in `jubilife_city.txt` is
Jubilife's, and nobody of another area is found. Someone a flag has taken off the map is still found.

### What is said

| Command | |
|---|---|
| `say "..." ["..."]` | Lines in the text box, under the current speaker's name. Lines one after another are one talk: the box stays open between them. |
| `text "..."` | The same with no name on the box: the game's own voice. |
| `sayown` | The lines the script was started with: a person's `dialog`, a signboard's text. |
| `broadcast` | A television's programme (plan 08 · P8; `Story/Broadcasts.cs`), said as one talk under the current speaker: one of the programmes on (the time check always; the Pokédex Report with a Pokédex; the trainer survey with a Pokémon; the Lottery Corner's number in Sinnoh; the Hall of Fame's news once a team has entered it), drawn on the script's die (`random`'s), its lines made of what is true of the game (`IScriptHost.Broadcast`, `BroadcastFacts`) in `ui.json`'s words (`broadcast.*`). Nothing is set. Plan 08 · P9's interview (Jubilife Street Talk: the answer given to Jubilife City's reporters, `StoryState.InterviewPhrase`) is among the programmes once an interview has been given. |
| `broadcast interview` | The programme named (`Broadcasts.Names`: `clock`, `pokedex`, `survey`, `lottery`, `halloffame`, `interview`, `gabbyandty`), with no die drawn, where it is on; where it isn't, whichever the die picks, as `broadcast` does. A television says the interview this way the first time it is looked at after one was given (`FLAG_JUBILIFE_TV_INTERVIEW_ON_AIR`, which the reporters set and `common.Television` and `PlayerHouse.TV` clear). |
| `objective roark` | What to do next (plan 12 · Q14; `Story/Objectives.cs`): stores the objective's number in `VAR_OBJECTIVE`, the name being one `Objectives.All` lists (a name that isn't one is a parse error). Sinnoh's objectives are derived from the flags, variables and badges its chapters already set, so its scripts need no `objective`; a region whose chapters leave nothing to derive one from ends each scene with the objective that follows. Nothing is said or shown; `{objective}` says the line for the region the player is in, with `{player}` and `{rival}` filled in (empty where there is none), and `if objective` asks whether there is one to say. Never stops a hurry. |
| `phrase INTERVIEW` | A phrase of two words the player chooses (plan 08 · P9; style guide, "Phrases"): the picker over the field (`PhraseScreen`, `GameState.Phrases`), on the phrase kept under that name, and the script waits. The words are the Pokémon seen, the team's moves, the types and our own lists (`PhraseGroups`); what is chosen is kept in the story under the name (`StoryState.Phrases`, saved as `SaveData.StoryPhrases`), each word by its key (`feelings:happy`). `RESULT` is 1 when a phrase was given and 0 when the picker was backed out of, the phrase left as it was. Never skipped: a scene in a hurry stops before it. `HeadlessScriptHost.PhraseChoice` is the phrase a test gives (null backs out). |
| `trainerline before` / `after` | The trainer's own line before the battle, or once beaten. `RESULT` is 1 if there was a line to say, 0 if not. |
| `trainerline before pair` | The same for the second of two trainers who came together (`pair`, below), under their own title. |
| `speaker "Name"` / `speaker <id> "Name"` / `none` / `self` | Who the following `say` lines are said by. A script starts with `self`. A bare id before the name (`speaker rowan "Prof. Rowan"`) is the voice of `Data/voice/cast.json` that speaks the lines (plan 26 · V1), so the shown name stays free to change; without one the cast is found by the display name. An id the cast lacks is a finding of `ScriptLibrary.Problems`. |
| `ask "..."` | A question with Yes and No beside it. Then `if yes` / `if no`. The cancel button answers no. |
| `choose "question" "A" "B" ...` | A question with up to six answers. `RESULT` is the place of the one picked, from 0. The cancel button picks the last, so the way out goes last. |

A line may hold `{player}`, `{assistant}` and `{rival}` (who they are is filled in as it is shown, a speaker's
name too), `{self}`, `{lead}` (the first Pokémon of the team), `{starter}`, `{rivalstarter}` and
`{assistantstarter}` (the one of the three neither child took), `{item}` (the last item given or taken),
`{money}`, `{result}` and `{var:NAME}`; `{member:NAME}` is the nickname of the team's Pokémon at the place the
variable gives (from 0), and `{swarm}` and `{swarmplace}` the species of the day's swarm and where it is,
`{trophygarden}` the Trophy Garden's newest (plan 06 · R13), `{marsh:1}` to `{marsh:6}` the Pokémon the day has brought to that area of the Great Marsh (the Sinnoh Pokédex's list; plan 01 · M11, part b: the observatory's binoculars), `{ticket}` the Lottery Corner's number of the day and
`{winner}` the Pokémon whose ID number the last `lottery draw` matched (plan 06 · R14a), `{berry}`, `{yield}` and
`{mulch}` what the patch of soil the script belongs to holds, as the last `berry` command left them, and (plan 06 ·
R15) `{daycare:0}` and `{daycare:1}` the Pokémon in each place of the Day Care, `{level:N}` the level its steps there
have brought it to, `{grown:N}` the levels it has grown and `{fee:N}` what taking it back costs, `{left}` the Pokémon
last left there and `{taken}` the one last taken back, and (plan 01 · M11, part c) `{size}` the size of the Pokémon
last measured with `team size` and `{record}` the record it was held against, in centimetres, and (plan 14 · B14)
`{chosen}` the Pokémon the last `choosepokemon` chose, by its nickname, (plan 19 · K9, part c)
`{species:NAME}` the species whose National Pokédex number the variable holds, and (plan 08 · P9) `{phrase:NAME}` the
phrase kept under that name (`phrase NAME`), as the chosen language says its words.

### Where a script goes

| Command | |
|---|---|
| `goto Label` | |
| `call Script` | Carries another script out and comes back after its `return` or its last line. |
| `return` | Back to the script that called this one; ends a script nobody called. |
| `end` | Ends everything, whoever called. |
| `if [not] <question> <command>` | Carries the command out when the answer is yes. Any one command may follow, most often `goto`. |
| `scene "Name"`, `endscene` | Mark a scene that may be skipped, from its first line to where it has been played to its end (plan 12 · Q20). "Scenes that may be skipped", below, has both. |
| `random seen` | A species the Pokédex has seen, drawn on the game's own chance (plan 19 · K9, part c; FireRed's `SampleResortGorgeousMon`): `RESULT` is its National Pokédex number, and 0 where the Pokédex has seen none. A test says which it has seen (`HeadlessScriptHost.PokedexSeen`) and rolls for the draw. |
| `random 26` | A die of that many sides (one or more), rolled on the game's own chance: `RESULT` is 0 to one less than its sides (the original's `GetRandom`; plan 01 · M11, part a: the flower shop's berry of the day, the Berry Master's, the Old Chateau's ghosts). A test rolls for it (`HeadlessScriptHost.Rolls`, and 0 once they run out). |

What an `if` can ask:

| Question | |
|---|---|
| `flag FLAG_X` | The flag is set. |
| `var VAR_X == 2`, `var VAR_X >= VAR_Y` | A variable against a number or another variable (`==`, `!=`, `<`, `<=`, `>`, `>=`). The game's own may be read too: `RESULT`, `PLAYER_X`, `PLAYER_Y`, `MONEY`, `PARTY_COUNT`, `BADGE_COUNT`, `GREETINGS` (the people spoken to, `greetings clear` below), `SEEN` (the species of the Sinnoh Pokédex seen), `LEAD_FRIENDSHIP` (the friendship of the first Pokémon of the team, 0 with none: the fan club's woman, plan 06 · R14c), `TURNS` (the turns of every battle this script has fought, added up as the original counts them: a battle's last turn is not one, so a battle won at once is none; the original's `VAR_TOTAL_TURNS_LAST_BATTLE`, for the Pokémon Mansion's maids) and `SAFARI_CAUGHT` (the Pokémon caught in the Safari Game under way, or in the last one until the next begins: `GetCurrentSafariGameCaughtNum`; both plan 01 · M11, part b) and `CYCLING` (1 while the player rides the Bicycle, else 0: the original's `CheckPlayerOnBike`, which the Cycling Road's gate keepers ask; plan 01 · M11, part d) and `CAUGHT` (the species caught, every one the Pokédex has: the original's `GetPokedexCount`, which Oak's aides in Kanto ask; plan 19 · K3), and, plan 21 · H6, part c, `FAINTED` (the Pokémon of the team that are no Eggs and lie fainted: Gabby and Ty ask whether one was lost in the battle), `LEAD_SPECIES` and `SECOND_SPECIES` (the National Pokédex number of the first and the second Pokémon of the team that are no Eggs and can still fight, 0 for none: what the pair note to quote in their segment), and `CROSSED_IN` (1 where the script plays in a region the game didn't begin in, else 0: a region's opening asks it before `namerival`, plan 18 · W5). |
| `yes`, `no` | The answer to the last `ask`. |
| `result == 1` | What the last question, battle, handing-over or taking came to. |
| `won`, `lost` | The last battle. |
| `badge coal`, `badges >= 3` | The badges of the region the player is in (plan 18 · W2): a badge by its name in that region's file (`coal` in Sinnoh, `boulder` in Kanto), and how many of that region's are won. A name no region has is a mistake the parser tells; one the place's own region hasn't, `ScriptLibrary.Problems` tells. |
| `movebadge "Cut"` | The Badge a field move needs outside battle in the region the player is in is won, or that region asks none (plan 19 · K4, part b; `FieldMoveRules.BadgeFor`, the region's `gates`): the Forest Badge for Cut in Sinnoh, the Cascade Badge in Kanto. The common scripts of the obstacles ask this, never a Badge by its name. |
| `item "Potion"`, `item "Coupon 1" >= 3` | The bag holds it (that many). |
| `party < 6`, `has "Starly"`, `knows "Cut"` | The team's size, a species on it, a move one of them knows. |
| `granted "Cut"` | A wild helper does this field move now (plan 12 · Q18): the options' Field move helpers row on for the rules, the region's Badge for the move held and the HM in the bag; false for Flash, Teleport, Dig and the rest, and with the row off. Asked beside `knows` by the obstacles' scripts, and `usemove` falls back to the helper when it is true and nobody on the team knows the move. |
| `has "Geodude" met "Safari Zone"` | A species on the team that was met at that place (plan 20 · J8, part b: the original's `CheckSafariZoneChallengeCompleted`, for Warden Baoba's tests). The place is the name the Pokémon keeps of where it was met (`Pokemon.MetLocation`), which is the area's name as it is shown. |
| `has var VAR_X` | A species on the team by the National Pokédex number a variable holds (plan 19 · K9, part c: Selphy wishes for a species she has heard of, `random seen`, and then asks whether one has been brought). Nothing is on the team for a variable that holds no species. |
| `defeated self`, `defeated "trainer_id"` | The trainer has been beaten. |
| `taken "item_id"` | An item on the ground has been picked up. |
| `starter "Piplup"` | The species the player took from the briefcase. |
| `money >= 500`, `facing left`, `boy`, `girl` | |
| `poketch` | The player has the Pokétch. |
| `poketchapp MemoPad` | The app is on the Pokétch (the original's `CheckPoketchAppRegistered`). |
| `pokerus` | A Pokémon of the team carries Pokérus now (the nurse's script asks it). |
| `safari` | A Safari Game is under way (the Great Marsh, Kanto's Safari Zone or Johto's). |
| `region "Kanto"` | The player is in that region (plan 19 · K6): for a common script two regions share, such as where a Safari Game that runs out sends the player back to. |
| `subject "pushable_boulder_2"` | Whoever the script belongs to is that one of its place, by their id in the area's file (plan 19 · K6, part b): for a script many share that does something of each's own, as the Seafoam Islands' holes do with the boulder that fell, each of which shows a boulder of its own on the floor below. |
| `partner` | Someone travels with the player (`partner`, below). |
| `rematch self` | The trainer is waiting for a rematch the Vs. Seeker found (plan 06 · R12). |
| `time morning` | The time of day now: `morning`, `day`, `twilight`, `night` or `latenight` (`GameClock`, plan 06 · R14a). |
| `weekday friday` | The day of the week today. |
| `objective` | The story has a line to say of what to do next in the player's region and the options' WHAT TO DO NEXT row allows it (plan 12 · Q14; `IScriptHost.ShowsObjectives`, `Objectives.Current`): asked before a line that says `{objective}`, as `common.NextHint` does. |
| `nationaldex` | The National Pokédex is the player's (plan 02 · S14; the original's `GetNationalDexEnabled`): the Hall of Fame's script, Mom and the assistant ask it, and whoever the original keeps for the National Pokédex (the assistant's grandfather, Bebe, Mr. Backlot, Byron, Pal Park's workers; plan 02 · S15, part a). The professor's lab gives it (`nationaldex`, "After the Hall of Fame", below). |

### What the story remembers

| Command | |
|---|---|
| `setflag FLAG_X`, `clearflag FLAG_X` | Flags are named `FLAG_` and capitals, after the original's where it has one. |
| `setflag own`, `clearflag own` | The script's own flag: the one that hides whoever it belongs to (an item's ball), or that says a hidden item has been found. Setting it is what makes the ball gone for good. |
| `setvar VAR_X 2`, `addvar VAR_X 1` | Variables likewise (`VAR_`). One never set is 0. |
| `setvar VAR_X VAR_Y`, `setvar VAR_X RESULT` | A variable takes another's value as it is now, or one of the game's own (plan 02 · S15, part c; the original's `SetVar` takes either): the Battleground keeps the day's draw so (`random 8`, then `setvar VAR_BATTLEGROUND_TRAINER_1 RESULT`). |

A flag named `FLAG_MAP_LOCAL_...` lasts only while the player stays in its place, as the original's local flags
do: it is cleared whenever the player comes to another area or through a warp (`StoryState.ClearLocal`). An
obstacle's flag is one of these, made its area's own (`FLAG_MAP_LOCAL_HIDE_OBSTACLE_1_ETERNA_CITY`), so a tree
that was cut grows back once the player has gone. So are three of the game's own: `FLAG_STRENGTH_ACTIVE` (cleared
on leaving any place), `FLAG_FLASH_ACTIVE` and `FLAG_DEFOG_ACTIVE` (cleared on going anywhere that isn't a cave).

### Giving and taking

| Command | |
|---|---|
| `give "Potion" [3]` | Into the bag, with the item fanfare and the lines that say so ("received", and where it was put; a TM or an HM also says the move it holds). |
| `find "Potion" [3]` | The same for something picked up: "found". |
| `give own`, `find own`, `additem own` | The script's own item and how many: what lies in the ball, or in the ground, that started it. |
| `additem "Potion" [3]` | Into the bag without a word. |
| `take "Coupon 1" [3]` | Out of the bag: all of them or none. `RESULT` is 1 if they were taken. |
| `givepokemon "Starly" 4` | Onto the team, or to the PC when the team is full. `RESULT` is 1 or 2. The words and the fanfare are the script's. |
| `givepokemon "Castform" 25 holding "Mystic Water"` | The same with an item in its hand (plan 21 · H6: the Weather Institute's Castform, as Emerald gives it). |
| `givepokemon "Dratini" 15 knowing "Extreme Speed"` | The same with a move in the place of its first (plan 20 · J11, part c: the Dragon Shrine's Dratini, as HeartGold's `SetMonMove` puts it); `holding` and `knowing` may both be given, in either order. A move the data doesn't have is a problem of the library's. |
| `givebadge coal` | Gives a badge of the region the player is in, by its name in that region's file (plan 18 · W2), with its fanfare. |
| `givemoney 500`, `takemoney 500` | `takemoney` takes all of it or nothing; `RESULT` says which. |
| `heal` | The whole team. |
| `seen "Dialga"` | The Pokédex has the species as seen, without a word and without a battle (plan 02 · S12; the original's `SetSpeciesSeen`, for a Pokémon a scene shows: Dialga and Palkia at Spear Pillar). |
| `greetings clear` | The count of people spoken to starts again from nought (plan 08 · P12; the original's `ClearSpiritombCounter`). The built-in variable `GREETINGS` is the count: everyone spoken to since, each once (`StoryState.Greeted`, saved). |

### Battles

| Command | |
|---|---|
| `battle self [canlose]` | A trainer battle with someone of the map who is a trainer. Won, the script goes on (and they are beaten for good, prize money paid). Lost, the script ends there and the player wakes up in the last Pokémon Center (at home before any), unless `canlose`: then the team is healed and the script goes on with `if lost`. With no Pokémon able to fight (`IScriptHost.CanBattle`), no battle starts: a line says so and the script ends there. |
| `battle self and other` | Two trainers of the map at once, each with a team of their own, in a double battle. Won, both are beaten. Whoever is battled again once beaten (the Seven Stars Restaurant's couples on another day, plan 01 · M11, part b) brings their team afresh from Platinum's data, as the original builds a trainer's team for every battle. |
| `battle self with cheryl`, `battle self with "cheryl_eterna_forest"` | A tag battle: someone of the map who is a trainer, or a trainer of Platinum's data by id, battles beside the player, with a team of their own and their own mind. The player loses when their own team is down, whatever the partner has left. The words after the first trainer come in any order (`battle a and b with c canlose`). |
| `battle self and pair with partner` | Two trainers who came together, side by side (plan 02 · S6): against the player's two Pokémon, or beside whoever travels with the player (`partner`), who brings a fresh team of their own from Platinum's data, as the original builds it for every battle. Nobody travelling, `with partner` adds no one. |
| `battle self rematch` | A rematch the Vs. Seeker found (plan 06 · R12): the trainer brings the team of the highest level of their row the story has unlocked and not yet beaten (`VsSeeker.RematchTeam`), for this battle only, and stops waiting. |
| `battle self first` | The game's first battle (the rival's on Route 201): no critical hits, as in Platinum. |
| `battle rival as "rival_route_201_turtwig"` | Someone of the map fights with a team of Platinum's data, and is that trainer from then on (who they think like, their items, their prize money): the rival, whose team hangs on the player's starter (plan 02 · S4). A trainer of the `Rival` class is called `{rival}`. |
| `battle self as "scientist_ingram" inverse canlose` | An inverse battle (plan 14 · B1; `docs/mechanics/rulings.md`, "Formats of the later games"): the type chart is turned round for the whole battle, what a type resisted or took nothing from hurting it and what it feared barely touching it. The word goes with the others in any order, and reaches the battle with its conditions as it is made (`BattleTerms`, `BattleConditions.Inverse`): nothing turns a running battle round. It is no part of Platinum, so only a person of our own says it (the Fight Area's scientist). Someone battled `as` a team of the table is called by that team's name and class in the battle and beats that id when beaten: a person of ours has a team of ours, in `Data/our-trainers.json` (plan 14 · B14; `docs/data-files.md`, "Trainers of our own"), which `as` finds as it finds Platinum's. |
| `master self [canlose]` | A Master Trainer's battle (plan 14 · B14; `docs/mechanics/rulings.md`, "Master Trainers"): the person fights as the master of the species of the Pokémon the last `choosepokemon "Species"` chose (`master_bidoof`, `Data/master-trainers.json`, its one Pokémon at level 75), one-on-one: the player's side is that Pokémon alone, the team's own, so the EXP, the levels and the hurts it takes stay with it, and nothing may be used from the bag by either side (`BattleTerms.NoBattleItems`). No prize is paid. With nobody chosen, or the one chosen fainted, no battle is fought and `RESULT` is 255. Won, `master_<species>` is beaten for good (`if defeated "master_bidoof"`), which is what a title is; lost under `canlose`, the team is healed and it may be tried again. |
| `wildbattle "Starly" 2` | A wild Pokémon put in the player's way. `RESULT`: 1 won, 0 lost, 2 fled, 3 caught. Like `battle`, it waits for a Pokémon able to fight. |
| `wildbattle "Giratina" 47 nofleeing` | One that can't be run from (the story's legendaries). |
| `wildbattle "Gyarados" 30 shiny` | A shiny one (plan 20 · J11: the Lake of Rage's red Gyarados), made of a shiny personality drawn for the player's trainer number as the original's does; `nofleeing` and `shiny` go in either order. |
| `wildbattle "Giratina-Origin" 47` | A form's name in place of its species': the Pokémon is met in that form (plan 02 · S12), and one caught keeps it only as the form's own rule lets it (Giratina's by the Griseous Orb, `FormRules.ByHeldItem`). |
| `defeat "lass_caroline"` | A trainer of Platinum's data counts as beaten without a battle, in this game and every save of it, and whoever carries that trainer on any map stops wanting to battle: a Leader's script does it for the trainers of the Gym once the Badge is won (the original's `SetTrainerFlag`, plan 01 · M9). |
| `catchinglesson "Bidoof" 2` | The assistant shows how a Pokémon is caught: their own starter at level 5 and twenty Poké Balls, nothing chosen by the player, no critical hit and no miss, and a ball that can't fail. What is caught is the assistant's. `RESULT` is 3. |
| `catchinglesson "Ralts" 5 by "Wally" "Zigzagoon" 7` | The same lesson shown by someone else, with a Pokémon of their own at that level (5 if none is given): plan 21 · H2's Wally, with his father's Zigzagoon. |

### People and the field

| Command | |
|---|---|
| `face <who> up` / `face <who> <whom>` | Turns someone a way, or to look at someone. |
| `walk <who> up 2 left 3 [fast]` | Walks them there and waits. The player takes their own steps (stairs, ledges); other people go where they are sent. |
| `move <who> ...` | Starts the same walk and goes on at once. `waitmoves` waits for everyone still walking. |
| `walk <who> onspot down 2 back up [fast]` | Two more words of a walk's (plan 11 · C11), each before a direction: `onspot` takes that direction's steps on the spot, turned that way with the legs walking and the tile the same (the original's `WalkOnSpot`); `back` takes them with the facing kept, a step back when the way is behind (the original's `LockDir` … `UnlockDir`). The player's are their own: a step back that can't be taken is given up as a walk into a wall is. They go with `move` too. |
| `emote <who> exclaim [0.9]` | A bubble over their head for that long: `exclaim`, `question`, `dots`, `note`, `heart`, `sleep`, `sweat`. |
| `act <who> wave [times]` | A gesture played once, or that many times in a row (up to 9), and waited for to its end as a walk is (plan 11 · C11; style guide, "Characters", "Animation"): `wave`, `nod`, `cheer`, `surprised`, `bow`, `shrug`, `point`, `laugh`, or `jump`, a jump on the spot (the hop's clip where they stand; the original's `JumpOnSpot`, which no script of Platinum's uses). A trainer's start under the "!" goes before it. Coming to another place ends it. |
| `mood <who> happy` | The face they hold from now on (plan 11 · C11): `neutral`, `happy`, `surprised`, `sad` or `angry`, over whatever they do, until a script changes it or the place is come to again. |
| `pose <who> sit` | How they are set (plan 11 · C7): `sit` on a seat, `sitground`, `lie` or `stand`, at once and whatever is under them, until the place is come to again; nothing is waited for. Someone a scene leaves alone on a bench, a chair, a sofa or a cushion sits by themselves, and on a bed lies; the player stands up again with their next step. A sitter keeps the seat's facing when spoken to. |
| `hide <who>`, `show <who>` | Off the map or back on it at once, until the map is come to again. Who is there from one visit to the next is decided by the flags (`hiddenBy`, `shownBy`), so a script that sends someone away for good sets their flag as well. |
| `place <who> X Y [down]` | Puts them on a tile. |
| `warp "Map" X Y [up]` | Through a fade to a tile of another map. |
| `fade out [0.4]`, `fade in` | The field to black and back. Text shows over the black. A script that ends in the dark is brought back. |
| `wait 0.5` | |
| `camera pan X Y [0.8]`, `camera release [0.6]`, `camera shake [0.5]` | Sends the camera to look at a tile, brings it back, shakes the picture. A pan and a release are waited for (a shake goes on under whatever comes next), and the release brings back everything the camera kit was told as well. |
| `camera zoom 1.5 [0.8]`, `camera follow <who> [0.8]`, `camera path X Y X Y … [4]`, `letterbox on\|off [0.4]`, `slowmo 0.25 [2]` | The camera kit (plan 09 · L4): the lens, a camera kept on someone, a path through tiles at a pace, the cinema bars, the field slowed for a time. "The camera kit", below, has each. |
| `effect burst X Y [seconds] [wait\|hold]`, `effect shadow X Y [2] [right] [wait\|hold]`, `effect stop X Y` | A scene effect on a tile (plan 09 · L11; style guide, "Life", "Scene effects"), with its sound: `burst` (light thrown out, a white flash over the picture), `beam` (a column of light), `rift` (a tear in the air), `shadow` (a dark shape crossing the ground the way it is sent, east if none is given; only a shadow is sent a way), `ripple` (the picture wavering) or `sparkles`. It plays its own length (0.6, 1.5, 3, 2, 1.2 and 1.5 s) or the time given under whatever comes next; `wait` waits for it to end, `hold` plays it until `effect stop` on its tile (a held one is given no time). Coming to another place clears every effect. |
| `anim heal`, `anim pc on\|off`, `anim elevator up\|down [4]`, `anim halloffame`, `shake <who> [8]`, `flicker <who> [6]` | The original's set pieces (plan 09 · L12): a Pokémon Center's machine, a PC's screen, a lift's ride, the Hall of Fame's machine, and someone shaken or flickering where they stand. Each is waited for. "The original's set pieces", below, has each. |

### Sound

| Command | |
|---|---|
| `music "sinnoh/jubilife"`, `music area`, `music stop` | A song, the place's own theme again, silence. |
| `fanfare heal` / `item` / `pokemon` / `levelup` / `keyitem` / `tm` / `badge` / `evolution` | `give` and `find` play the fanfare of the item's kind themselves (a TM's, a key item's, or the item's), and `givebadge` the badge's. |
| `sound "select"` | One of the game's sound effects (`AudioManager.SoundNames`; [`sound-effects.md`](sound-effects.md) lists them). |
| `cry "Shinx"`, `cry "Giratina-Origin"`, `cry own` | A Pokémon met in the field cries: a species, or a form by its name, with the original's field echo (`CryMode.FieldEvent`). `cry own` is the cry of the Pokémon the script belongs to (a Pokémon standing in the field, `NPC.Species`; plan 10 · F1). |

### The screens that exist

`starter` (the briefcase's three; `RESULT` is the one taken, 0 to 2), `chooseitem berries` and `chooseitem mulch` (the bag, at the pocket of berries or of items, to choose a berry that grows or a mulch: `RESULT` 1 and the item is the script's last, or 0 when none was chosen; plan 06 · R14a), `choosepokemon` (the party, to choose one of the team: `RESULT` is its place, from 0, or 255 when none was chosen; plan 06 · R12; `{chosen}` names it in a line), `choosepokemon "Bidoof"` (the same, only a Bidoof may be chosen: every other card is greyed NOT ABLE, an Egg too, and a choice the species refuses leaves `RESULT` 255; plan 14 · B14, for `master`), `pc halloffame` (the PC's Hall of Fame), `shop` (a Mart's counter: BUY, SELL and SEE YA!; `shop "jubilife"` opens a town's own counter of `marts.json`, and a bare `shop` the clerk's own `mart` or else the common counter by the badges), `pc`, `wardrobe` (the player's clothes, slot by slot, changed for nothing: the bedroom's wardrobe; plan 11 · C10; `wardrobe "jubilife"` is a boutique of `clothes.json`, whose stock is tried on and bought with a yes), `travel` (the way to the
next region: the attendant says how things stand; `RESULT` is 0 where no way leads on from here, 2 once the region's Hall of Fame is entered and the next region is built, and 1 before; `travel go` is the crossing itself, written over a script's black screen: the field's music stops, the vessel is heard (`crossing_horn`, or `crossing_engine` for a plane), its one line is said (`crossing.voyage.*`), and as the line is put away the player crosses with the whole team (plan 18 · W4, part b: `Models/Crossing.cs`) onto the next region's `ArrivalSpot`, through the arrival's own fade, with its music and name sign; the script goes on there, and lifts its dark with `fade in`. Where no way leads on now `travel go` does nothing. `common.Crossing` is the question, the dark and the arrival, which `common.Attendant` calls on 2 (plan 18 · W5). The headless host crosses too, at `travel go`), `namerival` (plan 18 · W4, part b: the introduction's keyboard over the field for the rival of the region the script plays in, on the name they have now; OK with nothing typed takes the region's own rival's, and B only deletes a letter, so `RESULT` is always 1; from then on `{rival}` in that region names them and the save keeps the name for the region, `SaveData.RivalNames`. A region crossed into asks this in its own opening; the headless host names the rival `RivalNameGiven`, or the region's own), `townmap` (a map on a wall, plan 08 · P8: the Town Map's screen in its wall mode, `TownMapMode.Wall`, back to the field when put away; `RESULT` is 1 when it opened, 0 where the region has no `townmap.json`), and the Poffin House's pot (`poffin cook`, below).

### The trainer tools

Plan 06 · R12.

| Command | |
|---|---|
| `trade kazza` | One of the original's trades with people of the game (`Models/NpcTrades.cs`): the Pokémon last chosen with `choosepokemon` goes, the trade's own comes, with its nickname, its trainer and its held item, and is met in a trade. `RESULT` is 1 when the one chosen was the species the trade asks for, and nothing changes hands otherwise. A traded Pokémon from a person never evolves by the trade (the original's `ScrCmd_TradeWithNpc` asks no evolution). A GBA game's trade gives each of the five conditions its own number and a sheen (`NpcTrade.Conditions`, `Sheen`: Fortree's `pluses`, plan 21 · H6, part b); Platinum's give all five one. |
| `halloffame` | The team enters the Hall of Fame (`Models/HallOfFame.cs`): the entry is written, and every Pokémon of the team that is no Egg wears the Sinnoh Champ Ribbon. The scene of it is `halloffame show` ("The Hall of Fame's end", below). |

### Poffins

Plan 06 · R14c. Poffins are kept in the Poffin Case (`Models/PoffinCase.cs`, a hundred of them, saved), cooked at the
Poffin House's pot (`UI/PoffinCookingScreen.cs`) and given to Pokémon from the case (the bag's OPEN), all by the
original's rules (`Models/Poffins.cs`, `Models/PoffinPot.cs`). One command, as the original's `CheckCanCookPoffin`,
`OpenPoffinCooking`, `CheckHasEmptyPoffinCaseSlot` and `GivePoffin` are four.

| Command | What it does |
|---|---|
| `poffin check` | `RESULT` is 0 when the player can cook: 1 when the bag holds no berry, 2 when the case is full (the original's `CheckCanCookPoffin`). Whether the player has the case at all is the script's to ask (`if item "Poffin Case"`). |
| `poffin cook` | The cooking: the bag opens on its berries, the berry chosen goes into the pot, the three stages are stirred, the Poffin goes into the case, and "Cook another?" opens the bag again, until the player stops (backing out of the bag, or NO). The script waits for all of it. |
| `poffin room` | `RESULT` is 1 while the case has room for another Poffin, 0 when it is full. |
| `poffin give 60 30 30 30 30 40` | A Poffin of these five flavours (spicy, dry, sweet, bitter, sour) and this smoothness goes into the case without a word, made as the pot makes one (`Poffins.Make`: its kind, Mild from 50). `RESULT` is its kind's number in the original's list (`PoffinType`), or 65535 when the case was full. The words and the fanfare are the script's. |

### The Day Care and Eggs

Plan 06 · R15. Solaceon Town's Day Care is `Models/DayCare.cs` (two places, their steps, the Egg the couple find,
the egg cycle under way; saved) and the rules of Eggs `Models/Breeding.cs`, both by the original's
`src/overlay005/daycare.c`. One command for the Day Care, as the original's dozen day-care commands, and two for Eggs.
The couple's scripts are `common.DayCareMan` and `common.DayCareLady`.

| Command | What it does |
|---|---|
| `daycare state` | `RESULT` is the Day Care's state (`GetDaycareState`): 0 empty, 1 an Egg waiting, 2 one Pokémon, 3 two. |
| `daycare check` | Whether the player may leave a Pokémon at all: `RESULT` 1 when only one Pokémon of the team isn't an Egg (`CountPartyNonEggs`), 2 when the team's Pokémon able to fight and the boxes' come to two (`CountAliveMonsAndBoxMons`), else 0. |
| `daycare leave` | Leaves the Pokémon last chosen with `choosepokemon` in the first free place (`StorePartyMonIntoDaycare`), and it cries; `{left}` names it. `RESULT` 0 when it was left, 1 when none was chosen, 2 when it is an Egg, 3 when it is the last that can fight, 4 when the Day Care is full. |
| `daycare take 0` | Gives the Pokémon in a place back for its fee, grown by its steps (`MoveMonToPartyFromDaycareSlot`), and it cries; `{taken}` names it. `RESULT` 0 when it was taken back, 1 when the player is short of the fee, 2 when the team is full, 3 when nobody is there. |
| `daycare grown 0` | `RESULT` is the levels the Pokémon in a place has grown (`BufferDaycareGainedLevelsBySlot`). |
| `daycare egg` | The Egg the couple found goes onto the team, received from the Day-Care Couple (`GiveEggFromDaycare`). `RESULT` 1, or 0 with no Egg or no room. The words and the fanfare are the script's. |
| `daycare keep` | The Egg turned down: the couple keep it, and nobody sees it again (`ResetDaycarePersonalityAndStepCounter`). |
| `daycare compatibility` | `RESULT` is how well the two get on (`GetDaycareCompatibilityLevel`): 0 very well, 1 well, 2 not much, 3 not at all. |
| `giveegg "Togepi"` | An Egg of a species onto the team (`GiveEgg`: Cynthia's Togepi, Riley's Riolu), received from the script's speaker. `RESULT` 1, or 0 when the team is full and nothing was given. The words and the fanfare are the script's. |
| `hatch` | The team's first Egg with no cycles left hatches in its scene (`HatchEgg`; `UI/HatchScreen.cs`) and goes into the Pokédex; the script waits for it. `RESULT` 0 when no Egg is ready. The field runs `common.HatchEgg` when an egg cycle finds one. |

### Gyms

Plan 01 · M9. A Gym's puzzle is the room's own (`puzzle` in its map file, `GymPuzzle`): the Hearthome Gym's doors are chosen, the Veilstone Gym's bags and tyres and the Sunyshore Gym's gears laid out as the player comes in, and a bag is kicked by facing it and pressing the button, all with no script. What a script does to a puzzle is here.

| Command | What it does |
|---|---|
| `pressbutton blue` | The Pastoria Gym's water sets off for the level of a button's colour, `blue` (four tiles), `green` (two) or `orange` (nought), and the script waits while it moves (the original's `PressPastoriaGymButton`; `PastoriaWater`). The room's coordinate events run it as the player steps on a button. |
| `gearbutton normal` | Every gear of the Sunyshore Gym's room turns together, each its own way: `normal` a quarter on, `reverse` a quarter back, `double` a half on, and the script waits while they turn (the original's `PressSunyshoreGymButton`; `SunyshoreGears`). The room's coordinate events run it as the player steps on the button on a gear's hub, every time. |
| `rotatetiles yellow` | Everyone standing on the Mossdeep Gym's arrows of a colour (`yellow`, `blue`, `green`, `purple` or `red`), trainers and statues alike, steps once the way their arrow points, keeping their facing, and turns a quarter anticlockwise; the script waits for the steps as for a walk (plan 21 · H8; Emerald's `moverotatingtileobjects` and `turnrotatingtileobjects`, `MossdeepTiles`). Where they stand lasts until the room is come to again. The room's coordinate events run it as the player steps on a floor switch, every time. Nothing in a room without the puzzle. |
| `flowerclock` | Turns the Eterna Gym's flower clock on to its next time (`VAR_ETERNA_GYM_FLOWER_CLOCK_STATE`, the original's `AdvanceEternaGymClock`; `EternaClock`) and waits while its hands turn and a fountain drains, the camera on them; `camera release` brings it back. `RESULT` is 0 where the clock is at its last time already, 1 when it turned, 2 when a fountain drained too. |
| `platformlift` | The platform of the room's shaft sets off for the top with the player on it, and the script waits while it rises (plan 01 · M9, part 2d; the original's `TriggerPlatformLift`; `LeagueLift`). `RESULT` is 1 when it set off, 0 where it stands at the top already (a League platform never comes down) or the room has none. A passage's coordinate event runs it as the player steps onto the platform's middle; the Champion's room's scene runs it once Cynthia is beaten. |
| `platformlift check` | `RESULT` is 1 when the platform stood at the foot of its shaft as the player came in, 0 when at the top (`CheckPlatformLiftNotUsedWhenEnteredMap`): a passage's `OnEnter` asks, and leaves its trigger dead for a platform that has been ridden. |

### Field moves and key items

Plan 02 · S2. These do what a move or an item does; whether it may be used is the script's to ask first
(`if knows "Cut"`, `if badge forest`), or the party menu's, which has asked before a `Use...` script runs.

| Command | What it does |
|---|---|
| `usemove "Cut"` | "{user} used Cut!", then the move's cut-in (the Pokémon on a band across the screen, with its cry). As the band closes, the obstacle the script belongs to gives way (Cut and Rock Smash). `{user}` in a line is the Pokémon that used it: the one chosen in the party menu, or the first of the team who knows the move. The move must be a field move. |
| `surf` | The player rides out onto the water ahead and the script waits for the step. `RESULT` is 0 where there is no water to ride out on. |
| `climb` | Up or down the waterfall or rock face ahead, to its end. `RESULT` 0 where there is none. |
| `dive` | Down from the dark water under a surfer to the same tile of the sea floor below, or up from the sea floor to the same tile of the sea above, behind a fade with the dive's sound (plan 21 · H5; `FieldMovement.CanDive` and `CanSurface`). Never hurried: a skipped scene stops before it. `RESULT` 0 where neither way is open, and nothing happens. |
| `fly` | Through a fade to the town chosen on Fly's map, in front of its Pokémon Center (or the player's house in Twinleaf Town). |
| `teleport` | Through a fade to the town of the Pokémon Center last gone into. |
| `escape` | Through a fade out of the caves, to where the player went into them (Dig, an Escape Rope). |
| `sweetscent` | Draws out a wild Pokémon of the place where the player stands, on land or water, and battles it. `RESULT` is 0 when nothing lives there. |
| `smashencounter` | After a rock is smashed: where the place keeps Pokémon under its rocks (Hoenn's areas, Emerald's `rock_smash_mons` and rate; plan 21 · H4), one may come out, and it is battled. `RESULT` is 1 when one did, 0 when the rock hid nothing (always, in Platinum's places). | The older name `rocksmash` is the same command; HeartGold's table (plan 20 · J9) counts its rate in a hundred and its rock is the tile ahead, 90 and 10 for its two slots.
| `poketch on` | Gives the player the Pokétch. |
| `poketchapp PartyStatus` | Puts an app on it, by its name in the original's list (`PoketchApp`). |
| `safari start`, `safari end` | Starts the Great Marsh's Safari Game (30 Safari Balls, 500 steps; the fee is the script's own `takemoney`) or ends it (plan 01 · M7). While one is on, every wild Pokémon is met in a Safari battle, and its last step or ball runs `common.SafariTimeUp` or `common.SafariOutOfBalls`. `safari start 600` is a game of another number of steps (plan 19 · K6: FireRed's Safari Zone in Fuchsia City), and the two common scripts send a player of Kanto back to its entrance. `safari start endless` is HeartGold's game (plan 20 · J8, part b): thirty balls and no steps, over when the last ball is thrown or the player walks out; the panel over the field shows the balls alone, and the common scripts send a player of Johto back inside the Safari Zone's gate. Emerald's is `safari start 500` at Route 121's gate (plan 21 · H6, part b), and the common scripts send a player of Hoenn back to that gate's door with `VAR_SAFARI_ZONE_STATE` 1, which its arrival walks out past the counter. |
| `bicycle on`, `bicycle off` | Puts the player on the Bicycle or takes them off it, with the riding song (plan 19 · K6: FireRed's `ForcePlayerOntoBike`, which Kanto's Cycling Road does to whoever comes onto it through its gates); nothing on the water. |
| `partner cheryl "cheryl_eterna_forest"` | Someone of the map travels with the player (plan 02 · S6; `Follower`): they walk a step behind, onto each tile the player leaves, and are never in the player's way (walking back into them swaps the two round). While they do, the team is healed after every battle that isn't lost, every Pokémon met in the grass comes with a second and the partner battles beside the player, two trainers who see the player at once come together for a tag battle, the Bicycle, the rods, an Escape Rope and the field moves that leave the place can't be used, and losing leaves them behind. The player gets off the Bicycle. Saved. |
| `partner off` | They stop, where they stand. |
| `turnback` | Aims the doors of the Turnback Cave room the player has just come into, as the original's `InitTurnbackCave` does (plan 01 · M8; `TurnbackCave`): the door they came in by leads back to the entrance, the other three on, to Giratina's room once `VAR_TURNBACK_CAVE_PILLARS_SEEN` is 3, to the entrance once `VAR_TURNBACK_CAVE_ROOMS_VISITED` is 30, and otherwise to a pillar's room one time in four or to one of the next pillar's six rooms. The room's own `OnEnter` keeps the counts and calls it. |

### Wild Pokémon

Plan 06 · R13. What the game remembers of its wild Pokémon beyond the tables (`Models/SpecialEncounters.cs`).

| Command | What it does |
|---|---|
| `honeytree status` | `RESULT` is the honey tree the player faces: 1 bare (no honey, or its day is over), 2 slathered but nothing has come yet, 3 something waits (`HoneyTrees.Status`). |
| `honeytree slather` | A day of honey on the tree faced, and what it will draw (`HoneyTrees.Slather`). The Honey is the script's own `take`. |
| `honeytree battle` | Battles whatever came to the tree faced, which takes the honey with it. A battle lost ends the script. |
| `swarms on` | Swarms begin (`SpecialEncounter_EnableSwarms`): from now on one place of 22 has one each day. |
| `trophygarden` | Mr. Backlot brings one more Pokémon to the Trophy Garden (`TrophyGarden_AddNewMon`); `{trophygarden}` names it. |
| `roamer start "Mesprit"` | Sets a roaming Pokémon loose (`RoamingPokemon_ActivateSlot`): Mesprit, Cresselia, Darkrai, Moltres, Zapdos or Articuno, made afresh at its level and full HP, somewhere at random; Raikou or Entei (plan 20 · J7, HeartGold's two) roam Johto's routes instead. |
| `lottery draw` | The Lottery Corner's draw (plan 06 · R14a; `Models/Lottery.cs`): `RESULT` is how many of the day's ticket's digits, from the right, match the ID number of the original trainer of a Pokémon on the team or in the boxes, at best (0 to 5); `{winner}` names it. |
| `berry status` | The patch of soil the script belongs to (plan 06 · R14a; `Overworld/BerryPatches.cs`): `RESULT` is its stage, 0 bare, 1 planted, 2 sprouted, 3 growing, 4 in flower, 5 in fruit; `{berry}` and `{yield}` say what grows and how many. |
| `berry mulched` | `RESULT` 1 when mulch is laid on the patch, and `{mulch}` names it. |
| `berry berries` | `RESULT` 1 when the bag holds a berry that grows; `berry mulches` the same for a mulch. |
| `berry mulch` | Lays the item last chosen with `chooseitem mulch` on the bare patch, taken from the bag. |
| `berry plant` | Plants the berry last chosen with `chooseitem berries`, taken from the bag: wet soil, the best rating, growing at once. |
| `berry water` | Waters the patch to the full. |
| `berry pick` | Picks what is in fruit into the bag, leaving bare soil with no mulch; `RESULT` is how many. |
| `lottery boxed` | `RESULT` is 1 when the last draw's winner is in the boxes, 0 when on the team (which wins a tie). |
| `survivepoison VAR_X` | The team's Pokémon at the place the variable gives comes through the poison if it is down to one hit point: cured, `RESULT` 1; otherwise 0. |

### The Super Contests

Plan 06 · R17a. A Super Contest is held by `Models/SuperContests/` (the contest, its dance, its acting; `docs/mechanics/rulings.md`, "Super Contests") on `UI/SuperContestScreen.cs`; the Fashion Case (`FashionCase`, saved) holds the accessories a Pokémon is dressed up with. One command, as the original's `NewContest`, `OpenPartyMenuForContest`, `RunContestApplication`, `EndContest`, `CheckPlayerMonHasRibbon` and `GetFirstTimeVictoryAccessory` are six, and one for `Common_GiveAccessory`.

| Command | What it does |
|---|---|
| `supercontest rank normal` | The rank to be entered: `normal`, `great`, `ultra` or `master`. `RESULT` is left alone (so a menu's answer can pick it line by line). |
| `supercontest category cool` | The contest: `cool`, `beauty`, `cute`, `smart` or `tough`. |
| `supercontest mode official` | What is held: `official` (a whole contest, for a ribbon) or one round for practice, `visual`, `dance` or `acting` (at the Normal Rank whatever rank is set). |
| `supercontest check` | Whether the Pokémon last chosen with `choosepokemon` (its place is `RESULT`) may enter (`CheckContestEligibility`): `RESULT` 0 it may, and it is the one `enter` enters; 1 none was chosen; 2 an Egg or fainted; 3 it lacks the ribbon of the rank before; 4 it knows fewer than two moves. |
| `supercontest enter` | Holds the contest for the Pokémon checked, and the script waits for it. `RESULT` is the player's place, 0 for the winner (in a practice round, how many did better). A win has already given the ribbon, the friendship and the accessory. |
| `supercontest ribbon` | `RESULT` 1 when that win gave a ribbon the Pokémon hadn't; `{ribbon}` names it. |
| `supercontest prize` | `RESULT` 1 when that win put its accessory in the Fashion Case; `{accessory}` names it. |
| `supercontest photo` | The photograph in the frame of the contest last named with `supercontest category` (plan 06 · R17b; `ContestPhotoHasData`): `RESULT` 1 and the photograph is shown, the script waiting until it is closed, or 0 for an empty frame. |
| `supercontest fashioncase` | What comes with the Fashion Case (plan 06 · R17b; the original's Jubilife City scene): five draws among the six fluffs, five among the six feathers and moustaches after them, and three backdrops (the first, and two different ones of the eight after it). Without a word; the item is the script's to `give`. |
| `giveaccessory "Glitter Powder" [2]` | An accessory (`contests.json`'s names) into the Fashion Case, with the item fanfare and the lines that say so. `RESULT` 1, or 0 and not a word when the case has no room for it (nine of a kind, one of the kinds that come alone). |

`{contestant}` is the Pokémon checked for the contest, `{ribbon}` the ribbon the last contest gave, `{accessory}` the accessory last given or won.

### Ball Capsules, Seals and ribbons

Plan 06 · R17b; `docs/mechanics/rulings.md`, "Ball Capsules, Seals and ribbons". The Seal Case (`SealCase`, saved) holds
the Seals and the twelve Ball Capsules; a Pokémon's ribbons are on the Pokémon. A ribbon's command is about the first
of the team that is no Egg, as the original's givers all are.

| Command | What it does |
|---|---|
| `seal give "Heart Seal A" [3]` | So many of a Seal (its name, or the original's constant, `HEART_SEAL_A`) into the Seal Case, without a word: `RESULT` 1, or 0 and none given where that would pass ninety-nine of the kind. `{seal}` and `{seals}` say which and how many. |
| `seal first` | The five Seals that come with the Seal Case (a Heart, two Stars, a Foamy and a Song Seal, each drawn). `RESULT` is how many went in. |
| `seal unown` | The Seals Solaceon Town's boy gives for the team's first Unown, ten of its shape's letter: `RESULT` 0 with no Unown; 1 given, with `{wearer}` the Unown and `{seal}`, `{seals}` what it brought; 2 for the shape he was shown last this visit, or a kind that is full. |
| `seal count` | `RESULT` is how many kinds of Seal the player has. |
| `seal capsules` | Opens the Ball Capsules, and the script waits until the screen is closed. |
| `seal shop` | Opens the Seal merchant's stall with the day's seven Seals, and the script waits. |
| `ribbon has RIBBON_FOOTPRINT` | `RESULT` 1 when the first of the team that is no Egg has the ribbon (the original's constant, or `today` for the ribbon of this day of the week, or `offer` for the one the Ribbon Syndicate offers it next). `{wearer}` names the Pokémon and `{ribbon}` the ribbon. |
| `ribbon give RIBBON_FOOTPRINT` | Puts the ribbon on that Pokémon, without a word: `RESULT` 1 when it hadn't it. `today` and `offer` as above. |
| `ribbon count` | `RESULT` is how many different ribbons the team shows between them (`CountPartyRibbons`). |
| `ribbon offer` | `RESULT` is the price of the ribbon the Syndicate offers that Pokémon next (10000, 100000, 999999), 0 once it has all three; `{ribbon}` names it. |
| `ribbon effort` | `RESULT` 1 when that Pokémon's effort values are full (510). |
| `ribbon feeling` | How Dr. Footstep reads that Pokémon's friendship, `RESULT` 1 to 5 (5 at 255). |

`{seal}` is the Seal last given and `{seals}` how many of it, `{wearer}` the Pokémon the last `ribbon` command or
`seal unown` was about, and `{ribbon}` the ribbon the last `ribbon` command named (the last contest's until one does).
`halloffame` gives every Pokémon of the team that is no Egg the Sinnoh Champ Ribbon as it records the team.

### The Underground

Plan 06 · R16a; `docs/mechanics/rulings.md`, "The Underground". What the player has below ground is the
`UndergroundState` (saved): the bag's Spheres, traps, goods and treasures, the base and its PC, the records.

| Command | What it does |
|---|---|
| `underground give trap "Bubble Trap"` | A trap into the bag below ground, without a word: `RESULT` 1, or 0 with forty already. The name is the trap's own, or the original's constant (`TRAP_BUBBLE`). |
| `underground give sphere red [12]` | A Sphere of a kind (`prism`, `pale`, `red`, `blue`, `green`) and a size (1 if left out) into the bag below ground: `RESULT` 1, or 0 with forty already. |
| `underground give good "Plain Table"` | A good into the PC of the player's base (whether there is a base yet or not): `RESULT` 1, or 0 with two hundred there. |
| `underground room` | `RESULT` 1 while the base's PC has room for another good. |
| `underground flags` | `RESULT` is how many flags the player has brought home. |
| `underground down` | Down through the ground where the player stands, as the Explorer Kit goes: `RESULT` 0 where it can't be used. |
| `underground up` | Back up to where the player went down. |
| `underground mine` | Opens the game of the wall ahead, and the script waits until it is over: `RESULT` 0 if the wall holds nothing. |
| `underground hiker` | Opens the trade of the hiker whose script this is, and the script waits. |
| `underground pc` | Opens the lists of the player's base's PC, and the script waits. |
| `underground bag` | Opens the bag below ground, and the script waits. |
| `underground cantake` | `RESULT` 1 when the flag of the digger whose base the player is in can be taken: no flag is being carried, and theirs wasn't taken today. |
| `underground takeflag` | Takes it: `RESULT` 1 when it was taken. |
| `underground home` | Puts a flag being carried up on the player's own PC: `RESULT` 0 with none carried, 1 put up, 2 put up and the base's own flag bettered by it. |

`{thing}` is what the last `underground give` gave, and `{digger}` the digger whose base the player is in, whose
flag is carried, or whose was last brought home.

### Fossils, the Game Corner, the Villa and Pal Park

Plan 06 · R16b; `docs/mechanics/rulings.md`, "Fossils, the Game Corner, the Villa and Pal Park". The Coin Case's
coins and the Catching Show are the game's `Pastimes` (saved); the Villa's furniture and visitors are the story's
own flags and variables (`FLAG_VILLA_FURNITURE_…`, `VAR_RESORT_VILLA_VISITOR`).

| Command | What it does |
|---|---|
| `fossil count` | `RESULT` is how many Fossils the bag holds, of whatever kind. |
| `fossil choose` | Picks the Fossil to revive, asking which when the bag holds more than one kind: `RESULT` 1, or 0 with none chosen. `{item}` names it and `{thing}` what it was. |
| `fossil revive` | Takes the Fossil chosen and remembers its species (`VAR_REVIVED_POKEMON_SPECIES`): `RESULT` 0 if it couldn't. |
| `fossil ready` | `RESULT` 1 while a revived Pokémon waits to be collected; `{thing}` names it. |
| `fossil give`, `fossil give 5` | Hands it over at level 20, or at the level given (plan 19 · K6, part b: Cinnabar Island's lab, as FireRed's, at 5): `RESULT` 1 into the team, 2 into a box, 0 with none waiting. |
| `gamecorner coins` | `RESULT` is what the Coin Case holds. `{coins}` in a line says it. |
| `gamecorner room 500` | `RESULT` 1 when that many more fit (the case holds 50,000). |
| `gamecorner add 50`, `gamecorner take 50` | Puts coins in or takes them out, all or none: `RESULT` 1 when it did. |
| `gamecorner slots 3` | Sits the player at a machine of that number (0 to 11: its mood is the day's deal for it) and waits until they get up. |
| `gamecorner prize`, `gamecorner prize mauville` | Asks which of the exchange's prizes (Veilstone's, or Mauville's five TMs: plan 21 · H3, part c): `RESULT` 1, or 0 for none. `{item}`, `{price}` in coins, and `{thing}` the move a TM holds. |
| `gamecorner buy` | Exchanges coins for the prize chosen: `RESULT` 1, or 0 with too few. |
| `gamecorner hiddenpower` | Reads the Hidden Power of the Pokémon `RESULT` names (after `choosepokemon`): `RESULT` 0 with none chosen, 1 when it knows the move, 2 when it doesn't, 3 for an Egg; `{thing}` is the type. |
| `gamecorner cointrick` | The Coin Case's clown tosses his coin: `RESULT` 0 or 1, the hand it is in. |
| `villa catalogue` | Asks which piece of what the catalogue lists now: `RESULT` 1 for one to order, 2 for one already in the room, 0 for none. `{thing}` names it and `{price}` is its price. |
| `villa choose "Table"` | Names a piece without asking, with the same `RESULT`. |
| `villa is "Piano"` | `RESULT` 1 when that is the piece last chosen. |
| `villa buy` | Pays for the piece chosen, and it is the player's: `RESULT` 0 with too little money. |
| `villa furnish` | Stands what is owned in the room, each piece in its place. |
| `villa owned` | `RESULT` is how many pieces are owned. |
| `palpark check` | `RESULT` 0 when a Catching Show can begin, 1 when the boxes haven't six free places, 2 when six Pokémon don't wait in the park. With nothing waiting, or another day's six, it first lets the day's six visitors come of the species Sinnoh has no other way to, by a fixed rotation by the date (plan 03 · D12a, `PalParkState.Welcome`). |
| `palpark start` | Lets the six loose and hands over six Park Balls: `RESULT` 0 if it couldn't. |
| `palpark go` | Starts the show's clock. |
| `palpark active` | `RESULT` 1 while a show is on. |
| `palpark caught` | `RESULT` is how many of the six are caught. |
| `palpark points catching` | Stops the clock; `RESULT` and `{number}` are a part of the score (`catching`, `time`, `type`) or all of it (`total`). |
| `palpark record` | `RESULT` 1 when the total beats `VAR_CATCHING_SHOW_RECORD`, which then keeps it. |
| `palpark prize` | Hands over the berry the score earns. |
| `palpark store` | Sends the six to the PC boxes, as Pokémon met at Pal Park. |
| `palpark end` | Ends the show and keeps nothing of it: the six go on waiting in the park. |

### The Battle Frontier and the Battle Tower

Plan 06 · R18a; `docs/mechanics/rulings.md`, "The Battle Frontier's frame and the Battle Tower". `frontier` is what
every facility shares (the records are the game's `FrontierRecords`, saved; the prints are the story's own
`VAR_BATTLE_<FACILITY>_PRINT_STATE`), and `tower` a Battle Tower challenge, which lives between its scripts as
`IScriptHost.Tower` and is never saved but when it is put down (`tower pause`).

| Command | What it does |
|---|---|
| `frontier points` | `RESULT` is the Battle Points held. `{points}` in a line says them. |
| `frontier give 3` | Adds Battle Points (9,999 at most): `RESULT` is what is held afterwards. |
| `frontier prize items`, `frontier prize machines` | Asks which of a booth's prizes at the Exchange Service Corner, with the points shown: `RESULT` 1, or 0 for none. `{item}`, `{price}` in Battle Points, and `{thing}` the move a TM holds. |
| `frontier buy` | Trades Battle Points for the prize chosen: `RESULT` 1, or 0 with too few. |
| `frontier records tower` | Opens a facility's board of records (`tower`, `factory`, `hall`, `castle`, `arcade`) and waits until it is closed. |
| `frontier print tower` | Hands over the commemorative print that waits at a facility: `RESULT` 0 for none, 1 the silver one, 2 the gold one. The fifth gold print sets `FLAG_OBTAINED_ALL_BATTLE_FRONTIER_GOLD_PRINTS`. |
| `frontier judge` | Reads the potential of the Pokémon `RESULT` names (after `choosepokemon`): `RESULT` 0 with none chosen, 3 for an Egg, else 1; `{wearer}` names it and `{thing}` its best stat. |
| `frontier judged total`, `frontier judged best` | `RESULT` is the six IVs added up, or the highest of them. |
| `frontier save` | Saves the game where the player stands, as a counter does before a challenge goes up. |
| `tower begin single` | Starts a challenge (`single`, `double`, `multi`): a streak still running in that mode is taken up. |
| `tower enough`, `tower enough 2` | `RESULT` 1 when the team holds enough Pokémon that could go in together (the challenge's own number, or the one given). `{number}` is how many are needed and `{banned}` the banned species the Pokédex has seen. |
| `tower choose` | Opens the team for the challenge's Pokémon to be entered: `RESULT` 1, or 0 when backed out of. |
| `tower check` | `RESULT` 0 when those entered may go in together, 1 for two of a species, 2 for two holding the same item. |
| `tower enter` | Takes the Tower's generator up and draws the set's trainers. |
| `tower active`, `tower mode` | `RESULT` 1 while a challenge is under way; its mode (0 single, 1 double, 2 multi; -1 with none). |
| `tower next` | `RESULT` and `{number}` are the number of the trainer to come, from 1. |
| `tower opponent` | Makes the next trainer's team (or the next two trainers') and has whoever stands as `opponent` and `opponent_2` in the room take their looks. `{foe}` names them. |
| `tower battle` | The battle, with the Pokémon entered as the Frontier's rules have them: `RESULT` 1 won, 0 lost. A battle lost doesn't end the script. |
| `tower won` | Counts the win: `RESULT` 1 once seven are beaten. `tower sevens` asks that again. |
| `tower streak` | `RESULT` and `{number}` are the streak as it stands. |
| `tower palmer` | Who comes next: `RESULT` 0, or 1 for the Tower Tycoon with the silver print at stake, 2 with the gold. |
| `tower brain` | Counts a Frontier Brain met. |
| `tower beat silver`, `tower beat gold` | Notes the Tower Tycoon beaten in this challenge and moves the print's variable on. `tower beaten` asks it back (0, 1, 2). |
| `tower finish won`, `tower finish lost` | Writes the records: the streak, the room and the Trainer Card's score for seven won, the streak's end for a loss or a challenge given up. |
| `tower points` | Hands over the Battle Points earned, with the points shown: `RESULT` and `{number}`. |
| `tower pause` | Puts the challenge down where it stands. `tower paused` is 1 while one waits. |
| `tower resume` | Takes it up again: `RESULT` 0 when none waits or its Pokémon are no longer as they were, which ends it. |
| `tower forfeit` | Ends a challenge that was walked out on, and its streak. |
| `tower end` | Lets go of the challenge. |
| `tower ribbon palmer`, `tower ribbon streak` | Gives the Pokémon entered the ribbon due: `RESULT` 1 when any was given. |
| `tower trophy` | Sends the trophy due to the secret base's PC: `RESULT` 0 for none, 1 to 3 as given (`{thing}`, and `{number}` the streak it is for), 4 with no room. `tower waiting` says whether one is due. |
| `tower salon` | Lets into the Battle Salon whoever the story has had the player meet (clears their hiding flags). |
| `tower partners` | Gives the Battle Salon's five their teams for this challenge. |
| `tower tells 2` | Fills `{ally:0}`, `{allymove:0}`, `{ally:1}` and `{allymove:1}` with what a partner brings (0 Cheryl, 1 Mira, 2 Riley, 3 Marley, 4 Buck). |
| `tower partner 2` | Chooses that partner. `tower partnered` is who was chosen (-1 for nobody), and has whoever stands as `partner` in the room take their look. |
| `tower door` | `RESULT` 0 to 3: which of the corridor's four doors the attendant leads to. |

`{partner}` names whoever battles beside the player, once chosen.

### What a room's people ask of the team

Plan 01 · M11, part b: one command, `team`, for what the people of the houses of the east ask of the team and do for
it, each after one command of the original's. Part c (the north and the Battle Zone) added `first`, `nature` and `size`.

| Command | What it does |
|---|---|
| `team befriend 3` | The Pokémon `RESULT` names (after `choosepokemon`) grows friendlier by so much, as the original's `IncreasePartyMonFriendship` counts it: half as much again with a Soothe Bell, then one more out of a Luxury Ball, never past 255. `RESULT` stays the Pokémon's place, so a line can go on naming it (`{member:RESULT}`); it is 254 for an Egg, which nothing changes, and 255 with none chosen. The Massage Girl's three points. |
| `team level VAR_DAILY_RANDOM_LEVEL` | `RESULT` is the place of the first of the team (no Egg) at the level the variable holds, or 255 with none: the original's loop of `GetPartyMonLevel` in Route 221's house. |
| `team combee` | `RESULT` 0 with no Combee on the team, 1 with one sex, 2 with both; an Egg is none (`CheckPartyCombeeGenderCount`). |
| `team condition cool` | `RESULT` is a contest condition of the first of the team that is no Egg: `cool`, `beauty`, `cute`, `smart` or `tough`, 0 to 255, 0 with nobody (`GetFirstNonEggInParty`, `GetPartyMonContestStat`). The Scarf Guy's five scarves. |
| `team massage room` | `RESULT` 1 while the Fashion Case has room for any of the Massage Girl's sixteen accessories (Pretty Dewdrop to Glitter Powder, `ScriptRunner.MassageAccessories`). |
| `team massage` | One of the sixteen goes into the Fashion Case, drawn by the script's die among those it still has room for (`TryGetRandomMassageGirlAccessory`), with the item fanfare and the lines `giveaccessory` says. `RESULT` 1, or 0 and not a word with room for none. `{accessory}` names it. |
| `team first` | `RESULT` is the place of the first of the team that is no Egg, or 255 with none (`GetFirstNonEggInParty`): the Ribbon Syndicate's spa treats that one (`team befriend` after it). |
| `team nature serious` | `RESULT` is the place of the first of the team, no Egg, of that nature, or 255 with none (`FindPartySlotWithNature`): the inventor of Sunyshore City, who gives a Pokétch app for the sight of one. |
| `team size "Remoraid"` | The Pokémon `RESULT` names (after `choosepokemon`) is measured against the size record (`CalcSizeContestResult`; `Models/SizeContest.cs`): `RESULT` 0 smaller, 1 the same, 2 larger, and 3 where it isn't that species (an Egg is none, and so is nobody chosen). `{size}` is its size and `{record}` the record's, as that species. The record is the story's `VAR_SIZE_CONTEST_RECORD`, and until one is kept the original's first (33,280). |
| `team size keep` | The Pokémon last measured is the record from now on (`UpdateSizeContestRecord`). |
| `team size "Heracross" VAR_HERACROSS_SIZE_RECORD`, `team size keep VAR_HERACROSS_SIZE_RECORD` | The same, against a record of the measurer's own (plan 19 · K9, part c: Kanto's two measurers each keep theirs, FireRed's `VAR_HERACROSS_SIZE_RECORD` and `VAR_MAGIKARP_SIZE_RECORD`). A variable of its own holds nothing until a Pokémon is kept, so the first one shown is larger than the record, as in FireRed. |
| `team record "Heracross" VAR_HERACROSS_SIZE_RECORD` | `{record}` is what that record comes to as that species, with nothing measured (FireRed's `GetHeracrossSizeRecordInfo`, for the chart on the measurer's wall); `RESULT` is 1 where a record is kept and 0 where none is yet. The variable may be left out for the story's own. |
| `team deoxys "Deoxys-Speed"` | Every Deoxys of the team, no Egg, takes that forme (`"Deoxys"` is its Normal Forme); `RESULT` is how many changed (plan 06 · R29; `ChangeDeoxysForm`, Veilstone City's four meteorites). |
| `team holds "Adamant Orb"` | `RESULT` 1 when anyone of the team holds the item, 0 when nobody does; an Egg holds nothing (plan 02 · S15, part c; `CheckPartyHasHeldItem`). Mt. Coronet's sixth floor asks it of the Adamant and the Lustrous Orb beside `if item`, which asks the bag. |

### The Battle Factory

Plan 06 · R18b; `docs/mechanics/rulings.md`, "The Battle Factory". A challenge lives between its scripts as
`IScriptHost.Factory` and is never saved but when it is put down (`factory pause`); what the Factory shares with the
other facilities (Battle Points, the board, the print, the save) is `frontier`'s, above. Its draws are made on the
game's own chance, as the original's are.

| Command | What it does |
|---|---|
| `factory begin single 50` | Starts a challenge: `single` or `double`, at Level `50` or the `open` level. A streak still running that way is taken up with its trades. The story's `VAR_BATTLE_FACTORY_CHALLENGE_TYPE` and `VAR_BATTLE_FACTORY_CHALLENGE_LEVEL` keep its kind. |
| `factory enter` | Draws the round's trainers, the six rentals on offer and the first trainer's three (a challenge taken up again keeps what it had), and counts a challenge begun. |
| `factory pause`, `factory resume` | Puts the challenge down between two battles, and takes it up again: `RESULT` 1, or 0 when none waits. `factory paused` is 1 while one is put down. |
| `factory forfeit` | Ends the streak of a challenge that was walked out on (the game closed in the middle of it). |
| `factory end`, `factory active` | Lets go of the challenge; `RESULT` 1 while one is under way. |
| `factory way`, `factory level` | `RESULT` 0 for a Single challenge and 1 for a Double one (-1 with none); 0 for Level 50 and 1 for the Open Level. |
| `factory battles`, `factory next` | The battles won in the round, and the number of the one to come: `RESULT` and `{number}`. |
| `factory hint` | What the staff tell of the trainer to come, by the round: `RESULT` 0 three species (`{hint:0}` to `{hint:2}`), 1 two, 2 a species and its first move (`{hint:0}`, `{hint:1}`), 3 a move, 4 a type, 5 no type in common. |
| `factory rent` | Opens the six on offer for three to be chosen in their order, and waits: `RESULT` 1 once they are. The taking counts as one trade. |
| `factory draw` | Draws the next trainer's three, clear of the three in hand and of the three just beaten. |
| `factory trade` | Offers one of the beaten trainer's three for one in hand, and waits: `RESULT` 1 when a trade was made (`{hint:0}` what was given up, `{hint:1}` what was taken), 0 when not. |
| `factory stand` | Has the trainer to come stand ready with the three drawn. |
| `factory head`, `factory announce` | Who the next battle is against (0, 1 the Factory Head for the silver print, 2 for the gold one); and 1 the one time he is to be announced. |
| `factory odds` | `RESULT` and `{number}`: 10 to 99, the chance the Factory Head's machine comes up with (a roll of `random 90`, and ten). |
| `factory opponent` | Stands the trainer of this place of the round in the battle room as `opponent`, with the look of their class. `{foe}` names them. |
| `factory battle` | The battle, with the three in hand, whole: `RESULT` 1 won, 0 lost. A Double challenge puts two out. |
| `factory won` | Counts the win: `RESULT` 1 once the round's seven are beaten. `factory streak` is the streak as it stands. |
| `factory brain`, `factory beat silver`, `factory beat gold` | Counts the Factory Head met; notes him beaten, so that the print waits in the lobby. |
| `factory finish won`, `factory finish lost` | Writes the records: the streak runs on to the next round, or ends where it stands. |
| `factory points` | Hands over the Battle Points a round earned: `RESULT` and `{number}`. |
| `factory beaten` | Who was beaten in this challenge: 0 nobody, 1 the Factory Head for the silver print, 2 for the gold one. |

`{rental:0}`, `{rental:1}` and `{rental:2}` name the three rentals in hand.

### The Hall of Fame's end

Plan 02 · S14. What the Hall of Fame's script does once the team is recorded (`halloffame`, above), as the original's `ClearGame` does it: the scene of the team's entry, the save, the credits.

| Command | What it does |
|---|---|
| `halloffame show` | The scene of the team's entry (`UI/HallOfFameCeremony.cs`; the original's `FieldTask_StartHallOfFame`): each Pokémon of the team in turn with who it is, whose it was and where it was met, then the Champion among them all. The script waits until the picture has closed. |
| `save` | Writes the game to its save where the player stands, with no question and no panel (the original saves so before its credits). `RESULT` is 1 when it was written, 0 when it couldn't be. The words and the sound are the script's. |
| `credits` | The credits (`UI/CreditsScreen.cs`, `Data/credits.json`): the script waits until they have rolled and the last card has been dismissed. |

### After the Hall of Fame

Plan 02 · S15, part a.

| Command | What it does |
|---|---|
| `nationaldex` | The Pokédex becomes the National one (`Pokedex.UnlockNational`; the original's `SetNationalDexEnabled`), as Professor Oak makes it in the professor's lab. It asks nothing and shows nothing: the words and the fanfare are the script's, and so is the question of whether it may be given (`RowanLab.Rowan` asks `flag FLAG_GAME_COMPLETED` and `var SEEN >= 210`, which a test holds to `Pokedex.CanUnlockNational`). `if nationaldex` holds from then on. The PC grows with it, from Platinum's eighteen boxes to a hundred (`PcBoxes.Grow`, plan 03 · D13), which Professor Oak's line after it says. |

`LEAGUE_VICTORIES` is one more of the game's own variables (`if var LEAGUE_VICTORIES >= 20`): the times a team has entered the Hall of Fame (the original's `GetLeagueVictories`), by which the rival in the Survival Area brings the stronger of his two teams.

A Gym Leader a scene battles is named by the script, as in their Gym (`battle volkner as "leader_volkner_fight_area" and flint with "rival_fight_area_turtwig"`: the first of the two by a team of the table, the second a trainer of the map, the partner the rival's team for the player's starter), never made a trainer of the map: the class brings no eye theme.

### The Battle Hall

Plan 06 · R18c; `docs/mechanics/rulings.md`, "The Battle Hall and the Battle Castle". A challenge lives between its
scripts as `IScriptHost.Hall` and is never saved but when it is put down (`hall pause`); what the Hall shares with
the other facilities (Battle Points, the board, the print, the save) is `frontier`'s, above. Its draws are made on
the game's own chance, as the original's are. A whole round is one arrival script of the stage, which loops.

| Command | What it does |
|---|---|
| `hall enough single`, `hall enough double` | `RESULT` 1 when the team holds a Pokémon of level 30 or more that may go in, or two of one species. `{banned}` is the banned species the Pokédex has seen. |
| `hall choose single`, `hall choose double` | Opens the team for the one to be chosen, or the two, and waits: `RESULT` 1, 0 when backed out of, 2 for two of different species. `{entrant}` names the species. |
| `hall streak single` | `RESULT` 0 with no streak running that way, 1 with one of the species chosen, 2 with another species' (`{thing}` names it), which `hall drop single` lets go of. |
| `hall begin single`, `hall begin double` | Starts a challenge with the Pokémon chosen. A streak still running that way is taken up with every type's rank. The story's `VAR_BATTLE_HALL_CHALLENGE_TYPE` keeps its kind. |
| `hall enter` | Counts a challenge begun. |
| `hall pause`, `hall resume` | Puts the challenge down between two battles, and takes it up again: `RESULT` 1, or 0 when none waits or its Pokémon is no longer in its place (which ends the streak). `hall paused` is 1 while one is put down. |
| `hall forfeit` | Ends the streak of a challenge that was walked out on (the game closed in the middle of it). |
| `hall end`, `hall active`, `hall way` | Lets go of the challenge; `RESULT` 1 while one is under way; 0 for a Single challenge and 1 for a Double one (-1 with none). |
| `hall battles`, `hall next`, `hall nth` | The battles won in the round, the number in the round of the one to come, and its number in the streak: `RESULT` and `{number}`. `hall round` is the rounds the streak has come through (0 with no challenge). |
| `hall board` | Opens the board of types and waits; the opponent is drawn for the type chosen. `{type}` names it and `{rank}` the rank it stands at, 1 to 10. |
| `hall argenta` | Who the battle to come is against: 0, 1 the Hall Matron for the silver print, 2 for the gold one. |
| `hall opponent` | Stands the trainer drawn on the stage as `opponent`, with the look of their class. `{foe}` names them. |
| `hall battle` | The battle, with the Pokémon entered, whole and at its own level: `RESULT` 1 won, 0 lost. A Double challenge puts both out against the same Pokémon twice. |
| `hall won` | Counts the win and raises the type's rank: `RESULT` 1 once the round's ten are beaten. `hall rank` is the wins over the type last chosen, 0 to 10 (`{rank}`). |
| `hall brain`, `hall beat silver`, `hall beat gold` | Counts the Hall Matron met; notes her beaten, so that the print waits in the lobby. |
| `hall finish won`, `hall finish lost` | Writes the records, and the species' own record: the streak runs on to the next round, or ends where it stands. `RESULT` 1 when the species' record moved. |
| `hall points` | Hands over the Battle Points a round earned: `RESULT` and `{number}`. |
| `hall beaten` | Who was beaten in this challenge: 0 nobody, 1 the Hall Matron for the silver print, 2 for the gold one. |
| `hall keeper` | The record keeper's count of every species' Single record: `RESULT` 0 nothing won yet, 1 a mark passed and its Battle Points handed over, 2 the next still to come, 3 every mark passed. `hall told total`, `passed`, `coming` and `points` are what was counted (`RESULT` and `{number}`). The story's `VAR_BATTLE_HALL_NEXT_TOTAL_RECORD_MILESTONE` keeps the next mark. |
| `hall fan`, `hall fanstage` | Which of ten things the fan says, by the species with a record (0 to 9), and how near the counter the fan stands (0 to 2). |
| `hall visitors`, `hall species` | How many well-known visitors may turn up, by the Single records added up (0, 1, 2 or 4); and the species with a record (`RESULT` and `{number}`). |

### The Battle Castle

Plan 06 · R18c. A challenge lives between its scripts as `IScriptHost.Castle`; its three are copies the challenge
carries from battle to battle, so `castle battle` is fought on them as they stand.

| Command | What it does |
|---|---|
| `castle enough` | `RESULT` 1 when the team holds three Pokémon of different species that may go in (what they hold doesn't matter: it is kept at the door). `{banned}` as above. |
| `castle choose`, `castle check` | Opens the team for three to be chosen, and waits: `RESULT` 1, or 0 when backed out of. Then 1 when two of them are of one species. |
| `castle begin single`, `castle begin double` | Starts a challenge with the three chosen: copies at level 50 or under, holding nothing, and ten Castle Points for the round. A streak still running that way is taken up with its Castle Points and its counters' ranks. Say it only once the save is agreed to. |
| `castle enter` | Draws the round's trainers and the first one's three (a challenge taken up again keeps what it had), and counts a challenge begun. |
| `castle pause`, `castle resume`, `castle paused`, `castle forfeit`, `castle end`, `castle active`, `castle way` | As the Hall's: a challenge put down keeps the three's HP, PP and what they hold. |
| `castle battles`, `castle next`, `castle nth`, `castle round` | As the Hall's. |
| `castle heard` | `RESULT` 1 once the valet has explained why items are kept at the door; asking tells it heard. |
| `castle cp` | The Castle Points held: `RESULT`, `{number}` and `{cp}`. They stand over the field until the script ends. |
| `castle self`, `castle foe` | Opens the valet's counter for one's own three (healing, rentals, the ranks) or the one about the trainer to come (who they are, their levels, their stats and moves), and waits. |
| `castle passable`, `castle pass` | `RESULT` 1 where a battle may be passed (from the fourth round, never the Castle Valet's); and passes it for fifty Castle Points: `RESULT` 1, or 0 with too few. |
| `castle draw` | Draws the next trainer's three. |
| `castle darach`, `castle announce` | Who the battle to come is against (0, 1 the Castle Valet for the silver print, 2 for the gold one); and 1 the one time he is to be announced. |
| `castle fainted` | `RESULT` 1 when one of the three fainted in the battle just won (it was revived with a single HP). |
| `castle opponent` | Stands the trainer drawn in the throne room as `opponent`. `{foe}` names them. |
| `castle battle` | The battle, with the three as they stand: `RESULT` 1 won, 0 lost. |
| `castle won` | Counts the win and earns its Castle Points: `RESULT` and `{number}` are what was earned. `castle done` is 1 once the round's seven are through. |
| `castle brain`, `castle beat silver`, `castle beat gold` | Counts the Castle Valet met; notes him beaten, so that the print waits in the lobby. |
| `castle finish won`, `castle finish lost`, `castle points`, `castle beaten` | As the Hall's: the records, the Battle Points a round earned, and who was beaten. |


### The move tutors and the Move Deleter

Plan 01 · M11, part d. The three who teach moves for Shards (Route 212's, Snowpoint City's and the Survival Area's),
Grandma Wilma on Route 210, the old man of Route 228 and Canalave's Move Deleter do what the original's do, by
`Models/MoveTutors.cs` (the original's table of tutor moves and their prices, the species' own lists of what they
can be taught, and the two scripts that ask a type or a species and full friendship). One command, `tutor`, each word
on the original's script command; all are for the Pokémon `RESULT` names after `choosepokemon`, `{wearer}` names it,
`{thing}` the move and `{forgotten}` the move given up for it.

| Command | What it does |
|---|---|
| `tutor moves "route_212"` | Asks which of the place's moves the Pokémon is to be taught, of those on its species' list that it doesn't know, each with its price in Shards (`ShowMoveTutorMoveSelectionMenu`). `RESULT` 1 one was picked, 0 none, 2 the tutor has nothing for it, 3 an Egg, 4 nobody chosen. The places are `route_212`, `snowpoint_city` and `survival_area`; any other is told as the file is read. |
| `tutor afford`, `tutor pay` | `RESULT` 1 when the bag holds the Shards the move picked costs (`CheckCanAffordMove`); `pay` takes them, all or none (`PayShardCost`). |
| `tutor special dracometeor`, `tutor special ultimate` | The checks of the two tutors of one move each: a Dragon type for Draco Meteor; one of the twelve starters of Kanto to Sinnoh fully grown for Blast Burn, Hydro Cannon or Frenzy Plant, whichever is its own. `RESULT` 0 it may learn, 1 nobody chosen, 2 an Egg, 3 it can't, 4 it knows the move, 5 its friendship is under 255. |
| `tutor once "Mega Punch"` | A tutor of Kanto's who teaches one move, free (plan 19 · K5; `MoveTutors.Kanto`, our own table of who may learn it): the answer for the Pokémon `RESULT` names, as `special`'s (0 it may learn, 1 nobody chosen, 2 an Egg, 3 it can't, 4 it knows the move); `learn` then teaches it. The script keeps the tutor to once a game with the move's flag (`FLAG_TUTOR_MEGA_PUNCH`). The table serves every region's tutor of one move: Hoenn's Sleep Talk in Fortree City (plan 21 · H6, part b) keeps Emerald's flag, `FLAG_MOVE_TUTOR_TAUGHT_SLEEP_TALK`. |
| `tutor dragons` | `RESULT` 1 when anyone of the team is a Dragon type (Grandma Wilma's first look). |
| `tutor learn` | Teaches the move settled on by `moves` or `special` (`MoveTeaching.Learn`): at once to a Pokémon with room (`RESULT` 1); one that knows four is asked which is to go (`RESULT` 3 learned in its place, 0 given up, 2 an HM's move was picked, which stays, and nothing is learned). |
| `tutor forget` | The Move Deleter's question: which of the Pokémon's moves is to be forgotten. `RESULT` 1 one was picked (`{thing}`), 0 none, 2 it knows only one, 3 an Egg, 4 nobody chosen. |
| `tutor delete` | Takes the move picked away for good, an HM's too (`ForgetMove`). `RESULT` 1, or 0 with none picked. |

Pastoria's Move Relearner and Eterna's Name Rater say what they do and change nothing: their rules are plan 12 ·
Q19's (`Models/Relearning.cs`) and plan 13 · V4's naming prompt.

### The Battle Arcade

Plan 06 · R18d; `docs/mechanics/rulings.md`, "The Battle Arcade". A challenge lives between its scripts as
`IScriptHost.Arcade`; its three are copies holding nothing that the challenge carries from game to game, and so are
the trainer's. A whole round is one arrival script of the game room, which loops: the board, what it did, the
battle, the question.

| Command | What it does |
|---|---|
| `arcade enough` | `RESULT` 1 when the team holds three Pokémon of different species that may go in (what they hold doesn't matter: the board takes it for the challenge). `{banned}` as above. |
| `arcade choose`, `arcade check` | Opens the team for three to be chosen, and waits: `RESULT` 1, or 0 when backed out of. Then 1 when two of them are of one species. |
| `arcade begin single`, `arcade begin double` | Starts a challenge with the three chosen: copies at level 50 or under, holding nothing. A streak still running that way is taken up. Say it only once the save is agreed to. The story's `VAR_BATTLE_ARCADE_CHALLENGE_TYPE` keeps its kind. |
| `arcade enter` | Draws the round's trainers and the first one's three (a challenge taken up again keeps what it had), and counts a challenge begun. |
| `arcade pause`, `arcade resume`, `arcade paused`, `arcade forfeit`, `arcade end`, `arcade active`, `arcade way` | As the Hall's: a challenge put down keeps the game it had come to, the cursor's speed and whether it jumps, the round's trainers, the next one's three and what the board has given the challenger's three to hold. |
| `arcade battles`, `arcade next`, `arcade nth`, `arcade round` | As the Hall's: the games of the round through, the one to come, its number in the streak, and the rounds the streak has come through. |
| `arcade draw`, `arcade opponent` | Draws the next trainer's three; stands the trainer drawn in the game room as `opponent`. `{foe}` names them. |
| `arcade board` | Brings up the game board and waits while it is played, then carries out the event its cursor stopped on: `RESULT` is the event's number (0 to 31, the original's `ARCADE_EFFECT_...`), `{event}` its name, `{struck}` whose Pokémon it fell on (the trainer's name, or the player's) and `{item}` what it gave to hold. |
| `arcade event`, `arcade side`, `arcade immune` | That number again; whom the event fell on (0 the trainer, 1 the challenger, 2 the battle or the board, 3 a bonus); and 1 when everyone it fell on was kept from it (a type or an ability that a status can't touch). |
| `arcade free` | Hands over the Battle Points the event gives outright: `RESULT` and `{number}`, 1 or 3 (0 for any other event). |
| `arcade skip` | Counts a battle the board passed as won, with nothing fought. |
| `arcade battle` | The battle, with both threes as the board left them (each other's when it swapped them), under the weather or the Trick Room it set: `RESULT` 1 won, 0 lost. Afterwards the battle is scored for the next board, what the board gave to hold is kept, a level it raised is taken back and both threes are made well. |
| `arcade won`, `arcade done` | Counts the win; 1 once the round's seven are through. |
| `arcade dahlia`, `arcade announce` | Who the battle to come is against (0, 1 the Arcade Star for the silver print, 2 for the gold one); and 1 the one time she is to be announced. |
| `arcade brain`, `arcade beat silver`, `arcade beat gold` | Counts the Arcade Star met; notes her beaten, so that the print waits in the lobby. |
| `arcade finish won`, `arcade finish lost`, `arcade points`, `arcade beaten` | As the Hall's: the records, the Battle Points a round earned, and who was beaten. |

### Amity Square's walk

Plan 06 · R16c; `docs/mechanics/rulings.md`, "Amity Square's walk". The rules are `Overworld/AmitySquare.cs`, and what
the walk remembers is the story's own variables under the original's names (`VAR_FOLLOWER_MON_ACTIVE`, `_PARTY_ID`,
`_SPECIES`, the three of the next find, `VAR_AMITY_SQUARE_STEP_COUNT`, `VAR_AMITY_SQUARE_GIFT_ID`, and our own
`VAR_AMITY_SQUARE_GIFT_MAN_PLACE`), so a script may ask them. The Pokémon out of its ball is the square's
`follower_mon`, off the map behind `FLAG_HIDE_AMITY_SQUARE_FOLLOWER_MON` until it walks; it follows the player by
itself, also on a walk a script sends the player on, and is behind them wherever `place player` puts them. `{walker}`
names the Pokémon last asked about, or the one that walks along.

| Command | What it does |
|---|---|
| `amity arrive` | Draws the day's gift of the man of the park and where he stands (`AmitySquare_OnTransition`: a die of sixteen, then one of five); `RESULT` is his place, 0 to 4. |
| `amity may` | `RESULT` 1 when the team has one of the species the square lets in, four more of them with the National Pokédex (`AmitySquare_CheckPartyHasSpecies`), and the asking starts from the team's first place. |
| `amity next` | The next Pokémon of the team that may walk, an Egg never: `RESULT` 1 and `{walker}` names it, or 0 with none left. |
| `amity walk` | The one last named is out of its ball: `follower_mon` takes its looks and its name and walks behind the player from here, its steps counted from nought and its first find decided (three dice). The script waits while its model is made. |
| `amity leave` | It follows nobody any more (the script takes it off the map by its flag). |
| `amity walker`, `amity grown` | `RESULT` 1 while a Pokémon walks along, which `{walker}` names; and 1 when it is one of the six starters grown up, who have moods of their own. |
| `amity find` | Speaking to it: `RESULT` 0 with fewer than two hundred steps since the last find or nowhere to put what it has, 1 a berry, 2 an accessory, which `{thing}` names. The count starts again and the next find is decided. |
| `amity gift` | What the man offers today: `RESULT` 0 five of a berry, 1 an accessory; `{thing}` names it. |
| `amity take`, `amity give` | What was found, or offered, goes into the bag or the Fashion Case with the item's fanfare and the lines `give` and `giveaccessory` say: `RESULT` 0 when the Fashion Case has no room for it. |

### The camera kit

Plan 09 · L4; style guide, "Story scenes", "The camera kit". The rules are `Overworld/CameraRig.cs`: where the field's
camera looks, its lens, the cinema bars and the field's pace as one state, over whatever camera the place has (the
open country's, a cave's, a room's). Every move eases out of where it is and into where it goes. A move of the
camera's own is waited for, as a pan always was; ended in `nowait` it goes on under whatever comes next, which is how
two are played at once (a path written `nowait`, and the zoom after it). The bars and slow motion never hold the
script up.

| Command | What it does |
|---|---|
| `camera pan X Y [0.8] [nowait]` | The camera looks at a tile's middle instead of the player, and stays. From a spot it was sent to already it eases on from there. |
| `camera follow <who> [0.8] [nowait]` | The camera gets onto someone's feet over that time and keeps on them wherever they walk. `camera follow player` brings it back to the player and leaves the lens, the bars and the pace as they are. |
| `camera path X Y [X Y …] [4] [nowait]` | The camera travels through the middles of up to eight tiles, from where it looks now, and stays at the last. The number left over after the pairs is its pace in tiles a second (0.25 to 16, 4 unless given), which it gathers over half a second and loses over the last half second. The script waits as long as the path takes. |
| `camera zoom 1.5 [0.8] [nowait]` | The lens alone, to a factor of 0.5 to 4: 1 is the place's own, 2 shows everything twice as large, 0.5 twice as much ground. The pitch and the distance don't change, so nothing leans. In a room, which is seen whole, a zoom in lets the camera look where it is sent as far as the zoom leaves room. |
| `letterbox on [0.4]`, `letterbox off [0.4]` | The cinema bars slide in or away (120 units at the top and the bottom): over the field and a script's black screen, under the text box. |
| `slowmo 0.25 [2]` | The field runs at that share of its pace (0.1 to 1) for that many seconds (2 unless given) and is back by itself; `slowmo 1` ends it now. It slows people's steps and gestures, the player's, life, weather and a scene's effects; never the camera, the bars, the text, a bubble, a fade or the script's own waits, so `wait 2` is two seconds at any pace. |
| `camera release [0.6] [nowait]` | Everything back over that time: the camera to the player, the lens to the place's own, the bars away, time to its pace. A script that ends, however it ends, releases what it left; so does a battle lost. |

A reveal, as a chapter writes it: the bars come in while the camera drifts up to the altar, the lens closes on what
appears there, and the moment itself is slowed.

```
script Reveal
  letterbox on
  camera path 31 26 31 23 1.5 nowait
  say "Everything is ready."
  camera zoom 1.4 2 nowait
  effect rift 29 21 2
  wait 0.8
  slowmo 0.3 1.5
  effect burst 29 21
  show dialga
  wait 1.5
  say "At last."
  camera release 1.0
  end
```

### The original's set pieces

Plan 09 · L12; style guide, "Rooms", "Machines at work", and "Life", "Set pieces". Where the original's scripts call a
set piece by name, ours call ours. The rules are `Overworld/SetPieces.cs`: what each piece shows is a function of the
time since its line, on the field's own clock. The counts are the original's; the timing, the art and the sounds are
ours. Every one of them is waited for, by the runner itself, for as long as it takes, and takes no time at all where
the place has nothing to play it on (a room with no machine, someone who isn't on the map), so no script is ever left
waiting on one. Coming to another place ends them all.

| Command | The original's | What it does |
|---|---|---|
| `anim heal` | `PlayPokecenterHealingAnimation` | The healing machine of the room the player is in: a Poké Ball is laid on its tray for each Pokémon of the team that is no Egg (the original counts with `CountPartyNonEggs`), one every 0.35 s, then all of them are lit for the 3.6 s the heal's fanfare sounds, which the machine plays itself; then the tray is empty again. It heals nobody: `heal` does. In a room with no machine there is the fanfare alone, and nothing to wait for. |
| `anim pc on`, `anim pc off` | `PlayPCBootUpAnimation`, `PlayPCShutDownAnimation` | The screen of the PC before the player (the nearest that is used) comes on, with the PC's sound, and stays on; `off` puts out the one that is on, with its sound. 0.4 s and 0.3 s. The PC's own screen (`pc`) plays neither sound: a script that opens it switches it on first and off afterwards, as `common.PC` does. |
| `anim elevator up`, `anim elevator down [4]` | `PlayElevatorAnimation` | A lift's ride in the car the player stands in: the indicator's arrow runs the way it goes, the car hums and shudders, and a chime says it has arrived. The number is how many times it hums (1 to 12; four unless given, as in six of the original's seven scripts, the Battle Tower's having three), half a second each, and the arrival takes half a second more. It moves nobody: the script's own `warp` does. |
| `anim halloffame` | `PlayHallOfFameHealingAnimation` | The Hall of Fame's machine: a ball is set in a socket for each of the team, an Egg's too (`GetPartyCount`), one every 0.35 s, and all of them are lit for 3.6 s to the machine's chime. The balls stay in their sockets until the room is left. |
| `shake <who> [8]` | `ShakeObject` | Someone is shaken where they stand: drawn three texels to either side by turns, so many times over (eight unless given, the original's count for Regigigas; 1 to 32), 0.13 s each. Their tile doesn't change. |
| `flicker <who> [6]` | `FlickerObject` | Someone flickers: unseen, then seen, changing so many times in all (six unless given, the original's count for Cresselia; 1 to 32) a fifth of a second apart, and seen again once it is over. They stay on the map throughout: a script that means them gone says `hide` afterwards, and sets their flag. |

The original's one-off pieces (the lake guardians' machines, the Vista Lighthouse's binoculars, Giratina's shadow over
the Distortion World) are their chapters' own scenes, written with `effect` and the camera kit.

### Scenes that may be skipped

Plan 12 · Q20; style guide, "Field menus and notices", "A scene skipped". A scene is a stretch of a script that takes
the field from the player for a while to show something: people walking, the camera, effects, a long talk, a boss's
speech before a battle. A person who says two lines is not one. Two commands mark it, and nothing else about the
script changes:

| Command | What it does |
|---|---|
| `scene "Name"` | A scene begins. The name is the scene's own in its file (one to forty letters, digits, spaces, hyphens, apostrophes and underscores; quoted, or a bare word), and with the file it is the scene's key in a save: `route_201.The professor`. |
| `endscene` | The scene has been played to its end. That is what makes it **seen**: by the game it was played in (`StoryState.ScenesSeen`, kept in the save) and by this machine (`GameSettings.ScenesSeen`, kept with the options, so a second game knows it). |

```
script OnEnter
  if var VAR_LAKE_ACUITY_STATE != 1 end
  scene "Lake Acuity"
  camera pan 16 45 1.0
  ...
  setvar VAR_LAKE_ACUITY_STATE 2
  endscene
  end
```

**Where the two lines go.** `scene` goes after the questions of the story that lead the scene (`if var … end`, `if flag …
goto Done`), so that a way through the script which never plays the scene never begins it; `endscene` goes after the
last thing the scene sets, before the `end` (or the `return`) of every way out of it. A scene with two ways out has an
`endscene` on each. Neither may be guarded by an `if`: jump to a label and begin the scene there. A scene's battle, its
question and its warp are inside it: the scene goes on in the room it warped to, and after its battle. What it
`call`s is inside it too, so a script called from a scene marks none of its own, and one that ends the whole script
(`end`, as a talk that is also started by itself does) is called after the `endscene`.

**What ends a scene.** Its `endscene`, seen. Or, unseen: the script's `end` on some other way out, a battle lost that
the script couldn't go on from, the `return` of a called script that began it, the game stopping a script that went
wrong, another script started in its place. A scene begun again while it is open (a jump back over its first line, as
a question asked until it hears yes makes) goes on as it was.

**What the files are held to** (`ScriptLibrary.Problems`, so the content check and `StoryTests` both tell it): a scene
left open (no `endscene` below it in its script before another scene begins), an `endscene` with no scene above it, a
scene inside a scene (written out, or begun by a script the scene calls), and two scenes of one name in one place's
file.

**Skipping.** The options' "Skip scenes" says what the player may skip: nothing (`Off`, unless chosen), the scenes this
game or this machine has seen (`Seen`), or any (`Any`). While a scene that may be skipped runs, the menu button held
for a second puts the runner in a **hurry** (`ScriptRunner.Hurry`) until the scene's `endscene`, or until something
that waits for the player. In a hurry:

- nothing is waited for: a `wait`, a walk, a camera's move, an effect, a set piece;
- **a walk is still walked**, unseen: whoever a scene sends walking takes their own steps (the player the player's, so
  a ledge is hopped and a wall, or someone standing in the way, is given up at), in the time the scene gives them.
  The time a hurry doesn't wait (a `wait`, a bubble's seconds, a camera's move, a fade, a gesture, a set piece) passes
  for whoever is walking (`IScriptHost.Elapse`), and where the scene waits for its walks (`walk`, `waitmoves`) everyone
  walks to the end together (`Arrive`). So someone sent ahead with `move` before a `wait` is as far ahead when the
  player's `walk` begins as when the scene is played, and everyone ends on the tile, and facing the way, a played
  scene leaves them. Lines take no time in a hurry: a scene must not count on how long its text is read, played either;
- what is only said, shown or heard is dropped: every `say` and `text` (and the lines `give` and `find` say) unread, a
  bubble, a gesture, a sound, a fanfare, a cry, a shake, an effect that passes, slow motion;
- what leaves something behind is ended at once in the state it leaves: a `warp` arrives, and the place come to runs
  its `OnEnter` when the script is over, as ever; a `fade` is black or gone; the camera is where it was sent, at its
  lens, with the bars in or away; an effect that is held is lit; a PC's screen is on or dark and the Hall of Fame's
  balls lie in their sockets; a change of music is kept;
- **every flag, variable, item, Pokémon, `hide`, `show` and `place` happens exactly as when the scene is played.**

The hurry **stops** before anything that waits for the player or that can't be ended at once, which is then carried out
at the scene's own pace: `ask` and `choose`; a battle (`battle`, `wildbattle`, `catchinglesson`, `sweetscent`, `smashencounter`,
`honeytree battle`, `master`); a screen (`starter`, `shop`, `pc`, `wardrobe`, `travel`, `namerival`, `choosepokemon`, `chooseitem`, `halloffame
show`, `credits`, `hatch`, `poffin cook`, `supercontest enter` and `photo`, `seal capsules` and `shop`, and everything of
`fossil`, `gamecorner`, `villa`, `palpark`, `frontier`, `tower`, `factory`, `hall`, `castle`, `arcade`, `tutor`, `amity`
and of the Underground's tunnels); `save`; a field move (`usemove`, `surf`, `climb`, `fly`, `teleport`, `escape`); and a
puzzle's pieces on the move (`flowerclock`, `pressbutton`, `gearbutton`, `platformlift`, `rotatetiles`). `ScriptRunner.StopsAHurry`
names every command one way or the other, and throws for one it hasn't been told of. After the answer, the battle or
the screen the scene goes on at its own pace, and the button held again skips on. A scene skipped to its `endscene`
counts as seen.

Outside a scene nothing can be skipped, and with the row off nothing is read or drawn. A test plays a script both
ways with `ScriptRunner.Impatient` (someone who holds the button the whole time), and `HeadlessScriptHost` notes what a
hurry dropped (`Unread`, `Dropped`) and tells as a problem anything it reached that it should have stopped before.

## The Pokégear's phone

Plan 20 · J3. Johto's Pokégear keeps its numbers in the story, a flag of ours to each (`FLAG_PHONE_CONTACT_JOEY`,
`Models/Pokegear.cs`), so a script may ask `if flag FLAG_PHONE_CONTACT_JOEY` and the test of flags asked about counts
`addcontact` as setting it.

| Command | What it does |
|---|---|
| `addcontact joey` | Registers a contact of the phone book (`Data/world/johto/phone.json`, by its id) in the Pokégear (the original's `RegisterPhoneNumber`): the host plays the jingle and says so over the field (dropped while a scene is skipped). `RESULT` is 1 for a number new to the phone and 0 for one it had. `ScriptLibrary.Problems` names a contact the book doesn't have. |

A call is a script of `phone.txt`, the contact's id with each word's first letter up (`prof_elm`: `ProfElm`; one with
no script of their own plays `Nobody`), started by the game over the field when the player rings from the Pokégear or
a contact rings on a step. Before it starts the game sets `VAR_PHONE_CALL`, one of the game's own variables
(`Pokegear.KeptByTheGame`): 0 the player rang, 1 the contact rang to talk, 2 a trainer on their day and hour now asks
for a rematch (and from then on waits on their map, spinning, until fought: `battle self rematch`, as the Vs. Seeker's
do), 3 a rematch asked for is still waiting. A trainer who gives a number is a script of their place's file that
battles as `common.Trainer` does and, beaten, asks (`route_30.txt`'s `Joey`).

Hoenn's PokéNav (plan 21 · H2, part b; `Models/PokeNav.cs`) keeps the numbers of its Match Call the same way, by the
same flags: `addcontact` registers a contact of Hoenn's book too (`Data/world/hoenn/phone.json`, written by hand: Mom,
Mr. Stone and the rival so far), whose call is a script of `phone.txt` in the same way (`hoenn_mom`: `HoennMom`). The
PokéNav itself is a flag (`FLAG_SYS_POKENAV_GET`, Emerald's own), and its Match Call page another
(`FLAG_HAS_MATCH_CALL`): a script sets them. Nobody of Hoenn rings on their own yet, and no trainer of Hoenn is in the
book: their rematches by Match Call are a later session's.

## The Ruins of Alph's panels

Plan 20 · J5. A chamber's stone panel is a screen of its own (`UI/AlphPuzzleScreen.cs`, on `Models/AlphPuzzle.cs`).

| Command | What it does |
|---|---|
| `puzzle kabuto` | Opens the panel of that name (`kabuto`, `omanyte`, `aerodactyl`, `hooh`: `AlphPuzzle.Pictures`) and waits until it is left. `RESULT` is 1 when the picture was put back together and 0 when the panel was left as it was; nothing is kept of a panel left half done. Never skipped: a scene in a hurry stops before it. `HeadlessScriptHost.PuzzleSolved` says which a test wants. `ScriptLibrary.Problems` names a panel there isn't. |

## Who is on the map

A person can carry a flag that takes them off the map while it is set (`hiddenBy`) or one they wait for
(`shownBy`). The world's people take the original's own `hiddenBy` from the area file unless their overlay
entry says otherwise (`"hiddenBy": ""` keeps someone whatever the flag). Whoever is off the map is in
`Map.Absent` instead of `Map.NPCs`: not drawn, not in the way, not looking for a battle. The game puts everyone
where the flags have them whenever a flag changes.

People who appear later in the story are hidden at the start, as in the original: the chapter that brings them
sets their flag in `common.NewGame` and clears it in their scene.

## Translating a script

The lines of a script are English, in the file; another language is a sibling file (`Data/text/<tag>/scripts/<file>.json`, `docs/data-files.md`, "Translating the scripts, the overlays and the rooms"). The parser numbers the lines that speak (`Instruction.TextIndex`, counted from nought in the order they are written: `say`, `text`, `ask`, `choose` with its answers, a `speaker` with a name; a line an `if` guards is numbered where it is written), and the runner asks `Text.Line(script, index, k, lines)` for the k-th string of the instruction before it fills the placeholders in. So:

- **A new line goes anywhere**, but rewording one makes its translations stale (they fall back to English and the report says so); inserting one between two others shifts the index of every later one, which the report shows as stale lines up to the end of the script. Add lines at the end of a script when it is easy to.
- **Write a placeholder as the English does** in every language (`{player}`, `{rival}`, `{var:VAR_X}`): a translation may use only those its English line has.
- `sayown` says a person's or a sign's own lines, found by the place and the id the overlay or map file gives them; a person with no id is found by the tile they begin on (`12,3`).
- The questions' Yes and No, and the lines the runner says for itself (a thing found, a prize list, a tutor's question), are the interface's: `ui.json`, section `scriptLines` and `found`.
- `dotnet run --project tools/TextTool -- extract <tag>` writes a language's skeleton, `-- report <tag> --lines` says what is missing.

## Trying a script without the game

`HeadlessScriptHost` is a game with no screen: messages are confirmed as they come (and kept in `Transcript`),
questions take the answers a test queued, battles come out as the test says, walks put people where they end
and note anything they walked into (`Problems`).

```csharp
var host = new HeadlessScriptHost { Map = map };
host.Answers.Enqueue(1);                      // "No" to the first question
var runner = new ScriptRunner(ScriptLibrary.Default, host);
runner.Start(ScriptLibrary.Default.Find("Clown1", "jubilife_city")!, clown);
runner.RunToEnd();
Assert.False(host.Story.Has("FLAG_RECEIVED_COUPON_1"));
```

A chapter's "story walk" test (plan 02) is this, script after script, on a copy of the chapter's maps.

In the game itself, `GameEngine.StartScript(name, subject)` starts one over the field, and the screenshot
harness's `story` mode shows them at work.
