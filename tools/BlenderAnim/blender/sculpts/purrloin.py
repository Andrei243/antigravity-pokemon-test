# Purrloin sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/purrloin.py -- <the export's Purrloin.glb> <out.glb>
# A slim purple cat sitting up (the skeleton's pose: the forelegs straight, the haunches folded on the ground): a big
# head with huge pointed ears flaring up and out, lined pink, the pink running down over each eye into a mask; spiky
# tufts out from the cheeks; three cream streaks on the brow; the cream lower face round a small cat's mouth; half-shut
# sly green eyes under the pink; a cream stripe down the throat and chest, cream patches on the haunches and cream
# toes; a long thin tail rising behind to a sickle with a jagged edge.
#
# Written to be grown from (liepard.py keeps this frame: the head, the ears and their lining, the mask, the eyes, the
# nose and mouth, the sickle at the tail's end). Each part is a function taking the kit and its sizes; the calls are
# at the end under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, slab, hull, srgb, inside, smoothstep, unit, tri, eye_look, polygon,
                       args)

# --- colours ----------------------------------------------------------------------------------------------------------
PURPLE = "#7C4E9C"          # between the drawing's grey-violet and the 3D model's brighter purple
CREAM = "#F2E2A8"
PINK = "#E86A9E"
GREEN, PUPIL = "#2E9E6A", "#10261C"
EYE_W, INK = "#F6F6EE", "#1E1424"
NOSE = "#3A2236"

# --- the frame (the skeleton's joints: body (0, 0, 0.16), head (0, -0.02, 0.21), ears (+-0.04, -0.02, 0.30),
#     forelegs (+-0.04, -0.05, 0.13), hind legs (+-0.04, 0.04, 0.08), tail (0, 0.07, 0.07)) -----------------------------
H = np.array([0, -0.046, 0.258])         # the head's middle, up and forward of its joint
HR = np.array([0.08, 0.066, 0.064])      # a tenth over the drawing's measure
LEG_X = 0.034


def on_head(x, z, lift=0.0, h=None, hr=None):
    """The head's front point at (x, z), lifted along its normal."""
    h = H if h is None else h
    hr = HR if hr is None else hr
    zn = (z - h[2]) / hr[2]
    q = max(0.0, 1 - zn * zn - (x / hr[0]) ** 2)
    p = np.array([x, h[1] - hr[1] * math.sqrt(q), z])
    n = (p - h) / hr ** 2; n /= np.linalg.norm(n)
    return p + n * lift, n


def in_poly(x, y, pts):
    pts = np.array(pts, float)
    res = np.zeros(len(x), bool)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        res ^= ((y1 > y) != (y2 > y)) & (x < (x2 - x1) * (y - y1) / (y2 - y1 + 1e-12) + x1)
    return res


def poly_mask(x, y, pts, edge=0.0025):
    """A soft-edged mask of a polygon in a plane of two coordinates."""
    pts = np.array(pts, float)
    d = np.full(len(x), 9.0)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        ex, ey = x2 - x1, y2 - y1
        t = np.clip(((x - x1) * ex + (y - y1) * ey) / (ex * ex + ey * ey + 1e-12), 0, 1)
        d = np.minimum(d, np.hypot(x - x1 - t * ex, y - y1 - t * ey))
    return inside(np.where(in_poly(x, y, pts), -d, d), edge)


def round_head(P, h, hr):
    """Coordinates round the head's upright axis: the arc out from the face's middle (|x| side), and height."""
    return np.abs(np.arctan2(P[:, 0], -(P[:, 1] - h[1]))) * hr[0], P[:, 2]


def along(P, pts, n=200):
    """Nearest place on a polyline (|x| taken, so a mirrored part's both sides): its share of the length and the offset."""
    pts = np.array(pts, float)
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    cum = np.concatenate([[0], np.cumsum(seg)]); L = cum[-1]
    s = np.linspace(0, L, n)
    line = np.stack([np.interp(s, cum, pts[:, i]) for i in range(3)], axis=1)
    Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
    j = np.concatenate([((Q[a:a + 4000, None, :] - line[None]) ** 2).sum(2).argmin(1) for a in range(0, len(Q), 4000)])
    return s[j] / L, Q - line[j]


# --- the head: wide across the cheeks, a short muzzle; spiky tufts out from the cheeks ---------------------------------
def cheeky(V):
    V = V.copy()
    low = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 + 0.1 * (1 - V[:, 2] ** 2) - 0.12 * low ** 2
    return V


