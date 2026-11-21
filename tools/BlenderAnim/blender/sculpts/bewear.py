# Bewear sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Stufful's script (stufful.py, imported as sibling("stufful")): the same pink and white, the same black-and-glint eye,
# the same white muzzle with its dark nose and smile, the same fan of a white ear with a pink root, the same pink pads.
#   blender --background --factory-startup --python sculpts/bewear.py -- <the export's Bewear.glb> <out.glb>
#
# From the references: a tall plush bear like a mascot costume, black from the shoulders down and pink above: the
# head a broad rounded column of pink nearly as wide as the body, its lower edge a level line under the jaw; a white
# oval muzzle with a black nose and a thin smile; small round black eyes with a glint above it; white fans of ears at
# the head's upper sides joined by a white band over the crown, a little tuft of pink leaves standing on top; a
# barrel of a black body; very long thick black arms (raised in the art, carried forward and down here, as for a hug),
# great round paws with a pink pad and three small pink toe pads on each palm; short thick black legs on broad feet
# with small white claws; a round pink tail at the back.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, leaf, outward, boxy, args, sibling

sf = sibling("stufful")
td = sf.td
PINK, WHITE, PAD = sf.PINK, sf.WHITE, sf.PAD
BLACK, TUFT, CLAW = "#2D2C30", "#F08AA2", "#ECEAE6"
INK = sf.INK


def bewear(src, out):
    k = Kit(src, coat="fur")
    B = np.array([0, 0.01, 0.34])
    H, HR = np.array([0, -0.015, 0.63]), np.array([0.14, 0.126, 0.16])

    # --- the body: a black barrel, fullest low; the head a broad pink column fused on top ----------------------------
    def pear(V):
        V = V.copy()
        s = 1 + 0.06 * np.clip(-V[:, 2], 0, 1) - 0.06 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell(B + (0, 0, -0.01), (0.178, 0.142, 0.19), deform=pear), BLACK, "body")
    def column(V):
        V = boxy(2.5)(V)
        s = 1 + 0.1 * np.clip(-V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("head", ell(H, HR, deform=column), PINK, "head")
    k.add("tail", ell((0, 0.15, 0.25), (0.05, 0.045, 0.05)), PINK, "body")
    # the level line under the jaw: pink above, black below, on the body's middle (the arms stay black)
    LINE = 0.5
    k.paint(lambda P: inside(LINE - P[:, 2], 0.0025) * inside(np.hypot(P[:, 0], P[:, 1] - H[1]) - 0.19, 0.004)
            * inside(np.abs(P[:, 0]) - 0.12 - 0.5 * np.clip(P[:, 2] - 0.56, 0, 1), 0.004), PINK)
    k.paint(lambda P: inside(P[:, 2] - LINE, 0.0025) * inside(P[:, 1] - 0.1, 0.004), BLACK)

    # --- the white muzzle, its nose and smile -----------------------------------------------------------------
    MC, MR = np.array([0, -0.136, 0.586]), np.array([0.068, 0.036, 0.05])
    sf.muzzle_face(k, MC, MR, "head", (0.022, 0.015), (0.04, 0.017), mouth_line=0.12)
    # the muzzle white again over the pink painted down to the jaw's line, its edge where it meets the face
    k.paint(lambda P: inside(np.linalg.norm((P - MC) / MR, axis=1) - 1.04, 0.02) * (P[:, 1] < MC[1]), WHITE)

    # --- white fans of ears at the head's upper sides, a white band over the crown, a tuft of pink leaves on top -----
    sf.fan_ear(k, (0.11, 0.005, 0.71), (0.8, 0.08, 0.55), (0.2, -1, 0.05), 0.1, 0.07, 0.018, "ear{s}",
               root_r=(0.03, 0.03, 0.03))
    k.paint(lambda P: inside(np.abs(P[:, 1] - 0.012) - 0.026, 0.0025) * inside(0.7 - P[:, 2], 0.004), WHITE)
    top = np.array([0, -0.01, 0.788])
    for i, d in enumerate([(0, -0.35, 1), (0.55, -0.15, 0.8), (-0.55, -0.15, 0.8), (0.2, 0.4, 0.9),
                           (-0.2, 0.4, 0.9)]):
        k.add(f"tuft{i}", outward(leaf(top + (0, 0, -0.008), unit(*d), 0.068, 0.022, thick=0.014)), TUFT, "head",
              fuse=False, surface="fur")

    # --- very long thick arms carried forward and down, great round paws, palms turned forward with pink pads ------
    k.add("arm", tube([(0.135, 0.0, 0.49), (0.205, -0.07, 0.42), (0.205, -0.16, 0.335), (0.17, -0.215, 0.3)],
                      [0.074, 0.07, 0.064, 0.06]), BLACK, "arm{s}", mirror=True)
    PC, PR = np.array([0.165, -0.228, 0.288]), np.array([0.072, 0.066, 0.075])
    k.add("paw", ell(PC, PR), BLACK, "arm{s}", mirror=True)
    pn = unit(-0.25, -0.92, 0.3)
    k.decal(sf.on_ell(PC, PR, pn), pn, (0.034, 0.03), PAD, eps=0.003, rings=12, segs=48, mirror=True,
            up=unit(0, 0.3, 1))
    for a in (-40, 0, 40):
        tn = unit(-0.25 + 0.6 * math.sin(math.radians(a)), -0.8, 0.62)
        k.decal(sf.on_ell(PC, PR, tn), tn, (0.012, 0.012), PAD, eps=0.003, rings=8, segs=40, mirror=True,
                up=unit(0, 0.3, 1))

    # --- short thick legs, the thighs fused into the body, broad feet with small white claws --------------------
    for s, bone in ((1, "legR"), (-1, "legL")):
        m = np.array([s, 1, 1.0])
        k.add("thigh" + bone[-1], ell(np.array((0.092, 0.0, 0.21)) * m, (0.1, 0.11, 0.12)), BLACK, bone)
        k.add("shin" + bone[-1], tube([np.array(p) * m for p in [(0.098, -0.005, 0.13), (0.1, -0.012, 0.075)]],
                                      [0.076, 0.07]), BLACK, bone)
        k.add("foot" + bone[-1], ell(np.array((0.1, -0.04, 0.033)) * m, (0.075, 0.095, 0.035)), BLACK, bone)
        for i, dx in enumerate((-0.03, 0.0, 0.03)):
            td.claw(k, f"toe{bone[-1]}{i}", np.array((0.1 + dx, -0.125, 0.024)) * m,
                    (0.2 * s * np.sign(dx), -1, 0.05), 0.022, 0.01, bone, CLAW)

    # --- the eyes: small, round and black with a glint, above the muzzle ------------------------------------------
    de = unit(0.3, -0.93, 0.15)
    E = sf.on_ell(H, HR, unit(0.27, -0.95, 0.12))
    lay = td.eyes(k, E, de, (0.021, 0.024), srgb(PINK), INK, td.bear_eye(INK, at=(0.25, 0.3), size=0.3))

    k.build(voxel=0.0075, smooth=4, band=0.006)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    bewear(*args())
