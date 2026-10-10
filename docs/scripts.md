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
  anything.
- **Facing an obstacle or the water** (plan 02 · S2). A thin tree, a cracked rock and a boulder are things of the
  map (`npcType` `CutTree`, `CrackedRock`, `StrengthBoulder`) and run `CutTree`, `Rock` and `Boulder` when the
  player faces one and presses the button. The tile ahead starts one too, as the original's
  `Field_TileBehaviorToScript` does: a rock face along its grain runs `RockFace`, a waterfall faced from the water
  `Waterfall`, and deep water at the player's feet `Water`, which the field offers only to someone who may surf
  (the move and the Fen Badge).
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
- **A gate onto the Cycling Road on foot.** A warp marked for riders only (an overlay's passage with
  `"bicycle": true`) runs `CyclistsOnly` instead of being taken.
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
| `trainerline before` / `after` | The trainer's own line before the battle, or once beaten. `RESULT` is 1 if there was a line to say, 0 if not. |
| `trainerline before pair` | The same for the second of two trainers who came together (`pair`, below), under their own title. |
| `speaker "Name"` / `none` / `self` | Who the following `say` lines are said by. A script starts with `self`. |
| `ask "..."` | A question with Yes and No beside it. Then `if yes` / `if no`. The cancel button answers no. |
| `choose "question" "A" "B" ...` | A question with up to six answers. `RESULT` is the place of the one picked, from 0. The cancel button picks the last, so the way out goes last. |

A line may hold `{player}`, `{assistant}` and `{rival}` (who they are is filled in as it is shown, a speaker's
name too), `{self}`, `{lead}` (the first Pokémon of the team), `{starter}`, `{rivalstarter}` and
`{assistantstarter}` (the one of the three neither child took), `{item}` (the last item given or taken),
`{money}`, `{result}` and `{var:NAME}`; `{member:NAME}` is the nickname of the team's Pokémon at the place the
variable gives (from 0), and `{swarm}` and `{swarmplace}` the species of the day's swarm and where it is,
`{trophygarden}` the Trophy Garden's newest (plan 06 · R13), `{ticket}` the Lottery Corner's number of the day and
`{winner}` the Pokémon whose ID number the last `lottery draw` matched (plan 06 · R14a), and `{berry}`, `{yield}` and
`{mulch}` what the patch of soil the script belongs to holds, as the last `berry` command left them.

### Where a script goes

| Command | |
|---|---|
| `goto Label` | |
| `call Script` | Carries another script out and comes back after its `return` or its last line. |
| `return` | Back to the script that called this one; ends a script nobody called. |
| `end` | Ends everything, whoever called. |
| `if [not] <question> <command>` | Carries the command out when the answer is yes. Any one command may follow, most often `goto`. |

What an `if` can ask:

