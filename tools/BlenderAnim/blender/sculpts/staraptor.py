# Staraptor sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/staraptor.py -- <the export's Staraptor.glb> <out.glb>
# Staravia grown: the same grey-brown body, dark head and neck, white face and chest and orange beak with a dark point,
# but a hawk: a great crest that rises from the crown and sweeps forward over the face to a point, its front lobe red
# behind a zigzag; a pale grey brow that comes down over the eyes and runs back to a point behind the head; small red
# eyes cut by that brow; a dark collar crossing the white chest in points; big folded wings, a fanned tail and strong
# legs with shaggy dark feathers over yellow-orange shanks and long dark talons.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Vector
from mathutils.geometry import tessellate_polygon
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

BODY, DARK, WHITE, GREY, WING = "#7C7377", "#3E3A3E", "#F2F2F0", "#928B8F", "#6E666A"
ORANGE, TIP, RED, CLAW = "#F4A92A", "#363236", "#E5483D", "#2C282B"
H = np.array([0, -0.13, 0.73])          # the head's centre
HR = np.array([0.14, 0.15, 0.145])


# --- helpers ------------------------------------------------------------------------------------------------------
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


def curve(pts, per=6):
    """An open Catmull-Rom curve through the points, per points a span, the last point kept."""
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
    return [tuple(q) for q in out_]


def area(p):
    return 0.5 * np.sum(p[:, 0] * np.roll(p[:, 1], -1) - np.roll(p[:, 0], -1) * p[:, 1])


def triangles(p):
    """Triangles of a simple polygon (Blender's own tessellation), each turned counter-clockwise: an ear-clipper's
    slivers came out turned over and showed as dark streaks on the crest's flat faces."""
    tris = tessellate_polygon([[Vector((float(a), float(b), 0.0)) for a, b in p]])
    out_ = []
    for a, b, c in tris:
        cr = (p[b][0] - p[a][0]) * (p[c][1] - p[a][1]) - (p[b][1] - p[a][1]) * (p[c][0] - p[a][0])
        out_.append((a, b, c) if cr > 0 else (a, c, b))
    return out_


def capped(pts):
    """A patch's outline from a polygon, never further out than its furthest point: where a ray from the middle slips
    between two of the polygon's points, polygon() answers 9 and the patch throws a spike off the eye."""
    r = polygon(pts)
    far = max(math.hypot(a, b) for a, b in pts)
    return lambda th: min(r(th), far)


def flat(outline, x, thick):
    """A plate in the side's plane (outline points (y, z)) about x, thick: its faces and rim share no points."""
    p = np.array(outline, float)
    if area(p) < 0:
        p = p[::-1]
    m = len(p)
    front = np.column_stack([np.full(m, x + thick / 2), p])
    back = np.column_stack([np.full(m, x - thick / 2), p])
    V = [front, back]
    F = []
    for a, b, c in triangles(p):
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


# --- the body --------------------------------------------------------------------------------------------------------
# A hawk's build: tall and upright, the breast pushed forward, narrowing to a high bottom that shows the legs
def egg(V):
    # broad and high at the chest, tapering hard below to the thighs: a hawk's, not a hen's
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.58 * t ** 1.1
    V[:, 1] *= 1 - 0.2 * t ** 1.5
    return V
k.add("body", ell((0, 0.06, 0.47), (0.21, 0.19, 0.25), rot=(-14, 0, 0), deform=egg), BODY, "body")
k.add("breast", ell((0, -0.07, 0.52), (0.195, 0.15, 0.23), deform=egg), BODY, "body")
# the feathered thighs the lower body tapers into, over the legs' tops
k.add("thigh", ell((0.075, -0.015, 0.26), (0.058, 0.065, 0.075)), BODY, "leg{s}", mirror=True)
k.add("neck", ell((0, -0.08, 0.6), (0.15, 0.15, 0.15)), DARK, "head")
k.add("head", ell(H, HR), DARK, "head")
# The pale brow runs back from over the eyes to a point behind the head, rising
k.add("plume", blade([(0, -0.06, 0.79), (0, 0.06, 0.84), (0, 0.17, 0.9), (0, 0.25, 0.96)],
                     [0.1, 0.07, 0.035, 0.004], (1, 0, 0), 0.45), GREY, "head")
