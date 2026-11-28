# Spiriduke (No. 10028, Carpathia) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/spiriduke.py -- <the export's Spiriduke.glb> <out.glb>
#
# From its picture (10028-Spiriduke.jpg) and its brief (CP-dex-1-plateau.md, 28): the house sprite grown old, floating.
# Largest first: a tall red knitted cap, its cuff low over the brow, rising and flopping over to -X and hanging down
# past the ear to a little brass bell at shoulder height; the big tan head, wrinkled, with long pointed ears out and
# up; narrow glowing green eyes under heavy brows sloping to the nose; a long drooping nose; a wide sly grin; a thin
# chest in a dark red waistcoat open at the front, trimmed and embroidered in gold; thin arms, the right (+X) holding
# a small clay oil lamp out with its flame lit, the left hanging down with a ring of old iron keys; below the waist
# the body fades into a curl of grey hearth smoke.
#
# Grown from spiridel.py (sibling): the same palette, ears, cap and cuff, nose, almond eyes, brows, smile and flame.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, lathe, outward, srgb, inside, smoothstep, tri, unit, args, sibling)

sp = sibling("spiridel")

SKIN, SMOKE, SMOKEDARK = "#C9A27A", "#8E8886", "#6E6866"
VEST, GOLD, IRON, CLAY, BRASS = "#8A2A2C", "#D8A847", "#5C4C44", "#B46C44", "#D2A44A"
CREASE = "#9A7656"

HC = np.array([0.0, -0.005, 0.748])
HR = np.array([0.144, 0.126, 0.13])
VOXEL = 0.0062


