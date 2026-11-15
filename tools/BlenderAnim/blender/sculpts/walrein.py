# Walrein sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/walrein.py -- <the export's Walrein.glb> <out.glb>
#
# Grown from sealeo.py and spheal.py: the pear of a sitting body, the flippers lobed into toes, the two-lobed tail, the
# tusks as solid cones grown long, the white of the whiskers grown into a mane. From the references: a great walrus,
# lighter blue, sitting up with its head raised on a thick neck; two white rings round the neck; a shaggy white mane in
# two great clouds beside and behind the face, its top white over the brow; a small round blue nose at the front of a
# white snout; the mouth wide open below it, pink inside; two long ivory tusks hanging from the snout's sides, down past
# the chin; small yellow eyes with a black pupil in the white of the brow; broad flippers on the ground in front with a
# white line along them; a long tail lying behind with a two-lobed fin, each lobe crossed by a white stripe.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, lumpy, eye_look, outward, args, sibling

sp = sibling("spheal")
BLUE, MANE, SHADE, TUSK, PINK, MOUTH, NOSE = ("#6DB8E2", "#F3F6FA", "#C9D8EA", "#EEDC9A", "#E8858E", "#B24A5A",
                                              "#4FA4D8")
YELLOW, INK = "#F2D648", sp.INK


def eye(u, v):
    """A small yellow eye with a black pupil toward the front (u outward, v up)."""
    c = np.tile(srgb(YELLOW), (len(u), 1))
    c[u * u + v * v > 0.62] = srgb(INK)
    c[((u + 0.18) / 0.36) ** 2 + ((v + 0.02) / 0.5) ** 2 < 1] = srgb(INK)
    return c


