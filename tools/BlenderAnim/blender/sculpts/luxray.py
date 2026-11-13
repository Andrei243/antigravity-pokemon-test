# Luxray sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/luxray.py -- <the export's Luxray.glb> <out.glb>
# Luxio grown: a lean black lion on long legs. A great spiky black mane over the head and round the neck and shoulders,
# out of which come the narrow blue face and the round blue ears with their yellow; fierce eyes, a red sclera round a
# yellow iris, cut along the top by the mane's black as Luxio's are; black forelegs whose backs are blue with three
# yellow bands; a blue belly and blue hind legs under black haunches; spikes along the back; and a long thin tail that
# rises in an arc and hangs a yellow four-pointed star behind.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, eye_look, polygon, lumpy, args

src, out = args()
k = Kit(src, coat="fur")

BLUE, BLACK, YELLOW, NOSE, INK = "#5EAEE2", "#2C2C36", "#F7C832", "#E0404C", "#22222C"
H = np.array([0, -0.31, 0.82])          # the head's centre
HR = np.array([0.15, 0.17, 0.15])


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


# --- the body -----------------------------------------------------------------------------------------------------------
# A lean black body, a deep chest under the mane, the head held up on a short neck; a longer muzzle than Luxio's
k.add("body", ell((0, 0.06, 0.53), (0.165, 0.33, 0.19)), BLACK, "body")
k.add("chest", ell((0, -0.19, 0.56), (0.18, 0.17, 0.22)), BLACK, "body")
k.add("neck", ell((0, -0.26, 0.71), (0.13, 0.13, 0.14)), BLACK, "head")
k.add("head", ell(H, HR), BLACK, "head")
k.add("muzzle", ell((0, -0.45, 0.755), (0.072, 0.075, 0.056)), BLUE, "head")

# The eyes' make, in an eye's own units (u outward, v up, one unit HE): an almond eye whose top is cut off straight,
# slanting down toward the nose, by the black fur of the forehead; the Fierce lid comes lower toward the nose
HE = 0.042
SL = 0.5
def eye_top(u):
    return 0.22 + SL * u
def cut(state, u):
    return np.minimum(eye_top(u), 0.04 + 0.65 * u) if state == "Fierce" else eye_top(u)
de = unit(0.5, -0.86, 0.1)
E = H + de / np.linalg.norm(de / HR) + np.array([0, 0, 0.0])
def frame(n):
    n = np.array(n, float); n /= np.linalg.norm(n)
    right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
    return right, np.cross(n, right)
ERI, EUP = frame(de)
R = 0.16
def round_head(P):
    P = np.atleast_2d(P)
    return np.arctan2(P[:, 0], -(P[:, 1] - H[1])) * R, P[:, 2]
def on_head(u, v):
    o = E + de * 0.35 + ERI * u * HE + EUP * v * HE
    oc, dd = (o - H) / HR, -de / HR
    a, b, c = dd @ dd, 2 * oc @ dd, oc @ oc - 1
    return o + dd * HR * (-b - math.sqrt(max(b * b - 4 * a * c, 0))) / (2 * a)

# The black of the mane comes down over the forehead in one shape: along the top of each eye (just under its cut), to
# a point between them at the nose's ridge, and out over the temples; the face is blue below, round the cheeks and
# down to the chin, where the ruff begins
brow = []
for u in np.linspace(-1.8, 1.6, 36):
    s, z = round_head(on_head(u, eye_top(u) - 0.2))
    if s[0] > 0.004:
        brow.append((float(s[0]), float(z[0])))
(s0, z0), (s1, z1) = brow[0], brow[1]
point = z0 - (z1 - z0) / (s1 - s0) * s0
FACE = mirrored([(0.0, point)] + brow + [(brow[-1][0] + 0.01, brow[-1][1] - 0.02), (0.2, 0.79), (0.19, 0.72),
                                        (0.15, 0.67), (0.08, 0.65), (0.0, 0.645)])
