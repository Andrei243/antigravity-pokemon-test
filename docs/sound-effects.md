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
| `save` | `SEQ_SE_DP_SAVE` | The game is saved. |
| `pc_on` | `SEQ_SE_DP_PC_LOGIN` | A PC is switched on: as its screen comes on in the field (`anim pc on`, plan 09 · L12). |
| `pc_off` | `SEQ_SE_DP_PC_LOGOFF` | A PC is switched off: as its screen goes dark in the field (`anim pc off`). |
| `vs_seeker` | `SE_DP_VS_SEEKER_BEEP_sseq` | The Vs. Seeker is used: it searches for trainers who want a rematch. |
| `heal` | `SEQ_SE_DP_KAIFUKU` | A medicine used from the bag, HP restored in battle. |
| `bike_bell` | `SEQ_SE_DP_JITENSYA` | Getting on the Bicycle (plan 02 · S2). |
| `gear` | `SEQ_SE_DP_GEAR` | The Bicycle's gear changes, on the run button while riding. |
| `boulder` | — | A boulder pushed with Strength, as it starts to slide. |
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

Clear skies, clouds, fog and ash make no sound, and a room none at all. Water is heard by its tiles' behaviour (`Waterfall`, `River`, `Sea`), each tile within reach counting less the further off it is; the original's `Sea` is also the still water of lakes and ponds, so a lake's shore laps more softly than the open sea only by being smaller. **Checking them**: `dotnet run --project tools/MusicRender -- <out dir> --ambience [bed ...] [--handheld]` renders each bed looped twice, with its spectrogram, its loudness against the bus's window and the size of its seam against its largest step; `AmbienceTests` holds the same, and the field's rules (the weather, the cave, a waterfall walked up to, the sea from the shore, thunder after each flash).

