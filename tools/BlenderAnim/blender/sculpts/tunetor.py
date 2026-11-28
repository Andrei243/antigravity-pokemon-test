# Tunetor (No. 10111, Carpathia) sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py), grown from Norel's script (sibling("norel")):
#   blender --background --factory-startup --python sculpts/tunetor.py -- <the export's Tunetor.glb> <out.glb>
#
# The design, from its picture (10111-Tunetor.jpg) and its brief (CP-dex-2b, "111. Tunetor: the thundercloud"): Norel
# grown into a great thundercloud, dark indigo-grey. Its head is Norel's whole cloud (the same billows round a
# scalloped face, the same glowering eyes, now white-hot, under heavy brows) set at the front top of a bigger body;
# bristles of static stand up off its crown. From its shoulders two broad wings run out and a little up, each a row of
# billows from which a sheet of falling rain hangs nearly to the ground, its lower edge ragged in long drops, with a
# fork of lightning down its outer edge. In its belly lightning glows yellow through a scalloped rim of dark cloud,
# bolts flickering on it; below the belly the cloud narrows to a wisp that curls out to its right (+X). Its mouth is a
# grimace of white teeth. The picture's loose sparks and drops in the air are left out.
#
# The skeleton: body (0, 0, 0.484), head (0, -0.08, 0.564), wings (+-0.18, 0.03, 0.544), tail (0, 0.06, 0.264).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, slab, sibling, args

n = sibling("norel")

# --- Tunetor's colours (Norel's, darkened into indigo) ---------------------------------------------------------------
CLOUD, CLOUD_TOP, CLOUD_LOW = "#454C66", "#5B6380", "#353B51"
FACE = "#4E5672"
SPIKE = "#252A3A"
BROW = "#20242F"
RAIN, STREAK = "#5A6F93", "#8EA4C4"
GLOW = "#FFD034"
BOLT = "#FFF1A0"
MOUTH = srgb("#1C1F2A")
TEETH = np.array([1.0, 1.0, 1.0])
EYE = n.glow_eye(core=srgb("#FFFFF2"), mid=srgb("#FFF3A8"), edge=srgb("#F4C640"), rim=srgb("#1A1D28"))

# --- the frame ------------------------------------------------------------------------------------------------------
C = np.array([0.0, 0.03, 0.47])               # the body's middle
R = np.array([0.18, 0.15, 0.19])
HC = np.array([0.0, -0.075, 0.575])              # the head: Norel's cloud, its middle here
HS = 1.25                                     # and Norel's frame at this size
FC = HC + (n.FC - n.C) * HS
FR = n.FR * HS
EYE_D = (0.42, -0.9, 0.12)
EYE_HALF = (0.044, 0.038)
CUT = (0.25, 0.55)
VOXEL = 0.0068

COLOURS = {n.CLOUD: CLOUD, n.CLOUD_TOP: CLOUD_TOP, n.CLOUD_LOW: CLOUD_LOW}


def head(k):
    """Norel's cloud at the head: its billows (but the three under its face, which the belly's rim takes), its face."""
    items = [(HC + (np.array(c) - n.C) * HS, r * HS, COLOURS[col]) for i, (c, r, col) in enumerate(n.BILLOWS)
             if i not in (5, 6, 7)]
    k.add("hcore", ell(HC, n.R * HS), CLOUD, "head")
    n.billows(k, "hbillow", items, "head")
    k.add("face", ell(FC, FR), FACE, "head")
    n.scallops(k, "scalltop", FC, FR, 0.75, 4, 0.04 * HS, FACE, "head", span=0.75)


