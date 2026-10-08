#!/usr/bin/env bash
# Shoots a mode of the screenshot harness (plan 16 · T16).
#
#   tools/dev/shots.sh <out dir> <mode> [filter] [more...]
#
# The harness is built in Release and run under a virtual X server with Mesa's software renderer on Linux (a cloud
# session, CI), or as it is on Windows. The filter becomes SHOTS_FILTER: llvmpipe fills the 4K screen slowly, so
# shoot what changed, not a whole mode ("" for no filter). Anything after it goes to the harness as it is
# (`shots.sh out area "" twinleaf_town`). POKEMON_CACHE is a folder under ~/.cache unless it is set already, so
# every tree meshes a model once.
set -euo pipefail
. "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

if [ $# -lt 2 ]; then
  echo "usage: tools/dev/shots.sh <out dir> <mode> [filter] [more...]" >&2
  exit 2
fi
out="$1" mode="$2"
shift 2
if [ $# -gt 0 ]; then
  [ -n "$1" ] && export SHOTS_FILTER="$1"
  shift
fi

build_harness "$root"
run_harness "$root" "$out" "$mode" "$@"
