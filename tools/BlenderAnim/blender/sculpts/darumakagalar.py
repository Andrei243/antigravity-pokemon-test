# Darumaka-Galar sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py),
# grown from darumaka.py (imported as sibling("darumaka")): its ball, its arms and feet, its grin, its way of placing
# marks on the ball.
#   blender --background --factory-startup --python sculpts/darumakagalar.py -- <the export's Darumaka-Galar.glb> <out.glb>
#
# The design, from the references: Darumaka's round doll in white, its lower half shaded a pale blue-grey; the face
# band (up the crown between the brows, round the eyes) a mid blue; round white eyes with a thick dark-blue rim and a
# small black pupil; the same white grin of upper teeth; for brows two faceted ice-blue cubes where Darumaka has its
# curls; on the belly two faceted ice diamonds one over the other (where Darumaka has three ovals); tiny white arms
# with blue three-lobed hands; blue three-toed feet. (No ball of snow on its head: the pictures show none.)
# Written to be grown from: Darmanitan-Galar keeps its colours and its ice.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, eye_look, args, sibling

d = sibling("darumaka")

# The Galarian line's colours (Darmanitan-Galar copies these)
SNOW, SHADE, BLUE, RIM, ICE, WHITE, INK = "#F3F5FA", "#C4D0E2", "#6A8FDA", "#2C4A9C", "#66CFCC", "#FFFFFF", "#1A1C26"

C, R = d.C, d.R


def eye_open(u, v, pupil=(-0.1, 0.0, 0.3), rim=0.74):
    """White, a thick dark-blue rim, a small round black pupil a little toward the nose."""
    pu, pv, pr = pupil
    r = np.hypot(u, v)
    ring = 1 - inside(r - rim, 0.02)
    dot = inside(np.hypot(u - pu, v - pv) - pr, 0.025)
    c = srgb(WHITE)[None] * np.ones((len(u), 1))
    c = c * (1 - ring[:, None]) + srgb(RIM)[None] * ring[:, None]
    return c * (1 - dot[:, None]) + srgb(INK)[None] * dot[:, None]


def round_eyes(k, c, r, az, z, half, state, lid=BLUE, look=eye_open):
    p, n = d.on_ball(c, r, az, z)
    k.decal(p, n, half, eye_look(state, look, srgb(lid), srgb(RIM), line=0.16), eps=0.005, rings=20, segs=56,
            mirror=True)


def ice_cube(k, name, centre, edge, tilt, side, bone="head", colour=ICE):
    """A faceted block of ice: a cube with its corners cut, turned outward and a little forward."""
    h, cut = edge / 2, edge * 0.2
    pts = []
    for sx in (-1, 1):
        for sy in (-1, 1):
            for sz in (-1, 1):
                corner = np.array([sx * h, sy * h, sz * h])
                for ax in range(3):
                    q = corner.copy()
                    q[ax] -= np.sign(q[ax]) * cut
                    pts.append(q)
    pts = np.array(pts)
    a, b = math.radians(tilt[0]), math.radians(side * tilt[1])
    rx = np.array([[1, 0, 0], [0, math.cos(a), -math.sin(a)], [0, math.sin(a), math.cos(a)]])
    ry = np.array([[math.cos(b), 0, math.sin(b)], [0, 1, 0], [-math.sin(b), 0, math.cos(b)]])
    rz = np.array([[math.cos(-b * 0.6), -math.sin(-b * 0.6), 0], [math.sin(-b * 0.6), math.cos(-b * 0.6), 0], [0, 0, 1]])
    pts = pts @ (rz @ ry @ rx).T + np.array(centre, float)
    k.add(name, hull(pts), colour, bone, fuse=False, surface="shell")


