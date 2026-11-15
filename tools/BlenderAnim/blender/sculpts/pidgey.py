# Pidgey sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pidgey.py -- <the export's Pidgey.glb> <out.glb>
# A small plump bird: a brown crown, back, wings and tail over a cream face, throat and belly; a tuft of brown
# feathers on the crown with pale ones swept back over each brow; a white almond eye with a black pupil and a black
# wedge running back and down from it; a short pink beak; a fan of three brown tail feathers; short pink legs with
# white claws. Written to be grown from: Pidgeotto and Pidgeot import it (sibling("pidgey")) and keep its frame, its
# functions and its way of making the eyes, the mark, the beak, the wings and the feet.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, slab, srgb, inside, tri, unit, eye_look, polygon, args

# --- the line's colours -------------------------------------------------------------------------------------------
BROWN, CREAM, PALE, DARK, EDGE = "#B47A48", "#F5E6AC", "#F8EDB8", "#8A5A34", "#DDB984"
BLACK, BEAK, BEAKLOW, FOOT, CLAW = "#1A1412", "#E3A4B0", "#C9808E", "#E9B3BF", "#F4F2F0"


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


def quill(base, tip, w0, w1, t, side=None):
    """A feather with a broad square end (a tail feather): w0 wide at its root, w1 at its end, t thick."""
    base, tip = np.array(base, float), np.array(tip, float)
    d, s, n = frame_of(tip - base, side)
    L = np.linalg.norm(tip - base)
    e = tip - d * L * 0.12
    return hull([base + s * w0 / 2 + n * t / 2, base + s * w0 / 2 - n * t / 2, base - s * w0 / 2 + n * t / 2,
                 base - s * w0 / 2 - n * t / 2, e + s * w1 / 2 + n * t * 0.3, e + s * w1 / 2 - n * t * 0.3,
                 e - s * w1 / 2 + n * t * 0.3, e - s * w1 / 2 - n * t * 0.3, tip + s * w1 * 0.25, tip - s * w1 * 0.25])


def pointed(V):
    t = np.clip(-V[:, 1], 0, 1)
    V = V.copy(); V[:, 0] *= 1 - 0.72 * t; V[:, 2] *= 1 - 0.55 * t
    return V


def hooked(V):
    """The upper bill: pointed, its tip turned down a little."""
    V = pointed(V)
    t = np.clip(-V[:, 1], 0, 1)
    V[:, 2] -= 0.35 * t ** 2
    return V


# --- the frame: every place and size from the skeleton's joints ----------------------------------------------------
def frame():
    F = SimpleNamespace()
    F.H = np.array([0, -0.095, 0.41])                # the head's centre, over the head's joint (0, -0.07, 0.35)
    F.HR = np.array([0.105, 0.108, 0.098])
    F.BODY = ((0, 0.005, 0.238), (0.15, 0.175, 0.172), (-10, 0, 0))
    F.BREAST = ((0, -0.08, 0.26), (0.128, 0.115, 0.145))
    F.NECK = ((0, -0.07, 0.35), (0.108, 0.1, 0.085))
    # the folded wing, shoulder to tip, fused along the side
    F.WING = ([(0.108, -0.03, 0.318), (0.136, 0.04, 0.285), (0.14, 0.12, 0.22), (0.105, 0.21, 0.15)],
              [0.05, 0.078, 0.064, 0.012], 0.32)
    F.WINGCREAM = 0.255                               # the wing's cream lower feathers end above this height at its front
    F.LEGX, F.LEGTOP, F.ANKLE, F.LEGY = 0.062, 0.115, 0.024, -0.02
    F.LEGR = (0.022, 0.018)
    F.TOE, F.HEEL, F.TOER = 0.058, 0.032, 0.0115
    F.BEAK = ((0, -0.21, 0.392), (0.029, 0.052, 0.026))
    F.BEAKLOW = ((0, -0.198, 0.368), (0.02, 0.034, 0.011))
    F.EYEDIR = unit(0.56, -0.8, 0.12)
    F.EYE = (0.027, 0.019)                            # the eye's half sizes
    F.R = 0.11                                        # the radius of the head for its round coordinates
    F.VOXEL = 0.0055
    F.TAILBASE = np.array([0, 0.15, 0.245])
    # the cream's edge down the neck's side and round under the body: (angle round the head's axis in radii, z)
    F.CREAMLOW = [(1.55, 0.26), (1.55, 0.2), (1.75, 0.11), (3.2, 0.05), (3.2, -0.2)]
    return F