# The crest: a thick swept tuft of feathers rising from the crown and curving forward to a point over the brow, flatter
# side to side than deep, ending well above the beak; its front part red behind a zigzag (painted below)
CREST = [(0, -0.045, 0.83), (0, -0.04, 0.95), (0, -0.03, 1.055)]
CRESTARM = [(0, -0.03, 1.04), (0, -0.12, 0.995), (0, -0.23, 0.94), (0, -0.35, 0.86)]
k.add("crest", blade(CREST, [0.105, 0.075, 0.02], (1, 0, 0), 0.55), DARK, "head")
k.add("crestarm", blade(CRESTARM, [0.03, 0.08, 0.06, 0.008], (1, 0, 0), 0.55), DARK, "head")
def crest_red(P):
    line = 1.02 + 0.45 * (P[:, 1] + 0.15)                      # above and behind this the crest stays dark
    # its back edge, a zigzag, drawn forward near the top so the red stays on the crest's front
    zig = -0.095 + 0.02 * tri((P[:, 2] - 0.9) / 0.045) - 1.3 * np.clip(P[:, 2] - 0.93, 0, 1)
    return inside(P[:, 2] - line, 0.003) * inside(P[:, 1] - zig, 0.003) * (P[:, 2] > 0.84) * (P[:, 2] < 1.01)
k.add("crestfill", blade([(0, -0.1, 0.85), (0, -0.22, 0.87)], [0.055, 0.03], (1, 0, 0), 0.6), DARK, "head")
k.paint(crest_red, RED, on=["crest"])
# Big folded wings down the sides, their tips sweeping back over the tail's root
def folded(V):
    V = V.copy(); V[:, 2] *= 1 - 0.45 * np.clip(V[:, 1], 0, 1); return V
k.add("wing", ell((0.17, 0.12, 0.46), (0.062, 0.27, 0.195), rot=(-38, 12, 4), deform=folded), WING, "wing{s}", mirror=True)
k.add("wingtip", blade([(0.17, 0.16, 0.36), (0.165, 0.28, 0.27), (0.13, 0.42, 0.19)], [0.075, 0.05, 0.006], (1, 0, 0), 0.5),
      WING, "wing{s}", mirror=True)

# The upper back and nape: the head's dark comes down the back of the neck to the shoulders
def nape(P):
    th = np.arctan2(P[:, 0], P[:, 1] + 0.04)          # 0 straight behind
    return inside(0.58 + 0.025 * tri(th * 5 / (2 * math.pi)) - P[:, 2], 0.004) * (np.abs(th) < 2.0)
k.paint(nape, DARK, on=["body", "breast"])

# The face and throat and chest: white down the front, round the head's axis and the body's
R = 0.155
def round_head(P):
    P = np.atleast_2d(P)
    return np.arctan2(P[:, 0], -(P[:, 1] - H[1])) * R, P[:, 2]

# The eyes' make, in an eye's own units (u outward, v up, one unit HE): a small round eye cut along its top by the grey
# brow, which slants down toward the beak; the Fierce lid comes lower still toward the beak
HE = 0.036
SL = 0.32
def eye_top(u):
    return 0.22 + SL * u
def cut(state, u):
    return np.minimum(eye_top(u), -0.02 + 0.55 * u) if state == "Fierce" else eye_top(u)
de = unit(0.5, -0.85, 0.16)
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

# The brow: from a point at the beak's root between the eyes, along each eye's cut and on back over the temples
brow = []
for u in np.linspace(-1.6, 2.6, 40):
    s, z = round_head(on_head(u, eye_top(u) - 0.04))
    if s[0] > 0.004:
        brow.append((float(s[0]), float(z[0])))
(s0, z0), (s1, z1) = brow[0], brow[1]
point = z0 - (z1 - z0) / (s1 - s0) * s0
BROW = ([(0.0, point)] + brow + [(0.42, brow[-1][1] + 0.05), (0.42, 1.1), (-0.42, 1.1), (-0.42, brow[-1][1] + 0.05)]
        + [(-a, z) for a, z in brow[::-1]])
HEADFRONT = lambda P: (P[:, 2] > 0.6)
k.paint(lambda P: inside(np.abs(np.arctan2(P[:, 0], -(P[:, 1] - H[1]))) - 1.75, 0.004) * HEADFRONT(P)
        * (P[:, 2] < 0.85), WHITE, on=["head", "neck"])
k.paint(lambda P: poly_mask(*round_head(P), BROW, edge=0.003) * (P[:, 2] > 0.66), GREY, on=["head", "neck", "plume"])

# The white chest and throat, the dark collar crossing it in points
CR = 0.22
def round_body(P):
    return np.arctan2(P[:, 0], -(P[:, 1] - 0.03)) * CR, P[:, 2]
CHEST = mirrored([(0.0, 0.66), (0.12, 0.66), (0.2, 0.6), (0.26, 0.48), (0.27, 0.34), (0.22, 0.29), (0.12, 0.25),
                  (0.0, 0.23)])
