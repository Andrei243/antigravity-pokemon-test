# Luxio sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/luxio.py -- <the export's Luxio.glb> <out.glb>
# Shinx grown: the same blue and black and yellow, but fiercer eyes cut by the black of the brow, longer legs, a black mane
# (a spike on the crown, locks down the cheeks, a jagged ruff under the chin), two yellow rings on every leg, and a
# longer tail whose star hangs low beside the body.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

BLUE, BLACK, YELLOW, NOSE, INK = "#68B6E6", "#2E2E38", "#F7C832", "#E8505A", "#24242E"
H = np.array([0, -0.2, 0.64])           # the head's centre
HR = np.array([0.175, 0.16, 0.16])


def in_poly(x, y, pts):
    pts = np.array(pts, float)
    res = np.zeros(len(x), bool)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        res ^= ((y1 > y) != (y2 > y)) & (x < (x2 - x1) * (y - y1) / (y2 - y1 + 1e-12) + x1)
    return res


def poly_mask(x, y, pts, edge=0.0035):
    """A polygon painted with a soft edge: the signed distance to its outline (negative inside), eased."""
    pts = np.array(pts, float)
    d = np.full(len(x), 9.0)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        ex, ey = x2 - x1, y2 - y1
        t = np.clip(((x - x1) * ex + (y - y1) * ey) / (ex * ex + ey * ey + 1e-12), 0, 1)
        d = np.minimum(d, np.hypot(x - x1 - t * ex, y - y1 - t * ey))
    return inside(np.where(in_poly(x, y, pts), -d, d), edge)


def mirrored(half):
    return half + [(-a, z) for a, z in half[::-1][1:-1]]


# The head, black but for the face; a short muzzle; a blue chest and a black rump
k.add("head", ell(H, HR), BLACK, "head")
k.add("muzzle", ell((0, -0.305, 0.6), (0.085, 0.06, 0.06)), BLUE, "head")
k.add("chest", ell((0, -0.1, 0.44), (0.145, 0.18, 0.15)), BLUE, "body")
k.add("rump", ell((0, 0.17, 0.44), (0.135, 0.17, 0.135)), BLACK, "body")

# The eyes' make, in an eye's own units (u outward, v up, one unit HE): a round eye whose top is cut off straight,
# slanting down toward the nose, by the black fur of the forehead; the Fierce lid comes lower toward the nose
HE = 0.05
SL = 0.45
def eye_top(u):
    return 0.3 + SL * u
def cut(state, u):
    return np.minimum(eye_top(u), 0.12 + 0.6 * u) if state == "Fierce" else eye_top(u)
de = unit(0.37, -0.92, 0.07)
E = np.array([0.07, -0.346, 0.652])
def frame(n):
    n = np.array(n, float); n /= np.linalg.norm(n)
    right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
    return right, np.cross(n, right)
ERI, EUP = frame(de)
R = 0.17
def round_head(P):
    """(the arc round the head's upright axis, z) of points: the coordinates the face's paint is measured in."""
    P = np.atleast_2d(P)
    return np.arctan2(P[:, 0], -(P[:, 1] - H[1])) * R, P[:, 2]
def on_head(u, v):
    """Where an eye's (u, v) lands on the head (its ellipsoid), cast along the eye's normal as a patch is."""
    o = E + de * 0.35 + ERI * u * HE + EUP * v * HE
    oc, dd = (o - H) / HR, -de / HR
    a, b, c = dd @ dd, 2 * oc @ dd, oc @ oc - 1
    return o + dd * HR * (-b - math.sqrt(max(b * b - 4 * a * c, 0))) / (2 * a)

# The black of the crown comes down over the forehead in one shape: along the top of each eye (just under the eye's
# own cut, so the eye's edge is the line), down to a point between them at the nose, and out over the temples to the
# mane; the face is blue below it, round the cheeks to the locks, down to the ruff under the chin
brow = []
for u in np.linspace(-1.8, 1.7, 36):
    s, z = round_head(on_head(u, eye_top(u) - 0.2))
    if s[0] > 0.004:
        brow.append((float(s[0]), float(z[0])))
