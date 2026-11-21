# Garganacl sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# nacli.py (its colours, salt blocks, banded rock, sparkle and framed eyes):
#   blender --background --factory-startup --python sculpts/garganacl.py -- <the export's Garganacl.glb> <out.glb>
#
# The design, from the references: a golem whose torso is a pyramid of white salt blocks, its largest mass: three
# tiers of blocks each side, widest at the bottom, and a tall block standing up in the middle as a spire. Between the
# two stacks, a little forward, its head: a narrow brown rock with a peak at its top, dark brown below, with two small
# yellow eyes in dark frames and white diamonds. Under the tiers a grey-white block for a pelvis, and from it two legs;
# from the bottom tier's outer ends two arms. Legs and arms are thick columns alike: knobbly dark brown rings at the
# top, salmon pink with pale stripes in the middle, white below with a ragged edge, scattered with white diamonds. The
# arms end in fists of salt with grey diamond knuckles.
# Where the pictures overruled the brief: the head is not like a small fort but a narrow peaked rock set into the
# blocks; the "heavy forearms" are columns like the legs. The art's one raised fist is a pose: here both arms hang
# forward and down with fists before the thighs.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, eye_look, args, hull, polygon, inside, boxy, sibling

n = sibling("nacli")

SALMON, STRIPE, BAND = "#CF9C86", "#F1DDD2", "#8A6556"
HEAD_TOP, HEAD_LOW = "#B4826A", "#6A4C40"
KNUCKLE = "#A8A6A4"
YEL, YEL_DK = "#FFD43A", "#E8901A"

# --- the frame ------------------------------------------------------------------------------------------------------
TIERS = [  # (x, y, z) centre on +X, half sizes, a turn (deg)
    ((0.205, 0.0, 0.552), (0.142, 0.1, 0.066), (0, -2, -3)),
    ((0.168, 0.012, 0.682), (0.112, 0.09, 0.064), (0, 3, 4)),
    ((0.128, 0.022, 0.802), (0.078, 0.08, 0.056), (0, -3, -5)),
]
HEAD_C, HEAD_R = (0.0, -0.078, 0.626), (0.086, 0.074, 0.148)
LEG_X, LEG_R = 0.084, 0.07
ARM = [(0.29, 0.0, 0.53), (0.305, -0.02, 0.34), (0.29, -0.1, 0.2)]   # shoulder, elbow, wrist (+X)
FIST_C = (0.29, -0.135, 0.15)


def column_bands(z_top, z_low):
    """The paint of a column from its top to its foot: dark brown rings above, salmon with pale stripes, white below."""
    span = z_top - z_low
    def at(f):
        return z_low + f * span
    def wav(P, z0, a=0.012):
        th = P[:, 0] * 70 + P[:, 1] * 55
        return z0 + a * np.sin(th) + 0.5 * a * np.sin(2.3 * th + 1.1)
    return [
        (lambda P: inside(P[:, 2] - wav(P, at(1.06)), 0.004), SALMON),
        (lambda P: inside(np.abs(P[:, 2] - wav(P, at(0.8), 0.008)) - 0.009, 0.003), STRIPE),
        (lambda P: inside(np.abs(P[:, 2] - wav(P, at(0.64), 0.008)) - 0.008, 0.003), STRIPE),
        (lambda P: inside(P[:, 2] - wav(P, at(0.42), 0.016), 0.004), n.SALT),
    ]


def leg(k):
    # the knob at the hip: two dark tyres, then the column flaring to its foot
    k.add("leg_knob", ell((LEG_X, 0.0, 0.43), (0.082, 0.078, 0.05), deform=boxy(2.6)), n.DARK, "leg{s}", mirror=True)
    k.add("leg_ring", ell((LEG_X, 0.0, 0.37), (0.08, 0.077, 0.036), deform=boxy(2.6)), n.DARK, "leg{s}", mirror=True)
    def flare(V):
        V = boxy(3.2)(V)
        t = (V[:, 2] + 1) / 2
        V = V.copy()
        V[:, 0] *= 1.12 - 0.16 * t
        V[:, 1] *= 1.12 - 0.16 * t
        V[:, 2] = np.maximum(V[:, 2], -0.97)
        return V
    k.add("leg_", ell((LEG_X, 0.0, 0.18), (LEG_R, LEG_R * 0.95, 0.19), deform=flare, sub=5), n.DARK, "leg{s}",
          mirror=True)


