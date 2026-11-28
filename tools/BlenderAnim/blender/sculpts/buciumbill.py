# Buciumbill sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/buciumbill.py -- <the export's Buciumbill.glb> <out.glb>
#
# The Carpathian Water starter's final stage (No. 10009, Water/Sound), Pelireed grown (its body, neck, folded wings,
# tail, pouch, feet and eyes imported from pelireed.py, which has them from pelicub.py), from its picture
# (docs/art/reference/gemini/carpathia/10009-Buciumbill.jpg) and its brief (CP-dex-1-plateau.md, 9): a great white
# Dalmatian pelican. Its beak has become a bucium, the shepherd's long wooden alphorn: a straight wooden tube out of
# the face, forward and down, bound twice with bands of woven bark and flaring to a wide bell with a dark bore. Under
# it the pouch is a drum of blue skin striped in cream; a white face with a stern brown eye under a heavy lid; a
# curly mop of a crest, grey with dark green, over the crown and down the nape; a grey-blue mantle over the folded
# wings, their long flight feathers deep teal running back over a grey tail; a white breast and belly, short orange
# legs on great orange webbed feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, ring_band, srgb, inside, smoothstep, unit, args, sibling, lumpy

r_ = sibling("pelireed")
c_ = r_.c_


class Cfg(r_.Cfg):
    g = 1.3                                     # the skeleton's scale against Pelireed's
    WHITE, SHADE = "#F2F6F6", "#C3D0DA"
    POUCH, STRIPE = "#4E85C8", "#EEDFB4"        # the drum: blue skin, cream stripes
    FOOT = "#F2A04C"
    WINGTIP, MANTLE = "#2E7A6C", "#8E9DB0"
    TAIL, TAIL2 = "#8E9DB0", "#6F7E92"
    IRIS, PUPIL = "#5A3424", "#1A120E"
    VOXEL = 0.0085
    HC, HR = (0.0, -0.08, 0.86), (0.122, 0.13, 0.112)
    NECK = [(0, -0.1, 0.4), (0, -0.145, 0.54), (0, -0.11, 0.67), (0, -0.085, 0.78)]
    NECKR = [0.115, 0.092, 0.082, 0.086]
    raised = False
    EYEDIR = unit(0.62, -0.7, 0.32)
    EYE = (0.028, 0.026)
    LID = (0.2, 0.32)                           # the heavy lid sloping down toward the horn: a stern look


C = Cfg
WOOD, WOOD2, BARK, BORE, ROOT = "#9A7550", "#B08A60", "#6E5034", "#3A2818", "#C7A676"
CURL, CURL2, MOSS = "#8C99A5", "#A9B3BC", "#3F6A50"


def mantle(k):
    """The grey-blue mantle over the back and the wings' tops; the breast and belly stay white."""
    k.paint(lambda P: inside(0.06 - P[:, 1] + 0.35 * (0.5 - P[:, 2]), 0.02) * inside(0.2 - P[:, 2], 0.02) * inside(P[:, 2] - 0.62, 0.02),
            C.MANTLE, on=["body", "wing"])


def crest(k):
    """A curly mop over the crown and down the nape: fused round curls of grey with dark green among them."""
    hc, hr = np.array(C.HC), np.array(C.HR)
    rs = np.random.RandomState(9)
    n = 0
    for ring, (el, cnt, rr) in enumerate(((1.25, 1, 0.07), (0.95, 6, 0.058), (0.6, 9, 0.052), (0.2, 7, 0.048))):
        for j in range(cnt):
            az = 2 * math.pi * (j + 0.5 * ring) / cnt
            if ring >= 2 and math.cos(az) < -0.2:      # leave the face and the brow free
                continue
            d = np.array([math.sin(az) * math.cos(el) * 1.05, math.cos(az) * math.cos(el) + 0.25, math.sin(el)])
            if ring == 0:
                d = np.array([0, 0.25, 1.0])
            p = hc + d / np.linalg.norm(d) * np.array([hr[0] * 1.05, hr[1], hr[2]]) + rs.uniform(-0.01, 0.01, 3)
            col = MOSS if (n % 3 == 1) else (CURL2 if n % 3 == 2 else CURL)
            k.add(f"curl{n}", ell(p, (rr, rr, rr * 0.9), deform=lumpy(3, 2, 0.12)), col, "head")
            n += 1
    # curls spilling down the nape, and a few locks curling out at the edges
    for j, (x, dy, dz) in enumerate(((0.06, 0.12, -0.02), (-0.06, 0.12, -0.02), (0.0, 0.14, -0.08), (0.05, 0.1, -0.12),
                                     (-0.05, 0.1, -0.12))):
        p = hc + np.array([x, dy, dz])
        k.add(f"curl{n}", ell(p, (0.05, 0.045, 0.05), deform=lumpy(3, 2, 0.12)), MOSS if j % 2 else CURL, "head")
        n += 1
    for j, a in enumerate((-0.45, 0.45)):
        b = hc + np.array([math.sin(a) * 0.1, 0.02 + 0.04 * abs(a), hr[2] * 0.85])
        o = np.array([math.sin(a), 0.4, 0.5])
        pts = [b, b + o * 0.06, b + o * 0.09 + np.array([0, 0.02, -0.03]), b + o * 0.075 + np.array([0, 0.03, -0.055])]
        k.add(f"lock{j}", tube(pts, [0.024, 0.021, 0.019, 0.017]), CURL if j % 2 else MOSS, "head")


