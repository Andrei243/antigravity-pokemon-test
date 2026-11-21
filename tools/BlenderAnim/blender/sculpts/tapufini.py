# Tapu Fini sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tapufini.py -- <the export's Tapu Fini.glb> <out.glb>
#
# The guardians' frame is tapukoko.py's (sibling). From the references, Tapu Fini: a slender dark figure with a white
# arrow on its forehead and teal eyes; long pale blue hair falls from its head down each side to its hips, where it
# ends in frilled fins; a purple piece like a heart at its chest; its arms forward and down, pale blue frills on the
# forearms, black hands with purple claws; its body tapers below into a dark tail with pale blue fins at its end. The
# shell stands behind it as two purple halves opened toward the front like a V, dark inside, black at the rims, white
# and pale blue blocks on the outside; a long black swordfish's bill rises up and out from the top of the +X half and
# a purple fin frilled pale blue from the top of the other.
# The skeleton: root, body (0, 0, 0.472), head (0, -0.03, 0.512), armR/L (+-0.035, -0.03, 0.492).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, slab, leaf, hull, outward, srgb, unit, polygon, args, sibling)

T = sibling("tapukoko")

BODY, PURPLE, LINING, RIM = "#25242E", "#7A50C8", "#3E3066", "#1C1A24"
FIN, WHITE, CLAW, TEAL = "#A8C8F0", "#EEF0F8", "#8C5CDA", "#40C6CE"

FINI = dict(
    body=BODY, coat="scales",
    torso=[((0, -0.035, 0.47), (0.033, 0.028, 0.05)), ((0, -0.031, 0.418), (0.024, 0.022, 0.036))],
    neck=((0, -0.035, 0.505), (0, -0.04, 0.53)), neck_r=0.018,
    head_c=(0, -0.046, 0.562), head_r=(0.048, 0.044, 0.052),
    arm=((0.035, -0.03, 0.492), (0.072, -0.06, 0.456), (0.082, -0.104, 0.414)), arm_r=0.012, hand_r=0.0145,
    eye_c=(0.021, -0.088, 0.556), eye_n=unit(0.42, -0.9, 0.05), eye_half=(0.015, 0.014),
)
FINI_A = math.radians(40)


def on_ell(c, r, d):
    d = np.array(d, float); d /= np.linalg.norm(d)
    p = d / math.sqrt(((d / r) ** 2).sum())
    n = p / r ** 2
    return c + p, n / np.linalg.norm(n)


def fini_dome(s):
    o = (s * math.sin(FINI_A), math.cos(FINI_A), 0)
    f = (s * math.cos(FINI_A), -math.sin(FINI_A), 0)
    return T.Dome((s * 0.075, 0.05, 0.46), o, f, (0, 0, 1), (0.1, 0.105, 0.27))


def shell(k):
    for s, side in ((1, "R"), (-1, "L")):
        D = fini_dome(s)
        T.half_shell(k, f"shell{side}", D, PURPLE, LINING, RIM, "body")
        for j, (t0, t1, e0, e1, col) in enumerate([(0.55, 0.66, 0.2, 1.1, WHITE), (0.72, 0.82, 0.3, 1.25, FIN),
                                                   (0.9, 1.0, 0.2, 1.1, WHITE), (1.08, 1.18, 0.35, 1.2, WHITE),
                                                   (1.26, 1.36, 0.2, 1.0, FIN)]):
            T.dome_patch(k, f"block{side}{j}", D, t0 * math.pi, t1 * math.pi, e0, e1, col, "body", lift=0.0035,
                         nu=20)
    # the bill: a long black point up and out from the +X half's top
    k.add("bill", T.spike((0.07, 0.05, 0.71), (0.23, 0.05, 0.82), 0.032, n=6, flat=0.6), RIM, "body", fuse=False,
          surface="shell")
    # the fin on the other half's top: purple, frilled pale blue below its trailing edge
    ex, ez, o = unit(-1, 0.4, 0), (0, 0, 1), np.array([-0.06, 0.08, 0.705])
    k.add("dorsal", slab([(0, 0), (0.1, 0.0), (0.13, 0.075), (0.065, 0.038), (0.0, 0.036)], o, ex, ez, 0.02),
          PURPLE, "body", fuse=False, surface="shell")
    k.add("dorsalfrill", slab([(0.015, 0.004), (0.105, 0.004), (0.1, -0.028), (0.08, -0.012), (0.062, -0.034),
                               (0.045, -0.012), (0.028, -0.028)], o, ex, ez, 0.016), FIN, "body", fuse=False)


