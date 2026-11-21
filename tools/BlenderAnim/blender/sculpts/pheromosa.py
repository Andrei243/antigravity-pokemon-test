# Pheromosa sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pheromosa.py -- <the export's Pheromosa.glb> <out.glb>
#
# From the references: a slender white insect like a dancer. An oval white head, wider at the brow and narrowing to the
# chin, with a gold tiara across the brow rising to a point at each side; heavy half-shut eyes, a dark lashed lid over
# a blue-violet iris; a thin neck to a gold collar; a torso of three flared white tiers like a stack of lampshades, the
# top one widest; a narrow waist over a short flared skirt; long thin white arms with gold cuffs (carried forward and
# down here); very long legs with gold knee knobs ending in pointed gold feet; a great pale veil of wings behind it from
# the back of the head to the ground, an oval forked at its foot; two long antennae arching up and out and down,
# tipped gold (shorter than the art's, which reach the ground).
# The skeleton: body (0, 0, 0.55), armL/R (-+0.03, 0, 0.67), head (0, 0, 0.68), legL/R (-+0.025, 0, 0.46).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, sheet, lathe, ring_band, outward, polygon, srgb, inside, smoothstep, unit,
                       eye_look, args)

WHITE, SHADE, GOLD, VEIL = "#F3F2EE", "#DEDDD8", "#E1BF63", "#D7D8E0"
IRIS, INK, LASH = "#5560B8", "#22202C", "#1E1A26"

HC, HR = np.array([0.0, -0.004, 0.79]), np.array([0.056, 0.05, 0.06])
VOXEL = 0.0042


def on_ell(c, r, d):
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r)) ** 2).sum())


def head_shape(V):
    """Wider at the brow, narrowing to the chin."""
    V = V.copy()
    z = V[:, 2]
    V[:, :2] *= (1 - 0.28 * np.clip(-z, 0, 1) ** 1.3)[:, None]
    return V


def tier(k, name, top, bottom, r_top, r_bot):
    """A flared tier of the torso: a crisp lathe, white above and its underside shaded."""
    k.add(name, lathe((0, 0, 0), (0, 0, 1), [(0, top), (r_top, top), (r_bot, bottom + 0.004), (r_bot, bottom),
                                               (r_bot * 0.75, bottom - 0.002), (0, bottom - 0.002)], seg=48),
          WHITE, "body", fuse=False)


