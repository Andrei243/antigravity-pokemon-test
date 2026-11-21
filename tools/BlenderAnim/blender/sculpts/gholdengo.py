# Gholdengo sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gholdengo.py -- <the export's Gholdengo.glb> <out.glb>
#
# Grown from Gimmighoul's script (sibling("gimmighoul")): its small chest of gold bars and red panels, its black strap
# and its silver disc, which are the belt, the buckle and the chest at the hip.
# From the references: a figure of gold coins stacked into smooth tubes, banded with thin orange lines where they lie on
# each other. No head of its own: the body is one column, broad and pear-shaped at the hips, narrowing up to a neck and
# widening a little again to the face, its top cut flat and open (an orange coin end). On the column's top part two tall
# oval eyes outlined dark with a dark pupil, a lopsided open smile under them. Out of the open top four tubes of hair:
# one drooping to the left, three rising and curling to the right, each ending in an open coin end. Two thick stumpy legs
# splayed apart, flaring at the feet. Long arms thickening toward great three-fingered hands, each finger an open tube:
# one hand reaching forward and down (the art holds it out sideways: carried in), the other bent to its hip, resting on
# a small red chest belted there. A black strap round the hips, a silver coin for its buckle at the front left.
# (The task gave it "coiled hair like springs": the pictures show smooth curved tubes, banded, with open ends.)
# The skeleton: body (0, 0, 0.32), arms (+-0.075, 0, 0.42), head (0, 0, 0.46), legs (+-0.045, 0, 0.2).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, lathe, ring_band, srgb, inside, unit, eye_look, args, outward, sibling

g = sibling("gimmighoul")

GOLD, BAND = "#F2CF3A", "#DE7236"
END = "#E58A3E"                    # the inside of an open coin end
INK, MOUTH, TONGUE = "#4E2E18", "#7E3418", "#EA7E3C"
BLACK = g.BLACK

# --- the frame --------------------------------------------------------------------------------------------------------
COL_Z = [0.25, 0.31, 0.37, 0.42, 0.47, 0.51, 0.54]     # the column's centre line (x 0, y 0) and its radii
COL_R = [0.098, 0.083, 0.064, 0.055, 0.062, 0.068, 0.069]
TOP = 0.624
HIP_C, HIP_R = np.array([0.0, 0.004, 0.225]), np.array([0.112, 0.088, 0.098])
FACE_Z = 0.508
VOXEL = 0.0055


def col_r(z):
    return float(np.interp(z, COL_Z, COL_R))


def coin_end(k, name, c, d, r, bone, depth=0.012):
    """An open coin end: a short drum along d whose face is a gold rim round an orange hollow."""
    c = np.array(c, float); d = unit(*d)
    k.add(name, lathe(c, d, [(0, -depth), (r, -depth), (r, 0.004), (r * 0.74, 0.004), (r * 0.74, -0.003),
                             (0, -0.003)], seg=40), GOLD, bone, fuse=False, surface="metal")
    k.add(name + "in", lathe(c, d, [(0, -0.006), (r * 0.745, -0.006), (r * 0.745, -0.0015), (0, -0.0015)], seg=40),
          END, bone, fuse=False, surface="metal")


def band(k, name, c, d, r, bone, length=0.008):
    k.add(name, ring_band(c, d, r, length, thick=0.005), BAND, bone, fuse=False, surface="metal")


def lock(k, name, pts, r, bone="head"):
    """A tube of hair: a fused tube from inside the open top, banded at its middle, an open coin end at its tip."""
    P = np.array(pts, float)
    d = unit(*(P[-1] - P[-2]))
    end = P[-1]
    body = np.vstack([P[:-1], end - d * (r + 0.004)])
    k.add(name, tube(body, [r] * len(body)), GOLD, bone)
    coin_end(k, name + "end", end, d, r + 0.003, bone)
    m = len(P) // 2
    band(k, name + "band", P[m], P[m + 1] - P[m - 1], r - 0.002, bone)


def finger(k, name, root, d, length, r, bone):
    d = unit(*d)
    tip = np.array(root, float) + d * length
    k.add(name, tube([root, tip - d * (r + 0.004)], [r * 0.95, r]), GOLD, bone)
    coin_end(k, name + "end", tip, d, r + 0.0025, bone, depth=0.01)


