# Bunnelby sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/bunnelby.py -- <the export's Bunnelby.glb> <out.glb>
#
# From the references: a small grey rabbit standing up, its two great ears rising up and a little out from the head's
# top, as long as the rest of it, narrow at the root and widening into a broad flat spoon whose top third is brown and
# ends in three rounded lobes like a shovel or a hand; a pink lining low in each ear's front. A round grey head; big
# round white eyes with a dark rim and dark pupils, set close; a tiny pink nose; two big white buck teeth hanging over a
# brown ragged collar of fur round the neck; short pale spikes of fur at the cheeks. A small pear of a body, the fore
# paws held together at the chest; great haunches with brown dirt spots on one, brown feet; a small grey tuft of a tail.
# The export's joints: body (0, 0, 0.1), head (0, -0.01, 0.17), ears (+-0.025, -0.005, 0.25), arms (+-0.035, -0.02,
# 0.13), legs (+-0.03, 0, 0.06); no tail bone. About 0.46 tall.
# Written to be grown from (diggersby.py): the colours, the ear, the eyes' rim, the teeth and the nose are names and
# functions here.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, boxy, tri, eye_look, args

GREY, LIGHT, BROWN, DARK = "#A9A8AE", "#C9C8CE", "#7A4E33", "#5A3624"
PINK, NOSE, INK, WHITE = "#E9939F", "#E07D8C", "#1C1A1E", "#FAFAFA"


# --- the line's own parts, shared with Diggersby ---------------------------------------------------------------------
def turn(rot):
    return np.array(Euler([math.radians(a) for a in rot]).to_matrix())


def long_ear(k, base, length, width, thick, rot, bone, colour=GREY, tip=BROWN, tip_from=0.6, lobes=True):
    """A long flat ear on +X (mirrored) from base, turned by rot (degrees; x back, y out, z its face to the side):
    narrow at the root, a broad spoon above, its top tip_from of the way brown with three rounded lobes."""
    base = np.array(base, float)
    m = turn(rot)

    def spoon(V):
        V = boxy(2.6)(V)
        V = V.copy()
        t = (V[:, 2] + 1) / 2
        f = (0.36 + 0.82 * smoothstep(0.1, 0.7, t)) / 1.18
        V[:, 0] *= f
        return V
    top = 0.86 if lobes else 1.0
    C = base + m @ np.array([0, 0, length * top / 2])
    k.add("ear", ell(C, (width, thick, length * top / 2), rot=rot, deform=spoon), colour, bone, mirror=True)
    if lobes:
        # three rounded lobes along the spoon's top, like the fingers of a shovel of a hand
        for i, x in enumerate((-0.62, 0.0, 0.62)):
            c = base + m @ np.array([x * width, 0, length * (0.81 - 0.025 * abs(x))])
            k.add(f"earlobe{i}", ell(c, (width * 0.4, thick * 0.95, width * 0.44), rot=rot), tip, bone, mirror=True)
    k.add("earroot", tube([base - m @ np.array([0, 0, 0.012]), base + m @ np.array([0, 0, length * 0.14])],
                          [width * 0.4, width * 0.44]), colour, bone, mirror=True)
    mi = m.T

    def brown_tip(P):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        L = (Q - base) @ mi.T                       # into the ear's own frame
        s = L[:, 2] / length
        edge = tip_from + 0.03 * np.sin(L[:, 0] / width * 4.2 + 0.6)
        return inside(edge - s, 0.0025) * (np.abs(L[:, 1]) < thick * 2.5) * (np.abs(L[:, 0]) < width * 1.3) \
            * (Q[:, 0] > 0.004)
    k.paint(brown_tip, tip, on=["ear"])
    return base, m


def ear_lining(k, base, m, length, at, half, colour=PINK, thick=0.0):
    """A lining patch on the ear's front, at a share of its length."""
    n = m @ np.array([0, -1.0, 0])
    c = base + m @ np.array([0, 0, length * at]) + n * thick
    k.decal(c, n, half, colour, up=m @ np.array([0, 0, 1.0]), eps=0.0018, rings=12, segs=44, mirror=True)


