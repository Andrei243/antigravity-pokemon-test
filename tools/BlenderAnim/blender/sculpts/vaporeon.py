# Vaporeon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/vaporeon.py -- <the export's Vaporeon.glb> <out.glb>
# Eevee grown into the water: the same frame of four legs and a trunk, the same round head and way of making the eyes,
# nose and mouth, but sleek light blue with a scaled coat; a white frilled collar round the neck, pleated and scalloped;
# a cream fin at each side of the head with a dark blue leading edge and rays; a dark blue fin on the crown and a dark
# blue V coming down the forehead from it; a dark blue ridge of points down the spine and the tail; a long thick tail
# curled round to the model's +X side, ending in a fish's fluke; dark eyes with a white glint and a small open mouth.
# Grown from eevee.py (copied, not imported), as the other evolutions are.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, tri, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="scales")

# --- colours ----------------------------------------------------------------------------------------------------------
BLUE = "#5EC3E6"           # the coat: between the drawing's pale blue and the 3D model's cyan
NAVY = "#2A6AAE"           # the crown fin, the forehead's V, the ridge, the fins' edges and rays
CREAM = "#F3EAA2"          # the side fins' webbing
FRILL = "#F1F4F7"          # the collar
EYE, PUPIL = "#2A2440", "#0F0B1A"   # dark eyes, a darker middle
NOSE, INK = "#1E2430", "#1C2A3A"
MOUTH, TONGUE = "#8C2234", "#F07A90"

# --- the frame (the skeleton's joints: body (0, 0.03, 0.19), head joint (0, -0.13, 0.34), ears (+-0.06, -0.13, 0.43),
#     crest (0, 0, 0.27), forelegs (+-0.05, -0.08, 0.17), hind legs (+-0.05, 0.13, 0.17), tail (0, 0.17, 0.2)) ----------
H = np.array([0, -0.155, 0.425])         # the head's centre, over and in front of its joint (as Eevee's)
HR = np.array([0.104, 0.093, 0.097])     # as large against the body as Eevee's: the family's big head
HS = HR[0] / 0.09                        # the face's offsets below were laid on a head of 0.09: scaled to this one
LEG_X = 0.05
FORE_Y, HIND_Y = -0.08, 0.13             # the legs stand further apart: a longer body
BODY_Z = 0.215
LEG_TOP = 0.205


def head_shape(V):
    V = V.copy()
    V[:, 0] *= 1 + 0.06 * np.clip(-V[:, 2], 0, 1) * (1 - np.clip(-V[:, 2], 0, 1))
    return V


def add_head():
    k.add("head", ell(H, HR, deform=head_shape), BLUE, "head")
    k.add("muzzle", ell(H + HS * np.array((0, -0.066, -0.032)), HS * np.array((0.04, 0.03, 0.027))), BLUE, "head")
    k.add("neck", tube([(0, -0.05, 0.25), (0, -0.09, 0.31), (0, -0.13, 0.37)], [0.062, 0.058, 0.06]), BLUE, "head")


def add_body():
    # Sleeker and longer than Eevee's: a chest, a long middle and a rump in one smooth line
    # The chest rises high under the collar, so the head sits on the shoulders with no neck showing
    k.add("chest", ell((0, -0.07, BODY_Z + 0.02), (0.08, 0.085, 0.105)), BLUE, "body")
    k.add("middle", ell((0, 0.035, BODY_Z - 0.005), (0.072, 0.12, 0.088)), BLUE, "body")
    k.add("rump", ell((0, 0.125, BODY_Z - 0.008), (0.078, 0.076, 0.09)), BLUE, "body")


def add_legs(color=BLUE):
    # Eevee's legs and paws, a little longer: broad shoulders and thighs over their upper halves, sturdy below
    k.add("shoulder", ell((LEG_X - 0.004, FORE_Y + 0.005, 0.17), (0.036, 0.05, 0.065)), color, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X - 0.004, FORE_Y + 0.005, LEG_TOP), (LEG_X, FORE_Y, 0.11), (LEG_X + 0.002, FORE_Y - 0.004, 0.03)],
                      [0.042, 0.035, 0.032]), color, "leg{s}", mirror=True)
    k.add("paw", ell((LEG_X + 0.003, FORE_Y - 0.017, 0.021), (0.036, 0.045, 0.023)), color, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X - 0.002, HIND_Y + 0.008, 0.165), (0.05, 0.064, 0.075)), color, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X, HIND_Y + 0.012, 0.16), (LEG_X + 0.002, HIND_Y + 0.01, 0.09),
                        (LEG_X + 0.002, HIND_Y - 0.002, 0.03)], [0.04, 0.034, 0.032]), color, "hind{s}", mirror=True)
    k.add("hpaw", ell((LEG_X + 0.003, HIND_Y - 0.015, 0.021), (0.036, 0.045, 0.023)), color, "hind{s}", mirror=True)


