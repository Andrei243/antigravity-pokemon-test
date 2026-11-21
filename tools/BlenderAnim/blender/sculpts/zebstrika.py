# Zebstrika sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/zebstrika.py -- <the export's Zebstrika.glb> <out.glb>
# Grown from blitzle.py (sibling("blitzle")): the same grey, white and black, its legs and hooves, its spikes and
# painted jagged marks. From the references: a tall dark grey zebra with a long horse's face pointing down to a black
# nose, a white blaze down the forehead; small sharp yellow eyes in a white star of rays, a heavy dark line over each;
# pointed ears, dark rimmed and blue within; two tall white points standing from the forelock like horns; a mane of
# big white spikes swept back down the neck; white zigzag rings round the neck, a jagged band across the chest,
# zigzag stripes down the flanks and the haunch; legs ringed white, grey hooves; a thin tail ending in a white star.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, slab, srgb, inside, smoothstep, unit, polygon, eye_look, args, sibling)

bz = sibling("blitzle")
GREY, WHITE, MANE, NOSE = bz.GREY, bz.WHITE, bz.MANE, bz.NOSE
EAR_RIM, EAR_IN = bz.EAR_RIM, bz.EAR_IN
HOOF = "#8C8C94"
YELLOW, INK, GLINT = srgb("#F2D43A"), bz.INK, bz.GLINT


def zig_band(y0, z_top, z_bot, w, amp, n, lean=0.0):
    """A white stripe down a flank in (y, z): a band 2w wide whose two edges zigzag together by amp, n corners."""
    zs = np.linspace(z_top, z_bot, n)
    off = [amp * (1 if i % 2 else -1) + lean * (z - z_top) for i, z in enumerate(zs)]
    left = [(y0 + o - w, z) for o, z in zip(off, zs)]
    right = [(y0 + o + w * (0.7 if 0 < i < n - 1 else 1.0), z) for i, (o, z) in enumerate(zip(off, zs))]
    return left + right[::-1]


def sharp_eye(u, v):
    """The open eye: a yellow almond, a round dark pupil toward the nose, a heavy dark line along the top."""
    c = np.tile(YELLOW, (len(u), 1))
    c[((u + 0.12) / 0.34) ** 2 + ((v + 0.02) / 0.5) ** 2 < 1] = INK
    c[((u + 0.0) / 0.13) ** 2 + ((v - 0.2) / 0.15) ** 2 < 1] = GLINT
    c[v > 0.46 + 0.28 * u] = INK
    r = np.sqrt(u * u + v * v)
    c[r > 0.86] = INK
    return c


def almond(th):
    c, s = math.cos(th), math.sin(th)
    return 1.0 / max((abs(c) ** 1.7 + abs(s) ** 1.7) ** (1 / 1.7), 1e-6) * (0.88 if s > 0 else 1.0)


