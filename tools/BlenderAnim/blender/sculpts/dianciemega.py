# Mega Diancie sculpted on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dianciemega.py -- <the export's Diancie-Mega.glb> <out.glb>
#
# Diancie grown (diancie.py's brilliant, colours, head and eyes, carbink.py's ears and on_ell, roggenrola.py's crystals,
# by sibling; the head and body copied here and raised by the joints' difference, 0.245: Mega Diancie floats where
# Diancie stood on its rock). From the references, the masses by size: (1) a great bell of pink crystal under the gown,
# prisms hanging point down round a pale core, the design's largest mass (0.55 of the height tall, as wide as it is tall
# at its top); (2) two long white veils from the shoulders, out over the arms and hanging down at the sides; (3) a wide
# white over-skirt of scallops over a gold band; (4) the crown: the round brilliant grown great on the brow, ringed by
# short pale crystals. The grey head and small pointed ears, large red eyes, a small mouth; two cream crescents on the
# chest facing each other round a small pink diamond; thin grey arms held out a little forward and down. A few small pink
# gems float under the skirt and the veils. No rock (the pictures and the game's sculptor agree).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, lathe, srgb, unit, eye_look, args, hull, outward, axes, sibling

D = sibling("diancie")
C, R = D.C, D.R

GREY, WHITE, CREAM, GOLD = D.GREY, D.WHITE, D.CREAM, "#E8C25C"
PINK, PINK_DK, PINK_LT = D.PINK, D.PINK_DK, D.PINK_LT
CORE = "#E4D2E2"
DZ = 0.245
HEAD_C = D.HEAD_C + np.array([0, 0, DZ])
HEAD_R = D.HEAD_R * 1.12


def gem(c, d, length, r):
    c = np.array(c, float); d = unit(*d)
    a, b = axes(d)
    pts = [c + d * length, c - d * length * 0.55]
    for i in range(6):
        t = 2 * math.pi * i / 6
        pts.append(c + r * (math.cos(t) * a + math.sin(t) * b))
    return hull(pts)


