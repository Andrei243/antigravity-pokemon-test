# Bradonor (No. 10077, Carpathia) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/bradonor.py -- <the export's Bradonor.glb> <out.glb>
#
# Bradlet grown (sibling("bradlet")): the same tiers of boughs with their drooping points and the shade under each hem,
# the same bark and pine cones, the face in a hollow of shade under a hem. From its picture
# (docs/art/reference/gemini/carpathia/10077-Bradonor.jpg): a tall snow fir, five tiers, the tip capped in snow and
# snow heaped on every tier's slope, icicles hanging from the hems; two glowing frost-blue eyes deep under the third
# hem and a great beard of frost falling from under them over the lower boughs; the trunk splitting into two strong
# root-legs, their roots spread on the ground; two bough arms, brown branches under green boughs heavy with snow and
# hung with icicles, ending in twig hands; a twig rising from the right (+X) forearm; small pine cones in the lowest
# boughs. The art flings the arms out and down: carried forward and down here.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, tri, unit, eye_look, lumpy, polygon, sibling, args

bl = sibling("bradlet")

SNOW, SNOW_SHADE, ICE, FROST = "#EEF4F8", "#B8D2E4", "#8CC6E6", "#D6E8F2"
EYE_GLOW, EYE_CORE, EYE_RIM = "#5CCDF2", "#C4F4FF", "#0E2A32"

# hem height, hem radius, top height, top radius, points, point length, point radius
TIERS = [(1.46, 0.22, 1.745, 0.0, 10, 0.12, 0.042),
         (1.27, 0.33, 1.56, 0.13, 12, 0.15, 0.05),
         (1.08, 0.42, 1.38, 0.2, 14, 0.17, 0.056),
         (0.84, 0.46, 1.16, 0.26, 16, 0.18, 0.06),
         (0.62, 0.40, 0.92, 0.30, 15, 0.17, 0.058)]


def icicle(k, name, root, length, r, bone):
    """A hanging icicle: a five-sided spike with a hard edge, loose so it keeps its point."""
    root = np.array(root, float)
    pts = [root + (r * math.cos(a), r * math.sin(a), 0.02) for a in np.linspace(0, 2 * math.pi, 5, endpoint=False)]
    pts += [root + (r * 0.6 * math.cos(a + 0.6), r * 0.6 * math.sin(a + 0.6), -length * 0.45) for a in
            np.linspace(0, 2 * math.pi, 5, endpoint=False)]
    pts.append(root - (0, 0, length))
    k.add(name, hull(pts), ICE, bone, fuse=False, surface="shell")


def snow_on_tier(k, i, zh, rh, zt, on):
    """Snow heaped on a tier's slope above its hem: patches with a wavy top edge, the gaps green."""
    def m(P, zh=zh, zt=zt, i=i):
        a = np.arctan2(P[:, 0], -P[:, 1])
        top = zh + 0.065 + 0.022 * np.sin(a * 5 + i) + 0.014 * np.sin(a * 11 + 2 * i)
        patch = (np.sin(a * 3 + 1.7 * i) > -0.1).astype(float)
        return inside(P[:, 2] - top, 0.006) * inside((zh + 0.012) - P[:, 2], 0.006) * patch
    k.paint(m, SNOW, on=on)


