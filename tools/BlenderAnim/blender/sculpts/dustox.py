# Dustox sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dustox.py -- <the export's Dustox.glb> <out.glb>
# The wings are a fan of loose flat plates, their pattern laid as thicker plates inside them so that it shows on the
# front and the back alike (patches land on the fused body alone). The pictures give the wing's edge a darker green
# band, not a pale one.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

LILAC, SEAM, FRILL = "#C4A4DE", "#9878BC", "#DCCDEE"
GREEN, RIM, RED = "#A2DC58", "#3FAA3C", "#E4405C"
YELLOW, BROWN, PINK, TIP = "#F4D93C", "#8C6A1C", "#E86A88", "#CF3550"


# --- flat plates ------------------------------------------------------------------------------------------------
def area(p):
    return 0.5 * np.sum(p[:, 0] * np.roll(p[:, 1], -1) - np.roll(p[:, 0], -1) * p[:, 1])


def earclip(p):
    """Triangles of a simple polygon, counter-clockwise."""
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


def frame(ex, ez):
    ex, ez = unit(*ex), unit(*ez)
    nrm = np.cross(ex, ez); nrm /= np.linalg.norm(nrm)
    return ex, ez, nrm


def flat(outline, origin, ex, ez, thick):
    """A flat plate of a polygon (a, b in the plane of ex, ez about origin) with sharp edges that shade flat."""
    ex, ez, nrm = frame(ex, ez)
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
        F.append([2 * m + 4 * i + q for q in range(4)])
    return np.vstack([front, back, np.array(rim)]), F


def oval(c, a, b, turn=0.0, n=40):
    th = np.linspace(0, 2 * math.pi, n, endpoint=False)
    ct, st = math.cos(math.radians(turn)), math.sin(math.radians(turn))
    x, y = a * np.cos(th), b * np.sin(th)
    return np.stack([c[0] + x * ct - y * st, c[1] + x * st + y * ct], axis=1)


def ring(c, a, b, w, turn, origin, ex, ez, thick, n=48):
    """A flat oval ring (a mark with a hole), sharp-edged, its faces and rims apart."""
    ex, ez, nrm = frame(ex, ez)
    o = np.array(origin, float)
    def lay(p):
        return o + p[:, :1] * ex + p[:, 1:] * ez
    A, B = lay(oval(c, a, b, turn, n)), lay(oval(c, a - w, b - w, turn, n))
    h = nrm * thick / 2
    V = [A + h, B + h, A - h, B - h]        # outer front, inner front, outer back, inner back
    F = []
    def q(s, i):
        return s * n + i % n
    for i in range(n):
        F.append([q(0, i), q(0, i + 1), q(1, i + 1), q(1, i)])          # front
        F.append([q(2, i), q(3, i), q(3, i + 1), q(2, i + 1)])          # back
    off = 4 * n
    rims = []
    for i in range(n):
        j = (i + 1) % n
        rims.append([A[i] + h, A[i] - h, A[j] - h, A[j] + h])            # outer rim, outward
        rims.append([B[i] + h, B[j] + h, B[j] - h, B[i] - h])            # inner rim, inward
    R = np.array([p for r in rims for p in r])
    F += [[off + 4 * i + t for t in range(4)] for i in range(len(rims))]
    return np.vstack(V + [R]), F


def fan(th0, th1, R, slits=(), depth=0.07, half=1.8, inset=0.0, n=90, root=0.02):
    """
    A wing as a fan about its root: angles th0..th1 (degrees, 0 straight out, up positive) at radius R(theta), V slits
    cut in from the edge, and the whole drawn in by inset (the light field inside the dark border keeps clear of the
    slits' sides).
    """
    pts = [(0.0, 0.0)]
    for t in np.linspace(th0, th1, n):
        r = R(t)
        cut = max([max(0.0, 1 - abs(t - s) / half) for s in slits] + [0.0])
        rn = r - depth * cut
        if inset:
            rn = min(r - inset, rn - 0.007)
        a = math.radians(t)
        pts.append((rn * math.cos(a), rn * math.sin(a)))
    return pts