def add_head(k, h=H, hr=HR, color=PURPLE, neck=None, tufts=True):
    k.add("head", ell(h, hr, deform=cheeky), color, "head")
    k.add("muzzle", ell(h + hr * np.array([0, -0.82, -0.45]), hr * np.array([0.5, 0.42, 0.4])), color, "head")
    if neck is not None:
        k.add("neck", tube(neck[0], neck[1]), color, "head")
    if tufts:
        # Three points of fur out from each cheek, the lowest pointing a little down
        for i, (z, out, down, r) in enumerate(((0.12, 0.05, 0.0, 0.016), (-0.15, 0.052, -0.015, 0.016),
                                               (-0.42, 0.04, -0.025, 0.013))):
            b = h + np.array([hr[0] * 0.88, -hr[1] * 0.1, hr[2] * z])
            t = b + np.array([out, 0.012, down])
            k.add(f"tuft{i}", blade([b, b + (t - b) * 0.5, t], [r, r * 0.65, 0.002], (0, 1, 0.2), 0.5), color,
                  "head", mirror=True)


# --- the ears: huge and pointed, from the top corners of the head up and out, flat front to back, the pink lining on
#     their front faces ---------------------------------------------------------------------------------------------
EAR = [(0.03, -0.05, 0.29), (0.065, -0.046, 0.322), (0.1, -0.04, 0.355), (0.132, -0.032, 0.384)]
EAR_R = [0.036, 0.034, 0.02, 0.003]


def add_ears(k, ear=EAR, radii=EAR_R, color=PURPLE, lining=PINK, width=0.026, bone="ear{s}", flat=0.32):
    k.add("ear", blade(ear, radii, (0, 1, 0), flat), color, bone, mirror=True)
    # a small tuft on the ear's outer edge, halfway up
    e = np.array(ear)
    mid = e[1] + (e[2] - e[1]) * 0.3
    d = unit(*(e[-1] - e[0]))
    out = unit(d[2], 0, -d[0])
    k.add("eartuft", blade([mid, mid + out * 0.022 + d * 0.004, mid + out * 0.04 - d * 0.002],
                           [radii[1] * 0.5, radii[1] * 0.3, 0.002], (0, 1, 0), 0.5), color, bone, mirror=True)

    def lin(P):
        t, off = along(P, ear)
        half = width * np.sin(np.pi * np.clip((t + 0.15) / 1.02, 0, 1)) ** 0.7
        across = np.cross(d, (0, 1.0, 0)); across /= np.linalg.norm(across)
        w = off @ across
        return inside(np.abs(w) - half, 0.002) * inside(off[:, 1] + 0.001, 0.0015) * inside(t - 0.86, 0.01)
    k.paint(lin, lining, on=["ear"])


# --- the pink mask: from the ear's lining down over each eye, its lower edge the eye's top ----------------------------
MASK = [(0.016, 0.264), (0.024, 0.274), (0.03, 0.283), (0.042, 0.296), (0.062, 0.306), (0.088, 0.296),
        (0.084, 0.276), (0.07, 0.262), (0.05, 0.256), (0.028, 0.254), (0.012, 0.256)]


def add_mask(k, h=H, hr=HR, outline=MASK, color=PINK):
    front = lambda P: (P[:, 1] < h[1] - hr[1] * 0.25)
    k.paint(lambda P: poly_mask(*round_head(P, h, hr), outline, 0.0018) * front(P), color, on=["head"])


# --- the cream of the lower face: under the eyes, out to the cheeks' tufts and round the muzzle and chin --------------
FACE = [(0.0, 0.246), (0.012, 0.247), (0.03, 0.244), (0.05, 0.248), (0.072, 0.252), (0.09, 0.246),
        (0.092, 0.226), (0.07, 0.205), (0.04, 0.196), (0.0, 0.193), (-0.001, 0.22)]


def add_face(k, h=H, hr=HR, outline=FACE, color=CREAM):
    front = lambda P: (P[:, 1] < h[1] - hr[1] * 0.1) * (P[:, 2] > h[2] - hr[2] * 1.4)
    k.paint(lambda P: poly_mask(*round_head(P, h, hr), outline, 0.0018) * front(P), color,
            on=["head", "muzzle", "tuft", "neck"])


