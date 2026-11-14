# Pikachu sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pikachu.py -- <the export's Pikachu.glb> <out.glb>
#
# The design, from the references: grown from Pichu's script (its eyes, cheeks, nose, flat bolt of a tail): a yellow
# mouse, the head round and as wide as the plump pear of a body under it; long pointed ears leaning out, black over
# their last third; black eyes with a white glint; round red cheeks; a dot of a nose and a small open mouth with a
# pink tongue (the pictures show it open); short arms held forward; small feet; two brown stripes across the back;
# the tail a flat lightning bolt, brown where it leaves the body, widening to a broad end at the head's height.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

YELLOW, INK, RED, BROWN = "#F8D23A", "#1E1E26", "#E2392E", "#8A5428"
NOSE, MOUTH, TONGUE = "#2A2026", "#7E2430", "#EE7A86"
H = np.array([0, -0.01, 0.386])           # the head's centre
HR = np.array([0.142, 0.124, 0.112])


def on_head(d, lift=0.0):
    d = unit(*d)
    s = 1 / math.sqrt(((d / HR) ** 2).sum())
    return H + d * (s + lift)


def triangulate(poly):
    """Ear clipping of a simple polygon."""
    P = [np.array(p) for p in poly]
    area = sum(P[i][0] * P[(i + 1) % len(P)][1] - P[(i + 1) % len(P)][0] * P[i][1] for i in range(len(P)))
    idx = list(range(len(P)))
    if area < 0:
        idx.reverse()
    def cross(a, b, c):
        return (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])
    tris = []
    while len(idx) > 3:
        for j in range(len(idx)):
            a, b, c = idx[j - 1], idx[j], idx[(j + 1) % len(idx)]
            if cross(P[a], P[b], P[c]) <= 1e-12:
                continue
            if any(cross(P[a], P[b], P[q]) > 0 and cross(P[b], P[c], P[q]) > 0 and cross(P[c], P[a], P[q]) > 0
                   for q in idx if q not in (a, b, c)):
                continue
            tris.append((a, b, c)); idx.pop(j)
            break
        else:
            break
    tris.append(tuple(idx[:3]))
    return tris


def slab(poly, thick, O, A, B, off=0.0):
    """An outline in the plane (A, B) through O extruded thick along the plane's normal, its middle off along it."""
    A, B = np.array(A, float), np.array(B, float)
    N = np.cross(A, B); N /= np.linalg.norm(N)
    m = len(poly)
    if sum(poly[i][0] * poly[(i + 1) % m][1] - poly[(i + 1) % m][0] * poly[i][1] for i in range(m)) < 0:
        poly = poly[::-1]
    lo = [O + A * a + B * b + N * (off - thick / 2) for a, b in poly]
    hi = [O + A * a + B * b + N * (off + thick / 2) for a, b in poly]
    V = np.array(lo + hi)
    F = []
    for a, b, c in triangulate(poly):
        F.append([a, c, b]); F.append([m + a, m + b, m + c])
    for i in range(m):
        j = (i + 1) % m
        F.append([i, j, m + j, m + i])
    return V, F


def offset_outline(line, widths, longest=3.0):
    """A closed outline round a polyline, each point its half-width, its corners mitred to points."""
    line = np.array(line, float)
    n = len(line)
    seg = [(line[i + 1] - line[i]) / np.linalg.norm(line[i + 1] - line[i]) for i in range(n - 1)]
    left = lambda t: np.array([-t[1], t[0]])
    L, R = [], []
    for i in range(n):
        w = widths[i]
        if i == 0 or i == n - 1:
            t = seg[0] if i == 0 else seg[-1]
            m = left(t); kk = 1.0
            p = line[i] + (-t if i == 0 else t) * w * 0.3
        else:
            m = left(seg[i - 1]) + left(seg[i]); m /= np.linalg.norm(m)
            kk = min(1 / max(np.dot(m, left(seg[i])), 1e-3), longest)
            p = line[i]
        L.append(p + m * w * kk); R.append(p - m * w * kk)
    return [tuple(p) for p in L + R[::-1]]


# The head, and a plump pear of a body as wide under it
k.add("head", ell(H, HR), YELLOW, "head")
def pear(V):
    V = V.copy(); s = 1 - 0.13 * V[:, 2]
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("body", ell((0, 0.004, 0.178), (0.12, 0.108, 0.148), deform=pear), YELLOW, "body")
# No neck: the shoulders fill out up into the head's underside, so that the head flows into the body
k.add("chest", ell((0, 0.01, 0.288), (0.104, 0.07, 0.07)), YELLOW, "body")

# Short legs and small feet
k.add("thigh", ell((0.064, -0.004, 0.075), (0.05, 0.055, 0.056)), YELLOW, "leg{s}", mirror=True)
k.add("foot", ell((0.068, -0.05, 0.02), (0.032, 0.05, 0.02)), YELLOW, "leg{s}", mirror=True)

# Short arms held forward and a little out, rounded paws
k.add("arm", tube([(0.078, -0.03, 0.24), (0.112, -0.074, 0.222), (0.122, -0.108, 0.204)], [0.034, 0.03, 0.027]),
      YELLOW, "arm{s}", mirror=True)
