# Weezing sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/weezing.py -- <the export's Weezing.glb> <out.glb>
# Grown from koffing.py (sibling("koffing")): its bumpy ball, vents and puffs, its lidded eye, its skull mark.
#
# From the references: two of Koffing's balls grown together, lavender, the larger on the left (-X) and a smaller one
# joined to its upper right and a little behind; on the larger a heavy-lidded bored scowl (the lids drooping to the
# outer corners, the pupils at the inner ends) over a wide open frown, dark red within, a big white fang hanging at its
# +X corner and another rising at its -X corner, and the skull and crossbones on its lower front; on the smaller the
# same sleepy eyes, a short line of a mouth with a small fang, and its skull alone, an oval ring, under it; a little
# lumpy knob on a short stalk hanging under the smaller ball; puffs of pale olive gas round both.
# The skeleton: body (-0.06, 0, 0.274), small (0.13, 0.04, 0.354).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, smoothstep, lumpy, eye_look, polygon, sibling, args

kf = sibling("koffing")
PURPLE = "#A08BBE"
kf.PURPLE = PURPLE                      # the line's makes colour by the module's own name
MARK, INK, FANG = kf.MARK, kf.INK, kf.FANG
MOUTH, TONGUE = "#6E1E2C", "#B2424F"
HOLE = "#5A4880"                      # the skulls' holes

BIG, RB = np.array([-0.07, 0.0, 0.29]), 0.2
SMALL, RS = np.array([0.165, 0.06, 0.385]), 0.125
YAW_SMALL = 24                            # the smaller ball looks a little out to its own side


def rotz(d, deg):
    a = math.radians(deg)
    x, y, z = d
    return np.array([x * math.cos(a) - y * math.sin(a), x * math.sin(a) + y * math.cos(a), z])


def eyes_turned(k, state, C, R, d, half, yaw, top=0.26, droop=0.2):
    """kf.eyes_on for a face turned by yaw about the upright axis."""
    for s in (1, -1):
        dd = rotz(np.array(d) * (s, 1, 1), yaw)
        at, n, right, up = kf.frame(C, R, dd)
        lk = eye_look(state, kf.eye_open_lidded(top=top, pupil=(-0.4, -0.2), pr=(0.24, 0.4), droop=droop, band=0.18), srgb(PURPLE), srgb(INK),
                      line=0.16)
        lk = lk if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(lk)
        out = polygon([(s * u, v) for u, v in kf.lid_outline(top, droop)])
        k.decal(at, n, half, lk, eps=0.004, outline=out, rings=20, segs=56)


def frown(k, C, R, d, half):
    """The wide open frown: its top edge nearly straight, sloping a little, its bottom round; dark red within, the
    tongue's red below, a big fang hanging at the +X corner and another rising at the -X corner."""
    at, n, right, up = kf.frame(C, R, d)
    dark, tongue, rim = srgb(MOUTH), srgb(TONGUE), srgb("#3A1620")
    def top(u):
        return 0.5 - 0.12 * u
    def look(u, v):
        c = np.tile(dark, (len(u), 1))
        t = smoothstep(-0.25, -0.45, v + 0.08 * u * u)[:, None]
        c = c * (1 - t) + tongue[None] * t
        c[(u * u + np.minimum(v, 0) ** 2 > 0.88 ** 2) | (v > top(u) - 0.12)] = rim
        return c
    out = [(1.0, top(1.0)), (0.0, top(0.0)), (-1.0, top(-1.0))] + \
          [(math.cos(a), 0.95 * math.sin(a)) for a in np.linspace(math.pi, 2 * math.pi, 15)[1:-1]]
    k.decal(at, n, half, look, eps=0.004, outline=polygon(out), rings=18, segs=64)
    tri_down = polygon([(-1, 1), (1, 1), (0, -1)])
    tri_up = polygon([(-1, -1), (1, -1), (0, 1)])
    p, nn = kf.onto(C, R, at + right * half[0] * 0.66 + up * half[1] * 0.18)
    k.decal(p, nn, (half[0] * 0.13, half[1] * 0.42), FANG, up=up, eps=0.0068, outline=tri_down, rings=6, segs=36)
    p, nn = kf.onto(C, R, at - right * half[0] * 0.62 - up * half[1] * 0.38)
    k.decal(p, nn, (half[0] * 0.12, half[1] * 0.38), FANG, up=up, eps=0.0068, outline=tri_up, rings=6, segs=36)


def small_face(k, C, R, yaw):
    """The smaller ball's line of a mouth with its small fang, and its skull alone, an oval ring."""
    at, n, right, up = kf.frame(C, R, rotz((0, -1, -0.12), yaw))
    k.decal(at, n, (0.034, 0.008), INK, eps=0.004, rings=6, segs=36,
            outline=polygon([(-1, 0.5), (1, 0.9), (1, -0.6), (-1, -0.9)]))
    p, nn = kf.onto(C, R, at - right * 0.018 - up * 0.013)
    k.decal(p, nn, (0.007, 0.011), FANG, up=up, eps=0.006, outline=polygon([(-1, 1), (1, 1), (0, -1)]), rings=5,
            segs=30)
    p, nn = kf.onto(C, R, at - up * 0.064)
    k.decal(p, nn, (0.046, 0.033), MARK, up=up, eps=0.0045, rings=10, segs=48)
    k.decal(p, nn, (0.023, 0.013), HOLE, up=up, eps=0.0075, rings=6, segs=40)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    kf.ball(k, "big", "body", BIG, RB, face=(0, -1, -0.15), clear=0.42, n=20)
    kf.ball(k, "small", "small", SMALL, RS, face=rotz((0, -1, 0), YAW_SMALL), clear=0.5, n=13, seed=3)
    # The knob under the smaller ball on its short stalk
    k.add("stalk", tube([SMALL + (-0.03, -0.03, -0.1), (0.13, -0.05, 0.215)], [0.026, 0.022], seg=14), PURPLE, "small")
    k.add("knob", ell((0.13, -0.055, 0.19), (0.042, 0.04, 0.036), deform=lumpy(4, 3, 0.12)), PURPLE, "small")
    # The gas
    for i, (C, R, d, s, bone) in enumerate([(BIG, RB, (-0.7, 0.55, 0.48), 0.045, "body"),
                                            (BIG, RB, (-0.68, 0.5, -0.55), 0.042, "body"),
                                            (BIG, RB, (-0.1, 0.4, -0.9), 0.04, "body"),
                                            (SMALL, RS, (0.6, 0.35, 0.72), 0.042, "small"),
                                            (SMALL, RS, (0.7, 0.55, -0.45), 0.038, "small"),
                                            (BIG, RB, (-0.2, 0.55, 0.81), 0.036, "body")]):
        kf.vent_puff(k, f"gas{i}", bone, C, R, d, s, seed=i)
    frown(k, BIG, RB, (0.0, -0.95, 0.3), (0.11, 0.055))
    kf.skull_mark(k, BIG, RB, (0.0, -0.933, -0.36), 0.16, hole=HOLE)
    small_face(k, SMALL, RS, YAW_SMALL)
    k.build(voxel=0.005, smooth=3)

    def eyes(state):
        eyes_turned(k, state, BIG, RB, (0.36, -0.7, 0.625), (0.062, 0.036), 0)
        eyes_turned(k, state, SMALL, RS, (0.36, -0.87, 0.34), (0.042, 0.026), YAW_SMALL)

    k.eye_states(eyes)
    k.export(out)
