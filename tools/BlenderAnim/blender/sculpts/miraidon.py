# Miraidon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/miraidon.py -- <the export's Miraidon.glb> <out.glb>
#
# From the references (the Ultimate Mode, as the game's own sculptor stands it): a violet machine of a dragon, rearing.
# Its largest mass is a great rounded trunk of pale lilac swirled with white, a yellow band down its front from the
# throat; under it a dark violet abdomen between two great round pods at the hips (its wheels,
# folded), short dark legs ringed silver below them on white-clawed feet. Thick dark violet arms with silver elbows are
# held forward and down to hands of great white claws. A long neck rises from the trunk's top and bends forward to a
# helmeted head: a glossy black dome with a lilac stripe running back to a point, the face under it a white visor with
# small green eyes. Two bolts of lightning, pale blue round a yellow core, rise and sweep back from the helmet. Two dark
# feelers curl from the back of the neck's root, a dark blade stands off each shoulder blade, and a long tail, yellow on
# top and lilac under, banded silver, curls out back and to the side to a lightning fin.
# Bones: body (0, 0, 0.5); arms (+-0.1, -0.04, 0.66); neck (0, -0.04, 0.68); head (0, -0.08, 1.14);
# ears (+-0.03, -0.12, 1.21); tail (0, 0.1, 0.46); legs (+-0.1, 0.04, 0.42).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, lathe, ring_band, srgb, inside, unit, polygon, eye_look, args

VIOLET = "#4636B0"
DARK = "#2A2078"
LILAC = "#B6B6F2"
SWIRL = "#F0F2FF"
BLUE = "#8CB4F6"
YELLOW = "#F6EC7A"
SILVER = "#C6C6D6"
HELM = "#1C1830"
VISOR = "#E8E8F2"
CLAW = "#F4F4FA"
BOLT = "#A8D6FA"
BOLTCORE = "#FAF08A"
GREEN = "#3EB888"
INK = "#121020"

HC = np.array([0.0, -0.185, 1.22])       # the helmet's middle
HR = np.array([0.108, 0.165, 0.096])
TORSO_C, TORSO_R = np.array([0.0, -0.01, 0.67]), np.array([0.19, 0.17, 0.215])


def on_ell(c, r, d):
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / np.array(r)) ** 2).sum())
    return np.array(c, float) + d * t


def kite(base, d, length, width, thick, up=(0, 0, 1), sink=0.015):
    """A solid point: a kite in section, wide across up, thick across the other way (a claw, a blade)."""
    base, d = np.array(base, float), unit(*d)
    up = np.array(up, float); up = up - d * (up @ d); up /= np.linalg.norm(up)
    side = np.cross(d, up)
    b = base - d * sink
    m = base + d * length * 0.45
    return hull([b + up * width / 2, b - up * width / 2, b + side * thick / 2, b - side * thick / 2,
                 m + up * width * 0.34, m - up * width * 0.34, m + side * thick * 0.34, m - side * thick * 0.34,
                 base + d * length])


BOLT_OUT = [(0.0, -0.034), (0.12, -0.054), (0.11, -0.016), (0.27, -0.04), (0.135, 0.04), (0.145, 0.006),
            (0.0, 0.034)]


def bolt(k, name, root, ex, n, bone, scale=1.0, mirror=False):
    """A bolt of lightning: a pale blue plate with a thicker yellow core showing through both its faces."""
    ex = unit(*ex); n = unit(*n)
    ez = np.cross(n, ex)
    out = [(a * scale, b * scale) for a, b in BOLT_OUT]
    core = [(0.02 * scale + a * 0.8, b * 0.5) for a, b in out]
    k.add(name, slab(out, root, ex, ez, 0.026 * scale), BOLT, bone, fuse=False, mirror=mirror, surface="metal")
    k.add(name + "core", slab(core, root, ex, ez, 0.036 * scale), BOLTCORE, bone, fuse=False, mirror=mirror,
          glow=BOLTCORE, surface="metal")