# --- the body: an egg leaning forward, its big end the head --------------------------------------------------------
H = np.array([0.0, -0.05, 0.345])          # the head's centre
HR = 0.106
k.add("head", ell(H, (HR, HR * 0.95, HR * 0.95)), LILAC, "head")
AX = unit(0, -0.55, 0.83)                  # the body's long axis, leaning forward
C = np.array([0.0, 0.015, 0.25])
def taper(V):
    V = V.copy()
    s = 1 - 0.32 * np.clip(-V[:, 2], 0, 1) ** 1.4
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("abdomen", ell(C, (0.098, 0.094, 0.14), rot=(33, 0, 0), deform=taper), LILAC, "body")
# The segments: darker seams across the lower body
for o in (-0.025, -0.06, -0.092):
    k.paint(lambda P, o=o: inside(np.abs((P - C) @ AX - o) - 0.0035, 0.0018) * inside((P - C) @ AX - 0.0, 0.002),
            SEAM, on=["abdomen"])

# A ragged pale frill round the tail's end
TIPC = C - AX * 0.105
side1 = np.array([1.0, 0, 0]); side2 = np.cross(AX, side1)
for i in range(11):
    a = 2 * math.pi * i / 11 + 0.2
    rd = side1 * math.cos(a) + side2 * math.sin(a)
    base = TIPC + rd * 0.04
    tipp = base + rd * 0.045 - AX * 0.035 + rd * 0.008 * (i % 2)
    k.add(f"frill{i}", tube([base, (base + tipp) / 2, tipp], [0.016, 0.01, 0.002]), FRILL, "body", fuse=False)

# Small pink legs with red tips under the front, two a side
for j, (b, e) in enumerate((((0.035, -0.105, 0.255), (0.05, -0.135, 0.22)), ((0.05, -0.06, 0.195), (0.065, -0.09, 0.16)))):
    b, e = np.array(b), np.array(e)
    k.add(f"leg{j}", tube([b, (b + e) / 2 + [0.003, 0, 0.003], e], [0.013, 0.012, 0.011]), PINK, "leg{s}",
          fuse=False, mirror=True)
    d = (e - b) / np.linalg.norm(e - b)
    for c in (-1, 1):
        k.add(f"claw{j}{'ab'[c > 0]}", tube([e, e + d * 0.014 + np.array([0.008 * c, 0, -0.006])], [0.009, 0.004]),
              TIP, "leg{s}", fuse=False, mirror=True)

# The antennae: thick yellow bullets rising up and out from the crown, ribbed in brown
def antenna_parts():
    b = np.array([0.045, -0.075, 0.42]); e = np.array([0.135, -0.09, 0.555])
    d = (e - b) / np.linalg.norm(e - b)
    L = np.linalg.norm(e - b)
    yield "astem", tube([b - d * 0.02, e - d * 0.02], [0.014, 0.008]), BROWN
    for i in range(4):
        cen = b + d * L * (0.1 + 0.22 * i + (0.04 if i == 3 else 0))
        r = 0.031 - 0.0035 * i
        length = L * 0.135 if i < 3 else L * 0.2
        def pointy(V, last=(i == 3)):
            V = V.copy()
            if last:
                s = 1 - 0.75 * np.clip(V[:, 2], 0, 1) ** 1.5
                V[:, 0] *= s; V[:, 1] *= s
            return V
        V, F = ell((0, 0, 0), (r, r, length), deform=pointy)
        # turn +Z onto the antenna's line
        z = np.array([0, 0, 1.0]); ax = np.cross(z, d); s_ = np.linalg.norm(ax); c_ = d @ z
        K = np.array([[0, -ax[2], ax[1]], [ax[2], 0, -ax[0]], [-ax[1], ax[0], 0]])
        Rm = np.eye(3) + K + K @ K * ((1 - c_) / (s_ * s_))
        yield f"aseg{i}", (V @ Rm.T + cen, F), YELLOW
