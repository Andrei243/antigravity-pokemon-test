# Rapidash-Galar sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rapidashgalar.py -- <the export's Rapidash-Galar.glb> <out.glb>
# Grown from ponytagalar.py through sibling (its trunk, neck, head, ears, horn, legs, hooves, nostrils and colours),
# as rapidash.py is Ponyta grown: a longer neck and head, longer legs. From the pictures: a white unicorn with a long
# dark violet horn wound with white spiral bands; a stern half-lidded eye, pale teal under a heavy dark lid; the mane
# its largest mass, long thick locks of lilac and turquoise from the crown down both sides of the neck past the chest,
# rolled into curls at their ends, a turquoise crest over the nape and a lilac forelock; a long tail of the same
# locks falling back and down to a curl; wavy lilac fetlocks over dark violet hooves, trailing back along the ground.
# The skeleton: body (0, 0.02, 0.5), head (0, -0.202, 0.6), ears (+-0.04, -0.302, 0.86), mane (0, -0.269, 0.82),
# legs (+-0.07, -0.179, 0.42), hinds (+-0.07, 0.224, 0.42), tail (0, 0.269, 0.56).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, lumpy, srgb, inside, smoothstep, unit, eye_look, outward, polygon,
                       sibling, args)

p = sibling("ponytagalar")
WHITE, LILAC, PERI, TURQ, HOOF, EARIN = p.WHITE, p.LILAC, p.PERI, p.TURQ, p.HOOF, p.EARIN
LILAC2 = "#DDB6EC"                       # the deeper lilac inside a lock
H = np.array([0, -0.352, 0.818])
HR = np.array([0.072, 0.1, 0.072])
LEG_X, FORE_Y, HIND_Y = 0.071, -0.179, 0.226


def lock(k, name, pts, radii, col, bone, flat=0.7, curl=None, curl_r=0.05, mirror=True):
    """
    A thick lock of mane: a tube flattened across its own fall (its face turned to the side), ending in a rolled curl
    (a ball where it turns back on itself).
    """
    P = np.array(pts, float)
    k.add(name, blade(P, list(radii), (1, 0, 0), flat), col, bone, mirror=mirror)
    if curl is not None:
        k.add(name + "curl", ell(curl, (curl_r * 0.8, curl_r, curl_r), deform=lumpy(3, 2, 0.05)), col, bone,
              mirror=mirror)


