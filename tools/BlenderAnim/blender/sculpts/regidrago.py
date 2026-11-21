# Regidrago sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/regidrago.py -- <the export's Regidrago.glb> <out.glb>
#
# The design, from the references: a giant made in the shape of a dragon. Its body is a round ball of deep pink-red
# crystal with a darker zigzag seam round its lower half, seven blue dots on its front (two above, three across, two
# below in a column), on two stubby legs ending in small dark claws. Its arms are the two halves of a dragon's head,
# dark slate green shells open toward the body on a jagged red lining, by far its largest masses: on its left (the
# turntable's viewer's left) the upper jaw and skull standing tall beside the body, its snout rising to a blunt point
# and a long spike pointing down from its foot; on its right the lower jaw, an arch rising out from the shoulder and
# hanging down to the ground's height. The stated exception to arms inside the body's width (necrozma.py's hands):
# held out as the pictures hold them, a little forward. Its dots are its eyes: open they shine, shut they dim.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, hull, sheet, outward, tri, axes

# --- colours ----------------------------------------------------------------------------------------------------------
RED, SEAM, LINING = "#D8245E", "#8E1236", "#E0305E"
GREEN, GREENDEEP = "#3C5A54", "#2A403C"
DOT, DOTRIM = "#3CA8E6", "#1E6AA8"

# --- the frame --------------------------------------------------------------------------------------------------------
BC, BR = np.array([0.0, 0.0, 0.272]), 0.19        # the ball's middle and radius
DOTS = [(-1, 1.3), (1, 1.3), (-1, 0.25), (0, 0.4), (1, 0.25), (0, -0.7), (0, -1.65)]
DOT_STEP, DOT_HALF = 0.036, 0.0145


