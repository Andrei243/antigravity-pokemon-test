# Whiscash sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# barboach.py (its plates, dotted fins and way of making eyes):
#   blender --background --factory-startup --python sculpts/whiscash.py -- <the export's Whiscash.glb> <out.glb>
# A big catfish: a fat bean of a body, deep blue, tall at the forehead and tapering to the tail, a pale cream belly; a
# wide mouth with thick light blue lips across the whole front, two light blue drops hanging from the lower lip; round
# white eyes with black pupils high on the cheeks; a broad yellow lightning bolt on the forehead; two pale yellow whiskers
# rising from the mouth's corners and curling at their tips; a round light blue dorsal fin with three black spots like
# a face; a light blue heart of a tail; dark blue flippers in front and behind, below.
# The export's joints: body (0, 0.04, 0.227), head (0, -0.14, 0.227), dorsal (0, 0.1, 0.367), tail (0, 0.28, 0.227),
# tailFin (0, 0.38, 0.247), finR/L (+-0.15, -0.06, 0.127).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

NAVY, LIGHT, CREAM, YELLOW, WHISK, BLACK = "#3048B4", "#66C4F0", "#F2DE9E", "#F6E07A", "#F0DC9A", "#1C1C26"
MOUTHLINE = "#24387E"


# --- flat plates (barboach.py) ---------------------------------------------------------------------------------------
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


def flat(outline, origin, ex, ez, thick):
    ex = unit(*ex); ez = np.array(ez, float); ez = ez - ez.dot(ex) * ex; ez /= np.linalg.norm(ez)
    nrm = np.cross(ex, ez)
    p = np.array(outline, float)
    if area(p) < 0:
        p = p[::-1]
    W = np.array(origin, float) + p[:, :1] * ex + p[:, 1:] * ez
    m = len(p)
    front, back = W + nrm * thick / 2, W - nrm * thick / 2
    F = []
    for a, b, c in earclip(p):
        F.append([a, b, c]); F.append([m + c, m + b, m + a])
    rim = []
    for i in range(m):
        j = (i + 1) % m
        rim.extend([front[i], back[i], back[j], front[j]])
        F.append([2 * m + 4 * i, 2 * m + 4 * i + 1, 2 * m + 4 * i + 2, 2 * m + 4 * i + 3])
    return np.vstack([front, back, np.array(rim)]), F


def oval(c, a, b, n=20):
    return [(c[0] + a * math.cos(t), c[1] + b * math.sin(t)) for t in np.linspace(0, 2 * math.pi, n, endpoint=False)]


def swish(shape, y0, deg, length):
    V, F = shape
    V = np.array(V, float).copy()
    d = V[:, 1] - y0
    th = math.radians(deg) * np.clip(d / length, 0, None) ** 1.2
    x = V[:, 0].copy()
    V[:, 0] = x * np.cos(th) + d * np.sin(th)
    V[:, 1] = y0 - x * np.sin(th) + d * np.cos(th)
    return V, F


# --- the body: a fat bean, tallest at the forehead, a flat-ish belly, tapering behind --------------------------------
HC, HR = np.array([0.0, -0.05, 0.24]), np.array([0.145, 0.155, 0.145])
def brow(V):
    V = V.copy()
    V[:, 2] *= 1 - 0.12 * np.clip(-V[:, 2], 0, 1) ** 2         # a flatter belly
    return V
k.add("head", ell(HC, HR, deform=brow), NAVY, "head")
def taper(V):
    V = V.copy()
    t = np.clip(V[:, 1], 0, 1)
    V[:, 0] *= 1 - 0.3 * t; V[:, 2] *= 1 - 0.22 * t
    V[:, 2] *= 1 - 0.12 * np.clip(-V[:, 2], 0, 1) ** 2
    return V
k.add("body", ell((0, 0.05, 0.232), (0.138, 0.17, 0.138), deform=taper), NAVY, "body")
k.add("tailstalk", tube([(0, 0.17, 0.238), (0, 0.24, 0.248), (0, 0.3, 0.256)], [0.085, 0.058, 0.036]), NAVY, "tail")


# The belly: cream below a line that drops a little toward the tail
def belly(P):
    zb = 0.186 - 0.012 * np.clip((P[:, 1] + 0.05) / 0.3, 0, 1) + 0.012 * np.sin(P[:, 1] * 28)
    return inside(P[:, 2] - zb, 0.003)
