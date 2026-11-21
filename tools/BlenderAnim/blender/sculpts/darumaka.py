# Darumaka sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/darumaka.py -- <the export's Darumaka.glb> <out.glb>
#
# The design, from the references: a round red daruma doll, head and body one ball; a tan patch on the face, a band
# up the crown between the brows widening round the eyes and down round the grin; round white eyes with a bold black
# rim and a black pupil a little toward the nose; a white grin showing its upper teeth; for brows two fat yellow
# curls like a ram's horns (curls, not flames, in both pictures); three tall yellow ovals across the red belly; tiny
# red arms ending in orange three-lobed hands; tiny legs under the ball with orange three-toed feet.
# Written to be grown from: Darmanitan keeps its colours, its eye, its grin and its way of placing marks on the ball.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, polygon, outward, args

# The line's colours (Darmanitan copies these)
RED, TAN, YELLOW, HAND, WHITE, INK = "#D8443F", "#F2A673", "#F4C443", "#EE995B", "#FFFFFF", "#1E1618"

C = np.array([0.0, 0.0, 0.204])            # the ball's middle (the body's bone is at 0.16, the head's at 0.24)
R = np.array([0.142, 0.13, 0.156])


def on_ball(c, r, az, z):
    """A point on an upright ellipsoid at an angle az round from the front (toward +X) and a height z, and its normal."""
    c, r = np.array(c, float), np.array(r, float)
    s = math.sqrt(max(1 - ((z - c[2]) / r[2]) ** 2, 0.0))
    p = np.array([c[0] + r[0] * s * math.sin(az), c[1] - r[1] * s * math.cos(az), z])
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def round_about(P, c, r):
    """(the arc round an upright axis through c, z): the coordinates a mark round the ball is painted in."""
    return np.arctan2(P[:, 0] - c[0], -(P[:, 1] - c[1])) * r, P[:, 2]


def in_poly(x, y, pts):
    pts = np.array(pts, float)
    res = np.zeros(len(x), bool)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        res ^= ((y1 > y) != (y2 > y)) & (x < (x2 - x1) * (y - y1) / (y2 - y1 + 1e-12) + x1)
    return res


def poly_mask(x, y, pts, edge=0.003):
    """A polygon painted with a crisp edge: the signed distance to its outline (negative inside), eased."""
    pts = np.array(pts, float)
    d = np.full(len(x), 9.0)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        ex, ey = x2 - x1, y2 - y1
        t = np.clip(((x - x1) * ex + (y - y1) * ey) / (ex * ex + ey * ey + 1e-12), 0, 1)
        d = np.minimum(d, np.hypot(x - x1 - t * ex, y - y1 - t * ey))
    return inside(np.where(in_poly(x, y, pts), -d, d), edge)


def eye_open(u, v, pupil=(-0.12, 0.02, 0.4), rim=0.8):
    """White, a bold black rim, a round black pupil a little toward the nose."""
    pu, pv, pr = pupil
    # eased over about a ring, or the edges step at the patch's rings
    ink = np.maximum(inside(np.hypot(u - pu, v - pv) - pr, 0.022), 1 - inside(np.hypot(u, v) - rim, 0.016))
    return srgb(WHITE)[None] * (1 - ink[:, None]) + srgb(INK)[None] * ink[:, None]


def round_eyes(k, c, r, az, z, half, state, lid=TAN, look=eye_open):
    """Both eyes on a ball, laid as a mirrored pair (u runs outward on each)."""
    p, n = on_ball(c, r, az, z)
    k.decal(p, n, half, eye_look(state, look, srgb(lid), srgb(INK), line=0.16), eps=0.005, rings=20, segs=56,
            mirror=True)


GRIN = [(-1, 0.72), (-0.5, 0.55), (0, 0.5), (0.5, 0.55), (1, 0.72), (0.86, 0.05), (0.55, -0.6), (0, -0.85),
        (-0.55, -0.6), (-0.86, 0.05)]


def grin(k, c, r, z, half, teeth=(-0.5, 0.0, 0.5), lift=0.0):
    """A white grin on the front of a ball, a bold dark rim (a larger patch under it), upper teeth parted by lines."""
    p, n = on_ball(c, r, 0.0, z)
    out = polygon(GRIN)
    k.decal(p, n, (half[0] * 1.16, half[1] * 1.32), INK, eps=0.004 + lift, outline=out, rings=14, segs=72)
    def look(u, v):
        ink = np.zeros(len(u))
        for t in teeth:
            ink = np.maximum(ink, inside(np.abs(u - t) - 0.08, 0.02) * inside(-0.05 - v, 0.06))
        return srgb(WHITE)[None] * (1 - ink[:, None]) + srgb(INK)[None] * ink[:, None]
    k.decal(p, n, half, look, eps=0.0058 + lift, outline=out, rings=16, segs=96)


