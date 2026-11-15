# Kingler sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/kingler.py -- <the export's Kingler.glb> <out.glb>
#
# Krabby grown (krabby.py's frame, by sibling): the same low red-orange dome on thin pale knobbed legs, the same bumps
# for the eyes. From the references: a crown of seven orange spikes fanned over the top; the cream of the underside
# comes up high across the front as a jaw whose edge is a row of sharp teeth just under the eyes, with a big pale tusk
# rising at each corner; the eyes narrowed and mean, the lid slanting down toward the middle, the pupils toward the
# nose. One enormous pincer on the right of the picture (+X), a tall rounded C as big as the body, its dark hollow low
# on its inner side; a small one on the other side.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, tube, srgb, unit, eye_look, args, hull, polygon, sibling

kr = sibling("krabby")
ORANGE, CREAM, INK = kr.ORANGE, kr.CREAM, kr.INK

# The teeth of the jaw: (|x|, rise, half width) points along a line high across the front
TEETH = [(0.0, 0.026, 0.014), (0.028, 0.03, 0.014), (0.056, 0.03, 0.014), (0.084, 0.028, 0.014), (0.112, 0.02, 0.014)]


def crown_spikes(k, n=7):
    base = np.array([0.0, -0.01, 0.21])
    for i, a in enumerate(np.linspace(-62, 62, n)):
        t = math.radians(a)
        d = unit(math.sin(t), 0.28, math.cos(t))
        L = 0.085 - 0.012 * abs(math.sin(t))
        k.add(f"spike{i}", tube([base + d * 0.02, base + d * (0.02 + L * 0.55), base + d * (0.02 + L)],
                                [0.017, 0.012, 0.0055]), ORANGE, "body")


def jaw_outline(n=7, top=0.72, depth=0.5):
    """The jaw's patch: a flat bottom and a row of n sharp teeth along its top (u, v in the patch's frame)."""
    pts = [(-1.0, -1.0), (1.0, -1.0)]
    xs = np.linspace(1.0, -1.0, 2 * n + 1)
    for i, x in enumerate(xs):
        y = top if i % 2 == 1 else top - depth
        pts.append((x, y - 0.12 * x * x))
    return polygon(pts)


def jaw(k):
    """The cream jaw's toothed edge across the front: a patch, so the teeth keep their edges where paint is ragged."""
    k.decal((0.0, -0.118, 0.143), unit(0, -1, 0.12), (0.118, 0.05), CREAM, eps=0.004, outline=jaw_outline(),
            rings=14, segs=160)


def tusks(k):
    for s, sn in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        root = np.array([0.118, -0.08, 0.135])
        tip = np.array([0.15, -0.11, 0.217])
        pts = [root + (0.018, 0, 0), root + (-0.014, 0, 0), root + (0, -0.018, 0), root + (0, 0.016, 0),
               root + (0.004, -0.004, -0.012), tip]
        k.add("tusk" + sn, hull([np.array(p) * m for p in pts]), CREAM, "body", fuse=False, surface="shell")


def big_arm(side):
    if side > 0:
        return [(0.125, -0.04, 0.155), (0.185, -0.07, 0.17), (0.215, -0.09, 0.2)]
    return [(0.125, -0.04, 0.155), (0.18, -0.065, 0.19), (0.195, -0.08, 0.235)]


BIG_CLAW = dict(centre=(0.222, -0.095, 0.37), rx=0.096, rz=0.142, r=0.08, opening=218.0, gap=34.0, depth=0.08)
SMALL_CLAW = dict(centre=(0.2, -0.085, 0.315), rx=0.058, rz=0.068, r=0.04, gap=40.0)

LEGS = [
    ("legfront", (0.125, -0.045, 0.095), (0.19, -0.083, 0.132), (0.218, -0.11, 0.008)),
    ("leghind", (0.125, 0.045, 0.095), (0.19, 0.078, 0.132), (0.218, 0.105, 0.008)),
]

# ---- the eyes: narrowed under a lid slanting down to the nose, cut under by the teeth -------------------------
CO, CW, CK, CP = srgb(ORANGE), srgb(kr.WHITE), srgb(INK), srgb(kr.PUPIL)
TOP = lambda u: 0.3 + 0.42 * u                  # the lid's cut, low toward the inner corner (u -1)
BOT = -0.5
EYE_HALF = (0.033, 0.03)


def eye(u, v):
    c = np.tile(CK, (len(u), 1))
    shape = u * u + v * v
    c[(shape < 0.72) & (v < TOP(u) - 0.17) & (v > BOT + 0.13)] = CW
    c[((u + 0.34) / 0.34) ** 2 + ((v + 0.08) / 0.4) ** 2 < 1] = CP
    return c


_t = np.linspace(0, 2 * math.pi, 96, endpoint=False)
_u, _v = 0.98 * np.cos(_t), 0.98 * np.sin(_t)
_v = np.clip(_v, BOT, None)
_v = np.minimum(_v, TOP(_u))
OUT_R = polygon(np.stack([_u, _v], axis=1))
OUT_L = polygon(np.stack([-_u, _v], axis=1))
M = np.array([-1.0, 1, 1])


def lay(k):
    def eyes(state):
        look = eye if state == "Open" else eye_look(state, eye, CO, CK, line=0.17)
        c, n = kr.eye_centre(), kr.EYE_N
        k.decal(c, n, EYE_HALF, look, eps=0.0035, rings=20, segs=56, outline=OUT_R)
        k.decal(c * M, n * M, EYE_HALF, lambda u, v: look(-u, v), eps=0.0035, rings=20, segs=56, outline=OUT_L)
    return eyes


def kingler(src, out):
    k = Kit(src, coat="shell")
    kr.body(k)
    kr.cream_line(k, front=0.135, back=0.12, fangs=())
    jaw(k)
    crown_spikes(k)
    tusks(k)
    kr.legs(k, legs=LEGS, r=(0.0225, 0.019, 0.015))
    kr.arms_and_claws(k, arm=big_arm, claws=(BIG_CLAW, SMALL_CLAW))
    k.build(voxel=0.0045, smooth=3, band=0.006)
    k.eye_states(lay(k))
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    kingler(src, out)
