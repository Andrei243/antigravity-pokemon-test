# Lunatone sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lunatone.py -- <the export's Lunatone.glb> <out.glb>
#
# The design, from the references: a pale cream crescent moon of stone floating upright and facing the front, thick
# and rounded like a croissant, its horns pointing to the viewer's left; out of the hollow of the crescent a long
# pointed beak of two parts, the upper one larger, a black line between them running back into the eye; one large eye
# on the crescent's face beside the beak's root: a black disc with a red iris and a dark upright slit of a pupil, a
# white glint; round craters pocking its face and its back, each a darker ring round a slightly darker floor. The
# pictures show only the front: the back is left with craters and no eye.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, unit, eye_look, args

STONE, HOLLOW, BEAK, BEAK_DARK = "#E8D6A0", "#D6C28A", "#E4D19C", "#CDB884"
RIM, FLOOR, INK, RED, SLIT = "#BFA670", "#DAC690", "#26201E", "#E2425E", "#4A0C18"

C = np.array([0.0, 0.0, 0.273])          # the moon's middle, on the body bone
R, T = 0.212, 0.085                       # the outer circle's radius; half the moon's thickness
CUT_X, CUT_R = -0.63 * R, 0.70 * R       # the hollow: its circle's middle (left of the moon's) and radius
WALL = CUT_X + CUT_R                      # where the hollow's deepest point lies, at the middle's height


def at(x, z):
    """A point of the moon's face at x, z of its radius from its middle."""
    return C + np.array([x * R, 0, z * R])


def band(phi):
    """Along a ray from the moon's middle at phi (radians from +X toward the top): where the stone begins (leaving the
    hollow's circle) and where it ends (the outer circle)."""
    d = np.array([math.cos(phi), math.sin(phi)])
    c = np.array([CUT_X, 0.0])
    b = d @ c
    t2 = b + math.sqrt(max(b * b - (c @ c - CUT_R ** 2), 0.0))
    return min(t2, R), R


def moon(k):
    """A thick crescent: a tube along the middle of the stone between the hollow and the outer circle, as wide as the
    stone there, flattened a little front to back: a croissant, rounded all over and thinning to its horns."""
    pts, rad = [], []
    for phi in np.radians(np.linspace(-131, 131, 31)):
        t2, t1 = band(phi)
        m = (t1 + t2) / 2
        pts.append(C + np.array([m * math.cos(phi), 0, m * math.sin(phi)]))
        rad.append(max((t1 - t2) / 2, 0.017))
    k.add("moon", blade(pts, rad, (0, 1, 0), T / max(rad)), STONE, "body")
    # the hollow's side of the stone, a shade darker
    k.paint(lambda P: inside(np.hypot(P[:, 0] - C[0] - CUT_X, P[:, 2] - C[2]) - CUT_R - 0.012, 0.006)
            * inside(np.abs(P[:, 1]) - T * 0.75, 0.01), HOLLOW)


def beak(k):
    """A long pointed beak of two parts out of the hollow, the black line between them."""
    z0 = C[2]
    x0 = C[0] + WALL + 0.03
    tip = C[0] + WALL - 0.19
    upper = [(x0, 0.034, z0 + 0.005), (x0, -0.034, z0 + 0.005), (x0, 0.03, z0 + 0.062), (x0, -0.03, z0 + 0.062),
             (x0 - 0.08, 0, z0 + 0.056), (x0 - 0.08, 0.026, z0 + 0.034), (x0 - 0.08, -0.026, z0 + 0.034),
             (tip, 0.005, z0 + 0.005), (tip, -0.005, z0 + 0.005), (tip + 0.01, 0, z0 + 0.012)]
    k.add("upper", hull(upper), BEAK, "head", fuse=False, surface="shell")
    lower = [(x0, 0.028, z0 - 0.009), (x0, -0.028, z0 - 0.009), (x0, 0.018, z0 - 0.03), (x0, -0.018, z0 - 0.03),
             (tip + 0.02, 0.004, z0 - 0.009), (tip + 0.02, -0.004, z0 - 0.009), (tip + 0.04, 0, z0 - 0.016)]
    k.add("lower", hull(lower), BEAK_DARK, "head", fuse=False, surface="shell")
    line = [(x0 + 0.01, 0.03, z0 + 0.005), (x0 + 0.01, -0.03, z0 + 0.005), (x0 + 0.01, 0.026, z0 - 0.009),
            (x0 + 0.01, -0.026, z0 - 0.009), (tip + 0.012, 0.003, z0 + 0.003), (tip + 0.012, -0.003, z0 + 0.003),
            (tip + 0.012, 0, z0 - 0.004)]
    k.add("line", hull(line), INK, "head", fuse=False, surface="shell")


def crater_look(u, v):
    r = np.sqrt(u * u + v * v)
    c = np.tile(srgb(FLOOR), (len(u), 1))
    c[r > 0.7] = srgb(RIM)
    return c


FRONT = [(0.42, 0.7, 0.024, 0.03), (0.66, 0.44, 0.009, 0.01), (0.78, -0.42, 0.018, 0.028), (0.34, -0.66, 0.028, 0.026),
         (0.02, -0.8, 0.018, 0.02)]
BACK = [(0.5, 0.6, 0.026, 0.026), (0.2, -0.7, 0.022, 0.024), (0.75, -0.2, 0.02, 0.02), (-0.1, 0.82, 0.016, 0.016),
        (0.48, -0.08, 0.03, 0.03)]


def marks(k):
    for x, z, a, b in FRONT:
        k.decal(at(x, z) + (0, -T, 0), unit(x * 0.35, -1, z * 0.35), (a, b), crater_look, eps=0.003, rings=10, segs=40)
    for x, z, a, b in BACK:
        k.decal(at(x, z) + (0, T, 0), unit(x * 0.35, 1, z * 0.35), (a, b), crater_look, eps=0.003, rings=10, segs=40)
    # the black line from the beak's root back into the eye
    k.decal(at(0.0, 0.0) + (WALL + 0.035, -T, 0), (0, -1, 0), (0.035, 0.0055), INK, eps=0.003, rings=8, segs=40)


EYE = at(0.43, 0.0) + (0, -T, 0)
EYE_N = unit(0.12, -1, 0)


def disc(k):
    """The eye's black disc: it stays whatever the eye does."""
    k.decal(EYE, EYE_N, (0.055, 0.057), INK, eps=0.0032, rings=16, segs=56)


def eye_open(u, v):
    c = np.tile(srgb(RED), (len(u), 1))
    c[(u / 0.17) ** 2 + (v / 0.62) ** 2 < 1] = srgb(SLIT)
    c[((u + 0.42) / 0.16) ** 2 + ((v - 0.42) / 0.16) ** 2 < 1] = srgb("#FFE6EA")
    c[u * u + v * v > 0.9] = srgb(INK)
    return c


def eyes(k, state):
    k.decal(EYE, EYE_N, (0.027, 0.033), eye_look(state, eye_open, srgb(INK), srgb(RED), line=0.2), eps=0.0055,
            rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    moon(k)
    beak(k)
    marks(k)
    disc(k)
    k.build(voxel=0.004, smooth=3)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