k.paint(belly, CREAM, on=["head", "body", "tailstalk"])

# The flippers: dark blue, in front reaching down and forward, behind reaching back; fused, so they grow out of it
k.add("pec", ell((0.118, -0.09, 0.09), (0.034, 0.072, 0.017), rot=(25, 0, 16)), NAVY, "fin{s}", mirror=True)
k.add("pelvic", ell((0.09, 0.17, 0.125), (0.026, 0.07, 0.02), rot=(-18, 0, -22)), NAVY, "body", mirror=True)


# --- the lips: a thick light blue band across the front, upper and lower, a dark line between --------------------------
def front(x, z):
    """A point on the head's front surface."""
    q = 1 - (x / HR[0]) ** 2 - ((z - HC[2]) / HR[2]) ** 2
    return np.array([x, HC[1] - HR[1] * math.sqrt(max(q, 0.0)), z])


def lip(z0, droop, r, inset):
    pts = []
    for x in np.linspace(-0.128, 0.128, 13):
        t = (x / 0.128) ** 2
        z = z0 - droop * t
        p = front(x, z)
        p[1] += inset
        pts.append(p)
    return tube(pts, [r * (0.75 + 0.25 * (1 - (x / 0.128) ** 2)) for x in np.linspace(-0.128, 0.128, 13)], per=6)


k.add("lipup", lip(0.2, 0.03, 0.025, 0.012), LIGHT, "head", fuse=False, surface="skin")
k.add("liplo", lip(0.16, 0.012, 0.023, 0.016), LIGHT, "head", fuse=False, surface="skin")
k.add("mouthline", lip(0.179, 0.022, 0.01, 0.0), MOUTHLINE, "head", fuse=False, surface="skin")
# Two teardrops hanging from the lower lip, thin at the lip and a bulb at the end
for s, sd in ((1, "R"), (-1, "L")):
    top = front(0.07 * s, 0.14); top[1] += 0.02
    k.add(f"drop{sd}", tube([top, top + (0.003 * s, -0.004, -0.03), top + (0.005 * s, -0.002, -0.058)],
                            [0.011, 0.013, 0.021]), LIGHT, "head", fuse=False, surface="skin")

# The whiskers: from the ends of the lips at the sides of the head, sweeping up and out away from the face (never
# across it from the front three-quarter), each ending in a curl above and outside the head that faces the front
k.add("whisker", tube([(0.136, -0.05, 0.172), (0.158, -0.045, 0.215), (0.18, -0.035, 0.27), (0.192, -0.02, 0.33),
                       (0.19, 0.0, 0.385), (0.172, 0.01, 0.42), (0.148, 0.012, 0.425), (0.134, 0.012, 0.405),
                       (0.142, 0.012, 0.385), (0.162, 0.012, 0.388), (0.166, 0.012, 0.404)],
                      [0.0085, 0.0085, 0.0082, 0.008, 0.0078, 0.0075, 0.0072, 0.007, 0.0068, 0.0066, 0.0064], per=6),
      WHISK, "head", fuse=False, mirror=True)

# --- the fins ------------------------------------------------------------------------------------------------------
# The dorsal: a round light blue paddle on a short stalk with three black spots like a face, turned 40 degrees
_yaw = math.radians(40)
DEX, DEZ, DO = (math.sin(_yaw), math.cos(_yaw), 0), (0, 0, 1), (0, 0.1, 0.345)
k.add("dorsalstalk", flat([(-0.02, 0.0), (0.02, 0.0), (0.016, 0.03), (-0.016, 0.03)], DO, DEX, DEZ, 0.016), LIGHT,
      "dorsal", fuse=False)
k.add("dorsal", flat(oval((0.004, 0.054), 0.048, 0.052, 32), DO, DEX, DEZ, 0.016), LIGHT, "dorsal", fuse=False)
for i, (c, a, b) in enumerate((((-0.016, 0.07), 0.009, 0.013), ((0.022, 0.074), 0.009, 0.013),
                               ((0.006, 0.032), 0.01, 0.0115))):
    k.add(f"dorsalspot{i}", flat(oval(c, a, b, 16), DO, DEX, DEZ, 0.024), BLACK, "dorsal", fuse=False)

