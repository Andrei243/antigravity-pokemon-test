# Pidgeotto sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pidgeotto.py -- <the export's Pidgeotto.glb> <out.glb>
# Pidgey grown (built by pidgey.py's own functions, on a frame of its own joints): taller and slimmer, the same brown
# crown and back over a cream face and belly, the same almond eye and black wedge, now under a slanting brow; the tuft
# become a broad pink-red crest laid back from the forehead past the nape; wings folded at the sides with more of their
# cream feathers showing; a long fan of tail feathers, red and yellow in turn, the red ones longer; longer pink legs
# with heavier white claws. Both pictures show it flying with its wings spread: the pose is the clips', so the wings
# stay folded here.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, sibling, srgb, unit, args

p = sibling("pidgey")
RED, REDTIP, YELLOW = "#E2475E", "#C8283E", "#F3C651"


def frame():
    F = p.frame()
    F.H = np.array([0, -0.1, 0.53])                  # over the head's joint (0, -0.08, 0.46), under the plume's
    F.HR = np.array([0.094, 0.104, 0.088])
    F.BODY = ((0, 0.005, 0.305), (0.148, 0.165, 0.19), (-8, 0, 0))
    F.BREAST = ((0, -0.065, 0.32), (0.13, 0.118, 0.18))
    F.NECK = ((0, -0.075, 0.45), (0.094, 0.092, 0.095))
    F.WING = ([(0.104, -0.035, 0.415), (0.134, 0.035, 0.375), (0.142, 0.13, 0.29), (0.104, 0.245, 0.2)],
              [0.05, 0.088, 0.074, 0.013], 0.32)
    F.WINGCREAM = 0.33
    F.LEGX, F.LEGTOP, F.ANKLE, F.LEGY = 0.075, 0.15, 0.028, -0.03
    F.LEGR = (0.023, 0.019)
    F.TOE, F.HEEL, F.TOER = 0.068, 0.038, 0.0125
    F.BEAK = ((0, -0.21, 0.51), (0.026, 0.048, 0.024))
    F.BEAKLOW = ((0, -0.198, 0.488), (0.018, 0.03, 0.01))
    F.EYEDIR = unit(0.56, -0.8, 0.12)
    F.EYE = (0.025, 0.017)
    F.R = 0.1
    F.VOXEL = 0.006
    F.TAILBASE = np.array([0, 0.155, 0.29])
    F.CREAMLOW = [(1.55, 0.37), (1.55, 0.28), (1.75, 0.15), (3.2, 0.06), (3.2, -0.2)]
    return F


def crest(k, F):
    """A broad band of pink-red feathers from the forehead, laid back over the crown and lifting past the nape."""
    H, HR = F.H, F.HR
    # the band's root: the crown painted red from the forehead back, as wide as the head
    k.paint(lambda P: p.inside(H[2] + 0.045 + 0.25 * (P[:, 1] - H[1]) - P[:, 2], 0.003) * (P[:, 1] < H[1] + 0.07)
            * (P[:, 2] > H[2]), RED, on=["head", "neck"])
    # its feathers laid back from the crown in a broad flat sheaf, lifting a little past the nape
    for x, z0, t, w, col in [(0.0, 0.07, (0.0, 0.26, 0.105), 0.075, RED), (0.036, 0.06, (0.062, 0.22, 0.075), 0.066, RED),
                             (-0.036, 0.06, (-0.062, 0.22, 0.075), 0.066, RED), (0.0, 0.085, (0.0, 0.22, 0.135), 0.055, REDTIP)]:
        k.add("plume", p.spike(H + (x * 0.6, -0.06, z0), H + t, w, 0.03, side=(1, 0, -0.0), at=0.4), col, "plume",
              fuse=False)


def thighs(k, F, colour=p.CREAM, size=(0.048, 0.055, 0.06)):
    """Cream feathered thighs over the tops of the legs."""
    k.add("thigh", p.ell((F.LEGX, F.LEGY - 0.005, F.LEGTOP + 0.005), size), colour, "leg{s}", mirror=True)


def browed(look, F):
    """The brown brow slanting down over the eye toward the beak, in every state."""
    CB = srgb(p.BROWN)
    def f(u, v):
        c = look(u, v)
        c[v > 0.32 + 0.38 * u] = CB
        return c
    return f


def eyes(k, F):
    LID = srgb(p.CREAM)
    E = p.eye_centre(F)
    m = np.array([-1, 1, 1.0])
    def lay(state):
        look = browed(p.eye_look(state, p.eye_open, LID, p.INK, line=0.17), F)
        k.decal(E, F.EYEDIR, F.EYE, look, eps=0.006, outline=p.polygon(p.ALMOND), rings=20, segs=56)
        k.decal(E * m, F.EYEDIR * m, F.EYE, lambda u, v: look(-u, v), eps=0.006,
                outline=p.polygon([(-a, b) for a, b in p.ALMOND]), rings=20, segs=56)
    return lay


def pidgeotto(src, out):
    F = frame()
    k = Kit(src, coat="fur")
    p.body(k, F)
    p.wings_folded(k, F)
    p.face(k, F)
    p.beak(k, F)
    crest(k, F)
    p.tail(k, F, [(0, 0.21, 0.052, RED, None), (10, 0.2, 0.05, YELLOW, None), (-10, 0.2, 0.05, YELLOW, None),
                  (20, 0.215, 0.052, RED, None), (-20, 0.215, 0.052, RED, None)])
    thighs(k, F)
    p.legs(k, F)
    k.build(voxel=F.VOXEL)
    k.eye_states(eyes(k, F))
    k.export(out)


if __name__ == "__main__":
    pidgeotto(*args())
