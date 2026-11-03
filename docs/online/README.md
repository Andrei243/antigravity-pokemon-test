# The online server

The server for trading and battling over the internet (plan 07), as [`server-setup.html`](server-setup.html) (and its
PDF) sets it up on one small Linux server of about $5 a month. That guide is the whole of it from an empty account;
this page is what its steps 7 to 10 need from the repository, and how to run the server on one machine for testing.

Nothing here has been deployed yet: plan 07 · O7's first real deployment is the user's to do.

## What it is

`server/PokemonPlatinum.Server`, an ASP.NET Core program (.NET 10) whose executable is `PokemonPlatinumServer`:

- `GET /health` answers `ok <protocol> <data version>`.
- The WebSocket at `/link` speaks the protocol of `PokemonPlatinumEngine/Online/Protocol.cs`: JSON messages with
  a `type`, inside an envelope with the protocol's version (`{"v":1,"msg":{...}}`). A game of another version is
  told to update.
- **Friend codes** (`1234-5678-9012`) are made the first time a game connects, kept by its install token (made once
  by the game and kept beside `settings.json`) and, for a game with none, by its address. No account and no password.
- **Link codes**: eight digits, made by one player, typed by the other, working once and for ten minutes, the room
  bound to its two players (a dropped player comes back within 60 seconds only with the same friend code and the
  same token or address). Ten wrong codes from one address in ten minutes stop it joining for ten minutes.
- **Trades** are checked (`Online/PokemonCheck.cs`: species, form, level and EXP, IVs, effort, gender, ability,
  moves by `Models/Legality.cs` and both rules' level-up lists, PP, item, friendship, HP), refused from a game whose
  data isn't the server's (`GameDataFiles.Version`), and refused for a Pokémon the log gives to someone else or that
  is in a trade not yet finished. Once both confirm, the trade is written to the database **before** either receipt
  goes out; each game applies its receipt, saves, and acknowledges; a receipt not yet acknowledged is handed back at
  the next connection, and a game never adds a Pokémon whose id it holds, so a crash on either end at any step loses
  and duplicates nothing.
- **Battles** run plan 28's `LinkServer` in the room (the server is the only one who rolls), with a 60-second turn
  timer (the rules' default choice when it runs out, the match given up at the third in a row), 60 seconds to come
  back after a drop, and the result counted against the two friend codes and sent to each game for its
  `LinkBattleRecord`. The battle's record (seed and answers) is kept for bug reports.
- **The database** is SQLite (`LinkServer:Database`): players by the keyed hashes of their tokens and addresses
  (HMAC-SHA256 with `LinkServer:IpHashSecret`; an address is never stored), the trade log, who each traded Pokémon
  belongs to now, and the battles. A player not seen for a year is forgotten.

## On one machine (`--local`)

```bash
dotnet run --project server/PokemonPlatinum.Server -- --local                    # ws://127.0.0.1:5080/link, link.db in the folder it runs in
dotnet run --project server/PokemonPlatinum.Server -- --local --port 5090 --database scratch/link.db
```

Local mode listens on localhost alone, believes no proxy, and makes an address-hashing secret for the run if none is
set (so friend codes made without a token change between runs; with a token they don't). Two game windows on one
machine are two players, each with its own install token. The game's screens for it aren't built yet (plan 07 · O5,
O6 and O7 list what is left); the tests drive it through `Online/OnlineClient.cs`.

## On the server (the setup guide's steps 7 to 10)

Step 7, the program. On your computer, in the repository:

```bash
dotnet publish server/PokemonPlatinum.Server -c Release -r linux-x64 --self-contained -o publish/server
# a CAX11 (ARM) server: -r linux-arm64
scp -r publish/server admin@203.0.113.25:/tmp/linkserver
```

On the server, as the guide says (rsync into `/opt/linkserver`, `chmod +x /opt/linkserver/PokemonPlatinumServer`,
the secret into `/etc/linkserver.env`), then the service from this folder rather than typed:

```bash
sudo cp linkserver.service /etc/systemd/system/linkserver.service
sudo systemctl daemon-reload
sudo systemctl enable --now linkserver
```

[`linkserver.service`](linkserver.service) is the guide's unit with three more hardening lines. The settings are the
guide's: `LinkServer__IpHashSecret` (in `/etc/linkserver.env`; the program refuses to start without it outside
`--local`), `LinkServer__Database`, `LinkServer__TrustedProxy` (`127.0.0.1`, Caddy) and `ASPNETCORE_URLS`.

Step 5's Caddyfile is [`Caddyfile`](Caddyfile): put your own name in it.

Step 8, the check: `curl https://<your name>/health` says `ok 1 <data version>`, and `journalctl -u linkserver -f`
shows each player's first connection as `Player <code> connected from address <hash>`. A warning that a player came
from a loopback address means `LinkServer__TrustedProxy` isn't Caddy's: every player would share one friend code.

Step 9, the game: `"OnlineServer": "wss://<your name>/link"` in `settings.json` (plan 07 · O7 adds the row to the
options; until then nothing in the game reads it).

Step 10, the backup: [`linkserver-backup.sh`](linkserver-backup.sh) is the guide's cron line as a script that also
checks each copy:

```bash
sudo install -m 755 linkserver-backup.sh /usr/local/bin/linkserver-backup
echo '30 3 * * * root /usr/local/bin/linkserver-backup' | sudo tee /etc/cron.d/linkserver-backup
```

## Updating

Publish, copy and rsync as in step 7, then `sudo systemctl restart linkserver`. The server tells every game it is
closing and closes each connection. A trade under way finishes by its receipt at the next connection; a battle under
way is lost (plan 28 · I5's snapshot could keep it, which nothing asks of it yet), so update when nobody is playing.
The database's shape has a version (`PRAGMA user_version`) that a later server brings up to date as it starts.
