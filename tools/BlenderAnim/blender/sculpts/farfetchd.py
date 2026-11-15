# Farfetch'd sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/farfetchd.py -- <the export's Farfetch'd.glb> <out.glb>
# A plump grey-brown duck with a cream throat and belly, a broad yellow bill, a black V over stern eyes, a small fan
# of feathers on the crown, a short pointed tail and orange webbed feet. Its left wing wraps forward round a leek
# (white stalk and bulb below, two green leaves above), held up beside it; its right wing is raised behind it, its
# fingered tip pale.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, slab, srgb, inside, smoothstep, unit, eye_look, polygon, outward, args)

BROWN, CREAM, PALE, DARK = "#B49C8C", "#EADFC6", "#E2D8C8", "#8C7668"
BILL, LOWBILL, FEET, BLACK = "#F5B93A", "#E59A26", "#F3B13A", "#26201E"
WHITE, LIGHTGREEN, GREEN, LEAF = "#F3F2E8", "#B3D56E", "#58A23E", "#2F7F2E"
H = np.array([0, -0.03, 0.55])           # the head's centre
HR = np.array([0.11, 0.105, 0.105])


def farfetchd(src, out):
    k = Kit(src, coat="fur")

    # --- the body: a plump duck, fuller below and in the breast, leaning a little forward ------------------------
    def pear(V):
        V = V.copy()
        s = 1 + 0.1 * np.clip(-V[:, 2], 0, 1) - 0.08 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell((0, 0.01, 0.255), (0.145, 0.16, 0.15), rot=(-10, 0, 0), deform=pear), BROWN, "body")
    k.add("breast", ell((0, -0.035, 0.29), (0.13, 0.125, 0.14)), BROWN, "body")
    k.add("neck", ell((0, -0.025, 0.43), (0.075, 0.075, 0.075)), BROWN, "head")
    k.add("head", ell(H, HR), BROWN, "head")
    # a short pointed tail up and back
    k.add("tail", blade([(0, 0.11, 0.21), (0, 0.18, 0.24), (0, 0.245, 0.29)], [0.055, 0.035, 0.005], (0, 0, 1), 0.5),
          BROWN, "tail")
    # the left wing comes down the side and forward to a hand round the leek in front of the body
    k.add("arm", tube([(-0.12, -0.0, 0.39), (-0.155, -0.06, 0.34), (-0.15, -0.14, 0.3)], [0.055, 0.05, 0.045]),
          BROWN, "wingL")
    HAND = np.array([-0.15, -0.19, 0.29])
    k.add("hand", ell(HAND, (0.048, 0.05, 0.058), rot=(0, 10, 0)), BROWN, "wingL")
    # the right wing's root on the shoulder, under its raised plate
    k.add("shoulder", ell((0.12, 0.01, 0.38), (0.05, 0.06, 0.06)), BROWN, "wingR")

    # The cream throat and belly, round the body's upright axis; the line a little scalloped
    def belly(P):
        th = np.arctan2(P[:, 0], -(P[:, 1] - 0.03))
        return inside(np.abs(th) - (0.82 - 0.22 * smoothstep(0.32, 0.48, P[:, 2])), 0.004) \
            * inside(P[:, 2] - 0.49, 0.004) * inside(0.07 - P[:, 2], 0.004)
    k.paint(belly, CREAM, on=["body", "breast", "neck", "head"])

    # --- the bill: broad and flat, widest at its round tip, a lower bill under it -----------------------------------
    def bill(V):
        V = V.copy()
        t = np.clip(-V[:, 1], -1, 1)
        V[:, 0] *= 0.62 + 0.45 * smoothstep(-0.9, 0.4, t)
        V[:, 2] += 0.5 * V[:, 0] ** 2
        return V
    k.add("bill", outward(ell((0, -0.205, 0.522), (0.062, 0.09, 0.022), rot=(8, 0, 0), deform=bill)), BILL, "head",
          fuse=False, surface="shell")
    k.add("lowbill", outward(ell((0, -0.19, 0.5), (0.048, 0.075, 0.016), rot=(10, 0, 0), deform=bill)), LOWBILL,
          "head", fuse=False, surface="shell")

    # --- the crown's fan: three short solid feathers laid back ------------------------------------------------------
    for i, (x, lean, length) in enumerate(((-0.03, -0.35, 0.075), (0.0, 0.0, 0.09), (0.03, 0.35, 0.075))):
        b0 = np.array([x * 0.6, 0.0, 0.64])
        d = unit(lean * 0.5, 0.45, 1.0)
        k.add(f"crest{i}", outward(blade([b0, b0 + d * length * 0.6, b0 + d * length], [0.02, 0.016, 0.004],
                                         (0, 1, 0), 0.6)), BROWN, "head", fuse=False)

    # --- the raised right wing, its fingers pale --------------------------------------------------------------------
    d = unit(0.28, 0.6, 0.75)
    n = unit(0.0, 1.0, -0.8)
    ez = np.cross(n, d); ez /= np.linalg.norm(ez)
    ORG = np.array([0.12, 0.02, 0.37])
    WING = [(0.0, -0.035), (0.1, -0.07), (0.19, -0.085), (0.245, -0.075), (0.235, -0.05), (0.272, -0.04),
            (0.255, -0.015), (0.288, 0.0), (0.26, 0.02), (0.28, 0.04), (0.245, 0.055), (0.17, 0.06), (0.07, 0.05),
            (0.0, 0.035)]
    k.add("wing", slab(WING, ORG, d, ez, 0.026), BROWN, "wingR", fuse=False)
    TIPS = [(0.16, -0.092), (0.19, -0.089), (0.245, -0.077), (0.235, -0.05), (0.272, -0.04), (0.255, -0.015),
            (0.288, 0.0), (0.26, 0.02), (0.28, 0.04), (0.245, 0.058), (0.17, 0.066), (0.16, 0.066)]
    k.add("wingtip", slab(TIPS, ORG, d, ez, 0.032), PALE, "wingR", fuse=False)

    # --- the leek, through the left hand: white bulb and stalk, light green, two dark green leaves ------------------
    AX = unit(-0.11, -0.09, 1.0)
    def at(z):
        return HAND + AX * (z - HAND[2]) / AX[2]
    k.add("bulb", outward(tube([at(0.045), at(0.07), at(0.11)], [0.024, 0.031, 0.025])), WHITE, "wingL", fuse=False,
          surface="leaf")
    k.add("stalk", outward(tube([at(0.1), at(0.2), at(0.3)], [0.024, 0.024, 0.024])), WHITE, "wingL", fuse=False,
          surface="leaf")
    k.add("green", outward(tube([at(0.295), at(0.36), at(0.43)], [0.0245, 0.026, 0.028])), LIGHTGREEN, "wingL",
          fuse=False, surface="leaf")
    side = np.cross(AX, (0, 1.0, 0)); side /= np.linalg.norm(side)
    for i, s in enumerate((-1, 1)):
        b0 = at(0.41) + side * s * 0.01
        tip = at(0.68) + side * s * 0.05
        mid = (b0 + tip) / 2 + side * s * 0.008
        k.add(f"leaf{i}", outward(blade([b0, mid, tip], [0.02, 0.022, 0.018], np.cross(AX, side), 0.55)), LEAF,
              "wingL", fuse=False, surface="leaf")
    # the fingers of the wing curled round the stalk's front
    for i, z in enumerate((0.27, 0.3, 0.33)):
        c = at(z)
        pts = [c + np.array([0.035, 0.0, 0.0]), c + np.array([0.022, -0.03, 0.0]), c + np.array([-0.012, -0.035, 0.0])]
        k.add(f"finger{i}", outward(tube(pts, [0.016, 0.015, 0.012])), BROWN, "wingL", fuse=False)

    # --- legs and webbed feet ---------------------------------------------------------------------------------------
    k.add("leg", outward(tube([(0.055, 0.0, 0.15), (0.06, -0.01, 0.08), (0.062, -0.02, 0.025)], [0.02, 0.017, 0.016])),
          FEET, "leg{s}", fuse=False, mirror=True, surface="scales")
    def toes(V):
        ph = np.arctan2(V[:, 0], -V[:, 1])
        front = np.clip(-V[:, 1], 0, 1)
        s = 1 + 0.2 * np.cos(3 * ph) * front
        V = V.copy(); V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("foot", outward(ell((0.066, -0.06, 0.015), (0.048, 0.07, 0.015), rot=(0, 0, -8), deform=toes)), FEET,
          "leg{s}", fuse=False, mirror=True, surface="scales")

    # --- the black V over the eyes: one patch on the forehead, its point between the eyes over the bill --------------
    VEE = [(0, -0.3), (0.92, 0.38), (0.88, 0.74), (0, 0.1), (-0.88, 0.74), (-0.92, 0.38)]
    k.decal((0, -0.133, 0.578), (0, -1, 0.3), (0.075, 0.075), BLACK, eps=0.009, outline=polygon(VEE), rings=12,
            segs=96)

    k.build(voxel=0.0072, smooth=7)

    # --- the eyes: an upright white oval, a big black pupil to the inside; the V cuts their inner top ---------------
    WH, PU, LID, INK = srgb("#FFFFFF"), srgb("#1A1616"), srgb(BROWN), srgb("#2A2220")
    de = unit(0.5, -0.82, 0.24)
    E = H + de / np.linalg.norm(de / HR)
    def eye(u, v):
        return np.tile(WH, (len(u), 1))
    def eyes(state):
        k.decal(E, de, (0.03, 0.039), eye_look(state, eye, LID, INK), eps=0.006, rings=20, segs=56, mirror=True)
        if state in ("Open", "Fierce"):
            for s in (1, -1):
                m = np.array([s, 1, 1.0])
                k.decal((E + np.array([-0.01, -0.006, -0.002])) * m, de * m, (0.0145, 0.022), PU, eps=0.0075,
                        rings=8, segs=40)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    farfetchd(*args())
