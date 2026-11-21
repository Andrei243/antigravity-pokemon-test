# Tauros-Paldea-Aqua-Breed sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/taurospaldeaaquabreed.py -- <the export> <out.glb>
#
# Built by taurospaldeacombatbreed.py's frame (see its header for what the three breeds share and where they differ):
# thick ribbed horns curling back and down like a ram's, blue drops on the mane's front under the chin, the three whips
# trailing straight back.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args

if __name__ == "__main__":
    src, out = args()
    sibling("taurospaldeacombatbreed").tauros(src, out, "Aqua")
