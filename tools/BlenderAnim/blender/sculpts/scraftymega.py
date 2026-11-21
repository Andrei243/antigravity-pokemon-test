# Scrafty's Mega Evolution sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/scraftymega.py -- <the export's Scrafty-Mega.glb> <out.glb>
# Scrafty grown (scrafty.py's head, face, body, arms and trousers copied here, since its build takes no arguments;
# scraggy.py's eye, grimace, nostrils, belly plate and trousers through sibling). From the references: the shed skin
# turned white and grown into a great hood pulled over the whole head, open round the orange face, its crown running
# back and up to a rounded point with a dark red hole like an eye socket on top; the red crest showing as a thick lobe
# out of the hood's brow over the face, curving forward and down, and as a red bow out of the hood's back; a long
# white streamer of skin trailing from the neck back and out to the right with a ragged end; the trousers white,
# baggier, torn; the orange face with its heavy grey-lidded eyes and crooked grimace; a crouch, the head forward.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, slab, outward, srgb, inside, unit, eye_look, args, sibling

sg = sibling("scraggy")

ORANGE = "#F0823A"
SKIN = "#F1F0EC"
SKIN_DARK = "#C4C3C2"
GREY, GREY_DARK = "#8F949B", "#55595F"
CREST = "#D8404A"
SOCKET = "#8C2430"
INK = sg.INK


