# Regieleki sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/regieleki.py -- <the export's Regieleki.glb> <out.glb>
#
# The design, from the references: a giant of electricity hanging in the air. Its body is its head, a lime-yellow urn
# taller than it is wide, its top cut in a shallow V between two blunt points, narrowing below into a coiled blue
# spring; under the spring a small cross of yellow lightning bolts. On each side a fat blue socket, and out of each
# socket its arm: a great spindle of crackling electricity, yellow with paler wavy streaks along it and a ragged edge,
# by far its largest masses (the stated exception to arms inside the body's width, as necrozma.py's hands; carried
# forward and a little down here so that the model is not one line across its cell). Its face is a row of pink dots
# rising across the front with a pair at its end. The dots are its eyes: open they shine, shut they dim with a line.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, hull, slab, lathe, axes, tri, outward

# --- colours ----------------------------------------------------------------------------------------------------------
YELLOW, PALE, DEEP = "#E4EE3E", "#F8FAB4", "#C8D428"
BLUE, BLUEDEEP = "#3448B8", "#22307E"
PINK, PINKRIM = "#F27AA8", "#B8406E"

# --- the frame --------------------------------------------------------------------------------------------------------
C = np.array([0.0, 0.0, 0.362])                    # the body's middle (the body bone stands at 0.327)
CR = np.array([0.098, 0.086, 0.135])                 # its half width, half depth, half height before the deform
SOCKET_X = 0.082                                   # where each socket's plug starts out of the side
WING_ROOT = np.array([0.142, -0.005, 0.36])        # where each spindle of electricity starts (the +X one)
WING_DIR = unit(0.78, -0.62, -0.2)                # out, forward and a little down
WING_LEN, WING_W, WING_T = 0.43, 0.082, 0.06       # its length, half breadth (up) and half thickness


def urn(V):
    """The body's deform of the unit sphere: a flattened top cut in a shallow V, narrowing below to the spring."""
    V = V.copy()
    z = V[:, 2]
    low = np.clip(-z, 0, 1)
    V[:, 0] *= 1 - 0.42 * low ** 1.6
    V[:, 1] *= 1 - 0.42 * low ** 1.6
    hi = np.clip(z, 0, 1)
    V[:, 2] = np.where(z > 0, z * 0.82 - 0.22 * hi ** 3 * (1 - np.abs(V[:, 0]) ** 1.2), z)
    return V


def body_point(theta, phi):
    """A point of the body's surface and its rough normal, by azimuth from the front (theta, toward +X) and height."""
    s = np.array([[math.sin(theta) * math.cos(phi), -math.cos(theta) * math.cos(phi), math.sin(phi)]])
    p = urn(s)[0] * CR + C
    n = unit(*(s[0] / CR))
    return p, n


def body(k):
    k.add("body", ell(C, CR, deform=urn), YELLOW, "body")
    # the two blunt points of the top, standing out a little
    for s in (1, -1):
        k.add("point" + ("R" if s > 0 else "L"),
              tube([(0.05 * s, 0.0, 0.425), (0.068 * s, 0.0, 0.452), (0.078 * s, 0.0, 0.468)], [0.034, 0.024, 0.012]),
              YELLOW, "body")
    # the neck where the spring starts, and the spring: a lathe of ridges narrowing to the foot
    prof = [(0.0, 0.0), (0.05, 0.0)]
    r0, z = 0.05, 0.0
    for i in range(3):
        r = r0 - 0.006 * i
        prof += [(r, z + 0.004), (r - 0.012, z + 0.013), (r - 0.012, z + 0.017), (r, z + 0.026)]
        z += 0.028
    prof += [(0.03, z + 0.008), (0.0, z + 0.01)]
    k.add("spring", lathe((0, 0, 0.245), (0, 0, -1), prof), BLUE, "body", fuse=False, surface="metal")
    k.add("collar", lathe((0, 0, 0.258), (0, 0, -1), [(0.0, 0.0), (0.056, 0.0), (0.06, 0.008), (0.056, 0.02),
                                                     (0.0, 0.02)]), BLUEDEEP, "body", fuse=False, surface="metal")


def foot(k):
    """The cross of yellow bolts under the spring, as the references show: a short core and bolts out and down."""
    F0 = np.array([0.0, 0.0, 0.135])
    k.add("core", ell(F0, (0.026, 0.026, 0.03), sub=3), YELLOW, "body", fuse=False, surface="metal")
    bolt = [(0.0, -0.016), (0.03, -0.012), (0.026, 0.004), (0.06, 0.0), (0.03, 0.016), (0.034, 0.002), (0.0, 0.016)]
    for i, (dx, dy, dz) in enumerate([(1, -0.35, -0.5), (-1, -0.35, -0.5), (0.25, 0.9, -0.55), (-0.3, -0.9, -0.7),
                                      (0.9, 0.3, 0.15), (-0.9, 0.3, 0.15)]):
        d = unit(dx, dy, dz)
        a, b = axes(d)
        k.add(f"bolt{i}", slab([(p[0] * 1.35, p[1] * 1.35) for p in bolt], F0 + d * 0.01, d, b, 0.016), YELLOW,
              "body", fuse=False, surface="metal")