FRONT = lambda P: (P[:, 1] < H[1] + 0.05) * (np.abs(np.arctan2(P[:, 0], -(P[:, 1] - H[1]))) < 1.4)
k.paint(lambda P: FRONT(P) * (P[:, 2] > 0.8), BLACK, on=["head", "muzzle"])
k.paint(lambda P: poly_mask(*round_head(P), FACE, edge=0.003) * FRONT(P) * (P[:, 2] > 0.64), BLUE)

# The mane: a few large swept masses, never a fringe. A smooth cap over the crown behind the face fills the roots; three
# big spikes sweep up and back from the crown (the front one the tallest, about 1.08); big pointed cheek manes sweep out
# and down beside the face; a ruff hangs over the chest; a tuft over each shoulder points back
k.add("hood", ell((0, -0.25, 0.89), (0.14, 0.15, 0.1)), BLACK, "head")
CROWN = [  # (points, radii, flattened along, how round)
    ([(0.0, -0.33, 0.9), (0.0, -0.33, 0.99), (0.0, -0.3, 1.08)], [0.12, 0.075, 0.006], (0, 1, 0.2), 0.7),
    ([(0.0, -0.26, 0.93), (0.0, -0.15, 1.01), (0.0, 0.0, 1.06)], [0.11, 0.07, 0.005], (1, 0, 0), 0.8),
    ([(0.06, -0.3, 0.92), (0.15, -0.25, 1.0), (0.27, -0.16, 1.06)], [0.095, 0.06, 0.005], (0.1, -0.6, 0.8), 0.45),
    ([(0.07, -0.22, 0.93), (0.16, -0.12, 0.99), (0.27, 0.0, 1.0)], [0.09, 0.055, 0.005], (0.2, -0.3, 0.9), 0.45),
]
for i, (pts, rr, ax, f) in enumerate(CROWN):
    k.add(f"mane{i}", blade(pts, rr, ax, f), BLACK, "head", mirror=pts[0][0] > 0)
# the cheek manes, flat front to back, sweeping out and down past the jaw; a second behind each
for i, (pts, rr) in enumerate([([(0.1, -0.29, 0.8), (0.22, -0.3, 0.71), (0.35, -0.27, 0.58)], [0.095, 0.06, 0.005]),
                               ([(0.1, -0.21, 0.84), (0.23, -0.17, 0.78), (0.36, -0.1, 0.72)], [0.09, 0.055, 0.005])]):
    k.add(f"cheek{i}", blade(pts, rr, (0, 1, 0.3), 0.5), BLACK, "head", mirror=True)
# the ruff over the chest, pointing down, and its two sides
k.add("ruff", blade([(0, -0.3, 0.68), (0, -0.35, 0.55), (0, -0.33, 0.42)], [0.1, 0.065, 0.005], (0, 1, 0), 0.5),
      BLACK, "body")
k.add("ruffside", blade([(0.08, -0.29, 0.66), (0.13, -0.32, 0.54), (0.16, -0.3, 0.43)], [0.075, 0.05, 0.005], (0, 1, 0),
                        0.5), BLACK, "body", mirror=True)
# the shoulders' tufts, swept back
k.add("shoulder", blade([(0.12, -0.15, 0.66), (0.19, -0.05, 0.7), (0.24, 0.06, 0.7)], [0.065, 0.04, 0.005], (0, 0, 1),
                        0.5), BLACK, "body", mirror=True)

# A few big tufts at the rump, rising up and back
k.add("spike0", blade([(0.0, 0.16, 0.66), (0.0, 0.23, 0.78), (0.0, 0.32, 0.87)], [0.07, 0.045, 0.005], (1, 0, 0), 0.45),
      BLACK, "body")
k.add("spike1", blade([(0.07, 0.2, 0.65), (0.13, 0.28, 0.75), (0.19, 0.36, 0.8)], [0.06, 0.04, 0.005], (0.5, -0.4, 0.8),
                      0.45), BLACK, "body", mirror=True)
