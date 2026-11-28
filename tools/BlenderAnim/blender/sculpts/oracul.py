# Oracul sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Brotacel's script (brotacel.py: its palette, eye, padded toes, stripe and board check):
#   blender --background --factory-startup --python sculpts/oracul.py -- <the export's Oracul.glb> <out.glb>
#
# Carpathia's chorus frog (No. 10120, Water/Sound), from its picture (docs/art/reference/gemini/carpathia/
# 10120-Oracul.jpg) and its brief (CP-dex-3-river-and-caves.md, 120): Brotacel grown into a big green frog standing
# upright like a conductor; a broad head with its mouth open in song, golden eyes on bulges and the line's dark
# stripe running back from each eye down the flank; its instrument, the part seen first, is its throat sac grown
# into a huge round resonator under the chin, with a smaller sac at each cheek, all three glowing pale blue; a cream
# belly; long green flaps hanging from its back like the tails of a coat, dark on their inner side, and pointed flaps
# at the shoulders; long bent frog legs on splayed padded feet; a reed baton in its +X hand. The picture's arms are
# flung wide: here they are carried forward and down, the hands in front of the belly under the sac. The rings of
# sound and the ripples of water in the picture are effects and are left out.
# The export's joints: body (0, 0, 0.501), arms (+-0.13, -0.02, 0.621), head (0, -0.03, 0.681), legs (+-0.08, 0.02,
# 0.361).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, srgb, inside, unit, eye_look, outward, args, sibling

br = sibling("brotacel")
GREEN, GREEN_DARK, CREAM, STRIPE, INK = br.GREEN, br.GREEN_DARK, br.CREAM, br.STRIPE, br.INK
on_ell, stripe, pads, board = br.on_ell, br.stripe, br.pads, br.board

SAC, SAC_GLOW = "#7CC6EC", "#4FB2E6"
MOUTH, LIP = "#D9707A", "#3C2A24"
IRIS = "#D9963A"
LINING = "#3B3326"
REED = "#C9B479"

VOXEL = 0.0068

BC, BR = np.array([0.0, 0.0, 0.5]), np.array([0.145, 0.122, 0.168])
HC, HR = np.array([0.0, -0.04, 0.708]), np.array([0.132, 0.11, 0.074])
SN_C, SN_R = np.array([0.0, -0.092, 0.692]), np.array([0.106, 0.064, 0.052])
BUMP_C, BUMP_R = np.array([0.076, -0.062, 0.758]), np.array([0.046, 0.044, 0.044])
EYEDIR = unit(0.6, -0.66, 0.45)
EYE_HALF = 0.031
SAC_C, SAC_R = np.array([0.012, -0.142, 0.575]), np.array([0.124, 0.096, 0.088])
CHEEK_C, CHEEK_R = np.array([0.136, -0.078, 0.668]), np.array([0.05, 0.048, 0.048])
STRIPE_PATH = [(-0.05, 0.765), (0.0, 0.74), (0.045, 0.69), (0.075, 0.6), (0.095, 0.48), (0.1, 0.36)]