def round_eye(white=WHITE, ink=INK, pupil=(-0.16, 0.0), pr=(0.33, 0.45), rim=0.835):
    """A big round white eye with a dark rim and a dark pupil a little inward, a white glint on it."""
    W, K = srgb(white), srgb(ink)
    def look(u, v):
        c = np.tile(W, (len(u), 1))
        c[u * u + v * v > rim * rim] = K
        c[((u - pupil[0]) / pr[0]) ** 2 + ((v - pupil[1]) / pr[1]) ** 2 < 1] = K
        c[((u - pupil[0] - 0.12) / 0.13) ** 2 + ((v - pupil[1] - 0.17) / 0.14) ** 2 < 1] = W
        return c
    return look


def teeth(k, c, half, gap, bone="head"):
    """Two big white buck teeth hanging side by side."""
    for s in (1, -1):
        k.add(f"tooth{'R' if s > 0 else 'L'}", ell((c[0] + s * gap, c[1], c[2]), half, deform=boxy(4), sub=3), WHITE,
              bone, fuse=False, surface="skin")


def nose(k, c, normal, half, colour=NOSE):
    k.decal(c, normal, half, colour, eps=0.0018, rings=6, segs=24)


def spikes(k, name, root, dirs, length, thick, colour, bone):
    """Short pointed tufts of fur from root along each direction (on +X; mirrored): loose flat-faced spikes."""
    root = np.array(root, float)
    for i, d in enumerate(dirs):
        d = unit(*d)
        a = np.cross(d, (0, 1.0, 0)); a /= np.linalg.norm(a)
        b = np.cross(d, a)
        w = thick
        pts = [root + a * w, root - a * w, root + b * w * 0.9, root - b * w * 0.9, root + d * length]
        k.add(f"{name}{i}", hull(pts), colour, bone, fuse=False, mirror=True)


# --- Bunnelby ----------------------------------------------------------------------------------------------------------
H = np.array([0.0, -0.012, 0.196])          # the head's centre
HR = np.array([0.058, 0.051, 0.051])


def on_head(d, lift=0.0):
    d = unit(*d)
    return H + d * (1 / math.sqrt(((d / HR) ** 2).sum()) + lift)


