# Wugtrio sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/wugtrio.py -- <the export's Wugtrio.glb> <out.glb>
#
# Grown from wiglett.py: three of its eels (its trunk, its face on the rounded end, its eyes) in red. From the
# references: three thick red garden eels rising from behind a grey faceted rock that stands in front of their roots,
# each leaning apart: the tallest in the middle, straight up; one curving out to the -X side and up to about half its
# height; the shortest curving out to the +X side and up; each head a rounded end with two small dark eyes and under
# them a small white oval nose, looking to the front and a little outward. The pictures overrule the task's "one
# base": the eels come out from under a rock. The drawing's darker lower trunks are shade: the 3D model is one red.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, hull, unit, args, sibling

w = sibling("wiglett")

RED, NOSE, EYE, INK = "#E8506A", "#F6F4F2", "#20203C", "#5A1424"
ROCK, ROCK_DARK = "#5C6A74", "#4A5660"
R = 0.045                                   # an eel's radius
VOXEL = 0.0058

# (tag, path, its head bone, the way the face looks, the point of the path from which the head bone takes over)
EELS = [
    ("M", [(0.02, 0.055, 0.05), (0.025, 0.048, 0.18), (0.02, 0.032, 0.33), (0.012, 0.022, 0.45), (0.008, 0.018, 0.553)],
     "head", unit(0.0, -1, 0.0), 3),
    ("L", [(-0.035, 0.03, 0.05), (-0.115, 0.015, 0.068), (-0.178, 0.004, 0.14), (-0.19, 0.0, 0.24),
           (-0.172, 0.0, 0.33)], "headL", unit(-0.38, -1, 0.0), 3),
    ("R", [(0.045, 0.02, 0.05), (0.125, 0.0, 0.066), (0.178, -0.012, 0.13), (0.186, -0.014, 0.205),
           (0.168, -0.014, 0.282)], "headR", unit(0.38, -1, 0.0), 3),
]


def stone(c, half, seed, n=16):
    """A faceted chunk of rock: the hull of points on an ellipsoid's upper half, on a flat foot."""
    rs = np.random.RandomState(seed)
    d = rs.normal(size=(n, 3)); d /= np.linalg.norm(d, axis=1, keepdims=True)
    d[:, 2] = np.abs(d[:, 2])
    pts = np.array(c, float) + d * rs.uniform(0.9, 1.02, size=(n, 1)) * np.array(half)
    foot = [np.array(c, float) + (math.cos(a) * half[0] * 0.95, math.sin(a) * half[1] * 0.95, 0)
            for a in np.linspace(0, 2 * math.pi, 7, endpoint=False) + rs.uniform(0, 0.5)]
    return hull(np.vstack([pts, foot]))


def wugtrio(src, out):
    k = Kit(src, coat="scales")
    specs = []
    for tag, path, bone, look, split in EELS:
        w.eel(k, tag, path, [R] * len(path), RED, "body", bone, split)
        specs.append(w.head(k, tag, path[-1], look, R, bone, NOSE, nose_r=(0.5, 0.32, 0.36), nose_down=0.3,
                            eye_up=0.55, eye_side=0.36))
    # The rock in front of their roots: a tall chunk, two lower beside it
    k.add("rock", stone((0.0, -0.06, 0.0), (0.125, 0.085, 0.25), 3, n=24), ROCK, "body", fuse=False, surface="shell")
    k.add("rockL", stone((-0.115, -0.03, 0.0), (0.085, 0.075, 0.15), 5), ROCK_DARK, "body", fuse=False, surface="shell")
    k.add("rockR", stone((0.115, -0.04, 0.0), (0.08, 0.07, 0.125), 7), ROCK_DARK, "body", fuse=False, surface="shell")
    # and a low one behind, out of which the roots come from the back
    k.add("rockB", stone((0.005, 0.06, 0.0), (0.13, 0.065, 0.085), 11), ROCK_DARK, "body", fuse=False,
          surface="shell")
    k.build(voxel=VOXEL, band=0.004)
    k.eye_states(lambda st: w.eyes(k, specs, st, RED, EYE, INK))
    k.export(out)


if __name__ == "__main__":
    wugtrio(*args())