def body(k):
    k.add("hips", ell(HIP_C, HIP_R), GOLD, "body")
    k.add("column", tube([(0, 0, z) for z in COL_Z], COL_R), GOLD, "body")
    coin_end(k, "top", (0, 0, TOP - 0.004), (0, 0, 1), COL_R[-1] + 0.003, "head", depth=0.055)
    for z in (0.33, 0.395, 0.455):
        band(k, f"colband{z}", (0, 0, z), (0, 0, 1), col_r(z) - 0.002, "body")
    band(k, "faceband", (0, 0, 0.555), (0, 0, 1), COL_R[-1] + 0.001, "head")


def legs(k):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"leg{side}", tube(np.array([(0.05, 0.0, 0.21), (0.068, -0.004, 0.13), (0.08, -0.008, 0.07)]) * m,
                                 [0.048, 0.046, 0.05]), GOLD, f"leg{side}")
        k.add(f"foot{side}", lathe(np.array([0.082, -0.009, 0.0]) * m, (0, 0, 1),
                                   [(0, 0.0), (0.06, 0.0), (0.057, 0.014), (0.05, 0.05), (0, 0.05)], seg=40),
              GOLD, f"leg{side}", fuse=False, surface="metal")
        band(k, f"legband{side}", np.array([0.067, -0.004, 0.135]) * m, np.array([0.18, -0.05, -1]) * m, 0.045,
             f"leg{side}")
        band(k, f"footband{side}", np.array([0.081, -0.009, 0.03]) * m, (0, 0, 1), 0.053, f"leg{side}")


def belt(k):
    """The black strap round the hips, lower at the front left, its seam behind; the silver buckle there."""
    tilt0 = -0.55                                           # where it hangs lowest (radians from straight ahead, +X)
    def zf(th):
        return 0.212 - 0.02 * np.cos(th - tilt0)
    def at(u, v):
        th = math.pi + 2 * math.pi * u
        z = zf(th) + (v - 0.5) * 0.034
        s = np.sqrt(np.clip(1 - ((z - HIP_C[2]) / HIP_R[2]) ** 2, 0.05, 1))
        return np.stack([HIP_C[0] + (HIP_R[0] * s + 0.004) * np.sin(th),
                         HIP_C[1] - (HIP_R[1] * s + 0.004) * np.cos(th), z], axis=-1)
    k.add("belt", outward(sheet(at, 97, 3, 0.009)), BLACK, "body", fuse=False, surface="leather")
    th = tilt0
    s = math.sqrt(1 - ((zf(th) - HIP_C[2]) / HIP_R[2]) ** 2)
    p = np.array([HIP_R[0] * s * math.sin(th), HIP_C[1] - HIP_R[1] * s * math.cos(th), zf(th)])
    n = unit(math.sin(th) / HIP_R[0], -math.cos(th) / HIP_R[1], 0.0)
    g.disc(k, "buckle", p + n * 0.012, n, 0.034, "body", thick=0.012)


