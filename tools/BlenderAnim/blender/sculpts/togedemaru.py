# Togedemaru sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/togedemaru.py -- <the export's Togedemaru.glb> <out.glb>
#
# From the references: a round ball of a hedgehog mouse, head and body one mass as wide as it is tall. Grey over the
# crown, the sides and the back; a white face and belly from the ears down, the white narrowing toward the bottom,
# light lavender-grey under the belly. Two round white ears at the top corners, a short grey stroke inside each.
# Small black oval eyes with a white glint, a tiny dark nose, a thin smile; round yellow cheeks. Tiny white paws held
# on the belly, small white feet. On each side a column of chevrons pointing toward the face: brown in the middle,
# yellow above and below. From the top of the back a grey lightning bolt rises back and out to its right (+X), a
# little peak at its root, a yellow band across it and a yellow tip.
# The skeleton: body (0, 0, 0.13), arms (+-0.07, -0.07, 0.105), ears (+-0.06, 0, 0.225), legs (+-0.045, -0.02, 0.05).
# About 0.35 tall with the tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, unit, eye_look, polygon, args

GREY, WHITE, UNDER, FOOT = "#B2B2BA", "#F4F3F0", "#D4D1DC", "#E8E6EE"
YELLOW, BROWN, CHEEK = "#F2D648", "#6E4C2E", "#F4DA4A"
INK, NOSE, EARLINE = "#1C1C24", "#2A2228", "#A4A4AE"

C = np.array([0.0, 0.004, 0.15])            # the ball's centre: head and body are one
R = np.array([0.134, 0.125, 0.143])
VOXEL = 0.0034


def on_ball(d, lift=0.0):
    d = unit(*d)
    return C + d * (1 / math.sqrt(((d / R) ** 2).sum()) + lift)


def ball_coords(P):
    """Longitude from the front (radians, + toward the sides) and latitude, on the ball's own sphere."""
    q = (P - C) / R
    q /= np.linalg.norm(q, axis=1, keepdims=True)
    return np.arctan2(np.abs(q[:, 0]), -q[:, 1]), np.arcsin(np.clip(q[:, 2], -1, 1)), q


def ball(k):
    def seat(V):
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] < -0.8, -0.8 + (V[:, 2] + 0.8) * 0.6, V[:, 2])      # a flatter seat
        return V
    k.add("ball", ell(C, R, deform=seat), GREY, "body")

    # The white face and belly: within some 52 degrees of the front, up to an arch that rises into the ears
    def face(P):
        lon, lat, q = ball_coords(P)
        top = 0.214 + 5.0 * P[:, 0] ** 2
        lim = 0.98 - 4.5 * np.clip(0.15 - P[:, 2], 0, None) - 2.0 * np.clip(P[:, 2] - 0.19, 0, None)
        return inside(lon - lim, 0.02) * inside(P[:, 2] - top, 0.0015) * inside(q[:, 1] + 0.05, 0.02)
    k.paint(face, WHITE, on=["ball"])

    def under(P):
        lon, lat, q = ball_coords(P)
        return inside(P[:, 2] - 0.05, 0.0015) * inside(lon - 1.05, 0.012)
    k.paint(under, UNDER, on=["ball"])

    # The chevrons down each side: brown in the middle, yellow above and below, each pointing toward the face; a
    # second column behind the first for the back camera
    def chevron(lon0, lat0, length, half):
        def m(P):
            lon, lat, q = ball_coords(P)
            t = (lon - lon0) / length                     # 0 at the point, 1 at the base
            return (inside(np.abs(lat - lat0) - half * np.clip(t, 0, 1), 0.022)
                    * inside(-t, 0.03) * inside(t - 1, 0.03))
        return m
    for lon0, rows in ((1.18, ((0.0, BROWN), (0.5, YELLOW), (-0.5, YELLOW))),
                       (1.98, ((0.25, BROWN), (-0.25, BROWN), (0.72, YELLOW), (-0.72, YELLOW)))):
        for lat0, col in rows:
            half = 0.21 if col == BROWN else 0.15
            k.paint(chevron(lon0, lat0, 0.62, half), col, on=["ball"])


