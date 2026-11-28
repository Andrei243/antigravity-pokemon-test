# Harciog (Carpathia No. 10013) sculpted from nothing on the skeleton and clips of the game's own export (sculptkit.py):
#   blender --background --factory-startup --python sculpts/harciog.py -- <the export's Harciog.glb> <out.glb>
# From its picture (docs/art/reference/gemini/carpathia/10013-Harciog.jpg): a stout field hamster sitting up, one
# rounded pear of a body with the head on it; rusty orange all over the back and sides, a pure black belly as a great
# oval down the front, and two white cheeks so stuffed with seed that they stand out as balls as wide as the head,
# meeting at a white muzzle with a small pink nose and a little mouth; small round dark eyes with a glint set high over
# the cheeks; small round orange ears with paler insides; tiny pink paws held together under the cheeks; pink feet
# flat forward at the bottom; a stub of a tail behind.
#
# Written to be grown from: harciogre.py imports it (sibling("harciog")) and keeps its frame (scaled by K), its
# cheeks, muzzle, nose, ears, eyes and belly, and changes what its picture changes.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, polygon, args

ORANGE, BLACK, WHITE, PINK = "#C2562B", "#1E1C20", "#F6F6F8", "#E4BCBE"
NOSE, MOUTH, EARIN = "#D99090", "#5A3E40", "#DE9A7E"
INK, IRIS = "#1A0E0E", "#4A2422"


# --- the frame at Harciog's size (skeleton: body (0, 0, 0.22), head (0, -0.02, 0.36), arms (+-0.13, -0.06, 0.3),
#     ears (+-0.08, -0.01, 0.54), legs (+-0.09, -0.02, 0.08), tail (0, 0.1, 0.1); the export stands 0.6 tall).
#     Harciogre's skeleton is this one times 1.5 -------------------------------------------------------------------
def frame(K=1.0):
    F = dict(
        BODY=((0, 0.0, 0.215), (0.19, 0.165, 0.21)),
        LOWER=((0, 0.012, 0.15), (0.2, 0.172, 0.15)),
        HEAD=((0, -0.035, 0.448), (0.138, 0.122, 0.118)),
        CHEEK=((0.118, -0.082, 0.392), (0.102, 0.092, 0.092)),
        MUZZLE=((0, -0.128, 0.41), (0.055, 0.045, 0.046)),
        CHIN=((0, -0.12, 0.37), (0.05, 0.04, 0.03)),
        EAR=((0.088, -0.012, 0.548), (0.046, 0.022, 0.046)),
        HAUNCH=((0.13, 0.0, 0.1), (0.085, 0.105, 0.095)),
        FOOT=((0.11, -0.085, 0.02), (0.046, 0.072, 0.021)),
        TAIL=((0, 0.17, 0.06), (0.036, 0.04, 0.032)),
    )
    s = lambda c, r: (tuple(np.array(c) * K), tuple(np.array(r) * K))
    return {key: s(*v) for key, v in F.items()}


def body(k, F, K=1.0, orange=ORANGE, white=WHITE):
    k.add("body", ell(*F["BODY"]), orange, "body")
    k.add("lower", ell(*F["LOWER"]), orange, "body")
    k.add("head", ell(*F["HEAD"]), orange, "head")
    k.add("cheek", ell(*F["CHEEK"]), white, "head", mirror=True)
    k.add("muzzle", ell(*F["MUZZLE"]), white, "head")
    k.add("chin", ell(*F["CHIN"]), white, "head")
    k.add("ear", ell(*F["EAR"], rot=(0, 18, 0)), orange, "ear{s}", mirror=True)
    k.add("haunch", ell(*F["HAUNCH"]), orange, "leg{s}", mirror=True)
    k.add("foot", ell(*F["FOOT"]), PINK, "leg{s}", mirror=True)
    k.add("tail", ell(*F["TAIL"]), orange, "tail")


def ear_linings(k, F, colour=EARIN):
    c, r = F["EAR"]
    for s in (1, -1):
        cc = np.array([c[0] * s, c[1] - r[1] * 0.6, c[2] + r[2] * 0.08])
        k.paint(lambda P, cc=cc, r=r: inside(np.hypot((P[:, 0] - cc[0]) / (r[0] * 0.62), (P[:, 2] - cc[2]) / (r[2] * 0.62)) - 1,
                                             0.08) * inside(P[:, 1] - cc[1], 0.002), colour, on=["ear"])