k.paint(lambda P: poly_mask(*round_body(P), CHEST) * (P[:, 1] < 0.0) * (P[:, 2] < 0.66), WHITE, on=["body", "breast", "neck"])
COLLAR = mirrored([(0.0, 0.535), (0.1, 0.55), (0.2, 0.58), (0.3, 0.62), (0.42, 0.62), (0.42, 0.42),
                   (0.3, 0.47), (0.25, 0.37), (0.19, 0.46), (0.12, 0.42), (0.07, 0.48), (0.0, 0.39)])
k.paint(lambda P: poly_mask(*round_body(P), COLLAR) * (P[:, 1] < 0.02) * (P[:, 2] < 0.64), DARK, on=["body", "breast", "neck"])
# a pale scribble on each wing, as on the art
k.paint(lambda P: inside(np.abs(P[:, 2] - (0.47 - 0.25 * (P[:, 1] - 0.06) + 0.012 * tri(P[:, 1] / 0.03))) - 0.008, 0.003)
        * (P[:, 1] > 0.0) * (P[:, 1] < 0.16), "#958C90", on=["wing"])

# --- loose pieces -------------------------------------------------------------------------------------------------------

# The beak: a short hooked wedge of yellow-orange and a dark point drooping off its tip
def pointed(V):
    t = np.clip(-V[:, 1], 0, 1)
    V = V.copy(); V[:, 0] *= 1 - 0.7 * t; V[:, 2] *= 1 - 0.6 * t
    return V
k.add("beak", ell((0, -0.27, 0.69), (0.052, 0.07, 0.042), rot=(8, 0, 0), deform=pointed), ORANGE, "head", fuse=False,
      surface="shell")
k.add("beaktip", tube([(0, -0.31, 0.684), (0, -0.345, 0.672), (0, -0.37, 0.652)], [0.02, 0.012, 0.004]), TIP, "head",
      fuse=False, surface="shell")

# The tail: a broad fan of pointed feathers behind, grey-brown in the middle and dark at the sides
for i, (ang, side, length, col) in enumerate([(40, 0.0, 0.34, BODY), (36, 0.045, 0.32, DARK), (36, -0.045, 0.32, DARK),
                                              (30, 0.09, 0.3, BODY), (30, -0.09, 0.3, BODY), (22, 0.12, 0.27, DARK),
                                              (22, -0.12, 0.27, DARK), (12, 0.06, 0.24, DARK), (12, -0.06, 0.24, DARK)]):
    r = math.radians(ang)
    d = np.array([side * 2.6, math.cos(r), math.sin(r)]); d /= np.linalg.norm(d)
    base = np.array([side * 0.4, 0.22, 0.36])
    tip = base + d * length
    k.add(f"tail{i}", blade([base, base + d * length * 0.5, tip], [0.06, 0.055, 0.006], (1, 0, 0), 0.28), col, "tail",
          fuse=False)

# Strong legs: shaggy dark feathers over the top, yellow-orange shanks, three long toes forward and one back, dark talons
for s, side in ((1, "R"), (-1, "L")):
    x = 0.085 * s
    k.add(f"leg{side}", tube([(x, -0.02, 0.22), (x * 1.04, -0.03, 0.11), (x * 1.07, -0.04, 0.035)], [0.028, 0.024, 0.022]),
          ORANGE, f"leg{side}", fuse=False, surface="scales")
    for j, ang in enumerate((-80, -45, -10, 25, 60, 100, 150, 200, 250)):
        rr = math.radians(ang)
        d = np.array([math.sin(rr), -math.cos(rr), 0])
        b0 = np.array([x * 1.02, -0.025, 0.2]) + d * 0.016
        t0 = b0 + d * 0.035 + np.array([0, 0, -0.06 - 0.01 * (j % 2)])
        k.add(f"fluff{side}{j}", blade([b0, t0], [0.02, 0.003], np.cross(d, (0, 0, 1.0)), 0.4), DARK, f"leg{side}",
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
# along the top where the grey brow comes down over it (the grey painted just under the cut, so the patch's edge is the
# line). The Fierce lid is a grey patch over the iris's top. The patches are laid by hand, u toward +X on both sides.
WHITEC, IRIS, PUPIL = srgb(WHITE), srgb("#E04A3C"), srgb("#1A1418")
CG, CI = srgb(GREY), srgb("#2A262A")


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
IR = (-0.04, 0.0, 0.76, 0.76)                 # the iris, its top third under the brow
PU = (-0.08, -0.1, 0.3, 0.32)                 # the pupil
GL = (0.18, -0.02, 0.14, 0.14)                # the glint


def fierce_lid():
    """The Fierce lid over the iris: between the Fierce cut and a little past the open cut, inside the iris; convex,
    so its outline is its points sorted round their middle."""
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
