# Plan 28 · Battles for two players

**Goal**: the battle rules can be played by two people on two machines, with a server in between that runs the battle, sends each player only what their own screen would show, waits for both choices, and can bring a dropped player back into the same battle. This is the battle half of plan 07 (online trading and battles); it replaces that plan's session O3 and is what its session O6 builds on. Hot-seat versus on one machine (plan 13 · V12) uses the same work.

Written 2026-10-12 at the user's request, before any session, from the code on `main` at `7cdd015`. It has the same shape as the other plans: where we are, design, sessions, decisions.

## Where we are

Plan 06 · R1 and R2 already rebuilt the battles in the shape a server needs, so this plan finishes a rework rather than starting one.

- **The rules alone**: `Battle/Sim/BattleCore` has no raylib, input, audio or clock. It asks (`Request`: an `ActionRequest` for the places that must choose, or a `ReplacementRequest` for one place after a faint), is answered (`Submit` of `BattleChoice`s: fight, switch, item, run, bait, mud), works out everything up to the next question and writes it into a log (`BattleLog.cs`, about forty event types) that `BattleEngine` plays through the HUD, the animator and the sounds.
- **Who chooses**: `CoreSetup` has a `PlayerController`, an `EnemyController` and a `PartnerController`. A null one makes the core ask that side from outside, which is how a remote player is meant to come in.
- **Chance**: one seedable `BattleRandom` for every die; the screen's own randomness is separate.
- **Replays**: `BattleRecord` (the seed and every answer) replays to the same log (`BattleCoreTests.ARecordedBattleReplaysTheSame`), and plan 24 · X2's 200 golden battles hold the logs still.
- **Close cousins**: `CoreSetup.Frontier` already makes a battle give no EXP, no money and no bag, makes every Pokémon obey and turns RUN into giving up, which is nearly what a link battle is.

What is still missing, from plan 07's notes and a read of the code:

1. **The log points at objects.** `Entered`, `ExpGained`, `LevelRose`, `MoveWanted` and `Caught` carry a `Pokemon`, and `Seen` a `PokemonSpecies`. That works inside one program and not over a wire: the other machine has its own copies.
2. **Everyone sees everything.** `Struck` and `HpChanged` carry exact HP for both sides, items and abilities are in the battle's objects, and nothing decides what one side may know. A server that sends the whole log would hand a modified client the foe's exact HP and anything else the log says.
3. **The menus ask only the player's side.** `BattleEngine.Menus.BeginChoosing` keeps the places whose side is the player's, and a trainer's side sends its next Pokémon in party order where a person would be asked.
4. **A battle's state can't be written out.** A battle can be rebuilt by replaying its record, which is enough to bring a player back while the server keeps running; nothing writes the state itself, so a battle can't survive a server restart, and a long battle can only be resynced by replaying it from the start.
5. **No link battle as such.** No setup for two players' teams on copies, no level rule, no clauses, no team preview, no forfeit choice, no default choice for a turn timer that runs out.

## Design

### Principles

- **The server is the only one who rolls.** Clients never run `BattleCore` for a link battle; they send choices and play the events they are sent. So the two clients can't disagree, and a modified client can't change a roll.
- **Nothing leaves the server that the player's screen wouldn't show.** The view for a side is made on the server, before sending, by one function with its own tests.
- **One code path for every way to play.** A battle against the AI, hot-seat versus (plan 13 · V12) and a link battle are the same core with different controllers and the same events; the game's own battles change in no visible way, and the golden battles must not move except where a session says so.
- **Plain data on the wire.** Every event, request and choice is a record of numbers, strings and enums that `System.Text.Json` writes and reads back unchanged, with a type name and a protocol version.

### The log on the wire (I1)

- A new `PokemonRef(BattleSide Side, int PartyIndex)` replaces the `Pokemon` in every event, and species are named by dex number and form rather than by object. The client turns a reference back into its own copy through the parties it was given at the start.
- `Said(Text)` stays English text for now: it is already what the screen shows, and plan 17 (localisation) turns the battle's lines into keys for every language at once. The wire format carries it unchanged.
- A `BattleWire` class reads and writes every `BattleEvent`, `BattleRequest` and `BattleChoice` as JSON with `System.Text.Json`'s type discriminators. A test runs each of the golden battles, writes its log, reads it back and compares, so a new event without a wire name fails the build's tests.
- A test scans `Battle/Sim` for anything that would make two runs differ: `Random.Shared`, `new Random(`, `DateTime.Now`, `Environment.TickCount`, and enumerating a `HashSet` or `Dictionary` where the order decides something.

### What each side may see (I2)

`BattleView.For(log, side)` turns the whole log into what one side's screen shows, following Platinum's link battles:

- **HP**: exact numbers for your own Pokémon; for the foe's, the bar's share (in 48ths, as Platinum's bar draws it) and nothing finer.
- **Moves**: a foe's move is known once it has been used; its PP never.
- **Items and abilities**: a foe's only once an `ItemShown` or `AbilityShown` names it (plan 12 · Q11's events), which is also when the information panel and `AiMemory` learn them.
- **The team**: the foe's party as the team preview showed it (species, form, level, gender, shiny; no moves, items or stats) until each Pokémon comes out.
- **Choices**: the other side's choice is never sent before the turn is worked out; the server sends a turn's view only after both answers are in.

The view is a filter over the same events, so `BattleEngine` plays a view exactly as it plays a full log. A test plays each golden battle and checks every view against a list of what may appear in it; another plays the same battle from both sides and checks that each side's view, played on its own `BattleEngine`, ends with the same Pokémon fainted and the same winner.

### Both sides asked (I3)

