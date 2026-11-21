# Tinkatink sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tinkatink.py -- <the export's Tinkatink.glb> <out.glb>
# The base of its line: tinkatuff.py and tinkaton.py import its frame (the colours, on_ell, the prisms of the hammers,
# the hands, the feet, the eyes cut by a lid, the cheeks).
#
# From the references: a small hot pink figure whose head is most of it, an onion wide below and rounding up to a
# paler pink cap with a soft rim; from the cap's top a cream sprout curls up and over toward +X into a fat bulb.
# Droopy, worried eyes: lavender grey under a pink lid that slopes down to the outer corner, a short dark brow raised
# over each inner corner; a wide open frown, dark purple; pale pink blush on each cheek. Under the head a narrow waist
# flaring to two stubby legs; a grey lavender octahedral nub on the lower front, a little to +X. Two thick stubby arms,
# the -X one out, forward and down to a round three-toed paddle, the +X one raised with its fist round the handle of a
# small hammer that points up and out: a purple handle with a knob below, a purple neck and a big pale lavender block
# of stone at its top, faceted, its end face paler still.
# Reference ratios (3D picture, sprout to feet, the hammer left out): head with its cap / height 0.47, head width /
# height 0.52, the sprout / height 0.2, waist width / head width 0.52, hammer from fist to top / height 0.65.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, axes, eye_look, outward, polygon, args

# --- the line's colours ---------------------------------------------------------------------------------------------
PINK, CAP, CREAM, CREAMDEEP = "#E4508A", "#F29CBC", "#F5D6CF", "#E9AEB0"
BLUSH, IRIS, IRISDEEP, INK = "#F6A9C6", "#A9A3C6", "#6F6894", "#2A1A2A"
MOUTH, LIP = "#5A2050", "#8E4A86"
GEM, HANDLE, NECK, STONE, STONEEND = "#9C94B8", "#5C4C96", "#6A5BA6", "#9A90C6", "#CBC4E6"

# --- the frame (joints: body 0.11, arms (+-0.055, 0, 0.13), head 0.17, legs (+-0.035, 0, 0.06)) ----------------------
HC = np.array([0.0, -0.004, 0.222])            # the onion of a head
HR = np.array([0.094, 0.08, 0.084])
CAPZ = 0.25                                    # the cap's rim


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) along direction d from its centre, and its normal there."""
    d = np.array(d, float); c = np.array(c, float); r = np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def onion(V):
    """A sphere made an onion: full in its lower half, narrowing a little toward its crown."""
    V = V.copy()
    z = V[:, 2]
    w = 1 + 0.05 * np.clip(-z, 0, 1) - 0.1 * np.clip(z, 0, 1) ** 1.5
    V[:, 0] *= w; V[:, 1] *= w
    return V


def prism(a, b, ra, rb, n=8, turn=0.0, jitter=0.0, seed=0):
    """A hard-edged prism of n sides from a to b, radius ra at a and rb at b: a hammer's head, a neck, a block."""
    a, b = np.array(a, float), np.array(b, float)
    e1, e2 = axes(b - a)
    rs = np.random.RandomState(seed)
    pts = []
    for c, r in ((a, ra), (b, rb)):
        for i in range(n):
            th = turn + 2 * math.pi * i / n
            rr = r * (1 + jitter * (rs.rand() - 0.5))
            pts.append(c + rr * (math.cos(th) * e1 + math.sin(th) * e2))
    return hull(pts)


def fist(k, at, d, r, colour, bone, name="fist"):
    """A round fist closed round a handle running along d: a ball and three knuckles across the front."""
    at = np.array(at, float)
    k.add(name, ell(at, (r, r * 0.95, r * 0.95)), colour, bone)
    e1, e2 = axes(d)
    for i, t in enumerate((-0.55, 0.0, 0.55)):
        p = at + np.array(d, float) * t * r * 0.9 + np.array([0, -1.0, 0]) * r * 0.62
        k.add(f"{name}k{i}", ell(p, (r * 0.42, r * 0.36, r * 0.36)), colour, bone)