def ovals(k, c, r, azs, z, half, colour):
    """Tall ovals on the ball's front, each laid along its own normal."""
    for az in azs:
        p, n = on_ball(c, r, az, z)
        k.decal(p, n, half, colour, eps=0.0045, rings=12, segs=48)


def brow(k, name, centre, radius, thick, side, bone="head"):
    """A fat yellow curl like a ram's horn: from a knob by the middle of the crown over the top and down outside."""
    centre = np.array(centre, float)
    ex = np.array([side, 0.0, 0.0])
    ez = unit(0, 0.3, 1)
    pts, rad = [], []
    for i, th in enumerate(np.linspace(1.08 * math.pi, -0.3 * math.pi, 15)):
        t = i / 14
        q = radius * (1 - 0.18 * (1 - t) ** 3)
        pts.append(centre + q * (math.cos(th) * ex + math.sin(th) * ez))
        rad.append(thick * (0.62 + 0.5 * math.sin(math.pi * min(t * 1.15, 1)) ** 0.7) if t < 0.97 else thick * 0.45)
    k.add(name, outward(tube(pts, rad, seg=20, per=4)), YELLOW, bone, fuse=False, surface="fur")
    k.add(name + "knob", ell(pts[0] + ez * 0.002, (thick * 0.95,) * 3, sub=3), YELLOW, bone, fuse=False,
          surface="fur")


def darumaka(src, out):
    k = Kit(src, coat="fur")
    # the ball: its lower part on the body's bone, its upper on the head's
    k.add("ball", ell(C, R), RED, "body")
    k.add("crown", ell(C + (0, -0.004, 0.05), R * (0.95, 0.94, 0.74)), RED, "head")

    # the tan face: a band up the crown between the brows, widening round the eyes and down round the grin
    FACE = [(-0.027, 0.39), (0.027, 0.39), (0.03, 0.322), (0.06, 0.305), (0.082, 0.27), (0.072, 0.232),
            (0.046, 0.208), (0.0, 0.2), (-0.046, 0.208), (-0.072, 0.232), (-0.082, 0.27), (-0.06, 0.305),
            (-0.03, 0.322)]
    k.paint(lambda P: poly_mask(*round_about(P, C, R[0]), FACE) * (P[:, 1] < C[1] - 0.02), TAN)

    # tiny arms, forward and a little down, orange three-lobed hands
    ARM = [(0.1, -0.035, 0.2), (0.128, -0.07, 0.188), (0.142, -0.1, 0.178)]
    k.add("arm", tube(ARM, [0.034, 0.03, 0.027]), RED, "arm{s}", mirror=True)
    HC = np.array([0.152, -0.118, 0.172])
    k.add("hand", ell(HC, (0.027, 0.024, 0.027)), HAND, "arm{s}", mirror=True)
    for i, d in enumerate([(0.75, -0.45, 0.6), (0.95, -0.55, -0.05), (0.55, -0.6, -0.65)]):
        d = unit(*d)
        k.add(f"finger{i}", tube([HC + d * 0.012, HC + d * 0.03], [0.0125, 0.0115]), HAND, "arm{s}", mirror=True)

    # tiny legs under the ball, orange three-toed feet
    k.add("leg", ell((0.056, -0.022, 0.068), (0.042, 0.042, 0.036)), RED, "leg{s}", mirror=True)
    FC = np.array([0.06, -0.045, 0.019])
    k.add("foot", ell(FC, (0.031, 0.034, 0.019)), HAND, "leg{s}", mirror=True)
    for i, a in enumerate((-35, 0, 35)):
        d = unit(math.sin(math.radians(a)), -math.cos(math.radians(a)), 0)
        k.add(f"toe{i}", tube([FC + d * 0.02, FC + d * 0.04], [0.0125, 0.011]), HAND, "leg{s}", mirror=True)

    # the brows: two fat yellow curls on the crown
    for side, nm in ((1, "browR"), (-1, "browL")):
        brow(k, nm, (side * 0.074, -0.05, 0.376), 0.04, 0.028, side)

    # the face and the belly, on the fused ball before it is built
    grin(k, C, R, 0.236, (0.036, 0.019))
    ovals(k, C, R, (-0.43, 0.0, 0.43), 0.158, (0.017, 0.032), YELLOW)

    k.build(voxel=0.005, smooth=3, band=0.006)
    k.eye_states(lambda s: round_eyes(k, C, R, 0.5, 0.278, (0.034, 0.035), s))
    k.export(out)


if __name__ == "__main__":
    darumaka(*args())
