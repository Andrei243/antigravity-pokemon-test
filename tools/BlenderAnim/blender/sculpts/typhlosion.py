# Typhlosion sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/typhlosion.py -- <the export's Typhlosion.glb> <out.glb>
# Quilava grown into a great upright beast: a huge cream pear of a body, heaviest at the hips where the thighs are
# fused in, the belly low over short legs and broad clawed feet; short thick arms carried forward and a little down,
# three small claws a hand; a thick neck carrying the head forward. The head has a broad blunt snout and an open grin
# (dark red, a pink tongue), narrow red eyes under a heavy brow. The dark blue-green is only a stripe now, from the back
# of the head down the spine to a short pointed tail. Round the neck a great ruff of fire, thick tongues bursting up
# and out at the sides and behind, orange-red with a yellow heart. Built by Cyndaquil's flames and claws and Quilava's
# eyes under a brow.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, smoothstep, unit, polygon, args, sibling

cq = sibling("cyndaquil")
ql = sibling("quilava")
NAVY, CREAM = "#24505E", "#F5E09A"
MOUTH, TONGUE = srgb("#7E2228"), srgb("#E58C98")

H, HR = np.array([0.0, -0.08, 0.75]), np.array([0.112, 0.114, 0.1])
SNOUT = [(0, -0.13, 0.73), (0, -0.2, 0.712), (0, -0.25, 0.7)]


