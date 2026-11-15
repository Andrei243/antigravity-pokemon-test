# Ludicolo sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Lotad's and Lombre's scripts (lotad.py, lombre.py): the line's way of sweeping a pad, its claws' hulls.
#   blender --background --factory-startup --python sculpts/ludicolo.py -- <the export's Ludicolo.glb> <out.glb>
#
# The design, from the references: a big pale yellow egg of a body, head and body one mass, like a pineapple. Its top
# under the hat green, the green's lower edge in a zigzag, two small round eyes in it; a huge open mouth across the
# front, a wide orange duck's bill over it and an orange rim round its lower edge, pink inside; two brown zigzag bands
# round the body below, the bottom green with a zigzag top; ragged yellow fringes at the sides. A sombrero of a lily
# pad: a thick green disc tilted down to the viewer's right, its top edge cut into teeth, a brown knob on top with a
# tuft of straw; green arms ending in round mitts with two black slits each, held forward; short green stub legs.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, ring_band, srgb, inside, unit, tri, eye_look, args, sibling

lt = sibling("lotad")
lb = sibling("lombre")

YELLOW, GREEN, BROWN = "#F2DE78", "#8CCB52", "#9A7656"
BILL, MOUTH, KNOB, KNOB_BAND, STRAW = "#F29A4A", "#E87A86", "#8C6648", "#6E4C36", "#E2C77E"
INK, WHITE = lt.INK, lt.WHITE

# --- the frame ------------------------------------------------------------------------------------------------------
BODY_C, BODY_R = np.array([0.0, 0.0, 0.33]), np.array([0.215, 0.18, 0.27])
MOUTH_C, MOUTH_HALF = np.array([0.0, -0.17, 0.385]), (0.078, 0.08)      # the opening's middle and half sizes (x, z)
GREEN_TOP, STRIPES, GREEN_FOOT = 0.49, (0.42, 0.262), 0.165             # the zones' heights on the body
TEETH = 7                                                               # zigzags round the body
HAT_C, HAT_TILT = np.array([0.0, 0.025, 0.588]), (6, 15, 0)
HAT_RADIUS, HAT_THICK, HAT_TOOTH, HAT_TEETH = 0.24, 0.05, 0.05, 12
SHOULDER, WRIST = np.array([0.19, -0.02, 0.42]), np.array([0.235, -0.13, 0.385])
MITT_C, MITT_R = np.array([0.245, -0.165, 0.395]), np.array([0.056, 0.05, 0.066])
HIP, ANKLE = np.array([0.1, 0.0, 0.13]), np.array([0.14, -0.02, 0.05])
FOOT_C, FOOT_R = np.array([0.145, -0.04, 0.032]), np.array([0.058, 0.07, 0.034])
EYE_AT, EYE_HALF = np.array([0.066, -0.15, 0.528]), (0.029, 0.03)
VOXEL = 0.0065


def angle(P):
    """Round the body's upright axis, nought at the front."""
    return np.arctan2(P[:, 0], -P[:, 1])


def zig(P, z0, amp, n=TEETH):
    return z0 + amp * tri(n * angle(P) / (2 * math.pi))


def surface_y(x, z):
    """The body's front surface (y) at x, z."""
    q = 1 - (x / BODY_R[0]) ** 2 - ((z - BODY_C[2]) / BODY_R[2]) ** 2
    return BODY_C[1] - BODY_R[1] * np.sqrt(np.clip(q, 0, 1))


def round_eye(rim=0.15, pupil=0.44):
    W, K = srgb(WHITE), srgb(INK)
    def look(u, v):
        c = np.tile(W, (len(u), 1))
        c[np.hypot(u + 0.05, v + 0.02) < pupil] = K
        c[np.hypot(u, v) > 1 - rim] = K
        return c
    return look


def hat(k):
    """The sombrero: a thick disc, its top edge cut into teeth, rising to a brown knob with a tuft of straw."""
    R = lt.rotation(HAT_TILT)
    Rr, H, T = HAT_RADIUS, HAT_THICK, HAT_TOOTH
    def profile(a):
        sp = T * (tri(HAT_TEETH * a / (2 * math.pi)) + 1) / 2
        return [(0.006, 0.0), (Rr - 0.014, 0.0), (Rr, 0.014), (Rr, H + sp), (Rr - 0.034, H), (0.006, H + 0.02)]
    k.add("hat", lt.pad_sweep(profile, 0.0, 0.0, 192, HAT_C, R, closed=True), GREEN, "head", fuse=False,
          surface="leaf")
    up = R @ np.array([0, 0, 1.0])
    top = HAT_C + up * (H + 0.012)
    k.add("knob", lathe(top, up, [(0, 0.0), (0.058, 0.0), (0.06, 0.026), (0.046, 0.05), (0.022, 0.064), (0, 0.068)]),
          KNOB, "head", fuse=False, surface="leaf")
    k.add("knobband", ring_band(top + up * 0.024, up, 0.062, 0.016, thick=0.008), KNOB_BAND, "head", fuse=False,
          surface="leaf")
    # the straw tuft: a few thick spikes out of the knob's top, fanned out and back
    a_, b_ = lt_axes(up)
    tip = top + up * 0.06
    for i in range(7):
        th = 2 * math.pi * i / 7 + 0.3
        d = unit(*(up * 0.9 + (math.cos(th) * a_ + math.sin(th) * b_) * 0.75))
        d2 = unit(*np.cross(d, up)) if np.linalg.norm(np.cross(d, up)) > 1e-3 else a_
        w = 0.014
        pts = [tip + d2 * w, tip - d2 * w, tip + up * 0.008, tip - up * 0.008, tip + d * (0.052 + 0.01 * (i % 2))]
        k.add(f"straw{i}", hull(pts), STRAW, "head", fuse=False, surface="hair")