def paddle(k, at, d, r, colour, bone, name="paddle"):
    """A round open paw at an arm's end, flattened along d, with three toes along its front edge."""
    at = np.array(at, float); d = unit(*d)
    side = unit(*np.cross(d, (0, 0, 1.0)))
    k.add(name, ell(at, (r, r, r * 0.75)), colour, bone)
    for i, t in enumerate((-0.62, 0.0, 0.62)):
        p = at + d * r * 0.78 + side * r * t - np.array([0, 0, r * 0.18])
        k.add(f"{name}t{i}", ell(p, (r * 0.36, r * 0.36, r * 0.32)), colour, bone)


def foot(k, at, r, colour, bone, name="foot"):
    """A round stubby foot, mirrored to -X."""
    k.add(name, ell(at, r), colour, bone, mirror=True)


def cheek(k, c, r, d, half, colour=BLUSH):
    p, n = on_ell(c, r, d)
    k.decal(p, n, half, colour, eps=0.0018, rings=12, segs=48, mirror=True)


def lid_eye(iris=IRIS, deep=IRISDEEP, ink=INK, lidc=PINK, slope=0.42, top=0.22, glint=True):
    """
    A droopy eye cut by its lid: lavender grey, deeper below, a pink lid over its top whose dark line slopes down to
    the outer corner (u is outward). Returns the open look and the lid's line.
    """
    I, D, K, L, W = srgb(iris), srgb(deep), srgb(ink), srgb(lidc), np.array([1.0, 1.0, 1.0])
    def line(u):
        return top - slope * u
    def look(u, v):
        c = np.tile(I, (len(u), 1))
        c[v < -0.35 + 0.15 * u * u] = D
        c[u * u + v * v > 0.86 ** 2] = K
        if glint:
            c[((u + 0.25) / 0.2) ** 2 + ((v - line(u) + 0.38) / 0.16) ** 2 < 1] = W
        c[v > line(u)] = L
        c[np.abs(v - line(u)) < 0.13] = K
        return c
    return look, line


def lay_eyes(k, c, r, d, half, look, lidc, ink, up=(0, 0, 1)):
    """Both eyes on an ellipsoid at direction d (+X side; the -X one mirrored), the five states from the open look."""
    p, n = on_ell(c, r, d)
    def lay(state):
        if state == "Fierce":
            def fierce(u, v):
                col = look(u, v)
                edge = 0.05 - 0.55 * u            # the lid slanting down to the inner corner
                col[v > edge] = lidc
                col[np.abs(v - edge) < 0.14] = ink
                return col
            f = fierce
        else:
            f = eye_look(state, look, lidc, ink, line=0.15)
        k.decal(p, n, half, f, eps=0.0018, rings=20, segs=56, mirror=True, up=up)
    return lay