def typhlosion(src, out):
    k = Kit(src, coat="fur")

    # The body: a huge pear leaning a little forward, the hips widest and low, the belly near the ground
    k.add("hips", ell((0, 0.02, 0.27), (0.215, 0.19, 0.19)), CREAM, "body")
    k.add("belly", ell((0, -0.015, 0.42), (0.19, 0.175, 0.23), rot=(-8, 0, 0)), CREAM, "body")
    k.add("chest", ell((0, -0.035, 0.54), (0.145, 0.13, 0.12), rot=(-10, 0, 0)), CREAM, "body")
    k.add("neck", tube([(0, -0.03, 0.56), (0, -0.05, 0.64), (0, -0.07, 0.7)], [0.11, 0.095, 0.085]), CREAM, "head")

    # The head carried forward: a crown, a broad blunt snout
    k.add("head", ell(H, HR), CREAM, "head")
    k.add("snout", tube(SNOUT, [0.078, 0.065, 0.052]), CREAM, "head")
    k.add("jaw", ell((0, -0.13, 0.695), (0.075, 0.09, 0.05)), CREAM, "head")

    # The arms: short and thick, carried forward and a little down, three small claws a hand
    k.add("arm", tube([(0.13, -0.04, 0.53), (0.19, -0.09, 0.49), (0.205, -0.16, 0.45)], [0.058, 0.05, 0.045]),
          CREAM, "arm{s}", mirror=True)
    HC = np.array([0.208, -0.19, 0.44])
    k.add("hand", ell(HC, (0.045, 0.04, 0.04)), CREAM, "arm{s}", mirror=True)
    for j, (dx, dz) in enumerate([(0.022, 0.006), (0.0, -0.004), (-0.022, 0.006)]):
        b = HC + np.array((dx, -0.032, -0.006 + dz))
        k.add(f"claw{j}", cq.claw(b, (dx * 1.2, -1, -0.6), 0.032, 0.02, 0.016, up=(0, 1, 0)), cq.CLAW, "arm{s}",
              fuse=False, mirror=True, surface="shell")

    # The legs: heavy thighs fused into the hips, a stub of shin, broad feet with three claws
    k.add("thigh", ell((0.13, -0.02, 0.17), (0.11, 0.13, 0.13)), CREAM, "leg{s}", mirror=True)
    k.add("shin", tube([(0.135, -0.04, 0.1), (0.14, -0.05, 0.068)], [0.072, 0.065]), CREAM, "leg{s}", mirror=True)
    k.add("foot", ell((0.142, -0.085, 0.028), (0.07, 0.1, 0.03)), CREAM, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.03, 0.0, 0.03)):
        k.add(f"toe{j}", cq.claw((0.142 + dx, -0.175, 0.026), (dx * 0.6, -1, -0.15), 0.034, 0.024, 0.018), cq.CLAW,
              "leg{s}", fuse=False, mirror=True, surface="shell")

    # The tail: short, pointed, dark, running back and down and a little out to the +X side
    k.add("tail", blade([(0, 0.17, 0.25), (0.04, 0.27, 0.17), (0.08, 0.35, 0.1)], [0.06, 0.035, 0.008],
                        (0.6, 0, 0.8), 0.6), NAVY, "tail")

    # The dark: a stripe from the tail up the spine, and over the head a hood from the nape over the crown down to a
    # point between the eyes, as Quilava's; cream everywhere else
    stripe_ys = ([0.1, 0.2, 0.3, 0.4, 0.5, 0.58, 0.66, 0.76, 0.86], [0.14, 0.16, 0.165, 0.15, 0.11, 0.06, 0.02, -0.05, -0.08])
    back = ql.back_mask(*stripe_ys, 0.86)
    narrow = lambda P: inside(np.abs(P[:, 0]) - (0.075 + 0.04 * smoothstep(0.7, 0.8, P[:, 2])), 0.0035)
    crown = cq.hood([-0.4, -0.2, -0.17, -0.15, -0.08, 0.0, 0.05, 0.1, 0.4],
                    [0.758, 0.758, 0.765, 0.77, 0.79, 0.75, 0.69, 0.65, 0.65], below=0.64)
    point = lambda P: inside(np.abs(P[:, 0]) - np.interp(P[:, 1], [-0.235, -0.2, -0.17, -0.12, 0.2],
                                                        [0.0, 0.045, 0.12, 0.2, 0.3]), 0.0035)
    dark = lambda P: np.maximum(back(P) * narrow(P), crown(P) * point(P))
    k.paint(dark, NAVY)
    k.paint(lambda P: 1 - dark(P), CREAM, on=["hips", "belly", "chest", "neck", "head", "snout", "jaw", "arm",
                                              "hand", "thigh", "shin", "foot"])

    # Small dark ears, nubs laid back at the crown's sides
    for s in (1, -1):
        b = np.array([0.075 * s, -0.035, 0.825])
        tip = b + np.array([0.035 * s, 0.05, 0.06])
        k.add(f"ear{s}", hull([b + (0.025, 0, 0), b - (0.025, 0, 0), b + (0, 0.025, 0), b - (0, 0.025, 0),
                               b - (0, 0, 0.02), tip]), NAVY, "head", fuse=False, surface="fur")

    # The ruff of fire round the neck: thick tongues bursting up and out at the sides and behind
    f = cq.Flames()
    for ang, L, w, up in [(90, 0.22, 0.075, 0.8), (115, 0.26, 0.08, 1.0), (140, 0.29, 0.085, 1.2),
                          (165, 0.3, 0.09, 1.4), (180, 0.3, 0.09, 1.6)]:
        for s in ((1, -1) if ang != 180 else (1,)):
            a = math.radians(ang)
            o = np.array([s * math.sin(a), -math.cos(a), 0.0])
            root = np.array([0, -0.03, 0.62]) + o * np.array([0.1, 0.09, 0]) + (0, 0, 0)
            d = o + np.array([0, 0, up])
            side = np.cross(unit(*d), (0, 0, 1.0))
            if np.linalg.norm(side) < 1e-3:
                side = np.array([1.0, 0, 0])
            f.flame(root, d, L * 1.25, w * 1.1, "flame", side, flick=0.3)
    f.add(k)

    # The mouth: a grin open under the snout's tip, dark red with a pink tongue low in it
    def mouth(u, v):
        c = np.tile(MOUTH, (len(u), 1))
        c[(u / 0.6) ** 2 + ((v + 0.65) / 0.5) ** 2 < 1] = TONGUE
        return c
    k.decal((0, -0.248, 0.668), unit(0, -0.8, -0.55), (0.048, 0.024), mouth, eps=0.004,
            outline=polygon([(-1, 0.6), (-0.5, 0.25), (0.5, 0.25), (1, 0.6), (0.7, -0.4), (0, -0.85), (-0.7, -0.4)]),
            rings=12, segs=56)
    k.decal((0, -0.29, 0.71), (0, -1, 0.2), (0.013, 0.01), cq.NOSE, eps=0.003, rings=4, segs=20)

    # The eyes: narrow and red under a heavy brow sloping down to the snout
    cut = ql.brow_cut(0.22, 0.42)
    de = unit(0.6, -0.74, 0.18)
    E = cq.on_ell(H + (0, -0.01, 0.0), HR, de)
    eyes = ql.eyes_under_brow(k, E, de, (0.045, 0.045), ql.fierce_eye(ql.RED, cut, iris_c=(-0.15, -0.18),
                                                                      iris_r=0.56, pupil_r=0.28, rim=0.8), cut, srgb(CREAM), line=0.22)

    k.build(voxel=0.0075, smooth=4, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    typhlosion(*args())
