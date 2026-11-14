# Feebas sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/feebas.py -- <the export's Feebas.glb> <out.glb>
# A shabby fish: an egg of a body, flat at the sides and tilted (round at the top behind, narrowing to the mouth low in
# front), tan with darker irregular blotches and a seam round it behind the eye; a big blank eye (white in a thick
# black rim, a dot of a pupil); thick pink lips pouting forward; ragged blue fins: a tall dorsal split like a tuning
# fork at its top, a combed fan of a tail, and small combed fins below. The fins are loose flat plates (beautifly.py).
# The export's joints: body (0, 0, 0.214), head (0, -0.06, 0.214), dorsal (0, 0.04, 0.334), tail (0, 0.15, 0.214),
# tailFin (0, 0.2, 0.214), finR/L (+-0.08, -0.04, 0.134).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

TAN, BLOTCH, SEAM = "#CDB385", "#A08259", "#80694A"
FIN, LIP, LIP2 = "#86A8D6", "#EC8A9C", "#DC7488"


# --- flat plates (beautifly.py) ------------------------------------------------------------------------------------
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
                tris.append((a, b, c))
                idx.pop(i)
                cut = True
                break
        if not cut:
            tris.append((idx[-1], idx[0], idx[1])); idx.pop(0)
    tris.append(tuple(idx))
    return tris


def flat(outline, origin, ex, ez, thick):
    """A flat plate of a polygon (a, b in the plane of ex, ez about origin), with sharp edges."""
    ex, ez = unit(*ex), unit(*ez)
    nrm = np.cross(ex, ez); nrm /= np.linalg.norm(nrm)
    p = np.array(outline, float)
    if area(p) < 0:
        p = p[::-1]
    o = np.array(origin, float)
    W = o + p[:, :1] * ex + p[:, 1:] * ez
    m = len(p)
    front, back = W + nrm * thick / 2, W - nrm * thick / 2
    V = [front, back]
    F = []
    for a, b, c in earclip(p):
        F.append([a, b, c])
        F.append([m + c, m + b, m + a])
    off = 2 * m
    rim = []
    for i in range(m):
        j = (i + 1) % m
        rim.extend([front[i], back[i], back[j], front[j]])
        F.append([off + 4 * i, off + 4 * i + 1, off + 4 * i + 2, off + 4 * i + 3])
    V.append(np.array(rim))
    return np.vstack(V), F


def comb(n, a0, a1, lengths, slit, gap=0.32, root=0.012):
    """A ragged fan of n rays from a root, angles a0..a1 (degrees, in the plate's plane), each ray its own length,
    with slits between them cut down to slit; the root a short stub across."""
    span = (a1 - a0) / n
    pts = [(root * math.cos(math.radians(a0 - 90)), root * math.sin(math.radians(a0 - 90)))]
    for i in range(n):
        lo, hi = a0 + span * i, a0 + span * (i + 1)
        g = span * gap / 2
        b0, b1 = (lo + g if i else lo), (hi - g if i < n - 1 else hi)
        R = lengths[i]
        for a in np.linspace(b0, b1, 4):
            # each ray's end cut a little slanted: tattered
            r = R * (1 - 0.06 * (a - b0) / max(b1 - b0, 1e-6) * (1 if i % 2 else -1))
            pts.append((r * math.cos(math.radians(a)), r * math.sin(math.radians(a))))
        if i < n - 1:
            pts.append((slit * math.cos(math.radians(hi)), slit * math.sin(math.radians(hi))))
    pts.append((root * math.cos(math.radians(a1 + 90)), root * math.sin(math.radians(a1 + 90))))
    return pts


# --- the body: a tilted egg, flat at the sides --------------------------------------------------------------------
def egg(V):
    V = V.copy()
    V[:, :2] *= (1 + 0.12 * V[:, 2:3])
    return V
BC = np.array([0.0, 0.0, 0.214])
k.add("body", ell(BC, (0.078, 0.118, 0.136), rot=(-20, 0, 0), deform=egg), TAN, "body")
# the face's half, on the head
k.add("face", ell((0, -0.045, 0.2), (0.074, 0.075, 0.118), rot=(-20, 0, 0)), TAN, "head")
# a little fullness at the tail's root
k.add("tailroot", ell((0, 0.09, 0.2), (0.05, 0.05, 0.075)), TAN, "tail")


# Blotches: convex patches of darker brown, each cut by a few planes at random turns (irregular polygons)
def blotch(c, r, seed, n=6):
    rs = np.random.RandomState(seed)
    c = np.array(c, float)
    N = rs.normal(size=(n, 3)); N[:, 0] *= 0.25            # the cuts run across the side, not into it
    N /= np.linalg.norm(N, axis=1, keepdims=True)
    d = r * (0.75 + 0.5 * rs.rand(n))
    def m(Q):
        A = np.abs(Q[:, :1])
        Qm = np.concatenate([A, Q[:, 1:]], axis=1)
        x = Qm - c
        s = ((x @ N.T) / d[None]).max(1)
        return inside((s - 1) * r, 0.003) * inside(np.linalg.norm(x, axis=1) - 2.2 * r, 0.003)
    return m