def arms(k):
    # its right (-X): reaching forward and down, the great hand before the hips
    S, E, W = np.array([-0.055, 0.0, 0.43]), np.array([-0.125, -0.035, 0.37]), np.array([-0.112, -0.125, 0.3])
    k.add("armL", tube([S, (S + E) / 2 + (-0.01, 0, 0.01), E, (E + W) / 2 + (-0.012, 0, 0), W],
                       [0.024, 0.024, 0.026, 0.029, 0.032]), GOLD, "armL")
    H = np.array([-0.108, -0.16, 0.278])
    k.add("handL", ell(H, (0.04, 0.036, 0.032), rot=(30, 0, 0)), GOLD, "armL")
    for j, d in enumerate([(-0.45, -0.8, -0.55), (0.0, -0.85, -0.6), (0.45, -0.75, -0.6)]):
        finger(k, f"fingerL{j}", H + unit(*d) * 0.026, d, 0.05 if j != 1 else 0.06, 0.017, "armL")
    band(k, "armbandL", (S + E) / 2 + (-0.01, 0, 0.01), E - S, 0.022, "armL")
    band(k, "wristbandL", (E + W) / 2 + (-0.012, 0, 0), W - E, 0.027, "armL")
    # its left (+X): bent to the hip, the hand on the small chest
    S, E, W = np.array([0.055, 0.0, 0.43]), np.array([0.13, 0.025, 0.36]), np.array([0.145, 0.0, 0.29])
    k.add("armR", tube([S, (S + E) / 2 + (0.01, 0, 0.01), E, W], [0.024, 0.024, 0.026, 0.029]), GOLD, "armR")
    H = np.array([0.142, -0.014, 0.272])
    k.add("handR", ell(H, (0.034, 0.036, 0.024)), GOLD, "armR")
    for j, dx in enumerate((-0.022, 0.0, 0.022)):
        finger(k, f"fingerR{j}", H + (dx, -0.02, -0.004), (dx * 6, -0.55, -1), 0.038, 0.0145, "armR")
    band(k, "armbandR", (S + E) / 2 + (0.01, 0, 0.01), E - S, 0.022, "armR")
    # the small chest belted at the hip
    g.chest(k, (0.14, -0.005, 0.18), 0.03, 0.042, 0.06, 0.0065, "body")


def oval_eye(pupil_u):
    ci, cg = srgb(INK), srgb(GOLD)
    def look(u, v):
        c = np.tile(ci, (len(u), 1))
        c[u * u + v * v < 0.7 ** 2] = cg
        c[((u - pupil_u) / 0.3) ** 2 + ((v + 0.05) / 0.34) ** 2 < 1] = ci
        return c
    return look


EYE_X, EYE_HALF = 0.03, (0.0165, 0.027)


def on_col(x, z):
    r = col_r(z)
    p = np.array([x, -math.sqrt(r * r - x * x), z])
    return p, unit(x, p[1], 0.0)


def lay_eyes(k, state):
    for s in (1, -1):
        p, n = on_col(s * EYE_X, FACE_Z)
        # the pupils both a little to the model's left (+X), as the pictures have them; u runs to +X on both eyes
        look = eye_look(state, oval_eye(0.28), srgb(GOLD), srgb(INK), line=0.16)
        k.decal(p, n, EYE_HALF, look, up=(0, 0, 1), eps=0.0018, rings=20, segs=56)


def mouth(k):
    p, n = on_col(0.006, 0.47)
    def flat_top(th):
        sv = math.sin(th)
        return min(1.0, 0.3 / sv) if sv > 0.05 else 1.0
    def look(u, v):
        c = np.tile(srgb(MOUTH), (len(u), 1))
        c[((u - 0.15) / 0.5) ** 2 + ((v + 0.95) / 0.55) ** 2 < 1] = srgb(TONGUE)
        return c
    k.decal(p, n, (0.025, 0.017), look, up=(0, 0, 1), eps=0.0018, outline=flat_top, rings=16, segs=60)


def gholdengo(src, out_path):
    k = Kit(src, coat="metal")
    body(k)
    legs(k)
    belt(k)
    arms(k)
    lock(k, "lockL", [(-0.03, 0.0, 0.6), (-0.07, 0.0, 0.645), (-0.118, -0.006, 0.632), (-0.14, -0.01, 0.585),
                      (-0.142, -0.012, 0.54)], 0.024)
    lock(k, "lockUp", [(0.0, 0.006, 0.6), (0.0, 0.01, 0.66), (0.006, 0.012, 0.715), (0.02, 0.012, 0.75)], 0.024)
    lock(k, "lockR1", [(0.018, 0.008, 0.6), (0.035, 0.012, 0.665), (0.07, 0.014, 0.71), (0.11, 0.012, 0.725),
                       (0.14, 0.01, 0.71)], 0.023)
    lock(k, "lockR2", [(0.03, -0.004, 0.598), (0.072, -0.008, 0.632), (0.12, -0.01, 0.626), (0.152, -0.012, 0.595),
                       (0.162, -0.014, 0.55)], 0.023)
    mouth(k)
    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.04)
    k.eye_states(lambda state: lay_eyes(k, state))
    k.export(out_path)


if __name__ == "__main__":
    src, out_path = args()
    gholdengo(src, out_path)