- `BeginChoosing` asks every place in the `ActionRequest` that has no controller, not only the player's, and a `ReplacementRequest` for any side is asked of that side's chooser. With a controller (the AI) nothing changes.
- A chooser is one of three: this machine's menus, the AI, or **a remote player**, whose answers arrive from outside. For a remote side the screen shows "Waiting for {name}…" and the turn plays when the server's next view arrives. The client does not choose for the remote side and never sees its menus.
- Plan 13 · V12 needs the same change for hot-seat versus (its curtain between the two players and the HUD drawn from the chooser's side). Whichever of V12 and I3 comes first writes the menu change; the other adds only its own chooser.
- A test bench, `LinkBattleBench` in the test project, joins a `BattleCore` to two headless `BattleEngine`s through I1's wire and I2's views, with no network, and plays golden battles through it. The harness gets a `link` mode that shoots both players' screens on the same turn.

### A link battle's setup (I4)

- `CoreSetup.Link` (or a `BattleKind.Link`), built on what `Frontier` already does: no EXP, no money, no bag, every Pokémon obeys, items and PP are back to how they were when the battle ends, nothing in either save changes but the win or loss.
- **Teams on copies**, levelled by plan 14 · B11's `LevelScaling` for **Flat 50**, **Flat 100** or **As is** (written here if I4 comes first, as plan 13 · V12 also allows).
- **Clauses**, checked when the teams are picked: species clause and item clause, as in Platinum's own link rooms. A sleep clause is a later games' rule and is offered only under the modern rules.
- **The rules set**: the room picks Platinum's or the modern rules (`Ruleset`), so two saves made under different rules battle under one. Flat 50 under Platinum's rules is the default.
- **Team preview**: before the first turn each side sees the other's six (as I2 allows) and picks its order, and for a "pick three of six" room which three.
- **Giving up and the timer**: a new choice, `ChoiceKind.Forfeit`, ends the battle for the side that gives it; `BattleCore.DefaultChoice(place)` gives the choice a timer that ran out makes (the first move with PP, or Struggle, or the first Pokémon able to come in), so the server never decides by itself what a player "would have" done.
- **Results**: a `LinkResult` (winner, turns, how it ended: knocked out, forfeit, timer, disconnect) that plan 07 · O6 records against the two friend codes.

### State written out (I5)

- `BattleSnapshot` writes a battle's whole state as JSON: the two parties, every `Battler` with its stages, volatile effects and counters, the field (weather, screens, hazards, terrain), the turn number, the pending request, and the dice (`BattleRandom.Resume` already restores them from one number).
- `BattleCore.Resume(snapshot)` carries on from it.
- A test snapshots each golden battle after every turn, resumes, and checks that the rest of the log is the same as the uninterrupted run.
- Plan 07 works without it (the server replays the record to bring a player back), so I5 can follow O6. It is needed for a server that keeps battles across a restart, for spectators joining halfway, and for plan 16's recorded play.

## Sessions

| # | Session | Delivers | Needs |
|---|---|---|---|
| I1 | The log on the wire | `PokemonRef` and species by number in every event; `BattleWire` for events, requests and choices; the round-trip test over the golden battles; the determinism scan | A wave when lane 3 is away from the battle's log (as the overview placed O3) |
| I2 | What each side may see | `BattleView.For`, HP as the bar for the foe, moves, items and abilities once shown, the team as previewed; the two tests above | I1 |
| I3 | Both sides asked | Every chooserless place asked; the remote chooser and its waiting line; replacements asked of their own side; `LinkBattleBench`; the harness's `link` mode | I1, I2; shares the menu change with plan 13 · V12 |
| I4 | A link battle's setup | `CoreSetup.Link`, copies at Flat 50, Flat 100 or As is, species and item clauses, the rules set, team preview, `Forfeit`, `DefaultChoice`, `LinkResult` | I3; plan 14 · B11's `LevelScaling` (or written here) |
| I5 | State written out (can follow plan 07 · O6) | `BattleSnapshot`, `BattleCore.Resume`, the resume test over every turn of the golden battles | I1 |

Each session builds the solution, the harness and `tools/MusicRender`, runs the battle tests and the golden battles, and adds tests for every new rule. I1 moves every golden battle's file once (the events name places instead of objects); it regenerates them with `GOLDEN=accept` and says so. No other session should move a golden battle.

## Needs and gives

- **Needs**: plan 06's core as it is; plan 14 · B11's `LevelScaling` (or I4 writes it); plan 12 · Q11's `AbilityShown` and `ItemShown` (done).
- **Gives**: plan 07 · O6 (link battles on the server), O1 (the core library split takes `Battle/Sim` with the wire format), plan 13 · V12 (both sides asked), plan 08's Vs. Recorder (records that travel), plan 16's recorded play and plan 27's Unity port (a battle as plain data).

## Risks

- **Lane 3 is still changing the core** (plan 06 R19 to R29, plan 14). I1 touches every place the log is written, so it runs in a wave when nobody else changes the log, as the overview says for O3.
- **A view that leaks**: one forgotten field in one event hands the foe's state to the other player. I2's tests check every event type against a list, so a new event fails until someone decides what each side sees of it.
- **Two builds that disagree**: the server and both clients must use the same data and rules. Plan 07 refuses a room whose data versions differ (plan 08 · P10's `GameDataFiles.Version`), and I1's wire format carries the protocol version.

## Decisions for the user

1. **How much of the foe's HP a player sees**: recommended, the bar only, as Platinum's link battles do. The other choice is exact numbers for both sides, as some later games show.
2. **When to do I5**: recommended after plan 07 · O6, since replaying the record already brings a dropped player back.

## Status

- [ ] I1 The log on the wire
- [ ] I2 What each side may see
- [ ] I3 Both sides asked
- [ ] I4 A link battle's setup
- [ ] I5 State written out