def along(P, pts, n=200, fold=True):
    """Nearest place on a polyline (|x| taken when fold, so a mirrored part's both sides): its share and the offset."""
    pts = np.array(pts, float)
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    cum = np.concatenate([[0], np.cumsum(seg)]); L = cum[-1]
    s = np.linspace(0, L, n)
    line = np.stack([np.interp(s, cum, pts[:, i]) for i in range(3)], axis=1)
    Q = P.copy()
    if fold:
        Q[:, 0] = np.abs(Q[:, 0])
    j = np.concatenate([((Q[a:a + 4000, None, :] - line[None]) ** 2).sum(2).argmin(1) for a in range(0, len(Q), 4000)])
    return s[j] / L, Q - line[j]


# --- the side fins (the ears' bones): a fan of cream webbing from the side of the head, up and out, its face to the
#     front; a dark blue leading edge along its top and two dark blue rays, the edge between them scalloped ----------
FIN_ROOT = np.array([0.086, -0.14, 0.445])
FIN_OUT = unit(1, 0.18, 0)                  # out and a little back
FIN_UP = np.array([0, 0, 1.0])
FIN_LEAD, FIN_TRAIL = math.radians(34), math.radians(-14)   # the fan's angles from the horizontal
FIN_LONG, FIN_SHORT = 0.185, 0.1


def fin_dir(phi):
    return np.cos(phi)[..., None] * FIN_OUT + np.sin(phi)[..., None] * FIN_UP


def fin_len(f):
    """The webbing's reach at a share f of the fan (0 the leading edge, 1 the trailing), scalloped between the rays."""
    base = FIN_LONG + (FIN_SHORT - FIN_LONG) * f ** 0.8
    scal = 1 - 0.13 * np.sin(np.pi * ((f * 3) % 1)) ** 2
    return base * scal


def add_fins():
    def at(u, v):
        f = 0.01 + 0.98 * v
        phi = FIN_LEAD + (FIN_TRAIL - FIN_LEAD) * f
        r = (0.012 + 0.988 * u) * fin_len(f)
        return FIN_ROOT + fin_dir(phi) * r[..., None]
    k.add("fin", sheet(at, 10, 40, 0.006), CREAM, "ear{s}", fuse=False, mirror=True)
    lead = [FIN_ROOT + fin_dir(np.array(FIN_LEAD + 0.02)) * FIN_LONG * t for t in (0.0, 0.5, 0.97)]
    k.add("finedge", tube(lead, [0.011, 0.008, 0.003], seg=12), NAVY, "ear{s}", fuse=False, mirror=True)
    for i, f in enumerate((1 / 3, 2 / 3)):
        phi = FIN_LEAD + (FIN_TRAIL - FIN_LEAD) * f
        ray = [FIN_ROOT + fin_dir(np.array(phi)) * fin_len(f) * t for t in (0.05, 0.5, 0.98)]
        k.add(f"finray{i}", blade(ray, [0.0075, 0.006, 0.003], unit(0, -1, 0.05), 0.75), NAVY, "ear{s}", fuse=False,
              mirror=True)


# --- the collar: a frill of white round the neck, opening toward the head, pleated in twelve with a round scallop to
#     each pleat; its seam at the back ------------------------------------------------------------------------------
COLLAR = np.array([0, -0.115, 0.325])
CA = unit(0, -0.52, 0.85)                   # its axis: toward the head
CE1 = np.array([1.0, 0, 0])
CE2 = np.cross(CA, CE1)                     # back and up
PLEATS = 12


def add_collar():
    def at(u, v):
        th = math.pi / 2 + 2 * math.pi * (v * 1.02 - 0.01)
        frac = (v * PLEATS) % 1
        bump = 1 - (2 * frac - 1) ** 2
        r = 0.05 + (0.078 + 0.015 * bump) * u
        lift = 0.06 * u ** 1.4 + 0.009 * u * bump
        return (COLLAR + (np.cos(th)[..., None] * CE1 + np.sin(th)[..., None] * CE2) * r[..., None]
                + CA * lift[..., None])
    k.add("collar", sheet(at, 8, PLEATS * 12 + 1, 0.007), FRILL, "head", fuse=False)


