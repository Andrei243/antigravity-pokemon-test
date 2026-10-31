# The player's guide

What this remake does that Pokémon Platinum didn't, and what it does differently, in plain words. The full list of rulings, with the reasons, is [`docs/mechanics/rulings.md`](mechanics/rulings.md); the controls and where the saves are kept are in the [README](../README.md).

## Starting

A new game begins in the player's room in Twinleaf Town with nothing, as Platinum's does: the television, the rival bursting in, the professor on Route 201 and the briefcase. The professor's introduction asks your name, whether you are a boy or a girl, and your rival's name.

The title keeps three save slots. NEW GAME takes the first empty one and says which; DELETE A SAVE frees one. CONTINUE on a slot's card takes that game up. JUKEBOX plays any song, sound, cry or ambience the game has (a song once you have heard it in a game), and CREDITS lists whose work is in the game.

### Ways to play

NEW GAME opens WAYS TO PLAY before the introduction. Press A at once and you play the game as it was made (NORMAL). Otherwise:

- **Presets**: NUZLOCKE (a Pokémon that faints is gone for good, only the first Pokémon met in each place may be caught, and every catch is named) and HARDCORE (the same, at Challenge difficulty under the level cap, without items in battle and in the Set battle style).
- **Difficulty**: Easy (more EXP, cheaper shops), Normal, or Challenge (trainers' teams stronger and smarter).
- **Level cap**: your Pokémon stop growing at the level of the next Gym Leader's ace until you win the Badge.
- **Restrictions**: one type only (Monotype), a smaller party, no items in battle, no Pokémon Center, no catching, no running, set battle style, and the odds of a shiny Pokémon.
- **Randomiser**: the wild and trainers' Pokémon, the gifts and the items found, shuffled from a nine-digit seed you can type or roll.

The choice is saved with the game and never changes. A run that isn't NORMAL is marked on its save card and on the Trainer Card.

## What differs from Platinum

**The battles follow the newest games' rules.** Every game is played by them; what stays Platinum's is the story, the maps, the wild Pokémon and the trainers. The differences you will notice first:

- A critical hit is half as strong again, not twice as strong, and comes less often.
- A burn takes a sixteenth of HP each turn; paralysis halves Speed; Electric types can't be paralysed; sleep lasts one to three turns; a confused Pokémon hurts itself one time in three.
- Steel no longer resists Ghost and Dark, and the Fairy type is there (Clefairy, Togepi and the rest are Fairy types).
- Moves have the newest games' power, accuracy and PP (Tackle is 40 and never misses), and Pokémon learn the newest games' moves by level.
- Weather an ability starts lasts five turns (eight with the weather's rock); Taunt, Encore and Disable are shorter.
- EXP goes to the whole team, a little less to those who didn't fight; effort stops at 252 in a stat.
- A shiny Pokémon is one in 4,096.
- Pokémon can grow past level 100, up to 260, and many species of our own evolve past it in the later regions.

**The regions go on.** After Sinnoh's Hall of Fame the captain on Canalave City's pier sails to Kanto, and each region's Hall of Fame opens the way to the next: Kanto, Johto, Hoenn, Unova. Your whole team comes with you, and Fly reaches back to any region you have been to. A region you cross into meets you at levels above the Champion you beat before it.

## In the field

- **Field moves** are Platinum's: Cut, Fly, Surf, Strength, Defog, Rock Smash, Waterfall and Rock Climb, each with its Badge, used by facing what they clear and pressing A, or from the party menu. With the option FIELD HELPERS on (the default), you need no Pokémon that knows the move: the Badge and the HM in your bag are enough, and a wild Pokémon comes to help.
- **The Pokétch** is the handheld's lower screen: P puts it on the screen, O changes the app, I takes it in hand to touch its buttons with the arrows.
- **The registered key item** (the Bicycle, a rod) is on C: choose REGISTER on it in the bag.
- **Running**: hold Shift once you have the Running Shoes.

## The helps

Each of these is an option, on unless you turn it off:

- **Move hints** in battle say how well a move will do against the foe.
- **Next objective**: the start menu, the Trainer Card and the save screen say what to do next, and the nurse and Mom remind you.
- **Boxes anywhere**: BOXES on the start menu opens the PC's boxes outside a Pokémon Center, and the bag can be sorted.
- **Party tools**: REMEMBER teaches a Pokémon a move it has forgotten, and RENAME names it again.
- **Trainers' lines**: Gym Leaders and others speak in battle as their last Pokémon comes out or runs low.

And these are off unless you turn them on: **autosave** (after a battle, a catch, a heal, a Badge and on coming to a new place), **skip scenes** (hold Enter for a second during a scene you have seen before, or any scene), **pause in the background**, and the **frame limit**.

## When something goes wrong

If the game stops unexpectedly it writes a report into `logs/` beside the save and keeps a copy of the game as it stood (`savegame.crash.json`); your own save is never written. The next start says so. F8 writes a report of the session into `repros/` at any time, which can be played back to show what happened.
