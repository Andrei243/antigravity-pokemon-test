# Azurill sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/azurill.py -- <the export's Azurill.glb> <out.glb>
#
# The design, from the references: a blue ball of a body sitting on the great blue ball at the end of its tail,
# which is a little wider and squatter than the body; round ears on top, set apart, pink inside; two short stubby
# arms, the one on its left held up, the one on its right lower and forward; no feet to be seen; small worried black
# eyes, their top edge rising toward the middle; a small wavy pink mouth; a white spot on each cheek; the tail a flat
# zigzag, white edged with black, from the back of the body out to its right and down to the ball.
# Marill and Azumarill grow from this script: the same blue, pink ears, eyes, zigzag tail and its ball.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

BLUE, PINK, INK, WHITE, MOUTH = "#3AA6EC", "#F0708A", "#16161E", "#FFFFFF", "#D2788A"
B = np.array([0, -0.004, 0.285])          # the body's middle
BR = np.array([0.132, 0.122, 0.124])
T = np.array([0, 0.0, 0.094])             # the tail's ball, sat on
TR = np.array([0.142, 0.13, 0.094])


def on_body(d, lift=0.0):
    """The point of the body's surface along direction d from its middle, and lift off it."""
    d = unit(*d)
    s = 1 / math.sqrt(((d / BR) ** 2).sum())
    return B + d * (s + lift)


# The body, a ball a little fuller below, and the ball of the tail under it
def fuller(V):
    V = V.copy()
    s = 1 + 0.05 * np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("body", ell(B, BR, deform=fuller), BLUE, "head")
k.add("waist", ell((0, 0, 0.19), (0.09, 0.085, 0.04)), BLUE, "body")
k.add("ball", ell(T, TR), BLUE, "root")

# Round ears on top, set apart and leaning out, pink inside
EC = np.array([0.088, 0.008, 0.412])
ER = np.array([0.056, 0.026, 0.054])
k.add("ear", ell(EC, ER, rot=(0, 18, 0)), BLUE, "ear{s}", mirror=True)
k.add("earroot", tube([(0.05, 0.008, 0.36), EC + (-0.01, 0, -0.02)], [0.035, 0.03]), BLUE, "ear{s}", mirror=True)
def ear_lining(P):
    Q = np.stack([np.abs(P[:, 0]), P[:, 1], P[:, 2]], axis=1) - EC
    a = math.radians(-18)
    x = Q[:, 0] * math.cos(a) - Q[:, 2] * math.sin(a)
    z = Q[:, 0] * math.sin(a) + Q[:, 2] * math.cos(a)
    return inside(np.sqrt((x / 0.036) ** 2 + (z / 0.035) ** 2) - 1, 0.025) * (Q[:, 1] < -0.004)
k.paint(ear_lining, PINK, on=["ear"])

# Short stubby arms: its left (-X) held up and out, its right lower and forward
k.add("armL", tube([(-0.09, -0.03, 0.29), (-0.14, -0.045, 0.32), (-0.158, -0.055, 0.36)], [0.03, 0.026, 0.024]),
      BLUE, "armL")
k.add("armR", tube([(0.07, -0.07, 0.25), (0.08, -0.115, 0.225), (0.078, -0.13, 0.198)], [0.028, 0.025, 0.023]),
      BLUE, "armR")

# The tail: a flat zigzag, white edged with black, from the back of the body out to its right and down to the ball,
# its face turned to the front three-quarter so that it reads from there and from behind
O = np.array([0.03, 0.105, 0.19])
U = np.array([0, 0, 1.0])
D = unit(0.72, 0.69, 0)
N = np.cross(U, D)
# The bolt is a broad flat white slab whose outline is its zigzag centre line offset either side with sharp mitred
# corners (the star-like points), and a thinner black slab a little larger behind it in the same plane: from either
# face the black shows only as a thin rim round the white.
ZIG = [(-0.01, 0.08), (0.125, 0.105), (0.07, 0.045), (0.155, 0.025), (0.08, -0.03), (0.14, -0.075), (0.03, -0.08)]


