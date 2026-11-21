# Mega Staraptor sculpted on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/staraptormega.py -- <the export's Staraptor-Mega.glb> <out.glb>
# Staraptor's script (which builds as it is read) copied and changed where the pictures change it: the same plump,
# forward-leaning hawk with its comb, white face and chest, small fierce red eyes and orange beak, but pale grey all
# over (head, neck and back too, no black bib), hovering. What the Mega adds, largest first: two great wings raised in
# a V, a grey arm banded three times in pale gold with dark brown flight feathers fanning out from it; a long grey tail
# fanned down behind, each feather tipped pale gold; the comb now red-orange all over, with grey crown feathers raised
# behind it; a red flap hanging from each cheek down the side of the chest; big golden talons, curled to grip, with
# black claws, under shaggy grey thighs.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, srgb, inside, tri, unit, eye_look, polygon, args, slab, hull, ring_band,
                       outward)

src, out = args()
kit = Kit(src, coat="fur")


class Lift:
    """The base's frame, raised: Staraptor's numbers are kept and everything is set DZ higher (the Mega hovers; its
    body joint is 0.063 higher, its head's 0.015, its legs' 0.071)."""
    def __init__(self, kit, dz):
        self.kit, self.d = kit, np.array([0, 0, dz])

    def add(self, name, shape, *a, **kw):
        V, F = shape
        self.kit.add(name, (np.asarray(V, float) + self.d, F), *a, **kw)

    def paint(self, mask, color, on=None):
        d = self.d
        self.kit.paint(lambda P: mask(P - d), color, on)

    def decal(self, center, *a, **kw):
        self.kit.decal(np.asarray(center, float) + self.d, *a, **kw)


DZ = 0.04
k = Lift(kit, DZ)

BODY, DARK, WHITE, MASK, WING = "#ACA6AC", "#8E888E", "#F2F2F0", "#8E888E", "#ACA6AC"
ORANGE, TIP, RED, CLAW = "#F6B12E", "#E0901C", "#E8553A", "#262224"
FLIGHT, FLIGHT2, GOLD, PALE = "#4A413B", "#564C46", "#F2B020", "#E8D890"
H = np.array([0, -0.165, 0.725])          # the head's centre, carried forward of the head's joint on a thick neck
HR = np.array([0.14, 0.15, 0.14])


# --- helpers (Staraptor's) ----------------------------------------------------------------------------------------
def in_poly(x, y, pts):
    pts = np.array(pts, float)
    res = np.zeros(len(x), bool)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        res ^= ((y1 > y) != (y2 > y)) & (x < (x2 - x1) * (y - y1) / (y2 - y1 + 1e-12) + x1)
    return res


def poly_mask(x, y, pts, edge=0.0025):
    pts = np.array(pts, float)
    d = np.full(len(x), 9.0)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        ex, ey = x2 - x1, y2 - y1
        t = np.clip(((x - x1) * ex + (y - y1) * ey) / (ex * ex + ey * ey + 1e-12), 0, 1)
        d = np.minimum(d, np.hypot(x - x1 - t * ex, y - y1 - t * ey))
    return inside(np.where(in_poly(x, y, pts), -d, d), edge)


def mirrored(half):
    return half + [(-a, z) for a, z in half[::-1][1:-1]]


def capped(pts):
    r = polygon(pts)
    far = max(math.hypot(a, b) for a, b in pts)
    return lambda th: min(r(th), far)


def spline(pts, per=10):
    p = np.array(pts, float)
    e = np.vstack([2 * p[0] - p[1], p, 2 * p[-1] - p[-2]])
    res = []
    for i in range(len(p) - 1):
        p0, p1, p2, p3 = e[i], e[i + 1], e[i + 2], e[i + 3]
        for j in range(per):
            t = j / per
            res.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    res.append(p[-1])
    return np.array(res)


