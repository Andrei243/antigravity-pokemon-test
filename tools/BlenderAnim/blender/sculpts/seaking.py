# Seaking sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/seaking.py -- <the export's Seaking.glb> <out.glb>
#
# Grown from goldeen.py (its plates, its ripples, its lips and its way of turning the eyes forward). From the
# references: a heavy round fish, orange-red over a white chin and throat whose edge runs back in a wave; a black band
# arching over the back behind the head and down the side, three black strokes between it and the eye, black spots
# behind it; a long tan horn leaning forward off the forehead, a darker ring at its root; big round eyes, white in a
# black rim with a large black pupil and a glint; thick pink pouting lips with two small white fangs in the lower
# one; and white fins dotted with black: a tall two-lobed dorsal, a broad tail of two round lobes, pectorals with
# wavy edges and two small fins hanging under the belly.
# The export's joints: body (0, 0, 0.347), head (0, -0.15, 0.347), dorsal (0, 0.04, 0.517), tail (0, 0.24, 0.347),
# tailFin (0, 0.33, 0.347), finR/L (+-0.1, -0.06, 0.257).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

ORANGE, WHITE, BLACK, FIN, SPOT = "#E85A32", "#F4F0EE", "#26222A", "#F2F0F2", "#2A262C"
HORN, HORNRING, LIPS, MOUTH, FANG, INK = "#E6D2A6", "#BFA678", "#EC7E9A", "#A8324A", "#FAFAF6", "#141418"


# --- flat plates (goldeen.py's) -------------------------------------------------------------------------------------
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
            if any(cross(p[a], p[b], p[j]) >= 0 and cross(p[b], p[c], p[j]) >= 0 and cross(p[c], p[a], p[j]) >= 0
                   for j in idx if j not in (a, b, c)):
                continue
            tris.append((a, b, c)); idx.pop(i); cut = True
            break
        if not cut:
            tris.append((idx[-1], idx[0], idx[1])); idx.pop(0)
    tris.append(tuple(idx))
    return tris


def flat(outline, origin, ex, ez, thick, sub=3):
    """A flat plate of a polygon (a, b in the plane of ex, ez about origin), thick about its middle plane. Its faces
    are split sub times, so that a plate bent afterwards (swish, billow) follows the bend: left as the outline's
    long triangles, it cut straight across the curve and buried the flecks and streaks laid on it."""
    ex, ez = unit(*ex), unit(*ez)
    nrm = np.cross(ex, ez); nrm /= np.linalg.norm(nrm)
    p = np.array(outline, float)
    if area(p) < 0:
        p = p[::-1]
    m = len(p)
    pts = [tuple(q) for q in p]
    mids = {}
    def mid(i, j):
        key = (min(i, j), max(i, j))
        if key not in mids:
            mids[key] = len(pts)
            pts.append(tuple((np.array(pts[i]) + np.array(pts[j])) / 2))
        return mids[key]
    tris = [tuple(t) for t in earclip(p)]
    for _ in range(sub):
        nt = []
        for a, b, c in tris:
            ab, bc, ca = mid(a, b), mid(b, c), mid(c, a)
            nt += [(a, ab, ca), (ab, b, bc), (ca, bc, c), (ab, bc, ca)]
        tris = nt
    q = np.array(pts)
    n = len(q)
    W = np.array(origin, float) + q[:, :1] * ex + q[:, 1:] * ez
    front, back = W + nrm * thick / 2, W - nrm * thick / 2
    F = []
    for a, b, c in tris:
        F.append([a, b, c]); F.append([n + c, n + b, n + a])
    rim = []
    for i in range(m):
        j = (i + 1) % m
        rim.extend([front[i], back[i], back[j], front[j]])
        F.append([2 * n + 4 * i, 2 * n + 4 * i + 1, 2 * n + 4 * i + 2, 2 * n + 4 * i + 3])
    return np.vstack([front, back, np.array(rim)]), F


def roll(shape, pivot, deg):
    V, F = shape
    V = np.array(V, float) - np.array(pivot, float)
    a = math.radians(deg)
    x, z = V[:, 0].copy(), V[:, 2].copy()
    V[:, 0] = x * math.cos(a) + z * math.sin(a)
    V[:, 2] = -x * math.sin(a) + z * math.cos(a)
    return V + np.array(pivot, float), F