def egg(V):
    V = V.copy()
    s = 1 + 0.1 * np.clip(-V[:, 2], 0, 1) - 0.12 * np.clip(V[:, 2], 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    return V


def body(k):
    k.add("body", ell(BC, BR, deform=egg), GREEN, "body")
    k.add("chest", ell((0.0, -0.02, 0.6), (0.13, 0.105, 0.09)), GREEN, "body")
    k.add("head", ell(HC, HR), GREEN, "head")
    k.add("snout", ell(SN_C, SN_R), GREEN, "head")
    k.add("bump", ell(BUMP_C, BUMP_R), GREEN, "head", mirror=True)


def legs(k):
    hip, knee, ankle = np.array([0.09, 0.02, 0.37]), np.array([0.172, -0.075, 0.205]), np.array([0.138, 0.0, 0.05])
    k.add("thigh", ell((0.1, 0.0, 0.34), (0.078, 0.084, 0.095), rot=(10, 0, -10)), GREEN, "leg{s}", mirror=True)
    k.add("leg", tube([hip, knee, ankle], [0.06, 0.048, 0.035]), GREEN, "leg{s}", mirror=True)
    k.add("foot", tube([ankle + np.array([0, 0, -0.02]), (0.15, -0.05, 0.02)], [0.03, 0.022]), GREEN, "leg{s}",
          mirror=True)
    pads(k, "toe", (0.152, -0.05, 0.014), [(0.95, -0.45, -0.08), (0.4, -1, -0.08), (-0.15, -1, -0.08),
                                           (1.0, 0.25, -0.08)], 0.085, 0.0128, 0.02, "leg{s}")


def arms(k):
    sh, el, wr = np.array([0.13, -0.02, 0.621]), np.array([0.172, -0.1, 0.53]), np.array([0.105, -0.185, 0.452])
    k.add("arm", tube([sh, el, wr], [0.038, 0.032, 0.027]), GREEN, "arm{s}", mirror=True)
    k.add("palm", ell(wr + np.array([0.0, -0.012, -0.012]), (0.026, 0.024, 0.02)), GREEN, "arm{s}", mirror=True)
    pads(k, "fing", wr + np.array([0.0, -0.015, -0.016]), [(0.6, -0.7, -0.5), (0.1, -0.8, -0.65), (-0.4, -0.7, -0.6),
                                                           (0.9, -0.2, -0.3)], 0.05, 0.0122, 0.0165, "arm{s}")


def baton(k):
    """The reed baton in the +X hand, pointing up and out; its tip below the eyes."""
    a = np.array([0.098, -0.175, 0.4])
    b = np.array([0.25, -0.285, 0.66])
    k.add("baton", outward(tube([a, a + (b - a) * 0.5, b], [0.0145, 0.013, 0.0122], seg=12, per=4)), REED, "armR",
          fuse=False, surface="leaf")
    k.add("batonknot", outward(ell(a + (b - a) * 0.36, (0.017, 0.017, 0.017))), "#A89660", "armR", fuse=False,
          surface="leaf")


def sacs(k):
    """The resonator: the great throat sac and the two at the cheeks, loose and glowing."""
    k.add("sac", outward(ell(SAC_C, SAC_R)), SAC, "body", fuse=False, glow=SAC_GLOW, surface="shell")
    k.add("cheeksac", outward(ell(CHEEK_C, CHEEK_R)), SAC, "head", fuse=False, mirror=True, glow=SAC_GLOW,
          surface="shell")
    gp, gn = on_ell(SAC_C, SAC_R, (0.5, -0.7, 0.45))
    k.add("sacglint", outward(ell(gp - gn * 0.01, (0.03, 0.022, 0.02), rot=(0, 30, -30))), "#E6F6FE", "body",
          fuse=False, surface="shell")


def coat(k):
    """The coat's tails: long green flaps from the back's sides down past the hips, dark on their inner side, and a
    pointed flap at each shoulder."""
    def taper(V):
        V = V.copy()
        t = np.clip((V[:, 2] + 1) / 2, 0, 1)            # 0 at the tail's tip, 1 at its root
        V[:, 0] *= 0.35 + 0.65 * np.sqrt(t)
        return V
    k.add("coattail", ell((0.09, 0.098, 0.46), (0.07, 0.026, 0.16), rot=(-6, 0, 40), deform=taper), GREEN,
          "body", mirror=True)
    # dark on their inner edges, where the two tails part over the rump
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.055, 0.004) * inside(P[:, 2] - 0.56, 0.01), LINING,
            on=["coattail"])
    for s, side in ((1, "R"), (-1, "L")):
        b = np.array([0.11 * s, 0.03, 0.66]); d = unit(0.75 * s, 0.25, -0.6)
        w = np.cross(d, (0, 0, 1.0)); w /= np.linalg.norm(w)
        t = np.cross(d, w)
        k.add(f"shoulderflap{side}", hull([b + w * 0.035, b - w * 0.035, b + t * 0.014, b - t * 0.014,
                                           b + d * 0.05 + w * 0.02, b + d * 0.05 - w * 0.02, b + d * 0.1]),
              GREEN, "body", fuse=False, surface="scales")


def marks(k):
    def belly(P):
        q = (P - np.array([0.0, -0.06, 0.48])) / np.array([0.112, 0.09, 0.15])
        return inside(np.linalg.norm(q, axis=1) - 1, 0.03)
    k.paint(belly, CREAM, on=["body", "chest"])
    k.paint(lambda P: inside(np.linalg.norm((P - np.array([0, 0.09, 0.58])) / np.array([0.09, 0.08, 0.16]), axis=1)
                             - 1, 0.25) * 0.5, GREEN_DARK, on=["body", "chest"])
    stripe(k, STRIPE_PATH, 0.014, 0.085, edge=0.006)
    # the mouth open in song: pink between two dark lips across the snout's front
    up = lambda x: 0.698 + 0.9 * x * x
    lo = lambda x: 0.672 + 0.55 * x * x
    front = lambda P: inside(P[:, 1] + 0.1, 0.004) * inside(np.abs(P[:, 0]) - 0.1, 0.003)
    k.paint(lambda P: inside(P[:, 2] - up(P[:, 0]), 0.0015) * inside(lo(P[:, 0]) - P[:, 2], 0.0015) * front(P),
            MOUTH)
    k.paint(lambda P: inside(np.abs(P[:, 2] - up(P[:, 0])) - 0.0035, 0.0015) * front(P), LIP)
    k.paint(lambda P: inside(np.abs(P[:, 2] - lo(P[:, 0])) - 0.003, 0.0015) * front(P), LIP)


def eyes(k):
    ep, en = on_ell(BUMP_C, BUMP_R, EYEDIR)
    lid, ink = srgb(GREEN), srgb(INK)
    look = br.frog_eye(iris=IRIS, pupil_r=0.5)
    def lay(state):
        k.decal(ep, en, (EYE_HALF, EYE_HALF), eye_look(state, look, lid, ink, line=0.16), eps=0.0025, rings=20,
                segs=56, mirror=True)
    return lay


def oracul(src, out_path):
    k = Kit(src, coat="scales")
    body(k)
    legs(k)
    arms(k)
    baton(k)
    sacs(k)
    coat(k)
    marks(k)
    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.05)
    board(k)
    k.eye_states(eyes(k))
    k.export(out_path)


if __name__ == "__main__":
    oracul(*args())
