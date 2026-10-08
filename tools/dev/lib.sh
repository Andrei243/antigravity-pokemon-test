# What tools/dev's scripts share (plan 16 · T16): sourced, never run.
#
#   root         the repository's working tree
#   scratch      where commits' trees and their shots are kept between runs ($DEV_SCRATCH, or under the cache)
#   POKEMON_CACHE  the meshes and sprites every tree shares (exported)

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
export POKEMON_CACHE="${POKEMON_CACHE:-$HOME/.cache/pokemon-platinum}"
scratch="${DEV_SCRATCH:-$POKEMON_CACHE/dev}"
mkdir -p "$POKEMON_CACHE" "$scratch"

on_windows() { case "$(uname -s)" in MINGW*|MSYS*|CYGWIN*) return 0 ;; *) return 1 ;; esac; }

# The harness's build output in a tree
harness_bin() { echo "$1/tools/ShotHarness/bin/Release/net10.0"; }

# Builds a tree's harness in Release; fails as dotnet does
build_harness() { dotnet build "$1/tools/ShotHarness" -c Release -v q -nologo >&2; }

# A tree from before T16 reads no POKEMON_CACHE: its harness keeps its cache beside itself, so that folder is
# filled from the shared one before a run and what it made is copied back after, never over anything
cache_in() {
  local tree="$1" kind
  [ -f "$tree/PokemonPlatinumEngine/Graphics/CacheFolders.cs" ] && return 0
  for kind in models sprites; do
    mkdir -p "$(harness_bin "$tree")/cache/$kind" "$POKEMON_CACHE/$kind"
    cp -rn "$POKEMON_CACHE/$kind/." "$(harness_bin "$tree")/cache/$kind/" 2>/dev/null || true
  done
}
cache_out() {
  local tree="$1" kind
  [ -f "$tree/PokemonPlatinumEngine/Graphics/CacheFolders.cs" ] && return 0
  for kind in models sprites; do
    cp -rn "$(harness_bin "$tree")/cache/$kind/." "$POKEMON_CACHE/$kind/" 2>/dev/null || true
  done
}

# Runs a tree's built harness: as it is on Windows, under a virtual X server with Mesa's software renderer on
# Linux (a cloud session, CI). The output folder and the arguments are the harness's own.
run_harness() {
  local tree="$1"; shift
  local dll; dll="$(harness_bin "$tree")/ShotHarness.dll"
  cache_in "$tree"
  local status=0
  if on_windows || [ "${SHOTS_NATIVE:-}" = 1 ]; then
    dotnet "$dll" "$@" || status=$?
  else
    LIBGL_ALWAYS_SOFTWARE=1 GALLIUM_DRIVER=llvmpipe \
      xvfb-run -a -s "-screen 0 3840x2160x24" dotnet "$dll" "$@" || status=$?
  fi
  cache_out "$tree"
  return $status
}

# A detached worktree of a commit under the scratch folder, made once and kept: prints its folder
commit_tree() {
  local hash; hash="$(git -C "$root" rev-parse --verify "$1^{commit}")"
  local tree="$scratch/trees/$hash"
  if [ ! -e "$tree/.git" ]; then
    git -C "$root" worktree prune
    git -C "$root" worktree add --detach "$tree" "$hash" >&2
  fi
  echo "$tree"
}

# The harness built in the working tree, which reads every run's output (`diff`, `ab`)
working_harness() { dotnet "$(harness_bin "$root")/ShotHarness.dll" "$@"; }
