# Morelull sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/morelull.py -- <the export's Morelull.glb> <out.glb>
#
# From the references: a little cream bulb with a lumpy crown, two upright black ovals for eyes and a small green
# dot on each cheek; out of its crown three pale stalks to three mushroom caps: a tall bullet-shaped one in the middle,
# pink above and a purple band below with a wavy edge, and two smaller rounder ones tilted out to the sides, lavender
# above and a deeper lilac band below, wavy too; under the bulb two pale green roots curling out like little feet.
# (The task called the tallest cap spotted: the pictures have no spots, each cap two colours split by a wavy line.)
#
# Written to be grown from (shiinotic.py imports it with sibling("morelull")): the line's colours, the cream body,
# its black oval eyes and cheek dots, the wavy band painted in a cap's own frame.
# Skeleton: body (0, 0, 0.08), legs (+-0.012, 0, 0.04); no head bone, so the caps go with the body.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args

# --- the line's colours ---------------------------------------------------------------------------------------------
CREAM = "#F2EED6"
STALK = "#EEEAD4"
CHEEK = "#B9DA8E"
ROOT = "#C8E39C"
PINK_CAP, PURPLE_BAND = "#F3B6CF", "#D58ED8"
LILAC_CAP, LILAC_BAND = "#DCDDF3", "#A39ADA"
INK = srgb("#14121A")

# --- Morelull's frame (about 0.31 tall) -----------------------------------------------------------------------------
BC = np.array([0.0, 0.0, 0.08])            # the bulb's middle
BR = np.array([0.039, 0.036, 0.038])
EYE_X, EYE_Z, EYE_HALF = 0.014, 0.084, (0.0078, 0.0135)
VOXEL = 0.0028


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre, and its normal."""
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def bullet(V):
    """The unit sphere made a bullet: its lower half a cylinder closed by a flat floor."""
    V = V.copy()
    z = V[:, 2]
    rxy = np.sqrt(np.clip(1 - z * z, 0, 1))
    low = z < 0
    s = np.where(low, np.minimum(rxy / 0.3, 1.0) / np.maximum(rxy, 1e-6), 1.0)
    s = np.where(low & (rxy < 1e-6), 0.0, s)
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.where(low, np.maximum(z, -0.9), z)
    return V


def egg_cap(V):
    """A rounder cap: its lower part drawn straight and closed by a floor."""
    V = V.copy()
    z = V[:, 2]
    rxy = np.sqrt(np.clip(1 - z * z, 0, 1))
    low = z < -0.2
    target = np.where(low, np.minimum(rxy / 0.35, 0.98), rxy)
    s = target / np.maximum(rxy, 1e-6)
    s = np.where(rxy < 1e-6, 0.0, s)
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.where(low, np.maximum(z, -0.8), z)
    return V


def cap(k, name, c, r, tilt, top, band, deform, edge=-0.12, waves=5, amp=0.12, bone="body", turn=0.0):
    """A cap fused on its stalk, tilted by tilt degrees about y (+ toward +X), its lower part painted the band's colour
    up to a wavy line in the cap's own frame."""
    k.add(name, ell(c, r, rot=(0, tilt, turn), deform=deform), top, bone)
    M = np.array(Euler([0, math.radians(tilt), math.radians(turn)]).to_matrix())
    c = np.array(c, float); r = np.array(r, float)
    def band_mask(P):
        q = (P - c) @ M                         # into the cap's own frame
        th = np.arctan2(q[:, 1], q[:, 0])
        h = q[:, 2] / r[2]
        near = inside(np.linalg.norm(q / (r * 1.15), axis=1) - 1, 0.004)
        return inside(h - (edge + amp * np.sin(waves * th + 0.7)), 0.012) * near
    k.paint(band_mask, band, on=[name])


def black_oval(u, v):
    return np.tile(INK, (len(u), 1))


