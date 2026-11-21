# Sawsbuck (its spring form) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/sawsbuck.py -- <the export's Sawsbuck.glb> <out.glb>
# Grown from deerling.py (sibling("deerling")): the same saddle with a scalloped hem, ears with a lining, flowers of
# flat-faced petals, legs with dark hooves. From the references: a tall brown stag with a deep body; a cream belly
# under the brown, the hem scalloped and edged in orange; a big fluffy cream ruff on the chest; cream socks from the
# knee down, dark hooves; a long head carried forward, the jaw and lower face cream under an orange line from the
# eye, a dark nose; serious half-shut eyes under a brown lid; ears out to the sides, cream within; dark brown antlers
# branching up and out, pink five-petalled blossoms with yellow middles on them and pink buds at the tips; pink spots
# on the back; a short tuft of a tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, outward, lumpy, srgb, inside, smoothstep, unit, polygon,
                       eye_look, args, sibling)

dl = sibling("deerling")
lv = sibling("larvitar")              # the eye cut by a lid

BROWN, CREAM, ORANGE = "#86593C", "#F2DFA8", "#EE963A"
HOOF, NOSE, ANTLER = "#4A2820", "#36231E", "#5A3324"
BLOSSOM, BUD, MIDDLE, SPOT = "#F27E9A", "#E9688A", "#F6D03C", "#E9849C"
IRIS = srgb("#2E2220")


def join(*shapes):
    V, F, off = [], [], 0
    for sv, sf in shapes:
        V.append(sv); F.extend([[i + off for i in f] for f in sf]); off += len(sv)
    return np.vstack(V), F


