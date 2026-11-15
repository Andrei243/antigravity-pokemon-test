# Tyrogue sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tyrogue.py -- <the export's Tyrogue.glb> <out.glb>
# From the references (the baby of Hitmonlee's and Hitmonchan's line, its eyes made as theirs are): a big lilac head,
# round, with three blunt horns across its crown (the middle one tallest) and a raised round disc on each side; fierce
# almond eyes, amber with a black slit and a heavy dark upper line, slanting down to the inner corner; a small frown;
# a short lilac trunk with a white band round the waist; bulging brown shorts; lilac legs on round brown shoes; lilac
# arms with white wraps at the wrists and big splayed hands of three fingers and a thumb. The art crouches; the model
# stands as the skeleton does, its arms hanging forward and a little out.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, lathe, outward, ring_band, polygon, srgb, unit, eye_look, args

SKIN, SHORTS, WRAP = "#D4B2DA", "#8E5E3E", "#F2EEE4"
INK, AMBER, MOUTH = "#1C1418", "#E4A23A", "#4E3446"
H = np.array([0.0, -0.02, 0.565])            # the head's middle (the head bone is at 0, -0.01, 0.45)
HR = np.array([0.152, 0.138, 0.142])


def tyrogue(src, out):
    k = Kit(src, coat="fur")

    # The head: big and round, three blunt horns across the crown, a raised disc on each side
    k.add("head", ell(H, HR), SKIN, "head")
    k.add("hornmid", tube([(0, -0.01, 0.68), (0, 0.0, 0.75), (0, 0.006, 0.79)], [0.052, 0.042, 0.032]), SKIN, "head")
    k.add("horn", tube([(0.085, -0.005, 0.665), (0.118, 0.0, 0.72), (0.135, 0.005, 0.75)], [0.042, 0.034, 0.027]),
          SKIN, "head", mirror=True)
    for s, side in ((1, "R"), (-1, "L")):
        d = np.array([s, 0, 0.12])
        k.add(f"disc{side}", lathe(np.array([0.128 * s, 0.004, 0.555]), d,
                                   [(0, 0), (0.068, 0), (0.07, 0.019), (0.06, 0.033), (0, 0.036)], seg=40),
              SKIN, "head", fuse=False)

    # The trunk: a short lilac waist under a thick neck, a white band round it, bulging brown shorts below
    k.add("neck", ell((0, 0.0, 0.445), (0.056, 0.05, 0.045)), SKIN, "head")
    k.add("torso", ell((0, 0.0, 0.385), (0.074, 0.064, 0.064)), SKIN, "body")
    def band_at(u, v):
        a = 2 * math.pi * u
        return np.stack([np.sin(a) * 0.078, np.cos(a) * 0.068 + 0.002, 0.333 + (v - 0.5) * 0.03 + 0 * u], -1)
    k.add("waistband", outward(sheet(band_at, 97, 3, 0.012)), WRAP, "body", fuse=False, surface="cloth")
    k.add("shorts", ell((0, 0.012, 0.268), (0.12, 0.096, 0.078)), SHORTS, "body", surface="cloth")
    k.add("seat", ell((0, 0.05, 0.262), (0.1, 0.07, 0.07)), SHORTS, "body", surface="cloth")
    k.add("cuff", ell((0.066, 0.01, 0.222), (0.064, 0.07, 0.058)), SHORTS, "leg{s}", mirror=True, surface="cloth")

    # The legs: lilac, tapering a little, on round brown shoes
    k.add("leg", tube([(0.07, 0.0, 0.22), (0.086, -0.008, 0.13), (0.094, -0.004, 0.06)], [0.036, 0.032, 0.03]),
          SKIN, "leg{s}", mirror=True)
    k.add("shoe", ell((0.097, -0.03, 0.033), (0.056, 0.078, 0.036)), SHORTS, "leg{s}", mirror=True, surface="leather")

    # The arms: lilac, hanging forward and a little out; a white wrap at the wrist; a big splayed hand
    SH, EL, WR = np.array([0.08, 0.0, 0.4]), np.array([0.13, -0.035, 0.325]), np.array([0.152, -0.085, 0.245])
    k.add("arm", tube([SH, EL, WR], [0.037, 0.033, 0.031]), SKIN, "arm{s}", mirror=True)
    d = unit(*(WR - EL))
    k.add("wrap", ring_band(EL + (WR - EL) * 0.72, d, 0.034, 0.032, thick=0.007), WRAP, "arm{s}", fuse=False,
          mirror=True, surface="cloth")
    PALM = np.array([0.16, -0.1, 0.216])
    k.add("palm", ell(PALM, (0.034, 0.028, 0.034)), SKIN, "arm{s}", mirror=True)
    for i, dd in enumerate(((0.55, -0.35, -0.75), (0.1, -0.45, -0.9), (-0.35, -0.45, -0.8), (-0.85, -0.5, -0.1))):
        dd = unit(*dd)
        a = PALM + dd * 0.02
        ln = 0.04 if i == 3 else 0.055
        k.add(f"finger{i}", tube([a, a + dd * ln], [0.017, 0.016]), SKIN, "arm{s}", mirror=True)

    # A small frown
    CS, CM = srgb(SKIN), srgb(MOUTH)
    def frown(u, v):
        c = np.tile(CS, (len(u), 1))
        c[(np.abs(v - (-0.45 + 0.75 * (1 - u * u))) < 0.3) & (np.abs(u) < 0.9)] = CM
        return c
    k.decal((0, -0.125, 0.475), unit(0, -1, -0.5), (0.026, 0.012), frown, eps=0.003, rings=10, segs=56)

    k.build(voxel=0.006, smooth=4, band=0.004)

    # The eyes: fierce almonds, a heavy dark line along the top sloping to the inner corner, amber with a black slit
    CK, CA = srgb(INK), srgb(AMBER)
    OUT = [(-1.0, -0.32), (-0.5, 0.05), (0.1, 0.42), (0.6, 0.62), (0.95, 0.5), (1.0, 0.0), (0.7, -0.5), (0.1, -0.7),
           (-0.5, -0.6)]
    def top(u):
        return np.interp(u, [-1.0, -0.5, 0.1, 0.6, 0.95, 1.0], [-0.32, 0.05, 0.42, 0.62, 0.5, 0.0])
    def open_eye(u, v):
        c = np.tile(CK, (len(u), 1))
        amber = (((u - 0.04) / 0.86) ** 2 + ((v + 0.06) / 0.6) ** 2 < 1) & (v < top(u) - 0.24)
        c[amber] = CA
        c[((u - 0.12) / 0.13) ** 2 + ((v + 0.08) / 0.44) ** 2 < 1] = CK
        c[((u - 0.36) / 0.1) ** 2 + ((v - 0.02) / 0.1) ** 2 < 1] = (1, 1, 1)
        return c
    E = np.array([0.066, -0.14, 0.545])
    def lay(state):
        look = eye_look(state, open_eye, CS, CK, line=0.18)
        for s in (1, -1):
            shape = polygon([(u * s, v) for u, v in OUT])
            col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
            k.decal(E * np.array([s, 1, 1]), unit(0.42 * s, -0.9, 0.05), (0.05, 0.044), col, up=(0, 0.1, 1),
                    eps=0.005, outline=shape, rings=20, segs=56)

    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    tyrogue(*args())
