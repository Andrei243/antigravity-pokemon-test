# Pichu sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pichu.py -- <the export's Pichu.glb> <out.glb>
#
# The design, from the references: a tiny pale-yellow mouse whose round head is as wide as its body is tall and wider
# than it; two huge flat ears, broad leaves as long as the head is wide, standing up and out, black over their outer
# halves and along their outer edges, yellow at the root with a jagged edge between; large round black eyes with a
# white glint, set wide and a little below the head's middle; pink round cheeks low on the sides; a dot of a nose and
# a small smile; a black collar of points under the chin; short stubby arms out to the sides; short legs and oval
# feet; a short tail out to its right, a yellow stub and a small black zigzag bolt.
# Pikachu grows from this script: the same eyes, cheeks, flat bolts and loose-tipped ears.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

YELLOW, INK, PINK, NOSE, MOUTH = "#F7E163", "#1E2330", "#F29AA8", "#2A2228", "#5A2A2C"
H = np.array([0, -0.006, 0.372])          # the head's centre
HR = np.array([0.152, 0.128, 0.122])


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
        F.append([a, c, b]); F.append([m + a, m + b, m + c])     # the low cap faces -N, the high one +N
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


# The head: a broad round ball, a little wider than tall, and a small pear of a body under it
k.add("head", ell(H, HR), YELLOW, "head")
def pear(V):
    V = V.copy(); s = 1 - 0.14 * V[:, 2]
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("body", ell((0, -0.004, 0.152), (0.088, 0.082, 0.102), deform=pear), YELLOW, "body")
k.add("neck", ell((0, -0.004, 0.238), (0.07, 0.064, 0.04)), YELLOW, "body")

# Short legs and oval feet
k.add("thigh", ell((0.05, -0.012, 0.075), (0.04, 0.042, 0.05)), YELLOW, "leg{s}", mirror=True)
k.add("foot", ell((0.054, -0.036, 0.022), (0.034, 0.05, 0.022)), YELLOW, "leg{s}", mirror=True)

# Stubby arms out to the sides, a little down and forward
k.add("arm", tube([(0.06, -0.03, 0.205), (0.1, -0.046, 0.19), (0.135, -0.06, 0.176)], [0.03, 0.026, 0.024]),
      YELLOW, "arm{s}", mirror=True)
k.add("paw", ell((0.142, -0.064, 0.173), (0.027, 0.025, 0.025)), YELLOW, "arm{s}", mirror=True)

# The ears: flat broad leaves as long as the head is wide, up and out from the top of the head, their faces turned a
# little outward. A black plate the whole ear, and a thicker yellow plate in the same plane for the root, its top
# edge jagged and stopping short of the outer edge, so that the black rims the outer side and covers the tip
ET = math.radians(42)                     # the ear's lean out from upright
A = np.array([math.sin(ET), 0.03, math.cos(ET)]); A /= np.linalg.norm(A)
B0 = np.array([math.cos(ET), 0, -math.sin(ET)])   # across, toward the outer edge
N0 = np.array([0, -1.0, 0])
PH = math.radians(10)
B = math.cos(PH) * B0 - math.sin(PH) * N0
B -= A * np.dot(B, A); B /= np.linalg.norm(B)
EO = np.array([0.086, 0.012, 0.46])        # the ear's root, on the head's top beside its joint
# A broad kite: some 0.6 of the head's width across at its widest, a wide flat cut across the end and a sharp
# corner at its outer top
EAR = [(-0.035, -0.034), (0.0, -0.048), (0.1, -0.08), (0.2, -0.097), (0.268, -0.092), (0.288, -0.06),
       (0.308, 0.072), (0.29, 0.094), (0.2, 0.1), (0.1, 0.082), (0.03, 0.056), (-0.035, 0.034)]
# The yellow field: the inner half of the ear from the root to some two thirds along, its edge jagged in three
# points running down to the outer side, which stays rimmed in black
ROOT = [(-0.03, -0.028), (0.0, -0.042), (0.1, -0.073), (0.198, -0.089),         # along the inner edge, inset
        (0.158, -0.056), (0.214, -0.036), (0.15, -0.014), (0.19, 0.008),          # the jagged edge
        (0.115, 0.022), (0.138, 0.048), (0.06, 0.042), (0.0, 0.028), (-0.03, 0.024)]
