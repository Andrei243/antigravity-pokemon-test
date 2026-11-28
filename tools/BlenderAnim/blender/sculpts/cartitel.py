# Cartitel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cartitel.py -- <the export's Cartitel.glb> <out.glb>
# Carpathia No. 10045 (plan 46), a velvety mole, Ground. From its picture: a plump pear of black velvet with no neck,
# its head the pear's top; a long pink snout poked upward and forward, sniffing, the pink spreading back onto the face
# at its root, two nostrils at its tip; tiny eyes shut in the fur as two happy arcs; broad pink spade hands with four
# long claws held at the belly's sides, palms down; short legs, pink feet with toes; a short pink tail; a little mound
# of fresh earth sitting on its head like a hat, crumbs fallen from it.
# Written to be grown from (cartiton.py): the hand, the claws, the foot, the mound, the snout and the eyes are
# functions.
# The skeleton: body (0, 0, 0.17), head (0, -0.02, 0.27), arms (+-0.1, -0.04, 0.21), legs (+-0.055, 0, 0.07),
# tail (0, 0.1, 0.1).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, eye_look, args, axes, lumpy, outward

BLACK, BELLY = "#2C2D34", "#3C3F4A"
PINK, PINK_D, CLAW = "#EE9E9E", "#D8807E", "#F2AEAA"
EARTH, EARTH_L, NOSTRIL = "#6C4828", "#8C6238", "#8A4A50"
INK = "#121014"
LASH = "#8C8E9C"          # the shut eye, a lighter line on the black fur so that it reads at the game's size


# --- pieces of the line --------------------------------------------------------------------------------------------
def claw(k, name, root, d, length, width, thick, colour, bone, mirror=True, surface="shell"):
    """A long claw: a flat-faced hull tapering from a wide root to a point along d, its flat across (width) square to
    d and to the up of its frame."""
    d = unit(*d)
    a, b = axes(d)
    root = np.array(root, float)
    pts = []
    for t, w in ((0.0, 1.0), (0.45, 0.8), (0.8, 0.42)):
        at = root + d * length * t
        for sa, sb in ((1, 0), (-1, 0), (0, 1), (0, -1), (0.7, 0.7), (-0.7, 0.7), (0.7, -0.7), (-0.7, -0.7)):
            pts.append(at + a * sa * width * w + b * sb * thick * w)
    pts.append(root + d * length)
    k.add(name, hull(pts, merge=1.0), colour, bone, fuse=False, mirror=mirror, surface=surface)


def spade_hand(k, centre, r, rot, colour, bone="arm{s}"):
    """A broad flat spade of a hand (fused, mirrored)."""
    k.add("hand", ell(centre, r, rot=rot), colour, bone, mirror=True)


def foot(k, centre, r, colour, toes, toe_r, bone="leg{s}"):
    """A pink foot and its rounded toes in a row at its front (mirrored)."""
    k.add("foot", ell(centre, r), colour, bone, mirror=True)
    c = np.array(centre, float)
    for i, dx in enumerate(np.linspace(-r[0] * 0.7, r[0] * 0.7, toes)):
        k.add(f"toe{i}", ell(c + (dx, -r[1] * 0.95, -r[2] * 0.3), (toe_r, toe_r * 1.3, toe_r * 0.8)), colour, bone,
              mirror=True)


def mound(k, centre, r, colour, light, bone="head", n=9, seed=3, crumbs=()):
    """A mound of fresh earth: a loose lumpy dome, lighter clods on it, crumbs fallen about it."""
    rs = np.random.RandomState(seed)
    c = np.array(centre, float)
    def dome(V):
        V = lumpy(7, 5, 0.12)(V)
        rr = np.sqrt(V[:, 0] ** 2 + V[:, 1] ** 2)
        V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * (1.0 + 0.9 * (1 - rr)), V[:, 2])
        V[:, 2] = np.where(V[:, 2] < -0.2, -0.2 + (V[:, 2] + 0.2) * 0.3, V[:, 2])
        return V
    k.add("mound", ell(c, r, deform=dome, sub=4), colour, bone, fuse=False, surface="leather")
    for i in range(n):
        th = rs.rand() * 2 * math.pi; rr = 0.35 + 0.5 * rs.rand()
        p = c + np.array([math.cos(th) * r[0] * rr, math.sin(th) * r[1] * rr, r[2] * (0.75 - 0.5 * rr * rr)])
        s = (0.18 + 0.14 * rs.rand()) * r[0]
        k.add(f"clod{i}", ell(p, (s, s * 0.9, s * 0.7), sub=2), light if i % 2 else colour, bone, fuse=False,
              surface="leather")
    for i, (p, s) in enumerate(crumbs):
        k.add(f"crumb{i}", ell(p, (s, s, s * 0.8), sub=2), colour, bone, fuse=False, surface="leather")


def arc_eye(kind, lid, ink, line=0.24):
    """An eye shut in the fur: a dark arc on the fur's colour. kind 'happy' curves up (the open state), 'shut' down."""
    L, K = srgb(lid), srgb(ink)
    def look(u, v):
        c = np.tile(L, (len(u), 1))
        wide = np.abs(u) < 0.86
        if kind == "happy":
            c[wide & (np.abs(v - (-0.3 + 0.55 * (1 - u * u))) < line)] = K
        elif kind == "shut":
            c[wide & (np.abs(v - (0.05 - 0.3 * (1 - u * u))) < line * 0.8)] = K
        elif kind == "fierce":
            c[wide & (np.abs(v - (-0.1 - 0.35 * u)) < line * 0.85)] = K
        elif kind == "squeeze":
            c[wide & (np.abs(np.abs(v) - 0.42 * (u + 0.86) / 1.72) < line * 0.7)] = K
        return c
    return look


