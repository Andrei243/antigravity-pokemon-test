# Magikarp sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/magikarp.py -- <the export's Magikarp.glb> <out.glb>
# A round red-orange carp hanging in the air: big scales drawn as rows of arcs behind a curved gill line, huge round
# vacant eyes, pink lips pushed out in an open O with two long yellow barbels drooping from its corners, a yellow
# crown of three points along the back, a small spiky yellow fin under the belly, white pectoral fins edged in red,
# and a large white tail rimmed in red. The fins are loose flat plates, the patterns on them thicker plates inside.
# The export's joints: body (0, 0, 0.527), head (0, -0.18, 0.527) forward, dorsal (0, 0.02, 0.797) on the back,
# tail (0, 0.3, 0.527) and tailFin (0, 0.42, 0.527) behind, finL/R (-+0.15, -0.08, 0.497) at the sides.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

ORANGE, LINE, YELLOW, WHITE, RIM, RAY = "#EC5A2C", "#C4421E", "#F4CF55", "#F2EFEA", "#DC4420", "#C8C4C2"
LIPS, MOUTH = "#F29AAE", "#A8324A"


# --- flat plates (as beautifly.py's) -------------------------------------------------------------------------------
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


def flat(outline, origin, ex, ez, thick, taper=None):
    """A flat plate of a polygon (a, b in the plane of ex, ez about origin); taper(b) scales its thickness."""
    ex, ez = unit(*ex), unit(*ez)
    nrm = np.cross(ex, ez); nrm /= np.linalg.norm(nrm)
    p = np.array(outline, float)
    if area(p) < 0:
        p = p[::-1]
    W = np.array(origin, float) + p[:, :1] * ex + p[:, 1:] * ez
    t = np.full(len(p), thick / 2) if taper is None else thick / 2 * taper(p[:, 1])
    m = len(p)
    front, back = W + nrm * t[:, None], W - nrm * t[:, None]
    F = []
    for a, b, c in earclip(p):
        F.append([a, b, c]); F.append([m + c, m + b, m + a])
    rim = []
    for i in range(m):
        j = (i + 1) % m
        rim.extend([front[i], back[i], back[j], front[j]])
        F.append([2 * m + 4 * i, 2 * m + 4 * i + 1, 2 * m + 4 * i + 2, 2 * m + 4 * i + 3])
    return np.vstack([front, back, np.array(rim)]), F


def lens(a0, a1, width, bulge=0.5, n=20):
    a0, a1 = np.array(a0, float), np.array(a1, float)
    d = a1 - a0; L = np.linalg.norm(d); d /= L
    side = np.array([-d[1], d[0]])
    up = [a0 + d * L * (i / (n - 1)) + side * width * np.sin(np.pi * (i / (n - 1)) ** (math.log(0.5) / math.log(bulge))) ** 0.7
          for i in range(n)]
    dn = [a0 + d * L * (i / (n - 1)) - side * width * np.sin(np.pi * (i / (n - 1)) ** (math.log(0.5) / math.log(bulge))) ** 0.7
          for i in range(n - 2, 0, -1)]
    return np.array(up + dn)


def swish(shape, root, scale, deg, length):
    """A tail's plate (authored in the midline) made smaller about its root (y, z) and bent to the +X side along its
    length like a fish mid-stroke, so that from the front and from behind it shows its face, not its edge."""
    V, F = shape
    V = np.array(V, float).copy()
    V[:, 1] = root[0] + (V[:, 1] - root[0]) * scale
    V[:, 2] = root[1] + (V[:, 2] - root[1]) * scale
    d = V[:, 1] - root[0]
    th = math.radians(deg) * np.clip(d / length, 0, None) ** 1.2
    x = V[:, 0].copy()
    V[:, 0] = x * np.cos(th) + d * np.sin(th)
    V[:, 1] = root[0] - x * np.sin(th) + d * np.cos(th)
    return V, F


# --- the body: a round carp, a little longer than tall, flattened a touch from side to side ---------------------------
BC, BR = np.array([0.0, 0.0, 0.527]), np.array([0.185, 0.238, 0.24])
k.add("body", ell(BC, BR), ORANGE, "body")
# the front, on the head's bone, inside the body's outline so that no step shows between them
FC, FR = np.array([0.0, -0.08, 0.53]), np.array([0.172, 0.156, 0.218])
k.add("face", ell(FC, FR), ORANGE, "head")
# the short narrowing to the tail
k.add("stalk", tube([(0, 0.17, 0.527), (0, 0.29, 0.53), (0, 0.365, 0.533)], [0.11, 0.062, 0.046]), ORANGE, "tail")