def eye_centre(F):
    de = F.EYEDIR
    return F.H + de / np.linalg.norm(de / F.HR)


def round_head(F, P):
    P = np.atleast_2d(P)
    return np.arctan2(P[:, 0], -(P[:, 1] - F.H[1])) * F.R, P[:, 2]


def eye_frame(F):
    de = F.EYEDIR
    right = np.cross((0, 0, 1.0), de); right /= np.linalg.norm(right)
    return right, np.cross(de, right)


def on_head(F, u, v):
    """The point of the head's ellipsoid under (u, v) in the eye's own units (u toward the back, v up)."""
    de = F.EYEDIR
    E = eye_centre(F)
    ri, up = eye_frame(F)
    o = E + de * 0.35 + ri * u * F.EYE[0] + up * v * F.EYE[1]
    oc, dd = (o - F.H) / F.HR, -de / F.HR
    a, b, c = dd @ dd, 2 * oc @ dd, oc @ oc - 1
    return o + dd * F.HR * (-b - math.sqrt(max(b * b - 4 * a * c, 0))) / (2 * a)


def in_round(F, uv):
    """Points given in the eye's units, as (a x R, z) round the head."""
    out_ = []
    for u, v in uv:
        s, z = round_head(F, on_head(F, u, v))
        out_.append((float(s[0]), float(z[0])))
    return out_


# --- the parts ----------------------------------------------------------------------------------------------------
def body(k, F, back=BROWN):
    c, r, rot = F.BODY
    k.add("body", ell(c, r, rot=rot), back, "body")
    k.add("breast", ell(*F.BREAST), back, "body")
    k.add("neck", ell(*F.NECK), back, "head")
    k.add("head", ell(F.H, F.HR), back, "head")


def wings_folded(k, F, colour=BROWN, cream=CREAM):
    pts, radii, f = F.WING
    k.add("wing", blade(pts, radii, (1, 0, 0), f), colour, "wing{s}", mirror=True)
    # the lower feathers at the wing's front are cream behind a ragged edge
    y0, y1 = pts[0][1], pts[0][1] + 0.6 * (pts[-1][1] - pts[0][1])
    k.paint(lambda P: inside(P[:, 2] - (F.WINGCREAM - 0.35 * (P[:, 1] - y0) + 0.012 * tri(P[:, 1] / 0.03)), 0.003)
            * inside(P[:, 1] - y1, 0.004), cream, on=["wing"])


def face(k, F, cream=CREAM, top=None):
    """
    The cream face, throat and belly, painted round the head's axis: below the brow line through the eye's top, down
    behind the black mark and round the body's sides; then the black wedge from the eye's back end down toward the neck.
    """
    E = eye_centre(F)
    s_e, z_e = round_head(F, E)
    s_e, z_e = float(s_e[0]), float(z_e[0])
    # the brow line (cream under it), in the eye's units, then down the neck and round under the body
    brow = in_round(F, [(-3.2, -0.3), (-2.0, 0.5), (-1.2, 0.95), (0.0, 1.05), (1.0, 0.95), (2.2, 0.0), (2.6, -2.6)])
    low = [(F.R * a, z) for a, z in F.CREAMLOW]
    half = [(0.0, brow[0][1])] + brow[1:] + low + [(0.0, -0.2)]
    CREAMP = mirrored(half)
    k.paint(lambda P: poly_mask(*round_head(F, P), CREAMP), cream, on=["body", "breast", "neck", "head"])
    # the black mark: from the eye's back end, a wedge back and down to a point
    MARK = in_round(F, [(0.55, 0.85), (1.6, 0.95), (2.7, 0.55), (2.6, -0.6), (2.0, -2.9), (1.2, -1.2), (0.6, -0.7)])
    MARKL = [(-a, z) for a, z in MARK]
    k.paint(lambda P: np.maximum(poly_mask(*round_head(F, P), MARK), poly_mask(*round_head(F, P), MARKL))
            * (P[:, 2] > F.H[2] - 0.2) * (P[:, 1] < F.H[1] + 0.12), BLACK)


