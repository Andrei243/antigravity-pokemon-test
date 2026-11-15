# Mareep sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mareep.py -- <the export's Mareep.glb> <out.glb>
#
# The design, from the references: a sheep that is all fleece: a cream cloud of round puffs hanging low round its
# body, a round cap of the same fleece on its head with a spiral curl on top; a blue face standing forward and down
# out of the fleece, long in the muzzle, round black eyes with a white glint, a small mouth line under the snout's
# end; ears like short horns pointing out to the sides, banded black at the root, yellow, black at the point; four
# short thick blue legs showing under the fleece; a tail rising out of the back, banded black and yellow, ending in a
# glowing orange ball.
#
# Written to be grown from (flaaffy.py, ampharos.py import it with sibling("mareep")): the colours, the wool, the
# banded ears, the banded tail with its ball and the eyes are functions here, and the build is under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from sculptkit import Kit, ell, tube, lathe, ring_band, srgb, inside, unit, eye_look, args, axes

# --- the line's colours --------------------------------------------------------------------------------------------------
WOOL, WOOL_SHADE = "#F1E4B6", "#E2D29C"
BLUE = "#5A8ED6"
BLACK, YELLOW = "#2B2A33", "#F3C232"
ORANGE = "#F7962C"
INK, GLINT = "#16141C", "#FFFFFF"
MOUTH = "#203A6A"


# --- small helpers -------------------------------------------------------------------------------------------------------
def hit(shape, o, d):
    """Where a ray from o along d first meets a part's own mesh."""
    V, F = shape
    h = BVHTree.FromPolygons(V.tolist(), F).ray_cast(Vector(o), Vector(unit(*d)))
    return np.array(h[0])


def wool(k, name, c, r, bone, col=WOOL, n=40, puff=0.06, sink=0.022, skip=None, seed=0):
    """
    A fused cloud of fleece: an ellipsoid with round puffs over its surface (a golden spiral of them, jittered), the
    puffs standing out by puff - sink. skip(d) leaves out the directions it picks (where a face stands).
    """
    c, r = np.array(c, float), np.array(r, float)
    k.add(name, ell(c, r), col, bone)
    rs = np.random.RandomState(seed)
    golden = math.pi * (3 - math.sqrt(5))
    j = 0
    for i in range(n):
        z = 1 - 2 * (i + 0.5) / n
        rr = math.sqrt(max(1 - z * z, 0))
        th = golden * i + rs.uniform(-0.25, 0.25)
        d = np.array([rr * math.cos(th), rr * math.sin(th), z])
        if skip is not None and skip(d):
            continue
        p = c + d * r                                       # on the ellipsoid
        nrm = unit(*(d / r))
        size = puff * rs.uniform(0.85, 1.15)
        k.add(f"{name}puff{j}", ell(p + nrm * (size - sink - puff * 0.5), (size, size, size * 0.92), sub=3), col,
              bone)
        j += 1


def ear_cone(k, name, base, d, L, R, cols, bounds, bone, mirror=True):
    """
    A short horn of an ear from base along d: a rounded root, a cone to a point. cols[0] its own colour, then bands
    (t0, t1, colour) over it, each a loose lathe a hair proud of the cone (t from 0 at the root to 1 at the point).
    """
    def r(t):
        t = np.clip(t, 0, 1)
        return R * (1 - t) ** 0.85 * (0.75 + 0.25 * np.sqrt(np.clip(t * 6, 0, 1)))
    ts = np.linspace(0, 1, 18)
    prof = [(0.0, -0.012)] + [(float(r(t)), float(t * L)) for t in ts[:-1]] + [(0.0, L)]
    k.add(name, lathe(base, d, prof, seg=32), cols, bone, fuse=False, mirror=mirror, surface="shell")
    for i, (t0, t1, col) in enumerate(bounds):
        e = 0.0028
        tt = np.linspace(t0, t1, 10)
        prof = [(0.0, t0 * L - (0.012 if t0 <= 0 else 0.0))]
        prof += [(float(r(t) + e), float(t * L)) for t in tt]
        prof += [(0.0, t1 * L + (0.002 if t1 >= 1 else 0.0))]
        k.add(f"{name}band{i}", lathe(base, d, prof, seg=32), col, bone, fuse=False, mirror=mirror, surface="shell")