def billow(shape, y0, amp, waves, length, zc):
    V, F = shape
    V = np.array(V, float).copy()
    d = np.clip(V[:, 1] - y0, 0, None) / length
    V[:, 0] += amp * d ** 1.3 * np.sin(waves * math.pi * (V[:, 2] - zc) / 0.25)
    return V, F


def swish(shape, y0, deg, length):
    V, F = shape
    V = np.array(V, float).copy()
    d = V[:, 1] - y0
    th = math.radians(deg) * np.clip(d / length, 0, None) ** 1.1
    x = V[:, 0].copy()
    V[:, 0] = x * np.cos(th) + d * np.sin(th)
    V[:, 1] = y0 - x * np.sin(th) + d * np.cos(th)
    return V, F


def spot(a, b, r, seed):
    """A black fleck on a fin: an irregular quad-ish blob in the fin's plane."""
    rs = np.random.RandomState(seed)
    n = 5
    th = np.sort(rs.rand(n) * 2 * math.pi)
    rr = r * (0.6 + 0.6 * rs.rand(n))
    s = 0.6 + 0.8 * rs.rand()
    return [(a + rr[i] * math.cos(th[i]) * s, b + rr[i] * math.sin(th[i]) / s) for i in range(n)]


# --- the body: round and heavy ------------------------------------------------------------------------------------
BC = np.array([0.0, 0.0, 0.347])


def taper(V):
    V = V.copy()
    y = V[:, 1]
    s = np.where(y > 0, 1 - 0.05 * y * y, 1.0)
    V[:, 0] *= s; V[:, 2] *= s
    return V


# A ball, as the references draw it, the tail fan set straight onto its back
k.add("body", ell((0, 0.01, 0.347), (0.14, 0.172, 0.158), deform=taper), ORANGE, "body")
HC = np.array([0.0, -0.05, 0.35])
k.add("head", ell(HC, (0.132, 0.13, 0.152)), ORANGE, "head")
k.add("stalk", tube([(0, 0.12, 0.35), (0, 0.17, 0.352), (0, 0.22, 0.354)], [0.12, 0.092, 0.05]), ORANGE, "tail")

# --- marks ----------------------------------------------------------------------------------------------------------
# The white chin and throat, its edge running back from under the lips in a wave and up toward the eye
def chin(Q):
    y, z = Q[:, 1], Q[:, 2]
    edge = 0.325 - 0.55 * (y + 0.17) + 0.012 * np.sin(y * 70 + 1.0)
    return inside(z - edge, 0.0025) * inside(y - 0.0, 0.003)
k.paint(chin, WHITE, on=["body", "head", "cheek", "stalk"])


def band_y(z):
    """The black band's middle along the body at a height: over the back, curving forward as it comes down."""
    return 0.035 - 0.35 * (0.5 - z) ** 1.4


def band(Q):
    y, z = Q[:, 1], Q[:, 2]
    zz = np.clip(z, 0.2, 0.6)
    w = 0.006 + 0.03 * smoothstep(0.24, 0.47, zz)
    return inside(np.abs(y - band_y(zz)) - w, 0.0025) * inside(0.255 - z, 0.003)
k.paint(band, BLACK)


def strokes(Q):
    """Three black strokes on the side between the eye and the band, slanting down toward the front."""
    q = np.stack([Q[:, 1], Q[:, 2]], axis=1)
    best = np.full(len(Q), 9.0)
    for zc, L in ((0.385, 0.045), (0.345, 0.055), (0.305, 0.045)):
        a = np.array([band_y(zc) - 0.022, zc]); b = a + np.array([-L, -0.016])
        e = b - a
        t = np.clip(((q - a) @ e) / (e @ e), 0, 1)
        d = np.linalg.norm(q - (a + t[:, None] * e), axis=1) - 0.009 * (1 - 0.5 * t)
        best = np.minimum(best, d)
    return inside(best, 0.0025) * (np.abs(Q[:, 0]) > 0.07)
k.paint(strokes, BLACK)


