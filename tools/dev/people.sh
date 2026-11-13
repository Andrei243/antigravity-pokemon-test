#!/usr/bin/env bash
# Builds every build's character file with Blender (style guide, "Characters from files"; plan 47: the files are
# built, never committed):
#   tools/dev/people.sh [--strict] <out dir> [build ...]   -> <out dir>/overrides/characters/<Build>.glb
#   tools/dev/people.sh [--strict] --here [build ...]      -> the repository's assets/characters (git-ignored)
# The harness run with the same <out dir> draws people from them; the game started from the repository reads
# assets/characters. --strict ends with 1 unless every file asked for was made (the release workflow's way).
# Blender is found by BLENDER, the PATH or Program Files.
set -euo pipefail
here="$(cd "$(dirname "$0")/../.." && pwd)"
strict=0
if [ "${1:-}" = "--strict" ]; then strict=1; shift; fi
out="${1:?an out dir, or --here}"; shift || true
if [ "$out" = "--here" ]; then dest="$here/assets/characters"; else dest="$out/overrides/characters"; fi
blender="${BLENDER:-}"
if [ -z "$blender" ]; then
  blender="$(command -v blender || true)"
  [ -n "$blender" ] || blender="$(ls -d "/c/Program Files/Blender Foundation"/Blender*/blender.exe 2>/dev/null | sort | tail -1)"
fi
[ -n "$blender" ] || { echo "Blender not found: set BLENDER" >&2; exit 1; }
mkdir -p "$dest"
chibi="Kid Adult Little Elder"
grown="Teen Teen-curved Grown Grown-curved GrownBroad GrownBroad-curved Child GrownElder GrownElder-curved"
builds="${*:-$chibi $grown}"
failed=""
for b in $builds; do
  case " $chibi " in *" $b "*) script=people_chibi.py ;; *) script=people_grown.py ;; esac
  rm -f "$dest/$b.glb"
  "$blender" --background --factory-startup --python "$here/tools/BlenderAnim/blender/sculpts/$script" -- "$b" "$dest/$b.glb" 2>&1 \
    | grep -E "PEOPLE|Error|Traceback" || true
  [ -s "$dest/$b.glb" ] || failed="$failed $b"
done
ls -la "$dest"
if [ -n "$failed" ]; then
  echo "not made:$failed" >&2
  [ "$strict" = 0 ] || exit 1
fi
