#!/usr/bin/env bash
# The online server's nightly backup (plan 07 · O7; the setup guide's step 10). Install with
#   sudo install -m 755 linkserver-backup.sh /usr/local/bin/linkserver-backup
#   echo '30 3 * * * root /usr/local/bin/linkserver-backup' | sudo tee /etc/cron.d/linkserver-backup
# SQLite's own .backup takes a consistent copy while the server runs (the database is in WAL mode); fourteen days of
# copies are kept. Needs the sqlite3 package (step 6).
set -euo pipefail

database="${LINKSERVER_DATABASE:-/var/lib/linkserver/link.db}"
folder="${LINKSERVER_BACKUPS:-/var/backups/linkserver}"
keep_days="${LINKSERVER_KEEP_DAYS:-14}"

mkdir -p "$folder"
target="$folder/link-$(date +%F).db"
sqlite3 "$database" ".backup '$target'"
# A copy that doesn't pass SQLite's own check is no backup
if [ "$(sqlite3 "$target" 'PRAGMA integrity_check;')" != "ok" ]; then
  echo "linkserver-backup: $target failed its integrity check" >&2
  exit 1
fi
find "$folder" -name 'link-*.db' -mtime +"$keep_days" -delete