def horn(k, root, d, L):
    """
    The bucium: a short stretch of the old beak at the face, then a straight wooden tube widening slowly, bound twice
    with woven bark, flaring to a bell with a lip and a dark bore. Loose pieces, each its own colour.
    """
    d = unit(*d)
    k.add("hornroot", lathe(root, d, [(0, -0.02), (0.058, -0.02), (0.06, 0.04), (0.058, 0.1), (0, 0.1)]), ROOT,
          "head", fuse=False, surface="shell")
    prof = [(0, 0.08), (0.056, 0.08), (0.06, 0.3 * L), (0.067, 0.6 * L), (0.076, 0.78 * L), (0.092, 0.88 * L),
            (0.118, 0.95 * L), (0.14, L), (0.127, L + 0.009), (0.108, 0.985 * L), (0.08, 0.93 * L), (0.0, 0.9 * L)]
    k.add("horn", lathe(root, d, prof, seg=48), WOOD, "head", fuse=False, surface="shell")
    # the bore's floor: dark, just inside the rim, so the bell reads open
    k.add("bore", lathe(root, d, [(0, 0.9 * L + 0.006), (0.082, 0.93 * L), (0.107, 0.98 * L), (0.0, 0.975 * L)], seg=48),
          BORE, "head", fuse=False, surface="shell")
    # two bindings of woven bark, a darker and a lighter band each
    for t, col in ((0.36, BARK), (0.44, WOOD2), (0.6, BARK), (0.68, WOOD2)):
        r = 0.06 + (0.067 - 0.06) * (t - 0.3) / 0.3 + 0.006
        k.add(f"binding{int(t * 100)}", ring_band(root + d * t * L, d, r, 0.05 * L if col == BARK else 0.025 * L,
                                                   thick=0.012), col, "head", fuse=False, surface="shell")


def eyes(k):
    E = c_.on_ell(C.HC, C.HR, C.EYEDIR)
    look = c_.eye_round(C.IRIS, C.PUPIL, pupil_r=0.48, lid=C.WHITE, lid_at=C.LID, glint=(-0.32, 0.02, 0.2))
    return c_.eye_layer(k, E, C.EYEDIR, C.EYE, look, C.WHITE)


def buciumbill(src, out_path):
    k = Kit(src, coat="fur")
    r_.body(k, C)
    r_.shade(k, C)
    r_.folded_wing(k, C, -1, "L")
    r_.folded_wing(k, C, 1, "R")
    mantle(k)
    r_.tail(k, C)
    crest(k)
    hc, hr = np.array(C.HC), np.array(C.HR)
    root = hc + np.array([0, -hr[1] * 0.72, -0.03])
    d = (0, -1, -0.72)
    L = 0.72
    horn(k, root, d, L)
    du = unit(*d)
    r_.pouch(k, C, root + np.array([0, 0.1, -0.11]), root + du * 0.42 + np.array([0, 0, -0.085]), 0.36, 0.11, stripes=4, slant=0.9)
    r_.legs(k, C)
    k.build(voxel=C.VOXEL, band=0.006)
    c_.board(k)
    k.eye_states(eyes(k))
    k.export(out_path)


if __name__ == "__main__":
    buciumbill(*args())