def curve(pts, n=24):
    """A smooth curve through control points (a Catmull-Rom spline), n points along it, and its tangents."""
    p = np.array(pts, float)
    ext = np.vstack([2 * p[0] - p[1], p, 2 * p[-1] - p[-2]])
    out = []
    segs = len(p) - 1
    for s in np.linspace(0, segs, n):
        i = min(int(s), segs - 1)
        t = s - i
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                          + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    out = np.array(out)
    T = np.gradient(out, axis=0)
    T /= np.linalg.norm(T, axis=1, keepdims=True)
    return out, T


def banded_tail(k, name, pts, r0, r1, col, bands, band_col, ball_r, ball_col, bone="tail", n=28, ball_gap=0.6):
    """
    A loose tail along a curve through pts, its radius from r0 to r1, bands of band_col (t0, t1 along it) as loose
    rings over it, and a glowing ball at its end. Returns the ball's centre.
    """
    P, T = curve(pts, n)
    R = np.linspace(r0, r1, n)
    k.add(name, tube(P, R, seg=20, per=3), col, bone, fuse=False, surface="fur")
    for i, (t0, t1) in enumerate(bands):
        a, b = int(round(t0 * (n - 1))), int(round(t1 * (n - 1)))
        mid = (P[a] + P[b]) / 2
        ax = P[b] - P[a]
        length = np.linalg.norm(ax)
        rr = (R[a] + R[b]) / 2 + 0.0035
        k.add(f"{name}band{i}", ring_band(mid, ax, rr, length + 0.002, thick=0.006), band_col, bone, fuse=False,
              surface="fur")
    c = P[-1] + T[-1] * (ball_r * ball_gap + r1 * 0.3)
    k.add(name + "ball", ell(c, (ball_r, ball_r, ball_r), sub=4), ball_col, bone, fuse=False, glow=ball_col,
          surface="shell")
    return c


def dark_eye(fill=INK, glint=GLINT, gu=-0.22, gv=0.32, gr=0.34):
    """A round eye all dark with one white glint high toward the nose (u outward, v up)."""
    F, G = srgb(fill), srgb(glint)
    def look(u, v):
        c = np.tile(F, (len(u), 1))
        c[((u - gu) / gr) ** 2 + ((v - gv) / (gr * 1.1)) ** 2 < 1] = G
        return c
    return look


def lay_dark_eyes(k, centre, d, half, lid, ink=INK, up=(0, 0, 1)):
    """The lay function for eye_states: the dark eye in eye_look's states, laid mirrored."""
    L, K = srgb(lid), srgb(ink)
    def lay(state):
        k.decal(centre, d, half, eye_look(state, dark_eye(ink), L, K, line=0.17), eps=0.005, rings=20, segs=56,
                mirror=True, up=up)
    return lay


# --- Mareep's frame (bones: body 0, 0.02, 0.22; head 0, -0.13, 0.24; ears +-0.06, -0.15, 0.29; legs +-0.07,
#     -0.07 / 0.1, 0.14; tail 0, 0.17, 0.26) ------------------------------------------------------------------------------
FLEECE_C, FLEECE_R = np.array([0.0, 0.03, 0.205]), np.array([0.145, 0.16, 0.126])
FACE_C, FACE_R = np.array([0.0, -0.178, 0.308]), np.array([0.066, 0.092, 0.07])
SNOUT_C, SNOUT_R = np.array([0.0, -0.245, 0.27]), np.array([0.047, 0.05, 0.043])
CAP_C, CAP_R = np.array([0.0, -0.125, 0.372]), np.array([0.088, 0.088, 0.066])
LEG_X, LEG_Y = 0.072, (-0.07, 0.1)
EAR_BASE, EAR_D = np.array([0.06, -0.15, 0.322]), unit(1.0, 0.12, 0.02)
TAIL = [(0.0, 0.17, 0.26), (0.012, 0.205, 0.3), (0.038, 0.222, 0.36), (0.06, 0.213, 0.405)]


