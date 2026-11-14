# Porygon-Z sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/porygonz.py -- <the export's Porygon-Z.glb> <out.glb>
# Grown from porygon2.py: its pink and blue, its bill and paddles (along, taper) and its way of making an eye (the
# white with a rim, the pupil a patch of its own) kept; the eye's ground turned yellow and given a ring.
#
# The design, from the references: Porygon2 come apart, every part floating a little off the others. A round pink
# head with no neck, a short pink horn of an antenna leaning back from its crown, the blue bill out of its front, a
# large round yellow eye on each side turned toward the front (a dark rim, a dark ring inside, a black oval pupil); a
# pink ball of a body under it with a blue band slanting round its middle; long blue paddles of arms, narrow at the
# root and wide at the end (held out and down, not up as in the art, so they are not taken for ears), and a blue
# paddle of a tail hanging under the body.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, eye_look, inside, args

PINK, BLUE = "#EA5379", "#66C6E6"
WHITE, INK, PUPIL = "#F7F7F8", "#2A2430", "#16141A"
YELLOW = "#F4CF3C"


# --- the eye: the line's eye on yellow, with a thick dark ring between the rim and the pupil ------------------------
EYE_HALF = (0.043, 0.046)                           # the eye's half width and height
RIM = 0.825                                         # the rim's inner edge, a share of the eye's half size
RING = (0.425, 0.625)                                 # the dark ring's inner and outer edges
PUPIL_HALF = (0.17, 0.27)                           # the pupil's half sizes, shares of the eye's
CY, CI, CP = srgb(YELLOW), srgb(INK), srgb(PUPIL)


def eye_open(u, v):
    c = np.tile(CY, (len(u), 1))
    r = np.sqrt(u * u + v * v)
    c[r > RIM] = CI
    c[(r > RING[0]) & (r < RING[1])] = CI
    return c


def eye(k, state, at, n, lid=PINK):
    """The yellow with its rim and ring (or a state of it), and in Open and Fierce the pupil as a patch of its own."""
    k.decal(at, n, EYE_HALF, eye_look(state, eye_open, srgb(lid), CI, line=0.15), eps=0.0035, rings=20, segs=56,
            mirror=True)
    if state in ("Open", "Fierce"):
        dz = 0.0 if state == "Open" else -0.2 * EYE_HALF[1]
        k.decal(np.array(at) + np.array([0, 0, dz]), n, (PUPIL_HALF[0] * EYE_HALF[0], PUPIL_HALF[1] * EYE_HALF[1] *
                                                          (1 if state == "Open" else 0.75)),
                PUPIL, eps=0.0052, rings=8, segs=40, mirror=True)


def along(V, base, d, tip_side=(0, 0, 1)):
    """Points made along +Z turned so that +Z runs along d and set at base (a right-handed frame)."""
    d = unit(*d)
    x = np.cross(np.array(tip_side, float), d)
    if np.linalg.norm(x) < 1e-3:
        x = np.cross((0, -1.0, 0), d)
    x /= np.linalg.norm(x)
    y = np.cross(d, x)
    return V @ np.stack([x, y, d], axis=1).T + np.array(base, float)


def taper(lo, hi, power=1.0):
    """A deform that scales a sphere's width from lo at its -Z end to hi at its +Z end."""
    def f(V):
        V = V.copy()
        t = np.clip((V[:, 2] + 1) / 2, 0, 1) ** power
        s = lo + (hi - lo) * t
        V[:, 0] *= s; V[:, 1] *= s
        return V
    return f


def paddle(root, d, half_len, wide, thick, lo=0.32):
    """A blue paddle from its root along d, narrow there and wide and round at its end, its broad face to the front."""
    V, F = ell((0, 0, 0), (wide, thick, half_len), deform=taper(lo, 1.0, 0.7))
    return along(V, np.array(root, float) + unit(*d) * half_len, d, (0, 1, 0)), F


# --- Porygon-Z -----------------------------------------------------------------------------------------------------------
HC = np.array([0.0, -0.025, 0.538])                 # the head's middle (its joint is at 0, -0.02, 0.459)
HR = (0.08, 0.078, 0.073)
BC = np.array([0.0, 0.0, 0.36])                     # the body's middle, on its joint
BR = (0.077, 0.075, 0.079)
EYE_DIR = unit(0.78, -0.56, 0.2)                    # +X eye, as Porygon2's
BAND = unit(0.35, -0.3, 1.0)                        # the slanting band's plane across the body
ARM = unit(0.74, -0.42, -0.52)                      # the +X arm, out, forward and down

if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")

    # The head, its antenna and the flat spots under the eyes
    k.add("head", ell(HC, HR), PINK, "head")
    k.add("antenna", tube([(0, 0.0, HC[2] + 0.05), (0, 0.025, HC[2] + 0.1), (0, 0.05, HC[2] + 0.14)],
                          [0.026, 0.014, 0.004]), PINK, "head")
    at = HC + EYE_DIR * np.array(HR) * 0.98
    flat = ell((0, 0, 0), (0.05, 0.052, 0.024))
    k.add("face", (along(flat[0], at - EYE_DIR * 0.029, EYE_DIR), flat[1]), PINK, "head", mirror=True)
    # The bill, as Porygon2's
    bill = ell((0, 0, 0), (0.042, 0.03, 0.075), deform=taper(1.0, 0.72))
    k.add("bill", (along(bill[0], (0, -0.15, HC[2] - 0.012), (0, -1, -0.15), (0, 0, 1)), bill[1]), BLUE, "head",
          fuse=False)
    # The body, apart from the head
    k.add("body", ell(BC, BR), PINK, "body")
    # The band: blue within a slab slanting across the body's middle
    k.paint(lambda P: inside(np.abs((P - BC) @ BAND + 0.008) - 0.036, 0.002) * inside(np.linalg.norm(P - BC, axis=1) - 0.12, 0.003),
            BLUE)
    # Arms and tail: paddles floating a little off the body
    k.add("arm", paddle((0.084, -0.03, 0.402), ARM, 0.088, 0.042, 0.026), BLUE, "arm{s}", fuse=False, mirror=True)
    k.add("tail", paddle((0.006, 0.01, 0.272), unit(0.12, 0.03, -1), 0.105, 0.042, 0.03), BLUE, "tail", fuse=False)

    # Head and body float apart: their weights must not blend across the gap
    k.build(voxel=0.0042, smooth=4, band=0.006, wband=0.015)
    k.eye_states(lambda state: eye(k, state, at, EYE_DIR))
    k.export(out)
