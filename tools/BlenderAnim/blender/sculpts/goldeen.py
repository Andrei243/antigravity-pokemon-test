# Goldeen sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/goldeen.py -- <the export's Goldeen.glb> <out.glb>
#
# From the references: a round white goldfish, a little longer than it is tall, with irregular orange-red patches (a
# cap over the brow and the heavy eyelids, splashes on the back and at the fins' roots); a pale ivory cone of a horn on
# the forehead, leaning a touch forward; heavy-lidded eyes, a turquoise iris and a black pupil under an orange lid;
# pink lips pouting forward in an O; and long billowing white fins like a gown, wavy at their edges and streaked
# orange at their roots: a broad tail of two lobes, a dorsal sail and two large pectorals. The fins are loose flat
# plates (magikarp.py's), the midline ones kept lower than the body is tall, rolled and bent to one side (feebas.py).
# The export's joints: body (0, 0, 0.299), head (0, -0.12, 0.299), dorsal (0, 0.04, 0.439), tail (0, 0.2, 0.299),
# tailFin (0, 0.28, 0.299), finR/L (+-0.08, -0.06, 0.239).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

WHITE, ORANGE, FIN, HORN = "#F2EEEE", "#E8603A", "#F4F0F2", "#E8E0D2"
LIPS, MOUTH, IRIS, INK = "#F09AAE", "#A8324A", "#3AB6B8", "#16181C"


# --- flat plates (magikarp.py's) ------------------------------------------------------------------------------------
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


def lens(a0, a1, width, bulge=0.5, n=20):
    a0, a1 = np.array(a0, float), np.array(a1, float)
    d = a1 - a0; L = np.linalg.norm(d); d /= L
    side = np.array([-d[1], d[0]])
    e = math.log(0.5) / math.log(bulge)
    up = [a0 + d * L * (i / (n - 1)) + side * width * np.sin(np.pi * (i / (n - 1)) ** e) ** 0.7 for i in range(n)]
    dn = [a0 + d * L * (i / (n - 1)) - side * width * np.sin(np.pi * (i / (n - 1)) ** e) ** 0.7
          for i in range(n - 2, 0, -1)]
    return np.array(up + dn)


def roll(shape, pivot, deg):
    """A plate turned about the body's long axis (y) through pivot, so its face shows from in front and behind."""
    V, F = shape
    V = np.array(V, float) - np.array(pivot, float)
    a = math.radians(deg)
    x, z = V[:, 0].copy(), V[:, 2].copy()
    V[:, 0] = x * math.cos(a) + z * math.sin(a)
    V[:, 2] = -x * math.sin(a) + z * math.cos(a)
    return V + np.array(pivot, float), F


def billow(shape, y0, amp, waves, length):
    """Soft folds across a midline plate, growing toward its far end: a gown's ripples."""
    V, F = shape
    V = np.array(V, float).copy()
    d = np.clip(V[:, 1] - y0, 0, None) / length
    V[:, 0] += amp * d ** 1.3 * np.sin(waves * math.pi * (V[:, 2] - 0.299) / 0.2)
    return V, F


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


# --- the body: a round goldfish, longer than tall, tapering into the tail's root -----------------------------------
BC = np.array([0.0, 0.0, 0.299])


def taper(V):
    V = V.copy()
    y = V[:, 1]
    s = np.where(y > 0, 1 - 0.28 * y * y, 1.0)
    V[:, 0] *= s; V[:, 2] *= s
    return V


k.add("body", ell(BC, (0.09, 0.14, 0.112), deform=taper), WHITE, "body")
HC = np.array([0.0, -0.058, 0.302])
k.add("head", ell(HC, (0.088, 0.095, 0.108)), WHITE, "head")
k.add("stalk", tube([(0, 0.11, 0.3), (0, 0.2, 0.302), (0, 0.268, 0.304)], [0.07, 0.038, 0.022]), WHITE, "tail")


