# Cobalion sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cobalion.py -- <the export's Cobalion.glb> <out.glb>
#
# Built on raikou.py's frame (its trunk, legs and eyes, through sibling("raikou")), as the beasts of Johto were
# corrected to. From the references: a teal stag of steel on long legs, the shoulders and thighs broad masses fused
# over the legs, the lower legs tapering into pale blue-grey boots with a black band at the top and black hooves. A
# thick neck rising forward to a small head: a pale grey-blue face from the eyes down, a black cap across the brow
# cutting the eyes' tops, the back of the head teal with two pointed hood tips. One long tan horn, ridged, rising from
# the cap and sweeping up and back. A great white beard from the chin flowing down the front of the neck onto the
# chest. A tan jagged plate like a bolt standing back from each shoulder; a pale blue-grey frond of blades for a tail;
# dark navy spots on the neck and the haunches. Eyes amber and stern.
#
# The frame is written to be grown from: Terrakion and Virizion import it (sibling("cobalion")) for the boots, hooves
# and the neck; nothing builds unless this file is run itself.
# Bones: body (0, 0.02, 0.5); head (0, -0.15, 0.58); legs (+-0.07, -0.17, 0.45); hind legs (+-0.07, 0.2, 0.45);
# tail (0, 0.23, 0.54).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, slab, ring_band, outward, srgb, inside, tri, unit, polygon, lumpy,
                       args, sibling)

r = sibling("raikou")

TEAL = "#2E9CC4"
PALE = "#BCD6E4"
CAP = "#262C38"
TAN = "#D2A86A"
RIDGE = "#A87C44"
WHITE = "#F0F2F6"
NAVY = "#24486E"
BOOT = "#B4CCDA"
BAND = "#22262E"
HOOF = "#3A3C44"
FROND = "#B8D8E8"
EYE, IRIS, PUPIL = "#F6F4EE", "#E89A2C", "#1A1012"

LEG_X = 0.07
FORE_Y, HIND_Y = -0.17, 0.2
LEG_TOP = 0.45
LEG_R = (0.058, 0.044, 0.034)
ANKLE = 0.031
PAW_R = (0.046, 0.054, 0.032)
BODY_Z = 0.5
H = np.array([0.0, -0.305, 0.83])
HR = np.array([0.074, 0.097, 0.076])
EYE_DIR = unit(0.62, -0.72, 0.24)
EYE_R = 0.027


# --- things the Swords share ------------------------------------------------------------------------------------------
def add_neck(k, color, pts, radii, bones):
    """A thick neck of fused ellipsoids from the chest up to the head (a bent tube folds on its inside)."""
    for i, (c, rr, b) in enumerate(zip(pts, radii, bones)):
        k.add(f"neck{i}", ell(c, rr), color, b)


def add_boots(k, color, top, x, fore_y, hind_y, band=None, band_h=0.018, slope=0.25):
    """
    The lower legs a boot of another colour: crisp paint below a line that runs lower at the front (top at the back),
    kept to the legs by place; band: a stripe of its own colour along the boot's top edge.
    """
    def legs(P):
        near = np.zeros(len(P), bool)
        for y in (fore_y, hind_y):
            near |= (np.abs(P[:, 1] - y) < 0.09) & (np.abs(np.abs(P[:, 0]) - x) < 0.07)
        return near
    def edge(P):
        y0 = np.where(np.abs(P[:, 1] - fore_y) < np.abs(P[:, 1] - hind_y), fore_y, hind_y)
        return top + slope * (P[:, 1] - y0)
    k.paint(lambda P: inside(P[:, 2] - edge(P), 0.003) * legs(P), color)
    if band:
        k.paint(lambda P: inside(P[:, 2] - edge(P), 0.003) * inside(edge(P) - band_h - P[:, 2], 0.003) * legs(P), band)