def dianciemega(src, out):
    k = Kit(src, coat="shell")
    glints = R.Glints()

    # --- (1) the bell of pink crystal ------------------------------------------------------------------------------
    k.add("core", lathe((0, 0.005, 0.0), (0, 0, 1), [(0, 0.09), (0.06, 0.16), (0.11, 0.26), (0.135, 0.35),
                                                     (0.13, 0.4), (0, 0.405)], seg=40),
          CORE, "body", fuse=False, surface="shell")
    for i in range(9):
        th = 2 * math.pi * (i + 0.5) / 9
        o = np.array([math.sin(th), -math.cos(th), 0.0])
        base = np.array([0, 0.005, 0.365]) + o * 0.09
        d = o * 0.62 + np.array([0, 0, -1.0])
        ln = 0.27 + 0.05 * (i % 2)
        glints.put(R.crystal(base, d, ln, 0.056, sides=6, tip=0.35, flat=1.15, turn=0, up=o),
                   PINK if i % 2 else PINK_DK, "body")
    for i in range(7):
        th = 2 * math.pi * i / 7
        o = np.array([math.sin(th), -math.cos(th), 0.0])
        base = np.array([0, 0.005, 0.31]) + o * 0.065
        glints.put(R.crystal(base, o * 0.3 + np.array([0, 0, -1.0]), 0.25, 0.046, sides=6, tip=0.4, flat=1.1,
                             up=o), PINK_LT if i % 2 else PINK, "body")
    glints.put(R.crystal((0, 0.005, 0.3), (0, 0, -1), 0.26, 0.045, sides=6, tip=0.4), PINK, "body")

    # --- (3) the gold band and the white over-skirt of scallops -------------------------------------------------------
    k.add("band", lathe((0, 0.0, 0.385), (0, 0, 1), [(0, 0.0), (0.13, 0.0), (0.124, 0.028), (0, 0.028)], seg=40),
          GOLD, "body", fuse=False, surface="metal")
    k.add("skirtcore", ell((0, 0.0, 0.44), (0.11, 0.1, 0.045)), WHITE, "body")
    for i in range(12):
        th = 2 * math.pi * i / 12
        k.add(f"frill{i}", ell((0.13 * math.sin(th), -0.122 * math.cos(th), 0.42 + 0.006 * (i % 2)),
                               (0.056, 0.052, 0.03)), WHITE, "body")
    k.add("torso", ell((0, -0.004, 0.5), (0.04, 0.036, 0.052)), WHITE, "body")

    # thin grey arms out a little forward and down, a small grey hand
    k.add("shoulder", ell((0.036, -0.004, 0.518), (0.022, 0.02, 0.02)), WHITE, "arm{s}", mirror=True)
    k.add("arm", tube([(0.04, -0.008, 0.518), (0.075, -0.035, 0.503), (0.1, -0.06, 0.488)], [0.0125, 0.012, 0.012]),
          GREY, "arm{s}", mirror=True)
    k.add("hand", ell((0.106, -0.066, 0.484), (0.016, 0.014, 0.015)), GREY, "arm{s}", mirror=True)

    # --- the head ---------------------------------------------------------------------------------------------------
    k.add("neck", ell((0, -0.006, 0.541), (0.022, 0.02, 0.02)), GREY, "head")
    k.add("head", ell(HEAD_C, HEAD_R), GREY, "head")
    C.ear(k, "ear", (0.054, 0.004, 0.61), unit(1, 0.15, 0.3), (0, 0.35, 1), 0.05, 0.034, 0.012, GREY, None, 0, "head")

    # --- (4) the crown: the great brilliant on the brow, a ring of pale crystals round it, a pink bonnet behind --------
    gc = HEAD_C + np.array([0, -0.03, 0.068])
    gn = unit(0, -0.78, 0.62)
    k.add("diamond", D.brilliant(gc, gn, 0.062, 0.032), PINK, "head", fuse=False, surface="shell")
    k.add("bonnet", hull([HEAD_C + p for p in
                          [(0.0, 0.05, 0.1), (0.05, 0.035, 0.075), (-0.05, 0.035, 0.075), (0.066, 0.01, 0.04),
                           (-0.066, 0.01, 0.04), (0.06, 0.05, 0.02), (-0.06, 0.05, 0.02), (0.0, 0.07, 0.02),
                           (0.035, -0.02, 0.09), (-0.035, -0.02, 0.09), (0.0, 0.0, 0.112)]]),
          PINK_LT, "head", fuse=False, surface="shell")
    a, b = axes(gn)
    for i in range(10):
        t = 2 * math.pi * i / 10 + math.pi / 2
        o = math.cos(t) * a + math.sin(t) * b
        if o[2] < -0.55:
            continue                                   # none over the face
        glints.put(R.crystal(gc + o * 0.052 - gn * 0.01, o * 1.0 + gn * 0.15, 0.036, 0.016, sides=5, tip=0.45,
                             flat=0.7), PINK_LT if i % 2 else PINK, "head")
    glints.flush(k)

    # --- (2) the veils: broad white ribbons from the shoulders, out over the arms and down at the sides ----------------
    path = np.array([(0.035, 0.02, 0.525), (0.095, 0.03, 0.585), (0.165, 0.045, 0.578), (0.212, 0.06, 0.49),
                     (0.232, 0.07, 0.34), (0.238, 0.075, 0.2), (0.226, 0.07, 0.11)])
    ext = np.vstack([2 * path[0] - path[1], path, 2 * path[-1] - path[-2]])
    dense = []
    for i in range(len(path) - 1):
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        for tt in np.linspace(0, 1, 12, endpoint=False):
            dense.append(0.5 * ((2 * p1) + (-p0 + p2) * tt + (2 * p0 - 5 * p1 + 4 * p2 - p3) * tt * tt
                                + (-p0 + 3 * p1 - 3 * p2 + p3) * tt ** 3))
    dense.append(path[-1])
    dense = np.array(dense)
    tang = np.gradient(dense, axis=0)
    across = np.cross(tang, (0, 1.0, 0)); across /= np.linalg.norm(across, axis=1, keepdims=True)
    q = np.linspace(0, 1, len(dense))

    def veil_at(u, v):
        # the ribbon's width runs across its path in the front's plane (its face to the cameras)
        idx = np.clip(np.round(u * (len(dense) - 1)).astype(int), 0, len(dense) - 1)
        w = (0.014 + 0.026 * np.minimum(q[idx] * 3, 1)) * (v - 0.5) * 2
        return dense[idx] + across[idx] * w[..., None] + np.array([0, 0.006, 0]) * np.sin(v * math.pi)[..., None]
    k.add("veil", outward(sheet(veil_at, len(dense), 5, 0.008)), WHITE, "veil{s}", fuse=False, surface="cloth", mirror=True)

    # --- floating gems under the skirt and the veils -----------------------------------------------------------------
    for i, (x, y, z, s) in enumerate(((0.14, -0.08, 0.06, 0.9), (-0.14, -0.08, 0.06, 0.9),
                                      (0.245, 0.06, 0.07, 0.9), (-0.245, 0.06, 0.07, 0.9))):
        k.add(f"float{i}", gem((x, y, z - 0.01), (0, 0, 1), 0.034 * s, 0.017 * s), PINK_DK, "body", fuse=False,
              surface="shell")

    # the two cream crescents on the chest round a small pink diamond
    for s in (1, -1):
        arc = [(s * (0.016 + 0.022 * math.sin(t)), -0.034 - 0.006 * math.sin(t), 0.508 - 0.028 * math.cos(t) * 0.9)
               for t in np.linspace(0.15, 2.9, 8)]
        k.add("crescent" + ("R" if s > 0 else "L"), outward(tube(arc, [0.006, 0.01, 0.012, 0.013, 0.013, 0.012, 0.01,
                                                                        0.006], seg=14)),
              CREAM, "body", fuse=False, surface="shell")
    k.add("chestgem", hull([(0, -0.044, 0.522), (0.013, -0.042, 0.508), (-0.013, -0.042, 0.508), (0, -0.038, 0.476),
                            (0, -0.03, 0.506), (0, -0.054, 0.508)]), PINK_DK, "body", fuse=False, surface="shell")

    # the mouth: small and open
    mp, mn = C.on_ell(HEAD_C, HEAD_R, 0.0, HEAD_C[2] - 0.034)
    dark, tongue = srgb("#5A1A2C"), srgb("#E6728C")
    def mouth(u, v):
        c = np.tile(dark, (len(u), 1))
        c[(u / 0.6) ** 2 + ((v + 0.55) / 0.5) ** 2 < 1] = tongue
        return c
    k.decal(mp, mn, (0.009, 0.007), mouth, eps=0.002, rings=10, segs=40,
            outline=lambda th: 1.0 if math.sin(th) < 0 else 0.55)

    k.build(voxel=0.0038, smooth=5, band=0.004, wband=0.03)

    # --- the eyes: Diancie's, large and red ---------------------------------------------------------------------------
    cr_, cd, ck, lid = srgb(D.RED), srgb(D.RED_DK), srgb(D.EYE_INK), srgb(GREY)
    def open_eye(u, v):
        c = np.tile(srgb("#F8F2F4"), (len(u), 1))
        c[((u + 0.05) / 0.72) ** 2 + ((v + 0.02) / 0.8) ** 2 < 1] = cr_
        c[((u + 0.08) / 0.34) ** 2 + ((v + 0.02) / 0.42) ** 2 < 1] = cd
        c[((u - 0.12) / 0.2) ** 2 + ((v - 0.34) / 0.2) ** 2 < 1] = (1, 1, 1)
        c[(u * u + v * v > 0.86 ** 2) | ((v > 0.55 - 0.25 * u) & (u * u + v * v > 0.7 ** 2))] = ck
        return c
    p, n = C.on_ell(HEAD_C, HEAD_R, 0.029, HEAD_C[2] - 0.011)

    def eyes(state):
        k.decal(p, n, (0.021, 0.023), eye_look(state, open_eye, lid, ck, line=0.15), eps=0.002, rings=20, segs=56,
                mirror=True)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    dianciemega(*args())