def rapidash_galar(src, out):
    k = Kit(src, coat="fur")
    p.trunk(k, Z=0.53, chest=-0.155, rump=0.19, rx=0.112, rz=0.135)
    p.neck(k, [(0, -0.15, 0.58), (0, -0.245, 0.69), (0, -0.305, 0.785)], [0.094, 0.076, 0.064])
    p.head(k, h=H, hr=HR, muzzle=((0, -0.448, 0.776), (0.052, 0.086, 0.05)), rot=(16, 0, 0))
    p.ears(k, (0.036, -0.322, 0.862), (0.092, -0.318, 0.962), radii=(0.03, 0.023, 0.004))
    p.horn(k, (0, -0.408, 0.868), (0, -0.42, 1.0), 0.24, 0.023, bands=((0.22, 0.013), (0.46, 0.012), (0.68, 0.01)))
    p.legs(k, x=LEG_X, fore=FORE_Y, hind=HIND_Y, top=0.44, r=(0.058, 0.042, 0.034, 0.034),
           shoulder=(0.064, 0.08, 0.11), thigh=(0.068, 0.105, 0.12))
    p.hooves(k, x=LEG_X, fore=FORE_Y, hind=HIND_Y, r=0.036, h=0.05)
    k.paint(lambda P: inside(P[:, 2] - 0.44, 0.03) * inside(np.abs(P[:, 1] - 0.02) - 0.3, 0.02), p.SHADE,
            on=["barrel", "chest", "rump"])
    # --- the mane, the largest mass: broad locks from the crown flowing back down both sides of the neck -----------
    # the turquoise crest arching over the crown and down the nape to the withers
    lock(k, "manecrest", [(0, -0.31, 0.93), (0, -0.22, 0.91), (0, -0.12, 0.81), (0, -0.04, 0.7), (0, 0.03, 0.64)],
         [0.055, 0.068, 0.066, 0.056, 0.04], TURQ, "mane", flat=0.85, mirror=False)
    # the great lilac locks flowing back down the neck's sides to the shoulders, rolled up into curls
    lock(k, "manelock", [(0.03, -0.27, 0.9), (0.08, -0.2, 0.8), (0.11, -0.13, 0.67), (0.13, -0.1, 0.54),
                         (0.14, -0.08, 0.42)], [0.06, 0.08, 0.088, 0.08, 0.06], LILAC, "head", flat=0.5,
         curl=(0.14, -0.04, 0.39), curl_r=0.058)
    # turquoise locks behind them, down over the withers
    lock(k, "maneteal", [(0.03, -0.25, 0.88), (0.075, -0.13, 0.78), (0.1, -0.04, 0.66), (0.105, 0.0, 0.56)],
         [0.055, 0.07, 0.07, 0.055], TURQ, "head", flat=0.5, curl=(0.105, 0.03, 0.53), curl_r=0.05)
    # the forelock: a lilac curl over the forehead behind the horn, toward the model's +X side
    k.add("maneforelock", ell((0.03, -0.37, 0.9), (0.05, 0.05, 0.035), deform=lumpy(3, 2, 0.05)), LILAC, "mane")
    k.add("manemass", ell((0, -0.14, 0.76), (0.08, 0.14, 0.1), deform=lumpy(3, 2, 0.04)), TURQ, "head")
    # the locks' outer faces a shade deeper toward their ends
    k.paint(lambda P: inside(P[:, 2] - 0.6, 0.02), LILAC2, on=["manelock"])

    # --- the tail: broad lilac and turquoise locks falling back and down from the rump to curls ---------------------
    lock(k, "tail", [(0, 0.27, 0.6), (0, 0.37, 0.66), (0, 0.47, 0.62), (0, 0.52, 0.5), (0, 0.52, 0.38),
                     (0, 0.48, 0.3)], [0.05, 0.07, 0.078, 0.075, 0.065, 0.05], LILAC, "tail", flat=0.6,
         curl=(0, 0.445, 0.28), curl_r=0.055, mirror=False)
    lock(k, "tailteal", [(0.025, 0.29, 0.63), (0.06, 0.38, 0.7), (0.08, 0.48, 0.66), (0.095, 0.55, 0.55),
                         (0.1, 0.58, 0.45)], [0.04, 0.056, 0.06, 0.055, 0.045], TURQ, "tail", flat=0.6,
         curl=(0.095, 0.555, 0.42), curl_r=0.045, mirror=False)
    lock(k, "taillow", [(-0.025, 0.29, 0.58), (-0.06, 0.39, 0.6), (-0.08, 0.47, 0.52), (-0.09, 0.5, 0.42)],
         [0.04, 0.055, 0.055, 0.045], LILAC2, "tail", flat=0.6, curl=(-0.085, 0.475, 0.39), curl_r=0.042, mirror=False)

    # --- the fetlocks: a wavy lilac cuff round each hoof, a thick ribbon trailing back along the ground -------------
    for name, y, bone in (("fet", FORE_Y, "leg{s}"), ("fetb", HIND_Y, "hind{s}")):
        for i in range(6):
            a = 2 * math.pi * (i + 0.3) / 6
            k.add(f"{name}{i}", ell((LEG_X + math.cos(a) * 0.036, y + math.sin(a) * 0.036, 0.08 + 0.012 * math.sin(3 * a)),
                                    (0.036, 0.036, 0.028), sub=3), LILAC, bone, mirror=True)
        pts = [(LEG_X + 0.012, y + 0.035, 0.075), (LEG_X + 0.03, y + 0.09, 0.06), (LEG_X + 0.035, y + 0.135, 0.075),
               (LEG_X + 0.038, y + 0.165, 0.062)]
        k.add(f"{name}trail", outward(blade(pts, [0.038, 0.036, 0.03, 0.012], (0, 0, 1), 0.5)), LILAC, bone,
              fuse=False, mirror=True, surface="fur")
    k.paint(lambda P: inside(P[:, 2] - 0.066, 0.005), PERI, on=["fet"])


    # --- the face: nostrils, a mouth, the ears' dark linings --------------------------------------------------------
    p.nostrils(k, (0.02, -0.525, 0.78), unit(0.25, -1, 0.0), (0.03, -0.5, 0.742), unit(0.6, -1, -0.35))
    p.ear_linings(k, (0.061, -0.334, 0.912), unit(0.35, -1, 0.1), (0.012, 0.03), unit(0.5, 0, 0.85))

    k.build(voxel=0.0072, band=0.004)

    # --- the eyes: a stern almond, pale teal under a heavy dark lid slanting down to the front (u runs to the back) -
    CC, LI = srgb(WHITE), srgb("#33193F")
    IR, IRD, PU, WH = srgb("#8CD6DE"), srgb("#3F7C96"), srgb("#1A2A38"), srgb("#F8FAFF")
    def top(u):
        return 0.12 + 0.28 * u - 0.12 * u * u
    def bottom(u):
        return -0.55 + 0.35 * u * u
    def eye(u, v):
        c = np.tile(CC, (len(u), 1))
        inn = (np.abs(u) < 0.92) & (v < top(u)) & (v > bottom(u))
        c[inn] = WH
        ir = ((u + 0.12) / 0.48) ** 2 + ((v + 0.2) / 0.55) ** 2
        c[inn & (ir < 1)] = IRD
        c[inn & (ir < 0.6)] = IR
        c[inn & (((u + 0.12) / 0.18) ** 2 + ((v + 0.22) / 0.26) ** 2 < 1)] = PU
        c[inn & (((u + 0.3) / 0.12) ** 2 + ((v + 0.02) / 0.12) ** 2 < 1)] = (1, 1, 1)
        c[(v > top(u) - 0.02) & (v < top(u) + 0.2) & (np.abs(u) < 0.98)] = LI
        c[(np.abs(v - bottom(u)) < 0.06) & (np.abs(u) < 0.82)] = LI
        return c
    de = unit(0.78, -0.55, 0.18)
    E = H + de / np.linalg.norm(de / HR) + np.array([0, -0.008, 0.006])
    def eyes(state):
        k.decal(E, de, (0.034, 0.03), eye_look(state, eye, CC, LI), eps=0.005, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    rapidash_galar(*args())
