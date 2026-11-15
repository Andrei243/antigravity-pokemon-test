# Ampharos sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Mareep's script (mareep.py, imported as sibling("mareep")): the same banded horn ears made of lathes, the same banded
# loose tail with a glowing ball, the same dark round eyes with a white glint; the wool is gone.
#   blender --background --factory-startup --python sculpts/ampharos.py -- <the export's Ampharos.glb> <out.glb>
#
# The design, from the references: a tall yellow biped with no wool; a pear of a body with a great white belly from
# the chest down between the legs; thick thighs fused into the hips, short shins, feet with a small white toe at the
# front; flipper arms tapering to a point; a long thick neck ringed black three times near the top; a head with a long
# rounded muzzle standing forward, small dark eyes, a red jewel glowing on the brow; ears like horns rising up and out,
# banded black in the middle and at the point; a long tail banded black, curling out behind and to one side, a
# glowing red ball at its end.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, ring_band, srgb, inside, unit, args, sibling

m = sibling("mareep")
YELLOW, BELLY, BLACK = "#F6CC2E", "#F4F2EA", m.BLACK
JEWEL, WHITE = "#E8263A", "#FAFAFA"


def pear(V):
    V = V.copy()
    s = 1 + 0.1 * np.clip(-V[:, 2], 0, 1) - 0.22 * np.clip(V[:, 2], 0, 1) ** 1.4
    V[:, 0] *= s; V[:, 1] *= s
    return V


def ampharos(src, out):
    k = Kit(src, coat="scales")
    B = np.array([0, 0.0, 0.33])

    # The body: a pear, fullest low down, shoulders narrowing into the neck
    k.add("body", ell(B, (0.135, 0.118, 0.175), deform=pear), YELLOW, "body")
    k.add("shoulders", ell((0, -0.005, 0.462), (0.098, 0.082, 0.07)), YELLOW, "body")

    # Thick thighs fused into the hips, short shins, broad feet with a white toe at the front
    for s, bone in ((1, "legR"), (-1, "legL")):
        x = np.array([s, 1, 1.0])
        k.add("thigh" + bone[-1], ell(np.array((0.088, -0.01, 0.205)) * x, (0.078, 0.088, 0.095)), YELLOW, bone)
        k.add("shin" + bone[-1], tube([np.array(p) * x for p in [(0.094, -0.018, 0.16), (0.098, -0.03, 0.05)]],
                                      [0.05, 0.043]), YELLOW, bone)
        k.add("foot" + bone[-1], ell(np.array((0.1, -0.05, 0.028)) * x, (0.05, 0.072, 0.03)), YELLOW, bone)
        t = np.array((0.1 * s, -0.116, 0.018))
        k.add("toe" + bone[-1], hull([t + (0, -0.012, -0.004), t + (0.02, 0.012, -0.014), t + (-0.02, 0.012, -0.014),
                                      t + (0.018, 0.012, 0.012), t + (-0.018, 0.012, 0.012), t + (0, 0.0, 0.014),
                                      t + (0.014, 0.03, 0.0), t + (-0.014, 0.03, 0.0)]), WHITE, bone, fuse=False,
              surface="shell")

    # The flipper arms: broad at the shoulder, tapering to a point, carried forward and down, faces to the front
    for s, bone in ((1, "armR"), (-1, "armL")):
        x = np.array([s, 1, 1.0])
        k.add("arm" + bone[-1], blade([np.array(p) * x for p in [(0.095, -0.03, 0.46), (0.15, -0.085, 0.42),
                                                                   (0.195, -0.135, 0.38), (0.22, -0.162, 0.355)]],
                                      [0.05, 0.044, 0.03, 0.012], np.array((0.45, -0.55, 0.7)) * x, 0.48),
              YELLOW, bone)

    # The white belly from the chest down between the legs: a crisp oval on the front
    def belly(P):
        e = ((P[:, 0] / 0.102) ** 2 + ((P[:, 2] - 0.325) / 0.165) ** 2)
        return inside(np.sqrt(e) - 1, 0.012) * (P[:, 1] < -0.02) * (P[:, 2] > 0.15)
    k.paint(belly, BELLY)

    # The long thick neck, three black rings near its top
    k.add("necklow", tube([(0, 0.0, 0.45), (0, -0.008, 0.56)], [0.064, 0.054]), YELLOW, "body")
    NECK = [(0, -0.008, 0.54), (0, -0.016, 0.63), (0, -0.026, 0.72)]
    k.add("neckup", tube(NECK, [0.054, 0.05, 0.05]), YELLOW, "head")
    for z in (0.585, 0.625, 0.665):
        yy = -0.008 - 0.018 * (z - 0.54) / 0.18
        k.add(f"ring{int(z * 1000)}", ring_band((0, yy, z), (0, -0.1, 1), 0.0505, 0.022, thick=0.007), BLACK, "head",
              fuse=False, surface="scales")

    # The head: a round skull, a long rounded muzzle standing forward and a little down
    H, HR = np.array([0, -0.032, 0.765]), np.array([0.082, 0.085, 0.08])
    HEAD = ell(H, HR)
    k.add("head", HEAD, YELLOW, "head")
    k.add("muzzle", tube([(0, -0.07, 0.758), (0, -0.14, 0.738), (0, -0.182, 0.72)], [0.056, 0.043, 0.034]), YELLOW,
          "head")
    k.add("jowl", ell((0, -0.06, 0.725), (0.062, 0.06, 0.045)), YELLOW, "head")

    # The jewel on the brow, glowing, with a white glint
    gd = unit(0, -0.5, 0.86)
    gp = m.hit(HEAD, H + gd * 0.4, -gd)
    k.add("jewel", ell(gp + gd * 0.012, (0.028, 0.028, 0.028), sub=4), JEWEL, "head", fuse=False, glow=JEWEL,
          surface="shell")

    # The ears: horns rising up and out, banded black in the middle and at the point
    m.ear_cone(k, "ear", np.array([0.042, -0.03, 0.805]), unit(0.42, 0.14, 0.9), 0.122, 0.035, YELLOW,
               [(0.36, 0.53, BLACK), (0.72, 1.0, BLACK)], "ear{s}")

    # The tail: long and thick at the root, out behind and curling to +X, banded black, the red ball at its end
    m.banded_tail(k, "tail", [(0, 0.075, 0.24), (0.05, 0.19, 0.17), (0.14, 0.27, 0.165), (0.212, 0.29, 0.255),
                              (0.236, 0.268, 0.365)], 0.042, 0.022, YELLOW,
                  [(0.3, 0.4), (0.5, 0.6), (0.7, 0.79), (0.88, 0.97)], BLACK, 0.038, JEWEL)

    k.build(voxel=0.0075, smooth=4, band=0.005)

    de = unit(0.55, -0.76, 0.3)
    E = m.hit(HEAD, H + np.array([0, -0.01, 0.008]) + de * 0.4, -de)
    k.eye_states(m.lay_dark_eyes(k, E, de, (0.019, 0.024), YELLOW))
    k.export(out)


if __name__ == "__main__":
    ampharos(*args())
