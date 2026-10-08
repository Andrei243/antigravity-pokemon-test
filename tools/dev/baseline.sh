#!/usr/bin/env bash
# What a change did to the pictures (plan 16 · T16): a commit's shots beside the working tree's, mode by mode.
#
#   tools/dev/baseline.sh <commit> [--only <path>...] <mode>...
#
# The commit gets a detached worktree in the scratch folder, kept and reused by its hash, with its own harness;
# both trees share POKEMON_CACHE, so no model is meshed twice. Each mode runs alone, into
# <scratch>/shots/<commit>/<mode> and <scratch>/shots/work/<mode>, and each pair is `diff`ed: the boards are in the
# working tree's folder. A commit's shots are kept and reused (they never change); SHOTS_FILTER is passed on and
# keeps shots of its own.
#
# --only makes the "after" the commit's tree with only those paths copied over it from the working tree: how a
# change of the renderer is told from other sessions' work in the same tree (CLAUDE.md, "Two runs draw the same
# pictures").
#
# Exits 1 when any mode's shots differ, 0 when every one drew the same.
set -euo pipefail
. "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

usage() { echo "usage: tools/dev/baseline.sh <commit> [--only <path>...] <mode>..." >&2; exit 2; }
[ $# -ge 2 ] || usage
commit="$1"; shift
only=() modes=()
while [ $# -gt 0 ]; do
  if [ "$1" = --only ]; then
    shift
    while [ $# -gt 0 ] && [ "$1" != -- ]; do
      # What is a path of the working tree is one; the first word that isn't begins the modes
      if [ -e "$root/$1" ]; then only+=("$1"); shift; else break; fi
    done
    [ "${1:-}" = -- ] && shift
  else
    modes+=("$1"); shift
  fi
done
[ ${#modes[@]} -gt 0 ] || usage

hash="$(git -C "$root" rev-parse --verify "$commit^{commit}")"
before_tree="$(commit_tree "$hash")"
suffix="${SHOTS_FILTER:+~$SHOTS_FILTER}"
suffix="${suffix//\//_}"

# The "after": the working tree, or the commit's own tree with only the given paths of the working tree over it
after_tree="$root"
if [ ${#only[@]} -gt 0 ]; then
  after_tree="$scratch/only/$hash"
  if [ ! -e "$after_tree/.git" ]; then
    git -C "$root" worktree prune
    git -C "$root" worktree add --detach "$after_tree" "$hash" >&2
  fi
  git -C "$after_tree" checkout --detach --force "$hash" >&2
  git -C "$after_tree" clean -fdq
  for path in "${only[@]}"; do
    mkdir -p "$after_tree/$(dirname "$path")"
    cp -r "$root/$path" "$after_tree/$(dirname "$path")/"
  done
  echo "baseline: the after is ${hash:0:10} with ${only[*]} over it" >&2
fi

echo "baseline: building the harness of ${hash:0:10} and of the after" >&2
build_harness "$before_tree"
build_harness "$after_tree"
[ "$after_tree" = "$root" ] || build_harness "$root"

moved=0
for mode in "${modes[@]}"; do
  before="$scratch/shots/$hash/$mode$suffix"
  after="$scratch/shots/work/$mode$suffix"
  if [ -f "$before/.done" ]; then
    echo "baseline: $mode of ${hash:0:10} was shot before" >&2
  else
    rm -rf "$before"
    echo "baseline: shooting $mode in ${hash:0:10}" >&2
    run_harness "$before_tree" "$before" "$mode"
    touch "$before/.done"
  fi
  rm -rf "$after"
  echo "baseline: shooting $mode in the after" >&2
  run_harness "$after_tree" "$after" "$mode"
  echo "--- $mode: $after against $before"
  status=0
  working_harness "$after" diff "$before" || status=$?
  [ $status -le 1 ] || exit $status
  [ $status -eq 0 ] || moved=1
done
exit $moved
