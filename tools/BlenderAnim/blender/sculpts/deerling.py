# Deerling (its spring form) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/deerling.py -- <the export's Deerling.glb> <out.glb>
# From the references: a small fawn with a big round head on a short neck; a pink hood over the crown and the back of
# the head, the face below it cream round big dark eyes with a white glint, a small black nose and a little smile;
# big ears out to the sides, pink outside and yellow within; a yellow five-petalled flower on the crown with a pink
# bud standing behind it; a pink saddle over the back and chest whose hem hangs in round scallops edged in yellow, the
# belly and the legs cream below it, yellow spots on the haunch; dark hooves; a short pink tuft of a tail standing up.
#
# Written to be grown from (sawsbuck.py imports it with sibling("deerling")): the hem, the legs, the ear and the
# flower are the functions below; the calls that build Deerling are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, slab, lathe, outward, srgb, inside, smoothstep, unit, polygon,
                       eye_look, args, sibling)

PINK, CREAM, YELLOW, HOOF = "#E99AB2", "#F5EAC6", "#F6D03C", "#4C4752"
NOSE, BUD, CENTRE = "#25212A", "#B784BC", "#E8A62C"
INK, IRIS, GLINT = srgb("#141218"), srgb("#3A3540"), np.array([1.0, 1.0, 1.0])


def on_ell(c, r, d):
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / np.array(r)) ** 2).sum())
    return np.array(c, float) + d * t


def leg(k, name, x, y, top, r, bone, color=CREAM, hoof=HOOF, knee=0.0):
    """A leg: a thick tapering shin from under its shoulder or thigh to a dark hoof."""
    rt, rk, rb = r
    k.add(name, tube([(x, y, top), (x + 0.001, y + knee, top * 0.5), (x + 0.001, y, 1.05 * rb)], [rt, rk, rb]),
          color, bone, mirror=True)
    hr = rb * 1.1
    k.add(name + "hoof", lathe((x + 0.001, y - 0.002, 0.0), (0, 0, 1),
                               [(0, 0), (hr, 0), (hr * 1.03, hr * 0.3), (hr * 0.85, hr * 1.25), (0, hr * 1.25)]),
          hoof, bone, fuse=False, mirror=True, surface="shell")


def hem_edge(y, base, drop, period, y0):
    """The saddle's lower edge along the body: round lobes hanging between points every period."""
    return base - drop * np.abs(np.sin(math.pi * (y - y0) / period)) ** 0.7


def hem(k, base, drop, period, y0, band, ylim, top, saddle, rim, edge=0.0025):
    """The saddle painted down to its scalloped hem, a rim of another colour just under it, on the trunk only."""
    def zone(P):
        return inside(ylim[0] - P[:, 1], 0.004) * inside(P[:, 1] - ylim[1], 0.004) * inside(P[:, 2] - top, 0.004)
    def over(P):
        return inside(hem_edge(P[:, 1], base, drop, period, y0) - P[:, 2], edge) * zone(P)
    def under(P):
        e = hem_edge(P[:, 1], base, drop, period, y0)
        return inside(e - band - P[:, 2], edge) * inside(P[:, 2] - e, edge) * zone(P)
    k.paint(under, rim)
    k.paint(over, saddle)