# --- three cream streaks on the brow: one up the middle, one slanting out over each eye --------------------------------
def add_streaks(k, h=H, hr=HR, color=CREAM, size=1.0):
    def streak(x, z, tilt, half):
        p, n = on_head(x, z, 0.0, h, hr)
        k.decal(p, n, half, color, up=unit(math.sin(math.radians(tilt)), 0, math.cos(math.radians(tilt))),
                eps=0.0025, outline=polygon([(0, 1), (0.9, 0.1), (0.5, -0.8), (0, -1), (-0.5, -0.8), (-0.9, 0.1)]),
                rings=8, segs=36)
    s = size
    streak(0.0, h[2] + hr[2] * 0.62, 0, (0.0062 * s, 0.017 * s))
    for side in (1, -1):
        streak(side * hr[0] * 0.3, h[2] + hr[2] * 0.47, -side * 34, (0.0055 * s, 0.013 * s))


# --- the nose and the cat's mouth -------------------------------------------------------------------------------------
def add_mouth(k, at, half, ground=CREAM, nose=True):
    CG, CI = srgb(ground), srgb(INK)
    def mouth(u, v):
        c = np.tile(CG, (len(u), 1))
        c[(np.abs(v - (-0.3 + 1.7 * (np.abs(u) - 0.42) ** 2)) < 0.18) & (np.abs(u) < 0.9) & (v < 0.3)] = CI
        c[(np.abs(u) < 0.1) & (v > 0.1)] = CI
        return c
    k.decal(at, unit(0, -1, -0.2), half, mouth, eps=0.0025, rings=10, segs=48)
    if nose:
        k.decal(at + np.array([0, -0.002, half[1] * 1.15]), unit(0, -1, 0.2), (half[0] * 0.38, half[1] * 0.42), NOSE,
                eps=0.003, outline=polygon([(-1, 0.7), (1, 0.7), (0, -1)]), rings=6, segs=24)


# --- the eyes: sly and half shut, a straight dark lid sloping down to the nose cuts the top; a green iris looking
#     forward, a dark pupil, a glint. The outline is not symmetric, so each is a patch of its own, u outward ------------
OUTLINE = [(-1, -0.02), (-0.5, 0.2), (0.3, 0.42), (1, 0.5), (0.92, -0.05), (0.4, -0.5), (-0.3, -0.58), (-0.85, -0.35)]


def eye_colour(green=GREEN):
    CW, CG, CP, CI = srgb(EYE_W), srgb(green), srgb(PUPIL), srgb(INK)
    rim = polygon(OUTLINE)
    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        c[np.hypot((u + 0.02) / 0.5, (v + 0.08) / 0.66) < 1] = CG
        c[np.hypot((u + 0.02) / 0.17, (v + 0.08) / 0.48) < 1] = CP
        c[np.hypot((u - 0.16) / 0.12, (v + 0.02) / 0.13) < 1] = (1, 1, 1)
        rr = np.hypot(u, v) / np.array([rim(t) for t in np.arctan2(v, u)])
        c[(rr > 0.66) & (v > 0.0 + 0.3 * u)] = CI           # the heavy lid along the top edge
        c[rr > 0.86] = CI
        return c
    return eye


def lay_eyes(k, state, h=H, hr=HR, x=0.034, dz=-0.004, half=(0.023, 0.017), lid=PINK, green=GREEN):
    p, n = on_head(x, h[2] + dz, 0.0, h, hr)
    n = unit(n[0] * 1.15, n[1], n[2])
    CL, CI = srgb(lid), srgb(INK)
    look = eye_look(state, eye_colour(green), CL, CI, line=0.16)
    k.decal(p, n, half, look, eps=0.003, outline=polygon(OUTLINE), rings=20, segs=56)
    q = np.array([-1, 1, 1.0])
    k.decal(p * q, n * q, half, lambda u, v: look(-u, v), eps=0.003,
            outline=polygon([(-a, b) for a, b in OUTLINE]), rings=20, segs=56)


# --- the sickle at the tail's end: one thick plate, a crescent hooking back toward the body with two jags on its back
#     edge, in the plane of the tail's own sweep (back and out to +X) ------------------------------------------------
SICKLE = [(0.0, -0.004), (0.014, 0.012), (0.024, 0.034), (0.03, 0.05), (0.036, 0.048), (0.032, 0.062),
          (0.038, 0.066), (0.026, 0.078), (0.004, 0.088), (-0.022, 0.086), (-0.04, 0.074),
          (-0.02, 0.07), (-0.004, 0.06), (0.006, 0.042), (0.002, 0.02), (-0.008, 0.004)]