def arm(k):
    sh, el, wr = [np.array(p) for p in ARM]
    k.add("arm_knob", ell(sh, (0.078, 0.075, 0.055), deform=boxy(2.6)), n.DARK, "arm{s}", mirror=True)
    k.add("arm_ring", ell(sh + (el - sh) * 0.32, (0.074, 0.072, 0.034), deform=boxy(2.6)), n.DARK, "arm{s}",
          mirror=True)
    k.add("arm_", tube([sh, el, wr], [0.064, 0.066, 0.068], seg=24), n.DARK, "arm{s}", mirror=True)
    # the fist: a block of salt, its knuckles grey diamonds on its front
    fc = np.array(FIST_C)
    k.add("fist", n.box(fc, (0.074, 0.07, 0.07), rot=(18, 0, -6), cham=0.18), n.SALT, "arm{s}", fuse=False,
          mirror=True, surface="shell")
    fy = fc[1] - 0.07 * math.cos(math.radians(18)) - 0.012
    for i, (dx, dz) in enumerate(((-0.03, -0.012), (0.012, -0.018), (-0.014, -0.05), (0.028, -0.054))):
        c = np.array([fc[0] + dx, fy + 0.012 * (dz + 0.03) / 0.03 * 0.0, fc[2] + dz])
        pts = [c + (0.019, 0, 0), c - (0.019, 0, 0), c + (0, 0, 0.014), c - (0, 0, 0.014),
               c + (0, 0.014, 0.0), c + (0.01, -0.006, 0), c - (0.01, 0.006, 0), c + (0, -0.006, 0.006),
               c + (0, -0.006, -0.006)]
        k.add(f"knuckle{i}", hull(pts), KNUCKLE, "arm{s}", fuse=False, mirror=True, surface="shell")


def torso(k):
    for i, (c, h, rot) in enumerate(TIERS):
        k.add(f"tier{i}", n.box(c, h, rot=rot, cham=0.12), n.SALT, "body", fuse=False, mirror=True, surface="shell")
    # the spire, a tall block standing up out of the middle
    k.add("spire", n.box((0.012, 0.03, 0.84), (0.046, 0.046, 0.165), rot=(-3, 4, 6), cham=0.12), n.SALT, "body",
          fuse=False, surface="shell")
    # the grey block behind the head and the pelvis under the tiers
    k.add("back", n.box((0.0, 0.05, 0.66), (0.066, 0.06, 0.13)), n.SALT_GREY, "body", fuse=False, surface="shell")
    k.add("pelvis", n.box((0.0, 0.005, 0.47), (0.1, 0.075, 0.045)), n.SALT_GREY, "body", fuse=False, surface="shell")


def head(k):
    k.add("head", ell(HEAD_C, HEAD_R, rot=(4, 0, 6), deform=n.rock_shape(21, amount=0.5, flat=-0.85, n=16), sub=5),
          HEAD_LOW, "head")
    k.add("peak", ell((0.024, -0.07, 0.77), (0.045, 0.046, 0.05), rot=(0, 18, 0),
                      deform=n.rock_shape(5, 0.5, -0.7, 10)), HEAD_TOP, "head")
    k.paint(lambda P: inside(0.615 - P[:, 2] + 0.01 * np.sin(P[:, 0] * 80), 0.004)
            * inside(np.hypot(P[:, 0], P[:, 1] + 0.07) - 0.13, 0.004), HEAD_TOP, on=["head", "peak"])
    # the dark frame the eyes sit in
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.672) - 0.03, 0.003) * inside(P[:, 1] + 0.1, 0.004)
            * inside(np.abs(P[:, 0]) - 0.07, 0.004), n.DEEP, on=["head"])


def sparkles(k):
    front = (0, -1, 0.1)
    for s in (1, -1):
        for dx, z in ((0.018, 0.4), (-0.03, 0.33)):
            n.sparkle(k, (s * (LEG_X + dx), -0.075, z), front, (0.011, 0.016))
        sh, el = np.array(ARM[0]), np.array(ARM[1])
        p = sh + (el - sh) * 0.6
        n.sparkle(k, (s * p[0], p[1] - 0.062, p[2]), front, (0.01, 0.015))
    for x, z in ((-0.03, 0.57), (0.035, 0.545)):
        n.sparkle(k, (x, HEAD_C[1] - HEAD_R[1] + 0.008, z), front, (0.011, 0.016))


def garganacl(src, dest):
    k = Kit(src, coat="shell")
    torso(k)
    head(k)
    leg(k)
    arm(k)
    for mask, col in column_bands(0.33, 0.0):
        k.paint(mask, col, on=["leg_"])
    for mask, col in column_bands(0.33, 0.06):
        k.paint(mask, col, on=["arm_"])
    # the brown band between the tyres
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.4) - 0.012, 0.003), BAND, on=["leg_knob", "leg_ring"])
    sparkles(k)
    k.build(voxel=0.0075, smooth=3, band=0.006)

    yel, dk, ink = srgb(YEL), srgb(YEL_DK), n.CF
    def eye_open(u, v):
        c = np.tile(yel, (len(u), 1))
        c[(v < -0.1) & (u < 0.2)] = dk
        c[(np.abs(u) > 0.72) | (np.abs(v) > 0.74)] = ink
        return c

    def lay(state):
        look = eye_look(state, eye_open, srgb(n.DEEP), srgb("#1A1210"), line=0.2)
        k.decal((0.031, HEAD_C[1] - HEAD_R[1], 0.672), (0, -1, 0), (0.021, 0.019), look, eps=0.0018,
                outline=n.square(), rings=20, segs=56, mirror=True)
    k.eye_states(lay)
    k.export(dest)


if __name__ == "__main__":
    garganacl(*args())