def sockets(k):
    """A fat blue plug on each side, the wing's root standing in it."""
    prof = [(0.0, 0.0), (0.05, 0.0), (0.06, 0.012), (0.062, 0.03), (0.058, 0.05), (0.046, 0.062), (0.0, 0.064)]
    k.add("socket", lathe((SOCKET_X, 0.0, 0.36), (1, 0, 0), prof), BLUE, "wing{s}", fuse=False, mirror=True,
          surface="metal")
    k.add("socketrim", lathe((SOCKET_X + 0.06, 0.0, 0.36), (1, 0, 0), [(0.0, 0.0), (0.034, 0.0), (0.038, 0.006),
                                                                      (0.0, 0.008)]),
          BLUEDEEP, "wing{s}", fuse=False, mirror=True, surface="metal")


def wing_frame():
    d = WING_DIR
    up = np.array([0.0, 0.0, 1.0]) - d * d[2]
    up /= np.linalg.norm(up)
    side = np.cross(up, d)
    return d, up, side


def spindle(V):
    """The unit sphere made a spindle of electricity along its z: pointed at both ends, fattest a third of the way out,
    its edge (along x, which is laid upward) ragged."""
    V = V.copy()
    t = (V[:, 2] + 1) / 2
    w = np.sin(np.pi * np.clip(t, 0, 1) ** 0.8) ** 0.9
    rag = 1 + 0.13 * tri(t * 6.5 + 0.25 * np.sign(V[:, 0])) * np.abs(V[:, 0]) ** 2
    V[:, 0] *= w * rag
    V[:, 1] *= w
    return V


def wings(k):
    d, up, side = wing_frame()
    mid = WING_ROOT + d * WING_LEN / 2
    V, F = ell((0, 0, 0), (WING_W, WING_T, WING_LEN / 2), deform=spindle, sub=5)
    M = np.stack([up, side, d], axis=1)             # the spindle's x up, y across, z along d
    V = V @ M.T + mid
    k.add("wing", (V, F), YELLOW, "wing{s}", mirror=True)

    # the paler streaks along it, wavy, and a deeper tone along its lower edge
    def streaks(P):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        R = Q - WING_ROOT
        a = R @ d                                       # along
        b = R @ up                                      # up
        t = np.clip(a / WING_LEN, 0, 1)
        w = WING_W * np.sin(np.pi * t ** 0.8) ** 0.9 + 1e-4
        v = b / w                                       # across, -1..1
        wav = 0.12 * np.sin(t * 26) + 0.06 * np.sin(t * 61 + 1.3)
        band = np.abs(np.abs(v - 0.05 + wav) - 0.42) - 0.13
        return inside(band, 0.04) * inside(t - 0.93, 0.03) * inside(0.06 - t, 0.03)
    k.paint(streaks, PALE, on=["wing"])

    # sparks: a few short loose bolts crackling off the spindle's edges
    bolt = [(0.0, -0.012), (0.022, -0.009), (0.02, 0.003), (0.045, 0.0), (0.022, 0.012), (0.025, 0.002), (0.0, 0.012)]
    for j, (t, sgn) in enumerate([(0.25, 1), (0.55, -1), (0.75, 1), (0.4, -1)]):
        base = WING_ROOT + d * WING_LEN * t + up * sgn * WING_W * 0.62 * math.sin(math.pi * t ** 0.8)
        dd = unit(*(up * sgn + d * 0.5))
        k.add(f"spark{j}", slab([(p[0] * 1.3, p[1] * 1.3) for p in bolt], base, dd, d, 0.014), PALE, "wing{s}",
              fuse=False, mirror=True, surface="metal")


# --- the dots, which are the eyes -------------------------------------------------------------------------------------
# a row of four rising across the front from the model's right to its left (the turntable's viewer's left to right),
# with a pair above and below its third
DOTS = [(-0.6, -0.12), (-0.27, 0.0), (0.06, 0.12), (0.4, 0.3), (0.42, -0.02)]
DOT_HALF = 0.016


def dot_look():
    c0, rim, hi = srgb(PINK), srgb(PINKRIM), np.array([1.0, 0.86, 0.92])
    def look(u, v):
        c = np.tile(c0, (len(u), 1))
        c[u * u + v * v > 0.78 ** 2] = rim
        c[(u + 0.25) ** 2 + (v - 0.3) ** 2 < 0.22 ** 2] = hi
        return c
    return look


def dots(k, state):
    c0 = srgb(PINK)
    look = eye_look(state, dot_look(), c0 * 0.55, c0 * 0.15, line=0.2)
    for th, ph in DOTS:
        p, n = body_point(th, ph)
        k.decal(p, n, (DOT_HALF, DOT_HALF), look, eps=0.0025, rings=20, segs=56)


def regieleki(src, out):
    k = Kit(src, coat="metal")
    body(k)
    foot(k)
    sockets(k)
    wings(k)
    k.build(voxel=0.0045, smooth=4, band=0.005, wband=0.03)
    k.eye_states(lambda state: dots(k, state))
    k.export(out)


if __name__ == "__main__":
    regieleki(*args())
