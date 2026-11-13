# Staraptor sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/staraptor.py -- <the export's Staraptor.glb> <out.glb>
# Staravia grown: the same grey-brown body, dark head and neck, white face and chest and orange beak with a dark point,
# but a plump, forward-leaning hawk: one great black comb that rises from the crown and sweeps forward over the face, its
# tip hanging past the beak and its front and underside red behind a zigzag; a white forehead between the eyes; a dark
# grey mask over and behind the small fierce red eyes that runs back into a pointed grey tuft from the nape; a black bib
# in points over the white chest; wings folded tight to the sides with dark tips; a raised fan of grey feathers tipped
# black; short shaggy thighs over yellow-orange feet with dark talons.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, tri, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

BODY, DARK, WHITE, MASK, WING = "#7C7276", "#3B373B", "#F2F2F0", "#666064", "#776E72"
ORANGE, TIP, RED, CLAW, FEATHER = "#F4A92A", "#363236", "#E5483D", "#2C282B", "#8A8286"
H = np.array([0, -0.165, 0.725])          # the head's centre, carried forward of the head's joint on a thick neck
HR = np.array([0.135, 0.145, 0.135])


# --- helpers ------------------------------------------------------------------------------------------------------
def in_poly(x, y, pts):
    pts = np.array(pts, float)
    res = np.zeros(len(x), bool)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        res ^= ((y1 > y) != (y2 > y)) & (x < (x2 - x1) * (y - y1) / (y2 - y1 + 1e-12) + x1)
    return res


def poly_mask(x, y, pts, edge=0.0025):
    """A polygon painted with a near-voxel edge: the signed distance to its outline (negative inside), eased."""
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
    """A patch's outline from a polygon, never further out than its furthest point."""
    r = polygon(pts)
    far = max(math.hypot(a, b) for a, b in pts)
    return lambda th: min(r(th), far)


def spline(pts, per=10):
    """An open Catmull-Rom curve through the points, per points a span, the last point kept; and each point's t."""
    p = np.array(pts, float)
    e = np.vstack([2 * p[0] - p[1], p, 2 * p[-1] - p[-2]])
    out_ = []
    for i in range(len(p) - 1):
        p0, p1, p2, p3 = e[i], e[i + 1], e[i + 2], e[i + 3]
        for j in range(per):
            t = j / per
            out_.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                               + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    out_.append(p[-1])
    return np.array(out_)


def comb(spine, depth, width, red_at, seg=48, per=16):
    """
    The crest: a slab lofted along a curve in the side's plane, each section an ellipse of depth (in the plane, across
    the curve) and width (side to side). Returns two meshes sharing the seam, the black and the red, split by face:
    red_at(t, s) says whether a face at t along the curve (0 base, 1 tip) and s round its section (-1 the curve's
    inner, concave side, +1 its outer) is red.
    """
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
    # outward normals: the loft winds one way; check against the curve's middle and turn every face if inward
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
# Plump and round, leaning forward like a big rooster: the breast full and high in front, the back sloping to the tail
def plump(V):
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.28 * t ** 1.4
    return V
k.add("body", ell((0, 0.05, 0.45), (0.235, 0.27, 0.225), rot=(-14, 0, 0), deform=plump), BODY, "body")
k.add("breast", ell((0, -0.085, 0.52), (0.205, 0.165, 0.2), deform=plump), BODY, "body")
k.add("thigh", ell((0.085, 0.0, 0.255), (0.07, 0.075, 0.07)), BODY, "leg{s}", mirror=True)
# a thick neck carrying the head forward
k.add("neck", ell((0, -0.13, 0.62), (0.13, 0.13, 0.13), rot=(-25, 0, 0)), DARK, "head")
k.add("head", ell(H, HR), DARK, "head")
k.add("cap", ell((0, -0.08, 0.81), (0.07, 0.115, 0.075)), DARK, "head")          # the crest's root, filling under it
# The grey-black tuft from the nape, pointing back and up behind the crest: the mask's own end
k.add("plume", blade([(0, -0.12, 0.78), (0, -0.01, 0.83), (0, 0.1, 0.9), (0, 0.21, 0.975)],
                     [0.09, 0.075, 0.042, 0.004], (1, 0, 0), 0.4), MASK, "head")

# Wings folded tight along the sides, merged at the shoulder, their tips low at the back over the tail's root
k.add("wing", blade([(0.17, -0.02, 0.53), (0.185, 0.08, 0.475), (0.175, 0.21, 0.385), (0.13, 0.33, 0.29)],
                    [0.095, 0.115, 0.085, 0.01], (1, 0, 0), 0.3), WING, "wing{s}", mirror=True)