def add_hooves(k, color, top=0.045):
    """The hooves: the paws' colour all round below a line."""
    k.paint(lambda P: inside(P[:, 2] - top, 0.003), color)


# --- Cobalion's own ---------------------------------------------------------------------------------------------------
def eye_point():
    return r.on_ell(H, HR, EYE_DIR)


def cap_edge(P):
    """The black cap's lower edge: just over the eye's top, rising behind it."""
    e, _ = eye_point()
    return e[2] + EYE_R * 0.45 + 0.35 * (P[:, 1] - e[1])


def add_head(k):
    k.add("head", ell(H, HR), TEAL, "head")
    k.add("snout", ell(H + (0, -0.083, -0.027), (0.049, 0.062, 0.047)), PALE, "head")
    k.add("chin", ell(H + (0, -0.045, -0.056), (0.051, 0.056, 0.034)), PALE, "head")
    # The hood's two points rising behind the head
    k.add("hood", blade([H + (0.035, 0.03, 0.03), H + (0.05, 0.075, 0.075), H + (0.056, 0.1, 0.105)],
                        [0.036, 0.024, 0.008], (1, 0, 0), 0.75), TEAL, "head", mirror=True)
    near = lambda P: np.linalg.norm((P - H) / (0.12, 0.17, 0.13), axis=1) < 1
    # The face pale from the eyes down and forward of the cap
    def face(P):
        q = (P - H)
        return inside(P[:, 2] - cap_edge(P), 0.0025) * inside(q[:, 1] - 0.015, 0.004) * inside(-0.085 - q[:, 2], 0.003) \
            * near(P)
    k.paint(face, PALE)
    # The black cap: a band across the brow, from over the eyes back to the hood
    def cap(P):
        q = (P - H)
        return inside(cap_edge(P) - P[:, 2], 0.0025) * inside(q[:, 1] - 0.03, 0.004) * inside(-0.1 - q[:, 1], 0.004) \
            * near(P)
    k.paint(cap, CAP)


def add_horn(k):
    """One long tan horn rising from the cap and sweeping up and back, darker ridges round it."""
    pts = [H + (0, -0.03, 0.05), H + (0, 0.005, 0.11), H + (0, 0.075, 0.16), H + (0, 0.17, 0.19), H + (0, 0.275, 0.19)]
    rad = [0.032, 0.03, 0.026, 0.019, 0.011]
    k.add("horn", outward(blade(pts, rad, (1, 0, 0), 0.82)), TAN, "head", fuse=False, surface="shell")
    P = np.array(pts)
    for i, t in enumerate((0.28, 0.48, 0.66, 0.82)):
        s = t * (len(P) - 1)
        j = min(int(s), len(P) - 2)
        f = s - j
        c = P[j] + (P[j + 1] - P[j]) * f
        d = P[j + 1] - P[j]
        rr = rad[j] + (rad[j + 1] - rad[j]) * f
        k.add(f"ridge{i}", ring_band(c, d, rr * 0.92 + 0.004, 0.014, thick=0.009), RIDGE, "head", fuse=False,
              surface="shell")


def add_beard(k):
    """The great white beard: from the chin down the front of the neck onto the chest, ragged at its hem."""
    for i, (c, rr, b) in enumerate((((0, -0.34, 0.765), (0.04, 0.04, 0.045), "head"),
                                     ((0, -0.3, 0.7), (0.066, 0.056, 0.075), "head"),
                                     ((0, -0.27, 0.6), (0.088, 0.066, 0.1), "body"),
                                     ((0.02, -0.25, 0.5), (0.074, 0.058, 0.08), "body"))):
        k.add(f"beard{i}", ell(c, rr, deform=lumpy(5, 3, 0.07)), WHITE, b)
    for j, (x, z) in enumerate(((-0.05, 0.47), (-0.015, 0.44), (0.025, 0.43), (0.06, 0.45))):
        at = np.array((x, -0.27, z))
        d = unit(x * 1.5, 0.15, -1)
        k.add(f"beardtuft{j}", blade([at, at + d * 0.04, at + d * 0.075], [0.03, 0.02, 0.004], (0, 1, 0), 0.6), WHITE,
              "body")


