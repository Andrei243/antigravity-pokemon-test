# Mega Victreebel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/victreebelmega.py -- <the export's Victreebel-Mega.glb> <out.glb>
# Grown from victreebel.py (its colours, spots, lip and half-lidded eyes) and bellsprout.py (lips). From the references:
# the pitcher has swollen into a great squat gourd, wider than tall, spotted with olive; out of its top a narrow neck
# wound round with three turns of its own brown vine; on it a small yellow head, its mouth now a closed pink lip rolled
# on top like a cap, two half-lidded eyes under it; from the cap's middle a short stem and a great crown of serrated
# leaves fanning up and out, green at the heart and turning orange to their edges, the largest change; the vine runs
# on from the neck to both sides, round the gourd's shoulders to small two-toned leaves.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, unit, eye_look, polygon, args, sibling

vb = sibling("victreebel")
bs = vb.bs
INK, WHITE = bs.INK, bs.WHITE
BELL, SPOT, LIP = "#E6DE48", "#A4A446", "#E9A2A6"
VINE = "#8E5C36"
GREEN, ORANGE, RIB = "#8CC662", "#F08E4E", "#5E9446"

# --- the frame -------------------------------------------------------------------------------------------------------
G, GR = np.array([0.0, 0.0, 0.205]), np.array([0.345, 0.3, 0.205])    # the gourd
HC, HR = np.array([0.0, -0.012, 0.548]), np.array([0.085, 0.08, 0.08])  # the head
CAP = np.array([0.0, -0.02, 0.618])                                     # the rolled lip's middle
CAPD = unit(0, -0.25, 1)
STEMTOP = np.array([0.0, 0.0, 0.66])                                    # where the crown's leaves spring from


def on_gourd(d, lift=0.0):
    d = np.array(d, float)
    return G + d / np.linalg.norm(d / GR) + unit(*d) * lift


def leaf(k, name, root, d, length, width, bone, tilt=-0.3, small=False):
    """A serrated leaf: an orange plate, a thicker green heart inside it showing on both faces, a dark midrib."""
    d = unit(*d)
    ez = unit(*(d + np.array([0, tilt, 0])))
    ex = np.cross(ez, (0, 1.0, 0))
    if np.linalg.norm(ex) < 1e-3:
        ex = np.array([1.0, 0, 0])
    ex = ex / np.linalg.norm(ex)
    L, W = length, width
    right = [(0.0, 0.0), (0.22 * W, 0.08 * L), (0.46 * W, 0.24 * L), (0.38 * W, 0.33 * L), (0.52 * W, 0.45 * L),
             (0.36 * W, 0.55 * L), (0.4 * W, 0.66 * L), (0.22 * W, 0.78 * L), (0.22 * W, 0.86 * L), (0.0, L)]
    if small:
        right = [(0.0, 0.0), (0.3 * W, 0.15 * L), (0.5 * W, 0.42 * L), (0.36 * W, 0.7 * L), (0.0, L)]
    outline = right + [(-a, b) for a, b in right[-2:0:-1]]
    k.add(name, slab(outline, root, ex, ez, 0.014), ORANGE, bone, fuse=False, surface="leaf")
    heart = [(math.sin(t) * 0.36 * W, (0.03 + 0.62 * (1 - math.cos(t)) / 2) * L) for t in np.linspace(0, 2 * math.pi, 24, endpoint=False)]
    if small:
        heart = [(a * 1.15, b * 1.05) for a, b in heart]
    k.add(name + "heart", slab(heart, root, ex, ez, 0.022), GREEN, bone, fuse=False, surface="leaf")
    rib = [(0.012, 0.0), (0.008, 0.62 * L), (-0.008, 0.62 * L), (-0.012, 0.0)]
    k.add(name + "rib", slab(rib, root, ex, ez, 0.026), RIB, bone, fuse=False, surface="leaf")