def beak(k, F, upper=BEAK, lower=BEAKLOW):
    c, r = F.BEAK
    k.add("beak", ell(c, r, rot=(10, 0, 0), deform=hooked), upper, "head", fuse=False, surface="shell")
    c, r = F.BEAKLOW
    k.add("beaklow", ell(c, r, rot=(-6, 0, 0), deform=pointed), lower, "head", fuse=False, surface="shell")


def legs(k, F, foot=FOOT, claw=CLAW):
    for s, side in ((1, "R"), (-1, "L")):
        x = F.LEGX * s
        y = F.LEGY
        ank = np.array([x * 1.06, y - 0.01, F.ANKLE])
        k.add(f"leg{side}", tube([(x, y, F.LEGTOP), (x * 1.03, y - 0.005, (F.LEGTOP + F.ANKLE) / 2), ank],
                                 [F.LEGR[0], F.LEGR[0] * 0.95, F.LEGR[1]]), foot, f"leg{side}", fuse=False,
              surface="scales")
        for j, ang in enumerate((-32, 0, 32, 180)):
            rr = math.radians(ang)
            d = np.array([math.sin(rr) * s, -math.cos(rr), 0])
            ln = F.TOE if ang != 180 else F.HEEL
            tip = ank + d * ln + np.array([0, 0, -F.ANKLE + F.TOER * 0.9])
            k.add(f"toe{side}{j}", tube([ank, (ank + tip) / 2 + np.array([0, 0, 0.003]), tip],
                                        [F.TOER * 1.15, F.TOER, F.TOER * 0.9]), foot, f"leg{side}", fuse=False,
                  surface="scales")
            cd = d + np.array([0, 0, -0.45])
            k.add(f"claw{side}{j}", spike(tip, tip + cd * F.TOER * 2.0, F.TOER * 1.4, F.TOER * 1.3), claw,
                  f"leg{side}", fuse=False, surface="shell")


def crest(k, F, bone="head"):
    """Three broad brown feathers laid back from the crown; broad pale ones from over each brow sweeping back under them."""
    H, HR = F.H, F.HR
    top = H + np.array([0, 0.0, HR[2] * 0.9])
    k.add("crest", spike(top + (0, -0.08, -0.02), top + (0, 0.04, 0.075), 0.062, 0.03, side=(1, 0, 0)), BROWN, bone,
          fuse=False)
    for s in (1, -1):
        k.add("crest", spike(top + (0.034 * s, -0.06, -0.015), top + (0.062 * s, 0.06, 0.052), 0.052, 0.028,
                             side=(1, 0, 0)), BROWN, bone, fuse=False)
        k.add("brow", spike(top + (0.05 * s, -0.07, -0.035), top + (0.1 * s, 0.085, -0.005), 0.05, 0.024,
                            side=(0.3 * s, 0, 1)), PALE, bone, fuse=False)
    k.add("crestback", spike(top + (0, -0.01, -0.01), top + (0, 0.12, 0.03), 0.058, 0.024, side=(1, 0, 0)), PALE, bone,
          fuse=False)


FEATHER = [(0.0, 0.32), (0.3, 0.82), (0.62, 1.0), (0.82, 0.88), (0.93, 0.6), (1.0, 0.0)]     # (along, half width)