def lay_eyes(k, c, r, x, z, half, lid, state, look=black_oval):
    """The line's black upright ovals on the bulb's front."""
    d = ((x - c[0]) / r[0], -math.sqrt(max(1 - ((x - c[0]) / r[0]) ** 2 - ((z - c[2]) / r[2]) ** 2, 0.05)),
         (z - c[2]) / r[2])
    p, n = on_ell(c, r, d)
    k.decal(p, n, half, eye_look(state, look, srgb(lid), INK, line=0.22), eps=0.0015, rings=20, segs=56,
            mirror=True)


def cheeks(k, c, r, x, z, half):
    d = (x / r[0], -math.sqrt(max(1 - (x / r[0]) ** 2 - ((z - c[2]) / r[2]) ** 2, 0.05)), (z - c[2]) / r[2])
    p, n = on_ell(c, r, d)
    k.decal(p, n, half, CHEEK, eps=0.0015, rings=8, segs=32, mirror=True)


def morelull(src, out_path):
    k = Kit(src, coat="leaf")

    # ---- the bulb and its lumpy crown
    k.add("bulb", ell(BC, BR), CREAM, "body")
    k.add("crowntop", ell((0, 0.004, 0.116), (0.028, 0.026, 0.016)), CREAM, "body")
    for i in range(8):
        a = 2 * math.pi * (i + 0.5) / 8
        k.add(f"lump{i}", ell((0.03 * math.sin(a), -0.027 * math.cos(a) + 0.004, 0.11 + 0.004 * math.cos(2 * a)),
                              (0.0125, 0.0125, 0.012), sub=3), CREAM, "body")
    for x, y in ((0.012, -0.008), (-0.014, 0.012), (0.0, 0.02)):
        k.add("lumptop", ell((x, y, 0.124), (0.012, 0.012, 0.011), sub=3), CREAM, "body")

    # ---- the stalks and the three caps
    k.add("stalkM", tube([(0, 0.002, 0.12), (0.004, 0.0, 0.155), (0.008, -0.002, 0.192)], [0.0078, 0.0072, 0.0072]),
          STALK, "body")
    k.add("stalkS", tube([(-0.008, 0.004, 0.118), (-0.03, 0.004, 0.142), (-0.044, 0.004, 0.16)],
                         [0.0072, 0.0068, 0.0068]), STALK, "body")
    k.add("stalkS2", tube([(0.008, 0.006, 0.116), (0.03, 0.006, 0.134), (0.044, 0.006, 0.15)],
                          [0.0072, 0.0068, 0.0068]), STALK, "body")
    cap(k, "capM", (0.012, -0.002, 0.255), (0.044, 0.041, 0.066), 7, PINK_CAP, PURPLE_BAND, bullet, edge=-0.08,
        waves=6, amp=0.11)
    cap(k, "capL", (-0.06, 0.004, 0.182), (0.032, 0.03, 0.035), -34, LILAC_CAP, LILAC_BAND, egg_cap, edge=-0.12,
        waves=5, amp=0.14)
    cap(k, "capR", (0.06, 0.006, 0.17), (0.032, 0.03, 0.035), 36, LILAC_CAP, LILAC_BAND, egg_cap, edge=-0.12,
        waves=5, amp=0.14)

    # ---- the roots: two pale green feet curling out under the bulb
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        k.add("root" + side, tube([(0.006 * s, 0.0, 0.05), (0.012 * s, -0.004, 0.022), (0.026 * s, -0.008, 0.007),
                                   (0.044 * s, -0.01, 0.007), (0.052 * s, -0.01, 0.015)],
                                  [0.0068, 0.0064, 0.006, 0.0058, 0.0055]), ROOT, "leg" + side)
    k.add("rootback", tube([(0, 0.004, 0.05), (0.0, 0.02, 0.012), (0.006, 0.034, 0.006)], [0.006, 0.0058, 0.0055]),
          ROOT, "legR")

    cheeks(k, BC, BR, 0.029, 0.077, (0.0058, 0.0048))
    k.build(voxel=VOXEL, smooth=4, band=0.0035, wband=0.02)

    def eyes(state):
        lay_eyes(k, BC, BR, EYE_X, EYE_Z, EYE_HALF, CREAM, state)
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    morelull(*args())