def spiriduke(src, out_path):
    sp.SKIN = SKIN                    # the smile's patch is laid on the old sprite's own skin
    k = Kit(src, coat="skin")

    # ---- the chest: thin, a waistcoat painted on it; a short neck into the head
    TC, TR = np.array([0.0, 0.006, 0.545]), np.array([0.118, 0.09, 0.112])
    k.add("torso", ell(TC, TR), SKIN, "body")
    k.add("neck", tube([(0, 0.0, 0.6), (0, -0.004, 0.668)], [0.062, 0.06]), SKIN, "head")

    # ---- the lower body: a billow of hearth smoke curling to -X and up at its end, two small curls off it
    k.add("smoke", tube([(0, 0.01, 0.47), (0.01, 0.02, 0.39), (0.025, 0.03, 0.31), (0.01, 0.045, 0.24),
                         (-0.05, 0.06, 0.18), (-0.11, 0.07, 0.168), (-0.152, 0.072, 0.205)],
                        [0.104, 0.1, 0.088, 0.07, 0.05, 0.036, 0.027], seg=20), SKIN, "tail")
    k.add("wispR", tube([(0.06, 0.03, 0.33), (0.12, 0.04, 0.312), (0.152, 0.05, 0.345)], [0.04, 0.03, 0.023]),
          SMOKE, "tail")
    k.add("wispL", tube([(-0.06, 0.03, 0.385), (-0.118, 0.04, 0.365), (-0.138, 0.05, 0.402)], [0.036, 0.027, 0.021]),
          SMOKE, "tail")
    k.paint(lambda P: smoothstep(0.45, 0.375, P[:, 2]), SMOKE, on=["smoke", "wisp", "torso"])
    k.paint(lambda P: smoothstep(0.3, 0.17, P[:, 2]) * 0.55, SMOKEDARK, on=["smoke", "wisp"])

    # ---- the waistcoat: dark red round the chest, open down the front, gold at its edges and a gold zigzag
    def opening(P):
        return 0.034 + 0.04 * np.clip((P[:, 2] - 0.45) / 0.18, 0, 1)
    front = lambda P: P[:, 1] < 0.0
    def vest(P):
        z = P[:, 2]
        band = inside(0.452 - z, 0.002) * inside(z - 0.63, 0.004)
        shut = inside(opening(P) - np.abs(P[:, 0]), 0.002) * front(P)
        return band * (1 - shut)
    k.paint(vest, VEST, on=["torso"])
    def trim(P):
        z = P[:, 2]; ax = np.abs(P[:, 0]); o = opening(P)
        edge = inside(np.abs(ax - o - 0.006) - 0.007, 0.0015) * front(P)
        hem = inside(np.abs(z - 0.459) - 0.007, 0.0015)
        zig = inside(np.abs(ax - o - 0.036 - 0.009 * tri(z / 0.034)) - 0.0045, 0.0015) * front(P)
        return (np.maximum(np.maximum(edge, hem), zig) * inside(0.452 - z, 0.002) * inside(z - 0.625, 0.004))
    k.paint(trim, GOLD, on=["torso"])

    # ---- the head, the cap hanging over to -X to its bell
    k.add("head", ell(HC, HR), SKIN, "head")
    sp.cap(k, top=(0.0, 0.02, 0.9), top_r=(0.14, 0.13, 0.095),
           flop=[(0.02, 0.02, 0.91), (-0.05, 0.03, 0.955), (-0.14, 0.05, 0.95), (-0.205, 0.06, 0.875),
                 (-0.243, 0.062, 0.775), (-0.255, 0.055, 0.645), (-0.25, 0.046, 0.565)],
           flop_r=[0.096, 0.082, 0.07, 0.056, 0.044, 0.034, 0.028],
           cuff_c=(0.0, 0.0, 0.852), cuff_axis=(0.04, -0.22, 1.0), r_in=0.106, r_out=0.154, below=-0.04,
           above=0.05, squash=0.93, cut=-0.012)
    BELL = np.array([-0.25, 0.046, 0.548])
    k.add("bell", lathe(BELL, (0, 0, -1), [(0, 0), (0.014, 0.002), (0.026, 0.012), (0.031, 0.03), (0.036, 0.046),
                                          (0.031, 0.052), (0.0, 0.052)]), BRASS, "head", fuse=False, surface="metal")
    k.add("clapper", outward(ell(BELL + (0, 0, -0.056), (0.011, 0.011, 0.011), sub=2)), BRASS, "head", fuse=False,
          surface="metal")

    # ---- the ears: long and pointed, out and up
    for s, side in ((1, "R"), (-1, "L")):
        base, nrm = sp.on_head(s * 1.3, 0.1, HC, HR)
        sp.ear(k, f"ear{side}", base - nrm * 0.016, (s * 0.8, 0.08, 0.6), (s * 0.22, -1.0, 0.14), 0.215, 0.062,
               0.011, SKIN, sp.EARIN, f"ear{side}", cup=0.45, curl=0.03)

    # ---- the nose: longer, drooping further
    sp.nose(k, [(0.0, -0.106, 0.724), (0.005, -0.158, 0.71), (0.012, -0.204, 0.684), (0.016, -0.226, 0.658)],
            [0.022, 0.02, 0.018, 0.017])

    # ---- the right arm: out to +X, holding a little clay oil lamp, its flame lit
    SH, EL, HA = np.array([0.094, -0.004, 0.592]), np.array([0.165, -0.04, 0.53]), np.array([0.226, -0.1, 0.562])
    k.add("upperR", tube([SH, EL], [0.02, 0.018]), SKIN, "armR")
    k.add("foreR", tube([EL, HA], [0.018, 0.017]), SKIN, "armR")
    k.add("handR", ell(HA + (0.006, -0.004, 0.0), (0.026, 0.024, 0.025)), SKIN, "armR")
    LAMP = np.array([0.296, -0.118, 0.546])
    k.add("lamp", lathe(LAMP, (0, 0, 1), [(0, 0), (0.026, 0.002), (0.044, 0.016), (0.046, 0.026), (0.03, 0.036),
                                          (0.014, 0.04), (0.0, 0.04)]), CLAY, "armR", fuse=False, surface="shell")
    k.add("spout", outward(tube([LAMP + (0.03, -0.012, 0.024), LAMP + (0.07, -0.026, 0.036)], [0.015, 0.011])),
          CLAY, "armR", fuse=False, surface="shell")
    k.add("loop", sp.torus(LAMP + (-0.046, 0.012, 0.024), unit(0.3, 1.0, 0.0), 0.018, 0.008, seg=32, nr=10),
          CLAY, "armR", fuse=False, surface="shell")
    sp.flame(k, "flame", LAMP + (0.0, 0.0, 0.036), 0.068, 0.021, "armR", lean=(0.05, 0.0))

    # ---- the left arm: hanging down, a ring of old iron keys from its hand
    SH, EL, HA = np.array([-0.094, -0.004, 0.592]), np.array([-0.158, -0.012, 0.5]), np.array([-0.196, -0.048, 0.42])
    k.add("upperL", tube([SH, EL], [0.02, 0.018]), SKIN, "armL")
    k.add("foreL", tube([EL, HA], [0.018, 0.017]), SKIN, "armL")
    k.add("handL", ell(HA + (-0.002, -0.004, -0.006), (0.026, 0.025, 0.027)), SKIN, "armL")
    RING = np.array([-0.2, -0.052, 0.37])
    ra = unit(0.3, -1.0, 0.0)
    k.add("keyring", sp.torus(RING, ra, 0.044, 0.016, seg=40, nr=12), IRON, "armL", fuse=False, surface="metal")
    across = np.cross(ra, (0, 0, 1.0)); across /= np.linalg.norm(across)
    for i, (dx, length, sw) in enumerate([(-0.046, 0.105, -0.25), (-0.015, 0.125, -0.08), (0.016, 0.115, 0.08),
                                          (0.047, 0.098, 0.25)]):
        top = RING + across * dx + np.array([0, 0, -0.04 + abs(dx) * 0.4]) - ra * (0.004 * i)
        dn = unit(sw, 0.0, -1.0)
        bowc = top + dn * 0.016
        k.add(f"bow{i}", sp.torus(bowc, ra, 0.02, 0.016, seg=28, nr=10), IRON, "armL", fuse=False,
              surface="metal")
        a = bowc + dn * 0.03; b = bowc + dn * length
        k.add(f"shaft{i}", outward(tube([a, b], [0.016, 0.016], seg=12)), IRON, "armL", fuse=False,
              surface="metal")
        side = np.cross(dn, ra); side /= np.linalg.norm(side)
        p = b - dn * 0.012
        k.add(f"bit{i}", sp.hull([p - ra * 0.016, p + ra * 0.016, p + dn * 0.026 - ra * 0.016,
                                  p + dn * 0.026 + ra * 0.016, p + side * 0.034 - ra * 0.016,
                                  p + side * 0.034 + ra * 0.016, p + side * 0.034 + dn * 0.026 - ra * 0.016,
                                  p + side * 0.034 + dn * 0.026 + ra * 0.016]),
              IRON, "armL", fuse=False, surface="metal")

    # ---- the face: heavy brows, a crease under each eye, the wide sly grin (every state)
    lay, ep, en, right, vup = sp.lay_eyes(k, HC, HR, 0.44, -0.1, (0.032, 0.021),
                                          [(-1.0, -0.12), (-0.55, 0.26), (0.05, 0.5), (0.7, 0.72), (1.0, 0.36),
                                           (0.82, -0.36), (0.25, -0.74), (-0.45, -0.58)],
                                          SKIN, sp.BROW)
    sp.brows(k, ep, en, right, vup, (0.038, 0.01), 0.022, sp.BROW, tilt=28, inward=0.12)
    mx = np.array([-1, 1, 1.0])
    for s in (1, -1):
        p, n = sp.on_head(s * 0.66, -0.3, HC, HR)
        k.decal(p, unit(s * 0.5, -1.0, -0.1), (0.022, 0.0048), srgb(CREASE), up=unit(-s * 0.35, 0.0, 1.0),
                eps=0.002, rings=4, segs=32)
    p, n = sp.on_head(0.07, -0.52, HC, HR)
    sp.smile(k, p, unit(0.12, -0.7, -1.0), (0.062, 0.013))

    k.build(voxel=VOXEL, smooth=4, band=0.008, wband=0.04)
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    spiriduke(*args())
