# Slurpuff sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/slurpuff.py -- <the export's Slurpuff.glb> <out.glb>
#
# Grown from Swirlix's script (sibling("swirlix")): the same upright pink eyes, tiny nose, wavy mouth and the pink
# tongue out of its right corner. From the references: a white cream puff standing on two stubby pink feet with toes,
# a pear wider below; over its head a great pink cap of whipped cream in soft lobes hanging as bangs at the face's
# sides, with a round pink puff on each side at the height of the face, wider than the body; on top a deep pink
# cherry with a little drip under it; a deep pink scarf of syrup round its neck, dripping in waves; a deep pink spot
# low on its right belly (the viewer's right); short white arms (out to the sides in the art: carried forward and a
# little down here).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, lumpy, args, sibling

sw = sibling("swirlix")

CREAM = sw.WHITE_C
CAP, CAP_LOW = "#F5C3D3", "#E7A6BC"
CHERRY = "#D9426F"
SYRUP = "#DE4E7A"
FEET = "#F0B2C6"

# --- Slurpuff's frame (joints: body (0, 0, 0.18), arms (+-0.1, -0.02, 0.18), legs (+-0.04, 0, 0.06); the export
#     stands 0 to 0.415) ---------------------------------------------------------------------------------------------
BC = np.array([0.0, 0.0, 0.15])          # the white body
BR = np.array([0.126, 0.108, 0.128])
FC = np.array([0.0, -0.04, 0.214])       # the face's smoother front under the cap
FR = np.array([0.08, 0.066, 0.05])
EYE_D = (0.48, -0.86, 0.32)
EYE_HALF = (0.015, 0.02)


def pear(V):
    V = V.copy()
    s = 1 + 0.12 * np.clip(-V[:, 2], 0, 1) - 0.08 * np.clip(V[:, 2], 0, 1)
    V[:, 0] *= s; V[:, 1] *= s
    return V


def collar(P):
    """The syrup round the neck: a band whose lower edge drips in waves."""
    th = np.arctan2(P[:, 0], -P[:, 1])
    low = 0.168 - 0.016 * (0.5 + 0.5 * np.cos(5 * th + 0.6)) ** 2
    rad = np.hypot(P[:, 0] / BR[0], P[:, 1] / BR[1])
    return inside(low - P[:, 2], 0.002) * inside(P[:, 2] - 0.198, 0.002) * (rad > 0.5) * (rad < 1.12)


def slurpuff(src, out):
    k = Kit(src, coat="fur")
    k.add("body", ell(BC, BR, deform=pear), CREAM, "body")
    k.add("face", ell(FC, FR), CREAM, "body")
    # the cap of whipped cream: soft lobes over the head, bangs at the face's sides
    k.add("cap", ell((0.0, 0.004, 0.27), (0.122, 0.11, 0.068), deform=lumpy(2, 1, 0.04)), CAP, "body")
    for side in (1, -1):
        nm = "R" if side > 0 else "L"
        k.add("bang" + nm, ell((0.072 * side, -0.062, 0.236), (0.032, 0.034, 0.042)), CAP, "body")
        # the round puff on each side, joined to the cap by a hidden bridge
        k.add("puff" + nm, ell((0.15 * side, 0.004, 0.236), (0.058, 0.054, 0.056)), CAP, "body")
        k.add("bridge" + nm, ell((0.106 * side, 0.004, 0.246), (0.05, 0.046, 0.04)), CAP, "body")
    # the cherry and its drip
    k.add("cherry", ell((0.0, 0.0, 0.358), (0.054, 0.052, 0.052)), CHERRY, "body")
    k.add("drip", tube([(0.0, -0.01, 0.31), (0.0, -0.02, 0.298), (0.0, -0.022, 0.29)], [0.016, 0.012, 0.008]),
          CHERRY, "body")
    # short white arms, forward and a little down
    k.add("arm", tube([(0.1, -0.02, 0.18), (0.118, -0.06, 0.158), (0.122, -0.088, 0.142)], [0.026, 0.023, 0.02]),
          CREAM, "arm{s}", mirror=True)
    # stubby feet with three toes
    k.add("leg", tube([(0.046, 0.0, 0.07), (0.05, -0.012, 0.034)], [0.034, 0.034]), FEET, "leg{s}", mirror=True)
    k.add("foot", ell((0.052, -0.018, 0.022), (0.04, 0.046, 0.024)), FEET, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.022, 0.0, 0.022)):
        k.add(f"toe{j}", ell((0.052 + dx, -0.058, 0.016), (0.012, 0.012, 0.014), sub=3), FEET, "leg{s}", mirror=True)
    # paints: the cap a shade deeper underneath, the syrup collar
    k.paint(lambda P: inside(P[:, 2] - 0.246, 0.012) * (P[:, 2] > 0.18), CAP_LOW, on=["puff", "bang", "cap", "lobe"])
    k.paint(collar, SYRUP)
    # the face and the spot
    sw.lay_mouth(k, FC, FR, (0.0, -1.0, -0.3), (0.024, 0.01), CREAM)
    sw.lay_nose(k, FC, FR, (0.0, -1.0, 0.06), (0.005, 0.004))
    sw.tongue(k, (0.018, -0.104, 0.186), (0.011, 0.005, 0.014), "body")
    p, n = sw.on_ell(BC, BR, (0.62, -0.72, -0.4))
    k.decal(p, n, (0.016, 0.022), SYRUP, eps=0.003, rings=10, segs=40)
    k.build(voxel=0.004, smooth=5, band=0.004, wband=0.04)

    def eyes(state):
        sw.lay_eyes(k, FC, FR, EYE_D, EYE_HALF, CREAM, state)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    slurpuff(*args())
