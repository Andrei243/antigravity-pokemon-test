# Seadra sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/seadra.py -- <the export's Seadra.glb> <out.glb>
#
# Horsea grown (horsea.py's frame, by sibling): the same light blue, the same tube of a snout open at its flared end,
# the same cream belly crossed by lines and the same thick coil of a tail, all larger, the snout longer, the coil much
# bigger. From the references: the head bristles with sharp blue spikes, a crest of them over the top and back and a
# pair of blades out of each cheek; two grey-tan spines stand behind the crest; from the back of the neck a great fin
# like a wing on each side, three long sharp points, blue at the root and cream toward the tips; the eyes narrowed and
# mean under a heavy lid slanting down toward the snout, a dark pupil in the white.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, unit, eye_look, args, hull, slab, polygon, sibling

h = sibling("horsea")
BLUE, CREAM, RIDGE, SPINE_GREY = "#6CB8E4", "#EAD6A0", "#BFA068", "#A89A8C"
TIP = "#F0E0A4"

HEAD_C, HEAD_R = np.array([0.0, -0.005, 0.695]), np.array([0.11, 0.13, 0.104])
SNOUT = [(0.0, -0.08, 0.682), (0.0, -0.2, 0.676), (0.0, -0.3, 0.672)]
SNOUT_R = [0.042, 0.037, 0.036]
TIP_R = (0.052, 0.028, 0.052)
SPINE = [(0.0, 0.035, 0.64), (0.0, 0.012, 0.555), (0.0, -0.004, 0.47), (0.0, 0.016, 0.38)]
SPINE_R = [0.072, 0.078, 0.076, 0.066]
BELLY_C, BELLY_R = np.array([0.0, -0.026, 0.5]), np.array([0.066, 0.056, 0.105])
COIL = dict(centre=np.array([0.0, -0.03, 0.246]), a0=58.0, turns=1.32, r0=0.118, r1=0.03, t0=0.066, t1=0.022,
            turn=32.0)


def blade3(root, tip, w, up=(0, 0, 1.0)):
    """A sharp flat-faced spike: a kite of a root and its point, thick across."""
    root, tip = np.array(root, float), np.array(tip, float)
    d = tip - root; d /= np.linalg.norm(d)
    a = np.cross(d, up); a /= np.linalg.norm(a) if np.linalg.norm(a) > 1e-6 else 1
    b = np.cross(a, d)
    return hull([root + b * w, root - b * w * 0.6, root + a * w * 0.42, root - a * w * 0.42,
                 root - d * w * 0.4, tip])


def crest(k):
    # Over the top and back of the head: three sharp blue spikes laid back
    for i, (r, t, w) in enumerate([((0.0, -0.01, 0.765), (0.0, 0.07, 0.86), 0.04),
                                    ((0.0, 0.06, 0.74), (0.0, 0.19, 0.79), 0.04),
                                    ((0.0, 0.085, 0.69), (0.0, 0.22, 0.675), 0.036)]):
        k.add(f"crest{i}", blade3(r, t, w, up=(1, 0, 0)), BLUE, "head", fuse=False, surface="scales")
    # Two grey-tan spines behind the crest, leaning a little apart
    for i, (r, t) in enumerate([((0.018, 0.04, 0.765), (0.04, 0.12, 0.845)), ((-0.018, 0.08, 0.735), (-0.04, 0.17, 0.79))]):
        k.add(f"greyspine{i}", blade3(r, t, 0.026, up=(1, 0, 0)), SPINE_GREY, "head", fuse=False, surface="scales")


def cheek_blades(k):
    for s, sn in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        for i, (r, t, w) in enumerate([((0.07, 0.02, 0.65), (0.165, 0.1, 0.6), 0.034),
                                        ((0.07, 0.05, 0.715), (0.16, 0.14, 0.75), 0.03)]):
            k.add(f"cheek{sn}{i}", blade3(np.array(r) * m, np.array(t) * m, w, up=(0, 1, 0)), BLUE, "head",
                  fuse=False, surface="scales")