k.add("ear", slab(EAR, 0.012, EO, A, B), INK, "ear{s}", fuse=False, mirror=True)
# The yellow lies on the ear's front alone (its normal A x B points back), so the backs of the ears are black
k.add("earroot", slab(ROOT, 0.004, EO, A, B, off=-0.0068), YELLOW, "ear{s}", fuse=False, mirror=True)
# The ears' roots thickened into the head, so that a plate does not stand on it as a blade
k.add("earbase", ell(EO - np.array([0, 0, 0.012]), (0.024, 0.016, 0.028), rot=(0, math.degrees(ET), 0)),
      YELLOW, "ear{s}", mirror=True)

# The tail: a short yellow stub out to its right and back, then a small black bolt, in a plane turned toward the
# front three-quarter so that its zigzag reads from there and from behind
TO = np.array([0.03, 0.072, 0.13])
TU = np.array([0, 0, 1.0])
TD = unit(0.72, 0.69, 0)
STUB = [(0.0, 0.0), (0.05, -0.012)]
BOLT = [(0.035, -0.008), (0.06, -0.016), (0.052, 0.022), (0.118, 0.014)]
k.add("tailstub", slab(offset_outline(STUB, [0.012, 0.011]), 0.016, TO, TD, TU), YELLOW, "tail", fuse=False)
k.add("tailbolt", slab(offset_outline(BOLT, [0.012, 0.014, 0.016, 0.018], longest=2.6), 0.013, TO, TD, TU), INK,
      "tail", fuse=False)
k.add("tailroot", ell(TO + np.array([0, -0.01, 0.0]), (0.02, 0.02, 0.018)), YELLOW, "tail")

# The black collar under the chin: a patch on the front of the neck, its lower edge hanging in four points; cast
# from a little below so that the chin does not catch it
ZIG = [(-1, 0.75), (1, 0.75), (1, -0.1), (0.75, -1), (0.5, -0.2), (0.25, -1.0), (0.0, -0.25), (-0.25, -1.0),
       (-0.5, -0.2), (-0.75, -1), (-1, -0.1)]
dk = unit(0, -1, -0.3)
k.decal(np.array([0, -0.074, 0.228]), dk, (0.064, 0.024), INK, eps=0.003, outline=polygon(ZIG), rings=14, segs=88)

# The face: round pink cheeks low on the sides, a dot of a nose and a small smile
dc = unit(0.84, -0.5, -0.2)
k.decal(on_head(dc), dc, (0.033, 0.031), PINK, eps=0.003, rings=10, segs=40, mirror=True)
k.add("nose", ell(on_head((0, -1, -0.12), -0.002), (0.0075, 0.005, 0.005)), NOSE, "head", fuse=False, surface="skin")
CY, CM = srgb(YELLOW), srgb(MOUTH)
def smile(u, v):
    c = np.tile(CY, (len(u), 1))
    c[(np.abs(v - (-0.35 + 0.8 * u * u)) < 0.3) & (np.abs(u) < 0.85)] = CM
    return c
dm = unit(0, -1, -0.32)
k.decal(on_head(dm), dm, (0.026, 0.013), smile, eps=0.003, rings=8, segs=40)

# Large round black eyes with a white glint, set wide and a little low
CI = srgb(INK)
def eye(u, v):
    c = np.tile(CI, (len(u), 1))
    c[((u - 0.12) / 0.36) ** 2 + ((v - 0.36) / 0.32) ** 2 < 1] = (1, 1, 1)
    return c
de = unit(0.5, -0.86, 0.02)
E = on_head(de)
def eyes(state):
    k.decal(E, de, (0.029, 0.034), eye_look(state, eye, CY, CI), eps=0.004, rings=20, segs=56, mirror=True)

k.build(voxel=0.005)
k.eye_states(eyes)
k.export(out)
