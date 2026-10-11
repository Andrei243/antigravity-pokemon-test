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
`{trophygarden}` the Trophy Garden's newest (plan 06 · R13), `{marsh:1}` to `{marsh:6}` the Pokémon the day has brought to that area of the Great Marsh (the Sinnoh Pokédex's list; plan 01 · M11, part b: the observatory's binoculars), `{ticket}` the Lottery Corner's number of the day and
`{winner}` the Pokémon whose ID number the last `lottery draw` matched (plan 06 · R14a), `{berry}`, `{yield}` and
`{mulch}` what the patch of soil the script belongs to holds, as the last `berry` command left them, and (plan 06 ·
R15) `{daycare:0}` and `{daycare:1}` the Pokémon in each place of the Day Care, `{level:N}` the level its steps there
have brought it to, `{grown:N}` the levels it has grown and `{fee:N}` what taking it back costs, `{left}` the Pokémon
last left there and `{taken}` the one last taken back, and (plan 01 · M11, part c) `{size}` the size of the Pokémon
last measured with `team size` and `{record}` the record it was held against, in centimetres.

### Where a script goes

| Command | |
|---|---|
| `goto Label` | |
| `call Script` | Carries another script out and comes back after its `return` or its last line. |
| `return` | Back to the script that called this one; ends a script nobody called. |
| `end` | Ends everything, whoever called. |
| `if [not] <question> <command>` | Carries the command out when the answer is yes. Any one command may follow, most often `goto`. |
| `random 26` | A die of that many sides (one or more), rolled on the game's own chance: `RESULT` is 0 to one less than its sides (the original's `GetRandom`; plan 01 · M11, part a: the flower shop's berry of the day, the Berry Master's, the Old Chateau's ghosts). A test rolls for it (`HeadlessScriptHost.Rolls`, and 0 once they run out). |

What an `if` can ask:

| Question | |
|---|---|
| `flag FLAG_X` | The flag is set. |
| `var VAR_X == 2`, `var VAR_X >= VAR_Y` | A variable against a number or another variable (`==`, `!=`, `<`, `<=`, `>`, `>=`). The game's own may be read too: `RESULT`, `PLAYER_X`, `PLAYER_Y`, `MONEY`, `PARTY_COUNT`, `BADGE_COUNT`, `GREETINGS` (the people spoken to, `greetings clear` below), `SEEN` (the species of the Sinnoh Pokédex seen), `LEAD_FRIENDSHIP` (the friendship of the first Pokémon of the team, 0 with none: the fan club's woman, plan 06 · R14c), `TURNS` (the turns of every battle this script has fought, added up as the original counts them: a battle's last turn is not one, so a battle won at once is none; the original's `VAR_TOTAL_TURNS_LAST_BATTLE`, for the Pokémon Mansion's maids) and `SAFARI_CAUGHT` (the Pokémon caught in the Safari Game under way, or in the last one until the next begins: `GetCurrentSafariGameCaughtNum`; both plan 01 · M11, part b). |
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
| `nationaldex` | The National Pokédex is the player's (plan 02 · S14; the original's `GetNationalDexEnabled`): the Hall of Fame's script, Mom and the assistant ask it, and whoever the original keeps for the National Pokédex (the assistant's grandfather, Bebe, Mr. Backlot, Byron, Pal Park's workers; plan 02 · S15, part a). The professor's lab gives it (`nationaldex`, "After the Hall of Fame", below). |

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
| `wildbattle "Starly" 2` | A wild Pokémon put in the player's way. `RESULT`: 1 won, 0 lost, 2 fled, 3 caught. Like `battle`, it waits for a Pokémon able to fight. |
| `wildbattle "Giratina" 47 nofleeing` | One that can't be run from (the story's legendaries). |
| `wildbattle "Giratina-Origin" 47` | A form's name in place of its species': the Pokémon is met in that form (plan 02 · S12), and one caught keeps it only as the form's own rule lets it (Giratina's by the Griseous Orb, `FormRules.ByHeldItem`). |
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
| `fossil give` | Hands it over at level 20: `RESULT` 1 into the team, 2 into a box, 0 with none waiting. |
| `gamecorner coins` | `RESULT` is what the Coin Case holds. `{coins}` in a line says it. |
| `gamecorner room 500` | `RESULT` 1 when that many more fit (the case holds 50,000). |
| `gamecorner add 50`, `gamecorner take 50` | Puts coins in or takes them out, all or none: `RESULT` 1 when it did. |
| `gamecorner slots 3` | Sits the player at a machine of that number (0 to 11: its mood is the day's deal for it) and waits until they get up. |
| `gamecorner prize` | Asks which of the exchange's prizes: `RESULT` 1, or 0 for none. `{item}`, `{price}` in coins, and `{thing}` the move a TM holds. |
| `gamecorner buy` | Exchanges coins for the prize chosen: `RESULT` 1, or 0 with too few. |
| `gamecorner hiddenpower` | Reads the Hidden Power of the Pokémon `RESULT` names (after `choosepokemon`): `RESULT` 0 with none chosen, 1 when it knows the move, 2 when it doesn't, 3 for an Egg; `{thing}` is the type. |
| `gamecorner cointrick` | The Coin Case's clown tosses his coin: `RESULT` 0 or 1, the hand it is in. |
| `villa catalogue` | Asks which piece of what the catalogue lists now: `RESULT` 1 for one to order, 2 for one already in the room, 0 for none. `{thing}` names it and `{price}` is its price. |
| `villa choose "Table"` | Names a piece without asking, with the same `RESULT`. |
| `villa is "Piano"` | `RESULT` 1 when that is the piece last chosen. |
| `villa buy` | Pays for the piece chosen, and it is the player's: `RESULT` 0 with too little money. |
| `villa furnish` | Stands what is owned in the room, each piece in its place. |
| `villa owned` | `RESULT` is how many pieces are owned. |
| `palpark check` | `RESULT` 0 when a Catching Show can begin, 1 when the boxes haven't six free places, 2 when six Pokémon don't wait in the park. |
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
| `nationaldex` | The Pokédex becomes the National one (`Pokedex.UnlockNational`; the original's `SetNationalDexEnabled`), as Professor Oak makes it in the professor's lab. It asks nothing and shows nothing: the words and the fanfare are the script's, and so is the question of whether it may be given (`RowanLab.Rowan` asks `flag FLAG_GAME_COMPLETED` and `var SEEN >= 210`, which a test holds to `Pokedex.CanUnlockNational`). `if nationaldex` holds from then on. |

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