# The upper back and nape: the head's dark comes down the back of the neck to the shoulders
def nape(P):
    th = np.arctan2(P[:, 0], P[:, 1] + 0.06)          # 0 straight behind
    return inside(0.625 + 0.02 * tri(th * 5 / (2 * math.pi)) - P[:, 2], 0.004) * (np.abs(th) < 2.0)
k.paint(nape, DARK, on=["body", "breast"])
# the wings' tips darker, in points
k.paint(lambda P: inside(0.22 + 0.02 * tri(P[:, 2] / 0.035) - P[:, 1], 0.003), TIP, on=["wing"])
# a pale scribble on each wing, as on the art
k.paint(lambda P: inside(np.abs(P[:, 2] - (0.47 - 0.3 * (P[:, 1] - 0.04) + 0.012 * tri(P[:, 1] / 0.03))) - 0.008, 0.003)
        * (P[:, 1] > -0.01) * (P[:, 1] < 0.12) * (np.abs(P[:, 0]) > 0.2), "#968E92", on=["wing"])

# The face and throat: white down the front, round the head's axis
R = 0.15
def round_head(P):
    P = np.atleast_2d(P)
    return np.arctan2(P[:, 0], -(P[:, 1] - H[1])) * R, P[:, 2]

# The eyes' make, in an eye's own units (u outward, v up, one unit HE): a small round eye cut along its top by the grey
# mask, which slants down toward the beak; the Fierce lid comes lower still toward the beak
HE = 0.035
SL = 0.32
def eye_top(u):
    return 0.22 + SL * u
def cut(state, u):
    return np.minimum(eye_top(u), -0.02 + 0.55 * u) if state == "Fierce" else eye_top(u)
de = unit(0.52, -0.84, 0.16)
E = H + de / np.linalg.norm(de / HR)               # on the head's ellipsoid along de
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

# The mask, one a side: its lower edge is the eye's cut from the eye's inner corner outward, dropping behind the eye
# to wrap its back; its upper edge runs back over the temple into the tuft. The forehead between stays white.
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
k.paint(lambda P: (P[:, 1] > -0.13) * (P[:, 2] > 0.74) * (np.abs(P[:, 0]) < 0.1), MASK, on=["plume"])

# The white chest and throat, the black bib crossing it in points, the grey-brown belly below
CR = 0.22
def round_body(P):
    return np.arctan2(P[:, 0], -(P[:, 1] - 0.0)) * CR, P[:, 2]
CHEST = mirrored([(0.0, 0.72), (0.12, 0.72), (0.15, 0.63), (0.2, 0.55), (0.25, 0.46), (0.26, 0.38), (0.22, 0.34),
                  (0.19, 0.365), (0.16, 0.33), (0.12, 0.355), (0.08, 0.32), (0.04, 0.345), (0.0, 0.315)])
FRONT = lambda P: (P[:, 1] < 0.02) * (P[:, 2] < 0.72) * (P[:, 2] > 0.2)
k.paint(lambda P: poly_mask(*round_body(P), CHEST) * FRONT(P), WHITE, on=["body", "breast", "neck"])
BIB = mirrored([(0.0, 0.52), (0.04, 0.56), (0.075, 0.62), (0.1, 0.7), (0.12, 0.73), (0.42, 0.73), (0.42, 0.42),
                (0.33, 0.49), (0.29, 0.41), (0.245, 0.5), (0.2, 0.43), (0.155, 0.51), (0.11, 0.45),
                (0.06, 0.525), (0.0, 0.47)])
k.paint(lambda P: poly_mask(*round_body(P), BIB) * FRONT(P), DARK, on=["body", "breast", "neck"])

# --- loose pieces -------------------------------------------------------------------------------------------------------

# The crest: one great comb, thick enough to read from the front, rising from the crown behind the forehead, its back
# edge nearly upright, sweeping forward and down to a tip that hangs past the beak. Its underside and front third are
# red behind a zigzag; the top and back stay black, so no red shows from behind.
def red_at(t, s):
    # the line runs across the comb, a zigzag slanting back toward the top: everything forward of it red, top, flanks
    # and underside alike; everything behind it black
    return t > 0.44 + 0.09 * s + 0.045 * tri(s * 1.6 + 0.25)
# a sail of a comb: the back edge upright to a peak at the top-back, a long leading edge down to the tip at the beak's
# height, the underside close over the forehead
SPINE = [(0, -0.1, 0.79), (0, -0.09, 0.9), (0, -0.13, 0.965), (0, -0.22, 0.935), (0, -0.31, 0.865),
         (0, -0.38, 0.8), (0, -0.43, 0.74)]
(blackV, blackF), (redV, redF) = comb(SPINE, [0.1, 0.11, 0.1, 0.085, 0.062, 0.036, 0.006],
                                      [0.066, 0.063, 0.055, 0.045, 0.035, 0.022, 0.006], red_at)
