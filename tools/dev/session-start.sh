#!/usr/bin/env bash
# Makes a machine ready to build, test and shoot (plan 16 · T16).
#
# Where `dotnet --list-sdks` has no 10.0 SDK (a cloud session's fresh container), it installs one and
# restores the solution and the screenshot harness. Where the SDK is there already (the Windows machine,
# CI) it does nothing. The build is left to the first command that needs one, so a session that only
# reads plans isn't kept waiting.
#
# The SDK comes from the distribution's own package where the script may install one (as root, or with sudo
# that asks no password, on Ubuntu, whose archive carries dotnet-sdk-10.0; a cloud container's network lets
# its mirrors through), and otherwise from Microsoft's dotnet-install.sh into $HOME/.dotnet, with DOTNET_ROOT
# and PATH written where the session's later commands read them (CLAUDE_ENV_FILE).
#
# .claude/settings.json runs it as a SessionStart hook; .devcontainer/devcontainer.json as postCreateCommand.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

has_sdk() { command -v dotnet >/dev/null 2>&1 && dotnet --list-sdks 2>/dev/null | grep -q '^10\.'; }

if has_sdk; then exit 0; fi

from_apt() {
  command -v apt-get >/dev/null 2>&1 || return 1
  local as_root=()
  if [ "$(id -u)" != 0 ]; then
    sudo -n true 2>/dev/null || return 1
    as_root=(sudo -n)
  fi
  echo "session-start: installing the .NET 10 SDK from the distribution" >&2
  "${as_root[@]}" apt-get update -q >&2 || return 1
  apt-cache show dotnet-sdk-10.0 >/dev/null 2>&1 || return 1
  "${as_root[@]}" env DEBIAN_FRONTEND=noninteractive apt-get install -y -q dotnet-sdk-10.0 >&2
}

from_microsoft() {
  export DOTNET_ROOT="$HOME/.dotnet"
  export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"
  if ! has_sdk; then
    echo "session-start: installing the .NET 10 SDK into $DOTNET_ROOT" >&2
    local dir; dir="$(mktemp -d)"
    curl -fsSL --retry 4 --retry-delay 2 https://dot.net/v1/dotnet-install.sh -o "$dir/dotnet-install.sh"
    bash "$dir/dotnet-install.sh" --channel 10.0 --install-dir "$DOTNET_ROOT" --no-path >&2
    rm -rf "$dir"
  fi
  if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
    {
      echo "export DOTNET_ROOT=\"$DOTNET_ROOT\""
      echo "export PATH=\"$DOTNET_ROOT:$DOTNET_ROOT/tools:\$PATH\""
    } >> "$CLAUDE_ENV_FILE"
  fi
}

# A .NET 10 already in $HOME/.dotnet only needs to be put on the PATH
if [ -x "$HOME/.dotnet/dotnet" ] || ! from_apt; then from_microsoft; fi
has_sdk || { echo "session-start: no .NET 10 SDK could be installed" >&2; exit 1; }

if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
  { echo "export DOTNET_CLI_TELEMETRY_OPTOUT=1"; echo "export DOTNET_NOLOGO=1"; } >> "$CLAUDE_ENV_FILE"
fi

echo "session-start: restoring the solution and the harness" >&2
dotnet restore "$root/PokemonPlatinum.sln" >&2
dotnet restore "$root/tools/ShotHarness" >&2