def ice_diamond(k, name, p, n, half_w, half_h, rise, bone="body", colour=ICE):
    """A faceted diamond of ice standing out of a surface: a rhombus wider than tall, its middle raised to a point."""
    n = np.array(n, float); n /= np.linalg.norm(n)
    ex = np.cross((0, 0, 1.0), n); ex /= np.linalg.norm(ex)
    ez = np.cross(n, ex)
    base = np.array(p, float) - n * 0.012
    pts = [base + ex * half_w, base - ex * half_w, base + ez * half_h, base - ez * half_h,
           base + n * (0.012 + rise * 0.35) + ex * half_w * 0.9, base + n * (0.012 + rise * 0.35) - ex * half_w * 0.9,
           base + n * (0.012 + rise * 0.35) + ez * half_h * 0.9, base + n * (0.012 + rise * 0.35) - ez * half_h * 0.9,
           base + n * (0.012 + rise)]
    k.add(name, hull(pts), colour, bone, fuse=False, surface="shell")


def darumaka_galar(src, out):
    k = Kit(src, coat="fur")
    k.add("ball", ell(C, R), SNOW, "body")
    k.add("crown", ell(C + (0, -0.004, 0.05), R * (0.95, 0.94, 0.74)), SNOW, "head")
    # the lower half shaded a pale blue-grey, eased up the ball
    k.paint(lambda P: 0.85 * smoothstep(0.17, 0.08, P[:, 2]), SHADE, on=["ball", "crown", "leg"])

    # the blue face: Darumaka's band up the crown between the brows, round the eyes and down to the grin
    FACE = [(-0.027, 0.39), (0.027, 0.39), (0.03, 0.322), (0.06, 0.305), (0.082, 0.27), (0.072, 0.232),
            (0.046, 0.212), (0.0, 0.206), (-0.046, 0.212), (-0.072, 0.232), (-0.082, 0.27), (-0.06, 0.305),
            (-0.03, 0.322)]
    k.paint(lambda P: d.poly_mask(*d.round_about(P, C, R[0]), FACE) * (P[:, 1] < C[1] - 0.02), BLUE)

    # tiny white arms, forward and a little down, blue three-lobed hands
    ARM = [(0.1, -0.035, 0.2), (0.128, -0.07, 0.188), (0.142, -0.1, 0.178)]
    k.add("arm", tube(ARM, [0.034, 0.03, 0.027]), SNOW, "arm{s}", mirror=True)
    HC = np.array([0.152, -0.118, 0.172])
    k.add("hand", ell(HC, (0.027, 0.024, 0.027)), BLUE, "arm{s}", mirror=True)
    for i, dd in enumerate([(0.75, -0.45, 0.6), (0.95, -0.55, -0.05), (0.55, -0.6, -0.65)]):
        dd = unit(*dd)
        k.add(f"finger{i}", tube([HC + dd * 0.012, HC + dd * 0.03], [0.0125, 0.0115]), BLUE, "arm{s}", mirror=True)

    # tiny legs under the ball, blue three-toed feet
    k.add("leg", ell((0.056, -0.022, 0.068), (0.042, 0.042, 0.036)), SHADE, "leg{s}", mirror=True)
    FC = np.array([0.06, -0.045, 0.019])
    k.add("foot", ell(FC, (0.031, 0.034, 0.019)), BLUE, "leg{s}", mirror=True)
    for i, a in enumerate((-35, 0, 35)):
        dd = unit(math.sin(math.radians(a)), -math.cos(math.radians(a)), 0)
        k.add(f"toe{i}", tube([FC + dd * 0.02, FC + dd * 0.04], [0.0125, 0.011]), BLUE, "leg{s}", mirror=True)

    # the brows: two blocks of ice on the crown where Darumaka's curls are
    for side, nm in ((1, "browR"), (-1, "browL")):
        ice_cube(k, nm, (side * 0.078, -0.052, 0.382), 0.07, (-12, 22), side)

    # the grin, and two diamonds of ice down the belly
    d.grin(k, C, R, 0.238, (0.036, 0.019))
    for i, z in enumerate((0.19, 0.136)):
        p, n = d.on_ball(C, R, 0.0, z)
        ice_diamond(k, f"diamond{i}", p, n, 0.04, 0.022, 0.016)

    k.build(voxel=0.005, smooth=3, band=0.006)
    k.eye_states(lambda s: round_eyes(k, C, R, 0.5, 0.282, (0.036, 0.037), s))
    k.export(out)


if __name__ == "__main__":
    darumaka_galar(*args())