# The wing fin: three long sharp points from a root at the neck (in its own plane: a back along the fin, b up)
WING = [(0.0, 0.045), (0.08, 0.075), (0.2, 0.2), (0.12, 0.05), (0.27, 0.035), (0.13, -0.02), (0.19, -0.12),
        (0.06, -0.05), (0.0, -0.045)]
WING_ROOT = [(0.0, 0.04), (0.07, 0.06), (0.1, 0.08), (0.095, 0.02), (0.1, -0.035), (0.07, -0.05), (0.0, -0.04)]


def wings(k):
    for s, sn in ((1, "R"), (-1, "L")):
        d = unit(0.72 * s, 0.69, 0.06)
        up = unit(0.12 * s, 0.0, 1.0)
        o = np.array([0.045 * s, 0.075, 0.6])
        k.add("wing" + sn, slab(WING, o, d, up, 0.012), TIP, "fin", fuse=False, surface="scales")
        k.add("wingroot" + sn, slab(WING_ROOT, o, d, up, 0.02), BLUE, "fin", fuse=False, surface="scales")


# ---- the eyes: narrowed under a heavy lid slanting down toward the snout ------------------------------------------
CB, CW, CK, CP = srgb(BLUE), srgb(h.WHITE), srgb(h.INK), srgb("#2A1C1C")
TOP = lambda u: 0.12 + 0.42 * u                 # +u runs toward the back; the lid low toward the snout (u -1)
BOT = -0.62
EYE_N = unit(0.76, -0.58, 0.2)
EYE_HALF = (0.042, 0.04)


def eye(u, v):
    c = np.tile(CK, (len(u), 1))
    c[(u * u + v * v < 0.74) & (v < TOP(u) - 0.18) & (v > BOT + 0.15)] = CW
    c[((u + 0.28) / 0.3) ** 2 + ((v + 0.12) / 0.38) ** 2 < 1] = CP
    return c


_t = np.linspace(0, 2 * math.pi, 96, endpoint=False)
_u, _v = 0.98 * np.cos(_t), 0.98 * np.sin(_t)
_v = np.minimum(np.clip(_v, BOT, None), TOP(_u))
OUT = np.stack([_u, _v], axis=1)


def lay(k):
    c = HEAD_C + EYE_N * HEAD_R * 0.98
    M = np.array([-1.0, 1, 1])

    def eyes(state):
        look = eye if state == "Open" else eye_look(state, eye, CB, CK, line=0.17)
        # Laid by hand a side, since a mirrored patch's outline is not mirrored. On the right +u runs back; on the
        # left the frame's +u runs forward, so the left one's outline and look are turned round.
        k.decal(c, EYE_N, EYE_HALF, look, eps=0.0035, rings=20, segs=56, outline=polygon(OUT))
        k.decal(c * M, EYE_N * M, EYE_HALF, lambda u, v: look(-u, v), eps=0.0035, rings=20, segs=56,
                outline=polygon(OUT * np.array([-1, 1])))
    return eyes


def cheek_flat(k):
    p = HEAD_C + EYE_N * HEAD_R * 0.7
    k.add("cheekflat", ell(p, (0.05, 0.05, 0.05)), BLUE, "head", mirror=True)


def seadra(src, out):
    k = Kit(src, coat="scales")
    h.head(k, HEAD_C, HEAD_R, BLUE)
    cheek_flat(k)
    h.snout(k, SNOUT, SNOUT_R, TIP_R, BLUE, hole_half=(0.031, 0.032))
    crest(k)
    cheek_blades(k)
    h.torso(k, SPINE, SPINE_R, BELLY_C, BELLY_R, BLUE)
    h.coil(k, start=SPINE[-1], color=BLUE, **COIL)
    h.belly(k, z0=0.4, z1=0.6, front=-0.02, color=CREAM, ridge=RIDGE, rows=(0.43, 0.465, 0.5, 0.535, 0.57),
            half=(0.048, 0.004))
    wings(k)
    k.build(voxel=0.0055, smooth=3, band=0.006)
    k.eye_states(lay(k))
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    seadra(src, out)