def bunnelby(src, out):
    k = Kit(src, coat="fur")

    # --- the round head, its cheeks full and low ------------------------------------------------------------------
    k.add("head", ell(H, HR, deform=boxy(2.2)), GREY, "head")
    k.add("cheek", ell(H + (0.028, -0.016, -0.02), (0.034, 0.034, 0.03)), GREY, "head", mirror=True)
    k.add("muzzle", ell(H + (0, -0.03, -0.026), (0.026, 0.024, 0.02)), GREY, "head")

    # --- a small pear of a body on great haunches -------------------------------------------------------------------
    def pear(V):
        V = V.copy(); s = 1 - 0.14 * V[:, 2]
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("torso", ell((0, 0.002, 0.098), (0.044, 0.039, 0.054), deform=pear), GREY, "body")
    k.add("neck", ell((0, -0.004, 0.142), (0.034, 0.031, 0.026)), GREY, "body")
    k.add("haunch", ell((0.034, 0.008, 0.055), (0.034, 0.044, 0.042)), GREY, "leg{s}", mirror=True)
    k.add("shin", tube([(0.038, 0.0, 0.04), (0.04, -0.01, 0.018)], [0.019, 0.017]), GREY, "leg{s}", mirror=True)
    k.add("foot", ell((0.041, -0.024, 0.012), (0.022, 0.04, 0.013)), BROWN, "leg{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 2] - 0.022, 0.0015) * (np.abs(P[:, 0]) > 0.012), BROWN, on=["foot", "shin"])

    # --- the fore paws held together at the chest: arms down and in, the paws before the belly ---------------------
    k.add("arm", tube([(0.034, -0.02, 0.13), (0.03, -0.04, 0.12), (0.014, -0.05, 0.113)], [0.014, 0.013, 0.012]),
          GREY, "arm{s}", mirror=True)
    k.add("paw", ell((0.011, -0.053, 0.108), (0.013, 0.012, 0.016)), GREY, "arm{s}", mirror=True)

    # --- a small grey tuft of a tail ---------------------------------------------------------------------------------
    k.add("tail", ell((0, 0.044, 0.06), (0.019, 0.022, 0.02), deform=boxy(1.8)), GREY, "body")

    # --- the brown ragged collar under the head: a ring, and its square flaps hanging down and out -------------------
    k.add("collar", ell((0, -0.006, 0.142), (0.047, 0.044, 0.013)), BROWN, "body")
    for i, a in enumerate(np.radians(np.linspace(-118, 118, 9))):
        d = np.array([math.sin(a), -math.cos(a), 0])
        side = np.array([math.cos(a), math.sin(a), 0])
        c0 = np.array([0, -0.006, 0.142]) + d * np.array([0.044, 0.041, 0])
        w = 0.011 if i % 2 else 0.009
        L = 0.022 if i % 2 else 0.017
        pts = [c0 + side * w + (0, 0, 0.006), c0 - side * w + (0, 0, 0.006), c0 - d * 0.012,
               c0 + side * w * 0.9 + d * 0.012 - (0, 0, L), c0 - side * w * 0.9 + d * 0.012 - (0, 0, L),
               c0 + d * 0.003 - (0, 0, L * 0.9)]
        k.add(f"flap{i}", hull(pts), BROWN, "body", fuse=False)

    # --- the great ears ----------------------------------------------------------------------------------------------
    EL = 0.24
    base, m = long_ear(k, (0.026, -0.004, 0.232), EL, 0.056, 0.0095, (-6, 19, 14), "ear{s}")

    # --- the face: pale spikes at the cheeks, a pink nose, the buck teeth -------------------------------------------
    spikes(k, "whisk", H + (0.052, -0.012, -0.016), [(1, -0.2, 0.35), (1, -0.15, -0.05), (1, -0.1, -0.45)], 0.024,
           0.011, LIGHT, "head")
    teeth(k, (0, -0.052, 0.153), (0.0068, 0.0032, 0.0095), 0.0072)
    nose(k, on_head((0, -1, -0.33)), unit(0, -1, -0.1), (0.0075, 0.0058))
    CG, CI = srgb(GREY), srgb(INK)

    def mouth(u, v):
        c = np.tile(CG, (len(u), 1))
        c[(np.abs(v - (-0.35 + 0.9 * (np.abs(u) - 0.42) ** 2 * 4)) < 0.28) & (np.abs(u) < 0.85)] = CI
        c[(np.abs(u) < 0.14) & (v > -0.3)] = CI
        return c
    k.decal((0, -0.06, 0.166), unit(0, -1, -0.3), (0.012, 0.0075), mouth, eps=0.0018, rings=10, segs=40)

    # --- the pink linings in the ears' fronts, the brown spots on one haunch ---------------------------------------
    ear_lining(k, base, m, EL, 0.38, (0.011, 0.04), thick=0.009)
    for c, r in (((0.064, 0.026, 0.06), 0.0085), ((0.066, 0.004, 0.04), 0.006), ((0.058, 0.04, 0.035), 0.005)):
        n = unit(1, 0.25, 0.1)
        k.decal(c, n, (r, r * 0.9), BROWN, eps=0.0018, rings=6, segs=24)

    k.build(voxel=0.0042, band=0.005, wband=0.03)

    # --- big round white eyes with a dark rim and pupil, close together ---------------------------------------------
    de = unit(0.36, -0.92, 0.16)
    E = on_head(de)

    def eyes(state):
        k.decal(E, de, (0.0175, 0.0205), eye_look(state, round_eye(), CG, CI), eps=0.002, rings=20, segs=56,
                mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    bunnelby(*args())