def add_sickle(k, at, out, scale, thick, color=PURPLE, bone="tail"):
    """at: the root; out: the way the tail sweeps (its plane holds out and up); the crescent hooks back toward -out."""
    pts = [(a * scale, b * scale) for a, b in SICKLE]
    k.add("sickle", slab(pts, at, out, (0, 0, 1.0), thick), color, bone, fuse=False)


# --- Purrloin's own: a sitting body, straight forelegs, haunches on the ground, the tail and the cream marks ---------
OUT = unit(0.57, 0.82, 0)          # the tail's sweep: back and some 35 degrees out to +X


def add_body(k):
    # A slim body sitting up: the chest high and forward, the belly and rump low and round on the ground
    k.add("chest", ell((0, -0.03, 0.15), (0.046, 0.044, 0.062), rot=(-22, 0, 0)), PURPLE, "body")
    k.add("belly", ell((0, 0.012, 0.085), (0.054, 0.058, 0.06), rot=(-30, 0, 0)), PURPLE, "body")
    k.add("rump", ell((0, 0.05, 0.05), (0.056, 0.05, 0.048)), PURPLE, "body")
    k.add("neck", tube([(0, -0.03, 0.17), (0, -0.038, 0.2), (0, -0.044, 0.225)], [0.032, 0.03, 0.032]), PURPLE, "head")


def add_legs(k):
    # Straight forelegs from the shoulders to small round paws; the haunches folded on the ground beside the belly
    k.add("shoulder", ell((LEG_X - 0.004, -0.045, 0.125), (0.026, 0.03, 0.04)), PURPLE, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X, -0.048, 0.12), (LEG_X + 0.002, -0.055, 0.07), (LEG_X + 0.003, -0.06, 0.016)],
                      [0.02, 0.017, 0.016]), PURPLE, "leg{s}", mirror=True)
    k.add("paw", ell((LEG_X + 0.003, -0.07, 0.011), (0.018, 0.024, 0.012)), PURPLE, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X + 0.012, 0.035, 0.05), (0.036, 0.054, 0.045), rot=(-15, 0, 0)), PURPLE, "hind{s}",
          mirror=True)
    k.add("hpaw", ell((LEG_X + 0.022, -0.022, 0.011), (0.019, 0.032, 0.012)), PURPLE, "hind{s}", mirror=True)


def add_tail(k):
    b = np.array([0, 0.085, 0.05])
    z = np.array([0, 0, 1.0])
    pts = [b, b + OUT * 0.05 - z * 0.012, b + OUT * 0.1 + z * 0.01, b + OUT * 0.125 + z * 0.07,
           b + OUT * 0.118 + z * 0.13, b + OUT * 0.1 + z * 0.17]
    k.add("tail", tube(pts, [0.013, 0.011, 0.009, 0.0085, 0.0085, 0.008]), PURPLE, "tail")
    add_sickle(k, pts[-1] - z * 0.006, OUT, 1.3, 0.013)


def add_marks(k):
    # A cream stripe down the throat and the chest, from the chin, widening a little to the belly
    def stripe(P):
        z = P[:, 2]
        w = 0.016 + 0.012 * smoothstep(0.17, 0.08, z) + 0.003 * tri(z / 0.02)
        return inside(np.abs(P[:, 0]) - w, 0.002) * (P[:, 1] < -0.02) * inside(z - 0.2, 0.003) * inside(0.07 - z, 0.003)
    k.paint(stripe, CREAM, on=["chest", "belly", "neck"])
    # A jagged cream patch on the side of each haunch
    def haunch(P):
        q = P - np.array([0, 0.03, 0.06])
        q[:, 0] = np.abs(P[:, 0]) - (LEG_X + 0.04)
        edge = 0.016 + 0.005 * tri(np.arctan2(q[:, 2], q[:, 1]) * 5 / (2 * math.pi))
        return inside(np.hypot(q[:, 1], q[:, 2]) - edge, 0.002) * (np.abs(P[:, 0]) > LEG_X + 0.02)
    k.paint(haunch, CREAM, on=["thigh"])
    # Cream toes on all four paws
    def toes(P):
        return inside(P[:, 2] - 0.016, 0.002) * (P[:, 1] < -0.02)
    k.paint(toes, CREAM, on=["paw", "hpaw", "leg"])


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_body(k)
    add_legs(k)
    add_head(k)
    add_ears(k)
    add_tail(k)
    add_marks(k)
    add_face(k)
    add_mask(k)
    add_streaks(k)
    add_mouth(k, H + np.array([0, -HR[1] * 1.2, -HR[2] * 0.5]), (0.015, 0.009))
    k.build(voxel=0.0036, band=0.004)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)