# --- Cartitel ------------------------------------------------------------------------------------------------------
BODY_C, BODY_R = np.array([0.0, 0.005, 0.165]), np.array([0.14, 0.128, 0.155])
HEAD_C, HEAD_R = np.array([0.0, -0.03, 0.3]), np.array([0.108, 0.1, 0.088])
HAND = np.array([0.128, -0.145, 0.14])


def pear(V):
    V = V.copy()
    s = 1 + 0.1 * np.clip(-V[:, 2], 0, 1) - 0.08 * np.clip(V[:, 2], 0, 1)
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.maximum(V[:, 2], -0.93)
    return V


def build(src, out):
    k = Kit(src, coat="fur")
    k.add("body", ell(BODY_C, BODY_R, deform=pear), BLACK, "body")
    k.add("head", ell(HEAD_C, HEAD_R), BLACK, "head")
    k.add("cheek", ell((0, -0.085, 0.29), (0.07, 0.06, 0.06)), BLACK, "head")
    # the long pink snout, poked upward and forward, the pink spreading back onto the face
    k.add("snout", tube([(0, -0.09, 0.305), (0, -0.14, 0.335), (0, -0.178, 0.362)], [0.034, 0.027, 0.023]), PINK,
          "head", surface="skin")
    k.add("tip", ell((0, -0.19, 0.372), (0.026, 0.02, 0.022), rot=(-35, 0, 0)), PINK, "head", surface="skin")
    def root(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        e = (x / 0.042) ** 2 + ((z - 0.312) / 0.038) ** 2
        return inside(np.sqrt(e) - 1, 0.01) * inside(y + 0.085, 0.006)
    k.paint(root, PINK, on=["head", "cheek"])
    # the belly a shade lighter
    k.paint(lambda P: inside(np.sqrt((P[:, 0] / 0.09) ** 2 + ((P[:, 2] - 0.15) / 0.11) ** 2) - 1, 0.03)
            * inside(P[:, 1] + 0.06, 0.02), BELLY, on=["body"])

    # arms: short black stubs, the spade hands at the belly's sides, palms down, claws hanging
    k.add("arm", tube([(0.1, -0.04, 0.21), (0.12, -0.09, 0.185), (0.125, -0.125, 0.165)], [0.04, 0.036, 0.034]),
          BLACK, "arm{s}", mirror=True)
    spade_hand(k, HAND, (0.058, 0.034, 0.058), (-45, 0, -25), PINK)
    for i, (dx, dy) in enumerate(((-0.032, 0.004), (-0.011, -0.006), (0.011, -0.008), (0.031, 0.0))):
        claw(k, f"claw{i}", (HAND[0] + dx, HAND[1] - 0.012 + dy, HAND[2] - 0.035), (0.25 * dx / 0.03, -0.75, -1.0),
             0.042, 0.0155, 0.0105, CLAW, "arm{s}")

    # legs and pink feet
    k.add("leg", tube([(0.06, 0.0, 0.075), (0.064, -0.012, 0.045)], [0.045, 0.04]), BLACK, "leg{s}", mirror=True)
    foot(k, (0.068, -0.042, 0.021), (0.036, 0.048, 0.019), PINK, 3, 0.012)
    # the short pink tail
    k.add("tail", tube([(0, 0.11, 0.075), (0, 0.15, 0.1), (0, 0.178, 0.125)], [0.024, 0.017, 0.01]), PINK, "tail",
          surface="skin")

    # the mound of earth on the head, crumbs fallen from it
    mound(k, (0, 0.0, 0.372), (0.078, 0.07, 0.04), EARTH, EARTH_L,
          crumbs=[((0.07, -0.06, 0.36), 0.008), ((-0.055, -0.07, 0.37), 0.007), ((0.085, 0.0, 0.345), 0.007)])

    # nostrils and the mouth under the snout's root
    k.decal((0.0085, -0.205, 0.368), unit(0.2, -0.8, 0.55), (0.005, 0.006), NOSTRIL, eps=0.0015, rings=6, segs=24,
            mirror=True)
    M, B = srgb(INK), srgb(PINK)
    def smile(u, v):
        c = np.tile(B, (len(u), 1))
        c[(np.abs(v - (0.3 - 0.7 * (1 - u * u))) < 0.3) & (np.abs(u) < 0.85)] = M
        return c
    k.decal((0, -0.127, 0.304), unit(0, -0.6, -0.8), (0.02, 0.009), smile, eps=0.0015, rings=8, segs=40)

    k.build(voxel=0.0038, smooth=4, band=0.005)

    E = np.array([0.052, -0.098, 0.345])
    n = unit(0.45, -0.8, 0.35)
    looks = {"Open": "happy", "Smile": "happy", "Shut": "shut", "Squeeze": "squeeze", "Fierce": "fierce"}
    def eyes(state):
        k.decal(E, n, (0.017, 0.011), arc_eye(looks[state], BLACK, LASH), eps=0.0018, rings=14, segs=48, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    build(*args())