# --- the trunk --------------------------------------------------------------------------------------------------------
def trunk(k):
    k.add("torso", ell(TORSO_C, TORSO_R, rot=(-14, 0, 0)), LILAC, "body")
    k.add("abdomen", ell((0, 0.025, 0.44), (0.125, 0.115, 0.11)), DARK, "body")
    k.add("neckroot", ell((0, -0.05, 0.84), (0.085, 0.08, 0.07)), VIOLET, "neck")
    # the yellow band down the front from the throat
    def band(P):
        w = 0.042 + 0.02 * np.clip((0.82 - P[:, 2]) / 0.3, 0, 1)
        return inside(np.abs(P[:, 0]) - w, 0.003) * inside(P[:, 1] + 0.06, 0.004) * inside(P[:, 2] - 0.9, 0.004) \
            * inside(0.5 - P[:, 2], 0.004)
    k.paint(band, YELLOW, on=["torso", "neckroot"])
    # white swirls with blue beside them over the sides and back
    def swirl(P, z0, slope, w):
        return inside(np.abs(P[:, 2] - (z0 + slope * P[:, 1] + 0.9 * P[:, 0] ** 2)) - w, 0.003) \
            * inside(0.065 - np.abs(P[:, 0]) - (P[:, 1] > -0.03) * 0.2, 0.004)
    for z0, slope in ((0.585, -0.75), (0.745, -0.6)):
        k.paint(lambda P, z0=z0, slope=slope: swirl(P, z0 - 0.038, slope, 0.016), BLUE, on=["torso"])
        k.paint(lambda P, z0=z0, slope=slope: swirl(P, z0, slope, 0.022), SWIRL, on=["torso"])
    # dark feelers curling from the back of the neck's root, a dark blade off each shoulder blade
    k.add("feeler", tube([(0.05, 0.07, 0.86), (0.12, 0.15, 0.86), (0.19, 0.19, 0.8), (0.21, 0.16, 0.72),
                          (0.18, 0.11, 0.69)], [0.03, 0.028, 0.026, 0.025, 0.024]), VIOLET, "neck", mirror=True)
    k.add("backblade", kite((0.08, 0.12, 0.66), (0.45, 0.75, 0.5), 0.17, 0.06, 0.034, up=(0, -0.5, 1)), HELM, "body",
          fuse=False, mirror=True, surface="metal")


def pods(k):
    """The wheels folded at the hips: great round dark pods, a silver hub on the outer face, short legs below."""
    for s, sd in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        bone = f"leg{sd}"
        k.add(f"pod{sd}", ell(np.array((0.16, 0.01, 0.33)) * m, (0.115, 0.135, 0.135)), VIOLET, bone)
        k.add(f"hub{sd}", lathe(np.array((0.268, 0.01, 0.33)) * m, (s, 0, 0),
                                [(0, -0.01), (0.04, -0.01), (0.048, 0.004), (0.035, 0.016), (0, 0.016)], seg=36),
              SILVER, bone, fuse=False, surface="metal")
        k.add(f"shin{sd}", tube(np.array([(0.16, 0.07, 0.22), (0.17, 0.06, 0.13), (0.17, 0.04, 0.07)]) * m,
                                [0.045, 0.04, 0.038]), DARK, bone)
        for z in (0.17, 0.1):
            k.add(f"ring{sd}{z}", ring_band(np.array((0.168, 0.058 - (0.17 - z) * 0.2, z)) * m, (0, -0.15, 1),
                                            0.042, 0.022, thick=0.01), SILVER, bone, fuse=False, surface="metal")
        k.add(f"foot{sd}", ell(np.array((0.17, -0.02, 0.034)) * m, (0.055, 0.085, 0.034)), SILVER, bone)
        for j, dx in enumerate((-0.03, 0.0, 0.03)):
            k.add(f"toe{sd}{j}", kite(np.array((0.17 + dx, -0.09, 0.03)) * m, np.array((dx * 3, -1, -0.3)) * m, 0.05,
                                      0.03, 0.022), CLAW, bone, fuse=False, surface="metal")


def arms(k):
    for s, sd in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        bone = f"arm{sd}"
        k.add(f"shoulder{sd}", ell(np.array((0.175, -0.03, 0.72)) * m, (0.075, 0.075, 0.072)), VIOLET, bone)
        k.add(f"upper{sd}", tube(np.array([(0.185, -0.03, 0.71), (0.24, -0.08, 0.62)]) * m, [0.052, 0.046]), VIOLET, bone)
        k.add(f"elbow{sd}", ell(np.array((0.24, -0.085, 0.615)) * m, (0.044, 0.044, 0.044)), SILVER, bone,
              fuse=False, surface="metal")
        k.add(f"fore{sd}", tube(np.array([(0.235, -0.09, 0.61), (0.2, -0.19, 0.56), (0.162, -0.27, 0.535)]) * m,
                                [0.046, 0.046, 0.05]), DARK, bone)
        # great white claws, the two outer long and curved, a short inner one
        for j, (dx, dz, L) in enumerate(((0.03, 0.01, 0.1), (-0.005, 0.02, 0.11), (-0.035, -0.01, 0.07))):
            base = np.array((0.162 + dx, -0.305, 0.535 + dz))
            d = unit(dx * 2, -1, -0.45)
            k.add(f"claw{sd}{j}", hull([base + (0, 0, 0.018), base - (0, 0, 0.018), base + (0.016, 0, 0), base - (0.016, 0, 0),
                                        base + d * L * 0.55 + (0, 0, 0.012), base + d * L * 0.55 - (0, 0, 0.004),
                                        base + d * L + (0, 0.02, -0.03)] * m), CLAW, bone, fuse=False, surface="metal")


NECK = np.array([(0, -0.06, 0.84), (0, -0.01, 0.96), (0, -0.06, 1.07), (0, -0.11, 1.14)])


def neck(k):
    k.add("neck", tube(NECK, [0.085, 0.074, 0.066, 0.062]), VIOLET, "neck")
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.022, 0.003) * inside(P[:, 1] - np.interp(P[:, 2], NECK[:, 2], NECK[:, 1])
                                                                     + 0.035, 0.004) * inside(P[:, 2] - 1.1, 0.004),
            YELLOW, on=["neck"])


