# Sealeo sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sealeo.py -- <the export's Sealeo.glb> <out.glb>
#
# Grown from spheal.py: its blue and cream, its round black eyes with a glint, its fangs, flippers and two-lobed tail.
# From the references: a blue seal sitting up, the body a pear from a broad seat to a round head with no neck; a great
# cream oval over the chest and belly from the chin to the ground, two cream spots on each side beside it; a short
# blue muzzle with a small dark nose, and from its sides a broad white whisker fanning out on each side, its far edge
# ragged into strands; two small white tusks under the muzzle; small black eyes high on the head; two broad blue
# flippers on the ground in front, lobed into three toes; a long tail lying behind with a two-lobed fin. No ears (the
# pictures show none, though the skeleton has their bones).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, unit, args, sibling

sp = sibling("spheal")
BLUE, CREAM, WHITE, INK = sp.BLUE, sp.CREAM, sp.WHITE, sp.INK
NOSE = "#26283A"


def whisker(k, name, root, out, length, h0, h1, strands, thick, bone, color=WHITE):
    """A broad fan of a whisker from root along out, narrow at the root and wide at its far edge, the edge cut into
    strands; its face to the front (mirrored)."""
    ex = unit(*out)
    ez = np.array([0, 0, 1.0]) - ex * ex[2]
    pts = [(0.0, -h0)]
    for i in range(strands * 2 + 1):
        t = i / (strands * 2)
        z = -h1 + 2 * h1 * t
        a = length if i % 2 == 0 else length * 0.9
        pts.append((a, z))
    pts.append((0.0, h0))
    k.add(name, slab(pts, root, ex, ez, thick), color, bone, fuse=False, mirror=True, surface="hair")


def sealeo(src, out):
    k = Kit(src, coat="fur")
    LB = np.array([0, 0.04, 0.2])             # the body: a pear from a broad seat, its top leaning forward
    LR = np.array([0.175, 0.19, 0.2])
    H = np.array([0, -0.075, 0.385])
    HR = np.array([0.1, 0.092, 0.088])
    def pear(V):
        V = V.copy()
        up = np.clip(V[:, 2], 0, 1)
        V[:, :2] *= (1 - 0.42 * up ** 1.3)[:, None]
        V[:, 1] -= 0.32 * up ** 2
        V[:, 2] = np.where(V[:, 2] < -0.75, -0.75 + (V[:, 2] + 0.75) * 0.5, V[:, 2])
        return V
    k.add("seat", ell(LB, LR, deform=pear), BLUE, "body")
    k.add("chest", ell((0, -0.06, 0.29), (0.115, 0.1, 0.11)), BLUE, "body")
    k.add("head", ell(H, HR), BLUE, "head")
    k.add("neck", ell((0, -0.04, 0.335), (0.105, 0.095, 0.075)), BLUE, "head")
    # the short muzzle and its nose
    M = H + np.array([0, -0.085, -0.02])
    k.add("muzzle", ell(M, (0.05, 0.04, 0.034)), BLUE, "head")
    k.add("nose", ell(M + (0, -0.036, 0.016), (0.016, 0.01, 0.01)), NOSE, "head", fuse=False, surface="skin")
    # the whiskers fanning out from the muzzle's sides, a little down and back
    whisker(k, "whisker", M + (0.03, -0.012, -0.004), (1, 0.18, -0.2), 0.125, 0.018, 0.05, 4, 0.026, "head")
    # small tusks under the muzzle
    sp.fang(k, "tusk", M + (0.02, -0.022, -0.026), (0.05, -0.2, -1), 0.03, 0.012, "head")
    # the front flippers on the ground, pointing forward and out
    k.add("arm", tube([(0.12, -0.07, 0.075), (0.145, -0.12, 0.04), (0.15, -0.14, 0.03)], [0.055, 0.045, 0.036]), BLUE,
          "leg{s}", mirror=True)
    k.add("flipper", sp.flipper((0.165, -0.185, 0.02), (0.068, 0.042, 0.017), (0, 0, -64), toes=3, amp=0.1), BLUE,
          "leg{s}", mirror=True)
    # the tail lying behind, tapering, and its fin
    k.add("tail", tube([(0, 0.15, 0.1), (0, 0.25, 0.05), (0, 0.31, 0.035)],
                       [0.1, 0.055, 0.034]), BLUE, "tail")
    sp.fluke(k, (0, 0.305, 0.035), 1.0, 0.6, 0.45, (0.065, 0.042, 0.017), BLUE, "tail")
    # the cream oval over the chest and belly, from the chin to the ground
    def cream(P):
        a = np.arctan2(P[:, 0], -(P[:, 1] - 0.02)) * 0.16
        return inside(np.hypot(a / 0.12, (P[:, 2] - 0.18) / 0.175) - 1, 0.012) * (P[:, 1] < 0.0)
    k.paint(cream, CREAM, on=["seat", "chest", "head", "neck"])
    # cream spots beside the belly on each side
    sp.spots(k, LB, LR, [((0.86, -0.42, 0.15), (0.034, 0.04)), ((0.8, -0.38, -0.35), (0.02, 0.024))], color=CREAM)
    k.build(voxel=0.006, smooth=4, band=0.006)
    E = [(sp.at_surface(H, HR, d), d, sx) for sx in (1, -1) for d in [unit(0.42 * sx, -0.8, 0.42)]]
    k.eye_states(lambda st: sp.eyes(k, E, (0.019, 0.023), st, BLUE))
    k.export(out)


if __name__ == "__main__":
    sealeo(*args())
