# Plan 07 · Online trading and battles

**Goal**: two people playing the game on different computers can find each other over the internet, trade Pokémon and battle each other, the way Platinum's Union Room and Wi-Fi Club let friends do it. It has to work in every region of the chained game (Kanto first, then Johto, Hoenn, Sinnoh and onward), between players at different points of the story.

It follows the shape of plans 01–06: where we are, design, sessions, decisions to take.

## Where we are

- **Save** (`Core/SaveManager.cs`): one local `savegame.json`. A saved Pokémon has species, nickname, level, gender, nature, shiny flag, HP, status, IVs, EXP and moves. It has **no** unique id, original trainer (name and id), EVs, held item, ability, friendship, met location or Poké Ball, all of which trading needs. The player has a name but no trainer id.
- **Battle** (`Battle/BattleEngine.cs`, ~1000 lines): rules, the enemy AI (`ChooseEnemyAction`), the message queue, the HUD, VFX, animation and audio all live in one class. Input is read inside `Update` (`InputManager`, `Raylib.IsKeyPressed`), the constructor plays music, and every random roll uses an unseeded `new Random()` (here and in `DamageCalculator`, `CatchCalculator`). Only the player side is chosen by a person; the other side is always the AI.
- **Rules without a GPU**: CLAUDE.md already requires battle logic to be callable without raylib, and tests drive battles through `SelectMainMenuOption`, `SelectMove`, `ConfirmMessage`. That is most of what an online battle needs; the rest is listed under "Needs from the battle rework".
- **Networking**: none. No server, accounts or online code exist.
- **In flight elsewhere**: the "Complete the battle system" thread is reworking the battle engine right now, "Move game data to files" is moving species/moves to data files, and "Chain regions Kanto onward" is restructuring the world into chained regions. This plan is designed to land after those and asks them for a few cheap properties now (below).

## Design

### Shape: a small relay server, battles decided on the server

```
 Game (player A) ──WebSocket/TLS──┐                ┌──WebSocket/TLS── Game (player B)
                                  ▼                ▼
                         PokemonPlatinum.Server (ASP.NET Core, .NET 9)
                         ├─ lobby: friend codes, link codes, presence
                         ├─ trade broker: offer → validate → commit → receipt
                         ├─ battle host: runs BattleRules headless, one per match
                         └─ store: SQLite (accounts, friends, trade log, battle records)
```

- **Why a server and not peer to peer**: home routers block direct connections (NAT), so P2P needs a relay anyway; a server can also check that a traded Pokémon is legal and keep a trade log so a crash mid-trade can never duplicate or lose a Pokémon.
- **Why the server runs the battle**: the server is C# like the game, so it can run the exact same battle rules library. Each client only sends "I choose move 2" and receives the turn's events (who moved first, damage, crits, faint) to animate. Neither player can fake a crit or see the other's choice early, and both screens always agree. The alternative, both clients simulating with a shared seed (lockstep), is cheaper to host but breaks the moment the two builds differ by one bug fix and lets a modified client read the opponent's hidden choices; it is kept only as the replay format (seed + choices).
- **Transport**: WebSockets over TLS (`System.Net.WebSockets` on the client, ASP.NET Core on the server), JSON messages with a `type` field and a protocol version. Traffic is tiny (a turn is a few hundred bytes), so JSON is fine and easy to debug.
- **Hosting**: one small Linux VM or container (Fly.io, Hetzner, Azure Container Apps; roughly €5/month) is plenty for friends-scale use. The server address lives in `settings.json` so anyone can run their own server. A `--local` server mode lets two game windows on one machine test everything with no internet.

### Shared code

A new GPU-free class library, **`PokemonPlatinum.Core`**, that both the game and the server reference: models (`Pokemon`, `Move`, `Party`), data (`PokemonDatabase`, `MoveDatabase`, `TypeChart`, once they load from files), the save DTOs, the battle rules, and the network message types. The game keeps rendering, audio, input and UI. The test project references the library too. This split is the largest single piece of work and also helps the rest of the roadmap (fast headless tests, tools).

### Identity and finding each other

