# Izvorana (Carpathia No. 10102, the mountain spring spirit) sculpted from nothing on the skeleton and clips of the
# game's own export of it (sculptkit.py), grown from Izvoras (sibling("izvoras")): its palette of spring water, on_ell,
# its navy eyes with one glint and the water's own colour for their lids, its mossy grey stone.
#   blender --background --factory-startup --python sculpts/izvorana.py -- <the export's Izvorana.glb> <out.glb>
#
# From its picture (docs/art/reference/gemini/carpathia/10102-Izvorana.jpg) and its brief (CP-dex-2b-mountains-more.md,
# 102): a slim maiden poured of clear running water. A small calm face with almond navy eyes and a faint smile, long
# hair of deeper water falling behind the shoulders to the waist with a lock framing each cheek; a crown of fern
# fronds and round watercress leaves on her head; a slender body with a pinched waist that flows below the hips into a
# long gown of water flaring to the ground and pooling there, streaked paler down its folds; arms held out and down,
# and from each forearm a broad streamer of water running down beside the gown; smooth grey pebbles with moss lying at
# the gown's hem (the brief's, kept on `root`). The picture's drops in the air are left out.
# Bones: root (0, 0, 0), body (0, 0, 0.562), armR/L (+-0.075, 0, 0.692), head (0, -0.005, 0.772).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, leaf, outward, srgb, inside, smoothstep, unit, eye_look, facets, lumpy, \
    polygon, args, sibling

iz = sibling("izvoras")

SKIN, HAIR, DEEP, PALE = "#84D3E6", "#56ADD9", iz.DEEP, iz.PALE
FERN, FERNDARK, CRESS = "#4E8E3E", "#3C7432", "#79B456"
STONE, MOSS = iz.STONE, iz.MOSS

HC, HR = np.array([0.0, -0.006, 0.836]), np.array([0.071, 0.067, 0.077])
EYE_AZ, EYE_EL, EYE_HALF = 25.0, 1.0, (0.018, 0.0135)
VOXEL = 0.0058


def gown(k):
    """The gown: a bell of water from the hips flaring to the ground, a little deeper side to side than front to back,
    pooling round its hem."""
    prof = [(0.57, 0.055), (0.5, 0.062), (0.42, 0.075), (0.33, 0.095), (0.24, 0.125), (0.16, 0.165), (0.09, 0.205),
            (0.045, 0.228)]
    V, F = tube([(0, 0.0, z) for z, _ in prof], [r for _, r in prof], seg=24)
    V = V * np.array([1.0, 0.86, 1.0])
    V[:, 2] = np.maximum(V[:, 2], 0.004)
    k.add("gown", (V, F), SKIN, "root", surface="shell")
    k.add("pool", ell((0, 0.0, 0.022), (0.25, 0.215, 0.026), deform=lumpy(7, 1, 0.08)), SKIN, "root",
          surface="shell")
    # paler streaks down its folds, deeper water at its foot
    def streaks(P):
        th = np.arctan2(P[:, 0], -P[:, 1])
        r = np.hypot(P[:, 0], P[:, 1])
        res = np.zeros(len(P))
        for j, a in enumerate((-50, 18, 52, 150, 215)):
            d = np.abs(np.sin((th - math.radians(a + 8 * math.sin(j))) / 2)) * 2 * r
            top = 0.44 - 0.06 * (j % 2)
            res = np.maximum(res, inside(d - 0.006 - 0.014 * smoothstep(top, 0.08, P[:, 2]), 0.004)
                             * inside(P[:, 2] - top, 0.03))
        return res * (P[:, 2] > 0.07)
    k.paint(streaks, PALE, on=["gown"])
    k.paint(lambda P: smoothstep(0.1, 0.03, P[:, 2]), DEEP, on=["gown", "pool"])


