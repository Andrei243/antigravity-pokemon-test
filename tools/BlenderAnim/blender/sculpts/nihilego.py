# Nihilego sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/nihilego.py -- <the export's Nihilego.glb> <out.glb>
#
# From the references: a jellyfish like a girl in a wide hat, hovering. A grey dome of a cap with a band of white
# five-pointed stars round it; a white brim flaring out from under the dome to a wavy blue rim; a ring of pale lumps
# under the brim where the body hangs from it; a white body like a dress, widening downward to a hem of rounded drips,
# glowing lilac-violet down its middle, darkest under the brim; two short thick tentacles out to the sides near the top
# that curl up at their tips, two longer ones lower down hanging out and down to bulbous ends, and two thin ones down
# the middle, longest of all. No face (hidden eyes: patches of the dome's own colour).
# The skeleton: root, body (0, 0, 0.315). The export stands 0.055 to 0.516.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, outward, polygon, srgb, inside, smoothstep, unit, args

DOME, STAR, BRIM, RIM, WHITE = "#C9CCD5", "#F7F8FB", "#EBEDF2", "#46AEEA", "#EEF0F5"
LILAC, VIOLET = "#BDB7E0", "#8A82BE"

# --- the frame ----------------------------------------------------------------------------------------------------------
DC, DR = np.array([0.0, 0.0, 0.392]), np.array([0.114, 0.11, 0.13])      # the dome (its rim plane at DC's height)
BRIM_IN, BRIM_OUT, BRIM_Z, WAVES = 0.098, 0.146, 0.372, 8
VOXEL = 0.0045


def dome_deform(V):
    """The cap's underside drawn up flat: the dome stands on its rim, the body hangs from its middle."""
    V = V.copy()
    low = V[:, 2] < 0
    V[low, 2] *= 0.38
    return V


def on_ell(c, r, d):
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r)) ** 2).sum())


def brim_at(th):
    """The brim's outer edge for an angle round the cap (0 at the back): its radius and height."""
    w = np.cos(WAVES * th)
    return BRIM_OUT + 0.006 * w, BRIM_Z - 0.006 - 0.013 * w


def star(points=5, inner=0.45):
    pts = []
    for i in range(points * 2):
        a = math.pi / 2 + math.pi * i / points
        r = 1.0 if i % 2 == 0 else inner
        pts.append((r * math.cos(a), r * math.sin(a)))
    return polygon(pts)


