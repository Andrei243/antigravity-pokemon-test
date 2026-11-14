# Raichu sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/raichu.py -- <the export's Raichu.glb> <out.glb>
#
# The design, from the references, grown from Pikachu's script (its eyes, cheeks, nose, back stripes and flat bolt of
# a tail): an orange mouse, larger and longer-bodied than Pikachu, the round head sitting on the body with no neck and
# one smooth outline; long ears sweeping out and up in a curve, broad through the middle, brown with a yellow hollow
# in front, tapering to a thin tip that winds into a small spiral; black eyes with a white glint; yellow cheek circles; a dot of a nose and a small
# closed smile; a big cream belly; short arms with brown hands; brown feet; a long thin dark tail ending in a flat
# yellow lightning bolt.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

ORANGE, CREAM, BROWN, CHEEK = "#F2A436", "#FAEFD6", "#7A4826", "#F9D84A"
INK, NOSE, MOUTH, WIRE, BOLT = "#1E1E26", "#2A2026", "#5A2A22", "#3A3A42", "#F7D23E"
H = np.array([0, -0.012, 0.525])           # the head's centre
HR = np.array([0.144, 0.126, 0.118])


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


# The head, round and smooth, wider than Pikachu's against the body
k.add("head", ell(H, HR), ORANGE, "head")

# A longer, plumper pear of a body; no neck: the shoulders fill out up into the head's underside
def pear(V):
    V = V.copy(); s = 1 - 0.12 * V[:, 2]
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("body", ell((0, 0.01, 0.255), (0.142, 0.128, 0.18), deform=pear), ORANGE, "body")
k.add("chest", ell((0, 0.012, 0.405), (0.112, 0.085, 0.07)), ORANGE, "body")

# Short thick legs and long feet, brown below a crisp line
k.add("thigh", ell((0.078, -0.014, 0.115), (0.062, 0.066, 0.078)), ORANGE, "leg{s}", mirror=True)
k.add("foot", ell((0.086, -0.06, 0.026), (0.044, 0.074, 0.026)), ORANGE, "leg{s}", mirror=True)
k.paint(lambda P: inside(P[:, 2] - 0.048, 0.003) * inside(0.03 - np.abs(P[:, 0]), 0.003), BROWN)

# Short arms held out and a little down and forward, brown over the hands
k.add("arm", tube([(0.09, -0.03, 0.385), (0.15, -0.06, 0.355), (0.19, -0.085, 0.325)], [0.04, 0.034, 0.03]),
      ORANGE, "arm{s}", mirror=True)
k.add("paw", ell((0.2, -0.092, 0.318), (0.034, 0.032, 0.03)), ORANGE, "arm{s}", mirror=True)
k.paint(lambda P: inside(0.178 - np.abs(P[:, 0]), 0.003) * inside(np.abs(P[:, 2] - 0.33) - 0.08, 0.003), BROWN)

# The big cream belly, from under the chest to between the legs (crisp: no `on`, kept to the front)
def belly(P):
    e = (P[:, 0] / 0.102) ** 2 + ((P[:, 2] - 0.245) / 0.15) ** 2 - 1
    return inside(e * 0.05, 0.0025) * inside(P[:, 1] + 0.03, 0.01)
k.paint(belly, CREAM, on=["body", "chest"])

# Long ears sweeping out and up in a curve, flat front to back, broad through the middle and tapering to a thin tip
# that winds into a small spiral of its own: brown, with a yellow hollow painted on the front
EAR = np.array([(0.072, 0.012, 0.6), (0.11, 0.012, 0.675), (0.16, 0.014, 0.735), (0.215, 0.016, 0.77),
                (0.258, 0.018, 0.776), (0.284, 0.018, 0.762), (0.292, 0.018, 0.741), (0.281, 0.018, 0.726),
                (0.266, 0.018, 0.732), (0.268, 0.018, 0.746)])
ERS = [0.04, 0.068, 0.068, 0.05, 0.028, 0.014, 0.0095, 0.008, 0.007, 0.006]
k.add("ear", blade(EAR, ERS, (0, 1, 0), 0.4), BROWN, "ear{s}", mirror=True)
# the ear's middle line, densely, with its half-width, to paint the hollow by
_line, _rad, _t = [], [], []
for i in range(4):
    for s in np.linspace(0, 1, 30, endpoint=False):
        _line.append(EAR[i] + (EAR[i + 1] - EAR[i]) * s); _rad.append(ERS[i] + (ERS[i + 1] - ERS[i]) * s)
        _t.append(i + s)