def body(k):
    # one smooth trunk: hips, a pinched waist, the chest broader under the shoulders
    V, F = tube([(0, 0.0, z) for z in (0.5, 0.55, 0.6, 0.65, 0.69)], [0.066, 0.06, 0.052, 0.07, 0.06], seg=24)
    k.add("torso", (V * np.array([1.0, 0.8, 1.0]), F), SKIN, "body", surface="shell")
    k.add("chest", ell((0, -0.012, 0.655), (0.066, 0.045, 0.042)), SKIN, "body", surface="shell")
    k.add("neck", tube([(0, 0.0, 0.695), (0, -0.003, 0.78)], [0.026, 0.024]), SKIN, "head", surface="shell")
    # a glint of the water on the chest's +X and at the waist
    def gloss(P):
        res = np.zeros(len(P))
        for c, r in (((0.04, -0.05, 0.665), (0.012, 0.02, 0.018)), ((-0.028, -0.045, 0.58), (0.01, 0.02, 0.03))):
            res = np.maximum(res, inside(np.linalg.norm((P - np.array(c)) / np.array(r), axis=1) - 1, 0.1))
        return res
    k.paint(gloss, PALE, on=["chest", "torso"])


def arms(k):
    for s, nm in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        sh, el, ha = np.array((0.072, 0.0, 0.69)) * m, np.array((0.125, -0.035, 0.605)) * m, \
            np.array((0.145, -0.085, 0.525)) * m
        k.add("arm" + nm, tube([sh, el, ha], [0.027, 0.023, 0.019]), SKIN, "arm" + nm, surface="shell")
        k.add("hand" + nm, ell(ha + np.array((0.004 * s, -0.01, -0.02)), (0.016, 0.02, 0.026), rot=(-20, 0, 0)), SKIN,
              "arm" + nm, surface="shell")
        # the streamer of water from the forearm, broad, its face turned to the front three-quarter and out
        # it leaves the whole forearm (from just below the elbow to the wrist) and falls broad beside the gown,
        # curling out at its foot
        pts = [el * 0.6 + ha * 0.4 + np.array((0.012 * s, 0.012, -0.01)), np.array((0.165 * s, 0.01, 0.46)),
               np.array((0.185 * s, 0.03, 0.36)), np.array((0.195 * s, 0.04, 0.26)), np.array((0.215 * s, 0.035, 0.17)),
               np.array((0.245 * s, 0.02, 0.12))]
        face = unit(0.55 * s, -0.83, 0)
        k.add("streamer" + nm, outward(blade(pts, [0.034, 0.045, 0.05, 0.048, 0.04, 0.022], face, 0.4)),
              SKIN, "arm" + nm, fuse=False, surface="shell")
        k.add("streampale" + nm, outward(blade([p + face * 0.011 for p in pts[1:5]],
                                               [0.01, 0.012, 0.011, 0.008], face, 0.5)),
              PALE, "arm" + nm, fuse=False, surface="shell")


def head(k):
    k.add("head", ell(HC, HR), SKIN, "head", surface="shell")
    k.add("jaw", ell(HC + (0, -0.014, -0.04), (0.044, 0.044, 0.038)), SKIN, "head", surface="shell")
    # the hair: a cap over the crown and sides set back so the face stands forward of it, a mass behind, long falls
    g = 1.1
    k.add("haircap", ell(HC + np.array((0, 0.02, 0.022)) * g, (0.079, 0.07, 0.075)), HAIR, "head", surface="hair")
    k.add("hairback", ell(HC + np.array((0, 0.05, -0.03)) * g, (0.077, 0.055, 0.077)), HAIR, "head", surface="hair")
    for s, nm in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0]) * g
        # each fall flows out over the shoulder and down the back in a slow wave, broad, ending at the waist
        # (rooted inside the cap behind the ear, hugging the head, widening only below the jaw)
        k.add("fall" + nm, tube([HC + np.array(p) * m for p in [(0.035, 0.035, 0.0), (0.058, 0.045, -0.06),
                                                                (0.085, 0.06, -0.13), (0.095, 0.07, -0.2),
                                                                (0.108, 0.07, -0.26), (0.098, 0.06, -0.31)]],
                                [0.034, 0.036, 0.036, 0.03, 0.024, 0.015]), HAIR, "head", surface="hair")
        # the hair's side mass over each ear, joining the cap to the falls, and a lock down the cheek from it
        k.add("side" + nm, ell(HC + np.array((0.05, 0.012, -0.02)) * m, (0.032, 0.05, 0.07)), HAIR, "head",
              surface="hair")
        k.add("lock" + nm, tube([HC + np.array(p) * m for p in [(0.056, -0.022, 0.0), (0.064, -0.024, -0.055),
                                                                (0.07, -0.016, -0.1), (0.074, -0.006, -0.13)]],
                                [0.019, 0.018, 0.015, 0.011]), HAIR, "head", surface="hair")
    k.add("fallmid", tube([HC + np.array(p) * g for p in [(0, 0.06, 0.0), (0, 0.08, -0.12), (0, 0.085, -0.25),
                                                          (0, 0.075, -0.31)]], [0.055, 0.05, 0.038, 0.022]),
          HAIR, "head", surface="hair")
    # the face's own water colour back over the front where the cap meets it
    def face(P):
        q = P - HC
        return inside(q[:, 1] + 0.045, 0.003) * inside(q[:, 2] - 0.03 + 0.5 * np.abs(q[:, 0]), 0.003)
    k.paint(face, SKIN, on=["head", "haircap", "jaw"])