# --- the crown fin: a dark blue blade up from the forehead, leaning back, its face turned 40 degrees toward +X so it is
#     not a pole from the front; and the dark blue V of the forehead under it -----------------------------------------
def add_crown():
    pts = [H + HS * np.array((0, -0.03, 0.06)), H + HS * np.array((0, -0.015, 0.105)), H + HS * np.array((0, 0.01, 0.15))]
    k.add("crown", blade(pts, [0.03, 0.016, 0.003], unit(0.77, -0.64, 0), 0.4), NAVY, "head", fuse=False)


def in_poly(x, y, pts):
    pts = np.array(pts, float)
    res = np.zeros(len(x), bool)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        res ^= ((y1 > y) != (y2 > y)) & (x < (x2 - x1) * (y - y1) / (y2 - y1 + 1e-12) + x1)
    return res


def poly_mask(x, y, pts, edge=0.003):
    pts = np.array(pts, float)
    d = np.full(len(x), 9.0)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        ex, ey = x2 - x1, y2 - y1
        t = np.clip(((x - x1) * ex + (y - y1) * ey) / (ex * ex + ey * ey + 1e-12), 0, 1)
        d = np.minimum(d, np.hypot(x - x1 - t * ex, y - y1 - t * ey))
    return inside(np.where(in_poly(x, y, pts), -d, d), edge)


def mirrored(half):
    return half + [(-a, z) for a, z in half[::-1][1:-1]]


R_HEAD = 0.09
z0 = H[2]
# (arc round the head's upright axis, z): a point between the eyes, the arms out over the brows to the fins' roots
V_MASK = mirrored([(0.0, z0 + 0.012), (0.018, z0 + 0.045), (0.04, z0 + 0.06), (0.075, z0 + 0.058),
                   (0.12, z0 + 0.045), (0.15, z0 + 0.06), (0.16, z0 + 0.2), (0.0, z0 + 0.2)])


def add_mask():
    def m(P):
        s = np.arctan2(P[:, 0], -(P[:, 1] - H[1])) * R_HEAD
        front = np.abs(np.arctan2(P[:, 0], -(P[:, 1] - H[1]))) < 1.75
        return poly_mask(s, z0 + (P[:, 2] - z0) / HS, V_MASK) * front * (P[:, 2] > z0) * (P[:, 1] < H[1] + 0.06)
    k.paint(m, NAVY, on=["head", "muzzle"])


# --- the ridge: a dark blue stripe down the spine and along the top of the tail, with a row of points standing on it,
#     each turned 40 degrees toward +X ---------------------------------------------------------------------------------
# Down behind the rump to the ground and round to the +X side, its end pointing out sideways so the fluke shows its
# face from the front three-quarter and from behind
TAIL = [(0.0, 0.16, 0.22), (0.01, 0.25, 0.18), (0.04, 0.31, 0.12), (0.09, 0.33, 0.08), (0.15, 0.315, 0.075),
        (0.2, 0.295, 0.09)]
TAIL_R = [0.052, 0.046, 0.04, 0.034, 0.028, 0.022]
SPINE = [(0, -0.06, 0.29), (0, 0.03, 0.283), (0, 0.11, 0.282), (0, 0.16, 0.268)]


def add_tail():
    k.add("tail", tube(TAIL, TAIL_R), BLUE, "tail")
    end = np.array(TAIL[-1]); d = unit(*(end - np.array(TAIL[-2])))
    side = unit(*np.cross(d, (0, 0, 1.0)))
    # The fluke: two broad lobes in the upright plane of the tail's end, one up and one down; loose, rooted inside the
    # tail's end, so their edges stay crisp (fused, the thin plate fell below the voxel and frayed)
    up = np.array([0, 0, 1.0])
    for name, w, ln in (("flukeup", 0.9, 0.135), ("flukedown", -0.45, 0.105)):
        dd = unit(*(d + up * w))
        root = end - d * 0.02
        pts = [root + dd * ln * t + d * 0.03 * t * t for t in (0.0, 0.22, 0.45, 0.7, 1.0)]
        k.add(name, blade(pts, [0.02, 0.042, 0.05, 0.036, 0.004], side, 0.3), BLUE, "tail", fuse=False)


def spline(pts, per=12):
    """The tail's own centre line, a Catmull-Rom curve as tube() makes it: its chords cut the bends' insides."""
    p = np.array(pts, float)
    ext = np.vstack([2 * p[0] - p[1], p, 2 * p[-1] - p[-2]])
    out_ = []
    for i in range(len(p) - 1):
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        for t in np.linspace(0, 1, per, endpoint=False):
            out_.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                               + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    out_.append(p[-1])
    return np.array(out_)


