# Xerneas sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/xerneas.py -- <the export's Xerneas.glb> <out.glb>
#
# Built on raikou.py's frame (its trunk, legs and eyes, through sibling("raikou")), as Cobalion was. From the
# references (the 3D model is the Neutral Mode, the drawing the Active): a great stag, black behind and blue in front.
# The trunk black, the chest, the shoulders' tops and the long upright neck royal blue, a pale V down the chest and two
# sharp blue points flaring out sideways at the base of the neck. Long black legs that taper to pale pointed hooves,
# the shoulders and thighs broad masses fused over the upper half of each leg, a pale stripe down the front of each
# foreleg and the back of each hind leg. A small blue head, the snout forward and down, a black mask through the eyes,
# blue points of a crest at the back of the head. The antlers its largest mass: two thick beams spreading up and out
# from the crown, each with five tines rising, pale blue (the Neutral Mode; ACTIVE sets a coloured light in each tine,
# as the drawing shows). A dark grey brush of a tail, ragged points fanned up and back.
# Bones: body (0, 0.01, 0.48); head (0, -0.2, 0.75); legs (+-0.06, -0.14, 0.38); hind legs (+-0.06, 0.16, 0.38);
# tail (0, 0.18, 0.49).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, slab, outward, srgb, inside, unit, polygon, lumpy, args, sibling)

r = sibling("raikou")

BLACK = "#2E303C"
BLUE = "#2F5ED0"
PALE = "#D4EAF8"
ANTLER = "#C2E0F4"
ANTLER2 = "#A6CCEA"
TAIL = "#464856"
MASK = "#16181F"
EYE, IRIS, PUPIL = "#EEF6FC", "#2CA6E8", "#0E1420"
LIGHTS = ["#F05050", "#F6A03C", "#FAD850", "#6CC86C", "#58A0F0", "#B46CDC"]

LEG_X = 0.065
FORE_Y, HIND_Y = -0.14, 0.16
LEG_TOP = 0.38
BODY_Z = 0.47
H = np.array([0.0, -0.24, 0.79])
HR = np.array([0.064, 0.077, 0.063])
EYE_DIR = unit(0.72, -0.6, 0.2)
EYE_R = 0.023


def add_body(k):
    r.add_trunk(k, BLACK, z=BODY_Z, w=0.1, deep=0.11, front=-0.12, back=0.17)
    r.add_legs(k, BLACK, LEG_X, FORE_Y, HIND_Y, LEG_TOP, r=(0.046, 0.034, 0.028), shoulder=(0.05, 0.066, 0.11),
               thigh=(0.058, 0.076, 0.12), paw=(0.024, 0.03, 0.03), bend=0.03, ankle=0.022)
    # The hooves: pale and pointed
    k.paint(lambda P: inside(P[:, 2] - 0.05, 0.003) * (P[:, 2] < 0.12), PALE)
    # The long neck rising from the chest, a little forward, to the head
    for i, (c, rr) in enumerate((((0, -0.17, 0.55), (0.072, 0.07, 0.09)), ((0, -0.205, 0.64), (0.054, 0.054, 0.08)),
                                 ((0, -0.225, 0.72), (0.046, 0.048, 0.06)))):
        k.add(f"neck{i}", ell(c, rr), BLUE, "body" if i == 0 else "head")
    # Blue over the chest, the shoulders' tops and the neck, its edge slanting back and up across the shoulder
    k.paint(lambda P: inside(P[:, 1] - (-0.07 + 0.9 * (P[:, 2] - 0.47)), 0.003) * (P[:, 2] > 0.39), BLUE)
    # A little blue on the rump's top where the tail springs from
    k.paint(lambda P: inside(np.linalg.norm((P - (0, 0.21, 0.56)) / (0.06, 0.06, 0.04), axis=1) - 1, 0.04), BLUE)


def add_points(k):
    """The two sharp blue points flaring out sideways at the base of the neck."""
    a = np.array((0.075, -0.17, 0.48))
    tip = np.array((0.2, -0.21, 0.55))
    d = unit(*(tip - a))
    pts = [a + (0, 0.035, 0.03), a + (0, -0.03, 0.03), a + (0, 0.03, -0.035), a + (0, -0.03, -0.03),
           a + d * 0.07 + (0, 0, 0.018), a + d * 0.07 + (0, 0, -0.012), tip]
    k.add("point", hull(pts), BLUE, "body", fuse=False, mirror=True, surface="fur")