# The tail: a light blue heart, its point at the stalk, its two lobes behind, tilted up and bent to one side
TS, TY, TZ = 0.0052, 0.295, 0.262
HEART = []
for t in np.linspace(0, 2 * math.pi, 64, endpoint=False):
    hx = 16 * math.sin(t) ** 3
    hy = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
    a, b = (hy + 17) * TS, hx * TS
    tilt = math.radians(22)
    HEART.append((TY + a * math.cos(tilt), TZ + b + a * math.sin(tilt)))
k.add("tailfin", swish(flat(HEART, (0, 0, 0), (0, 1, 0), (0, 0, 1), 0.016), TY, 38, 0.11), LIGHT, "tailFin",
      fuse=False)

# --- the forehead's mark: a broad yellow lightning bolt on the top of the head, a loose strip lying on its surface ----
def strip(line, half, at, thick, nu=10, nv=3):
    """A band along a polyline in a plane (a, b), its corners mitred sharp, laid by at(a, b) onto a surface: a sheet a
    segment, each segment's ends on the mitre lines so neighbours meet without a gap. half: a half-width a point."""
    p = np.array(line, float)
    n = len(p)
    seg = [p[i + 1] - p[i] for i in range(n - 1)]
    nrm = [np.array([-d[1], d[0]]) / np.linalg.norm(d) for d in seg]
    off = []
    for i in range(n):
        if i == 0:
            m = nrm[0]
        elif i == n - 1:
            m = nrm[-1]
        else:
            b = nrm[i - 1] + nrm[i]; b /= np.linalg.norm(b)
            m = b / max(np.dot(b, nrm[i]), 0.2)
        off.append(m * half[i])
    Vs, Fs, o = [], [], 0
    for i in range(n - 1):
        L0, L1, R0, R1 = p[i] + off[i], p[i + 1] + off[i + 1], p[i] - off[i], p[i + 1] - off[i + 1]
        def f(u, v, L0=L0, L1=L1, R0=R0, R1=R1):
            A = L0[:, None, None] * (1 - u) + L1[:, None, None] * u
            B = R0[:, None, None] * (1 - u) + R1[:, None, None] * u
            Q = A * (1 - v) + B * v
            return at(Q[0], Q[1])
        V, F = sheet(f, nu, nv, thick)
        Vs.append(V); Fs.extend([[j + o for j in fc] for fc in F]); o += len(V)
    return np.vstack(Vs), Fs


mn = unit(0, -0.5, 0.86)
MC = HC + mn * HR[2]
MU = np.cross(mn, (1.0, 0, 0))
def on_brow(a, b):
    """a across the head, b up over its brow (metres), cast from the head's middle onto its surface."""
    p = MC[None, None, :] + a[..., None] * np.array([1.0, 0, 0]) + b[..., None] * MU
    d = p - HC
    t = 1 / np.sqrt(((d / HR) ** 2).sum(-1, keepdims=True))
    dn = d / np.linalg.norm(d, axis=-1, keepdims=True)
    return HC + d * t + dn * 0.003
BOLT = [(-0.07, 0.02), (-0.024, -0.016), (0.008, 0.014), (0.068, -0.024)]
k.add("bolt", strip(BOLT, [0.019, 0.019, 0.017, 0.005], on_brow, 0.004), YELLOW, "head", fuse=False)

# --- the eyes: round, white, a black pupil and a black rim, high on the cheeks and turned forward --------------------
INK, WH, LID, LINE = srgb("#141418"), srgb("#FFFFFF"), srgb(NAVY), srgb("#B4C6F2")
def eye(u, v):
    c = np.tile(INK, (len(u), 1))
    r2 = u * u + v * v
    c[r2 < 0.775 ** 2] = WH
    c[(u / 0.36) ** 2 + ((v + 0.02) / 0.4) ** 2 < 1] = INK
    return c
de = unit(0.74, -0.6, 0.3)
EC = HC + de / np.sqrt(((de / HR) ** 2).sum())
def eyes(state):
    look = eye_look(state, eye, LID, LINE, line=0.18)
    if state == "Fierce":
        look = eye_look(state, eye, LID, INK, line=0.18)
    k.decal(EC, de, (0.03, 0.032), look, eps=0.005, rings=20, segs=56, mirror=True)

k.build(voxel=0.0055, smooth=5)
k.eye_states(eyes)
k.export(out)
