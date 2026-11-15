# Grovyle sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/grovyle.py -- <the export's Grovyle.glb> <out.glb>
# Treecko grown lean and quick (built by treecko.py's functions: the line's green and red, its slit eyes cut by the
# brow, its red from the mouth down the throat, its digits, legs and arms): a long flat head with a pointed snout
# carried forward on a slim neck, small gold eyes under a scowling brow, the lower jaw red and the red running down the
# front of the neck and the belly to a red band across the hips; one long dark green leaf laid back from the top of the
# head; thin arms forward and a little down, three fingers a hand, three long dark leaves hanging back from each
# forearm; fat round haunches fused into the hips reaching down near the ankle, short thick shins, feet of three toes; a tail that is a fan of long dark leaves
# drooping back and out to the +X side.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from types import SimpleNamespace
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, args, sibling

T = sibling("treecko")
GREEN, RED = "#68C254", T.RED
LEAF = "#3A8A3C"
VOXEL = 0.0058

L = SimpleNamespace(**vars(T.L))
L.H, L.HR = np.array([0.0, -0.11, 0.67]), np.array([0.078, 0.122, 0.066])
L.eye_dir, L.eye_shift, L.eye_h = (0.82, -0.42, 0.38), (0, 0, 0.0), (0.032, 0.026)
L.eye_outline = T.brow_cut(0.42, 0.32)
L.eye_look = T.slit_eye(L.eye_outline, rim=0.78)
L.slit, L.slit_at = (0.2, 0.6), (0.0, -0.12)
L.lid = GREEN
L.red = [(0.31, 0.0), (0.318, 0.085), (0.355, 0.098), (0.375, 0.064), (0.4, 0.064), (0.46, 0.06), (0.52, 0.046), (0.58, 0.036), (0.62, 0.04),
         (0.64, 0.052)]
L.red_axis = [(0.3, 0.0), (0.45, -0.02), (0.56, -0.05), (0.62, -0.075), (0.7, -0.06)]
L.smile = (0.648, 0.012, 1.45)


def head(k, L):
    """A long flat head tapering to a pointed snout, a brow ridge over each eye, two nostril dots."""
    H, HR = L.H, L.HR

    def wedge(V):
        V = V.copy()
        t = np.clip(-V[:, 1], 0, 1)                        # toward the snout
        V[:, 0] *= 1 - 0.58 * t ** 1.3
        V[:, 2] *= 1 - 0.42 * t ** 1.3
        V[:, 2] += 0.06 * (1 - V[:, 1] ** 2) * (V[:, 2] > 0)      # a flat crown, a little high
        return V
    k.add("head", ell(H, HR, deform=wedge), GREEN, "head")
    k.add("jaw", ell(H + (0, -0.02, -0.03), (0.062, 0.09, 0.04)), GREEN, "head")
    k.add("brow", ell(H + (0.04, -0.03, 0.04), (0.03, 0.04, 0.018), rot=(0, 0, -20)), GREEN, "head", mirror=True)
    k.decal(H + (0.013, -0.1, 0.0), unit(0.15, -0.8, 0.6), (0.006, 0.005), T.NOSTRIL, eps=0.003, rings=4, segs=16,
            mirror=True)


def grovyle(src, out):
    k = Kit(src, coat="scales")

    # The body: a slim trunk leaning forward over round hips, a slim neck carrying the head forward
    k.add("torso", ell((0, -0.008, 0.43), (0.082, 0.076, 0.115), rot=(12, 0, 0)), GREEN, "body")
    k.add("hips", ell((0, 0.02, 0.32), (0.105, 0.092, 0.08)), GREEN, "body")
    k.add("neck", tube([(0, -0.025, 0.5), (0, -0.05, 0.57), (0, -0.075, 0.625)], [0.068, 0.062, 0.057]), GREEN, "head")
    head(k, L)

    # The legs: round haunches fused into the hips, slim shins, feet of three toes
    for s, side in ((1, "R"), (-1, "L")):
        T.leg(k, side, s, (0.082, -0.005, 0.205), (0.074, 0.088, 0.155),
              [(0.09, -0.012, 0.1), (0.094, -0.012, 0.045)], [0.046, 0.042],
              (0.096, -0.045, 0.02), (0.042, 0.07, 0.02),
              [(0.55, -1, 0.05), (0.05, -1, 0.05), (-0.45, -1, 0.05)], 0.045, 0.0125, 0.0, color=GREEN)

    # The arms: thin, forward and a little down, three fingers; three long dark leaves hanging back off each forearm
    for s, side in ((1, "R"), (-1, "L")):
        sh, el, wr = np.array((0.065, -0.03, 0.5)), np.array((0.105, -0.06, 0.425)), np.array((0.1, -0.15, 0.41))
        T.arm(k, side, s, [sh, el, wr], [0.032, 0.028, 0.025], wr + (0, -0.024, 0), (0.028, 0.028, 0.024),
              [(0.5, -0.9, -0.2), (0.05, -1, -0.25), (-0.4, -0.9, -0.2)], 0.035, 0.0105, 0.0, color=GREEN)
        m = np.array([s, 1, 1.0])
        for j, (t, d, ln) in enumerate([(0.25, (0.55, 0.55, -0.62), 0.2), (0.5, (0.45, 0.35, -0.82), 0.24),
                                        (0.75, (0.35, 0.12, -0.93), 0.19)]):
            root = el + (wr - el) * t + np.array([0.012, 0.0, -0.012])
            k.add(f"armleaf{side}{j}", T.leaf_blade(root * m, np.array(d) * m, (s, -0.6, 0.1), ln, 0.034, 0.026),
                  LEAF, f"arm{side}", fuse=False, surface="leaf")

    # The tail: a short stub off the hips, a fan of long dark leaves drooping back and out to the +X side
    TAIL = np.array([(0, 0.06, 0.35), (0.015, 0.12, 0.32), (0.035, 0.17, 0.29)])
    k.add("tail", tube(TAIL, [0.05, 0.036, 0.026]), GREEN, "tail")
    for j, (d, ln, w) in enumerate([((0.45, 0.85, -0.12), 0.34, 0.036), ((0.2, 0.9, -0.42), 0.36, 0.038),
                                    ((0.62, 0.7, -0.38), 0.3, 0.034), ((-0.12, 0.92, -0.25), 0.28, 0.032)]):
        k.add(f"tailleaf{j}", T.leaf_blade(TAIL[-1] - unit(*d) * 0.02, d, (1, -0.25, 0.45), ln, w, 0.028),
              LEAF, "tail", fuse=False, surface="leaf")

    # The leaf on the head: long, laid back off the crown and a little out to the +X side
    H = L.H
    k.add("headleaf", T.leaf_blade(H + (0.0, -0.01, 0.07), (0.16, 1.0, -0.38), (0.35, 0.3, 1.0), 0.44, 0.042, 0.032,
                                   widest=0.3), LEAF, "head", fuse=False, surface="leaf")

    # The red from the lower jaw down the front, widening at the hips into a band across them
    k.paint(T.red_front(L), RED)

    lay, E, de = T.eyes(k, L)
    k.build(voxel=VOXEL, smooth=4, band=0.006)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    grovyle(*args())
