#!/usr/bin/env bash
# The first commit that moved a picture (plan 16 · T16).
#
#   tools/dev/bisect.sh <mode> <shot> <good> <bad>
#
# Shoots the good commit's picture once, then runs `git bisect` in a worktree of its own (the working tree is never
# touched). Each step builds that commit's harness (skipped where it doesn't build, or where the mode throws or
# saves no such shot), runs the whole mode with SHOTS_FILTER=<shot>, so the state carried from earlier shots is the
# same, and compares the one picture with the good one through the working tree's `diff`, built once, so an old
# commit needs only to draw.
set -euo pipefail
. "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

# One step of `git bisect run`: 0 the picture is the good one's, 1 it moved, 125 this commit can't say
if [ "${1:-}" = --step ]; then
  mode="$2" shot="$3" good="$4" tree="$5"
  build_harness "$tree" || exit 125
  out="$scratch/bisect/step"
  rm -rf "$out" "$scratch/bisect/compare"
  SHOTS_FILTER="$shot" run_harness "$tree" "$out" "$mode" >&2 || exit 125
  [ -f "$out/$shot.png" ] || exit 125
  mkdir -p "$scratch/bisect/compare"
  cp "$out/$shot.png" "$scratch/bisect/compare/"
  status=0
  working_harness "$scratch/bisect/compare" diff "$good" >&2 || status=$?
  [ $status -le 1 ] && exit $status
  exit 125
fi

[ $# -eq 4 ] || { echo "usage: tools/dev/bisect.sh <mode> <shot> <good> <bad>" >&2; exit 2; }
mode="$1" shot="$2"
good="$(git -C "$root" rev-parse --verify "$3^{commit}")"
bad="$(git -C "$root" rev-parse --verify "$4^{commit}")"

echo "bisect: building the working tree's harness, which compares" >&2
build_harness "$root"

echo "bisect: shooting $shot in ${good:0:10}" >&2
good_tree="$(commit_tree "$good")"
build_harness "$good_tree"
good_shots="$scratch/bisect/good-${good:0:10}-$mode"
rm -rf "$good_shots" "$scratch/bisect/good"
SHOTS_FILTER="$shot" run_harness "$good_tree" "$good_shots" "$mode"
[ -f "$good_shots/$shot.png" ] || { echo "bisect: $mode saved no shot called $shot in ${good:0:10}" >&2; exit 2; }
mkdir -p "$scratch/bisect/good"
cp "$good_shots/$shot.png" "$scratch/bisect/good/"

tree="$scratch/bisect/tree"
git -C "$root" worktree prune
[ -e "$tree/.git" ] || git -C "$root" worktree add --detach "$tree" "$bad" >&2
git -C "$tree" bisect reset >/dev/null 2>&1 || true
git -C "$tree" bisect start "$bad" "$good" >&2
status=0
git -C "$tree" bisect run bash "$root/tools/dev/bisect.sh" --step "$mode" "$shot" "$scratch/bisect/good" "$tree" || status=$?
git -C "$tree" bisect log | tail -n 3
git -C "$tree" bisect reset >&2
exit $status
