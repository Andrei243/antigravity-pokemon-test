# Porygon2 sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/porygon2.py -- <the export's Porygon2.glb> <out.glb>
# Grown from porygon.py: its pink and blue and its way of making an eye (eye_open, eye) kept; the facets gone.
#
# The design, from the references: Porygon smoothed into a toy duck. A round pink head on a short thick neck, a large
# white eye on each side turned toward the front (a dark rim, a tall black oval pupil), a long flat blue bill out of
# the lower front of the face, tapering to a rounded tip; an egg of a pink body whose whole underside is blue, a
# rounded blue foot forward under the chest on each side and a bigger blue one behind, and a long blue paddle of a
# tail rising up and back, narrow at its root and wide and round at its end. No arms show (the skeleton's armL and
# armR carry nothing).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, eye_look, inside, args

PINK, BLUE = "#EA5379", "#66C6E6"
WHITE, INK, PUPIL = "#F7F7F8", "#2A2430", "#16141A"


# --- the eye: the line's white, now round, with a rim and a tall oval pupil ------------------------------------------
EYE_HALF = (0.041, 0.05)                            # the eye's half width and height
RIM = 0.825                                         # the rim's inner edge, a share of the eye's half size
PUPIL_HALF = (0.25, 0.44)                            # the pupil's half sizes, shares of the eye's
CW, CI, CP = srgb(WHITE), srgb(INK), srgb(PUPIL)


def eye_open(u, v):
    c = np.tile(CW, (len(u), 1))
    c[u * u + v * v > RIM ** 2] = CI
    return c


def eye(k, state, at, n, lid=PINK):
    """The white with its rim (or a state of it), and in Open and Fierce the pupil as a patch of its own over it (in
    the colour function its edge came out ragged); in Fierce it sits lower, under the lid."""
    k.decal(at, n, EYE_HALF, eye_look(state, eye_open, srgb(lid), CI, line=0.15), eps=0.0035, rings=20, segs=56,
            mirror=True)
    if state in ("Open", "Fierce"):
        dz = 0.0 if state == "Open" else -0.2 * EYE_HALF[1]
        k.decal(np.array(at) + np.array([0, 0, dz]), n, (PUPIL_HALF[0] * EYE_HALF[0], PUPIL_HALF[1] * EYE_HALF[1] *
                                                          (1 if state == "Open" else 0.75)),
                PUPIL, eps=0.0052, rings=8, segs=40, mirror=True)


def along(V, base, d, tip_side=(0, 0, 1)):
    """Points made along +Z turned so that +Z runs along d and set at base."""
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


# --- Porygon2 ------------------------------------------------------------------------------------------------------------
HC = np.array([0.0, -0.10, 0.43])                  # the head's middle (its joint is at 0, -0.07, 0.33)
HR = (0.098, 0.1, 0.105)
EYE_DIR = unit(0.78, -0.56, 0.2)                    # +X eye: from the head's middle, some 45 degrees off ahead
BELLY = lambda y: 0.152 - 0.085 * (y + 0.1)         # the blue underside's edge on the body, rising a little behind

if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")

    # Head, neck and body: one smooth pink mass
    k.add("head", ell(HC, HR), PINK, "head")
    k.add("neck", tube([(0, -0.03, 0.26), (0, -0.07, 0.33), (0, -0.095, 0.40)], [0.072, 0.06, 0.066]), PINK, "head")
    k.add("body", ell((0, 0.04, 0.195), (0.118, 0.165, 0.122), rot=(-6, 0, 0)), PINK, "body")
    # The bill: long, flat and tapering, out of the lower front of the face
    bill = ell((0, 0, 0), (0.048, 0.034, 0.09), deform=taper(1.0, 0.72))
    k.add("bill", (along(bill[0], (0, -0.24, 0.395), (0, -1, -0.12), (0, 0, 1)), bill[1]), BLUE, "head", fuse=False)
    # Feet: a rounded blue one forward under the chest each side, a bigger one behind
    k.add("fore", ell((0.072, -0.095, 0.06), (0.058, 0.1, 0.06), rot=(-6, 0, 6)), BLUE, "leg{s}", mirror=True)
    k.add("under", ell((0, 0.04, 0.085), (0.1, 0.17, 0.06)), BLUE, "body")          # fills the creases between feet and belly
    k.add("hind", ell((0.068, 0.22, 0.085), (0.07, 0.11, 0.08), rot=(12, 0, -6)), BLUE, "body", mirror=True)
    # The tail: a paddle rising up and back and a little out to +X, narrow at its root
    paddle = ell((0, 0, 0), (0.046, 0.032, 0.14), deform=taper(0.32, 1.0, 0.7))
    k.add("tail", (along(paddle[0], (0.035, 0.235, 0.39), unit(0.22, 0.42, 0.88), (0, 1, 0)), paddle[1]), BLUE, "tail", fuse=False)
    # A smooth flat spot under each eye, so the large patch lies without wrapping
    at = HC + EYE_DIR * np.array(HR) * 0.98
    flat = ell((0, 0, 0), (0.05, 0.056, 0.024))
    k.add("face", (along(flat[0], at - EYE_DIR * 0.029, EYE_DIR), flat[1]), PINK, "head", mirror=True)

    # The underside blue, its edge crisp: below the line on the body alone (the feet and bill are blue already)
    k.paint(lambda P: inside(P[:, 2] - BELLY(P[:, 1]), 0.003) * inside(P[:, 2] - 0.3, 0.003), BLUE)

    k.build(voxel=0.0045, smooth=4, band=0.006)
    k.eye_states(lambda state: eye(k, state, at, EYE_DIR))
    k.export(out)
