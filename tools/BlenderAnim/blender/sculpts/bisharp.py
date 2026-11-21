# Bisharp sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/bisharp.py -- <the export's Bisharp.glb> <out.glb>
# Grown from pawniard.py (sibling): its colours, its helmet painted with a red crown and a dark visor, its eyes peering
# from under the rim, its knife blades and its bands of blade round the body; what the pictures change.
#
# From the references: a tall armoured warrior. The same round helmet, red on top and grey at the sides, the grey
# guards hanging low; a gold axe-head crest standing on its brow, a crescent whose horns point up; a gold chin under
# the visor. Great round red pauldrons, a red chest with a dark grey spine down its middle, a narrow grey waist with two
# white blades wrapped round it and flaring out like a disc. Grey upper arms; each forearm sheathed in a great white
# curved blade. Big round red hips like puffed breeches, the grey spine running down between them; short red shins
# ending in white blade boots, a long point forward and a spur behind. The brief's "yellow-and-black" crest is gold
# alone in both pictures.
# Skeleton: body (0, 0, 0.38), head (0, -0.01, 0.55), arms (+-0.08, 0, 0.52), legs (+-0.045, 0, 0.27).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, srgb, inside, unit, args, sibling

pw = sibling("pawniard")
RED, GREY, DARK, SEAM, BODY, BLADE = pw.RED, pw.GREY, pw.DARK, pw.SEAM, pw.BODY, pw.BLADE
GOLD = "#E8B830"

HC, HR = np.array([0.0, -0.012, 0.64]), np.array([0.087, 0.08, 0.08])
SPINE_C, SPINE_R = np.array([0.0, 0.0, 0.43]), (0.042, 0.036, 0.13)
SHOULDER = np.array([0.085, 0.0, 0.515])
ELBOW = np.array([0.13, -0.035, 0.445])
HAND = np.array([0.15, -0.105, 0.41])
HIP = np.array([0.058, 0.0, 0.262])
KNEE = np.array([0.07, 0.0, 0.165])
ANKLE = np.array([0.066, -0.008, 0.075])