def blotch(c, r, seed, n=6):
    rs = np.random.RandomState(seed)
    c = np.array(c, float)
    N = rs.normal(size=(n, 3)); N[:, 0] *= 0.25
    N /= np.linalg.norm(N, axis=1, keepdims=True)
    d = r * (0.75 + 0.5 * rs.rand(n))
    def m(Q):
        Qm = np.concatenate([np.abs(Q[:, :1]), Q[:, 1:]], axis=1)
        x = Qm - c
        s = ((x @ N.T) / d[None]).max(1)
        return inside((s - 1) * r, 0.0025) * inside(np.linalg.norm(x, axis=1) - 2.2 * r, 0.003)
    return m
for i, (c, r) in enumerate([((0.11, 0.09, 0.4), 0.013), ((0.12, 0.08, 0.33), 0.011), ((0.105, 0.12, 0.29), 0.012),
                            ((0.08, 0.15, 0.37), 0.01)]):
    k.paint(blotch(c, r, 51 + i), BLACK)

# --- the lips: thick pink, pouting forward under the eyes, the mouth carved inside; two small fangs ----------------
AX = unit(0, -1, -0.05)
MC = np.array([0.0, -0.198, 0.318])
ex = np.array([1.0, 0, 0]); ez = np.cross(AX, ex); ez /= np.linalg.norm(ez)
if ez[2] < 0:
    ez = -ez
ring = [MC + 0.036 * math.cos(a) * ex + 0.021 * math.sin(a) * ez for a in np.linspace(0, 2 * math.pi * 1.08, 34)]
k.add("lips", tube(ring, [0.019] * len(ring), seg=16, per=4), LIPS, "head", surface="skin")
k.add("lipsfill", ell(MC - AX * 0.02, (0.045, 0.03, 0.035)), LIPS, "head", surface="skin")
k.add("mouth", ell(MC - AX * 0.005, (0.024, 0.032, 0.0075)), MOUTH, "head", carve=True)
k.add("fang", tube([(0.018, -0.222, 0.302), (0.019, -0.225, 0.322)], [0.0055, 0.001], seg=12), FANG, "head",
      fuse=False, mirror=True, surface="shell")

# --- the horn: tan, long, leaning forward off the forehead, a darker ring at its root -------------------------------
k.add("horn", tube([(0, -0.12, 0.47), (0, -0.152, 0.53), (0, -0.188, 0.59), (0, -0.205, 0.615)],
                   [0.033, 0.022, 0.009, 0.0015], seg=20), HORN, "head", fuse=False, surface="shell")
k.add("hornring", tube([(0, -0.117, 0.466), (0, -0.124, 0.479)], [0.036, 0.034], seg=20), HORNRING, "head",
      fuse=False, surface="shell")

# --- the fins: white, dotted black ----------------------------------------------------------------------------------
# The tail: a broad fan of two round lobes, the upper the larger; lower than the body is tall, rippled, bent aside
TAIL = smooth([(-0.01, 0.035), (0.05, 0.06), (0.12, 0.105), (0.19, 0.14), (0.24, 0.135), (0.26, 0.1),
               (0.235, 0.06), (0.19, 0.02), (0.215, -0.02), (0.235, -0.065), (0.215, -0.1), (0.17, -0.11),
               (0.11, -0.08), (0.05, -0.05), (-0.01, -0.03)], per=5)
T0 = np.array([0.0, 0.215, 0.352])
TSPOTS = [(0.17, 0.09, 0.011), (0.215, 0.11, 0.008), (0.13, 0.05, 0.008), (0.2, 0.05, 0.009), (0.17, -0.07, 0.01),
          (0.12, -0.04, 0.007), (0.08, 0.06, 0.007)]
def tailplate(outline, thick):
    sh = flat(outline, T0, (0, 1, 0), (0, 0, 1), thick)
    return swish(billow(roll(sh, T0, 12), 0.215, 0.012, 2.5, 0.26, 0.352), 0.215, 40, 0.24)
k.add("tailfin", tailplate(TAIL, 0.01), FIN, "tailFin", fuse=False)
for i, (a, b, r) in enumerate(TSPOTS):
    k.add(f"tailspot{i}", tailplate(spot(a, b, r * 1.5, 70 + i), 0.0145), SPOT, "tailFin", fuse=False)

# The dorsal: a broad low fan of two rounded lobes lying back along the curve of the back, leant well over to one
# side and bent the other way, so that from the front and from behind its face shows (upright, it read as a second
# horn in the game)
DORS = smooth([(-0.04, -0.012), (-0.01, 0.04), (0.04, 0.083), (0.09, 0.105), (0.13, 0.09), (0.17, 0.105),
               (0.21, 0.085), (0.24, 0.048), (0.22, 0.01), (0.17, -0.02), (0.08, -0.025)], per=5)