(s0, z0), (s1, z1) = brow[0], brow[1]
point = z0 - (z1 - z0) / (s1 - s0) * s0
FACE = mirrored([(0.0, point)] + brow + [(0.178, brow[-1][1] - 0.012), (0.182, 0.655), (0.175, 0.6), (0.155, 0.555), (0.1, 0.53),
                                        (0.06, 0.53), (0.0, 0.52)])
FRONT = lambda P: (P[:, 1] < H[1] + 0.05) * (np.abs(np.arctan2(P[:, 0], -(P[:, 1] - H[1]))) < 1.35)
def face(P):
    s, z = round_head(P)
    return poly_mask(s, z, FACE, edge=0.003) * FRONT(P) * (P[:, 2] > 0.47)
k.paint(lambda P: FRONT(P) * (P[:, 2] > 0.6), BLACK, on=["head", "muzzle"])
k.paint(face, BLUE)

# The spike on the crown, leaning back and to one side, and a shorter one behind it
k.add("crown", blade([(0, -0.2, 0.76), (-0.01, -0.19, 0.86), (-0.035, -0.16, 0.95), (-0.07, -0.12, 1.0)],
                     [0.06, 0.04, 0.02, 0.004], (1, 0, 0), 0.5), BLACK, "head")
k.add("crown2", blade([(0, -0.12, 0.77), (0.01, -0.08, 0.84), (0.02, -0.03, 0.88)], [0.04, 0.022, 0.004], (1, 0, 0),
                      0.5), BLACK, "head")

# Locks of the mane down the cheeks to the shoulders, and the jagged ruff under the chin down the chest
for i, (a, m, b, r) in enumerate([((0.15, -0.24, 0.58), (0.22, -0.23, 0.5), (0.26, -0.21, 0.37), 0.065),
                                  ((0.15, -0.15, 0.62), (0.23, -0.13, 0.57), (0.29, -0.1, 0.5), 0.06),
                                  ((0.12, -0.14, 0.52), (0.18, -0.12, 0.42), (0.2, -0.1, 0.33), 0.055)]):
    k.add(f"lock{i}", blade([a, m, b], [r, r * 0.7, 0.004], (1, 0.3, 0), 0.45), BLACK, "head", mirror=True)
for i, (x, top, low) in enumerate([(0.0, 0.52, 0.4), (0.06, 0.51, 0.37), (0.12, 0.49, 0.34)]):
    y = -0.27 + 0.02 * i * i
    k.add(f"ruff{i}", blade([(x, y, top), (x * 1.05, y + 0.005, (top + low) / 2), (x * 1.1, y + 0.02, low)],
                            [0.045, 0.03, 0.004], (0, 1, 0), 0.45), BLACK, "head", mirror=i > 0)

# Big round ears at the head's upper corners, tilted a little out, a yellow oval within each
EC = np.array([0.165, -0.18, 0.815])
ER = (0, 26, 10)
k.add("ear", ell(EC, (0.08, 0.027, 0.094), rot=ER), BLUE, "ear{s}", mirror=True)

# Legs: blue in front; black thighs behind going blue below; two yellow rings above every paw
k.add("leg", tube([(0.115, -0.17, 0.42), (0.12, -0.18, 0.05)], [0.062, 0.054]), BLUE, "leg{s}", mirror=True)
k.add("paw", ell((0.12, -0.2, 0.035), (0.058, 0.07, 0.036)), BLUE, "leg{s}", mirror=True)
k.add("thigh", ell((0.105, 0.21, 0.36), (0.075, 0.1, 0.12)), BLACK, "hind{s}", mirror=True)
k.add("hind", tube([(0.12, 0.22, 0.36), (0.12, 0.21, 0.05)], [0.06, 0.052]), BLACK, "hind{s}", mirror=True)
k.add("hpaw", ell((0.12, 0.19, 0.035), (0.058, 0.07, 0.036)), BLUE, "hind{s}", mirror=True)
k.paint(lambda P: inside(P[:, 2] - 0.19, 0.0025) * (P[:, 1] > 0.05), BLUE)
k.paint(lambda P: inside(np.minimum(np.abs(P[:, 2] - 0.155), np.abs(P[:, 2] - 0.095)) - 0.014, 0.0025)
        * (P[:, 2] < 0.2), YELLOW)

