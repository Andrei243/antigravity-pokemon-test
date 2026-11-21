# Scrafty sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/scrafty.py -- <the export's Scrafty.glb> <out.glb>
#
# Scraggy grown (scraggy.py: its round eyes with an upright pupil, its grimace of white teeth with black lines, its
# belly plate in bands, its fists and its trousers of shed skin). From the references: an orange lizard, its head
# smaller against the body than Scraggy's, a tall red mohawk of five rounded lobes from the brow back over the crown.
# Its eyes white with a small black pupil under a heavy grey lid, the grey running back from the eye; two nostril dots
# and a crooked grimace of four teeth. A ruffled yellow hood of skin round its neck, bigger behind. An orange body with
# a grey belly plate in bands; arms bent out with the fists at the hips holding up baggy yellow trousers whose two
# wide legs splay out to ragged cuffs; the skin's end trailing back and out to +X with a red point standing up.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, slab, outward, srgb, inside, unit, eye_look, args, sibling

sg = sibling("scraggy")

ORANGE = "#F0823A"
SKIN = "#E9AE45"
SKIN_DARK = "#C7903A"
GREY, GREY_DARK = "#8F949B", "#55595F"
CREST = "#D8404A"
TAIL_TIP = "#D8404A"
INK = sg.INK


def scrafty(src, out):
    k = Kit(src, coat="fur")

    # --- the head: a ball a little wider than tall, eyes on bulges at its front corners -----------------------------
    H, HR = np.array([0.0, -0.035, 0.535]), np.array([0.16, 0.14, 0.122])
    k.add("head", ell(H, HR), ORANGE, "head")
    k.add("face", ell((0, -0.085, 0.5), (0.122, 0.085, 0.075)), ORANGE, "head")
    k.add("chin", ell((0, -0.06, 0.45), (0.1, 0.08, 0.045)), ORANGE, "head")
    EB, EBR = np.array([0.128, -0.09, 0.545]), np.array([0.05, 0.048, 0.052])
    k.add("bulge", ell(EB, EBR), ORANGE, "head", mirror=True)

    # the mohawk: five thick rounded lobes from the brow back over the crown, laid back, the middle ones longest
    for i, (ang, length, x) in enumerate([(-28, 0.075, 0.01), (-8, 0.098, -0.012), (13, 0.11, 0.012),
                                          (35, 0.1, -0.012), (58, 0.08, 0.01)]):
        a = math.radians(ang)
        d = unit(0, math.sin(a) + 0.18, math.cos(a))
        root = H + np.array([x, 0, 0]) + d * (HR[2] * 0.82)
        pts = [root, root + d * length * 0.55 + np.array([0, 0.012, 0]), root + d * length + np.array([0, 0.03, 0])]
        k.add(f"crest{i}", outward(blade(pts, [0.042, 0.04, 0.032], (1, 0, 0), 0.9)), CREST, "head", fuse=False,
              surface="skin")

    # --- the hood of skin round the neck: ruffled, bigger behind ---------------------------------------------------
    for j in range(10):
        t = 2 * math.pi * j / 10
        back = 0.5 + 0.5 * math.sin(t)          # 1 behind (+Y)
        r = 0.04 + 0.022 * back
        c = (0.105 * math.cos(t), 0.0 + 0.095 * math.sin(t) + 0.02 * back, 0.432 + 0.02 * back + 0.006 * (j % 2))
        k.add(f"hood{j}", ell(c, (r, r, r * 0.8)), SKIN, "body")
    k.add("hoodback", ell((0, 0.075, 0.46), (0.1, 0.05, 0.06)), SKIN, "body")

    # --- body, the grey plate in bands --------------------------------------------------------------------------
    k.add("torso", ell((0, 0.0, 0.34), (0.098, 0.078, 0.105)), ORANGE, "body")
    k.add("neck", tube([(0, -0.005, 0.38), (0, -0.02, 0.45)], [0.06, 0.06]), ORANGE, "body")
    sg.banded_plate(k, (0, -0.077, 0.335), (0, -1, 0.04), (0.05, 0.085), (-0.62, -0.25, 0.12, 0.5), GREY, GREY_DARK,
                    width=0.0048, outline=sg.polygon([(-0.75, -1), (0.75, -1), (1, 1), (-1, 1)]),
                    widths=[0.034, 0.038, 0.042, 0.046])

    # --- arms bent out at the elbow, fists at the hips holding the trousers up -----------------------------------
    SH, EL, WR = np.array((0.08, -0.01, 0.37)), np.array((0.152, 0.012, 0.32)), np.array((0.135, -0.05, 0.275))
    k.add("arm", tube([SH, (0.118, 0.0, 0.352), EL, WR], [0.031, 0.03, 0.029, 0.028]), ORANGE, "arm{s}", mirror=True)
    k.add("fist", ell((0.13, -0.068, 0.262), (0.042, 0.04, 0.037)), ORANGE, "arm{s}", mirror=True)
    for i, dz in enumerate((0.022, 0.0, -0.022)):
        k.add(f"knuckle{i}", ell((0.128, -0.1, 0.262 + dz), (0.017, 0.014, 0.012)), ORANGE, "arm{s}", mirror=True)

    # --- the trousers: a wide seat, two legs splaying out to ragged cuffs ------------------------------------------
    sg.trousers(k, (0, 0.01, 0.2), (0.15, 0.112, 0.085), SKIN, lambda th: 0.0, [(0.205, 1.02)],
                rim_z=0.272, rim_r=(0.138, 0.1, 0.024))
    k.add("pleg", tube([(0.07, 0.0, 0.17), (0.11, -0.01, 0.11), (0.14, -0.02, 0.075)], [0.085, 0.078, 0.072]), SKIN,
          "leg{s}", mirror=True)
    cuff = sg.zigzag(6, 0.0, 0.02)
    def ragged(V):
        V = V.copy()
        th = np.arctan2(V[:, 1], V[:, 0])
        lift = np.array([cuff(t) for t in th]) / 0.04
        V[:, 2] = V[:, 2] + lift * np.clip(-V[:, 2], 0, 1) ** 1.5
        return V
    k.add("cuff", ell((0.145, -0.025, 0.042), (0.09, 0.09, 0.042), deform=ragged), SKIN, "leg{s}", mirror=True)
    # a dark fold line round the seat and across each leg
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.193) - 0.004, 0.002) * inside(np.abs(P[:, 0]) - 0.12)
            * (P[:, 2] < 0.26), SKIN_DARK, on=["pants", "fold", "pleg"])
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.085) - 0.0035, 0.002) * inside(0.1 - np.abs(P[:, 0])),
            SKIN_DARK, on=["pleg", "cuff"])

    # the skin's end: a flat tongue trailing back and out to +X, a red point standing up at its end
    TD = unit(0.6, 0.8, 0)
    k.add("tail", blade([(0.0, 0.09, 0.15), (0.06, 0.16, 0.09), (0.12, 0.23, 0.06), (0.16, 0.27, 0.055)],
                        [0.07, 0.05, 0.036, 0.03], (0, 0, 1), 0.6), SKIN, "tail")
    k.add("tip", slab([(-0.03, -0.01), (0.035, -0.01), (0.02, 0.1)], (0.16, 0.27, 0.06), TD, (0, 0, 1), 0.034),
          TAIL_TIP, "tail", fuse=False, surface="skin")

    # --- the face: nostrils, a crooked grimace, the grey round each eye ------------------------------------------
    sg.nostrils(k, (0, -0.168, 0.53), unit(0, -1, 0.1), 0.024, half=0.0078)
    sg.grimace(k, (0, -0.163, 0.47), 4, (0.072, 0.028), gap=0.006, rim=0.006, wobble=lambda i: (0.004, -0.003, 0.003)[i])

    de = unit(0.7, -0.7, 0.04)
    E = sg.on_ell(EB, EBR, de)
    k.decal(E + np.array([0.002, 0.003, 0.002]), de, (0.052, 0.056), GREY, eps=0.004,
            rings=10, segs=48, mirror=True)
    CG, CGD = srgb(GREY), srgb(GREY_DARK)
    look = sg.round_eye(pupil=(-0.1, -0.15), pupil_r=(0.2, 0.24), lid=lambda u: 0.12 + 0.12 * u,
                        lid_line=(CGD, CG, 0.16))
    def eyes(state):
        k.decal(E, de, (0.042, 0.047), eye_look(state, look, CG, INK, line=0.13), eps=0.006, rings=20, segs=56,
                mirror=True)

    k.build(voxel=0.006, smooth=4, band=0.004)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    scrafty(*args())