def zebstrika(src, out):
    k = Kit(src, coat="fur")

    # --- the trunk: deep, a round chest and rump --------------------------------------------------------------
    k.add("barrel", ell((0, 0.01, 0.38), (0.086, 0.19, 0.086)), GREY, "body")
    k.add("chest", ell((0, -0.12, 0.392), (0.084, 0.09, 0.095)), GREY, "body")
    k.add("rump", ell((0, 0.145, 0.392), (0.09, 0.095, 0.09)), GREY, "body")

    # --- legs: broad shoulders and thighs fused over the upper half, shins tapering, grey hooves ---------------
    k.add("shoulder", ell((0.05, -0.12, 0.305), (0.045, 0.062, 0.1)), GREY, "leg{s}", mirror=True)
    k.add("thigh", ell((0.054, 0.145, 0.315), (0.056, 0.084, 0.115)), GREY, "hind{s}", mirror=True)
    bz.leg(k, "fore", 0.054, -0.12, 0.29, -0.005, (0.038, 0.027, 0.025), "leg{s}", hoof=HOOF)
    bz.leg(k, "hind", 0.056, 0.152, 0.29, 0.01, (0.041, 0.027, 0.025), "hind{s}", hoof=HOOF)
    LEGS = [(0.055, -0.12), (0.057, 0.152)]
    for z0, z1 in ((0.03, 0.06), (0.095, 0.125), (0.165, 0.2)):
        k.paint(bz.leg_mask(LEGS, z0, z1, 0.045), WHITE)

    # --- a thick neck up to a long face pointing down and forward, a black nose -------------------------------
    NA, NB = (0, -0.13, 0.43), (0, -0.205, 0.61)
    k.add("neck", tube([NA, (0, -0.17, 0.53), NB], [0.08, 0.068, 0.06]), GREY, "head")
    C, CR = np.array([0, -0.228, 0.64]), np.array([0.064, 0.074, 0.066])
    k.add("cranium", ell(C, CR), GREY, "head")
    FC = np.array([0, -0.29, 0.578])
    k.add("face", ell(FC, (0.05, 0.052, 0.098), rot=(-38, 0, 0)), GREY, "head")
    k.add("nose", ell((0, -0.345, 0.506), (0.039, 0.034, 0.034), rot=(-38, 0, 0)), NOSE, "head", fuse=False,
          surface="skin")
    # the white blaze down the forehead
    fn = unit(0, -0.79, 0.62)
    k.decal(FC + fn * 0.052 + np.array([0, 0.012, 0.016]), fn, (0.012, 0.05), WHITE, up=unit(0, 0.62, 0.79),
            eps=0.003, outline=polygon([(-1, 1), (1, 1), (0.45, -1), (-0.45, -1)]), rings=10, segs=40)

    # --- pointed ears up and out, dark rimmed and blue within ------------------------------------------------
    k.add("ear", tube([(0.036, -0.212, 0.66), (0.055, -0.216, 0.705), (0.078, -0.22, 0.755)], [0.026, 0.019, 0.004]),
          GREY, "ear{s}", mirror=True)
    en = unit(0.25, -1, 0.05)
    tear = polygon([(0, 1), (0.75, -0.2), (0.55, -1), (-0.55, -1), (-0.75, -0.2)])
    k.decal((0.058, -0.234, 0.705), en, (0.015, 0.03), EAR_RIM, up=unit(0.45, 0, 1), eps=0.003, outline=tear,
            rings=8, segs=40, mirror=True)
    k.decal((0.058, -0.234, 0.703), en, (0.01, 0.022), EAR_IN, up=unit(0.45, 0, 1), eps=0.0045, outline=tear,
            rings=8, segs=40, mirror=True)

    # --- two tall white points standing from the forelock, leaning back -----------------------------------
    for sx, h in ((1, 0.2), (-1, 0.17)):
        a = np.array((0.014 * sx, -0.245, 0.69))
        k.add("horn" + ("R" if sx > 0 else "L"), bz.spike(a, a + np.array((0.02 * sx, 0.05, h)), 0.022, 0.028,
                                                         up=(0, 1, 0)), WHITE, "head", fuse=False, surface="hair")
    # the mane: big white spikes swept back down the neck, alternating a little to each side
    for i, t in enumerate(np.linspace(0, 1, 7)):
        p = np.array((0, -0.19, 0.68)) * (1 - t) + np.array((0, -0.06, 0.47)) * t
        sx = 1 if i % 2 else -1
        L = 0.13 - 0.05 * t
        d = unit(0.35 * sx, 1, 0.75 - 0.35 * t)
        k.add(f"mane{i}", bz.spike(p, p + d * L, 0.02, 0.03, up=(1, 0, 0)), MANE, "head" if t < 0.6 else "body",
              fuse=False, surface="hair")

    # --- the tail: thin, falling back and down to a white star -----------------------------------------------
    k.add("tail", tube([(0, 0.22, 0.43), (0, 0.29, 0.42), (0.015, 0.34, 0.36), (0.025, 0.36, 0.3)],
                       [0.02, 0.016, 0.014, 0.013]), GREY, "tail")
    TS = np.array((0.026, 0.365, 0.285))
    for i in range(7):
        a = 2 * math.pi * i / 7 + 0.3
        d = unit(math.cos(a) * 0.9, 0.35, math.sin(a))
        k.add(f"star{i}", bz.spike(TS - d * 0.01, TS + d * (0.055 if i % 2 else 0.045), 0.016, 0.012, up=(0, 1, 0)),
              WHITE, "tail", fuse=False, surface="hair")
    k.add("starcore", ell(TS, (0.02, 0.018, 0.02), sub=3), WHITE, "tail", fuse=False, surface="hair")

    # --- the white marks: rings round the neck, a band across the chest, stripes down the flanks and haunch ---
    for s0, s1 in ((0.03, 0.06), (0.1, 0.13), (0.165, 0.19)):
        k.paint(bz.collar(NA, NB, s0, s1, 0.01, 5, 0.1), WHITE)
    k.paint(bz.collar((0, -0.07, 0.36), (0, -0.2, 0.42), 0.06, 0.09, 0.012, 7, 0.12), WHITE)
    for y0, lean in ((-0.03, 0.1), (0.075, 0.0), (0.175, -0.08)):
        pts = zig_band(y0, 0.48, 0.3, 0.012, 0.022, 4, lean)
        k.paint(lambda P, pts=pts: bz.el.poly_mask(P[:, 1], P[:, 2], pts, 0.0025) * inside(0.3 - P[:, 2], 0.003),
                WHITE)

    # --- the eyes: small and sharp in a white star of rays, high on the sides of the head ---------------------
    de = unit(0.72, -0.62, 0.15)
    E = bz.on_ell(C, CR, de)
    star = polygon([(math.cos(a) * (1 if i % 2 == 0 else 0.62), math.sin(a) * (1 if i % 2 == 0 else 0.62))
                    for i, a in enumerate(np.linspace(0, 2 * math.pi, 12, endpoint=False) + 0.2)])
    k.decal(E, de, (0.044, 0.041), WHITE, up=unit(0, 0.2, 1), eps=0.003, outline=star, rings=10, segs=60,
            mirror=True)
    W = srgb(WHITE)
    def eyes(state):
        k.decal(E, de, (0.029, 0.021), eye_look(state, sharp_eye, W, INK, line=0.17), up=unit(0, 0.25, 1),
                eps=0.0055, outline=almond, rings=20, segs=56, mirror=True)

    k.build(voxel=0.006, smooth=4)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    zebstrika(*args())
