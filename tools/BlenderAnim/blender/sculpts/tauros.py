# Tauros sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tauros.py -- <the export's Tauros.glb> <out.glb>
#
# From the references: a tan bull, a deep barrel on thick legs, its head carried low in front of a great shaggy mane
# that wraps its neck, shoulders and chest and ends ragged half way along its back (dark grey-brown, between the art's
# grey and the 3D picture's brown). Two grey horns curving out and up from the head's sides; three light grey studs on
# the brow (one over two); a dark muzzle; small angry eyes. Dark brown forelegs under the mane (the art's; the hind legs
# tan), blue-grey hooves; three thin dark whip tails, each ending in a blue-grey tuft, here raised up and fanned to the
# sides rather than trailing far back, so that the bull stays as short as it is tall.
# Reference ratios (3D picture, without the tails): head (horn tips to chin) 0.45 of the height, trunk depth 0.42 and
# leg showing under it 0.42 of the height, length nose to rump 1.0 of the height, the mane's top 1.0.
# The export's joints: body (0, 0.04, 0.44), head (0, -0.3, 0.5), tail (0, 0.31, 0.5), legs (+-0.11, -0.2, 0.38),
# hind (+-0.11, 0.24, 0.38).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, lumpy, hull, outward, polygon, args

src, out = args()
k = Kit(src, coat="fur")

HIDE, MANE, SHIN, HOOF = "#D8A262", "#5E4E46", "#6E4A3A", "#6C78A8"
HORN, STUD, NOSE, WHIP, TUFT = "#AEB3C6", "#C9CCD8", "#5C4038", "#3A2C28", "#6C78A8"
WHITE, INK = "#F6F4EE", "#1C1614"
H = np.array([0.0, -0.4, 0.47])         # the head's centre
HR = np.array([0.107, 0.127, 0.109])

# --- the body: a deep barrel, a chest under the mane, a round rump --------------------------------------------------
k.add("barrel", ell((0, 0.05, 0.46), (0.16, 0.3, 0.175)), HIDE, "body")
k.add("belly", ell((0, 0.0, 0.36), (0.14, 0.22, 0.08)), HIDE, "body")
k.add("chest", ell((0, -0.19, 0.46), (0.165, 0.14, 0.2)), HIDE, "body")
k.add("rump", ell((0, 0.25, 0.49), (0.145, 0.12, 0.15)), HIDE, "body")

# --- the head: low and forward, a broad brow, a muzzle with a dark nose ---------------------------------------------
k.add("neck", tube([(0, -0.18, 0.53), (0, -0.3, 0.5), (0, -0.37, 0.48)], [0.11, 0.09, 0.08]), HIDE, "head")
k.add("head", ell(H, HR), HIDE, "head")
k.add("muzzle", ell((0, -0.515, 0.4), (0.075, 0.077, 0.068), rot=(25, 0, 0)), HIDE, "head")
k.paint(lambda P: inside((P[:, 1] + 0.54) + 0.45 * (P[:, 2] - 0.42), 0.003) * (P[:, 1] < -0.4), NOSE)
# two nostrils
k.decal((0.028, -0.587, 0.395), unit(0.2, -1, -0.2), (0.011, 0.008), "#2C1E1A", eps=0.003, rings=4, segs=20, mirror=True)

# --- the mane: a lumpy collar round neck, shoulders and chest, ragged behind ------------------------------------------
k.add("mane", ell((0, -0.16, 0.54), (0.19, 0.17, 0.205), deform=lumpy(7, 5, 0.09)), MANE, "body")
k.add("manetop", ell((0, -0.24, 0.6), (0.14, 0.12, 0.13), deform=lumpy(6, 4, 0.1)), MANE, "head")
# the shaggy fringe: a few fused tufts along the back edge, under the chest and over the brow
for i, (pts, rr, ax) in enumerate([
        ([(0.0, -0.02, 0.66), (0.0, 0.06, 0.66), (0.0, 0.13, 0.63)], [0.08, 0.05, 0.012], (1, 0, 0)),
        ([(0.07, -0.25, 0.4), (0.075, -0.27, 0.33), (0.07, -0.27, 0.27)], [0.065, 0.045, 0.012], (0, 1, 0)),
        ([(0.0, -0.27, 0.4), (0.0, -0.29, 0.32), (0.0, -0.28, 0.25)], [0.065, 0.045, 0.012], (0, 1, 0)),
        ([(0.0, -0.33, 0.6), (0.0, -0.38, 0.6), (0.0, -0.42, 0.575)], [0.06, 0.04, 0.012], (0, 0, 1))]):
    k.add(f"shag{i}", blade(pts, rr, ax, 0.55), MANE, "body" if i < 3 else "head", mirror=pts[0][0] > 0)

# --- horns: grey, out from the head's sides, curving up and a little forward ------------------------------------------
HP = [(0.055, -0.375, 0.52), (0.12, -0.375, 0.545), (0.165, -0.385, 0.6), (0.165, -0.41, 0.655), (0.14, -0.43, 0.685)]
k.add("horn", outward(tube(HP, [0.036, 0.032, 0.026, 0.017, 0.006])), HORN, "head", fuse=False, mirror=True,
      surface="shell")