# --- the scales: a curved gill line behind the head, then big scales as overlapping arcs, convex to the tail ----------
def around(P):
    """Coordinates on the body's side: along it (y) and round it (the angle from the side's middle times a radius)."""
    phi = np.arctan2(P[:, 2] - BC[2], np.abs(P[:, 0]))
    return P[:, 1], phi


def gill(P):
    y, phi = around(P)
    yg = -0.085 + 0.065 * np.cos(np.clip(phi, -1.6, 1.6))
    return inside(np.abs(y - yg) - 0.0055, 0.003) * inside(phi - 1.25, 0.02) * (phi > -0.75)


SY, SZ, SR, W = 0.085, 0.13, 0.068, 0.0055     # spacing along, spacing round (radius times angle), a scale's radius


def scales(P):
    y, phi = around(P)
    s = phi * 0.22
    yg = -0.085 + 0.065 * np.cos(np.clip(phi, -1.6, 1.6))
    best_d = np.full(len(P), 9.0)
    best_i = np.full(len(P), 99.0)
    for i in range(-2, 6):
        yc = -0.035 + i * SY
        for j in range(-4, 5):
            zc = j * SZ + (i % 2) * SZ / 2
            d = np.sqrt((y - yc) ** 2 + (s - zc) ** 2)
            take = (d < SR) & (i < best_i)            # the frontmost scale that holds the point lies on top
            best_d = np.where(take, d, best_d)
            best_i = np.where(take, i, best_i)
    line = inside(np.abs(best_d - SR + W) - W, 0.003) * (best_d < 1)
    return line * smoothstep(yg + 0.01, yg + 0.03, y) * inside(y - 0.23, 0.02) * (phi > -1.2)


k.paint(scales, LINE, on=["body", "face", "stalk"])
k.paint(gill, LINE, on=["body", "face"])

# --- the lips: a pink ring pushed forward and a little up, the mouth an open O carved inside it ----------------------
AX = unit(0, -1, 0.36)                     # the mouth's way out
MC = np.array([0.0, -0.262, 0.56])         # the ring's middle, on the face's front
ex = np.array([1.0, 0, 0]); ez = np.cross(AX, ex); ez /= np.linalg.norm(ez)
if ez[2] < 0:
    ez = -ez
ring = [MC + 0.054 * math.cos(a) * ex + 0.066 * math.sin(a) * ez for a in np.linspace(0, 2 * math.pi * 1.08, 30)]
k.add("lips", tube(ring, [0.027] * len(ring), seg=18, per=4), LIPS, "head", surface="skin")
k.add("lipsfill", ell(MC - AX * 0.035, (0.07, 0.05, 0.08), rot=(-20, 0, 0)), LIPS, "head", surface="skin")
k.add("mouth", ell(MC - AX * 0.01, (0.034, 0.07, 0.044), rot=(-20, 0, 0)), MOUTH, "head", carve=True)

# The barbels: long, thin, yellow, drooping from the corners of the mouth and curling a little forward at the ends
k.add("barbel", tube([(0.05, -0.255, 0.52), (0.085, -0.275, 0.46), (0.1, -0.275, 0.37), (0.095, -0.255, 0.28),
                      (0.08, -0.265, 0.21), (0.065, -0.29, 0.175)], [0.014, 0.013, 0.012, 0.011, 0.01, 0.009]),
      YELLOW, "head", fuse=False, mirror=True)

# --- the fins --------------------------------------------------------------------------------------------------------
# The crown on the back: three yellow points, the middle tallest leaning back, the last streaming back; thick at the
# root so that from the front it rises out of the back as a wedge
CROWN = [(-0.13, 0.72), (-0.115, 0.82), (-0.11, 0.935), (-0.07, 0.86), (-0.03, 0.89), (0.03, 0.985), (0.065, 0.9),
         (0.09, 0.84), (0.16, 0.87), (0.24, 0.875), (0.19, 0.8), (0.14, 0.71), (0.04, 0.68), (-0.06, 0.69)]
k.add("crown", flat(CROWN, (0, 0, 0), (0, 1, 0), (0, 0, 1), 0.07,
                    taper=lambda b: np.clip(1 - 0.75 * (b - 0.76) / 0.2, 0.25, 1)), YELLOW, "dorsal", fuse=False)

# The belly fin: a pair of small spiky yellow plates under the middle, pointing down and back
BELLY = [(-0.06, -0.035), (-0.055, 0.04), (-0.05, 0.1), (-0.015, 0.05), (0.02, 0.11), (0.04, 0.05), (0.09, 0.09),
         (0.075, 0.0), (0.06, -0.04)]
k.add("bellyfin", flat(BELLY, (0.045, 0.03, 0.305), (0, 1, 0), (0.35, 0, -1), 0.02,
                       taper=lambda b: np.clip(1 - 0.6 * b / 0.1, 0.35, 1)), YELLOW, "body", fuse=False, mirror=True)