def belly(k, K=1.0, colour=BLACK, top=0.335, half=0.122, mid=0.18, front=-0.02):
    """The black belly, a great oval down the front from under the cheeks to the feet."""
    k.paint(lambda P: inside((P[:, 0] / (half * K)) ** 2 + ((P[:, 2] - mid * K) / ((top - mid) * K)) ** 2 - 1, 0.03)
            * inside(P[:, 1] - front * K, 0.004 * K), colour, on=["body", "lower"])


def nose(k, F, K=1.0, colour=NOSE):
    c, r = F["MUZZLE"]
    p = np.array(c) + np.array([0, -r[1] * 0.92, r[2] * 0.5])
    k.add("nose", ell(p, np.array([0.017, 0.011, 0.012]) * K, rot=(-15, 0, 0)), colour, "head", fuse=False,
          surface="skin")


def mouth(k, F, K=1.0, frown=False, colour=MOUTH, ground=WHITE):
    """A little cat's mouth under the nose (a frown on Harciogre): one clean line in a patch on the muzzle."""
    c, r = F["MUZZLE"]
    p = np.array(c) + np.array([0, -r[1] * 0.94, -r[2] * 0.25])
    G, M = srgb(ground), srgb(colour)
    def look(u, v):
        out_ = np.tile(G, (len(u), 1))
        if frown:
            line = np.abs(v - (0.25 - 0.7 * u * u)) < 0.2
            line |= (np.abs(u) < 0.14) & (v > 0.15)
        else:
            line = np.abs(v - (-0.15 + 0.55 * (np.abs(u) - 0.45) ** 2 * 3)) < 0.2
            line &= np.abs(u) < 0.9
            line |= (np.abs(u) < 0.12) & (v > -0.1)
        out_[line] = M
        return out_
    k.decal(p, unit(0, -1, -0.15), (0.024 * K, 0.016 * K), look, eps=0.003 * K, rings=12, segs=48)


def eye_open(u, v):
    c = np.tile(srgb(INK), (len(u), 1))
    c[np.hypot(u + 0.05, v + 0.1) < 0.62] = srgb(IRIS)
    c[np.hypot(u + 0.05, v + 0.1) < 0.36] = srgb(INK)
    c[np.hypot(u + 0.3, v - 0.32) < 0.27] = (1.0, 1.0, 1.0)
    return c


def eyes(k, F, K=1.0, lid=ORANGE, look=eye_open, half=(0.023, 0.026), d=(0.38, -0.78, 0.34), outline=None):
    """Two patches laid by hand, u toward the back on both."""
    c, r = F["HEAD"]
    D = unit(*d)
    E = np.array(c) + D / np.linalg.norm(D / np.array(r))
    m = np.array([-1, 1, 1.0])
    h = (half[0] * K, half[1] * K)
    def lay(state):
        f = eye_look(state, look, srgb(lid), srgb(INK), line=0.16)
        kw = dict(eps=0.004 * K, rings=20, segs=56)
        if outline is not None:
            k.decal(E, D, h, f, outline=polygon(outline), **kw)
            k.decal(E * m, D * m, h, lambda u, v: f(-u, v), outline=polygon([(-a, b) for a, b in outline]), **kw)
        else:
            k.decal(E, D, h, f, **kw)
            k.decal(E * m, D * m, h, lambda u, v: f(-u, v), **kw)
    return lay


def paws_together(k):
    """Tiny arms round the front of the belly, the pink paws held together under the cheeks."""
    k.add("arm", tube([(0.15, -0.05, 0.31), (0.13, -0.135, 0.265), (0.055, -0.172, 0.27)], [0.04, 0.034, 0.028]),
          ORANGE, "arm{s}", mirror=True)
    k.add("paw", ell((0.034, -0.18, 0.272), (0.03, 0.022, 0.026)), PINK, "arm{s}", mirror=True)


def harciog(src, out_path):
    F = frame()
    k = Kit(src, coat="fur")
    body(k, F)
    paws_together(k)
    belly(k)
    ear_linings(k, F)
    nose(k, F)
    mouth(k, F)
    k.build(voxel=0.006, band=0.004, wband=0.05)
    k.eye_states(eyes(k, F))
    k.export(out_path)


if __name__ == "__main__":
    harciog(*args())
