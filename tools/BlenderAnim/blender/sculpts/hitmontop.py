# Hitmontop sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/hitmontop.py -- <the export's Hitmontop.glb> <out.glb>
# The skeleton stands it upside down, as the art spins it: the head bone (0, 0, 0.26) under the body (0.36), the legs
# (0.44) and the tail (0.46) above. From the references: a round brown head set in a wide brown brim like a spinning
# top's, its point under it on the ground; small round eyes, white with a black pupil, low on its face under a dark
# creased brow; a round blue body over the head, a brown point running up its front from the head; thin brown legs
# up and out to great blue boots with two white spikes each and white bands at the ankles (a brown spot on one
# boot); a thin brown tail up behind to a small blue ball with one white spike; thin brown arms down and out to round
# brown fists, white bands at the wrists.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, outward, ring_band, axes, srgb, inside, unit, eye_look, args

BROWN, BLUE, BAND, SPIKE = "#B88C6E", "#4C7EC8", "#F0ECE2", "#F4F2EC"
INK, BROW = "#1A1416", "#6A4234"
HEAD = np.array([0.0, 0.0, 0.2])            # the head's middle
HR = np.array([0.118, 0.11, 0.112])
BODY = np.array([0.0, 0.0, 0.356])
BR = np.array([0.106, 0.1, 0.1])


def spike(base, d, length=0.065, half=0.021):
    """A white spike: a solid four-sided point from a square root of 2 x half to its tip."""
    d = unit(*d)
    a, b = axes(d)
    base = np.array(base, float)
    pts = [base + a * x + b * y for x, y in ((half, half), (half, -half), (-half, -half), (-half, half))]
    pts.append(base + d * length)
    return hull(np.array(pts))


def hitmontop(src, out):
    k = Kit(src, coat="fur")

    # The top: a brim flaring from the point on the ground up and out round the head, wider to the sides
    V, F = lathe((0, 0.012, 0.0), (0, 0, 1), [(0, 0), (0.022, 0.03), (0.06, 0.06), (0.15, 0.09), (0.2, 0.115),
                                               (0.206, 0.13), (0.17, 0.138), (0.09, 0.142), (0, 0.146)], seg=56)
    V = V * np.array([1.08, 0.9, 1.0])
    k.add("brim", outward((V, F)), BROWN, "head", fuse=False)

    # The head, round, set in the brim
    k.add("head", ell(HEAD, HR), BROWN, "head")

    # The body: a round blue ball over the head, the head's brown point running up its front
    k.add("body", ell(BODY, BR), BLUE, "body")
    k.add("neck", ell((0, 0.0, 0.275), (0.075, 0.07, 0.035)), BROWN, "head")
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.078 * (0.44 - P[:, 2]) / 0.17, 0.0025) * (P[:, 2] > 0.25)
            * (P[:, 1] < -0.02) * (P[:, 2] < 0.45), BROWN)
    k.paint(lambda P: inside(P[:, 2] - 0.272, 0.003) * (P[:, 2] > 0.2), BROWN)

    # The legs: thin and brown, up and out to great blue boots with white bands and two white spikes
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        root, boot = np.array([0.065, 0.0, 0.43]) * m, np.array([0.215, -0.02, 0.665]) * m
        k.add(f"leg{side}", tube([root, (root + boot) / 2 + np.array([0, -0.01, 0.01]), boot], [0.024, 0.022, 0.022]),
              BROWN, f"leg{side}")
        k.add(f"boot{side}", ell(boot, (0.064, 0.062, 0.064)), BLUE, f"leg{side}")
        d = unit(*(boot - root))
        k.add(f"ankle{side}", ring_band(boot - d * 0.07, d, 0.026, 0.022, thick=0.007), BAND, f"leg{side}",
              fuse=False, surface="cloth")
        for i, dd in enumerate(((0.85 * s, -0.1, 0.5), (0.12 * s, 0.05, 1.0))):
            dd = unit(*dd)
            k.add(f"spike{side}{i}", spike(boot + dd * 0.05, dd), SPIKE, f"leg{side}", fuse=False, surface="shell")
    # a brown spot on the outer face of the right boot (+X, the viewer's right from the front)
    sb = np.array([0.215, -0.02, 0.665]); sn = unit(0.6, -0.8, 0.05)
    k.decal(sb + sn * 0.064, sn, (0.024, 0.03), BROWN, eps=0.003, rings=10, segs=44)

    # The tail: thin and brown, up behind to a small blue ball with one white spike
    k.add("tail", tube([(0, 0.05, 0.44), (0, 0.068, 0.51), (0, 0.08, 0.56)], [0.02, 0.019, 0.018]), BROWN, "tail")
    TB = np.array([0.0, 0.085, 0.588])
    k.add("tailball", ell(TB, (0.038, 0.038, 0.038)), BLUE, "tail")
    k.add("tailspike", spike(TB + unit(0, 0.15, 1) * 0.03, (0, 0.15, 1), length=0.05, half=0.016), SPIKE, "tail",
          fuse=False, surface="shell")

    # The arms: thin and brown, down and out to round fists, white bands at the wrists
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        root, fist = np.array([0.09, 0.0, 0.33]) * m, np.array([0.258, -0.05, 0.215]) * m
        k.add(f"arm{side}", tube([root, (root + fist) / 2 + np.array([0, -0.01, 0.015]), fist], [0.022, 0.02, 0.02]),
              BROWN, f"arm{side}")
        k.add(f"fist{side}", ell(fist + np.array([0.008 * s, -0.004, -0.006]), (0.04, 0.04, 0.04)), BROWN, f"arm{side}")
        d = unit(*(fist - root))
        k.add(f"wrist{side}", ring_band(fist - d * 0.058, d, 0.024, 0.02, thick=0.007), BAND, f"arm{side}",
              fuse=False, surface="cloth")

    # The brow: two dark creases over the eyes, low at the middle and rising outward
    CB, CD = srgb(BROWN), srgb(BROW)
    def brow(u, v):
        c = np.tile(CB, (len(u), 1))
        c[(np.abs(v - 0.55 * u) < 0.3) & (np.abs(u) < 0.92)] = CD
        return c
    bp = np.array([0.045, -0.098, 0.226])
    k.decal(bp, unit(0.38, -0.9, 0.2), (0.042, 0.013), brow, eps=0.003, rings=12, segs=56, mirror=True)

    k.build(voxel=0.006, smooth=4, band=0.004)

    # The eyes: small round whites with a dark rim and a black pupil, low on the face over the brim
    CI, CW = srgb(INK), np.array([1.0, 1.0, 1.0])
    def eye(u, v):
        c = np.tile(CI, (len(u), 1))
        c[u * u + v * v < 0.72] = CW
        c[((u + 0.02) / 0.44) ** 2 + ((v + 0.04) / 0.46) ** 2 < 1] = CI
        c[((u - 0.12) / 0.14) ** 2 + ((v - 0.16) / 0.14) ** 2 < 1] = CW
        return c
    E = np.array([0.048, -0.095, 0.178])
    def lay(state):
        k.decal(E, unit(0.42, -0.88, -0.2), (0.03, 0.03), eye_look(state, eye, CB, CI, line=0.18), eps=0.005,
                rings=20, segs=56, mirror=True)

    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    hitmontop(*args())
