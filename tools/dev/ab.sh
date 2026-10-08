#!/usr/bin/env bash
# Whether a change made frames faster or slower (plan 16 · T16): `profile` in a commit's tree, the working tree
# and the commit's again, back to back, then the harness's `ab` over the three.
#
#   tools/dev/ab.sh <commit> [words]
#
# Words go on as SHOTS_ONLY (`ab.sh HEAD~1 battle,route`). Both trees are built first, so nothing compiles while
# frames are timed; run it with nothing else on the machine, and SHOTS_WINDOW=3840x2160 for the game as it is
# full screen. Milliseconds mean something only on a graphics card: under Mesa's software renderer (a cloud
# session, CI) it refuses. A commit from before profile.json is read from what its `profile` printed.
set -euo pipefail
. "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

[ $# -ge 1 ] || { echo "usage: tools/dev/ab.sh <commit> [words]" >&2; exit 2; }
if [ "${LIBGL_ALWAYS_SOFTWARE:-}" = 1 ] || [ "${GALLIUM_DRIVER:-}" = llvmpipe ] || { ! on_windows && [ -z "${DISPLAY:-}${WAYLAND_DISPLAY:-}" ]; }; then
  echo "ab: no graphics card to time on here (software rendering or no display); timings are read on the Windows machine" >&2
  exit 2
fi
[ $# -ge 2 ] && export SHOTS_ONLY="$2"

hash="$(git -C "$root" rev-parse --verify "$1^{commit}")"
tree="$(commit_tree "$hash")"
echo "ab: building the harness of ${hash:0:10} and of the working tree" >&2
build_harness "$tree"
build_harness "$root"

runs="$scratch/ab/$(date +%Y%m%d-%H%M%S)"
mkdir -p "$runs"
profile() {
  local where="$1" out="$runs/$2"
  echo "ab: profile in $2" >&2
  SHOTS_NATIVE=1 run_harness "$where" "$out" profile | tee "$out.txt"
  # What profile printed, for a commit whose profile wrote no profile.json
  [ -f "$out/profile.json" ] || cp "$out.txt" "$out/profile.txt"
}
profile "$tree" before
profile "$root" after
profile "$tree" again
working_harness "$runs" ab "$runs/before" "$runs/after" "$runs/again"