for nm, shape, col in antenna_parts():
    k.add(nm, shape, col, "ear{s}", fuse=False, mirror=True)

# --- the wings: a broad fan, upper and lower, dark green bordered, slit at the edge -------------------------------
WO = np.array([0.055, 0.055, 0.285])
EX, EZ = (1.0, 0.16, 0.0), (0.0, 0.05, 1.0)
def RU(t):
    return 0.47 - 0.1 * smoothstep(28, 58, t)
def RL(t):
    return 0.38 - 0.13 * smoothstep(-30, -88, t)
UPS, LOWS = (4,), (-30, -52, -72)
k.add("wing", flat(fan(-6, 58, RU, UPS), WO, EX, EZ, 0.008), RIM, "wing{s}", fuse=False, mirror=True)
k.add("field", flat(fan(-6 + 2.5, 58 - 3, RU, UPS, inset=0.05), WO, EX, EZ, 0.012), GREEN, "wing{s}", fuse=False,
      mirror=True)
WL = WO + np.array([0, 0.014, 0.015])
k.add("hind", flat(fan(-90, -10, RL, LOWS), WL, EX, EZ, 0.008), RIM, "wing{s}", fuse=False, mirror=True)
k.add("hfield", flat(fan(-90 + 3, -10 - 2.5, RL, LOWS, inset=0.045), WL, EX, EZ, 0.012), GREEN, "wing{s}", fuse=False,
      mirror=True)
# A red ring on each upper wing, a red bar on each lower one
k.add("ring", ring((0.255, 0.115), 0.085, 0.04, 0.017, 14, WO, EX, EZ, 0.0165), RED, "wing{s}", fuse=False,
      mirror=True)
k.add("bar", flat(oval((0.2 * math.cos(math.radians(-38)), 0.2 * math.sin(math.radians(-38))), 0.065, 0.017, -24),
                  WL, EX, EZ, 0.0165), RED, "wing{s}", fuse=False, mirror=True)

# --- the face: yellow compound eyes of three dots each, a small dark smile -----------------------------------------
INK, LID = srgb("#1C1424"), srgb(YELLOW)
EYE = polygon([(-0.86, 0.86), (0.6, 0.97), (0.92, 0.7), (0.96, 0.45), (0.25, -0.86), (0.0, -0.96), (-0.25, -0.8),
               (-0.98, 0.5)])
def eye(u, v):
    c = np.tile(LID, (len(u), 1))
    for du, dv in ((-0.42, 0.4), (0.4, 0.45), (0.02, -0.3)):
        c[((u - du) ** 2 + (v - dv) ** 2) < 0.23 ** 2] = INK
    return c
de = unit(0.62, -0.72, -0.12)
def eyes(state):
    look = eye_look(state, eye, LID, INK)
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        k.decal((H + de * HR) * m, de * m, (0.046, 0.048),
                (look if s > 0 else (lambda u, v, look=look: look(-u, v))), up=(0, 0, 1),
                eps=0.007, rings=20, segs=56,
                outline=(EYE if s > 0 else (lambda th: EYE(math.pi - th))))

LIL, MOUTH = srgb(LILAC), srgb("#2A1A30")
def smile(u, v):
    c = np.tile(LIL, (len(u), 1))
    c[(np.abs(v - (-0.35 + 0.75 * u * u)) < 0.2) & (np.abs(u) < 0.85)] = MOUTH
    return c
dm = unit(0, -0.82, -0.57)
k.decal(H + dm * HR, dm, (0.036, 0.024), smile, up=(0, 0, 1), eps=0.004, rings=14, segs=48)

k.build(voxel=0.007, smooth=4)
k.eye_states(eyes)
k.export(out)
