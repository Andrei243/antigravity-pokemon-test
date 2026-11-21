# Diglett-Alola sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py),
# grown from diglett.py (imported: its peg, eyes, nose, shine, earth and stones):
#   blender --background --factory-startup --python sculpts/diglettalola.py -- <the export's Diglett-Alola.glb> <out.glb>
#
# From the references: Diglett's peg in a warmer, darker brown with a pinker nose, standing in a ring of dark grey
# broken earth, and three pale golden hairs rising from the crown on the "hair" bone: the middle one up and curling a
# little to the right, the other two arcing out and down to either side. The hairs are flat strips, wide enough to
# read at the game's size, and short, so the peg keeps its size in the cell.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, blade, sibling, outward, args

d = sibling("diglett")

BROWN, NOSE, SHINE = "#9C6438", "#EC7894", "#F6AEC0"
EARTH, EARTH_DARK, STONE = "#6E6E78", "#4A4A54", "#9A9CA6"
HAIR = "#F2DC82"


def colour(mod=d):
    """The Alolan's colours on the base's module: its functions read them as they run."""
    mod.BROWN, mod.NOSE, mod.SHINE = BROWN, NOSE, SHINE
    mod.EARTH, mod.EARTH_DARK, mod.STONE = EARTH, EARTH_DARK, STONE


def hairs(k, top, bone="hair", scale=1.0):
    """Three golden hairs from the crown at height top: flat strips, root sunk in the dome."""
    z = top
    strands = [
        [(0.0, 0.0, -0.012), (0.003, 0.0, 0.05), (0.022, 0.0, 0.098), (0.05, 0.004, 0.1)],
        [(0.012, 0.0, -0.012), (0.034, 0.0, 0.045), (0.07, 0.004, 0.08), (0.108, 0.008, 0.068)],
        [(-0.012, 0.0, -0.012), (-0.034, 0.0, 0.045), (-0.07, 0.004, 0.08), (-0.108, 0.008, 0.068)],
    ]
    for i, pts in enumerate(strands):
        P = [(x * scale, y * scale, z + h * scale) for x, y, h in pts]
        k.add(f"hair{i}", outward(blade(P, [0.0125, 0.012, 0.0105, 0.009], (0, 1, 0), 0.5)), HAIR, bone,
              fuse=False, surface="hair")


if __name__ == "__main__":
    src, out = args()
    colour()
    k = Kit(src, coat="scales")
    d.burrow(k, 0.215, 0.2, 13, 82)
    spec = d.diglett(k, "", (0.0, 0.0), 0.165, 0.5, "body")
    hairs(k, 0.5)
    k.build(voxel=0.006, smooth=3, band=0.004)
    k.eye_states(lambda s: d.eyes(k, [spec], s))
    k.export(out)