k.add("spike2", blade([(0.0, 0.05, 0.68), (0.0, 0.1, 0.77), (0.0, 0.17, 0.83)], [0.055, 0.035, 0.005], (1, 0, 0), 0.45),
      BLACK, "body")

# Round ears at the head's upper corners, coming out of the mane and tilted out
EC = np.array([0.2, -0.25, 0.97])
ER = (0, 30, 8)
k.add("ear", ell(EC, (0.075, 0.03, 0.088), rot=ER), BLUE, "ear{s}", mirror=True)

# Legs: the forelegs black down to the paws, their backs blue with three yellow bands; black haunches, blue below
k.add("leg", tube([(0.12, -0.2, 0.52), (0.125, -0.21, 0.28), (0.125, -0.22, 0.06)], [0.075, 0.062, 0.056]), BLACK,
      "leg{s}", mirror=True)
k.add("paw", ell((0.125, -0.25, 0.045), (0.068, 0.085, 0.045)), BLACK, "leg{s}", mirror=True)
k.add("thigh", ell((0.11, 0.27, 0.49), (0.095, 0.14, 0.17)), BLACK, "hind{s}", mirror=True)
k.add("hind", tube([(0.12, 0.29, 0.44), (0.125, 0.31, 0.25), (0.125, 0.27, 0.06)], [0.07, 0.056, 0.052]), BLACK,
      "hind{s}", mirror=True)
k.add("hpaw", ell((0.125, 0.25, 0.045), (0.065, 0.083, 0.045)), BLUE, "hind{s}", mirror=True)

# the forelegs' backs: behind the leg's own line, from the elbow to the ankle, blue and then three yellow bands
def legline(z):
    return -0.2 - 0.02 * (0.52 - z) / 0.46
def back_of_leg(P):
    return inside(legline(P[:, 2]) - P[:, 1], 0.0025) * inside(P[:, 2] - 0.43, 0.0025) * inside(0.12 - P[:, 2], 0.0025)
k.paint(back_of_leg, BLUE, on=["leg"])
k.paint(lambda P: back_of_leg(P) * inside(np.min(np.abs(P[:, 2][:, None] - np.array([0.37, 0.28, 0.19])[None]), axis=1)
                                          - 0.022, 0.0025), YELLOW, on=["leg"])
# the hind legs blue from below the haunch
k.paint(lambda P: inside(P[:, 2] - (0.4 - 0.25 * (P[:, 1] - 0.22)), 0.0025) * (P[:, 1] > 0.1), BLUE)
# a blue belly from behind the forelegs to the haunches
k.paint(lambda P: inside(P[:, 2] - (0.45 + 0.03 * tri((P[:, 1] + 0.1) / 0.09)), 0.0025) * (P[:, 1] > -0.13)
        * (P[:, 1] < 0.24) * (P[:, 2] > 0.3), BLUE, on=["body", "chest"])

# The long thin tail: up from the rump in an arc and down, the star hanging behind
k.add("tail", tube([(0, 0.38, 0.6), (0, 0.48, 0.72), (0, 0.56, 0.86), (0, 0.66, 0.93), (0, 0.76, 0.9), (0, 0.82, 0.8),
                    (0, 0.84, 0.69)], [0.03, 0.026, 0.023, 0.021, 0.02, 0.019, 0.018]), BLACK, "tail")
def star(V):
    V = V.copy()
    th = np.arctan2(V[:, 2], V[:, 1])
    p = 0.7
    s = (np.abs(np.cos(th)) ** p + np.abs(np.sin(th)) ** p) ** (-1 / p)
    V[:, 1] *= s; V[:, 2] *= s
    return V
k.add("star", ell((0, 0.85, 0.57), (0.035, 0.14, 0.14), rot=(20, 0, 0), deform=star), YELLOW, "tail", fuse=False,
      surface="shell")