def frill(origin, ex, ez, pts, thick):
    return slab(pts, origin, ex, ez, thick)


def fini_extras(k, F):
    # the tail
    k.add("tail", tube([(0, -0.03, 0.4), (0, -0.022, 0.31), (0.008, -0.004, 0.22), (0.018, 0.012, 0.155)],
                       [0.024, 0.02, 0.015, 0.012]), BODY, "body")
    k.add("chest", ell((0.017, -0.058, 0.486), (0.021, 0.014, 0.021)), PURPLE, "body", mirror=True)
    tip = np.array([0.018, 0.012, 0.15])
    for j, (dx, L) in enumerate([(0.0, 0.07), (0.7, 0.06), (-0.7, 0.06)]):
        k.add(f"tailfin{j}", outward(leaf(tip + np.array([dx * 0.01, 0, 0.012]), unit(dx, 0.1, -1), L, 0.02,
                                          thick=0.01)), FIN, "body", fuse=False)
    # the forehead's white arrow
    p, n = on_ell(np.array(F["head_c"]), np.array(F["head_r"]), (0, -1, 0.55))
    k.decal(p, n, (0.021, 0.02), WHITE, eps=0.0025, rings=10, segs=52,
            outline=polygon([(-1, 1), (1, 1), (0.28, 0.15), (0.14, -1), (-0.14, -1), (-0.28, 0.15)]))
    S, E, H = (np.array(p_, float) for p_ in F["arm"])
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        # the hair, flat from front to back, falling to the hips
        lock = np.array([(0.03, -0.034, 0.572), (0.052, -0.038, 0.54), (0.058, -0.03, 0.48), (0.064, -0.022, 0.42),
                         (0.07, -0.018, 0.375)]) * m
        k.add(f"hair{side}", outward(blade(lock, [0.022, 0.024, 0.022, 0.019, 0.015], (0, 1, 0), 0.45)), FIN, "head",
              fuse=False, surface="hair")
        k.add(f"hipfin{side}", frill((0.062 * s, -0.018, 0.385), (s, 0, 0), (0, 0, 1),
                                     [(0.0, 0.02), (0.05, 0.01), (0.088, -0.012), (0.06, -0.02), (0.078, -0.048),
                                      (0.045, -0.04), (0.05, -0.074), (0.02, -0.052), (0.0, -0.03)], 0.02),
              FIN, "body", fuse=False)
        mid = ((S + E) * 0.25 + (E + H) * 0.25) * m
        k.add(f"armfin{side}", frill(mid, (s, 0, 0), (0, 0, 1),
                                     [(0, 0.004), (0.034, -0.008), (0.046, -0.036), (0.026, -0.03), (0.02, -0.052),
                                      (0, -0.026)], 0.016), FIN, f"arm{side}", fuse=False)
        h = H * m
        for j, d in enumerate([(0.3, -0.6, -0.8), (0.0, -0.75, -0.75), (-0.3, -0.6, -0.8)]):
            dd = np.array(d) * np.array([s, 1, 1])
            k.add(f"claw{side}{j}", T.spike(h + dd * 0.01, h + unit(*dd) * 0.032 + np.array([0.004 * s, 0, 0]),
                                            0.0075, n=5), CLAW, f"arm{side}", fuse=False, surface="shell")
    shell(k)


def fini_eyes(k, F):
    IRIS, PUP = srgb(TEAL), srgb("#102428")

    def open_eye(u, v):
        c = np.tile(srgb(BODY), (len(u), 1))
        r = (u / 0.96) ** 2 + (v / 0.9) ** 2
        c[r < 1] = srgb(FIN)
        c[r < 0.7] = IRIS
        c[((u + 0.03) / 0.32) ** 2 + ((v + 0.05) / 0.42) ** 2 < 1] = PUP
        c[((u + 0.24) / 0.14) ** 2 + ((v - 0.22) / 0.14) ** 2 < 1] = (1, 1, 1)
        c[v > 0.3 + 0.45 * u] = srgb(BODY)
        return c
    return T.eye_lay(k, F, open_eye, BODY, FIN)


if __name__ == "__main__":
    src, out = args()
    T.guardian(src, out, FINI, fini_extras, fini_eyes, voxel=0.004)