def nihilego(src, out):
    k = Kit(src, coat="scales")

    # --- the cap: a grey dome, the brim and its wavy blue rim ---------------------------------------------------------
    k.add("dome", ell(DC, DR, deform=dome_deform), DOME, "body")

    def brim(u, v):
        th = 2 * math.pi * u                                    # 0 at the back: the seam behind
        R, Z = brim_at(th)
        r = BRIM_IN + (R - BRIM_IN) * v
        z = (BRIM_Z + 0.012) + (Z - BRIM_Z - 0.012) * v ** 1.2
        return np.stack([r * np.sin(th), r * np.cos(th), z], -1)
    k.add("brim", outward(sheet(brim, 160, 7, 0.009)), BRIM, "body", fuse=False)

    ring = []
    for th in np.linspace(-0.08, 2 * math.pi + 0.08, 170):      # starts and ends behind, overlapping
        R, Z = brim_at(th)
        ring.append((R * math.sin(th), R * math.cos(th), Z))
    k.add("rim", outward(tube(ring, [0.0088] * len(ring), seg=14, per=3)), RIM, "body", fuse=False)

    # a ring of pale lumps under the brim, where the body hangs from it
    for i in range(14):
        a = 2 * math.pi * (i + 0.5) / 14
        k.add(f"lump{i}_", ell((0.064 * math.sin(a), 0.058 * math.cos(a), 0.338), (0.024, 0.024, 0.02)), WHITE,
              "body")

    # --- the body: a dress widening down to a hem of drips -----------------------------------------------------------
    k.add("neck", ell((0, 0.0, 0.31), (0.058, 0.05, 0.05)), WHITE, "body")
    k.add("bell", ell((0, 0.004, 0.235), (0.068, 0.054, 0.085)), WHITE, "body")
    k.add("hem", ell((0, 0.006, 0.18), (0.074, 0.058, 0.04)), WHITE, "body")
    # drips round the hem, the front and back ones longest
    for i in range(9):
        a = 2 * math.pi * (i + 0.5) / 9                         # 0 at the back
        front = -math.cos(a)
        low = 0.15 - 0.022 * max(front, 0) - 0.01 * max(-front, 0) - 0.008 * (i % 2)
        p = np.array([0.062 * math.sin(a), 0.006 + 0.048 * math.cos(a), 0.19])
        q = np.array([0.064 * math.sin(a), 0.006 + 0.05 * math.cos(a), low])
        k.add(f"drip{i}_", tube([p, (p + q) / 2, q], [0.022, 0.021, 0.02]), WHITE, "body")

    # the lilac glow down the middle, darkest under the brim
    def glow(P):
        x, z = np.abs(P[:, 0]), P[:, 2]
        return inside(x - 0.032, 0.022) * smoothstep(0.15, 0.25, z) * inside(z - 0.35, 0.006)
    k.paint(glow, LILAC, on=["neck", "bell", "hem", "drip"])

    def deep(P):
        x, z = np.abs(P[:, 0]), P[:, 2]
        return inside(x - 0.026, 0.018) * smoothstep(0.24, 0.31, z) * inside(z - 0.35, 0.006) * 0.85
    k.paint(deep, VIOLET, on=["neck", "bell"])

    # --- the tentacles -----------------------------------------------------------------------------------------------
    # two short ones near the top, out to the sides, their tips curling up
    up = [(0.045, -0.012, 0.305), (0.088, -0.03, 0.283), (0.128, -0.04, 0.27), (0.152, -0.042, 0.284),
          (0.156, -0.038, 0.31)]
    k.add("arm", tube(up, [0.019, 0.018, 0.017, 0.016, 0.0145]), WHITE, "body", mirror=True)
    # two longer ones lower down, out and down to bulbous ends
    low = [(0.05, -0.004, 0.245), (0.09, -0.012, 0.21), (0.118, -0.02, 0.165), (0.128, -0.022, 0.13)]
    k.add("hang", tube(low, [0.021, 0.019, 0.018, 0.018]), WHITE, "body", mirror=True)
    k.add("bulb", ell((0.129, -0.022, 0.122), (0.024, 0.024, 0.026)), WHITE, "body", mirror=True)
    # two thin ones down the middle, longest of all
    for s, side in ((1, "R"), (-1, "L")):
        mid = np.array([(0.012, 0.004, 0.17), (0.016, 0.0, 0.13), (0.02, -0.004, 0.09), (0.022, -0.006, 0.072)])
        mid *= np.array([s, 1, 1.0])
        k.add(f"tail{side}", tube(mid, [0.012, 0.011, 0.0095, 0.0085]), WHITE, "body")

    # --- the stars round the dome, white on the grey (before build: in every eye state) ------------------------------
    for i in range(9):
        a = 2 * math.pi * i / 9                                  # 0 at the front
        z = 0.442 if i % 2 == 0 else 0.418
        d = unit(math.sin(a), -math.cos(a), (z - DC[2]) / DR[2] * 1.3)
        k.decal(on_ell(DC, DR, d), d, (0.024, 0.024), STAR, eps=0.0015, outline=star(), rings=10, segs=80)

    k.build(voxel=VOXEL, smooth=5, band=0.005, wband=0.03)

    # --- no face: the eyes are hidden, patches of the dome's own colour high on its front ----------------------------
    def lay(state):
        for s in (1, -1):
            d = unit(0.25 * s, -0.75, 0.7)
            k.decal(on_ell(DC, DR, d), d, (0.012, 0.012), DOME, eps=0.0015, rings=20, segs=56)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    nihilego(*args())
