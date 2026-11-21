# Dragalge sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dragalge.py -- <the export's Dragalge.glb> <out.glb>
#
# Skrelp grown (skrelp.py's frame, by sibling: the lilac body split brown down its back, the darker lines across its
# front, the leaves as thick plates, the red eye cut by the brown over it). From the references: a leafy sea dragon,
# hovering, its lilac body an S, the chest bulging forward and the tail curling down under it to a lobed brown leaf of a
# fin; the back brown, set with a few hooked brown spines; the head carried forward, brown over its top, a long brown
# snout tapering to a point, a small mean red eye under the brown; on top of the head a crest of great red leaves fanned
# out, darker veins down them, with two narrow olive leaves standing up among them (the largest mass after the body);
# a brown twig runs back from the head; two thin brown arms reach forward and down to lobed brown leaves; a long lobed
# brown leaf of a fin trails back from the hip. (The pictures' Dragalge is lunging forward; the skeleton stands it as an
# upright S with the head forward, and the trailing fins are shorter than the picture's so the model is not drawn small.)
# Bones: body (0, 0, 0.375), arm{s} (+-0.03, -0.06, 0.435), head (0, -0.12, 0.515).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, slab, hull, sibling

s = sibling("skrelp")
LILAC, LINE, BROWN = "#CE9AD4", "#9A6A9E", "#8E5E3C"
RED, VEIN, OLIVE = "#D8303C", "#962032", "#8E9A42"
EYE_RED, EYE_DEEP = "#E02A34", "#8A1424"

HEAD_C, HEAD_R = np.array([0.0, -0.13, 0.53]), np.array([0.058, 0.066, 0.055])
SNOUT = [(0.0, -0.175, 0.525), (0.0, -0.23, 0.514), (0.0, -0.285, 0.5), (0.0, -0.315, 0.492)]
SNOUT_R = [0.032, 0.022, 0.015, 0.009]
SPINE = [(0.0, -0.1, 0.5), (0.0, -0.08, 0.445), (0.0, -0.068, 0.385), (0.0, -0.025, 0.322), (0.0, 0.035, 0.278),
         (0.0, 0.062, 0.23), (0.0, 0.045, 0.18), (0.0, 0.008, 0.145), (0.0, -0.03, 0.125)]
SPINE_R = [0.046, 0.056, 0.064, 0.063, 0.056, 0.046, 0.037, 0.03, 0.024]


def lobed(L, W, lobes=4, depth=0.28, n=48, point=1.0):
    """A leaf with a wavy, lobed edge from (0, 0) to (L, 0)."""
    pts = []
    for i in range(n + 1):
        t = i / n
        w = W * math.sin(math.pi * t ** 0.75) ** point * (1 - depth * abs(math.sin(math.pi * lobes * t)))
        pts.append((L * t, w))
    for i in range(n - 1, 0, -1):
        t = i / n
        w = W * math.sin(math.pi * t ** 0.75) ** point * (1 - depth * abs(math.sin(math.pi * lobes * t + 0.8)))
        pts.append((L * t, -w))
    return np.array(pts)


def plate(k, name, root, d, across, outline, thick, color, bone, mirror=False):
    """A leaf plate along d, its width along `across` (made square to d)."""
    d = unit(*d)
    ez = np.array(across, float); ez = ez - d * (ez @ d); ez /= np.linalg.norm(ez)
    k.add(name, slab(outline, np.array(root, float), d, ez, thick), color, bone, fuse=False, mirror=mirror,
          surface="leaf")


def head(k):
    k.add("head", ell(HEAD_C, HEAD_R), LILAC, "head")
    k.add("jaw", ell(HEAD_C + (0, -0.02, -0.022), HEAD_R * (0.75, 0.7, 0.6)), LILAC, "head")
    s.cheek(k, HEAD_C, HEAD_R, EYE_N, LILAC)
    k.add("snout", tube(SNOUT, SNOUT_R), BROWN, "head")
    # brown over the top of the head and down the snout's root, the eye under its edge
    def mask(P):
        d = (((P - HEAD_C) / (HEAD_R * 1.5)) ** 2).sum(1)
        dy = P[:, 1] - HEAD_C[1]
        line = HEAD_C[2] + 0.012 + 0.18 * np.minimum(dy, 0) - 0.9 * np.maximum(dy, 0)
        return inside(d - 1, 0.01) * inside(line - P[:, 2], 0.003)
    k.paint(mask, BROWN)


def crest(k):
    top = HEAD_C + (0, 0.005, HEAD_R[2] * 0.85)
    # the red leaves fanned out: one forward, one back, a pair out to the sides, each its face to the cameras
    for name, root, d, ac, L, W, mir in (
            ("leaffront", top + (0, -0.02, 0), (0, -0.62, 0.78), (1, 0, 0), 0.17, 0.052, False),
            ("leafback", top + (0, 0.02, 0), (0, 0.62, 0.78), (1, 0, 0), 0.19, 0.056, False),
            ("leafside", top + (0.02, 0, 0), (0.78, 0.05, 0.62), (0, 0.3, 1), 0.18, 0.054, True)):
        plate(k, name, root, d, ac, lobed(L, W, lobes=2, depth=0.18), 0.022, RED, "head", mir)
        dd = unit(*d)
        vein = [(0.01, 0.006), (L * 0.85, 0.002), (L * 0.85, -0.002), (0.01, -0.006)]
        plate(k, name + "vein", np.array(root) + dd * 0.0, d, ac, vein, 0.03, VEIN, "head", mir)
    for i, sx in enumerate((1, -1)):
        plate(k, f"olive{i}", top + (0.014 * sx, 0.012, 0), (0.22 * sx, 0.18, 0.96), (1, 0, 0),
              s.leaf_outline(0.19, 0.016, point=1.2), 0.02, OLIVE, "head")