def tail(k, F, feathers, pitch=-10, thick=0.02, roll=1.5):
    """
    A fan of flat tapering feathers from the rump, nearly level, drooping a little (pitch) and the outer ones banked
    (roll x yaw), so the camera behind, only 10 degrees up, sees their upper faces and not their edges: (yaw in
    degrees, length, width, colour, edge colour or None) each. A feather with an edge is a paler
    plate a little wider and thinner under a narrower one of its colour, so the pale rim shows above and below.
    """
    base = F.TAILBASE
    p = math.radians(pitch)
    for i, (yaw, ln, w, col, edge) in enumerate(feathers):
        y = math.radians(yaw)
        d = np.array([math.sin(y) * math.cos(p), math.cos(y) * math.cos(p), math.sin(p)])
        side = np.array([math.cos(y), -math.sin(y), 0])
        # the outer feathers banked, their outer edges down: a shallow cup whose faces the camera behind sees, where
        # a level fan is edge-on to it
        r = math.radians(roll * yaw)
        side = side * math.cos(r) - np.array([0, 0, 1.0]) * math.sin(r)
        # the middle feather lies on top, the outer ones a little under it
        b = base + np.array([math.sin(y) * 0.02, 0, -0.012 * abs(yaw) / 20])
        def outline(scale):
            half = [(a * ln, h * w / 2 * scale) for a, h in FEATHER]
            return half + [(a, -h) for a, h in half[::-1][1:-1]]
        k.add(f"tail{i}", slab(outline(0.82 if edge else 1.0), b, d, side, thick), col, "tail", fuse=False)
        if edge:
            k.add(f"tailedge{i}", slab(outline(1.0), b - d * 0.004, d, side, thick * 0.6), edge, "tail", fuse=False)


# --- the eyes -----------------------------------------------------------------------------------------------------
WHITEC, INK, PUP = np.array([1.0, 1.0, 1.0]), srgb("#1A1412"), srgb("#141010")
ALMOND = [(-1.0, -0.05), (-0.75, 0.45), (-0.3, 0.82), (0.25, 0.92), (0.75, 0.68), (1.0, 0.2), (0.85, -0.35),
          (0.35, -0.75), (-0.25, -0.8), (-0.7, -0.5)]


def eye_open(u, v):
    """A white almond, a large black pupil toward its back end, a white glint at the pupil's front top."""
    c = np.tile(WHITEC, (len(u), 1))
    c[((u - 0.4) / 0.6) ** 2 + ((v + 0.02) / 0.8) ** 2 < 1] = PUP
    c[((u - 0.16) / 0.2) ** 2 + ((v - 0.32) / 0.22) ** 2 < 1] = WHITEC
    return c


def eyes(k, F, lid=CREAM, open_look=eye_open, outline=ALMOND):
    LID = srgb(lid)
    E = eye_centre(F)
    m = np.array([-1, 1, 1.0])
    def lay(state):
        look = eye_look(state, open_look, LID, INK, line=0.17)
        # two patches laid by hand (a mirrored patch's outline is not mirrored): u toward the back on both
        k.decal(E, F.EYEDIR, F.EYE, look, eps=0.006, outline=polygon(outline), rings=20, segs=56)
        k.decal(E * m, F.EYEDIR * m, F.EYE, lambda u, v: look(-u, v), eps=0.006,
                outline=polygon([(-a, b) for a, b in outline]), rings=20, segs=56)
    return lay


def pidgey(src, out):
    F = frame()
    k = Kit(src, coat="fur")
    body(k, F)
    wings_folded(k, F)
    face(k, F)
    beak(k, F)
    crest(k, F)
    tail(k, F, [(0, 0.19, 0.075, BROWN, EDGE), (20, 0.175, 0.07, BROWN, EDGE), (-20, 0.175, 0.07, BROWN, EDGE)])
    legs(k, F)
    k.build(voxel=F.VOXEL)
    k.eye_states(eyes(k, F))
    k.export(out)


if __name__ == "__main__":
    pidgey(*args())