_line, _rad, _t = np.array(_line), np.array(_rad), np.array(_t)
def lining(P):
    Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
    near = np.zeros(len(Q), int)
    sel = Q[:, 2] > 0.58
    D = ((Q[sel, None, :] - _line[None]) ** 2)
    near[sel] = np.argmin(D[:, :, 0] + D[:, :, 2], axis=1)
    c = _line[near]
    lat = np.hypot(Q[:, 0] - c[:, 0], Q[:, 2] - c[:, 2])
    m = inside(lat - 0.6 * _rad[near], 0.003) * inside(0.45 - _t[near], 0.004) * inside(_t[near] - 3.3, 0.004)
    return m * inside(Q[:, 1] - c[:, 1] + 0.004, 0.002) * sel
k.paint(lining, CHEEK)

# Brown stripes across the back, as Pikachu's (lens patches cast from behind)
LENS = polygon([(-1, 0), (-0.8, 0.45), (-0.4, 0.82), (0, 1), (0.4, 0.82), (0.8, 0.45), (1, 0),
                (0.8, -0.45), (0.4, -0.82), (0, -1), (-0.4, -0.82), (-0.8, -0.45)])
for zc, hw in ((0.35, 0.085), (0.285, 0.098)):
    k.decal((0, 0.12, zc), unit(0, 1, 0.06), (hw, 0.017), BROWN, eps=0.003, outline=LENS, rings=10, segs=64)

# The tail: a long thin dark wire out of the back, down and round behind to the right, kept close, and a flat yellow
# lightning bolt in a plane that runs back and out to the model's +X side (seen from the front three-quarter and from
# behind, as Pikachu's)
wire = [(0.0, 0.1, 0.2), (0.015, 0.16, 0.15), (0.05, 0.215, 0.1), (0.1, 0.25, 0.085), (0.14, 0.27, 0.11),
        (0.16, 0.28, 0.15)]
k.add("tailwire", tube(wire, [0.012, 0.011, 0.0105, 0.01, 0.0095, 0.009], seg=12), WIRE, "tail", fuse=False)
TO = np.array([0.155, 0.278, 0.145])
TD = unit(0.6, 0.8, 0)
BOLTP = [(0.0, -0.012), (0.06, -0.022), (0.09, 0.008), (0.23, -0.06), (0.105, 0.072), (0.07, 0.04),
         (0.018, 0.13), (-0.004, 0.012)]
BOLTP = [(1.25 * a, 1.25 * b) for a, b in BOLTP]           # the second signature: a quarter larger
k.add("tailbolt", slab(BOLTP, 0.016, TO, TD, (0, 0, 1.0)), BOLT, "tail", fuse=False)

# The face: flat yellow cheek circles on the round face, a dot of a nose and a small closed smile
dc = unit(0.72, -0.6, -0.3)
k.decal(on_head(dc), dc, (0.044, 0.042), CHEEK, eps=0.003, rings=10, segs=40, mirror=True)
k.add("nose", ell(on_head((0, -1, -0.04), -0.002), (0.009, 0.0055, 0.0055)), NOSE, "head", fuse=False,
      surface="skin")
CO, CM = srgb(ORANGE), srgb(MOUTH)
def smile(u, v):
    c = np.tile(CO, (len(u), 1))
    c[(np.abs(v - (-0.3 + 0.95 * u * u)) < 0.26) & (np.abs(u) < 0.9)] = CM
    return c
dm = unit(0, -1, -0.36)
k.decal(on_head(dm), dm, (0.036, 0.017), smile, eps=0.003, rings=18, segs=72)

# Black eyes with a white glint, as Pikachu's
CI = srgb(INK)
def eye(u, v):
    c = np.tile(CI, (len(u), 1))
    c[((u - 0.12) / 0.36) ** 2 + ((v - 0.36) / 0.32) ** 2 < 1] = (1, 1, 1)
    return c
de = unit(0.45, -0.88, 0.16)
E = on_head(de)
def eyes(state):
    k.decal(E, de, (0.024, 0.028), eye_look(state, eye, CO, CI), eps=0.004, rings=20, segs=56, mirror=True)

k.build(voxel=0.0055)
k.eye_states(eyes)
k.export(out)
