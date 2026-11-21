# Druddigon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/druddigon.py -- <the export's Druddigon.glb> <out.glb>
#
# From the references: a teal-blue dragon hunched forward on crouched legs, its big craggy head red like rough rock,
# crags standing round its crown and the back of its skull; a narrow yellow eye with a small black pupil under a
# heavy brow sloping to the snout; the jaws open on a pink mouth with white fangs at its corners. Tan belly plates in
# bands from the throat to the groin. Long thick arms reaching forward and down to hands of three white claws, red
# spikes on the shoulders and the forearms; thick thighs with a red spike each, short shins, broad feet of three white
# claws. A thick tail back along the ground with red spikes along its top. Its wings are BLUE (the brief said red; the
# pictures overrule it): broad jagged membranes held out from the shoulder blades, a red spike at each root.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, boxy, srgb, inside, unit, eye_look, args, sibling

bg = sibling("bagon")           # its eye under a brow and its fang
ch = sibling("charmander")      # its claw
sg = sibling("scraggy")         # its belly plate in bands

BLUE = "#2D7FA8"
RED = "#D23A54"
TAN, TAN_LINE = "#E2B25C", "#A9772F"
CLAW = "#F2F2EE"
MOUTH, MOUTH_DEEP = "#E99AA6", "#A9505F"
YELLOW = srgb("#F4D44C")


def spike(k, name, base, d, length, width, bone, up=(0, 0, 1)):
    """A red spike: one flat-faced solid, a kite at its root."""
    k.add(name, bg.fang(base, d, length, width, up=up), RED, bone, fuse=False, surface="shell")


