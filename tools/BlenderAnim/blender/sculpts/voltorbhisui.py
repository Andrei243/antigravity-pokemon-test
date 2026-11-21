# Voltorb-Hisui sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/voltorbhisui.py -- <the export's Voltorb-Hisui.glb> <out.glb>
# Grown from voltorb.py through `sibling` (its ball, its crisp line and the way a patch is laid on its front). From
# the references: an old wooden ball, orange lacquer over its upper part and bare tan wood with dark grain rings under a
# line a little below its middle; a small dark hole on top, a little behind the crown. The face is two eye sockets
# cut into the lacquer: each a raised angular orange frame (a flat-faced ridge) round a white opening shaped like an
# uneven hexagon whose top edge slopes down toward the middle, the two frames joined over the middle by a raised
# bridge; a small round black pupil low at each opening's inner side (a sly, sideways look). No mouth.
#
# Written to be grown from (Electrode-Hisui is the same wooden ball turned over): `point`, `ridge` and `grain` below.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, hull, srgb, inside, unit, eye_look, polygon, args, sibling

v = sibling("voltorb")

ORANGE, WOOD, GRAIN = "#E2692A", "#DEBD8B", "#C29C6A"
WHITE, INK, HOLE = "#F2F0EA", "#1E1A18", "#2A1C14"


# --- the ball's front: a point at face shares (x, z) of the radius, lifted off the surface --------------------------
def point(c, r, x, z, lift=0.0):
    p, n = v.on_ball(c, r, x, z, lift)
    return p


def ridge(k, name, c, r, inner, outer, closed=True, low=-0.006, high=0.034, edge=0.02, color=ORANGE):
    """A raised flat-faced ridge on the ball: one hull a stretch between the inner and the outer outline (face
    shares), its inner side standing `high` off the surface and its outer side `edge`, both sunk to `low`."""
    m = len(inner)
    for i in range(m if closed else m - 1):
        j = (i + 1) % m
        pts = []
        for (x, z), lifts in ((inner[i], (low, high)), (inner[j], (low, high)),
                              (outer[i], (low, edge)), (outer[j], (low, edge))):
            pts.extend(point(c, r, x, z, lf) for lf in lifts)
        k.add(f"{name}{i}", hull(pts), color, "body", fuse=False, mirror=False, surface="shell")


def grain(k, c, r, under, centre, step=0.055, width=0.0035, color=GRAIN):
    """Dark rings of grain on the bare wood: circles about a point on the ball, where `under(P)` holds."""
    q = np.array(c, float) + r * np.array(centre)

    def rings(P):
        d = np.linalg.norm(P - q, axis=1)
        f = np.abs((d / step) % 1.0 - 0.5) * step          # distance to the nearest ring
        return inside(f - width, 0.0015) * under(P) * (d > step * 0.6)
    k.paint(rings, color)


def hole(k, c, r, d, size=0.012, color=HOLE):
    d = unit(*d)
    k.decal(np.array(c) + d * r, d, (size, size), color, up=(1, 0, 0), eps=0.003, rings=8, segs=32)


# --- Voltorb-Hisui's face ----------------------------------------------------------------------------------------------
C, R = v.C, v.R                   # (0, 0, 0.25), 0.25
LINE = -0.16
EC = (0.33, 0.13)                 # the +X eye's middle (face shares)
# The opening (face shares, +X eye): a low inner corner, the top edge rising outward, a blunt outer side
OPEN = [(0.07, 0.15), (0.16, 0.26), (0.45, 0.33), (0.6, 0.17), (0.54, 0.0), (0.3, -0.05), (0.11, 0.02)]


def grown(pts, f, about=EC):
    return [(about[0] + (x - about[0]) * f, about[1] + (z - about[1]) * f) for x, z in pts]


OUTER = grown(OPEN, 1.45)
HALF = (0.33 * R, 0.24 * R)       # the eye patch's half sizes in metres, about EC
PUPIL = (0.17, 0.06)                # face shares
CW, CO, CI = srgb(WHITE), srgb(ORANGE), srgb(INK)


def to_uv(pts):
    return [((x - EC[0]) * R / HALF[0], (z - EC[1]) * R / HALF[1]) for x, z in pts]


EYE_OUT = to_uv(grown(OPEN, 1.12))


def frames(k):
    for s in (1, -1):
        inner = [(x * s, z) for x, z in OPEN]
        outer = [(x * s, z) for x, z in OUTER]
        if s < 0:
            inner, outer = inner[::-1], outer[::-1]
        ridge(k, "frame" + ("R" if s > 0 else "L"), C, R, inner, outer)
    # The bridge joining the two frames over the middle, rising a little in a blunt point
    inner = [(-0.12, 0.22), (0.0, 0.26), (0.12, 0.22)]
    outer = [(-0.15, 0.33), (0.0, 0.39), (0.15, 0.33)]
    ridge(k, "bridge", C, R, inner, outer, closed=False, high=0.026, edge=0.022)


def eye_open(u, vv):
    return np.tile(CW, (len(u), 1))


def eyes(k, state):
    for s in (1, -1):
        v.patch(k, C, R, EC[0], EC[1], HALF, eye_look(state, eye_open, CO, CI, line=0.16), outline=EYE_OUT, side=s)
        if state in ("Open", "Fierce"):
            v.patch(k, C, R, PUPIL[0], PUPIL[1], (0.022, 0.025), INK, side=s, eps=0.0045, rings=8, segs=32)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    v.ball(k, C, R, ORANGE, WOOD, LINE)
    grain(k, C, R, lambda P: inside(P[:, 2] - (C[2] + LINE * R) + 0.012, 0.003), (-0.35, -0.45, -0.82))
    hole(k, C, R, (0, 0.35, 0.94))
    frames(k)
    k.build(voxel=0.005, smooth=3)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