def walrein(src, out):
    k = Kit(src, coat="fur")
    LB = np.array([0, 0.06, 0.24])
    LR = np.array([0.225, 0.25, 0.24])
    def pear(V):
        V = V.copy()
        up = np.clip(V[:, 2], 0, 1)
        V[:, :2] *= (1 - 0.38 * up ** 1.3)[:, None]
        V[:, 1] -= 0.3 * up ** 2
        V[:, 2] = np.where(V[:, 2] < -0.75, -0.75 + (V[:, 2] + 0.75) * 0.5, V[:, 2])
        return V
    k.add("body", ell(LB, LR, deform=pear), BLUE, "body")
    # the thick neck rising up and forward to the head
    NECK = [(0, -0.02, 0.3), (0, -0.09, 0.4), (0, -0.14, 0.47)]
    k.add("neck", tube(NECK, [0.155, 0.135, 0.115], seg=32, per=8), BLUE, "body")
    H = np.array([0, -0.17, 0.53])
    HR = np.array([0.108, 0.1, 0.1])
    k.add("head", ell(H, HR), BLUE, "head")
    # the white snout and its round blue nose, the chin below, the open mouth between
    k.add("snout", ell(H + (0, -0.085, 0.0), (0.078, 0.065, 0.055), rot=(-15, 0, 0)), MANE, "head")
    k.add("chin", ell(H + (0, -0.06, -0.1), (0.065, 0.06, 0.045), rot=(20, 0, 0)), BLUE, "head")
    k.add("mouth", ell(H + (0, -0.095, -0.06), (0.056, 0.04, 0.045)), PINK, "head")
    k.add("nose", ell(H + (0, -0.15, 0.03), (0.036, 0.03, 0.03)), NOSE, "head", fuse=False, surface="skin")
    # the tusks: long ivory cones from the snout's sides, down past the chin, a little out
    for sx in (1, -1):
        r0 = H + np.array([0.042 * sx, -0.115, -0.03])
        pts = [r0, r0 + (0.012 * sx, -0.025, -0.07), r0 + (0.022 * sx, -0.035, -0.15), r0 + (0.02 * sx, -0.03, -0.22)]
        k.add(f"tusk{'RL'[sx < 0]}", outward(tube(pts, [0.023, 0.021, 0.014, 0.004], seg=18, per=5)), TUSK, "head",
              fuse=False, surface="shell")
    # the mane: two great clouds of white beside and behind the face, over the brow and down to the neck's top
    rs = np.random.RandomState(365)
    puffs = [((0.12, -0.11, 0.57), 0.05), ((0.155, -0.06, 0.545), 0.065), ((0.165, 0.0, 0.5), 0.07),
             ((0.14, -0.1, 0.47), 0.05), ((0.16, 0.04, 0.43), 0.065), ((0.11, 0.0, 0.59), 0.055),
             ((0.12, 0.08, 0.36), 0.06), ((0.07, 0.06, 0.52), 0.07), ((0.15, -0.12, 0.415), 0.04)]
    for i, (c, r) in enumerate(puffs):
        rr = r * (0.9 + 0.2 * rs.rand(3))
        k.add(f"mane{i}", ell(c, rr, deform=lumpy(5 + i % 3, 4, 0.12)), MANE, "head" if c[2] > 0.47 else "body",
              mirror=True)
    k.add("brow", ell(H + (0, 0.0, 0.06), (0.09, 0.08, 0.05)), MANE, "head")
    k.add("maneback", ell((0, 0.05, 0.53), (0.09, 0.07, 0.08), deform=lumpy(6, 4, 0.12)), MANE, "head")
    # the front flippers on the ground, pointing forward and out
    k.add("arm", tube([(0.16, -0.1, 0.16), (0.2, -0.17, 0.07), (0.21, -0.2, 0.04)], [0.08, 0.065, 0.05]), BLUE,
          "leg{s}", mirror=True)
    k.add("flipper", sp.flipper((0.24, -0.27, 0.024), (0.105, 0.058, 0.022), (0, 0, -62), toes=3, amp=0.1), BLUE,
          "leg{s}", mirror=True)
    # the tail lying behind, tapering, and its fin
    k.add("tail", tube([(0, 0.22, 0.13), (0, 0.36, 0.06), (0, 0.45, 0.045)], [0.13, 0.07, 0.045]), BLUE, "tail")
    TR = np.array([0, 0.445, 0.045])
    sp.fluke(k, TR, 1.0, 0.6, 0.45, (0.09, 0.055, 0.02), BLUE, "tail")
    # the two white rings round the neck, sloping down to the back
    def rings(P):
        z = P[:, 2] - 0.4 * (P[:, 1] + 0.1)        # level along the rings' slope
        a = inside(np.abs(z - 0.355) - 0.011, 0.003)
        b = inside(np.abs(z - 0.425) - 0.011, 0.003)
        return np.maximum(a, b) * (P[:, 2] > 0.28)
    k.paint(rings, MANE, on=["neck", "body"])
    # the shade in the mane's hollows and a white line along each flipper and across each fin lobe
    k.paint(lambda P: 0.2 * inside(np.abs(P[:, 0]) - 0.06, 0.03) * (P[:, 1] > -0.05), SHADE, on=["mane"])
    for sx in (1, -1):
        fc = np.array([0.24 * sx, -0.27, 0.024])
        d = np.array([0.45 * sx, -0.9, 0.0])
        k.paint(lambda P, fc=fc, d=d: inside(np.abs(np.cross(P - fc, d)[:, 2]) - 0.008, 0.003) * (P[:, 2] > 0.025),
                MANE, on=["flipper"])
    k.paint(lambda P: inside(np.abs(np.linalg.norm(P - TR, axis=1) - 0.085) - 0.009, 0.003), MANE, on=["fluke"])
    # the open mouth's throat darker, the white of the snout kept clean
    k.paint(lambda P: inside(np.linalg.norm((P - (H + (0, -0.12, -0.06))) / (0.035, 0.03, 0.03), axis=1) - 1, 0.1),
            MOUTH, on=["mouth"])
    k.build(voxel=0.008, smooth=4, band=0.004)
    E = [(sp.at_surface(H + (0, 0, 0.02), HR, d), d, sx) for sx in (1, -1) for d in [unit(0.55 * sx, -0.72, 0.42)]]
    k.eye_states(lambda st: sp.eyes(k, E, (0.023, 0.02), st, MANE, open_look=eye))
    k.export(out)


if __name__ == "__main__":
    walrein(*args())