def limbs(k):
    k.add("foot", ell((0.056, -0.036, 0.017), (0.031, 0.037, 0.019)), FOOT, "leg{s}", mirror=True)
    # tiny paws held forward on the belly, well inside its width
    k.add("arm", tube([(0.064, -0.078, 0.104), (0.054, -0.1, 0.09), (0.043, -0.112, 0.082)], [0.022, 0.02, 0.019]),
          WHITE, "arm{s}", mirror=True)
    k.add("paw", ell((0.04, -0.119, 0.078), (0.021, 0.017, 0.019)), WHITE, "arm{s}", mirror=True)


def ears(k):
    """Round white ears at the top corners, leaning out, a short grey stroke inside each (loose, crisp on the grey)."""
    k.add("ear", ell((0.088, -0.036, 0.246), (0.037, 0.022, 0.036), rot=(-8, 24, 0), sub=3), WHITE, "ear{s}",
          fuse=False, mirror=True)
    import mathutils
    m = np.array(mathutils.Euler([math.radians(-8), math.radians(24), 0]).to_matrix())
    loc = [(-0.013, -0.0212, 0.004), (-0.009, -0.0218, -0.009), (0.0, -0.0212, -0.016)]
    pts = [np.array((0.088, -0.036, 0.246)) + m @ np.array(p) for p in loc]
    k.add("earline", tube(pts, [0.0048, 0.0052, 0.0046], seg=12), EARLINE, "ear{s}", fuse=False, mirror=True)


def tail(k):
    """The flat bolt on its back, in a plane run back and some 37 degrees out to +X."""
    TO = np.array([0.008, 0.058, 0.262])
    TD, TU = unit(0.6, 0.8, 0), np.array([0, 0, 1.0])
    bolt = [(-0.03, -0.02), (0.07, -0.012), (0.07, 0.008), (0.2, 0.05), (0.222, 0.088), (0.08, 0.042),
            (0.065, 0.035), (0.045, 0.064), (0.0, 0.032), (-0.035, 0.015)]
    k.add("tail", slab(bolt, TO, TD, TU, 0.015), GREY, "body", fuse=False)
    bot = lambda a: 0.008 + (a - 0.07) * 0.3231 - 0.002
    top = lambda a: 0.042 + (a - 0.08) * 0.3239 + 0.002
    band = [(0.108, bot(0.108)), (0.134, bot(0.134)), (0.134, top(0.134)), (0.108, top(0.108))]
    k.add("tailband", slab(band, TO, TD, TU, 0.019), YELLOW, "body", fuse=False)
    tip = [(0.164, bot(0.164)), (0.202, 0.048), (0.226, 0.091), (0.164, top(0.164))]
    k.add("tailtip", slab(tip, TO, TD, TU, 0.019), YELLOW, "body", fuse=False)


def face(k):
    dc = unit(0.6, -0.76, -0.16)
    k.decal(on_ball(dc), dc, (0.026, 0.025), CHEEK, eps=0.002, rings=12, segs=48, mirror=True)
    dn = unit(0, -1, 0.06)
    k.decal(on_ball(dn), dn, (0.0085, 0.0058), NOSE, eps=0.002, rings=8, segs=32)
    smile = polygon([(u, 0.6 * (u * u - 0.33) + 0.27) for u in np.linspace(-0.9, 0.9, 13)]
                    + [(u, 0.6 * (u * u - 0.33) - 0.27) for u in np.linspace(0.9, -0.9, 13)])
    ds = unit(0, -1, -0.1)
    k.decal(on_ball(ds), ds, (0.027, 0.012), INK, eps=0.002, outline=smile, rings=10, segs=96)


def eyes(k):
    CI, CW = srgb(INK), srgb(WHITE)
    def eye(u, v):
        c = np.tile(CI, (len(u), 1))
        c[((u + 0.02) / 0.34) ** 2 + ((v - 0.38) / 0.3) ** 2 < 1] = (1, 1, 1)
        return c
    de = unit(0.33, -0.93, 0.19)
    E = on_ball(de)
    def lay(state):
        k.decal(E, de, (0.0135, 0.0172), eye_look(state, eye, CW, CI), eps=0.002, rings=20, segs=56, mirror=True)
    k.eye_states(lay)


def togedemaru(src, out):
    k = Kit(src, coat="fur")
    ball(k)
    limbs(k)
    ears(k)
    tail(k)
    face(k)
    k.build(voxel=VOXEL, band=0.004, wband=0.03)
    eyes(k)
    k.export(out)


if __name__ == "__main__":
    togedemaru(*args())
