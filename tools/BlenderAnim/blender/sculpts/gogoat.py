# Gogoat sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Skiddo's script (skiddo.py, imported as sibling("skiddo")): the same leafy spikes on a lumpy fused core for the mane,
# the same sprig of leaves, the same horns banded darker, the same shaggy fringe and legs with socks and hooves.
#   blender --background --factory-startup --python sculpts/gogoat.py -- <the export's Gogoat.glb> <out.glb>
#
# The design, from the references: a big goat standing tall; a tan body with a jagged shaggy fringe under the belly
# and at the elbows, a brown saddle over the back whose lower edge hangs in points; a great green mane of leaves from
# behind the head down over the chest and shoulders (the largest mass at the front), running as a ridge along the back
# to a tall leafy tuft of a tail; a brown head held high, white over the snout and down into a white beard, a dark grey
# nose; a serious red eye in a white patch outlined in grey under a brown brow; small pointed brown ears; great dark
# grey horns from the crown sweeping up, back and down, banded darker; brown lower legs with a grey band, white socks
# and red-orange hooves.
# Reference ratios (3D picture): head 0.25 of the height, horns over the head 0.23, leg showing under the fringe 0.2,
# mane 0.45 of the height at the front.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, outward, srgb, inside, unit, tri, polygon, eye_look, args, sibling

sk = sibling("skiddo")
t, dl = sk.t, sk.dl
lv = sibling("larvitar")              # the eye cut by a brow

TAN, BROWN, DARK, GREY = "#D3B98A", "#8E5A34", "#7A4A2C", "#A9A8B4"
SOCK, HOOF, GREEN, GREEN2 = "#F4F2EE", "#E4502E", "#1F9566", "#2DAA78"
WHITE, NOSE, HORN, BAND, RIM = "#F6F4F0", "#5E6068", "#5A585E", "#36343A", "#7E7C86"
IRIS = srgb("#D8342A")


