# Sound effects

Every sound effect of the game (plan 05 · A3), what it stands for in the original and when this game plays it. The sounds are our own: synthesised in code the first time each is asked for (`Audio/SoundBank`, built with the layers of `Audio/SoundDesign`: oscillators whose pitch glides, two-operator FM bells, noise through a resonant filter whose cutoff glides, scattered clicks and bubbles, an echo). Nothing is taken from the games; the second column names the original's sound effect (from the decompilation's `res/sound/pl_sound_data.json` and the code that plays it) only as the checklist of what needs a sound, and is empty where the original's name isn't known or there is none.

**How they are heard.** Call `AudioManager.PlaySound(name, pan)`; a script says `sound <name>`. Every sound is brought to its own peak (a menu's blip lowest, about −16 dBFS; a blow in battle highest, about −10 dBFS) and starts and ends at nothing, so the mixer's sound bus needs no balancing of its own; measured as the mixer plays it, each lies within the sound bus's loudness window (`Audio/Loudness`, −38 to −20 dB: [`music-format.md`](music-format.md), "Loudness"). In battle a sound comes from the side of the Pokémon it belongs to: the player's a little to the left, the foe's to the right (`BattleEngine.Sound`). In the field a sound made by someone comes from their side of the screen (`GameEngine.PanAt`, `FieldAmbience.PanOf`: a trainer's "!" or a script's surprise, eight tiles to the side leaning 0.8 of the way), and the player's own from the middle.

**Timing.** In battle a sound is heard when what it belongs to is seen: a move's sound as its line appears, the hit's as it lands (`HitDelay`), the send-out as the ball opens (`BattleAnimator.SendOutBallTime`), each of a thrown ball's wobbles as it begins (`BattleAnimator.BallWobbleAt`) and its click or burst as it settles. In the field a footstep's sound comes as the step begins, as the original's `player_move.c` plays it: in snow, a puddle, water ankle deep, shallow mud and **very** tall grass, and nowhere else (the original's ordinary tall grass and sand are silent). A door's sound comes as it begins to open (`FieldLife.OpenDoor` says so) and as it shuts behind someone who came out (`FieldLife.TakeShutting`); glass doors slide both ways. Going through any other warp is the original's footsteps on stairs, or a warp panel's own sound.

**Not played.** The original's looping low-HP beep: this game changes the battle music instead (plan 05 · A2). The HP bar is silent, as in the original. Sounds whose moment doesn't exist yet are made and checked already, and wait for it (the "waits for" entries below).

**Checking them.** `dotnet run --project tools/MusicRender -- <out dir> --sounds [name ...]` renders each as the mixer plays it, a WAV and a spectrogram, and reports its length, levels, loudest frequency, ends and offset. `SoundTests` holds every sound to the same, holds the set to the plan's checklist, finds every name the engine's code asks for in the bank, and hears a battle through `AudioManager.Listen` (a move, a hit of each strength, a thrown ball wobble by wobble, running away).

## Menus

| Sound | The original's | Played when |
|---|---|---|
| `cursor` | `SEQ_SE_DP_SELECT` | The cursor moves in a menu or a list. |
| `select` | `SEQ_SE_DP_DECIDE` | A choice is made. |
| `cancel` | — | A menu is backed out of, an answer is no. |
| `error` | `SEQ_SE_DP_BEEP` | A choice that can't be made: a move with no PP, a Pokémon that can't battle, an item that can't be used here, money short. |
| `menu_open` | `SEQ_SE_DP_WIN_OPEN` | The start menu opens. |
| `menu_close` | — | The start menu closes. |
| `page` | `SEQ_SE_DP_MEKURU` | A page turns: the bag's pockets, a Pokédex entry's pages, the PC's boxes. |
| `text` | — | The text box moves on to its next line. |
| `levelup` | — | A Pokédex completed, an evolution done: a bright arpeggio. |
| `shutter` | — | The screenshot key (F12) takes a picture of the screen: a shutter's snap and click (plan 12 · Q16). |

## The field

| Sound | The original's | Played when |
|---|---|---|
| `bump` | `SEQ_SE_DP_WALL_HIT` | Walking into something. |
| `ledge` | `SEQ_SE_DP_DANSA4` | Hopping down a ledge. |
| `grass` | `SEQ_SE_DP_KUSA` | A step through very tall grass (the original makes no sound in ordinary tall grass); the Poké Radar setting its patches shaking (plan 06 · R13; the original starts the radar's own theme instead, `SEQ_KUSAGASA`, which is plan 05's). |
| `step_snow` | `SEQ_SE_PL_YUKI` | A step in snow. |
| `step_puddle` | `SEQ_SE_DP_FOOT3_0` | A step through a puddle. |
| `step_shallows` | `SEQ_SE_DP_FOOT3_1` | A step through ankle-deep water. |
| `step_mud` | `SEQ_SE_DP_MARSH_WALK` | A step in mud that isn't deep. |
| `door_open` | `SEQ_SE_DP_DOOR_OPEN` | A door opens as the player steps up to it. |
| `door_close` | `SEQ_SE_DP_DOOR_CLOSE2` | The door shuts behind the player who came out of it. |
| `door_slide` | `SEQ_SE_DP_DOOR10` | Glass doors slide open (a Pokémon Center, a Mart). |
| `stairs` | `SEQ_SE_DP_KAIDAN2` | Going up or down stairs, or through a way out that isn't a door. |
| `warp` | `SEQ_SE_DP_TELE2` | A warp panel. And, until it has a sound of its own, a hut of Amity Square's ruins carrying the player to another (the original's `SEQ_SE_DP_GYURU`; plan 06 · R16c). |
| `exclaim` | — | The "!" over a trainer who has seen the player, or anyone a script surprises. |
| `surf` | — | A Pokémon is ridden out onto the water. |
| `dive` | — | Dive taken down under the sea, or back up from its floor: a plunge and a stream of bubbles (plan 21 · H5). |
| `bridge_walk` | — | A step on, along or off a plank bridge of Hoenn: a dry wooden knock and a short creak as the plank gives (Emerald's `SE_BRIDGE_WALK`; plan 21 · H6, part c). The dip of the walker's sprite is `FieldLife.PlankRows`'s. |
| `bubbles` | — | A diver on the sea floor breathes out: a soft low rush and three bubbles a tenth of a second apart, every 2.8 seconds, as the field shows them (`FieldLife.Bubbles`, plan 21 · H5). |
| `crossing_horn` | — | A ship or a ferry leaves for another region: two long, low blasts of its horn over the engine's throb, as the crossing's dark begins (`travel go` in `common.Crossing`, `RegionDatabase.CrossingSound`; plan 18 · W5). The field's music stops under it, and the region arrived in plays its own. |
| `crossing_engine` | — | A plane leaves for another region: its engines spool up from a low rumble to a steady roar as the crossing's dark begins (plan 18 · W5). |
| `save` | `SEQ_SE_DP_SAVE` | The game is saved. |
| `pc_on` | `SEQ_SE_DP_PC_LOGIN` | A PC is switched on: as its screen comes on in the field (`anim pc on`, plan 09 · L12). |
| `pc_off` | `SEQ_SE_DP_PC_LOGOFF` | A PC is switched off: as its screen goes dark in the field (`anim pc off`). |
| `vs_seeker` | `SE_DP_VS_SEEKER_BEEP_sseq` | The Vs. Seeker is used: it searches for trainers who want a rematch. |
| `phone_ring` | `SEQ_SE_GS_PHONE0` | The Pokégear's phone rings, or a call is placed (plan 20 · J3). |
| `phone_registered` | `SEQ_ME_POKEGEAR_REGIST` | A number is registered in the Pokégear's phone (plan 20 · J3). |
| `heal` | `SEQ_SE_DP_KAIFUKU` | A medicine used from the bag, HP restored in battle. |
| `bike_bell` | `SEQ_SE_DP_JITENSYA` | Getting on the Bicycle (plan 02 · S2). |
| `gear` | `SEQ_SE_DP_GEAR` | The Bicycle's gear changes, on the run button while riding. |
| `boulder` | — | A boulder pushed with Strength, as it starts to slide; and a block of ice of the Mahogany Gym pushed off over the ice (plan 20 · J11). |
| `rock_smash` | — | A rock broken with Rock Smash, as the cut-in's band closes. |
| `cut` | `SEQ_SE_DP_FW015` | A tree cut down with Cut, as the cut-in's band closes. |
| `fish_cast` | — | A rod cast. |
| `fish_bite` | `SEQ_SE_DP_FW104` | Something bites, with the "!" over the player. |
| `fish_reel` | — | The line reeled in: a Pokémon hooked in time. |
| `poketch` | `SEQ_SE_DP_POKETCH_003` | The Pokétch put on the screen or away, its app changed, and most of its apps' buttons touched. |
| `poketch_count` | `SEQ_SE_DP_POKETCH_010` | The Pokétch's Counter counts one more. |
| `poketch_beep` | `SEQ_SE_DP_BEEP` | The Pokétch's short beep for a button that can't be used now (the roulette or the kitchen timer at the wrong moment), and the link searcher done. |
| `coin_flip` | `SEQ_SE_DP_DENSI09` | The Pokétch's coin is tossed. |
| `coin_land` | `SEQ_SE_DP_DENSI10` | The Pokétch's coin lands. |
| `roulette_spin` | `SEQ_SE_DP_POKETCH_011` | The Pokétch's roulette comes to rest. |
| `timer_alarm` | `SEQ_SE_DP_DENSI11` | The Pokétch's kitchen timer runs out. |
| `dowsing_ping` | `SEQ_SE_DP_POKETCH_009` | The Dowsing Machine touched: a ring spreading over the screen. |
| `lift` | `SEQ_SE_DP_ELEBETA` | A platform of the Canalave Gym sets off with the player on it; a platform lift of the League sets off up its shaft (the original's `SEQ_SE_DP_ELEBETA4`, plan 01 · M9 2d); the Vista Lighthouse's lift as Volkner goes down in it (plan 02 · S13; a ride in a lift's car is `lift_hum`'s since plan 09 · L12). |
| `lift_stop` | `SEQ_SE_DP_KI_GASYAN` | The platform comes to its end and locks in place; in the League, also a door of the Elite Four's rooms coming down behind the challenger, and the door on opening once the room's master is beaten. |
| `snowball` | `SEQ_SE_DP_FW291` | A snowball of the Snowpoint Gym bursts as the player slides into it fast enough (`ov5_021E06A8`). |
| `gears` | `SEQ_SE_DP_GAGAGA` | The Sunyshore Gym's gears grind round as the player steps on a button on a hub (`SunyshoreGym_PressButton`). |
| `thunder` | `SEQ_SE_DP_T_KAMI2` | Thunder cracking close, a fifth of a second after a storm's lightning (two strikes of three, as the original's storm chooses). |
| `thunder_rumble` | `SEQ_SE_DP_T_KAMI` | Thunder rolling from further off, a second after the lightning (one strike of three). |
| `effect_burst` | — | A story's scene throws out a burst of light (`effect burst`, plan 09 · L11): a rush of air, a deep knock and a shimmer. The original plays its own sound in each such scene; this one stands for all of them. |
| `effect_beam` | — | A column of light stands on a tile (`effect beam`): a hum rising with a slow waver and air rushing up inside it. |
| `effect_rift` | — | A rift tears open in the air (`effect rift`): noise climbing through a narrow band in fits, over a low growl. |
| `effect_shadow` | — | A great shadow sweeps over the ground (`effect shadow`): a low rush of air swelling and dying over a deep note. |
| `effect_ripple` | — | The picture wavers (`effect ripple`): two soft tones a little apart, wobbling against each other. |
| `effect_sparkles` | — | Points of light rise round a tile (`effect sparkles`): high bells struck here and there, ringing out. |
| `machine_ball` | `SEQ_SE_DP_BOWA` | A Poké Ball is laid on a Pokémon Center's machine (`anim heal`, plan 09 · L12): one to each Pokémon of the team that is no Egg, a soft pop and a small round note, before the heal's fanfare as the balls light. |
| `lift_hum` | `SEQ_SE_DP_ELEBETA2` | A lift's car between floors (`anim elevator`, plan 09 · L12): half a second of a motor's even hum, played once to each round of the indicator's lights (four in every lift but the Battle Tower's, which has three), where the original loops its sound until the lights have gone round. |
| `lift_ding` | `SEQ_SE_DP_PINPON` | A lift arrives at its floor (`anim elevator`): two notes of a chime, the second a third below the first. |

**The Underground** (plan 06 · R16a) has no sounds of its own yet, and plays ones the game already has until plan 05 · A6 gives it its own: `stairs` going down, coming up and through a base's way in; `poketch_beep` for the radar's pulse and as the wall's game begins; in the wall, `ledge` for a blow of the pick, `rock_smash` for one of the hammer, `bump` on bare dark rock, `select` as a thing is bared, `levelup` when the last is out and `boulder` when the wall falls in; `exclaim` as a trap goes off, `cursor` for a turn in a hole or a bubble burst and `bump` for a blow at a trap's boulder; `rock_smash` for the Digger Drill; `levelup` for a flag put up at home.