def comb(spine, depth, width, red_at, seg=48, per=16):
    """Staraptor's crest loft: two meshes split by red_at(t, s)."""
    P = spline(spine, per)
    n = len(P)
    t = np.linspace(0, 1, n)
    D = np.interp(t, np.linspace(0, 1, len(depth)), depth)
    Wd = np.interp(t, np.linspace(0, 1, len(width)), width)
    T = np.gradient(P, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
    X = np.array([1.0, 0, 0])
    N = np.cross(T, X); N /= np.linalg.norm(N, axis=1, keepdims=True)
    ang = np.linspace(0, 2 * math.pi, seg, endpoint=False)
    V = (P[:, None, :] + D[:, None, None] * np.cos(ang)[None, :, None] * N[:, None, :]
         + Wd[:, None, None] * np.sin(ang)[None, :, None] * X[None, None, :]).reshape(-1, 3)
    V = np.vstack([V, P[0] - T[0] * 0.01, P[-1] + T[-1] * 0.004])
    s0, s1 = len(V) - 2, len(V) - 1
    faces, red = [], []
    for i in range(n - 1):
        for j in range(seg):
            a, b = i * seg + j, i * seg + (j + 1) % seg
            faces.append([a, b, b + seg, a + seg])
            red.append(red_at((t[i] + t[i + 1]) / 2, math.cos((ang[j] + ang[(j + 1) % seg]) / 2 if j < seg - 1
                                                              else ang[j] + math.pi / seg)))
    last = (n - 1) * seg
    for j in range(seg):
        faces.append([s0, (j + 1) % seg, j]); red.append(False)
        faces.append([s1, last + j, last + (j + 1) % seg]); red.append(True)
    f0 = faces[seg * (n // 2)]
    c = V[f0].mean(axis=0)
    nrm = np.cross(V[f0[1]] - V[f0[0]], V[f0[2]] - V[f0[0]])
    if np.dot(nrm, c - P[n // 2]) < 0:
        faces = [f[::-1] for f in faces]

    def pick(want):
        fs = [f for f, r in zip(faces, red) if r == want]
        used = sorted({i for f in fs for i in f})
        remap = {o: i for i, o in enumerate(used)}
        return V[used], [[remap[i] for i in f] for f in fs]
    return pick(False), pick(True)


# --- the body --------------------------------------------------------------------------------------------------------
def plump(V):
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.28 * t ** 1.4
    return V
k.add("body", ell((0, 0.05, 0.45), (0.235, 0.27, 0.225), rot=(-14, 0, 0), deform=plump), BODY, "body")
k.add("breast", ell((0, -0.085, 0.52), (0.205, 0.165, 0.2), deform=plump), BODY, "body")
k.add("thigh", ell((0.085, -0.01, 0.255), (0.075, 0.08, 0.075)), BODY, "leg{s}", mirror=True)
k.add("neck", ell((0, -0.13, 0.62), (0.13, 0.13, 0.13), rot=(-25, 0, 0)), BODY, "head")
k.add("head", ell(H, HR), BODY, "head")
k.add("cap", ell((0, -0.08, 0.81), (0.07, 0.115, 0.075)), MASK, "head")
# the shoulders, where the raised arms leave the body
k.add("shoulder", ell((0.15, 0.0, 0.56), (0.07, 0.07, 0.08), rot=(0, -35, 0)), BODY, "wing{s}", mirror=True)

# The face and throat: white down the front, round the head's axis
R = 0.15
def round_head(P):
    P = np.atleast_2d(P)
    return np.arctan2(P[:, 0], -(P[:, 1] - H[1])) * R, P[:, 2]

HE = 0.035
SL = 0.32
def eye_top(u):
    return 0.22 + SL * u
def cut(state, u):
    return np.minimum(eye_top(u), -0.02 + 0.55 * u) if state == "Fierce" else eye_top(u)
de = unit(0.52, -0.84, 0.16)
E = H + de / np.linalg.norm(de / HR)
def frame(n):
    n = np.array(n, float); n /= np.linalg.norm(n)
    right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
    return right, np.cross(n, right)
ERI, EUP = frame(de)
def on_head(u, v):
    o = E + de * 0.35 + ERI * u * HE + EUP * v * HE
    oc, dd = (o - H) / HR, -de / HR
    a, b, c = dd @ dd, 2 * oc @ dd, oc @ oc - 1
    return o + dd * HR * (-b - math.sqrt(max(b * b - 4 * a * c, 0))) / (2 * a)

# The darker grey over and behind the eyes, as on Staraptor (the eye's cut is its lower edge); the forehead white
low, high = [], []
for u in np.linspace(-0.95, 3.4, 44):
    v_low = eye_top(u) - 0.04 if u < 0.95 else max(-0.75, eye_top(0.95) - 0.04 - 1.6 * (u - 0.95))
    s, z = round_head(on_head(u, v_low))
    low.append((float(s[0]), float(z[0])))
    s2, z2 = round_head(on_head(u, eye_top(u) + 1.25 + 0.25 * max(u, 0)))
    high.append((float(s2[0]), float(z2[0])))
MASKP = low + [(0.5, low[-1][1]), (0.5, 1.2)] + [(high[-1][0], 1.2)] + high[::-1]
MASKL = [(-a, z) for a, z in MASKP]
HEADFRONT = lambda P: (P[:, 2] > 0.6)
k.paint(lambda P: inside(np.abs(np.arctan2(P[:, 0], -(P[:, 1] - H[1]))) - 1.2, 0.0025) * HEADFRONT(P)
        * (P[:, 2] < 0.87) * (P[:, 1] < -0.02), WHITE, on=["head", "neck"])
k.paint(lambda P: np.maximum(poly_mask(*round_head(P), MASKP), poly_mask(*round_head(P), MASKL))
        * (P[:, 2] > 0.64) * (P[:, 1] < 0.02), MASK)

# The white chest and throat, its lower edge in ragged points; no bib on the Mega
CR = 0.22
def round_body(P):
    return np.arctan2(P[:, 0], -(P[:, 1] - 0.0)) * CR, P[:, 2]
CHEST = mirrored([(0.0, 0.72), (0.12, 0.72), (0.15, 0.63), (0.19, 0.55), (0.2, 0.46), (0.19, 0.4), (0.16, 0.34),
                  (0.13, 0.37), (0.1, 0.32), (0.07, 0.36), (0.035, 0.31), (0.0, 0.345)])
FRONT = lambda P: (P[:, 1] < 0.02) * (P[:, 2] < 0.72) * (P[:, 2] > 0.2)
k.paint(lambda P: poly_mask(*round_body(P), CHEST) * FRONT(P), WHITE, on=["body", "breast", "neck"])

# --- loose pieces -------------------------------------------------------------------------------------------------------

# The comb, Staraptor's sail, red-orange now from just above its grey root to its tip over the beak
SPINE = [(0, -0.1, 0.79), (0, -0.09, 0.9), (0, -0.13, 0.965), (0, -0.22, 0.935), (0, -0.31, 0.865),
         (0, -0.38, 0.8), (0, -0.43, 0.74)]
(rootV, rootF), (redV, redF) = comb(SPINE, [0.1, 0.11, 0.1, 0.085, 0.062, 0.036, 0.006],
                                    [0.066, 0.063, 0.055, 0.045, 0.035, 0.022, 0.006], lambda t, s: t > 0.1)
k.add("crestroot", (rootV, rootF), MASK, "head", fuse=False)
k.add("crest", (redV, redF), RED, "head", fuse=False)

# Grey crown feathers raised behind the comb, swept up and back (the Mega's own, pale at their points)
def spike(root, tip, r):
    root, tip = np.array(root, float), np.array(tip, float)
    a, b = np.cross(tip - root, (1.0, 0, 0)), np.array([1.0, 0, 0])
    a /= np.linalg.norm(a)
    pts = [root + r * (math.cos(t) * a * 0.7 + math.sin(t) * b) for t in np.linspace(0, 2 * math.pi, 8, endpoint=False)]
    return hull(pts + [tip])
for i, (x, y0, z0, y1, z1, r) in enumerate([(0.0, -0.04, 0.86, 0.12, 1.0, 0.04), (0.045, -0.02, 0.84, 0.15, 0.95, 0.034),
                                             (-0.045, -0.02, 0.84, 0.15, 0.95, 0.034), (0.0, 0.03, 0.8, 0.22, 0.88, 0.04)]):
    k.add(f"crown{i}", spike((x, y0, z0), (x * 1.6, y1, z1), r), "#B8B2B8", "head", fuse=False)

# The beak: a short hooked wedge of yellow-orange, the point a little deeper orange
def pointed(V):
    t = np.clip(-V[:, 1], 0, 1)
    V = V.copy(); V[:, 0] *= 1 - 0.7 * t; V[:, 2] *= 1 - 0.6 * t
    return V
k.add("beak", ell((0, -0.3, 0.685), (0.05, 0.068, 0.04), rot=(8, 0, 0), deform=pointed), ORANGE, "head", fuse=False,
      surface="shell")
k.add("beaktip", tube([(0, -0.34, 0.68), (0, -0.37, 0.668), (0, -0.388, 0.65)], [0.019, 0.013, 0.006]), TIP, "head",
      fuse=False, surface="shell")

# The red flaps hanging from each cheek down the side of the chest, flat side to side and pointed
k.add("flap", blade([(0.1, -0.23, 0.68), (0.135, -0.21, 0.6), (0.16, -0.17, 0.52), (0.165, -0.13, 0.46)],
                    [0.036, 0.04, 0.03, 0.008], (1, 0, 0), 0.5), RED, "head", fuse=False, mirror=True)

# The wings: raised in a V some 50 degrees, swept a little back, their faces turned to the front and the back.
# s runs out along the arm, c out across it (the feathers' way), n square to both (pointing back).
SHO = np.array([0.15, 0.0, 0.57])
S_ = unit(0.6, 0.25, 0.75)
c0 = unit(0.75, 0.15, -0.6)
C_ = c0 - S_ * (c0 @ S_); C_ /= np.linalg.norm(C_)
N_ = np.cross(S_, C_)
ARM = 0.3
WRIST = SHO + S_ * ARM


def turn(a):
    """A direction in the wing's plane a degrees from the arm's line toward c."""
    r = math.radians(a)
    return S_ * math.cos(r) + C_ * math.sin(r)


def feather(name, root, d, L, w, col, off, thick=0.03):
    d = d / np.linalg.norm(d)
    ez = np.cross(N_, d)
    outline = [(-0.01, -w * 0.4), (L * 0.5, -w / 2), (L * 0.82, -w * 0.34), (L, 0), (L * 0.82, w * 0.34),
               (L * 0.5, w / 2), (-0.01, w * 0.4)]
    k.add(name, slab(outline, root + N_ * off, d, ez, thick), col, "wing{s}", fuse=False, mirror=True, surface="fur")


# the arm, grey, with three pale gold bands across its middle
k.add("arm", tube([SHO, SHO + S_ * ARM * 0.55, WRIST], [0.052, 0.045, 0.034]), WING, "wing{s}", fuse=False, mirror=True)
for i, t in enumerate((0.35, 0.53, 0.71)):
    rr = 0.052 + (0.034 - 0.052) * t + 0.005
    k.add(f"band{i}", ring_band(SHO + S_ * ARM * t, S_, rr, 0.03, thick=0.007), PALE, "wing{s}", fuse=False,
          mirror=True)
# primaries fanning from the wrist, from up along the arm to out across it; secondaries hanging from the forearm
for i, (a, L) in enumerate([(4, 0.33), (22, 0.35), (40, 0.33), (58, 0.3), (76, 0.27)]):
    feather(f"prim{i}", WRIST - S_ * 0.03 * i, turn(a), L, 0.085, FLIGHT if i % 2 == 0 else FLIGHT2, 0.006 * i)
for i, (t, a, L) in enumerate([(0.8, 92, 0.25), (0.6, 100, 0.24), (0.4, 108, 0.22), (0.2, 116, 0.19)]):
    feather(f"sec{i}", SHO + S_ * ARM * t, turn(a), L, 0.085, FLIGHT2 if i % 2 == 0 else FLIGHT, 0.03 + 0.006 * i)
# grey coverts in front of the flight feathers' roots
for i, (t, a, L) in enumerate([(0.95, 45, 0.15), (0.75, 75, 0.15), (0.5, 95, 0.14), (0.25, 110, 0.13)]):
    feather(f"cov{i}", SHO + S_ * ARM * t, turn(a), L, 0.09, "#B4AEB4", -0.03 - 0.006 * i)

# The tail: a long fan of grey feathers hanging down behind, each tipped pale gold, spread side to side
for i, (ang, side, length) in enumerate([(-22, 0.12, 0.4), (-22, -0.12, 0.4), (-27, 0.06, 0.46), (-27, -0.06, 0.46),
                                         (-31, 0.0, 0.5)]):
    r = math.radians(ang)
    d = np.array([side * 1.3, math.cos(r), math.sin(r)]); d /= np.linalg.norm(d)
    base = np.array([side * 0.5, 0.2, 0.39])
    sq = np.array([0, 0, 1.0]) - d * d[2]; sq /= np.linalg.norm(sq)
    k.add(f"tail{i}", blade([base, base + d * length * 0.45, base + d * length * 0.84], [0.05, 0.06, 0.05], sq, 0.45),
          "#B4AEB4", "tail", fuse=False)
    k.add(f"tailtip{i}", blade([base + d * length * 0.83, base + d * length * 0.92, base + d * length],
                               [0.051, 0.038, 0.006], sq, 0.47), PALE, "tail", fuse=False)

# Legs: shaggy grey feathers over the top, short golden shanks hanging forward, toes curled to grip, black claws
for s, side in ((1, "R"), (-1, "L")):
    x = 0.08 * s
    k.add(f"leg{side}", tube([(x, -0.02, 0.22), (x * 1.05, -0.05, 0.15), (x * 1.08, -0.075, 0.1)],
                             [0.03, 0.027, 0.026]), GOLD, f"leg{side}", fuse=False, surface="scales")
    for j, ang in enumerate((-80, -45, -10, 25, 60, 100, 150, 200, 250)):
        rr = math.radians(ang)
        d = np.array([math.sin(rr), -math.cos(rr), 0])
        b0 = np.array([x * 1.02, -0.02, 0.215]) + d * 0.03
        t0 = b0 + d * 0.035 + np.array([0, 0, -0.055 - 0.01 * (j % 2)])
        k.add(f"fluff{side}{j}", blade([b0, t0], [0.024, 0.004], np.cross(d, (0, 0, 1.0)), 0.4), "#9A949A", f"leg{side}",
              fuse=False)
    foot = np.array([x * 1.08, -0.08, 0.09])
    for j, ang in enumerate((-30, 0, 30, 180)):
        rr = math.radians(ang)
        d = np.array([math.sin(rr), -math.cos(rr), 0])
        ln = 0.07 if ang != 180 else 0.045
        mid = foot + d * ln * 0.7 + np.array([0, 0, -0.01])
        tip = foot + d * ln + np.array([0, 0, -0.05])
        k.add(f"toe{side}{j}", tube([foot, mid, tip], [0.025, 0.022, 0.018]), GOLD, f"leg{side}", fuse=False,
              surface="scales")
        cd = unit(*(d * 0.3 + np.array([0, 0, -1.0]))) if ang != 180 else unit(*(d * 0.5 + np.array([0, 0, -1.0])))
        cb = np.cross(cd, (1.0, 0, 0) if abs(cd[0]) < 0.9 else (0, 1.0, 0)); cb /= np.linalg.norm(cb)
        cc = np.cross(cd, cb)
        root = tip + cd * 0.005
        pts = [root + 0.014 * (math.cos(t) * cb + math.sin(t) * cc) for t in np.linspace(0, 2 * math.pi, 6, endpoint=False)]
        k.add(f"claw{side}{j}", hull(pts + [tip + cd * 0.04 - d * 0.012]), CLAW, f"leg{side}", fuse=False,
              surface="shell")

# --- the eyes (Staraptor's) -----------------------------------------------------------------------------------------
WHITEC, IRIS, PUPIL = srgb(WHITE), srgb("#E04A3C"), srgb("#1A1418")
CG, CI = srgb(MASK), srgb("#2A262A")


def clipped(cx, cy, rx, ry, top, n=96):
    pts = []
    for th in np.linspace(0, 2 * math.pi, n, endpoint=False):
        du, dv = rx * math.cos(th), ry * math.sin(th)
        t = 1.0
        if cy + dv > float(top(cx + du)):
            lo, hi = 0.0, 1.0
            for _ in range(40):
                t = (lo + hi) / 2
                if cy + t * dv > float(top(cx + t * du)):
                    hi = t
                else:
                    lo = t
            t = lo
        pts.append((t * math.cos(th), t * math.sin(th)))
    return pts


SC = (0.0, -0.05, 0.95, 0.85)
IR = (-0.04, 0.0, 0.8, 0.8)
PU = (-0.08, -0.1, 0.34, 0.36)
GL = (0.18, -0.02, 0.14, 0.14)


def fierce_lid():
    cx, cy, r = IR[0], IR[1], IR[2] * 1.04
    inside_lid = lambda u, v: (v > cut("Fierce", u) - 1e-9) & (v < eye_top(u) + 0.06 + 1e-9)
    pts = []
    for th in np.linspace(0, 2 * math.pi, 240, endpoint=False):
        u, v = cx + r * math.cos(th), cy + r * math.sin(th)
        if inside_lid(u, v):
            pts.append((u, v))
    for u in np.linspace(cx - r, cx + r, 89):
        for v in (float(cut("Fierce", u)), float(eye_top(u)) + 0.06):
            if (u - cx) ** 2 + (v - cy) ** 2 <= r * r and inside_lid(u, v):
                pts.append((u, v))
    pts = np.array(pts)
    mid = pts.mean(axis=0)
    rel = pts - mid
    rel = rel[np.argsort(np.arctan2(rel[:, 1], rel[:, 0]))]
    return mid, [tuple(p) for p in rel]


LID_MID, LID_PTS = fierce_lid()


def place(side, cx, cy):
    return (E + ERI * cx * HE + EUP * cy * HE) * np.array([side, 1, 1.0])


def piece(state, side, shape, look, eps, rings, segs, top=None):
    cx, cy, rx, ry = shape
    pts = clipped(cx, cy, rx, ry, top or (lambda u: cut(state, u)))
    outline = capped([(side * a, b) for a, b in pts])
    def col(a, b):
        return look(cx + side * a * rx, cy + b * ry)
    k.decal(place(side, cx, cy), de * np.array([side, 1, 1.0]), (rx * HE, ry * HE), col, eps=eps, outline=outline,
            rings=rings, segs=segs)


def eyes(state):
    for side in (1, -1):
        if state in ("Open", "Fierce"):
            piece(state, side, IR, lambda u, v: np.tile(IRIS, (len(u), 1)), 0.007, 14, 64, top=eye_top)
            piece(state, side, PU, lambda u, v: np.tile(PUPIL, (len(u), 1)), 0.009, 6, 32, top=eye_top)
            piece(state, side, GL, lambda u, v: np.tile(WHITEC, (len(u), 1)), 0.011, 4, 24, top=eye_top)
            if state == "Fierce":
                k.decal(place(side, *LID_MID), de * np.array([side, 1, 1.0]), (HE, HE), CG, eps=0.013,
                        outline=capped([(side * a, b) for a, b in LID_PTS]), rings=6, segs=72)
        else:
            open_look = lambda u, v: np.tile(WHITEC, (len(u), 1))
            piece(state, side, SC, eye_look(state, open_look, WHITEC, CI), 0.006, 20, 56,
                  top=lambda u: eye_top(u) - 0.06)


kit.build(voxel=0.0072)
kit.eye_states(eyes)
kit.export(out)
