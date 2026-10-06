# Plan 25 · Battle presentation, second pass

Written 2026-10-06, before any session.

**Goal**: a battle that shows everything its rules do. The weather over the stage, a Substitute's doll, what lies on each side of the field and what the end of each turn does to whom; a picture and a sound of its own for every move, Platinum's 467 first; each Pokémon sent out and called back in its own ball; and Pokémon whose bodies answer the battle: a dodge from a miss, tiredness in the red, a cheer for a win. None of it changes a rule. The core only says more about what happened, the screen shows it one line behind, and plan 04's two laws hold: two harness runs draw the same pictures, and a frame stays under 8 ms on High, measured with `profile` before and after.

## Where we are

- **The log and its face** (plan 06 · R2 to R10): `BattleEngine.Show` has a case for every event of `Battle/Sim/BattleLog.cs` but `SubstituteChanged`, which the core emits in six places (a doll made, hit to nothing, faded, passed by Baton Pass, its owner fainting) and the screen ignores. `WeatherChanged` only sets `BattleEngine.Weather`, and nothing under `Graphics` or `UI` reads it. A battle opens under the area's sky (`Weathers.InBattle(map.WeatherAt(...))`, `OpenUnderTheSky`), so since plan 01 · M7 imported Platinum's calendar every battle on Route 215 is fought in rain nobody sees. The style guide's known gaps still say a battle doesn't begin in the field's weather.
- **The field's state** is `FieldState` in `Battle/Sim/BattleField.cs` (the weather and its turns, Trick Room, Gravity), a `SideState` per side (Reflect, Light Screen, Mist, Safeguard, Tailwind and Lucky Chant by turns; Spikes and Toxic Spikes by layers; Stealth Rock) and a `PlaceState` per place (a Wish; a Future Sight or Doom Desire on its way). No event tells the screen about any of them: their lines are bare `Said`s, and so is "Rain continues to fall.".
- **The turn's end**: every residual hurt or heal goes through `LoseHp` and `RestoreHp` (`BattleCore.Context.cs`), which attach an `HpChanged` to the line, and `Show` plays it as a hit (`Anim.Hit`, `hit_normal`) or as a heal cue. A poison tick, Leech Seed, a bind, a sandstorm and Leftovers look like a hit or a Potion. A charge turn (`BeginCharge`, `BattleCore.Moves.cs`) says its line with a `Lunged(..., Status)` and nothing else.
- **Cues** (plan 04 · G8): `CueKind` is `Move`, `StatUp`, `StatDown`, `Heal` and `Status`. `BattleAnimator.Cue` seeds each cue from its own count, and `MoveFx` draws each as a pure function of the cue's age and seed. 146 moves have a recipe of their own (`MoveFx.Overrides`), 145 of them Platinum's (Wild Charge is later), so 322 of the 467 play their type's template (`MoveFx.Template`, by type and category). The shape atlas `FxTextures` is 8 × 4 cells of 128 px, and all 32 cells are used. `FxList` counts its quads and ribbons (`Count`) and carries the screen's `Flash` and `Shake`.
- **Sound**: a move sets off with its type's sound (`SoundBank.MoveSound(type)`, the eighteen `move_<type>`; [`docs/sound-effects.md`](../sound-effects.md), "Moves"). That is plan 05 · A3's decision, made "until plan 04's move effects ask for sounds of their own". `CryMode` has A4's seven modes, and A4 left the move-specific ones for this work.
- **Balls**: `BattleRenderer.PlaceSendOutBall` asks `BattleBall.Get("Poké Ball")` for every send-out, and every release is the same white `Burst`. `Pokemon.Ball` (null for a Pokémon that wasn't caught) is set by `Show`'s `Caught`, saved and copied by `CopyStateFrom`, and nothing in a battle reads it. A recall (`Recalled`, `BattleAnimator.Recall`) shrinks the model into red light (`Appear`'s `FlashColor`) with no ball. `BattleBall.LookOf` knows fifteen kinds; the Cherish Ball in `items.json` falls back to the Poké Ball's look. The trainer leaves the platform after the first send-out, and later send-outs come in from that side of the field (style guide, "Poké Ball (G8)").
- **Bodies**: `PokePose` is `Time`, `Blink`, `Attack`, `Kind`, `Hurt`, `Faint` and `Entry`, built each frame from a `CombatantView` in `BattleRenderer`. `PokemonAnimation.EyesOf` gives `Shut`, `Squeeze` or `Fierce` for a faint, a hit or an attack and nothing else. An imported model's clips are matched by name to seven `ClipRole`s (`ImportedModels.cs`). A miss (`MoveShown.Missed`) flies past a target that never moves, a Pokémon in the red breathes as it does at full health, and nobody moves at `Won`.
- **The original**, at the pinned commit:
  - `generated/battle_sub_animations.txt` lists its forty battle animations that are not moves: one for each condition (asleep, poisoned, burned, frozen, paralysed, confused, infatuated), level up, a bag item, a held item, shiny, a stat's boost and drop, HP restored, the doll's four swaps, an escape item, the five weathers, the Great Marsh's happy, eating and angry, and the damage from a curse, a nightmare, Leech Seed, Ingrain and the seven binding moves.
  - `BtlCmd_PlayBattleAnimation` (`src/battle/battle_script.c`) plays them only with the Battle Scene on, apart from the doll's four. On a Pokémon behind a Substitute or out of sight, `BattleSystem_ShouldShowStatusEffect` (`battle_lib.c`) shows only the weathers, the doll's swaps and the escape. After a poison tick the bar drains with no blink (`SYSCTL_SKIP_SPRITE_BLINK`), and Future Sight lands with its move's own animation.
  - Each move's animation is a script of its own (`res/moves/<move>/anim.s`). Of the 460 found under their folder names, 107 darken the background, 279 shake a Pokémon, and together they play 261 different sounds. Five play the user's cry in a mode of their own: Growl (`UPROAR_1`, `_2`), Roar (`HOWL_1`, `_2`), Hyper Voice (`HYPERVOICE_1`, `_2`), Howl (`MID_MOVE`) and Chatter (its plain cry). Secret Power plays one plain hit.
  - Nothing is drawn while a hazard, a screen or Trick Room lasts, no ability has an animation of its own (`subscript_intimidate` plays only the stat drop), and a trainer's Pokémon are made in Poké Balls (`BoxPokemon_InitWith`, `src/pokemon.c`; the trainer data names only a seal, `ball_seal`).
- **The frame** (plan 04 · G11, High, the 1080p window): a battle at its menu takes 6.73 ms, a double battle at its menu 7.00, and Surf's wave arriving 7.47, the heaviest moment measured.

## Design

**The screen is told, never asks.** Anything new on screen starts as an event of `BattleLog.cs` with a case in `BattleEngine.Show`, as plan 06's Risks require. The core attaches the event to the line that tells of it with `Say(...).With(...)`. The screen therefore stays a line behind, no roll changes, and a `BattleRecord` replays to the same log, events included. What lasts (the weather in the air, the doll, what lies on the field) is state of `BattleAnimator` that `Show` sets from those events. It is never read from `core.Field`, which is a turn ahead. `SyncFromCore` brings it level whenever a menu opens, as it already does for `Weather`. Every look is a function of a cue's or a state's age and of a seed from the animator, drawn as quads and ribbons by `FxRenderer` inside the 3D pass. This plan adds no pass and no render target.

**One shape for a condition's event**, shared with plan 09 · L2's `VolatileChanged` and plan 12 · Q11's `AbilityShown` and `ItemShown`: where it is, what it is, and whether it is up. The new events:
- `WeatherContinues(BattleWeather)`, on the turn's weather line.
- `SideConditionChanged(BattleSide, SideCondition, int Layers)`: a screen, a veil, Tailwind or Lucky Chant at 1 or 0; Spikes 0–3, Toxic Spikes 0–2, Stealth Rock 0–1.
- `FieldConditionChanged(FieldCondition, bool Up)`: Trick Room (`OpenUnderTheSky`'s too) and Gravity.
- `PlaceConditionChanged(Place, PlaceCondition, bool Up)`: a Wish, or a Future Sight or Doom Desire on its way.
- `MomentShown(Place, BattleMoment, Place? From)`: the turn's moments.
- `Charging(Place, string Move)`: on a charge turn's line.

The three condition events go on every line that sets, wears off or clears a condition: Brick Break, Defog, Rapid Spin, a Poison type taking up Toxic Spikes, and the turn's end. Q11's `ItemShown` comes in Q11's shape from whichever session is first.

**The original's list is the checklist.** Of its forty animations, stat boost and drop, HP restored and a condition given are drawn already (G8). Shiny is L1's, and a condition that lasts is L2's. This plan draws the rest: the weathers and the doll's swaps, a held item through `ItemShown`, and 24 `BattleMoment`s, each named after its original (asleep, poisoned, burned, frozen, paralysed, confused and infatuated as each plays on its turn; level up, bag item, escape item; the marsh's happy, eating and angry; curse, nightmare, Leech Seed, Ingrain and the seven binds). The screen follows `BattleSystem_ShouldShowStatusEffect` when it decides what to show, from what it already draws; the core never filters. A moment that hurts plays before its `HpChanged`, and the bar then drains with no flicker and no `hit_normal`. Future Sight and Doom Desire land with their own move's cue, from the place they were sent from.

**What Platinum doesn't draw is ours, and the style guide says so.** Hazards lying on a platform, panes for the screens and veils, Trick Room's walls while it lasts, a dodge, tiredness and a cheer are not in the original. Each is written into the style guide as our own presentation before the code (decision 1). With plan 08 · P1's Battle Scene off, a state stays drawn and a moment goes (decision 2). The doll swaps either way, as the original always swaps it.

**Weather on the stage.** A new `ArenaFx.Weather(weather, age, time, fx)` gives:
- rain in streaks, with rings on the platforms;
- the harsh sun's motes and a warm haze;
- sand blown across the stage;
- hail bouncing;
- fog lying in low layers.

The light goes through `ArtLook.Weathered`, using the field weather each battle weather comes from (`Weathers` maps both ways). The harsh sun has a rig of its own (a stronger, warmer key and more bloom), because the field has no such weather. A cue plays as each weather starts and on each turn it goes on, and the layer stays for as long as the weather lasts. A snow or sand arena's own drift gives way to the battle's weather.

**The doll.** A new `Graphics/SubstituteDoll.cs` is a small figure of our own, sculpted with the SDF kit and meshed once and cached like `BattleBall`. There is one look for everyone, sized to the place. `CombatantView.Substitute` holds it and its age:
- the Pokémon shrinks out of sight as the doll drops in (`SUBSTITUTE_IN`);
- the doll flinches for the hits it takes (the flicker of `subscript_hit_substitute`);
- it slides aside for its owner's own moves and back after them;
- when it breaks or a form changes behind it, it fades in a puff and the Pokémon grows back (`SUBSTITUTE_OUT`, `SUB_IN`);
- a doll passed by Baton Pass comes in with the next Pokémon.

**What lies on the field** (decision 1). A new `BattleAnimator.FieldLook`, drawn by a new GPU-free `Graphics/BattleFieldFx.cs`:
- Spikes are one to three rings of caltrops round the platform's rim, Toxic Spikes the same with purple tips, and Stealth Rock pointed stones hovering at the platform's edge;
- Reflect and Light Screen are thin warm and cool panes in front of their side, Safeguard and Mist a faint dome, Tailwind streaks, and Lucky Chant a few charms;
- Trick Room is a grid of light walling the field, and Gravity lowers the stage's light;
- a Wish is a star over its place, and a Future Sight a mote waiting over its target.

Each appears with the move's own effect and fades when its line says it is gone. Each stays small and low and never covers a Pokémon's body. Plan 06 · R19's terrains, Aurora Veil and new hazards join these enums and this file with their session.

**Each Pokémon in its own ball.** `PlaceSendOutBall` asks for `Pokemon.Ball ?? "Poké Ball"`. A trainer's Pokémon is in a Poké Ball, as Platinum makes them (decision 5). Each kind has its own release: a new `BattleBall.ReleaseOf(look)` takes its colours from the look and adds matter of its own (the Great Ball's blue sparks, the Dive Ball's bubbles, the Net Ball's mesh, the Luxury Ball's gold, the Dusk Ball's dark motes, the Heal Ball's hearts, the Quick Ball's streaks), and `Burst` draws the release it is handed. A recall:
- the ball comes in from where the next send-out would come from, since the trainer has left the platform;
- it opens and a red beam draws the Pokémon in;
- it shuts and flies back out.

Plan 06 · R17's seals add their stickers to this release; they never replace it. The Cherish Ball gets a look of its own.

**Bodies that answer.** `PokePose` gains `Dodge`, `Tired`, `Cheer` and `Charge` (0..1 each), and `PokemonAnimation` gains a clip of each for every body plan:
- **Dodge**: as a miss passes at `ImpactTime`, a biped or a quadruped hops aside, a bird banks, a fish or a floater dips and a serpent coils, then each comes back.
- **Tired**: the head lowers and the breath slows and deepens while the bar is in the red (`BattleAnimator.LowHpRatio`, read from `DisplayedHp`, so it follows the bar).
- **Cheer**: two hops with the head up, on `Won` or `Caught` for the player's side and on a loss for the foe's.
- **Charge**: the Pokémon gathers itself low through its charge turn.

`EyesOf` follows one order, which the style guide's "Clips" bullet writes down:
- a faint, then asleep (L2), shut;
- a hit, squeezed;
- an attack or a charge, fierce;
- a held fierce look (plan 10 · F10);
- a stat falling, squeezed for 0.6 s, and a stat rising, fierce for 0.6 s;
- a cheer, shut in a smile's curve;
- tiredness, longer blinks;
- otherwise the blink.

`ClipRole` gains `Dodge`, `Tired` and `Cheer`, matched by name ("dodge", "evade"; "tired", "lowhp"; "win", "victory", "cheer"). An imported model without one of them plays its body plan's clip.

**Moves of their own.**
- **The recipe**: a move's look is a recipe in `MoveFx`, with a file per batch (`MoveFx.Platinum1.cs` and on), built from the `Fx` primitives. New primitives and cells are added where a move needs them, and Y6 grows the atlas to 8 × 8. Secret Power's look by its ground is our own, as plan 06 · R6 asked; `EffectCue` gains the battle's `BattleTerrain` for it and for Camouflage.
- **The sound**: a `SoundBank` entry `use_<move key>`. The prefix is its own because the move Psychic and the type's `move_psychic` would otherwise clash. Its `Original` names the original's first sound, and `SoundBank.MoveSound(move, type)` falls back to the type's sound.
- **Read first, then written**: each batch first reads each move's script for its beats: its parts in order, when it lands, whether the background darkens, who shakes, and the names of its sounds. The particle files (`*_spa`), sprites and palettes are art and are never opened. Shapes, colours and sounds are our own.
- **What every recipe holds to**: it lands at `ImpactTime` (a new `FxList.Landed` mark, set by `Impact` and by any recipe that lands its own way), ends by `EffectTime`, and at no age draws more quads than `MoveFx.MaxQuads`, the heaviest of G8's 146 as measured in Y6.
- **The cries**: the cry modes that Growl, Roar, Hyper Voice and Howl ask for join `CryMode` in Y6.

**The batches follow the story.** Y6 works the order out once from the data, as a GPU-free `MoveSightings` in the test project. For each Platinum move it finds the first area, in plan 01's order of opening, where the player can see it: in a trainer's team as `TrainerDatabase.Fill` builds it, in a wild Pokémon of the area's tables at its level, or in a TM or HM found there. Y6 takes the first chapter's moves itself and writes the rest into Y7 to Y10 in that order, about seventy to a session. Moves that nothing shows before the post-game close Y10. The later games' moves follow plan 06's batches R24–R26, the Z-Moves R21 and the Max Moves R22. Until then each plays its type's template.

**The frame.** Each session times its scenes before and after, back to back: `profile`'s battle scenes on High, and on all three presets for a look drawn every frame. The plan adds four scenes to `profile`:
- a battle in rain at its menu;
- a double battle in a sandstorm, with every hazard and screen on both sides;
- a Substitute up;
- each batch's heaviest move at its three moments (by `FxList.Count` at impact, which the move board prints).

A look that pushes a scene over 8 ms is cut down before it lands. No look goes to the lower presets only, because a battle's state must read the same on every machine. Every look reads `BattleAnimator.Time`, a cue's age and an animator seed, never `Raylib.GetTime`, `Random` or a string's hash.

## Sessions

### Y1 · The weather and the doll
- The style guide first: new "Weather in battle" and "The Substitute" under "Battles (3D)"; the known gap about a battle's weather rewritten.
- `WeatherContinues` on the turn's line (`BattleCore.Turn.cs`). `Show` starts a weather's cue and its layer on `WeatherChanged`, and finally gets a case for `SubstituteChanged`.
- `ArenaFx.Weather` and the rigs under each weather; `CombatantView.Substitute`; `Graphics/SubstituteDoll.cs`, preloaded with `BattleBall`'s; the doll's swaps and flinch in `BattleRenderer`; the rule for what shows behind a doll; five weather sounds and the doll's two in `SoundBank`, with their rows in `docs/sound-effects.md`.
- Tests: the new event on each "continues" line and the log otherwise unchanged; `ARecordedBattleReplaysTheSame` and the random battles with the new events; the view's doll up and down by the lines, not by the core; the weather's and the doll's looks pure in age and seed; a status cue hidden behind a doll while a weather's shows.
- Harness: `conditions` gains `96_weather_<kind>` for the five, both at a menu and at their turn's line, and `96_substitute_up`, `_aside` and `_faded`; `arenas` gains a grass arena in rain and one in harsh sun; `profile` gains the rain scene.
- **Done when** a battle on Route 215 is fought in rain that can be seen, a Substitute stands in front of its Pokémon until it fades, and the battle at its menu in rain stays under 8 ms on High.

### Y2 · What lies on the field
- The style guide first: a new "What lies on the field", each look written as our own.
- `SideCondition`, `FieldCondition` and `PlaceCondition` with their events, attached in `BattleCore.Effects.cs`, `BattleCore.Turn.cs` and `BattleCore.Switch.cs` wherever one begins or ends; `BattleAnimator.FieldLook` and its cases in `Show`; `Graphics/BattleFieldFx.cs`.
- Tests: an event on every line where each condition begins or ends, walked through scenarios from `BattleFieldTests`; the look after a battle's last line equal to `core.Field`'s state; every look pure and under its quad cap.
- Harness: `96_hazards` (three Spikes, two Toxic Spikes and Stealth Rock on the foe's side), `96_screens`, `96_trick_room`, `96_gravity`, `96_tailwind` and `96_wish`; `profile` gains the double battle in a sandstorm.
- **Done when** every state in `SideState`, `FieldState` and `PlaceState` can be seen while it lasts and is gone when its line says so, and the full double battle stays under 8 ms on High.

### Y3 · The turn's moments
- The style guide first: "Marks on a Pokémon" in "Move effects (G8)" gains each moment.
- `BattleMoment`, and `MomentShown` attached by the callers of `LoseHp` and `RestoreHp` in `BattleCore.Turn.cs`; by the checks before a move for a sleeping, frozen, paralysed, confused or infatuated Pokémon (`BattleCore.Moves.cs`); for the marsh's bait and mud (`BattleCore.Special.cs`, a place since plan 01 · M7); for the bag's items, the escape item and `LevelRose`.
- Q11's `ItemShown` where the original plays its held-item animation (`HeldItemEffects`, `BattleCore.Items.cs`). A cue for Future Sight's and Doom Desire's landing. `CueKind.Moment` and its looks in `MoveFx`. `Show` plays a moment before its `HpChanged`. A sound for each moment.
- Tests: a scenario (`CoreScenario`) for each `BattleMoment` whose log carries it on its line; each look draws and stops; Leech Seed's travels from the seeded to the seeder; a poison tick leaves `HitAge` at -1; `AiMemory` still passes `TrainerAiTests`; the replay holds.
- Harness: `96_turn_<moment>` for poison, burn, Leech Seed, Fire Spin, curse, Leftovers, a berry and the marsh's bait, and `96_future_sight`.
- **Done when** the end of a turn can be read from the picture alone (who is poisoned, who is seeded and by whom, which item went off), and a poison tick no longer looks like a hit.

### Y4 · Each its own ball
- The style guide first: "Poké Ball (G8)" gains each kind's release and the recall.
- The Pokémon's own ball in `PlaceSendOutBall`; `BattleBall.ReleaseOf`, with `Burst` taking the release; the recall's ball and beam; the Cherish Ball's look. A Pokémon that breaks out of a thrown ball keeps that ball.
- Tests: the ball chosen is the Pokémon's own, and a Pokémon never caught or a trainer's comes in a Poké Ball; every kind's release is distinct and pure; the recall's ball has left the field when the line goes on.
- Harness: `demo` gains a Dive Ball and a Luxury Ball send-out after its own shots; `conditions` gains `96_release_<kind>` for the fifteen and `96_recall`; `versus` shows its lead's ball.
- **Done when** a Pokémon caught in a Luxury Ball comes out of one in gold and goes back into it, and `demo`'s Poké Ball send-outs show no change under `diff`.

### Y5 · Bodies that answer
- The style guide first: the "Clips" bullet of "Pokémon (version 2, G7)".
- `PokePose.Dodge`, `Tired`, `Cheer` and `Charge`, with the six body plans' clips; `EyesOf` in the order of the design, keeping L2's and F10's places whether or not they have landed; the views' ages, set in `Show` from `MoveShown.Missed`, `Won`, `Caught`, `Ended` and the new `Charging` on `BeginCharge`'s line.
- `ClipRole.Dodge`, `Tired` and `Cheer`, with their names and the fallback. A move's charge half on the cue (`EffectCue.Charging`): the template gathers an aura of its type, and the charge moves get theirs in their batches.
- Tests: each clip pure in time, starting and ending at rest with planted feet (G7's tests extended); the dodge clear of the miss's path at `ImpactTime`; tiredness taken from the bar, not from the HP; `EyesOf` case by case; an imported model with and without the clip; `Charging` on every charge turn, and the replay holds.
- Harness: `96_dodge`, `96_tired`, `96_cheer` and `96_charge` in `conditions`; the `pokemon` mode's `92_clips_*` boards gain the three clips; `profile`'s battle at its menu with both Pokémon tired.
- **Done when** a miss is dodged, a Pokémon in the red is seen to be, the winner cheers, and the battle at its menu costs what it did before.

### Y6 · Moves of their own: the kit, the board and the first chapter
- The style guide first: "Move effects (G8)" gains "Moves of their own" (beats read from the original; shapes and sounds our own; the landing, the length, the cap) and the new cells.
- The atlas at 8 × 8 (`FxTextures.Rows`), `FxList.Landed`, `MoveFx.MaxQuads`, `EffectCue`'s terrain for Secret Power and Camouflage, and the per-batch files.
- The sounds: `SoundBank.MoveSound(move, type)` and the `use_<key>` entries, with their rows under "Moves". The cry modes the five moves ask for, with their rows under "Cries".
- `MoveSightings`, with the order written into Y7 to Y10 here; the first chapter's moves (Twinleaf Town to Oreburgh's gym, about forty).
- A new harness mode, `moves [move ...]`, not part of `all`: boards of sixty moves at impact (`mv_NN`), each with its name and `FxList.Count` and the template's moves marked, and four moments of each move named (`mv_<move>`).
- Tests: `MovePresentationTests`. Every recipe is pure in age and seed, lands at `ImpactTime` within a frame, draws nothing after `EffectTime` and stays under `MaxQuads`. Floors for Platinum's moves with a look and a sound of their own are raised to the batch, as `CoverageTests` does. Every `use_` sound renders clean and inside the effects' loudness window (`SoundTests`), and the new modes pass `CryTests`.
- Harness: the board; `demo` against its run before; `profile` with the batch's heaviest move.
- **Done when** the first chapter's battles show and sound each move as its own, the board shows all 467 with the templates marked, and `profile` holds.

### Y7 · Platinum's moves, batch 1
- About seventy moves in Y6's order, each read first and written: recipe, sound, any new primitive or cell (the style guide's shapes line first); the floors raised.
- Tests and harness as Y6: the batch on the board, its heaviest in `profile`, `demo` against its run before.
- **Done when** the batch's moves have their own look and sound, the floors are raised, and `profile` holds.

### Y8 · Platinum's moves, batch 2
- About seventy more, as Y7.
- **Done when** as Y7.

### Y9 · Platinum's moves, batch 3
- About seventy more, as Y7.
- **Done when** as Y7.

### Y10 · Platinum's moves, batch 4 and the 467
- The rest of Platinum's moves, the post-game's last; the floors become "every one", as plan 06 · R6's report became 467 of 467.
- **Done when** none of Platinum's 467 moves plays a template or its type's sound, and the board's eight pages show it.

### Y11 · Later moves, batch 1
- After plan 06 · R24 (about 127 moves): their recipes and sounds, read from Pokémon Showdown's move data for what each does and from no game's art; their own floor.
- **Done when** every move R24 made usable has its own look and sound, and `profile` holds.

### Y12 · Later moves, batch 2
- After plan 06 · R25, as Y11.
- **Done when** as Y11.

### Y13 · Later moves, batch 3
- After plan 06 · R26, as Y11; the later floor reaches every regular move.
- **Done when** all 847 regular moves have their own look and sound.

### Y14 · Z-Moves
- After plan 06 · R21: the 35 Z-Moves (type and exclusive) and the gathering of Z-Power before each, on the event R21 attaches; a status move's Z-Power cue before its own effect.
- **Done when** each Z-Move is its own and a recorded battle with one replays the same.

### Y15 · Max Moves and G-Max Moves
- After plan 06 · R22: the 19 Max Moves and 33 G-Max Moves, sized for a Dynamax Pokémon, their side effects drawn through Y1's weathers and Y2's field looks.
- **Done when** every Max and G-Max Move is its own and the heaviest stays under 8 ms on High.

## Risks

- **The frame.** Lasting looks are drawn every frame, and a double battle at its menu was already 7.00 ms. Every look is timed back to back in `profile`, capped in quads by a test, and cut down if a scene crosses 8 ms.
- **Size and taste.** 467 Platinum moves and about 470 later ones are our own looks and sounds. The board and `tools/MusicRender --sounds use_` are what the user reviews each batch, and a batch that runs long splits in two.
- **Overlap.** Plan 09 · L2 and plan 10 · F2 and F10 add fields to `PokePose` and cases to `EyesOf`. One order, written in the style guide, decides between them, whichever plan lands first.
- **The log grows.** New events go on many lines. The replay and random-battle tests carry them; a test that counts a line's events changes with them.
- **Test time.** Up to 900 more sounds render in `SoundTests`. If the run grows by more than a few seconds, the full check moves to plan 16 · T4's nightly, and every run keeps a sample of each batch.
- **Determinism.** A `new Random()` or a string's hash in a recipe breaks `diff`. The purity tests catch it.

## Needs and gives

- **Needs**: plan 04 · G8's kit and G11's `profile` (done); plan 06 · R2 to R10 (done). Y11 to Y15 wait for plan 06 · R24–R26, R21 and R22. Y3's `ItemShown` is plan 12 · Q11's in shape, and whichever comes first adds it. Y5 shares `PokePose` and `EyesOf` with plan 09 · L2 and plan 10 · F2 and F10, in either order. Plan 08 · P1's Battle Scene off skips Y's moments (decision 2), and plan 12 · Q9's `ReduceFlashes` and `ReduceMotion` reach every new flash and shake through `FxList.Flash` and `Shake`.
- **Gives**: plan 06 · R6 the doll, the weather and Secret Power's ground it handed to plan 04; plan 06 · R17 a release for its seals; plan 06 · R19 the field looks its terrains, Aurora Veil and new hazards join; plan 06 · R20–R23 the cue kinds their transformations use (Mega Evolution's light, Dynamax's growth, the Tera crystal; plan 06's Risks gave these to plan 04 · G8, which is closed, so they come with their own sessions); plan 05 the move sounds A3 left and the cry modes A4 left; plan 08 · P10–P11's replays and plan 12 · Q17's photo mode in battle a battle with more to see; plan 14's hordes and triples per-place and per-side looks that take more places as they are.

## Decisions for the user

1. **Draw hazards, screens and Trick Room while they last?** Platinum draws only the move as it is used. *Recommended:* yes, small and low, as our own (the later games show hazards on the field), since otherwise only lines that have scrolled away tell of them. Alternative: Platinum's way, with plan 12 · Q11's panel telling the rest.
2. **What does the Battle Scene off take away?** *Recommended:* the moments go (a move's own effect, the turn's moments, a release's sparks, a dodge, a cheer) and the state stays (the weather in the air, the doll, what lies on the field, a tired breath); the original keeps only its Substitute swaps. Alternative: everything this plan adds goes with the scene.
3. **A sound per move, or per original sound?** *Recommended:* one entry per move, built from shared layers, so a test holds every move to its own. Alternative: about 260 sounds shared as the original shares its own.
4. **How far do the move batches go?** *Recommended:* every move: Platinum's 467, then the later ones and the Z- and Max Moves with plan 06's sessions. Alternative: stop at the 467, and later moves keep their type's template.
5. **Which ball does a trainer's Pokémon come out of?** *Recommended:* Platinum's Poké Ball for every trainer, with plan 06 · R17's seals where the trainer data names one. Alternative: a ball by trainer class, our own choice.

## Status

- [ ] Y1 The weather and the doll
- [ ] Y2 What lies on the field
- [ ] Y3 The turn's moments
- [ ] Y4 Each its own ball
- [ ] Y5 Bodies that answer
- [ ] Y6 Moves of their own: the kit, the board and the first chapter
- [ ] Y7 Platinum's moves, batch 1
- [ ] Y8 Platinum's moves, batch 2
- [ ] Y9 Platinum's moves, batch 3
- [ ] Y10 Platinum's moves, batch 4 and the 467
- [ ] Y11 Later moves, batch 1
- [ ] Y12 Later moves, batch 2
- [ ] Y13 Later moves, batch 3
- [ ] Y14 Z-Moves
- [ ] Y15 Max Moves and G-Max Moves