**The Battle Arcade's game board** (plan 06 · R18d) has no sounds of its own yet either, and plays the menus' until plan 05 · A6 gives it its own: `poketch_beep` for each number of its count, `exclaim` as the events are shown and the cursor sets off, `cursor` as the cursor moves (no oftener than a held key's in a list, though at its fastest it moves every frame), and `select` as it is stopped.

## Battles

| Sound | The original's | Played when |
|---|---|---|
| `send_out` | `SEQ_SE_DP_BOWA4` | A ball opens and a Pokémon comes out. |
| `recall` | — | A Pokémon is called back into its ball. |
| `shiny` | `SEQ_SE_DP_REAPOKE` | A shiny Pokémon sparkles as it comes out of its ball or appears (plan 09 · L1): a run of bright bell notes as the first stars burst. |
| `run_away` | `SEQ_SE_DP_NIGERU` | Getting away from a wild Pokémon. |
| `ball_throw` | `SEQ_SE_DP_NAGERU` | A ball thrown. |
| `ball_shake` | `SEQ_SE_DP_KON` | Each wobble of a thrown ball; each rock of an Egg about to hatch (plan 06 · R15). |
| `ball_click` | `SEQ_SE_DP_GETTING` | The ball clicks shut: caught; each Poké Ball set in a socket of the Hall of Fame's machine (plan 02 · S14; since plan 09 · L12 by the machine's own piece, `anim halloffame`). |
| `ball_break` | — | The ball bursts open: the Pokémon broke free; an Egg breaks open as it hatches. |
| `hit_normal` | `SEQ_SE_DP_KOUKA_M` | A hit lands. |
| `hit_super` | `SEQ_SE_DP_KOUKA_H` | A super-effective hit lands. |
| `hit_weak` | `SEQ_SE_DP_KOUKA_L` | A not very effective hit lands. |
| `stat_up` | — | A stat rises. |
| `stat_down` | — | A stat falls. |
| `status_poison` | — | Poisoned, or badly poisoned; poison biting in the field, every fourth step (plan 06 · R13; the original's `SEQ_SE_DP_DOKU2`). |
| `status_burn` | — | Burned. |
| `status_paralysis` | — | Paralysed. |
| `status_sleep` | — | Fallen asleep. |
| `status_freeze` | — | Frozen solid. |
| `status_confusion` | — | Confused, and each turn it is confused. |
| `faint` | `SEQ_SE_DP_POKE_DEAD3` | A Pokémon faints. |
| `exp` | `SEQ_SE_DP_EXP` | The EXP bar fills. |
| `weather_rain` | — | Rain begins over a battle, and each turn it goes on (plan 25 · Y1; the original plays its rain animation's own sounds, `BattleEngine` the weather's sound with its cue). |
| `weather_sun` | — | The sunlight turns harsh over a battle, and each turn it stays strong. |
| `weather_sandstorm` | — | A sandstorm kicks up over a battle, and each turn it rages. |
| `weather_hail` | — | Hail begins over a battle, and each turn it goes on. |
| `weather_fog` | — | A battle opens in deep fog, and each turn the fog stays. |
| `substitute_in` | — | A Substitute's doll drops in front of its Pokémon, made or passed by Baton Pass (plan 25 · Y1). |
| `substitute_out` | — | A Substitute's doll fades in a puff: broken, its Pokémon fainting or called back. |
| `moment_infatuated` | — | A Pokémon in love as it tries to move (plan 25 · Y3, the original's `BATTLE_ANIMATION_INFATUATED`): two soft bells over a note that wavers. |
| `moment_bag_item` | — | An item from the bag used on a Pokémon that stands on the field, with the line that says who used it (where the original's `subscript_use_potion` plays a sound of its own, number 1516; `heal` is still heard on the line after, as the HP comes back): a quick chime climbing. |
| `moment_escape_item` | — | A Poké Doll or a Fluffy Tail used: a puff of smoke, before `run_away` on the line after (`BATTLE_ANIMATION_ITEM_ESCAPE`). |
| `moment_held_item` | — | A held item goes off (`BATTLE_ANIMATION_HELD_ITEM`): Leftovers, Black Sludge, a Sticky Barb, a berry eaten, a herb, a Focus Sash or Band, a Quick Claw or Custap Berry, a Power Herb, a Destiny Knot, a Toxic or Flame Orb. A ding and its answer. |
| `moment_happy` | — | The Great Marsh's Pokémon as bait lands by it (`BATTLE_ANIMATION_HAPPY`): three chirps hopping up. |
| `moment_eating` | — | The Great Marsh's Pokémon eating the bait (`BATTLE_ANIMATION_EATING`): three bites. |
| `moment_angry` | — | The Great Marsh's Pokémon as mud hits it (`BATTLE_ANIMATION_ANGRY`): steam let off over a growl. |
| `moment_curse` | — | A curse takes its quarter at the turn's end (`BATTLE_ANIMATION_DAMAGE_CURSE`): something driven in a third of a second in, as the mark lands, and a low moan. |
| `moment_nightmare` | — | A nightmare takes its quarter from a sleeper (`BATTLE_ANIMATION_DAMAGE_NIGHTMARE`): two low notes grating, a whisper, a cold bell as it bites. |
| `moment_leech_seed` | — | Leech Seed saps a Pokémon's health (`BATTLE_ANIMATION_DAMAGE_LEECH_SEED`): a breath rising and six motes leaving, each a little higher. |
| `moment_ingrain` | — | Roots draw up a sixteenth (`BATTLE_ANIMATION_DAMAGE_INGRAIN`): the ground creaking, a rising note, a soft bell. |
| `moment_bind` | — | Bind or Wrap tightens (`BATTLE_ANIMATION_DAMAGE_BIND`, `_WRAP`): a creak rising, a knock, and the grip let go. |
| `moment_fire_spin` | — | Fire Spin or Magma Storm whirls round a Pokémon (`BATTLE_ANIMATION_DAMAGE_FIRE_SPIN`, `_MAGMA_STORM`): a roar that comes and goes, crackling. |
| `moment_clamp` | — | Clamp snaps shut (`BATTLE_ANIMATION_DAMAGE_CLAMP`): a rush inward and a hard clack. |
| `moment_whirlpool` | — | Whirlpool turns under a Pokémon (`BATTLE_ANIMATION_DAMAGE_WHIRLPOOL`): a wash that swells and sinks, bubbling. |
| `moment_sand_tomb` | — | Sand Tomb draws a Pokémon down (`BATTLE_ANIMATION_DAMAGE_SAND_TOMB`): a hiss sinking over a low rumble. |
| `mega_evolve` | — | A Pokémon Mega Evolves (plan 25 · Y14; our own, Platinum has none): light gathering in a rising hum, the change of shape 0.8 s in on a blow, three bells and a sparkle (1.6 s). |
| `primal_reversion` | — | Kyogre or Groudon undergoes Primal Reversion: a deep swell, a roaring burst as its shape changes 0.8 s in (1.6 s). |
| `ultra_burst` | — | Necrozma Ultra Bursts: a dark drone, cracks as the light breaks through, a blow and ringing bells as it bursts 0.85 s in (1.8 s). |
| `z_power` | — | A Pokémon surrounds itself with its Z-Power: a rising hum and three notes as the Z is drawn, a bell (1.3 s). |
| `z_move_normal` | — | A Z-Move of the Normal type, and a species' own of that type (`SoundBank.ZMoveSound`): a rush to the landing at 0.35 s and a great blow, then a great blow under a bell (1.5 s). |
| `z_move_fire` | — | A Z-Move of the Fire type, and a species' own of that type (`SoundBank.ZMoveSound`): a rush to the landing at 0.35 s and a great blow, then a roaring blaze crackling (1.5 s). |
| `z_move_water` | — | A Z-Move of the Water type, and a species' own of that type (`SoundBank.ZMoveSound`): a rush to the landing at 0.35 s and a great blow, then a surging wash, bubbling (1.5 s). |
| `z_move_electric` | — | A Z-Move of the Electric type, and a species' own of that type (`SoundBank.ZMoveSound`): a rush to the landing at 0.35 s and a great blow, then four cracks of thunder, one after another (1.5 s). |
| `z_move_grass` | — | A Z-Move of the Grass type, and a species' own of that type (`SoundBank.ZMoveSound`): a rush to the landing at 0.35 s and a great blow, then a rustle and a rising run of notes (1.5 s). |
| `z_move_ice` | — | A Z-Move of the Ice type, and a species' own of that type (`SoundBank.ZMoveSound`): a rush to the landing at 0.35 s and a great blow, then a cold bell and the crackle of ice (1.5 s). |
| `z_move_fighting` | — | A Z-Move of the Fighting type, and a species' own of that type (`SoundBank.ZMoveSound`): a rush to the landing at 0.35 s and a great blow, then ten blows raining down (1.5 s). |
| `z_move_poison` | — | A Z-Move of the Poison type, and a species' own of that type (`SoundBank.ZMoveSound`): a rush to the landing at 0.35 s and a great blow, then bubbling, under a low gurgle (1.5 s). |
| `z_move_ground` | — | A Z-Move of the Ground type, and a species' own of that type (`SoundBank.ZMoveSound`): a rush to the landing at 0.35 s and a great blow, then a rumble and the ground breaking (1.5 s). |
| `z_move_flying` | — | A Z-Move of the Flying type, and a species' own of that type (`SoundBank.ZMoveSound`): a rush to the landing at 0.35 s and a great blow, then a rushing dive of wind (1.5 s). |
| `z_move_psychic` | — | A Z-Move of the Psychic type, and a species' own of that type (`SoundBank.ZMoveSound`): a rush to the landing at 0.35 s and a great blow, then a glide rising, wavering, and a bell (1.5 s). |
| `z_move_bug` | — | A Z-Move of the Bug type, and a species' own of that type (`SoundBank.ZMoveSound`): a rush to the landing at 0.35 s and a great blow, then a buzzing swarm (1.5 s). |
| `z_move_rock` | — | A Z-Move of the Rock type, and a species' own of that type (`SoundBank.ZMoveSound`): a rush to the landing at 0.35 s and a great blow, then six boulders falling (1.5 s). |
| `z_move_ghost` | — | A Z-Move of the Ghost type, and a species' own of that type (`SoundBank.ZMoveSound`): a rush to the landing at 0.35 s and a great blow, then a falling wail over a hollow breath (1.5 s). |
| `z_move_dragon` | — | A Z-Move of the Dragon type, and a species' own of that type (`SoundBank.ZMoveSound`): a rush to the landing at 0.35 s and a great blow, then a growling roar of fire (1.5 s). |
| `z_move_dark` | — | A Z-Move of the Dark type, and a species' own of that type (`SoundBank.ZMoveSound`): a rush to the landing at 0.35 s and a great blow, then a sinking drone drawn into nothing (1.5 s). |
| `z_move_steel` | — | A Z-Move of the Steel type, and a species' own of that type (`SoundBank.ZMoveSound`): a rush to the landing at 0.35 s and a great blow, then a ringing strike and a grinding drill (1.5 s). |
| `z_move_fairy` | — | A Z-Move of the Fairy type, and a species' own of that type (`SoundBank.ZMoveSound`): a rush to the landing at 0.35 s and a great blow, then a twinkling run of notes (1.5 s). |
| `dynamax` | — | A Pokémon Dynamaxes (plan 25 · Y15; our own, Platinum has none): a deep swell rising as it grows, a boom and a blast of cloud 0.9 s in (1.6 s). |
| `gigantamax` | — | A Pokémon Gigantamaxes: the Dynamax's swell and boom, with three cracks of thunder in its clouds and two bells (1.6 s). |
| `dynamax_end` | — | A Dynamax ends: a falling sigh as the Pokémon shrinks back and a gust as its clouds blow away (1.0 s). |
| `dynamax_call` | — | A trainer calls a Dynamax before it grows (plan 25 · YD1; the first of a battle only): a hiss and a falling note as the red beam draws the Pokémon back, a low hum swelling with the ball in the hand, a whoosh as it is thrown 0.8 s in (1.0 s); the Dynamax's own sound follows it. |
| `max_guard` | — | Max Guard: a rising hum and a dome closing on two deep bells (1.2 s). |
| `max_move_normal` | — | A Max Move of the Normal type, and a G-Max Move of that type (`SoundBank.MaxMoveSound`): a deep drone swelling to the landing at 0.35 s and a blow with a boom under it, then a great blow under a bell (1.5 s). |
| `max_move_fire` | — | A Max Move of the Fire type, and a G-Max Move of that type (`SoundBank.MaxMoveSound`): a deep drone swelling to the landing at 0.35 s and a blow with a boom under it, then a roaring blaze crackling (1.5 s). |
| `max_move_water` | — | A Max Move of the Water type, and a G-Max Move of that type (`SoundBank.MaxMoveSound`): a deep drone swelling to the landing at 0.35 s and a blow with a boom under it, then a surging wash, bubbling (1.5 s). |
| `max_move_electric` | — | A Max Move of the Electric type, and a G-Max Move of that type (`SoundBank.MaxMoveSound`): a deep drone swelling to the landing at 0.35 s and a blow with a boom under it, then four cracks of thunder, one after another (1.5 s). |
| `max_move_grass` | — | A Max Move of the Grass type, and a G-Max Move of that type (`SoundBank.MaxMoveSound`): a deep drone swelling to the landing at 0.35 s and a blow with a boom under it, then a rustle and a rising run of notes (1.5 s). |
| `max_move_ice` | — | A Max Move of the Ice type, and a G-Max Move of that type (`SoundBank.MaxMoveSound`): a deep drone swelling to the landing at 0.35 s and a blow with a boom under it, then a cold bell and the crackle of ice (1.5 s). |
| `max_move_fighting` | — | A Max Move of the Fighting type, and a G-Max Move of that type (`SoundBank.MaxMoveSound`): a deep drone swelling to the landing at 0.35 s and a blow with a boom under it, then ten blows raining down (1.5 s). |
| `max_move_poison` | — | A Max Move of the Poison type, and a G-Max Move of that type (`SoundBank.MaxMoveSound`): a deep drone swelling to the landing at 0.35 s and a blow with a boom under it, then bubbling, under a low gurgle (1.5 s). |
| `max_move_ground` | — | A Max Move of the Ground type, and a G-Max Move of that type (`SoundBank.MaxMoveSound`): a deep drone swelling to the landing at 0.35 s and a blow with a boom under it, then a rumble and the ground breaking (1.5 s). |
| `max_move_flying` | — | A Max Move of the Flying type, and a G-Max Move of that type (`SoundBank.MaxMoveSound`): a deep drone swelling to the landing at 0.35 s and a blow with a boom under it, then a rushing dive of wind (1.5 s). |
| `max_move_psychic` | — | A Max Move of the Psychic type, and a G-Max Move of that type (`SoundBank.MaxMoveSound`): a deep drone swelling to the landing at 0.35 s and a blow with a boom under it, then a glide rising, wavering, and a bell (1.5 s). |
| `max_move_bug` | — | A Max Move of the Bug type, and a G-Max Move of that type (`SoundBank.MaxMoveSound`): a deep drone swelling to the landing at 0.35 s and a blow with a boom under it, then a buzzing swarm (1.5 s). |
| `max_move_rock` | — | A Max Move of the Rock type, and a G-Max Move of that type (`SoundBank.MaxMoveSound`): a deep drone swelling to the landing at 0.35 s and a blow with a boom under it, then six boulders falling (1.5 s). |
| `max_move_ghost` | — | A Max Move of the Ghost type, and a G-Max Move of that type (`SoundBank.MaxMoveSound`): a deep drone swelling to the landing at 0.35 s and a blow with a boom under it, then a falling wail over a hollow breath (1.5 s). |
| `max_move_dragon` | — | A Max Move of the Dragon type, and a G-Max Move of that type (`SoundBank.MaxMoveSound`): a deep drone swelling to the landing at 0.35 s and a blow with a boom under it, then a growling roar of fire (1.5 s). |
| `max_move_dark` | — | A Max Move of the Dark type, and a G-Max Move of that type (`SoundBank.MaxMoveSound`): a deep drone swelling to the landing at 0.35 s and a blow with a boom under it, then a sinking drone drawn into nothing (1.5 s). |
| `max_move_steel` | — | A Max Move of the Steel type, and a G-Max Move of that type (`SoundBank.MaxMoveSound`): a deep drone swelling to the landing at 0.35 s and a blow with a boom under it, then a ringing strike and a grinding drill (1.5 s). |
| `max_move_fairy` | — | A Max Move of the Fairy type, and a G-Max Move of that type (`SoundBank.MaxMoveSound`): a deep drone swelling to the landing at 0.35 s and a blow with a boom under it, then a twinkling run of notes (1.5 s). |
| `gmax_wildfire` | — | G-Max Wildfire sets off and lands (plan 25 · YD7, our own; `SoundBank.GMaxSound`, chosen before its type's Max Move sound): the Max Moves' drone and blow at 0.35 s, then a roar of fire springing up all round in five swells, crackling as the walls burn and dying to a hiss by 1.4 s (1.6 s). |
| `gmax_volt_crash` | — | G-Max Volt Crash sets off and lands (plan 25 · YD7, our own): the drone and blow, a thunderclap on the landing, then six cracks running out along the ground 0.05 s apart from 0.43 s and a fizzing tail (1.6 s). |
| `gmax_terror` | — | G-Max Terror sets off and lands (plan 25 · YD7, our own): the drone and blow, a hollow moan rising under the landing, a whisper of wind circling and a low thrum as the eye opens at 0.55 s (1.6 s). |
| `gmax_resonance` | — | G-Max Resonance sets off and lands (plan 25 · YD7, our own): the drone and blow, a slow chord of four bells from 0.38 s over a high shimmering drone, with a cold glassy crackle (1.6 s). |
| `gmax_replenish` | — | G-Max Replenish sets off and lands (plan 25 · YD7, our own): the drone and blow, a soft burst, berries pattering as they land from 0.62 s, and a bright little run of four notes at 0.8 s as the user's side is given its berries back (1.6 s). |
| `max_land` | — | Played under every Max Move's and G-Max Move's impact but Max Guard's (`SoundBank.LandsAsAGiant`; plan Y · YD5, our own): a kick of sub-bass falling under a long low boom, a dull body in the low mids and the air of the blow thrown out after it (1.3 s). With the Battle Scene off it is heard all the same, as the move lands. |
| `max_guard_hit` | — | A blow meets Max Guard's dome (plan 25 · YD3, our own): a move that comes to nothing on a Pokémon whose Max Guard went up this turn, at the blow's impact. Struck glass that rings and dies quickly over a short thud (0.9 s). With the Battle Scene off it is heard all the same. |
| `giant_fall` | — | A giant falls (plan 25 · YD3, our own): on its Dynamax's end it plays at the collapse, 0.3 s after `dynamax_end`'s sigh as the shudder ends; on a faint while giant it plays at once as the body sinks into its clouds. A falling groan and a low crash (1.3 s). With the Battle Scene off it is heard at once. |
| `terastallize` | — | A Pokémon Terastallizes: crystal ringing as its facets lock on, a shatter 0.9 s in, and a chime as the crown stands (1.7 s). |
| `tera_stellar` | — | A Stellar Terastallization: the crystal's ringing and shatter, and a rising run of seven notes (1.7 s). |

**The later mechanics' set pieces** (plan 25 · Y14; `SoundBank.Later.cs`) run up to 2 s, where a move's own sound runs to 1 (`SoundBank.IsSetPiece`, which `SoundTests` allows its 2 s): each plays as its line appears, a Z-Move's with its cue in place of the move's own sound; with the Battle Scene off they are still heard.

**The turn's moments** (plan 25 · Y3; `SoundBank.MomentSound`): each moment of the turn has a sound, heard as its line appears and only where its mark is seen (none on a Pokémon behind a Substitute or out of sight). A condition as it plays on its turn sounds as it did when it was given: `status_sleep` for a Pokémon fast asleep, `status_poison` and `status_burn` as poison and a burn bite at the turn's end, `status_freeze` frozen solid, `status_paralysis` fully paralysed, `status_confusion` confused. A level has none of its own: its fanfare plays with it. An HP change that comes with a moment makes no sound: no `hit_normal` for a poison tick, no `heal` for Leftovers. An attack sent ahead (Future Sight, Doom Desire) lands with its type's `move_…` sound, from the side it lands on. The original plays its animations' own sounds; these stand for no named one.

## Moves

One sound for each type, played as a move of that type sets off (`SoundBank.MoveSound`), from the side of its user; the hit has its own sound as it lands (`hit_normal`, `hit_super`, `hit_weak`). A move aimed at several Pokémon is heard once.

`move_normal`, `move_fire`, `move_water`, `move_grass`, `move_electric`, `move_ice`, `move_fighting`, `move_poison`, `move_ground`, `move_flying`, `move_psychic`, `move_bug`, `move_rock`, `move_ghost`, `move_dragon`, `move_steel`, `move_dark`, `move_fairy`.

**A move's own sound** (plan 25 · Y6 on, decision 3: one entry per move, built from the bank's shared layers): `use_<key>`, the key being the move's name in lower case with its words joined by underscores (`SoundBank.MoveKey`: `use_mud_slap`, `use_will_o_wisp`; the prefix keeps the move Psychic's apart from `move_psychic`). `SoundBank.MoveSound(move, type)` plays it as the move sets off, and the type's `move_…` for a move whose batch hasn't come yet. Its beats are read from the original's animation script (when it rings, what it is made of); its sound is our own, and the entry names the original's first sound of the move. Five moves are the user's own voice and play no sound of the bank: Growl, Roar, Hyper Voice and Howl play the user's cry in the modes of their own below (`SoundBank.CryModesOf`; the second as the first ends), Chatter its plain cry. An attack sent ahead lands with its move's own sound, where it lands.

| Sound | The original's | What it is |
|---|---|---|
| `use_leer` | `SEQ_SE_DP_W043` | Leer: a glint and a low hum as the eyes narrow, twice. |
| `use_pound` | `SEQ_SE_DP_030` | Pound: a flat slap. |
| `use_scratch` | `SEQ_SE_DP_W010` | Scratch: claws raked once, a dry scrape. |
| `use_tackle` | `SEQ_SE_DP_050` | Tackle: a run's rush and a body's thud. |
| `use_withdraw` | `SEQ_SE_DP_W029` | Withdraw: a shell drawn shut, a slide down and a hollow knock. |
| `use_bide` | `SEQ_SE_DP_W025B` | Bide: a low swell held under a pulse, then a thump. |
| `use_return` | `SEQ_SE_DP_W039` | Return: a warm rising chime and a soft blow. |
| `use_quick_attack` | `SEQ_SE_DP_W104` | Quick Attack: a whip of air and a quick knock. |
| `use_absorb` | `SEQ_SE_DP_W071B` | Absorb: a soft draw upward and a little bell. |
| `use_bubble` | `SEQ_SE_DP_W145` | Bubble: a string of bubbles popping. |
| `use_ember` | `SEQ_SE_DP_W052` | Ember: a small flame's puff and crackle. |
| `use_fake_out` | `SEQ_SE_DP_W166` | Fake Out: a sharp clap and its ring. |
| `use_fury_swipes` | `SEQ_SE_DP_W010` | Fury Swipes: two scrapes, quick on one another. |
| `use_poison_gas` | `SEQ_SE_DP_W054` | Poison Gas: a slow hiss of gas, bubbling. |
| `use_screech` | `SEQ_SE_DP_020` | Screech: a grating squeal that wavers. |
| `use_splash` | `SEQ_SE_DP_023` | Splash: a flop and its splash. |
| `use_growth` | `SEQ_SE_DP_W036` | Growth: a green swell climbing. |
| `use_leech_life` | `SEQ_SE_DP_W071` | Leech Life: a needle's tick and a sip drawn back. |
| `use_low_kick` | `SEQ_SE_DP_007` | Low Kick: a low sweep of air and a knock at the feet. |
| `use_supersonic` | `SEQ_SE_DP_W048` | Supersonic: a thin, high wobbling whine. |
| `use_tail_whip` | `SEQ_SE_DP_112` | Tail Whip: a tail swished twice. |
| `use_teleport` | `SEQ_SE_PL_W100` | Teleport: a shimmer rising out of hearing. |
| `use_water_sport` | `SEQ_SE_DP_W057` | Water Sport: water thrown up and pattering down. |
| `use_defense_curl` | `SEQ_SE_DP_W161` | Defense Curl: a body rolled up, a soft curl and a pop. |
| `use_mud_sport` | `SEQ_SE_DP_W091` | Mud Sport: wet mud flung up and slapping down. |
| `use_rock_smash` | `SEQ_SE_DP_W233B` | Rock Smash: a punch and stone cracking. |
| `use_aerial_ace` | `SEQ_SE_DP_W013B` | Aerial Ace: a rush of air and a clean slash. |
| `use_astonish` | `SEQ_SE_DP_W280B` | Astonish: a sudden boo, a jump of pitch and a shiver. |
| `use_brick_break` | `SEQ_SE_DP_W233` | Brick Break: a chop and something breaking. |
| `use_counter` | `SEQ_SE_DP_BASI` | Counter: a turn caught and struck back. |
| `use_earthquake` | `SEQ_SE_DP_W089` | Earthquake: the ground heaving, a long rumble. |
| `use_flash` | `SEQ_SE_DP_W043` | Flash: a bright pop of light ringing. |
| `use_focus_punch` | `SEQ_SE_DP_W082` | Focus Punch: focus gathering, a rush and a heavy blow. |
| `use_force_palm` | `SEQ_SE_DP_208` | Force Palm: a palm's push and a ringing shock. |
| `use_rock_polish` | `SEQ_SE_DP_BRADE` | Rock Polish: stone rubbed bright, a glint at the end. |
| `use_rock_slide` | `SEQ_SE_DP_W088` | Rock Slide: rocks tumbling down. |
| `use_take_down` | `SEQ_SE_DP_W029` | Take Down: a step back, a charge and a hard crash. |
| `use_water_gun` | `SEQ_SE_DP_W055` | Water Gun: a squirt of water. |
| `use_harden` | `SEQ_SE_DP_W231` | Harden: a body set hard, a metal ring. |
| `use_headbutt` | `SEQ_SE_DP_W029` | Headbutt: a run and a hard knock of heads. |
| `use_pursuit` | `SEQ_SE_DP_030` | Pursuit: a dark rush closing in and a blow. |
| `use_rock_throw` | `SEQ_SE_DP_W088` | Rock Throw: stones thrown, landing one, two. |
| `use_stealth_rock` | `SEQ_SE_DP_W070` | Stealth Rock: stones scattered, settling round the foe. |
| `use_focus_energy` | `SEQ_SE_DP_W082` | Focus Energy: breath drawn in and held, rising. |
| `use_secret_power` | — | Secret Power: a hidden blow, a quick rising whisper and a knock. |
| `use_camouflage` | — | Camouflage: colours washing over the user, a soft shimmer sliding down. |
| `use_string_shot` | `SEQ_SE_DP_W081B` | String Shot: silk spat out in a thin hiss, then wound tight. |
| `use_rock_tomb` | `SEQ_SE_DP_W070` | Rock Tomb: rocks dropped one after another, four heavy knocks. |
| `use_water_pulse` | `SEQ_SE_DP_W145C` | Water Pulse: a ball of water throbbing as it rolls, and its break. |
| `use_bullet_seed` | `SEQ_SE_DP_W202B` | Bullet Seed: seeds spat in a quick rattle. |
| `use_charge` | `SEQ_SE_DP_W360` | Charge: a hum of electricity gathering, buzzing louder. |
| `use_charm` | `SEQ_SE_DP_W204` | Charm: two light hops of a chime, sweetly. |
| `use_leech_seed` | `SEQ_SE_DP_001` | Leech Seed: seeds tossed with a soft pop and a sprout's creak. |
| `use_poison_sting` | `SEQ_SE_DP_W004` | Poison Sting: a needle's jab and a fizz. |
| `use_stun_spore` | `SEQ_SE_DP_W077B` | Stun Spore: a puff of powder sifting down, crackling. |
| `use_wing_attack` | `SEQ_SE_DP_W017` | Wing Attack: two beats of wings and a slap. |
| `use_pluck` | `SEQ_SE_DP_030` | Pluck: three quick pecks. |
| `use_baton_pass` | `SEQ_SE_DP_SLOT02` | Baton Pass: a baton whirled, a bright ring as it is passed. |
| `use_foresight` | `SEQ_SE_DP_100` | Foresight: a lens turning into focus, a ping. |
| `use_karate_chop` | `SEQ_SE_DP_HURU` | Karate Chop: a hand cutting the air and a crack. |
| `use_mega_drain` | `SEQ_SE_DP_W145C` | Mega Drain: a long draw upward with a bell at its end. |
| `use_mud_bomb` | `SEQ_SE_DP_W145C` | Mud Bomb: a wet lob and a heavy splat. |
| `use_mud_slap` | `SEQ_SE_DP_W028` | Mud-Slap: mud flung, a wet slap. |
| `use_peck` | `SEQ_SE_DP_W030` | Peck: two hard pecks. |
| `use_rage` | `SEQ_SE_DP_131` | Rage: a low snarl rising and a blow. |
| `use_sand_attack` | `SEQ_SE_DP_W028` | Sand Attack: two dry scatters of sand. |
| `use_sonic_boom` | `SEQ_SE_DP_146` | Sonic Boom: three quick booms of air. |
| `use_hypnosis` | `SEQ_SE_DP_154` | Hypnosis: a slow wavering tone, rising and falling. |
| `use_thunderbolt` | `SEQ_SE_DP_KAMI` | Thunderbolt: thunder cracking down three times. |
| `use_bite` | `SEQ_SE_DP_W044` | Bite: jaws snapping shut. |
| `use_feint_attack` | `SEQ_SE_DP_061` | Feint Attack: a vanishing whoosh, then a blow from behind. |
| `use_toxic` | `SEQ_SE_DP_161` | Toxic: a thick bubbling, fouler and lower. |
| `use_agility` | `SEQ_SE_PL_W097` | Agility: two quick swishes side to side. |
| `use_aqua_jet` | `SEQ_SE_DP_W029` | Aqua Jet: a rushing jet of water and a splash. |
| `use_body_slam` | `SEQ_SE_DP_W036` | Body Slam: a heavy leap and a crash. |
| `use_confuse_ray` | `SEQ_SE_DP_153` | Confuse Ray: an eerie wavering tone circling. |
| `use_crunch` | `SEQ_SE_DP_W044` | Crunch: jaws snapping hard and something breaking. |
| `use_fire_punch` | `SEQ_SE_DP_BASI` | Fire Punch: a punch and a burst of flame. |
| `use_fire_spin` | `SEQ_SE_DP_W221B` | Fire Spin: flames roaring round in a whirl. |
| `use_hidden_power` | `SEQ_SE_DP_W036` | Hidden Power: a soft swell of chimes, then a gathering rush. |
| `use_lock_on` | `SEQ_SE_DP_GETTING` | Lock-On: beeps quickening to a steady tone. |
| `use_magnet_bomb` | `SEQ_SE_DP_290` | Magnet Bomb: metal humming in and four sharp pops. |
| `use_rain_dance` | `SEQ_SE_DP_W240` | Rain Dance: clouds rumbling and rain pattering down. |
| `use_spark` | `SEQ_SE_DP_W209` | Spark: a crackle round the user and a zap. |
| `use_swift` | `SEQ_SE_DP_165` | Swift: a twinkling run of stars. |
| `use_thunder_wave` | `SEQ_SE_DP_W085C` | Thunder Wave: a buzzing ring tightening, crackling. |
| `use_confusion` | `SEQ_SE_DP_480` | Confusion: a warbling ripple of the mind. |
| `use_gust` | `SEQ_SE_DP_W239` | Gust: a gust of wind whirling up. |
| `use_horn_attack` | `SEQ_SE_DP_W030` | Horn Attack: two hard jabs of a horn. |
| `use_rollout` | `SEQ_SE_DP_W025B` | Rollout: a rumbling roll and a knock. |
| `use_sleep_talk` | `SEQ_SE_DP_W173B` | Sleep Talk: a sleepy mumble, low and wobbling. |
| `use_curse` | `SEQ_SE_DP_W185` | Curse: a dark low swell and a shudder. |
| `use_endure` | `SEQ_SE_DP_W082` | Endure: a body braced, a held low pulse. |
| `use_frustration` | `SEQ_SE_DP_W082` | Frustration: an angry huff and a flurry of blows. |
| `use_mean_look` | `SEQ_SE_DP_W060` | Mean Look: a deep stare, a low tone and a hiss. |
| `use_meditate` | `SEQ_SE_DP_W029` | Meditate: a calm breath and a soft bell. |
| `use_protect` | `SEQ_SE_DP_W115` | Protect: a shield springing up with a glassy ring. |
| `use_spite` | `SEQ_SE_DP_W060` | Spite: a thin, sour wail drawn out. |
| `use_night_shade` | `SEQ_SE_DP_W109` | Night Shade: a dark drone and a ghostly whoosh. |
| `use_substitute` | `SEQ_SE_DP_061` | Substitute: a puff of smoke and a pop. |
| `use_cut` | `SEQ_SE_DP_W015` | Cut: one clean slicing swish. |
| `use_thief` | `SEQ_SE_DP_W233` | Thief: a quick snatch and a jingle. |
| `use_grass_knot` | `SEQ_SE_DP_300` | Grass Knot: grass rustling up and pulling tight. |
| `use_magical_leaf` | `SEQ_SE_DP_PASA2` | Magical Leaf: leaves rustling round with a twinkle. |
| `use_razor_leaf` | `SEQ_SE_DP_KAZE` | Razor Leaf: leaves whirring through the air. |
| `use_reflect` | `SEQ_SE_DP_W115` | Reflect: panels of a wall chiming up one after another. |
| `use_safeguard` | `SEQ_SE_DP_W208` | Safeguard: a soft, shining chord settling. |
| `use_smokescreen` | `SEQ_SE_DP_W104` | Smokescreen: a lobbed pop and smoke billowing. |
| `use_sunny_day` | `SEQ_SE_DP_W080` | Sunny Day: a warm swell of light. |
| `use_recycle` | `SEQ_SE_DP_W278` | Recycle: a whirring loop and a ding. |
| `use_taunt` | `SEQ_SE_DP_W118` | Taunt: a mocking two-note call. |
| `use_giga_drain` | `SEQ_SE_DP_W202` | Giga Drain: a great draw upward, leafy, with a bell. |
| `use_night_slash` | `SEQ_SE_DP_BRADE` | Night Slash: a dark blade drawn and two slashes. |
| `use_double_team` | `SEQ_SE_DP_053` | Double Team: four quick swishes, one after another, rising. |
| `use_endeavor` | `SEQ_SE_DP_W039` | Endeavor: four quick flat blows. |
| `use_flame_wheel` | `SEQ_SE_DP_W172` | Flame Wheel: a fiery whirr winding up, then a crackling break. |
| `use_fury_cutter` | `SEQ_SE_DP_W043` | Fury Cutter: three thin cuts of the air, the last with a ring. |
| `use_knock_off` | `SEQ_SE_DP_W233` | Knock Off: a smack and a small thing popping off with a chirp. |
| `use_magnitude` | `SEQ_SE_DP_W070` | Magnitude: the ground heaving: four low thumps and a rumble. |
| `use_seismic_toss` | `SEQ_SE_DP_W327` | Seismic Toss: a whoosh up, a whoosh down, a heavy slam and a rebound. |
| `use_self_destruct` | `SEQ_SE_DP_W120` | Self-Destruct: a fuse hissing higher and a great boom. |
| `use_slam` | `SEQ_SE_DP_W025B` | Slam: a whip through the air and a hard slap. |
| `use_stomp` | `SEQ_SE_DP_W036` | Stomp: a heavy thud and a knock. |
| `use_block` | `SEQ_SE_DP_050` | Block: two hard clanks of metal and a low ring. |
| `use_detect` | `SEQ_SE_DP_081` | Detect: a bright rising chime and a soft ring. |
| `use_mind_reader` | `SEQ_SE_DP_W109` | Mind Reader: a scanning whir sweeping up and down, a ping. |
| `use_disable` | `SEQ_SE_DP_081` | Disable: a tone sagging out of tune and a dull pop. |
| `use_dragon_rage` | `SEQ_SE_DP_W082C` | Dragon Rage: a roaring blaze rushing over and its blast, twice. |
| `use_extrasensory` | `SEQ_SE_DP_W020` | Extrasensory: three throbbing pulses rising and a ring. |
| `use_imprison` | `SEQ_SE_DP_050` | Imprison: five clinks of bars dropped in turn and a deep lock. |
| `use_sand_tomb` | `SEQ_SE_DP_W028` | Sand Tomb: gritty sand swirling round and round. |
| `use_sandstorm` | `SEQ_SE_DP_W201` | Sandstorm: a long wind howling with sand in it. |
| `use_tickle` | `SEQ_SE_DP_W197` | Tickle: four squeaky giggles over a sparkle. |
| `use_uproar` | `SEQ_SE_DP_W253` | Uproar: a loud blaring din, three times. |
| `use_double_slap` | `SEQ_SE_DP_030C` | Double Slap: two flat slaps. |
| `use_follow_me` | `SEQ_SE_DP_W213` | Follow Me: a bright call for attention and three bouncy pops. |
| `use_sing` | `SEQ_SE_DP_W047` | Sing: a soft lullaby of three long notes. |
| `use_bug_bite` | `SEQ_SE_DP_W025B` | Bug Bite: four quick crunching bites. |
| `use_encore` | `SEQ_SE_DP_W227B` | Encore: five claps in a row over a little tune. |
| `use_explosion` | `SEQ_SE_DP_SHUSHU` | Explosion: a fuse hissing higher, a huge boom and a long roll. |
| `use_flail` | `SEQ_SE_DP_050` | Flail: six frantic flaps and a thump. |
| `use_ice_fang` | `SEQ_SE_DP_W044` | Ice Fang: a chomp and an icy crackle with a chime. |
| `use_lucky_chant` | `SEQ_SE_DP_163` | Lucky Chant: bright bells climbing in a chant. |
| `use_mimic` | `SEQ_SE_DP_W107` | Mimic: a warbling tone copied a beat later and a pop. |
| `use_rest` | `SEQ_SE_DP_W173B` | Rest: two slow snoring breaths and a soft bell. |
| `use_revenge` | `SEQ_SE_DP_W036` | Revenge: a low growl gathering and one heavy punch. |
| `use_rock_blast` | `SEQ_SE_DP_W207` | Rock Blast: four rocks flung with knocks and rubble. |
| `use_snore` | `SEQ_SE_DP_W173B` | Snore: two big rumbling snores. |
| `use_sweet_scent` | `SEQ_SE_DP_W230` | Sweet Scent: a soft sweep of chimes with a shimmer. |
| `use_twister` | `SEQ_SE_DP_060` | Twister: a wind spiralling up and round. |
| `use_worry_seed` | `SEQ_SE_DP_001` | Worry Seed: a seed tossed with a plop and two uneasy notes. |
| `use_bubble_beam` | `SEQ_SE_DP_AWA` | Bubble Beam: a stream of bubbling pops and a final burst. |
| `use_mach_punch` | `SEQ_SE_DP_W010` | Mach Punch: a fast whoosh and a flat crack. |
| `use_metal_claw` | `SEQ_SE_DP_W231` | Metal Claw: two metallic rakes, each with a ring. |
| `use_future_sight` | `SEQ_SE_DP_W060` | Future Sight: a pulsing hum and a foggy chime. |
| `use_pain_split` | `SEQ_SE_DP_W025B` | Pain Split: six pangs, one side and the other in turn. |
| `use_psybeam` | `SEQ_SE_DP_W060C` | Psybeam: a wobbling beam crossing the field and a dull bang. |
| `use_shadow_ball` | `SEQ_SE_DP_W028` | Shadow Ball: a dark swirl building, a throw and a dull burst. |
| `use_shadow_claw` | `SEQ_SE_DP_161` | Shadow Claw: two ghostly rakes and a hollow toll. |
| `use_shadow_sneak` | `SEQ_SE_DP_161` | Shadow Sneak: a hush along the ground and a muffled strike. |
| `use_spit_up` | `SEQ_SE_DP_W036` | Spit Up: a heave, a pop and a wet splat. |
| `use_stockpile` | `SEQ_SE_DP_W025` | Stockpile: two swelling breaths, each ending in a soft ping. |
| `use_sucker_punch` | `SEQ_SE_DP_143` | Sucker Punch: a sudden dull smack and a second, smaller. |
| `use_swallow` | `SEQ_SE_DP_W036` | Swallow: a gathering rush, a gulp and a soft chime. |
| `use_will_o_wisp` | `SEQ_SE_DP_W052` | Will-O-Wisp: a flickering cold wind that ignites with a whoosh. |
| `use_minimize` | `SEQ_SE_DP_W107` | Minimize: a tone sliding down and shrinking, with little pops. |
| `use_payback` | `SEQ_SE_DP_W185` | Payback: a heavy growl gathering and four hard knocks with a jingle. |
| `use_attract` | `SEQ_SE_DP_W204` | Attract: a sweet lilt, two kisses and a bell. |
| `use_amnesia` | `SEQ_SE_DP_W118` | Amnesia: a hollow mutter and a bubbling drift away. |
| `use_aqua_ring` | `SEQ_SE_DP_W392` | Aqua Ring: a gentle watery shimmer closing into a ring. |
| `use_copycat` | `SEQ_SE_DP_SHUSHU` | Copycat: a rising hiss and a tune repeated a beat later. |
| `use_fury_attack` | `SEQ_SE_DP_W030` | Fury Attack: four quick jabs. |
| `use_jump_kick` | `SEQ_SE_DP_131` | Jump Kick: a spring, a whoosh and one hard kick. |
| `use_light_screen` | `SEQ_SE_DP_W115` | Light Screen: a curtain of chimes shimmering up. |
| `use_nasty_plot` | `SEQ_SE_DP_W118` | Nasty Plot: a low scheming murmur and a sly rising note. |
| `use_refresh` | `SEQ_SE_DP_W287` | Refresh: a clean breeze of chimes. |
| `use_soft_boiled` | `SEQ_SE_DP_112` | Soft-Boiled: a wobbling egg, a crack and a warm bell. |
| `use_steel_wing` | `SEQ_SE_DP_W231` | Steel Wing: two metal whooshes and four ringing strikes. |
| `use_sweet_kiss` | `SEQ_SE_DP_041` | Sweet Kiss: a kiss and a dizzy little tune. |
| `use_whirlwind` | `SEQ_SE_DP_KAZE` | Whirlwind: a great wind rising and a gust carrying off. |
| `use_mirror_move` | — | Mirror Move: a bright silver ping and a glassy ring (the original plays none of its own). |
| `use_haze` | `SEQ_SE_DP_W114` | Haze: a long cold hiss and a murmur of mist. |
| `use_defog` | `SEQ_SE_DP_BFBRADE` | Defog: a wind rushing across the field and clearing. |
| `use_dig` | `SEQ_SE_DP_W091` | Dig: dirt scrabbled three times, a burst out of the ground and a thud. |
| `use_yawn` | `SEQ_SE_DP_W281` | Yawn: a long drowsy sigh carried across. |
| `use_acupressure` | `SEQ_SE_DP_081` | Acupressure: three presses, each with a ping, and a warm chime. |
| `use_air_cutter` | `SEQ_SE_DP_W104` | Air Cutter: a whoosh and two cuts, crossing. |
| `use_false_swipe` | `SEQ_SE_DP_W104` | False Swipe: a whoosh and a soft tap of a blade. |
| `use_feint` | `SEQ_SE_DP_W104` | Feint: two faint whooshes, then one sharp quick hit. |
| `use_metronome` | `SEQ_SE_DP_W118` | Metronome: a pendulum ticking and a bright flourish. |
| `use_pin_missile` | `SEQ_SE_DP_W026` | Pin Missile: three light whistling needles, each with a prick. |
| `use_roost` | `SEQ_SE_DP_210` | Roost: wings settling, a soft sigh and a chime. |
| `use_scary_face` | `SEQ_SE_DP_W060` | Scary Face: a sudden low roar-hiss and a stab of high notes. |
| `use_shock_wave` | `SEQ_SE_DP_W082` | Shock Wave: a rising crackle and a crooked zap. |
| `use_slash` | `SEQ_SE_DP_W013` | Slash: three quick sharp swishes, the last with a ring. |
| `use_swagger` | `SEQ_SE_DP_W207D` | Swagger: a cocky rising fanfare and two hard snorts. |
| `use_thunder_shock` | `SEQ_SE_DP_W085` | Thunder Shock: a short sharp zap and a crackle. |
| `use_toxic_spikes` | `SEQ_SE_DP_W026` | Toxic Spikes: five spikes thrown with a prick each, and bubbling. |
| `use_wake_up_slap` | `SEQ_SE_DP_030C` | Wake-Up Slap: four slaps and a ring. |
| `use_cosmic_power` | `SEQ_SE_DP_W322` | Cosmic Power: a deep space hum with twinkling bells. |
| `use_lick` | `SEQ_SE_DP_W122` | Lick: a wet slurp. |
| `use_air_slash` | `SEQ_SE_DP_BRADE` | Air Slash: a gathering wind and one sharp slash with a ring. |
| `use_ancient_power` | `SEQ_SE_DP_W082` | Ancient Power: a low rumble, rocks clattering up and a heavy burst of stones. |
| `use_aqua_tail` | `SEQ_SE_DP_W057` | Aqua Tail: a coil of rushing water, a crack of the whip and a splash. |
| `use_assurance` | `SEQ_SE_DP_050` | Assurance: a dull blow and a heavier one behind it. |
| `use_calm_mind` | `SEQ_SE_DP_W048` | Calm Mind: a still, low hum and soft bells settling. |
| `use_double_hit` | `SEQ_SE_DP_030` | Double Hit: two swings, each with a thud. |
| `use_dragon_claw` | `SEQ_SE_DP_W221B` | Dragon Claw: a growling roar rising and two ringing rakes. |
| `use_hyper_fang` | `SEQ_SE_DP_W044` | Hyper Fang: a snap of teeth and a heavy crunch. |
| `use_ingrain` | `SEQ_SE_DP_W010` | Ingrain: roots creeping into the ground with three soft thuds. |
| `use_iron_defense` | `SEQ_SE_DP_W231` | Iron Defense: two bright metallic rings, each sweeping up. |
| `use_mist` | `SEQ_SE_DP_W109` | Mist: a long, soft hiss of mist and a faint shimmer. |
| `use_poison_powder` | `SEQ_SE_DP_W077B` | Poison Powder: a puff of powder and a long sprinkling hiss. |
| `use_submission` | `SEQ_SE_DP_W104` | Submission: a grip and a strained wrench, then five blows. |
| `use_vital_throw` | `SEQ_SE_DP_W233` | Vital Throw: a spinning whoosh up and over, and a heavy slam. |
| `use_whirlpool` | `SEQ_SE_DP_W250` | Whirlpool: water churning round and round with bubbles. |
| `use_beat_up` | `SEQ_SE_DP_W233B` | Beat Up: three quick smacks and a heavier one. |
| `use_fire_fang` | `SEQ_SE_DP_W044` | Fire Fang: a snap of teeth that ignites into a crackling roar. |
| `use_odor_sleuth` | `SEQ_SE_DP_W207` | Odor Sleuth: four quick sniffs and a bark. |
| `use_kinesis` | `SEQ_SE_DP_W060` | Kinesis: a wavering tone and a bell struck at each swing. |
| `use_metal_sound` | `SEQ_SE_DP_W103` | Metal Sound: a shrieking scrape of metal. |
| `use_miracle_eye` | `SEQ_SE_DP_183` | Miracle Eye: a rising tone, three notes and a bright bell. |
| `use_ominous_wind` | `SEQ_SE_DP_W466` | Ominous Wind: a cold wind moaning past twice. |
| `use_psychic` | `SEQ_SE_DP_480` | Psychic: a wobbling tone climbing and a heavy crush. |
| `use_wrap` | `SEQ_SE_DP_W020B` | Wrap: a rustling coil winding round, twice. |
| `use_embargo` | `SEQ_SE_DP_W373` | Embargo: two falling buzzes and a hard stamp. |
| `use_gravity` | `SEQ_SE_DP_W356` | Gravity: a deep tone sinking and a heavy press. |
| `use_psycho_cut` | `SEQ_SE_DP_020` | Psycho Cut: a humming gather and three sharp cuts rising. |
| `use_torment` | `SEQ_SE_DP_W029` | Torment: six sharp jabs and a sour growl. |
| `use_assist` | `SEQ_SE_DP_W010` | Assist: four quick ticks, each with a bell, and a bright pop. |
| `use_bone_rush` | `SEQ_SE_DP_W155` | Bone Rush: a bone whirring in, two hollow knocks and rattling. |
| `use_captivate` | `SEQ_SE_DP_164` | Captivate: a sweet rising lilt and a shimmer of sparkles. |
| `use_drain_punch` | `SEQ_SE_DP_050` | Drain Punch: a solid punch and a long draw upward. |
| `use_fly` | `SEQ_SE_DP_W327` | Fly: a whoosh up, a whistle down and a hard landing. |
| `use_recover` | `SEQ_SE_DP_W025` | Recover: a warm swelling tone, three bells climbing and a chime. |
| `use_snatch` | `SEQ_SE_DP_W036` | Snatch: a swift grab, a small pop and a sly jingle. |
| `use_strength` | `SEQ_SE_DP_W036` | Strength: a straining groan and a heavy shove with rubble. |
| `use_ice_beam` | `SEQ_SE_DP_025` | Ice Beam: a thin icy beam and frost crackling to a chime. |
| `use_poison_fang` | `SEQ_SE_DP_W044` | Poison Fang: a snap of teeth and a bubbling ooze. |
| `use_poison_jab` | `SEQ_SE_DP_030` | Poison Jab: two quick jabs, each with a wet pop. |
| `use_signal_beam` | `SEQ_SE_DP_W062` | Signal Beam: a beam of pulsing tones changing in turn. |
| `use_sludge_bomb` | `SEQ_SE_DP_W145B` | Sludge Bomb: a gurgling lob and a wet splat. |
| `use_tri_attack` | `SEQ_SE_DP_W161` | Tri Attack: a rising gather and three different bursts in turn. |
| `use_memento` | `SEQ_SE_DP_W060` | Memento: a dark sinking drone and a tolling bell. |
| `use_drill_peck` | `SEQ_SE_DP_W029` | Drill Peck: a rising whir and eight quick pecks. |
| `use_ice_punch` | `SEQ_SE_DP_BASI` | Ice Punch: a hard punch and a crackle of frost with a chime. |
| `use_gyro_ball` | `SEQ_SE_DP_W360` | Gyro Ball: a whirring spin rising and a metallic crash. |
| `use_x_scissor` | `SEQ_SE_DP_W015` | X-Scissor: two crossing cuts, each with a ring. |
| `use_horn_drill` | `SEQ_SE_DP_W030` | Horn Drill: a drilling whine, three times, each ending in a knock. |
| `use_last_resort` | `SEQ_SE_DP_W387` | Last Resort: four notes drawn in and two crushing blows. |
| `use_magnet_rise` | `SEQ_SE_DP_W085B` | Magnet Rise: a humming field rising with two bells. |
| `use_smog` | `SEQ_SE_DP_W109` | Smog: three wheezing belches and a thick hiss. |
| `use_vine_whip` | `SEQ_SE_PL_WHIP` | Vine Whip: a whistle through the air, a crack and rustling leaves. |
| `use_dream_eater` | `SEQ_SE_DP_153` | Dream Eater: soft dreamy notes drawn in with a gulp and a chime. |
| `use_grass_whistle` | `SEQ_SE_DP_W320` | Grass Whistle: a bright whistle, a lilting tune and a hush. |
| `use_reversal` | `SEQ_SE_DP_W197` | Reversal: two bright surges, a blow and a crushing second blow. |
| `use_psych_up` | `SEQ_SE_DP_W060B` | Psych Up: a rising shimmer, a run of bells and a chime as it settles. |
| `use_thunder` | `SEQ_SE_DP_W161B` | Thunder: a rumble, a flash-crack of lightning and a long rolling boom. |
| `use_aurora_beam` | `SEQ_SE_DP_W062` | Aurora Beam: a bright rising glide with a ripple of bells and a sparkle. |
| `use_barrier` | `SEQ_SE_DP_W112B` | Barrier: five panes of glass set one after another, each with a ring. |
| `use_double_edge` | `SEQ_SE_DP_W025` | Double-Edge: a rush, a crash and a rebounding knock on the user. |
| `use_thrash` | `SEQ_SE_DP_007` | Thrash: a growling rage and three wild blows. |
| `use_trick_room` | `SEQ_SE_DP_061` | Trick Room: a low hum with a strange run of notes turning back on themselves. |
| `use_brine` | `SEQ_SE_DP_W362` | Brine: a stinging spray and a splash. |
| `use_mud_shot` | `SEQ_SE_DP_W250` | Mud Shot: three wet squelches and a heavy splat. |
| `use_waterfall` | `SEQ_SE_DP_W152` | Waterfall: a pouring torrent, a rise and a crashing splash. |
| `use_bind` | `SEQ_SE_DP_W020B` | Bind: two creaking squeezes. |
| `use_silver_wind` | `SEQ_SE_DP_W016` | Silver Wind: a long gust with a glitter of high notes. |
| `use_synthesis` | `SEQ_SE_DP_W025` | Synthesis: a warm rising tone with soft chimes. |
| `use_thunder_punch` | `SEQ_SE_DP_BASI` | Thunder Punch: a heavy punch and a crackle of lightning. |
| `use_helping_hand` | `SEQ_SE_DP_W227` | Helping Hand: two pats and a rising chime. |
| `use_bounce` | `SEQ_SE_PL_W100_1` | Bounce: a spring up, a hush in the air and a heavy landing. |
| `use_egg_bomb` | `SEQ_SE_DP_W039` | Egg Bomb: a lobbed whistle and a rolling blast. |
| `use_fling` | `SEQ_SE_DP_W374` | Fling: a fast whip through the air and a hard knock. |
| `use_meteor_mash` | `SEQ_SE_DP_W112` | Meteor Mash: a falling comet, a boom and a ringing steel punch. |
| `use_petal_dance` | `SEQ_SE_DP_W080` | Petal Dance: a rustling whirl of petals and three flung gusts. |
| `use_wish` | `SEQ_SE_DP_W115` | Wish: a falling star's whistle, a warm bell and a rising twinkle. |
| `use_u_turn` | `SEQ_SE_DP_080` | U-turn: a dash, a hit and a swerving dash back. |
| `use_flash_cannon` | `SEQ_SE_DP_W082` | Flash Cannon: a rising whine, a flash of high light and a boom. |
| `use_iron_tail` | `SEQ_SE_DP_W231` | Iron Tail: a ring of steel, a heavy swing and a clang. |
| `use_metal_burst` | `SEQ_SE_DP_W368` | Metal Burst: two rising rings of metal and a bursting boom. |
| `use_skill_swap` | `SEQ_SE_DP_W179` | Skill Swap: six bell pings that change pitch halfway. |
| `use_stone_edge` | `SEQ_SE_DP_W088` | Stone Edge: six stones thrust up one after another and a heavy shatter. |
| `use_dragon_breath` | `SEQ_SE_DP_400` | Dragon Breath: a growl and two puffs of roaring fire. |
| `use_close_combat` | `SEQ_SE_DP_003` | Close Combat: eight quick blows, each a notch heavier, and a last thud. |
| `use_dizzy_punch` | `SEQ_SE_DP_W004` | Dizzy Punch: two punches and a wobbling run of dizzy notes. |
| `use_high_jump_kick` | `SEQ_SE_DP_W026` | High Jump Kick: a spring up, a whoosh down and a heavy kick. |
| `use_cross_chop` | `SEQ_SE_DP_W025` | Cross Chop: two chops in turn and a hard cross-chop with a ring. |
| `use_ice_shard` | `SEQ_SE_DP_KAZE` | Ice Shard: an icy gathering, a chime and a sharp cracking hit. |
| `use_swords_dance` | `SEQ_SE_DP_SHUSHU` | Swords Dance: four blade-swishes round a rising hum and a bright ring. |
| `use_wood_hammer` | `SEQ_SE_DP_W452` | Wood Hammer: a swing, a hollow wooden crash and a rustle of leaves. |
| `use_heal_bell` | `SEQ_SE_DP_W234` | Heal Bell: three ringing bells with a soft run of high notes over them. |
| `use_psywave` | `SEQ_SE_DP_181` | Psywave: three wobbling psychic waves and a ringing break. |
| `use_fire_blast` | `SEQ_SE_DP_W172` | Fire Blast: a roaring gather, a flash and a great burning boom. |
| `use_flamethrower` | `SEQ_SE_DP_W053` | Flamethrower: a long steady roar of flame with a crackle. |
| `use_punishment` | `SEQ_SE_DP_W104` | Punishment: a whip-crack, a thud and three stern beeps. |
| `use_conversion_2` | `SEQ_SE_DP_W112` | Conversion 2: a scanning sweep and a run of changing notes. |
| `use_icy_wind` | `SEQ_SE_DP_W016B` | Icy Wind: a long cold wind with chimes of frost. |
| `use_shadow_punch` | `SEQ_SE_DP_W026` | Shadow Punch: a hollow ghostly rush and a cold dull blow. |
| `use_hail` | `SEQ_SE_DP_W258` | Hail: a rattle of ice and many small stones hitting. |
| `use_rock_climb` | `SEQ_SE_DP_W088` | Rock Climb: four stone steps heaving up and a charge into a crash. |
| `use_avalanche` | `SEQ_SE_DP_W419` | Avalanche: a long rumbling slide of snow and a heavy pile-up. |
| `use_blizzard` | `SEQ_SE_DP_KAZE` | Blizzard: a howling snowstorm, bells of frost and a freezing crack. |
| `use_focus_blast` | `SEQ_SE_DP_W062` | Focus Blast: a rising gather of power, a throw and a great blast. |
| `use_discharge` | `SEQ_SE_DP_W085B` | Discharge: a charging whine and a long crackle of lightning every way. |
| `use_dragon_dance` | `SEQ_SE_DP_CLIMAX09_1` | Dragon Dance: three deep beats with bells and a winding dragon tune. |
| `use_hydro_pump` | `SEQ_SE_DP_SUIRYU` | Hydro Pump: a hammering jet of water and a great splash. |
| `use_muddy_water` | `SEQ_SE_DP_W057` | Muddy Water: a thick heaving surge of brown water and bubbling mud. |
| `use_thunder_fang` | `SEQ_SE_DP_W044` | Thunder Fang: a snap of teeth and a crackle of lightning. |
| `use_zen_headbutt` | `SEQ_SE_DP_293` | Zen Headbutt: a gathering hum, a bell and a heavy ringing butt. |
| `use_super_fang` | `SEQ_SE_DP_W044` | Super Fang: a snap of teeth, a thud and a second clack. |
| `use_mirror_shot` | `SEQ_SE_DP_146` | Mirror Shot: four glassy pings, a rising whine and a bright flash. |
| `use_charge_beam` | `SEQ_SE_DP_206` | Charge Beam: a crackling charge and a long humming beam. |
| `use_giga_impact` | `SEQ_SE_DP_131` | Giga Impact: a heavy wind-up, a rushing charge and a great crash. |
| `use_octazooka` | `SEQ_SE_DP_W025B` | Octazooka: three wet spits and a splattering burst. |
| `use_wring_out` | `SEQ_SE_DP_021` | Wring Out: three creaking twists, each ending in a squeeze. |
| `use_dark_pulse` | `SEQ_SE_DP_W399` | Dark Pulse: a low dark hum, three rising rings and a deep boom. |
| `use_dragon_pulse` | `SEQ_SE_DP_W373` | Dragon Pulse: a rising roar with a run of bright notes and a ringing boom. |
| `use_dragon_rush` | `SEQ_SE_PL_WHIP_1` | Dragon Rush: a whip through the air, a growl and a heavy crash. |
| `use_hammer_arm` | `SEQ_SE_DP_W011` | Hammer Arm: a raised swing and a very heavy slam with rumble. |
| `use_hyper_beam` | `SEQ_SE_DP_W062D` | Hyper Beam: a rising charge and a huge roaring beam ending in a boom. |
| `use_natural_gift` | `SEQ_SE_PL_W363_1` | Natural Gift: four light rising notes, a bell and a soft burst. |
| `use_power_whip` | `SEQ_SE_DP_W104` | Power Whip: two cracking lashes and a thick leafy thud. |
| `use_brave_bird` | `SEQ_SE_DP_W360C` | Brave Bird: a rising rush of wind, a heavy crash and a ringing recoil. |
| `use_head_smash` | `SEQ_SE_DP_143` | Head Smash: a long wind-up, a stony smash and a rolling rumble. |
| `use_leaf_storm` | `SEQ_SE_DP_KAZE2` | Leaf Storm: a long whirl of rustling leaves and a last thud. |
| `use_attack_order` | `SEQ_SE_DP_W456` | Attack Order: a droning swarm and a flurry of stings. |
| `use_bug_buzz` | `SEQ_SE_DP_W405` | Bug Buzz: a loud two-tone insect buzz. |
| `use_cross_poison` | `SEQ_SE_DP_W015` | Cross Poison: two crossing slashes and a bubbling poison hiss. |
| `use_defend_order` | `SEQ_SE_DP_W456` | Defend Order: a droning swarm ending in a bright hardening ring. |
| `use_heal_order` | `SEQ_SE_DP_W456` | Heal Order: a droning swarm ending in a rising healing chime. |
| `use_iron_head` | `SEQ_SE_DP_W029` | Iron Head: a ring of steel, a heavy thud and a clang. |
| `use_megahorn` | `SEQ_SE_DP_W082` | Megahorn: a rushing wind-up, a stabbing boom and a ringing horn. |
| `use_power_gem` | `SEQ_SE_DP_W408` | Power Gem: a rising shimmer, a fan of gem notes and a glittering burst. |
| `use_earth_power` | `SEQ_SE_DP_W053` | Earth Power: three eruptions of earth, each with a rumble and a crack. |
| `use_rock_wrecker` | `SEQ_SE_DP_W082` | Rock Wrecker: a clatter of stones packing, a throw and a shattering smash. |
| `use_flare_blitz` | `SEQ_SE_DP_W052` | Flare Blitz: a roaring wind of flame, a crash and a burning boom. |
| `use_overheat` | `SEQ_SE_DP_W082` | Overheat: a trembling heat, a flash and a vast roaring blaze. |
| `use_solar_beam` | `SEQ_SE_DP_SHUSHU` | Solar Beam: a bright rising gather and a long humming beam. |
| `use_energy_ball` | `SEQ_SE_DP_211` | Energy Ball: a rising green swell, a throw and a bright burst. |
| `use_leaf_blade` | `SEQ_SE_DP_W015` | Leaf Blade: two swift blade swishes and a leafy thud. |
| `use_aura_sphere` | `SEQ_SE_DP_131` | Aura Sphere: a deep rising gather, a throw and a ringing boom. |
| `use_extreme_speed` | `SEQ_SE_DP_W013B` | Extreme Speed: a rush of speed and three quick blows. |
| `use_mirror_coat` | `SEQ_SE_DP_W115` | Mirror Coat: glassy bells and a returning flash. |
| `use_surf` | `SEQ_SE_DP_W057` | Surf: a great rolling surge of water and a crashing splash. |
| `use_heat_wave` | `SEQ_SE_DP_W257` | Heat Wave: a long hot roaring wind with embers crackling in it. |
| `use_acid` | `SEQ_SE_DP_W145C` | Acid: a wet fling, a splash and a fizzing hiss. |
| `use_constrict` | `SEQ_SE_DP_W020` | Constrict: five quick wraps and two tighter squeezes. |
| `use_razor_wind` | `SEQ_SE_DP_060` | Razor Wind: a rising whirl of wind and two keen cutting blasts. |
| `use_tailwind` | `SEQ_SE_DP_KAZE` | Tailwind: a long rushing wind with a rising chime. |
| `use_bullet_punch` | `SEQ_SE_DP_W025C` | Bullet Punch: two fast steel punches, each with a ring. |
| `use_dive` | `SEQ_SE_DP_W291` | Dive: a splash and gurgle, then a spout of water erupting. |
| `use_dynamic_punch` | `SEQ_SE_DP_W233B` | Dynamic Punch: a fiery wind-up and a huge booming punch with a rumble. |
| `use_guard_swap` | `SEQ_SE_DP_W179` | Guard Swap: six bell pings that change pitch halfway. |
| `use_gunk_shot` | `SEQ_SE_DP_290` | Gunk Shot: a thick wet heave and a rank, bubbling splatter. |
| `use_outrage` | `SEQ_SE_DP_W082` | Outrage: a furious growl with two heavy blows. |
| `use_perish_song` | `SEQ_SE_DP_W195` | Perish Song: a slow falling tune over a deep drone. |
| `use_power_swap` | `SEQ_SE_DP_W179` | Power Swap: six bell pings that change pitch halfway, lower and heavier. |
| `use_sheer_cold` | `SEQ_SE_DP_W258` | Sheer Cold: an icy rush, chimes of frost and a freezing crack. |
| `use_spore` | `SEQ_SE_DP_W077B` | Spore: a soft fizzing drift and a slow sleepy tune. |
| `use_acid_armor` | `SEQ_SE_DP_154` | Acid Armor: a wobbling melt and four wet plops. |
| `use_aromatherapy` | `SEQ_SE_DP_W080` | Aromatherapy: a soft breeze with a gentle run of healing notes. |
| `use_destiny_bond` | `SEQ_SE_DP_W060` | Destiny Bond: a low ghostly drone and two heavy tolling bells. |
| `use_gastro_acid` | `SEQ_SE_DP_W380` | Gastro Acid: a bubbling hiss and a gurgling sizzle. |
| `use_grudge` | `SEQ_SE_DP_W060` | Grudge: a low growling drone, a sinking tune and a dull crackle. |
| `use_mega_kick` | `SEQ_SE_DP_140` | Mega Kick: a swing and a huge booming kick. |
| `use_moonlight` | `SEQ_SE_DP_W236` | Moonlight: a soft rising hum and a slow twinkle of high notes. |
| `use_trick` | `SEQ_SE_DP_166` | Trick: a puff, a quick run of notes and four sparkling pings. |
| `use_clamp` | `SEQ_SE_DP_050` | Clamp: a shell's clack, a thud and a tighter second clack with bubbles. |
| `use_crabhammer` | `SEQ_SE_DP_W233B` | Crabhammer: a watery gather, a very heavy hammer blow and a crashing splash. |
| `use_double_kick` | `SEQ_SE_DP_030` | Double Kick: two swings, each ending in a thud. |
| `use_icicle_spear` | `SEQ_SE_DP_W196` | Icicle Spear: four icy whooshes and four ringing shatters. |
| `use_rapid_spin` | `SEQ_SE_DP_207` | Rapid Spin: a rising whirl and three rolling thuds. |
| `use_water_spout` | `SEQ_SE_DP_W291` | Water Spout: a rising rush of water, a spout and a heavy falling splash. |
| `use_belly_drum` | `SEQ_SE_DP_W187` | Belly Drum: six deep drum beats and a rising tone. |
| `use_covet` | `SEQ_SE_DP_W204` | Covet: a pleading run of soft notes, a pat and a twinkle. |
| `use_slack_off` | `SEQ_SE_DP_W281` | Slack Off: a long low yawn and a soft healing run of notes. |
| `use_superpower` | `SEQ_SE_DP_W025` | Superpower: a rising growl of power and a huge booming blow with a crackle. |
| `use_blaze_kick` | `SEQ_SE_DP_W172` | Blaze Kick: a roaring flame, a kick and a burning crackle. |
| `use_eruption` | `SEQ_SE_DP_W153` | Eruption: two deep rumbles and a run of six booming blasts. |
| `use_facade` | `SEQ_SE_DP_W207` | Facade: four pulsing tones that climb and a heavy blow. |
| `use_seed_bomb` | `SEQ_SE_DP_HURU2` | Seed Bomb: five quick pings thrown and three booming bursts. |
| `use_triple_kick` | `SEQ_SE_DP_W007` | Triple Kick: three quick kicks, each harder. |
| `use_mega_punch` | `SEQ_SE_DP_140` | Mega Punch: a swing and a huge booming punch with a bell. |
| `use_skull_bash` | `SEQ_SE_DP_W036` | Skull Bash: a tucking swish, a rushing charge and a ringing crack. |
| `use_glare` | `SEQ_SE_DP_W043D` | Glare: a low menacing drone with sour notes and a crackle. |
| `use_healing_wish` | `SEQ_SE_DP_W361` | Healing Wish: a long rising glow with a run of bright notes and a bell. |
| `use_lava_plume` | `SEQ_SE_DP_W436` | Lava Plume: a thick roaring surge of lava with bubbling and crackling. |
| `use_sky_uppercut` | `SEQ_SE_DP_W327` | Sky Uppercut: a rising rush, a booming blow and a second lighter one. |
| `use_cotton_spore` | `SEQ_SE_DP_W199` | Cotton Spore: a long soft rustle with a gentle drifting tune. |
| `use_crush_claw` | `SEQ_SE_DP_W013` | Crush Claw: two heavy slashes, each with a thud and a crack. |
| `use_fissure` | `SEQ_SE_DP_W090` | Fissure: a long grinding rumble, a crackle of rock and a deep thud. |
| `use_needle_arm` | `SEQ_SE_DP_W030` | Needle Arm: four quick prickling jabs and a last hard blow. |
| `use_spikes` | `SEQ_SE_DP_W026` | Spikes: five thrown pings, each landing as a bright iron ring. |
| `use_teeter_dance` | `SEQ_SE_DP_W298` | Teeter Dance: a wobbling tune that sways and a rising twinkle. |
| `use_guillotine` | `SEQ_SE_DP_W011` | Guillotine: a swing, a snip of steel and a ringing chop. |
| `use_present` | `SEQ_SE_DP_W039` | Present: a playful toss, a boom and a bright run of notes. |
| `use_heal_block` | `SEQ_SE_DP_W377` | Heal Block: a low dark drone, falling notes and a dull toll. |
| `use_psycho_shift` | `SEQ_SE_DP_W375` | Psycho Shift: a wavering rise, a spiral of notes and a falling tone with a bell. |
| `use_comet_punch` | `SEQ_SE_DP_030` | Comet Punch: four quick punches, each a thud, and a small bell. |
| `use_pay_day` | `SEQ_SE_DP_W013B` | Pay Day: a rush of ringing coins, a thud and a bright scatter of coins. |
| `use_vise_grip` | `SEQ_SE_DP_W011` | Vise Grip: a closing swish, a tight clamp and a straining creak. |
| `use_rolling_kick` | `SEQ_SE_DP_050` | Rolling Kick: a spinning rush and a heavy kick with a rumble. |
| `use_twineedle` | `SEQ_SE_DP_HURU2` | Twineedle: two thin whistling needles, each a prick, and a poisonous bubbling. |
| `use_sleep_powder` | `SEQ_SE_DP_W077B` | Sleep Powder: a long soft sifting hiss and a slow falling lullaby. |
| `use_sludge` | `SEQ_SE_DP_W145C` | Sludge: a wet fling, a heavy splat and a bubbling ooze. |
| `use_bone_club` | `SEQ_SE_DP_W155` | Bone Club: a swing and a hollow clonk of bone with a thud. |
| `use_spike_cannon` | `SEQ_SE_DP_W013` | Spike Cannon: a rattling volley of twelve spikes and a clatter of them landing. |
| `use_barrage` | `SEQ_SE_DP_W207` | Barrage: five lobbed pops, each ending in a thud and a puff. |
| `use_lovely_kiss` | `SEQ_SE_DP_W213` | Lovely Kiss: a run of sweet notes, a smack and a drowsy falling tune. |
| `use_sky_attack` | `SEQ_SE_DP_W360` | Sky Attack: a rising shriek of wind, a vast crash and a ringing flare. |
| `use_transform` | `SEQ_SE_PL_W100` | Transform: a crackling pixel whine that rises, breaks and falls again, with a bell. |
| `use_bonemerang` | `SEQ_SE_DP_HURU` | Bonemerang: a spinning whoosh out, a thud, a whoosh back and a second thud. |
| `use_sharpen` | `SEQ_SE_DP_W112` | Sharpen: a bright rising run of notes and a glassy ring. |
| `use_conversion` | `SEQ_SE_DP_W112` | Conversion: four climbing tones that shift and a bright closing run. |
| `use_struggle` | `SEQ_SE_DP_001` | Struggle: a flailing patter of weak thumps and a weak blow. |
| `use_sketch` | `SEQ_SE_DP_W092` | Sketch: a quick scratching of pencil lines and a small bell. |
| `use_spider_web` | `SEQ_SE_DP_W081B` | Spider Web: two sticky threads thrown and a trembling web. |
| `use_nightmare` | `SEQ_SE_DP_W171` | Nightmare: a low wavering drone and a creeping, falling tune. |
| `use_aeroblast` | `SEQ_SE_DP_KAZE` | Aeroblast: a roaring rush of wind, a vast boom and a rushing burst. |
| `use_powder_snow` | `SEQ_SE_DP_150` | Powder Snow: a long cold sifting hiss with a chime of frost. |
| `use_zap_cannon` | `SEQ_SE_DP_W086` | Zap Cannon: a whining charge, a thunderous boom and a long crackle. |
| `use_milk_drink` | `SEQ_SE_DP_W152` | Milk Drink: gurgling bubbles, a gulp and a soft healing chime. |
| `use_sacred_fire` | `SEQ_SE_DP_W221B` | Sacred Fire: a rising holy tune over a roaring flame and a rumbling blaze. |
| `use_morning_sun` | `SEQ_SE_DP_W234` | Morning Sun: a warm rising swell with bright bells and twinkles. |
| `use_flatter` | `SEQ_SE_DP_W227B` | Flatter: a coaxing run of notes with a shimmer and a wobbling falling tone. |
| `use_smelling_salts` | `SEQ_SE_DP_030C` | Smelling Salts: a sharp sniffing hiss, a crackling jolt and a bright jump. |
| `use_nature_power` | `SEQ_SE_DP_W053` | Nature Power: a rising rustle of nature, a throw and a booming burst. |
| `use_role_play` | `SEQ_SE_DP_W161` | Role Play: a gliding tone, a run of mimicking notes and a bell. |
| `use_magic_coat` | `SEQ_SE_DP_W112` | Magic Coat: a shimmering run of glassy notes and a bell. |
| `use_arm_thrust` | `SEQ_SE_DP_030` | Arm Thrust: five quick open-palm thuds and a last blow. |
| `use_tail_glow` | `SEQ_SE_DP_W234` | Tail Glow: a rising shining tone and a string of bright bells. |
| `use_luster_purge` | `SEQ_SE_DP_W076` | Luster Purge: a swelling shimmer, a bright flash and a glittering burst. |
| `use_mist_ball` | `SEQ_SE_DP_W081` | Mist Ball: a soft rolling mist, a throw and a misty burst with falling notes. |
| `use_feather_dance` | `SEQ_SE_DP_W080` | Feather Dance: a soft drifting hush with a gentle falling tune. |
| `use_ice_ball` | `SEQ_SE_DP_W196` | Ice Ball: an icy rolling clatter, a thud and a shattering chime. |
| `use_blast_burn` | `SEQ_SE_DP_W085` | Blast Burn: a roaring wind of flame, a vast boom and a long burning crackle. |
| `use_hydro_cannon` | `SEQ_SE_DP_W056` | Hydro Cannon: a rising roar of water, a cannon blast and a crashing splash. |
| `use_weather_ball` | `SEQ_SE_DP_W207` | Weather Ball: a wavering gather, a bright flash and a booming burst. |
| `use_fake_tears` | `SEQ_SE_DP_W039` | Fake Tears: a sobbing falling tune and a patter of drops. |
| `use_frenzy_plant` | `SEQ_SE_DP_W010` | Frenzy Plant: four creaking vines lashing, a very heavy blow and a crackle of leaves. |
| `use_bulk_up` | `SEQ_SE_DP_W207` | Bulk Up: three straining beats that climb and a firm closing run. |
| `use_poison_tail` | `SEQ_SE_DP_BRADE` | Poison Tail: a swishing tail, a thud and four bubbling drips. |
| `use_volt_tackle` | `SEQ_SE_DP_W120` | Volt Tackle: a crackling charge, a huge crash and a long sizzle of lightning. |
| `use_doom_desire` | `SEQ_SE_DP_W466` | Doom Desire: a deep golden hum, a tolling bell and a rising run of notes. |
| `use_psycho_boost` | `SEQ_SE_DP_W376` | Psycho Boost: a swelling charge with a run of notes and a huge falling wave. |
| `use_trump_card` | `SEQ_SE_DP_W376` | Trump Card: four flicked cards, each a swish and a tap, and a bell. |
| `use_power_trick` | `SEQ_SE_DP_CLIMAX09` | Power Trick: two rising swaps, each with a bell and a run of notes. |
| `use_me_first` | `SEQ_SE_DP_SHUSHU` | Me First: a falling pull of light, a quick run of notes and a thud. |
| `use_heart_swap` | `SEQ_SE_DP_W179` | Heart Swap: six bell pings, a gliding run of notes and a last twinkle. |
| `use_vacuum_wave` | `SEQ_SE_DP_W015` | Vacuum Wave: three whistling whirls and a punch of air. |
| `use_switcheroo` | `SEQ_SE_DP_166` | Switcheroo: a quick run of notes, a rustle of shuffling cups and five bright pings. |
| `use_draco_meteor` | `SEQ_SE_DP_201` | Draco Meteor: a deep rising roar and six meteors, each a rush and a boom. |
| `use_judgment` | `SEQ_SE_DP_CLIMAX03` | Judgment: a rising holy tone, a booming bell and a glittering fall of light. |
| `use_roar_of_time` | `SEQ_SE_DP_W459` | Roar of Time: a deep drone, a ticking of bells and a vast crash with a toll. |
| `use_spacial_rend` | `SEQ_SE_PL_W460B` | Spacial Rend: a rising whine, a tearing slash and a falling hum with a bell. |
| `use_lunar_dance` | `SEQ_SE_DP_W461` | Lunar Dance: a slow dancing tune over a soft rising hum and a twinkle. |
| `use_crush_grip` | `SEQ_SE_DP_W462` | Crush Grip: a swing, a vast crushing blow and a creaking squeeze. |
| `use_magma_storm` | `SEQ_SE_DP_W463` | Magma Storm: a long deep roaring storm of fire with crackling and bubbling lava. |
| `use_dark_void` | `SEQ_SE_DP_W464` | Dark Void: a deep sinking drone, a falling tune and a hollow toll. |
| `use_seed_flare` | `SEQ_SE_DP_W465` | Seed Flare: a bright rising gather, a glittering flash and a ringing burst. |
| `use_shadow_force` | `SEQ_SE_DP_W467` | Shadow Force: a fading shadowy whine, a dark slam and a growling crackle. |
| `use_hone_claws` | — | Hone Claws: three whetting scrapes, a rising hum and a glassy ring. |
| `use_wide_guard` | — | Wide Guard: five stony thuds in a row and a ringing, shimmering wall. |
| `use_guard_split` | — | Guard Split: two gliding tones that meet in a bell and part again with a falling run. |
| `use_power_split` | — | Power Split: two rising growls that meet in a thud and part with a bold run of notes. |
| `use_wonder_room` | — | Wonder Room: a slow wavering hum, a tumbling run of notes and a ringing bell. |
| `use_psyshock` | — | Psyshock: a rising whine, a quick run of notes, then a thud and a crackle of breaking crystal. |
| `use_venoshock` | — | Venoshock: a gurgling pour, bubbling, a heavy splat and a low sour drone. |
| `use_autotomize` | — | Autotomize: a crackle of peeling metal flakes, a hiss and a rising glassy ring. |
| `use_rage_powder` | — | Rage Powder: a hissing puff of dust over an angry growl and three sharp notes. |
| `use_telekinesis` | — | Telekinesis: a slow rising hum, lifting notes and a soft whoosh ending in a high bell. |
| `use_magic_room` | — | Magic Room: a wavering drone with six bright pings one after another and a last hiss. |
| `use_smack_down` | — | Smack Down: a falling rush, a vast thud, a crack of rock and a scatter of grit. |
| `use_storm_throw` | — | Storm Throw: a whirling rush, a heavy slam, a crack and three quick notes. |
| `use_flame_burst` | — | Flame Burst: a rising roar, a boom and a crackling scatter of flames. |
| `use_sludge_wave` | — | Sludge Wave: a long thick rolling rush, bubbling, a heavy splat and a growling drone. |
| `use_quiver_dance` | — | Quiver Dance: a dancing run of notes over a soft hum, a shimmer and a high bell. |
| `use_heavy_slam` | — | Heavy Slam: a falling growl, a vast crashing thud with a ringing steel bell and a rattle. |
| `use_synchronoise` | — | Synchronoise: three paired bell pings, a wavering hum, a thud and a ringing bell. |
| `use_electro_ball` | — | Electro Ball: a rising crackling whine, a thud and a burst of sparks with a bell. |
| `use_soak` | — | Soak: a long drenching rain, bubbles, a small splash and a falling tune. |
| `use_flame_charge` | — | Flame Charge: a rushing roar with crackling, a thud and a hiss of embers. |
| `use_coil` | — | Coil: a winding hiss, a rising growl, a thud and a small bell. |
| `use_low_sweep` | — | Low Sweep: a swish low to the ground and a sharp thud with a skid of grit. |
| `use_acid_spray` | — | Acid Spray: a spitting spray, bubbling, a splat and a long sizzle. |
| `use_foul_play` | — | Foul Play: a dark wavering growl, a rush, a heavy hit and a falling groan. |
| `use_simple_beam` | — | Simple Beam: a plain steady tone with a thin hiss and two flat bell pings. |
| `use_entrainment` | — | Entrainment: a run of notes in a tune, then three beats and a bell. |
| `use_after_you` | — | After You: a soft rustle, three polite notes and a bright bell. |
| `use_round` | — | Round: three rounds of a tune overlapping, each a little higher, over a bell. |
| `use_echoed_voice` | — | Echoed Voice: one clear note and its echo rolling after it, with a soft thud. |
| `use_chip_away` | — | Chip Away: three quick chisel taps with thuds and a patter of chips. |
| `use_clear_smog` | — | Clear Smog: a hissing puff, a thud and a bright sweeping clearing hiss with a rising run. |
| `use_stored_power` | — | Stored Power: a rising glow, a quick run of notes, a thud, a bell and a falling run. |
| `use_quick_guard` | — | Quick Guard: a snap, a thud with a bright bell and three glancing hisses. |
| `use_ally_switch` | — | Ally Switch: two gliding tones that cross over, a rustle and a bell. |
| `use_scald` | — | Scald: a spitting jet, a thud and a long hiss of steam with bubbles. |
| `use_shell_smash` | — | Shell Smash: a rising tone, a vast cracking burst and four bright notes. |
| `use_heal_pulse` | — | Heal Pulse: a bell, a run of soft notes and two more bells over a warm hum. |
| `use_hex` | — | Hex: a low wavering drone, falling spooky notes, a thud and a dark bell. |
| `use_sky_drop` | — | Sky Drop: a rising rush of wind, a snap of talons, a heavy thud and dust. |
| `use_shift_gear` | — | Shift Gear: six quick ratchet clicks over a rising whine and a bell. |
| `use_circle_throw` | — | Circle Throw: a whirling rush, a heavy slam, a crack and a skid. |
| `use_incinerate` | — | Incinerate: a rising flame roar, crackling, a thud and a sizzling hiss. |
| `use_quash` | — | Quash: a falling growl, a vast pressing thud and a low rumble. |
| `use_acrobatics` | — | Acrobatics: a rush, a quick tumbling run of notes, two quick cuts and a light thud. |
| `use_reflect_type` | — | Reflect Type: a glassy bell, a shimmering hiss, a falling tune and a high chime. |
| `use_retaliate` | — | Retaliate: a ghostly wavering hum, a rush, a heavy thud and a bell. |
| `use_final_gambit` | — | Final Gambit: a rising swell, a run of notes, a vast boom with a bright bell and a falling tone. |
| `use_bestow` | — | Bestow: a soft toss, a gentle run of notes, a tap and a bell. |
| `use_inferno` | — | Inferno: a long roaring blaze with a growl, crackling and a boom. |
| `use_water_pledge` | — | Water Pledge: a rising rush of water, a thud, a splash and bubbles. |
| `use_fire_pledge` | — | Fire Pledge: a rising roar, a thud and a long crackling blaze. |
| `use_grass_pledge` | — | Grass Pledge: a rustling burst of vines, a thud, a shower of leaves and a short tune. |
| `use_volt_switch` | — | Volt Switch: a crackling zap, a thud, then a falling zip back. |
| `use_struggle_bug` | — | Struggle Bug: a scuttling patter of tiny feet, a thud and a buzzing scatter. |
| `use_bulldoze` | — | Bulldoze: a long grinding rumble, a vast thud and a rattle of rocks. |
| `use_frost_breath` | — | Frost Breath: a cold hissing breath, a rising glassy tone, a thud and a high bell with a crackle of frost. |
| `use_dragon_tail` | — | Dragon Tail: a whooshing lash, a rising growl, a heavy thud and a falling groan. |
| `use_work_up` | — | Work Up: a rising hum, four quick notes, two heavy beats and a bright bell. |
| `use_electroweb` | — | Electroweb: a spreading crackle, a wavering whine, a thud and three falling pings. |
| `use_wild_charge` | — | Wild Charge: a rising crackling roar, a heavy thud with a burst of sparks and a falling zip. |
| `use_drill_run` | — | Drill Run: a rising whirring whine with grinding, a thud and a rattle of rock. |
| `use_dual_chop` | — | Dual Chop: two sharp cuts each with a thud and a ringing bell. |
| `use_heart_stamp` | — | Heart Stamp: a falling note, a heavy thud, a sweet bell and three rising notes. |
| `use_horn_leech` | — | Horn Leech: a sharp rush, a thud, a rising hum with a rustle and four warm notes. |
| `use_sacred_sword` | — | Sacred Sword: a holy rising tone, two bright cuts, a thud and a ringing bell. |
| `use_razor_shell` | — | Razor Shell: two sharp swishes, a thud, a wash of water with bubbles and a high bell. |
| `use_heat_crash` | — | Heat Crash: a falling roar, a vast crashing thud and a long crackling blaze. |
| `use_leaf_tornado` | — | Leaf Tornado: a whirling rush of leaves, a thud, a rustling shower and three falling notes. |
| `use_steamroller` | — | Steamroller: a rumbling roll with six beats, a vast thud and three falling notes. |
| `use_cotton_guard` | — | Cotton Guard: a soft billowing hush, a gentle rising tune and a small bell. |
| `use_night_daze` | — | Night Daze: a low falling drone, three deep beats, a dark swell and a falling tune. |
| `use_psystrike` | — | Psystrike: a rising whine, a thud, a crackle of breaking crystal and a ringing bell. |
| `use_tail_slap` | — | Tail Slap: four quick slaps each with a thud and three bright notes. |
| `use_hurricane` | — | Hurricane: a long howling gale with a rising tone, a thud, a rushing hiss and four notes. |
| `use_head_charge` | — | Head Charge: a rushing charge, a vast thud, a low ring and a rebounding thump. |
| `use_gear_grind` | — | Gear Grind: a whirring grind, a thud and shower of sparks, a second thud and a ringing bell. |
| `use_searing_shot` | — | Searing Shot: six quick whooshes of fire, a thud and a long crackling sizzle. |
| `use_techno_blast` | — | Techno Blast: a quick digital run of notes, a rising zip, a thud and a falling run. |
| `use_relic_song` | — | Relic Song: a slow old tune over a low drone, a bell and three falling notes. |
| `use_secret_sword` | — | Secret Sword: a rising tone, a bright long cut, a thud, a ringing bell and three notes. |
| `use_glaciate` | — | Glaciate: a long icy wind, a thud, a crackle of frost and a run of high chimes. |
| `use_bolt_strike` | — | Bolt Strike: a rising zap, a vast thunderous thud, a long crackle and a low rolling rumble. |
| `use_blue_flare` | — | Blue Flare: a rising roar of flame, a vast boom, a rushing blaze and a ringing bell. |
| `use_fiery_dance` | — | Fiery Dance: a dancing run of notes with a roar of flame, a thud, crackling and three high notes. |
| `use_freeze_shock` | — | Freeze Shock: a falling glassy tone, a vast crack of ice with a storm of sparks and a bell. |
| `use_ice_burn` | — | Ice Burn: a rising cold roar of flame, a thud, crackling and a glassy ringing bell. |
| `use_snarl` | — | Snarl: a growling snarl, a thud and three falling notes. |
| `use_icicle_crash` | — | Icicle Crash: three falling shatters each with a thud and a bell, then a last crash. |
| `use_v_create` | — | V-create: a rising blazing roar, a vast boom, a crackling blaze and a long falling tone. |
| `use_fusion_flare` | — | Fusion Flare: a falling roar of flame, a vast boom, a long blaze and a low bell. |
| `use_fusion_bolt` | — | Fusion Bolt: a rising electric whine, a vast thud with a burst of sparks and a ringing bell. |
| `use_flying_press` | — | Flying Press: a rushing fall, a vast thud, a rush of air and three bright notes. |
| `use_mat_block` | — | Mat Block: a slap of a mat, two thuds, a low bell and three soft notes. |
| `use_belch` | — | Belch: a deep rumbling burp, gurgling bubbles, a thud and a sour hiss. |
| `use_rototiller` | — | Rototiller: a chugging engine with six digging beats and a bright rising tune. |
| `use_sticky_web` | — | Sticky Web: a stringy hiss, a thin wavering tone, a soft thud and four tiny chimes. |
| `use_fell_stinger` | — | Fell Stinger: two thin stabs with thuds, a heavy thud and three sharp notes. |
| `use_phantom_force` | — | Phantom Force: a hollow falling hum, a dark thud, a hiss and a groaning tail. |
| `use_trick_or_treat` | — | Trick-or-Treat: a spooky little tune, a wobbling hum, a bell and three sharp notes. |
| `use_noble_roar` | — | Noble Roar: a great regal roar, a thud and four rising notes. |
| `use_ion_deluge` | — | Ion Deluge: a long crackling hum under a charged hiss and four falling pings. |
| `use_parabolic_charge` | — | Parabolic Charge: a crackling zap, a thud, a rising swell and three bright notes. |
| `use_forest_s_curse` | — | Forest's Curse: a rustle, a low wavering drone, four falling notes and a dark bell. |
| `use_petal_blizzard` | — | Petal Blizzard: a long rushing gale, a rising tune, a soft thud and a patter of petals. |
| `use_freeze_dry` | — | Freeze-Dry: a thin icy tone, a hiss, a thud and a crackle of fine frost. |
| `use_disarming_voice` | — | Disarming Voice: a sweet sung tune in two voices over a bell and three high chimes. |
| `use_parting_shot` | — | Parting Shot: a growl, a thud, three falling notes and a rushing zip away. |
| `use_topsy_turvy` | — | Topsy-Turvy: a falling tone crossing a rising one, a run of notes and a bell. |
| `use_draining_kiss` | — | Draining Kiss: three sweet notes, a bell, a small smack and a rising tone with chimes. |
| `use_crafty_shield` | — | Crafty Shield: a quick run of six glassy notes, a rising hum and a bell. |
| `use_flower_shield` | — | Flower Shield: a rising run of five soft notes, a warm hum and a bell. |
| `use_grassy_terrain` | — | Grassy Terrain: a long rustle, a warm hum, four rising notes and a bell. |
| `use_misty_terrain` | — | Misty Terrain: a soft breath of mist, a wavering hum and five shimmering notes. |
| `use_electrify` | — | Electrify: a rising crackling zap, a thud, a long crackle and a buzz. |
| `use_play_rough` | — | Play Rough: a scuffling rush, five quick thumps with a tune, a last thud and a bell. |
| `use_fairy_wind` | — | Fairy Wind: a light breeze, a run of six sparkling notes, a soft thud and a chime. |
| `use_moonblast` | — | Moonblast: a rising glow, four bright notes, a vast thud with a bell and a falling tone. |
| `use_boomburst` | — | Boomburst: a rising growl, a colossal boom, four rolling thuds and a crackle. |
| `use_fairy_lock` | — | Fairy Lock: three bright notes, a clank, a ringing bell and five links of chain. |
| `use_king_s_shield` | — | King's Shield: a rush, a ringing clang of steel, a long bell and three firm notes. |
| `use_play_nice` | — | Play Nice: a friendly three-note tune, two soft claps, a bell and three notes. |
| `use_confide` | — | Confide: a whispering hiss, three soft notes, a bell and three falling notes. |
| `use_diamond_storm` | — | Diamond Storm: a rush of glittering grit, a thud, a shower of high chimes and a bell. |
| `use_steam_eruption` | — | Steam Eruption: a rising rumble, a vast thud, a long jet of steam and bubbling. |
| `use_hyperspace_hole` | — | Hyperspace Hole: a rising hum, four stepping notes, a thud, a crackle, a bell and a falling tone. |
| `use_water_shuriken` | — | Water Shuriken: three quick throws each with a thud and a splash, bubbles and a bell. |
| `use_mystical_fire` | — | Mystical Fire: a rising flicker of flame, a thud, crackling and four falling notes. |
| `use_spiky_shield` | — | Spiky Shield: six quick spikes, a rising tone, a thud, a ringing bell and three notes. |
| `use_aromatic_mist` | — | Aromatic Mist: a soft breath of mist, five gentle rising notes, a bell and a warm tone. |
| `use_eerie_impulse` | — | Eerie Impulse: a wavering hum, four pulsing notes, a crackle and a falling groan. |
| `use_venom_drench` | — | Venom Drench: a hiss, the smash of a flask, a thick splash, bubbling and a sour drone. |
| `use_powder` | — | Powder: a hissing puff of dust, four soft notes, a faint crackle and a tone. |
| `use_geomancy` | — | Geomancy: a deep rising hum, six climbing notes, a shimmering hiss and a bell. |
| `use_magnetic_flux` | — | Magnetic Flux: two humming tones drawing together, a crackle, four notes and a bell. |
| `use_happy_hour` | — | Happy Hour: a shower of nine coin-bells over a jaunty run of notes. |
| `use_electric_terrain` | — | Electric Terrain: a long buzzing hum, a spreading crackle and four high pings. |
| `use_dazzling_gleam` | — | Dazzling Gleam: a rising glittering tone, a thud, a great bell and four high chimes. |
| `use_celebrate` | — | Celebrate: a pop, a burst of streamers, a bouncing tune and two bells. |
| `use_hold_hands` | — | Hold Hands: a warm four-note tune over a soft hum, a bell and three notes. |
| `use_baby_doll_eyes` | — | Baby-Doll Eyes: a quick run of four twinkling notes, two bells and three falling notes. |
| `use_nuzzle` | — | Nuzzle: a small crackle, a soft thud, a longer crackle and three tingling notes. |
| `use_hold_back` | — | Hold Back: a soft rustle, a gentle tap, two notes and a small bell. |
| `use_infestation` | — | Infestation: a skittering scatter of tiny feet, a thud, a buzzing drone and three notes. |
| `use_power_up_punch` | — | Power-Up Punch: a rising growl, a whoosh, a heavy thud and four rising notes with a bell. |
| `use_oblivion_wing` | — | Oblivion Wing: a flapping rush, a growl, a thud and a long rising draining tone. |
| `use_thousand_arrows` | — | Thousand Arrows: nine quick arrows each with a thud, then one great thud and a crackle. |
| `use_thousand_waves` | — | Thousand Waves: five rolling waves of earth, a deep thud and a rattle. |
| `use_land_s_wrath` | — | Land's Wrath: a deep grinding rumble, a vast thud, a long crumbling crackle and a low bell. |
| `use_light_of_ruin` | — | Light of Ruin: five rising bright notes, a vast thud, a great bell and a falling tone. |
| `use_origin_pulse` | — | Origin Pulse: a rising hum, three beams hissing in, a vast thud, bubbling and a bell. |
| `use_precipice_blades` | — | Precipice Blades: a rising growl, four heavy thrusts, a vast thud and a crackling fall of rock. |
| `use_dragon_ascent` | — | Dragon Ascent: a climbing rush, a vast thud, a falling tone and a ringing bell. |
| `use_hyperspace_fury` | — | Hyperspace Fury: a rising hum, four quick pounding blows, a vast thud, a crackle and a bell. |
| `use_shore_up` | — | Shore Up: a soft rush of sand, a small thud, a rising tune and a bell. |
| `use_first_impression` | — | First Impression: a bright quick hiss, a sharp thud, a second hiss and two high notes. |
| `use_baneful_bunker` | — | Baneful Bunker: a dull thud, a wavering hum, gurgling bubbles and a low bell. |
| `use_spirit_shackle` | — | Spirit Shackle: a falling hiss, a dark thud, three hollow bells and a groaning tail. |
| `use_darkest_lariat` | — | Darkest Lariat: a whirling rush with a growl, a vast thud and a dark falling hiss. |
| `use_sparkling_aria` | — | Sparkling Aria: a sung run of notes over bubbles, a thud and a high bell. |
| `use_ice_hammer` | — | Ice Hammer: a falling tone, a vast thud, a crackle of frost, a bell and three falling notes. |
| `use_floral_healing` | — | Floral Healing: five rising soft notes, a bell and a gentle shimmer. |
| `use_high_horsepower` | — | High Horsepower: a first stamp, a vast second thud, a crumbling crackle and two low notes. |
| `use_strength_sap` | — | Strength Sap: a falling draining tone, bubbles, then a rising tone and two notes. |
| `use_solar_blade` | — | Solar Blade: a rising tone with bright notes, a long bright cut, a thud and a bell. |
| `use_leafage` | — | Leafage: four quick leafy flicks, a small thud and two notes. |
| `use_spotlight` | — | Spotlight: a click, a steady hum, four bright notes and a high bell. |
| `use_toxic_thread` | — | Toxic Thread: a thin stringy hiss, a wavering buzz, bubbles and three falling notes. |
| `use_laser_focus` | — | Laser Focus: a rising whine, a thin hiss and a sharp locking tick. |
| `use_gear_up` | — | Gear Up: six clicking gears, a whirring hum, a bell and three rising notes. |
| `use_throat_chop` | — | Throat Chop: a sharp hiss, a heavy thud with a choked-off tone and two low notes. |
| `use_pollen_puff` | — | Pollen Puff: a soft rising hum, a puff, a small thud and a shimmer of three notes. |
| `use_anchor_shot` | — | Anchor Shot: a rushing swish, a vast thud with a low bell and four rattling chain links. |
| `use_psychic_terrain` | — | Psychic Terrain: two slow wavering drones, four soft notes and a high bell. |
| `use_lunge` | — | Lunge: a quick rush, a thud, two snapping notes and a falling tone. |
| `use_fire_lash` | — | Fire Lash: a cracking whip, a roar of flame, a thud and a crackle. |
| `use_power_trip` | — | Power Trip: a growing growl, four climbing beats, a vast thud and a deep falling tone. |
| `use_burn_up` | — | Burn Up: a long roaring flame, a vast boom, a crackling blaze and a dying hiss. |
| `use_speed_swap` | — | Speed Swap: a rising tone crossing a falling one, a fast hiss, a bell and three notes. |
| `use_smart_strike` | — | Smart Strike: two ticks of a sight locking, a sharp hiss, a thud and a bright bell. |
| `use_purify` | — | Purify: a clear bell, four rising notes, a shimmer and a gentle glow of a tone. |
| `use_revelation_dance` | — | Revelation Dance: a lilting dance of seven notes with soft beats, a thud and a bell. |
| `use_core_enforcer` | — | Core Enforcer: a rising growl, a rush, a vast thud, a sharp tone and a deep bell. |
| `use_trop_kick` | — | Trop Kick: a swinging rush, a thud and three bright notes. |
| `use_instruct` | — | Instruct: two sharp ticks, a wavering tone, a tune of five notes and a bell. |
| `use_beak_blast` | — | Beak Blast: a heating hum, a vast thud, a sizzling crackle and two sharp notes. |
| `use_clanging_scales` | — | Clanging Scales: six ringing scales, a vast thud, a great bell and a rush of hiss. |
| `use_dragon_hammer` | — | Dragon Hammer: a falling tone, a vast thud, a crackling split and a low bell. |
| `use_brutal_swing` | — | Brutal Swing: a heavy sweeping rush, a vast thud, a dark hiss and three low notes. |
| `use_aurora_veil` | — | Aurora Veil: two slow shimmering drones, four high notes, a fine hiss and a bell. |
| `use_shell_trap` | — | Shell Trap: a click of a trap, a vast boom, a rushing blaze and a crackle. |
| `use_fleur_cannon` | — | Fleur Cannon: four soft notes, a rising roar, a vast thud, a bell and a falling tone. |
| `use_psychic_fangs` | — | Psychic Fangs: a wavering tone, a thud, a double snap, a crackle of glass and a bell. |
| `use_stomping_tantrum` | — | Stomping Tantrum: an angry buzz, a first stamp, a vast second stamp and a crumbling crackle. |
| `use_shadow_bone` | — | Shadow Bone: a spinning whirr, a clack, a thud and a hollow falling tone. |
| `use_accelerock` | — | Accelerock: a fast rising rush, a thud and a rocky crackle. |
| `use_liquidation` | — | Liquidation: a keen rush of water, a thud, a splash, bubbling and three notes. |
| `use_prismatic_laser` | — | Prismatic Laser: a rising whine, five bright notes, a vast thud, a crackle and a ringing bell. |
| `use_spectral_thief` | — | Spectral Thief: a hollow falling hum, a dark thud, four creeping notes and a bell. |
| `use_sunsteel_strike` | — | Sunsteel Strike: a blazing falling roar, a vast boom, a ringing bell and a crackle. |
| `use_moongeist_beam` | — | Moongeist Beam: a slow dark hum, four soft notes, a deep thud, a bell and a falling tone. |
| `use_tearful_look` | — | Tearful Look: a trembling tone, four falling notes, a few bubbles and a small bell. |
| `use_zing_zap` | — | Zing Zap: a hard crackling hiss, a thud and three sharp notes. |
| `use_nature_s_madness` | — | Nature's Madness: a rustling wild wash, a wobbling tone, five notes, a thud and a bell. |
| `use_multi_attack` | — | Multi-Attack: a rising run of six notes, a vast thud and four clashing bells. |
| `use_mind_blown` | — | Mind Blown: a swelling growl, a vast boom, a crackle, and a second boom. |
| `use_plasma_fists` | — | Plasma Fists: two thuds each with a crackle of plasma and a rising buzz. |
| `use_photon_geyser` | — | Photon Geyser: a rising hum, a vast thud, a soaring tone, four bright notes and a bell. |
| `use_zippy_zap` | — | Zippy Zap: a quick crackling zip, a thud and three sharp notes. |
| `use_splishy_splash` | — | Splishy Splash: a rushing wash with bubbles, a thud, a splash, a few sparks and two notes. |
| `use_floaty_fall` | — | Floaty Fall: a floating tone, a hush of air, a drop, a soft thud and three falling notes. |
| `use_pika_papow` | — | Pika Papow: four bright notes, a quavering tone, a thud, a crackle and four high notes. |
| `use_bouncy_bubble` | — | Bouncy Bubble: three bouncing bubbles, a thud, a burst of bubbles and three notes. |
| `use_buzzy_buzz` | — | Buzzy Buzz: a long buzzing hum with a crackle, a small thud and two sharp notes. |
| `use_sizzly_slide` | — | Sizzly Slide: a long sizzling slide, a thud, a crackle and two notes. |
| `use_glitzy_glow` | — | Glitzy Glow: six glittering high notes, a fine hiss, a thud and a bell. |
| `use_baddy_bad` | — | Baddy Bad: a low growl, four sneering notes, a dark swell, a thud and a bell. |
| `use_sappy_seed` | — | Sappy Seed: a flick, a small thud, a growing tone, a rustle, bubbling and three notes. |
| `use_freezy_frost` | — | Freezy Frost: a long icy hiss, a glassy tone, a thud, a crackle of frost and four chimes. |
| `use_sparkly_swirl` | — | Sparkly Swirl: a rising swirl of notes, a thud, a bell, a glitter and three falling notes. |
| `use_veevee_volley` | — | Veevee Volley: three quick light pats each with a note, a thud and a bright run. |
| `use_double_iron_bash` | — | Double Iron Bash: two iron blows each with a clang, a crackle and a ring. |
| `use_dynamax_cannon` | — | Dynamax Cannon: three ringing muzzle bells, a rising roar, a colossal thud, a long crackle and a deep falling tone. |
| `use_snipe_shot` | — | Snipe Shot: three ticks of a sight, a razor hiss, a thud, bubbles and two notes. |
| `use_jaw_lock` | — | Jaw Lock: a growl, a heavy thud with a snap, then five rattling chain links and a bell. |
| `use_stuff_cheeks` | — | Stuff Cheeks: four soft pops with a rustle, a swelling tone and three happy notes. |
| `use_no_retreat` | — | No Retreat: four heavy stakes driven in, a rising growl, five climbing notes and a bell. |
| `use_tar_shot` | — | Tar Shot: a sticky rush, a wet thud, thick bubbling and a low falling tone. |
| `use_magic_powder` | — | Magic Powder: a fine glittering hiss, five high notes over a wavering tone and a bell. |
| `use_dragon_darts` | — | Dragon Darts: two quick hisses each with a thud, a deep bell and two falling notes. |
| `use_teatime` | — | Teatime: a china chime, a gentle five-note tune over a soft steam hiss and a low glow. |
| `use_octolock` | — | Octolock: a writhing growl, a grinding rush, three squeezing thuds and three falling notes. |
| `use_bolt_beak` | — | Bolt Beak: a crackling charge, a thud with a burst of sparks and three high notes. |
| `use_fishious_rend` | — | Fishious Rend: a rushing hiss, a thud with a double snap, bubbling and three notes. |
| `use_court_change` | — | Court Change: a rising tone crossing a falling one, two thuds, a bell and three notes. |
| `use_clangorous_soul` | — | Clangorous Soul: five rising notes, a roaring rise, a soft thud, a bell. |
| `use_body_press` | — | Body Press: a first knock, a heavy thud, two high notes. |
| `use_decorate` | — | Decorate: five rising notes, four sparkling notes, a soft thud, a bell. |
| `use_drum_beating` | — | Drum Beating: a leafy rustle, 4 quick blows, a bell. |
| `use_snap_trap` | — | Snap Trap: a leafy rustle, a heavy thud, two high notes. |
| `use_pyro_ball` | — | Pyro Ball: a roaring rush, a heavy thud, two high notes. |
| `use_behemoth_blade` | — | Behemoth Blade: a ringing of metal, a heavy thud, a bell. |
| `use_behemoth_bash` | — | Behemoth Bash: a ringing of metal, a heavy thud, a bell. |
| `use_aura_wheel` | — | Aura Wheel: a crackle of sparks, a heavy thud, a bell. |
| `use_breaking_swipe` | — | Breaking Swipe: a roaring rise, a heavy thud, two high notes. |
| `use_branch_poke` | — | Branch Poke: a leafy rustle, a heavy thud, two high notes. |
| `use_overdrive` | — | Overdrive: a rising hum, a hissing beam, a crackle of sparks, a thud, a bell. |
| `use_apple_acid` | — | Apple Acid: a rising hum, a hissing beam, a leafy rustle, a thud, a bell. |
| `use_grav_apple` | — | Grav Apple: a leafy rustle, a heavy thud, a bell. |
| `use_spirit_break` | — | Spirit Break: four sparkling notes, a heavy thud, a bell. |
| `use_strange_steam` | — | Strange Steam: a rising hum, a hissing beam, four sparkling notes, a thud, a bell. |
| `use_life_dew` | — | Life Dew: five rising notes, bubbling, a soft thud, a bell. |
| `use_obstruct` | — | Obstruct: five rising notes, a dark growl, a soft thud, a bell. |
| `use_false_surrender` | — | False Surrender: a dark growl, a heavy thud, two high notes. |
| `use_meteor_assault` | — | Meteor Assault: a first knock, a heavy thud, two high notes. |
| `use_eternabeam` | — | Eternabeam: a rising hum, a hissing beam, a roaring rise, a thud, a bell. |
| `use_steel_beam` | — | Steel Beam: a rising hum, a hissing beam, a ringing of metal, a thud, a bell. |
| `use_expanding_force` | — | Expanding Force: a rising hum, a heavy thud, two high notes. |
| `use_steel_roller` | — | Steel Roller: a ringing of metal, a heavy thud, two high notes. |
| `use_scale_shot` | — | Scale Shot: a roaring rise, 4 quick blows, a bell. |
| `use_meteor_beam` | — | Meteor Beam: a rising hum, a hissing beam, a clatter of stones, a thud, a bell. |
| `use_shell_side_arm` | — | Shell Side Arm: a rising hum, a hissing beam, sour gurgling, a thud, a bell. |
| `use_misty_explosion` | — | Misty Explosion: four sparkling notes, a heavy thud, two high notes. |
| `use_grassy_glide` | — | Grassy Glide: a leafy rustle, a heavy thud, a bell. |
| `use_rising_voltage` | — | Rising Voltage: a crackle of sparks, a heavy thud, a bell. |
| `use_terrain_pulse` | — | Terrain Pulse: five rising notes, a plain hiss, a soft thud, a bell. |
| `use_skitter_smack` | — | Skitter Smack: skittering clicks, a heavy thud, two high notes. |
| `use_burning_jealousy` | — | Burning Jealousy: a roaring rush, a heavy thud, two high notes. |
| `use_lash_out` | — | Lash Out: a dark growl, 4 quick blows, a bell. |
| `use_poltergeist` | — | Poltergeist: a hollow falling moan, a heavy thud, a bell. |
| `use_corrosive_gas` | — | Corrosive Gas: five rising notes, sour gurgling, a soft thud, a bell. |
| `use_coaching` | — | Coaching: five rising notes, a first knock, a soft thud, a bell. |
| `use_flip_turn` | — | Flip Turn: bubbling, a heavy thud, a bell. |
| `use_triple_axel` | — | Triple Axel: an icy ringing, 4 quick blows, a bell. |
| `use_dual_wingbeat` | — | Dual Wingbeat: a rush of wind, 3 quick blows, a bell. |
| `use_scorching_sands` | — | Scorching Sands: a crumbling crackle, a heavy thud, two high notes. |
| `use_jungle_healing` | — | Jungle Healing: five rising notes, a leafy rustle, a soft thud, a bell. |
| `use_wicked_blow` | — | Wicked Blow: a dark growl, a heavy thud, two high notes. |
| `use_surging_strikes` | — | Surging Strikes: bubbling, 4 quick blows, a bell. |
| `use_thunder_cage` | — | Thunder Cage: a crackle of sparks, a heavy thud, a bell. |
| `use_dragon_energy` | — | Dragon Energy: a rising hum, a hissing beam, a roaring rise, a thud, a bell. |
| `use_freezing_glare` | — | Freezing Glare: a rising hum, a hissing beam, a rising hum, a thud, a bell. |
| `use_fiery_wrath` | — | Fiery Wrath: a dark growl, a heavy thud, two high notes. |
| `use_thunderous_kick` | — | Thunderous Kick: a first knock, a heavy thud, two high notes. |
| `use_glacial_lance` | — | Glacial Lance: an icy ringing, a heavy thud, a bell. |
| `use_astral_barrage` | — | Astral Barrage: a rising hum, a hissing beam, a hollow falling moan, a thud, a bell. |
| `use_eerie_spell` | — | Eerie Spell: a rising hum, a heavy thud, a bell. |
| `use_dire_claw` | — | Dire Claw: sour gurgling, a heavy thud, two high notes. |
| `use_psyshield_bash` | — | Psyshield Bash: a rising hum, a heavy thud, two high notes. |
| `use_power_shift` | — | Power Shift: five rising notes, a plain hiss, a soft thud, a bell. |
| `use_stone_axe` | — | Stone Axe: a clatter of stones, a heavy thud, two high notes. |
| `use_springtide_storm` | — | Springtide Storm: a rising gale, four sparkling notes, a deep thud, a crackle. |
| `use_mystical_power` | — | Mystical Power: a rising hum, a heavy thud, two high notes. |
| `use_raging_fury` | — | Raging Fury: a roaring rush, a heavy thud, a bell. |
| `use_wave_crash` | — | Wave Crash: bubbling, a heavy thud, a bell. |
| `use_chloroblast` | — | Chloroblast: a rising hum, a hissing beam, a leafy rustle, a thud, a bell. |
| `use_mountain_gale` | — | Mountain Gale: an icy ringing, a heavy thud, a bell. |
| `use_victory_dance` | — | Victory Dance: five rising notes, a first knock, a soft thud, a bell. |
| `use_headlong_rush` | — | Headlong Rush: a crumbling crackle, a heavy thud, a bell. |
| `use_barb_barrage` | — | Barb Barrage: sour gurgling, 3 quick blows, a bell. |
| `use_esper_wing` | — | Esper Wing: a rising hum, a heavy thud, a bell. |
| `use_bitter_malice` | — | Bitter Malice: a hollow falling moan, a heavy thud, two high notes. |
| `use_shelter` | — | Shelter: five rising notes, a ringing of metal, a soft thud, a bell. |
| `use_triple_arrows` | — | Triple Arrows: a first knock, 4 quick blows, a bell. |
| `use_infernal_parade` | — | Infernal Parade: a hollow falling moan, a heavy thud, a bell. |
| `use_ceaseless_edge` | — | Ceaseless Edge: a dark growl, a heavy thud, two high notes. |
| `use_bleakwind_storm` | — | Bleakwind Storm: a rising gale, a rush of wind, a deep thud, a crackle. |
| `use_wildbolt_storm` | — | Wildbolt Storm: a rising gale, a crackle of sparks, a deep thud, a crackle. |
| `use_sandsear_storm` | — | Sandsear Storm: a rising gale, a crumbling crackle, a deep thud, a crackle. |
| `use_lunar_blessing` | — | Lunar Blessing: five rising notes, a rising hum, a soft thud, a bell. |
| `use_take_heart` | — | Take Heart: five rising notes, a rising hum, a soft thud, a bell. |
| `use_tera_blast` | — | Tera Blast: a rising hum, a hissing beam, a plain hiss, a thud, a bell. |
| `use_silk_trap` | — | Silk Trap: five rising notes, skittering clicks, a soft thud, a bell. |
| `use_axe_kick` | — | Axe Kick: a first knock, a heavy thud, a bell. |
| `use_last_respects` | — | Last Respects: a hollow falling moan, a heavy thud, two high notes. |
| `use_lumina_crash` | — | Lumina Crash: a rising hum, a heavy thud, two high notes. |
| `use_order_up` | — | Order Up: a roaring rise, a heavy thud, two high notes. |
| `use_jet_punch` | — | Jet Punch: bubbling, a heavy thud, two high notes. |
| `use_spicy_extract` | — | Spicy Extract: five rising notes, a leafy rustle, a soft thud, a bell. |
| `use_spin_out` | — | Spin Out: a ringing of metal, a heavy thud, a bell. |
| `use_population_bomb` | — | Population Bomb: a plain hiss, 3 quick blows, a bell. |
| `use_ice_spinner` | — | Ice Spinner: an icy ringing, a heavy thud, a bell. |
| `use_glaive_rush` | — | Glaive Rush: a roaring rise, a heavy thud, a bell. |
| `use_revival_blessing` | — | Revival Blessing: five rising notes, a plain hiss, a soft thud, a bell. |
| `use_salt_cure` | — | Salt Cure: a clatter of stones, a heavy thud, two high notes. |
| `use_triple_dive` | — | Triple Dive: bubbling, 4 quick blows, a bell. |
| `use_mortal_spin` | — | Mortal Spin: sour gurgling, a heavy thud, two high notes. |
| `use_doodle` | — | Doodle: five rising notes, a plain hiss, a soft thud, a bell. |
| `use_fillet_away` | — | Fillet Away: five rising notes, a plain hiss, a soft thud, a bell. |
| `use_kowtow_cleave` | — | Kowtow Cleave: a dark growl, a heavy thud, two high notes. |
| `use_flower_trick` | — | Flower Trick: a leafy rustle, a heavy thud, a bell. |
| `use_torch_song` | — | Torch Song: a rising hum, a hissing beam, a roaring rush, a thud, a bell. |
| `use_aqua_step` | — | Aqua Step: bubbling, a heavy thud, a bell. |
| `use_raging_bull` | — | Raging Bull: a plain hiss, a heavy thud, two high notes. |
| `use_make_it_rain` | — | Make It Rain: a ringing of metal, a heavy thud, two high notes. |
| `use_psyblade` | — | Psyblade: a rising hum, a heavy thud, a bell. |
| `use_hydro_steam` | — | Hydro Steam: a rising hum, a hissing beam, bubbling, a thud, a bell. |
| `use_ruination` | — | Ruination: a dark growl, a heavy thud, two high notes. |
| `use_collision_course` | — | Collision Course: a first knock, a heavy thud, two high notes. |
| `use_electro_drift` | — | Electro Drift: a crackle of sparks, a heavy thud, two high notes. |
| `use_shed_tail` | — | Shed Tail: five rising notes, a plain hiss, a soft thud, a bell. |
| `use_chilly_reception` | — | Chilly Reception: five rising notes, an icy ringing, a soft thud, a bell. |
| `use_tidy_up` | — | Tidy Up: five rising notes, a plain hiss, a soft thud, a bell. |
| `use_snowscape` | — | Snowscape: five rising notes, an icy ringing, a soft thud, a bell. |
| `use_pounce` | — | Pounce: skittering clicks, a heavy thud, a bell. |
| `use_trailblaze` | — | Trailblaze: a leafy rustle, a heavy thud, a bell. |
| `use_chilling_water` | — | Chilling Water: a rising hum, a hissing beam, bubbling, a thud, a bell. |
| `use_hyper_drill` | — | Hyper Drill: a plain hiss, a heavy thud, two high notes. |
| `use_twin_beam` | — | Twin Beam: a rising hum, a hissing beam, a rising hum, a thud, a bell. |
| `use_rage_fist` | — | Rage Fist: a hollow falling moan, a heavy thud, two high notes. |
| `use_armor_cannon` | — | Armor Cannon: a rising hum, a hissing beam, a roaring rush, a thud, a bell. |
| `use_bitter_blade` | — | Bitter Blade: a roaring rush, a heavy thud, a bell. |
| `use_double_shock` | — | Double Shock: a crackle of sparks, 3 quick blows, a bell. |
| `use_gigaton_hammer` | — | Gigaton Hammer: a ringing of metal, a heavy thud, two high notes. |
| `use_comeuppance` | — | Comeuppance: a dark growl, a heavy thud, a bell. |
| `use_aqua_cutter` | — | Aqua Cutter: bubbling, a heavy thud, two high notes. |
| `use_blazing_torque` | — | Blazing Torque: a roaring rush, a heavy thud, two high notes. |
| `use_wicked_torque` | — | Wicked Torque: a dark growl, a heavy thud, two high notes. |
| `use_noxious_torque` | — | Noxious Torque: sour gurgling, a heavy thud, two high notes. |
| `use_combat_torque` | — | Combat Torque: a first knock, a heavy thud, a bell. |
| `use_magical_torque` | — | Magical Torque: four sparkling notes, a heavy thud, a bell. |
| `use_blood_moon` | — | Blood Moon: a plain hiss, a heavy thud, two high notes. |
| `use_matcha_gotcha` | — | Matcha Gotcha: a rising hum, a hissing beam, a leafy rustle, a thud, a bell. |
| `use_syrup_bomb` | — | Syrup Bomb: a leafy rustle, a heavy thud, two high notes. |
| `use_ivy_cudgel` | — | Ivy Cudgel: a leafy rustle, a heavy thud, a bell. |
| `use_electro_shot` | — | Electro Shot: a rising hum, a hissing beam, a crackle of sparks, a thud, a bell. |
| `use_tera_starstorm` | — | Tera Starstorm: a plain hiss, a heavy thud, two high notes. |
| `use_fickle_beam` | — | Fickle Beam: a rising hum, a hissing beam, a roaring rise, a thud, a bell. |
| `use_burning_bulwark` | — | Burning Bulwark: five rising notes, a roaring rush, a soft thud, a bell. |
| `use_thunderclap` | — | Thunderclap: a crackle of sparks, a heavy thud, a bell. |
| `use_mighty_cleave` | — | Mighty Cleave: a clatter of stones, a heavy thud, a bell. |
| `use_tachyon_cutter` | — | Tachyon Cutter: a ringing of metal, 3 quick blows, a bell. |
| `use_hard_press` | — | Hard Press: a ringing of metal, a heavy thud, a bell. |
| `use_dragon_cheer` | — | Dragon Cheer: five rising notes, a roaring rise, a soft thud, a bell. |
| `use_alluring_voice` | — | Alluring Voice: a rising hum, a hissing beam, four sparkling notes, a thud, a bell. |
| `use_temper_flare` | — | Temper Flare: a roaring rush, a heavy thud, two high notes. |
| `use_supercell_slam` | — | Supercell Slam: a crackle of sparks, a heavy thud, a bell. |
| `use_psychic_noise` | — | Psychic Noise: a rising hum, a hissing beam, a rising hum, a thud, a bell. |
| `use_upper_hand` | — | Upper Hand: a first knock, a heavy thud, two high notes. |
| `use_malignant_chain` | — | Malignant Chain: sour gurgling, a heavy thud, a bell. |

## Cries

Every species and every form has a cry of its own (plan 05 · A4), synthesised by `Audio/Cries` from its data alone and the same on every run; none of the games' cries is copied or imitated.

- **The voice** (`CryVoice.Of(species, form)`):
  - **Pitch** comes from size. A Pokémon of a few kilograms cries around 600–1000 Hz, a whale or a legendary under 200 Hz (1150 Hz × (weight + 1)^−0.26 × (height + 0.3)^−0.18, nudged by up to 12% by its own name). Legendary and mythical Pokémon are a fifth lower, longer, and ring with an echo.
  - **Length** grows with weight and with each evolution, from 0.35 to 1.45 seconds.
  - **Syllables** follow the vowel groups of the name, one to four: Pikachu cries three, Mew one or two. Each syllable has its vowel's two formants and a shape (rising, falling, an arch, a dip, a trill) chosen by the letters around it. The last syllable is drawn out.
  - **Timbre** comes from its types and is seeded by the first species of its line (`PokemonGenomes.Family`), so a line sounds related and deepens as it grows. The source is a mix of saw, pulse, sine and FM, with breath, a growl and a vibrato. Fire breathes and growls, Electric buzzes, Water gurgles, Steel rings, Ghost wavers with an echo, Bug whirs, Dragon roars.
  - **A form** takes its own size and types and tunes the voice by its name: Giratina's Origin Forme keeps Giratina's syllables at its own pitch.
- **Modes** (`CryMode`, after the original's `POKECRY_*` in `src/sound_playback.c`). The DS pitches a sample by playing it faster or slower, and so do we:

| Mode | What it does | Heard |
|---|---|---|
| `Normal` | As it is. | A wild Pokémon appearing; the party's summary; the starter's question; an evolution, before and after; Giratina at the title. |
| `Pinch` | 1.5 semitones lower. | Sent out with a status condition or with its HP bar not green (24 of its 48 pixels or fewer), unless its HP is full (`Cries.SendOutMode`). |
| `Faint` | 3.5 semitones lower. | As it faints, before its fall. |
| `Half`, `PinchHalf` | Twenty frames of sixty, the last ten fading. | Kept for the moments the original uses them. |
| `FieldEvent` | A semitone higher, with an echo a third of a semitone up beside it at half the volume. | A script's `cry`. |
| `Pokedex` | As it is. | An entry of the Pokédex opening. |
| `Uproar1`, `Uproar2` | Two semitones lower and rumbling (its loudness shaken 28 times a second); then a semitone lower, rumbling harder, cut to twenty frames. | Growl, the second as the first ends (plan 25 · Y6; the original's `UPROAR_1` and `_2`). |
| `Howl1`, `Howl2` | Three semitones lower, ringing out in echoes 0.11 s apart; then a semitone and a half lower with a louder ring. | Roar (the original's `HOWL_1` and `_2`). |
| `HyperVoice1`, `HyperVoice2` | Two semitones higher, doubled seven semitones over itself; then three higher, doubled, ringing and cut short. | Hyper Voice (the original's `HYPERVOICE_1` and `_2`). |
| `MidMove` | A semitone lower, drawn out by an echo 0.16 s behind. | Howl (the original's `MID_MOVE`). |
| `Giant` | Seven semitones lower, doubled an octave below at half its level, ringing in echoes 0.14 s apart at 0.55 for a third of a second more. | A Pokémon grown giant, at its Dynamax's burst (plan 25 · YD1; our own, the original has no Dynamax). |

- **The Pokédex's cry page** (plan 08 · P18, after the original's `crysub.c`): `Cries.Shaped(species, form, CryShape)` bends the Pokédex's cry by a `CryShape` of the page's controls, deterministically, in this order: **PITCH** −24 to +12 semitones (the original's own range is 0 to −24: the sample is played faster or slower, so lower is longer and +12 is half the length), then **CHORUS** 0 to 8 (a second voice 0.3125 semitone above at up to 0.6 of the first's level; or **PAN**, −8 to 8, which changes no sample but the place the cry is played at, `CryShape.PanPosition`), then **FILTER** 0 to 11 (a two-stage low-pass from 9 kHz, each step ×0.72 of the cutoff) or **REVERB** 0 to 11 (one echo 75 ms behind, 0.06 of the level a step up to 0.66), and last the finish every cry gets, so the loudest shape stays at the peak or the cry bus's ceiling. A shape of nothing (or only a pan) is `Cries.Get(..., CryMode.Pokedex)` itself, sample for sample. The last twelve shaped cries are kept (`Cries.ShapedKept`). `AudioManager.PlayCry(species, form, shape)` plays one on the cry bus (tests hear `cry <name> shaped p3 chorus4 reverb0`); `Cries.Scope(samples, playhead, columns)` is the page's scope, GPU-free. The page's LOOP plays the cry again as the screen's playhead ends.
- **Playing one**: `AudioManager.PlayCry(pokemon or species, form, mode, pan)` plays it on the cry bus, which dips the music under it. A battle asks for its Pokémon's cries ahead (`AudioManager.RequestCries`), and they are made on a worker. The last 48 are kept (`Cries.Kept`); a cry takes about ten milliseconds to make.
- **Checking them**: `dotnet run --project tools/MusicRender -- <out dir> --cries [species or form ...] [--modes] [--all]` renders cries with their spectrograms (a sample of every size and type when no species is named; `--all` checks every species and form without writing files). `CryTests` makes every species' and form's cry and holds it clean, unlike any other and inside the cry bus's loudness window as the mixer plays it. A cry is brought to its peak (`Cries.Peak`) unless that would make it louder than `Loudness.CryCeiling`, and then turned down to the ceiling instead: a long, even cry (Leafeon, Florges) at its peak would sound louder than the rest.

## Ambience

The field's background sound (plan 05 · A7): loops ("beds") on the mixer's ambience bus, synthesised in code by `Audio/Ambience` like the sound effects, each made a second longer than it lasts and its last second crossfaded into its first, so it loops without a seam. Every bed is brought to the ambience bus's level (about ten dB under the music), so two can play under a song. What is heard where the player stands is `Overworld/FieldAmbience` (no drawing or audio device), worked out again on each step and whenever the weather changes; the mixer glides each bed to its gain and pan over 1.2 seconds (`AudioMixer.SetAmbience`), so walking up to a waterfall or out of the rain is smooth. A battle, an evolution, the title and the introduction silence it.

| Bed | The original's | Heard |
|---|---|---|
| `Rain` | `SEQ_SE_DP_T_AME` | Rain where the player stands. |
| `HeavyRain` | `SEQ_SE_DP_T_OOAME` | Heavy rain, or a thunderstorm (with `thunder` after its lightning). |
| `Hail` | — | Hail. |
| `Wind` | `SEQ_SE_DP_KAZE` | Snow: softly in light snow, harder in heavy snow. |
| `Blizzard` | `SEQ_SE_DP_KAZE2` | A blizzard: a gale with a whistle in it. |
| `Sandstorm` | — | A sandstorm. |
| `Waterfall` | — | A waterfall within nine tiles: louder the nearer, from its side of the screen. |
| `River` | — | Running water (river tiles) within reach. |
| `Waves` | `SEQ_SE_DP_NAMI` | The sea, or a lake: louder the more of the view is water, from its side. |
| `Cave` | — | In a cave (a map whose setting is a cave): a low rumble, and drips that ring round the walls. |
| `Underwater` | — | On the sea floor (a map whose setting is `Underwater`, plan 21 · H5): the water's muffled hum, the sea overhead swelling and drawing back every six seconds, and now and then a few low bubbles, from neither side. It is heard alone: the weather of the world above and the tiles' water (which are the sea's own down there) are not. |
| `Giant` | — | A battle in which a Dynamaxed Pokémon stands on its last turn (plan 25 · YD3, our own): a slow heartbeat, two low beats every 0.8 s with a soft rumble under them, from neither side, until the Dynamax ends. The only bed heard in a battle; with the Battle Scene off as well, since it is state of the giant standing. |

Clear skies, clouds, fog and ash make no sound, and a room none at all. Water is heard by its tiles' behaviour (`Waterfall`, `River`, `Sea`), each tile within reach counting less the further off it is; the original's `Sea` is also the still water of lakes and ponds, so a lake's shore laps more softly than the open sea only by being smaller. **Checking them**: `dotnet run --project tools/MusicRender -- <out dir> --ambience [bed ...] [--handheld]` renders each bed looped twice, with its spectrogram, its loudness against the bus's window and the size of its seam against its largest step; `AmbienceTests` holds the same, and the field's rules (the weather, the cave, a waterfall walked up to, the sea from the shore, thunder after each flash).

