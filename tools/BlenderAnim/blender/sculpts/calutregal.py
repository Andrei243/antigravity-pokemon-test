# Calutregal sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown
# from calutmar.py (its coil, its fans of fin with spines, its belly's grooves and its eye):
#   blender --background --factory-startup --python sculpts/calutregal.py -- <the export's Calutregal.glb> <out.glb>
#
# Carpathia No. 10132, the Sea Steed Pokemon (plan 46, CP-dex-3, "132. Calutregal: the royal seahorse"); no sculptor
# in the game, so its export gives only a fish's skeleton, its clips and its size. From the picture: a large seahorse
# in pearly cream with coral pink swirls, standing upright; its head a proud horse's, with a long muzzle and a nostril,
# two pointed ears, and between them a crown of a coral scallop shell, two little spiral shells and a row of pearls;
# a calm almond eye, blue, with a lash at its outer corner; a coral blaze down its face; from the back of its head a
# mane of pink fins with mint ribs and edges (the brief's "translucent fins"), and long pink locks with mint tips
# streaming down from under its jaw beside its chest; a belly of pale plates crossed by lines; a great pink fin like a
# wing on its back with mint ribs, a smaller one under it; its long tail curled forward under it and wrapped with
# strings of pearls.
#
# As Calutmar's: every part on `body` (an upright seahorse on a level fish's skeleton: the head joint lies 0.7 under
# the head, the tail swings 46 degrees), built to the export's longest side as its height, hovering from its lowest.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, args, outward, slab, polygon, hull, lathe, sibling

c = sibling("calutmar")

PEARL, BELLY, GROOVE = "#F6EDE6", "#FBF5F0", "#D9BFB2"
CORAL, PINK, MINT = "#EE7F78", "#F4A8A0", "#86D8C4"
SHELL, SHELL_DK, BEAD = "#F0866E", "#D96852", "#F4F0F4"
NOSTRIL, IRIS, INK = "#7A4A48", "#4E9CC8", "#3A2026"

S_, Z0, PX0, PY0 = 0.00127, 0.09, 500, 925


def at(px, py, x=0.0):
    return np.array([x, (px - PX0) * S_, Z0 + (PY0 - py) * S_])


HEAD_C, HEAD_R = at(458, 240), np.array([0.092, 0.088, 0.082])
EYE_N = unit(0.76, -0.6, 0.1)
EYE_HALF = (0.034, 0.031)
EYE_TOP = lambda u: 0.5 - 0.22 * u * u + 0.35 * np.clip(u - 0.45, 0, None)
LASH = lambda u, v: (u > 0.35) & (v > EYE_TOP(u) - 0.42) & (u * u + v * v < 0.95)


def head(k):
    k.add("head", ell(HEAD_C, HEAD_R), PEARL, "body")
    k.add("jowl", ell(at(470, 285), (0.074, 0.072, 0.062)), PEARL, "body")
    k.add("cheek", ell(HEAD_C + EYE_N * HEAD_R * 0.6, (0.046, 0.042, 0.046),
                       rot=(0, 0, -math.degrees(math.atan2(EYE_N[0], -EYE_N[1])))), PEARL, "body", mirror=True)
    # the horse's muzzle, long and forward and down, with a soft round nose
    k.add("muzzle", tube([at(445, 262), at(400, 305), at(362, 340)], [0.056, 0.045, 0.04]), PEARL, "body")
    nose = at(350, 350)
    k.add("nose", ell(nose, (0.044, 0.04, 0.036)), PEARL, "body")
    k.paint(lambda P: inside(np.hypot((P[:, 1] - nose[1]) / 1.0, (P[:, 2] - nose[2]) / 1.2) - 0.05, 0.006)
            * inside(P[:, 1] - nose[1] + 0.005, 0.006), "#F6DCD6", on=["nose", "muzzle"])
    for s in (1, -1):
        k.decal(nose + (s * 0.024, -0.026, 0.006), unit(s * 0.5, -1, 0.15), (0.009, 0.007), NOSTRIL, eps=0.0025,
                rings=6, segs=28)
    # a coral blaze from the brow down the bridge of the muzzle
    def blaze(P):
        t = np.clip(((P[:, 1] - at(462, 205)[1]) * -1 + (at(462, 205)[2] - P[:, 2])) / 0.16, 0, 1)
        y0 = at(462, 205)[1] + (at(392, 300)[1] - at(462, 205)[1]) * t
        z0 = at(462, 205)[2] + (at(392, 300)[2] - at(462, 205)[2]) * t
        d = np.hypot(P[:, 1] - y0, P[:, 2] - z0)
        return inside(np.abs(P[:, 0]) - (0.014 + 0.006 * (1 - t)), 0.003) * inside(d - 0.03, 0.004) * (P[:, 1] < -0.02)
    k.paint(blaze, CORAL, on=["head", "muzzle"])