def add_marks(k):
    """The pale V down the chest and the stripes down the legs."""
    V = polygon([(-1.0, 1.0), (-0.55, 1.0), (0.0, 0.3), (0.55, 1.0), (1.0, 1.0), (0.12, -1.0), (-0.12, -1.0)])
    k.decal((0, -0.235, 0.5), unit(0, -1, 0.15), (0.05, 0.06), PALE, eps=0.003, outline=V, rings=14, segs=60)

    def near_line(P, pts, w):
        pts = np.array(pts, float)
        d = np.full(len(P), 9.0)
        for a, b in zip(pts, pts[1:]):
            e = b - a
            t = np.clip(((P - a) @ e) / (e @ e), 0, 1)
            d = np.minimum(d, np.linalg.norm(P - (a + t[:, None] * e), axis=1))
        return d - w

    def stripes(P):
        """A strip down the middle of each foreleg's front and each hind leg's back, from the elbow and the hock to
        the hoof: points of the leg's own surface on that side of its axis, within a narrow band of x."""
        band = inside(np.abs(np.abs(P[:, 0]) - LEG_X) - 0.011, 0.003)
        z = P[:, 2]
        low = (z > 0.05) & (z < 0.3)
        T = LEG_TOP
        fz = [0.055, 0.11, T * 0.3, T * 0.6, T]                          # raikou.add_legs' own points, from below
        fy = [FORE_Y - 0.016, FORE_Y - 0.012, FORE_Y - 0.004, FORE_Y + 0.03, FORE_Y]
        hy = [HIND_Y - 0.004, HIND_Y + 0.008, HIND_Y + 0.042, HIND_Y - 0.02, HIND_Y + 0.01]
        fore = (np.abs(P[:, 1] - FORE_Y) < 0.07) & (P[:, 1] < np.interp(z, fz, fy) - 0.01)
        hind = (np.abs(P[:, 1] - HIND_Y) < 0.08) & (P[:, 1] > np.interp(z, fz, hy) + 0.01)
        return band * low * (fore | hind)
    k.paint(stripes, PALE)


def eye_point():
    return r.on_ell(H, HR, EYE_DIR)


def add_head(k):
    k.add("head", ell(H, HR), BLUE, "head")
    # the snout forward and down, narrowing
    k.add("snout", ell(H + (0, -0.08, -0.032), (0.04, 0.064, 0.036), rot=(-18, 0, 0)), BLUE, "head")
    k.add("jaw", ell(H + (0, -0.044, -0.054), (0.044, 0.054, 0.028)), BLUE, "head")
    # The black mask: a long pointed lens round each eye, drawn forward toward the snout and back along the head
    e, n = eye_point()
    for s in (1, -1):
        m = np.array((s, 1, 1.0))
        out_ = polygon([(s * a, b) for a, b in [(-1.0, 0.1), (-0.5, 0.75), (0.45, 0.85), (1.0, 0.25), (0.55, -0.7),
                                                 (-0.45, -0.55)]])
        k.decal(e * m, n * m, (EYE_R * 1.75, EYE_R * 1.25), MASK, eps=0.002, outline=out_, rings=12, segs=60)
    # the crest's blue points at the back of the head, laid back and up
    for j, (dx, dz, L) in enumerate(((0.03, 0.04, 0.09), (0.045, 0.0, 0.08))):
        a = H + np.array((dx, 0.03, dz))
        d = unit(0.35, 0.8, 0.6)
        k.add(f"crest{j}", outward(blade([a, a + d * L * 0.5, a + d * L], [0.024, 0.017, 0.006], (0, -0.6, 0.8), 0.55)),
              BLUE, "head", fuse=False, mirror=True, surface="fur")


# --- the antlers: a beam each side from the crown up and out, five tines rising from it -------------------------------
BEAM = [(0.02, -0.235, 0.83), (0.07, -0.23, 0.88), (0.15, -0.21, 0.94), (0.24, -0.19, 1.0), (0.33, -0.17, 1.05),
        (0.4, -0.16, 1.08)]