k.add("paw", ell((0.124, -0.118, 0.2), (0.028, 0.028, 0.027)), YELLOW, "arm{s}", mirror=True)

# Long pointed ears leaning out, flat front to back, and black over their last third: a loose black tip a little
# fuller than the ear over the same line, so that its edge is crisp
EA = unit(0.52, 0.1, 0.86)
EO = np.array([0.066, 0.01, 0.462])
EL = 0.22
TS = np.round(np.arange(-0.1, 1.0001, 0.1), 3)
def ear_r(t):
    if t <= 0.45:
        return 0.03 + 0.014 * min(max(t, 0) / 0.4, 1)
    return 0.044 * (1 - ((t - 0.45) / 0.58) ** 1.6)
RS = [max(ear_r(t), 0.004) for t in TS]
PTS = [EO + EA * EL * t for t in TS]
k.add("ear", blade(PTS, RS, (0, 1, 0), 0.48), YELLOW, "ear{s}", mirror=True)
TIP = TS >= 0.695
k.add("eartip", blade([p for p, w in zip(PTS, TIP) if w], [r + 0.0028 for r, w in zip(RS, TIP) if w], (0, 1, 0), 0.5),
      INK, "ear{s}", fuse=False, mirror=True)

# Two brown stripes across the back, wide in the middle and tapering to the sides
# (patches cast from behind, each a lens narrower than the back, so that their edges are crisp)
LENS = polygon([(-1, 0), (-0.8, 0.45), (-0.4, 0.82), (0, 1), (0.4, 0.82), (0.8, 0.45), (1, 0),
                (0.8, -0.45), (0.4, -0.82), (0, -1), (-0.4, -0.82), (-0.8, -0.45)])
for zc, hw in ((0.252, 0.078), (0.196, 0.088)):
    k.decal((0, 0.1, zc), unit(0, 1, 0.06), (hw, 0.016), BROWN, eps=0.003, outline=LENS, rings=10, segs=64)

# The tail: a flat lightning bolt in a plane turned toward the front three-quarter, out behind to its right; brown
# where it leaves the body, then yellow, each zag wider than the last, to a broad end at the head's height
# (the game looks from the front three-quarter and from behind: the plane runs back and well out to its side, some
# 37 degrees off straight back; nearly straight back it was hidden behind the body from the front and edge-on from behind)
TO = np.array([0.016, 0.104, 0.15])
TU = np.array([0, 0, 1.0])
TD = unit(0.6, 0.8, 0)                    # seen face-on from the front three-quarter and well from behind
ZIG = [(0.0, 0.0), (0.04, 0.06), (0.005, 0.085), (0.1, 0.18), (0.05, 0.212), (0.2, 0.36)]
WID = [0.013, 0.018, 0.026, 0.036, 0.044, 0.07]
k.add("tailbase", slab(offset_outline(ZIG[:2] + [(0.03, 0.08)], [0.013, 0.018, 0.02]), 0.022, TO, TD, TU), BROWN,
      "tail", fuse=False)
k.add("tailbolt", slab(offset_outline(ZIG[1:], WID[1:], longest=2.6), 0.016, TO, TD, TU), YELLOW, "tail", fuse=False)
k.add("tailroot", ell(TO + np.array([-0.006, -0.012, 0.0]), (0.022, 0.022, 0.02)), YELLOW, "tail")

# The face: round red cheeks, a dot of a nose and a small open mouth with a pink tongue
dc = unit(0.82, -0.5, -0.24)
k.decal(on_head(dc), dc, (0.041, 0.039), RED, eps=0.003, rings=10, segs=40, mirror=True)
k.add("nose", ell(on_head((0, -1, -0.02), -0.002), (0.008, 0.005, 0.005)), NOSE, "head", fuse=False, surface="skin")
CY, CM, CT = srgb(YELLOW), srgb(MOUTH), srgb(TONGUE)
def mouth(u, v):
    c = np.tile(CM, (len(u), 1))
    c[(u / 0.62) ** 2 + ((v + 0.8) / 0.55) ** 2 < 1] = CT
    return c
def mouth_shape(th):
    s = math.sin(th)
    return min(1.0, 0.3 / s) if s > 0 else 1.0
dm = unit(0, -1, -0.34)
k.decal(on_head(dm), dm, (0.025, 0.024), mouth, eps=0.003, outline=mouth_shape, rings=20, segs=64)

# Black eyes with a white glint, as Pichu's but a little smaller against the head
CI = srgb(INK)
def eye(u, v):
    c = np.tile(CI, (len(u), 1))
    c[((u - 0.12) / 0.36) ** 2 + ((v - 0.36) / 0.32) ** 2 < 1] = (1, 1, 1)
    return c
de = unit(0.47, -0.87, 0.14)
E = on_head(de)
def eyes(state):
    k.decal(E, de, (0.023, 0.027), eye_look(state, eye, CY, CI), eps=0.004, rings=20, segs=56, mirror=True)

k.build(voxel=0.005)
k.eye_states(eyes)
k.export(out)