BLOTCHES = [((0.065, -0.07, 0.305), 0.022), ((0.05, -0.105, 0.265), 0.017), ((0.06, -0.115, 0.19), 0.017),
            ((0.065, -0.085, 0.115), 0.014), ((0.06, -0.02, 0.33), 0.016),
            ((0.07, 0.035, 0.285), 0.02), ((0.072, 0.05, 0.225), 0.02), ((0.07, 0.04, 0.165), 0.018),
            ((0.05, 0.085, 0.31), 0.016), ((0.06, 0.09, 0.19), 0.015), ((0.04, 0.02, 0.345), 0.015)]
for i, (c, r) in enumerate(BLOTCHES):
    k.paint(blotch(c, r, 11 + i), BLOTCH)
# the seam round the body just behind the eye
SN = unit(0, 1, -0.08)
k.paint(lambda Q: inside(np.abs((Q - (0, 0.008, 0.214)) @ SN) - 0.0035, 0.002) * (Q[:, 2] > 0.09), SEAM)

# --- thick pink lips pouting forward, low on the face ---------------------------------------------------------------
def lip(z, y, r, droop):
    xs = np.linspace(-0.042, 0.042, 7)
    pts = [(x, y + 0.03 * (x / 0.042) ** 2, z - droop * (x / 0.042) ** 2) for x in xs]
    rad = [r * (0.7 + 0.3 * (1 - (x / 0.042) ** 2)) for x in xs]
    return tube(pts, rad)
k.add("lipup", lip(0.16, -0.124, 0.016, 0.006), LIP, "head", fuse=False, surface="shell")
k.add("liplo", lip(0.131, -0.122, 0.0165, -0.004), LIP2, "head", fuse=False, surface="shell")

# --- the fins: loose blue plates ------------------------------------------------------------------------------------
# The dorsal: a tall stem from the back, split at its top into two flat-topped prongs (a tuning fork)
DORS = [(-0.011, -0.02), (0.013, -0.02), (0.016, 0.03), (0.042, 0.15), (0.017, 0.156), (0.002, 0.112),
        (-0.014, 0.158), (-0.042, 0.152), (-0.013, 0.03)]
# Lower than drawn (it stood nearly as tall as the body) and turned 40 degrees about the vertical, leaning a touch to
# the side, so that from the front and from behind it shows its face as a fork, not its edge as a pole
DORS = [(a * 1.15, b * 0.62) for a, b in DORS]
_yaw, _lean = math.radians(40), math.radians(12)
k.add("dorsal", flat(DORS, (0, 0.025, 0.338), (math.sin(_yaw), math.cos(_yaw), 0),
                     (math.sin(_lean) * math.cos(_yaw), -math.sin(_lean) * math.sin(_yaw), math.cos(_lean)), 0.016),
      FIN, "dorsal", fuse=False)


def swish(shape, y0, deg, length):
    """A plate authored in the midline bent to the +X side along its length, like a fish's tail mid-stroke."""
    V, F = shape
    V = np.array(V, float).copy()
    d = V[:, 1] - y0
    th = math.radians(deg) * np.clip(d / length, 0, None) ** 1.2
    x = V[:, 0].copy()
    V[:, 0] = x * np.cos(th) + d * np.sin(th)
    V[:, 1] = y0 - x * np.sin(th) + d * np.cos(th)
    return V, F


# The tail: a ragged fan of six rays, bent to one side
TAILF = comb(6, -62, 62, [0.13, 0.142, 0.122, 0.138, 0.128, 0.118], 0.072)
k.add("tailfin", swish(flat(TAILF, (0, 0.105, 0.205), (0, 1, 0), (0, 0, 1), 0.014), 0.105, 36, 0.13), FIN, "tailFin",
      fuse=False)
# Small combed side fins, low on the flanks, raking down and back
SIDEF = comb(3, -8, 62, [0.075, 0.088, 0.07], 0.042, gap=0.38)
k.add("side", flat(SIDEF, (0.06, -0.035, 0.125), unit(0.35, 0, -1), (0, 1, 0), 0.012), FIN, "fin{s}", fuse=False,
      mirror=True)
# A small fin under the mouth, raking down and forward
VENT = comb(3, -75, -10, [0.06, 0.068, 0.056], 0.034, gap=0.4)
k.add("ventral", flat(VENT, (0, -0.07, 0.1), (0, -1, -0.3), (0, -0.3, 1), 0.012), FIN, "body", fuse=False)

# --- the eyes: big and blank, white in a thick black rim, a dot of a pupil -------------------------------------------
LID, INK, WHITE = srgb(TAN), srgb("#141418"), srgb("#F4F4F2")
def eye(u, v):
    c = np.tile(INK, (len(u), 1))
    c[(u / 0.6) ** 2 + (v / 0.76) ** 2 < 1] = WHITE
    c[((u + 0.12) / 0.2) ** 2 + ((v + 0.02) / 0.14) ** 2 < 1] = INK
    return c
# Forward on the face and turned side-forward (about 42 degrees off straight ahead) on a slightly bulged cheek, so both
# read from the front three-quarter view (they sat on the flat sides and were slits from the front)
de = unit(0.9, -1.0, 0.06)
CHEEK = np.array([0.03, -0.072, 0.215])
k.add("cheek", ell(CHEEK, (0.04, 0.04, 0.056), rot=(0, 0, -42)), TAN, "head", mirror=True)
def eyes(state):
    k.decal(CHEEK + de * 0.036, de, (0.026, 0.05), eye_look(state, eye, LID, INK), eps=0.006, rings=20, segs=56,
            mirror=True)

k.build(voxel=0.0065, smooth=4)
k.eye_states(eyes)
k.export(out)