- **No passwords at first.** On first going online the game creates a key pair and a random **trainer id** (also shown on the trainer card) and registers the public key with the server; the server issues a **friend code** (`1234-5678-9012`). Requests are signed, so nobody can act as someone else. The private key lives next to the save; losing it means a new friend code, not a lost save.
- **Two ways to meet**, both at a new **Global Link counter** upstairs in every Pokémon Center (Platinum's Wi-Fi Club spot), in every region:
  - **Link code**: both players type the same 8-digit code, as in modern Pokémon games. No friend list needed. Good default.
  - **Friends**: add a friend code once; the list shows who is online and lets you invite them to trade or battle.
- **No public matchmaking or global trade board in the first version.** Random strangers bring moderation, abuse and legal attention (see Risks). Wonder-trade and a GTS-style board are listed as later options.

### Trading

1. Both players enter the trade room (link code or invite). Each picks one Pokémon from party or PC boxes; both see the other's offer with a summary page (species, level, nature, moves, OT, held item).
2. Each presses **Offer**, then **Confirm**. Either can cancel until both confirm.
3. The server **validates** each Pokémon (below), writes a trade record (`pending`), and sends each client the Pokémon it receives with a **trade receipt id**.
4. Each client applies the trade: removes its Pokémon, adds the new one, **saves immediately**, then acknowledges. When both acknowledge, the record becomes `complete`.
5. **Crash safety**: on the next connect, a client with a pending receipt asks the server for its state and finishes or rolls back. Every Pokémon gets a unique id (a GUID), and the client refuses to add an id it already holds, so a replayed receipt can't duplicate anything.

*Ready since 2026-10-02:* `GameEngine.ReceiveTradedPokemon(received, given)` is the call the client makes when it applies a trade. It swaps the Pokémon in, resets friendship, registers the Pokédex and plays the trade evolution (plain, with a held item that is used up, or Karrablast for Shelmet), with the Everstone stopping it and Kadabra ignoring the Everstone as in Platinum. The rule alone is `Evolution.Find(pokemon, EvolutionTrigger.Trade, context)` with `context.TradedFor`, which the server can call too. Saved Pokémon now carry `Friendship`, `Personality` and `Ball`. Still O2's: the id, the original trainer, EVs, met data, and telling the other side that its own Pokémon evolved.

After the trade, Platinum's rules apply: traded Pokémon gain 1.5× EXP; trade evolutions trigger (Kadabra, Machoke, Graveler, Haunter; with a held item: Onix + Metal Coat, Scyther + Metal Coat, Seadra + Dragon Scale, Porygon + Up-Grade, and the Gen 4 ones like Electabuzz + Electirizer), with the Everstone stopping them; traded Pokémon above the level your badges allow may disobey. The receiving player's Pokédex registers the species as seen and caught.

**Across chained regions**: a Kanto player can receive a Sinnoh species from a friend further along. That follows the series (you can always trade for a Pokémon from elsewhere). Two guards: the receiver's game must have the species' data and model (otherwise the trade is refused with "your friend's Pokémon can't come to this version yet"), and obedience follows the badges of the region you are in.

**Validation on the server** ("is this Pokémon possible?"): species exists in the data version; level 1–100 and EXP matches level for its growth rate; IVs 0–31, EVs ≤ 252 each and ≤ 510 total; stats recomputed from base/IV/EV/nature rather than trusted; nature, gender and ability possible for the species; moves exist and are learnable by the species (level-up, TM/HM, egg, tutor, event list); PP within limits; held item exists. It cannot prove a Pokémon was caught fairly (the save is a local file anyone can edit), so the goal is "no impossible Pokémon", not "no cheaters". That is the right level for playing with friends.

### Battles

- **Formats**: single battle first; double battle once the battle rework supports it. Teams of up to six picked from party and boxes, with a team preview (see the other side's six, pick your order).
- **Rule sets**, chosen in the room: **Flat 50** (everyone scaled to level 50, the default, so players at different points of the game can still have a fair match), **Flat 100**, **As is** (real levels). Clauses: species clause, item clause, sleep clause, all on by default for Flat 50.
- **Nothing in a link battle changes the save**: no EXP, no money, items and PP restored afterwards, as in Platinum. The trainer card records wins and losses.
- **Turn flow**: both clients send a choice (move, switch, forfeit); the server waits for both, resolves the turn with a seeded RNG, and sends the event list; each client plays it through the existing message queue and animator. A **60-second turn timer** (a default choice is made when it runs out) keeps a stalled opponent from freezing the match.
- **Disconnects**: a dropped player has 60 seconds to reconnect and resume (the server keeps the battle); after that, they forfeit.
- **Replays**: the server keeps seed + choices + data version; replaying them through the same rules reproduces the battle. Useful for bug reports even if never shown to players.

### Versions and data

- Every message carries the **protocol version**; every room carries the **data version** (a hash of species, moves, items and type chart). Two players can trade only if the receiver has every species and move involved; they can battle only on identical data versions, so both sides compute the same damage. The server tells an out-of-date client to update.
- The save format needs a `SaveVersion` and a migration step (the new fields below default sensibly for existing saves).

### Save additions (needed before anything goes online)

Per Pokémon: `Id` (GUID), `OriginalTrainerName`, `OriginalTrainerId`, `EVs`, `HeldItem`, `Ability`, `Friendship`, `MetLocation`, `MetLevel`, `Ball`, `IsTraded` (derived from OT). Per player: `TrainerId`, `SecretId`, online key reference, link-battle record. IVs move to the real 0–31 range if they are still 0–15. Several of these the battle rework and the story plan need anyway (held items, abilities, friendship).

*Ready from plan 06 · R1 (2026-10-04), for asks 2 and 3 below and for the rule sets:* `Battle/Sim/BattleRandom.cs` is the one seedable source (Platinum's own generator; its whole state is one number, so a resynchronised battle carries on), handed to a battle through `BattleSetup.Random`; presentation draws its own numbers already. The game's own battles still roll with `System.Random` until R2. A game is played by Platinum's rules or the modern ones (`Data/Ruleset.cs`, kept in the save), and a battle is given its rules (`BattleSetup.Rules`): two players whose saves differ need one set for the room, so add it to the room's rule sets in O6 (Flat 50 under Platinum's rules is the natural default), and count it into the data version.

### Needs from the battle rework (ask now, cheap if done during the rework)

1. **Rules separate from presentation**: a `BattleRules` (or similar) class with no raylib, `InputManager` or `AudioManager` calls, that takes both sides' choices and produces a list of events. `BattleEngine`'s HUD, VFX, animator and music stay on the client and play those events.
2. **Both sides are chosen the same way**: an interface such as `IBattleController` with implementations for local input, the AI, and (later) a remote player. Today the AI is called inside `ExecuteTurn`.
3. **One injectable random source** for the whole battle (damage, crits, accuracy, catch, speed ties, secondary effects), seedable. Presentation-only randomness (VFX particles) stays separate.
4. **Battle state that can be serialised** (both parties, field effects, turn counter), so a reconnecting player can be resynced.

If the rework already gives (1)–(3), session O3 shrinks to wiring.

### Risks

- **Takedowns**: an online service for a Pokémon fan game is far more visible than a downloadable one. Keep it friends-only (link codes, friend codes, no public listing or board), self-hostable, and with no Nintendo names or art on the server side.
- **Cheating**: local saves can be edited. Validation stops impossible Pokémon; nothing stops a hand-edited "perfect" one. Fine between friends; ranked play would need server-held saves, which is out of scope.
- **Running cost and upkeep**: someone has to host and patch the server. The self-host option and SQLite keep this small.
- **Scope**: it touches save, battle, UI and a new server. It should come after the battle rework and the data-files move, not alongside them.

## Sessions

| # | Session | Delivers | Needs |
|---|---|---|---|
| O1 | Core library split | `PokemonPlatinum.Core` project (models, data, save DTOs, battle rules) referenced by game and tests; no behaviour change | Battle rework, data-files move landed |
| O2 | Save for trading | Pokémon id, OT, trainer id, EVs, held item, ability, friendship, met data, ball; `SaveVersion` and migration of old saves; tests | O1 |
| O3 | Network-ready battles | Rules/events split, `IBattleController`, seeded RNG, event replay test (same seed + choices → same events) | Battle rework |
| O4 | Server skeleton | ASP.NET Core server, WebSocket protocol with versioning, key-pair registration, friend codes, link-code rooms, presence, SQLite; `--local` mode; protocol tests with two in-process clients | O1 |
| O5 | Trading | Trade room UI at the Global Link counter (ModernUi kit), offer/confirm flow, server validation, receipts and crash recovery, trade evolutions, 1.5× EXP, obedience; tests for duplication and rollback | O2, O4 |
| O6 | Battles | Team pick and preview, rule sets and clauses, server-hosted battle, turn timer, reconnect and forfeit, win/loss record; harness shots of the battle room | O3, O4 |
| O7 | Friends and polish | Friend list with online status and invites, connection status in the menu, error messages, settings for server address, hosting guide in `docs/` | O5, O6 |
| O8 | (Optional) More ways to trade | Wonder trade, a GTS-style board, double battles online, spectating | O7, decisions below |

Every session runs the build and tests; O4 onward also adds a test that starts the server in-process and drives two fake clients through a full trade or battle.

## Decisions for the user

1. **Who hosts the server?** Recommended: you run one small server (about €5/month), with the address in settings so friends can point at another one.
2. **Friends only, or public?** Recommended: friends only (link codes and friend codes) for the first version; public matchmaking and a trade board stay optional.
3. **Accounts**: recommended: no passwords, a key pair and friend code created automatically. A login (Discord, email) can come later if saves should move between computers.
4. **When**: recommended after the battle rework and the move to data files land; O3's asks can go to the battle rework thread now so that work isn't redone.

## Status

- [ ] O1 Core library split
- [ ] O2 Save for trading
- [ ] O3 Network-ready battles
- [ ] O4 Server skeleton
- [ ] O5 Trading
- [ ] O6 Battles
- [ ] O7 Friends and polish
- [ ] O8 More ways to trade (optional)