def pheromosa(src, out):
    k = Kit(src, coat="shell")

    # --- the head: an oval wider at the brow, a gold tiara --------------------------------------------------------------
    k.add("head", ell(HC, HR, deform=head_shape), WHITE, "head")
    # the tiara: a gold band across the brow, dipping to a point between the eyes
    def tiara(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        low = 0.806 - 0.012 * np.clip(1 - np.abs(x) / 0.02, 0, 1)
        return inside(low - z, 0.0018) * inside(z - 0.822, 0.0018) * smoothstep(0.0, -0.02, y)
    k.paint(tiara, GOLD, on=["head"])
    for s, side in ((1, "R"), (-1, "L")):
        b = np.array([0.046 * s, -0.026, 0.808])
        k.add(f"tiara{side}", hull([b + (0.0, 0.012, -0.008), b + (0.0, -0.01, -0.008), b + (-0.012 * s, 0.0, 0.004),
                                    b + (0.022 * s, 0.006, 0.042)]), GOLD, "head", fuse=False, surface="metal")

    # --- the neck, collar and tiers -----------------------------------------------------------------------------------
    k.add("neck", tube([(0, 0.0, 0.66), (0, 0.0, 0.705), (0, -0.002, 0.745)], [0.016, 0.0145, 0.016]), WHITE, "head")
    k.add("collar", ell((0, 0.0, 0.684), (0.03, 0.027, 0.011)), GOLD, "body", fuse=False, surface="metal")
    k.add("torso", ell((0, 0.0, 0.625), (0.033, 0.028, 0.065)), WHITE, "body")
    tier(k, "tier1", 0.68, 0.652, 0.026, 0.072)
    tier(k, "tier2", 0.652, 0.624, 0.03, 0.058)
    tier(k, "tier3", 0.624, 0.598, 0.026, 0.046)
    k.add("waist", ell((0, 0.0, 0.575), (0.026, 0.023, 0.03)), WHITE, "body")
    k.add("skirt", lathe((0, 0, 0), (0, 0, 1), [(0, 0.566), (0.026, 0.566), (0.058, 0.5), (0.05, 0.494), (0, 0.494)],
                         seg=48), WHITE, "body", fuse=False)

    # --- the arms: long and thin, forward and down, gold cuffs --------------------------------------------------------
    SH, EL, WR = np.array([0.05, 0.0, 0.648]), np.array([0.086, -0.02, 0.56]), np.array([0.066, -0.085, 0.505])
    k.add("upper", tube([SH, (SH + EL) / 2, EL], [0.017, 0.016, 0.014]), WHITE, "arm{s}", mirror=True)
    k.add("fore", tube([EL, (EL + WR) / 2, WR], [0.014, 0.014, 0.0128]), WHITE, "arm{s}", mirror=True)
    k.add("hand", ell(WR + (0, -0.01, -0.006), (0.014, 0.016, 0.018)), WHITE, "arm{s}", mirror=True)
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        w = WR * m
        k.add(f"cuff{side}", ring_band(w + (EL * m - w) * 0.12, EL * m - w, 0.0155, 0.016, thick=0.006), GOLD,
              f"arm{side}", fuse=False, surface="metal")
        t = w + np.array([0, -0.02, -0.028])
        k.add(f"claw{side}", hull([t + (-0.01, 0.008, 0.012), t + (0.01, 0.008, 0.012), t + (0, -0.006, 0.014),
                                   t + (0, 0.002, -0.014)]), GOLD, f"arm{side}", fuse=False, surface="metal")

    # --- the legs: very long, gold knees, pointed gold feet ----------------------------------------------------------
    HP, KN, AN = np.array([0.03, 0.0, 0.5]), np.array([0.036, -0.012, 0.25]), np.array([0.04, 0.0, 0.058])
    k.add("thigh", tube([HP, (HP + KN) / 2, KN], [0.031, 0.025, 0.019]), WHITE, "leg{s}", mirror=True)
    k.add("shin", tube([KN, (KN + AN) / 2, AN], [0.0185, 0.017, 0.0135]), WHITE, "leg{s}", mirror=True)
    k.add("knee", ell(KN + (0, -0.008, 0), (0.022, 0.02, 0.026)), GOLD, "leg{s}", fuse=False, mirror=True,
          surface="metal")
    for s, side in ((1, "R"), (-1, "L")):
        a = AN * np.array([s, 1, 1.0])
        k.add(f"foot{side}", hull([a + (-0.016, 0.012, 0.02), a + (0.016, 0.012, 0.02), a + (0, -0.02, 0.02),
                                   a + (-0.012, 0.01, -0.02), a + (0.012, 0.01, -0.02), a + (0, -0.032, -0.058)]),
              GOLD, f"leg{side}", fuse=False, surface="metal")
    # the gold collar and knees show crisp; a shade under each tier on the torso
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.6) - 0.02, 0.004), SHADE, on=["torso", "waist"])

    # --- the veil of wings behind it, from the back of the head to the ground ----------------------------------------
    def veil(u, v):
        x = (u * 2 - 1)                                          # -1..1 across
        z = 0.8 - 0.79 * v                                       # top to foot
        w = 0.035 + 0.19 * np.sin(np.pi * np.clip(v / 0.72, 0, 1) ** 0.75) ** 0.8 * (v < 0.72) \
            + (v >= 0.72) * (0.225 + 0.03 * (v - 0.72) / 0.28)
        fork = 0.07 * (1 - np.abs(x)) * smoothstep(0.85, 1.0, v)  # the foot forked at its middle
        X = x * w
        Y = 0.035 + 0.07 * (1 - x * x) * np.sin(np.pi * np.clip(v, 0.05, 0.95)) + 0.02 * v
        return np.stack([X, Y, z + fork], -1)
    k.add("veil", outward(sheet(veil, 41, 70, 0.008)), VEIL, "body", fuse=False)

    # --- the antennae: up and out from the head's top, down to gold tips ----------------------------------------------
    for s, side in ((1, "R"), (-1, "L")):
        A = np.array([(0.016, 0.004, 0.842), (0.06, 0.02, 0.915), (0.15, 0.03, 0.9), (0.21, 0.03, 0.75),
                      (0.232, 0.025, 0.56), (0.236, 0.02, 0.43)]) * np.array([s, 1, 1.0])
        k.add(f"antenna{side}", outward(tube(A, [0.0135] * 6)), WHITE, "head", fuse=False)
        k.add(f"atip{side}", ell(A[-1] + (0, 0, -0.03), (0.019, 0.017, 0.034)), GOLD, "head", fuse=False,
              surface="metal")

    k.build(voxel=VOXEL, smooth=4, band=0.005, wband=0.04)

    # --- the eyes: half shut, a dark lashed lid over a blue-violet iris ---------------------------------------------
    CW, CI, CK, CL = srgb(WHITE), srgb(IRIS), srgb(INK), srgb(LASH)
    def lidded(slope, base):
        def look(u, v):
            c = np.tile(srgb("#FFFFFF"), (len(u), 1))
            c[((u + 0.1) / 0.5) ** 2 + ((v + 0.3) / 0.6) ** 2 < 1] = CI
            c[((u + 0.1) / 0.22) ** 2 + ((v + 0.35) / 0.3) ** 2 < 1] = CK
            lid = base + slope * u
            c[v > lid] = CW
            c[np.abs(v - lid) < 0.17] = CL
            c[(v > lid) & (v < lid + 0.4) & (u > 0.55)] = CL            # the lashes swept out at the corner
            return c
        return look
    open_eye = lidded(0.15, 0.0)
    fierce = lidded(0.45, -0.1)
    ALMOND = [(-1, 0.0), (-0.6, -0.6), (0.2, -0.85), (0.85, -0.45), (1, 0.2), (0.55, 0.7), (-0.2, 0.75), (-0.75, 0.45)]
    def lay(state):
        look = fierce if state == "Fierce" else eye_look(state, open_eye, CW, CL, line=0.17)
        for s in (1, -1):
            shape = polygon([(u * s, v) for u, v in ALMOND])
            col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
            d = unit(0.5 * s, -0.85, 0.05)
            k.decal(on_ell(HC, HR, d) + (0, 0, -0.012), d, (0.02, 0.015), col, up=(0, 0.1, 1), eps=0.002,
                    outline=shape, rings=20, segs=56)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    pheromosa(*args())
