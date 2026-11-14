# Remoraid sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/remoraid.py -- <the export's Remoraid.glb> <out.glb>
#
# From the references: a plump, blunt-nosed fish, one even pale mint all over (no darker back), with two short dark
# teal stripes stacked lengthwise on the upper rear of each flank; a large round eye (white in a dark rim, a dark
# upright pupil) forward on the face; a wide mouth line across the blunt nose; a small cone of a spike on the top of
# the head; a great swept-back fin rising from the top of the back, a forked tail below and behind, small pale fins
# on the flanks and one under the chin. The fins are pale, near white, loose flat plates (finneon.py's), each with a
# grey vein; the midline ones are kept lower than the body is tall and swished 40 degrees to one side (feebas.py).
# The export's joints: body (0, 0, 0.21), head (0, -0.1, 0.21), dorsal (0, 0.02, 0.31), tail (0, 0.2, 0.21),
# tailFin (0, 0.27, 0.21), finR/L (+-0.08, -0.04, 0.15).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

MINT, BAND, FIN, VEIN, INK, MOUTH = "#A8D6C8", "#3F8274", "#E6F0EC", "#9FB8B0", "#141A1A", "#4C6E66"


# --- flat plates (finneon.py's) -------------------------------------------------------------------------------------
def smooth(pts, per=6):
    p = np.array(pts, float)
    n = len(p)
    o = []
    for i in range(n):
        p0, p1, p2, p3 = p[i - 1], p[i], p[(i + 1) % n], p[(i + 2) % n]
        for j in range(per):
            t = j / per
            o.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                            + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    return np.array(o)


def area(p):
    return 0.5 * np.sum(p[:, 0] * np.roll(p[:, 1], -1) - np.roll(p[:, 0], -1) * p[:, 1])


def earclip(p):
    idx = list(range(len(p)))
    tris = []
    def cross(o, a, b):
        return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    guard = 0
    while len(idx) > 3 and guard < 100000:
        guard += 1
        m = len(idx)
        cut = False
        for i in range(m):
            a, b, c = idx[i - 1], idx[i], idx[(i + 1) % m]
            if cross(p[a], p[b], p[c]) <= 1e-12:
                continue
            ok = True
            for j in idx:
                if j in (a, b, c):
                    continue
                q = p[j]
                if cross(p[a], p[b], q) >= 0 and cross(p[b], p[c], q) >= 0 and cross(p[c], p[a], q) >= 0:
                    ok = False
                    break
            if ok:
                tris.append((a, b, c)); idx.pop(i); cut = True
                break
        if not cut:
            tris.append((idx[-1], idx[0], idx[1])); idx.pop(0)
    tris.append(tuple(idx))
    return tris


def flat(outline, origin, ex, ez, thick):
    """A flat plate of a polygon (a, b in the plane of ex, ez about origin), thick about its middle plane."""
    ex, ez = unit(*ex), unit(*ez)
    nrm = np.cross(ex, ez); nrm /= np.linalg.norm(nrm)
    p = np.array(outline, float)
    if area(p) < 0:
        p = p[::-1]
    W = np.array(origin, float) + p[:, :1] * ex + p[:, 1:] * ez
    m = len(p)
    front, back = W + nrm * thick / 2, W - nrm * thick / 2
    V = [front, back]
    F = []
    for a, b, c in earclip(p):
        F.append([a, b, c]); F.append([m + c, m + b, m + a])
    off = 2 * m
    rim = []
    for i in range(m):
        j = (i + 1) % m
        rim.extend([front[i], back[i], back[j], front[j]])
        F.append([off + 4 * i, off + 4 * i + 1, off + 4 * i + 2, off + 4 * i + 3])
    V.append(np.array(rim))
    return np.vstack(V), F


def swish(shape, y0, deg, length):
    """A plate authored in the midline bent to the +X side along its length (feebas.py's)."""
    V, F = shape
    V = np.array(V, float).copy()
    d = V[:, 1] - y0
    th = math.radians(deg) * np.clip(d / length, 0, None) ** 1.1
    x = V[:, 0].copy()
    V[:, 0] = x * np.cos(th) + d * np.sin(th)
    V[:, 1] = y0 - x * np.sin(th) + d * np.cos(th)
    return V, F


def strip(a0, a1, w):
    """A thin vein from a0 to a1 in a plate's plane (a, b)."""
    a0, a1 = np.array(a0, float), np.array(a1, float)
    d = a1 - a0; n = np.array([-d[1], d[0]]) / np.linalg.norm(d) * w
    return [tuple(a0 + n), tuple(a1 + n * 0.4), tuple(a1 - n * 0.4), tuple(a0 - n)]


# --- the body: plump and blunt in front, narrowing into the tail's root --------------------------------------------
k.add("body", ell((0, 0.03, 0.21), (0.088, 0.14, 0.104)), MINT, "body")
k.add("head", ell((0, -0.055, 0.207), (0.086, 0.09, 0.098), deform=None), MINT, "head")
k.add("stalk", tube([(0, 0.11, 0.205), (0, 0.18, 0.2), (0, 0.235, 0.198)], [0.06, 0.04, 0.024]), MINT, "tail")
# A small cone of a spike on top of the head, leaning back
k.add("spike", tube([(0, -0.072, 0.285), (0, -0.064, 0.322), (0, -0.05, 0.352)], [0.03, 0.017, 0.004]), MINT, "head")


