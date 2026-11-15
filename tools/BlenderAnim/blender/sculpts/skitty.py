# Skitty sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/skitty.py -- <the export's Skitty.glb> <out.glb>
# From the references: a pink kitten that is mostly head: a big round head, pink over the crown and down its sides
# like a hood, a cream face, the pink coming down the middle of the brow in a rounded tab; the eyes always shut, two
# arcs bowed up in a smile; a tiny nose and a little cat's mouth; huge pointed ears from the top corners, pink, purple
# inside, each tip split into a few short spikes; a small body on four stubby cream legs, pink over the back and flanks
# and cream underneath; a thin cream tail running back and out to a big pink pod, wrinkled at its foot, with three dark
# stalks on its top ending in cream beads. (The skeleton stands it on four legs where the art sits it.)
# Reference ratios (3D picture, ear tips to feet): head chin to crown / height 0.48; head width / height 0.56; head
# width / body width 1.4; the ears' rise over the crown / height 0.2; the pod's length / height 0.44; legs under the
# body / height 0.08.
# Written to be grown from (delcatty.py keeps this frame: the head's paint, the ears' way, the legs, the face).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, polygon, lumpy, args

# --- colours ----------------------------------------------------------------------------------------------------------
PINK = "#F2A0BA"
CREAM = "#FAECB6"
LINING = "#7A55B8"
INK = "#3A2A30"
STALK = "#3A3036"

# --- the frame (the skeleton's joints: body (0, 0, 0.14), head (0, -0.06, 0.18), ears (+-0.06, -0.08, 0.29), forelegs
#     (+-0.05, -0.05, 0.1), hind legs (+-0.05, 0.07, 0.1), tail (0, 0.1, 0.14)) -----------------------------------------
H = np.array([0.0, -0.085, 0.238])           # the head's middle, forward of its joint
HR = np.array([0.128, 0.104, 0.106])
BODY_Z = 0.105
LEG_X = 0.05
FORE_Y, HIND_Y = -0.045, 0.075
EAR = [(0.066, -0.075, 0.305), (0.098, -0.072, 0.355), (0.128, -0.068, 0.4), (0.152, -0.064, 0.438)]
EAR_R = [0.056, 0.05, 0.032, 0.006]


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


def on_head(x, z, lift=0.0, h=None, hr=None):
    """The head's front point at (x, z), lifted along its normal."""
    h = H if h is None else h
    hr = HR if hr is None else hr
    zn = (z - h[2]) / hr[2]
    q = max(0.0, 1 - zn * zn - (x / hr[0]) ** 2)
    p = np.array([x, h[1] - hr[1] * math.sqrt(q), z])
    n = (p - h) / hr ** 2; n /= np.linalg.norm(n)
    return p + n * lift, n


# --- the body and legs ---------------------------------------------------------------------------------------------
def add_body(k):
    """A small round body, the belly low between the legs, pink over the back and flanks, cream below."""
    k.add("chest", ell((0, -0.03, BODY_Z), (0.078, 0.075, 0.072)), PINK, "body")
    k.add("middle", ell((0, 0.035, BODY_Z - 0.004), (0.075, 0.085, 0.068)), PINK, "body")
    k.add("rump", ell((0, 0.075, BODY_Z), (0.078, 0.06, 0.07)), PINK, "body")


def add_legs(k, color=CREAM):
    """Stubby legs: a haunch fused into the body over each, a stub and a round paw."""
    for name, y, bone in (("leg", FORE_Y, "leg{s}"), ("hind", HIND_Y, "hind{s}")):
        k.add(name + "top", ell((LEG_X - 0.004, y, 0.075), (0.042, 0.046, 0.045)), PINK, bone, mirror=True)
        k.add(name, tube([(LEG_X, y, 0.07), (LEG_X + 0.003, y - 0.004, 0.025)], [0.034, 0.031]), color, bone,
              mirror=True)
        k.add(name + "paw", ell((LEG_X + 0.004, y - 0.014, 0.017), (0.033, 0.04, 0.019)), color, bone, mirror=True)


def underside(P):
    """Cream below a line that rises toward the chest; the legs cream too."""
    y, z = P[:, 1], P[:, 2]
    return inside(z - (0.068 - 0.28 * np.clip(y, -0.2, 0.05)), 0.003) * (z < 0.2)


def paint_body(k):
    k.paint(underside, CREAM)


# --- the head --------------------------------------------------------------------------------------------------------
def add_head(k):
    def round_cheeks(V):
        V = V.copy()
        V[:, 0] *= 1 + 0.05 * np.clip(-V[:, 2], 0, 1)
        return V
    k.add("head", ell(H, HR, deform=round_cheeks), PINK, "head")
    k.add("muzzle", ell(H + (0, -0.072, -0.045), (0.05, 0.04, 0.034)), PINK, "head")
    k.add("neck", ell((0, -0.055, 0.15), (0.06, 0.05, 0.05)), PINK, "head")


R_FACE = 0.124
# the cream face in (angle round the head x radius, height): an oval from the chin up past the eyes, the pink tab
# coming down into its top in the middle
FACE = mirrored([(0.0, 0.262), (0.012, 0.264), (0.021, 0.272), (0.026, 0.287), (0.034, 0.302), (0.05, 0.308),
                 (0.066, 0.304), (0.085, 0.285),
                 (0.104, 0.255), (0.11, 0.215), (0.1, 0.17), (0.075, 0.138), (0.04, 0.124), (0.0, 0.12)])


def face_mask(P, h=None, face=FACE, r=R_FACE):
    h = H if h is None else h
    a = np.arctan2(P[:, 0], -(P[:, 1] - h[1])) * r
    return poly_mask(a, P[:, 2], face, 0.0025) * (P[:, 1] < h[1] + 0.01)