def lt_axes(d):
    from sculptkit import axes
    return axes(d)


def ludicolo(src, out):
    k = Kit(src, coat="fur")
    k.add("body", ell(BODY_C, BODY_R), YELLOW, "body")
    k.add("crown", ell((0, 0.0, 0.54), (0.16, 0.14, 0.08)), YELLOW, "head")
    # green arms held forward, round mitts
    k.add("arm", tube([SHOULDER, WRIST], [0.044, 0.04]), GREEN, "arm{s}", mirror=True)
    k.add("mitt", ell(MITT_C, MITT_R, rot=(-10, 0, -15)), GREEN, "arm{s}", mirror=True)
    # short green stub legs out of the bottom, wide feet
    k.add("leg", tube([HIP, ANKLE], [0.06, 0.05]), GREEN, "leg{s}", mirror=True)
    k.add("foot", ell(FOOT_C, FOOT_R), GREEN, "leg{s}", mirror=True)
    # the mouth: a big carved opening, pink inside
    k.add("mouth", ell(MOUTH_C, (MOUTH_HALF[0], 0.07, MOUTH_HALF[1])), MOUTH, "head", carve=True)
    # the wide orange bill over it, and the orange rim round its lower edge
    def bill(V):
        V = V.copy(); t = np.clip(-V[:, 1], 0, 1)
        V[:, 0] *= 1 + 0.12 * t; V[:, 2] += 0.35 * V[:, 0] ** 2
        return V
    k.add("bill", ell((0, -0.21, 0.468), (0.085, 0.068, 0.026), rot=(4, 0, 0), deform=bill), BILL, "head",
          fuse=False, surface="shell")
    ts = np.linspace(-0.3, math.pi + 0.3, 19)
    xs = (MOUTH_HALF[0] - 0.002) * np.cos(ts)
    zs = MOUTH_C[2] - (MOUTH_HALF[1] - 0.002) * np.sin(ts)
    ys = surface_y(xs, zs) + 0.002
    k.add("lowbill", tube(np.stack([xs, ys, zs], 1), [0.022] * len(ts), seg=14, per=4), BILL, "head", fuse=False,
          surface="shell")
    # ragged yellow fringes at the sides, under each band
    for level, z in enumerate((0.385, 0.232)):
        for j, a in enumerate((70, 92, 114)):
            ar = math.radians(a)
            dx, dy = math.sin(ar), -math.cos(ar)
            q = 1 - ((z - BODY_C[2]) / BODY_R[2]) ** 2
            r = 1 / math.sqrt((dx / BODY_R[0]) ** 2 + (dy / BODY_R[1]) ** 2) * math.sqrt(q)
            root = np.array([dx * r, dy * r, z]) * np.array([0.97, 0.97, 1])
            out_ = unit(dx, dy, -1.1)
            side = unit(-dy, dx, 0)
            w = 0.017
            pts = [root + side * w, root - side * w, root + np.array([0, 0, 0.026]), root - unit(dx, dy, 0) * 0.012,
                   root + out_ * 0.05]
            k.add(f"fringe{level}{j}", hull(pts), YELLOW, "body", fuse=False, mirror=True, surface="hair")
    hat(k)

    E = 0.0035
    # the green top with its zigzag lower edge, the two brown bands, the green bottom
    k.paint(lambda P: inside(zig(P, GREEN_TOP, 0.022) - P[:, 2], E), GREEN, on=["body", "crown"])
    for z0 in STRIPES:
        k.paint(lambda P, z0=z0: inside(np.abs(P[:, 2] - zig(P, z0, 0.026)) - 0.011, E), BROWN, on=["body"])
    k.paint(lambda P: inside(P[:, 2] - zig(P, GREEN_FOOT, 0.026), E), GREEN, on=["body"])
    # inside the mouth pink, by distance from its axis
    k.paint(lambda P: inside(((P[:, 0] / MOUTH_HALF[0]) ** 2 + ((P[:, 2] - MOUTH_C[2]) / MOUTH_HALF[1]) ** 2) - 1.02,
                             0.02) * inside(P[:, 1] + 0.06, 0.01), MOUTH)
    # two black slits on each mitt
    for x in (-0.017, 0.017):
        k.decal(MITT_C + np.array([x, -0.048, 0.004]), unit(0.2, -1, 0.1), (0.008, 0.02), INK, eps=0.004,
                rings=8, segs=32, mirror=True)

    k.build(voxel=VOXEL, band=0.005)
    d = EYE_AT - BODY_C
    p, n = lt.on_ell(BODY_C, BODY_R, d)
    look = round_eye()
    k.eye_states(lambda state: lt.lay_eye(k, state, p, n, EYE_HALF, look, GREEN))
    k.export(out)


if __name__ == "__main__":
    ludicolo(*args())
