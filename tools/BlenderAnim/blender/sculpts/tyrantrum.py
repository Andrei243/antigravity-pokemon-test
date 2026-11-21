# Tyrantrum sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tyrantrum.py -- <the export's Tyrantrum.glb> <out.glb>
#
# Tyrunt grown (tyrunt.py: the same head made by `jaws`, the same flat-faced spikes and horns, the same eye under a
# brow), from the references: a great red-brown tyrannosaur, its trunk leaning forward on huge haunches that reach
# down to just above the ankle, short thick shins, broad feet of black claws; a pale grey belly from the throat to the
# groin. The head is carried high in front on a thick neck, its jaws gaping on a pink mouth lined with a few big white
# teeth, little orange points at the snout's tip. A crown of golden spikes stands across the brow, laid up and
# forward like a visor over a small black eye. A white jagged beard hangs from under the jaw down the throat, and a
# great ruff of white blades fans up and back behind the neck (the third mass after the trunk and the head). Tiny
# arms held forward under the chest, two black claws each. A thick tail sweeping up behind, three orange spikes at its
# tip. Orange plates scattered over the back, flanks, thighs and tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, args, sibling

ty = sibling("tyrunt")
bg = ty.bg

RED = "#A9444F"
BELLY = "#D3D4DA"
PLATE = "#E8923A"
GOLD, GOLD_TIP = "#EE9A2C", "#F7C445"
WHITE = "#F3F2F5"
CLAW = "#2B2627"
TOOTH = ty.TOOTH
HP = np.array([0.0, -0.14, 0.66])      # the head's first growth was about this point of it
HJ = np.array([0.0, -0.1, 0.58])       # the head's joint: grown a third again about it
G = 1.22 * 1.33
D = np.array([0.0, -0.08, -0.17])      # and carried forward and low, in front of the chest


