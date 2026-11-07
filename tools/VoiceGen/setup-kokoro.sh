#!/usr/bin/env bash
# Sets up plan 26's voice engine, Kokoro-82M (Apache-2.0; approved by the user on 2026-11-07), in a folder of its own:
#   <home>/env      a Python environment of its own with requirements-kokoro.txt's packages
#   <home>/models   the model (ONNX, fp32), its config (the phoneme vocabulary) and Kokoro's English voices
# and writes tools/VoiceGen/engine.json (git-ignored) from engine.kokoro.json. <home> is VOICEGEN_HOME, else
# ~/.cache/pokemon-voicegen: never POKEMON_CACHE, which may be removed whole at any time. Nothing of it goes into the
# repository; run it again on a new machine.
#
#   bash tools/VoiceGen/setup-kokoro.sh            # PYTHON=<a Python 3.9 to 3.12> to choose the interpreter the environment is made from
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
root="${VOICEGEN_HOME:-$HOME/.cache/pokemon-voicegen}"
models="$root/models"
mkdir -p "$models/voices"

python="${PYTHON:-}"
if [ -z "$python" ]; then
  if command -v py >/dev/null 2>&1; then python="py -3"; else python="python3"; fi
fi

if [ ! -d "$root/env" ]; then
  echo "making the environment in $root/env with $python"
  $python -m venv "$root/env"
fi
if [ -x "$root/env/Scripts/python.exe" ]; then env_python="$root/env/Scripts/python.exe"; bin="Scripts/python.exe"; else env_python="$root/env/bin/python"; bin="bin/python"; fi
"$env_python" -I -m pip install --quiet --upgrade pip
"$env_python" -I -m pip install --quiet --no-deps -r "$here/requirements-kokoro.txt"

# The model's files, each held to the checksum it had when it was approved (a file that differs is fetched again once, then refused)
hf="https://huggingface.co"
fetch() { # <url> <file> <sha256>
  local url="$1" file="$2" want="$3"
  if [ -f "$file" ] && [ "$(sha256sum "$file" | cut -d' ' -f1)" = "$want" ]; then return 0; fi
  echo "fetching $(basename "$file")"
  curl -sSfL -o "$file" "$url"
  local got; got="$(sha256sum "$file" | cut -d' ' -f1)"
  if [ "$got" != "$want" ]; then echo "$(basename "$file"): checksum $got, not $want; removed" >&2; rm -f "$file"; exit 1; fi
}
fetch "$hf/onnx-community/Kokoro-82M-v1.0-ONNX/resolve/main/onnx/model.onnx" "$models/model.onnx" 8fbea51ea711f2af382e88c833d9e288c6dc82ce5e98421ea61c058ce21a34cb
fetch "$hf/hexgrad/Kokoro-82M/resolve/main/config.json" "$models/config.json" 5abb01e2403b072bf03d04fde160443e209d7a0dad49a423be15196b9b43c17f
while read -r sum name; do
  fetch "$hf/onnx-community/Kokoro-82M-v1.0-ONNX/resolve/main/voices/$name.bin" "$models/voices/$name.bin" "$sum"
done <<'VOICES'
c4a6b876047fd7fb472edf4ebd63cfac7c3b958a7cae7c106e8f038ca6308c45 af_alloy
4a004c33430762e2461eedb2013fad808ef4ab3121f5300f554476caf58d8361 af_aoede
f69d836209b78eb8c66e75e3cda491e26ea838a3674257e9d4e5703cbaf55c8b af_bella
d583ccff3cdca2f7fae535cb998ac07e9fcb90f09737b9a41fa2734ec44a8f0b af_heart
a240a5e3c15b43563d6e923bdca8ef5613a23471d9b77653694012435df23bd8 af_jessica
9be5221b6a941c04b561959b8ff0b06e809444dcc4ab7e75a7b23606f691819e af_kore
cd2191ab31b914ed7b318416b0e4440fdf392ddad9106a060819aa600a64f59a af_nicole
18778272caa0d0eebaea251c35fd635f038434f9eee5e691d02a174bd328414f af_nova
00a2bcf82b1d86e8f19902ede58c65ccf6c0e43b44b7d74fad54e5d8933c9c30 af_river
4409fbc125afabacc615d94db5398d847006a737b0247d6892b7a9a0007a2f0a af_sarah
4435255c9744f3f31659e0d714ab7689bf65d9e77ec1cce060f083912614f0b9 af_sky
162b035ed91cfc48b6046982184c645f72edcdd1b82843347f605d7bf7b15716 am_adam
3968b92c3c4cd1c4416dbded36c13eaa388a90d5788d02a13e4d781f5f8cf3c3 am_echo
e8b5be17edd1e3636901ce7598baafe2dc8dd8ff707a0c23bf9e461add7e2832 am_eric
c27989f741f7ee34d273a39d8a595cc0837d35f5ced9a29b7cc162614616df43 am_fenrir
52403be32fd047c6a44517cb0bcd6b134f2a18baa73e70ef41651e0eab921ade am_liam
1d1f21dd8da39c30705cd4c75d039d265e9bc4a2a93ed09bc9e1b1225eb95ba1 am_michael
da5d135b424164916d75a68ffb4c2abce3d7d5ccc82dd1ee6cf447ce286145e6 am_onyx
fcf73c989033e9233e0b98713eca600c8c74dcc1614b37009d5450ff4a2274a0 am_puck
61150cf726ab6c5ed7a99f90a304f91f5a72c00c592e89ec94e5df11c319227a am_santa
08afa6ba24da61ea5e8efa139e5aadc938d83f0a6da5a900adaf763ac1da5573 bf_alice
669fe0647f9dd04fcab92f1439a40eeb4c8b4ab1f82e4996fe3d918ce4a63b73 bf_emma
3754352c4aaa46d17f27654ab7518d65b62ad6163a0f55a5f4330c2da2c4e94f bf_isabella
5e0ee32ebe64a467124976b14e69590746f1c4ce41a12b587a50c862edfea335 bf_lily
6b3194bbceffb746733cbc22c8f593dd44e401a71d53895a2dca891bc595a1e8 bm_daniel
f889083196807b4adb15e9204252165f503b8d33d3982e681c52443c49d798f1 bm_fable
c4b235a4c1f2cd3b939fed08b899ce9385638b763f7b73a59616c4fc9bd6c9bc bm_george
b8f671cef828c30e66fdf0b0756a76bba58f6bb3398cbbf27058642acbcedb97 bm_lewis
VOICES

sed "s#{voicegen}/env/Scripts/python.exe#{voicegen}/env/$bin#" "$here/engine.kokoro.json" > "$here/engine.json"
echo "Kokoro-82M is ready in $root; $here/engine.json written. Next: dotnet run --project tools/VoiceTool -- generate rowan --region sinnoh"