def ears(k):
    for i, s in enumerate((1, -1)):
        root = at(470, 190, s * 0.044)
        tip = root + np.array([s * 0.03, 0.012, 0.085])
        side = np.array([s * 1.0, 0, 0])
        pts = [root + (0, -0.026, 0), root + (0, 0.022, 0), root + side * 0.02, root - side * 0.016,
               root + (tip - root) * 0.5 + (0, -0.018, 0) + side * 0.012, root + (tip - root) * 0.5 + (0, 0.016, 0),
               tip]
        k.add(f"ear{'RL'[i]}", hull(pts), PEARL, "body", fuse=False, surface="skin")
        # the pink inside, a thinner hull just in front
        inn = [root + (0, -0.03, 0.01) + side * 0.004, root + (tip - root) * 0.55 + (0, -0.02, 0) + side * 0.008,
               tip + (0, -0.006, -0.01), root + (tip - root) * 0.5 + (0, -0.012, 0) - side * 0.006,
               root + (0, -0.018, 0.012)]
        k.add(f"earin{'RL'[i]}", hull(inn), PINK, "body", fuse=False, surface="skin")


def crown(k):
    """A coral scallop shell on the brow between the ears, a spiral shell each side, a row of pearls before them."""
    o = at(452, 178)
    ex, ez = unit(0.0, 0.25, 1.0), unit(1.0, 0, 0)        # a fan standing up, its face forward
    pts = [(0.0, -0.016), (0.0, 0.016)]
    out = [(-0.004, -0.01)]
    for i in range(13):
        a = math.radians(-70 + 140 * i / 12)
        r = 0.056 if i % 2 == 0 else 0.05
        out.append((0.008 + r * math.cos(a), r * math.sin(a)))
    out.append((-0.004, 0.01))
    k.add("scallop", slab(out, o, ex, ez, 0.026), SHELL, "body", fuse=False, surface="shell")
    n = np.cross(ex, ez)
    for i in range(5):
        a = math.radians(-56 + 112 * i / 4)
        d = ex * math.cos(a) + ez * math.sin(a)
        p0, p1 = o + d * 0.012 - n * 0.013, o + d * 0.05 - n * 0.013
        k.add(f"scalrib{i}", outward(tube([p0, (p0 + p1) / 2, p1], [0.008, 0.008, 0.007], seg=8, per=3)), SHELL_DK,
              "body", fuse=False, surface="shell")
    for s in (1, -1):
        b = at(470, 192, s * 0.032) + np.array([0, 0, 0.004])
        d = unit(s * 0.5, 0.2, 1.0)
        prof = [(0.0, -0.002), (0.02, 0.0), (0.022, 0.008), (0.016, 0.018), (0.017, 0.024), (0.011, 0.034),
                (0.011, 0.04), (0.005, 0.05), (0.0, 0.056)]
        k.add("conch" + ("R" if s > 0 else "L"), lathe(b, d, prof, seg=20), "#F4C2A8", "body", fuse=False,
              surface="shell")
    for i in range(7):
        t = (i - 3) / 3.0
        p = at(452 + 20 * abs(t), 196 + 6 * t * t, 0.058 * t) + np.array([0, -0.01, 0])
        k.add(f"pearl{i}", ell(p, (0.017, 0.017, 0.017), sub=3), BEAD, "body", fuse=False, surface="shell")