D0 = np.array([0.0, -0.035, 0.49])
DSPOTS = [(0.06, 0.07, 0.009), (0.15, 0.07, 0.009), (0.2, 0.05, 0.008), (0.1, 0.02, 0.007)]
def dorsplate(outline, thick):
    sh = flat(outline, D0, unit(0, 1, -0.38), unit(0, 0.38, 1), thick)
    return swish(roll(sh, D0, -42), -0.035, -28, 0.24)
k.add("dorsal", dorsplate(DORS, 0.01), FIN, "dorsal", fuse=False)
for i, (a, b, r) in enumerate(DSPOTS):
    k.add(f"dorsalspot{i}", dorsplate(spot(a, b, r * 1.5, 80 + i), 0.0145), SPOT, "dorsal", fuse=False)

# The pectorals: out, down and back, their faces toward the front, wavy along the trailing edge
_n = unit(0.45, -0.85, 0.12)
_x = np.array(unit(0.62, 0.32, -0.7)); _x = _x - _x.dot(_n) * _n; _x /= np.linalg.norm(_x)
FO, FX, FZ = np.array([0.105, -0.06, 0.262]), tuple(_x), tuple(np.cross(_n, _x))
PEC = smooth([(-0.01, 0.03), (0.05, 0.045), (0.1, 0.06), (0.15, 0.07), (0.15, 0.035), (0.17, 0.005),
              (0.14, -0.02), (0.15, -0.05), (0.1, -0.05), (0.05, -0.035), (-0.01, -0.02)], per=5)
for i, (a, b, r) in enumerate([(0.1, 0.035, 0.009), (0.13, -0.02, 0.008), (0.06, -0.01, 0.007)]):
    k.add(f"pecspot{i}", flat(spot(a, b, r * 1.5, 90 + i), FO, FX, FZ, 0.0145), SPOT, "fin{s}", fuse=False, mirror=True)
k.add("pec", flat(PEC, FO, FX, FZ, 0.01), FIN, "fin{s}", fuse=False, mirror=True)

# Two small fins under the belly, short, swept back and out and lying flat-ish against it (hanging straight down they
# read as two poles from behind)
VEN = smooth([(-0.01, 0.015), (0.03, 0.02), (0.07, 0.026), (0.08, 0.0), (0.068, -0.022), (0.03, -0.018),
              (-0.01, -0.012)], per=5)
VO = np.array([0.06, 0.05, 0.218])
VX = unit(0.5, 0.78, -0.38)
VZ = np.array(unit(-0.8, 0.5, 0.15)); VZ = VZ - VZ.dot(VX) * np.array(VX); VZ = tuple(VZ / np.linalg.norm(VZ))
k.add("ven", flat(VEN, VO, VX, VZ, 0.01), FIN, "body", fuse=False, mirror=True)
k.add("venspot", flat(spot(0.06, 0.005, 0.012, 99), VO, VX, VZ, 0.0145), SPOT, "body", fuse=False, mirror=True)

# --- the eyes: big and round, white in a black rim, a large black pupil and a glint ---------------------------------
CO, CW, CK = srgb(ORANGE), srgb("#FFFFFF"), srgb(INK)
def eye(u, v):
    r2 = u * u + v * v
    c = np.tile(CW, (len(u), 1))
    c[((u + 0.03) / 0.5) ** 2 + ((v + 0.02) / 0.52) ** 2 < 1] = CK
    c[((u + 0.2) / 0.16) ** 2 + ((v - 0.18) / 0.16) ** 2 < 1] = CW
    c[r2 > 0.8 ** 2] = CK
    return c
de = unit(0.85, -1.0, 0.18)
CHEEK = np.array([0.07, -0.118, 0.39])
k.add("cheek", ell(CHEEK, (0.05, 0.05, 0.06), rot=(0, 0, -40)), ORANGE, "head", mirror=True)
def eyes(state):
    k.decal(CHEEK + de * 0.044, de, (0.044, 0.046), eye_look(state, eye, CO, CK, line=0.15), eps=0.005, rings=20,
            segs=56, mirror=True)

k.build(voxel=0.0065, smooth=4)
k.eye_states(eyes)
k.export(out)
