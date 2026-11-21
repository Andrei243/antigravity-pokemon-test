# Diggersby sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/diggersby.py -- <the export's Diggersby.glb> <out.glb>
# Grown from bunnelby.py: the same grey, brown and pink, its nose, buck teeth and ear linings.
#
# From the references: a big fat grey rabbit. Its great ears rise from the head's top as short grey points lined pink,
# and from each a huge thick limb arches up, out and down to hang beside the body, grey with dark brown spots, ending
# in a great brown fist of fur at the belly's height. The head grey above the eyes and brown below them, wide brown
# cheeks with brown whiskers; eyes narrowed in a smug half-closed look under dark brows; a pink nose and two big white
# buck teeth in a wide grin. Small grey arms folded over the top of a great belly: a fat brown band of shaggy fur round
# the waist with a fluffy yellow patch in front, the white paws resting on it. Short grey legs, brown feet.
# The export's joints: body (0, 0, 0.22), head (0, -0.02, 0.34), ears (+-0.04, -0.02, 0.42), arms (+-0.08, -0.03,
# 0.29), legs (+-0.06, 0, 0.1). About 0.5 tall.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, outward, srgb, inside, smoothstep, unit, boxy, tri, lumpy, eye_look,
                       sibling, args)

b = sibling("bunnelby")
GREY, BROWN, DARK, PINK, INK, WHITE = b.GREY, b.BROWN, b.DARK, b.PINK, b.INK, b.WHITE
YELLOW, PAW = "#F2C930", "#F4F2EE"

H = np.array([0.0, -0.026, 0.378])          # the head's centre
HR = np.array([0.072, 0.064, 0.065])


def on_head(d, lift=0.0):
    d = unit(*d)
    return H + d * (1 / math.sqrt(((d / HR) ** 2).sum()) + lift)


# The great ear's line, from the root behind the pointed ear up, out and down to the fist (on +X)
ARC = np.array([(0.036, 0.006, 0.44), (0.07, 0.012, 0.47), (0.12, 0.008, 0.455), (0.165, -0.002, 0.405),
                (0.192, -0.022, 0.335), (0.202, -0.038, 0.27)])
ARR = [0.02, 0.028, 0.038, 0.048, 0.055, 0.06]
FIST = np.array([0.205, -0.048, 0.215])