# Black spikes of fur on the back behind the mane
for i, (a, b, r) in enumerate([((0.07, 0.04, 0.55), (0.12, 0.15, 0.62), 0.04),
                               ((0.12, 0.08, 0.48), (0.21, 0.16, 0.52), 0.035),
                               ((0.0, 0.1, 0.56), (0.0, 0.22, 0.62), 0.035)]):
    k.add(f"spike{i}", blade([a, b], [r, 0.004], (0, 0, 1) if i < 2 else (1, 0, 0), 0.5), BLACK, "body", mirror=i < 2)

# The blue front meets the black behind in a crisp line, leaning forward toward the top
def behind(P):
    return 0.04 - 0.15 * (P[:, 2] - 0.42) - P[:, 1]
BODY = lambda P: (P[:, 2] > 0.25) * (P[:, 2] < 0.6) * (P[:, 1] > -0.1)
k.paint(lambda P: inside(behind(P), 0.0025) * BODY(P), BLACK)
# and the back's black runs forward over the top of the shoulders to the mane
k.paint(lambda P: inside(0.53 - 0.5 * (P[:, 1] + 0.12) - P[:, 2], 0.0025) * (P[:, 1] > -0.2) * (P[:, 2] < 0.66), BLACK)

# The long black tail: back, out to the side and down, the star hanging low beside the body
k.add("tail", tube([(0, 0.3, 0.48), (0.05, 0.44, 0.55), (0.17, 0.56, 0.56), (0.3, 0.62, 0.48), (0.37, 0.62, 0.36)],
                   [0.028, 0.025, 0.023, 0.021, 0.02]), BLACK, "tail")
def star(V):
    V = V.copy()
    th = np.arctan2(V[:, 2], V[:, 1])
    p = 0.7
    s = (np.abs(np.cos(th)) ** p + np.abs(np.sin(th)) ** p) ** (-1 / p)
    V[:, 1] *= s; V[:, 2] *= s
    return V
k.add("star", ell((0.385, 0.61, 0.25), (0.032, 0.13, 0.13), rot=(0, 0, -55), deform=star), YELLOW, "tail", fuse=False,
      surface="shell")

# The ears' yellow ovals, crisp
m = np.array(Euler([math.radians(a) for a in ER]).to_matrix())
en = m @ np.array([0, -1.0, 0]); eu = m @ np.array([0, 0, 1.0])
k.decal(EC + en * 0.024 - eu * 0.011, en, (0.045, 0.06), YELLOW, up=eu, eps=0.003, rings=10, segs=48, mirror=True)

# A red nose like a triangle and a small frown
def nose(V):
    V = V.copy()
    t = np.clip((V[:, 2] + 1) / 2, 0, 1)
    V[:, 0] *= 0.25 + 0.85 * t
    return V
k.add("nose", ell((0, -0.362, 0.623), (0.022, 0.012, 0.015), deform=nose), NOSE, "head", fuse=False, surface="skin")
CB, CI = srgb(BLUE), srgb(INK)
def frown(u, v):
    c = np.tile(CB, (len(u), 1))
    c[(np.abs(v - (0.15 - 0.75 * u * u)) < 0.15) & (np.abs(u) < 0.8)] = CI    # the small frown
    c[(np.abs(u) < 0.09) & (v > 0.05)] = CI                                  # and the short line down from the nose
    return c
k.decal((0, -0.362, 0.59), unit(0, -1, -0.25), (0.028, 0.022), frown, eps=0.003, rings=16, segs=72)

