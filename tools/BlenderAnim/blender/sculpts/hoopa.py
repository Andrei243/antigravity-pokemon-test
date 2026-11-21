# Hoopa (Confined) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/hoopa.py -- <the export's Hoopa.glb> <out.glb>
#
# From the references: a small pink-and-lilac imp that floats, its head more than half its height and twice the
# torso's width. The head lilac below, a pink cap over its top coming down at the sides to two pink lobes, from which a
# dark grey horn curves out and up on each side, a golden ring round each horn; a pink topknot curling over to the +X
# side; a golden ring set in the cap over the brow. Big eyes, yellow round a green iris and a black slit, their tops
# cut straight and sloping down toward the nose (mischievous); pink cheek dots; a wide grin, dark, with two white front
# teeth. A small lilac torso with a pink V of a collar, a golden ring round the waist, two stubby legs ending in dark
# points. The arms: a short lilac arm into a big pink cuff and a lilac cone of a hand pointing forward and down; the
# -X hand holds the great golden ring (the art holds it raised: carried low here, its face to the front).
# Bones: body (0, 0, 0.18); head (0, -0.01, 0.2); armR/L (+-0.04, 0, 0.18); earR/L (+-0.04, -0.005, 0.3) (the horns);
# legR/L (+-0.02, 0, 0.11).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, outward, srgb, inside, unit, eye_look, polygon, args

LILAC, PINK, GOLD, HORN, INKH = "#C4BAD8", "#E8467C", "#F4CE3A", "#5C5660", "#2A1A24"
SCLERA, IRIS, MOUTH, CHEEK = "#EEEE7A", "#62BE38", "#4A2034", "#F49AB8"

C, R = np.array([0.0, -0.014, 0.272]), np.array([0.103, 0.087, 0.084])


def on_head(d):
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / R) ** 2).sum())
    return C + d * t


def torus(c, n, big, small, seg=56, ring=14):
    t = np.linspace(0, 2 * math.pi, ring + 1)
    prof = [(big + small * math.cos(a), small * math.sin(a)) for a in t]
    return lathe(c, n, prof, seg=seg)


def head(k):
    k.add("head", ell(C, R), LILAC, "head")
    k.add("lobe", ell((0.098, 0.0, 0.3), (0.03, 0.036, 0.036)), PINK, "head", mirror=True)
    k.add("knot", ell((0.0, 0.02, 0.352), (0.034, 0.03, 0.022)), PINK, "head")
    k.add("tail", tube([(0.0, 0.03, 0.359), (0.03, 0.04, 0.384), (0.07, 0.042, 0.384), (0.1, 0.038, 0.364),
                        (0.112, 0.034, 0.344)], [0.022, 0.02, 0.016, 0.012, 0.009], seg=20, per=6), PINK, "head")

    # the cap: pink over the top, dipping toward the lobes at the sides
    def cap(P):
        q = (P - C) / R
        return inside(0.66 - 0.42 * q[:, 0] ** 2 - q[:, 2], 0.03)
    k.paint(cap, PINK, on=["head"])
    # the horns: dark grey, curving out and up from the lobes
    for s, sd in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        pts = np.array([(0.114, 0.0, 0.3), (0.144, 0.004, 0.312), (0.166, 0.008, 0.34), (0.172, 0.01, 0.375)]) * m
        k.add(f"horn{sd}", tube(pts, [0.021, 0.017, 0.012, 0.006], seg=20, per=6), HORN, f"ear{sd}")
        k.add(f"hornring{sd}", torus(np.array((0.158, 0.006, 0.326)) * m, unit(0.7 * s, 0, 0.75), 0.024, 0.0062),
              GOLD, f"ear{sd}", fuse=False, surface="metal")


def body(k):
    k.add("torso", ell((0.0, 0.0, 0.165), (0.043, 0.038, 0.045)), LILAC, "body")
    for s, sd in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"leg{sd}", tube(np.array([(0.018, 0.0, 0.135), (0.026, -0.006, 0.095), (0.03, -0.012, 0.068)]) * m,
                               [0.021, 0.017, 0.012], seg=16, per=5), LILAC, f"leg{sd}")
        k.add(f"foot{sd}", tube(np.array([(0.03, -0.012, 0.075), (0.033, -0.016, 0.05)]) * m, [0.012, 0.004],
                                seg=16, per=4), HORN, f"leg{sd}")
    k.add("waistring", torus((0.0, 0.0, 0.135), unit(0.18, 0.08, 1), 0.047, 0.0068), GOLD, "body", fuse=False,
          surface="metal")
    # the pink V of the collar, lilac within
    V = polygon([(-1, 1), (1, 1), (0, -1)])
    k.decal((0.0, -0.037, 0.18), unit(0, -1, 0.1), (0.034, 0.026), PINK, outline=V, eps=0.003, rings=10, segs=60)
    k.decal((0.0, -0.038, 0.187), unit(0, -1, 0.1), (0.019, 0.015), LILAC, outline=V, eps=0.0045, rings=8, segs=60)


