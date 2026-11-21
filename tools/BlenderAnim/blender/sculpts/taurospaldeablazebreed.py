# Tauros-Paldea-Blaze-Breed sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/taurospaldeablazebreed.py -- <the export> <out.glb>
#
# Built by taurospaldeacombatbreed.py's frame (see its header for what the three breeds share and where they differ):
# long straight horns reaching far out, the mane's top in flame-like points with red streaks, the three tails braided
# into one rope curling forward over the back.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args

if __name__ == "__main__":
    src, out = args()
    sibling("taurospaldeacombatbreed").tauros(src, out, "Blaze")
