# Iepuroi sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/iepuroi.py -- <the export's Iepuroi.glb> <out.glb>
#
# Grown from iepurel.py (sibling("iepurel")): the line's tawny brown and sand, its broad ears painted pink-brown
# inside and black at the tips, its dark nose, its eyes in pale rings (here cut by a scowling brow), its fluffy tufts.
# From the picture (10038-Iepuroi.jpg) and its brief (CP-dex-1b-plateau-more.md, 38), the masses largest first: a big
# muscular hare standing upright, broad shoulders and a deep chest over a pinched waist, great haunches fused into the
# hips and reaching nearly to the knee, short thick shins and long feet; thick forearms ending in big furry fists held
# in a boxer's guard (the lead fist, +X, raised before the shoulder; the other guarding the chest under the chin); a
# head carried forward with a pale muzzle, spiky cheeks, heavy brows over narrowed eyes and a wide grin of white
# teeth; two tall black-tipped ears standing up like banners; a pale throat, a pale chest notched in a V at the breast
# bone and a pale belly; a fluffy tail behind.
# Skeleton: body (0, 0, 0.55), arms (+-0.15, -0.02, 0.73), head (0, -0.03, 0.78), ears (+-0.04, -0.03, 0.95),
# legs (+-0.09, 0, 0.44), tail (0, 0.1, 0.46). Height about 1.28 to the ears' tips.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, inside, eye_look, polygon, lumpy, args, sibling

ie = sibling("iepurel")
BROWN, SAND, NOSE, INK = ie.BROWN, ie.SAND, ie.NOSE, ie.INK
BROW = "#8C5C36"
TEETH, LIP = srgb("#F6F2E6"), srgb("#3A2018")
IRIS, PUPIL = srgb("#BBC1A8"), srgb("#1C1C18")

# --- Iepuroi's frame -------------------------------------------------------------------------------------------------
HC = np.array([0.0, -0.075, 0.885])
HR = np.array([0.13, 0.12, 0.12])
MC = np.array([0.0, -0.18, 0.84])
MR = np.array([0.066, 0.058, 0.052])
EYE_D = (0.5, -0.84, 0.2)
EYE_HALF = (0.036, 0.028)
RING_HALF = (0.043, 0.034)
EAR = [(0.042, -0.06, 0.975), (0.056, -0.055, 1.06), (0.074, -0.045, 1.16), (0.088, -0.035, 1.25)]
EAR_R = [0.03, 0.046, 0.043, 0.024]


