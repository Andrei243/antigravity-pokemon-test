# Milcery sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/milcery.py -- <the export's Milcery.glb> <out.glb>
#
# The design, from the references: a splash of cream frozen as it leaps: a soft round blob a little wider below, a
# crown of thick prongs with round drops at their tips rising from its top (two to the viewer's left, a tall one in the
# middle, two to the right, the small ones set a little back), a prong straight out and a flattened lobe out and down
# on its -X side, a longer flattened lobe up and out on +X like a raised arm, and one round drop flying free above its
# -X side. Its face low on its front: two upright oval eyes, white and glossy in a tan rim, and a tiny tan smile.
# (The task's text spoke of a drop of cream with drips floating round it: the pictures show a splash with a crown of
# prongs and a single free drop.)
# The skeleton: body (0, 0, 0.101) alone; everything is the body's.
# Written to be grown from: Alcremie keeps its cream, its tan ink and its way of making the face.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, outward, args

# --- the line's colours -------------------------------------------------------------------------------------------
CREAM, SHADE, INK = "#F4EBC4", "#E3D49E", "#A9814F"
WHITE = np.array([1.0, 1.0, 1.0])

# --- the frame ----------------------------------------------------------------------------------------------------
C = np.array([0.0, 0.0, 0.104])
R = np.array([0.1, 0.082, 0.08])
EYE_X, EYE_Z, EYE_HALF = 0.043, 0.09, (0.013, 0.021)
MOUTH_Z = 0.07
VOXEL = 0.0024
# The prongs: (where the root is, as an angle in the upright plane from straight up, +X positive; the way it
# reaches, the same; length past the surface; radius; depth back; flattened into a lobe)
PRONGS = [(-46, -50, 0.072, 0.025, 0.006, False), (-16, -16, 0.046, 0.02, 0.03, False),
          (4, 2, 0.078, 0.026, 0.004, False), (24, 26, 0.048, 0.02, 0.03, False), (46, 50, 0.066, 0.024, 0.008, False),
          (-78, -80, 0.058, 0.022, 0.01, False), (-108, -122, 0.056, 0.027, 0.0, True),
          (84, 46, 0.074, 0.028, -0.004, True)]


def on_ell(c, r, d):
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def droop(V):
    """The blob a little wider and flatter below, as cream settles."""
    V = V.copy()
    s = 1 + 0.1 * np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= s; V[:, 1] *= s
    return V


def gloss_eye(ink=INK, shade=SHADE):
    """A glossy upright eye: white, a pale shade in its lower part, a rim of ink a fifth of its half size."""
    k_, s_ = srgb(ink), srgb(shade)
    def look(u, v):
        c = np.tile(WHITE, (len(u), 1))
        c[((u + 0.05) / 0.62) ** 2 + ((v + 0.62) / 0.4) ** 2 < 1] = s_
        c[np.hypot(u, v) > 0.78] = k_
        return c
    return look


def lay_eyes(k, state, c, r, x, z, half, lid=CREAM, ink=INK, look=None):
    for s in (1, -1):
        p, n = on_ell(c, r, ((x * s) / r[0] * 1.15, -1.0, (z - c[2]) / r[2]))
        f = eye_look(state, look or gloss_eye(ink), srgb(lid), srgb(ink), line=0.17)
        k.decal(p, n, half, f if s > 0 else (lambda u, v, g=f: g(-u, v)), eps=0.0018, rings=20, segs=56)


def lay_mouth(k, c, r, z, half, ground=CREAM, ink=INK):
    g, i_ = srgb(ground), srgb(ink)
    def smile(u, v):
        col = np.tile(g, (len(u), 1))
        line = -0.25 + 0.6 * u * u
        col[(np.abs(v - line) < 0.28) & (np.abs(u) < 0.9)] = i_
        return col
    p, n = on_ell(c, r, (0.0, -1.0, (z - c[2]) / r[2]))
    k.decal(p, n, half, smile, eps=0.0018, rings=10, segs=56)


def splash(k):
    k.add("blob", ell(C, R, deform=droop), CREAM, "body")
    for i, (at, ang, length, rad, back, flat) in enumerate(PRONGS):
        a, b = math.radians(at), math.radians(ang)
        r0 = np.array([math.sin(a), back * 4, math.cos(a)]); r0 /= np.linalg.norm(r0)
        d = np.array([math.sin(b), back * 4, math.cos(b)]); d /= np.linalg.norm(d)
        root, _ = on_ell(C, R, r0)
        tip = root + d * length
        if flat:
            # a paddle flattened front to back, its broad face to the front, its root sunk in the blob
            mid = root + d * length * 0.42
            k.add(f"lobe{i}", ell(mid, (rad * 1.2, rad * 0.66, length * 0.72), rot=(0, ang, 0)), CREAM, "body")
        else:
            # thick, flared where it leaves the blob, a little waisted, a round drop at its tip
            k.add(f"prong{i}", tube([root - d * 0.03, root + d * 0.006, root + d * length * 0.55, tip],
                                    [rad * 1.45, rad * 1.12, rad * 0.74, rad * 0.8]), CREAM, "body")
            k.add(f"drop{i}", ell(tip, (rad * 1.08, rad * 1.08, rad * 1.08), sub=3), CREAM, "body")
    # the drop flying free above its -X side
    k.add("free", outward(ell((-0.152, 0.012, 0.212), (0.014, 0.014, 0.014), sub=3)), CREAM, "body", fuse=False)
    # a soft shade under the blob and in the crown's crevices behind
    k.paint(lambda P: smoothstep(0.055, 0.02, P[:, 2]) * 0.7, SHADE)


def milcery(src, dst):
    k = Kit(src, coat="fur")
    splash(k)
    lay_mouth(k, C, R, MOUTH_Z, (0.009, 0.005))
    k.build(voxel=VOXEL, smooth=4, band=0.004, wband=0.03)
    k.eye_states(lambda st: lay_eyes(k, st, C, R, EYE_X, EYE_Z, EYE_HALF))
    k.export(dst)


if __name__ == "__main__":
    milcery(*args())
