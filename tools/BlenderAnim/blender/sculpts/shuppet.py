# Shuppet sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/shuppet.py -- <the export's Shuppet.glb> <out.glb>
#
# The design, from the references: a slate blue-grey cloth ghost, a round head with a cone of a horn rising from its
# top and leaning back; below the head the cloth hangs and flares out to the sides and behind in round, wavy lobes
# (its ragged hem); two big almond eyes slanting down toward the nose, rimmed thick black, pale blue within, a
# lavender ring round a yellow pupil at the inner side; a long thin smile under them. It floats, with no limbs.
# The skeleton: body (0, 0, 0.217), tail (0, 0.08, 0.197) behind, head (0, 0, 0.297).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, polygon, args

SLATE, INK = "#646894", "#14141E"
BLUE, RING, PUPIL = "#A8DAF6", "#9A9ACC", "#F4CE3A"

# --- the frame ------------------------------------------------------------------------------------------------------
H, HR = np.array([0.0, 0.0, 0.235]), np.array([0.118, 0.112, 0.106])
EYE_ARC, EYE_Z, EYE_HALF = 0.07, 0.216, (0.052, 0.036)
VOXEL = 0.0045
# the eye's outline (u outward, v up): its inner corner low, its upper edge straight and slanting up outward
EYE_OUT = [(-1.0, -0.38), (-0.4, 0.22), (0.2, 0.68), (0.55, 0.92), (0.85, 0.75), (1.0, 0.3), (0.88, -0.2),
           (0.55, -0.55), (0.05, -0.72), (-0.5, -0.62)]


def on_ell(c, r, d):
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r)) ** 2).sum())


def head_dir(arc, z):
    th = arc / HR[0]
    return unit(math.sin(th), -math.cos(th), (z - H[2]) / HR[2] * 1.05)


def eye_open(u, v):
    r = polygon(EYE_OUT)
    rho = np.array([math.hypot(a, b) / r(math.atan2(b, a)) for a, b in zip(u, v)])
    c = np.tile(srgb(BLUE), (len(u), 1))
    c[((u + 0.22) / 0.52) ** 2 + ((v + 0.06) / 0.66) ** 2 < 1] = srgb(RING)
    c[((u + 0.24) / 0.3) ** 2 + ((v + 0.06) / 0.44) ** 2 < 1] = srgb(PUPIL)
    c[rho > 0.725] = srgb(INK)
    return c


def mouth(k):
    """A long thin smile under the eyes, one stroke."""
    def smile(u, v):
        c = np.tile(srgb(SLATE), (len(u), 1))
        line = 0.35 - 0.6 * (1 - u * u)
        c[(np.abs(v - line) < 0.26) & (np.abs(u) < 0.86)] = srgb(INK)
        return c
    d = head_dir(0.0, 0.166)
    k.decal(on_ell(H, HR, d), d, (0.058, 0.022), smile, eps=0.003, rings=12, segs=72)


def hem(k):
    """The cloth below the head: a lower body, and round lobes flaring out to the sides and behind."""
    k.add("cloth", ell((0, 0.02, 0.15), (0.1, 0.1, 0.07)), SLATE, "body")

    def skirt(V):
        """A flat bell whose edge waves in round lobes, each lobe drooping at its tip, fuller behind."""
        V = V.copy()
        th = np.arctan2(V[:, 0], -V[:, 1])
        rxy = np.hypot(V[:, 0], V[:, 1])
        wave = np.cos(7 * th + 0.4)
        s = (1 + 0.13 * wave) * (0.82 + 0.18 * (1 - np.cos(th)) / 2)
        V[:, 0] *= s; V[:, 1] *= s
        V[:, 2] -= (0.75 + 0.35 * wave) * rxy ** 3
        return V
    k.add("skirt", ell((0, 0.035, 0.125), (0.165, 0.16, 0.06), deform=skirt), SLATE, "body")
    k.add("skirtback", ell((0, 0.09, 0.105), (0.11, 0.1, 0.055), deform=skirt), SLATE, "tail")


def shuppet(src, out):
    k = Kit(src, coat="fur")
    k.add("head", ell(H, HR), SLATE, "head")
    # the horn: a cone from the top of the head, rising and leaning back
    k.add("horn", tube([(0, 0.015, 0.3), (0, 0.032, 0.36), (0, 0.05, 0.4), (0, 0.068, 0.432)],
                       [0.052, 0.034, 0.022, 0.014]), SLATE, "head")
    hem(k)
    mouth(k)
    k.build(voxel=VOXEL, smooth=3, band=0.006)

    out_r = polygon(EYE_OUT)
    out_l = polygon([(-a, b) for a, b in EYE_OUT])
    def eyes(state):
        for side in (1, -1):
            d = head_dir(EYE_ARC * side, EYE_Z)
            look = eye_look(state, eye_open, srgb(SLATE), srgb(INK), line=0.15)
            col = look if side == 1 else (lambda u, v, f=look: f(-u, v))
            k.decal(on_ell(H, HR, d), d, EYE_HALF, col, eps=0.0035, outline=out_r if side == 1 else out_l,
                    rings=20, segs=56)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    shuppet(*args())