# --- the orange patches: irregular blotches, mirrored on both sides -------------------------------------------------
def blotch(c, r, seed, n=6, flat_x=0.3):
    rs = np.random.RandomState(seed)
    c = np.array(c, float)
    N = rs.normal(size=(n, 3)); N[:, 0] *= flat_x
    N /= np.linalg.norm(N, axis=1, keepdims=True)
    d = r * (0.75 + 0.5 * rs.rand(n))
    def m(Q):
        Qm = np.concatenate([np.abs(Q[:, :1]), Q[:, 1:]], axis=1)
        x = Qm - c
        s = ((x @ N.T) / d[None]).max(1)
        return inside((s - 1) * r, 0.0025) * inside(np.linalg.norm(x, axis=1) - 2.2 * r, 0.003)
    return m


# The cap over the brow and the eyelids: everything over a wavy line across the top of the head, from the horn back
def cap(Q):
    y, z = Q[:, 1], Q[:, 2]
    edge = 0.355 + 0.012 * np.sin(Q[:, 0] * 55 + 1.0) + 0.006 * np.sin(y * 70)
    return (inside(edge - z, 0.0025) * inside(-0.112 - y + 0.008 * np.sin(Q[:, 0] * 60), 0.003)
            * inside(y - (-0.03 + 0.012 * np.sin(Q[:, 0] * 40)), 0.003))
k.paint(cap, ORANGE)
BLOTS = [((0.045, 0.06, 0.38), 0.03), ((0.06, 0.13, 0.33), 0.02), ((0.03, 0.215, 0.33), 0.014)]
for i, (c, r) in enumerate(BLOTS):
    k.paint(blotch(c, r, 31 + i), ORANGE)

# --- the lips: a pink ring pouting forward, the mouth an O carved inside it ----------------------------------------
AX = unit(0, -1, -0.08)
MC = np.array([0.0, -0.163, 0.278])
ex = np.array([1.0, 0, 0]); ez = np.cross(AX, ex); ez /= np.linalg.norm(ez)
if ez[2] < 0:
    ez = -ez
ring = [MC + 0.021 * math.cos(a) * ex + 0.017 * math.sin(a) * ez for a in np.linspace(0, 2 * math.pi * 1.08, 30)]
k.add("lips", tube(ring, [0.012] * len(ring), seg=16, per=4), LIPS, "head", surface="skin")
k.add("lipsfill", ell(MC - AX * 0.016, (0.034, 0.024, 0.03)), LIPS, "head", surface="skin")
k.add("mouth", ell(MC - AX * 0.004, (0.013, 0.03, 0.008)), MOUTH, "head", carve=True)

# --- the horn: a pale ivory cone on the forehead, leaning a little forward ------------------------------------------
k.add("horn", tube([(0, -0.072, 0.37), (0, -0.088, 0.43), (0, -0.104, 0.49), (0, -0.112, 0.52)],
                   [0.026, 0.017, 0.007, 0.0015], seg=20), HORN, "head", fuse=False, surface="shell")

# --- the fins --------------------------------------------------------------------------------------------------------
# The tail, the design's glory: a wide billowing gown of three scalloped lobes streaming back, as long as the body and
# taller than it, orange running out from its root; rippled, rolled and bent to one side so its face shows from the
# front three-quarter and its lobes from behind (it was 60% of the body in the game and read as one more fin)
TAIL = smooth([(-0.01, 0.035), (0.05, 0.065), (0.11, 0.11), (0.17, 0.145), (0.23, 0.155), (0.275, 0.13),
               (0.26, 0.095), (0.3, 0.07), (0.285, 0.035), (0.235, 0.02), (0.29, -0.005), (0.305, -0.04),
               (0.27, -0.07), (0.28, -0.1), (0.24, -0.125), (0.18, -0.12), (0.11, -0.085), (0.05, -0.05),
               (-0.01, -0.03)], per=5)
T0 = np.array([0.0, 0.25, 0.302])
TS = 1.1
def tailplate(outline, thick):
    sh = flat(np.array(outline) * TS, T0, (0, 1, 0), (0, 0, 1), thick)
    return swish(billow(roll(sh, T0, 22), 0.25, 0.022, 3.0, 0.32), 0.25, 42, 0.3)
k.add("tailfin", tailplate(TAIL, 0.009), FIN, "tailFin", fuse=False)
for i, (a0, a1, w) in enumerate([((-0.005, 0.012), (0.2, 0.1), 0.032), ((-0.005, 0.0), (0.21, 0.01), 0.022),
                                 ((-0.005, -0.01), (0.18, -0.08), 0.026)]):
    k.add(f"tailstreak{i}", tailplate(lens(a0, a1, w, bulge=0.3), 0.0135), ORANGE, "tailFin", fuse=False)

