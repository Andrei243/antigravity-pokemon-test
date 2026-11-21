# Swirlix sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/swirlix.py -- <the export's Swirlix.glb> <out.glb>
#
# From the references: a hovering ball of white cotton candy made of round puffs, its lower part blushed pink; two
# pink-tipped puffs high on its sides like ears; a smooth face at its front: two upright pink eyes (a pink iris with a
# deeper upright pupil, a white glint, a dark pink rim), a tiny pink nose, a wavy little mouth and a pink tongue
# hanging out of its right corner (the viewer's right); under it behind, a short curled tail of spun sugar, pinker at
# its end.
#
# Written to be grown from (slurpuff.py imports it with sibling("swirlix")): the eye, the mouth, the tongue and the
# puffs are the functions below; the calls that build Swirlix are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, lumpy, eye_look, args

# --- the line's colours -------------------------------------------------------------------------------------------
WHITE_C = "#FBF6F8"         # the cotton, the cream
BLUSH = "#F1C6D6"
PINK = "#EE9CBA"
TONGUE = "#E2557E"
EYE_PINK, EYE_DEEP = srgb("#DE5C88"), srgb("#A0254E")
EYE_RIM = srgb("#8E1E44")
MOUTH = srgb("#9A3050")
WHITE = np.array([1.0, 1.0, 1.0])

# --- Swirlix's frame (joints: body (0, 0, 0.1), tail (0, 0.05, 0.07); the export stands 0.035 to 0.21) -------------
C = np.array([0.0, 0.006, 0.128])
R = np.array([0.08, 0.072, 0.076])
FC = np.array([0.0, -0.026, 0.13])       # the smooth face at the front
FR = np.array([0.062, 0.05, 0.05])
EYE_D = (0.5, -0.86, 0.3)
EYE_HALF = (0.0135, 0.017)


def on_ell(c, r, d):
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def pink_eye(fill=EYE_PINK, deep=EYE_DEEP, rim=EYE_RIM):
    """The line's upright eye: a pink iris, a deeper upright pupil, a white glint high and out, a dark pink rim."""
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(fill, (len(u), 1))
        c[(u / 0.36) ** 2 + ((v + 0.04) / 0.62) ** 2 < 1] = deep
        c[((u - 0.12) / 0.22) ** 2 + ((v - 0.34) / 0.24) ** 2 < 1] = WHITE
        c[r > 0.78] = rim
        return c
    return look


def lay_eyes(k, c, r, d, half, lid, state, look=None, eps=0.003):
    p, nrm = on_ell(c, r, d)
    k.decal(p, nrm, half, eye_look(state, look or pink_eye(), srgb(lid) if isinstance(lid, str) else lid, EYE_RIM,
                                   line=0.16), eps=eps, rings=20, segs=56, mirror=True)


def wavy_mouth(ground):
    """A small wavy mouth, three soft bumps, as one dark line."""
    g = srgb(ground) if isinstance(ground, str) else ground
    def look(u, v):
        c = np.tile(g, (len(u), 1))
        line = 0.12 * np.cos(u * 3 * math.pi) - 0.05
        c[(np.abs(v - line) < 0.22) & (np.abs(u) < 0.94)] = MOUTH
        return c
    return look


def lay_mouth(k, c, r, d, half, ground, eps=0.0025):
    p, nrm = on_ell(c, r, d)
    k.decal(p, nrm, half, wavy_mouth(ground), eps=eps, rings=12, segs=72)


def lay_nose(k, c, r, d, half, eps=0.0025):
    p, nrm = on_ell(c, r, d)
    k.decal(p, nrm, half, EYE_PINK, eps=eps, rings=6, segs=32)


def tongue(k, at, size, bone, color=TONGUE):
    """The tongue hanging out of the mouth's right corner: a flat rounded lobe, loose so its edge stays crisp."""
    at = np.array(at, float)
    k.add("tongue", ell(at, size, rot=(-20, 0, 15)), color, bone, fuse=False)
    k.add("tongueroot", ell(at + np.array([-0.002, 0.006, size[2] * 0.6]), (size[0] * 0.8, size[1] * 2, size[2] * 0.5)),
          color, bone, fuse=False)


def puffs(k, name, c, r, bone, n, zs, scale, color, seed, avoid=None):
    """Round puffs about an ellipsoid at the heights zs (shares of r up), jittered; none where avoid(point) holds."""
    c, r = np.array(c, float), np.array(r, float)
    rs = np.random.RandomState(seed)
    i = 0
    for z in zs:
        for j in range(n):
            a = 2 * math.pi * (j + rs.rand() * 0.5) / n
            q = math.sqrt(max(0.0, 1 - z * z))
            d = np.array([math.cos(a) * q, math.sin(a) * q, z])
            p = c + d * r * 0.86
            if avoid is not None and avoid(p):
                continue
            s = scale * rs.uniform(0.85, 1.15)
            k.add(f"{name}{i}", ell(p, (s, s, s * 0.92), sub=3), color, bone)
            i += 1


def swirlix(src, out):
    k = Kit(src, coat="fur")
    k.add("body", ell(C, R, deform=lumpy(4, 3, 0.04)), WHITE_C, "body")
    k.add("face", ell(FC, FR), WHITE_C, "body")
    def avoid(p):
        return p[1] < -0.02 and abs(p[0]) < 0.06 and 0.09 < p[2] < 0.175
    puffs(k, "puff", C, R, "body", 9, (-0.55, -0.1, 0.35), 0.034, WHITE_C, 3, avoid)
    for i, (x, y, z, s) in enumerate(((0.02, 0.01, 0.19, 0.032), (-0.024, 0.02, 0.188, 0.03), (0.0, -0.022, 0.184, 0.028),
                                      (0.0, 0.04, 0.18, 0.03))):
        k.add(f"top{i}", ell((x, y, z), (s, s, s * 0.9), sub=3), WHITE_C, "body")
    # the pink-tipped puffs high on the sides, like ears
    k.add("ear", ell((0.064, 0.008, 0.18), (0.028, 0.026, 0.026), sub=3), WHITE_C, "body", mirror=True)
    k.paint(lambda P: inside(np.linalg.norm((np.abs(P) - np.array([0.082, 0.0, 0.19])) * np.array([1, 0.9, 1]), axis=1)
                             - 0.022, 0.004) * (P[:, 2] > 0.16), PINK)
    # the tail: a short curl of spun sugar under it behind, pinker at its end
    k.add("tail", tube([(0.0, 0.05, 0.078), (0.006, 0.07, 0.058), (0.02, 0.078, 0.04), (0.036, 0.07, 0.03)],
                       [0.016, 0.015, 0.013, 0.01]), BLUSH, "tail")
    k.paint(lambda P: inside(np.linalg.norm(P - np.array([0.03, 0.074, 0.034]), axis=1) - 0.02, 0.006), PINK, on=["tail"])
    # the lower part blushed
    k.paint(lambda P: inside(P[:, 2] - 0.085, 0.016), BLUSH, on=["body", "puff", "face"])
    # the face
    lay_mouth(k, FC, FR, (0.0, -1.0, -0.28), (0.022, 0.009), WHITE_C)
    lay_nose(k, FC, FR, (0.0, -1.0, 0.1), (0.0045, 0.0035))
    tongue(k, (0.016, -0.074, 0.108), (0.009, 0.004, 0.011), "body")
    k.build(voxel=0.0024, smooth=5, band=0.004, wband=0.025)

    def eyes(state):
        lay_eyes(k, FC, FR, EYE_D, EYE_HALF, WHITE_C, state)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    swirlix(*args())
