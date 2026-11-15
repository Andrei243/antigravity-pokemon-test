# Taillow sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/taillow.py -- <the export's Taillow.glb> <out.glb>
# A small round swallow: dark navy all over, a red face and throat running from a point on the forehead down to a
# point on the breast, a white belly under it; a short yellow beak; big black eyes under a heavy flat lid; long folded
# wings whose tips cross behind it; a forked tail of two pointed prongs rising behind; short dark legs with yellow
# claws. Written to be grown from: Swellow imports it (sibling("taillow")) and keeps its frame and its functions.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, unit, eye_look, polygon, outward, args

# --- the line's colours -------------------------------------------------------------------------------------------
NAVY, RED, WHITE, BEAK = "#2F3A6A", "#D4405A", "#F2F2F6", "#F0C238"
FOOT, CLAW, BLACK = "#2A3152", "#F2C230", "#141418"


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
    """A polygon symmetric about a = 0 from its right half (from the top middle round to the bottom middle)."""
    return half + [(-a, z) for a, z in half[::-1][1:-1]]


def frame_of(d, side=None):
    """A right-handed frame round a direction: (d, side, n), side across the piece's width, n through its thickness."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    s = np.array(side if side is not None else np.cross(d, (0, 0, 1.0)), float)
    if np.linalg.norm(s) < 1e-3:
        s = np.array([1.0, 0, 0])
    s = s - d * (s @ d); s /= np.linalg.norm(s)
    return d, s, np.cross(d, s)


def spike(base, tip, w, t, side=None, at=0.35):
    """A pointed feather: a flat-faced kite, w wide across side, t thick, widest at `at` of its length."""
    base, tip = np.array(base, float), np.array(tip, float)
    d, s, n = frame_of(tip - base, side)
    m = base + (tip - base) * at
    return hull([base + s * w * 0.3, base - s * w * 0.3, base + n * t * 0.5, base - n * t * 0.5,
                 m + s * w * 0.5, m - s * w * 0.5, m + n * t * 0.5, m - n * t * 0.5, tip])


def pointed(V):
    t = np.clip(-V[:, 1], 0, 1)
    V = V.copy(); V[:, 0] *= 1 - 0.78 * t; V[:, 2] *= 1 - 0.62 * t
    return V


# --- the frame: every place and size from the skeleton's joints ----------------------------------------------------
def frame():
    F = SimpleNamespace()
    F.H = np.array([0, -0.058, 0.282])               # the head's centre, over the head's joint (0, -0.05, 0.24)
    F.HR = np.array([0.1, 0.094, 0.09])
    F.BODY = ((0, 0.02, 0.152), (0.11, 0.128, 0.12), (-12, 0, 0))
    F.NECK = ((0, -0.04, 0.218), (0.098, 0.092, 0.08))
    # the folded wing fused along the side, shoulder to the end of its covert feathers
    F.WING = ([(0.085, -0.035, 0.24), (0.108, 0.04, 0.18), (0.098, 0.13, 0.11), (0.07, 0.2, 0.07)],
              [0.042, 0.072, 0.062, 0.025], 0.34)
    # the long primaries: a pointed blade from inside the wing back past the tail to its tip
    F.PRIMARY = ((0.07, 0.12, 0.12), (0.052, 0.36, 0.035), 0.072, 0.03)
    # the forked tail: two prongs from the rump, back, up and out
    F.TAILBASE = np.array([0.012, 0.11, 0.17])
    F.PRONG = (np.array([0.075, 0.37, 0.25]), 0.058, 0.03)
    F.LEGX, F.LEGTOP, F.ANKLE, F.LEGY = 0.036, 0.07, 0.018, -0.01
    F.LEGR = (0.019, 0.017)
    F.TOE, F.HEEL, F.TOER = 0.044, 0.026, 0.0105
    F.BEAK = ((0, -0.175, 0.264), (0.034, 0.055, 0.027), (8, 0, 0))
    F.EYEDIR = unit(0.6, -0.76, 0.2)
    F.EYE = (0.031, 0.035)                           # the eye's half sizes
    F.LIDLINE = 0.42                                 # the heavy flat lid cuts the eye above this v
    F.R = 0.1                                        # the radius of the head for its round coordinates
    F.VOXEL = 0.0055
    # the red face and throat in round coordinates (angle round the head's axis x R, z), its right half from the top
    F.REDMASK = [(0.0, 0.362), (0.03, 0.34), (0.052, 0.318), (0.068, 0.3), (0.08, 0.272), (0.11, 0.24),
                 (0.135, 0.205), (0.12, 0.17), (0.075, 0.135), (0.0, 0.1)]
    # the white belly below it, its right half
    F.WHITEMASK = [(0.0, 0.215), (0.13, 0.215), (0.165, 0.13), (0.2, 0.06), (0.28, -0.05), (0.0, -0.05)]
    return F


def eye_centre(F):
    de = F.EYEDIR
    return F.H + de / np.linalg.norm(de / F.HR)


def round_head(F, P):
    P = np.atleast_2d(P)
    return np.arctan2(P[:, 0], -(P[:, 1] - F.H[1])) * F.R, P[:, 2]


# --- the parts ----------------------------------------------------------------------------------------------------
def body(k, F, colour=NAVY):
    c, r, rot = F.BODY
    k.add("body", ell(c, r, rot=rot), colour, "body")
    k.add("neck", ell(*F.NECK), colour, "head")
    k.add("head", ell(F.H, F.HR), colour, "head")


def wings(k, F, colour=NAVY):
    pts, radii, f = F.WING
    k.add("wing", blade(pts, radii, (1, 0, 0), f), colour, "wing{s}", mirror=True)
    base, tip, w, t = F.PRIMARY
    # the plate tilted to face outward and up a little, so its face and not its edge shows from behind
    k.add("primary", spike(base, tip, w, t, side=(0.35, 0, 1), at=0.3), colour, "wing{s}", fuse=False, mirror=True)


def tail(k, F, colour=NAVY):
    """Two pointed prongs from the rump, back, up and out to each side: a swallow's fork."""
    m = np.array([-1, 1, 1.0])
    tip, w, t = F.PRONG
    for s, side in ((1, "R"), (-1, "L")):
        b = F.TAILBASE * (m if s < 0 else 1)
        e = tip * (m if s < 0 else 1)
        k.add(f"prong{side}", spike(b, e, w, t, at=0.3), colour, "tail", fuse=False)