def body(k):
    k.add("neck", tube([at(505, 270), at(505, 330), at(490, 400), at(470, 470)], [0.07, 0.078, 0.088, 0.1]),
          PEARL, "body")
    k.add("belly", ell(at(462, 545), (0.12, 0.112, 0.17)), PEARL, "body")
    k.add("back", tube([at(535, 300), at(548, 420), at(556, 540), at(550, 650)], [0.05, 0.058, 0.06, 0.058]),
          PEARL, "body")
    k.add("lower", tube([at(468, 620), at(500, 690), at(545, 745), at(592, 780)], [0.106, 0.092, 0.078, 0.066]),
          PEARL, "body")
    P, R = c.coil(k, centre=at(507, 820), a0=24.0, turns=1.38, r0=0.15, r1=0.03, t0=0.058, t1=0.02,
                  turn=c.BACK_TURN, color=PEARL)
    k.paint(lambda Q: inside(Q[:, 1] - 0.005, 0.004) * inside(0.36 - Q[:, 2], 0.005) * inside(Q[:, 2] - 0.8, 0.005),
            BELLY, on=["belly", "neck", "lower"])
    rows = []
    for py in (410, 455, 500, 545, 590, 635, 680):
        z = at(0, py)[2]
        w = 0.082 * (1 - 0.5 * ((z - 0.56) / 0.26) ** 2)
        rows.append((z, max(w, 0.045)))
    c.grooves(k, rows, -0.1, color=GROOVE)
    # coral swirls along the sides and the back, carried round both flanks
    def swirls(Q):
        # long flame-like strokes slanting down and back along each flank
        t = Q[:, 2] * 7.0 - Q[:, 1] * 5.0 + 0.35 * np.sin(Q[:, 1] * 40.0 + Q[:, 2] * 9.0)
        f = t - np.floor(t)
        stroke = inside(np.abs(f - 0.5) - 0.09, 0.02)
        return (stroke * inside(0.3 - Q[:, 2], 0.01) * inside(Q[:, 2] - 0.86, 0.01)
                * inside(0.055 - np.abs(Q[:, 0]), 0.01) * inside(-0.02 - Q[:, 1], 0.01))
    k.paint(swirls, CORAL, on=["belly", "neck", "back", "lower", "tail"])
    # stripes down the back's ridge, seen from behind
    for py in (340, 430, 520, 610):
        k.decal(at(552, py) + (0, 0.02, 0), (0, 1.0, 0), (0.042, 0.009), CORAL, eps=0.0035, rings=4, segs=36)
    for py in (385, 475, 565, 655):
        k.decal(at(550, py) + (0, 0.02, 0), (0, 1.0, 0), (0.05, 0.005), GROOVE, eps=0.0035, rings=4, segs=36)
    return P, R


def pearl_strings(k, P, R):
    """Strings of pearls wrapped round the tail: rings of beads at three places of the coil, and a loop between."""
    P = np.array(P)
    n = len(P)
    beads = 0
    for f in (0.18, 0.34, 0.5, 0.66):
        i = int(f * (n - 1))
        p, d = P[i], P[min(i + 1, n - 1)] - P[max(i - 1, 0)]
        d /= np.linalg.norm(d)
        a = np.cross(d, (1.0, 0, 0)); a /= np.linalg.norm(a)
        b = np.cross(d, a)
        r = R[i] + 0.012
        m = max(8, int(2 * math.pi * r / 0.036))
        for j in range(m):
            t = 2 * math.pi * j / m
            q = p + r * (math.cos(t) * a + math.sin(t) * b) + d * 0.012 * math.sin(t)
            k.add(f"bead{beads}", ell(q, (0.016, 0.016, 0.016), sub=2), BEAD, "body", fuse=False, surface="shell")
            beads += 1


