# Plan 07 · Online trading and battles

**Goal**: two people playing the game on different computers can find each other over the internet, trade Pokémon and battle each other, the way Platinum's Union Room and Wi-Fi Club let friends do it. It has to work in every region of the chained game (Kanto first, then Johto, Hoenn, Sinnoh and onward), between players at different points of the story.

It follows the shape of plans 01–06: where we are, design, sessions, decisions. Written 2026-10-01; brought up to date and given the user's decisions on 2026-10-12.

## Where we are

Rewritten 2026-10-12 (the first version, of 2026-10-01, described the game before plan 06).

- **Save** (`Core/SaveManager.cs`): a saved Pokémon already carries what a trade shows and checks: its original trainer (`Pokemon.OriginalTrainer`, a `TrainerMark` of name, number and look), EVs, held item, ability, friendship, personality, ball, met place and met level. The player has a trainer number (`PlayerIdentity`, the card's ID with a hidden half, plan 06 · R15). Two things are still missing, and both are O2's: a Pokémon's own id (nothing tells two identical Pokémon apart, which a trade receipt needs) and a version for the save's Pokémon (the save has a world version and a story version, not one for its Pokémon). The overview names both as one seam O2 owns (`Pokemon.Id` and the save's version, lane 6).
- **Battles** (`Battle/Sim/`): the rules are `BattleCore`, with no raylib, input or audio. A battle asks (`Request`) and is answered (`Submit` of `BattleChoice`s), writes a log of events (`BattleLog.cs`) that `BattleEngine` plays on screen, rolls every die with one seedable `BattleRandom`, and records itself as a seed and a list of answers (`BattleRecord`) that replays to the same battle. That is what plan 06 · R1 and R2 gave to this plan's first three asks. What a link battle still needs from the battles is plan 28 (below), which replaces O3.
- **Trades**: `GameEngine.ReceiveTradedPokemon` and `Evolution.Find(..., EvolutionTrigger.Trade, ...)` apply a received Pokémon and its trade evolution (since 2026-10-02, notes under Trading).
- **Places**: the upper floors of every Pokémon Center and Jubilife's Global Terminal are built to the original's plans (plan 01 · M11d), with nobody at the counters that were online in Platinum.
- **Shared with other plans**: plan 13 · V12 (local versus) asks both sides of a battle from the menus and fights on copies; plan 14 · B11 writes `LevelScaling` (Flat 50 and Flat 100) and a shared legality check (`Models/Legality.cs`), which O5 and O6 use rather than writing their own; plan 08 · P10 writes the data version (`GameDataFiles.Version`). None of them is done yet; whichever comes first writes the shared piece.
- **Networking**: none. No server or online code exists.

## Design

### Shape: a small relay server, battles decided on the server

```
 Game (player A) ──WebSocket/TLS──┐                ┌──WebSocket/TLS── Game (player B)
                                  ▼                ▼
                         PokemonPlatinum.Server (ASP.NET Core, .NET 10)
                         ├─ lobby: friend codes by IP, link-code rooms
                         ├─ trade broker: offer → validate → commit → receipt
                         ├─ battle host: runs BattleRules headless, one per match
                         └─ store: SQLite (friend codes, hashed IPs, trade log, battle records)
```

- **Why a server and not peer to peer**: home routers block direct connections (NAT), so P2P needs a relay anyway; a server can also check that a traded Pokémon is legal and keep a trade log so a crash mid-trade can never duplicate or lose a Pokémon.
- **Why the server runs the battle**: the server is C# like the game, so it can run the exact same battle rules library. Each client only sends "I choose move 2" and receives the turn's events (who moved first, damage, crits, faint) to animate. Neither player can fake a crit or see the other's choice early, and both screens always agree. The alternative, both clients simulating with a shared seed (lockstep), is cheaper to host but breaks the moment the two builds differ by one bug fix and lets a modified client read the opponent's hidden choices; it is kept only as the replay format (seed + choices).
- **Transport**: WebSockets over TLS (`System.Net.WebSockets` on the client, ASP.NET Core on the server), JSON messages with a `type` field and a protocol version. Traffic is tiny (a turn is a few hundred bytes), so JSON is fine and easy to debug.
- **Hosting** (decided 2026-10-12): one small Linux server of about $5 a month that the user runs, set up as [`docs/online/server-setup.pdf`](../online/server-setup.pdf) describes: Ubuntu, the .NET server as a systemd service, Caddy in front of it for HTTPS with a free DuckDNS name, a firewall, automatic security updates and a daily backup. The server address lives in `settings.json`, so anyone can run their own server. A `--local` server mode lets two game windows on one machine test everything with no internet.

### Shared code

A new GPU-free class library, **`PokemonPlatinum.Core`**, that both the game and the server reference: models (`Pokemon`, `Move`, `Party`), data (`PokemonDatabase`, `MoveDatabase`, `TypeChart`, once they load from files), the save DTOs, the battle rules, and the network message types. The game keeps rendering, audio, input and UI. The test project references the library too. This split is the largest single piece of work and also helps the rest of the roadmap (fast headless tests, tools).

### Identity and finding each other

Decided by the user on 2026-10-12: no accounts and no passwords, a friend code made automatically the first time an IP address reaches the server, people meet by link code alone, and a link code is bound to the IP addresses of the two players in it.

- **The friend code**: the first time a game connects from an IP address, the server creates a **friend code** (`1234-5678-9012`) and remembers the pair. The code is shown in the Global Link menu and to the other player in a room, so each knows who they are trading or battling with. Nothing has to be typed or remembered.
- **An install token beside the IP**: the game also sends a random token it creates once and keeps next to `settings.json` (never shown, never typed), and the server recognises a player by IP and token together. Without it, two people playing from one house (one IP behind one router) would share one friend code, and a home connection whose IP changes would get a new friend code each time. With it, a new token from a known IP is a second player, and a known token from a new IP keeps its friend code, which then moves to the new IP. It is still no account and no password.
- **The link code, bound to IPs**: the only way to meet. One player chooses "Make a code"; the server hands out an 8-digit code tied to that player's IP and friend code. The other player types it; the server binds the room to the second IP and friend code too. From then on the room accepts messages only from those two IPs with those two tokens; a third person who types the code is told the room is full; a dropped player may come back within 60 seconds only from the same IP or with the same token. A code works once and expires after 10 minutes if nobody joins.
- **Guessing is limited**: ten wrong codes from one IP within ten minutes stop that IP joining rooms for ten minutes.
- **IPv4 only**: the server's name has only an IPv4 address (an A record, no AAAA). Home IPv6 addresses change on their own every day (privacy addresses), which would make an IP binding useless. A player whose connection has no IPv4 at all (rare, a few mobile networks) can't connect in the first version.
- **The real IP behind the proxy**: the server sits behind Caddy (the setup guide below), so it reads the player's address from `X-Forwarded-For` through ASP.NET Core's forwarded-headers middleware, trusting only the proxy on the same machine. Without that, every player would look like `127.0.0.1`.
- **Privacy**: an IP address is personal data under the GDPR. The server stores a keyed hash of each address (HMAC-SHA256 with a secret from its configuration), never the address itself, which is enough to recognise a returning player and to check a room's binding. It forgets a player not seen for twelve months, and it stores nothing else about anyone.
- **Where**: a **Global Link counter** on the upper floor of every Pokémon Center, in every region, and the Global Terminal in Jubilife.
- **Not in the first version**: a friend list with invites (optional, O8), public matchmaking, a global trade board. Strangers bring moderation, abuse and legal attention (see Risks).

### Trading

1. Both players enter the trade room by link code. Each picks one Pokémon from party or PC boxes; both see the other's offer with a summary page (species, level, nature, moves, OT, held item).
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

Already in the save (Where we are): the original trainer, EVs, held item, ability, friendship, personality, ball, met place and level, and the player's trainer number. Still O2's: `Pokemon.Id` (a GUID, given when a Pokémon is made or first loaded), the version of the save's Pokémon with its migration, and the link-battle record (wins and losses by friend code).

*Ready from plan 06 · R1 (2026-10-04), for the battles (plan 28) and for the rule sets:* `Battle/Sim/BattleRandom.cs` is the one seedable source (Platinum's own generator; its whole state is one number, so a resynchronised battle carries on), handed to a battle through `BattleSetup.Random`; presentation draws its own numbers already. The game's own battles roll with it since R2. A game is played by Platinum's rules or the modern ones (`Data/Ruleset.cs`, kept in the save), and a battle is given its rules (`BattleSetup.Rules`): two players whose saves differ need one set for the room, so add it to the room's rule sets in O6 (Flat 50 under Platinum's rules is the natural default), and count it into the data version.

### What the battles still owe a link battle: plan 28

This plan first listed four asks of the battle rework. Plan 06 · R1 and R2 gave the first three: the rules apart from the screen (`BattleCore` and its log), one way to choose for a side (`IBattleController`, with both sides asked from outside when it is null) and one seedable source of chance (`BattleRandom`), plus the replay (`BattleRecord`). The fourth was half given. What is left (a log that names places instead of objects, a view of the log each side may see, both sides asked by the menus, a battle's state written out and read back, a link battle's own setup with its rules, clauses and timer) is collected in [plan 28 · Battles for two players](28-battles-for-two-players.md), which replaces session O3. O6 starts once plan 28's sessions I1 to I4 are done.

### Risks

- **Takedowns**: an online service for a Pokémon fan game is far more visible than a downloadable one. Keep it friends-only (link codes, no public listing or board), self-hostable, and with no Nintendo names or art on the server side or in the server's domain name.
- **Identity by IP**: an IP address says where a connection comes from, not who is behind it. The install token covers a shared house and a changing home IP; it does not cover someone who copies another player's `settings.json`, which between friends is not worth defending. Mobile connections behind carrier-grade NAT share one IP among many strangers, which the token also covers. Nothing about a player is secret enough to need more.
- **Cheating**: local saves can be edited. Validation stops impossible Pokémon; nothing stops a hand-edited "perfect" one. Fine between friends; ranked play would need server-held saves, which is out of scope.
- **Running cost and upkeep**: the server costs about €5 a month and needs its updates applied. The setup guide turns on automatic security updates and a daily backup of the database; the self-host option and SQLite keep it small.
- **Scope**: it touches save, battle, UI and a new server. It comes after plan 28 and the shared pieces of plans 08, 13 and 14, not alongside them.

## Sessions

| # | Session | Delivers | Needs |
|---|---|---|---|
| O1 | Core library split | `PokemonPlatinum.Core` project (models, data, save DTOs, battle rules) referenced by game and tests; no behaviour change. The same library plan 27 (Unity) describes, so it is written once, for both | Plan 28 · I1 |
| O2 | Save for trading | `Pokemon.Id` and the save's version for its Pokémon, with the migration of old saves; the link-battle record; tests | — |
| O3 | Network-ready battles | Moved to plan 28 (I1 to I5) | — |
| O4 | Server skeleton | ASP.NET Core server, WebSocket protocol with versioning, friend codes made on an IP's first visit with the install token, link-code rooms bound to their two IPs, guess limits, forwarded headers behind Caddy, hashed IPs, SQLite; `--local` mode; protocol tests with two in-process clients | O1 |
| O5 | Trading | Trade room UI at the Global Link counter (ModernUi kit), offer/confirm flow, server validation (plan 14 · B11's legality check), receipts and crash recovery, trade evolutions, 1.5× EXP, obedience; tests for duplication and rollback | O2, O4 |
| O6 | Battles | Team pick and preview, rule sets and clauses, server-hosted battle, turn timer, reconnect and forfeit, win/loss record; harness shots of the battle room | Plan 28 · I1–I4, O4 |
| O7 | Going live | Connection status, error messages, the server's address in settings, the server's deployment files (Caddy config, systemd unit, backup script) as the setup guide describes, a first real deployment on the user's server | O5, O6 |
| O8 | (Optional) More | A friend list with invites, wonder trade, a GTS-style board, double battles online, spectating | O7 |

Every session runs the build and tests; O4 onward also adds a test that starts the server in-process and drives two fake clients through a full trade or battle.

## Decisions

Taken by the user on 2026-10-12:

1. **Hosting**: the user runs one small server of about $5 a month. The setup guide, with the provider recommended and every step from an empty account to a running server, is [`docs/online/server-setup.pdf`](../online/server-setup.pdf) (source: [`server-setup.html`](../online/server-setup.html)). Recommended there: Hetzner Cloud's smallest shared server; Vultr's or Akamai's $5 server if the budget must stay at exactly $5.
2. **Who can connect**: people meet by link code; nothing public.
3. **Accounts**: none, and no passwords. The friend code is made automatically the first time an IP reaches the server, and a link code is bound to the IPs of its two players. The install token beside the IP (Identity, above) was added on 2026-10-12 so that two players behind one router, or one whose IP changes, keep working under that decision.
4. **The battle rework**: a plan of its own, plan 28, written 2026-10-12.

## Status

- [ ] O1 Core library split
- [ ] O2 Save for trading
- [x] O3 Network-ready battles (moved to plan 28 on 2026-10-12)
- [ ] O4 Server skeleton
- [ ] O5 Trading
- [ ] O6 Battles
- [ ] O7 Going live
- [ ] O8 More (optional)