# The dorsal sail: rising from the back and streaming back, wavy along its top; rolled and bent the other way
DORS = smooth([(-0.035, -0.01), (-0.01, 0.04), (0.03, 0.08), (0.07, 0.1), (0.1, 0.088), (0.13, 0.098),
               (0.16, 0.068), (0.15, 0.04), (0.17, 0.012), (0.12, -0.012), (0.05, -0.018)], per=5)
D0 = np.array([0.0, -0.005, 0.388])
def dorsplate(outline, thick):
    return swish(roll(flat(outline, D0, (0, 1, 0), (0, 0, 1), thick), D0, -14), -0.005, -38, 0.17)
k.add("dorsal", dorsplate(DORS, 0.009), FIN, "dorsal", fuse=False)
k.add("dorsalstreak", dorsplate(lens((-0.01, 0.005), (0.11, 0.06), 0.02, bulge=0.35), 0.0135), ORANGE, "dorsal",
      fuse=False)

# The pectorals: large white sails swept out, down and back, their faces toward the front, wavy at the trailing
# edge, orange at the root
_n = unit(0.45, -0.85, 0.12)
_x = np.array(unit(0.62, 0.32, -0.7)); _x = _x - _x.dot(_n) * _n; _x /= np.linalg.norm(_x)
FO, FX, FZ = np.array([0.078, -0.06, 0.245]), tuple(_x), tuple(np.cross(_n, _x))
PEC = smooth([(-0.01, 0.03), (0.06, 0.045), (0.12, 0.06), (0.175, 0.07), (0.17, 0.035), (0.19, 0.005),
              (0.16, -0.022), (0.17, -0.055), (0.12, -0.055), (0.06, -0.035), (-0.01, -0.02)], per=5)
k.add("pec", flat(PEC, FO, FX, FZ, 0.009), FIN, "fin{s}", fuse=False, mirror=True)
k.add("pecstreak", flat(lens((-0.01, 0.004), (0.12, 0.022), 0.015, bulge=0.3), FO, FX, FZ, 0.0135), ORANGE,
      "fin{s}", fuse=False, mirror=True)

# --- the eyes: heavy-lidded, a turquoise iris and a black pupil under an orange lid ---------------------------------
CW, CO, CI, CK = srgb("#FFFFFF"), srgb(ORANGE), srgb(IRIS), srgb(INK)
LIDV = 0.18                 # where the heavy lid's edge crosses the eye
def eye(u, v):
    c = np.tile(CW, (len(u), 1))
    c[((u + 0.05) / 0.5) ** 2 + ((v + 0.05) / 0.55) ** 2 < 1] = CI
    c[((u + 0.05) / 0.25) ** 2 + ((v + 0.05) / 0.28) ** 2 < 1] = CK
    c[((u + 0.2) / 0.13) ** 2 + ((v - 0.05) / 0.13) ** 2 < 1] = CW
    c[u * u + v * v > 0.86 ** 2] = CK
    lid = LIDV - 0.08 * u * u
    c[v > lid] = CO
    c[np.abs(v - lid) < 0.13] = CK
    return c


def look(state):
    base = eye_look(state, eye, srgb(WHITE), CK, line=0.16)
    def f(u, v):
        c = base(u, v)
        if state in ("Shut", "Smile", "Squeeze"):
            c[v > 0.42] = CO            # the orange lid stays over the eye's top whatever it does
        return c
    return f


# Cheeks forward on the face, turned about 40 degrees off straight ahead, so the eyes read from the front three-quarter
de = unit(0.85, -1.0, 0.12)
CHEEK = np.array([0.048, -0.098, 0.318])
k.add("cheek", ell(CHEEK, (0.04, 0.04, 0.05), rot=(0, 0, -40)), WHITE, "head", mirror=True)
def eyes(state):
    k.decal(CHEEK + de * 0.036, de, (0.03, 0.032), look(state), eps=0.005, rings=20, segs=56, mirror=True)

k.build(voxel=0.0055, smooth=4)
k.eye_states(eyes)
k.export(out)
