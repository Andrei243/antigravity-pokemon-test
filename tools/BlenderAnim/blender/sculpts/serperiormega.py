# Mega Serperior (a Mega of our own, plan 06 · R20b) sculpted on the skeleton and clips of the game's own export of
# the form (sculptkit.py), grown from serperior.py (sibling("serperior"): its coils, head, eyes, marks, vines and tail):
#   blender --background --factory-startup --python sculpts/serperiormega.py -- <the export's Serperior-Mega.glb> <out.glb>
# No pictures: the game's own sculptor (PokemonModels.Ours.Megas.cs, SerperiorMega) is the design. Its leaf collar
# spread into a cobra's hood of leaves framing the face, yellow and light green on a deep green backing; two curved
# horns of deep green leaf sweeping back from its crown (they take the place of the base's two yellow blades, as in the
# export); and a pair of leaf-wings standing from behind the neck, each a deep green spar with a fan of long leaves
# hanging from it, raised in a V, their faces to the front and the back.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, unit, args, outward, sibling

se = sibling("serperior")
D = se.D

GREEN, DEEP, LIGHT = "#469650", "#2E703C", "#6EB85A"
YELLOW = se.YELLOW
HC = se.HC
HOOD = np.array([0.0, -0.01, 0.515])        # the hood's middle, just behind the head


def leaf_outline(L, w):
    """A long leaf, root at a = 0, point at a = L, w its half width."""
    return [(0.0, -0.4 * w), (0.18 * L, -0.9 * w), (0.48 * L, -w), (0.78 * L, -0.6 * w), (L, 0.0),
            (0.78 * L, 0.6 * w), (0.48 * L, w), (0.18 * L, 0.9 * w), (0.0, 0.4 * w)]


def leaf_plate(root, tip, w, face, thick=0.02):
    """A flat leaf from root to tip, its faces turned toward face (and away from it)."""
    d = np.array(tip, float) - root
    L = float(np.linalg.norm(d)); d /= L
    ez = np.cross(unit(*face), d)
    return slab(leaf_outline(L, w), root, d, ez, thick)


def hood(k):
    """The cobra's hood: a deep green backing behind the head, eleven leaves in an arc round it, tips a little forward."""
    k.add("hoodback", outward(ell(HOOD + (0, 0.016, -0.005), (0.11, 0.022, 0.11), sub=3)), YELLOW, "head", fuse=False,
          surface="leaf")
    for i in range(11):
        a = math.radians(-115 + 23 * i)
        rad = np.array([math.sin(a), 0.0, math.cos(a) * 0.9])
        rad /= np.linalg.norm(rad)
        L = 0.17 - 0.025 * abs(math.sin(a * 0.5))
        root = HOOD + rad * 0.06 + (0, 0.004 * (i % 2), 0)
        tip = HOOD + rad * (0.06 + L) + (0, -0.035, 0)
        k.add(f"hood{i}", leaf_plate(root, tip, 0.04, (0, -1, 0)), YELLOW if i % 2 == 0 else LIGHT, "head",
              fuse=False, surface="leaf")


def horns(k):
    """Two horns of deep green leaf from the crown, sweeping up and back."""
    c = HC
    for s, side in ((1, "R"), (-1, "L")):
        pts = [c + (0.025 * s, 0.03, 0.035), c + (0.045 * s, 0.06, 0.11), c + (0.042 * s, 0.13, 0.165),
               c + (0.032 * s, 0.2, 0.17), c + (0.025 * s, 0.25, 0.14)]
        k.add(f"horn{side}", outward(tube(pts, [0.02, 0.019, 0.016, 0.013, 0.011])), DEEP, "head", fuse=False,
              surface="leaf")


def wings(k):
    """The leaf-wings behind the neck: a spar raised up and out, a fan of long leaves hanging from it."""
    for s, side in ((1, "R"), (-1, "L")):
        root = np.array([0.03 * s, 0.01, 0.36])
        spar = np.array([0.24 * s, 0.1, 0.6])
        face = (-0.25 * s, -1.0, 0.0)
        k.add(f"spar{side}", outward(tube([root, (root + spar) / 2 + (0, 0, 0.02), spar], [0.024, 0.02, 0.015])),
              DEEP, "seg12", fuse=False, surface="leaf")
        for i in range(6):
            t = min(1.0, 0.25 + 0.15 * i)
            at = root + (spar - root) * t
            tip = at + np.array([(0.045 + 0.016 * i) * s, 0.05, -0.085 - 0.02 * i + (0.13 if i == 5 else 0.0)])
            k.add(f"wing{side}{i}", leaf_plate(at, tip, 0.03, face), GREEN if i % 2 == 0 else LIGHT, "seg12",
                  fuse=False, surface="leaf")
        k.add(f"wingtip{side}", leaf_plate(spar, spar + np.array([0.08 * s, 0.03, 0.07]), 0.03, face), GREEN,
              "seg12", fuse=False, surface="leaf")


def serperiormega(src, out):
    k = Kit(src, coat="scales")
    line = D.Line(se.LINE, se.RADS, rise=(0.18, 0.28))
    line.tubes(k, se.BONES, se.DARK)
    se.head(k)
    se.vines(k)
    se.tail_leaves(k, line)
    hood(k)
    horns(k)
    wings(k)

    k.paint(D.belly(line, 2.5, 20.0, ground=-0.35, risen=-0.2, rise=(0.18, 0.28), edge=0.07), se.MINT, on=["seg", "nape"])
    k.paint(se.bands(line, [9.6, 10.2]), YELLOW, on=["seg"])
    k.paint(se.white_face, se.WHITE, on=["skull", "jaw", "nape", "seg"])
    se.hooks(k, line)
    se.cheek_curl(k)

    k.build(voxel=se.VOXEL, smooth=4, band=0.005)
    k.eye_states(se.lay_eyes(k))
    k.export(out)


if __name__ == "__main__":
    serperiormega(*args())
