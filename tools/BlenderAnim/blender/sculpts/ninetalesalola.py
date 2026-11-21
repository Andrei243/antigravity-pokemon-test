# Ninetales-Alola sculpted on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ninetalesalola.py -- <the export's Ninetales-Alola.glb> <out.glb>
# Grown from ninetales.py through sibling (its trunk, legs, head, ruff, nose and its nine plumes; the module's own Kit
# is this script's, made on this form's export as it is imported). From the references: the same tall fox in a coat
# of white with a breath of blue; a long flowing crest of pale blue-white hair from the crown, one thick mass sweeping
# up, back and out to the fox's right and falling in a wave, a shorter lock to its left; tall pointed ears lined deep
# blue; blue almond eyes slanting to the nose; a white ruff on the chest; nine tails as one great cloud of pale blue,
# deeper blue in the middle of the fan and fading to white at their curling ends.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import tube, srgb, inside, unit, eye_look, polygon, sibling

n9 = sibling("ninetales")
k = n9.k

COAT = "#EEF4FA"
CREST = "#DDEAF6"         # the crest: a little bluer than the coat
TAILS = "#C4DEF2"         # the tails' pale blue
DEEP = "#A6CEEE"         # deeper blue in the middle of the fan
TIP = "#EEF5FB"           # the curling ends fade to white
LINING = "#3E6EB8"
EYE, PUPIL = "#2E80D4", "#10306C"
NOSE = "#3A4E78"
INK = "#7A8CAE"           # the closed eyes' line and the mouth: a mid blue-grey on the white
CB, CI = srgb(COAT), srgb(INK)


def add_ear_lining(color=LINING):
    EAR = n9.EAR
    d = unit(*(np.array(EAR[-1]) - np.array(EAR[0])))
    across = np.cross(d, (0, 1.0, 0)); across /= np.linalg.norm(across)
    def lin(P):
        t, off = n9.along(P, EAR)
        half = 0.022 * np.sin(np.pi * np.clip((t + 0.12) / 1.0, 0, 1)) ** 0.7
        w = off @ across
        front = -off[:, 1]
        return (inside(np.abs(w) - half, 0.0025) * inside(-front + 0.002, 0.002) * inside(t - 0.86, 0.01)
                * (P[:, 2] > 0.7))
    k.paint(lin, color, on=["ear"])


def add_flowing_crest(color=CREST):
    """One thick lock up, back and out to +X falling in a wave; a shorter one back and out to -X."""
    H, HR = n9.H, n9.HR
    top = H + np.array([0, 0.02, HR[2] * 0.75])
    # laid low along the back of the head and the neck, then falling in a wave behind the shoulder: never an arch
    main = [top, top + (0.025, 0.07, 0.05), top + (0.08, 0.15, 0.03), top + (0.14, 0.21, -0.04),
            top + (0.175, 0.25, -0.12), top + (0.16, 0.29, -0.19), top + (0.19, 0.32, -0.26)]
    k.add("crest0", tube(main, [0.066, 0.07, 0.066, 0.056, 0.046, 0.034, 0.018]), color, "crest")
    second = [top + (-0.01, 0.01, 0.0), top + (-0.05, 0.08, 0.03), top + (-0.1, 0.15, -0.01),
              top + (-0.125, 0.2, -0.08), top + (-0.12, 0.23, -0.15)]
    k.add("crest1", tube(second, [0.055, 0.054, 0.045, 0.032, 0.017]), color, "crest")
    # a fringe over the brow, between the ears
    k.add("crest2", tube([top + (0, -0.01, -0.01), top + (0.0, -0.05, 0.02), top + (0.02, -0.08, 0.0)],
                         [0.04, 0.032, 0.018]), color, "crest")


def add_cloud_tails():
    n9.add_tails(TAILS, tip=TIP)
    root = n9.TAIL_ROOT
    # deeper blue in the middle of the fan, round the roots
    k.paint(lambda P: inside(np.linalg.norm((P - (root + (0, 0.1, 0.1))) / (0.12, 0.1, 0.13), axis=1) - 1, 0.03),
            DEEP, on=["tail"])


def mouth(u, v):
    c = np.tile(CB, (len(u), 1))
    c[(np.abs(v - (-0.1 + 0.9 * (np.abs(u) - 0.3) ** 2)) < 0.22) & (np.abs(u) < 0.9)] = CI
    return c


def eye(u, v):
    c = np.tile(srgb(EYE), (len(u), 1))
    c[((u - 0.05) / 0.38) ** 2 + (v / 0.82) ** 2 < 1] = srgb(PUPIL)
    c[((u - 0.4) / 0.2) ** 2 + ((v - 0.4) / 0.3) ** 2 < 1] = (1, 1, 1)
    return c


def eyes(state):
    look = eye_look(state, eye, CB, CI)
    E, D, HALF, OUT = n9.E, n9.EYE_DIR, n9.EYE_HALF, n9.EYE_OUTLINE
    k.decal(E, D, HALF, look, eps=0.004, rings=20, segs=56, outline=polygon(OUT))
    m = np.array([-1, 1, 1.0])
    k.decal(E * m, D * m, HALF, lambda u, v: look(-u, v), eps=0.004, rings=20, segs=56,
            outline=polygon([(-x, y) for x, y in OUT]))


if __name__ == "__main__":
    n9.add_body(COAT)
    n9.add_legs(COAT)
    n9.add_head(COAT)
    n9.add_ears(COAT)
    n9.add_ruff(COAT)
    add_flowing_crest()
    add_cloud_tails()
    add_ear_lining()
    n9.add_nose(color=NOSE)
    n9.add_mouth(look=mouth)

    k.build(voxel=0.0078, band=0.004)
    k.eye_states(eyes)
    k.export(n9.out)