def paint_face(k):
    k.paint(face_mask, CREAM, on=["head", "muzzle", "neck"])


def add_ears(k, ear=EAR, radii=EAR_R, color=PINK, lining=LINING, tips=True):
    """Huge pointed ears from the top corners of the head, out and up, flat front to back, lined purple."""
    k.add("ear", blade(ear, radii, (0, 1, 0), 0.34), color, "ear{s}", mirror=True)
    d = unit(*(np.array(ear[-1]) - np.array(ear[0])))
    across = np.cross(d, (0, 1.0, 0)); across /= np.linalg.norm(across)

    def mask(P):
        t, off = along(P, ear)
        half = 0.031 * np.sin(np.pi * np.clip((t + 0.05) / 0.95, 0, 1)) ** 0.7
        w = off @ across
        return (inside(np.abs(w) - half, 0.0025) * inside(off[:, 1] + 0.002, 0.002) * inside(t - 0.8, 0.008)
                * inside(0.08 - t, 0.006) * (P[:, 2] > ear[0][2] - 0.01))
    k.paint(mask, lining)
    if tips:
        tip = np.array(ear[-1])
        for i, (dx, dz, ln) in enumerate(((-0.3, 1.0, 0.036), (0.6, 0.8, 0.032), (1.0, 0.15, 0.028))):
            base = tip - d * 0.03
            dd = unit(*(d * 0.4 + np.array((dx, 0.0, dz)) * 0.6))
            k.add(f"eartip{i}", blade([base, base + dd * ln * 0.5, base + dd * ln], [0.014, 0.01, 0.003],
                                      (0, 1, 0), 0.6), color, "ear{s}", mirror=True)


# --- the tail ------------------------------------------------------------------------------------------------------
TAIL = [(0.0, 0.11, 0.115), (0.03, 0.155, 0.1), (0.085, 0.2, 0.105), (0.13, 0.225, 0.13), (0.15, 0.232, 0.165)]
POD = np.array((0.158, 0.236, 0.255))
POD_R = (0.046, 0.044, 0.088)


def add_tail(k):
    k.add("tail", tube(TAIL, [0.017, 0.016, 0.016, 0.016, 0.017]), CREAM, "tail")
    # the pod, leaning a little out, its foot gathered into a wrinkled knob
    k.add("pod", ell(POD, POD_R, rot=(0, 10, 0)), PINK, "tail")
    k.add("podfoot", ell(POD + (-0.012, 0, -0.088), (0.026, 0.026, 0.022), deform=lumpy(5, 2, 0.25)), PINK, "tail")
    # three dark stalks from its top, beaded with cream
    top = POD + np.array((0.012, 0, 0.082))
    for i, dx in enumerate((-0.04, 0.0, 0.04)):
        end = top + np.array((dx, 0.0, 0.06 - 0.008 * abs(dx) / 0.04))
        k.add(f"stalk{i}", tube([top + (dx * 0.25, 0, -0.01), end], [0.015, 0.013]), STALK, "tail", fuse=False,
              surface="hair")
        k.add(f"bead{i}", ell(end + (0, 0, 0.012), (0.021, 0.021, 0.021)), CREAM, "tail", fuse=False)


# --- the face's marks ------------------------------------------------------------------------------------------------
CC, CP, CI = srgb(CREAM), srgb(PINK), srgb(INK)


def cat_mouth(u, v):
    """A little cat's mouth: a line dipping in the middle and turning up at the corners."""
    c = np.tile(CC, (len(u), 1))
    c[(np.abs(v - (-0.2 + 1.4 * (np.abs(u) - 0.45) ** 2)) < 0.26) & (np.abs(u) < 0.88)] = CI
    c[(np.abs(u) < 0.1) & (v > -0.1)] = CI
    return c


def add_nose_mouth(k, h=None, nose_z=-0.035, mouth_z=-0.056, half=(0.022, 0.014)):
    h = H if h is None else h
    nose = h + np.array((0, -0.11, nose_z))
    k.decal(nose, unit(0, -1, 0.15), (0.008, 0.006), INK, eps=0.003,
            outline=polygon([(-1, 0.7), (1, 0.7), (0, -1)]), rings=6, segs=24)
    k.decal(h + np.array((0, -0.11, mouth_z)), unit(0, -1, -0.15), half, cat_mouth, eps=0.003, rings=16, segs=72)


def arc(u, v):
    """The shut eye: an arc bowed up in a smile."""
    c = np.tile(CC, (len(u), 1))
    c[(np.abs(u) < 0.88) & (np.abs(v - (0.5 * (1 - u * u) - 0.25)) < 0.22)] = CI
    return c


def shut_eyes(state):
    """eye_look's states on the arc; Fierce is the arc straightened and slanted down toward the nose."""
    if state != "Fierce":
        return eye_look(state, arc, CC, CI, line=0.2)
    def fierce(u, v):
        c = np.tile(CC, (len(u), 1))
        c[(np.abs(u) < 0.88) & (np.abs(v - (0.45 * u - 0.05)) < 0.22)] = CI
        return c
    return fierce


EYE_X, EYE_Z, EYE_HALF = 0.05, 0.232, (0.03, 0.021)


def lay_eyes(k, state):
    p, n = on_head(EYE_X, EYE_Z)
    k.decal(p, n, EYE_HALF, shut_eyes(state), eps=0.004, rings=20, segs=56, mirror=True)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_body(k)
    add_legs(k)
    paint_body(k)
    add_head(k)
    paint_face(k)
    add_ears(k)
    add_tail(k)
    add_nose_mouth(k)
    k.build(voxel=0.005, band=0.006)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)