def victreebelmega(src, out):
    k = Kit(src, coat="leaf")

    # The gourd: squat, flatter beneath; the neck out of its top; the small head
    def squat(V):
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.85, V[:, 2])
        return V
    k.add("gourd", ell(G, GR, deform=squat), BELL, "body")
    k.add("neck", tube([(0, 0, 0.34), (0, -0.005, 0.44), HC], [0.09, 0.062, 0.06]), BELL, "body")
    k.add("head", ell(HC, HR), BELL, "head")

    # The rolled pink lip on top like a cap, its middle filled
    k.add("lip", bs.lips(CAP, CAPD, 0.062, 0.03), LIP, "head", fuse=False, surface="skin")
    k.add("capfill", ell(CAP + CAPD * 0.006, (0.07, 0.065, 0.022)), LIP, "head", fuse=False, surface="skin")
    # a short brown stem out of the cap to the crown
    k.add("stem", tube([CAP + CAPD * 0.01, STEMTOP], [0.022, 0.02], seg=14), VINE, "head", fuse=False)

    # The crown: serrated leaves fanning up and out from the stem, faces to the front, a pair leaning back
    for name, a, L, W, back in (("crown0", 0, 0.41, 0.25, 0), ("crown1", 38, 0.36, 0.23, 0), ("crown2", -38, 0.36, 0.23, 0),
                                ("crown3", 72, 0.3, 0.2, 0), ("crown4", -72, 0.3, 0.2, 0),
                                ("crown5", 22, 0.3, 0.2, 1), ("crown6", -22, 0.3, 0.2, 1)):
        ar = math.radians(a)
        d = (math.sin(ar), 0.35 if back else -0.05 - 0.3 * abs(a) / 72, math.cos(ar))
        leaf(k, name, STEMTOP + np.array([0, 0.01 if back else -0.01, -0.01]), d, L, W, "head",
             tilt=0.35 if back else -0.3)

    # The vine: three turns round the neck, then on to both sides over the gourd's shoulders
    coil = [((0.09 - 0.0012 * t) * math.cos(t), -0.005 + (0.087 - 0.0012 * t) * math.sin(t), 0.385 + 0.035 * t / (2 * math.pi))
            for t in np.linspace(-0.6, 6 * math.pi - 0.6, 64)]
    k.add("coil", tube(coil, [0.019] * len(coil), seg=12, per=2), VINE, "body", fuse=False)
    right = [coil[-1], on_gourd((0.3, -0.35, 0.85), 0.018), on_gourd((0.6, -0.5, 0.55), 0.018),
             on_gourd((0.78, -0.45, 0.3), 0.018)]
    k.add("viner", tube(right, [0.019, 0.019, 0.018, 0.017], seg=12), VINE, "body", fuse=False)
    left = [coil[0], on_gourd((-0.35, 0.1, 0.9), 0.018), on_gourd((-0.75, 0.15, 0.6), 0.02),
            np.array([-0.39, 0.06, 0.32]), np.array([-0.42, 0.0, 0.22]), np.array([-0.39, -0.03, 0.16])]
    k.add("vinel", tube(left, [0.019, 0.019, 0.018, 0.017, 0.016, 0.014], seg=12), VINE, "body", fuse=False)
    # the small two-toned leaves: at the right vine's end, at the left vine's curl, one on the left shoulder
    leaf(k, "leafr", right[-1], (0.45, -0.6, 0.2), 0.15, 0.1, "body", tilt=0.0, small=True)
    leaf(k, "leafl", np.array([-0.4, 0.04, 0.3]), (-0.35, -0.3, 0.9), 0.14, 0.09, "body", tilt=-0.2, small=True)
    leaf(k, "leafs", on_gourd((-0.45, -0.45, 0.75), 0.0), (-0.25, -0.45, 0.85), 0.15, 0.1, "body", tilt=-0.2, small=True)

    # The olive spots over the gourd
    rs = np.random.RandomState(29)
    for j, d in enumerate([(0.35, -0.85, 0.3), (-0.4, -0.8, -0.1), (0.05, -0.95, -0.35), (0.75, -0.5, -0.2),
                           (-0.8, -0.4, 0.3), (0.85, 0.1, 0.25), (-0.85, 0.3, -0.25), (0.5, 0.65, -0.2),
                           (-0.3, 0.85, 0.3), (0.2, 0.8, 0.45), (-0.55, -0.55, -0.5), (0.55, -0.5, -0.6),
                           (0.0, 0.95, -0.2), (-0.15, -0.7, 0.65), (0.7, 0.4, 0.55)]):
        n = unit(*d)
        p = on_gourd(d)
        shape = [(math.cos(t) * (0.75 + 0.4 * rs.rand()), math.sin(t) * (0.6 + 0.35 * rs.rand()))
                 for t in np.linspace(0, 2 * math.pi, 7, endpoint=False)]
        k.decal(p, n, (0.04, 0.032), SPOT, eps=0.004, outline=polygon(shape), rings=8, segs=36)

    k.build(voxel=0.0065, smooth=4, band=0.006)

    # The eyes: the base's, under the cap: half lidded (their top a flat lid line)
    lid = srgb(BELL)
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(WHITE, (len(u), 1))
        c[u ** 2 + (v + 0.3) ** 2 < 0.42 ** 2] = INK
        c[(r > 0.84) | (v > -0.02)] = INK
        return c
    TOPCUT = polygon([(math.cos(t), max(min(math.sin(t), 0.14), -1)) for t in np.linspace(0, 2 * math.pi, 40,
                                                                                         endpoint=False)])
    n = unit(0.4, -0.88, 0.2)
    EYE = HC + n / np.linalg.norm(n / HR)
    def eyes(state):
        k.decal(EYE, n, (0.026, 0.026), eye_look(state, look, lid, INK, line=0.15), eps=0.005,
                outline=TOPCUT if state in ("Open", "Fierce") else None, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    victreebelmega(*args())