def scrafty_mega(src, path):
    k = Kit(src, coat="fur")

    # --- the head: the base's ball, eyes on bulges at its front corners, carried a little forward ----------------
    H, HR = np.array([0.0, -0.045, 0.53]), np.array([0.16, 0.14, 0.122])
    k.add("head", ell(H, HR), ORANGE, "head")
    k.add("face", ell((0, -0.095, 0.495), (0.122, 0.085, 0.075)), ORANGE, "head")
    k.add("chin", ell((0, -0.07, 0.445), (0.1, 0.08, 0.045)), ORANGE, "head")
    EB, EBR = np.array([0.128, -0.1, 0.54]), np.array([0.05, 0.048, 0.052])
    k.add("bulge", ell(EB, EBR), ORANGE, "head", mirror=True)

    # --- the hood: a great white shell over the crown and sides, open round the face, drawn back to a point -------
    def opening(V):
        V = V.copy()
        # the front below the brow drawn in under the head, so the face stands out of the hood's rim
        front = np.clip(-V[:, 1] - 0.1, 0, 1) * np.clip(0.55 - V[:, 2], 0, 1.5)
        V[:, 1] += front * 0.9
        return V
    k.add("hood", ell((0, 0.035, 0.585), (0.205, 0.19, 0.17), deform=opening), SKIN, "head")
    k.add("hoodtop", ell((0, 0.11, 0.69), (0.11, 0.14, 0.085), rot=(-28, 0, 0)), SKIN, "head")
    k.add("drape", ell((0.15, 0.04, 0.46), (0.065, 0.1, 0.1)), SKIN, "head", mirror=True)

    # the red crest: a thick lobe out of the hood's brow curving forward and down, a bow out of its back
    # (the pictures put the lobe over the face's left side, the bow on the right of the hood's back)
    k.add("crest", outward(blade([(-0.035, -0.11, 0.66), (-0.065, -0.2, 0.655), (-0.095, -0.255, 0.615),
                                  (-0.108, -0.27, 0.565)], [0.052, 0.062, 0.05, 0.028], (1, 0, 0), 0.6)), CREST, "head",
          fuse=False, surface="skin")
    root = np.array((0.115, 0.15, 0.5))
    for i, d in enumerate((unit(0.6, 0.5, 0.6), unit(0.7, 0.5, -0.5))):
        k.add(f"bow{i}", outward(blade([root, root + d * 0.045, root + d * 0.085], [0.035, 0.045, 0.025],
                                       unit(0.45, -0.89, 0), 0.55)), CREST, "head", fuse=False, surface="skin")

    # --- body, the grey plate in bands, the neck's white ruff ----------------------------------------------------
    k.add("torso", ell((0, 0.0, 0.34), (0.098, 0.078, 0.105)), ORANGE, "body")
    k.add("neck", tube([(0, -0.01, 0.38), (0, -0.03, 0.45)], [0.06, 0.06]), ORANGE, "body")
    sg.banded_plate(k, (0, -0.077, 0.335), (0, -1, 0.04), (0.05, 0.085), (-0.62, -0.25, 0.12, 0.5), GREY, GREY_DARK,
                    width=0.0048, outline=sg.polygon([(-0.75, -1), (0.75, -1), (1, 1), (-1, 1)]),
                    widths=[0.034, 0.038, 0.042, 0.046])
    for j in range(8):
        t = 2 * math.pi * j / 8
        back = 0.5 + 0.5 * math.sin(t)
        if back < 0.3:
            continue                    # the front of the neck shows: the ruff is behind and at the sides
        r = 0.04 + 0.02 * back
        c = (0.11 * math.cos(t), 0.0 + 0.09 * math.sin(t) + 0.02 * back, 0.43 + 0.02 * back)
        k.add(f"ruff{j}", ell(c, (r, r, r * 0.8)), SKIN, "body")

    # --- arms bent out at the elbow, fists at the hips holding the trousers up -----------------------------------
    SH, EL, WR = np.array((0.08, -0.01, 0.37)), np.array((0.152, 0.012, 0.32)), np.array((0.135, -0.05, 0.275))
    k.add("arm", tube([SH, (0.118, 0.0, 0.352), EL, WR], [0.031, 0.03, 0.029, 0.028]), ORANGE, "arm{s}", mirror=True)
    k.add("fist", ell((0.13, -0.068, 0.262), (0.042, 0.04, 0.037)), ORANGE, "arm{s}", mirror=True)
    for i, dz in enumerate((0.022, 0.0, -0.022)):
        k.add(f"knuckle{i}", ell((0.128, -0.1, 0.262 + dz), (0.017, 0.014, 0.012)), ORANGE, "arm{s}", mirror=True)

    # --- the trousers: white, baggier, two legs splaying wide to torn cuffs ------------------------------------
    sg.trousers(k, (0, 0.01, 0.2), (0.165, 0.12, 0.09), SKIN, lambda th: 0.0, [(0.205, 1.02)],
                rim_z=0.272, rim_r=(0.145, 0.105, 0.026))
    k.add("pleg", tube([(0.075, 0.0, 0.17), (0.125, -0.015, 0.11), (0.16, -0.03, 0.075)], [0.095, 0.088, 0.08]), SKIN,
          "leg{s}", mirror=True)
    cuff = sg.zigzag(6, 0.0, 0.022)
    def ragged(V):
        V = V.copy()
        th = np.arctan2(V[:, 1], V[:, 0])
        lift = np.array([cuff(t) for t in th]) / 0.04
        V[:, 2] = V[:, 2] + lift * np.clip(-V[:, 2], 0, 1) ** 1.5
        return V
    k.add("cuff", ell((0.165, -0.035, 0.043), (0.098, 0.098, 0.043), deform=ragged), SKIN, "leg{s}", mirror=True)
    # orange feet showing under the cuffs
    k.add("foot", ell((0.17, -0.11, 0.018), (0.04, 0.05, 0.02)), ORANGE, "leg{s}", mirror=True)
    # grey folds and tears round the seat and across each leg
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.193) - 0.004, 0.002) * inside(np.abs(P[:, 0]) - 0.12)
            * (P[:, 2] < 0.26), SKIN_DARK, on=["pants", "fold", "pleg"])
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.09) - 0.0035, 0.002) * inside(0.11 - np.abs(P[:, 0])),
            SKIN_DARK, on=["pleg", "cuff"])
    for s in (1, -1):
        for p in ((0.16 * s, -0.11, 0.13), (0.06 * s, -0.11, 0.2)):
            k.paint(lambda P, p=np.array(p): inside(np.abs(P[:, 2] - p[2] - 0.25 * (P[:, 0] - p[0])) - 0.004, 0.002)
                    * inside(np.abs(P[:, 0] - p[0]) - 0.03, 0.003) * (P[:, 1] < -0.04), SKIN_DARK, on=["pleg", "pants"])

    # the tail: orange, short, a red point at its end
    TD = unit(0.6, 0.8, 0)
    k.add("tail", tube([(0.0, 0.07, 0.17), (0.06, 0.15, 0.1), (0.11, 0.21, 0.065)], [0.034, 0.03, 0.024]), ORANGE,
          "tail")
    k.add("tip", slab([(-0.025, -0.01), (0.03, -0.01), (0.015, 0.075)], (0.115, 0.215, 0.06), TD, (0, 0, 1), 0.03),
          CREST, "tail", fuse=False, surface="skin")

    # the streamer: a broad ribbon of skin from the neck trailing back and out to the right, drooping, its end torn
    P = [(0.13, 0.06, 0.43), (0.2, 0.15, 0.37), (0.27, 0.23, 0.3), (0.33, 0.3, 0.22)]
    k.add("streamer", outward(blade(P, [0.06, 0.06, 0.055, 0.046], (0.3, -0.5, 0.81), 0.34)), SKIN, "body", fuse=False,
          surface="cloth")
    for i, (end, w) in enumerate((((0.4, 0.35, 0.17), 0.03), ((0.36, 0.38, 0.13), 0.026))):
        k.add(f"tatter{i}", outward(blade([P[-1], end], [0.038, w * 0.3], (0.3, -0.5, 0.81), 0.4)), SKIN, "body",
              fuse=False, surface="cloth")

    # --- the face: nostrils, a crooked grimace, the grey round each eye; the hood's socket ----------------------
    sg.nostrils(k, (0, -0.178, 0.525), unit(0, -1, 0.1), 0.024, half=0.0078)
    sg.grimace(k, (0, -0.173, 0.465), 4, (0.072, 0.028), gap=0.006, rim=0.006, wobble=lambda i: (0.004, -0.003, 0.003)[i])
    # the dark red hole on the hood's crown, like an eye socket
    k.decal((0.05, 0.0, 0.745), unit(0.25, -0.4, 0.88), (0.05, 0.038), SOCKET, eps=0.004, rings=12, segs=48)

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

    k.build(voxel=0.0062, smooth=4, band=0.004)
    k.eye_states(eyes)
    k.export(path)


if __name__ == "__main__":
    scrafty_mega(*args())