# Two short dark stripes stacked lengthwise on the upper rear of each flank
def band(yc, zc, a, b):
    def m(P):
        r = np.sqrt(((P[:, 1] - yc) / a) ** 2 + ((P[:, 2] - zc) / b) ** 2)
        return inside((r - 1) * b, 0.0025) * (np.abs(P[:, 0]) > 0.02)
    return m
k.paint(band(0.07, 0.276, 0.055, 0.0135), BAND)
k.paint(band(0.075, 0.232, 0.062, 0.0145), BAND)

# The mouth: one wide line across the blunt nose, its corners turned down
def mouth(u, v):
    c = np.tile(srgb(MINT), (len(u), 1))
    c[np.abs(v - (0.3 - 0.7 * u * u)) < 0.2] = srgb(MOUTH)
    return c
k.decal((0, -0.145, 0.188), unit(0, -1, -0.12), (0.05, 0.018), mouth, eps=0.004, rings=10, segs=48)

# Cheeks for the eyes, forward on the face and turned off straight ahead
CHEEK = np.array([0.045, -0.092, 0.222])
k.add("cheek", ell(CHEEK, (0.036, 0.036, 0.046), rot=(0, 0, -40)), MINT, "head", mirror=True)

# --- fins -----------------------------------------------------------------------------------------------------------
# The great fin swept back from the top of the back (lower than the body is tall), swished to one side
DORS = smooth([(0.0, 0.0), (0.03, 0.03), (0.07, 0.085), (0.12, 0.14), (0.175, 0.175), (0.15, 0.135),
               (0.13, 0.1), (0.1, 0.07), (0.08, 0.035), (0.075, -0.005)], per=5)
D0 = (0, 0.06, 0.29)
k.add("dorsal", swish(flat(DORS, D0, (0, 1, 0), (0, 0, 1), 0.01), 0.06, 40, 0.18), FIN, "dorsal", fuse=False)
k.add("dorsalvein", swish(flat(strip((0.02, 0.01), (0.15, 0.15), 0.004), (0.0, 0.06, 0.29), (0, 1, 0), (0, 0, 1),
                               0.014), 0.06, 40, 0.18), VEIN, "dorsal", fuse=False)

# The forked tail behind and below, swished the other way
TAIL = smooth([(0.0, 0.02), (0.05, 0.015), (0.1, -0.02), (0.15, -0.035), (0.13, -0.065), (0.155, -0.11),
               (0.11, -0.11), (0.06, -0.075), (0.02, -0.035), (-0.005, -0.01)], per=5)
T0 = (0, 0.215, 0.19)
k.add("tailfin", swish(flat(TAIL, T0, (0, 1, 0), (0, 0, 1), 0.01), 0.215, -40, 0.16), FIN, "tailFin", fuse=False)
k.add("tailvein", swish(flat(strip((0.02, -0.01), (0.12, -0.07), 0.004), T0, (0, 1, 0), (0, 0, 1), 0.014),
                        0.215, -40, 0.16), VEIN, "tailFin", fuse=False)

# Small pale fins low on the flanks, swept back and out, their faces toward the front
PEC = smooth([(0.0, 0.0), (0.03, 0.012), (0.075, 0.008), (0.09, -0.015), (0.06, -0.035), (0.02, -0.02)], per=5)
k.add("pec", flat(PEC, (0.072, -0.03, 0.16), unit(0.6, 0.75, -0.25), unit(0.2, 0.15, 1), 0.01), FIN, "fin{s}",
      fuse=False, mirror=True)
# A fin under the chin, raking down and back, turned to show its face
CHIN = smooth([(0.0, 0.0), (0.035, -0.01), (0.07, -0.05), (0.06, -0.08), (0.03, -0.065), (0.005, -0.03)], per=5)
k.add("chin", flat(CHIN, (0.0, -0.055, 0.12), unit(0.6, 0.8, 0), (0, 0, 1), 0.01), FIN, "body", fuse=False)

# --- the eyes: large and round, white in a dark rim, an upright dark pupil -------------------------------------------
CM, CK, CW = srgb(MINT), srgb(INK), np.array([0.97, 0.98, 0.97])
def eye(u, v):
    r2 = u * u + v * v
    c = np.tile(CW, (len(u), 1))
    c[((u + 0.08) / 0.32) ** 2 + ((v + 0.02) / 0.48) ** 2 < 1] = CK
    c[r2 > 0.78 ** 2] = CK
    return c
de = unit(0.85, -1.0, 0.08)
def eyes(state):
    k.decal(CHEEK + de * 0.034, de, (0.034, 0.036), eye_look(state, eye, CM, CK, line=0.16), eps=0.005, rings=20,
            segs=56, mirror=True)

k.build(voxel=0.004, smooth=4)
k.eye_states(eyes)
k.export(out)