def body(k):
    k.add("core", ell(C, R), CLOUD, "body")
    # the great billows of its back, over and behind the head
    for i, (c, r, col) in enumerate((((0.0, 0.08, 0.66), 0.1, CLOUD_TOP), ((0.12, 0.07, 0.645), 0.088, CLOUD_TOP),
                                     ((-0.12, 0.07, 0.645), 0.088, CLOUD_TOP), ((0.0, 0.15, 0.56), 0.11, CLOUD),
                                     ((0.15, 0.1, 0.46), 0.1, CLOUD), ((-0.15, 0.1, 0.46), 0.1, CLOUD),
                                     ((0.0, 0.13, 0.36), 0.1, CLOUD_LOW))):
        k.add(f"back{i}", ell(c, (r, r * 0.95, r * 0.92), sub=3), col, "body")
    # the belly's scalloped rim round the glow
    for i, (x, z, r) in enumerate(((0.155, 0.35, 0.065), (0.12, 0.25, 0.06), (0.0, 0.215, 0.062), (-0.12, 0.25, 0.06),
                                   (-0.155, 0.35, 0.065), (0.08, 0.44, 0.05), (-0.08, 0.44, 0.05))):
        k.add(f"rim{i}", ell((x, -0.07, z), (r, r * 0.9, r * 0.9), sub=3), CLOUD_LOW if z < 0.3 else CLOUD, "body")
    # the glow in its belly (loose, so it glows alone), with bolts flickering on it
    k.add("belly", ell((0.0, -0.08, 0.33), (0.13, 0.075, 0.085)), GLOW, "body", fuse=False, glow=GLOW)
    for i, (d, lean) in enumerate((((0.25, -1.0, 0.05), 0.3), ((-0.45, -0.9, -0.15), -0.4), ((0.6, -0.8, -0.3), 0.6))):
        p, nn = n.on_ell((0.0, -0.08, 0.33), (0.13, 0.075, 0.085), d)
        n.bolt(k, f"bellybolt{i}", p, nn, 0.06, BOLT, "body", 0.008, lean=lean, mirror_x=i == 1)
    # the wisp under it, curling out to +X
    k.add("wisp", tube([(0.0, 0.03, 0.27), (0.0, 0.0, 0.2), (0.05, -0.02, 0.155), (0.12, -0.02, 0.15),
                        (0.165, -0.02, 0.185), (0.16, -0.02, 0.225)], [0.075, 0.055, 0.04, 0.03, 0.022, 0.017]),
          CLOUD_LOW, "tail")


# a sheet of rain under a wing: (a out along the wing, b up) from its top inner corner; its lower edge in long drops
SHEET = [(0.0, 0.0), (0.27, 0.03), (0.26, -0.42), (0.235, -0.52), (0.215, -0.43), (0.19, -0.5), (0.165, -0.4),
         (0.14, -0.46), (0.115, -0.37), (0.09, -0.41), (0.065, -0.32), (0.04, -0.35), (0.015, -0.27), (0.0, -0.29)]
STREAKS = [(0.05, -0.04, -0.27), (0.12, -0.03, -0.36), (0.2, -0.02, -0.42)]
FORK = [(0.265, -0.02), (0.235, -0.11), (0.262, -0.14), (0.228, -0.26), (0.255, -0.28), (0.232, -0.38)]


def ribbon(pts, width):
    """A zigzag line as a plate's outline: each point offset either side by half the width."""
    p = np.array(pts, float)
    left, right = [], []
    for i in range(len(p)):
        d = p[min(i + 1, len(p) - 1)] - p[max(i - 1, 0)]
        nrm = np.array([-d[1], d[0]]) / np.linalg.norm(d)
        left.append(p[i] + nrm * width / 2); right.append(p[i] - nrm * width / 2)
    return [tuple(q) for q in left] + [tuple(q) for q in right[::-1]]


