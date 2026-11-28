# Pupazoi sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pupazoi.py -- <the export's Pupazoi.glb> <out.glb>
# Grown from pupazel.py (sibling("pupazel")): the same cinnamon, cream, black and white, the same round dark eye in its
# pale tan ring, the same crest feather (cinnamon, a white band, a black tip), the same barred feathers and feet.
# From its picture (docs/art/reference/gemini/carpathia/10048-Pupazoi.jpg) and its brief (CP-dex-1b, No. 48): the bold
# hoopoe, standing tall and leaning a little forward on grey legs, a short thick neck, a cream breast ending in a
# point; a great crest fanned open over the head like a crown, every feather tipped white and black; a long dark beak
# curving down, open and calling, pink inside; a mischievous eye under a slanting brow; wings half spread and raised,
# a cinnamon inner wing and long black flight feathers barred with white; a long black tail with a white band hanging
# back and down. The art flings the wings wide: here they are raised in a V and swept back so the model stays in its
# cell and both of the game's cameras see their faces.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, tri, unit, args, sibling

pz = sibling("pupazel")
CINNAMON, CREAM, BLACK, WHITE = "#CC7A5A", pz.CREAM, pz.BLACK, pz.WHITE     # the grown bird a little pinker


def frame():
    F = pz.frame()
    F.BODY = ((0, 0.02, 0.37), (0.148, 0.165, 0.2))
    F.H = np.array([0, -0.092, 0.71])
    F.HR = np.array([0.097, 0.1, 0.09])
    F.EYEDIR = unit(0.6, -0.7, 0.3)
    F.EYE = (0.025, 0.025)
    F.BEAK_UP = ([(0, -0.16, 0.724), (0, -0.23, 0.722), (0, -0.3, 0.705), (0, -0.365, 0.675), (0, -0.405, 0.648)],
                 [0.023, 0.018, 0.013, 0.0095, 0.0072])
    F.BEAK_LO = ([(0, -0.16, 0.692), (0, -0.215, 0.674), (0, -0.27, 0.648), (0, -0.31, 0.624)],
                 [0.019, 0.014, 0.0098, 0.0072])
    F.MOUTH = ((0, -0.185, 0.706), (0.017, 0.04, 0.015))
    F.CREST = [(-48, 0.14), (-36, 0.165), (-24, 0.188), (-12, 0.205), (0, 0.215), (12, 0.218), (24, 0.212), (36, 0.2), (48, 0.185), (62, 0.16),
               (76, 0.13)]
    F.CREST_RAKE = 8
    F.CREST_W, F.CREST_T = 0.072, 0.02
    F.TAILBASE = np.array([0, 0.15, 0.3])
    F.TAIL = [(-8, 0.34, 0.062), (8, 0.34, 0.062), (24, 0.3, 0.056), (-24, 0.3, 0.056)]
    F.LEGX, F.LEGTOP, F.ANKLE, F.LEGY = 0.06, 0.22, 0.026, 0.004
    F.LEGR = (0.0165, 0.0145)
    F.TOE, F.HEEL, F.TOER = 0.07, 0.04, 0.011
    F.WINGROOT = np.array([0.105, 0.01, 0.56])
    F.VOXEL = 0.0072
    return F


def body(k, F):
    c, r = F.BODY
    k.add("body", ell(c, r, rot=(18, 0, 0)), CINNAMON, "body")
    k.add("neck", tube([(0, -0.03, 0.52), (0, -0.058, 0.6), (0, -0.078, 0.67)], [0.096, 0.08, 0.072]), CINNAMON, "head")
    k.add("head", ell(F.H, F.HR), CINNAMON, "head")
    k.add("shoulder", ell((0.1, 0.005, 0.555), (0.05, 0.055, 0.065)), CINNAMON, "wing{s}", mirror=True)
    k.add("thigh", ell((0.064, 0.025, 0.22), (0.062, 0.07, 0.08)), CINNAMON, "leg{s}", mirror=True)


def breast(k, F):
    """Cream from the throat down the chest to a point, its edge ragged."""
    def mask(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        az = np.arctan2(np.abs(x), -(y - 0.0))
        edge = 0.37 + 1.5 * np.abs(x) + 0.014 * tri(x / 0.03 + 0.5)
        return (inside(edge - z, 0.004) * inside(az - 0.62, 0.05) * inside(z - 0.69, 0.006)
                * inside(y + 0.03, 0.01))
    k.paint(mask, CREAM)


def wings(k, F):
    """Half spread and raised: a cinnamon inner wing over six long black flight feathers barred white."""
    ex, ez = unit(1, 0.62, -0.05), unit(0, 0.3, 1)
    ez = ez - ex * (ez @ ex); ez /= np.linalg.norm(ez)
    n = np.cross(ex, ez)                     # toward the front
    root = F.WINGROOT
    bars = [(0.36, 0.45, WHITE), (0.56, 0.65, WHITE), (0.76, 0.85, WHITE)]
    for i, (th, ln) in enumerate(((16, 0.27), (29, 0.3), (42, 0.31), (55, 0.31), (68, 0.29), (81, 0.25))):
        t = math.radians(th)
        d = ex * math.cos(t) + ez * math.sin(t)
        across = -ex * math.sin(t) + ez * math.cos(t)
        b = root + d * 0.03 - n * (0.004 * i)
        pz.feather(k, f"flight{i}", b, d, across, ln, 0.094, 0.016, BLACK, "wing{s}", bands=bars, mirror=True)
    for i, (th, ln) in enumerate(((22, 0.19), (40, 0.205), (58, 0.2), (76, 0.17))):
        t = math.radians(th)
        d = ex * math.cos(t) + ez * math.sin(t)
        across = -ex * math.sin(t) + ez * math.cos(t)
        b = root + d * 0.01 + n * 0.012
        pz.feather(k, f"covert{i}", b, d, across, ln, 0.085, 0.026, CINNAMON, "wing{s}", mirror=True)


def pupazoi(src, out_path):
    F = frame()
    k = Kit(src, coat="fur")
    pz.CINNAMON = CINNAMON                   # the crest and the lids take the grown bird's colour
    body(k, F)
    breast(k, F)
    pz.crest(k, F, tip_white=(0.66, 0.8), tip_black=(0.8, 1.0), spread=0.016, splay=10)
    wings(k, F)
    pz.tail(k, F, pitch=-40, lean=34, band=(0.5, 0.64), thick=0.016)
    pz.beak(k, F)
    pz.legs(k, F)
    k.build(voxel=F.VOXEL, band=0.008, wband=0.06)
    # the brow slants down toward the beak (u runs toward the back): a bold, mischievous look
    brow = [(-1.0, 0.0), (-0.75, -0.66), (0.0, -1.0), (0.75, -0.66), (1.0, 0.05), (0.8, 0.62), (0.2, 0.84),
            (-0.55, 0.42)]
    k.eye_states(pz.eyes(k, F, lid=CINNAMON, outline=brow))
    k.export(out_path)


if __name__ == "__main__":
    pupazoi(*args())