k.add("crest", (blackV, blackF), DARK, "head", fuse=False)
k.add("crestred", (redV, redF), RED, "head", fuse=False)

# The beak: a short hooked wedge of yellow-orange and a black point drooping off its tip
def pointed(V):
    t = np.clip(-V[:, 1], 0, 1)
    V = V.copy(); V[:, 0] *= 1 - 0.7 * t; V[:, 2] *= 1 - 0.6 * t
    return V
k.add("beak", ell((0, -0.3, 0.685), (0.05, 0.068, 0.04), rot=(8, 0, 0), deform=pointed), ORANGE, "head", fuse=False,
      surface="shell")
k.add("beaktip", tube([(0, -0.34, 0.68), (0, -0.372, 0.668), (0, -0.395, 0.648)], [0.019, 0.012, 0.004]), TIP, "head",
      fuse=False, surface="shell")

# The tail: a raised fan of grey feathers, each tipped black
# spread up and down like a rooster's, seen whole from the side, each feather flat side to side
for i, (ang, side, length) in enumerate([(14, 0.085, 0.3), (14, -0.085, 0.3), (27, 0.055, 0.35), (27, -0.055, 0.35),
                                         (40, 0.02, 0.38), (40, -0.02, 0.38), (53, 0.0, 0.38), (66, 0.0, 0.33)]):
    r = math.radians(ang)
    d = np.array([side * 1.2, math.cos(r), math.sin(r)]); d /= np.linalg.norm(d)
    base = np.array([side * 0.6, 0.2, 0.41])
    sq = np.array([1.0, 0, 0]) - d * d[0]; sq /= np.linalg.norm(sq)
    k.add(f"tail{i}", blade([base, base + d * length * 0.45, base + d * length * 0.75], [0.05, 0.06, 0.05], sq, 0.25),
          FEATHER, "tail", fuse=False)
    k.add(f"tailtip{i}", blade([base + d * length * 0.72, base + d * length * 0.86, base + d * length],
                               [0.051, 0.04, 0.005], sq, 0.27), TIP, "tail", fuse=False)

# Short legs: shaggy feathers over the top, yellow-orange shanks, three toes forward and one back, black talons
for s, side in ((1, "R"), (-1, "L")):
    x = 0.08 * s
    k.add(f"leg{side}", tube([(x, -0.02, 0.22), (x * 1.04, -0.03, 0.11), (x * 1.07, -0.04, 0.035)], [0.028, 0.024, 0.022]),
          ORANGE, f"leg{side}", fuse=False, surface="scales")
    for j, ang in enumerate((-80, -45, -10, 25, 60, 100, 150, 200, 250)):
        rr = math.radians(ang)
        d = np.array([math.sin(rr), -math.cos(rr), 0])
        b0 = np.array([x * 1.02, -0.02, 0.215]) + d * 0.03
        t0 = b0 + d * 0.035 + np.array([0, 0, -0.055 - 0.01 * (j % 2)])
        k.add(f"fluff{side}{j}", blade([b0, t0], [0.022, 0.003], np.cross(d, (0, 0, 1.0)), 0.4), "#5A5357", f"leg{side}",
              fuse=False)
    base = np.array([x * 1.07, -0.045, 0.025])
    for j, ang in enumerate((-34, 0, 34, 180)):
        rr = math.radians(ang)
        d = np.array([math.sin(rr), -math.cos(rr), 0])
        tip = base + d * (0.095 if ang != 180 else 0.05) + np.array([0, 0, -0.012])
        k.add(f"toe{side}{j}", tube([base, (base + tip) / 2 + np.array([0, 0, 0.005]), tip], [0.019, 0.016, 0.013]),
              ORANGE, f"leg{side}", fuse=False, surface="scales")
        k.add(f"claw{side}{j}", tube([tip, tip + (d + np.array([0, 0, -0.55])) * 0.032], [0.011, 0.0015]), CLAW,
              f"leg{side}", fuse=False, surface="shell")

# --- the eyes -------------------------------------------------------------------------------------------------------------
# No rim and no white of its own (the face is the white): a plain red iris, a black pupil and a glint, each cut straight
# along the top where the grey mask comes down over it (the mask painted just under the cut, so the patch's edge is the
# line). The Fierce lid is a mask-coloured patch over the iris's top. The patches are laid by hand, u toward +X.
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


SC = (0.0, -0.05, 0.95, 0.85)                 # the closed eye's ground (the face's white) for its line
IR = (-0.04, 0.0, 0.76, 0.76)                 # the iris, its top third under the mask
PU = (-0.08, -0.1, 0.3, 0.32)                 # the pupil
GL = (0.18, -0.02, 0.14, 0.14)                # the glint


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


k.build(voxel=0.0072)
k.eye_states(eyes)
k.export(out)
