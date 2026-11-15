# Kingdra sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/kingdra.py -- <the export's Kingdra.glb> <out.glb>
#
# Seadra grown (horsea.py's frame and seadra.py's eye, by sibling: the head, the tube of a snout open at its end, the
# body, the coil of a tail and the belly crossed by lines). From the references: a large blue sea horse floating
# upright; a long snout tapering from a thick root to a small round mouth at its end; a pointed crest at the back of the
# head; branching blue antlers like coral on top of the head; a small white fan of a fin behind each jaw; narrowed
# angry eyes, a red iris under a heavy lid slanting down toward the snout; a plump body whose front is a golden yellow
# belly crossed by darker lines; on its back two branching blue stalks with a pale grey fin stretched between them; the
# tail curled under it in a big thick coil. (The pictures' belly is golden yellow, not cream.)
# Bones: body (0, 0, 0.531), tail (0, 0.02, 0.391), fin (0, 0.08, 0.551), head (0, 0, 0.651).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, eye_look, args, hull, slab, polygon, sibling

h = sibling("horsea")
BLUE, GOLD, LINE, FIN, WHITEFIN = "#5AB2E2", "#F2B840", "#B8802E", "#DCDEE4", "#EEF0F2"
IRIS = "#D4283A"

HEAD_C, HEAD_R = np.array([0.0, 0.015, 0.665]), np.array([0.112, 0.132, 0.108])
SNOUT = [(0.0, -0.09, 0.655), (0.0, -0.2, 0.646), (0.0, -0.3, 0.639), (0.0, -0.37, 0.636)]
SNOUT_R = [0.056, 0.038, 0.027, 0.023]
TIP_R = (0.031, 0.018, 0.031)
SPINE = [(0.0, 0.05, 0.6), (0.0, 0.03, 0.53), (0.0, 0.015, 0.45), (0.0, 0.03, 0.34), (0.0, 0.05, 0.27)]
SPINE_R = [0.078, 0.095, 0.112, 0.105, 0.085]
BELLY_C, BELLY_R = np.array([0.0, -0.03, 0.43]), np.array([0.112, 0.105, 0.15])
COIL = dict(centre=np.array([0.0, 0.0, 0.2]), a0=58.0, turns=1.35, r0=0.13, r1=0.03, t0=0.08, t1=0.026, turn=32.0)


def crest(k):
    """The pointed crest at the back of the head, back and up: a solid blade with width, notched into two points."""
    for i, (tip, w) in enumerate((((0.0, 0.2, 0.75), 0.045), ((0.0, 0.215, 0.69), 0.035))):
        root = np.array([0.0, 0.07, 0.71 - i * 0.05])
        tip = np.array(tip)
        pts = [root + (w * 0.6, 0, 0), root - (w * 0.6, 0, 0), root + (0, -0.02, w), root + (0, -0.02, -w * 0.7),
               tip]
        k.add(f"crest{i}", hull(pts), BLUE, "head", fuse=False, surface="scales")


def branch(k, name, root, pts, r, bone):
    """A blue stalk like coral through points, its radius tapering to r[1] (never under 0.015)."""
    P = [np.array(root)] + [np.array(p) for p in pts]
    radii = np.linspace(r[0], r[1], len(P))
    k.add(name, tube(P, radii, seg=14, per=5), BLUE, bone)


def antlers(k):
    """Branching antlers on top of the head, a pair, each a stalk up and a little out with a branch back and out."""
    for s, sn in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        branch(k, "antler" + sn, np.array([0.03, -0.01, 0.74]) * m,
               [np.array(p) * m for p in ((0.045, -0.015, 0.8), (0.06, -0.005, 0.86), (0.075, 0.01, 0.9))],
               (0.022, 0.016), "head")
        branch(k, "antlerb" + sn, np.array([0.05, -0.008, 0.81]) * m,
               [np.array(p) * m for p in ((0.085, 0.03, 0.835), (0.12, 0.07, 0.86))], (0.017, 0.015), "head")


def cheek_fins(k):
    """A small white fan behind each jaw, turned out so the front cameras see its face."""
    fan = [(0.0, -0.012), (0.05, -0.035), (0.085, -0.03), (0.095, -0.005), (0.09, 0.02), (0.06, 0.035),
           (0.0, 0.014)]
    o = np.array([0.075, 0.055, 0.625])
    k.add("cheekfin", slab(fan, o, unit(0.55, 0.8, -0.1), unit(0.1, 0.0, 1.0), 0.016), WHITEFIN, "head",
          fuse=False, mirror=True, surface="shell")