def gogoat(src, out_path):
    k = Kit(src, coat="fur")
    H = np.array([0, -0.23, 0.53])              # the head's middle (its bone is at 0, -0.162, 0.364)
    HR = np.array([0.071, 0.079, 0.075])
    B = np.array([0, 0.03, 0.3])                # the trunk's middle

    # --- the trunk: deep, the belly low, shoulders and thighs fused over the legs ------------------------------
    k.add("barrel", ell(B, (0.108, 0.21, 0.11)), TAN, "body")
    k.add("belly", ell((0, 0.03, 0.235), (0.095, 0.18, 0.075)), TAN, "body")
    k.add("rump", ell((0, 0.17, 0.31), (0.108, 0.1, 0.11)), TAN, "body")
    k.add("chest", ell((0, -0.13, 0.31), (0.1, 0.1, 0.115)), TAN, "body")
    k.add("shoulder", ell((0.075, -0.16, 0.225), (0.05, 0.06, 0.085)), TAN, "leg{s}", mirror=True)
    k.add("thigh", ell((0.077, 0.17, 0.235), (0.056, 0.072, 0.09)), TAN, "hind{s}", mirror=True)
    dl.leg(k, "fore", 0.079, -0.162, 0.19, (0.042, 0.034, 0.03), "leg{s}", color=BROWN, hoof=HOOF)
    dl.leg(k, "hind", 0.08, 0.175, 0.19, (0.044, 0.034, 0.03), "hind{s}", color=BROWN, hoof=HOOF, knee=0.008)
    LEGS = lambda P: inside(np.abs(P[:, 0]) - 0.16, 0.004)
    k.paint(lambda P: inside(P[:, 2] - 0.15, 0.003) * LEGS(P), BROWN)
    k.paint(lambda P: inside(P[:, 2] - 0.095, 0.0025) * LEGS(P), GREY)
    k.paint(lambda P: inside(P[:, 2] - 0.075, 0.0025) * LEGS(P), SOCK)

    # the saddle: brown over the back, its lower edge hanging in points
    def saddle(P):
        edge = 0.36 - 0.035 * (1 - np.abs(tri((P[:, 1] + 0.3) / 0.085)))
        return inside(edge - P[:, 2], 0.003) * inside(P[:, 1] - 0.3, 0.004) * inside(-0.18 - P[:, 1], 0.004)
    k.paint(saddle, BROWN, on=["barrel", "rump", "chest", "belly", "thigh", "shoulder"])

    # --- the neck inside the mane, the head held high; a muzzle forward and down, a big grey nose ---------------
    k.add("neck", tube([(0, -0.15, 0.36), (0, -0.19, 0.46), (0, -0.205, 0.5)], [0.07, 0.062, 0.058]), BROWN, "head")
    HEAD = ell(H, HR)
    k.add("head", HEAD, BROWN, "head")
    MUZ = ell(H + (0, -0.068, -0.046), (0.05, 0.062, 0.046), rot=(28, 0, 0))
    k.add("muzzle", MUZ, BROWN, "head")
    k.add("nose", outward(ell(H + (0, -0.12, -0.052), (0.032, 0.021, 0.028), rot=(-25, 0, 0))), NOSE, "head",
          fuse=False, surface="skin")
    k.add("ear", ell((0.072, -0.2, 0.575), (0.04, 0.014, 0.02), rot=(0, -20, 25)), BROWN, "ear{s}", mirror=True)

    # the face white over the snout and the lower face, the crown and the brow brown
    FRONT = lambda P: (P[:, 1] < H[1] + 0.02) * (P[:, 2] > 0.38)
    z0 = H[2]
    FACE = t.mirrored([(0.0, z0 - 0.005), (0.03, z0 - 0.01), (0.06, z0 - 0.022), (0.085, z0 - 0.03),
                       (0.09, z0 - 0.06), (0.075, z0 - 0.11), (0.0, z0 - 0.13)])
    k.paint(lambda P: t.poly_mask(*t.round_about(P, H, HR[0]), FACE, edge=0.002) * FRONT(P), WHITE,
            on=["head", "muzzle"])

    # the white beard under the jaw
    for i, (x, L, d) in enumerate(((0.0, 0.075, (0, -0.2, -1)), (0.022, 0.06, (0.35, -0.15, -1)),
                                   (-0.022, 0.06, (-0.35, -0.15, -1)), (0.042, 0.045, (0.7, -0.1, -0.8)),
                                   (-0.042, 0.045, (-0.7, -0.1, -0.8)))):
        sk.spike(k, f"beard{i}", (x, -0.262 + 0.02 * abs(x) / 0.042, 0.465), d, L, 0.02, 0.009, "head", WHITE,
                 side=(1, 0, 0))

    # --- great horns from the crown, up, back and down, banded darker -------------------------------------------
    for s in (1, -1):
        sk.horn(k, [(0.026, -0.215, 0.585), (0.034, -0.2, 0.645), (0.05, -0.158, 0.69), (0.068, -0.1, 0.705),
                    (0.088, -0.04, 0.69), (0.104, 0.008, 0.652), (0.112, 0.03, 0.612), (0.116, 0.034, 0.585)],
                [0.022, 0.021, 0.019, 0.017, 0.015, 0.013, 0.011, 0.0085], [2, 3, 4, 5], s, colour=HORN, band=BAND)

    # --- the mane of leaves: round the neck, over the chest and shoulders, a ridge along the back --------------
    sk.mane(k, [((0, -0.17, 0.39), (0.118, 0.11, 0.14)), ((0, -0.13, 0.515), (0.088, 0.07, 0.07)),
                ((0, -0.02, 0.43), (0.075, 0.11, 0.045)), ((0, 0.1, 0.425), (0.05, 0.1, 0.03))],
            [((0, -0.17, 0.28), (0.1, 0.1), np.linspace(-120, 120, 11), 0.085, 0.036, 0.011, 1.0),
             ((0, -0.17, 0.37), (0.118, 0.106), np.linspace(-115, 115, 10), 0.075, 0.034, 0.011, 0.55),
             ((0, -0.15, 0.47), (0.1, 0.085), np.linspace(-120, 120, 9), 0.065, 0.032, 0.011, 0.3),
             ((0, -0.03, 0.43), (0.07, 0.09), [-120, -95, -70, 70, 95, 120], 0.055, 0.03, 0.01, 0.7),
             ((0, 0.1, 0.42), (0.05, 0.08), [-110, -80, 80, 110], 0.045, 0.026, 0.01, 0.8)],
            [GREEN, GREEN2])

    # --- the tall leafy tuft of the tail ------------------------------------------------------------------------
    sk.sprig(k, (0, 0.235, 0.41), [((0, 0.2, 1), 0.2, 0.04), ((0.45, 0.25, 0.9), 0.16, 0.036),
                                    ((-0.45, 0.25, 0.9), 0.16, 0.036), ((0.3, 0.8, 0.6), 0.13, 0.032),
                                    ((-0.3, 0.8, 0.6), 0.13, 0.032), ((0, 0.6, 0.85), 0.15, 0.034)],
             [GREEN, GREEN2])

    # --- the shaggy fringe under the belly, at the elbows and round the rump ------------------------------------
    roots = [((0.09, y, 0.215), (0.55, 0, -1)) for y in np.linspace(-0.08, 0.2, 6)]
    roots += [((0.04, y, 0.165), (0.3, 0.1, -1)) for y in (-0.02, 0.07, 0.15)]
    roots += [((0.105, -0.16, 0.2), (0.8, -0.2, -0.8)), ((0.11, 0.17, 0.2), (0.8, 0.2, -0.8)),
              ((0.06, 0.26, 0.26), (0.4, 0.9, -0.6))]
    sk.fringe(k, roots, TAN, 0.065, 0.026, 0.011)

    # --- the eye: a white patch outlined in grey, the red eye in it cut by the brown brow -----------------------
    de = unit(0.62, -0.74, 0.18)
    E = t.hit(HEAD, H + (0.04, -1, 0.012), (0, 1, 0))
    RIMC, WC = srgb(RIM), srgb(WHITE)
    def patch(u, v):
        c = np.tile(WC, (len(u), 1))
        c[np.hypot(u, v / 0.8) > 0.8] = RIMC
        return c
    k.decal(E + (0, 0, -0.002), de, (0.034, 0.024), patch, eps=0.004, rings=12, segs=56, mirror=True)

    cut = lv.brow_cut(0.22, 0.25)
    look = lv.angry_eye(cut, iris=IRIS, rim=0.8, iris_c=(-0.15, -0.12), iris_r=0.55, pupil_r=0.22)
    m = np.array([-1, 1, 1.0])
    INK = lv.INK

    def eyes(state):
        lk = eye_look(state, look, WC, INK, line=0.16)
        k.decal(E, de, (0.022, 0.017), lk, eps=0.0065, rings=20, segs=56, outline=cut)
        k.decal(E * m, de * m, (0.022, 0.017), lambda u, v: lk(-u, v), eps=0.0065, rings=20, segs=56,
                outline=lambda th: cut(math.pi - th))

    k.build(voxel=0.006, band=0.006, wband=0.05)
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    gogoat(*args())