def mareep(src, out):
    k = Kit(src, coat="fur")

    # The fleece: a low cloud of puffs, hanging to within some 0.06 of the ground
    wool(k, "fleece", FLEECE_C, FLEECE_R, "body", n=46, puff=0.064, sink=0.026, seed=3)
    # the fleece a shade deeper underneath
    k.paint(lambda P: inside(P[:, 2] - 0.12, 0.03) * 0.55, WOOL_SHADE, on=["fleece"])

    # The face: blue, standing forward and down out of the fleece, a long muzzle
    FACE = ell(FACE_C, FACE_R, rot=(22, 0, 0))
    k.add("face", FACE, BLUE, "head")
    k.add("snout", ell(SNOUT_C, SNOUT_R, rot=(30, 0, 0)), BLUE, "head")
    k.add("cheek", ell((0, -0.14, 0.29), (0.07, 0.06, 0.06)), BLUE, "head")
    # The cap of fleece over the head's top and back, a spiral curl on its top
    wool(k, "cap", CAP_C, CAP_R, "head", n=16, puff=0.04, sink=0.016, seed=7,
         skip=lambda d: d[1] < -0.55 and d[2] < 0.35)
    spiral = []
    for t in np.linspace(0, 1, 16):
        a = 0.6 + 4.2 * math.pi * t
        rad = 0.05 * (1 - 0.85 * t)
        q = np.array([rad * math.cos(a), -0.008 + rad * math.sin(a) * 0.9, 0.0])
        top = CAP_C + np.array([q[0], q[1], 0.0])
        z = CAP_C[2] + CAP_R[2] * math.sqrt(max(1 - (q[0] / CAP_R[0]) ** 2 - ((q[1] + 0.0) / CAP_R[1]) ** 2, 0))
        spiral.append(top + np.array([0, 0, z - CAP_C[2] + 0.024]))
    k.add("curl", tube(spiral, [0.013] * 15 + [0.011]), WOOL, "head")
    def groove(P):
        Q = np.array(spiral)
        d = np.min(np.linalg.norm(P[:, None, :] - Q[None, ::1, :], axis=2), axis=1)
        return inside(np.abs(d - 0.019) - 0.003, 0.003) * (P[:, 2] > CAP_C[2] + 0.02) * 0.8
    k.paint(groove, WOOL_SHADE)

    # Four short thick blue legs under the fleece, a hoof's cleft at the front of each
    for y, bone in ((LEG_Y[0], "leg{s}"), (LEG_Y[1], "hind{s}")):
        k.add("leg" + bone[:3], tube([(LEG_X, y, 0.14), (LEG_X + 0.004, y - 0.004, 0.06), (LEG_X + 0.006, y - 0.008,
                                                                                          0.03)],
                                     [0.038, 0.035, 0.034]), BLUE, bone, mirror=True)
        k.add("foot" + bone[:3], ell((LEG_X + 0.006, y - 0.014, 0.022), (0.038, 0.044, 0.024)), BLUE, bone,
              mirror=True)
    def cleft(P):
        m = np.zeros(len(P))
        for y in LEG_Y:
            for sx in (1, -1):
                x0 = sx * (LEG_X + 0.006)
                m = np.maximum(m, inside(np.abs(P[:, 0] - x0) - 0.0035, 0.0025) * inside(P[:, 2] - 0.032, 0.003)
                               * inside(P[:, 1] - (y - 0.042), 0.003) * (np.abs(P[:, 1] - y) < 0.07))
        return m
    k.paint(cleft, "#2E4E86")

    # The ears: short horns out to the sides, black at the root and the point, yellow between
    ear_cone(k, "ear", EAR_BASE, EAR_D, 0.112, 0.036, YELLOW,
             [(0.0, 0.3, BLACK), (0.62, 1.0, BLACK)], "ear{s}")

    # The tail out of the back, banded black and yellow, the glowing orange ball at its end
    banded_tail(k, "tail", TAIL, 0.022, 0.019, YELLOW, [(0.0, 0.2), (0.4, 0.58), (0.8, 1.0)], BLACK, 0.036, ORANGE)

    # The mouth: a short dark line curving under the snout's end, two small nostril marks over it
    MC = SNOUT_C + np.array([0, -0.035, -0.022])
    dm = unit(0, -0.75, -0.62)
    CB, CM = srgb(BLUE), srgb(MOUTH)
    def mouth(u, v):
        c = np.tile(CB, (len(u), 1))
        c[(np.abs(v - 0.55 * (u * u) + 0.1) < 0.28) & (np.abs(u) < 0.9)] = CM
        return c
    k.decal(hit((ell(SNOUT_C, SNOUT_R, rot=(30, 0, 0))), MC - dm * 0.3, dm), dm, (0.03, 0.012), mouth, eps=0.004,
            rings=10, segs=48, up=(0, -0.6, 0.8))

    k.build(voxel=0.0062, smooth=4, band=0.005)

    # The eyes: round, dark, a white glint, high on the face's sides under the cap
    de = unit(0.52, -0.8, 0.3)
    E = hit(FACE, FACE_C + np.array([0.0, 0.0, 0.03]) + de * 0.4, -de)
    k.eye_states(lay_dark_eyes(k, E, de, (0.021, 0.025), BLUE))
    k.export(out)


if __name__ == "__main__":
    mareep(*args())