def tyrantrum(src, out):
    k = Kit(src, coat="scales")
    spike, horn = ty.spike, ty.horn

    # --- the head: high in front, the jaws gaping (tyrunt.py's frame, grown about a point of the head) -----------
    def h(q):
        first = HP + (np.array(q, float) - HP) * 1.22
        return HJ + (first - HJ) * 1.33 + D

    def r(v):
        return np.array(v, float) * G

    J = dict(body=RED, surface="scales", jawColor=RED, rim=0.74,
             S=h((0, -0.22, 0.725)), SR=r((0.115, 0.13, 0.095)),
             snout=h((0, -0.36, 0.69)), snoutR=r((0.09, 0.125, 0.064)), snoutRot=8,
             cheek=h((0.075, -0.22, 0.665)), cheekR=r((0.06, 0.09, 0.07)),
             root=h((0, -0.2, 0.62)), rootR=r((0.095, 0.08, 0.07)),
             jaw=h((0, -0.315, 0.555)), jawR=r((0.084, 0.12, 0.03)), jawRot=38,
             mouth=h((0, -0.31, 0.615)), mouthR=r((0.068, 0.12, 0.055)), mouthRot=24, throat=0.2)
    ty.jaws(k, J)
    S, SR = J["S"], J["SR"]
    # the golden crown's base across the brow, and its spikes laid up and forward like a visor
    k.add("crown", ell(h((0, -0.25, 0.79)), r((0.1, 0.085, 0.035)), rot=(-12, 0, 0)), GOLD, "head", surface="shell")
    crown = [(0.0, (0, -0.7, 1), 0.11), (0.045, (0.25, -0.6, 1), 0.105), (0.085, (0.55, -0.45, 1), 0.09)]
    for i, (x, d, ln) in enumerate(crown):
        for s in ((1, -1) if x else (1,)):
            sd = "R" if s > 0 else "L"
            horn(k, f"crown{i}{sd}", h((x * s, -0.27 + abs(x) * 0.3, 0.8 - abs(x) * 0.25)), (d[0] * s, d[1], d[2]),
                 ln * G, 0.06 * G, "head", root=GOLD, tip=GOLD_TIP)
    # the brow over each eye, sloping to the snout
    k.add("brow", ell(h((0.08, -0.3, 0.745)), r((0.04, 0.07, 0.022)), rot=(-14, 0, -12)), GOLD, "head", mirror=True,
          surface="shell")

    # teeth: a few big ones down from the upper jaw, up from the lower
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        for j, (y, ln) in enumerate([(-0.45, 0.055), (-0.39, 0.05), (-0.32, 0.045)]):
            spike(k, f"fang{sd}{j}", h((0.062 * s, y, 0.648)), (0.15 * s, -0.1, -1), ln * G, 0.03 * G, TOOTH, "head",
                  up=(0, 1, 0))
        for j, (y, z, ln) in enumerate([(-0.395, 0.5, 0.045), (-0.335, 0.545, 0.04)]):
            spike(k, f"tusk{sd}{j}", h((0.058 * s, y, z)), (0.1 * s, -0.25, 1), ln * G, 0.028 * G, TOOTH, "head",
                  up=(0, 1, 0))
        # small orange points at the snout's tip
        spike(k, f"nub{sd}", h((0.045 * s, -0.47, 0.675)), (0.3 * s, -0.6, -0.6), 0.04 * G, 0.03 * G, PLATE, "head",
              up=(0, 0, 1))

    # --- the beard: white, jagged, from under the jaw down the throat -------------------------------------------
    k.add("throat", ell(h((0, -0.2, 0.545)), r((0.085, 0.11, 0.07)), rot=(-25, 0, 0)), WHITE, "head")
    beard = [(0.06, -0.29, 0.52, (0.3, 0.2, -1), 0.1), (0.075, -0.22, 0.53, (0.5, 0.3, -1), 0.12),
             (0.0, -0.25, 0.51, (0, 0.1, -1), 0.12), (0.07, -0.14, 0.5, (0.5, 0.4, -1), 0.1),
             (0.0, -0.15, 0.49, (0, 0.2, -1), 0.1)]
    for i, (x, y, z, d, ln) in enumerate(beard):
        for s in ((1, -1) if x else (1,)):
            sd = "R" if s > 0 else "L"
            spike(k, f"beard{i}{sd}", h((x * s, y, z)), (d[0] * s, d[1], d[2]), ln * G, 0.065 * G, WHITE, "head",
                  up=(0, 1, 0))

    # --- the ruff: great white blades fanning up and back behind the neck ----------------------------------------
    ruff = [(0.0, -0.1, 0.79, (0, 0.55, 1), 0.19), (0.065, -0.08, 0.75, (0.3, 0.6, 1), 0.22),
            (0.1, -0.05, 0.67, (0.6, 0.75, 0.65), 0.21), (0.12, -0.03, 0.58, (0.8, 0.75, 0.15), 0.17),
            (0.11, -0.04, 0.5, (0.75, 0.5, -0.35), 0.12)]
    for i, (x, y, z, d, ln) in enumerate(ruff):
        for s in ((1, -1) if x else (1,)):
            sd = "R" if s > 0 else "L"
            spike(k, f"ruff{i}{sd}", (x * s * 1.15, y - 0.1, z - 0.06), (d[0] * s, d[1], d[2]), ln * 1.25, 0.1, WHITE, "head", up=(0, 1, 0))

    # --- the trunk: chest out, leaning forward, a pale belly; a thick neck ----------------------------------------
    k.add("neck", tube([(0, -0.1, 0.52), (0, -0.2, 0.57), (0, -0.28, 0.58)], [0.125, 0.12, 0.115]), RED, "body")
    # the trunk's long axis leans forward some 40 degrees from upright, from the hips up to the chest
    k.add("chest", ell((0, -0.06, 0.47), (0.165, 0.16, 0.23), rot=(40, 0, 0)), RED, "body")
    k.add("belly", ell((0, -0.03, 0.36), (0.165, 0.16, 0.14)), RED, "body")
    k.add("hips", ell((0, 0.1, 0.37), (0.16, 0.15, 0.15)), RED, "body")

    def belly(P):
        front = P[:, 1] - (-0.08 + 0.32 * (P[:, 2] - 0.42))
        return inside(front, 0.004) * inside(np.abs(P[:, 0]) - 0.11 + 0.15 * np.abs(P[:, 2] - 0.4), 0.004) * \
            inside(0.24 - P[:, 2], 0.004) * inside(P[:, 2] - 0.6, 0.004)

    k.paint(belly, BELLY, on=["chest", "belly", "hips", "neck"])

    # --- the tail: thick, sweeping up behind, a little out to +X, three orange spikes at its tip -----------------
    TP = [(0, 0.17, 0.42), (0.01, 0.31, 0.47), (0.04, 0.43, 0.56), (0.08, 0.52, 0.68)]
    k.add("tail", tube(TP, [0.14, 0.11, 0.075, 0.04]), RED, "tail")
    tip = np.array(TP[-1])
    for j, d in enumerate([(0, 0.3, 1), (0.6, 0.6, 0.6), (-0.5, 0.6, 0.6)]):
        spike(k, f"tailspike{j}", tip + np.array([0, -0.01, 0.0]), d, 0.09, 0.05, PLATE, "tail", up=(0, 0, 1)
              if j == 0 else (0, 1, 0))

    # --- the arms: tiny, held forward under the chest, two black claws -------------------------------------------
    k.add("arm", tube([(0.08, -0.08, 0.46), (0.14, -0.21, 0.4), (0.15, -0.28, 0.35)], [0.042, 0.036, 0.032]), RED,
          "arm{s}", mirror=True)
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        for j, dx in enumerate((-0.015, 0.015)):
            spike(k, f"hclaw{sd}{j}", (s * (0.15 + dx), -0.3, 0.335), (s * dx * 5, -0.55, -0.8), 0.05, 0.026,
                  CLAW, f"arm{sd}", up=(0, 0, 1))

    # --- the legs: great haunches fused into the hips down to just above the ankle, short shins, broad feet ------
    k.add("thigh", ell((0.14, 0.07, 0.3), (0.12, 0.155, 0.18), rot=(-12, 0, 0)), RED, "leg{s}", mirror=True)
    k.add("shin", tube([(0.15, 0.09, 0.17), (0.155, 0.07, 0.07)], [0.08, 0.07]), RED, "leg{s}", mirror=True)
    k.add("foot", ell((0.155, -0.01, 0.035), (0.08, 0.12, 0.04)), RED, "leg{s}", mirror=True)
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        for j, dx in enumerate((-0.05, 0.0, 0.05)):
            spike(k, f"toe{sd}{j}", (s * (0.155 + dx), -0.1, 0.03), (s * dx * 5, -1, -0.3), 0.07, 0.035, CLAW,
                  f"leg{sd}", up=(0, 0, 1))
        spike(k, f"spur{sd}", (s * 0.155, 0.1, 0.03), (0, 1, -0.3), 0.05, 0.03, CLAW, f"leg{sd}", up=(0, 0, 1))

    # --- orange plates: a row down the back, a few on the flanks, thighs, tail and snout --------------------------
    plates = [((0.0, 0.04, 0.62), (0.06, 0.05, 0.04)), ((0.0, 0.13, 0.52), (0.06, 0.05, 0.045)),
              ((0.0, 0.3, 0.52), (0.05, 0.045, 0.04)), ((0.03, 0.44, 0.62), (0.04, 0.04, 0.035)),
              ((0.24, 0.07, 0.33), (0.05, 0.06, 0.06)), ((0.2, 0.0, 0.2), (0.04, 0.04, 0.035)),
              ((0.15, 0.17, 0.45), (0.05, 0.05, 0.04)), ((0.14, -0.08, 0.52), (0.04, 0.04, 0.035)),
              (tuple(h((0.07, -0.42, 0.755))), (0.06, 0.065, 0.045)), ((0.155, -0.04, 0.07), (0.05, 0.04, 0.03))]

    def plates_mask(P):
        m = np.zeros(len(P))
        for c, rad in plates:
            for s in ((1, -1) if c[0] else (1,)):
                q = (P - np.array([c[0] * s, c[1], c[2]])) / np.array(rad)
                q = q @ np.array([[1, 0, 0], [0, 0.94, 0.34], [0, -0.34, 0.94]])   # turned a little: uneven
                d = (np.abs(q) ** 4).sum(axis=1) ** 0.25                            # a plate with corners
                m = np.maximum(m, inside(d - 1, 0.03))
        return m

    k.paint(plates_mask, PLATE, on=["chest", "belly", "hips", "tail", "thigh", "foot", "snout", "neck"])
    k.paint(belly, BELLY, on=["chest", "belly", "hips", "neck"])

    # --- the eyes: small, under the golden brow, a white sliver round a big black pupil ------------------------
    de = unit(0.8, -0.5, 0.18)
    E = bg.on_ell(S, SR, de) + np.array([0, -0.007, -0.014])
    cut = bg.brow_cut(0.5, 0.45)
    look = bg.angry_eye(cut, rim=0.8, pupil_c=(-0.25, -0.1), pupil_r=0.55)
    eyes = bg.eyes_under_brow(k, E, de, (0.053, 0.048), look, cut, srgb(RED))

    k.build(voxel=0.008, smooth=3, band=0.006, wband=0.06)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    tyrantrum(*args())