def bradonor(src, out):
    k = Kit(src, coat="leaf")
    for i, (zh, rh, zt, rt, n, L, r) in enumerate(TIERS):
        bl.fir_tier(k, f"tier{i}", zh, rh, zt, rt, n, 0.37 * i + 0.2, L, r,
                    gap=(0, 24) if i == 2 else ((0, 34) if i == 3 else None))
    for i, (zh, rh, zt, rt, n, L, r) in enumerate(TIERS):
        k.paint(lambda P, zh=zh, zt=zt: inside((zh + 0.35 * (zt - zh)) - P[:, 2], 0.03)
                * inside(P[:, 2] - zt + 0.03, 0.03), bl.LIGHT, on=[f"tier{i}"])
    for i in range(1, 5):
        m, col, on = bl.under_hem(TIERS[i - 1][0], TIERS[i - 1][1], 0.08, [f"tier{i}"])
        k.paint(m, col, on=on)
    for i, (zh, rh, zt, rt, n, L, r) in enumerate(TIERS):
        if i:
            snow_on_tier(k, i, zh, rh, zt, [f"tier{i}"])
    # the tip's cap of snow, its lower edge ragged
    k.paint(lambda P: inside((1.575 + 0.025 * tri(np.arctan2(P[:, 0], P[:, 1]) * 4 / math.pi)) - P[:, 2], 0.005),
            SNOW, on=["tier0"])
    # lumps of snow standing out at the tiers' sides, where the picture heaps it
    for i, (zh, rh, zt, rt, n, L, r) in enumerate(TIERS[1:], 1):
        for j, a in enumerate((-118, 112, 175)):
            t = math.radians(a + 9 * i)
            o = np.array([math.sin(t), -math.cos(t), 0])
            c = o * rh * 0.86 + (0, 0, zh + 0.055)
            k.add(f"lump{i}_{j}", ell(c, (rh * 0.34, rh * 0.2, 0.03), rot=(0, 0, a + 9 * i), deform=lumpy(3, 2, 0.12)),
                  SNOW, "body", surface="fur")
    # icicles from every hem
    for i, (zh, rh, zt, rt, n, L, r) in enumerate(TIERS):
        for j in range(7 if i else 5):
            a = 0.5 + 2 * math.pi * j / (7 if i else 5) + 0.4 * i
            deg = (math.degrees(a) + 180) % 360 - 180
            if i in (2, 3) and abs(deg) < 38:
                continue                        # the face and the beard
            o = np.array([math.sin(a), -math.cos(a), 0])
            icicle(k, f"ice{i}_{j}", o * rh * 0.93 + (0, 0, zh - 0.005), 0.1 + 0.07 * ((j * 3 + i) % 3) / 2, 0.026,
                   "body")

    # --- the face: a hollow of shade under the third hem, the beard of frost below it ---
    k.paint(lambda P: inside(((P[:, 0] / 0.2) ** 2 + ((P[:, 2] - 1.02) / 0.085) ** 2) - 1, 0.012)
            * (P[:, 1] < -0.2), bl.DEEP, on=["tier3"])
    k.add("tache", ell((0, -0.365, 0.94), (0.16, 0.06, 0.042), deform=lumpy(4, 2, 0.08)), FROST, "head", surface="fur")
    LOCKS = [(-0.2, 0.8, 0.045), (-0.15, 0.73, 0.055), (-0.1, 0.67, 0.06), (-0.05, 0.63, 0.065), (0.0, 0.6, 0.07),
             (0.05, 0.63, 0.065), (0.1, 0.67, 0.06), (0.15, 0.73, 0.055), (0.2, 0.8, 0.045)]
    for j, (x, zend, r) in enumerate(LOCKS):
        k.add(f"beard{j}", tube([(x * 0.6, -0.36, 0.935), (x * 0.95, -0.45, 0.86), (x * 1.1, -0.5, (0.86 + zend) / 2),
                                 (x * 1.18, -0.52, zend + 0.03)], [r, r * 0.95, r * 0.7, 0.016]),
              FROST, "head", surface="fur")
    # the beard pale at the top, icy in its lower half and its points
    k.paint(lambda P: inside(P[:, 2] - 0.8, 0.03) * (P[:, 1] < -0.4) * (np.abs(P[:, 0]) < 0.26), ICE, on=["beard"])
    k.paint(lambda P: inside(np.abs(tri(P[:, 0] * 14)) - 0.25, 0.04) * inside(0.92 - P[:, 2], 0.02)
            * inside(P[:, 2] - 0.74, 0.02) * (P[:, 1] < -0.4), SNOW_SHADE, on=["beard"])
    k.paint(lambda P: inside(0.93 - P[:, 2], 0.01) * (P[:, 1] < -0.3), SNOW, on=["tache", "beard"])

    # --- the trunk and the two root-legs ---
    k.add("trunk", tube([(0, 0.0, 0.95), (0, 0.0, 0.78), (0, 0.0, 0.62)], [0.17, 0.165, 0.16]), bl.TRUNK, "body",
          surface="shell")
    for s, sd in ((1, "R"), (-1, "L")):
        foot = np.array([0.25 * s, -0.03, 0.125])
        k.add(f"leg{sd}", tube([(0.07 * s, 0.0, 0.66), (0.16 * s, -0.01, 0.38), foot],
                               [0.16, 0.135, 0.12]), bl.TRUNK, f"leg{sd}", surface="shell")
        for j, (dx, dy) in enumerate(((0.12, -0.12), (-0.02, -0.17), (0.17, 0.02), (0.06, 0.15))):
            tip = foot + (dx * s, dy, -0.075)
            k.add(f"root{sd}{j}", tube([foot + (0, 0, 0.02), (foot + tip) / 2 + (0, 0, 0.01), tip], [0.075, 0.05, 0.03]),
                  bl.TRUNK, f"leg{sd}", surface="shell")
    bl.bark(k, (0, 0), 8, ["trunk"], 0.72)
    for s, sd in ((1, "R"), (-1, "L")):
        bl.bark(k, (0.17 * s, -0.01), 6, [f"leg{sd}", f"root{sd}"], 0.76)

    # --- the bough arms: a branch under a green bough, snow on top, icicles under, twig fingers ---
    for s, sd in ((1, "R"), (-1, "L")):
        arm = f"arm{sd}"
        SH, EL, WR = np.array([0.26 * s, -0.02, 0.97]), np.array([0.44 * s, -0.12, 0.8]), np.array([0.53 * s, -0.24, 0.6])
        k.add(f"branch{sd}", tube([SH, EL, WR], [0.075, 0.063, 0.055]), bl.TRUNK, arm, surface="shell")
        k.add(f"bough{sd}", ell((0.42 * s, -0.1, 0.85), (0.22, 0.13, 0.085), rot=(14, 40 * s, -32 * s),
                                deform=lumpy(4, 3, 0.1)), bl.FRINGE, arm, surface="leaf")
        k.add(f"armsnow{sd}", ell((0.405 * s, -0.095, 0.915), (0.17, 0.1, 0.04), rot=(14, 40 * s, -32 * s),
                                  deform=lumpy(3, 2, 0.12)), SNOW, arm, surface="fur")
        # needle points hanging off the bough's edge
        for j, t in enumerate((0.15, 0.4, 0.65, 0.9)):
            root = SH + (EL - SH) * t * 1.4 + (0, -0.06, -0.04)
            tip = root + (0.05 * s, -0.05, -0.12)
            k.add(f"boughpt{sd}{j}", blade([root, (root + tip) / 2, tip], [0.05, 0.035, 0.01], (0, 0, 1), 0.6),
                  bl.FRINGE, arm, surface="leaf")
        for j, (x, z, ln) in enumerate(((0.32, 0.86, 0.13), (0.45, 0.76, 0.17), (0.55, 0.66, 0.12))):
            icicle(k, f"armice{sd}{j}", (x * s, -0.18, z), ln, 0.024, arm)
        # twig fingers hooking down
        for j, (dx, dy, dz) in enumerate(((0.05, -0.04, -0.13), (0.0, -0.09, -0.14), (-0.06, -0.08, -0.11),
                                          (0.08, 0.03, -0.1))):
            tip = WR + (dx * s, dy, dz)
            k.add(f"finger{sd}{j}", tube([WR, WR + (dx * 0.6 * s, dy * 0.6, dz * 0.4), tip], [0.04, 0.032, 0.024]),
                  bl.TRUNK, arm, surface="shell")
    # the twig rising from the right forearm
    k.add("twig", tube([(0.47, -0.17, 0.72), (0.56, -0.18, 0.84), (0.6, -0.19, 0.95)], [0.035, 0.028, 0.022]),
          bl.TRUNK, "armR", surface="shell")
    k.add("twig2", tube([(0.56, -0.18, 0.84), (0.66, -0.2, 0.9)], [0.026, 0.022]), bl.TRUNK, "armR", surface="shell")

    # --- pine cones in the lowest boughs ---
    for j, (x, y, z) in enumerate(((0.2, -0.33, 0.55), (-0.2, -0.33, 0.56), (0.34, -0.17, 0.53), (-0.33, -0.18, 0.52),
                                   (0.12, 0.36, 0.55))):
        k.add(f"conestalk{j}", tube([(x * 0.85, y * 0.85, z + 0.09), (x, y, z + 0.04)], [0.022, 0.02]), bl.TRUNK,
              "body", surface="shell")
        bl.pine_cone(k, f"cone{j}", (x, y, z), 0.05, "body")

    k.build(voxel=0.0125, smooth=4, band=0.012, wband=0.08)

    def glow_eye(u, v):
        c = np.tile(srgb(EYE_RIM), (len(u), 1))
        c[(u / 0.86) ** 2 + (v / 0.8) ** 2 < 1] = srgb(EYE_GLOW)
        c[((u + 0.08) / 0.38) ** 2 + ((v + 0.05) / 0.32) ** 2 < 1] = srgb(EYE_CORE)
        return c
    # an almond, its outer corner a little up: calm and stern
    OUT = polygon([(-1, -0.15), (-0.55, -0.7), (0.3, -0.75), (1.0, -0.15), (0.75, 0.55), (0.0, 0.85), (-0.7, 0.45)])
    EYE_C, EYE_N, EYE_H = np.array([0.08, -0.322, 1.025]), unit(0.3, -0.95, 0.05), (0.064, 0.038)
    def eyes(state):
        look = glow_eye if state == "Open" else eye_look(state, glow_eye, srgb(bl.DEEP), srgb(EYE_GLOW), line=0.2)
        k.decal(EYE_C, EYE_N, EYE_H, look, eps=0.004, outline=OUT if state in ("Open", "Fierce") else None,
                rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    bradonor(*args())