def mane(k):
    # the fins at the back of the head and down the neck: pink plates, mint spines, in the head's plane turned to +X
    ex = c.turned(c.BACK_TURN)
    rel = lambda px, py: ((px - 470) * S_, (240 - py) * S_)
    tips = [rel(510, 78), rel(632, 98), rel(720, 185), rel(700, 270), rel(660, 330), rel(625, 400)]
    inner = [rel(560, 430), rel(530, 330), rel(500, 250), rel(485, 180)]
    c.fan(k, "manefin", HEAD_C, ex, tips, inner, hub=rel(510, 240), membrane=PINK, spine=MINT, thick=0.03,
          spine_r=0.018, notch=0.82)
    # long pink locks streaming from under the jaw down beside the chest, their tips mint, lifting clear
    locks = [  # (x at the root, x at the tip, start px py, end px py)
        (0.05, 0.1, (470, 300), (395, 560)), (0.07, 0.135, (488, 310), (440, 620)),
        (0.04, 0.075, (450, 305), (360, 500)), (0.075, 0.15, (505, 300), (500, 590))]
    for i, (x0, x1, (sx, sy), (ex_, ey)) in enumerate(locks):
        for s in (1, -1):
            a, b = at(sx, sy, s * x0), at(ex_, ey, s * x1)
            m1 = a + (b - a) * 0.4 + np.array([s * 0.018, -0.045, 0.0])
            m2 = a + (b - a) * 0.75 + np.array([s * 0.012, -0.03, 0.0])
            # the tip runs on down and a little out, and turns mint where the pink ends (no step between)
            end = b + (b - m2) / np.linalg.norm(b - m2) * 0.05 + np.array([s * 0.02, -0.012, 0.0])
            k.add(f"lock{i}{'RL'[s < 0]}", outward(tube([a, m1, m2, b], [0.033, 0.031, 0.026, 0.021], seg=14, per=6)),
                  PINK, "body", fuse=False, surface="hair")
            k.add(f"locktip{i}{'RL'[s < 0]}", outward(tube([b, (b + end) / 2 + np.array([0, -0.01, -0.012]), end],
                                                         [0.0205, 0.019, 0.0168], seg=14, per=4)), MINT, "body",
                  fuse=False, surface="hair")


def fins(k):
    # the great fin like a wing on the back, and the smaller one under it, turned toward +X
    o = at(550, 545)
    rel = lambda px, py: ((px - 550) * S_, (545 - py) * S_)
    tips = [rel(745, 322), rel(838, 315), rel(820, 405), rel(790, 470), rel(740, 545), rel(700, 610)]
    inner = [rel(580, 590), rel(575, 520)]
    c.fan(k, "wing", o, c.turned(38.0), tips, inner, hub=rel(560, 545), membrane=PINK, spine=MINT, thick=0.03,
          spine_r=0.018, notch=0.86)
    o2 = at(560, 700)
    rel2 = lambda px, py: ((px - 560) * S_, (700 - py) * S_)
    tips2 = [rel2(640, 665), rel2(712, 712), rel2(720, 770), rel2(690, 812)]
    inner2 = [rel2(585, 760), rel2(575, 690)]
    c.fan(k, "lowfin", o2, c.turned(40.0), tips2, inner2, hub=rel2(570, 715), membrane=PINK, spine=MINT,
          thick=0.028, spine_r=0.017, notch=0.84)
    c.pectoral(k, at(510, 520, 0.08), 0.1, color="#FBE4E0", rib=CORAL)


def calutregal(src, out):
    k = Kit(src, coat="scales")
    head(k)
    ears(k)
    crown(k)
    P, R = body(k)
    pearl_strings(k, P, R)
    mane(k)
    fins(k)
    k.build(voxel=0.0062, smooth=3, band=0.006)
    eye = c.make_eye(iris=IRIS, ink=INK, top=EYE_TOP, iris_r=0.6, lash=LASH)
    k.eye_states(c.lay_eyes(k, HEAD_C + EYE_N * HEAD_R * 1.02, EYE_N, EYE_HALF, eye, PEARL, ink=INK, top=EYE_TOP))
    k.export(out)


if __name__ == "__main__":
    calutregal(*args())