| Question | |
|---|---|
| `flag FLAG_X` | The flag is set. |
| `var VAR_X == 2`, `var VAR_X >= VAR_Y` | A variable against a number or another variable (`==`, `!=`, `<`, `<=`, `>`, `>=`). The game's own may be read too: `RESULT`, `PLAYER_X`, `PLAYER_Y`, `MONEY`, `PARTY_COUNT`, `BADGE_COUNT`, `GREETINGS` (the people spoken to, `greetings clear` below), `SEEN` (the species of the Sinnoh Pokédex seen), `LEAD_FRIENDSHIP` (the friendship of the first Pokémon of the team, 0 with none: the fan club's woman, plan 06 · R14c). |
| `yes`, `no` | The answer to the last `ask`. |
| `result == 1` | What the last question, battle, handing-over or taking came to. |
| `won`, `lost` | The last battle. |
| `badge coal`, `badges >= 3` | |
| `item "Potion"`, `item "Coupon 1" >= 3` | The bag holds it (that many). |
| `party < 6`, `has "Starly"`, `knows "Cut"` | The team's size, a species on it, a move one of them knows. |
| `defeated self`, `defeated "trainer_id"` | The trainer has been beaten. |
| `taken "item_id"` | An item on the ground has been picked up. |
| `starter "Piplup"` | The species the player took from the briefcase. |
| `money >= 500`, `facing left`, `boy`, `girl` | |
| `poketch` | The player has the Pokétch. |
| `poketchapp MemoPad` | The app is on the Pokétch (the original's `CheckPoketchAppRegistered`). |
| `pokerus` | A Pokémon of the team carries Pokérus now (the nurse's script asks it). |
| `safari` | A Safari Game is under way in the Great Marsh. |
| `partner` | Someone travels with the player (`partner`, below). |
| `rematch self` | The trainer is waiting for a rematch the Vs. Seeker found (plan 06 · R12). |
| `time morning` | The time of day now: `morning`, `day`, `twilight`, `night` or `latenight` (`GameClock`, plan 06 · R14a). |
| `weekday friday` | The day of the week today. |

### What the story remembers

| Command | |
|---|---|
| `setflag FLAG_X`, `clearflag FLAG_X` | Flags are named `FLAG_` and capitals, after the original's where it has one. |
| `setflag own`, `clearflag own` | The script's own flag: the one that hides whoever it belongs to (an item's ball), or that says a hidden item has been found. Setting it is what makes the ball gone for good. |
| `setvar VAR_X 2`, `addvar VAR_X 1` | Variables likewise (`VAR_`). One never set is 0. |

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
| `givebadge coal` | |
| `givemoney 500`, `takemoney 500` | `takemoney` takes all of it or nothing; `RESULT` says which. |
| `heal` | The whole team. |
| `greetings clear` | The count of people spoken to starts again from nought (plan 08 · P12; the original's `ClearSpiritombCounter`). The built-in variable `GREETINGS` is the count: everyone spoken to since, each once (`StoryState.Greeted`, saved). |

### Battles

| Command | |
|---|---|
| `battle self [canlose]` | A trainer battle with someone of the map who is a trainer. Won, the script goes on (and they are beaten for good, prize money paid). Lost, the script ends there and the player wakes up in the last Pokémon Center (at home before any), unless `canlose`: then the team is healed and the script goes on with `if lost`. With no Pokémon able to fight (`IScriptHost.CanBattle`), no battle starts: a line says so and the script ends there. |
| `battle self and other` | Two trainers of the map at once, each with a team of their own, in a double battle. Won, both are beaten. |
| `battle self with cheryl`, `battle self with "cheryl_eterna_forest"` | A tag battle: someone of the map who is a trainer, or a trainer of Platinum's data by id, battles beside the player, with a team of their own and their own mind. The player loses when their own team is down, whatever the partner has left. The words after the first trainer come in any order (`battle a and b with c canlose`). |
| `battle self and pair with partner` | Two trainers who came together, side by side (plan 02 · S6): against the player's two Pokémon, or beside whoever travels with the player (`partner`), who brings a fresh team of their own from Platinum's data, as the original builds it for every battle. Nobody travelling, `with partner` adds no one. |
| `battle self rematch` | A rematch the Vs. Seeker found (plan 06 · R12): the trainer brings the team of the highest level of their row the story has unlocked and not yet beaten (`VsSeeker.RematchTeam`), for this battle only, and stops waiting. |
| `battle self first` | The game's first battle (the rival's on Route 201): no critical hits, as in Platinum. |
| `battle rival as "rival_route_201_turtwig"` | Someone of the map fights with a team of Platinum's data, and is that trainer from then on (who they think like, their items, their prize money): the rival, whose team hangs on the player's starter (plan 02 · S4). A trainer of the `Rival` class is called `{rival}`. |
| `wildbattle "Starly" 2` | A wild Pokémon put in the player's way. `RESULT`: 1 won, 0 lost, 2 fled, 3 caught. Like `battle`, it waits for a Pokémon able to fight. |
| `wildbattle "Giratina" 47 nofleeing` | One that can't be run from (the story's legendaries). |
| `defeat "lass_caroline"` | A trainer of Platinum's data counts as beaten without a battle, in this game and every save of it, and whoever carries that trainer on any map stops wanting to battle: a Leader's script does it for the trainers of the Gym once the Badge is won (the original's `SetTrainerFlag`, plan 01 · M9). |
| `catchinglesson "Bidoof" 2` | The assistant shows how a Pokémon is caught: their own starter at level 5 and twenty Poké Balls, nothing chosen by the player, no critical hit and no miss, and a ball that can't fail. What is caught is the assistant's. `RESULT` is 3. |

### People and the field

| Command | |
|---|---|
| `face <who> up` / `face <who> <whom>` | Turns someone a way, or to look at someone. |
| `walk <who> up 2 left 3 [fast]` | Walks them there and waits. The player takes their own steps (stairs, ledges); other people go where they are sent. |
| `move <who> ...` | Starts the same walk and goes on at once. `waitmoves` waits for everyone still walking. |
| `emote <who> exclaim [0.9]` | A bubble over their head for that long: `exclaim`, `question`, `dots`, `note`, `heart`, `sleep`, `sweat`. |
| `hide <who>`, `show <who>` | Off the map or back on it at once, until the map is come to again. Who is there from one visit to the next is decided by the flags (`hiddenBy`, `shownBy`), so a script that sends someone away for good sets their flag as well. |
| `place <who> X Y [down]` | Puts them on a tile. |
| `warp "Map" X Y [up]` | Through a fade to a tile of another map. |
| `fade out [0.4]`, `fade in` | The field to black and back. Text shows over the black. A script that ends in the dark is brought back. |
| `wait 0.5` | |
| `camera pan X Y [0.8]`, `camera release [0.6]`, `camera shake [0.5]` | Sends the camera to look at a tile, brings it back, shakes the picture. |

### Sound

| Command | |
|---|---|
| `music "sinnoh/jubilife"`, `music area`, `music stop` | A song, the place's own theme again, silence. |
| `fanfare heal` / `item` / `pokemon` / `levelup` / `keyitem` / `tm` / `badge` / `evolution` | `give` and `find` play the fanfare of the item's kind themselves (a TM's, a key item's, or the item's), and `givebadge` the badge's. |
| `sound "select"` | One of the game's sound effects (`AudioManager.SoundNames`; [`sound-effects.md`](sound-effects.md) lists them). |
| `cry "Shinx"`, `cry "Giratina-Origin"`, `cry own` | A Pokémon met in the field cries: a species, or a form by its name, with the original's field echo (`CryMode.FieldEvent`). `cry own` is the cry of the Pokémon the script belongs to (a Pokémon standing in the field, `NPC.Species`; plan 10 · F1). |