def crown(k):
    """Fern fronds and watercress round the crown: fronds swept out and up on a ring, cress leaves between."""
    top = HC + (0, 0.012, 0.068)
    for i in range(8):
        a = (i / 8) * 2 * math.pi + 0.3
        rim = top + np.array([math.sin(a) * 0.052, -math.cos(a) * 0.048, -0.01])
        outd = np.array([math.sin(a), -math.cos(a), 0.0])
        if i % 2 == 0:
            d = unit(*(outd * 0.75 + np.array([0, 0, 0.75])))
            k.add(f"frond{i}", outward(leaf(rim, d, 0.085, 0.017, thick=0.014)), FERN, "head", fuse=False,
                  surface="leaf")
            side = unit(*np.cross(d, (0, 0, 1.0)))
            for j, t in enumerate((0.3, 0.55)):
                for sg in (1, -1):
                    b0 = rim + d * 0.085 * t
                    dd = unit(*(d * 0.5 + side * sg * 0.85))
                    k.add(f"leaflet{i}{j}{'a' if sg > 0 else 'b'}", outward(leaf(b0, dd, 0.03, 0.011, thick=0.012)),
                          FERNDARK, "head", fuse=False, surface="leaf")
        for j, (ofs, lift) in enumerate(((0.0, 0.006), (0.4, 0.018))):
            aa = a + ofs
            p = top + np.array([math.sin(aa) * 0.05, -math.cos(aa) * 0.046, lift])
            k.add(f"cress{i}{j}", outward(ell(p, (0.019, 0.019, 0.011), rot=(20 * math.cos(aa), -20 * math.sin(aa), 0),
                                             sub=3)), CRESS, "head", fuse=False, surface="leaf")


def pebbles(k):
    for j, (x, y, r) in enumerate(((0.22, -0.13, 0.045), (-0.215, -0.1, 0.04), (0.13, -0.2, 0.03), (-0.2, 0.12, 0.034),
                                   (0.2, 0.12, 0.03))):
        c = (x, y, r * 0.55)
        k.add(f"pebble{j}", outward(ell(c, (r, r * 0.82, r * 0.6), deform=facets(8, 0.3, j + 3), sub=3)), STONE,
              "root", fuse=False, surface="shell")
        if j < 3:
            k.add(f"pebblemoss{j}", outward(ell((x, y, r * 1.1), (r * 0.45, r * 0.4, r * 0.2), deform=lumpy(4, 2, 0.3),
                                                sub=2)), MOSS, "root", fuse=False, surface="leaf")


def mouth(k):
    Sk, Mo = srgb(SKIN), srgb("#2D5A8C")
    def smile(u, v):
        c = np.tile(Sk, (len(u), 1))
        c[(np.abs(v - (-0.25 + 0.6 * u * u)) < 0.3) & (np.abs(u) < 0.85)] = Mo
        return c
    p, n = iz.on_ell(HC + (0, -0.014, -0.04), (0.044, 0.044, 0.038), 0, 18)
    k.decal(p, n, (0.013, 0.005), smile, eps=0.0018, rings=10, segs=48)


def izvorana(src, out):
    k = Kit(src, coat="shell")
    gown(k)
    body(k)
    arms(k)
    head(k)
    crown(k)
    pebbles(k)
    mouth(k)
    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.05)
    iz.board(k.ob)
    k.eye_states(lambda state: iz.lay_eyes(k, HC, HR, EYE_AZ, EYE_EL, EYE_HALF, SKIN, state, eps=0.0018))
    k.export(out)


if __name__ == "__main__":
    izvorana(*args())