def twig(k):
    a = HEAD_C + (0.0, 0.04, 0.045)
    pts = [a, a + (0.03, 0.07, 0.015), a + (0.075, 0.13, 0.02), a + (0.115, 0.19, 0.03)]
    k.add("twig", tube(pts, [0.014, 0.012, 0.011, 0.01], seg=12, per=6), BROWN, "head")
    b = a + (0.05, 0.1, 0.017)
    k.add("twigb", tube([b, b + (0.02, 0.03, 0.04), b + (0.03, 0.05, 0.065)], [0.011, 0.01, 0.0095], seg=12, per=6),
          BROWN, "head")


def spike(root, tip, w):
    root, tip = np.array(root, float), np.array(tip, float)
    d = unit(*(tip - root))
    a = np.cross(d, (1.0, 0, 0)); a /= np.linalg.norm(a)
    return hull([root + a * w, root - a * w, root + (w * 0.5, 0, 0), root - (w * 0.5, 0, 0), root - d * w * 0.5,
                 tip])


def back_spikes(k):
    for i, (z, up) in enumerate(((0.44, 0.06), (0.36, 0.06), (0.29, 0.055), (0.2, 0.045))):
        y = s.spine_y(z, SPINE) + np.interp(z, np.array(SPINE)[::-1, 2], np.array(SPINE_R)[::-1]) * 0.75
        root = (0.0, y, z)
        tip = (0.0, y + up * 0.9, z + up * 0.75)
        k.add(f"bspike{i}", spike(root, tip, 0.02), BROWN, "body", fuse=False, surface="scales")


def arms(k):
    sh = np.array([0.03, -0.062, 0.435])
    elbow, hand = np.array([0.05, -0.125, 0.41]), np.array([0.052, -0.18, 0.375])
    k.add("arm", tube([sh, elbow, hand], [0.017, 0.014, 0.013], seg=12, per=6), BROWN, "arm{s}", mirror=True)
    plate(k, "handleaf", hand, (0.18, -0.85, -0.5), (1, 0.1, 0.3), lobed(0.15, 0.05, lobes=3), 0.022, BROWN,
          "arm{s}", mirror=True)


def fins(k):
    # the long lobed leaf trailing back from the hip, back and out to +X
    plate(k, "trail", (0.02, 0.07, 0.3), (0.52, 0.82, -0.18), (0, 0, 1), lobed(0.27, 0.06, lobes=4), 0.024,
          BROWN, "body")
    # smaller lobed leaves off the tail
    plate(k, "tailleaf", (-0.02, 0.06, 0.2), (-0.55, 0.78, -0.2), (0, 0, 1), lobed(0.12, 0.034, lobes=2), 0.022,
          BROWN, "body")
    # the fan of a leaf at the tail's end, forward, tilted up for the cameras
    end = np.array(SPINE[-1])
    plate(k, "tailfan", end + (0, -0.01, -0.01), (0.25, -0.9, -0.12), (1, 0, 0.5), lobed(0.15, 0.06, lobes=3,
          depth=0.35), 0.024, BROWN, "body")


# ---- the eyes: small and red under the brown, mean ----------------------------------------------------------------
EYE_N = unit(0.6, -0.78, 0.12)
CL, CK = srgb(LILAC), srgb(s.INK)
TOP = lambda u: 0.34 + 0.4 * u          # +u runs toward the back: the brown slanting down toward the snout


def dragalge_eye(u, v):
    c = np.tile(CK, (len(u), 1))
    rr = u * u + v * v
    c[rr < 0.74] = srgb(EYE_DEEP)
    c[rr < 0.48] = srgb(EYE_RED)
    c[((u + 0.12) / 0.2) ** 2 + ((v + 0.08) / 0.3) ** 2 < 1] = srgb(s.PUPIL)
    return c


def dragalge(src, out):
    k = Kit(src, coat="scales")
    head(k)
    crest(k)
    twig(k)
    k.add("neck", tube(SPINE, SPINE_R, seg=18, per=6), LILAC, "body")
    s.back_brown(k, z0=0.15, z1=0.49, low=0.16, color=BROWN, spine=SPINE, off=0.004)
    s.belly_lines(k, rows=(0.27, 0.31, 0.35, 0.39, 0.43), half=(0.04, 0.005), color=LINE, spine=SPINE,
                  radii=SPINE_R)
    back_spikes(k)
    arms(k)
    fins(k)
    k.build(voxel=0.0062, smooth=3, band=0.006, wband=0.05)
    k.eye_states(s.eyes(k, look=dragalge_eye, centre=HEAD_C + EYE_N * HEAD_R * 0.96 + (0, 0, 0.004), n=EYE_N,
                        half=(0.024, 0.022), top=TOP, lid=CL, ink=CK))
    k.export(out)


if __name__ == "__main__":
    dragalge(*args())