def spine(pts, n=40):
    """A smooth curve through points (Catmull-Rom by the kit's tube), as n points and their tangents."""
    from sculptkit import tube as _t
    P = np.array(pts, float)
    ext = np.vstack([2 * P[0] - P[1], P, 2 * P[-1] - P[-2]])
    out_ = []
    for i in range(len(P) - 1):
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        for t in np.linspace(0, 1, 12, endpoint=False):
            out_.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                               + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    out_.append(P[-1])
    out_ = np.array(out_)
    s = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(out_, axis=0), axis=1))])
    u = np.linspace(0, s[-1], n)
    C = np.array([np.interp(u, s, out_[:, j]) for j in range(3)]).T
    T = np.gradient(C, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
    return C, T


def jaw_shell(pts, radii, opening, gap, zig, teeth, inset=0.0, thick=0.024):
    """
    A half of a dragon's head: a thick shell round a curved spine, open along one side. pts and radii lay the spine
    out (radii shared along it); opening(i, C, T) the direction of the open side at each point; gap the half angle of
    the opening, and zig how far a zigzag of teeth eats into the shell's edges (teeth along the length).
    """
    C, T = spine(pts)
    n = len(C)
    R = np.interp(np.linspace(0, 1, n), np.linspace(0, 1, len(radii)), radii) - inset
    N = []
    for i in range(n):
        o = np.array(opening(i / (n - 1), C[i], T[i]), float)
        o = o - T[i] * (o @ T[i]); o /= np.linalg.norm(o)
        N.append(o)
    N = np.array(N); B = np.cross(T, N)

    def at(u, v):
        i = np.clip(np.round(u * (n - 1)).astype(int), 0, n - 1)
        g = gap + zig * (tri(u * teeth) + 1) / 2
        a = g + v * (2 * math.pi - 2 * g)
        r = R[i][..., None]
        return C[i] + r * (np.cos(a)[..., None] * N[i] + np.sin(a)[..., None] * B[i])
    return outward(sheet(at, n, 36, thick))


def ball(k):
    k.add("ball", ell(BC, (BR, BR * 0.95, BR)), RED, "body")
    # the darker zigzag seam round the lower half
    def seam(P):
        Q = P - BC
        th = np.arctan2(Q[:, 0], -Q[:, 1])
        z0 = -0.06 + 0.022 * tri(th * 7 / (2 * math.pi))
        r = np.linalg.norm(Q, axis=1)
        return inside(np.abs(Q[:, 2] - z0) - 0.0045, 0.0025) * inside(BR * 0.9 - r, 0.004)
    k.paint(seam, SEAM)


def legs(k):
    for s, side in ((1, "R"), (-1, "L")):
        bone = f"leg{side}"
        k.add(f"stump{side}", tube([(0.075 * s, -0.01, 0.16), (0.08 * s, -0.015, 0.08)], [0.055, 0.05]), RED, bone)
        # a small dark cup of claws, its rim pointed
        F = np.array([0.08 * s, -0.02, 0.0])
        k.add(f"foot{side}", hull([F + (0.05 * math.cos(a), 0.05 * math.sin(a), 0.0) for a in np.linspace(0, 6.28, 9)]
                                  + [F + (0.045 * math.cos(a), 0.045 * math.sin(a), 0.06) for a in np.linspace(0, 6.28, 9)]),
              GREEN, bone, fuse=False, surface="shell")
        for j, a in enumerate(np.linspace(0.3, 2 * math.pi + 0.3, 5, endpoint=False)):
            root = F + np.array([0.046 * math.cos(a), 0.046 * math.sin(a), 0.035])
            tip = root + np.array([0.03 * math.cos(a), 0.03 * math.sin(a), 0.055])
            ca, sa, cb, sb = math.cos(a), math.sin(a), math.cos(a + 1.57), math.sin(a + 1.57)
            k.add(f"claw{j}{side}", hull([root + np.array((0.016 * cb, 0.016 * sb, 0.0)),
                                          root - np.array((0.016 * cb, 0.016 * sb, 0.0)),
                                          root + np.array((0.012 * ca, 0.012 * sa, -0.01)),
                                          root - np.array((0.012 * ca, 0.012 * sa, 0.02)), tip]),
                  GREENDEEP, bone, fuse=False, surface="shell")


def upper_jaw(k):
    """On the model's left (-X): the upper jaw and skull, standing tall, open toward the body and the front."""
    pts = [(-0.17, -0.04, 0.21), (-0.225, -0.07, 0.36), (-0.24, -0.08, 0.54), (-0.22, -0.08, 0.7),
           (-0.195, -0.075, 0.81)]
    radii = [0.1, 0.13, 0.112, 0.07, 0.009]
    op = lambda t, c, d: (1.0, -0.75, -0.1)
    k.add("skull", jaw_shell(pts, radii, op, 0.34, 0.34, 7), GREEN, "armL", fuse=False, surface="shell")
    k.add("skulllining", jaw_shell(pts, [r * 0.88 for r in radii], op, 0.24, 0.0, 7, thick=0.014), LINING, "armL",
          fuse=False, surface="shell")
    # the long spike pointing down from its foot
    k.add("spike", hull([(-0.24, -0.04, 0.26), (-0.2, -0.1, 0.24), (-0.27, -0.11, 0.3), (-0.23, -0.02, 0.32),
                         (-0.37, -0.15, 0.03)]), GREEN, "armL", fuse=False, surface="shell")
    k.add("shoulderL", ell((-0.15, -0.02, 0.34), (0.065, 0.075, 0.075)), GREEN, "armL")


def lower_jaw(k):
    """On the model's right (+X): the lower jaw, an arch rising out from the shoulder and hanging to the ground."""
    pts = [(0.16, -0.03, 0.37), (0.26, -0.06, 0.49), (0.37, -0.08, 0.49), (0.43, -0.09, 0.36), (0.43, -0.09, 0.19),
           (0.41, -0.08, 0.06)]
    radii = [0.075, 0.1, 0.105, 0.1, 0.085, 0.03]
    mid = np.array([0.31, -0.02, 0.28])
    def op(t, c, d):
        v = mid - c
        return v / np.linalg.norm(v) + np.array([0, -0.5, -0.2])
    k.add("jaw", jaw_shell(pts, radii, op, 0.55, 0.36, 8), GREEN, "armR", fuse=False, surface="shell")
    k.add("jawlining", jaw_shell(pts, [r * 0.88 for r in radii], op, 0.42, 0.0, 8, thick=0.014), LINING, "armR",
          fuse=False, surface="shell")
    k.add("shoulderR", ell((0.15, -0.02, 0.36), (0.065, 0.075, 0.075)), GREEN, "armR")


def dot_look():
    c0, rim, hi = srgb(DOT), srgb(DOTRIM), np.array([0.8, 0.95, 1.0])
    def look(u, v):
        c = np.tile(c0, (len(u), 1))
        c[u * u + v * v > 0.8 ** 2] = rim
        c[(u + 0.25) ** 2 + (v - 0.3) ** 2 < 0.22 ** 2] = hi
        return c
    return look


def dots(k, state):
    c0 = srgb(DOT)
    look = eye_look(state, dot_look(), c0 * 0.45, c0 * 0.12, line=0.2)
    for col, row in DOTS:
        x, z = col * DOT_STEP, 0.02 + row * DOT_STEP
        y = -math.sqrt(max((BR * 0.95) ** 2 * (1 - (x / BR) ** 2 - (z / BR) ** 2), 1e-6))
        p = BC + np.array([x, y, z])
        n = unit(x / BR ** 2, y / (BR * 0.95) ** 2, z / BR ** 2)
        k.decal(p, n, (DOT_HALF, DOT_HALF), look, eps=0.0025, rings=20, segs=56)


def regidrago(src, out):
    k = Kit(src, coat="shell")
    ball(k)
    legs(k)
    upper_jaw(k)
    lower_jaw(k)
    k.build(voxel=0.0065, smooth=4, band=0.006, wband=0.04)
    k.eye_states(lambda state: dots(k, state))
    k.export(out)


if __name__ == "__main__":
    regidrago(*args())
