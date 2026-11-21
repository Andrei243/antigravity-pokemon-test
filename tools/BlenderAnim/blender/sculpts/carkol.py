# Carkol sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/carkol.py -- <the export's Carkol.glb> <out.glb>
# Grown from rolycoly.py (sibling("rolycoly")): its coal, its blocks, its colours and its heap over a glowing core.
#
# The design, from the references: a mine cart of grey stone, a tub narrower at the foot than at its top, under a
# broad flat rim plate lighter on top; four stubby faceted rocks for legs at its corners. On the rim a great heap of
# black coal blocks, red-hot in the cracks between them, rising to a peak toward the back: the heap is the largest mass,
# about half the height. At the front of the rim, half out of the heap, the head: a dark grey rounded wedge with two
# white eyes slanting down to the nose (a dark slit in each) and a wide orange grin under them.
# The skeleton: body (0, 0, 0.15), head (0, -0.15, 0.12), legs (+-0.11, -0.1, 0.06), hinds (+-0.11, 0.1, 0.06).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, srgb, unit, boxy, eye_look, polygon, args, sibling

ro = sibling("rolycoly")
rg = ro.rg

HEAD = "#4A484E"
EYE_W, EYE_INK, EYE_SLIT = "#F4F2EE", "#1C1A20", "#4A4850"
GRIN, GRIN_HI, GRIN_RIM = "#FF7E22", "#FFB040", "#8A2416"

HEAD_C, HEAD_R = np.array([0.0, -0.19, 0.268]), (0.088, 0.08, 0.052)
EYE_C = np.array([0.04, -0.243, 0.296])
EYE_N = unit(0.35, -0.8, 0.55)
EYE_HALF = (0.03, 0.021)
EYE_PTS = [(1.0, 0.72), (-1.0, 0.05), (-0.9, -0.45), (-0.35, -0.9), (0.4, -0.88), (0.92, -0.35)]
GRIN_C = np.array([0.0, -0.266, 0.255])
GRIN_N = unit(0, -1, -0.15)
GRIN_HALF = (0.058, 0.024)


def ring(z, r, n, seed, jit=0.06, turn=0.0):
    rs = np.random.RandomState(seed)
    return [(r[0] * (1 + jit * (rs.rand() * 2 - 1)) * math.cos(2 * math.pi * i / n + turn),
             r[1] * (1 + jit * (rs.rand() * 2 - 1)) * math.sin(2 * math.pi * i / n + turn), z) for i in range(n)]


def eye_open(u, v):
    out_r = polygon(EYE_PTS)
    rr = np.sqrt(u * u + v * v)
    lim = np.array([out_r(math.atan2(b, a)) for a, b in zip(u, v)])
    c = np.tile(srgb(EYE_W), (len(u), 1))
    c[((u + 0.3) / 0.16) ** 2 + ((v + 0.12) / 0.55) ** 2 < 1] = srgb(EYE_SLIT)
    c[rr > lim * 0.8] = srgb(EYE_INK)
    return c


def grin(u, v):
    c = np.tile(srgb(HEAD), (len(u), 1))
    mid = 0.62 * u * u - 0.25
    inner = (np.abs(v - mid) < 0.42) & (np.abs(u) < 0.92)
    c[inner] = srgb(GRIN)
    c[inner & (v > mid + 0.08)] = srgb(GRIN_HI)
    c[(np.abs(v - mid) >= 0.42) & (np.abs(v - mid) < 0.6) & (np.abs(u) < 0.97)] = srgb(GRIN_RIM)
    return c


def carkol(src, out):
    k = Kit(src, coat="shell")
    lot = ro.Lot()
    # four stubby faceted rocks for legs at the cart's corners
    for x, y, bone, seed in ((0.125, -0.1, "legR", 1), (-0.125, -0.1, "legL", 2), (0.125, 0.105, "hindR", 3),
                             (-0.125, 0.105, "hindL", 4)):
        lot.put(ro.knob((x * 1.02, y * 1.05, 0.049), 0.046, 60 + seed), ro.STONE_DK, bone)
    # the tub: narrower at its foot, under a broad flat rim lighter on top
    lot.put(hull(ring(0.06, (0.13, 0.135), 9, 7) + ring(0.2, (0.188, 0.195), 9, 8, turn=0.2)), ro.STONE, "body")
    lot.put(hull(ring(0.196, (0.212, 0.218), 11, 9, jit=0.05) + ring(0.236, (0.226, 0.232), 11, 10, jit=0.05, turn=0.1)),
            ro.STONE_LT, "body")
    # a few small points standing out of the rim's edge
    for i, th in enumerate((0.6, 1.4, 2.5, 3.6, 4.4, 5.5)):
        d = np.array([math.cos(th), math.sin(th), 0])
        b = d * 0.218 + np.array([0, 0, 0.214])
        a1, _ = rg.axes(d)
        lot.put(hull([b + a1 * 0.016, b - a1 * 0.016, b + np.array([0, 0, 0.012]), b - np.array([0, 0, 0.012]),
                      b + d * 0.026]), ro.STONE, "body")
    # the heap of red-hot coal, peaked toward the back, bare where the head comes out at the front
    ro.heap(k, lot, (0.0, 0.035, 0.232), 0.185, (0.0, 0.09, 0.55), 120, 0.037, 17, "body", fat=0.95,
            clear=((0, -1, 0), 30, 0.25))
    lot.flush(k, "coal")
    # the head: a dark rounded wedge, half out of the heap over the rim's front
    def wedge(V):
        V = boxy(2.6)(V)
        V[:, 0] *= 1 + 0.18 * V[:, 1]           # narrower toward the nose
        V[:, 2] *= 1 + 0.12 * V[:, 1]
        return V
    k.add("head", ell(HEAD_C, HEAD_R, rot=(-6, 0, 0), deform=wedge), HEAD, "head")
    k.add("brow", ell(HEAD_C + np.array([0, 0.01, 0.022]), (0.07, 0.06, 0.03), deform=boxy(2.4)), HEAD, "head")
    k.decal(GRIN_C, GRIN_N, GRIN_HALF, grin, up=(0, 0, 1), eps=0.0025, rings=12, segs=56)
    k.build(voxel=0.0035, smooth=3)
    def lay(state):
        # two patches by hand: the outline is not symmetric, and a mirrored patch mirrors only its colours
        look = eye_look(state, eye_open, srgb(HEAD), srgb(EYE_INK))
        k.decal(EYE_C, EYE_N, EYE_HALF, look, up=(0, 0, 1), eps=0.0022, rings=20, segs=56, outline=polygon(EYE_PTS))
        m = np.array([-1, 1, 1.0])
        k.decal(EYE_C * m, EYE_N * m, EYE_HALF, lambda u, v: look(-u, v), up=(0, 0, 1), eps=0.0022, rings=20,
                segs=56, outline=polygon([(-a, b) for a, b in EYE_PTS]))
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    carkol(src, out)