def ear(k, root, d, n, length, half, outer, inner, bone="ear{s}"):
    """An ear: a flat leaf from root along d, its face to n; a smaller plate of the lining laid on its face."""
    d = unit(*d); n = np.array(n, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    ez = np.cross(n, d)
    L, W = length, half
    out = [(0, -0.6 * W), (0.3 * L, -W), (0.7 * L, -0.75 * W), (L, 0), (0.7 * L, 0.75 * W), (0.3 * L, W),
           (0, 0.6 * W)]
    k.add("ear", slab(out, root, d, ez, 0.016), outer, bone, fuse=False, mirror=True)
    lin = [(0.12 * L + 0.75 * a, 0.62 * b) for a, b in out]
    k.add("earin", slab(lin, np.array(root) + n * 0.0085, d, ez, 0.006), inner, bone, fuse=False, mirror=True)


def flower(k, c, n, petal, colour, centre, bone="head", petals=5, turn=0.3):
    """A flower: petals of flat-faced plates radiating in the plane square to n, a round middle."""
    c = np.array(c, float); n = unit(*n)
    a = np.cross(n, (0, 1.0, 0)); a /= np.linalg.norm(a); b = np.cross(n, a)
    for i in range(petals):
        t = 2 * math.pi * i / petals + turn
        d = math.cos(t) * a + math.sin(t) * b
        side = np.cross(n, d)
        # a rounded petal with a soft point: a teardrop of corners round its own middle
        p = [c]
        for j in range(1, 10):
            w = math.pi * j / 10
            r = 0.5 - 0.5 * math.cos(w)
            p += [c + d * petal * r + side * petal * 0.36 * math.sin(w) ** 0.8 * (1 - 0.35 * r) * sg for sg in (1, -1)]
        p.append(c + d * petal)
        pts = [q + n * s for q in p for s in (0.004, -0.004)]
        pts.append(c + d * petal * 0.5 + n * 0.007)
        k.add(f"petal{i}", hull(pts), colour, bone, fuse=False, surface="leaf")
    k.add("bloom", ell(c + n * 0.005, (petal * 0.28, petal * 0.28, petal * 0.28), sub=3), centre, bone, fuse=False,
          surface="leaf")


def dark_eye(u, v):
    """The open eye: dark grey with a black pupil, a big white glint up and out and a small one low."""
    c = np.tile(INK, (len(u), 1))
    c[np.hypot(u, v) < 0.85] = IRIS
    c[np.hypot(u + 0.05, v + 0.06) < 0.5] = INK
    c[((u - 0.12) / 0.26) ** 2 + ((v - 0.3) / 0.24) ** 2 < 1] = GLINT
    c[np.hypot(u + 0.28, v + 0.42) < 0.11] = GLINT
    return c


def oval(th):
    return 1.0


def deerling(src, out):
    k = Kit(src, coat="fur")

    # --- the trunk: round chest and rump, cream below the saddle --------------------------------------------
    k.add("barrel", ell((0, 0.01, 0.157), (0.05, 0.098, 0.048)), CREAM, "body")
    k.add("chest", ell((0, -0.056, 0.165), (0.048, 0.05, 0.053)), CREAM, "body")
    k.add("rump", ell((0, 0.07, 0.162), (0.052, 0.056, 0.053)), CREAM, "body")

    # --- legs: shoulders and thighs fused over the top, thick shins, dark hooves ----------------------------
    k.add("shoulder", ell((0.03, -0.058, 0.122), (0.029, 0.036, 0.05)), CREAM, "leg{s}", mirror=True)
    k.add("thigh", ell((0.032, 0.076, 0.126), (0.033, 0.046, 0.056)), CREAM, "hind{s}", mirror=True)
    leg(k, "fore", 0.034, -0.06, 0.12, (0.02, 0.0155, 0.0145), "leg{s}")
    leg(k, "hind", 0.035, 0.08, 0.12, (0.021, 0.0155, 0.0145), "hind{s}", knee=0.006)

    # --- a short thick neck to a big round head; a short muzzle, a small black nose ------------------------
    k.add("neck", tube([(0, -0.062, 0.18), (0, -0.08, 0.235), (0, -0.092, 0.262)], [0.04, 0.035, 0.034]), PINK,
          "head")
    H, HR = np.array([0, -0.105, 0.305]), np.array([0.059, 0.063, 0.068])
    k.add("head", ell(H, HR), PINK, "head")
    k.add("muzzle", ell((0, -0.163, 0.276), (0.037, 0.043, 0.034), rot=(8, 0, 0)), PINK, "head")
    k.add("nose", ell((0, -0.204, 0.284), (0.012, 0.009, 0.009)), NOSE, "head", fuse=False, surface="skin")

    # the hood: pink over the crown and the back of the head, the face cream below and in front
    def face(P):
        f = -(P[:, 1] - H[1]); up = P[:, 2] - H[2]
        front = inside(0.012 - (f - 0.55 * (up - 0.012)), 0.003)
        chin = inside(up + 0.045, 0.004) * inside(-0.03 - f, 0.004)
        near = inside(np.linalg.norm(P - H, axis=1) - 0.13, 0.004) * inside(0.235 - P[:, 2], 0.004)
        return np.maximum(front, chin) * near
    k.paint(face, CREAM)
    # the saddle: pink over the back, its scalloped hem edged in yellow; spots on the haunch
    hem(k, 0.142, 0.016, 0.042, -0.1, 0.007, (-0.12, 0.14), 0.235, PINK, YELLOW)
    for c, r in (((0.044, 0.07, 0.19), 0.011), ((0.04, 0.095, 0.2), 0.007), ((0.047, 0.045, 0.177), 0.007)):
        k.decal(c, unit(1, 0.1, 0.55), (r, r), YELLOW, eps=0.002, rings=8, segs=32, mirror=True)

    # --- the tail: a short pink tuft standing up and back ---------------------------------------------------
    k.add("tail", blade([(0, 0.105, 0.185), (0, 0.122, 0.225), (0, 0.14, 0.25)], [0.022, 0.016, 0.003],
                        (0, 1, 0), 0.6), PINK, "tail")

    # --- the ears: big, out to the sides and up, pink with a yellow lining ---------------------------------
    ear(k, (0.034, -0.094, 0.345), (1, 0.45, 0.55), unit(0.1, -0.85, 0.5), 0.088, 0.024, PINK, YELLOW)

    # --- the flower on the crown, the pink bud standing behind it -----------------------------------------
    k.add("bud", hull([(0.011, -0.088, 0.355), (-0.011, -0.088, 0.355), (0, -0.075, 0.357), (0, -0.1, 0.357),
                       (0.009, -0.07, 0.385), (-0.009, -0.07, 0.385), (0.0, -0.05, 0.41)]), BUD, "head", fuse=False,
          surface="leaf")
    flower(k, (0, -0.112, 0.37), (0, -0.75, 1), 0.04, YELLOW, CENTRE)

    # --- a little smile on each side of the muzzle --------------------------------------------------------
    k.decal((0.026, -0.17, 0.262), unit(0.55, -0.7, -0.35), (0.011, 0.0028), INK, up=unit(0, 0.4, 1), eps=0.003,
            rings=3, segs=24, mirror=True)

    # --- the eyes: big and dark, on the cream face, turned a little forward ------------------------------
    de = unit(0.6, -0.74, 0.12)
    E = on_ell(H, HR, de)
    C = srgb(CREAM)
    def eyes(state):
        k.decal(E, de, (0.024, 0.03), eye_look(state, dark_eye, C, INK, line=0.15), up=unit(0, 0.1, 1), eps=0.006,
                rings=20, segs=56, mirror=True)

    k.build(voxel=0.0034, smooth=4)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    deerling(*args())