def offset_outline(line, w, longest=3.2):
    """A closed outline round a polyline at half-width w, its corners mitred to points (capped at longest x w)."""
    line = np.array(line, float)
    n = len(line)
    seg = [(line[i + 1] - line[i]) / np.linalg.norm(line[i + 1] - line[i]) for i in range(n - 1)]
    left = lambda t: np.array([-t[1], t[0]])
    L, R = [], []
    for i in range(n):
        if i == 0 or i == n - 1:
            t = seg[0] if i == 0 else seg[-1]
            m = left(t); k_ = 1.0
            # square ends pushed a little past the line's end
            p = line[i] + (-t if i == 0 else t) * w * 0.4
        else:
            m = left(seg[i - 1]) + left(seg[i]); m /= np.linalg.norm(m)
            k_ = min(1 / max(np.dot(m, left(seg[i])), 1e-3), longest)
            p = line[i]
        L.append(p + m * w * k_); R.append(p - m * w * k_)
    return np.array(L + R[::-1])


def triangulate(poly):
    """Ear clipping of a simple polygon (counter-clockwise made first)."""
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


def slab(poly, thick, off=0.0):
    """The outline in the bolt's plane (d, u) extruded thick along N, its middle off along N."""
    m = len(poly)
    if sum(poly[i][0] * poly[(i + 1) % m][1] - poly[(i + 1) % m][0] * poly[i][1] for i in range(m)) < 0:
        poly = poly[::-1]
    front = [O + D * d + U * u + N * (off - thick / 2) for d, u in poly]
    back = [O + D * d + U * u + N * (off + thick / 2) for d, u in poly]
    V = np.array(front + back)
    F = []
    for a, b, c in triangulate(poly):
        F.append([a, b, c]); F.append([m + a, m + c, m + b])
    for i in range(m):
        j = (i + 1) % m
        F.append([i, m + i, m + j, j])
    # a counter-clockwise outline in (D, U) faces -N, the front cap's side; the sides turn out
    return V, F


W_OUT = offset_outline(ZIG, 0.016)
B_OUT = offset_outline(ZIG, 0.0205, longest=3.3)
k.add("tailface", slab([tuple(p) for p in W_OUT], 0.016), WHITE, "tail", fuse=False)
k.add("tailedge", slab([tuple(p) for p in B_OUT], 0.009), INK, "tail", fuse=False)

# White spots on the cheeks, low on the sides of the face
for s in (1, -1):
    d = unit(0.78 * s, -0.55, -0.12)
    k.decal(on_body(d), d, (0.024, 0.023), WHITE, eps=0.003, rings=10, segs=40)

# A small worried mouth, pink, a little wavy
CM, CB = srgb(MOUTH), srgb(BLUE)
def mouth(u, v):
    c = np.tile(CB, (len(u), 1))
    c[np.abs(v - 0.28 * np.sin(u * math.pi * 1.5)) < 0.5] = CM
    return c
dm = unit(0, -1, 0.15)
k.decal(on_body(dm), dm, (0.02, 0.008), mouth, eps=0.003, rings=12, segs=48,
        outline=lambda th: 1.0)

# Small black eyes with a glint, their top edge rising toward the middle: worried
CI, CW = srgb(INK), srgb(WHITE)
def eye(u, v):
    c = np.tile(CI, (len(u), 1))
    c[((u - 0.12) / 0.3) ** 2 + ((v - 0.15) / 0.3) ** 2 < 1] = CW
    c[v > 0.6 - 0.3 * u] = CB
    return c
de = unit(0.36, -0.88, 0.3)
E = on_body(de)
def eyes(state):
    k.decal(E, de, (0.02, 0.024), eye_look(state, eye, CB, CI, line=0.2), eps=0.004, rings=20, segs=56, mirror=True)

k.build(voxel=0.0045)
k.eye_states(eyes)
k.export(out)