# The pectoral fins: white wedges swept back from behind the gill line, the leading edge red, two grey rays
# turned so that they reach out and down like little arms, their white faces toward the front
_n = unit(0.42, -0.86, 0.2)
_x = np.array(unit(0.36, 0.28, -0.89)); _x = _x - _x.dot(_n) * _n; _x /= np.linalg.norm(_x)
FO, FX, FZ = np.array([0.15, -0.06, 0.45]), tuple(_x), tuple(np.cross(_n, _x))
PS = 0.85
PEC = smooth([(-0.01, 0.04), (-0.01, -0.03), (0.08, -0.05), (0.16, -0.075), (0.15, -0.005), (0.19, 0.06),
              (0.25, 0.115), (0.13, 0.08), (0.05, 0.06)], per=5) * PS
k.add("pec", flat(PEC, FO, FX, FZ, 0.012), WHITE, "fin{s}", fuse=False, mirror=True)
k.add("pecedge", flat(lens(np.array((0.0, 0.05)) * PS, np.array((0.25, 0.115)) * PS, 0.012, bulge=0.4), FO, FX, FZ,
                      0.018), RIM, "fin{s}", fuse=False, mirror=True)
k.add("pecroot", flat(lens((-0.02, 0.008), (0.07, 0.01), 0.04, bulge=0.3), FO, FX, FZ, 0.018), ORANGE, "fin{s}",
      fuse=False, mirror=True)
for i, a1 in enumerate([(0.19, 0.05), (0.15, -0.05)]):
    k.add(f"pecray{i}", flat(lens((0.05, 0.01), np.array(a1) * PS, 0.005), FO, FX, FZ, 0.0145), RAY, "fin{s}",
          fuse=False, mirror=True)

# The tail: a big white fin, its upper lobe the longer, the trailing edge hollowed between the lobes, both outer edges
# rimmed in red-orange
TAIL = smooth([(0.34, 0.6), (0.42, 0.7), (0.52, 0.8), (0.63, 0.885), (0.61, 0.77), (0.565, 0.64), (0.545, 0.52),
               (0.56, 0.4), (0.6, 0.28), (0.62, 0.18), (0.51, 0.29), (0.41, 0.4), (0.34, 0.47)], per=5)
# Smaller about its root than drawn and bent to one side mid-stroke (swish); the rim is the thickest plate, so that
# where an edge is seen it is one colour
TR, TS, TB, TL = (0.34, 0.535), 0.8, 40, 0.24
def tail(shape):
    return swish(shape, TR, TS, TB, TL)
k.add("tailfin", tail(flat(TAIL, (0, 0, 0), (0, 1, 0), (0, 0, 1), 0.016)), WHITE, "tailFin", fuse=False)
k.add("tailedge0", tail(flat(lens((0.35, 0.615), (0.635, 0.888), 0.017, bulge=0.45), (0, 0, 0), (0, 1, 0), (0, 0, 1),
                             0.024)), RIM, "tailFin", fuse=False)
k.add("tailedge1", tail(flat(lens((0.35, 0.455), (0.625, 0.175), 0.017, bulge=0.45), (0, 0, 0), (0, 1, 0), (0, 0, 1),
                             0.024)), RIM, "tailFin", fuse=False)
k.add("tailroot", tail(flat(lens((0.32, 0.535), (0.44, 0.535), 0.07, bulge=0.3), (0, 0, 0), (0, 1, 0), (0, 0, 1),
                            0.024)), ORANGE, "tailFin", fuse=False)
for i, a1 in enumerate([(0.59, 0.77), (0.54, 0.535), (0.58, 0.29)]):
    k.add(f"tailray{i}", tail(flat(lens((0.42, 0.535), a1, 0.005), (0, 0, 0), (0, 1, 0), (0, 0, 1), 0.0175)), RAY,
          "tailFin", fuse=False)

# --- the eyes: huge white rounds with a small black pupil in the middle and a black rim, staring at nothing --------
INK, EW, LID = srgb("#18181C"), srgb("#FFFFFF"), srgb(ORANGE)
def eye(u, v):
    r2 = u * u + v * v
    c = np.tile(EW, (len(u), 1))
    c[r2 > 0.8 ** 2] = INK
    c[r2 < 0.27 ** 2] = INK
    return c


def on_body(d):
    d = unit(*d)
    t = 1 / math.sqrt(float(((d / BR) ** 2).sum()))
    return BC + d * t, d


EC, de = on_body((0.8, -0.5, 0.33))
def eyes(state):
    k.decal(EC, de, (0.062, 0.062), eye_look(state, eye, LID, INK, line=0.16), eps=0.006, rings=20, segs=56,
            mirror=True)

k.build(voxel=0.007, smooth=4)
k.eye_states(eyes)
k.export(out)