def diggersby(src, out):
    k = Kit(src, coat="fur")

    # --- the head, wide brown cheeks ------------------------------------------------------------------------------
    k.add("head", ell(H, HR, deform=boxy(2.2)), GREY, "head")
    k.add("cheek", ell(H + (0.04, -0.018, -0.026), (0.042, 0.038, 0.034)), BROWN, "head", mirror=True)
    k.add("muzzle", ell(H + (0, -0.036, -0.03), (0.034, 0.028, 0.026)), BROWN, "head")

    # --- the pointed ears on the head's top, lined pink -------------------------------------------------------------
    k.add("ear", blade([(0.03, -0.012, 0.415), (0.04, -0.013, 0.455), (0.05, -0.01, 0.495)], [0.021, 0.018, 0.004],
                       (0, 1, 0), 0.5), GREY, "ear{s}", mirror=True)
    k.decal((0.041, -0.03, 0.452), unit(0.1, -1, 0), (0.0075, 0.022), PINK, up=unit(0.25, 0, 1), eps=0.002,
            rings=10, segs=40, mirror=True)

    # --- the great ears: an arch of grey out and down to a brown fist with three knuckles ----------------------------
    k.add("bigear", tube(ARC, ARR, seg=24), GREY, "ear{s}", mirror=True)
    k.add("hand", ell(FIST, (0.064, 0.06, 0.062), deform=lumpy(3, 2, 0.1)), BROWN, "ear{s}", mirror=True)
    for i, dx in enumerate((-0.03, 0.0, 0.03)):
        k.add(f"knuckle{i}", ell(FIST + (dx, -0.034, -0.05), (0.026, 0.026, 0.024)), BROWN, "ear{s}", mirror=True)

    def fur_end(P):
        x = np.abs(P[:, 0])
        edge = 0.272 + 0.012 * np.sin(x * 140 + P[:, 1] * 90)
        return inside(P[:, 2] - edge, 0.002) * (x > 0.13)
    k.paint(fur_end, BROWN, on=["bigear", "hand", "knuckle"])
    for j, n, r in ((3, unit(0.62, -0.62, 0.48), 0.019), (4, unit(0.8, -0.58, 0.1), 0.014),
                    (3, unit(0.75, 0.25, 0.55), 0.015)):
        k.decal(ARC[j], n, (r, r * 0.9), DARK, eps=0.002, rings=7, segs=28, mirror=True)

    # --- the body: a grey chest, a fat brown band of shaggy fur round the belly, a fluffy yellow patch in front -------
    k.add("chest", ell((0, -0.008, 0.292), (0.09, 0.074, 0.056)), GREY, "body")
    k.add("belly", ell((0, 0.0, 0.178), (0.1, 0.094, 0.086), deform=lumpy(5, 3, 0.07)), BROWN, "body")
    for i, a in enumerate(np.radians(np.arange(40, 330, 36))):
        k.add(f"bellytuft{i}", ell((0.088 * math.sin(a), -0.085 * math.cos(a), 0.17 + 0.03 * ((i % 2) - 0.5)),
                                   (0.03, 0.03, 0.036)), BROWN, "body")
    k.add("yellow", ell((0, -0.074, 0.172), (0.062, 0.04, 0.074), deform=lumpy(4, 3, 0.12)), YELLOW, "body")
    k.add("hips", ell((0, 0.006, 0.1), (0.085, 0.075, 0.05)), GREY, "body")

    # --- short grey legs, brown feet ----------------------------------------------------------------------------------
    k.add("leg", tube([(0.06, 0.0, 0.1), (0.063, -0.006, 0.05), (0.064, -0.01, 0.026)], [0.035, 0.033, 0.03]), GREY,
          "leg{s}", mirror=True)
    k.add("foot", ell((0.066, -0.036, 0.015), (0.036, 0.058, 0.016)), BROWN, "leg{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 2] - 0.026, 0.0015) * (np.abs(P[:, 0]) > 0.02) * (P[:, 2] < 0.06), BROWN,
            on=["foot", "leg"])

    # --- small grey arms folded over the belly's top, white paws resting on it ----------------------------------------
    k.add("arm", tube([(0.08, -0.03, 0.29), (0.078, -0.078, 0.272), (0.035, -0.108, 0.262)], [0.024, 0.022, 0.02]),
          GREY, "arm{s}", mirror=True)
    k.add("paw", ell((0.024, -0.112, 0.258), (0.026, 0.021, 0.02)), PAW, "arm{s}", mirror=True)

    # --- the face: brown below the eyes, a pink nose, the buck teeth in a wide grin, brown whiskers -------------------
    def lower_face(P):
        d = P - H
        near = np.linalg.norm(d / (HR * 1.35), axis=1) < 1
        edge = -0.012 + 0.004 * tri(d[:, 0] * 55)
        return inside(d[:, 2] - edge, 0.0018) * inside(d[:, 1] - 0.03, 0.004) * near * (P[:, 2] > 0.31)
    k.paint(lower_face, BROWN, on=["head", "cheek", "muzzle"])
    b.nose(k, on_head((0, -1, -0.16)), unit(0, -1, 0.1), (0.0095, 0.0072), colour=PINK)
    CB, CI = srgb(BROWN), srgb(INK)

    def grin(u, v):
        c = np.tile(CB, (len(u), 1))
        c[(np.abs(v - (-0.15 + 0.55 * u * u)) < 0.22) & (np.abs(u) < 0.92)] = CI
        return c
    k.decal(H + (0, -0.06, -0.026), unit(0, -1, -0.15), (0.03, 0.011), grin, eps=0.002, rings=10, segs=48)
    b.teeth(k, (0, -0.088, 0.339), (0.0095, 0.004, 0.012), 0.0102)
    for i, (dy, dz) in enumerate(((0.0, 0.016), (-0.004, 0.0), (0.0, -0.016))):
        k.add(f"whisker{i}", outward(tube([(0.062, -0.04, 0.358), (0.11, -0.048 + dy, 0.36 + dz)], [0.0078, 0.0066],
                                          seg=10)), BROWN, "head", fuse=False, mirror=True)


    k.build(voxel=0.005, band=0.004, wband=0.04)

    # --- the eyes: narrowed and smug, the lids heavy over dark eyes with a glint ------------------------------------
    CG = srgb(GREY)

    def smug(u, v):
        c = np.tile(CG, (len(u), 1))
        top = 0.12 + 0.12 * u - 0.1 * u * u
        low = -0.62 + 0.55 * u * u
        eye = (v < top) & (v > low)
        c[eye] = CI
        c[eye & (((u + 0.12) / 0.19) ** 2 + ((v + 0.12) / 0.17) ** 2 < 1)] = (1, 1, 1)
        return c
    de = unit(0.33, -0.9, 0.2)

    def eyes(state):
        if state == "Fierce":
            def fierce(u, v):
                c = smug(u, v)
                c[v > 0.05 + 0.35 * u] = CG
                return c
            look = fierce
        else:
            look = eye_look(state, smug, CG, CI)
        k.decal(on_head(de), de, (0.022, 0.017), look, eps=0.0022, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    diggersby(*args())