def add_ridge():
    curve = spline(TAIL)

    def stripe(P):
        body = inside(np.abs(P[:, 0]) - 0.012, 0.0025) * (P[:, 2] > 0.262) * (P[:, 1] > -0.07) * (P[:, 1] < 0.16)
        t, off = along(P, curve, n=400, fold=False)
        horiz = np.hypot(off[:, 0], off[:, 1])
        # Along the tail's top only as far as it runs back: past the bend "top" meets the flank and blotched it
        tl = (inside(horiz - 0.011, 0.0025) * (off[:, 2] > 0.85 * np.linalg.norm(off, axis=1))
              * (off[:, 2] > 0.01) * (t < 0.3))
        return np.maximum(body, tl)
    k.paint(stripe, NAVY, on=["middle", "chest", "rump", "tail", "neck"])
    # The ridge's points on the crest bone (the C# sculpt's), few and large, each turned 40 degrees toward +X
    turn = unit(0.77, 0.64, 0)
    for i, (y, z) in enumerate(((-0.025, 0.322), (0.035, 0.293), (0.095, 0.29), (0.15, 0.275))):
        base = np.array((0, y, z))
        k.add(f"ridge{i}", blade([base, base + (0, 0.012, 0.016), base + (0, 0.022, 0.032)], [0.018, 0.01, 0.002], turn,
                                 0.45), NAVY, "crest", fuse=False)
    tl = np.array(TAIL)
    seg = np.linalg.norm(np.diff(tl, axis=0), axis=1); cum = np.concatenate([[0], np.cumsum(seg)])
    for i, f in enumerate((0.1, 0.24)):
        s = f * cum[-1]
        c = np.array([np.interp(s, cum, tl[:, a]) for a in range(3)])
        r = np.interp(s, cum, TAIL_R)
        j = min(int(np.searchsorted(cum, s)), len(tl) - 1)
        dd = unit(*(tl[j] - tl[j - 1]))
        base = c + np.array([0, 0, r - 0.004])
        tip = base + dd * 0.018 + np.array([0, 0, 0.026])
        k.add(f"tridge{i}", blade([base, tip], [0.016, 0.002], unit(*np.cross(dd, (0, 0, 1.0))) * 0.6 + dd * 0.4, 0.4),
              NAVY, "tail", fuse=False)


# --- the face: patches on the fused head, laid before the body's build ------------------------------------------------
CB, CI = srgb(BLUE), srgb(INK)


def add_nose(at=H + HS * np.array((0, -0.093, -0.025)), half=(0.01, 0.0075), color=NOSE):
    k.decal(at, unit(0, -1, 0.1), half, color, eps=0.003, outline=polygon([(-1, 0.7), (1, 0.7), (0, -1)]),
            rings=6, segs=24)


CM, CT = srgb(MOUTH), srgb(TONGUE)


def mouth(u, v):
    """A small open mouth: a flat top edge, a round lower lip; dark red inside, a pink tongue in its lower half."""
    c = np.tile(CB, (len(u), 1))
    top = 0.6 - 0.15 * u * u
    open_ = ((u / 0.8) ** 2 + ((v - 0.6) / 1.45) ** 2 < 1) & (v < top)
    c[open_] = CM
    c[open_ & ((u / 0.55) ** 2 + ((v + 0.85) / 0.75) ** 2 < 1)] = CT
    return c


def add_mouth(at=H + HS * np.array((0, -0.088, -0.052)), half=(0.026, 0.023), look=mouth):
    k.decal(at, unit(0, -1, -0.3), half, look, eps=0.003, rings=16, segs=72)


# The eyes: Eevee's construction, dark with a darker middle and a white glint up and out; a little larger and set wide
EYE_DIR = unit(0.47, -0.88, -0.02)
EYE_HALF = (0.026, 0.035)
CE, CP = srgb(EYE), srgb(PUPIL)


def eye(u, v):
    c = np.tile(CE, (len(u), 1))
    c[((u + 0.05) / 0.62) ** 2 + ((v + 0.08) / 0.7) ** 2 < 1] = CP
    c[((u - 0.2) / 0.32) ** 2 + ((v - 0.38) / 0.28) ** 2 < 1] = (1, 1, 1)
    return c
E = H + EYE_DIR * 0.086 * HS + np.array([0, 0, 0.002])


def eyes(state):
    k.decal(E, EYE_DIR, EYE_HALF, eye_look(state, eye, CB, CI), eps=0.004, rings=20, segs=56, mirror=True)


add_body()
add_legs()
add_head()
add_tail()
add_ridge()
add_mask()
add_fins()
add_collar()
add_crown()
add_nose()
add_mouth()

k.build(voxel=0.0052)
k.eye_states(eyes)
k.export(out)