def bisharp(src, out):
    k = Kit(src, coat="metal")

    # --- the head: pawniard's helmet, grown --------------------------------------------------------------------
    def helmet(V):
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] < -0.4, -0.4 + (V[:, 2] + 0.4) * 0.6, V[:, 2])
        return V
    k.add("head", ell(HC, HR, deform=helmet), GREY, "head")
    k.add("cheekguard", ell(HC + (0.054, -0.004, -0.036), (0.022, 0.046, 0.044), rot=(0, -10, 0)), GREY, "head",
          mirror=True)
    k.add("neck", tube([(0, 0.0, 0.53), (0, -0.006, 0.6)], [0.03, 0.028]), DARK, "head")
    # the gold chin under the visor
    k.paint(lambda P: inside((P[:, 2] - HC[2]) + 0.048, 0.002) * inside(np.abs(P[:, 0]) - 0.03, 0.002)
            * inside(P[:, 1] - HC[1] + 0.03, 0.003) * inside(-(P[:, 2] - HC[2]) - 0.085, 0.002), GOLD, on=["head"])

    def crown(P):
        rel = P - HC
        half = 0.04 + 0.01 * np.clip(-rel[:, 1] / 0.07, 0, 1)
        return inside(np.abs(rel[:, 0]) - half, 0.002) * inside(-(rel[:, 2] + 0.004 + 0.012 * np.clip(rel[:, 1] / 0.07, 0, 1)), 0.002)
    k.paint(crown, RED, on=["head"])

    def visor(P):
        rel = P - HC
        return (inside(rel[:, 2] + 0.006, 0.002) * inside(-(rel[:, 2] + 0.042), 0.002)
                * inside(np.abs(rel[:, 0]) - 0.058, 0.003) * inside(rel[:, 1] + 0.035, 0.004))
    k.paint(visor, DARK, on=["head"])

    # the gold axe-head crest on the brow: a crescent, its face to the front, horns up; laid a little back
    ex, ez = np.array([1.0, 0, 0]), unit(0, 0.35, 1)
    axe = [(-0.012, 0.0), (0.012, 0.0), (0.016, 0.03), (0.045, 0.05), (0.06, 0.085), (0.052, 0.112), (0.038, 0.09),
           (0.018, 0.075), (-0.005, 0.078), (-0.03, 0.095), (-0.048, 0.122), (-0.06, 0.09), (-0.042, 0.052),
           (-0.016, 0.03)]
    k.add("crest", pw.slab(axe, HC + (0, -0.05, 0.035), ex, ez, 0.016), GOLD, "head", fuse=False)

    # --- the body ----------------------------------------------------------------------------------------------
    k.add("spine", ell(SPINE_C, SPINE_R), BODY, "body")
    k.add("chest", ell((0, 0.002, 0.5), (0.07, 0.048, 0.05)), RED, "body")
    k.add("pauldron", ell(SHOULDER + (0.006, 0, 0.004), (0.054, 0.056, 0.048)), RED, "arm{s}", mirror=True)
    # the grey spine down the chest's middle, and its seams
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.022, 0.0015) * inside(P[:, 2] - 0.56, 0.002), BODY,
            on=["chest", "spine"])
    for z in (0.37, 0.41, 0.46, 0.5):
        k.paint(lambda P, z=z: inside(np.abs(P[:, 2] - z) - 0.003, 0.0015) * inside(np.abs(P[:, 0]) - 0.024, 0.0015),
                SEAM, on=["spine", "chest"])
    # gold trim at the pauldrons' inner edge
    k.paint(lambda P: inside(np.abs(np.abs(P[:, 0]) - 0.047) - 0.004, 0.0015) * inside(P[:, 1] + 0.01, 0.003)
            * inside(np.abs(P[:, 2] - 0.51) - 0.03, 0.002), GOLD, on=["pauldron", "chest"])
    # two white blades wrapped round the waist, flaring out like a disc
    for name, z, start, span, rise in (("waistblade", 0.4, -160, 300, -0.01), ("hipblade", 0.355, 160, -300, 0.008)):
        f = math.sqrt(max(1 - ((z - SPINE_C[2]) / SPINE_R[2]) ** 2, 0.2))
        k.add(name, pw.band(SPINE_C, SPINE_R[0] * f + 0.008, SPINE_R[1] * f + 0.008, z, 0.036, 0.016, span=span,
                            start=start, flare=0.9, rise=rise), BLADE, "body", fuse=False)

    # --- the arms: grey upper arms, each forearm sheathed in a great white curved blade -------------------------
    k.add("arm", tube([SHOULDER, ELBOW, HAND], [0.024, 0.022, 0.022]), DARK, "arm{s}", mirror=True)
    k.add("fist", ell(HAND + (0.004, -0.012, -0.004), (0.024, 0.026, 0.024)), DARK, "arm{s}", mirror=True)
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        e, h = ELBOW * m, HAND * m
        d = unit(*(h - e))
        up = np.array([0, 0, 1.0]) - d * d[2]; up /= np.linalg.norm(up)
        ow = np.cross(d, up)
        ow = ow / np.linalg.norm(ow)
        if ow[0] * s < 0:
            ow = -ow
        # a great lens-shaped sheath round the forearm, pointed past the hand and up behind the elbow
        sheath = [e - d * 0.075 + up * 0.075 + ow * 0.012,
                  e - d * 0.02 + up * 0.05 + ow * 0.03, e - d * 0.02 + up * 0.05 - ow * 0.022,
                  e + d * 0.05 + up * 0.045 + ow * 0.034, e + d * 0.05 + up * 0.045 - ow * 0.024,
                  e + d * 0.05 - up * 0.04 + ow * 0.034, e + d * 0.05 - up * 0.04 - ow * 0.024,
                  e - d * 0.01 - up * 0.03 + ow * 0.026, e - d * 0.01 - up * 0.03 - ow * 0.02,
                  h + d * 0.01 + up * 0.03 + ow * 0.03, h + d * 0.01 + up * 0.03 - ow * 0.02,
                  h + d * 0.01 - up * 0.03 + ow * 0.03, h + d * 0.01 - up * 0.03 - ow * 0.02,
                  h + d * 0.09 + up * 0.01 + ow * 0.01]
        k.add(f"guard{side}", hull(sheath), BLADE, f"arm{side}", fuse=False)

    # --- the legs: round red hips like breeches, short red shins, white blade boots ----------------------------
    k.add("pelvis", ell((0, 0.0, 0.31), (0.06, 0.05, 0.045)), RED, "body")
    k.add("hip", ell(HIP + (0.012, 0, -0.025), (0.066, 0.066, 0.09)), RED, "leg{s}", mirror=True)
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.018, 0.0015) * (P[:, 1] < 0) * inside(P[:, 2] - 0.34, 0.002)
            * inside(0.2 - P[:, 2], 0.002), BODY, on=["pelvis", "hip"])
    k.add("shin", tube([KNEE, ANKLE], [0.032, 0.024]), RED, "leg{s}", mirror=True)
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        a = ANKLE * m
        boot = [a + (0.026, 0.02, 0.03), a + (-0.026, 0.02, 0.03), a + (0.026, -0.02, 0.035), a + (-0.026, -0.02, 0.035),
                a + (0.022, -0.03, -0.06), a + (-0.022, -0.03, -0.06), a + (0.014, -0.11, -0.075), a + (-0.014, -0.11, -0.075),
                a + (0, -0.14, -0.072), a + (0.02, 0.03, -0.075), a + (-0.02, 0.03, -0.075)]
        k.add(f"boot{side}", hull(boot), BLADE, f"leg{side}", fuse=False)
        spur = [a + (0.014, 0.02, 0.0), a + (-0.014, 0.02, 0.0), a + (0.014, 0.03, -0.07), a + (-0.014, 0.03, -0.07),
                a + (0, 0.085, -0.074)]
        k.add(f"spur{side}", hull(spur), BLADE, f"leg{side}", fuse=False)

    k.build(voxel=0.0065, smooth=3, band=0.008)

    EC, EN = pw.on_ell(HC, HR, (0.42, -0.86, -0.3))
    def lay(state):
        pw.lay_eyes(k, state, EC, EN, (0.019, 0.014))
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    bisharp(*args())