def iepuroi(src, out):
    k = Kit(src, coat="fur")
    # The trunk: broad deep chest, pinched waist, the hips; the shoulders' round masses
    k.add("chest", ell((0, -0.04, 0.72), (0.16, 0.122, 0.112)), BROWN, "body")
    k.add("belly", ell((0, -0.022, 0.565), (0.124, 0.112, 0.115)), BROWN, "body")
    k.add("hips", ell((0, 0.008, 0.448), (0.15, 0.112, 0.09)), BROWN, "body")
    k.add("shoulder", ell((0.165, -0.02, 0.745), (0.078, 0.074, 0.074)), BROWN, "arm{s}", mirror=True)
    k.add("neck", ell((0, -0.05, 0.8), (0.085, 0.08, 0.07)), BROWN, "head")
    # The head carried forward, the pale muzzle, spiky cheeks, heavy brows
    k.add("head", ell(HC, HR), BROWN, "head")
    k.add("muzzle", ell(MC, MR), SAND, "head")
    for i, (d, ln) in enumerate((((0.97, 0.1, -0.25), 0.045), ((0.88, 0.3, -0.5), 0.04), ((0.9, -0.1, -0.5), 0.034),
                                 ((0.95, 0.2, 0.05), 0.032))):
        p, n = ie.on_ell(HC, HR, d)
        ie.tuft(k, f"cheek{i}", p - n * 0.01, n + np.array([0, 0.5, -0.2]), ln * 1.35, 0.029, BROWN, "head", mirror=True)
    for i, d in enumerate(((0.0, 0.2, 1.0), (0.22, 0.05, 1.0))):
        p, n = ie.on_ell(HC, HR, d)
        ie.tuft(k, f"crown{i}", p - n * 0.008, n + np.array([0, 0.5, 0]), 0.032, 0.02, BROWN, "head", mirror=(i == 1))
    bp, bn = ie.on_ell(HC, HR, (0.42, -0.84, 0.42))
    k.add("brow", ell(bp - bn * 0.012, (0.066, 0.031, 0.028), rot=(0, -30, -28)), BROW, "head", mirror=True)
    # The tall ears, a little apart
    ie.ear(k, "earR", EAR, EAR_R, "earR")
    ie.ear(k, "earL", [(-x, y, z) for x, y, z in EAR], EAR_R, "earL")
    # Arms of thick forearms and big furry fists in a boxer's guard, held at the chest so the whole face stays clear
    # from the front camera: the lead (+X) at the upper chest, its top no higher than the collar bone (0.75), the
    # other at the lower chest. Both inside the shoulders' width and well in front of the chest.
    k.add("uparmR", tube([(0.17, -0.02, 0.73), (0.22, -0.07, 0.64), (0.225, -0.12, 0.595)], [0.06, 0.056, 0.054]),
          BROWN, "armR")
    k.add("forearmR", tube([(0.225, -0.12, 0.595), (0.2, -0.2, 0.635), (0.165, -0.25, 0.665)], [0.056, 0.06, 0.056]),
          BROWN, "armR")
    k.add("fistR", ell((0.15, -0.27, 0.68), (0.07, 0.066, 0.068), deform=lumpy(3, 2, 0.06)), BROWN, "armR")
    k.add("uparmL", tube([(-0.17, -0.02, 0.73), (-0.22, -0.08, 0.63), (-0.21, -0.14, 0.565)], [0.06, 0.056, 0.054]),
          BROWN, "armL")
    k.add("forearmL", tube([(-0.21, -0.14, 0.565), (-0.15, -0.215, 0.585), (-0.09, -0.24, 0.6)], [0.056, 0.06, 0.056]),
          BROWN, "armL")
    k.add("fistL", ell((-0.065, -0.255, 0.6), (0.07, 0.066, 0.066), deform=lumpy(3, 2, 0.06)), BROWN, "armL")
    for s, c, d in ((1, (0.24, -0.11, 0.6), (0.6, 0.5, -0.6)), (-1, (-0.235, -0.13, 0.56), (-0.6, 0.5, -0.6))):
        ie.tuft(k, f"elbow{s}", c, d, 0.034, 0.026, BROWN, "armR" if s > 0 else "armL")
    # The chest's ruff: thick pointed cream tufts down the V of the breast, and two at the collar
    for i, (x, y, z, d) in enumerate(((0.068, -0.148, 0.715, (0.35, -0.5, -0.8)), (0.04, -0.158, 0.672, (0.2, -0.55, -0.8)),
                                      (0.0, -0.16, 0.635, (0, -0.55, -0.85)), (-0.04, -0.158, 0.672, (-0.2, -0.55, -0.8)),
                                      (-0.068, -0.148, 0.715, (-0.35, -0.5, -0.8)), (0.09, -0.12, 0.78, (0.5, -0.5, -0.5)),
                                      (-0.09, -0.12, 0.78, (-0.5, -0.5, -0.5)))):
        ie.tuft(k, f"ruff{i}", (x, y, z), d, 0.05, 0.026, SAND, "body")
    # Great haunches fused into the hips and reaching nearly to the knee, short thick shins sloping back, long feet
    k.add("thigh", ell((0.1, -0.018, 0.372), (0.1, 0.117, 0.15), rot=(16, 0, 0)), BROWN, "leg{s}", mirror=True)
    k.add("shin", tube([(0.108, -0.03, 0.255), (0.112, 0.035, 0.15), (0.114, 0.08, 0.065)], [0.072, 0.06, 0.048]),
          BROWN, "leg{s}", mirror=True)
    k.add("foot", ell((0.116, -0.04, 0.036), (0.056, 0.14, 0.036)), BROWN, "leg{s}", mirror=True)
    for j in range(3):
        k.add(f"toe{j}", ell((0.116 + (j - 1) * 0.03, -0.168, 0.03), (0.022, 0.03, 0.026)), BROWN, "leg{s}",
              mirror=True)
    # The fluffy tail
    TC = np.array([0, 0.165, 0.485])
    k.add("tail", ell(TC, (0.092, 0.088, 0.1), rot=(-30, 0, 0), deform=lumpy(5, 3, 0.24)), BROWN, "tail")
    for i, d in enumerate(((0, 0.6, 0.8), (0.7, 0.5, 0.3), (-0.7, 0.5, 0.3), (0.4, 0.8, -0.3), (-0.4, 0.8, -0.3),
                           (0, 0.9, 0.1))):
        d = unit(*d)
        ie.tuft(k, f"tailtuft{i}", TC + d * 0.07, d, 0.055, 0.03, BROWN, "tail")

    # Paint: the pale throat, chest notched in a V at the breast bone, the belly
    def bib(P):
        z = P[:, 2]
        w = np.interp(z, [0.385, 0.45, 0.56, 0.62, 0.65, 0.7, 0.76, 0.82], [0.0, 0.075, 0.095, 0.06, 0.03, 0.085, 0.07, 0.05])
        notch = inside(np.abs(P[:, 0]) - w, 0.004)
        vcut = 1 - inside(np.abs(P[:, 0]) * 1.4 - (z - 0.62) * 2.2, 0.004) * inside(z - 0.68, 0.004) * inside(0.62 - z, 0.004)
        return notch * inside(P[:, 1] + 0.05, 0.01) * inside(z - 0.83, 0.004) * inside(0.385 - z, 0.004) * vcut
    k.paint(bib, SAND, on=["chest", "belly", "hips", "neck", "muzzle"])
    k.paint(lambda P: inside(P[:, 2] - 0.465, 0.004) * inside(0.1 - P[:, 1], 0.004), SAND, on=["tail"])

    # Rings round the eyes and the nose: what every eye state keeps
    p, n = ie.on_ell(HC, HR, EYE_D)
    RING = [(-1, 0.1), (-0.8, -0.6), (0, -1), (0.8, -0.6), (1, 0.45), (0.6, 0.62), (-0.5, 0.25)]
    k.decal(p, n, RING_HALF, SAND, eps=0.0026, rings=10, segs=48, outline=polygon(RING))
    k.decal(p * np.array([-1, 1, 1.0]), n * np.array([-1, 1, 1.0]), RING_HALF, SAND, eps=0.0026, rings=10, segs=48,
            outline=polygon([(-a, b) for a, b in RING]))
    npt, nn = ie.on_ell(MC, MR, (0, -0.7, 0.72))
    k.decal(npt, nn, (0.022, 0.016), NOSE, eps=0.003, outline=polygon([(-1, 0.75), (1, 0.75), (0, -1)]), rings=6, segs=30)
    # The grin: a wide crescent of white teeth with a dark lip line round it
    GRIN = polygon([(-1, 0.42), (-0.5, 0.2), (0, 0.1), (0.5, 0.2), (1, 0.42), (0.78, -0.25), (0.38, -0.62), (0, -0.72),
                    (-0.38, -0.62), (-0.78, -0.25)])
    def grin(u, v):
        c = np.tile(LIP, (len(u), 1))
        top = 0.1 + 0.32 * u * u
        bot = -0.72 + 0.62 * u * u
        teeth = (v < top - 0.17) & (v > bot + 0.24) & (np.abs(u) < 0.88)
        c[teeth] = TEETH
        c[teeth & (np.abs(v - (top + bot) / 2 + 0.02) < 0.06)] = srgb("#BDB4A2")
        return c
    gp, gn = ie.on_ell(MC, MR, (0, -0.9, -0.44))
    k.decal(gp, gn, (0.078, 0.038), grin, eps=0.003, outline=GRIN, rings=10, segs=56)
    k.build(voxel=0.0085, smooth=4, band=0.01, wband=0.07)
    ie.board(k.ob)

    # The eyes: pale grey-green, narrowed, their top cut by a brow sloping down toward the nose (each its own patch)
    PTS = [(-1, -0.22), (-0.7, -0.65), (0.0, -0.85), (0.72, -0.6), (1, -0.05), (0.84, 0.72), (0.3, 0.62), (-0.4, 0.2),
           (-0.85, -0.1)]
    OUT_R = polygon(PTS)
    OUT_L = polygon([(-a, b) for a, b in PTS])
    def look_cut(drop):
        def look(u, v):
            c = np.tile(IRIS, (len(u), 1))
            c[((u + 0.02) / 0.42) ** 2 + ((v + 0.12) / 0.6) ** 2 < 1] = PUPIL
            c[((u - 0.14) / 0.16) ** 2 + ((v - 0.08) / 0.16) ** 2 < 1] = (1, 1, 1)
            top = np.interp(u, [-1, -0.4, 0.3, 1], [-0.45, -0.05, 0.5, 0.8]) - drop
            c[v > top - 0.26] = INK
            c[v > top] = srgb(BROWN)
            return c
        return look
    open_look = look_cut(0.0)
    fierce = look_cut(0.22)
    def eyes(state):
        look = fierce if state == "Fierce" else eye_look(state, open_look, srgb(SAND), INK, line=0.18)
        k.decal(p, n, EYE_HALF, look, eps=0.0036, outline=OUT_R, rings=20, segs=56)
        m = np.array([-1, 1, 1.0])
        k.decal(p * m, n * m, EYE_HALF, lambda u, v: look(-u, v), eps=0.0036, outline=OUT_L, rings=20, segs=56)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    iepuroi(*args())
