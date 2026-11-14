# Rotom sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rotom.py -- <the export's Rotom.glb> <out.glb>
#
# The design, from the references: a small round orange body of plasma drawn up at its top into a long spike leaning
# to its right (-X), with a small round knob under it; the whole wrapped in a pale cyan aura that shows as a rim round
# the ball and the spike and runs to a point under the knob; two arms of lightning, flat bolts of white panels edged
# in cyan, the -X one running down and out to a long point, the +X one zigzagging out to its side; large white eyes
# with a big blue iris and a glint, and a small sly smile. It hovers: the aura's point is its lowest place.
# The game cannot draw a see-through glow, so the aura is opaque cyan standing behind the orange (a ball and cones a
# little larger, set back), which leaves the face free and gives the silhouette its outline; from behind a loose copy
# of the ball and knob shows through it the same way. Every loose piece's faces were checked to turn outward.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="glow")

ORANGE, AURA, PANEL = "#F27028", "#9EDDF0", "#EEF6FA"
WHITE, IRIS, INK, MOUTH = "#FFFFFF", "#2468D6", "#1A2C5A", "#6A2614"
C = np.array([0.0, 0.0, 0.272])           # the ball's middle, a little over the body bone
R = np.array([0.1, 0.094, 0.096])
BACK = 0.034                              # how far the aura stands behind the orange


def on_ball(d, lift=0.0):
    d = unit(*d)
    s = 1 / math.sqrt(((d / R) ** 2).sum())
    return C + d * (s + lift)


# --- the orange core: the ball, its spike and the knob under it (fused, for the face's patches) ----------------
k.add("ball", ell(C, R, sub=5), ORANGE, "body")
SPB = C + np.array([-0.018, 0.0, 0.07])
SPT = C + np.array([-0.07, 0.0, 0.215])
SPM = (SPB + SPT) / 2 + np.array([0.004, 0, 0])
k.add("spike", tube([SPB, SPM, SPT], [0.054, 0.032, 0.008]), ORANGE, "body")
KN = C + np.array([0.016, -0.008, -0.112])
k.add("knobneck", ell(C + np.array([0.01, -0.004, -0.085]), (0.026, 0.024, 0.02)), ORANGE, "body")
k.add("knob", ell(KN, (0.034, 0.032, 0.03)), ORANGE, "body")

# --- the aura: opaque cyan a little larger than the core and set back, so it rims the orange from the front ----
AB = np.array([0, BACK, 0])
pieces = []
pieces.append(("auraball", ell(C + AB, R * 1.15, sub=5)))
ext = SPT + (SPT - SPB) / np.linalg.norm(SPT - SPB) * 0.022
pieces.append(("auraspike", tube([SPB + AB * 0.9 + (0, 0, -0.01), SPM + AB * 0.75, SPT + AB * 0.5, ext + AB * 0.3],
                                 [0.074, 0.048, 0.022, 0.003])))
LOW = np.array([0.012, BACK * 0.7, 0.056])
pieces.append(("auralow", tube([C + np.array([0.004, BACK, -0.06]), KN + np.array([0.0, BACK * 0.8, -0.01]),
                                (KN + LOW) / 2 + np.array([0.002, 0, -0.012]), LOW],
                               [0.056, 0.04, 0.018, 0.002])))
for name, (V, F) in pieces:
    k.add(name, (V, F), AURA, "body", fuse=False)

# From behind the core shows through the aura as it does from the front: the core again, mirrored front to back
# about the aura's middle, loose (a mirror turns the faces inside out, so they are turned back)
def behind(shape):
    V, F = shape
    V = V.copy(); V[:, 1] = 2 * BACK - V[:, 1]
    return V, [f[::-1] for f in F]
# (the ball and the knob only: a spike behind showed as a second horn from the front three-quarter)
for name, shape in (("backball", ell(C, R, sub=5)), ("backknob", ell(KN, (0.034, 0.032, 0.03)))):
    k.add(name, behind(shape), ORANGE, "body", fuse=False)


# --- the arms: flat bolts in the front's plane, white panels edged wide in cyan --------------------------------
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
    L, Rr = [], []
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
        L.append(p + m * w * kk); Rr.append(p - m * w * kk)
    return [tuple(p) for p in L + Rr[::-1]]


# Each bolt a zigzag centre line in (x, z) from the arm's joint, widening to its last zag and then to a point
O = np.array([0.0, 0.012, 0.0])
AX, AZ = (1.0, 0, 0), (0, 0, 1.0)
BOLTS = {
    "R": ([(0.075, 0.245), (0.186, 0.22), (0.114, 0.164), (0.21, 0.114)], [0.014, 0.019, 0.022, 0.004]),
    "L": ([(-0.07, 0.228), (-0.142, 0.145), (-0.08, 0.142), (-0.196, 0.072)], [0.014, 0.02, 0.022, 0.004]),
}
for s, (line, wid) in BOLTS.items():
    panel = offset_outline(line, wid, longest=2.6)
    edge = offset_outline(line, [w + 0.011 for w in wid], longest=2.6)
    for name, poly, thick, col in (("panel", panel, 0.016, PANEL), ("edge", edge, 0.009, AURA)):
        V, F = slab(poly, thick, O, AX, AZ)
        k.add(name + s, (V, F), col, "arm" + s, fuse=False)

# --- the face: a small sly smile under the eyes, a little to its left (+X) -----------------------------------
CO, CM = srgb(ORANGE), srgb(MOUTH)
def smile(u, v):
    c = np.tile(CO, (len(u), 1))
    c[np.abs(v - (0.95 * u * u - 0.32)) < 0.3] = CM
    return c
dm = unit(0.12, -1, -0.22)
k.decal(on_ball(dm), dm, (0.024, 0.014), smile, eps=0.003, outline=lambda th: 1.0, rings=14, segs=56)

k.build(voxel=0.004)

# Large white eyes with a big blue iris and a glint, a thin dark edge
CW, CI, CK = srgb(WHITE), srgb(IRIS), srgb(INK)
def eye(u, v):
    c = np.tile(CW, (len(u), 1))
    c[((u - 0.06) / 0.5) ** 2 + ((v + 0.02) / 0.68) ** 2 < 1] = CI
    c[((u - 0.06) / 0.2) ** 2 + ((v + 0.02) / 0.32) ** 2 < 1] = CK
    c[((u + 0.1) / 0.17) ** 2 + ((v - 0.3) / 0.17) ** 2 < 1] = CW
    c[np.sqrt(u * u + v * v) > 0.82] = CK
    return c
de = unit(0.44, -0.84, 0.24)
E = on_ball(de)
def eyes(state):
    k.decal(E, de, (0.031, 0.036), eye_look(state, eye, CO, CK, line=0.16), eps=0.004, rings=20, segs=56,
            up=(-0.18, 0, 1), mirror=True)

k.eye_states(eyes)
k.export(out)