def wings(k):
    ex = n.unit(1.0, 0.3, 0.0) if hasattr(n, "unit") else unit(1.0, 0.3, 0.0)
    ez = np.array([0.0, 0.0, 1.0])
    origin = np.array([0.16, 0.04, 0.65])
    # the billows along the wing, rising a little outward
    for i, t in enumerate(np.linspace(0, 1, 5)):
        c = (0.17 + 0.21 * t, 0.03 + 0.05 * t, 0.63 + 0.07 * t)
        r = 0.095 - 0.02 * t
        k.add(f"wbillow{i}", ell(c, (r, r * 0.9, r * 0.85), sub=3), CLOUD, "wing{s}", mirror=True)
    # a row of smaller billows riding on top of the wing, lighter, so the wing reads as a mass of cloud
    for i, t in enumerate(np.linspace(0.1, 0.9, 4)):
        c = (0.18 + 0.21 * t + 0.02 * (i % 2), 0.05 + 0.05 * t, 0.7 + 0.065 * t)
        r = 0.062 - 0.012 * t
        k.add(f"wtop{i}", ell(c, (r, r * 0.9, r * 0.85), sub=3), CLOUD_TOP, "wing{s}", mirror=True)
    k.add("wingroot", ell((0.15, 0.03, 0.6), (0.07, 0.07, 0.07), sub=3), CLOUD, "wing{s}", mirror=True)
    # the rain hanging from it, its streaks thicker than it so they show front and back, the fork on its outer edge
    k.add("rain", slab(SHEET, origin, ex, ez, 0.022), RAIN, "wing{s}", fuse=False, mirror=True, surface="shell")
    for i, (a, top, bot) in enumerate(STREAKS):
        strip = [(a - 0.012, top), (a + 0.012, top), (a + 0.009, bot), (a, bot - 0.03), (a - 0.009, bot)]
        k.add(f"streak{i}", slab(strip, origin, ex, ez, 0.03), STREAK, "wing{s}", fuse=False, mirror=True,
              surface="shell")
    k.add("fork", slab(ribbon(FORK, 0.024), origin, ex, ez, 0.034), BOLT, "wing{s}", fuse=False, mirror=True, glow=BOLT)


def tunetor(src, out):
    k = Kit(src, coat="fur")
    body(k)
    head(k)
    wings(k)
    n.brows(k, FC, FR, EYE_D, EYE_HALF, CUT, BROW)

    def grimace(u, v):
        c = np.tile(MOUTH, (len(u), 1))
        c[(v > -0.05) & (v < 0.55) & (np.abs(u) < 0.82)] = TEETH
        c[(np.abs(u) < 0.06) & (v > -0.05)] = MOUTH
        return c
    n.mouth_frown(k, FC, FR, (0.0, -1.0, -0.4), (0.026, 0.014), grimace)
    # static bristling over the crown and the back
    for i, (d, length, c, r) in enumerate((((0.25, 0.1, 0.95), 0.09, (0.0, -0.04, 0.705), 0.098),
                                           ((-0.3, 0.05, 0.95), 0.085, (0.0, -0.04, 0.705), 0.098),
                                           ((0.6, 0.2, 0.75), 0.09, (0.12, 0.07, 0.645), 0.088),
                                           ((-0.6, 0.2, 0.75), 0.085, (-0.12, 0.07, 0.645), 0.088),
                                           ((0.05, 0.75, 0.65), 0.1, (0.0, 0.15, 0.56), 0.11),
                                           ((0.45, 0.6, 0.65), 0.08, (0.12, 0.07, 0.645), 0.088))):
        root, _ = n.on_ell(c, (r, r, r), d)
        n.spike_cluster(k, f"spike{i}", root, d, length, 0.024, SPIKE, "body" if i > 1 else "head")
    # a crackle at the crown's right, where the picture's lightning runs up off the head
    b = n.BILLOWS[2]
    p, nn = n.on_ell(HC + (np.array(b[0]) - n.C) * HS, (b[1] * HS,) * 3, (0.5, -0.7, 0.4))
    n.bolt(k, "crownbolt", p, nn, 0.055, BOLT, "head", 0.009, lean=0.5)
    k.build(voxel=VOXEL, smooth=4, band=0.008, wband=0.06)
    n.board(k)

    def eyes(state):
        n.lay_glower(k, FC, FR, EYE_D, EYE_HALF, CUT, FACE, state, look=EYE)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    tunetor(*args())
