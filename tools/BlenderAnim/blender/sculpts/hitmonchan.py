# Hitmonchan sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/hitmonchan.py -- <the export's Hitmonchan.glb> <out.glb>
# From the references: a broad tan boxer, its head a rounded wedge whose top breaks into five flat pointed pads
# fanned up and back like a spread hand; a broad face with large fierce eyes under a heavy brow, no mouth; big
# rounded tan shoulders standing out past a sleeveless lilac tunic with a dark belt and a short pleated skirt;
# thick arms in great red gloves with a rolled cuff, one up beside the chin and one forward at chest height;
# thick legs and big lilac shoes.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import (Kit, ell, tube, sheet, lathe, hull, outward, polygon, srgb, inside, unit, tri, boxy,
                       eye_look, args)

SKIN, TUNIC, BELT, GLOVE, CUFF, SHOE = "#C8AA86", "#D2CAEC", "#3A3442", "#DC4C48", "#C23C3C", "#C9C3E8"
INK, IRIS = "#1A1820", "#2C3C5E"
H = np.array([0.0, -0.03, 0.79])                 # the head's centre (the head bone is at 0, -0.01, 0.68)
CR = H + np.array([0.0, 0.012, 0.06])            # the crown, where the pads take root


def blade(angle, length, width=0.04, thick=0.036, back=10):
    """One flat pad of the crest: a kite of a solid from its root inside the crown to a point, fanned by angle."""
    R = np.array(Euler((math.radians(-back), math.radians(angle), 0)).to_matrix())
    loc = [(-width, -thick / 2, 0), (width, -thick / 2, 0), (-width, thick / 2, 0), (width, thick / 2, 0),
           (-width * 0.6, -thick * 0.4, length * 0.5), (width * 0.6, -thick * 0.4, length * 0.5),
           (-width * 0.6, thick * 0.4, length * 0.5), (width * 0.6, thick * 0.4, length * 0.5),
           (0, -0.004, length), (0, 0.004, length)]
    root = CR + np.array([0.0011 * angle, 0, -0.0004 * abs(angle)])       # the outer pads root lower and to the side
    return hull(np.array([root + R @ np.array(p) for p in loc]))