def back_fin(k):
    """Two branching stalks up from the back, back and out toward +X, a pale fin stretched between them."""
    o = np.array([0.03, 0.1, 0.58])
    ex = unit(0.3, 0.25, 0.92)                       # up the front stalk, a little back and out
    ez = unit(0.6, 0.8, 0.0)                         # across, back and out toward +X (the back camera sees it)
    ez = ez - ex * (ez @ ex); ez /= np.linalg.norm(ez)
    P = lambda a, b: o + ex * a + ez * b
    fin = [(0.02, 0.005), (0.3, 0.0), (0.29, 0.06), (0.28, 0.12), (0.27, 0.17), (0.12, 0.12), (-0.03, 0.07)]
    k.add("backfin", slab(fin, o, ex, ez, 0.014), FIN, "fin", fuse=False, surface="shell")
    branch(k, "stalka", P(-0.02, 0.0), [P(0.16, -0.004), P(0.33, -0.008)], (0.022, 0.016), "fin")
    branch(k, "stalkab", P(0.22, -0.006), [P(0.27, -0.05), P(0.3, -0.085)], (0.017, 0.015), "fin")
    branch(k, "stalkb", P(-0.04, 0.07), [P(0.12, 0.12), P(0.28, 0.17)], (0.021, 0.016), "fin")
    branch(k, "stalkbb", P(0.21, 0.15), [P(0.25, 0.2), P(0.28, 0.23)], (0.017, 0.015), "fin")


# ---- the eyes: narrowed under a heavy lid slanting down toward the snout, a red iris, a dark pupil ------------------
CB, CW, CK, CI, CP = srgb(BLUE), srgb(h.WHITE), srgb(h.INK), srgb(IRIS), srgb("#1A1018")
TOP = lambda u: 0.14 + 0.42 * u                 # +u runs toward the back; the lid low toward the snout (u -1)
BOT = -0.62
EYE_N = unit(0.7, -0.64, 0.25)
EYE_HALF = (0.05, 0.046)


def eye(u, v):
    c = np.tile(CK, (len(u), 1))
    white = (u * u + v * v < 0.74) & (v < TOP(u) - 0.18) & (v > BOT + 0.15)
    c[white] = CW
    c[white & (((u + 0.22) / 0.4) ** 2 + ((v + 0.1) / 0.46) ** 2 < 1)] = CI
    c[white & (((u + 0.24) / 0.16) ** 2 + ((v + 0.12) / 0.26) ** 2 < 1)] = CP
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
        # laid by hand a side, since a mirrored patch's outline is not mirrored (seadra.py)
        k.decal(c, EYE_N, EYE_HALF, look, eps=0.0035, rings=20, segs=56, outline=polygon(OUT))
        k.decal(c * M, EYE_N * M, EYE_HALF, lambda u, v: look(-u, v), eps=0.0035, rings=20, segs=56,
                outline=polygon(OUT * np.array([-1, 1])))
    return eyes


def kingdra(src, out):
    k = Kit(src, coat="scales")
    h.head(k, HEAD_C, HEAD_R, BLUE)
    k.add("cheekflat", ell(HEAD_C + EYE_N * HEAD_R * 0.7, (0.05, 0.05, 0.05)), BLUE, "head", mirror=True)
    h.snout(k, SNOUT, SNOUT_R, TIP_R, BLUE, hole_half=(0.018, 0.018))
    crest(k)
    antlers(k)
    cheek_fins(k)
    h.torso(k, SPINE, SPINE_R, BELLY_C, BELLY_R, BLUE)
    h.coil(k, start=SPINE[-1], color=BLUE, **COIL)
    h.belly(k, z0=0.3, z1=0.56, front=-0.035, color=GOLD, ridge=LINE, rows=(0.34, 0.39, 0.44, 0.49, 0.535),
            half=(0.075, 0.0045))
    back_fin(k)
    k.build(voxel=0.0055, smooth=3, band=0.006)
    k.eye_states(lay(k))
    k.export(out)


if __name__ == "__main__":
    kingdra(*args())
