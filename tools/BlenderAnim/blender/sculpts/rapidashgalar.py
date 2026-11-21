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
HR = np.array([0.08, 0.108, 0.08])
LEG_X, FORE_Y, HIND_Y = 0.084, -0.179, 0.226   # each leg's middle, a little outside its joint so the thick legs part


def lock(k, name, pts, radii, col, bone, flat=0.7, curl=None, curl_r=0.05, mirror=True):
    """
    A broad lock of mane: a tube flattened across its own fall (its face turned to the side), ending in a rolled curl
    (a ball where it turns back on itself). Fused, so overlapping locks make one flowing mass.
    """
    P = np.array(pts, float)
    k.add(name, blade(P, list(radii), (1, 0, 0), flat), col, bone, mirror=mirror)
    if curl is not None:
        k.add(name + "curl", ell(curl, (curl_r * 0.8, curl_r, curl_r), deform=lumpy(3, 2, 0.05)), col, bone,
              mirror=mirror)


def rapidash_galar(src, out):
    k = Kit(src, coat="fur")
    # a deep trunk: the belly some 0.3 of the height off the ground, as the pictures have it
    p.trunk(k, Z=0.5, chest=-0.155, rump=0.19, rx=0.13, rz=0.17)
    p.neck(k, [(0, -0.15, 0.57), (0, -0.245, 0.69), (0, -0.305, 0.785)], [0.115, 0.095, 0.08])
    p.head(k, h=H, hr=HR, muzzle=((0, -0.455, 0.774), (0.058, 0.09, 0.055)), rot=(16, 0, 0))
    p.ears(k, (0.04, -0.322, 0.872), (0.1, -0.318, 0.975), radii=(0.034, 0.026, 0.004))
    p.horn(k, (0, -0.41, 0.876), (0, -0.42, 1.0), 0.24, 0.025, bands=((0.22, 0.013), (0.46, 0.012), (0.68, 0.01)))
    p.legs(k, x=LEG_X, fore=FORE_Y, hind=HIND_Y, top=0.44, r=(0.075, 0.056, 0.047, 0.046),
           shoulder=(0.08, 0.095, 0.15), thigh=(0.085, 0.115, 0.16))
    p.hooves(k, x=LEG_X, fore=FORE_Y, hind=HIND_Y, r=0.048, h=0.06)
    k.paint(lambda P: inside(P[:, 2] - 0.38, 0.03) * inside(np.abs(P[:, 1] - 0.02) - 0.3, 0.02), p.SHADE,
            on=["barrel", "chest", "rump"])
    # --- the mane, the largest mass: broad overlapping locks from the crown down both sides of the neck to the knees
    # the turquoise crest arching over the crown and down the nape, and a mass of it behind the neck
    lock(k, "manecrest", [(0, -0.31, 0.94), (0, -0.22, 0.92), (0, -0.12, 0.82), (0, -0.04, 0.71), (0, 0.03, 0.65)],
         [0.06, 0.075, 0.075, 0.065, 0.045], TURQ, "mane", flat=0.85, mirror=False)
    k.add("manemass", ell((0, -0.13, 0.75), (0.1, 0.16, 0.12), deform=lumpy(3, 2, 0.04)), TURQ, "head")
    # the great lilac lock in front, flowing down past the shoulder to the knee and rolling up
    lock(k, "manelock", [(0.04, -0.29, 0.92), (0.1, -0.22, 0.82), (0.15, -0.15, 0.68), (0.17, -0.12, 0.52),
                         (0.18, -0.12, 0.38), (0.175, -0.14, 0.28)], [0.06, 0.08, 0.09, 0.09, 0.08, 0.06], LILAC,
         "head", flat=0.45, curl=(0.17, -0.1, 0.24), curl_r=0.06)
    # a turquoise lock behind it, to the shoulder's foot
    lock(k, "maneteal", [(0.03, -0.24, 0.9), (0.09, -0.13, 0.78), (0.14, -0.04, 0.62), (0.155, 0.0, 0.48),
                         (0.16, 0.01, 0.36)], [0.055, 0.075, 0.085, 0.08, 0.06], TURQ, "head", flat=0.45,
         curl=(0.155, 0.04, 0.32), curl_r=0.055)
    # a second lilac lock further back over the withers
    lock(k, "maneback", [(0.02, -0.17, 0.88), (0.07, -0.05, 0.76), (0.11, 0.04, 0.64), (0.125, 0.08, 0.54)],
         [0.05, 0.07, 0.075, 0.06], LILAC, "head", flat=0.45, curl=(0.12, 0.11, 0.5), curl_r=0.05)
    # the forelock: a lilac curl over the forehead behind the horn, toward the model's +X side
    k.add("maneforelock", ell((0.03, -0.375, 0.91), (0.05, 0.05, 0.035), deform=lumpy(3, 2, 0.05)), LILAC, "mane")
    # the locks' outer faces a shade deeper toward their ends
    k.paint(lambda P: inside(P[:, 2] - 0.55, 0.02), LILAC2, on=["manelock", "maneback"])

    # --- the tail: broad locks falling back and down from the rump to the ground, rolled into curls --------------------
    lock(k, "tail", [(0, 0.28, 0.62), (0, 0.4, 0.67), (0, 0.5, 0.6), (0, 0.55, 0.45), (0, 0.55, 0.28),
                     (0, 0.52, 0.14)], [0.06, 0.085, 0.095, 0.095, 0.085, 0.065], LILAC, "tail", flat=0.55,
         curl=(0, 0.47, 0.1), curl_r=0.065, mirror=False)
    lock(k, "tailteal", [(0.03, 0.3, 0.63), (0.07, 0.42, 0.68), (0.1, 0.53, 0.63), (0.12, 0.6, 0.5),
                         (0.13, 0.62, 0.32)], [0.05, 0.07, 0.08, 0.075, 0.06], TURQ, "tail", flat=0.55,
         curl=(0.125, 0.59, 0.27), curl_r=0.055, mirror=False)
    lock(k, "taillow", [(-0.03, 0.3, 0.6), (-0.07, 0.41, 0.6), (-0.09, 0.48, 0.45), (-0.1, 0.5, 0.28)],
         [0.05, 0.07, 0.075, 0.06], LILAC2, "tail", flat=0.55, curl=(-0.095, 0.47, 0.23), curl_r=0.05, mirror=False)

    # --- the fetlocks: a round lilac cloud round each hoof, twice the leg's width, a broad ribbon trailing back ------
    for name, y, bone in (("fet", FORE_Y, "leg{s}"), ("fetb", HIND_Y, "hind{s}")):
        k.add(f"{name}core", ell((LEG_X, y, 0.1), (0.075, 0.075, 0.055), deform=lumpy(4, 3, 0.06)), LILAC, bone,
              mirror=True)
        for i in range(7):
            a = 2 * math.pi * (i + 0.3) / 7
            k.add(f"{name}{i}", ell((LEG_X + math.cos(a) * 0.05, y + math.sin(a) * 0.05, 0.1 + 0.014 * math.sin(3 * a)),
                                    (0.048, 0.048, 0.04), sub=3), LILAC, bone, mirror=True)
        pts = [(LEG_X + 0.015, y + 0.05, 0.065), (LEG_X + 0.04, y + 0.12, 0.05), (LEG_X + 0.05, y + 0.18, 0.068),
               (LEG_X + 0.055, y + 0.22, 0.052)]
        k.add(f"{name}trail", outward(blade(pts, [0.056, 0.052, 0.042, 0.016], (0, 0, 1), 0.5)), LILAC, bone,
              fuse=False, mirror=True, surface="fur")
    k.paint(lambda P: inside(P[:, 2] - 0.075, 0.005), PERI, on=["fet"])



    # --- the face: nostrils, a mouth, the ears' dark linings --------------------------------------------------------
    p.nostrils(k, (0.022, -0.54, 0.778), unit(0.25, -1, 0.0), (0.034, -0.512, 0.736), unit(0.6, -1, -0.35))
    p.ear_linings(k, (0.068, -0.334, 0.925), unit(0.35, -1, 0.1), (0.012, 0.03), unit(0.5, 0, 0.85))

    k.build(voxel=0.0085, band=0.004)

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
        k.decal(E, de, (0.04, 0.035), eye_look(state, eye, CC, LI), eps=0.005, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    rapidash_galar(*args())