TAIL = np.array([(0, 0.1, 0.44), (0, 0.23, 0.32), (0.05, 0.35, 0.2), (0.13, 0.45, 0.13), (0.23, 0.52, 0.12),
                 (0.31, 0.55, 0.16)])
TAIL_R = [0.075, 0.062, 0.05, 0.04, 0.032, 0.025]


def tail(k):
    k.add("tail", tube(TAIL, TAIL_R), LILAC, "tail")
    dense = np.vstack([TAIL[i] + (TAIL[i + 1] - TAIL[i]) * t for i in range(len(TAIL) - 1) for t in np.linspace(0, 1, 20)])
    seg = np.r_[0, np.cumsum(np.linalg.norm(np.diff(dense, axis=0), axis=1))]
    def nearest(P):
        return np.argmin(((P[:, None, :] - dense[None]) ** 2).sum(2), axis=1)
    def top(P):
        i = nearest(P)
        return inside(dense[i, 2] - P[:, 2] + 0.004, 0.004)
    k.paint(top, YELLOW, on=["tail"])
    def bands(P):
        i = nearest(P)
        t = seg[i]
        return ((t > 0.18) & ((t / 0.075) % 1 < 0.28)).astype(float)
    k.paint(bands, SILVER, on=["tail"])
    # the lightning fin at its end
    bolt(k, "tailfin", TAIL[-1] + (-0.01, -0.005, 0.0), (0.7, 0.35, 0.4), (-0.2, 0.75, -0.6), "tail", scale=0.85)


# --- the head ---------------------------------------------------------------------------------------------------------
def head(k):
    k.add("helmet", ell(HC, HR, rot=(-12, 0, 0)), VISOR, "head")
    k.add("chin", ell(HC + (0, -0.025, -0.068), (0.074, 0.11, 0.05), rot=(-10, 0, 0)), VISOR, "head")
    k.add("helmback", kite(HC + (0, 0.11, 0.05), (0, 1, 0.45), 0.12, 0.085, 0.06, up=(1, 0, 0), sink=0.04), HELM, "head",
          fuse=False, surface="metal")
    def dome(P):
        q = P - HC
        return inside(-q[:, 2] + 0.012 + 0.22 * (-q[:, 1]), 0.003)
    k.paint(dome, HELM, on=["helmet"])
    def stripe(P):
        q = P - HC
        return inside(np.abs(q[:, 0]) - 0.016, 0.003) * inside(-q[:, 2] + 0.04 + 0.22 * (-q[:, 1]), 0.003)
    k.paint(stripe, LILAC, on=["helmet"])
    # the back of the head under the dome violet, running into the neck
    k.paint(lambda P: inside(-(P[:, 1] - HC[1]) + 0.03, 0.006) * inside((P[:, 2] - HC[2]) - 0.012 - 0.22 * (HC[1] - P[:, 1]),
                                                                        0.003), VIOLET, on=["helmet", "chin"])
    # the bolts of lightning rising and sweeping back from the helmet's sides
    bolt(k, "antenna", np.array((0.075, -0.14, 1.29)), (0.45, 0.6, 0.6), (0.3, -0.9, 0.3), "ear{s}", scale=1.2, mirror=True)


EYE_OUT = polygon([(-1, -0.4), (-0.4, -0.85), (0.6, -0.75), (1.05, -0.05), (0.7, 0.6), (-0.1, 0.7), (-0.8, 0.35)])
CGN, CK, CW, LID = srgb(GREEN), srgb(INK), srgb("#FFFFFF"), srgb(VISOR)


def eye_open(u, v):
    c = np.tile(CGN, (len(u), 1))
    c[(u / 1.0) ** 2 + (v / 0.68) ** 2 > 0.7] = CK
    c[((u + 0.05) / 0.28) ** 2 + ((v + 0.05) / 0.42) ** 2 < 1] = CK
    c[(u - 0.3) ** 2 + (v - 0.25) ** 2 < 0.03] = CW
    return c


DE = unit(0.62, -0.72, -0.12)


def eyes(k, state):
    E = on_ell(HC, HR, DE) + np.array([0, -0.01, 0.0])
    look = eye_look(state, eye_open, LID, CK, line=0.2)
    m = np.array([-1, 1, 1.0])
    k.decal(E, DE, (0.04, 0.03), look, eps=0.002, rings=20, segs=56, outline=EYE_OUT)
    k.decal(E * m, DE * m, (0.04, 0.03), lambda u, v: look(-u, v), eps=0.002, rings=20, segs=56,
            outline=lambda th: EYE_OUT(math.pi - th))


def miraidon(src, out_path):
    k = Kit(src, coat="metal")
    trunk(k)
    pods(k)
    arms(k)
    neck(k)
    tail(k)
    head(k)
    k.build(voxel=0.0075, smooth=4, band=0.006)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out_path)


if __name__ == "__main__":
    miraidon(*args())