def sawsbuck(src, out):
    k = Kit(src, coat="fur")

    # --- the trunk: deep, cream under the brown saddle --------------------------------------------------------
    k.add("barrel", ell((0, 0.02, 0.425), (0.096, 0.2, 0.11)), CREAM, "body")
    k.add("chest", ell((0, -0.13, 0.44), (0.092, 0.1, 0.115)), CREAM, "body")
    k.add("rump", ell((0, 0.155, 0.44), (0.102, 0.1, 0.112)), CREAM, "body")

    # --- legs: broad shoulders and thighs fused over the upper half, cream socks, dark hooves -----------------
    k.add("shoulder", ell((0.058, -0.12, 0.36), (0.054, 0.07, 0.125)), BROWN, "leg{s}", mirror=True)
    k.add("thigh", ell((0.062, 0.15, 0.37), (0.064, 0.095, 0.135)), BROWN, "hind{s}", mirror=True)
    dl.leg(k, "fore", 0.06, -0.12, 0.33, (0.047, 0.035, 0.031), "leg{s}", color=BROWN, hoof=HOOF)
    dl.leg(k, "hind", 0.062, 0.155, 0.33, (0.05, 0.035, 0.031), "hind{s}", color=BROWN, hoof=HOOF, knee=0.015)
    LEGS = [(0.061, -0.12), (0.063, 0.155)]
    def socks(P):
        ax = np.abs(P[:, 0])
        d = np.min([np.hypot(ax - x, P[:, 1] - y) for x, y in LEGS], axis=0)
        z = 0.17 + 0.012 * np.cos(np.arctan2(P[:, 1] - np.where(P[:, 1] < 0.02, -0.12, 0.155), ax - 0.062) * 3)
        return inside(d - 0.05, 0.002) * inside(P[:, 2] - z, 0.0025)
    k.paint(socks, CREAM)

    # --- a thick neck forward to a long head; a dark nose ---------------------------------------------------
    k.add("neck", tube([(0, -0.15, 0.53), (0, -0.2, 0.64), (0, -0.235, 0.73)], [0.086, 0.072, 0.064]), BROWN,
          "head")
    C, CR = np.array([0, -0.262, 0.785]), np.array([0.07, 0.078, 0.068])
    k.add("cranium", ell(C, CR), BROWN, "head")
    FC = np.array([0, -0.335, 0.742])
    k.add("face", ell(FC, (0.05, 0.052, 0.092), rot=(-62, 0, 0)), BROWN, "head")
    k.add("nose", ell((0, -0.408, 0.708), (0.02, 0.016, 0.017)), NOSE, "head", fuse=False, surface="skin")
    # the jaw and lower face cream under an orange line from below the eye to the nose
    def zline(P):
        return 0.738 + 0.24 * (P[:, 1] + 0.24)
    near = lambda P: (inside(np.linalg.norm(P - C, axis=1) - 0.18, 0.004) * inside(0.665 - P[:, 2], 0.004)
                      * inside(P[:, 1] + 0.252 - 0.5 * (0.74 - P[:, 2]), 0.004))
    k.paint(lambda P: inside(P[:, 2] - zline(P) - 0.009, 0.0025) * inside(zline(P) - P[:, 2], 0.0025) * near(P),
            ORANGE)
    k.paint(lambda P: inside(P[:, 2] - zline(P), 0.0025) * near(P), CREAM)

    # --- the ruff: a big fluffy cream collar on the chest -------------------------------------------------
    k.add("ruff", ell((0, -0.205, 0.5), (0.082, 0.05, 0.085), deform=lumpy(5, 3, 0.28)), CREAM, "body")
    for i, (x, z, d) in enumerate([(0.0, 0.43, (0, -0.4, -1)), (0.045, 0.44, (0.6, -0.5, -0.8)),
                                   (0.075, 0.5, (1, -0.4, -0.2))]):
        base = np.array((x, -0.215, z))
        k.add(f"tuft{i}", blade([base, base + unit(*d) * 0.03, base + unit(*d) * 0.055], [0.03, 0.02, 0.004],
                                (0, 1, 0), 0.5), CREAM, "body", mirror=x > 0)

    # --- the saddle: brown over the back, its scalloped hem edged in orange; pink spots on the back ----------
    dl.hem(k, 0.365, 0.026, 0.075, -0.2, 0.012, (-0.2, 0.27), 0.62, BROWN, ORANGE)
    for c, r in (((0.05, 0.12, 0.535), 0.022), ((0.03, 0.2, 0.54), 0.014), ((0.06, -0.06, 0.515), 0.013)):
        k.decal(c, unit(0.5, 0, 1), (r, r * 0.8), SPOT, eps=0.003, rings=6, segs=32, mirror=True)

    # --- the tail: a short brown tuft back and up ----------------------------------------------------------
    k.add("tail", blade([(0, 0.24, 0.5), (0, 0.29, 0.54), (0, 0.33, 0.57)], [0.03, 0.022, 0.004], (0, 1, 0), 0.6),
          BROWN, "tail")

    # --- the ears: out to the sides, cream within ----------------------------------------------------------
    dl.ear(k, (0.04, -0.25, 0.81), (1, 0.45, 0.35), unit(0.1, -0.9, 0.4), 0.085, 0.027, BROWN, CREAM)

    # --- the antlers: dark brown beams up and out from the crown, a tine forward and one in; blossoms and buds --
    for s, nm in ((1, "R"), (-1, "L")):
        m = np.array((s, 1, 1.0))
        P = lambda *p: np.array(p) * m
        beam = outward(tube([P(0.028, -0.245, 0.825), P(0.07, -0.235, 0.9), P(0.11, -0.215, 0.975),
                             P(0.13, -0.195, 1.04)], [0.022, 0.019, 0.016, 0.012], seg=14, per=6))
        fwd = outward(tube([P(0.07, -0.235, 0.9), P(0.12, -0.27, 0.93), P(0.155, -0.29, 0.955)],
                           [0.016, 0.014, 0.011], seg=12, per=6))
        inn = outward(tube([P(0.11, -0.215, 0.975), P(0.085, -0.22, 1.02), P(0.075, -0.225, 1.05)],
                           [0.015, 0.013, 0.011], seg=12, per=6))
        k.add("antler" + nm, join(beam, fwd, inn), ANTLER, "head", fuse=False, surface="shell")
        for c, n in ((P(0.095, -0.24, 0.93), (0.4 * s, -1, 0.35)), (P(0.123, -0.21, 1.005), (0.5 * s, -1, 0.5))):
            dl.flower(k, c + unit(*n) * 0.018, n, 0.042, BLOSSOM, MIDDLE)
        for c in (P(0.158, -0.292, 0.958), P(0.133, -0.193, 1.048), P(0.073, -0.226, 1.054)):
            k.add("bud" + nm, ell(c, (0.014, 0.014, 0.014), sub=3), BUD, "head", fuse=False, surface="leaf")

    # --- the eyes: serious, white with a dark iris toward the nose, cut by a brown lid -------------------
    de = unit(0.66, -0.7, 0.18)
    E = lv.on_ell(C, CR, de)
    cut = lv.brow_cut(0.12, 0.22)
    look = lv.angry_eye(cut, iris=IRIS, rim=0.8, iris_c=(-0.18, -0.15), iris_r=0.5, pupil_r=0.24)
    m = np.array([-1, 1, 1.0])
    LID, INK = srgb(BROWN), lv.INK

    def eyes(state):
        lk = eye_look(state, look, LID, INK, line=0.16)
        k.decal(E, de, (0.034, 0.03), lk, eps=0.006, rings=20, segs=56, outline=cut)
        k.decal(E * m, de * m, (0.034, 0.03), lambda u, v: lk(-u, v), eps=0.006, rings=20, segs=56,
                outline=lambda th: cut(math.pi - th))

    k.build(voxel=0.0065, smooth=4, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    sawsbuck(*args())