def hitmonchan(src, out):
    k = Kit(src, coat="fur")

    # The head: a broad rounded wedge, a crown the crest's roots sink into, a heavy brow, a broad chin
    def wedge(V):
        V = boxy(2.3)(V)
        V[:, 0] *= 1 + 0.08 * V[:, 2]                 # a little broader toward the top
        return V
    k.add("skull", ell(H, (0.104, 0.096, 0.1), deform=wedge), SKIN, "head")
    k.add("crown", ell(CR, (0.088, 0.066, 0.06)), SKIN, "head")
    k.add("chin", ell(H + (0, -0.035, -0.072), (0.074, 0.064, 0.05)), SKIN, "head")
    k.add("brow", ell(H + (0.046, -0.09, 0.014), (0.05, 0.03, 0.02), rot=(0, -16, 0)), SKIN, "head", mirror=True)
    for i, (ang, ln) in enumerate(((-48, 0.13), (-24, 0.17), (0, 0.19), (24, 0.17), (48, 0.13))):
        k.add(f"pad{i}", blade(ang, ln), SKIN, "head", fuse=False, surface="fur")

    # The body: a neck, big rounded shoulders, a broad chest and a waist under the tunic
    k.add("neck", ell((0, -0.005, 0.68), (0.06, 0.058, 0.05)), SKIN, "head")
    k.add("trap", ell((0.11, 0.01, 0.655), (0.085, 0.07, 0.055), rot=(0, -20, 0)), SKIN, "body", mirror=True)
    k.add("pad", ell((0.17, 0.0, 0.635), (0.072, 0.068, 0.064)), SKIN, "arm{s}", mirror=True)
    k.add("chest", ell((0, 0.0, 0.575), (0.155, 0.1, 0.105)), SKIN, "body")
    k.add("waist", ell((0, 0.005, 0.48), (0.118, 0.087, 0.07)), TUNIC, "body")
    k.add("hips", ell((0, 0.01, 0.405), (0.125, 0.092, 0.055)), TUNIC, "body")
    # the tunic over the chest from a neckline under the shoulders
    k.paint(lambda P: inside(P[:, 2] - (0.632 - 0.04 * (np.abs(P[:, 0]) / 0.13) ** 2), 0.003)
            * inside(np.abs(P[:, 0]) - 0.15, 0.003), TUNIC, on=["chest", "waist"])

    # The belt: a dark band round the waist
    def belt_at(u, v):
        a = 2 * math.pi * u
        return np.stack([np.sin(a) * 0.122, np.cos(a) * 0.092 + 0.005, 0.462 + (v - 0.5) * 0.032 + 0 * u], -1)
    k.add("belt", outward(sheet(belt_at, 97, 3, 0.01)), BELT, "body", fuse=False, surface="leather")

    # The skirt: short and pleated, flaring from the belt, its hem cut in points; the seam behind
    def skirt_at(u, v):
        a = 2 * math.pi * u
        fold = 1 + 0.05 * np.abs(np.sin(9 * a))
        rx, ry = 0.124 + 0.075 * v, 0.094 + 0.06 * v
        z = 0.455 - 0.12 * v - 0.02 * v * (tri(u * 18) + 1) / 2
        return np.stack([np.sin(a) * rx * fold, np.cos(a) * ry * fold + 0.01, z], -1)
    k.add("skirt", sheet(skirt_at, 145, 7, 0.008), TUNIC, "body", fuse=False, surface="cloth")

    # The legs: thick tan thighs out of the skirt, knees a little bent, big lilac shoes
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", tube(np.array([(0.075, 0.0, 0.4), (0.095, -0.025, 0.22)]) * m, [0.066, 0.048]), SKIN,
              f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.095, -0.025, 0.22), (0.098, -0.005, 0.15), (0.096, 0.0, 0.08)]) * m,
                                  [0.048, 0.042, 0.036]), SKIN, f"leg{side}")
    k.add("shoe", ell((0.098, -0.035, 0.042), (0.064, 0.102, 0.045)), SHOE, "leg{s}", mirror=True, surface="leather")
    k.add("ankle", ell((0.097, -0.005, 0.078), (0.045, 0.047, 0.034)), SHOE, "leg{s}", mirror=True, surface="leather")
    k.paint(lambda P: inside(P[:, 2] - 0.084, 0.003), SHOE, on=["shin", "ankle", "shoe"])

    # The arms: thick, in great red gloves; the model's left (-X) up beside the chin, its right forward at the chest
    ARMS = (("R", 1, (0.23, -0.06, 0.56), (0.175, -0.19, 0.6), (0.145, -0.275, 0.61)),
            ("L", -1, (0.235, -0.04, 0.56), (0.185, -0.1, 0.68), (0.155, -0.15, 0.765)))
    for side, s, el, wr, g in ARMS:
        m = np.array([s, 1, 1.0])
        SH, EL, WR, G = np.array((0.17, 0.0, 0.63)) * m, np.array(el) * m, np.array(wr) * m, np.array(g) * m
        k.add(f"upper{side}", tube([SH, EL], [0.052, 0.044]), SKIN, f"arm{side}")
        k.add(f"fore{side}", tube([EL, WR], [0.044, 0.036]), SKIN, f"arm{side}")
        d = unit(*(G - WR))
        k.add(f"glove{side}", outward(ell(G, (0.088, 0.094, 0.088))), GLOVE, f"arm{side}", fuse=False,
              surface="leather")
        k.add(f"thumbG{side}", outward(ell(G + (-0.06 * s, -0.024, 0.04), (0.032, 0.038, 0.032))), GLOVE,
              f"arm{side}", fuse=False, surface="leather")
        k.add(f"cuff{side}", lathe(G - d * 0.11, d, [(0, 0), (0.046, 0), (0.057, 0.009), (0.057, 0.036),
                                                     (0.05, 0.047), (0, 0.047)], seg=32), CUFF, f"arm{side}",
              fuse=False, surface="leather")

    # The eyes: large fierce almonds under the brow, white with a dark iris and a glint
    CS, CW, CI, CK = srgb(SKIN), srgb("#F4F2EE"), srgb(IRIS), srgb(INK)
    OUT = [(-1.0, -0.25), (-0.45, 0.3), (0.3, 0.68), (0.95, 0.75), (1.0, 0.1), (0.6, -0.6), (0.0, -0.78),
           (-0.6, -0.62)]
    def open_eye(u, v):
        c = np.tile(CW, (len(u), 1))
        c[((u + 0.08) / 0.52) ** 2 + ((v + 0.02) / 0.68) ** 2 < 1] = CI
        c[((u + 0.1) / 0.28) ** 2 + ((v + 0.05) / 0.4) ** 2 < 1] = CK
        c[((u - 0.1) / 0.16) ** 2 + ((v - 0.2) / 0.18) ** 2 < 1] = (1, 1, 1)
        return c
    E = H + np.array([0.046, -0.095, -0.03])
    def lay(state):
        look = eye_look(state, open_eye, CS, CK, line=0.18)
        for s in (1, -1):
            shape = polygon([(u * s, v) for u, v in OUT])
            col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
            k.decal(E * np.array([s, 1, 1]), unit(0.4 * s, -0.91, 0.05), (0.03, 0.03), col, up=(0, 0.1, 1),
                    eps=0.005, outline=shape, rings=20, segs=56)

    k.build(voxel=0.0072, smooth=4, band=0.006)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    hitmonchan(*args())