def druddigon(src, out):
    k = Kit(src, coat="scales")

    # --- the trunk: leaning forward, broad, deep -------------------------------------------------------------------
    k.add("hips", ell((0, 0.04, 0.22), (0.125, 0.115, 0.09)), BLUE, "body")
    k.add("belly", ell((0, 0.0, 0.3), (0.125, 0.115, 0.1)), BLUE, "body")
    k.add("chest", ell((0, -0.045, 0.4), (0.13, 0.11, 0.1), rot=(-15, 0, 0)), BLUE, "body")
    k.add("neck", tube([(0, -0.04, 0.42), (0, -0.1, 0.48), (0, -0.14, 0.51)], [0.075, 0.07, 0.065]), BLUE, "head")
    # the tan plates in bands down the front
    sg.banded_plate(k, (0, -0.118, 0.29), (0, -1, 0.0), (0.055, 0.09), (-0.72, -0.4, -0.08, 0.24, 0.56), TAN, TAN_LINE,
                    width=0.006, outline=sg.polygon([(-0.8, -1), (0.8, -1), (1, 1), (-1, 1)]))

    # --- the head: a big craggy red skull, the jaws open ----------------------------------------------------------
    S, SR = np.array([0.0, -0.16, 0.57]), np.array([0.105, 0.11, 0.08])
    k.add("skull", ell(S, SR, deform=boxy(2.4)), RED, "head", surface="shell")
    k.add("snout", ell((0, -0.27, 0.54), (0.078, 0.095, 0.05), rot=(-6, 0, 0)), RED, "head", surface="shell")
    k.add("cheek", ell((0.06, -0.2, 0.52), (0.05, 0.07, 0.05)), RED, "head", mirror=True, surface="shell")
    k.add("jaw", ell((0, -0.245, 0.418), (0.07, 0.085, 0.028), rot=(16, 0, 0)), RED, "head", surface="shell")
    k.add("jawroot", ell((0, -0.18, 0.45), (0.075, 0.06, 0.04)), RED, "head", surface="shell")
    # the mouth's inside, set back between the jaws
    k.add("mouth", ell((0, -0.25, 0.462), (0.048, 0.07, 0.036)), MOUTH, "head")
    k.paint(lambda P: inside(np.hypot(P[:, 0] / 0.05, (P[:, 2] - 0.462) / 0.016) - 1, 0.004) * (P[:, 1] < -0.22),
            MOUTH_DEEP, on=["mouth"])
    # the brow over each eye, sloping to the snout
    k.add("brow", ell((0.062, -0.215, 0.6), (0.045, 0.06, 0.022), rot=(-14, 0, -18)), RED, "head", mirror=True,
          surface="shell")
    # crags round the crown and the back of the skull
    for i, (p, d, ln) in enumerate([((0.05, -0.2, 0.635), (0.3, -0.2, 1), 0.05), ((0.08, -0.13, 0.63), (0.6, 0.1, 1), 0.06),
                                    ((0.095, -0.07, 0.6), (0.8, 0.5, 0.6), 0.06), ((0.07, -0.05, 0.53), (0.7, 0.8, -0.1), 0.05),
                                    ((0.0, -0.1, 0.645), (0, 0.4, 1), 0.06), ((0.0, -0.05, 0.6), (0, 1, 0.5), 0.06)]):
        for s in ((1, -1) if p[0] else (1,)):
            spike(k, f"crag{i}{'R' if s > 0 else 'L'}", (p[0] * s, p[1], p[2]), unit(d[0] * s, d[1], d[2]), ln, 0.05,
                  "head", up=(1, 0, 0) if p[0] == 0 else (0, 1, 0))
    # fangs: one down from each corner of the upper jaw, one up from the lower
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        k.add(f"fang{side}", bg.fang((0.045 * s, -0.33, 0.505), unit(0.1 * s, -0.2, -1), 0.04, 0.024), CLAW, "head",
              fuse=False, surface="shell")
        k.add(f"tusk{side}", bg.fang((0.045 * s, -0.31, 0.44), unit(0.1 * s, -0.25, 1), 0.03, 0.022), CLAW, "head",
              fuse=False, surface="shell")

    # --- the arms: long and thick, forward and down, three white claws a hand -----------------------------------
    SH, EL, WR = np.array((0.12, -0.05, 0.42)), np.array((0.185, -0.11, 0.32)), np.array((0.175, -0.23, 0.2))
    k.add("shoulder", ell((0.125, -0.04, 0.42), (0.07, 0.075, 0.07)), BLUE, "arm{s}", mirror=True)
    k.add("arm", tube([SH, EL, WR], [0.052, 0.045, 0.04]), BLUE, "arm{s}", mirror=True)
    k.add("hand", ell((0.175, -0.255, 0.18), (0.052, 0.05, 0.04)), BLUE, "arm{s}", mirror=True)
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        for j, dx in enumerate((-0.03, 0.0, 0.03)):
            k.add(f"hclaw{side}{j}", ch.claw((s * (0.175 + dx), -0.29, 0.17), unit(s * dx, -0.6, -0.8), 0.06, 0.024),
                  CLAW, f"arm{side}", fuse=False, surface="shell")
        spike(k, f"sspike{side}", (s * 0.15, -0.02, 0.47), unit(s * 0.6, 0.3, 1), 0.075, 0.05, f"arm{side}",
              up=(0, 1, 0))
        for j, t in enumerate((0.3, 0.6)):
            p = EL + (WR - EL) * t
            spike(k, f"aspike{side}{j}", (s * (p[0] + 0.03), p[1] + 0.02, p[2]), unit(s * 0.8, 0.5, 0.25), 0.05,
                  0.04, f"arm{side}", up=(0, 0, 1))

    # --- the legs: thick thighs fused into the hips, short shins, broad feet -----------------------------------
    k.add("thigh", ell((0.11, 0.0, 0.17), (0.085, 0.1, 0.11)), BLUE, "leg{s}", mirror=True)
    k.add("shin", tube([(0.12, -0.01, 0.11), (0.125, -0.02, 0.045)], [0.06, 0.055]), BLUE, "leg{s}", mirror=True)
    k.add("foot", ell((0.125, -0.06, 0.028), (0.062, 0.095, 0.03)), BLUE, "leg{s}", mirror=True)
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        for j, dx in enumerate((-0.035, 0.0, 0.035)):
            k.add(f"fclaw{side}{j}", ch.claw((s * (0.125 + dx), -0.14, 0.028), unit(s * dx, -1, -0.25), 0.05, 0.024),
                  CLAW, f"leg{side}", fuse=False, surface="shell")
        spike(k, f"tspike{side}", (s * 0.18, 0.04, 0.2), unit(s * 0.7, 0.6, 0.3), 0.06, 0.045, f"leg{side}",
              up=(0, 0, 1))

    # --- the tail: thick, back along the ground and a little out to +X, red spikes along its top ------------------
    TP = [(0, 0.1, 0.22), (0.03, 0.22, 0.14), (0.08, 0.34, 0.08), (0.13, 0.44, 0.05)]
    k.add("tail", tube(TP, [0.085, 0.065, 0.045, 0.022]), BLUE, "tail")
    for j, (t, ln) in enumerate([(0.22, 0.07), (0.42, 0.065), (0.62, 0.055), (0.8, 0.045)]):
        i = min(int(t * 3), 2); f = t * 3 - i
        p = np.array(TP[i]) + (np.array(TP[i + 1]) - np.array(TP[i])) * f
        r = [0.085, 0.065, 0.045, 0.022][i] * (1 - f) + [0.085, 0.065, 0.045, 0.022][i + 1] * f
        spike(k, f"tlspike{j}", p + np.array([0, 0, r * 0.8]), unit(0.15, 0.5, 1), ln, 0.045, "tail", up=(1, 0, 0))
    spike(k, "bspike", (0, 0.1, 0.4), unit(0, 0.5, 1), 0.05, 0.045, "body", up=(1, 0, 0))

    # --- the wings: broad jagged blue membranes out from the shoulder blades, raised in a V ------------------------
    wing = [(0.0, -0.04), (0.03, 0.12), (0.09, 0.22), (0.13, 0.17), (0.2, 0.2), (0.28, 0.13), (0.22, 0.1),
            (0.25, 0.04), (0.18, 0.05), (0.18, -0.02), (0.12, 0.02), (0.09, -0.05), (0.05, -0.01)]
    k.add("wing", slab(wing, (0.07, 0.07, 0.43), unit(0.82, 0.42, 0.38), unit(-0.15, 0.4, 1), 0.024), BLUE,
          "wing{s}", fuse=False, mirror=True)
    spike(k, "wspike", (0.1, 0.07, 0.47), unit(0.5, 0.3, 1), 0.06, 0.045, "wingR", up=(0, 1, 0))
    spike(k, "wspikeL", (-0.1, 0.07, 0.47), unit(-0.5, 0.3, 1), 0.06, 0.045, "wingL", up=(0, 1, 0))

    # --- the eyes: narrow and yellow under the brow, a small black pupil ----------------------------------------
    de = unit(0.62, -0.74, 0.18)
    E = sg.on_ell(S, SR, de) + np.array([0, -0.008, -0.01])
    cut = bg.brow_cut(0.22, 0.5)
    look = bg.angry_eye(cut, white=YELLOW, pupil_c=(-0.25, -0.15), pupil_r=0.32, rim=0.82)
    eyes = bg.eyes_under_brow(k, E, de, (0.036, 0.03), look, cut, srgb(RED))

    k.build(voxel=0.007, smooth=4, band=0.005)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    druddigon(*args())