# three studs on the brow, one over two
for x, z, r in ((0.0, 1.05, 0.015), (0.3, 0.64, 0.015), (-0.3, 0.64, 0.015)):
    d = unit(x, -0.8, z)
    n = d / HR ** 2; n /= np.linalg.norm(n)
    c = H + d / np.linalg.norm(d / HR)
    k.add(f"stud{x}", ell(c - n * r * 0.35, (r, r, r), sub=3), STUD, "head", fuse=False, surface="shell")

# --- legs: broad shoulder and thigh masses fused over the upper half, the lower legs tapering to the hooves at a slant --
LEGS = (("fore", "leg{s}", [(0.1, -0.2, 0.42), (0.104, -0.215, 0.27), (0.106, -0.235, 0.15), (0.107, -0.25, 0.045)]),
        ("hind", "hind{s}", [(0.1, 0.26, 0.42), (0.104, 0.3, 0.27), (0.106, 0.31, 0.17), (0.107, 0.28, 0.045)]))
for name, bone, pts in LEGS:
    k.add(name, tube(pts, [0.08, 0.05, 0.031, 0.033]), HIDE, bone, mirror=True)
    x, y = pts[-1][0], pts[-1][1]
    k.add(name + "hoof", ell((x, y - 0.01, 0.03), (0.04, 0.046, 0.031),
                            deform=lambda V: V * (1 + 0.12 * np.clip(-V[:, 2:3], 0, 1))), HOOF, bone, mirror=True,
          surface="shell")
k.add("shoulder", ell((0.1, -0.205, 0.37), (0.088, 0.115, 0.16)), HIDE, "leg{s}", mirror=True)
k.add("thigh", ell((0.098, 0.265, 0.37), (0.088, 0.135, 0.165)), HIDE, "hind{s}", mirror=True)
# the forelegs dark brown below the mane (the art's)
k.paint(lambda P: inside(P[:, 2] - 0.27, 0.004) * (P[:, 1] < -0.1) * (np.abs(P[:, 0]) > 0.04),
        SHIN, on=["foreR", "foreL"])

# --- three whip tails, raised and fanned to the sides, each ending in a solid blue-grey tuft --------------------------
TB = np.array([0.0, 0.36, 0.56])
TAILS = [
    [TB, (0.01, 0.39, 0.64), (0.03, 0.395, 0.7), (0.06, 0.375, 0.76)],
    [TB, (0.05, 0.385, 0.61), (0.13, 0.39, 0.67), (0.2, 0.37, 0.71)],
    [TB, (-0.04, 0.385, 0.61), (-0.1, 0.39, 0.66), (-0.15, 0.37, 0.71)],
]
for i, pts in enumerate(TAILS):
    pts = np.array(pts, float)
    k.add(f"whip{i}", outward(tube(pts, [0.012, 0.011, 0.01, 0.01], seg=10)), WHIP, "tail", fuse=False,
          surface="hair")
    tip = pts[-1]
    d = pts[-1] - pts[-2]; d /= np.linalg.norm(d)
    a = np.cross(d, (0, 1.0, 0)); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    for j, (sa, sb) in enumerate(((0.0, 0.0), (0.7, 0.3), (-0.7, -0.3))):
        dd = d + a * sa * 0.5 + b * sb * 0.5; dd /= np.linalg.norm(dd)
        w = 0.026 if j == 0 else 0.018
        L = 0.085 if j == 0 else 0.06
        base = tip - d * 0.015
        k.add(f"tuft{i}{j}", hull([base + a * w, base - a * w, base + b * w * 0.7, base - b * w * 0.7,
                                   base + dd * L * 0.45 + a * w * 0.9, base + dd * L * 0.45 - a * w * 0.9,
                                   base + dd * L]), TUFT, "tail", fuse=False, surface="hair")

k.build(voxel=0.007, band=0.004)

# --- the eyes: small and angry, white with a dark pupil, cut along the top by the brow slanting to the nose ---------
CH, CW, CK = srgb(HIDE), srgb(WHITE), srgb(INK)


def eye(u, v):
    c = np.tile(CW, (len(u), 1))
    c[((u + 0.15) / 0.42) ** 2 + ((v + 0.1) / 0.6) ** 2 < 1] = CK
    c[u * u + v * v > 0.74] = CK
    return c


# u toward the back on a sideways patch: the inner (nose) corner is at -u; the top slants down toward the nose
SHAPE = [(-1.0, -0.15), (-0.3, 0.2), (1.0, 0.75), (1.0, -0.3), (0.4, -0.7), (-0.5, -0.6)]
de = unit(0.72, -0.62, 0.25)
E = H + np.array([0, -0.04, 0.02]) + de * 0.075


def eyes(state):
    col = eye_look(state, eye, CH, CK, line=0.15)
    k.decal(E, de, (0.034, 0.028), col, eps=0.005, rings=20, segs=56, outline=polygon(SHAPE))
    m = np.array([-1, 1, 1.0])
    k.decal(E * m, de * m, (0.034, 0.028), lambda u, v: col(-u, v), eps=0.005, rings=20, segs=56,
            outline=polygon([(-a, b) for a, b in SHAPE]))


k.eye_states(eyes)
k.export(out)