def add_bolts(k):
    """The tan jagged plate standing back from each shoulder like a bolt, turned out so the cameras see its face."""
    out = [(0.0, 0.0), (0.095, 0.0), (0.15, 0.05), (0.115, 0.06), (0.205, 0.125), (0.14, 0.125), (0.225, 0.205),
           (0.1, 0.14), (0.035, 0.08)]
    k.add("bolt", slab(out, (0.07, -0.15, 0.555), unit(0.22, 0.96, 0.12), unit(0.32, 0, 0.95), 0.03), TAN, "body",
          fuse=False, mirror=True, surface="shell")


def add_frond(k):
    """The tail: a frond of pale blades fanning up and back from the rump, spread to both sides."""
    base = np.array((0.0, 0.25, 0.56))
    for j, (sx, sz, ln) in enumerate(((-0.45, 0.35, 0.13), (-0.2, 0.6, 0.16), (0.0, 0.85, 0.18), (0.2, 0.6, 0.16),
                                      (0.45, 0.35, 0.13))):
        d = unit(sx, 0.85, sz)
        a = np.cross(d, (0, 0, 1.0)); a /= np.linalg.norm(a)
        n = np.cross(a, d)
        b = base + np.array((sx * 0.03, 0, 0))
        pts = [b + n * 0.03, b - n * 0.03, b + a * 0.018, b - a * 0.018, b + d * ln * 0.45 + n * 0.022,
               b + d * ln * 0.45 - n * 0.016, b + d * ln]
        k.add(f"frond{j}", hull(pts), FROND, "tail", fuse=False, surface="shell")
    k.add("tailroot", ell((0, 0.24, 0.55), (0.04, 0.05, 0.04)), TEAL, "tail")


def add_spots(k):
    for c, n, s in (((0.06, -0.24, 0.66), (1, -0.3, 0.1), 0.016), ((0.075, -0.2, 0.6), (1, -0.2, 0.1), 0.012),
                    ((0.11, 0.21, 0.52), (1, 0.1, 0.2), 0.016), ((0.105, 0.26, 0.46), (1, 0.3, 0.1), 0.012)):
        k.decal(c, n, (s * 0.8, s * 1.25), NAVY, eps=0.003, rings=8, segs=36, mirror=True)


SHUT_LINE = "#1A1C24"


def eye_top(u):
    return 0.3 + 0.45 * u


def eyes(k, state):
    p, n = eye_point()
    r.lay_eyes(k, state, p, n, EYE_R, r.eye_open(EYE, IRIS, PUPIL, iris_r=(0.66, 0.82)), PALE, SHUT_LINE, eye_top)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    r.add_trunk(k, TEAL, z=BODY_Z, w=0.115, deep=0.115, front=-0.15, back=0.21)
    r.add_legs(k, TEAL, LEG_X, FORE_Y, HIND_Y, LEG_TOP, r=LEG_R, shoulder=(0.072, 0.088, 0.15),
               thigh=(0.082, 0.1, 0.16), paw=PAW_R, paw_color=HOOF, bend=0.03, ankle=ANKLE)
    add_boots(k, BOOT, 0.17, LEG_X, FORE_Y, HIND_Y, band=BAND)
    add_hooves(k, HOOF)
    add_neck(k, TEAL, [(0, -0.19, 0.6), (0, -0.24, 0.68), (0, -0.28, 0.75)],
             [(0.08, 0.085, 0.1), (0.066, 0.07, 0.09), (0.058, 0.06, 0.07)], ["body", "head", "head"])
    add_head(k)
    add_beard(k)
    add_spots(k)
    add_bolts(k)
    add_frond(k)
    add_horn(k)
    k.build(voxel=0.007, band=0.004)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)