def face(k, F, red=RED, white=WHITE):
    """The white belly, then the red face and throat over it from the forehead's point to the breast's point."""
    W = mirrored(F.WHITEMASK)
    k.paint(lambda P: poly_mask(*round_head(F, P), W) * (P[:, 1] < 0.09), white, on=["body", "neck"])
    RM = mirrored(F.REDMASK)
    k.paint(lambda P: poly_mask(*round_head(F, P), RM) * (P[:, 1] < F.H[1] + 0.06), red)


def beak(k, F, colour=BEAK):
    c, r, rot = F.BEAK
    k.add("beak", ell(c, r, rot=rot, deform=pointed), colour, "head", fuse=False, surface="shell")


def legs(k, F, foot=FOOT, claw=CLAW):
    for s, side in ((1, "R"), (-1, "L")):
        x = F.LEGX * s
        y = F.LEGY
        ank = np.array([x * 1.06, y - 0.01, F.ANKLE])
        k.add(f"leg{side}", tube([(x, y, F.LEGTOP), (x * 1.03, y - 0.005, (F.LEGTOP + F.ANKLE) / 2), ank],
                                 [F.LEGR[0], F.LEGR[0] * 0.95, F.LEGR[1]]), foot, f"leg{side}", fuse=False,
              surface="scales")
        for j, ang in enumerate((-30, 0, 30, 180)):
            rr = math.radians(ang)
            d = np.array([math.sin(rr) * s, -math.cos(rr), 0])
            ln = F.TOE if ang != 180 else F.HEEL
            tip = ank + d * ln + np.array([0, 0, -F.ANKLE + F.TOER * 0.9])
            k.add(f"toe{side}{j}", tube([ank, (ank + tip) / 2 + np.array([0, 0, 0.003]), tip],
                                        [F.TOER * 1.15, F.TOER, F.TOER * 0.9]), foot, f"leg{side}", fuse=False,
                  surface="scales")
            cd = d + np.array([0, 0, -0.45])
            k.add(f"claw{side}{j}", spike(tip, tip + cd * F.TOER * 2.2, F.TOER * 1.5, F.TOER * 1.3), claw,
                  f"leg{side}", fuse=False, surface="shell")


# --- the eyes -----------------------------------------------------------------------------------------------------
WHITEC, INK = np.array([1.0, 1.0, 1.0]), srgb("#141418")
ROUND = [(-1.0, 0.0), (-0.85, 0.55), (-0.45, 0.92), (0.1, 1.0), (0.6, 0.85), (0.95, 0.4), (1.0, -0.1),
         (0.8, -0.6), (0.35, -0.95), (-0.2, -0.98), (-0.7, -0.7)]


def eye_open(F, lid):
    """A white eye, a big black pupil toward its front (u toward the back), a white glint, a heavy flat lid on top."""
    def look(u, v):
        c = np.tile(WHITEC, (len(u), 1))
        c[((u + 0.18) / 0.66) ** 2 + ((v + 0.08) / 0.8) ** 2 < 1] = INK
        c[((u + 0.36) / 0.2) ** 2 + ((v - 0.2) / 0.2) ** 2 < 1] = WHITEC
        c[v > F.LIDLINE] = lid
        c[np.abs(v - F.LIDLINE) < 0.11] = INK
        return c
    return look


def eyes(k, F, lid=NAVY, open_look=None, outline=ROUND, fierce=None):
    LID = srgb(lid)
    look_open = open_look or eye_open(F, LID)
    E = eye_centre(F)
    m = np.array([-1, 1, 1.0])
    def lay(state):
        if state == "Fierce" and fierce is not None:
            look = fierce
        else:
            look = eye_look(state, look_open, LID, INK, line=0.16)
        # two patches laid by hand (a mirrored patch's outline is not mirrored): u toward the back on both
        k.decal(E, F.EYEDIR, F.EYE, look, eps=0.006, outline=polygon(outline), rings=20, segs=56)
        k.decal(E * m, F.EYEDIR * m, F.EYE, lambda u, v: look(-u, v), eps=0.006,
                outline=polygon([(-a, b) for a, b in outline]), rings=20, segs=56)
    return lay


def taillow(src, out):
    F = frame()
    k = Kit(src, coat="fur")
    body(k, F)
    wings(k, F)
    face(k, F)
    beak(k, F)
    tail(k, F)
    legs(k, F)
    k.build(voxel=F.VOXEL)
    k.eye_states(eyes(k, F))
    k.export(out)


if __name__ == "__main__":
    taillow(*args())