def tinkatink(src, output):
    k = Kit(src, coat="fur")

    # --- the head: an onion, a paler cap with a soft rim over its crown, a sprout curling up from the cap
    k.add("head", ell(HC, HR, deform=onion), PINK, "head")
    def capdeform(V):
        V = onion(V)
        low = np.clip((0.28 - V[:, 2]) / 0.25, 0, 1)
        V[:, 0] *= 1 - 0.5 * low; V[:, 1] *= 1 - 0.5 * low
        return V
    k.add("cap", ell(HC + (0, 0, 0.002), HR + 0.005, deform=capdeform), CAP, "head")
    k.paint(lambda P: inside(CAPZ + 0.006 * np.cos(np.arctan2(P[:, 1], P[:, 0])) - P[:, 2], 0.0025)
            * inside(np.linalg.norm((P - HC) / (HR * 1.3), axis=1) - 1, 0.01), CAP)
    sprout = [(0.0, 0.0, 0.29), (0.0, -0.004, 0.315), (0.008, -0.008, 0.338), (0.024, -0.01, 0.35)]
    k.add("sprout", outward(tube(sprout, [0.014, 0.016, 0.021, 0.024], seg=20)), CREAM, "head", fuse=False,
          surface="hair")
    k.add("bulb", outward(ell((0.028, -0.012, 0.352), (0.034, 0.028, 0.026), rot=(0, 20, 0))), CREAM, "head",
          fuse=False, surface="hair")
    k.add("hook", outward(ell((0.055, -0.012, 0.338), (0.016, 0.017, 0.022), rot=(0, 30, 0))), CREAMDEEP, "head",
          fuse=False, surface="hair")

    # --- the body: a narrow waist under the head flaring to two stubby legs, a lavender nub on its lower front
    k.add("lower", ell((0, 0.0, 0.098), (0.062, 0.052, 0.07), deform=lambda V: V * np.array([1, 1, 1.0])[None] * (1 + 0.12 * np.clip(-V[:, 2:3], 0, 1))), PINK, "body")
    k.add("leg", tube([(0.036, 0.0, 0.06), (0.042, -0.004, 0.025)], [0.028, 0.027]), PINK, "leg{s}", mirror=True)
    foot(k, (0.044, -0.01, 0.016), (0.03, 0.034, 0.017), PINK, "leg{s}")
    nub = np.array([0.018, -0.056, 0.078])
    k.add("nub", hull([nub + (0.022, 0.004, 0), nub - (0.022, -0.004, 0), nub + (0, 0, 0.024), nub - (0, 0, 0.024),
                       nub + (0, -0.02, 0), nub + (0, 0.012, 0)]), GEM, "body", fuse=False, surface="metal")

    # --- the arms: thick and stubby. -X out, forward and down to a three-toed paddle; +X raised round the hammer
    k.add("armL", tube([(-0.05, 0.0, 0.13), (-0.085, -0.03, 0.105), (-0.112, -0.062, 0.072)], [0.03, 0.027, 0.026]),
          PINK, "armL")
    paddle(k, (-0.122, -0.074, 0.06), (-0.35, -1, -0.2), 0.03, PINK, "armL", "paddleL")
    G = np.array([0.135, -0.06, 0.118])            # the fist on the handle
    k.add("armR", tube([(0.05, 0.0, 0.13), (0.09, -0.03, 0.125), G + (-0.012, 0.006, 0.0)], [0.03, 0.027, 0.025]),
          PINK, "armR")
    D = unit(0.42, -0.18, 0.89)                   # the hammer's way: up, out to +X and a little forward
    fist(k, G, D, 0.026, PINK, "armR", "fistR")

    # --- the hammer: a purple handle, a knob below the fist, a purple neck and a faceted block of pale stone
    k.add("handle", outward(tube([G - D * 0.035, G + D * 0.085], [0.012, 0.012], seg=16)), HANDLE, "armR",
          fuse=False, surface="metal")
    k.add("knob", prism(G - D * 0.05, G - D * 0.03, 0.017, 0.016, n=8, turn=0.3), HANDLE, "armR", fuse=False,
          surface="metal")
    k.add("neck", prism(G + D * 0.08, G + D * 0.128, 0.02, 0.03, n=8, turn=0.2, jitter=0.12, seed=3), NECK, "armR",
          fuse=False, surface="metal")
    k.add("block", prism(G + D * 0.122, G + D * 0.22, 0.044, 0.047, n=8, turn=0.1, jitter=0.16, seed=5), STONE,
          "armR", fuse=False, surface="metal")
    k.add("end", prism(G + D * 0.215, G + D * 0.228, 0.04, 0.036, n=8, turn=0.1, jitter=0.1, seed=6), STONEEND,
          "armR", fuse=False, surface="metal")

    # --- the face: cheeks, worried brows, a wide open frown
    cheek(k, HC, HR, (0.82, -0.52, -0.25), (0.022, 0.018))
    for s in (1, -1):
        p, n = on_ell(HC, HR, (0.24 * s, -0.95, 0.27))
        k.decal(p, n, (0.016, 0.0042), INK, eps=0.0018, rings=8, segs=40, up=unit(0.42 * s, 0, 1))
    M, Lp = srgb(MOUTH), srgb(LIP)
    def frown(u, v):
        c = np.tile(M, (len(u), 1))
        c[v < 0.05 - 0.5 * (1 - u * u)] = Lp
        return c
    mp, mn = on_ell(HC, HR, (0.0, -1.0, -0.5))
    k.decal(mp, mn, (0.021, 0.019), frown, eps=0.0018, rings=14, segs=64,
            outline=lambda th: 1.0 if math.sin(th) > 0 else 0.3 + 0.7 * abs(math.cos(th)) ** 2)

    k.build(voxel=0.0035, smooth=4, band=0.004, wband=0.025)

    # --- the eyes: droopy, cut by the lid
    look, _ = lid_eye()
    k.eye_states(lay_eyes(k, HC, HR, (0.44, -0.88, -0.02), (0.024, 0.02), look, srgb(PINK), srgb(INK)))
    k.export(output)


if __name__ == "__main__":
    tinkatink(*args())