### The screens that exist

`starter` (the briefcase's three; `RESULT` is the one taken, 0 to 2), `chooseitem berries` and `chooseitem mulch` (the bag, at the pocket of berries or of items, to choose a berry that grows or a mulch: `RESULT` 1 and the item is the script's last, or 0 when none was chosen; plan 06 · R14a), `choosepokemon` (the party, to choose one of the team: `RESULT` is its place, from 0, or 255 when none was chosen; plan 06 · R12), `pc halloffame` (the PC's Hall of Fame), `shop` (a Mart's counter: BUY, SELL and SEE YA!; `shop "jubilife"` opens a town's own counter of `marts.json`, and a bare `shop` the clerk's own `mart` or else the common counter by the badges), `pc`, `wardrobe` (the player's clothes, slot by slot, changed for nothing: the bedroom's wardrobe; plan 11 · C10; `wardrobe "jubilife"` is a boutique of `clothes.json`, whose stock is tried on and bought with a yes), `travel` (the way to the
next region: the attendant says how things stand; `RESULT` is 0 where no way leads on from here), and the Poffin House's pot (`poffin cook`, below).

### The trainer tools

Plan 06 · R12.

| Command | |
|---|---|
| `trade kazza` | One of the original's trades with people of the game (`Models/NpcTrades.cs`): the Pokémon last chosen with `choosepokemon` goes, the trade's own comes, with its nickname, its trainer and its held item, and is met in a trade. `RESULT` is 1 when the one chosen was the species the trade asks for, and nothing changes hands otherwise. A traded Pokémon from a person never evolves by the trade (the original's `ScrCmd_TradeWithNpc` asks no evolution). |
| `halloffame` | The team enters the Hall of Fame (`Models/HallOfFame.cs`): the entry is written and its screen shows. |

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

### Gyms

Plan 01 · M9. A Gym's puzzle is the room's own (`puzzle` in its map file, `GymPuzzle`): the Hearthome Gym's doors are chosen and the Veilstone Gym's bags and tyres laid out as the player comes in, and a bag is kicked by facing it and pressing the button, all with no script. What a script does to a puzzle is here.

| Command | What it does |
|---|---|
| `pressbutton blue` | The Pastoria Gym's water sets off for the level of a button's colour, `blue` (four tiles), `green` (two) or `orange` (nought), and the script waits while it moves (the original's `PressPastoriaGymButton`; `PastoriaWater`). The room's coordinate events run it as the player steps on a button. |
| `flowerclock` | Turns the Eterna Gym's flower clock on to its next time (`VAR_ETERNA_GYM_FLOWER_CLOCK_STATE`, the original's `AdvanceEternaGymClock`; `EternaClock`) and waits while its hands turn and a fountain drains, the camera on them; `camera release` brings it back. `RESULT` is 0 where the clock is at its last time already, 1 when it turned, 2 when a fountain drained too. |

### Field moves and key items

Plan 02 · S2. These do what a move or an item does; whether it may be used is the script's to ask first
(`if knows "Cut"`, `if badge forest`), or the party menu's, which has asked before a `Use...` script runs.

| Command | What it does |
|---|---|
| `usemove "Cut"` | "{user} used Cut!", then the move's cut-in (the Pokémon on a band across the screen, with its cry). As the band closes, the obstacle the script belongs to gives way (Cut and Rock Smash). `{user}` in a line is the Pokémon that used it: the one chosen in the party menu, or the first of the team who knows the move. The move must be a field move. |
| `surf` | The player rides out onto the water ahead and the script waits for the step. `RESULT` is 0 where there is no water to ride out on. |
| `climb` | Up or down the waterfall or rock face ahead, to its end. `RESULT` 0 where there is none. |
| `fly` | Through a fade to the town chosen on Fly's map, in front of its Pokémon Center (or the player's house in Twinleaf Town). |
| `teleport` | Through a fade to the town of the Pokémon Center last gone into. |
| `escape` | Through a fade out of the caves, to where the player went into them (Dig, an Escape Rope). |
| `sweetscent` | Draws out a wild Pokémon of the place where the player stands, on land or water, and battles it. `RESULT` is 0 when nothing lives there. |
| `poketch on` | Gives the player the Pokétch. |
| `poketchapp PartyStatus` | Puts an app on it, by its name in the original's list (`PoketchApp`). |
| `safari start`, `safari end` | Starts the Great Marsh's Safari Game (30 Safari Balls, 500 steps; the fee is the script's own `takemoney`) or ends it (plan 01 · M7). While one is on, every wild Pokémon is met in a Safari battle, and its last step or ball runs `common.SafariTimeUp` or `common.SafariOutOfBalls`. |
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
| `roamer start "Mesprit"` | Sets a roaming Pokémon loose (`RoamingPokemon_ActivateSlot`): Mesprit, Cresselia, Darkrai, Moltres, Zapdos or Articuno, made afresh at its level and full HP, somewhere at random. |
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

## Who is on the map

A person can carry a flag that takes them off the map while it is set (`hiddenBy`) or one they wait for
(`shownBy`). The world's people take the original's own `hiddenBy` from the area file unless their overlay
entry says otherwise (`"hiddenBy": ""` keeps someone whatever the flag). Whoever is off the map is in
`Map.Absent` instead of `Map.NPCs`: not drawn, not in the way, not looking for a battle. The game puts everyone
where the flags have them whenever a flag changes.

People who appear later in the story are hidden at the start, as in the original: the chapter that brings them
sets their flag in `common.NewGame` and clears it in their scene.

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