HAND_D = unit(0.32, -0.6, -0.73)


def arms(k):
    for s, sd in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        cuff = np.array((0.082, -0.03, 0.162)) * m
        k.add(f"arm{sd}", tube(np.array([(0.038, 0.0, 0.178), (0.07, -0.022, 0.166)]) * m, [0.015, 0.016], seg=16,
                               per=4), LILAC, f"arm{sd}")
        k.add(f"cuff{sd}", ell(cuff, (0.03, 0.031, 0.028), rot=(30, 0, 0)), PINK, f"arm{sd}")
        d = HAND_D * m
        prof = [(0.0, -0.01), (0.022, 0.0), (0.027, 0.02), (0.02, 0.05), (0.0, 0.08)]
        k.add(f"hand{sd}", lathe(cuff + d * 0.012, d, prof, seg=36), LILAC, f"arm{sd}", fuse=False, surface="fur")
        tip = cuff + d * 0.068
        k.add(f"handtip{sd}", lathe(tip, d, [(0.0, -0.002), (0.0085, 0.0), (0.0, 0.026)], seg=24), GOLD,
              f"arm{sd}", fuse=False, surface="metal")


def big_ring(k):
    """The great ring, held through the -X hand, its face turned to the front and a little out."""
    mid = np.array((-0.082, -0.03, 0.162)) + HAND_D * np.array([-1, 1, 1]) * 0.035
    n = unit(-0.4, -0.92, 0)
    h = unit(0.92, -0.4, 0)
    c = mid - h * 0.062 + np.array([0, 0, 0.05])
    k.add("bigring", torus(c, n, 0.078, 0.0085, seg=72), GOLD, "armL", fuse=False, surface="metal")


# --- the face ---------------------------------------------------------------------------------------------------
EYE_N = unit(0.42, -0.86, 0.12)
EA, EB = 0.42, 0.42                    # the eye's top: v = EA + EB u (+u outward), sloping down toward the nose


def cut(th):
    s = math.sin(th) - EB * math.cos(th)
    return min(1.0, EA / s) if s > 1e-6 else 1.0


CS, CI, CK, CL = srgb(SCLERA), srgb(IRIS), srgb(INKH), srgb(LILAC)


def eye_open(u, v):
    c = np.tile(CS, (len(u), 1))
    c[((u + 0.08) / 0.62) ** 2 + ((v + 0.05) / 0.66) ** 2 < 1] = CI
    c[((u + 0.08) / 0.17) ** 2 + ((v + 0.05) / 0.42) ** 2 < 1] = CK
    c[v > EA + EB * u - 0.22] = CK          # the dark lid along the cut top
    return c


def marks(k):
    # the golden ring set in the cap over the brow
    g = on_head(unit(0, -0.84, 0.54))
    k.decal(g, unit(0, -0.84, 0.54), (0.014, 0.016), GOLD, eps=0.003, rings=10, segs=48)
    k.decal(g, unit(0, -0.84, 0.54), (0.007, 0.009), PINK, eps=0.0045, rings=8, segs=40)
    # pink cheek dots
    ch = unit(0.62, -0.72, -0.3)
    k.decal(on_head(ch), ch, (0.011, 0.011), CHEEK, eps=0.003, rings=8, segs=40, mirror=True)
    # the grin: a half moon, flat on top; two white front teeth under its top edge
    mn = unit(0, -1, -0.32)
    mc = on_head(mn)
    k.decal(mc, mn, (0.04, 0.018), MOUTH, eps=0.003, rings=12, segs=72,
            outline=lambda th: min(1.0, 0.55 / math.sin(th)) if math.sin(th) > 1e-6 else 1.0)
    for x in (-0.0105, 0.0105):
        tn = unit(x * 3, -1, -0.26)
        k.decal(on_head(tn) + np.array([0, 0, 0.0045]), tn, (0.0095, 0.0068), "#F8F6F2", eps=0.005, rings=8, segs=40,
                outline=polygon([(-1, -1), (1, -1), (1, 1), (-1, 1)]))


def eyes(k, state):
    e = on_head(EYE_N)
    look = eye_look(state, eye_open, CL, CK, line=0.16)
    k.decal(e, EYE_N, (0.025, 0.023), look, eps=0.004, rings=20, segs=56, outline=cut, mirror=True)


def hoopa(src, dst):
    k = Kit(src, coat="fur")
    head(k)
    body(k)
    arms(k)
    big_ring(k)
    marks(k)
    k.build(voxel=0.0036, smooth=4, band=0.004, wband=0.02)
    k.eye_states(lambda state: eyes(k, state))
    k.export(dst)


if __name__ == "__main__":
    hoopa(*args())