# The ears' yellow, crisp
m = np.array(Euler([math.radians(a) for a in ER]).to_matrix())
en = m @ np.array([0, -1.0, 0]); eu = m @ np.array([0, 0, 1.0])
k.decal(EC + en * 0.026 - eu * 0.008, en, (0.05, 0.064), YELLOW, up=eu, eps=0.003, rings=10, segs=48, mirror=True)

# A red nose like a triangle at the muzzle's end, and a short mouth line
def nose(V):
    V = V.copy()
    t = np.clip((V[:, 2] + 1) / 2, 0, 1)
    V[:, 0] *= 0.25 + 0.85 * t
    return V
k.add("nose", ell((0, -0.512, 0.795), (0.024, 0.013, 0.016), deform=nose), NOSE, "head", fuse=False, surface="skin")
CB, CI = srgb(BLUE), srgb(INK)
def mouth(u, v):
    c = np.tile(CB, (len(u), 1))
    c[(np.abs(v - (0.1 - 0.45 * u * u)) < 0.14) & (np.abs(u) < 0.85)] = CI
    c[(np.abs(u) < 0.08) & (v > 0.05)] = CI
    return c
k.decal((0, -0.508, 0.76), unit(0, -1, -0.3), (0.034, 0.022), mouth, eps=0.003, rings=16, segs=72)

# --- the eyes ---------------------------------------------------------------------------------------------------------------
# No rim: a red sclera, a big yellow iris with an orange ring, a small black pupil of its own, each piece's outline cut
# straight along the top where the mane's black comes over it (the black is painted just under the cut). The Fierce lid
# is a black patch of its own. The patches are laid by hand, u toward +X on both sides.
SCLERA, IRIS, RING, PUPIL = srgb("#D8303A"), srgb(YELLOW), srgb("#D8861A"), srgb("#16161C")


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


SC = (0.0, 0.0, 1.0, 0.8)                     # the sclera: an almond, wider than tall
IR = (-0.1, -0.05, 0.62, 0.62)                # the iris
PU = (-0.13, -0.05, 0.2, 0.3)                 # the pupil


def fierce_lid():
    inside_lid = lambda u, v: (v > cut("Fierce", u) - 1e-9) & (v < eye_top(u) + 0.06 + 1e-9)
    pts = []
    for th in np.linspace(0, 2 * math.pi, 240, endpoint=False):
        u, v = 1.04 * math.cos(th), 0.84 * math.sin(th)
        if inside_lid(u, v):
            pts.append((u, v))
    for u in np.linspace(-1.1, 1.1, 89):
        for v in (float(cut("Fierce", u)), float(eye_top(u)) + 0.06):
            if (u / 1.04) ** 2 + (v / 0.84) ** 2 <= 1 and inside_lid(u, v):
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
    def col(a, b):
        return look(cx + side * a * rx, cy + b * ry)
    k.decal(place(side, cx, cy), de * np.array([side, 1, 1.0]), (rx * HE, ry * HE), col, eps=eps, outline=outline,
            rings=rings, segs=segs)


def eyes(state):
    for side in (1, -1):
        if state in ("Open", "Fierce"):
            piece(state, side, SC, lambda u, v: np.tile(SCLERA, (len(u), 1)), 0.007, 18, 72)
            piece(state, side, IR, lambda u, v: np.where((np.hypot(u - IR[0], v - IR[1]) > 0.8 * IR[2])[:, None],
                                                         RING, IRIS), 0.009, 12, 56)
            piece(state, side, PU, lambda u, v: np.tile(PUPIL, (len(u), 1)), 0.011, 6, 32)
            if state == "Fierce":
                k.decal(place(side, *LID_MID), de * np.array([side, 1, 1.0]), (HE, HE), srgb(BLACK), eps=0.013,
                        outline=polygon([(side * a, b) for a, b in LID_PTS]), rings=6, segs=72)
        else:
            open_look = lambda u, v: np.tile(CB, (len(u), 1))
            piece(state, side, SC, eye_look(state, open_look, CB, CI), 0.007, 18, 72)


k.build(voxel=0.009)
k.eye_states(eyes)
k.export(out)