# The eyes: no rim; white, a big yellow iris with a darker yellow-orange ring, a small black pupil of its own, each
# piece's outline cut straight along the top where the forehead's black comes down over it, so the line is the
# patches' own edge (the black is painted under it). The Fierce lid is a black patch of its own over the white's top.
# The patches are laid by hand, u toward +X on both sides.
WHITE, IRIS, RING, PUPIL = np.array([1.0, 1.0, 1.0]), srgb(YELLOW), srgb("#D08A1A"), srgb("#16161C")
def clipped(cx, cy, rx, ry, top, n=96):
    """An ellipse about (cx, cy) in eye units cut off above v = top(u): a patch's outline in its own units."""
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
SC = (0.0, 0.0, 1.0, 0.95)                    # the white: middle and half sizes in eye units
IR = (-0.12, -0.1, 0.66, 0.66)                # the iris
PU = (-0.14, -0.1, 0.12, 0.23)                # the pupil
def fierce_lid():
    """The Fierce lid: the white above the lid's line, and a little past the white's top; convex, so its outline is
    its points sorted round their middle. Returns its middle and outline in eye units."""
    inside_lid = lambda u, v: (v > cut("Fierce", u) - 1e-9) & (v < eye_top(u) + 0.06 + 1e-9)
    pts = []
    for th in np.linspace(0, 2 * math.pi, 240, endpoint=False):
        u, v = 1.04 * math.cos(th), 0.99 * math.sin(th)
        if inside_lid(u, v):
            pts.append((u, v))
    for u in np.linspace(-1.1, 1.1, 89):
        for v in (float(cut("Fierce", u)), float(eye_top(u)) + 0.06):
            if (u / 1.04) ** 2 + (v / 0.99) ** 2 <= 1 and inside_lid(u, v):
                pts.append((u, v))
    pts = np.array(pts)
    mid = pts.mean(axis=0)
    rel = pts - mid
    rel = rel[np.argsort(np.arctan2(rel[:, 1], rel[:, 0]))]
    return mid, [tuple(p) for p in rel]
LID_MID, LID_PTS = fierce_lid()
def place(side, cx, cy):
    return (E + ERI * cx * HE + EUP * cy * HE) * np.array([side, 1, 1.0])
def piece(state, side, shape, look, eps, rings, segs):
    cx, cy, rx, ry = shape
    pts = clipped(cx, cy, rx, ry, (lambda u: cut(state, u)) if shape is not SC else eye_top)
    outline = polygon([(side * a, b) for a, b in pts])
    def col(a, b):                            # a toward +X; the eye's u outward
        return look(cx + side * a * rx, cy + b * ry)
    k.decal(place(side, cx, cy), de * np.array([side, 1, 1.0]), (rx * HE, ry * HE), col, eps=eps, outline=outline,
            rings=rings, segs=segs)
def eyes(state):
    for side in (1, -1):
        if state in ("Open", "Fierce"):
            piece(state, side, SC, lambda u, v: np.tile(WHITE, (len(u), 1)), 0.007, 18, 72)
            piece(state, side, IR, lambda u, v: np.where((np.hypot(u - IR[0], v - IR[1]) > 0.78 * IR[2])[:, None],
                                                         RING, IRIS), 0.009, 12, 56)
            piece(state, side, PU, lambda u, v: np.tile(PUPIL, (len(u), 1)), 0.011, 6, 32)
            if state == "Fierce":
                k.decal(place(side, *LID_MID), de * np.array([side, 1, 1.0]), (HE, HE), srgb(BLACK), eps=0.013,
                        outline=polygon([(side * a, b) for a, b in LID_PTS]), rings=6, segs=72)
        else:
            open_look = lambda u, v: np.tile(WHITE, (len(u), 1))
            piece(state, side, SC, eye_look(state, open_look, CB, CI), 0.007, 18, 72)


k.build(voxel=0.0075)
k.eye_states(eyes)
k.export(out)