BEAM_R = [0.03, 0.027, 0.024, 0.021, 0.018, 0.015]
# (where along the beam, direction, length)
TINES = [(0.1, (0.08, 0.05, 1.0), 0.2), (0.32, (0.05, 0.15, 1.0), 0.17), (0.55, (0.25, 0.1, 1.0), 0.15),
         (0.75, (0.4, 0.15, 1.0), 0.12), (0.35, (0.2, -0.6, 0.6), 0.11)]


def along(pts, t):
    P = np.array(pts, float)
    s = t * (len(P) - 1)
    j = min(int(s), len(P) - 2)
    f = s - j
    return P[j] + (P[j + 1] - P[j]) * f, np.interp(s, range(len(BEAM_R)), BEAM_R)


def add_antlers(k, active=False):
    k.add("beam", outward(blade(BEAM, BEAM_R, (0, 1, 0), 0.8)), ANTLER, "head", fuse=False, mirror=True,
          surface="shell")
    n = 0
    for j, (t, d, L) in enumerate(TINES):
        a, rr = along(BEAM, t)
        d = unit(*d)
        mid = a + d * L * 0.55 + np.array((0.012, 0, 0))
        tip = a + d * L
        k.add(f"tine{j}", outward(blade([a, mid, tip], [rr * 0.85, rr * 0.75, 0.014], (0, 1, 0), 0.8)),
              ANTLER2 if j % 2 else ANTLER, "head", fuse=False, mirror=True, surface="shell")
        if active:
            # a coloured light near the tine's tip: a long gem round it
            g = a + d * L * 0.78
            gd = d * 0.045
            pts = [g - gd, g + gd] + [g + 0.02 * np.array(v) for v in ((1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0))]
            k.add(f"light{j}", hull(pts), LIGHTS[n % len(LIGHTS)], "head", fuse=False, mirror=True, surface="shell",
                  glow=LIGHTS[n % len(LIGHTS)])
            n += 1


def add_tail(k):
    """The brush: ragged dark points fanned up and back from the rump, the fan turned out toward +X."""
    base = np.array((0.0, 0.22, 0.55))
    k.add("tailroot", ell(base + (0, 0.01, 0.0), (0.045, 0.05, 0.045)), BLACK, "tail")
    # the fan's plane: its middle up and back, spread across a line turned 35 degrees from +X toward the back
    mid = unit(0.25, 0.7, 0.67)
    lat = unit(0.82, -0.25, 0.1)
    lat = unit(*(lat - mid * (lat @ mid)))
    face = np.cross(lat, mid)
    for j, (ang, L, w) in enumerate(((-60, 0.17, 0.08), (-30, 0.25, 0.09), (0, 0.28, 0.095), (30, 0.24, 0.09),
                                     (58, 0.17, 0.08), (-12, 0.2, 0.08), (15, 0.21, 0.08))):
        t = math.radians(ang)
        d = unit(*(mid * math.cos(t) + lat * math.sin(t)))
        across = np.cross(face, d)
        tip = base + d * L
        pts = [base + face * 0.022, base - face * 0.022, base + across * w * 0.5, base - across * w * 0.5,
               base + d * L * 0.5 + across * w * 0.55 + face * 0.015,
               base + d * L * 0.5 - across * w * 0.35 - face * 0.015, tip]
        k.add(f"brush{j}", hull(pts), TAIL if j % 2 else BLACK, "tail", fuse=False, surface="fur")


def eye_top(u):
    return 0.42 + 0.3 * u


def eyes(k, state):
    p, n = eye_point()
    r.lay_eyes(k, state, p, n, EYE_R, r.eye_open(EYE, IRIS, PUPIL, iris_r=(0.68, 0.84)), MASK, "#C8D6E4", eye_top)


def xerneas(src, out, active=False):
    k = Kit(src, coat="fur")
    add_body(k)
    add_head(k)
    add_marks(k)
    add_points(k)
    add_antlers(k, active)
    add_tail(k)
    k.build(voxel=0.007, band=0.004)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    xerneas(*args())
